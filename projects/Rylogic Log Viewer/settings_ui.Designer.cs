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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsUI));
			this.m_tabctrl = new System.Windows.Forms.TabControl();
			this.m_tab_general = new System.Windows.Forms.TabPage();
			this.m_tab_highlight = new System.Windows.Forms.TabPage();
			this.m_grid_highlight = new System.Windows.Forms.DataGridView();
			this.m_tab_filter = new System.Windows.Forms.TabPage();
			this.m_grid_filter = new System.Windows.Forms.DataGridView();
			this.m_check_alternate_line_colour = new System.Windows.Forms.CheckBox();
			this.m_btn_fore_colour = new System.Windows.Forms.Button();
			this.m_btn_back_colour = new System.Windows.Forms.Button();
			this.m_tabctrl.SuspendLayout();
			this.m_tab_general.SuspendLayout();
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
			this.m_tabctrl.Size = new System.Drawing.Size(405, 374);
			this.m_tabctrl.TabIndex = 0;
			// 
			// m_tab_general
			// 
			this.m_tab_general.Controls.Add(this.m_btn_back_colour);
			this.m_tab_general.Controls.Add(this.m_btn_fore_colour);
			this.m_tab_general.Controls.Add(this.m_check_alternate_line_colour);
			this.m_tab_general.Location = new System.Drawing.Point(4, 22);
			this.m_tab_general.Name = "m_tab_general";
			this.m_tab_general.Padding = new System.Windows.Forms.Padding(3);
			this.m_tab_general.Size = new System.Drawing.Size(397, 348);
			this.m_tab_general.TabIndex = 0;
			this.m_tab_general.Text = "General";
			this.m_tab_general.UseVisualStyleBackColor = true;
			// 
			// m_tab_highlight
			// 
			this.m_tab_highlight.Controls.Add(this.m_grid_highlight);
			this.m_tab_highlight.Location = new System.Drawing.Point(4, 22);
			this.m_tab_highlight.Name = "m_tab_highlight";
			this.m_tab_highlight.Padding = new System.Windows.Forms.Padding(3);
			this.m_tab_highlight.Size = new System.Drawing.Size(397, 348);
			this.m_tab_highlight.TabIndex = 1;
			this.m_tab_highlight.Text = "Highlight";
			this.m_tab_highlight.UseVisualStyleBackColor = true;
			// 
			// m_grid_highlight
			// 
			this.m_grid_highlight.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_grid_highlight.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_grid_highlight.Location = new System.Drawing.Point(3, 6);
			this.m_grid_highlight.Name = "m_grid_highlight";
			this.m_grid_highlight.Size = new System.Drawing.Size(391, 318);
			this.m_grid_highlight.TabIndex = 1;
			// 
			// m_tab_filter
			// 
			this.m_tab_filter.Controls.Add(this.m_grid_filter);
			this.m_tab_filter.Location = new System.Drawing.Point(4, 22);
			this.m_tab_filter.Name = "m_tab_filter";
			this.m_tab_filter.Size = new System.Drawing.Size(397, 348);
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
			// m_check_alternate_line_colour
			// 
			this.m_check_alternate_line_colour.AutoSize = true;
			this.m_check_alternate_line_colour.Location = new System.Drawing.Point(8, 16);
			this.m_check_alternate_line_colour.Name = "m_check_alternate_line_colour";
			this.m_check_alternate_line_colour.Size = new System.Drawing.Size(129, 17);
			this.m_check_alternate_line_colour.TabIndex = 0;
			this.m_check_alternate_line_colour.Text = "Alternate Line Colours";
			this.m_check_alternate_line_colour.UseVisualStyleBackColor = true;
			// 
			// m_btn_fore_colour
			// 
			this.m_btn_fore_colour.Location = new System.Drawing.Point(143, 9);
			this.m_btn_fore_colour.Name = "m_btn_fore_colour";
			this.m_btn_fore_colour.Size = new System.Drawing.Size(112, 28);
			this.m_btn_fore_colour.TabIndex = 1;
			this.m_btn_fore_colour.Text = "Foreground Colour";
			this.m_btn_fore_colour.UseVisualStyleBackColor = true;
			// 
			// m_btn_back_colour
			// 
			this.m_btn_back_colour.Location = new System.Drawing.Point(261, 9);
			this.m_btn_back_colour.Name = "m_btn_back_colour";
			this.m_btn_back_colour.Size = new System.Drawing.Size(112, 28);
			this.m_btn_back_colour.TabIndex = 2;
			this.m_btn_back_colour.Text = "Background Colour";
			this.m_btn_back_colour.UseVisualStyleBackColor = true;
			// 
			// SettingsUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(405, 374);
			this.Controls.Add(this.m_tabctrl);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "SettingsUI";
			this.Text = "Options";
			this.m_tabctrl.ResumeLayout(false);
			this.m_tab_general.ResumeLayout(false);
			this.m_tab_general.PerformLayout();
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
		private System.Windows.Forms.Button m_btn_back_colour;
		private System.Windows.Forms.Button m_btn_fore_colour;
		private System.Windows.Forms.CheckBox m_check_alternate_line_colour;
		private System.Windows.Forms.DataGridView m_grid_filter;
	}
}