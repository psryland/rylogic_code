module main;

import dgui.all;
import prd.view3d.view3d;

class MainForm :Form
{
	private View3d m_view3d;

	public this()
	{
		this.text = "DApp";
		this.size = Size(300, 250);
		this.startPosition = FormStartPosition.CENTER_SCREEN; // Set Form Position

		m_view3d = new View3d();
		m_view3d.dock = DockStyle.FILL;
		m_view3d.parent = this;
	}
}

int main(string[] args)
{
	return Application.run(new MainForm());
}

