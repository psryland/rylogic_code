//**********************************************************************
//
//	A tool bar for the physics lab plugin
//
//**********************************************************************
#include "Stdafx.h"
#include "PhysicsLab_LDPI/PhysicsLab.h"
#include "PhysicsLab_LDPI/PhysicsLabToolBar.h"

IMPLEMENT_DYNAMIC(CPhysicsLabToolBar, CDialog)

BEGIN_MESSAGE_MAP(CPhysicsLabToolBar, CDialog)
	ON_BN_CLICKED(IDC_BUTTON_Open, OnBnClickedButtonOpen)
	ON_BN_CLICKED(IDC_BUTTON_Reset, OnBnClickedButtonReset)
	ON_BN_CLICKED(IDC_BUTTON_Go, OnBnClickedButtonGo)
	ON_BN_CLICKED(IDC_BUTTON_Step, OnBnClickedButtonStep)
	ON_BN_CLICKED(IDC_BUTTON_Pause, OnBnClickedButtonPause)
	ON_BN_CLICKED(IDC_BUTTON_ZoomAll, OnBnClickedButtonZoomall)
	ON_BN_CLICKED(IDC_CHECK_ShowVelocity, OnBnClickedCheckShowvelocity)
	ON_BN_CLICKED(IDC_CHECK_ShowAngVel, OnBnClickedCheckShowangvel)
	ON_BN_CLICKED(IDC_CHECK_ShowAngMom, OnBnClickedCheckShowangmom)
END_MESSAGE_MAP()

//*****
// Constructor
CPhysicsLabToolBar::CPhysicsLabToolBar(CWnd* pParent)
:CDialog(CPhysicsLabToolBar::IDD, pParent)
,m_show_velocity(FALSE)
,m_show_ang_velocity(FALSE)
,m_show_ang_momentum(FALSE)
{}

//*****
// Destructor
CPhysicsLabToolBar::~CPhysicsLabToolBar()
{}

void CPhysicsLabToolBar::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	DDX_Check(pDX, IDC_CHECK_ShowVelocity, m_show_velocity);
	DDX_Check(pDX, IDC_CHECK_ShowAngVel, m_show_ang_velocity);
	DDX_Check(pDX, IDC_CHECK_ShowAngMom, m_show_ang_momentum);
}

//*****
// Open a scene
void CPhysicsLabToolBar::OnBnClickedButtonOpen()
{
	CFileDialog filedlg(TRUE);
	INT_PTR result = filedlg.DoModal();
	if( result != IDOK ) return;

	PhysicsLab::Get().LoadFile(filedlg.GetPathName().GetString());
}

//*****
// Reset the simulation
void CPhysicsLabToolBar::OnBnClickedButtonReset()
{
	PhysicsLab::Get().ResetSim();
}

//*****
// Start the simulation
void CPhysicsLabToolBar::OnBnClickedButtonGo()
{
	PhysicsLab::Get().StartSim();
}

//*****
// Step the simulation
void CPhysicsLabToolBar::OnBnClickedButtonStep()
{
	PhysicsLab::Get().StepSim();
}

//*****
// Pause the simulation
void CPhysicsLabToolBar::OnBnClickedButtonPause()
{
	PhysicsLab::Get().PauseSim();
}

//*****
// View the whole scene
void CPhysicsLabToolBar::OnBnClickedButtonZoomall()
{
	ldrSetCameraViewAll();
}

void CPhysicsLabToolBar::OnBnClickedCheckShowvelocity()
{
	UpdateData();
	PhysicsLab::Get().m_show_velocity = m_show_velocity == TRUE;
	PhysicsLab::Get().Refresh();
}

void CPhysicsLabToolBar::OnBnClickedCheckShowangvel()
{
	UpdateData();
	PhysicsLab::Get().m_show_ang_velocity = m_show_ang_velocity == TRUE;
	PhysicsLab::Get().Refresh();
}

void CPhysicsLabToolBar::OnBnClickedCheckShowangmom()
{
	UpdateData();
	PhysicsLab::Get().m_show_ang_momentum = m_show_ang_momentum == TRUE;
	PhysicsLab::Get().Refresh();
}

