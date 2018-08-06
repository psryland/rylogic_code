import { Pattern, EPattern } from '../../src/common/pattern';
import { expect } from 'chai';
import 'mocha'

describe('Pattern Helper', function()
{
	// /** Matches 'pat' to 'test' and checks the results agree with 'captures' */
		// function // Check(pat:Pattern, test:string, captures?:string[]): boolean
	// {
	// 	if (captures === undefined)
	// 		return pat.IsMatch(test) == false;

	// 	if (!pat.IsMatch(test))
	// 		return false;
	
	// 	var caps = pat.Match(test);
	// 	if (captures.length != caps.length)
	// 		return false;
		
	// 	for (let i = 0; i != caps.length; ++i)
	// 		if (captures[i] != caps[i])
	// 			return false;
		
	// 	return true;
	// }
	
	it("should match a sub string", function()
	{
		let p = new Pattern({ patn_type: EPattern.Substring, expr: "test", whole_line: false });
		let m = p.Match("A test string");
		expect(m.length).to.equal(1);
		expect(m[0]).to.equal("test");
	});
	it("should not match a sub string when WholeLine is true", function()
	{
		var p = new Pattern({ patn_type: EPattern.Substring, expr: "test", whole_line: true });
		let m = p.Match("A test string");
		expect(m).to.equal(null);
	});
});


