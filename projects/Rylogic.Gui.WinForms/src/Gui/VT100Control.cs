using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Interop.Win32;
using Rylogic.Scintilla;
using Rylogic.Utility;

namespace Rylogic.Gui.WinForms
{
	/// <summary>A control that displays the VT100 buffer</summary>
	public class VT100Display :ScintillaCtrl
	{
		private static readonly string FileFilters = Util.FileDialogFilter("Text Files","*.txt","Log Files","*.log","All Files","*.*");

		private HoverScroll m_hs;
		private EventBatcher m_eb;
		private Dictionary<VT100.Style, byte> m_sty; // map from vt100 style to scintilla style index
		private Sci.CellBuf m_cells;

		public VT100Display(VT100.Buffer buf)
		{
			m_hs = new HoverScroll(Handle);
			m_eb = new EventBatcher(UpdateText, TimeSpan.FromMilliseconds(1)){TriggerOnFirst = true};
			m_sty = new Dictionary<VT100.Style,byte>();
			m_cells = new Sci.CellBuf();
			ContextMenuStrip = new CMenu(this);
				
			BlinkTimer = new System.Windows.Forms.Timer{Interval = 1000, Enabled = false};
			BlinkTimer.Tick += SignalRefresh;

			AllowDrop = true;
			AutoScrollToBottom = true;
			ScrollToBottomOnInput = true;
			EndAtLastLine = true;

			// Use our own context menu
			UsePopUp = false;

			// Turn off undo history
			UndoCollection = false;

			Buffer = buf;
		}
		protected override void Dispose(bool disposing)
		{
			Buffer = null;
			Util.Dispose(ref m_hs);
			Util.Dispose(ref m_eb);
			base.Dispose(disposing);
		}

		/// <summary>Return the vt100 settings</summary>
		[Browsable(false)]
		public VT100.Settings Settings
		{
			get { return Buffer != null ? Buffer.Settings : null; }
		}

		/// <summary>Get/Set the underlying VT100 buffer</summary>
		[Browsable(false)]
		public VT100.Buffer Buffer
		{
			get { return m_buffer; }
			set
			{
				if (m_buffer == value) return;
				if (m_buffer != null)
				{
					m_buffer.CaptureToFileEnd();
					m_buffer.Overflow -= HandleBufferOverflow;
					m_buffer.BufferChanged -= UpdateText;
					ContextMenuStrip = null;
				}
				m_buffer = value;
				if (m_buffer != null)
				{
					m_buffer.Overflow += HandleBufferOverflow;
					m_buffer.BufferChanged += UpdateText;
					ContextMenuStrip = new CMenu(this);
				}
				SignalRefresh();

				// Handler
				void HandleBufferOverflow(object sender, VT100BufferOverflowEventArgs args)
				{
					// Handle buffer lines being dropped</summary>
					if (args.Before)
					{
						// Calculate the number of bytes dropped.
						// Note: Buffer does not store line endings, but the saved selection does.
						var bytes = 0;
						for (int i = args.Dropped.Begi; i != args.Dropped.Endi; ++i)
							bytes += Encoding.UTF8.GetByteCount(Buffer.Lines[i].m_line.ToString()) + 1; // +1 '\n' per line

						// Save the selection and adjust it by the number of characters to be dropped
						m_sel = Selection.Save(this);
						m_sel.m_current -= bytes;
						m_sel.m_anchor -= bytes;

						// Remove the scrolled text from the control
						DeleteRange(0, bytes);

						// Scroll up to move with the buffer text
						var vis = FirstVisibleLine;
						vis = Math.Max(0, vis - args.Dropped.Sizei);
						FirstVisibleLine = vis;
					}
					else
					{
						Selection.Restore(this, m_sel);
					}
				}
			}
		}
		private VT100.Buffer m_buffer;
		private Selection m_sel;

		/// <summary>Timer that causes refreshes once a seconds</summary>
		[Browsable(false)]
		public System.Windows.Forms.Timer BlinkTimer { get; private set; }

