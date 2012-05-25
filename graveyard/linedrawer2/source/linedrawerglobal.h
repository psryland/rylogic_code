//*******************************************************************************************
//
// The global app for Linedrawer 
//
//*******************************************************************************************
#ifndef LINEDRAWERGLOBAL_H
#define LINEDRAWERGLOBAL_H

class LineDrawerGlobal : public CWinApp
{
public:
	BOOL	InitInstance();
	DECLARE_MESSAGE_MAP()
};

extern LineDrawerGlobal LineDrawerApp;

#endif//GLINEDRAWERGLOBAL_H
