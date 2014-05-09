using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;
using pr.common;
using pr.container;
using pr.extn;
using pr.maths;
using pr.gfx;
using pr.ldr;
using pr.util;

using EBtn    = pr.gfx.View3d.EBtn;
using EBtnIdx = pr.gfx.View3d.EBtnIdx;

namespace pr.gui
{
	/// <summary>A control for drawing box and line diagrams</summary>
	public class DiagramControl :UserControl ,ISupportInitialize
	{
		/// <summary>
		/// The types of elements that can be on a diagram.
		/// Also defines the order that elements are written to xml</summary>
		public enum Entity
		{
			Node,
			Connector,
			Label,
		}

		#region Elements

		/// <summary>Base class for anything on a diagram</summary>
		public abstract class Element :IDisposable
		{
			protected Element(string ldr_desc, m4x4 position)
			{
				Id = Guid.NewGuid();
				Graphics = new View3d.Object(ldr_desc);
				Position = position;
			}
			protected Element(string ldr_desc, XElement node)
			{
				Graphics = new View3d.Object(ldr_desc);
				Id       = node.Element("id").As<Guid>();
				Position = node.Element("pos").As<m4x4>();
			}
			public virtual void Dispose()
			{
				Graphics = null;
			}

			/// <summary>Get the entity type for this element</summary>
			public abstract Entity Entity { get; }

			/// <summary>Unique id for this element</summary>
			public Guid Id { get; private set; }

			/// <summary>Export to xml</summary>
			public virtual XElement ToXml(XElement node)
			{
				node.Add2("id"  ,Id       ,false);
				node.Add2("pos" ,Position ,false);
				return node;
			}

			/// <summary>Indicate a refresh is needed</summary>
			public void Invalidate(object sender = null, EventArgs args = null)
			{
				if (Dirty) return;
				Dirty = true;
				Invalidated.Raise(this, EventArgs.Empty);
			}
			protected void AllowInvalidate() { m_allow_invalidate = true; Invalidate(); }
			private bool m_allow_invalidate = false;

			/// <summary>Dirty flag = need to refresh</summary>
			private bool Dirty { get { return m_impl_dirty; } set { m_impl_dirty = value && m_allow_invalidate; } }
			private bool m_impl_dirty;

			public event EventHandler Invalidated;

			/// <summary>Get/Set the selected state</summary>
			public virtual bool Selected
			{
				get { return m_impl_selected; }
				set
				{
					if (m_impl_selected == value) return;
					m_impl_selected = value;
					PositionAtSelectionChange = Position;
					SelectedChanged.Raise(this, EventArgs.Empty);
				}
			}
			private bool m_impl_selected;

			/// <summary>Raised whenever the element is selected or deselected</summary>
			public event EventHandler SelectedChanged;

			/// <summary>Redraw this node if dirty</summary>
			public void Refresh(bool force = false)
			{
				// Redraw the texture if dirty
				if (!Dirty && !force) return;

				if (m_impl_refreshing) return; // Protect against reentrancy
				using (Scope.Create(() => m_impl_refreshing = true, () => m_impl_refreshing = false))
					Render();

				Dirty = false;
			}
			private bool m_impl_refreshing;

			/// <summary>The element to diagram transform</summary>
			public m4x4 Position
			{
				get { return Graphics.O2P; }
				set { Graphics.O2P = value; PositionChanged.Raise(this, EventArgs.Empty); }
			}
			internal m4x4 PositionAtSelectionChange { get; set; }

			/// <summary>Raised whenever the node is moved</summary>
			public event EventHandler PositionChanged;

			/// <summary>The graphics object for this node</summary>
			public View3d.Object Graphics
			{
				get { Refresh(); return m_impl_obj; }
				protected set
				{
					if (m_impl_obj != null) m_impl_obj.Dispose();
					m_impl_obj = value;
				}
			}
			private View3d.Object m_impl_obj;

			/// <summary>AABB for the element in diagram space</summary>
			public abstract BRect Bounds { get; }

			/// <summary>Perform a hit test on this object. Returns null for no hit. 'point' is in diagram space</summary>
			public abstract HitTestResult.Hit HitTest(v2 point, View3d.CameraControls cam);

			/// <summary>Render the node into the surface</summary>
			public abstract void Render();
		}

		#endregion

		#region Nodes

		/// <summary>A base class for nodes</summary>
		public abstract class Node :Element
		{
			protected Node(string ldr_desc, uint width, uint height, string tex_obj_name, string text, m4x4 position, NodeStyle style)
				:base(ldr_desc, position)
			{
				Surf     = new View3d.Texture(width,height);
				Text     = text;
				Style    = style;
				Graphics.SetTexture(Surf, tex_obj_name);

				Style.StyleChanged += Invalidate;
			}
			protected Node(string ldr_desc, string tex_obj_name, XElement node)
				:base(ldr_desc, node)
			{
				var width  = node.Element("tex_sx").As<uint>();
				var height = node.Element("tex_sy").As<uint>();
				Text       = node.Element("text").As<string>();
				Style      = node.Element("style").As<NodeStyle>();
				Surf = new View3d.Texture(width, height);
				Graphics.SetTexture(Surf, tex_obj_name);
			}
			public override void Dispose()
			{
				Style.StyleChanged -= Invalidate;
				Surf = null;
				base.Dispose();
			}

			/// <summary>Get the entity type for this element</summary>
			public override Entity Entity { get { return Entity.Node; } }

			/// <summary>Export to xml</summary>
			public override XElement ToXml(XElement node)
			{
				node.Add2("tex_sx" ,Surf.Size.Width  ,false);
				node.Add2("tex_sy" ,Surf.Size.Height ,false);
				node.Add2("text"   ,Text             ,false);
				node.Add2("style"  ,Style            ,false);
				return base.ToXml(node);
			}

			/// <summary>The centre point of the node</summary>
			public virtual v4 Centre { get { return new v4(Bounds.Centre, 0, 1); } }

