module user_data;

import std.xml;

// The user settings for the app
class UserData
{
	string m_filename;
	string m_window_title = ""; // 
	string m_control_type = ""; // 
	string m_button_text  = ""; // 
	int m_poll_freq       = 1;  // 
	int m_poll_freq_unit  = 1;  // 
}
