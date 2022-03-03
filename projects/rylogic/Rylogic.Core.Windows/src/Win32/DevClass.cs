using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Rylogic.Interop.Win32
{
	/// <summary>Device manager device classes</summary>
	[DebuggerDisplay("{Name,nq} - {Id,nq}")]
	public class DevClass
	{
		// Notes:
		//  - The following classes and GUIDs are defined by the operating system. Unless otherwise noted, these classes
		//    and GUIDs can be used to install devices (or drivers) on Windows 2000 and later versions of Windows:

		public DevClass(string name, Guid id, string desc)
		{
			Name = name;
			Id   = id;
			Desc = desc;
		}

		/// <summary>Device class name</summary>
		public string Name { get; }

		/// <summary>Device class unique identifier</summary>
		public Guid Id { get; }

		/// <summary>Device description</summary>
		public string Desc { get; }

		/// <summary>Enumerable all dev classes</summary>
		public static IEnumerable<DevClass> All
		{
			get
			{
				var ty = typeof(DevClass);
				var fields = ty.GetFields(BindingFlags.Static|BindingFlags.Public);
				foreach (var field in fields.Where(x => x.FieldType == ty))
				{
					if (!(field.GetValue(null) is DevClass dc)) continue;
					yield return dc;
				}
			}
		}

		/// <summary>Return the DevClass associated with the given Guid</summary>
		public static DevClass From(Guid device_class_guid)
		{
			return All.FirstOrDefault(x => x.Id == device_class_guid) ?? new DevClass("Unknown", device_class_guid, "Unknown hardware device class");
		}

		#region Device class GUIDs
		public static readonly DevClass ApplicationLaunchButton     = new DevClass(""                                                            , new Guid("629758ee-986e-4d9e-8e47-de27f8ab054d"), "");
		public static readonly DevClass AVC                         = new DevClass(""                                                            , new Guid("095780c3-48a1-4570-bd95-46707f78c2dc"), "");
		public static readonly DevClass Battery                     = new DevClass("Battery Devices"                                             , new Guid("72631e54-78a4-11d0-bcf7-00aa00b7b32a"), "This class includes battery devices and UPS devices.");
		public static readonly DevClass Biometric                   = new DevClass("Biometric Devices"                                           , new Guid("53D29EF7-377C-4D14-864B-EB3A85769359"), "(Windows Server 2003 and later versions of Windows) This class includes all biometric-based personal identification devices.");
		public static readonly DevClass Bluetooth                   = new DevClass("Bluetooth Devices"                                           , new Guid("e0cbf06c-cd8b-4647-bb8a-263b43f0f974"), "(Windows XP SP1 and later versions of Windows) This class includes all Bluetooth devices.");
		public static readonly DevClass BluetoothPort               = new DevClass(""                                                            , new Guid("0850302a-b344-4fda-9be9-90576b8d46f0"), "");
		public static readonly DevClass Brightness                  = new DevClass(""                                                            , new Guid("fde5bba4-b3f9-46fb-bdaa-0728ce3100b4"), "");
		public static readonly DevClass CDChanger                   = new DevClass(""                                                            , new Guid("53f56312-b6bf-11d0-94f2-00a0c91efb8b"), "");
		public static readonly DevClass CDROM                       = new DevClass("CD-ROM Drives"                                               , new Guid("4d36e965-e325-11ce-bfc1-08002be10318"), "CD-ROM Drives. This class includes CD-ROM drives, including SCSI CD-ROM drives. By default, the system's CD-ROM class installer also installs a system-supplied CD audio driver and CD-ROM changer driver as Plug and Play filters.");
		public static readonly DevClass CDROM2                      = new DevClass("CD-ROM Drives"                                               , new Guid("53f56308-b6bf-11d0-94f2-00a0c91efb8b"), "CD-ROM Drives. This class includes CD-ROM drives, including SCSI CD-ROM drives. By default, the system's CD-ROM class installer also installs a system-supplied CD audio driver and CD-ROM changer driver as Plug and Play filters.");
		public static readonly DevClass Disk                        = new DevClass("Disk Drives"                                                 , new Guid("53f56307-b6bf-11d0-94f2-00a0c91efb8b"), "Disk Drives. This class includes hard disk drives. See also the HDC and SCSIAdapter classes.");
		public static readonly DevClass DiskLegacy                  = new DevClass("Disk Drives"                                                 , new Guid("4d36e967-e325-11ce-bfc1-08002be10318"), "Disk Drives. This class includes hard disk drives. See also the HDC and SCSIAdapter classes.");
		public static readonly DevClass Display                     = new DevClass("Display Adapters"                                            , new Guid("4d36e968-e325-11ce-bfc1-08002be10318"), "This class includes video adapters. Drivers for this class include display drivers and video mini-port drivers.");
		public static readonly DevClass DisplayAdapter              = new DevClass(""                                                            , new Guid("5b45201d-f2f2-4f3b-85bb-30ff1f953599"), "");
		public static readonly DevClass DisplayDevice               = new DevClass(""                                                            , new Guid("1ca05180-a699-450a-9a0c-de4fbe3ddd89"), "");
		public static readonly DevClass Floppy                      = new DevClass("Floppy Disk "                                                , new Guid("53f56311-b6bf-11d0-94f2-00a0c91efb8b"), "");
		public static readonly DevClass FloppyDiskController        = new DevClass("Floppy Disk Controllers"                                     , new Guid("4d36e969-e325-11ce-bfc1-08002be10318"), "This class includes floppy disk drive controllers.");
		public static readonly DevClass FloppyDiskDrive             = new DevClass("Floppy Disk Drives"                                          , new Guid("4d36e980-e325-11ce-bfc1-08002be10318"), "This class includes floppy disk drives.");
		public static readonly DevClass GPS                         = new DevClass("Global Positioning System/Global Navigation Satellite System", new Guid("6bdd1fc3-810f-11d0-bec7-08002be2092f"), "This class includes GNSS devices that use the Universal Windows driver model introduced in Windows 10.");
		public static readonly DevClass HardDiskController          = new DevClass("Hard Disk Controllers"                                       , new Guid("4d36e96a-e325-11ce-bfc1-08002be10318"), "This class includes hard disk controllers, including ATA/ATAPI controllers but not SCSI and RAID disk controllers.");
		public static readonly DevClass HID                         = new DevClass("Human Interface Devices (HID)"                               , new Guid("745a17a0-74d3-11d0-b6fe-00a0c90f57da"), "This class includes interactive input devices that are operated by the system-supplied HID class driver. This includes USB devices that comply with the USB HID Standard and non-USB devices that use a HID mini-driver. For more information, see HIDClass Device Set up Class. (See also the Keyboard or Mouse classes later in this list.)");
		public static readonly DevClass HID2                        = new DevClass(""                                                            , new Guid("4d1e55b2-f16f-11cf-88cb-001111000030"), "");
		public static readonly DevClass HiddenVolume                = new DevClass(""                                                            , new Guid("7f108a28-9833-4b3b-b780-2c6b5fa5c062"), "");
		public static readonly DevClass IEEE_1284_4                 = new DevClass("IEEE 1284.4 Devices"                                         , new Guid("48721b56-6795-11d2-b1a8-0080c72e74a2"), "This class includes devices that control the operation of multifunction IEEE 1284.4 peripheral devices.");
		public static readonly DevClass IEEE_1284_4_Print           = new DevClass("IEEE 1284.4 Print Functions"                                 , new Guid("49ce6ac8-6f86-11d2-b1e5-0080c72e74a2"), "This class includes Dot4 print functions. A Dot4 print function is a function on a Dot4 device and has a single child device, which is a member of the Printer device set up class.");
		public static readonly DevClass IEEE_1394_61883Protocol     = new DevClass("IEEE 1394 Devices That Support the 61883 Protocol"           , new Guid("7ebefbc0-3200-11d2-b4c2-00a0C9697d07"), "This class includes IEEE 1394 devices that support the IEC-61883 protocol device class. The 61883 component includes the 61883.sys protocol driver that transmits various audio and video data streams over the 1394 bus. These currently include standard/high/low quality DV, MPEG2, DSS, and Audio. These data streams are defined by the IEC-61883 specifications.");
		public static readonly DevClass IEEE_1394_AVCProtocol       = new DevClass("IEEE 1394 Devices That Support the AVC Protocol"             , new Guid("c06ff265-ae09-48f0-812c-16753d7cba83"), "This class includes IEEE 1394 devices that support the AVC protocol device class.");
		public static readonly DevClass IEEE_1394_SBP2Protocol      = new DevClass("IEEE 1394 Devices That Support the SBP2 Protocol"            , new Guid("d48179be-ec20-11d1-b6b8-00c04fa372a7"), "This class includes IEEE 1394 devices that support the SBP2 protocol device class.");
		public static readonly DevClass IEEE_1394_HostBusController = new DevClass("IEEE 1394 Host Bus Controller"                               , new Guid("6bdd1fc1-810f-11d0-bec7-08002be2092f"), "This class includes 1394 host controllers connected on a PCI bus, but not 1394 peripherals. Drivers for this class are system-supplied.");
		public static readonly DevClass IEEE_61883                  = new DevClass(""                                                            , new Guid("7ebefbc0-3200-11d2-b4c2-00a0c9697d07"), "");
		public static readonly DevClass I2C                         = new DevClass(""                                                            , new Guid("2564aa4f-dddb-4495-b497-6ad4a84163d7"), "");
		public static readonly DevClass Imaging                     = new DevClass("Imaging Device"                                              , new Guid("6bdd1fc6-810f-11d0-bec7-08002be2092f"), "This class includes still-image capture devices, digital cameras, and scanners.");
		public static readonly DevClass IrDA                        = new DevClass("Infra-red"                                                   , new Guid("6bdd1fc5-810f-11d0-bec7-08002be2092f"), "This class includes infra-red devices. Drivers for this class include Serial-IR and Fast-IR NDIS mini-ports, but see also the Network Adapter class for other NDIS network adapter mini-ports.");
		public static readonly DevClass Keyboard                    = new DevClass("Keyboard"                                                    , new Guid("4d36e96b-e325-11ce-bfc1-08002be10318"), "This class includes all keyboards. That is, it must also be specified in the (secondary) INF for an enumerated child HID keyboard device.");
		public static readonly DevClass Keyboard2                   = new DevClass(""                                                            , new Guid("884b96c3-56ef-11d1-bc8c-00a0c91405dd"), "");
		public static readonly DevClass Lid                         = new DevClass("Lid"                                                         , new Guid("4afa3d52-74a7-11d0-be5e-00a0c9062857"), "");
		public static readonly DevClass MediumChanger               = new DevClass("Media Changers"                                              , new Guid("ce5939ae-ebde-11d0-b181-0000f8753ec4"), "This class includes SCSI media changer devices.");
		public static readonly DevClass MediumChanger2              = new DevClass(""                                                            , new Guid("53f56310-b6bf-11d0-94f2-00a0c91efb8b"), "");
		public static readonly DevClass Memory                      = new DevClass(""                                                            , new Guid("3fd0f03d-92e0-45fb-b75c-5ed8ffb01021"), "");
		public static readonly DevClass MemoryTechnologyDriver      = new DevClass("Memory Technology Driver"                                    , new Guid("4d36e970-e325-11ce-bfc1-08002be10318"), "This class includes memory devices, such as flash memory cards.");
		public static readonly DevClass MessageIndicator            = new DevClass(""                                                            , new Guid("cd48a365-fa94-4ce2-a232-a1b764e5d8b4"), "");
		public static readonly DevClass Modem                       = new DevClass("Modem"                                                       , new Guid("4d36e96d-e325-11ce-bfc1-08002be10318"), "This class includes modem devices. An INF file for a device of this class specifies the features and configuration of the device and stores this information in the registry. An INF file for a device of this class can also be used to install device drivers for a controller-less modem or a software modem. These devices split the functionality between the modem device and the device driver. For more information about modem INF files and Microsoft Windows Driver Model (WDM) modem devices, see Overview of Modem INF Files and Adding WDM Modem Support.");
		public static readonly DevClass Modem2                      = new DevClass(""                                                            , new Guid("2c7089aa-2e0e-11d1-b114-00c04fc2aae4"), "");
		public static readonly DevClass Monitor                     = new DevClass(""                                                            , new Guid("e6f07b5f-ee97-4a90-b076-33f57bf4eaa7"), "");
		public static readonly DevClass Monitor2                    = new DevClass("Monitor"                                                     , new Guid("4d36e96e-e325-11ce-bfc1-08002be10318"), "This class includes display monitors. An INF for a device of this class installs no device driver(s), but instead specifies the features of a particular monitor to be stored in the registry for use by drivers of video adapters. (Monitors are enumerated as the child devices of display adapters.)");
		public static readonly DevClass Mouse                       = new DevClass("Mouse"                                                       , new Guid("4d36e96f-e325-11ce-bfc1-08002be10318"), "This class includes all mouse devices and other kinds of pointing devices, such as trackballs. That is, this class must also be specified in the (secondary) INF for an enumerated child HID mouse device.");
		public static readonly DevClass Mouse2                      = new DevClass(""                                                            , new Guid("378de44c-56ef-11d1-bc8c-00a0c91405dd"), "");
		public static readonly DevClass Multifunction               = new DevClass("Multifunction Devices"                                       , new Guid("4d36e971-e325-11ce-bfc1-08002be10318"), "This class includes combo cards, such as a PCMCIA modem and net-card adapter. The driver for such a Plug and Play multifunction device is installed under this class and enumerates the modem and net-card separately as its child devices.");
		public static readonly DevClass Multimedia                  = new DevClass("Multimedia"                                                  , new Guid("4d36e96c-e325-11ce-bfc1-08002be10318"), "This class includes Audio and DVD multimedia devices, joystick ports, and full-motion video capture devices.");
		public static readonly DevClass MultiportSerialAdapter      = new DevClass("Multi-port Serial Adapters"                                  , new Guid("50906cb8-ba12-11d1-bf5d-0000f805f530"), "This class includes intelligent multi-port serial cards, but not peripheral devices that connect to its ports. It does not include unintelligent (16550-type) multi-port serial controllers or single-port serial controllers (see the Ports class).");
		public static readonly DevClass Net                         = new DevClass(""                                                            , new Guid("cac88484-7515-4c03-82e6-71a87abac361"), "");
		public static readonly DevClass NetworkAdapter              = new DevClass("Network Adapter"                                             , new Guid("4d36e972-e325-11ce-bfc1-08002be10318"), "This class includes NDIS mini-port drivers excluding Fast-IR mini-port drivers, NDIS intermediate drivers (of virtual adapters), and CoNDIS MCM mini-port drivers.");
		public static readonly DevClass NetworkClient               = new DevClass("Network Client"                                              , new Guid("4d36e973-e325-11ce-bfc1-08002be10318"), "This class includes network and/or print providers. Note: NetClient components are deprecated in Windows 8.1, Windows Server 2012 R2, and later.");
		public static readonly DevClass NetworkService              = new DevClass("Network Service"                                             , new Guid("4d36e974-e325-11ce-bfc1-08002be10318"), "This class includes network services, such as redirectors and servers.");
		public static readonly DevClass NetworkTransport            = new DevClass("Network Transport"                                           , new Guid("4d36e975-e325-11ce-bfc1-08002be10318"), "This class includes NDIS protocols CoNDIS stand-alone call managers, and CoNDIS clients, in addition to higher level drivers in transport stacks.");
		public static readonly DevClass NVDimmPassthrough           = new DevClass("NVDIMM PassThrough"                                          , new Guid("4309ac30-0d11-11e4-9191-0800200c9a66"), "");
		public static readonly DevClass OPM                         = new DevClass(""                                                            , new Guid("bf4672de-6b4e-4be4-a325-68a91ea49c09"), "");
		public static readonly DevClass ParallelClass               = new DevClass(""                                                            , new Guid("811fc6a5-f728-11d0-a537-0000f8753ed1"), "");
		public static readonly DevClass ParallelPort                = new DevClass(""                                                            , new Guid("97f76ef0-f883-11d0-af1f-0000f800845c"), "");
		public static readonly DevClass Partition                   = new DevClass(""                                                            , new Guid("53f5630a-b6bf-11d0-94f2-00a0c91efb8b"), "");
		public static readonly DevClass PCI_SLL_Accelerator         = new DevClass("PCI SSL Accelerator"                                         , new Guid("268c95a1-edfe-11d3-95c3-0010dc4050a5"), "This class includes devices that accelerate secure socket layer (SSL) cryptographic processing.");
		public static readonly DevClass PCMCIAAdapter               = new DevClass("PCMCIA Adapter"                                              , new Guid("4d36e977-e325-11ce-bfc1-08002be10318"), "This class includes PCMCIA and CardBus host controllers, but not PCMCIA or CardBus peripherals. Drivers for this class are system-supplied.");
		public static readonly DevClass PNPPrinters                 = new DevClass("Printers (Bus-specific class drivers)"                       , new Guid("4658ee7e-f050-11d1-b6bd-00c04fa372a7"), "This class includes SCSI/1394-enumerated printers. Drivers for this class provide printer communication for a specific bus.");
		public static readonly DevClass Ports                       = new DevClass("Ports (COM & LPT)"                                           , new Guid("4d36e978-e325-11ce-bfc1-08002be10318"), "This class includes serial and parallel port devices. See also the MultiportSerial class.");
		public static readonly DevClass PortsSerEnum                = new DevClass("Ports (COM & LPT)"                                           , new Guid("86e0d1e0-8089-11d0-9ce4-08003e301f73"), "This class includes serial and parallel port devices that support plug and play.");
		public static readonly DevClass Printer                     = new DevClass("Printers"                                                    , new Guid("4d36e979-e325-11ce-bfc1-08002be10318"), "This class includes printers.");
		public static readonly DevClass Processor                   = new DevClass(""                                                            , new Guid("97fadb10-4e33-40ae-359c-8bef029dbdd0"), "");
		public static readonly DevClass Processors                  = new DevClass("Processors"                                                  , new Guid("50127dc3-0f36-415e-a6cc-4cb3be910b65"), "This class includes processor types.");
		public static readonly DevClass ScmPhysicalDevice           = new DevClass(""                                                            , new Guid("4283609d-4dc2-43be-bbb4-4f15dfce2c61"), "");
		public static readonly DevClass SCSIAdapter                 = new DevClass("SCSI and RAID Controllers"                                   , new Guid("4d36e97b-e325-11ce-bfc1-08002be10318"), "This class includes SCSI HBAs (Host Bus Adapters) and disk-array controllers.");
		public static readonly DevClass Sensor                      = new DevClass("Sensors"                                                     , new Guid("5175d334-c371-4806-b3ba-71fd53c9258d"), "(Windows 7 and later versions of Windows) This class includes sensor and location devices, such as GPS devices.");
		public static readonly DevClass Sensor2                     = new DevClass(""                                                            , new Guid("ba1bb692-9b7a-4833-9a1e-525ed134e7e2"), "");
		public static readonly DevClass ServiceVolume               = new DevClass(""                                                            , new Guid("6ead3d82-25ec-46bc-b7fd-c1f0df8f5037"), "");
		public static readonly DevClass SES                         = new DevClass(""                                                            , new Guid("1790c9ec-47d5-4df3-b5af-9adf3cf23e48"), "");
		public static readonly DevClass SideShow                    = new DevClass(""                                                            , new Guid("152e5811-feb9-4b00-90f4-d32947ae1681"), "");
		public static readonly DevClass SmartCardReader             = new DevClass("Smart Card Readers"                                          , new Guid("50dd5230-ba8a-11d1-bf5d-0000f805f530"), "This class includes smart card readers.");
		public static readonly DevClass StoragePort                 = new DevClass(""                                                            , new Guid("2accfe60-c130-11d2-b082-00a0c91efb8b"), "");
		public static readonly DevClass StorageVolume               = new DevClass("Storage Volumes"                                             , new Guid("71a27cdd-812a-11d0-bec7-08002be2092f"), "This class includes storage volumes as defined by the system-supplied logical volume manager and class drivers that create device objects to represent storage volumes, such as the system disk class driver.");
		public static readonly DevClass SystemButton                = new DevClass(""                                                            , new Guid("4afa3d53-74a7-11d0-be5e-00a0c9062857"), "");
		public static readonly DevClass SystemDevice                = new DevClass("System Devices"                                              , new Guid("4d36e97d-e325-11ce-bfc1-08002be10318"), "This class includes HALs, system buses, system bridges, the system ACPI driver, and the system volume manager driver.");
		public static readonly DevClass Tape                        = new DevClass(""                                                            , new Guid("53f5630b-b6bf-11d0-94f2-00a0c91efb8b"), "");
		public static readonly DevClass TapeDrive                   = new DevClass("Tape Drives"                                                 , new Guid("6d807884-7d21-11cf-801c-08002be10318"), "This class includes tape drives, including all tape mini-class drivers.");
		public static readonly DevClass ThermalZone                 = new DevClass(""                                                            , new Guid("4afa3d51-74a7-11d0-be5e-00a0c9062857"), "");
		public static readonly DevClass UnifiedAccessRPMB           = new DevClass(""                                                            , new Guid("27447c21-bcc3-4d07-a05b-a3395bb4eee7"), "");
		public static readonly DevClass USBDevice                   = new DevClass("USB Device"                                                  , new Guid("a5dcbf10-6530-11d2-901f-00c04fb951ed"), "USBDevice includes all USB devices that do not belong to another class. This class is not used for USB host controllers and hubs.");
		public static readonly DevClass USBCategory                 = new DevClass("USB Category"                                                , new Guid("88BAE032-5A81-49f0-BC3D-A4FF138216D6"), "USBDevice includes all USB devices that do not belong to another class. This class is not used for USB host controllers and hubs.");
		public static readonly DevClass USBHostController           = new DevClass(""                                                            , new Guid("3abf6f2d-71c4-462a-8a92-1e6861e6af27"), "");
		public static readonly DevClass USBHub                      = new DevClass(""                                                            , new Guid("f18a0e88-c30c-11d0-8815-00a0c906bed8"), "");
		public static readonly DevClass VideoOutput                 = new DevClass(""                                                            , new Guid("1ad9e4f0-f88d-4360-bab9-4c2d55e564cd"), "");
		public static readonly DevClass VirtualAVC                  = new DevClass(""                                                            , new Guid("616ef4d0-23ce-446d-a568-c31eb01913d0"), "");
		public static readonly DevClass VMLUN                       = new DevClass(""                                                            , new Guid("6f416619-9f29-42a5-b20b-37e219ca02b0"), "");
		public static readonly DevClass Volume                      = new DevClass(""                                                            , new Guid("53f5630d-b6bf-11d0-94f2-00a0c91efb8b"), "");
		public static readonly DevClass WindowsCE_USBS              = new DevClass("Windows CE USB ActiveSync Devices"                           , new Guid("25dbce51-6c8f-4a72-8a6d-b54c2b4fc835"), "This class includes Windows CE ActiveSync devices. The WCEUSBS setup class supports communication between a personal computer and a device that is compatible with the Windows CE ActiveSync driver (generally, PocketPC devices) over USB.");
		public static readonly DevClass WindowsPortableDevice       = new DevClass("Windows Portable Devices (WPD)"                              , new Guid("eec5ad98-8080-425f-922a-dabf3de3f69a"), "(Windows Vista and later versions of Windows) This class includes WPD devices.");
		public static readonly DevClass WindowsSideShow             = new DevClass("Windows SideShow"                                            , new Guid("997b5d8d-c442-4f2e-baf3-9c8e671e9e21"), "(Windows Vista and later versions of Windows) This class includes all devices that are compatible with Windows SideShow.");
		public static readonly DevClass WPD                         = new DevClass(""                                                            , new Guid("6ac27878-a6fa-4155-ba85-f98f491d4f33"), "");
		public static readonly DevClass WPDPrivate                  = new DevClass(""                                                            , new Guid("ba0c718f-4ded-49b7-bdd3-fabe28661211"), "");
		public static readonly DevClass WriteOnceDisk               = new DevClass(""                                                            , new Guid("53f5630c-b6bf-11d0-94f2-00a0c91efb8b"), "");

#if false
	public static readonly DevClass KSCATEGORY_ACOUSTIC_ECHO_CANCEL          = new DevClass("", new Guid("bf963d80-c559-11d0-8a2b-00a0c9255ac1"), "");
	public static readonly DevClass KSCATEGORY_AUDIO                         = new DevClass("", new Guid("6994ad04-93ef-11d0-a3cc-00a0c9223196"), "");
	public static readonly DevClass KSCATEGORY_AUDIO_DEVICE                  = new DevClass("", new Guid("fbf6f530-07b9-11d2-a71e-0000f8004788"), "");
	public static readonly DevClass KSCATEGORY_AUDIO_GFX                     = new DevClass("", new Guid("9baf9572-340c-11d3-abdc-00a0c90ab16f"), "");
	public static readonly DevClass KSCATEGORY_AUDIO_SPLITTER                = new DevClass("", new Guid("9ea331fa-b91b-45f8-9285-bd2bc77afcde"), "");
	public static readonly DevClass KSCATEGORY_BDA_IP_SINK                   = new DevClass("", new Guid("71985f4a-1ca1-11d3-9cc8-00c04f7971e0"), "");
	public static readonly DevClass KSCATEGORY_BDA_NETWORK_EPG               = new DevClass("", new Guid("71985f49-1ca1-11d3-9cc8-00c04f7971e0"), "");
	public static readonly DevClass KSCATEGORY_BDA_NETWORK_PROVIDER          = new DevClass("", new Guid("71985f4b-1ca1-11d3-9cc8-00c04f7971e0"), "");
	public static readonly DevClass KSCATEGORY_BDA_NETWORK_TUNER             = new DevClass("", new Guid("71985f48-1ca1-11d3-9cc8-00c04f7971e0"), "");
	public static readonly DevClass KSCATEGORY_BDA_RECEIVER_COMPONENT        = new DevClass("", new Guid("fd0a5af4-b41d-11d2-9c95-00c04f7971e0"), "");
	public static readonly DevClass KSCATEGORY_BDA_TRANSPORT_INFORMATION     = new DevClass("", new Guid("a2e3074f-6c3d-11d3-b653-00c04f79498e"), "");
	public static readonly DevClass KSCATEGORY_BRIDGE                        = new DevClass("", new Guid("085aff00-62ce-11cf-a5d6-28db04c10000"), "");
	public static readonly DevClass KSCATEGORY_CAPTURE                       = new DevClass("", new Guid("65e8773d-8f56-11d0-a3b9-00a0c9223196"), "");
	public static readonly DevClass KSCATEGORY_CLOCK                         = new DevClass("", new Guid("53172480-4791-11d0-a5d6-28db04c10000"), "");
	public static readonly DevClass KSCATEGORY_COMMUNICATIONSTRANSFORM       = new DevClass("", new Guid("cf1dda2c-9743-11d0-a3ee-00a0c9223196"), "");
	public static readonly DevClass KSCATEGORY_CROSSBAR                      = new DevClass("", new Guid("a799a801-a46d-11d0-a18c-00a02401dcd4"), "");
	public static readonly DevClass KSCATEGORY_DATACOMPRESSOR                = new DevClass("", new Guid("1e84c900-7e70-11d0-a5d6-28db04c10000"), "");
	public static readonly DevClass KSCATEGORY_DATADECOMPRESSOR              = new DevClass("", new Guid("2721ae20-7e70-11d0-a5d6-28db04c10000"), "");
	public static readonly DevClass KSCATEGORY_DATATRANSFORM                 = new DevClass("", new Guid("2eb07ea0-7e70-11d0-a5d6-28db04c10000"), "");
	public static readonly DevClass KSCATEGORY_DRM_DESCRAMBLE                = new DevClass("", new Guid("ffbb6e3f-ccfe-4d84-90d9-421418b03a8e"), "");
	public static readonly DevClass KSCATEGORY_ENCODER                       = new DevClass("", new Guid("19689bf6-c384-48fd-ad51-90e58c79f70b"), "");
	public static readonly DevClass KSCATEGORY_ESCALANTE_PLATFORM_DRIVER     = new DevClass("", new Guid("74f3aea8-9768-11d1-8e07-00a0c95ec22e"), "");
	public static readonly DevClass KSCATEGORY_FILESYSTEM                    = new DevClass("", new Guid("760fed5e-9357-11d0-a3cc-00a0c9223196"), "");
	public static readonly DevClass KSCATEGORY_INTERFACETRANSFORM            = new DevClass("", new Guid("cf1dda2d-9743-11d0-a3ee-00a0c9223196"), "");
	public static readonly DevClass KSCATEGORY_MEDIUMTRANSFORM               = new DevClass("", new Guid("cf1dda2e-9743-11d0-a3ee-00a0c9223196"), "");
	public static readonly DevClass KSCATEGORY_MICROPHONE_ARRAY_PROCESSOR    = new DevClass("", new Guid("830a44f2-a32d-476b-be97-42845673b35a"), "");
	public static readonly DevClass KSCATEGORY_MIXER                         = new DevClass("", new Guid("ad809c00-7b88-11d0-a5d6-28db04c10000"), "");
	public static readonly DevClass KSCATEGORY_MULTIPLEXER                   = new DevClass("", new Guid("7a5de1d3-01a1-452c-b481-4fa2b96271e8"), "");
	public static readonly DevClass KSCATEGORY_NETWORK                       = new DevClass("", new Guid("67c9cc3c-69c4-11d2-8759-00a0c9223196"), "");
	public static readonly DevClass KSCATEGORY_PREFERRED_MIDIOUT_DEVICE      = new DevClass("", new Guid("d6c50674-72c1-11d2-9755-0000f8004788"), "");
	public static readonly DevClass KSCATEGORY_PREFERRED_WAVEIN_DEVICE       = new DevClass("", new Guid("d6c50671-72c1-11d2-9755-0000f8004788"), "");
	public static readonly DevClass KSCATEGORY_PREFERRED_WAVEOUT_DEVICE      = new DevClass("", new Guid("d6c5066e-72c1-11d2-9755-0000f8004788"), "");
	public static readonly DevClass KSCATEGORY_PROXY                         = new DevClass("", new Guid("97ebaaca-95bd-11d0-a3ea-00a0c9223196"), "");
	public static readonly DevClass KSCATEGORY_QUALITY                       = new DevClass("", new Guid("97ebaacb-95bd-11d0-a3ea-00a0c9223196"), "");
	public static readonly DevClass KSCATEGORY_REALTIME                      = new DevClass("", new Guid("eb115ffc-10c8-4964-831d-6dcb02e6f23f"), "");
	public static readonly DevClass KSCATEGORY_RENDER                        = new DevClass("", new Guid("65e8773e-8f56-11d0-a3b9-00a0c9223196"), "");
	public static readonly DevClass KSCATEGORY_SPLITTER                      = new DevClass("", new Guid("0a4252a0-7e70-11d0-a5d6-28db04c10000"), "");
	public static readonly DevClass KSCATEGORY_SYNTHESIZER                   = new DevClass("", new Guid("dff220f3-f70f-11d0-b917-00a0c9223196"), "");
	public static readonly DevClass KSCATEGORY_SYSAUDIO                      = new DevClass("", new Guid("a7c7a5b1-5af3-11d1-9ced-00a024bf0407"), "");
	public static readonly DevClass KSCATEGORY_TEXT                          = new DevClass("", new Guid("6994ad06-93ef-11d0-a3cc-00a0c9223196"), "");
	public static readonly DevClass KSCATEGORY_TOPOLOGY                      = new DevClass("", new Guid("dda54a40-1e4c-11d1-a050-405705c10000"), "");
	public static readonly DevClass KSCATEGORY_TVAUDIO                       = new DevClass("", new Guid("a799a802-a46d-11d0-a18c-00a02401dcd4"), "");
	public static readonly DevClass KSCATEGORY_TVTUNER                       = new DevClass("", new Guid("a799a800-a46d-11d0-a18c-00a02401dcd4"), "");
	public static readonly DevClass KSCATEGORY_VBICODEC                      = new DevClass("", new Guid("07dad660-22f1-11d1-a9f4-00c04fbbde8f"), "");
	public static readonly DevClass KSCATEGORY_VIDEO                         = new DevClass("", new Guid("6994ad05-93ef-11d0-a3cc-00a0c9223196"), "");
	public static readonly DevClass KSCATEGORY_VIRTUAL                       = new DevClass("", new Guid("3503eac4-1f26-11d1-8ab0-00a0c9223196"), "");
	public static readonly DevClass KSCATEGORY_VPMUX                         = new DevClass("", new Guid("a799a803-a46d-11d0-a18c-00a02401dcd4"), "");
	public static readonly DevClass KSCATEGORY_WDMAUD                        = new DevClass("", new Guid("3e227e76-690d-11d2-8161-0000f8775bf1"), "");
	public static readonly DevClass KSMFT_CATEGORY_AUDIO_DECODER             = new DevClass("", new Guid("9ea73fb4-ef7a-4559-8d5d-719d8f0426c7"), "");
	public static readonly DevClass KSMFT_CATEGORY_AUDIO_EFFECT              = new DevClass("", new Guid("11064c48-3648-4ed0-932e-05ce8ac811b7"), "");
	public static readonly DevClass KSMFT_CATEGORY_AUDIO_ENCODER             = new DevClass("", new Guid("91c64bd0-f91e-4d8c-9276-db248279d975"), "");
	public static readonly DevClass KSMFT_CATEGORY_DEMULTIPLEXER             = new DevClass("", new Guid("a8700a7a-939b-44c5-99d7-76226b23b3f1"), "");
	public static readonly DevClass KSMFT_CATEGORY_MULTIPLEXER               = new DevClass("", new Guid("059c561e-05ae-4b61-b69d-55b61ee54a7b"), "");
	public static readonly DevClass KSMFT_CATEGORY_OTHER                     = new DevClass("", new Guid("90175d57-b7ea-4901-aeb3-933a8747756f"), "");
	public static readonly DevClass KSMFT_CATEGORY_VIDEO_DECODER             = new DevClass("", new Guid("d6c02d4b-6833-45b4-971a-05a4b04bab91"), "");
	public static readonly DevClass KSMFT_CATEGORY_VIDEO_EFFECT              = new DevClass("", new Guid("12e17c21-532c-4a6e-8a1c-40825a736397"), "");
	public static readonly DevClass KSMFT_CATEGORY_VIDEO_ENCODER             = new DevClass("", new Guid("f79eac7d-e545-4387-bdee-d647d7bde42a"), "");
	public static readonly DevClass KSMFT_CATEGORY_VIDEO_PROCESSOR           = new DevClass("", new Guid("302ea3fc-aa5f-47f9-9f7a-c2188bb16302"), "");
#endif
		#endregion
	}
}
