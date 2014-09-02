module prd.common.cmdline;

import std.exception;
import std.stdio;
import std.string;

public abstract static class CmdLine
{
	// Receives command line options
	public interface IReceiver
	{
		/// Display help information in the case of an invalid command line
		void ShowHelp();

		/// Handle a command line option. Return true to continue parsing, false to stop
		bool CmdLineOption(string option, string[] args, ref int arg);

		/// Handle anything not preceded by '-'. Return true to continue parsing, false to stop
		bool CmdLineData(string[] args, ref int arg);

		/// Return true if all required options have been given
		@property bool OptionsValid();
	}


	public enum Result
	{
		Success,
		Interrupted,
		Failed,
	}

	/// Enumerate the provided command line options.
	/// Returns true of all command line parameters were parsed
	public static Result Parse(IReceiver cr, string[] args)
	{
		auto result = Result.Success;
		try
		{
			for (auto i = 1, iend = args.length; i != iend; ++i)
			{
				auto option = args[i];
				if (option.length >= 2 && option[0] == '-')
				{
					++i;
					if (!cr.CmdLineOption(option.toLower(), args, i))
					{
						result = Result.Interrupted;
						break;
					}
				}
				else
				{
					if (!cr.CmdLineData(args, i))
					{
						result = Result.Interrupted;
						break;
					}
				}
			}
			if (!cr.OptionsValid)
			{
				result = Result.Failed;
				cr.ShowHelp();
			}
		}
		catch (Exception ex)
		{
			writeln("Error parsing command line: "~ex.msg);
			cr.ShowHelp();
			result = Result.Failed;
		}
		return result;
	}

}