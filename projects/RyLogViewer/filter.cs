using System.Collections.Generic;
using System.Xml.Linq;

namespace RyLogViewer
{
	public class Filter :Pattern
	{
		/// <summary>
		/// If true, then a positive match excludes the line, negative match includes the line.
		/// If false, then a positive match include the line, negative match excludes the line.</summary>
		public bool Exclude { get; set; }
		
		public Filter()
		{
			Exclude = false;
		}
		public Filter(Filter rhs) :base(rhs)
		{
			Exclude = rhs.Exclude;
		}

		/// <summary>Construct from xml description</summary>
		public Filter(XElement node) :base(node)
		{
			// ReSharper disable PossibleNullReferenceException
			try
			{
				Exclude =bool.Parse(node.Element("exclude").Value);
			} catch {} // swallow bad input data
			// ReSharper restore PossibleNullReferenceException
		}

		/// <summary>Export this highlight as xml</summary>
		public override XElement ToXml(XElement node)
		{
			base.ToXml(node);
			node.Add
			(
				new XElement("exclude" ,Exclude)
			);
			return node;
		}
		
		/// <summary>Reads an xml description of the highlight expressions</summary>
		public static List<Filter> Import(string filters)
		{
			var list = new List<Filter>();
			
			XDocument doc;
			try { doc = XDocument.Parse(filters); } catch { return list; }
			if (doc.Root == null) return list;
			foreach (XElement n in doc.Root.Elements("filter"))
				list.Add(new Filter(n));
			
			return list;
		}
		
		/// <summary>Serialise the highlight patterns to xml</summary>
		public static string Export(List<Filter> filters)
		{
			XDocument doc = new XDocument(new XElement("root"));
			if (doc.Root == null) return "";
			
			foreach (var hl in filters)
				doc.Root.Add(hl.ToXml(new XElement("filter")));
			
			return doc.ToString(SaveOptions.None);
		}
		
		/// <summary>Creates a new object that is a copy of the current instance.</summary>
		public override object Clone()
		{
			return new Filter(this);
		}
	}
}
