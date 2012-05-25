//*************************************************************************
//
// SplitterCtrl
//
//*************************************************************************
// Use:
//	In the resource editor, create a custom control.
//	Set it's 'Class' property to 'PRSplitterCtrl'
//	To resize a split window:
//		Get the split fraction
//		Move the side1/side2 windows to [0, split_fraction - d] and [split_fraction + d, width]
//		Move the splitter window to [split_fraction - d, split_fraction + d]
//		Call ResetMinMaxRange();
//		Call SetSplitFraction(split_fraction)
//	
//	OnSize():
//		float split_fraction = m_splitter.GetSplitFraction();
//		(CWnd*)side1->MoveWindow(side1_rect);
//		(CWnd*)side2->MoveWindow(side2_rect);
//		(CWnd*)splitter->MoveWindow(splitter_rect);
//		m_splitter.ResetMinMaxRange();
//		m_splitter.SetSplitFraction(split_fraction);
//

#ifndef PR_SPLITTER_CTRL_H
#define PR_SPLITTER_CTRL_H

#include <afxwin.h>         // MFC core and standard components
//	#ifndef _AFX_NO_AFXCMN_SUPPORT
//#include <afxcmn.h>			// MFC support for Windows Common Controls
//#endif // _AFX_NO_AFXCMN_SUPPORT

namespace pr
{
	class SplitterCtrl : public CWnd
	{
	public:
		// Dialog Data
		enum Type {Horizontal, Vertical};

		SplitterCtrl();
		virtual ~SplitterCtrl();
		
		struct Settings
		{
			SplitterCtrl::Type	m_type;
			CWnd*				m_parent;
			CWnd*				m_side1;
			CWnd*				m_side2;
		};
		void	Initialise(const Settings& settings);
		void	ResetMinMaxRange();
		float	GetSplitFraction() const;
		void	SetSplitFraction(float split);

		DECLARE_MESSAGE_MAP()
		afx_msg int  OnCreate(LPCREATESTRUCT lpCreateStruct);
		afx_msg void OnLButtonDown(UINT nFlags, CPoint point);
		afx_msg void OnMouseMove(UINT nFlags, CPoint point);
		afx_msg void OnLButtonUp(UINT nFlags, CPoint point);

	private:
		Settings	m_settings;
		int			m_min;
		int			m_max;
		bool		m_dragging;		// True when we're dragging
	};
}//namespace pr

#endif//PR_SPLITTER_CTRL_H

