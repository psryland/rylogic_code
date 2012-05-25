// AnimationControlDlg.cpp : implementation file
//

#include "Stdafx.h"
#include "LineDrawer/Source/LineDrawer.h"
#include "LineDrawer/GUI/AnimationControlDlg.h"

BEGIN_MESSAGE_MAP(AnimationControlDlg, CDialog)
	ON_NOTIFY(NM_RELEASEDCAPTURE, IDC_SLIDER_ANIMATION_TIMELINE, OnNMReleasedcaptureSliderAnimationTimeline)
	ON_EN_CHANGE(IDC_EDIT_ANIMATION_MAX_TIME, OnEnChangeEditAnimationMaxTime)
	ON_NOTIFY(NM_CUSTOMDRAW, IDC_SLIDER_ANIMATION_TIME_MULTIPLIER, OnNMCustomdrawSliderAnimationTimeMultiplier)
	ON_EN_CHANGE(IDC_EDIT_ANIMATION_TIME_MULTIPLIER, OnEnChangeEditAnimationTimeMultiplier)
	ON_BN_CLICKED(IDC_ANIMATION_PAUSE, OnBnClickedAnimationPause)
	ON_BN_CLICKED(IDC_ANIMATION_STEP_FORWARD, OnBnClickedAnimationStepForward)
	ON_BN_CLICKED(IDC_ANIMATION_PLAY_FORWARD, OnBnClickedAnimationPlayForward)
	ON_BN_CLICKED(IDC_ANIMATION_STEP_BACKWARD, OnBnClickedAnimationStepBackward)
	ON_BN_CLICKED(IDC_ANIMATION_PLAY_BACKWARD, OnBnClickedAnimationPlayBackward)
	ON_BN_CLICKED(IDCLOSE, OnBnClickedClose)
END_MESSAGE_MAP()

// AnimationControlDlg dialog
IMPLEMENT_DYNAMIC(AnimationControlDlg, CDialog)
AnimationControlDlg::AnimationControlDlg(CWnd* pParent)
:CDialog					(AnimationControlDlg::IDD, pParent)
,m_linedrawer				(0)
,m_state					(Paused)
,m_timeline					()
,m_time_multiplier_slider	()
,m_time_multiplier			(1.0f)
,m_max_time					(10000)
,m_time						(0)
,m_play_start				(0)
,m_direction				(1)
,m_poller_started			(false)
{}

AnimationControlDlg::~AnimationControlDlg()
{}

void AnimationControlDlg::DoDataExchange(CDataExchange* pDX)
{
	float max_time = m_max_time / 1000.0f;

	CDialog::DoDataExchange(pDX);
	DDX_Control	(pDX, IDC_SLIDER_ANIMATION_TIMELINE			, m_timeline);
	DDX_Text	(pDX, IDC_EDIT_ANIMATION_MAX_TIME			, max_time);
	DDX_Control	(pDX, IDC_SLIDER_ANIMATION_TIME_MULTIPLIER	, m_time_multiplier_slider);
	DDX_Text	(pDX, IDC_EDIT_ANIMATION_TIME_MULTIPLIER	, m_time_multiplier);

	m_max_time = static_cast<DWORD>(max_time * 1000.0f);
}

//*****
BOOL AnimationControlDlg::OnInitDialog()
{
	CDialog::OnInitDialog();

	m_timeline.SetRange(0, m_max_time, TRUE);
	m_timeline.SetPos(0);

	m_time_multiplier_slider.SetRange(0, 2 * TimeMultiplerRes, TRUE);
	m_time_multiplier_slider.SetPos(TimeMultiplerRes);

	return TRUE;
}

//*****
// Create the animation control dialog
void AnimationControlDlg::CreateGUI()
{
	m_linedrawer = &LineDrawer::Get();
	Create(AnimationControlDlg::IDD, (CWnd*)m_linedrawer->m_line_drawer_GUI);
}

