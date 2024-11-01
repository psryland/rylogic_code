using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Text;

namespace Rylogic.Interop.Win32
{
	#region COM Interfaces

	// Disable warning CS0108: 'x' hides inherited member 'y'. Use the new keyword if hiding was intended.
	#pragma warning disable 0108

	[ComImport(), Guid(IIDGuid.IModalWindow), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IModalWindow
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), PreserveSig]
		int Show([In] IntPtr parent);
	}

	[ComImport(), Guid(IIDGuid.IFileDialog), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IFileDialog :IModalWindow
	{
		// Defined on IModalWindow - repeated here due to requirements of COM interop layer
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), PreserveSig]
		int Show([In] IntPtr parent);

		// IFileDialog-Specific interface members
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetFileTypes([In] uint cFileTypes, [In, MarshalAs(UnmanagedType.LPArray)] Win32.COMDLG_FILTERSPEC[] rgFilterSpec);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetFileTypeIndex([In] uint iFileType);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetFileTypeIndex(out uint piFileType);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void Advise([In, MarshalAs(UnmanagedType.Interface)] IFileDialogEvents pfde, out uint pdwCookie);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void Unadvise([In] uint dwCookie);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetOptions([In] Win32.FOS fos);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetOptions(out Win32.FOS pfos);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetDefaultFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetFolder([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetCurrentSelection([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetFileName([In, MarshalAs(UnmanagedType.LPWStr)] string pszName);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetTitle([In, MarshalAs(UnmanagedType.LPWStr)] string pszTitle);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetOkButtonLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszText);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetFileNameLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszLabel);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetResult([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void AddPlace([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, Win32.FDAP fdap);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetDefaultExtension([In, MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void Close([MarshalAs(UnmanagedType.Error)] int hr);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetClientGuid([In] ref Guid guid);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void ClearClientData();

		// Not supported:  IShellItemFilter is not defined, converting to IntPtr
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetFilter([MarshalAs(UnmanagedType.Interface)] IntPtr pFilter);
	}

	[ComImport(), Guid(IIDGuid.IFileOpenDialog), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IFileOpenDialog :IFileDialog
	{
		// Defined on IModalWindow - repeated here due to requirements of COM interop layer
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), PreserveSig]
		int Show([In] IntPtr parent);

		// Defined on IFileDialog - repeated here due to requirements of COM interop layer
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetFileTypes([In] uint cFileTypes, [In] ref Win32.COMDLG_FILTERSPEC rgFilterSpec);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetFileTypeIndex([In] uint iFileType);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetFileTypeIndex(out uint piFileType);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void Advise([In, MarshalAs(UnmanagedType.Interface)] IFileDialogEvents pfde, out uint pdwCookie);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void Unadvise([In] uint dwCookie);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetOptions([In] Win32.FOS fos);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetOptions(out Win32.FOS pfos);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetDefaultFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetFolder([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetCurrentSelection([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetFileName([In, MarshalAs(UnmanagedType.LPWStr)] string pszName);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetTitle([In, MarshalAs(UnmanagedType.LPWStr)] string pszTitle);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetOkButtonLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszText);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetFileNameLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszLabel);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetResult([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void AddPlace([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, Win32.FDAP fdap);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetDefaultExtension([In, MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void Close([MarshalAs(UnmanagedType.Error)] int hr);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetClientGuid([In] ref Guid guid);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void ClearClientData();

		// Not supported:  IShellItemFilter is not defined, converting to IntPtr
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetFilter([MarshalAs(UnmanagedType.Interface)] IntPtr pFilter);

		// Defined by IFileOpenDialog
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetResults([MarshalAs(UnmanagedType.Interface)] out IShellItemArray ppenum);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetSelectedItems([MarshalAs(UnmanagedType.Interface)] out IShellItemArray ppsai);
	}

	[ComImport(), Guid(IIDGuid.IFileSaveDialog), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IFileSaveDialog : IFileDialog
	{
		// Defined on IModalWindow - repeated here due to requirements of COM interop layer
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), PreserveSig]
		int Show([In] IntPtr parent);

		// Defined on IFileDialog - repeated here due to requirements of COM interop layer
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetFileTypes([In] uint cFileTypes, [In] ref Win32.COMDLG_FILTERSPEC rgFilterSpec);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetFileTypeIndex([In] uint iFileType);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetFileTypeIndex(out uint piFileType);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void Advise([In, MarshalAs(UnmanagedType.Interface)] IFileDialogEvents pfde, out uint pdwCookie);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void Unadvise([In] uint dwCookie);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetOptions([In] Win32.FOS fos);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetOptions(out Win32.FOS pfos);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetDefaultFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetFolder([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetCurrentSelection([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetFileName([In, MarshalAs(UnmanagedType.LPWStr)] string pszName);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetTitle([In, MarshalAs(UnmanagedType.LPWStr)] string pszTitle);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetOkButtonLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszText);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetFileNameLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszLabel);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetResult([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void AddPlace([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, Win32.FDAP fdap);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetDefaultExtension([In, MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void Close([MarshalAs(UnmanagedType.Error)] int hr);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetClientGuid([In] ref Guid guid);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void ClearClientData();

		// Not supported:  IShellItemFilter is not defined, converting to IntPtr
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetFilter([MarshalAs(UnmanagedType.Interface)] IntPtr pFilter);

		// Defined by IFileSaveDialog interface
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetSaveAsItem([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);

		// Not currently supported: IPropertyStore
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetProperties([In, MarshalAs(UnmanagedType.Interface)] IntPtr pStore);

		// Not currently supported: IPropertyDescriptionList
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetCollectedProperties([In, MarshalAs(UnmanagedType.Interface)] IntPtr pList, [In] int fAppendDefault);

		// Not currently supported: IPropertyStore
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetProperties([MarshalAs(UnmanagedType.Interface)] out IntPtr ppStore);

		// Not currently supported: IPropertyStore, IFileOperationProgressSink
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void ApplyProperties([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, [In, MarshalAs(UnmanagedType.Interface)] IntPtr pStore, [In, ComAliasName("Interop.wireHWND")] ref IntPtr hwnd, [In, MarshalAs(UnmanagedType.Interface)] IntPtr pSink);
	}

	[ComImport, Guid(IIDGuid.IFileDialogEvents), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IFileDialogEvents
	{
		// NOTE: some of these callbacks can be cancelled - returning S_FALSE means that 
		// the dialog should not proceed (e.g. with closing, changing folder); to 
		// support this, we need to use the PreserveSig attribute to enable us to return
		// the proper HRESULT
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), PreserveSig]
		Win32.HRESULT OnFileOk([In, MarshalAs(UnmanagedType.Interface)] IFileDialog pfd);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), PreserveSig]
		Win32.HRESULT OnFolderChanging([In, MarshalAs(UnmanagedType.Interface)] IFileDialog pfd, [In, MarshalAs(UnmanagedType.Interface)] IShellItem psiFolder);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void OnFolderChange([In, MarshalAs(UnmanagedType.Interface)] IFileDialog pfd);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void OnSelectionChange([In, MarshalAs(UnmanagedType.Interface)] IFileDialog pfd);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void OnShareViolation([In, MarshalAs(UnmanagedType.Interface)] IFileDialog pfd, [In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, out Win32.FDE_SHAREVIOLATION_RESPONSE pResponse);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void OnTypeChange([In, MarshalAs(UnmanagedType.Interface)] IFileDialog pfd);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void OnOverwrite([In, MarshalAs(UnmanagedType.Interface)] IFileDialog pfd, [In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, out Win32.FDE_OVERWRITE_RESPONSE pResponse);
	}

	[ComImport, Guid(IIDGuid.IShellItem), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IShellItem
	{
		// Not supported: IBindCtx
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void BindToHandler([In, MarshalAs(UnmanagedType.Interface)] IntPtr pbc, [In] ref Guid bhid, [In] ref Guid riid, out IntPtr ppv);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetParent([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetDisplayName([In] Win32.SIGDN sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetAttributes([In] uint sfgaoMask, out uint psfgaoAttribs);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void Compare([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, [In] uint hint, out int piOrder);
	}

	[ComImport, Guid(IIDGuid.IShellItemArray), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IShellItemArray
	{
		// Not supported: IBindCtx
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void BindToHandler([In, MarshalAs(UnmanagedType.Interface)] IntPtr pbc, [In] ref Guid rbhid, [In] ref Guid riid, out IntPtr ppvOut);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetPropertyStore([In] int Flags, [In] ref Guid riid, out IntPtr ppv);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetPropertyDescriptionList([In] ref Win32.PROPERTYKEY keyType, [In] ref Guid riid, out IntPtr ppv);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetAttributes([In] Win32.SIATTRIBFLAGS dwAttribFlags, [In] uint sfgaoMask, out uint psfgaoAttribs);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetCount(out uint pdwNumItems);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetItemAt([In] uint dwIndex, [MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

		// Not supported: IEnumShellItems (will use GetCount and GetItemAt instead)
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void EnumItems([MarshalAs(UnmanagedType.Interface)] out IntPtr ppenumShellItems);
	}

	[ComImport, Guid(IIDGuid.IKnownFolder), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IKnownFolder
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetId(out Guid pkfid);

		// Not yet supported - adding to fill slot in V-table
		//[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		//void GetCategory(out mbtagKF_CATEGORY pCategory);
		void spacer1();

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetShellItem([In] uint dwFlags, ref Guid riid, out IShellItem ppv);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetPath([In] uint dwFlags, [MarshalAs(UnmanagedType.LPWStr)] out string ppszPath);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetPath([In] uint dwFlags, [In, MarshalAs(UnmanagedType.LPWStr)] string pszPath);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetLocation([In] uint dwFlags, [Out, ComAliasName("Interop.wirePIDL")] IntPtr ppidl);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetFolderType(out Guid pftid);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetRedirectionCapabilities(out uint pCapabilities);

		// Not yet supported - adding to fill slot in V-table
		//[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		//void GetFolderDefinition(out tagKNOWNFOLDER_DEFINITION pKFD);
		void spacer2();
	}

	[ComImport, Guid(IIDGuid.IKnownFolderManager), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IKnownFolderManager
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void FolderIdFromCsidl([In] int nCsidl, out Guid pfid);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void FolderIdToCsidl([In] ref Guid rfid, out int pnCsidl);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetFolderIds([Out] IntPtr ppKFId, [In, Out] ref uint pCount);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetFolder([In] ref Guid rfid, [MarshalAs(UnmanagedType.Interface)] out IKnownFolder ppkf);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetFolderByName([In, MarshalAs(UnmanagedType.LPWStr)] string pszCanonicalName, [MarshalAs(UnmanagedType.Interface)] out IKnownFolder ppkf);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void RegisterFolder([In] ref Guid rfid, [In] ref Win32.KNOWNFOLDER_DEFINITION pKFD);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void UnregisterFolder([In] ref Guid rfid);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void FindFolderFromPath([In, MarshalAs(UnmanagedType.LPWStr)] string pszPath, [In] Win32.FFFP_MODE mode, [MarshalAs(UnmanagedType.Interface)] out IKnownFolder ppkf);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void FindFolderFromIDList([In] IntPtr pidl, [MarshalAs(UnmanagedType.Interface)] out IKnownFolder ppkf);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void Redirect([In] ref Guid rfid, [In] IntPtr hwnd, [In] uint Flags, [In, MarshalAs(UnmanagedType.LPWStr)] string pszTargetPath, [In] uint cFolders, [In] ref Guid pExclusion, [MarshalAs(UnmanagedType.LPWStr)] out string ppszError);
	}

	[ComImport, Guid(IIDGuid.IFileDialogCustomize), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IFileDialogCustomize
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void EnableOpenDropDown([In] int dwIDCtl);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void AddMenu([In] int dwIDCtl, [In, MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void AddPushButton([In] int dwIDCtl, [In, MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void AddComboBox([In] int dwIDCtl);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void AddRadioButtonList([In] int dwIDCtl);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void AddCheckButton([In] int dwIDCtl, [In, MarshalAs(UnmanagedType.LPWStr)] string pszLabel, [In] bool bChecked);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void AddEditBox([In] int dwIDCtl, [In, MarshalAs(UnmanagedType.LPWStr)] string pszText);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void AddSeparator([In] int dwIDCtl);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void AddText([In] int dwIDCtl, [In, MarshalAs(UnmanagedType.LPWStr)] string pszText);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetControlLabel([In] int dwIDCtl, [In, MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetControlState([In] int dwIDCtl, [Out] out Win32.CDCONTROLSTATE pdwState);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetControlState([In] int dwIDCtl, [In] Win32.CDCONTROLSTATE dwState);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetEditBoxText([In] int dwIDCtl, [Out] IntPtr ppszText);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetEditBoxText([In] int dwIDCtl, [In, MarshalAs(UnmanagedType.LPWStr)] string pszText);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetCheckButtonState([In] int dwIDCtl, [Out] out bool pbChecked);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetCheckButtonState([In] int dwIDCtl, [In] bool bChecked);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void AddControlItem([In] int dwIDCtl, [In] int dwIDItem, [In, MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void RemoveControlItem([In] int dwIDCtl, [In] int dwIDItem);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void RemoveAllControlItems([In] int dwIDCtl);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetControlItemState([In] int dwIDCtl, [In] int dwIDItem, [Out] out Win32.CDCONTROLSTATE pdwState);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetControlItemState([In] int dwIDCtl, [In] int dwIDItem, [In] Win32.CDCONTROLSTATE dwState);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetSelectedControlItem([In] int dwIDCtl, [Out] out int pdwIDItem);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetSelectedControlItem([In] int dwIDCtl, [In] int dwIDItem); // Not valid for OpenDropDown
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void StartVisualGroup([In] int dwIDCtl, [In, MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void EndVisualGroup();
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void MakeProminent([In] int dwIDCtl);
	}

	[ComImport, Guid(IIDGuid.IFileDialogControlEvents), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IFileDialogControlEvents
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void OnItemSelected([In, MarshalAs(UnmanagedType.Interface)] IFileDialogCustomize pfdc, [In] int dwIDCtl, [In] int dwIDItem);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void OnButtonClicked([In, MarshalAs(UnmanagedType.Interface)] IFileDialogCustomize pfdc, [In] int dwIDCtl);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void OnCheckButtonToggled([In, MarshalAs(UnmanagedType.Interface)] IFileDialogCustomize pfdc, [In] int dwIDCtl, [In] bool bChecked);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void OnControlActivating([In, MarshalAs(UnmanagedType.Interface)] IFileDialogCustomize pfdc, [In] int dwIDCtl);
	}

	[ComImport, Guid(IIDGuid.IPropertyStore), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IPropertyStore
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetCount([Out] out uint cProps);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetAt([In] uint iProp, out Win32.PROPERTYKEY pkey);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetValue([In] ref Win32.PROPERTYKEY key, out object pv);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetValue([In] ref Win32.PROPERTYKEY key, [In] ref object pv);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void Commit();
	}

	#endregion

	#region CoClasses

	// Dummy base interface for CommonFileDialog CoClasses'
	public interface NativeCommonFileDialog {}

	// CoClass interfaces - designed to "look like" the object 
	// in the API, so that the 'new' operator can be used in a 
	// straightforward way. Behind the scenes, the C# compiler
	// morphs all 'new CoClass()' calls to 'new CoClassWrapper()'
	[ComImport, Guid(IIDGuid.IFileOpenDialog), CoClass(typeof(FileOpenDialogRCW))]
	public interface NativeFileOpenDialog :IFileOpenDialog {}
	[ComImport, ClassInterface(ClassInterfaceType.None), TypeLibType(TypeLibTypeFlags.FCanCreate), Guid(CLSIDGuid.FileOpenDialog)]
	public class FileOpenDialogRCW {}

	[ComImport, Guid(IIDGuid.IFileSaveDialog), CoClass(typeof(FileSaveDialogRCW))]
	public interface NativeFileSaveDialog :IFileSaveDialog {}
	[ComImport,ClassInterface(ClassInterfaceType.None),TypeLibType(TypeLibTypeFlags.FCanCreate),Guid(CLSIDGuid.FileSaveDialog)]
	public class FileSaveDialogRCW {}

	[ComImport, Guid(IIDGuid.IKnownFolderManager), CoClass(typeof(KnownFolderManagerRCW))]
	public interface KnownFolderManager : IKnownFolderManager {}
	[ComImport, ClassInterface(ClassInterfaceType.None), TypeLibType(TypeLibTypeFlags.FCanCreate), Guid(CLSIDGuid.KnownFolderManager)]
	public class KnownFolderManagerRCW {}

	#endregion

	public static partial class Shell32
	{
		/// <summary>Creates and initializes a Shell item object from a parsing name.</summary>
		public static IShellItem CreateItemFromParsingName(string path)
		{
			var guid = new Guid(IIDGuid.IShellItem);
			var hr = SHCreateItemFromParsingName(path, IntPtr.Zero, ref guid, out var item);
			if (hr != 0) throw new Win32Exception(hr); // This throws if 'path' starts with '.'
			return (IShellItem)item;
		}

		[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
		public static extern void DragAcceptFiles(IntPtr hwnd, bool accept);

		[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
		public static extern uint DragQueryFile(IntPtr hDrop, uint iFile, [Out] StringBuilder? lpszFile, uint cch);

		[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
		private static extern int SHCreateItemFromParsingName([MarshalAs(UnmanagedType.LPWStr)] string pszPath, IntPtr pbc, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppv);

		[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
		public static extern int SHFileOperationW(ref Win32.SHFILEOPSTRUCTW32 FileOp); 

		[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
		public static extern int SHFileOperationW(ref Win32.SHFILEOPSTRUCTW64 FileOp);

		[DllImport("shell32.Dll", EntryPoint = "Shell_NotifyIcon", CharSet = CharSet.Unicode)]
		public static extern bool NotifyIcon(Win32.ENotifyCommand cmd, [In] ref Win32.NOTIFYICONDATA data);
	}
}
