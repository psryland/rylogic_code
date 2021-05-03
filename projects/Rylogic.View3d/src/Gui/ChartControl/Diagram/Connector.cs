using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;
using Rylogic.Extn;
using Rylogic.Maths;
using Rylogic.Utility;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Rylogic.Common;
using Rylogic.LDraw;

namespace Rylogic.Gui.WPF.ChartDiagram
{
	/// <summary>
	/// A base class for connectors between elements.
	/// Connectors have two ends (nodes), either of which is allowed to be null.
	/// AnchorPoints are never null however.</summary>
	[DebuggerDisplay("{Description,nq}")]
	public class Connector :ChartControl.Element, IHasStyle
	{
		public Connector()
			: this(null, null)
		{ }
		public Connector(Node? node0, Node? node1, EType? type = null, Guid? id = null, ConnectorStyle? style = null, string? text = null)
			: base(id ?? Guid.NewGuid(), text, m4x4.Translation(AttachCentre(node0, node1)))
		{
			m_anc0 = new AnchorPoint();
			m_anc1 = new AnchorPoint();
			m_centre_offset = v4.Zero;
			Type = type ?? EType.Line;
			Style = style ?? new ConnectorStyle();
			Node0 = node0;
			Node1 = node1;

			Init(false);
		}
		public Connector(XElement node)
			: base(node)
		{
			m_anc0 = new AnchorPoint();
			m_anc1 = new AnchorPoint();
			m_centre_offset = node.Element(XmlTag.CentreOffset).As(v4.Zero);
			Anc0 = node.Element(XmlTag.Anchor0).As<AnchorPoint>();
			Anc1 = node.Element(XmlTag.Anchor1).As<AnchorPoint>();
			Type = node.Element(XmlTag.Type).As<EType>();
			Style = new ConnectorStyle(node.Element(XmlTag.Style).As<Guid>());

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
			Style = null!;
			Util.Dispose(ref m_gfx_line!);
			Util.Dispose(ref m_gfx_fwd!);
			Util.Dispose(ref m_gfx_bak!);
			base.Dispose(disposing);
		}
		private static readonly v4 Bias = new v4(0, 0, 0.001f, 0);

		/// <summary>Graphics for the connector line</summary>
		private View3d.Object m_gfx_line = null!;

		/// <summary>Graphics for the forward arrow</summary>
		private View3d.Object m_gfx_fwd = null!;

		/// <summary>Graphics for the backward arrow</summary>
		private View3d.Object m_gfx_bak = null!;

		/// <summary>
		/// Controls how the connector is positioned relative to the
		/// mid point between Anc0.LocationDS and Anc1.LocationDS</summary>
		private v4 m_centre_offset;

		/// <summary>Export to XML</summary>
		public override XElement ToXml(XElement node)
		{
			base.ToXml(node);
			node.Add2(XmlTag.Anchor0, Anc0, false);
			node.Add2(XmlTag.Anchor1, Anc1, false);
			node.Add2(XmlTag.Type, Type, false);
			node.Add2(XmlTag.Style, Style.Id, false);
			return node;
		}
		protected override void FromXml(XElement node)
		{
			Style = new ConnectorStyle(node.Element(XmlTag.Style).As<Guid>(Style.Id));
			Anc0 = node.Element(XmlTag.Anchor0).As<AnchorPoint>(Anc0);
			Anc1 = node.Element(XmlTag.Anchor1).As<AnchorPoint>(Anc1);
			base.FromXml(node);
		}

		/// <summary>The 'from' anchor. Note: assigning to this anchor changes it's content, not the instance</summary>
		public AnchorPoint Anc0
		{
			get => m_anc0;
			set
			{
				Node0 = value.Node;
				m_anc0.Location = value.Location;
				m_anc0.Normal = value.Normal;
			}
		}
		private readonly AnchorPoint m_anc0;

		/// <summary>The 'to' anchor. Note: assigning to this anchor changes it's content, not the instance</summary>
		public AnchorPoint Anc1
		{
			get => m_anc1;
			set
			{
				Node1 = value.Node;
				m_anc1.Location = value.Location;
				m_anc1.Normal = value.Normal;
			}
		}
		private readonly AnchorPoint m_anc1;

		/// <summary>Get the anchor point associated with 'node'</summary>
		public AnchorPoint? Anc(Node node)
		{
			return
				Node0 == node ? Anc0 :
				Node1 == node ? Anc1 :
				null;
		}

