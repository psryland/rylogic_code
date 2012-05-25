namespace Fishomatic2
{
	partial class MainForm
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
			this.m_btn_search_bounds = new System.Windows.Forms.Button();
			this.m_btn_gofish = new System.Windows.Forms.CheckBox();
			this.m_list_output = new System.Windows.Forms.ListView();
			this.m_status_strip = new System.Windows.Forms.StatusStrip();
			this.m_status = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_progress = new System.Windows.Forms.ToolStripProgressBar();
			this.m_lbl_move_delta = new System.Windows.Forms.Label();
			this.m_btn_options = new System.Windows.Forms.Button();
			this.m_table = new System.Windows.Forms.TableLayoutPanel();
			this.m_panel = new System.Windows.Forms.Panel();
			this.m_edit_move_threshold = new System.Windows.Forms.TextBox();
			this.m_scroll_move_threshold = new System.Windows.Forms.HScrollBar();
			this.m_menu = new System.Windows.Forms.MenuStrip();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_reset_options = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_file_exit = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_always_on_top = new System.Windows.Forms.ToolStripMenuItem();
			this.m_status_strip.SuspendLayout();
			this.m_table.SuspendLayout();
			this.m_panel.SuspendLayout();
			this.m_menu.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_btn_search_bounds
			// 
			this.m_btn_search_bounds.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_btn_search_bounds.Location = new System.Drawing.Point(72, 176);
			this.m_btn_search_bounds.Name = "m_btn_search_bounds";
			this.m_btn_search_bounds.Size = new System.Drawing.Size(64, 24);
			this.m_btn_search_bounds.TabIndex = 4;
			this.m_btn_search_bounds.Text = "Search Area";
			this.m_btn_search_bounds.UseVisualStyleBackColor = true;
			// 
			// m_btn_gofish
			// 
			this.m_btn_gofish.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_gofish.Appearance = System.Windows.Forms.Appearance.Button;
			this.m_table.SetColumnSpan(this.m_btn_gofish, 2);
			this.m_btn_gofish.Location = new System.Drawing.Point(3, 251);
			this.m_btn_gofish.Name = "m_btn_gofish";
			this.m_btn_gofish.Size = new System.Drawing.Size(133, 24);
			this.m_btn_gofish.TabIndex = 8;
			this.m_btn_gofish.Text = "Go Fish!";
			this.m_btn_gofish.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.m_btn_gofish.UseVisualStyleBackColor = true;
			// 
			// m_list_output
			// 
			this.m_list_output.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.m_table.SetColumnSpan(this.m_list_output, 2);
			this.m_list_output.Font = new System.Drawing.Font("Microsoft Sans Serif", 6F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_list_output.Location = new System.Drawing.Point(3, 3);
			this.m_list_output.Name = "m_list_output";
			this.m_list_output.Size = new System.Drawing.Size(133, 167);
			this.m_list_output.TabIndex = 9;
			this.m_list_output.UseCompatibleStateImageBehavior = false;
			this.m_list_output.View = System.Windows.Forms.View.Details;
			// 
			// m_status_strip
			// 
			this.m_status_strip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_progress,
            this.m_status});
			this.m_status_strip.Location = new System.Drawing.Point(0, 302);
			this.m_status_strip.Name = "m_status_strip";
			this.m_status_strip.Size = new System.Drawing.Size(139, 22);
			this.m_status_strip.TabIndex = 10;
			this.m_status_strip.Text = "statusStrip1";
			// 
			// m_status
			// 
			this.m_status.Name = "m_status";
			this.m_status.Size = new System.Drawing.Size(72, 17);
			this.m_status.Spring = true;
			this.m_status.Text = "Idle";
			this.m_status.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// m_progress
			// 
			this.m_progress.Name = "m_progress";
			this.m_progress.Size = new System.Drawing.Size(50, 16);
			this.m_progress.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
			// 
			// m_lbl_move_delta
			// 
			this.m_lbl_move_delta.AutoSize = true;
			this.m_lbl_move_delta.Location = new System.Drawing.Point(0, 2);
			this.m_lbl_move_delta.Name = "m_lbl_move_delta";
			this.m_lbl_move_delta.Size = new System.Drawing.Size(87, 13);
			this.m_lbl_move_delta.TabIndex = 11;
			this.m_lbl_move_delta.Text = "Move Threshold:";
			// 
			// m_btn_options
			// 
			this.m_btn_options.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_btn_options.Location = new System.Drawing.Point(3, 176);
			this.m_btn_options.Name = "m_btn_options";
			this.m_btn_options.Size = new System.Drawing.Size(63, 24);
			this.m_btn_options.TabIndex = 13;
			this.m_btn_options.Text = "Options";
			this.m_btn_options.UseVisualStyleBackColor = true;
			// 
			// m_table
			// 
			this.m_table.ColumnCount = 2;
			this.m_table.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.m_table.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.m_table.Controls.Add(this.m_panel, 0, 2);
			this.m_table.Controls.Add(this.m_list_output, 0, 0);
			this.m_table.Controls.Add(this.m_btn_gofish, 0, 3);
			this.m_table.Controls.Add(this.m_btn_options, 0, 1);
			this.m_table.Controls.Add(this.m_btn_search_bounds, 1, 1);
			this.m_table.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_table.Location = new System.Drawing.Point(0, 24);
			this.m_table.Name = "m_table";
			this.m_table.RowCount = 4;
			this.m_table.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
			this.m_table.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 45F));
			this.m_table.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
			this.m_table.Size = new System.Drawing.Size(139, 278);
			this.m_table.TabIndex = 14;
			// 
			// m_panel
			// 
			this.m_table.SetColumnSpan(this.m_panel, 2);
			this.m_panel.Controls.Add(this.m_edit_move_threshold);
			this.m_panel.Controls.Add(this.m_lbl_move_delta);
			this.m_panel.Controls.Add(this.m_scroll_move_threshold);
			this.m_panel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_panel.Location = new System.Drawing.Point(3, 206);
			this.m_panel.Name = "m_panel";
			this.m_panel.Size = new System.Drawing.Size(133, 39);
			this.m_panel.TabIndex = 15;
			// 
			// m_edit_move_threshold
			// 
			this.m_edit_move_threshold.Location = new System.Drawing.Point(87, -1);
			this.m_edit_move_threshold.Name = "m_edit_move_threshold";
			this.m_edit_move_threshold.Size = new System.Drawing.Size(35, 20);
			this.m_edit_move_threshold.TabIndex = 13;
			// 
			// m_scroll_move_threshold
			// 
			this.m_scroll_move_threshold.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.m_scroll_move_threshold.Location = new System.Drawing.Point(4, 20);
			this.m_scroll_move_threshold.Name = "m_scroll_move_threshold";
			this.m_scroll_move_threshold.Size = new System.Drawing.Size(124, 20);
			this.m_scroll_move_threshold.TabIndex = 12;
			// 
			// m_menu
			// 
			this.m_menu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
			this.m_menu.Location = new System.Drawing.Point(0, 0);
			this.m_menu.Name = "m_menu";
			this.m_menu.Size = new System.Drawing.Size(139, 24);
			this.m_menu.TabIndex = 15;
			this.m_menu.Text = "menuStrip1";
			// 
			// fileToolStripMenuItem
			// 
			this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_file_reset_options,
            this.m_menu_file_always_on_top,
            this.toolStripSeparator1,
            this.m_menu_file_exit});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
			this.fileToolStripMenuItem.Text = "&File";
			// 
			// m_menu_file_reset_options
			// 
			this.m_menu_file_reset_options.Name = "m_menu_file_reset_options";
			this.m_menu_file_reset_options.Size = new System.Drawing.Size(154, 22);
			this.m_menu_file_reset_options.Text = "&Reset Options";
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(151, 6);
			// 
			// m_menu_file_exit
			// 
			this.m_menu_file_exit.Name = "m_menu_file_exit";
			this.m_menu_file_exit.Size = new System.Drawing.Size(154, 22);
			this.m_menu_file_exit.Text = "E&xit";
			// 
			// m_menu_file_always_on_top
			// 
			this.m_menu_file_always_on_top.Name = "m_menu_file_always_on_top";
			this.m_menu_file_always_on_top.Size = new System.Drawing.Size(154, 22);
			this.m_menu_file_always_on_top.Text = "Always On &Top";
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(139, 324);
			this.Controls.Add(this.m_table);
			this.Controls.Add(this.m_status_strip);
			this.Controls.Add(this.m_menu);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.MainMenuStrip = this.m_menu;
			this.MinimumSize = new System.Drawing.Size(155, 358);
			this.Name = "MainForm";
			this.Text = "Fishomatic";
			this.m_status_strip.ResumeLayout(false);
			this.m_status_strip.PerformLayout();
			this.m_table.ResumeLayout(false);
			this.m_panel.ResumeLayout(false);
			this.m_panel.PerformLayout();
			this.m_menu.ResumeLayout(false);
			this.m_menu.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button m_btn_search_bounds;
		private System.Windows.Forms.CheckBox m_btn_gofish;
		private System.Windows.Forms.ListView m_list_output;
		private System.Windows.Forms.StatusStrip m_status_strip;
		private System.Windows.Forms.ToolStripStatusLabel m_status;
		private System.Windows.Forms.ToolStripProgressBar m_progress;
		private System.Windows.Forms.Label m_lbl_move_delta;
		private System.Windows.Forms.Button m_btn_options;
		private System.Windows.Forms.TableLayoutPanel m_table;
		private System.Windows.Forms.Panel m_panel;
		private System.Windows.Forms.TextBox m_edit_move_threshold;
		private System.Windows.Forms.HScrollBar m_scroll_move_threshold;
		private System.Windows.Forms.MenuStrip m_menu;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem m_menu_file_reset_options;
		private System.Windows.Forms.ToolStripMenuItem m_menu_file_exit;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem m_menu_file_always_on_top;
	}
}

