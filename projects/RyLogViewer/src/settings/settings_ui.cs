using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using pr.extn;
using pr.gui;
using pr.maths;
using pr.util;
using RyLogViewer.Properties;

namespace RyLogViewer
{
	public partial class SettingsUI :Form
	{
		private readonly Main        m_main;         // The main app
		private readonly Settings    m_settings;     // The app settings changed by this UI
		private readonly ToolTip     m_tt;           // Tooltips
		private readonly ToolTip     m_balloon;      // Balloon hits
		private readonly HoverScroll m_hover_scroll; // Hover scroll for the pattern grid
		private List<Highlight>      m_highlights;   // The highlight patterns currently in the grid
		private List<Filter>         m_filters;      // The filter patterns currently in the grid
		private List<Transform>      m_transforms;   // The transforms currently in the grid
		private List<ClkAction>      m_actions;      // The actions currently in the grid

		public enum ETab
		{
			General    = 0,
			LogView    = 1,
			Highlights = 2,
			Filters    = 3,
			Transforms = 4,
			Actions    = 5,
		}
		private static class ColumnNames
		{
			public const string Active       = "Active";
			public const string Pattern      = "Pattern";
			public const string Modify       = "Modify";
			public const string Highlighting = "Highlighting";
			public const string Behaviour    = "IfMatch";
			public const string ClickAction  = "ClickAction";
		}

		public enum ESpecial
		{
			None,

			// Show a tip balloon over the line ending field
			ShowLineEndingTip,
		}
		private ESpecial m_special;

		/// <summary>Returns a bit mask of the settings data that's changed</summary>
		public EWhatsChanged WhatsChanged { get; private set; }
		
		/// <summary>Access to the highlight pattern ui</summary>
		public PatternUI HighlightUI { get { return m_pattern_hl; } }
		public bool HighlightsChanged { get; set; }

		/// <summary>Access to the filter pattern ui</summary>
		public PatternUI FilterUI { get { return m_pattern_ft; } }
		public bool FiltersChanged { get; set; }

		/// <summary>Access to the transform pattern ui</summary>
		public TransformUI TransformUI { get { return m_pattern_tx; } }
		public bool TransformsChanged { get; set; }
		
		/// <summary>Access to the action pattern ui</summary>
		public PatternUI ActionUI { get { return m_pattern_ac; } }
		public bool ActionsChanged { get; set; }

		public SettingsUI(Main main, Settings settings, ETab tab, ESpecial special = ESpecial.None)
		{
			InitializeComponent();
			KeyPreview     = true;
			m_main         = main;
			m_settings     = settings;
			m_special      = special;
			m_tt           = new ToolTip();
			m_balloon      = new ToolTip{IsBalloon = true,UseFading = true,ReshowDelay = 0};
			m_hover_scroll = new HoverScroll();
			ReadSettings();

			m_tabctrl.SelectedIndex = (int)tab;
			m_pattern_hl.NewPattern(new Highlight());
			m_pattern_ft.NewPattern(new Filter()   );
			m_pattern_tx.NewPattern(new Transform());
			m_pattern_ac.NewPattern(new ClkAction());
			
			m_settings.SettingChanged += (s,a) => UpdateUI();
			m_tabctrl.Deselecting += (s,a) => TabChanging(a);
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

			Shown += (s,a) =>
				{
					FocusInput();
					PerformSpecial();
				};

			// Watch for unsaved changes on closing
			Closing += (s,a) =>
				{
					Focus(); // grab focus to ensure all controls persist their state
					SavePatternChanges(a); // Watch for unsaved changes
				};

			// Save on close
			Closed += (s,a) =>
				{
					Focus(); // grab focus to ensure all controls persist their state
					m_settings.HighlightPatterns = Highlight.Export(m_highlights);
					m_settings.FilterPatterns    = Filter   .Export(m_filters);
					m_settings.TransformPatterns = Transform.Export(m_transforms);
					m_settings.ActionPatterns    = ClkAction.Export(m_actions);

					m_main.UseLicensedFeature(FeatureName.Highlighting, new HighlightingCountLimiter(m_main, m_settings));
					m_main.UseLicensedFeature(FeatureName.Filtering   , new FilteringCountLimiter(m_main, m_settings));
				};
			
			UpdateUI();
			WhatsChanged = EWhatsChanged.Nothing;
		}

		/// <summary>Populate the internal lists from the settings data</summary>
		private void ReadSettings()
		{
			m_highlights = Highlight.Import(m_settings.HighlightPatterns);
			m_filters    = Filter   .Import(m_settings.FilterPatterns   );
			m_transforms = Transform.Import(m_settings.TransformPatterns);
			m_actions    = ClkAction.Import(m_settings.ActionPatterns   );
		}

