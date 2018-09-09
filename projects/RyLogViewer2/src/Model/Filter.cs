using System.Collections.Generic;
using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Extn;

namespace RyLogViewer
{
	public class Filter : Pattern, IFilter, IFeatureTreeItem
	{
		/// <summary>A static instance of a 'KeepAll' filter</summary>
		public static readonly Filter KeepAll = new Filter { Expr = "", Invert = true, IfMatch = EIfMatch.Keep };

		/// <summary>Returns an instance of a 'RejectAll' filter</summary>
		public static readonly Filter RejectAll = new Filter { Expr = "", Invert = true, IfMatch = EIfMatch.Reject };

		public Filter()
		{
			IfMatch = EIfMatch.Keep;
		}
		public Filter(Filter rhs)
			: base(rhs)
		{
			IfMatch = rhs.IfMatch;
		}
		public Filter(XElement node)
			: base(node)
		{
			IfMatch = node.Element(nameof(IfMatch)).As<EIfMatch>();
		}
		public override XElement ToXml(XElement node)
		{
			base.ToXml(node);
			node.Add2(nameof(IfMatch), IfMatch, false);
			return node;
		}

		/// <summary>Defines what a match with this filter means</summary>
		public EIfMatch IfMatch { get; set; }

		/// <summary>Reads an xml description of the highlight expressions</summary>
		public static List<Filter> Import(string filters)
		{
			var list = new List<Filter>();

			XDocument doc;
			try { doc = XDocument.Parse(filters); } catch { return list; }
			if (doc.Root == null)
				return list;

			foreach (var n in doc.Root.Elements(nameof(Filter)))
				try { list.Add(new Filter(n)); } catch { } // Ignore those that fail

			return list;
		}

		/// <summary>Serialise the highlight patterns to xml</summary>
		public static string Export(IEnumerable<Filter> filters)
		{
			var root = new XElement("root");
			foreach (var hl in filters)
				root.Add(hl.ToXml(new XElement(nameof(Filter))));

			return root.ToString(SaveOptions.None);
		}

		#region Equals
		public bool Equals(Filter rhs)
		{
			return rhs != null
				&& base.Equals(rhs)
				&& Equals(IfMatch, rhs.IfMatch);
		}
		public override bool Equals(object obj)
		{
			return obj is Filter ft && Equals(ft);
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
