//*********************************************
// The Line Drawer main GUI
//	(C)opyright Rylogic Limited 2007
//*********************************************
#ifndef LINEDRAWERGUI_H
#define LINEDRAWERGUI_H

#include "pr/common/Events.h"
#include "LineDrawer/Resource.h"
#include "LineDrawer/Source/NavigationManager.h"
#include "LineDrawer/Source/ToolTip.h"
#include "LineDrawer/Source/Forward.h"
#include "LineDrawer/GUI/CoordinatesDlg.h"

//*******************************************************************************************
// LineDrawerGUI dialog
class LineDrawerGUI : public CDialog, public events::IRecv<GUIUpdate>
{
public:
	enum { IDD = IDD_LINEDRAWER_DIALOG, MAX_SINGLE_CLICK_TIME = 140 };
	enum EMenuItemsWithState
	{
		EMenuItemsWithState_PlugInRunning = 0,
		EMenuItemsWithState_ShowOrigin,
		EMenuItemsWithState_ShowAxis,
		EMenuItemsWithState_ShowFocus,
		EMenuItemsWithState_ShowSelectionBox,
		EMenuItemsWithState_AlignToX,
		EMenuItemsWithState_AlignToY,
		EMenuItemsWithState_AlignToZ,
		EMenuItemsWithState_AlignToSelected,
		EMenuItemsWithState_LockToSelection,
		EMenuItemsWithState_FreeCamera_Off,
		EMenuItemsWithState_FreeCamera_FreeCam,
		EMenuItemsWithState_PersistState,
		EMenuItemsWithState_AutoRefresh,
		EMenuItemsWithState_Listener,
		EMenuItemsWithState_CyclicsStarted,
		EMenuItemsWithState_Render2d,
		EMenuItemsWithState_RightHanded,
		EMenuItemsWithState_ShowCoords,
		EMenuItemsWithState_CameraWander,
		EMenuItemsWithState_DisableRendering,
		EMenuItemsWithState_AlwaysOnTop,
		EMenuItemsWithState_StereoView,
		EMenuItemsWithState_NumberOf
	};
	LineDrawerGUI(CWnd* pParent = 0);
	~LineDrawerGUI();

	bool GetMenuItemState(EMenuItemsWithState item) const;
	void UpdateMenuItemState(EMenuItemsWithState item, bool new_state);
	void UpdateRecentFiles();

	bool GetCameraAlignAxis(v4& axis);
	void SetInitialWindowPosition();
	void MouseMoveManipulate(CPoint const& point);
	void MouseMoveNavigate(CPoint const& point);
	
	enum { BUTTON_NOT_DOWN = 0x7FFFFFFF };
	virtual void	DoDataExchange(CDataExchange* pDX);	// DDX/DDV support
	virtual BOOL	OnInitDialog();
	virtual BOOL	PreTranslateMessage(MSG* pMsg);
	virtual void	OnDestroy();
	virtual void	OnCancel();
	//virtual void	OnOK();
	virtual void	OnEvent(GUIUpdate const& e);

