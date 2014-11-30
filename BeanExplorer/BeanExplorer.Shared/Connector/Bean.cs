using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Devices.Enumeration.Pnp;
using Windows.Storage.Streams;

namespace BeanExplorer.Connector
{
	/// <summary>
	/// PunchThrough Bean communication class
	/// </summary>
	public class Bean
	{
		private Object recvLock = new Object();
		private PnpObjectWatcher watcher;
		private String deviceContainerId;
		private GattDeviceService currentService;
		private GattCharacteristic currentCharacteristic;
		private Byte[][] packets;
		private Int32 msgCounter;


		/*	Protocol
			"�\0\0\0Listening...}M"
			Header:		128
			Length:		14
			Reserved:	0
			Msg Id:		0, 0
			Data:
			76	byte
			105	byte
			115	byte
			116	byte
			101	byte
			110	byte
			105	byte
			110	byte
			103	byte
			46	byte
			46	byte
			46	byte

			CRC:	125, 77
		 */


		public event ProgressEventHandler Progress;
		protected virtual void OnProgress(ProgressEventArgs e)
		{
			ProgressEventHandler handler = Progress;
			if (handler != null) handler(this, e);
		}

		public event DataReceivedEventHandler DataReceived;
		protected virtual void OnDataReceived(DataReceivedEventArgs e)
		{
			DataReceivedEventHandler handler = DataReceived;
			if (handler != null) handler(this, e);
		}



		/// <summary>
		/// Find all connected Bean devices
		/// </summary>
		/// <returns></returns>
		public async Task<List<DeviceInformation>> FindDevices()
		{
			List<DeviceInformation> results = new List<DeviceInformation>();

			OnProgress(new ProgressEventArgs("Scanning devices", 0, 0));
			DeviceInformationCollection devices = await DeviceInformation.FindAllAsync(GattDeviceService.GetDeviceSelectorFromUuid(BeanConstants.BeanSerialServiceUuid), null);
			Int32 index = 0;
			foreach (DeviceInformation di in devices)
			{
				index++;
				OnProgress(new ProgressEventArgs("Device: " + di.Name, index, devices.Count));
				// if the device supports the Bean serial service and characteristic then add it to the list of found devices
				results.Add(di);
			}
			OnProgress(new ProgressEventArgs(results.Count + " Beans found", devices.Count, devices.Count));

			return results;
		}

		/// <summary>
		/// Register to be notified when a connection is established to the Bluetooth device
		/// </summary>
		private void StartDeviceConnectionWatcher()
		{
			watcher = PnpObject.CreateWatcher(PnpObjectType.DeviceContainer, new[] { "System.Devices.Connected" }, String.Empty);
			watcher.Updated += DeviceConnectionUpdated;
			watcher.Start();
		}


		/// <summary>
		/// Invoked when a connection is established to the Bluetooth device
		/// </summary>
		/// <param name="sender">The watcher object that sent the notification</param>
		/// <param name="args">The updated device object properties</param>
		private async void DeviceConnectionUpdated(PnpObjectWatcher sender, PnpObjectUpdate args)
		{
			if (this.deviceContainerId != args.Id)
				return;

			var connectedProperty = args.Properties["System.Devices.Connected"];
			OnProgress(new ProgressEventArgs("Connection state changed to " + connectedProperty, 0, 0));

			/*bool isConnected;
			if (Boolean.TryParse(connectedProperty.ToString(), out isConnected) && isConnected)
			{
				var status = await currentCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
				if (status == GattCommunicationStatus.Success)
				{
					//IsServiceInitialized = true;
					// Once the Client Characteristic Configuration Descriptor is set, the watcher is no longer required
					watcher.Stop();
					watcher = null;
				}
			}*/
		}


		public async void Subscribe(String deviceId)
		{
			Unsubscribe();

			try
			{
				OnProgress(new ProgressEventArgs("Subscribing to " + deviceId, 0, 0));
				currentService = await GattDeviceService.FromIdAsync(deviceId);
				if (currentService == null)
				{
					OnProgress(new ProgressEventArgs("Serial service not found", 0, 0));
					return;
				}

				IReadOnlyList<GattCharacteristic> chars = currentService.GetCharacteristics(BeanConstants.BeanSerialCharUuid);
				// check characteristic, windows cuts off everything after the \0 so we allow "B" as well
				if (chars.Count == 0 || (chars[0].UserDescription != "B\0e\0a\0n\0 \0T\0r\0a\0n\0s\0p\0o\0r\0t" && chars[0].UserDescription != "B"))
				{
					OnProgress(new ProgressEventArgs("Serial characteristic not found", 0, 0));
					return;
				}
				GattCharacteristic c = chars[0];

				Boolean canNotify = c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify);
				if (!canNotify)
				{
					OnProgress(new ProgressEventArgs("Device does not support notifications", 0, 0));
					return;
				}

				this.deviceContainerId = deviceId;
				c.ValueChanged += OnValueChanged;
				GattCommunicationStatus status = await c.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
				if (status == GattCommunicationStatus.Unreachable)
				{
					// Register a PnpObjectWatcher to detect when a connection to the device is established,
					// such that the application can retry device configuration.
					//StartDeviceConnectionWatcher();
				}
				DeviceInformation di = await DeviceInformation.CreateFromIdAsync(deviceId, new[] { "System.Devices.ContainerId" });
				deviceContainerId = "{" + di.Properties["System.Devices.ContainerId"] + "}";
				StartDeviceConnectionWatcher();
				currentCharacteristic = c;
				OnProgress(new ProgressEventArgs("Subscribed successfully", 0, 0));
			}
			catch (Exception)
			{
				OnProgress(new ProgressEventArgs("Subscribe failed", 0, 0));
			}
		}


