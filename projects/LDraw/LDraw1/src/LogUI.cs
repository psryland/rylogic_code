using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gui.WinForms;
using Rylogic.Interop.Win32;
using Rylogic.Scintilla;
using ToolStripContainer = Rylogic.Gui.WinForms.ToolStripContainer;

namespace LDraw
{
	public class LogUI :BaseUI
	{
		#region UI Elements
		private ToolStripContainer m_tsc;
		private ImageList m_il_toolbar;
		private LogPanel m_text;
		#endregion

		public LogUI(Model model)
			:base(model, "Log")
		{
			InitializeComponent();
			ExternalLogHelper.LogUI = this;
			DockControl.DefaultDockLocation = new DockContainer.DockLocation(auto_hide:EDockSite.Right);
			DockControl.TabColoursActive = new DockContainer.OptionData().TabStrip.ActiveTab;
			DockControl.ActiveChanged += HandleActiveChanged;

			// Create the text panel for displaying the log
			m_text = m_tsc.ContentPanel.Controls.Add2(new LogPanel(this));

			SetupUI();
			UpdateUI();
		}
		protected override void Dispose(bool disposing)
		{
			ExternalLogHelper.LogUI = null;
			DockControl.ActiveChanged -= HandleActiveChanged;
			base.Dispose(disposing);
		}

		/// <summary>Reset the error log to empty</summary>
		public void Clear()
		{
			m_text.ClearAll();
		}

		/// <summary>Add text to the log</summary>
		public void AddErrorMessage(string text, bool? popout = null)
		{
			m_text.AppendText(text.EnsureNewLine());

			if (popout ?? Settings.UI.ShowErrorLogOnNewMessages)
			{
				DockControl.DockContainer.FindAndShow(this);
			}
			DockControl.TabColoursActive.Text = Color.Red;
			DockControl.InvalidateTab();
		}

		/// <summary>Add a message to the log</summary>
		public void AddMessage(string text, bool? popout = null)
		{
			if (!IsHandleCreated) return;
			if (InvokeRequired) { Model.Dispatcher.BeginInvoke(() => AddMessage(text, popout)); return; }

			m_text.AppendText(text.EnsureNewLine());

			if (popout ?? Settings.UI.ShowErrorLogOnNewMessages)
			{
				DockControl.DockContainer.FindAndShow(this);
			}
		}

		/// <summary>Set up UI Elements</summary>
		private void SetupUI()
		{
		}

		/// <summary>Update the state of UI Elements</summary>
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
		}

		/// <summary>Handle this window becoming active</summary>
		private void HandleActiveChanged(object sender, EventArgs e)
		{
			if (DockControl.IsActiveContent)
			{
				DockControl.TabColoursActive.Text = Color.Black;
				DockControl.InvalidateTab();
			}
		}

		#region Log Panel
		private class LogPanel :ScintillaCtrl
		{
			/// <summary>LogUI</summary>
			private readonly LogUI m_ui;

			public LogPanel(LogUI ui)
			{
				m_ui = ui;
				Dock = DockStyle.Fill;

				// Create styles for the log levels
				var fontname = Encoding.UTF8.GetBytes("tahoma");
				using (var fonth = GCHandle_.Alloc(fontname, GCHandleType.Pinned))
				{
					Cmd(Sci.SCI_STYLESETFONT, 0, fonth.Handle.AddrOfPinnedObject());
					Cmd(Sci.SCI_STYLESETFORE, 0, Color.Black.ToAbgr() & 0x00FFFFFF);
				}

				// Use our own context menu
				UsePopUp = false;
				ContextMenuStrip = new CMenu(ui);

				// Disable undo history
				UndoCollection = false;

				ScrollToBottom();
				Frozen = false;
			}

			/// <summary>Get/Set frozen mode</summary>
			public bool Frozen
			{
				get { return m_frozen; }
				set
				{
					if (m_frozen == value) return;
					m_frozen = value;

					// Indicate frozen mode with colour
					var col = (m_frozen ? 0x00e0e0e0 : SystemColors.Window.ToAbgr()) & 0x00FFFFFF;
					Cmd(Sci.SCI_STYLESETBACK, Sci.STYLE_DEFAULT, col);
					Cmd(Sci.SCI_STYLESETBACK, 0, col);

				//	// Indicate frozen mode in the status bar
				//	if (m_frozen)
				//		m_ui.Status.SetStatusMessage(msg:"Log Paused - scroll to bottom to continue", fr_color:Color.Blue);
				//	else
				//		m_ui.Status.SetStatusMessage();
				//
				//	if (!m_frozen)
				//	{
				//		ScrollToBottom();
				//		m_ui.SignalRefresh();
				//	}

					Invalidate();
				}
			}
			private bool m_frozen;

