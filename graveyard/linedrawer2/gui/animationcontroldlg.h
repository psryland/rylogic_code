#pragma once
#include <afxcmn.h>
#include <afxwin.h>
#include "LineDrawer/Resource.h"

// AnimationControlDlg dialog
class AnimationControlDlg : public CDialog
{
	DECLARE_DYNAMIC(AnimationControlDlg)
public:
	AnimationControlDlg(CWnd* pParent = 0);   // standard constructor
	virtual ~AnimationControlDlg();

	void	CreateGUI();
	void	ShowGUI(bool show = true);
	float	GetAnimationTime();
	bool	IsAnimationOn() const		{ return IsWindowVisible() == TRUE; }

	// Dialog Data
	enum { IDD = IDD_ANIMATION_CONTROL };
	enum { TimeMultiplerRes = 100, StepSize = 100 };
	enum State { Paused, Step, Play };

	afx_msg void OnNMCustomdrawSliderAnimationTimeMultiplier(NMHDR *pNMHDR, LRESULT *pResult);
	afx_msg void OnEnChangeEditAnimationMaxTime();
	afx_msg void OnBnClickedAnimationPause();
	afx_msg void OnBnClickedAnimationStepForward();
	afx_msg void OnBnClickedAnimationPlayForward();
	afx_msg void OnBnClickedAnimationStepBackward();
	afx_msg void OnBnClickedAnimationPlayBackward();

protected:
	void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	BOOL OnInitDialog();
	void OnClose()	{ ShowGUI(false); }
	void OnCancel() { OnClose(); }
	void OnOK()		{}
	DECLARE_MESSAGE_MAP()

private:
	LineDrawer*	m_linedrawer;
	State		m_state;
	CSliderCtrl m_timeline;
	CSliderCtrl m_time_multiplier_slider;
	float		m_time_multiplier;
	int			m_max_time;
	int			m_time;
	int			m_play_start;
	int			m_direction;
	bool		m_poller_started;
public:
	afx_msg void OnNMReleasedcaptureSliderAnimationTimeline(NMHDR *pNMHDR, LRESULT *pResult);
	afx_msg void OnEnUpdateEditAnimationTimeMultiplier();
	afx_msg void OnBnClickedClose();
	afx_msg void OnEnChangeEditAnimationTimeMultiplier();
};
