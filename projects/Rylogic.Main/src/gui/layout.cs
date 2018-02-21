using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Rylogic.Gui
{
	/// <summary>A collection of types for perform android-style GUI layout</summary>
	public static class Layout
	{
		// Notes:
		//  American spellings are used for convenience of converting strings to enums

		/// <summary>Element alignment flags</summary>
		[Flags] public enum EAlign
		{
			Left    = 1 << 0,
			CenterH = 1 << 1,
			Right   = 1 << 2,
			Top     = 1 << 3,
			CenterV = 1 << 4,
			Bottom  = 1 << 5,
			Centre = CenterH | CenterV,
		}

		/// <summary>Element auto size values</summary>
		public static class ESize
		{
			public const int Fill = -1;
			public const int Auto = -2;
		}

		/// <summary>Element description attributes</summary>
		public static class Tag
		{
			public const string Id            = "id";
			public const string Text          = "text";
			public const string Value         = "value";
			public const string Align         = "align";
			public const string AlignText     = "align-text";
			public const string AlignContent  = "align-content";
			public const string FontSize      = "font-size";
			public const string FontStyle     = "font-style";
			public const string Border        = "border";
			public const string Size          = "size";
			public const string Visible       = "visible";
			public const string Enabled       = "enabled";
			public const string Margin        = "margin";
			public const string Padding       = "padding";
			public const string BkColor       = "bkcolor";
			public const string FrColor       = "frcolor";
		}

		/// <summary>Base class for an element in the UI</summary>
		public class Elem :IDisposable, INotifyPropertyChanged
		{
			public Elem(IElemDescription desc)
			{
				Desc  = desc;
				RelTo = new string[2]{null,null};
				Side  = new int[2]{0,0};

				// The id
				Id = Desc.Attr(Tag.Id, string.Empty).ToLowerInvariant();

				// Get the optional value corresponding to the element
				Value = Desc.Attr(Tag.Value, null);

				// The field size
				var size = Desc.Attr(Tag.Size, "auto,auto").ToLowerInvariant().Split(',');
				Size = new Size(
					size[0] == "fill" ? ESize.Fill : size[0] == "auto" ? ESize.Auto : int.Parse(size[0]),
					size[1] == "fill" ? ESize.Fill : size[1] == "auto" ? ESize.Auto : int.Parse(size[1]));

				// The element position
				var hor = "left";
				var ver = "top";
				{
					Match m;
					var align = Desc.Attr(Tag.Align, "left|top").ToLowerInvariant();
					if ((m = Regex.Match(align, @"(.*)\|(.*)")).Success) // match e.g. "left[...]|top[...]"
					{
						foreach (var g in new[]{1,2})
						{
							if      (Regex.IsMatch(m.Groups[g].Value, @"left|right|centerh")) hor = m.Groups[g].Value;
							else if (Regex.IsMatch(m.Groups[g].Value, @"top|bottom|centerv")) ver = m.Groups[g].Value;
						}
					}
					else if ((m = Regex.Match(align, @"center(:.*)?")).Success) // match "centre[...]"
					{
						hor = "centerh" + m.Groups[1].Value;
						ver = "centerv" + m.Groups[1].Value;
					}
				}

				// Interpret the 'relative-to' field
				for (int i = 0; i != 2; ++i)
				{
					Match m;
					var ori = i == 0 ? hor : ver;
					if ((m = Regex.Match(ori, @"(\w+)(:([+-=])?(.*)?)?")).Success) // match e.g. left:+foo, top:-, right:bar
					{
						var align = Enum<EAlign>.Parse(m.Groups[1].Value, ignore_case:true);
						if (m.Groups[2].Value.HasValue()) // the relative-to part is given
						{
							Align |= align;
							RelTo[i] = m.Groups[4].Value;
							Side[i] =
								m.Groups[3].Value == "+" ? +1 :
								m.Groups[3].Value == "-" ? -1 :
								m.Groups[3].Value == "=" ?  0 :
								0;
						}
						else
						{
							Align |= align;
							Side[i] =
								align == EAlign.Left  || align == EAlign.Top    ? +1 :
								align == EAlign.Right || align == EAlign.Bottom ? -1 :
								0;
						}
					}
				}
			}
			public virtual void Dispose()
			{
				Ctrl = Util.Dispose(Ctrl);
			}
			public override string ToString()
			{
 				return $"{GetType().Name} (id:{Id})";
			}

			/// <summary>Implicit conversion to WinForms control</summary>
			public static implicit operator Control(Elem elem)
			{
				return elem.Ctrl;
			}

			/// <summary>The layout description for this element</summary>
			public IElemDescription Desc { get; private set; }

			/// <summary>The string id for the element</summary>
			public string Id { get; private set; }

			/// <summary>The WinForms control for the element</summary>
			public Control Ctrl { get; protected set; }

			/// <summary>Occurs when the 'Value' property in this element changes</summary>
			public event PropertyChangedEventHandler PropertyChanged;

			/// <summary>Return the value of this field</summary>
			public virtual object Value
			{
				get { return m_value; }
				set
				{
					if (m_value == value) return;
					m_value = value;
					PropertyChanged.Raise(this, new PropertyChangedEventArgs(nameof(Value)));
				}
			}
			private object m_value;

			/// <summary>The size of the control for auto sizing</summary>
			public Size Size { get; set; }

			/// <summary>Positioning alignment for the element</summary>
			public EAlign Align { get; set; }

			/// <summary>The id of an element to align relative to</summary>
			public string[] RelTo { get; private set; }

			/// <summary>The side of the 'relative to' element to align to</summary>
			public int[] Side { get; private set; }

			/// <summary>Layout this element within the given parent rectangle</summary>
			public void Layout(Rectangle parent_rect, IEnumerable<Elem> elems)
			{
				Ctrl.Size     = GetSize(parent_rect);
				Ctrl.Location = GetLocation(parent_rect, elems);
			}

			/// <summary>Return the location for this element within 'parent_rect'</summary>
			public Point GetLocation(Rectangle parent_rect, IEnumerable<Elem> elems)
			{
				// Find the rectangle to position the element relative to
				var hor = parent_rect;
				var ver = parent_rect;
				if (RelTo[0].HasValue())
				{
					var elem = elems.FirstOrDefault(e => e.Id == RelTo[0]);
					if (elem != null)
					{
						var m = elem.Ctrl.Margin;
						hor = elem.Ctrl.Bounds.Inflated(m.Left, m.Top, m.Right, m.Bottom);
					}
				}
				if (RelTo[1].HasValue())
				{
					var elem = elems.FirstOrDefault(e => e.Id == RelTo[1]);
					if (elem != null)
					{
						var m = elem.Ctrl.Margin;
						ver = elem.Ctrl.Bounds.Inflated(m.Left, m.Top, m.Right, m.Bottom);
					}
				}

				// The reference point on the 'relative-to' rectangle
				var x = Side[0] == +1 ? hor.Left : Side[0] == -1 ? hor.Right  : hor.Centre().X;
				var y = Side[1] == +1 ? ver.Top  : Side[1] == -1 ? ver.Bottom : ver.Centre().Y;
				
				var pt = new Point();
				var w = Ctrl.Margin.Left + Ctrl.Width  + Ctrl.Margin.Right;
				var h = Ctrl.Margin.Top  + Ctrl.Height + Ctrl.Margin.Bottom;
				if (Align.HasFlag(EAlign.Left   )) pt.X = x - 0   + Ctrl.Margin.Left;
				if (Align.HasFlag(EAlign.CenterH)) pt.X = x - w/2 + Ctrl.Margin.Left;
				if (Align.HasFlag(EAlign.Right  )) pt.X = x - w   + Ctrl.Margin.Left;
				if (Align.HasFlag(EAlign.Top    )) pt.Y = y - 0   + Ctrl.Margin.Top;
				if (Align.HasFlag(EAlign.CenterV)) pt.Y = y - h/2 + Ctrl.Margin.Top;
				if (Align.HasFlag(EAlign.Bottom )) pt.Y = y - h   + Ctrl.Margin.Top;
				return pt;
			}

			/// <summary>Return the size for this field within 'parent_rect'</summary>
			public Size GetSize(Rectangle parent_rect)
			{
				// Reduce the parent_rect size by the size of this control's margin
				var margin = Ctrl.Margin;
				parent_rect = parent_rect.Inflated(-margin.Left, -margin.Top, -margin.Right, -margin.Bottom).NormalizeRect();

				var sz = Size;
				if (sz.Width  == ESize.Fill) sz.Width  = parent_rect.Width;
				if (sz.Height == ESize.Fill) sz.Height = parent_rect.Height;
				if      (sz.Width  == ESize.Auto && sz.Height == ESize.Auto) sz = Ctrl.GetPreferredSize(parent_rect.Size);
				else if (sz.Width  == ESize.Auto) sz.Width  = Ctrl.GetPreferredSize(new Size(parent_rect.Width, sz.Height)).Width;
				else if (sz.Height == ESize.Auto) sz.Height = Ctrl.GetPreferredSize(new Size(sz.Width, parent_rect.Height)).Height;
				return sz;
			}

			/// <summary>Create the control for the field</summary>
			protected T CreateControl<T>() where T:Control, new()
			{
				var ctrl = new T();
				ctrl.AutoSize  = false;
				ctrl.Text      = Text(Desc, string.Empty);
				ctrl.Font      = new Font(FontFamily.GenericSansSerif, FSize(Desc, 12f), FStyle(Desc, FontStyle.Regular));
				ctrl.Anchor    = AnchorStyles.None;
				ctrl.BackColor = BkColor(Desc, ctrl.BackColor);
				ctrl.ForeColor = FrColor(Desc, ctrl.ForeColor);
				ctrl.Margin    = Margin(Desc, System.Windows.Forms.Padding.Empty);
				ctrl.Padding   = Padding(Desc, System.Windows.Forms.Padding.Empty);
				ctrl.Visible   = Visible(Desc, ctrl.Visible);
				ctrl.Enabled   = Enabled(Desc, ctrl.Enabled);

				Ctrl = ctrl;
				return ctrl;
			}

			/// <summary>Hook up dependencies between the UI elements</summary>
			public void SetupDependenices(IEnumerable<Elem> elems)
			{
				// The enabled attribute, should be a boolean expression
				var enabled = Desc.Attr(Tag.Enabled, null);
				if (enabled != null)
				{
				//	// e.g. [id1] <= [id2]
				//	System.Linq.Dynamic.DynamicExpression.Parse
				//	if (enabled.Repl
				////	System.Linq.Expressions.Expression.
				}
				// For each attribute
				// Does it have [id] tags in it?
				// Yes, create a dependency that:
				//	watches PropertyChanged on the referenced elements
				//  updates the appropriate property on this elem based on the result of the expression
			}

			#region Methods used by subclassed elements

			// Note:
			//  Attributes do not throw on format errors because a higher level
			//  may want to change the meaning of a particular attribute.

			/// <summary>Convert an auto size to a string</summary>
			protected static string SizeStr(Size sz)
			{
				return
					$"{(sz.Width  == ESize.Auto ? "auto" : sz.Width  == ESize.Fill ? "fill" : sz.Width.ToString())},"+
					$"{(sz.Height == ESize.Auto ? "auto" : sz.Height == ESize.Fill ? "fill" : sz.Height.ToString())}";
			}

			/// <summary>Convert a colour to a string</summary>
			protected static string ColorStr(Color col)
			{
				return col.IsKnownColor ? col.ToKnownColor().ToString() : $"#{col.ToArgb()}";
			}

			/// <summary>Convert a padding object to a string</summary>
			protected static string PaddingStr(Padding pad)
			{
				if (pad.Left == pad.Top && pad.Left == pad.Right && pad.Left == pad.Bottom) return $"{pad.Left}";
				if (pad.Left == pad.Right && pad.Top == pad.Bottom) return $"{pad.Left},{pad.Top}";
				return $"{pad.Left},{pad.Top},{pad.Right},{pad.Bottom}";
			}

			/// <summary>Read the visibility</summary>
			protected static bool Visible(IElemDescription desc, bool def)
			{
				bool visible;
				var s = desc.Attr(Tag.Visible, def.ToString());
				return bool.TryParse(s, out visible) ? visible : def;
			}

			/// <summary>Read the 'enabledness'</summary>
			protected static bool Enabled(IElemDescription desc, bool def)
			{
				bool enabled;
				var s = desc.Attr(Tag.Enabled, def.ToString());
				return bool.TryParse(s, out enabled) ? enabled : def;
			}

			/// <summary>Read a colour</summary>
			protected static Color Color(IElemDescription desc, string tag, Color def)
			{
				var col = desc.Attr(tag, ColorStr(def));
				if (col[0] == '#')
				{
					int argb;
					return int.TryParse(col.Substring(1), NumberStyles.HexNumber, null, out argb)
						? System.Drawing.Color.FromArgb(argb) : def;
				}
				else
				{
					var kc = Enum<KnownColor>.TryParse(col, true);
					return kc != null ? System.Drawing.Color.FromKnownColor(kc.Value) : def;
				}
			}

			/// <summary>Read the background colour</summary>
			protected static Color BkColor(IElemDescription desc, Color def)
			{
				return Color(desc, Tag.BkColor, def);
			}

			/// <summary>Read the foreground colour</summary>
			protected static Color FrColor(IElemDescription desc, Color def)
			{
				return Color(desc, Tag.FrColor, def);
			}

			/// <summary>Read the label</summary>
			protected static string Text(IElemDescription desc, string def)
			{
				return desc.Attr(Tag.Text, def);
			}

			/// <summary>Read the font size</summary>
			protected static float FSize(IElemDescription desc, float def)
			{
				float fsize;
				var s = desc.Attr(Tag.FontSize, def.ToString());
				return float.TryParse(s, out fsize) ? fsize : def;
			}

			/// <summary>Read the font style</summary>
			protected static FontStyle FStyle(IElemDescription desc, FontStyle def)
			{
				var s = desc.Attr(Tag.FontStyle, def.ToString());
				return Enum<FontStyle>.TryParse(s, true) ?? def;
			}

			/// <summary>Read a padding rectangle</summary>
			protected static Padding Padding(IElemDescription desc, string tag, Padding def)
			{
				var pad = new int[4];
				var s = desc.Attr(tag, "0").Split(',');
				for (int i = 0; i != s.Length; ++i)
				{
					if (int.TryParse(s[i], out pad[i])) continue;
					return def;
				}
				switch (s.Length)
				{
				case 4: return new Padding(pad[0], pad[1], pad[2], pad[3]);
				case 2: return new Padding(pad[0], pad[1], pad[0], pad[1]);
				case 1: return new Padding(pad[0], pad[0], pad[0], pad[0]);
				default: throw new Exception($"{tag} attribute has the wrong number of values");
				}
			}

			/// <summary>Field margin, controls the boundary gap around the control</summary>
			protected static Padding Margin(IElemDescription desc, Padding def)
			{
				return Padding(desc, Tag.Margin, def);
			}

			/// <summary>Field padding, controls the boundary gap within the control</summary>
			protected static Padding Padding(IElemDescription desc, Padding def)
			{
				return Padding(desc, Tag.Padding, def);
			}

			/// <summary>Border style</summary>
			protected static BorderStyle Border(IElemDescription desc, BorderStyle def)
			{
				var s = desc.Attr(Tag.Border, def.ToString());
				return Enum<BorderStyle>.TryParse(s, true) ?? def;
			}

			/// <summary>Text alignment</summary>
			protected static HorizontalAlignment TextAlign(IElemDescription desc, HorizontalAlignment def)
			{
				var s = desc.Attr(Tag.AlignText, def.ToString());
				return Enum<HorizontalAlignment>.TryParse(s, true) ?? def;
			}

			/// <summary>Content alignment</summary>
			protected static ContentAlignment ContentAlign(IElemDescription desc, ContentAlignment def)
			{
				var s = desc.Attr(Tag.AlignContent, def.ToString());
				return Enum<ContentAlignment>.TryParse(s, true) ?? def;
			}

			#endregion
		}

		/// <summary>A label</summary>
		public class Lbl :Elem
		{
			public static Lbl Create(string id = null, string text = null, string align = null, Size? size = null, Color? bkcolor = null, float? font_size = null, FontStyle? font_style = null, Padding? padding = null, Padding? margin = null)
			{
				var dic = new DictElemDescription();
				if (id         != null) dic.Add("id"        , id);
				if (text       != null) dic.Add("text"      , text);
				if (align      != null) dic.Add("align"     , align);
				if (size       != null) dic.Add("size"      , SizeStr(size.Value));
				if (bkcolor    != null) dic.Add("bkcolor"   , ColorStr(bkcolor.Value));
				if (font_size  != null) dic.Add("font-size" , font_size.Value.ToString());
				if (font_style != null) dic.Add("font-style", font_style.Value.ToString());
				if (padding    != null) dic.Add("padding"   , PaddingStr(padding.Value));
				if (margin     != null) dic.Add("margin"    , PaddingStr(margin.Value));
				return new Lbl(dic);
			}
			public Lbl(IElemDescription desc) :base(desc)
			{
				// Create the UI element for the label
				var ctrl = CreateControl<Label>();
				ctrl.TextAlign   = ContentAlign(desc, ctrl.TextAlign);
				ctrl.BorderStyle = Border(desc, BorderStyle.None);
			}
		}

		/// <summary>A text box</summary>
		public class TBox :Elem
		{
			public TBox(IElemDescription desc) :base(desc)
			{
				// Create the text box
				var ctrl = CreateControl<TextBox>();
				ctrl.BorderStyle = Border(desc, BorderStyle.None);
				ctrl.Multiline   = true;
				ctrl.TextAlign   = TextAlign(desc, ctrl.TextAlign);
			}
		}

		/// <summary>A button</summary>
		public class Btn :Elem
		{
			public Btn(IElemDescription desc) :base(desc)
			{
				// Create the UI element
				var ctrl = CreateControl<Button>();
				ctrl.TextAlign = ContentAlign(desc, ctrl.TextAlign);
			}
		}

		/// <summary>An interface to the source that describes the gui layout for an element</summary>
		public abstract class IElemDescription
		{
			/// <summary>Return the attribute with name 'name' or 'def' if not given</summary>
			public abstract string Attr(string name, string def);

			/// <summary>Implicit conversion from string[] to IElemDescription</summary>
			public static implicit operator IElemDescription(string[] config)
			{
				return new DictElemDescription(config);
			}

			/// <summary>Implicit conversion from XElement to IElemDescription</summary>
			public static implicit operator IElemDescription(XElement elem)
			{
				return new XmlElemDescription(elem);
			}
		}

		/// <summary>An implementation of IElemDescription using a dictionary</summary>
		public class DictElemDescription :IElemDescription
		{
			private readonly Dictionary<string,string> m_dic;
			public DictElemDescription(Dictionary<string,string> dic = null)
			{
				m_dic = dic ?? new Dictionary<string,string>();
			}
			public DictElemDescription(params string[] config) :this()
			{
				foreach (var c in config)
				{
					var m = Regex.Match(c, @"(\w+)=['""](.*)['""]");
					if (!m.Success) throw new Exception("Layout UI element description syntax error");
					m_dic.Add(m.Groups[1].Value, m.Groups[2].Value);
				}
			}
			public void Add(string key, string value)
			{
				m_dic.Add(key, value);
			}

			/// <summary>Return the attribute with name 'name' or 'def' if not given</summary>
			public override string Attr(string name, string def)
			{
				string value;
				return m_dic.TryGetValue(name, out value) ? value : def;
			}
		}

		/// <summary>An implementation of IElemDescription using XElement</summary>
		public class XmlElemDescription :IElemDescription
		{
			private readonly XElement m_elem;
			public XmlElemDescription(XElement elem)
			{
				m_elem = elem;
			}

			/// <summary>Return the attribute with name 'name' or 'def' if not given</summary>
			public override string Attr(string name, string def)
			{
				return m_elem.AttrValue(name, def);
			}
		}
	}
}
