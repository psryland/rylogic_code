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
			ContentPanel        .Name = "Content";
			TopToolStripPanel   .Name = "Top";
			LeftToolStripPanel  .Name = "Left";
			RightToolStripPanel .Name = "Right";
			BottomToolStripPanel.Name = "Bottom";

			// ContentPanel is not a container control
			TopToolStripPanel   .AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
			LeftToolStripPanel  .AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
			RightToolStripPanel .AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
			BottomToolStripPanel.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
		}
	}
}
