using System;
using System.Collections.Generic;

namespace Rylogic.Common
{
	public static class CmdLine
	{
		/// <summary>The result of parsing a command line</summary>
		public enum Result
		{
			Success,
			Interrupted,
			Failed,
		}

		/// <summary>Receives command line options</summary>
		public interface IReceiver
		{
			/// <summary>Display help information in the case of an invalid command line</summary>
			void ShowHelp(Exception? ex = null);

			/// <summary>
			/// Handle a command line option. Return true to continue parsing, false to stop.
			/// 'arg' is the index of the argument immediately after 'option'. If args[arg] is
			/// used (i.e. its a parameter related to the option) then arg should be incremented.
			/// If additional arguments are used, 'arg' should be incremented for each arg used.</summary>
			bool CmdLineOption(string option, string[] args, ref int arg);

			/// <summary>
			/// Handle anything not preceded by '-'. Return true to continue parsing, false to stop.
			/// 'arg' is the index of the argument immediately after 'data'. If args[arg] is
			/// used then arg should be incremented. If additional arguments are used, 'arg'
			/// should be incremented for each arg used.</summary>
			bool CmdLineData(string data, string[] args, ref int arg);

			/// <summary>Return true if all required options have been given</summary>
			bool OptionsValid();
		}

		/// <summary>Returns true if 'arg' is prefixed by '-'</summary>
		public static bool IsOption(string arg)
		{
			return arg.Length >= 2 && arg[0] == '-';
		}

		/// <summary>Enumerate the provided command line options. Returns true of all command line parameters were parsed</summary>
		public static Result Parse(IReceiver cr, string[] args)
		{
			var result = Result.Success;
			try
			{
				for (int i = 0, iend = args.Length; i != iend;)
				{
					if (IsOption(args[i]))
					{
						var option = args[i++];
						if (!cr.CmdLineOption(option.ToLowerInvariant(), args, ref i))
						{
							result = Result.Interrupted;
							break;
						}
					}
					else
					{
						var data = args[i++];
						if (!cr.CmdLineData(data, args, ref i))
						{
							result = Result.Interrupted;
							break;
						}
					}
				}

				// Check that the given command line arguments are self consistent
				if (result == Result.Success && !cr.OptionsValid())
				{
					result = Result.Failed;
					cr.ShowHelp();
				}
			}
			catch (Exception ex)
			{
				// Note: implementations of IReceiver should trap and log exceptions
				// if they want detailed error information.
				result = Result.Failed;

				// On failure, display the help info
				cr.ShowHelp(ex);
			}
			return result;
		}

