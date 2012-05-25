//*************************************************************************
//
// Error Output
//
//*************************************************************************

#include "Stdafx.h"
#include "LineDrawer/GUI/LineDrawerGUI.h"
#include "LineDrawer/Source/ErrorOutput.h"
#include "LineDrawer/Source/LineDrawer.h"

ErrorOutput::ErrorOutput()
:m_linedrawer(0)
{}

void ErrorOutput::ResetLogFile(const char* filename)
{
	FILE* file = fopen(filename, "w");
	if( !file ) MsgBox("Failed to open error log: %s", filename);
	fclose(file);
}

void ErrorOutput::Error(const char* msg)	{ Out("ERROR", msg); }
void ErrorOutput::Warn(const char* msg)		{ Out(" Warn", msg);  }
void ErrorOutput::Info(const char* msg)		{ Out(" Info", msg);  }
void ErrorOutput::Out(const char* type, const char* msg)
{
	std::string str = type;
	str				+= ": ";
	str				+= msg;
	str				+= "\n";

	if( !m_linedrawer ) m_linedrawer = &LineDrawer::Get();
	if( m_linedrawer->m_user_settings.m_error_output_to_file )
	{
        FILE* file = fopen(m_linedrawer->m_user_settings.m_error_output_log_filename.c_str(), "a");
		if( file ) { fprintf(file, "%s", str.c_str()); fclose(file); }
	}
	if( m_linedrawer->m_user_settings.m_error_output_msgbox )
	{
		MessageBox(m_linedrawer->m_window_handle, str.c_str(), "LineDrawer", MB_ICONEXCLAMATION|MB_OK);
	}
}

