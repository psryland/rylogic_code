#pragma once
#include "Res/Resource.h"

// EditorDlg dialog
class EditorDlg : public CDialog
{
	DECLARE_DYNAMIC(EditorDlg)
public:
	enum { IDD = IDD_DIALOG_TETRAMESH_EDITOR };
	EditorDlg(CWnd* pParent = NULL);
	virtual ~EditorDlg();

	afx_msg void OnClose();
	afx_msg void OnFileNew();
	afx_msg void OnFileOpen();
	afx_msg void OnFileSave();
	afx_msg void OnFileSaveAs();
	afx_msg void OnFileExit();
	afx_msg void OnEditUndo();
	afx_msg void OnEditGridSize();
	afx_msg void OnEditWeldVerts();

	std::string	m_filename;

protected:
	virtual void DoDataExchange(CDataExchange* pDX);
	DECLARE_MESSAGE_MAP()
}
;
