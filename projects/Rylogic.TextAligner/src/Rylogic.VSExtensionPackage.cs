﻿using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Outlining;

namespace Rylogic.TextAligner
{
	/// <summary>
	/// This is the class that implements the package exposed by this assembly.
	/// The minimum requirement for a class to be considered a valid package for Visual Studio
	/// is to implement the IVsPackage interface and register itself with the shell.
	/// This package uses the helper classes defined inside the Managed Package Framework (MPF)
	/// to do it: it derives from the Package class that provides the implementation of the
	/// IVsPackage interface and uses the registration attributes defined in the framework to
	/// register itself and its components with the shell.
	/// </summary>
	[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)] // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is a package.
	[InstalledProductRegistration("Rylogic.TextAligner", "Rylogic extensions", "1.1", IconResourceID = 400)] // This attribute is used to register the information needed to show this package in the Help/About dialog of Visual Studio.
	[ProvideMenuResource("Menus.ctmenu", 1)] // This attribute is needed to let the shell know that this package exposes some menus.
	[ProvideOptionPage(typeof(AlignOptions), "Rylogic", "Align Options", 0, 0, true)]
	[Guid(GuidList.guidRylogicTextAlignerPkgString)]
	public sealed class RylogicTextAlignerPackage :AsyncPackage ,IOleCommandTarget
	{
		/// <summary>
		/// Default constructor of the package.
		/// Inside this method you can place any initialization code that does not require
		/// any Visual Studio service because at this point the package object is created but
		/// not sited yet inside Visual Studio environment. The place to do all the other
		/// initialization is the Initialize method.</summary>
		public RylogicTextAlignerPackage()
		{}

		/// <summary>
		/// Initialization of the package; this method is called right after the package is sited, so this is the place
		/// where you can put all the initialization code that rely on services provided by VisualStudio.</summary>
		protected async override System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
		{
			await base.InitializeAsync(cancellationToken, progress);

			// Add our command handlers for menu (commands must exist in the .vsct file)
			if (await GetServiceAsync(typeof(IMenuCommandService)) is OleMenuCommandService mcs)
			{
				mcs.AddCommand(new AlignMenuCommand(this));
			}
		}

		/// <summary>Return the VS service of type 'TService'</summary>
		public object GetService<TService>()
		{
			// Note: the return value of 'GetService' is not always cast-able to 'TService'
			ThreadHelper.ThrowIfNotOnUIThread();
			return GetService(typeof(TService));
		}

		[Import] public IOutliningManagerService OutliningManagerService;

		/// <summary>Return a dialog page</summary>
		public T GetDialogPage<T>() where T :DialogPage
		{
			return (T)GetDialogPage(typeof(T));
		}
	}
}
