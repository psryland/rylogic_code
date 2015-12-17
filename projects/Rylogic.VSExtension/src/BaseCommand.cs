using System;
using System.ComponentModel.Design;

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
	}
}
