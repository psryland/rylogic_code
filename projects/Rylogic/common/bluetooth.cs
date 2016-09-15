using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using pr.attrib;
using pr.extn;
using pr.maths;
using pr.util;
using pr.win32;

namespace pr.common
{
	public static class Bluetooth
	{
		public const int TimeoutQuantumMS = 1280;
		public const int DefaultTimeoutMS = TimeoutQuantumMS * 2;

		public enum EClassOfDeviceMajor
		{
			[Assoc("img", 0)] Miscellaneous = 0x00,
			[Assoc("img", 1)] Computer      = 0x01,
			[Assoc("img", 2)] Phone         = 0x02,
			[Assoc("img", 3)] LanAccess     = 0x03,
			[Assoc("img", 4)] Audio         = 0x04,
			[Assoc("img", 5)] Peripheral    = 0x05,
			[Assoc("img", 6)] Imaging       = 0x06,
			[Assoc("img", 7)] Wearable      = 0x07,
			[Assoc("img", 0)] Toy           = 0x08,
			[Assoc("img", 0)] Unclassified  = 0x1F,
		}
		public enum EClassOfDeviceMinor
		{
			// Computer
			Computer_Unclassified = 0x00,
			Computer_Desktop      = 0x01,
			Computer_Server       = 0x02,
			Computer_Laptop       = 0x03,
			Computer_Handheld     = 0x04,
			Computer_Palm         = 0x05,
			Computer_Wearable     = 0x06,

			// Phone
			Phone_Unclassified     = 0x00,
			Phone_Cellular         = 0x01,
			Phone_Cordless         = 0x02,
			Phone_Smart            = 0x03,
			Phone_WiredModem       = 0x04,
			Phone_CommonISDNAccess = 0x05,

			// Audio
			Audio_Unclassified             = 0x00,
			Audio_Headset                  = 0x01,
			Audio_HandsFree                = 0x02,
			Audio_HeadsetHandsFree         = 0x03,
			Audio_Microphone               = 0x04,
			Audio_LoudSpeaker              = 0x05,
			Audio_Headphones               = 0x06,
			Audio_PortableAudio            = 0x07,
			Audio_CarAudio                 = 0x08,
			Audio_SetTopBox                = 0x09,
			Audio_HifiAudio                = 0x0a,
			Audio_VCR                      = 0x0b,
			Audio_VideoCamera              = 0x0c,
			Audio_Camcorder                = 0x0d,
			Audio_VideoMonitor             = 0x0e,
			Audio_VideoDisplayLoudSpeaker  = 0x0F,
			Audio_VideoDisplayConferencing = 0x10,
			Audio_GamingToy                = 0x12,

			// Peripheral
			Peripheral_NoCategory      = 0x00,
			Peripheral_Unclassified    = 0x00,
			Peripheral_Joystick        = 0x01,
			Peripheral_Gamepad         = 0x02,
			Peripheral_RemoteControl   = 0x03,
			Peripheral_Sensing         = 0x04,
			Peripheral_DigitizerTablet = 0x05,
			Peripheral_CardReader      = 0x06,
			Peripheral_KeyboardMask    = 0x10,
			Peripheral_PointerMask     = 0x20,

			// Imaging
			Imaging_DisplayMask = 0x04,
			Imaging_CameraMask  = 0x08,
			Imaging_ScannerMask = 0x10,
			Imaging_PrinterMask = 0x20,

			// Wearable
			Wearable_WristWatch = 0x01,
			Wearable_Pager      = 0x02,
			Wearable_Jacket     = 0x03,
			Wearable_Helmet     = 0x04,
			Wearable_Glasses    = 0x05,

			// Toy
			Toy_Robot            = 0x01,
			Toy_Vehicle          = 0x02,
			Toy_DollActionFigure = 0x03,
			Toy_Controller       = 0x04,
			Toy_Game             = 0x05,

