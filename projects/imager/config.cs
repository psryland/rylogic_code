using System;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using pr.attrib;
using pr.maths;

namespace imager
{
	public partial class Config :Form
	{
		private readonly Settings m_settings;
		private readonly DirectoryManager m_ss_dir_manager;
		private readonly ToolTip m_tt;

		public Config(Settings settings)
		{
			InitializeComponent();
			m_settings = settings;
			m_ss_dir_manager = new DirectoryManager(m_settings, true);
			m_ss_dir_manager.ListChanged += (s,e)=>{ m_btn_apply.Enabled |= (e.ListChangedType != ListChangedType.Reset); };
			m_tt = new ToolTip{InitialDelay = 100, ReshowDelay = 100, AutoPopDelay = 10000};
			
			m_btn_reset.Click   += (s,e)=>{ if (MessageBox.Show("Reset all settings to their default values?", "Reset Settings", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK) {m_settings.Reset(); SetValues();} };
			m_btn_apply.Click   += (s,e)=>{ Apply(); };
			m_btn_ss_dirs.Click += (s,e)=>{ m_ss_dir_manager.Show(this); };
			m_check_image_files           .CheckedChanged       += (s,e)=>{ m_btn_apply.Enabled = true; };
			m_check_video_files           .CheckedChanged       += (s,e)=>{ m_btn_apply.Enabled = true; };
			m_check_audio_files           .CheckedChanged       += (s,e)=>{ m_btn_apply.Enabled = true; };
			m_chklist_image_formats       .ItemCheck            += (s,e)=>{ m_btn_apply.Enabled = true; };
			m_chklist_video_formats       .ItemCheck            += (s,e)=>{ m_btn_apply.Enabled = true; };
			m_chklist_audio_formats       .ItemCheck            += (s,e)=>{ m_btn_apply.Enabled = true; };
			m_combo_primary_display       .SelectedIndexChanged += (s,e)=>{ m_btn_apply.Enabled = true; };
			m_check_always_on_top         .CheckedChanged       += (s,e)=>{ m_btn_apply.Enabled = true; };
			m_check_startup_version_check .CheckedChanged       += (s,e)=>{ m_btn_apply.Enabled = true; };
			m_check_allow_duplicates      .CheckedChanged       += (s,e)=>{ m_btn_apply.Enabled = true; };
			m_check_reset_zoom_on_load    .CheckedChanged       += (s,e)=>{ m_btn_apply.Enabled = true; };
			m_combo_zoom_type             .SelectedIndexChanged += (s,e)=>{ m_btn_apply.Enabled = true; };
			m_edit_slide_show_rate        .TextChanged          += (s,e)=>{ m_btn_apply.Enabled = true; };
			m_combo_folder_order          .SelectedIndexChanged += (s,e)=>{ m_btn_apply.Enabled = true; };
			m_combo_ss_folder_order       .SelectedIndexChanged += (s,e)=>{ m_btn_apply.Enabled = true; };
			m_combo_file_order            .SelectedIndexChanged += (s,e)=>{ m_btn_apply.Enabled = true; };
			m_combo_ss_file_order         .SelectedIndexChanged += (s,e)=>{ m_btn_apply.Enabled = true; };
			m_check_show_filenames        .CheckedChanged       += (s,e)=>{ m_btn_apply.Enabled = true; };
			m_track_ss_volume             .ValueChanged         += (s,e)=>{ m_btn_apply.Enabled = true; };
			m_chklist_image_formats       .MouseDown            += OnMouseDown;
			m_chklist_video_formats       .MouseDown            += OnMouseDown;
			m_chklist_audio_formats       .MouseDown            += OnMouseDown;
			SetValues();
			
			FormClosing += (s,e)=>{ if (DialogResult == DialogResult.OK) Apply(); };
		}
		
		/// <summary>Set the values in the dialog from the settings</summary>
		private void SetValues()
		{
			// Image files
			m_check_image_files.Checked = Bit.AllSet((int)m_settings.MediaType, (int)EMediaType.Image);
			m_tt.SetToolTip(m_check_image_files, "Include image file formats when searching for media files");
			
			// Video files
			m_check_video_files.Checked = Bit.AllSet((int)m_settings.MediaType, (int)EMediaType.Video);
			m_tt.SetToolTip(m_check_image_files, "Include video file formats when searching for media files");
			
			// Audio files
			m_check_audio_files.Checked = Bit.AllSet((int)m_settings.MediaType, (int)EMediaType.Audio);
			m_tt.SetToolTip(m_check_image_files, "Include audio file formats when searching for media files");
			
			// Image extns
			ExtnStringToList(m_settings.ImageExtensions, m_chklist_image_formats);
			m_tt.SetToolTip(m_chklist_image_formats, "Image file extensions to include when searching for media files");
			
			// Video extns
			ExtnStringToList(m_settings.VideoExtensions, m_chklist_video_formats);
			m_tt.SetToolTip(m_chklist_video_formats, m_settings.DescAttr("VideoExtensions"));
			
			// Audio extns
			ExtnStringToList(m_settings.AudioExtensions, m_chklist_audio_formats);
			m_tt.SetToolTip(m_chklist_audio_formats, m_settings.DescAttr("AudioExtensions"));
			
			// Primary display
			m_combo_primary_display.Items.Clear();
			m_combo_primary_display.Items.Add("Not Set");
			foreach (Screen s in Screen.AllScreens) m_combo_primary_display.Items.Add(s.DeviceName.TrimStart(new[]{'\\','.'}));
			m_combo_primary_display.SelectedIndex = m_settings.PrimaryDisplay;
			m_tt.SetToolTip(m_combo_primary_display, m_settings.DescAttr("PrimaryDisplay"));
			
			// Always on top
			m_check_always_on_top.Checked = m_settings.AlwaysOnTop;
			m_tt.SetToolTip(m_check_always_on_top, m_settings.DescAttr("AlwaysOnTop"));
			
			// Startup version check
			m_check_startup_version_check.Checked = m_settings.StartupVersionCheck;
			m_tt.SetToolTip(m_check_startup_version_check, m_settings.DescAttr("StartupVersionCheck"));
			
			// Allow duplicates
			m_check_allow_duplicates.Checked = m_settings.AllowDuplicates;
			m_tt.SetToolTip(m_check_allow_duplicates, m_settings.DescAttr("AllowDuplicates"));
			
			// Cache media list
			m_check_cache_media_list.Checked = m_settings.CacheSSMediaList;
			m_tt.SetToolTip(m_check_cache_media_list, m_settings.DescAttr("CacheSSMediaList"));
			
			// Reset zoom on load
			m_check_reset_zoom_on_load.Checked = m_settings.ResetZoomOnLoad;
			m_tt.SetToolTip(m_check_reset_zoom_on_load, m_settings.DescAttr("ResetZoomOnLoad"));
			
			// Zoom type
			m_combo_zoom_type.Items.Clear();
			foreach (EZoomType e in Enum.GetValues(typeof(EZoomType))) m_combo_zoom_type.Items.Add(e.StrAttr());
			m_combo_zoom_type.SelectedIndex = (int)m_settings.ZoomType;
			m_tt.SetToolTip(m_combo_zoom_type, m_settings.DescAttr("ZoomType"));
			
			// Slide show rate
			m_edit_slide_show_rate.Text = m_settings.SlideShowRate.ToString();
			m_tt.SetToolTip(m_edit_slide_show_rate, m_settings.DescAttr("SlideShowRate"));
			
			// Folder sort order
			m_combo_folder_order.Items.Clear();
			foreach (ESortOrder e in Enum.GetValues(typeof(ESortOrder))) m_combo_folder_order.Items.Add(e.StrAttr());
			m_combo_folder_order.SelectedIndex = (int)m_settings.FolderOrder;
			m_tt.SetToolTip(m_combo_folder_order, m_settings.DescAttr("FolderOrder"));
			
			// SS Folder sort order
			m_combo_ss_folder_order.Items.Clear();
			foreach (ESortOrder e in Enum.GetValues(typeof(ESortOrder))) m_combo_ss_folder_order.Items.Add(e.StrAttr());
			m_combo_ss_folder_order.SelectedIndex = (int)m_settings.SSFolderOrder;
			m_tt.SetToolTip(m_combo_ss_folder_order, m_settings.DescAttr("SSFolderOrder"));
			
			// File sort order
			m_combo_file_order.Items.Clear();
			foreach (ESortOrder e in Enum.GetValues(typeof(ESortOrder))) m_combo_file_order.Items.Add(e.StrAttr());
			m_combo_file_order.SelectedIndex = (int)m_settings.FilesOrder;
			m_tt.SetToolTip(m_combo_file_order, m_settings.DescAttr("FilesOrder"));
			
			// SS File sort order
			m_combo_ss_file_order.Items.Clear();
			foreach (ESortOrder e in Enum.GetValues(typeof(ESortOrder))) m_combo_ss_file_order.Items.Add(e.StrAttr());
			m_combo_ss_file_order.SelectedIndex = (int)m_settings.SSFilesOrder;
			m_tt.SetToolTip(m_combo_ss_file_order, m_settings.DescAttr("SSFilesOrder"));

			// Show filenames
			m_check_show_filenames.Checked = m_settings.ShowFilenames;
			m_tt.SetToolTip(m_check_show_filenames, m_settings.DescAttr("ShowFilenames"));
			
			// SS Volume
			m_track_ss_volume.Value = m_settings.SSVolume;
			m_tt.SetToolTip(m_track_ss_volume, m_settings.DescAttr("SSVolume"));

			m_btn_apply.Enabled = false;
		}
		
		/// <summary>Apply the current settings to m_settings</summary>
		private void Apply()
		{
			int ssrate, media_types = 0;
			media_types = Bit.SetBits(media_types, (int)EMediaType.Image, m_check_image_files.Checked);
			media_types = Bit.SetBits(media_types, (int)EMediaType.Video, m_check_video_files.Checked);
			media_types = Bit.SetBits(media_types, (int)EMediaType.Audio, m_check_audio_files.Checked);
			
			m_settings.MediaType           = (EMediaType)media_types;
			m_settings.ImageExtensions     = ExtnListToString(m_chklist_image_formats);
			m_settings.VideoExtensions     = ExtnListToString(m_chklist_video_formats);
			m_settings.AudioExtensions     = ExtnListToString(m_chklist_audio_formats);
			m_settings.AllowDuplicates     = m_check_allow_duplicates.Checked;
			m_settings.AlwaysOnTop         = m_check_always_on_top.Checked;
			m_settings.PrimaryDisplay      = m_combo_primary_display.SelectedIndex;
			m_settings.StartupVersionCheck = m_check_startup_version_check.Checked;
			m_settings.ResetZoomOnLoad     = m_check_reset_zoom_on_load.Checked;
			m_settings.ZoomType            = (EZoomType)m_combo_zoom_type.SelectedIndex;
			m_settings.SlideShowRate       = int.TryParse(m_edit_slide_show_rate.Text, out ssrate) ? ssrate : 5;
			m_settings.FolderOrder         = (ESortOrder)m_combo_folder_order.SelectedIndex;
			m_settings.SSFolderOrder       = (ESortOrder)m_combo_ss_folder_order.SelectedIndex;
			m_settings.FilesOrder          = (ESortOrder)m_combo_file_order.SelectedIndex;
			m_settings.SSFilesOrder        = (ESortOrder)m_combo_ss_file_order.SelectedIndex;
			m_settings.ShowFilenames       = m_check_show_filenames.Checked;
			m_settings.SSVolume            = m_track_ss_volume.Value;
			m_settings.Save();

			SetValues();
		}
		
		/// <summary>Mouse down handler</summary>
		private void OnMouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button != MouseButtons.Right) return;
			CheckedListBox lb = sender as CheckedListBox;
			if (lb == null) return;
			ContextMenuStrip menu = new ContextMenuStrip();
			ToolStripMenuItem opt = new ToolStripMenuItem("Edit");
			opt.Click += (s,a)=>{ EditFormats(lb); };
			menu.Items.Add(opt);
			menu.Show(MousePosition);
		}
		
