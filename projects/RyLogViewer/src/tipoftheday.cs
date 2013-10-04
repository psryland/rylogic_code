using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using RyLogViewer.Properties;
using pr.extn;
using pr.script;

namespace RyLogViewer
{
	public class TipOfTheDay :Form
	{
		#region Tip Html
		private struct Tip { public string Title; public string Body; }
		private static readonly Tip[] m_tips = new[]
			{
				new Tip
				{
					Title = "Welcome!",
					Body  = "RyLogViewer is an application designed to make the viewing of log files or streaming " +
							"log data easier through the use of highlighting, filtering, and text transformations. " +
							"However, that is just the beginning of what it can do!<br/><br/>" +
							"Click next to explore some of the features of RyLogViewer"
				}
				,new Tip
				{
					Title = "Getting Started",
					Body  = "To begin experimenting with some of the features of RyLogViewer, load the " +
							"<a href='cmd://open_example_logfile'><i>example logfile.txt</i></a> from the " +
							"examples directory.<br/>"
				}
				,new Tip
				{
					Title = "Pattern Sets",
					Body  = "RyLogViewer allows highlighting and filtering patterns that you use frequently " +
					        "to be saved as 'Pattern Sets'. Once saved these can be selected quickly from " +
							"a drop-down list.<br/><br/>" +
							"Highlighting and filtering patterns are configured under the Options menu.",
				}
				,new Tip
				{
					Title = "Tool Tips",
					Body  = "Almost every UI element in RyLogViewer has a popup tool tip. If you're unsure about " +
							"any part of the application, hover your mouse over it to read a quick description. " +
							"If you would like more information, the main application documentation is found under " +
							"the help menu."
				}
				,new Tip
				{
					Title = "Export",
					Body  = "The export feature allows loaded data to be exported to a new file. Transforms and " +
					        "filtering are applied during the export process, which means RyLogViewer can be used " +
					        "as a powerful text data transformation tool. There is also command line support for " +
					        "exporting allowing RyLogViewer to be used in batch files or scripting tasks."
				}
			};
				//<p>if you need more than one column delimiter, use a string transform to turn the column delimiters into one common column delimiter, then set that to the column delimiter</p>
				//<p>command line interface</p>-->
		#endregion

		private readonly Main m_main;
		private readonly Random m_rng;
		private readonly List<int> m_order;
		private Panel m_panel;
		private WebBrowser m_html;
		private Button m_btn_ok;
		private Button m_btn_next;
		private Button m_btn_prev;
		private CheckBox m_check_show_on_startup;

		public TipOfTheDay(Main main, Settings settings)
		{
			InitializeComponent();
			m_main      = main;
			m_rng       = new Random();
			m_order     = SetOrder(settings.FirstRun);
			m_tip_index = 0;

			m_html.Navigating += OnNavigating;
			m_html.Navigate("about:blank");

			m_check_show_on_startup.Checked = settings.ShowTOTD;
			m_check_show_on_startup.CheckedChanged += (s,a)=>
				{
					settings.ShowTOTD = m_check_show_on_startup.Checked;
				};
			m_btn_next.Click += (s,a)=>
				{
					TipIndex++;
					ShowTip();
				};
			m_btn_prev.Click += (s,a) =>
				{
					TipIndex += m_order.Count - 1;
					ShowTip();
				};
			Shown += (s,a) =>
				{
					ShowTip();
				};
		}

		/// <summary>Get/Set the currently displayed tip. If the index supplied doesn't exist, the TotD0 is displayed</summary>
		public int TipIndex
		{
			get { return m_tip_index; }
			set { m_tip_index = value % m_order.Count; }
		}
		private int m_tip_index;

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

		/// <summary>Show the totd for the current index</summary>
		private void ShowTip()
		{
			var tip = m_tips[m_order[TipIndex]];
			using (var tr = new TemplateReplacer(new StringSrc(Resources.totd), @"\[(\w+)\]", (x,match) =>
				{
					switch (match.Value)
					{
					default: throw new Exception("Unknown template field");
					case "[Title]":   return tip.Title;
					case "[Content]": return tip.Body;
					}
				}))
				Html = tr.ReadToEnd();
		}

