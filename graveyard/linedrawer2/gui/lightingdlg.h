#ifndef LIGHTINGDLG_H
#define LIGHTINGDLG_H

#include "pr/renderer/renderer.h"

//*****
// LightingDlg dialog
class LightingDlg : public CDialog
{
	DECLARE_DYNAMIC(LightingDlg)
public:
	LightingDlg(CWnd* pParent);
	virtual ~LightingDlg();
	virtual BOOL OnInitDialog();

	afx_msg void OnBnClickedRadioAmbient()			{ EnableWhatsActive(); }
	afx_msg void OnBnClickedRadioPoint()			{ EnableWhatsActive(); }
	afx_msg void OnBnClickedRadioSpot()				{ EnableWhatsActive(); }
	afx_msg void OnBnClickedRadioDirectional()		{ EnableWhatsActive(); }
	afx_msg void OnBnClickedButtonApply();

	rdr::Light		m_light;
	BOOL			m_camera_relative;
	
	// Dialog Data
	enum { IDD = IDD_LIGHTING_DIALOG };
	DECLARE_MESSAGE_MAP()

protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	void	EnableWhatsActive();
};

#endif//LIGHTINGDLG_H
