// GridSize.cpp : implementation file
//
#include "Stdafx.h"
#include "TetraMeshEditor.h"
#include "GridSize.h"

// GridSize dialog
IMPLEMENT_DYNAMIC(GridSize, CDialog)
GridSize::GridSize(CWnd* pParent, unsigned int grid_size)
:CDialog(GridSize::IDD, pParent)
,m_grid_size(grid_size)
{}

GridSize::~GridSize()
{}

void GridSize::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	DDX_Text(pDX, IDC_EDIT_GRID_SIZE, m_grid_size);
}

BEGIN_MESSAGE_MAP(GridSize, CDialog)
END_MESSAGE_MAP()
