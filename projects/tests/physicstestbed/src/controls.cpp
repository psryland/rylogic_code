//******************************************
// Controls
//******************************************

#include "PhysicsTestbed/Stdafx.h"
#include "PhysicsTestbed/PhysicsTestbed.h"
#include "PhysicsTestbed/Controls.h"
#include "PhysicsTestbed/ShapeGenParams.h"
#include "PhysicsTestbed/ShapeGenParamsDlg.h"

using namespace pr;

enum EUpdate
{
	EUpdate_Send = FALSE,
	EUpdate_Read = TRUE
};

bool CollisionWatchPreCollisionCallBack(col::Data const& col_data);
void CollisionWatchPstCollisionCallBack(col::Data const& col_data);

bool PCCB_ShowContacts(col::Data const& col_data)
{
	for( pr::uint i = 0; i != col_data.NumContacts(); ++i )
	{
		col::Contact ct = col_data.GetContact(0,i);
		Testbed().m_scene_manager.AddContact(ct.m_ws_point, ct.m_ws_normal);
	}
	return true;
}
bool PCCB_StopOnObjVsObj(col::Data const& col_data)
{
	if( col_data.m_objA && col_data.m_objB &&
		PhysicsEngine::ObjectGetPhysObjType(col_data.m_objA) == EPhysObjType_Terrain &&
		PhysicsEngine::ObjectGetPhysObjType(col_data.m_objB) == EPhysObjType_Terrain )
		Testbed().m_controls.Pause();
	return true;
}
bool PCCB_StopOnObjVsTerrain(col::Data const& col_data)
{
	if( col_data.m_objA && col_data.m_objB &&
		(PhysicsEngine::ObjectGetPhysObjType(col_data.m_objA) == EPhysObjType_Terrain ||
		 PhysicsEngine::ObjectGetPhysObjType(col_data.m_objB) == EPhysObjType_Terrain) )
		Testbed().m_controls.Pause();
	return true;
}
void PCCB_ShowImpulses(col::Data const& col_data)
{
	for( pr::uint i = 0; i != col_data.NumContacts(); ++i )
	{
		col::Contact ct = col_data.GetContact(0,i);
		Testbed().m_scene_manager.AddImpulse(ct.m_ws_point, ct.m_ws_impulse);
	}
}

// CControls dialog
IMPLEMENT_DYNAMIC(CControls, CDialog)
CControls::CControls(CWnd* pParent)
:CDialog(CControls::IDD, pParent)
,m_ctrl_frame_number()
,m_frame_number(0)
,m_ctrl_frame_rate()
,m_frame_rate()
,m_ctrl_object_count()
,m_object_count(0)
,m_ctrl_sel_position()
,m_ctrl_sel_velocity()
,m_ctrl_sel_ang_vel()
,m_ctrl_sel_address()
,m_ctrl_rand_seed()
,m_rand_seed(0)
,m_change_rand_seed(false)
,m_ctrl_step_size()
,m_ctrl_step_rate()
,m_ctrl_step_rate_slider()
,m_stop_on_obj_vs_terrain(false)
,m_stop_on_obj_vs_obj(false)
,m_ctrl_stop_at_frame()
,m_run_mode(ERunMode_Pause)
,m_export_filename("C:/DeleteMe/phystestbed_Snapshot.pr_script")
,m_export_every_frame(false)
,m_export_as_physics_scene(true)
,m_last_refresh_time(0)
,m_frame_end(GetTickCount())
,m_time_remainder(0.0f)
{}

CControls::~CControls()
{}

