using System;
using System.Collections.Generic;

namespace Rylogic.TextAligner
{
	public sealed class UnalignMenuCommand :BaseCommand
	{
		public UnalignMenuCommand(RylogicTextAlignerPackage package)
			:base(package, PkgCmdIDList.cmdidUnalign)
		{}

		/// <summary>
		/// This function is the callback used to execute a command when the a menu item is clicked.
		/// See the Initialize method to see how the menu item is associated to this function using
		/// the OleMenuCommandService service and the MenuCommand class.</summary>
		protected override void Execute()
		{
			var view_host = CurrentViewHost;
			if (view_host == null) return;
			var options = Package.GetDialogPage<AlignOptions>();
			new Aligner(options.Groups, options.AlignStyle, view_host.TextView, EAction.Unalign);
		}
	}
}