		/// <summary>Get the anchor point not associated with 'node'</summary>
		public AnchorPoint? OtherAnc(Node node)
		{
			return
				Node0 == node ? Anc1 :
				Node1 == node ? Anc0 :
				null;
		}

		/// <summary>The 'from' node. Nodes can be null, implying the connector is dangling</summary>
		public Node? Node0
		{
			get => Anc0.Node;
			set
			{
				if (Node0 == value) return;
				if (Node0 != null)
				{
					Node0.SizeChanged -= Relink;
					Node0.PositionChanged -= Invalidate;
					Node0.Connectors.Remove(this);
				}
				Anc0.Node = value;
				if (Node0 != null)
				{
					Node0.Connectors.Add(this);
					Node0.PositionChanged += Invalidate;
					Node0.SizeChanged += Relink;
				}
			}
		}

		/// <summary>The 'to' node. Nodes can be null, implying the connector is connected to the diagram</summary>
		public Node? Node1
		{
			get => Anc1.Node;
			set
			{
				if (Node1 == value) return;
				if (Node1 != null)
				{
					Node1.SizeChanged -= Relink;
					Node1.PositionChanged -= Invalidate;
					Node1.Connectors.Remove(this);
				}
				Anc1.Node = value;
				if (Node1 != null)
				{
					Node1.Connectors.Add(this);
					Node1.PositionChanged += Invalidate;
					Node1.SizeChanged += Relink;
				}
			}
		}

		/// <summary>The non-null 'from' then 'to' nodes, as a sequence</summary>
		public IEnumerable<Node> Nodes
		{
			get
			{
				if (Node0 != null) yield return Node0;
				if (Node1 != null) yield return Node1;
			}
		}

		/// <summary>Returns the other connected node</summary>
		public Node? OtherNode(Node node)
		{
			if (Node0 == node) return Node1;
			if (Node1 == node) return Node0;
			throw new Exception($"{Description} is not connected to {node}");
		}

		/// <summary>Get/Set the 'to' or 'from' end of the connector</summary>
		internal Node? Node(bool to)
		{
			return to ? Node1 : Node0;
		}
		internal void Node(bool to, Node? node)
		{
			if (to)
				Node1 = node;
			else
				Node0 = node;
		}

		/// <summary>True if the connector is not attached at both ends</summary>
		public bool Dangling => Node0 == null || Node1 == null;

		/// <summary>True if at least one end of the connector is attached to a node</summary>
		public bool Attached => Node0 != null || Node1 != null;

		/// <summary>True if this connector is attached at both ends to the same node</summary>
		public bool Loop => Node0 != null && Node0 == Node1;

		/// <summary>Detach from the nodes at each end of the connector</summary>
		public void DetachNodes()
		{
			Node0 = null;
			Node1 = null;
		}

		/// <summary>The connector type</summary>
		public EType Type
		{
			get => m_connector_type;
			set
			{
				if (m_connector_type == value) return;
				m_connector_type = value;
				Invalidate();
			}
		}
		private EType m_connector_type;

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
			get => m_style;
			set
			{
				if (Style == value) return;
				if (m_style != null)
				{
					m_style.PropertyChanged -= HandleStyleChanged;
				}
				m_style = value ?? new ConnectorStyle();
				if (m_style != null)
				{
					m_style.PropertyChanged += HandleStyleChanged;
				}
				Invalidate();

				// Handlers
				void HandleStyleChanged(object sender, PropertyChangedEventArgs e)
				{
					Invalidate();
				}
			}
		}
		IStyle IHasStyle.Style => Style;
		private ConnectorStyle m_style = new ConnectorStyle();

		/// <inheritdoc/>
		protected override void SetPosition(m4x4 pos)
		{
			base.SetPosition(pos);
			UpdatePositions();
		}

		/// <inheritdoc/>
		public override BBox Bounds
		{
			get
			{
				var bounds = BBox.Reset;
				foreach (var pt in Points(true))
					BBox.Grow(ref bounds, pt);
				return bounds;
			}
		}

