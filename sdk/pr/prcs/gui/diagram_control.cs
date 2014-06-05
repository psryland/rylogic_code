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
		// Notes:
		// - The diagram control doesn't *own* the elements so should never
		//  call dispose on them.
		// - Prefer direct calls on the diagram from an element rather than
		//  having the diagram watch for events... Events can be suspended.

		/// <summary>The types of elements that can be on a diagram</summary>
		public enum Entity { Node, Connector, Label, }

		/// <summary>Says "I have a guid"</summary>
		private interface IHasId { Guid Id { get; } }

		/// <summary>Marker interface for element styles</summary>
		public interface IStyle
		{
			/// <summary>Raised whenever a style property is changed</summary>
			event EventHandler StyleChanged;
		}

		/// <summary>For elements that have an associated IStyle object</summary>
		public interface IHasStyle
		{
			IStyle Style { get; }
		}

		#region Elements

		/// <summary>Base class for anything on a diagram</summary>
		public abstract class Element :IDisposable
		{
			protected Element(Guid id, m4x4 position)
			{
				m_diag             = null;
				Id                 = id;
				m_impl_position    = position;
				m_impl_selected    = false;
				m_impl_visible     = true;
			}
			protected Element(XElement node)
			{
				Diagram  = null;
				Id       = node.Element(XmlField.Id).As<Guid>();
				Position = node.Element(XmlField.Position).As<m4x4>();
				Visible  = true;
			}
			public virtual void Dispose()
			{
				Diagram = null;
				Invalidated = null;
				PositionChanged = null;
				DataChanged = null;
				SelectedChanged = null;
			}

			/// <summary>Get the entity type for this element</summary>
			public abstract Entity Entity { get; }

			/// <summary>Non-null when the element has been added to a diagram</summary>
			public DiagramControl Diagram
			{
				get { return m_diag; }
				set { SetDiagramInternal(value, true); }
			}
			internal virtual void SetDiagramInternal(DiagramControl diag, bool mod_elements_collection)
			{
				// There are two ways of adding an element to a diagram; adding to the
				// diagrams Elements collection, or assigning this property. To handle
				// this we set the diagram property to the new value, then call Add/Remove
				// on the diagrams elements container. The Add/Remove will be ignored
				// if the Diagram property already has the correct value
				if (m_diag == diag) return;

				// Remove this element from any selected collection on the outgoing diagram
				Selected = false;

				// Detach from the old diagram
				if (m_diag != null && mod_elements_collection)
					m_diag.Elements.Remove(this);

				// Assign the new diagram
				m_diag = diag;

				// Attach to the new diagram
				if (m_diag != null && mod_elements_collection)
					m_diag.Elements.Add(this);
			}
			private DiagramControl m_diag;

			/// <summary>Unique id for this element</summary>
			public Guid Id { get; private set; }

			/// <summary>Export to xml</summary>
			public virtual XElement ToXml(XElement node)
			{
				node.Add2(XmlField.Id       ,Id       ,false);
				node.Add2(XmlField.Position ,Position ,false);
				return node;
			}
			protected virtual void FromXml(XElement node)
			{
				Position = node.Element(XmlField.Position).As<m4x4>(Position);
			}

			/// <summary>Replace the contents of this element with data from 'node'</summary>
			internal void Update(XElement node)
			{
			//	if (node.Attribute(XmlField.TypeAttribute).Value != GetType().FullName)
			//		throw new Exception("Must update a diagram element with xml data for the same type");

				using (SuspendEvents())
					FromXml(node);

				Invalidate();
			}

			/// <summary>Indicate a render is needed. Note: doesn't call Render</summary>
			public void Invalidate(object sender = null, EventArgs args = null)
			{
				if (Dirty) return;
				Dirty = true;
				Invalidated.Raise(this, EventArgs.Empty);
			}

			/// <summary>Dirty flag = need to render</summary>
			private bool Dirty { get { return m_impl_dirty; } set { m_impl_dirty = value; } }
			private bool m_impl_dirty;

			/// <summary>Get/Set whether events from this element are suspended or not</summary>
			public virtual bool RaiseEvents
			{
				get { return Invalidated.IsSuspended(); }
				set
				{
					var suspend = !value;
					Invalidated.Suspend(suspend);
					DataChanged.Suspend(suspend);
					SelectedChanged.Suspend(suspend);
					PositionChanged.Suspend(suspend);
					SizeChanged.Suspend(suspend);
				}
			}

			/// <summary>RAII object for suspending events on this node</summary>
			public Scope SuspendEvents()
			{
				return Scope.Create(() => RaiseEvents = false, () => RaiseEvents = true);
			}

			/// <summary>Raised whenever the element needs to be redrawn</summary>
			public event EventHandler Invalidated;

			/// <summary>Raised whenever data associated with the element changes</summary>
			public event EventHandler DataChanged;
			protected virtual void RaiseDataChanged()
			{
				if (Diagram != null) Diagram.Edited = true;
				DataChanged.Raise(this, EventArgs.Empty);
			}

			/// <summary>Raised whenever the element is moved</summary>
			public event EventHandler PositionChanged;

			/// <summary>Raised whenever the element changes size</summary>
			public event EventHandler SizeChanged;
			protected void RaiseSizeChanged() { SizeChanged.Raise(this, EventArgs.Empty); }

			/// <summary>Raised whenever the element is selected or deselected</summary>
			public event EventHandler SelectedChanged;

			/// <summary>Get/Set the selected state</summary>
			public virtual bool Selected
			{
				get { return m_impl_selected; }
				set
				{
					if (m_impl_selected == value) return;
					m_impl_selected = value;
					Invalidate();

					// Update our selected state in the diagram's selected collection
					// Don't use the event for this as events can be suspended
					if (m_diag != null)
					{
						if (value) m_diag.Selected.Add(this);
						else m_diag.Selected.Remove(this);
					}

					// Record the diagram location at the time we were selected/deselected
					PositionAtSelectionChange = Position;

					// Notify observers about the selection change
					SelectedChanged.Raise(this, EventArgs.Empty);
				}
			}
			private bool m_impl_selected;

			/// <summary>Get/Set whether this node is visible in the diagram</summary>
			public bool Visible
			{
				get { return m_impl_visible; }
				set
				{
					if (m_impl_visible == value) return;
					m_impl_visible = value;
				}
			}
			private bool m_impl_visible;

			/// <summary>Send this element to the bottom of the stack</summary>
			public void SendToBack()
			{
				// Z order is determined by position in the Elements collection
				if (m_diag == null) return;

				// Save the diagram pointer because removing
				// this element will remove it from the Diagram
				var diag = m_diag;
				diag.Elements.Remove(this);
				diag.Elements.Insert(0, this);
				diag.UpdateElementZOrder();
				diag.Refresh();
			}

			/// <summary>Bring this element to top of the stack</summary>
			public void BringToFront()
			{
				// Z order is determined by position in the Elements collection
				if (m_diag == null) return;

				// Save the diagram pointer because removing
				// this element will remove it from the Diagram
				var diag = m_diag;
				diag.Elements.Remove(this);
				diag.Elements.Add(this);
				diag.UpdateElementZOrder();
				diag.Refresh();
			}

			/// <summary>The element to diagram transform</summary>
			public m4x4 Position
			{
				get { return m_impl_position; }
				set
				{
					if (Equals(m_impl_position, value)) return;
					SetPosition(value);
				}
			}

			/// <summary>Get/Set the XY position of the element</summary>
			public v2 PositionXY
			{
				get { return Position.pos.xy; }
				set { var o2p = Position; Position = new m4x4(o2p.rot, new v4(value, o2p.pos.z, o2p.pos.w)); }
			}

			/// <summary>Get/Set the z position of the element</summary>
			public float PositionZ
			{
				get { return Position.pos.z; }
				internal set { var o2p = Position; o2p.pos.z = value; Position = o2p; }
			}

			/// <summary>Position recorded at the time of selection/deselection</summary>
			internal m4x4 PositionAtSelectionChange { get; set; }

			/// <summary>Position recorded at the time dragging starts</summary>
			internal m4x4 DragStartPosition { get; set; }

			/// <summary>Internal set position and raise event</summary>
			protected virtual void SetPosition(m4x4 pos)
			{
				m_impl_position = pos;
				if (Diagram != null) Diagram.Edited = true;
				PositionChanged.Raise(this, EventArgs.Empty);
			}
			private m4x4 m_impl_position;

			/// <summary>Get/Set the centre point of the element (in diagram space)</summary>
			public v2 Centre
			{
				get { return Bounds.Centre; }
				set { PositionXY = value + (PositionXY - Centre); }
			}

			/// <summary>AABB for the element in diagram space</summary>
			public virtual BRect Bounds { get { return new BRect(PositionXY, v2.Zero); } }

			/// <summary>Perform a hit test on this object. Returns null for no hit. 'point' is in diagram space</summary>
			public virtual HitTestResult.Hit HitTest(v2 point, View3d.CameraControls cam) { return null; }

			/// <summary>Handle a click event on this element</summary>
			internal virtual bool HandleClicked(HitTestResult.Hit hit, View3d.CameraControls cam)
			{
				return false;
			}

			/// <summary>Drag the element 'delta' from the DragStartPosition</summary>
			public virtual void Drag(v2 delta, bool commit)
			{
				var p = DragStartPosition;
				p.pos.x += delta.x;
				p.pos.y += delta.y;
				Position = p;
				if (commit)
					DragStartPosition = Position;
			}

			/// <summary>Return the attachment location and normal nearest to 'pt'.</summary>
			public AnchorPoint NearestAnchor(v2 pt, bool pt_in_element_space)
			{
				var point = new v4(pt, PositionZ, 1f);
				if (!pt_in_element_space)
					point = m4x4.InverseFast(Position) * point;

				return AnchorPoints().MinBy(x => (x.Location - point).Length3Sq);
			}

			/// <summary>Return all the locations that connectors can attach to this node (in node space)</summary>
			public virtual IEnumerable<AnchorPoint> AnchorPoints()
			{
				yield return new AnchorPoint(this, v4.Origin, v4.Zero);
			}

			/// <summary>Update the graphics and object transforms associated with this element</summary>
			public void Refresh(bool force = false)
			{
				if (!Dirty && !force) return;  // Only if dirty
				if (m_impl_refreshing) return; // Protect against reentrancy
				using (Scope.Create(() => m_impl_refreshing = true, () => m_impl_refreshing = false))
				{
					RefreshInternal();
					Dirty = false;
				}
			}
			protected virtual void RefreshInternal() {}
			private bool m_impl_refreshing;

			/// <summary>Add the graphics associated with this element to the drawset</summary>
			internal void AddToDrawset(View3d.DrawsetInterface drawset) { AddToDrawsetInternal(drawset); }
			protected virtual void AddToDrawsetInternal(View3d.DrawsetInterface drawset) {}

			/// <summary>Edit the element contents</summary>
			public virtual void Edit()
			{
				// Get the user control used to edit the node
				var editing_control = EditingControl();
				if (editing_control == null)
					return; // not editable

				// Create a form to host the control
				var edit = new EditField(editing_control)
				{
					Bounds = Diagram.RectangleToScreen(Diagram.DiagramToClient(Bounds)),
				};

				// Display modally
				if (edit.ShowDialog(Diagram.ParentForm) == DialogResult.OK)
					editing_control.Commit();
			}

			/// <summary>Gets a control to use for editing the element. Return null if not editable</summary>
			protected virtual EditingControl EditingControl()
			{
				return null;
			}

			/// <summary>Check the self consistency of this element</summary>
			public virtual bool CheckConsistency()
			{
				return true;
			}
		}

		/// <summary>Base class for a rectangular resizable element</summary>
		public abstract class ResizeableElement :Element
		{
			/// <summary>Base node constructor</summary>
			/// <param name="id">Globally unique id for the element</param>
			/// <param name="autosize">If true, the width and height are maximum size limits (0 meaning unlimited)</param>
			/// <param name="width">The width of the node, or if 'autosize' is true, the maximum width of the node</param>
			/// <param name="height">The height of the node, or if 'autosize' is true, the maximum height of the node</param>
			/// <param name="position">The position of the node on the diagram</param>
			protected ResizeableElement(Guid id, bool autosize, uint width, uint height, m4x4 position)
				:base(id, position)
			{
				// Set the node size
				m_impl_autosize = autosize;
				m_impl_sizemax = AutoSize ? new v2(width, height) : v2.Zero;
				m_impl_size = new v2(width, height);
				m_size_changed = true;
			}
			protected ResizeableElement(XElement node)
				:base(node)
			{
				// Set the node size
				m_impl_autosize = node.Element(XmlField.AutoSize).As<bool>();
				m_impl_sizemax = node.Element(XmlField.SizeMax).As<v2>();
				m_impl_size = node.Element(XmlField.Size).As<v2>();
				m_size_changed = true;
			}

			/// <summary>Export to xml</summary>
			public override XElement ToXml(XElement node)
			{
				base.ToXml(node);
				node.Add2(XmlField.AutoSize ,AutoSize ,false);
				node.Add2(XmlField.SizeMax  ,SizeMax  ,false);
				node.Add2(XmlField.Size     ,Size     ,false);
				return node;
			}
			protected override void FromXml(XElement node)
			{
				base.FromXml(node);
				AutoSize = node.Element(XmlField.AutoSize).As<bool>(AutoSize);
				SizeMax  = node.Element(XmlField.SizeMax).As<v2>(SizeMax);
				Size     = node.Element(XmlField.Size).As<v2>(Size);
			}

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
				get { return m_impl_size; }
				set
				{
					if (AutoSize) return;
					SetSize(value);
				}
			}
			protected virtual void SetSize(v2 sz)
			{
				m_impl_size = sz;
				m_size_changed = true;
				RaiseSizeChanged();
				Invalidate();
			}
			private v2 m_impl_size;
			
			/// <summary>Size-changed dirty flag for derived elements</summary>
			protected bool m_size_changed;

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

			/// <summary>Returns the preferred size of the element</summary>
			protected virtual v2 PreferredSize(v2 layout)
			{
				return Size;
			}

			/// <summary>AABB for the element in diagram space</summary>
			public override BRect Bounds { get { return new BRect(PositionXY, Size / 2); } }
		}

		#endregion

		#region Nodes

		/// <summary>Base class for all node types</summary>
		public abstract class Node :ResizeableElement ,IHasStyle
		{
			/// <summary>Base node constructor</summary>
			/// <param name="id">Globally unique id for the element</param>
			/// <param name="autosize">If true, the width and height are maximum size limits (0 meaning unlimited)</param>
			/// <param name="width">The width of the node, or if 'autosize' is true, the maximum width of the node</param>
			/// <param name="height">The height of the node, or if 'autosize' is true, the maximum height of the node</param>
			/// <param name="text">The text of the node</param>
			/// <param name="position">The position of the node on the diagram</param>
			/// <param name="style">Style properties for the node</param>
			protected Node(Guid id, bool autosize, uint width, uint height, string text, m4x4 position, NodeStyle style)
				:base(id, autosize, width, height, position)
			{
				m_impl_text = text;
				m_impl_style = style;

				Init();
			}
			protected Node(XElement node)
				:base(node)
			{
				m_impl_text = node.Element(XmlField.Text).As<string>();
				m_impl_style = new NodeStyle{Id = node.Element(XmlField.Style).As<Guid>()};

				Init();
			}
			private void Init()
			{
				Connectors = new BindingListEx<Connector>();
				Style.StyleChanged += Invalidate;
			}
			public override void Dispose()
			{
				// Remove this node from the connectors
				while (!Connectors.Empty())
					Connectors.First().Remove(this);

				Style = null;
				base.Dispose();
			}

			/// <summary>Get the entity type for this element</summary>
			public override Entity Entity { get { return Entity.Node; } }

			/// <summary>Export to xml</summary>
			public override XElement ToXml(XElement node)
			{
				base.ToXml(node);
				node.Add2(XmlField.Text     ,Text     ,false);
				node.Add2(XmlField.Style    ,Style.Id ,false);
				return node;
			}
			protected override void FromXml(XElement node)
			{
				base.FromXml(node);
				Text = node.Element(XmlField.Text).As<string>(Text);
			}

			/// <summary>The connectors linked to this node</summary>
			public BindingListEx<Connector> Connectors { get; private set; }

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

			/// <summary>Return the preferred node size given the current text and upper size bounds</summary>
			protected override v2 PreferredSize(v2 layout)
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
			protected v2 PreferredSize(float max_width = 0f, float max_height = 0f)
			{
				return PreferredSize(new v2(max_width, max_height));
			}

			/// <summary>Style attributes for the node</summary>
			public virtual NodeStyle Style
			{
				get { return m_impl_style; }
				set
				{
					if (m_impl_style == value) return;
					if (m_impl_style != null) m_impl_style.StyleChanged -= Invalidate;
					m_impl_style = value;
					if (m_impl_style != null) m_impl_style.StyleChanged += Invalidate;
					Invalidate();
				}
			}
			private NodeStyle m_impl_style;
			IStyle IHasStyle.Style { get { return Style; } }

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

			/// <summary>Gets a control to use for editing the element. Return null if not editable</summary>
			protected override EditingControl EditingControl()
			{
				var tb = new TextBox
					{
						Multiline = true,
						MinimumSize = new Size(80,30),
						MaximumSize = new Size(640,480),
						BorderStyle = BorderStyle.None,
						Margin = Padding.Empty,
						Text = Text,
					};
				Action<Form> autosize = form =>
					{
						var sz         = tb.PreferredSize;
						sz.Width      += 1;
						sz.Height     += tb.Font.Height + 2;
						form.Size      = sz;
						tb.Size        = sz;
						form.Size      = tb.Size;
						tb.ScrollBars  = tb.Size != sz ? ScrollBars.Both : ScrollBars.None;
					};
				Action commit = () =>
					{
						Text = tb.Text.Trim();
					};
				return new EditingControl(tb, autosize, commit);
			}

			/// <summary>Update the connectors attached to this node</summary>
			public void Relink(bool find_previous_anchors)
			{
				foreach (var c in Connectors)
					c.Relink(find_previous_anchors);
			}

			/// <summary>Assign this element to a diagram</summary>
			internal override void SetDiagramInternal(DiagramControl diag, bool mod_elements_collection)
			{
				base.SetDiagramInternal(diag, mod_elements_collection);

				// When an element is saved to xml, it only saves the style id.
				// When loaded from xml, a default style is created with the id changed
				// to that from the xml data. When added to a diagram, we use the style id
				// to get the correct style for that id. (also with the side effect of adding
				// new styles to the diagram)
				if (diag != null)
					Style = diag.m_node_styles[Style];
			}

			/// <summary>Check the self consistency of this element</summary>
			public override bool CheckConsistency()
			{
				// The Connectors collection in the node should contain a connector that references this node exactly once
				foreach (var conn in Connectors)
				{
					if (conn.Node0 != this && conn.Node1 != this)
						throw new Exception("Node {0} contains connector {1} but is not referenced by the connector".Fmt(ToString(), conn.ToString()));
					if (conn.Node0 == this && conn.Node1 == this)
						throw new Exception("Node {0} contains connector {1} that is attached to it at both ends".Fmt(ToString(), conn.ToString()));
				}
				return base.CheckConsistency();
			}

			// ToString
			public override string ToString() { return "Node (" + Text.Summary(10) + ")"; }
		}

		/// <summary>An invisible 'node-like' object used for detaching/attaching connectors</summary>
		public class NodeProxy :Node
		{
			private readonly AnchorPoint m_anchor_point;

			public NodeProxy()
				:base(Guid.NewGuid(), false, 20, 20, "ProxyNode", m4x4.Identity, NodeStyle.Default)
			{
				m_anchor_point = new AnchorPoint(this, v4.Origin, v4.Zero);
			}

			/// <summary>The normal of the sole anchor point on this node. v4.Zero is valid</summary>
			public v4 AnchorNormal
			{
				get { return m_anchor_point.Normal; }
				set
				{
					if (m_anchor_point.Normal == value) return;
					m_anchor_point.Normal = value;
					if (Connectors.Count != 0)
						Connectors[0].Anc(this).Normal = value;
				}
			}

			/// <summary>Perform a hit test on this object. Returns null for no hit. 'point' is in diagram space</summary>
			public override HitTestResult.Hit HitTest(v2 point, View3d.CameraControls cam)
			{
				var pt = point - Centre;
				if (pt.Length2Sq > Maths.Sqr(Bounds.Diametre/2f))
					return null;

				var hit = new HitTestResult.Hit(this, pt);
				return hit;
			}

			/// <summary>Return all the locations that connectors can attach to on this element</summary>
			public override IEnumerable<AnchorPoint> AnchorPoints()
			{
				yield return m_anchor_point;
			}

			/// <summary>Update the graphics and object transforms associated with this element</summary>
			protected override void RefreshInternal()
			{}

			/// <summary>Add the graphics associated with this element to the drawset</summary>
			protected override void AddToDrawsetInternal(View3d.DrawsetInterface drawset)
			{}
		}

		/// <summary>Simple rectangular box node</summary>
		public class BoxNode :Node
		{
			private const string LdrName = "node";

			/// <summary>Only one graphics object for a box node</summary>
			private readonly TexturedShape<QuadShape> m_gfx;

			public BoxNode()
				:this(Guid.NewGuid())
			{}
			public BoxNode(Guid id)
				:this(id, "", true, 0, 0)
			{}
			public BoxNode(string text)
				:this(Guid.NewGuid(), text)
			{}
			public BoxNode(Guid id, string text)
				:this(id, text, true, 0, 0)
			{}
			public BoxNode(Guid id, string text, bool autosize, uint width, uint height)
				:this(id, text, autosize, width, height, Color.WhiteSmoke, Color.Black, 5f)
			{}
			public BoxNode(Guid id, string text, bool autosize, uint width, uint height, Color bkgd, Color border, float corner_radius)
				:this(id, text, autosize, width, height, bkgd, border, corner_radius, m4x4.Identity, NodeStyle.Default)
			{}
			public BoxNode(Guid id, string text, bool autosize, uint width, uint height, Color bkgd, Color border, float corner_radius, m4x4 position, NodeStyle style)
				:base(id, autosize, width, height, text, position, style)
			{
				m_gfx = new TexturedShape<QuadShape>(new QuadShape(corner_radius), Size);
				Init();
			}
			public BoxNode(XElement node)
				:base(node)
			{
				var corner_radius = node.Element(XmlField.CornerRadius).As<float>();
				m_gfx = new TexturedShape<QuadShape>(new QuadShape(corner_radius), Size);
				Init();
			}
			private void Init()
			{
				if (AutoSize)
					SetSize(PreferredSize(SizeMax));
			}
			public override void Dispose()
			{
				if (m_gfx != null) m_gfx.Dispose();
				base.Dispose();
			}

			/// <summary>Export to xml</summary>
			public override XElement ToXml(XElement node)
			{
				base.ToXml(node);
				node.Add2(XmlField.CornerRadius ,CornerRadius, false);
				return node;
			}
			protected override void FromXml(XElement node)
			{
				base.FromXml(node);
				CornerRadius = node.Element(XmlField.CornerRadius).As<float>(CornerRadius);
			}

			/// <summary>The radius of the box node corners</summary>
			public float CornerRadius
			{
				get { return m_gfx.Shape.CornerRadius; }
				set { m_gfx.Shape.CornerRadius = value; Invalidate(); }
			}

			/// <summary>Set the position of the element</summary>
			protected override void SetPosition(m4x4 pos)
			{
				base.SetPosition(pos);
				if (m_gfx != null) m_gfx.O2P = pos;
			}

			/// <summary>Change the size of the node</summary>
			protected override void SetSize(v2 sz)
			{
				base.SetSize(sz);
				m_gfx.Size = sz;
			}

			/// <summary>Perform a hit test on this element. Returns null for no hit. 'point' is in diagram space</summary>
			public override HitTestResult.Hit HitTest(v2 point, View3d.CameraControls cam)
			{
				var bounds = Bounds;
				if (!bounds.IsWithin(point)) return null;
				point -= bounds.Centre;

				var hit = new HitTestResult.Hit(this, point);
				return hit;
			}

			/// <summary>Return all the locations that connectors can attach to on this element</summary>
			public override IEnumerable<AnchorPoint> AnchorPoints()
			{
				const float units_per_anchor = 10f;
				
				// Get the dimensions and half dimensions
				var sz = Size;
				var hsx = sz.x/2f - CornerRadius - units_per_anchor;
				var hsy = sz.y/2f - CornerRadius - units_per_anchor;

				// Find how many anchors will fit along x
				var divx = Math.Max((int)(hsx / units_per_anchor), 1);
				var divy = Math.Max((int)(hsy / units_per_anchor), 1);

				for (int i = -divx; i <= divx; ++i)
				{
					var x = hsx * i / divx;
					var y = sz.y / 2f;
					yield return new AnchorPoint(this, new v4(x, +y, 0, 1), +v4.YAxis);
					yield return new AnchorPoint(this, new v4(x, -y, 0, 1), -v4.YAxis);
				}
				for (int i = -divy; i <= divy; ++i)
				{
					var x = sz.x / 2f;
					var y = hsy * i / divy;
					yield return new AnchorPoint(this, new v4(+x, y, 0, 1), +v4.XAxis);
					yield return new AnchorPoint(this, new v4(-x, y, 0, 1), -v4.XAxis);
				}
			}

			/// <summary>Update the graphics and object transforms associated with this element</summary>
			protected override void RefreshInternal()
			{
				// Update the graphics model when the size changes
				m_gfx.Refresh();

				// Update the position
				m_gfx.O2P = Position;

				// Update the texture surface
				using (var tex = m_gfx.LockSurface())
				{
					var rect = Bounds.ToRectangle();
					rect = rect.Shifted(-rect.X, -rect.Y).Inflated(-1,-1);

					tex.Gfx.Clear(Selected ? Style.Selected : Style.Fill);
					using (var bsh = new SolidBrush(Style.Text))
						tex.Gfx.DrawString(Text, Style.Font, bsh, TextLocation(tex.Gfx));
				}
			}

			/// <summary>Add the graphics associated with this element to the drawset</summary>
			protected override void AddToDrawsetInternal(View3d.DrawsetInterface drawset)
			{
				drawset.AddObject(m_gfx);
			}
		}

		#endregion

		#region Labels
		
		/// <summary>Base class for all labels</summary>
		public abstract class Label :Element
		{
			// Labels are attached to elements and move as child objects
			public Label()
				:this(Guid.NewGuid())
			{}
			public Label(Guid id)
				:this(id, m4x4.Identity)
			{}
			public Label(Guid id, m4x4 position)
				:base(id, position)
			{
				m_anc = new AnchorPoint();
			}
			public Label(XElement node)
				:base(node)
			{
				m_anc = new AnchorPoint();
				Anc = node.Element(XmlField.Anchor).As<AnchorPoint>();
			}

			/// <summary>Get the entity type for this element</summary>
			public override Entity Entity
			{
				get { return Entity.Label; }
			}

			/// <summary>Export to xml</summary>
			public override XElement ToXml(XElement node)
			{
				base.ToXml(node);
				node.Add2(XmlField.Anchor, Anc, false);
				return node;
			}
			protected override void FromXml(XElement node)
			{
				base.FromXml(node);
				Anc = node.Element(XmlField.Anchor).As<AnchorPoint>(Anc);
			}

			/// <summary>Disable selection for labels</summary>
			public override bool Selected { get { return false; } set { } }
	
			/// <summary>The anchor point that the label is attached to</summary>
			public AnchorPoint Anc
			{
				get { return m_anc; }
				set
				{
					Elem = value.Elem;
					m_anc.Location = value.Location;
					m_anc.Normal = value.Normal;
				}
			}
			private readonly AnchorPoint m_anc;

			/// <summary>The element that the label is attached to</summary>
			public Element Elem
			{
				get { return Anc.Elem; }
				set
				{
					if (Anc.Elem == value) return;
					if (Elem != null)
					{
						Elem.PositionChanged -= HandleElementMoved;
						if (Elem is IHasStyle) ((IHasStyle)Elem).Style.StyleChanged -= HandleElementMoved;
						Diagram = null;
					}
					Anc.Elem = value;
					if (Elem != null)
					{
						Diagram = Elem.Diagram;
						if (Elem is IHasStyle) ((IHasStyle)Elem).Style.StyleChanged += HandleElementMoved;
						Elem.PositionChanged += HandleElementMoved;
					}
				}
			}

			/// <summary>Update the anchor position whenever the parent element moves</summary>
			private void HandleElementMoved(object sender = null, EventArgs args = null)
			{
				// Need to call update on the anchor because the anchor can move on the parent element
				Anc.Update(Elem, true);
				Position = m4x4.Translation(Anc.LocationDS.xy, PositionZ);
				Invalidate();
			}

			/// <summary>Edit the label contents</summary>
			protected override EditingControl EditingControl()
			{
				return null;
			}
		}

		/// <summary>Base class for labels containing a texture</summary>
		public class TexturedLabel<TShape> :Label where TShape :IShape, new()
		{
			private readonly TexturedShape<TShape> m_gfx;

			public TexturedLabel()
				:this(new TShape(), 0U, 0U)
			{}
			public TexturedLabel(TShape shape, uint sx, uint sy)
				:this(Guid.NewGuid(), shape, sx, sy)
			{}
			public TexturedLabel(Guid id, TShape shape, uint sx, uint sy)
				:this(id, shape, sx, sy, m4x4.Identity)
			{}
			public TexturedLabel(Guid id, TShape shape, uint sx, uint sy, m4x4 position)
				:base(id, position)
			{
				m_gfx = new TexturedShape<TShape>(shape, new v2(sx, sy));
			}
			public override void Dispose()
			{
				if (m_gfx != null) m_gfx.Dispose();
				base.Dispose();
			}

			/// <summary>Get/Set the size of the element</summary>
			public v2 Size
			{
				get { return m_gfx.Size; }
				set { m_gfx.Size = value; Invalidate(); }
			}

			/// <summary>AABB for the element in diagram space</summary>
			public override BRect Bounds
			{
				get { return new BRect(Position.pos.xy, Size / 2); }
			}

			/// <summary>Access the graphics object</summary>
			protected View3d.Object Gfx { get { return m_gfx; } }

			/// <summary>The surface to draw on for the node</summary>
			protected View3d.Texture.Lock LockSurface()
			{
				return m_gfx.LockSurface();
			}

			/// <summary>Update the graphics and object transforms associated with this element</summary>
			protected override void RefreshInternal()
			{
				m_gfx.Refresh();
				m_gfx.O2P = Position;
			}

			/// <summary>Perform a hit test on this element. Returns null for no hit. 'point' is in diagram space</summary>
			public override HitTestResult.Hit HitTest(v2 point, View3d.CameraControls cam)
			{
				var bounds = Bounds;
				if (!bounds.IsWithin(point)) return null;
				point -= bounds.Centre;

				var hit = new HitTestResult.Hit(this, point);
				return hit;
			}

			/// <summary>Add the graphics associated with this element to the drawset</summary>
			protected override void AddToDrawsetInternal(View3d.DrawsetInterface drawset)
			{
				drawset.AddObject(m_gfx);
			}
		}

		#endregion

		#region Connectors

		/// <summary>A base class for connectors between elements</summary>
		public class Connector :Element ,IHasStyle
		{
			/// <summary>Connector type</summary>
			[Flags] public enum EType
			{
				Line    = 1 << 0,
				Forward = 1 << 1,
				Back    = 1 << 2,
				BiDir   = Forward|Back,
			}
			private static readonly v4 Bias = new v4(0,0,0.001f,0);

			/// <summary>Graphics for the connector line</summary>
			private View3d.Object m_gfx_line;

			/// <summary>Graphics for the forward arrow</summary>
			private View3d.Object m_gfx_fwd;

			/// <summary>Graphics for the backward arrow</summary>
			private View3d.Object m_gfx_bak;

			/// <summary>
			/// Controls how the connector is positioned relative to the
			/// mid point between Anc0.LocationDS and Anc1.LocationDS</summary>
			private v4 m_centre_offset;

			public Connector()
				:this(Guid.NewGuid(), null, null)
			{}
			public Connector(Node node0, Node node1)
				:this(Guid.NewGuid(), node0, node1)
			{}
			public Connector(Guid id, Node node0, Node node1)
				:this(id, node0, node1, ConnectorStyle.Default)
			{}
			public Connector(Guid id, Node node0, Node node1, ConnectorStyle style)
				:base(id, m4x4.Translation(AttachCentre(node0, node1), 0f))
			{
				m_anc0 = new AnchorPoint();
				m_anc1 = new AnchorPoint();
				m_centre_offset = v4.Zero;
				Style = style;
				Label = null;
				Node0 = node0;
				Node1 = node1;
				
				Init(false);
			}
			public Connector(XElement node)
				:base(node)
			{
				m_anc0          = new AnchorPoint();
				m_anc1          = new AnchorPoint();
				m_centre_offset = node.Element(XmlField.CentreOffset).As<v4>(v4.Zero);
				Anc0            = node.Element(XmlField.Anchor0).As<AnchorPoint>();
				Anc1            = node.Element(XmlField.Anchor1).As<AnchorPoint>();
				Type            = node.Element(XmlField.Type).As<EType>(EType.Line);
				Style           = new ConnectorStyle{Id = node.Element(XmlField.Style).As<Guid>(ConnectorStyle.Default.Id)};
				Label           = node.Element(XmlField.Label).ToObject().As<Label>();

				Init(true);
			}
			private void Init(bool find_previous_anchors)
			{
				// Create graphics for the connector
				m_gfx_line = new View3d.Object("*Group{}");
				m_gfx_fwd = new View3d.Object("*Triangle conn_fwd FFFFFFFF {1.5 0 0  -0.5 +1.2 0  -0.5 -1.2 0}");
				m_gfx_bak = new View3d.Object("*Triangle conn_bak FFFFFFFF {1.5 0 0  -0.5 +1.2 0  -0.5 -1.2 0}");
				
				Relink(find_previous_anchors);
				Refresh(true);
			}
			public override void Dispose()
			{
				Node0 = null;
				Node1 = null;
				Style = null;
				if (m_gfx_line != null) m_gfx_line.Dispose();
				if (m_gfx_fwd  != null) m_gfx_fwd.Dispose();
				if (m_gfx_bak  != null) m_gfx_bak.Dispose();
				m_gfx_line = null;
				m_gfx_fwd  = null;
				m_gfx_bak  = null;
				base.Dispose();
			}

			/// <summary>Get the entity type for this element</summary>
			public override Entity Entity { get { return Entity.Connector; } }

			/// <summary>Export to xml</summary>
			public override XElement ToXml(XElement node)
			{
				base.ToXml(node);
				node.Add2(XmlField.Anchor0 ,Anc0    , false);
				node.Add2(XmlField.Anchor1 ,Anc1    , false);
				node.Add2(XmlField.Type    ,Type    , false);
				node.Add2(XmlField.Style   ,Style.Id, false);
				node.Add2(XmlField.Label   ,Label   , true );
				return node;
			}
			protected override void FromXml(XElement node)
			{
				base.FromXml(node);
				var id   = node.Element(XmlField.Style).As<Guid>(Style.Id);
				Style    = id != Style.Id ? new ConnectorStyle{Id = id} : Style;
				Label    = node.Element(XmlField.Label).ToObject(Label).As<Label>();
				Anc0     = node.Element(XmlField.Anchor0).As<AnchorPoint>(Anc0);
				Anc1     = node.Element(XmlField.Anchor1).As<AnchorPoint>(Anc1);
			}

			/// <summary>The 'from' anchor</summary>
			public AnchorPoint Anc0
			{
				get { return m_anc0; }
				set
				{
					Node0 = value.Elem.As<Node>();
					m_anc0.Location = value.Location;
					m_anc0.Normal = value.Normal;
				}
			}
			private readonly AnchorPoint m_anc0;

			/// <summary>The 'to' anchor</summary>
			public AnchorPoint Anc1
			{
				get { return m_anc1; }
				set
				{
					Node1 = value.Elem.As<Node>();
					m_anc1.Location = value.Location;
					m_anc1.Normal = value.Normal;
				}
			}
			private readonly AnchorPoint m_anc1;

			/// <summary>Get the anchor point associated with 'node'</summary>
			public AnchorPoint Anc(Node node)
			{
				return
					Node0 == node ? Anc0 :
					Node1 == node ? Anc1 :
					null;
			}

			/// <summary>The 'from' node</summary>
			public Node Node0
			{
				get { return Anc0.Elem.As<Node>(); }
				set
				{
					Debug.Assert(Node1 == null || Node1 != value, "Don't allow connections between anchors on the same element");
					if (Anc0.Elem == value) return;
					if (Node0 != null)
					{
						Node0.PositionChanged -= Relink;
						Node0.Connectors.Remove(this);
					}
					Anc0.Elem = value;
					if (Node0 != null)
					{
						Node0.Connectors.Add(this);
						Node0.PositionChanged += Relink;
					}
				}
			}

			/// <summary>The 'to' node</summary>
			public Node Node1
			{
				get { return Anc1.Elem.As<Node>(); }
				set
				{
					Debug.Assert(Node0 == null || Node0 != value, "Don't allow connections between anchors on the same element");
					if (Anc1.Elem == value) return;
					if (Node1 != null)
					{
						Node1.PositionChanged -= Relink;
						Node1.Connectors.Remove(this);
					}
					Anc1.Elem = value;
					if (Node1 != null)
					{
						Node1.Connectors.Add(this);
						Node1.PositionChanged += Relink;
					}
				}
			}

			/// <summary>Get/Set the node</summary>
			internal Node Node(bool to)
			{
				return to ? Node1 : Node0;
			}
			internal void Node(bool to, Node node)
			{
				if (to)
					Node1 = node;
				else
					Node0 = node;
			}

			/// <summary>True if the connector is not attached at both ends</summary>
			public bool Dangling { get { return Node0 == null || Node1 ==  null; } }

			/// <summary>True at least one end of the connector is attached to a node</summary>
			public bool Attached { get { return Node0 != null || Node1 !=  null; } }

			/// <summary>The connector type</summary>
			public EType Type
			{
				get { return m_impl_type; }
				set
				{
					if (m_impl_type == value) return;
					m_impl_type = value;
					Invalidate();
				}
			}
			private EType m_impl_type;

			/// <summary>A label graphic for the connector</summary>
			public virtual Label Label
			{
				get { return m_impl_label; }
				set
				{
					if (m_impl_label == value) return;
					m_impl_label = value;
					Invalidate();
				}
			}
			private Label m_impl_label;

			/// <summary>Style attributes for the connector</summary>
			public virtual ConnectorStyle Style
			{
				get { return m_impl_style; }
				set
				{
					if (m_impl_style == value) return;
					if (m_impl_style != null) m_impl_style.StyleChanged -= Invalidate;
					m_impl_style = value;
					if (m_impl_style != null) m_impl_style.StyleChanged += Invalidate;
					Invalidate();
				}
			}
			private ConnectorStyle m_impl_style;
			IStyle IHasStyle.Style { get { return Style; } }

			/// <summary>Get/Set the selected state</summary>
			public override bool Selected
			{
				get { return base.Selected; }
				set
				{
					if (Selected == value) return;
					base.Selected = value;
					if (m_gfx_line != null) m_gfx_line.SetColour(Selected ? Style.Selected : Style.Line);
					if (m_gfx_fwd  != null) m_gfx_fwd .SetColour(Selected ? Style.Selected : Style.Line);
					if (m_gfx_bak  != null) m_gfx_bak .SetColour(Selected ? Style.Selected : Style.Line);
					UpdatePositions();
				}
			}

			/// <summary>Set the position of the element</summary>
			protected override void SetPosition(m4x4 pos)
			{
				base.SetPosition(pos);
				UpdatePositions();
			}
			private void UpdatePositions()
			{
				// Raise selected objects above others
				var bias = (Selected ? new v4(0,0,1,0) : v4.Zero) + Bias;
				
				// Set the line transform
				if (m_gfx_line != null)
					m_gfx_line.O2P = new m4x4(Position.rot, Position.pos + bias);

				// If the connector has a back arrow, add the arrow head graphics
				if (m_gfx_bak != null && (Type & EType.Back) != 0)
				{
					var dir = -Anc0.NormalDS;
					if (dir == v4.Zero) dir = Anc1.NormalDS;
					if (dir == v4.Zero) dir = v4.Normalise3(Anc0.LocationDS - Anc1.LocationDS, v4.YAxis);
					var pos = new v4(Anc0.LocationDS.xy, Math.Max(Anc0.LocationDS.z, PositionZ), 1f);
					m_gfx_bak.O2P = m4x4.Rotation(v4.ZAxis, (float)Math.Atan2(dir.y, dir.x), pos + bias) * m4x4.Scale(Style.Width, v4.Origin);
				}

				// If the connector has a forward arrow, add the arrow head graphics
				if (m_gfx_fwd != null && (Type & EType.Forward) != 0)
				{
					var dir = -Anc1.NormalDS;
					if (dir == v4.Zero) dir = Anc0.NormalDS;
					if (dir == v4.Zero) dir = v4.Normalise3(Anc1.LocationDS - Anc0.LocationDS, v4.YAxis);
					var pos = new v4(Anc1.LocationDS.xy, Math.Max(Anc1.LocationDS.z, PositionZ), 1f);
					m_gfx_fwd.O2P = m4x4.Rotation(v4.ZAxis, (float)Math.Atan2(dir.y, dir.x), pos + bias) * m4x4.Scale(Style.Width, v4.Origin);
				}
			}

			/// <summary>AABB for the element in diagram space</summary>
			public override BRect Bounds
			{
				get
				{
					var bounds = BRect.Reset;
					bounds.Encompass(Points(true));
					return bounds;
				}
			}

			/// <summary>Perform a hit test on this object. Returns null for no hit. 'point' is in diagram space</summary>
			public override HitTestResult.Hit HitTest(v2 point, View3d.CameraControls cam)
			{
				var points = Points(true);
				var dist_sq = float.MaxValue;
				var closest_pt = v2.Zero;
				if (Style.Smooth && points.Length > 2)
				{
					// Smooth connectors convert the points to splines
					foreach (var spline in Spline.CreateSplines(points.Select(x => new v4(x,0,1))))
					{
						// Find the closest point to the spline
						var t = Geometry.ClosestPoint(spline, new v4(point,0,1));
						var pt = spline.Position(t);
						var dsq = (point - pt.xy).Length2Sq;
						if (dsq < dist_sq)
						{
							dist_sq = dsq;
							closest_pt = pt.xy;
						}
					}
				}
				else
				{
					for (int i = 0; i < points.Length - 1; ++i)
					{
						var t = Geometry.ClosestPoint(points[i], points[i+1], point);
						var pt = v2.Lerp(points[i], points[i+1], t);
						var dsq = (point - pt).Length2Sq;
						if (dsq < dist_sq)
						{
							dist_sq = dsq;
							closest_pt = pt;
						}
					}
				}

				// Convert separating distance screen space
				var dist_cs = cam.SSVecFromWSVec(closest_pt, point);
				if (dist_cs.Length2Sq > MinCSSelectionDistanceSq) return null;
				return new HitTestResult.Hit(this, closest_pt - Position.pos.xy);
			}

			/// <summary>Handle a click event on this element</summary>
			internal override bool HandleClicked(HitTestResult.Hit hit, View3d.CameraControls cam)
			{
				// Only respond if selected and editing is allowed
				if (Selected && Diagram.AllowEditing)
				{
					// Hit point in screen space
					var hit_point_ds = Position.pos + new v4(hit.Point,0,0);
					var hit_point_cs = v2.From(cam.SSPointFromWSPoint(hit_point_ds));
					var anc0_cs = v2.From(cam.SSPointFromWSPoint(Anc0.LocationDS));
					var anc1_cs = v2.From(cam.SSPointFromWSPoint(Anc1.LocationDS));

					// If the click was at the ends of the connector and diagram editing
					// is allowed, detach the connector and start a move link mouse op
					if ((hit_point_cs - anc0_cs).Length2Sq < DiagramControl.MinCSSelectionDistanceSq)
					{
						Diagram.m_mouse_op[(int)EBtnIdx.None] = new MouseOpMoveLink(Diagram, this, false);
						return true;
					}
					if ((hit_point_cs - anc1_cs).Length2Sq < DiagramControl.MinCSSelectionDistanceSq)
					{
						Diagram.m_mouse_op[(int)EBtnIdx.None] = new MouseOpMoveLink(Diagram, this, true);
						return true;
					}
				}
				return false;
			}

			/// <summary>Drag the element 'delta' from the DragStartPosition</summary>
			public override void Drag(v2 delta, bool commit)
			{
				Invalidate();
			}

			/// <summary>Return all the locations that connectors can attach to on this element</summary>
			public override IEnumerable<AnchorPoint> AnchorPoints()
			{
				var pts = Points(false);
				v4 ctr;
				if      (pts.Length == 4)                  ctr = new v4((pts[1] + pts[2]) / 2f, PositionZ, 1);
				else if (pts.Length == 3 && !Style.Smooth) ctr = new v4(pts[1], PositionZ, 1);
				else if (pts.Length == 3)                  ctr = Spline.CreateSplines(pts.Select(x => new v4(x,PositionZ,1))).First().Position(0.5f);
				else throw new Exception("unexpected number of connector points");
				yield return new AnchorPoint(this, ctr, v4.Zero);
			}

			/// <summary>Update the graphics and transforms for the connector</summary>
			protected override void RefreshInternal()
			{
				// Update the transform
				PositionXY = AttachCentre(Anc0, Anc1);

				var ldr   = new LdrBuilder();
				var col   = Selected ? Style.Selected : Dangling ? Style.Dangling : Style.Line;
				var width = Style.Width;
				var pts   = Points(false);

				// Update the connector line graphics
				ldr.Append("*Ribbon connector ",col,"{3 ",width);
				pts.ForEach(pt => ldr.Append(" ",new v4(pt,0,1)));
				if (Style.Smooth) ldr.Append(" *Smooth");
				ldr.Append("}");

				m_gfx_line.UpdateModel(ldr.ToString());

				// If the connector has a back arrow, add the arrow head graphics
				if ((Type & EType.Back) != 0)
					m_gfx_bak.SetColour(col);

				// If the connector has a forward arrow, add the arrow head graphics
				if ((Type & EType.Forward) != 0)
					m_gfx_fwd.SetColour(col);

				UpdatePositions();
			}

			/// <summary>Add the graphics associated with this element to the drawset</summary>
			protected override void AddToDrawsetInternal(View3d.DrawsetInterface drawset)
			{
				// Add the main connector line
				drawset.AddObject(m_gfx_line);

				// If the connector has a back arrow, add the arrow head graphics
				if ((Type & EType.Back) != 0)
					drawset.AddObject(m_gfx_bak);

				// If the connector has a forward arrow, add the arrow head graphics
				if ((Type & EType.Forward) != 0)
					drawset.AddObject(m_gfx_fwd);
			}

			/// <summary>Detach from 'node'</summary>
			public void Remove(Node node)
			{
				if (node == null) throw new ArgumentNullException("node");
				if (Node0 == node) { Node0 = null; return; }
				if (Node1 == node) { Node1 = null; return; }
				throw new Exception("Connector '{0}' is not connected to node {0}".Fmt(ToString(), node.ToString()));
			}

			/// <summary>Returns the other connected node</summary>
			internal Node OtherNode(Node node)
			{
				if (Node0 == node) return Node1;
				if (Node1 == node) return Node0;
				throw new Exception("{0} is not connected to {1}".Fmt(ToString(), node.ToString()));
			}

			/// <summary>Update the node anchors</summary>
			public void Relink(bool find_previous_anchors)
			{
				m_anc0.Update(m_anc1.Elem, find_previous_anchors);
				m_anc1.Update(m_anc0.Elem, find_previous_anchors);
				Invalidate();
			}
			public void Relink(object sender = null, EventArgs args = null)
			{
				var find_previous_anchors = Diagram == null || !Diagram.Options.Node.AutoRelink;
				Relink(find_previous_anchors);
			}

			/// <summary>Assign this element to a diagram</summary>
			internal override void SetDiagramInternal(DiagramControl diag, bool mod_elements_collection)
			{
				base.SetDiagramInternal(diag, mod_elements_collection);

				// When an element is saved to xml, it only saves the style id.
				// When loaded from xml, a default style is created with the id changed
				// to that from the xml data. When added to a diagram, we use the style id
				// to get the correct style for that id. (also with the side effect of adding
				// new styles to the diagram)
				if (diag != null)
					Style = diag.m_connector_styles[Style];
			}

			/// <summary>Returns the diagram space position of the centre between nearest anchor points on two nodes</summary>
			private static v2 AttachCentre(AnchorPoint anc0, AnchorPoint anc1)
			{
				v2 centre = 0.5f * (anc0.LocationDS + anc1.LocationDS).xy;
				return centre;
			}
			private static v2 AttachCentre(Node node0, Node node1)
			{
				AnchorPoint anc0, anc1;
				AttachPoints(node0, node1, out anc0, out anc1);
				return AttachCentre(anc0, anc1);
			}

			/// <summary>Returns the nearest anchor points on two nodes</summary>
			private static void AttachPoints(Node node0, Node node1, out AnchorPoint anc0, out AnchorPoint anc1)
			{
				anc0 = node0.NearestAnchor(node1.Centre, false);
				anc1 = node1.NearestAnchor(node0.Centre, false);
			}

			/// <summary>Returns the corner points of the connector from Anc0 to Anc1</summary>
			private v2[] Points(bool diagram_space)
			{
				// A connector is constructed from: Anc0.LocationDS, CentreOffset, Anc1.LocationDS.
				// A line from Anc0.LocationDS is extended in the direction of Anc0.NormalDS till it
				// is perpendicular to m_centre_offset. The same is done from Anc1.LocationDS
				// A connecting line between these points is then formed.

				var origin = diagram_space ? v4.Zero : Position.pos.w0;
				var centre = (Position.pos - origin + m_centre_offset).xy;
				var start  = (Anc0.LocationDS - origin).xy;
				var end    = (Anc1.LocationDS - origin).xy;

				v2 intersect;
				if (Geometry.Intersect(
					start, start + Anc0.NormalDS.xy,
					end  , end   + Anc1.NormalDS.xy, out intersect))
				{
					if (v2.Dot2(intersect - start, Anc0.NormalDS.xy) > 0 &&
						v2.Dot2(intersect - end  , Anc1.NormalDS.xy) > 0)
						return new[]{start, intersect, end};
				}

				var dir0 = Anc0.NormalDS.xy;
				var dir1 = Anc1.NormalDS.xy;
				if (dir0 == v2.Zero) dir0 = -dir1;
				if (dir1 == v2.Zero) dir1 = -dir0;
				if (dir0 == v2.Zero) dir0 = v2.Normalise2(end - start, v2.Zero);
				if (dir1 == v2.Zero) dir1 = v2.Normalise2(start - end, v2.Zero);
				
				var slen = v2.Dot2(centre - start, dir0);
				var elen = v2.Dot2(centre -   end, dir1);
				slen = Math.Min(Math.Max(slen, MinConnectorLen), Style.Smooth ? MinConnectorLen : slen);
				elen = Math.Min(Math.Max(elen, MinConnectorLen), Style.Smooth ? MinConnectorLen : elen);

				return new[]{start, start + slen*dir0, end + elen*dir1, end};
			}

			/// <summary>Check the self consistency of this element</summary>
			public override bool CheckConsistency()
			{
				// Should be connected to different nodes
				if (Node0 == Node1 && Node0 != null)
					throw new Exception("Connector {0} is connected to the same node at each end".Fmt(ToString()));

				// The connected nodes should contain a reference to this connector
				if (Node0 != null && Node0.Connectors.FirstOrDefault(x => x.Id == Id) == null)
					throw new Exception("Connector {0} is connected to node {0}, but that node does not contain a reference to this connector".Fmt(ToString(), Node0.ToString()));
				if (Node1 != null && Node1.Connectors.FirstOrDefault(x => x.Id == Id) == null)
					throw new Exception("Connector {0} is connected to node {0}, but that node does not contain a reference to this connector".Fmt(ToString(), Node1.ToString()));

				return base.CheckConsistency();
			}

			// ToString
			public override string ToString() { return "Connector (" + Anc0.ToString() + "-" + Anc1.ToString() + ")"; }
		}

		#endregion

		#region Styles

		/// <summary>Style attributes for nodes</summary>
		[TypeConverter(typeof(NodeStyle))]
		public class NodeStyle :GenericTypeConverter<NodeStyle> ,IHasId ,ICloneable ,IStyle
		{
			public static readonly NodeStyle Default = new NodeStyle{Id = Guid.Empty};

			public NodeStyle()
			{
				Id        = Guid.NewGuid();
				Border    = Color.Black;
				Fill      = Color.WhiteSmoke;
				Selected  = Color.LightBlue;
				Text      = Color.Black;
				TextAlign = ContentAlignment.MiddleCenter;
				Font      = new Font(FontFamily.GenericSansSerif, 12f, GraphicsUnit.Point);
				Padding   = new Padding(10,10,10,10);
			}
			public NodeStyle(XElement node) :this()
			{
				Id        = node.Element(XmlField.Id      ).As<Guid>            (Id       );
				Border    = node.Element(XmlField.Border  ).As<Color>           (Border   );
				Fill      = node.Element(XmlField.Fill    ).As<Color>           (Fill     );
				Selected  = node.Element(XmlField.Selected).As<Color>           (Selected );
				Text      = node.Element(XmlField.Text    ).As<Color>           (Text     );
				TextAlign = node.Element(XmlField.Align   ).As<ContentAlignment>(TextAlign);
				Font      = node.Element(XmlField.Font    ).As<Font>            (Font     );
				Padding   = node.Element(XmlField.Padding ).As<Padding>         (Padding  );
			}
			public NodeStyle(NodeStyle rhs)
			{
				Id           = rhs.Id                ;
				Border       = rhs.Border            ;
				Fill         = rhs.Fill              ;
				Text         = rhs.Text              ;
				TextAlign    = rhs.TextAlign         ;
				Font         = (Font)rhs.Font.Clone();
				Padding      = rhs.Padding           ;
				StyleChanged = rhs.StyleChanged      ;
			}

			/// <summary>Unique id for the style</summary>
			[Browsable(false)]
			public Guid Id { get; internal set; }

			/// <summary>The colour of the node border</summary>
			public Color Border
			{
				get { return m_impl_border; }
				set { Util.SetAndRaise(this, ref m_impl_border, value, StyleChanged); }
			}
			private Color m_impl_border;

			/// <summary>The node background colour</summary>
			public Color Fill
			{
				get { return m_impl_fill; }
				set { Util.SetAndRaise(this, ref m_impl_fill, value, StyleChanged); }
			}
			private Color m_impl_fill;

			/// <summary>The colour of the node when selected</summary>
			public Color Selected
			{
				get { return m_impl_selected; }
				set { Util.SetAndRaise(this, ref m_impl_selected, value, StyleChanged); }
			}
			private Color m_impl_selected;

			/// <summary>The node text colour</summary>
			public Color Text
			{
				get { return m_impl_text; }
				set { Util.SetAndRaise(this, ref m_impl_text, value, StyleChanged); }
			}
			private Color m_impl_text;

			/// <summary>The alignment of the text within the node</summary>
			public ContentAlignment TextAlign
			{
				get { return m_impl_align; }
				set { Util.SetAndRaise(this, ref m_impl_align, value, StyleChanged); }
			}
			private ContentAlignment m_impl_align;

			/// <summary>The font to use for the node text</summary>
			public Font Font
			{
				get { return m_impl_font; }
				set { Util.SetAndRaise(this, ref m_impl_font, value, StyleChanged); }
			}
			private Font m_impl_font;

			/// <summary>The padding surrounding the text in the node</summary>
			public Padding Padding
			{
				get { return m_impl_padding; }
				set { Util.SetAndRaise(this, ref m_impl_padding, value, StyleChanged); }
			}
			private Padding m_impl_padding;

			/// <summary>Raised whenever a style property is changed</summary>
			public event EventHandler StyleChanged;

			/// <summary>Export to xml</summary>
			public XElement ToXml(XElement node)
			{
				node.Add2(XmlField.Id       ,Id        ,false);
				node.Add2(XmlField.Border   ,Border    ,false);
				node.Add2(XmlField.Fill     ,Fill      ,false);
				node.Add2(XmlField.Selected ,Selected  ,false);
				node.Add2(XmlField.Text     ,Text      ,false);
				node.Add2(XmlField.Font     ,Font      ,false);
				node.Add2(XmlField.Align    ,TextAlign ,false);
				node.Add2(XmlField.Padding  ,Padding   ,false);
				return node;
			}

			/// <summary>Clone this style object</summary>
			public object Clone()
			{
				return new NodeStyle(this);
			}
			public void Update(NodeStyle rhs)
			{
				using (StyleChanged.SuspendScope())
				{
					Id        = rhs.Id       ;
					Border    = rhs.Border   ;
					Fill      = rhs.Fill     ;
					Selected  = rhs.Selected ;
					Text      = rhs.Text     ;
					TextAlign = rhs.TextAlign;
					Font      = rhs.Font     ;
					Padding   = rhs.Padding  ;
				}
				StyleChanged.Raise(this, EventArgs.Empty);
			}
		}

		/// <summary>Style properties for connectors</summary>
		[TypeConverter(typeof(ConnectorStyle))]
		public class ConnectorStyle :GenericTypeConverter<ConnectorStyle> ,IHasId ,ICloneable ,IStyle
		{
			public static readonly ConnectorStyle Default = new ConnectorStyle{Id = Guid.Empty};

			public ConnectorStyle()
			{
				Id       = Guid.NewGuid();
				Line     = Color.Black;
				Selected = Color.Blue;
				Dangling = Color.DarkRed;
				Width    = 5f;
				Smooth   = false;
			}
			public ConnectorStyle(XElement node) :this()
			{
				Id       = node.Element(XmlField.Id      ).As<Guid> (Id      );
				Line     = node.Element(XmlField.Line    ).As<Color>(Line    );
				Selected = node.Element(XmlField.Selected).As<Color>(Selected);
				Dangling = node.Element(XmlField.Dangling).As<Color>(Dangling);
				Width    = node.Element(XmlField.Width   ).As<float>(Width   );
				Smooth   = node.Element(XmlField.Smooth  ).As<bool> (Smooth  );
			}
			public ConnectorStyle(ConnectorStyle rhs)
			{
				Id           = Guid.NewGuid()  ;
				Line         = rhs.Line        ;
				Selected     = rhs.Selected    ;
				Dangling     = rhs.Dangling    ;
				Width        = rhs.Width       ;
				Smooth       = rhs.Smooth      ;
				StyleChanged = rhs.StyleChanged;
			}

			/// <summary>Unique id for the style</summary>
			[Browsable(false)]
			public Guid Id { get; internal set; }

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

			/// <summary>The colour of dangling connectors</summary>
			public Color Dangling
			{
				get { return m_impl_dangling; }
				set { Util.SetAndRaise(this, ref m_impl_dangling, value, StyleChanged); }
			}
			private Color m_impl_dangling;

			/// <summary>The width of the connector line</summary>
			public float Width
			{
				get { return m_impl_width; }
				set { Util.SetAndRaise(this, ref m_impl_width, value, StyleChanged); }
			}
			private float m_impl_width;

			/// <summary>True for a smooth connector, false for a straight edged connector</summary>
			public bool Smooth
			{
				get { return m_impl_smooth; }
				set { Util.SetAndRaise(this, ref m_impl_smooth, value, StyleChanged); }
			}
			private bool m_impl_smooth;

			/// <summary>Raised whenever a style property is changed</summary>
			public event EventHandler StyleChanged;

			/// <summary>Export to xml</summary>
			public XElement ToXml(XElement node)
			{
				node.Add2(XmlField.Id       ,Id       ,false);
				node.Add2(XmlField.Line     ,Line     ,false);
				node.Add2(XmlField.Selected ,Selected ,false);
				node.Add2(XmlField.Width    ,Width    ,false);
				node.Add2(XmlField.Smooth   ,Smooth   ,false);
				return node;
			}

			/// <summary>Clone this style object</summary>
			public object Clone()
			{
				return new ConnectorStyle(this);
			}
			public void Update(ConnectorStyle rhs)
			{
				using (StyleChanged.SuspendScope())
				{
					Id       = rhs.Id      ;
					Line     = rhs.Line    ;
					Selected = rhs.Selected;
					Dangling = rhs.Dangling;
					Width    = rhs.Width   ;
					Smooth   = rhs.Smooth  ;
				}
				StyleChanged.Raise(this, EventArgs.Empty);
			}
		}

		/// <summary>A cache of node or connector style objects</summary>
		private class StyleCache<T> where T:IHasId
		{
			private readonly Dictionary<Guid, T> m_map = new Dictionary<Guid, T>();

			public StyleCache() {}
			public StyleCache(XElement node)
			{
				foreach (var n in node.Elements())
				{
					var style = n.As<T>();
					m_map[style.Id] = style;
				}
			}
			public XElement ToXml(XElement node)
			{
				foreach (var ns in m_map)
					node.Add2("style", ns.Value, false);
				return node;
			}

			/// <summary>Add the styles in 'rhs' to this cache</summary>
			public void Merge(StyleCache<T> rhs)
			{
				foreach (var s in rhs.m_map)
					m_map[s.Key] = s.Value;
			}

			/// <summary>Reset the style cache</summary>
			public void Clear()
			{
				m_map.Clear();
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
		
			/// <summary>Remove styles that are not referenced by any elements</summary>
			internal void RemoveUnused(IEnumerable<Element> elements)
			{
				// Determine which styles are unused
				var unused = m_map.Keys.ToHashSet();
				foreach (var elem in elements)
				{
					var node = elem as Node;
					if (node != null) unused.Remove(node.Style.Id);
					var conn = elem as Connector;
					if (conn != null) unused.Remove(conn.Style.Id);
				}

				// Remove any remaining styles 
				foreach (var id in unused)
					m_map.Remove(id);
			}
		}

		/// <summary>Wraps the default style options</summary>
		public class DefaultStyles
		{
			public NodeStyle Node
			{
				get { return NodeStyle.Default; }
				set { NodeStyle.Default.Update(value); }
			}
			public ConnectorStyle Connector
			{
				get { return ConnectorStyle.Default; }
				set { ConnectorStyle.Default.Update(value); }
			}
		}

		#endregion

		#region Anchors

		/// <summary>A point that a connector can connect to</summary>
		public class AnchorPoint
		{
			public AnchorPoint()
				:this(null,v4.Origin,v4.YAxis)
			{}
			public AnchorPoint(Element elem, v4 loc, v4 norm)
			{
				m_impl_elem     = elem;
				m_impl_location = loc;
				m_impl_normal   = norm;
			}
			public AnchorPoint(IDictionary<Guid,Element> elements, XElement node)
				:this()
			{
				var id = node.Element(XmlField.ElementId).As<Guid>();
				m_impl_elem = elements.TryGetValue(id, out m_impl_elem) ? m_impl_elem : null;
				m_impl_location = node.Element(XmlField.Location).As<v4>(v4.Origin);
				Update(null, true);
			}

			/// <summary>Export to xml</summary>
			public XElement ToXml(XElement node)
			{
				node.Add2(XmlField.ElementId ,Elem != null ? Elem.Id : Guid.Empty ,false);
				node.Add2(XmlField.Location ,Location ,false);
				return node;
			}

			/// <summary>The element this anchor point is attached to</summary>
			public Element Elem
			{
				get { return m_impl_elem; }
				set
				{
					if (m_impl_elem == value) return;
					if (value == null)
					{
						// When set to null, record the last diagram space position
						// so that anything connected to this anchor point doesn't
						// snap to the origin.
						Location = LocationDS;
						Normal   = NormalDS;
					}
					m_impl_elem = value;
				}
			}
			private Element m_impl_elem;

			/// <summary>Get/Set the node-relative position of the anchor</summary>
			public v4 Location
			{
				get { return m_impl_location; }
				set
				{
					if (m_impl_location == value) return;
					m_impl_location = value;
				}
			}
			private v4 m_impl_location;

			/// <summary>Get/Set the node-relative anchor normal</summary>
			public v4 Normal
			{
				get { return m_impl_normal; }
				set
				{
					if (m_impl_normal == value) return;
					m_impl_normal = value;
				}
			}
			private v4 m_impl_normal;

			/// <summary>Get the diagram space position of the anchor</summary>
			public v4 LocationDS { get { return Elem != null ? Elem.Position * Location : Location; } }
			
			/// <summary>Get the diagram space anchor normal</summary>
			public v4 NormalDS   { get { return Elem != null ? Elem.Position * Normal   : Normal  ; } }

			/// <summary>Update this anchor point location after it has moved/resized</summary>
			public void Update(Element connected, bool nearest_previous)
			{
				if (Elem == null) return;
				var anc = connected != null && !nearest_previous
					? Elem.NearestAnchor(connected.Position.pos.xy, false)
					: Elem.NearestAnchor(Location.xy, true);
				Location = anc.Location;
				Normal   = anc.Normal;
			}

			// ToString
			public override string ToString() { return "Anchor (" + (Elem!=null?Elem.ToString():"dangling") + ")"; }
		}

		#endregion

		#region Mouse Op

		/// <summary>Base class for a mouse operation performed with the mouse down->drag->up sequence</summary>
		public abstract class MouseOp
		{
			protected DiagramControl m_diag; // The diagram

			//Selection data for a mouse button
			public bool  m_btn_down;   // True while the corresponding mouse button is down
			public Point m_grab_cs;    // The client space location of where the diagram was "grabbed"
			public v2    m_grab_ds;    // The diagram space location of where the diagram was "grabbed"
			public HitTestResult m_hit_result; // The hit test result on mouse down

			public MouseOp(DiagramControl diag)
			{
				m_diag = diag;
			}

			/// <summary>True if the mouse down event should be treated as a click (so far)</summary>
			protected bool IsClick(Point location)
			{
				var grab = v2.From(m_grab_cs);
				var diff = v2.From(location) - grab;
				return diff.Length2Sq < MinDragPixelDistanceSq;
			}

			/// <summary>Called on mouse down</summary>
			public abstract void MouseDown(MouseEventArgs e);

			/// <summary>Called on mouse move</summary>
			public abstract void MouseMove(MouseEventArgs e);

			/// <summary>Called on mouse up</summary>
			public abstract void MouseUp(MouseEventArgs e);
		}

		/// <summary>A mouse operation for dragging selected elements around</summary>
		public class MouseOpDragOrClickElements :MouseOp
		{
			private bool m_selection_graphic_added;
			public MouseOpDragOrClickElements(DiagramControl diag) :base(diag)
			{
				m_selection_graphic_added = false;
			}
			public override void MouseDown(MouseEventArgs e)
			{
				// Record the drag start positions for selected objects
				foreach (var elem in m_diag.Selected)
					elem.DragStartPosition = elem.Position;

				// Prevent events while dragging the elements around
				m_diag.RaiseEvents = false;
			}
			public override void MouseMove(MouseEventArgs e)
			{
				// If we haven't dragged, treat it as a click instead (i.e. ignore till it's a drag operation)
				if (IsClick(e.Location))
					return;

				var area_select = m_hit_result.Hits.FirstOrDefault(x => x.Element.Selected) == null;
				if (!area_select)
				{
					// If the drag operation started on a selected element then drag the
					// selected elements within the diagram.
					m_diag.DragSelected(m_diag.ClientToDiagram(e.Location) - m_grab_ds, false);
				}
				else
				{
					// Otherwise change the selection area
					if (!m_selection_graphic_added)
					{
						m_diag.Drawset.AddObject(m_diag.m_tools.AreaSelect);
						m_selection_graphic_added = true;
					}

					// Position the selection graphic
					var selection_area = BRect.FromBounds(m_grab_ds, m_diag.ClientToDiagram(e.Location));
					m_diag.m_tools.AreaSelect.O2P = m4x4.Scale(
						Math.Max(1f, selection_area.SizeX),
						Math.Max(1f, selection_area.SizeY),
						1.0f, new v4(selection_area.Centre, m_diag.HighestZ + 1f,1));
				}
				m_diag.Refresh();
			}
			public override void MouseUp(MouseEventArgs e)
			{
				m_diag.RaiseEvents = true;
				var is_area_select = m_hit_result.Hits.FirstOrDefault(x => x.Element.Selected) == null;

				// If this is a single click...
				if (IsClick(e.Location))
				{
					// Check to see if the click is on an existing selected object
					// If so, allow that object handle the click, otherwise select elements.
					var ht = m_diag.HitTestCS(e.Location);
					var sel = ht.Hits.FirstOrDefault(x => x.Element.Selected);
					if (sel == null || !sel.Element.HandleClicked(sel, ht.Camera))
					{
						var selection_area = new BRect(m_grab_ds, v2.Zero);
						m_diag.SelectElements(selection_area, ModifierKeys);
					}
				}
				else if (is_area_select)
				{
					var selection_area = BRect.FromBounds(m_grab_ds, m_diag.ClientToDiagram(e.Location));
					m_diag.SelectElements(selection_area, ModifierKeys);
				}
				else
					m_diag.DragSelected(m_diag.ClientToDiagram(e.Location) - m_grab_ds, true);

				// Remove the area selection graphic
				if (m_selection_graphic_added)
					m_diag.Drawset.RemoveObject(m_diag.m_tools.AreaSelect);

				m_diag.Refresh();
			}
		}

		/// <summary>A mouse operation for dragging the diagram around</summary>
		public class MouseOpDragOrClickDiagram :MouseOp
		{
			public MouseOpDragOrClickDiagram(DiagramControl diag) :base(diag) {}
			public override void MouseDown(MouseEventArgs e)
			{}
			public override void MouseMove(MouseEventArgs e)
			{
				// If we haven't dragged, treat it as a click instead (i.e. ignore till it's a drag operation)
				if (IsClick(e.Location))
					return;

				// Change the cursor once dragging
				m_diag.Cursor = Cursors.SizeAll;
				m_diag.PositionDiagram(e.Location, m_grab_ds);
				m_diag.Refresh();
			}
			public override void MouseUp(MouseEventArgs e)
			{
				m_diag.Cursor = Cursors.Default;

				// If we haven't dragged, treat it as a click instead
				if (IsClick(e.Location))
					m_diag.OnDiagramClicked(e);
				else
					m_diag.PositionDiagram(e.Location, m_grab_ds);

				m_diag.Refresh();
			}
		}

		/// <summary>Base class for mouse operations that involve moving a link</summary>
		public abstract class MouseOpMoveLinkBase :MouseOp
		{
			protected readonly Node m_fixed;      // The unmoving node
			protected readonly NodeProxy m_proxy; // Proxy node that one end of the link will be attached to
			protected readonly bool m_forward;    // True if 'm_fixed' is the 'from' end of the link
			protected Connector m_conn;           // The connector being moved

			public MouseOpMoveLinkBase(DiagramControl diag, Node fixed_, bool forward) :base(diag)
			{
				m_fixed   = fixed_;
				m_conn    = null;
				m_forward = forward;
				m_proxy   = new NodeProxy(){Diagram = diag}; // Create a proxy for the floating end to attach to
			}
			private HitTestResult.Hit HitTestNode(Point location_cs)
			{
				return m_diag.HitTestCS(location_cs).Hits.FirstOrDefault(x => x.Entity == Entity.Node && x.Element != m_proxy && x.Element != m_fixed);
			}
			public override void MouseDown(MouseEventArgs e)
			{}
			public override void MouseMove(MouseEventArgs e)
			{
				// If we're hovering over a node, find the nearest anchor on that node and snap to it
				var hit = HitTestNode(e.Location);
				var target = hit != null ? hit.Element.As<Node>() : null;

				if (target != null)
				{
					var anchor = target.NearestAnchor(hit.Point, true);
					m_proxy.PositionXY = anchor.LocationDS.xy;
					m_proxy.AnchorNormal = anchor.Normal;
				}
				else
				{
					m_proxy.PositionXY = m_diag.ClientToDiagram(e.Location);
					m_proxy.AnchorNormal = v4.Zero;
				}
				m_conn.Invalidate();
				m_diag.Refresh();
			}
			public override void MouseUp(MouseEventArgs e)
			{
				// Find the node to make the link with
				var hit = HitTestNode(e.Location);
				var target = hit != null ? hit.Element.As<Node>() : null;

				// No link, just revert the link
				if (target == null)
				{
					Revert();
					m_proxy.Dispose();
				}
				else
				{
					var anchor = target.NearestAnchor(hit.Point, true);

					// Attach 'm_conn' to 'target'
					if (m_forward)
					{
						m_conn.Node1 = target;
						m_conn.Anc1.Location = anchor.Location;
						m_conn.Anc1.Normal   = anchor.Normal;
					}
					else
					{
						m_conn.Node0 = target;
						m_conn.Anc0.Location = anchor.Location;
						m_conn.Anc0.Normal   = anchor.Normal;
					}

					// Notify the caller
					var res = m_diag.RaiseDiagramChanged(GetNotifyArgs(target));
					if (res.Cancel)
						Revert();
					else
						m_conn.Invalidate();

					m_proxy.Dispose();
				}
				m_diag.Refresh();
			}
			protected abstract void Revert();
			protected abstract DiagramChangedEventArgs GetNotifyArgs(Node target);
		}

		/// <summary>
		/// A mouse operation for creating a new link between nodes.
		/// If a link gets made between nodes where there is already an existing
		/// link, the new link just disappears</summary>
		public class MouseOpCreateLink :MouseOpMoveLinkBase
		{
			public MouseOpCreateLink(DiagramControl diag, Node fixed_, bool forward)
				:base(diag, fixed_, forward)
			{
				m_conn = m_forward
					? new Connector(m_fixed, m_proxy){Diagram = diag}
					: new Connector(m_proxy, m_fixed){Diagram = diag};
			}
			protected override void Revert()
			{
				m_conn.Dispose();
			}
			protected override DiagramChangedEventArgs GetNotifyArgs(Node target)
			{
				return new DiagramChangedEventArgs(DiagramChangedEventArgs.EType.AddingLink, conn:m_conn);
			}
		}

		/// <summary>
		/// A mouse operation for relinking an existing link to another node.
		/// If a link already existings for the new fixed,target pair, revert
		/// the link back to it's original position</summary>
		public class MouseOpMoveLink :MouseOpMoveLinkBase
		{
			private readonly AnchorPoint m_prev; // The anchor point that the moving end was originally attached to

			public MouseOpMoveLink(DiagramControl diag, Connector conn, bool forward)
				:base(diag, forward ? conn.Node0 : conn.Node1, forward)
			{
				m_conn = conn;
				m_prev = forward ? m_conn.Anc1 : m_conn.Anc0;

				// Replace the moving end with the proxy
				if (m_forward) m_conn.Node1 = m_proxy;
				else           m_conn.Node0 = m_proxy;
			}
			protected override void Revert()
			{
				if (m_forward) m_conn.Anc1 = m_prev;
				else           m_conn.Anc0 = m_prev;
				m_conn.Relink(true);
			}
			protected override DiagramChangedEventArgs GetNotifyArgs(Node target)
			{
				return new DiagramChangedEventArgs(DiagramChangedEventArgs.EType.MovingLink, node:target, conn:m_conn);
			}
		}

		#endregion

		#region Event Args

		/// <summary>Event args for the diagram changed event</summary>
		public class DiagramChangedEventArgs :EventArgs
		{
			// Note: many events are available by attaching to the Elements binding list

			public enum EType
			{
				/// <summary>
				/// Raised after the diagram has had data changed. This event will be raised
				/// in addition to the more detailed modification notification events below</summary>
				Edited,

				/// <summary>
				/// A new link is being created.
				/// Setting 'Cancel' for this event will abort the add.
				/// 'Connector' holds the new connector being added.</summary>
				AddingLink,

				/// <summary>
				/// An end of a connector is being moved to another node
				/// Setting 'Cancel' for this event will abort the move.
				/// 'Connector' holds the connector being moved.
				/// 'Node' holds the new node being attached to.</summary>
				MovingLink,
			}

			/// <summary>The type of change that occurred</summary>
			public EType ChgType { get; private set; }

			/// <summary>A cancel property for "about to change" events</summary>
			public bool Cancel { get; set; }

			/// <summary>The node involved in the change (or null if not relevant)</summary>
			public Node Node { get; private set; }

			/// <summary>The connector involved in the change (or null if not relevant)</summary>
			public Connector Connector { get; private set; }

			public DiagramChangedEventArgs(EType ty, Node node = null, Connector conn = null)
			{
				ChgType   = ty;
				Node      = node;
				Connector = conn;
				Cancel    = false;
			}
		}

		/// <summary>Event args for user context menu options</summary>
		public class AddUserMenuOptionsEventArgs :EventArgs
		{
			public ContextMenuStrip Menu { get; private set; }
			public AddUserMenuOptionsEventArgs(ContextMenuStrip menu) { Menu = menu; }
		}

		#endregion

		#region Misc

		/// <summary>How close a click as to be for selection to occur (in screen space)</summary>
		private const float MinCSSelectionDistanceSq = 100f;

		/// <summary>Minimum distance in pixels before the diagram starts dragging</summary>
		private const int MinDragPixelDistanceSq = 25;

		/// <summary>The minimum distance a connector sticks out from a node</summary>
		private const int MinConnectorLen = 30;

		/// <summary>Interface for shape creation types</summary>
		public interface IShape
		{
			/// <summary>Generate the ldr string for the shape</summary>
			string Make(v2 sz);
		}
		
		/// <summary>A quad shape</summary>
		public class QuadShape :IShape
		{
			public QuadShape(float corner_radius)
			{
				CornerRadius = corner_radius;
			}

			/// <summary>The radius of the quad corners</summary>
			public float CornerRadius { get; set; }

			/// <summary>Generate the ldr string for the shape</summary>
			public string Make(v2 sz)
			{
				var ldr = new LdrBuilder();
				ldr.Append("*Rect {3 ",sz.x," ",sz.y," ",Ldr.Solid()," ",Ldr.CornerRadius(CornerRadius),"}\n");
				return ldr.ToString();
			}
		}

		/// <summary>A ellipse shape</summary>
		public class EllipseShape :IShape
		{
			public string Make(v2 sz)
			{
				var ldr = new LdrBuilder();
				ldr.Append("*Circle {3 ",sz.x/2f," ",sz.y/2f," ",Ldr.Solid(),"}\n");
				return ldr.ToString();
			}
		}

		/// <summary>A 'mixin' class for a textured round-cornered quad</summary>
		internal class TexturedShape<TShape> :View3d.Object where TShape :IShape
		{
			/// <summary>True when the model needs updating</summary>
			private bool m_model_dirty;

			public TexturedShape(TShape shape, v2 sz, uint texture_scale = 4)
			{
				Shape = shape;
				m_surf = new Surface((uint)(sz.x + 0.5f), (uint)(sz.y + 0.5f), texture_scale);
				m_model_dirty = true;
			}
			public override void Dispose()
			{
 				m_surf.Dispose();
				base.Dispose();
			}

			/// <summary>The underlying shape</summary>
			public TShape Shape { get; private set; }

			/// <summary>The texture surface of the quad</summary>
			public Surface Surface { get { return m_surf; } }
			private readonly Surface m_surf;

			/// <summary>Texture scaling factor</summary>
			public uint TextureScale
			{
				get { return Surface.TextureScale; }
			}

			/// <summary>Lock the texture for drawing on</summary>
			public View3d.Texture.Lock LockSurface()
			{
				return Surface.LockSurface();
			}

			/// <summary>Get/Set the size of the quad and texture</summary>
			public v2 Size
			{
				get { return m_surf.Size; }
				set
				{
					if (Size == value) return;
					m_surf.Size = value;
					m_model_dirty = true;
				}
			}

			/// <summary>Update the model</summary>
			public void Refresh()
			{
				if (!m_model_dirty) return;

				var ldr = Shape.Make(Size);
				UpdateModel(ldr, View3d.EUpdateObject.All ^ View3d.EUpdateObject.Transform);
				SetTexture(Surface.Surf);
				m_model_dirty = false;
			}
		}

		/// <summary>A 'mixin' class for elements containing a texture</summary>
		internal class Surface :IDisposable
		{
			public Surface(uint sx, uint sy, uint texture_scale = 4)
			{
				Debug.Assert(texture_scale != 0);
				TextureScale = texture_scale;
				sx = Math.Max(1, sx * TextureScale);
				sy = Math.Max(1, sy * TextureScale);
				Surf = new View3d.Texture(sx, sy, new View3d.TextureOptions(true){Filter=View3d.EFilter.D3D11_FILTER_ANISOTROPIC});
			}
			public void Dispose()
			{
				Surf = null;
			}

			/// <summary>Texture scaling factor</summary>
			public uint TextureScale { get; private set; }

			/// <summary>Get/Set the logical (i.e not-scaled) size of the texture</summary>
			public v2 Size
			{
				get { return v2.From(Surf.Size) / TextureScale; }
				set
				{
					var tex_size = (value * TextureScale).ToSize();
					if (Surf == null || Surf.Size == tex_size) return;
					Surf.Size = tex_size;
				}
			}

			/// <summary>The texture surface</summary>
			public View3d.Texture Surf
			{
				get { return m_impl_surf; }
				private set
				{
					if (m_impl_surf != null) m_impl_surf.Dispose();
					m_impl_surf = value;
				}
			}
			private View3d.Texture m_impl_surf;

			/// <summary>
			/// Lock the texture for drawing on.
			/// Draw in logical surface area units, ignore texture scale</summary>
			public View3d.Texture.Lock LockSurface()
			{
				var lck = Surf.LockSurface();
				lck.Gfx.ScaleTransform(TextureScale, TextureScale);
				return lck;
			}
		}

		/// <summary>Wraps a hosted control used to edit an element value</summary>
		public class EditingControl
		{
			/// <summary>The control to be display when editing the element</summary>
			public Control Ctrl;

			/// <summary>Called to resize the form during editing</summary>
			public Action<Form> DoAutoSize;
			
			/// <summary>Called if the editing is accepted</summary>
			public Action Commit;

			public EditingControl(Control ctrl, Action commit)
				:this(ctrl, null, commit)
			{}
			public EditingControl(Control ctrl, Action<Form> autosize, Action commit)
			{
				Ctrl       = ctrl;
				DoAutoSize = autosize;
				Commit     = commit;
			}
		}

		/// <summary>Helper form for editing the value of an element using a hosted control</summary>
		private class EditField :Form
		{
			private readonly EditingControl m_ctrl;
			public EditField(EditingControl ctrl)
			{
				m_ctrl = ctrl;
				m_ctrl.Ctrl.Dock = DockStyle.Fill;

				ShowInTaskbar   = false;
				FormBorderStyle = FormBorderStyle.None;
				KeyPreview      = true;
				Margin          = Padding.Empty;
				StartPosition   = FormStartPosition.Manual;
				Controls.Add(m_ctrl.Ctrl);
			}
			protected override void OnShown(EventArgs e)
			{
				if (m_ctrl.DoAutoSize != null)
					m_ctrl.DoAutoSize(this);

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
				
				if (m_ctrl.DoAutoSize != null)
					m_ctrl.DoAutoSize(this);

				base.OnKeyDown(e);
			}
		}

		/// <summary>String constants used in xml export/import</summary>
		private static class XmlField
		{
			public const string Root            = "root"            ;
			public const string TypeAttribute   = "ty"              ;
			public const string Element         = "elem"            ;
			public const string Id              = "id"              ;
			public const string Position        = "pos"             ;
			public const string Text            = "text"            ;
			public const string AutoSize        = "autosize"        ;
			public const string SizeMax         = "sizemax"         ;
			public const string Size            = "size"            ;
			public const string CornerRadius    = "cnr"             ;
			public const string Anchor          = "anc"             ;
			public const string Anchor0         = "anc0"            ;
			public const string Anchor1         = "anc1"            ;
			public const string CentreOffset    = "centre_offset"   ;
			public const string Label           = "label"           ;
			public const string Styles          = "style"           ;
			public const string NodeStyles      = "node_styles"     ;
			public const string ConnStyles      = "conn_styles"     ;
			public const string Style           = "style"           ;
			public const string Border          = "border"          ;
			public const string Fill            = "fill"            ;
			public const string Selected        = "selected"        ;
			public const string Dangling        = "dangling"        ;
			public const string Font            = "font"            ;
			public const string Type            = "type"            ;
			public const string Align           = "align"           ;
			public const string Line            = "line"            ;
			public const string Width           = "width"           ;
			public const string Smooth          = "smooth"          ;
			public const string Padding         = "padding"         ;
			public const string ElementId       = "elem_id"         ;
			public const string Location        = "loc"             ;
			public const string Margin          = "margin"          ;
			public const string AutoRelink      = "auto_relink"     ;
			public const string Iterations      = "iterations"      ;
			public const string SpringConstant  = "spring_constant" ;
			public const string CoulombConstant = "coulomb_constant";
			public const string ConnectorScale  = "connector_scale" ;
			public const string Equilibrium     = "equilibrium"     ;
			public const string NodeSettings    = "node_settings"   ;
			public const string ScatterSettings = "scatter_settings";
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

		/// <summary>Results collection for a hit test</summary>
		public class HitTestResult
		{
			public class Hit
			{
				/// <summary>The type of element hit</summary>
				public Entity Entity { get { return Element.Entity; } }

				/// <summary>The element that was hit</summary>
				public Element Element { get; private set; }

				/// <summary>Where on the element it was hit (in element space)</summary>
				public PointF Point { get; private set; }

				/// <summary>The element's diagram location at the time it was hit</summary>
				public m4x4 Location { get; private set; }

				public Hit(Element elem, PointF pt)
				{
					Element  = elem;
					Point    = pt;
					Location = elem.Position;
				}
				public override string ToString() { return Element.ToString(); }
			}

			/// <summary>The collection of hit objects</summary>
			public List<Hit> Hits { get; private set; }

			/// <summary>The camera position when the hit test was performed (needed for diagram to screen space conversion)</summary>
			public View3d.CameraControls Camera { get; private set; }

			public HitTestResult(View3d.CameraControls cam)
			{
				Hits = new List<Hit>();
				Camera = cam;
			}
		}

		/// <summary>A collection of graphics used by the diagram itself</summary>
		public class Tools :IDisposable
		{
			/// <summary>Graphic for area selection</summary>
			public View3d.Object AreaSelect { get; private set; }

			public Tools()
			{
				AreaSelect = new View3d.Object("*Rect selection 80000000 {3 1 1 *Solid}");
			}
			public void Dispose()
			{
				if (AreaSelect != null)
					AreaSelect.Dispose();
			}
		}

		#endregion

		#region Options

		/// <summary>Rendering options</summary>
		public class DiagramOptions
		{
			// Colours for graph elements
			public Color m_bg_colour      = SystemColors.ControlDark;      // The fill colour of the background
			public Color m_title_colour   = Color.Black;                   // The colour of the title text
			//public float m_left_margin    = 0.0f; // Fractional distance from the left edge
			//public float m_right_margin   = 0.0f;
			//public float m_top_margin     = 0.0f;
			//public float m_bottom_margin  = 0.0f;
			//public float m_title_top      = 0.01f; // Fractional distance  down from the top of the client area to the top of the title text
			//public Font  m_title_font     = new Font("tahoma", 12, FontStyle.Bold);    // Font to use for the title text
			//public Font  m_note_font      = new Font("tahoma",  8, FontStyle.Regular); // Font to use for graph notes

			// Graph margins and constants
			public class NodeOptions
			{
				public float Margin = 30f;
				public bool AutoRelink = false;

				public NodeOptions() {}
				public NodeOptions(XElement node)
				{
					Margin     = node.Element(XmlField.Margin    ).As<float>(Margin    );
					AutoRelink = node.Element(XmlField.AutoRelink).As<bool> (AutoRelink);
				}
				public XElement ToXml(XElement node)
				{
					node.Add2(XmlField.Margin, Margin, false);
					node.Add2(XmlField.AutoRelink, AutoRelink, false);
					return node;
				}
			}
			public NodeOptions Node = new NodeOptions();

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
					MaxIterations   = node.Element(XmlField.Iterations     ).As<int>  (MaxIterations  );
					SpringConstant  = node.Element(XmlField.SpringConstant ).As<float>(SpringConstant );
					CoulombConstant = node.Element(XmlField.CoulombConstant).As<float>(CoulombConstant);
					ConnectorScale  = node.Element(XmlField.ConnectorScale ).As<float>(ConnectorScale );
					Equilibrium     = node.Element(XmlField.Equilibrium    ).As<float>(Equilibrium    );
				}
				public XElement ToXml(XElement node)
				{
					node.Add2(XmlField.Iterations      ,MaxIterations   ,false);
					node.Add2(XmlField.SpringConstant  ,SpringConstant  ,false);
					node.Add2(XmlField.CoulombConstant ,CoulombConstant ,false);
					node.Add2(XmlField.ConnectorScale  ,ConnectorScale  ,false);
					node.Add2(XmlField.Equilibrium     ,Equilibrium     ,false);
					return node;
				}
			}
			public ScatterOptions Scatter = new ScatterOptions();

			public DiagramOptions() {}
			public DiagramOptions(XElement node)
			{
				Node    = node.Element(XmlField.NodeSettings   ).As<NodeOptions   >(Node   );
				Scatter = node.Element(XmlField.ScatterSettings).As<ScatterOptions>(Scatter);
			}
			public XElement ToXml(XElement node)
			{
				node.Add2(XmlField.NodeSettings    ,Node    ,false);
				node.Add2(XmlField.ScatterSettings ,Scatter ,false);
				return node;
			}
			public DiagramOptions Clone() { return (DiagramOptions)MemberwiseClone(); }
		}

		#endregion

		// Members
		private readonly View3d                  m_view3d;           // Renderer
		private readonly View3d.DrawsetInterface m_drawset;          // A drawset for this control instance
		private readonly EventBatcher            m_eb_update_diag;   // Event batcher for updating the diagram graphics
		private readonly HoverScroll             m_hoverscroll;      // Hoverscroll
		private View3d.CameraControls            m_camera;           // The virtual window over the diagram
		private readonly Tools                   m_tools;            // Tools
		private StyleCache<NodeStyle>            m_node_styles;      // The collection of node styles
		private StyleCache<ConnectorStyle>       m_connector_styles; // The collection of node styles
		private MouseOp[]                        m_mouse_op;         // Per button current mouse operation

		public DiagramControl() :this(new DiagramOptions()) {}
		public DiagramControl(DiagramOptions options)
		{
			Elements           = new BindingListEx<Element>();
			Selected           = new BindingListEx<Element>();
			m_impl_options     = options ?? new DiagramOptions();
			m_eb_update_diag   = new EventBatcher(UpdateDiagram);
			m_hoverscroll      = new HoverScroll(Handle);
			m_node_styles      = new StyleCache<NodeStyle>();
			m_connector_styles = new StyleCache<ConnectorStyle>();
			m_mouse_op         = new MouseOp[Enum<EBtnIdx>.Count];
			Edited             = false;

			if (this.IsInDesignMode()) return;
			m_view3d  = new View3d(Handle, Render);
			m_drawset = m_view3d.DrawsetCreate();
			m_tools   = new Tools();
			m_camera  = new View3d.CameraControls(m_drawset);
			m_camera.SetClipPlanes(0.5f, 1.1f, true);

			InitializeComponent();

			m_drawset.LightProperties = View3d.Light.Directional(-v4.ZAxis, Colour32.Zero, Colour32.Gray, Colour32.Zero, 0f, 0f);
			m_drawset.FocusPointVisible = false;
			m_drawset.OriginVisible = false;
			m_drawset.Orthographic = false;

			Elements.ListChanging += HandleElementListChanging;

			ResetView();
			DefaultKeyboardShortcuts = true;
			DefaultMouseControl = true;
			AllowEditing = true;
			AllowMove = true;
			AllowSelection = true;
		}
		protected override void Dispose(bool disposing)
		{
			// We don't own the elements, so don't dispose them
			if (disposing)
			{
				ResetDiagram();
				if (m_eb_update_diag != null)
					m_eb_update_diag.Dispose();
				if (m_view3d != null)
					m_view3d.Dispose();
				if (components != null)
					components.Dispose();
				if (m_hoverscroll != null)
					m_hoverscroll.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <summary>Raised whenever elements in the diagram have been edited or moved</summary>
		public event EventHandler<DiagramChangedEventArgs> DiagramChanged;
		private DiagramChangedEventArgs RaiseDiagramChanged(DiagramChangedEventArgs args)
		{
			DiagramChanged.Raise(this, args);
			return args;
		}

		/// <summary>Event allowing callers to add options to the context menu</summary>
		public event EventHandler<AddUserMenuOptionsEventArgs> AddUserMenuOptions;

		/// <summary>Get/Set whether DiagramChanged events are raised</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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
			if (Elements != null)
				Elements.Clear();
		}

		/// <summary>Diagram objects</summary>
		public BindingListEx<Element> Elements { get; private set; }

		/// <summary>The set of selected diagram elements</summary>
		public BindingListEx<Element> Selected { get; private set; }

		/// <summary>True when the diagram has been edited and requires saving</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool Edited { get; set; }

		/// <summary>Controls for how the diagram is rendered</summary>
		public DiagramOptions Options
		{
			get { return m_impl_options; }
			set
			{
				m_impl_options = value ?? new DiagramOptions();
				if (!DesignMode) UpdateDiagram(true);
			}
		}
		private DiagramOptions m_impl_options;

		/// <summary>Minimum bounding area for view reset</summary>
		public BRect ResetMinBounds
		{
			get { return new BRect(v2.Zero, new v2(Width/1.5f, Height/1.5f)); }
		}

		/// <summary>Perform a hit test on the diagram</summary>
		public HitTestResult HitTest(v2 ds_point)
		{
			var result = new HitTestResult(m_camera);
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
					return l.Element.PositionZ.CompareTo(r.Element.PositionZ);
				});
			return result;
		}
		public HitTestResult HitTestCS(Point cs_point)
		{
			return HitTest(ClientToDiagram(cs_point));
		}

		/// <summary>Standard keyboard shortcuts</summary>
		public void TranslateKey(object sender, KeyEventArgs args)
		{
			switch (args.KeyCode)
			{
			case Keys.Escape:
				{
					if (AllowSelection)
					{
						Selected.ToArray().ForEach(x => x.Selected = false);
						Refresh();
					}
					break;
				}
			case Keys.Delete:
				{
					if (AllowEditing)
					{
						Selected.ToArray().ForEach(x => Elements.Remove(x));
					}
					break;
				}
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
			case Keys.A:
				if ((args.Modifiers & Keys.Control) != 0)
				{
					Elements.ForEach(x => x.Selected = true);
					Refresh();
				}
				break;
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
		public bool DefaultMouseControl { get; set; }

		/// <summary>True if users are allowed to add/remove/edit nodes on the diagram</summary>
		public bool AllowEditing { get; set; }

		/// <summary>True if users are allowed to move elements on the diagram</summary>
		public bool AllowMove { get; set; }

		/// <summary>True if users are allowed to select elements on the diagram</summary>
		public bool AllowSelection { get; set; }

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);

			// Look for the mouse op to perform
			var btn = View3d.ButtonIndex(e.Button);
			if (m_mouse_op[(int)btn] == null)
			{
				// If this is a left mouse button down event and there is a current
				// operation in the 'EBtnIdx.None' slot, swap it to the left btn slot
				if (btn == EBtnIdx.Left && m_mouse_op[(int)EBtnIdx.None] != null)
				{
					m_mouse_op.Swap((int)EBtnIdx.Left, (int)EBtnIdx.None);
				}
				// Otherwise, if default navigation is on, create the default mouse op
				else if (DefaultMouseControl)
				{
					m_mouse_op[(int)btn] = CreateDefaultMouseOp(btn);
				}

				// If there's still no mouse op, ignore the event
				if (m_mouse_op[(int)btn] == null)
					return;
			}

			// Get the mouse op, save mouse location and hit test data, then call op.MouseDown()
			var op = m_mouse_op[(int)btn];
			op.m_btn_down   = true;
			op.m_grab_cs    = e.Location;
			op.m_grab_ds    = ClientToDiagram(op.m_grab_cs);
			op.m_hit_result = HitTest(op.m_grab_ds);
			op.MouseDown(e);

			Capture = true;
		}
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			// Look for the mouse op to perform
			var btn = View3d.ButtonIndex(e.Button);
			if (m_mouse_op[(int)btn] == null)
				return;

			var op = m_mouse_op[(int)btn];
			op.MouseMove(e);
		}
		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);

			// Only release the mouse when all buttons are up
			if (MouseButtons == MouseButtons.None)
				Capture = false;

			// Look for the mouse op to perform
			var btn = View3d.ButtonIndex(e.Button);
			if (m_mouse_op[(int)btn] == null)
				return;

			var op = m_mouse_op[(int)btn];
			op.MouseUp(e);

			m_mouse_op[(int)btn] = null;
		}
		protected override void OnMouseWheel(MouseEventArgs e)
		{
			base.OnMouseWheel(e);

			var delta = e.Delta < -999 ? -999 : e.Delta > 999 ? 999 : e.Delta;
			m_camera.Navigate(0, 0, e.Delta / 120f);
			Refresh();
		}

		/// <summary>Create the default navigation mouse operation based on mouse button</summary>
		private MouseOp CreateDefaultMouseOp(EBtnIdx btn_idx)
		{
			switch (btn_idx)
			{
			default: return null;
			case EBtnIdx.Left: return new MouseOpDragOrClickElements(this);
			case EBtnIdx.Right: return new MouseOpDragOrClickDiagram(this);
			}
		}

		/// <summary>Handle mouse clicks on the diagram</summary>
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
			var ht = HitTestCS(e.Location);
			var hit = ht.Hits.FirstOrDefault();
			if (hit == null)
				return;

			// Edit the element
			hit.Element.Edit();
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

		/// <summary>Returns a selected element if it is the only selected element</summary>
		private Element SingleSelection
		{
			get { return Selected.Count == 1 ? Selected[0] : null; }
		}

		/// <summary>Handle elements added/removed from the elements list</summary>
		private void HandleElementListChanging(object sender, ListChgEventArgs<Element> args)
		{
			switch (args.ChangeType)
			{
			case ListChg.PreReset:
				{
					// This Elements list is about to be cleared, detach all elements from the
					// diagram. Note, don't dispose, we don't own the elements
					foreach (var elem in Elements)
						elem.SetDiagramInternal(null, false);

					// no sanity check here, because the Elements.Clear() is still in progress
					break;
				}
			case ListChg.ItemPreRemove:
				{
					var elem = args.Item;
					if (elem.Diagram != null && elem.Diagram != this) throw new ArgumentException("element belongs to another diagram");
					elem.SetDiagramInternal(null, false);
					// no sanity check here, because the Elements.Remove() is still in progress
					break;
				}
			case ListChg.ItemAdded:
				{
					var elem = args.Item;
					if (elem.Diagram != null && elem.Diagram != this) throw new ArgumentException("element belongs to another diagram");
					elem.SetDiagramInternal(this, false);

					// Update the diagram
					m_eb_update_diag.Signal();
					Debug.Assert(CheckConsistency());
					break;
				}
			case ListChg.ItemRemoved:
			case ListChg.Reset:
				{
					// Update the diagram
					m_eb_update_diag.Signal();
					Debug.Assert(CheckConsistency());
					break;
				}
			}
		}

		/// <summary>
		/// Select elements that are wholy within 'rect'. (rect is in diagram space)
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
				// Otherwise select the next element below the current selection or the top
				// element if nothing is selected. Clear all other selections
				else
				{
					// Find the element to select
					var to_select = hits.Hits.FirstOrDefault();
					for (int i = 0; i < hits.Hits.Count - 1; ++i)
					{
						if (!hits.Hits[i].Element.Selected) continue;
						to_select = hits.Hits[i+1];
						break;
					}

					// Deselect all elements
					foreach (var elem in Selected.ToArray())
						elem.Selected = false;

					// Select the next element
					if (to_select != null)
						to_select.Element.Selected = true;
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
			var conns = nodes.SelectMany(x => x.Connectors).Where(x => !x.Dangling && x.Node0.Selected == selected && x.Node1.Selected == selected).Distinct().ToList();

			// Block events while we move the nodes around
			using (Scope.Create(
				() =>
					{
						nodes.ForEach(x => x.RaiseEvents = false);
						conns.ForEach(x => x.Visible = false);
					},
				() =>
					{
						nodes.ForEach(x => x.RaiseEvents = true);
						conns.ForEach(x => x.Visible = true);
					}))
			{
				UpdateDiagram();

				// Prevent issues with nodes exactly on top of each other
				var rand = new Rand(); var djitter = 10f;
				Func<v2> jitter = () => v2.Random2(-djitter, djitter, rand);

				// Finds the minimum separation distance between to nodes for a given direction
				Func<v2,Node,Node,float> min_separation = (v,n0,n1) =>
					{
						var sz = n1.Bounds.Size + n0.Bounds.Size;
						return (Math.Abs(v.x / sz.x) > Math.Abs(v.y / sz.y) ? sz.x : sz.y) + Options.Node.Margin;
					};

				// A quad tree for faster coulomb force calculation
				// Force tables to detect equilibrium
				var qtree = new QuadTree<Node>(ContentBounds);
				var force = new Dictionary<Node,v2>();
				var last  = new Dictionary<Node,v2>();

				// Initialise the forces and scatter the initial positions of the nodes
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

						// Add the forces to each node
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
								if (node0 == node1) return true;

								// Find the minimum separation and the current separation
								var vec     = node1.PositionXY - node0.PositionXY;
								var min_sep = min_separation(vec, node0, node1);
								var sep     = Math.Max(min_sep, vec.Length2);

								// Coulomb force F = kQq/x²
								var q1 = 1f + node1.Connectors.Count * Options.Scatter.ConnectorScale;
								var coulumb = Options.Scatter.CoulombConstant * q0 * q1 / (sep * sep);

								// Only apply the force to 'node0' since we'll apply it to node1 when 'j' gets to it
								var f = vec * (coulumb / sep);
								force[node0] -= f;
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

					Refresh();

					if (equilibrium)
						break;
				}
			}

			// Relink all connectors connected to nodes that have moved
			foreach (var c in nodes.SelectMany(x => x.Connectors).Distinct())
				c.Relink(!Options.Node.AutoRelink);
			UpdateDiagram();
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

		/// <summary>The Z value of the highest element in the diagram</summary>
		private float HighestZ { get; set; }

		/// <summary>The Z value of the lowest element in the diagram</summary>
		private float LowestZ { get; set; }

		/// <summary>Set the Z value for all elements</summary>
		private void UpdateElementZOrder()
		{
			LowestZ = 0f;
			HighestZ = 0f;

			foreach (var elem in Elements.Where(x => x.Entity == Entity.Node))
				elem.PositionZ = HighestZ += 0.001f;

			foreach (var elem in Elements.Where(x => x.Entity == Entity.Label))
				elem.PositionZ = HighestZ += 0.001f;

			foreach (var elem in Elements.Where(x => x.Entity == Entity.Connector))
				elem.PositionZ = LowestZ -= 0.001f;
		}

		/// <summary>
		/// Removes and re-adds all elements to the diagram.
		/// Should only be used when the elements collection is modifed, otherwise use Refresh()</summary>
		private void UpdateDiagram(bool invalidate_all)
		{
			m_drawset.RemoveAllObjects();

			// Invalidate all first (if needed)
			if (invalidate_all)
				Elements.ForEach(x => x.Invalidate());

			// Ensure the z order is up to date
			UpdateElementZOrder();

			// Add the elements to the drawset
			foreach (var elem in Elements)
			{
				if (!elem.Visible) continue;
				elem.AddToDrawset(m_drawset);
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
				{
					var b = elem.Bounds;
					if (b.IsValid) bounds.Encompass(b);
				}
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
				if (Edited)
				{
					RaiseDiagramChanged(new DiagramChangedEventArgs(DiagramChangedEventArgs.EType.Edited));
					Edited = false;
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

			if (m_drawset == null)
				base.OnPaintBackground(e);
		}

		// Paint the control
		protected override void OnPaint(PaintEventArgs e)
		{
			if (this.IsInDesignMode()) { base.OnPaint(e); return; }

			if (m_drawset == null)
				base.OnPaint(e);
			else
				m_view3d.Refresh();
		}

		/// <summary>Render and Present the scene</summary>
		private void Render()
		{
			m_drawset.Render();
			m_view3d.Present();
		}

		/// <summary>Access the drawset associated with this instance</summary>
		private View3d.DrawsetInterface Drawset
		{
			get { return m_drawset; }
		}

		/// <summary>Export the current diagram as xml</summary>
		public XElement ExportXml(XElement node)
		{
			// Node styles
			m_node_styles.RemoveUnused(Elements);
			node.Add2(XmlField.NodeStyles, m_node_styles ,false);
			
			// Connector styles
			m_connector_styles.RemoveUnused(Elements);
			node.Add2(XmlField.ConnStyles, m_connector_styles, false);
			
			// Nodes
			foreach (var elem in Elements.Where(x => x.Entity == Entity.Node))
				node.Add2(XmlField.Element, elem, true);

			// Labels
			foreach (var elem in Elements.Where(x => x.Entity == Entity.Label))
				node.Add2(XmlField.Element, elem, true);

			// Connectors
			foreach (var elem in Elements.Where(x => x.Entity == Entity.Connector))
				node.Add2(XmlField.Element, elem, true);

			return node;
		}
		public XDocument ExportXml()
		{
			var xml = new XDocument();
			var node = xml.Add2(new XElement(XmlField.Root));
			ExportXml(node);
			return xml;
		}

		/// <summary>
		/// Import the diagram from xml.
		/// If 'merge' is false, the diagram contents are replaced with the data from 'node'
		/// If true, the element data is copied from 'node' where Ids match. Unmatched Ids are added.</summary>
		public void ImportXml(XElement node, bool merge = false)
		{
			// Build a map of Guids to elements
			var map = merge
				? Elements.ToDictionary(x => x.Id)
				: new Dictionary<Guid,Element>();

			// Add a map function for AnchorPoint that references the elements being imported
			using (Scope.Create(
				() => XmlExtensions.AsMap[typeof(AnchorPoint)] = (elem, type, ctor) => new AnchorPoint(map, elem),
				() => XmlExtensions.AsMap[typeof(AnchorPoint)] = null))
			{
				foreach (var n in node.Elements())
				{
					switch (n.Name.LocalName)
					{
					case XmlField.NodeStyles:
						{
							var node_styles = n.As<StyleCache<NodeStyle>>();
							if (!merge)
								m_node_styles = node_styles ?? new StyleCache<NodeStyle>();
							else if (node_styles != null)
								m_node_styles.Merge(node_styles);
							break;
						}
					case XmlField.ConnStyles:
						{
							var conn_styles = n.As<StyleCache<ConnectorStyle>>();
							if (!merge)
								m_connector_styles = conn_styles ?? new StyleCache<ConnectorStyle>();
							else if (conn_styles != null)
								m_connector_styles.Merge(conn_styles);
							break;
						}
					case XmlField.Element:
						if (!merge)
						{
							var elem = (Element)n.ToObject();
							map.Add(elem.Id, elem);
						}
						else
						{
							// Read the id of the element and look for it amoung the existing elements
							var id = n.Element(XmlField.Id).As<Guid>();
							Element elem = map.TryGetValue(id, out elem) ? elem : null;

							// If not found, add a new element
							if (elem == null)
							{
								elem = (Element)n.ToObject();
								map.Add(elem.Id, elem);
								Elements.Add(elem);
							}

							// If found, update the existing element.
							else
								elem.Update(n);
						}
						break;
					}
				}
			}

			// Add 'elements' to the diagram
			if (!merge)
			{
				ResetDiagram();
				Elements.AddRange(map.Values);
			}

			// Remove unused styles
			m_node_styles.RemoveUnused(Elements);
			m_connector_styles.RemoveUnused(Elements);
		}

		/// <summary>
		/// Import the diagram layout from a string containing xml.
		/// If 'merge' is false, the diagram contents are replaced with the data from 'node'
		/// If true, the element data is copied from 'node' where Ids match. Unmatched Ids are added.</summary>
		public void ImportXml(string layout_xml, bool merge = false)
		{
			var xml = XDocument.Parse(layout_xml);
			if (xml.Root == null) throw new InvalidDataException("xml file does not contain any config data");
			ImportXml(xml.Root, merge);
		}

		/// <summary>
		/// Import the diagram layout from an xml file.
		/// If 'merge' is false, the diagram contents are replaced with the data from 'node'
		/// If true, the element data is copied from 'node' where Ids match. Unmatched Ids are added.</summary>
		public void LoadXml(string filepath, bool merge = false)
		{
			var xml = XDocument.Load(filepath);
			if (xml.Root == null) throw new InvalidDataException("xml file does not contain any config data");
			ImportXml(xml.Root, merge);
		}

		/// <summary>Create and display a context menu</summary>
		public void ShowContextMenu(Point location)
		{
			// Note: using lists and AddRange here for performance reasons
			// Using 'DropDownItems.Add' causes lots of PerformLayout calls
			var context_menu = new ContextMenuStrip { Renderer = new ContextMenuRenderer() };

			// Helper for adding sections to the context menu
			var section = 0;
			var lvl0 = new List<ToolStripItem>();
			Func<List<ToolStripItem>,int,ToolStripItem,ToolStripItem> add = (list,sect,item) =>
				{
					if (sect != section)
					{
						if (list.Count != 0) list.Add(new ToolStripSeparator());
						section = sect;
					}
					return list.Add2(item);
				};

			using (this.ChangeCursor(Cursors.WaitCursor))
			using (context_menu.SuspendLayout(false))
			{
				// Allow users to add menu options
				AddUserMenuOptions.Raise(this, new AddUserMenuOptionsEventArgs(context_menu));

				if (AllowEditing)
				{
					#region Create Link
					if (SingleSelection != null && SingleSelection.Entity == Entity.Node)
					{
						var create_link = add(lvl0, 1, new ToolStripMenuItem("Create Link"));
						create_link.Click += (s,a) =>
							{
								// Start the link create mouse op
								m_mouse_op[(int)EBtnIdx.None] = new MouseOpCreateLink(this, SingleSelection.As<Node>(), true);
							};
					}
					#endregion
				}

				if (AllowMove)
				{
					#region Scatter
					var scatter = add(lvl0,2,new ToolStripMenuItem("Scatter"));
					scatter.Click += (s,a) => ScatterNodes();
					#endregion

					#region Relink
					var relink = add(lvl0,2,new ToolStripMenuItem("Relink"));
					relink.Click += (s,a) => RelinkNodes();
					#endregion

					#region Bring to Front
					if (Selected.Count != 0)
					{
						var tofront = add(lvl0,2,new ToolStripMenuItem("Bring to Front"));
						tofront.Click += (s,a) =>
							{
								foreach (var elem in Selected.ToArray())
									elem.BringToFront();
							};
					}
					#endregion

					#region Send to Back
					if (Selected.Count != 0)
					{
						var toback = add(lvl0,2,new ToolStripMenuItem("Send to Back"));
						toback.Click += (s,a) =>
							{
								foreach (var elem in Selected.ToArray())
									elem.SendToBack();
							};
					}
					#endregion
				}

				if (AllowSelection)
				{
					#region Selection
					if (Selected.Count != 0)
					{
						var select_connected = add(lvl0,3,new ToolStripMenuItem("Select Connected Nodes"));
						select_connected.Click += (s,a) =>
							{
								SelectConnectedNodes();
							};
					}
					#endregion
				}

				if (AllowEditing)
				{
					#region Properties
					var props = add(lvl0,4,new ToolStripMenuItem(Selected.Count != 0 ? "Properties" : "Default Properties"));
					props.Click += (s,a) =>
						{
							EditProperties();
						};
					#endregion

					#region Use Default Properties
					if (Selected.Count != 0)
					{
						var usedef = add(lvl0,4,new ToolStripMenuItem("Use Default Properties"));
						usedef.Click += (s,a) =>
							{
								foreach (var elem in Selected.ToArray())
								{
									if (elem.Entity == Entity.Node)
										elem.As<Node>().Style = NodeStyle.Default;
									if (elem.Entity == Entity.Connector)
										elem.As<Connector>().Style = ConnectorStyle.Default;
								}
								Refresh();
							};
					}
					#endregion
				}

				context_menu.Items.AddRange(lvl0.ToArray());
			}

			context_menu.Closed += (s,a) => Refresh();
			context_menu.Show(this, location);
		}

		/// <summary>Display a dialog for editing properties of selected elements</summary>
		private void EditProperties()
		{
			// Ensure all selected elements have a unique style object
			foreach (var elem in Selected)
			{
				var node = elem as Node;
				if (node != null && node.Style == NodeStyle.Default)
					node.Style = (NodeStyle)NodeStyle.Default.Clone();

				var conn = elem as Connector;
				if (conn != null && conn.Style == ConnectorStyle.Default)
					conn.Style = (ConnectorStyle)ConnectorStyle.Default.Clone();
			}

			var form = new Form
			{
				StartPosition = FormStartPosition.CenterParent,
				ShowInTaskbar = false,
			};
			var props = new PropertyGrid
			{
				Dock = DockStyle.Fill,
			};

			if (Selected.Count != 0)
			{
				form.Text = "Properties";
				props.SelectedObjects = Selected
					.Where(x => x.Entity == Entity.Node || x.Entity == Entity.Connector)
					.Cast<object>()
					.Select(x => x is Node ? (object)((Node)x).Style : (object)((Connector)x).Style)
					.ToArray();
			}
			else
			{
				form.Text = "Default Properties";
				props.SelectedObject = new DefaultStyles();
			}

			form.Controls.Add(props);
			form.ShowDialog(this);
			Refresh();
		}

		/// <summary>Check the self consistency of elements</summary>
		public bool CheckConsistency()
		{
			// Take a copy to prevent events being raised
			var elements = Elements.ToArray();

			// The elements collection should be distinct
			elements.Sort(ByGuid);
			for (int i = 0; i < elements.Length - 1; ++i)
			{
				if (Equals(elements[i].Id, elements[i+1].Id))
					throw new Exception("Element {0} is in the Elements collection more than once".Fmt(elements[i].ToString()));
			}

			// All elements in the diagram should have their Diagram property set to this diagram
			foreach (var elem in Elements)
			{
				if (elem.Diagram == this) continue;
				throw new Exception("Element {0} is in the Elements collection but does not have its Diagram property set correctly".Fmt(elem.ToString()));
			}

			// The selected collection contains all that is selected and no more
			var selected0 = elements.Where(x => x.Selected).ToArray();
			var selected1 = Selected.ToArray();
			selected0.Sort(ByGuid);
			selected1.Sort(ByGuid);
			if (!selected0.SequenceEqual(selected1, ByGuid))
				throw new Exception("Selected elements collection is inconsistent with the selected state of the elements");

			// Check the consistency of all elements
			foreach (var elem in Elements)
				elem.CheckConsistency();

			return true;
		}

		/// <summary>Element sorting predicates</summary>
		private Cmp<Element> ByGuid   = Cmp<Element>.From((l,r) => l.Id.CompareTo(r.Id));

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