		/// <summary>True if the display automatically scrolls to the bottom</summary>
		[Browsable(false)]
		public bool AutoScrollToBottom { get; set; }

		/// <summary>True if adding input causes the display to automatically scroll to the bottom</summary>
		[Browsable(false)]
		public bool ScrollToBottomOnInput { get; set; }

		/// <summary>Request an update of the display</summary>
		public void SignalRefresh(object sender = null, EventArgs args = null)
		{
			m_eb.Signal();
		}

		/// <summary>Clear the buffer and the display</summary>
		public new void Clear()
		{
			if (Buffer != null)
				Buffer.Clear();

			ClearAll();
		}

		/// <summary>Copy the current selection</summary>
		public new void Copy()
		{
			base.Copy();
		}

		/// <summary>Paste the clipboard into the input buffer</summary>
		public new void Paste()
		{
			if (Clipboard.ContainsText())
				Buffer.AddInput(Clipboard.GetText());
		}

		/// <summary>Adds text to the input buffer</summary>
		protected virtual void AddToBuffer(EKeyCodes vk)
		{
			// If input triggers auto scroll
			if (ScrollToBottomOnInput)
			{
				AutoScrollToBottom = true;
				ScrollToBottom();
			}

			// Try to add the raw key code first. If not handled,
			// try to convert it to a character and add it again.
			if (!Buffer.AddInput(vk))
			{
				if (Win32.CharFromVKey(vk, out var ch))
					Buffer.AddInput(ch);
			}
		}

