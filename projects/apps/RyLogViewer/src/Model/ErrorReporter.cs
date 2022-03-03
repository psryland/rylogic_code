using System;
using Rylogic.Gui.WPF;
using Rylogic.Utility;

namespace RyLogViewer
{
	public class ErrorReporter : IReport
	{
		public ErrorReporter()
		{
		}

		/// <summary>The main window</summary>
		public MainWindow? Owner { get; set; }

		//todo string Status { get; private set; } // for binding
		//todo string StatusMessage(string msg)

		/// <summary>Report an error to the user</summary>
		public void ErrorPopup(string msg, Exception ex)
		{
			MsgBox.Show(Owner, $"{msg}\r\n{ex.Message}", $"{Util.AppProductName} Error", MsgBox.EButtons.OK, MsgBox.EIcon.Error);
		}
		public void ErrorPopup(string msg)
		{
			MsgBox.Show(Owner, msg, $"{Util.AppProductName} Error", MsgBox.EButtons.OK, MsgBox.EIcon.Error);
		}
	}
}
