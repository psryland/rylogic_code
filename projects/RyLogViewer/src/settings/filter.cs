using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gui;
using Rylogic.Utility;

namespace RyLogViewer
{
	public class Filter :Pattern, IFilter, IFeatureTreeItem
	{
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
		public Filter(XElement node) :base(node)
		{
			IfMatch = node.Element(XmlTag.IfMatch).As<EIfMatch>();
		}

		/// <summary>Defines what a match with this filter means</summary>
		public EIfMatch IfMatch { get; set; }

		/// <summary>Export this highlight as xml</summary>
		public override XElement ToXml(XElement node)
		{
			base.ToXml(node);
			node.Add(IfMatch.ToXml(XmlTag.IfMatch ,false));
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

		#region Equals
		public override bool Equals(object obj)
		{
			var rhs = obj as Filter;
			return rhs != null
				&& base.Equals(obj)
				&& IfMatch.Equals(rhs.IfMatch);
		}
		public override int GetHashCode()
		{
			var hash = base.GetHashCode();
			return new { hash, IfMatch }.GetHashCode();
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
