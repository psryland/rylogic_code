#ifndef VIEWPROPERTIESDLG_H
#define VIEWPROPERTIESDLG_H

#include <afxwin.h>
#include "LineDrawer/Source/NavigationManager.h"

class ViewPropertiesDlg : public CDialog
{
public:
	enum { IDD = IDD_VIEW_DIALOG };

	ViewPropertiesDlg(CWnd* pParent);

	m4x4	m_camera_to_world;
	v4		m_focus_point;
	float	m_near_clip_plane;
	float	m_far_clip_plane;
	int		m_cull_mode;

protected:
	void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
public:
	DECLARE_MESSAGE_MAP()
public:
	afx_msg void OnBnClickedButtonCopyCamXform();
};

#endif//VIEWPROPERTIESDLG_H
