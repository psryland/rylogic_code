// NuggetView.h : main header file for the PROJECT_NAME application
//

#pragma once

#ifndef __AFXWIN_H__
	#error include 'stdafx.h' before including this file for PCH
#endif

#include "resource.h"		// main symbols

// NuggetViewApp:
// See NuggetView.cpp for the implementation of this class
class NuggetViewApp : public CWinApp
{
public:
	virtual BOOL InitInstance();
	DECLARE_MESSAGE_MAP()
};

extern NuggetViewApp theApp;