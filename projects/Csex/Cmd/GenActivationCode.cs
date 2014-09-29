using System;
using System.IO;
using System.Windows.Forms;
using pr.common;
using pr.extn;

namespace Csex
{
	public class GenActivationCode :Cmd
	{
		private string m_pk;

		/// <summary>Display help information in the case of an invalid command line</summary>
		public override void ShowHelp(Exception ex)
		{
			if (ex != null) Console.WriteLine("Error parsing command line: {0}", ex.Message);
			Console.Write(
				"Sign a file using RSA\n" +
				" Syntax: Csex -gencode -pk private_key.xml\n" +
				"  -pk : the RSA private key xml file to use\n" +
				"");
		}

		/// <summary>Handle a command line option.</summary>
		public override bool CmdLineOption(string option, string[] args, ref int arg)
		{
			switch (option)
			{
			default: return base.CmdLineOption(option, args, ref arg);
			case "-gencode": return true;
			case "-pk": m_pk = args[arg++]; return true;
			}
		}

		/// <summary>Return true if all required options have been given</summary>
		public override bool OptionsValid()
		{
			return m_pk.HasValue();
		}

		/// <summary>Run the command</summary>
		public override int Run()
		{
			var priv = File.ReadAllText(m_pk);
			var code = ActivationCode.Generate(priv);
			Clipboard.SetText(code);
			Console.WriteLine("Code Generated:\n"+code+"\n\nCode has been copied to the clipboard");
			return 0;
		}
	}
}