			//// LAN Access - The minor CODs for LAN Access uses different constrants:
			//COD_LAN_ACCESS_BIT_OFFSET = 5
			//COD_LAN_MINOR_MASK = 0x00001C
			//COD_LAN_ACCESS_MASK = 0x0000E0
			//COD_LAN_MINOR_UNCLASSIFIED = 0x00
			//COD_LAN_ACCESS_0_USED = 0x00
			//COD_LAN_ACCESS_17_USED = 0x01
			//COD_LAN_ACCESS_33_USED = 0x02
			//COD_LAN_ACCESS_50_USED = 0x03
			//COD_LAN_ACCESS_67_USED = 0x04
			//COD_LAN_ACCESS_83_USED = 0x05
			//COD_LAN_ACCESS_99_USED = 0x06
			//COD_LAN_ACCESS_FULL = 0x07
		}
		[Flags] public enum EOptions
		{
			None                = 0,
			ReturnAuthenticated = 1 << 0,
			ReturnRemembered    = 1 << 1,
			ReturnUnknown       = 1 << 2,
			ReturnConnected     = 1 << 3,
			ReturnAll           = 
				ReturnAuthenticated |
				ReturnRemembered    |
				ReturnUnknown       |
				ReturnConnected     ,
		}

		/// <summary>Bluetooth radio info</summary>
		[DebuggerDisplay("{Name} {Connectible} {Discoverable}")]
		public class Radio
		{
			private BLUETOOTH_RADIO_INFO m_info;

			public Radio(IntPtr handle = default(IntPtr))
			{
				Handle = handle;
				if (handle != IntPtr.Zero)
				{
					m_info = BLUETOOTH_RADIO_INFO.New();
					BluetoothGetRadioInfo(handle, ref m_info);
				}
			}

			/// <summary>System handle</summary>
			public IntPtr Handle { get; private set; }

			/// <summary>The name of the bluetooth radio</summary>
			public string Name
			{
				get { return Handle != IntPtr.Zero ? m_info.szName : "All Radios"; }
			}

			/// <summary>Enable/Disable connect-ability for this radio</summary>
			public bool Connectible
			{
				get { return BluetoothIsConnectable(Handle); }
				set { BluetoothEnableIncomingConnections(Handle, value); }
			}

			/// <summary>Enable/Disable discovery for this radio</summary>
			public bool Discoverable
			{
				get { return BluetoothIsDiscoverable(Handle); }
				set { BluetoothEnableDiscovery(Handle, value); }
			}
		}

		/// <summary>Bluetooth device info</summary>
		[DebuggerDisplay("{Name} {ClassOfDeviceMajor} {StatusString}")]
		public class Device
		{
			private const uint CoDServiceMask      = 0xFFE000;
			private const uint CoDMajorMask        = 0x001F00;
			private const uint CoDMinorMask        = 0x0000FC;
			private const int  CoDMinorBitOffset   = 2;
			private const int  CoDMajorBitOffset   = 8 * 1;
			private const int  CoDServiceBitOffset = 8 * 1 + 5;
			private BLUETOOTH_DEVICE_INFO m_info;

			internal Device(BLUETOOTH_DEVICE_INFO info)
			{
				m_info = info;
			}

			/// <summary>The name of the bluetooth device</summary>
			public string Name
			{
				get { return m_info.szName; }
			}

			/// <summary>Major class of this device</summary>
			public EClassOfDeviceMajor ClassOfDeviceMajor
			{
				get { return (EClassOfDeviceMajor)((CoD & CoDMajorMask) >> CoDMajorBitOffset); }
			}

			/// <summary>Minor class of this device</summary>
			public EClassOfDeviceMinor ClassOfDeviceMinor
			{
				get { return (EClassOfDeviceMinor)((CoD & CoDMinorMask) >> CoDMinorBitOffset); }
			}

			/// <summary>Raw class of device value</summary>
			public uint CoD
			{
				get { return m_info.ulClassofDevice; }
			}

			/// <summary>True if this device is currently connected/in use</summary>
			public bool	IsConnected
			{
				get { return m_info.fConnected != 0; }
			}

			/// <summary>Device authenticated/paired/bonded</summary>
			public bool IsPaired
			{
				get { return m_info.fAuthenticated != 0; }
			}

			/// <summary>True if the device is remembered</summary>
			public bool IsRemembered
			{
				get { return m_info.fRemembered != 0; }
			}

			/// <summary>A string describing the device's current state</summary>
			public string StatusString
			{
				get
				{
					var s = new StringBuilder();
					if (IsConnected ) s.Append(s.Length != 0 ? ", " : string.Empty).Append("Connected" );
					if (IsPaired    ) s.Append(s.Length != 0 ? ", " : string.Empty).Append("Paired"    );
					if (IsRemembered) s.Append(s.Length != 0 ? ", " : string.Empty).Append("Remembered");
					if (s.Length == 0) s.Append("Unknown");
					return s.ToString();
				}
			}

