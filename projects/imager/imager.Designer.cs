namespace imager
{
	sealed partial class Imager
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Imager));
			this.m_status = new System.Windows.Forms.StatusStrip();
			this.m_msg = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_status_spacer = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_lbl_image_info = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_lbl_zoom_type = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_lbl_building_media_list = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_menu = new System.Windows.Forms.MenuStrip();
			this.m_menu_file = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_open_file = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_directories = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_options = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_recent = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_exit = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_window = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_help = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_media_list = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_slide_show = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_full_screen = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_check_for_updates = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_about = new System.Windows.Forms.ToolStripMenuItem();
			this.m_lbl_msg = new System.Windows.Forms.Label();
			this.m_image_list = new System.Windows.Forms.ImageList(this.components);
			this.m_status.SuspendLayout();
			this.m_menu.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_status
			// 
			this.m_status.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_msg,
            this.m_status_spacer,
            this.m_lbl_image_info,
            this.m_lbl_zoom_type,
            this.m_lbl_building_media_list});
			this.m_status.Location = new System.Drawing.Point(0, 671);
			this.m_status.Name = "m_status";
			this.m_status.Size = new System.Drawing.Size(683, 24);
			this.m_status.TabIndex = 1;
			this.m_status.Text = "statusStrip1";
			// 
			// m_msg
			// 
			this.m_msg.Name = "m_msg";
			this.m_msg.Size = new System.Drawing.Size(26, 19);
			this.m_msg.Text = "Idle";
			// 
			// m_status_spacer
			// 
			this.m_status_spacer.Name = "m_status_spacer";
			this.m_status_spacer.Size = new System.Drawing.Size(362, 19);
			this.m_status_spacer.Spring = true;
			// 
			// m_lbl_image_info
			// 
			this.m_lbl_image_info.Name = "m_lbl_image_info";
			this.m_lbl_image_info.Size = new System.Drawing.Size(64, 19);
			this.m_lbl_image_info.Text = "Image Info";
			// 
			// m_lbl_zoom_type
			// 
			this.m_lbl_zoom_type.Name = "m_lbl_zoom_type";
			this.m_lbl_zoom_type.Size = new System.Drawing.Size(64, 19);
			this.m_lbl_zoom_type.Text = "Actual Size";
			// 
			// m_lbl_building_media_list
			// 
			this.m_lbl_building_media_list.BackColor = System.Drawing.Color.LightSteelBlue;
			this.m_lbl_building_media_list.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top)
						| System.Windows.Forms.ToolStripStatusLabelBorderSides.Right)
						| System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
			this.m_lbl_building_media_list.BorderStyle = System.Windows.Forms.Border3DStyle.Sunken;
			this.m_lbl_building_media_list.Name = "m_lbl_building_media_list";
			this.m_lbl_building_media_list.Size = new System.Drawing.Size(121, 19);
			this.m_lbl_building_media_list.Text = "Building Media List...";
			this.m_lbl_building_media_list.Visible = false;
			// 
			// m_menu
			// 
			this.m_menu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_file,
            this.m_menu_window});
			this.m_menu.Location = new System.Drawing.Point(0, 0);
			this.m_menu.Name = "m_menu";
			this.m_menu.Size = new System.Drawing.Size(683, 24);
			this.m_menu.TabIndex = 2;
			this.m_menu.Text = "m_menu";
			// 
			// m_menu_file
			// 
			this.m_menu_file.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_open_file,
            this.m_menu_directories,
            this.toolStripSeparator4,
            this.m_menu_options,
            this.toolStripSeparator2,
            this.m_menu_recent,
            this.toolStripSeparator1,
            this.m_menu_exit});
			this.m_menu_file.Name = "m_menu_file";
			this.m_menu_file.Size = new System.Drawing.Size(37, 20);
			this.m_menu_file.Text = "&File";
			// 
			// m_menu_open_file
			// 
			this.m_menu_open_file.Name = "m_menu_open_file";
			this.m_menu_open_file.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.m_menu_open_file.Size = new System.Drawing.Size(181, 22);
			this.m_menu_open_file.Text = "&Open File...";
			// 
			// m_menu_directories
			// 
			this.m_menu_directories.Name = "m_menu_directories";
			this.m_menu_directories.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D)));
			this.m_menu_directories.Size = new System.Drawing.Size(181, 22);
			this.m_menu_directories.Text = "&Directories...";
			// 
			// toolStripSeparator4
			// 
			this.toolStripSeparator4.Name = "toolStripSeparator4";
			this.toolStripSeparator4.Size = new System.Drawing.Size(178, 6);
			// 
			// m_menu_options
			// 
			this.m_menu_options.Name = "m_menu_options";
			this.m_menu_options.Size = new System.Drawing.Size(181, 22);
			this.m_menu_options.Text = "&Options...";
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(178, 6);
			// 
			// m_menu_recent
			// 
			this.m_menu_recent.Name = "m_menu_recent";
			this.m_menu_recent.Size = new System.Drawing.Size(181, 22);
			this.m_menu_recent.Text = "&Recent";
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(178, 6);
			// 
			// m_menu_exit
			// 
			this.m_menu_exit.Name = "m_menu_exit";
			this.m_menu_exit.Size = new System.Drawing.Size(181, 22);
			this.m_menu_exit.Text = "E&xit";
			// 
			// m_menu_window
			// 
			this.m_menu_window.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_help,
            this.toolStripSeparator6,
            this.m_menu_media_list,
            this.m_menu_slide_show,
            this.m_menu_full_screen,
            this.toolStripSeparator5,
            this.m_menu_check_for_updates,
            this.toolStripSeparator3,
            this.m_menu_about});
			this.m_menu_window.Name = "m_menu_window";
			this.m_menu_window.Size = new System.Drawing.Size(63, 20);
			this.m_menu_window.Text = "&Window";
			// 
			// m_menu_help
			// 
			this.m_menu_help.Name = "m_menu_help";
			this.m_menu_help.ShortcutKeys = System.Windows.Forms.Keys.F1;
			this.m_menu_help.Size = new System.Drawing.Size(171, 22);
			this.m_menu_help.Text = "&Help";
			// 
			// toolStripSeparator6
			// 
			this.toolStripSeparator6.Name = "toolStripSeparator6";
			this.toolStripSeparator6.Size = new System.Drawing.Size(168, 6);
			// 
			// m_menu_media_list
			// 
			this.m_menu_media_list.Name = "m_menu_media_list";
			this.m_menu_media_list.ShortcutKeys = System.Windows.Forms.Keys.F8;
			this.m_menu_media_list.Size = new System.Drawing.Size(171, 22);
			this.m_menu_media_list.Text = "Media &List...";
			// 
			// m_menu_slide_show
			// 
			this.m_menu_slide_show.Name = "m_menu_slide_show";
			this.m_menu_slide_show.ShortcutKeys = System.Windows.Forms.Keys.F11;
			this.m_menu_slide_show.Size = new System.Drawing.Size(171, 22);
			this.m_menu_slide_show.Text = "&Slide Show";
			// 
			// m_menu_full_screen
			// 
			this.m_menu_full_screen.Name = "m_menu_full_screen";
			this.m_menu_full_screen.ShortcutKeys = System.Windows.Forms.Keys.F12;
			this.m_menu_full_screen.Size = new System.Drawing.Size(171, 22);
			this.m_menu_full_screen.Text = "&Full Screen";
			// 
			// toolStripSeparator5
			// 
			this.toolStripSeparator5.Name = "toolStripSeparator5";
			this.toolStripSeparator5.Size = new System.Drawing.Size(168, 6);
			// 
			// m_menu_check_for_updates
			// 
			this.m_menu_check_for_updates.Name = "m_menu_check_for_updates";
			this.m_menu_check_for_updates.Size = new System.Drawing.Size(171, 22);
			this.m_menu_check_for_updates.Text = "&Check for Updates";
			// 
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(168, 6);
			// 
			// m_menu_about
			// 
			this.m_menu_about.Name = "m_menu_about";
			this.m_menu_about.Size = new System.Drawing.Size(171, 22);
			this.m_menu_about.Text = "&About";
			// 
			// m_lbl_msg
			// 
			this.m_lbl_msg.AutoSize = true;
			this.m_lbl_msg.Location = new System.Drawing.Point(3, 28);
			this.m_lbl_msg.Name = "m_lbl_msg";
			this.m_lbl_msg.Size = new System.Drawing.Size(79, 13);
			this.m_lbl_msg.TabIndex = 3;
			this.m_lbl_msg.Text = "Message Label";
			this.m_lbl_msg.Visible = false;
			// 
			// m_image_list
			// 
			this.m_image_list.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_image_list.ImageStream")));
			this.m_image_list.TransparentColor = System.Drawing.Color.Magenta;
			this.m_image_list.Images.SetKeyName(0, "Left");
			this.m_image_list.Images.SetKeyName(1, "Right");
			this.m_image_list.Images.SetKeyName(2, "FullScreen");
			this.m_image_list.Images.SetKeyName(3, "Fit");
			this.m_image_list.Images.SetKeyName(4, "Play");
			this.m_image_list.Images.SetKeyName(5, "Stop");
			// 
			// Imager
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(683, 695);
			this.Controls.Add(this.m_lbl_msg);
			this.Controls.Add(this.m_status);
			this.Controls.Add(this.m_menu);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.m_menu;
			this.Name = "Imager";
			this.Text = "Imager";
			this.m_status.ResumeLayout(false);
			this.m_status.PerformLayout();
			this.m_menu.ResumeLayout(false);
			this.m_menu.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.StatusStrip m_status;
		private System.Windows.Forms.MenuStrip m_menu;
		private System.Windows.Forms.ToolStripMenuItem m_menu_file;
		private System.Windows.Forms.ToolStripMenuItem m_menu_exit;
		private System.Windows.Forms.ToolStripStatusLabel m_msg;
		private System.Windows.Forms.ToolStripStatusLabel m_status_spacer;
		private System.Windows.Forms.ToolStripMenuItem m_menu_options;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripMenuItem m_menu_recent;
		private System.Windows.Forms.ToolStripMenuItem m_menu_window;
		private System.Windows.Forms.ToolStripMenuItem m_menu_media_list;
		private System.Windows.Forms.ToolStripMenuItem m_menu_open_file;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.ToolStripMenuItem m_menu_about;
		private System.Windows.Forms.Label m_lbl_msg;
		private System.Windows.Forms.ToolStripMenuItem m_menu_full_screen;
		private System.Windows.Forms.ToolStripMenuItem m_menu_slide_show;
		private System.Windows.Forms.ToolStripStatusLabel m_lbl_building_media_list;
		private System.Windows.Forms.ToolStripMenuItem m_menu_directories;
		private System.Windows.Forms.ImageList m_image_list;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
		private System.Windows.Forms.ToolStripMenuItem m_menu_check_for_updates;
		private System.Windows.Forms.ToolStripMenuItem m_menu_help;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
		private System.Windows.Forms.ToolStripStatusLabel m_lbl_zoom_type;
		private System.Windows.Forms.ToolStripStatusLabel m_lbl_image_info;
	}
}

