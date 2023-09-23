using System;
using Rylogic.Gfx;
using Rylogic.Gui.WPF.ChartDiagram;

namespace UFADO.Gfx;

public class ParentLink :Connector
{
	public ParentLink()
	{
	}

	public ParentLink(Node? node0, Node? node1)
		: base(node0, node1, EType.Line, Guid.NewGuid(), DefaultStyle)
	{
	}

	private static readonly ConnectorStyle DefaultStyle = new()
	{
		Line = new Colour32(0xFF908050U),
		Width = 16,
	};
}
