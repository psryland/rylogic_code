namespace RyLogViewer
{
	partial class SettingsUI
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsUI));
			this.m_tabctrl = new System.Windows.Forms.TabControl();
			this.m_tab_general = new System.Windows.Forms.TabPage();
			this.m_group_startup = new System.Windows.Forms.GroupBox();
			this.m_check_save_screen_loc = new System.Windows.Forms.CheckBox();
			this.m_check_load_last_file = new System.Windows.Forms.CheckBox();
			this.m_group_log_text_colours = new System.Windows.Forms.GroupBox();
			this.m_lbl_line2_example = new System.Windows.Forms.Label();
			this.m_lbl_line1_example = new System.Windows.Forms.Label();
			this.m_lbl_selection_example = new System.Windows.Forms.Label();
			this.m_lbl_line2_colours = new System.Windows.Forms.Label();
			this.m_lbl_line1_colours = new System.Windows.Forms.Label();
			this.m_lbl_selection_colour = new System.Windows.Forms.Label();
			this.m_check_alternate_line_colour = new System.Windows.Forms.CheckBox();
			this.m_tab_highlight = new System.Windows.Forms.TabPage();
			this.m_split_hl = new System.Windows.Forms.SplitContainer();
			this.m_pattern_hl = new RyLogViewer.PatternUI();
			this.m_grid_highlight = new System.Windows.Forms.DataGridView();
			this.m_tab_filter = new System.Windows.Forms.TabPage();
			this.m_split_ft = new System.Windows.Forms.SplitContainer();
			this.m_pattern_ft = new RyLogViewer.PatternUI();
			this.m_grid_filter = new System.Windows.Forms.DataGridView();
			this.m_tabctrl.SuspendLayout();
			this.m_tab_general.SuspendLayout();
			this.m_group_startup.SuspendLayout();
			this.m_group_log_text_colours.SuspendLayout();
			this.m_tab_highlight.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_split_hl)).BeginInit();
			this.m_split_hl.Panel1.SuspendLayout();
			this.m_split_hl.Panel2.SuspendLayout();
			this.m_split_hl.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_highlight)).BeginInit();
			this.m_tab_filter.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_split_ft)).BeginInit();
			this.m_split_ft.Panel1.SuspendLayout();
			this.m_split_ft.Panel2.SuspendLayout();
			this.m_split_ft.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_filter)).BeginInit();
			this.SuspendLayout();
			// 
			// m_tabctrl
			// 
			this.m_tabctrl.Controls.Add(this.m_tab_general);
			this.m_tabctrl.Controls.Add(this.m_tab_highlight);
			this.m_tabctrl.Controls.Add(this.m_tab_filter);
			this.m_tabctrl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_tabctrl.Location = new System.Drawing.Point(0, 0);
			this.m_tabctrl.Name = "m_tabctrl";
			this.m_tabctrl.SelectedIndex = 0;
			this.m_tabctrl.Size = new System.Drawing.Size(494, 445);
			this.m_tabctrl.TabIndex = 0;
			// 
			// m_tab_general
			// 
			this.m_tab_general.Controls.Add(this.m_group_startup);
			this.m_tab_general.Controls.Add(this.m_group_log_text_colours);
			this.m_tab_general.Location = new System.Drawing.Point(4, 22);
			this.m_tab_general.Name = "m_tab_general";
			this.m_tab_general.Padding = new System.Windows.Forms.Padding(3);
			this.m_tab_general.Size = new System.Drawing.Size(486, 419);
			this.m_tab_general.TabIndex = 0;
			this.m_tab_general.Text = "General";
			this.m_tab_general.UseVisualStyleBackColor = true;
			// 
			// m_group_startup
			// 
			this.m_group_startup.Controls.Add(this.m_check_save_screen_loc);
			this.m_group_startup.Controls.Add(this.m_check_load_last_file);
			this.m_group_startup.Location = new System.Drawing.Point(8, 6);
			this.m_group_startup.Name = "m_group_startup";
			this.m_group_startup.Size = new System.Drawing.Size(205, 66);
			this.m_group_startup.TabIndex = 4;
			this.m_group_startup.TabStop = false;
			this.m_group_startup.Text = "Startup";
			// 
			// m_check_save_screen_loc
			// 
			this.m_check_save_screen_loc.AutoSize = true;
			this.m_check_save_screen_loc.Location = new System.Drawing.Point(14, 42);
			this.m_check_save_screen_loc.Name = "m_check_save_screen_loc";
			this.m_check_save_screen_loc.Size = new System.Drawing.Size(180, 17);
			this.m_check_save_screen_loc.TabIndex = 1;
			this.m_check_save_screen_loc.Text = "Restore previous screen position";
			this.m_check_save_screen_loc.UseVisualStyleBackColor = true;
			// 
			// m_check_load_last_file
			// 
			this.m_check_load_last_file.AutoSize = true;
			this.m_check_load_last_file.Location = new System.Drawing.Point(14, 19);
			this.m_check_load_last_file.Name = "m_check_load_last_file";
			this.m_check_load_last_file.Size = new System.Drawing.Size(85, 17);
			this.m_check_load_last_file.TabIndex = 0;
			this.m_check_load_last_file.Text = "Load last file";
			this.m_check_load_last_file.UseVisualStyleBackColor = true;
			// 
			// m_group_log_text_colours
			// 
			this.m_group_log_text_colours.Controls.Add(this.m_lbl_line2_example);
			this.m_group_log_text_colours.Controls.Add(this.m_lbl_line1_example);
			this.m_group_log_text_colours.Controls.Add(this.m_lbl_selection_example);
			this.m_group_log_text_colours.Controls.Add(this.m_lbl_line2_colours);
			this.m_group_log_text_colours.Controls.Add(this.m_lbl_line1_colours);
			this.m_group_log_text_colours.Controls.Add(this.m_lbl_selection_colour);
			this.m_group_log_text_colours.Controls.Add(this.m_check_alternate_line_colour);
			this.m_group_log_text_colours.Location = new System.Drawing.Point(8, 78);
			this.m_group_log_text_colours.Name = "m_group_log_text_colours";
			this.m_group_log_text_colours.Size = new System.Drawing.Size(319, 130);
			this.m_group_log_text_colours.TabIndex = 3;
			this.m_group_log_text_colours.TabStop = false;
			this.m_group_log_text_colours.Text = "Log Text Colours";
			// 
			// m_lbl_line2_example
			// 
			this.m_lbl_line2_example.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_lbl_line2_example.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_lbl_line2_example.Location = new System.Drawing.Point(164, 67);
			this.m_lbl_line2_example.Name = "m_lbl_line2_example";
			this.m_lbl_line2_example.Size = new System.Drawing.Size(149, 21);
			this.m_lbl_line2_example.TabIndex = 13;
			this.m_lbl_line2_example.Text = "Click here to modify colours";
			this.m_lbl_line2_example.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// m_lbl_line1_example
			// 
			this.m_lbl_line1_example.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_lbl_line1_example.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_lbl_line1_example.Location = new System.Drawing.Point(98, 16);
			this.m_lbl_line1_example.Name = "m_lbl_line1_example";
			this.m_lbl_line1_example.Size = new System.Drawing.Size(215, 21);
			this.m_lbl_line1_example.TabIndex = 12;
			this.m_lbl_line1_example.Text = "Click here to modify colours";
			this.m_lbl_line1_example.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// m_lbl_selection_example
			// 
			this.m_lbl_selection_example.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_lbl_selection_example.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_lbl_selection_example.Location = new System.Drawing.Point(98, 98);
			this.m_lbl_selection_example.Name = "m_lbl_selection_example";
			this.m_lbl_selection_example.Size = new System.Drawing.Size(215, 21);
			this.m_lbl_selection_example.TabIndex = 11;
			this.m_lbl_selection_example.Text = "Click here to modify colours";
			this.m_lbl_selection_example.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// m_lbl_line2_colours
			// 
			this.m_lbl_line2_colours.AutoSize = true;
			this.m_lbl_line2_colours.Location = new System.Drawing.Point(28, 71);
			this.m_lbl_line2_colours.Name = "m_lbl_line2_colours";
			this.m_lbl_line2_colours.Size = new System.Drawing.Size(130, 13);
			this.m_lbl_line2_colours.TabIndex = 10;
			this.m_lbl_line2_colours.Text = "Alternate Log Text Colour:";
			// 
			// m_lbl_line1_colours
			// 
			this.m_lbl_line1_colours.AutoSize = true;
			this.m_lbl_line1_colours.Location = new System.Drawing.Point(7, 20);
			this.m_lbl_line1_colours.Name = "m_lbl_line1_colours";
			this.m_lbl_line1_colours.Size = new System.Drawing.Size(85, 13);
			this.m_lbl_line1_colours.TabIndex = 8;
			this.m_lbl_line1_colours.Text = "Log Text Colour:";
			// 
			// m_lbl_selection_colour
			// 
			this.m_lbl_selection_colour.AutoSize = true;
			this.m_lbl_selection_colour.Location = new System.Drawing.Point(7, 102);
			this.m_lbl_selection_colour.Name = "m_lbl_selection_colour";
			this.m_lbl_selection_colour.Size = new System.Drawing.Size(87, 13);
			this.m_lbl_selection_colour.TabIndex = 5;
			this.m_lbl_selection_colour.Text = "Selection Colour:";
			// 
			// m_check_alternate_line_colour
			// 
			this.m_check_alternate_line_colour.AutoSize = true;
			this.m_check_alternate_line_colour.Location = new System.Drawing.Point(10, 47);
			this.m_check_alternate_line_colour.Name = "m_check_alternate_line_colour";
			this.m_check_alternate_line_colour.Size = new System.Drawing.Size(129, 17);
			this.m_check_alternate_line_colour.TabIndex = 4;
			this.m_check_alternate_line_colour.Text = "Alternate Line Colours";
			this.m_check_alternate_line_colour.UseVisualStyleBackColor = true;
			// 
			// m_tab_highlight
			// 
			this.m_tab_highlight.Controls.Add(this.m_split_hl);
			this.m_tab_highlight.Location = new System.Drawing.Point(4, 22);
			this.m_tab_highlight.Name = "m_tab_highlight";
			this.m_tab_highlight.Padding = new System.Windows.Forms.Padding(3);
			this.m_tab_highlight.Size = new System.Drawing.Size(486, 419);
			this.m_tab_highlight.TabIndex = 1;
			this.m_tab_highlight.Text = "Highlight";
			this.m_tab_highlight.UseVisualStyleBackColor = true;
			// 
			// m_split_hl
			// 
			this.m_split_hl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_split_hl.Location = new System.Drawing.Point(3, 3);
			this.m_split_hl.Name = "m_split_hl";
			this.m_split_hl.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// m_split_hl.Panel1
			// 
			this.m_split_hl.Panel1.Controls.Add(this.m_pattern_hl);
			// 
			// m_split_hl.Panel2
			// 
			this.m_split_hl.Panel2.Controls.Add(this.m_grid_highlight);
			this.m_split_hl.Size = new System.Drawing.Size(480, 413);
			this.m_split_hl.SplitterDistance = 167;
			this.m_split_hl.TabIndex = 3;
			// 
			// m_pattern_hl
			// 
			this.m_pattern_hl.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_pattern_hl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_pattern_hl.Location = new System.Drawing.Point(0, 0);
			this.m_pattern_hl.MinimumSize = new System.Drawing.Size(420, 78);
			this.m_pattern_hl.Name = "m_pattern_hl";
			this.m_pattern_hl.Size = new System.Drawing.Size(480, 167);
			this.m_pattern_hl.TabIndex = 2;
			// 
			// m_grid_highlight
			// 
			this.m_grid_highlight.AllowUserToAddRows = false;
			this.m_grid_highlight.AllowUserToOrderColumns = true;
			this.m_grid_highlight.AllowUserToResizeRows = false;
			this.m_grid_highlight.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_grid_highlight.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
			this.m_grid_highlight.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_grid_highlight.Location = new System.Drawing.Point(0, 0);
			this.m_grid_highlight.MultiSelect = false;
			this.m_grid_highlight.Name = "m_grid_highlight";
			this.m_grid_highlight.RowHeadersWidth = 24;
			this.m_grid_highlight.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid_highlight.Size = new System.Drawing.Size(480, 242);
			this.m_grid_highlight.TabIndex = 1;
			// 
			// m_tab_filter
			// 
			this.m_tab_filter.Controls.Add(this.m_split_ft);
			this.m_tab_filter.Location = new System.Drawing.Point(4, 22);
			this.m_tab_filter.Name = "m_tab_filter";
			this.m_tab_filter.Size = new System.Drawing.Size(486, 419);
			this.m_tab_filter.TabIndex = 2;
			this.m_tab_filter.Text = "Filter";
			this.m_tab_filter.UseVisualStyleBackColor = true;
			// 
			// m_split_ft
			// 
			this.m_split_ft.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_split_ft.Location = new System.Drawing.Point(0, 0);
			this.m_split_ft.Name = "m_split_ft";
			this.m_split_ft.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// m_split_ft.Panel1
			// 
			this.m_split_ft.Panel1.Controls.Add(this.m_pattern_ft);
			// 
			// m_split_ft.Panel2
			// 
			this.m_split_ft.Panel2.Controls.Add(this.m_grid_filter);
			this.m_split_ft.Size = new System.Drawing.Size(486, 419);
			this.m_split_ft.SplitterDistance = 143;
			this.m_split_ft.TabIndex = 5;
			// 
			// m_pattern_ft
			// 
			this.m_pattern_ft.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_pattern_ft.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_pattern_ft.Location = new System.Drawing.Point(0, 0);
			this.m_pattern_ft.MinimumSize = new System.Drawing.Size(420, 78);
			this.m_pattern_ft.Name = "m_pattern_ft";
			this.m_pattern_ft.Size = new System.Drawing.Size(486, 143);
			this.m_pattern_ft.TabIndex = 4;
			// 
			// m_grid_filter
			// 
			this.m_grid_filter.AllowUserToAddRows = false;
			this.m_grid_filter.AllowUserToOrderColumns = true;
			this.m_grid_filter.AllowUserToResizeRows = false;
			this.m_grid_filter.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_grid_filter.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
			this.m_grid_filter.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_grid_filter.Location = new System.Drawing.Point(0, 0);
			this.m_grid_filter.MultiSelect = false;
			this.m_grid_filter.Name = "m_grid_filter";
			this.m_grid_filter.RowHeadersWidth = 24;
			this.m_grid_filter.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid_filter.Size = new System.Drawing.Size(486, 272);
			this.m_grid_filter.TabIndex = 3;
			// 
			// SettingsUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(494, 445);
			this.Controls.Add(this.m_tabctrl);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MinimumSize = new System.Drawing.Size(510, 277);
			this.Name = "SettingsUI";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Options";
			this.m_tabctrl.ResumeLayout(false);
			this.m_tab_general.ResumeLayout(false);
			this.m_group_startup.ResumeLayout(false);
			this.m_group_startup.PerformLayout();
			this.m_group_log_text_colours.ResumeLayout(false);
			this.m_group_log_text_colours.PerformLayout();
			this.m_tab_highlight.ResumeLayout(false);
			this.m_split_hl.Panel1.ResumeLayout(false);
			this.m_split_hl.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_split_hl)).EndInit();
			this.m_split_hl.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_grid_highlight)).EndInit();
			this.m_tab_filter.ResumeLayout(false);
			this.m_split_ft.Panel1.ResumeLayout(false);
			this.m_split_ft.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_split_ft)).EndInit();
			this.m_split_ft.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_grid_filter)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TabControl m_tabctrl;
		private System.Windows.Forms.TabPage m_tab_general;
		private System.Windows.Forms.TabPage m_tab_highlight;
		private System.Windows.Forms.DataGridView m_grid_highlight;
		private System.Windows.Forms.TabPage m_tab_filter;
		private System.Windows.Forms.CheckBox m_check_alternate_line_colour;
		private System.Windows.Forms.GroupBox m_group_log_text_colours;
		private System.Windows.Forms.Label m_lbl_selection_colour;
		private System.Windows.Forms.Label m_lbl_line1_colours;
		private System.Windows.Forms.Label m_lbl_line2_example;
		private System.Windows.Forms.Label m_lbl_line1_example;
		private System.Windows.Forms.Label m_lbl_selection_example;
		private System.Windows.Forms.Label m_lbl_line2_colours;
		private PatternUI m_pattern_hl;
		private System.Windows.Forms.GroupBox m_group_startup;
		private System.Windows.Forms.CheckBox m_check_load_last_file;
		private System.Windows.Forms.CheckBox m_check_save_screen_loc;
		private PatternUI m_pattern_ft;
		private System.Windows.Forms.DataGridView m_grid_filter;
		private System.Windows.Forms.SplitContainer m_split_hl;
		private System.Windows.Forms.SplitContainer m_split_ft;
	}
}