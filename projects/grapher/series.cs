using System;
using System.Windows.Forms;
using pr.gui;

namespace pr
{
	public class Series
	{
		private readonly Grapher.Block m_xblock;
		private readonly Grapher.Block m_yblock;
		private readonly GraphControl.Series m_series;
		
		public Series(DataGridView grid, Grapher.Block xblock, Grapher.Block yblock, GraphControl.Series.RdrOpts rdr_options)
		{
			m_xblock = xblock;
			m_yblock = yblock;
			m_series = new GraphControl.Series{RenderOptions=rdr_options};

			// Choose the smallest range
			if (m_xblock.m_range.Count < m_yblock.m_range.Count)	m_yblock.m_range.Count = m_xblock.m_range.Count;
			if (m_yblock.m_range.Count < m_xblock.m_range.Count)	m_xblock.m_range.Count = m_yblock.m_range.Count;

			// Create the series
			double px = -double.MaxValue;
			for (int i = 0; i != m_xblock.m_range.Count; ++i)
			{
				try
				{
					DataGridViewCell cellx = grid[m_xblock.m_column, (int)m_xblock.m_range.Begin + i];
					DataGridViewCell celly = grid[m_yblock.m_column, (int)m_yblock.m_range.Begin + i];
					object x_boxed = Convert.ChangeType(cellx.Value, typeof(double));
					object y_boxed = Convert.ChangeType(celly.Value, typeof(double));
					double x = x_boxed != null ? (double)x_boxed : 0.0;
					double y = y_boxed != null ? (double)y_boxed : 0.0;
					m_series.Values.Add(new GraphControl.GraphValue(x, y));
					m_series.Sorted &= x >= px;
					px = x;
				}
				catch (FormatException) {}
			}
		}

		// Return the series to add to a graph
		public GraphControl.Series GraphSeries
		{
			get { return m_series; }
		}
	}
}