// Initialise the dialog
BOOL CControls::OnInitDialog()
{
	if( !CDialog::OnInitDialog() )
		return FALSE;

	pr::IRect ldr_rect = ldrGetMainWindowRect();
	CRect controls_rect; GetWindowRect(&controls_rect);
	MoveWindow(ldr_rect.m_min.x - controls_rect.Width(), ldr_rect.m_min.y, controls_rect.Width(), controls_rect.Height());

	m_ctrl_step_rate_slider	.SetRange(1, 200);
	m_ctrl_rand_seed		.SetWindowText(FmtS("%d", m_rand_seed));
	m_ctrl_step_size		.SetWindowText(FmtS("%d", Testbed().m_state.m_step_size_inv));
	m_ctrl_step_rate		.SetWindowText(FmtS("%d", Testbed().m_state.m_step_rate));
	m_ctrl_stop_at_frame	.SetWindowText(FmtS("%d", Testbed().m_state.m_stop_at_frame_number));
	m_ctrl_step_rate_slider	.SetPos(Testbed().m_state.m_step_rate);
	UpdateData(EUpdate_Send);
	OnBnClickedCheckStopAtFrame();
	OnBnClickedCheckShowContacts();
	OnBnClickedCheckShowCollisionImpulses();
	Clear();
	RefreshMenuState();
	return TRUE;
}

// Destroy
void CControls::OnDestroy()
{}


// Return true if the simulation should be advanced in time
bool CControls::StartFrame()
{
	if( !m_hWnd ) return false;

	DWORD now = GetTickCount();

	// Only update the control window data every 0.5 second
	if( now - m_last_refresh_time > 500 )
	{
		RefreshControlData();
		m_last_refresh_time = now;
	}

	switch( m_run_mode )
	{
	case ERunMode_Pause:
		return false;

	case ERunMode_Step:
		OnFileExport();
		return true;

	case ERunMode_Go:
		if( Testbed().m_state.m_stop_at_frame && Testbed().m_state.m_stop_at_frame_number == m_frame_number )
		{
			m_run_mode = ERunMode_Pause;
			return false;
		}
		m_time_remainder += (now - m_frame_end) / 1000.0f;
		if( m_time_remainder < 1.0f / GetStepRate() )
			return false;
		
		if( m_export_every_frame ) OnFileExport();
		return true;

	default:
		PR_ASSERT_STR(PR_DBG_COMMON, false, "Unknown run mode");
		return false;
	}
}

// Return true if the simulation should be advanced in time
bool CControls::AdvanceFrame()
{
	DWORD now = GetTickCount();
	float const MaxFrameTime = 1.0f; // second
	float step_size = 1.0f / GetStepRate();

	switch( m_run_mode )
	{
	case ERunMode_Pause:
		return false;

	case ERunMode_Step:
		m_run_mode = ERunMode_Pause;
		return false;

	case ERunMode_Go:
		if( m_export_every_frame ) OnFileExport();
		if( Testbed().m_state.m_stop_at_frame && Testbed().m_state.m_stop_at_frame_number == m_frame_number )
		{
			m_run_mode = ERunMode_Pause;
			return false;
		}

		// Remove a step size worth of time
		m_time_remainder -= step_size;
		if( m_time_remainder < step_size )
			return false;

		// Handle loops for which the time to step is greater than step_size
		if( (now - m_frame_end) / 200.0f > MaxFrameTime )
		{
			m_time_remainder = pr::Fmod(m_time_remainder, step_size);
			return false;
		}

		// Step another frame
		return true;

	default:
		PR_ASSERT_STR(PR_DBG_COMMON, false, "Unknown run mode");
		return false;
	}
}

// Called when a frame has been completed
void CControls::EndFrame()
{
	m_frame_end = GetTickCount();
	if( m_run_mode != ERunMode_Go )
		m_time_remainder = 0.0f;
}

// Update output values in the control window
void CControls::RefreshControlData()
{
	UpdateData(EUpdate_Read);
	m_ctrl_frame_number	.SetWindowText(FmtS("%d", m_frame_number));
	m_ctrl_frame_rate	.SetWindowText(FmtS("%3.3f", m_frame_rate.m_avr));
	m_ctrl_object_count	.SetWindowText(FmtS("%d", m_object_count));
	m_ctrl_sel_position	.SetWindowText("{0.00 0.00 0.00}");
	m_ctrl_sel_velocity	.SetWindowText("{0.00 0.00 0.00}");
	m_ctrl_sel_ang_vel	.SetWindowText("{0.00 0.00 0.00}");
	m_ctrl_sel_address	.SetWindowText("0x00000000");
}

