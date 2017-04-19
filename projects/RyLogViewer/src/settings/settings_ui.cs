using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using pr.common;
using pr.extn;
using pr.gfx;
using pr.gui;
using pr.maths;
using pr.util;
using RyLogViewer.Properties;

namespace RyLogViewer
{
	public partial class SettingsUI :Form
	{
		// Notes:
		//  - The settings UI operates on the main settings.
		//  - There is no cancel button, settings are modified as they're changed.

		private HoverScroll m_hover_scroll; // Hover scroll for the pattern grid
		private ESpecial m_special;

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
			get { return m_main; }
			set
			{
				if (m_main == value) return;
				if (m_main != null)
				{
					m_main.Settings.SettingChanged -= UpdateUI;
				}
				m_main = value;
				if (m_main != null)
				{
					m_main.Settings.SettingChanged += UpdateUI;
				}
			}
		}
		private Main m_main;

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
			string tt;

			// Load last file on startup
			m_check_load_last_file.ToolTip(m_tt, "Automatically load the last loaded file on startup");
			m_check_load_last_file.Checked = Settings.LoadLastFile;
			m_check_load_last_file.Click += (s,a)=>
			{
				Settings.LoadLastFile = m_check_load_last_file.Checked;
				WhatsChanged |= EWhatsChanged.StartupOptions;
			};

			// Restore window position on startup
			m_check_save_screen_loc.ToolTip(m_tt, "Restore the window to its last position on startup");
			m_check_save_screen_loc.Checked = Settings.RestoreScreenLoc;
			m_check_save_screen_loc.Click += (s,a)=>
			{
				Settings.RestoreScreenLoc = m_check_save_screen_loc.Checked;
				WhatsChanged |= EWhatsChanged.StartupOptions;
			};

			// Show tip of the day on startup
			m_check_show_totd.ToolTip(m_tt, "Show the 'Tip of the Day' dialog on startup");
			m_check_show_totd.Checked = Settings.ShowTOTD;
			m_check_show_totd.Click += (s,a)=>
			{
				Settings.ShowTOTD = m_check_show_totd.Checked;
				WhatsChanged |= EWhatsChanged.StartupOptions;
			};

			// Check for updates
			m_check_c4u.ToolTip(m_tt, "Check for newer versions on startup");
			m_check_c4u.Checked = Settings.CheckForUpdates;
			m_check_c4u.Click += (s,a)=>
			{
				Settings.CheckForUpdates = m_check_c4u.Checked;
				WhatsChanged |= EWhatsChanged.StartupOptions;
			};

			// Use web proxy
			m_check_use_web_proxy.ToolTip(m_tt, "Use a web proxy for internet connections (such as checking for updates)");
			m_check_use_web_proxy.Checked = Settings.UseWebProxy;
			m_check_use_web_proxy.Click += (s,a)=>
			{
				Settings.UseWebProxy = m_check_use_web_proxy.Checked;
				WhatsChanged |= EWhatsChanged.Nothing;
			};

			// Web proxy host
			tt = "The host name or IP address of the web proxy server";
			m_lbl_web_proxy_host.ToolTip(m_tt, tt);
			m_edit_web_proxy_host.ToolTip(m_tt, tt);
			m_edit_web_proxy_host.Text = Settings.WebProxyHost;
			m_edit_web_proxy_host.TextChanged += (s,a)=>
			{
				if (!((TextBox)s).Modified) return;
				Settings.WebProxyHost = m_edit_web_proxy_host.Text;
				WhatsChanged |= EWhatsChanged.Nothing;
			};

			// Web proxy port
			tt = "The port number of the web proxy server";
			m_lbl_web_proxy_port.ToolTip(m_tt, tt);
			m_spinner_web_proxy_port.ToolTip(m_tt, tt);
			m_spinner_web_proxy_port.Minimum = Constants.PortNumberMin;
			m_spinner_web_proxy_port.Maximum = Constants.PortNumberMax;
			m_spinner_web_proxy_port.Value = Maths.Clamp(Settings.WebProxyPort, Constants.PortNumberMin, Constants.PortNumberMax);
			m_spinner_web_proxy_port.ValueChanged += (s,a)=>
			{
				Settings.WebProxyPort = (int)m_spinner_web_proxy_port.Value;
				WhatsChanged |= EWhatsChanged.Nothing;
			};

