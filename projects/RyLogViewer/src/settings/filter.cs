using System.Collections.Generic;
using System.Xml.Linq;
using pr.util;

namespace RyLogViewer
{
	public enum EIfMatch
	{
		Keep,
		Reject,
	}

	public interface IFilter
	{
		/// <summary>Defines what a match with this filter means</summary>
		EIfMatch IfMatch { get; }

		/// <summary>Returns true if this pattern matches a substring in 'text'</summary>
		bool IsMatch(string text);
	}

	public class Filter :Pattern, IFilter
	{
		/// <summary>Defines what a match with this filter means</summary>
		public EIfMatch IfMatch { get; set; }

		/// <summary>A static instance of a 'KeepAll' filter</summary>
		public static readonly Filter KeepAll = new Filter{Expr = "", Invert = true, IfMatch = EIfMatch.Keep};

		/// <summary>Returns an instance of a 'RejectAll' filter</summary>
		public static readonly Filter RejectAll = new Filter{Expr = "", Invert = true, IfMatch = EIfMatch.Reject};

		public Filter()
		{
			IfMatch = EIfMatch.Keep;
		}
		public Filter(Filter rhs) :base(rhs)
		{
			IfMatch = rhs.IfMatch;
		}

		/// <summary>Construct from xml description</summary>
		public Filter(XElement node) :base(node)
		{
			// ReSharper disable PossibleNullReferenceException
			IfMatch = Enum<EIfMatch>.Parse(node.Element(XmlTag.IfMatch).Value);
			// ReSharper restore PossibleNullReferenceException
		}

		/// <summary>Export this highlight as xml</summary>
		public override XElement ToXml(XElement node)
		{
			base.ToXml(node);
			node.Add
			(
				new XElement(XmlTag.IfMatch ,IfMatch)
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
			foreach (XElement n in doc.Root.Elements(XmlTag.Filter))
				try { list.Add(new Filter(n)); } catch {} // Ignore those that fail
			
			return list;
		}
		
		/// <summary>Serialise the highlight patterns to xml</summary>
		public static string Export(IEnumerable<Filter> filters)
		{
			XDocument doc = new XDocument(new XElement(XmlTag.Root));
			if (doc.Root == null) return "";
			
			foreach (var hl in filters)
				doc.Root.Add(hl.ToXml(new XElement(XmlTag.Filter)));
			
			return doc.ToString(SaveOptions.None);
		}
		
		/// <summary>Creates a new object that is a copy of the current instance.</summary>
		public override object Clone()
		{
			return new Filter(this);
		}

		/// <summary>Value equality test</summary>
		public override bool Equals(object obj)
		{
			Filter rhs = obj as Filter;
			return 
				rhs != null
				&& base.Equals(obj)
				&& IfMatch.Equals(rhs.IfMatch)
				;
		}
		
		/// <summary>Value hash code</summary>
		public override int GetHashCode()
		{
			return
				base.GetHashCode()
				^IfMatch.GetHashCode()
				;
		}
	}
}
