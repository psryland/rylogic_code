using System.Collections.Generic;
using System.Drawing;
using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gui.WinForms;

namespace RyLogViewer
{
	public class Highlight :Pattern, IFilter, IFeatureTreeItem
	{
		public Highlight()
		{
			ForeColour  = Color.White;
			BackColour  = Color.DarkRed;
			BinaryMatch = true;
		}
		public Highlight(Highlight rhs) :base(rhs)
		{
			ForeColour  = rhs.ForeColour;
			BackColour  = rhs.BackColour;
			BinaryMatch = rhs.BinaryMatch;
		}
		public Highlight(XElement node) :base(node)
		{
			ForeColour  = node.Element(XmlTag.ForeColour).As<Color>();
			BackColour  = node.Element(XmlTag.BackColour).As<Color>();
			BinaryMatch = node.Element(XmlTag.Binary).As<bool>();
		}

		/// <summary>Foreground colour of highlight</summary>
		public Color ForeColour { get; set; }

		/// <summary>Background colour of highlight</summary>
		public Color BackColour { get; set; }

		/// <summary>True if a match anywhere on the row is considered a match for the full row</summary>
		public bool BinaryMatch { get; set; }

		/// <summary>Defines what a match with this filter means</summary>
		public EIfMatch IfMatch
		{
			get { return EIfMatch.Keep; }
		}

		/// <summary>Export this highlight as xml</summary>
		public override XElement ToXml(XElement node)
		{
			base.ToXml(node);
			node.Add2(XmlTag.ForeColour, ForeColour, false);
			node.Add2(XmlTag.BackColour, BackColour, false);
			node.Add2(XmlTag.Binary, BinaryMatch, false);
			return node;
		}

		/// <summary>Reads an xml description of the highlight expressions</summary>
		public static List<Highlight> Import(string highlights)
		{
			var list = new List<Highlight>();

			XDocument doc;
			try { doc = XDocument.Parse(highlights); } catch { return list; }
			if (doc.Root == null) return list;
			foreach (var n in doc.Root.Elements(XmlTag.Highlight))
				try { list.Add(new Highlight(n)); } catch {} // Ignore those that fail

			return list;
		}

		/// <summary>Serialise the highlight patterns to xml</summary>
		public static string Export(IEnumerable<Highlight> highlights)
		{
			var doc = new XDocument(new XElement(XmlTag.Root));
			if (doc.Root == null) return "";

			foreach (var hl in highlights)
				doc.Root.Add(hl.ToXml(new XElement(XmlTag.Highlight)));

			return doc.ToString(SaveOptions.None);
		}

		/// <summary>Creates a new object that is a copy of the current instance.</summary>
		public override object Clone()
		{
			return MemberwiseClone();
		}

		#region Equals
		public override bool Equals(object obj)
		{
			var rhs = obj as Highlight;
			return rhs != null
				&& base.Equals(obj)
				&& Equals(ForeColour, rhs.ForeColour)
			    && Equals(BackColour, rhs.BackColour)
			    && Equals(BinaryMatch, rhs.BinaryMatch);
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