		/// <summary>Hook up events for the general tab</summary>
		private void SetupGeneralTab()
		{
			string tt;

			// Load last file on startup
			m_check_load_last_file.ToolTip(m_tt, "Automatically load the last loaded file on startup");
			m_check_load_last_file.Checked = m_settings.LoadLastFile;
			m_check_load_last_file.Click += (s,a)=>
				{
					m_settings.LoadLastFile = m_check_load_last_file.Checked;
					WhatsChanged |= EWhatsChanged.StartupOptions;
				};
			
			// Restore window position on startup
			m_check_save_screen_loc.ToolTip(m_tt, "Restore the window to its last position on startup");
			m_check_save_screen_loc.Checked = m_settings.RestoreScreenLoc;
			m_check_save_screen_loc.Click += (s,a)=>
				{
					m_settings.RestoreScreenLoc = m_check_save_screen_loc.Checked;
					WhatsChanged |= EWhatsChanged.StartupOptions;
				};
			
			// Show tip of the day on startup
			m_check_show_totd.ToolTip(m_tt, "Show the 'Tip of the Day' dialog on startup");
			m_check_show_totd.Checked = m_settings.ShowTOTD;
			m_check_show_totd.Click += (s,a)=>
				{
					m_settings.ShowTOTD = m_check_show_totd.Checked;
					WhatsChanged |= EWhatsChanged.StartupOptions;
				};

			// Check for updates
			m_check_c4u.ToolTip(m_tt, "Check for newer versions on startup");
			m_check_c4u.Checked = m_settings.CheckForUpdates;
			m_check_c4u.Click += (s,a)=>
				{
					m_settings.CheckForUpdates = m_check_c4u.Checked;
					WhatsChanged |= EWhatsChanged.StartupOptions;
				};

			// Use web proxy
			m_check_use_web_proxy.ToolTip(m_tt, "Use a web proxy for internet connections (such as checking for updates)");
			m_check_use_web_proxy.Checked = m_settings.UseWebProxy;
			m_check_use_web_proxy.Click += (s,a)=>
				{
					m_settings.UseWebProxy = m_check_use_web_proxy.Checked;
					WhatsChanged |= EWhatsChanged.Nothing;
				};

			// Web proxy host
			tt = "The hostname or IP address of the web proxy server";
			m_lbl_web_proxy_host.ToolTip(m_tt, tt);
			m_edit_web_proxy_host.ToolTip(m_tt, tt);
			m_edit_web_proxy_host.Text = m_settings.WebProxyHost;
			m_edit_web_proxy_host.TextChanged += (s,a)=>
				{
					if (!((TextBox)s).Modified) return;
					m_settings.WebProxyHost = m_edit_web_proxy_host.Text;
					WhatsChanged |= EWhatsChanged.Nothing;
				};

			// Web proxy port
			tt = "The port number of the web proxy server";
			m_lbl_web_proxy_port.ToolTip(m_tt, tt);
			m_spinner_web_proxy_port.ToolTip(m_tt, tt);
			m_spinner_web_proxy_port.Minimum = Constants.PortNumberMin;
			m_spinner_web_proxy_port.Maximum = Constants.PortNumberMax;
			m_spinner_web_proxy_port.Value = Maths.Clamp(m_settings.WebProxyPort, Constants.PortNumberMin, Constants.PortNumberMax);
			m_spinner_web_proxy_port.ValueChanged += (s,a)=>
				{
					m_settings.WebProxyPort = (int)m_spinner_web_proxy_port.Value;
					WhatsChanged |= EWhatsChanged.Nothing;
				};

			// Line endings
			tt = "Set the line ending characters to expect in the log data.\r\nUse '<CR>' for carriage return, '<LF>' for line feed.\r\nLeave blank to auto detect";
			m_lbl_line_ends.ToolTip(m_tt, tt);
			m_edit_line_ends.ToolTip(m_tt, tt);
			m_edit_line_ends.Text = m_settings.RowDelimiter;
			m_edit_line_ends.TextChanged += (s,a)=>
			{
				if (!((TextBox)s).Modified) return;
				m_settings.RowDelimiter = m_edit_line_ends.Text;
				WhatsChanged |= EWhatsChanged.FileParsing;
			};
			
			// Column delimiters
			tt = "Set the characters that separate columns in the log data.\r\nUse '<TAB>' for a tab character.\r\nLeave blank for no column delimiter";
			m_lbl_col_delims.ToolTip(m_tt, tt);
			m_edit_col_delims.ToolTip(m_tt, tt);
			m_edit_col_delims.Text = m_settings.ColDelimiter;
			m_edit_col_delims.TextChanged += (s,a)=>
				{
					if (!((TextBox)s).Modified) return;
					m_settings.ColDelimiter = m_edit_col_delims.Text;
					WhatsChanged |= EWhatsChanged.FileParsing;
				};
			
			// Column Count
			tt = "The number of columns to display in the grid.\r\nUsed when the column delimiter is not blank";
			m_lbl_column_count.ToolTip(m_tt, tt);
			m_spinner_column_count.ToolTip(m_tt, tt);
			m_spinner_column_count.Minimum = Constants.ColumnCountMin;
			m_spinner_column_count.Maximum = Constants.ColumnCountMax;
			m_spinner_column_count.Value = Maths.Clamp(m_settings.ColumnCount, Constants.ColumnCountMin, Constants.ColumnCountMax);
			m_spinner_column_count.ValueChanged += (s,a)=>
				{
					m_settings.ColumnCount = (int)m_spinner_column_count.Value;
					WhatsChanged |= EWhatsChanged.Rendering;
				};

			// Include blank lines
			m_check_ignore_blank_lines.ToolTip(m_tt, "Ignore blank lines when loading the log file");
			m_check_ignore_blank_lines.Checked = m_settings.IgnoreBlankLines;
			m_check_ignore_blank_lines.CheckedChanged += (s,a)=>
				{
					m_settings.IgnoreBlankLines = m_check_ignore_blank_lines.Checked;
					WhatsChanged |= EWhatsChanged.FileParsing;
				};
			
			// Lines cached
			m_spinner_line_cache_count.ToolTip(m_tt, "The number of lines to scan into memory around the currently selected line");
			m_spinner_line_cache_count.Minimum = Constants.LineCacheCountMin;
			m_spinner_line_cache_count.Maximum = Constants.LineCacheCountMax;
			m_spinner_line_cache_count.Value = Maths.Clamp(m_settings.LineCacheCount, (int)m_spinner_line_cache_count.Minimum, (int)m_spinner_line_cache_count.Maximum);
			m_spinner_line_cache_count.ValueChanged += (s,a)=>
				{
					m_settings.LineCacheCount = (int)m_spinner_line_cache_count.Value;
					WhatsChanged |= EWhatsChanged.FileParsing;
				};
			
			// Max memory range
			tt = "The maximum number of bytes to scan when finding lines around the currently selected row (in MB).";
			m_lbl_max_scan_size0.ToolTip(m_tt, tt);
			m_spinner_max_mem_range.ToolTip(m_tt, tt);
			m_spinner_max_mem_range.Minimum = Constants.FileBufSizeMin / Constants.OneMB;
			m_spinner_max_mem_range.Maximum = Constants.FileBufSizeMax / Constants.OneMB;
			m_spinner_max_mem_range.Value = Maths.Clamp(m_settings.FileBufSize / Constants.OneMB, (int)m_spinner_max_mem_range.Minimum, (int)m_spinner_max_mem_range.Maximum);
			m_spinner_max_mem_range.ValueChanged += (s,a)=>
				{
					m_settings.FileBufSize = (int)m_spinner_max_mem_range.Value * Constants.OneMB;
					WhatsChanged |= EWhatsChanged.FileParsing;
				};
			
			// Max line length
			tt = "The maximum length of a line in the log file.\r\nIf the log contains lines longer than this an error will be reported when loading the file";
			m_lbl_max_line_len_kb.ToolTip(m_tt, tt);
			m_lbl_max_line_length.ToolTip(m_tt, tt);
			m_spinner_max_line_length.ToolTip(m_tt, tt);
			m_spinner_max_line_length.Minimum = Constants.MaxLineLengthMin / Constants.OneKB;
			m_spinner_max_line_length.Maximum = Constants.MaxLineLengthMax / Constants.OneKB;
			m_spinner_max_line_length.Value = Maths.Clamp(m_settings.MaxLineLength / Constants.OneKB, (int)m_spinner_max_line_length.Minimum, (int)m_spinner_max_line_length.Maximum);
			m_spinner_max_line_length.ValueChanged += (s,a)=>
				{
					m_settings.MaxLineLength = (int)m_spinner_max_line_length.Value * Constants.OneKB;
					WhatsChanged |= EWhatsChanged.FileParsing;
				};
			
			// Open at end
			m_check_open_at_end.ToolTip(m_tt, "If checked, opens files showing the end of the file.\r\nIf unchecked opens files at the beginning");
			m_check_open_at_end.Checked = m_settings.OpenAtEnd;
			m_check_open_at_end.CheckedChanged += (s,a)=>
				{
					m_settings.OpenAtEnd = m_check_open_at_end.Checked;
					WhatsChanged |= EWhatsChanged.FileOpenOptions;
				};
			
			// File changes additive
			m_check_file_changes_additive.ToolTip(m_tt, "Assume all changes to the watched file are additive only\r\nIf checked, reloading of changed files will not invalidate existing cached data");
			m_check_file_changes_additive.Checked = m_settings.FileChangesAdditive;
			m_check_file_changes_additive.CheckedChanged += (s,a)=>
				{
					m_settings.FileChangesAdditive = m_check_file_changes_additive.Checked;
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
						Filter = Resources.SettingsFileFilter,
						CheckFileExists = true,
						InitialDirectory = Path.GetDirectoryName(m_settings.Filepath)
					};
					if (dg.ShowDialog(this) != DialogResult.OK) return;
					m_settings.Filepath = dg.FileName;
					m_settings.Reload();
					ReadSettings();
					UpdateUI();
					WhatsChanged |= EWhatsChanged.Everything;
				};
			
