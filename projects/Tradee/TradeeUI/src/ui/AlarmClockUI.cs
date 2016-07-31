using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using pr.gui;
using pr.util;
using ToolStripContainer = pr.gui.ToolStripContainer;

namespace Tradee
{
	public class AlarmClockUI :BaseUI
	{
		#region UI Elements
		private ToolStripContainer m_tsc;
		private DataGridView m_grid;
		private ToolStrip m_ts;
		private ToolStripButton m_btn_add_reminder;
		private ToolTip m_tt;
		private ImageList m_il;
		#endregion

		public AlarmClockUI(MainModel model) :base(model, "Alarm Clock")
		{
			InitializeComponent();

			SetupUI();
			UpdateUI();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>Set up UI Elements</summary>
		private void SetupUI()
		{
			// Docking
			DockControl.DefaultDockLocation = new DockContainer.DockLocation(new[] {EDockSite.Left, EDockSite.Bottom});

			// Alarm grid
			m_grid.DataSource = Model.Alarms.AlarmList;

			// Tool bar buttons
			m_btn_add_reminder.Click += (s,a) =>
			{
				Model.Alarms.AddReminder();
			};
		}

		/// <summary>Update the state of the UI elements</summary>
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
		}

		#region Component Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AlarmClockUI));
			this.m_tsc = new pr.gui.ToolStripContainer();
			this.m_grid = new System.Windows.Forms.DataGridView();
			this.m_ts = new System.Windows.Forms.ToolStrip();
			this.m_btn_add_reminder = new System.Windows.Forms.ToolStripButton();
			this.m_il = new System.Windows.Forms.ImageList(this.components);
			this.m_tt = new System.Windows.Forms.ToolTip(this.components);
			this.m_tsc.ContentPanel.SuspendLayout();
			this.m_tsc.TopToolStripPanel.SuspendLayout();
			this.m_tsc.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid)).BeginInit();
			this.m_ts.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_tsc
			// 
			// 
			// m_tsc.ContentPanel
			// 
			this.m_tsc.ContentPanel.Controls.Add(this.m_grid);
			this.m_tsc.ContentPanel.Size = new System.Drawing.Size(392, 362);
			this.m_tsc.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_tsc.Location = new System.Drawing.Point(0, 0);
			this.m_tsc.Name = "m_tsc";
			this.m_tsc.Size = new System.Drawing.Size(392, 401);
			this.m_tsc.TabIndex = 0;
			this.m_tsc.Text = "toolStripContainer1";
			// 
			// m_tsc.TopToolStripPanel
			// 
			this.m_tsc.TopToolStripPanel.Controls.Add(this.m_ts);
			// 
			// m_grid
			// 
			this.m_grid.AllowUserToAddRows = false;
			this.m_grid.AllowUserToResizeRows = false;
			this.m_grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_grid.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
			this.m_grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_grid.ColumnHeadersVisible = false;
			dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.m_grid.DefaultCellStyle = dataGridViewCellStyle1;
			this.m_grid.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_grid.Location = new System.Drawing.Point(0, 0);
			this.m_grid.Name = "m_grid";
			this.m_grid.RowHeadersVisible = false;
			this.m_grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid.Size = new System.Drawing.Size(392, 362);
			this.m_grid.TabIndex = 0;
			// 
			// m_ts
			// 
			this.m_ts.Dock = System.Windows.Forms.DockStyle.None;
			this.m_ts.ImageScalingSize = new System.Drawing.Size(32, 32);
			this.m_ts.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_btn_add_reminder});
			this.m_ts.Location = new System.Drawing.Point(3, 0);
			this.m_ts.Name = "m_ts";
			this.m_ts.Size = new System.Drawing.Size(48, 39);
			this.m_ts.TabIndex = 0;
			// 
			// m_btn_add_reminder
			// 
			this.m_btn_add_reminder.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_add_reminder.Image = global::Tradee.Properties.Resources.bell;
			this.m_btn_add_reminder.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_add_reminder.Name = "m_btn_add_reminder";
			this.m_btn_add_reminder.Size = new System.Drawing.Size(36, 36);
			this.m_btn_add_reminder.Text = "toolStripButton1";
			// 
			// m_il
			// 
			this.m_il.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_il.ImageStream")));
			this.m_il.TransparentColor = System.Drawing.Color.Transparent;
			this.m_il.Images.SetKeyName(0, "reminders.png");
			// 
			// AlarmClockUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_tsc);
			this.Name = "AlarmClockUI";
			this.Size = new System.Drawing.Size(392, 401);
			this.m_tsc.ContentPanel.ResumeLayout(false);
			this.m_tsc.TopToolStripPanel.ResumeLayout(false);
			this.m_tsc.TopToolStripPanel.PerformLayout();
			this.m_tsc.ResumeLayout(false);
			this.m_tsc.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid)).EndInit();
			this.m_ts.ResumeLayout(false);
			this.m_ts.PerformLayout();
			this.ResumeLayout(false);

		}
		#endregion
	}
}