		/// <inheritdoc/>
		public override ChartControl.HitTestResult.Hit? HitTest(Point chart_point, Point client_point, ModifierKeys modifier_keys, EMouseBtns mouse_btns, View3d.Camera cam)
		{
		//	var points = Points(true);
		//	var dist_sq = float.MaxValue;
		//	var closest_pt = v2.Zero;
		//	if (Style.Smooth && points.Length > 2)
		//	{
		//		// Smooth connectors convert the points to splines
		//		foreach (var spline in Spline.CreateSplines(points.Select(x => new v4(x, 0, 1))))
		//		{
		//			// Find the closest point to the spline
		//			var t = Geometry.ClosestPoint(spline, new v4(point, 0, 1));
		//			var pt = spline.Position(t);
		//			var dsq = (point - pt.xy).LengthSq;
		//			if (dsq < dist_sq)
		//			{
		//				dist_sq = dsq;
		//				closest_pt = pt.xy;
		//			}
		//		}
		//	}
		//	else
		//	{
		//		for (int i = 0; i < points.Length - 1; ++i)
		//		{
		//			var t = Geometry.ClosestPoint(points[i], points[i + 1], point);
		//			var pt = Math_.Lerp(points[i], points[i + 1], t);
		//			var dsq = (point - pt).LengthSq;
		//			if (dsq < dist_sq)
		//			{
		//				dist_sq = dsq;
		//				closest_pt = pt;
		//			}
		//		}
		//	}
		//
		//	// Convert separating distance screen space
		//	var dist_cs = cam.WSVecToSSVec(closest_pt, point);
		//	if (dist_cs.LengthSq > MinCSSelectionDistanceSq) return null;
		//	return new HitTestResult.Hit(this, closest_pt - Position.pos.xy);
			return null; // todo
		}

		/// <summary>Handle a click event on this element</summary>
		protected override void HandleClicked(ChartControl.ChartClickedEventArgs args)
		{
		//	// Only respond if selected and editing is allowed
		//	if (Selected && Diagram.AllowChanges)
		//	{
		//		// Hit point in screen space
		//		var hit_point_ds = Position.pos + new v4(hit.Point, 0, 0);
		//		var hit_point_cs = v2.From(cam.WSPointToSSPoint(hit_point_ds));
		//		var anc0_cs = v2.From(cam.WSPointToSSPoint(Anc0.LocationDS));
		//		var anc1_cs = v2.From(cam.WSPointToSSPoint(Anc1.LocationDS));
		//
		//		// If the click was at the ends of the connector and diagram editing
		//		// is allowed, detach the connector and start a move link mouse op
		//		if ((hit_point_cs - anc0_cs).LengthSq < MinCSSelectionDistanceSq)
		//		{
		//			Diagram.m_mouse_op.SetPending(1, new MouseOpMoveLink(Diagram, this, false) { StartOnMouseDown = false });
		//			return true;
		//		}
		//		if ((hit_point_cs - anc1_cs).LengthSq < MinCSSelectionDistanceSq)
		//		{
		//			Diagram.m_mouse_op.SetPending(1, new MouseOpMoveLink(Diagram, this, true) { StartOnMouseDown = false });
		//			return true;
		//		}
		//	}
		//	return false;
		}

		/// <summary>Drag the element 'delta' from the DragStartPosition</summary>
		protected override void HandleDragged(ChartControl.ChartDraggedEventArgs args)
		{
			Invalidate();
		}

		/// <summary>Return all the locations that connectors can attach to on this element</summary>
		public virtual IEnumerable<AnchorPoint> AnchorPoints()
		{
		//	var pts = Points(false);
		//	v4 ctr;
		//	if (pts.Length == 4) ctr = new v4((pts[1] + pts[2]) / 2f, PositionZ, 1);
		//	else if (pts.Length == 3 && !Style.Smooth) ctr = new v4(pts[1], PositionZ, 1);
		//	else if (pts.Length == 3) ctr = Spline.CreateSplines(pts.Select(x => new v4(x, PositionZ, 1))).First().Position(0.5f);
		//	else throw new Exception("unexpected number of connector points");
		//	yield return new AnchorPoint(this, ctr, v4.Zero);
		yield break;
		}

