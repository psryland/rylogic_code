using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using RyLogViewer.Properties;
using Rylogic.Extn;
using Rylogic.Graphix;
using Rylogic.Gui;
using Rylogic.Utility;

namespace RyLogViewer
{
	using WebBrowser = System.Windows.Forms.WebBrowser;

	public sealed class FirstRunTutorial :ToolForm
	{
		public enum ETutPage
		{
			OpenLogData,
			EnableHighlights,
			EnableFilters,
			EnableTransforms,
			EnableActions,
			FileWatching,
			FileScroll,
			PatternEditor,
			PatternsList,
			PatternSets,
			HelpMenu,

			First = OpenLogData,
			Last  = HelpMenu,
		}

		private readonly Main m_main;
		private readonly Settings m_orig_settings;

		#region UI Elements
		private SettingsUI m_settings_ui;
		private ETutPage m_page_index;
		private PageBase m_current_page;
		private WebBrowser m_html;
		private Button m_btn_next;
		private Button m_btn_back;
		private Panel m_panel;
		#endregion

		public FirstRunTutorial(Main main)
			:base(main, EPin.TopRight, new Point(-200,80))
		{
			InitializeComponent();
			ShowInTaskbar = false;
			Icon = main.Icon;
			HideOnClose = false;

			// Close the current log file, save the user settings
			// and load the default settings
			m_main = main;
			m_main.Src = null;
			m_page_index = ETutPage.First - 1;

			// Grab a reference to the main settings then replace the
			// main settings with a default instance
			m_orig_settings = m_main.Settings;
			m_main.Settings = new Settings() { AutoSaveOnChanges = false };

			// Create a new settings UI for the tutorial
			m_settings_ui = new SettingsUI(m_main, SettingsUI.ETab.Highlights){StartPosition = FormStartPosition.CenterParent, Owner = m_main};
			m_current_page = new PageBase(this, m_main);

			m_html.Navigate("about:blank");

			m_btn_back.Click += (s,a) => SwitchPage(false);
			m_btn_next.Click += (s,a) => SwitchPage(true);
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref m_settings_ui);
			Util.Dispose(ref m_current_page);
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}
		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
			SwitchPage(true);
		}
		protected override void OnFormClosed(FormClosedEventArgs e)
		{
			// On shutdown, restore the original settings
			base.OnFormClosed(e);
			m_main.Settings = m_orig_settings;
			m_main.BringToFront();
		}

		/// <summary>Update the form to display the next/previous page</summary>
		private void SwitchPage(bool forward)
		{
			m_page_index += forward ? 1 : -1;
			if (m_page_index < ETutPage.First) { Close(); return; }
			if (m_page_index > ETutPage.Last ) { Close(); return; }
			m_btn_back.Text = m_page_index == ETutPage.First ? "&Close" : "&Back";
			m_btn_next.Text = m_page_index == ETutPage.Last  ? "&Close" : "&Next";

			m_current_page.Exit(forward);
			m_current_page.Dispose();
			m_current_page = PageFactory(m_page_index);
			m_current_page.Enter();
		}

		/// <summary>Set the content of the dialog</summary>
		public string Html
		{
			set
			{
				Debug.Assert(m_html.Document != null);
				m_html.Document.OpenNew(true);
				m_html.Document.Write(value ?? string.Empty);
				m_html.Refresh();
			}
		}

		/// <summary>Factory for making pages</summary>
		private PageBase PageFactory(ETutPage page)
		{
			switch (page)
			{
			default: throw new Exception("Unknown page number");
			case ETutPage.OpenLogData:      return new Main.TutOpenLogData(this, m_main);
			case ETutPage.EnableHighlights: return new Main.TutEnableHighlights(this, m_main);
			case ETutPage.EnableFilters:    return new Main.TutEnableFilters(this, m_main);
			case ETutPage.EnableTransforms: return new Main.TutEnableTransforms(this, m_main);
			case ETutPage.EnableActions:    return new Main.TutEnableActions(this, m_main);
			case ETutPage.FileWatching:     return new Main.TutFileWatching(this, m_main);
			case ETutPage.FileScroll:       return new Main.TutFileScroll(this, m_main);
			case ETutPage.PatternEditor:    return new SettingsUI.TutPatternEditor(this, m_main, m_settings_ui);
			case ETutPage.PatternsList:     return new SettingsUI.TutPatternsList(this, m_main, m_settings_ui);
			case ETutPage.PatternSets:      return new Main.TutPatternSets(this, m_main);
			case ETutPage.HelpMenu:         return new Main.TutHelpMenu(this, m_main);
			}
		}

		#region Windows Form Designer generated code

		/// <summary> Required designer variable. </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.m_btn_next = new System.Windows.Forms.Button();
			this.m_btn_back = new System.Windows.Forms.Button();
			this.m_html = new System.Windows.Forms.WebBrowser();
			this.m_panel = new System.Windows.Forms.Panel();
			this.m_panel.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_btn_next
			// 
			this.m_btn_next.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_next.Location = new System.Drawing.Point(154, 236);
			this.m_btn_next.Name = "m_btn_next";
			this.m_btn_next.Size = new System.Drawing.Size(118, 28);
			this.m_btn_next.TabIndex = 1;
			this.m_btn_next.Text = "&Next";
			this.m_btn_next.UseVisualStyleBackColor = true;
			// 
			// m_btn_back
			// 
			this.m_btn_back.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_back.Location = new System.Drawing.Point(30, 236);
			this.m_btn_back.Name = "m_btn_back";
			this.m_btn_back.Size = new System.Drawing.Size(118, 28);
			this.m_btn_back.TabIndex = 0;
			this.m_btn_back.Text = "&Cancel";
			this.m_btn_back.UseVisualStyleBackColor = true;
			// 
			// m_html
			// 
			this.m_html.AllowNavigation = false;
			this.m_html.AllowWebBrowserDrop = false;
			this.m_html.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_html.IsWebBrowserContextMenuEnabled = false;
			this.m_html.Location = new System.Drawing.Point(0, 0);
			this.m_html.MinimumSize = new System.Drawing.Size(20, 20);
			this.m_html.Name = "m_html";
			this.m_html.ScrollBarsEnabled = false;
			this.m_html.Size = new System.Drawing.Size(261, 216);
			this.m_html.TabIndex = 0;
			this.m_html.WebBrowserShortcutsEnabled = false;
			// 
			// m_panel
			// 
			this.m_panel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_panel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_panel.Controls.Add(this.m_html);
			this.m_panel.Location = new System.Drawing.Point(9, 12);
			this.m_panel.Name = "m_panel";
			this.m_panel.Size = new System.Drawing.Size(263, 218);
			this.m_panel.TabIndex = 3;
			// 
			// FirstRunTutorial
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(284, 274);
			this.ControlBox = false;
			this.Controls.Add(this.m_btn_back);
			this.Controls.Add(this.m_panel);
			this.Controls.Add(this.m_btn_next);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(279, 145);
			this.Name = "FirstRunTutorial";
			this.m_panel.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion
	}

	/// <summary>Base class for first run tutorial pages</summary>
	public class PageBase :IDisposable
	{
		protected readonly FirstRunTutorial m_tut;
		protected readonly Main m_main;
		protected readonly Overlay m_overlay;

		public PageBase(FirstRunTutorial tut, Main main)
		{
			m_tut       = tut;
			m_main      = main;
			m_overlay   = new Overlay();
		}
		public virtual void Dispose()
		{
			m_overlay.Dispose();
		}

		/// <summary>State entry</summary>
		public virtual void Enter()
		{
			m_tut.Html = Resources.firstrun.Replace("[Content]", HtmlContent);
			m_tut.PinTarget = PinTarget;
			m_tut.PinOffset = Offset;
		}

		/// <summary>State exit</summary>
		public virtual void Exit(bool forward)
		{}

		/// <summary>Return the window or control to pin the tutorial to</summary>
		protected virtual Control PinTarget
		{
			get { return m_main; }
		}

		/// <summary>The HTML to display in the tutorial window</summary>
		protected virtual string HtmlContent
		{
			get { return string.Empty; }
		}

		/// <summary>The screen space position at which to display the tutorial window</summary>
		protected virtual Point Offset
		{
			get { return new Point(-200, 80); }
		}

		/// <summary>Paints the overlay</summary>
		protected void PaintHighlight(Graphics gfx, Rectangle win_rect, Rectangle highlight_rect)
		{
			// Paint the blue layer
			highlight_rect.Inflate(2,2);
			using (gfx.SaveState())
			{
				gfx.SetClip(highlight_rect, CombineMode.Exclude);
				using (var b = Gdi.CreateRadialGradientBrush(highlight_rect.Centre(), 500, 500, Color.FromArgb(0xA0, Color.DarkBlue), Color.FromArgb(0x10, Color.LightSkyBlue)))
					gfx.FillRectangle(b, win_rect);
			}

			// Draw a border
			using (var pen0 = new Pen(Color.DarkBlue, 3f))
			using (var pen1 = new Pen(Color.LightSkyBlue, 5f))
			{
				gfx.DrawRectangle(pen0, highlight_rect);
				highlight_rect.Inflate(5,5);
				gfx.DrawRectangle(pen1, highlight_rect);
			}
		}
	}

	// These classes are within 'Main' so that they can access it's privates
	public partial class Main
	{
		public sealed class TutOpenLogData :PageBase
		{
			public TutOpenLogData(FirstRunTutorial tut, Main main) :base(tut, main)
			{
				m_overlay.SnapShotCaptured += (s,a) =>
				{
					using (a.Gfx.SaveState())
					{
						m_main.m_menu_file.ShowDropDown();
						m_main.m_menu_file_data_sources.ShowDropDown();
						var pt1 = m_main.PointToClient(m_main.m_menu_file.DropDown.Bounds.Location);
						var pt2 = m_main.PointToClient(m_main.m_menu_file_data_sources.DropDown.Bounds.Location);
						m_main.m_menu_file.HideDropDown();
						m_main.m_menu_file_data_sources.HideDropDown();

						// Fill the overlay with light blue, then redraw the file menu and drop downs
						PaintHighlight(a.Gfx, a.ClientRectangle, m_main.m_menu_file.ParentFormRectangle());
						a.Gfx.DrawImageUnscaled(m_main.m_menu_file.DropDown.ToBitmap(), pt1);
						a.Gfx.DrawImageUnscaled(m_main.m_menu_file_data_sources.DropDown.ToBitmap(), pt2);
					}
				};
			}
			public override void Enter()
			{
				base.Enter();
				m_main.Src = null;
				m_main.EnableHighlights(false);
				m_main.EnableFilters(false);
				m_main.EnableTransforms(false);
				m_main.EnableActions(false);
				m_main.m_grid.TryScrollToRowIndex(0);
				m_overlay.Attachee = m_main;

				var path = Util2.ResolveAppPath(ExampleFiles.LogFile);
				m_main.SetLineEnding(ELineEnding.Detect);
				m_main.SetEncoding(null);
				m_main.OpenSingleLogFile(path, false);
			}
			public override void Exit(bool forward)
			{
				base.Exit(forward);
				if (!forward)
					m_main.Src = null;
			}
			protected override string HtmlContent
			{
				get { return "To get started, open a log file, or capture streaming log data using one of the data source dialogs."; }
			}
		}
		public sealed class TutEnableHighlights :PageBase
		{
			public TutEnableHighlights(FirstRunTutorial tut, Main main) :base(tut, main)
			{
				m_overlay.SnapShotCaptured += (s,a) =>
					{
						PaintHighlight(a.Gfx, a.ClientRectangle, m_main.m_btn_highlights.ParentFormRectangle());
					};
			}
			public override void Enter()
			{
				base.Enter();
				m_main.EnableHighlights(true);
				m_main.EnableFilters(false);
				m_main.EnableTransforms(false);
				m_main.EnableActions(false);
				m_main.m_grid.TryScrollToRowIndex(7);
				m_overlay.Attachee = m_main;
			}
			public override void Exit(bool forward)
			{
				base.Exit(forward);
				m_main.EnableHighlights(forward);
			}
			protected override string HtmlContent
			{
				get
				{
					return
						"To highlight important lines in the log data you need to create highlighting patterns. Right click this " +
						"button to open the highlighting patterns editor. Left clicking this button will enable or disable highlighting.";
				}
			}
		}
		public sealed class TutEnableFilters :PageBase
		{
			public TutEnableFilters(FirstRunTutorial tut, Main main) :base(tut, main)
			{
				m_overlay.SnapShotCaptured += (s,a) =>
					{
						PaintHighlight(a.Gfx, a.ClientRectangle, m_main.m_btn_filters.ParentFormRectangle());
					};
			}
			public override void Enter()
			{
				base.Enter();
				m_main.EnableHighlights(true);
				m_main.EnableFilters(true);
				m_main.EnableTransforms(false);
				m_main.EnableActions(false);
				m_main.m_grid.TryScrollToRowIndex(14);
				m_overlay.Attachee = m_main;
			}
			public override void Exit(bool forward)
			{
				base.Exit(forward);
				m_main.EnableFilters(forward);
			}
			protected override string HtmlContent
			{
				get
				{
					return
						"You can filter unwanted lines from the log data using filter patterns. Right click this button to open the filtering " +
						"patterns editor. Left clicking this button will enable or disable filtering.";
				}
			}
		}
		public sealed class TutEnableTransforms :PageBase
		{
			public TutEnableTransforms(FirstRunTutorial tut, Main main) :base(tut, main)
			{
				m_overlay.SnapShotCaptured += (s,a) =>
				{
					PaintHighlight(a.Gfx, a.ClientRectangle, m_main.m_btn_transforms.ParentFormRectangle());
				};
			}
			public override void Enter()
			{
				base.Enter();
				m_main.EnableHighlights(true);
				m_main.EnableFilters(true);
				m_main.EnableTransforms(true);
				m_main.EnableActions(false);
				m_main.m_grid.FirstDisplayedScrollingRowIndex = 23;
				m_overlay.Attachee = m_main;
			}
			public override void Exit(bool forward)
			{
				base.Exit(forward);
				m_main.EnableTransforms(forward);
			}
			protected override string HtmlContent
			{
				get
				{
					return
						"Transforms can be used to rearrange the text or make text substitutions, a handy feature for replacing " +
						"error codes with descriptive error messages. Right click this button to open the transforms pattern editor, " +
						"left click to enable or disable transforms.";
				}
			}
		}
		public sealed class TutEnableActions :PageBase
		{
			public TutEnableActions(FirstRunTutorial tut, Main main) :base(tut, main)
			{
				m_overlay.SnapShotCaptured += (s,a) =>
					{
						PaintHighlight(a.Gfx, a.ClientRectangle, m_main.m_btn_actions.ParentFormRectangle());
					};
			}
			public override void Enter()
			{
				base.Enter();
				m_main.EnableHighlights(true);
				m_main.EnableFilters(true);
				m_main.EnableTransforms(true);
				m_main.EnableActions(true);
				m_main.m_grid.TryScrollToRowIndex(36);
				m_overlay.Attachee = m_main;
			}
			public override void Exit(bool forward)
			{
				base.Exit(forward);
				m_main.EnableActions(forward);
			}
			protected override string HtmlContent
			{
				get
				{
					return
						"Actions allow a program or script to run when a line in the log data is double clicked. " +
						"Actions are triggered based on the contents of the line, e.g. a text editor can be launched " +
						"for lines containing file paths. Right click to open the actions pattern editor, left click to " +
						"enable or disable actions.";
				}
			}
		}
		public sealed class TutFileWatching :PageBase
		{
			public TutFileWatching(FirstRunTutorial tut, Main main) :base(tut, main)
			{
				m_overlay.SnapShotCaptured += (s,a) =>
					{
						PaintHighlight(a.Gfx, a.ClientRectangle, m_main.m_btn_watch.ParentFormRectangle());
					};
			}
			public override void Enter()
			{
				base.Enter();
				m_main.EnableWatch(false);
				m_overlay.Attachee = m_main;
			}
			public override void Exit(bool forward)
			{
				base.Exit(forward);
				m_main.EnableWatch(false);
			}
			protected override string HtmlContent
			{
				get
				{
					return
						"Left click this button to enable or disable \"watching\" mode. When enabled, RyLogViewer watches for changes " +
						"to the log file or data source and automatically updates the display whenever there are changes.";
				}
			}
		}
		public sealed class TutFileScroll :PageBase
		{
			public TutFileScroll(FirstRunTutorial tut, Main main) :base(tut, main)
			{
				m_overlay.SnapShotCaptured += (s,a) =>
					{
						PaintHighlight(a.Gfx, a.ClientRectangle, m_main.m_scroll_file.ParentFormRectangle());
					};
			}
			public override void Enter()
			{
				base.Enter();
				m_overlay.Attachee = m_main;
			}
			protected override string HtmlContent
			{
				get
				{
					return
						"This bar shows the current position within the log file or captured log data. " +
						"It also shows the positions of bookmarks and selected lines. The darker shade of blue " +
						"shows the region currently visible, the lighter shade shows the portion of log data loaded into memory.";
				}
			}
			protected override Point Offset
			{
				get { return new Point(-350,130); }
			}
		}
		public sealed class TutPatternSets :PageBase
		{
			public TutPatternSets(FirstRunTutorial tut, Main main) :base(tut, main)
			{
				m_overlay.SnapShotCaptured += (s,a) =>
				{
					using (a.Gfx.SaveState())
					{
						m_main.m_menu_file.ShowDropDown();
						m_main.m_menu_file_pattern_set.ShowDropDown();
						var pt1 = m_main.PointToClient(m_main.m_menu_file.DropDown.Bounds.Location);
						var pt2 = m_main.PointToClient(m_main.m_menu_file_pattern_set.DropDown.Bounds.Location);
						m_main.m_menu_file.HideDropDown();
						m_main.m_menu_file_pattern_set.HideDropDown();

						// Fill the overlay with light blue, then redraw the file menu and drop downs
						PaintHighlight(a.Gfx, a.ClientRectangle, m_main.m_menu_file.ParentFormRectangle());
						a.Gfx.DrawImageUnscaled(m_main.m_menu_file.DropDown.ToBitmap(), pt1);
						a.Gfx.DrawImageUnscaled(m_main.m_menu_file_pattern_set.DropDown.ToBitmap(), pt2);
					}
				};
			}
			public override void Enter()
			{
				base.Enter();
				m_overlay.Attachee = m_main;
			}
			public override void Exit(bool forward)
			{
				base.Exit(forward);
			}
			protected override string HtmlContent
			{
				get
				{
					return
						"These menu options are used to save and load sets of patterns. Pattern sets are a convenient way to " +
						"store groups of patterns used for a particular log data format.";
				}
			}
		}
		public sealed class TutHelpMenu :PageBase
		{
			public TutHelpMenu(FirstRunTutorial tut, Main main) :base(tut, main)
			{
				m_overlay.SnapShotCaptured += (s,a) =>
					{
						PaintHighlight(a.Gfx, a.ClientRectangle, m_main.m_menu_help.ParentFormRectangle());
					};
			}
			public override void Enter()
			{
				base.Enter();
				m_overlay.Attachee = m_main;
			}
			public override void Exit(bool forward)
			{
				base.Exit(forward);
				m_main.Enabled = true;
			}
			protected override string HtmlContent
			{
				get { return "This covers the basics of RyLogViewer. More detailed information is available in the main documentation, found here."; }
			}
		}
	}
	public partial class SettingsUI
	{
		public sealed class TutPatternEditor :PageBase
		{
			private readonly SettingsUI m_ui;
			public TutPatternEditor(FirstRunTutorial tut, Main main, SettingsUI ui) :base(tut, main)
			{
				m_ui = ui;
				m_overlay.SnapShotCaptured += (s,a) =>
					{
						PaintHighlight(a.Gfx, a.ClientRectangle, m_ui.m_pattern_hl.ParentFormRectangle());
					};
			}
			public override void Enter()
			{
				base.Enter();
				m_tut.PinTarget = m_ui;
				m_main.Enabled = false;
				m_ui.Visible = true;
				m_overlay.Attachee = m_ui;
			}
			public override void Exit(bool forward)
			{
				base.Exit(forward);
				m_ui.Visible = forward;
				m_main.Enabled = !forward;
			}
			protected override Control PinTarget { get { return m_ui; } }
			protected override string HtmlContent
			{
				get
				{
					return
						"This is the pattern editor used to create highlighting, filtering, transform, and action patterns. " +
						"The pattern is applied to text in the test area allowing complex patterns to be tested and refined.";
				}
			}
			protected override Point Offset
			{
				get { return new Point(0,0); }
			}
		}
		public sealed class TutPatternsList :PageBase
		{
			private readonly SettingsUI m_ui;
			public TutPatternsList(FirstRunTutorial tut, Main main, SettingsUI ui) :base(tut, main)
			{
				m_ui = ui;
				m_overlay.SnapShotCaptured += (s,a) =>
					{
						PaintHighlight(a.Gfx, a.ClientRectangle, m_ui.m_grid_highlight.ParentFormRectangle());
					};
			}
			public override void Enter()
			{
				base.Enter();
				m_main.Enabled = false;
				m_ui.Visible = true;
				m_overlay.Attachee = m_ui;
			}
			public override void Exit(bool forward)
			{
				base.Exit(forward);
				m_main.Enabled = forward;
				m_ui.Visible = !forward;
			}
			protected override Control PinTarget { get { return m_ui; } }
			protected override string HtmlContent
			{
				get
				{
					return
						"This area contains the existing patterns. You can edit an individual pattern by clicking the pencil icon. " +
						"The order can be changed by dragging rows (click and drag in the leftmost column). " +
						"Patterns can be deleted by selecting rows and pressing the delete key.";
				}
			}
			protected override Point Offset
			{
				get { return new Point(0,0); }
			}
		}
	}
}
