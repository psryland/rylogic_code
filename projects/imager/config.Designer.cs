namespace imager
{
	partial class Config
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Config));
			this.m_tab = new System.Windows.Forms.TabControl();
			this.m_tab_page_general = new System.Windows.Forms.TabPage();
			this.m_lbl_reset_to_defaults = new System.Windows.Forms.Label();
			this.m_btn_reset = new System.Windows.Forms.Button();
			this.m_check_audio_files = new System.Windows.Forms.CheckBox();
			this.m_check_video_files = new System.Windows.Forms.CheckBox();
			this.m_check_image_files = new System.Windows.Forms.CheckBox();
			this.m_chklist_audio_formats = new System.Windows.Forms.CheckedListBox();
			this.m_chklist_video_formats = new System.Windows.Forms.CheckedListBox();
			this.m_chklist_image_formats = new System.Windows.Forms.CheckedListBox();
			this.m_check_startup_version_check = new System.Windows.Forms.CheckBox();
			this.m_lbl_media_types = new System.Windows.Forms.Label();
			this.m_tab_view = new System.Windows.Forms.TabPage();
			this.m_lbl_primary_display = new System.Windows.Forms.Label();
			this.m_combo_primary_display = new System.Windows.Forms.ComboBox();
			this.m_check_always_on_top = new System.Windows.Forms.CheckBox();
			this.m_check_show_filenames = new System.Windows.Forms.CheckBox();
			this.m_edit_slide_show_rate = new System.Windows.Forms.TextBox();
			this.m_lbl_slide_show_rate = new System.Windows.Forms.Label();
			this.m_lbl_zoom_type = new System.Windows.Forms.Label();
			this.m_combo_zoom_type = new System.Windows.Forms.ComboBox();
			this.m_check_reset_zoom_on_load = new System.Windows.Forms.CheckBox();
			this.m_tab_media_list = new System.Windows.Forms.TabPage();
			this.m_lbl_file_sort_order = new System.Windows.Forms.Label();
			this.m_combo_file_order = new System.Windows.Forms.ComboBox();
			this.m_lbl_folder_order = new System.Windows.Forms.Label();
			this.m_combo_folder_order = new System.Windows.Forms.ComboBox();
			this.m_check_allow_duplicates = new System.Windows.Forms.CheckBox();
			this.tabPage1 = new System.Windows.Forms.TabPage();
			this.m_lbl_volume = new System.Windows.Forms.Label();
			this.m_track_ss_volume = new System.Windows.Forms.TrackBar();
			this.m_lbl_ss_file_order = new System.Windows.Forms.Label();
			this.m_btn_ss_dirs = new System.Windows.Forms.Button();
			this.m_combo_ss_file_order = new System.Windows.Forms.ComboBox();
			this.m_lbl_choose_dirs = new System.Windows.Forms.Label();
			this.m_lbl_ss_folder_order = new System.Windows.Forms.Label();
			this.m_combo_ss_folder_order = new System.Windows.Forms.ComboBox();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_btn_cancel = new System.Windows.Forms.Button();
			this.m_btn_apply = new System.Windows.Forms.Button();
			this.m_check_cache_media_list = new System.Windows.Forms.CheckBox();
			this.m_tab.SuspendLayout();
			this.m_tab_page_general.SuspendLayout();
			this.m_tab_view.SuspendLayout();
			this.m_tab_media_list.SuspendLayout();
			this.tabPage1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_track_ss_volume)).BeginInit();
			this.SuspendLayout();
			// 
			// m_tab
			// 
			this.m_tab.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.m_tab.Controls.Add(this.m_tab_page_general);
			this.m_tab.Controls.Add(this.m_tab_view);
			this.m_tab.Controls.Add(this.m_tab_media_list);
			this.m_tab.Controls.Add(this.tabPage1);
			this.m_tab.Location = new System.Drawing.Point(0, 0);
			this.m_tab.Name = "m_tab";
			this.m_tab.SelectedIndex = 0;
			this.m_tab.Size = new System.Drawing.Size(259, 247);
			this.m_tab.TabIndex = 0;
			// 
			// m_tab_page_general
			// 
			this.m_tab_page_general.BackColor = System.Drawing.SystemColors.Window;
			this.m_tab_page_general.Controls.Add(this.m_lbl_reset_to_defaults);
			this.m_tab_page_general.Controls.Add(this.m_btn_reset);
			this.m_tab_page_general.Controls.Add(this.m_check_audio_files);
			this.m_tab_page_general.Controls.Add(this.m_check_video_files);
			this.m_tab_page_general.Controls.Add(this.m_check_image_files);
			this.m_tab_page_general.Controls.Add(this.m_chklist_audio_formats);
			this.m_tab_page_general.Controls.Add(this.m_chklist_video_formats);
			this.m_tab_page_general.Controls.Add(this.m_chklist_image_formats);
			this.m_tab_page_general.Controls.Add(this.m_check_startup_version_check);
			this.m_tab_page_general.Controls.Add(this.m_lbl_media_types);
			this.m_tab_page_general.Location = new System.Drawing.Point(4, 22);
			this.m_tab_page_general.Name = "m_tab_page_general";
			this.m_tab_page_general.Padding = new System.Windows.Forms.Padding(3);
			this.m_tab_page_general.Size = new System.Drawing.Size(251, 221);
			this.m_tab_page_general.TabIndex = 0;
			this.m_tab_page_general.Text = "General";
			// 
			// m_lbl_reset_to_defaults
			// 
			this.m_lbl_reset_to_defaults.AutoSize = true;
			this.m_lbl_reset_to_defaults.Location = new System.Drawing.Point(13, 182);
			this.m_lbl_reset_to_defaults.Name = "m_lbl_reset_to_defaults";
			this.m_lbl_reset_to_defaults.Size = new System.Drawing.Size(113, 26);
			this.m_lbl_reset_to_defaults.TabIndex = 22;
			this.m_lbl_reset_to_defaults.Text = "Reset these options to\r\ntheir default values";
			// 
			// m_btn_reset
			// 
			this.m_btn_reset.Location = new System.Drawing.Point(130, 185);
			this.m_btn_reset.Name = "m_btn_reset";
			this.m_btn_reset.Size = new System.Drawing.Size(111, 23);
			this.m_btn_reset.TabIndex = 21;
			this.m_btn_reset.Text = "Reset to Defaults";
			this.m_btn_reset.UseVisualStyleBackColor = true;
			// 
			// m_check_audio_files
			// 
			this.m_check_audio_files.AutoSize = true;
			this.m_check_audio_files.Location = new System.Drawing.Point(166, 28);
			this.m_check_audio_files.Name = "m_check_audio_files";
			this.m_check_audio_files.Size = new System.Drawing.Size(77, 17);
			this.m_check_audio_files.TabIndex = 20;
			this.m_check_audio_files.Text = "Audio Files";
			this.m_check_audio_files.UseVisualStyleBackColor = true;
			// 
			// m_check_video_files
			// 
			this.m_check_video_files.AutoSize = true;
			this.m_check_video_files.Location = new System.Drawing.Point(89, 28);
			this.m_check_video_files.Name = "m_check_video_files";
			this.m_check_video_files.Size = new System.Drawing.Size(77, 17);
			this.m_check_video_files.TabIndex = 19;
			this.m_check_video_files.Text = "Video Files";
			this.m_check_video_files.UseVisualStyleBackColor = true;
			// 
			// m_check_image_files
			// 
			this.m_check_image_files.AutoSize = true;
			this.m_check_image_files.Location = new System.Drawing.Point(9, 28);
			this.m_check_image_files.Name = "m_check_image_files";
			this.m_check_image_files.Size = new System.Drawing.Size(79, 17);
			this.m_check_image_files.TabIndex = 18;
			this.m_check_image_files.Text = "Image Files";
			this.m_check_image_files.UseVisualStyleBackColor = true;
			// 
			// m_chklist_audio_formats
			// 
			this.m_chklist_audio_formats.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_chklist_audio_formats.FormattingEnabled = true;
			this.m_chklist_audio_formats.Location = new System.Drawing.Point(182, 46);
			this.m_chklist_audio_formats.Name = "m_chklist_audio_formats";
			this.m_chklist_audio_formats.Size = new System.Drawing.Size(59, 92);
			this.m_chklist_audio_formats.TabIndex = 14;
			// 
			// m_chklist_video_formats
			// 
			this.m_chklist_video_formats.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_chklist_video_formats.FormattingEnabled = true;
			this.m_chklist_video_formats.Location = new System.Drawing.Point(103, 46);
			this.m_chklist_video_formats.Name = "m_chklist_video_formats";
			this.m_chklist_video_formats.Size = new System.Drawing.Size(59, 92);
			this.m_chklist_video_formats.TabIndex = 13;
			// 
			// m_chklist_image_formats
			// 
			this.m_chklist_image_formats.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_chklist_image_formats.FormattingEnabled = true;
			this.m_chklist_image_formats.Location = new System.Drawing.Point(22, 46);
			this.m_chklist_image_formats.Name = "m_chklist_image_formats";
			this.m_chklist_image_formats.Size = new System.Drawing.Size(59, 92);
			this.m_chklist_image_formats.TabIndex = 12;
			// 
			// m_check_startup_version_check
			// 
			this.m_check_startup_version_check.AutoSize = true;
			this.m_check_startup_version_check.Location = new System.Drawing.Point(9, 161);
			this.m_check_startup_version_check.Name = "m_check_startup_version_check";
			this.m_check_startup_version_check.Size = new System.Drawing.Size(196, 17);
			this.m_check_startup_version_check.TabIndex = 5;
			this.m_check_startup_version_check.Text = "Check for newer versions on startup";
			this.m_check_startup_version_check.UseVisualStyleBackColor = true;
			// 
			// m_lbl_media_types
			// 
			this.m_lbl_media_types.AutoSize = true;
			this.m_lbl_media_types.Location = new System.Drawing.Point(6, 9);
			this.m_lbl_media_types.Name = "m_lbl_media_types";
			this.m_lbl_media_types.Size = new System.Drawing.Size(95, 13);
			this.m_lbl_media_types.TabIndex = 1;
			this.m_lbl_media_types.Text = "Media files to load:";
			// 
			// m_tab_view
			// 
			this.m_tab_view.BackColor = System.Drawing.SystemColors.Window;
			this.m_tab_view.Controls.Add(this.m_lbl_primary_display);
			this.m_tab_view.Controls.Add(this.m_combo_primary_display);
			this.m_tab_view.Controls.Add(this.m_check_always_on_top);
			this.m_tab_view.Controls.Add(this.m_check_show_filenames);
			this.m_tab_view.Controls.Add(this.m_edit_slide_show_rate);
			this.m_tab_view.Controls.Add(this.m_lbl_slide_show_rate);
			this.m_tab_view.Controls.Add(this.m_lbl_zoom_type);
			this.m_tab_view.Controls.Add(this.m_combo_zoom_type);
			this.m_tab_view.Controls.Add(this.m_check_reset_zoom_on_load);
			this.m_tab_view.Location = new System.Drawing.Point(4, 22);
			this.m_tab_view.Name = "m_tab_view";
			this.m_tab_view.Size = new System.Drawing.Size(251, 221);
			this.m_tab_view.TabIndex = 3;
			this.m_tab_view.Text = "View";
			// 
			// m_lbl_primary_display
			// 
			this.m_lbl_primary_display.AutoSize = true;
			this.m_lbl_primary_display.Location = new System.Drawing.Point(11, 14);
			this.m_lbl_primary_display.Name = "m_lbl_primary_display";
			this.m_lbl_primary_display.Size = new System.Drawing.Size(78, 13);
			this.m_lbl_primary_display.TabIndex = 8;
			this.m_lbl_primary_display.Text = "Primary Display";
			// 
			// m_combo_primary_display
			// 
			this.m_combo_primary_display.FormattingEnabled = true;
			this.m_combo_primary_display.Location = new System.Drawing.Point(95, 11);
			this.m_combo_primary_display.Name = "m_combo_primary_display";
			this.m_combo_primary_display.Size = new System.Drawing.Size(153, 21);
			this.m_combo_primary_display.TabIndex = 7;
			// 
			// m_check_always_on_top
			// 
			this.m_check_always_on_top.AutoSize = true;
			this.m_check_always_on_top.Location = new System.Drawing.Point(11, 120);
			this.m_check_always_on_top.Name = "m_check_always_on_top";
			this.m_check_always_on_top.Size = new System.Drawing.Size(205, 17);
			this.m_check_always_on_top.TabIndex = 6;
			this.m_check_always_on_top.Text = "Show Imager above all other windows";
			this.m_check_always_on_top.UseVisualStyleBackColor = true;
			// 
			// m_check_show_filenames
			// 
			this.m_check_show_filenames.AutoSize = true;
			this.m_check_show_filenames.Location = new System.Drawing.Point(11, 98);
			this.m_check_show_filenames.Name = "m_check_show_filenames";
			this.m_check_show_filenames.Size = new System.Drawing.Size(234, 17);
			this.m_check_show_filenames.TabIndex = 5;
			this.m_check_show_filenames.Text = "Display the file path for the current media file";
			this.m_check_show_filenames.UseVisualStyleBackColor = true;
			// 
			// m_edit_slide_show_rate
			// 
			this.m_edit_slide_show_rate.Location = new System.Drawing.Point(159, 147);
			this.m_edit_slide_show_rate.Name = "m_edit_slide_show_rate";
			this.m_edit_slide_show_rate.Size = new System.Drawing.Size(41, 20);
			this.m_edit_slide_show_rate.TabIndex = 4;
			// 
			// m_lbl_slide_show_rate
			// 
			this.m_lbl_slide_show_rate.AutoSize = true;
			this.m_lbl_slide_show_rate.Location = new System.Drawing.Point(8, 150);
			this.m_lbl_slide_show_rate.Name = "m_lbl_slide_show_rate";
			this.m_lbl_slide_show_rate.Size = new System.Drawing.Size(240, 13);
			this.m_lbl_slide_show_rate.TabIndex = 3;
			this.m_lbl_slide_show_rate.Text = "Slide show changes files every                seconds";
			// 
			// m_lbl_zoom_type
			// 
			this.m_lbl_zoom_type.AutoSize = true;
			this.m_lbl_zoom_type.Location = new System.Drawing.Point(4, 41);
			this.m_lbl_zoom_type.Name = "m_lbl_zoom_type";
			this.m_lbl_zoom_type.Size = new System.Drawing.Size(85, 13);
			this.m_lbl_zoom_type.TabIndex = 2;
			this.m_lbl_zoom_type.Text = "Zoom Behaviour";
			// 
			// m_combo_zoom_type
			// 
			this.m_combo_zoom_type.FormattingEnabled = true;
			this.m_combo_zoom_type.Location = new System.Drawing.Point(95, 38);
			this.m_combo_zoom_type.Name = "m_combo_zoom_type";
			this.m_combo_zoom_type.Size = new System.Drawing.Size(153, 21);
			this.m_combo_zoom_type.TabIndex = 1;
			// 
			// m_check_reset_zoom_on_load
			// 
			this.m_check_reset_zoom_on_load.AutoSize = true;
			this.m_check_reset_zoom_on_load.Location = new System.Drawing.Point(11, 75);
			this.m_check_reset_zoom_on_load.Name = "m_check_reset_zoom_on_load";
			this.m_check_reset_zoom_on_load.Size = new System.Drawing.Size(219, 17);
			this.m_check_reset_zoom_on_load.TabIndex = 0;
			this.m_check_reset_zoom_on_load.Text = "Reset the zoom each time a file is loaded";
			this.m_check_reset_zoom_on_load.UseVisualStyleBackColor = true;
			// 
			// m_tab_media_list
			// 
			this.m_tab_media_list.BackColor = System.Drawing.SystemColors.Window;
			this.m_tab_media_list.Controls.Add(this.m_lbl_file_sort_order);
			this.m_tab_media_list.Controls.Add(this.m_combo_file_order);
			this.m_tab_media_list.Controls.Add(this.m_lbl_folder_order);
			this.m_tab_media_list.Controls.Add(this.m_combo_folder_order);
			this.m_tab_media_list.Controls.Add(this.m_check_allow_duplicates);
			this.m_tab_media_list.Location = new System.Drawing.Point(4, 22);
			this.m_tab_media_list.Name = "m_tab_media_list";
			this.m_tab_media_list.Padding = new System.Windows.Forms.Padding(3);
			this.m_tab_media_list.Size = new System.Drawing.Size(251, 221);
			this.m_tab_media_list.TabIndex = 2;
			this.m_tab_media_list.Text = "Media List";
			// 
			// m_lbl_file_sort_order
			// 
			this.m_lbl_file_sort_order.AutoSize = true;
			this.m_lbl_file_sort_order.Location = new System.Drawing.Point(8, 41);
			this.m_lbl_file_sort_order.Name = "m_lbl_file_sort_order";
			this.m_lbl_file_sort_order.Size = new System.Drawing.Size(74, 13);
			this.m_lbl_file_sort_order.TabIndex = 5;
			this.m_lbl_file_sort_order.Text = "File Sort Order";
			// 
			// m_combo_file_order
			// 
			this.m_combo_file_order.FormattingEnabled = true;
			this.m_combo_file_order.Location = new System.Drawing.Point(101, 38);
			this.m_combo_file_order.Name = "m_combo_file_order";
			this.m_combo_file_order.Size = new System.Drawing.Size(147, 21);
			this.m_combo_file_order.TabIndex = 4;
			// 
			// m_lbl_folder_order
			// 
			this.m_lbl_folder_order.AutoSize = true;
			this.m_lbl_folder_order.Location = new System.Drawing.Point(8, 14);
			this.m_lbl_folder_order.Name = "m_lbl_folder_order";
			this.m_lbl_folder_order.Size = new System.Drawing.Size(87, 13);
			this.m_lbl_folder_order.TabIndex = 3;
			this.m_lbl_folder_order.Text = "Folder Sort Order";
			// 
			// m_combo_folder_order
			// 
			this.m_combo_folder_order.FormattingEnabled = true;
			this.m_combo_folder_order.Location = new System.Drawing.Point(101, 11);
			this.m_combo_folder_order.Name = "m_combo_folder_order";
			this.m_combo_folder_order.Size = new System.Drawing.Size(147, 21);
			this.m_combo_folder_order.TabIndex = 2;
			// 
			// m_check_allow_duplicates
			// 
			this.m_check_allow_duplicates.AutoSize = true;
			this.m_check_allow_duplicates.Location = new System.Drawing.Point(8, 68);
			this.m_check_allow_duplicates.Name = "m_check_allow_duplicates";
			this.m_check_allow_duplicates.Size = new System.Drawing.Size(193, 17);
			this.m_check_allow_duplicates.TabIndex = 0;
			this.m_check_allow_duplicates.Text = "Allow duplicate files in the media list";
			this.m_check_allow_duplicates.UseVisualStyleBackColor = true;
			// 
			// tabPage1
			// 
			this.tabPage1.Controls.Add(this.m_check_cache_media_list);
			this.tabPage1.Controls.Add(this.m_lbl_volume);
			this.tabPage1.Controls.Add(this.m_track_ss_volume);
			this.tabPage1.Controls.Add(this.m_lbl_ss_file_order);
			this.tabPage1.Controls.Add(this.m_btn_ss_dirs);
			this.tabPage1.Controls.Add(this.m_combo_ss_file_order);
			this.tabPage1.Controls.Add(this.m_lbl_choose_dirs);
			this.tabPage1.Controls.Add(this.m_lbl_ss_folder_order);
			this.tabPage1.Controls.Add(this.m_combo_ss_folder_order);
			this.tabPage1.Location = new System.Drawing.Point(4, 22);
			this.tabPage1.Name = "tabPage1";
			this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage1.Size = new System.Drawing.Size(251, 221);
			this.tabPage1.TabIndex = 4;
			this.tabPage1.Text = "Screen Saver";
			this.tabPage1.UseVisualStyleBackColor = true;
			// 
			// m_lbl_volume
			// 
			this.m_lbl_volume.AutoSize = true;
			this.m_lbl_volume.Location = new System.Drawing.Point(13, 161);
			this.m_lbl_volume.Name = "m_lbl_volume";
			this.m_lbl_volume.Size = new System.Drawing.Size(42, 13);
			this.m_lbl_volume.TabIndex = 10;
			this.m_lbl_volume.Text = "Volume";
			// 
			// m_track_ss_volume
			// 
			this.m_track_ss_volume.AutoSize = false;
			this.m_track_ss_volume.BackColor = System.Drawing.SystemColors.Window;
			this.m_track_ss_volume.LargeChange = 10;
			this.m_track_ss_volume.Location = new System.Drawing.Point(56, 153);
			this.m_track_ss_volume.Maximum = 100;
			this.m_track_ss_volume.Name = "m_track_ss_volume";
			this.m_track_ss_volume.Size = new System.Drawing.Size(189, 37);
			this.m_track_ss_volume.TabIndex = 11;
			this.m_track_ss_volume.TickFrequency = 5;
			// 
			// m_lbl_ss_file_order
			// 
			this.m_lbl_ss_file_order.AutoSize = true;
			this.m_lbl_ss_file_order.Location = new System.Drawing.Point(13, 131);
			this.m_lbl_ss_file_order.Name = "m_lbl_ss_file_order";
			this.m_lbl_ss_file_order.Size = new System.Drawing.Size(74, 13);
			this.m_lbl_ss_file_order.TabIndex = 15;
			this.m_lbl_ss_file_order.Text = "File Sort Order";
			// 
			// m_btn_ss_dirs
			// 
			this.m_btn_ss_dirs.Location = new System.Drawing.Point(124, 22);
			this.m_btn_ss_dirs.Name = "m_btn_ss_dirs";
			this.m_btn_ss_dirs.Size = new System.Drawing.Size(110, 23);
			this.m_btn_ss_dirs.TabIndex = 16;
			this.m_btn_ss_dirs.Text = "Choose Directories";
			this.m_btn_ss_dirs.UseVisualStyleBackColor = true;
			// 
			// m_combo_ss_file_order
			// 
			this.m_combo_ss_file_order.FormattingEnabled = true;
			this.m_combo_ss_file_order.Location = new System.Drawing.Point(106, 128);
			this.m_combo_ss_file_order.Name = "m_combo_ss_file_order";
			this.m_combo_ss_file_order.Size = new System.Drawing.Size(139, 21);
			this.m_combo_ss_file_order.TabIndex = 14;
			// 
			// m_lbl_choose_dirs
			// 
			this.m_lbl_choose_dirs.AutoSize = true;
			this.m_lbl_choose_dirs.Location = new System.Drawing.Point(7, 10);
			this.m_lbl_choose_dirs.Name = "m_lbl_choose_dirs";
			this.m_lbl_choose_dirs.Size = new System.Drawing.Size(117, 52);
			this.m_lbl_choose_dirs.TabIndex = 16;
			this.m_lbl_choose_dirs.Text = "Choose directories from\r\nwhich to display media\r\nfiles when running as\r\na screen " +
				"saver.";
			// 
			// m_lbl_ss_folder_order
			// 
			this.m_lbl_ss_folder_order.AutoSize = true;
			this.m_lbl_ss_folder_order.Location = new System.Drawing.Point(13, 104);
			this.m_lbl_ss_folder_order.Name = "m_lbl_ss_folder_order";
			this.m_lbl_ss_folder_order.Size = new System.Drawing.Size(87, 13);
			this.m_lbl_ss_folder_order.TabIndex = 13;
			this.m_lbl_ss_folder_order.Text = "Folder Sort Order";
			// 
			// m_combo_ss_folder_order
			// 
			this.m_combo_ss_folder_order.FormattingEnabled = true;
			this.m_combo_ss_folder_order.Location = new System.Drawing.Point(106, 101);
			this.m_combo_ss_folder_order.Name = "m_combo_ss_folder_order";
			this.m_combo_ss_folder_order.Size = new System.Drawing.Size(139, 21);
			this.m_combo_ss_folder_order.TabIndex = 12;
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(12, 254);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 1;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_btn_cancel
			// 
			this.m_btn_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_cancel.Location = new System.Drawing.Point(93, 254);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_btn_cancel.TabIndex = 2;
			this.m_btn_cancel.Text = "Cancel";
			this.m_btn_cancel.UseVisualStyleBackColor = true;
			// 
			// m_btn_apply
			// 
			this.m_btn_apply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_apply.Location = new System.Drawing.Point(174, 254);
			this.m_btn_apply.Name = "m_btn_apply";
			this.m_btn_apply.Size = new System.Drawing.Size(75, 23);
			this.m_btn_apply.TabIndex = 3;
			this.m_btn_apply.Text = "Apply";
			this.m_btn_apply.UseVisualStyleBackColor = true;
			// 
			// m_check_cache_media_list
			// 
			this.m_check_cache_media_list.AutoSize = true;
			this.m_check_cache_media_list.Location = new System.Drawing.Point(16, 72);
			this.m_check_cache_media_list.Name = "m_check_cache_media_list";
			this.m_check_cache_media_list.Size = new System.Drawing.Size(137, 17);
			this.m_check_cache_media_list.TabIndex = 17;
			this.m_check_cache_media_list.Text = "Cache the media file list";
			this.m_check_cache_media_list.UseVisualStyleBackColor = true;
			// 
			// Config
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_cancel;
			this.ClientSize = new System.Drawing.Size(261, 289);
			this.Controls.Add(this.m_btn_apply);
			this.Controls.Add(this.m_btn_cancel);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_tab);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "Config";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Imager Options";
			this.m_tab.ResumeLayout(false);
			this.m_tab_page_general.ResumeLayout(false);
			this.m_tab_page_general.PerformLayout();
			this.m_tab_view.ResumeLayout(false);
			this.m_tab_view.PerformLayout();
			this.m_tab_media_list.ResumeLayout(false);
			this.m_tab_media_list.PerformLayout();
			this.tabPage1.ResumeLayout(false);
			this.tabPage1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_track_ss_volume)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TabControl m_tab;
		private System.Windows.Forms.TabPage m_tab_page_general;
		private System.Windows.Forms.Label m_lbl_media_types;
		private System.Windows.Forms.Button m_btn_ok;
		private System.Windows.Forms.Button m_btn_cancel;
		private System.Windows.Forms.Button m_btn_apply;
		private System.Windows.Forms.CheckBox m_check_startup_version_check;
		private System.Windows.Forms.TabPage m_tab_media_list;
		private System.Windows.Forms.CheckBox m_check_allow_duplicates;
		private System.Windows.Forms.TabPage m_tab_view;
		private System.Windows.Forms.CheckBox m_check_reset_zoom_on_load;
		private System.Windows.Forms.Label m_lbl_zoom_type;
		private System.Windows.Forms.ComboBox m_combo_zoom_type;
		private System.Windows.Forms.TextBox m_edit_slide_show_rate;
		private System.Windows.Forms.Label m_lbl_slide_show_rate;
		private System.Windows.Forms.ComboBox m_combo_folder_order;
		private System.Windows.Forms.Label m_lbl_folder_order;
		private System.Windows.Forms.Label m_lbl_file_sort_order;
		private System.Windows.Forms.ComboBox m_combo_file_order;
		private System.Windows.Forms.CheckBox m_check_show_filenames;
		private System.Windows.Forms.Label m_lbl_primary_display;
		private System.Windows.Forms.ComboBox m_combo_primary_display;
		private System.Windows.Forms.CheckBox m_check_always_on_top;
		private System.Windows.Forms.CheckedListBox m_chklist_audio_formats;
		private System.Windows.Forms.CheckedListBox m_chklist_video_formats;
		private System.Windows.Forms.CheckedListBox m_chklist_image_formats;
		private System.Windows.Forms.CheckBox m_check_audio_files;
		private System.Windows.Forms.CheckBox m_check_video_files;
		private System.Windows.Forms.CheckBox m_check_image_files;
		private System.Windows.Forms.Label m_lbl_reset_to_defaults;
		private System.Windows.Forms.Button m_btn_reset;
		private System.Windows.Forms.TabPage tabPage1;
		private System.Windows.Forms.Button m_btn_ss_dirs;
		private System.Windows.Forms.Label m_lbl_choose_dirs;
		private System.Windows.Forms.Label m_lbl_ss_file_order;
		private System.Windows.Forms.ComboBox m_combo_ss_file_order;
		private System.Windows.Forms.Label m_lbl_ss_folder_order;
		private System.Windows.Forms.ComboBox m_combo_ss_folder_order;
		private System.Windows.Forms.TrackBar m_track_ss_volume;
		private System.Windows.Forms.Label m_lbl_volume;
		private System.Windows.Forms.CheckBox m_check_cache_media_list;
	}
}