// Update checked/unchecked items in the menu
void CControls::RefreshMenuState()
{
	GetMenu()->CheckMenuItem(ID_OPTIONS_EXPORTEVERYFRAME, (m_export_every_frame) ? (MF_CHECKED) : (MF_UNCHECKED));
	GetMenu()->CheckMenuItem(ID_OPTIONS_TERRAINSAMPLER, (Testbed().m_state.m_show_terrain_sampler) ? (MF_CHECKED) : (MF_UNCHECKED));
}

// Reset any state
void CControls::Clear()
{
	if( !m_hWnd ) return;

	UpdateData(EUpdate_Read);
	if( m_change_rand_seed )
	{
		m_rand_seed = pr::Rand(0, 65535);
		m_ctrl_rand_seed.SetWindowText(FmtS("%d", m_rand_seed));
	}
	pr::Seed(m_rand_seed);

	m_frame_rate.clear();
}

// Return the step rate in hertz
// This is how frequently we want to step the simulation, NOT how much we
// want to step the simulation by (thats GetStepSize()).
int CControls::GetStepRate() const
{
	return Testbed().m_state.m_step_rate;
}

// Return the amount to advance the simulation by in seconds
float CControls::GetStepSize() const
{
	return 1.0f / Testbed().m_state.m_step_size_inv;
}

// Object count
void CControls::SetObjectCount(std::size_t object_count)
{
	m_object_count = static_cast<unsigned int>(object_count);
}

// Display the current frame rate
void CControls::SetFrameRate(float rate)
{
	m_frame_rate.add(rate);
}

// Update the frame number
void CControls::SetFrameNumber(unsigned int frame_number)
{
	m_frame_number = frame_number;
}

// Enable/Disable showing collision impulses
void CControls::ShowCollisionImpulses(bool yes)
{
	RegisterPstCollisionCB(PCCB_ShowImpulses, yes);
	if( !yes )	{ Testbed().m_scene_manager.ClearImpulses(); }
}

// Enable/Disable showing contact points
void CControls::ShowContactPoints(bool yes)
{
	RegisterPreCollisionCB(PCCB_ShowContacts, yes);
	if( !yes )	{ Testbed().m_scene_manager.ClearContacts(); }
}

// Pause the simulation
void CControls::Pause()
{
	m_run_mode = ERunMode_Pause;
}

// Handle key presses
char const g_key_commands_str[] =
"Key Commands:\n"
"\t'B' : Generate box\n"
"\t'C' : Generate cylinder\n"
"\t'S' : Generate sphere\n"
"\t'P' : Generate polytope\n"
"\t'D' : Generate deformable\n"
"\t'L' : Cast a ray from the camera to the focus point\n"
"\t'K' : Cast a ray from the camera to the focus point, applies an impulse to whatever it hits\n"
"\t'G' : Run the simulation (i.e. 'Go')\n"
"\t'T' : Step/Pause the simulation\n"
"\t'R' : Reset the simulation to time = 0\n"
"\t'H' : This help message\n";
pr::ldr::EPlugInResult CControls::HandleKeys(UINT nChar, UINT, UINT)
{
	switch( nChar )
	{
	case 'B':	Testbed().m_scene_manager.CreateBox();				break;
	case 'C':	Testbed().m_scene_manager.CreateCylinder();			break;
	case 'D':	Testbed().m_scene_manager.CreateDeformableMesh();	break;
	case 'G':	OnBnClickedButtonSimGo();							break;
	case 'H':	OnHelpKeycommands();								break;
	case 'K':	Testbed().m_scene_manager.CastRay(true);			break;
	case 'L':	Testbed().m_scene_manager.CastRay(false);			break;
	case 'P':	Testbed().m_scene_manager.CreatePolytope();			break;
	case VK_F5:
	case 'R':	OnBnClickedButtonSimReset();						break;
	case 'S':	Testbed().m_scene_manager.CreateSphere();			break;
	case 'T':	OnBnClickedButtonSimStep();							break;
	default:	return pr::ldr::EPlugInResult_NotHandled;
	}
	return pr::ldr::EPlugInResult_Handled;
}

