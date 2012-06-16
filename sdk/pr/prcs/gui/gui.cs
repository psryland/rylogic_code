using System;
using System.Windows.Forms;

namespace pr.gui
{
	public static class Gui
	{
		/// <summary>An RAII scope for suspending control layout</summary>
		public class SuspendedLayoutScope :IDisposable
		{
			private readonly Control m_ctrl;
			private readonly bool    m_layout_on_resume;
			public SuspendedLayoutScope(Control ctrl, bool layout_on_resume) { m_ctrl = ctrl; m_layout_on_resume = layout_on_resume; }
			public void Dispose()                                            { m_ctrl.ResumeLayout(m_layout_on_resume); }
		}
		
		/// <summary>Returns an instance of a SuspendedLayoutScope</summary>
		public static SuspendedLayoutScope SuspendLayout(this Control ctrl, bool layout_on_resume)
		{
			return new SuspendedLayoutScope(ctrl, layout_on_resume);
		}
	}
}
