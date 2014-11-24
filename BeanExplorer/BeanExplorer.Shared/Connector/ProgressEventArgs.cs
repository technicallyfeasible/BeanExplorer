using System;

namespace BeanExplorer.Connector
{
	public class ProgressEventArgs : EventArgs
	{
		/// <summary>
		/// The activity currently under way
		/// </summary>
		public String Activity { get; set; }
		public Int32 Progress { get; set; }
		public Int32 Length { get; set; }

		public ProgressEventArgs(String activity, Int32 progress, Int32 length)
		{
			Activity = activity;
			Progress = progress;
			Length = length;
		}
	}

	public delegate void ProgressEventHandler(Object sender, ProgressEventArgs e);
}