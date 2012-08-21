namespace pr.common
{
	public static class CmdLine
	{
		/// <summary>Receives command line options</summary>
		public abstract class Receiver
		{
			/// <summary>Display help information in the case of an invalid command line</summary>
			public abstract void ShowHelp();
		
			/// <summary>Handle a command line option.</summary>
			public virtual bool CmdLineOption(string option, string[] args, ref int arg) { ++arg; return true; }
			
			/// <summary>Handle anything not preceded by '-'</summary>
			public virtual bool CmdLineData(string[] args, ref int arg) { return true; }
		}

		/// <summary>Enumerate the provided command line options. Returns true of all command line parameters were parsed</summary>
		public static bool Parse(Receiver cr, string[] args)
		{
			for (int i = 0, iend = args.Length; i != iend;)
			{
				var option = args[i++];
				if (option.Length >= 2 && option[0] == '-')
				{
					if (!cr.CmdLineOption(option.ToLowerInvariant(), args, ref i))
						return false;
				}
				else
				{
					if (!cr.CmdLineData(args, ref i))
						return false;
				}
			}
			return true;
		}
	}
}