			// Settings save
			m_btn_settings_save.ToolTip(m_tt, "Save current settings to a file");
			m_btn_settings_save.Click += (s,a)=>
				{
					var dg = new SaveFileDialog
						{
							Title = "Save current settings",
							Filter = Resources.SettingsFileFilter,
							CheckPathExists = true,
							InitialDirectory = Path.GetDirectoryName(m_settings.Filepath)
						};
					if (dg.ShowDialog(this) != DialogResult.OK) return;
					m_settings.Filepath = dg.FileName;
					m_settings.Save();
					UpdateUI();
					WhatsChanged |= EWhatsChanged.Nothing;
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
					var dlg = new ColourPickerUI {FontColor = m_settings.LineSelectForeColour, BkgdColor = m_settings.LineSelectBackColour};
					if (dlg.ShowDialog(this) != DialogResult.OK) return;
					m_settings.LineSelectForeColour = dlg.FontColor;
					m_settings.LineSelectBackColour = dlg.BkgdColor;
					WhatsChanged |= EWhatsChanged.Rendering;
				};
			
			// Log text colour
			m_lbl_line1_example.ToolTip(m_tt, "Set the foreground and background colours in the log view\r\nClick here to modify the colours");
			m_lbl_line1_example.MouseClick += (s,a)=>
				{
					var dlg = new ColourPickerUI {FontColor = m_settings.LineForeColour1, BkgdColor = m_settings.LineBackColour1};
					if (dlg.ShowDialog(this) != DialogResult.OK) return;
					m_settings.LineForeColour1 = dlg.FontColor;
					m_settings.LineBackColour1 = dlg.BkgdColor;
					WhatsChanged |= EWhatsChanged.Rendering;
				};
			
			// Alt log text colour
			m_lbl_line2_example.ToolTip(m_tt, "Set the foreground and background colours for odd numbered rows in the log view\r\nClick here to modify the colours");
			m_lbl_line2_example.MouseClick += (s,a)=>
				{
					var dlg = new ColourPickerUI {FontColor = m_settings.LineForeColour2, BkgdColor = m_settings.LineBackColour2};
					if (dlg.ShowDialog(this) != DialogResult.OK) return;
					m_settings.LineForeColour2 = dlg.FontColor;
					m_settings.LineBackColour2 = dlg.BkgdColor;
					WhatsChanged |= EWhatsChanged.Rendering;
				};
			
			// Enable alt line colours
			m_check_alternate_line_colour.ToolTip(m_tt, "Enable alternating colours in the log view");
			m_check_alternate_line_colour.CheckedChanged += (s,a)=>
				{
					m_settings.AlternateLineColours = m_check_alternate_line_colour.Checked;
					WhatsChanged |= EWhatsChanged.Rendering;
				};
			
			// Row height
			m_spinner_row_height.ToolTip(m_tt, "The height of each row in the log view");
			m_spinner_row_height.Minimum = Constants.RowHeightMinHeight;
			m_spinner_row_height.Maximum = Constants.RowHeightMaxHeight;
			m_spinner_row_height.Value = Maths.Clamp(m_settings.RowHeight, (int)m_spinner_row_height.Minimum, (int)m_spinner_row_height.Maximum);
			m_spinner_row_height.ValueChanged += (s,a)=>
				{
					m_settings.RowHeight = (int)m_spinner_row_height.Value;
					WhatsChanged |= EWhatsChanged.Rendering;
				};

