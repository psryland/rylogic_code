using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Xml.Linq;

namespace RyLogViewer
{
	public class Highlight :Pattern, IFilter
	{
		/// <summary>Defines what a match with this filter means</summary>
		public EIfMatch IfMatch { get { return EIfMatch.Keep; } }

		/// <summary>Foreground colour of highlight</summary>
		public Color ForeColour { get; set; }

		/// <summary>Background colour of highlight</summary>
		public Color BackColour { get; set; }

		public Highlight()
		{
			ForeColour = Color.White;
			BackColour = Color.DarkRed;
		}
		public Highlight(Highlight rhs) :base(rhs)
		{
			ForeColour = rhs.ForeColour;
			BackColour = rhs.BackColour;
		}

		/// <summary>Construct from xml description</summary>
		public Highlight(XElement node) :base(node)
		{
			// ReSharper disable PossibleNullReferenceException
			ForeColour = Color.FromArgb(int.Parse(node.Element(XmlTag.ForeColour).Value, NumberStyles.HexNumber));
			BackColour = Color.FromArgb(int.Parse(node.Element(XmlTag.BackColour).Value, NumberStyles.HexNumber));
			// ReSharper restore PossibleNullReferenceException
		}

		/// <summary>Export this highlight as xml</summary>
		public override XElement ToXml(XElement node)
		{
			base.ToXml(node);
			node.Add
			(
				new XElement(XmlTag.ForeColour ,ForeColour.ToArgb().ToString("X")),
				new XElement(XmlTag.BackColour ,BackColour.ToArgb().ToString("X"))
			);
			return node;
		}

		/// <summary>Reads an xml description of the highlight expressions</summary>
		public static List<Highlight> Import(string highlights)
		{
			var list = new List<Highlight>();
			
			XDocument doc;
			try { doc = XDocument.Parse(highlights); } catch { return list; }
			if (doc.Root == null) return list;
			foreach (XElement n in doc.Root.Elements(XmlTag.Highlight))
				try { list.Add(new Highlight(n)); } catch {} // Ignore those that fail
			
			return list;
		}

		/// <summary>Serialise the highlight patterns to xml</summary>
		public static string Export(IEnumerable<Highlight> highlights)
		{
			XDocument doc = new XDocument(new XElement(XmlTag.Root));
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

		/// <summary>Value equality test</summary>
		public override bool Equals(object obj)
		{
			var rhs = obj as Highlight;
			return rhs != null
				&& base.Equals(obj)
				&& ForeColour.Equals(rhs.ForeColour)
				&& BackColour.Equals(rhs.BackColour);
		}

		/// <summary>Value hash code</summary>
		public override int GetHashCode()
		{
			return
				base.GetHashCode()^
				ForeColour.GetHashCode()^
				BackColour.GetHashCode();
		}
	}
}
