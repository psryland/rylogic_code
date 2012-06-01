namespace Rylogic_Log_Viewer
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
			Rylogic_Log_Viewer.Pattern pattern1 = new Rylogic_Log_Viewer.Pattern();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsUI));
			this.m_tabctrl = new System.Windows.Forms.TabControl();
			this.m_tab_general = new System.Windows.Forms.TabPage();
			this.m_group_log_text_colours = new System.Windows.Forms.GroupBox();
			this.m_lbl_line2_example = new System.Windows.Forms.Label();
			this.m_lbl_line1_example = new System.Windows.Forms.Label();
			this.m_lbl_selection_example = new System.Windows.Forms.Label();
			this.m_lbl_line2_colours = new System.Windows.Forms.Label();
			this.m_btn_line2_back_colour = new System.Windows.Forms.Button();
			this.m_lbl_line1_colours = new System.Windows.Forms.Label();
			this.m_btn_line1_back_colour = new System.Windows.Forms.Button();
			this.m_btn_selection_back_colour = new System.Windows.Forms.Button();
			this.m_lbl_selection_colour = new System.Windows.Forms.Label();
			this.m_btn_selection_fore_colour = new System.Windows.Forms.Button();
			this.m_btn_line2_fore_colour = new System.Windows.Forms.Button();
			this.m_btn_line1_fore_colour = new System.Windows.Forms.Button();
			this.m_check_alternate_line_colour = new System.Windows.Forms.CheckBox();
			this.m_tab_highlight = new System.Windows.Forms.TabPage();
			this.m_pattern_hl = new Rylogic_Log_Viewer.PatternUI();
			this.m_grid_highlight = new System.Windows.Forms.DataGridView();
			this.m_tab_filter = new System.Windows.Forms.TabPage();
			this.m_grid_filter = new System.Windows.Forms.DataGridView();
			this.m_tabctrl.SuspendLayout();
			this.m_tab_general.SuspendLayout();
			this.m_group_log_text_colours.SuspendLayout();
			this.m_tab_highlight.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_highlight)).BeginInit();
			this.m_tab_filter.SuspendLayout();
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
			this.m_tabctrl.Size = new System.Drawing.Size(479, 483);
			this.m_tabctrl.TabIndex = 0;
			// 
			// m_tab_general
			// 
			this.m_tab_general.Controls.Add(this.m_group_log_text_colours);
			this.m_tab_general.Location = new System.Drawing.Point(4, 22);
			this.m_tab_general.Name = "m_tab_general";
			this.m_tab_general.Padding = new System.Windows.Forms.Padding(3);
			this.m_tab_general.Size = new System.Drawing.Size(471, 457);
			this.m_tab_general.TabIndex = 0;
			this.m_tab_general.Text = "General";
			this.m_tab_general.UseVisualStyleBackColor = true;
			// 
			// m_group_log_text_colours
			// 
			this.m_group_log_text_colours.Controls.Add(this.m_lbl_line2_example);
			this.m_group_log_text_colours.Controls.Add(this.m_lbl_line1_example);
			this.m_group_log_text_colours.Controls.Add(this.m_lbl_selection_example);
			this.m_group_log_text_colours.Controls.Add(this.m_lbl_line2_colours);
			this.m_group_log_text_colours.Controls.Add(this.m_btn_line2_back_colour);
			this.m_group_log_text_colours.Controls.Add(this.m_lbl_line1_colours);
			this.m_group_log_text_colours.Controls.Add(this.m_btn_line1_back_colour);
			this.m_group_log_text_colours.Controls.Add(this.m_btn_selection_back_colour);
			this.m_group_log_text_colours.Controls.Add(this.m_lbl_selection_colour);
			this.m_group_log_text_colours.Controls.Add(this.m_btn_selection_fore_colour);
			this.m_group_log_text_colours.Controls.Add(this.m_btn_line2_fore_colour);
			this.m_group_log_text_colours.Controls.Add(this.m_btn_line1_fore_colour);
			this.m_group_log_text_colours.Controls.Add(this.m_check_alternate_line_colour);
			this.m_group_log_text_colours.Location = new System.Drawing.Point(6, 6);
			this.m_group_log_text_colours.Name = "m_group_log_text_colours";
			this.m_group_log_text_colours.Size = new System.Drawing.Size(462, 129);
			this.m_group_log_text_colours.TabIndex = 3;
			this.m_group_log_text_colours.TabStop = false;
			this.m_group_log_text_colours.Text = "Log Text Colours";
			// 
			// m_lbl_line2_example
			// 
			this.m_lbl_line2_example.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_lbl_line2_example.Location = new System.Drawing.Point(384, 92);
			this.m_lbl_line2_example.Name = "m_lbl_line2_example";
			this.m_lbl_line2_example.Size = new System.Drawing.Size(72, 28);
			this.m_lbl_line2_example.TabIndex = 13;
			this.m_lbl_line2_example.Text = "Example";
			this.m_lbl_line2_example.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// m_lbl_line1_example
			// 
			this.m_lbl_line1_example.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_lbl_line1_example.Location = new System.Drawing.Point(384, 48);
			this.m_lbl_line1_example.Name = "m_lbl_line1_example";
			this.m_lbl_line1_example.Size = new System.Drawing.Size(72, 28);
			this.m_lbl_line1_example.TabIndex = 12;
			this.m_lbl_line1_example.Text = "Example";
			this.m_lbl_line1_example.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// m_lbl_selection_example
			// 
			this.m_lbl_selection_example.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_lbl_selection_example.Location = new System.Drawing.Point(384, 14);
			this.m_lbl_selection_example.Name = "m_lbl_selection_example";
			this.m_lbl_selection_example.Size = new System.Drawing.Size(72, 28);
			this.m_lbl_selection_example.TabIndex = 11;
			this.m_lbl_selection_example.Text = "Example";
			this.m_lbl_selection_example.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// m_lbl_line2_colours
			// 
			this.m_lbl_line2_colours.AutoSize = true;
			this.m_lbl_line2_colours.Location = new System.Drawing.Point(27, 100);
			this.m_lbl_line2_colours.Name = "m_lbl_line2_colours";
			this.m_lbl_line2_colours.Size = new System.Drawing.Size(130, 13);
			this.m_lbl_line2_colours.TabIndex = 10;
			this.m_lbl_line2_colours.Text = "Alternate Log Text Colour:";
			// 
			// m_btn_line2_back_colour
			// 
			this.m_btn_line2_back_colour.Location = new System.Drawing.Point(272, 92);
			this.m_btn_line2_back_colour.Name = "m_btn_line2_back_colour";
			this.m_btn_line2_back_colour.Size = new System.Drawing.Size(106, 28);
			this.m_btn_line2_back_colour.TabIndex = 6;
			this.m_btn_line2_back_colour.Text = "Background Colour";
			this.m_btn_line2_back_colour.UseVisualStyleBackColor = true;
			// 
			// m_lbl_line1_colours
			// 
			this.m_lbl_line1_colours.AutoSize = true;
			this.m_lbl_line1_colours.Location = new System.Drawing.Point(72, 56);
			this.m_lbl_line1_colours.Name = "m_lbl_line1_colours";
			this.m_lbl_line1_colours.Size = new System.Drawing.Size(85, 13);
			this.m_lbl_line1_colours.TabIndex = 8;
			this.m_lbl_line1_colours.Text = "Log Text Colour:";
			// 
			// m_btn_line1_back_colour
			// 
			this.m_btn_line1_back_colour.Location = new System.Drawing.Point(272, 48);
			this.m_btn_line1_back_colour.Name = "m_btn_line1_back_colour";
			this.m_btn_line1_back_colour.Size = new System.Drawing.Size(106, 28);
			this.m_btn_line1_back_colour.TabIndex = 3;
			this.m_btn_line1_back_colour.Text = "Background Colour";
			this.m_btn_line1_back_colour.UseVisualStyleBackColor = true;
			// 
			// m_btn_selection_back_colour
			// 
			this.m_btn_selection_back_colour.Location = new System.Drawing.Point(272, 14);
			this.m_btn_selection_back_colour.Name = "m_btn_selection_back_colour";
			this.m_btn_selection_back_colour.Size = new System.Drawing.Size(106, 28);
			this.m_btn_selection_back_colour.TabIndex = 1;
			this.m_btn_selection_back_colour.Text = "Background Colour";
			this.m_btn_selection_back_colour.UseVisualStyleBackColor = true;
			// 
			// m_lbl_selection_colour
			// 
			this.m_lbl_selection_colour.AutoSize = true;
			this.m_lbl_selection_colour.Location = new System.Drawing.Point(70, 22);
			this.m_lbl_selection_colour.Name = "m_lbl_selection_colour";
			this.m_lbl_selection_colour.Size = new System.Drawing.Size(87, 13);
			this.m_lbl_selection_colour.TabIndex = 5;
			this.m_lbl_selection_colour.Text = "Selection Colour:";
			// 
			// m_btn_selection_fore_colour
			// 
			this.m_btn_selection_fore_colour.Location = new System.Drawing.Point(160, 14);
			this.m_btn_selection_fore_colour.Name = "m_btn_selection_fore_colour";
			this.m_btn_selection_fore_colour.Size = new System.Drawing.Size(106, 28);
			this.m_btn_selection_fore_colour.TabIndex = 0;
			this.m_btn_selection_fore_colour.Text = "Foreground Colour";
			this.m_btn_selection_fore_colour.UseVisualStyleBackColor = true;
			// 
			// m_btn_line2_fore_colour
			// 
			this.m_btn_line2_fore_colour.Location = new System.Drawing.Point(160, 92);
			this.m_btn_line2_fore_colour.Name = "m_btn_line2_fore_colour";
			this.m_btn_line2_fore_colour.Size = new System.Drawing.Size(106, 28);
			this.m_btn_line2_fore_colour.TabIndex = 5;
			this.m_btn_line2_fore_colour.Text = "Foreground Colour";
			this.m_btn_line2_fore_colour.UseVisualStyleBackColor = true;
			// 
			// m_btn_line1_fore_colour
			// 
			this.m_btn_line1_fore_colour.Location = new System.Drawing.Point(160, 48);
			this.m_btn_line1_fore_colour.Name = "m_btn_line1_fore_colour";
			this.m_btn_line1_fore_colour.Size = new System.Drawing.Size(106, 28);
			this.m_btn_line1_fore_colour.TabIndex = 2;
			this.m_btn_line1_fore_colour.Text = "Foreground Colour";
			this.m_btn_line1_fore_colour.UseVisualStyleBackColor = true;
			// 
			// m_check_alternate_line_colour
			// 
			this.m_check_alternate_line_colour.AutoSize = true;
			this.m_check_alternate_line_colour.Location = new System.Drawing.Point(6, 80);
			this.m_check_alternate_line_colour.Name = "m_check_alternate_line_colour";
			this.m_check_alternate_line_colour.Size = new System.Drawing.Size(129, 17);
			this.m_check_alternate_line_colour.TabIndex = 4;
			this.m_check_alternate_line_colour.Text = "Alternate Line Colours";
			this.m_check_alternate_line_colour.UseVisualStyleBackColor = true;
			// 
			// m_tab_highlight
			// 
			this.m_tab_highlight.Controls.Add(this.m_pattern_hl);
			this.m_tab_highlight.Controls.Add(this.m_grid_highlight);
			this.m_tab_highlight.Location = new System.Drawing.Point(4, 22);
			this.m_tab_highlight.Name = "m_tab_highlight";
			this.m_tab_highlight.Padding = new System.Windows.Forms.Padding(3);
			this.m_tab_highlight.Size = new System.Drawing.Size(471, 457);
			this.m_tab_highlight.TabIndex = 1;
			this.m_tab_highlight.Text = "Highlight";
			this.m_tab_highlight.UseVisualStyleBackColor = true;
			// 
			// m_pattern_hl
			// 
			this.m_pattern_hl.Location = new System.Drawing.Point(3, 3);
			this.m_pattern_hl.MinimumSize = new System.Drawing.Size(420, 78);
			this.m_pattern_hl.Name = "m_pattern_hl";
			pattern1.Active = true;
			pattern1.Expr = "";
			pattern1.IgnoreCase = false;
			pattern1.Invert = false;
			pattern1.IsRegex = false;
			this.m_pattern_hl.Pattern = pattern1;
			this.m_pattern_hl.Size = new System.Drawing.Size(465, 78);
			this.m_pattern_hl.TabIndex = 2;
			// 
			// m_grid_highlight
			// 
			this.m_grid_highlight.AllowUserToAddRows = false;
			this.m_grid_highlight.AllowUserToOrderColumns = true;
			this.m_grid_highlight.AllowUserToResizeRows = false;
			this.m_grid_highlight.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_grid_highlight.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_grid_highlight.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
			this.m_grid_highlight.Location = new System.Drawing.Point(3, 87);
			this.m_grid_highlight.MultiSelect = false;
			this.m_grid_highlight.Name = "m_grid_highlight";
			this.m_grid_highlight.RowHeadersWidth = 24;
			this.m_grid_highlight.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid_highlight.Size = new System.Drawing.Size(465, 367);
			this.m_grid_highlight.TabIndex = 1;
			// 
			// m_tab_filter
			// 
			this.m_tab_filter.Controls.Add(this.m_grid_filter);
			this.m_tab_filter.Location = new System.Drawing.Point(4, 22);
			this.m_tab_filter.Name = "m_tab_filter";
			this.m_tab_filter.Size = new System.Drawing.Size(471, 457);
			this.m_tab_filter.TabIndex = 2;
			this.m_tab_filter.Text = "Filter";
			this.m_tab_filter.UseVisualStyleBackColor = true;
			// 
			// m_grid_filter
			// 
			this.m_grid_filter.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_grid_filter.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_grid_filter.Location = new System.Drawing.Point(3, 3);
			this.m_grid_filter.Name = "m_grid_filter";
			this.m_grid_filter.Size = new System.Drawing.Size(391, 318);
			this.m_grid_filter.TabIndex = 2;
			// 
			// SettingsUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(479, 483);
			this.Controls.Add(this.m_tabctrl);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "SettingsUI";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Options";
			this.m_tabctrl.ResumeLayout(false);
			this.m_tab_general.ResumeLayout(false);
			this.m_group_log_text_colours.ResumeLayout(false);
			this.m_group_log_text_colours.PerformLayout();
			this.m_tab_highlight.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_grid_highlight)).EndInit();
			this.m_tab_filter.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_grid_filter)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TabControl m_tabctrl;
		private System.Windows.Forms.TabPage m_tab_general;
		private System.Windows.Forms.TabPage m_tab_highlight;
		private System.Windows.Forms.DataGridView m_grid_highlight;
		private System.Windows.Forms.TabPage m_tab_filter;
		private System.Windows.Forms.Button m_btn_line1_fore_colour;
		private System.Windows.Forms.CheckBox m_check_alternate_line_colour;
		private System.Windows.Forms.DataGridView m_grid_filter;
		private System.Windows.Forms.GroupBox m_group_log_text_colours;
		private System.Windows.Forms.Button m_btn_line2_fore_colour;
		private System.Windows.Forms.Button m_btn_selection_fore_colour;
		private System.Windows.Forms.Button m_btn_selection_back_colour;
		private System.Windows.Forms.Label m_lbl_selection_colour;
		private System.Windows.Forms.Label m_lbl_line1_colours;
		private System.Windows.Forms.Button m_btn_line1_back_colour;
		private System.Windows.Forms.Label m_lbl_line2_example;
		private System.Windows.Forms.Label m_lbl_line1_example;
		private System.Windows.Forms.Label m_lbl_selection_example;
		private System.Windows.Forms.Label m_lbl_line2_colours;
		private System.Windows.Forms.Button m_btn_line2_back_colour;
		private Rylogic_Log_Viewer.PatternUI m_pattern_hl;
	}
}