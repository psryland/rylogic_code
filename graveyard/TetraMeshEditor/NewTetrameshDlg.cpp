// NewTetrameshDlg.cpp : implementation file
#include "stdafx.h"
#include "TetraMeshEditor.h"
#include "NewTetrameshDlg.h"

// NewTetrameshDlg dialog
IMPLEMENT_DYNAMIC(NewTetrameshDlg, CDialog)
NewTetrameshDlg::NewTetrameshDlg(CWnd* pParent)
:CDialog(NewTetrameshDlg::IDD, pParent)
,m_single(TRUE)
,m_dimX(1)
,m_dimY(1)
,m_dimZ(1)
,m_sizeX(1.0f)
,m_sizeY(1.0f)
,m_sizeZ(1.0f)
{}

NewTetrameshDlg::~NewTetrameshDlg()
{}

void NewTetrameshDlg::DoDataExchange(CDataExchange* pDX)
{
	BOOL grid = !m_single;
	CDialog::DoDataExchange(pDX);
	DDX_Check(pDX, IDC_RADIO_SINGLE,	m_single);
	DDX_Check(pDX, IDC_RADIO_GRID,		grid);
	DDX_Text (pDX, IDC_EDIT_NEWT_DIMX,	m_dimX);
	DDX_Text (pDX, IDC_EDIT_NEWT_DIMY,	m_dimY);
	DDX_Text (pDX, IDC_EDIT_NEWT_DIMZ,	m_dimZ);
	DDX_Text (pDX, IDC_EDIT_NEWT_SIZEX,	m_sizeX);
	DDX_Text (pDX, IDC_EDIT_NEWT_SIZEY,	m_sizeY);
	DDX_Text (pDX, IDC_EDIT_NEWT_SIZEZ,	m_sizeZ);
}

BEGIN_MESSAGE_MAP(NewTetrameshDlg, CDialog)
	ON_BN_CLICKED(IDC_RADIO_SINGLE,			&NewTetrameshDlg::OnRadioSingle)
	ON_BN_CLICKED(IDC_RADIO_GRID,			&NewTetrameshDlg::OnRadioGrid)
END_MESSAGE_MAP()

BOOL NewTetrameshDlg::OnInitDialog()
{
	OnRadioSingle();
	return CDialog::OnInitDialog();
}

// NewTetrameshDlg message handlers
void NewTetrameshDlg::OnRadioSingle()
{
	CWnd* item;
	item = GetDlgItem(IDC_EDIT_NEWT_DIMX);	item->EnableWindow(FALSE);
	item = GetDlgItem(IDC_EDIT_NEWT_DIMY);	item->EnableWindow(FALSE);
	item = GetDlgItem(IDC_EDIT_NEWT_DIMZ);	item->EnableWindow(FALSE);
	item = GetDlgItem(IDC_EDIT_NEWT_SIZEX);	item->EnableWindow(FALSE);
	item = GetDlgItem(IDC_EDIT_NEWT_SIZEY);	item->EnableWindow(FALSE);
	item = GetDlgItem(IDC_EDIT_NEWT_SIZEZ);	item->EnableWindow(FALSE);
}

void NewTetrameshDlg::OnRadioGrid()
{
	CWnd* item;
	item = GetDlgItem(IDC_EDIT_NEWT_DIMX);	item->EnableWindow(TRUE);
	item = GetDlgItem(IDC_EDIT_NEWT_DIMY);	item->EnableWindow(TRUE);
	item = GetDlgItem(IDC_EDIT_NEWT_DIMZ);	item->EnableWindow(TRUE);
	item = GetDlgItem(IDC_EDIT_NEWT_SIZEX);	item->EnableWindow(TRUE);
	item = GetDlgItem(IDC_EDIT_NEWT_SIZEY);	item->EnableWindow(TRUE);
	item = GetDlgItem(IDC_EDIT_NEWT_SIZEZ);	item->EnableWindow(TRUE);
}
