//**********************************************************************
//
//	A tool bar for the physics lab plugin
//
//**********************************************************************
#ifndef PHYSICS_LAB_TOOL_BAR_H
#define PHYSICS_LAB_TOOL_BAR_H

#include "PhysicsLab_LDPI/Resource.h"

class CPhysicsLabToolBar : public CDialog
{
	DECLARE_DYNAMIC(CPhysicsLabToolBar)
public:
	CPhysicsLabToolBar(CWnd* pParent = NULL);
	virtual ~CPhysicsLabToolBar();

	afx_msg void OnBnClickedButtonOpen();
	afx_msg void OnBnClickedButtonReset();
	afx_msg void OnBnClickedButtonGo();
	afx_msg void OnBnClickedButtonStep();
	afx_msg void OnBnClickedButtonPause();
	afx_msg void OnBnClickedButtonZoomall();
	afx_msg void OnBnClickedCheckShowvelocity();
	afx_msg void OnBnClickedCheckShowangvel();
	afx_msg void OnBnClickedCheckShowangmom();

	// Dialog Data
	enum { IDD = IDD_DIALOG_ControlPanel };
	BOOL m_show_velocity;
	BOOL m_show_ang_velocity;
	BOOL m_show_ang_momentum;

protected:
	virtual void DoDataExchange(CDataExchange* pDX);
	DECLARE_MESSAGE_MAP()
};

#endif//PHYSICS_LAB_TOOL_BAR_H
