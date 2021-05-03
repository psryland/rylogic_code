using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Xml.Linq;
using Rylogic.Container;
using Rylogic.Extn;
using Rylogic.Gui.WPF;
using Rylogic.Maths;

namespace Rylogic.Gui.WPF.ChartDiagram
{
	/// <summary>Base class for all diagram node types</summary>
	[DebuggerDisplay("{Description,nq}")]
	public abstract class Node :ChartControl.Element, IHasStyle
	{
		// Notes:
		//  - A node is a chart element set up for connecting to other node elements via connectors.
		//  - 'Node' has support for text and size, but does not imply a particular shape (2d or 3d).

		/// <summary>Base node constructor</summary>
		/// <param name="id">Globally unique id for the element</param>
		/// <param name="size">The initial dimensions of the node</param>
		/// <param name="text">The text of the node</param>
		/// <param name="position">The position of the node on the diagram</param>
		/// <param name="style">Style properties for the node</param>
		protected Node(v4 size, Guid id, string text, m4x4? position = null, NodeStyle? style = null)
			: base(id, text, position)
		{
			Text = text;
			Style = style ?? new NodeStyle();
			SizeMax = v4.MaxValue;
			SizeMin = v4.Zero;
			Size = size;
			Connectors = new BindingListEx<Connector>();
			TextFormat = new StringFormat(0);
		}
		protected Node(XElement node)
			: base(node)
		{
			Text = node.Element(XmlTag.Text).As(Text);
			Style = new NodeStyle(node.Element(XmlTag.Style).As<Guid>());
			SizeMax = node.Element(XmlTag.SizeMax).As(SizeMax);
			Size = node.Element(XmlTag.Size).As(Size);
			Connectors = new BindingListEx<Connector>();
			TextFormat = new StringFormat(0);

			// Be careful using Style in here, it's only a place-holder
			// instance until the element has been added to a diagram.
		}
		protected override void Dispose(bool disposing)
		{
			DetachConnectors();
			Style = null!;
			base.Dispose(disposing);
		}

		/// <summary>Export to XML</summary>
		public override XElement ToXml(XElement node)
		{
			base.ToXml(node);
			node.Add2(XmlTag.Text, Text, false);
			node.Add2(XmlTag.Style, Style.Id, false);
			node.Add2(XmlTag.SizeMax, SizeMax, false);
			node.Add2(XmlTag.Size, Size, false);
			return node;
		}
		protected override void FromXml(XElement node)
		{
			Style = new NodeStyle(node.Element(XmlTag.Style).As(Style.Id));
			Text = node.Element(XmlTag.Text).As(Text);
			SizeMax = node.Element(XmlTag.SizeMax).As(SizeMax);
			Size = node.Element(XmlTag.Size).As(Size);
			base.FromXml(node);
		}

		/// <summary>The connectors linked to this node</summary>
		public BindingListEx<Connector> Connectors { get; }

		/// <summary>Text to display in this node</summary>
		public virtual string Text
		{
			get => m_text;
			set
			{
				if (Text == value) return;
				m_text = value;

				if (Style.AutoSize) PerformAutoSize();
				NotifyDataChanged();
				Invalidate();
			}
		}
		private string m_text = string.Empty;

		/// <summary>How to layout the text in the node</summary>
		public StringFormat TextFormat
		{
			get => m_text_fmt;
			set
			{
				if (m_text_fmt == value) return;
				m_text_fmt = value;

				if (Style.AutoSize) PerformAutoSize();
				Invalidate();
			}
		}
		private StringFormat m_text_fmt = null!;

		/// <summary>The diagram space width/height/depth of the element</summary>
		public v4 Size
		{
			get => m_size;
			set
			{
				// Clamp 'value' to the size limits
				value = Math_.Clamp(value, SizeMin, SizeMax);
				if (Size == value)
					return;

				m_size = value;
				NotifySizeChanged();
				NotifyPropertyChanged(nameof(Size));
				Invalidate();
			}
		}
		private v4 m_size;

