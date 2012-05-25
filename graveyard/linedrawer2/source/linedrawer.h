//*********************************************
// Line Drawer
//	(C)opyright Rylogic Limited 2007
//*********************************************
#ifndef LINEDRAWER_H
#define LINEDRAWER_H
#include "pr/common/Singleton.h"
#include "pr/common/PollingToEvent.h"
#include "pr/common/CommandLine.h"
#include "pr/renderer/renderer.h"
#include "LineDrawer/Resource.h"
#include "LineDrawer/Source/RenderBin.h"
#include "LineDrawer/Source/DataManager.h"
#include "LineDrawer/Source/PipeInput.h"
#include "LineDrawer/Source/LuaInput.h"
#include "LineDrawer/Source/FileLoader.h"
#include "LineDrawer/Source/NavigationManager.h"
#include "LineDrawer/PlugIn/PlugInManager.h"
#include "LineDrawer/Source/Progress.h"
#include "LineDrawer/Source/ErrorOutput.h"
#include "LineDrawer/GUI/AnimationControlDlg.h"
#include "LineDrawer/GUI/CameraLocksDlg.h"
#include "LineDrawer/Objects/Asterix.h"
#include "LineDrawer/Objects/AxisOverlay.h"
#include "LineDrawer/Objects/SelectionBox.h"
#if USE_OLD_PARSER
#include "LineDrawer/Objects/StringParser.h"
#else
#include "LineDrawer/Objects/Parser.h"
#endif
#include "LineDrawer/Source/UserSettings.h"
#include "LineDrawer/Source/Forward.h"
#include "LineDrawer/Source/LineDrawerAssertEnable.h"

//***************************************************************************
// LineDrawer:
class LineDrawer : public Singleton<LineDrawer>, public cmdline::IOptionReceiver
{
public:
	// Public members
	HWND				m_window_handle;
	HINSTANCE			m_app_instance;
	std::string			m_root_directory;
	LineDrawerGUI*		m_line_drawer_GUI;
	Renderer*			m_renderer;
	NavigationManager	m_navigation_manager;
	PlugInManager		m_plugin_manager;
	FileLoader			m_file_loader;
	DataManager			m_data_manager;
	DataManagerGUI*		m_data_manager_GUI;
	PipeInput			m_listener;
	LuaInput			m_lua_input;
	AnimationControlDlg	m_animation_control;
	CCameraLocksDlg		m_camera_lock_GUI;
	ErrorOutput			m_error_output;
	UserSettings		m_user_settings;

private:
	union
	{	
		rdr::Viewport*		m_viewport;
		rdr::Viewport*		m_Lviewport;
	};
	union
	{
		rdr::Viewport*		m_stereo_viewport;
		rdr::Viewport*		m_Rviewport;
	};
	rdr::Allocator			m_allocator;
	IRect					m_client_area;
	//IRect					m_window_bounds;
	v4						m_camera_to_light;
	v4						m_light_direction;
	Asterix					m_origin;
	AxisOverlay				m_axis;
	Asterix					m_focus_point;
	SelectionBox			m_selection_box;
	LdrObject*				m_selected;
	bool					m_stereo_view;
	EGlobalWireframeMode	m_global_wireframe;
	HANDLE					m_render_pending_event;
	Progress				m_progress_dlg;
	uint					m_last_refresh_from_file_time;
	PollingToEvent			m_poller;
	std::auto_ptr<rdr::ResourceMonitor>	m_resource_monitor;

public:
	enum { ShowProgressTime = 1000 };
	LineDrawer();
	~LineDrawer();
	void		DoModal();
	bool		Initialise();
	void		UnInitialise();
	void		Resize(bool force_resize = false);
	void		Render();
	void		RenderViewport(rdr::Viewport& viewport);
	
	void		Refresh();
	#if USE_OLD_PARSER
	bool		RefreshCommon(StringParser& string_parser, bool clear_data, bool recentre);
	#else
	bool		RefreshCommon(ParseResult& data, bool clear_data, bool recentre);
	#endif
	bool		RefreshFromFile(uint now, bool recentre);
	bool		RefreshFromString(const char* source, std::size_t length, bool clear_data, bool recentre);
	void		RefreshWindowText();
	bool		SetProgress(uint number, uint maximum, const char* caption, uint running_time = ShowProgressTime);

	IRect		CRectToIRect(const CRect& crect) const				{ IRect irect = {crect.left, crect.top, crect.right, crect.bottom}; return irect; }
	CRect		IRectToCRect(const IRect& irect) const				{ CRect crect(irect.m_min.x, irect.m_min.y, irect.m_max.x, irect.m_max.y); return crect; }
	IRect		GetClientArea() const								{ CRect area; GetClientRect(m_window_handle, &area); return CRectToIRect(area); }
	uint		GetCullMode() const									{ return m_viewport->GetRenderState(D3DRS_CULLMODE); }
	void		SetCullMode(D3DCULL mode)							{ m_viewport->SetRenderState(D3DRS_CULLMODE, mode); }
	bool		IsLightCameraRelative() const						{ return m_user_settings.m_light_is_camera_relative; }
	void		SetLight(const rdr::Light& light, bool camera_relative);
	rdr::Light const& GetLight();
	void					SetGlobalWireframeMode(EGlobalWireframeMode mode);
	EGlobalWireframeMode	GetGlobalWireframeMode() const;
	
	void		InputFile(std::string const& filename, bool additive, bool refresh = true);
	void		AddRecentFile(const std::string& filename, bool update_menu = true);
	void		RemoveRecentFile(const std::string& filename, bool update_menu = true);
	void		ApplyUserSettings();
	
	rdr::EResult CreateModel(rdr::model::Settings const& settings, rdr::Model*& model_out);
	void		DeleteModel(rdr::Model*& model);

	bool		IsBusy() const;
	void		Poller(bool start);
	void		SetStereoView(bool on);
	bool		CmdLineOption(std::string const& option, cmdline::TArgIter& arg, cmdline::TArgIter arg_end);
	bool		CmdLineData(cmdline::TArgIter& data, cmdline::TArgIter data_end);

private:
	bool		StartRenderer();
	static PollingToEventSettings LineDrawerPollerSettings(void* user_data);
	static bool	PollingFunction(void*);
};

#endif//LINEDRAWER_H
