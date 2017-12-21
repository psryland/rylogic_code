using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using pr.common;
using pr.container;
using pr.extn;
using pr.gfx;
using pr.ldr;
using pr.maths;
using pr.util;
using pr.view3d;

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

		/// <summary>Says "I have a GUID"</summary>
		private interface IHasId
		{
			Guid Id { get; }
		}

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
			protected Element(Entity entity, Guid id, m4x4 position)
			{
				Entity          = entity;
				Id              = id;
				m_impl_diag     = null;
				m_impl_position = position;
				m_impl_selected = false;
				m_impl_visible  = true;
				m_impl_enabled  = true;
				EditControl     = null;
				UserData        = new Dictionary<Guid, object>();
			}
			protected Element(Entity entity, XElement node)
			{
				Diagram     = null;
				Entity      = entity;
				Id          = node.Element(XmlTag.Id).As<Guid>();
				Position    = node.Element(XmlTag.Position).As<m4x4>();
				Selected    = false;
				Visible     = true;
				Enabled     = true;
				EditControl = null;
				UserData    = new Dictionary<Guid, object>();
			}
			public void Dispose()
			{
				Dispose(true); // Mimic the WinForms Control disposing pattern
			}
			protected bool Disposed { get; private set; }
			protected virtual void Dispose(bool disposing)
			{
				Util.BreakIf(!Disposed && Util.IsGCFinalizerThread);
				Diagram = null;
				Invalidated = null;
				PositionChanged = null;
				DataChanged = null;
				SelectedChanged = null;
				Disposed = true;
			}

			/// <summary>Get the entity type for this element</summary>
			public Entity Entity { get; private set; }

			/// <summary>Non-null when the element has been added to a diagram. Not virtual, override 'SetDiagramInternal' instead</summary>
			public DiagramControl Diagram
			{
				get { return m_impl_diag; }
				set { SetDiagramInternal(value, true); }
			}
			internal void SetDiagramInternal(DiagramControl diag, bool update_diagram)
			{
				// An Element can be added to a diagram by assigning to the Diagram property
				// or by adding it to the Elements collection. It can be removed by setting
				// this property to null, by removing it from the Elements collection, or
				// by Disposing the element. Note: the diagram does not own the elements, 
				// elements should only be disposed by the caller.
				if (m_impl_diag == diag) return;

				// Detach from the old diagram
				if (m_impl_diag != null && update_diagram)
				{
					m_impl_diag.Elements.Remove(this);
				}

				// Assign the new diagram
				SetDiagramCore(diag);

				// Attach to the new diagram
				if (m_impl_diag != null && update_diagram)
				{
					m_impl_diag.Elements.Add(this);
				}

				Debug.Assert(CheckConsistency());
				Invalidate();
			}
			private DiagramControl m_impl_diag;

			/// <summary>Add or remove this element from 'diag'</summary>
			public virtual void SetDiagramCore(DiagramControl diag)
			{
				// Note: don't suspend events on m_diag.Elements.
				// User code maybe watching for ListChanging events.
				Debug.Assert(!Disposed);

				// Remove this element from any selected collection on the outgoing diagram
				// This also clears the 'selected' state for the element
				Selected = false;

				// Ensure this element isn't in the dirty list on the out-going diagram
				m_impl_diag?.m_dirty.Remove(this);

				// Set the new diagram
				m_impl_diag = diag;
			}

			/// <summary>Unique id for this element</summary>
			public Guid Id { get; set; }

			/// <summary>Export to XML</summary>
			public virtual XElement ToXml(XElement node)
			{
				node.Add2(XmlTag.Id       ,Id       ,false);
				node.Add2(XmlTag.Position ,Position ,false);
				return node;
			}

			/// <summary>Import from XML. Used to update the state of this element without having to delete/recreate it</summary>
			protected virtual void FromXml(XElement node)
			{
				Position = node.Element(XmlTag.Position).As<m4x4>(Position);
			}

			/// <summary>Replace the contents of this element with data from 'node'</summary>
			internal void Update(XElement node)
			{
				// Don't validate the TypeAttribute as users may have sub-classed the element
				using (SuspendEvents())
					FromXml(node);

				Invalidate();
			}

			/// <summary>Indicate a render is needed. Note: doesn't call Render</summary>
			public void Invalidate(object sender = null, EventArgs args = null)
			{
				if (Diagram == null || Diagram.m_dirty.Contains(this)) return;
				Diagram.m_dirty.Add(this);
				Invalidated.Raise(this);
			}

			/// <summary>RAII object for suspending events on this node</summary>
			public Scope SuspendEvents()
			{
				return Scope.Create(
					() =>
					{
						return new[]
						{
							Invalidated.Suspend(),
							DataChanged.Suspend(),
							SelectedChanged.Suspend(),
							PositionChanged.Suspend(),
							SizeChanged.Suspend(),
						};
					},
					arr =>
					{
						Util.DisposeAll(arr);
					});
			}

			/// <summary>Raised whenever the element needs to be redrawn</summary>
			public event EventHandler Invalidated;

			/// <summary>Raised whenever data associated with the element changes</summary>
			public event EventHandler DataChanged;
			protected virtual void OnDataChanged()
			{
				DataChanged.Raise(this, EventArgs.Empty);
				RaiseDiagramChanged();
			}

			/// <summary>Raised whenever the element is moved</summary>
			public event EventHandler PositionChanged;
			protected void OnPositionChanged()
			{
				PositionChanged.Raise(this);
				RaiseDiagramChanged();
			}

			/// <summary>Raised whenever the element changes size</summary>
			public event EventHandler SizeChanged;
			protected void OnSizeChanged()
			{
				SizeChanged.Raise(this, EventArgs.Empty);
				RaiseDiagramChanged();
			}

			/// <summary>Raised whenever the element is selected or deselected</summary>
			public event EventHandler SelectedChanged;
			protected virtual void OnSelectedChanged()
			{
				SelectedChanged.Raise(this);
			}

			/// <summary>Signal that the diagram needs laying out</summary>
			protected virtual void RaiseDiagramChanged()
			{
				if (Diagram == null) return;
				Diagram.RaiseDiagramChanged(new DiagramChangedEventArgs(EDiagramChangeType.Edited));
			}

			/// <summary>Get/Set the selected state</summary>
			public virtual bool Selected
			{
				get { return m_impl_selected; }
				set { SetSelectedInternal(value, true); }
			}
			internal void SetSelectedInternal(bool selected, bool update_selected_collection)
			{
				// This allows the diagram to set the selected state without
				// adding/removing from the diagrams 'Selected' collection.
				if (m_impl_selected == selected) return;
				if (m_impl_selected && update_selected_collection)
				{
					Diagram?.Selected.Remove(this);
				}

				m_impl_selected = selected;

				if (m_impl_selected && update_selected_collection)
				{
					// Selection state is changed by assigning to this property or by
					// addition/removal from the diagram's 'Selected' collection.
					Diagram?.Selected.Add(this);
				}

				// Notify observers about the selection change
				OnSelectedChanged();
				Invalidate();
			}
			private bool m_impl_selected;

			/// <summary>Get/Set whether this element is visible in the diagram</summary>
			public bool Visible
			{
				get { return m_impl_visible; }
				set
				{
					if (m_impl_visible == value) return;
					m_impl_visible = value;
					Invalidate();
				}
			}
			private bool m_impl_visible;

			/// <summary>Get/Set whether this element is enabled</summary>
			public bool Enabled
			{
				get { return m_impl_enabled; }
				set
				{
					if (m_impl_enabled == value) return;
					m_impl_enabled = value;
					Invalidate();
				}
			}
			private bool m_impl_enabled;

			/// <summary>Allow users to bind arbitrary data to the diagram element</summary>
			public IDictionary<Guid, object> UserData { get; private set; }

			/// <summary>Send this element to the bottom of the stack.</summary>
			public void SendToBack()
			{
				// Z order is determined by position in the Elements collection
				if (Diagram == null)
					return;

				// Save the diagram pointer because removing the element will remove
				// it from the Diagram causing 'Diagram' to become null before Insert is called
				var diag = Diagram;
				using (diag.Elements.SuspendEvents())
				{
					diag.Elements.Remove(this);
					diag.Elements.Insert(0, this);
				}
				diag.UpdateElementZOrder();
				diag.Invalidate();
			}

			/// <summary>Bring this element to top of the stack.</summary>
			public void BringToFront()
			{
				// Z order is determined by position in the Elements collection
				if (Diagram == null)
					return;

				// Save the diagram pointer because removing the element will remove
				// it from the Diagram causing 'Diagram' to become null before Insert is called
				var diag = Diagram;
				using (diag.Elements.SuspendEvents())
				{
					diag.Elements.Remove(this);
					diag.Elements.Add(this);
				}
				diag.UpdateElementZOrder();
				diag.Invalidate();
			}

			/// <summary>The element to diagram transform</summary>
			public m4x4 Position
			{
				get { return m_impl_position; }
				set
				{
					if (Maths.FEql(m_impl_position, value)) return;
					SetPosition(value);
				}
			}

			/// <summary>Get/Set the XY position of the element</summary>
			public v2 PositionXY
			{
				get { return Position.pos.xy; }
				set
				{
					var o2p = Position;
					Position = new m4x4(o2p.rot, new v4(value, o2p.pos.z, o2p.pos.w));
				}
			}

			/// <summary>Get/Set the z position of the element</summary>
			public float PositionZ
			{
				get { return Position.pos.z; }
				set
				{
					var o2p = Position;
					o2p.pos.z = value;
					Position = o2p;
				}
			}

			/// <summary>Position recorded at the time dragging starts</summary>
			internal m4x4 DragStartPosition { get; set; }

			/// <summary>Internal set position and raise event</summary>
			protected virtual void SetPosition(m4x4 pos)
			{
				m_impl_position = pos;
				OnPositionChanged();
			}
			private m4x4 m_impl_position;

			/// <summary>Get/Set the centre point of the element (in diagram space)</summary>
			public v2 Centre
			{
				get { return Bounds.Centre; }
				set { PositionXY = value + (PositionXY - Centre); }
			}

			/// <summary>AABB for the element in diagram space</summary>
			public virtual BRect Bounds
			{
				get { return new BRect(PositionXY, v2.Zero); }
			}

			/// <summary>True if this element can be resized</summary>
			public virtual bool Resizeable
			{
				get { return false; }
			}

			/// <summary>Perform a hit test on this object. Returns null for no hit. 'point' is in diagram space</summary>
			public virtual HitTestResult.Hit HitTest(v2 point, View3d.Camera cam)
			{
				return null;
			}

			/// <summary>Handle a click event on this element</summary>
			internal virtual bool HandleClicked(HitTestResult.Hit hit, View3d.Camera cam)
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

			/// <summary>
			/// Return the attachment location and normal nearest to 'pt'.
			/// 'existing' is the element already attached to the anchor (can be null)</summary>
			public virtual AnchorPoint NearestAnchor(v2 pt, bool pt_in_element_space, Element existing = null)
			{
				var point = new v4(pt, PositionZ, 1f);
				if (!pt_in_element_space)
					point = m4x4.InvertFast(Position) * point;

				return AnchorPoints().MinBy(x => (x.Location - point).Length3Sq);
			}

			/// <summary>Return all the locations that connectors can attach to this node (in node space)</summary>
			public virtual IEnumerable<AnchorPoint> AnchorPoints()
			{
				yield return new AnchorPoint(this, v4.Origin, v4.Zero);
			}

			/// <summary>Update the graphics and object transforms associated with this element</summary>
			public void UpdateGfx()
			{
				if (m_impl_updating_gfx) return; // Protect against reentrancy
				using (Scope.Create(() => m_impl_updating_gfx = true, () => m_impl_updating_gfx = false))
					UpdateGfxCore();
			}
			protected virtual void UpdateGfxCore() {}
			private bool m_impl_updating_gfx;

			/// <summary>Add the graphics associated with this element to the window</summary>
			internal void AddToScene(View3d.Window window)
			{
				AddToSceneCore(window);
			}
			protected virtual void AddToSceneCore(View3d.Window window)
			{
			}

			/// <summary>Edit the element contents</summary>
			public void Edit()
			{
				// Get the user control used to edit the node
				if (EditControl == null)
					return; // not editable

				// Create a form to host the control
				var edit = new EditField(this, EditControl);

				// Display modally
				EditControl.Init(this, edit);
				if (edit.ShowDialog(Diagram.ParentForm) == DialogResult.OK)
					EditControl.Commit(this);
			}

			/// <summary>A control wrapper use for editing the element. Set to null if not editable</summary>
			public EditingControl EditControl { get; set; }

			/// <summary>Check the self consistency of this element</summary>
			public virtual bool CheckConsistency()
			{
				return true;
			}
		}

		/// <summary>Base class for a rectangular resizeable element</summary>
		public abstract class ResizeableElement :Element
		{
			/// <summary>Base node constructor</summary>
			protected ResizeableElement(Entity entity, Guid id, uint width, uint height, m4x4 position)
				:base(entity, id, position)
			{
				m_impl_sizemax = v2.Zero;
				m_impl_size = new v2(width, height);
			}
			protected ResizeableElement(Entity entity, XElement node)
				:base(entity, node)
			{
				m_impl_sizemax = node.Element(XmlTag.SizeMax).As<v2>();
				m_impl_size = node.Element(XmlTag.Size).As<v2>();
			}

			/// <summary>Export to XML</summary>
			public override XElement ToXml(XElement node)
			{
				base.ToXml(node);
				node.Add2(XmlTag.SizeMax  ,SizeMax  ,false);
				node.Add2(XmlTag.Size     ,Size     ,false);
				return node;
			}
			protected override void FromXml(XElement node)
			{
				SizeMax = node.Element(XmlTag.SizeMax).As<v2>(SizeMax);
				Size    = node.Element(XmlTag.Size).As<v2>(Size);
				base.FromXml(node);
			}

			/// <summary>The diagram space width/height of the element</summary>
			public virtual v2 Size
			{
				get { return m_impl_size; }
				set
				{
					if (SizeMax.x != 0 && value.x > SizeMax.x) value.x = SizeMax.x;
					if (SizeMax.y != 0 && value.y > SizeMax.y) value.y = SizeMax.y;
					if (m_impl_size == value) return;
					m_impl_size = value;
					OnSizeChanged();
					Invalidate();
				}
			}
			private v2 m_impl_size;

			/// <summary>A flag that's true while an element is resizing</summary>
			internal virtual bool Resizing { get; set; }

			/// <summary>Get/Set limits for auto size</summary>
			public virtual v2 SizeMax
			{
				get { return m_impl_sizemax; }
				set
				{
					if (m_impl_sizemax == value) return;
					m_impl_sizemax = value;
					Size = Size;
				}
			}
			private v2 m_impl_sizemax;

			/// <summary>Returns the preferred size of the element</summary>
			public virtual v2 PreferredSize(v2 layout)
			{
				return Size;
			}
			public v2 PreferredSize(float max_width, float max_height)
			{
				return PreferredSize(new v2(max_width, max_height));
			}
			public v2 PreferredSize()
			{
				return PreferredSize(SizeMax);
			}

			/// <summary>AABB for the element in diagram space</summary>
			public override BRect Bounds
			{
				get { return new BRect(PositionXY, Size / 2); }
			}
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
			protected Node(Guid id, uint width, uint height, string text, m4x4 position, NodeStyle style)
				:base(Entity.Node, id, width, height, position)
			{
				m_impl_text = text;
				Style = style;

				Init();
			}
			protected Node(XElement node)
				:base(Entity.Node, node)
			{
				m_impl_text = node.Element(XmlTag.Text).As<string>();
				Style = new NodeStyle(node.Element(XmlTag.Style).As<Guid>());

				// Be careful using Style in here, it's only a place-holder
				// instance until the element has been added to a diagram.
				Init();
			}
			private void Init()
			{
				m_impl_text_fmt = new StringFormat(0);
				Connectors = new BindingListEx<Connector>();
				EditControl = DefaultEditControl;
			}
			protected override void Dispose(bool disposing)
			{
				DetachConnectors();
				Style = null;
				base.Dispose(disposing);
			}

			/// <summary>Change the diagram that this element is associated with</summary>
			public override void SetDiagramCore(DiagramControl diag)
			{
				base.SetDiagramCore(diag);

				// Detach from objects owned by the current diagram
				// Do this after the diagram has changed because changing the style causes
				// Invalidate() to be called, which adds this element to the dirty list on the new diagram.
				Style = new NodeStyle(Guid.Empty);
			}

			/// <summary>Export to XML</summary>
			public override XElement ToXml(XElement node)
			{
				base.ToXml(node);
				node.Add2(XmlTag.Text  ,Text     ,false);
				node.Add2(XmlTag.Style ,Style.Id ,false);
				return node;
			}
			protected override void FromXml(XElement node)
			{
				Style = new NodeStyle(node.Element(XmlTag.Style).As<Guid>(Style.Id));
				Text = node.Element(XmlTag.Text).As<string>(Text);
				base.FromXml(node);
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

					if (Style.AutoSize) PerformAutoSize();
					OnDataChanged();
					Invalidate();
				}
			}
			private string m_impl_text;

			/// <summary>How to layout the text in the node</summary>
			public StringFormat TextFormat
			{
				get { return m_impl_text_fmt; }
				set
				{
					if (m_impl_text_fmt == value) return;
					m_impl_text_fmt = value;

					if (Style.AutoSize) PerformAutoSize();
					Invalidate();
				}
			}
			private StringFormat m_impl_text_fmt;

			/// <summary>Get/Set the size of the node</summary>
			public override v2 Size
			{
				get { return base.Size; }
				set
				{
					// Only change the size if the node is resizeable, but save the value
					// for when the style changes to non-auto size.
					m_impl_size = value;
					if (Resizeable)
						base.Size = m_impl_size;
				}
			}
			private v2 m_impl_size;

			/// <summary>Resize the node if AutoSize is enabled, otherwise no-op</summary>
			public void PerformAutoSize()
			{
				base.Size = PreferredSize(SizeMax);
			}

			/// <summary>True if this element can be resized</summary>
			public override bool Resizeable
			{
				get { return !Style.AutoSize; }
			}

			/// <summary>Return the preferred node size given the current text and upper size bounds</summary>
			public override v2 PreferredSize(v2 layout)
			{
				if (!Text.HasValue())
					return Size;

				if (layout.x == 0f) layout.x = float.MaxValue;
				if (layout.y == 0f) layout.y = float.MaxValue;

				using (var img = new Bitmap(1,1,PixelFormat.Format32bppArgb))
				using (var gfx = Graphics.FromImage(img))
				{
					v2 sz = gfx.MeasureString(Text, Style.Font, layout.ToSizeF(), TextFormat);
					sz.x += Style.Padding.Left + Style.Padding.Right;
					sz.y += Style.Padding.Top + Style.Padding.Bottom;
					return sz;
				}
			}

			/// <summary>Style attributes for the node</summary>
			public NodeStyle Style
			{
				get { return Diagram != null ? (m_impl_style = Diagram.m_node_styles[m_impl_style]) : m_impl_style; }
				set
				{
					if (m_impl_style == value) return;
					if (m_impl_style != null)
					{
						m_impl_style.StyleChanged -= HandleStyleChanged;
					}
					value = value ?? NodeStyle.Default;
					m_impl_style = Diagram != null ? Diagram.m_node_styles[value] : value;
					if (m_impl_style != null)
					{
						m_impl_style.StyleChanged += HandleStyleChanged;
					}
					HandleStyleChanged();
				}
			}
			private void HandleStyleChanged(object sender = null, EventArgs args = null)
			{
				// Reassign the size to cause an auto size (if necessary)
				Size = m_impl_size;
				Invalidate();
			}
			private NodeStyle m_impl_style;
			IStyle IHasStyle.Style
			{
				get { return Style; }
			}

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
			protected RectangleF TextLocation(Graphics gfx)
			{
				var pad = Style.Padding;
				var bnd = Size.ToRectangleF().Inflated(-pad.Left, -pad.Top, -pad.Right, -pad.Bottom);
				var tx = gfx.MeasureString(Text, Style.Font, bnd.Size, TextFormat);
				switch (Style.TextAlign)
				{
				default: throw new ArgumentException("unknown text alignment");
				case ContentAlignment.TopLeft     : return new RectangleF(bnd.Left                                 , bnd.Top                                   , tx.Width , tx.Height);
				case ContentAlignment.TopCenter   : return new RectangleF(bnd.Left + (bnd.Width - tx.Width) * 0.5f , bnd.Top                                   , tx.Width , tx.Height);
				case ContentAlignment.TopRight    : return new RectangleF(bnd.Left + (bnd.Width - tx.Width)        , bnd.Top                                   , tx.Width , tx.Height);
				case ContentAlignment.MiddleLeft  : return new RectangleF(bnd.Left                                 , bnd.Top + (bnd.Height - tx.Height) * 0.5f , tx.Width , tx.Height);
				case ContentAlignment.MiddleCenter: return new RectangleF(bnd.Left + (bnd.Width - tx.Width) * 0.5f , bnd.Top + (bnd.Height - tx.Height) * 0.5f , tx.Width , tx.Height);
				case ContentAlignment.MiddleRight : return new RectangleF(bnd.Left + (bnd.Width - tx.Width)        , bnd.Top + (bnd.Height - tx.Height) * 0.5f , tx.Width , tx.Height);
				case ContentAlignment.BottomLeft  : return new RectangleF(bnd.Left                                 , bnd.Top + (bnd.Height - tx.Height)        , tx.Width , tx.Height);
				case ContentAlignment.BottomCenter: return new RectangleF(bnd.Left + (bnd.Width - tx.Width) * 0.5f , bnd.Top + (bnd.Height - tx.Height)        , tx.Width , tx.Height);
				case ContentAlignment.BottomRight : return new RectangleF(bnd.Left + (bnd.Width - tx.Width)        , bnd.Top + (bnd.Height - tx.Height)        , tx.Width , tx.Height);
				}
			}

			/// <summary>Gets a control to use for editing the element. Return null if not editable</summary>
			public static EditingControl DefaultEditControl
			{
				get
				{
					var tb = new TextBox
					{
						Multiline = true,
						BorderStyle = BorderStyle.None,
						Margin = Padding.Empty,
						ScrollBars = ScrollBars.Both,
					};
					Action<Element,Form> init = (elem,form) =>
					{
						tb.Text = ((Node)elem).Text;
						form.ClientSize = tb.PreferredSize;
						form.Height += 16;
					};
					Action<Element> commit = elem =>
					{
						((Node)elem).Text = tb.Text.Trim();
					};
					return new EditingControl(tb, init, commit);
				}
			}

			/// <summary>Remove this node from the connectors that reference it</summary>
			public void DetachConnectors()
			{
				while (!Connectors.Empty())
					Connectors.First().Remove(this);
			}

			/// <summary>Try to untangle the connectors to this node</summary>
			public void Untangle()
			{
				// Copy of the connectors connected to this node
				var connectors = Connectors.ToList();

				// For each connector pair, see if they cross
				for (int i =   0; i != connectors.Count; ++i)
				for (int j = i+1; j != connectors.Count; ++j)
				{
					var c0 = connectors[i];
					var c1 = connectors[j];
					var pt00 = c0.Anc0.LocationDS.xy;
					var pt01 = c0.Anc1.LocationDS.xy;
					var pt10 = c1.Anc0.LocationDS.xy;
					var pt11 = c1.Anc1.LocationDS.xy;

					float t0,t1;
					Geometry.ClosestPoint(pt00, pt01, pt10, pt11, out t0, out t1);
					if (t0 != 0f && t0 != 1f && t1 != 0f && t1 != 1f)
					{
						// Swap location
						var loc = c0.Anc(this).Location;
						c1.Anc(this).Location = c0.Anc(this).Location;
						c0.Anc(this).Location = loc;

						// Swap normal
						var norm = c0.Anc(this).Normal;
						c1.Anc(this).Normal = c0.Anc(this).Normal;
						c0.Anc(this).Normal = norm;
					}
				}

				//// The anchor points used by those connectors
				//var anchors = Connectors.Select(x => x.Anc(this));

				//// For each used anchor point, find the connector
				//// that will be the shortest if it uses this anchor
				//foreach (var anc in anchors)
				//{
				//	var idx = connectors.IndexOfMinBy(x => (anc.Location - x.OtherAnc(this).Location).Length2Sq);
				//	var conn_anc = connectors[idx].Anc(this);
				//	conn_anc.Location = anc.Location;
				//	conn_anc.Normal = anc.Normal;
				//	connectors.RemoveAt(idx);
				//	if (connectors.Count == 0)
				//		break;
				//}

				//// For the remaining connectors, 
				//while (connectors.Count != 0)
				//{

				//}
			}

			/// <summary>Check the self consistency of this element</summary>
			public override bool CheckConsistency()
			{
				// The Connectors collection in the node should contain a connector that references this node exactly once
				foreach (var conn in Connectors)
				{
					if (conn.Node0 != this && conn.Node1 != this)
						throw new Exception("Node {0} contains connector {1} but is not referenced by the connector".Fmt(ToString(), conn.ToString()));
					
					// Allow the connectors to be connected to this node at both ends
					//if (conn.Node0 == this && conn.Node1 == this)
					//	throw new Exception("Node {0} contains connector {1} that is attached to it at both ends".Fmt(ToString(), conn.ToString()));
				}

				// The style should be known to the diagram
				if (Diagram != null)
				{
					if (!Diagram.m_node_styles.ContainsKey(Style.Id))
						throw new Exception("Node {0} style is not in the diagram's style cache".Fmt(ToString()));
				}
				return base.CheckConsistency();
			}

			/// <summary></summary>
			public override string ToString()
			{
				return "Node["+Text.Summary(20)+"]";
			}
		}

		/// <summary>
		/// An invisible 'node-like' object used for detaching/attaching connectors.
		/// Public so that sub-classed controls can detect and exclude it</summary>
		public class NodeProxy :Node
		{
			private readonly AnchorPoint m_anchor_point;

			public NodeProxy()
				:base(Guid.NewGuid(), 20, 20, "ProxyNode", m4x4.Identity, new NodeStyle(Guid.Empty))
			{
				m_anchor_point = new AnchorPoint(this, v4.Origin, v4.Zero);
			}

			/// <summary>Get the single anchor point of the proxy node</summary>
			public AnchorPoint Anchor
			{
				get { return m_anchor_point; }
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

			/// <summary>Signal that the diagram needs laying out</summary>
			protected override void RaiseDiagramChanged()
			{
				// Don't raise diagram changed for the proxy node.
			}
			
			/// <summary>Return the preferred node size given the current text and upper size bounds</summary>
			public override v2 PreferredSize(v2 layout)
			{
				return new v2(20, 20);
			}

			/// <summary>Perform a hit test on this object. Returns null for no hit. 'point' is in diagram space</summary>
			public override HitTestResult.Hit HitTest(v2 point, View3d.Camera cam)
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
			protected override void UpdateGfxCore()
			{}

			/// <summary>Add the graphics associated with this element to the window</summary>
			protected override void AddToSceneCore(View3d.Window window)
			{}
		}

		/// <summary>Simple rectangular box node</summary>
		public class BoxNode :Node
		{
			private const string LdrName = "node";

			public BoxNode()
				:this(Guid.NewGuid())
			{}
			public BoxNode(Guid id)
				:this(id, "")
			{}
			public BoxNode(string text)
				:this(Guid.NewGuid(), text)
			{}
			public BoxNode(Guid id, string text)
				:this(id, text, 80, 30)
			{}
			public BoxNode(Guid id, string text, uint width, uint height)
				:this(id, text, width, height, Color.WhiteSmoke, Color.Black, 5f)
			{}
			public BoxNode(Guid id, string text, uint width, uint height, Color bkgd, Color border, float corner_radius)
				:this(id, text, width, height, bkgd, border, corner_radius, m4x4.Identity, new NodeStyle(Guid.Empty))
			{}
			public BoxNode(Guid id, string text, uint width, uint height, Color bkgd, Color border, float corner_radius, m4x4 position, NodeStyle style)
				:base(id, width, height, text, position, style)
			{
				var sz = Style.AutoSize ? PreferredSize(SizeMax) : Size;
				Gfx = new TexturedShape<QuadShape>(new QuadShape(corner_radius), sz);
				Size = sz;
			}
			public BoxNode(XElement node)
				:base(node)
			{
				var corner_radius = node.Element(XmlTag.CornerRadius).As<float>();
				Gfx = new TexturedShape<QuadShape>(new QuadShape(corner_radius), Size);
			}
			protected override void Dispose(bool disposing)
			{
				Gfx = null;
				base.Dispose(disposing);
			}

			/// <summary>Export to XML</summary>
			public override XElement ToXml(XElement node)
			{
				base.ToXml(node);
				node.Add2(XmlTag.CornerRadius ,CornerRadius, false);
				return node;
			}
			protected override void FromXml(XElement node)
			{
				CornerRadius = node.Element(XmlTag.CornerRadius).As<float>(CornerRadius);
				base.FromXml(node);
			}

			/// <summary>Only one graphics object for a box node</summary>
			protected TexturedShape<QuadShape> Gfx
			{
				get { return m_impl_gfx; }
				private set
				{
					if (m_impl_gfx == value) return;
					Util.Dispose(ref m_impl_gfx);
					m_impl_gfx = value;
				}
			}
			private TexturedShape<QuadShape> m_impl_gfx;

			/// <summary>The radius of the box node corners</summary>
			public float CornerRadius
			{
				get { return Gfx.Shape.CornerRadius; }
				set { Gfx.Shape.CornerRadius = value; Invalidate(); }
			}

			/// <summary>Set the position of the element</summary>
			protected override void SetPosition(m4x4 pos)
			{
				base.SetPosition(pos);
				if (Gfx != null) Gfx.O2P = pos;
			}

			/// <summary>Get/Set the size of the node</summary>
			public override v2 Size
			{
				get { return base.Size; }
				set
				{
					// Don't guard for same size here, because base.Size implements auto size.
					// Also, Size can be assigned during the base class constructor.
					base.Size = value;
					if (Gfx != null && Gfx.Size != Size)
					{
						Gfx.Size = Size;
						UpdateNodeTexture();
					}
				}
			}

			/// <summary>A flag that's true while an element is resizing</summary>
			internal override bool Resizing
			{
				get { return base.Resizing; }
				set
				{
					base.Resizing = value;
					Size = Size;
				}
			}

			/// <summary>Perform a hit test on this element. Returns null for no hit. 'point' is in diagram space</summary>
			public override HitTestResult.Hit HitTest(v2 point, View3d.Camera cam)
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
				float units_per_anchor = Diagram != null ? Diagram.Options.Node.AnchorSpacing : 10f;

				// Get the dimensions and half dimensions
				var sz = Size;
				var hsx = Math.Max(0f, (sz.x - 2*CornerRadius - units_per_anchor)/2f);
				var hsy = Math.Max(0f, (sz.y - 2*CornerRadius - units_per_anchor)/2f);

				// Find how many anchors will fit along x
				var divx = (int)(hsx / units_per_anchor);
				var divy = (int)(hsy / units_per_anchor);

				var x = sz.x / 2f;
				var y = sz.y / 2f;
				var z = PositionZ;
				yield return new AnchorPoint(this, new v4(0, +y, z, 1), +v4.YAxis);
				yield return new AnchorPoint(this, new v4(0, -y, z, 1), -v4.YAxis);
				yield return new AnchorPoint(this, new v4(+x, 0, z, 1), +v4.XAxis);
				yield return new AnchorPoint(this, new v4(-x, 0, z, 1), -v4.XAxis);

				for (int i = 0; i != divx; ++i)
				{
					var dx = Maths.Lerp(0f, hsx, (i+1f) / divx);
					yield return new AnchorPoint(this, new v4(0-dx, -y, z, 1), -v4.YAxis);
					yield return new AnchorPoint(this, new v4(0-dx, +y, z, 1), +v4.YAxis);
					yield return new AnchorPoint(this, new v4(0+dx, -y, z, 1), -v4.YAxis);
					yield return new AnchorPoint(this, new v4(0+dx, +y, z, 1), +v4.YAxis);
				}
				for (int i = 0; i != divy; ++i)
				{
					var dy = Maths.Lerp(0f, hsy, (i+1f) / divy);
					yield return new AnchorPoint(this, new v4(-x, 0-dy, z, 1), -v4.XAxis);
					yield return new AnchorPoint(this, new v4(+x, 0-dy, z, 1), +v4.XAxis);
					yield return new AnchorPoint(this, new v4(-x, 0+dy, z, 1), -v4.XAxis);
					yield return new AnchorPoint(this, new v4(+x, 0+dy, z, 1), +v4.XAxis);
				}
			}

			/// <summary>Return the attachment location and normal nearest to 'pt'.</summary>
			public override AnchorPoint NearestAnchor(v2 pt, bool pt_in_element_space, Element existing)
			{
				var point = new v4(pt, PositionZ, 1f);
				if (!pt_in_element_space)
					point = m4x4.InvertFast(Position) * point;

				float bias_distance_sq = Maths.Sqr(Diagram != null ? Diagram.Options.Node.AnchorSharingBias : 150f);

				// Select the nearest anchor, but bias the anchor points that are already used
				var used = Connectors.Where(x => x != existing).Select(x => x.Anc(this).Location).ToArray();
				return AnchorPoints().MinBy(x => (x.Location - point).Length2Sq + (used.Any(v => Maths.FEql(v.xy, x.Location.xy)) ? bias_distance_sq : 0));
			}

			/// <summary>Update the graphics and object transforms associated with this element</summary>
			protected override void UpdateGfxCore()
			{
				Gfx.O2P = Position;
				UpdateNodeTexture();
				base.UpdateGfxCore();
			}

			/// <summary>Add the graphics associated with this element to the window</summary>
			protected override void AddToSceneCore(View3d.Window window)
			{
				window.AddObject(Gfx);
			}

			/// <summary>Raised whenever data associated with the element changes</summary>
			protected override void OnDataChanged()
			{
				base.OnDataChanged();
				UpdateNodeTexture();
			}

			/// <summary>Update the texture surface. Default implementation renders the text</summary>
			protected virtual void UpdateNodeTexture()
			{
				using (var tex = Gfx.LockSurface(discard:true))
				{
					tex.Gfx.CompositingMode    = CompositingMode.SourceOver;
					tex.Gfx.CompositingQuality = CompositingQuality.HighQuality;
					tex.Gfx.SmoothingMode      = SmoothingMode.AntiAlias;

					FillBackground(tex);
					DrawText(tex);
				}
			}
			protected void FillBackground(View3d.Texture.Lock tex)
			{
				var fill_colour = Style.Fill;
				if (Selected) fill_colour = Style.Selected;
				if (!Enabled) fill_colour = Style.Disabled;
				tex.Gfx.Clear(fill_colour);
			}
			protected void DrawText(View3d.Texture.Lock tex)
			{
				var text_colour = Style.Text;
				if (!Enabled) text_colour = Style.TextDisabled;
				var fmt = new StringFormat(0);
				using (var bsh = new SolidBrush(text_colour))
					tex.Gfx.DrawString(Text, Style.Font, bsh, TextLocation(tex.Gfx), TextFormat);
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
				:base(Entity.Label, id, position)
			{
				m_anc = new AnchorPoint();
			}
			public Label(XElement node)
				:base(Entity.Label, node)
			{
				m_anc = new AnchorPoint();
				Anc = node.Element(XmlTag.Anchor).As<AnchorPoint>();
			}

			/// <summary>Export to XML</summary>
			public override XElement ToXml(XElement node)
			{
				base.ToXml(node);
				node.Add2(XmlTag.Anchor, Anc, false);
				return node;
			}
			protected override void FromXml(XElement node)
			{
				base.FromXml(node);
				Anc = node.Element(XmlTag.Anchor).As<AnchorPoint>(Anc);
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
					}
					Anc.Elem = value;
					if (Elem != null)
					{
						if (Elem is IHasStyle) ((IHasStyle)Elem).Style.StyleChanged += HandleElementMoved;
						Elem.PositionChanged += HandleElementMoved;
					}
				}
			}

			/// <summary>Update the anchor position whenever the parent element moves</summary>
			private void HandleElementMoved(object sender = null, EventArgs args = null)
			{
				// Need to call update on the anchor because the anchor can move on the parent element
				Anc.Update(this);
				Position = m4x4.Translation(Anc.LocationDS.xy, PositionZ);
				Invalidate();
			}

			/// <summary>Update the graphics and object transforms associated with this element</summary>
			protected override void UpdateGfxCore()
			{
				// Ensure the attached element is refreshed first, since our position depends on it
				if (Elem != null) Elem.UpdateGfx();
				base.UpdateGfxCore();
			}
		}

		/// <summary>Base class for labels containing a texture</summary>
		public class TexturedLabel<TShape> :Label where TShape :IShape, new()
		{
			private TexturedShape<TShape> m_gfx;

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
			public TexturedLabel(XElement node)
				:base(node)
			{
				m_gfx = node.Element(XmlTag.TexturedShape).As<TexturedShape<TShape>>();
			}
			protected override void Dispose(bool disposing)
			{
				if (m_gfx != null) m_gfx.Dispose();
				m_gfx = null;
				base.Dispose(disposing);
			}

			/// <summary>Export to XML</summary>
			public override XElement ToXml(XElement node)
			{
				base.ToXml(node);
				node.Add2(XmlTag.TexturedShape, m_gfx, false);
				return node;
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
			public View3d.Texture.Lock LockSurface(bool discard)
			{
				return m_gfx.LockSurface(discard);
			}

			/// <summary>Update the graphics and object transforms associated with this element</summary>
			protected override void UpdateGfxCore()
			{
				base.UpdateGfxCore();
				m_gfx.O2P = Position;
			}

			/// <summary>Perform a hit test on this element. Returns null for no hit. 'point' is in diagram space</summary>
			public override HitTestResult.Hit HitTest(v2 point, View3d.Camera cam)
			{
				var bounds = Bounds;
				if (!bounds.IsWithin(point)) return null;
				point -= bounds.Centre;

				var hit = new HitTestResult.Hit(this, point);
				return hit;
			}

			/// <summary>Add the graphics associated with this element to the window</summary>
			protected override void AddToSceneCore(View3d.Window window)
			{
				window.AddObject(m_gfx);
			}
		}

		#endregion

		#region Connectors

		/// <summary>
		/// A base class for connectors between elements.
		/// Connectors have two ends (nodes), either of which is allowed to be null.
		/// AnchorPoints are never null however.</summary>
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
				:this(id, node0, node1, new ConnectorStyle(Guid.Empty))
			{}
			public Connector(Guid id, Node node0, Node node1, ConnectorStyle style)
				:base(Entity.Connector, id, m4x4.Translation(AttachCentre(node0, node1), 0f))
			{
				m_anc0 = new AnchorPoint();
				m_anc1 = new AnchorPoint();
				m_centre_offset = v4.Zero;
				Style = style;
				Node0 = node0;
				Node1 = node1;

				Init(false);
			}
			public Connector(XElement node)
				:base(Entity.Connector, node)
			{
				m_anc0          = new AnchorPoint();
				m_anc1          = new AnchorPoint();
				m_centre_offset = node.Element(XmlTag.CentreOffset).As<v4>(v4.Zero);
				Anc0            = node.Element(XmlTag.Anchor0).As<AnchorPoint>();
				Anc1            = node.Element(XmlTag.Anchor1).As<AnchorPoint>();
				Type            = node.Element(XmlTag.Type).As<EType>();
				Style           = new ConnectorStyle(node.Element(XmlTag.Style).As<Guid>());

				Init(true);
			}
			private void Init(bool find_previous_anchors)
			{
				// Create graphics for the connector
				m_gfx_line = new View3d.Object("*Group{}", false, Id);
				m_gfx_fwd = new View3d.Object("*Triangle conn_fwd FFFFFFFF {1.5 0 0  -0.5 +1.2 0  -0.5 -1.2 0}", false, Id);
				m_gfx_bak = new View3d.Object("*Triangle conn_bak FFFFFFFF {1.5 0 0  -0.5 +1.2 0  -0.5 -1.2 0}", false, Id);

				Relink(find_previous_anchors);
			}
			protected override void Dispose(bool disposing)
			{
				DetachNodes();
				Style = null;
				Util.Dispose(ref m_gfx_line);
				Util.Dispose(ref m_gfx_fwd);
				Util.Dispose(ref m_gfx_bak);
				base.Dispose(disposing);
			}

			/// <summary>Change the diagram that this element is associated with</summary>
			public override void SetDiagramCore(DiagramControl diag)
			{
				base.SetDiagramCore(diag);

				// Detach from objects owned by the current diagram
				// Do this after the diagram has changed because changing the style causes
				// Invalidate() to be called, which adds this element to the dirty list on the new diagram.
				Style = new ConnectorStyle(Guid.Empty);
			}

			/// <summary>Export to XML</summary>
			public override XElement ToXml(XElement node)
			{
				base.ToXml(node);
				node.Add2(XmlTag.Anchor0 ,Anc0    , false);
				node.Add2(XmlTag.Anchor1 ,Anc1    , false);
				node.Add2(XmlTag.Type    ,Type    , false);
				node.Add2(XmlTag.Style   ,Style.Id, false);
				return node;
			}
			protected override void FromXml(XElement node)
			{
				Style    = new ConnectorStyle(node.Element(XmlTag.Style).As<Guid>(Style.Id));
				Anc0     = node.Element(XmlTag.Anchor0).As<AnchorPoint>(Anc0);
				Anc1     = node.Element(XmlTag.Anchor1).As<AnchorPoint>(Anc1);
				base.FromXml(node);
			}

			/// <summary>The 'from' anchor. Note: assigning to this anchor changes it's content, not the instance</summary>
			public AnchorPoint Anc0
			{
				get { return m_anc0; }
				set
				{
					Debug.Assert(value != null);
					Node0 = (Node)value.Elem;
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
					Debug.Assert(value != null);
					Node1 = (Node)value.Elem;
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

			/// <summary>Get the anchor point not associated with 'node'</summary>
			public AnchorPoint OtherAnc(Node node)
			{
				return
					Node0 == node ? Anc1 :
					Node1 == node ? Anc0 :
					null;
			}

			/// <summary>The 'from' node. Nodes can be null, implying the connector is connected to the diagram</summary>
			public Node Node0
			{
				get { return (Node)Anc0.Elem; }
				set
				{
					// Allowing this now
					//Debug.Assert(Node1 == null || Node1 != value, "Don't allow connections between anchors on the same element");
					if (Anc0.Elem == value) return;
					if (Node0 != null)
					{
						Node0.SizeChanged -= Relink;
						Node0.PositionChanged -= Invalidate;
						Node0.Connectors.Remove(this);
					}
					Anc0.Elem = value;
					if (Node0 != null)
					{
						Node0.Connectors.Add(this);
						Node0.PositionChanged += Invalidate;
						Node0.SizeChanged += Relink;
					}
				}
			}

			/// <summary>The 'to' node. Nodes can be null, implying the connector is connected to the diagram</summary>
			public Node Node1
			{
				get { return (Node)Anc1.Elem; }
				set
				{
					// Allowing this now
					//Debug.Assert(Node0 == null || Node0 != value, "Don't allow connections between anchors on the same element");
					if (Anc1.Elem == value) return;
					if (Node1 != null)
					{
						Node1.SizeChanged -= Relink;
						Node1.PositionChanged -= Invalidate;
						Node1.Connectors.Remove(this);
					}
					Anc1.Elem = value;
					if (Node1 != null)
					{
						Node1.Connectors.Add(this);
						Node1.PositionChanged += Invalidate;
						Node1.SizeChanged += Relink;
					}
				}
			}

			/// <summary>The 'from' then 'to' nodes, as a sequence</summary>
			public IEnumerable<Node> Nodes
			{
				get
				{
					if (Node0 != null) yield return Node0;
					if (Node1 != null) yield return Node1;
				}
			}

			/// <summary>Returns the other connected node</summary>
			public Node OtherNode(Node node)
			{
				if (Node0 == node) return Node1;
				if (Node1 == node) return Node0;
				throw new Exception("{0} is not connected to {1}".Fmt(ToString(), node.ToString()));
			}

			/// <summary>Get/Set the 'to' or 'from' end of the connector</summary>
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

			/// <summary>True if at least one end of the connector is attached to a node</summary>
			public bool Attached { get { return Node0 != null || Node1 !=  null; } }

			/// <summary>Detach from the nodes at each end of the connector</summary>
			public void DetachNodes()
			{
				Node0 = null;
				Node1 = null;
			}

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

			///// <summary>A label graphic for the connector</summary>
			//public virtual Label Label
			//{
			//	get { return m_impl_label; }
			//	set
			//	{
			//		if (m_impl_label == value) return;
			//		m_impl_label = value;
			//		Invalidate();
			//	}
			//}
			//private Label m_impl_label;

			/// <summary>Style attributes for the connector</summary>
			public ConnectorStyle Style
			{
				get { return Diagram != null ? (m_impl_style = Diagram.m_connector_styles[m_impl_style]) : m_impl_style; }
				set
				{
					if (m_impl_style == value) return;
					if (m_impl_style != null)
					{
						m_impl_style.StyleChanged -= Invalidate;
					}
					value = value ?? ConnectorStyle.Default;
					m_impl_style = Diagram != null ? Diagram.m_connector_styles[value] : value;
					if (m_impl_style != null)
					{
						m_impl_style.StyleChanged += Invalidate;
					}
					Invalidate();
				}
			}
			IStyle IHasStyle.Style { get { return Style; } }
			private ConnectorStyle m_impl_style;

			/// <summary>Get/Set the selected state</summary>
			public override bool Selected
			{
				get { return base.Selected; }
				set
				{
					if (Selected == value) return;
					base.Selected = value;
					if (m_gfx_line != null) m_gfx_line.ColourSet(Selected ? Style.Selected : Style.Line);
					if (m_gfx_fwd  != null) m_gfx_fwd .ColourSet(Selected ? Style.Selected : Style.Line);
					if (m_gfx_bak  != null) m_gfx_bak .ColourSet(Selected ? Style.Selected : Style.Line);
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
					m_gfx_bak.O2P = m4x4.Transform(v4.ZAxis, (float)Math.Atan2(dir.y, dir.x), pos + bias) * m4x4.Scale(Style.Width, v4.Origin);
				}

				// If the connector has a forward arrow, add the arrow head graphics
				if (m_gfx_fwd != null && (Type & EType.Forward) != 0)
				{
					var dir = -Anc1.NormalDS;
					if (dir == v4.Zero) dir = Anc0.NormalDS;
					if (dir == v4.Zero) dir = v4.Normalise3(Anc1.LocationDS - Anc0.LocationDS, v4.YAxis);
					var pos = new v4(Anc1.LocationDS.xy, Math.Max(Anc1.LocationDS.z, PositionZ), 1f);
					m_gfx_fwd.O2P = m4x4.Transform(v4.ZAxis, (float)Math.Atan2(dir.y, dir.x), pos + bias) * m4x4.Scale(Style.Width, v4.Origin);
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
			public override HitTestResult.Hit HitTest(v2 point, View3d.Camera cam)
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
				var dist_cs = cam.WSVecToSSVec(closest_pt, point);
				if (dist_cs.Length2Sq > MinCSSelectionDistanceSq) return null;
				return new HitTestResult.Hit(this, closest_pt - Position.pos.xy);
			}

			/// <summary>Handle a click event on this element</summary>
			internal override bool HandleClicked(HitTestResult.Hit hit, View3d.Camera cam)
			{
				// Only respond if selected and editing is allowed
				if (Selected && Diagram.AllowEditing)
				{
					// Hit point in screen space
					var hit_point_ds = Position.pos + new v4(hit.Point,0,0);
					var hit_point_cs = v2.From(cam.WSPointToSSPoint(hit_point_ds));
					var anc0_cs = v2.From(cam.WSPointToSSPoint(Anc0.LocationDS));
					var anc1_cs = v2.From(cam.WSPointToSSPoint(Anc1.LocationDS));

					// If the click was at the ends of the connector and diagram editing
					// is allowed, detach the connector and start a move link mouse op
					if ((hit_point_cs - anc0_cs).Length2Sq < MinCSSelectionDistanceSq)
					{
						Diagram.m_mouse_op.SetPending(1, new MouseOpMoveLink(Diagram, this, false){StartOnMouseDown = false});
						return true;
					}
					if ((hit_point_cs - anc1_cs).Length2Sq < MinCSSelectionDistanceSq)
					{
						Diagram.m_mouse_op.SetPending(1, new MouseOpMoveLink(Diagram, this, true){StartOnMouseDown = false});
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
			protected override void UpdateGfxCore()
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
					m_gfx_bak.ColourSet(col);

				// If the connector has a forward arrow, add the arrow head graphics
				if ((Type & EType.Forward) != 0)
					m_gfx_fwd.ColourSet(col);

				UpdatePositions();
			}

			/// <summary>Add the graphics associated with this element to the window</summary>
			protected override void AddToSceneCore(View3d.Window window)
			{
				// Add the main connector line
				window.AddObject(m_gfx_line);

				// If the connector has a back arrow, add the arrow head graphics
				if ((Type & EType.Back) != 0)
					window.AddObject(m_gfx_bak);

				// If the connector has a forward arrow, add the arrow head graphics
				if ((Type & EType.Forward) != 0)
					window.AddObject(m_gfx_fwd);
			}

			/// <summary>Detach from 'node'</summary>
			public void Remove(Node node)
			{
				if (node == null) throw new ArgumentNullException("node");
				if (Node0 == node) { Node0 = null; Invalidate(); return; }
				if (Node1 == node) { Node1 = null; Invalidate(); return; }
				throw new Exception("Connector '{0}' is not connected to node {0}".Fmt(ToString(), node.ToString()));
			}

			/// <summary>Update the node anchors</summary>
			public void Relink(bool find_previous_anchors)
			{
				if (find_previous_anchors)
				{
					m_anc0.Update(this);
					m_anc1.Update(this);
				}
				else
				{
					// Find the preferred 'anc0' position closest to the centre of element1,
					// then update the position of 'anc1' given 'anc0's position,
					// finally update 'anc0' again since 'anc1' has possibly changed.
					if (m_anc1.Elem != null)
						m_anc0.Update(m_anc1.Elem.Position.pos.xy, false, this);

					m_anc1.Update(m_anc0.LocationDS.xy, false, this);
					m_anc0.Update(m_anc1.LocationDS.xy, false, this);
				}
				Invalidate();
			}
			public void Relink(object sender = null, EventArgs args = null)
			{
				var find_previous_anchors = Diagram == null || !Diagram.Options.Node.AutoRelink;
				Relink(find_previous_anchors);
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
				var pt0 = node1?.Centre ?? node0?.Centre ?? v2.Zero;
				var pt1 = node0?.Centre ?? node1?.Centre ?? v2.Zero;
				anc0 = node0?.NearestAnchor(pt0, false, null) ?? new AnchorPoint(null, new v4(pt0, 0, 1f), v4.Zero);
				anc1 = node1?.NearestAnchor(pt1, false, null) ?? new AnchorPoint(null, new v4(pt1, 0, 1f), v4.Zero);
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

				//v2 intersect;
				//if (Geometry.Intersect(
				//	start, start + Anc0.NormalDS.xy,
				//	end  , end   + Anc1.NormalDS.xy, out intersect))
				//{
				//	if (v2.Dot2(intersect - start, Anc0.NormalDS.xy) > 0 &&
				//		v2.Dot2(intersect - end  , Anc1.NormalDS.xy) > 0)
				//		return new[]{start, intersect, end};
				//}

				var dir0 = Anc0.NormalDS.xy;
				var dir1 = Anc1.NormalDS.xy;
				if (dir0 == v2.Zero) dir0 = -dir1;
				if (dir1 == v2.Zero) dir1 = -dir0;
				if (dir0 == v2.Zero) dir0 = v2.Normalise2(end - start, v2.Zero);
				if (dir1 == v2.Zero) dir1 = v2.Normalise2(start - end, v2.Zero);

				var slen = v2.Dot2(centre - start, dir0);
				var elen = v2.Dot2(centre -   end, dir1);
				slen = Style.Smooth ? MinConnectorLen : Math.Max(slen, MinConnectorLen);
				elen = Style.Smooth ? MinConnectorLen : Math.Max(elen, MinConnectorLen);

				return new[]{start, start + slen*dir0, end + elen*dir1, end};
			}

			/// <summary>Check the self consistency of this element</summary>
			public override bool CheckConsistency()
			{
				// Connections to the same node are allowed
				// Null nodes are allowed.

				// The connected nodes should contain a reference to this connector
				if (Node0 != null && Node0.Connectors.FirstOrDefault(x => x.Id == Id) == null)
					throw new Exception("Connector {0} is connected to node {0}, but that node does not contain a reference to this connector".Fmt(ToString(), Node0.ToString()));
				if (Node1 != null && Node1.Connectors.FirstOrDefault(x => x.Id == Id) == null)
					throw new Exception("Connector {0} is connected to node {0}, but that node does not contain a reference to this connector".Fmt(ToString(), Node1.ToString()));

				// The style should be known to the diagram
				if (Diagram != null)
				{
					if (!Diagram.m_connector_styles.ContainsKey(Style.Id))
						throw new Exception("Connector {0} style is not in the diagram's style cache".Fmt(ToString()));
				}

				return base.CheckConsistency();
			}

			// ToString
			public override string ToString() { return "Connector["+Anc0.ToString()+" -> "+Anc1.ToString()+"]"; }
		}

		#endregion

		#region Shapes

		/// <summary>Interface for shape creation types</summary>
		public interface IShape
		{
			/// <summary>Generate the ldr string for the shape</summary>
			string Make(v2 sz);
		}

		/// <summary>A quad shape</summary>
		public class QuadShape :IShape
		{
			public QuadShape(float corner_radius) { CornerRadius = corner_radius; }
			public QuadShape(XElement node)       { CornerRadius = node.Element(XmlTag.CornerRadius).As<float>(); }
			public XElement ToXml(XElement node)  { node.Add2(XmlTag.CornerRadius, CornerRadius, false); return node; }

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
			public EllipseShape() {}
			public EllipseShape(XElement node) {}
			public XElement ToXml(XElement node) { return node; }

			/// <summary>Generate the ldr string for the shape</summary>
			public string Make(v2 sz)
			{
				var ldr = new LdrBuilder();
				ldr.Append("*Circle {3 ",sz.x/2f," ",sz.y/2f," ",Ldr.Solid(),"}\n");
				return ldr.ToString();
			}
		}

		/// <summary>A 'mixin' class for a textured shape</summary>
		public class TexturedShape<TShape> :View3d.Object where TShape :IShape
		{
			public TexturedShape(TShape shape, v2 sz, uint texture_scale = 4)
			{
				Shape = shape;
				m_surf = new Surface((uint)(sz.x + 0.5f), (uint)(sz.y + 0.5f), texture_scale);
				UpdateModel();
			}
			public TexturedShape(XElement node)
			{
				Shape = node.Element(XmlTag.Shape).As<TShape>();
				m_surf = node.Element(XmlTag.Surface).As<Surface>();
				UpdateModel();
			}
			public override void Dispose()
			{
 				m_surf.Dispose();
				base.Dispose();
			}

			/// <summary>Export to XML</summary>
			public XElement ToXml(XElement node)
			{
				node.Add2(XmlTag.Shape, Shape, false);
				node.Add2(XmlTag.Surface, Surface, false);
				return node;
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
			public View3d.Texture.Lock LockSurface(bool discard)
			{
				return Surface.LockSurface(discard);
			}

			/// <summary>Get/Set the size of the quad and texture</summary>
			public v2 Size
			{
				get { return m_surf.Size; }
				set
				{
					if (Size == value) return;
					m_surf.Size = value;
					UpdateModel();
				}
			}

			/// <summary>Update the model shape and set the texture</summary>
			private void UpdateModel()
			{
				var ldr = Shape.Make(Size);
				UpdateModel(ldr, View3d.EUpdateObject.All ^ View3d.EUpdateObject.Transform);
				SetTexture(Surface.Surf);
			}
		}

		#endregion

		#region Styles

		/// <summary>Style attributes for nodes</summary>
		[TypeConverter(typeof(NodeStyle))]
		public class NodeStyle :GenericTypeConverter<NodeStyle> ,IHasId ,IStyle
		{
			public static readonly NodeStyle Default = new NodeStyle(Guid.Empty);

			public NodeStyle() :this(Guid.NewGuid())
			{}
			public NodeStyle(NodeStyle rhs) :this(Guid.NewGuid(), rhs)
			{}
			public NodeStyle(Guid id)
			{
				Id           = id;
				AutoSize     = true;
				Border       = Color.Black;
				Fill         = Color.WhiteSmoke;
				Selected     = Color.LightBlue;
				Disabled     = Color.LightGray;
				Text         = Color.Black;
				TextDisabled = Color.DarkGray;
				TextAlign    = ContentAlignment.MiddleCenter;
				Font         = new Font(FontFamily.GenericSansSerif, 12f, GraphicsUnit.Point);
				Padding      = new Padding(10,10,10,10);
			}
			public NodeStyle(XElement node) :this()
			{
				Id           = node.Element(XmlTag.Id          ).As<Guid>            (Id          );
				AutoSize     = node.Element(XmlTag.AutoSize    ).As<bool>            (AutoSize    );
				Border       = node.Element(XmlTag.Border      ).As<Color>           (Border      );
				Fill         = node.Element(XmlTag.Fill        ).As<Color>           (Fill        );
				Selected     = node.Element(XmlTag.Selected    ).As<Color>           (Selected    );
				Disabled     = node.Element(XmlTag.Disabled    ).As<Color>           (Disabled    );
				Text         = node.Element(XmlTag.Text        ).As<Color>           (Text        );
				TextDisabled = node.Element(XmlTag.TextDisabled).As<Color>           (TextDisabled);
				TextAlign    = node.Element(XmlTag.Align       ).As<ContentAlignment>(TextAlign   );
				Font         = node.Element(XmlTag.Font        ).As<Font>            (Font        );
				Padding      = node.Element(XmlTag.Padding     ).As<Padding>         (Padding     );
			}
			public NodeStyle(Guid id, NodeStyle rhs)
			{
				Id                   = id;
				AutoSize             = rhs.AutoSize;
				Border               = rhs.Border;
				Fill                 = rhs.Fill;
				Selected             = rhs.Selected;
				Disabled             = rhs.Disabled;
				Text                 = rhs.Text;
				TextDisabled         = rhs.TextDisabled;
				TextAlign            = rhs.TextAlign;
				Font                 = (Font)rhs.Font.Clone();
				Padding              = rhs.Padding;
				StyleChangedInternal = rhs.StyleChangedInternal;
			}

			/// <summary>Export to XML</summary>
			public XElement ToXml(XElement node)
			{
				node.Add2(XmlTag.Id           ,Id           ,false);
				node.Add2(XmlTag.AutoSize     ,AutoSize     ,false);
				node.Add2(XmlTag.Border       ,Border       ,false);
				node.Add2(XmlTag.Fill         ,Fill         ,false);
				node.Add2(XmlTag.Selected     ,Selected     ,false);
				node.Add2(XmlTag.Disabled     ,Disabled     ,false);
				node.Add2(XmlTag.Text         ,Text         ,false);
				node.Add2(XmlTag.TextDisabled ,TextDisabled ,false);
				node.Add2(XmlTag.Font         ,Font         ,false);
				node.Add2(XmlTag.Align        ,TextAlign    ,false);
				node.Add2(XmlTag.Padding      ,Padding      ,false);
				return node;
			}

			/// <summary>Unique id for the style</summary>
			[Browsable(false)]
			public Guid Id { get; private set; }

			/// <summary>True if the node's size is determined automatically from its content</summary>
			public bool AutoSize
			{
				get { return m_impl_autosize; }
				set { Set(ref m_impl_autosize, value); }
			}
			private bool m_impl_autosize;

			/// <summary>The colour of the node border</summary>
			public Color Border
			{
				get { return m_impl_border; }
				set { Set(ref m_impl_border, value); }
			}
			private Color m_impl_border;

			/// <summary>The node background colour</summary>
			public Color Fill
			{
				get { return m_impl_fill; }
				set { Set(ref m_impl_fill, value); }
			}
			private Color m_impl_fill;

			/// <summary>The colour of the node when selected</summary>
			public Color Selected
			{
				get { return m_impl_selected; }
				set { Set(ref m_impl_selected, value); }
			}
			private Color m_impl_selected;

			/// <summary>The colour of the node when disabled</summary>
			public Color Disabled
			{
				get { return m_impl_disabled; }
				set { Set(ref m_impl_disabled, value); }
			}
			private Color m_impl_disabled;

			/// <summary>The node text colour</summary>
			public Color Text
			{
				get { return m_impl_text; }
				set { Set(ref m_impl_text, value); }
			}
			private Color m_impl_text;

			/// <summary>The node text colour when disabled</summary>
			public Color TextDisabled
			{
				get { return m_impl_text_disabled; }
				set { Set(ref m_impl_text_disabled, value); }
			}
			private Color m_impl_text_disabled;

			/// <summary>The alignment of the text within the node</summary>
			public ContentAlignment TextAlign
			{
				get { return m_impl_align; }
				set { Set(ref m_impl_align, value); }
			}
			private ContentAlignment m_impl_align;

			/// <summary>The font to use for the node text</summary>
			public Font Font
			{
				get { return m_impl_font; }
				set { Set(ref m_impl_font, value); }
			}
			private Font m_impl_font;

			/// <summary>The padding surrounding the text in the node</summary>
			public Padding Padding
			{
				get { return m_impl_padding; }
				set { Set(ref m_impl_padding, value); }
			}
			private Padding m_impl_padding;

			/// <summary>Raised whenever a style property is changed. Note: Weak event</summary>
			public event EventHandler StyleChanged
			{
				add { StyleChangedInternal += value.MakeWeak(eh => StyleChangedInternal -= eh); }
				remove {}
			}
			private event EventHandler StyleChangedInternal;

			/// <summary>Helper for setting properties and raising events</summary>
			private void Set<T>(ref T existing, T value)
			{
				if (Equals(existing, value)) return;
				existing = value;
				StyleChangedInternal.Raise(this, EventArgs.Empty);
			}
		
			public override string ToString() { return "NS:"+Id; }
		}

		/// <summary>Style properties for connectors</summary>
		[TypeConverter(typeof(ConnectorStyle))]
		public class ConnectorStyle :GenericTypeConverter<ConnectorStyle> ,IHasId ,IStyle
		{
			public static readonly ConnectorStyle Default = new ConnectorStyle(Guid.Empty);

			public ConnectorStyle() :this(Guid.NewGuid())
			{}
			public ConnectorStyle(ConnectorStyle rhs) :this(Guid.NewGuid(), rhs)
			{}
			public ConnectorStyle(Guid id)
			{
				Id       = id;
				Line     = Color.Black;
				Selected = Color.Blue;
				Dangling = Color.DarkRed;
				Width    = 5f;
				Smooth   = false;
			}
			public ConnectorStyle(XElement node) :this()
			{
				Id       = node.Element(XmlTag.Id      ).As<Guid> (Id      );
				Line     = node.Element(XmlTag.Line    ).As<Color>(Line    );
				Selected = node.Element(XmlTag.Selected).As<Color>(Selected);
				Dangling = node.Element(XmlTag.Dangling).As<Color>(Dangling);
				Width    = node.Element(XmlTag.Width   ).As<float>(Width   );
				Smooth   = node.Element(XmlTag.Smooth  ).As<bool> (Smooth  );
			}
			public ConnectorStyle(Guid id, ConnectorStyle rhs)
			{
				Id                   = id;
				Line                 = rhs.Line;
				Selected             = rhs.Selected;
				Dangling             = rhs.Dangling;
				Width                = rhs.Width;
				Smooth               = rhs.Smooth;
				StyleChangedInternal = rhs.StyleChangedInternal;
			}

			/// <summary>Export to XML</summary>
			public XElement ToXml(XElement node)
			{
				node.Add2(XmlTag.Id       ,Id       ,false);
				node.Add2(XmlTag.Line     ,Line     ,false);
				node.Add2(XmlTag.Selected ,Selected ,false);
				node.Add2(XmlTag.Width    ,Width    ,false);
				node.Add2(XmlTag.Smooth   ,Smooth   ,false);
				return node;
			}

			/// <summary>Unique id for the style</summary>
			[Browsable(false)]
			public Guid Id { get; private set; }

			/// <summary>The colour of the line portion of the connector</summary>
			public Color Line
			{
				get { return m_impl_line; }
				set { Set(ref m_impl_line, value); }
			}
			private Color m_impl_line;

			/// <summary>The colour of the line when selected</summary>
			public Color Selected
			{
				get { return m_impl_selected; }
				set { Set(ref m_impl_selected, value); }
			}
			private Color m_impl_selected;

			/// <summary>The colour of dangling connectors</summary>
			public Color Dangling
			{
				get { return m_impl_dangling; }
				set { Set(ref m_impl_dangling, value); }
			}
			private Color m_impl_dangling;

			/// <summary>The width of the connector line</summary>
			public float Width
			{
				get { return m_impl_width; }
				set { Set(ref m_impl_width, value); }
			}
			private float m_impl_width;

			/// <summary>True for a smooth connector, false for a straight edged connector</summary>
			public bool Smooth
			{
				get { return m_impl_smooth; }
				set { Set(ref m_impl_smooth, value); }
			}
			private bool m_impl_smooth;

			/// <summary>Raised whenever a style property is changed. Note: Weak event</summary>
			public event EventHandler StyleChanged
			{
				add { StyleChangedInternal += value.MakeWeak(eh => StyleChangedInternal -= eh); }
				remove {}
			}
			private event EventHandler StyleChangedInternal;

			/// <summary>Helper for setting properties and raising events</summary>
			private void Set<T>(ref T existing, T value)
			{
				if (Equals(existing, value)) return;
				existing = value;
				StyleChangedInternal.Raise(this, EventArgs.Empty);
			}
		
			public override string ToString() { return "CS:"+Id; }
		}

		/// <summary>A cache of node or connector style objects</summary>
		private class StyleCache<T> where T:IHasId
		{
			private readonly Dictionary<Guid, T> m_map;

			public StyleCache()
			{
				m_map = new Dictionary<Guid,T>();
				m_map.Add(Guid.Empty, (T)Activator.CreateInstance(typeof(T), Guid.Empty));
			}
			public StyleCache(XElement node) :this()
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

			/// <summary>True if 'id' is in the cache</summary>
			public bool ContainsKey(Guid id)
			{
				return m_map.ContainsKey(id);
			}

			/// <summary>Add the styles in 'rhs' to this cache</summary>
			public void Merge(StyleCache<T> rhs)
			{
				foreach (var s in rhs.m_map)
					m_map[s.Key] = s.Value;
			}

			/// <summary>Get/Cache 'style'</summary>
			public T this[T style]
			{
				get
				{
					if (style == null)
						return m_map[Guid.Empty];

					T out_style;
					if (m_map.TryGetValue(style.Id, out out_style))
						return out_style;
					
					m_map.Add(style.Id, style);
					return style;
				}
			}
			public T this[Guid id]
			{
				get { return m_map[id]; }
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
				unused.Remove(Guid.Empty);

				// Remove any remaining styles
				foreach (var id in unused)
					m_map.Remove(id);
			}
		
			/// <summary>Access to the styles</summary>
			public IEnumerable<T> Styles
			{
				get { return m_map.Values; }
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
			public AnchorPoint(AnchorPoint rhs)
				:this(rhs.m_impl_elem, rhs.m_impl_location, rhs.m_impl_normal)
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
				var id = node.Element(XmlTag.ElementId).As<Guid>();
				m_impl_elem = elements.TryGetValue(id, out m_impl_elem) ? m_impl_elem : null;
				m_impl_location = node.Element(XmlTag.Location).As<v4>(v4.Origin);
				Update(null);
			}

			/// <summary>Export to XML</summary>
			public XElement ToXml(XElement node)
			{
				node.Add2(XmlTag.ElementId ,Elem != null ? Elem.Id : Guid.Empty ,false);
				node.Add2(XmlTag.Location ,Location ,false);
				return node;
			}

			/// <summary>The element this anchor point is attached to</summary>
			public Element Elem
			{
				get { return m_impl_elem; }
				set
				{
					if (m_impl_elem == value) return;
					
					// When set to null, record the last diagram space position so that
					// anything connected to this anchor point doesn't snap to the origin.
					if (value == null)
					{
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

			/// <summary>Get/Set the node-relative anchor normal (can be zero)</summary>
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

			/// <summary>Get the diagram space anchor normal (can be zero)</summary>
			public v4 NormalDS   { get { return Elem != null ? Elem.Position * Normal   : Normal  ; } }

			/// <summary>Update this anchor point location after it has moved/resized</summary>
			public void Update(v2 pt, bool pt_in_element_space, Element existing)
			{
				if (Elem == null) return;
				var anc = Elem.NearestAnchor(pt, pt_in_element_space, existing);
				Location = anc.Location;
				Normal   = anc.Normal;
			}

			/// <summary>Update this anchor point location after it has moved/resized</summary>
			public void Update(Element existing)
			{
				if (Elem == null) return;
				var anc = Elem.NearestAnchor(Location.xy, true, existing);
				Location = anc.Location;
				Normal   = anc.Normal;
			}

			// ToString
			public override string ToString() { return "Anc["+(Elem!=null?Elem.ToString():"dangling")+"]"; }
		}

		#endregion

		#region Mouse Op

		/// <summary>Manages per-button mouse operations</summary>
		private class MouseOps
		{
			private readonly Dictionary<int, MouseOp> m_ops;
			private readonly Dictionary<int, MouseOp> m_pending;

			public MouseOps()
			{
				m_ops     = int_.Range(Util.MouseButtonCount).ToDictionary(k => k, k => (MouseOp)null);
				m_pending = int_.Range(Util.MouseButtonCount).ToDictionary(k => k, k => (MouseOp)null);
			}

			/// <summary>The currently active mouse op</summary>
			public MouseOp Active { get; private set; }

			/// <summary>Return the op pending for button 'idx'</summary>
			public MouseOp Pending(int idx)
			{
				return m_pending[idx];
			}

			/// <summary>Add a mouse op to be started on the next mouse down event for button 'idx'</summary>
			public void SetPending(int idx, MouseOp op)
			{
				if (m_pending[idx] != null) m_pending[idx].Dispose();
				m_pending[idx] = op;
			}

			/// <summary>Start/End the next mouse op for button 'idx'</summary>
			public void BeginOp(int idx)
			{
				Active = m_pending[idx];
				m_pending[idx] = null;

				// If the op starts immediately without a mouse down, fake
				// a mouse down event as soon as it becomes active.
				if (Active != null)
				{
					if (!Active.StartOnMouseDown) Active.MouseDown(null);
				}
			}
			public void EndOp(int idx)
			{
				if (Active != null)
				{
					if (Active.Cancelled) Active.NotifyCancelled();
					Active.Dispose();
				}
				Active = null;

				// If the next op starts immediately, begin it now
				if (m_pending[idx] != null && !m_pending[idx].StartOnMouseDown)
					BeginOp(idx);
			}
		}

		/// <summary>Base class for a mouse operation performed with the mouse down->drag->up sequence</summary>
		private abstract class MouseOp :IDisposable
		{
			// The general process goes:
			//  A mouse op is created and set as the pending operation in 'MouseOps'.
			//  MouseDown on the diagram calls 'BeginOp' which moves the pending op to 'Active'.
			//  Mouse events on the diagram are forwarded to the active op
			//  MouseUp ends the current Active op, if the pending op should start immediately
			//  then mouse up causes the next op to start (with a faked MouseDown event).
			//  If at any point a mouse op is cancelled, no further mouse events are forwarded
			//  to the op. When EndOp is called, a notification can be sent by the op to indicate cancelled.

			/// <summary>The owning diagram</summary>
			protected DiagramControl m_diag;
			protected Scope m_suspend_scope;

			// Selection data for a mouse button
			public bool  m_btn_down;   // True while the corresponding mouse button is down
			public Point m_grab_cs;    // The client space location of where the diagram was "grabbed"
			public v2    m_grab_ds;    // The diagram space location of where the diagram was "grabbed"
			public HitTestResult m_hit_result; // The hit test result on mouse down

			public MouseOp(DiagramControl diag)
			{
				m_diag = diag;
				StartOnMouseDown = true;
				Cancelled = false;
			}
			public virtual void Dispose()
			{
				Util.Dispose(ref m_suspend_scope);
			}

			/// <summary>True if mouse down starts the op, false if the op should start as soon as possible</summary>
			public bool StartOnMouseDown { get; set; }

			/// <summary>True if the op was aborted</summary>
			public bool Cancelled { get; protected set; }

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

			/// <summary>Called on key down</summary>
			public virtual void OnKeyDown(KeyEventArgs e) { }

			/// <summary>Called on key up</summary>
			public virtual void OnKeyUp(KeyEventArgs e) { }

			/// <summary>Called when the mouse operation is cancelled</summary>
			public virtual void NotifyCancelled() {}
		}

		/// <summary>A mouse operation for dragging selected elements around</summary>
		private class MouseOpDragOrClickElements :MouseOp
		{
			private HitTestResult.Hit m_hit_selected;
			private bool m_selection_graphic_added;

			public MouseOpDragOrClickElements(DiagramControl diag) :base(diag)
			{
				m_selection_graphic_added = false;
			}
			public override void MouseDown(MouseEventArgs e)
			{
				// Look for a selected object that the mouse operation starts on
				m_hit_selected = m_hit_result.Hits.FirstOrDefault(x => x.Element.Selected);

				// Record the drag start positions for selected objects
				foreach (var elem in m_diag.Selected)
					elem.DragStartPosition = elem.Position;

				// Prevent events while dragging the elements around
				m_suspend_scope = m_diag.SuspendDiagramChanged(raise_on_resume:true);
			}
			public override void MouseMove(MouseEventArgs e)
			{
				// If we haven't dragged, treat it as a click instead (i.e. ignore till it's a drag operation)
				if (IsClick(e.Location))
					return;

				var drag_selected = m_diag.AllowEditing && m_hit_selected != null;
				if (drag_selected)
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
						m_diag.m_window.AddObject(m_diag.m_tools.AreaSelect);
						m_selection_graphic_added = true;
					}

					// Position the selection graphic
					var selection_area = BRect.FromBounds(m_grab_ds, m_diag.ClientToDiagram(e.Location));
					m_diag.m_tools.AreaSelect.O2P = m4x4.Scale(
						Math.Max(1f, selection_area.SizeX),
						Math.Max(1f, selection_area.SizeY),
						1.0f, new v4(selection_area.Centre, m_diag.HighestZ + 1f,1));
				}
				m_diag.Invalidate();
			}
			public override void MouseUp(MouseEventArgs e)
			{
				Util.Dispose(ref m_suspend_scope);

				// If this is a single click...
				if (IsClick(e.Location))
				{
					// Check to see if the click is on an existing selected object
					// If so, allow that object to handle the click, otherwise select elements.
					if (m_hit_selected == null || !m_hit_selected.Element.HandleClicked(m_hit_selected, m_hit_result.Camera))
					{
						var selection_area = new BRect(m_grab_ds, v2.Zero);
						m_diag.SelectElements(selection_area, ModifierKeys);
					}
				}
				else if (m_hit_selected != null && m_diag.AllowEditing)
				{
					m_diag.DragSelected(m_diag.ClientToDiagram(e.Location) - m_grab_ds, true);
				}
				else
				{
					var selection_area = BRect.FromBounds(m_grab_ds, m_diag.ClientToDiagram(e.Location));
					m_diag.SelectElements(selection_area, ModifierKeys);
				}

				// Remove the area selection graphic
				if (m_selection_graphic_added)
					m_diag.m_window.RemoveObject(m_diag.m_tools.AreaSelect);

				m_diag.Cursor = Cursors.Default;
				m_diag.Invalidate();
			}
		}

		/// <summary>A mouse operation for dragging the diagram around</summary>
		private class MouseOpDragOrClickDiagram :MouseOp
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
			}
			public override void MouseUp(MouseEventArgs e)
			{
				m_diag.Cursor = Cursors.Default;

				// If we haven't dragged, treat it as a click instead
				if (IsClick(e.Location))
					m_diag.OnDiagramClicked(e);
				else
					m_diag.PositionDiagram(e.Location, m_grab_ds);

				m_diag.Invalidate();
			}
		}

		/// <summary>
		/// A mouse operation for creating a new node.</summary>
		private class MouseOpCreateNode :MouseOp
		{
			private Node m_node;
			public MouseOpCreateNode(DiagramControl diag) :base(diag)
			{}
			public override void Dispose()
			{
				if (m_node != null) m_node.Dispose();
				base.Dispose();
			}
			public override void MouseDown(MouseEventArgs e)
			{
				// Allow the caller to cancel the AddNode before we start.
				Cancelled = m_diag.RaiseDiagramChanged(new DiagramChangedNodeEventArgs(EDiagramChangeType.AddNodeBegin, null)).Cancel;
				if (Cancelled)
					return;

				m_node = new BoxNode{PositionXY = m_grab_ds};
				m_node.Diagram = m_diag;
				m_node.PositionXY = m_grab_ds;
			}
			public override void MouseMove(MouseEventArgs e)
			{}
			public override void MouseUp(MouseEventArgs e)
			{
				// Allow the client to cancel the AddNode.
				Cancelled = m_diag.RaiseDiagramChanged(new DiagramChangedNodeEventArgs(EDiagramChangeType.AddNodeEnd, m_node)).Cancel;
				if (Cancelled)
					return;

				m_node = null; // Prevent the node being disposed
			}
			public override void NotifyCancelled()
			{
				m_diag.RaiseDiagramChanged(new DiagramChangedNodeEventArgs(EDiagramChangeType.AddNodeCancelled, null));
			}
		}

		/// <summary>Base class for mouse operations that involve moving a link</summary>
		private abstract class MouseOpMoveLinkBase :MouseOp
		{
			protected readonly NodeProxy m_proxy; // Proxy node that one end of the link will be attached to
			protected readonly bool m_forward;    // Indicates whether the start or the end of the connector is floating
			protected Connector m_conn;           // The connector being moved
			protected Node m_fixed;               // The unmoving node
			private bool m_is_add;
			
			public MouseOpMoveLinkBase(DiagramControl diag, bool forward, bool is_add) :base(diag)
			{
				m_proxy = new NodeProxy{Diagram = diag}; // Create a proxy for the floating end to attach to
				m_forward = forward;
				m_is_add = is_add;
			}
			public override void Dispose()
			{
				if (m_proxy != null) m_proxy.Dispose();
				base.Dispose();
			}
			protected virtual IEnumerable<HitTestResult.Hit> HitTestNode(Point location_cs)
			{
				// This returns all nodes under 'location_cs' including !Enabled ones
				Func<Element,bool> pred = x => x.Entity == Entity.Node && x != m_proxy;// Allow linking to self && x != m_fixed;
				return m_diag.HitTestCS(location_cs, pred).Hits;
			}
			public override void MouseDown(MouseEventArgs e)
			{
				var ty = m_is_add ? EDiagramChangeType.AddLinkBegin : EDiagramChangeType.MoveLinkBegin;
				Cancelled = m_diag.RaiseDiagramChanged(new DiagramChangedLinkEventArgs(ty, m_conn, m_fixed, null)).Cancel;
			}
			public override void MouseMove(MouseEventArgs e)
			{
				// Find the node we're hovering over
				var hits = HitTestNode(e.Location);
				
				// Look for the first hit enabled node, if none, first any-state node
				var hit = hits.FirstOrDefault(x => x.Element.Enabled) ?? hits.FirstOrDefault();
				var target = hit != null ? (Node)hit.Element : null;
				
				if (target != null)
					Cancelled = m_diag.RaiseDiagramChanged(new DiagramChangedLinkEventArgs(EDiagramChangeType.LinkMoving, m_conn, m_fixed, target)).Cancel;

				// If we're hovering over a node, find the nearest anchor on that node and snap to it
				if (target != null && target.Enabled)
				{
					var anchor = target.NearestAnchor(hit.Point, true, m_conn);
					m_proxy.PositionXY = anchor.LocationDS.xy;
					m_proxy.AnchorNormal = anchor.Normal;
				}
				else
				{
					m_proxy.PositionXY = m_diag.ClientToDiagram(e.Location);
					m_proxy.AnchorNormal = v4.Zero;
				}
				m_conn.Invalidate();
				m_diag.Invalidate();
			}
			public override void MouseUp(MouseEventArgs e)
			{
				// Find the node to make the link with
				var hit = HitTestNode(e.Location).FirstOrDefault(x => x.Element.Enabled);
				var target = hit != null ? (Node)hit.Element : null;

				// No node to link to? Just revert the link
				if (target == null)
				{
					Cancelled = true;
					Revert();
				}
				else
				{
					var anchor = target.NearestAnchor(hit.Point, true, m_conn);

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
					var ty = m_is_add ? EDiagramChangeType.AddLinkEnd : EDiagramChangeType.MoveLinkEnd;
					Cancelled = m_diag.RaiseDiagramChanged(new DiagramChangedLinkEventArgs(ty, m_conn, m_fixed, target)).Cancel;
					if (Cancelled)
					{
						Revert();
					}
					else
					{
						Commit();
						m_diag.RaiseDiagramChanged(new DiagramChangedEventArgs(EDiagramChangeType.Edited));
					}
				}
				m_diag.Invalidate();
			}
			public override void NotifyCancelled()
			{
				var ty = m_is_add ? EDiagramChangeType.AddLinkAbort : EDiagramChangeType.MoveLinkAbort;
				m_diag.RaiseDiagramChanged(new DiagramChangedLinkEventArgs(ty, m_conn, m_fixed, null));
			}
			protected abstract void Commit();
			protected abstract void Revert();
		}

		/// <summary>
		/// A mouse operation for creating a new link between nodes.
		/// If a link gets made between nodes where there is already an existing
		/// link, the new link just disappears</summary>
		private class MouseOpCreateLink :MouseOpMoveLinkBase
		{
			private readonly Connector.EType m_type;

			public MouseOpCreateLink(DiagramControl diag, Connector.EType type) :base(diag, true, true)
			{
				m_type = type;
			}
			public override void Dispose()
			{
				if (m_conn != null) m_conn.Dispose();
				base.Dispose();
			}
			public override void MouseDown(MouseEventArgs e)
			{
				// Find the node to start the link from. If the mouse
				// down did not start on a node cancel the create link op.
				var hit = m_hit_result.Hits.FirstOrDefault(x => x.Entity == Entity.Node);
				m_fixed = hit != null ? (Node)hit.Element : null;
				if (m_fixed == null)
				{
					Revert();
					Cancelled = true;
					return;
				}

				// Create a link from the hit node to the proxy node
				m_conn = new Connector(m_fixed, m_proxy){Type = m_type, Diagram = m_diag};
				base.MouseDown(e);
			}
			public override void MouseMove(MouseEventArgs e)
			{
				base.MouseMove(e);
				m_conn.Anc(m_fixed).Update(m_proxy.PositionXY, false, m_conn);
			}
			protected override void Commit()
			{
				m_conn = null;
			}
			protected override void Revert()
			{
				Util.Dispose(ref m_conn);
			}
		}

		/// <summary>
		/// A mouse operation for relinking an existing link to another node.
		/// If a link already existings for the new fixed,target pair, revert
		/// the link back to it's original position</summary>
		private class MouseOpMoveLink :MouseOpMoveLinkBase
		{
			private readonly AnchorPoint m_prev; // The anchor point that the moving end was originally attached to

			public MouseOpMoveLink(DiagramControl diag, Connector conn, bool forward) :base(diag, forward, false)
			{
				m_conn = conn;
				m_prev = new AnchorPoint(forward ? m_conn.Anc1 : m_conn.Anc0);
				m_fixed = forward ? conn.Node0 : conn.Node1;

				// Replace the moving end with the proxy
				if (m_forward) m_conn.Anc1 = m_proxy.Anchor;
				else           m_conn.Anc0 = m_proxy.Anchor;
			}
			protected override void Commit()
			{
			}
			protected override void Revert()
			{
				if (m_forward) m_conn.Anc1 = m_prev;
				else           m_conn.Anc0 = m_prev;
				m_conn.Relink(true);
			}
		}

		/// <summary>
		/// A mouse operation for resizing selected elements</summary>
		private class MouseOpResize :MouseOp
		{
			private class Resizee
			{
				public ResizeableElement Elem;
				public v2 InitialPosition;
				public v2 InitialSize;
			}

			private readonly Tools.ResizeGrabber m_grabber; // The grabber to use for resizing
			private readonly Resizee[] m_resizees;

			public MouseOpResize(DiagramControl diag, Tools.ResizeGrabber grabber) :base(diag)
			{
				m_grabber = grabber;
				
				// Make a collection of the resizeable selected elements and their initial size
				m_resizees = m_diag.Selected
					.Where(x => x is ResizeableElement && x.Resizeable)
					.Cast<ResizeableElement>()
					.Select(x => new Resizee{Elem = x, InitialPosition = x.PositionXY, InitialSize = x.Size})
					.ToArray();
			}
			public override void MouseDown(MouseEventArgs e)
			{
				m_diag.Cursor = m_grabber.Cursor;
				m_resizees.ForEach(x => x.Elem.Resizing = true);
			}
			public override void MouseMove(MouseEventArgs e)
			{
				// The resize delta in diagram space
				var vec_ds = m_diag.m_camera.SSVecToWSVec(m_grab_cs, e.Location);
				var delta = v2.Dot2(vec_ds.xy, m_grabber.Direction);

				// Scale all of the resizeable selected elements
				foreach (var elem in m_resizees)
				{
					elem.Elem.PositionXY = elem.InitialPosition + (delta/2)*m_grabber.Direction;
					elem.Elem.Size = new v2(
						Maths.Max(10, elem.InitialSize.x + delta * Math.Abs(m_grabber.Direction.x)),
						Maths.Max(10, elem.InitialSize.y + delta * Math.Abs(m_grabber.Direction.y)));
				}

				m_diag.Invalidate();
			}
			public override void MouseUp(MouseEventArgs e)
			{
				m_diag.Cursor = Cursors.Default;
				m_resizees.ForEach(x => x.Elem.Resizing = false);
				m_diag.Invalidate();
			}
		}
		#endregion

		#region Event Args

		public enum EDiagramChangeType
		{
			/// <summary>
			/// Raised after elements in the diagram have been moved, resized, or had their content changed.
			/// This event will be raised in addition to the more detailed modification events below</summary>
			Edited,

			/// <summary>
			/// A new node is being created.
			/// Setting 'Cancel' for this event will about the add.</summary>
			AddNodeBegin,
			AddNodeEnd,
			AddNodeCancelled,

			/// <summary>
			/// A new link is being created.
			/// Setting 'Cancel' for this event will abort the add.</summary>
			AddLinkBegin,
			AddLinkEnd,
			AddLinkAbort,

			/// <summary>
			/// An end of a connector is being moved to another node.
			/// Setting 'Cancel' for this event will abort the move.</summary>
			MoveLinkBegin,
			MoveLinkEnd,
			MoveLinkAbort,

			/// <summary>
			/// An end of a connector is being moved around on the diagram.
			/// Called whenever the end is above a node (even if !Enabled).
			/// Setting 'Cancel' for this event will abort the move/add</summary>
			LinkMoving,

			/// <summary>
			/// Elements are about to be deleted from the diagram by the user.
			/// Setting 'Cancel' for this event will abort the deletion.</summary>
			RemovingElements,
		}

		/// <summary>Event args for the diagram changed event</summary>
		public class DiagramChangedEventArgs :EventArgs
		{
			// Note: many events are available by attaching to the Elements binding list

			/// <summary>The type of change that occurred</summary>
			public EDiagramChangeType ChgType { get; private set; }

			/// <summary>A cancel property for "about to change" events</summary>
			public bool Cancel { get; set; }

			public DiagramChangedEventArgs(EDiagramChangeType ty)
			{
				ChgType  = ty;
				Cancel   = false;
			}
		}
		public class DiagramChangedNodeEventArgs :DiagramChangedEventArgs
		{
			/// <summary>The created node</summary>
			public Node NewNode { get; private set; }

			public DiagramChangedNodeEventArgs(EDiagramChangeType type, Node added)
				:base(type)
			{
				NewNode = added;
			}
		}
		public class DiagramChangedLinkEventArgs :DiagramChangedEventArgs
		{
			/// <summary>The link being changed or added</summary>
			public Connector Link { get; private set; }

			/// <summary>The node at the fixed end of the link</summary>
			public Node Fixed { get; private set; }

			/// <summary>The node that the dangling end of the link was dropped on to. (Null for AddLinkBegin)</summary>
			public Node Target { get; private set; }

			public DiagramChangedLinkEventArgs(EDiagramChangeType type, Connector link, Node fixed_end, Node target)
				:base(type)
			{
				Link = link;
				Fixed = fixed_end;
				Target = target;
			}
		}
		public class DiagramChangedRemoveElementsEventArgs :DiagramChangedEventArgs
		{
			/// <summary>
			/// The elements flagged for removing.
			/// This collection can be replaced with a different set of elements to remove</summary>
			public Element[] Elements { get; set; }

			public DiagramChangedRemoveElementsEventArgs(params Element[] elements)
				:base(EDiagramChangeType.RemovingElements)
			{
				Elements = elements;
			}
		}

		/// <summary>Event args for user context menu options</summary>
		public class AddUserMenuOptionsEventArgs :EventArgs
		{
			public ContextMenuStrip Menu { get; private set; }
			public AddUserMenuOptionsEventArgs(ContextMenuStrip menu) { Menu = menu; }
		}

		#endregion

		#region Editing

		/// <summary>Wraps a hosted control used to edit an element value</summary>
		public class EditingControl
		{
			/// <summary>The control to be display when editing the element</summary>
			public Control Ctrl;

			/// <summary>Initialise the control with the existing element</summary>
			public Action<Element,Form> Init;

			/// <summary>Called if the editing is accepted</summary>
			public Action<Element> Commit;

			public EditingControl(Control ctrl, Action<Element,Form> init, Action<Element> commit)
			{
				Ctrl       = ctrl;
				Init       = init;
				Commit     = commit;
			}
		}

		/// <summary>Helper form for editing the value of an element using a hosted control</summary>
		private class EditField :Form
		{
			private class SizeBar :StatusBar
			{
				private Point m_fixed;
				private bool m_resizing;

				public SizeBar()
				{
					Dock = DockStyle.Bottom;
					SizingGrip = true;
					Height = 12;
				}
				protected override void OnMouseDown(MouseEventArgs e)
				{
					base.OnMouseDown(e);
					if (Width - e.Location.X < 12)
					{
						m_fixed = Parent.Location;
						Capture = true;
						m_resizing = true;
					}
				}
				protected override void OnMouseMove(MouseEventArgs e)
				{
					base.OnMouseMove(e);
					Cursor = Width - e.Location.X < 12 ? Cursors.SizeNWSE : Cursors.Default;
					if (m_resizing)
					{
						var loc = PointToScreen(e.Location);
						Parent.Size = new Size(loc.X - m_fixed.X, loc.Y - m_fixed.Y);
					}
				}
				protected override void OnMouseUp(MouseEventArgs e)
				{
					base.OnMouseUp(e);
					Cursor = Cursors.Default;
					m_resizing = false;
				}
			}
			private readonly EditingControl m_ctrl;

			public EditField(Element elem, EditingControl ctrl)
			{
				m_ctrl = ctrl;
				m_ctrl.Ctrl.Dock = DockStyle.Fill;

				ShowInTaskbar   = false;
				FormBorderStyle = FormBorderStyle.None;
				MinimumSize     = new Size(80,30);
				MaximumSize     = new Size(640,480);
				KeyPreview      = true;
				Margin          = Padding.Empty;
				StartPosition   = FormStartPosition.Manual;

				var bnds    = elem.Diagram.RectangleToScreen(elem.Diagram.DiagramToClient(elem.Bounds));
				bnds.Width  = Math.Min(Bounds.Width, 80);
				bnds.Height = Math.Min(Bounds.Height, 30);
				Bounds      = bnds;

				Controls.Add(m_ctrl.Ctrl);
				Controls.Add(new SizeBar());
			}
			protected override void OnShown(EventArgs e)
			{
				m_ctrl.Ctrl.Capture = true;
				m_ctrl.Ctrl.MouseDown += ClickToClose;
				base.OnShown(e);
			}
			protected override void OnClosed(EventArgs e)
			{
				m_ctrl.Ctrl.MouseDown -= ClickToClose;
				m_ctrl.Ctrl.Capture = false;
				base.OnClosed(e);
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
				base.OnKeyDown(e);
			}
			private void ClickToClose(object sender, MouseEventArgs e)
			{
				var pt = e.Location;
				if (pt.X < 0 || pt.Y < 0 || pt.X > Size.Width || pt.Y > Size.Height)
				{
					DialogResult = DialogResult.Cancel;
					Close();
				}
			}
		}

		#endregion

		#region Misc

		/// <summary>How close a click as to be for selection to occur (in screen space)</summary>
		private const float MinCSSelectionDistanceSq = 100f;

		/// <summary>Minimum distance in pixels before the diagram starts dragging</summary>
		private const int MinDragPixelDistanceSq = 25;

		/// <summary>The minimum distance a connector sticks out from a node</summary>
		private const int MinConnectorLen = 30;

		public class Cursors
		{
			public static readonly Cursor Default    = System.Windows.Forms.Cursors.Default;
			public static readonly Cursor SizeWE     = System.Windows.Forms.Cursors.SizeWE;
			public static readonly Cursor SizeNS     = System.Windows.Forms.Cursors.SizeNS;
			public static readonly Cursor SizeNESW   = System.Windows.Forms.Cursors.SizeNESW;
			public static readonly Cursor SizeNWSE   = System.Windows.Forms.Cursors.SizeNWSE;
			public static readonly Cursor SizeAll    = System.Windows.Forms.Cursors.SizeAll;
			public static readonly Cursor Arrow      = Resources.cursor_arrow.ToCursor(Point.Empty);
			public static readonly Cursor ArrowPlus  = Resources.cursor_arrow_plus.ToCursor(Point.Empty);
			public static readonly Cursor ArrowMinus = Resources.cursor_arrow_minus.ToCursor(Point.Empty);
		}

		/// <summary>String constants used in XML export/import</summary>
		public static class XmlTag
		{
			public const string Root              = "root"               ;
			public const string TypeAttribute     = "ty"                 ;
			public const string Element           = "elem"               ;
			public const string Id                = "id"                 ;
			public const string Position          = "pos"                ;
			public const string Text              = "text"               ;
			public const string AutoSize          = "autosize"           ;
			public const string SizeMax           = "sizemax"            ;
			public const string Size              = "size"               ;
			public const string CornerRadius      = "cnr"                ;
			public const string Anchor            = "anc"                ;
			public const string Anchor0           = "anc0"               ;
			public const string Anchor1           = "anc1"               ;
			public const string CentreOffset      = "centre_offset"      ;
			public const string Label             = "label"              ;
			public const string TexturedShape     = "textured_shape"     ;
			public const string Shape             = "shape"              ;
			public const string Surface           = "surface"            ;
			public const string TextureScale      = "texture_scale"      ;
			public const string Styles            = "style"              ;
			public const string NodeStyles        = "node_styles"        ;
			public const string ConnStyles        = "conn_styles"        ;
			public const string Style             = "style"              ;
			public const string Border            = "border"             ;
			public const string Fill              = "fill"               ;
			public const string Selected          = "selected"           ;
			public const string Disabled          = "disabled"           ;
			public const string TextDisabled      = "text_disabled"      ;
			public const string Dangling          = "dangling"           ;
			public const string Font              = "font"               ;
			public const string Type              = "type"               ;
			public const string Align             = "align"              ;
			public const string Line              = "line"               ;
			public const string Width             = "width"              ;
			public const string Smooth            = "smooth"             ;
			public const string Padding           = "padding"            ;
			public const string ElementId         = "elem_id"            ;
			public const string Location          = "loc"                ;
			public const string Margin            = "margin"             ;
			public const string AutoRelink        = "auto_relink"        ;
			public const string Iterations        = "iterations"         ;
			public const string SpringConstant    = "spring_constant"    ;
			public const string CoulombConstant   = "coulomb_constant"   ;
			public const string ConnectorScale    = "connector_scale"    ;
			public const string Equilibrium       = "equilibrium"        ;
			public const string NodeSettings      = "node_settings"      ;
			public const string ScatterSettings   = "scatter_settings"   ;
			public const string BkColour          = "bk_colour"          ;
			public const string AnchorSpacing     = "anchor_spacing"     ;
			public const string AnchorSharingBias = "anchor_sharing_bias";
		}

		/// <summary>A 'mixin' class for elements containing a texture</summary>
		public class Surface :IDisposable
		{
			public Surface(uint sx, uint sy, uint texture_scale = 2, string dbg_name = null)
			{
				Debug.Assert(texture_scale != 0);
				TextureScale = texture_scale;
				sx = Math.Max(1, sx * TextureScale);
				sy = Math.Max(1, sy * TextureScale);
				Surf = new View3d.Texture(sx, sy, new View3d.TextureOptions(true){ Filter=View3d.EFilter.D3D11_FILTER_MIN_MAG_MIP_LINEAR, DbgName=dbg_name});// D3D11_FILTER_ANISOTROPIC});
			}
			public Surface(XElement node)
			{
				TextureScale = node.Element(XmlTag.TextureScale).As<uint>();
				var sz = node.Element(XmlTag.Size).As<v2>();
				var sx = Math.Max(1, (uint)(sz.x + 0.5f));
				var sy = Math.Max(1, (uint)(sz.y + 0.5f));
				Surf = new View3d.Texture(sx, sy, new View3d.TextureOptions(true){Filter=View3d.EFilter.D3D11_FILTER_MIN_MAG_MIP_LINEAR });// D3D11_FILTER_ANISOTROPIC});
			}
			public void Dispose()
			{
				Surf = null;
			}

			/// <summary>Export to XML</summary>
			public XElement ToXml(XElement node)
			{
				node.Add2(XmlTag.TextureScale, TextureScale, false);
				node.Add2(XmlTag.Size, Size, false);
				return node;
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
					if (tex_size == System.Drawing.Size.Empty) tex_size = new Size(1,1);
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
			public View3d.Texture.Lock LockSurface(bool discard)
			{
				var lck = Surf.LockSurface(discard);
				lck.Gfx.ScaleTransform(TextureScale, TextureScale);
				return lck;
			}
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
			public View3d.Camera Camera { get; private set; }

			public HitTestResult(View3d.Camera cam)
			{
				Hits = new List<Hit>();
				Camera = cam;
			}
		}

		/// <summary>A collection of graphics used by the diagram itself</summary>
		public class Tools :IDisposable
		{
			public static readonly Guid Id = Guid.NewGuid();

			public Tools()
			{
				m_area_select = new View3d.Object("*Rect selection 80000000 {3 1 1 *Solid}", false, Id);
				Resizer = Array_.New(8, i => new ResizeGrabber(i));
			}
			public void Dispose()
			{
				Util.Dispose(ref m_area_select);
				if (Resizer != null)
				{
					Resizer.ForEach(x => x.Dispose());
					Resizer = null;
				}
			}

			/// <summary>Graphic for area selection</summary>
			public View3d.Object AreaSelect { get { return m_area_select; } }
			private View3d.Object m_area_select;

			/// <summary>Graphics for the resizing grab zones</summary>
			public ResizeGrabber[] Resizer { get; private set; }
			public class ResizeGrabber :View3d.Object
			{
				public ResizeGrabber(int corner)
					:base("*Box {5}", false, Id)
				{
					switch (corner)
					{
					case 0:
						Cursor = Cursors.SizeNESW;
						Direction = v2.Normalise2(new v2(-1,-1));
						Update = (b,z) => O2P = m4x4.Translation(b.Lower.x, b.Lower.y, z);
						break;
					case 1:
						Cursor = Cursors.SizeNESW;
						Direction = v2.Normalise2(new v2(+1,+1));
						Update = (b,z) => O2P = m4x4.Translation(b.Upper.x, b.Upper.y, z);
						break;
					case 2:
						Cursor = Cursors.SizeNWSE;
						Direction = v2.Normalise2(new v2(-1,+1));
						Update = (b,z) => O2P = m4x4.Translation(b.Lower.x, b.Upper.y, z);
						break;
					case 3:
						Cursor = Cursors.SizeNWSE;
						Direction = v2.Normalise2(new v2(+1,-1));
						Update = (b,z) => O2P = m4x4.Translation(b.Upper.x, b.Lower.y, z);
						break;
					case 4:
						Direction = new v2(-1,0);
						Cursor = Cursors.SizeWE;
						Update = (b,z) => O2P = m4x4.Translation(b.Lower.x, b.Centre.y, z);
						break;
					case 5:
						Direction = new v2(+1,0);
						Cursor = Cursors.SizeWE;
						Update = (b,z) => O2P = m4x4.Translation(b.Upper.x, b.Centre.y, z);
						break;
					case 6:
						Direction = new v2(0,-1);
						Cursor = Cursors.SizeNS;
						Update = (b,z) => O2P = m4x4.Translation(b.Centre.x, b.Lower.y, z);
						break;
					case 7:
						Direction = new v2(0,+1);
						Cursor = Cursors.SizeNS;
						Update = (b,z) => O2P = m4x4.Translation(b.Centre.x, b.Upper.y, z);
						break;
					}
				}

				/// <summary>The direction that this grabber can resize in </summary>
				public v2 Direction { get; private set; }

				/// <summary>The cursor to display when this grabber is used</summary>
				public Cursor Cursor { get; set; }

				/// <summary>Updates the position of the grabber</summary>
				public Action<BRect,float> Update;
			}
		}

		/// <summary>HashSet wrapper for dirty Elements</summary>
		public class DirtyElements :HashSet<Element>
		{
			private readonly DiagramControl m_diag;
			public DirtyElements(DiagramControl owner)
			{
				m_diag = owner;
			}
			public new void Add(Element item)
			{
				Debug.Assert(item.Diagram == m_diag);
				base.Add(item);
			}
		}
		#endregion

		#region Options

		/// <summary>Rendering options</summary>
		[TypeConverter(typeof(DiagramOptions))]
		public class DiagramOptions :GenericTypeConverter<DiagramOptions>
		{
			// Graph margins and constants
			[TypeConverter(typeof(NodeOptions))]
			public class NodeOptions :GenericTypeConverter<NodeOptions>
			{
				/// <summary>This distance between snap-to points on a node</summary>
				[Description("This distance between snap-to points on a node")]
				public float AnchorSpacing { get; set; }

				/// <summary>Anchor point sharing bias</summary>
				[Description("Controls how anchor points are chosen in automatic relinking. Larger values reduce anchor point sharing")]
				public float AnchorSharingBias { get; set; }

				/// <summary>The margin between nodes</summary>
				[Description("")]
				public float Margin { get; set; }

				/// <summary>Set to true to have links between nodes dynamically reconnect</summary>
				[Description("Set to true to have links between nodes dynamically reconnect")]
				public bool AutoRelink { get; set; }

				public NodeOptions()
				{
					AnchorSpacing     = 25f;
					AnchorSharingBias = 150f;
					Margin            = 30f;
					AutoRelink        = false;
				}
				internal NodeOptions(XElement node) :this()
				{
					AnchorSpacing     = node.Element(XmlTag.AnchorSpacing    ).As <float >(AnchorSpacing    );
					AnchorSharingBias = node.Element(XmlTag.AnchorSharingBias).As <float >(AnchorSharingBias);
					Margin            = node.Element(XmlTag.Margin           ).As <float >(Margin           );
					AutoRelink        = node.Element(XmlTag.AutoRelink       ).As <bool  >(AutoRelink       );
				}
				internal XElement ToXml(XElement node)
				{
					node.Add2(XmlTag.AnchorSpacing     , AnchorSpacing     , false);
					node.Add2(XmlTag.AnchorSharingBias , AnchorSharingBias , false);
					node.Add2(XmlTag.Margin            , Margin            , false);
					node.Add2(XmlTag.AutoRelink        , AutoRelink        , false);
					return node;
				}
			}

			// Scatter parameters
			[TypeConverter(typeof(ScatterOptions))]
			public class ScatterOptions :GenericTypeConverter<ScatterOptions>
			{
				[Description("The number of iterations to use when scattering nodes")]
				public int MaxIterations { get; set; }

				[Description("Tuning constant for the attractive force between connected nodes")]
				public float SpringConstant { get; set; }

				[Description("Tuning constant for the repulsive force between nodes")]
				public float CoulombConstant { get; set; }

				[Description("Tuning constant that multiples the coulomb force proportionally to the number of connections a node has")]
				public float ConnectorScale { get; set; }

				[Description("Threshold for node movements that indicates equilibrium has been reached")]
				public float Equilibrium { get; set; }

				public ScatterOptions()
				{
					MaxIterations   = 20;
					SpringConstant  = 0.01f;
					CoulombConstant = 1000f;
					ConnectorScale  = 1f;
					Equilibrium     = 0.01f;
				}
				internal ScatterOptions(XElement node) :this()
				{
					MaxIterations   = node.Element(XmlTag.Iterations     ).As<int>  (MaxIterations  );
					SpringConstant  = node.Element(XmlTag.SpringConstant ).As<float>(SpringConstant );
					CoulombConstant = node.Element(XmlTag.CoulombConstant).As<float>(CoulombConstant);
					ConnectorScale  = node.Element(XmlTag.ConnectorScale ).As<float>(ConnectorScale );
					Equilibrium     = node.Element(XmlTag.Equilibrium    ).As<float>(Equilibrium    );
				}
				internal XElement ToXml(XElement node)
				{
					node.Add2(XmlTag.Iterations      ,MaxIterations   ,false);
					node.Add2(XmlTag.SpringConstant  ,SpringConstant  ,false);
					node.Add2(XmlTag.CoulombConstant ,CoulombConstant ,false);
					node.Add2(XmlTag.ConnectorScale  ,ConnectorScale  ,false);
					node.Add2(XmlTag.Equilibrium     ,Equilibrium     ,false);
					return node;
				}
			}

			/// <summary>The fill colour of the background</summary>
			[Description("The background colour of the diagram")]
			public Color BkColour { get; set; }

			/// <summary>Node options</summary>
			[Description("Node Options")]
			public NodeOptions Node { get; set; }
			
			/// <summary>Scatter options</summary>
			[Description("Node Scatter Options")]
			public ScatterOptions Scatter { get; set; }

			public DiagramOptions()
			{
				BkColour = SystemColors.ControlDark;      // The fill colour of the background
				Node     = new NodeOptions();
				Scatter  = new ScatterOptions();
			}
			public DiagramOptions(XElement node) :this()
			{
				BkColour = node.Element(XmlTag.BkColour       ).As<Color>(BkColour);
				Node     = node.Element(XmlTag.NodeSettings   ).As<NodeOptions>(Node);
				Scatter  = node.Element(XmlTag.ScatterSettings).As<ScatterOptions>(Scatter);
			}
			public XElement ToXml(XElement node)
			{
				node.Add2(XmlTag.BkColour        , BkColour , false);
				node.Add2(XmlTag.NodeSettings    ,Node      ,false );
				node.Add2(XmlTag.ScatterSettings ,Scatter   ,false );
				return node;
			}
			public DiagramOptions Clone()
			{
				return (DiagramOptions)MemberwiseClone();
			}
		}

		#endregion

		// Members
		private View3d                     m_view3d;           // Renderer
		private View3d.Window              m_window;           // A view3d window for this control instance
		private HoverScroll                m_hoverscroll;      // Hover-scroll
		private View3d.Camera              m_camera;           // The virtual window over the diagram
		private Tools                      m_tools;            // Tools
		private StyleCache<NodeStyle>      m_node_styles;      // The collection of node styles
		private StyleCache<ConnectorStyle> m_connector_styles; //
		private MouseOps                   m_mouse_op;         // Per button current mouse operation
		private ToolStrip                  m_toolstrip_edit;   //
		private readonly DirtyElements     m_dirty;            // Elements that need refreshing

		public DiagramControl() :this(new DiagramOptions()) {}
		public DiagramControl(DiagramOptions options)
		{
			try
			{
				ConsistencyCheckSuspended = true;

				Elements              = new BindingListEx<Element> { PerItem = true, UseHashSet = true };
				Selected              = new BindingListEx<Element> { PerItem = false };
				ElemIds               = new Dictionary<Guid, Element>();
				m_impl_options        = options ?? new DiagramOptions();
				m_hoverscroll         = new HoverScroll(Handle);
				m_node_styles         = new StyleCache<NodeStyle>();
				m_connector_styles    = new StyleCache<ConnectorStyle>();
				m_mouse_op            = new MouseOps();
				m_toolstrip_edit      = new ToolStrip{Name = "m_diagram_edit_tools", Visible = false, Dock = DockStyle.Right};
				m_dirty               = new DirtyElements(this);

				if (this.IsInDesignMode()) return;

				// We don't need GDI compatibility in the swap chain,
				// it's only textures that we're using with the GDI.
				m_view3d = View3d.Create();
				m_window = new View3d.Window(m_view3d, Handle, new View3d.WindowOptions(null, IntPtr.Zero) { DbgName = "Diagram" });
				m_tools  = new Tools();
				m_camera = m_window.Camera;
				m_camera.ClipPlanes(0.5f, 1.1f, true);
				m_window.LightProperties = View3d.LightInfo.Directional(-v4.ZAxis, Colour32.Zero, Colour32.Gray, Colour32.Zero, 0f, 0f);
				m_window.FocusPointVisible = false;
				m_window.OriginPointVisible = false;
				m_window.Orthographic = false;

				InitializeComponent();
				SetupEditToolstrip();

				Elements.ListChanging += (s,a) => OnElementListChanging(a);
				Selected.ListChanging += (s,a) => OnSelectionListChanging(a);

				SetCursor();
				ResetView();
				DefaultKeyboardShortcuts = true;
				DefaultMouseControl = true;
				AllowEditing = true;
				AllowSelection = true;

				// Enable self-checking
				ConsistencyCheckSuspended = false;
			}
			catch
			{
				Dispose();
				throw;
			}
		}
		protected override void Dispose(bool disposing)
		{
			// We don't own the elements, so don't dispose them
			ResetDiagram();
			Util.Dispose(ref m_hoverscroll);
			Util.Dispose(ref m_tools);
			Util.Dispose(ref m_window);
			Util.Dispose(ref m_view3d);
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>Raised whenever elements in the diagram have been edited or moved</summary>
		public event EventHandler<DiagramChangedEventArgs> DiagramChanged;
		protected virtual void OnDiagramChanged(DiagramChangedEventArgs args)
		{
			DiagramChanged.Raise(this, args);
		}
		private DiagramChangedEventArgs RaiseDiagramChanged(DiagramChangedEventArgs args)
		{
			if (!DiagramChanged.IsSuspended())
				OnDiagramChanged(args);
			return args;
		}
		public Scope SuspendDiagramChanged(bool raise_on_resume = true)
		{
			return DiagramChanged.Suspend(raise_if_signalled:raise_on_resume, sender:this, args:new DiagramChangedEventArgs(EDiagramChangeType.Edited));
		}

		/// <summary>Remove all data from the diagram</summary>
		public void ResetDiagram()
		{
			if (Elements != null)
				Elements.Clear();
		}

		/// <summary>All diagram objects</summary>
		public BindingListEx<Element> Elements { get; private set; }

		/// <summary>The set of selected diagram elements</summary>
		public BindingListEx<Element> Selected { get; private set; }

		/// <summary>Associative array from Element Ids to Elements</summary>
		public Dictionary<Guid, Element> ElemIds { get; private set; }

		/// <summary>Event allowing callers to add options to the context menu</summary>
		public event EventHandler<AddUserMenuOptionsEventArgs> AddUserMenuOptions;
		protected virtual void OnAddUserMenuOptions(AddUserMenuOptionsEventArgs args)
		{
			AddUserMenuOptions.Raise(this, args);
		}

		/// <summary>Controls for how the diagram is rendered</summary>
		public DiagramOptions Options
		{
			get { return m_impl_options; }
			set
			{
				m_impl_options = value ?? new DiagramOptions();
				InvalidateAllElements();
				Invalidate();
			}
		}
		private DiagramOptions m_impl_options;
		
		/// <summary>Raised after the options dialog is closed</summary>
		public event EventHandler OptionsChanged;

		/// <summary>Minimum bounding area for view reset</summary>
		public BRect ResetMinBounds
		{
			get { return new BRect(v2.Zero, new v2(Width/1.5f, Height/1.5f)); }
		}

		/// <summary>Perform a hit test on the diagram</summary>
		public HitTestResult HitTest(v2 ds_point, Func<Element, bool> pred)
		{
			var result = new HitTestResult(m_camera);
			var elements = pred != null ? Elements.Where(pred) : Elements;
			foreach (var elem in elements)
			{
				var hit = elem.HitTest(ds_point, m_camera);
				if (hit != null)
					result.Hits.Add(hit);
			}

			// Sort the results by type then z order
			result.Hits.Sort((l,r) =>
				{
					if (l.Entity != r.Entity) return l.Entity.CompareTo(r.Entity);
					return -l.Element.PositionZ.CompareTo(r.Element.PositionZ);
				});
			return result;
		}
		public HitTestResult HitTestCS(Point cs_point, Func<Element, bool> pred)
		{
			return HitTest(ClientToDiagram(cs_point), pred);
		}

		/// <summary>Standard keyboard shortcuts</summary>
		public virtual void TranslateKey(object sender, KeyEventArgs args)
		{
			switch (args.KeyCode)
			{
			case Keys.Escape:
				#region Clear Selection
				{
					if (AllowSelection)
					{
						Selected.Clear();
						Invalidate();
					}
					break;
				}
				#endregion
			case Keys.Delete:
				#region Delete Elements
				{
					if (AllowEditing)
					{
						// Allow the caller to cancel the deletion or change the selection
						var res = new DiagramChangedRemoveElementsEventArgs(Selected.ToArray());
						if (!RaiseDiagramChanged(res).Cancel)
						{
							foreach (var elem in res.Elements)
							{
								var node = elem as Node;
								if (node != null)
									node.DetachConnectors();
								
								var conn = elem as Connector;
								if (conn != null)
									conn.DetachNodes();

								Elements.Remove(elem);
							}
							Invalidate();
						}
					}
					break;
				}
				#endregion
			case Keys.F5:
				#region Redraw Diagram
				{
					InvalidateAllElements();
					Invalidate();
					break;
				}
				#endregion
			case Keys.F7:
				#region Reset View
				{
					ResetView();
					break;
				}
				#endregion
			case Keys.A:
				#region Select All
				{
					if ((args.Modifiers & Keys.Control) != 0)
					{
						Selected.Clear();
						Selected.AddRange(Elements);
						Invalidate();
						Debug.Assert(CheckConsistency());
					}
					break;
				}
				#endregion
			}
		}

		/// <summary>Enable/Disable default keyboard shortcuts</summary>
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
		public bool DefaultMouseControl { get; set; }

		/// <summary>Get the default node style</summary>
		public NodeStyle DefaultNodeStyle
		{
			get { return m_node_styles[Guid.Empty]; }
		}

		/// <summary>Get the default connector style</summary>
		public ConnectorStyle DefaultConnectorStyle
		{
			get { return m_connector_styles[Guid.Empty]; }
		}

		/// <summary>True if users are allowed to add/remove/edit nodes on the diagram</summary>
		public bool AllowEditing
		{
			get { return m_impl_allow_editing; }
			set
			{
				m_impl_allow_editing = value;
				UpdateEditToolbar();
			}
		}
		private bool m_impl_allow_editing;

		/// <summary>True if users are allowed to select elements on the diagram</summary>
		public bool AllowSelection { get; set; }

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			
			// If a mouse op is already active, ignore mouse down
			if (m_mouse_op.Active != null)
				return;

			// Look for the mouse op to perform
			var btn = e.Button.ButtonIndex();
			if (m_mouse_op.Pending(btn) == null && DefaultMouseControl)
				m_mouse_op.SetPending(btn, CreateDefaultMouseOp(e.Button, e.Location));

			// Start the next mouse op
			m_mouse_op.BeginOp(btn);

			// Get the mouse op, save mouse location and hit test data, then call op.MouseDown()
			var op = m_mouse_op.Active;
			if (op != null && !op.Cancelled)
			{
				op.m_btn_down   = true;
				op.m_grab_cs    = e.Location;
				op.m_grab_ds    = ClientToDiagram(op.m_grab_cs);
				op.m_hit_result = HitTest(op.m_grab_ds, null);
				op.MouseDown(e);
				Capture = true;
			}
		}
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			// Look for the mouse op to perform
			var op = m_mouse_op.Active;
			if (op != null && !op.Cancelled)
				op.MouseMove(e);
		}
		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);

			// Only release the mouse when all buttons are up
			if (MouseButtons == MouseButtons.None)
				Capture = false;

			// Look for the mouse op to perform
			var btn = e.Button.ButtonIndex();
			var op = m_mouse_op.Active;
			if (op != null && !op.Cancelled)
				op.MouseUp(e);
			
			m_mouse_op.EndOp(btn);
			UpdateEditToolbar();
		}
		protected override void OnMouseWheel(MouseEventArgs e)
		{
			base.OnMouseWheel(e);

			var delta = e.Delta < -999 ? -999 : e.Delta > 999 ? 999 : e.Delta;
			if (m_window.Navigate(0, 0, e.Delta / 120f))
				Invalidate();
		}
		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			SetCursor();

			var op = m_mouse_op.Active;
			if (op != null)
				op.OnKeyDown(e);
		}
		protected override void OnKeyUp(KeyEventArgs e)
		{
			base.OnKeyUp(e);
			SetCursor();

			var op = m_mouse_op.Active;
			if (op != null)
				op.OnKeyUp(e);
		}

		/// <summary>Set the mouse cursor based on key state</summary>
		private void SetCursor()
		{
			if      (ModifierKeys.HasFlag(Keys.Shift))   Cursor = Cursors.ArrowPlus;
			else if (ModifierKeys.HasFlag(Keys.Control)) Cursor = Cursors.ArrowMinus;
			else                                         Cursor = Cursors.Default;
		}

		/// <summary>Create the default navigation mouse operation based on mouse button</summary>
		private MouseOp CreateDefaultMouseOp(MouseButtons btn, Point pt_cs)
		{
			switch (btn)
			{
			default: return null;
			case MouseButtons.Left:
				{
					// If elements are selected, see if the mouse has selected one of the resize-grabbers
					if (AllowEditing && SelectionResizeable)
					{
						var pt_ds = ClientToDiagram(pt_cs);
						var nearest = m_tools.Resizer.MinBy(x => (x.O2P.pos.xy - pt_ds).Length2Sq);
						if (m_camera.WSVecToSSVec(pt_ds, nearest.O2P.pos.xy).Length2Sq < MinCSSelectionDistanceSq)
							return new MouseOpResize(this, nearest);
					}

					// Otherwise, normal drag/click behaviour
					return new MouseOpDragOrClickElements(this);
				}
			case MouseButtons.Right:
				{
					return new MouseOpDragOrClickDiagram(this);
				}
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
			var ht = HitTestCS(e.Location, x => x.Enabled);
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
			Invalidate();
		}

		/// <summary>Returns a point in diagram space from a point in client space. Use to convert mouse (client-space) locations to diagram coordinates</summary>
		public v2 ClientToDiagram(Point point)
		{
			var ws = m_camera.SSPointToWSPoint(point);
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
			return m_camera.WSPointToSSPoint(ws);
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
			if (m_window.Navigate(ds.x - dst.x, ds.y - dst.y, 0))
				Invalidate();
		}

		/// <summary>Move the selected elements by 'delta'</summary>
		private void DragSelected(v2 delta, bool commit)
		{
			if (!AllowEditing) return;
			foreach (var elem in Selected)
				elem.Drag(delta, commit);
		}

		/// <summary>Returns a selected element if it is the only selected element</summary>
		private Element SingleSelection
		{
			get { return Selected.Count == 1 ? Selected[0] : null; }
		}

		/// <summary>Handle elements added/removed from the elements list</summary>
		protected virtual void OnElementListChanging(ListChgEventArgs<Element> args)
		{
			var elem = args.Item;
			if (elem != null && (elem.Diagram != null && elem.Diagram != this))
				throw new ArgumentException("element belongs to another diagram");

			switch (args.ChangeType)
			{
			case ListChg.Reset:
				{
					Elements.ForEach(x => x.SetDiagramInternal(this, false));
					ElemIds.Clear();
					Elements.ForEach(x => ElemIds.Add(x.Id, x));
					Debug.Assert(CheckConsistency());
					break;
				}
			case ListChg.ItemAdded:
				{
					elem.SetDiagramInternal(this, false);
					ElemIds.Add(elem.Id, elem);
					Debug.Assert(CheckConsistency());
					break;
				}
			case ListChg.ItemRemoved:
				{
					elem.SetDiagramInternal(null, false);
					ElemIds.Remove(elem.Id);
					Debug.Assert(CheckConsistency());
					break;
				}
			}
		}

		/// <summary>Handle elements added/removed from the selection list</summary>
		protected virtual void OnSelectionListChanging(ListChgEventArgs<Element> args)
		{
			var elem = args.Item;
			if (elem != null && (elem.Diagram != null && elem.Diagram != this))
				throw new ArgumentException("element belongs to another diagram");

			switch (args.ChangeType)
			{
			case ListChg.Reset:
				{
					Elements.ForEach(x => x.SetSelectedInternal(false, false));
					Selected.ForEach(x => x.SetSelectedInternal(true, false));
					Debug.Assert(CheckConsistency());
					break;
				}
			case ListChg.ItemAdded:
				{
					elem.SetSelectedInternal(true, false);
					Debug.Assert(CheckConsistency());
					break;
				}
			case ListChg.ItemRemoved:
				{
					elem.SetSelectedInternal(false, false);
					Debug.Assert(CheckConsistency());
					break;
				}
			}
		}

		/// <summary>Deselect all elements</summary>
		public void ClearSelection()
		{
			Selected.Clear();
		}

		/// <summary>
		/// Select elements that are wholly within 'rect'. (rect is in diagram space)
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
				var hits = HitTest(rect.Location, x => x.Enabled);

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

					// Deselect all elements and select the next element
					Selected.Clear();
					if (to_select != null)
						to_select.Element.Selected = true;
				}
			}
			// Otherwise it's area selection
			else
			{
				using (Selected.SuspendEvents(true))
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
			}

			Invalidate();
		}

		/// <summary>True if any of the selected elements can be resized by the user</summary>
		public bool SelectionResizeable
		{
			get { return Selected.Any(x => x.Resizeable); }
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

			Invalidate();
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
					var arr = nodes.Select(x => x.SuspendEvents()).ToArray();
					foreach (var x in conns) x.Visible = false;
					return arr;
				},
				arr =>
				{
					Util.DisposeAll(arr);
					foreach (var x in conns) x.Visible = true;
				}))
			{
				// Prevent issues with nodes exactly on top of each other
				var rng = new Random(); var djitter = 10f;
				Func<v2> jitter = () => v2.Random2(-djitter, djitter, rng);

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
						qtree.Insert(node, node.Bounds.Centre.ToArray(), node.Bounds.Diametre/2);

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
						qtree.Traverse(node0.PositionXY.ToArray(), node0.Bounds.Diametre, node1 =>
							{
								if (node0 == node1) return true;

								// Find the minimum separation and the current separation
								var vec     = node1.PositionXY - node0.PositionXY;
								var min_sep = min_separation(vec, node0, node1);
								var sep     = Math.Max(min_sep, vec.Length2);

								// Coulomb force F = kQq/x�
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

					Invalidate();

					if (equilibrium)
						break;
				}
			}

			// Re-link all connectors connected to nodes that have moved
			foreach (var c in nodes.SelectMany(x => x.Connectors).Distinct())
				c.Relink(!Options.Node.AutoRelink);
		}

		/// <summary>Update the links between nodes to pick the nearest anchor points</summary>
		public void RelinkNodes()
		{
			// Re-link the connectors from selected nodes or all connectors if nothing is selected
			var connectors = Selected.Count != 0
				? Selected.Where(x => x.Entity == Entity.Node).Cast<Node>().SelectMany(x => x.Connectors)
				: Elements.Where(x => x.Entity == Entity.Connector).Cast<Connector>();
			foreach (var connector in connectors)
				connector.Relink(false);

			var nodes = Selected.Count != 0
				? Selected.Where(x => x.Entity == Entity.Node).Cast<Node>()
				: Elements.Where(x => x.Entity == Entity.Node).Cast<Node>();
			foreach (var node in nodes)
				node.Untangle();

			Invalidate();
		}

		/// <summary>The Z value of the highest element in the diagram</summary>
		private float HighestZ { get; set; }

		/// <summary>The Z value of the lowest element in the diagram</summary>
		private float LowestZ { get; set; }

		/// <summary>Set the Z value for all elements.</summary>
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

		/// <summary>Cause all diagram elements to mark themselves as dirty (requiring repaint)</summary>
		private void InvalidateAllElements()
		{
			foreach (var elem in Elements)
				elem.Invalidate();
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

		/// <summary>Get the area encompassing the selected elements</summary>
		public BRect SelectionBounds
		{
			get
			{
				var bounds = BRect.Reset;
				foreach (var elem in Selected)
				{
					var b = elem.Bounds;
					if (b.IsValid) bounds.Encompass(b);
				}
				return bounds;
			}
		}

		/// <summary>Resize the control</summary>
		protected override void OnResize(EventArgs e)
		{
			if (m_window == null || this.IsInDesignMode())
			{
				base.OnResize(e);
				return;
			}

			base.OnResize(e);
			m_window.RenderTargetSize = new Size(Width,Height);
		}

		/// <summary>Control has been invalidated</summary>
		protected override void OnInvalidated(InvalidateEventArgs e)
		{
			if (m_window == null || this.IsInDesignMode())
			{
				base.OnInvalidated(e);
				return;
			}

			m_window.Invalidate();
			base.OnInvalidated(e);
		}

		/// <summary>Clear background</summary>
		protected override void OnPaintBackground(PaintEventArgs e)
		{
			if (m_window == null || this.IsInDesignMode())
			{
				base.OnPaintBackground(e);
				return;
			}
		}

		/// <summary>Paint the control</summary>
		protected override void OnPaint(PaintEventArgs e)
		{
			if (m_window == null || this.IsInDesignMode())
			{
				base.OnPaint(e);
				return;
			}

			// Prevent updating element graphics raising the data changed event
			using (SuspendDiagramChanged(raise_on_resume: false))
			{
				// Update the invalidated elements
				while (m_dirty.Count != 0)
				{
					var elem = m_dirty.First();
					m_dirty.Remove(elem);
					elem.UpdateGfx();
				}

				// Ensure the z order is up to date
				UpdateElementZOrder();

				// Update the state of the edit tool bar
				UpdateEditToolbar();
			}

			// Clear the scene
			m_window.RemoveAllObjects();

			// Add the elements to the window
			foreach (var elem in Elements)
			{
				if (!elem.Visible) continue;
				elem.AddToScene(m_window);
			}

			// Add selection graphics is something is selected
			if (AllowEditing && SelectionResizeable)
			{
				// Position the resizer tool above the other graphics
				var z = HighestZ;
				var bounds = BRect.Reset;
				foreach (var elem in Selected.OfType<ResizeableElement>().Where(x => x.Resizeable))
				{
					var b = elem.Bounds;
					if (b.IsValid) bounds.Encompass(b);
				}
				m_tools.Resizer.ForEach(x => x.Update(bounds, z));

				// Add the resizer tool
				m_window.AddObjects(m_tools.Resizer);
			}

			// Render the diagram
			m_window.Render();
			m_window.Present();
		}

		[Flags] public enum EExportOptions
		{
			/// <summary>Export the nodes</summary>
			Nodes = 1 << 0,

			/// <summary>Export the connectors</summary>
			Connectors = 1 << 1,

			/// <summary>Export the labels</summary>
			Labels = 1 << 2,

			/// <summary>Export all nodes and connectors</summary>
			AllElements = Nodes | Connectors | Labels,

			/// <summary>Include node styles in the exported XML data</summary>
			NodeStyles = 1 << 3,

			/// <summary>Include connector styles in the exported XML data</summary>
			ConnectorStyles = 1 << 4,

			/// <summary>Include all styles in exported XML data</summary>
			AllStyles = NodeStyles | ConnectorStyles,

			/// <summary>Default XML export options</summary>
			Default = AllElements | AllStyles,
		}
		[Flags] public enum EImportOptions
		{
			/// <summary>Import nodes</summary>
			Nodes = 1 << 0,

			/// <summary>Import connectors</summary>
			Connectors = 1 << 1,

			/// <summary>Import labels</summary>
			Labels = 1 << 2,

			/// <summary>Import all elements</summary>
			AllElements = Nodes | Connectors | Labels,

			/// <summary>Import node styles from XML data</summary>
			NodeStyles = 1 << 3,

			/// <summary>Import connector styles from XML Data</summary>
			ConnectorStyles = 1 << 4,

			/// <summary>Import all styles from XML data</summary>
			AllStyles = NodeStyles | ConnectorStyles,

			/// <summary>Default XML import options</summary>
			Default = AllElements | AllStyles,

			/// <summary>XML nodes that fail to import will be silently ignored</summary>
			IgnoreImportFailures = 1 << 5,
		}

		/// <summary>Export the current diagram as XML</summary>
		public XElement ExportXml(XElement node, EExportOptions opts = EExportOptions.Default)
		{
			// Node styles
			if (opts.HasFlag(EExportOptions.NodeStyles))
			{
				m_node_styles.RemoveUnused(Elements);
				var ns = m_node_styles.Styles.OrderBy(x => x.Id);
				node.Add2(XmlTag.NodeStyles, ns, false);
			}

			// Connector styles
			if (opts.HasFlag(EExportOptions.ConnectorStyles))
			{
				m_connector_styles.RemoveUnused(Elements);
				var cs = m_connector_styles.Styles.OrderBy(x => x.Id);
				node.Add2(XmlTag.ConnStyles, cs, false);
			}

			// Sort the elements so that the produced XML has a stable order

			// Nodes
			if (opts.HasFlag(EExportOptions.Nodes))
			{
				var nodes = Elements.Where(x => x.Entity == Entity.Node && !(x is NodeProxy)).OrderBy(x => x.Id).ToArray();
				foreach (var elem in nodes)
					node.Add2(XmlTag.Element, elem, true);
			}

			// Connectors
			if (opts.HasFlag(EExportOptions.Connectors))
			{
				var conns = Elements.Where(x => x.Entity == Entity.Connector).OrderBy(x => x.Id).ToArray();
				foreach (var elem in conns)
					node.Add2(XmlTag.Element, elem, true);
			}

			// Labels
			if (opts.HasFlag(EExportOptions.Labels))
			{
				var labls = Elements.Where(x => x.Entity == Entity.Label).OrderBy(x => x.Id).ToArray();
				foreach (var elem in labls)
					node.Add2(XmlTag.Element, elem, true);
			}

			return node;
		}
		public XDocument ExportXml(EExportOptions opts = EExportOptions.Default)
		{
			var xml = new XDocument();
			var node = xml.Add2(new XElement(XmlTag.Root));
			ExportXml(node, opts);
			return xml;
		}

		/// <summary>
		/// Import the diagram from XML.
		/// If 'merge' is false, the diagram contents are replaced with the data from 'node'
		/// If true, the element data is copied from 'node' where Ids match. Unmatched Ids are added.</summary>
		public void ImportXml(XElement node, bool merge = false, EImportOptions opts = EImportOptions.Default)
		{
			// Build a map of Guids to elements
			var map = merge
				? Elements.ToDictionary(x => x.Id)
				: new Dictionary<Guid,Element>();

			// If not merging, clear the diagram data
			if (!merge)
				ResetDiagram();

			// Add a map function for AnchorPoint that references the elements being imported
			var xml_anchor_point = Scope.Create(
				() => Xml_.AsMap[typeof(AnchorPoint)] = (elem, type, ctor) => new AnchorPoint(map, elem),
				() => Xml_.AsMap[typeof(AnchorPoint)] = null);

			// Suspend diagram changed events while importing
			using (xml_anchor_point)
			using (SuspendDiagramChanged(raise_on_resume:false))
			{
				if (node != null)
				{
					foreach (var n in node.Elements())
					{
						switch (n.Name.LocalName)
						{
						case XmlTag.NodeStyles:
							#region
							{
								if (opts.HasFlag(EImportOptions.NodeStyles))
								{
									try
									{
										var node_styles = n.As<StyleCache<NodeStyle>>() ?? new StyleCache<NodeStyle>();
										if (!merge) m_node_styles = node_styles;
										else        m_node_styles.Merge(node_styles);
									}
									catch
									{
										if (!opts.HasFlag(EImportOptions.IgnoreImportFailures))
											throw;
									}
								}
								break;
							}
							#endregion
						case XmlTag.ConnStyles:
							#region
							{
								if (opts.HasFlag(EImportOptions.ConnectorStyles))
								{
									try
									{
										var conn_styles = n.As<StyleCache<ConnectorStyle>>() ?? new StyleCache<ConnectorStyle>();
										if (!merge) m_connector_styles = conn_styles;
										else        m_connector_styles.Merge(conn_styles);
									}
									catch
									{
										if (!opts.HasFlag(EImportOptions.IgnoreImportFailures))
											throw;
									}
								}
								break;
							}
							#endregion
						case XmlTag.Element:
							#region
							{
								// True if the 'opts' say import the element
								Func<Element, bool> import = e =>
									(e is Node      && opts.HasFlag(EImportOptions.Nodes)) ||
									(e is Connector && opts.HasFlag(EImportOptions.Connectors)) ||
									(e is Label     && opts.HasFlag(EImportOptions.Labels));

								try
								{
									// Add the element to the diagram
									if (!merge)
									{
										// Not merging, just add everything
										var elem = (Element)n.ToObject();
										if (import(elem))
										{
											map.Add(elem.Id, elem);
											Elements.Add(elem);
										}
									}
									else
									{
										// Read the id of the element and look for it among the existing elements
										var id = n.Element(XmlTag.Id).As<Guid>();

										Element elem = map.TryGetValue(id, out elem) ? elem : null;
										if (elem == null)
										{
											// If not found, add a new element
											elem = (Element)n.ToObject();
											if (import(elem))
											{
												map.Add(elem.Id, elem);
												Elements.Add(elem);
											}
										}
										else
										{
											// If found, update the existing element.
											if (import(elem))
												elem.Update(n);
										}
									}
								}
								catch
								{
									if (!opts.HasFlag(EImportOptions.IgnoreImportFailures))
										throw;
								}
								break;
							}
							#endregion
						}
					}
				}
			}

			// Remove unused styles
			m_node_styles.RemoveUnused(Elements);
			m_connector_styles.RemoveUnused(Elements);
		}

		/// <summary>
		/// Import the diagram layout from a string containing XML.
		/// If 'merge' is false, the diagram contents are replaced with the data from 'node'
		/// If true, the element data is copied from 'node' where Ids match. Unmatched Ids are added.</summary>
		public void ImportXml(string layout_xml, bool merge = false, EImportOptions opts = EImportOptions.Default)
		{
			var xml = XDocument.Parse(layout_xml);
			if (xml.Root == null) throw new InvalidDataException("XML file does not contain any config data");
			ImportXml(xml.Root, merge, opts);
		}

		/// <summary>
		/// Import the diagram layout from an XML file.
		/// If 'merge' is false, the diagram contents are replaced with the data from 'node'
		/// If true, the element data is copied from 'node' where Ids match. Unmatched Ids are added.</summary>
		public void LoadXml(string filepath, bool merge = false, EImportOptions opts = EImportOptions.Default)
		{
			var xml = XDocument.Load(filepath);
			if (xml.Root == null) throw new InvalidDataException("XML file does not contain any config data");
			ImportXml(xml.Root, merge, opts);
		}

		/// <summary>Create and display a context menu</summary>
		public void ShowContextMenu(Point location)
		{
			var cmenu = new ContextMenuStrip { Renderer = new ContextMenuRenderer() };
			using (cmenu.SuspendLayout(false))
			{
				#region Scatter
				if (AllowEditing)
				{
					cmenu.Items.AddSeparator();
					var opt = cmenu.Items.Add2(new ToolStripMenuItem("Scatter") { Name = CMenuItems.Scatter });
					opt.Click += (s,a) => ScatterNodes();
				}
				#endregion

				#region Re-link
				if (AllowEditing)
				{
					var opt = cmenu.Items.Add2(new ToolStripMenuItem("Re-link") { Name = CMenuItems.ReLink });
					opt.Click += (s,a) => RelinkNodes();
				}
				#endregion

				#region Bring to Front
				if (Selected.Count != 0)
				{
					var opt = cmenu.Items.Add2(new ToolStripMenuItem("Bring to Front") { Name = CMenuItems.BringToFront });
					opt.Enabled = Selected.Count != 0;
					opt.Click += (s,a) =>
					{
						using (SuspendDiagramChanged(raise_on_resume: true))
							foreach (var elem in Selected.ToArray())
								elem.BringToFront();
					};
				}
				#endregion

				#region Send to Back
				if (Selected.Count != 0)
				{
					var opt = cmenu.Items.Add2(new ToolStripMenuItem("Send to Back") { Name = CMenuItems.SendToBack });
					opt.Enabled = Selected.Count != 0;
					opt.Click += (s, a) =>
					{
						using (SuspendDiagramChanged(raise_on_resume: true))
							foreach (var elem in Selected.ToArray())
								elem.SendToBack();
					};
				}
				#endregion

				#region Selection
				if (AllowSelection && Selected.Count != 0)
				{
					cmenu.Items.AddSeparator();
					var opt = cmenu.Items.Add2(new ToolStripMenuItem("Select Connected Nodes") { Name = CMenuItems.SelectConnected });
					opt.Enabled = Selected.Any(x => x.Entity == Entity.Node);
					opt.Click += (s, a) => SelectConnectedNodes();
				}
				#endregion

				#region Properties
				if (AllowEditing)
				{
					cmenu.Items.AddSeparator();
					var opt = Selected.Count != 0
						? cmenu.Items.Add2(new ToolStripMenuItem("Properties") { Name = CMenuItems.Properties })
						: cmenu.Items.Add2(new ToolStripMenuItem("Default Properties") { Name = CMenuItems.DefaultProps });
					opt.Click += (s,a) => EditProperties();
				}
				#endregion

				#region Use Default Properties
				if (AllowEditing && Selected.Count != 0)
				{
					var opt = cmenu.Items.Add2(new ToolStripMenuItem("Use Default Properties") { Name = CMenuItems.UseDefaultProps });
					opt.Click += (s,a) =>
					{
						foreach (var elem in Selected.ToArray())
						{
							if (elem.Entity == Entity.Node)
								((Node)elem).Style = m_node_styles[Guid.Empty];
							if (elem.Entity == Entity.Connector)
								((Connector)elem).Style = m_connector_styles[Guid.Empty];
						}
						Invalidate();
					};
				}
				#endregion
			}

			// Allow users to add/remove menu options
			OnAddUserMenuOptions(new AddUserMenuOptionsEventArgs(cmenu));

			cmenu.Items.TidySeparators();
			cmenu.Closed += (s,a) => Invalidate();
			cmenu.Show(this, location);
		}

		/// <summary>Names for items in the context menu</summary>
		public static class CMenuItems
		{
			public const string Scatter         = "cmenu_scatter";
			public const string ReLink          = "cmenu_relink";
			public const string BringToFront    = "cmenu_bring_to_front";
			public const string SendToBack      = "cmenu_send_to_back";
			public const string SelectConnected = "cmenu_select_connected";
			public const string Properties      = "cmenu_props";
			public const string DefaultProps    = "cmenu_default_props";
			public const string UseDefaultProps = "cmenu_use_default_props";
		}

		/// <summary>Gets the tool strip containing edit controls for the diagram</summary>
		public ToolStrip EditToolstrip
		{
			get { return m_toolstrip_edit; }
		}

		/// <summary>Key names for the edit tools</summary>
		public static class EditTools
		{
			public static class Node
			{
				public const string Key     = "edittools_node";
				public const string BoxNode = "edittools_node_boxnode";
			}
			public static class Conn
			{
				public const string Key     = "edittools_conn";
				public const string Line    = "edittools_conn_line";
				public const string Forward = "edittools_conn_fwd";
				public const string Back    = "edittools_conn_back";
				public const string Bidir   = "edittools_conn_bidi";
			}
		}

		/// <summary>Set up a tool strip with tools for editing the diagram</summary>
		private void SetupEditToolstrip()
		{
			using (m_toolstrip_edit.SuspendLayout(true))
			{
				#region Node Split Button
				{
					var btn = m_toolstrip_edit.Items.Add2(new ToolStripSplitButtonCheckable
					{
						CheckOnClicked      = true,
						DisplayStyle        = ToolStripItemDisplayStyle.Image,
						Image               = Resources.boxes,
						Name                = EditTools.Node.Key,
						AutoSize            = true,
						ImageScaling        = ToolStripItemImageScaling.SizeToFit,
						Text                = "Node",
						ToolTipText         = "Add Node",
					});
					btn.ButtonClick += (s,a) =>
					{
						switch (btn.DefaultItem.Name)
						{
						case EditTools.Node.BoxNode:
							m_mouse_op.SetPending(1, btn.Checked ? new MouseOpCreateNode(this) : null);
							break;
						}
					};
					#region Box Node
					{
						btn.DropDownItems.Add2(new ToolStripMenuItem
						{
							CheckOnClick = true,
							Image        = global::pr.Resources.boxes,
							Name         = EditTools.Node.BoxNode,
							Size         = new Size(78, 22),
							AutoSize     = true,
							Text         = "Box Node",
						});
					}
					#endregion
					btn.DefaultItem = btn.DropDownItems[0];
				}
				#endregion
				#region Connector Split Button
				{
					var btn = m_toolstrip_edit.Items.Add2(new ToolStripSplitButtonCheckable
					{
						CheckOnClicked      = true,
						DisplayStyle        = ToolStripItemDisplayStyle.Image,
						Image               = pr.Resources.connector1,
						Name                = EditTools.Conn.Key,
						AutoSize            = true,
						ImageScaling        = ToolStripItemImageScaling.SizeToFit,
						Text                = "Connector",
						ToolTipText         = "Add Connector",
					});
					btn.ButtonClick += (s,a) =>
					{
						switch (btn.DefaultItem.Name)
						{
						case EditTools.Conn.Line:    m_mouse_op.SetPending(1, btn.Checked ? new MouseOpCreateLink(this, Connector.EType.Line   ) : null); break;
						case EditTools.Conn.Forward: m_mouse_op.SetPending(1, btn.Checked ? new MouseOpCreateLink(this, Connector.EType.Forward) : null); break;
						case EditTools.Conn.Back:    m_mouse_op.SetPending(1, btn.Checked ? new MouseOpCreateLink(this, Connector.EType.Back   ) : null); break;
						case EditTools.Conn.Bidir:   m_mouse_op.SetPending(1, btn.Checked ? new MouseOpCreateLink(this, Connector.EType.BiDir  ) : null); break;
						}
					};
					#region Line Connector
					{
						btn.DropDownItems.Add2(new ToolStripMenuItem
						{
							CheckOnClick = true,
							Image        = global::pr.Resources.connector1,
							Name         = EditTools.Conn.Line,
							Size         = new Size(108, 22),
							AutoSize     = true,
							Text         = "Line Connector",
						});
					}
					#endregion
					#region Line Forward Connector
					{
						btn.DropDownItems.Add2(new ToolStripMenuItem
						{
							CheckOnClick = true,
							Image        = global::pr.Resources.connector2,
							Name         = EditTools.Conn.Forward,
							Size         = new Size(129, 22),
							AutoSize     = true,
							Text         = "Forward Connector",
						});
					}
					#endregion
					#region Line Backward Connector
					{
						btn.DropDownItems.Add2(new ToolStripMenuItem
						{
							CheckOnClick = true,
							Image        = global::pr.Resources.connector3,
							Name         = EditTools.Conn.Back,
							Size         = new Size(137, 22),
							AutoSize     = true,
							Text         = "Backward Connector",
						});
					}
					#endregion
					#region Line Bi-Directional Connector
					{
						btn.DropDownItems.Add2(new ToolStripMenuItem
						{
							CheckOnClick = true,
							Image        = global::pr.Resources.connector4,
							Name         = EditTools.Conn.Bidir,
							Size         = new Size(152, 22),
							AutoSize            = true,
							Text         = "Bidir Connector",
						});
					}
					#endregion
					btn.DefaultItem = btn.DropDownItems[0];
				}
				#endregion
			}

			UpdateEditToolbar();
		}

		/// <summary>Update the state of the edit tool bar as selections change</summary>
		private void UpdateEditToolbar()
		{
			if (this.IsInDesignMode())
				return;

			// Note, visibility of items in the edit tool-bar should not be changed
			// as callers may set the visibility to suit their needs

			var btn_node = (ToolStripSplitButtonCheckable)m_toolstrip_edit.Items[EditTools.Node.Key];
			btn_node.Checked = m_mouse_op.Pending(1) is MouseOpCreateNode;
			btn_node.Enabled = AllowEditing;

			var btn_conn = (ToolStripSplitButtonCheckable)m_toolstrip_edit.Items[EditTools.Conn.Key];
			btn_conn.Checked = m_mouse_op.Pending(1) is MouseOpCreateLink;
			btn_conn.Enabled = AllowEditing;
		}

		/// <summary>Display a dialog for editing properties of selected elements</summary>
		private void EditProperties()
		{
			// Ensure all selected elements have a unique style object
			foreach (var elem in Selected)
			{
				var node = elem as Node;
				if (node != null && node.Style.Id == Guid.Empty)
					node.Style = new NodeStyle(node.Style);

				var conn = elem as Connector;
				if (conn != null && conn.Style.Id == Guid.Empty)
					conn.Style = new ConnectorStyle(conn.Style);
			}

			var form = new Form
			{
				StartPosition = FormStartPosition.CenterParent,
				ShowInTaskbar = false,
				ShowIcon = false,
			};
			var props = new PropertyGrid
			{
				Dock = DockStyle.Fill,
				PropertySort = PropertySort.NoSort,
			};

			if (Selected.Count != 0)
			{
				form.Text = "Properties";
				props.SelectedObjects = Selected
					.OfType<IHasStyle>()
					.Select(x => x.Style)
					.ToArray();
			}
			else
			{
				form.Text = "Default Properties";
	
				var fields = new
					{
						MainOptions = Options,
						NodeStyle = m_node_styles[Guid.Empty],
						ConnectorStyle = m_connector_styles[Guid.Empty],
					};

				props.SelectedObject = fields;
			}

			form.Controls.Add(props);
			form.FormClosed += (s,a) =>
			{
				OptionsChanged.Raise(this, EventArgs.Empty);
				Invalidate();
			};
			form.Show(this);
		}

		/// <summary>Check the self consistency of elements</summary>
		public bool CheckConsistency()
		{
			if (ConsistencyCheckSuspended) return true;
			try { CheckConsistencyInternal(); }
			catch (Exception ex)
			{
				Debug.WriteLine(ex.MessageFull());
				return false;
			}
			return true;
		}

		/// <summary>Check consistency of the diagram elements</summary>
		protected virtual void CheckConsistencyInternal()
		{
			// Take a copy to prevent events being raised
			var elements = Elements.ToArray();

			// The elements collection should be distinct
			elements.Sort(ByGuid);
			for (int i = 0; i < elements.Length - 1; ++i)
			{
				if (!Equals(elements[i].Id, elements[i+1].Id)) continue;
				throw new Exception("Element {0} is in the Elements collection more than once".Fmt(elements[i].ToString()));
			}

			// All elements in the diagram should have their Diagram property set to this diagram
			foreach (var elem in Elements)
			{
				if (elem.Diagram == this) continue;
				throw new Exception("Element {0} is in the Elements collection but does not have its Diagram property set correctly".Fmt(elem.ToString()));
			}

			// All "dirty" elements should belong to this diagram
			foreach (var elem in m_dirty)
			{
				if (elem.Diagram == this) continue;
				throw new Exception("Element {0} is in the dirty list but is not associated with this diagram".Fmt(elem.ToString()));
			}

			// The selected collection contains all that is selected and no more
			var selected0 = elements.Where(x => x.Selected).ToArray().Sort(ByGuid);
			var selected1 = Selected.ToArray().Sort(ByGuid);
			if (!selected0.SequenceEqual(selected1, ByGuid))
				throw new Exception("Selected elements collection is inconsistent with the selected state of the elements");

			// The 'ElemIds' should match the Elements collection
			if (ElemIds.Count != Elements.Count)
				throw new Exception("Elements collection and Element Id lookup table don't match");
			foreach (var elem in Elements)
				if (ElemIds.GetOrDefault(elem.Id) != elem)
					throw new Exception("Element {0} is not in the Element Id lookup table".Fmt(elem.ToString()));

			// Check the consistency of all elements
			foreach (var elem in Elements)
				elem.CheckConsistency();
		}

		/// <summary>True while consistency checks are suspended (Set calls are reference counted)</summary>
		public bool ConsistencyCheckSuspended
		{
			get { return m_consistency_check_ref_count != 0; }
			set
			{
				m_consistency_check_ref_count += value ? +1 : -1;
				Debug.Assert(m_consistency_check_ref_count >= 0);
			}
		}
		private int m_consistency_check_ref_count;

		/// <summary>Temporarily disable the consistency check</summary>
		public Scope SuspendConsistencyCheck(bool check_on_exit = true)
		{
			return Scope.Create(
				() => ConsistencyCheckSuspended = true,
				() =>
				{
					ConsistencyCheckSuspended = false;
					if (check_on_exit && !ConsistencyCheckSuspended)
						Debug.Assert(CheckConsistency());
				});
		}

		/// <summary>Element sorting predicates</summary>
		protected Cmp<Element> ByGuid   = Cmp<Element>.From((l,r) => l.Id.CompareTo(r.Id));

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
			this.PerformLayout();
		}

		public void BeginInit(){}
		public void EndInit(){}

		#endregion
	}
}