		/// <summary>Get/Set minimum size limit for auto size</summary>
		public v4 SizeMin
		{
			get => m_size_min;
			set
			{
				if (SizeMin == value) return;
				m_size_min = value;
				SizeMax = Math_.Max(value, SizeMax);
				Size = Size;
				NotifyPropertyChanged(nameof(SizeMin));
			}
		}
		private v4 m_size_min;

		/// <summary>Get/Set maximum size limit for auto size</summary>
		public v4 SizeMax
		{
			get => m_size_max;
			set
			{
				if (SizeMax == value) return;
				m_size_max = value;
				SizeMin = Math_.Min(value, SizeMin);
				Size = Size;
				NotifyPropertyChanged(nameof(SizeMax));
			}
		}
		private v4 m_size_max;

		/// <summary>Resize the node if AutoSize is enabled, otherwise no-op</summary>
		public void PerformAutoSize()
		{
		//	base.Size = PreferredSize(SizeMax);
		}

		/// <summary>Return the preferred node size given the current text and size limits</summary>
		public virtual v4 PreferredSize(v4 layout)
		{
			if (!Text.HasValue())
				return Size;

			//if (layout.x == 0f)
			//	layout.x = float.MaxValue;
			//if (layout.y == 0f)
			//	layout.y = float.MaxValue;
			//if (layout.z == 0f)
			//	layout.z = float.MaxValue;

			using var img = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
			using var gfx = Graphics.FromImage(img);
			v2 sz = gfx.MeasureString(Text, Style.Font, layout.xy.ToSizeF(), TextFormat);
			sz.x += Style.Padding.Left + Style.Padding.Right;
			sz.y += Style.Padding.Top + Style.Padding.Bottom;
			return new v4(sz, 0, 0);
		}
		public v4 PreferredSize(double max_width, double max_height)
		{
			return PreferredSize(new v4((float)max_width, (float)max_height, 0f, 0f));
		}
		public v4 PreferredSize()
		{
			return PreferredSize(SizeMax);
		}

		/// <summary>Style attributes for the node</summary>
		public NodeStyle Style
		{
			get => m_style;
			set
			{
				if (Style == value) return;
				if (m_style != null)
				{
					m_style.PropertyChanged -= HandleStyleChanged;
				}
				m_style = value ?? new NodeStyle();
				if (m_style != null)
				{
					m_style.PropertyChanged += HandleStyleChanged;
				}
				HandleStyleChanged(null, new PropertyChangedEventArgs(string.Empty));

				// Handlers
				void HandleStyleChanged(object? sender, PropertyChangedEventArgs e)
				{
					// Reassign the size to cause an auto size (if necessary)
					Size = Size;
					Invalidate();
				}
			}
		}
		IStyle IHasStyle.Style => Style;
		private NodeStyle m_style = new NodeStyle();

		/// <inheritdoc/>
		public override BBox Bounds => new BBox(Position.pos, Size / 2);

		/// <summary>Returns the position to draw text given the current size and text alignment</summary>
		protected RectangleF TextLocation(Graphics gfx)
		{
			var pad = Style.Padding;
			var bnd = Size.xy.ToRectangleF().Inflated(-pad.Left, -pad.Top, -pad.Right, -pad.Bottom);
			var tx = gfx.MeasureString(Text, Style.Font, bnd.Size, TextFormat);
			return Style.TextAlign switch
			{
				ContentAlignment.TopLeft      => new RectangleF(bnd.Left, bnd.Top, tx.Width, tx.Height),
				ContentAlignment.TopCenter    => new RectangleF(bnd.Left + (bnd.Width - tx.Width) * 0.5f, bnd.Top, tx.Width, tx.Height),
				ContentAlignment.TopRight     => new RectangleF(bnd.Left + (bnd.Width - tx.Width), bnd.Top, tx.Width, tx.Height),
				ContentAlignment.MiddleLeft   => new RectangleF(bnd.Left, bnd.Top + (bnd.Height - tx.Height) * 0.5f, tx.Width, tx.Height),
				ContentAlignment.MiddleCenter => new RectangleF(bnd.Left + (bnd.Width - tx.Width) * 0.5f, bnd.Top + (bnd.Height - tx.Height) * 0.5f, tx.Width, tx.Height),
				ContentAlignment.MiddleRight  => new RectangleF(bnd.Left + (bnd.Width - tx.Width), bnd.Top + (bnd.Height - tx.Height) * 0.5f, tx.Width, tx.Height),
				ContentAlignment.BottomLeft   => new RectangleF(bnd.Left, bnd.Top + (bnd.Height - tx.Height), tx.Width, tx.Height),
				ContentAlignment.BottomCenter => new RectangleF(bnd.Left + (bnd.Width - tx.Width) * 0.5f, bnd.Top + (bnd.Height - tx.Height), tx.Width, tx.Height),
				ContentAlignment.BottomRight  => new RectangleF(bnd.Left + (bnd.Width - tx.Width), bnd.Top + (bnd.Height - tx.Height), tx.Width, tx.Height),
				_                             => throw new ArgumentException("unknown text alignment"),
			};
		}

