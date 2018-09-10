using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Plugin;

namespace RyLogViewer
{
	/// <summary>A substitution that rearranges text</summary>
	[Plugin(typeof(ITransformSubstitution))]
	public class SubSwizzle :TransformSubstitutionBase
	{
		private string m_src;
		private string m_dst;
		private Map[] m_map;

		public class Map
		{
			public enum ECase { NoChange, ToLower, ToUpper };

			/// <summary>The char (in lower case) of the char block</summary>
			public char Char;

			/// <summary>The case changes to make</summary>
			public ECase[] Case;

			/// <summary>The start and length of the source characters</summary>
			public Range Src;

			/// <summary>The start and length of where to write the characters</summary>
			public Range Dst;
		}

		/// <summary>Construct a mapping from 'src' to 'dst'</summary>
		public static Map[] CreateMapping(string src, string dst)
		{
			var mapping = new List<Map>();
			var cas = new List<Map.ECase>();
			for (int i = 0; i != dst.Length;)
			{
				char ch = char.ToLowerInvariant(dst[i]);
				if (char.IsWhiteSpace(ch)) { ++i; continue; }

				// Check the char doesn't already exist
				foreach (var m in mapping)
					if (m.Char == ch)
						throw new ArgumentException("Character block '"+ch+"' is not contiguous in the output mapping");

				// Create a map block
				var map = new Map();
				map.Char = ch;

				// Read the span of chars from 'dst'
				map.Dst = new Range(i,i);
				for (; i != dst.Length && char.ToLowerInvariant(dst[i]) == ch; ++map.Dst.Size, ++i) {}

				// Find the corresponding span in 'src'
				int j; for (j = 0; j != src.Length && char.ToLowerInvariant(src[j]) != ch; ++j) {}

				// Read the span of chars from 'src'
				map.Src = new Range(j,j);
				for (; j != src.Length && char.ToLowerInvariant(src[j]) == ch; ++map.Src.Size, ++j) {}

				// Check that there are no other 'ch' blocks in 'src'
				for (; j != src.Length && char.ToLowerInvariant(src[j]) != ch; ++j) {}
				if (j != src.Length)
					throw new ArgumentException("Character block '"+ch+"' is not contiguous in the source mapping");

				// Read the case changes
				cas.Clear();
				for (int k = 0; k != map.Dst.Size; ++k)
				{
					int s = map.Src.Begi + (k%map.Src.Sizei);
					int d = map.Dst.Begi + k;
					cas.Add(
						src[s] == dst[d]
						? Map.ECase.NoChange : src[s] == char.ToLowerInvariant(src[s])
						? Map.ECase.ToUpper  : Map.ECase.ToLower);
				}
				map.Case = cas.ToArray();
				mapping.Add(map);
			}
			return mapping.ToArray();
		}

		/// <summary>Return 'text' swizzled</summary>
		public static string Apply(string text, IEnumerable<Map> mapping)
		{
			var sb = new StringBuilder();
			foreach (var m in mapping)
			{
				if (m.Src.Begi >= text.Length) continue;
				for (int i = 0, iend = Math.Min(m.Dst.Sizei, text.Length - m.Src.Begi); i != iend; ++i)
				{
					char ch = text[m.Src.Begi + (i % m.Src.Sizei)];
					switch (m.Case[i])
					{
					case Map.ECase.ToUpper: ch = char.ToUpperInvariant(ch); break;
					case Map.ECase.ToLower: ch = char.ToLowerInvariant(ch); break;
					}
					while (sb.Length < m.Dst.Begi) sb.Append(' ');
					sb.Append(ch);
				}
			}
			return sb.ToString();
		}

		/// <summary>
		/// A unique id for this text transform, used to associate
		/// saved configuration data with this transformation.</summary>
		public override Guid Guid { get { return new Guid("2F45569D-EFAF-4B0F-B41B-6756D4E9C56D"); } }

		/// <summary>
		/// The name that appears in the transform column drop down
		/// for this text transformation.</summary>
		public override string DropDownName { get { return "Swizzle"; } }

		/// <summary>True if this substitution can be configured</summary>
		public override bool Configurable { get { return true; } }

		/// <summary>
		/// Returns a summary of the configuration for this transform in a form suitable
		/// for the tooltip that is displayed when the user hovers their mouse over the
		/// selected transform. Return null to not display a tooltip</summary>
		public override string ConfigSummary { get { return m_map != null ? m_src + " -> " + m_dst : null; } }

		/// <summary>
		/// Called when a user selects to edit the configuration for this transform.
		/// Implementers should display a modal dialog that collects any necessary data
		/// for the text transform.</summary>
		public override void ShowConfigUI(Form main_window)
		{
			var dg = new SwizzleUI{Src = m_src, Dst = m_dst};
			if (dg.ShowDialog(main_window) != DialogResult.OK)
				return;

			m_src = dg.Src;
			m_dst = dg.Dst;
			m_map = CreateMapping(m_src, m_dst);
		}

		/// <summary>
		/// Returns the result of applying this text transform to 'captured_text'.
		/// This method provides the functionality of the text transform and should
		/// be efficiently implemented.</summary>
		public override string Result(string elem)
		{
			return m_map != null ? Apply(elem, m_map) : elem;
		}

		/// <summary>
		/// Save data for the transform to the provided xml node.
		/// This is used to persist per-instance settings for this text
		/// transform within the main RyLogViewer settings xml file.
		/// Implementers should add xml nodes to 'data_root'</summary>
		public override XElement ToXml(XElement node)
		{
			node.Add
			(
				m_src.ToXml(XmlTag.Src, false),
				m_dst.ToXml(XmlTag.Dst, false)
			);
			return node;
		}

		/// <summary>
		/// Load instance data for this transform from 'data_root'.
		/// This method should be the symmetric opposite of 'ToXml()'</summary>
		public override void FromXml(XElement node)
		{
			base.FromXml(node);
			m_src = node.Element(XmlTag.Src).As<string>();
			m_dst = node.Element(XmlTag.Dst).As<string>();
			m_map = CreateMapping(m_src, m_dst);
		}
	}
}
