using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Gui.WinForms;
using Rylogic.Maths;
using Rylogic.Utility;
using Util = Rylogic.Utility.Util;

namespace RyLogViewer
{
	public partial class SettingsUI :Form
	{
		// Notes:
		//  - The settings UI operates on the main settings.
		//  - There is no cancel button, settings are modified as they're changed.

		private HoverScroll m_hover_scroll; // Hover scroll for the pattern grid
		private ESpecial m_special;

		#region UI Elements
		private TabControl m_tabctrl;
		private TabPage m_tab_general;
		private TabPage m_tab_highlight;
		private DataGridView m_grid_highlight;
		private TabPage m_tab_filter;
		private PatternUI m_pattern_hl;
		private GroupBox m_group_startup;
		private CheckBox m_chk_load_last_file;
		private CheckBox m_chk_save_screen_loc;
		private PatternUI m_pattern_ft;
		private DataGridView m_grid_filter;
		private SplitContainer m_split_hl;
		private SplitContainer m_split_ft;
		private CheckBox m_chk_show_totd;
		private TableLayoutPanel m_table_hl;
		private TableLayoutPanel m_table_ft;
		private GroupBox m_group_line_ends;
		private Label m_lbl_line_ends;
		private ValueBox m_tb_line_ends;
		private GroupBox m_group_grid;
		private Label m_lbl_history_length0;
		private NumericUpDown m_spinner_line_cache_count;
		private Label m_lbl_col_delims;
		private CheckBox m_chk_open_at_end;
		private CheckBox m_chk_ignore_blank_lines;
		private TabPage m_tab_logview;
		private GroupBox m_group_font;
		private Button m_btn_change_font;
		private TextBox m_text_font;
		private GroupBox m_group_log_text_colours;
		private NumericUpDown m_spinner_file_scroll_width;
		private Label m_lbl_file_scroll_width;
		private Label m_lbl_line2_example;
		private Label m_lbl_line1_example;
		private Label m_lbl_row_height;
		private NumericUpDown m_spinner_row_height;
		private Label m_lbl_selection_example;
		private Label m_lbl_line1_colours;
		private Label m_lbl_selection_colour;
		private CheckBox m_check_alternate_line_colour;
		private CheckBox m_chk_file_changes_additive;
		private ValueBox m_tb_col_delims;
		private Label m_lbl_column_count;
		private NumericUpDown m_spinner_column_count;
		private NumericUpDown m_spinner_max_mem_range;
		private Label m_lbl_max_scan_size0;
		private Label m_lbl_history_length1;
		private Label m_lbl_max_scan_size1;
		private CheckBox m_chk_c4u;
		private TabPage m_tab_transform;
		private SplitContainer m_split_tx;
		private TransformUI m_pattern_tx;
		private TableLayoutPanel m_table_tx;
		private DataGridView m_grid_transform;
		private Label m_lbl_max_line_len_kb;
		private NumericUpDown m_spinner_max_line_length;
		private Label m_lbl_max_line_length;
		private GroupBox m_group_settings;
		private Button m_btn_settings_reset;
		private Button m_btn_settings_save;
		private Button m_btn_settings_load;
		private TextBox m_text_settings;
		private Label label1;
		private ImageList m_image_list;
		private TabPage m_tab_action;
		private SplitContainer m_split_ac;
		private PatternUI m_pattern_ac;
		private TableLayoutPanel m_table_ac;
		private DataGridView m_grid_action;
		private Label label2;
		private Label m_lbl_ft_grid_desc;
		private Label label4;
		private Label label5;
		private CheckBox m_chk_reject_all_by_default;
		private Label m_lbl_web_proxy_port;
		private Label m_lbl_web_proxy_host;
		private NumericUpDown m_spinner_web_proxy_port;
		private ValueBox m_tb_web_proxy_host;
		private CheckBox m_chk_use_web_proxy;
		private GroupBox m_group_file_scroll;
		private Label m_lbl_fs_bookmark_colour;
		private Label m_lbl_fs_edit_bookmark_colour;
		private Label m_lbl_fs_visible_colour;
		private Label m_lbl_fs_edit_visible_colour;
		private Label m_lbl_fs_cached_colour;
		private Label m_lbl_fs_edit_cached_colour;
		private TableLayoutPanel m_table_general0;
		private TableLayoutPanel m_table_general1;
		private TableLayoutPanel m_table_general2;
		private TableLayoutPanel m_table_appearance0;
		private TableLayoutPanel m_table_appearance1;
		private GroupBox m_group_misc;
		private CheckBox m_check_full_filepath_in_title;
		private TableLayoutPanel m_table_appearance2;
		private Label m_lbl_tabsize;
		private NumericUpDown m_spinner_tabsize;
		private ToolTip m_tt;
		#endregion

		public SettingsUI(Main main, ETab tab, ESpecial special = ESpecial.None)
		{
			InitializeComponent();

			KeyPreview     = true;
			Main           = main;
			m_special      = special;
			m_hover_scroll = new HoverScroll();

			SetupUI(tab);
			UpdateUI();

			WhatsChanged = EWhatsChanged.Nothing;
		}
		protected override void Dispose(bool disposing)
		{
			Main = null;
			Util.Dispose(ref m_hover_scroll);
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			if (StartPosition == FormStartPosition.CenterParent)
				CenterToParent();
		}
		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
			FocusInput();
			PerformSpecial();
		}
		protected override void OnClosing(CancelEventArgs e)
		{
			// Watch for unsaved changes on closing
			base.OnClosing(e);

			// Grab focus to ensure all controls persist their state
			Focus();

			// Watch for unsaved changes
			SavePatternChanges(e);
		}
		protected override void OnClosed(EventArgs e)
		{
			// Save on close
			base.OnClosed(e);

			// Grab focus to ensure all controls persist their state
			Focus();

			// Save the settings
			Settings.Save();

			Main.UseLicensedFeature(FeatureName.Highlighting, new HighlightingCountLimiter(Main));
			Main.UseLicensedFeature(FeatureName.Filtering   , new FilteringCountLimiter(Main));
		}
		protected override void OnKeyDown(KeyEventArgs e)
		{
			switch (e.KeyCode)
			{
			default:
				e.Handled = false;
				break;
			case Keys.Escape:
				e.Handled = true;
				Close();
				break;
			}
			base.OnKeyDown(e);
		}

		/// <summary>The main UI</summary>
		private Main Main
		{
			get;
			set
			{
				if (field == value) return;
				if (field != null)
				{
					field.Settings.SettingChange -= HandleSettingChange;
				}
				field = value;
				if (field != null)
				{
					field.Settings.SettingChange += HandleSettingChange;
				}
				void HandleSettingChange(object sender, SettingChangeEventArgs e)
				{
					if (e.Before) return;
					UpdateUI();
				}
			}
		}

		/// <summary>App settings</summary>
		public Settings Settings
		{
			[DebuggerStepThrough] get { return Main.Settings; }
		}

		/// <summary>Returns a bit mask of the settings data that's changed</summary>
		public EWhatsChanged WhatsChanged { get; private set; }

		/// <summary>Access to the highlight pattern UI</summary>
		public PatternUI HighlightUI { get { return m_pattern_hl; } }
		public bool HighlightsChanged { get; set; }

		/// <summary>Access to the filter pattern UI</summary>
		public PatternUI FilterUI { get { return m_pattern_ft; } }
		public bool FiltersChanged { get; set; }

		/// <summary>Access to the transform pattern UI</summary>
		public TransformUI TransformUI { get { return m_pattern_tx; } }
		public bool TransformsChanged { get; set; }

		/// <summary>Access to the action pattern UI</summary>
		public PatternUI ActionUI { get { return m_pattern_ac; } }
		public bool ActionsChanged { get; set; }

		/// <summary>Set up UI elements</summary>
		private void SetupUI(ETab initial_tab)
		{
			m_pattern_hl.NewPattern(new Highlight());
			m_pattern_ft.NewPattern(new Filter()   );
			m_pattern_tx.NewPattern(new Transform());
			m_pattern_ac.NewPattern(new ClkAction());

			m_tabctrl.SelectedIndex = (int)initial_tab;
			m_tabctrl.Deselecting += (s,a) =>
			{
				TabChanging(a);
			};
			m_tabctrl.SelectedIndexChanged += (s,a) =>
			{
				UpdateUI();
				FocusInput();
			};

			SetupGeneralTab();
			SetupAppearanceTab();
			SetupHighlightTab();
			SetupFilterTab();
			SetupTransformTab();
			SetupActionTab();
		}

