using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Rylogic.TextAligner
{
	/// <summary>Base class for MenuCommands</summary>
	public class BaseCommand :MenuCommand
	{
		public BaseCommand(RylogicTextAlignerPackage package, int cmd_id)
			:base(RunCommand, new CommandID(GuidList.guidRylogicTextAlignerCmdSet, cmd_id))
		{
			Package = package;
		}

		/// <summary>The owning package instance</summary>
		protected RylogicTextAlignerPackage Package { get; private set; }

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
		protected IWpfTextViewHost? CurrentViewHost
		{
			get
			{
				// Code to get access to the editor's currently selected text cribbed from
				// http://msdn.microsoft.com/en-us/library/dd884850.aspx
				var text_manager = (IVsTextManager)Package.GetService<VsTextManagerClass>();
				text_manager.GetActiveView(fMustHaveFocus: 1, pBuffer: null, ppView: out var text_view);
				if (text_view is IVsUserData user_data)
				{
					var guidViewHost = DefGuidList.guidIWpfTextViewHost;
					user_data.GetData(ref guidViewHost, out var holder);
					return (IWpfTextViewHost)holder;
				}
				return null;
			}
		}
	}
}
