using System;
using System.IO;
using System.Windows.Forms;
using pr.common;
using pr.crypt;

namespace Csex
{
	public class Program :Cmd
	{
		private const string VersionString = "v1.0";
		private readonly string[] m_args;
		private Cmd m_cmd;

		[STAThread]
		static void Main(string[] args) { new Program(args).Run(); }
		public Program(string[] args)   { m_args = args; }

		/// <summary>Main run</summary>
		public override void Run()
		{
			// Check the name of the exe and do behaviour based on that.
			//Path.GetFileNameWithoutExtension(ExecutablePath);
			
			try { CmdLine.Parse(this, m_args); }
			catch (Exception ex)
			{
				Console.WriteLine("Error: "+ex.Message);
				ShowHelp();
				return;
			}

			if (m_cmd == null) { ShowHelp(); return; }
			m_cmd.Run();
		}

		/// <summary>Display help information in the case of an invalid command line</summary>
		public override void ShowHelp()
		{
			Console.WriteLine(
				"\n"+
				"***********************************************************\n"+
				" --- Commandline Extensions - Copyright © Rylogic 2012 --- \n"+
				"***********************************************************\n"+
				" Version: "+VersionString+"\n"+
				"  Syntax: Csex -command [parameters]\n"+
				"    -gencode   : Generates an activation code\n"+
				"    -signfile  : Sign a file using RSA\n"+
				// NEW_COMMAND - add a help string
				"\n"+
				"  Type Cex -command -help for help on a particular command\n"+
				"");
		}

		/// <summary>Handle a command line option.</summary>
		public override bool CmdLineOption(string option, string[] args, ref int arg)
		{
			if (m_cmd == null)
			{
				switch (option)
				{
				case "-gencode":  m_cmd = new GenActivationCode(); break;
				case "-signfile": m_cmd = new SignFile(); break;
				// NEW_COMMAND - handle the command
				}
			}
			return m_cmd == null
				? base.CmdLineOption(option, args, ref arg)
				: m_cmd.CmdLineOption(option, args, ref arg);
		}

		/// <summary>Forward arg to the command</summary>
		public override bool CmdLineData(string[] args, ref int arg)
		{
			return m_cmd == null
				? base.CmdLineData(args, ref arg)
				: m_cmd.CmdLineData(args, ref arg);
		}
	}
	#region Cmd base class
	public abstract class Cmd :CmdLine.Receiver
	{
		/// <summary>Run the command</summary>
		public abstract void Run();

		/// <summary>Handle a command line option.</summary>
		public override bool CmdLineOption(string option, string[] args, ref int arg)
		{
			switch (option)
			{
			case "/?":
			case "-h":
			case "-help":
				ShowHelp();
				arg = args.Length;
				return true;
			}
			Console.WriteLine("Error: Unknown option '"+option+"'\n");
			return false;
		}
	}
	#endregion

	#region Generate Activation Code
	public class GenActivationCode :Cmd
	{
		private string m_pk;
		
		/// <summary>Display help information in the case of an invalid command line</summary>
		public override void ShowHelp()
		{
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

		/// <summary>Run the command</summary>
		public override void Run()
		{
			try
			{
				if (m_pk == null) throw new ArgumentException("No private key xml file given");
				var priv = File.ReadAllText(m_pk);
				var code = ActivationCode.Generate(priv);
				Clipboard.SetText(code);
				Console.WriteLine("Code Generated:\n"+code+"\n\nCode has been copied to the clipboard");
			}
			catch (Exception ex)
			{
				Console.WriteLine("Code generation failed: "+ex.Message);
			}
		}
	}
	#endregion

	#region SignFile
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

		/// <summary>Run the command</summary>
		public override void Run()
		{
			try
			{
				if (m_file == null) throw new ArgumentException("No file to sign");
				if (m_pk == null) throw new ArgumentException("No private key xml file given");
				var priv = File.ReadAllText(m_pk);
				Crypt.SignFile(m_file, priv);
				Console.WriteLine("'"+m_file+"' signed.");
			}
			catch (Exception ex)
			{
				Console.WriteLine("Signing failed: "+ex.Message);
			}
		}
	}
	#endregion

	#region NEW_COMMAND
	// NEW_COMMAND - implement
	public class NEW_COMMAND :Cmd
	{
		public override void ShowHelp()
		{
			Console.Write(
				"This is a template command\n" +
				" Syntax: Csex -NEW_COMMAND -o option\n" +
				"  -o : this option is optional\n" +
				"");
		}

		public override bool CmdLineOption(string option, string[] args, ref int arg)
		{
			switch (option)
			{
			default: return base.CmdLineOption(option, args, ref arg);
			case "-NEW_COMMAND": return true;
			}
		}

		public override void Run()
		{
			throw new NotImplementedException();
		}
	}
	#endregion
}