			/// <summary>The time that the device was last detected</summary>
			public DateTimeOffset LastSeen
			{
				get { return DateTimeOffset_.FromSystemTime(m_info.stLastSeen); }
			}

			/// <summary>Last time the device was used for other than RNR, inquiry, or SDP</summary>
			public DateTimeOffset LastUsed
			{
				get { return DateTimeOffset_.FromSystemTime(m_info.stLastUsed); }
			}

			/// <summary>Pair this device to the PC</summary>
			public void Pair()
			{
			}
		}

		/// <summary>Enumerable bluetooth radios on the system</summary>
		public static IEnumerable<Radio> Radios()
		{
			var parms = BLUETOOTH_FIND_RADIO_PARAMS.New();
			var radio = IntPtr.Zero;
			var find = IntPtr.Zero;
			using (Scope.Create(null, () => BluetoothFindRadioClose(find)))
			{
				// Find the first radio
				find = BluetoothFindFirstRadio(ref parms, out radio);
				if (find != IntPtr.Zero)
				{
					yield return new Radio(radio);
				}
				else
				{
					var err = Marshal.GetLastWin32Error();
					switch (err) {
					default: throw new Win32Exception(err, "Failed to enumerate bluetooth radio devices");
					case Win32.RPC_S_SERVER_UNAVAILABLE: yield break; // Bluetooth radio turned off
					case Win32.ERROR_NO_MORE_ITEMS: yield break;
					}
				}

				// Find more radios
				for (;;)
				{
					if (BluetoothFindNextRadio(find, out radio))
					{
						yield return new Radio(radio);
					}
					else
					{
						var err = Marshal.GetLastWin32Error();
						switch(err) {
						default: throw new Win32Exception(err, "Failed to enumerate bluetooth radio devices");
						case Win32.ERROR_NO_MORE_ITEMS: yield break;
						}
					}
				}
			}
		}

		/// <summary>Enumerable bluetooth devices on the system</summary>
		public static IEnumerable<Device> Devices(EOptions opts = EOptions.ReturnAll, int timeout_ms = DefaultTimeoutMS)
		{
			var device_info = BLUETOOTH_DEVICE_INFO.New();
			var find = IntPtr.Zero;

			var search_params = BLUETOOTH_DEVICE_SEARCH_PARAMS.New();
			search_params.cTimeoutMultiplier = (byte)Maths.Clamp((timeout_ms + TimeoutQuantumMS - 1) / TimeoutQuantumMS, 0, 48);
			if (opts.HasFlag(EOptions.ReturnAuthenticated)) search_params.fReturnAuthenticated = true;
			if (opts.HasFlag(EOptions.ReturnRemembered))    search_params.fReturnRemembered = true;
			if (opts.HasFlag(EOptions.ReturnUnknown))       search_params.fReturnUnknown = true;
			if (opts.HasFlag(EOptions.ReturnConnected))     search_params.fReturnConnected = true;

			using (Scope.Create(null,() => BluetoothFindDeviceClose(find)))
			{
				// Find the first device on this radio
				find = BluetoothFindFirstDevice(ref search_params, ref device_info);
				if (find != IntPtr.Zero)
				{
					yield return new Device(device_info);
				}
				else
				{
					var err = Marshal.GetLastWin32Error();
					switch (err) {
					default: throw new Win32Exception(err, "Failed to enumerate devices");
					case Win32.ERROR_INVALID_HANDLE: yield break; // Device disabled (among other reasons)
					case Win32.RPC_S_SERVER_UNAVAILABLE: yield break; // Bluetooth radio turned off
					case Win32.ERROR_NO_MORE_ITEMS: yield break;
					}
				}
			
				// Look for more devices
				for (;;)
				{
					if (BluetoothFindNextDevice(find, ref device_info))
					{
						yield return new Device(device_info);
					}
					else
					{
						var err = Marshal.GetLastWin32Error();
						switch (err) {
						default: throw new Win32Exception(err, "Failed to enumerate devices");
						case Win32.ERROR_NO_MORE_ITEMS: yield break;
						}
					}
				}
			}
		}

