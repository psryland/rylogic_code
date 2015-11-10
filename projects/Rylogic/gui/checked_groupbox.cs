using System;
using System.Drawing;
using System.Windows.Forms;
using pr.extn;
using pr.util;

namespace pr.gui
{
	public class CheckedGroupBox :GroupBox
	{
		private CheckBox m_chk;

		public CheckedGroupBox()
		{
			m_chk = new CheckBox
			{
				AutoSize = true,
				Text = string.Empty,
				ForeColor = SystemColors.GrayText,
			};
			m_chk.CheckedChanged += (s,a) =>
			{
				Enabled = m_chk.Checked;
			};
			Enabled = false;
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref m_chk);
			base.Dispose(disposing);
		}

		/// <summary>Get/Set the check state for the group box</summary>
		public bool Checked
		{
			get { return m_chk.Checked; }
			set { m_chk.Checked = value; }
		}

		/// <summary>Get/Set the group check state</summary>
		public CheckState CheckState
		{
			get { return m_chk.CheckState; }
			set { m_chk.CheckState = value; }
		}

		/// <summary>Raised when the checked state changes</summary>
		public event EventHandler CheckedChanged
		{
			add { m_chk.CheckedChanged += value; }
			remove { m_chk.CheckedChanged -= value; }
		}

		/// <summary>Raised when the CheckState changes</summary>
		public event EventHandler CheckStateChanged
		{
			add { m_chk.CheckStateChanged += value; }
			remove { m_chk.CheckStateChanged -= value; }
		}

		/// <summary>Get/Set the group text label</summary>
		public override string Text
		{
			get { return m_chk.Text; }
			set { base.Text = string.Empty; m_chk.Text = value; }
		}

		protected override void OnLocationChanged(EventArgs e)
		{
			base.OnLocationChanged(e);
			m_chk.Location = Location.Shifted(3, 0);
			m_chk.BringToFront();
		}
		protected override void OnParentChanged(EventArgs e)
		{
			base.OnParentChanged(e);
			m_chk.Parent = Parent;
			m_chk.BringToFront();
		}
	}
}