		/// <summary>Choose an order for the tips of the day</summary>
		private List<int> SetOrder(bool first_run)
		{
			// On the first run, play the tips of the day in order
			// On subsequent runs, shuffle them
			var order = new List<int>(Enumerable.Range(0, m_tips.Length));
			if (!first_run)
			{
				order.RemoveAt(0);
				var count = order.Count;
				for (int i = 0; i != 2 * count; ++i)
					order.Swap(m_rng.Next(count), m_rng.Next(count));
			}
			return order;
		}

		/// <summary>Handle special navigation urls in totd's</summary>
		private void OnNavigating(object sender, WebBrowserNavigatingEventArgs args)
		{
			if (args.Url.Scheme == "cmd")
			{
				args.Cancel = true;
				switch (args.Url.Host)
				{
				case "open_example_logfile":
					m_main.SetLineEnding(ELineEnding.Detect);
					m_main.SetEncoding(null);
					m_main.OpenSingleLogFile(Misc.ResolveAppFile(@"examples\example logfile.txt"), false);
					Close();
					break;
				}
			}
		}

		#region Windows Form Designer generated code

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

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TipOfTheDay));
			this.m_btn_next = new System.Windows.Forms.Button();
			this.m_check_show_on_startup = new System.Windows.Forms.CheckBox();
			this.m_html = new System.Windows.Forms.WebBrowser();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_panel = new System.Windows.Forms.Panel();
			this.m_btn_prev = new System.Windows.Forms.Button();
			this.m_panel.SuspendLayout();
			this.SuspendLayout();
			//
			// m_btn_next
			//
			this.m_btn_next.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_next.Location = new System.Drawing.Point(185, 244);
			this.m_btn_next.Name = "m_btn_next";
			this.m_btn_next.Size = new System.Drawing.Size(63, 23);
			this.m_btn_next.TabIndex = 0;
			this.m_btn_next.Text = "&Next";
			this.m_btn_next.UseVisualStyleBackColor = true;
			//
			// m_check_show_on_startup
			//
			this.m_check_show_on_startup.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_check_show_on_startup.AutoSize = true;
			this.m_check_show_on_startup.Location = new System.Drawing.Point(12, 248);
			this.m_check_show_on_startup.MinimumSize = new System.Drawing.Size(103, 17);
			this.m_check_show_on_startup.Name = "m_check_show_on_startup";
			this.m_check_show_on_startup.Size = new System.Drawing.Size(103, 17);
			this.m_check_show_on_startup.TabIndex = 1;
			this.m_check_show_on_startup.Text = "Show on startup";
			this.m_check_show_on_startup.UseVisualStyleBackColor = true;
			//
			// m_html
			//
			this.m_html.AllowWebBrowserDrop = false;
			this.m_html.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_html.IsWebBrowserContextMenuEnabled = false;
			this.m_html.Location = new System.Drawing.Point(0, 0);
			this.m_html.MinimumSize = new System.Drawing.Size(20, 20);
			this.m_html.Name = "m_html";
			this.m_html.ScriptErrorsSuppressed = true;
			this.m_html.Size = new System.Drawing.Size(321, 233);
			this.m_html.TabIndex = 2;
			//
			// m_btn_ok
			//
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_ok.Location = new System.Drawing.Point(254, 244);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(63, 23);
			this.m_btn_ok.TabIndex = 3;
			this.m_btn_ok.Text = "&Close";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			//
			// m_panel
			//
			this.m_panel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_panel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_panel.Controls.Add(this.m_html);
			this.m_panel.Location = new System.Drawing.Point(4, 3);
			this.m_panel.Name = "m_panel";
			this.m_panel.Size = new System.Drawing.Size(323, 235);
			this.m_panel.TabIndex = 5;
			//
			// m_btn_prev
			//
			this.m_btn_prev.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_prev.Location = new System.Drawing.Point(116, 244);
			this.m_btn_prev.Name = "m_btn_prev";
			this.m_btn_prev.Size = new System.Drawing.Size(63, 23);
			this.m_btn_prev.TabIndex = 6;
			this.m_btn_prev.Text = "&Previous";
			this.m_btn_prev.UseVisualStyleBackColor = true;
			//
			// TipOfTheDay
			//
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_ok;
			this.ClientSize = new System.Drawing.Size(330, 271);
			this.Controls.Add(this.m_btn_prev);
			this.Controls.Add(this.m_panel);
			this.Controls.Add(this.m_check_show_on_startup);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_btn_next);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MinimumSize = new System.Drawing.Size(346, 305);
			this.Name = "TipOfTheDay";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Tip Of The Day";
			this.m_panel.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();
		}

		#endregion
	}
}
