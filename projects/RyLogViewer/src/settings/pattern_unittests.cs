using System.Linq;

#if PR_UNITTESTS
namespace pr
{
	using NUnit.Framework;
	using RyLogViewer;

	[TestFixture] internal static partial class RyLogViewerUnitTests
	{
		internal static class TestPattern
		{
			[TestFixtureSetUp] public static void Setup()
			{
			}
			[TestFixtureTearDown] public static void CleanUp()
			{
			}
			private static void Check(Pattern pat, string test, string[] grp_names, string[] captures)
			{
				if (grp_names == null || captures == null)
				{
					Assert.IsFalse(pat.IsMatch(test));
				}
				else
				{
					Assert.IsTrue(pat.IsMatch(test));

					var caps = pat.CaptureGroups(test).ToArray();
					Assert.AreEqual(grp_names.Length, caps.Length);
					Assert.AreEqual(captures.Length, caps.Length);

					for (int i = 0; i != caps.Length; ++i)
					{
						Assert.AreEqual(grp_names[i], caps[i].Key);
						Assert.AreEqual(captures[i], caps[i].Value);
					}
				}
			}
			[Test] public static void SubStringMatches0()
			{
				var p = new Pattern(EPattern.Substring, "test");
				Check(p, "A test string",
					new[]{"0"},
					new[]{"test"});
			}
			[Test] public static void SubStringMatches1()
			{
				var p = new Pattern(EPattern.Substring, "test"){WholeLine = true};
				Check(p, "A test string",
					null,
					null);
			}
			[Test] public static void WildcardMatches0()
			{
				var p = new Pattern(EPattern.Wildcard, "test");
				Check(p, "A test string",
					new[]{"0"},
					new[]{"test"});
			}
			[Test] public static void WildcardMatches1()
			{
				var p = new Pattern(EPattern.Wildcard, "*test");
				Check(p, "A test string",
					new[]{"0"},
					new[]{"A test"});
			}
			[Test] public static void WildcardMatches2()
			{
				var p = new Pattern(EPattern.Wildcard, "test*");
				Check(p, "A test string",
					new[]{"0"},
					new[]{"test string"});
			}
			[Test] public static void WildcardMatches3()
			{
				var p = new Pattern(EPattern.Wildcard, "A * string");
				Check(p, "A test string",
					new[]{"0"},
					new[]{"A test string"});
			}
			[Test] public static void WildcardMatches4()
			{
				var p = new Pattern(EPattern.Wildcard, "b*e?g");
				Check(p, "abcdefgh",
					new[]{"0"},
					new[]{"bcdefg"});
			}
			[Test] public static void WildcardMatches5()
			{
				var p = new Pattern(EPattern.Wildcard, "b*e?g");
				Check(p, "1b2345e6g7",
					new[]{"0"},
					new[]{"b2345e6g"});
			}
			[Test] public static void WildcardMatches6()
			{
				var p = new Pattern(EPattern.Wildcard, "b*e?g"){WholeLine = true};
				Check(p, "1b2345e6g7",
					null,
					null);
				Check(p, "b2345e6g",
					new[]{"0"},
					new[]{"b2345e6g"});
			}
			[Test] public static void RegexMatches0()
			{
				var p = new Pattern(EPattern.RegularExpression, "ax*b");
				Check(p, "ab",
					new[]{"0"},
					new[]{"ab"});
			}
			[Test] public static void RegexMatches1()
			{
				var p = new Pattern(EPattern.RegularExpression, "ax*b");
				Check(p, "axb",
					new[]{"0"},
					new[]{"axb"});
			}
			[Test] public static void RegexMatches2()
			{
				var p = new Pattern(EPattern.RegularExpression, "ax*b");
				Check(p, "axxxxb",
					new[]{"0"},
					new[]{"axxxxb"});
			}
			[Test] public static void RegexMatches3()
			{
				var p = new Pattern(EPattern.RegularExpression, "a.b"){WholeLine = true};
				Check(p, "eaxbe",
					null,
					null);
				Check(p, "axb",
					new[]{"0"},
					new[]{"axb"});
			}
			[Test] public static void RegexMatches4()
			{
				var p = new Pattern(EPattern.RegularExpression, @"f.ll st.p\.");
				Check(p, "full stop",
					null,
					null);
				Check(p, "full stop.",
					new[]{"0"},
					new[]{"full stop."});
			}
			[Test] public static void RegexMatches5()
			{
				var p = new Pattern(EPattern.RegularExpression, @"[aeiousy]");
				Check(p, "purple",
					new[]{"0"},
					new[]{"u"});
				Check(p, "monkey",
					new[]{"0"},
					new[]{"o"});
				Check(p, "dishwasher",
					new[]{"0"},
					new[]{"i"});
			}
			[Test] public static void RegexMatches6()
			{
				var p = new Pattern(EPattern.RegularExpression, @"[^abc]");
				Check(p, "boat",
					new[]{"0"},
					new[]{"o"});
				Check(p, "cat",
					new[]{"0"},
					new[]{"t"});
				Check(p, "#123",
					new[]{"0"},
					new[]{"#"});
			}
			[Test] public static void RegexMatches7()
			{
				var p = new Pattern(EPattern.RegularExpression, @"[A-C]");
				Check(p, "fat",
					null,
					null);
				Check(p, "cAt",
					new[]{"0"},
					new[]{"A"});
				Check(p, "BAT",
					new[]{"0"},
					new[]{"B"});
			}
			[Test] public static void RegexMatches8()
			{
				var p = new Pattern(EPattern.RegularExpression, @"\w");
				Check(p, "@You",
					new[]{"0"},
					new[]{"Y"});
				Check(p, "#123",
					new[]{"0"},
					new[]{"1"});
				Check(p, "_up_",
					new[]{"0"},
					new[]{"_"});
				Check(p, "@#!$%^-",
					null,
					null);
			}
			[Test] public static void RegexMatches9()
			{
				var p = new Pattern(EPattern.RegularExpression, @"\W");
				Check(p, "@You",
					new[]{"0"},
					new[]{"@"});
				Check(p, "#123",
					new[]{"0"},
					new[]{"#"});
				Check(p, "_up_",
					null,
					null);
				Check(p, "@#!$%^-",
					new[]{"0"},
					new[]{"@"});
			}
		}
	}
}
#endif
