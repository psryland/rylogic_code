using System.ComponentModel;
using System.Windows.Input;
using Rylogic.Common;

namespace Rylogic.Gui.WPF
{
	public interface IVT100CMenu :INotifyPropertyChanged
	{
		/// <summary>Clear the text buffer</summary>
		ICommand DoClearAll { get; }

		/// <summary>Copy text from the text buffer</summary>
		ICommand DoCopy { get; }

		/// <summary>Paste text into the text buffer</summary>
		ICommand DoPaste { get; }

		/// <summary>Capture received text to a file</summary>
		bool CapturingToFile { get; }
		ICommand DoCaptureToFile { get; }

		/// <summary>Stream the contents of a file to the terminal</summary>
		ICommand DoSendFile { get; }

		/// <summary>Enable/Disable local echo</summary>
		bool LocalEcho { get; set; }
		ICommand ToggleLocalEcho { get; }

		/// <summary>Get/Set the terminal character width</summary>
		int TerminalWidth { get; set; }

		/// <summary>Get/Set the terminal character height</summary>
		int TerminalHeight { get; set; }

		/// <summary>Get/Set the number of spaces that TAB corresponds to</summary>
		int TabSize { get; set; }

		/// <summary>The expected format of received newlines</summary>
		VT100.ENewLineMode NewLineRecv { get; set; }

		/// <summary>The format of newlines to transmit</summary>
		VT100.ENewLineMode NewLineSend { get; set; }

		/// <summary>Allow/Disallow unicode text in the terminal</summary>
		bool UnicodeText { get; set; }
		ICommand ToggleUnicodeText { get; }

		/// <summary>Get/Set Hex output mode</summary>
		bool HexOutput { get; set; }
		ICommand ToggleHexOutput { get; }
	}
	public interface IVT100CMenuContext
	{
		/// <summary>The data context for vt100 control menu items</summary>
		IVT100CMenu VT100CMenuContext { get; }
	}
}