		/// <summary>Set the position of the graphics models based on the current connector position</summary>
		private void UpdatePositions()
		{
			// Raise selected objects above others
			var bias = (Selected ? new v4(0, 0, 1, 0) : v4.Zero) + Bias;

			// Set the line transform
			if (m_gfx_line != null)
				m_gfx_line.O2P = new m4x4(Position.rot, Position.pos + bias);

			// If the connector has a back arrow, add the arrow head graphics
			if (m_gfx_bak != null && Type.HasFlag(EType.Back))
			{
				var dir = -Anc0.NormalDS;
				if (dir == v4.Zero) dir = Anc1.NormalDS;
				if (dir == v4.Zero) dir = Math_.Normalise(Anc0.LocationDS - Anc1.LocationDS, v4.YAxis);
				var pos = new v4(Anc0.LocationDS.xy, Math.Max(Anc0.LocationDS.z, PositionZ), 1f);
				m_gfx_bak.O2P = m4x4.Transform(v4.ZAxis, (float)Math.Atan2(dir.y, dir.x), pos + bias) * m4x4.Scale((float)Style.Width, v4.Origin);
			}

			// If the connector has a forward arrow, add the arrow head graphics
			if (m_gfx_fwd != null && Type.HasFlag(EType.Forward))
			{
				var dir = -Anc1.NormalDS;
				if (dir == v4.Zero) dir = Anc0.NormalDS;
				if (dir == v4.Zero) dir = Math_.Normalise(Anc1.LocationDS - Anc0.LocationDS, v4.YAxis);
				var pos = new v4(Anc1.LocationDS.xy, Math.Max(Anc1.LocationDS.z, PositionZ), 1f);
				m_gfx_fwd.O2P = m4x4.Transform(v4.ZAxis, (float)Math.Atan2(dir.y, dir.x), pos + bias) * m4x4.Scale((float)Style.Width, v4.Origin);
			}
		}

		/// <inheritdoc/>
		protected override void UpdateGfxCore()
		{
			// Update the transform
			Position = m4x4.Translation(AttachCentre(Anc0, Anc1));

			var col = Selected ? Style.Selected : Hovered ? Style.Hovered : Dangling ? Style.Dangling : Style.Line;
			var width = Style.Width;
			var pts = Points(false);

			// Update the connector line graphics
			var ldr = new LdrBuilder();
			ldr.Ribbon("connector", col, pts, EAxisId.PosZ, (float)width, Style.Smooth);
			m_gfx_line.UpdateModel(ldr);

			// If the connector has a back arrow, add the arrow head graphics
			if (Type.HasFlag(EType.Back))
				m_gfx_bak.ColourSet(col);
		
			// If the connector has a forward arrow, add the arrow head graphics
			if (Type.HasFlag(EType.Forward))
				m_gfx_fwd.ColourSet(col);

			UpdatePositions();
		}

		/// <inheritdoc/>
		protected override void UpdateSceneCore()
		{
			base.UpdateSceneCore();
			if (Chart == null)
				return;

			// Add the main connector line
			if (Visible)
				Chart.Scene.Window.AddObject(m_gfx_line);
			else
				Chart.Scene.Window.RemoveObject(m_gfx_line);

			// If the connector has a back arrow, add the arrow head graphics
			if (Visible && Type.HasFlag(EType.Back))
				Chart.Scene.Window.AddObject(m_gfx_bak);
			else
				Chart.Scene.Window.RemoveObject(m_gfx_bak);

			// If the connector has a forward arrow, add the arrow head graphics
			if (Visible && Type.HasFlag(EType.Forward))
				Chart.Scene.Window.AddObject(m_gfx_fwd);
			else
				Chart.Scene.Window.RemoveObject(m_gfx_fwd);
		}