			// Font 
			m_text_font.ToolTip(m_tt, "The font used to display the log file data");
			m_text_font.Text = string.Format("{0}, {1}pt" ,m_settings.Font.Name ,m_settings.Font.Size);
			m_text_font.Font = m_settings.Font;
			
			// Font button
			m_btn_change_font.ToolTip(m_tt, "Change the log view font");
			m_btn_change_font.Click += (s,a)=>
				{
					var dg = new FontDialog{Font = m_settings.Font};
					if (dg.ShowDialog(this) != DialogResult.OK) return;
					m_text_font.Font = dg.Font;
					m_settings.Font = dg.Font;
				};

			// File scroll width
			m_spinner_file_scroll_width.ToolTip(m_tt, "The width of the scroll bar that shows the current position within the log file");
			m_spinner_file_scroll_width.Minimum = Constants.FileScrollMinWidth;
			m_spinner_file_scroll_width.Maximum = Constants.FileScrollMaxWidth;
			m_spinner_file_scroll_width.Value = Maths.Clamp(m_settings.FileScrollWidth, (int)m_spinner_file_scroll_width.Minimum, (int)m_spinner_file_scroll_width.Maximum);
			m_spinner_file_scroll_width.ValueChanged += (s,a)=>
				{
					m_settings.FileScrollWidth = (int)m_spinner_file_scroll_width.Value;
					WhatsChanged |= EWhatsChanged.Rendering;
				};

			tt = "The colour representing the cached portion of the log file in the file scroll bar\r\nClick here to modify the colour";
			m_lbl_fs_cached_colour.ToolTip(m_tt, tt);
			m_lbl_fs_edit_cached_colour.ToolTip(m_tt, tt);
			m_lbl_fs_edit_cached_colour.BackColor = m_settings.ScrollBarCachedRangeColour;
			m_lbl_fs_edit_cached_colour.MouseClick += (s,a)=>
				{
					m_settings.ScrollBarCachedRangeColour = PickColour(m_settings.ScrollBarCachedRangeColour);
					m_lbl_fs_edit_cached_colour.BackColor = m_settings.ScrollBarCachedRangeColour;
					WhatsChanged |= EWhatsChanged.Rendering;
				};

			tt = "The colour representing the portion of the log file currently on screen\r\nClick here to modify the colour";
			m_lbl_fs_visible_colour.ToolTip(m_tt, tt);
			m_lbl_fs_edit_visible_colour.ToolTip(m_tt, tt);
			m_lbl_fs_edit_visible_colour.BackColor = m_settings.ScrollBarDisplayRangeColour;
			m_lbl_fs_edit_visible_colour.MouseClick += (s,a)=>
				{
					m_settings.ScrollBarDisplayRangeColour = PickColour(m_settings.ScrollBarDisplayRangeColour);
					m_lbl_fs_edit_visible_colour.BackColor = m_settings.ScrollBarDisplayRangeColour;
					WhatsChanged |= EWhatsChanged.Rendering;
				};

			tt = "The colour of the bookmarked locations\r\nClick here to modify the colour";
			m_lbl_fs_bookmark_colour.ToolTip(m_tt, tt);
			m_lbl_fs_edit_bookmark_colour.ToolTip(m_tt, tt);
			m_lbl_fs_edit_bookmark_colour.BackColor = m_settings.BookmarkColour;
			m_lbl_fs_edit_bookmark_colour.MouseClick += (s,a)=>
				{
					m_settings.BookmarkColour = PickColour(m_settings.BookmarkColour);
					m_lbl_fs_edit_bookmark_colour.BackColor = m_settings.BookmarkColour;
					WhatsChanged |= EWhatsChanged.Rendering;
				};
			
		}