		/// <summary>Edit the list of extensions for a media file type</summary>
		private void EditFormats(CheckedListBox lb)
		{
			Size sz = new Size(300, 60);
			TextBox edit = new TextBox
			{
				Location = new Point((sz.Width - 290) / 2, 2),
				Size = new Size(290,22),
				Text = ExtnListToString(lb),
			};
			Button btn_ok = new Button
			{
				Text = "OK",
				DialogResult = DialogResult.OK,
				Location = new Point(sz.Width - 164, sz.Height - 32),
			};
			Button btn_cancel = new Button
			{
				Text = "Cancel",
				DialogResult = DialogResult.Cancel,
				Location = new Point(sz.Width - 82, sz.Height - 32),
			};
			Form dlg = new Form
			{
				Text = "Edit file extensions",
				FormBorderStyle = FormBorderStyle.FixedToolWindow,
				StartPosition = FormStartPosition.CenterParent,
				ClientSize = sz,
				AcceptButton = btn_ok,
				CancelButton = btn_cancel,
			};
			m_tt.SetToolTip(edit, "A semi colon separated list of file extensions. Prefix with a '+' to indicate checked, or '-' to indicate not checked");
			dlg.Controls.Add(edit);
			dlg.Controls.Add(btn_ok);
			dlg.Controls.Add(btn_cancel);
			if (dlg.ShowDialog(this) == DialogResult.OK)
			{
				ExtnStringToList(edit.Text, lb);
				m_btn_apply.Enabled = true;
			}
			m_tt.SetToolTip(edit, null);
		}
		
		/// <summary>Convert the contents of a checked listbox into a string</summary>
		private static string ExtnListToString(CheckedListBox lb)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i != lb.Items.Count; ++i)
			{
				sb.Append(lb.GetItemChecked(i) ? '+' : '-').Append((string)lb.Items[i]).Append(';');
			}
			return sb.ToString(0, sb.Length-1);
		}
		
		/// <summary>Convert a string into items in a checked listbox</summary>
		private static void ExtnStringToList(string extns, CheckedListBox lb)
		{
			lb.Items.Clear();
			foreach (string e in extns.Split(';'))
			{
				if (e.Length < 2) continue;
				lb.Items.Add(e.Substring(1), e[0] == '+');
			}
		}
	}
}
