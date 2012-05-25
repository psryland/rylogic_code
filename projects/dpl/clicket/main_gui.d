module main_gui;

import dgui.all;

class MainForm :Form
{
	private Button m_btn_exit;
	
	public this()
	{
		this.text = "Clicket";
		this.size = Size(300, 250);
		this.startPosition = FormStartPosition.CENTER_SCREEN; // Set Form Position

		m_btn_exit = new Button();
		m_btn_exit.text = "Click Me!";
		m_btn_exit.dock = DockStyle.FILL; // Fill the whole form area
		m_btn_exit.parent = this;
		m_btn_exit.click.attach((s,a){ MsgBox.show("exiting", "Exit clicked"); }); //Attach the click event with the selected procedure
	}

//    private void onBtnOkClick(Control sender, EventArgs e)
//    {
//        // Display a message box
//        MsgBox.show("OnClick", "Button.onClick()");
//    }
}
