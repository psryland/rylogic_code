// MultiViewerDlg.cpp : implementation file
//

#include "stdafx.h"
#include "Common\RegistryKey.h"
#include "Common\Fmt.h"
#include "Common\PRFileSys.h"
#include "Common/PRString.h"
#include "MultiViewer.h"
#include "MultiViewerDlg.h"
#include "SettingsDlg.h"

//#define DEBUGOUTPUT
	#ifdef DEBUGOUTPUT
		#include "Common\Console.h"
		pr::Console g_Cons;
		#define DebugPrint(x) g_Cons.Print(x)
	#else
		#define DebugPrint(x)
	#endif//DEBUGOUTPUT

#ifndef NDEBUG
#define new DEBUG_NEW
#endif//NDEBUG


// CMultiViewerDlg dialog



CMultiViewerDlg::CMultiViewerDlg(CWnd* pParent /*=NULL*/)
	: CDialog(CMultiViewerDlg::IDD, pParent)
	, m_viewer(_T(""))
	, m_file_types(_T(""))
	, m_recursive(false)
	, m_current(0)
{
	m_hIcon = AfxGetApp()->LoadIcon(IDR_MAINFRAME);
}

void CMultiViewerDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	DDX_Control(pDX, IDC_BUTTON_LEFT, m_left_button);
	DDX_Control(pDX, IDC_BUTTON_RIGHT, m_right_button);
	DDX_Control(pDX, IDC_BUTTON_SETTINGS, m_settings_button);
}

BEGIN_MESSAGE_MAP(CMultiViewerDlg, CDialog)
	ON_WM_DESTROY()
	ON_WM_PAINT()
	ON_WM_QUERYDRAGICON()
	//}}AFX_MSG_MAP
	ON_BN_CLICKED(IDC_BUTTON_SETTINGS, OnBnClickedButtonSettings)
	ON_BN_CLICKED(IDC_BUTTON_LEFT, OnBnClickedButtonLeft)
	ON_BN_CLICKED(IDC_BUTTON_RIGHT, OnBnClickedButtonRight)
END_MESSAGE_MAP()


// CMultiViewerDlg message handlers

BOOL CMultiViewerDlg::OnInitDialog()
{
	CDialog::OnInitDialog();

	// Set the icon for this dialog.  The framework does this automatically
	//  when the application's main window is not a dialog
	SetIcon(m_hIcon, TRUE);			// Set big icon
	SetIcon(m_hIcon, FALSE);		// Set small icon

	m_left_bitmap.LoadBitmap(IDB_BITMAP_LEFT);
	m_left_button.SetBitmap(m_left_bitmap);
	
	m_right_bitmap.LoadBitmap(IDB_BITMAP_RIGHT);
	m_right_button.SetBitmap(m_right_bitmap);

	m_settings_bitmap.LoadBitmap(IDB_BITMAP_SETTINGS);
	m_settings_button.SetBitmap(m_settings_bitmap);

	// Look in the registry for the viewer app string and file types
	RegistryKey key;
	if( key.Open("Software\\MultiViewer", RegistryKey::Readonly) )
	{
		DWORD viewer_str_length		= key.GetKeyLength("Viewer");
		DWORD filetypes_str_length	= key.GetKeyLength("FileTypes");
		if( viewer_str_length > 0 )
		{
			char* str = new char[viewer_str_length];
			if( key.Read("Viewer", str, viewer_str_length) )
				m_viewer = str;
			delete str;
		}
		if( filetypes_str_length > 0 )
		{
			char* str = new char[filetypes_str_length];
			if( key.Read("FileTypes", str, filetypes_str_length) )
				m_file_types = str;
			delete str;
		}
		key.Read("Recursive", m_recursive);
	}

	LPWSTR* argv; int argc;
	argv = CommandLineToArgvW(GetCommandLineW(), &argc);
	CString command_line = (argc < 2) ? ("") : (CW2A(argv[1]));
	GlobalFree(argv);

	if( argc < 2 )
	{
		MessageBox(	Fmt("No source file or path provided\n"
						"Command Line: %s", GetCommandLine()), "Info");
		EndDialog(0);
		return TRUE;
	}
	
	BuildListOfFilesToView(command_line);
	ViewFile();
	SetWindowPos(&wndTopMost, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);

	return TRUE;  // return TRUE  unless you set the focus to a control
}

