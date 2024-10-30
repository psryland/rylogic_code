using System;
using System.Threading;
using System.Threading.Tasks;
using Rylogic.Utility;

namespace Rylogic.TextAligner
{
	public sealed class UnalignMenuCommand(IPackage package, Guid cmd_set, int unalign_id)
		: BaseCommand(package, cmd_set, unalign_id)
	{
		/// <summary>
		/// This function is the callback used to execute a command when the a menu item is clicked.
		/// See the Initialize method to see how the menu item is associated to this function using
		/// the OleMenuCommandService service and the MenuCommand class.</summary>
		protected override async Task ExecuteAsync(CancellationToken cancellation_token)
		{
			try
			{
				var view_host = await CurrentViewHostAsync(cancellation_token);
				if (view_host == null)
					return;

				var options = Package.GetDialogPage<AlignOptions>();
				new Aligner(options.Groups, options.AlignStyle, options.LineIgnorePattern, view_host.TextView, EAction.Unalign);
			}
			catch (Exception ex)
			{
				Log.Write(ELogLevel.Error, ex, "Execute UnalignMenuCommand failed");
				throw;
			}
		}
	}
}
