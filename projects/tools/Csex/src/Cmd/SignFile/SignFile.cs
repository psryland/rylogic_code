using System;
using System.IO;
using Rylogic.Crypt;
using Rylogic.Extn;

namespace Csex
{
	public class SignFile :Cmd
	{
		private string m_file = string.Empty;
		private string m_pk = string.Empty;

		/// <summary>Display help information in the case of an invalid command line</summary>
		public override void ShowHelp(Exception? ex)
		{
			if (ex != null) Console.WriteLine("Error parsing command line: {0}", ex.Message);
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
				case "-signfile": return true;
				case "-f": m_file = args[arg++]; return true;
				case "-pk": m_pk = args[arg++]; return true;
				default: return base.CmdLineOption(option, args, ref arg);
			}
		}

		/// <summary>Return true if all required options have been given</summary>
		public override Exception? Validate()
		{
			return
				!m_file.HasValue() ? new Exception($"No filepath given") :
				!m_pk.HasValue() ? new Exception($"No private key XML file given") :
				null;
		}

		/// <summary>Run the command</summary>
		public override int Run()
		{
			var priv = File.ReadAllText(m_pk);
			Crypt.SignFile(m_file, priv);
			Console.WriteLine("'"+m_file+"' signed.");
			return 0;
		}
	}
}
