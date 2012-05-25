//***************************************************************************
//
//	Progress
//
//***************************************************************************

#ifndef PROGRESS_H
#define PROGRESS_H

#include <afxcmn.h>
#include "pr/maths/maths.h"

class Progress : public CDialog
{
	DECLARE_DYNAMIC(Progress)
public:
	enum { IDD = IDD_PROGRESS };
	Progress(CWnd* pParent = NULL);
	virtual ~Progress();

	bool SetProgress(uint number, uint maximum, const char* caption);
	afx_msg void OnClose();

protected:
	virtual void DoDataExchange(CDataExchange* pDX);
	DECLARE_MESSAGE_MAP()

private:
	CProgressCtrl	m_progress;
	bool			m_continue;
};

#endif//PROGRESS_H