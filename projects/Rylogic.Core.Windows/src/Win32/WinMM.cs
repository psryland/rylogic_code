using System;
using System.Runtime.InteropServices;

namespace Rylogic.Interop.Win32
{
	public class WinMM
	{
		// ReSharper disable UnusedMember.Local
		#pragma warning disable 169

		private const int MMSYSERR_BASE          = 0;
		private const int WAVERR_BASE            = 32;
		private const int MIDIERR_BASE           = 64;
		private const int TIMERR_BASE            = 96;
		private const int JOYERR_BASE            = 160;
		private const int MCIERR_BASE            = 256;
		private const int MIXERR_BASE            = 1024;

		// Error return values
		private const int MMSYSERR_NOERROR      = 0;                    // no error
		private const int MMSYSERR_ERROR        = (MMSYSERR_BASE + 1);  // unspecified error
		private const int MMSYSERR_BADDEVICEID  = (MMSYSERR_BASE + 2);  // device ID out of range
		private const int MMSYSERR_NOTENABLED   = (MMSYSERR_BASE + 3);  // driver failed enable
		private const int MMSYSERR_ALLOCATED    = (MMSYSERR_BASE + 4);  // device already allocated
		private const int MMSYSERR_INVALHANDLE  = (MMSYSERR_BASE + 5);  // device handle is invalid
		private const int MMSYSERR_NODRIVER     = (MMSYSERR_BASE + 6);  // no device driver present
		private const int MMSYSERR_NOMEM        = (MMSYSERR_BASE + 7);  // memory allocation error
		private const int MMSYSERR_NOTSUPPORTED = (MMSYSERR_BASE + 8);  // function isn't supported
		private const int MMSYSERR_BADERRNUM    = (MMSYSERR_BASE + 9);  // error value out of range
		private const int MMSYSERR_INVALFLAG    = (MMSYSERR_BASE + 10); // invalid flag passed
		private const int MMSYSERR_INVALPARAM   = (MMSYSERR_BASE + 11); // invalid parameter passed
		private const int MMSYSERR_HANDLEBUSY   = (MMSYSERR_BASE + 12); // handle being used simultaneously on another thread (eg callback)
		private const int MMSYSERR_INVALIDALIAS = (MMSYSERR_BASE + 13); // specified alias not found
		private const int MMSYSERR_BADDB        = (MMSYSERR_BASE + 14); // bad registry database
		private const int MMSYSERR_KEYNOTFOUND  = (MMSYSERR_BASE + 15); // registry key not found
		private const int MMSYSERR_READERROR    = (MMSYSERR_BASE + 16); // registry read error
		private const int MMSYSERR_WRITEERROR   = (MMSYSERR_BASE + 17); // registry write error
		private const int MMSYSERR_DELETEERROR  = (MMSYSERR_BASE + 18); // registry delete error
		private const int MMSYSERR_VALNOTFOUND  = (MMSYSERR_BASE + 19); // registry value not found
		private const int MMSYSERR_NODRIVERCB   = (MMSYSERR_BASE + 20); // driver does not call DriverCallback
		private const int MMSYSERR_MOREDATA     = (MMSYSERR_BASE + 21); // more data to be returned
		private const int MMSYSERR_LASTERROR    = (MMSYSERR_BASE + 21); // last error in range
		
		[Flags] private enum EMixerObjectFlags :uint
		{
			MIXER           = 0x00000000U,      //The uMxId parameter is a mixer device identifier in the range of zero to one less than the number of devices returned by the mixerGetNumDevs function. This flag is optional.
			WAVEOUT         = 0x10000000U,      //The uMxId parameter is the identifier of a waveform-audio output device in the range of zero to one less than the number of devices returned by the waveOutGetNumDevs function.
			WAVEIN          = 0x20000000U,      //The uMxId parameter is the identifier of a waveform-audio input device in the range of zero to one less than the number of devices returned by the waveInGetNumDevs function.
			MIDIOUT         = 0x30000000U,      //The uMxId parameter is the identifier of a MIDI output device. This identifier must be in the range of zero to one less than the number of devices returned by the midiOutGetNumDevs function.
			MIDIIN          = 0x40000000U,      //The uMxId parameter is the identifier of a MIDI input device. This identifier must be in the range of zero to one less than the number of devices returned by the midiInGetNumDevs function.
			AUX             = 0x50000000U,      //The uMxId parameter is an auxiliary device identifier in the range of zero to one less than the number of devices returned by the auxGetNumDevs function.
			HANDLE          = 0x80000000U,
			HMIDIIN         = (HANDLE|MIDIIN),  //The uMxId parameter is the handle of a MIDI input device. This handle must have been returned by the midiInOpen function.
			HMIDIOUT        = (HANDLE|MIDIOUT), //The uMxId parameter is the handle of a MIDI output device. This handle must have been returned by the midiOutOpen function.
			HMIXER          = (HANDLE|MIXER),   //The uMxId parameter is a mixer device handle returned by the mixerOpen function. This flag is optional.
			HWAVEIN         = (HANDLE|WAVEIN),  //The uMxId parameter is a waveform-audio input handle returned by the waveInOpen function.
			HWAVEOUT        = (HANDLE|WAVEOUT), //The uMxId parameter is a waveform-audio output handle returned by the waveOutOpen function.
			CALLBACK_WINDOW = 0x00010000U,      //The dwCallback parameter is assumed to be a window handle (HWND).
		}

