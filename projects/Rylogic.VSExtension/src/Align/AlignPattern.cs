using pr.common;

namespace Rylogic.VSExtension
{
	/// <summary>A pattern representing an alignment candidate</summary>
	public class AlignPattern :Pattern
	{
		/// <summary>The position of matching text is offset from the ideal alignment column by this amount</summary>
		public int Offset { get; set; }

		/// <summary>A comment to go with the pattern to remember what it is</summary>
		public string Comment { get; set; }

		public AlignPattern() :this(EPattern.Substring, string.Empty)
		{
		}
		public AlignPattern(EPattern type, string expr, int offset = 0, string comment = "") :base(type, expr)
		{
			Offset = offset;
			Comment = comment;
		}
		public AlignPattern(AlignPattern rhs) :base(rhs)
		{
			Offset  = rhs.Offset;
			Comment = rhs.Comment;
		}

		public override object Clone()
		{
			return new AlignPattern(this);
		}
	}
}
