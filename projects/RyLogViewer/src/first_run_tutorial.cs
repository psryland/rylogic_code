using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using RyLogViewer.Properties;
using pr.extn;
using pr.gui;

namespace RyLogViewer
{
	public sealed class FirstRunTutorial :Form
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
		private readonly Settings m_settings;
		private readonly SettingsUI m_settings_ui;
		private readonly string m_settings_filepath;
		private readonly Overlay m_overlay;
		private ETutPage m_page_index;
		private PageBase m_current_page;
		private WebBrowser m_html;
		private Button m_btn_next;
		private Button m_btn_back;
		private Panel m_panel;

		public FirstRunTutorial(Main main, Settings settings)
		{
			InitializeComponent();
			Icon = main.Icon;

			m_main = main;
			m_settings = settings;
			m_settings_filepath = m_settings.Filepath;
			m_settings_ui = new SettingsUI(m_main, m_settings, SettingsUI.ETab.Highlights){StartPosition = FormStartPosition.CenterParent, Owner = m_main};
			m_overlay = new Overlay();
			m_overlay.Paint += OnOverlayPaint;
			m_page_index = ETutPage.First - 1;
			m_current_page = new TutDummy();

			m_main.CloseLogFile();

			var settings_path = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath) ?? string.Empty, @"examples\example_settings.xml");
			m_settings.Load(settings_path, true);
			m_main.ApplySettings();

			m_html.Navigate("about:blank");

			m_btn_back.Click += (s,a) => SwitchPage(false);
			m_btn_next.Click += (s,a) => SwitchPage(true);

			Shown += (s,a) =>
				{
					SwitchPage(true);
				};
			FormClosed += (s,a) =>
				{
					m_settings.Load(m_settings_filepath);
					m_main.ApplySettings();
				};
		}
		protected override void Dispose(bool disposing)
		{
			m_settings_ui.Dispose();
			m_overlay.Dispose();
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <summary>Update the form to display the next/previous page</summary>
		private void SwitchPage(bool forward)
		{
			m_page_index += forward ? 1 : -1;
			if (m_page_index < ETutPage.First) { Close(); return; }
			if (m_page_index > ETutPage.Last ) { Close(); return; }

			m_current_page.Exit(forward);
			m_current_page = PageFactory(m_page_index);
			m_current_page.Enter(forward);
			m_overlay.Invalidate();

			// Load the html into the web view and set the button text
			Html = m_current_page.Html;
			SetButtonText();

			// Move the window to the target position
			var location = m_current_page.Location(this);
			Location = location == Point.Empty ? new Point(m_main.Right - Width/2, m_main.Top + 50) : location;
		}

		/// <summary>Set the content of the dialog</summary>
		private string Html
		{
			set
			{
				Debug.Assert(m_html.Document != null);
				m_html.Document.OpenNew(true);
				m_html.Document.Write(value ?? string.Empty);
				m_html.Refresh();
			}
		}

		/// <summary>Paints the overlay</summary>
		private void OnOverlayPaint(object sender, PaintEventArgs args)
		{
			var rect = m_current_page.HighlightRect;
			if (rect == Rectangle.Empty) return;
			rect = m_overlay.RectangleToClient(rect);

			args.Graphics.CompositingMode = CompositingMode.SourceCopy;
			args.Graphics.CompositingQuality = CompositingQuality.GammaCorrected;
			args.Graphics.SmoothingMode = SmoothingMode.HighQuality;

			rect.Inflate(2, 2);
			args.Graphics.FillRectangle(m_overlay.TransparentBrush, rect);

			rect.Inflate(-1, -1);
			using (var pen = new Pen(Brushes.DarkBlue, 3f))
				args.Graphics.DrawRectangle(pen, rect);

			this.BringToFront();
		}

		/// <summary>Updates the text on the buttons for the given page index</summary>
		private void SetButtonText()
		{
			m_btn_back.Text = m_page_index == ETutPage.First ? "&Close" : "&Back";
			m_btn_next.Text = m_page_index == ETutPage.Last  ? "&Close" : "&Next";
		}

		/// <summary>Factory for making pages</summary>
		private PageBase PageFactory(ETutPage page)
		{
			switch (page)
			{
			default: throw new Exception("Unknown page number");
			case ETutPage.OpenLogData:      return new Main.TutOpenLogData(m_main, m_overlay);
			case ETutPage.EnableHighlights: return new Main.TutEnableHighlights(m_main, m_overlay);
			case ETutPage.EnableFilters:    return new Main.TutEnableFilters(m_main, m_overlay);
			case ETutPage.EnableTransforms: return new Main.TutEnableTransforms(m_main, m_overlay);
			case ETutPage.EnableActions:    return new Main.TutEnableActions(m_main, m_overlay);
			case ETutPage.FileWatching:     return new Main.TutFileWatching(m_main, m_overlay);
			case ETutPage.FileScroll:       return new Main.TutFileScroll(m_main, m_overlay);
			case ETutPage.PatternEditor:    return new SettingsUI.TutPatternEditor(m_settings_ui, m_overlay);
			case ETutPage.PatternsList:     return new SettingsUI.TutPatternsList(m_settings_ui, m_overlay);
			case ETutPage.PatternSets:      return new SettingsUI.TutPatternSets(m_settings_ui, m_overlay);
			case ETutPage.HelpMenu:         return new Main.TutHelpMenu(m_main, m_overlay);
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
			this.m_btn_next.Location = new System.Drawing.Point(133, 172);
			this.m_btn_next.Name = "m_btn_next";
			this.m_btn_next.Size = new System.Drawing.Size(118, 28);
			this.m_btn_next.TabIndex = 0;
			this.m_btn_next.Text = "&Next";
			this.m_btn_next.UseVisualStyleBackColor = true;
			//
			// m_btn_back
			//
			this.m_btn_back.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_back.Location = new System.Drawing.Point(9, 172);
			this.m_btn_back.Name = "m_btn_back";
			this.m_btn_back.Size = new System.Drawing.Size(118, 28);
			this.m_btn_back.TabIndex = 1;
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
			this.m_html.Size = new System.Drawing.Size(240, 152);
			this.m_html.TabIndex = 2;
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
			this.m_panel.Size = new System.Drawing.Size(242, 154);
			this.m_panel.TabIndex = 3;
			//
			// FirstRunTutorial
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
			this.ClientSize = new System.Drawing.Size(263, 210);
			this.Controls.Add(this.m_btn_back);
			this.Controls.Add(this.m_panel);
			this.Controls.Add(this.m_btn_next);
			this.DoubleBuffered = true;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FirstRunTutorial";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "First Run Tutorial";
			this.m_panel.ResumeLayout(false);
			this.ResumeLayout(false);
		}

		#endregion
	}

	// Some of these classes are within 'Main' so that they can access it's privates

	/// <summary>Base class for first run tutorial pages</summary>
	public abstract class PageBase
	{
		/// <summary>State entry</summary>
		public virtual void Enter(bool forward) {}

		/// <summary>State exit</summary>
		public virtual void Exit(bool forward) {}

		/// <summary>Returns the text content of the tutorial window</summary>
		public string Html { get { return Resources.firstrun.Replace("[Content]", HtmlContent); } }

		/// <summary>The HTML to display in the tutorial window</summary>
		protected abstract string HtmlContent { get; }

		/// <summary>The screen space position at which to display the tutorial window</summary>
		public virtual Point Location(Form tut) { return Point.Empty; }

		/// <summary>The rectangle to highlight that corresponds to the tutorial message</summary>
		public virtual Rectangle HighlightRect { get { return Rectangle.Empty; } }
	}

	public class TutDummy :PageBase
	{
		protected override string HtmlContent { get { return string.Empty; } }
	}

	public partial class Main
	{
		public sealed class TutOpenLogData :PageBase
		{
			private readonly Main m_main;
			private readonly Overlay m_overlay;
			public TutOpenLogData(Main main, Overlay overlay)
			{
				m_main = main;
				m_overlay = overlay;
			}
			public override void Enter(bool forward)
			{
				m_main.CloseLogFile();
				m_overlay.Attachee = m_main;
				m_main.m_menu_file.ShowDropDown();
				m_main.m_menu_file_data_sources.ShowDropDown();
			}
			public override void Exit(bool forward)
			{
				m_main.m_menu_file.HideDropDown();
				m_main.m_menu_file_data_sources.HideDropDown();

				if (forward)
				{
					var path = Misc.ResolveAppFile(@"examples\example logfile.txt");
					m_main.SetLineEnding(ELineEnding.Detect);
					m_main.SetEncoding(null);
					m_main.OpenSingleLogFile(path, false);
				}
			}
			protected override string HtmlContent
			{
				get { return "To get started, open a log file, or capture streaming log data using one of the data source dialogs."; }
			}
			public override Point Location(Form tut)
			{
				return m_main.m_menu_file_data_sources.DropDown.ScreenRectangle().TopRight().Shifted(10,0);
			}
			public override Rectangle HighlightRect
			{
				get { return m_main.m_menu_file.ScreenRectangle(); }
			}
		}
		public sealed class TutEnableHighlights :PageBase
		{
			private readonly Main m_main;
			private readonly Overlay m_overlay;
			public TutEnableHighlights(Main main, Overlay overlay)
			{
				m_main = main;
				m_overlay = overlay;
			}
			public override void Enter(bool forward)
			{
				m_main.EnableHighlights(false);
				m_overlay.Attachee = m_main;
			}
			public override void Exit(bool forward)
			{
				if (forward)
					m_main.EnableHighlights(true);
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
			public override Point Location(Form tut)
			{
				return HighlightRect.TopRight().Shifted(10,0);
			}
			public override Rectangle HighlightRect
			{
				get { return m_main.m_btn_highlights.ScreenRectangle(); }
			}
		}
		public sealed class TutEnableFilters :PageBase
		{
			private readonly Main m_main;
			private readonly Overlay m_overlay;
			public TutEnableFilters(Main main, Overlay overlay)
			{
				m_main = main;
				m_overlay = overlay;
			}
			public override void Enter(bool forward)
			{
				m_main.EnableFilters(false);
				m_overlay.Attachee = m_main;
			}
			public override void Exit(bool forward)
			{
				if (forward)
					m_main.EnableFilters(true);
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
			public override Point Location(Form tut)
			{
				return HighlightRect.TopRight().Shifted(10,0);
			}
			public override Rectangle HighlightRect
			{
				get { return m_main.m_btn_filters.ScreenRectangle(); }
			}
		}
		public sealed class TutEnableTransforms :PageBase
		{
			private readonly Main m_main;
			private readonly Overlay m_overlay;
			public TutEnableTransforms(Main main, Overlay overlay)
			{
				m_main = main;
				m_overlay = overlay;
			}
			public override void Enter(bool forward)
			{
				m_main.EnableTransforms(false);
				m_overlay.Attachee = m_main;
			}
			public override void Exit(bool forward)
			{
				if (forward)
					m_main.EnableTransforms(true);
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
			public override Point Location(Form tut)
			{
				return HighlightRect.TopRight().Shifted(10,0);
			}
			public override Rectangle HighlightRect
			{
				get { return m_main.m_btn_transforms.ScreenRectangle(); }
			}
		}
		public sealed class TutEnableActions :PageBase
		{
			private readonly Main m_main;
			private readonly Overlay m_overlay;
			public TutEnableActions(Main main, Overlay overlay)
			{
				m_main = main;
				m_overlay = overlay;
			}
			public override void Enter(bool forward)
			{
				m_main.EnableActions(false);
				m_overlay.Attachee = m_main;
			}
			public override void Exit(bool forward)
			{
				if (forward)
					m_main.EnableActions(true);
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
			public override Point Location(Form tut)
			{
				return HighlightRect.TopRight().Shifted(10,0);
			}
			public override Rectangle HighlightRect
			{
				get { return m_main.m_btn_actions.ScreenRectangle(); }
			}
		}
		public sealed class TutFileWatching :PageBase
		{
			private readonly Main m_main;
			private readonly Overlay m_overlay;
			public TutFileWatching(Main main, Overlay overlay)
			{
				m_main = main;
				m_overlay = overlay;
			}
			public override void Enter(bool forward)
			{
				m_main.EnableWatch(false);
				m_overlay.Attachee = m_main;
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
			public override Point Location(Form tut)
			{
				return HighlightRect.TopRight().Shifted(10,0);
			}
			public override Rectangle HighlightRect
			{
				get { return m_main.m_btn_watch.ScreenRectangle(); }
			}
		}
		public sealed class TutFileScroll :PageBase
		{
			private readonly Main m_main;
			private readonly Overlay m_overlay;
			public TutFileScroll(Main main, Overlay overlay)
			{
				m_main = main;
				m_overlay = overlay;
			}
			public override void Enter(bool forward)
			{
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
			public override Point Location(Form tut)
			{
				return HighlightRect.TopLeft().Shifted(-(tut.Width + 20), 40);
			}
			public override Rectangle HighlightRect
			{
				get { return m_main.m_scroll_file.ScreenRectangle(); }
			}
		}
	}

	public partial class SettingsUI
	{
		public sealed class TutPatternEditor :PageBase
		{
			private readonly SettingsUI m_ui;
			private readonly Overlay m_overlay;
			public TutPatternEditor(SettingsUI ui, Overlay overlay)
			{
				m_ui = ui;
				m_overlay = overlay;
			}
			public override void Enter(bool forward)
			{
				m_ui.Visible = true;
				m_overlay.Attachee = m_ui;
			}
			public override void Exit(bool forward)
			{
				if (!forward)
					m_ui.Visible = false;
			}
			protected override string HtmlContent
			{
				get
				{
					return
						"This is the pattern editor used to create highlighting patterns. The pattern editor is identical for " +
						"filters and actions, and very similar for transforms.";
				}
			}
			public override Point Location(Form tut)
			{
				return HighlightRect.TopRight().Shifted(10, 0);
			}
			public override Rectangle HighlightRect
			{
				get { return m_ui.m_pattern_hl.ScreenRectangle(); }
			}
		}
		public sealed class TutPatternsList :PageBase
		{
			private readonly SettingsUI m_ui;
			private readonly Overlay m_overlay;
			public TutPatternsList(SettingsUI ui, Overlay overlay)
			{
				m_ui = ui;
				m_overlay = overlay;
			}
			public override void Enter(bool forward)
			{
				m_ui.Visible = true;
				m_overlay.Attachee = m_ui;
			}
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
			public override Point Location(Form tut)
			{
				return HighlightRect.TopRight().Shifted(10, 0);
			}
			public override Rectangle HighlightRect
			{
				get { return m_ui.m_grid_highlight.ScreenRectangle(); }
			}
		}
		public sealed class TutPatternSets :PageBase
		{
			private readonly SettingsUI m_ui;
			private readonly Overlay m_overlay;
			public TutPatternSets(SettingsUI ui, Overlay overlay)
			{
				m_ui = ui;
				m_overlay = overlay;
			}
			public override void Enter(bool forward)
			{
				m_ui.Visible = true;
				m_overlay.Attachee = m_ui;
			}
			public override void Exit(bool forward)
			{
				if (forward)
					m_ui.Visible = false;
			}
			protected override string HtmlContent
			{
				get
				{
					return
						"These controls are used to save and load sets of patterns. Pattern sets are a convenient way to " +
						"store groups of patterns used for a particular log data format.";
				}
			}
			public override Point Location(Form tut)
			{
				return HighlightRect.TopRight().Shifted(10, -tut.Height / 2);
			}
			public override Rectangle HighlightRect
			{
				get { return m_ui.m_pattern_set_hl.ScreenRectangle(); }
			}
		}
	}

	public partial class Main
	{
		public sealed class TutHelpMenu :PageBase
		{
			private readonly Main m_main;
			private readonly Overlay m_overlay;
			public TutHelpMenu(Main main, Overlay overlay)
			{
				m_main = main;
				m_overlay = overlay;
				m_main.m_menu_help.ShowDropDown();
			}
			public override void Enter(bool forward)
			{
				m_overlay.Attachee = m_main;
			}
			protected override string HtmlContent
			{
				get { return "This covers the basics of RyLogViewer. More detailed information can be found in the main documentation found here."; }
			}
			public override Point Location(Form tut)
			{
				return m_main.m_menu_help.DropDown.ScreenRectangle().TopRight().Shifted(10, 0);
			}
			public override Rectangle HighlightRect
			{
				get { return m_main.m_menu_help.ScreenRectangle(); }
			}
		}
	}
}
