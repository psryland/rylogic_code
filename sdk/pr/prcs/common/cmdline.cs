using System;

namespace pr.common
{
	public static class CmdLine
	{
		/// <summary>Receives command line options</summary>
		public interface IReceiver
		{
			/// <summary>Display help information in the case of an invalid command line</summary>
			void ShowHelp();
		
			/// <summary>Handle a command line option. Return true to continue parsing, false to stop</summary>
			bool CmdLineOption(string option, string[] args, ref int arg);
			
			/// <summary>Handle anything not preceded by '-'. Return true to continue parsing, false to stop</summary>
			bool CmdLineData(string[] args, ref int arg);

			/// <summary>Return true if all required options have been given</summary>
			bool OptionsValid();
		}

		public enum Result
		{
			Success,
			Interrupted,
			Failed,
		}

		/// <summary>Enumerate the provided command line options. Returns true of all command line parameters were parsed</summary>
		public static Result Parse(IReceiver cr, string[] args)
		{
			Result result = Result.Success;
			try
			{
				for (int i = 0, iend = args.Length; i != iend;)
				{
					var option = args[i++];
					if (option.Length >= 2 && option[0] == '-')
					{
						if (!cr.CmdLineOption(option.ToLowerInvariant(), args, ref i))
						{
							result = Result.Interrupted;
							break;
						}
					}
					else
					{
						if (!cr.CmdLineData(args, ref i))
						{
							result = Result.Interrupted;
							break;
						}
					}
				}
				if (!cr.OptionsValid())
				{
					result = Result.Failed;
					cr.ShowHelp();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error parsing command line: {0}", ex.Message);
				cr.ShowHelp();
				result = Result.Failed;
			}
			return result;
		}
	}
}
