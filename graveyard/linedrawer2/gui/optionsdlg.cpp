// OptionsDlg.cpp : implementation file
//

#include "stdafx.h"
#include "LineDrawer/Source/LineDrawer.h"
#include "LineDrawer/GUI/OptionsDlg.h"


// OptionsDlg dialog
IMPLEMENT_DYNAMIC(OptionsDlg, CDialog)
OptionsDlg::OptionsDlg(CWnd* pParent)
:CDialog(OptionsDlg::IDD, pParent)
,m_shader_version         (_T("v3.0"))
,m_geometry_quality       (0)
,m_texture_quality        (0)
,m_ignore_missing_includes(FALSE)
,m_error_output_msgbox    (FALSE)
,m_error_output_log       (FALSE)
,m_error_log_filename     (_T(""))
,m_focus_point_size       (50)
,m_reset_camera_on_load   (FALSE)
,m_enable_resource_monitor(FALSE)
{}

OptionsDlg::~OptionsDlg()
{}

void OptionsDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	DDX_CBString(pDX, IDC_COMBO_SHADER_VERSION           , m_shader_version         );
	DDX_CBIndex (pDX, IDC_COMBO_GEOMETRY_QUALITY         , m_geometry_quality       );
	DDX_CBIndex (pDX, IDC_COMBO_TEXTURE_QUALITY          , m_texture_quality        );
	DDX_Check   (pDX, IDC_CHECK_IGNORE_MISSING_INCLUDES  , m_ignore_missing_includes);
	DDX_Check   (pDX, IDC_CHECK_ERROR_MSGBOX             , m_error_output_msgbox    );
	DDX_Check   (pDX, IDC_CHECK_ERROR_LOGTOFILE          , m_error_output_log       );
	DDX_Text    (pDX, IDC_EDIT_ERRORLOG_FILENAME         , m_error_log_filename     );
	DDX_Slider  (pDX, IDC_SLIDER_FOCUS_POINT_SIZE        , m_focus_point_size       );
	DDX_Check   (pDX, IDC_CHECK_RESET_CAMERA_ON_LOAD     , m_reset_camera_on_load   );
	DDX_Check   (pDX, IDC_CHECK_ENABLE_RESOURCE_MONITOR  , m_enable_resource_monitor);
}

BOOL OptionsDlg::OnInitDialog()
{
	if( CDialog::OnInitDialog() )
	{
        EnableVisibleItems();
		return true;
	}
	return false;
}

BEGIN_MESSAGE_MAP(OptionsDlg, CDialog)
	ON_BN_CLICKED(IDC_CHECK_ERROR_LOGTOFILE          , OnBnClickedCheckErrorLogtofile)
	ON_BN_CLICKED(IDC_BUTTON_ERRORFILE_FIND          , OnBnClickedButtonErrorfileFind)
	ON_BN_CLICKED(IDC_BUTTON_SHADER_PATHS            , OnBnClickedButtonShaderPaths)
	ON_BN_CLICKED(IDC_CHECK_ENABLE_RESOURCE_MONITOR  , OnBnClickedCheckEnableResourceMonitor)
END_MESSAGE_MAP()


// OptionsDlg message handlers
void OptionsDlg::OnBnClickedCheckErrorLogtofile()
{
	UpdateData(TRUE);
	EnableVisibleItems();
}

void OptionsDlg::OnBnClickedButtonErrorfileFind()
{
	CFileDialog filedlg(TRUE);
	INT_PTR result = filedlg.DoModal();
	if( result != IDOK ) return;
	m_error_log_filename = filedlg.GetPathName();

	UpdateData(FALSE);
}

void OptionsDlg::OnBnClickedCheckEnableResourceMonitor()
{
	UpdateData(TRUE);
	EnableVisibleItems();
}

void OptionsDlg::OnBnClickedButtonShaderPaths()
{
	// TODO: Add your control notification handler code here
}

void OptionsDlg::EnableVisibleItems()
{
	CWnd* wnd;
	wnd = GetDlgItem(IDC_BUTTON_SHADER_PATHS);
	wnd->EnableWindow(m_enable_resource_monitor);

	wnd = GetDlgItem(IDC_OPTIONS_EDIT_ERRORLOG_FILENAME);
	if( wnd ) wnd->EnableWindow(m_error_output_log);

	wnd = GetDlgItem(IDC_OPTIONS_BUTTON_ERRORFILE_FIND);
	if( wnd ) wnd->EnableWindow(m_error_output_log);
}