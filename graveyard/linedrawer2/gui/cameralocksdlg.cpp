// CCameraLocksDlg.cpp : implementation file
//

#include "Stdafx.h"
#include "LineDrawer/Source/LineDrawer.h"
#include "LineDrawer/Source/NavigationManager.h"
#include "LineDrawer/GUI/CameraLocksDlg.h"

BEGIN_MESSAGE_MAP(CCameraLocksDlg, CDialog)
	ON_BN_CLICKED(IDC_LOCK_TRANSLATION_X, &CCameraLocksDlg::OnBnClickedLockTranslationX)
	ON_BN_CLICKED(IDC_LOCK_TRANSLATION_Y, &CCameraLocksDlg::OnBnClickedLockTranslationY)
	ON_BN_CLICKED(IDC_LOCK_TRANSLATION_Z, &CCameraLocksDlg::OnBnClickedLockTranslationZ)
	ON_BN_CLICKED(IDC_LOCK_ROTATION_X, &CCameraLocksDlg::OnBnClickedLockRotationX)
	ON_BN_CLICKED(IDC_LOCK_ROTATION_Y, &CCameraLocksDlg::OnBnClickedLockRotationY)
	ON_BN_CLICKED(IDC_LOCK_ROTATION_Z, &CCameraLocksDlg::OnBnClickedLockRotationZ)
	ON_BN_CLICKED(IDC_LOCK_ZOOM, &CCameraLocksDlg::OnBnClickedLockZoom)
	ON_BN_CLICKED(IDC_LOCK_CAMERA_RELATIVE, &CCameraLocksDlg::OnBnClickedLockCameraRelative)
END_MESSAGE_MAP()

// CCameraLocksDlg dialog
IMPLEMENT_DYNAMIC(CCameraLocksDlg, CDialog)
CCameraLocksDlg::CCameraLocksDlg(CWnd* pParent)
:CDialog(CCameraLocksDlg::IDD, pParent)
,m_nav(0)
,m_locks()
{}

CCameraLocksDlg::~CCameraLocksDlg()
{}

void DDX_Check(CDataExchange* pDX, int nIDC, LockMask::reference value)
{
	BOOL bit = value;
	DDX_Check(pDX, nIDC, bit);
	value = bit != 0;
}
void CCameraLocksDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	DDX_Check(pDX, IDC_LOCK_TRANSLATION_X	, m_locks[LockMask::TransX]);
	DDX_Check(pDX, IDC_LOCK_TRANSLATION_Y	, m_locks[LockMask::TransY]);
	DDX_Check(pDX, IDC_LOCK_TRANSLATION_Z	, m_locks[LockMask::TransZ]);
	DDX_Check(pDX, IDC_LOCK_ROTATION_X		, m_locks[LockMask::RotX]);
	DDX_Check(pDX, IDC_LOCK_ROTATION_Y		, m_locks[LockMask::RotY]);
	DDX_Check(pDX, IDC_LOCK_ROTATION_Z		, m_locks[LockMask::RotZ]);
	DDX_Check(pDX, IDC_LOCK_ZOOM			, m_locks[LockMask::Zoom]);
	DDX_Check(pDX, IDC_LOCK_CAMERA_RELATIVE	, m_locks[LockMask::CameraRelative]);
}

//*****
BOOL CCameraLocksDlg::OnInitDialog()
{
	m_nav = &LineDrawer::Get().m_navigation_manager;
	CDialog::OnInitDialog();
	return TRUE;
}

// Create the animation control dialog
void CCameraLocksDlg::CreateGUI()
{
	Create(CCameraLocksDlg::IDD, (CWnd*)LineDrawer::Get().m_line_drawer_GUI);
}

// Show the gui window
void CCameraLocksDlg::ShowGUI()
{
	SetWindowPos((CWnd*)LineDrawer::Get().m_line_drawer_GUI, 0,0,0,0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
}

void CCameraLocksDlg::OnBnClickedLockTranslationX()		{ m_locks[LockMask::TransX        ] = !m_locks[LockMask::TransX        ]; m_nav->SetLockMask(m_locks); }
void CCameraLocksDlg::OnBnClickedLockTranslationY()		{ m_locks[LockMask::TransY        ] = !m_locks[LockMask::TransY        ]; m_nav->SetLockMask(m_locks); }
void CCameraLocksDlg::OnBnClickedLockTranslationZ()		{ m_locks[LockMask::TransZ        ] = !m_locks[LockMask::TransZ        ]; m_nav->SetLockMask(m_locks); }
void CCameraLocksDlg::OnBnClickedLockRotationX()		{ m_locks[LockMask::RotX          ] = !m_locks[LockMask::RotX          ]; m_nav->SetLockMask(m_locks); }
void CCameraLocksDlg::OnBnClickedLockRotationY()		{ m_locks[LockMask::RotY          ] = !m_locks[LockMask::RotY          ]; m_nav->SetLockMask(m_locks); }
void CCameraLocksDlg::OnBnClickedLockRotationZ()		{ m_locks[LockMask::RotZ          ] = !m_locks[LockMask::RotZ          ]; m_nav->SetLockMask(m_locks); }
void CCameraLocksDlg::OnBnClickedLockZoom()				{ m_locks[LockMask::Zoom          ] = !m_locks[LockMask::Zoom          ]; m_nav->SetLockMask(m_locks); }
void CCameraLocksDlg::OnBnClickedLockCameraRelative()	{ m_locks[LockMask::CameraRelative] = !m_locks[LockMask::CameraRelative]; m_nav->SetLockMask(m_locks); }