			/// <summary>The surface to draw on for the node</summary>
			protected View3d.Texture Surf
			{
				get { return m_impl_surf; }
				private set
				{
					if (m_impl_surf != null) m_impl_surf.Dispose();
					m_impl_surf = value;
				}
			}
			private View3d.Texture m_impl_surf;

			/// <summary>Text to display in this node</summary>
			public virtual string Text
			{
				get { return m_impl_text; }
				set { m_impl_text = value; Invalidate(); }
			}
			private string m_impl_text;

			/// <summary>Style attributes for the node</summary>
			public virtual NodeStyle Style
			{
				get { return m_impl_style; }
				set { m_impl_style = value; Invalidate(); }
			}
			private NodeStyle m_impl_style;

			/// <summary>Get/Set the selected state</summary>
			public override bool Selected
			{
				get { return base.Selected; }
				set
				{
					if (Selected == value) return;
					base.Selected = value;
					Invalidate();
				}
			}

			/// <summary>Returns the position to draw the text given the current alignment</summary>
			protected v2 TextLocation(Graphics gfx)
			{
				var sz = v2.From(Surf.Size);
				sz = sz - new v2(gfx.MeasureString(Text, Style.Font, sz));
				switch (Style.TextAlign)
				{
				default: throw new ArgumentException("unknown text alignment");
				case ContentAlignment.TopLeft     : return new v2(sz.x * 0.0f, sz.y * 0.0f);
				case ContentAlignment.TopCenter   : return new v2(sz.x * 0.5f, sz.y * 0.0f);
				case ContentAlignment.TopRight    : return new v2(sz.x * 1.0f, sz.y * 0.0f);
				case ContentAlignment.MiddleLeft  : return new v2(sz.x * 0.0f, sz.y * 0.5f);
				case ContentAlignment.MiddleCenter: return new v2(sz.x * 0.5f, sz.y * 0.5f);
				case ContentAlignment.MiddleRight : return new v2(sz.x * 1.0f, sz.y * 0.5f);
				case ContentAlignment.BottomLeft  : return new v2(sz.x * 0.0f, sz.y * 1.0f);
				case ContentAlignment.BottomCenter: return new v2(sz.x * 0.5f, sz.y * 1.0f);
				case ContentAlignment.BottomRight : return new v2(sz.x * 1.0f, sz.y * 1.0f);
				}
			}

			/// <summary>Return all the locations that connectors can attach to this node (in node space)</summary>
			public abstract IEnumerable<NodeAnchor> AnchorPoints { get; }

			/// <summary>Return the attachment location and normal nearest to 'pt'. 'pt' should be in node space</summary>
			public NodeAnchor NearestAnchor(v4 pt)
			{
				NodeAnchor nearest = null;
				float distance_sq = float.MaxValue;
				foreach (var anchor in AnchorPoints)
				{
					var dist_sq = (anchor.Location - pt).Length3Sq;
					if (dist_sq >= distance_sq) continue;
					distance_sq = dist_sq;
					nearest = anchor;
				}
				return nearest;
			}
		}

		/// <summary>Simple rectangular box node</summary>
		public class BoxNode :Node
		{
			private const string LdrName = "node";

			public BoxNode()
				:this("", 100, 50)
			{}
			public BoxNode(string text, uint width, uint height)
				:this(text, width, height, Color.WhiteSmoke, Color.Black, 5f)
			{}
			public BoxNode(string text, uint width, uint height, Color bkgd, Color border, float corner_radius)
				:this(text, width, height, bkgd, border, corner_radius, m4x4.Identity, new NodeStyle())
			{}
			public BoxNode(string text, uint width, uint height, Color bkgd, Color border, float corner_radius, m4x4 position, NodeStyle style)
				:base(Make(width, height, bkgd, corner_radius), width, height, LdrName, text, position, style)
			{
				Size = new Size((int)width, (int)height);
				CornerRadius = corner_radius;

				AllowInvalidate();
			}
			public BoxNode(XElement node)
				:base(Make(node), LdrName, node)
			{
				Size         = node.Element("size").As<Size>();
				CornerRadius = node.Element("cnr").As<float>();

				AllowInvalidate();
			}

			/// <summary>Export to xml</summary>
			public override XElement ToXml(XElement node)
			{
				node.Add2("size" ,Size         ,false);
				node.Add2("cnr"  ,CornerRadius ,false);
				return base.ToXml(node);
			}

			/// <summary>Create geometry for the node</summary>
			private static string Make(uint width, uint height, Color bkgd, float corner_radius)
			{
				var ldr = new LdrBuilder();
				ldr.Append("*Rect ",LdrName," ",bkgd," {3 ",width," ",height," ",Ldr.Solid()," ",Ldr.CornerRadius(corner_radius),"}\n");
				return ldr.ToString();
			}
			private static string Make(XElement node)
			{
				var size   = node.Element("size" ).As<Size>();
				var cnr    = node.Element("cnr"  ).As<float>();
				var style  = node.Element("style").As<NodeStyle>();
				return Make((uint)size.Width, (uint)size.Height, style.Fill, cnr);
			}

			/// <summary>The diagram space width/height of the node</summary>
			public Size Size
			{
				get { return Surf.Size; }
				set
				{
					if (Surf.Size == value) return;
					Surf.Size = value;
					Invalidate();
				}
			}

			/// <summary>The radius of the box node corners</summary>
			public float CornerRadius
			{
				get { return m_impl_corner_radius; }
				set { m_impl_corner_radius = value; Invalidate(); }
			}
			private float m_impl_corner_radius;

			/// <summary>AABB for the element in diagram space</summary>
			public override BRect Bounds { get { return new BRect(Position.p.xy, v2.From(Size) / 2); } }

			/// <summary>Render the node into the surface</summary>
			public override void Render()
			{
				using (var tex = Surf.LockSurface)
				{
					tex.Gfx.Clear(Selected ? Style.Selected : Style.Fill);
					using (var b = new SolidBrush(Style.Text))
						tex.Gfx.DrawString(Text, Style.Font, b, TextLocation(tex.Gfx));
				}
			}