// 		[Test] public void SubStringMatches2()
// 		{
// 			var p = new Pattern(EPattern.Substring, "test ");
// 			Check(p, "A test string",
// 				new[]{"0"},
// 				new[]{"test "});
// 		}
// 		[Test] public void SubStringMatches3()
// 		{
// 			var p = new Pattern(EPattern.Substring, "string ");
// 			Check(p, "A test string",
// 				new[]{"0"},
// 				new[]{"string"});
// 		}
// 		[Test] public void WildcardMatches0()
// 		{
// 			var p = new Pattern(EPattern.Wildcard, "test");
// 			Check(p, "A test string",
// 				new[]{"0"},
// 				new[]{"test"});
// 		}
// 		[Test] public void WildcardMatches1()
// 		{
// 			var p = new Pattern(EPattern.Wildcard, "*test");
// 			Check(p, "A test string",
// 				new[]{"0", "1"},
// 				new[]{"A test", "A "});
// 		}
// 		[Test] public void WildcardMatches2()
// 		{
// 			var p = new Pattern(EPattern.Wildcard, "test*");
// 			Check(p, "A test string",
// 				new[]{"0", "1"},
// 				new[]{"test string", " string"});
// 		}
// 		[Test] public void WildcardMatches3()
// 		{
// 			var p = new Pattern(EPattern.Wildcard, "A * string");
// 			Check(p, "A test string",
// 				new[]{"0", "1"},
// 				new[]{"A test string", "test"});
// 		}
// 		[Test] public void WildcardMatches4()
// 		{
// 			var p = new Pattern(EPattern.Wildcard, "b*e?g");
// 			Check(p, "abcdefgh",
// 				new[]{"0", "1", "2"},
// 				new[]{"bcdefg", "cd", "f"});
// 		}
// 		[Test] public void WildcardMatches5()
// 		{
// 			var p = new Pattern(EPattern.Wildcard, "b*e?g");
// 			Check(p, "1b2345e6g7",
// 				new[]{"0", "1", "2"},
// 				new[]{"b2345e6g", "2345","6"});
// 		}
// 		[Test] public void WildcardMatches6()
// 		{
// 			var p = new Pattern(EPattern.Wildcard, "b*e?g"){WholeLine = true};
// 			Check(p, "1b2345e6g7",
// 				null,
// 				null);
// 			Check(p, "b2345e6g",
// 				new[]{"0","1","2"},
// 				new[]{"b2345e6g","2345","6"});
// 		}
// 		[Test] public void RegexMatches0()
// 		{
// 			var p = new Pattern(EPattern.RegularExpression, "ax*b");
// 			Check(p, "ab",
// 				new[]{"0"},
// 				new[]{"ab"});
// 		}
// 		[Test] public void RegexMatches1()
// 		{
// 			var p = new Pattern(EPattern.RegularExpression, "ax*b");
// 			Check(p, "axb",
// 				new[]{"0"},
// 				new[]{"axb"});
// 		}
// 		[Test] public void RegexMatches2()
// 		{
// 			var p = new Pattern(EPattern.RegularExpression, "ax*b");
// 			Check(p, "axxxxb",
// 				new[]{"0"},
// 				new[]{"axxxxb"});
// 		}
// 		[Test] public void RegexMatches3()
// 		{
// 			var p = new Pattern(EPattern.RegularExpression, "a.b"){WholeLine = true};
// 			Check(p, "eaxbe",
// 				null,
// 				null);
// 			Check(p, "axb",
// 				new[]{"0"},
// 				new[]{"axb"});
// 		}
// 		[Test] public void RegexMatches4()
// 		{
// 			var p = new Pattern(EPattern.RegularExpression, @"f.ll st.p\.");
// 			Check(p, "full stop",
// 				null,
// 				null);
// 			Check(p, "full stop.",
// 				new[]{"0"},
// 				new[]{"full stop."});
// 		}
// 		[Test] public void RegexMatches5()
// 		{
// 			var p = new Pattern(EPattern.RegularExpression, @"[aeiousy]");
// 			Check(p, "purple",
// 				new[]{"0"},
// 				new[]{"u"});
// 			Check(p, "monkey",
// 				new[]{"0"},
// 				new[]{"o"});
// 			Check(p, "dishwasher",
// 				new[]{"0"},
// 				new[]{"i"});
// 		}
// 		[Test] public void RegexMatches6()
// 		{
// 			var p = new Pattern(EPattern.RegularExpression, @"[^abc]");
// 			Check(p, "boat",
// 				new[]{"0"},
// 				new[]{"o"});
// 			Check(p, "cat",
// 				new[]{"0"},
// 				new[]{"t"});
// 			Check(p, "#123",
// 				new[]{"0"},
// 				new[]{"#"});
// 		}
// 		[Test] public void RegexMatches7()
// 		{
// 			var p = new Pattern(EPattern.RegularExpression, @"[A-C]");
// 			Check(p, "fat",
// 				null,
// 				null);
// 			Check(p, "cAt",
// 				new[]{"0"},
// 				new[]{"A"});
// 			Check(p, "BAT",
// 				new[]{"0"},
// 				new[]{"B"});
// 		}
// 		[Test] public void RegexMatches8()
// 		{
// 			var p = new Pattern(EPattern.RegularExpression, @"\w");
// 			Check(p, "@You",
// 				new[]{"0"},
// 				new[]{"Y"});
// 			Check(p, "#123",
// 				new[]{"0"},
// 				new[]{"1"});
// 			Check(p, "_up_",
// 				new[]{"0"},
// 				new[]{"_"});
// 			Check(p, "@#!$%^-",
// 				null,
// 				null);
// 		}
// 		[Test] public void RegexMatches9()
// 		{
// 			var p = new Pattern(EPattern.RegularExpression, @"\W");
// 			Check(p, "@You",
// 				new[]{"0"},
// 				new[]{"@"});
// 			Check(p, "#123",
// 				new[]{"0"},
// 				new[]{"#"});
// 			Check(p, "_up_",
// 				null,
// 				null);
// 			Check(p, "@#!$%^-",
// 				new[]{"0"},
// 				new[]{"@"});
// 		}
// 		[Test] public void RegexMatches10()
// 		{
// 			const string s = "xoxox";
// 			var p = new Pattern(EPattern.RegularExpression, @"(x)");

// 			// Match returns the whole expr match, then the capture group
// 			var r1 = p.Match(s).ToList();
// 			Assert.True(r1.SequenceEqual(new[]{ new Range(0,1), new Range(0,1) }));

// 			// AllMatches returns only whole expr matches but all occurrences in the string
// 			var r2 = p.AllMatches(s).ToList();
// 			Assert.True(r2.SequenceEqual(new[]{ new Range(0,1), new Range(2,3), new Range(4,5) }));
// 		}
// 	}
// }
