namespace RyLogViewer
{
	partial class Main
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
			this.m_toolstrip = new System.Windows.Forms.ToolStrip();
			this.m_btn_open_log = new System.Windows.Forms.ToolStripButton();
			this.m_btn_refresh = new System.Windows.Forms.ToolStripButton();
			this.m_sep = new System.Windows.Forms.ToolStripSeparator();
			this.m_btn_highlights = new System.Windows.Forms.ToolStripButton();
			this.m_btn_filters = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.m_check_tail = new System.Windows.Forms.ToolStripButton();
			this.m_menu = new System.Windows.Forms.MenuStrip();
			this.m_menu_file = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_open = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_close = new System.Windows.Forms.ToolStripMenuItem();
			this.m_sep1 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_file_recent = new System.Windows.Forms.ToolStripMenuItem();
			this.m_sep2 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_file_exit = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_edit = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_edit_selectall = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_edit_copy = new System.Windows.Forms.ToolStripMenuItem();
			this.m_sep3 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_edit_find = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_edit_find_next = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_edit_find_prev = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_tools = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_tools_alwaysontop = new System.Windows.Forms.ToolStripMenuItem();
			this.m_sep4 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_tools_highlights = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_tools_filters = new System.Windows.Forms.ToolStripMenuItem();
			this.m_sep5 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_tools_options = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_help = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_help_totd = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_help_about = new System.Windows.Forms.ToolStripMenuItem();
			this.m_status = new System.Windows.Forms.StatusStrip();
			this.m_lbl_file_size = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_toolstrip_cont = new System.Windows.Forms.ToolStripContainer();
			this.m_grid = new System.Windows.Forms.DataGridView();
			this.m_toolstrip.SuspendLayout();
			this.m_menu.SuspendLayout();
			this.m_status.SuspendLayout();
			this.m_toolstrip_cont.BottomToolStripPanel.SuspendLayout();
			this.m_toolstrip_cont.ContentPanel.SuspendLayout();
			this.m_toolstrip_cont.TopToolStripPanel.SuspendLayout();
			this.m_toolstrip_cont.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid)).BeginInit();
			this.SuspendLayout();
			// 
			// m_toolstrip
			// 
			this.m_toolstrip.Dock = System.Windows.Forms.DockStyle.None;
			this.m_toolstrip.ImageScalingSize = new System.Drawing.Size(20, 20);
			this.m_toolstrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_btn_open_log,
            this.m_btn_refresh,
            this.m_sep,
            this.m_btn_highlights,
            this.m_btn_filters,
            this.toolStripSeparator1,
            this.m_check_tail});
			this.m_toolstrip.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow;
			this.m_toolstrip.Location = new System.Drawing.Point(3, 0);
			this.m_toolstrip.Name = "m_toolstrip";
			this.m_toolstrip.Size = new System.Drawing.Size(175, 27);
			this.m_toolstrip.TabIndex = 0;
			this.m_toolstrip.Text = "toolStrip1";
			// 
			// m_btn_open_log
			// 
			this.m_btn_open_log.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_open_log.Image = global::RyLogViewer.Properties.Resources.my_documents;
			this.m_btn_open_log.ImageTransparentColor = System.Drawing.Color.Transparent;
			this.m_btn_open_log.Margin = new System.Windows.Forms.Padding(0);
			this.m_btn_open_log.Name = "m_btn_open_log";
			this.m_btn_open_log.Size = new System.Drawing.Size(24, 27);
			this.m_btn_open_log.Text = "Open Log File";
			// 
			// m_btn_refresh
			// 
			this.m_btn_refresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_refresh.Image = global::RyLogViewer.Properties.Resources.reload;
			this.m_btn_refresh.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_refresh.Name = "m_btn_refresh";
			this.m_btn_refresh.Size = new System.Drawing.Size(24, 24);
			this.m_btn_refresh.Text = "Refresh";
			// 
			// m_sep
			// 
			this.m_sep.Name = "m_sep";
			this.m_sep.Size = new System.Drawing.Size(6, 27);
			// 
			// m_btn_highlights
			// 
			this.m_btn_highlights.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_highlights.Image = global::RyLogViewer.Properties.Resources.highlight;
			this.m_btn_highlights.ImageTransparentColor = System.Drawing.Color.Transparent;
			this.m_btn_highlights.Name = "m_btn_highlights";
			this.m_btn_highlights.Size = new System.Drawing.Size(24, 24);
			this.m_btn_highlights.Text = "Highlights";
			// 
			// m_btn_filters
			// 
			this.m_btn_filters.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_filters.Image = global::RyLogViewer.Properties.Resources.filter;
			this.m_btn_filters.ImageTransparentColor = System.Drawing.Color.Transparent;
			this.m_btn_filters.Name = "m_btn_filters";
			this.m_btn_filters.Size = new System.Drawing.Size(24, 24);
			this.m_btn_filters.Text = "Filters";
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(6, 27);
			// 
			// m_check_tail
			// 
			this.m_check_tail.CheckOnClick = true;
			this.m_check_tail.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_check_tail.Image = global::RyLogViewer.Properties.Resources._3dowarrow;
			this.m_check_tail.ImageTransparentColor = System.Drawing.Color.Transparent;
			this.m_check_tail.Name = "m_check_tail";
			this.m_check_tail.Size = new System.Drawing.Size(24, 24);
			this.m_check_tail.Text = "Tail";
			// 
			// m_menu
			// 
			this.m_menu.Dock = System.Windows.Forms.DockStyle.None;
			this.m_menu.GripStyle = System.Windows.Forms.ToolStripGripStyle.Visible;
			this.m_menu.ImageScalingSize = new System.Drawing.Size(22, 22);
			this.m_menu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_file,
            this.m_menu_edit,
            this.m_menu_tools,
            this.m_menu_help});
			this.m_menu.Location = new System.Drawing.Point(3, 27);
			this.m_menu.Name = "m_menu";
			this.m_menu.Size = new System.Drawing.Size(180, 24);
			this.m_menu.Stretch = false;
			this.m_menu.TabIndex = 1;
			this.m_menu.Text = "m_menu";
			// 
			// m_menu_file
			// 
			this.m_menu_file.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_file_open,
            this.m_menu_file_close,
            this.m_sep1,
            this.m_menu_file_recent,
            this.m_sep2,
            this.m_menu_file_exit});
			this.m_menu_file.Name = "m_menu_file";
			this.m_menu_file.Size = new System.Drawing.Size(37, 20);
			this.m_menu_file.Text = "&File";
			// 
			// m_menu_file_open
			// 
			this.m_menu_file_open.Name = "m_menu_file_open";
			this.m_menu_file_open.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.m_menu_file_open.Size = new System.Drawing.Size(190, 22);
			this.m_menu_file_open.Text = "&Open Log File";
			// 
			// m_menu_file_close
			// 
			this.m_menu_file_close.Name = "m_menu_file_close";
			this.m_menu_file_close.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.W)));
			this.m_menu_file_close.Size = new System.Drawing.Size(190, 22);
			this.m_menu_file_close.Text = "&Close";
			// 
			// m_sep1
			// 
			this.m_sep1.Name = "m_sep1";
			this.m_sep1.Size = new System.Drawing.Size(187, 6);
			// 
			// m_menu_file_recent
			// 
			this.m_menu_file_recent.Name = "m_menu_file_recent";
			this.m_menu_file_recent.Size = new System.Drawing.Size(190, 22);
			this.m_menu_file_recent.Text = "&Recent Files";
			// 
			// m_sep2
			// 
			this.m_sep2.Name = "m_sep2";
			this.m_sep2.Size = new System.Drawing.Size(187, 6);
			// 
			// m_menu_file_exit
			// 
			this.m_menu_file_exit.Name = "m_menu_file_exit";
			this.m_menu_file_exit.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
			this.m_menu_file_exit.Size = new System.Drawing.Size(190, 22);
			this.m_menu_file_exit.Text = "E&xit";
			// 
			// m_menu_edit
			// 
			this.m_menu_edit.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_edit_selectall,
            this.m_menu_edit_copy,
            this.m_sep3,
            this.m_menu_edit_find,
            this.m_menu_edit_find_next,
            this.m_menu_edit_find_prev});
			this.m_menu_edit.Name = "m_menu_edit";
			this.m_menu_edit.Size = new System.Drawing.Size(39, 20);
			this.m_menu_edit.Text = "&Edit";
			// 
			// m_menu_edit_selectall
			// 
			this.m_menu_edit_selectall.Name = "m_menu_edit_selectall";
			this.m_menu_edit_selectall.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.A)));
			this.m_menu_edit_selectall.Size = new System.Drawing.Size(196, 22);
			this.m_menu_edit_selectall.Text = "Select &All";
			// 
			// m_menu_edit_copy
			// 
			this.m_menu_edit_copy.Name = "m_menu_edit_copy";
			this.m_menu_edit_copy.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
			this.m_menu_edit_copy.Size = new System.Drawing.Size(196, 22);
			this.m_menu_edit_copy.Text = "&Copy";
			// 
			// m_sep3
			// 
			this.m_sep3.Name = "m_sep3";
			this.m_sep3.Size = new System.Drawing.Size(193, 6);
			// 
			// m_menu_edit_find
			// 
			this.m_menu_edit_find.Name = "m_menu_edit_find";
			this.m_menu_edit_find.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F)));
			this.m_menu_edit_find.Size = new System.Drawing.Size(196, 22);
			this.m_menu_edit_find.Text = "&Find";
			// 
			// m_menu_edit_find_next
			// 
			this.m_menu_edit_find_next.Name = "m_menu_edit_find_next";
			this.m_menu_edit_find_next.ShortcutKeys = System.Windows.Forms.Keys.F3;
			this.m_menu_edit_find_next.Size = new System.Drawing.Size(196, 22);
			this.m_menu_edit_find_next.Text = "Find &Next";
			// 
			// m_menu_edit_find_prev
			// 
			this.m_menu_edit_find_prev.Name = "m_menu_edit_find_prev";
			this.m_menu_edit_find_prev.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.F3)));
			this.m_menu_edit_find_prev.Size = new System.Drawing.Size(196, 22);
			this.m_menu_edit_find_prev.Text = "Find &Previous";
			// 
			// m_menu_tools
			// 
			this.m_menu_tools.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_tools_alwaysontop,
            this.m_sep4,
            this.m_menu_tools_highlights,
            this.m_menu_tools_filters,
            this.m_sep5,
            this.m_menu_tools_options});
			this.m_menu_tools.Name = "m_menu_tools";
			this.m_menu_tools.Size = new System.Drawing.Size(48, 20);
			this.m_menu_tools.Text = "&Tools";
			// 
			// m_menu_tools_alwaysontop
			// 
			this.m_menu_tools_alwaysontop.Name = "m_menu_tools_alwaysontop";
			this.m_menu_tools_alwaysontop.Size = new System.Drawing.Size(154, 22);
			this.m_menu_tools_alwaysontop.Text = "Always On &Top";
			// 
			// m_sep4
			// 
			this.m_sep4.Name = "m_sep4";
			this.m_sep4.Size = new System.Drawing.Size(151, 6);
			// 
			// m_menu_tools_highlights
			// 
			this.m_menu_tools_highlights.Name = "m_menu_tools_highlights";
			this.m_menu_tools_highlights.Size = new System.Drawing.Size(154, 22);
			this.m_menu_tools_highlights.Text = "&Highlights";
			// 
			// m_menu_tools_filters
			// 
			this.m_menu_tools_filters.Name = "m_menu_tools_filters";
			this.m_menu_tools_filters.Size = new System.Drawing.Size(154, 22);
			this.m_menu_tools_filters.Text = "&Filters";
			// 
			// m_sep5
			// 
			this.m_sep5.Name = "m_sep5";
			this.m_sep5.Size = new System.Drawing.Size(151, 6);
			// 
			// m_menu_tools_options
			// 
			this.m_menu_tools_options.Name = "m_menu_tools_options";
			this.m_menu_tools_options.Size = new System.Drawing.Size(154, 22);
			this.m_menu_tools_options.Text = "&Options";
			// 
			// m_menu_help
			// 
			this.m_menu_help.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_help_totd,
            this.toolStripSeparator2,
            this.m_menu_help_about});
			this.m_menu_help.Name = "m_menu_help";
			this.m_menu_help.Size = new System.Drawing.Size(44, 20);
			this.m_menu_help.Text = "&Help";
			// 
			// m_menu_help_totd
			// 
			this.m_menu_help_totd.Name = "m_menu_help_totd";
			this.m_menu_help_totd.Size = new System.Drawing.Size(148, 22);
			this.m_menu_help_totd.Text = "&Tip of the Day";
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(145, 6);
			// 
			// m_menu_help_about
			// 
			this.m_menu_help_about.Name = "m_menu_help_about";
			this.m_menu_help_about.Size = new System.Drawing.Size(148, 22);
			this.m_menu_help_about.Text = "&About";
			// 
			// m_status
			// 
			this.m_status.Dock = System.Windows.Forms.DockStyle.None;
			this.m_status.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_lbl_file_size});
			this.m_status.Location = new System.Drawing.Point(0, 0);
			this.m_status.Name = "m_status";
			this.m_status.Size = new System.Drawing.Size(593, 22);
			this.m_status.TabIndex = 3;
			this.m_status.Text = "statusStrip1";
			// 
			// m_lbl_file_size
			// 
			this.m_lbl_file_size.Name = "m_lbl_file_size";
			this.m_lbl_file_size.Size = new System.Drawing.Size(124, 17);
			this.m_lbl_file_size.Text = "Size: 2147483647 bytes";
			// 
			// m_toolstrip_cont
			// 
			// 
			// m_toolstrip_cont.BottomToolStripPanel
			// 
			this.m_toolstrip_cont.BottomToolStripPanel.Controls.Add(this.m_status);
			// 
			// m_toolstrip_cont.ContentPanel
			// 
			this.m_toolstrip_cont.ContentPanel.AutoScroll = true;
			this.m_toolstrip_cont.ContentPanel.Controls.Add(this.m_grid);
			this.m_toolstrip_cont.ContentPanel.Padding = new System.Windows.Forms.Padding(3);
			this.m_toolstrip_cont.ContentPanel.Size = new System.Drawing.Size(593, 422);
			this.m_toolstrip_cont.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_toolstrip_cont.Location = new System.Drawing.Point(0, 0);
			this.m_toolstrip_cont.Name = "m_toolstrip_cont";
			this.m_toolstrip_cont.Size = new System.Drawing.Size(593, 495);
			this.m_toolstrip_cont.TabIndex = 6;
			this.m_toolstrip_cont.Text = "m_toolstrip_cont";
			// 
			// m_toolstrip_cont.TopToolStripPanel
			// 
			this.m_toolstrip_cont.TopToolStripPanel.Controls.Add(this.m_toolstrip);
			this.m_toolstrip_cont.TopToolStripPanel.Controls.Add(this.m_menu);
			// 
			// m_grid
			// 
			this.m_grid.AllowUserToAddRows = false;
			this.m_grid.AllowUserToDeleteRows = false;
			this.m_grid.AllowUserToOrderColumns = true;
			this.m_grid.AllowUserToResizeRows = false;
			this.m_grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_grid.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
			this.m_grid.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
			this.m_grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_grid.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_grid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
			this.m_grid.Location = new System.Drawing.Point(3, 3);
			this.m_grid.Name = "m_grid";
			this.m_grid.ReadOnly = true;
			this.m_grid.RowHeadersVisible = false;
			this.m_grid.RowTemplate.Height = 18;
			this.m_grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid.Size = new System.Drawing.Size(587, 416);
			this.m_grid.TabIndex = 3;
			this.m_grid.VirtualMode = true;
			// 
			// Main
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(593, 495);
			this.Controls.Add(this.m_toolstrip_cont);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.m_menu;
			this.MinimumSize = new System.Drawing.Size(200, 220);
			this.Name = "Main";
			this.Text = "Rylogic Log Viewer";
			this.m_toolstrip.ResumeLayout(false);
			this.m_toolstrip.PerformLayout();
			this.m_menu.ResumeLayout(false);
			this.m_menu.PerformLayout();
			this.m_status.ResumeLayout(false);
			this.m_status.PerformLayout();
			this.m_toolstrip_cont.BottomToolStripPanel.ResumeLayout(false);
			this.m_toolstrip_cont.BottomToolStripPanel.PerformLayout();
			this.m_toolstrip_cont.ContentPanel.ResumeLayout(false);
			this.m_toolstrip_cont.TopToolStripPanel.ResumeLayout(false);
			this.m_toolstrip_cont.TopToolStripPanel.PerformLayout();
			this.m_toolstrip_cont.ResumeLayout(false);
			this.m_toolstrip_cont.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ToolStrip m_toolstrip;
		private System.Windows.Forms.ToolStripButton m_btn_open_log;
		private System.Windows.Forms.MenuStrip m_menu;
		private System.Windows.Forms.ToolStripMenuItem m_menu_file;
		private System.Windows.Forms.ToolStripMenuItem m_menu_file_open;
		private System.Windows.Forms.ToolStripSeparator m_sep1;
		private System.Windows.Forms.ToolStripMenuItem m_menu_file_exit;
		private System.Windows.Forms.StatusStrip m_status;
		private System.Windows.Forms.ToolStripMenuItem m_menu_file_recent;
		private System.Windows.Forms.ToolStripSeparator m_sep2;
		private System.Windows.Forms.ToolStripButton m_check_tail;
		private System.Windows.Forms.ToolStripContainer m_toolstrip_cont;
		private System.Windows.Forms.DataGridView m_grid;
		private System.Windows.Forms.ToolStripSeparator m_sep;
		private System.Windows.Forms.ToolStripButton m_btn_highlights;
		private System.Windows.Forms.ToolStripButton m_btn_filters;
		private System.Windows.Forms.ToolStripMenuItem m_menu_file_close;
		private System.Windows.Forms.ToolStripMenuItem m_menu_edit;
		private System.Windows.Forms.ToolStripMenuItem m_menu_edit_selectall;
		private System.Windows.Forms.ToolStripMenuItem m_menu_edit_copy;
		private System.Windows.Forms.ToolStripSeparator m_sep3;
		private System.Windows.Forms.ToolStripMenuItem m_menu_edit_find;
		private System.Windows.Forms.ToolStripMenuItem m_menu_edit_find_next;
		private System.Windows.Forms.ToolStripMenuItem m_menu_edit_find_prev;
		private System.Windows.Forms.ToolStripMenuItem m_menu_tools;
		private System.Windows.Forms.ToolStripMenuItem m_menu_tools_alwaysontop;
		private System.Windows.Forms.ToolStripSeparator m_sep4;
		private System.Windows.Forms.ToolStripMenuItem m_menu_tools_highlights;
		private System.Windows.Forms.ToolStripMenuItem m_menu_tools_filters;
		private System.Windows.Forms.ToolStripSeparator m_sep5;
		private System.Windows.Forms.ToolStripMenuItem m_menu_tools_options;
		private System.Windows.Forms.ToolStripMenuItem m_menu_help;
		private System.Windows.Forms.ToolStripMenuItem m_menu_help_about;
		private System.Windows.Forms.ToolStripStatusLabel m_lbl_file_size;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripButton m_btn_refresh;
		private System.Windows.Forms.ToolStripMenuItem m_menu_help_totd;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
	}
}

