module main;

import std.concurrency;
import dgui.all;
import main_gui;
import media_list;

int main(string[] args)
{
	// Create a media list
	//auto ml = new MediaList();
	//spawn(&ml.Run);
	
	return Application.run(new MainGUI());
}
