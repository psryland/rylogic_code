using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Rylogic_Log_Viewer.Properties;

namespace Rylogic_Log_Viewer
{
	internal partial class SettingsUI :Form
	{
		private readonly Settings                   m_settings;    // The app settings changed by this UI
		private readonly BindingList<Pattern> m_highlights;  // The highlight patterns
		public SettingsUI()
		{
			InitializeComponent();
			m_settings = new Settings();
			m_highlights = new BindingList<Pattern>(Pattern.Import(m_settings.HighlightPatterns));
			m_settings.PropertyChanged += (s,a) => UpdateUI();
			UpdateUI();
			
			// General
			m_check_alternate_line_colour.CheckedChanged += (s,a)=>
				{
					m_settings.AlternateLineColours = m_check_alternate_line_colour.Checked;
				};
			m_btn_selection_fore_colour.Click += (s,a)=>
				{
					m_settings.LineSelectForeColour = PickColour(m_settings.LineSelectForeColour);
				};
			m_btn_selection_back_colour.Click += (s,a)=>
				{
					m_settings.LineSelectBackColour = PickColour(m_settings.LineSelectBackColour);
				};
			m_btn_line1_fore_colour.Click += (s,a)=>
				{
					m_settings.LineForeColour1 = PickColour(m_settings.LineForeColour1);
				};
			m_btn_line1_back_colour.Click += (s,a)=>
				{
					m_settings.LineBackColour1 = PickColour(m_settings.LineBackColour1);
				};
			m_btn_line2_fore_colour.Click += (s,a)=>
				{
					m_settings.LineForeColour2 = PickColour(m_settings.LineForeColour2);
				};
			m_btn_line2_back_colour.Click += (s,a)=>
				{
					m_settings.LineBackColour2 = PickColour(m_settings.LineBackColour2);
				};
			
			// Highlights
			m_grid_highlight.DataSource = m_highlights;
			//m_patternedit_regexp.TextChanged += (s,a)=>
			//    {
			//        new Regex(m_edit_regexp.Text, RegexOptions);
			//    };
			//m_btn_hl_add.Click += (s,a)=>
			//    {
			//        if (string.IsNullOrWhiteSpace(m_edit_regexp.Text)) return;
			//        m_highlights.Add(new Pattern{Active = true, Pattern = m_edit_regexp.Text});
			//    };
			m_grid_highlight.MouseClick += (s,a)=>
				{
					var hit = m_grid_highlight.HitTest(a.X, a.Y);
					switch (hit.ColumnIndex)
					{
					case 0:break;
					case 1:break;
					case 2:break;
					}
					//if (m_grid_highlight.Columns[].HeaderText == "Highlighting")
					//{
					//    // Show colour pickers
					//}
				};
			
			// Save on close (if OK is pressed)
			Closed += (s,a) =>
			{
				if (DialogResult != DialogResult.OK) return;
				m_settings.HighlightPatterns = Pattern.Export(m_highlights);
				m_settings.Save();
			};
		}

		private void UpdateUI()
		{
			SuspendLayout();
			m_check_alternate_line_colour.Checked = m_settings.AlternateLineColours;
			m_lbl_selection_example.BackColor = m_settings.LineSelectBackColour;
			m_lbl_selection_example.ForeColor = m_settings.LineSelectForeColour;
			m_lbl_line1_example.BackColor = m_settings.LineBackColour1;
			m_lbl_line1_example.ForeColor = m_settings.LineForeColour1;
			m_lbl_line2_example.BackColor = m_settings.LineBackColour2;
			m_lbl_line2_example.ForeColor = m_settings.LineForeColour2;
			m_btn_line2_fore_colour.Enabled = m_settings.AlternateLineColours;
			m_btn_line2_back_colour.Enabled = m_settings.AlternateLineColours;
			m_lbl_line2_example.Enabled = m_settings.AlternateLineColours;
			ResumeLayout();
		}

		/// <summary>Colour picker helper</summary>
		private Color PickColour(Color current)
		{
			var d = new ColorDialog{AllowFullOpen = true, AnyColor = true, Color = current};
			return d.ShowDialog(this) == DialogResult.OK ? d.Color : current;
		}
	}
}
