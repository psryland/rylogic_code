using System.Windows.Controls;
using Rylogic.Common;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	public static class TextBox_
	{
		/// <summary>Returns a disposable object that preserves the current selection</summary>
		public static Scope<Range> SelectionScope(this TextBox edit)
		{
			return Scope.Create(
				() => Range.FromStartLength(edit.SelectionStart, edit.SelectionLength),
				rn => edit.Select(rn.Begi, rn.Sizei));
		}
	}
}