		///// <summary>Enable bluetooth on this system</summary>
		//public static bool Enabled
		//{
		//	get
		//	{
		//		int enabled = 0;
		//		var r = IsBluetoothRadioEnabled(ref enabled);
		//		if (r != Win32.ERROR_SUCCESS)
		//			throw new Win32Exception(r);

		//		return enabled != 0;
		//	}
		//	set
		//	{
		//		var r = BluetoothEnableRadio(value);
		//		if (r != Win32.ERROR_SUCCESS)
		//			throw new Win32Exception(r);
		//	}
		//}

		/// <summary>Enable incoming connections on all BT radios. Returns true if any radio changes state</summary>
		public static bool Connectable
		{
			get { return BluetoothIsConnectable(IntPtr.Zero); }
			set
			{
				var r = BluetoothEnableIncomingConnections(IntPtr.Zero, value);
				if (r) System.Diagnostics.Debug.Write("Bluetooth radios {0} incoming connections".Fmt(value ? "listening for" : "ignoring"));
				else   System.Diagnostics.Debug.Write("Bluetooth connectivity unchanged");
			}
		}

		/// <summary>True if this PC is discoverable</summary>
		public static bool Discoverable
		{
			get { return BluetoothIsDiscoverable(IntPtr.Zero); }
			set
			{
				var r = BluetoothEnableDiscovery(IntPtr.Zero, value);
				if (r) System.Diagnostics.Debug.Write("Bluetooth discovery {0}".Fmt(value ? "enabled" : "disabled"));
				else   System.Diagnostics.Debug.Write("Bluetooth discovery unchanged");
			}
		}

		#region Interop
		private const string Dll = "Bthprops.cpl";//"irprops.cpl";
		private const int BLUETOOTH_MAX_NAME_SIZE = 248;

		[StructLayout(LayoutKind.Sequential)]
		private struct BLUETOOTH_FIND_RADIO_PARAMS
		{
			public int dwSize; // IN  sizeof this structure

			public static BLUETOOTH_FIND_RADIO_PARAMS New()
			{
				return new BLUETOOTH_FIND_RADIO_PARAMS{dwSize = Marshal.SizeOf(typeof(BLUETOOTH_FIND_RADIO_PARAMS))};
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct BLUETOOTH_DEVICE_SEARCH_PARAMS
		{
			public uint dwSize;              //  IN  sizeof this structure
			public bool fReturnAuthenticated; //  IN  return authenticated devices
			public bool fReturnRemembered;    //  IN  return remembered devices
			public bool fReturnUnknown;       //  IN  return unknown devices
			public bool fReturnConnected;     //  IN  return connected devices
			public bool fIssueInquiry;        //  IN  issue a new inquiry
			public byte cTimeoutMultiplier;  //  IN  timeout for the inquiry
			public IntPtr hRadio;               //  IN  handle to radio to enumerate - NULL == all radios will be searched

			public static BLUETOOTH_DEVICE_SEARCH_PARAMS New()
			{
				return new BLUETOOTH_DEVICE_SEARCH_PARAMS{dwSize = (uint)Marshal.SizeOf(typeof(BLUETOOTH_DEVICE_SEARCH_PARAMS))};
			}
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		internal struct BLUETOOTH_RADIO_INFO
		{
			public uint dwSize;               // Size, in bytes, of this entire data structure
			public uint pad_;                 // for 8 byte alignment of 'Address'
			public UInt64 Address; // Address of the local radio
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = BLUETOOTH_MAX_NAME_SIZE)]
			public string szName;        // Name of the local radio
			public uint ulClassofDevice; // Class of device for the local radio
			public ushort lmpSubversion; // lmpSubversion, manufacturer specifc.
			public ushort manufacturer;  // Manufacturer of the radio, BTH_MFG_Xxx value.  For the most up to date list, goto the Bluetooth specification website and get the Bluetooth assigned numbers document.

			public static BLUETOOTH_RADIO_INFO New()
			{
				return new BLUETOOTH_RADIO_INFO
				{
					dwSize = (uint)Marshal.SizeOf(typeof(BLUETOOTH_RADIO_INFO)),
				};
			}
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		internal struct BLUETOOTH_DEVICE_INFO
		{
			public uint dwSize;                 //  size, in bytes, of this structure - must be the sizeof(BLUETOOTH_DEVICE_INFO)
			public uint pad_;                   // for 8 byte alignment of 'Address'
			public UInt64 Address;              //  Bluetooth address
			public uint ulClassofDevice;        //  Bluetooth "Class of Device"
			public int fConnected;              //  Device connected/in use
			public int fRemembered;             //  Device remembered
			public int fAuthenticated;          //  Device authenticated/paired/bonded
			public Win32.SYSTEMTIME stLastSeen; //  Last time the device was seen
			public Win32.SYSTEMTIME stLastUsed; //  Last time the device was used for other than RNR, inquiry, or SDP
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = BLUETOOTH_MAX_NAME_SIZE)]
			public string szName; //  Name of the device