		/// <summary>Hook up events for the general tab</summary>
		private void SetupGeneralTab()
		{
			// Load last file on startup
			m_chk_load_last_file.ToolTip(m_tt, "Automatically load the last loaded file on startup");
			m_chk_load_last_file.Checked = Settings.LoadLastFile;
			m_chk_load_last_file.Click += (s,a)=>
			{
				Settings.LoadLastFile = m_chk_load_last_file.Checked;
				WhatsChanged |= EWhatsChanged.StartupOptions;
			};

			// Restore window position on startup
			m_chk_save_screen_loc.ToolTip(m_tt, "Restore the window to its last position on startup");
			m_chk_save_screen_loc.Checked = Settings.RestoreScreenLoc;
			m_chk_save_screen_loc.Click += (s,a)=>
			{
				Settings.RestoreScreenLoc = m_chk_save_screen_loc.Checked;
				WhatsChanged |= EWhatsChanged.StartupOptions;
			};

			// Show tip of the day on startup
			m_chk_show_totd.ToolTip(m_tt, "Show the 'Tip of the Day' dialog on startup");
			m_chk_show_totd.Checked = Settings.ShowTOTD;
			m_chk_show_totd.Click += (s,a)=>
			{
				Settings.ShowTOTD = m_chk_show_totd.Checked;
				WhatsChanged |= EWhatsChanged.StartupOptions;
			};

			// Check for updates
			m_chk_c4u.ToolTip(m_tt, "Check for newer versions on startup");
			m_chk_c4u.Checked = Settings.CheckForUpdates;
			m_chk_c4u.Click += (s,a)=>
			{
				Settings.CheckForUpdates = m_chk_c4u.Checked;
				WhatsChanged |= EWhatsChanged.StartupOptions;
			};

			// Use web proxy
			m_chk_use_web_proxy.ToolTip(m_tt, "Use a web proxy for internet connections (such as checking for updates)");
			m_chk_use_web_proxy.Checked = Settings.UseWebProxy;
			m_chk_use_web_proxy.Click += (s,a)=>
			{
				Settings.UseWebProxy = m_chk_use_web_proxy.Checked;
				WhatsChanged |= EWhatsChanged.Nothing;
			};

			// Web proxy host
			m_tb_web_proxy_host.ToolTip(m_tt, "The host name or IP address of the web proxy server");
			m_tb_web_proxy_host.ValueType = typeof(string);
			m_tb_web_proxy_host.ValidateText = t =>
			{
				if (!t.HasValue()) return true;
				var ty = Uri.CheckHostName(t);
				return ty == UriHostNameType.IPv4 || ty == UriHostNameType.Dns;
			};
			m_tb_web_proxy_host.Text = Settings.WebProxyHost;
			m_tb_web_proxy_host.ValueCommitted += (s,a)=>
			{
				Settings.WebProxyHost = m_tb_web_proxy_host.Text;
				WhatsChanged |= EWhatsChanged.Nothing;
			};

			// Web proxy port
			m_spinner_web_proxy_port.ToolTip(m_tt, "The port number of the web proxy server");
			m_spinner_web_proxy_port.Minimum = Constants.PortNumberMin;
			m_spinner_web_proxy_port.Maximum = Constants.PortNumberMax;
			m_spinner_web_proxy_port.Value = Math_.Clamp(Settings.WebProxyPort, Constants.PortNumberMin, Constants.PortNumberMax);
			m_spinner_web_proxy_port.ValueChanged += (s,a)=>
			{
				Settings.WebProxyPort = (int)m_spinner_web_proxy_port.Value;
				WhatsChanged |= EWhatsChanged.Nothing;
			};

			// Line endings
			m_tb_line_ends.ToolTip(m_tt,
				"Set the line ending characters to expect in the log data.\r\n"+
				"Use '<CR>' for carriage return, '<LF>' for line feed, '<TAB>' for tab characters.\r\n"+
				"Specify UNICODE characters using the form \\uXXXX\r\n"+
				"Leave blank to auto detect");
			m_tb_line_ends.ValueType = typeof(string);
			m_tb_line_ends.Text = Settings.RowDelimiter;
			m_tb_line_ends.ValueCommitted += (s,a) =>
			{
				Settings.RowDelimiter = m_tb_line_ends.Text;
				WhatsChanged |= EWhatsChanged.FileParsing;
			};

			// Column delimiters
			m_tb_col_delims.ToolTip(m_tt, "Set the characters that separate columns in the log data.\r\nUse '<TAB>' for a tab character.\r\nLeave blank for no column delimiter");
			m_tb_col_delims.ValueType = typeof(string);
			m_tb_col_delims.Text = Settings.ColDelimiter;
			m_tb_col_delims.ValueCommitted += (s,a) =>
			{
				Settings.ColDelimiter = m_tb_col_delims.Text;
				WhatsChanged |= EWhatsChanged.FileParsing;
			};

			// Column Count
			m_spinner_column_count.ToolTip(m_tt, "The number of columns to display in the grid.\r\nUsed when the column delimiter is not blank");
			m_spinner_column_count.Minimum = Constants.ColumnCountMin;
			m_spinner_column_count.Maximum = Constants.ColumnCountMax;
			m_spinner_column_count.Value = Math_.Clamp(Settings.ColumnCount, Constants.ColumnCountMin, Constants.ColumnCountMax);
			m_spinner_column_count.ValueChanged += (s,a) =>
			{
				Settings.ColumnCount = (int)m_spinner_column_count.Value;
				WhatsChanged |= EWhatsChanged.Rendering;
			};

			// Include blank lines
			m_chk_ignore_blank_lines.ToolTip(m_tt, "Ignore blank lines when loading the log file");
			m_chk_ignore_blank_lines.Checked = Settings.IgnoreBlankLines;
			m_chk_ignore_blank_lines.CheckedChanged += (s,a)=>
			{
				Settings.IgnoreBlankLines = m_chk_ignore_blank_lines.Checked;
				WhatsChanged |= EWhatsChanged.FileParsing;
			};

			// Lines cached
			m_spinner_line_cache_count.ToolTip(m_tt, "The number of lines to scan into memory around the currently selected line");
			m_spinner_line_cache_count.Minimum = Constants.LineCacheCountMin;
			m_spinner_line_cache_count.Maximum = Constants.LineCacheCountMax;
			m_spinner_line_cache_count.Value = Math_.Clamp(Settings.LineCacheCount, (int)m_spinner_line_cache_count.Minimum, (int)m_spinner_line_cache_count.Maximum);
			m_spinner_line_cache_count.ValueChanged += (s,a)=>
			{
				Settings.LineCacheCount = (int)m_spinner_line_cache_count.Value;
				WhatsChanged |= EWhatsChanged.FileParsing;
			};

			// Max memory range
			m_spinner_max_mem_range.ToolTip(m_tt, "The maximum number of bytes to scan when finding lines around the currently selected row (in MB).");
			m_spinner_max_mem_range.Minimum = Constants.FileBufSizeMin / Constants.OneMB;
			m_spinner_max_mem_range.Maximum = Constants.FileBufSizeMax / Constants.OneMB;
			m_spinner_max_mem_range.Value = Math_.Clamp(Settings.FileBufSize / Constants.OneMB, (int)m_spinner_max_mem_range.Minimum, (int)m_spinner_max_mem_range.Maximum);
			m_spinner_max_mem_range.ValueChanged += (s,a)=>
			{
				Settings.FileBufSize = (int)m_spinner_max_mem_range.Value * Constants.OneMB;
				WhatsChanged |= EWhatsChanged.FileParsing;
			};

			// Max line length
			m_spinner_max_line_length.ToolTip(m_tt, "The maximum length of a line in the log file.\r\nIf the log contains lines longer than this an error will be reported when loading the file");
			m_spinner_max_line_length.Minimum = Constants.MaxLineLengthMin / Constants.OneKB;
			m_spinner_max_line_length.Maximum = Constants.MaxLineLengthMax / Constants.OneKB;
			m_spinner_max_line_length.Value = Math_.Clamp(Settings.MaxLineLength / Constants.OneKB, (int)m_spinner_max_line_length.Minimum, (int)m_spinner_max_line_length.Maximum);
			m_spinner_max_line_length.ValueChanged += (s,a)=>
			{
				Settings.MaxLineLength = (int)m_spinner_max_line_length.Value * Constants.OneKB;
				WhatsChanged |= EWhatsChanged.FileParsing;
			};

			// Open at end
			m_chk_open_at_end.ToolTip(m_tt, "If checked, opens files showing the end of the file.\r\nIf unchecked opens files at the beginning");
			m_chk_open_at_end.Checked = Settings.OpenAtEnd;
			m_chk_open_at_end.CheckedChanged += (s,a)=>
			{
				Settings.OpenAtEnd = m_chk_open_at_end.Checked;
				WhatsChanged |= EWhatsChanged.FileOpenOptions;
			};

			// File changes additive
			m_chk_file_changes_additive.ToolTip(m_tt, "Assume all changes to the watched file are additive only\r\nIf checked, reloading of changed files will not invalidate existing cached data");
			m_chk_file_changes_additive.Checked = Settings.FileChangesAdditive;
			m_chk_file_changes_additive.CheckedChanged += (s,a)=>
			{
				Settings.FileChangesAdditive = m_chk_file_changes_additive.Checked;
				WhatsChanged |= EWhatsChanged.FileParsing;
			};

			// Settings reset
			m_btn_settings_reset.ToolTip(m_tt, "Reset settings to their default values.");
			m_btn_settings_reset.Click += (s,a)=>
			{
				ResetSettingsToDefaults();
				UpdateUI();
				WhatsChanged |= EWhatsChanged.Everything;
			};

			// Settings filepath
			m_text_settings.ToolTip(m_tt, "The path to the current settings file");

			// Settings load
			m_btn_settings_load.ToolTip(m_tt, "Load settings from file");
			m_btn_settings_load.Click += (s,a)=>
			{
				var dg = new OpenFileDialog
				{
					Title = "Choose a settings file to load",
					Filter = Constants.SettingsFileFilter,
					CheckFileExists = true,
					InitialDirectory = Path.GetDirectoryName(Settings.Filepath)
				};
				using (dg)
				{
					if (dg.ShowDialog(this) != DialogResult.OK) return;
					Settings.Load(dg.FileName);
					UpdateUI();
					WhatsChanged |= EWhatsChanged.Everything;
				}
			};

			// Settings save
			m_btn_settings_save.ToolTip(m_tt, "Save current settings to a file");
			m_btn_settings_save.Click += (s,a)=>
			{
				var dg = new SaveFileDialog
				{
					Title = "Save current settings",
					Filter = Constants.SettingsFileFilter,
					CheckPathExists = true,
					InitialDirectory = Path.GetDirectoryName(Settings.Filepath)
				};
				using (dg)
				{
					if (dg.ShowDialog(this) != DialogResult.OK) return;
					Settings.Filepath = dg.FileName;
					Settings.Save();
					UpdateUI();
					WhatsChanged |= EWhatsChanged.Nothing;
				}
			};
		}