		/// <summary>Remove this node from the connectors that reference it</summary>
		public void DetachConnectors()
		{
			while (!Connectors.Empty())
				Connectors[0].Remove(this);
		}

		/// <summary>Try to untangle the connectors to this node. True if connectors where tangled</summary>
		public bool Untangle()
		{
			var tangled = false;

			// For each connector pair, see if they cross
			for (int i = 0; i != Connectors.Count; ++i)
			{
				for (int j = i + 1; j != Connectors.Count; ++j)
				{
					var c0 = Connectors[i];
					var c1 = Connectors[j];

					var c0a0 = c0.Anc(this);
					var c1a0 = c1.Anc(this);
					var c0a1 = c0.OtherAnc(this);
					var c1a1 = c1.OtherAnc(this);
					if (c0a0 == null) continue;
					if (c1a0 == null) continue;
					if (c0a1 == null) continue;
					if (c1a1 == null) continue;

					var c0_s = c0a0.LocationDS.xy;
					var c1_s = c1a0.LocationDS.xy;
					var c0_e = c0a1.LocationDS.xy;
					var c1_e = c1a1.LocationDS.xy;

					// See if the line segments cross
					Geometry.ClosestPoint(c0_s, c0_e, c1_s, c1_e, out var t0, out var t1);
					if (t0 > 0 && t0 < 1 && t1 > 0 && t1 < 1)
					{
						// Swap anchor points on this node
						var loc = c0a0.Location;
						c0a0.Location = c1a0.Location;
						c1a0.Location = loc;

						var norm = c0a0.Normal;
						c0a0.Normal = c1a0.Normal;
						c1a0.Normal = norm;

						c0.Invalidate();
						c1.Invalidate();
						tangled = true;
					}
				}
			}
			return tangled;
		}

		/// <summary>Return all the locations that connectors can attach to this node (in node space)</summary>
		public virtual IEnumerable<AnchorPoint> AnchorPoints()
		{
			yield return new AnchorPoint(this, v4.Origin, v4.Zero);
		}

		/// <summary>Return the attachment location and normal nearest to 'pt'.</summary>
		public virtual AnchorPoint NearestAnchor(v4 pt, bool pt_in_node_space)
		{
			if (!pt_in_node_space)
				pt = Math_.InvertFast(Position) * pt;

			return AnchorPoints().MinBy(x => (x.Location - pt).LengthSq);
		}

		/// <inheritdoc/>
		public override bool CheckConsistency()
		{
			// The Connectors collection in the node should contain a connector that references this node exactly once
			foreach (var conn in Connectors)
			{
				if (conn.Node0 != this && conn.Node1 != this)
					throw new Exception($"Node {ToString()} contains connector {conn} but is not referenced by the connector");

				// Allow the connectors to be connected to this node at both ends
				//if (conn.Node0 == this && conn.Node1 == this)
				//	throw new Exception($"Node {ToString()} contains connector { conn.ToString(} that is attached to it at both ends"));
			}

			//// The style should be known to the diagram
			//if (Diagram != null)
			//{
			//	if (!Diagram.m_node_styles.ContainsKey(Style.Id))
			//		throw new Exception($"Node {ToString()} style is not in the diagram's style cache");
			//}
			return base.CheckConsistency();
		}

		/// <summary></summary>
		public string Description => $"Node[{Text.Summary(20)}]";
	}
}