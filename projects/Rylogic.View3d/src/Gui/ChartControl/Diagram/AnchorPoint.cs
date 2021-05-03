using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Rylogic.Extn;
using Rylogic.Gui.WPF;
using Rylogic.Maths;

namespace Rylogic.Gui.WPF.ChartDiagram
{
	/// <summary>A point that a connector can connect to</summary>
	public class AnchorPoint
	{
		// Notes:
		//  - An anchor point can be orphaned, allowing connectors to be dangling.

		public AnchorPoint()
			: this(null, v4.Origin, v4.YAxis)
		{ }
		public AnchorPoint(AnchorPoint rhs)
			: this(rhs.m_node, rhs.m_location, rhs.m_normal)
		{ }
		public AnchorPoint(Node? node, v4 loc, v4 norm)
		{
			m_node = node;
			m_location = loc;
			m_normal = norm;
		}
		public AnchorPoint(IDictionary<Guid, Node> nodes, XElement node)
			: this()
		{
			var id = node.Element(XmlTag.ElementId).As<Guid>();
			m_node = nodes.TryGetValue(id, out m_node) ? m_node : null;
			m_location = node.Element(XmlTag.Location).As(v4.Origin);
			Update();
		}

		/// <summary>Export to XML</summary>
		public XElement ToXml(XElement node)
		{
			node.Add2(XmlTag.ElementId, Node != null ? Node.Id : Guid.Empty, false);
			node.Add2(XmlTag.Location, Location, false);
			return node;
		}

		/// <summary>The node this anchor point is attached to. Null when the anchor point is orphaned</summary>
		public Node? Node
		{
			get => m_node;
			set
			{
				if (m_node == value) return;

				// When set to null, record the last diagram space position so that
				// anything connected to this anchor point doesn't snap to the origin.
				if (value == null)
				{
					Location = LocationDS;
					Normal = NormalDS;
				}

				m_node = value;
			}
		}
		private Node? m_node;

		/// <summary>Get/Set the node-relative position of the anchor (in node space)</summary>
		public v4 Location
		{
			get => m_location;
			set
			{
				if (m_location == value) return;
				m_location = value;
			}
		}
		private v4 m_location;

		/// <summary>Get/Set the node-relative anchor normal (can be zero)</summary>
		public v4 Normal
		{
			get => m_normal;
			set
			{
				if (m_normal == value) return;
				m_normal = value;
			}
		}
		private v4 m_normal;

		/// <summary>Get the diagram space position of the anchor</summary>
		public v4 LocationDS => Node?.Position * Location ?? Location;

		/// <summary>Get the diagram space anchor normal (can be zero)</summary>
		public v4 NormalDS => Node?.Position * Normal ?? Normal;

		/// <summary>Update this anchor point location after it has moved/resized</summary>
		public void Update(v4 pt, bool pt_in_node_space)
		{
			if (Node == null)
				return;

			var anc = Node.NearestAnchor(pt, pt_in_node_space);
			Location = anc.Location;
			Normal = anc.Normal;
		}

		/// <summary>Update this anchor point location after it has moved/resized</summary>
		public void Update()
		{
			if (Node == null) return;
			var anc = Node.NearestAnchor(Location, true);
			Location = anc.Location;
			Normal = anc.Normal;
		}

		/// <summary></summary>
		public string Description => $"Anc[{Node?.ToString() ?? "dangling"}]";

		/// <summary></summary>
		public bool Equals(AnchorPoint ap)
		{
			// Anchor points are equal if they're on the same object and at the same location
			return
				ap != null &&
				Node == ap.Node &&
				Location == ap.Location &&
				Normal == ap.Normal;
		}
		public override bool Equals(object? obj)
		{
			return obj is AnchorPoint ap && Equals(ap);
		}
		public override int GetHashCode()
		{
			return new { Node, Location, Normal }.GetHashCode();
		}
	}
}