void DDX_Check(CDataExchange *pDX, int nIDC, bool& value)	{ BOOL b = value; DDX_Check(pDX, nIDC, b); value = b == TRUE; }
void CControls::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	DDX_Control	(pDX, IDC_EDIT_FRAME_NUM,					m_ctrl_frame_number);
	DDX_Control	(pDX, IDC_EDIT_FRAME_RATE,					m_ctrl_frame_rate);
	DDX_Control	(pDX, IDC_EDIT_OBJECT_COUNT,				m_ctrl_object_count);
	DDX_Control	(pDX, IDC_EDIT_SEL_POSITION,				m_ctrl_sel_position);
	DDX_Control	(pDX, IDC_EDIT_SEL_VELOCITY,				m_ctrl_sel_velocity);
	DDX_Control	(pDX, IDC_EDIT_SEL_ANG_VEL,					m_ctrl_sel_ang_vel);
	DDX_Control	(pDX, IDC_EDIT_PHYS_OBJ_ADDR,				m_ctrl_sel_address);
	DDX_Check	(pDX, IDC_CHECK_SHOW_VELOCITY,				Testbed().m_state.m_show_velocity);
	DDX_Check	(pDX, IDC_CHECK_SHOW_ANG_VELOCITY,			Testbed().m_state.m_show_ang_velocity);
	DDX_Check	(pDX, IDC_CHECK_SHOW_ANG_MOMENTUM,			Testbed().m_state.m_show_ang_momentum);
	DDX_Check	(pDX, IDC_CHECK_SHOW_WS_BBOX,				Testbed().m_state.m_show_ws_bounding_boxes);
	DDX_Check	(pDX, IDC_CHECK_SHOW_OS_BBOX,				Testbed().m_state.m_show_os_bounding_boxes);
	DDX_Check	(pDX, IDC_CHECK_SHOW_COM,					Testbed().m_state.m_show_centre_of_mass);
	DDX_Check	(pDX, IDC_CHECK_SHOW_CONTACTS,				Testbed().m_state.m_show_contact_points);
	DDX_Check	(pDX, IDC_CHECK_SHOW_COLLISION_IMPULSES,	Testbed().m_state.m_show_collision_impulses);
	DDX_Check	(pDX, IDC_CHECK_SHOW_SLEEPING,				Testbed().m_state.m_show_sleeping);
	DDX_Check	(pDX, IDC_CHECK_SHOW_INERTIA,				Testbed().m_state.m_show_inertia);
	DDX_Check	(pDX, IDC_CHECK_SHOW_RESTING_CONTACTS,		Testbed().m_state.m_show_resting_contacts);
	DDX_Slider	(pDX, IDC_SLIDER_COL_IMP_SCALE,				Testbed().m_state.m_scale);
	DDX_Control	(pDX, IDC_EDIT_RANDOM_SEED,					m_ctrl_rand_seed);	
	DDX_Check	(pDX, IDC_CHECK_CHANGE_RANDSEED,			m_change_rand_seed);
	DDX_Control	(pDX, IDC_EDIT_STEP_SIZE,					m_ctrl_step_size);
	DDX_Control	(pDX, IDC_EDIT_STEP_RATE,					m_ctrl_step_rate);
	DDX_Control	(pDX, IDC_SLIDER_STEP_RATE,					m_ctrl_step_rate_slider);
	DDX_Check	(pDX, IDC_CHECK_STOP_OBJ_VS_TERRAIN,		m_stop_on_obj_vs_terrain);
	DDX_Check	(pDX, IDC_CHECK_STOP_OBJ_VS_OBJ,			m_stop_on_obj_vs_obj);
	DDX_Check	(pDX, IDC_CHECK_STOP_AT_FRAME,				Testbed().m_state.m_stop_at_frame);
	DDX_Control	(pDX, IDC_EDIT_STOP_AT_FRAME,				m_ctrl_stop_at_frame);
}
BEGIN_MESSAGE_MAP(CControls, CDialog)
	ON_WM_DESTROY()
	ON_WM_CLOSE()
	ON_WM_KEYDOWN()
	ON_COMMAND(ID_FILE_EXIT,							OnFileExit)
	ON_COMMAND(ID_FILE_OPEN32771,						OnFileOpen)
	ON_COMMAND(ID_FILE_EXPORT,							OnFileExport)
	ON_COMMAND(ID_FILE_EXPORTAS,						OnFileExportAs)
	ON_COMMAND(ID_OPTIONS_SHAPEGENERATION,				OnOptionsShapegeneration)
	ON_COMMAND(ID_OPTIONS_EXPORTEVERYFRAME,				OnOptionsExportEveryFrame)
	ON_COMMAND(ID_OPTIONS_TERRAINSAMPLER,				OnOptionsTerrainSampler)
	ON_COMMAND(ID_HELP_KEYCOMMANDS,						OnHelpKeycommands)
	ON_BN_CLICKED(IDC_CHECK_SHOW_VELOCITY,				OnBnClickedCheckViewStateChange)
	ON_BN_CLICKED(IDC_CHECK_SHOW_ANG_VELOCITY,			OnBnClickedCheckViewStateChange)
	ON_BN_CLICKED(IDC_CHECK_SHOW_ANG_MOMENTUM,			OnBnClickedCheckViewStateChange)
	ON_BN_CLICKED(IDC_CHECK_SHOW_WS_BBOX,				OnBnClickedCheckViewStateChange)
	ON_BN_CLICKED(IDC_CHECK_SHOW_OS_BBOX,				OnBnClickedCheckViewStateChange)
	ON_BN_CLICKED(IDC_CHECK_SHOW_COM,					OnBnClickedCheckViewStateChange)
	ON_BN_CLICKED(IDC_CHECK_SHOW_SLEEPING,				OnBnClickedCheckViewStateChange)
	ON_BN_CLICKED(IDC_CHECK_SHOW_INERTIA,				OnBnClickedCheckViewStateChange)
	ON_BN_CLICKED(IDC_CHECK_SHOW_RESTING_CONTACTS,		OnBnClickedCheckViewStateChange)
	ON_BN_CLICKED(IDC_CHECK_SHOW_CONTACTS,				OnBnClickedCheckShowContacts)
	ON_BN_CLICKED(IDC_CHECK_SHOW_COLLISION_IMPULSES,	OnBnClickedCheckShowCollisionImpulses)
	ON_BN_CLICKED(IDC_CHECK_STOP_AT_FRAME,				OnBnClickedCheckStopAtFrame)
	ON_BN_CLICKED(IDC_CHECK_STOP_OBJ_VS_TERRAIN,		OnBnClickedCheckStopObjVsTerrain)
	ON_BN_CLICKED(IDC_CHECK_STOP_OBJ_VS_OBJ,			OnBnClickedCheckStopObjVsObj)
	ON_BN_CLICKED(IDC_BUTTON_SIM_RESET,					OnBnClickedButtonSimReset)
	ON_BN_CLICKED(IDC_BUTTON_SIM_GO,					OnBnClickedButtonSimGo)
	ON_BN_CLICKED(IDC_BUTTON_SIM_PAUSE,					OnBnClickedButtonSimPause)
	ON_BN_CLICKED(IDC_BUTTON_SIM_STEP,					OnBnClickedButtonSimStep)
	ON_EN_CHANGE(IDC_EDIT_RANDOM_SEED,					OnEnChangeEditRandSeed)
	ON_EN_CHANGE(IDC_EDIT_STEP_SIZE,					OnEnChangeEditStepSize)
	ON_EN_CHANGE(IDC_EDIT_STEP_RATE,					OnEnChangeEditStepRate)
	ON_WM_HSCROLL()
	ON_EN_CHANGE(IDC_EDIT_STOP_AT_FRAME,				OnEnChangeEditStopAtFrame)
