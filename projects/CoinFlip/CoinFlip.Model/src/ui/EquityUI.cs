using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using pr.extn;
using pr.gui;
using pr.util;

namespace CoinFlip
{
	public class EquityUI :UserControl ,IDockable
	{
		#region UI Elements
		private TableLayoutPanel m_table0;
		private GraphControl m_graph_equity;
		#endregion

		private EquityUI() {}
		public EquityUI(string name)
		{
			InitializeComponent();
			DockControl = new DockControl(this, name) { TabText = name };

			SetupUI();
		}
		protected override void Dispose(bool disposing)
		{
			Data = null;
			DockControl = null;
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>Provides support for the DockContainer</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public DockControl DockControl
		 {
		 	get { return m_impl_dock_control; }
		 	private set
		 	{
		 		if (m_impl_dock_control == value) return;
		 		Util.Dispose(ref m_impl_dock_control);
		 		m_impl_dock_control = value;
		 	}
		 }
		private DockControl m_impl_dock_control;

		/// <summary>The equity data displayed in this chart</summary>
		public EquityMap Data
		{
			get { return m_data; }
			set
			{
				if (m_data == value) return;
				if (m_data != null)
				{
					m_data.DataChanged -= HandleDataChanged;
				}
				m_data = value;
				if (m_data != null)
				{
					m_data.DataChanged += HandleDataChanged;
				}

				// Handlers
				void HandleDataChanged(object sender, EventArgs e)
				{
					InvalidateData();
				}
			}
		}
		private EquityMap m_data;

		/// <summary>Set up UI Elements</summary>
		private void SetupUI()
		{
			// Graph
			m_graph_equity.XAxis.TickText = (double value, double step_size) =>
			{
				// Get the 'ticks' relative to 'StartTime'
				var dt = (Data.StartTime + new TimeSpan(Misc.TimeFrameToTicks(value, Data.TimeFrame))).LocalDateTime;
				return dt.ToString("HH:mm'\r\n'ddd dd");
			};
		}

		/// <summary>Trigger a refresh of the graph data</summary>
		public void InvalidateData()
		{
			if (!IsHandleCreated) return;
			if (m_update_in_progress != m_update_issue) return;
			++m_update_issue;

			// Kick off an update of the data
			this.BeginInvoke(UpdateSeriesData);
		}
		private int m_update_in_progress;
		private int m_update_issue;

		/// <summary>Update the series data</summary>
		private void UpdateSeriesData()
		{
			// Signal that 'm_update_issue' is in progress
			m_update_in_progress = m_update_issue;

			m_graph_equity.Data.Clear();
			foreach (var table in Data)
			{
				var series = m_graph_equity.Data.Add2(new GraphControl.Series(table.Key.Symbol, table.Value.Count));
				using (var lk = series.Lock())
				{
					foreach (var pt in table.Value)
					{
						// pt.Timestamp is in ticks relative to 'StartTime'
						var x = Misc.TicksToTimeFrame(pt.Timestamp, Data.TimeFrame);
						lk.Data.Add(new GraphControl.GraphValue(x, pt.Volume));
					}
				}
			}

			m_graph_equity.Invalidate();
		}

		#region Component Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.m_table0 = new System.Windows.Forms.TableLayoutPanel();
			this.m_graph_equity = new pr.gui.GraphControl();
			this.m_table0.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_table0
			// 
			this.m_table0.ColumnCount = 1;
			this.m_table0.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table0.Controls.Add(this.m_graph_equity, 0, 0);
			this.m_table0.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_table0.Location = new System.Drawing.Point(0, 0);
			this.m_table0.Margin = new System.Windows.Forms.Padding(0);
			this.m_table0.Name = "m_table0";
			this.m_table0.RowCount = 1;
			this.m_table0.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table0.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.m_table0.Size = new System.Drawing.Size(838, 278);
			this.m_table0.TabIndex = 1;
			// 
			// m_graph_equity
			// 
			this.m_graph_equity.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_graph_equity.Location = new System.Drawing.Point(0, 0);
			this.m_graph_equity.Margin = new System.Windows.Forms.Padding(0);
			this.m_graph_equity.Name = "m_graph_equity";
			this.m_graph_equity.Size = new System.Drawing.Size(838, 278);
			this.m_graph_equity.TabIndex = 0;
			this.m_graph_equity.Title = "";
			this.m_graph_equity.ZoomMax = 3.4028234663852886E+38D;
			this.m_graph_equity.ZoomMin = 1.4012984643248171E-45D;
			// 
			// EquityUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_table0);
			this.Name = "EquityUI";
			this.Size = new System.Drawing.Size(838, 278);
			this.m_table0.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion
	}
}
