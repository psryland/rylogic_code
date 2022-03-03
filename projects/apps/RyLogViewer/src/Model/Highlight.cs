using System.Collections.Generic;
using System.Windows.Media;
using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Extn;

namespace RyLogViewer
{
	public class Highlight : Pattern, IFilter, IFeatureTreeItem
	{
		public Highlight()
		{
			ForeColour = Colors.White;
			BackColour = Colors.DarkRed;
			BinaryMatch = true;
		}
		public Highlight(Highlight rhs)
			: base(rhs)
		{
			ForeColour = rhs.ForeColour;
			BackColour = rhs.BackColour;
			BinaryMatch = rhs.BinaryMatch;
		}
		public Highlight(XElement node)
			: base(node)
		{
			ForeColour = node.Element(nameof(ForeColour)).As<Color>();
			BackColour = node.Element(nameof(BackColour)).As<Color>();
			BinaryMatch = node.Element(nameof(BinaryMatch)).As<bool>();
		}
		public override XElement ToXml(XElement node)
		{
			base.ToXml(node);
			node.Add2(nameof(ForeColour), ForeColour, false);
			node.Add2(nameof(BackColour), BackColour, false);
			node.Add2(nameof(BinaryMatch), BinaryMatch, false);
			return node;
		}

		/// <summary>Foreground colour of highlight</summary>
		public Color ForeColour { get; set; }

		/// <summary>Background colour of highlight</summary>
		public Color BackColour { get; set; }

		/// <summary>True if a match anywhere on the row is considered a match for the full row</summary>
		public bool BinaryMatch { get; set; }

		/// <summary>Defines what a match with this filter means</summary>
		public EIfMatch IfMatch => EIfMatch.Keep;

		/// <summary>Reads an xml description of the highlight expressions</summary>
		public static List<Highlight> Import(string highlights)
		{
			var list = new List<Highlight>();

			XDocument doc;
			try { doc = XDocument.Parse(highlights); } catch { return list; }
			if (doc.Root == null)
				return list;

			foreach (var n in doc.Root.Elements(nameof(Highlight)))
				try { list.Add(new Highlight(n)); } catch { } // Ignore those that fail

			return list;
		}

		/// <summary>Serialise the highlight patterns to xml</summary>
		public static string Export(IEnumerable<Highlight> highlights)
		{
			var root = new XElement("root");
			foreach (var hl in highlights)
				root.Add(hl.ToXml(new XElement(nameof(Highlight))));

			return root.ToString(SaveOptions.None);
		}

		#region Equals
		public bool Equals(Highlight rhs)
		{
			return rhs != null
				&& base.Equals(rhs)
				&& Equals(ForeColour, rhs.ForeColour)
				&& Equals(BackColour, rhs.BackColour)
				&& Equals(BinaryMatch, rhs.BinaryMatch);
		}
		public override bool Equals(object? obj)
		{
			return obj is Highlight hl && Equals(hl);
		}
		public override int GetHashCode()
		{
			var hash = base.GetHashCode();
			return new { hash, ForeColour, BackColour, BinaryMatch }.GetHashCode();
		}
		#endregion

		#region IFeatureTreeItem
		string IFeatureTreeItem.Name
		{
			get { return Expr; }
		}
		IEnumerable<IFeatureTreeItem> IFeatureTreeItem.Children
		{
			get { yield break; }
		}
		bool IFeatureTreeItem.Allowed
		{
			get;
			set;
		}
		#endregion
	}
}
