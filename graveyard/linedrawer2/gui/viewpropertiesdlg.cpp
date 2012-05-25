//*****************************************************************************
//
// View Properties dialog
//
//*****************************************************************************
#include "Stdafx.h"
#include "pr/common/Clipboard.h"
#include "LineDrawer/Source/LineDrawer.h"
#include "LineDrawer/GUI/ViewPropertiesDlg.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

//*****
// ViewPropertiesDlg dialog constructor
ViewPropertiesDlg::ViewPropertiesDlg(CWnd* pParent)
:CDialog(ViewPropertiesDlg::IDD, pParent)
,m_camera_to_world(m4x4Identity)
,m_focus_point(v4Origin)
,m_cull_mode(0)
,m_near_clip_plane(0)
,m_far_clip_plane(0)
{}

void ViewPropertiesDlg::DoDataExchange(CDataExchange* pDX)
{
	CString focus_pos;		focus_pos	.Format("{%3.3f %3.3f %3.3f}", m_focus_point.x, m_focus_point.y, m_focus_point.z);
	CString cam_pos;		cam_pos		.Format("{%3.3f %3.3f %3.3f}", m_camera_to_world.pos.x, m_camera_to_world.pos.y, m_camera_to_world.pos.z);
	CString cam_left;		cam_left	.Format("{%3.3f %3.3f %3.3f}", m_camera_to_world.x.x, m_camera_to_world.x.y, m_camera_to_world.x.z);
	CString cam_up;			cam_up		.Format("{%3.3f %3.3f %3.3f}", m_camera_to_world.y.x, m_camera_to_world.y.y, m_camera_to_world.y.z);
	CString cam_forward;	cam_forward	.Format("{%3.3f %3.3f %3.3f}", m_camera_to_world.z.x, m_camera_to_world.z.y, m_camera_to_world.z.z);
	
	CDialog::DoDataExchange(pDX);
	DDX_Text(pDX, IDC_EDIT_FOCUS_POSITION,	focus_pos);
	DDX_Text(pDX, IDC_EDIT_CAM_POSITION,	cam_pos);
	DDX_Text(pDX, IDC_EDIT_CAM_LEFT,		cam_left);
	DDX_Text(pDX, IDC_EDIT_CAM_UP,			cam_up);
	DDX_Text(pDX, IDC_EDIT_CAM_FORWARD,		cam_forward);
	DDX_Text(pDX, IDC_EDIT_NEAR_CLIP_PLANE,	m_near_clip_plane);
	DDX_Text(pDX, IDC_EDIT_FAR_CLIP_PLANE,	m_far_clip_plane);
	DDX_CBIndex(pDX, IDC_COMBO_CULLMODE,	m_cull_mode);

	bool success = true;
	m4x4 c2w;
	v4 focus;
	success &= sscanf(focus_pos  .GetString(), "{%3.3f %3.3f %3.3f}", &focus.x, &focus.y, &focus.z) == 3; focus.w = 1.0f;
	success &= sscanf(cam_pos    .GetString(), "{%3.3f %3.3f %3.3f}", &c2w.pos.x, &c2w.pos.y, &c2w.pos.z) == 3; c2w.pos.w = 1.0f;
	success &= sscanf(cam_left   .GetString(), "{%3.3f %3.3f %3.3f}", &c2w.x.x, &c2w.x.y, &c2w.x.z) == 3; c2w.x.w = 0.0f;
	success &= sscanf(cam_up     .GetString(), "{%3.3f %3.3f %3.3f}", &c2w.y.x, &c2w.y.y, &c2w.y.z) == 3; c2w.y.w = 0.0f;
	success &= sscanf(cam_forward.GetString(), "{%3.3f %3.3f %3.3f}", &c2w.z.x, &c2w.z.y, &c2w.z.z) == 3; c2w.z.w = 0.0f;
	if( success )
	{
		m_camera_to_world = c2w;
		m_focus_point = focus;
	}
}
BEGIN_MESSAGE_MAP(ViewPropertiesDlg, CDialog)
	ON_BN_CLICKED(IDC_BUTTON_COPY_CAM_XFORM, &ViewPropertiesDlg::OnBnClickedButtonCopyCamXform)
END_MESSAGE_MAP()

void ViewPropertiesDlg::OnBnClickedButtonCopyCamXform()
{
	m4x4 const& c2w = m_camera_to_world;
	pr::SetClipBoardText(m_hWnd,
		pr::Fmt(
		"{%3.3f %3.3f %3.3f %3.3f  %3.3f %3.3f %3.3f %3.3f  %3.3f %3.3f %3.3f %3.3f  %3.3f %3.3f %3.3f %3.3f}"
		,c2w.x.x ,c2w.x.y ,c2w.x.z ,c2w.x.w
		,c2w.y.x ,c2w.y.y ,c2w.y.z ,c2w.y.w
		,c2w.z.x ,c2w.z.y ,c2w.z.z ,c2w.z.w
		,c2w.w.x ,c2w.w.y ,c2w.w.z ,c2w.w.w
		));
}
