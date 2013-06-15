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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
			this.m_toolstrip = new System.Windows.Forms.ToolStrip();
			this.m_btn_open_log = new System.Windows.Forms.ToolStripButton();
			this.m_btn_refresh = new System.Windows.Forms.ToolStripButton();
			this.m_sep = new System.Windows.Forms.ToolStripSeparator();
			this.m_btn_highlights = new System.Windows.Forms.ToolStripButton();
			this.m_btn_filters = new System.Windows.Forms.ToolStripButton();
			this.m_btn_transforms = new System.Windows.Forms.ToolStripButton();
			this.m_btn_actions = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator9 = new System.Windows.Forms.ToolStripSeparator();
			this.m_btn_options = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator12 = new System.Windows.Forms.ToolStripSeparator();
			this.m_btn_bookmarks = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.m_btn_jump_to_start = new System.Windows.Forms.ToolStripButton();
			this.m_btn_jump_to_end = new System.Windows.Forms.ToolStripButton();
			this.m_btn_tail = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
			this.m_btn_watch = new System.Windows.Forms.ToolStripButton();
			this.m_btn_additive = new System.Windows.Forms.ToolStripButton();
			this.m_menu = new System.Windows.Forms.MenuStrip();
			this.m_menu_file = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_open = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_wizards = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_wizards_androidlogcat = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator17 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_file_open_stdout = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_open_serial_port = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_open_network = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_open_named_pipe = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator16 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_file_close = new System.Windows.Forms.ToolStripMenuItem();
			this.m_sep1 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_file_export = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
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
			this.toolStripSeparator13 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_edit_toggle_bookmark = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_edit_next_bookmark = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_edit_prev_bookmark = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_edit_clearall_bookmarks = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_edit_bookmarks = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_encoding = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_encoding_detect = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_encoding_ascii = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_encoding_utf8 = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_encoding_ucs2_littleendian = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_encoding_ucs2_bigendian = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_line_ending = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_line_ending_detect = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_line_ending_cr = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_line_ending_crlf = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_line_ending_lf = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_line_ending_custom = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_tools = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_tools_alwaysontop = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_tools_ghost_mode = new System.Windows.Forms.ToolStripMenuItem();
			this.m_sep4 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_tools_clear_log_file = new System.Windows.Forms.ToolStripMenuItem();
			this.m_sep5 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_tools_highlights = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_tools_filters = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_tools_transforms = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_tools_actions = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_tools_options = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_help = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_help_totd = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_help_check_for_updates = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator11 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_help_visit_store = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_help_register = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_help_about = new System.Windows.Forms.ToolStripMenuItem();
			this.m_status = new System.Windows.Forms.StatusStrip();
			this.m_status_filesize = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_status_line_end = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_status_encoding = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_status_spring = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_status_message = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_status_progress = new System.Windows.Forms.ToolStripProgressBar();
			this.m_toolstrip_cont = new System.Windows.Forms.ToolStripContainer();
			this.m_table = new System.Windows.Forms.TableLayoutPanel();
			this.m_cmenu_grid = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.m_cmenu_select_all = new System.Windows.Forms.ToolStripMenuItem();
			this.m_cmenu_copy = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator15 = new System.Windows.Forms.ToolStripSeparator();
			this.m_cmenu_clear_log = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator10 = new System.Windows.Forms.ToolStripSeparator();
			this.m_cmenu_highlight_row = new System.Windows.Forms.ToolStripMenuItem();
			this.m_cmenu_filter_row = new System.Windows.Forms.ToolStripMenuItem();
			this.m_cmenu_transform_row = new System.Windows.Forms.ToolStripMenuItem();
			this.m_cmenu_action_row = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.m_cmenu_find_next = new System.Windows.Forms.ToolStripMenuItem();
			this.m_cmenu_find_prev = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator14 = new System.Windows.Forms.ToolStripSeparator();
			this.m_cmenu_toggle_bookmark = new System.Windows.Forms.ToolStripMenuItem();
			this.m_grid = new DGV();
			this.m_scroll_file = new RyLogViewer.SubRangeScroll();
			this.m_toolstrip.SuspendLayout();
			this.m_menu.SuspendLayout();
			this.m_status.SuspendLayout();
			this.m_toolstrip_cont.BottomToolStripPanel.SuspendLayout();
			this.m_toolstrip_cont.ContentPanel.SuspendLayout();
			this.m_toolstrip_cont.TopToolStripPanel.SuspendLayout();
			this.m_toolstrip_cont.SuspendLayout();
			this.m_table.SuspendLayout();
			this.m_cmenu_grid.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid)).BeginInit();
			this.SuspendLayout();
			// 
			// m_toolstrip
			// 
			this.m_toolstrip.Dock = System.Windows.Forms.DockStyle.None;
			this.m_toolstrip.ImageScalingSize = new System.Drawing.Size(24, 24);
			this.m_toolstrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_btn_open_log,
            this.m_btn_refresh,
            this.m_sep,
            this.m_btn_highlights,
            this.m_btn_filters,
            this.m_btn_transforms,
            this.m_btn_actions,
            this.toolStripSeparator9,
            this.m_btn_options,
            this.toolStripSeparator12,
            this.m_btn_bookmarks,
            this.toolStripSeparator1,
            this.m_btn_jump_to_start,
            this.m_btn_jump_to_end,
            this.m_btn_tail,
            this.toolStripSeparator8,
            this.m_btn_watch,
            this.m_btn_additive});
			this.m_toolstrip.Location = new System.Drawing.Point(3, 24);
			this.m_toolstrip.Name = "m_toolstrip";
			this.m_toolstrip.Size = new System.Drawing.Size(406, 31);
			this.m_toolstrip.TabIndex = 0;
			// 
			// m_btn_open_log
			// 
			this.m_btn_open_log.AutoSize = false;
			this.m_btn_open_log.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_open_log.Image = global::RyLogViewer.Properties.Resources.folder_with_file;
			this.m_btn_open_log.ImageTransparentColor = System.Drawing.Color.Transparent;
			this.m_btn_open_log.Margin = new System.Windows.Forms.Padding(0);
			this.m_btn_open_log.Name = "m_btn_open_log";
			this.m_btn_open_log.Size = new System.Drawing.Size(28, 28);
			this.m_btn_open_log.Text = "Open Log File";
			// 
			// m_btn_refresh
			// 
			this.m_btn_refresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_refresh.Image = global::RyLogViewer.Properties.Resources.Refresh;
			this.m_btn_refresh.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_refresh.Name = "m_btn_refresh";
			this.m_btn_refresh.Size = new System.Drawing.Size(28, 28);
			this.m_btn_refresh.Text = "Refresh";
			// 
			// m_sep
			// 
			this.m_sep.Name = "m_sep";
			this.m_sep.Size = new System.Drawing.Size(6, 31);
			// 
			// m_btn_highlights
			// 
			this.m_btn_highlights.CheckOnClick = true;
			this.m_btn_highlights.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_highlights.Image = global::RyLogViewer.Properties.Resources.highlight;
			this.m_btn_highlights.ImageTransparentColor = System.Drawing.Color.Transparent;
			this.m_btn_highlights.Name = "m_btn_highlights";
			this.m_btn_highlights.Size = new System.Drawing.Size(28, 28);
			this.m_btn_highlights.Text = "Highlights";
			// 
			// m_btn_filters
			// 
			this.m_btn_filters.CheckOnClick = true;
			this.m_btn_filters.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_filters.Image = global::RyLogViewer.Properties.Resources.filter;
			this.m_btn_filters.ImageTransparentColor = System.Drawing.Color.Transparent;
			this.m_btn_filters.Name = "m_btn_filters";
			this.m_btn_filters.Size = new System.Drawing.Size(28, 28);
			this.m_btn_filters.Text = "Filters";
			// 
			// m_btn_transforms
			// 
			this.m_btn_transforms.CheckOnClick = true;
			this.m_btn_transforms.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_transforms.Image = global::RyLogViewer.Properties.Resources.exchange;
			this.m_btn_transforms.ImageTransparentColor = System.Drawing.Color.Transparent;
			this.m_btn_transforms.Name = "m_btn_transforms";
			this.m_btn_transforms.Size = new System.Drawing.Size(28, 28);
			this.m_btn_transforms.Text = "Transforms";
			// 
			// m_btn_actions
			// 
			this.m_btn_actions.CheckOnClick = true;
			this.m_btn_actions.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_actions.Image = global::RyLogViewer.Properties.Resources.execute;
			this.m_btn_actions.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_actions.Name = "m_btn_actions";
			this.m_btn_actions.Size = new System.Drawing.Size(28, 28);
			this.m_btn_actions.Text = "Actions";
			// 
			// toolStripSeparator9
			// 
			this.toolStripSeparator9.Name = "toolStripSeparator9";
			this.toolStripSeparator9.Size = new System.Drawing.Size(6, 31);
			// 
			// m_btn_options
			// 
			this.m_btn_options.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_options.Image = global::RyLogViewer.Properties.Resources.options;
			this.m_btn_options.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_options.Name = "m_btn_options";
			this.m_btn_options.Size = new System.Drawing.Size(28, 28);
			this.m_btn_options.Text = "Show Options";
			// 
			// toolStripSeparator12
			// 
			this.toolStripSeparator12.Name = "toolStripSeparator12";
			this.toolStripSeparator12.Size = new System.Drawing.Size(6, 31);
			// 
			// m_btn_bookmarks
			// 
			this.m_btn_bookmarks.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_bookmarks.Image = global::RyLogViewer.Properties.Resources.bookmark;
			this.m_btn_bookmarks.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_bookmarks.Name = "m_btn_bookmarks";
			this.m_btn_bookmarks.Size = new System.Drawing.Size(28, 28);
			this.m_btn_bookmarks.Text = "Bookmarks";
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(6, 31);
			// 
			// m_btn_jump_to_start
			// 
			this.m_btn_jump_to_start.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_jump_to_start.Image = global::RyLogViewer.Properties.Resources.green_up;
			this.m_btn_jump_to_start.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_jump_to_start.Name = "m_btn_jump_to_start";
			this.m_btn_jump_to_start.Size = new System.Drawing.Size(28, 28);
			this.m_btn_jump_to_start.Text = "File Start";
			this.m_btn_jump_to_start.ToolTipText = "Jump to the file start";
			// 
			// m_btn_jump_to_end
			// 
			this.m_btn_jump_to_end.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_jump_to_end.Image = global::RyLogViewer.Properties.Resources.green_down;
			this.m_btn_jump_to_end.ImageTransparentColor = System.Drawing.Color.Transparent;
			this.m_btn_jump_to_end.Name = "m_btn_jump_to_end";
			this.m_btn_jump_to_end.Size = new System.Drawing.Size(28, 28);
			this.m_btn_jump_to_end.Text = "File End";
			this.m_btn_jump_to_end.ToolTipText = "Jump to the file end";
			// 
			// m_btn_tail
			// 
			this.m_btn_tail.CheckOnClick = true;
			this.m_btn_tail.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_tail.Image = global::RyLogViewer.Properties.Resources.bottom;
			this.m_btn_tail.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_tail.Name = "m_btn_tail";
			this.m_btn_tail.Size = new System.Drawing.Size(28, 28);
			this.m_btn_tail.Text = "Tail Mode";
			// 
			// toolStripSeparator8
			// 
			this.toolStripSeparator8.Name = "toolStripSeparator8";
			this.toolStripSeparator8.Size = new System.Drawing.Size(6, 31);
			// 
			// m_btn_watch
			// 
			this.m_btn_watch.CheckOnClick = true;
			this.m_btn_watch.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_watch.Image = global::RyLogViewer.Properties.Resources.Eyeball;
			this.m_btn_watch.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_watch.Name = "m_btn_watch";
			this.m_btn_watch.Size = new System.Drawing.Size(28, 28);
			this.m_btn_watch.Text = "Live Update";
			// 
			// m_btn_additive
			// 
			this.m_btn_additive.CheckOnClick = true;
			this.m_btn_additive.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_additive.Image = global::RyLogViewer.Properties.Resources.edit_add;
			this.m_btn_additive.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_additive.Name = "m_btn_additive";
			this.m_btn_additive.Size = new System.Drawing.Size(28, 28);
			this.m_btn_additive.Text = "Additive Only";
			// 
			// m_menu
			// 
			this.m_menu.Dock = System.Windows.Forms.DockStyle.None;
			this.m_menu.GripStyle = System.Windows.Forms.ToolStripGripStyle.Visible;
			this.m_menu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_file,
            this.m_menu_edit,
            this.m_menu_encoding,
            this.m_menu_line_ending,
            this.m_menu_tools,
            this.m_menu_help});
			this.m_menu.Location = new System.Drawing.Point(0, 0);
			this.m_menu.Name = "m_menu";
			this.m_menu.Size = new System.Drawing.Size(593, 24);
			this.m_menu.TabIndex = 1;
			this.m_menu.Text = "m_menu";
			// 
			// m_menu_file
			// 
			this.m_menu_file.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_file_open,
            this.m_menu_file_wizards,
            this.toolStripSeparator17,
            this.m_menu_file_open_stdout,
            this.m_menu_file_open_serial_port,
            this.m_menu_file_open_network,
            this.m_menu_file_open_named_pipe,
            this.toolStripSeparator16,
            this.m_menu_file_close,
            this.m_sep1,
            this.m_menu_file_export,
            this.toolStripSeparator5,
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
			this.m_menu_file_open.Size = new System.Drawing.Size(216, 22);
			this.m_menu_file_open.Text = "&Open Log File";
			// 
			// m_menu_file_wizards
			// 
			this.m_menu_file_wizards.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_file_wizards_androidlogcat});
			this.m_menu_file_wizards.Name = "m_menu_file_wizards";
			this.m_menu_file_wizards.Size = new System.Drawing.Size(216, 22);
			this.m_menu_file_wizards.Text = "&Wizards";
			// 
			// m_menu_file_wizards_androidlogcat
			// 
			this.m_menu_file_wizards_androidlogcat.Name = "m_menu_file_wizards_androidlogcat";
			this.m_menu_file_wizards_androidlogcat.Size = new System.Drawing.Size(156, 22);
			this.m_menu_file_wizards_androidlogcat.Text = "Android Logcat";
			// 
			// toolStripSeparator17
			// 
			this.toolStripSeparator17.Name = "toolStripSeparator17";
			this.toolStripSeparator17.Size = new System.Drawing.Size(213, 6);
			// 
			// m_menu_file_open_stdout
			// 
			this.m_menu_file_open_stdout.Name = "m_menu_file_open_stdout";
			this.m_menu_file_open_stdout.Size = new System.Drawing.Size(216, 22);
			this.m_menu_file_open_stdout.Text = "Log &Program Output...";
			// 
			// m_menu_file_open_serial_port
			// 
			this.m_menu_file_open_serial_port.Name = "m_menu_file_open_serial_port";
			this.m_menu_file_open_serial_port.Size = new System.Drawing.Size(216, 22);
			this.m_menu_file_open_serial_port.Text = "Log &Serial Port...";
			// 
			// m_menu_file_open_network
			// 
			this.m_menu_file_open_network.Name = "m_menu_file_open_network";
			this.m_menu_file_open_network.Size = new System.Drawing.Size(216, 22);
			this.m_menu_file_open_network.Text = "Log Ne&twork Connection...";
			// 
			// m_menu_file_open_named_pipe
			// 
			this.m_menu_file_open_named_pipe.Name = "m_menu_file_open_named_pipe";
			this.m_menu_file_open_named_pipe.Size = new System.Drawing.Size(216, 22);
			this.m_menu_file_open_named_pipe.Text = "Log &Named Pipe...";
			// 
			// toolStripSeparator16
			// 
			this.toolStripSeparator16.Name = "toolStripSeparator16";
			this.toolStripSeparator16.Size = new System.Drawing.Size(213, 6);
			// 
			// m_menu_file_close
			// 
			this.m_menu_file_close.Name = "m_menu_file_close";
			this.m_menu_file_close.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.W)));
			this.m_menu_file_close.Size = new System.Drawing.Size(216, 22);
			this.m_menu_file_close.Text = "&Close";
			// 
			// m_sep1
			// 
			this.m_sep1.Name = "m_sep1";
			this.m_sep1.Size = new System.Drawing.Size(213, 6);
			// 
			// m_menu_file_export
			// 
			this.m_menu_file_export.Name = "m_menu_file_export";
			this.m_menu_file_export.Size = new System.Drawing.Size(216, 22);
			this.m_menu_file_export.Text = "&Export...";
			// 
			// toolStripSeparator5
			// 
			this.toolStripSeparator5.Name = "toolStripSeparator5";
			this.toolStripSeparator5.Size = new System.Drawing.Size(213, 6);
			// 
			// m_menu_file_recent
			// 
			this.m_menu_file_recent.Name = "m_menu_file_recent";
			this.m_menu_file_recent.Size = new System.Drawing.Size(216, 22);
			this.m_menu_file_recent.Text = "&Recent Files";
			// 
			// m_sep2
			// 
			this.m_sep2.Name = "m_sep2";
			this.m_sep2.Size = new System.Drawing.Size(213, 6);
			// 
			// m_menu_file_exit
			// 
			this.m_menu_file_exit.Name = "m_menu_file_exit";
			this.m_menu_file_exit.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
			this.m_menu_file_exit.Size = new System.Drawing.Size(216, 22);
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
            this.m_menu_edit_find_prev,
            this.toolStripSeparator13,
            this.m_menu_edit_toggle_bookmark,
            this.m_menu_edit_next_bookmark,
            this.m_menu_edit_prev_bookmark,
            this.m_menu_edit_clearall_bookmarks,
            this.m_menu_edit_bookmarks});
			this.m_menu_edit.Name = "m_menu_edit";
			this.m_menu_edit.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F2)));
			this.m_menu_edit.Size = new System.Drawing.Size(39, 20);
			this.m_menu_edit.Text = "&Edit";
			// 
			// m_menu_edit_selectall
			// 
			this.m_menu_edit_selectall.Name = "m_menu_edit_selectall";
			this.m_menu_edit_selectall.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.A)));
			this.m_menu_edit_selectall.Size = new System.Drawing.Size(258, 22);
			this.m_menu_edit_selectall.Text = "Select &All";
			// 
			// m_menu_edit_copy
			// 
			this.m_menu_edit_copy.Name = "m_menu_edit_copy";
			this.m_menu_edit_copy.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
			this.m_menu_edit_copy.Size = new System.Drawing.Size(258, 22);
			this.m_menu_edit_copy.Text = "&Copy";
			// 
			// m_sep3
			// 
			this.m_sep3.Name = "m_sep3";
			this.m_sep3.Size = new System.Drawing.Size(255, 6);
			// 
			// m_menu_edit_find
			// 
			this.m_menu_edit_find.Name = "m_menu_edit_find";
			this.m_menu_edit_find.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F)));
			this.m_menu_edit_find.Size = new System.Drawing.Size(258, 22);
			this.m_menu_edit_find.Text = "&Find...";
			// 
			// m_menu_edit_find_next
			// 
			this.m_menu_edit_find_next.Name = "m_menu_edit_find_next";
			this.m_menu_edit_find_next.ShortcutKeys = System.Windows.Forms.Keys.F3;
			this.m_menu_edit_find_next.Size = new System.Drawing.Size(258, 22);
			this.m_menu_edit_find_next.Text = "Find &Next";
			// 
			// m_menu_edit_find_prev
			// 
			this.m_menu_edit_find_prev.Name = "m_menu_edit_find_prev";
			this.m_menu_edit_find_prev.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.F3)));
			this.m_menu_edit_find_prev.Size = new System.Drawing.Size(258, 22);
			this.m_menu_edit_find_prev.Text = "Find &Previous";
			// 
			// toolStripSeparator13
			// 
			this.toolStripSeparator13.Name = "toolStripSeparator13";
			this.toolStripSeparator13.Size = new System.Drawing.Size(255, 6);
			// 
			// m_menu_edit_toggle_bookmark
			// 
			this.m_menu_edit_toggle_bookmark.Name = "m_menu_edit_toggle_bookmark";
			this.m_menu_edit_toggle_bookmark.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F2)));
			this.m_menu_edit_toggle_bookmark.Size = new System.Drawing.Size(258, 22);
			this.m_menu_edit_toggle_bookmark.Text = "Toggle Bookmark";
			// 
			// m_menu_edit_next_bookmark
			// 
			this.m_menu_edit_next_bookmark.Name = "m_menu_edit_next_bookmark";
			this.m_menu_edit_next_bookmark.ShortcutKeys = System.Windows.Forms.Keys.F2;
			this.m_menu_edit_next_bookmark.Size = new System.Drawing.Size(258, 22);
			this.m_menu_edit_next_bookmark.Text = "Next Bookmark";
			// 
			// m_menu_edit_prev_bookmark
			// 
			this.m_menu_edit_prev_bookmark.Name = "m_menu_edit_prev_bookmark";
			this.m_menu_edit_prev_bookmark.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.F2)));
			this.m_menu_edit_prev_bookmark.Size = new System.Drawing.Size(258, 22);
			this.m_menu_edit_prev_bookmark.Text = "Previous Bookmark";
			// 
			// m_menu_edit_clearall_bookmarks
			// 
			this.m_menu_edit_clearall_bookmarks.Name = "m_menu_edit_clearall_bookmarks";
			this.m_menu_edit_clearall_bookmarks.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.F2)));
			this.m_menu_edit_clearall_bookmarks.Size = new System.Drawing.Size(258, 22);
			this.m_menu_edit_clearall_bookmarks.Text = "Clear All Bookmarks";
			// 
			// m_menu_edit_bookmarks
			// 
			this.m_menu_edit_bookmarks.Name = "m_menu_edit_bookmarks";
			this.m_menu_edit_bookmarks.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.B)));
			this.m_menu_edit_bookmarks.Size = new System.Drawing.Size(258, 22);
			this.m_menu_edit_bookmarks.Text = "Bookmarks...";
			// 
			// m_menu_encoding
			// 
			this.m_menu_encoding.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_encoding_detect,
            this.toolStripSeparator3,
            this.m_menu_encoding_ascii,
            this.m_menu_encoding_utf8,
            this.m_menu_encoding_ucs2_littleendian,
            this.m_menu_encoding_ucs2_bigendian});
			this.m_menu_encoding.Name = "m_menu_encoding";
			this.m_menu_encoding.Size = new System.Drawing.Size(69, 20);
			this.m_menu_encoding.Text = "E&ncoding";
			// 
			// m_menu_encoding_detect
			// 
			this.m_menu_encoding_detect.Name = "m_menu_encoding_detect";
			this.m_menu_encoding_detect.Size = new System.Drawing.Size(185, 22);
			this.m_menu_encoding_detect.Text = "&Detect Automatically";
			// 
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(182, 6);
			// 
			// m_menu_encoding_ascii
			// 
			this.m_menu_encoding_ascii.Name = "m_menu_encoding_ascii";
			this.m_menu_encoding_ascii.Size = new System.Drawing.Size(185, 22);
			this.m_menu_encoding_ascii.Text = "ASCII";
			// 
			// m_menu_encoding_utf8
			// 
			this.m_menu_encoding_utf8.Name = "m_menu_encoding_utf8";
			this.m_menu_encoding_utf8.Size = new System.Drawing.Size(185, 22);
			this.m_menu_encoding_utf8.Text = "UTF-8";
			// 
			// m_menu_encoding_ucs2_littleendian
			// 
			this.m_menu_encoding_ucs2_littleendian.Name = "m_menu_encoding_ucs2_littleendian";
			this.m_menu_encoding_ucs2_littleendian.Size = new System.Drawing.Size(185, 22);
			this.m_menu_encoding_ucs2_littleendian.Text = "UCS-2 (little endian)";
			// 
			// m_menu_encoding_ucs2_bigendian
			// 
			this.m_menu_encoding_ucs2_bigendian.Name = "m_menu_encoding_ucs2_bigendian";
			this.m_menu_encoding_ucs2_bigendian.Size = new System.Drawing.Size(185, 22);
			this.m_menu_encoding_ucs2_bigendian.Text = "UCS-2 (big endian)";
			// 
			// m_menu_line_ending
			// 
			this.m_menu_line_ending.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_line_ending_detect,
            this.toolStripSeparator7,
            this.m_menu_line_ending_cr,
            this.m_menu_line_ending_crlf,
            this.m_menu_line_ending_lf,
            this.m_menu_line_ending_custom});
			this.m_menu_line_ending.Name = "m_menu_line_ending";
			this.m_menu_line_ending.Size = new System.Drawing.Size(81, 20);
			this.m_menu_line_ending.Text = "&Line Ending";
			// 
			// m_menu_line_ending_detect
			// 
			this.m_menu_line_ending_detect.Name = "m_menu_line_ending_detect";
			this.m_menu_line_ending_detect.Size = new System.Drawing.Size(185, 22);
			this.m_menu_line_ending_detect.Text = "Detect &Automatically";
			// 
			// toolStripSeparator7
			// 
			this.toolStripSeparator7.Name = "toolStripSeparator7";
			this.toolStripSeparator7.Size = new System.Drawing.Size(182, 6);
			// 
			// m_menu_line_ending_cr
			// 
			this.m_menu_line_ending_cr.Name = "m_menu_line_ending_cr";
			this.m_menu_line_ending_cr.Size = new System.Drawing.Size(185, 22);
			this.m_menu_line_ending_cr.Text = "CR";
			// 
			// m_menu_line_ending_crlf
			// 
			this.m_menu_line_ending_crlf.Name = "m_menu_line_ending_crlf";
			this.m_menu_line_ending_crlf.Size = new System.Drawing.Size(185, 22);
			this.m_menu_line_ending_crlf.Text = "CR+LF";
			// 
			// m_menu_line_ending_lf
			// 
			this.m_menu_line_ending_lf.Name = "m_menu_line_ending_lf";
			this.m_menu_line_ending_lf.Size = new System.Drawing.Size(185, 22);
			this.m_menu_line_ending_lf.Text = "LF";
			// 
			// m_menu_line_ending_custom
			// 
			this.m_menu_line_ending_custom.Name = "m_menu_line_ending_custom";
			this.m_menu_line_ending_custom.Size = new System.Drawing.Size(185, 22);
			this.m_menu_line_ending_custom.Text = "Custom";
			// 
			// m_menu_tools
			// 
			this.m_menu_tools.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_tools_alwaysontop,
            this.m_menu_tools_ghost_mode,
            this.m_sep4,
            this.m_menu_tools_clear_log_file,
            this.m_sep5,
            this.m_menu_tools_highlights,
            this.m_menu_tools_filters,
            this.m_menu_tools_transforms,
            this.m_menu_tools_actions,
            this.toolStripSeparator6,
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
			// m_menu_tools_ghost_mode
			// 
			this.m_menu_tools_ghost_mode.Name = "m_menu_tools_ghost_mode";
			this.m_menu_tools_ghost_mode.Size = new System.Drawing.Size(154, 22);
			this.m_menu_tools_ghost_mode.Text = "&Ghost Mode";
			// 
			// m_sep4
			// 
			this.m_sep4.Name = "m_sep4";
			this.m_sep4.Size = new System.Drawing.Size(151, 6);
			// 
			// m_menu_tools_clear_log_file
			// 
			this.m_menu_tools_clear_log_file.Name = "m_menu_tools_clear_log_file";
			this.m_menu_tools_clear_log_file.Size = new System.Drawing.Size(154, 22);
			this.m_menu_tools_clear_log_file.Text = "&Clear Log File";
			// 
			// m_sep5
			// 
			this.m_sep5.Name = "m_sep5";
			this.m_sep5.Size = new System.Drawing.Size(151, 6);
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
			// m_menu_tools_transforms
			// 
			this.m_menu_tools_transforms.Name = "m_menu_tools_transforms";
			this.m_menu_tools_transforms.Size = new System.Drawing.Size(154, 22);
			this.m_menu_tools_transforms.Text = "&Transforms";
			// 
			// m_menu_tools_actions
			// 
			this.m_menu_tools_actions.Name = "m_menu_tools_actions";
			this.m_menu_tools_actions.Size = new System.Drawing.Size(154, 22);
			this.m_menu_tools_actions.Text = "&Actions";
			// 
			// toolStripSeparator6
			// 
			this.toolStripSeparator6.Name = "toolStripSeparator6";
			this.toolStripSeparator6.Size = new System.Drawing.Size(151, 6);
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
            this.m_menu_help_check_for_updates,
            this.toolStripSeparator11,
            this.m_menu_help_visit_store,
            this.m_menu_help_register,
            this.toolStripSeparator2,
            this.m_menu_help_about});
			this.m_menu_help.Name = "m_menu_help";
			this.m_menu_help.Size = new System.Drawing.Size(44, 20);
			this.m_menu_help.Text = "&Help";
			// 
			// m_menu_help_totd
			// 
			this.m_menu_help_totd.Name = "m_menu_help_totd";
			this.m_menu_help_totd.Size = new System.Drawing.Size(171, 22);
			this.m_menu_help_totd.Text = "&Tip of the Day";
			// 
			// m_menu_help_check_for_updates
			// 
			this.m_menu_help_check_for_updates.Name = "m_menu_help_check_for_updates";
			this.m_menu_help_check_for_updates.Size = new System.Drawing.Size(171, 22);
			this.m_menu_help_check_for_updates.Text = "Check for &Updates";
			// 
			// toolStripSeparator11
			// 
			this.toolStripSeparator11.Name = "toolStripSeparator11";
			this.toolStripSeparator11.Size = new System.Drawing.Size(168, 6);
			// 
			// m_menu_help_visit_store
			// 
			this.m_menu_help_visit_store.Name = "m_menu_help_visit_store";
			this.m_menu_help_visit_store.Size = new System.Drawing.Size(171, 22);
			this.m_menu_help_visit_store.Text = "&Visit Store";
			// 
			// m_menu_help_register
			// 
			this.m_menu_help_register.Name = "m_menu_help_register";
			this.m_menu_help_register.Size = new System.Drawing.Size(171, 22);
			this.m_menu_help_register.Text = "&Register...";
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(168, 6);
			// 
			// m_menu_help_about
			// 
			this.m_menu_help_about.Name = "m_menu_help_about";
			this.m_menu_help_about.Size = new System.Drawing.Size(171, 22);
			this.m_menu_help_about.Text = "&About";
			// 
			// m_status
			// 
			this.m_status.Dock = System.Windows.Forms.DockStyle.None;
			this.m_status.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_status_filesize,
            this.m_status_line_end,
            this.m_status_encoding,
            this.m_status_spring,
            this.m_status_message,
            this.m_status_progress});
			this.m_status.Location = new System.Drawing.Point(0, 0);
			this.m_status.Name = "m_status";
			this.m_status.Size = new System.Drawing.Size(593, 24);
			this.m_status.TabIndex = 3;
			this.m_status.Text = "statusStrip1";
			// 
			// m_status_filesize
			// 
			this.m_status_filesize.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
			this.m_status_filesize.Name = "m_status_filesize";
			this.m_status_filesize.Size = new System.Drawing.Size(128, 19);
			this.m_status_filesize.Text = "Size: 2147483647 bytes";
			this.m_status_filesize.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// m_status_line_end
			// 
			this.m_status_line_end.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
			this.m_status_line_end.Name = "m_status_line_end";
			this.m_status_line_end.Size = new System.Drawing.Size(76, 19);
			this.m_status_line_end.Text = "Line Ending:";
			// 
			// m_status_encoding
			// 
			this.m_status_encoding.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
			this.m_status_encoding.Name = "m_status_encoding";
			this.m_status_encoding.Size = new System.Drawing.Size(64, 19);
			this.m_status_encoding.Text = "Encoding:";
			// 
			// m_status_spring
			// 
			this.m_status_spring.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
			this.m_status_spring.Name = "m_status_spring";
			this.m_status_spring.Size = new System.Drawing.Size(310, 19);
			this.m_status_spring.Spring = true;
			this.m_status_spring.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// m_status_message
			// 
			this.m_status_message.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
			this.m_status_message.Name = "m_status_message";
			this.m_status_message.Size = new System.Drawing.Size(109, 19);
			this.m_status_message.Text = "Transient Message";
			this.m_status_message.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.m_status_message.Visible = false;
			// 
			// m_status_progress
			// 
			this.m_status_progress.Name = "m_status_progress";
			this.m_status_progress.Size = new System.Drawing.Size(100, 18);
			this.m_status_progress.Visible = false;
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
			this.m_toolstrip_cont.ContentPanel.Controls.Add(this.m_table);
			this.m_toolstrip_cont.ContentPanel.Padding = new System.Windows.Forms.Padding(3);
			this.m_toolstrip_cont.ContentPanel.Size = new System.Drawing.Size(593, 416);
			this.m_toolstrip_cont.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_toolstrip_cont.LeftToolStripPanelVisible = false;
			this.m_toolstrip_cont.Location = new System.Drawing.Point(0, 0);
			this.m_toolstrip_cont.Name = "m_toolstrip_cont";
			this.m_toolstrip_cont.RightToolStripPanelVisible = false;
			this.m_toolstrip_cont.Size = new System.Drawing.Size(593, 495);
			this.m_toolstrip_cont.TabIndex = 6;
			this.m_toolstrip_cont.Text = "m_toolstrip_cont";
			// 
			// m_toolstrip_cont.TopToolStripPanel
			// 
			this.m_toolstrip_cont.TopToolStripPanel.Controls.Add(this.m_menu);
			this.m_toolstrip_cont.TopToolStripPanel.Controls.Add(this.m_toolstrip);
			// 
			// m_table
			// 
			this.m_table.ColumnCount = 3;
			this.m_table.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.m_table.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.m_table.ContextMenuStrip = this.m_cmenu_grid;
			this.m_table.Controls.Add(this.m_grid, 0, 0);
			this.m_table.Controls.Add(this.m_scroll_file, 1, 0);
			this.m_table.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_table.Location = new System.Drawing.Point(3, 3);
			this.m_table.Name = "m_table";
			this.m_table.RowCount = 1;
			this.m_table.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table.Size = new System.Drawing.Size(587, 410);
			this.m_table.TabIndex = 5;
			// 
			// m_cmenu_grid
			// 
			this.m_cmenu_grid.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_cmenu_select_all,
            this.m_cmenu_copy,
            this.toolStripSeparator15,
            this.m_cmenu_clear_log,
            this.toolStripSeparator10,
            this.m_cmenu_highlight_row,
            this.m_cmenu_filter_row,
            this.m_cmenu_transform_row,
            this.m_cmenu_action_row,
            this.toolStripSeparator4,
            this.m_cmenu_find_next,
            this.m_cmenu_find_prev,
            this.toolStripSeparator14,
            this.m_cmenu_toggle_bookmark});
			this.m_cmenu_grid.Name = "m_cmenu_grid";
			this.m_cmenu_grid.Size = new System.Drawing.Size(169, 248);
			// 
			// m_cmenu_select_all
			// 
			this.m_cmenu_select_all.Name = "m_cmenu_select_all";
			this.m_cmenu_select_all.Size = new System.Drawing.Size(168, 22);
			this.m_cmenu_select_all.Text = "Select &All";
			// 
			// m_cmenu_copy
			// 
			this.m_cmenu_copy.Name = "m_cmenu_copy";
			this.m_cmenu_copy.Size = new System.Drawing.Size(168, 22);
			this.m_cmenu_copy.Text = "&Copy";
			// 
			// toolStripSeparator15
			// 
			this.toolStripSeparator15.Name = "toolStripSeparator15";
			this.toolStripSeparator15.Size = new System.Drawing.Size(165, 6);
			// 
			// m_cmenu_clear_log
			// 
			this.m_cmenu_clear_log.Name = "m_cmenu_clear_log";
			this.m_cmenu_clear_log.Size = new System.Drawing.Size(168, 22);
			this.m_cmenu_clear_log.Text = "C&lear Log";
			// 
			// toolStripSeparator10
			// 
			this.toolStripSeparator10.Name = "toolStripSeparator10";
			this.toolStripSeparator10.Size = new System.Drawing.Size(165, 6);
			// 
			// m_cmenu_highlight_row
			// 
			this.m_cmenu_highlight_row.Name = "m_cmenu_highlight_row";
			this.m_cmenu_highlight_row.Size = new System.Drawing.Size(168, 22);
			this.m_cmenu_highlight_row.Text = "&Highlight Row...";
			// 
			// m_cmenu_filter_row
			// 
			this.m_cmenu_filter_row.Name = "m_cmenu_filter_row";
			this.m_cmenu_filter_row.Size = new System.Drawing.Size(168, 22);
			this.m_cmenu_filter_row.Text = "&Filter Row...";
			// 
			// m_cmenu_transform_row
			// 
			this.m_cmenu_transform_row.Name = "m_cmenu_transform_row";
			this.m_cmenu_transform_row.Size = new System.Drawing.Size(168, 22);
			this.m_cmenu_transform_row.Text = "&Transform Row...";
			// 
			// m_cmenu_action_row
			// 
			this.m_cmenu_action_row.Name = "m_cmenu_action_row";
			this.m_cmenu_action_row.Size = new System.Drawing.Size(168, 22);
			this.m_cmenu_action_row.Text = "&Action Row...";
			// 
			// toolStripSeparator4
			// 
			this.toolStripSeparator4.Name = "toolStripSeparator4";
			this.toolStripSeparator4.Size = new System.Drawing.Size(165, 6);
			// 
			// m_cmenu_find_next
			// 
			this.m_cmenu_find_next.Name = "m_cmenu_find_next";
			this.m_cmenu_find_next.Size = new System.Drawing.Size(168, 22);
			this.m_cmenu_find_next.Text = "Find &Next";
			// 
			// m_cmenu_find_prev
			// 
			this.m_cmenu_find_prev.Name = "m_cmenu_find_prev";
			this.m_cmenu_find_prev.Size = new System.Drawing.Size(168, 22);
			this.m_cmenu_find_prev.Text = "Find &Previous";
			// 
			// toolStripSeparator14
			// 
			this.toolStripSeparator14.Name = "toolStripSeparator14";
			this.toolStripSeparator14.Size = new System.Drawing.Size(165, 6);
			// 
			// m_cmenu_toggle_bookmark
			// 
			this.m_cmenu_toggle_bookmark.Name = "m_cmenu_toggle_bookmark";
			this.m_cmenu_toggle_bookmark.Size = new System.Drawing.Size(168, 22);
			this.m_cmenu_toggle_bookmark.Text = "Toggle &Bookmark";
			// 
			// m_grid
			// 
			this.m_grid.AllowUserToAddRows = false;
			this.m_grid.AllowUserToDeleteRows = false;
			this.m_grid.AllowUserToOrderColumns = true;
			this.m_grid.AllowUserToResizeRows = false;
			this.m_grid.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
			this.m_grid.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
			this.m_grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_grid.ContextMenuStrip = this.m_cmenu_grid;
			this.m_grid.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_grid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
			this.m_grid.Location = new System.Drawing.Point(3, 3);
			this.m_grid.Name = "m_grid";
			this.m_grid.ReadOnly = true;
			this.m_grid.RowHeadersVisible = false;
			this.m_grid.RowTemplate.Height = 18;
			this.m_grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid.ShowCellErrors = false;
			this.m_grid.ShowCellToolTips = false;
			this.m_grid.ShowEditingIcon = false;
			this.m_grid.ShowRowErrors = false;
			this.m_grid.Size = new System.Drawing.Size(557, 404);
			this.m_grid.TabIndex = 3;
			this.m_grid.VirtualMode = true;
			// 
			// m_scroll_file
			// 
			this.m_scroll_file.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_scroll_file.LargeChange = ((long)(1));
			this.m_scroll_file.Location = new System.Drawing.Point(566, 3);
			this.m_scroll_file.MinimumSize = new System.Drawing.Size(10, 10);
			this.m_scroll_file.MinThumbSize = 20;
			this.m_scroll_file.Name = "m_scroll_file";
			this.m_scroll_file.Overlay = null;
			this.m_scroll_file.OverlayAttributes = null;
			this.m_scroll_file.Size = new System.Drawing.Size(18, 404);
			this.m_scroll_file.SmallChange = ((long)(1));
			this.m_scroll_file.TabIndex = 4;
			this.m_scroll_file.ThumbColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
			this.m_scroll_file.TrackColor = System.Drawing.SystemColors.ControlLight;
			// 
			// Main
			// 
			this.AllowDrop = true;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(593, 495);
			this.Controls.Add(this.m_toolstrip_cont);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.KeyPreview = true;
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
			this.m_table.ResumeLayout(false);
			this.m_cmenu_grid.ResumeLayout(false);
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
		private System.Windows.Forms.ToolStripButton m_btn_jump_to_end;
		private System.Windows.Forms.ToolStripContainer m_toolstrip_cont;
		private DGV m_grid;
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
		private System.Windows.Forms.ToolStripStatusLabel m_status_filesize;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripButton m_btn_refresh;
		private System.Windows.Forms.ToolStripMenuItem m_menu_help_totd;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripMenuItem m_menu_encoding;
		private System.Windows.Forms.ToolStripMenuItem m_menu_encoding_ascii;
		private System.Windows.Forms.ToolStripMenuItem m_menu_encoding_utf8;
		private System.Windows.Forms.ToolStripMenuItem m_menu_encoding_ucs2_bigendian;
		private System.Windows.Forms.ToolStripMenuItem m_menu_encoding_ucs2_littleendian;
		private System.Windows.Forms.ToolStripStatusLabel m_status_spring;
		private System.Windows.Forms.ToolStripStatusLabel m_status_message;
		private System.Windows.Forms.ToolStripStatusLabel m_status_line_end;
		private System.Windows.Forms.ToolStripMenuItem m_menu_encoding_detect;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.ToolStripStatusLabel m_status_encoding;
		private System.Windows.Forms.ToolStripButton m_btn_options;
		private RyLogViewer.SubRangeScroll m_scroll_file;
		private System.Windows.Forms.TableLayoutPanel m_table;
		private System.Windows.Forms.ToolStripButton m_btn_watch;
		private System.Windows.Forms.ContextMenuStrip m_cmenu_grid;
		private System.Windows.Forms.ToolStripMenuItem m_cmenu_copy;
		private System.Windows.Forms.ToolStripMenuItem m_cmenu_select_all;
		private System.Windows.Forms.ToolStripMenuItem m_menu_file_export;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
		private System.Windows.Forms.ToolStripMenuItem m_menu_tools_clear_log_file;
		private System.Windows.Forms.ToolStripMenuItem m_menu_file_open_stdout;
		private System.Windows.Forms.ToolStripMenuItem m_menu_file_open_network;
		private System.Windows.Forms.ToolStripProgressBar m_status_progress;
		private System.Windows.Forms.ToolStripMenuItem m_menu_file_open_named_pipe;
		private System.Windows.Forms.ToolStripMenuItem m_menu_file_open_serial_port;
		private System.Windows.Forms.ToolStripMenuItem m_menu_line_ending;
		private System.Windows.Forms.ToolStripMenuItem m_menu_line_ending_detect;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
		private System.Windows.Forms.ToolStripMenuItem m_menu_line_ending_cr;
		private System.Windows.Forms.ToolStripMenuItem m_menu_line_ending_crlf;
		private System.Windows.Forms.ToolStripMenuItem m_menu_line_ending_lf;
		private System.Windows.Forms.ToolStripMenuItem m_menu_line_ending_custom;
		private System.Windows.Forms.ToolStripMenuItem m_menu_tools_ghost_mode;
		private System.Windows.Forms.ToolStripButton m_btn_jump_to_start;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator8;
		private System.Windows.Forms.ToolStripMenuItem m_menu_help_check_for_updates;
		private System.Windows.Forms.ToolStripButton m_btn_transforms;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator9;
		private System.Windows.Forms.ToolStripButton m_btn_additive;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator10;
		private System.Windows.Forms.ToolStripMenuItem m_cmenu_highlight_row;
		private System.Windows.Forms.ToolStripMenuItem m_cmenu_filter_row;
		private System.Windows.Forms.ToolStripMenuItem m_cmenu_transform_row;
		private System.Windows.Forms.ToolStripMenuItem m_cmenu_action_row;
		private System.Windows.Forms.ToolStripMenuItem m_menu_tools_transforms;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
		private System.Windows.Forms.ToolStripMenuItem m_cmenu_find_next;
		private System.Windows.Forms.ToolStripMenuItem m_cmenu_find_prev;
		private System.Windows.Forms.ToolStripButton m_btn_actions;
		private System.Windows.Forms.ToolStripMenuItem m_menu_tools_actions;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator11;
		private System.Windows.Forms.ToolStripMenuItem m_menu_help_visit_store;
		private System.Windows.Forms.ToolStripMenuItem m_menu_help_register;
		private System.Windows.Forms.ToolStripButton m_btn_tail;
		private System.Windows.Forms.ToolStripButton m_btn_bookmarks;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator12;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator13;
		private System.Windows.Forms.ToolStripMenuItem m_menu_edit_toggle_bookmark;
		private System.Windows.Forms.ToolStripMenuItem m_menu_edit_next_bookmark;
		private System.Windows.Forms.ToolStripMenuItem m_menu_edit_prev_bookmark;
		private System.Windows.Forms.ToolStripMenuItem m_menu_edit_clearall_bookmarks;
		private System.Windows.Forms.ToolStripMenuItem m_menu_edit_bookmarks;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator14;
		private System.Windows.Forms.ToolStripMenuItem m_cmenu_toggle_bookmark;
		private System.Windows.Forms.ToolStripMenuItem m_cmenu_clear_log;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator15;
		private System.Windows.Forms.ToolStripMenuItem m_menu_file_wizards;
		private System.Windows.Forms.ToolStripMenuItem m_menu_file_wizards_androidlogcat;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator17;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator16;
	}
}

