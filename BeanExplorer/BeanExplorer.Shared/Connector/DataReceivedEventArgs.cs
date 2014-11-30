using System;

namespace BeanExplorer.Connector
{
	public class DataReceivedEventArgs : EventArgs
	{
	}

	public class InvalidDataReceivedEventArgs : DataReceivedEventArgs
	{
		public Byte[] Data { get; set; }

		public InvalidDataReceivedEventArgs(Byte[] data)
		{
			Data = data;
		}
	}

	public class SerialDataReceivedEventArgs : DataReceivedEventArgs
	{
		public Byte[] Data { get; set; }

		public SerialDataReceivedEventArgs(Byte[] data)
		{
			Data = data;
		}
	}

	public class TemperatureDataReceivedEventArgs : DataReceivedEventArgs
	{
		public Int32 Temperature { get; set; }

		public TemperatureDataReceivedEventArgs(Int32 temperature)
		{
			Temperature = temperature;
		}
	}

	public class AccelerometerDataReceivedEventArgs : DataReceivedEventArgs
	{
		public Double X { get; set; }
		public Double Y { get; set; }
		public Double Z { get; set; }

		public AccelerometerDataReceivedEventArgs(Double x, Double y, Double z)
		{
			X = x;
			Y = y;
			Z = z;
		}
	}

	public delegate void DataReceivedEventHandler(Object sender, DataReceivedEventArgs e);
}