void CMultiViewerDlg::OnDestroy()
{
	m_process.Stop();

	// Save the viewer app string and file types to the registry
	RegistryKey key;
	if( key.Open("Software\\MultiViewer", RegistryKey::Writeable) )
	{
		DWORD viewer_str_length		= (DWORD)m_viewer.length();
		if( viewer_str_length > 0 )
			key.Write("Viewer", m_viewer.c_str());
		
		DWORD filetypes_str_length	= (DWORD)m_file_types.length();
		if( filetypes_str_length > 0 )
			key.Write("FileTypes", m_file_types.c_str());

		key.Write("Recursive", m_recursive);
	}
}

// If you add a minimize button to your dialog, you will need the code below
//  to draw the icon.  For MFC applications using the document/view model,
//  this is automatically done for you by the framework.

void CMultiViewerDlg::OnPaint() 
{
	if (IsIconic())
	{
		CPaintDC dc(this); // device context for painting

		SendMessage(WM_ICONERASEBKGND, reinterpret_cast<WPARAM>(dc.GetSafeHdc()), 0);

		// Center icon in client rectangle
		int cxIcon = GetSystemMetrics(SM_CXICON);
		int cyIcon = GetSystemMetrics(SM_CYICON);
		CRect rect;
		GetClientRect(&rect);
		int x = (rect.Width() - cxIcon + 1) / 2;
		int y = (rect.Height() - cyIcon + 1) / 2;

		// Draw the icon
		dc.DrawIcon(x, y, m_hIcon);
	}
	else
	{
		CDialog::OnPaint();
	}
}

// The system calls this function to obtain the cursor to display while the user drags
//  the minimized window.
HCURSOR CMultiViewerDlg::OnQueryDragIcon()
{
	return static_cast<HCURSOR>(m_hIcon);
}

void CMultiViewerDlg::OnBnClickedButtonSettings()
{
	SettingsDlg settingsdlg;

	settingsdlg.m_source		= m_source;
	settingsdlg.m_viewer		= m_viewer;
	settingsdlg.m_file_types	= m_file_types;
	settingsdlg.m_recursive		= m_recursive;

	INT_PTR result = settingsdlg.DoModal();
	if( result != IDOK ) return;

	m_source					= settingsdlg.m_source;
	m_viewer					= settingsdlg.m_viewer;
	m_file_types				= settingsdlg.m_file_types;
	m_recursive					= settingsdlg.m_recursive;
	PR::Str::LowerCase(m_file_types);

	BuildListOfFilesToView(m_source.c_str());
	ViewFile();
}

void CMultiViewerDlg::OnBnClickedButtonLeft()
{
	if( m_files.size() == 0 ) return;

	if( --m_current < 0 ) m_current = 0;
	ViewFile();
}

void CMultiViewerDlg::OnBnClickedButtonRight()
{
	if( m_files.size() == 0 ) return;

	if( ++m_current == (int)m_files.size() ) m_current = (int)m_files.size() - 1;
	ViewFile();
}

void CMultiViewerDlg::ViewFile()
{
	SetWindowText(Fmt("MultiViewer - %d of %d", m_current + 1, m_files.size()));

	if( m_files.size() == 0 ) return;
	if( m_process.IsActive() ) m_process.Stop();
	
	std::string args = "\"" + m_viewer + "\" " + m_files[m_current];
	char* _args = static_cast<char*>(_alloca(args.length()));
	strcpy(_args, args.c_str());
	m_process.Start(NULL, _args);

	DebugPrint(Fmt("Start: %s\n", _args));
}

bool CMultiViewerDlg::IsViewable(const char* extension)
{
	if( extension[0] == '\0' ) return false;
	return m_file_types.find(extension + 1) != std::string::npos;
}

void CMultiViewerDlg::BuildListOfFilesToView(const char* path)
{
	m_files.clear();
	m_current = 0;

	m_source = PR::FileSys::GetDirectory(path);
	std::string filename = PR::FileSys::GetFilename(path);
	std::string mask = m_source + "/*.*";

	// Find the files 
	WIN32_FIND_DATA find_data;
	HANDLE find_handle = FindFirstFile(mask.c_str(), &find_data);
	if( find_handle == INVALID_HANDLE_VALUE )
	{
		MessageBox("Invalid source file or path", "Info");
		PostQuitMessage(0);	
		return;
	}
	do
	{
		std::string file = PR::FileSys::Make(m_source, find_data.cFileName);
		if( PR::FileSys::GetAttribs(file) & PR::FileSys::Attrib_Directory )
			continue;

		if( stricmp(filename.c_str(), find_data.cFileName) == 0 )
			m_current = (int)m_files.size();
		
		if( IsViewable(PR::FileSys::GetExtension(file)) )
			m_files.push_back(std::string("\"" + file + "\""));
	}
	while( FindNextFile(find_handle, &find_data) );
	FindClose(find_handle);
}