		/// <summary>Hook up events for the log view tab</summary>
		private void SetupAppearanceTab()
		{
			string tt;

			// Selection colour
			m_lbl_selection_example.ToolTip(m_tt, "Set the selection foreground and back colours in the log view\r\nClick here to modify the colours");
			m_lbl_selection_example.MouseClick += (s,a)=>
				{
					var dlg = new ColourPickerUI(Settings.LineSelectForeColour, Settings.LineSelectBackColour);
					if (dlg.ShowDialog(this) != DialogResult.OK) return;
					Settings.LineSelectForeColour = dlg.FontColor;
					Settings.LineSelectBackColour = dlg.BkgdColor;
					WhatsChanged |= EWhatsChanged.Rendering;
				};

			// Log text colour
			m_lbl_line1_example.ToolTip(m_tt, "Set the foreground and background colours in the log view\r\nClick here to modify the colours");
			m_lbl_line1_example.MouseClick += (s,a)=>
				{
					var dlg = new ColourPickerUI(Settings.LineForeColour1, Settings.LineBackColour1);
					if (dlg.ShowDialog(this) != DialogResult.OK) return;
					Settings.LineForeColour1 = dlg.FontColor;
					Settings.LineBackColour1 = dlg.BkgdColor;
					WhatsChanged |= EWhatsChanged.Rendering;
				};

			// Alt log text colour
			m_lbl_line2_example.ToolTip(m_tt, "Set the foreground and background colours for odd numbered rows in the log view\r\nClick here to modify the colours");
			m_lbl_line2_example.MouseClick += (s,a)=>
				{
					var dlg = new ColourPickerUI(Settings.LineForeColour2, Settings.LineBackColour2);
					if (dlg.ShowDialog(this) != DialogResult.OK) return;
					Settings.LineForeColour2 = dlg.FontColor;
					Settings.LineBackColour2 = dlg.BkgdColor;
					WhatsChanged |= EWhatsChanged.Rendering;
				};

			// Enable alt line colours
			m_check_alternate_line_colour.ToolTip(m_tt, "Enable alternating colours in the log view");
			m_check_alternate_line_colour.CheckedChanged += (s,a)=>
				{
					Settings.AlternateLineColours = m_check_alternate_line_colour.Checked;
					WhatsChanged |= EWhatsChanged.Rendering;
				};

			// Row height
			m_spinner_row_height.ToolTip(m_tt, "The height of each row in the log view");
			m_spinner_row_height.Minimum = Constants.RowHeightMinHeight;
			m_spinner_row_height.Maximum = Constants.RowHeightMaxHeight;
			m_spinner_row_height.Value = Math_.Clamp(Settings.RowHeight, (int)m_spinner_row_height.Minimum, (int)m_spinner_row_height.Maximum);
			m_spinner_row_height.ValueChanged += (s,a)=>
				{
					Settings.RowHeight = (int)m_spinner_row_height.Value;
					WhatsChanged |= EWhatsChanged.Rendering;
				};

			// Font
			m_text_font.ToolTip(m_tt, "The font used to display the log file data");
			m_text_font.Text = $"{Settings.Font.Name}, {Settings.Font.Size}pt";
			m_text_font.Font = Settings.Font;

			// Font button
			m_btn_change_font.ToolTip(m_tt, "Change the log view font");
			m_btn_change_font.Click += (s,a)=>
				{
					var dg = new FontDialog{Font = Settings.Font};
					if (dg.ShowDialog(this) != DialogResult.OK) return;
					m_text_font.Font = dg.Font;
					m_text_font.Text = $"{dg.Font.Name}, {dg.Font.Size}pt";
					Settings.Font = dg.Font;
				};

			// Full file path
			m_check_full_filepath_in_title.ToolTip(m_tt, "Check to show the full file path of the open log file in the window title bar");
			m_check_full_filepath_in_title.CheckedChanged += (s,a)=>
				{
					Settings.FullPathInTitle = m_check_full_filepath_in_title.Checked;
					WhatsChanged |= EWhatsChanged.WindowDisplay;
				};

			// Tab size in spaces
			m_spinner_tabsize.ToolTip(m_tt, "The width to display tab characters measured in spaces");
			m_spinner_tabsize.Minimum = 0;
			m_spinner_tabsize.Maximum = 100;
			m_spinner_tabsize.Value = Math_.Clamp(Settings.TabSizeInSpaces, (int)m_spinner_tabsize.Minimum, (int)m_spinner_tabsize.Maximum);
			m_spinner_tabsize.ValueChanged += (s,a) =>
				{
					Settings.TabSizeInSpaces = (int)m_spinner_tabsize.Value;
					WhatsChanged |= EWhatsChanged.Rendering;
				};

			// File scroll width
			m_spinner_file_scroll_width.ToolTip(m_tt, "The width of the scroll bar that shows the current position within the log file");
			m_spinner_file_scroll_width.Minimum = Constants.FileScrollMinWidth;
			m_spinner_file_scroll_width.Maximum = Constants.FileScrollMaxWidth;
			m_spinner_file_scroll_width.Value = Math_.Clamp(Settings.FileScrollWidth, (int)m_spinner_file_scroll_width.Minimum, (int)m_spinner_file_scroll_width.Maximum);
			m_spinner_file_scroll_width.ValueChanged += (s,a)=>
				{
					Settings.FileScrollWidth = (int)m_spinner_file_scroll_width.Value;
					WhatsChanged |= EWhatsChanged.Rendering;
				};

			tt = "The colour representing the cached portion of the log file in the file scroll bar\r\nClick here to modify the colour";
			m_lbl_fs_cached_colour.ToolTip(m_tt, tt);
			m_lbl_fs_edit_cached_colour.ToolTip(m_tt, tt);
			m_lbl_fs_edit_cached_colour.BackColor = Settings.ScrollBarCachedRangeColour;
			m_lbl_fs_edit_cached_colour.MouseClick += (s,a)=>
				{
					Settings.ScrollBarCachedRangeColour = PickColour(Settings.ScrollBarCachedRangeColour);
					m_lbl_fs_edit_cached_colour.BackColor = Settings.ScrollBarCachedRangeColour;
					WhatsChanged |= EWhatsChanged.Rendering;
				};

			tt = "The colour representing the portion of the log file currently on screen\r\nClick here to modify the colour";
			m_lbl_fs_visible_colour.ToolTip(m_tt, tt);
			m_lbl_fs_edit_visible_colour.ToolTip(m_tt, tt);
			m_lbl_fs_edit_visible_colour.BackColor = Settings.ScrollBarDisplayRangeColour;
			m_lbl_fs_edit_visible_colour.MouseClick += (s,a)=>
				{
					Settings.ScrollBarDisplayRangeColour = PickColour(Settings.ScrollBarDisplayRangeColour);
					m_lbl_fs_edit_visible_colour.BackColor = Settings.ScrollBarDisplayRangeColour;
					WhatsChanged |= EWhatsChanged.Rendering;
				};

			tt = "The colour of the bookmarked locations\r\nClick here to modify the colour";
			m_lbl_fs_bookmark_colour.ToolTip(m_tt, tt);
			m_lbl_fs_edit_bookmark_colour.ToolTip(m_tt, tt);
			m_lbl_fs_edit_bookmark_colour.BackColor = Settings.BookmarkColour;
			m_lbl_fs_edit_bookmark_colour.MouseClick += (s,a)=>
				{
					Settings.BookmarkColour = PickColour(Settings.BookmarkColour);
					m_lbl_fs_edit_bookmark_colour.BackColor = Settings.BookmarkColour;
					WhatsChanged |= EWhatsChanged.Rendering;
				};
		}

		/// <summary>Hook up events for the highlights tab</summary>
		private void SetupHighlightTab()
		{
			var hl_style = new DataGridViewCellStyle
			{
				Font      = Settings.Font,
				ForeColor = Settings.LineForeColour1,
				BackColor = Settings.LineBackColour1,
			};

			// Highlight grid
			m_grid_highlight.AllowDrop           = true;
			m_grid_highlight.VirtualMode         = true;
			m_grid_highlight.AutoGenerateColumns = false;
			m_grid_highlight.ColumnCount         = m_grid_highlight.RowCount = 0;
			m_grid_highlight.Columns.Add(new DataGridViewImageColumn   {Name = ColumnNames.Active    ,HeaderText = "Active"   ,FillWeight = 25  ,AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader ,ImageLayout = DataGridViewImageCellLayout.Zoom ,ToolTipText = ColumnTT.Active});
			m_grid_highlight.Columns.Add(new DataGridViewTextBoxColumn {Name = ColumnNames.Pattern   ,HeaderText = "Pattern"  ,FillWeight = 100 ,ReadOnly = true ,ToolTipText = ColumnTT.Pattern});
			m_grid_highlight.Columns.Add(new DataGridViewTextBoxColumn {Name = ColumnNames.Highlight ,HeaderText = "Highlight",FillWeight = 30  ,ReadOnly = true ,ToolTipText = ColumnTT.Highlight});
			m_grid_highlight.Columns.Add(new DataGridViewTextBoxColumn {Name = ColumnNames.Colours   ,HeaderText = "Colours"  ,FillWeight = 100 ,ReadOnly = true ,DefaultCellStyle = hl_style ,ToolTipText = ColumnTT.Colours});
			m_grid_highlight.Columns.Add(new DataGridViewImageColumn   {Name = ColumnNames.Edit      ,HeaderText = "Edit"     ,FillWeight = 15  ,AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader ,ImageLayout = DataGridViewImageCellLayout.Zoom ,ToolTipText = ColumnTT.Edit});
			m_grid_highlight.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
			m_grid_highlight.KeyDown                    += DataGridView_.Copy;
			m_grid_highlight.KeyDown                    += DataGridView_.SelectAll;
			m_grid_highlight.UserDeletingRow            += (s,a)=> OnDeletingRow    (m_grid_highlight, Settings.Patterns.Highlights, m_pattern_hl, a.Row.Index);
			m_grid_highlight.MouseDown                  += (s,a)=> OnMouseDown      (m_grid_highlight, Settings.Patterns.Highlights, a);
			m_grid_highlight.DragOver                   += (s,a)=> DoDragDrop       (m_grid_highlight, Settings.Patterns.Highlights, a, false);
			m_grid_highlight.CellValueNeeded            += (s,a)=> OnCellValueNeeded(m_grid_highlight, Settings.Patterns.Highlights, a);
			m_grid_highlight.CellClick                  += (s,a)=> OnCellClick      (m_grid_highlight, Settings.Patterns.Highlights, m_pattern_hl, a);
			m_grid_highlight.CellDoubleClick            += (s,a)=> OnCellDoubleClick(m_grid_highlight, Settings.Patterns.Highlights, m_pattern_hl, a);
			m_grid_highlight.CellFormatting             += (s,a)=> OnCellFormatting (m_grid_highlight, Settings.Patterns.Highlights, a);
			m_grid_highlight.CellContextMenuStripNeeded += (s,a)=> OnCellClick      (m_grid_highlight, Settings.Patterns.Highlights, m_pattern_hl, a);
			m_grid_highlight.DataError                  += (s,a)=> a.Cancel = true;

			m_hover_scroll.WindowHandles.Add(m_grid_highlight.Handle);

			// Highlight pattern
			m_pattern_hl.Commit += (s,a)=>
			{
				WhatsChanged |= EWhatsChanged.Rendering;
				CommitPattern(m_pattern_hl, Settings.Patterns.Highlights);
				HighlightsChanged = true;
				UpdateUI();
			};
		}

