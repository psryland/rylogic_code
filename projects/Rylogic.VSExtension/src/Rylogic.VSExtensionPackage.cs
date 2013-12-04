using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using pr.common;

namespace Rylogic.VSExtension
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
	[PackageRegistration(UseManagedResourcesOnly = true)] // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is a package.
	[InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // This attribute is used to register the information needed to show this package in the Help/About dialog of Visual Studio.
	[ProvideMenuResource("Menus.ctmenu", 1)] // This attribute is needed to let the shell know that this package exposes some menus.
	[ProvideOptionPage(typeof(AlignOptions), "Rylogic", "Align Options", 0, 0, true)]
	[Guid(GuidList.guidRylogic_VSExtensionPkgString)]
	public sealed class Rylogic_VSExtensionPackage :Package,IOleCommandTarget
	{
		private Align m_aligner;

		/// <summary>
		/// Default constructor of the package.
		/// Inside this method you can place any initialization code that does not require
		/// any Visual Studio service because at this point the package object is created but
		/// not sited yet inside Visual Studio environment. The place to do all the other
		/// initialization is the Initialize method.</summary>
		public Rylogic_VSExtensionPackage()
		{}

		/// <summary>
		/// Initialization of the package; this method is called right after the package is sited, so this is the place
		/// where you can put all the initialization code that rely on services provided by VisualStudio.</summary>
		protected override void Initialize()
		{
			base.Initialize();

			// Add our command handlers for menu (commands must exist in the .vsct file)
			var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
			if (null != mcs)
			{
				// Create the command for the menu item.
				var menu_command_id = new CommandID(GuidList.guidRylogic_VSExtensionCmdSet, (int)PkgCmdIDList.cmdidAlign);
				var menu_item = new AlignMenuCommand(MenuItemCallback, menu_command_id);
				mcs.AddCommand(menu_item);
			}
		}

		/// <summary>
		/// This function is the callback used to execute a command when the a menu item is clicked.
		/// See the Initialize method to see how the menu item is associated to this function using
		/// the OleMenuCommandService service and the MenuCommand class.
		/// </summary>
		private void MenuItemCallback(object sender, EventArgs e)
		{
			var view_host = CurrentViewHost;
			if (view_host != null)
				m_aligner = new Align(Groups, view_host.TextView);
		}

		/// <summary>Get the view host for the currently selected text editor</summary>
		private IWpfTextViewHost CurrentViewHost
		{
			get
			{
				// Code to get access to the editor's currently selected text cribbed from
				// http://msdn.microsoft.com/en-us/library/dd884850.aspx
				var txtMgr = (IVsTextManager)GetService(typeof(SVsTextManager));

				IVsTextView text_view = null;
				txtMgr.GetActiveView(fMustHaveFocus:1, pBuffer:null, ppView:out text_view);
				var user_data = text_view as IVsUserData;
				if (user_data == null)
					return null;

				Guid guidViewHost = DefGuidList.guidIWpfTextViewHost;

				object holder;
				user_data.GetData(ref guidViewHost, out holder);
				var view_host = (IWpfTextViewHost)holder;
				return view_host;
			}
		}

		/// <summary>The align patterns</summary>
		internal IEnumerable<AlignGroup> Groups
		{
			get
			{
				var align_options = (AlignOptions)GetDialogPage(typeof(AlignOptions));
				return align_options.Groups;
			}
		}
	}
}

//// Show a Message Box to prove we were here
//var ui_shell = (IVsUIShell)GetService(typeof(SVsUIShell));
//var clsid = Guid.Empty;
//int result;

//Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(ui_shell.ShowMessageBox(
//		   0,
//		   ref clsid,
//		   "Rylogic.VSExtension",
//		   string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.ToString()),
//		   string.Empty,
//		   0,
//		   OLEMSGBUTTON.OLEMSGBUTTON_OK,
//		   OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
//		   OLEMSGICON.OLEMSGICON_INFO,
//		   0,        // false
//		   out result));

/////<summary>
///// Given an IWpfTextViewHost representing the currently selected editor pane,
///// return the ITextDocument for that view. That's useful for learning things
///// like the filename of the document, its creation date, and so on.</summary>
//ITextDocument GetTextDocumentForView(IWpfTextViewHost view_host)
//{
//	ITextDocument document;
//	view_host.TextView.TextDataModel.DocumentBuffer.Properties.TryGetProperty(typeof(ITextDocument), out document);
//	return document;
//}

///// Get the current editor selection
//ITextSelection GetSelection( IWpfTextViewHost viewHost )
//{
//	return viewHost.TextView.Selection;
//}
