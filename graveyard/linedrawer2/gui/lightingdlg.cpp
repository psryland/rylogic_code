// LightingDlg.cpp : implementation file
//

#include "Stdafx.h"
#include "LineDrawer/Resource.h"
#include "LineDrawer/Source/LineDrawer.h"
#include "LineDrawer/GUI/LightingDlg.h"

BEGIN_MESSAGE_MAP(LightingDlg, CDialog)
	ON_BN_CLICKED(IDC_RADIO_AMBIENT,		OnBnClickedRadioAmbient)
	ON_BN_CLICKED(IDC_RADIO_POINT,			OnBnClickedRadioPoint)
	ON_BN_CLICKED(IDC_RADIO_SPOT,			OnBnClickedRadioSpot)
	ON_BN_CLICKED(IDC_RADIO_DIRECTIONAL,	OnBnClickedRadioDirectional)
	ON_BN_CLICKED(IDC_BUTTON_APPLY,			OnBnClickedButtonApply)
END_MESSAGE_MAP()

//*****
// LightingDlg dialog
IMPLEMENT_DYNAMIC(LightingDlg, CDialog)
LightingDlg::LightingDlg(CWnd* pParent)
:CDialog(LightingDlg::IDD, pParent)
,m_camera_relative(FALSE)
{}

LightingDlg::~LightingDlg()
{}

void LightingDlg::DoDataExchange(CDataExchange* pDX)
{
	BOOL ambient		= m_light.m_type == pr::rdr::ELight::Ambient;
	BOOL point			= m_light.m_type == pr::rdr::ELight::Point;
	BOOL spot			= m_light.m_type == pr::rdr::ELight::Spot;
	BOOL directional	= m_light.m_type == pr::rdr::ELight::Directional;
	float inner			= pr::ACos(m_light.m_inner_cos_angle) * 180.0f / maths::pi;
	float outer			= pr::ACos(m_light.m_outer_cos_angle) * 180.0f / maths::pi;

	CDialog::DoDataExchange(pDX);
	DDX_Check(pDX,	IDC_RADIO_AMBIENT,					ambient);
	DDX_Check(pDX,	IDC_RADIO_POINT,					point);
	DDX_Check(pDX,	IDC_RADIO_SPOT,						spot);
	DDX_Check(pDX,	IDC_RADIO_DIRECTIONAL,				directional);
	DDX_Text(pDX,	IDC_LIGHTING_AMBIENT_RED,			m_light.m_ambient.r);
	DDX_Text(pDX,	IDC_LIGHTING_AMBIENT_GREEN,			m_light.m_ambient.g);
	DDX_Text(pDX,	IDC_LIGHTING_AMBIENT_BLUE,			m_light.m_ambient.b);
	DDX_Text(pDX,	IDC_LIGHTING_DIFFUSE_RED,			m_light.m_diffuse.r);
	DDX_Text(pDX,	IDC_LIGHTING_DIFFUSE_GREEN,			m_light.m_diffuse.g);
	DDX_Text(pDX,	IDC_LIGHTING_DIFFUSE_BLUE,			m_light.m_diffuse.b);
	DDX_Text(pDX,	IDC_LIGHTING_SPECULAR_RED,			m_light.m_specular.r);
	DDX_Text(pDX,	IDC_LIGHTING_SPECULAR_GREEN,		m_light.m_specular.g);
	DDX_Text(pDX,	IDC_LIGHTING_SPECULAR_BLUE,			m_light.m_specular.b);
	DDX_Text(pDX,	IDC_LIGHTING_SPECULAR_POWER,		m_light.m_specular_power);
	DDX_Check(pDX,	IDC_CHECK_CameraRelative,			m_camera_relative);
	DDX_Text(pDX,	IDC_LIGHTING_POSITION_X,			m_light.m_position[0]);
	DDX_Text(pDX,	IDC_LIGHTING_POSITION_Y,			m_light.m_position[1]);
	DDX_Text(pDX,	IDC_LIGHTING_POSITION_Z,			m_light.m_position[2]);
	DDX_Text(pDX,	IDC_LIGHTING_DIRECTION_X,			m_light.m_direction[0]);
	DDX_Text(pDX,	IDC_LIGHTING_DIRECTION_Y,			m_light.m_direction[1]);
	DDX_Text(pDX,	IDC_LIGHTING_DIRECTION_Z,			m_light.m_direction[2]);
	DDX_Text(pDX,	IDC_LIGHTING_INNER_SOLID_ANGLE,		inner);
	DDX_Text(pDX,	IDC_LIGHTING_OUTER_SOLID_ANGLE,		outer);
	DDX_Text(pDX,	IDC_LIGHTING_RANGE,					m_light.m_range);
	DDX_Text(pDX,	IDC_LIGHTING_FALLOFF,				m_light.m_falloff);
	DDX_Text(pDX,	IDC_LIGHTING_ATTEN0,				m_light.m_attenuation0);
	DDX_Text(pDX,	IDC_LIGHTING_ATTEN1,				m_light.m_attenuation1);
	DDX_Text(pDX,	IDC_LIGHTING_ATTEN2,				m_light.m_attenuation2);

	if     ( ambient     )	m_light.m_type = pr::rdr::ELight::Ambient;
	else if( point       )	m_light.m_type = pr::rdr::ELight::Point;
	else if( spot        )	m_light.m_type = pr::rdr::ELight::Spot;
	else if( directional )	m_light.m_type = pr::rdr::ELight::Directional;
	Normalise3(m_light.m_direction);
	m_light.m_outer_cos_angle = pr::Clamp<float>(pr::Cos(outer * maths::pi / 180.0f), 0.0f, 1.0f);
	m_light.m_inner_cos_angle = pr::Clamp<float>(pr::Cos(inner * maths::pi / 180.0f), 0.0f, 1.0f);

	if( FEql(m_light.m_attenuation0, 0.0f) && FEql(m_light.m_attenuation1, 0.0f) && FEql(m_light.m_attenuation2, 0.0f) )
		m_light.m_attenuation0 = 1.0f;
}

