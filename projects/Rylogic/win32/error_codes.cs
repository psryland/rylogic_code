using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using pr.extn;

namespace pr.win32
{
	public static partial class Win32
	{
		#region Code Values
		public const int ERROR_SUCCESS                                              = 0  ;//(0x0) The operation completed successfully.
		public const int ERROR_INVALID_FUNCTION                                     = 1  ;//(0x1) Incorrect function.
		public const int ERROR_FILE_NOT_FOUND                                       = 2  ;//(0x2) The system cannot find the file specified.
		public const int ERROR_PATH_NOT_FOUND                                       = 3  ;//(0x3) The system cannot find the path specified.
		public const int ERROR_TOO_MANY_OPEN_FILES                                  = 4  ;//(0x4) The system cannot open the file.
		public const int ERROR_ACCESS_DENIED                                        = 5  ;//(0x5) Access is denied.
		public const int ERROR_INVALID_HANDLE                                       = 6  ;//(0x6) The handle is invalid.
		public const int ERROR_ARENA_TRASHED                                        = 7  ;//(0x7) The storage control blocks were destroyed.
		public const int ERROR_NOT_ENOUGH_MEMORY                                    = 8  ;//(0x8) Not enough storage is available to process this command.
		public const int ERROR_INVALID_BLOCK                                        = 9  ;//(0x9) The storage control block address is invalid.
		public const int ERROR_BAD_ENVIRONMENT                                      = 10 ;//(0xA) The environment is incorrect.
		public const int ERROR_BAD_FORMAT                                           = 11 ;//(0xB) An attempt was made to load a program with an incorrect format.
		public const int ERROR_INVALID_ACCESS                                       = 12 ;//(0xC) The access code is invalid.
		public const int ERROR_INVALID_DATA                                         = 13 ;//(0xD) The data is invalid.
		public const int ERROR_OUTOFMEMORY                                          = 14 ;//(0xE) Not enough storage is available to complete this operation.
		public const int ERROR_INVALID_DRIVE                                        = 15 ;//(0xF) The system cannot find the drive specified.
		public const int ERROR_CURRENT_DIRECTORY                                    = 16 ;//(0x10) The directory cannot be removed.
		public const int ERROR_NOT_SAME_DEVICE                                      = 17 ;//(0x11) The system cannot move the file to a different disk drive.
		public const int ERROR_NO_MORE_FILES                                        = 18 ;//(0x12) There are no more files.
		public const int ERROR_WRITE_PROTECT                                        = 19 ;//(0x13) The media is write protected.
		public const int ERROR_BAD_UNIT                                             = 20 ;//(0x14) The system cannot find the device specified.
		public const int ERROR_NOT_READY                                            = 21 ;//(0x15) The device is not ready.
		public const int ERROR_BAD_COMMAND                                          = 22 ;//(0x16) The device does not recognize the command.
		public const int ERROR_CRC                                                  = 23 ;//(0x17) Data error (cyclic redundancy check).
		public const int ERROR_BAD_LENGTH                                           = 24 ;//(0x18) The program issued a command but the command length is incorrect.
		public const int ERROR_SEEK                                                 = 25 ;//(0x19) The drive cannot locate a specific area or track on the disk.
		public const int ERROR_NOT_DOS_DISK                                         = 26 ;//(0x1A) The specified disk or diskette cannot be accessed.
		public const int ERROR_SECTOR_NOT_FOUND                                     = 27 ;//(0x1B) The drive cannot find the sector requested.
		public const int ERROR_OUT_OF_PAPER                                         = 28 ;//(0x1C) The printer is out of paper.
		public const int ERROR_WRITE_FAULT                                          = 29 ;//(0x1D) The system cannot write to the specified device.
		public const int ERROR_READ_FAULT                                           = 30 ;//(0x1E) The system cannot read from the specified device.
		public const int ERROR_GEN_FAILURE                                          = 31 ;//(0x1F) A device attached to the system is not functioning.
		public const int ERROR_SHARING_VIOLATION                                    = 32 ;//(0x20) The process cannot access the file because it is being used by another process.
		public const int ERROR_LOCK_VIOLATION                                       = 33 ;//(0x21) The process cannot access the file because another process has locked a portion of the file.
		public const int ERROR_WRONG_DISK                                           = 34 ;//(0x22) The wrong diskette is in the drive. Insert %2 (Volume Serial Number: %3) into drive %1.
		public const int ERROR_SHARING_BUFFER_EXCEEDED                              = 36 ;//(0x24) Too many files opened for sharing.
		public const int ERROR_HANDLE_EOF                                           = 38 ;//(0x26) Reached the end of the file.
		public const int ERROR_HANDLE_DISK_FULL                                     = 39 ;//(0x27) The disk is full.
		public const int ERROR_NOT_SUPPORTED                                        = 50 ;//(0x32) The request is not supported.
		public const int ERROR_REM_NOT_LIST                                         = 51 ;//(0x33) Windows cannot find the network path. Verify that the network path is correct and the destination computer is not busy or turned off. If Windows still cannot find the network path, contact your network administrator.
		public const int ERROR_DUP_NAME                                             = 52 ;//(0x34) You were not connected because a duplicate name exists on the network. If joining a domain, go to System in Control Panel to change the computer name and try again. If joining a workgroup, choose another workgroup name.
		public const int ERROR_BAD_NETPATH                                          = 53 ;//(0x35) The network path was not found.
		public const int ERROR_NETWORK_BUSY                                         = 54 ;//(0x36) The network is busy.
		public const int ERROR_DEV_NOT_EXIST                                        = 55 ;//(0x37) The specified network resource or device is no longer available.
		public const int ERROR_TOO_MANY_CMDS                                        = 56 ;//(0x38) The network BIOS command limit has been reached.
		public const int ERROR_ADAP_HDW_ERR                                         = 57 ;//(0x39) A network adapter hardware error occurred.
		public const int ERROR_BAD_NET_RESP                                         = 58 ;//(0x3A) The specified server cannot perform the requested operation.
		public const int ERROR_UNEXP_NET_ERR                                        = 59 ;//(0x3B) An unexpected network error occurred.
		public const int ERROR_BAD_REM_ADAP                                         = 60 ;//(0x3C) The remote adapter is not compatible.
		public const int ERROR_PRINTQ_FULL                                          = 61 ;//(0x3D) The printer queue is full.
		public const int ERROR_NO_SPOOL_SPACE                                       = 62 ;//(0x3E) Space to store the file waiting to be printed is not available on the server.
		public const int ERROR_PRINT_CANCELLED                                      = 63 ;//(0x3F) Your file waiting to be printed was deleted.
		public const int ERROR_NETNAME_DELETED                                      = 64 ;//(0x40) The specified network name is no longer available.
		public const int ERROR_NETWORK_ACCESS_DENIED                                = 65 ;//(0x41) Network access is denied.
		public const int ERROR_BAD_DEV_TYPE                                         = 66 ;//(0x42) The network resource type is not correct.
		public const int ERROR_BAD_NET_NAME                                         = 67 ;//(0x43) The network name cannot be found.
		public const int ERROR_TOO_MANY_NAMES                                       = 68 ;//(0x44) The name limit for the local computer network adapter card was exceeded.
		public const int ERROR_TOO_MANY_SESS                                        = 69 ;//(0x45) The network BIOS session limit was exceeded.
		public const int ERROR_SHARING_PAUSED                                       = 70 ;//(0x46) The remote server has been paused or is in the process of being started.
		public const int ERROR_REQ_NOT_ACCEP                                        = 71 ;//(0x47) No more connections can be made to this remote computer at this time because there are already as many connections as the computer can accept.
		public const int ERROR_REDIR_PAUSED                                         = 72 ;//(0x48) The specified printer or disk device has been paused.
		public const int ERROR_FILE_EXISTS                                          = 80 ;//(0x50) The file exists.
		public const int ERROR_CANNOT_MAKE                                          = 82 ;//(0x52) The directory or file cannot be created.
		public const int ERROR_FAIL_I24                                             = 83 ;//(0x53) Fail on INT 24.
		public const int ERROR_OUT_OF_STRUCTURES                                    = 84 ;//(0x54) Storage to process this request is not available.
		public const int ERROR_ALREADY_ASSIGNED                                     = 85 ;//(0x55) The local device name is already in use.
		public const int ERROR_INVALID_PASSWORD                                     = 86 ;//(0x56) The specified network password is not correct.
		public const int ERROR_INVALID_PARAMETER                                    = 87 ;//(0x57) The parameter is incorrect.
		public const int ERROR_NET_WRITE_FAULT                                      = 88 ;//(0x58) A write fault occurred on the network.
		public const int ERROR_NO_PROC_SLOTS                                        = 89 ;//(0x59) The system cannot start another process at this time.
		public const int ERROR_TOO_MANY_SEMAPHORES                                  = 100;//(0x64) Cannot create another system semaphore.
		public const int ERROR_EXCL_SEM_ALREADY_OWNED                               = 101;//(0x65) The exclusive semaphore is owned by another process.
		public const int ERROR_SEM_IS_SET                                           = 102;//(0x66) The semaphore is set and cannot be closed.
		public const int ERROR_TOO_MANY_SEM_REQUESTS                                = 103;//(0x67) The semaphore cannot be set again.
		public const int ERROR_INVALID_AT_INTERRUPT_TIME                            = 104;//(0x68) Cannot request exclusive semaphores at interrupt time.
		public const int ERROR_SEM_OWNER_DIED                                       = 105;//(0x69) The previous ownership of this semaphore has ended.
		public const int ERROR_SEM_USER_LIMIT                                       = 106;//(0x6A) Insert the diskette for drive %1.
		public const int ERROR_DISK_CHANGE                                          = 107;//(0x6B) The program stopped because an alternate diskette was not inserted.
		public const int ERROR_DRIVE_LOCKED                                         = 108;//(0x6C) The disk is in use or locked by another process.
		public const int ERROR_BROKEN_PIPE                                          = 109;//(0x6D) The pipe has been ended.
		public const int ERROR_OPEN_FAILED                                          = 110;//(0x6E) The system cannot open the device or file specified.
		public const int ERROR_BUFFER_OVERFLOW                                      = 111;//(0x6F) The file name is too long.
		public const int ERROR_DISK_FULL                                            = 112;//(0x70) There is not enough space on the disk.
		public const int ERROR_NO_MORE_SEARCH_HANDLES                               = 113;//(0x71) No more internal file identifiers available.
		public const int ERROR_INVALID_TARGET_HANDLE                                = 114;//(0x72) The target internal file identifier is incorrect.
		public const int ERROR_INVALID_CATEGORY                                     = 117;//(0x75) The IOCTL call made by the application program is not correct.
		public const int ERROR_INVALID_VERIFY_SWITCH                                = 118;//(0x76) The verify-on-write switch parameter value is not correct.
		public const int ERROR_BAD_DRIVER_LEVEL                                     = 119;//(0x77) The system does not support the command requested.
		public const int ERROR_CALL_NOT_IMPLEMENTED                                 = 120;//(0x78) This function is not supported on this system.
		public const int ERROR_SEM_TIMEOUT                                          = 121;//(0x79) The semaphore timeout period has expired.
		public const int ERROR_INSUFFICIENT_BUFFER                                  = 122;//(0x7A) The data area passed to a system call is too small.
		public const int ERROR_INVALID_NAME                                         = 123;//(0x7B) The filename, directory name, or volume label syntax is incorrect.
		public const int ERROR_INVALID_LEVEL                                        = 124;//(0x7C) The system call level is not correct.
		public const int ERROR_NO_VOLUME_LABEL                                      = 125;//(0x7D) The disk has no volume label.
		public const int ERROR_MOD_NOT_FOUND                                        = 126;//(0x7E) The specified module could not be found.
		public const int ERROR_PROC_NOT_FOUND                                       = 127;//(0x7F) The specified procedure could not be found.
		public const int ERROR_WAIT_NO_CHILDREN                                     = 128;//(0x80) There are no child processes to wait for.
		public const int ERROR_CHILD_NOT_COMPLETE                                   = 129;//(0x81) The %1 application cannot be run in Win32 mode.
		public const int ERROR_DIRECT_ACCESS_HANDLE                                 = 130;//(0x82) Attempt to use a file handle to an open disk partition for an operation other than raw disk I/O.
		public const int ERROR_NEGATIVE_SEEK                                        = 131;//(0x83) An attempt was made to move the file pointer before the beginning of the file.
		public const int ERROR_SEEK_ON_DEVICE                                       = 132;//(0x84) The file pointer cannot be set on the specified device or file.
		public const int ERROR_IS_JOIN_TARGET                                       = 133;//(0x85) A JOIN or SUBST command cannot be used for a drive that contains previously joined drives.
		public const int ERROR_IS_JOINED                                            = 134;//(0x86) An attempt was made to use a JOIN or SUBST command on a drive that has already been joined.
		public const int ERROR_IS_SUBSTED                                           = 135;//(0x87) An attempt was made to use a JOIN or SUBST command on a drive that has already been substituted.
		public const int ERROR_NOT_JOINED                                           = 136;//(0x88) The system tried to delete the JOIN of a drive that is not joined.
		public const int ERROR_NOT_SUBSTED                                          = 137;//(0x89) The system tried to delete the substitution of a drive that is not substituted.
		public const int ERROR_JOIN_TO_JOIN                                         = 138;//(0x8A) The system tried to join a drive to a directory on a joined drive.
		public const int ERROR_SUBST_TO_SUBST                                       = 139;//(0x8B) The system tried to substitute a drive to a directory on a substituted drive.
		public const int ERROR_JOIN_TO_SUBST                                        = 140;//(0x8C) The system tried to join a drive to a directory on a substituted drive.
		public const int ERROR_SUBST_TO_JOIN                                        = 141;//(0x8D) The system tried to SUBST a drive to a directory on a joined drive.
		public const int ERROR_BUSY_DRIVE                                           = 142;//(0x8E) The system cannot perform a JOIN or SUBST at this time.
		public const int ERROR_SAME_DRIVE                                           = 143;//(0x8F) The system cannot join or substitute a drive to or for a directory on the same drive.
		public const int ERROR_DIR_NOT_ROOT                                         = 144;//(0x90) The directory is not a subdirectory of the root directory.
		public const int ERROR_DIR_NOT_EMPTY                                        = 145;//(0x91) The directory is not empty.
		public const int ERROR_IS_SUBST_PATH                                        = 146;//(0x92) The path specified is being used in a substitute.
		public const int ERROR_IS_JOIN_PATH                                         = 147;//(0x93) Not enough resources are available to process this command.
		public const int ERROR_PATH_BUSY                                            = 148;//(0x94) The path specified cannot be used at this time.
		public const int ERROR_IS_SUBST_TARGET                                      = 149;//(0x95) An attempt was made to join or substitute a drive for which a directory on the drive is the target of a previous substitute.
		public const int ERROR_SYSTEM_TRACE                                         = 150;//(0x96) System trace information was not specified in your CONFIG.SYS file, or tracing is disallowed.
		public const int ERROR_INVALID_EVENT_COUNT                                  = 151;//(0x97) The number of specified semaphore events for DosMuxSemWait is not correct.
		public const int ERROR_TOO_MANY_MUXWAITERS                                  = 152;//(0x98) DosMuxSemWait did not execute; too many semaphores are already set.
		public const int ERROR_INVALID_LIST_FORMAT                                  = 153;//(0x99) The DosMuxSemWait list is not correct.
		public const int ERROR_LABEL_TOO_LONG                                       = 154;//(0x9A) The volume label you entered exceeds the label character limit of the target file system.
		public const int ERROR_TOO_MANY_TCBS                                        = 155;//(0x9B) Cannot create another thread.
		public const int ERROR_SIGNAL_REFUSED                                       = 156;//(0x9C) The recipient process has refused the signal.
		public const int ERROR_DISCARDED                                            = 157;//(0x9D) The segment is already discarded and cannot be locked.
		public const int ERROR_NOT_LOCKED                                           = 158;//(0x9E) The segment is already unlocked.
		public const int ERROR_BAD_THREADID_ADDR                                    = 159;//(0x9F) The address for the thread ID is not correct.
		public const int ERROR_BAD_ARGUMENTS                                        = 160;//(0xA0) One or more arguments are not correct.
		public const int ERROR_BAD_PATHNAME                                         = 161;//(0xA1) The specified path is invalid.
		public const int ERROR_SIGNAL_PENDING                                       = 162;//(0xA2) A signal is already pending.
		public const int ERROR_MAX_THRDS_REACHED                                    = 164;//(0xA4) No more threads can be created in the system.
		public const int ERROR_LOCK_FAILED                                          = 167;//(0xA7) Unable to lock a region of a file.
		public const int ERROR_BUSY                                                 = 170;//(0xAA) The requested resource is in use.
		public const int ERROR_DEVICE_SUPPORT_IN_PROGRESS                           = 171;//(0xAB) Device's command support detection is in progress.
		public const int ERROR_CANCEL_VIOLATION                                     = 173;//(0xAD) A lock request was not outstanding for the supplied cancel region.
		public const int ERROR_ATOMIC_LOCKS_NOT_SUPPORTED                           = 174;//(0xAE) The file system does not support atomic changes to the lock type.
		public const int ERROR_INVALID_SEGMENT_NUMBER                               = 180;//(0xB4) The system detected a segment number that was not correct.
		public const int ERROR_INVALID_ORDINAL                                      = 182;//(0xB6) The operating system cannot run %1.
		public const int ERROR_ALREADY_EXISTS                                       = 183;//(0xB7) Cannot create a file when that file already exists.
		public const int ERROR_INVALID_FLAG_NUMBER                                  = 186;//(0xBA) The flag passed is not correct.
		public const int ERROR_SEM_NOT_FOUND                                        = 187;//(0xBB) The specified system semaphore name was not found.
		public const int ERROR_INVALID_STARTING_CODESEG                             = 188;//(0xBC) The operating system cannot run %1.
		public const int ERROR_INVALID_STACKSEG                                     = 189;//(0xBD) The operating system cannot run %1.
		public const int ERROR_INVALID_MODULETYPE                                   = 190;//(0xBE) The operating system cannot run %1.
		public const int ERROR_INVALID_EXE_SIGNATURE                                = 191;//(0xBF) Cannot run %1 in Win32 mode.
		public const int ERROR_EXE_MARKED_INVALID                                   = 192;//(0xC0) The operating system cannot run %1.
		public const int ERROR_BAD_EXE_FORMAT                                       = 193;//(0xC1) %1 is not a valid Win32 application.
		public const int ERROR_ITERATED_DATA_EXCEEDS_64k                            = 194;//(0xC2) The operating system cannot run %1.
		public const int ERROR_INVALID_MINALLOCSIZE                                 = 195;//(0xC3) The operating system cannot run %1.
		public const int ERROR_DYNLINK_FROM_INVALID_RING                            = 196;//(0xC4) The operating system cannot run this application program.
		public const int ERROR_IOPL_NOT_ENABLED                                     = 197;//(0xC5) The operating system is not presently configured to run this application.
		public const int ERROR_INVALID_SEGDPL                                       = 198;//(0xC6) The operating system cannot run %1.
		public const int ERROR_AUTODATASEG_EXCEEDS_64k                              = 199;//(0xC7) The operating system cannot run this application program.
		public const int ERROR_RING2SEG_MUST_BE_MOVABLE                             = 200;//(0xC8) The code segment cannot be greater than or equal to 64K.
		public const int ERROR_RELOC_CHAIN_XEEDS_SEGLIM                             = 201;//(0xC9) The operating system cannot run %1.
		public const int ERROR_INFLOOP_IN_RELOC_CHAIN                               = 202;//(0xCA) The operating system cannot run %1.
		public const int ERROR_ENVVAR_NOT_FOUND                                     = 203;//(0xCB) The system could not find the environment option that was entered.
		public const int ERROR_NO_SIGNAL_SENT                                       = 205;//(0xCD) No process in the command subtree has a signal handler.
		public const int ERROR_FILENAME_EXCED_RANGE                                 = 206;//(0xCE) The filename or extension is too long.
		public const int ERROR_RING2_STACK_IN_USE                                   = 207;//(0xCF) The ring 2 stack is in use.
		public const int ERROR_META_EXPANSION_TOO_LONG                              = 208;//(0xD0) The global filename characters, * or ?, are entered incorrectly or too many global filename characters are specified.
		public const int ERROR_INVALID_SIGNAL_NUMBER                                = 209;//(0xD1) The signal being posted is not correct.
		public const int ERROR_THREAD_1_INACTIVE                                    = 210;//(0xD2) The signal handler cannot be set.
		public const int ERROR_LOCKED                                               = 212;//(0xD4) The segment is locked and cannot be reallocated.
		public const int ERROR_TOO_MANY_MODULES                                     = 214;//(0xD6) Too many dynamic-link modules are attached to this program or dynamic-link module.
		public const int ERROR_NESTING_NOT_ALLOWED                                  = 215;//(0xD7) Cannot nest calls to LoadModule.
		public const int ERROR_EXE_MACHINE_TYPE_MISMATCH                            = 216;//(0xD8) This version of %1 is not compatible with the version of Windows you're running. Check your computer's system information and then contact the software publisher.
		public const int ERROR_EXE_CANNOT_MODIFY_SIGNED_BINARY                      = 217;//(0xD9) The image file %1 is signed, unable to modify.
		public const int ERROR_EXE_CANNOT_MODIFY_STRONG_SIGNED_BINARY               = 218;//(0xDA) The image file %1 is strong signed, unable to modify.
		public const int ERROR_FILE_CHECKED_OUT                                     = 220;//(0xDC) This file is checked out or locked for editing by another user.
		public const int ERROR_CHECKOUT_REQUIRED                                    = 221;//(0xDD) The file must be checked out before saving changes.
		public const int ERROR_BAD_FILE_TYPE                                        = 222;//(0xDE) The file type being saved or retrieved has been blocked.
		public const int ERROR_FILE_TOO_LARGE                                       = 223;//(0xDF) The file size exceeds the limit allowed and cannot be saved.
		public const int ERROR_FORMS_AUTH_REQUIRED                                  = 224;//(0xE0) Access Denied. Before opening files in this location, you must first add the web site to your trusted sites list, browse to the web site, and select the option to login automatically.
		public const int ERROR_VIRUS_INFECTED                                       = 225;//(0xE1) Operation did not complete successfully because the file contains a virus or potentially unwanted software.
		public const int ERROR_VIRUS_DELETED                                        = 226;//(0xE2) This file contains a virus or potentially unwanted software and cannot be opened. Due to the nature of this virus or potentially unwanted software, the file has been removed from this location.
		public const int ERROR_PIPE_LOCAL                                           = 229;//(0xE5) The pipe is local.
		public const int ERROR_BAD_PIPE                                             = 230;//(0xE6) The pipe state is invalid.
		public const int ERROR_PIPE_BUSY                                            = 231;//(0xE7) All pipe instances are busy.
		public const int ERROR_NO_DATA                                              = 232;//(0xE8) The pipe is being closed.
		public const int ERROR_PIPE_NOT_CONNECTED                                   = 233;//(0xE9) No process is on the other end of the pipe.
		public const int ERROR_MORE_DATA                                            = 234;//(0xEA) More data is available.
		public const int ERROR_VC_DISCONNECTED                                      = 240;//(0xF0) The session was canceled.
		public const int ERROR_INVALID_EA_NAME                                      = 254;//(0xFE) The specified extended attribute name was invalid.
		public const int ERROR_EA_LIST_INCONSISTENT                                 = 255;//(0xFF) The extended attributes are inconsistent.
		public const int WAIT_TIMEOUT                                               = 258;//(0x102) The wait operation timed out.
		public const int ERROR_NO_MORE_ITEMS                                        = 259;//(0x103) No more data is available.
		public const int ERROR_CANNOT_COPY                                          = 266;//(0x10A) The copy functions cannot be used.
		public const int ERROR_DIRECTORY                                            = 267;//(0x10B) The directory name is invalid.
		public const int ERROR_EAS_DIDNT_FIT                                        = 275;//(0x113) The extended attributes did not fit in the buffer.
		public const int ERROR_EA_FILE_CORRUPT                                      = 276;//(0x114) The extended attribute file on the mounted file system is corrupt.
		public const int ERROR_EA_TABLE_FULL                                        = 277;//(0x115) The extended attribute table file is full.
		public const int ERROR_INVALID_EA_HANDLE                                    = 278;//(0x116) The specified extended attribute handle is invalid.
		public const int ERROR_EAS_NOT_SUPPORTED                                    = 282;//(0x11A) The mounted file system does not support extended attributes.
		public const int ERROR_NOT_OWNER                                            = 288;//(0x120) Attempt to release mutex not owned by caller.
		public const int ERROR_TOO_MANY_POSTS                                       = 298;//(0x12A) Too many posts were made to a semaphore.
		public const int ERROR_PARTIAL_COPY                                         = 299;//(0x12B) Only part of a ReadProcessMemory or WriteProcessMemory request was completed.
		public const int ERROR_OPLOCK_NOT_GRANTED                                   = 300;//(0x12C) The oplock request is denied.
		public const int ERROR_INVALID_OPLOCK_PROTOCOL                              = 301;//(0x12D) An invalid oplock acknowledgment was received by the system.
		public const int ERROR_DISK_TOO_FRAGMENTED                                  = 302;//(0x12E) The volume is too fragmented to complete this operation.
		public const int ERROR_DELETE_PENDING                                       = 303;//(0x12F) The file cannot be opened because it is in the process of being deleted.
		public const int ERROR_INCOMPATIBLE_WITH_GLOBAL_SHORT_NAME_REGISTRY_SETTING = 304;//(0x130) Short name settings may not be changed on this volume due to the global registry setting.
		public const int ERROR_SHORT_NAMES_NOT_ENABLED_ON_VOLUME                    = 305;//(0x131) Short names are not enabled on this volume.
		public const int ERROR_SECURITY_STREAM_IS_INCONSISTENT                      = 306;//(0x132) The security stream for the given volume is in an inconsistent state. Please run CHKDSK on the volume.
		public const int ERROR_INVALID_LOCK_RANGE                                   = 307;//(0x133) A requested file lock operation cannot be processed due to an invalid byte range.
		public const int ERROR_IMAGE_SUBSYSTEM_NOT_PRESENT                          = 308;//(0x134) The subsystem needed to support the image type is not present.
		public const int ERROR_NOTIFICATION_GUID_ALREADY_DEFINED                    = 309;//(0x135) The specified file already has a notification GUID associated with it.
		public const int ERROR_INVALID_EXCEPTION_HANDLER                            = 310;//(0x136) An invalid exception handler routine has been detected.
		public const int ERROR_DUPLICATE_PRIVILEGES                                 = 311;//(0x137) Duplicate privileges were specified for the token.
		public const int ERROR_NO_RANGES_PROCESSED                                  = 312;//(0x138) No ranges for the specified operation were able to be processed.
		public const int ERROR_NOT_ALLOWED_ON_SYSTEM_FILE                           = 313;//(0x139) Operation is not allowed on a file system internal file.
		public const int ERROR_DISK_RESOURCES_EXHAUSTED                             = 314;//(0x13A) The physical resources of this disk have been exhausted.
		public const int ERROR_INVALID_TOKEN                                        = 315;//(0x13B) The token representing the data is invalid.
		public const int ERROR_DEVICE_FEATURE_NOT_SUPPORTED                         = 316;//(0x13C) The device does not support the command feature.
		public const int ERROR_MR_MID_NOT_FOUND                                     = 317;//(0x13D) The system cannot find message text for message number 0x%1 in the message file for %2.
		public const int ERROR_SCOPE_NOT_FOUND                                      = 318;//(0x13E) The scope specified was not found.
		public const int ERROR_UNDEFINED_SCOPE                                      = 319;//(0x13F) The Central Access Policy specified is not defined on the target machine.
		public const int ERROR_INVALID_CAP                                          = 320;//(0x140) The Central Access Policy obtained from Active Directory is invalid.
		public const int ERROR_DEVICE_UNREACHABLE                                   = 321;//(0x141) The device is unreachable.
		public const int ERROR_DEVICE_NO_RESOURCES                                  = 322;//(0x142) The target device has insufficient resources to complete the operation.
		public const int ERROR_DATA_CHECKSUM_ERROR                                  = 323;//(0x143) A data integrity checksum error occurred. Data in the file stream is corrupt.
		public const int ERROR_INTERMIXED_KERNEL_EA_OPERATION                       = 324;//(0x144) An attempt was made to modify both a KERNEL and normal Extended Attribute (EA) in the same operation.
		public const int ERROR_FILE_LEVEL_TRIM_NOT_SUPPORTED                        = 326;//(0x146) Device does not support file-level TRIM.
		public const int ERROR_OFFSET_ALIGNMENT_VIOLATION                           = 327;//(0x147) The command specified a data offset that does not align to the device's granularity/alignment.
		public const int ERROR_INVALID_FIELD_IN_PARAMETER_LIST                      = 328;//(0x148) The command specified an invalid field in its parameter list.
		public const int ERROR_OPERATION_IN_PROGRESS                                = 329;//(0x149) An operation is currently in progress with the device.
		public const int ERROR_BAD_DEVICE_PATH                                      = 330;//(0x14A) An attempt was made to send down the command via an invalid path to the target device.
		public const int ERROR_TOO_MANY_DESCRIPTORS                                 = 331;//(0x14B) The command specified a number of descriptors that exceeded the maximum supported by the device.
		public const int ERROR_SCRUB_DATA_DISABLED                                  = 332;//(0x14C) Scrub is disabled on the specified file.
		public const int ERROR_NOT_REDUNDANT_STORAGE                                = 333;//(0x14D) The storage device does not provide redundancy.
		public const int ERROR_RESIDENT_FILE_NOT_SUPPORTED                          = 334;//(0x14E) An operation is not supported on a resident file.
		public const int ERROR_COMPRESSED_FILE_NOT_SUPPORTED                        = 335;//(0x14F) An operation is not supported on a compressed file.
		public const int ERROR_DIRECTORY_NOT_SUPPORTED                              = 336;//(0x150) An operation is not supported on a directory.
		public const int ERROR_NOT_READ_FROM_COPY                                   = 337;//(0x151) The specified copy of the requested data could not be read.
		public const int ERROR_FAIL_NOACTION_REBOOT                                 = 350;//(0x15E) No action was taken as a system reboot is required.
		public const int ERROR_FAIL_SHUTDOWN                                        = 351;//(0x15F) The shutdown operation failed.
		public const int ERROR_FAIL_RESTART                                         = 352;//(0x160) The restart operation failed.
		public const int ERROR_MAX_SESSIONS_REACHED                                 = 353;//(0x161) The maximum number of sessions has been reached.
		public const int ERROR_THREAD_MODE_ALREADY_BACKGROUND                       = 400;//(0x190) The thread is already in background processing mode.
		public const int ERROR_THREAD_MODE_NOT_BACKGROUND                           = 401;//(0x191) The thread is not in background processing mode.
		public const int ERROR_PROCESS_MODE_ALREADY_BACKGROUND                      = 402;//(0x192) The process is already in background processing mode.
		public const int ERROR_PROCESS_MODE_NOT_BACKGROUND                          = 403;//(0x193) The process is not in background processing mode.
		public const int ERROR_INVALID_ADDRESS                                      = 487;//(0x1E7) Attempt to access invalid address.
		#endregion

