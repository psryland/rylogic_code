using System;
using System.IO;
using pr.extn;
using pr.script;

namespace Csex
{
	// Html Text substitute
	public class MarkupExpand :Cmd
	{
		private string m_infile;
		private string m_outfile;

		public override void ShowHelp()
		{
			Console.Write(
				"Expand special comments in a markup file\n" +
				" Syntax: Csex -expand_template -f input_file -o output_file\n" +
				"  -i : the file parse for template substitutions\n" +
				"  -o : the file to write out containing the text substitutions\n" +
				"");
		}

		/// <summary>Handle a command line option.</summary>
		public override bool CmdLineOption(string option, string[] args, ref int arg)
		{
			switch (option)
			{
			default: return base.CmdLineOption(option, args, ref arg);
			case "-expand_template": return true;
			case "-f": m_infile = args[arg++]; return true;
			case "-o": m_outfile = args[arg++]; return true;
			}
		}

		/// <summary>Return true if all required options have been given</summary>
		public override bool OptionsValid()
		{
			return m_infile.HasValue() && m_outfile.HasValue();
		}

		/// <summary>Run the command</summary>
		public override int Run()
		{
			var current_dir = Path.GetDirectoryName(m_infile) ?? string.Empty;
			var result = Expand.Markup(new FileSrc(m_infile), current_dir);
			File.WriteAllText(m_outfile, result);
			return 0;
		}
	}
}