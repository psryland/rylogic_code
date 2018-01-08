using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Rylogic.Common;
using Rylogic.Extn;

namespace Csex
{
	public class GenActivationCode :Cmd
	{
		private string m_pk;
		private string m_filepath;
		private string m_data;
		private string m_outfile;
		private bool m_outclip;

		/// <summary>Display help information in the case of an invalid command line</summary>
		public override void ShowHelp(Exception ex)
		{
			if (ex != null) Console.WriteLine("Error parsing command line: {0}", ex.Message);
			Console.Write(
				"Sign a file using RSA\n" +
				" Syntax: Csex -gencode -pk private_key.xml -data \"unique_details_string\"|-file \"filepath\" [-out_file \"out_file\"] [-out_clipboard]\n" +
				"  -pk : the RSA private key xml file to use\n" +
				"  -data : A string to generate the key from\n" +
				"  -file : A file whose contents should be used to generate the key\n" +
				"  -out_file : A file to write the key to\n" +
				"  -out_clipboard : Copy the key to the system clipboard\n" +
				"\n" +
				" Only one of -data or -file should be given\n" +
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
			case "-data": m_data = args[arg++]; return true;
			case "-file": m_filepath = args[arg++]; return true;
			case "-out_file": m_outfile = args[arg++]; return true;
			case "-out_clipboard": m_outclip = true; return true;
			}
		}

		/// <summary>Return true if all required options have been given</summary>
		public override bool OptionsValid()
		{
			return
				m_pk.HasValue() &&
				(m_data.HasValue() || m_filepath.HasValue());
		}

		/// <summary>Run the command</summary>
		public override int Run()
		{
			// If a file is given, load the data from it
			if (m_filepath.HasValue())
			{
				if (!Path_.FileExists(m_filepath))
					throw new Exception($"File '{m_filepath}' doesn't exist");

				m_data = File.ReadAllText(m_filepath);
			}

			// If no data is available, error
			if (!m_data.HasValue())
				throw new Exception("No data to generate key from");

			// No private key?
			if (!m_pk.HasValue() || !Path_.FileExists(m_pk))
				throw new Exception($"Private key '{m_pk}' doesn't exist");

			var priv = File.ReadAllText(m_pk);
			var code = ActivationCode.Generate(m_data, priv);
			var key = Convert.ToBase64String(code);

			// Save to the system clipboard
			if (m_outclip)
				Clipboard.SetText(key);

			// Save to a file
			if (m_outfile.HasValue())
				key.ToFile(m_outfile);

			// Write to StdOut
			Console.WriteLine(key);
			return 0;
		}
	}
}
