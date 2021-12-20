using System.Windows.Input;
using Rylogic.Common;

namespace Rylogic.Gui.WPF
{
	public partial class VT100Control: IVT100CMenu
	{
		private void InitCommands()
		{
			DoClearAll = Command.Create(this, DoClearAllInternal);
			DoCopy = Command.Create(this, DoCopyInternal);
			DoPaste = Command.Create(this, DoPasteInternal);
			DoCaptureToFile = Command.Create(this, DoCaptureToFileInternal);
			DoSendFile = Command.Create(this, DoSendFileInternal);
			ToggleLocalEcho = Command.Create(this, ToggleLocalEchoInternal);
			ToggleUnicodeText = Command.Create(this, ToggleUnicodeTextInternal);
			ToggleHexOutput = Command.Create(this, ToggleHexOutputInternal);
		}

		/// <inheritdoc/>
		public IVT100CMenu VT100CMenuContext => this;

		/// <inheritdoc/>
		public ICommand DoClearAll { get; private set; } = null!;
		private void DoClearAllInternal()
		{
			ClearAll();
		}

		/// <inheritdoc/>
		public ICommand DoCopy { get; private set; } = null!;
		private void DoCopyInternal()
		{
			Copy();
		}

		/// <inheritdoc/>
		public ICommand DoPaste { get; private set; } = null!;
		private void DoPasteInternal()
		{
			Paste();
		}

		/// <inheritdoc/>
		public ICommand DoCaptureToFile { get; private set; } = null!;
		private void DoCaptureToFileInternal()
		{
			CaptureToFile(!CapturingToFile);
		}

		/// <inheritdoc/>
		public ICommand DoSendFile { get; private set; } = null!;
		private void DoSendFileInternal()
		{
			SendFile();
		}

		/// <inheritdoc/>
		public bool LocalEcho
		{
			get => Settings.LocalEcho;
			set => Settings.LocalEcho = value;
		}
		public ICommand ToggleLocalEcho { get; private set; } = null!;
		private void ToggleLocalEchoInternal()
		{
			LocalEcho = !LocalEcho;
		}

		/// <inheritdoc/>
		public int TerminalWidth
		{
			get => Settings.TerminalWidth;
			set => Settings.TerminalWidth = value;
		}

		/// <inheritdoc/>
		public int TerminalHeight
		{
			get => Settings.TerminalHeight;
			set => Settings.TerminalHeight = value;
		}

		/// <inheritdoc/>
		public int TabSize
		{
			get => Settings.TabSize;
			set => Settings.TabSize = value;
		}

		/// <inheritdoc/>
		public VT100.ENewLineMode NewLineRecv
		{
			get => Settings.NewLineRecv;
			set => Settings.NewLineRecv = value;
		}

		/// <inheritdoc/>
		public VT100.ENewLineMode NewLineSend
		{
			get => Settings.NewLineSend;
			set => Settings.NewLineSend = value;
		}

		/// <inheritdoc/>
		public bool UnicodeText
		{
			get => Settings.UnicodeText;
			set => Settings.UnicodeText = value;
		}
		public ICommand ToggleUnicodeText { get; private set; } = null!;
		private void ToggleUnicodeTextInternal()
		{
			UnicodeText = !UnicodeText;
		}

		/// <inheritdoc/>
		public bool HexOutput
		{
			get => Settings.HexOutput;
			set => Settings.HexOutput = value;
		}
		public ICommand ToggleHexOutput { get; private set; } = null!;
		private void ToggleHexOutputInternal()
		{
			HexOutput = !HexOutput;
		}
	}
}