		/// <summary>Detach from 'node'</summary>
		public void Remove(Node node)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (Node0 == node) { Node0 = null; Invalidate(); return; }
			if (Node1 == node) { Node1 = null; Invalidate(); return; }
			throw new Exception($"Connector '{ToString()}' is not connected to node {node}");
		}

		/// <summary>Update the node anchors</summary>
		public void Relink(bool find_previous_anchors)
		{
			if (find_previous_anchors)
			{
				m_anc0.Update();
				m_anc1.Update();
			}
			else
			{
				// Find the preferred 'anc0' position closest to the centre of element1,
				// then update the position of 'anc1' given 'anc0's position,
				// finally update 'anc0' again since 'anc1' has possibly changed.
				if (m_anc1.Node != null)
					m_anc0.Update(m_anc1.Node.Position.pos, false);

				m_anc1.Update(m_anc0.LocationDS, false);
				m_anc0.Update(m_anc1.LocationDS, false);
			}
			Invalidate();
		}
		public void Relink(object? sender = null, EventArgs? args = null)
		{
		//	var find_previous_anchors = Diagram == null || !Diagram.Options.Node.AutoRelink;
		//	Relink(find_previous_anchors);
		}

		/// <summary>Returns the diagram space position of the centre between nearest anchor points on two nodes</summary>
		private static v4 AttachCentre(AnchorPoint anc0, AnchorPoint anc1)
		{
			var centre = 0.5f * (anc0.LocationDS + anc1.LocationDS);
			return centre;
		}
		private static v4 AttachCentre(Node? node0, Node? node1)
		{
			AnchorPoint anc0, anc1;
			AttachPoints(node0, node1, out anc0, out anc1);
			return AttachCentre(anc0, anc1);
		}

		/// <summary>Returns the nearest anchor points on two nodes</summary>
		private static void AttachPoints(Node? node0, Node? node1, out AnchorPoint anc0, out AnchorPoint anc1)
		{
			var pt0 = node1?.Centre ?? node0?.Centre ?? v4.Origin;
			var pt1 = node0?.Centre ?? node1?.Centre ?? v4.Origin;
			anc0 = node0?.NearestAnchor(pt0, false) ?? new AnchorPoint(null, pt0, v4.Zero);
			anc1 = node1?.NearestAnchor(pt1, false) ?? new AnchorPoint(null, pt1, v4.Zero);
		}

		/// <summary>Returns the corner points of the connector from Anc0 to Anc1</summary>
		private v4[] Points(bool diagram_space)
		{
			// A connector is a line from Anc0 to Anc1, via CentreOffset.
			var origin = diagram_space ? v4.Zero : Position.pos.w0;
			var centre = Position.pos - origin + m_centre_offset;
			var beg = Anc0.LocationDS - origin;
			var end = Anc1.LocationDS - origin;

			// Choose directions for the end points
			var dir0 = Anc0.NormalDS;
			var dir1 = Anc1.NormalDS;
			if (dir0 == v4.Zero) dir0 = -dir1;
			if (dir1 == v4.Zero) dir1 = -dir0;
			if (dir0 == v4.Zero) dir0 = Math_.Normalise(end - beg, v4.Zero);
			if (dir1 == v4.Zero) dir1 = Math_.Normalise(beg - end, v4.Zero);

			// If the directions 



			// A connector is constructed from: Anc0.LocationDS, CentreOffset, Anc1.LocationDS.
			// A line from Anc0.LocationDS is extended in the direction of Anc0.NormalDS till it
			// is perpendicular to m_centre_offset. The same is done from Anc1.LocationDS
			// A connecting line between these points is then formed.


			//v2 intersect;
			//if (Geometry.Intersect(
			//	start, start + Anc0.NormalDS.xy,
			//	end  , end   + Anc1.NormalDS.xy, out intersect))
			//{
			//	if (v2.Dot2(intersect - start, Anc0.NormalDS.xy) > 0 &&
			//		v2.Dot2(intersect - end  , Anc1.NormalDS.xy) > 0)
			//		return new[]{start, intersect, end};
			//}

			var blen = (double)Math_.Dot(centre - beg, dir0);
			var elen = (double)Math_.Dot(centre - end, dir1);
			blen = Style.Smooth ? Style.MinLength : Math.Max(blen, Style.MinLength);
			elen = Style.Smooth ? Style.MinLength : Math.Max(elen, Style.MinLength);
		
			return new[] { beg, beg + blen * dir0, end + elen * dir1, end };
		}

		/// <summary>Check the self consistency of this element</summary>
		public override bool CheckConsistency()
		{
		//	// Connections to the same node are allowed
		//	// Null nodes are allowed.
		//
		//	// The connected nodes should contain a reference to this connector
		//	if (Node0 != null && Node0.Connectors.FirstOrDefault(x => x.Id == Id) == null)
		//		throw new Exception($"Connector {ToString()} is connected to node {Node0}, but that node does not contain a reference to this connector");
		//	if (Node1 != null && Node1.Connectors.FirstOrDefault(x => x.Id == Id) == null)
		//		throw new Exception($"Connector {ToString()} is connected to node {Node1}, but that node does not contain a reference to this connector");
		//
		//	// The style should be known to the diagram
		//	if (Diagram != null)
		//	{
		//		if (!Diagram.m_connector_styles.ContainsKey(Style.Id))
		//			throw new Exception($"Connector {ToString()} style is not in the diagram's style cache");
		//	}
		//
		//	return base.CheckConsistency();
		return true;
		}

		/// <summary>Debugging description</summary>
		public string Description=> $"Connector[{Anc0} -> {Anc1}]";

		/// <summary>Connector type</summary>
		[Flags]
		public enum EType
		{
			Line = 1 << 0,
			Forward = 1 << 1,
			Back = 1 << 2,
			BiDir = Forward | Back,
		}
	}

}
