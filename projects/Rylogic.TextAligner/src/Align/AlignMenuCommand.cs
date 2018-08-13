using System;
using System.Collections.Generic;

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
			var view_host = CurrentViewHost;
			if (view_host != null)
				new Align(Groups, view_host.TextView);
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