			public static BLUETOOTH_DEVICE_INFO New()
			{
				return new BLUETOOTH_DEVICE_INFO
				{
					dwSize = (uint)Marshal.SizeOf(typeof(BLUETOOTH_DEVICE_INFO)),
				};
			}
		}

		[StructLayout(LayoutKind.Sequential, Size = 8)]
		internal struct BLUETOOTH_ADDRESS
		{
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
			public byte[] rgBytes; //  easier to format when broken out

			public static BLUETOOTH_ADDRESS New()
			{
				return new BLUETOOTH_ADDRESS{rgBytes = new byte[6]};
			}
		}

		[DllImport(Dll, SetLastError = true, CharSet = CharSet.Auto)]
		private static extern int IsBluetoothRadioEnabled(ref int enabled);

		[DllImport(Dll, SetLastError = true, CharSet = CharSet.Auto)]
		private static extern bool BluetoothIsConnectable(IntPtr hRadio);

		[DllImport(Dll, SetLastError = true, CharSet = CharSet.Auto)]
		private static extern bool BluetoothIsDiscoverable(IntPtr hRadio);

		[DllImport(Dll, SetLastError = true, CharSet = CharSet.Auto)]
		private static extern int BluetoothEnableRadio(bool fEnable);

		[DllImport(Dll, SetLastError = true, CharSet = CharSet.Auto)]
		private static extern bool BluetoothEnableIncomingConnections(IntPtr hRadio, bool fEnable);

		[DllImport(Dll, SetLastError = true, CharSet = CharSet.Auto)]
		private static extern bool BluetoothEnableDiscovery(IntPtr hRadio, bool fEnable);

		[DllImport(Dll, SetLastError = true, CharSet = CharSet.Auto)]
		private static extern uint BluetoothGetRadioInfo(IntPtr hRadio, ref BLUETOOTH_RADIO_INFO pRadioInfo);

		[DllImport(Dll, SetLastError = true, CharSet = CharSet.Auto)]
		private static extern IntPtr BluetoothFindFirstRadio(ref BLUETOOTH_FIND_RADIO_PARAMS pbtfrp, out IntPtr phRadio);

		[DllImport(Dll, SetLastError = true, CharSet = CharSet.Auto)]
		private static extern bool BluetoothFindNextRadio(IntPtr hFind, out IntPtr phRadio);

		[DllImport(Dll, SetLastError = true)]
		private static extern bool BluetoothFindRadioClose(IntPtr hFind);

		[DllImport(Dll, SetLastError = true, CharSet = CharSet.Auto)]
		private static extern IntPtr BluetoothFindFirstDevice(ref BLUETOOTH_DEVICE_SEARCH_PARAMS pbtsp, ref BLUETOOTH_DEVICE_INFO pbtdi);

		[DllImport(Dll, SetLastError = true, CharSet = CharSet.Auto)]
		private static extern bool BluetoothFindNextDevice(IntPtr hFind, ref BLUETOOTH_DEVICE_INFO pbtdi);

		[DllImport(Dll, SetLastError = true)]
		private static extern bool BluetoothFindDeviceClose(IntPtr hFind);

		#endregion
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	using common;

	[TestFixture] public class TestBluetooth
	{
		[Test] public void BlueToothEnumRadios()
		{
			foreach (var radio in Bluetooth.Radios())
				Assert.True(radio != null);
		}
		[Test] public void BlueToothEnumDevices()
		{
			foreach (var device in Bluetooth.Devices())
				Assert.True(device != null);
		}
	}
}
#endif