			/// <summary>Perform a hit test on this object. Returns null for no hit. 'point' is in diagram space</summary>
			public override HitTestResult.Hit HitTest(v2 point, View3d.CameraControls cam)
			{
				var bounds = Bounds;
				if (!bounds.IsWithin(point)) return null;
				point -= bounds.Lower;

				var hit = new HitTestResult.Hit(this, point);
				return hit;
			}

			/// <summary>Return all the locations that connectors can attach to on this node (in node space)</summary>
			public override IEnumerable<NodeAnchor> AnchorPoints
			{
				get
				{
					var x = new []{-Size.Width /2f + CornerRadius, 0f, Size.Width /2f - CornerRadius};
					var y = new []{-Size.Height/2f + CornerRadius, 0f, Size.Height/2f - CornerRadius};
					for (int i = 0; i != 3; ++i)
					{
						yield return new NodeAnchor(this, new v4(+Size.Width/2f,  y[i], 0, 1), +v4.XAxis);
						yield return new NodeAnchor(this, new v4(-Size.Width/2f,  y[i], 0, 1), -v4.XAxis);
						yield return new NodeAnchor(this, new v4(x[i], +Size.Height/2f, 0, 1), +v4.YAxis);
						yield return new NodeAnchor(this, new v4(x[i], -Size.Height/2f, 0, 1), -v4.YAxis);
					}
				}
			}
		}

		/// <summary>Style attributes for nodes</summary>
		public class NodeStyle
		{
			/// <summary>The colour of the node border</summary>
			public Color Border
			{
				get { return m_impl_border; }
				set { Util.SetAndRaise(this, ref m_impl_border, value, StyleChanged); }
			}
			private Color m_impl_border = Color.Black;

			/// <summary>The node background colour</summary>
			public Color Fill
			{
				get { return m_impl_fill; }
				set { Util.SetAndRaise(this, ref m_impl_border, value, StyleChanged); }
			}
			private Color m_impl_fill = Color.WhiteSmoke;

			/// <summary>The colour of the node when selected</summary>
			public Color Selected
			{
				get { return m_impl_selected; }
				set { Util.SetAndRaise(this, ref m_impl_selected, value, StyleChanged); }
			}
			private Color m_impl_selected = Color.LightBlue;

			/// <summary>The node text colour</summary>
			public Color Text
			{
				get { return m_impl_text; }
				set { Util.SetAndRaise(this, ref m_impl_text, value, StyleChanged); }
			}
			private Color m_impl_text;

			/// <summary>The font to use for the node text</summary>
			public Font Font
			{
				get { return m_impl_font; }
				set { Util.SetAndRaise(this, ref m_impl_font, value, StyleChanged); }
			}
			private Font m_impl_font;

			/// <summary>The alignment of the text within the node</summary>
			public ContentAlignment TextAlign
			{
				get { return m_impl_align; }
				set { m_impl_align = value; }
			}
			private ContentAlignment m_impl_align;

			/// <summary>Raised whenever a style property is changed</summary>
			public event EventHandler StyleChanged;

			public NodeStyle()
			{
				using (StyleChanged.SuspendScope())
				{
					Border    = Color.Black;
					Fill      = Color.WhiteSmoke;
					Text      = Color.Black;
					TextAlign = ContentAlignment.MiddleCenter;
					Font      = new Font(FontFamily.GenericSansSerif, 16f, GraphicsUnit.Point);
				}
			}
			public NodeStyle(XElement node)
			{
				Border    = node.Element("border"  ).As<Color>();
				Fill      = node.Element("fill"    ).As<Color>();
				Selected  = node.Element("selected").As<Color>();
				Text      = node.Element("text"    ).As<Color>();
				Font      = node.Element("font"    ).As<Font>();
				TextAlign = node.Element("align"   ).As<ContentAlignment>();
			}

			/// <summary>Export to xml</summary>
			public XElement ToXml(XElement node)
			{
				node.Add2("border"   ,Border    ,false);
				node.Add2("fill"     ,Fill      ,false);
				node.Add2("selected" ,Selected  ,false);
				node.Add2("text"     ,Text      ,false);
				node.Add2("font"     ,Font      ,false);
				node.Add2("align"    ,TextAlign ,false);
				return node;
			}
		}

		#endregion

		#region Connectors

		/// <summary>A base class for contectors between nodes</summary>
		public class Connector :Element
		{
			private const float DefaultSplineControlLength = 50f;
			private const float MinSelectionDistanceSq = 25f;

			public Connector(Node node0, Node node1)
				:this(node0, node1, new ConnectorStyle())
			{}
			public Connector(Node node0, Node node1, ConnectorStyle style)
				:base(Make(node0, node1, false, style), m4x4.Translation(AttachCentre(node0, node1)))
			{
				NodeAnchor anc0, anc1;
				AttachPoints(node0, node1, out anc0, out anc1);
				Anc0 = anc0;
				Anc1 = anc1;
				Style = style;

				Anc0.Elem.PositionChanged += Invalidate;
				Anc1.Elem.PositionChanged += Invalidate;
				Style.StyleChanged += Invalidate;

				AllowInvalidate();
			}
			public Connector(DiagramControl diag, XElement node)
				:base(Make(diag, node), node)
			{
				Anc0  = node.Element("anc0" ).As<NodeAnchor>();
				Anc1  = node.Element("anc1" ).As<NodeAnchor>();
				Style = node.Element("style").As<ConnectorStyle>();

				Anc0.Elem.PositionChanged += Invalidate;
				Anc1.Elem.PositionChanged += Invalidate;
				Style.StyleChanged += Invalidate;

				AllowInvalidate();
			}
			public override void Dispose()
			{
				Anc0.Elem.PositionChanged -= Invalidate;
				Anc1.Elem.PositionChanged -= Invalidate;
				Style.StyleChanged -= Invalidate;
				base.Dispose();
			}

			/// <summary>Get the entity type for this element</summary>
			public override Entity Entity { get { return Entity.Connector; } }