//*****
// Initialise the dialog
BOOL LightingDlg::OnInitDialog()
{
	if( CDialog::OnInitDialog() )
	{
        EnableWhatsActive();
		return true;
	}
	return false;
}

//*****
// Enable parts of the dialog based on what lights are selected
void LightingDlg::EnableWhatsActive()
{
	UpdateData(TRUE);
	CWnd* item;
	bool visible;
	rdr::ELight::Type type = m_light.m_type;

	visible = type == rdr::ELight::Point || type == rdr::ELight::Spot || type == rdr::ELight::Directional;
	item = GetDlgItem(IDC_LIGHTING_DIFFUSE_RED);		item->EnableWindow(visible);
	item = GetDlgItem(IDC_LIGHTING_DIFFUSE_GREEN);		item->EnableWindow(visible);
	item = GetDlgItem(IDC_LIGHTING_DIFFUSE_BLUE);		item->EnableWindow(visible);
	
	item = GetDlgItem(IDC_LIGHTING_SPECULAR_RED);		item->EnableWindow(visible);
	item = GetDlgItem(IDC_LIGHTING_SPECULAR_GREEN);		item->EnableWindow(visible);
	item = GetDlgItem(IDC_LIGHTING_SPECULAR_BLUE);		item->EnableWindow(visible);

	item = GetDlgItem(IDC_LIGHTING_SPECULAR_POWER);		item->EnableWindow(visible);

	visible = type == rdr::ELight::Point || type == rdr::ELight::Spot;
	item = GetDlgItem(IDC_LIGHTING_POSITION_X);			item->EnableWindow(visible);
	item = GetDlgItem(IDC_LIGHTING_POSITION_Y);			item->EnableWindow(visible);
	item = GetDlgItem(IDC_LIGHTING_POSITION_Z);			item->EnableWindow(visible);

	visible = type == rdr::ELight::Directional || type == rdr::ELight::Spot;
	item = GetDlgItem(IDC_LIGHTING_DIRECTION_X);		item->EnableWindow(visible);
	item = GetDlgItem(IDC_LIGHTING_DIRECTION_Y);		item->EnableWindow(visible);
	item = GetDlgItem(IDC_LIGHTING_DIRECTION_Z);		item->EnableWindow(visible);

	visible = type == rdr::ELight::Spot;
	item = GetDlgItem(IDC_LIGHTING_INNER_SOLID_ANGLE);	item->EnableWindow(visible);
	item = GetDlgItem(IDC_LIGHTING_OUTER_SOLID_ANGLE);	item->EnableWindow(visible);
	item = GetDlgItem(IDC_LIGHTING_FALLOFF);			item->EnableWindow(visible);
	
	visible = type == rdr::ELight::Point || type == rdr::ELight::Spot;
	item = GetDlgItem(IDC_LIGHTING_RANGE);				item->EnableWindow(visible);
	item = GetDlgItem(IDC_LIGHTING_ATTEN0);				item->EnableWindow(visible);
	item = GetDlgItem(IDC_LIGHTING_ATTEN1);				item->EnableWindow(visible);
	item = GetDlgItem(IDC_LIGHTING_ATTEN2);				item->EnableWindow(visible);
}

void LightingDlg::OnBnClickedButtonApply()
{
	UpdateData(TRUE);
	LineDrawer::Get().SetLight(m_light, m_camera_relative == TRUE);
	LineDrawer::Get().Refresh();
	UpdateData(FALSE);
}
