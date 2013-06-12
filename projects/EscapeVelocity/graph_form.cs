using System.Windows.Forms;
using pr.gui;
using pr.maths;

namespace EscapeVelocity
{
	/// <summary>Global functions</summary>
	public class GraphForm :Form
	{
		public readonly GraphControl Graph;
		public GraphForm()
		{
			Graph = new GraphControl();
			Graph.Dock = DockStyle.Fill;
			Controls.Add(Graph);
		}
	}
}