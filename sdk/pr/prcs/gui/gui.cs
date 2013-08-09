using System;
using System.Windows.Forms;
using pr.util;

namespace pr.gui
{
	public static class Gui
	{
		/// <summary>Returns an RAII scope for suspending layout</summary>
		public static Scope SuspendLayout(this Control ctrl, bool layout_on_resume)
		{
			return Scope.Create(ctrl.SuspendLayout, () => ctrl.ResumeLayout(layout_on_resume));
		}

		/// <summary>Wrapper of begin invoke that takes a lambda</summary>
		public static IAsyncResult BeginInvoke(this Form form, Action action)
		{
			return form.BeginInvoke(action);
		}
	}
}
