using System;
using System.ComponentModel.Design;

namespace Rylogic.VSExtension
{
	public sealed class AlignMenuCommand :MenuCommand
	{
		[Flags] private enum EOleStatus
		{
			SUPPORTED = 1,
			ENABLED = 2,
			CHECKED = 4,
			INVISIBLE = 16
		}

		public AlignMenuCommand(EventHandler handler,CommandID command) :base(handler,command)
		{}
	}
}
