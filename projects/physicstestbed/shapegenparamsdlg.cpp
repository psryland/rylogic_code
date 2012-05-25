//*******************************
// Shape Generator Params
//*******************************
#include "PhysicsTestbed/Stdafx.h"
#include "PhysicsTestbed/ShapeGenParamsDlg.h"

// CShapeGenParamsDlg dialog
IMPLEMENT_DYNAMIC(CShapeGenParamsDlg, CDialog)
CShapeGenParamsDlg::CShapeGenParamsDlg(CWnd* pParent)
:CDialog(CShapeGenParamsDlg::IDD, pParent)
,m_params(ShapeGen())
{}

CShapeGenParamsDlg::~CShapeGenParamsDlg()
{}

void CShapeGenParamsDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	DDX_Text(pDX, IDC_EDIT_SHAPE_GEN_SPH_MIN_RADIUS,	m_params.m_sph_min_radius);
	DDX_Text(pDX, IDC_EDIT_SHAPE_GEN_SPH_MAX_RADIUS,	m_params.m_sph_max_radius);

	DDX_Text(pDX, IDC_EDIT_SHAPE_GEN_CYL_MIN_RADIUS,	m_params.m_cyl_min_radius);
	DDX_Text(pDX, IDC_EDIT_SHAPE_GEN_CYL_MAX_RADIUS,	m_params.m_cyl_max_radius);
	DDX_Text(pDX, IDC_EDIT_SHAPE_GEN_CYL_MIN_HEIGHT,	m_params.m_cyl_min_height);
	DDX_Text(pDX, IDC_EDIT_SHAPE_GEN_CYL_MAX_HEIGHT,	m_params.m_cyl_max_height);

	DDX_Text(pDX, IDC_EDIT_SHAPE_GEN_BOX_MIN_DIMX,		m_params.m_box_min_dim.x);
	DDX_Text(pDX, IDC_EDIT_SHAPE_GEN_BOX_MIN_DIMY,		m_params.m_box_min_dim.y);
	DDX_Text(pDX, IDC_EDIT_SHAPE_GEN_BOX_MIN_DIMZ,		m_params.m_box_min_dim.z);
	DDX_Text(pDX, IDC_EDIT_SHAPE_GEN_BOX_MAX_DIMX,		m_params.m_box_max_dim.x);
	DDX_Text(pDX, IDC_EDIT_SHAPE_GEN_BOX_MAX_DIMY,		m_params.m_box_max_dim.y);
	DDX_Text(pDX, IDC_EDIT_SHAPE_GEN_BOX_MAX_DIMZ,		m_params.m_box_max_dim.z);

	DDX_Text(pDX, IDC_EDIT_SHAPE_GEN_PLY_VCOUNT,		m_params.m_ply_vert_count);
	DDX_Text(pDX, IDC_EDIT_SHAPE_GEN_PLY_MIN_DIMX,		m_params.m_ply_min_dim.x);
	DDX_Text(pDX, IDC_EDIT_SHAPE_GEN_PLY_MIN_DIMY,		m_params.m_ply_min_dim.y);
	DDX_Text(pDX, IDC_EDIT_SHAPE_GEN_PLY_MIN_DIMZ,		m_params.m_ply_min_dim.z);
	DDX_Text(pDX, IDC_EDIT_SHAPE_GEN_PLY_MAX_DIMX,		m_params.m_ply_max_dim.x);
	DDX_Text(pDX, IDC_EDIT_SHAPE_GEN_PLY_MAX_DIMY,		m_params.m_ply_max_dim.y);
	DDX_Text(pDX, IDC_EDIT_SHAPE_GEN_PLY_MAX_DIMZ,		m_params.m_ply_max_dim.z);
}

BEGIN_MESSAGE_MAP(CShapeGenParamsDlg, CDialog)
END_MESSAGE_MAP()

// CShapeGenParamsDlg message handlers
