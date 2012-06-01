using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Xml.Linq;

namespace Rylogic_Log_Viewer
{
	public class Highlight :Pattern
	{
		/// <summary>Foreground colour of highlight</summary>
		public Color ForeColour { get; set; }
		
		/// <summary>Background colour of highlight</summary>
		public Color BackColour { get; set; }
		
		/// <summary>True if a match anywhere on the row highlights the full row</summary>
		public bool FullRow { get; set; }
		
		public Highlight()
		{
			ForeColour = Color.White;
			BackColour = Color.DarkRed;
			FullRow = true;
		}

		/// <summary>Construct from xml description</summary>
		public Highlight(XElement node) :base(node)
		{
			try
			{
				// ReSharper disable PossibleNullReferenceException
				ForeColour = Color.FromArgb(int.Parse(node.Element("forecolour").Value, NumberStyles.HexNumber));
				BackColour = Color.FromArgb(int.Parse(node.Element("backcolour").Value, NumberStyles.HexNumber));
				FullRow    = bool.Parse(node.Element("fullrow").Value);
				// ReSharper restore PossibleNullReferenceException
			} catch {} // swallow bad input data
		}

		/// <summary>Export this highlight as xml</summary>
		public override XElement ToXml(XElement node)
		{
			base.ToXml(node);
			node.Add
			(
				new XElement("forecolour" ,ForeColour.ToArgb().ToString("X")),
				new XElement("backcolour" ,BackColour.ToArgb().ToString("X")),
				new XElement("fullrow"    ,FullRow)
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
			foreach (XElement n in doc.Root.Elements("highlight"))
				list.Add(new Highlight(n));
			
			return list;
		}
		
		/// <summary>Serialise the highlight patterns to xml</summary>
		public static string Export(List<Highlight> highlights)
		{
			XDocument doc = new XDocument(new XElement("root"));
			if (doc.Root == null) return "";
			
			foreach (var hl in highlights)
				doc.Root.Add(hl.ToXml(new XElement("highlight")));
			
			return doc.ToString(SaveOptions.None);
		}
	}
}
