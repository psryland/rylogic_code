using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Rylogic.VSExtension
{
	public sealed class AlignMenuCommand :BaseCommand
	{
		public AlignMenuCommand(Rylogic_VSExtensionPackage package)
			:base(package, PkgCmdIDList.cmdidAlign)
		{}

		/// <summary>
		/// This function is the callback used to execute a command when the a menu item is clicked.
		/// See the Initialize method to see how the menu item is associated to this function using
		/// the OleMenuCommandService service and the MenuCommand class.
		/// </summary>
		protected override void Execute()
		{
			var view_host = CurrentViewHost;
			if (view_host != null)
				new Align(Groups, view_host.TextView);
		}

		/// <summary>Get the view host for the currently selected text editor</summary>
		private IWpfTextViewHost CurrentViewHost
		{
			get
			{
				// Code to get access to the editor's currently selected text cribbed from
				// http://msdn.microsoft.com/en-us/library/dd884850.aspx
				var txtMgr = (IVsTextManager)Package.GetService<SVsTextManager>();

				IVsTextView text_view = null;
				txtMgr.GetActiveView(fMustHaveFocus: 1, pBuffer: null, ppView: out text_view);
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
		private IEnumerable<AlignGroup> Groups
		{
			get
			{
				var align_options = Package.GetDialogPage<AlignOptions>();
				return align_options.Groups;
			}
		}
	}
}