		/// <summary>Hook up events for the highlights tab</summary>
		private void SetupHighlightTab()
		{
			var hl_style = new DataGridViewCellStyle
			{
				Font      = m_settings.Font,
				ForeColor = m_settings.LineForeColour1,
				BackColor = m_settings.LineBackColour1,
			};

			// Highlight grid
			m_grid_highlight.AllowDrop           = true;
			m_grid_highlight.VirtualMode         = true;
			m_grid_highlight.AutoGenerateColumns = false;
			m_grid_highlight.ColumnCount         = m_grid_highlight.RowCount = 0;
			m_grid_highlight.Columns.Add(new DataGridViewImageColumn   {Name = ColumnNames.Active       ,HeaderText = Resources.Active       ,FillWeight = 25  ,AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader ,ImageLayout = DataGridViewImageCellLayout.Zoom});
			m_grid_highlight.Columns.Add(new DataGridViewTextBoxColumn {Name = ColumnNames.Pattern      ,HeaderText = Resources.Pattern      ,FillWeight = 100 ,ReadOnly = true });
			m_grid_highlight.Columns.Add(new DataGridViewTextBoxColumn {Name = ColumnNames.Highlighting ,HeaderText = Resources.Highlighting ,FillWeight = 100 ,ReadOnly = true ,DefaultCellStyle = hl_style});
			m_grid_highlight.Columns.Add(new DataGridViewImageColumn   {Name = ColumnNames.Modify       ,HeaderText = Resources.Edit         ,FillWeight = 15  ,AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader ,ImageLayout = DataGridViewImageCellLayout.Zoom});
			m_grid_highlight.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
			m_grid_highlight.KeyDown          += DataGridView_Extensions.Copy;
			m_grid_highlight.KeyDown          += DataGridView_Extensions.SelectAll;
			m_grid_highlight.UserDeletingRow  += (s,a)=> OnDeletingRow    (m_grid_highlight, m_highlights, m_pattern_hl, a.Row.Index);
			m_grid_highlight.MouseDown        += (s,a)=> OnMouseDown      (m_grid_highlight, m_highlights, a);
			m_grid_highlight.DragOver         += (s,a)=> DoDragDrop       (m_grid_highlight, m_highlights, a, false);
			m_grid_highlight.CellValueNeeded  += (s,a)=> OnCellValueNeeded(m_grid_highlight, m_highlights, a);
			m_grid_highlight.CellClick        += (s,a)=> OnCellClick      (m_grid_highlight, m_highlights, m_pattern_hl, a);
			m_grid_highlight.CellDoubleClick  += (s,a)=> OnCellDoubleClick(m_grid_highlight, m_highlights, m_pattern_hl, a);
			m_grid_highlight.CellFormatting   += (s,a)=> OnCellFormatting (m_grid_highlight, m_highlights, a);
			m_grid_highlight.DataError        += (s,a)=> a.Cancel = true;
			m_grid_highlight.CellContextMenuStripNeeded += (s,a)=> OnCellClick(m_grid_highlight, m_highlights, m_pattern_hl, a);

			m_hover_scroll.WindowHandles.Add(m_grid_highlight.Handle);

			// Highlight pattern
			m_pattern_hl.Commit += (s,a)=>
				{
					WhatsChanged |= EWhatsChanged.Rendering;
					CommitPattern(m_pattern_hl, m_highlights);
					HighlightsChanged = true;
					UpdateUI();
				};

			// Highlight pattern sets
			m_pattern_set_hl.Init(m_settings, m_highlights);
			m_pattern_set_hl.CurrentSetChanged += (s,a)=>
				{
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
			m_grid_filter.Columns.Add(new DataGridViewImageColumn   {Name = ColumnNames.Active    ,HeaderText = Resources.Active  ,FillWeight = 25  ,ReadOnly = true ,AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader ,ImageLayout = DataGridViewImageCellLayout.Zoom});
			m_grid_filter.Columns.Add(new DataGridViewTextBoxColumn {Name = ColumnNames.Behaviour ,HeaderText = Resources.IfMatch ,FillWeight = 25  ,ReadOnly = true ,AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader});
			m_grid_filter.Columns.Add(new DataGridViewTextBoxColumn {Name = ColumnNames.Pattern   ,HeaderText = Resources.Pattern ,FillWeight = 100 ,ReadOnly = true });
			m_grid_filter.Columns.Add(new DataGridViewImageColumn   {Name = ColumnNames.Modify    ,HeaderText = Resources.Edit    ,FillWeight = 15  ,ReadOnly = true ,AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader ,ImageLayout = DataGridViewImageCellLayout.Zoom});
			m_grid_filter.ClipboardCopyMode   = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
			m_grid_filter.KeyDown            += DataGridView_Extensions.Copy;
			m_grid_filter.KeyDown            += DataGridView_Extensions.SelectAll;
			m_grid_filter.UserDeletingRow    += (s,a)=> OnDeletingRow    (m_grid_filter, m_filters, m_pattern_ft, a.Row.Index);
			m_grid_filter.MouseDown          += (s,a)=> OnMouseDown      (m_grid_filter, m_filters, a);
			m_grid_filter.DragOver           += (s,a)=> DoDragDrop       (m_grid_filter, m_filters, a, false);
			m_grid_filter.CellValueNeeded    += (s,a)=> OnCellValueNeeded(m_grid_filter, m_filters, a);
			m_grid_filter.CellClick          += (s,a)=> OnCellClick      (m_grid_filter, m_filters, m_pattern_ft, a);
			m_grid_filter.CellDoubleClick    += (s,a)=> OnCellDoubleClick(m_grid_filter, m_filters, m_pattern_ft, a);
			m_grid_filter.CellFormatting     += (s,a)=> OnCellFormatting (m_grid_filter, m_filters, a);
			m_grid_filter.DataError          += (s,a)=> a.Cancel = true;

			m_hover_scroll.WindowHandles.Add(m_grid_filter.Handle);

			// Check reject all by default
			m_check_reject_all_by_default.Checked = m_filters.Contains(Filter.RejectAll);
			m_check_reject_all_by_default.Click += (s,a)=>
				{
					m_filters.Remove(Filter.RejectAll);
					if (m_check_reject_all_by_default.Checked)
						m_filters.Add(Filter.RejectAll);
					
					FlagAsChanged(m_grid_filter);
					UpdateUI();
				};
			
			// Filter pattern
			m_pattern_ft.Commit += (s,a)=>
				{
					WhatsChanged |= EWhatsChanged.FileParsing;
					CommitPattern(m_pattern_ft, m_filters);
					FiltersChanged = true;
					UpdateUI();
				};
			
			// Filter pattern sets
			m_pattern_set_ft.Init(m_settings, m_filters);
			m_pattern_set_ft.CurrentSetChanged += (s,a)=>
				{
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
			m_grid_transform.Columns.Add(new DataGridViewImageColumn  {Name = ColumnNames.Active  ,HeaderText = Resources.Active  ,FillWeight = 25  ,AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader ,ImageLayout = DataGridViewImageCellLayout.Zoom});
			m_grid_transform.Columns.Add(new DataGridViewTextBoxColumn{Name = ColumnNames.Pattern ,HeaderText = Resources.Pattern ,FillWeight = 100 ,ReadOnly = true });
			m_grid_transform.Columns.Add(new DataGridViewImageColumn  {Name = ColumnNames.Modify  ,HeaderText = Resources.Edit    ,FillWeight = 15  ,AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader ,ImageLayout = DataGridViewImageCellLayout.Zoom});
			m_grid_transform.ClipboardCopyMode   = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
			m_grid_transform.KeyDown            += DataGridView_Extensions.Copy;
			m_grid_transform.KeyDown            += DataGridView_Extensions.SelectAll;
			m_grid_transform.UserDeletingRow    += (s,a)=> OnDeletingRow    (m_grid_transform, m_transforms, m_pattern_tx, a.Row.Index);
			m_grid_transform.MouseDown          += (s,a)=> OnMouseDown      (m_grid_transform, m_transforms, a);
			m_grid_transform.DragOver           += (s,a)=> DoDragDrop       (m_grid_transform, m_transforms, a, false);
			m_grid_transform.CellValueNeeded    += (s,a)=> OnCellValueNeeded(m_grid_transform, m_transforms, a);
			m_grid_transform.CellClick          += (s,a)=> OnCellClick      (m_grid_transform, m_transforms, m_pattern_tx, a);
			m_grid_transform.CellDoubleClick    += (s,a)=> OnCellDoubleClick(m_grid_transform, m_transforms, m_pattern_tx, a);
			m_grid_transform.CellFormatting     += (s,a)=> OnCellFormatting (m_grid_transform, m_transforms, a);
			m_grid_transform.DataError          += (s,a)=> a.Cancel = true;

			m_hover_scroll.WindowHandles.Add(m_grid_transform.Handle);

			// Transform
			m_pattern_tx.Commit += (s,a)=>
				{
					WhatsChanged |= EWhatsChanged.Rendering;
					CommitPattern(m_pattern_tx, m_transforms);
					TransformsChanged = true;
					UpdateUI();
				};
			
			// Transform sets
			m_pattern_set_tx.Init(m_settings, m_transforms);
			m_pattern_set_tx.CurrentSetChanged += (s,a)=>
				{
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
			m_grid_action.Columns.Add(new DataGridViewImageColumn   {Name = ColumnNames.Active       ,HeaderText = Resources.Active  ,FillWeight = 25  ,AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader ,ImageLayout = DataGridViewImageCellLayout.Zoom});
			m_grid_action.Columns.Add(new DataGridViewTextBoxColumn {Name = ColumnNames.Pattern      ,HeaderText = Resources.Pattern ,FillWeight = 100 ,ReadOnly = true });
			m_grid_action.Columns.Add(new DataGridViewTextBoxColumn {Name = ColumnNames.ClickAction  ,HeaderText = Resources.Action  ,FillWeight = 100 ,ReadOnly = true });
			m_grid_action.Columns.Add(new DataGridViewImageColumn   {Name = ColumnNames.Modify       ,HeaderText = Resources.Edit    ,FillWeight = 15  ,AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader ,ImageLayout = DataGridViewImageCellLayout.Zoom});
			m_grid_action.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
			m_grid_action.KeyDown          += DataGridView_Extensions.Copy;
			m_grid_action.KeyDown          += DataGridView_Extensions.SelectAll;
			m_grid_action.UserDeletingRow  += (s,a)=> OnDeletingRow    (m_grid_action, m_actions, m_pattern_ac, a.Row.Index);
			m_grid_action.MouseDown        += (s,a)=> OnMouseDown      (m_grid_action, m_actions, a);
			m_grid_action.DragOver         += (s,a)=> DoDragDrop       (m_grid_action, m_actions, a, false);
			m_grid_action.CellValueNeeded  += (s,a)=> OnCellValueNeeded(m_grid_action, m_actions, a);
			m_grid_action.CellClick        += (s,a)=> OnCellClick      (m_grid_action, m_actions, m_pattern_ac, a);
			m_grid_action.CellDoubleClick  += (s,a)=> OnCellDoubleClick(m_grid_action, m_actions, m_pattern_ac, a);
			m_grid_action.CellFormatting   += (s,a)=> OnCellFormatting (m_grid_action, m_actions, a);
			m_grid_action.DataError        += (s,a)=> a.Cancel = true;
			m_grid_action.CellContextMenuStripNeeded += (s,a)=> OnCellClick(m_grid_action, m_actions, m_pattern_ac, a);

			m_hover_scroll.WindowHandles.Add(m_grid_action.Handle);

			// Action pattern
			m_pattern_ac.Commit += (s,a)=>
				{
					WhatsChanged |= EWhatsChanged.Nothing;
					CommitPattern(m_pattern_ac, m_actions);
					ActionsChanged = true;
					UpdateUI();
				};
			
			// Action pattern sets
			m_pattern_set_ac.Init(m_settings, m_actions);
			m_pattern_set_ac.CurrentSetChanged += (s,a)=>
				{
					UpdateUI();
				};
		}

		/// <summary>Check all pattern tabs for unsaved changes and prompt to save if any</summary>
		private void SavePatternChanges(CancelEventArgs args)
		{
			SavePatternChanges(m_pattern_hl, m_highlights, "Highlighting", args);
			SavePatternChanges(m_pattern_ft, m_filters   , "Filtering"   , args);
			SavePatternChanges(m_pattern_tx, m_transforms, "Transform"   , args);
			SavePatternChanges(m_pattern_ac, m_actions   , "Click action", args);
		}

		/// <summary>Helper for prompting to save changes to a pattern before leaving the tab</summary>
		private void SavePatternChanges<TPattern>(IPatternUI pattern_ui, List<TPattern> patterns, string text, CancelEventArgs args) where TPattern:IPattern,new()
		{
			if (pattern_ui.HasUnsavedChanges)
			{
				var res = MessageBox.Show(this, "{0} pattern contains unsaved changes.\r\n\r\nSave changes?".Fmt(text),"Unsaved Changes",MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
				if (res == DialogResult.Cancel) { args.Cancel = true; return; }
				if (res == DialogResult.No)     { pattern_ui.NewPattern(new TPattern()); return; }
				if (!pattern_ui.CommitEnabled)  { args.Cancel = true; return; }
				CommitPattern(pattern_ui, patterns);
			}
		}

		/// <summary>Save a modified pattern from a pattern UI</summary>
		private void CommitPattern<TPattern>(IPatternUI pattern_ui, List<TPattern> patterns) where TPattern:IPattern,new()
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
			DialogResult res = MessageBox.Show(this, "This will reset all current settings to their default values\r\n\r\nContinue?", "Confirm Reset Settings", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
			if (res != DialogResult.Yes) return;
			
			// Flatten the settings
			try { m_settings.Reset(); Close(); }
			catch (Exception ex)
			{
				Log.Exception(this, ex, "Resetting settings to defaults");
				Misc.ShowErrorMessage(this, ex, "Failed to reset settings to their default values.", "Reset Settings Failed");
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
					m_edit_line_ends.ShowHintBalloon(m_balloon, "Set the line ending characters to expect in the log data.\r\nUse '<CR>' for carriage return, '<LF>' for line feed.\r\nLeave blank to auto detect");
					m_special = ESpecial.None;
					break;
				}
			}
		}

		/// <summary>Set appropriate changed flags when a grid is changed</summary>
		private void FlagAsChanged(DataGridView grid)
		{
			if      (grid == m_grid_highlight) { HighlightsChanged = true; WhatsChanged |= EWhatsChanged.Rendering; }
			else if (grid == m_grid_filter   ) { FiltersChanged    = true; WhatsChanged |= EWhatsChanged.FileParsing; }
			else if (grid == m_grid_transform) { TransformsChanged = true; WhatsChanged |= EWhatsChanged.FileParsing; }
			else if (grid == m_grid_action   ) { ActionsChanged    = true; WhatsChanged |= EWhatsChanged.Nothing; }
		}

		/// <summary>Handle key down</summary>
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
			case Keys.F1:
				{
					e.Handled = true;
					var ctrl = this.GetChildAtScreenPointRec(MousePosition);
					if (ctrl != null)
					{
						var msg = ctrl.ToolTipText();
						if (msg != null)
						{
							m_tt.Hide(ctrl);
							m_balloon.Hide(ctrl);
							ctrl.ShowHintBalloon(m_balloon, msg);
						}
					}
				}
				break;
			}
			base.OnKeyDown(e);
		}

		/// <summary>Delete a pattern</summary>
		private void OnDeletingRow<TPattern>(DataGridView grid, List<TPattern> patterns, IPatternUI pattern_ui, int index) where TPattern:IPattern,new()
		{
			// Note, don't update the grid here or it causes an ArgumentOutOfRange exception.
			// Other stuff must be using the grid row that will be deleted.

			// If the deleted row is currently being edited, remove it from the editor first
			if (ReferenceEquals(pattern_ui.Original, patterns[index]))
				pattern_ui.NewPattern(new TPattern());

			patterns.RemoveAt(index);
			FlagAsChanged(grid);

			if (typeof(TPattern) == typeof(Filter))
				m_check_reject_all_by_default.Checked = m_filters.Contains(Filter.RejectAll);
		}

		/// <summary>OnDragDrop functionality for grid rows</summary>
		private void DoDragDrop<T>(DataGridView grid, List<T> patterns, DragEventArgs args, bool test_can_drop)
		{
			args.Effect = DragDropEffects.None;
			if (!args.Data.GetDataPresent(typeof(T))) return;
			Point pt = grid.PointToClient(new Point(args.X, args.Y));
			var hit = grid.HitTest(pt.X, pt.Y);
			if (hit.Type != DataGridViewHitTestType.RowHeader || hit.RowIndex < 0 || hit.RowIndex >= patterns.Count) return;
			args.Effect = args.AllowedEffect;
			if (test_can_drop) return;

			// Swap the rows
			T pat = (T)args.Data.GetData(typeof(T));
			int idx1 = patterns.IndexOf(pat);
			int idx2 = hit.RowIndex;
			patterns.Swap(idx1, idx2);
			grid.InvalidateRow(idx1);
			grid.InvalidateRow(idx2);

			FlagAsChanged(grid);
		}

		/// <summary>Handle mouse down on the patterns grid</summary>
		private static void OnMouseDown<T>(DataGridView grid, List<T> patterns, MouseEventArgs e)
		{
			var hit = grid.HitTest(e.X, e.Y);
			if (hit.Type == DataGridViewHitTestType.RowHeader && hit.RowIndex >= 0 && hit.RowIndex < patterns.Count)
			{
				grid.DoDragDrop(patterns[hit.RowIndex], DragDropEffects.Move);
			}
		}

		/// <summary>Provide cells for the grid</summary>
		private static void OnCellValueNeeded<T>(DataGridView grid, List<T> patterns, DataGridViewCellValueEventArgs e) where T: class, IPattern
		{
			if (e.RowIndex < 0 || e.RowIndex >= patterns.Count) { e.Value = ""; return; }
			var cell = grid[e.ColumnIndex, e.RowIndex];
			T pat = patterns[e.RowIndex];
			Pattern   pt = pat as Pattern;
			Highlight hl = pat as Highlight;
			Filter    ft = pat as Filter;
			ClkAction ac = pat as ClkAction;
			
			switch (grid.Columns[e.ColumnIndex].Name)
			{
			default: e.Value = string.Empty; break;
			case ColumnNames.Pattern:
				string val = pat.ToString();
				if (string.IsNullOrEmpty(val)) val = "<blank>";
				if (string.IsNullOrWhiteSpace(val)) val = "'"+val+"'";
				if (pt != null && pt.Invert) val = "not "+val;
				e.Value = val;
				break;
			case ColumnNames.Highlighting:
				e.Value = Resources.ClickToModifyHighlight;
				cell.Style.BackColor = cell.Style.SelectionBackColor = hl != null ? hl.BackColour : Color.White;
				cell.Style.ForeColor = cell.Style.SelectionForeColor = hl != null ? hl.ForeColour : Color.White;
				break;
			case ColumnNames.Behaviour:
				if (ft != null) switch (ft.IfMatch) {
				case EIfMatch.Keep:   e.Value = "Keep"; break;
				case EIfMatch.Reject: e.Value = "Reject"; break;
				}
				break;
			case ColumnNames.ClickAction:
				e.Value = ac != null && !string.IsNullOrEmpty(ac.Executable) ? ac.ActionString : Resources.ClickToModifyAction;
				break;
			}
		}

		/// <summary>Handle cell clicks</summary>
		private void OnCellClick<T>(DataGridView grid, List<T> patterns, IPatternUI ctrl, DataGridViewCellEventArgs e) where T: class, IPattern
		{
			if (e.RowIndex    < 0 || e.RowIndex    >= patterns.Count  ) return;
			if (e.RowIndex    < 0 || e.RowIndex    >= grid.RowCount   ) return;
			if (e.ColumnIndex < 0 || e.ColumnIndex >= grid.ColumnCount) return;
			T pat = patterns[e.RowIndex];
			Highlight hl = pat as Highlight;
			Filter    ft = pat as Filter;
			ClkAction ac = pat as ClkAction;
			
			switch (grid.Columns[e.ColumnIndex].Name)
			{
			default: return;
			case ColumnNames.Active: pat.Active = !pat.Active; break;
			case ColumnNames.Modify:
				ctrl.EditPattern(patterns[e.RowIndex]);
				break;
			case ColumnNames.Highlighting:
				if (hl != null)
				{
					var dlg = new ColourPickerUI {FontColor = hl.ForeColour, BkgdColor = hl.BackColour};
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
					patterns[e.RowIndex] = dg.Action as T;
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
		private void OnCellDoubleClick<T>(DataGridView grid, List<T> patterns, IPatternUI ctrl, DataGridViewCellEventArgs e) where T:IPattern
		{
			if (e.RowIndex    < 0 || e.RowIndex    >= patterns.Count  ) return;
			if (e.RowIndex    < 0 || e.RowIndex    >= grid.RowCount   ) return;
			if (e.ColumnIndex < 0 || e.ColumnIndex >= grid.ColumnCount) return;
			T pat = patterns[e.RowIndex];
			
			ctrl.EditPattern(pat);
			FlagAsChanged(grid);
		}
		
		/// <summary>Cell formatting...</summary>
		private static void OnCellFormatting<T>(DataGridView grid, List<T> patterns, DataGridViewCellFormattingEventArgs e) where T:IPattern
		{
			if (e.RowIndex    < 0 || e.RowIndex    >= patterns.Count  ) return;
			if (e.RowIndex    < 0 || e.RowIndex    >= grid.RowCount   ) return;
			if (e.ColumnIndex < 0 || e.ColumnIndex >= grid.ColumnCount) return;
			T pat = patterns[e.RowIndex];
			
			switch (grid.Columns[e.ColumnIndex].Name)
			{
			default: return;
			case ColumnNames.Active:
				e.Value = pat.Active ? Resources.pattern_active : Resources.pattern_inactive;
				break;
			case ColumnNames.Modify:
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

		/// <summary>Update the UI state based on current settings</summary>
		private void UpdateUI()
		{
			try
			{
				SuspendLayout();
			
				bool use_web_proxy = m_settings.UseWebProxy;
				m_lbl_web_proxy_host.Enabled = use_web_proxy;
				m_lbl_web_proxy_port.Enabled = use_web_proxy;
				m_edit_web_proxy_host.Enabled = use_web_proxy;
				m_spinner_web_proxy_port.Enabled = use_web_proxy;
				m_spinner_column_count.Enabled = m_settings.ColDelimiter.Length != 0;
			
				m_check_alternate_line_colour.Checked = m_settings.AlternateLineColours;
				m_lbl_selection_example.BackColor = m_settings.LineSelectBackColour;
				m_lbl_selection_example.ForeColor = m_settings.LineSelectForeColour;
				m_lbl_line1_example.BackColor = m_settings.LineBackColour1;
				m_lbl_line1_example.ForeColor = m_settings.LineForeColour1;
				m_lbl_line2_example.BackColor = m_settings.LineBackColour2;
				m_lbl_line2_example.ForeColor = m_settings.LineForeColour2;
				m_lbl_line2_example.Enabled = m_settings.AlternateLineColours;
				
				m_check_reject_all_by_default.Checked = m_filters.Contains(Filter.RejectAll);
				
				int selected = m_grid_highlight.FirstSelectedRowIndex();
				m_grid_highlight.CurrentCell = null;
				m_grid_highlight.RowCount = 0;
				m_grid_highlight.RowCount = m_highlights.Count;
				m_grid_highlight.SelectRow(selected);
				
				selected = m_grid_filter.FirstSelectedRowIndex();
				m_grid_filter.CurrentCell = null;
				m_grid_filter.RowCount = 0;
				m_grid_filter.RowCount = m_filters.Count;
				m_grid_filter.SelectRow(selected);
				
				selected = m_grid_transform.FirstSelectedRowIndex();
				m_grid_transform.CurrentCell = null;
				m_grid_transform.RowCount = 0;
				m_grid_transform.RowCount = m_transforms.Count;
				m_grid_transform.SelectRow(selected);
				
				selected = m_grid_action.FirstSelectedRowIndex();
				m_grid_action.CurrentCell = null;
				m_grid_action.RowCount = 0;
				m_grid_action.RowCount = m_actions.Count;
				m_grid_action.SelectRow(selected);
				
				m_text_settings.Text = m_settings.Filepath;

				m_main.UseLicensedFeature(FeatureName.Highlighting, new SettingsHighlightingCountLimiter(m_main, m_settings, this));
				m_main.UseLicensedFeature(FeatureName.Filtering,    new SettingsFilteringCountLimiter(m_main, m_settings, this));
			}
			finally
			{
				ResumeLayout();
			}
		}

		/// <summary>A highlighting count limiter for when the settings dialog is displayed</summary>
		private class SettingsHighlightingCountLimiter :HighlightingCountLimiter
		{
			private readonly SettingsUI m_ui; 
			public SettingsHighlightingCountLimiter(Main main, Settings settings, SettingsUI ui)
				:base(main, settings)
			{
				m_ui = ui;
			}

			/// <summary>True if the licensed feature is still currently in use</summary>
			public override bool FeatureInUse
			{
				get { return m_ui.m_highlights.Count > FreeEditionLimits.MaxHighlights || base.FeatureInUse; }
			}

			/// <summary>Called to stop the use of the feature</summary>
			public override void CloseFeature()
			{
				m_ui.m_highlights.RemoveToEnd(FreeEditionLimits.MaxHighlights);
				m_ui.UpdateUI();
				base.CloseFeature();
			}
		}

		/// <summary>A filtering count limiter for when the settings dialog is displayed</summary>
		private class SettingsFilteringCountLimiter :HighlightingCountLimiter
		{
			private readonly SettingsUI m_ui; 
			public SettingsFilteringCountLimiter(Main main, Settings settings, SettingsUI ui)
				:base(main, settings)
			{
				m_ui = ui;
			}

			/// <summary>True if the licensed feature is still currently in use</summary>
			public override bool FeatureInUse
			{
				get { return m_ui.m_filters.Count > FreeEditionLimits.MaxFilters || base.FeatureInUse; }
			}

			/// <summary>Called to stop the use of the feature</summary>
			public override void CloseFeature()
			{
				m_ui.m_filters.RemoveToEnd(FreeEditionLimits.MaxFilters);
				m_ui.UpdateUI();
				base.CloseFeature();
			}
		}
	}
}
