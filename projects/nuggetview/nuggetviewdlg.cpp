// NuggetViewDlg.cpp : implementation file
//

#include "stdafx.h"
#include "NuggetView.h"
#include "NuggetViewDlg.h"
#include "About.h"

using namespace pr;

#ifdef _DEBUG
#define new DEBUG_NEW
#endif

// About box message map
BEGIN_MESSAGE_MAP(CAboutDlg, CDialog)
END_MESSAGE_MAP()

// ListCtrl message map
BEGIN_MESSAGE_MAP(ListCtrl, CListCtrl)
	ON_WM_DROPFILES()
END_MESSAGE_MAP()

// TreeCtrl message map
BEGIN_MESSAGE_MAP(TreeCtrl, CTreeCtrl)
	ON_WM_DROPFILES()
END_MESSAGE_MAP()

// NuggetView message map
BEGIN_MESSAGE_MAP(NuggetViewDlg, CDialog)
	ON_WM_SYSCOMMAND()
	ON_WM_QUERYDRAGICON()
	ON_WM_PAINT()
	ON_WM_GETMINMAXINFO()
	ON_WM_SIZE()
	ON_WM_DROPFILES()
	ON_COMMAND(ID_FILE_OPEN,	OnFileOpen)
	ON_COMMAND(ID_FILE_SAVE,	OnFileSave)
	ON_COMMAND(ID_FILE_SAVEAS,	OnFileSaveas)

	// List handlers
	// Tree handlers
	//ON_NOTIFY(TVN_ITEMEXPANDED, IDC_TREE1,	OnTvnItemexpandedDataTree)

END_MESSAGE_MAP()


//*****
// NuggetViewDlg dialog
NuggetViewDlg::NuggetViewDlg()
:CDialog(NuggetViewDlg::IDD, 0)
,m_list(this)
,m_tree(this)
{
	m_hIcon = AfxGetApp()->LoadIcon(IDR_MAINFRAME);
}

void NuggetViewDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	DDX_Control(pDX, IDC_LIST1, m_list);
	DDX_Control(pDX, IDC_TREE1, m_tree);
	DDX_Control(pDX, IDC_SPLITTER, m_splitter);
}

//*****
// NuggetViewDlg message handlers
BOOL NuggetViewDlg::OnInitDialog()
{
	CDialog::OnInitDialog();

	// Add "About..." menu item to system menu.
	// IDM_ABOUTBOX must be in the system command range.
	ASSERT((IDM_ABOUTBOX & 0xFFF0) == IDM_ABOUTBOX);
	ASSERT(IDM_ABOUTBOX < 0xF000);

	CMenu* pSysMenu = GetSystemMenu(FALSE);
	if (pSysMenu != NULL)
	{
		CString strAboutMenu;
		strAboutMenu.LoadString(IDS_ABOUTBOX);
		if (!strAboutMenu.IsEmpty())
		{
			pSysMenu->AppendMenu(MF_SEPARATOR);
			pSysMenu->AppendMenu(MF_STRING, IDM_ABOUTBOX, strAboutMenu);
		}
	}

	// Set the icon for this dialog. The framework does this automatically
	// when the application's main window is not a dialog
	SetIcon(m_hIcon, TRUE);			// Set big icon
	SetIcon(m_hIcon, FALSE);		// Set small icon

	// Set up the splitter control
	SplitterCtrl::Settings settings;
	settings.m_type		= SplitterCtrl::Vertical;
	settings.m_parent	= this;
	settings.m_side1	= GetDlgItem(IDC_TREE1);
	settings.m_side2	= GetDlgItem(IDC_LIST1);
	m_splitter.Initialise(settings);

	// Set up the list control
	CRect rect;
	m_list.GetClientRect(&rect);
	m_list.InsertColumn(Id			, "Id         "	, LVCFMT_LEFT, rect.Width() / NumColumns, Id         );
	m_list.InsertColumn(Version		, "Version    " , LVCFMT_LEFT, rect.Width() / NumColumns, Version    );
	m_list.InsertColumn(Flags		, "Flags      " , LVCFMT_LEFT, rect.Width() / NumColumns, Flags      );
	m_list.InsertColumn(Description	, "Description"	, LVCFMT_LEFT, rect.Width() / NumColumns, Description);
	m_list.InsertColumn(Size		, "Size       " , LVCFMT_LEFT, rect.Width() / NumColumns, Size       );

	PostMessage(WM_SIZE);
	return TRUE;  // return TRUE  unless you set the focus to a control
}