			/// <summary>Export to xml</summary>
			public override XElement ToXml(XElement node)
			{
				node.Add2("anc0"  ,Anc0  ,false);
				node.Add2("anc1"  ,Anc1  ,false);
				node.Add2("style" ,Style ,false);
				return base.ToXml(node);
			}

			/// <summary>Returns the nearest anchor points on two nodes</summary>
			private static void AttachPoints(Node node0, Node node1, out NodeAnchor anc0, out NodeAnchor anc1)
			{
				anc0 = node0.NearestAnchor(m4x4.InverseFast(node0.Position) * node1.Centre);
				anc1 = node1.NearestAnchor(m4x4.InverseFast(node1.Position) * node0.Centre);
			}

			/// <summary>Returns the diagram space position of the centre between nearest anchor points on two nodes</summary>
			private static v4 AttachCentre(NodeAnchor anc0, NodeAnchor anc1)
			{
				v4 centre = (anc0.LocationWS + anc1.LocationWS) / 2f; centre.z = 0;
				return centre;
			}
			private static v4 AttachCentre(Node node0, Node node1)
			{
				NodeAnchor anc0, anc1;
				AttachPoints(node0, node1, out anc0, out anc1);
				return AttachCentre(anc0, anc1);
			}

			/// <summary>Returns the spline that connects two anchor points</summary>
			private static Spline MakeSpline(NodeAnchor anc0, NodeAnchor anc1, bool diag_space)
			{
				var centre = diag_space ? v4.Zero : AttachCentre(anc0, anc1).w0;
				var pt0 = anc0.LocationWS - centre;                         pt0.z = 0;
				var ct0 = pt0 + anc0.NormalWS * DefaultSplineControlLength; ct0.z = 0;
				var pt1 = anc1.LocationWS - centre;                         pt1.z = 0;
				var ct1 = pt1 + anc1.NormalWS * DefaultSplineControlLength; ct1.z = 0;
				return new Spline(pt0,ct0,ct1,pt1);
			}

			/// <summary>Create geometry for the connector</summary>
			private static string Make(NodeAnchor anc0, NodeAnchor anc1, bool selected, ConnectorStyle style)
			{
				var spline = MakeSpline(anc0, anc1, false);
				var ldr = new LdrBuilder();
				using (ldr.Group("connector"))
				{
					ldr.Append("*Spline line ",selected ? style.Selected : style.Line ,"{",spline.Point0,"  ",spline.Ctrl0,"  ",spline.Ctrl1,"  ",spline.Point1,"}\n");
				}
				return ldr.ToString();
			}
			private static string Make(Node node0, Node node1, bool selected, ConnectorStyle style)
			{
				NodeAnchor anc0, anc1;
				AttachPoints(node0, node1, out anc0, out anc1);
				return Make(anc0, anc1, selected, style);
			}
			private static string Make(DiagramControl diag, XElement node)
			{
				var anc0  = node.Element("anc0" ).As<NodeAnchor>();
				var anc1  = node.Element("anc1" ).As<NodeAnchor>();
				var style = node.Element("style").As<ConnectorStyle>();
				return Make(anc0, anc1, false, style);
			}

			/// <summary>The 'from' node</summary>
			public NodeAnchor Anc0 { get; private set; }

			/// <summary>The 'to' node</summary>
			public NodeAnchor Anc1 { get; private set; }

			/// <summary>Style attributes for the connector</summary>
			public virtual ConnectorStyle Style
			{
				get { return m_impl_style; }
				set { m_impl_style = value; Invalidate(); }
			}
			private ConnectorStyle m_impl_style;

			/// <summary>AABB for the element in diagram space</summary>
			public override BRect Bounds
			{
				get
				{
					var spline = MakeSpline(Anc0, Anc1, true);
					var bounds = BRect.Reset;
					bounds.Encompass(spline.Point0.xy, spline.Ctrl0.xy, spline.Ctrl1.xy, spline.Point1.xy);
					return bounds;
				}
			}

			/// <summary>Perform a hit test on this object. Returns null for no hit. 'point' is in diagram space</summary>
			public override HitTestResult.Hit HitTest(v2 point, View3d.CameraControls cam)
			{
				// Find the closest point to the spline
				var spline = MakeSpline(Anc0, Anc1, true);
				var t = Geometry.ClosestPoint(spline, new v4(point, 0, 1));
				var pt = spline.Position(t);
				var diff_cs = // Convert separating distance screen space
					v2.From(cam.SSPointFromWSPoint(new v4(point,0,1))) -
					v2.From(cam.SSPointFromWSPoint(pt));

				if (diff_cs.Length2Sq > MinSelectionDistanceSq)
					return null;

				return new HitTestResult.Hit(this, pt.xy);
			}

			/// <summary>Get/Set the selected state</summary>
			public override bool Selected
			{
				get { return base.Selected; }
				set
				{
					if (Selected == value) return;
					base.Selected = value;
					Graphics.SetColour(Selected ? Style.Selected : Style.Line, "line");
				}
			}

			/// <summary>Update the graphics for the connector</summary>
			public override void Render()
			{
				Graphics.UpdateModel(Make(Anc0, Anc1, Selected, Style));
				Position = m4x4.Translation(AttachCentre(Anc0, Anc1));
			}
		}

		/// <summary>Style properties for connectors</summary>
		public class ConnectorStyle
		{
			/// <summary>Connector styles</summary>
			public enum EType
			{
				Line,
				ForwardArrow,
				BackArrow,
			}

			/// <summary>The connector style</summary>
			public EType Type
			{
				get { return m_impl_type; }
				set { Util.SetAndRaise(this, ref m_impl_type, value, StyleChanged); }
			}
			private EType m_impl_type;

			/// <summary>The colour of the line portion of the connector</summary>
			public Color Line
			{
				get { return m_impl_line; }
				set { Util.SetAndRaise(this, ref m_impl_line, value, StyleChanged); }
			}
			private Color m_impl_line;