		[Flags] public enum EMixerCtrlType :uint
		{
			CLASS_FADER     = 0x50000000,
			UNITS_UNSIGNED  = 0x30000,
			FADER           = (CLASS_FADER | UNITS_UNSIGNED),
			VOLUME          = (FADER + 1),
		}

		[Flags] public enum EMixerLineComponentType :uint
		{
			DST_FIRST       = 0x0,
			SRC_FIRST       = 0x1000,
			DST_SPEAKERS    = (DST_FIRST + 4),
			SRC_MICROPHONE  = (SRC_FIRST + 3),
			SRC_LINE        = (SRC_FIRST + 2),
		}

		[Flags] public enum EMixerGetLineInfoFlag :uint
		{
			DESTINATION     = 0x00000000U,
			SOURCE          = 0x00000001U,
			LINEID          = 0x00000002U,
			COMPONENTTYPE   = 0x00000003U,
			TARGETTYPE      = 0x00000004U,
			QUERYMASK       = 0x0000000FU,
		}

		[Flags] public enum EMixerGetLineControlsFlag :uint
		{
			ALL             = 0x00000000U,
			ONEBYID         = 0x00000001U,
			ONEBYTYPE       = 0x00000002U,
			QUERYMASK       = 0x0000000FU,
		}

		[Flags] public enum EMixerGetControlDetailsFlag :uint
		{
			VALUE           = 0x00000000U,
			LISTTEXT        = 0x00000001U,
			QUERYMASK       = 0x0000000FU,
		}

		[Flags] public enum EMixerSetControlDetailsFlag :uint
		{
			VALUE           = 0x00000000U,
			CUSTOM          = 0x00000001U,
			QUERYMASK       = 0x0000000FU,
		}

		/// <summary>Constants</summary>
		private const int MAXPNAMELEN     = 32;
		private const int MIXER_LONG_NAME_CHARS  = 64;
		private const int MIXER_SHORT_NAME_CHARS = 16;

		#pragma warning restore 169
		// ReSharper restore UnusedMember.Local

