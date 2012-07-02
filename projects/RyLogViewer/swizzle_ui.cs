
using System;
using System.Drawing;
using System.Windows.Forms;
using pr.util;

namespace RyLogViewer
{
	/// <summary>A UI for configuring a swizzle element</summary>
	public partial class SwizzleUI :Form
	{
		private readonly ToolTip m_tt;
		
		/// <summary>The source mapping</summary>
		public string Src { get { return m_edit_source.Text; } set { m_edit_source.Text = value; } }
		
		/// <summary>The destination mapping</summary>
		public string Dst { get { return m_edit_output.Text; } set { m_edit_output.Text = value; } }
		
		public SwizzleUI()
		{
			InitializeComponent();
			m_tt = new ToolTip();
			
			m_btn_help.Click += (s,a)=>
				{
					HelpUI.Show(this, "RyLogViewer.docs.SwizzleQuickRef.html", "Swizzle Help");
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
					var mapping = Swizzle.CreateMapping(Src, Dst);
					m_edit_source.BackColor = SystemColors.Window;
					m_edit_output  .BackColor = SystemColors.Window;
					m_edit_source.ToolTip(m_tt, "The source pattern for the mapping");
					m_edit_output  .ToolTip(m_tt, "The destination pattern for the mapping");
					m_edit_result.Text = Swizzle.Apply(m_edit_test.Text, mapping);
				}
				catch (Exception ex)
				{
					m_edit_source.BackColor = Color.LightSalmon;
					m_edit_output  .BackColor = Color.LightSalmon;
					m_edit_source.ToolTip(m_tt, ex.Message);
					m_edit_output  .ToolTip(m_tt, ex.Message);
				}
			}
			finally
			{
				m_in_update_ui = false;
			}
		}

	}
}
