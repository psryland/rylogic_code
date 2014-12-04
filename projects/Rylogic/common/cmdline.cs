using System;

namespace pr.common
{
	public static class CmdLine
	{
		/// <summary>Receives command line options</summary>
		public interface IReceiver
		{
			/// <summary>Display help information in the case of an invalid command line</summary>
			void ShowHelp(Exception ex = null);

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

		public enum Result
		{
			Success,
			Interrupted,
			Failed,
		}

		/// <summary>Enumerate the provided command line options. Returns true of all command line parameters were parsed</summary>
		public static Result Parse(IReceiver cr, string[] args)
		{
			var result = Result.Success;
			try
			{
				Func<string,bool> is_option = a => a.Length >= 2 && a[0] == '-';

				for (int i = 0, iend = args.Length; i != iend;)
				{
					if (is_option(args[i]))
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
				if (!cr.OptionsValid())
				{
					result = Result.Failed;
					cr.ShowHelp();
				}
			}
			catch (Exception ex)
			{
				cr.ShowHelp(ex);
				result = Result.Failed;
			}
			return result;
		}
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	using common;

	[TestFixture] public class TestCmdLine
	{
		public class Thing :CmdLine.IReceiver
		{
			public int HelpShownCount;
			public int ValidateCount;
			public string Option;
			public string OptionArg1;
			public string OptionArg2;
			public string Data1;
			public string Data2;

			public bool CmdLineOptionResult = true;
			public bool CmdLineDataResult = true;

			/// <summary>Display help information in the case of an invalid command line</summary>
			public void ShowHelp(Exception ex) { ++HelpShownCount; }

			public bool CmdLineOption(string option, string[] args, ref int arg)
			{
				Option = option;
				OptionArg1 = args[arg++];
				OptionArg2 = args[arg++];
				return CmdLineOptionResult;
			}

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
			Assert.AreEqual(0, t.HelpShownCount);
			Assert.AreEqual(1, t.ValidateCount);
			Assert.AreEqual("-a", t.Option);
			Assert.AreEqual("B", t.OptionArg1);
			Assert.AreEqual("C", t.OptionArg2);
			Assert.AreEqual("D", t.Data1);
			Assert.AreEqual("E", t.Data2);
		}
		[Test] public void TestParse1()
		{
			var t = new Thing{CmdLineOptionResult = true, CmdLineDataResult = false};
			var r  = CmdLine.Parse(t, new[]{"-A","B", "C", "D", "E"});
			Assert.True(r == CmdLine.Result.Interrupted);
			Assert.AreEqual(0, t.HelpShownCount);
			Assert.AreEqual(1, t.ValidateCount);
			Assert.AreEqual("-a", t.Option);
			Assert.AreEqual("B", t.OptionArg1);
			Assert.AreEqual("C", t.OptionArg2);
			Assert.AreEqual("D", t.Data1);
			Assert.AreEqual("E", t.Data2);
		}
		[Test] public void TestParse2()
		{
			var t = new Thing{CmdLineOptionResult = true, CmdLineDataResult = true};
			var r  = CmdLine.Parse(t, new[]{"-A","B", "C", "D"});
			Assert.True(r == CmdLine.Result.Failed);
			Assert.AreEqual(1, t.HelpShownCount);
			Assert.AreEqual(0, t.ValidateCount);
			Assert.AreEqual("-a", t.Option);
			Assert.AreEqual("B", t.OptionArg1);
			Assert.AreEqual("C", t.OptionArg2);
			Assert.AreEqual("D", t.Data1);
			Assert.AreEqual(null, t.Data2);
		}
		[Test] public void TestParse3()
		{
			var t = new Thing{CmdLineOptionResult = true, CmdLineDataResult = true};
			var r  = CmdLine.Parse(t, new[]{"-A","X", "C", "D", "E"});
			Assert.True(r == CmdLine.Result.Failed);
			Assert.AreEqual(1, t.HelpShownCount);
			Assert.AreEqual(1, t.ValidateCount);
			Assert.AreEqual("-a", t.Option);
			Assert.AreEqual("X", t.OptionArg1);
			Assert.AreEqual("C", t.OptionArg2);
			Assert.AreEqual("D", t.Data1);
			Assert.AreEqual("E", t.Data2);
		}
	}
}
#endif