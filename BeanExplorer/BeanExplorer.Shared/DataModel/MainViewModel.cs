using System;
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
		public String Id { get; set; }
		public String Name { get; set; }

		public Device(String id, String name)
		{
			Id = id;
			Name = name;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
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

		public Device CurrentDevice { get { return currentDevice; } }

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
				Status.Insert(0, Encoding.UTF8.GetString(e.Data, 0, e.Data.Length));
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
		    this.currentDevice = device;
		    this.bean.Subscribe(device.Id);
			OnPropertyChanged("CurrentDevice");
		}

	    public void Test()
	    {
		    this.bean.Send(BeanMsgId.SerialData, Encoding.UTF8.GetBytes("I can send messages to my Bean!"));
		    //this.bean.Send(BeanMsgId.SerialData, Encoding.UTF8.GetBytes("Listening..."));
	    }
    }
}
