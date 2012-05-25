// EditorDlg.cpp : implementation file
#include "stdafx.h"
#include "TetraMeshEditor.h"
#include "EditorDlg.h"
#include "GridSize.h"
#include "NewTetrameshDlg.h"

// EditorDlg dialog
IMPLEMENT_DYNAMIC(EditorDlg, CDialog)

EditorDlg::EditorDlg(CWnd* pParent)
:CDialog(EditorDlg::IDD, pParent)
{}

EditorDlg::~EditorDlg()
{}

void EditorDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
}

BEGIN_MESSAGE_MAP(EditorDlg, CDialog)
	ON_WM_CLOSE()
	ON_COMMAND(ID_FILE_NEW32770,		&EditorDlg::OnFileNew)
	ON_COMMAND(ID_FILE_OPEN32768,		&EditorDlg::OnFileOpen)
	ON_COMMAND(ID_FILE_SAVE32773,		&EditorDlg::OnFileSave)
	ON_COMMAND(ID_FILE_SAVEAS,			&EditorDlg::OnFileSaveAs)
	ON_COMMAND(ID_FILE_EXIT,			&EditorDlg::OnFileExit)
	ON_COMMAND(ID_EDIT_UNDO32771,		&EditorDlg::OnEditUndo)
	ON_COMMAND(ID_EDIT_GRIDSIZE,		&EditorDlg::OnEditGridSize)
	ON_COMMAND(ID_EDIT_WELDVERTS,		&EditorDlg::OnEditWeldVerts)
END_MESSAGE_MAP()

// Exit the plugin
void EditorDlg::OnClose()
{
	Editor().Shutdown();
}

void EditorDlg::OnFileNew()
{
	NewTetrameshDlg nt_dlg;
	INT_PTR result = nt_dlg.DoModal();
	if( result != IDOK ) return;
	Editor().New(nt_dlg.m_single != 0, nt_dlg.m_dimX, nt_dlg.m_dimY, nt_dlg.m_dimZ, nt_dlg.m_sizeX, nt_dlg.m_sizeY, nt_dlg.m_sizeZ);
}

void EditorDlg::OnFileOpen()
{
	CFileDialog filedlg(TRUE);
	filedlg.GetOFN().lpstrTitle = "Open a tetrahedral mesh";
	INT_PTR result = filedlg.DoModal();
	if( result != IDOK ) return;

	m_filename = filedlg.GetPathName().GetString();
	Editor().Open(m_filename.c_str());
}

void EditorDlg::OnFileSave()
{
	if( m_filename.empty() ) return OnFileSaveAs();
	Editor().Save(m_filename.c_str());
}

void EditorDlg::OnFileSaveAs()
{
	CFileDialog filedlg(FALSE);
	filedlg.GetOFN().lpstrTitle = "Save a tetrahedral mesh";
	INT_PTR result = filedlg.DoModal();
	if( result != IDOK ) return;

	m_filename = filedlg.GetPathName().GetString();
	OnFileSave();
}

void EditorDlg::OnFileExit()
{
	OnClose();
}

void EditorDlg::OnEditUndo()
{
	Editor().Undo(1);
}

void EditorDlg::OnEditGridSize()
{
	GridSize gridsize_dlg(AfxGetMainWnd(), Editor().GetGridSize());
	
	INT_PTR result = gridsize_dlg.DoModal();
	if( result != IDOK ) return;

	Editor().SetGridSize(gridsize_dlg.m_grid_size);
}

void EditorDlg::OnEditWeldVerts()
{
	Editor().WeldVerts();
}
