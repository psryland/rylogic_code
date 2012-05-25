#pragma once

// CCameraLocksDlg dialog
class CCameraLocksDlg : public CDialog
{
	DECLARE_DYNAMIC(CCameraLocksDlg)
public:
	CCameraLocksDlg(CWnd* pParent = NULL);
	virtual ~CCameraLocksDlg();

	// Dialog Data
	enum { IDD = IDD_LOCKS };

	BOOL OnInitDialog();
	void CreateGUI();
	void ShowGUI();

	NavigationManager* m_nav;
	LockMask m_locks;

protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support

	DECLARE_MESSAGE_MAP()
	afx_msg void OnBnClickedLockTranslationX();
	afx_msg void OnBnClickedLockTranslationY();
	afx_msg void OnBnClickedLockTranslationZ();
	afx_msg void OnBnClickedLockRotationX();
	afx_msg void OnBnClickedLockRotationY();
	afx_msg void OnBnClickedLockRotationZ();
	afx_msg void OnBnClickedLockZoom();
	afx_msg void OnBnClickedLockCameraRelative();
};
