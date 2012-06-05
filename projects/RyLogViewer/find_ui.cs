using System;
using System.Windows.Forms;

namespace RyLogViewer
{
	public partial class FindUI :Form
	{
		public event EventHandler FindNext;
		public event EventHandler FindPrev;

		public FindUI()
		{
			InitializeComponent();
			m_pattern.m_check_active.Visible = false;
			m_pattern.m_btn_add.Visible = false;
		}
	}
}
