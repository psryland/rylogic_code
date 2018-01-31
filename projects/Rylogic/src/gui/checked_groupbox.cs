using System;
using System.ComponentModel;
using System.Windows.Forms;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Rylogic.Gui
{
	public class CheckedGroupBox :GroupBox
	{
		public enum EStyle
		{
			Basic,
			CheckBox,
			RadioBtn,
		}

		private CheckBox m_chk;
		private RadioButton m_rdo;

		public CheckedGroupBox()
		{
			Enabled = false;
			Style = EStyle.CheckBox;
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref m_chk);
			Util.Dispose(ref m_rdo);
			base.Dispose(disposing);
		}
		protected override void OnLayout(LayoutEventArgs levent)
		{
			base.OnLayout(levent);
			DoLayout();
		}
		protected override void OnLocationChanged(EventArgs e)
		{
			base.OnLocationChanged(e);
			DoLayout();
		}
		protected override void OnParentChanged(EventArgs e)
		{
			base.OnParentChanged(e);
			HandleParentChanged();
		}
		protected override void OnForeColorChanged(EventArgs e)
		{
			if (m_chk != null) m_chk.ForeColor = ForeColor;
			if (m_rdo != null) m_rdo.ForeColor = ForeColor;
			base.OnForeColorChanged(e);
		}
		protected override void OnBackColorChanged(EventArgs e)
		{
			if (m_chk != null) m_chk.BackColor = BackColor;
			if (m_rdo != null) m_rdo.BackColor = BackColor;
			base.OnBackColorChanged(e);
		}

		/// <summary>The style of checked group box</summary>
		[DefaultValue(EStyle.CheckBox)]
		public EStyle Style
		{
			get { return m_style; }
			set
			{
				if (m_style == value) return;
				m_style = value;

				Util.Dispose(ref m_chk);
				Util.Dispose(ref m_rdo);

				switch (m_style)
				{
				default: throw new Exception("Unknown checked group box style");
				case EStyle.Basic:
					{
						break;
					}
				case EStyle.CheckBox:
					{
						m_chk = new CheckBox
						{
							AutoSize = true,
							Text = m_text,
							BackColor = BackColor,
							ForeColor = ForeColor,
							Checked = false,
						};
						m_chk.CheckedChanged += HandleCheckedChanged;
						break;
					}
				case EStyle.RadioBtn:
					{
						m_rdo = new RadioButton
						{
							AutoSize = true,
							Text = m_text,
							BackColor = BackColor,
							ForeColor = ForeColor,
							Checked = false,
						};
						m_rdo.CheckedChanged += HandleCheckedChanged;
						break;
					}
				}

				// Reset the text and parent on the new controls
				Text = m_text;
				HandleCheckedChanged();
				HandleParentChanged();
			}
		}
		private EStyle m_style;

		/// <summary>Get/Set the check state for the group box</summary>
		[DefaultValue(false)]
		public bool Checked
		{
			get
			{
				if (m_chk != null) return m_chk.Checked;
				if (m_rdo != null) return m_rdo.Checked;
				return true;
			}
			set
			{
				if (m_chk != null) { m_chk.Checked = value; return; }
				if (m_rdo != null) { m_rdo.Checked = value; return; }
			}
		}

		/// <summary>Get/Set the group check state</summary>
		[DefaultValue(CheckState.Unchecked)]
		public CheckState CheckState
		{
			get
			{
				if (m_chk != null) return m_chk.CheckState;
				if (m_rdo != null) return m_rdo.Checked ? CheckState.Checked : CheckState.Unchecked;
				return CheckState.Checked;
			}
			set
			{
				if (m_chk != null) { m_chk.CheckState = value; return; }
				if (m_rdo != null) { m_rdo.Checked = value == CheckState.Checked; return; }
			}
		}

		/// <summary>Raised when the checked state changes</summary>
		public event EventHandler CheckedChanged;

		/// <summary>Raised when the CheckState changes</summary>
		public event EventHandler CheckStateChanged;

		/// <summary>Get/Set the group text label</summary>
		public override string Text
		{
			get { return m_text; }
			set
			{
				m_text = value;
				if (m_chk != null) m_chk.Text = value;
				if (m_rdo != null) m_rdo.Text = value;
				base.Text = Style == EStyle.Basic ? value : string.Empty;
			}
		}
		private string m_text;

		/// <summary>Called when the check state changes for the group box</summary>
		private void HandleCheckedChanged(object sender = null, EventArgs args = null)
		{
			Enabled = Checked;
			CheckedChanged.Raise(this);
			CheckStateChanged.Raise(this);
		}

		/// <summary>Called when the parent of the group box is changed</summary>
		private void HandleParentChanged(object sender = null, EventArgs args = null)
		{
			var chk = (ButtonBase)m_chk ?? m_rdo;
			if (chk != null) chk.Parent = Parent;
			DoLayout();
		}

		/// <summary>Reposition the CheckBox</summary>
		private void DoLayout()
		{
			using (this.SuspendLayout(layout_on_resume: false))
			{
				var chk = (ButtonBase)m_chk ?? m_rdo;
				if (chk != null)
				{
					chk.Location = Location.Shifted(3, 0);
					chk.BringToFront();
				}
			}
		}
	}
}
