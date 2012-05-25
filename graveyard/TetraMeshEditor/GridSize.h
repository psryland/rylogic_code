#pragma once

// GridSize dialog
class GridSize : public CDialog
{
	DECLARE_DYNAMIC(GridSize)
public:
	enum { IDD = IDD_DIALOG_GRID_SIZE };
	GridSize(CWnd* pParent, unsigned int grid_size);
	virtual ~GridSize();

	unsigned int m_grid_size;

protected:
	virtual void DoDataExchange(CDataExchange* pDX);
	DECLARE_MESSAGE_MAP()
};
