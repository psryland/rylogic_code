using System;
using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Extn;

namespace Rylogic.TextAligner
{
	/// <summary>A pattern representing an alignment candidate</summary>
	public class AlignPattern :Pattern
	{
		public AlignPattern()
			:this(EPattern.Substring, string.Empty)
		{
		}
		public AlignPattern(EPattern type, string expr, int offset = 0, int min_width = 0, string comment = "")
			:base(type, expr)
		{
			Offset   = offset;
			MinWidth = min_width;
			Comment  = comment;
		}
		public AlignPattern(AlignPattern rhs)
			:base(rhs)
		{
			Offset   = rhs.Offset;
			MinWidth = rhs.MinWidth;
			Comment  = rhs.Comment;
		}
		public AlignPattern(XElement node)
			:base(node)
		{
			Offset   = node.Element(nameof(Offset)).As<int>();
			MinWidth = node.Element(nameof(MinWidth)).As<int>();
			Comment  = node.Element(nameof(Comment)).As<string>();
		}
		public override XElement ToXml(XElement node)
		{
			base.ToXml(node);
			node.Add2(nameof(Offset), Offset, false);
			node.Add2(nameof(MinWidth), MinWidth, false);
			node.Add2(nameof(Comment), Comment, false);
			return node;
		}

		/// <summary>The position of matching text is offset from the ideal alignment column by this amount</summary>
		public int Offset
		{
			get;
			set
			{
				if (field == value) return;
				field = value;
				NotifyPropertyChanged(nameof(Offset));
			}
		}

		/// <summary>Matched text is padded to be at least this wide</summary>
		public int MinWidth
		{
			get;
			set
			{
				if (field == value) return;
				field = Math.Max(value, 0);
				NotifyPropertyChanged(nameof(MinWidth));
			}
		}

		/// <summary>A comment to go with the pattern to remember what it is</summary>
		public string Comment
		{
			get => m_comment ?? Description;
			set
			{
				if (m_comment == value) return;
				m_comment = value;
				NotifyPropertyChanged(nameof(Comment));
			}
		}
		private string? m_comment;

		/// <summary>Allows derived patterns to optionally keep whitespace in Substring/wildcard patterns</summary>
		protected override bool PreserveWhitespace => true;

		/// <summary>Returns the range of characters this pattern should occupy, relative to the aligning column</summary>
		public RangeI Position => new(Offset, Offset + MinWidth);

		/// <summary></summary>
		public override object Clone() => new AlignPattern(this);
	}
}
