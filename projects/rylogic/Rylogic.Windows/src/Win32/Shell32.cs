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
		void SetFileTypes([In] uint cFileTypes, [In, MarshalAs(UnmanagedType.LPArray)] Shell32.COMDLG_FILTERSPEC[] rgFilterSpec);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetFileTypeIndex([In] uint iFileType);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetFileTypeIndex(out uint piFileType);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void Advise([In, MarshalAs(UnmanagedType.Interface)] IFileDialogEvents pfde, out uint pdwCookie);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void Unadvise([In] uint dwCookie);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetOptions([In] Shell32.FOS fos);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetOptions(out Shell32.FOS pfos);

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
		void AddPlace([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, Shell32.FDAP fdap);

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
		void SetFileTypes([In] uint cFileTypes, [In] ref Shell32.COMDLG_FILTERSPEC rgFilterSpec);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetFileTypeIndex([In] uint iFileType);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetFileTypeIndex(out uint piFileType);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void Advise([In, MarshalAs(UnmanagedType.Interface)] IFileDialogEvents pfde, out uint pdwCookie);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void Unadvise([In] uint dwCookie);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetOptions([In] Shell32.FOS fos);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetOptions(out Shell32.FOS pfos);

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
		void AddPlace([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, Shell32.FDAP fdap);

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
		void SetFileTypes([In] uint cFileTypes, [In] ref Shell32.COMDLG_FILTERSPEC rgFilterSpec);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetFileTypeIndex([In] uint iFileType);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetFileTypeIndex(out uint piFileType);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void Advise([In, MarshalAs(UnmanagedType.Interface)] IFileDialogEvents pfde, out uint pdwCookie);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void Unadvise([In] uint dwCookie);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetOptions([In] Shell32.FOS fos);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void GetOptions(out Shell32.FOS pfos);

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
		void AddPlace([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, Shell32.FDAP fdap);

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
		// NOTE: some of these callbacks are cancelable - returning S_FALSE means that 
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
		void OnShareViolation([In, MarshalAs(UnmanagedType.Interface)] IFileDialog pfd, [In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, out Shell32.FDE_SHAREVIOLATION_RESPONSE pResponse);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void OnTypeChange([In, MarshalAs(UnmanagedType.Interface)] IFileDialog pfd);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void OnOverwrite([In, MarshalAs(UnmanagedType.Interface)] IFileDialog pfd, [In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, out Shell32.FDE_OVERWRITE_RESPONSE pResponse);
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
		void GetDisplayName([In] Shell32.SIGDN sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);

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
		void GetAttributes([In] Shell32.SIATTRIBFLAGS dwAttribFlags, [In] uint sfgaoMask, out uint psfgaoAttribs);

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

		// Not yet supported - adding to fill slot in vtable
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

		// Not yet supported - adding to fill slot in vtable
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
		void GetControlState([In] int dwIDCtl, [Out] out Shell32.CDCONTROLSTATE pdwState);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetControlState([In] int dwIDCtl, [In] Shell32.CDCONTROLSTATE dwState);
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
		void GetControlItemState([In] int dwIDCtl, [In] int dwIDItem, [Out] out Shell32.CDCONTROLSTATE pdwState);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetControlItemState([In] int dwIDCtl, [In] int dwIDItem, [In] Shell32.CDCONTROLSTATE dwState);
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

	// Dummy base interface for CommonFileDialog coclasses
	public interface NativeCommonFileDialog {}

	// Coclass interfaces - designed to "look like" the object 
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
		#pragma warning disable CA1401 // P/Invokes should not be visible

		#region File Operations Definitions

		// Shell File Operations
		public const int FO_MOVE   = 0x0001;
		public const int FO_COPY   = 0x0002;
		public const int FO_DELETE = 0x0003;
		public const int FO_RENAME = 0x0004;

		// SHFILEOPSTRUCT.fFlags and IFileOperation::SetOperationFlags() flag values
		public const int FOF_MULTIDESTFILES        = 0x0001;
		public const int FOF_CONFIRMMOUSE          = 0x0002;
		public const int FOF_SILENT                = 0x0004;  // don't display progress UI (confirm prompts may be displayed still)
		public const int FOF_RENAMEONCOLLISION     = 0x0008;  // automatically rename the source files to avoid the collisions
		public const int FOF_NOCONFIRMATION        = 0x0010;  // don't display confirmation UI, assume "yes" for cases that can be bypassed, "no" for those that can not
		public const int FOF_WANTMAPPINGHANDLE     = 0x0020;  // Fill in SHFILEOPSTRUCT.hNameMappings.  Must be freed using SHFreeNameMappings
		public const int FOF_ALLOWUNDO             = 0x0040;  // enable undo including Recycle behavior for IFileOperation::Delete()
		public const int FOF_FILESONLY             = 0x0080;  // only operate on the files (non folders), both files and folders are assumed without this
		public const int FOF_SIMPLEPROGRESS        = 0x0100;  // means don't show names of files
		public const int FOF_NOCONFIRMMKDIR        = 0x0200;  // don't dispplay confirmatino UI before making any needed directories, assume "Yes" in these cases
		public const int FOF_NOERRORUI             = 0x0400;  // don't put up error UI, other UI may be displayed, progress, confirmations
		public const int FOF_NOCOPYSECURITYATTRIBS = 0x0800;  // dont copy file security attributes (ACLs)
		public const int FOF_NORECURSION           = 0x1000;  // don't recurse into directories for operations that would recurse
		public const int FOF_NO_CONNECTED_ELEMENTS = 0x2000;  // don't operate on connected elements ("xxx_files" folders that go with .htm files)
		public const int FOF_WANTNUKEWARNING       = 0x4000;  // during delete operation, warn if object is being permanently destroyed instead of recycling (partially overrides FOF_NOCONFIRMATION)
		public const int FOF_NORECURSEREPARSE      = 0x8000; // deprecated; the operations engine always does the right thing on FolderLink objects (symlinks, reparse points, folder shortcuts)
		public const int FOF_NO_UI                 = (FOF_SILENT | FOF_NOCONFIRMATION | FOF_NOERRORUI | FOF_NOCONFIRMMKDIR); // don't display any UI at all

		public enum FDAP
		{
			BOTTOM = 0x00000000,
			TOP    = 0x00000001,
		}

		public enum FDE_SHAREVIOLATION_RESPONSE
		{
			FDESVR_DEFAULT = 0x00000000,
			FDESVR_ACCEPT  = 0x00000001,
			FDESVR_REFUSE  = 0x00000002
		}

		public  enum FDE_OVERWRITE_RESPONSE
		{
			FDEOR_DEFAULT = 0x00000000,
			FDEOR_ACCEPT  = 0x00000001,
			FDEOR_REFUSE  = 0x00000002
		}

		public  enum SIATTRIBFLAGS
		{
			AND       = 0x00000001 , // if multiple items and the attirbutes together.
			OR        = 0x00000002 , // if multiple items or the attributes together.
			APPCOMPAT = 0x00000003 , // Call GetAttributes directly on the ShellFolder for multiple attributes
		}

		public  enum SIGDN :uint
		{
			NORMALDISPLAY               = 0x00000000, // SHGDN_NORMAL
			PARENTRELATIVEPARSING       = 0x80018001, // SHGDN_INFOLDER | SHGDN_FORPARSING
			DESKTOPABSOLUTEPARSING      = 0x80028000, // SHGDN_FORPARSING
			PARENTRELATIVEEDITING       = 0x80031001, // SHGDN_INFOLDER | SHGDN_FOREDITING
			DESKTOPABSOLUTEEDITING      = 0x8004c000, // SHGDN_FORPARSING | SHGDN_FORADDRESSBAR
			FILESYSPATH                 = 0x80058000, // SHGDN_FORPARSING
			URL                         = 0x80068000, // SHGDN_FORPARSING
			PARENTRELATIVEFORADDRESSBAR = 0x8007c001, // SHGDN_INFOLDER | SHGDN_FORPARSING | SHGDN_FORADDRESSBAR
			PARENTRELATIVE              = 0x80080001  // SHGDN_INFOLDER
		}

		[Flags] public enum FOS :uint
		{
			OVERWRITEPROMPT    = 0x00000002,
			STRICTFILETYPES    = 0x00000004,
			NOCHANGEDIR        = 0x00000008,
			PICKFOLDERS        = 0x00000020,
			FORCEFILESYSTEM    = 0x00000040, // Ensure that items returned are filesystem items.
			ALLNONSTORAGEITEMS = 0x00000080, // Allow choosing items that have no storage.
			NOVALIDATE         = 0x00000100,
			ALLOWMULTISELECT   = 0x00000200,
			PATHMUSTEXIST      = 0x00000800,
			FILEMUSTEXIST      = 0x00001000,
			CREATEPROMPT       = 0x00002000,
			SHAREAWARE         = 0x00004000,
			NOREADONLYRETURN   = 0x00008000,
			NOTESTFILECREATE   = 0x00010000,
			HIDEMRUPLACES      = 0x00020000,
			HIDEPINNEDPLACES   = 0x00040000,
			NODEREFERENCELINKS = 0x00100000,
			DONTADDTORECENT    = 0x02000000,
			FORCESHOWHIDDEN    = 0x10000000,
			DEFAULTNOMINIMODE  = 0x20000000
		}

		public enum CDCONTROLSTATE
		{
			CDCS_INACTIVE = 0x00000000,
			CDCS_ENABLED  = 0x00000001,
			CDCS_VISIBLE  = 0x00000002
		}

		#endregion

		#region Interop Structures
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
		public struct SHFILEOPSTRUCTW32
		{
			public IntPtr hwnd;
			public UInt32 wFunc;
			public IntPtr pFrom; // Must be a double null terminated string, can't use strings because interop drops the double null
			public IntPtr pTo;   // Must be a double null terminated string, can't use strings because interop drops the double null
			public UInt16 fFlags;
			public Int32 fAnyOperationsAborted;
			public IntPtr hNameMappings;
			[MarshalAs(UnmanagedType.LPWStr)] public string lpszProgressTitle;
		}
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 8)]
		public struct SHFILEOPSTRUCTW64
		{
			public IntPtr hwnd;
			public UInt32 wFunc;
			public IntPtr pFrom; // Must be a double null terminated string, can't use strings because interop drops the double null
			public IntPtr pTo;   // Must be a double null terminated string, can't use strings because interop drops the double null
			public UInt16 fFlags;
			public Int32 fAnyOperationsAborted;
			public IntPtr hNameMappings;
			[MarshalAs(UnmanagedType.LPWStr)] public string lpszProgressTitle;
		}
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
		public struct COMDLG_FILTERSPEC
		{
			[MarshalAs(UnmanagedType.LPWStr)] public string pszName;
			[MarshalAs(UnmanagedType.LPWStr)] public string pszSpec;
		}
		#endregion

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
		public static extern int SHFileOperationW(ref SHFILEOPSTRUCTW32 FileOp); 

		[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
		public static extern int SHFileOperationW(ref SHFILEOPSTRUCTW64 FileOp);

		#region Notification Icon Enums
		public enum ENotifyIconVersion
		{
			/// <summary>Default behavior (legacy Win95). Expects a 'NotifyIconData' size of 488.</summary>
			Win95 = 0x0,

			/// <summary>Behavior representing Win2000 an higher. Expects a 'NotifyIconData' size of 504.</summary>
			Win2000 = 0x3,

			/// <summary>
			/// Extended tooltip support, which is available for Vista and later.
			/// Detailed information about what the different versions do, can be found
			/// <a href="https://docs.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shell_notifyicona">here</a>
			/// </summary>
			Vista = 0x4,
		}
		public enum ENotifyCommand
		{
			/// <summary>The taskbar icon is being created.</summary>
			Add = 0x00,

			/// <summary>The settings of the taskbar icon are being updated.</summary>
			Modify = 0x01,

			/// <summary>The taskbar icon is deleted.</summary>
			Delete = 0x02,

			/// <summary>Focus is returned to the taskbar icon. Currently not in use.</summary>
			SetFocus = 0x03,

			/// <summary>
			/// Shell32.dll version 5.0 and later only. Instructs the taskbar to behave according to the version number specified in the 
			/// uVersion member of the structure pointed to by lpdata. This message allows you to specify whether you want the version
			/// 5.0 behavior found on Microsoft Windows 2000 systems, or the behavior found on earlier Shell versions.
			/// The default value for uVersion is zero, indicating that the original Windows 95 notify icon behavior should be used.</summary>
			SetVersion = 0x04,
		}
		[Flags]
		public enum ENotifyIconDataMembers :uint
		{
			/// <summary>The message ID is set.</summary>
			Message = 0x01,

			/// <summary>The notification icon is set.</summary>
			Icon = 0x02,

			/// <summary>The tooltip is set.</summary>
			Tip = 0x04,

			/// <summary>State information 'IconState' is set. This applies to both 'IconState' and 'StateMask'.</summary>
			State = 0x08,

			/// <summary>The balloon ToolTip is set. Accordingly, the following members are set: 'szInfo', 'szInfoTitle', 'BalloonFlags', 'VersionOrTimeout'</summary>
			Info = 0x10,

			/// <summary>Internal identifier is set. Reserved.</summary>
			Guid = 0x20,

			/// <summary>
			/// Windows Vista (Shell32.dll version 6.0.6) and later. If the ToolTip cannot be displayed immediately, discard it.<br/>
			/// Use this flag for ToolTips that represent real-time information which would be meaningless or misleading if displayed at a later time.
			/// For example, a message that states "Your telephone is ringing."<br/> This modifies and must be combined with the <see cref="Info"/> flag.</summary>
			Realtime = 0x40,

			/// <summary>
			/// Windows Vista (Shell32.dll version 6.0.6) and later. Use the standard ToolTip. Normally, when uVersion is set
			/// to NOTIFYICON_VERSION_4, the standard ToolTip is replaced by the application-drawn pop-up user interface (UI).
			/// If the application wants to show the standard tooltip in that case, regardless of whether the on-hover UI is showing,
			/// it can specify NIF_SHOWTIP to indicate the standard tooltip should still be shown.<br/>
			/// Note that the NIF_SHOWTIP flag is effective until the next call to Shell_NotifyIcon.</summary>
			UseLegacyToolTips = 0x80
		}
		[Flags]
		public enum EIconState :uint
		{
			/// <summary>The icon is visible.</summary>
			Visible = 0x00,

			/// <summary>Hide the icon.</summary>
			Hidden = 0x01,

			/// <summary>The icon is shared</summary>
			Shared = 0x02, // currently not supported
		}
		public enum ENotifyIconBalloonFlags :uint
		{
			/// <summary>No icon is displayed.</summary>
			None = 0x00,

			/// <summary>An information icon is displayed.</summary>
			Info = 0x01,

			/// <summary>A warning icon is displayed.</summary>
			Warning = 0x02,

			/// <summary>An error icon is displayed.</summary>
			Error = 0x03,

			/// <summary>Windows XP Service Pack 2 (SP2) and later. Use a custom icon as the title icon.</summary>
			User = 0x04,

			/// <summary>Windows XP (Shell32.dll version 6.0) and later. Do not play the associated sound. Applies only to balloon ToolTips.</summary>
			NoSound = 0x10,

			/// <summary>
			/// Windows Vista (Shell32.dll version 6.0.6) and later. The large version of the icon should be used as the balloon icon.
			/// This corresponds to the icon with dimensions SM_CXICON x SM_CYICON. If this flag is not set, the icon with dimensions
			/// XM_CXSMICON x SM_CYSMICON is used.<br/>
			/// - This flag can be used with all stock icons.<br/>
			/// - Applications that use older customized icons (NIIF_USER with hIcon) must
			///   provide a new SM_CXICON x SM_CYICON version in the tray icon (hIcon). These
			///   icons are scaled down when they are displayed in the System Tray or
			///   System Control Area (SCA).<br/>
			/// - New customized icons (NIIF_USER with hBalloonIcon) must supply an
			///   SM_CXICON x SM_CYICON version in the supplied icon (hBalloonIcon).</summary>
			LargeIcon = 0x20,

			/// <summary>Windows 7 and later.</summary>
			RespectQuietTime = 0x80
		}
		#endregion

		#region Notification Icon Interop Structs
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct NOTIFYICONDATA
		{
			/// <summary>Size of this structure, in bytes.</summary>
			public uint cbSize;

			/// <summary>
			/// Handle to the window that receives notification messages associated with an icon in the
			/// taskbar status area. The Shell uses hWnd and uID to identify which icon to operate on
			/// when Shell_NotifyIcon is invoked.</summary>
			public IntPtr hWnd; // WindowHandle

			/// <summary>
			/// Application-defined identifier of the taskbar icon. The Shell uses hWnd and uID to identify
			/// which icon to operate on when Shell_NotifyIcon is invoked. You can have multiple icons
			/// associated with a single hWnd by assigning each a different uID. This feature, however
			/// is currently not used.</summary>
			public uint uID; // TaskbarIconId

			/// <summary>
			/// Flags that indicate which of the other members contain valid data.
			/// This member can be a combination of the NIF_XXX constants.</summary>
			public ENotifyIconDataMembers uFlags; // ValidMembers

			/// <summary>
			/// Application-defined message identifier. The system uses this identifier to send
			/// notifications to the window identified in hWnd.</summary>
			public uint uCallbackMessage;

			/// <summary>A handle to the icon that should be displayed.</summary>
			public IntPtr hIcon; // IconHandle

			/// <summary>
			/// String with the text for a standard ToolTip. It can have a maximum of 64 characters including
			/// the terminating NULL. For Version 5.0 and later, szTip can have a maximum of 128 characters, including the terminating NULL.</summary>
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
			public string szTip; // ToolTipText

			/// <summary>State of the icon. Remember to also set the 'StateMask'</summary>
			public EIconState IconState;

			/// <summary>
			/// A value that specifies which bits of the state member are retrieved or modified.
			/// For example, setting this member to 'Hidden' causes only the item's hidden state to be retrieved.</summary>
			public EIconState StateMask;

			/// <summary>
			/// String with the text for a balloon ToolTip. It can have a maximum of 255 characters.
			/// To remove the ToolTip, set the NIF_INFO flag in uFlags and set szInfo to an empty string.</summary>
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
			public string szInfo; // BalloonText

			/// <summary>
			/// Mainly used to set the version when 'Shell_NotifyIcon' is invoked with 'NotifyCommand.SetVersion'.
			/// However, for legacy operations, the same member is also used to set timeouts for balloon ToolTips.</summary>
			public uint VersionOrTimeout;

			/// <summary>
			/// String containing a title for a balloon ToolTip.
			/// This title appears in boldface above the text. It can have a maximum of 63 characters.</summary>
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
			public string szInfoTitle; // BalloonTitle

			/// <summary>
			/// Adds an icon to a balloon ToolTip, which is placed to the left of the title.
			/// If the 'szInfoTitle' member is zero-length, the icon is not shown.</summary>
			public ENotifyIconBalloonFlags dwInfoFlags; // BalloonFlags

			/// <summary>
			/// Windows XP (Shell32.dll version 6.0) and later.<br/>
			/// - Windows 7 and later: A registered GUID that identifies the icon.
			///   This value overrides uID and is the recommended method of identifying the icon.<br/>
			/// - Windows XP through Windows Vista: Reserved.</summary>
			public Guid guidItem; // TaskbarIconGuid

			/// <summary>
			/// Windows Vista (Shell32.dll version 6.0.6) and later. The handle of a customized balloon icon provided
			/// by the application that should be used independently of the tray icon. If this member is non-NULL and
			/// the 'BalloonFlags.User' flag is set, this icon is used as the balloon icon.<br/>
			/// If this member is NULL, the legacy behavior is carried out.</summary>
			public IntPtr hBalloonIcon; // CustomBalloonIconHandle

			/// <summary>Creates a default data structure that provides a hidden taskbar icon without the icon being set.</summary>
			public static NOTIFYICONDATA New(IntPtr handle, uint callback_message, ENotifyIconVersion version)
			{
				var data = new NOTIFYICONDATA();

				// Need to set another size on xp/2003- otherwise certain features (e.g. balloon tooltips) don't work.
				data.cbSize = Environment.OSVersion.Version.Major >= 6
					? (uint)Marshal.SizeOf(data) // Use the current size
					: 952; // NOTIFYICONDATAW_V3_SIZE

				// Set to fixed timeout for old versions
				if (Environment.OSVersion.Version.Major < 6)
					data.VersionOrTimeout = 10;

				data.hWnd = handle;
				data.uID = 0x0;
				data.uCallbackMessage = callback_message;
				data.VersionOrTimeout = (uint)version;
				data.hIcon = IntPtr.Zero;
				data.IconState = EIconState.Hidden;
				data.StateMask = EIconState.Hidden;
				data.uFlags = ENotifyIconDataMembers.Message | ENotifyIconDataMembers.Icon | ENotifyIconDataMembers.Tip;
				data.szTip = string.Empty;
				data.szInfo = string.Empty;
				data.szInfoTitle = string.Empty;
				return data;
			}
		}
		#endregion

		[DllImport("shell32.Dll", EntryPoint = "Shell_NotifyIcon", CharSet = CharSet.Unicode)]
		public static extern bool NotifyIcon(ENotifyCommand cmd, [In] ref NOTIFYICONDATA data);

		#pragma warning restore CA1401 // P/Invokes should not be visible
	}
}