END_MESSAGE_MAP()

// CControls message handlers

// Exit the plugin
void CControls::OnClose()
{
	Testbed().Shutdown();
}

// Handle key presses
void CControls::OnKeyDown(UINT nChar, UINT nRepCnt, UINT nFlags)
{
	HandleKeys(nChar, nRepCnt, nFlags);
	CDialog::OnKeyDown(nChar, nRepCnt, nFlags);
}

// Open a source physics scene file
void CControls::OnFileOpen()
{
	CFileDialog filedlg(TRUE);
	filedlg.GetOFN().lpstrTitle = "Open a script file";
	INT_PTR result = filedlg.DoModal();
	if( result != IDOK ) return;

	Testbed().LoadSourceFile(filedlg.GetPathName());
}

// Export the scene to a file
void CControls::OnFileExport()
{
	if( m_export_filename.empty() ) return OnFileExportAs();
	Testbed().m_scene_manager.ExportScene(m_export_filename.c_str(), m_export_as_physics_scene);
}
void CControls::OnFileExportAs()
{
	CFileDialog filedlg(FALSE);
	filedlg.GetOFN().lpstrTitle  = "Save to script file";
	filedlg.GetOFN().lpstrFilter = "Physics Scene (*.pr_script)\0*.pr_script;\0Linedrawer Scene (*.pr_script)\0*.pr_script;\0\0";
	INT_PTR result = filedlg.DoModal();
	if( result != IDOK ) return;

	m_export_filename = filedlg.GetPathName();
	m_export_as_physics_scene = filedlg.GetOFN().nFilterIndex == 0;
	Testbed().m_scene_manager.ExportScene(m_export_filename.c_str(), m_export_as_physics_scene);
}

