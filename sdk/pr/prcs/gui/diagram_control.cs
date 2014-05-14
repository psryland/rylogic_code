using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
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
			protected Element(m4x4 position)
			{
				Diagram  = null;
				Id       = Guid.NewGuid();
				Graphics = new View3d.Object("*Group {}");
				Position = position;
			}
			protected Element(XElement node)
			{
				Diagram  = null;
				Graphics = new View3d.Object("*Group {}");
				Id       = node.Element("id").As<Guid>();
				Position = node.Element("pos").As<m4x4>();
			}
			public virtual void Dispose()
			{
				if (Diagram != null)
					Diagram.Elements.Remove(this);

				Graphics = null;
				Invalidated = null;
				PositionChanged = null;
				DataChanged = null;
				SelectedChanged = null;
			}

			/// <summary>Get the entity type for this element</summary>
			public abstract Entity Entity { get; }

			/// <summary>Non-null when the element has been added to a diagram</summary>
			internal DiagramControl Diagram
			{
				get { return m_impl_diag; }
				set
				{
					if (m_impl_diag == value) return;

					Selected = false;

					// Detach from the old diagram
					if (m_impl_diag != null)
					{
						Invalidated     -= m_impl_diag.HandleElementInvalidated;
						SelectedChanged -= m_impl_diag.HandleElementSelectionChanged;
						PositionChanged -= m_impl_diag.HandleElementPositionChanged;
						DataChanged     -= m_impl_diag.HandleElementDataChanged;
					}

					// Assign the new diagram
					m_impl_diag = value;

					// Attach handlers
					if (m_impl_diag != null)
					{
						Invalidated     += m_impl_diag.HandleElementInvalidated;
						SelectedChanged += m_impl_diag.HandleElementSelectionChanged;
						PositionChanged += m_impl_diag.HandleElementPositionChanged;
						DataChanged     += m_impl_diag.HandleElementDataChanged;

						// Set a new Z position
						var pos = Position;
						pos.p.z = m_impl_diag.ElementZ;
						Position = pos;
					}
				}
			}
			private DiagramControl m_impl_diag;

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

			/// <summary>Raised whenever the element needs to be redrawn</summary>
			public event EventHandler Invalidated;

			/// <summary>Raised whenever data associated with the element changes</summary>
			public event EventHandler DataChanged;
			protected void RaiseDataChanged() { DataChanged.Raise(this, EventArgs.Empty); }

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
				get
				{
					return Graphics != null ? Graphics.O2P : m4x4.Identity;
				}
				set
				{
					if (Graphics == null || Equals(Graphics.O2P, value)) return;
					Graphics.O2P = value;
					PositionChanged.Raise(this, EventArgs.Empty);
				}
			}
			internal m4x4 PositionAtSelectionChange { get; set; }
			internal m4x4 DragStartPosition { get; set; }

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

			/// <summary>Drag the element 'delta' from the DragStartPosition</summary>
			public abstract void Drag(v2 delta, bool commit);

			/// <summary>Render the node into the surface</summary>
			public abstract void Render();
		}

		#endregion

		#region Nodes

		/// <summary>A base class for nodes</summary>
		public abstract class Node :Element
		{
			/// <summary>Function prototype for creating an ldr string describing a graphics model</summary>
			private delegate string MakeGfx(uint width, uint height, NodeStyle style);

			/// <summary>Base node constructor</summary>
			/// <param name="make_gfx">Function for creating the graphics model</param>
			/// <param name="autosize">If true, the width and height a maximum size limits (0 meaning unlimited)</param>
			/// <param name="width">The width of the node, or if 'autosize' is true, the maximum width of the node</param>
			/// <param name="height">The height of the node, or if 'autosize' is true, the maximum height of the node</param>
			/// <param name="tex_obj_name">The graphics object name that receives the surface texture</param>
			/// <param name="text">The text of the node</param>
			/// <param name="position">The position of the node on the diagram</param>
			/// <param name="style">Style properties for the node</param>
			protected Node(bool autosize, uint width, uint height, string text, m4x4 position, NodeStyle style)
				:base(position)
			{
				// Set the node text and style
				m_impl_text = text;
				m_impl_style = style;

				// Set the node size
				m_impl_autosize = autosize;
				m_impl_sizemax = m_impl_autosize ? new Size((int)width, (int)height) : Size.Empty;
				var sz = PreferredSize(m_impl_sizemax.Width, m_impl_sizemax.Height);
				if (width  == 0) width  = (uint)sz.Width;
				if (height == 0) height = (uint)sz.Height;

				Init(width, height);
			}
			protected Node(XElement node)
				:base(node)
			{
				// Set the node text and style
				m_impl_text  = node.Element("text").As<string>();
				m_impl_style = node.Element("style").As<NodeStyle>();
				
				// Set the node size
				m_impl_autosize = node.Element("autosize").As<bool>();
				m_impl_sizemax = node.Element("sizemax").As<Size>();
				var size = node.Element("size").As<Size>();
				var sz = PreferredSize(m_impl_sizemax.Width, m_impl_sizemax.Height);
				if (size.Width  == 0) size.Width  = sz.Width;
				if (size.Height == 0) size.Height = sz.Height;
				
				Init((uint)size.Width, (uint)size.Height);
			}
			private void Init(uint width, uint height)
			{
				// Create the surface texture and assign it to the graphics object
				Surf = new View3d.Texture(width, height, new View3d.TextureOptions(true){Filter=View3d.EFilter.D3D11_FILTER_MIN_LINEAR_MAG_POINT_MIP_LINEAR});

				// Watch for style changes
				Style.StyleChanged += Invalidate;
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
				node.Add2("autosize" ,AutoSize ,false);
				node.Add2("sizemax"  ,SizeMax  ,false);
				node.Add2("size"     ,Size     ,false);
				node.Add2("text"     ,Text     ,false);
				node.Add2("style"    ,Style    ,false);
				return base.ToXml(node);
			}

			/// <summary>Text to display in this node</summary>
			public virtual string Text
			{
				get { return m_impl_text; }
				set
				{
					if (m_impl_text == value) return;
					m_impl_text = value;
					
					if (AutoSize)
						SetSize(PreferredSize(SizeMax.Width, SizeMax.Height));

					Invalidate();
					RaiseDataChanged();
				}
			}
			private string m_impl_text;

			/// <summary>Get/Set whether node's auto size to their contents</summary>
			public virtual bool AutoSize
			{
				get { return m_impl_autosize; }
				set
				{
					if (m_impl_autosize == value) return;
					m_impl_autosize = value;
					
					if (AutoSize)
						SetSize(PreferredSize(SizeMax.Width, SizeMax.Height));

					Invalidate();
				}
			}
			private bool m_impl_autosize;

			/// <summary>The diagram space width/height of the node</summary>
			public virtual Size Size
			{
				get { return Surf != null ? Surf.Size : Size.Empty; }
				set
				{
					if (AutoSize) return;
					SetSize(value);
				}
			}
			protected virtual void SetSize(Size sz)
			{
				if (Surf == null || Surf.Size == sz) return;
				Surf.Size = sz;
				Invalidate();
			}

			/// <summary>Get/Set limits for auto size</summary>
			public virtual Size SizeMax
			{
				get { return m_impl_sizemax; }
				set
				{
					if (m_impl_sizemax == value) return;
					m_impl_sizemax = value;
					
					if (AutoSize)
						SetSize(PreferredSize(SizeMax.Width, SizeMax.Height));
				}
			}
			private Size m_impl_sizemax;

			/// <summary>Return the preferred node size given the current text and upper size bounds</summary>
			public Size PreferredSize(float max_width = 0f, float max_height = 0f)
			{
				v2 layout = new v2(
					max_width  != 0f ? max_width  : float.MaxValue,
					max_height != 0f ? max_height : float.MaxValue);

				using (var img = new Bitmap(1,1,PixelFormat.Format32bppArgb))
				using (var gfx = System.Drawing.Graphics.FromImage(img))
				{
					v2 sz = gfx.MeasureString(Text, Style.Font, layout);
					sz.x += Style.Padding.Left + Style.Padding.Right;
					sz.y += Style.Padding.Top + Style.Padding.Bottom;
					return new Size((int)Math.Round(sz.x),(int)Math.Round(sz.y));
				}
			}

			/// <summary>Get/Set the centre point of the node</summary>
			public virtual v4 Centre
			{
				get { return new v4(Bounds.Centre, 0, 1); }
				set
				{
					var pos = Position;
					var ofs = pos.p - Centre;
					pos.p = value + ofs;
					Position = pos;
				}
			}

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
				var sz = v2.From(Size);
				sz.x -= Style.Padding.Left + Style.Padding.Right;
				sz.y -= Style.Padding.Top + Style.Padding.Bottom;
				sz -= new v2(gfx.MeasureString(Text, Style.Font, sz));
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

			/// <summary>Drag the element 'delta' from the DragStartPosition</summary>
			public override void Drag(v2 delta, bool commit)
			{
				var pos = DragStartPosition;
				pos.p.x += delta.x;
				pos.p.y += delta.y;
				Position = pos;
				if (commit)
					DragStartPosition = Position;
			}
		}

		/// <summary>Simple rectangular box node</summary>
		public class BoxNode :Node
		{
			private const string LdrName = "node";

			public BoxNode()
				:this("", true, 0, 0)
			{}
			public BoxNode(string text)
				:this(text, true, 0, 0)
			{}
			public BoxNode(string text, bool autosize, uint width, uint height)
				:this(text, autosize, width, height, Color.WhiteSmoke, Color.Black, 5f)
			{}
			public BoxNode(string text, bool autosize, uint width, uint height, Color bkgd, Color border, float corner_radius)
				:this(text, autosize, width, height, bkgd, border, corner_radius, m4x4.Identity, NodeStyle.Default)
			{}
			public BoxNode(string text, bool autosize, uint width, uint height, Color bkgd, Color border, float corner_radius, m4x4 position, NodeStyle style)
				:base(autosize, width, height, text, position, style)
			{
				CornerRadius = corner_radius;
				Init();
			}
			public BoxNode(XElement node)
				:base(node)
			{
				CornerRadius = node.Element("cnr").As<float>();
				Init();
			}
			private void Init()
			{
				// Create the graphics object
				m_size_changed = true;
				Render();

				AllowInvalidate();
			}

			/// <summary>Export to xml</summary>
			public override XElement ToXml(XElement node)
			{
				node.Add2("cnr" ,CornerRadius ,false);
				return base.ToXml(node);
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

			/// <summary>Detect resize and update the graphics as needed</summary>
			protected override void SetSize(Size sz)
			{
				base.SetSize(sz);
				m_size_changed = true;
			}
			private bool m_size_changed;

			/// <summary>Render the node into the surface</summary>
			public override void Render()
			{
				// Update the graphics model when the size changes
				if (m_size_changed)
				{
					var ldr = new LdrBuilder();
					ldr.Append("*Rect node {3 ",Size.Width," ",Size.Height," ",Ldr.Solid()," ",Ldr.CornerRadius(CornerRadius),"}\n");
					Graphics.UpdateModel(ldr.ToString());
					Graphics.SetTexture(Surf);
					m_size_changed = false;
				}

				using (var tex = Surf.LockSurface())
				{
					var rect = Bounds.ToRectangle();
					rect = rect.Shifted(-rect.X, -rect.Y).Inflated(-1,-1);

					tex.Gfx.Clear(Selected ? Style.Selected : Style.Fill);
					using (var bsh = new SolidBrush(Style.Text))
						tex.Gfx.DrawString(Text, Style.Font, bsh, TextLocation(tex.Gfx));
					//using (var pen = new Pen(Style.Border, 2f))
					//	tex.Gfx.DrawRectangleRounded(pen, rect, CornerRadius);
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
			public static readonly NodeStyle Default = new NodeStyle();

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
				set { Util.SetAndRaise(this, ref m_impl_fill, value, StyleChanged); }
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
				set { Util.SetAndRaise(this, ref m_impl_align, value, StyleChanged); }
			}
			private ContentAlignment m_impl_align;

			/// <summary>The padding surrounding the text in the node</summary>
			public Padding Padding
			{
				get { return m_impl_padding; }
				set { Util.SetAndRaise(this, ref m_impl_padding, value, StyleChanged); }
			}
			private Padding m_impl_padding;

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
					Padding   = new Padding(10,10,10,10);
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
				Padding   = node.Element("padding" ).As<Padding>();
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
				node.Add2("padding"  ,Padding   ,false);
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
				:this(node0, node1, ConnectorStyle.Default)
			{}
			public Connector(Node node0, Node node1, ConnectorStyle style)
				:base(m4x4.Translation(AttachCentre(node0, node1)))
			{
				NodeAnchor anc0, anc1;
				AttachPoints(node0, node1, out anc0, out anc1);
				Anc0 = anc0;
				Anc1 = anc1;
				Style = style;
				Init();
			}
			public Connector(XElement node)
				:base(node)
			{
				Anc0  = node.Element("anc0" ).As<NodeAnchor>();
				Anc1  = node.Element("anc1" ).As<NodeAnchor>();
				Style = node.Element("style").As<ConnectorStyle>();
				Init();
			}
			private void Init()
			{
				// Update the graphics object
				Render();

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

			/// <summary>Drag the element 'delta' from the DragStartPosition</summary>
			public override void Drag(v2 delta, bool commit)
			{
				Invalidate();
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
				var spline = MakeSpline(Anc0, Anc1, false);
				var ldr = new LdrBuilder();
				ldr.Append("*Spline line ",Selected ? Style.Selected : Style.Line ,"{",spline.Point0,"  ",spline.Ctrl0,"  ",spline.Ctrl1,"  ",spline.Point1," *Width {",Style.Width,"} }\n");
				Graphics.UpdateModel(ldr.ToString());
				Position = m4x4.Translation(AttachCentre(Anc0, Anc1));
			}
		}

		/// <summary>Style properties for connectors</summary>
		public class ConnectorStyle
		{
			public static readonly ConnectorStyle Default = new ConnectorStyle();

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
					Width = 5f;
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

		/// <summary>Graphics objects used by the diagram</summary>
		private class Tools :IDisposable
		{
			/// <summary>A rectangular selection box</summary>
			public View3d.Object m_selection_box;

			public Tools()
			{
				var ldr = new LdrBuilder();
				using (ldr.Group("selection_box"))
				{

				}

			}
			public void Dispose()
			{
			}
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
		private readonly View3d       m_view3d;         // Renderer
		private readonly RdrOptions   m_rdr_options;    // Rendering options
		private readonly EventBatcher m_eb_update_diag; // Event batcher for updating the diagram graphics
		private View3d.CameraControls m_camera;         // The virtual window over the diagram
		private MouseSelection[]      m_mbutton;        // Per button mouse selection data
		private bool                  m_edited;         // True when the diagram has been edited and requires saving

		public DiagramControl() :this(new RdrOptions()) {}
		private DiagramControl(RdrOptions rdr_options)
		{
			if (this.IsInDesignMode()) return;

			m_view3d         = new View3d(Handle, Render);
			m_rdr_options    = rdr_options;
			m_eb_update_diag = new EventBatcher(UpdateDiagram);
			m_camera         = new View3d.CameraControls(m_view3d.Drawset);
			m_mbutton        = Util.NewArray<MouseSelection>(Enum<EBtnIdx>.Count);
			m_edited         = false;

			InitializeComponent();

			m_view3d.Drawset.FocusPointVisible = false;
			m_view3d.Drawset.OriginVisible = false;
			m_view3d.Drawset.Orthographic = true;

			Elements = new BindingListEx<Element>();
			Selected = new BindingListEx<Element>();
			Elements.ListChanging += HandleElementListChanging;

			ResetView();
			DefaultKeyboardShortcuts = true;
			DefaultMouseNavigation = true;
			AllowEditing = true;
			AllowMove = true;
		}
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				ResetDiagram();
				m_eb_update_diag.Dispose();
				if (m_view3d != null)
					m_view3d.Dispose();
				if (components != null)
					components.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <summary>Raised whenever elements in the diagram have been edited or moved</summary>
		public event EventHandler DiagramChanged;

		/// <summary>Remove all data from the diagram</summary>
		public void ResetDiagram()
		{
			Elements.Clear();
		}

		/// <summary>Diagram objects</summary>
		public BindingListEx<Element> Elements { get; private set; }

		/// <summary>The set of selected diagram elements</summary>
		public BindingListEx<Element> Selected { get; private set; }

		/// <summary>Controls for how the diagram is rendered</summary>
		public RdrOptions RenderOptions
		{
			get { return m_rdr_options; }
		}

		/// <summary>Minimum bounding area for view reset</summary>
		public BRect ResetMinBounds { get { return new BRect(v2.Zero, new v2(Width/1.5f, Height/1.5f)); } }

		/// <summary>Used to control z order</summary>
		private float ElementZ { get { return m_impl_element_z += 0.0001f; } }
		private float m_impl_element_z;

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

		/// <summary>True if users are allowed to add/remove/edit nodes on the diagram</summary>
		public bool AllowEditing { get; set; }

		/// <summary>True if users are allowed to move elements on the diagram</summary>
		public bool AllowMove { get; set; }

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

			foreach (var elem in Selected)
				elem.DragStartPosition = elem.Position;

			MouseMove -= OnMouseDrag;
			MouseMove += OnMouseDrag;
			Capture = true;
		}
		public void OnMouseUp(object sender, MouseEventArgs e)
		{
			// Get the mouse selection data for the mouse button
			var sel = m_mbutton[(int)View3d.ButtonIndex(e.Button)];
			if (!sel.m_btn_down) return; // Button down wasn't received first
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
					var area_select = sel.m_hit_result.Hits.FirstOrDefault(x => x.Element.Selected) == null;
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
			var idx = (int)View3d.ButtonIndex(e.Button);
			if (idx == -1) return; // Drag event with no mouse button down?
			var sel = m_mbutton[idx];

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
					var area_select = sel.m_hit_result.Hits.FirstOrDefault(x => x.Element.Selected) == null;
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
			// Show the context menu on right click
			if (e.Button == MouseButtons.Right)
				ShowContextMenu(e.Location);
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
			if (!AllowMove) return;
			foreach (var elem in Selected)
				elem.Drag(delta, commit);
			Refresh();
		}

		/// <summary>Handle elements added/removed from the elements list</summary>
		private void HandleElementListChanging(object sender, BindingListEx<Element>.ListChgEventArgs args)
		{
			switch (args.ChangeType)
			{
			case ListChg.PreReset:
				{
					// This Elements list is about to be cleared, detach handlers from all elements
					foreach (var elem in Elements)
						elem.Diagram = null;

					break;
				}
			case ListChg.ItemPreAdd:
				{
					// An element is about to be added, attach handlers
					var elem = args.Item;
					if (elem.Diagram == this) throw new ArgumentException("element already belongs to this diagram");
					if (elem.Diagram != null) throw new ArgumentException("element already belongs to another diagram");
					elem.Diagram = this;
					break;
				}
			case ListChg.ItemPreRemove:
				{
					// An element is about to be removed, detach handlers
					args.Item.Diagram = null;
					break;
				}
			case ListChg.ItemAdded:
			case ListChg.ItemRemoved:
			case ListChg.Reset:
				{
					// Update the diagram
					m_eb_update_diag.Signal();
					break;
				}
			}
		}

		/// <summary>Handle an element being invalidated</summary>
		private void HandleElementInvalidated(object sender, EventArgs e)
		{
		}

		/// <summary>Called when an element in the diagram has been moved</summary>
		private void HandleElementPositionChanged(object sender, EventArgs e)
		{
			m_edited = true;
		}

		/// <summary>Called when the data associated with an element has changed</summary>
		private void HandleElementDataChanged(object sender, EventArgs e)
		{
			m_edited = true;
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
				// Otherwise clear all selections and select the top element in the hit list
				else
				{
					foreach (var elem in Selected.ToArray())
						elem.Selected = false;

					if (hits.Hits.Count != 0)
					{
						var sel = hits.Hits.MaxBy(x => x.Element.Position.p.z);
						if (sel != null)
							sel.Element.Selected = true;
					}
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

		/// <summary>Spread the nodes on the diagram so none are overlapping</summary>
		public void ScatterNodes()
		{
			int node_count = Elements.Count(x => x.Entity == Entity.Node);
			int col_count = 1 + (int)Math.Sqrt(node_count);

			int col = 0;
			float row_height = 0f;
			v2 xy = v2.Zero, margin = new v2(30,30);
			foreach (var node in Elements.Where(x => x.Entity == Entity.Node))
			{
				var pos = node.Position;
				pos.p = new v4(xy, 0, 1);
				node.Position = pos;

				col++;
				xy.x += node.Bounds.SizeX + margin.x;
				row_height = Math.Max(row_height, node.Bounds.SizeY);

				if (col == col_count)
				{
					col = 0;
					xy.x = 0;
					xy.y += row_height + margin.y;
					row_height = 0;
				}
			}
			ResetView();
			Refresh();
		}

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

			// Notify observers that the diagram has changed
			if (m_edited)
			{
				DiagramChanged.Raise(this, EventArgs.Empty);
				m_edited = false;
			}

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

			if (m_view3d.Drawset == null)
				base.OnPaint(e);
			else
				m_view3d.Refresh();
		}

		/// <summary>Render and Present the scene</summary>
		private void Render()
		{
			m_view3d.Drawset.Render();

			using (var tex = m_view3d.RenderTarget.LockSurface())
				tex.Gfx.DrawEllipse(Pens.Blue, new Rectangle(100,100,200,150));

			m_view3d.Present();
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
		public void ImportXml(XDocument xml, bool reset = true)
		{
			if (xml.Root == null) throw new InvalidDataException("xml file does not contain any config data");

			XmlExtensions.AsMap[typeof(NodeAnchor)] = (elem, type, ctor) => new NodeAnchor(this, elem);

			if (reset)
				ResetDiagram();

			foreach (var node in xml.Root.Elements())
				Elements.Add((Element)node.ToObject());

			UpdateDiagram();
		}
		public void ImportXml(string xml, bool reset = true)
		{
			ImportXml(XDocument.Parse(xml), reset);
		}
		public void LoadXml(string filepath, bool reset = true)
		{
			ImportXml(XDocument.Load(filepath), reset);
		}

		/// <summary>Event allowing callers to add options to the context menu</summary>
		public event Action<DiagramControl, ContextMenuStrip> AddUserMenuOptions;

		/// <summary>Create and display a context menu</summary>
		public void ShowContextMenu(Point location)
		{
			// Note: using lists and AddRange here for performance reasons
			// Using 'DropDownItems.Add' causes lots of PerformLayout calls
			var context_menu = new ContextMenuStrip { Renderer = new ContextMenuRenderer() };
			var lvl0 = new List<ToolStripItem>();

			using (this.ChangeCursor(Cursors.WaitCursor))
			using (context_menu.SuspendLayout(false))
			{
				// Allow users to add menu options
				AddUserMenuOptions.Raise(this, context_menu);

				if (AllowEditing)
				{
				}

				if (AllowMove)
				{
					#region Scatter
					var scatter = lvl0.Add2(new ToolStripMenuItem("Scatter"));
					scatter.Click += (s,a) => ScatterNodes();
					#endregion
				}

				/*
				#region Show Value
				{
					var show_value = lvl0.Add2(new ToolStripMenuItem("Show Value"));
					show_value.Checked = (bool)m_tooltip.Tag;
					show_value.Click += (s,a) =>
						{
							m_tooltip.Hide(this);
							if ((bool)m_tooltip.Tag) MouseMove -= OnMouseMoveTooltip;
							m_tooltip.Tag = !(bool)m_tooltip.Tag;
							if ((bool)m_tooltip.Tag) MouseMove += OnMouseMoveTooltip;
						};
				}
				#endregion

				#region Zoom Menu
				{
					var zoom_menu = lvl0.Add2(new ToolStripMenuItem("Zoom"));
					var lvl1 = new List<ToolStripItem>();

					var default_zoom = lvl1.Add2(new ToolStripMenuItem("Default"));
					default_zoom.Click += (s,a) =>
						{
							FindDefaultRange();
							ResetToDefaultRange();
							Refresh();
						};

					var zoom_in = lvl1.Add2(new ToolStripMenuItem("In"));
					zoom_in.Click += (s,a) =>
						{
							var point = PointToGraph(m_selection.Location);
							Zoom *= 0.5;
							PositionGraph(m_selection.Location,point);
							Refresh();
						};

					var zoom_out = lvl1.Add2(new ToolStripMenuItem("Out"));
					zoom_out.Click += (s,a) =>
						{
							var point = PointToGraph(m_selection.Location);
							Zoom *= 2.0;
							PositionGraph(m_selection.Location,point);
							Refresh();
						};

					zoom_menu.DropDownItems.AddRange(lvl1.ToArray());
				}
				#endregion

				#region Series menus
				if (m_data.Count != 0)
				{
					#region All Series
					{
						var series_menu = lvl0.Add2(new ToolStripMenuItem("Series: All"));
						var lvl1 = new List<ToolStripItem>();

						#region Visible
						{
							var state = m_data.Aggregate(0, (x,s) => x | (s.RenderOptions.m_visible ? 2 : 1));
							var option = lvl1.Add2(new ToolStripMenuItem("Visible"));
							option.CheckState = state == 2 ? CheckState.Checked : state == 1 ? CheckState.Unchecked : CheckState.Indeterminate;
							option.Click += (s,a) =>
								{
									option.CheckState = option.CheckState == CheckState.Checked ? CheckState.Unchecked : CheckState.Checked;
									foreach (var x in m_data) x.RenderOptions.m_visible = option.CheckState == CheckState.Checked;
									RegenBitmap = true;
								};
						}
						#endregion

						series_menu.DropDownItems.AddRange(lvl1.ToArray());
					}
					#endregion
					#region Individual Series
					for (var index = 0; index != m_data.Count; ++index)
					{
						var series = m_data[index];

						var series_menu = lvl0.Add2(new ToolStripMenuItem("Series: " + series.Name));
						var lvl1 = new List<ToolStripItem>();

						if      (series.RenderOptions.m_plot_type == Series.RdrOpts.PlotType.Point) series_menu.ForeColor = series.RenderOptions.m_point_colour;
						else if (series.RenderOptions.m_plot_type == Series.RdrOpts.PlotType.Line ) series_menu.ForeColor = series.RenderOptions.m_line_colour;
						else if (series.RenderOptions.m_plot_type == Series.RdrOpts.PlotType.Bar  ) series_menu.ForeColor = series.RenderOptions.m_bar_colour;
						series_menu.Checked = series.RenderOptions.m_visible;
						series_menu.Tag = index;
						series_menu.Click += (s,a) =>
							{
								series.RenderOptions.m_visible = !series.RenderOptions.m_visible;
								series_menu.Checked = series.RenderOptions.m_visible;
								RegenBitmap = true;
							};

						#region Elements
						{
							var elements_menu = lvl1.Add2(new ToolStripMenuItem("Elements"));
							var lvl2 = new List<ToolStripItem>();

							#region Draw main data
							{
								var option = lvl2.Add2(new ToolStripMenuItem("Series data"));
								option.Checked = series.RenderOptions.m_draw_data;
								option.Click += (s,a) =>
									{
										series.RenderOptions.m_draw_data = !series.RenderOptions.m_draw_data;
										RegenBitmap = true;
									};
							}
							#endregion
							#region Draw error bars
							{
								var option = lvl2.Add2(new ToolStripMenuItem("Error Bars"));
								option.Checked = series.RenderOptions.m_draw_error_bars;
								option.Click += (s,a) =>
									{
										series.RenderOptions.m_draw_error_bars = !series.RenderOptions.m_draw_error_bars;
										RegenBitmap = true;
									};
							}
							#endregion
							#region Draw zeros
							{
								var draw_zeros = lvl2.Add2(new ToolStripComboBox());
								draw_zeros.Items.AddRange(Enum<Series.RdrOpts.PlotZeros>.Names.Select(x => x + " Zeros").Cast<object>().ToArray());
								draw_zeros.SelectedIndex = (int)series.RenderOptions.m_draw_zeros;
								draw_zeros.SelectedIndexChanged += (s,a) =>
									{
										series.RenderOptions.m_draw_zeros = (Series.RdrOpts.PlotZeros)draw_zeros.SelectedIndex;
										RegenBitmap = true;
									};
								draw_zeros.KeyDown += (s,a) =>
									{
										if (a.KeyCode == Keys.Return)
											context_menu.Close();
									};
							}
							#endregion

							elements_menu.DropDownItems.AddRange(lvl2.ToArray());
						}
						#endregion

						#region Plot Type Menu
						{
							var plot_type_menu = lvl1.Add2(new ToolStripMenuItem("Plot Type"));
							{
								var option = new ToolStripComboBox();
								plot_type_menu.DropDownItems.Add(option);
								option.Items.AddRange(Enum<Series.RdrOpts.PlotType>.Names.Cast<object>().ToArray());
								option.SelectedIndex = (int)series.RenderOptions.m_plot_type;
								option.SelectedIndexChanged += (s,a) =>
									{
										series.RenderOptions.m_plot_type = (Series.RdrOpts.PlotType)option.SelectedIndex;
										RegenBitmap = true;
									};
								option.KeyDown += (s,a) =>
									{
										if (a.KeyCode == Keys.Return)
											context_menu.Close();
									};
							}
						}
						#endregion

						#region Appearance Menu
						{
							var appearance_menu = lvl1.Add2(new ToolStripMenuItem("Appearance"));
							var lvl2 = new List<ToolStripItem>();

							#region Points
							if (series.RenderOptions.m_plot_type == Series.RdrOpts.PlotType.Point || series.RenderOptions.m_plot_type == Series.RdrOpts.PlotType.Line)
							{
								var point_menu = lvl2.Add2(new ToolStripMenuItem("Points"));
								var lvl3 = new List<ToolStripItem>();

								#region Size
								{
									var size_menu = lvl3.Add2(new ToolStripMenuItem("Size"));
									{
										var option = new ToolStripTextBox{AcceptsReturn = false};
										size_menu.DropDownItems.Add(option);
										option.Text = series.RenderOptions.m_point_size.ToString("0.00");
										option.TextChanged += (s,a) =>
											{
												float size;
												if (!float.TryParse(option.Text,out size)) return;
												series.RenderOptions.m_point_size = size;
												RegenBitmap = true;
											};
									}
								}
								#endregion
								#region Colour
								{
									var colour_menu = lvl3.Add2(new ToolStripMenuItem("Colour"));
									{
										var option = new NoHighlightToolStripMenuItem("     "){DisplayStyle = ToolStripItemDisplayStyle.Text, BackColor = series.RenderOptions.m_point_colour};
										colour_menu.DropDownItems.Add(option);
										option.Click += (s,a) =>
											{
												var cd = new ColourUI{InitialColour = option.BackColor};
												if (cd.ShowDialog() != DialogResult.OK) return;
												series.RenderOptions.m_point_colour = cd.Colour;
												RegenBitmap = true;
											};
									}
								}
								#endregion

								point_menu.DropDownItems.AddRange(lvl3.ToArray());
							}
							#endregion
							#region Lines
							if (series.RenderOptions.m_plot_type == Series.RdrOpts.PlotType.Line)
							{
								var line_menu = lvl2.Add2(new ToolStripMenuItem("Lines"));
								var lvl3 = new List<ToolStripItem>();

								#region Width
								{
									var width_menu = lvl3.Add2(new ToolStripMenuItem("Width"));
									{
										var option = new ToolStripTextBox();
										width_menu.DropDownItems.Add(option);
										option.Text = series.RenderOptions.m_line_width.ToString("0.00");
										option.TextChanged += (s,a) =>
											{
												float width;
												if (!float.TryParse(option.Text,out width)) return;
												series.RenderOptions.m_line_width = width;
												RegenBitmap = true;
											};
									}
								}
								#endregion
								#region Colour
								{
									var colour_menu = lvl3.Add2(new ToolStripMenuItem("Colour"));
									{
										var option = new NoHighlightToolStripMenuItem("     "){DisplayStyle = ToolStripItemDisplayStyle.Text, BackColor = series.RenderOptions.m_line_colour};
										colour_menu.DropDownItems.Add(option);
										option.Click += (s,a) =>
											{
												var cd = new ColourUI{InitialColour = option.BackColor};
												if (cd.ShowDialog() != DialogResult.OK) return;
												series.RenderOptions.m_line_colour = cd.Colour;
												RegenBitmap = true;
											};
									}
								}
								#endregion

								line_menu.DropDownItems.AddRange(lvl3.ToArray());
							}
							#endregion
							#region Bars
							if (series.RenderOptions.m_plot_type == Series.RdrOpts.PlotType.Bar)
							{
								var bar_menu = lvl2.Add2(new ToolStripMenuItem("Bars"));
								var lvl3 = new List<ToolStripItem>();

								#region Width
								{
									var width_menu = lvl3.Add2(new ToolStripMenuItem("Width"));
									{
										var option = new ToolStripTextBox();
										width_menu.DropDownItems.Add(option);
										option.Text = series.RenderOptions.m_bar_width.ToString("0.00");
										option.TextChanged += (s,a) =>
											{
												float width;
												if (!float.TryParse(option.Text,out width)) return;
												series.RenderOptions.m_bar_width = width;
												RegenBitmap = true;
											};
										option.KeyDown += (s,a) =>
											{
												if (a.KeyCode == Keys.Return)
													context_menu.Close();
											};
									}
								}
								#endregion
								#region Bar Colour
								{
									var colour_menu = lvl3.Add2(new ToolStripMenuItem("Colour"));
									{
										var option = new NoHighlightToolStripMenuItem("     "){DisplayStyle = ToolStripItemDisplayStyle.Text, BackColor = series.RenderOptions.m_bar_colour};
										colour_menu.DropDownItems.Add(option);
										option.Click += (s,a) =>
											{
												var cd = new ColourUI{InitialColour = option.BackColor};
												if (cd.ShowDialog() != DialogResult.OK) return;
												series.RenderOptions.m_bar_colour = cd.Colour;
												RegenBitmap = true;
											};
									}
								}
								#endregion

								bar_menu.DropDownItems.AddRange(lvl3.ToArray());
							}
							#endregion
							#region Error Bars
							if (series.RenderOptions.m_draw_error_bars)
							{
								var errorbar_menu = lvl2.Add2(new ToolStripMenuItem("Error Bars"));
								var lvl3 = new List<ToolStripItem>();

								#region Colour
								{
									var colour_menu = lvl3.Add2(new ToolStripMenuItem("Colour"));
									{
										var option = new NoHighlightToolStripMenuItem("     "){DisplayStyle = ToolStripItemDisplayStyle.Text, BackColor = series.RenderOptions.m_error_bar_colour};
										colour_menu.DropDownItems.Add(option);
										option.Click += (s,a) =>
											{
												var cd = new ColourUI{InitialColour = option.BackColor};
												if (cd.ShowDialog() != DialogResult.OK) return;
												series.RenderOptions.m_error_bar_colour = cd.Colour;
												RegenBitmap = true;
											};
									}
								}
								#endregion

								errorbar_menu.DropDownItems.AddRange(lvl3.ToArray());
							}
							#endregion

							appearance_menu.DropDownItems.AddRange(lvl2.ToArray());
						}
						#endregion

						#region Moving Average
						{
							var mv_avr_menu = lvl1.Add2(new ToolStripMenuItem("Moving Average"));
							#region Draw Moving Average
							{
								var option = new ToolStripMenuItem("Draw Moving Average");
								mv_avr_menu.DropDownItems.Add(option);
								option.Checked = series.RenderOptions.m_draw_moving_avr;
								option.Click += (s,a) =>
									{
										series.RenderOptions.m_draw_moving_avr = !series.RenderOptions.m_draw_moving_avr;
										RegenBitmap = true;
									};
							}
							#endregion
							#region Window Size
							{
								var win_size_menu = new ToolStripMenuItem("Window Size");
								mv_avr_menu.DropDownItems.Add(win_size_menu);
								{
									var option = new ToolStripTextBox();
									win_size_menu.DropDownItems.Add(option);
									option.Text = series.RenderOptions.m_ma_window_size.ToString(CultureInfo.InvariantCulture);
									option.TextChanged += (s,a) =>
										{
											int size;
											if (!int.TryParse(option.Text,out size)) return;
											series.RenderOptions.m_ma_window_size = size;
											RegenBitmap = true;
										};
								}
							}
							#endregion
							#region Line Width
							{
								var line_width_menu = new ToolStripMenuItem("Line Width");
								mv_avr_menu.DropDownItems.Add(line_width_menu);
								{
									var option = new ToolStripTextBox();
									line_width_menu.DropDownItems.Add(option);
									option.Text = series.RenderOptions.m_ma_line_width.ToString(CultureInfo.InvariantCulture);
									option.TextChanged += (s,a) =>
										{
											int size;
											if (!int.TryParse(option.Text,out size)) return;
											series.RenderOptions.m_ma_line_width = size;
											RegenBitmap = true;
										};
								}
							}
							#endregion
							#region Line Colour
							{
								var line_colour_menu = new ToolStripMenuItem("Line Colour");
								mv_avr_menu.DropDownItems.Add(line_colour_menu);
								{
									var option = new NoHighlightToolStripMenuItem("     "){DisplayStyle = ToolStripItemDisplayStyle.Text, BackColor = series.RenderOptions.m_ma_line_colour};
									line_colour_menu.DropDownItems.Add(option);
									option.Click += (s,a) =>
										{
											var cd = new ColourUI{InitialColour = option.BackColor};
											if (cd.ShowDialog() != DialogResult.OK) return;
											series.RenderOptions.m_ma_line_colour = cd.Colour;
											RegenBitmap = true;
										};
								}
							}
							#endregion
						}
						#endregion

						#region Delete Series
						if (series.AllowDelete)
						{
							var delete_menu = lvl1.Add2(new ToolStripMenuItem("Delete"));
							delete_menu.Click += (s,a) =>
								{
									if (MessageBox.Show(this,"Confirm delete?","Delete Series",MessageBoxButtons.YesNo,MessageBoxIcon.Warning) != DialogResult.Yes) return;
									m_data.RemoveAt((int)series_menu.Tag);
									FindDefaultRange();
									RegenBitmap = true;
								};
						}
						#endregion

						series_menu.DropDownItems.AddRange(lvl1.ToArray());
					}
					#endregion
				}
				#endregion

				#region Rendering Options
				{
					var appearance_menu = lvl0.Add2(new ToolStripMenuItem("Appearance"));
					var lvl1 = new List<ToolStripItem>();

					#region Background Colour
					{
						var bk_colour_menu = lvl1.Add2(new ToolStripMenuItem("Background Colour"));
						{
							var option = new NoHighlightToolStripMenuItem("     "){DisplayStyle = ToolStripItemDisplayStyle.Text, BackColor = m_rdr_options.m_bg_colour};
							bk_colour_menu.DropDownItems.Add(option);
							option.Click += (s,a) =>
								{
									var cd = new ColourUI{InitialColour = option.BackColor};
									if (cd.ShowDialog() != DialogResult.OK) return;
									m_rdr_options.m_bg_colour = cd.Colour;
									RegenBitmap = true;
								};
						}
					}
					#endregion
					#region Opacity
					if (ParentForm != null)
					{
						var opacity_menu = lvl1.Add2(new ToolStripMenuItem("Opacity"));
						{
							var option = new ToolStripTextBox();
							opacity_menu.DropDownItems.Add(option);
							option.Text = ParentForm.Opacity.ToString("0.00");
							option.TextChanged += (s,a) =>
								{
									double opc;
									if (ParentForm != null && double.TryParse(option.Text,out opc))
										ParentForm.Opacity = Math.Max(0.1,Math.Min(1.0,opc));
								};
						}
					}
					#endregion

					appearance_menu.DropDownItems.AddRange(lvl1.ToArray());
				}
				#endregion

				#region Axis Options
				{
					var axes_menu = lvl0.Add2(new ToolStripMenuItem("Axes"));
					var lvl1 = new List<ToolStripItem>();

					#region X Axis
					{
						var x_axis_menu = lvl1.Add2(new ToolStripMenuItem("X Axis"));
						var lvl2 = new List<ToolStripItem>();

						#region Min
						{
							var min_menu = lvl2.Add2(new ToolStripMenuItem("Min"));
							{
								var option = new ToolStripTextBox();
								min_menu.DropDownItems.Add(option);
								option.Text = XAxis.Min.ToString("G5");
								option.TextChanged += (s,a) =>
									{
										float v;
										if (!float.TryParse(option.Text,out v)) return;
										XAxis.Min = v;
										RegenBitmap = true;
									};
							}
						}
						#endregion
						#region Max
						{
							var max_menu = lvl2.Add2(new ToolStripMenuItem("Max"));
							{
								var option = new ToolStripTextBox();
								max_menu.DropDownItems.Add(option);
								option.Text = XAxis.Max.ToString("G5");
								option.TextChanged += (s,a) =>
									{
										float v;
										if (!float.TryParse(option.Text,out v)) return;
										XAxis.Max = v;
										RegenBitmap = true;
									};
							}
						}
						#endregion
						#region Allow Scroll
						{
							var allow_scroll = lvl2.Add2(new ToolStripMenuItem { Text = "Allow Scroll", Checked = m_xaxis.AllowScroll });
							allow_scroll.Click += (s,a) => m_xaxis.AllowScroll = !m_xaxis.AllowScroll;
						}
						#endregion
						#region Allow Zoom
						{
							var allow_zoom = lvl2.Add2(new ToolStripMenuItem { Text = "Allow Zoom", Checked = m_xaxis.AllowZoom });
							allow_zoom.Click += (s,e) => m_xaxis.AllowZoom = !m_xaxis.AllowZoom;
						}
						#endregion

						x_axis_menu.DropDownItems.AddRange(lvl2.ToArray());
					}
					#endregion
					#region Y Axis
					{
						var y_axis_menu = lvl1.Add2(new ToolStripMenuItem("Y Axis"));
						var lvl2 = new List<ToolStripItem>();

						#region Min
						{
							var min_menu = lvl2.Add2(new ToolStripMenuItem("Min"));
							{
								var option = new ToolStripTextBox();
								min_menu.DropDownItems.Add(option);
								option.Text = YAxis.Min.ToString("G5");
								option.TextChanged += (s,a) =>
									{
										float v;
										if (!float.TryParse(option.Text,out v)) return;
										YAxis.Min = v;
										RegenBitmap = true;
									};
							}
						}
						#endregion
						#region Max
						{
							var max_menu = lvl2.Add2(new ToolStripMenuItem("Max"));
							{
								var option = new ToolStripTextBox();
								max_menu.DropDownItems.Add(option);
								option.Text = YAxis.Max.ToString("G5");
								option.TextChanged += (s,a) =>
									{
										float v;
										if (!float.TryParse(option.Text,out v)) return;
										YAxis.Max = v;
										RegenBitmap = true;
									};
							}
						}
						#endregion
						#region Allow Scroll
						{
							var allow_scroll = lvl2.Add2(new ToolStripMenuItem { Text = "Allow Scroll", Checked = m_yaxis.AllowScroll });
							allow_scroll.Click += (s,e) => m_yaxis.AllowScroll = !m_yaxis.AllowScroll;
						}
						#endregion
						#region Allow Zoom
						{
							var allow_zoom = lvl2.Add2(new ToolStripMenuItem { Text = "Allow Zoom", Checked = m_yaxis.AllowZoom });
							allow_zoom.Click += (s,e) => m_yaxis.AllowZoom = !m_yaxis.AllowZoom;
						}
						#endregion

						y_axis_menu.DropDownItems.AddRange(lvl2.ToArray());
					}
					#endregion

					axes_menu.DropDownItems.AddRange(lvl1.ToArray());
				}
				#endregion

				#region Notes
				{
					var note_menu = lvl0.Add2(new ToolStripMenuItem("Notes"));
					var lvl1 = new List<ToolStripItem>();

					#region Add
					{
						var option = lvl1.Add2(new ToolStripMenuItem("Add"));
						option.Click += (s,e) =>
							{
								var form = new Form
									{
										Text = "Add Note",
										FormBorderStyle = FormBorderStyle.SizableToolWindow,
										StartPosition = FormStartPosition.Manual,
										Location = PointToScreen(location),
										Size = new Size(160,100),
										KeyPreview = true
									};
								form.KeyDown += (o,a) =>
									{
										if (a.KeyCode == Keys.Enter && (a.Modifiers & Keys.Control) == 0)
											form.DialogResult = DialogResult.OK;
									};
								var tb = new TextBox
									{
										Dock = DockStyle.Fill,
										Multiline = true,
										AcceptsReturn = false
									};
								form.Controls.Add(tb);
								if (form.ShowDialog(this) != DialogResult.OK || tb.Text.Length == 0) return;
								m_notes.Add(new Note(tb.Text,PointToGraph(location)));
								RegenBitmap = true;
							};
					}
					#endregion
					#region Delete
					{
						var dist = 100f;
						Note nearest = null;
						foreach (var note in m_notes)
						{
							var sep = GraphToPoint(note.m_loc) - (Size)location;
							var d = (float)Math.Sqrt(sep.X*sep.X + sep.Y*sep.Y);
							if (d >= dist) continue;
							dist = d;
							nearest = note;
						}
						if (nearest != null)
						{
							var option = lvl1.Add2(new ToolStripMenuItem("Delete '{0}'".Fmt(nearest.m_msg.Summary(12))));
							option.Click += (s,e) =>
								{
									m_notes.Remove(nearest);
									RegenBitmap = true;
								};
						}
					}
					#endregion

					note_menu.DropDownItems.AddRange(lvl1.ToArray());
				}
				#endregion

				#region Export
				{
					var export_menu = lvl0.Add2(new ToolStripMenuItem("Export"));
					export_menu.Click += (s,a) => ExportCSV();
				}
				#endregion

				#region Import
				{
					var import_menu = lvl0.Add2(new ToolStripMenuItem("Import"));
					import_menu.Click += (s,a) => ImportCSV(null);
				}
				#endregion
*/
				context_menu.Items.AddRange(lvl0.ToArray());
			}

			context_menu.Closed += (s,a) => Refresh();
			context_menu.Show(this, location);
		}

		/// <summary>Custom button renderer because the office 'checked' state buttons look crap</summary>
		private class ContextMenuRenderer :ToolStripProfessionalRenderer
		{
			protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
			{
				var item = e.Item as NoHighlightToolStripMenuItem;
				if (item == null) { base.OnRenderMenuItemBackground(e); return; }

				e.Graphics.FillRectangle(new SolidBrush(item.BackColor), e.Item.ContentRectangle);
				e.Graphics.DrawRectangle(Pens.Black, e.Item.ContentRectangle);
			}
		}

		/// <summary>Special menu item that doesn't draw highlighted</summary>
		private class NoHighlightToolStripMenuItem :ToolStripMenuItem
		{
			public NoHighlightToolStripMenuItem(string text) :base(text) {}
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