//*****
// System commands
void NuggetViewDlg::OnSysCommand(UINT nID, LPARAM lParam)
{
	if ((nID & 0xFFF0) == IDM_ABOUTBOX)
	{
		CAboutDlg dlgAbout;
		dlgAbout.DoModal();
	}
	else
	{
		CDialog::OnSysCommand(nID, lParam);
	}
}

//*****
// The system calls this function to obtain the cursor to
// display while the user drags the minimized window.
HCURSOR NuggetViewDlg::OnQueryDragIcon()
{
	return static_cast<HCURSOR>(m_hIcon);
}

//*****
// If you add a minimize button to your dialog, you will need the code below
//  to draw the icon.  For MFC applications using the document/view model,
//  this is automatically done for you by the framework.
void NuggetViewDlg::OnPaint() 
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

//*****
// Define the limits for resizing
void NuggetViewDlg::OnGetMinMaxInfo(MINMAXINFO* lpMMI)
{
	const uint MinSizeX = 50;
	const uint MinSizeY = 50;
	lpMMI->ptMinTrackSize.x = MinSizeX;
	lpMMI->ptMinTrackSize.y = MinSizeY;
	CDialog::OnGetMinMaxInfo(lpMMI);
}

//*****
// Resize the DataManagerGUI window
void NuggetViewDlg::OnSize(UINT nType, int cx, int cy)
{
	CDialog::OnSize(nType, cx, cy);
	if( nType == SIZE_MINIMIZED ) return;

	// Get the new window size
	CRect client_area;
	GetClientRect(&client_area);
	client_area.DeflateRect(7, 7, 7, 7);
	
	int   width = client_area.Width();
	float split_fraction = m_splitter.GetSplitFraction();

	CWnd* wnd;
	CRect wndrect;

	// Move the list
	wndrect = client_area;
	wndrect.DeflateRect(static_cast<int>(split_fraction * width), 0, 0, 0);
	wnd = GetDlgItem(IDC_LIST1);
	if( wnd ) wnd->MoveWindow(wndrect);

	// Move the tree
	wndrect = client_area;
	wndrect.DeflateRect(0, 0, static_cast<int>((1.0f - split_fraction) * width), 0);
	wnd = GetDlgItem(IDC_TREE1);
	if( wnd ) wnd->MoveWindow(wndrect);

	// Move the 'splitter bar'
	wndrect = client_area;
	wndrect.DeflateRect(static_cast<int>(split_fraction * width) - 1, 0, static_cast<int>((1.0f - split_fraction) * width) - 1, 0);
	wnd = GetDlgItem(IDC_SPLITTER);
	if( wnd ) wnd->MoveWindow(wndrect);

	m_splitter.ResetMinMaxRange();
	m_splitter.SetSplitFraction(split_fraction);

	Invalidate();
}

//*****
// Called when return is pressed
void NuggetViewDlg::OnOK()
{
	// Ignore return's
}

//*****
// Dropped nugget file
void NuggetViewDlg::OnDropFiles(HDROP hDropInfo)
{
	uint num_files = DragQueryFile(hDropInfo, 0xFFFFFFFF, NULL, 0);
	if( num_files == 0 ) return;

	char filename[1024];
	if( DragQueryFile(hDropInfo, 0, filename, 1024) > 0 )
	{
		LoadNuggetFile(filename);
	}
	Invalidate();
}
void ListCtrl::OnDropFiles(HDROP hDropInfo) { return m_parent->OnDropFiles(hDropInfo); }
void TreeCtrl::OnDropFiles(HDROP hDropInfo) { return m_parent->OnDropFiles(hDropInfo); }

//*****
// File open
void NuggetViewDlg::OnFileOpen()
{
	CFileDialog filedlg(TRUE);
	filedlg.GetOFN().lpstrTitle = "Open a nugget file";
	INT_PTR result = filedlg.DoModal();
	if( result != IDOK ) return;
	LoadNuggetFile(filedlg.GetPathName());
}