		#region HRESULT
		public enum HRESULT :long
		{
			S_FALSE         = 0x00000001,
			S_OK            = 0x00000000,
			E_OUTOFMEMORY   = 0x8007000E,
			E_INVALIDARG    = 0x80070057,
			ERROR_CANCELLED = 0x800704C7,
		}
		#endregion

		#region Format Message
		public const uint FORMAT_MESSAGE_IGNORE_INSERTS  = 0x00000200U;
		public const uint FORMAT_MESSAGE_FROM_STRING     = 0x00000400U;
		public const uint FORMAT_MESSAGE_FROM_HMODULE    = 0x00000800U;
		public const uint FORMAT_MESSAGE_FROM_SYSTEM     = 0x00001000U;
		public const uint FORMAT_MESSAGE_ARGUMENT_ARRAY  = 0x00002000U;
		public const uint FORMAT_MESSAGE_MAX_WIDTH_MASK  = 0x000000FFU;
		#endregion

		/// <summary>Translate an error code to a string message</summary>
		public static string ErrorCodeToString(int error_code)
		{
			switch (error_code)
			{
			default: return "Error Code {0}".Fmt(error_code);
			case ERROR_SUCCESS                                              : return "(0x0) The operation completed successfully.";
			case ERROR_INVALID_FUNCTION                                     : return "(0x1) Incorrect function.";
			case ERROR_FILE_NOT_FOUND                                       : return "(0x2) The system cannot find the file specified.";
			case ERROR_PATH_NOT_FOUND                                       : return "(0x3) The system cannot find the path specified.";
			case ERROR_TOO_MANY_OPEN_FILES                                  : return "(0x4) The system cannot open the file.";
			case ERROR_ACCESS_DENIED                                        : return "(0x5) Access is denied.";
			case ERROR_INVALID_HANDLE                                       : return "(0x6) The handle is invalid.";
			case ERROR_ARENA_TRASHED                                        : return "(0x7) The storage control blocks were destroyed.";
			case ERROR_NOT_ENOUGH_MEMORY                                    : return "(0x8) Not enough storage is available to process this command.";
			case ERROR_INVALID_BLOCK                                        : return "(0x9) The storage control block address is invalid.";
			case ERROR_BAD_ENVIRONMENT                                      : return "(0xA) The environment is incorrect.";
			case ERROR_BAD_FORMAT                                           : return "(0xB) An attempt was made to load a program with an incorrect format.";
			case ERROR_INVALID_ACCESS                                       : return "(0xC) The access code is invalid.";
			case ERROR_INVALID_DATA                                         : return "(0xD) The data is invalid.";
			case ERROR_OUTOFMEMORY                                          : return "(0xE) Not enough storage is available to complete this operation.";
			case ERROR_INVALID_DRIVE                                        : return "(0xF) The system cannot find the drive specified.";
			case ERROR_CURRENT_DIRECTORY                                    : return "(0x10) The directory cannot be removed.";
			case ERROR_NOT_SAME_DEVICE                                      : return "(0x11) The system cannot move the file to a different disk drive.";
			case ERROR_NO_MORE_FILES                                        : return "(0x12) There are no more files.";
			case ERROR_WRITE_PROTECT                                        : return "(0x13) The media is write protected.";
			case ERROR_BAD_UNIT                                             : return "(0x14) The system cannot find the device specified.";
			case ERROR_NOT_READY                                            : return "(0x15) The device is not ready.";
			case ERROR_BAD_COMMAND                                          : return "(0x16) The device does not recognize the command.";
			case ERROR_CRC                                                  : return "(0x17) Data error (cyclic redundancy check).";
			case ERROR_BAD_LENGTH                                           : return "(0x18) The program issued a command but the command length is incorrect.";
			case ERROR_SEEK                                                 : return "(0x19) The drive cannot locate a specific area or track on the disk.";
			case ERROR_NOT_DOS_DISK                                         : return "(0x1A) The specified disk or diskette cannot be accessed.";
			case ERROR_SECTOR_NOT_FOUND                                     : return "(0x1B) The drive cannot find the sector requested.";
			case ERROR_OUT_OF_PAPER                                         : return "(0x1C) The printer is out of paper.";
			case ERROR_WRITE_FAULT                                          : return "(0x1D) The system cannot write to the specified device.";
			case ERROR_READ_FAULT                                           : return "(0x1E) The system cannot read from the specified device.";
			case ERROR_GEN_FAILURE                                          : return "(0x1F) A device attached to the system is not functioning.";
			case ERROR_SHARING_VIOLATION                                    : return "(0x20) The process cannot access the file because it is being used by another process.";
			case ERROR_LOCK_VIOLATION                                       : return "(0x21) The process cannot access the file because another process has locked a portion of the file.";
			case ERROR_WRONG_DISK                                           : return "(0x22) The wrong diskette is in the drive. Insert %2 (Volume Serial Number: %3) into drive %1.";
			case ERROR_SHARING_BUFFER_EXCEEDED                              : return "(0x24) Too many files opened for sharing.";
			case ERROR_HANDLE_EOF                                           : return "(0x26) Reached the end of the file.";
			case ERROR_HANDLE_DISK_FULL                                     : return "(0x27) The disk is full.";
			case ERROR_NOT_SUPPORTED                                        : return "(0x32) The request is not supported.";
			case ERROR_REM_NOT_LIST                                         : return "(0x33) Windows cannot find the network path. Verify that the network path is correct and the destination computer is not busy or turned off. If Windows still cannot find the network path, contact your network administrator.";
			case ERROR_DUP_NAME                                             : return "(0x34) You were not connected because a duplicate name exists on the network. If joining a domain, go to System in Control Panel to change the computer name and try again. If joining a workgroup, choose another workgroup name.";
			case ERROR_BAD_NETPATH                                          : return "(0x35) The network path was not found.";
			case ERROR_NETWORK_BUSY                                         : return "(0x36) The network is busy.";
			case ERROR_DEV_NOT_EXIST                                        : return "(0x37) The specified network resource or device is no longer available.";
			case ERROR_TOO_MANY_CMDS                                        : return "(0x38) The network BIOS command limit has been reached.";
			case ERROR_ADAP_HDW_ERR                                         : return "(0x39) A network adapter hardware error occurred.";
			case ERROR_BAD_NET_RESP                                         : return "(0x3A) The specified server cannot perform the requested operation.";
			case ERROR_UNEXP_NET_ERR                                        : return "(0x3B) An unexpected network error occurred.";
			case ERROR_BAD_REM_ADAP                                         : return "(0x3C) The remote adapter is not compatible.";
			case ERROR_PRINTQ_FULL                                          : return "(0x3D) The printer queue is full.";
			case ERROR_NO_SPOOL_SPACE                                       : return "(0x3E) Space to store the file waiting to be printed is not available on the server.";
			case ERROR_PRINT_CANCELLED                                      : return "(0x3F) Your file waiting to be printed was deleted.";
			case ERROR_NETNAME_DELETED                                      : return "(0x40) The specified network name is no longer available.";
			case ERROR_NETWORK_ACCESS_DENIED                                : return "(0x41) Network access is denied.";
			case ERROR_BAD_DEV_TYPE                                         : return "(0x42) The network resource type is not correct.";
			case ERROR_BAD_NET_NAME                                         : return "(0x43) The network name cannot be found.";
			case ERROR_TOO_MANY_NAMES                                       : return "(0x44) The name limit for the local computer network adapter card was exceeded.";
			case ERROR_TOO_MANY_SESS                                        : return "(0x45) The network BIOS session limit was exceeded.";
			case ERROR_SHARING_PAUSED                                       : return "(0x46) The remote server has been paused or is in the process of being started.";
			case ERROR_REQ_NOT_ACCEP                                        : return "(0x47) No more connections can be made to this remote computer at this time because there are already as many connections as the computer can accept.";
			case ERROR_REDIR_PAUSED                                         : return "(0x48) The specified printer or disk device has been paused.";
			case ERROR_FILE_EXISTS                                          : return "(0x50) The file exists.";
			case ERROR_CANNOT_MAKE                                          : return "(0x52) The directory or file cannot be created.";
			case ERROR_FAIL_I24                                             : return "(0x53) Fail on INT 24.";
			case ERROR_OUT_OF_STRUCTURES                                    : return "(0x54) Storage to process this request is not available.";
			case ERROR_ALREADY_ASSIGNED                                     : return "(0x55) The local device name is already in use.";
			case ERROR_INVALID_PASSWORD                                     : return "(0x56) The specified network password is not correct.";
			case ERROR_INVALID_PARAMETER                                    : return "(0x57) The parameter is incorrect.";
			case ERROR_NET_WRITE_FAULT                                      : return "(0x58) A write fault occurred on the network.";
			case ERROR_NO_PROC_SLOTS                                        : return "(0x59) The system cannot start another process at this time.";
			case ERROR_TOO_MANY_SEMAPHORES                                  : return "(0x64) Cannot create another system semaphore.";
			case ERROR_EXCL_SEM_ALREADY_OWNED                               : return "(0x65) The exclusive semaphore is owned by another process.";
			case ERROR_SEM_IS_SET                                           : return "(0x66) The semaphore is set and cannot be closed.";
			case ERROR_TOO_MANY_SEM_REQUESTS                                : return "(0x67) The semaphore cannot be set again.";
			case ERROR_INVALID_AT_INTERRUPT_TIME                            : return "(0x68) Cannot request exclusive semaphores at interrupt time.";
			case ERROR_SEM_OWNER_DIED                                       : return "(0x69) The previous ownership of this semaphore has ended.";
			case ERROR_SEM_USER_LIMIT                                       : return "(0x6A) Insert the diskette for drive %1.";
			case ERROR_DISK_CHANGE                                          : return "(0x6B) The program stopped because an alternate diskette was not inserted.";
			case ERROR_DRIVE_LOCKED                                         : return "(0x6C) The disk is in use or locked by another process.";
			case ERROR_BROKEN_PIPE                                          : return "(0x6D) The pipe has been ended.";
			case ERROR_OPEN_FAILED                                          : return "(0x6E) The system cannot open the device or file specified.";
			case ERROR_BUFFER_OVERFLOW                                      : return "(0x6F) The file name is too long.";
			case ERROR_DISK_FULL                                            : return "(0x70) There is not enough space on the disk.";
			case ERROR_NO_MORE_SEARCH_HANDLES                               : return "(0x71) No more internal file identifiers available.";
			case ERROR_INVALID_TARGET_HANDLE                                : return "(0x72) The target internal file identifier is incorrect.";
			case ERROR_INVALID_CATEGORY                                     : return "(0x75) The IOCTL call made by the application program is not correct.";
			case ERROR_INVALID_VERIFY_SWITCH                                : return "(0x76) The verify-on-write switch parameter value is not correct.";
			case ERROR_BAD_DRIVER_LEVEL                                     : return "(0x77) The system does not support the command requested.";
			case ERROR_CALL_NOT_IMPLEMENTED                                 : return "(0x78) This function is not supported on this system.";
			case ERROR_SEM_TIMEOUT                                          : return "(0x79) The semaphore timeout period has expired.";
			case ERROR_INSUFFICIENT_BUFFER                                  : return "(0x7A) The data area passed to a system call is too small.";
			case ERROR_INVALID_NAME                                         : return "(0x7B) The filename, directory name, or volume label syntax is incorrect.";
			case ERROR_INVALID_LEVEL                                        : return "(0x7C) The system call level is not correct.";
			case ERROR_NO_VOLUME_LABEL                                      : return "(0x7D) The disk has no volume label.";
			case ERROR_MOD_NOT_FOUND                                        : return "(0x7E) The specified module could not be found.";
			case ERROR_PROC_NOT_FOUND                                       : return "(0x7F) The specified procedure could not be found.";
			case ERROR_WAIT_NO_CHILDREN                                     : return "(0x80) There are no child processes to wait for.";
			case ERROR_CHILD_NOT_COMPLETE                                   : return "(0x81) The %1 application cannot be run in Win32 mode.";
			case ERROR_DIRECT_ACCESS_HANDLE                                 : return "(0x82) Attempt to use a file handle to an open disk partition for an operation other than raw disk I/O.";
			case ERROR_NEGATIVE_SEEK                                        : return "(0x83) An attempt was made to move the file pointer before the beginning of the file.";
			case ERROR_SEEK_ON_DEVICE                                       : return "(0x84) The file pointer cannot be set on the specified device or file.";
			case ERROR_IS_JOIN_TARGET                                       : return "(0x85) A JOIN or SUBST command cannot be used for a drive that contains previously joined drives.";
			case ERROR_IS_JOINED                                            : return "(0x86) An attempt was made to use a JOIN or SUBST command on a drive that has already been joined.";
			case ERROR_IS_SUBSTED                                           : return "(0x87) An attempt was made to use a JOIN or SUBST command on a drive that has already been substituted.";
			case ERROR_NOT_JOINED                                           : return "(0x88) The system tried to delete the JOIN of a drive that is not joined.";
			case ERROR_NOT_SUBSTED                                          : return "(0x89) The system tried to delete the substitution of a drive that is not substituted.";
			case ERROR_JOIN_TO_JOIN                                         : return "(0x8A) The system tried to join a drive to a directory on a joined drive.";
			case ERROR_SUBST_TO_SUBST                                       : return "(0x8B) The system tried to substitute a drive to a directory on a substituted drive.";
			case ERROR_JOIN_TO_SUBST                                        : return "(0x8C) The system tried to join a drive to a directory on a substituted drive.";
			case ERROR_SUBST_TO_JOIN                                        : return "(0x8D) The system tried to SUBST a drive to a directory on a joined drive.";
			case ERROR_BUSY_DRIVE                                           : return "(0x8E) The system cannot perform a JOIN or SUBST at this time.";
			case ERROR_SAME_DRIVE                                           : return "(0x8F) The system cannot join or substitute a drive to or for a directory on the same drive.";
			case ERROR_DIR_NOT_ROOT                                         : return "(0x90) The directory is not a subdirectory of the root directory.";
			case ERROR_DIR_NOT_EMPTY                                        : return "(0x91) The directory is not empty.";
			case ERROR_IS_SUBST_PATH                                        : return "(0x92) The path specified is being used in a substitute.";
			case ERROR_IS_JOIN_PATH                                         : return "(0x93) Not enough resources are available to process this command.";
			case ERROR_PATH_BUSY                                            : return "(0x94) The path specified cannot be used at this time.";
			case ERROR_IS_SUBST_TARGET                                      : return "(0x95) An attempt was made to join or substitute a drive for which a directory on the drive is the target of a previous substitute.";
			case ERROR_SYSTEM_TRACE                                         : return "(0x96) System trace information was not specified in your CONFIG.SYS file, or tracing is disallowed.";
			case ERROR_INVALID_EVENT_COUNT                                  : return "(0x97) The number of specified semaphore events for DosMuxSemWait is not correct.";
			case ERROR_TOO_MANY_MUXWAITERS                                  : return "(0x98) DosMuxSemWait did not execute; too many semaphores are already set.";
			case ERROR_INVALID_LIST_FORMAT                                  : return "(0x99) The DosMuxSemWait list is not correct.";
			case ERROR_LABEL_TOO_LONG                                       : return "(0x9A) The volume label you entered exceeds the label character limit of the target file system.";
			case ERROR_TOO_MANY_TCBS                                        : return "(0x9B) Cannot create another thread.";
			case ERROR_SIGNAL_REFUSED                                       : return "(0x9C) The recipient process has refused the signal.";
			case ERROR_DISCARDED                                            : return "(0x9D) The segment is already discarded and cannot be locked.";
			case ERROR_NOT_LOCKED                                           : return "(0x9E) The segment is already unlocked.";
			case ERROR_BAD_THREADID_ADDR                                    : return "(0x9F) The address for the thread ID is not correct.";
			case ERROR_BAD_ARGUMENTS                                        : return "(0xA0) One or more arguments are not correct.";
			case ERROR_BAD_PATHNAME                                         : return "(0xA1) The specified path is invalid.";
			case ERROR_SIGNAL_PENDING                                       : return "(0xA2) A signal is already pending.";
			case ERROR_MAX_THRDS_REACHED                                    : return "(0xA4) No more threads can be created in the system.";
			case ERROR_LOCK_FAILED                                          : return "(0xA7) Unable to lock a region of a file.";
			case ERROR_BUSY                                                 : return "(0xAA) The requested resource is in use.";
			case ERROR_DEVICE_SUPPORT_IN_PROGRESS                           : return "(0xAB) Device's command support detection is in progress.";
			case ERROR_CANCEL_VIOLATION                                     : return "(0xAD) A lock request was not outstanding for the supplied cancel region.";
			case ERROR_ATOMIC_LOCKS_NOT_SUPPORTED                           : return "(0xAE) The file system does not support atomic changes to the lock type.";
			case ERROR_INVALID_SEGMENT_NUMBER                               : return "(0xB4) The system detected a segment number that was not correct.";
			case ERROR_INVALID_ORDINAL                                      : return "(0xB6) The operating system cannot run %1.";
			case ERROR_ALREADY_EXISTS                                       : return "(0xB7) Cannot create a file when that file already exists.";
			case ERROR_INVALID_FLAG_NUMBER                                  : return "(0xBA) The flag passed is not correct.";
			case ERROR_SEM_NOT_FOUND                                        : return "(0xBB) The specified system semaphore name was not found.";
			case ERROR_INVALID_STARTING_CODESEG                             : return "(0xBC) The operating system cannot run %1.";
			case ERROR_INVALID_STACKSEG                                     : return "(0xBD) The operating system cannot run %1.";
			case ERROR_INVALID_MODULETYPE                                   : return "(0xBE) The operating system cannot run %1.";
			case ERROR_INVALID_EXE_SIGNATURE                                : return "(0xBF) Cannot run %1 in Win32 mode.";
			case ERROR_EXE_MARKED_INVALID                                   : return "(0xC0) The operating system cannot run %1.";
			case ERROR_BAD_EXE_FORMAT                                       : return "(0xC1) %1 is not a valid Win32 application.";
			case ERROR_ITERATED_DATA_EXCEEDS_64k                            : return "(0xC2) The operating system cannot run %1.";
			case ERROR_INVALID_MINALLOCSIZE                                 : return "(0xC3) The operating system cannot run %1.";
			case ERROR_DYNLINK_FROM_INVALID_RING                            : return "(0xC4) The operating system cannot run this application program.";
			case ERROR_IOPL_NOT_ENABLED                                     : return "(0xC5) The operating system is not presently configured to run this application.";
			case ERROR_INVALID_SEGDPL                                       : return "(0xC6) The operating system cannot run %1.";
			case ERROR_AUTODATASEG_EXCEEDS_64k                              : return "(0xC7) The operating system cannot run this application program.";
			case ERROR_RING2SEG_MUST_BE_MOVABLE                             : return "(0xC8) The code segment cannot be greater than or equal to 64K.";
			case ERROR_RELOC_CHAIN_XEEDS_SEGLIM                             : return "(0xC9) The operating system cannot run %1.";
			case ERROR_INFLOOP_IN_RELOC_CHAIN                               : return "(0xCA) The operating system cannot run %1.";
			case ERROR_ENVVAR_NOT_FOUND                                     : return "(0xCB) The system could not find the environment option that was entered.";
			case ERROR_NO_SIGNAL_SENT                                       : return "(0xCD) No process in the command subtree has a signal handler.";
			case ERROR_FILENAME_EXCED_RANGE                                 : return "(0xCE) The filename or extension is too long.";
			case ERROR_RING2_STACK_IN_USE                                   : return "(0xCF) The ring 2 stack is in use.";
			case ERROR_META_EXPANSION_TOO_LONG                              : return "(0xD0) The global filename characters, * or ?, are entered incorrectly or too many global filename characters are specified.";
			case ERROR_INVALID_SIGNAL_NUMBER                                : return "(0xD1) The signal being posted is not correct.";
			case ERROR_THREAD_1_INACTIVE                                    : return "(0xD2) The signal handler cannot be set.";
			case ERROR_LOCKED                                               : return "(0xD4) The segment is locked and cannot be reallocated.";
			case ERROR_TOO_MANY_MODULES                                     : return "(0xD6) Too many dynamic-link modules are attached to this program or dynamic-link module.";
			case ERROR_NESTING_NOT_ALLOWED                                  : return "(0xD7) Cannot nest calls to LoadModule.";
			case ERROR_EXE_MACHINE_TYPE_MISMATCH                            : return "(0xD8) This version of %1 is not compatible with the version of Windows you're running. Check your computer's system information and then contact the software publisher.";
			case ERROR_EXE_CANNOT_MODIFY_SIGNED_BINARY                      : return "(0xD9) The image file %1 is signed, unable to modify.";
			case ERROR_EXE_CANNOT_MODIFY_STRONG_SIGNED_BINARY               : return "(0xDA) The image file %1 is strong signed, unable to modify.";
			case ERROR_FILE_CHECKED_OUT                                     : return "(0xDC) This file is checked out or locked for editing by another user.";
			case ERROR_CHECKOUT_REQUIRED                                    : return "(0xDD) The file must be checked out before saving changes.";
			case ERROR_BAD_FILE_TYPE                                        : return "(0xDE) The file type being saved or retrieved has been blocked.";
			case ERROR_FILE_TOO_LARGE                                       : return "(0xDF) The file size exceeds the limit allowed and cannot be saved.";
			case ERROR_FORMS_AUTH_REQUIRED                                  : return "(0xE0) Access Denied. Before opening files in this location, you must first add the web site to your trusted sites list, browse to the web site, and select the option to login automatically.";
			case ERROR_VIRUS_INFECTED                                       : return "(0xE1) Operation did not complete successfully because the file contains a virus or potentially unwanted software.";
			case ERROR_VIRUS_DELETED                                        : return "(0xE2) This file contains a virus or potentially unwanted software and cannot be opened. Due to the nature of this virus or potentially unwanted software, the file has been removed from this location.";
			case ERROR_PIPE_LOCAL                                           : return "(0xE5) The pipe is local.";
			case ERROR_BAD_PIPE                                             : return "(0xE6) The pipe state is invalid.";
			case ERROR_PIPE_BUSY                                            : return "(0xE7) All pipe instances are busy.";
			case ERROR_NO_DATA                                              : return "(0xE8) The pipe is being closed.";
			case ERROR_PIPE_NOT_CONNECTED                                   : return "(0xE9) No process is on the other end of the pipe.";
			case ERROR_MORE_DATA                                            : return "(0xEA) More data is available.";
			case ERROR_VC_DISCONNECTED                                      : return "(0xF0) The session was canceled.";
			case ERROR_INVALID_EA_NAME                                      : return "(0xFE) The specified extended attribute name was invalid.";
			case ERROR_EA_LIST_INCONSISTENT                                 : return "(0xFF) The extended attributes are inconsistent.";
			case WAIT_TIMEOUT                                               : return "(0x102) The wait operation timed out.";
			case ERROR_NO_MORE_ITEMS                                        : return "(0x103) No more data is available.";
			case ERROR_CANNOT_COPY                                          : return "(0x10A) The copy functions cannot be used.";
			case ERROR_DIRECTORY                                            : return "(0x10B) The directory name is invalid.";
			case ERROR_EAS_DIDNT_FIT                                        : return "(0x113) The extended attributes did not fit in the buffer.";
			case ERROR_EA_FILE_CORRUPT                                      : return "(0x114) The extended attribute file on the mounted file system is corrupt.";
			case ERROR_EA_TABLE_FULL                                        : return "(0x115) The extended attribute table file is full.";
			case ERROR_INVALID_EA_HANDLE                                    : return "(0x116) The specified extended attribute handle is invalid.";
			case ERROR_EAS_NOT_SUPPORTED                                    : return "(0x11A) The mounted file system does not support extended attributes.";
			case ERROR_NOT_OWNER                                            : return "(0x120) Attempt to release mutex not owned by caller.";
			case ERROR_TOO_MANY_POSTS                                       : return "(0x12A) Too many posts were made to a semaphore.";
			case ERROR_PARTIAL_COPY                                         : return "(0x12B) Only part of a ReadProcessMemory or WriteProcessMemory request was completed.";
			case ERROR_OPLOCK_NOT_GRANTED                                   : return "(0x12C) The oplock request is denied.";
			case ERROR_INVALID_OPLOCK_PROTOCOL                              : return "(0x12D) An invalid oplock acknowledgment was received by the system.";
			case ERROR_DISK_TOO_FRAGMENTED                                  : return "(0x12E) The volume is too fragmented to complete this operation.";
			case ERROR_DELETE_PENDING                                       : return "(0x12F) The file cannot be opened because it is in the process of being deleted.";
			case ERROR_INCOMPATIBLE_WITH_GLOBAL_SHORT_NAME_REGISTRY_SETTING : return "(0x130) Short name settings may not be changed on this volume due to the global registry setting.";
			case ERROR_SHORT_NAMES_NOT_ENABLED_ON_VOLUME                    : return "(0x131) Short names are not enabled on this volume.";
			case ERROR_SECURITY_STREAM_IS_INCONSISTENT                      : return "(0x132) The security stream for the given volume is in an inconsistent state. Please run CHKDSK on the volume.";
			case ERROR_INVALID_LOCK_RANGE                                   : return "(0x133) A requested file lock operation cannot be processed due to an invalid byte range.";
			case ERROR_IMAGE_SUBSYSTEM_NOT_PRESENT                          : return "(0x134) The subsystem needed to support the image type is not present.";
			case ERROR_NOTIFICATION_GUID_ALREADY_DEFINED                    : return "(0x135) The specified file already has a notification GUID associated with it.";
			case ERROR_INVALID_EXCEPTION_HANDLER                            : return "(0x136) An invalid exception handler routine has been detected.";
			case ERROR_DUPLICATE_PRIVILEGES                                 : return "(0x137) Duplicate privileges were specified for the token.";
			case ERROR_NO_RANGES_PROCESSED                                  : return "(0x138) No ranges for the specified operation were able to be processed.";
			case ERROR_NOT_ALLOWED_ON_SYSTEM_FILE                           : return "(0x139) Operation is not allowed on a file system internal file.";
			case ERROR_DISK_RESOURCES_EXHAUSTED                             : return "(0x13A) The physical resources of this disk have been exhausted.";
			case ERROR_INVALID_TOKEN                                        : return "(0x13B) The token representing the data is invalid.";
			case ERROR_DEVICE_FEATURE_NOT_SUPPORTED                         : return "(0x13C) The device does not support the command feature.";
			case ERROR_MR_MID_NOT_FOUND                                     : return "(0x13D) The system cannot find message text for message number 0x%1 in the message file for %2.";
			case ERROR_SCOPE_NOT_FOUND                                      : return "(0x13E) The scope specified was not found.";
			case ERROR_UNDEFINED_SCOPE                                      : return "(0x13F) The Central Access Policy specified is not defined on the target machine.";
			case ERROR_INVALID_CAP                                          : return "(0x140) The Central Access Policy obtained from Active Directory is invalid.";
			case ERROR_DEVICE_UNREACHABLE                                   : return "(0x141) The device is unreachable.";
			case ERROR_DEVICE_NO_RESOURCES                                  : return "(0x142) The target device has insufficient resources to complete the operation.";
			case ERROR_DATA_CHECKSUM_ERROR                                  : return "(0x143) A data integrity checksum error occurred. Data in the file stream is corrupt.";
			case ERROR_INTERMIXED_KERNEL_EA_OPERATION                       : return "(0x144) An attempt was made to modify both a KERNEL and normal Extended Attribute (EA) in the same operation.";
			case ERROR_FILE_LEVEL_TRIM_NOT_SUPPORTED                        : return "(0x146) Device does not support file-level TRIM.";
			case ERROR_OFFSET_ALIGNMENT_VIOLATION                           : return "(0x147) The command specified a data offset that does not align to the device's granularity/alignment.";
			case ERROR_INVALID_FIELD_IN_PARAMETER_LIST                      : return "(0x148) The command specified an invalid field in its parameter list.";
			case ERROR_OPERATION_IN_PROGRESS                                : return "(0x149) An operation is currently in progress with the device.";
			case ERROR_BAD_DEVICE_PATH                                      : return "(0x14A) An attempt was made to send down the command via an invalid path to the target device.";
			case ERROR_TOO_MANY_DESCRIPTORS                                 : return "(0x14B) The command specified a number of descriptors that exceeded the maximum supported by the device.";
			case ERROR_SCRUB_DATA_DISABLED                                  : return "(0x14C) Scrub is disabled on the specified file.";
			case ERROR_NOT_REDUNDANT_STORAGE                                : return "(0x14D) The storage device does not provide redundancy.";
			case ERROR_RESIDENT_FILE_NOT_SUPPORTED                          : return "(0x14E) An operation is not supported on a resident file.";
			case ERROR_COMPRESSED_FILE_NOT_SUPPORTED                        : return "(0x14F) An operation is not supported on a compressed file.";
			case ERROR_DIRECTORY_NOT_SUPPORTED                              : return "(0x150) An operation is not supported on a directory.";
			case ERROR_NOT_READ_FROM_COPY                                   : return "(0x151) The specified copy of the requested data could not be read.";
			case ERROR_FAIL_NOACTION_REBOOT                                 : return "(0x15E) No action was taken as a system reboot is required.";
			case ERROR_FAIL_SHUTDOWN                                        : return "(0x15F) The shutdown operation failed.";
			case ERROR_FAIL_RESTART                                         : return "(0x160) The restart operation failed.";
			case ERROR_MAX_SESSIONS_REACHED                                 : return "(0x161) The maximum number of sessions has been reached.";
			case ERROR_THREAD_MODE_ALREADY_BACKGROUND                       : return "(0x190) The thread is already in background processing mode.";
			case ERROR_THREAD_MODE_NOT_BACKGROUND                           : return "(0x191) The thread is not in background processing mode.";
			case ERROR_PROCESS_MODE_ALREADY_BACKGROUND                      : return "(0x192) The process is already in background processing mode.";
			case ERROR_PROCESS_MODE_NOT_BACKGROUND                          : return "(0x193) The process is not in background processing mode.";
			case ERROR_INVALID_ADDRESS                                      : return "(0x1E7) Attempt to access invalid address.";
			}
		}

		/// <summary>Returns the last error set by the last called native function with the "SetLastError" attribute</summary>
		public static uint GetLastError()
		{
			return unchecked((uint)Marshal.GetLastWin32Error());
		}
		public static string GetLastErrorString()
		{
			return new Win32Exception(Marshal.GetLastWin32Error()).Message;
		}
	}
}