		/// <summary>Struct for holding data for the mixer caps</summary>
		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)] 
		public struct MixerCaps
		{
			public short wMid;
			public short wPid;
			public uint  vDriverVersion;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst=MAXPNAMELEN)] public string szPname;
			public uint  fdwSupport;
			public uint  cDestinations;
		}

		/// <summary>Struct for holding data about the details of the mixer control</summary>
		[StructLayout(LayoutKind.Explicit, CharSet=CharSet.Ansi)] 
		public struct MixerDetails
		{
			[FieldOffset( 0)] public uint cbStruct;
			[FieldOffset( 4)] public uint dwControlID;
			[FieldOffset( 8)] public uint cChannels;
			[FieldOffset(12)] public uint hwndOwner;
			[FieldOffset(12)] public uint cMultipleItems;
			[FieldOffset(16)] public uint cbDetails;
			[FieldOffset(20)] public IntPtr paDetails;

			public static MixerDetails New {get{return new MixerDetails{cbStruct = (uint)Marshal.SizeOf(typeof(MixerDetails))};}}
		}

		/// <summary>Struct to hold data for the mixer line</summary>
		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)]
		public struct MixerLine
		{
			public uint cbStruct;
			public uint dwDestination;
			public uint dwSource;
			public uint dwLineID;
			public uint fdwLine;
			public IntPtr dwUser;
			public uint dwComponentType;
			public uint cChannels;
			public uint cConnections;
			public uint cControls;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = MIXER_SHORT_NAME_CHARS)] public string szShortName;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = MIXER_LONG_NAME_CHARS )] public string szName;
			public struct _0
			{
				public uint  dwType;
				public uint  dwDeviceID;
				public short wMid;
				public short wPid;
				public uint  vDriverVersion;
				[MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAXPNAMELEN)] public string szPname;
			}
			public _0 Target;

			public static MixerLine New {get{return new MixerLine{cbStruct = (uint)Marshal.SizeOf(typeof(MixerLine))};}}
		}

		/// <summary>Struct for holding data for the mixer line controls</summary>
		[StructLayout(LayoutKind.Explicit, CharSet=CharSet.Ansi)]
		public struct MixerLineControls
		{
			[FieldOffset( 0)] public uint cbStruct;
			[FieldOffset( 4)] public uint dwLineID;
			[FieldOffset( 8)] public uint dwControlID;
			[FieldOffset( 8)] public uint dwControlType;
			[FieldOffset(12)] public uint cControls;
			[FieldOffset(16)] public uint cbmxctrl;
			[FieldOffset(20)] public IntPtr pamxctrl;
			
			public static MixerLineControls New {get{return new MixerLineControls{cbStruct = (uint)Marshal.SizeOf(typeof(MixerLineControls))};}}
		}

		/// <summary>Struct to hold data for the mixer control</summary>
		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)]
		public struct MixerControl
		{
			public uint cbStruct;
			public uint dwControlID;
			public uint dwControlType;
			public uint fdwControl;
			public uint cMultipleItems;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = MIXER_SHORT_NAME_CHARS)] public string szShortName;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = MIXER_LONG_NAME_CHARS )] public string szName;
			public uint lMinimum;
			public uint lMaximum;
			[MarshalAs(UnmanagedType.ByValArray, ArraySubType=UnmanagedType.U4, SizeConst=4)]  public int[] Bounds;
			[MarshalAs(UnmanagedType.ByValArray, ArraySubType=UnmanagedType.U4, SizeConst=6)]  public int[] Metrics;

			public static MixerControl New {get{return new MixerControl{cbStruct=(uint)Marshal.SizeOf(typeof(MixerControl))};}}
		}

		/// <summary>Returns number of audio mixer devices present in the system. The device identifier specified by 'uMxId' varies from zero to one less than the number of devices present.</summary>
		[DllImport("winmm.dll", CharSet=CharSet.Ansi)] public static extern int mixerGetNumDevs();

		/// <summary>This function retrieves the device identifier for a mixer device associated with a specified device handle</summary>
		/// <param name="hmxobj">Handle to the audio mixer object to map to a mixer device identifier</param>
		/// <param name="pumxID">Pointer to a variable that receives the mixer device identifier. If no mixer device is available for the hmxobj object, the value –1 is placed in this location and the MMSYSERR_NODRIVER error value is returned.</param>
		/// <param name="fdwId">Flags for mapping the mixer object hmxobj</param>
		[DllImport("winmm.dll", CharSet=CharSet.Ansi)] public static extern int mixerGetID(int hmxobj, out IntPtr pumxID, uint fdwId);

		/// <summary>The mixerGetDevCaps function queries a specified mixer device to determine its capabilities</summary>
		/// <param name="uMxId">Identifier or handle of an open mixer device</param>
		/// <param name="pmxcaps">Pointer to a MIXERCAPS structure that receives information about the capabilities of the device</param>
		/// <param name="cbmxcaps">Size, in bytes, of the MIXERCAPS structure</param>
		[DllImport("winmm.dll", CharSet=CharSet.Ansi)] public static extern int mixerGetDevCapsA(int uMxId, MixerCaps pmxcaps, int cbmxcaps);

		/// <summary>Opens a specified mixer device and ensures that the device will not be removed until the application closes the handle.</summary>
		/// <param name="handle">The returned handle for the audio mixer</param>
		/// <param name="uMxId">Identifier of the mixer device to open. Use a valid device identifier or any HMIXEROBJ (see the mixerGetID function for a description of mixer object handles). A "mapper" for audio mixer devices does not currently exist, so a mixer device identifier of -1 is not valid.</param>
		/// <param name="dwCallback">Handle to a window called when the state of an audio line and/or control associated with the device being opened is changed. Specify NULL for this parameter if no callback mechanism is to be used.</param>
		/// <param name="dwInstance">Reserved, must be 0</param>
		/// <param name="fdwOpen">Flags for opening the device (use EMixerObjectFlags)</param>
		[DllImport("winmm.dll", CharSet=CharSet.Ansi)] public static extern int mixerOpen(out IntPtr handle, int uMxId, IntPtr dwCallback, int dwInstance, uint fdwOpen);
		[DllImport("winmm.dll", CharSet=CharSet.Ansi)] public static extern int mixerClose(IntPtr handle);

		/// <summary>The mixerGetLineInfo function retrieves information about a specific line of a mixer device.</summary>
		/// <param name="hmxobj">Handle to the mixer device object that controls the specific audio line.</param>
		/// <param name="pmxl">Pointer to a MIXERLINE structure. This structure is filled with information about the audio line for the mixer device. The cbStruct member must always be initialized to be the size, in bytes, of the MIXERLINE structure</param>
		/// <param name="fdwInfo">Flags for retrieving information about an audio line</param>
		[DllImport("winmm.dll", CharSet=CharSet.Ansi)] public static extern int mixerGetLineInfoA(IntPtr hmxobj, ref MixerLine pmxl, uint fdwInfo);

		/// <summary>The mixerGetLineControls function retrieves one or more controls associated with an audio line</summary>
		/// <param name="hmxobj">Handle to the mixer device object that is being queried</param>
		/// <param name="pmxlc">Pointer to a MIXERLINECONTROLS structure. This structure is used to reference one or more MIXERCONTROL structures to be filled with information about the controls associated with an audio line. The cbStruct member of the MIXERLINECONTROLS structure must always be initialized to be the size, in bytes, of the MIXERLINECONTROLS structure</param>
		/// <param name="fdwControls">Flags for retrieving information about one or more controls associated with an audio line</param>
		[DllImport("winmm.dll", CharSet=CharSet.Ansi)] public static extern int mixerGetLineControlsA(IntPtr hmxobj, ref MixerLineControls pmxlc, uint fdwControls);

		/// <summary>This function retrieves details about a single control associated with an audio line</summary>
		/// <param name="hmxobj">Handle to the mixer device object being queried</param>
		/// <param name="pmxcd">Pointer to a MIXERCONTROLDETAILS structure, which is filled with state information about the control</param>
		/// <param name="fdwDetails">Flags for retrieving control details</param>
		[DllImport("winmm.dll", CharSet=CharSet.Ansi)] public static extern int mixerGetControlDetailsA(IntPtr hmxobj, ref MixerDetails pmxcd, uint fdwDetails);
		[DllImport("winmm.dll", CharSet=CharSet.Ansi)] public static extern int mixerSetControlDetails(IntPtr hmxobj, ref MixerDetails pmxcd, uint fdwDetails);
		
		/// <summary>This function sends a custom mixer driver message directly to a mixer driver</summary>
		/// <param name="hmx">Handle to an open instance of a mixer device. This handle is returned by the mixerOpen function.</param>
		/// <param name="uMsg">Custom mixer driver message to send to the mixer driver. This message must be above or equal to the MXDM_USER constant.</param>
		/// <param name="dwParam1">Parameter associated with the message being sent</param>
		/// <param name="dwParam2">Parameter associated with the message being sent</param>
		[DllImport("winmm.dll", CharSet=CharSet.Ansi)] public static extern int mixerMessage(IntPtr hmx, int uMsg, int dwParam1, int dwParam2);

		/// <summary>RAII wrapper for a mixer handle</summary>
		public sealed class Mixer :IDisposable
		{
			public IntPtr Handle {get;set;}

			public Mixer(int mixer_id)
			{
				IntPtr handle;
				Check(mixerOpen(out handle, mixer_id, IntPtr.Zero, 0, (uint)EMixerObjectFlags.MIXER));
				Handle = handle;
			}
			
			public void Dispose()
			{
				if (Handle != IntPtr.Zero) mixerClose(Handle);
				Handle = IntPtr.Zero;
			}
		
			/// <summary>Return the info for a mixer line component</summary>
			public MixerLine LineInfo(EMixerLineComponentType line_component_type)
			{
				MixerLine line = MixerLine.New; line.dwComponentType = (uint)line_component_type;
				Check(mixerGetLineInfoA(Handle, ref line, (uint)EMixerGetLineInfoFlag.COMPONENTTYPE));
				return line;
			}

			/// <summary>Retrieve mixer data</summary>
			public MixerControl LineControl(EMixerLineComponentType line_component_type, EMixerCtrlType ctrl_type)
			{
				uint mixer_data_size = (uint)Marshal.SizeOf(typeof(MixerControl));

				MixerLine         line       = LineInfo(line_component_type);
				MixerLineControls line_ctrls = MixerLineControls.New;
				line_ctrls.pamxctrl          = Marshal.AllocCoTaskMem((int)mixer_data_size);
				line_ctrls.dwLineID          = line.dwLineID;
				line_ctrls.dwControlType     = (uint)ctrl_type;
				line_ctrls.cControls         = 1;
				line_ctrls.cbmxctrl          = mixer_data_size;

				Check(mixerGetLineControlsA(Handle, ref line_ctrls, (uint)EMixerGetLineControlsFlag.ONEBYTYPE));
				return (MixerControl?)Marshal.PtrToStructure(line_ctrls.pamxctrl, typeof(MixerControl)) ?? throw new NullReferenceException($"Null returned for WinMM MixerControl");
			}

			/// <summary>Return the volume level for this mixer</summary>
			public uint Volume
			{
				get
				{
					MixerControl mixer_control   = LineControl(EMixerLineComponentType.DST_SPEAKERS, EMixerCtrlType.VOLUME);
					MixerDetails mixer_details   = MixerDetails.New;
					mixer_details.dwControlID    = mixer_control.dwControlID;
					mixer_details.cbDetails      = (uint)Marshal.SizeOf(typeof(uint));
					mixer_details.paDetails      = Marshal.AllocCoTaskMem((int)mixer_details.cbDetails);
					mixer_details.cChannels      = 1;
					mixer_details.cMultipleItems = 0;
					Check(mixerGetControlDetailsA(Handle, ref mixer_details, (uint)EMixerGetControlDetailsFlag.VALUE));
					return (uint?)(Marshal.PtrToStructure(mixer_details.paDetails, typeof(uint))) ?? throw new NullReferenceException($"Null returned for WinMM Volume");
				}
				set
				{
					MixerControl mixer_control   = LineControl(EMixerLineComponentType.DST_SPEAKERS, EMixerCtrlType.VOLUME);
					MixerDetails mixer_details   = MixerDetails.New;
					mixer_details.dwControlID    = mixer_control.dwControlID;
					mixer_details.cbDetails      = (uint)Marshal.SizeOf(typeof(uint));
					mixer_details.paDetails      = Marshal.AllocCoTaskMem((int)mixer_details.cbDetails);
					mixer_details.cChannels      = 1;
					mixer_details.cMultipleItems = 0;
					Marshal.StructureToPtr(Math.Max(mixer_control.lMinimum, Math.Min(mixer_control.lMaximum, value)), mixer_details.paDetails, false);
					Check(mixerSetControlDetails(Handle, ref mixer_details, (uint)EMixerSetControlDetailsFlag.VALUE));
				}
			}

			/// <summary>Throw an exception for a non-success error code</summary>
			private static void Check(int error_code)
			{
				switch (error_code)
				{
				default:                   throw new Exception("winmm api function returned an error ("+error_code+")");
				case MMSYSERR_ALLOCATED:   throw new Exception("The specified resource is already allocated by the maximum number of clients possible.");
				case MMSYSERR_BADDEVICEID: throw new Exception("The uMxId parameter specifies an invalid device identifier.");
				case MMSYSERR_INVALFLAG:   throw new Exception("One or more flags are invalid.");
				case MMSYSERR_INVALHANDLE: throw new Exception("The uMxId parameter specifies an invalid handle.");
				case MMSYSERR_INVALPARAM:  throw new Exception("One or more parameters are invalid.");
				case MMSYSERR_NODRIVER:    throw new Exception("No mixer device is available for the object specified by uMxId. Note that the location referenced by uMxId will also contain the value  - 1.");
				case MMSYSERR_NOMEM:       throw new Exception("Unable to allocate resources.");
				case MMSYSERR_NOERROR:     return;
				}
			}
		}

		/// <summary>Get/Set the volume of the default system sound device</summary>
		public static uint SystemVolume
		{
			get { using (Mixer mx = new Mixer(0)) return mx.Volume; }
			set { using (Mixer mx = new Mixer(0)) mx.Volume = value; }
		}
	}
}
