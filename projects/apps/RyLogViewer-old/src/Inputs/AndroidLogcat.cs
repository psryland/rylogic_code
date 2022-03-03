using System;
using System.Xml.Linq;
using Rylogic.Extn;

namespace RyLogViewer
{
	public class AndroidLogcat :ICloneable
	{
		public enum ELogBuffer { Main, System, Radio, Events }
		public enum ELogFormat { Brief, Process, Tag, Thread, Raw, Time, ThreadTime, Long }
		public enum EFilterPriority { Verbose, Debug, Info, Warn, Error, Fatal, Silent}
		public enum EConnectionType { Usb, Tcpip }
		public class FilterSpec
		{
			// no private setters, they are used to make the grid cells editable
			public string Tag { get; set; }
			public EFilterPriority Priority { get; set; }
			public FilterSpec() { Tag = "*"; Priority = EFilterPriority.Verbose; }
			public FilterSpec(XElement node)
			{
				Tag      = node.Element(XmlTag.Tag).As<string>();
				Priority = node.Element(XmlTag.Priority).As<EFilterPriority>();
			}
			public FilterSpec(string tag, EFilterPriority priority) { Tag = tag; Priority = priority; }
			public XElement ToXml(XElement node)
			{
				node.Add
				(
					Tag     .ToXml(XmlTag.Tag      , false),
					Priority.ToXml(XmlTag.Priority , false)
				);
				return node;
			}
		}

		public string          AdbFullPath           = string.Empty;
		public string          SelectedDevice        = string.Empty;
		public bool            CaptureOutputToFile   = false;
		public bool            AppendOutputFile      = true;
		public ELogBuffer[]    LogBuffers            = new []{ELogBuffer.Main};
		public FilterSpec[]    FilterSpecs           = new []{new FilterSpec("*", EFilterPriority.Verbose)};
		public ELogFormat      LogFormat             = ELogFormat.Time;
		public EConnectionType ConnectionType        = EConnectionType.Usb;
		public string[]        IPAddressHistory      = new string[0];
		public int             ConnectionPort        = 5555;

		public AndroidLogcat() {}
		public AndroidLogcat(XElement node)
		{
			AdbFullPath         = node.Element(XmlTag.ADBPath).As<string>();
			SelectedDevice      = node.Element(XmlTag.Device).As<string>();
			CaptureOutputToFile = node.Element(XmlTag.CaptureOutput).As<bool>();
			AppendOutputFile    = node.Element(XmlTag.AppendOutput).As<bool>();
			LogBuffers          = node.Element(XmlTag.LogBuffers).As<ELogBuffer[]>();
			FilterSpecs         = node.Element(XmlTag.FilterSpecs).As<FilterSpec[]>();
			LogFormat           = node.Element(XmlTag.LogFormat).As<ELogFormat>();
			ConnectionType      = node.Element(XmlTag.ConnType).As<EConnectionType>();
			IPAddressHistory    = node.Element(XmlTag.IPAddrHistory).As<string[]>();
			ConnectionPort      = node.Element(XmlTag.Port).As<int>();
		}
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
		public virtual XElement ToXml(XElement node)
		{
			node.Add
			(
				AdbFullPath         .ToXml(XmlTag.ADBPath       , false),
				SelectedDevice      .ToXml(XmlTag.Device        , false),
				CaptureOutputToFile .ToXml(XmlTag.CaptureOutput , false),
				AppendOutputFile    .ToXml(XmlTag.AppendOutput  , false),
				LogBuffers          .ToXml(XmlTag.LogBuffers    , false),
				FilterSpecs         .ToXml(XmlTag.FilterSpecs   , false),
				LogFormat           .ToXml(XmlTag.LogFormat     , false),
				ConnectionType      .ToXml(XmlTag.ConnType      , false),
				IPAddressHistory    .ToXml(XmlTag.IPAddrHistory , false),
				ConnectionPort      .ToXml(XmlTag.Port          , false)
			);
			return node;
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