		/// <summary>Hook up events for the filters tab</summary>
		private void SetupFilterTab()
		{
			m_grid_filter.AllowDrop           = true;
			m_grid_filter.VirtualMode         = true;
			m_grid_filter.AutoGenerateColumns = false;
			m_grid_filter.ColumnCount         = m_grid_filter.RowCount = 0;
			m_grid_filter.Columns.Add(new DataGridViewImageColumn   {Name = ColumnNames.Active    ,HeaderText = "Active"      ,FillWeight = 25  ,ReadOnly = true ,AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader ,ImageLayout = DataGridViewImageCellLayout.Zoom ,ToolTipText = ColumnTT.Active});
			m_grid_filter.Columns.Add(new DataGridViewTextBoxColumn {Name = ColumnNames.Behaviour ,HeaderText = "If Match..." ,FillWeight = 25  ,ReadOnly = true ,AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader ,ToolTipText = ColumnTT.Behaviour});
			m_grid_filter.Columns.Add(new DataGridViewTextBoxColumn {Name = ColumnNames.Pattern   ,HeaderText = "Pattern"     ,FillWeight = 100 ,ReadOnly = true ,ToolTipText = ColumnTT.Pattern});
			m_grid_filter.Columns.Add(new DataGridViewImageColumn   {Name = ColumnNames.Edit      ,HeaderText = "Edit"        ,FillWeight = 15  ,ReadOnly = true ,AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader ,ImageLayout = DataGridViewImageCellLayout.Zoom ,ToolTipText = ColumnTT.Edit});
			m_grid_filter.ClipboardCopyMode   = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
			m_grid_filter.KeyDown            += DataGridView_.Copy;
			m_grid_filter.KeyDown            += DataGridView_.SelectAll;
			m_grid_filter.UserDeletingRow    += (s,a)=> OnDeletingRow    (m_grid_filter, Settings.Patterns.Filters, m_pattern_ft, a.Row.Index);
			m_grid_filter.MouseDown          += (s,a)=> OnMouseDown      (m_grid_filter, Settings.Patterns.Filters, a);
			m_grid_filter.DragOver           += (s,a)=> DoDragDrop       (m_grid_filter, Settings.Patterns.Filters, a, false);
			m_grid_filter.CellValueNeeded    += (s,a)=> OnCellValueNeeded(m_grid_filter, Settings.Patterns.Filters, a);
			m_grid_filter.CellClick          += (s,a)=> OnCellClick      (m_grid_filter, Settings.Patterns.Filters, m_pattern_ft, a);
			m_grid_filter.CellDoubleClick    += (s,a)=> OnCellDoubleClick(m_grid_filter, Settings.Patterns.Filters, m_pattern_ft, a);
			m_grid_filter.CellFormatting     += (s,a)=> OnCellFormatting (m_grid_filter, Settings.Patterns.Filters, a);
			m_grid_filter.DataError          += (s,a)=> a.Cancel = true;

			m_hover_scroll.WindowHandles.Add(m_grid_filter.Handle);

			// Check reject all by default
			m_chk_reject_all_by_default.Checked = Settings.Patterns.Filters.Contains(Filter.RejectAll);
			m_chk_reject_all_by_default.Click += (s,a)=>
			{
				Settings.Patterns.Filters.Remove(Filter.RejectAll);
				if (m_chk_reject_all_by_default.Checked)
					Settings.Patterns.Filters.Add(Filter.RejectAll);

				FlagAsChanged(m_grid_filter);
				UpdateUI();
			};

			// Filter pattern
			m_pattern_ft.Commit += (s,a)=>
			{
				WhatsChanged |= EWhatsChanged.FileParsing;
				CommitPattern(m_pattern_ft, Settings.Patterns.Filters);
				FiltersChanged = true;
				UpdateUI();
			};
		}

		/// <summary>Hook up events for the transforms tab</summary>
		private void SetupTransformTab()
		{
			m_grid_transform.AllowDrop           = true;
			m_grid_transform.VirtualMode         = true;
			m_grid_transform.AutoGenerateColumns = false;
			m_grid_transform.ColumnCount         = m_grid_transform.RowCount = 0;
			m_grid_transform.Columns.Add(new DataGridViewImageColumn  {Name = ColumnNames.Active  ,HeaderText = "Active"  ,FillWeight = 25  ,AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader ,ImageLayout = DataGridViewImageCellLayout.Zoom ,ToolTipText = ColumnTT.Active});
			m_grid_transform.Columns.Add(new DataGridViewTextBoxColumn{Name = ColumnNames.Pattern ,HeaderText = "Pattern" ,FillWeight = 100 ,ReadOnly = true ,ToolTipText = ColumnTT.Pattern});
			m_grid_transform.Columns.Add(new DataGridViewImageColumn  {Name = ColumnNames.Edit    ,HeaderText = "Edit"    ,FillWeight = 15  ,AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader ,ImageLayout = DataGridViewImageCellLayout.Zoom ,ToolTipText = ColumnTT.Edit});
			m_grid_transform.ClipboardCopyMode   = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
			m_grid_transform.KeyDown            += DataGridView_.Copy;
			m_grid_transform.KeyDown            += DataGridView_.SelectAll;
			m_grid_transform.UserDeletingRow    += (s,a)=> OnDeletingRow    (m_grid_transform, Settings.Patterns.Transforms, m_pattern_tx, a.Row.Index);
			m_grid_transform.MouseDown          += (s,a)=> OnMouseDown      (m_grid_transform, Settings.Patterns.Transforms, a);
			m_grid_transform.DragOver           += (s,a)=> DoDragDrop       (m_grid_transform, Settings.Patterns.Transforms, a, false);
			m_grid_transform.CellValueNeeded    += (s,a)=> OnCellValueNeeded(m_grid_transform, Settings.Patterns.Transforms, a);
			m_grid_transform.CellClick          += (s,a)=> OnCellClick      (m_grid_transform, Settings.Patterns.Transforms, m_pattern_tx, a);
			m_grid_transform.CellDoubleClick    += (s,a)=> OnCellDoubleClick(m_grid_transform, Settings.Patterns.Transforms, m_pattern_tx, a);
			m_grid_transform.CellFormatting     += (s,a)=> OnCellFormatting (m_grid_transform, Settings.Patterns.Transforms, a);
			m_grid_transform.DataError          += (s,a)=> a.Cancel = true;

			m_hover_scroll.WindowHandles.Add(m_grid_transform.Handle);

			// Transform
			m_pattern_tx.Commit += (s,a)=>
			{
				WhatsChanged |= EWhatsChanged.Rendering;
				CommitPattern(m_pattern_tx, Settings.Patterns.Transforms);
				TransformsChanged = true;
				UpdateUI();
			};
		}

		/// <summary>Hook up events for the actions tab</summary>
		private void SetupActionTab()
		{
			// Action grid
			m_grid_action.AllowDrop           = true;
			m_grid_action.VirtualMode         = true;
			m_grid_action.AutoGenerateColumns = false;
			m_grid_action.ColumnCount         = m_grid_action.RowCount = 0;
			m_grid_action.Columns.Add(new DataGridViewImageColumn   {Name = ColumnNames.Active      ,HeaderText = "Active" ,FillWeight = 25  ,AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader ,ImageLayout = DataGridViewImageCellLayout.Zoom ,ToolTipText = ColumnTT.Active});
			m_grid_action.Columns.Add(new DataGridViewTextBoxColumn {Name = ColumnNames.Pattern     ,HeaderText = "Pattern" ,FillWeight = 100 ,ReadOnly = true ,ToolTipText = ColumnTT.Pattern});
			m_grid_action.Columns.Add(new DataGridViewTextBoxColumn {Name = ColumnNames.ClickAction ,HeaderText = "Action" ,FillWeight = 100 ,ReadOnly = true ,ToolTipText = ColumnTT.ClickAction});
			m_grid_action.Columns.Add(new DataGridViewImageColumn   {Name = ColumnNames.Edit        ,HeaderText = "Edit"    ,FillWeight = 15  ,AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader ,ImageLayout = DataGridViewImageCellLayout.Zoom ,ToolTipText = ColumnTT.Edit});
			m_grid_action.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
			m_grid_action.KeyDown                    += DataGridView_.Copy;
			m_grid_action.KeyDown                    += DataGridView_.SelectAll;
			m_grid_action.UserDeletingRow            += (s,a)=> OnDeletingRow    (m_grid_action, Settings.Patterns.Actions, m_pattern_ac, a.Row.Index);
			m_grid_action.MouseDown                  += (s,a)=> OnMouseDown      (m_grid_action, Settings.Patterns.Actions, a);
			m_grid_action.DragOver                   += (s,a)=> DoDragDrop       (m_grid_action, Settings.Patterns.Actions, a, false);
			m_grid_action.CellValueNeeded            += (s,a)=> OnCellValueNeeded(m_grid_action, Settings.Patterns.Actions, a);
			m_grid_action.CellClick                  += (s,a)=> OnCellClick      (m_grid_action, Settings.Patterns.Actions, m_pattern_ac, a);
			m_grid_action.CellDoubleClick            += (s,a)=> OnCellDoubleClick(m_grid_action, Settings.Patterns.Actions, m_pattern_ac, a);
			m_grid_action.CellFormatting             += (s,a)=> OnCellFormatting (m_grid_action, Settings.Patterns.Actions, a);
			m_grid_action.CellContextMenuStripNeeded += (s,a)=> OnCellClick      (m_grid_action, Settings.Patterns.Actions, m_pattern_ac, a);
			m_grid_action.DataError                  += (s,a)=> a.Cancel = true;

			m_hover_scroll.WindowHandles.Add(m_grid_action.Handle);

			// Action pattern
			m_pattern_ac.Commit += (s,a)=>
			{
				WhatsChanged |= EWhatsChanged.Nothing;
				CommitPattern(m_pattern_ac, Settings.Patterns.Actions);
				ActionsChanged = true;
				UpdateUI();
			};
		}

