using System;
using System.Text;

namespace pr.script
{
	/// <summary>Base class for a source of script characters</summary>
	public class Buffer :Src
	{
		private readonly StringBuilder m_buf;
		private readonly Src m_src;

		public Buffer(Src src)                { m_buf = new StringBuilder(); m_src = src; }
		public override void Dispose()        { m_buf.Clear(); m_src.Dispose(); }
		public override SrcType SrcType       { get { return m_src.SrcType; } }
		public override Loc Location          { get { return m_src.Location; } }

		/// <summary>The source being read from into the buffer</summary>
		public Src  Src
		{
			get { return m_src; }
		}

		/// <summary>True if the buffer is empty </summary>
		public bool Empty
		{
			get { return m_buf.Length == 0; }
		}

		/// <summary>The length of the buffered text</summary>
		public int  Length
		{
			get { return m_buf.Length; }
		}

		/// <summary>The text buffer contents</summary>
		public StringBuilder TextBuffer
		{
			get { return m_buf; }
		}

		/// <summary>Reset the text buffer</summary>
		public void Clear()
		{
			m_buf.Clear();
		}

		/// <summary>Index access to the text buffer</summary>
		public char this[int i]
		{
			get { Cache(i+1); return m_buf[i]; }
			set { Cache(i+1); m_buf[i] = value; }
		}

		/// <summary>Ensures 'n' characters are cached in the buffer. Negative values of 'n' have no effect</summary>
		public void Cache(int n = 1)
		{
			n -= m_buf.Length;
			for (; n-- > 0 && m_src.Peek != 0; m_buf.Append(m_src.Peek), m_src.Next()) {}
		}

		// String compare - note asymmetric, i.e. buf="abcd", str="ab", buf.match(str) == true
		public bool Match(string str) { return Match(str, str.Length); }
		public bool Match(string str, int count)
		{
			Cache(count);
			if (m_buf.Length < count)
				return false;

			int i;
			for (i = 0; i != count && str[i] == m_buf[i]; ++i) {}
			return i == count;
		}

		protected override char PeekInternal()
		{
			return Empty ? m_src.Peek : m_buf[0];
		}
		protected override void Advance(int n)
		{
			var d = Math.Min(n, m_buf.Length);
			m_buf.Remove(0, d);
			m_src.Next(n - d);
		}

		public override string ToString() { return Empty ? m_src.ToString() : m_buf.ToString(); }
	}
}

#if PR_UNITTESTS

namespace pr
{
	using NUnit.Framework;
	using script;

	[TestFixture] public static partial class UnitTests
	{
		internal static partial class TestScript
		{
			[Test] public static void TestBuffer()
			{
				const string str1 = "1234567890";
				var src = new StringSrc(str1);

				var buf = new Buffer(src);
				Assert.AreEqual(true, buf.Empty);
				Assert.AreEqual('1', buf.Peek);
				Assert.AreEqual(true, buf.Empty);
				Assert.AreEqual('1', buf[0]);
				Assert.AreEqual(1, buf.Length);
				Assert.AreEqual('2', buf[1]);
				Assert.AreEqual(2, buf.Length);
				Assert.AreEqual(true, buf.Match(str1, 4));
				Assert.AreEqual(4, buf.Length);

				buf.Next();
				Assert.AreEqual(3, buf.Length);
				Assert.AreEqual('2', buf.Peek);
				Assert.AreEqual(3, buf.Length);
				Assert.AreEqual('2', buf[0]);
				Assert.AreEqual(3, buf.Length);
				Assert.AreEqual('3', buf[1]);
				Assert.AreEqual(3, buf.Length);
				Assert.AreEqual(true, buf.Match(str1.Substring(1, 4)));
				Assert.AreEqual(4, buf.Length);
				Assert.AreEqual(true, !buf.Match("235"));
				Assert.AreEqual(4, buf.Length);

				buf.Next(4);
				Assert.AreEqual(true, buf.Empty);
				Assert.AreEqual(true, !buf.Match("6780"));
				Assert.AreEqual(4, buf.Length);
			}
		}
	}
}

#endif
