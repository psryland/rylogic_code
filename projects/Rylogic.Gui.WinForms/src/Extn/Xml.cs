//***************************************************
// Xml Helper Functions
//  Copyright (c) Rylogic Ltd 2010
//***************************************************

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Rylogic.Extn
{
	// Object hierarchy:
	//   XObject
	//   +- XAttribute
	//   +- XNode
	//      +- XComment
	//      +- XContainer
	//      |  +- XDocument
	//      |  +- XElement
	//      +- XDocumentType
	//      +- XProcessingInstruction
	//      +- XText
	//         +- XCData

	/// <summary>XML helper methods</summary>
	public static class XmlWinFormsExtensions
	{
		public static XmlConfig SupportWinFormsTypes(this XmlConfig cfg)
		{
			Xml_.ToMap[typeof(Padding)] = (obj, node) =>
			{
				var p = (Padding)obj;
				node.SetValue($"{p.Left} {p.Top} {p.Right} {p.Bottom}");
				return node;
			};
			Xml_.AsMap[typeof(Padding)] = (elem, type, ctor) =>
			{
				var ltrb = elem.Value.Split(Xml_.WhiteSpace, StringSplitOptions.RemoveEmptyEntries);
				return new Padding(int.Parse(ltrb[0]), int.Parse(ltrb[1]), int.Parse(ltrb[2]), int.Parse(ltrb[3]));
			};

			Xml_.ToMap[typeof(Font)] = (obj, node) =>
			{
				var font = (Font)obj;
				node.Add(
					font.FontFamily.Name.ToXml(nameof(Font.FontFamily), false),
					font.Size.ToXml(nameof(Font.Size), false),
					font.Style.ToXml(nameof(Font.Style), false),
					font.Unit.ToXml(nameof(Font.Unit), false),
					font.GdiCharSet.ToXml(nameof(Font.GdiCharSet), false),
					font.GdiVerticalFont.ToXml(nameof(Font.GdiVerticalFont), false));
				return node;
			};
			Xml_.AsMap[typeof(Font)] = (elem, type, instance) =>
			{
				var font_family       = elem.Element(nameof(Font.FontFamily     )).As<string>();
				var size              = elem.Element(nameof(Font.Size           )).As<float>();
				var style             = elem.Element(nameof(Font.Style          )).As<FontStyle>();
				var unit              = elem.Element(nameof(Font.Unit           )).As<GraphicsUnit>();
				var gdi_charset       = elem.Element(nameof(Font.GdiCharSet     )).As<byte>();
				var gdi_vertical_font = elem.Element(nameof(Font.GdiVerticalFont)).As<bool>();
				return new Font(font_family, size, style, unit, gdi_charset, gdi_vertical_font);
			};

			Xml_.ToMap[typeof(Blend)] = (obj, node) =>
			{
				var blend = (Blend)obj;
				node.Add2(nameof(Blend.Factors), string.Join(",", blend.Factors), false);
				node.Add2(nameof(Blend.Positions), string.Join(",", blend.Positions), false);
				return node;
			};
			Xml_.AsMap[typeof(Blend)] = (elem, type, instance) =>
			{
				var factors   = float_.ParseArray(elem.Element(nameof(Blend.Factors  )).As<string>());
				var positions = float_.ParseArray(elem.Element(nameof(Blend.Positions)).As<string>());
				return new Blend(factors.Length)
				{
					Factors   = factors,
					Positions = positions,
				};
			};

			Xml_.ToMap[typeof(Matrix)] = (obj, node) =>
			{
				var mat = (Matrix)obj;
				node.Add(string.Join(" ", mat.Elements));
				return node;
			};
			Xml_.AsMap[typeof(Matrix)] = (elem, type, instance) =>
			{
				var v = float_.ParseArray(elem.Value);
				return new Matrix(v[0], v[1], v[2], v[3], v[4], v[5]);
			};

			return cfg;
		}
	}
}

