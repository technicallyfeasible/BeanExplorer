using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;

namespace BeanExplorer.Connector
{
	public class DataReceivedEventArgs : EventArgs
	{
		public BeanMsgId MsgId { get; set; }
		public Byte[] Data { get; set; }

		public DataReceivedEventArgs(BeanMsgId msgId, Byte[] data)
		{
			MsgId = msgId;
			Data = data;
		}
	}

	public delegate void DataReceivedEventHandler(Object sender, DataReceivedEventArgs e);


	/// <summary>
	/// PunchThrough Bean communication class
	/// </summary>
	public class Bean
	{
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
			ProgressEventHandler handler = this.Progress;
			if (handler != null) handler(this, e);
		}

		public event DataReceivedEventHandler DataReceived;
		protected virtual void OnDataReceived(DataReceivedEventArgs e)
		{
			DataReceivedEventHandler handler = this.DataReceived;
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

		public async void Subscribe(String deviceId)
		{
			Unsubscribe();

			GattDeviceService service = await GattDeviceService.FromIdAsync(deviceId);
			if (service == null)
			{
				OnProgress(new ProgressEventArgs("Serial service not found", 0, 0));
				return;
			}
			IReadOnlyList<GattCharacteristic> chars = service.GetCharacteristics(BeanConstants.BeanSerialCharUuid);
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

			c.ValueChanged += OnValueChanged;
			GattCommunicationStatus status = await c.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
			if (status != GattCommunicationStatus.Success)
			{
				OnProgress(new ProgressEventArgs("Error: " + status, 0, 0));
			}
			currentCharacteristic = c;
		}


		public void Unsubscribe()
		{
			if (currentCharacteristic == null)
				return;

			currentCharacteristic.ValueChanged -= OnValueChanged;

			currentCharacteristic = null;
		}

		private void OnValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
		{
			var data = new byte[args.CharacteristicValue.Length];
			DataReader.FromBuffer(args.CharacteristicValue).ReadBytes(data);
			
			String debugString = Encoding.UTF8.GetString(data, 0, data.Length);
			OnProgress(new ProgressEventArgs(debugString, 0, 0));

			// Received a single GT packet
			var start = ((data[0] & 0x80) != 0);	//Set to 1 for the first packet of each App Message, 0 for every other packet
			var messageCount = (data[0] & 0x60);	//Increments and rolls over on each new GT Message (0, 1, 2, 3, 0, ...)
			var packetCount = (data[0] & 0x1F);		//Represents the number of packets remaining in the GST message

			if (start)
				this.packets = new Byte[packetCount + 1][];

			// append buffer without header
			this.packets[this.packets.Length - packetCount - 1] = data;

			if (packetCount == 0)
			{
				// merge packets
				Int32 length = this.packets.Sum(p => p.Length - 1);
				Byte[] finalData = new Byte[length];
				Int32 index = 0;
				foreach (Byte[] packet in this.packets)
				{
					Array.Copy(packet, 1, finalData, index, packet.Length - 1);
					index += packet.Length - 1;
				}
				this.packets = null;

				Int32 size = finalData[0];

				// check msg crc
				UInt16 dataCrc = Crc16.ComputeChecksum(finalData, 0, finalData.Length - 2);
				UInt16 msgCrc = (UInt16) (finalData[finalData.Length - 2] | (UInt16) (finalData[finalData.Length - 1] << 8));
				if (dataCrc != msgCrc || size != finalData.Length - 4)
				{
					OnDataReceived(new DataReceivedEventArgs(BeanMsgId.Invalid, finalData));
					return;
				}

				Byte[] payload = new Byte[size - 2];
				Array.Copy(finalData, 4, payload, 0, payload.Length);
				BeanMsgId msgId = (BeanMsgId) BitConverter.ToInt16(finalData, 2);

				if (msgId == BeanMsgId.SerialData)
					OnDataReceived(new DataReceivedEventArgs(msgId, payload));
			}
		}


		/// <summary>
		/// Send a packet to the device
		/// </summary>
		public async void Send(BeanMsgId msgId, Byte[] data)
		{
			Int32 flag = 0x80;

			// build message
			DataWriter msg = new DataWriter();
			msg.ByteOrder = ByteOrder.LittleEndian;
			msg.WriteByte((Byte) (data.Length + 2));
			msg.WriteByte(0);
			msg.WriteUInt16((UInt16) msgId);
			msg.WriteBytes(data);
			IBuffer b = msg.DetachBuffer();
			UInt16 crc = Crc16.ComputeChecksum(b, 0, (Int32) b.Length);
			msg.WriteBuffer(b);
			msg.WriteUInt16(crc);
			b = msg.DetachBuffer();
			
			Byte[] debug = new Byte[b.Length];
			b.CopyTo(debug);

			Int32 packages = (Int32) Math.Ceiling((Double) b.Length / 19) - 1;

			// loop until everything has been sent
			DataWriter writer = new DataWriter();
			writer.ByteOrder = ByteOrder.LittleEndian;
			UInt32 offset = 0;
			do
			{
				UInt32 size = Math.Min(19, b.Length - offset);
				Byte header = (Byte) (flag | ((this.msgCounter & 0x03) << 5) | (packages & 0x1F));
				writer.WriteByte(header);
				writer.WriteBuffer(b, offset, size);
				IBuffer package = writer.DetachBuffer();
				debug = new Byte[package.Length];
				package.CopyTo(debug);
				GattCommunicationStatus result = await this.currentCharacteristic.WriteValueAsync(package, GattWriteOption.WriteWithoutResponse);
				flag = 0;
				packages--;
				this.msgCounter++;
				this.msgCounter &= 0x03;
				offset += size;
			} while (offset < b.Length);
		}
	}
}