			// Line endings
			tt = "Set the line ending characters to expect in the log data.\r\nUse '<CR>' for carriage return, '<LF>' for line feed.\r\nLeave blank to auto detect";
			m_lbl_line_ends.ToolTip(m_tt, tt);
			m_edit_line_ends.ToolTip(m_tt, tt);
			m_edit_line_ends.Text = Settings.RowDelimiter;
			m_edit_line_ends.TextChanged += (s,a)=>
			{
				if (!((TextBox)s).Modified) return;
				Settings.RowDelimiter = m_edit_line_ends.Text;
				WhatsChanged |= EWhatsChanged.FileParsing;
			};

			// Column delimiters
			tt = "Set the characters that separate columns in the log data.\r\nUse '<TAB>' for a tab character.\r\nLeave blank for no column delimiter";
			m_lbl_col_delims.ToolTip(m_tt, tt);
			m_edit_col_delims.ToolTip(m_tt, tt);
			m_edit_col_delims.Text = Settings.ColDelimiter;
			m_edit_col_delims.TextChanged += (s,a)=>
			{
				if (!((TextBox)s).Modified) return;
				Settings.ColDelimiter = m_edit_col_delims.Text;
				WhatsChanged |= EWhatsChanged.FileParsing;
			};

			// Column Count
			tt = "The number of columns to display in the grid.\r\nUsed when the column delimiter is not blank";
			m_lbl_column_count.ToolTip(m_tt, tt);
			m_spinner_column_count.ToolTip(m_tt, tt);
			m_spinner_column_count.Minimum = Constants.ColumnCountMin;
			m_spinner_column_count.Maximum = Constants.ColumnCountMax;
			m_spinner_column_count.Value = Maths.Clamp(Settings.ColumnCount, Constants.ColumnCountMin, Constants.ColumnCountMax);
			m_spinner_column_count.ValueChanged += (s,a)=>
			{
				Settings.ColumnCount = (int)m_spinner_column_count.Value;
				WhatsChanged |= EWhatsChanged.Rendering;
			};

			// Include blank lines
			m_check_ignore_blank_lines.ToolTip(m_tt, "Ignore blank lines when loading the log file");
			m_check_ignore_blank_lines.Checked = Settings.IgnoreBlankLines;
			m_check_ignore_blank_lines.CheckedChanged += (s,a)=>
			{
				Settings.IgnoreBlankLines = m_check_ignore_blank_lines.Checked;
				WhatsChanged |= EWhatsChanged.FileParsing;
			};

			// Lines cached
			m_spinner_line_cache_count.ToolTip(m_tt, "The number of lines to scan into memory around the currently selected line");
			m_spinner_line_cache_count.Minimum = Constants.LineCacheCountMin;
			m_spinner_line_cache_count.Maximum = Constants.LineCacheCountMax;
			m_spinner_line_cache_count.Value = Maths.Clamp(Settings.LineCacheCount, (int)m_spinner_line_cache_count.Minimum, (int)m_spinner_line_cache_count.Maximum);
			m_spinner_line_cache_count.ValueChanged += (s,a)=>
			{
				Settings.LineCacheCount = (int)m_spinner_line_cache_count.Value;
				WhatsChanged |= EWhatsChanged.FileParsing;
			};

			// Max memory range
			tt = "The maximum number of bytes to scan when finding lines around the currently selected row (in MB).";
			m_lbl_max_scan_size0.ToolTip(m_tt, tt);
			m_spinner_max_mem_range.ToolTip(m_tt, tt);
			m_spinner_max_mem_range.Minimum = Constants.FileBufSizeMin / Constants.OneMB;
			m_spinner_max_mem_range.Maximum = Constants.FileBufSizeMax / Constants.OneMB;
			m_spinner_max_mem_range.Value = Maths.Clamp(Settings.FileBufSize / Constants.OneMB, (int)m_spinner_max_mem_range.Minimum, (int)m_spinner_max_mem_range.Maximum);
			m_spinner_max_mem_range.ValueChanged += (s,a)=>
			{
				Settings.FileBufSize = (int)m_spinner_max_mem_range.Value * Constants.OneMB;
				WhatsChanged |= EWhatsChanged.FileParsing;
			};

