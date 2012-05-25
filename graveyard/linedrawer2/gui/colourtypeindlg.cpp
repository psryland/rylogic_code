//*******************************************************************************************
//
//	Colour Typein
//
//*******************************************************************************************
#include "Stdafx.h"
#include "ColourTypeinDlg.h"
#include ".\colourtypeindlg.h"

IMPLEMENT_DYNAMIC(ColourTypein, CDialog)
ColourTypein::ColourTypein(CWnd* pParent)
:CDialog(ColourTypein::IDD, pParent)
,m_colour(ColourZero)
{}

ColourTypein::~ColourTypein()
{}

void ColourTypein::DoDataExchange(CDataExchange* pDX)
{
	m_colour.a = Clamp(m_colour.a, 0.0f, 1.0f);
	m_colour.r = Clamp(m_colour.r, 0.0f, 1.0f);
	m_colour.g = Clamp(m_colour.g, 0.0f, 1.0f);
	m_colour.b = Clamp(m_colour.b, 0.0f, 1.0f);

	CDialog::DoDataExchange(pDX);
	DDX_Text(pDX,	IDC_COLOUR_TYPEIN_AA,	m_colour.a);
	DDX_Text(pDX,	IDC_COLOUR_TYPEIN_RR,	m_colour.r);
	DDX_Text(pDX,	IDC_COLOUR_TYPEIN_GG,	m_colour.g);
	DDX_Text(pDX,	IDC_COLOUR_TYPEIN_BB,	m_colour.b);

	m_colour.a = Clamp(m_colour.a, 0.0f, 1.0f);
	m_colour.r = Clamp(m_colour.r, 0.0f, 1.0f);
	m_colour.g = Clamp(m_colour.g, 0.0f, 1.0f);
	m_colour.b = Clamp(m_colour.b, 0.0f, 1.0f);
}

void ColourTypein::SetColour32(Colour32 col)
{
	m_colour = col;
}

Colour32 ColourTypein::GetColour32() const
{
	return m_colour.argb();
}

BEGIN_MESSAGE_MAP(ColourTypein, CDialog)
	ON_WM_PAINT()
	ON_EN_CHANGE(IDC_COLOUR_TYPEIN_AA, OnEnChangeColourTypein)
	ON_EN_CHANGE(IDC_COLOUR_TYPEIN_RR, OnEnChangeColourTypein)
	ON_EN_CHANGE(IDC_COLOUR_TYPEIN_GG, OnEnChangeColourTypein)
	ON_EN_CHANGE(IDC_COLOUR_TYPEIN_BB, OnEnChangeColourTypein)
END_MESSAGE_MAP()

// ColourTypein message handlers

void ColourTypein::OnPaint()
{
	CWnd* wnd = GetDlgItem(IDC_COLOUR_TYPEIN_COLOUR_INDICATOR);
	if( wnd )
	{
		CRect rect;
		wnd->GetClientRect(&rect);
		CDC* dc = wnd->GetDC();
		dc->FillSolidRect(&rect, m_colour.GetColorRef());
		wnd->ReleaseDC(dc);
	}
}

void ColourTypein::OnEnChangeColourTypein()
{
	UpdateData(TRUE);
}