		/// <summary>Start or stop capturing to a file</summary>
		public void CaptureToFile(bool start)
		{
			if (Buffer == null) return;
			if (start)
			{
				using (var dlg = new ChooseCaptureFile())
				{
					if (dlg.ShowDialog(this) != DialogResult.OK) return;
					try
					{
						Buffer.CaptureToFile(dlg.Filepath, dlg.CaptureAllData);
					}
					catch (Exception ex)
					{
						MessageBox.Show(this, $"Capture to file could not start\r\n{ex.Message}", "Capture To File", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			}
			else
			{
				Buffer.CaptureToFileEnd();
			}
		}

		/// <summary>True if capturing to file is currently enabled</summary>
		public bool CapturingToFile
		{
			get { return Buffer != null && Buffer.CapturingToFile; }
		}

		/// <summary>Send a file to the terminal</summary>
		public void SendFile()
		{
			if (Buffer == null) return;
			using (var dlg = new OpenFileDialog { Title = "Choose the file to send", Filter = FileFilters })
			{
				if (dlg.ShowDialog(this) != DialogResult.OK) return;
				Buffer.SendFile(dlg.FileName);
			}
		}

		/// <summary>Refresh the control with text from the vt100 buffer</summary>
		private void UpdateText(object sender = null, EventArgs args = null)
		{
			var buf = Buffer;

			// No buffer = empty display
			if (buf == null)
			{
				ClearAll();
				return;
			}

			// Get the buffer region that has changed
			var region = buf.InvalidRect;
			buf.ValidateRect();
			if (region.IsEmpty)
				return;

			using (this.SuspendRedraw(true))
			using (ScrollScope())
			{
				// Grow the text in the control to the required number of lines (if necessary)
				var line_count = LineCount;
				if (line_count < region.Bottom)
				{
					var pad = new byte[region.Bottom - line_count].Memset(0x0a);
					using (var p = GCHandle_.Alloc(pad, GCHandleType.Pinned))
						Cmd(Sci.SCI_APPENDTEXT, pad.Length, p.Handle.AddrOfPinnedObject());
				}

				// Update the text in the control from the invalid buffer region
				for (int i = region.Top, iend = region.Bottom; i != iend; ++i)
				{
					// Update whole lines, to hard to bother with x ranges
					// Note, the invalid region can be outside the buffer when the buffer gets cleared
					if (i >= buf.LineCount) break;
					var line = buf.Lines[i];

					byte sty = 0;
					foreach (var span in line.Spans)
					{
						var str = Encoding.UTF8.GetBytes(span.m_str);
						sty = SciStyle(span.m_sty);
						foreach (var c in str)
							m_cells.Add(c, sty);
					}
					m_cells.Add(0x0a, sty);
				}

				// Remove the last newline if the last line updated is also the last line in the buffer,
				// and append a null terminator so that 'InsertStyledText' can determine the length.
				if (buf.LineCount == region.Bottom) m_cells.Pop(1);
				m_cells.Add(0, 0);

				// Determine the character range to be updated
				var beg = PositionFromLine(region.Top);
				var end = PositionFromLine(region.Bottom);
				if (beg < 0) beg = 0;
				if (end < 0) end = TextLength;

				// Overwrite the visible lines with the buffer of cells
				DeleteRange(beg, end - beg);
				InsertStyledText(beg, m_cells);

				// Reset, ready for next time
				m_cells.Length = 0;
			}

			// Auto scroll to the bottom if told to and the last line is off the bottom 
			if (AutoScrollToBottom)
				ScrollToBottom();
		}

		/// <summary>Return the scintilla style index for the given vt100 style</summary>
		private byte SciStyle(VT100.Style sty)
		{
			byte idx;
			if (!m_sty.TryGetValue(sty, out idx))
			{
				idx = (byte)Math.Min(m_sty.Count, 255);

				var forecol = VT100.HBGR.ToColor(!sty.RevserseVideo ? sty.ForeColour : sty.BackColour);
				var backcol = VT100.HBGR.ToColor(!sty.RevserseVideo ? sty.BackColour : sty.ForeColour);
				StyleSetFont(idx, "consolas");
				StyleSetFore(idx, forecol);
				StyleSetBack(idx, backcol);
				StyleSetBold(idx, sty.Bold);
				StyleSetUnderline(idx, sty.Underline);
				m_sty[sty] = idx;
			}
			return idx;
		}

		/// <summary>Intercept the WndProc for this control</summary>
		protected override void WndProc(ref Message m)
		{
			//Win32.WndProcDebug(hwnd, msg, wparam, lparam, "vt100");
			switch (m.Msg)
			{
			case Win32.WM_KEYDOWN:
				#region
				{
					var ks = new Win32.KeyState(m.LParam);
					if (!ks.Alt && Buffer != null)
					{
						var vk = Win32.ToVKey(m.WParam);

						// Forward navigation keys to the control
						if (vk == EKeyCodes.Up || vk == EKeyCodes.Down || vk == EKeyCodes.Left || vk == EKeyCodes.Right)
						{
							Cmd(m.Msg, m.WParam, m.LParam);
							return;
						}

						// Handle clipboard shortcuts
						if (Win32.KeyDown(EKeyCodes.ControlKey))
						{
							if (vk == EKeyCodes.C) { Copy(); return; }
							if (vk == EKeyCodes.V) { Paste(); return; }
							return; // Disable all other shortcuts
						}

						// Add the key press to the buffer input
						AddToBuffer(vk);

						// Let the key events through if local echo is on
						if (Buffer.Settings.LocalEcho)
							break;

						// Block key events from getting to the ctrl
						return;
					}
					break;
				}
				#endregion
			case Win32.WM_CHAR:
				#region
				{
					return;
				}
			#endregion
			case Win32.WM_DROPFILES:
				#region
				{
					var drop_info = m.WParam;
					var count = Shell32.DragQueryFile(drop_info, 0xFFFFFFFFU, null, 0);
					var files = new List<string>();
					for (int i = 0; i != count; ++i)
					{
						var sb = new StringBuilder((int)Shell32.DragQueryFile(drop_info, (uint)i, null, 0) + 1);
						if (Shell32.DragQueryFile(drop_info, (uint)i, sb, (uint)sb.Capacity) == 0)
							throw new Exception("Failed to query file name from dropped files");
						files.Add(sb.ToString());
						sb.Clear();
					}
					HandleDropFiles(files);
					return;
				}
				#endregion
			}
			base.WndProc(ref m);
		}

		/// <summary>Scroll event</summary>
		protected override void OnScroll(ScrollEventArgs args)
		{
			base.OnScroll(args);

			// Turn on auto scroll if the last line is visible
			var vis = VisibleLineIndexRange;
			var last = LineCount - 1;
			AutoScrollToBottom = vis.Contains(last);
		}

		/// <summary>Drag drop event</summary>
		protected virtual void HandleDropFiles(IEnumerable<string> files)
		{
			if (Buffer == null) return;
			foreach (var file in files)
				Buffer.SendFile(file);
		}

		/// <summary>The context menu for this vt100 display</summary>
		private class CMenu :ContextMenuStrip
		{
			private VT100Display m_disp;
			public CMenu(VT100Display disp)
			{
				m_disp = disp;
				using (this.SuspendLayout(false))
				{
					#region Clear
					{
						var item = Items.Add2(new ToolStripMenuItem("Clear", null));
						item.Click += (s,e) => m_disp.Clear();
					}
					#endregion
					Items.Add(new ToolStripSeparator());
					#region Copy
					{
						var item = Items.Add2(new ToolStripMenuItem("Copy", null));
						item.Click += (s,e) => m_disp.Copy();
					}
					#endregion
					#region Paste
					{
						var item = Items.Add2(new ToolStripMenuItem("Paste", null));
						item.Click += (s,e)=> m_disp.Paste();
					}
					#endregion
					Items.Add(new ToolStripSeparator());
					#region Capture To File
					{
						var item = Items.Add2(new ToolStripMenuItem("Capture To File", null));
						Opening += (s,a) =>
							{
								item.Enabled = m_disp.Buffer != null;
								item.Checked = m_disp.CapturingToFile;
							};
						item.Click += (s,e) =>
							{
								m_disp.CaptureToFile(!m_disp.CapturingToFile);
							};
					}
					#endregion
					#region Send File
					{
						var item = Items.Add2(new ToolStripMenuItem("Send File", null));
						Opening += (s,a) =>
							{
								item.Enabled = m_disp.Buffer != null;
							};
						item.Click += (s,e) =>
							{
								m_disp.SendFile();
							};
					}
					#endregion
					if (m_disp.Settings != null)
					{
						Items.Add(new ToolStripSeparator());
						var options = Items.Add2(new ToolStripMenuItem("Terminal Options", null));

						#region Local Echo
						{
							var item = options.DropDownItems.Add2(new ToolStripMenuItem("Local Echo"){CheckOnClick = true});
							options.DropDown.Opening += (s,a) =>
								{
									item.Checked = m_disp.Settings.LocalEcho;
								};
							item.Click += (s,a) =>
								{
									m_disp.Settings.LocalEcho = item.Checked;
								};
						}
						#endregion
						#region Terminal Width
						{
							var item = options.DropDownItems.Add2(new ToolStripMenuItem("Terminal Width"));
							var edit = item.DropDownItems.Add2(new ToolStripTextBox());
							item.DropDown.Opening += (s,a) =>
								{
									edit.Text = m_disp.Settings.TerminalWidth.ToString();
								};
							item.DropDown.Closing += (s,a) =>
								{
									int w;
									if (!(a.Cancel = !int.TryParse(edit.Text, out w)))
										m_disp.Settings.TerminalWidth = w;
								};
							edit.KeyDown += (s,e) =>
								{
									if (e.KeyCode != Keys.Return) return;
									item.DropDown.Close();
								};
						}
						#endregion
						#region Terminal Height
						{
							var item = options.DropDownItems.Add2(new ToolStripMenuItem("Terminal Height"));
							var edit = item.DropDownItems.Add2(new ToolStripTextBox());
							item.DropDown.Opening += (s,a) =>
								{
									edit.Text = m_disp.Settings.TerminalHeight.ToString();
								};
							item.DropDown.Closing += (s,e) =>
								{
									int h;
									if (!(e.Cancel = !int.TryParse(edit.Text, out h)))
										m_disp.Settings.TerminalHeight = h;
								};
							edit.KeyDown += (s,e) =>
								{
									if (e.KeyCode != Keys.Return) return;
									item.DropDown.Close();
								};
						}
						#endregion
						#region Tab Size
						{
							var item = options.DropDownItems.Add2(new ToolStripMenuItem("Tab Size"));
							var edit = item.DropDownItems.Add2(new ToolStripTextBox());
							item.DropDown.Opening += (s,a) =>
								{
									edit.Text = m_disp.Settings.TabSize.ToString();
								};
							item.DropDown.Closing += (s,e) =>
								{
									int sz;
									if (!(e.Cancel = !int.TryParse(edit.Text, out sz)))
										m_disp.Settings.TabSize = sz;
								};
							edit.KeyDown += (s,e) =>
								{
									if (e.KeyCode != Keys.Return) return;
									Close();
								};
						}
						#endregion
						#region Newline Recv
						{
							var item = options.DropDownItems.Add2(new ToolStripMenuItem("Newline Recv"));
							var cb = new ComboBox(); cb.Items.AddRange(Enum<VT100.ENewLineMode>.Values.Cast<object>().ToArray());
							var edit = item.DropDownItems.Add2(new ToolStripControlHost(cb));
							item.DropDown.Opening += (s,a) =>
								{
									cb.SelectedItem = m_disp.Settings.NewLineRecv;
								};
							cb.SelectedIndexChanged += (s,e) =>
								{
									m_disp.Settings.NewLineRecv = (VT100.ENewLineMode)cb.SelectedItem;
								};
						}
						#endregion
						#region Newline Send
						{
							var item = options.DropDownItems.Add2(new ToolStripMenuItem("Newline Send"));
							var cb = new ComboBox(); cb.Items.AddRange(Enum<VT100.ENewLineMode>.Values.Cast<object>().ToArray());
							var edit = item.DropDownItems.Add2(new ToolStripControlHost(cb));
							item.DropDown.Opening += (s,a) =>
								{
									cb.SelectedItem = m_disp.Settings.NewLineSend;
								};
							cb.SelectedIndexChanged += (s,e) =>
								{
									m_disp.Settings.NewLineSend = (VT100.ENewLineMode)cb.SelectedItem;
								};
						}
						#endregion
						#region Hex Output
						{
							var item = options.DropDownItems.Add2(new ToolStripMenuItem("Hex Output"){CheckOnClick = true});
							options.DropDown.Opening += (s,a) =>
								{
									item.Checked = m_disp.Settings.HexOutput;
								};
							item.Click += (s,e) =>
								{
									m_disp.Settings.HexOutput = item.Checked;
								};
						}
						#endregion
					}
				}
			}
		}

		/// <summary>Dialog for selecting the capture file</summary>
		private class ChooseCaptureFile :Form
		{
			#region UI Elements
			private TextBox m_tb_filepath;
			private Button m_btn_browse;
			private CheckBox m_chk_capture_all;
			private Button m_btn_ok;
			private Button m_btn_cancel;
			#endregion

			public ChooseCaptureFile()
			{
				InitializeComponent();
				StartPosition = FormStartPosition.CenterParent;

				m_btn_browse.Click += (s,a) =>
					{
						using (var dlg = new SaveFileDialog{Title = "Capture file path", FileName = Filepath, Filter = FileFilters })
						{
							if (dlg.ShowDialog(this) != DialogResult.OK) return;
							DialogResult = DialogResult.None;
							Filepath = dlg.FileName;
						}
					};
			}
			protected override void Dispose(bool disposing)
			{
				Util.Dispose(ref components);
				base.Dispose(disposing);
			}

			/// <summary>The selected filepath</summary>
			public string Filepath
			{
				get { return m_tb_filepath.Text; }
				set { m_tb_filepath.Text = value; }
			}

			/// <summary>Get/Set whether all data is captured</summary>
			public bool CaptureAllData
			{
				get { return m_chk_capture_all.Checked; }
				set { m_chk_capture_all.Checked = value; }
			}

			/// <summary>
			/// Required method for Designer support - do not modify
			/// the contents of this method with the code editor.</summary>
			private void InitializeComponent()
			{
				this.m_tb_filepath = new System.Windows.Forms.TextBox();
				this.m_btn_browse = new System.Windows.Forms.Button();
				this.m_chk_capture_all = new System.Windows.Forms.CheckBox();
				this.m_btn_ok = new System.Windows.Forms.Button();
				this.m_btn_cancel = new System.Windows.Forms.Button();
				this.SuspendLayout();
				// 
				// m_tb_filepath
				// 
				this.m_tb_filepath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
				this.m_tb_filepath.Location = new System.Drawing.Point(12, 12);
				this.m_tb_filepath.Name = "m_tb_filepath";
				this.m_tb_filepath.Size = new System.Drawing.Size(203, 20);
				this.m_tb_filepath.TabIndex = 0;
				// 
				// m_btn_browse
				// 
				this.m_btn_browse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
				this.m_btn_browse.DialogResult = System.Windows.Forms.DialogResult.Cancel;
				this.m_btn_browse.Location = new System.Drawing.Point(221, 10);
				this.m_btn_browse.Name = "m_btn_browse";
				this.m_btn_browse.Size = new System.Drawing.Size(51, 23);
				this.m_btn_browse.TabIndex = 1;
				this.m_btn_browse.Text = ". . .";
				this.m_btn_browse.UseVisualStyleBackColor = true;
				// 
				// m_chk_capture_all
				// 
				this.m_chk_capture_all.AutoSize = true;
				this.m_chk_capture_all.Location = new System.Drawing.Point(24, 38);
				this.m_chk_capture_all.Name = "m_chk_capture_all";
				this.m_chk_capture_all.Size = new System.Drawing.Size(251, 17);
				this.m_chk_capture_all.TabIndex = 2;
				this.m_chk_capture_all.Text = "Capture all data (including vt100 escape codes)";
				this.m_chk_capture_all.UseVisualStyleBackColor = true;
				// 
				// m_btn_ok
				// 
				this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
				this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
				this.m_btn_ok.Location = new System.Drawing.Point(113, 63);
				this.m_btn_ok.Name = "m_btn_ok";
				this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
				this.m_btn_ok.TabIndex = 3;
				this.m_btn_ok.Text = "OK";
				this.m_btn_ok.UseVisualStyleBackColor = true;
				// 
				// m_btn_cancel
				// 
				this.m_btn_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
				this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
				this.m_btn_cancel.Location = new System.Drawing.Point(197, 63);
				this.m_btn_cancel.Name = "m_btn_cancel";
				this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
				this.m_btn_cancel.TabIndex = 4;
				this.m_btn_cancel.Text = "Cancel";
				this.m_btn_cancel.UseVisualStyleBackColor = true;
				// 
				// Form1
				// 
				this.AcceptButton = this.m_btn_ok;
				this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
				this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
				this.CancelButton = this.m_btn_cancel;
				this.ClientSize = new System.Drawing.Size(284, 93);
				this.Controls.Add(this.m_btn_cancel);
				this.Controls.Add(this.m_btn_ok);
				this.Controls.Add(this.m_chk_capture_all);
				this.Controls.Add(this.m_btn_browse);
				this.Controls.Add(this.m_tb_filepath);
				this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
				this.MaximumSize = new System.Drawing.Size(1000, 132);
				this.MinimumSize = new System.Drawing.Size(300, 132);
				this.Name = "Form1";
				this.Text = "Select a capture file";
				this.ResumeLayout(false);
				this.PerformLayout();

			}
			private IContainer components = null;
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Gui;

	[TestFixture] public class TestVT100
	{
		[Test] public void Robustness()
		{
			// Blast the buffer with noise to make sure it can handle any input
			var settings = new VT100.Settings();
			var buf = new VT100.Buffer(settings);
			buf.ReportUnsupportedEscapeSequences = false;

			var rnd = new Random(0);
			for (int i = 0; i != 10000; ++i)
			{
				var noise = rnd.Bytes().Take(rnd.Next(1000)).Select(x => (char)x).ToArray();
				buf.Output(new string(noise));
			}
		}
	}
}
#endif