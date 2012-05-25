//*****************************************************************************
//
// Network Listener Settings dialog
//
//*****************************************************************************
#include "Stdafx.h"
#include "LineDrawer/GUI/NetworkListenerSettingsDlg.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

BEGIN_MESSAGE_MAP(NetworkListenerSettingsDlg, CDialog)
	//{{AFX_MSG_MAP(NetworkListenerSettingsDlg)
	//}}AFX_MSG_MAP
END_MESSAGE_MAP()

//*****
// NetworkListenerSettingsDlg dialog constructor
NetworkListenerSettingsDlg::NetworkListenerSettingsDlg(CWnd* pParent)
:CDialog(NetworkListenerSettingsDlg::IDD, pParent)
{
	//{{AFX_DATA_INIT(NetworkListenerSettingsDlg)
	m_ip1 = 0;
	m_ip2 = 0;
	m_ip3 = 0;
	m_ip4 = 0;
	m_port = 0;
	m_buffer_size = 1024;
	m_sample_period = 1000;
	//}}AFX_DATA_INIT
}

void NetworkListenerSettingsDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	//{{AFX_DATA_MAP(UDPListenerSettingsDlg)
	DDX_Text(pDX, IDC_EDIT_IP1, m_ip1);
	DDV_MinMaxByte(pDX, m_ip1, 0, 255);
	DDX_Text(pDX, IDC_EDIT_IP2, m_ip2);
	DDV_MinMaxByte(pDX, m_ip2, 0, 255);
	DDX_Text(pDX, IDC_EDIT_IP3, m_ip3);
	DDV_MinMaxByte(pDX, m_ip3, 0, 255);
	DDX_Text(pDX, IDC_EDIT_IP4, m_ip4);
	DDV_MinMaxByte(pDX, m_ip4, 0, 255);
	DDX_Text(pDX, IDC_EDIT_PORT, m_port);
	DDV_MinMaxUInt(pDX, m_port, 0, 65535);
	DDX_Text(pDX, IDC_EDIT_RECV_BUFFER_SIZE, m_buffer_size);
	DDV_MinMaxDWord(pDX, m_buffer_size, 1, 1000000);
	DDX_Text(pDX, IDC_EDIT_SAMPLE_PERIOD, m_sample_period);
	DDV_MinMaxDWord(pDX, m_sample_period, 0, 1000000);
	//}}AFX_DATA_MAP
}
