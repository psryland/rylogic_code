using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace pr.win32
{
	// The following classes and GUIDs are defined by the operating system. Unless otherwise noted, these classes
	// and GUIDs can be used to install devices (or drivers) on Windows 2000 and later versions of Windows:
	public class DevClass
	{
		public DevClass(string name, Guid id, string desc)
		{
			Name = name;
			Id   = id;
			Desc = desc;
		}

		public static readonly DevClass Battery                     = new DevClass("Battery Devices"                                             , new Guid("72631e54-78a4-11d0-bcf7-00aa00b7b32a"), "This class includes battery devices and UPS devices.");
		public static readonly DevClass Biometric                   = new DevClass("Biometric Devices"                                           , new Guid("53D29EF7-377C-4D14-864B-EB3A85769359"), "(Windows Server 2003 and later versions of Windows) This class includes all biometric-based personal identification devices.");
		public static readonly DevClass Bluetooth                   = new DevClass("Bluetooth Devices"                                           , new Guid("e0cbf06c-cd8b-4647-bb8a-263b43f0f974"), "(Windows XP SP1 and later versions of Windows) This class includes all Bluetooth devices.");
		public static readonly DevClass CDROM                       = new DevClass("CD-ROM Drives"                                               , new Guid("4d36e965-e325-11ce-bfc1-08002be10318"), "CD-ROM Drives. This class includes CD-ROM drives, including SCSI CD-ROM drives. By default, the system's CD-ROM class installer also installs a system-supplied CD audio driver and CD-ROM changer driver as Plug and Play filters.");
		public static readonly DevClass DiskDrive                   = new DevClass("Disk Drives"                                                 , new Guid("4d36e967-e325-11ce-bfc1-08002be10318"), "Disk Drives. This class includes hard disk drives. See also the HDC and SCSIAdapter classes.");
		public static readonly DevClass Display                     = new DevClass("Display Adapters"                                            , new Guid("4d36e968-e325-11ce-bfc1-08002be10318"), "This class includes video adapters. Drivers for this class include display drivers and video mini-port drivers.");
		public static readonly DevClass FloppyDiskController        = new DevClass("Floppy Disk Controllers"                                     , new Guid("4d36e969-e325-11ce-bfc1-08002be10318"), "This class includes floppy disk drive controllers.");
		public static readonly DevClass FloppyDiskDrive             = new DevClass("Floppy Disk Drives"                                          , new Guid("4d36e980-e325-11ce-bfc1-08002be10318"), "This class includes floppy disk drives.");
		public static readonly DevClass GPS                         = new DevClass("Global Positioning System/Global Navigation Satellite System", new Guid("6bdd1fc3-810f-11d0-bec7-08002be2092f"), "This class includes GNSS devices that use the Universal Windows driver model introduced in Windows 10.");
		public static readonly DevClass HardDiskController          = new DevClass("Hard Disk Controllers"                                       , new Guid("4d36e96a-e325-11ce-bfc1-08002be10318"), "This class includes hard disk controllers, including ATA/ATAPI controllers but not SCSI and RAID disk controllers.");
		public static readonly DevClass HID                         = new DevClass("Human Interface Devices (HID)"                               , new Guid("745a17a0-74d3-11d0-b6fe-00a0c90f57da"), "This class includes interactive input devices that are operated by the system-supplied HID class driver. This includes USB devices that comply with the USB HID Standard and non-USB devices that use a HID mini-driver. For more information, see HIDClass Device Set up Class. (See also the Keyboard or Mouse classes later in this list.)");
		public static readonly DevClass IEEE_1284_4                 = new DevClass("IEEE 1284.4 Devices"                                         , new Guid("48721b56-6795-11d2-b1a8-0080c72e74a2"), "This class includes devices that control the operation of multifunction IEEE 1284.4 peripheral devices.");
		public static readonly DevClass IEEE_1284_4_Print           = new DevClass("IEEE 1284.4 Print Functions"                                 , new Guid("49ce6ac8-6f86-11d2-b1e5-0080c72e74a2"), "This class includes Dot4 print functions. A Dot4 print function is a function on a Dot4 device and has a single child device, which is a member of the Printer device set up class.");
		public static readonly DevClass IEEE_1394_61883Protocol     = new DevClass("IEEE 1394 Devices That Support the 61883 Protocol"           , new Guid("7ebefbc0-3200-11d2-b4c2-00a0C9697d07"), "This class includes IEEE 1394 devices that support the IEC-61883 protocol device class. The 61883 component includes the 61883.sys protocol driver that transmits various audio and video data streams over the 1394 bus. These currently include standard/high/low quality DV, MPEG2, DSS, and Audio. These data streams are defined by the IEC-61883 specifications.");
		public static readonly DevClass IEEE_1394_AVCProtocol       = new DevClass("IEEE 1394 Devices That Support the AVC Protocol"             , new Guid("c06ff265-ae09-48f0-812c-16753d7cba83"), "This class includes IEEE 1394 devices that support the AVC protocol device class.");
		public static readonly DevClass IEEE_1394_SBP2Protocol      = new DevClass("IEEE 1394 Devices That Support the SBP2 Protocol"            , new Guid("d48179be-ec20-11d1-b6b8-00c04fa372a7"), "This class includes IEEE 1394 devices that support the SBP2 protocol device class.");
		public static readonly DevClass IEEE_1394_HostBusController = new DevClass("IEEE 1394 Host Bus Controller"                               , new Guid("6bdd1fc1-810f-11d0-bec7-08002be2092f"), "This class includes 1394 host controllers connected on a PCI bus, but not 1394 peripherals. Drivers for this class are system-supplied.");
		public static readonly DevClass Imaging                     = new DevClass("Imaging Device"                                              , new Guid("6bdd1fc6-810f-11d0-bec7-08002be2092f"), "This class includes still-image capture devices, digital cameras, and scanners.");
		public static readonly DevClass IrDA                        = new DevClass("Infra-red"                                                   , new Guid("6bdd1fc5-810f-11d0-bec7-08002be2092f"), "This class includes infra-red devices. Drivers for this class include Serial-IR and Fast-IR NDIS mini-ports, but see also the Network Adapter class for other NDIS network adapter mini-ports.");
		public static readonly DevClass Keyboard                    = new DevClass("Keyboard"                                                    , new Guid("4d36e96b-e325-11ce-bfc1-08002be10318"), "This class includes all keyboards. That is, it must also be specified in the (secondary) INF for an enumerated child HID keyboard device.");
		public static readonly DevClass MediumChanger               = new DevClass("Media Changers"                                              , new Guid("ce5939ae-ebde-11d0-b181-0000f8753ec4"), "This class includes SCSI media changer devices.");
		public static readonly DevClass MemoryTechnologyDriver      = new DevClass("Memory Technology Driver"                                    , new Guid("4d36e970-e325-11ce-bfc1-08002be10318"), "This class includes memory devices, such as flash memory cards.");
		public static readonly DevClass Modem                       = new DevClass("Modem"                                                       , new Guid("4d36e96d-e325-11ce-bfc1-08002be10318"), "This class includes modem devices. An INF file for a device of this class specifies the features and configuration of the device and stores this information in the registry. An INF file for a device of this class can also be used to install device drivers for a controller-less modem or a software modem. These devices split the functionality between the modem device and the device driver. For more information about modem INF files and Microsoft Windows Driver Model (WDM) modem devices, see Overview of Modem INF Files and Adding WDM Modem Support.");
		public static readonly DevClass Monitor                     = new DevClass("Monitor"                                                     , new Guid("4d36e96e-e325-11ce-bfc1-08002be10318"), "This class includes display monitors. An INF for a device of this class installs no device driver(s), but instead specifies the features of a particular monitor to be stored in the registry for use by drivers of video adapters. (Monitors are enumerated as the child devices of display adapters.)");
		public static readonly DevClass Mouse                       = new DevClass("Mouse"                                                       , new Guid("4d36e96f-e325-11ce-bfc1-08002be10318"), "This class includes all mouse devices and other kinds of pointing devices, such as trackballs. That is, this class must also be specified in the (secondary) INF for an enumerated child HID mouse device.");
		public static readonly DevClass Multifunction               = new DevClass("Multifunction Devices"                                       , new Guid("4d36e971-e325-11ce-bfc1-08002be10318"), "This class includes combo cards, such as a PCMCIA modem and net-card adapter. The driver for such a Plug and Play multifunction device is installed under this class and enumerates the modem and net-card separately as its child devices.");
		public static readonly DevClass Multimedia                  = new DevClass("Multimedia"                                                  , new Guid("4d36e96c-e325-11ce-bfc1-08002be10318"), "This class includes Audio and DVD multimedia devices, joystick ports, and full-motion video capture devices.");
		public static readonly DevClass MultiportSerialAdapter      = new DevClass("Multi-port Serial Adapters"                                  , new Guid("50906cb8-ba12-11d1-bf5d-0000f805f530"), "This class includes intelligent multi-port serial cards, but not peripheral devices that connect to its ports. It does not include unintelligent (16550-type) multi-port serial controllers or single-port serial controllers (see the Ports class).");
		public static readonly DevClass NetworkAdapter              = new DevClass("Network Adapter"                                             , new Guid("4d36e972-e325-11ce-bfc1-08002be10318"), "This class includes NDIS mini-port drivers excluding Fast-IR mini-port drivers, NDIS intermediate drivers (of virtual adapters), and CoNDIS MCM mini-port drivers.");
		public static readonly DevClass NetworkClient               = new DevClass("Network Client"                                              , new Guid("4d36e973-e325-11ce-bfc1-08002be10318"), "This class includes network and/or print providers. Note: NetClient components are deprecated in Windows 8.1, Windows Server 2012 R2, and later.");
		public static readonly DevClass NetworkService              = new DevClass("Network Service"                                             , new Guid("4d36e974-e325-11ce-bfc1-08002be10318"), "This class includes network services, such as redirectors and servers.");
		public static readonly DevClass NetworkTransport            = new DevClass("Network Transport"                                           , new Guid("4d36e975-e325-11ce-bfc1-08002be10318"), "This class includes NDIS protocols CoNDIS stand-alone call managers, and CoNDIS clients, in addition to higher level drivers in transport stacks.");
		public static readonly DevClass PCI_SLL_Accelerator         = new DevClass("PCI SSL Accelerator"                                         , new Guid("268c95a1-edfe-11d3-95c3-0010dc4050a5"), "This class includes devices that accelerate secure socket layer (SSL) cryptographic processing.");
		public static readonly DevClass PCMCIAAdapter               = new DevClass("PCMCIA Adapter"                                              , new Guid("4d36e977-e325-11ce-bfc1-08002be10318"), "This class includes PCMCIA and CardBus host controllers, but not PCMCIA or CardBus peripherals. Drivers for this class are system-supplied.");
		public static readonly DevClass Ports                       = new DevClass("Ports (COM & LPT)."                                          , new Guid("4d36e978-e325-11ce-bfc1-08002be10318"), "This class includes serial and parallel port devices. See also the MultiportSerial class.");
		public static readonly DevClass Printer                     = new DevClass("Printers"                                                    , new Guid("4d36e979-e325-11ce-bfc1-08002be10318"), "This class includes printers.");
		public static readonly DevClass PNPPrinters                 = new DevClass("Printers, Bus-specific class drivers"                        , new Guid("4658ee7e-f050-11d1-b6bd-00c04fa372a7"), "This class includes SCSI/1394-enumerated printers. Drivers for this class provide printer communication for a specific bus.");
		public static readonly DevClass Processor                   = new DevClass("Processors"                                                  , new Guid("50127dc3-0f36-415e-a6cc-4cb3be910b65"), "This class includes processor types.");
		public static readonly DevClass SCSIAdapter                 = new DevClass("SCSI and RAID Controllers"                                   , new Guid("4d36e97b-e325-11ce-bfc1-08002be10318"), "This class includes SCSI HBAs (Host Bus Adapters) and disk-array controllers.");
		public static readonly DevClass Sensor                      = new DevClass("Sensors"                                                     , new Guid("5175d334-c371-4806-b3ba-71fd53c9258d"), "(Windows 7 and later versions of Windows) This class includes sensor and location devices, such as GPS devices.");
		public static readonly DevClass SmartCardReader             = new DevClass("Smart Card Readers"                                          , new Guid("50dd5230-ba8a-11d1-bf5d-0000f805f530"), "This class includes smart card readers.");
		public static readonly DevClass StorageVolume               = new DevClass("Storage Volumes"                                             , new Guid("71a27cdd-812a-11d0-bec7-08002be2092f"), "This class includes storage volumes as defined by the system-supplied logical volume manager and class drivers that create device objects to represent storage volumes, such as the system disk class driver.");
		public static readonly DevClass SystemDevice                = new DevClass("System Devices"                                              , new Guid("4d36e97d-e325-11ce-bfc1-08002be10318"), "This class includes HALs, system buses, system bridges, the system ACPI driver, and the system volume manager driver.");
		public static readonly DevClass TapeDrive                   = new DevClass("Tape Drives"                                                 , new Guid("6d807884-7d21-11cf-801c-08002be10318"), "This class includes tape drives, including all tape mini-class drivers.");
		public static readonly DevClass USBDevice                   = new DevClass("USB Device"                                                  , new Guid("88BAE032-5A81-49f0-BC3D-A4FF138216D6"), "USBDevice includes all USB devices that do not belong to another class. This class is not used for USB host controllers and hubs.");
		public static readonly DevClass WindowsCE_USBS              = new DevClass("Windows CE USB ActiveSync Devices"                           , new Guid("25dbce51-6c8f-4a72-8a6d-b54c2b4fc835"), "This class includes Windows CE ActiveSync devices. The WCEUSBS setup class supports communication between a personal computer and a device that is compatible with the Windows CE ActiveSync driver (generally, PocketPC devices) over USB.");
		public static readonly DevClass WindowsPortableDevice       = new DevClass("Windows Portable Devices (WPD)"                              , new Guid("eec5ad98-8080-425f-922a-dabf3de3f69a"), "(Windows Vista and later versions of Windows) This class includes WPD devices.");
		public static readonly DevClass WindowsSideShow             = new DevClass("Windows SideShow"                                            , new Guid("997b5d8d-c442-4f2e-baf3-9c8e671e9e21"), "(Windows Vista and later versions of Windows) This class includes all devices that are compatible with Windows SideShow.");

		/// <summary>Device class name</summary>
		public string Name { get; private set; }

		/// <summary>Device class unique identifier</summary>
		public Guid Id { get; private set; }

		/// <summary>Device description</summary>
		public string Desc { get; private set; }
	}
	
	/// <summary>Class for accessing hardware devices</summary>
	public static class DeviceManager
	{
		// Notes:
		// Example use:
		//	public static void EnableMouse(bool enable)
		//	{
		//		// Get this from the properties dialog box of this device in Device Manager
		//		string instancePath = @"ACPI\PNP0F03\4&3688D3F&0";
		//
		//		// Every type of device has a hard-coded GUID, this is the one for mice
		//		DeviceHelper.SetDeviceEnabled(DevClass.Mouse.Id, instancePath, enable);
		//	}

		/// <summary>Enable or disable a device.</summary>
		/// <param name="dev_class_guid">The class guid of the device. Available in the device manager.</param>
		/// <param name="instance_id">The device instance id of the device. Available in the device manager properties dialog.</param>
		/// <param name="enable">True to enable, False to disable.</param>
		/// <remarks>Will throw an exception if the device is not Disable-able.</remarks>
		public static void SetDeviceEnabled(Guid dev_class_guid, string instance_id, bool enable)
		{
			// Get the handle to a device information set for all devices matching classGuid that are present on the system.
			using (var diSetHandle = SetupDiGetClassDevs(ref dev_class_guid, null, IntPtr.Zero, SetupDiGetClassDevsFlags.Present))
			{
				// Get the device information data for each matching device.
				var diData = GetDeviceInfoData(diSetHandle);

				// Find the index of our instance. i.e. the touch-pad mouse - I have 3 mice attached...
				var index = GetIndexOfInstance(diSetHandle, diData, instance_id);

				// Enable/Disable...
				EnableDevice(diSetHandle, diData[index], enable);
			}
		}

		/// <summary>Enumerate the device info for a given handle</summary>
		private static DeviceInfoData[] GetDeviceInfoData(SafeDeviceInfoSetHandle handle)
		{
			var data = new List<DeviceInfoData>();
			var did = new DeviceInfoData{ Size = Marshal.SizeOf(typeof(DeviceInfoData)) };
			while (SetupDiEnumDeviceInfo(handle, data.Count, ref did))
			{
				data.Add(did);
				did = new DeviceInfoData{ Size = Marshal.SizeOf(typeof(DeviceInfoData)) };
			}
			return data.ToArray();
		}

		/// <summary>Find the index of the particular DeviceInfoData for the instanceId.</summary>
		private static int GetIndexOfInstance(SafeDeviceInfoSetHandle handle, DeviceInfoData[] device_info, string instance_id)
		{
			for (int index = 0; index <= device_info.Length - 1; index++)
			{
				var sb = new StringBuilder(1);

				int requiredSize = 0;
				var result = SetupDiGetDeviceInstanceId(handle.DangerousGetHandle(), ref device_info[index], sb, sb.Capacity, out requiredSize);
				if (!result && Marshal.GetLastWin32Error() == Win32.ERROR_INSUFFICIENT_BUFFER)
				{
					sb.Capacity = requiredSize;
					result = SetupDiGetDeviceInstanceId(handle.DangerousGetHandle(), ref device_info[index], sb, sb.Capacity, out requiredSize);
				}

				if (result == false)
				{
					var err = Marshal.GetLastWin32Error();
					throw new Win32Exception(err);
				}

				if (instance_id.Equals(sb.ToString()))
					return index;
			}

			// Not found
			return -1;
		}

		/// <summary>Enable/Disable the given device</summary>
		private static void EnableDevice(SafeDeviceInfoSetHandle handle, DeviceInfoData diData, bool enable)
		{
			// The size is just the size of the header, but we've flattened the structure.
			// The header comprises the first two fields, both integer.
			var parms = new PropertyChangeParameters
			{
				Size        = 8,
				DiFunction  = DiFunction.PropertyChange,
				Scope       = Scopes.Global,
				StateChange = enable ? StateChangeAction.Enable : StateChangeAction.Disable,
			};

			var result = SetupDiSetClassInstallParams(handle, ref diData, ref parms, Marshal.SizeOf(parms));
			if (!result)
			{
				int err = Marshal.GetLastWin32Error();
				throw new Win32Exception(err);
			}

			result = SetupDiCallClassInstaller(DiFunction.PropertyChange, handle, ref diData);
			if (!result)
			{
				var err = Marshal.GetLastWin32Error();
				if (err == (int)SetupApiError.NotDisableable)
					throw new ArgumentException("Device can't be disabled (programmatically or in Device Manager).");
				if (err >= (int)SetupApiError.NoAssociatedClass && err <= (int)SetupApiError.OnlyValidateViaAuthenticode)
					throw new Win32Exception("SetupAPI error: " + ((SetupApiError)err).ToString());
				throw new Win32Exception(err);
			}
		}

		/// <summary>RAII handle</summary>
		private class SafeDeviceInfoSetHandle :SafeHandleZeroOrMinusOneIsInvalid
		{
			public SafeDeviceInfoSetHandle() :base(true) {}
			protected override bool ReleaseHandle() { return SetupDiDestroyDeviceInfoList(this.handle); }
		}

		#region Enumerations
		[Flags] internal enum SetupDiGetClassDevsFlags
		{
			Default         = 1 << 0,
			Present         = 1 << 1,
			AllClasses      = 1 << 2,
			Profile         = 1 << 3,
			DeviceInterface = 1 << 4,
		}
		[Flags] internal enum Scopes
		{
			Global         = 1,
			ConfigSpecific = 2,
			ConfigGeneral  = 4
		}
		internal enum DiFunction
		{
			SelectDevice                  = 1,
			InstallDevice                 = 2,
			AssignResources               = 3,
			Properties                    = 4,
			Remove                        = 5,
			FirstTimeSetup                = 6,
			FoundDevice                   = 7,
			SelectClassDrivers            = 8,
			ValidateClassDrivers          = 9,
			InstallClassDrivers           = (int)0xa,
			CalcDiskSpace                 = (int)0xb,
			DestroyPrivateData            = (int)0xc,
			ValidateDriver                = (int)0xd,
			Detect                        = (int)0xf,
			InstallWizard                 = (int)0x10,
			DestroyWizardData             = (int)0x11,
			PropertyChange                = (int)0x12,
			EnableClass                   = (int)0x13,
			DetectVerify                  = (int)0x14,
			InstallDeviceFiles            = (int)0x15,
			UnRemove                      = (int)0x16,
			SelectBestCompatDrv           = (int)0x17,
			AllowInstall                  = (int)0x18,
			RegisterDevice                = (int)0x19,
			NewDeviceWizardPreSelect      = (int)0x1a,
			NewDeviceWizardSelect         = (int)0x1b,
			NewDeviceWizardPreAnalyze     = (int)0x1c,
			NewDeviceWizardPostAnalyze    = (int)0x1d,
			NewDeviceWizardFinishInstall  = (int)0x1e,
			Unused1                       = (int)0x1f,
			InstallInterfaces             = (int)0x20,
			DetectCancel                  = (int)0x21,
			RegisterCoInstallers          = (int)0x22,
			AddPropertyPageAdvanced       = (int)0x23,
			AddPropertyPageBasic          = (int)0x24,
			Reserved1                     = (int)0x25,
			Troubleshooter                = (int)0x26,
			PowerMessageWake              = (int)0x27,
			AddRemotePropertyPageAdvanced = (int)0x28,
			UpdateDriverUI                = (int)0x29,
			Reserved2                     = (int)0x30
		}
		internal enum StateChangeAction
		{
			Enable     = 1,
			Disable    = 2,
			PropChange = 3,
			Start      = 4,
			Stop       = 5
		}
		internal enum SetupApiError
		{
			NoAssociatedClass               = unchecked((int)0xe0000200),
			ClassMismatch                   = unchecked((int)0xe0000201),
			DuplicateFound                  = unchecked((int)0xe0000202),
			NoDriverSelected                = unchecked((int)0xe0000203),
			KeyDoesNotExist                 = unchecked((int)0xe0000204),
			InvalidDevinstName              = unchecked((int)0xe0000205),
			InvalidClass                    = unchecked((int)0xe0000206),
			DevinstAlreadyExists            = unchecked((int)0xe0000207),
			DevinfoNotRegistered            = unchecked((int)0xe0000208),
			InvalidRegProperty              = unchecked((int)0xe0000209),
			NoInf                           = unchecked((int)0xe000020a),
			NoSuchHDevinst                  = unchecked((int)0xe000020b),
			CantLoadClassIcon               = unchecked((int)0xe000020c),
			InvalidClassInstaller           = unchecked((int)0xe000020d),
			DiDoDefault                     = unchecked((int)0xe000020e),
			DiNoFileCopy                    = unchecked((int)0xe000020f),
			InvalidHwProfile                = unchecked((int)0xe0000210),
			NoDeviceSelected                = unchecked((int)0xe0000211),
			DevinfolistLocked               = unchecked((int)0xe0000212),
			DevinfodataLocked               = unchecked((int)0xe0000213),
			DiBadPath                       = unchecked((int)0xe0000214),
			NoClassInstallParams            = unchecked((int)0xe0000215),
			FileQueueLocked                 = unchecked((int)0xe0000216),
			BadServiceInstallSect           = unchecked((int)0xe0000217),
			NoClassDriverList               = unchecked((int)0xe0000218),
			NoAssociatedService             = unchecked((int)0xe0000219),
			NoDefaultDeviceInterface        = unchecked((int)0xe000021a),
			DeviceInterfaceActive           = unchecked((int)0xe000021b),
			DeviceInterfaceRemoved          = unchecked((int)0xe000021c),
			BadInterfaceInstallSect         = unchecked((int)0xe000021d),
			NoSuchInterfaceClass            = unchecked((int)0xe000021e),
			InvalidReferenceString          = unchecked((int)0xe000021f),
			InvalidMachineName              = unchecked((int)0xe0000220),
			RemoteCommFailure               = unchecked((int)0xe0000221),
			MachineUnavailable              = unchecked((int)0xe0000222),
			NoConfigMgrServices             = unchecked((int)0xe0000223),
			InvalidPropPageProvider         = unchecked((int)0xe0000224),
			NoSuchDeviceInterface           = unchecked((int)0xe0000225),
			DiPostProcessingRequired        = unchecked((int)0xe0000226),
			InvalidCOInstaller              = unchecked((int)0xe0000227),
			NoCompatDrivers                 = unchecked((int)0xe0000228),
			NoDeviceIcon                    = unchecked((int)0xe0000229),
			InvalidInfLogConfig             = unchecked((int)0xe000022a),
			DiDontInstall                   = unchecked((int)0xe000022b),
			InvalidFilterDriver             = unchecked((int)0xe000022c),
			NonWindowsNTDriver              = unchecked((int)0xe000022d),
			NonWindowsDriver                = unchecked((int)0xe000022e),
			NoCatalogForOemInf              = unchecked((int)0xe000022f),
			DevInstallQueueNonNative        = unchecked((int)0xe0000230),
			NotDisableable                  = unchecked((int)0xe0000231),
			CantRemoveDevinst               = unchecked((int)0xe0000232),
			InvalidTarget                   = unchecked((int)0xe0000233),
			DriverNonNative                 = unchecked((int)0xe0000234),
			InWow64                         = unchecked((int)0xe0000235),
			SetSystemRestorePoint           = unchecked((int)0xe0000236),
			IncorrectlyCopiedInf            = unchecked((int)0xe0000237),
			SceDisabled                     = unchecked((int)0xe0000238),
			UnknownException                = unchecked((int)0xe0000239),
			PnpRegistryError                = unchecked((int)0xe000023a),
			RemoteRequestUnsupported        = unchecked((int)0xe000023b),
			NotAnInstalledOemInf            = unchecked((int)0xe000023c),
			InfInUseByDevices               = unchecked((int)0xe000023d),
			DiFunctionObsolete              = unchecked((int)0xe000023e),
			NoAuthenticodeCatalog           = unchecked((int)0xe000023f),
			AuthenticodeDisallowed          = unchecked((int)0xe0000240),
			AuthenticodeTrustedPublisher    = unchecked((int)0xe0000241),
			AuthenticodeTrustNotEstablished = unchecked((int)0xe0000242),
			AuthenticodePublisherNotTrusted = unchecked((int)0xe0000243),
			SignatureOSAttributeMismatch    = unchecked((int)0xe0000244),
			OnlyValidateViaAuthenticode     = unchecked((int)0xe0000245)
		}
		#endregion

		#region Interop
		[StructLayout(LayoutKind.Sequential)]
		internal struct DeviceInfoData
		{
			public int Size;
			public Guid ClassGuid;
			public int DevInst;
			public IntPtr Reserved;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct PropertyChangeParameters
		{
			public int Size;
			// part of header. It's flattened out into 1 structure.
			public DiFunction DiFunction;
			public StateChangeAction StateChange;
			public Scopes Scope;
			public int HwProfile;
		}

		private const string Dll = "setupapi.dll";

		[DllImport(Dll, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
		private static extern bool SetupDiCallClassInstaller(DiFunction installFunction, SafeDeviceInfoSetHandle deviceInfoSet, [In] ref DeviceInfoData deviceInfoData);

		[DllImport(Dll, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
		private static extern bool SetupDiEnumDeviceInfo(SafeDeviceInfoSetHandle deviceInfoSet, int memberIndex, ref DeviceInfoData deviceInfoData);

		[DllImport(Dll, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern SafeDeviceInfoSetHandle SetupDiGetClassDevs([In()] ref Guid classGuid, [MarshalAs(UnmanagedType.LPWStr)] string enumerator, IntPtr hwndParent, SetupDiGetClassDevsFlags flags);

		[DllImport(Dll, SetLastError = true, CharSet = CharSet.Auto)]
		private static extern bool SetupDiGetDeviceInstanceId(IntPtr DeviceInfoSet, ref DeviceInfoData did, [MarshalAs(UnmanagedType.LPTStr)] StringBuilder DeviceInstanceId, int DeviceInstanceIdSize, out int RequiredSize);

		[SuppressUnmanagedCodeSecurity()]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[DllImport(Dll, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
		private static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

		[DllImport(Dll, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
		private static extern bool SetupDiSetClassInstallParams(SafeDeviceInfoSetHandle deviceInfoSet, [In] ref DeviceInfoData deviceInfoData, [In] ref PropertyChangeParameters classInstallParams, int classInstallParamsSize);
		#endregion
	}
}