//*****
// Save the current scene
void NuggetViewDlg::OnFileSave()
{
	SaveNuggetFile(m_filename.c_str());
}

//*****
// Save the current scene to a new file
void NuggetViewDlg::OnFileSaveas()
{
	CFileDialog filedlg(FALSE);
	INT_PTR result = filedlg.DoModal();
	if( result != IDOK ) return;
	SaveNuggetFile(filedlg.GetPathName());
}

//*****
// Load a nugget file
void NuggetViewDlg::LoadNuggetFile(const char* filename)
{
	// Reset the data
	m_filename = filename;
	m_nugget.clear();
	m_list.DeleteAllItems();
	m_tree.DeleteAllItems();

	// Recursively build the tree
	try
	{
		pr::Handle src_file = pr::FileOpen(m_filename.c_str(), pr::EFileOpen::Reading);
		FileIO src(&src_file);
		BuildTree(src, 0, TVI_ROOT);
	}
	catch (std::exception const& e)
	{
		MessageBox("Nugget file load failure", Fmt("Failed to load. Reason: '%s'", e.what()).c_str(), MB_OK|MB_ICONERROR);
	}
}

//*****
// Save a nugget file
void NuggetViewDlg::SaveNuggetFile(const char* filename)
{
	m_filename = filename;
}

//*****
// Recursively add nuggets to the tree.
void NuggetViewDlg::BuildTree(FileIO& src, uint32 offset, HTREEITEM parent)
{
	src;
	offset;
	parent;
	//// Loop over nuggets in the source data adding them to the tree
	//HTREEITEM prev = TVI_FIRST;
	//while( offset != src_file.m_size )
	//{
	//	if( offset > src_file.m_size ) { throw nugget::Exception(EResult_NuggetDataCorrupt, "Nugget data corrupt"); }
	//	
	//	Nugget nugget;
	//	EResult result = nugget.Initialise(src, offset, nugget::ECopyFlag_Reference);
	//	if( Failed(result) && result != nugget::EResult_NotNuggetData )	{ throw nugget::Exception(result, "Nugget data corrupt"); }

	//	// Add the nugget to the tree
	//	if( Succeeded(result) )
	//	{
	//		m_tree.InsertItem(
	//								object->m_name.c_str(),
	//								(object->m_parent)	? (object->m_parent->m_tree_item)	: (TVI_ROOT),
	//								(last_object)		? (last_object->m_tree_item)		: (TVI_FIRST)
	//								);
	//// Add the nuggets to the tree
	//
	//for( TNugget::const_iterator n = m_nugget.begin(), n_end = m_nugget.end(); n != n_end; ++n )
	//{
	//	union { uint id; char ch[4]; } as;
	//	as.id = n->GetId();
	//	prev = m_tree.InsertItem(
	//		Fmt("%8.8x (%c%c%c%c)", as.id, as.ch[0], as.ch[1], as.ch[2], as.ch[3]).c_str(),
	//		TVI_ROOT,
	//		prev);
	//
	//	m_tree.SetItemData(prev, reinterpret_cast<const DWORD_PTR>(&*n));
	//}
	//	}


	//	// Move the offset on to the next nugget
	//	offset += nugget.GetNuggetSizeInBytes();
	//	if( offset > src_size ) { return ; }

	//	// Add the nugget
	//	if( !nuggets_out.AddNugget(nugget) ) return EResult_SuccessPartialLoad;
	//}



	//// Load the nuggets at this level
	//if( Failed(nugget::Load(src_file filename, nugget::ECopyFlag_Reference, std::back_inserter(m_nugget))) )
	//{
	//	AfxMessageBox("Failed to load nugget file", MB_OK|MB_ICONEXCLAMATION, 0);
	//	return;
	//}


	//// Save a pointer to this object in the tree
	//m_data_tree.SetItemData(object->m_tree_item, reinterpret_cast<const DWORD&>(object));

	//// Add the children
	//for( std::size_t c = 0; c < object->m_child.size(); ++c )
	//{
	//	AddToTree(object->m_child[c], (c == 0) ? (0) : (object->m_child[c - 1]));
	//}




}
