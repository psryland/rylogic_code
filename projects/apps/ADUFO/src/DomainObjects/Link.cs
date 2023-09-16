using System;
using System.Xml.Linq;
using Rylogic.Gfx;
using Rylogic.Gui.WPF.ChartDiagram;

namespace ADUFO.DomainObjects;

public class Link :Connector
{
	public Link()
	{
	}

	public Link(XElement node) : base(node)
	{
	}

	public Link(Node? node0, Node? node1, EType? type = null, Guid? id = null)
		: base(node0, node1, type, id, DefaultStyle)
	{
	}

	private static readonly ConnectorStyle DefaultStyle = new ConnectorStyle
	{
		Line = new Colour32(0xFF908050U),
		Width = 16,
	};
}
