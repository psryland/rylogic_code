namespace pr
{
	sealed partial class Grapher
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
			this.m_menu = new System.Windows.Forms.MenuStrip();
			this.m_menu_file = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_open = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_separator1 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_file_recent_files = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_separator2 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_exit = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_data = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_data_add_series = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_data_show_series_list = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_graphs = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_add_plot = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_plot_separator1 = new System.Windows.Forms.ToolStripSeparator();
			this.m_grid = new System.Windows.Forms.DataGridView();
			this.Column1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.m_status = new System.Windows.Forms.StatusStrip();
			this.m_menu.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid)).BeginInit();
			this.SuspendLayout();
			// 
			// m_menu
			// 
			this.m_menu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_file,
            this.m_menu_data,
            this.m_menu_graphs});
			this.m_menu.Location = new System.Drawing.Point(0, 0);
			this.m_menu.Name = "m_menu";
			this.m_menu.Size = new System.Drawing.Size(578, 24);
			this.m_menu.TabIndex = 0;
			this.m_menu.Text = "menuStrip1";
			// 
			// m_menu_file
			// 
			this.m_menu_file.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_file_open,
            this.m_menu_file_separator1,
            this.m_menu_file_recent_files,
            this.m_menu_file_separator2,
            this.m_menu_exit});
			this.m_menu_file.Name = "m_menu_file";
			this.m_menu_file.Size = new System.Drawing.Size(37, 20);
			this.m_menu_file.Text = "&File";
			// 
			// m_menu_file_open
			// 
			this.m_menu_file_open.Name = "m_menu_file_open";
			this.m_menu_file_open.Size = new System.Drawing.Size(136, 22);
			this.m_menu_file_open.Text = "&Open";
			// 
			// m_menu_file_separator1
			// 
			this.m_menu_file_separator1.Name = "m_menu_file_separator1";
			this.m_menu_file_separator1.Size = new System.Drawing.Size(133, 6);
			// 
			// m_menu_file_recent_files
			// 
			this.m_menu_file_recent_files.Name = "m_menu_file_recent_files";
			this.m_menu_file_recent_files.Size = new System.Drawing.Size(136, 22);
			this.m_menu_file_recent_files.Text = "&Recent Files";
			// 
			// m_menu_file_separator2
			// 
			this.m_menu_file_separator2.Name = "m_menu_file_separator2";
			this.m_menu_file_separator2.Size = new System.Drawing.Size(133, 6);
			// 
			// m_menu_exit
			// 
			this.m_menu_exit.Name = "m_menu_exit";
			this.m_menu_exit.Size = new System.Drawing.Size(136, 22);
			this.m_menu_exit.Text = "E&xit";
			// 
			// m_menu_data
			// 
			this.m_menu_data.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_data_add_series,
            this.m_menu_data_show_series_list});
			this.m_menu_data.Name = "m_menu_data";
			this.m_menu_data.Size = new System.Drawing.Size(43, 20);
			this.m_menu_data.Text = "&Data";
			// 
			// m_menu_data_add_series
			// 
			this.m_menu_data_add_series.Name = "m_menu_data_add_series";
			this.m_menu_data_add_series.Size = new System.Drawing.Size(157, 22);
			this.m_menu_data_add_series.Text = "&Add Series";
			// 
			// m_menu_data_show_series_list
			// 
			this.m_menu_data_show_series_list.Name = "m_menu_data_show_series_list";
			this.m_menu_data_show_series_list.Size = new System.Drawing.Size(157, 22);
			this.m_menu_data_show_series_list.Text = "Show Series &List";
			// 
			// m_menu_graphs
			// 
			this.m_menu_graphs.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_add_plot,
            this.m_menu_plot_separator1});
			this.m_menu_graphs.Name = "m_menu_graphs";
			this.m_menu_graphs.Size = new System.Drawing.Size(56, 20);
			this.m_menu_graphs.Text = "&Graphs";
			// 
			// m_menu_add_plot
			// 
			this.m_menu_add_plot.Name = "m_menu_add_plot";
			this.m_menu_add_plot.Size = new System.Drawing.Size(129, 22);
			this.m_menu_add_plot.Text = "&Add Plot...";
			// 
			// m_menu_plot_separator1
			// 
			this.m_menu_plot_separator1.Name = "m_menu_plot_separator1";
			this.m_menu_plot_separator1.Size = new System.Drawing.Size(126, 6);
			// 
			// m_grid
			// 
			this.m_grid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.m_grid.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
			dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
			dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.m_grid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
			this.m_grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_grid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Column1});
			dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
			dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.m_grid.DefaultCellStyle = dataGridViewCellStyle2;
			this.m_grid.Location = new System.Drawing.Point(0, 27);
			this.m_grid.Name = "m_grid";
			this.m_grid.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
			dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle3.Format = "N0";
			dataGridViewCellStyle3.NullValue = null;
			dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.m_grid.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
			this.m_grid.RowHeadersVisible = false;
			this.m_grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
			this.m_grid.ShowEditingIcon = false;
			this.m_grid.Size = new System.Drawing.Size(578, 533);
			this.m_grid.TabIndex = 1;
			// 
			// Column1
			// 
			this.Column1.HeaderText = "Column1";
			this.Column1.Name = "Column1";
			// 
			// m_status
			// 
			this.m_status.Location = new System.Drawing.Point(0, 563);
			this.m_status.Name = "m_status";
			this.m_status.Size = new System.Drawing.Size(578, 22);
			this.m_status.TabIndex = 2;
			this.m_status.Text = "statusStrip1";
			// 
			// Grapher
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(578, 585);
			this.Controls.Add(this.m_status);
			this.Controls.Add(this.m_grid);
			this.Controls.Add(this.m_menu);
			this.MainMenuStrip = this.m_menu;
			this.Name = "Grapher";
			this.Text = "Grapher";
			this.m_menu.ResumeLayout(false);
			this.m_menu.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.MenuStrip m_menu;
		private System.Windows.Forms.ToolStripMenuItem m_menu_file;
		private System.Windows.Forms.ToolStripSeparator m_menu_file_separator2;
		private System.Windows.Forms.ToolStripMenuItem m_menu_exit;
		private System.Windows.Forms.DataGridView m_grid;
		private System.Windows.Forms.ToolStripMenuItem m_menu_data;
		private System.Windows.Forms.ToolStripMenuItem m_menu_graphs;
		private System.Windows.Forms.ToolStripMenuItem m_menu_add_plot;
		private System.Windows.Forms.ToolStripMenuItem m_menu_file_open;
		private System.Windows.Forms.ToolStripSeparator m_menu_file_separator1;
		private System.Windows.Forms.ToolStripMenuItem m_menu_file_recent_files;
		private System.Windows.Forms.StatusStrip m_status;
		private System.Windows.Forms.ToolStripSeparator m_menu_plot_separator1;
		private System.Windows.Forms.ToolStripMenuItem m_menu_data_add_series;
		private System.Windows.Forms.ToolStripMenuItem m_menu_data_show_series_list;
		private System.Windows.Forms.DataGridViewTextBoxColumn Column1;
	}
}

