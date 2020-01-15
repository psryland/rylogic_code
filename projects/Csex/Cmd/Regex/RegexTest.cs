using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using Rylogic.Gui.WPF;

namespace Csex
{
	public class RegexTest :Cmd
	{
		/// <summary></summary>
		public override void ShowHelp(Exception ex)
		{
			if (ex != null) Console.WriteLine("Error parsing command line: {0}", ex.Message);
			Console.Write(
				"Shows the regex pattern tester\n" +
				" Syntax: Csex -regex_test\n" +
				"");
		}

		/// <summary></summary>
		public override bool CmdLineOption(string option, string[] args, ref int arg)
		{
			switch (option.ToLowerInvariant())
			{
			default: return base.CmdLineOption(option, args, ref arg);
			case "-regex_test": return true;
			}
		}

		/// <summary>Validate the current options</summary>
		public override Exception Validate()
		{
			return null;
		}

		/// <summary></summary>
		public override int Run()
		{
			// WinForms
			// new Rylogic.Gui.WinForms.PatternUI().FormWrap(title:"Edit Pattern", sz:new Size(1024,768), start_pos:FormStartPosition.CenterScreen).ShowDialog();

			var dlg = new PatternEditorUI
			{ 
				WindowStartupLocation = WindowStartupLocation.CenterScreen,
				Title = "Edit Regex Pattern",
			};
			dlg.ShowDialog();
			return 0;
		}
	}
}