			/// <summary>Freeze the log if the last line is not visible</summary>
			private void CheckForFreeze()
			{
				var vis = VisibleLineIndexRange;
				var last = LineCount - 1;
				Frozen = !vis.Contains(last);
			}

			// Update the contents of the LogPanel from the log file
			public void UpdateView(Sci.CellBuf cells)
			{
				// Only update the text panel if not frozen
				if (Frozen)
					return;

				using (this.SuspendRedraw(true))
				{
					ClearAll();
					AppendStyledText(cells);
				}

				ScrollToBottom();
			}

			/// <summary>WndProc function for the scintilla control</summary>
			protected override void WndProc(ref Message m)
			{
				switch (m.Msg)
				{
				case Win32.WM_KEYDOWN:
					#region
					{
						var vk = Win32.ToVKey(m.WParam);

						// Allow navigation keys through
						if (vk == EKeyCodes.Up || vk == EKeyCodes.Down || vk == EKeyCodes.Left || vk == EKeyCodes.Right)
							break;

						// Handle the copy keyboard shortcut
						if (Win32.KeyDown(EKeyCodes.ControlKey) && vk == EKeyCodes.C)
							Cmd(Sci.SCI_COPY);

						return;
					}
					#endregion
				case Win32.WM_CHAR:
					return;
				}
				base.WndProc(ref m);
			}

			/// <summary>Watch for scrolling events</summary>
			protected override void OnScroll(ScrollEventArgs args)
			{
				base.OnScroll(args);
				CheckForFreeze();
			}

			/// <summary>Scroll on resize</summary>
			protected override void OnSizeChanged(EventArgs e)
			{
				// Resizing the window preserves the first visible line.
				// If not frozen, we want the last line visible
				base.OnSizeChanged(e);
				if (!Frozen) ScrollToBottom();
			}

			/// <summary>Context menu</summary>
			private class CMenu :ContextMenuStrip
			{
				private LogUI m_this;
				public CMenu(LogUI this_)
				{
					m_this = this_;
					using (this.SuspendLayout(false))
					{
						#region Clear
						{
						//	var item = Items.Add2(new ToolStripMenuItem("Clear", null));
						//	item.Click += (s,a) => m_this.ClearLog();
						}
						#endregion
					}
				}
			}
		}
		#endregion

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.m_tsc = new Rylogic.Gui.WinForms.ToolStripContainer();
			this.m_il_toolbar = new System.Windows.Forms.ImageList(this.components);
			this.m_tsc.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_tsc
			// 
			// 
			// m_tsc.ContentPanel
			// 
			this.m_tsc.ContentPanel.Size = new System.Drawing.Size(495, 630);
			this.m_tsc.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_tsc.Location = new System.Drawing.Point(0, 0);
			this.m_tsc.Name = "m_tsc";
			this.m_tsc.Size = new System.Drawing.Size(495, 630);
			this.m_tsc.TabIndex = 0;
			this.m_tsc.Text = "toolStripContainer1";
			// 
			// m_il_toolbar
			// 
			this.m_il_toolbar.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
			this.m_il_toolbar.ImageSize = new System.Drawing.Size(16, 16);
			this.m_il_toolbar.TransparentColor = System.Drawing.Color.Transparent;
			// 
			// LogUI
			// 
			this.Controls.Add(this.m_tsc);
			this.Name = "LogUI";
			this.Size = new System.Drawing.Size(495, 630);
			this.m_tsc.ResumeLayout(false);
			this.m_tsc.PerformLayout();
			this.ResumeLayout(false);

		}
		#endregion
	}

	// A global object to allow embedded CSharp script to write to the LogUI
	public static class ExternalLogHelper
	{
		public static LogUI LogUI { get; set; }
		public static void AddMessage(string message)
		{
			LogUI?.AddMessage(message);
		}
	}
}
