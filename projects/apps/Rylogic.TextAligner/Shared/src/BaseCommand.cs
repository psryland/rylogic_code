using System;
using System.ComponentModel.Design;
using System.Threading;
using System.Threading.Tasks;
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
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "<Pending>")]
		private static async void RunCommand(object sender, EventArgs e)
		{
			try
			{
				if (sender is BaseCommand cmd)
					await cmd.ExecuteAsync(CancellationToken.None);
			}
			catch (Exception ex)
			{
				Log.Write(Utility.ELogLevel.Error, ex, "RunCommand Failed");
			}
		}

		/// <summary>Execute the command</summary>
		protected virtual Task ExecuteAsync(CancellationToken cancellation_token)
		{
			return Task.CompletedTask;
		}

		/// <summary>Get the view host for the currently selected text editor</summary>
		protected async Task<IWpfTextViewHost?> CurrentViewHostAsync(CancellationToken cancellation_token)
		{
			// Code to get access to the editor's currently selected text cribbed from
			// http://msdn.microsoft.com/en-us/library/dd884850.aspx
			if (await Package.GetServiceAsync<VsTextManagerClass>(cancellation_token) is not IVsTextManager text_manager)
				return null;

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