			/// <summary>The colour of the line when selected</summary>
			public Color Selected
			{
				get { return m_impl_selected; }
				set { Util.SetAndRaise(this, ref m_impl_selected, value, StyleChanged); }
			}
			private Color m_impl_selected;

			/// <summary>The width of the connector line</summary>
			public float Width
			{
				get { return m_impl_width; }
				set { Util.SetAndRaise(this, ref m_impl_width, value, StyleChanged); }
			}
			private float m_impl_width;

			/// <summary>Raised whenever a style property is changed</summary>
			public event EventHandler StyleChanged;

			public ConnectorStyle()
			{
				using (StyleChanged.SuspendScope())
				{
					Type = EType.Line;
					Line = Color.Black;
					Selected = Color.Blue;
					Width = 0f;
				}
			}
			public ConnectorStyle(XElement node)
			{
				Type     = node.Element("type"    ).As<EType>();
				Line     = node.Element("line"    ).As<Color>();
				Selected = node.Element("selected").As<Color>();
				Width    = node.Element("width"   ).As<float>();
			}

			/// <summary>Export to xml</summary>
			public XElement ToXml(XElement node)
			{
				node.Add2("type"     ,Type     ,false);
				node.Add2("line"     ,Line     ,false);
				node.Add2("selected" ,Selected ,false);
				node.Add2("width"    ,Width    ,false);
				return node;
			}
		}

		#endregion

		#region Misc

		/// <summary>Minimum distance in pixels before the diagram starts dragging</summary>
		private const int MinDragPixelDistanceSq = 25;

		/// <summary>A point that a connector can anchor to</summary>
		public class NodeAnchor
		{
			public v4 LocationWS { get { return Elem.Position * Location; } }
			public v4 NormalWS   { get { return Elem.Position * Normal; } }
			public v4 Location   { get; private set; }
			public v4 Normal     { get; private set; }
			public Element Elem  { get; private set; }

			public NodeAnchor(Element elem, v4 loc, v4 norm) { Elem = elem; Location = loc; Normal = norm; }
			public NodeAnchor(DiagramControl diag, XElement node)
			{
				var id  = node.Element("node_id").As<Guid>();
				var loc = node.Element("loc").As<v4>();
				var elem = (Node)diag.Elements.First(x => x.Id == id);
				var anc = elem.NearestAnchor(loc);

				Elem     = anc.Elem;
				Location = anc.Location;
				Normal   = anc.Normal;
			}

			/// <summary>Export to xml</summary>
			public XElement ToXml(XElement node)
			{
				node.Add2("node_id" ,Elem.Id  ,false);
				node.Add2("loc"     ,Location ,false);
				return node;
			}
		}

		/// <summary>Selection data for a mouse button</summary>
		private class MouseSelection
		{
			public bool          m_btn_down;   // True while the corresponding mouse button is down
			public Point         m_grab_cs;    // The client space location of where the diagram was "grabbed"
			public RectangleF    m_selection;  // Area selection, has width, height of zero when the user isn't selecting
			public HitTestResult m_hit_result; // The hit test result on mouse down
		}

		#endregion

		#region Render Options

		/// <summary>Rendering options</summary>
		public class RdrOptions
		{
			// Colours for graph elements
			public Color m_bg_colour      = SystemColors.ControlDark;      // The fill colour of the background
			public Color m_title_colour   = Color.Black;                   // The colour of the title text
			public Color m_grid_colour    = Color.FromArgb(230, 230, 230); // The colour of the grid lines

			// Graph margins and constants
			public float m_left_margin    = 0.0f; // Fractional distance from the left edge
			public float m_right_margin   = 0.0f;
			public float m_top_margin     = 0.0f;
			public float m_bottom_margin  = 0.0f;
			public float m_title_top      = 0.01f; // Fractional distance  down from the top of the client area to the top of the title text
			public Font  m_title_font     = new Font("tahoma", 12, FontStyle.Bold);    // Font to use for the title text
			public Font  m_note_font      = new Font("tahoma",  8, FontStyle.Regular); // Font to use for graph notes
			public RdrOptions Clone() { return (RdrOptions)MemberwiseClone(); }
		}

		#endregion

		// Members
		private readonly View3d       m_view3d;           // Renderer
		private readonly RdrOptions   m_rdr_options;      // Rendering options
		private readonly EventBatcher m_eb_update_diag;   // Event batcher for updating the diagram graphics
		private View3d.CameraControls m_camera;           // The virtual window over the diagram
		private MouseSelection[]      m_mbutton;          // Per button mouse selection data

