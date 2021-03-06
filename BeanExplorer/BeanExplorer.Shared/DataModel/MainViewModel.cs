﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.System.Threading;
using Windows.UI.Core;
using BeanExplorer.Connector;

namespace BeanExplorer.DataModel
{
	public class Device : INotifyPropertyChanged
	{
		private String serialMessage;
		private String temperature;
		private String x;
		private String y;
		private String z;

		public String Id { get; set; }
		public String Name { get; set; }

		public String SerialMessage
		{
			get { return this.serialMessage; }
			set
			{
				if (value == this.serialMessage)
					return;
				this.serialMessage = value;
				OnPropertyChanged();
			}
		}

		public String Temperature
		{
			get { return this.temperature; }
			set
			{
				if (value == this.temperature)
					return;
				this.temperature = value;
				OnPropertyChanged();
			}
		}

		public String X
		{
			get { return this.x; }
			set
			{
				if (value == this.x)
					return;
				this.x = value;
				OnPropertyChanged();
			}
		}
		public String Y
		{
			get { return this.y; }
			set
			{
				if (value == this.y)
					return;
				this.y = value;
				OnPropertyChanged();
			}
		}
		public String Z
		{
			get { return this.z; }
			set
			{
				if (value == this.z)
					return;
				this.z = value;
				OnPropertyChanged();
			}
		}

		public Device(String id, String name)
		{
			Id = id;
			Name = name;
			SerialMessage = "Hello Bean!";
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected virtual void OnPropertyChanged([CallerMemberName] String propertyName = null)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
		}
	}

    public class MainViewModel : INotifyPropertyChanged
    {
	    private CoreDispatcher dispatcher;
		private readonly Bean bean;
	    private Device currentDevice;
	    public ObservableCollection<String> Status { get; set; }
	    public ObservableCollection<Device> Devices { get; set; }

	    public Device CurrentDevice
	    {
		    get { return currentDevice; }
		    set
		    {
			    if (this.currentDevice == value)
				    return;
			    this.currentDevice = value;
				OnPropertyChanged();
		    }
	    }

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
		}


	    public MainViewModel()
	    {
			this.dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
			Status = new ObservableCollection<String>();
			Devices = new ObservableCollection<Device>();

		    if (Windows.ApplicationModel.DesignMode.DesignModeEnabled)
		    {
			    Status.Add("Scanning devices");
				Devices.Add(new Device("1", "Bean 1"));
				Devices.Add(new Device("2", "Bean 2"));
				Devices.Add(new Device("3", "Bean 3"));
				currentDevice = new Device("1", "Bean 1");
		    }
		    else
		    {
			    this.bean = new Bean();
			    bean.Progress += BeanProgress;
			    bean.DataReceived += BeanReceived;
			    ThreadPool.RunAsync(ScanDevices);
		    }
	    }


	    private void OnUiThread(Action action)
		{
			this.dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => action());
		}


	    /// <summary>
		/// Progress callback
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BeanProgress(object sender, ProgressEventArgs e)
	    {
			OnUiThread(() =>
			{
				Status.Insert(0, e.Activity);
				while (Status.Count > 50)
					Status.RemoveAt(Status.Count - 1);
			});
		}

		private void BeanReceived(object sender, DataReceivedEventArgs e)
		{
			OnUiThread(() =>
			{
				SerialDataReceivedEventArgs serial = (e as SerialDataReceivedEventArgs);
				if (serial != null)
					Status.Insert(0, "RCV: " + Encoding.UTF8.GetString(serial.Data, 0, serial.Data.Length));
				TemperatureDataReceivedEventArgs temp = (e as TemperatureDataReceivedEventArgs);
				if (temp != null)
					this.currentDevice.Temperature = temp.Temperature + " °C";
				AccelerometerDataReceivedEventArgs accel = (e as AccelerometerDataReceivedEventArgs);
				if (accel != null)
				{
					this.currentDevice.X = accel.X.ToString("N5");
					this.currentDevice.Y = accel.Y.ToString("N5");
					this.currentDevice.Z = accel.Z.ToString("N5");
				}
				while (Status.Count > 50)
					Status.RemoveAt(Status.Count - 1);
			});
		}

		/// <summary>
		/// Scan for devices and update the list
		/// </summary>
		/// <param name="operation"></param>
		private async void ScanDevices(IAsyncAction operation)
		{
			List<DeviceInformation> devices = await this.bean.FindDevices();
			OnUiThread(() =>
			{
				foreach (DeviceInformation di in devices)
				{
					Devices.Add(new Device(di.Id, di.Name));
				}
			});
		}


	    public void DeviceSelected(Device device)
	    {
		    CurrentDevice = device;
		    this.bean.Subscribe(device.Id);
			OnPropertyChanged("CurrentDevice");
		}

	    public void Send()
	    {
		    this.bean.Send(BeanMsgId.SerialData, Encoding.UTF8.GetBytes(currentDevice.SerialMessage));
	    }

		public void RequestTemperature()
	    {
		    this.bean.RequestTemperature();
	    }

		public void RequestAccelerometer()
	    {
			this.bean.RequestAccelerometer();
	    }
    }
}
