//****************************************************
//
//	MsgBox - An inline function for displaying a message box
//
//****************************************************
#ifndef MSGBOX_H
#define MSGBOX_H

#include <stdio.h>

// Display a message box with variable arguments
inline void MsgBox(const char* title, const char* msg)
{
	#ifdef MessageBox
		MessageBox(NULL, msg, title, MB_ICONEXCLAMATION | MB_OK);
	#else//MessageBox
		printf("%s\n", title);
		printf("%s\n", msg);
	#endif//MessageBox
}

#endif//MSGBOX_H