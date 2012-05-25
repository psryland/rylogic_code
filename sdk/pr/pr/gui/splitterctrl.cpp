//*************************************************************************
//
// SplitterCtrl
//
//*************************************************************************

#include "pr/common/assert.h"
#include "SplitterCtrl.h"

using namespace pr;

BEGIN_MESSAGE_MAP(SplitterCtrl, CWnd)
	ON_WM_CREATE()
	ON_WM_LBUTTONDOWN()
	ON_WM_MOUSEMOVE()
	ON_WM_LBUTTONUP()
END_MESSAGE_MAP()


const char wnd_class_name[] = "PRSplitterCtrl";

SplitterCtrl::SplitterCtrl()
:m_settings()
,m_min(0)
,m_max(0)
,m_dragging(false)
{
	WNDCLASS windowclass;
    HINSTANCE hInst = AfxGetInstanceHandle();

    // Check weather the class is registerd already
    if( !(::GetClassInfo(hInst, wnd_class_name, &windowclass)) )
    {
        // If not then we have to register the new class
        windowclass.style			= 0;//CS_DBLCLKS;// | CS_HREDRAW | CS_VREDRAW;
        windowclass.lpfnWndProc		= ::DefWindowProc;
        windowclass.cbClsExtra		= 0;
		windowclass.cbWndExtra		= 0;
        windowclass.hInstance		= hInst;
        windowclass.hIcon			= 0;
        windowclass.hCursor			= 0;
        windowclass.hbrBackground	= 0;
        windowclass.lpszMenuName	= 0;
        windowclass.lpszClassName	= wnd_class_name;
        if( !AfxRegisterClass(&windowclass) )
        {
            AfxThrowResourceException();
        }
    }
}

SplitterCtrl::~SplitterCtrl()
{}

// Initialise the splitter
void SplitterCtrl::Initialise(const Settings& settings)
{
	PR_ASSERT(PR_DBG, settings.m_parent && settings.m_side1 && settings.m_side2, "");
	m_settings = settings;
	ResetMinMaxRange();
}

//*****
// Reset the values for min/max.
void SplitterCtrl::ResetMinMaxRange()
{
	if( m_hWnd == 0 ) return;

	CRect rect;

	m_settings.m_side1->GetWindowRect(&rect);
	CPoint topleft = rect.TopLeft();
	m_settings.m_parent->ScreenToClient(&topleft);
	m_min = (m_settings.m_type == Horizontal) ? (topleft.y) : (topleft.x);

	m_settings.m_side2->GetWindowRect(&rect);
	CPoint bottomright = rect.BottomRight();
	m_settings.m_parent->ScreenToClient(&bottomright);
	m_max = (m_settings.m_type == Horizontal) ? (bottomright.y) : (bottomright.x);
}

//*****
// Return the fraction between m_min and m_max
float SplitterCtrl::GetSplitFraction() const
{
	if( m_hWnd == 0 ) return 0.0f;

	CRect rect;
	GetWindowRect(&rect);
	CPoint centre = rect.CenterPoint();
	m_settings.m_parent->ScreenToClient(&centre);

	switch( m_settings.m_type )
	{
	case Horizontal:	return static_cast<float>(centre.y - m_min) / static_cast<float>(m_max - m_min);
	case Vertical:		return static_cast<float>(centre.x - m_min) / static_cast<float>(m_max - m_min);
	default: PR_ASSERT(PR_DBG, false, "Unknown splitter type"); return 0.0f;
	}
}

//*****
// Set the split point
void SplitterCtrl::SetSplitFraction(float split)
{
	if( m_hWnd == 0 ) return;

	CRect window_rect;
	GetWindowRect(&window_rect);
	CPoint topleft = window_rect.TopLeft();
	m_settings.m_parent->ScreenToClient(&topleft);

	CPoint point(0,0);
	switch( m_settings.m_type )
	{
	case Horizontal:
		point.y = static_cast<int>(split * (m_max - m_min)) + m_min;
		point.y -= topleft.y;
		break;
	case Vertical:
		point.x = static_cast<int>(split * (m_max - m_min)) + m_min;
		point.x -= topleft.x;
		break;
	};

	m_dragging = true;
	OnMouseMove(0, point);
	m_dragging = false;
}

