#ifndef LDR_OPTIONS_DLG_H
#define LDR_OPTIONS_DLG_H
#pragma once


// OptionsDlg dialog
class OptionsDlg : public CDialog
{
	DECLARE_DYNAMIC(OptionsDlg)
public:
	enum { IDD = IDD_OPTIONS };
	OptionsDlg(CWnd* pParent);
	virtual ~OptionsDlg();

	afx_msg void OnBnClickedCheckErrorLogtofile();
	afx_msg void OnBnClickedButtonErrorfileFind();
	afx_msg void OnBnClickedCheckEnableResourceMonitor();
	afx_msg void OnBnClickedButtonShaderPaths();

protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	virtual BOOL OnInitDialog();
	DECLARE_MESSAGE_MAP()

	void EnableVisibleItems();

public:
	CString	m_shader_version;
	int		m_geometry_quality;
	int		m_texture_quality;
	BOOL	m_ignore_missing_includes;
	BOOL	m_error_output_msgbox;
	BOOL	m_error_output_log;
	CString	m_error_log_filename;
	int		m_focus_point_size;
	BOOL	m_reset_camera_on_load;
	BOOL	m_enable_resource_monitor;
};

#endif//LDR_OPTIONS_DLG_H