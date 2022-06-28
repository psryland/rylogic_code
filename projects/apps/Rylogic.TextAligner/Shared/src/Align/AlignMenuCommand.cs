using System;
using Rylogic.Utility;

namespace Rylogic.TextAligner
{
	public sealed class AlignMenuCommand :BaseCommand
	{
		public AlignMenuCommand(RylogicTextAlignerPackage package)
			:base(package, PkgCmdIDList.cmdidAlign)
		{}

		/// <summary>
		/// This function is the callback used to execute a command when the a menu item is clicked.
		/// See the Initialize method to see how the menu item is associated to this function using
		/// the OleMenuCommandService service and the MenuCommand class.</summary>
		protected override void Execute()
		{
			try
			{
				var view_host = CurrentViewHost;
				if (view_host == null) return;
				var options = Package.GetDialogPage<AlignOptions>();
				new Aligner(options.Groups, options.AlignStyle, options.LineIgnorePattern, view_host.TextView, EAction.Align);
			}
			catch (Exception ex)
			{
				Log.Write(ELogLevel.Error, ex, "Execute AlignMenuCommand failed");
				throw;
			}
		}
	}
}