// Exit the plugin
void CControls::OnFileExit()
{
	Testbed().Shutdown();
}

// Display options for the shape generation parameters
void CControls::OnOptionsShapegeneration()
{
	CShapeGenParamsDlg shape_gen;
	INT_PTR result = shape_gen.DoModal();
	if( result != IDOK ) return;
	ShapeGen() = shape_gen.m_params;
	Testbed().m_state.Save();
}

// Display options for the shape generation parameters
void CControls::OnOptionsExportEveryFrame()
{
	m_export_every_frame = !m_export_every_frame;
	RefreshMenuState();
}

// Enable the terrain sampler
void CControls::OnOptionsTerrainSampler()
{
	Testbed().m_state.m_show_terrain_sampler = !Testbed().m_state.m_show_terrain_sampler;
	RefreshMenuState();
}

// Display a message box containing the key commands
void CControls::OnHelpKeycommands()
{
	MessageBox(g_key_commands_str, "Key Command Help", MB_OK);
}

// Display graphics for the props
void CControls::OnBnClickedCheckViewStateChange()
{
	UpdateData(EUpdate_Read);
	Testbed().m_scene_manager.ViewStateUpdate();
}

// Enable/Disable showing contact points
void CControls::OnBnClickedCheckShowContacts()
{
	UpdateData(EUpdate_Read);
	ShowContactPoints(Testbed().m_state.m_show_contact_points == TRUE);
}

// Enable/Disable show collision impulses
void CControls::OnBnClickedCheckShowCollisionImpulses()
{
	UpdateData(EUpdate_Read);
	ShowCollisionImpulses(Testbed().m_state.m_show_collision_impulses == TRUE);
}

// Enable the obj vs. terrain collision watch
void CControls::OnBnClickedCheckStopObjVsTerrain()
{
	UpdateData(EUpdate_Read);
	RegisterPreCollisionCB(PCCB_StopOnObjVsTerrain, m_stop_on_obj_vs_terrain != 0);
}

// Enable the obj vs obj collision watch
void CControls::OnBnClickedCheckStopObjVsObj()
{
	UpdateData(EUpdate_Read);
	RegisterPreCollisionCB(PCCB_StopOnObjVsObj, m_stop_on_obj_vs_obj != 0);
}

