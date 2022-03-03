using System;
using System.Windows;
using Rylogic.Gui.WPF;

namespace Csex
{
	public class RegexTest :Cmd
	{
		/// <inheritdoc/>
		public override void ShowHelp(Exception? ex)
		{
			if (ex != null) Console.WriteLine("Error parsing command line: {0}", ex.Message);
			Console.Write(
				"Shows the regex pattern tester\n" +
				" Syntax: Csex -regex_test\n" +
				"");
		}

		/// <inheritdoc/>
		public override bool CmdLineOption(string option, string[] args, ref int arg)
		{
			switch (option.ToLowerInvariant())
			{
				case "-regex_test": return true;
				default: return base.CmdLineOption(option, args, ref arg);
			}
		}

		/// <inheritdoc/>
		public override Exception? Validate()
		{
			return null;
		}

		/// <summary></summary>
		public override int Run()
		{
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
