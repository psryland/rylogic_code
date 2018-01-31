using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Extn;

namespace Rylogic.VSExtension
{
	/// <summary>A pattern representing an alignment candidate</summary>
	internal class AlignPattern :Pattern
	{
		/// <summary>The position of matching text is offset from the ideal alignment column by this amount</summary>
		public int Offset { get; set; }

		/// <summary>Matched text is padded to be at least this wide</summary>
		public int MinWidth { get; set; }

		/// <summary>A comment to go with the pattern to remember what it is</summary>
		public string Comment { get; set; }

		/// <summary>Allows derived patterns to optionally keep whitespace in Substring/wildcard patterns</summary>
		protected override bool PreserveWhitespace { get { return true; } }

		/// <summary>Returns the range of characters this pattern should occupy, relative to the aligning column</summary>
		public Range Position { get { return new Range(Offset, Offset + MinWidth); } }

		public AlignPattern() :this(EPattern.Substring, string.Empty)
		{
		}
		public AlignPattern(EPattern type, string expr, int offset = 0, int min_width = 0, string comment = "") :base(type, expr)
		{
			Offset   = offset;
			MinWidth = min_width;
			Comment  = comment;
		}
		public AlignPattern(AlignPattern rhs) :base(rhs)
		{
			Offset   = rhs.Offset;
			MinWidth = rhs.MinWidth;
			Comment  = rhs.Comment;
		}
		public AlignPattern(XElement node) :base(node)
		{
			Offset   = node.Element("offset"   ).As<int>();
			MinWidth = node.Element("min_width").As<int>();
			Comment  = node.Element("comment"  ).As<string>();
		}

		public override object Clone()
		{
			return new AlignPattern(this);
		}

		public override XElement ToXml(XElement node)
		{
			base.ToXml(node);
			node.Add
			(
				Offset   .ToXml("offset"   , false),
				MinWidth .ToXml("min_width", false),
				Comment  .ToXml("comment"  , false)
			);
			return node;
		}
	}
}