// Enable the stop at frame edit box
void CControls::OnBnClickedCheckStopAtFrame()
{
	UpdateData(EUpdate_Read);
	m_ctrl_stop_at_frame.EnableWindow(Testbed().m_state.m_stop_at_frame);
}

// Reset the simulation
void CControls::OnBnClickedButtonSimReset()
{
	Testbed().Reload();
	m_run_mode = ERunMode_Pause;
	m_ctrl_step_size.EnableWindow(m_run_mode != ERunMode_Go);
	m_ctrl_step_rate.EnableWindow(m_run_mode != ERunMode_Go);
}
// Run the simulation
void CControls::OnBnClickedButtonSimGo()
{
	// Save the testbed state on initial runs of the simulation
	if( m_frame_number == 0 )
		Testbed().m_state.Save();

	if( m_run_mode == ERunMode_Go )		m_run_mode = ERunMode_Pause;
	else								m_run_mode = ERunMode_Go;
	m_ctrl_step_size.EnableWindow(m_run_mode != ERunMode_Go);
	m_ctrl_step_rate.EnableWindow(m_run_mode != ERunMode_Go);
}

// Pause the simulation
void CControls::OnBnClickedButtonSimPause()
{
	m_run_mode = ERunMode_Pause;
	m_ctrl_step_size.EnableWindow(m_run_mode != ERunMode_Go);
	m_ctrl_step_rate.EnableWindow(m_run_mode != ERunMode_Go);
}

// Advance the simulation one frame at a time
void CControls::OnBnClickedButtonSimStep()
{
	// Save the testbed state on initial runs of the simulation
	if( m_frame_number == 0 )
		Testbed().m_state.Save();

	m_run_mode = ERunMode_Step;
	m_ctrl_step_size.EnableWindow(m_run_mode != ERunMode_Go);
	m_ctrl_step_rate.EnableWindow(m_run_mode != ERunMode_Go);
}

// The random seed has been changed
void CControls::OnEnChangeEditRandSeed()
{
	CString str; m_ctrl_rand_seed.GetWindowText(str);
	m_rand_seed = static_cast<unsigned int>(strtoul(str.GetString(), 0, 10));
}

// The step size has changed
void CControls::OnEnChangeEditStepSize()
{
	CString str; m_ctrl_step_size.GetWindowText(str);
	int step_size_inv = static_cast<int>(strtol(str.GetString(), 0, 10));
	if( step_size_inv != 0 )
	{
		Testbed().m_state.m_step_size_inv = step_size_inv;
	}
}

// The step rate has changed
void CControls::OnEnChangeEditStepRate()
{
	CString str; m_ctrl_step_rate.GetWindowText(str);
	int step_rate = static_cast<int>(strtol(str.GetString(), 0, 10));
	if( step_rate != 0 && step_rate != Testbed().m_state.m_step_rate )
	{
		Testbed().m_state.m_step_rate = Clamp(step_rate, m_ctrl_step_rate_slider.GetRangeMin(), m_ctrl_step_rate_slider.GetRangeMax());
		m_ctrl_step_rate_slider.SetPos(Testbed().m_state.m_step_rate);
		if( Testbed().m_state.m_step_rate != step_rate )
		{
			m_ctrl_step_rate.SetWindowText(FmtS("%d", GetStepRate()));
			m_ctrl_step_rate.SetSel(0, -1);
		}
	}
}

// The step rate slider has been moved
void CControls::OnHScroll(UINT, UINT, CScrollBar* pScrollBar)
{
	if( (CSliderCtrl*)pScrollBar != &m_ctrl_step_rate_slider )
		return;

	m_ctrl_step_rate.SetWindowText(FmtS("%d", m_ctrl_step_rate_slider.GetPos()));
}

// The frame number to stop at has been changed
void CControls::OnEnChangeEditStopAtFrame()
{
	if( !Testbed().m_state.m_stop_at_frame )
		return;

	CString str; m_ctrl_stop_at_frame.GetWindowText(str);
	Testbed().m_state.m_stop_at_frame_number = static_cast<unsigned int>(strtoul(str.GetString(), 0, 10));
}


