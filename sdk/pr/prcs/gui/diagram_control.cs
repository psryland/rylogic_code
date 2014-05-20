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
		private const int TextureScale = 4;

		/// <summary>
		/// The types of elements that can be on a diagram.
		/// Also defines the order that elements are written to xml</summary>
		public enum Entity
		{
			Node,
			Connector,
			Label,
		}

		/// <summary>Says "I have a guid"</summary>
		private interface IHasId
		{
			Guid Id { get; }
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
						var p = Position;
						p.pos.z = m_impl_diag.ElementZ;
						Graphics.O2P = p; // Set directly so we don't raise the event
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
			public v2 PositionXY
			{
				get { return Position.pos.xy; }
				set { var p = Position; Position = new m4x4(p.rot, new v4(value, p.pos.z, p.pos.w)); }
			}
			internal m4x4 PositionAtSelectionChange { get; set; }
			internal m4x4 DragStartPosition { get; set; }

			/// <summary>Raised whenever the element is moved</summary>
			public event EventHandler PositionChanged;

			/// <summary>Raised whenever the element changes size</summary>
			public event EventHandler SizeChanged;
			protected void RaiseSizeChanged() { SizeChanged.Raise(this, EventArgs.Empty); }

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
				Text = text;
				Style = style;

				// Set the node size
				AutoSize = autosize;
				SizeMax = AutoSize ? new v2(width, height) : v2.Zero;
				var sz = PreferredSize(SizeMax);
				if (width  == 0) width  = (uint)sz.x;
				if (height == 0) height = (uint)sz.y;

				Init(width, height);
			}
			protected Node(XElement node)
				:base(node)
			{
				// Set the node text and style
				Text     = node.Element("text").As<string>();
				Style    = new NodeStyle();
				Style.Id = node.Element("style").As<Guid>();

				// Set the node size
				AutoSize = node.Element("autosize").As<bool>();
				SizeMax  = node.Element("sizemax").As<v2>();
				var size = node.Element("size").As<v2>();
				var sz = PreferredSize(SizeMax);
				if (size.x == 0) size.x = sz.x;
				if (size.y == 0) size.y = sz.y;

				Init((uint)size.x, (uint)size.y);
			}
			private void Init(uint width, uint height)
			{
				Connectors = new BindingListEx<Connector>();

				// Create the surface texture and assign it to the graphics object
				Surf = new View3d.Texture(TextureScale * width, TextureScale * height, new View3d.TextureOptions(true){Filter=View3d.EFilter.D3D11_FILTER_ANISOTROPIC});// D3D11_FILTER_MIN_MAG_MIP_LINEAR});//D3D11_FILTER_MIN_LINEAR_MAG_POINT_MIP_LINEAR});

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
				base.ToXml(node);
				node.Add2("autosize" ,AutoSize ,false);
				node.Add2("sizemax"  ,SizeMax  ,false);
				node.Add2("size"     ,Size     ,false);
				node.Add2("text"     ,Text     ,false);
				node.Add2("style"    ,Style.Id ,false);
				return node;
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
						SetSize(PreferredSize(SizeMax));

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
						SetSize(PreferredSize(SizeMax));

					Invalidate();
				}
			}
			private bool m_impl_autosize;

			/// <summary>The diagram space width/height of the node</summary>
			public virtual v2 Size
			{
				get { return Surf != null ? v2.From(Surf.Size) / TextureScale : v2.Zero; }
				set
				{
					if (AutoSize) return;
					SetSize(value);
				}
			}
			protected virtual void SetSize(v2 sz)
			{
				var tex_size = (sz * TextureScale).ToSize();
				if (Surf == null || Surf.Size == tex_size) return;
				Surf.Size = tex_size;
				RaiseSizeChanged();
				Invalidate();
			}

			/// <summary>Get/Set limits for auto size</summary>
			public virtual v2 SizeMax
			{
				get { return m_impl_sizemax; }
				set
				{
					if (m_impl_sizemax == value) return;
					m_impl_sizemax = value;

					if (AutoSize)
						SetSize(PreferredSize(SizeMax));
				}
			}
			private v2 m_impl_sizemax;

			/// <summary>Return the preferred node size given the current text and upper size bounds</summary>
			public v2 PreferredSize(v2 layout)
			{
				if (layout.x == 0f) layout.x = float.MaxValue;
				if (layout.y == 0f) layout.y = float.MaxValue;

				using (var img = new Bitmap(1,1,PixelFormat.Format32bppArgb))
				using (var gfx = System.Drawing.Graphics.FromImage(img))
				{
					v2 sz = gfx.MeasureString(Text, Style.Font, layout);
					sz.x += Style.Padding.Left + Style.Padding.Right;
					sz.y += Style.Padding.Top + Style.Padding.Bottom;
					return sz;
				}
			}
			public v2 PreferredSize(float max_width = 0f, float max_height = 0f)
			{
				return PreferredSize(new v2(max_width, max_height));
			}

			/// <summary>Get/Set the centre point of the node</summary>
			public virtual v2 Centre
			{
				get { return Bounds.Centre; }
				set { PositionXY = value + (PositionXY - Centre); }
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

			/// <summary>The connectors linked to this node</summary>
			public BindingListEx<Connector> Connectors { get; private set; }

			/// <summary>Returns the position to draw the text given the current alignment</summary>
			protected v2 TextLocation(Graphics gfx)
			{
				var sz = Size;
				sz.x -= Style.Padding.Left + Style.Padding.Right;
				sz.y -= Style.Padding.Top + Style.Padding.Bottom;
				var tx = new v2(gfx.MeasureString(Text, Style.Font, sz));
				switch (Style.TextAlign)
				{
				default: throw new ArgumentException("unknown text alignment");
				case ContentAlignment.TopLeft     : return new v2(Style.Padding.Left                    , Style.Padding.Top);
				case ContentAlignment.TopCenter   : return new v2((Size.x - tx.x) * 0.5f                , Style.Padding.Top);
				case ContentAlignment.TopRight    : return new v2((Size.x - tx.x) - Style.Padding.Right , Style.Padding.Top);
				case ContentAlignment.MiddleLeft  : return new v2(Style.Padding.Left                    , (Size.y - tx.y) * 0.5f);
				case ContentAlignment.MiddleCenter: return new v2((Size.x - tx.x) * 0.5f                , (Size.y - tx.y) * 0.5f);
				case ContentAlignment.MiddleRight : return new v2((Size.x - tx.x) - Style.Padding.Right , (Size.y - tx.y) * 0.5f);
				case ContentAlignment.BottomLeft  : return new v2(Style.Padding.Left                    , (Size.y - tx.y) - Style.Padding.Bottom);
				case ContentAlignment.BottomCenter: return new v2((Size.x - tx.x) * 0.5f                , (Size.y - tx.y) - Style.Padding.Bottom);
				case ContentAlignment.BottomRight : return new v2((Size.x - tx.x) - Style.Padding.Right , (Size.y - tx.y) - Style.Padding.Bottom);
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
				var p = DragStartPosition;
				p.pos.x += delta.x;
				p.pos.y += delta.y;
				Position = p;
				if (commit)
					DragStartPosition = Position;
			}

			/// <summary>Edit the node contents</summary>
			public virtual void Edit(DiagramControl diag)
			{
				var bounds = diag.DiagramToClient(Bounds);
				var edit = new EditField{Location = diag.PointToScreen(bounds.Location), Text = Text, Font = Style.Font};
				if (edit.ShowDialog(diag.ParentForm) == DialogResult.OK)
					Text = edit.Text.Trim();
			}

			// ToString
			public override string ToString() { return "Node (" + Text.Summary(10) + ")"; }
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
				base.ToXml(node);
				node.Add2("cnr" ,CornerRadius, false);
				return node;
			}

			/// <summary>The radius of the box node corners</summary>
			public float CornerRadius
			{
				get { return m_impl_corner_radius; }
				set { m_impl_corner_radius = value; Invalidate(); }
			}
			private float m_impl_corner_radius;

			/// <summary>AABB for the element in diagram space</summary>
			public override BRect Bounds { get { return new BRect(Position.pos.xy, Size / 2); } }

			/// <summary>Detect resize and update the graphics as needed</summary>
			protected override void SetSize(v2 sz)
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
					ldr.Append("*Rect node {3 ",Size.x," ",Size.y," ",Ldr.Solid()," ",Ldr.CornerRadius(CornerRadius),"}\n");
					Graphics.UpdateModel(ldr.ToString(), new View3d.Object.Keep{Transform = true});
					Graphics.SetTexture(Surf);
					m_size_changed = false;
				}

				using (var tex = Surf.LockSurface())
				{
					tex.Gfx.ScaleTransform(TextureScale, TextureScale);

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
					var x = new []{0f};
					var y = new []{0f};
					//var x = new []{-Size.x/2f + CornerRadius, 0f, Size.x/2f - CornerRadius};
					//var y = new []{-Size.y/2f + CornerRadius, 0f, Size.y/2f - CornerRadius};
					for (int i = 0; i != x.Length; ++i)
					{
						yield return new NodeAnchor(this, new v4(+Size.x/2f, y[i], 0, 1), +v4.XAxis);
						yield return new NodeAnchor(this, new v4(-Size.x/2f, y[i], 0, 1), -v4.XAxis);
						yield return new NodeAnchor(this, new v4(x[i], +Size.y/2f, 0, 1), +v4.YAxis);
						yield return new NodeAnchor(this, new v4(x[i], -Size.y/2f, 0, 1), -v4.YAxis);
					}
				}
			}
		}

		/// <summary>Style attributes for nodes</summary>
		public class NodeStyle :IHasId
		{
			public static readonly NodeStyle Default = new NodeStyle();

			/// <summary>Unique id for the style</summary>
			public Guid Id { get; internal set; }

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
					Id        = Guid.NewGuid();
					Border    = Color.Black;
					Fill      = Color.WhiteSmoke;
					Text      = Color.Black;
					TextAlign = ContentAlignment.MiddleCenter;
					Font      = new Font(FontFamily.GenericSansSerif, 12f, GraphicsUnit.Point);
					Padding   = new Padding(10,10,10,10);
				}
			}
			public NodeStyle(XElement node)
			{
				Id        = node.Element("id"      ).As<Guid>();
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
				node.Add2("id"       ,Id        ,false);
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
				:this(node0, node1, string.Empty)
			{}
			public Connector(Node node0, Node node1, string label)
				:this(node0, node1, label, ConnectorStyle.Default)
			{}
			public Connector(Node node0, Node node1, string label, ConnectorStyle style)
				:base(m4x4.Translation(AttachCentre(node0, node1)))
			{
				NodeAnchor anc0, anc1;
				AttachPoints(node0, node1, out anc0, out anc1);
				Anc0  = anc0;
				Anc1  = anc1;
				Label = label;
				Style = style;
				Init();
			}
			public Connector(XElement node)
				:base(node)
			{
				Anc0     = node.Element("anc0" ).As<NodeAnchor>();
				Anc1     = node.Element("anc1" ).As<NodeAnchor>();
				Label    = node.Element("label").As<string>();
				Style    = new ConnectorStyle();
				Style.Id = node.Element("style").As<Guid>();
				Init();
			}
			private void Init()
			{
				// Update the graphics object
				Render();

				Anc0.Elem.SizeChanged += Relink;
				Anc1.Elem.SizeChanged += Relink;
				Anc0.Elem.PositionChanged += Invalidate;
				Anc1.Elem.PositionChanged += Invalidate;
				Style.StyleChanged += Invalidate;

				AllowInvalidate();
			}
			public override void Dispose()
			{
				Anc0.Elem.SizeChanged -= Relink;
				Anc1.Elem.SizeChanged -= Relink;
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
				base.ToXml(node);
				node.Add2("anc0"  ,Anc0    , false);
				node.Add2("anc1"  ,Anc1    , false);
				node.Add2("label" ,Label   , false);
				node.Add2("style" ,Style.Id, false);
				return node;
			}

			/// <summary>Returns the nearest anchor points on two nodes</summary>
			private static void AttachPoints(Node node0, Node node1, out NodeAnchor anc0, out NodeAnchor anc1)
			{
				anc0 = node0.NearestAnchor(m4x4.InverseFast(node0.Position) * new v4(node1.Centre, 0f, 1f));
				anc1 = node1.NearestAnchor(m4x4.InverseFast(node1.Position) * new v4(node0.Centre, 0f, 1f));
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
			public NodeAnchor Anc0
			{
				get { return m_impl_anc0; }
				private set
				{
					Debug.Assert(value.GetNode() == null || value.GetNode() != Anc1.GetNode(), "Don't allow connections between anchors of the same node");
					var n0 = Anc0.GetNode();
					var n1 = value.GetNode();
					if (n0 != null && n0 != n1) n0.Connectors.Remove(this);
					if (n1 != null && n0 != n1) n1.Connectors.Add(this);
					m_impl_anc0 = value;
				}
			}
			private NodeAnchor m_impl_anc0;

			/// <summary>The 'to' node</summary>
			public NodeAnchor Anc1
			{
				get { return m_impl_anc1; }
				private set
				{
					Debug.Assert(value.GetNode() == null || value.GetNode() != Anc0.GetNode(), "Don't allow connections between anchors of the same node");
					var n0 = Anc1.GetNode();
					var n1 = value.GetNode();
					if (n0 != null && n0 != n1) n0.Connectors.Remove(this);
					if (n1 != null && n0 != n1) n1.Connectors.Add(this);
					m_impl_anc1 = value;
				}
			}
			private NodeAnchor m_impl_anc1;

			/// <summary>The 'from' node</summary>
			public Node Node0
			{
				get { return Anc0.GetNode(); }
			}

			/// <summary>The 'to' node</summary>
			public Node Node1
			{
				get { return Anc1.GetNode(); }
			}

			/// <summary>A string label for the connector</summary>
			public virtual string Label
			{
				get { return m_impl_label; }
				set
				{
					if (m_impl_label == value) return;
					m_impl_label = value;
					Invalidate();
				}
			}
			private string m_impl_label;

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
				Graphics.O2P = m4x4.Translation(AttachCentre(Anc0, Anc1));
			}

			/// <summary>Returns the other connected node</summary>
			internal Node OtherNode(Node node)
			{
				if (Anc0.GetNode() == node)
					return Anc1.GetNode();
				if (Anc1.GetNode() == node)
					return Anc0.GetNode();
				throw new Exception("{0} is not connected to {1}".Fmt(ToString(), node.ToString()));
			}

			/// <summary>Update the node anchors</summary>
			public void Relink(bool find_previous_anchors)
			{
				if (find_previous_anchors)
				{
					Anc0 = Anc0.Elem.As<Node>().NearestAnchor(Anc0.Location);
					Anc1 = Anc1.Elem.As<Node>().NearestAnchor(Anc1.Location);
				}
				else
				{
					NodeAnchor anc0,anc1;
					AttachPoints(Anc0.Elem.As<Node>(), Anc1.Elem.As<Node>(), out anc0, out anc1);
					Anc0 = anc0;
					Anc1 = anc1;
				}
				Invalidate();
			}
			public void Relink(object sender = null, EventArgs args = null)
			{
				Relink(true);
			}

			// ToString
			public override string ToString() { return "Connector (" + Anc0.ToString() + "-" + Anc1.ToString() + ")"; }
		}

		/// <summary>Style properties for connectors</summary>
		public class ConnectorStyle :IHasId
		{
			public static readonly ConnectorStyle Default = new ConnectorStyle();

			/// <summary>Unique id for the style</summary>
			public Guid Id { get; internal set; }

			/// <summary>Connector styles</summary>
			[Flags] public enum EType
			{
				Line         = 1 << 0,
				ForwardArrow = 1 << 1,
				BackArrow    = 1 << 2,
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
					Id       = Guid.NewGuid();
					Type     = EType.Line;
					Line     = Color.Black;
					Selected = Color.Blue;
					Width    = 5f;
				}
			}
			public ConnectorStyle(XElement node)
			{
				Id       = node.Element("id"      ).As<Guid>();
				Type     = node.Element("type"    ).As<EType>();
				Line     = node.Element("line"    ).As<Color>();
				Selected = node.Element("selected").As<Color>();
				Width    = node.Element("width"   ).As<float>();
			}

			/// <summary>Export to xml</summary>
			public XElement ToXml(XElement node)
			{
				node.Add2("id"       ,Id       ,false);
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

			public override string ToString() { return "Anchor (" + Elem.ToString() + ")"; }
		}

		/// <summary>Selection data for a mouse button</summary>
		private class MouseSelection
		{
			public bool          m_btn_down;   // True while the corresponding mouse button is down
			public Point         m_grab_cs;    // The client space location of where the diagram was "grabbed"
			public RectangleF    m_selection;  // Area selection, has width, height of zero when the user isn't selecting
			public HitTestResult m_hit_result; // The hit test result on mouse down
		}

		/// <summary>Helper form for editing the value in a node</summary>
		private class EditField :Form
		{
			private readonly TextBox m_field;
			public override string Text
			{
				get { return m_field != null ? m_field.Text : string.Empty; }
				set { if (m_field != null) m_field.Text = value; }
			}

			public EditField()
			{
				ShowInTaskbar   = false;
				FormBorderStyle = FormBorderStyle.None;
				KeyPreview      = true;
				Margin          = Padding.Empty;
				StartPosition   = FormStartPosition.Manual;

				m_field           = new TextBox
				{
					Multiline = true,
					MinimumSize = new Size(80,30),
					MaximumSize = new Size(640,480),
					BorderStyle = BorderStyle.None,
					Margin = Padding.Empty,
				};

				Controls.Add(m_field);
			}
			private void DoAutoSize()
			{
				var sz              = m_field.PreferredSize;
				sz.Width           += 1;
				sz.Height          += m_field.Font.Height + 2;
				Size                = sz;
				m_field.Size        = sz;
				Size                = m_field.Size;
				m_field.ScrollBars  = m_field.Size != sz ? ScrollBars.Both : ScrollBars.None;
			}
			protected override void OnShown(EventArgs e)
			{
				DoAutoSize();
 				base.OnShown(e);
			}
			protected override void OnKeyDown(KeyEventArgs e)
			{
				if (e.KeyCode == Keys.Escape)
				{
					DialogResult = DialogResult.Cancel;
					Close();
					return;
				}
				if (e.KeyCode == Keys.Return && e.Control)
				{
					DialogResult = DialogResult.OK;
					Close();
					return;
				}
				DoAutoSize();
				base.OnKeyDown(e);
			}
		}

		/// <summary>A cache of node or connector style objects</summary>
		private class StyleCache<T> where T:IHasId
		{
			private readonly Dictionary<Guid, T> m_map = new Dictionary<Guid, T>();
			private readonly NodeStyle m_default = new NodeStyle();

			public StyleCache() {}
			public StyleCache(XElement node)
			{
				foreach (var n in node.Elements())
				{
					var style = n.As<T>();
					m_map.Add(style.Id, style);
				}
			}
			public XElement ToXml(XElement node)
			{
				foreach (var ns in m_map)
					node.Add2("style", ns.Value, false);
				return node;
			}

			/// <summary>Returns the style associated with 'style.Id'summary>
			internal T this[T style]
			{
				get
				{
					// 'style' may be a default constructed node style with just the id changed
					// This lookup tries to find the correct style for the id. If not found, then
					// 'style' is added to the cache.
					T out_style;
					if (m_map.TryGetValue(style.Id, out out_style))
						return out_style;

					m_map.Add(style.Id, style);
					return style;
				}
			}
		}

		///// <summary>Graphics objects used by the diagram</summary>
		//private class Tools :IDisposable
		//{
		//	/// <summary>A rectangular selection box</summary>
		//	public View3d.Object m_selection_box;

		//	public Tools()
		//	{
		//		var ldr = new LdrBuilder();
		//		using (ldr.Group("selection_box"))
		//		{
		//		}

		//	}
		//	public void Dispose()
		//	{
		//	}
		//}

		#endregion

		#region Options

		/// <summary>Rendering options</summary>
		public class DiagramOptions
		{
			// Colours for graph elements
			public Color m_bg_colour      = SystemColors.ControlDark;      // The fill colour of the background
			public Color m_title_colour   = Color.Black;                   // The colour of the title text
			public Color m_grid_colour    = Color.FromArgb(230, 230, 230); // The colour of the grid lines

			// Graph margins and constants
			public class NodeOptions
			{
				public float Margin = 30f;
				public bool AutoRelink = true;
				
				public NodeOptions() {}
				public NodeOptions(XElement node)
				{
					Margin = node.Element("margin").As<float>();
					AutoRelink = node.Element("auto_relink").As<bool>();
				}
				public XElement ToXml(XElement node)
				{
					node.Add2("margin", Margin, false);
					node.Add2("auto_relink", AutoRelink, false);
					return node;
				}
			}
			public NodeOptions Node = new NodeOptions();

			//public float m_left_margin    = 0.0f; // Fractional distance from the left edge
			//public float m_right_margin   = 0.0f;
			//public float m_top_margin     = 0.0f;
			//public float m_bottom_margin  = 0.0f;
			//public float m_title_top      = 0.01f; // Fractional distance  down from the top of the client area to the top of the title text
			//public Font  m_title_font     = new Font("tahoma", 12, FontStyle.Bold);    // Font to use for the title text
			//public Font  m_note_font      = new Font("tahoma",  8, FontStyle.Regular); // Font to use for graph notes
			
			// Scatter parameters
			public class ScatterOptions
			{
				public int   MaxIterations = 20;
				public float SpringConstant = 0.01f;
				public float CoulombConstant = 1000f;
				public float ConnectorScale = 1f;
				public float Equilibrium = 0.01f;

				public ScatterOptions() {}
				public ScatterOptions(XElement node)
				{
					MaxIterations   = node.Element("iterations"      ).As<int>();
					SpringConstant  = node.Element("spring_constant" ).As<float>();
					CoulombConstant = node.Element("coulomb_constant").As<float>();
					ConnectorScale  = node.Element("connector_scale" ).As<float>();
					Equilibrium     = node.Element("equilibrium"     ).As<float>();
				}
				public XElement ToXml(XElement node)
				{
					node.Add2("iterations"       ,MaxIterations   ,false);
					node.Add2("spring_constant"  ,SpringConstant  ,false);
					node.Add2("coulomb_constant" ,CoulombConstant ,false);
					node.Add2("connector_scale"  ,ConnectorScale  ,false);
					node.Add2("equilibrium"      ,Equilibrium     ,false);
					return node;
				}
			}
			public ScatterOptions Scatter = new ScatterOptions();

			public DiagramOptions() {}
			public DiagramOptions(XElement node)
			{
				Node    = node.Element("node"   ).As<NodeOptions>();
				Scatter = node.Element("scatter").As<ScatterOptions>();
			}
			public XElement ToXml(XElement node)
			{
				node.Add2("node"    ,Node    ,false);
				node.Add2("scatter" ,Scatter ,false);
				return node;
			}
			public DiagramOptions Clone() { return (DiagramOptions)MemberwiseClone(); }
		}

		#endregion

		// Members
		private readonly View3d            m_view3d;           // Renderer
		private readonly EventBatcher      m_eb_update_diag;   // Event batcher for updating the diagram graphics
		private View3d.CameraControls      m_camera;           // The virtual window over the diagram
		private StyleCache<NodeStyle>      m_node_styles;      // The collection of node styles
		private StyleCache<ConnectorStyle> m_connector_styles; // The collection of node styles
		private MouseSelection[]           m_mbutton;          // Per button mouse selection data
		private bool                       m_edited;           // True when the diagram has been edited and requires saving

		public DiagramControl() :this(new DiagramOptions()) {}
		private DiagramControl(DiagramOptions options)
		{
			if (this.IsInDesignMode()) return;

			m_impl_options     = options ?? new DiagramOptions();
			m_view3d           = new View3d(Handle, Render);
			m_eb_update_diag   = new EventBatcher(UpdateDiagram);
			m_camera           = new View3d.CameraControls(m_view3d.Drawset);
			m_node_styles      = new StyleCache<NodeStyle>();
			m_connector_styles = new StyleCache<ConnectorStyle>();
			m_mbutton          = Util.NewArray<MouseSelection>(Enum<EBtnIdx>.Count);
			m_edited           = false;

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
			AllowSelection = true;
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
		
		/// <summary>Get/Set whether DiagramChanged events are raised</summary>
		public bool RaiseEvents
		{
			get { return !DiagramChanged.IsSuspended(); }
			set { DiagramChanged.Suspend(!value); }
		}
		public Scope SuspendEvents()
		{
			return Scope.Create(
				() => RaiseEvents = false,
				() => RaiseEvents = true);
		}

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
		public DiagramOptions Options
		{
			get { return m_impl_options; }
			set
			{
				m_impl_options = value ?? new DiagramOptions();
				UpdateDiagram(true);
			}
		}
		private DiagramOptions m_impl_options;

		/// <summary>Minimum bounding area for view reset</summary>
		public BRect ResetMinBounds { get { return new BRect(v2.Zero, new v2(Width/1.5f, Height/1.5f)); } }

		/// <summary>Used to control z order</summary>
		private float ElementZ { get { return m_impl_element_z += 0.0001f; } }
		private float m_impl_element_z = 0.1f;

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

			// Sort the results by type then z order
			result.Hits.Sort((l,r) =>
				{
					if (l.Entity != r.Entity) return l.Entity.CompareTo(r.Entity);
					return l.Element.Position.pos.z.CompareTo(r.Element.Position.pos.z);
				});
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
				public override string ToString() { return Element.ToString(); }
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

		/// <summary>True if users are allowed to select elements on the diagram</summary>
		public bool AllowSelection { get; set; }

		/// <summary>Default Mouse navigation. Public to allow users to forward mouse calls to us.</summary>
		public void OnMouseDown(object sender, MouseEventArgs e)
		{
			// Get the mouse selection data for the mouse button
			var sel                  = m_mbutton[(int)View3d.ButtonIndex(e.Button)];
			sel.m_btn_down           = true;
			sel.m_grab_cs            = e.Location;
			sel.m_selection.Location = ClientToDiagram(sel.m_grab_cs);
			sel.m_selection.Size     = v2.Zero;
			sel.m_hit_result         = HitTest(sel.m_selection.Location);

			foreach (var elem in Selected)
				elem.DragStartPosition = elem.Position;

			RaiseEvents = false;
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
				RaiseEvents = true;
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
						DragSelected(ClientToDiagram(e.Location) - sel.m_selection.Location, true);
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
						sel.m_selection.Size = ClientToDiagram(e.Location) - v2.From(sel.m_selection.Location);
					else
						DragSelected(ClientToDiagram(e.Location) - sel.m_selection.Location, false);
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

		/// <summary>Handle double clicks</summary>
		protected override void OnMouseDoubleClick(MouseEventArgs e)
		{
			base.OnMouseDoubleClick(e);
			if (!AllowEditing)
				return;

			// Find what was hit
			var hit = HitTest(ClientToDiagram(e.Location));
			var elem = hit.Hits.FirstOrDefault();
			if (elem == null)
				return;

			switch (elem.Entity)
			{
			default: throw new Exception("Unknown diagram element");
			case Entity.Node: elem.Element.As<Node>().Edit(this); break;
			case Entity.Connector: break;
			case Entity.Label: break;
			}
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

		/// <summary>Returns a point in diagram space from a point in client space. Use to convert mouse (client-space) locations to diagram coordinates</summary>
		public v2 ClientToDiagram(Point point)
		{
			var ws = m_camera.WSPointFromSSPoint(point);
			return new v2(ws.x, ws.y);
		}

		/// <summary>Returns a rectangle diagram space from a rectangle in client space.</summary>
		public BRect DiagramToClient(Rectangle rect)
		{
			var r = BRect.From(rect);
			r.Centre = ClientToDiagram(r.Centre.ToPoint());
			r.Radius = ClientToDiagram(r.Radius.ToPoint()) - r.Centre;
			return r;
		}

		/// <summary>Returns a point in client space from a point in diagram space. Inverse of ClientToDiagram</summary>
		public Point DiagramToClient(v2 point)
		{
			var ws = new v4(point, 0.0f, 1.0f);
			return m_camera.SSPointFromWSPoint(ws);
		}

		/// <summary>Returns a rectangle in client space from a rectangle in diagram space. Inverse of ClientToDiagram</summary>
		public Rectangle DiagramToClient(BRect rect)
		{
			rect.Centre = v2.From(DiagramToClient(rect.Centre));
			rect.Radius = v2.Abs(v2.From(DiagramToClient(rect.Radius)) - v2.From(DiagramToClient(v2.Zero)));
			return rect.ToRectangle();
		}

		/// <summary>Shifts the camera so that diagram space position 'ds' is at client space position 'cs'</summary>
		public void PositionDiagram(Point cs, v2 ds)
		{
			// Dragging the diagram is the same as shifting the camera in the opposite direction
			var dst = ClientToDiagram(cs);
			m_camera.Navigate(ds.x - dst.x, ds.y - dst.y, 0);
			Refresh();
		}

		/// <summary>Move the selected elements by 'delta'</summary>
		private void DragSelected(v2 delta, bool commit)
		{
			if (!AllowMove) return;
			foreach (var elem in Selected)
				elem.Drag(delta, commit);
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
					// An element is about to be added, attach handlers.
					// If the new element is already in this list, remove it first (the list is being reordered)
					var elem = args.Item;
					if (elem.Diagram == this) elem.Diagram = null;
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
				{
					// When an element is saved to xml, it only saves the style id.
					// When loaded from xml, a default style is created with the id changed
					// to that from the xml data. When added to a diagram, we use the style id
					// to get the correct style for that id. (also with the side effect of adding
					// new styles to the diagram)
					var node = args.Item as Node;
					if (node != null)
						node.Style = m_node_styles[node.Style];

					var conn = args.Item as Connector;
					if (conn != null)
						conn.Style = m_connector_styles[conn.Style];

					// Update the diagram
					m_eb_update_diag.Signal();
					break;
				}
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
			var node = sender as Node;
			if (node != null && Options.Node.AutoRelink)
				foreach (var conn in node.Connectors)
					conn.Relink(false);

			m_edited = true;
			Refresh();
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
			if (!AllowSelection)
				return;

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

					var sel = hits.Hits.FirstOrDefault();
					if (sel != null)
						sel.Element.Selected = true;
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

		/// <summary>Selects nodes that are connected to already selected nodes</summary>
		public void SelectConnectedNodes()
		{
			if (!AllowSelection)
				return;

			var nodes = Selected.Where(x => x.Entity == Entity.Node).Cast<Node>().ToList();
			foreach (var node in nodes)
			foreach (var conn in node.Connectors)
				conn.OtherNode(node).Selected = true;

			Refresh();
		}

		/// <summary>Spread the nodes on the diagram so none are overlapping</summary>
		public void ScatterNodes()
		{
			// Find all nodes and connectors involved in the scatter
			bool selected = Selected.Count != 0;
			var nodes = (selected ? Selected : Elements).Where(x => x.Entity == Entity.Node).Cast<Node>().ToList();
			var conns = nodes.SelectMany(x => x.Connectors).Where(x => x.Node0.Selected == selected && x.Node1.Selected == selected).Distinct().ToList();

			// Prevent issues with nodes exactly on top of each other
			var rand = new Rand();
			var djitter = 10f;
			Func<v2> jitter = () => v2.Random2(-djitter, djitter, rand);

			// Finds the minimum separation distance between to nodes for a given direction
			Func<v2,Node,Node,float> min_separation = (v,n0,n1) =>
				{
					var sz = n1.Bounds.Size + n0.Bounds.Size;
					return (Math.Abs(v.x / sz.x) > Math.Abs(v.y / sz.y) ? sz.x : sz.y) + Options.Node.Margin;
				};

			var qtree = new QuadTree<Node>(ContentBounds);
			var force = new Dictionary<Node,v2>();
			var last  = new Dictionary<Node,v2>();

			// Initialise the forces
			foreach (var node in nodes)
			{
				force[node] = last[node] = v2.Zero;
				node.PositionXY += jitter();
			}

			// Spring-mass + Coulomb field simulation
			for (int i = 0; i != Options.Scatter.MaxIterations; ++i)
			{
				// Build a quad tree of the nodes
				foreach (var node in nodes)
					qtree.Insert(node, node.Bounds.Centre, node.Bounds.Diametre/2);

				// Determine spring forces
				for (int j = 0; j != conns.Count; ++j)
				{
					var conn = conns[j];
					var node0 = conn.Node0;
					var node1 = conn.Node1;
				
					// Find the minimum separation and the current separation
					var vec     = node1.PositionXY - node0.PositionXY;
					var min_sep = min_separation(vec, node0, node1);
					var sep     = Math.Max(min_sep, vec.Length2);
					
					// Spring force F = -Kx
					var spring = -Options.Scatter.SpringConstant * (float)Math.Max(0, sep - min_sep);
					
					var f = vec * (spring / sep);
					force[node0] -= f;
					force[node1] += f;
				}

				// Determine coulomb forces
				for (int j = 0; j != nodes.Count; ++j)
				{
					var node0 = nodes[j];

					// Nodes are more repulsive if they have more connections
					var q0 = 1f + node0.Connectors.Count * Options.Scatter.ConnectorScale;

					// Find nearby nodes and apply forces
					qtree.Traverse(node0.PositionXY, node0.Bounds.Diametre, node1 =>
						{
							// Find the minimum separation and the current separation
							var vec     = node1.PositionXY - node0.PositionXY;
							var min_sep = min_separation(vec, node0, node1);
							var sep     = Math.Max(min_sep, vec.Length2);
					
							// Coulomb force F = kQq/xÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â²
							var q1 = 1f + node1.Connectors.Count * Options.Scatter.ConnectorScale;
							var coulumb = Options.Scatter.CoulombConstant * q0 * q1 / (sep * sep);

							var f = vec * (coulumb / sep);
							force[node0] -= f;
							force[node1] += f;
							return true;
						});
				}

				// Apply forces... sort of, don't simulate acceleration/velocity
				bool equilibrium = true;
				for (int j = 0; j != nodes.Count; ++j)
				{
					var node = nodes[j];
					var frc = force[node];
					var lst = last[node];

					equilibrium &= (frc - lst).Length2Sq < Options.Scatter.Equilibrium;

					// Limit the magnitude of the position change
					var force_lensq = frc.Length2Sq;
					var bound_lensq = node.Bounds.DiametreSq;
					var frc_limited = force_lensq > bound_lensq
						? frc * (float)Math.Sqrt(bound_lensq / force_lensq)
						: frc;

					node.PositionXY += frc_limited;

					// Reset the forces
					last[node] = frc;
					force[node] = v2.Zero;
				}
				if (equilibrium)
					break;
			}

			if (Options.Node.AutoRelink)
				RelinkNodes();
			Refresh();
		}

		/// <summary>Update the links between nodes to pick the nearest anchor points</summary>
		public void RelinkNodes()
		{
			// Relink the connectors from selected nodes or all connectors if nothing is selected
			var connectors = Selected.Count != 0
				? Selected.Where(x => x.Entity == Entity.Node).Cast<Node>().SelectMany(x => x.Connectors)
				: Elements.Where(x => x.Entity == Entity.Connector).Cast<Connector>();

			foreach (var connector in connectors)
				connector.Relink(false);
			Refresh();
		}

		/// <summary>
		/// Removes and re-adds all elements to the diagram.
		/// Should only be used when the elements collection is modifed, otherwise use Refresh()</summary>
		private void UpdateDiagram(bool invalidate_all)
		{
			var elements = new BindingListEx<Element>(Elements);
			
			m_view3d.Drawset.RemoveAllObjects();
			foreach (var elem in elements)
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
			var marginL = 0;//(int)(target_size.Width  * Options.m_left_margin  );
			var marginT = 0;//(int)(target_size.Height * Options.m_top_margin   );
			var marginR = 0;//(int)(target_size.Width  * Options.m_right_margin );
			var marginB = 0;//(int)(target_size.Height * Options.m_bottom_margin);

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
			if (m_in_refresh) return; // Prevent reentrancy
			using (Scope.Create(() => m_in_refresh = true, () => m_in_refresh = false))
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
		}
		private bool m_in_refresh;

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
			m_view3d.Present();
		}

		/// <summary>Export the current diagram as xml</summary>
		public XElement ExportXml(XElement node)
		{
			using (Elements.SuspendEvents())
				Elements.Sort((l,r) => l.Entity.CompareTo(r.Entity));

			node.Add2("node_styles", m_node_styles ,false);
			node.Add2("connector_styles", m_connector_styles, false);
			foreach (var elem in Elements)
				node.Add2("elem", elem, true);
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
			{
				switch (node.Name.LocalName)
				{
				case "node_styles":      m_node_styles = node.As<StyleCache<NodeStyle>>(); break;
				case "connector_styles": m_connector_styles = node.As<StyleCache<ConnectorStyle>>(); break;
				case "elem":             Elements.Add((Element)node.ToObject()); break;
				}
			}

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
		public event EventHandler<AddUserMenuOptionsEventArgs> AddUserMenuOptions;
		public class AddUserMenuOptionsEventArgs :EventArgs
		{
			public ContextMenuStrip Menu { get; private set; }
			public AddUserMenuOptionsEventArgs(ContextMenuStrip menu) { Menu = menu; }
		}

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
				AddUserMenuOptions.Raise(this, new AddUserMenuOptionsEventArgs(context_menu));

				if (AllowEditing)
				{
				}

				if (AllowMove)
				{
					lvl0.Add(new ToolStripSeparator());

					#region Scatter
					var scatter = lvl0.Add2(new ToolStripMenuItem("Scatter"));
					scatter.Click += (s,a) => ScatterNodes();
					#endregion

					#region Relink
					var relink = lvl0.Add2(new ToolStripMenuItem("Relink"));
					relink.Click += (s,a) => RelinkNodes();
					#endregion
				}

				if (AllowSelection)
				{
					lvl0.Add(new ToolStripSeparator());

					#region Selection
					var select_connected = lvl0.Add2(new ToolStripMenuItem("Select Connected Nodes"));
					select_connected.Click += (s,a) =>
						{
							SelectConnectedNodes();
						};
					#endregion
				}
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

	internal static class DiagramControlExtn
	{
		/// <summary>Helper for getting the node from a NodeAnchor</summary>
		internal static DiagramControl.Node GetNode(this DiagramControl.NodeAnchor anc) { return anc != null ? anc.Elem.As<DiagramControl.Node>() : null; }
	}
}
