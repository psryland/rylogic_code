using System;
using System.IO;
using pr.crypt;
using pr.extn;

namespace Csex
{
	public class SignFile :Cmd
	{
		private string m_file;
		private string m_pk;

		/// <summary>Display help information in the case of an invalid command line</summary>
		public override void ShowHelp()
		{
			Console.Write(
				"Sign a file using RSA\n" +
				" Syntax: Csex -signfile -f file_to_sign -pk private_key.xml\n" +
				"  -f : the file to be signed. Signature will be appended to the file if not present\n" +
				"  -pk : the RSA private key xml file to use\n" +
				"");
		}

		/// <summary>Handle a command line option.</summary>
		public override bool CmdLineOption(string option, string[] args, ref int arg)
		{
			switch (option)
			{
			default: return base.CmdLineOption(option, args, ref arg);
			case "-signfile": return true;
			case "-f": m_file = args[arg++]; return true;
			case "-pk": m_pk = args[arg++]; return true;
			}
		}

		/// <summary>Return true if all required options have been given</summary>
		public override bool OptionsValid()
		{
			return m_file.HasValue() && m_pk.HasValue();
		}

		/// <summary>Run the command</summary>
		public override void Run()
		{
			var priv = File.ReadAllText(m_pk);
			Crypt.SignFile(m_file, priv);
			Console.WriteLine("'"+m_file+"' signed.");
		}
	}
}
