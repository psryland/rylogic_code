using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Forms;
using System.Xml.Linq;

namespace RyLogViewer
{
	/// <summary>A substitution that swaps code values for text values</summary>
	[Export(typeof(ITxfmSub))]
	public class CodeLookup :TxfmSubBase
	{
		private readonly Dictionary<string, string> m_values;  // The code lookup table
		
		protected CodeLookup()
		:base("Code Lookup")
		{
			m_values = new Dictionary<string, string>();
		}

		/// <summary>A summary of the configuration for this transform substitution</summary>
		public override string ConfigSummary
		{
			get { return m_values.Count == 0 ? "<click here to configure>" : string.Format("{0} lookup codes", m_values.Count); }
		}
		
		/// <summary>A method to setup the transform substitution's specific data</summary>
		public override void Config(IWin32Window owner)
		{
			var dg = new CodeLookupUI(m_values);
			if (dg.ShowDialog(owner) != DialogResult.OK) return;
			
			m_values.Clear();
			foreach (var v in dg.Values)
				m_values.Add(v.Key, v.Value);
		}

		/// <summary>Returns 'elem' transformed</summary>
		public override string Result(string elem)
		{
			string result;
			return m_values.TryGetValue(elem, out result) ? result : elem;
		}
		
		/// <summary>Serialise data for the substitution to an xml node</summary>
		public override XElement ToXml(XElement node)
		{
			var codes = new XElement(XmlTag.CodeValues);
			foreach (var v in m_values)
				codes.Add(new XElement(XmlTag.CodeValue,
					new XElement(XmlTag.Code , v.Key),
					new XElement(XmlTag.Value, v.Value)
					));
			node.Add(codes);
			return node;
		}

		/// <summary>Deserialise data for the substitution from an xml node</summary>
		public override void FromXml(XElement node)
		{
			// ReSharper disable PossibleNullReferenceException
			base.FromXml(node);
			var codes = node.Element(XmlTag.CodeValues);
			m_values.Clear();
			foreach (var code in codes.Elements(XmlTag.CodeValue))
			{
				string c = code.Element(XmlTag.Code ).Value;
				string v = code.Element(XmlTag.Value).Value;
				m_values.Add(c, v);
			}
			// ReSharper restore PossibleNullReferenceException
		}
	}
}
