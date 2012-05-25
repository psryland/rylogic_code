#ifndef ADDOBJECTDLG_H
#define ADDOBJECTDLG_H

//*****
// AddObjectDlg dialog
class AddObjectDlg : public CDialog
{
	DECLARE_DYNAMIC(AddObjectDlg)
public:
	AddObjectDlg(CWnd* pParent, const char* window_title);
	virtual ~AddObjectDlg();

	// Dialog Data
	enum { IDD = IDD_ADD_OBJECT };
	CString m_object_string;
	CString m_window_title;

	DECLARE_MESSAGE_MAP()
	afx_msg void OnGetMinMaxInfo(MINMAXINFO* lpMMI);
	afx_msg void OnSize(UINT nType, int cx, int cy);

protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	virtual BOOL OnInitDialog();
};

#endif//ADDOBJECTDLG_H
