module main_gui;

import dgui.all;

public class MainGUI :Form
{
	private Button m_btn_ok;
	
	public this()
	{
		this.text = "Slydeshow";
		this.size = Size(300, 250);
		this.startPosition = FormStartPosition.CENTER_SCREEN; // Set Form Position
		
		this.m_btn_ok = new Button();
		this.m_btn_ok.text = "Click Me!";
		this.m_btn_ok.dock = DockStyle.FILL;
		this.m_btn_ok.parent = this;
		this.m_btn_ok.click.attach(&this.onBtnOkClick); //Attach the click event with the selected procedure
	}
	
	private void onBtnOkClick(Control sender, EventArgs e)
	{
		// Display a message box
		MsgBox.show("OnClick", "Button.onClick()");
	}
}
