using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Windows.Forms;
using System.Xml.Linq;
using pr.extn;

namespace RyLogViewer
{
	/// <summary>A substitution that swaps code values for text values</summary>
	[DataContract] 
	public class SubCodeLookup :TransformSubstitutionBase
	{
		[DataMember] private readonly Dictionary<string, string> m_values = new Dictionary<string, string>(); // The code lookup table

		/// <summary>
		/// A unique id for this text transform, used to associate
		/// saved configuration data with this transformation.</summary>
		public override Guid Guid { get { return new Guid("47BF0EAE-35FE-49E6-8E5B-3927BD9B07C3"); } }

		/// <summary>
		/// The name that appears in the transform column dropdown
		/// for this text transformation.</summary>
		public override string DropDownName { get { return "Code Lookup"; } }

		/// <summary>True if this substitution can be configured</summary>
		public override bool Configurable { get { return true; } }

		/// <summary>A summary of the configuration for this transform substitution</summary>
		public override string ConfigSummary { get { return m_values.Count != 0 ? "{0} lookup codes".Fmt(m_values.Count) : null; } }

		/// <summary>
		/// Called when a user selects to edit the configuration for this transform.
		/// Implementers should display a modal dialog that collects any necessary data
		/// for the text transform.</summary>
		public override void ShowConfigUI(Form main_window)
		{
			var dg = new CodeLookupUI(m_values);
			if (dg.ShowDialog(main_window) != DialogResult.OK)
				return;

			m_values.Clear();
			foreach (var v in dg.Values)
				m_values.Add(v.Key, v.Value);
		}

		/// <summary>
		/// Returns the result of applying this text transform to 'captured_text'.
		/// This method provides the functionality of the text transform and should
		/// be efficiently implemented.</summary>
		public override string Result(string elem)
		{
			string result;
			return m_values.TryGetValue(elem, out result) ? result : elem;
		}

		/// <summary>
		/// Save data for the transform to the provided xml node.
		/// This is used to persist per-instance settings for this text
		/// transform within the main RyLogViewer settings xml file.
		/// Implementers should add xml nodes to 'data_root'</summary>
		public override void ToXml(XElement node)
		{
			var codes = new XElement(XmlTag.CodeValues);
			foreach (var v in m_values)
			{
				var value = new XElement(XmlTag.CodeValue,
					new XElement(XmlTag.Code , v.Key),
					new XElement(XmlTag.Value, v.Value)
					);
				codes.Add(value);
			}
			node.Add(codes);
		}

		/// <summary>
		/// Load instance data for this transform from 'data_root'.
		/// This method should be the symmetric opposite of 'ToXml()'</summary>
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