	afx_msg void	OnSysCommand(UINT nID, LPARAM lParam);
	afx_msg void	OnHelp();
	afx_msg void	OnPaint();
	afx_msg HCURSOR	OnQueryDragIcon();
	afx_msg void	OnDropFiles(HDROP hDropInfo);
	afx_msg void	OnKeyDown(UINT nChar, UINT nRepCnt, UINT nFlags);
	afx_msg void	OnKeyUp(UINT nChar, UINT nRepCnt, UINT nFlags);
	afx_msg void	OnGetMinMaxInfo(MINMAXINFO* lpMMI);
	afx_msg void	OnSize(UINT nType, int cx, int cy);
	afx_msg void	OnMove(int x, int y);
	afx_msg void	OnMouseMove(UINT nFlags, CPoint point);
	afx_msg BOOL	OnMouseWheel(UINT nFlags, short zDelta, CPoint pt);
	afx_msg void	OnLButtonDown(UINT nFlags, CPoint point);
	afx_msg void	OnLButtonUp(UINT nFlags, CPoint point);
	afx_msg void	OnLButtonDblClk(UINT nFlags, CPoint point);
	afx_msg void	OnMButtonDown(UINT nFlags, CPoint point);
	afx_msg void	OnMButtonUp(UINT nFlags, CPoint point);
	afx_msg void	OnMButtonDblClk(UINT nFlags, CPoint point);
	afx_msg void	OnRButtonDown(UINT nFlags, CPoint point);
	afx_msg void	OnRButtonUp(UINT nFlags, CPoint point);
	afx_msg void	OnRButtonDblClk(UINT nFlags, CPoint point);
	afx_msg void	OnAutoRefreshFromFile();
	afx_msg void	OnPollCamera();
	afx_msg void	OnStepPlugIn();
	afx_msg void	OnRefresh();
	afx_msg void	OnFileLdrConsole();
	afx_msg void	OnFileLuaConsole();
	afx_msg void	OnFileOpen();
	afx_msg void	OnFileAdditiveopen();
	afx_msg void	OnFileSave();
	afx_msg void	OnFileSaveas();
	afx_msg void	OnFileOptions();
	afx_msg void	OnFileRunplugin();
	afx_msg void	OnRecentfilesSelect(UINT nID);
	afx_msg void	OnNavigationJumptoOrigin();
	afx_msg void	OnNavigationJumptoVisible();
	afx_msg void	OnNavigationJumptoSelected();
	afx_msg void	OnNavigationSelectNext();
	afx_msg void	OnNavigationSelectPrev();
	afx_msg void 	OnNavigationAligntoX();
	afx_msg void 	OnNavigationAligntoY();
	afx_msg void 	OnNavigationAligntoZ();
	afx_msg void 	OnNavigationAligntoSelected();
	afx_msg void	OnNavigationShoworigin();
	afx_msg void	OnNavigationShowaxis();
	afx_msg void	OnNavigationShowfocuspoint();
	afx_msg void	OnNavigationLock();
	afx_msg void	OnNavigationLocktoselection();
	afx_msg void	OnNavigationVieworigin();
	afx_msg void	OnNavigationViewTopdown();
	afx_msg void	OnNavigationViewBottomup();
	afx_msg void	OnNavigationViewLeftside();
	afx_msg void	OnNavigationViewRightside();
	afx_msg void	OnNavigationViewFront();
	afx_msg void	OnNavigationViewBack();
	afx_msg void	OnNavigationViewproperties();
	afx_msg void	OnNavigationFreeCameraOff();
	afx_msg void	OnNavigationFreeCameraFreeCam();
	afx_msg void	OnDataSelectNone();
	afx_msg void	OnDataShowselection();
	afx_msg void	OnDataClear();
	afx_msg void	OnDataPersiststate();
	afx_msg void	OnDataAutoclear();
	afx_msg void	OnDataAutorefresh();
	afx_msg void	OnDatalistener();
	afx_msg void	OnDataStartcyclicobjects();
	afx_msg void	OnDataAnimation();
	afx_msg void	OnDataDatalist();
	afx_msg	void	OnRenderingWireframe();
	afx_msg void	OnRenderingCoordinates();
	afx_msg void	OnRenderingCamerawander();
	afx_msg	void	OnRenderingRender2d();
	afx_msg	void	OnRenderingRighthanded();
	afx_msg void	OnRenderingStereoview();
	afx_msg void	OnRenderingLighting();
	afx_msg void	OnRenderingDisableRendering();
	afx_msg void	OnWindowAlwaysontop();
	afx_msg void	OnWindowBackgroundcolour();
	afx_msg void	OnWindowLinedrawerhelp();
	afx_msg	void	OnWindowAbout();
	afx_msg void	OnAcceleratorNew();
	afx_msg void	OnAcceleratorConsole();
	afx_msg void	OnAcceleratorOpen();
	afx_msg void	OnAcceleratorAdditiveopen();
	afx_msg void	OnAcceleratorSave();
	afx_msg void	OnAcceleratorSaveas();
	afx_msg void	OnAcceleratorRunplugin();
	afx_msg void	OnAcceleratorLighting();
	afx_msg void	OnAcceleratorFocus();
	afx_msg void	OnAcceleratorWireframe();
	DECLARE_MESSAGE_MAP()

	bool MouseL() const	{ return m_mouse_left_down_at	!= BUTTON_NOT_DOWN; }
	bool MouseM() const	{ return m_mouse_middle_down_at != BUTTON_NOT_DOWN; }
	bool MouseR() const	{ return m_mouse_right_down_at	!= BUTTON_NOT_DOWN; }

private:
	LineDrawer*			m_linedrawer;
	HICON				m_hIcon;
	HACCEL				m_haccel;
	NavigationManager&	m_nav;
	DataManager&		m_dm;
	CCoordinatesDlg		m_coords;
	std::string			m_new_object_string;
	DWORD				m_mouse_left_down_at;
	DWORD				m_mouse_middle_down_at;
	DWORD				m_mouse_right_down_at;
	CPoint				m_mouse_down_pt;
	bool				m_menu_item_state[EMenuItemsWithState_NumberOf];
	bool				m_resize_needed;
};

#endif//LINEDRAWERGUI_H