		/// <summary>Update the UI state based on current settings</summary>
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
			bool use_web_proxy = Settings.UseWebProxy;
			m_lbl_web_proxy_host.Enabled = use_web_proxy;
			m_lbl_web_proxy_port.Enabled = use_web_proxy;
			m_tb_web_proxy_host.Enabled = use_web_proxy;
			m_spinner_web_proxy_port.Enabled = use_web_proxy;
			m_spinner_column_count.Enabled = Settings.ColDelimiter.Length != 0;

			m_check_alternate_line_colour.Checked = Settings.AlternateLineColours;
			m_lbl_selection_example.BackColor = Settings.LineSelectBackColour;
			m_lbl_selection_example.ForeColor = Settings.LineSelectForeColour;
			m_lbl_line1_example.BackColor = Settings.LineBackColour1;
			m_lbl_line1_example.ForeColor = Settings.LineForeColour1;
			m_lbl_line2_example.BackColor = Settings.LineBackColour2;
			m_lbl_line2_example.ForeColor = Settings.LineForeColour2;
			m_lbl_line2_example.Enabled = Settings.AlternateLineColours;
			m_check_full_filepath_in_title.Checked = Settings.FullPathInTitle;

			m_chk_reject_all_by_default.Checked = Settings.Patterns.Filters.Contains(Filter.RejectAll);

			int selected = m_grid_highlight.FirstSelectedRowIndex();
			m_grid_highlight.CurrentCell = null;
			m_grid_highlight.RowCount = 0;
			m_grid_highlight.RowCount = Settings.Patterns.Highlights.Count;
			m_grid_highlight.SelectSingleRow(selected);

			selected = m_grid_filter.FirstSelectedRowIndex();
			m_grid_filter.CurrentCell = null;
			m_grid_filter.RowCount = 0;
			m_grid_filter.RowCount = Settings.Patterns.Filters.Count;
			m_grid_filter.SelectSingleRow(selected);

			selected = m_grid_transform.FirstSelectedRowIndex();
			m_grid_transform.CurrentCell = null;
			m_grid_transform.RowCount = 0;
			m_grid_transform.RowCount = Settings.Patterns.Transforms.Count;
			m_grid_transform.SelectSingleRow(selected);

			selected = m_grid_action.FirstSelectedRowIndex();
			m_grid_action.CurrentCell = null;
			m_grid_action.RowCount = 0;
			m_grid_action.RowCount = Settings.Patterns.Actions.Count;
			m_grid_action.SelectSingleRow(selected);

			m_text_settings.Text = Settings.Filepath;

