using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pr.gui
{
	public class ToolStripContainer :System.Windows.Forms.ToolStripContainer
	{
		public ToolStripContainer()
		{
			ContentPanel.Name         = "Content";
			TopToolStripPanel.Name    = "Top";
			LeftToolStripPanel.Name   = "Left";
			RightToolStripPanel.Name  = "Right";
			BottomToolStripPanel.Name = "Bottom";
		}
	}
}
