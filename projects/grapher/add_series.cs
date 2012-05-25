using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using pr.gui;

namespace pr
{
	public partial class AddSeries :Form
	{
		private readonly List<Plot> m_plot;
		private readonly List<Grapher.Block> m_blocks;
		private Color m_colour;
		
		public AddSeries(List<Plot> plot, List<Grapher.Block> blocks)
		{
			InitializeComponent();
			m_plot = plot;
			m_blocks = blocks;

			// Series name
			m_edit_series_name.Text = "Series";
			m_edit_series_name.SelectAll();
			m_edit_series_name.Focus();

			// Populate the axis range combos
			foreach (Grapher.Block b in blocks)
			{
				m_combo_xaxis_column.Items.Add(b.Description);
				m_combo_yaxis_column.Items.Add(b.Description);
			}

			// take a guess at the x,y blocks to use
			m_combo_xaxis_column.SelectedIndex = 0;
			m_combo_yaxis_column.SelectedIndex = blocks.Count > 1 ? 1 : 0;

			// Populate the graph window combo
			foreach (Plot p in plot)
				m_combo_graphs.Items.Add(p.Graph.Title);
			m_combo_graphs.SelectedIndex = 0;

			// Plot type
			foreach (var x in Enum.GetValues(typeof(GraphControl.Series.RdrOpts.PlotType)))
				m_combo_plot_type.Items.Add(x);
			m_combo_plot_type.SelectedIndex = 1;

			// Plot colour
			m_btn_plot_colour_black .BackColor = Color.Black;  m_btn_plot_colour_black  .Click += SelectPlotColour;
			m_btn_plot_colour_blue  .BackColor = Color.Blue;   m_btn_plot_colour_blue   .Click += SelectPlotColour;
			m_btn_plot_colour_red   .BackColor = Color.Red;    m_btn_plot_colour_red    .Click += SelectPlotColour;
			m_btn_plot_colour_green .BackColor = Color.Green;  m_btn_plot_colour_green  .Click += SelectPlotColour;
			m_btn_plot_colour_gray  .BackColor = Color.Gray;   m_btn_plot_colour_gray   .Click += SelectPlotColour;
			m_btn_plot_colour_custom.BackColor = Color.Purple; m_btn_plot_colour_custom .Click += SelectPlotColour;
			m_btn_plot_colour_custom.DoubleClick += EditCustomColour;
			SelectPlotColour(m_btn_plot_colour_black, new EventArgs());

			m_edit_point_size.Text = "0";
			m_edit_line_size.Text = "0";
		}

		// Construct a series from the selected blocks of data
		public Series NewSeries(DataGridView grid)
		{
			GraphControl.Series.RdrOpts ropts = new GraphControl.Series.RdrOpts();
			ropts.m_plot_type = (GraphControl.Series.RdrOpts.PlotType)m_combo_plot_type.SelectedIndex;
			ropts.m_point_colour = ropts.m_line_colour = ropts.m_bar_colour = m_colour;
			float.TryParse(m_edit_point_size.Text, out ropts.m_point_size);
			float.TryParse(m_edit_line_size.Text, out ropts.m_line_width);
			return new Series(grid, m_blocks[m_combo_xaxis_column.SelectedIndex], m_blocks[m_combo_yaxis_column.SelectedIndex], ropts);
		}

		// Return the selected plot window
		public Plot Plot
		{
			get { return m_plot[m_combo_graphs.SelectedIndex]; }
		}

		// Called when the user clicks on a plot colour
		private void SelectPlotColour(object sender, EventArgs e)
		{
			Button selected = (Button)sender;
			Size big = new Size(27+1,21+1);
			Size sml = new Size(27-1,21-1);
			m_btn_plot_colour_black .Size = selected == m_btn_plot_colour_black  ? big : sml;
			m_btn_plot_colour_blue  .Size = selected == m_btn_plot_colour_blue   ? big : sml;
			m_btn_plot_colour_red   .Size = selected == m_btn_plot_colour_red    ? big : sml;
			m_btn_plot_colour_green .Size = selected == m_btn_plot_colour_green  ? big : sml;
			m_btn_plot_colour_gray  .Size = selected == m_btn_plot_colour_gray   ? big : sml;
			m_btn_plot_colour_custom.Size = selected == m_btn_plot_colour_custom ? big : sml;
			m_colour = selected.BackColor;
		}

		// Set the back color for a control
		private static void EditCustomColour(object sender, EventArgs e)
		{
			Control ctrl = (Control)sender;
			ColorDialog cd = new ColorDialog{Color = ctrl.BackColor};
			if (cd.ShowDialog() == DialogResult.OK) ctrl.BackColor = cd.Color;
		}
	}
}
