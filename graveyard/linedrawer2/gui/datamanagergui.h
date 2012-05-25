//*************************************************************************
//
// A Dialog interface for the data manager
//
//*************************************************************************
#ifndef DATA_MANAGER_GUI_H
#define DATA_MANAGER_GUI_H

#include <afxcmn.h>
#include "pr/GUI/SplitterCtrl.h"
#include "LineDrawer/Resource.h"
#include "LineDrawer/Objects/ObjectTypes.h"
#include "LineDrawer/Source/Forward.h"

class DataManagerGUI : public CDialog
{
	DECLARE_DYNAMIC(DataManagerGUI)
public:
	enum		{ IDD = IDD_DATA_LIST };
	enum EColumn { EColumn_Name = 0, EColumn_Type, EColumn_Visible, EColumn_Wireframe, EColumn_Volume, EColumn_Colour, EColumn_NumColumns };
	static HTREEITEM const	INVALID_TREE_ITEM;
	static int const		INVALID_LIST_ITEM;

	DataManagerGUI();
	~DataManagerGUI();

	void	Clear();
	void	Add(LdrObject* object, LdrObject* insert_after);
	void	Delete(LdrObject* object);
	void	SelectNone();
	void	SelectObject(LdrObject* object, bool make_visible = true);
	bool	GetSelectionBBox(BoundingBox& bbox, bool force_update = false);
	bool	GetSelectionTransform(m4x4& transform);

	DECLARE_MESSAGE_MAP()
	afx_msg void OnGetMinMaxInfo(MINMAXINFO* lpMMI);
	afx_msg void OnSize(UINT nType, int cx, int cy);
	afx_msg void OnPaint();
	afx_msg void OnDestroy();
	
	// Button Methods
	afx_msg void OnBnClickedButtonHideAll();
	afx_msg void OnBnClickedButtonUnhideAll();
	afx_msg void OnBnClickedButtonHide();
	afx_msg void OnBnClickedButtonUnhide();
	afx_msg void OnBnClickedButtonToggleVisibility();
	afx_msg void OnBnClickedButtonWireframeAll();
	afx_msg void OnBnClickedButtonUnwireframeAll();
	afx_msg void OnBnClickedButtonWireframe();
	afx_msg void OnBnClickedButtonUnwireframe();
	afx_msg void OnBnClickedButtonToggleWire();
	afx_msg void OnBnClickedButtonToggleAlpha();
	afx_msg void OnBnClickedButtonSetColour();
	afx_msg void OnBnClickedButtonInvSelection();
	afx_msg void OnBnClickedButtonEditSelection();
	afx_msg void OnBnClickedButtonDelSelection();
	afx_msg void OnBnClickedButtonExpandAll();
	afx_msg void OnBnClickedButtonCollapseAll();

	// List Ctrl Methods
	afx_msg void OnNMDblclkListData(NMHDR *pNMHDR, LRESULT *pResult);
	afx_msg void OnLvnKeydownListData(NMHDR *pNMHDR, LRESULT *pResult);
	afx_msg void OnLvnItemchangedListData(NMHDR *pNMHDR, LRESULT *pResult);
	
	// Tree Ctrl Methods
	afx_msg void OnTvnItemexpandedDataTree(NMHDR *pNMHDR, LRESULT *pResult);
	afx_msg void OnTvnSelchangedDataTree(NMHDR *pNMHDR, LRESULT *pResult);
	afx_msg void OnTvnKeydownDataTree(NMHDR *pNMHDR, LRESULT *pResult);

	// Misc Methods
	afx_msg void OnEnChangeEditSelectMask();
	afx_msg void OnKeyDown(UINT nChar, UINT nRepCnt, UINT nFlags);

protected:
	void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	BOOL OnInitDialog();
	void OnCancel();

private:
	// Private methods
	void	AddToTree(LdrObject* object, LdrObject* insert_after);
	void	DeleteFromTree(LdrObject* object);
	void	UpdateListItem(LdrObject* object, bool recursive);
	void	FixListCtrlReferences(int start_index);

	void	Collapse(LdrObject* object);
	void	CollapseRecursive(LdrObject* object);
	void	Expand(LdrObject* object, bool recursive);
	void	ExpandRecursive(LdrObject* object, bool recursive, int list_position);
	void	Refresh();
	
private:
	LineDrawer*			m_linedrawer;
	CTreeCtrl			m_data_tree;
	CListCtrl			m_data_list;
	CString				m_selection_mask;
	pr::SplitterCtrl	m_splitter;
	bool				m_refresh_pending;
	bool				m_selection_changed;
	BoundingBox			m_selection_last_bbox;
};

#endif//DATA_MANAGER_GUI_H