int SplitterCtrl::OnCreate(LPCREATESTRUCT lpCreateStruct) 
{
	return CWnd::OnCreate(lpCreateStruct);
}

void SplitterCtrl::OnLButtonDown(UINT, CPoint)
{
	PR_ASSERT(PR_DBG, m_settings.m_parent && m_settings.m_side1 && m_settings.m_side2, "");
	m_dragging = true;
	SetCapture();
}

void SplitterCtrl::OnLButtonUp(UINT, CPoint)
{
	m_dragging = false;
	ReleaseCapture();
}

void SplitterCtrl::OnMouseMove(UINT, CPoint point)
{
	CRect client_rect, window_rect;
	GetClientRect(&client_rect);	
	GetWindowRect(&window_rect);
	
	if( client_rect.PtInRect(point) )
		SetCursor(AfxGetApp()->LoadStandardCursor(m_settings.m_type == Vertical ? IDC_SIZEWE : IDC_SIZENS));

	if( !m_dragging ) return;

	CPoint centre = window_rect.CenterPoint();
	m_settings.m_parent->ScreenToClient(&centre);

	CRect side1_rect, side2_rect;
	m_settings.m_side1->GetWindowRect(&side1_rect);
	m_settings.m_side2->GetWindowRect(&side2_rect);
	CPoint side1_topleft		= side1_rect.TopLeft();
	CPoint side1_bottomright	= side1_rect.BottomRight();
	CPoint side2_topleft		= side2_rect.TopLeft();
	CPoint side2_bottomright	= side2_rect.BottomRight();
	m_settings.m_parent->ScreenToClient(&side1_topleft);
	m_settings.m_parent->ScreenToClient(&side1_bottomright);
	m_settings.m_parent->ScreenToClient(&side2_topleft);
	m_settings.m_parent->ScreenToClient(&side2_bottomright);
	
	// Get 'point' relative to the centre of the splitter window
	switch( m_settings.m_type )
	{
	case Horizontal:
		{
			point.x = 0;
			point.y -= client_rect.Height() / 2;
			centre += point;
			if( centre.y < m_min ) centre.y = m_min;
			if( centre.y > m_max ) centre.y = m_max;
			int top		= centre.y - window_rect.Height() / 2;
			int bottom	= centre.y + window_rect.Height() / 2;
			SetWindowPos(0, centre.x - window_rect.Width() / 2, top, 0, 0, SWP_NOZORDER | SWP_NOSIZE);

			m_settings.m_side1->SetWindowPos(0, side1_topleft.x, side1_topleft.x, side1_rect.Width(), top - side1_topleft.y       , SWP_NOZORDER);
			m_settings.m_side2->SetWindowPos(0, side2_topleft.x, bottom         , side2_rect.Width(), side2_bottomright.y - bottom, SWP_NOZORDER);
		}break;

	case Vertical:
		{
			point.x -= client_rect.Width() / 2;
			point.y = 0;
			centre += point;
			if( centre.x < m_min ) centre.x = m_min;
			if( centre.x > m_max ) centre.x = m_max;
			int left  = centre.x - window_rect.Width() / 2;
			int right = centre.x + window_rect.Width() / 2;
			SetWindowPos(0, left, centre.y - window_rect.Height() / 2, 0, 0, SWP_NOZORDER | SWP_NOSIZE);

			m_settings.m_side1->SetWindowPos(0, side1_topleft.x, side1_topleft.y, left - side1_topleft.x     , side1_rect.Height(), SWP_NOZORDER);
			m_settings.m_side2->SetWindowPos(0, right          , side2_topleft.y, side2_bottomright.x - right, side2_rect.Height(), SWP_NOZORDER);
		}break;
	};
	m_settings.m_side1->Invalidate();
	m_settings.m_side2->Invalidate();
}


