using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using pr.common;

namespace RyLogViewer
{
	/// <summary>A substitution that rearranges text</summary>
	[Export(typeof(ITxfmSub))]
	public class Swizzle :TxfmSubBase
	{
		public class Map
		{
			public enum ECase { NoChange, ToLower, ToUpper };

			/// <summary>The char (in lowercase) of the char block</summary>
			public char Char;
			
			/// <summary>The case changes to make</summary>
			public ECase[] Case;
			
			/// <summary>The start and length of the source characters</summary>
			public Span Src;
			
			/// <summary>The start and length of where to write the characters</summary>
			public Span Dst;
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
						throw new ArgumentException("Character block '"+ch+"' is not contiguous");
				
				// Create a map block
				var map = new Map();
				map.Char = ch;
				
				// Read the span of chars from 'dst'
				map.Dst = new Span(i,0);
				for (; i != dst.Length && char.ToLowerInvariant(dst[i]) == ch; ++map.Dst.Count, ++i) {}
				
				// Find the corresponding span in 'src'
				int j; for (j = 0; j != src.Length && char.ToLowerInvariant(src[j]) != ch; ++j) {}
				
				// Read the span of chars from 'src'
				map.Src = new Span(j,0);
				for (; j != src.Length && char.ToLowerInvariant(src[j]) == ch; ++map.Src.Count, ++j) {}
				
				//// map.Dst.Count must be <= map.Src.Count
				//if (map.Src.Count < map.Dst.Count)
				//    throw new ArgumentException("Output mapping block '"+ch+"' is longer than the source block");
				
				// Read the case changes
				cas.Clear();
				for (int k = 0; k != map.Dst.Count; ++k)
				{
					int s = map.Src.Index + (k%map.Src.Count);
					int d = map.Dst.Index + k;
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
			foreach (Map m in mapping)
			{
				if (m.Src.Index >= text.Length) continue;
				for (int i = 0, iend = Math.Min(m.Dst.Count, text.Length - m.Src.Index); i != iend; ++i)
				{
					char ch = text[m.Src.Index + (i % m.Src.Count)];
					switch (m.Case[i])
					{
					case Map.ECase.ToUpper: ch = char.ToUpperInvariant(ch); break;
					case Map.ECase.ToLower: ch = char.ToLowerInvariant(ch); break;
					}
					while (sb.Length < m.Dst.Index) sb.Append(' ');
					sb.Append(ch);
				}
			}
			return sb.ToString();
		}

		private string m_src;
		private string m_dst;
		private Map[] m_map;
		
		public Swizzle() :base("Swizzle") {}
		
		/// <summary>A summary of the configuration for this transform substitution</summary>
		public override string ConfigSummary
		{
			get { return m_map == null ? "<click here to configure>" : m_src + " -> " + m_dst; }
		}

		/// <summary>A method to setup the transform substitution's specific data</summary>
		public override void Config(IWin32Window owner)
		{
			var dg = new SwizzleUI{Src = m_src, Dst = m_dst};
			if (dg.ShowDialog(owner) != DialogResult.OK) return;
			m_src = dg.Src;
			m_dst = dg.Dst;
			m_map = CreateMapping(m_src, m_dst);
		}

		/// <summary>Returns 'elem' transformed</summary>
		public override string Result(string elem)
		{
			return m_map == null ? elem : Apply(elem, m_map);
		}

		/// <summary>Serialise data for the substitution to an xml node</summary>
		public override XElement ToXml(XElement node)
		{
			node.Add
			(
				new XElement(XmlTag.Src, m_src),
				new XElement(XmlTag.Dst, m_dst)
			);
			return node;
		}

		/// <summary>Deserialise data for the substitution from an xml node</summary>
		public override void FromXml(XElement node)
		{
			// ReSharper disable PossibleNullReferenceException
			base.FromXml(node);
			m_src = node.Element(XmlTag.Src).Value;
			m_dst = node.Element(XmlTag.Dst).Value;
			m_map = CreateMapping(m_src, m_dst);
			// ReSharper restore PossibleNullReferenceException
		}
	}
}