			Main.UseLicensedFeature(FeatureName.Highlighting, new SettingsHighlightingCountLimiter(Main, this));
			Main.UseLicensedFeature(FeatureName.Filtering, new SettingsFilteringCountLimiter(Main, this));
			HeuristicHints();
		}

		/// <summary>Check all pattern tabs for unsaved changes and prompt to save if any</summary>
		private void SavePatternChanges(CancelEventArgs args)
		{
			SavePatternChanges(m_pattern_hl, Settings.Patterns.Highlights, "Highlighting", args);
			SavePatternChanges(m_pattern_ft, Settings.Patterns.Filters   , "Filtering"   , args);
			SavePatternChanges(m_pattern_tx, Settings.Patterns.Transforms, "Transform"   , args);
			SavePatternChanges(m_pattern_ac, Settings.Patterns.Actions   , "Click action", args);
		}

		/// <summary>Helper for prompting to save changes to a pattern before leaving the tab</summary>
		private void SavePatternChanges<TPattern>(IPatternUI pattern_ui, IList<TPattern> patterns, string text, CancelEventArgs args) where TPattern:IPattern,new()
		{
			if (pattern_ui.HasUnsavedChanges)
			{
				var res = MsgBox.Show(this, $"{text} pattern contains unsaved changes.\r\n\r\nSave changes?","Unsaved Changes",MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
				if (res == DialogResult.Cancel) { args.Cancel = true; return; }
				if (res == DialogResult.No)     { pattern_ui.NewPattern(new TPattern()); return; }
				if (!pattern_ui.CommitEnabled)  { args.Cancel = true; return; }
				CommitPattern(pattern_ui, patterns);
			}
		}

		/// <summary>Save a modified pattern from a pattern UI</summary>
		private void CommitPattern<TPattern>(IPatternUI pattern_ui, IList<TPattern> patterns) where TPattern:IPattern,new()
		{
			if (pattern_ui.IsNew) patterns.Insert(0, (TPattern)pattern_ui.Pattern);
			else                  patterns.Replace((TPattern)pattern_ui.Original, (TPattern)pattern_ui.Pattern);
			pattern_ui.NewPattern(new TPattern());
		}

		/// <summary>Called as the tab is changing/closing</summary>
		private void TabChanging(TabControlCancelEventArgs args)
		{
			if (args.Action == TabControlAction.Deselecting)
				SavePatternChanges(args);
		}

		/// <summary>Reset the settings to their default values</summary>
		private void ResetSettingsToDefaults()
		{
			// Confirm first...
			var res = MsgBox.Show(this, "This will reset all current settings to their default values\r\n\r\nContinue?", "Confirm Reset Settings", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
			if (res != DialogResult.Yes) return;

			// Flatten the settings
			try
			{
				// Reset the settings after the dialog has closed so that the save
				// on close mechanism doesn't overwrite the default settings
				var main = Main;
				main.BeginInvoke(() =>
				{
					main.Settings.Reset();
					main.ApplySettings();
					main.ShowOptions(ETab.General);
				});
				Close();
			}
			catch (Exception ex)
			{
				Log.Write(ELogLevel.Error, ex, "Resetting settings to defaults");
				Misc.ShowMessage(this, "Failed to reset settings to their default values.", "Reset Settings Failed", MessageBoxIcon.Error, ex);
			}
		}

		/// <summary>Set input focus to the primary input field of the current tab</summary>
		private void FocusInput()
		{
			switch ((ETab)m_tabctrl.SelectedIndex)
			{
			case ETab.Highlights: m_pattern_hl.FocusInput(); break;
			case ETab.Filters:    m_pattern_ft.FocusInput(); break;
			case ETab.Transforms: m_pattern_tx.FocusInput(); break;
			case ETab.Actions:    m_pattern_ac.FocusInput(); break;
			}
		}

		/// <summary>Execute any special behaviour</summary>
		private void PerformSpecial()
		{
			switch (m_special)
			{
			default: Debug.Assert(false, "Unknown special behaviour"); break;
			case ESpecial.None: break;
			case ESpecial.ShowLineEndingTip:
				{
					Misc.ShowHint(m_tb_line_ends,
						"Set the line ending characters to expect in the log data.\r\n"+
						"Use '<CR>' for carriage return, '<LF>' for line feed, '<TAB>' for tab characters.\r\n"+
						"Specify UNICODE characters using the form \\uXXXX\r\n"+
						"Leave blank to auto detect", 7000);
					break;
				}
			case ESpecial.ShowColumnDelimiterTip:
				{
					Misc.ShowHint(m_tb_col_delims, "Multi-column mode is enabled when a column delimiter is given here");
					break;
				}
			}
			m_special = ESpecial.None;
		}

		/// <summary>Set appropriate changed flags when a grid is changed</summary>
		private void FlagAsChanged(DataGridView grid)
		{
			if      (grid == m_grid_highlight) { HighlightsChanged = true; WhatsChanged |= EWhatsChanged.Rendering; }
			else if (grid == m_grid_filter   ) { FiltersChanged    = true; WhatsChanged |= EWhatsChanged.FileParsing; }
			else if (grid == m_grid_transform) { TransformsChanged = true; WhatsChanged |= EWhatsChanged.FileParsing; }
			else if (grid == m_grid_action   ) { ActionsChanged    = true; WhatsChanged |= EWhatsChanged.Nothing; }
		}

		/// <summary>Delete a pattern</summary>
		private void OnDeletingRow<TPattern>(DataGridView grid, IList<TPattern> patterns, IPatternUI pattern_ui, int index) where TPattern:IPattern,new()
		{
			// Note, don't update the grid here or it causes an ArgumentOutOfRange exception.
			// Other stuff must be using the grid row that will be deleted.

			// If the deleted row is currently being edited, remove it from the editor first
			if (ReferenceEquals(pattern_ui.Original, patterns[index]))
				pattern_ui.NewPattern(new TPattern());

			patterns.RemoveAt(index);
			FlagAsChanged(grid);

			if (typeof(TPattern) == typeof(Filter))
				m_chk_reject_all_by_default.Checked = Settings.Patterns.Filters.Contains(Filter.RejectAll);
		}

		/// <summary>OnDragDrop functionality for grid rows</summary>
		private void DoDragDrop<T>(DataGridView grid, IList<T> patterns, DragEventArgs args, bool test_can_drop)
		{
			args.Effect = DragDropEffects.None;
			if (!args.Data.GetDataPresent(typeof(T))) return;
			var pt = grid.PointToClient(new Point(args.X, args.Y));
			var hit = grid.HitTest(pt.X, pt.Y);
			if (hit.Type != DataGridViewHitTestType.RowHeader || hit.RowIndex < 0 || hit.RowIndex >= patterns.Count) return;
			args.Effect = args.AllowedEffect;
			if (test_can_drop) return;

			// Swap the rows
			var pat = (T)args.Data.GetData(typeof(T));
			var idx1 = patterns.IndexOf(pat);
			var idx2 = hit.RowIndex;
			((IList)patterns).Swap(idx1, idx2);
			grid.InvalidateRow(idx1);
			grid.InvalidateRow(idx2);

			FlagAsChanged(grid);
		}

		/// <summary>Handle mouse down on the patterns grid</summary>
		private static void OnMouseDown<T>(DataGridView grid, IList<T> patterns, MouseEventArgs e)
		{
			var hit = grid.HitTest(e.X, e.Y);
			if (hit.Type == DataGridViewHitTestType.RowHeader && hit.RowIndex >= 0 && hit.RowIndex < patterns.Count)
			{
				grid.DoDragDrop(patterns[hit.RowIndex], DragDropEffects.Move);
			}
		}

		/// <summary>Provide cells for the grid</summary>
		private static void OnCellValueNeeded<T>(DataGridView grid, IList<T> patterns, DataGridViewCellValueEventArgs e) where T: class, IPattern
		{
			if (e.RowIndex < 0 || e.RowIndex >= patterns.Count) { e.Value = ""; return; }
			var cell = grid[e.ColumnIndex, e.RowIndex];
			var pat = patterns[e.RowIndex];
			var pt = pat as Pattern;
			var hl = pat as Highlight;
			var ft = pat as Filter;
			var ac = pat as ClkAction;

			switch (grid.Columns[e.ColumnIndex].Name)
			{
			default: e.Value = string.Empty; break;
			case ColumnNames.Pattern:
				string val = pat.Expr;
				if (string.IsNullOrEmpty(val)) val = "<blank>";
				if (string.IsNullOrWhiteSpace(val)) val = "'"+val+"'";
				if (pt != null && pt.Invert) val = "not "+val;
				e.Value = val;
				break;
			case ColumnNames.Colours:
				e.Value = "Click to change colours";
				cell.Style.BackColor = cell.Style.SelectionBackColor = hl != null ? Gfx_.Lerp(Color.FromArgb(255, hl.BackColour), Color.White, 1f - hl.BackColour.A/255f) : Color.White;
				cell.Style.ForeColor = cell.Style.SelectionForeColor = hl != null ? hl.ForeColour : Color.White;
				break;
			case ColumnNames.Behaviour:
				if (ft != null) switch (ft.IfMatch) {
				case EIfMatch.Keep:   e.Value = "Keep"; break;
				case EIfMatch.Reject: e.Value = "Reject"; break;
				}
				break;
			case ColumnNames.Highlight:
				if (hl != null) {
				e.Value = hl.BinaryMatch ? "Full" : "Partial";
				}
				break;
			case ColumnNames.ClickAction:
				e.Value = ac != null && !string.IsNullOrEmpty(ac.Executable) ? ac.ActionString : "Click here to modify action";
				break;
			}
		}

		/// <summary>Handle cell clicks</summary>
		private void OnCellClick<T>(DataGridView grid, IList<T> patterns, IPatternUI ctrl, DataGridViewCellEventArgs e) where T: class, IPattern
		{
			if (e.RowIndex    < 0 || e.RowIndex    >= patterns.Count  ) return;
			if (e.RowIndex    < 0 || e.RowIndex    >= grid.RowCount   ) return;
			if (e.ColumnIndex < 0 || e.ColumnIndex >= grid.ColumnCount) return;
			T pat = patterns[e.RowIndex];
			var hl = pat as Highlight;
			var ft = pat as Filter;
			var ac = pat as ClkAction;

			switch (grid.Columns[e.ColumnIndex].Name)
			{
			default: return;
			case ColumnNames.Active:
				pat.Active = !pat.Active;
				break;
			case ColumnNames.Edit:
				ctrl.EditPattern(patterns[e.RowIndex]);
				break;
			case ColumnNames.Highlight:
				if (hl != null)
				{
					hl.BinaryMatch = !hl.BinaryMatch;
				}
				break;
			case ColumnNames.Colours:
				if (hl != null)
				{
					var dlg = new ColourPickerUI(hl.ForeColour, hl.BackColour);
					if (dlg.ShowDialog(this) != DialogResult.OK) return;
					hl.ForeColour = dlg.FontColor;
					hl.BackColour = dlg.BkgdColor;
					grid.InvalidateCell(e.ColumnIndex, e.RowIndex);
				}
				break;
			case ColumnNames.Behaviour:
				if (ft != null)
				{
					ft.IfMatch = Enum<EIfMatch>.Cycle(ft.IfMatch);
				}
				break;
			case ColumnNames.ClickAction:
				if (ac != null)
				{
					var dg = new ClkActionUI(ac);
					if (dg.ShowDialog(this) != DialogResult.OK) break;
					ac.Executable       = dg.Action.Executable;
					ac.Arguments        = dg.Action.Arguments;
					ac.WorkingDirectory = dg.Action.WorkingDirectory;
				}
				break;
			}

			//// Context menu for the grid
			//var menu = new ContextMenuStrip();
			//menu.Items.Add("Delete pattern", null, (ss,aa) => patterns.RemoveAt(e.RowIndex));
			//menu.Show(pt);

			FlagAsChanged(grid);
		}

		/// <summary>Double click edits the pattern</summary>
		private void OnCellDoubleClick<T>(DataGridView grid, IList<T> patterns, IPatternUI ctrl, DataGridViewCellEventArgs e) where T:IPattern
		{
			if (e.RowIndex    < 0 || e.RowIndex    >= patterns.Count  ) return;
			if (e.RowIndex    < 0 || e.RowIndex    >= grid.RowCount   ) return;
			if (e.ColumnIndex < 0 || e.ColumnIndex >= grid.ColumnCount) return;
			var pat = patterns[e.RowIndex];

			ctrl.EditPattern(pat);
			FlagAsChanged(grid);
		}

		/// <summary>Cell formatting...</summary>
		private static void OnCellFormatting<T>(DataGridView grid, IList<T> patterns, DataGridViewCellFormattingEventArgs e) where T:IPattern
		{
			if (e.RowIndex    < 0 || e.RowIndex    >= patterns.Count  ) return;
			if (e.RowIndex    < 0 || e.RowIndex    >= grid.RowCount   ) return;
			if (e.ColumnIndex < 0 || e.ColumnIndex >= grid.ColumnCount) return;
			var pat = patterns[e.RowIndex];

			switch (grid.Columns[e.ColumnIndex].Name)
			{
			default: return;
			case ColumnNames.Active:
				e.Value = pat.Active ? Resources.pattern_active : Resources.pattern_inactive;
				break;
			case ColumnNames.Edit:
				e.Value = Resources.pencil;
				break;
			}
		}

		/// <summary>Colour picker helper</summary>
		private Color PickColour(Color current)
		{
			var d = new ColorDialog{AllowFullOpen = true, AnyColor = true, Color = current};
			return d.ShowDialog(this) == DialogResult.OK ? d.Color : current;
		}

		/// <summary>Show a hint balloon for situations that users might find confusing but are still valid</summary>
		private void HeuristicHints()
		{
			// Show a hint if filters are active, the file isn't empty, but there are no visible rows
			if (m_last_hint != EHeuristicHint.ColumnDelims &&
				Settings.ColDelimiter.Length != 0 &&
				Settings.ColumnCount == 1)
			{
				Misc.ShowHint(m_spinner_column_count, "Set maximum columns here");
				m_last_hint = EHeuristicHint.ColumnDelims;
			}
		}
		private enum EHeuristicHint { None, ColumnDelims }
		private EHeuristicHint m_last_hint;

		public enum ETab
		{
			General    = 0,
			LogView    = 1,
			Highlights = 2,
			Filters    = 3,
			Transforms = 4,
			Actions    = 5,
		}
		public enum ESpecial
		{
			None,
			ShowLineEndingTip,      // Show a tip balloon over the line ending field
			ShowColumnDelimiterTip, // Show a tip balloon over the column delimiter UI
		}

		private static class ColumnNames
		{
			public const string Active       = "Active";
			public const string Pattern      = "Pattern";
			public const string Edit         = "Edit";
			public const string Colours      = "Colours";
			public const string Highlight    = "Highlight";
			public const string Behaviour    = "IfMatch";
			public const string ClickAction  = "ClickAction";
		}
		private static class ColumnTT
		{
			public const string Active      = "Enable or disable the pattern";
			public const string Pattern     = "The pattern used to match lines in the log data";
			public const string Edit        = "Click to edit the pattern in the editor";
			public const string Colours     = "The colours to use when highlighting lines for the highlight pattern";
			public const string Highlight   = "Highlight the full row, or just the parts that match the highlight pattern";
			public const string Behaviour   = "Whether to keep or remove the lines that match this filter pattern";
			public const string ClickAction = "The command line executed when a row matching the pattern is double clicked";
		}

		/// <summary>A highlighting count limiter for when the settings dialog is displayed</summary>
		private class SettingsHighlightingCountLimiter :HighlightingCountLimiter
		{
			private readonly SettingsUI m_ui;
			public SettingsHighlightingCountLimiter(Main main, SettingsUI ui)
				:base(main)
			{
				m_ui = ui;
			}

			/// <summary>True if the licensed feature is still currently in use</summary>
			public override bool FeatureInUse
			{
				get { return m_ui.Settings.Patterns.Highlights.Count > FreeEditionLimits.MaxHighlights || base.FeatureInUse; }
			}

			/// <summary>Called to stop the use of the feature</summary>
			public override void CloseFeature()
			{
				m_ui.Settings.Patterns.Highlights.RemoveToEnd(FreeEditionLimits.MaxHighlights);
				m_ui.UpdateUI();
				base.CloseFeature();
			}
		}

		/// <summary>A filtering count limiter for when the settings dialog is displayed</summary>
		private class SettingsFilteringCountLimiter :FilteringCountLimiter
		{
			private readonly SettingsUI m_ui;
			public SettingsFilteringCountLimiter(Main main, SettingsUI ui)
				:base(main)
			{
				m_ui = ui;
			}

			/// <summary>True if the licensed feature is still currently in use</summary>
			public override bool FeatureInUse
			{
				get { return m_ui.Settings.Patterns.Filters.Count > FreeEditionLimits.MaxFilters || base.FeatureInUse; }
			}

			/// <summary>Called to stop the use of the feature</summary>
			public override void CloseFeature()
			{
				m_ui.Settings.Patterns.Filters.RemoveToEnd(FreeEditionLimits.MaxFilters);
				m_ui.UpdateUI();
				base.CloseFeature();
			}
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
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
			this.m_tb_web_proxy_host = new Rylogic.Gui.WinForms.ValueBox();
			this.m_chk_use_web_proxy = new System.Windows.Forms.CheckBox();
			this.m_chk_c4u = new System.Windows.Forms.CheckBox();
			this.m_chk_show_totd = new System.Windows.Forms.CheckBox();
			this.m_chk_save_screen_loc = new System.Windows.Forms.CheckBox();
			this.m_chk_load_last_file = new System.Windows.Forms.CheckBox();
			this.m_group_grid = new System.Windows.Forms.GroupBox();
			this.m_lbl_max_line_len_kb = new System.Windows.Forms.Label();
			this.m_spinner_max_line_length = new System.Windows.Forms.NumericUpDown();
			this.m_lbl_max_line_length = new System.Windows.Forms.Label();
			this.m_lbl_max_scan_size1 = new System.Windows.Forms.Label();
			this.m_lbl_history_length1 = new System.Windows.Forms.Label();
			this.m_spinner_line_cache_count = new System.Windows.Forms.NumericUpDown();
			this.m_spinner_max_mem_range = new System.Windows.Forms.NumericUpDown();
			this.m_lbl_max_scan_size0 = new System.Windows.Forms.Label();
			this.m_chk_file_changes_additive = new System.Windows.Forms.CheckBox();
			this.m_chk_open_at_end = new System.Windows.Forms.CheckBox();
			this.m_lbl_history_length0 = new System.Windows.Forms.Label();
			this.m_table_general2 = new System.Windows.Forms.TableLayoutPanel();
			this.m_group_line_ends = new System.Windows.Forms.GroupBox();
			this.m_lbl_column_count = new System.Windows.Forms.Label();
			this.m_spinner_column_count = new System.Windows.Forms.NumericUpDown();
			this.m_chk_ignore_blank_lines = new System.Windows.Forms.CheckBox();
			this.m_tb_col_delims = new Rylogic.Gui.WinForms.ValueBox();
			this.m_lbl_col_delims = new System.Windows.Forms.Label();
			this.m_tb_line_ends = new Rylogic.Gui.WinForms.ValueBox();
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
			this.m_pattern_hl = new Rylogic.Gui.WinForms.PatternUI();
			this.m_table_hl = new System.Windows.Forms.TableLayoutPanel();
			this.m_grid_highlight = new RyLogViewer.DataGridView();
			this.label2 = new System.Windows.Forms.Label();
			this.m_tab_filter = new System.Windows.Forms.TabPage();
			this.m_split_ft = new System.Windows.Forms.SplitContainer();
			this.m_pattern_ft = new Rylogic.Gui.WinForms.PatternUI();
			this.m_table_ft = new System.Windows.Forms.TableLayoutPanel();
			this.m_lbl_ft_grid_desc = new System.Windows.Forms.Label();
			this.m_grid_filter = new RyLogViewer.DataGridView();
			this.m_chk_reject_all_by_default = new System.Windows.Forms.CheckBox();
			this.m_tab_transform = new System.Windows.Forms.TabPage();
			this.m_split_tx = new System.Windows.Forms.SplitContainer();
			this.m_pattern_tx = new RyLogViewer.TransformUI();
			this.m_table_tx = new System.Windows.Forms.TableLayoutPanel();
			this.label4 = new System.Windows.Forms.Label();
			this.m_grid_transform = new RyLogViewer.DataGridView();
			this.m_tab_action = new System.Windows.Forms.TabPage();
			this.m_split_ac = new System.Windows.Forms.SplitContainer();
			this.m_pattern_ac = new Rylogic.Gui.WinForms.PatternUI();
			this.m_table_ac = new System.Windows.Forms.TableLayoutPanel();
			this.label5 = new System.Windows.Forms.Label();
			this.m_grid_action = new RyLogViewer.DataGridView();
			this.m_image_list = new System.Windows.Forms.ImageList(this.components);
			this.m_tt = new System.Windows.Forms.ToolTip(this.components);
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
			this.m_text_settings.TabIndex = 0;
			// 
			// m_btn_settings_reset
			// 
			this.m_btn_settings_reset.Location = new System.Drawing.Point(8, 40);
			this.m_btn_settings_reset.Name = "m_btn_settings_reset";
			this.m_btn_settings_reset.Size = new System.Drawing.Size(122, 23);
			this.m_btn_settings_reset.TabIndex = 1;
			this.m_btn_settings_reset.Text = "Reset to Defaults";
			this.m_btn_settings_reset.UseVisualStyleBackColor = true;
			// 
			// m_btn_settings_save
			// 
			this.m_btn_settings_save.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_settings_save.Location = new System.Drawing.Point(410, 40);
			this.m_btn_settings_save.Name = "m_btn_settings_save";
			this.m_btn_settings_save.Size = new System.Drawing.Size(72, 23);
			this.m_btn_settings_save.TabIndex = 3;
			this.m_btn_settings_save.Text = "Save As";
			this.m_btn_settings_save.UseVisualStyleBackColor = true;
			// 
			// m_btn_settings_load
			// 
			this.m_btn_settings_load.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_settings_load.Location = new System.Drawing.Point(332, 40);
			this.m_btn_settings_load.Name = "m_btn_settings_load";
			this.m_btn_settings_load.Size = new System.Drawing.Size(72, 23);
			this.m_btn_settings_load.TabIndex = 2;
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
			this.m_group_startup.Controls.Add(this.m_tb_web_proxy_host);
			this.m_group_startup.Controls.Add(this.m_chk_use_web_proxy);
			this.m_group_startup.Controls.Add(this.m_chk_c4u);
			this.m_group_startup.Controls.Add(this.m_chk_show_totd);
			this.m_group_startup.Controls.Add(this.m_chk_save_screen_loc);
			this.m_group_startup.Controls.Add(this.m_chk_load_last_file);
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
			// m_tb_web_proxy_host
			// 
			this.m_tb_web_proxy_host.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_web_proxy_host.BackColorValid = System.Drawing.Color.White;
			this.m_tb_web_proxy_host.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_web_proxy_host.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_web_proxy_host.Location = new System.Drawing.Point(13, 149);
			this.m_tb_web_proxy_host.Name = "m_tb_web_proxy_host";
			this.m_tb_web_proxy_host.Size = new System.Drawing.Size(125, 20);
			this.m_tb_web_proxy_host.TabIndex = 5;
			this.m_tb_web_proxy_host.UseValidityColours = true;
			this.m_tb_web_proxy_host.Value = null;
			// 
			// m_chk_use_web_proxy
			// 
			this.m_chk_use_web_proxy.AutoSize = true;
			this.m_chk_use_web_proxy.Location = new System.Drawing.Point(14, 111);
			this.m_chk_use_web_proxy.Name = "m_chk_use_web_proxy";
			this.m_chk_use_web_proxy.Size = new System.Drawing.Size(96, 17);
			this.m_chk_use_web_proxy.TabIndex = 4;
			this.m_chk_use_web_proxy.Text = "Use web proxy";
			this.m_chk_use_web_proxy.UseVisualStyleBackColor = true;
			// 
			// m_chk_c4u
			// 
			this.m_chk_c4u.AutoSize = true;
			this.m_chk_c4u.Location = new System.Drawing.Point(14, 88);
			this.m_chk_c4u.Name = "m_chk_c4u";
			this.m_chk_c4u.Size = new System.Drawing.Size(115, 17);
			this.m_chk_c4u.TabIndex = 3;
			this.m_chk_c4u.Text = "Check for Updates";
			this.m_chk_c4u.UseVisualStyleBackColor = true;
			// 
			// m_chk_show_totd
			// 
			this.m_chk_show_totd.AutoSize = true;
			this.m_chk_show_totd.Location = new System.Drawing.Point(14, 65);
			this.m_chk_show_totd.Name = "m_chk_show_totd";
			this.m_chk_show_totd.Size = new System.Drawing.Size(127, 17);
			this.m_chk_show_totd.TabIndex = 2;
			this.m_chk_show_totd.Text = "Show \'Tip of the Day\'";
			this.m_chk_show_totd.UseVisualStyleBackColor = true;
			// 
			// m_chk_save_screen_loc
			// 
			this.m_chk_save_screen_loc.AutoSize = true;
			this.m_chk_save_screen_loc.Location = new System.Drawing.Point(14, 42);
			this.m_chk_save_screen_loc.Name = "m_chk_save_screen_loc";
			this.m_chk_save_screen_loc.Size = new System.Drawing.Size(180, 17);
			this.m_chk_save_screen_loc.TabIndex = 1;
			this.m_chk_save_screen_loc.Text = "Restore previous screen position";
			this.m_chk_save_screen_loc.UseVisualStyleBackColor = true;
			// 
			// m_chk_load_last_file
			// 
			this.m_chk_load_last_file.AutoSize = true;
			this.m_chk_load_last_file.Location = new System.Drawing.Point(14, 19);
			this.m_chk_load_last_file.Name = "m_chk_load_last_file";
			this.m_chk_load_last_file.Size = new System.Drawing.Size(85, 17);
			this.m_chk_load_last_file.TabIndex = 0;
			this.m_chk_load_last_file.Text = "Load last file";
			this.m_chk_load_last_file.UseVisualStyleBackColor = true;
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
			this.m_group_grid.Controls.Add(this.m_chk_file_changes_additive);
			this.m_group_grid.Controls.Add(this.m_chk_open_at_end);
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
			this.m_spinner_max_line_length.TabIndex = 2;
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
			// m_chk_file_changes_additive
			// 
			this.m_chk_file_changes_additive.AutoSize = true;
			this.m_chk_file_changes_additive.Location = new System.Drawing.Point(13, 130);
			this.m_chk_file_changes_additive.Name = "m_chk_file_changes_additive";
			this.m_chk_file_changes_additive.Size = new System.Drawing.Size(181, 17);
			this.m_chk_file_changes_additive.TabIndex = 4;
			this.m_chk_file_changes_additive.Text = "Assume file changes are additive";
			this.m_chk_file_changes_additive.UseVisualStyleBackColor = true;
			// 
			// m_chk_open_at_end
			// 
			this.m_chk_open_at_end.AutoSize = true;
			this.m_chk_open_at_end.Location = new System.Drawing.Point(14, 106);
			this.m_chk_open_at_end.Name = "m_chk_open_at_end";
			this.m_chk_open_at_end.Size = new System.Drawing.Size(124, 17);
			this.m_chk_open_at_end.TabIndex = 3;
			this.m_chk_open_at_end.Text = "Open files at the end";
			this.m_chk_open_at_end.UseVisualStyleBackColor = true;
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
			this.m_group_line_ends.Controls.Add(this.m_chk_ignore_blank_lines);
			this.m_group_line_ends.Controls.Add(this.m_tb_col_delims);
			this.m_group_line_ends.Controls.Add(this.m_lbl_col_delims);
			this.m_group_line_ends.Controls.Add(this.m_tb_line_ends);
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
			// m_chk_ignore_blank_lines
			// 
			this.m_chk_ignore_blank_lines.AutoSize = true;
			this.m_chk_ignore_blank_lines.Location = new System.Drawing.Point(12, 19);
			this.m_chk_ignore_blank_lines.Name = "m_chk_ignore_blank_lines";
			this.m_chk_ignore_blank_lines.Size = new System.Drawing.Size(109, 17);
			this.m_chk_ignore_blank_lines.TabIndex = 0;
			this.m_chk_ignore_blank_lines.Text = "Ignore blank lines";
			this.m_chk_ignore_blank_lines.UseVisualStyleBackColor = true;
			// 
			// m_tb_col_delims
			// 
			this.m_tb_col_delims.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_col_delims.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_col_delims.BackColorValid = System.Drawing.Color.White;
			this.m_tb_col_delims.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_col_delims.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_col_delims.Location = new System.Drawing.Point(108, 73);
			this.m_tb_col_delims.Name = "m_tb_col_delims";
			this.m_tb_col_delims.Size = new System.Drawing.Size(131, 20);
			this.m_tb_col_delims.TabIndex = 2;
			this.m_tb_col_delims.UseValidityColours = true;
			this.m_tb_col_delims.Value = null;
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
			// m_tb_line_ends
			// 
			this.m_tb_line_ends.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_line_ends.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_line_ends.BackColorValid = System.Drawing.Color.White;
			this.m_tb_line_ends.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_line_ends.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_line_ends.Location = new System.Drawing.Point(108, 45);
			this.m_tb_line_ends.Name = "m_tb_line_ends";
			this.m_tb_line_ends.Size = new System.Drawing.Size(131, 20);
			this.m_tb_line_ends.TabIndex = 1;
			this.m_tb_line_ends.UseValidityColours = true;
			this.m_tb_line_ends.Value = null;
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
			this.m_lbl_line2_example.Size = new System.Drawing.Size(165, 21);
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
			this.m_lbl_line1_example.Size = new System.Drawing.Size(215, 21);
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
			this.m_lbl_selection_example.Size = new System.Drawing.Size(215, 21);
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
			this.m_lbl_fs_edit_cached_colour.TabIndex = 1;
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
			this.m_lbl_fs_edit_bookmark_colour.TabIndex = 3;
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
			this.m_lbl_fs_edit_visible_colour.TabIndex = 2;
			this.m_lbl_fs_edit_visible_colour.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// m_spinner_file_scroll_width
			// 
			this.m_spinner_file_scroll_width.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_spinner_file_scroll_width.Location = new System.Drawing.Point(98, 17);
			this.m_spinner_file_scroll_width.Name = "m_spinner_file_scroll_width";
			this.m_spinner_file_scroll_width.Size = new System.Drawing.Size(55, 20);
			this.m_spinner_file_scroll_width.TabIndex = 0;
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
			this.m_table_hl.ColumnCount = 1;
			this.m_table_hl.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table_hl.Controls.Add(this.m_grid_highlight, 0, 1);
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
			this.m_grid_highlight.Location = new System.Drawing.Point(0, 32);
			this.m_grid_highlight.Margin = new System.Windows.Forms.Padding(0);
			this.m_grid_highlight.MultiSelect = false;
			this.m_grid_highlight.Name = "m_grid_highlight";
			this.m_grid_highlight.RowHeadersWidth = 24;
			this.m_grid_highlight.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid_highlight.Size = new System.Drawing.Size(498, 216);
			this.m_grid_highlight.TabIndex = 0;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(3, 0);
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
			this.m_split_ft.SplitterDistance = 176;
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
			this.m_pattern_ft.Size = new System.Drawing.Size(498, 176);
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
			this.m_table_ft.Controls.Add(this.m_chk_reject_all_by_default, 1, 0);
			this.m_table_ft.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_table_ft.Location = new System.Drawing.Point(0, 0);
			this.m_table_ft.Margin = new System.Windows.Forms.Padding(0);
			this.m_table_ft.Name = "m_table_ft";
			this.m_table_ft.RowCount = 3;
			this.m_table_ft.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table_ft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table_ft.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table_ft.Size = new System.Drawing.Size(498, 248);
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
			this.m_grid_filter.Size = new System.Drawing.Size(498, 216);
			this.m_grid_filter.TabIndex = 0;
			// 
			// m_chk_reject_all_by_default
			// 
			this.m_chk_reject_all_by_default.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.m_chk_reject_all_by_default.AutoSize = true;
			this.m_chk_reject_all_by_default.Location = new System.Drawing.Point(351, 7);
			this.m_chk_reject_all_by_default.Name = "m_chk_reject_all_by_default";
			this.m_chk_reject_all_by_default.Size = new System.Drawing.Size(144, 17);
			this.m_chk_reject_all_by_default.TabIndex = 4;
			this.m_chk_reject_all_by_default.Text = "Reject all rows by default";
			this.m_chk_reject_all_by_default.UseVisualStyleBackColor = true;
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
			this.m_split_tx.SplitterDistance = 176;
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
			this.m_pattern_tx.Size = new System.Drawing.Size(498, 176);
			this.m_pattern_tx.TabIndex = 0;
			this.m_pattern_tx.TestText = "Enter text here to test your pattern";
			this.m_pattern_tx.Touched = false;
			// 
			// m_table_tx
			// 
			this.m_table_tx.ColumnCount = 1;
			this.m_table_tx.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table_tx.Controls.Add(this.label4, 0, 0);
			this.m_table_tx.Controls.Add(this.m_grid_transform, 0, 1);
			this.m_table_tx.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_table_tx.Location = new System.Drawing.Point(0, 0);
			this.m_table_tx.Margin = new System.Windows.Forms.Padding(0);
			this.m_table_tx.Name = "m_table_tx";
			this.m_table_tx.RowCount = 3;
			this.m_table_tx.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table_tx.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table_tx.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table_tx.Size = new System.Drawing.Size(498, 248);
			this.m_table_tx.TabIndex = 3;
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(3, 0);
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
			this.m_grid_transform.Location = new System.Drawing.Point(0, 32);
			this.m_grid_transform.Margin = new System.Windows.Forms.Padding(0);
			this.m_grid_transform.MultiSelect = false;
			this.m_grid_transform.Name = "m_grid_transform";
			this.m_grid_transform.RowHeadersWidth = 24;
			this.m_grid_transform.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid_transform.Size = new System.Drawing.Size(498, 216);
			this.m_grid_transform.TabIndex = 0;
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
			this.m_split_ac.SplitterDistance = 176;
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
			this.m_pattern_ac.Size = new System.Drawing.Size(498, 176);
			this.m_pattern_ac.TabIndex = 0;
			this.m_pattern_ac.TestText = "Enter text here to test your pattern";
			this.m_pattern_ac.Touched = false;
			// 
			// m_table_ac
			// 
			this.m_table_ac.ColumnCount = 1;
			this.m_table_ac.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table_ac.Controls.Add(this.label5, 0, 0);
			this.m_table_ac.Controls.Add(this.m_grid_action, 0, 1);
			this.m_table_ac.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_table_ac.Location = new System.Drawing.Point(0, 0);
			this.m_table_ac.Margin = new System.Windows.Forms.Padding(0);
			this.m_table_ac.Name = "m_table_ac";
			this.m_table_ac.RowCount = 3;
			this.m_table_ac.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table_ac.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table_ac.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table_ac.Size = new System.Drawing.Size(498, 248);
			this.m_table_ac.TabIndex = 2;
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(3, 0);
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
			this.m_grid_action.Location = new System.Drawing.Point(0, 32);
			this.m_grid_action.Margin = new System.Windows.Forms.Padding(0);
			this.m_grid_action.MultiSelect = false;
			this.m_grid_action.Name = "m_grid_action";
			this.m_grid_action.RowHeadersWidth = 24;
			this.m_grid_action.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid_action.Size = new System.Drawing.Size(498, 216);
			this.m_grid_action.TabIndex = 0;
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
	}
}
