using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using pr.util;
using pr.win32;

namespace pr.common
{
	public static class Bluetooth
	{
		[Flags] public enum EOptions
		{
			None                = 0,
			ReturnAuthenticated = 1 << 0,
			ReturnRemembered    = 1 << 1,
			ReturnUnknown       = 1 << 2,
			ReturnConnected     = 1 << 3,
			ReturnAll           = ~None,
		}

		/// <summary>Bluetooth radio info</summary>
		public class Radio
		{
			internal Radio(BLUETOOTH_RADIO_INFO info)
			{
				Name = info.szName;
			}

			/// <summary>The name of the bluetooth device</summary>
			public string Name { get; private set; }
		}

		/// <summary>Bluetooth device info</summary>
		public class Device
		{
			internal Device(BLUETOOTH_DEVICE_INFO info)
			{
				Name = info.szName;
			}

			/// <summary>The name of the bluetooth device</summary>
			public string Name { get; private set; }
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
					var info = BLUETOOTH_RADIO_INFO.New();
					BluetoothGetRadioInfo(radio, ref info);
					yield return new Radio(info);
				}
				else
				{
					var err = Marshal.GetLastWin32Error();
					switch (err) {
					default: throw new Win32Exception(err, "Failed to enumerate bluetooth radio devices");
					case Win32.ERROR_NO_MORE_ITEMS: yield break;
					}
				}

				// Find more radios
				for (;;)
				{
					if (BluetoothFindNextRadio(find, out radio))
					{
						var info = BLUETOOTH_RADIO_INFO.New();
						BluetoothGetRadioInfo(radio, ref info);
						yield return new Radio(info);
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
		public static IEnumerable<Device> Devices(EOptions opts = EOptions.ReturnAll)
		{
			var device_info = BLUETOOTH_DEVICE_INFO.New();
			var find = IntPtr.Zero;

			var search_params = BLUETOOTH_DEVICE_SEARCH_PARAMS.New();
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

		#region Interop
		private const string Dll = "irprops.cpl";
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
			public uint pad_;                 // for 8 byte alignment of 'Address'
			public UInt64 Address;   //  Bluetooth address
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