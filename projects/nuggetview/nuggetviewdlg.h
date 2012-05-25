// NuggetViewDlg.h : header file
//

#pragma once
#include <list>
#include "afxcmn.h"
#include "pr/common/prtypes.h"
#include "pr/common/fmt.h"
#include "pr/gui/splitterctrl.h"
#include "pr/storage/nugget_file/nuggetfile.h"

class NuggetViewDlg;
struct ListCtrl : CListCtrl
{
	ListCtrl(NuggetViewDlg* parent) : m_parent(parent) {}
	
	afx_msg void OnDropFiles(HDROP hDropInfo);
	DECLARE_MESSAGE_MAP()

	NuggetViewDlg* m_parent;
};

struct TreeCtrl : CTreeCtrl
{
	TreeCtrl(NuggetViewDlg* parent) : m_parent(parent) {}
	
	afx_msg void OnDropFiles(HDROP hDropInfo);
	DECLARE_MESSAGE_MAP()

	NuggetViewDlg* m_parent;
};

typedef std::list<pr::nugget::Nugget> TNugget;

// NuggetViewDlg dialog
class NuggetViewDlg : public CDialog
{
public:
	enum		{ IDD = IDD_NUGGETVIEW_DIALOG };
	enum Column { Id = 0, Version, Flags, Description, Size, NumColumns };
	NuggetViewDlg();	// standard constructor

	virtual void DoDataExchange(CDataExchange* pDX);	// DDX/DDV support
	virtual BOOL OnInitDialog();

	// Generated message map functions
	afx_msg void OnSysCommand(UINT nID, LPARAM lParam);
	afx_msg void OnPaint();
	afx_msg void OnGetMinMaxInfo(MINMAXINFO* lpMMI);
	afx_msg HCURSOR OnQueryDragIcon();
	afx_msg void OnSize(UINT nType, int cx, int cy);
	afx_msg void OnOK();
	afx_msg void OnDropFiles(HDROP hDropInfo);
	afx_msg void OnFileOpen();
	afx_msg void OnFileSave();
	afx_msg void OnFileSaveas();
	DECLARE_MESSAGE_MAP()

	HICON m_hIcon;

private:
	void LoadNuggetFile(const char* filename);
	void SaveNuggetFile(const char* filename);
	void BuildTree(FileIO& src, uint32 offset, HTREEITEM parent);

	ListCtrl			m_list;
	TreeCtrl			m_tree;
	pr::SplitterCtrl	m_splitter;
	std::string			m_filename;
	TNugget				m_nugget;	// Storage for the loaded nuggets
};
