using System;
using System.Runtime.Serialization;
using pr.extn;

namespace RyLogViewer
{
	[DataContract]
	public class AndroidLogcat :ICloneable
	{
		public enum ELogBuffer { Main, System, Radio, Events }
		public enum ELogFormat { Brief, Process, Tag, Thread, Raw, Time, ThreadTime, Long }
		public enum EFilterPriority { Verbose, Debug, Info, Warn, Error, Fatal, Silent}
		public enum EConnectionType { Usb, Tcpip }
		[DataContract] public class FilterSpec
		{
			// no private setters, they are used to make the grid cells editable
			[DataMember] public string Tag { get; set; }
			[DataMember] public EFilterPriority Priority { get; set; }
			public FilterSpec() { Tag = "*"; Priority = EFilterPriority.Verbose; }
			public FilterSpec(string tag, EFilterPriority priority) { Tag = tag; Priority = priority; }
		}

		[DataMember] public string          AdbFullPath           = string.Empty;
		[DataMember] public string          SelectedDevice        = string.Empty;
		[DataMember] public bool            CaptureOutputToFile   = false;
		[DataMember] public bool            AppendOutputFile      = true;
		[DataMember] public ELogBuffer[]    LogBuffers            = new []{ELogBuffer.Main};
		[DataMember] public FilterSpec[]    FilterSpecs           = new []{new FilterSpec("*", EFilterPriority.Verbose)};
		[DataMember] public ELogFormat      LogFormat             = ELogFormat.Time;
		[DataMember] public EConnectionType ConnectionType        = EConnectionType.Usb;
		[DataMember] public string[]        IPAddressHistory      = new string[0];
		[DataMember] public int             ConnectionPort        = 5555;

		public AndroidLogcat() {}
		public AndroidLogcat(AndroidLogcat rhs)
		{
			AdbFullPath           = rhs.AdbFullPath;
			SelectedDevice        = rhs.SelectedDevice;
			CaptureOutputToFile   = rhs.CaptureOutputToFile;
			AppendOutputFile      = rhs.AppendOutputFile;
			LogBuffers            = rhs.LogBuffers.Dup();
			FilterSpecs           = rhs.FilterSpecs.Dup();
			LogFormat             = rhs.LogFormat;
			ConnectionType        = rhs.ConnectionType;
			IPAddressHistory      = rhs.IPAddressHistory.Dup();
			ConnectionPort        = rhs.ConnectionPort;
		}
		public override string ToString()
		{
			return AdbFullPath;
		}
		public object Clone()
		{
			return new AndroidLogcat(this);
		}
	}
}