		public DiagramControl() :this(new RdrOptions()) {}
		private DiagramControl(RdrOptions rdr_options)
		{
			if (this.IsInDesignMode()) return;

			m_view3d         = new View3d(Handle);
			m_rdr_options    = rdr_options;
			m_eb_update_diag = new EventBatcher(UpdateDiagram);
			m_camera         = new View3d.CameraControls(m_view3d.Drawset);
			m_mbutton        = Util.NewArray<MouseSelection>(Enum<EBtnIdx>.Count);

			InitializeComponent();

			m_view3d.Drawset.FocusPointVisible = false;
			m_view3d.Drawset.OriginVisible = false;
			m_view3d.Drawset.Orthographic = true;

			Elements = new BindingList<Element>();
			Selected = new BindingList<Element>();
			Elements.ListChanged += HandleElementListChanged;

			ResetView();
			DefaultKeyboardShortcuts = true;
			DefaultMouseNavigation = true;
			AllowEditing = true;
		}
		protected override void Dispose(bool disposing)
		{
			ResetDiagram();
			if (disposing && m_view3d != null)
			{
				m_view3d.Dispose();
			}
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <summary>Remove all data from the diagram</summary>
		public void ResetDiagram()
		{
			foreach (var elem in Elements)
				elem.Dispose();
			Elements.Clear();
		}

		/// <summary>Diagram objects</summary>
		public BindingList<Element> Elements { get; private set; }

		/// <summary>The set of selected diagram elements</summary>
		public BindingList<Element> Selected { get; private set; }

		/// <summary>Controls for how the diagram is rendered</summary>
		public RdrOptions RenderOptions
		{
			get { return m_rdr_options; }
		}

		/// <summary>Minimum bounding area for view reset</summary>
		public BRect ResetMinBounds { get { return new BRect(v2.Zero, new v2(Width/1.5f, Height/1.5f)); } }

		/// <summary>Perform a hit test on the diagram</summary>
		public HitTestResult HitTest(v2 ds_point)
		{
			var result = new HitTestResult();
			foreach (var elem in Elements)
			{
				var hit = elem.HitTest(ds_point, m_camera);
				if (hit != null)
					result.Hits.Add(hit);
			}
			result.Hits.Sort((l,r) => r.Element.Position.p.z.CompareTo(l.Element.Position.p.z)); // Top to bottom z order
			return result;
		}
		public class HitTestResult
		{
			public class Hit
			{
				/// <summary>The type of element hit</summary>
				public Entity Entity { get; private set; }

				/// <summary>The element that was hit</summary>
				public Element Element { get; private set; }

				/// <summary>Where on the element it was hit</summary>
				public PointF Point { get; private set; }

				/// <summary>The element's diagram location at the time it was hit</summary>
				public m4x4 Location { get; private set; }

				public Hit(Node node, PointF pt)
				{
					Entity   = Entity.Node;
					Element  = node;
					Point    = pt;
					Location = node.Position;
				}
				public Hit(Connector conn, PointF pt)
				{
					Entity   = Entity.Connector;
					Element  = conn;
					Point    = pt;
					Location = conn.Position;
				}
			}

			/// <summary>All</summary>
			public List<Hit> Hits { get; private set; }
			public HitTestResult() { Hits = new List<Hit>(); }
		}

		/// <summary>Standard keyboard shortcuts</summary>
		public void TranslateKey(object sender, KeyEventArgs e)
		{
			switch (e.KeyCode)
			{
			case Keys.F5:
				{
					UpdateDiagram(true);
					break;
				}
			case Keys.F7:
				{
					ResetView();
					break;
				}
			}
		}

		/// <summary>Enable/Disable default keyboard shortcuts</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool DefaultKeyboardShortcuts
		{
			set
			{
				KeyDown -= TranslateKey;
				if (value)
					KeyDown += TranslateKey;
			}
		}

		/// <summary>Enable/Disable default mouse control</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool DefaultMouseNavigation
		{
			set
			{
				MouseDown  -= OnMouseDown;
				MouseUp    -= OnMouseUp;
				MouseWheel -= OnMouseWheel;
				if (value)
				{
					MouseDown  += OnMouseDown;
					MouseUp    += OnMouseUp;
					MouseWheel += OnMouseWheel;
				}
			}
		}

		/// <summary>Enable/Disable editing</summary>
		public bool AllowEditing { get; set; }

		/// <summary>Default Mouse navigation. Public to allow users to forward mouse calls to us.</summary>
		public void OnMouseDown(object sender, MouseEventArgs e)
		{
			// Get the mouse selection data for the mouse button
			var sel                  = m_mbutton[(int)View3d.ButtonIndex(e.Button)];
			sel.m_btn_down           = true;
			sel.m_grab_cs            = e.Location;
			sel.m_selection.Location = PointToDiagram(sel.m_grab_cs);
			sel.m_selection.Size     = v2.Zero;
			sel.m_hit_result         = HitTest(sel.m_selection.Location);

			MouseMove -= OnMouseDrag;
			MouseMove += OnMouseDrag;
			Capture = true;
		}
		public void OnMouseUp(object sender, MouseEventArgs e)
		{
			// Get the mouse selection data for the mouse button
			var sel = m_mbutton[(int)View3d.ButtonIndex(e.Button)];
			sel.m_btn_down = false;

			// Only release the mouse when all buttons are up
			if (!m_mbutton.Any(mb => mb.m_btn_down))
			{
				Capture = false;
				Cursor = Cursors.Default;
				MouseMove -= OnMouseDrag;
			}

			// If we haven't dragged, treat it as a click instead
			var grab = v2.From(sel.m_grab_cs);
			var diff = v2.From(e.Location) - grab;
			var is_click = diff.Length2Sq < MinDragPixelDistanceSq;

			// Respond to the button
			switch (e.Button)
			{
			case MouseButtons.Left:
				{
					// If this is a click or an area select, select/deselect elements
					var first = sel.m_hit_result.Hits.FirstOrDefault();
					var area_select = first == null || !first.Element.Selected;
					if (is_click || area_select)
						SelectElements(sel.m_selection, ModifierKeys);
					else
						DragSelected(PointToDiagram(e.Location) - sel.m_selection.Location, true);
					break;
				}
			case MouseButtons.Right:
				{
					if (is_click)
						OnDiagramClicked(e);
					break;
				}
			}
			Refresh();
		}
		private void OnMouseDrag(object sender, MouseEventArgs e)
		{
			var sel = m_mbutton[(int)View3d.ButtonIndex(e.Button)];

			// If we haven't dragged, treat it as a click instead (i.e. ignore till it's a drag operation)
			var grab = v2.From(sel.m_grab_cs);
			var diff = v2.From(e.Location) - grab;
			var is_click = diff.Length2Sq < MinDragPixelDistanceSq;
			if (is_click)
				return;

			switch (e.Button)
			{
			case MouseButtons.Left:
				{
					// If the drag operation started on a selected element then drag the
					// selected elements within the diagram, otherwise change the selection area
					var first = sel.m_hit_result.Hits.FirstOrDefault();
					var area_select = first == null || !first.Element.Selected;
					if (area_select)
						sel.m_selection.Size = PointToDiagram(e.Location) - v2.From(sel.m_selection.Location);
					else
						DragSelected(PointToDiagram(e.Location) - sel.m_selection.Location, false);
					break;
				}
			case MouseButtons.Right:
				{
					// Change the cursor once dragging
					Cursor = Cursors.SizeAll;
					PositionDiagram(e.Location, sel.m_selection.Location);
					Refresh();
					break;
				}
			}
		}
		public void OnMouseWheel(object sender, MouseEventArgs e)
		{
			var delta = e.Delta < -999 ? -999 : e.Delta > 999 ? 999 : e.Delta;
			m_camera.Navigate(0, 0, e.Delta / 120f);
			Refresh();
		}
		private void OnDiagramClicked(MouseEventArgs e)
		{
			//var hit = HitTest(e.Location
		}

		/// <summary>Reset the view of the diagram back to the default</summary>
		public void ResetView(bool reset_position = true, bool reset_size = true)
		{
			var bounds = ContentBounds;
			if (bounds.IsValid)
				bounds = bounds.Inflate(
					Maths.Max(ResetMinBounds.SizeX - bounds.SizeX, 0),
					Maths.Max(ResetMinBounds.SizeY - bounds.SizeY, 0));
			else
				bounds = ResetMinBounds;

			if (reset_size)
			{
				float dist0 = 0.5f * bounds.SizeX / (float)Math.Tan(m_camera.FovX * 0.5f);
				float dist1 = 0.5f * bounds.SizeY / (float)Math.Tan(m_camera.FovY * 0.5f);
				m_camera.FocusDist = Maths.Max(dist0, dist1);
			}
			if (reset_position)
			{
				var eye = new v4(bounds.Centre, m_camera.FocusDist, 1);
				var tar = new v4(bounds.Centre, 0, 1);
				m_camera.SetPosition(eye, tar, v4.YAxis);
			}
			Refresh();
		}

		/// <summary>
		/// Returns a point in diagram space from a point in client space
		/// Use to convert mouse (client-space) locations to diagram coordinates</summary>
		public v2 PointToDiagram(Point point)
		{
			var ws = m_camera.WSPointFromSSPoint(point);
			return new v2(ws.x, ws.y);
		}

		/// <summary>Returns a point in client space from a point in diagram space. Inverse of PointToDiagram</summary>
		public Point DiagramToPoint(v2 point)
		{
			var ws = new v4(point, 0.0f, 1.0f);
			return m_camera.SSPointFromWSPoint(ws);
		}

		/// <summary>Shifts the camera so that diagram space position 'ds' is at client space position 'cs'</summary>
		public void PositionDiagram(Point cs, v2 ds)
		{
			// Dragging the diagram is the same as shifting the camera in the opposite direction
			var dst = PointToDiagram(cs);
			m_camera.Navigate(ds.x - dst.x, ds.y - dst.y, 0);
			Refresh();
		}

		/// <summary>Move the selected elements by </summary>
		private void DragSelected(v2 delta, bool commit)
		{
			foreach (var elem in Selected)
			{
				var pos = elem.PositionAtSelectionChange;
				pos.p.x += delta.x;
				pos.p.y += delta.y;
				elem.Position = pos;
				if (commit)
					elem.PositionAtSelectionChange = elem.Position;
			}
			Refresh();
		}

		/// <summary>Handle elements added/removed from the elements list</summary>
		private void HandleElementListChanged(object sender, ListChangedEventArgs args)
		{
			switch (args.ListChangedType)
			{
			case ListChangedType.Reset:
				{
					// Update the diagram
					m_eb_update_diag.Signal();
					break;
				}
			case ListChangedType.ItemAdded:
				{
					var elem = (Element)Elements[args.NewIndex];

					// Set z-order for new items
					var pos = elem.Position;
					pos.p.z = ElementZ;
					elem.Position = pos;

					// Watch for selected/deselected
					elem.SelectedChanged -= HandleElementSelectionChanged;
					elem.SelectedChanged += HandleElementSelectionChanged;

					// Update the diagram
					m_eb_update_diag.Signal();
					break;
				}
			case ListChangedType.ItemDeleted:
				{
					var elem = (Element)Elements[args.NewIndex];

					// Watch for selected/deselected
					elem.SelectedChanged -= HandleElementSelectionChanged;

					// Update the diagram
					m_eb_update_diag.Signal();
					break;
				}
			}
		}

		/// <summary>Add remove elements from our collection of selected objects when their selection changes</summary>
		private void HandleElementSelectionChanged(object sender, EventArgs args)
		{
			var elem = (Element)sender;
			if (elem.Selected)
				Selected.Add(elem);
			else
				Selected.Remove(elem);
		}

		/// <summary>
		/// Select elements that are wholy within 'rect'.
		/// If no modifier keys are down, elements not in 'rect' are deselected.
		/// If 'shift' is down, elements within 'rect' are selected in addition to the existing selection
		/// If 'ctrl' is down, elements within 'rect' are removed from the existing selection.</summary>
		public void SelectElements(RectangleF rect, Keys modifiers)
		{
			// Normalise the selection
			BRect r = rect;

			// If the area of selection is less than the min drag distance, assume click selection
			var is_click = r.DiametreSq < MinDragPixelDistanceSq;
			if (is_click)
			{
				var hits = HitTest(rect.Location);

				// If control is down, deselect the first selected element in the hit list
				if (Bit.AllSet((int)modifiers, (int)Keys.Control))
				{
					var first = hits.Hits.FirstOrDefault(x => x.Element.Selected);
					if (first != null)
						first.Element.Selected = false;
				}
				// If shift is down, select the first element not already selected in the hit list
				else if (Bit.AllSet((int)modifiers, (int)Keys.Shift))
				{
					var first = hits.Hits.FirstOrDefault(x => !x.Element.Selected);
					if (first != null)
						first.Element.Selected = true;
				}
				// Otherwise clear all selections and select the first element in the hit list
				else
				{
					foreach (var elem in Selected.ToArray())
						elem.Selected = false;

					var first = hits.Hits.FirstOrDefault();
					if (first != null)
						first.Element.Selected = true;
				}
			}
			// Otherwise it's area selection
			else
			{
				// If control is down, those in the selection area become deselected
				if (Bit.AllSet((int)modifiers, (int)Keys.Control))
				{
					// Only need to look in the selected list
					foreach (var elem in Selected.Where(x => r.IsWithin(x.Bounds)).ToArray())
						elem.Selected = false;
				}
				// If shift is down, those in the selection area become selected
				else if (Bit.AllSet((int)modifiers, (int)Keys.Shift))
				{
					foreach (var elem in Elements.Where(x => !x.Selected && r.IsWithin(x.Bounds)))
						elem.Selected = true;
				}
				// Otherwise, the existing selection is cleared, and those within the selection area become selected
				else
				{
					foreach (var elem in Elements)
						elem.Selected = r.IsWithin(elem.Bounds);
				}
			}

			Refresh();
		}

		/// <summary>Used to control z order</summary>
		private float ElementZ
		{
			get { return m_impl_element_z += 0.0001f; }
		}
		private float m_impl_element_z;

		/// <summary>
		/// Removes and re-adds all elements to the diagram.
		/// Should only be used when the elements collection is modifed, otherwise use Refresh()</summary>
		private void UpdateDiagram(bool invalidate_all)
		{
			m_view3d.Drawset.RemoveAllObjects();
			foreach (var elem in Elements)
			{
				if (invalidate_all) elem.Invalidate();
				m_view3d.Drawset.AddObject(elem.Graphics);
			}

			Refresh();
		}

		/// <summary>
		/// Removes and re-adds all elements to the diagram.
		/// Should only be used when the elements collection is modifed, otherwise use Refresh()</summary>
		private void UpdateDiagram()
		{
			UpdateDiagram(false);
		}

		/// <summary>Return the area in diagram space that contains all diagram content</summary>
		public BRect ContentBounds
		{
			get
			{
				var bounds = BRect.Reset;
				foreach (var elem in Elements)
					bounds.Encompass(elem.Bounds);
				return bounds;
			}
		}

		/// <summary>Get the rectangular area of the diagram for a given client area.</summary>
		public Rectangle DiagramRegion(Size target_size)
		{
			var marginL = (int)(target_size.Width  * RenderOptions.m_left_margin  );
			var marginT = (int)(target_size.Height * RenderOptions.m_top_margin   );
			var marginR = (int)(target_size.Width  * RenderOptions.m_right_margin );
			var marginB = (int)(target_size.Height * RenderOptions.m_bottom_margin);

			//if (m_title.Length != 0)       { marginT += TextRenderer.MeasureText(m_title      ,       RenderOptions.m_title_font).Height; }
			//if (m_yaxis.Label.Length != 0) { marginL += TextRenderer.MeasureText(m_yaxis.Label, YAxis.RenderOptions.m_label_font).Height; }
			//if (m_xaxis.Label.Length != 0) { marginB += TextRenderer.MeasureText(m_xaxis.Label, XAxis.RenderOptions.m_label_font).Height; }

			//var sx = TextRenderer.MeasureText(YAxis.TickText(AestheticTickValue(9999999.9)), YAxis.RenderOptions.m_tick_font);
			//var sy = TextRenderer.MeasureText(XAxis.TickText(AestheticTickValue(9999999.9)), XAxis.RenderOptions.m_tick_font);
			//marginL += YAxis.RenderOptions.m_tick_length + sx.Width;
			//marginB += XAxis.RenderOptions.m_tick_length + sy.Height;

			var x      = Math.Max(0, marginL);
			var y      = Math.Max(0, marginT);
			var width  = Math.Max(0, target_size.Width  - (marginL + marginR));
			var height = Math.Max(0, target_size.Height - (marginT + marginB));
			return new Rectangle(x, y, width, height);
		}

		/// <summary>Update all invalidated objects in the diagram</summary>
		public override void Refresh()
		{
			base.Refresh();
			foreach (var elem in Elements)
				elem.Refresh();

			m_view3d.SignalRefresh();
		}

		/// <summary>Resize the control</summary>
		protected override void OnResize(EventArgs e)
		{
			if (this.IsInDesignMode()) { base.OnResize(e); return; }

			base.OnResize(e);
			m_view3d.RenderTargetSize = new Size(Width,Height);
		}

		// Absorb this event
		protected override void OnPaintBackground(PaintEventArgs e)
		{
			if (this.IsInDesignMode()) { base.OnPaintBackground(e); return; }

			if (m_view3d.Drawset == null)
				base.OnPaintBackground(e);
		}

		// Paint the control
		protected override void OnPaint(PaintEventArgs e)
		{
			if (this.IsInDesignMode()) { base.OnPaint(e); return; }

			if (m_view3d.Drawset == null) base.OnPaint(e);
			else m_view3d.Drawset.Render();
		}

		/// <summary>Export the current diagram as xml</summary>
		public XElement ExportXml(XElement node)
		{
			Elements.Sort((l,r) => l.Entity.CompareTo(r.Entity));
			foreach (var elem in Elements)
				node.Add2("elem", elem);
			return node;
		}
		public XDocument ExportXml()
		{
			var xml = new XDocument();
			var node = xml.Add2(new XElement("root"));
			ExportXml(node);
			return xml;
		}

		/// <summary>Import the diagram from xml</summary>
		public void ImportXml(XDocument xml)
		{
			if (xml.Root == null) throw new InvalidDataException("xml file does not contain any config data");

			XmlExtensions.AsMap[typeof(Connector )] = (elem, type, ctor) => new Connector(this, elem);
			XmlExtensions.AsMap[typeof(NodeAnchor)] = (elem, type, ctor) => new NodeAnchor(this, elem);

			foreach (var node in xml.Root.Elements())
				Elements.Add((Element)node.ToObject());

			UpdateDiagram();
		}
		public void ImportXml(string xml)
		{
			ImportXml(XDocument.Parse(xml));
		}
		public void LoadXml(string filepath)
		{
			ImportXml(XDocument.Load(filepath));
		}

		#region Component Designer generated code

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.SuspendLayout();
			//
			// DiagramControl
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Name = "DiagramControl";
			this.Size = new System.Drawing.Size(373, 368);
			this.ResumeLayout(false);
		}

		public void BeginInit(){}
		public void EndInit(){}

		#endregion
	}
}
