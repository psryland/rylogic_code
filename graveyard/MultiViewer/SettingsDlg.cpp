// SettingsDlg.cpp : implementation file
//

#include "stdafx.h"
#include "MultiViewer.h"
#include "SettingsDlg.h"


// SettingsDlg dialog

IMPLEMENT_DYNAMIC(SettingsDlg, CDialog)
SettingsDlg::SettingsDlg(CWnd* pParent /*=NULL*/)
:CDialog(SettingsDlg::IDD, pParent)
,m_viewer(_T(""))
,m_file_types(_T(""))
,m_recursive(false)
,m_source(_T(""))
{
	// Initialise COM
	HRESULT hres = CoInitialize(NULL);
	if( FAILED(hres) )
	{
		MessageBox("Failed to Initialise COM", NULL, MB_ICONEXCLAMATION | MB_OK);
	}

}

SettingsDlg::~SettingsDlg()
{
	// Uninitialise COM
	CoUninitialize();
}

void SettingsDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	DDX_Text(pDX, IDC_EDIT_VIEWERAPP, CString(m_viewer.c_str()));
	DDX_Text(pDX, IDC_EDIT_FILETYPES, CString(m_file_types.c_str()));
	DDX_Text(pDX, IDC_EDIT_SOURCE, CString(m_source.c_str()));
}


BEGIN_MESSAGE_MAP(SettingsDlg, CDialog)
	ON_BN_CLICKED(IDC_BUTTON_BROWSE, OnBnClickedButtonBrowse)
	ON_BN_CLICKED(IDC_BUTTON_BROWSE_SOURCE, OnBnClickedButtonBrowseSource)
END_MESSAGE_MAP()


// SettingsDlg message handlers

void SettingsDlg::OnBnClickedButtonBrowse()
{
	CFileDialog opendlg(TRUE, NULL, NULL, OFN_HIDEREADONLY|OFN_OVERWRITEPROMPT,
		"Executables (*.exe)||", this);

	INT_PTR result = opendlg.DoModal();
	if( result != IDOK ) return;

	m_viewer = opendlg.GetPathName();
	UpdateData(FALSE);
}

void SettingsDlg::OnBnClickedButtonBrowseSource()
{
	char display_name[MAX_PATH];
	int image_number = 0;
	BROWSEINFO browse_info;
	browse_info.hwndOwner		= GetSafeHwnd();
	browse_info.pidlRoot		= NULL;
	browse_info.pszDisplayName	= display_name;
	browse_info.lpszTitle		= "Select a source directory...";
	browse_info.ulFlags			= BIF_RETURNONLYFSDIRS | BIF_USENEWUI;
	browse_info.lpfn			= NULL;
	browse_info.lParam			= 0;
	browse_info.iImage			= image_number;
	ITEMIDLIST* pidl			= SHBrowseForFolder(&browse_info);
	if( !pidl ) return;

	char path[MAX_PATH];
	if( !SHGetPathFromIDList(pidl, path) )
	{
		MessageBox("Could find selected path", NULL, MB_ICONEXCLAMATION | MB_OK);
	}
	else
	{
		m_source = path;
		m_source += "\\";
		UpdateData(FALSE);
	}
		
	// We need to delete the ITEMIDLIST using the IMalloc::Free method
	IMalloc *pMalloc;
	HRESULT hres = SHGetMalloc(&pMalloc);
	if( SUCCEEDED(hres) )
	{
		pMalloc->Free(pidl);
		pMalloc->Release();
	}
}
