using System;
using System.Drawing;
using System.Windows.Forms;
using RyLogViewer.Properties;
using pr.extn;
using pr.gui;

namespace RyLogViewer
{
	/// <summary>A UI for configuring a swizzle element</summary>
	public partial class SwizzleUI :Form
	{
		private readonly ToolTip m_tt;
		private HelpUI           m_dlg_help;

		/// <summary>The source mapping</summary>
		public string Src { get { return m_edit_source.Text; } set { m_edit_source.Text = value; } }

		/// <summary>The destination mapping</summary>
		public string Dst { get { return m_edit_output.Text; } set { m_edit_output.Text = value; } }

		/// <summary>Return the Form for displaying the quick help for the match field syntax (lazy loaded)</summary>
		private HelpUI SwizzleHelpUI
		{
			get
			{
				return m_dlg_help ?? (m_dlg_help = HelpUI.FromHtml(this, Resources.swizzle_quick_ref, "Swizzle Help", new Point(1,1), new Size(640,480), ToolForm.EPin.TopRight));
			}
		}

		public SwizzleUI()
		{
			InitializeComponent();
			m_tt = new ToolTip();

			m_btn_help.Click += (s,a)=>
				{
					SwizzleHelpUI.Display();
				};
			m_edit_source.TextChanged += (s,a)=>
				{
					UpdateUI();
				};
			m_edit_output.TextChanged += (s,a)=>
				{
					UpdateUI();
				};
			m_edit_test.TextChanged += (s,a)=>
				{
					UpdateUI();
				};
			Shown += (s,a)=> UpdateUI();
			Disposed += (s,a) =>
				{
					m_tt.Dispose();
				};
		}

		private bool m_in_update_ui;
		private void UpdateUI()
		{
			if (m_in_update_ui) return;
			try
			{
				m_in_update_ui = true;
				try
				{
					var mapping = SubSwizzle.CreateMapping(Src, Dst);
					m_edit_source.BackColor = SystemColors.Window;
					m_edit_output  .BackColor = SystemColors.Window;
					m_edit_source.ToolTip(m_tt, "The source pattern for the mapping");
					m_edit_output  .ToolTip(m_tt, "The destination pattern for the mapping");
					m_edit_result.Text = SubSwizzle.Apply(m_edit_test.Text, mapping);
				}
				catch (Exception ex)
				{
					m_edit_source.BackColor = Color.LightSalmon;
					m_edit_output.BackColor = Color.LightSalmon;
					m_edit_source.ToolTip(m_tt, ex.Message);
					m_edit_output.ToolTip(m_tt, ex.Message);
				}
			}
			finally
			{
				m_in_update_ui = false;
			}
		}
	}
}
