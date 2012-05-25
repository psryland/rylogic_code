//******************************************
// Controls
//******************************************
#pragma once

#include "PhysicsTestbed/Forwards.h"
#include "pr/maths/averager.h"
#include "afxcmn.h"
#include "afxwin.h"

enum ERunMode
{
	ERunMode_Pause,
	ERunMode_Step,
	ERunMode_Go
};

// CControls dialog
class CControls : public CDialog
{
	DECLARE_DYNAMIC(CControls)
public:
	enum { IDD = IDD_DIALOG_CONTROLS };
	CControls(CWnd* pParent = NULL);
	virtual ~CControls();

	BOOL	OnInitDialog();
	void	OnDestroy();
	bool	StartFrame();
	bool	AdvanceFrame();
	void	EndFrame();
	void	RefreshControlData();
	void	RefreshMenuState();
	void	Clear();
	int		GetStepRate() const;
	float	GetStepSize() const;
	void	SetObjectCount(std::size_t object_count);
	void	SetFrameRate(float rate);
	void	SetFrameNumber(unsigned int frame_number);
	void	ShowCollisionImpulses(bool yes);
	void	ShowContactPoints(bool yes);
	void	Pause();
	pr::ldr::EPlugInResult HandleKeys(UINT nChar, UINT nRepCnt, UINT nFlags);

protected:
	virtual void DoDataExchange(CDataExchange* pDX);
	DECLARE_MESSAGE_MAP()
	afx_msg void OnClose();
	afx_msg void OnKeyDown(UINT nChar, UINT nRepCnt, UINT nFlags);
	afx_msg void OnFileOpen();
	afx_msg void OnFileExport();
	afx_msg void OnFileExportAs();
	afx_msg void OnFileExit();
	afx_msg void OnOptionsShapegeneration();
	afx_msg void OnOptionsExportEveryFrame();
	afx_msg void OnOptionsTerrainSampler();
	afx_msg void OnHelpKeycommands();
	afx_msg void OnBnClickedCheckViewStateChange();
	afx_msg void OnBnClickedCheckShowContacts();
	afx_msg void OnBnClickedCheckShowCollisionImpulses();
	afx_msg void OnBnClickedCheckStopObjVsTerrain();
	afx_msg void OnBnClickedCheckStopObjVsObj();
	afx_msg void OnBnClickedCheckStopAtFrame();
	afx_msg void OnBnClickedButtonSimReset();
	afx_msg void OnBnClickedButtonSimGo();
	afx_msg void OnBnClickedButtonSimPause();
	afx_msg void OnBnClickedButtonSimStep();
	afx_msg void OnEnChangeEditRandSeed();
	afx_msg void OnEnChangeEditStepSize();
	afx_msg void OnEnChangeEditStepRate();
	afx_msg void OnHScroll(UINT nSBCode, UINT nPos, CScrollBar* pScrollBar);
	afx_msg void OnEnChangeEditStopAtFrame();

private:
	void CreateBox();
	void CreateCylinder();
	void CreateSphere();
	void CreatePolytope();

private:
	CEdit					m_ctrl_frame_number;
	unsigned int			m_frame_number;
	CEdit					m_ctrl_frame_rate;
	pr::Averager<float, 60>	m_frame_rate;
	CEdit					m_ctrl_object_count;
	unsigned int			m_object_count;
	CEdit					m_ctrl_sel_position;
	CEdit					m_ctrl_sel_velocity;
	CEdit					m_ctrl_sel_ang_vel;
	CEdit					m_ctrl_sel_address;
	CEdit					m_ctrl_rand_seed;
	unsigned int			m_rand_seed;
	bool					m_change_rand_seed;
	CEdit					m_ctrl_step_size;
	CEdit					m_ctrl_step_rate;
	CSliderCtrl				m_ctrl_step_rate_slider;
	bool					m_stop_on_obj_vs_terrain;
	bool					m_stop_on_obj_vs_obj;
	CEdit					m_ctrl_stop_at_frame;
	ERunMode				m_run_mode;
	std::string				m_export_filename;
	bool					m_export_every_frame;
	bool					m_export_as_physics_scene;
	DWORD					m_last_refresh_time;
	DWORD					m_frame_end;
	float					m_time_remainder;
};
