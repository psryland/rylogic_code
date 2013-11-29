namespace pr.common
{
	public struct Span
	{
		/// <summary>The first index of a span</summary>
		public int Index;

		/// <summary>The number items in the span</summary>
		public int Count;

		/// <summary>The default empty span</summary>
		public static readonly Span Zero = new Span{Index = 0, Count = 0};

		/// <summary>An invalid span. Used as an initialiser when finding a bounding range</summary>
		public static readonly Span Invalid = new Span{Index = 0, Count = -1};

		/// <summary>The first index in the range<para/>Note: setting this value moves the span (i.e. Count is unchanged)</summary>
		public int Begin { get { return Index; } set { Index = value; } }

		/// <summary>One past the last index in the range<para/>Note: setting this value moves the span (i.e. Index *is* changed)</summary>
		public int End { get { return Index + Count; } set { Index = value - Count; } }

		/// <summary>True if 'i' is within this span</summary>
		public bool Contains(int i) { return i >= Index && i < Index + Count; }

		public Span(int index, int count)
		{
			Index = index;
			Count = count;
		}
		public override string ToString()       { return string.Format("[{0},{1})",Index,Index+Count); }
		public override bool Equals(object obj) { return obj is Span && Equals((Span)obj); }
		public bool Equals(Span other)          { return Index == other.Index && Count == other.Count; }
		public override int GetHashCode()       { unchecked { return (Index*397) ^ Count; } }
	}
}
