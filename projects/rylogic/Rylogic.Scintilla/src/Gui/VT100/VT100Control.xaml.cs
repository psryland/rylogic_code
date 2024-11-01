using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Interop.Win32;
using Rylogic.Scintilla;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	public partial class VT100Control :ScintillaControl, IVT100CMenuContext
	{
		// Notes:
		//  - This is a wrapper of the Scintilla control that provides methods for
		//    rendering a VT100 terminal.

		private readonly Sci.CellBuf m_cells; // Buffer for copying styled text to the scintilla control
		private readonly Dictionary<VT100.Style, byte> m_sty; // map from vt100 style to scintilla style index

		static VT100Control()
		{
			Sci.LoadDll(throw_if_missing: false);
		}
		public VT100Control()
		{
			InitializeComponent();
			m_cells = new Sci.CellBuf();
			m_sty = new Dictionary<VT100.Style, byte>();

			Blink = false;
			AllowDrop = true;
			EndAtLastLine = true;

			// Use our own context menu
			UsePopUp = false;

			// Turn off undo history
			UndoCollection = false;

			// Default context menu implementation
			if (ContextMenu != null && ContextMenu.DataContext == null)
				ContextMenu.DataContext = this;

			InitCommands();
		}
		protected override void Dispose(bool disposing)
		{
			Blink = false;
			Buffer = null!;
		}
		protected override IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
		{
			//Win32.WndProcDebug(hwnd, msg, wparam, lparam, "vt100");
			switch (msg)
			{
				case Win32.WM_KEYDOWN:
				{
					var ks = new Win32.KeyState(lparam);
					if (!ks.Alt && Buffer != null)
					{
						var vk = Win32.ToVKey(wparam);

						// Forward navigation keys to the control
						if (vk == EKeyCodes.Up || vk == EKeyCodes.Down || vk == EKeyCodes.Left || vk == EKeyCodes.Right ||
							vk == EKeyCodes.PageUp || vk == EKeyCodes.PageDown || vk == EKeyCodes.End || vk == EKeyCodes.Home)
						{
							Cmd(msg, wparam, lparam);
							handled = true;
							return IntPtr.Zero;
						}

						// Handle clipboard shortcuts
						if (vk == (EKeyCodes.Control | EKeyCodes.C))
						{
							Copy();
							handled = true;
							return IntPtr.Zero;
						}
						if (vk == (EKeyCodes.Control | EKeyCodes.V))
						{
							Paste();
							handled = true;
							return IntPtr.Zero;
						}

						// Add the key press to the input buffer.
						var added = AddToBuffer(vk);
						
						// Let the key events through if local echo is on.
						handled = added && !Buffer.Settings.LocalEcho;
						return IntPtr.Zero;
					}
					break;
				}
				case Win32.WM_CHAR:
				{
					var vk = Win32.ToVKey(wparam);
					handled = vk.HasFlag(EKeyCodes.Control) || !Buffer.Settings.LocalEcho;
					return IntPtr.Zero;
				}
				case Win32.WM_DROPFILES:
				{
					var drop_info = wparam;
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
					return IntPtr.Zero;
				}
			}

			return base.WndProc(hwnd, msg, wparam, lparam, ref handled);
		}
		public override void Copy()
		{
			base.Copy();
		}
		public override void Paste()
		{
			try
			{
				// The Scintilla.Paste function writes text into the control, but we need the text to go into the VT100 input buffer first.
				AddToBuffer(Clipboard.GetText(TextDataFormat.UnicodeText));
				if (Buffer.Settings.LocalEcho)
					base.Paste();
			}
			catch { }
		}

		/// <summary></summary>
		public VT100.Settings Settings => Buffer.Settings;

		/// <summary>The underlying vt100 virtual display buffer</summary>
		public VT100.Buffer Buffer
		{
			get => (VT100.Buffer)GetValue(BufferProperty);
			set => SetValue(BufferProperty, value);
		}
		private void Buffer_Changed(VT100.Buffer nue, VT100.Buffer old)
		{
			Scope<RangeI>? selection = null;

			if (old != null)
			{
				// Ensure capturing is stopped on the old buffer
				old.CaptureToFileEnd();
				old.Overflow -= HandleOverflow;
				old.BufferChanged -= HandleBufferChanged;
				old.Settings.PropertyChanged -= HandleSettingChanged;
			}
			ClearAll();
			if (nue != null)
			{
				nue.Settings.PropertyChanged += HandleSettingChanged;
				nue.BufferChanged += HandleBufferChanged;
				nue.Overflow += HandleOverflow;
			}

			// Notify of affected changes
			NotifyPropertyChanged(nameof(Buffer));
			NotifyPropertyChanged(nameof(Settings));
			SignalUpdateText();

			// Handlers
			void HandleSettingChanged(object? sender, PropertyChangedEventArgs e)
			{
				switch (e.PropertyName)
				{
					case nameof(Settings.LocalEcho):
					{
						NotifyPropertyChanged(nameof(LocalEcho));
						break;
					}
					case nameof(Settings.TerminalWidth):
					{
						NotifyPropertyChanged(nameof(TerminalWidth));
						break;
					}
					case nameof(Settings.TerminalHeight):
					{
						NotifyPropertyChanged(nameof(TerminalHeight));
						break;
					}
					case nameof(Settings.TabSize):
					{
						NotifyPropertyChanged(nameof(TabSize));
						break;
					}
					case nameof(Settings.NewLineRecv):
					{
						NotifyPropertyChanged(nameof(NewLineRecv));
						break;
					}
					case nameof(Settings.NewLineSend):
					{
						NotifyPropertyChanged(nameof(NewLineSend));
						break;
					}
					case nameof(Settings.UnicodeText):
					{
						NotifyPropertyChanged(nameof(UnicodeText));
						break;
					}
					case nameof(Settings.HexOutput):
					{
						NotifyPropertyChanged(nameof(HexOutput));
						break;
					}
				}
			}
			void HandleBufferChanged(object? sender, VT100BufferChangedEventArgs e)
			{
				SignalUpdateText();
			}
			void HandleOverflow(object? sender, VT100BufferOverflowEventArgs e)
			{
				// Handle buffer lines being dropped</summary>
				if (e.Before)
				{
					// Calculate the number of characters being dropped.
					// Note: Buffer does not store line endings, but the saved selection does.
					var chars_being_removed = 0;
					for (int i = e.Dropped.Begi; i != e.Dropped.Endi; ++i)
						chars_being_removed += Buffer.Lines[i].Length + 1; // +1 for the '\n'

						// Save the selection and adjust it by the number of characters to be dropped
					selection = PreserveSelection();
					selection.Value.Beg -= chars_being_removed;
					selection.Value.End -= chars_being_removed;

					// Remove the dropped text from the control
					DeleteRange(0, chars_being_removed);

					// Scroll up to move with the buffer text
					var vis = FirstVisibleLine;
					vis = Math.Max(0, vis - e.Dropped.Sizei);
					FirstVisibleLine = vis;
				}
				else
				{
					// Restore the selection
					selection?.Dispose();
					selection = null;
				}
			}
		}

		/// <summary>Shared-default buffer. This should only be used for initialisation, instances of the VT100Control should set their own Buffer instance</summary>
		private static readonly VT100.Buffer DefaultBuffer = new(new VT100.Settings());
		public static readonly DependencyProperty BufferProperty = Gui_.DPRegister<VT100Control>(nameof(Buffer), DefaultBuffer, Gui_.EDPFlags.None);

		/// <summary>The terminal context menu</summary>
		public ContextMenu CMenu
		{
			get => ContextMenu;
			set
			{
				ContextMenu = value;
				if (ContextMenu != null && ContextMenu.Name == "VT100CMenu")
					ContextMenu.DataContext = this;
			}
		}

		/// <summary>Enable/Disable blinking</summary>
		private bool Blink
		{
			get => m_blinker != null;
			set
			{
				if (Blink == value) return;
				if (m_blinker != null)
				{
					m_blinker.Stop();
				}
				m_blinker = value ? new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, HandleBlink, Dispatcher) : null;
				if (m_blinker != null)
				{
					m_blinker.Start();
				}

				// Handlers
				void HandleBlink(object? sender, EventArgs e)
				{
					SignalUpdateText();
				}
			}
		}
		private DispatcherTimer? m_blinker;

		/// <summary>True if the display automatically scrolls to the bottom</summary>
		public bool AutoScrollToBottom
		{
			get => (bool)GetValue(AutoScrollToBottomProperty);
			set => SetValue(AutoScrollToBottomProperty, value);
		}
		public static readonly DependencyProperty AutoScrollToBottomProperty = Gui_.DPRegister<VT100Control>(nameof(AutoScrollToBottom), true, Gui_.EDPFlags.TwoWay);

		/// <summary>True if adding input causes the display to automatically scroll to the bottom</summary>
		public bool ScrollToBottomOnInput
		{
			get => (bool)GetValue(ScrollToBottomOnInputProperty);
			set => SetValue(ScrollToBottomOnInputProperty, value);
		}
		public static readonly DependencyProperty ScrollToBottomOnInputProperty = Gui_.DPRegister<VT100Control>(nameof(ScrollToBottomOnInput), true, Gui_.EDPFlags.TwoWay);

		/// <summary>Refresh the control with text from the vt100 buffer</summary>
		private void UpdateText()
		{
			m_update_pending = false;

			// No buffer = empty display
			if (Buffer is not VT100.Buffer buf)
				return;

			// Get the buffer region that has changed
			var region = buf.InvalidRect;
			buf.ValidateRect();
			if (region.IsEmpty)
				return;

			// Preserve the scroll position
			using var scroll_scope = !AutoScrollToBottom ? ScrollScope() : null;

			// Grow the text in the control to the required number of lines (if necessary)
			var line_count = LineCount;
			if (line_count < region.Bottom)
			{
				var pad = new byte[region.Bottom - line_count].Memset(0x0a);
				using var p = GCHandle_.Alloc(pad, GCHandleType.Pinned);
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
			var beg = PositionFromLineIndex(region.Top);
			var end = PositionFromLineIndex(region.Bottom);
			if (beg < 0) beg = 0;
			if (end < 0) end = TextLength;

			// Overwrite the visible lines with the buffer of cells
			DeleteRange(beg, end - beg);
			InsertStyledText(beg, m_cells);

			// Reset, ready for next time
			m_cells.Length = 0;

			// Auto scroll to the bottom if told to and the last line is off the bottom 
			if (AutoScrollToBottom)
				ScrollToBottom();
		}
		private void SignalUpdateText()
		{
			if (m_update_pending) return;
			Dispatcher.BeginInvoke(new Action(UpdateText));
			m_update_pending = true;
		}
		private bool m_update_pending;

		/// <summary>Return the scintilla style index for the given vt100 style</summary>
		private byte SciStyle(VT100.Style sty)
		{
			// See if the style is cached
			if (m_sty.TryGetValue(sty, out var idx))
				return idx;

			// Generate the style
			idx = (byte)Math.Min(m_sty.Count, 255);
			var forecol = VT100.HBGR.ToColor(!sty.RevserseVideo ? sty.ForeColour : sty.BackColour);
			var backcol = VT100.HBGR.ToColor(!sty.RevserseVideo ? sty.BackColour : sty.ForeColour);
			StyleSetFont(idx, "consolas");
			StyleSetFore(idx, forecol);
			StyleSetBack(idx, backcol);
			StyleSetBold(idx, sty.Bold);
			StyleSetUnderline(idx, sty.Underline);
			m_sty[sty] = idx;
			return idx;
		}

		/// <summary>Adds text to the input buffer</summary>
		protected virtual bool AddToBuffer(EKeyCodes vk)
		{
			// Try to add the raw key code first.
			var added = Buffer.AddInput(vk);

			// If not handled, try to convert it to a character and then add it.
			if (!added && Win32.CharFromVKey(vk, out var ch))
			{
				Buffer.AddInput(ch);
				added = true;
			}

			// If input triggers auto scroll
			if (added && ScrollToBottomOnInput)
			{
				AutoScrollToBottom = true;
				ScrollToBottom();
			}

			return added;
		}
		protected virtual bool AddToBuffer(string text)
		{
			var added = text.Length != 0;
			
			// Add the text to the input buffer
			foreach (var ch in text)
				Buffer.AddInput(ch);

			// If input triggers auto scroll
			if (added && ScrollToBottomOnInput)
			{
				AutoScrollToBottom = true;
				ScrollToBottom();
			}

			return added;
		}

		/// <summary>Start or stop capturing to a file</summary>
		public void CaptureToFile(bool start)
		{
			if (Buffer == null)
				return;

			if (start)
			{
				var dlg = new VT100CaptureToFileOptions(Settings);
				if (dlg.ShowDialog(this) != true)
					return;

				try
				{
					Buffer.CaptureToFile(dlg.FileName, dlg.BinaryCapture);
				}
				catch (Exception ex)
				{
					MsgBox.Show(Window.GetWindow(this), $"Capture to file could not start\r\n{ex.Message}", "Capture To File", MsgBox.EButtons.OK, MsgBox.EIcon.Error);
				}
			}
			else
			{
				Buffer.CaptureToFileEnd();
			}
			NotifyPropertyChanged(nameof(CapturingToFile));
		}

		/// <summary>True if capturing to file is currently enabled</summary>
		public bool CapturingToFile => Buffer?.CapturingToFile ?? false;

		/// <summary>Send a file to the terminal</summary>
		public void SendFile()
		{
			if (Buffer == null) return;
			var dlg = new OpenFileDialog
			{
				Title = "Choose the file to send",
				Filter = Util.FileDialogFilter("Text Files", "*.txt", "Log Files", "*.log", "All Files", "*.*"),
			};
			if (dlg.ShowDialog(this) != true)
				return;

			try
			{
				Buffer.SendFile(dlg.FileName);
			}
			catch (Exception ex)
			{
				MsgBox.Show(Window.GetWindow(this), $"Sending file {dlg.FileName} failed\r\n{ex.Message}", "Send File", MsgBox.EButtons.OK, MsgBox.EIcon.Error);
			}
		}

		/// <summary>Drag drop event</summary>
		protected virtual void HandleDropFiles(IEnumerable<string> files)
		{
			if (Buffer == null) return;
			foreach (var file in files)
				Buffer.SendFile(file);
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using System.Linq;

	[TestFixture]
	public class TestVT100
	{
		[Test]
		public void Robustness()
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