			// Max line length
			tt = "The maximum length of a line in the log file.\r\nIf the log contains lines longer than this an error will be reported when loading the file";
			m_lbl_max_line_len_kb.ToolTip(m_tt, tt);
			m_lbl_max_line_length.ToolTip(m_tt, tt);
			m_spinner_max_line_length.ToolTip(m_tt, tt);
			m_spinner_max_line_length.Minimum = Constants.MaxLineLengthMin / Constants.OneKB;
			m_spinner_max_line_length.Maximum = Constants.MaxLineLengthMax / Constants.OneKB;
			m_spinner_max_line_length.Value = Maths.Clamp(Settings.MaxLineLength / Constants.OneKB, (int)m_spinner_max_line_length.Minimum, (int)m_spinner_max_line_length.Maximum);
			m_spinner_max_line_length.ValueChanged += (s,a)=>
			{
				Settings.MaxLineLength = (int)m_spinner_max_line_length.Value * Constants.OneKB;
				WhatsChanged |= EWhatsChanged.FileParsing;
			};

			// Open at end
			m_check_open_at_end.ToolTip(m_tt, "If checked, opens files showing the end of the file.\r\nIf unchecked opens files at the beginning");
			m_check_open_at_end.Checked = Settings.OpenAtEnd;
			m_check_open_at_end.CheckedChanged += (s,a)=>
			{
				Settings.OpenAtEnd = m_check_open_at_end.Checked;
				WhatsChanged |= EWhatsChanged.FileOpenOptions;
			};

