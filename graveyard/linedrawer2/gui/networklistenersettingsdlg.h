#ifndef NETWORKLISTENERSETTINGSDLG_H
#define NETWORKLISTENERSETTINGSDLG_H

#include "LineDrawer/Resource.h"

class NetworkListenerSettingsDlg : public CDialog
{
public:
	NetworkListenerSettingsDlg(CWnd* pParent);

	// Dialog Data
	//{{AFX_DATA(UDPListenerSettingsDlg)
	enum { IDD = IDD_NETWORKSETTINGS_DIALOG };
	BYTE	m_ip1;
	BYTE	m_ip2;
	BYTE	m_ip3;
	BYTE	m_ip4;
	UINT	m_port;
	DWORD	m_buffer_size;
	DWORD	m_sample_period;
	//}}AFX_DATA


	// Overrides
	// ClassWizard generated virtual function overrides
	//{{AFX_VIRTUAL(NetworkListenerSettingsDlg)
	protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	//}}AFX_VIRTUAL

protected:

	// Generated message map functions
	//{{AFX_MSG(NetworkListenerSettingsDlg)
	//}}AFX_MSG
	DECLARE_MESSAGE_MAP()
};

#endif//NETWORKLISTENERSETTINGSDLG_H