		/// <summary>Converts a single command line string in to a collection of arguments (obeying quoted items)</summary>
		public static IEnumerable<string> Tokenise(string cmd_line)
		{
			for (int i = 0, iend = cmd_line.Length; i != iend; ++i)
			{
				if (cmd_line[i] == '"') // Extract whole strings
				{
					int j = i;
					for (++j; j != iend && cmd_line[j] != '"'; ++j) {}
					yield return cmd_line.Substring(i + 1, j - i - 1);
					if (j == iend) yield break;
					i = j;
				}
				else if (!char.IsWhiteSpace(cmd_line[i]))
				{
					int j = i;
					for (++j; j != iend && !char.IsWhiteSpace(cmd_line[j]); ++j) {}
					yield return cmd_line.Substring(i, j - i);
					if (j == iend) yield break;
					i = j;
				}
			}
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using System.Linq;
	using Common;

	[TestFixture] public class TestCmdLine
	{
		public class Thing :CmdLine.IReceiver
		{
			public int HelpShownCount;
			public int ValidateCount;
			public string? Option;
			public string? OptionArg1;
			public string? OptionArg2;
			public string? Data1;
			public string? Data2;
			public bool CmdLineOptionResult;
			public bool CmdLineDataResult;

			public Thing()
			{
				HelpShownCount = 0;
				ValidateCount = 0;
				Option =  null;
				OptionArg1 = null;
				OptionArg2 = null;
				Data1 = null;
				Data2 = null;
				CmdLineOptionResult = true;
				CmdLineDataResult = true;
			}

			/// <summary>Display help information in the case of an invalid command line</summary>
			public void ShowHelp(Exception? ex)
			{
				++HelpShownCount; 
			}

			/// <summary>Expects '-option data data'</summary>
			public bool CmdLineOption(string option, string[] args, ref int arg)
			{
				Option = option;
				OptionArg1 = args[arg++];
				OptionArg2 = args[arg++];
				return CmdLineOptionResult;
			}

			/// <summary>Expects 'data data'</summary>
			public bool CmdLineData(string data, string[] args, ref int arg)
			{
				Data1 = data;
				Data2 = args[arg++];
				return CmdLineDataResult;
			}

			/// <summary>Return true if all required options have been given</summary>
			public bool OptionsValid()
			{
				++ValidateCount;
				return OptionArg1 == "B";
			}
		}
		[Test] public void TestParse0()
		{
			var t = new Thing{CmdLineOptionResult = true, CmdLineDataResult = true};
			var r  = CmdLine.Parse(t, new[]{"-A","B", "C", "D", "E"});
			Assert.True(r == CmdLine.Result.Success);
			Assert.Equal(0, t.HelpShownCount);
			Assert.Equal(1, t.ValidateCount);
			Assert.Equal("-a", t.Option);
			Assert.Equal("B", t.OptionArg1);
			Assert.Equal("C", t.OptionArg2);
			Assert.Equal("D", t.Data1);
			Assert.Equal("E", t.Data2);
		}
		[Test] public void TestParse1()
		{
			var t = new Thing{CmdLineOptionResult = true, CmdLineDataResult = false};
			var r  = CmdLine.Parse(t, new[]{"-A","B", "C", "D", "E"});
			Assert.True(r == CmdLine.Result.Interrupted);
			Assert.Equal(0, t.HelpShownCount);
			Assert.Equal(0, t.ValidateCount);
			Assert.Equal("-a", t.Option);
			Assert.Equal("B", t.OptionArg1);
			Assert.Equal("C", t.OptionArg2);
			Assert.Equal("D", t.Data1);
			Assert.Equal("E", t.Data2);
		}
		[Test] public void TestParse2()
		{
			var t = new Thing{CmdLineOptionResult = true, CmdLineDataResult = true};
			var r  = CmdLine.Parse(t, new[]{"-A","B", "C", "D"});
			Assert.True(r == CmdLine.Result.Failed);
			Assert.Equal(1, t.HelpShownCount);
			Assert.Equal(0, t.ValidateCount);
			Assert.Equal("-a", t.Option);
			Assert.Equal("B", t.OptionArg1);
			Assert.Equal("C", t.OptionArg2);
			Assert.Equal("D", t.Data1);
			Assert.Equal(null, t.Data2);
		}
		[Test] public void TestParse3()
		{
			var t = new Thing{CmdLineOptionResult = true, CmdLineDataResult = true};
			var r  = CmdLine.Parse(t, new[]{"-A","X", "C", "D", "E"});
			Assert.True(r == CmdLine.Result.Failed);
			Assert.Equal(1, t.HelpShownCount);
			Assert.Equal(1, t.ValidateCount);
			Assert.Equal("-a", t.Option);
			Assert.Equal("X", t.OptionArg1);
			Assert.Equal("C", t.OptionArg2);
			Assert.Equal("D", t.Data1);
			Assert.Equal("E", t.Data2);
		}
		[Test] public void TestTokenise()
		{
			var cmd_line = "thing.exe -a \"Hello World\" -b data.txt output";
			var r = CmdLine.Tokenise(cmd_line).ToArray();
			Assert.True(r.Length == 6);
			Assert.Equal(r[0], "thing.exe");
			Assert.Equal(r[1], "-a");
			Assert.Equal(r[2], "Hello World");
			Assert.Equal(r[3], "-b");
			Assert.Equal(r[4], "data.txt");
			Assert.Equal(r[5], "output");
		}
	}
}
#endif