			// File changes additive
			m_check_file_changes_additive.ToolTip(m_tt, "Assume all changes to the watched file are additive only\r\nIf checked, reloading of changed files will not invalidate existing cached data");
			m_check_file_changes_additive.Checked = Settings.FileChangesAdditive;
			m_check_file_changes_additive.CheckedChanged += (s,a)=>
			{
				Settings.FileChangesAdditive = m_check_file_changes_additive.Checked;
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
					InitialDirectory = Path.GetDirectoryName(Settings.Filepath)
				};
				using (dg)
				{
					if (dg.ShowDialog(this) != DialogResult.OK) return;
					Settings.Filepath = dg.FileName;
					Settings.Reload();
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
					Filter = Resources.SettingsFileFilter,
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
			m_spinner_row_height.Value = Maths.Clamp(Settings.RowHeight, (int)m_spinner_row_height.Minimum, (int)m_spinner_row_height.Maximum);
			m_spinner_row_height.ValueChanged += (s,a)=>
				{
					Settings.RowHeight = (int)m_spinner_row_height.Value;
					WhatsChanged |= EWhatsChanged.Rendering;
				};

			// Font
			m_text_font.ToolTip(m_tt, "The font used to display the log file data");
			m_text_font.Text = "{0}, {1}pt".Fmt(Settings.Font.Name ,Settings.Font.Size);
			m_text_font.Font = Settings.Font;

			// Font button
			m_btn_change_font.ToolTip(m_tt, "Change the log view font");
			m_btn_change_font.Click += (s,a)=>
				{
					var dg = new FontDialog{Font = Settings.Font};
					if (dg.ShowDialog(this) != DialogResult.OK) return;
					m_text_font.Font = dg.Font;
					m_text_font.Text = "{0}, {1}pt".Fmt(dg.Font.Name ,dg.Font.Size);
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
			m_spinner_tabsize.Value = Maths.Clamp(Settings.TabSizeInSpaces, (int)m_spinner_tabsize.Minimum, (int)m_spinner_tabsize.Maximum);
			m_spinner_tabsize.ValueChanged += (s,a) =>
				{
					Settings.TabSizeInSpaces = (int)m_spinner_tabsize.Value;
					WhatsChanged |= EWhatsChanged.Rendering;
				};

			// File scroll width
			m_spinner_file_scroll_width.ToolTip(m_tt, "The width of the scroll bar that shows the current position within the log file");
			m_spinner_file_scroll_width.Minimum = Constants.FileScrollMinWidth;
			m_spinner_file_scroll_width.Maximum = Constants.FileScrollMaxWidth;
			m_spinner_file_scroll_width.Value = Maths.Clamp(Settings.FileScrollWidth, (int)m_spinner_file_scroll_width.Minimum, (int)m_spinner_file_scroll_width.Maximum);
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
			m_grid_highlight.Columns.Add(new DataGridViewImageColumn   {Name = ColumnNames.Active    ,HeaderText = Resources.Active       ,FillWeight = 25  ,AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader ,ImageLayout = DataGridViewImageCellLayout.Zoom ,ToolTipText = ColumnTT.Active});
			m_grid_highlight.Columns.Add(new DataGridViewTextBoxColumn {Name = ColumnNames.Pattern   ,HeaderText = Resources.Pattern      ,FillWeight = 100 ,ReadOnly = true ,ToolTipText = ColumnTT.Pattern});
			m_grid_highlight.Columns.Add(new DataGridViewTextBoxColumn {Name = ColumnNames.Highlight ,HeaderText = Resources.RowHighlight ,FillWeight = 30  ,ReadOnly = true ,ToolTipText = ColumnTT.Highlight});
			m_grid_highlight.Columns.Add(new DataGridViewTextBoxColumn {Name = ColumnNames.Colours   ,HeaderText = Resources.Colours      ,FillWeight = 100 ,ReadOnly = true ,DefaultCellStyle = hl_style ,ToolTipText = ColumnTT.Colours});
			m_grid_highlight.Columns.Add(new DataGridViewImageColumn   {Name = ColumnNames.Edit      ,HeaderText = Resources.Edit         ,FillWeight = 15  ,AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader ,ImageLayout = DataGridViewImageCellLayout.Zoom ,ToolTipText = ColumnTT.Edit});
			m_grid_highlight.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
			m_grid_highlight.KeyDown                    += DataGridViewEx.Copy;
			m_grid_highlight.KeyDown                    += DataGridViewEx.SelectAll;
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
			m_grid_filter.Columns.Add(new DataGridViewImageColumn   {Name = ColumnNames.Active    ,HeaderText = Resources.Active  ,FillWeight = 25  ,ReadOnly = true ,AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader ,ImageLayout = DataGridViewImageCellLayout.Zoom ,ToolTipText = ColumnTT.Active});
			m_grid_filter.Columns.Add(new DataGridViewTextBoxColumn {Name = ColumnNames.Behaviour ,HeaderText = Resources.IfMatch ,FillWeight = 25  ,ReadOnly = true ,AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader ,ToolTipText = ColumnTT.Behaviour});
			m_grid_filter.Columns.Add(new DataGridViewTextBoxColumn {Name = ColumnNames.Pattern   ,HeaderText = Resources.Pattern ,FillWeight = 100 ,ReadOnly = true ,ToolTipText = ColumnTT.Pattern});
			m_grid_filter.Columns.Add(new DataGridViewImageColumn   {Name = ColumnNames.Edit      ,HeaderText = Resources.Edit    ,FillWeight = 15  ,ReadOnly = true ,AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader ,ImageLayout = DataGridViewImageCellLayout.Zoom ,ToolTipText = ColumnTT.Edit});
			m_grid_filter.ClipboardCopyMode   = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
			m_grid_filter.KeyDown            += DataGridViewEx.Copy;
			m_grid_filter.KeyDown            += DataGridViewEx.SelectAll;
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
			m_grid_transform.Columns.Add(new DataGridViewImageColumn  {Name = ColumnNames.Active  ,HeaderText = Resources.Active  ,FillWeight = 25  ,AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader ,ImageLayout = DataGridViewImageCellLayout.Zoom ,ToolTipText = ColumnTT.Active});
			m_grid_transform.Columns.Add(new DataGridViewTextBoxColumn{Name = ColumnNames.Pattern ,HeaderText = Resources.Pattern ,FillWeight = 100 ,ReadOnly = true ,ToolTipText = ColumnTT.Pattern});
			m_grid_transform.Columns.Add(new DataGridViewImageColumn  {Name = ColumnNames.Edit    ,HeaderText = Resources.Edit    ,FillWeight = 15  ,AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader ,ImageLayout = DataGridViewImageCellLayout.Zoom ,ToolTipText = ColumnTT.Edit});
			m_grid_transform.ClipboardCopyMode   = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
			m_grid_transform.KeyDown            += DataGridViewEx.Copy;
			m_grid_transform.KeyDown            += DataGridViewEx.SelectAll;
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
			m_grid_action.Columns.Add(new DataGridViewImageColumn   {Name = ColumnNames.Active      ,HeaderText = Resources.Active  ,FillWeight = 25  ,AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader ,ImageLayout = DataGridViewImageCellLayout.Zoom ,ToolTipText = ColumnTT.Active});
			m_grid_action.Columns.Add(new DataGridViewTextBoxColumn {Name = ColumnNames.Pattern     ,HeaderText = Resources.Pattern ,FillWeight = 100 ,ReadOnly = true ,ToolTipText = ColumnTT.Pattern});
			m_grid_action.Columns.Add(new DataGridViewTextBoxColumn {Name = ColumnNames.ClickAction ,HeaderText = Resources.Action  ,FillWeight = 100 ,ReadOnly = true ,ToolTipText = ColumnTT.ClickAction});
			m_grid_action.Columns.Add(new DataGridViewImageColumn   {Name = ColumnNames.Edit        ,HeaderText = Resources.Edit    ,FillWeight = 15  ,AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader ,ImageLayout = DataGridViewImageCellLayout.Zoom ,ToolTipText = ColumnTT.Edit});
			m_grid_action.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
			m_grid_action.KeyDown                    += DataGridViewEx.Copy;
			m_grid_action.KeyDown                    += DataGridViewEx.SelectAll;
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
			m_edit_web_proxy_host.Enabled = use_web_proxy;
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
			m_grid_highlight.SelectRow(selected);

			selected = m_grid_filter.FirstSelectedRowIndex();
			m_grid_filter.CurrentCell = null;
			m_grid_filter.RowCount = 0;
			m_grid_filter.RowCount = Settings.Patterns.Filters.Count;
			m_grid_filter.SelectRow(selected);

			selected = m_grid_transform.FirstSelectedRowIndex();
			m_grid_transform.CurrentCell = null;
			m_grid_transform.RowCount = 0;
			m_grid_transform.RowCount = Settings.Patterns.Transforms.Count;
			m_grid_transform.SelectRow(selected);

			selected = m_grid_action.FirstSelectedRowIndex();
			m_grid_action.CurrentCell = null;
			m_grid_action.RowCount = 0;
			m_grid_action.RowCount = Settings.Patterns.Actions.Count;
			m_grid_action.SelectRow(selected);

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
				var res = MsgBox.Show(this, "{0} pattern contains unsaved changes.\r\n\r\nSave changes?".Fmt(text),"Unsaved Changes",MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
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
				EventHandler reset_on_close = null;
				reset_on_close = (s,a) =>
					{
						Settings.Reset();
						Closed -= reset_on_close;
					};
				Closed += reset_on_close;
				Close();
			}
			catch (Exception ex)
			{
				Log.Exception(this, ex, "Resetting settings to defaults");
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
					Misc.ShowHint(m_edit_line_ends,
						"Set the line ending characters to expect in the log data.\r\n" +
						"Use '<CR>' for carriage return, '<LF>' for line feed.\r\n" +
						"Leave blank to auto detect", 7000);
					break;
				}
			//case ESpecial.ShowPatternSetsTip:
			//	{
			//		Misc.ShowHint(m_pattern_set_hl, "Pattern sets are loaded, saved, and selected here");
			//		break;
			//	}
			case ESpecial.ShowColumnDelimiterTip:
				{
					Misc.ShowHint(m_edit_col_delims, "Multi-column mode is enabled when a column delimiter is given here");
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
			patterns.Swap(idx1, idx2);
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
				e.Value = Resources.ClickToModifyHighlight;
				cell.Style.BackColor = cell.Style.SelectionBackColor = hl != null ? Gfx.Blend(Color.FromArgb(255, hl.BackColour), Color.White, 1f - hl.BackColour.A/255f) : Color.White;
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
				e.Value = ac != null && !string.IsNullOrEmpty(ac.Executable) ? ac.ActionString : Resources.ClickToModifyAction;
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
	}
}