		public void Unsubscribe()
		{
			if (currentCharacteristic == null)
				return;

			currentCharacteristic.ValueChanged -= OnValueChanged;
			currentCharacteristic = null;

			if (currentService != null)
				currentService.Dispose();
			currentService = null;
		}

		private void OnValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
		{
			var data = new byte[args.CharacteristicValue.Length];
			DataReader.FromBuffer(args.CharacteristicValue).ReadBytes(data);
			
			//String debugString = Encoding.UTF8.GetString(data, 0, data.Length);
			//OnProgress(new ProgressEventArgs(debugString, 0, 0));

			// Received a single GT packet
			var start = ((data[0] & 0x80) != 0);	//Set to 1 for the first packet of each App Message, 0 for every other packet
			var messageCount = (data[0] & 0x60);	//Increments and rolls over on each new GT Message (0, 1, 2, 3, 0, ...)
			var packetCount = (data[0] & 0x1F);		//Represents the number of packets remaining in the GST message
			List<Byte> finalData;

			lock (recvLock)
			{
				if (start)
					this.packets = new Byte[packetCount + 1][];

				// append buffer without header
				this.packets[this.packets.Length - packetCount - 1] = data;

				if (packetCount > 0)
					return;

				// merge packets
				finalData = new List<Byte>();
				foreach (Byte[] packet in this.packets)
				{
					for (Int32 i = 0; i < packet.Length - 1; i++)
						finalData.Add(packet[i + 1]);
				}
				this.packets = null;
			}

			Int32 size = finalData[0];

			// check msg crc
			UInt16 dataCrc = Crc16.ComputeChecksum(finalData.ToArray(), 0, finalData.Count - 2);
			UInt16 msgCrc = (UInt16) (finalData[finalData.Count - 2] | (UInt16) (finalData[finalData.Count - 1] << 8));
			if (dataCrc != msgCrc || size != finalData.Count - 4)
			{
				OnDataReceived(new InvalidDataReceivedEventArgs(finalData.ToArray()));
				return;
			}

			Byte[] payload = new Byte[size - 2];
			Array.Copy(finalData.ToArray(), 4, payload, 0, payload.Length);
			BeanMsgId msgId = (BeanMsgId) (BitConverter.ToInt16(finalData.ToArray(), 2) & ~0x8000);

			if (msgId == BeanMsgId.SerialData)
				OnDataReceived(new SerialDataReceivedEventArgs(payload));
			else if (msgId == BeanMsgId.CcTempRead)
				OnDataReceived(new TemperatureDataReceivedEventArgs(payload[0]));
			else if (msgId == BeanMsgId.CcAccelRead)
			{
				var x = (Int16) ((payload[1] << 8) | payload[0]) * 0.00391;
				var y = (Int16)((payload[3] << 8) | payload[2]) * 0.00391;
				var z = (Int16)((payload[5] << 8) | payload[4]) * 0.00391;
				OnDataReceived(new AccelerometerDataReceivedEventArgs(x, y, z));
			}
		}


		private Byte[] GetBytes(UInt16 value)
		{
			return new[]{ (Byte) (value & 0xFF), (Byte) ((value >> 8) & 0xFF) };
		}

		/// <summary>
		/// Send a packet to the device
		/// </summary>
		public async void Send(BeanMsgId msgId, Byte[] data)
		{
			try
			{
				// no data, make it quicker
				List<Byte> package = new List<Byte>(6 + (data != null ? data.Length : 0));
				if (data == null || data.Length == 0)
				{
					package.Add(2);
					package.Add(0);
					package.AddRange(GetBytes((UInt16)msgId));
				}
				else
				{
					package.Add((Byte)(data.Length + 2));
					package.Add(0);
					package.AddRange(GetBytes((UInt16)msgId));
					package.AddRange(data);
				}
				UInt16 crc = Crc16.ComputeChecksum(package.ToArray());
				package.AddRange(GetBytes(crc));
				
				// loop until everything has been sent
				DataWriter msg = new DataWriter();
				msg.ByteOrder = ByteOrder.LittleEndian;
				Int32 flag = 0x80;
				Int32 packages = (Int32)Math.Ceiling((Double)package.Count / 19) - 1;
				Int32 offset = 0;
				do
				{
					Byte header = (Byte)(flag | ((this.msgCounter & 0x03) << 5) | (packages & 0x1F));
					Int32 size = Math.Min(package.Count - offset, 19);

					// build message
					msg.WriteByte(header);
					for (Int32 i = 0; i < size; i++)
						msg.WriteByte(package[offset + i]);
					IBuffer b = msg.DetachBuffer();

					Byte[] debug = new Byte[b.Length];
					b.CopyTo(debug);

					GattCommunicationStatus result = await this.currentCharacteristic.WriteValueAsync(b, GattWriteOption.WriteWithoutResponse);
					if (result != GattCommunicationStatus.Success)
						throw new Exception(result.ToString());

					flag = 0;
					packages--;
					this.msgCounter++;
					this.msgCounter &= 0x03;
					offset += size;
				} while (offset < package.Count);
			}
			catch (Exception ex)
			{
				OnProgress(new ProgressEventArgs("Send error: " + ex.Message, 0, 0));
			}
		}

		public void RequestTemperature()
		{
			Send(BeanMsgId.CcTempRead, null);
		}

		public void RequestAccelerometer()
		{
			Send(BeanMsgId.CcAccelRead, null);
		}
	}
}
