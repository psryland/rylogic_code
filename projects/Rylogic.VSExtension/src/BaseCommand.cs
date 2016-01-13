using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Rylogic.VSExtension
{
	/// <summary>Base class for MenuCommands</summary>
	public class BaseCommand :MenuCommand
	{
		public BaseCommand(Rylogic_VSExtensionPackage package, int cmd_id)
			:base(RunCommand, new CommandID(GuidList.guidRylogic_VSExtensionCmdSet, cmd_id))
		{
			Package = package;
		}

		/// <summary>The owning package instance</summary>
		protected Rylogic_VSExtensionPackage Package { get; private set; }

		/// <summary></summary>
		private static void RunCommand(object sender, EventArgs e)
		{
			var cmd = (BaseCommand)sender;
			cmd.Execute();
		}

		/// <summary>Execute the command</summary>
		protected virtual void Execute()
		{
		}

		/// <summary>Get the view host for the currently selected text editor</summary>
		protected IWpfTextViewHost CurrentViewHost
		{
			get
			{
				// Code to get access to the editor's currently selected text cribbed from
				// http://msdn.microsoft.com/en-us/library/dd884850.aspx
				var txtMgr = Package.GetService<VsTextManagerClass>() as IVsTextManager;

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
	}
}