//*****
// Show the gui window
void AnimationControlDlg::ShowGUI(bool show)
{
	if( show )
	{
		SetWindowPos((CWnd*)m_linedrawer->m_line_drawer_GUI, 0,0,0,0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
		if( !m_poller_started ) { m_linedrawer->Poller(true); m_poller_started = true; }
	}
	else
	{
		SetWindowPos((CWnd*)m_linedrawer->m_line_drawer_GUI, 0,0,0,0, SWP_NOMOVE | SWP_NOSIZE | SWP_HIDEWINDOW);
		if( m_poller_started ) { m_linedrawer->Poller(false); m_poller_started = false; }
	}
}

//*****
// Return the number of seconds of elapsed animation time
float AnimationControlDlg::GetAnimationTime()
{
	if( m_state == Play )
	{
		int now = static_cast<int>(GetTickCount());
		if( now > m_play_start )
		{
			m_time += m_direction * static_cast<int>(pr::Maximum<float>((now - m_play_start) * m_time_multiplier, 1.0f));
			if( m_time < 0 ) { m_time = 0; m_state = Paused; }
			if( m_time > m_max_time ) { m_time = m_max_time; m_state = Paused; }
			m_play_start = now;

			m_timeline.SetPos(m_time);
			m_timeline.RedrawWindow();
		}
	}
	return m_time / 1000.0f;
}

//*****
// Move the time line
void AnimationControlDlg::OnNMReleasedcaptureSliderAnimationTimeline(NMHDR*, LRESULT* pResult)
{
	m_time = m_timeline.GetPos();
	*pResult = 0;
}

//*****
// The max time has been changed
void AnimationControlDlg::OnEnChangeEditAnimationMaxTime()
{
	UpdateData(TRUE);
	m_timeline.SetRange(0, m_max_time, TRUE);
}

//*****
// Move the time multiplier
void AnimationControlDlg::OnNMCustomdrawSliderAnimationTimeMultiplier(NMHDR*, LRESULT* pResult)
{
	m_time_multiplier = pr::Pow(m_time_multiplier_slider.GetPos() / (float)TimeMultiplerRes, 3);
	UpdateData(FALSE);
	*pResult = 0;
}

//*****
// The time multiplier has been changed
void AnimationControlDlg::OnEnChangeEditAnimationTimeMultiplier()
{
//	int pos = static_cast<int>((float)TimeMultiplerRes * pr::Pow(m_time_multiplier, 1.0f/3.0f));
//	pos = pr::Clamp<int>(pos, 0, 2 * TimeMultiplerRes);
//	m_time_multiplier_slider.SetPos(pos);
}

//*****
// Pause the animation
void AnimationControlDlg::OnBnClickedAnimationPause()
{
	m_state = Paused;
}

void AnimationControlDlg::OnBnClickedAnimationStepForward()
{
	m_state		= Step;
	m_time		+= static_cast<int>(pr::Maximum<float>(StepSize * m_time_multiplier, 1.0f));
	m_time		= pr::Clamp<DWORD>(m_time, 0, m_max_time);

	m_timeline.SetPos(m_time);
	m_timeline.RedrawWindow();
}

void AnimationControlDlg::OnBnClickedAnimationStepBackward()
{
	m_state		= Step;
	m_time		-= static_cast<int>(pr::Maximum<float>(StepSize * m_time_multiplier, 1.0f));
	m_time		= pr::Clamp<int>(m_time, 0, m_max_time);
	
	m_timeline.SetPos(m_time);
	m_timeline.RedrawWindow();
}

void AnimationControlDlg::OnBnClickedAnimationPlayForward()
{
	m_state			= Play;
	m_direction		= 1;
	m_play_start	= static_cast<int>(GetTickCount());
}

void AnimationControlDlg::OnBnClickedAnimationPlayBackward()
{
	m_state			= Play;
	m_direction		= -1;
	m_play_start	= static_cast<int>(GetTickCount());
}

void AnimationControlDlg::OnBnClickedClose()
{
	OnClose();
}
