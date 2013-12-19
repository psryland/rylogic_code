using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using pr.gui;

namespace Csex
{
	public class PatternUI :Cmd
	{
		public override void ShowHelp()
		{
			Console.Write(
				"Shows the regex pattern UI\n" +
				" Syntax: Csex -PatternUI\n" +
				"");
		}

		public override bool CmdLineOption(string option, string[] args, ref int arg)
		{
			switch (option.ToLowerInvariant())
			{
			default: return base.CmdLineOption(option, args, ref arg);
			case "-patternui": return true;
			}
		}

		/// <summary>Return true if all required options have been given</summary>
		public override bool OptionsValid()
		{
			return true;
		}

		public override int Run()
		{
			using (var f = new Form{Text="Pattern Test", Size = new Size(1024,768), FormBorderStyle = FormBorderStyle.Sizable, StartPosition = FormStartPosition.CenterScreen})
			{
				var c = new pr.gui.PatternUI(){Dock = DockStyle.Fill};
				f.Controls.Add(c);
				f.ShowDialog();
			}
			return 0;
		}
	}
}
