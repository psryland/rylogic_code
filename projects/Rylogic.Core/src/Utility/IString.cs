//***************************************************
// Utility Functions
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rylogic.Utility
{
	/// <summary>An interface for string-like objects (typically StringBuilder or System.String)</summary>
	public abstract class IString :IEnumerable<char>
	{
		protected readonly int m_ofs, m_length;
		protected IString() :this(0, 0) {}
		protected IString(int ofs, int length)
		{
			m_ofs = ofs;
			m_length = length;
		}

		public abstract int Length       { get; }
		public abstract char this[int i] { get; }
		public abstract IString Substring(int ofs, int length);

		/// <summary>
		/// Split the string into substrings at places where 'pred' returns >= 0.
		/// int Pred(IString s, int idx) - "returns the length of the separater that starts at 's[idx]'".
		/// So < 0 means not a separater</summary>
		public IEnumerable<IString> Split(Func<IString, int,int> pred)
		{
			if (Length == 0) yield break;
			for (int s = 0, e, end = Length;;)
			{
				int sep_len = 0;
				for (e = s; e != end && (sep_len = pred(this,e)) < 0; ++e) {}
				yield return Substring(s, e - s);
				if (e == end) break;
				s = e + sep_len;
			}
		}
		public IEnumerable<IString> Split(params char[] sep)
		{
			return Split((s,i) => sep.Contains(s[i]) ? 1 : -1);
		}

		/// <summary>Trim chars from the front/back of the string</summary>
		public IString Trim(params char[] ch)
		{
			int s = 0, e = m_length;
			for (; s != m_length && ch.Contains(this[s]); ++s) {}
			for (; e != s && ch.Contains(this[e-1]); --e) {}
			return Substring(s, e - s);
		}

		IEnumerator<char> IEnumerable<char>.GetEnumerator()
		{
			for (int i = 0, iend = i + m_length; i != iend; ++i)
				yield return this[i];
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<char>)this).GetEnumerator();
		}

		public static implicit operator string(IString s)        { return s.ToString() ?? throw new NullReferenceException(); }
		public static implicit operator IString(string s)        { return new StringProxy(s); }
		public static implicit operator IString(StringBuilder s) { return new StringBuilderProxy(s); }
		public static implicit operator IString(char[] s)        { return new CharArrayProxy(s); }

		public static IString From(string s        , int ofs = 0, int length = int.MaxValue) { return new StringProxy(s, ofs, length); }
		public static IString From(StringBuilder s , int ofs = 0, int length = int.MaxValue) { return new StringBuilderProxy(s, ofs, length); }
		public static IString From(char[] s        , int ofs = 0, int length = int.MaxValue) { return new CharArrayProxy(s, ofs, length); }

		private class StringProxy :IString
		{
			private readonly string m_str;
			public StringProxy(string s, int ofs = 0, int length = int.MaxValue)
				:base(ofs, Math.Min(s.Length, length))
			{
				m_str = s;
			}
			public override int Length
			{
				get { return m_length; }
			}
			public override char this[int i]
			{
				get { return m_str[m_ofs + i]; }
			}
			public override IString Substring(int ofs, int length)
			{
				return new StringProxy(m_str, m_ofs + ofs, length);
			}
			public override string ToString()
			{
				return m_str.Substring(m_ofs, m_length);
			}
		}
		private class StringBuilderProxy :IString
		{
			private readonly StringBuilder m_str;
			public StringBuilderProxy(StringBuilder s, int ofs = 0, int length = int.MaxValue)
				:base(ofs, Math.Min(s.Length, length))
			{
				m_str = s;
			}
			public override int Length
			{
				get { return m_str.Length; }
			}
			public override char this[int i]
			{
				get { return m_str[m_ofs + i]; }
			}
			public override IString Substring(int ofs, int length)
			{
				return new StringBuilderProxy(m_str, m_ofs + ofs, length);
			}
			public override string ToString()
			{
				return m_str.ToString(m_ofs, m_length);
			}
		}
		private class CharArrayProxy :IString
		{
			private readonly char[] m_str;
			public CharArrayProxy(char[] s, int ofs = 0, int length = int.MaxValue)
				:base(ofs, Math.Min(s.Length, length))
			{
				m_str = s;
			}
			public override int Length
			{
				get { return m_str.Length; }
			}
			public override char this[int i]
			{
				get { return m_str[m_ofs + i]; }
			}
			public override IString Substring(int ofs, int length)
			{
				return new CharArrayProxy(m_str, m_ofs + ofs, length);
			}
			public override string ToString()
			{
				return new string(m_str, m_ofs, m_length);
			}
		}
	}
}
#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Utility;

	[TestFixture]
	public class TestIString
	{
		[Test]
		public void StringProxy()
		{
			static char func(IString s, int i) => s[i];

			var s0 = "Hello World";
			var s1 = new StringBuilder("Hello World");
			var s2 = "Hello World".ToCharArray();

			Assert.Equal(func(s0, 6), 'W');
			Assert.Equal(func(s1, 6), 'W');
			Assert.Equal(func(s2, 6), 'W');
		}
	}
}
#endif