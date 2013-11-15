namespace RyLogViewer
{
	public partial class SettingsUI
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsUI));
			this.m_tabctrl = new System.Windows.Forms.TabControl();
			this.m_tab_general = new System.Windows.Forms.TabPage();
			this.m_table_general0 = new System.Windows.Forms.TableLayoutPanel();
			this.m_group_settings = new System.Windows.Forms.GroupBox();
			this.label1 = new System.Windows.Forms.Label();
			this.m_text_settings = new System.Windows.Forms.TextBox();
			this.m_btn_settings_reset = new System.Windows.Forms.Button();
			this.m_btn_settings_save = new System.Windows.Forms.Button();
			this.m_btn_settings_load = new System.Windows.Forms.Button();
			this.m_table_general1 = new System.Windows.Forms.TableLayoutPanel();
			this.m_group_startup = new System.Windows.Forms.GroupBox();
			this.m_lbl_web_proxy_port = new System.Windows.Forms.Label();
			this.m_lbl_web_proxy_host = new System.Windows.Forms.Label();
			this.m_spinner_web_proxy_port = new System.Windows.Forms.NumericUpDown();
			this.m_edit_web_proxy_host = new System.Windows.Forms.TextBox();
			this.m_check_use_web_proxy = new System.Windows.Forms.CheckBox();
			this.m_check_c4u = new System.Windows.Forms.CheckBox();
			this.m_check_show_totd = new System.Windows.Forms.CheckBox();
			this.m_check_save_screen_loc = new System.Windows.Forms.CheckBox();
			this.m_check_load_last_file = new System.Windows.Forms.CheckBox();
			this.m_group_grid = new System.Windows.Forms.GroupBox();
			this.m_lbl_max_line_len_kb = new System.Windows.Forms.Label();
			this.m_spinner_max_line_length = new System.Windows.Forms.NumericUpDown();
			this.m_lbl_max_line_length = new System.Windows.Forms.Label();
			this.m_lbl_max_scan_size1 = new System.Windows.Forms.Label();
			this.m_lbl_history_length1 = new System.Windows.Forms.Label();
			this.m_spinner_line_cache_count = new System.Windows.Forms.NumericUpDown();
			this.m_spinner_max_mem_range = new System.Windows.Forms.NumericUpDown();
			this.m_lbl_max_scan_size0 = new System.Windows.Forms.Label();
			this.m_check_file_changes_additive = new System.Windows.Forms.CheckBox();
			this.m_check_open_at_end = new System.Windows.Forms.CheckBox();
			this.m_lbl_history_length0 = new System.Windows.Forms.Label();
			this.m_table_general2 = new System.Windows.Forms.TableLayoutPanel();
			this.m_group_line_ends = new System.Windows.Forms.GroupBox();
			this.m_lbl_column_count = new System.Windows.Forms.Label();
			this.m_spinner_column_count = new System.Windows.Forms.NumericUpDown();
			this.m_check_ignore_blank_lines = new System.Windows.Forms.CheckBox();
			this.m_edit_col_delims = new System.Windows.Forms.TextBox();
			this.m_lbl_col_delims = new System.Windows.Forms.Label();
			this.m_edit_line_ends = new System.Windows.Forms.TextBox();
			this.m_lbl_line_ends = new System.Windows.Forms.Label();
			this.m_tab_logview = new System.Windows.Forms.TabPage();
			this.m_table_appearance0 = new System.Windows.Forms.TableLayoutPanel();
			this.m_table_appearance1 = new System.Windows.Forms.TableLayoutPanel();
			this.m_group_log_text_colours = new System.Windows.Forms.GroupBox();
			this.m_lbl_line2_example = new System.Windows.Forms.Label();
			this.m_lbl_line1_example = new System.Windows.Forms.Label();
			this.m_lbl_row_height = new System.Windows.Forms.Label();
			this.m_spinner_row_height = new System.Windows.Forms.NumericUpDown();
			this.m_lbl_selection_example = new System.Windows.Forms.Label();
			this.m_lbl_line1_colours = new System.Windows.Forms.Label();
			this.m_lbl_selection_colour = new System.Windows.Forms.Label();
			this.m_check_alternate_line_colour = new System.Windows.Forms.CheckBox();
			this.m_group_font = new System.Windows.Forms.GroupBox();
			this.m_btn_change_font = new System.Windows.Forms.Button();
			this.m_text_font = new System.Windows.Forms.TextBox();
			this.m_group_misc = new System.Windows.Forms.GroupBox();
			this.m_lbl_tabsize = new System.Windows.Forms.Label();
			this.m_spinner_tabsize = new System.Windows.Forms.NumericUpDown();
			this.m_check_full_filepath_in_title = new System.Windows.Forms.CheckBox();
			this.m_table_appearance2 = new System.Windows.Forms.TableLayoutPanel();
			this.m_group_file_scroll = new System.Windows.Forms.GroupBox();
			this.m_lbl_fs_cached_colour = new System.Windows.Forms.Label();
			this.m_lbl_fs_edit_cached_colour = new System.Windows.Forms.Label();
			this.m_lbl_fs_bookmark_colour = new System.Windows.Forms.Label();
			this.m_lbl_fs_edit_bookmark_colour = new System.Windows.Forms.Label();
			this.m_lbl_fs_visible_colour = new System.Windows.Forms.Label();
			this.m_lbl_fs_edit_visible_colour = new System.Windows.Forms.Label();
			this.m_spinner_file_scroll_width = new System.Windows.Forms.NumericUpDown();
			this.m_lbl_file_scroll_width = new System.Windows.Forms.Label();
			this.m_tab_highlight = new System.Windows.Forms.TabPage();
			this.m_split_hl = new System.Windows.Forms.SplitContainer();
			this.m_pattern_hl = new RyLogViewer.PatternUI();
			this.m_table_hl = new System.Windows.Forms.TableLayoutPanel();
			this.m_grid_highlight = new RyLogViewer.DataGridView();
			this.m_pattern_set_hl = new RyLogViewer.PatternSetHL();
			this.label2 = new System.Windows.Forms.Label();
			this.m_tab_filter = new System.Windows.Forms.TabPage();
			this.m_split_ft = new System.Windows.Forms.SplitContainer();
			this.m_pattern_ft = new RyLogViewer.PatternUI();
			this.m_table_ft = new System.Windows.Forms.TableLayoutPanel();
			this.m_lbl_ft_grid_desc = new System.Windows.Forms.Label();
			this.m_grid_filter = new RyLogViewer.DataGridView();
			this.m_pattern_set_ft = new RyLogViewer.PatternSetFT();
			this.m_check_reject_all_by_default = new System.Windows.Forms.CheckBox();
			this.m_tab_transform = new System.Windows.Forms.TabPage();
			this.m_split_tx = new System.Windows.Forms.SplitContainer();
			this.m_pattern_tx = new RyLogViewer.TransformUI();
			this.m_table_tx = new System.Windows.Forms.TableLayoutPanel();
			this.label4 = new System.Windows.Forms.Label();
			this.m_grid_transform = new RyLogViewer.DataGridView();
			this.m_pattern_set_tx = new RyLogViewer.PatternSetTX();
			this.m_tab_action = new System.Windows.Forms.TabPage();
			this.m_split_ac = new System.Windows.Forms.SplitContainer();
			this.m_pattern_ac = new RyLogViewer.PatternUI();
			this.m_table_ac = new System.Windows.Forms.TableLayoutPanel();
			this.label5 = new System.Windows.Forms.Label();
			this.m_grid_action = new RyLogViewer.DataGridView();
			this.m_pattern_set_ac = new RyLogViewer.PatternSetAC();
			this.m_image_list = new System.Windows.Forms.ImageList(this.components);
			this.m_tabctrl.SuspendLayout();
			this.m_tab_general.SuspendLayout();
			this.m_table_general0.SuspendLayout();
			this.m_group_settings.SuspendLayout();
			this.m_table_general1.SuspendLayout();
			this.m_group_startup.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_web_proxy_port)).BeginInit();
			this.m_group_grid.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_max_line_length)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_line_cache_count)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_max_mem_range)).BeginInit();
			this.m_table_general2.SuspendLayout();
			this.m_group_line_ends.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_column_count)).BeginInit();
			this.m_tab_logview.SuspendLayout();
			this.m_table_appearance0.SuspendLayout();
			this.m_table_appearance1.SuspendLayout();
			this.m_group_log_text_colours.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_row_height)).BeginInit();
			this.m_group_font.SuspendLayout();
			this.m_group_misc.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_tabsize)).BeginInit();
			this.m_table_appearance2.SuspendLayout();
			this.m_group_file_scroll.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_file_scroll_width)).BeginInit();
			this.m_tab_highlight.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_split_hl)).BeginInit();
			this.m_split_hl.Panel1.SuspendLayout();
			this.m_split_hl.Panel2.SuspendLayout();
			this.m_split_hl.SuspendLayout();
			this.m_table_hl.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_highlight)).BeginInit();
			this.m_tab_filter.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_split_ft)).BeginInit();
			this.m_split_ft.Panel1.SuspendLayout();
			this.m_split_ft.Panel2.SuspendLayout();
			this.m_split_ft.SuspendLayout();
			this.m_table_ft.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_filter)).BeginInit();
			this.m_tab_transform.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_split_tx)).BeginInit();
			this.m_split_tx.Panel1.SuspendLayout();
			this.m_split_tx.Panel2.SuspendLayout();
			this.m_split_tx.SuspendLayout();
			this.m_table_tx.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_transform)).BeginInit();
			this.m_tab_action.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_split_ac)).BeginInit();
			this.m_split_ac.Panel1.SuspendLayout();
			this.m_split_ac.Panel2.SuspendLayout();
			this.m_split_ac.SuspendLayout();
			this.m_table_ac.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_action)).BeginInit();
			this.SuspendLayout();
			// 
			// m_tabctrl
			// 
			this.m_tabctrl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tabctrl.Controls.Add(this.m_tab_general);
			this.m_tabctrl.Controls.Add(this.m_tab_logview);
			this.m_tabctrl.Controls.Add(this.m_tab_highlight);
			this.m_tabctrl.Controls.Add(this.m_tab_filter);
			this.m_tabctrl.Controls.Add(this.m_tab_transform);
			this.m_tabctrl.Controls.Add(this.m_tab_action);
			this.m_tabctrl.Location = new System.Drawing.Point(1, 1);
			this.m_tabctrl.Margin = new System.Windows.Forms.Padding(0);
			this.m_tabctrl.Name = "m_tabctrl";
			this.m_tabctrl.Padding = new System.Drawing.Point(3, 3);
			this.m_tabctrl.SelectedIndex = 0;
			this.m_tabctrl.Size = new System.Drawing.Size(508, 456);
			this.m_tabctrl.TabIndex = 0;
			// 
			// m_tab_general
			// 
			this.m_tab_general.AutoScroll = true;
			this.m_tab_general.Controls.Add(this.m_table_general0);
			this.m_tab_general.Location = new System.Drawing.Point(4, 22);
			this.m_tab_general.Margin = new System.Windows.Forms.Padding(0);
			this.m_tab_general.Name = "m_tab_general";
			this.m_tab_general.Padding = new System.Windows.Forms.Padding(0, 1, 2, 1);
			this.m_tab_general.Size = new System.Drawing.Size(500, 430);
			this.m_tab_general.TabIndex = 0;
			this.m_tab_general.Text = "General";
			this.m_tab_general.UseVisualStyleBackColor = true;
			// 
			// m_table_general0
			// 
			this.m_table_general0.ColumnCount = 2;
			this.m_table_general0.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 49.59839F));
			this.m_table_general0.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50.40161F));
			this.m_table_general0.Controls.Add(this.m_group_settings, 0, 0);
			this.m_table_general0.Controls.Add(this.m_table_general1, 0, 1);
			this.m_table_general0.Controls.Add(this.m_table_general2, 1, 1);
			this.m_table_general0.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_table_general0.Location = new System.Drawing.Point(0, 1);
			this.m_table_general0.Margin = new System.Windows.Forms.Padding(0);
			this.m_table_general0.Name = "m_table_general0";
			this.m_table_general0.RowCount = 2;
			this.m_table_general0.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table_general0.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table_general0.Size = new System.Drawing.Size(498, 428);
			this.m_table_general0.TabIndex = 5;
			// 
			// m_group_settings
			// 
			this.m_table_general0.SetColumnSpan(this.m_group_settings, 2);
			this.m_group_settings.Controls.Add(this.label1);
			this.m_group_settings.Controls.Add(this.m_text_settings);
			this.m_group_settings.Controls.Add(this.m_btn_settings_reset);
			this.m_group_settings.Controls.Add(this.m_btn_settings_save);
			this.m_group_settings.Controls.Add(this.m_btn_settings_load);
			this.m_group_settings.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_group_settings.Location = new System.Drawing.Point(3, 3);
			this.m_group_settings.Name = "m_group_settings";
			this.m_group_settings.Size = new System.Drawing.Size(492, 71);
			this.m_group_settings.TabIndex = 3;
			this.m_group_settings.TabStop = false;
			this.m_group_settings.Text = "Settings";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.ForeColor = System.Drawing.SystemColors.ControlDark;
			this.label1.Location = new System.Drawing.Point(136, 39);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(159, 26);
			this.label1.TabIndex = 6;
			this.label1.Text = "Note: Settings are saved\r\nautomatically after any changes.";
			// 
			// m_text_settings
			// 
			this.m_text_settings.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_text_settings.Location = new System.Drawing.Point(8, 16);
			this.m_text_settings.Name = "m_text_settings";
			this.m_text_settings.ReadOnly = true;
			this.m_text_settings.Size = new System.Drawing.Size(474, 20);
			this.m_text_settings.TabIndex = 5;
			// 
			// m_btn_settings_reset
			// 
			this.m_btn_settings_reset.Location = new System.Drawing.Point(8, 40);
			this.m_btn_settings_reset.Name = "m_btn_settings_reset";
			this.m_btn_settings_reset.Size = new System.Drawing.Size(122, 23);
			this.m_btn_settings_reset.TabIndex = 3;
			this.m_btn_settings_reset.Text = "Reset to Defaults";
			this.m_btn_settings_reset.UseVisualStyleBackColor = true;
			// 
			// m_btn_settings_save
			// 
			this.m_btn_settings_save.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_settings_save.Location = new System.Drawing.Point(410, 40);
			this.m_btn_settings_save.Name = "m_btn_settings_save";
			this.m_btn_settings_save.Size = new System.Drawing.Size(72, 23);
			this.m_btn_settings_save.TabIndex = 2;
			this.m_btn_settings_save.Text = "Save As";
			this.m_btn_settings_save.UseVisualStyleBackColor = true;
			// 
			// m_btn_settings_load
			// 
			this.m_btn_settings_load.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_settings_load.Location = new System.Drawing.Point(332, 40);
			this.m_btn_settings_load.Name = "m_btn_settings_load";
			this.m_btn_settings_load.Size = new System.Drawing.Size(72, 23);
			this.m_btn_settings_load.TabIndex = 0;
			this.m_btn_settings_load.Text = "Load";
			this.m_btn_settings_load.UseVisualStyleBackColor = true;
			// 
			// m_table_general1
			// 
			this.m_table_general1.ColumnCount = 1;
			this.m_table_general1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table_general1.Controls.Add(this.m_group_startup, 0, 0);
			this.m_table_general1.Controls.Add(this.m_group_grid, 0, 1);
			this.m_table_general1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_table_general1.Location = new System.Drawing.Point(0, 77);
			this.m_table_general1.Margin = new System.Windows.Forms.Padding(0);
			this.m_table_general1.Name = "m_table_general1";
			this.m_table_general1.RowCount = 2;
			this.m_table_general1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table_general1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table_general1.Size = new System.Drawing.Size(246, 351);
			this.m_table_general1.TabIndex = 4;
			// 
			// m_group_startup
			// 
			this.m_group_startup.Controls.Add(this.m_lbl_web_proxy_port);
			this.m_group_startup.Controls.Add(this.m_lbl_web_proxy_host);
			this.m_group_startup.Controls.Add(this.m_spinner_web_proxy_port);
			this.m_group_startup.Controls.Add(this.m_edit_web_proxy_host);
			this.m_group_startup.Controls.Add(this.m_check_use_web_proxy);
			this.m_group_startup.Controls.Add(this.m_check_c4u);
			this.m_group_startup.Controls.Add(this.m_check_show_totd);
			this.m_group_startup.Controls.Add(this.m_check_save_screen_loc);
			this.m_group_startup.Controls.Add(this.m_check_load_last_file);
			this.m_group_startup.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_group_startup.Location = new System.Drawing.Point(3, 3);
			this.m_group_startup.MinimumSize = new System.Drawing.Size(205, 179);
			this.m_group_startup.Name = "m_group_startup";
			this.m_group_startup.Size = new System.Drawing.Size(240, 179);
			this.m_group_startup.TabIndex = 0;
			this.m_group_startup.TabStop = false;
			this.m_group_startup.Text = "Startup";
			// 
			// m_lbl_web_proxy_port
			// 
			this.m_lbl_web_proxy_port.AutoSize = true;
			this.m_lbl_web_proxy_port.Location = new System.Drawing.Point(140, 134);
			this.m_lbl_web_proxy_port.Name = "m_lbl_web_proxy_port";
			this.m_lbl_web_proxy_port.Size = new System.Drawing.Size(29, 13);
			this.m_lbl_web_proxy_port.TabIndex = 8;
			this.m_lbl_web_proxy_port.Text = "Port:";
			this.m_lbl_web_proxy_port.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_lbl_web_proxy_host
			// 
			this.m_lbl_web_proxy_host.AutoSize = true;
			this.m_lbl_web_proxy_host.Location = new System.Drawing.Point(11, 133);
			this.m_lbl_web_proxy_host.Name = "m_lbl_web_proxy_host";
			this.m_lbl_web_proxy_host.Size = new System.Drawing.Size(32, 13);
			this.m_lbl_web_proxy_host.TabIndex = 7;
			this.m_lbl_web_proxy_host.Text = "Host:";
			this.m_lbl_web_proxy_host.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_spinner_web_proxy_port
			// 
			this.m_spinner_web_proxy_port.Location = new System.Drawing.Point(143, 149);
			this.m_spinner_web_proxy_port.Name = "m_spinner_web_proxy_port";
			this.m_spinner_web_proxy_port.Size = new System.Drawing.Size(56, 20);
			this.m_spinner_web_proxy_port.TabIndex = 6;
			// 
			// m_edit_web_proxy_host
			// 
			this.m_edit_web_proxy_host.Location = new System.Drawing.Point(13, 149);
			this.m_edit_web_proxy_host.Name = "m_edit_web_proxy_host";
			this.m_edit_web_proxy_host.Size = new System.Drawing.Size(125, 20);
			this.m_edit_web_proxy_host.TabIndex = 5;
			// 
			// m_check_use_web_proxy
			// 
			this.m_check_use_web_proxy.AutoSize = true;
			this.m_check_use_web_proxy.Location = new System.Drawing.Point(14, 111);
			this.m_check_use_web_proxy.Name = "m_check_use_web_proxy";
			this.m_check_use_web_proxy.Size = new System.Drawing.Size(96, 17);
			this.m_check_use_web_proxy.TabIndex = 4;
			this.m_check_use_web_proxy.Text = "Use web proxy";
			this.m_check_use_web_proxy.UseVisualStyleBackColor = true;
			// 
			// m_check_c4u
			// 
			this.m_check_c4u.AutoSize = true;
			this.m_check_c4u.Location = new System.Drawing.Point(14, 88);
			this.m_check_c4u.Name = "m_check_c4u";
			this.m_check_c4u.Size = new System.Drawing.Size(115, 17);
			this.m_check_c4u.TabIndex = 3;
			this.m_check_c4u.Text = "Check for Updates";
			this.m_check_c4u.UseVisualStyleBackColor = true;
			// 
			// m_check_show_totd
			// 
			this.m_check_show_totd.AutoSize = true;
			this.m_check_show_totd.Location = new System.Drawing.Point(14, 65);
			this.m_check_show_totd.Name = "m_check_show_totd";
			this.m_check_show_totd.Size = new System.Drawing.Size(127, 17);
			this.m_check_show_totd.TabIndex = 2;
			this.m_check_show_totd.Text = "Show \'Tip of the Day\'";
			this.m_check_show_totd.UseVisualStyleBackColor = true;
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
			// m_group_grid
			// 
			this.m_group_grid.Controls.Add(this.m_lbl_max_line_len_kb);
			this.m_group_grid.Controls.Add(this.m_spinner_max_line_length);
			this.m_group_grid.Controls.Add(this.m_lbl_max_line_length);
			this.m_group_grid.Controls.Add(this.m_lbl_max_scan_size1);
			this.m_group_grid.Controls.Add(this.m_lbl_history_length1);
			this.m_group_grid.Controls.Add(this.m_spinner_line_cache_count);
			this.m_group_grid.Controls.Add(this.m_spinner_max_mem_range);
			this.m_group_grid.Controls.Add(this.m_lbl_max_scan_size0);
			this.m_group_grid.Controls.Add(this.m_check_file_changes_additive);
			this.m_group_grid.Controls.Add(this.m_check_open_at_end);
			this.m_group_grid.Controls.Add(this.m_lbl_history_length0);
			this.m_group_grid.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_group_grid.Location = new System.Drawing.Point(3, 188);
			this.m_group_grid.Name = "m_group_grid";
			this.m_group_grid.Size = new System.Drawing.Size(240, 160);
			this.m_group_grid.TabIndex = 1;
			this.m_group_grid.TabStop = false;
			this.m_group_grid.Text = "File Loading";
			// 
			// m_lbl_max_line_len_kb
			// 
			this.m_lbl_max_line_len_kb.AutoSize = true;
			this.m_lbl_max_line_len_kb.Location = new System.Drawing.Point(158, 81);
			this.m_lbl_max_line_len_kb.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
			this.m_lbl_max_line_len_kb.Name = "m_lbl_max_line_len_kb";
			this.m_lbl_max_line_len_kb.Size = new System.Drawing.Size(21, 13);
			this.m_lbl_max_line_len_kb.TabIndex = 9;
			this.m_lbl_max_line_len_kb.Text = "KB";
			this.m_lbl_max_line_len_kb.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// m_spinner_max_line_length
			// 
			this.m_spinner_max_line_length.Location = new System.Drawing.Point(111, 79);
			this.m_spinner_max_line_length.Maximum = new decimal(new int[] {
            128,
            0,
            0,
            0});
			this.m_spinner_max_line_length.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.m_spinner_max_line_length.Name = "m_spinner_max_line_length";
			this.m_spinner_max_line_length.Size = new System.Drawing.Size(44, 20);
			this.m_spinner_max_line_length.TabIndex = 8;
			this.m_spinner_max_line_length.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
			// 
			// m_lbl_max_line_length
			// 
			this.m_lbl_max_line_length.AutoSize = true;
			this.m_lbl_max_line_length.Location = new System.Drawing.Point(6, 81);
			this.m_lbl_max_line_length.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
			this.m_lbl_max_line_length.Name = "m_lbl_max_line_length";
			this.m_lbl_max_line_length.Size = new System.Drawing.Size(102, 13);
			this.m_lbl_max_line_length.TabIndex = 7;
			this.m_lbl_max_line_length.Text = "Maximum line length";
			this.m_lbl_max_line_length.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_lbl_max_scan_size1
			// 
			this.m_lbl_max_scan_size1.AutoSize = true;
			this.m_lbl_max_scan_size1.Location = new System.Drawing.Point(121, 52);
			this.m_lbl_max_scan_size1.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
			this.m_lbl_max_scan_size1.Name = "m_lbl_max_scan_size1";
			this.m_lbl_max_scan_size1.Size = new System.Drawing.Size(75, 13);
			this.m_lbl_max_scan_size1.TabIndex = 6;
			this.m_lbl_max_scan_size1.Text = "MB of file data";
			this.m_lbl_max_scan_size1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// m_lbl_history_length1
			// 
			this.m_lbl_history_length1.AutoSize = true;
			this.m_lbl_history_length1.Location = new System.Drawing.Point(117, 24);
			this.m_lbl_history_length1.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
			this.m_lbl_history_length1.Name = "m_lbl_history_length1";
			this.m_lbl_history_length1.Size = new System.Drawing.Size(78, 13);
			this.m_lbl_history_length1.TabIndex = 5;
			this.m_lbl_history_length1.Text = "lines in memory";
			this.m_lbl_history_length1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// m_spinner_line_cache_count
			// 
			this.m_spinner_line_cache_count.Location = new System.Drawing.Point(45, 21);
			this.m_spinner_line_cache_count.Maximum = new decimal(new int[] {
            99999999,
            0,
            0,
            0});
			this.m_spinner_line_cache_count.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.m_spinner_line_cache_count.Name = "m_spinner_line_cache_count";
			this.m_spinner_line_cache_count.Size = new System.Drawing.Size(68, 20);
			this.m_spinner_line_cache_count.TabIndex = 0;
			this.m_spinner_line_cache_count.Value = new decimal(new int[] {
            99999999,
            0,
            0,
            0});
			// 
			// m_spinner_max_mem_range
			// 
			this.m_spinner_max_mem_range.Location = new System.Drawing.Point(76, 50);
			this.m_spinner_max_mem_range.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.m_spinner_max_mem_range.Name = "m_spinner_max_mem_range";
			this.m_spinner_max_mem_range.Size = new System.Drawing.Size(44, 20);
			this.m_spinner_max_mem_range.TabIndex = 1;
			this.m_spinner_max_mem_range.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
			// 
			// m_lbl_max_scan_size0
			// 
			this.m_lbl_max_scan_size0.AutoSize = true;
			this.m_lbl_max_scan_size0.Location = new System.Drawing.Point(5, 52);
			this.m_lbl_max_scan_size0.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
			this.m_lbl_max_scan_size0.Name = "m_lbl_max_scan_size0";
			this.m_lbl_max_scan_size0.Size = new System.Drawing.Size(69, 13);
			this.m_lbl_max_scan_size0.TabIndex = 4;
			this.m_lbl_max_scan_size0.Text = "Scan at most";
			this.m_lbl_max_scan_size0.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_check_file_changes_additive
			// 
			this.m_check_file_changes_additive.AutoSize = true;
			this.m_check_file_changes_additive.Location = new System.Drawing.Point(13, 130);
			this.m_check_file_changes_additive.Name = "m_check_file_changes_additive";
			this.m_check_file_changes_additive.Size = new System.Drawing.Size(181, 17);
			this.m_check_file_changes_additive.TabIndex = 3;
			this.m_check_file_changes_additive.Text = "Assume file changes are additive";
			this.m_check_file_changes_additive.UseVisualStyleBackColor = true;
			// 
			// m_check_open_at_end
			// 
			this.m_check_open_at_end.AutoSize = true;
			this.m_check_open_at_end.Location = new System.Drawing.Point(14, 106);
			this.m_check_open_at_end.Name = "m_check_open_at_end";
			this.m_check_open_at_end.Size = new System.Drawing.Size(124, 17);
			this.m_check_open_at_end.TabIndex = 2;
			this.m_check_open_at_end.Text = "Open files at the end";
			this.m_check_open_at_end.UseVisualStyleBackColor = true;
			// 
			// m_lbl_history_length0
			// 
			this.m_lbl_history_length0.AutoSize = true;
			this.m_lbl_history_length0.Location = new System.Drawing.Point(7, 24);
			this.m_lbl_history_length0.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
			this.m_lbl_history_length0.Name = "m_lbl_history_length0";
			this.m_lbl_history_length0.Size = new System.Drawing.Size(38, 13);
			this.m_lbl_history_length0.TabIndex = 3;
			this.m_lbl_history_length0.Text = "Cache";
			this.m_lbl_history_length0.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_table_general2
			// 
			this.m_table_general2.ColumnCount = 1;
			this.m_table_general2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table_general2.Controls.Add(this.m_group_line_ends, 0, 0);
			this.m_table_general2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_table_general2.Location = new System.Drawing.Point(246, 77);
			this.m_table_general2.Margin = new System.Windows.Forms.Padding(0);
			this.m_table_general2.Name = "m_table_general2";
			this.m_table_general2.RowCount = 2;
			this.m_table_general2.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table_general2.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table_general2.Size = new System.Drawing.Size(252, 351);
			this.m_table_general2.TabIndex = 5;
			// 
			// m_group_line_ends
			// 
			this.m_group_line_ends.Controls.Add(this.m_lbl_column_count);
			this.m_group_line_ends.Controls.Add(this.m_spinner_column_count);
			this.m_group_line_ends.Controls.Add(this.m_check_ignore_blank_lines);
			this.m_group_line_ends.Controls.Add(this.m_edit_col_delims);
			this.m_group_line_ends.Controls.Add(this.m_lbl_col_delims);
			this.m_group_line_ends.Controls.Add(this.m_edit_line_ends);
			this.m_group_line_ends.Controls.Add(this.m_lbl_line_ends);
			this.m_group_line_ends.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_group_line_ends.Location = new System.Drawing.Point(3, 3);
			this.m_group_line_ends.MinimumSize = new System.Drawing.Size(182, 128);
			this.m_group_line_ends.Name = "m_group_line_ends";
			this.m_group_line_ends.Size = new System.Drawing.Size(246, 128);
			this.m_group_line_ends.TabIndex = 2;
			this.m_group_line_ends.TabStop = false;
			this.m_group_line_ends.Text = "Row Properties";
			// 
			// m_lbl_column_count
			// 
			this.m_lbl_column_count.AutoSize = true;
			this.m_lbl_column_count.Location = new System.Drawing.Point(23, 101);
			this.m_lbl_column_count.Name = "m_lbl_column_count";
			this.m_lbl_column_count.Size = new System.Drawing.Size(76, 13);
			this.m_lbl_column_count.TabIndex = 7;
			this.m_lbl_column_count.Text = "Column Count:";
			// 
			// m_spinner_column_count
			// 
			this.m_spinner_column_count.Location = new System.Drawing.Point(108, 99);
			this.m_spinner_column_count.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
			this.m_spinner_column_count.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.m_spinner_column_count.Name = "m_spinner_column_count";
			this.m_spinner_column_count.Size = new System.Drawing.Size(65, 20);
			this.m_spinner_column_count.TabIndex = 3;
			this.m_spinner_column_count.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			// 
			// m_check_ignore_blank_lines
			// 
			this.m_check_ignore_blank_lines.AutoSize = true;
			this.m_check_ignore_blank_lines.Location = new System.Drawing.Point(12, 19);
			this.m_check_ignore_blank_lines.Name = "m_check_ignore_blank_lines";
			this.m_check_ignore_blank_lines.Size = new System.Drawing.Size(109, 17);
			this.m_check_ignore_blank_lines.TabIndex = 0;
			this.m_check_ignore_blank_lines.Text = "Ignore blank lines";
			this.m_check_ignore_blank_lines.UseVisualStyleBackColor = true;
			// 
			// m_edit_col_delims
			// 
			this.m_edit_col_delims.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_col_delims.Location = new System.Drawing.Point(108, 73);
			this.m_edit_col_delims.Name = "m_edit_col_delims";
			this.m_edit_col_delims.Size = new System.Drawing.Size(131, 20);
			this.m_edit_col_delims.TabIndex = 2;
			// 
			// m_lbl_col_delims
			// 
			this.m_lbl_col_delims.AutoSize = true;
			this.m_lbl_col_delims.Location = new System.Drawing.Point(11, 76);
			this.m_lbl_col_delims.Name = "m_lbl_col_delims";
			this.m_lbl_col_delims.Size = new System.Drawing.Size(88, 13);
			this.m_lbl_col_delims.TabIndex = 3;
			this.m_lbl_col_delims.Text = "Column Delimiter:";
			// 
			// m_edit_line_ends
			// 
			this.m_edit_line_ends.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_line_ends.Location = new System.Drawing.Point(108, 45);
			this.m_edit_line_ends.Name = "m_edit_line_ends";
			this.m_edit_line_ends.Size = new System.Drawing.Size(131, 20);
			this.m_edit_line_ends.TabIndex = 1;
			// 
			// m_lbl_line_ends
			// 
			this.m_lbl_line_ends.AutoSize = true;
			this.m_lbl_line_ends.Location = new System.Drawing.Point(33, 48);
			this.m_lbl_line_ends.Name = "m_lbl_line_ends";
			this.m_lbl_line_ends.Size = new System.Drawing.Size(66, 13);
			this.m_lbl_line_ends.TabIndex = 1;
			this.m_lbl_line_ends.Text = "Line Ending:";
			// 
			// m_tab_logview
			// 
			this.m_tab_logview.Controls.Add(this.m_table_appearance0);
			this.m_tab_logview.Location = new System.Drawing.Point(4, 22);
			this.m_tab_logview.Margin = new System.Windows.Forms.Padding(0);
			this.m_tab_logview.Name = "m_tab_logview";
			this.m_tab_logview.Padding = new System.Windows.Forms.Padding(0, 1, 2, 1);
			this.m_tab_logview.Size = new System.Drawing.Size(500, 430);
			this.m_tab_logview.TabIndex = 3;
			this.m_tab_logview.Text = "Appearance";
			this.m_tab_logview.UseVisualStyleBackColor = true;
			// 
			// m_table_appearance0
			// 
			this.m_table_appearance0.ColumnCount = 2;
			this.m_table_appearance0.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.m_table_appearance0.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 171F));
			this.m_table_appearance0.Controls.Add(this.m_table_appearance1, 0, 0);
			this.m_table_appearance0.Controls.Add(this.m_table_appearance2, 1, 0);
			this.m_table_appearance0.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_table_appearance0.Location = new System.Drawing.Point(0, 1);
			this.m_table_appearance0.Margin = new System.Windows.Forms.Padding(0);
			this.m_table_appearance0.Name = "m_table_appearance0";
			this.m_table_appearance0.RowCount = 1;
			this.m_table_appearance0.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.m_table_appearance0.Size = new System.Drawing.Size(498, 428);
			this.m_table_appearance0.TabIndex = 7;
			// 
			// m_table_appearance1
			// 
			this.m_table_appearance1.ColumnCount = 1;
			this.m_table_appearance1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table_appearance1.Controls.Add(this.m_group_log_text_colours, 0, 0);
			this.m_table_appearance1.Controls.Add(this.m_group_font, 0, 1);
			this.m_table_appearance1.Controls.Add(this.m_group_misc, 0, 2);
			this.m_table_appearance1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_table_appearance1.Location = new System.Drawing.Point(0, 0);
			this.m_table_appearance1.Margin = new System.Windows.Forms.Padding(0);
			this.m_table_appearance1.Name = "m_table_appearance1";
			this.m_table_appearance1.RowCount = 4;
			this.m_table_appearance1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table_appearance1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table_appearance1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table_appearance1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table_appearance1.Size = new System.Drawing.Size(327, 428);
			this.m_table_appearance1.TabIndex = 0;
			// 
			// m_group_log_text_colours
			// 
			this.m_group_log_text_colours.Controls.Add(this.m_lbl_line2_example);
			this.m_group_log_text_colours.Controls.Add(this.m_lbl_line1_example);
			this.m_group_log_text_colours.Controls.Add(this.m_lbl_row_height);
			this.m_group_log_text_colours.Controls.Add(this.m_spinner_row_height);
			this.m_group_log_text_colours.Controls.Add(this.m_lbl_selection_example);
			this.m_group_log_text_colours.Controls.Add(this.m_lbl_line1_colours);
			this.m_group_log_text_colours.Controls.Add(this.m_lbl_selection_colour);
			this.m_group_log_text_colours.Controls.Add(this.m_check_alternate_line_colour);
			this.m_group_log_text_colours.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_group_log_text_colours.Location = new System.Drawing.Point(3, 3);
			this.m_group_log_text_colours.Name = "m_group_log_text_colours";
			this.m_group_log_text_colours.Size = new System.Drawing.Size(321, 136);
			this.m_group_log_text_colours.TabIndex = 4;
			this.m_group_log_text_colours.TabStop = false;
			this.m_group_log_text_colours.Text = "Log View Style";
			// 
			// m_lbl_line2_example
			// 
			this.m_lbl_line2_example.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_lbl_line2_example.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_lbl_line2_example.Location = new System.Drawing.Point(148, 75);
			this.m_lbl_line2_example.Name = "m_lbl_line2_example";
			this.m_lbl_line2_example.Size = new System.Drawing.Size(308, 21);
			this.m_lbl_line2_example.TabIndex = 3;
			this.m_lbl_line2_example.Text = "Click here to modify colours";
			this.m_lbl_line2_example.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// m_lbl_line1_example
			// 
			this.m_lbl_line1_example.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_lbl_line1_example.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_lbl_line1_example.Location = new System.Drawing.Point(98, 45);
			this.m_lbl_line1_example.Name = "m_lbl_line1_example";
			this.m_lbl_line1_example.Size = new System.Drawing.Size(308, 21);
			this.m_lbl_line1_example.TabIndex = 1;
			this.m_lbl_line1_example.Text = "Click here to modify colours";
			this.m_lbl_line1_example.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// m_lbl_row_height
			// 
			this.m_lbl_row_height.AutoSize = true;
			this.m_lbl_row_height.Location = new System.Drawing.Point(26, 109);
			this.m_lbl_row_height.Name = "m_lbl_row_height";
			this.m_lbl_row_height.Size = new System.Drawing.Size(66, 13);
			this.m_lbl_row_height.TabIndex = 1;
			this.m_lbl_row_height.Text = "Row Height:";
			// 
			// m_spinner_row_height
			// 
			this.m_spinner_row_height.Location = new System.Drawing.Point(98, 107);
			this.m_spinner_row_height.Name = "m_spinner_row_height";
			this.m_spinner_row_height.Size = new System.Drawing.Size(66, 20);
			this.m_spinner_row_height.TabIndex = 4;
			// 
			// m_lbl_selection_example
			// 
			this.m_lbl_selection_example.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_lbl_selection_example.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_lbl_selection_example.Location = new System.Drawing.Point(98, 16);
			this.m_lbl_selection_example.Name = "m_lbl_selection_example";
			this.m_lbl_selection_example.Size = new System.Drawing.Size(308, 21);
			this.m_lbl_selection_example.TabIndex = 0;
			this.m_lbl_selection_example.Text = "Click here to modify colours";
			this.m_lbl_selection_example.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// m_lbl_line1_colours
			// 
			this.m_lbl_line1_colours.AutoSize = true;
			this.m_lbl_line1_colours.Location = new System.Drawing.Point(7, 48);
			this.m_lbl_line1_colours.Name = "m_lbl_line1_colours";
			this.m_lbl_line1_colours.Size = new System.Drawing.Size(85, 13);
			this.m_lbl_line1_colours.TabIndex = 8;
			this.m_lbl_line1_colours.Text = "Log Text Colour:";
			// 
			// m_lbl_selection_colour
			// 
			this.m_lbl_selection_colour.AutoSize = true;
			this.m_lbl_selection_colour.Location = new System.Drawing.Point(5, 20);
			this.m_lbl_selection_colour.Name = "m_lbl_selection_colour";
			this.m_lbl_selection_colour.Size = new System.Drawing.Size(87, 13);
			this.m_lbl_selection_colour.TabIndex = 5;
			this.m_lbl_selection_colour.Text = "Selection Colour:";
			// 
			// m_check_alternate_line_colour
			// 
			this.m_check_alternate_line_colour.AutoSize = true;
			this.m_check_alternate_line_colour.Location = new System.Drawing.Point(10, 77);
			this.m_check_alternate_line_colour.Name = "m_check_alternate_line_colour";
			this.m_check_alternate_line_colour.Size = new System.Drawing.Size(132, 17);
			this.m_check_alternate_line_colour.TabIndex = 2;
			this.m_check_alternate_line_colour.Text = "Alternate Line Colours:";
			this.m_check_alternate_line_colour.UseVisualStyleBackColor = true;
			// 
			// m_group_font
			// 
			this.m_group_font.Controls.Add(this.m_btn_change_font);
			this.m_group_font.Controls.Add(this.m_text_font);
			this.m_group_font.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_group_font.Location = new System.Drawing.Point(3, 145);
			this.m_group_font.Name = "m_group_font";
			this.m_group_font.Size = new System.Drawing.Size(321, 52);
			this.m_group_font.TabIndex = 5;
			this.m_group_font.TabStop = false;
			this.m_group_font.Text = "Font";
			// 
			// m_btn_change_font
			// 
			this.m_btn_change_font.Location = new System.Drawing.Point(238, 17);
			this.m_btn_change_font.Name = "m_btn_change_font";
			this.m_btn_change_font.Size = new System.Drawing.Size(75, 23);
			this.m_btn_change_font.TabIndex = 0;
			this.m_btn_change_font.Text = "Font...";
			this.m_btn_change_font.UseVisualStyleBackColor = true;
			// 
			// m_text_font
			// 
			this.m_text_font.Location = new System.Drawing.Point(6, 19);
			this.m_text_font.Name = "m_text_font";
			this.m_text_font.ReadOnly = true;
			this.m_text_font.Size = new System.Drawing.Size(226, 20);
			this.m_text_font.TabIndex = 0;
			// 
			// m_group_misc
			// 
			this.m_group_misc.Controls.Add(this.m_lbl_tabsize);
			this.m_group_misc.Controls.Add(this.m_spinner_tabsize);
			this.m_group_misc.Controls.Add(this.m_check_full_filepath_in_title);
			this.m_group_misc.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_group_misc.Location = new System.Drawing.Point(3, 203);
			this.m_group_misc.Name = "m_group_misc";
			this.m_group_misc.Size = new System.Drawing.Size(321, 69);
			this.m_group_misc.TabIndex = 6;
			this.m_group_misc.TabStop = false;
			this.m_group_misc.Text = "Miscellaneous";
			// 
			// m_lbl_tabsize
			// 
			this.m_lbl_tabsize.AutoSize = true;
			this.m_lbl_tabsize.Location = new System.Drawing.Point(7, 43);
			this.m_lbl_tabsize.Name = "m_lbl_tabsize";
			this.m_lbl_tabsize.Size = new System.Drawing.Size(98, 13);
			this.m_lbl_tabsize.TabIndex = 2;
			this.m_lbl_tabsize.Text = "Tab size in spaces:";
			// 
			// m_spinner_tabsize
			// 
			this.m_spinner_tabsize.Location = new System.Drawing.Point(111, 41);
			this.m_spinner_tabsize.Name = "m_spinner_tabsize";
			this.m_spinner_tabsize.Size = new System.Drawing.Size(56, 20);
			this.m_spinner_tabsize.TabIndex = 1;
			// 
			// m_check_full_filepath_in_title
			// 
			this.m_check_full_filepath_in_title.AutoSize = true;
			this.m_check_full_filepath_in_title.Location = new System.Drawing.Point(10, 19);
			this.m_check_full_filepath_in_title.Name = "m_check_full_filepath_in_title";
			this.m_check_full_filepath_in_title.Size = new System.Drawing.Size(157, 17);
			this.m_check_full_filepath_in_title.TabIndex = 0;
			this.m_check_full_filepath_in_title.Text = "Show full file path in title bar";
			this.m_check_full_filepath_in_title.UseVisualStyleBackColor = true;
			// 
			// m_table_appearance2
			// 
			this.m_table_appearance2.ColumnCount = 1;
			this.m_table_appearance2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table_appearance2.Controls.Add(this.m_group_file_scroll, 0, 0);
			this.m_table_appearance2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_table_appearance2.Location = new System.Drawing.Point(330, 3);
			this.m_table_appearance2.Name = "m_table_appearance2";
			this.m_table_appearance2.RowCount = 2;
			this.m_table_appearance2.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table_appearance2.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table_appearance2.Size = new System.Drawing.Size(165, 422);
			this.m_table_appearance2.TabIndex = 1;
			// 
			// m_group_file_scroll
			// 
			this.m_group_file_scroll.Controls.Add(this.m_lbl_fs_cached_colour);
			this.m_group_file_scroll.Controls.Add(this.m_lbl_fs_edit_cached_colour);
			this.m_group_file_scroll.Controls.Add(this.m_lbl_fs_bookmark_colour);
			this.m_group_file_scroll.Controls.Add(this.m_lbl_fs_edit_bookmark_colour);
			this.m_group_file_scroll.Controls.Add(this.m_lbl_fs_visible_colour);
			this.m_group_file_scroll.Controls.Add(this.m_lbl_fs_edit_visible_colour);
			this.m_group_file_scroll.Controls.Add(this.m_spinner_file_scroll_width);
			this.m_group_file_scroll.Controls.Add(this.m_lbl_file_scroll_width);
			this.m_group_file_scroll.Location = new System.Drawing.Point(3, 3);
			this.m_group_file_scroll.Name = "m_group_file_scroll";
			this.m_group_file_scroll.Size = new System.Drawing.Size(159, 133);
			this.m_group_file_scroll.TabIndex = 6;
			this.m_group_file_scroll.TabStop = false;
			this.m_group_file_scroll.Text = "File Scroll Bar";
			// 
			// m_lbl_fs_cached_colour
			// 
			this.m_lbl_fs_cached_colour.AutoSize = true;
			this.m_lbl_fs_cached_colour.Location = new System.Drawing.Point(17, 47);
			this.m_lbl_fs_cached_colour.Name = "m_lbl_fs_cached_colour";
			this.m_lbl_fs_cached_colour.Size = new System.Drawing.Size(80, 13);
			this.m_lbl_fs_cached_colour.TabIndex = 19;
			this.m_lbl_fs_cached_colour.Text = "Cached Colour:";
			// 
			// m_lbl_fs_edit_cached_colour
			// 
			this.m_lbl_fs_edit_cached_colour.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_lbl_fs_edit_cached_colour.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_lbl_fs_edit_cached_colour.Location = new System.Drawing.Point(98, 43);
			this.m_lbl_fs_edit_cached_colour.Name = "m_lbl_fs_edit_cached_colour";
			this.m_lbl_fs_edit_cached_colour.Size = new System.Drawing.Size(55, 21);
			this.m_lbl_fs_edit_cached_colour.TabIndex = 18;
			this.m_lbl_fs_edit_cached_colour.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// m_lbl_fs_bookmark_colour
			// 
			this.m_lbl_fs_bookmark_colour.AutoSize = true;
			this.m_lbl_fs_bookmark_colour.Location = new System.Drawing.Point(6, 104);
			this.m_lbl_fs_bookmark_colour.Name = "m_lbl_fs_bookmark_colour";
			this.m_lbl_fs_bookmark_colour.Size = new System.Drawing.Size(91, 13);
			this.m_lbl_fs_bookmark_colour.TabIndex = 17;
			this.m_lbl_fs_bookmark_colour.Text = "Bookmark Colour:";
			// 
			// m_lbl_fs_edit_bookmark_colour
			// 
			this.m_lbl_fs_edit_bookmark_colour.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_lbl_fs_edit_bookmark_colour.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_lbl_fs_edit_bookmark_colour.Location = new System.Drawing.Point(98, 100);
			this.m_lbl_fs_edit_bookmark_colour.Name = "m_lbl_fs_edit_bookmark_colour";
			this.m_lbl_fs_edit_bookmark_colour.Size = new System.Drawing.Size(55, 21);
			this.m_lbl_fs_edit_bookmark_colour.TabIndex = 16;
			this.m_lbl_fs_edit_bookmark_colour.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// m_lbl_fs_visible_colour
			// 
			this.m_lbl_fs_visible_colour.AutoSize = true;
			this.m_lbl_fs_visible_colour.Location = new System.Drawing.Point(24, 76);
			this.m_lbl_fs_visible_colour.Name = "m_lbl_fs_visible_colour";
			this.m_lbl_fs_visible_colour.Size = new System.Drawing.Size(73, 13);
			this.m_lbl_fs_visible_colour.TabIndex = 15;
			this.m_lbl_fs_visible_colour.Text = "Visible Colour:";
			// 
			// m_lbl_fs_edit_visible_colour
			// 
			this.m_lbl_fs_edit_visible_colour.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_lbl_fs_edit_visible_colour.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_lbl_fs_edit_visible_colour.Location = new System.Drawing.Point(98, 72);
			this.m_lbl_fs_edit_visible_colour.Name = "m_lbl_fs_edit_visible_colour";
			this.m_lbl_fs_edit_visible_colour.Size = new System.Drawing.Size(55, 21);
			this.m_lbl_fs_edit_visible_colour.TabIndex = 9;
			this.m_lbl_fs_edit_visible_colour.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// m_spinner_file_scroll_width
			// 
			this.m_spinner_file_scroll_width.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_spinner_file_scroll_width.Location = new System.Drawing.Point(98, 17);
			this.m_spinner_file_scroll_width.Name = "m_spinner_file_scroll_width";
			this.m_spinner_file_scroll_width.Size = new System.Drawing.Size(55, 20);
			this.m_spinner_file_scroll_width.TabIndex = 5;
			// 
			// m_lbl_file_scroll_width
			// 
			this.m_lbl_file_scroll_width.AutoSize = true;
			this.m_lbl_file_scroll_width.Location = new System.Drawing.Point(40, 19);
			this.m_lbl_file_scroll_width.Name = "m_lbl_file_scroll_width";
			this.m_lbl_file_scroll_width.Size = new System.Drawing.Size(57, 13);
			this.m_lbl_file_scroll_width.TabIndex = 14;
			this.m_lbl_file_scroll_width.Text = "Bar Width:";
			// 
			// m_tab_highlight
			// 
			this.m_tab_highlight.Controls.Add(this.m_split_hl);
			this.m_tab_highlight.Location = new System.Drawing.Point(4, 22);
			this.m_tab_highlight.Margin = new System.Windows.Forms.Padding(0);
			this.m_tab_highlight.Name = "m_tab_highlight";
			this.m_tab_highlight.Padding = new System.Windows.Forms.Padding(0, 1, 2, 1);
			this.m_tab_highlight.Size = new System.Drawing.Size(500, 430);
			this.m_tab_highlight.TabIndex = 1;
			this.m_tab_highlight.Text = "Highlight";
			this.m_tab_highlight.UseVisualStyleBackColor = true;
			// 
			// m_split_hl
			// 
			this.m_split_hl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_split_hl.Location = new System.Drawing.Point(0, 1);
			this.m_split_hl.Margin = new System.Windows.Forms.Padding(0);
			this.m_split_hl.Name = "m_split_hl";
			this.m_split_hl.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// m_split_hl.Panel1
			// 
			this.m_split_hl.Panel1.Controls.Add(this.m_pattern_hl);
			// 
			// m_split_hl.Panel2
			// 
			this.m_split_hl.Panel2.Controls.Add(this.m_table_hl);
			this.m_split_hl.Size = new System.Drawing.Size(498, 428);
			this.m_split_hl.SplitterDistance = 176;
			this.m_split_hl.TabIndex = 3;
			// 
			// m_pattern_hl
			// 
			this.m_pattern_hl.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_pattern_hl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_pattern_hl.Location = new System.Drawing.Point(0, 0);
			this.m_pattern_hl.Margin = new System.Windows.Forms.Padding(0);
			this.m_pattern_hl.MinimumSize = new System.Drawing.Size(336, 92);
			this.m_pattern_hl.Name = "m_pattern_hl";
			this.m_pattern_hl.Size = new System.Drawing.Size(498, 176);
			this.m_pattern_hl.TabIndex = 0;
			this.m_pattern_hl.TestText = "Enter text here to test your pattern";
			this.m_pattern_hl.Touched = false;
			// 
			// m_table_hl
			// 
			this.m_table_hl.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single;
			this.m_table_hl.ColumnCount = 1;
			this.m_table_hl.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table_hl.Controls.Add(this.m_grid_highlight, 0, 1);
			this.m_table_hl.Controls.Add(this.m_pattern_set_hl, 0, 2);
			this.m_table_hl.Controls.Add(this.label2, 0, 0);
			this.m_table_hl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_table_hl.Location = new System.Drawing.Point(0, 0);
			this.m_table_hl.Margin = new System.Windows.Forms.Padding(0);
			this.m_table_hl.Name = "m_table_hl";
			this.m_table_hl.RowCount = 3;
			this.m_table_hl.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table_hl.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table_hl.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table_hl.Size = new System.Drawing.Size(498, 248);
			this.m_table_hl.TabIndex = 2;
			// 
			// m_grid_highlight
			// 
			this.m_grid_highlight.AllowUserToAddRows = false;
			this.m_grid_highlight.AllowUserToOrderColumns = true;
			this.m_grid_highlight.AllowUserToResizeRows = false;
			this.m_grid_highlight.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_grid_highlight.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
			this.m_grid_highlight.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_grid_highlight.Location = new System.Drawing.Point(1, 34);
			this.m_grid_highlight.Margin = new System.Windows.Forms.Padding(0);
			this.m_grid_highlight.MultiSelect = false;
			this.m_grid_highlight.Name = "m_grid_highlight";
			this.m_grid_highlight.RowHeadersWidth = 24;
			this.m_grid_highlight.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid_highlight.Size = new System.Drawing.Size(496, 172);
			this.m_grid_highlight.TabIndex = 0;
			// 
			// m_pattern_set_hl
			// 
			this.m_pattern_set_hl.AutoSize = true;
			this.m_pattern_set_hl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_pattern_set_hl.Location = new System.Drawing.Point(1, 207);
			this.m_pattern_set_hl.Margin = new System.Windows.Forms.Padding(0);
			this.m_pattern_set_hl.MinimumSize = new System.Drawing.Size(274, 38);
			this.m_pattern_set_hl.Name = "m_pattern_set_hl";
			this.m_pattern_set_hl.Size = new System.Drawing.Size(496, 40);
			this.m_pattern_set_hl.TabIndex = 1;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(4, 1);
			this.label2.Name = "label2";
			this.label2.Padding = new System.Windows.Forms.Padding(3);
			this.label2.Size = new System.Drawing.Size(313, 32);
			this.label2.TabIndex = 2;
			this.label2.Text = "A line in the log is highlighted based on the first match in this list.\r\nThe list" +
    " order can be changed by dragging the rows.\r\n";
			// 
			// m_tab_filter
			// 
			this.m_tab_filter.Controls.Add(this.m_split_ft);
			this.m_tab_filter.Location = new System.Drawing.Point(4, 22);
			this.m_tab_filter.Margin = new System.Windows.Forms.Padding(0);
			this.m_tab_filter.Name = "m_tab_filter";
			this.m_tab_filter.Padding = new System.Windows.Forms.Padding(0, 1, 2, 1);
			this.m_tab_filter.Size = new System.Drawing.Size(500, 430);
			this.m_tab_filter.TabIndex = 2;
			this.m_tab_filter.Text = "Filter";
			this.m_tab_filter.UseVisualStyleBackColor = true;
			// 
			// m_split_ft
			// 
			this.m_split_ft.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_split_ft.Location = new System.Drawing.Point(0, 1);
			this.m_split_ft.Margin = new System.Windows.Forms.Padding(0);
			this.m_split_ft.Name = "m_split_ft";
			this.m_split_ft.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// m_split_ft.Panel1
			// 
			this.m_split_ft.Panel1.Controls.Add(this.m_pattern_ft);
			// 
			// m_split_ft.Panel2
			// 
			this.m_split_ft.Panel2.Controls.Add(this.m_table_ft);
			this.m_split_ft.Size = new System.Drawing.Size(498, 428);
			this.m_split_ft.SplitterDistance = 145;
			this.m_split_ft.TabIndex = 5;
			// 
			// m_pattern_ft
			// 
			this.m_pattern_ft.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_pattern_ft.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_pattern_ft.Location = new System.Drawing.Point(0, 0);
			this.m_pattern_ft.Margin = new System.Windows.Forms.Padding(0);
			this.m_pattern_ft.MinimumSize = new System.Drawing.Size(336, 92);
			this.m_pattern_ft.Name = "m_pattern_ft";
			this.m_pattern_ft.Size = new System.Drawing.Size(498, 145);
			this.m_pattern_ft.TabIndex = 0;
			this.m_pattern_ft.TestText = "Enter text here to test your pattern";
			this.m_pattern_ft.Touched = false;
			// 
			// m_table_ft
			// 
			this.m_table_ft.ColumnCount = 2;
			this.m_table_ft.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table_ft.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.m_table_ft.Controls.Add(this.m_lbl_ft_grid_desc, 0, 0);
			this.m_table_ft.Controls.Add(this.m_grid_filter, 0, 1);
			this.m_table_ft.Controls.Add(this.m_pattern_set_ft, 0, 2);
			this.m_table_ft.Controls.Add(this.m_check_reject_all_by_default, 1, 0);
			this.m_table_ft.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_table_ft.Location = new System.Drawing.Point(0, 0);
			this.m_table_ft.Margin = new System.Windows.Forms.Padding(0);
			this.m_table_ft.Name = "m_table_ft";
			this.m_table_ft.RowCount = 3;
			this.m_table_ft.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table_ft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table_ft.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table_ft.Size = new System.Drawing.Size(498, 279);
			this.m_table_ft.TabIndex = 4;
			// 
			// m_lbl_ft_grid_desc
			// 
			this.m_lbl_ft_grid_desc.AutoSize = true;
			this.m_lbl_ft_grid_desc.Location = new System.Drawing.Point(3, 0);
			this.m_lbl_ft_grid_desc.Name = "m_lbl_ft_grid_desc";
			this.m_lbl_ft_grid_desc.Padding = new System.Windows.Forms.Padding(3);
			this.m_lbl_ft_grid_desc.Size = new System.Drawing.Size(293, 32);
			this.m_lbl_ft_grid_desc.TabIndex = 3;
			this.m_lbl_ft_grid_desc.Text = "A line in the log is filtered based on the first match in this list.\r\nThe list or" +
    "der can be changed by dragging the rows.\r\n";
			// 
			// m_grid_filter
			// 
			this.m_grid_filter.AllowUserToAddRows = false;
			this.m_grid_filter.AllowUserToOrderColumns = true;
			this.m_grid_filter.AllowUserToResizeRows = false;
			this.m_grid_filter.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_grid_filter.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
			this.m_table_ft.SetColumnSpan(this.m_grid_filter, 2);
			this.m_grid_filter.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_grid_filter.Location = new System.Drawing.Point(0, 32);
			this.m_grid_filter.Margin = new System.Windows.Forms.Padding(0);
			this.m_grid_filter.MultiSelect = false;
			this.m_grid_filter.Name = "m_grid_filter";
			this.m_grid_filter.RowHeadersWidth = 24;
			this.m_grid_filter.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid_filter.Size = new System.Drawing.Size(498, 207);
			this.m_grid_filter.TabIndex = 0;
			// 
			// m_pattern_set_ft
			// 
			this.m_pattern_set_ft.AutoSize = true;
			this.m_table_ft.SetColumnSpan(this.m_pattern_set_ft, 2);
			this.m_pattern_set_ft.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_pattern_set_ft.Location = new System.Drawing.Point(0, 239);
			this.m_pattern_set_ft.Margin = new System.Windows.Forms.Padding(0);
			this.m_pattern_set_ft.MinimumSize = new System.Drawing.Size(274, 38);
			this.m_pattern_set_ft.Name = "m_pattern_set_ft";
			this.m_pattern_set_ft.Size = new System.Drawing.Size(498, 40);
			this.m_pattern_set_ft.TabIndex = 1;
			// 
			// m_check_reject_all_by_default
			// 
			this.m_check_reject_all_by_default.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.m_check_reject_all_by_default.AutoSize = true;
			this.m_check_reject_all_by_default.Location = new System.Drawing.Point(351, 7);
			this.m_check_reject_all_by_default.Name = "m_check_reject_all_by_default";
			this.m_check_reject_all_by_default.Size = new System.Drawing.Size(144, 17);
			this.m_check_reject_all_by_default.TabIndex = 4;
			this.m_check_reject_all_by_default.Text = "Reject all rows by default";
			this.m_check_reject_all_by_default.UseVisualStyleBackColor = true;
			// 
			// m_tab_transform
			// 
			this.m_tab_transform.Controls.Add(this.m_split_tx);
			this.m_tab_transform.Location = new System.Drawing.Point(4, 22);
			this.m_tab_transform.Margin = new System.Windows.Forms.Padding(0);
			this.m_tab_transform.Name = "m_tab_transform";
			this.m_tab_transform.Padding = new System.Windows.Forms.Padding(0, 1, 2, 1);
			this.m_tab_transform.Size = new System.Drawing.Size(500, 430);
			this.m_tab_transform.TabIndex = 4;
			this.m_tab_transform.Text = "Transform";
			this.m_tab_transform.UseVisualStyleBackColor = true;
			// 
			// m_split_tx
			// 
			this.m_split_tx.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_split_tx.Location = new System.Drawing.Point(0, 1);
			this.m_split_tx.Margin = new System.Windows.Forms.Padding(0);
			this.m_split_tx.Name = "m_split_tx";
			this.m_split_tx.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// m_split_tx.Panel1
			// 
			this.m_split_tx.Panel1.Controls.Add(this.m_pattern_tx);
			// 
			// m_split_tx.Panel2
			// 
			this.m_split_tx.Panel2.Controls.Add(this.m_table_tx);
			this.m_split_tx.Size = new System.Drawing.Size(498, 428);
			this.m_split_tx.SplitterDistance = 163;
			this.m_split_tx.TabIndex = 0;
			// 
			// m_pattern_tx
			// 
			this.m_pattern_tx.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_pattern_tx.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_pattern_tx.Location = new System.Drawing.Point(0, 0);
			this.m_pattern_tx.Margin = new System.Windows.Forms.Padding(0);
			this.m_pattern_tx.MinimumSize = new System.Drawing.Size(400, 150);
			this.m_pattern_tx.Name = "m_pattern_tx";
			this.m_pattern_tx.Size = new System.Drawing.Size(498, 163);
			this.m_pattern_tx.TabIndex = 0;
			this.m_pattern_tx.TestText = "Enter text here to test your pattern";
			this.m_pattern_tx.Touched = false;
			// 
			// m_table_tx
			// 
			this.m_table_tx.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single;
			this.m_table_tx.ColumnCount = 1;
			this.m_table_tx.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table_tx.Controls.Add(this.label4, 0, 0);
			this.m_table_tx.Controls.Add(this.m_grid_transform, 0, 1);
			this.m_table_tx.Controls.Add(this.m_pattern_set_tx, 0, 2);
			this.m_table_tx.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_table_tx.Location = new System.Drawing.Point(0, 0);
			this.m_table_tx.Margin = new System.Windows.Forms.Padding(0);
			this.m_table_tx.Name = "m_table_tx";
			this.m_table_tx.RowCount = 3;
			this.m_table_tx.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table_tx.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table_tx.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table_tx.Size = new System.Drawing.Size(498, 261);
			this.m_table_tx.TabIndex = 3;
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(4, 1);
			this.label4.Name = "label4";
			this.label4.Padding = new System.Windows.Forms.Padding(3);
			this.label4.Size = new System.Drawing.Size(259, 32);
			this.label4.TabIndex = 4;
			this.label4.Text = "Transforms are applied in the order given in this list.\r\nThe list order can be ch" +
    "anged by dragging the rows.\r\n";
			// 
			// m_grid_transform
			// 
			this.m_grid_transform.AllowUserToAddRows = false;
			this.m_grid_transform.AllowUserToOrderColumns = true;
			this.m_grid_transform.AllowUserToResizeRows = false;
			this.m_grid_transform.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_grid_transform.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
			this.m_grid_transform.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_grid_transform.Location = new System.Drawing.Point(1, 34);
			this.m_grid_transform.Margin = new System.Windows.Forms.Padding(0);
			this.m_grid_transform.MultiSelect = false;
			this.m_grid_transform.Name = "m_grid_transform";
			this.m_grid_transform.RowHeadersWidth = 24;
			this.m_grid_transform.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid_transform.Size = new System.Drawing.Size(496, 185);
			this.m_grid_transform.TabIndex = 0;
			// 
			// m_pattern_set_tx
			// 
			this.m_pattern_set_tx.AutoSize = true;
			this.m_pattern_set_tx.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_pattern_set_tx.Location = new System.Drawing.Point(1, 220);
			this.m_pattern_set_tx.Margin = new System.Windows.Forms.Padding(0);
			this.m_pattern_set_tx.MinimumSize = new System.Drawing.Size(274, 38);
			this.m_pattern_set_tx.Name = "m_pattern_set_tx";
			this.m_pattern_set_tx.Size = new System.Drawing.Size(496, 40);
			this.m_pattern_set_tx.TabIndex = 1;
			// 
			// m_tab_action
			// 
			this.m_tab_action.Controls.Add(this.m_split_ac);
			this.m_tab_action.Location = new System.Drawing.Point(4, 22);
			this.m_tab_action.Margin = new System.Windows.Forms.Padding(0);
			this.m_tab_action.Name = "m_tab_action";
			this.m_tab_action.Padding = new System.Windows.Forms.Padding(0, 1, 2, 1);
			this.m_tab_action.Size = new System.Drawing.Size(500, 430);
			this.m_tab_action.TabIndex = 5;
			this.m_tab_action.Text = "Actions";
			this.m_tab_action.UseVisualStyleBackColor = true;
			// 
			// m_split_ac
			// 
			this.m_split_ac.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_split_ac.Location = new System.Drawing.Point(0, 1);
			this.m_split_ac.Margin = new System.Windows.Forms.Padding(0);
			this.m_split_ac.Name = "m_split_ac";
			this.m_split_ac.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// m_split_ac.Panel1
			// 
			this.m_split_ac.Panel1.Controls.Add(this.m_pattern_ac);
			// 
			// m_split_ac.Panel2
			// 
			this.m_split_ac.Panel2.Controls.Add(this.m_table_ac);
			this.m_split_ac.Size = new System.Drawing.Size(498, 428);
			this.m_split_ac.SplitterDistance = 145;
			this.m_split_ac.TabIndex = 4;
			// 
			// m_pattern_ac
			// 
			this.m_pattern_ac.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_pattern_ac.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_pattern_ac.Location = new System.Drawing.Point(0, 0);
			this.m_pattern_ac.Margin = new System.Windows.Forms.Padding(0);
			this.m_pattern_ac.MinimumSize = new System.Drawing.Size(336, 92);
			this.m_pattern_ac.Name = "m_pattern_ac";
			this.m_pattern_ac.Size = new System.Drawing.Size(498, 145);
			this.m_pattern_ac.TabIndex = 0;
			this.m_pattern_ac.TestText = "Enter text here to test your pattern";
			this.m_pattern_ac.Touched = false;
			// 
			// m_table_ac
			// 
			this.m_table_ac.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single;
			this.m_table_ac.ColumnCount = 1;
			this.m_table_ac.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table_ac.Controls.Add(this.label5, 0, 0);
			this.m_table_ac.Controls.Add(this.m_grid_action, 0, 1);
			this.m_table_ac.Controls.Add(this.m_pattern_set_ac, 0, 2);
			this.m_table_ac.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_table_ac.Location = new System.Drawing.Point(0, 0);
			this.m_table_ac.Margin = new System.Windows.Forms.Padding(0);
			this.m_table_ac.Name = "m_table_ac";
			this.m_table_ac.RowCount = 3;
			this.m_table_ac.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table_ac.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table_ac.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table_ac.Size = new System.Drawing.Size(498, 279);
			this.m_table_ac.TabIndex = 2;
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(4, 1);
			this.label5.Name = "label5";
			this.label5.Padding = new System.Windows.Forms.Padding(3);
			this.label5.Size = new System.Drawing.Size(269, 32);
			this.label5.TabIndex = 4;
			this.label5.Text = "An action is applied based on the first match in this list.\r\nThe list order can b" +
    "e changed by dragging the rows.\r\n";
			// 
			// m_grid_action
			// 
			this.m_grid_action.AllowUserToAddRows = false;
			this.m_grid_action.AllowUserToOrderColumns = true;
			this.m_grid_action.AllowUserToResizeRows = false;
			this.m_grid_action.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_grid_action.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
			this.m_grid_action.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_grid_action.Location = new System.Drawing.Point(1, 34);
			this.m_grid_action.Margin = new System.Windows.Forms.Padding(0);
			this.m_grid_action.MultiSelect = false;
			this.m_grid_action.Name = "m_grid_action";
			this.m_grid_action.RowHeadersWidth = 24;
			this.m_grid_action.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid_action.Size = new System.Drawing.Size(496, 203);
			this.m_grid_action.TabIndex = 0;
			// 
			// m_pattern_set_ac
			// 
			this.m_pattern_set_ac.AutoSize = true;
			this.m_pattern_set_ac.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_pattern_set_ac.Location = new System.Drawing.Point(1, 238);
			this.m_pattern_set_ac.Margin = new System.Windows.Forms.Padding(0);
			this.m_pattern_set_ac.MinimumSize = new System.Drawing.Size(274, 38);
			this.m_pattern_set_ac.Name = "m_pattern_set_ac";
			this.m_pattern_set_ac.Size = new System.Drawing.Size(496, 40);
			this.m_pattern_set_ac.TabIndex = 1;
			// 
			// m_image_list
			// 
			this.m_image_list.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_image_list.ImageStream")));
			this.m_image_list.TransparentColor = System.Drawing.Color.Transparent;
			this.m_image_list.Images.SetKeyName(0, "fileclose.png");
			this.m_image_list.Images.SetKeyName(1, "pencil.png");
			// 
			// SettingsUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(508, 458);
			this.Controls.Add(this.m_tabctrl);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MinimumSize = new System.Drawing.Size(524, 277);
			this.Name = "SettingsUI";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Options";
			this.m_tabctrl.ResumeLayout(false);
			this.m_tab_general.ResumeLayout(false);
			this.m_table_general0.ResumeLayout(false);
			this.m_group_settings.ResumeLayout(false);
			this.m_group_settings.PerformLayout();
			this.m_table_general1.ResumeLayout(false);
			this.m_group_startup.ResumeLayout(false);
			this.m_group_startup.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_web_proxy_port)).EndInit();
			this.m_group_grid.ResumeLayout(false);
			this.m_group_grid.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_max_line_length)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_line_cache_count)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_max_mem_range)).EndInit();
			this.m_table_general2.ResumeLayout(false);
			this.m_group_line_ends.ResumeLayout(false);
			this.m_group_line_ends.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_column_count)).EndInit();
			this.m_tab_logview.ResumeLayout(false);
			this.m_table_appearance0.ResumeLayout(false);
			this.m_table_appearance1.ResumeLayout(false);
			this.m_group_log_text_colours.ResumeLayout(false);
			this.m_group_log_text_colours.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_row_height)).EndInit();
			this.m_group_font.ResumeLayout(false);
			this.m_group_font.PerformLayout();
			this.m_group_misc.ResumeLayout(false);
			this.m_group_misc.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_tabsize)).EndInit();
			this.m_table_appearance2.ResumeLayout(false);
			this.m_group_file_scroll.ResumeLayout(false);
			this.m_group_file_scroll.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_file_scroll_width)).EndInit();
			this.m_tab_highlight.ResumeLayout(false);
			this.m_split_hl.Panel1.ResumeLayout(false);
			this.m_split_hl.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_split_hl)).EndInit();
			this.m_split_hl.ResumeLayout(false);
			this.m_table_hl.ResumeLayout(false);
			this.m_table_hl.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_highlight)).EndInit();
			this.m_tab_filter.ResumeLayout(false);
			this.m_split_ft.Panel1.ResumeLayout(false);
			this.m_split_ft.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_split_ft)).EndInit();
			this.m_split_ft.ResumeLayout(false);
			this.m_table_ft.ResumeLayout(false);
			this.m_table_ft.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_filter)).EndInit();
			this.m_tab_transform.ResumeLayout(false);
			this.m_split_tx.Panel1.ResumeLayout(false);
			this.m_split_tx.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_split_tx)).EndInit();
			this.m_split_tx.ResumeLayout(false);
			this.m_table_tx.ResumeLayout(false);
			this.m_table_tx.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_transform)).EndInit();
			this.m_tab_action.ResumeLayout(false);
			this.m_split_ac.Panel1.ResumeLayout(false);
			this.m_split_ac.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_split_ac)).EndInit();
			this.m_split_ac.ResumeLayout(false);
			this.m_table_ac.ResumeLayout(false);
			this.m_table_ac.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_action)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TabControl m_tabctrl;
		private System.Windows.Forms.TabPage m_tab_general;
		private System.Windows.Forms.TabPage m_tab_highlight;
		private DataGridView m_grid_highlight;
		private System.Windows.Forms.TabPage m_tab_filter;
		private PatternUI m_pattern_hl;
		private System.Windows.Forms.GroupBox m_group_startup;
		private System.Windows.Forms.CheckBox m_check_load_last_file;
		private System.Windows.Forms.CheckBox m_check_save_screen_loc;
		private PatternUI m_pattern_ft;
		private DataGridView m_grid_filter;
		private System.Windows.Forms.SplitContainer m_split_hl;
		private System.Windows.Forms.SplitContainer m_split_ft;
		private System.Windows.Forms.CheckBox m_check_show_totd;
		private System.Windows.Forms.TableLayoutPanel m_table_hl;
		private PatternSetHL m_pattern_set_hl;
		private System.Windows.Forms.TableLayoutPanel m_table_ft;
		private PatternSetFT m_pattern_set_ft;
		private System.Windows.Forms.GroupBox m_group_line_ends;
		private System.Windows.Forms.Label m_lbl_line_ends;
		private System.Windows.Forms.TextBox m_edit_line_ends;
		private System.Windows.Forms.GroupBox m_group_grid;
		private System.Windows.Forms.Label m_lbl_history_length0;
		private System.Windows.Forms.NumericUpDown m_spinner_line_cache_count;
		private System.Windows.Forms.Label m_lbl_col_delims;
		private System.Windows.Forms.CheckBox m_check_open_at_end;
		private System.Windows.Forms.CheckBox m_check_ignore_blank_lines;
		private System.Windows.Forms.TabPage m_tab_logview;
		private System.Windows.Forms.GroupBox m_group_font;
		private System.Windows.Forms.Button m_btn_change_font;
		private System.Windows.Forms.TextBox m_text_font;
		private System.Windows.Forms.GroupBox m_group_log_text_colours;
		private System.Windows.Forms.NumericUpDown m_spinner_file_scroll_width;
		private System.Windows.Forms.Label m_lbl_file_scroll_width;
		private System.Windows.Forms.Label m_lbl_line2_example;
		private System.Windows.Forms.Label m_lbl_line1_example;
		private System.Windows.Forms.Label m_lbl_row_height;
		private System.Windows.Forms.NumericUpDown m_spinner_row_height;
		private System.Windows.Forms.Label m_lbl_selection_example;
		private System.Windows.Forms.Label m_lbl_line1_colours;
		private System.Windows.Forms.Label m_lbl_selection_colour;
		private System.Windows.Forms.CheckBox m_check_alternate_line_colour;
		private System.Windows.Forms.CheckBox m_check_file_changes_additive;
		private System.Windows.Forms.TextBox m_edit_col_delims;
		private System.Windows.Forms.Label m_lbl_column_count;
		private System.Windows.Forms.NumericUpDown m_spinner_column_count;
		private System.Windows.Forms.NumericUpDown m_spinner_max_mem_range;
		private System.Windows.Forms.Label m_lbl_max_scan_size0;
		private System.Windows.Forms.Label m_lbl_history_length1;
		private System.Windows.Forms.Label m_lbl_max_scan_size1;
		private System.Windows.Forms.CheckBox m_check_c4u;
		private System.Windows.Forms.TabPage m_tab_transform;
		private System.Windows.Forms.SplitContainer m_split_tx;
		private TransformUI m_pattern_tx;
		private System.Windows.Forms.TableLayoutPanel m_table_tx;
		private DataGridView m_grid_transform;
		private PatternSetTX m_pattern_set_tx;
		private System.Windows.Forms.Label m_lbl_max_line_len_kb;
		private System.Windows.Forms.NumericUpDown m_spinner_max_line_length;
		private System.Windows.Forms.Label m_lbl_max_line_length;
		private System.Windows.Forms.GroupBox m_group_settings;
		private System.Windows.Forms.Button m_btn_settings_reset;
		private System.Windows.Forms.Button m_btn_settings_save;
		private System.Windows.Forms.Button m_btn_settings_load;
		private System.Windows.Forms.TextBox m_text_settings;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ImageList m_image_list;
		private System.Windows.Forms.TabPage m_tab_action;
		private System.Windows.Forms.SplitContainer m_split_ac;
		private PatternUI m_pattern_ac;
		private System.Windows.Forms.TableLayoutPanel m_table_ac;
		private DataGridView m_grid_action;
		private PatternSetAC m_pattern_set_ac;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label m_lbl_ft_grid_desc;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.CheckBox m_check_reject_all_by_default;
		private System.Windows.Forms.Label m_lbl_web_proxy_port;
		private System.Windows.Forms.Label m_lbl_web_proxy_host;
		private System.Windows.Forms.NumericUpDown m_spinner_web_proxy_port;
		private System.Windows.Forms.TextBox m_edit_web_proxy_host;
		private System.Windows.Forms.CheckBox m_check_use_web_proxy;
		private System.Windows.Forms.GroupBox m_group_file_scroll;
		private System.Windows.Forms.Label m_lbl_fs_bookmark_colour;
		private System.Windows.Forms.Label m_lbl_fs_edit_bookmark_colour;
		private System.Windows.Forms.Label m_lbl_fs_visible_colour;
		private System.Windows.Forms.Label m_lbl_fs_edit_visible_colour;
		private System.Windows.Forms.Label m_lbl_fs_cached_colour;
		private System.Windows.Forms.Label m_lbl_fs_edit_cached_colour;
		private System.Windows.Forms.TableLayoutPanel m_table_general0;
		private System.Windows.Forms.TableLayoutPanel m_table_general1;
		private System.Windows.Forms.TableLayoutPanel m_table_general2;
		private System.Windows.Forms.TableLayoutPanel m_table_appearance0;
		private System.Windows.Forms.TableLayoutPanel m_table_appearance1;
		private System.Windows.Forms.GroupBox m_group_misc;
		private System.Windows.Forms.CheckBox m_check_full_filepath_in_title;
		private System.Windows.Forms.TableLayoutPanel m_table_appearance2;
		private System.Windows.Forms.Label m_lbl_tabsize;
		private System.Windows.Forms.NumericUpDown m_spinner_tabsize;
	}
}