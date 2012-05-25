//*********************************************
// Line Drawer
//	(C)opyright Rylogic Limited 2007
//*********************************************
#include "Stdafx.h"
#include "pr/common/commandline.h"
#include "pr/macros/link.h"
#include "pr/macros/auto_do.h"
#include "pr/filesys/FileSysWin.h"
#include "LineDrawer/Source/LineDrawerGlobal.h"
#include "LineDrawer/Source/LineDrawer.h"
#include "LineDrawer/Source/EventTypes.h"
#include "LineDrawer/GUI/LineDrawerGUI.h"

namespace pr
{
	namespace ldr
	{
		const char g_version_string[]		= "LineDrawer - v1.02 (c) Paul Ryland 2005";
		const uint g_version_string_length	= sizeof(g_version_string);
	}
}

// Return settings for the poller
PollingToEventSettings LineDrawer::LineDrawerPollerSettings(void* user_data)
{
	PollingToEventSettings settings;
	settings.m_polling_function		= LineDrawer::PollingFunction;
	settings.m_event_function		= 0;
	settings.m_polling_frequency	= 50;
	settings.m_user_data			= user_data;
	return settings;
}

// Constructor
LineDrawer::LineDrawer()
:m_window_handle(0)
,m_app_instance(0)
,m_line_drawer_GUI(0)
,m_renderer(0)
,m_navigation_manager()
,m_plugin_manager(*this)
,m_file_loader()
,m_data_manager()
,m_data_manager_GUI(0)
,m_listener()
,m_lua_input(*this)
,m_animation_control()
,m_camera_lock_GUI()
,m_error_output()
,m_user_settings()
,m_viewport(0)
,m_stereo_viewport(0)
,m_camera_to_light(v4Origin)
,m_light_direction(v4ZAxis)
,m_selected(0)
,m_render_pending_event(0)
,m_stereo_view(false)
,m_global_wireframe(EGlobalWireframeMode_Solid)
,m_last_refresh_from_file_time(0)
,m_poller(LineDrawerPollerSettings(this))
,m_resource_monitor(0)
{}

//*****
// Destructor
LineDrawer::~LineDrawer()
{
	UnInitialise();
}

//*****
// Start point for the whole app
void LineDrawer::DoModal()
{
	m_app_instance = LineDrawerApp.m_hInstance;

	// Determine the directory we're running in
	char temp[MAX_PATH];
	GetModuleFileName(0, temp, MAX_PATH);
	m_root_directory = temp;
	pr::filesys::RmvFilename(m_root_directory);
	
	m_user_settings.m_settings_filename = temp;
	pr::filesys::RmvExtension(m_user_settings.m_settings_filename);
	m_user_settings.m_settings_filename += ".ini";
	try { m_user_settings.Load(); }
	catch (...) {}

	// Create the dialog
	LineDrawerGUI dlg;
	LineDrawerApp.m_pMainWnd = &dlg;
	dlg.DoModal();
}

//*****
// Start up everything
bool LineDrawer::Initialise()
{
	CRect window_bounds, client_area;
	GetWindowRect(m_window_handle, &window_bounds);
    GetClientRect(m_window_handle, &client_area);
//	m_window_bounds = CRectToIRect(window_bounds);
	m_client_area   = CRectToIRect(client_area);

	if( !StartRenderer() ) return false;

	// Initialise all of the internal objects
	m_origin				.Create(*m_renderer, Colour32Red, Colour32Green, Colour32Blue);
	m_axis					.Create(*m_renderer, Colour32Red, Colour32Green, Colour32Blue);
	m_focus_point			.Create(*m_renderer, Colour32Red, Colour32Green, Colour32Blue);
	m_selection_box			.Create(*m_renderer);
	m_progress_dlg			.Create(Progress::IDD, m_line_drawer_GUI);
	m_lua_input				.CreateGUI();
	m_data_manager			.CreateGUI();
	m_animation_control		.CreateGUI();
	m_camera_lock_GUI		.CreateGUI();
	m_render_pending_event	= CreateEvent(0, TRUE, FALSE, 0);
	if (!m_render_pending_event) return false;

	// Check the command line
	m_navigation_manager.SetView(BBoxUnit);
	EnumCommandLine(GetCommandLine(), *this);
	m_navigation_manager.ApplyView();

	ApplyUserSettings();
	return true;
}

//*****
// Shut down everything
void LineDrawer::UnInitialise()
{
	m_navigation_manager.SetCameraMode(ECameraMode_Off);

	m_poller.Stop();
	CloseHandle(m_render_pending_event);
	m_render_pending_event = 0;

	// UnInitialise all of the internal objects
	m_progress_dlg			.DestroyWindow();
	m_plugin_manager		.StopPlugIn();
	m_data_manager			.Clear();
	
	delete m_Lviewport; m_Lviewport = 0;
	delete m_Rviewport; m_Rviewport = 0;
	delete m_renderer; m_renderer = 0;
}

// Resize the display. Only if we have to
void LineDrawer::Resize(bool force_resize)
{
	if( m_window_handle == 0 ) return;

	CRect bounds, area;
	//GetWindowRect(m_window_handle, &bounds);
	GetClientRect(m_window_handle, &area);
	//IRect window_bounds = CRectToIRect(bounds);
	IRect client_area   = CRectToIRect(area);
	//m_user_settings.m_window_pos = bounds;
	m_user_settings.Save();
	
	if( client_area != m_client_area /*|| window_bounds != m_window_bounds*/ || force_resize )
	{
		SetWindowText(m_window_handle, "LineDrawer - Reloading objects. ... Please wait");
		bool plugin_running = m_plugin_manager.IsPlugInLoaded();

		m_plugin_manager	.StopPlugIn();
		m_client_area		= client_area;
		//m_window_bounds		= window_bounds;

		if( m_renderer )
			m_renderer		->Resize(m_client_area);//, m_window_bounds);

		if( plugin_running ) m_plugin_manager.RestartPlugIn();
	/*	m_data_manager		.CreateDeviceDependentObjects();
		m_origin			.Create(Colour32Red, Colour32Green, Colour32Blue);
		m_axis				.Create(Colour32Red, Colour32Green, Colour32Blue);
		m_focus_point		.Create(Colour32White, Colour32White, Colour32White);
		m_selection_box		.Create();*/
		m_navigation_manager.Resize(m_client_area);
		m_navigation_manager.SetStereoView(m_stereo_view);
	}
	RefreshWindowText();
	Refresh();
}

struct AutoReset
{
	HANDLE m_event;
	AutoReset(HANDLE var) :m_event(var)	{}
	~AutoReset()						{ ResetEvent(m_event); }
};

// Render all viewports
void LineDrawer::Render()
{
	// Set the renderering event on return
	AutoReset auto_reset(m_render_pending_event);

	// Don't render if there isn't a renderer or we've been told not to
	if( !m_renderer || m_line_drawer_GUI->GetMenuItemState(LineDrawerGUI::EMenuItemsWithState_DisableRendering) ) return;

	// This can happen when an assert is thrown
	if (m_renderer->GetRenderingPhase() != pr::rdr::EState::Idle) return;

	if( Failed(m_renderer->RenderStart()) ) return;
	
	// Align the camera if necessary
	v4 align_axis;
	if( m_line_drawer_GUI->GetCameraAlignAxis(align_axis) )
	{
		m_navigation_manager.AlignCamera(align_axis);
	}

	// Add camera wander
	if( m_line_drawer_GUI->GetMenuItemState(LineDrawerGUI::EMenuItemsWithState_CameraWander) )
	{
		m_navigation_manager.WanderCamera();
	}

	// Update the light
	if( m_user_settings.m_light_is_camera_relative )
	{
		// Position the light using the camera relative position and direction
		m4x4 camera_matrix = m_navigation_manager.GetCameraToWorld();

		rdr::Light& light	= m_renderer->m_lighting_manager.m_light[0];
		light.m_position	= camera_matrix * m_camera_to_light;
		light.m_direction	= camera_matrix * m_light_direction;
	}

	if( !m_stereo_view )
	{
		RenderViewport(*m_viewport);
	}
	else
	{
		const float EYE_SEPARATION = 0.01f;

		// Get the distance to the focus point from the camera
		float focus_distance = Length3(m_navigation_manager.m_camera.GetPosition() - m_navigation_manager.GetFocusPoint());
		float yaw = ATan2(EYE_SEPARATION, focus_distance);

		// Render the left eye
		m_navigation_manager.m_camera.DTranslateRel(EYE_SEPARATION, 0.0f, 0.0f);
		m_navigation_manager.m_camera.DRotateRel(0.0f, -yaw, 0.0f);
		RenderViewport(*m_Lviewport);
		m_navigation_manager.m_camera.DRotateRel(0.0f,  yaw, 0.0f);
		m_navigation_manager.m_camera.DTranslateRel(-1.0f * EYE_SEPARATION, 0.0f, 0.0f);

		// Render the right eye
		m_navigation_manager.m_camera.DTranslateRel(-1.0f * EYE_SEPARATION, 0.0f, 0.0f);
		m_navigation_manager.m_camera.DRotateRel(0.0f,  yaw, 0.0f);
		RenderViewport(*m_Rviewport);
		m_navigation_manager.m_camera.DRotateRel(0.0f, -yaw, 0.0f);
		m_navigation_manager.m_camera.DTranslateRel(EYE_SEPARATION, 0.0f, 0.0f);
	}

	m_renderer->RenderEnd();
	m_renderer->Present();

	if( m_resource_monitor.get() )
	{
		m_resource_monitor->Sync();
	}
}

//*****
// Render all objects into a viewport
void LineDrawer::RenderViewport(rdr::Viewport& viewport)
{
	viewport.ClearDrawlist();

	// Set the view and projection matrices
	viewport.SetCameraToScreen(m_navigation_manager.GetCameraToScreen());
	viewport.SetWorldToCamera(m_navigation_manager.GetWorldToCamera());
	viewport.SetViewFrustum(m_navigation_manager.GetViewFrustum());
	
	// Render all of the objects in the data manager
	m_data_manager.Render(viewport);

	// Render the origin
	if( m_user_settings.m_show_origin )
	{
		float scale = m_user_settings.m_asterix_scale * Length3(m_navigation_manager.m_camera.GetPosition());
		m_origin.SetPositionAndScale(v4Origin, scale);
		m_origin.Render(viewport);
	}

	// Render the axis
	if( m_user_settings.m_show_axis )
	{
		Camera& camera	= m_navigation_manager.m_camera;
		float width		= camera.GetViewProperty(Camera::Width);
		float height	= camera.GetViewProperty(Camera::Height);
		float Near		= camera.GetViewProperty(Camera::Near);
		float Far		= camera.GetViewProperty(Camera::Far);
		m_axis.SetProjection(width, height, Near, Far, camera.IsRightHanded());

		// There is something wrong here, ScreenToWorld works because the
		// 'DataManager::SelectObject' stuff works. It must be something to
		// to with the projection for the axis :/
		//const v4 axis_position	= {0.95f, 0.95f, 0.01f, 1.0f};
		v4 axis_position		= {0.504f, 0.504f, 0.01f, 1.0f};
		v4 axis_ws_position     = camera.ScreenToWorld(axis_position);
		float axis_scale		= 1.0f;//Near * m_user_settings.m_asterix_scale;
		m_axis.SetPositionAndScale(axis_ws_position, axis_scale);
		m_axis.Render(viewport);
	}

	// Render the focus point
	if( m_user_settings.m_show_focus_point )
	{
		float scale = m_user_settings.m_asterix_scale * Length3(m_navigation_manager.GetFocusPoint() - m_navigation_manager.m_camera.GetPosition());
		m_focus_point.SetPositionAndScale(m_navigation_manager.GetFocusPoint(), scale);
		m_focus_point.Render(viewport);
	}

	// Render the selection box
	if( m_user_settings.m_show_selection_box )
	{
		BoundingBox bbox;
		if( m_data_manager_GUI->GetSelectionBBox(bbox) )
		{
			m_selection_box.SetSelection(bbox);
			m_selection_box.Render(viewport);
		}
	}
	
	// Set the global wireframe mode
	switch( m_global_wireframe )
	{
	case EGlobalWireframeMode_Solid:		viewport.SetRenderState(D3DRS_FILLMODE, D3DFILL_SOLID); break;
	case EGlobalWireframeMode_Wire:			viewport.SetRenderState(D3DRS_FILLMODE, D3DFILL_WIREFRAME); break;
	case EGlobalWireframeMode_SolidAndWire: viewport.SetRenderState(D3DRS_FILLMODE, D3DFILL_SOLID); break;
	}
	
	// Render the viewport
	viewport.Render();
	
	// Render the wire overtop of the solid
	if( m_global_wireframe == EGlobalWireframeMode_SolidAndWire )
	{
		rdr::rs::Block rsb_override;
		rsb_override.SetRenderState(D3DRS_ALPHABLENDENABLE, FALSE);
		rsb_override.SetRenderState(D3DRS_FILLMODE, D3DFILL_WIREFRAME);
		viewport.Render(false, rsb_override);
	}
}

// Refresh the window
void LineDrawer::Refresh()
{
	if( !IsBusy() )
	{
		SetEvent(m_render_pending_event);
		PostMessage(m_window_handle, WM_COMMAND, ID_REFRESH, 0);
	}
}

// Common refresh behaviour
#if USE_OLD_PARSER
bool LineDrawer::RefreshCommon(StringParser& string_parser, bool clear_data, bool recentre)
#else
bool LineDrawer::RefreshCommon(ParseResult& data, bool clear_data, bool recentre)
#endif
{
	// Store info about what we had
	bool had_data = m_data_manager.GetNumObjects() != 0;
	v4 old_centre = (had_data) ? (m_data_manager.m_bbox.Centre()) : (v4Origin);
	v4 old_cam_pos = m_navigation_manager.m_camera.GetPosition();

	// Save object states so we can restore them later
	TObjectState state;
	if( m_user_settings.m_persist_object_state ) m_data_manager.SaveObjectStates(state);

	// Update the data manager with the parsed data
	if( clear_data ) m_data_manager.Clear();
#if USE_OLD_PARSER
	uint num_objects = string_parser.GetNumObjects();
	for( uint i = 0; i < num_objects; ++i )
	{
		m_data_manager.AddObject(string_parser.GetObject(i));
	}
#else
	for (TBaseTmpPtrVec::iterator i = data.m_objects.begin(); i_end = data.m_objects.end(); i != i_end; ++i)
		m_data_manager.AddObject(*i), *i = 0;
#endif

	// Restore object states
	if( m_user_settings.m_persist_object_state ) m_data_manager.ApplyObjectStates(state);

	// Set the new default view (note, not applied yet)
	m_navigation_manager.SetView(m_data_manager.m_bbox);

	// Recentre the view based on the change in bounding box centre
	if( recentre )
	{
		m_navigation_manager.m_camera.DTranslateWorld(m_data_manager.m_bbox.Centre() - old_centre);
	}
	// Apply the view after each load
	else if( m_user_settings.m_reset_camera_on_load )
	{
		m_navigation_manager.ApplyView();
	}
	// Restore the camera position 
	else
	{
		m_navigation_manager.m_camera.SetPosition(old_cam_pos);
	}

	// Apply the lock mask if set
#if USE_OLD_PARSER
	if( string_parser.GetLockMask() )
		m_navigation_manager.SetLockMask(string_parser.GetLockMask());
#else
	if (data.m_lock_mask)
		m_navigation_manager.SetLockMask(data.m_lock_mask);
#endif
#if USE_OLD_PARSER
	// Set the wireframe mode if set
	if( string_parser.ContainsGlobalWireframeMode() )
		SetGlobalWireframeMode((EGlobalWireframeMode)string_parser.GetGlobalWireframeMode());
#else
	if (data.m_global_wireframe_mode != EGlobalWireframeMode_NotSet)
		SetGlobalWireframeMode(data.m_global_wireframe_mode);
#endif
#if USE_OLD_PARSER
	// Apply any view changes if set
	ViewMask view_mask = string_parser.GetViewMask();
	if( view_mask )
	{
		CameraView const& view = string_parser.GetView();
		v4 pos = m_navigation_manager.m_camera.GetPosition();
		if( view_mask[ViewMask::PositionX] )	pos.x = view.m_camera_position.x;
		if( view_mask[ViewMask::PositionY] )	pos.y = view.m_camera_position.y;
		if( view_mask[ViewMask::PositionZ] )	pos.z = view.m_camera_position.z;
		m_navigation_manager.m_camera.SetPosition(pos);
		v4 up = m_navigation_manager.m_camera.GetUp();
		if( view_mask[ViewMask::UpX] )			up.x = view.m_camera_up.x;
		if( view_mask[ViewMask::UpY] )			up.y = view.m_camera_up.y;
		if( view_mask[ViewMask::UpZ] )			up.z = view.m_camera_up.z;
		m_navigation_manager.m_camera.SetUp(up);
		if( view_mask[ViewMask::LookAt] )	m_navigation_manager.m_camera.LookAt(view.m_lookat_centre);
		if( view_mask[ViewMask::FOV] )		m_navigation_manager.m_camera.SetViewProperty(Camera::FOV, view.m_fov);
		if( view_mask[ViewMask::Aspect] )	m_navigation_manager.m_camera.SetViewProperty(Camera::Aspect, view.m_aspect);
		if( view_mask[ViewMask::Near] )		m_navigation_manager.m_camera.SetViewProperty(Camera::Near, view.m_near);
		if( view_mask[ViewMask::Far] )		m_navigation_manager.m_camera.SetViewProperty(Camera::Far, view.m_far);
		if     ( view_mask[ViewMask::AlignX] )	m_line_drawer_GUI->UpdateMenuItemState(LineDrawerGUI::EMenuItemsWithState_AlignToX, true);
		else if( view_mask[ViewMask::AlignY] )	m_line_drawer_GUI->UpdateMenuItemState(LineDrawerGUI::EMenuItemsWithState_AlignToY, true);
		else if( view_mask[ViewMask::AlignZ] )	m_line_drawer_GUI->UpdateMenuItemState(LineDrawerGUI::EMenuItemsWithState_AlignToZ, true);
	}
#else
	// Apply any view changes if set
	if (data.m_view_mask)
	{
		v4 pos = m_navigation_manager.m_camera.GetPosition();
		if (data.m_view_mask[ViewMask::PositionX])	pos.x = data.m_view.m_camera_position.x;
		if (data.m_view_mask[ViewMask::PositionY])	pos.y = data.m_view.m_camera_position.y;
		if (data.m_view_mask[ViewMask::PositionZ])	pos.z = data.m_view.m_camera_position.z;
		m_navigation_manager.m_camera.SetPosition(pos);
		v4 up = m_navigation_manager.m_camera.GetUp();
		if (data.m_view_mask[ViewMask::UpX])		up.x = data.m_view.m_camera_up.x;
		if (data.m_view_mask[ViewMask::UpY])		up.y = data.m_view.m_camera_up.y;
		if (data.m_view_mask[ViewMask::UpZ])		up.z = data.m_view.m_camera_up.z;
		m_navigation_manager.m_camera.SetUp(up);
		if (data.m_view_mask[ViewMask::LookAt])		m_navigation_manager.m_camera.LookAt(data.m_view.m_lookat_centre);
		if (data.m_view_mask[ViewMask::FOV])		m_navigation_manager.m_camera.SetViewProperty(Camera::FOV, data.m_view.m_fov);
		if (data.m_view_mask[ViewMask::Aspect])		m_navigation_manager.m_camera.SetViewProperty(Camera::Aspect, data.m_view.m_aspect);
		if (data.m_view_mask[ViewMask::Near])		m_navigation_manager.m_camera.SetViewProperty(Camera::Near, data.m_view.m_near);
		if (data.m_view_mask[ViewMask::Far])		m_navigation_manager.m_camera.SetViewProperty(Camera::Far, data.m_view.m_far);
		if     (data.m_view_mask[ViewMask::AlignX])	m_line_drawer_GUI->UpdateMenuItemState(LineDrawerGUI::EMenuItemsWithState_AlignToX, true);
		else if(data.m_view_mask[ViewMask::AlignY])	m_line_drawer_GUI->UpdateMenuItemState(LineDrawerGUI::EMenuItemsWithState_AlignToY, true);
		else if(data.m_view_mask[ViewMask::AlignZ])	m_line_drawer_GUI->UpdateMenuItemState(LineDrawerGUI::EMenuItemsWithState_AlignToZ, true);
	}
#endif

	Refresh();
	RefreshWindowText();
	return true;
}

// Refresh the file and display
bool LineDrawer::RefreshFromFile(uint now, bool recentre)
{
	// If the time of the request to refresh is older than when we last finished refreshing then ignore it.
	if( now < m_last_refresh_from_file_time ) return true;

#if USE_OLD_PARSER
	// Parse the file data
	StringParser string_parser(this);
	if( !string_parser.Parse(m_file_loader) ) { RefreshWindowText(); return false; }
#else
	ParseResult data;
	if (!ParseSource(*this, m_file_loader, data)) { RefreshWindowText(); return false; }
#endif

	// Do the common refresh code
	bool result = RefreshCommon(string_parser, true, recentre);

	// Remember the time when we finished the refresh
	m_last_refresh_from_file_time = GetTickCount();
	return result;
}

// Refresh the display from a string source
bool LineDrawer::RefreshFromString(const char* source, std::size_t length, bool clear_data, bool recentre)
{
#if USE_OLD_PARSER
	StringParser string_parser(this);
	if( !string_parser.Parse(source, length) ) return false;
	return RefreshCommon(string_parser, clear_data, recentre);
#else
	ParseResult data;
	if (!ParseSource(*this, source, data)) return false;
	return RefreshCommon(data, clear_data, recentre);
#endif
}

// Update the window text
void LineDrawer::RefreshWindowText()
{
	SetWindowText(m_window_handle,
		Fmt("LineDrawer - \"%s\": %s",
			m_file_loader.GetCurrentFilename(),
			m_navigation_manager.GetStatusString()).c_str());
}

// Show a progress dialog.
// 'running_time' is the number of seconds since the process started.
bool LineDrawer::SetProgress(uint number, uint maximum, const char* caption, uint running_time)
{
	if( running_time >= ShowProgressTime )
		return m_progress_dlg.SetProgress(number, maximum, caption);
	return true;
}

// Toggle wireframe rendering
void LineDrawer::SetGlobalWireframeMode(EGlobalWireframeMode mode)
{
	PR_ASSERT_STR(PR_DBG_LDR, mode >= 0 && mode <= 2, "Invalid wireframe mode");
	m_global_wireframe = mode;
	
	// Notify a change in wireframe mode
	GUIUpdate e;
	e.m_type = GUIUpdate::EType_GlobalWireframe;
	e.m_data = m_global_wireframe;
	events::Send(e);
}
EGlobalWireframeMode LineDrawer::GetGlobalWireframeMode() const
{
	return m_global_wireframe;
}

// Set the light we're using. Position and Direction are in camera space
void LineDrawer::SetLight(const rdr::Light& light, bool camera_relative)
{
	m_user_settings.m_light_is_camera_relative	= camera_relative;
	m_user_settings.m_light						= light;
	if( m_user_settings.m_light_is_camera_relative )
	{
		m_camera_to_light = light.m_position;
		m_light_direction = light.m_direction;
		m_renderer->m_lighting_manager.m_light[0]				= light;
		m_renderer->m_lighting_manager.m_light[0].m_position	= v4Origin;
		m_renderer->m_lighting_manager.m_light[0].m_direction	= v4ZAxis;
	}
	else
	{
		m_renderer->m_lighting_manager.m_light[0] = light;
	}
	m_user_settings.Save();
}

// Return the light we're using. Position and Direction are in camera space
const rdr::Light& LineDrawer::GetLight()
{
	rdr::Light& light = m_renderer->m_lighting_manager.m_light[0];
	if( m_user_settings.m_light_is_camera_relative )
	{
		light.m_position	= m_camera_to_light;
		light.m_direction	= m_light_direction;
	}
	m_user_settings.m_light = light;
	m_user_settings.Save();
	return light;
}

// A common path for adding files
void LineDrawer::InputFile(std::string const& filename, bool additive, bool refresh)
{
	std::string extn = str::LowerCase(pr::filesys::GetExtension(filename));
	
	// Handle shortcuts
	if( extn == "lnk" ) file_sys::ResolveShortcut(filename);
	
	AddRecentFile(filename);

	// Send lua files to 'LuaInput'
	if( extn == "lua" )
	{
		m_lua_input.DoFile(filename);
	}
	// Otherwise add files to the file loader
	else
	{
		if( additive )	m_file_loader.AddSource(filename.c_str());
		else			m_file_loader.SetSource(filename.c_str());
		if( refresh )   RefreshFromFile(static_cast<uint>(GetMessageTime()), false);
	}
}

// Add a file to the recent files list
void LineDrawer::AddRecentFile(const std::string& filename, bool update_menu)
{
	// Do a case insensitive search for the file
	UserSettings::TStringList::iterator iter = m_user_settings.m_recent_files.begin(), iter_end = m_user_settings.m_recent_files.end();
	for( ; iter != iter_end; ++iter )
	{
		if( str::EqualNoCase(filename, *iter) )
		{
			m_user_settings.m_recent_files.push_front(filename);
			m_user_settings.m_recent_files.erase(iter);
			m_user_settings.Save();
			return;
		}
	}

	// Otherwise add the file to the front of the list
	m_user_settings.m_recent_files.push_front(filename);
	while( (uint)m_user_settings.m_recent_files.size() > UserSettings::MaxRecentFiles )
	{
		m_user_settings.m_recent_files.pop_back();
	}

	// Update the recent files menu
	if( update_menu ) m_line_drawer_GUI->UpdateRecentFiles();
	m_user_settings.Save();
}

//*****
// Remove a file from the recent files list
void LineDrawer::RemoveRecentFile(const std::string& filename, bool update_menu)
{
	// Do a case insensitive search for the file
	UserSettings::TStringList::iterator iter = m_user_settings.m_recent_files.begin(), iter_end = m_user_settings.m_recent_files.end();
	for( ; iter != iter_end; ++iter )
	{
		if( str::EqualNoCase(filename, *iter) ) break;
	}
	if( iter == iter_end ) return;

	m_user_settings.m_recent_files.erase(iter);

	// Update the recent files menu
	if( update_menu ) m_line_drawer_GUI->UpdateRecentFiles();
	m_user_settings.Save();
}

//*****
// Returns true if LineDrawer is busy. This method is intended to be called by other threads
bool LineDrawer::IsBusy() const
{
	return WaitForSingleObject(m_render_pending_event, 0) == WAIT_OBJECT_0;
}

// Start/Stop the poller thread
void LineDrawer::Poller(bool start)
{
	if( start )	{ m_poller.Start(); }
	else		{ m_poller.Stop(); }
}

//*****
// Turn on/off stereo view
void LineDrawer::SetStereoView(bool on)
{
	if( m_stereo_view == on ) return;
	m_stereo_view = on;
	m_navigation_manager.SetStereoView(on);

	FRect rect;
	if( m_stereo_view )
	{
		rect.set(0.0f, 0.0f, 0.5f, 1.0f);
		m_Lviewport->SetViewportRect(rect);

		rect.set(0.5f, 0.0f, 1.0f, 1.0f);
		m_Rviewport->SetViewportRect(rect);
	}
	else
	{
		rect.set(0.0f, 0.0f, 1.0f, 1.0f);
		m_viewport->SetViewportRect(rect);
	}
	LineDrawer::Get().Refresh();
}

//*****
// Parse command line options
bool LineDrawer::CmdLineOption(std::string const& option, cmdline::TArgIter& arg, cmdline::TArgIter arg_end)
{
	if( str::EqualNoCase(option, "-Plugin") && arg != arg_end )
	{
		// Stop the plugin if its currently running
		if( m_line_drawer_GUI->GetMenuItemState(LineDrawerGUI::EMenuItemsWithState_PlugInRunning) )
		{
			m_plugin_manager.StopPlugIn();
			m_line_drawer_GUI->UpdateMenuItemState(LineDrawerGUI::EMenuItemsWithState_PlugInRunning, false);
		}
		
		// Copy the remaining command line args
		pr::cmdline::TArgs args(arg + 1, arg_end);

		// Start the plugin
		bool plugin_running = m_plugin_manager.StartPlugIn(arg->c_str(), args);
		m_line_drawer_GUI->UpdateMenuItemState(LineDrawerGUI::EMenuItemsWithState_PlugInRunning, plugin_running);

		arg = arg_end;
		return true;
	}
	else
	{
		MessageBox(m_window_handle,
			"Invalid command line options\n"
			"\n"
			"Syntax:\n"
			"   -S source_file_name\n"
			"   -Plugin plugin_name\n"
			"\n",
			"Command Line Error",
			MB_OK | MB_ICONERROR);
		return false;
	}
}

//*****
// Assume data without an option is a file to load
bool LineDrawer::CmdLineData(cmdline::TArgIter& data, cmdline::TArgIter data_end)
{
	m_file_loader.ClearSource();
	for( ; data != data_end && !cmdline::IsOption(*data); ++data )
	{
		m_file_loader.AddSource(data->c_str());
	}
	RefreshFromFile(GetTickCount(), false);
	return true;
}

//*******************************************************************************************
// Private methods
bool LineDrawer::StartRenderer()
{
//#pragma message(PR_LINK "Remove me")
//return true; // Disable renderering while gfx card is poked

	// Create a renderer
	rdr::RdrSettings rdr_settings;
	rdr_settings.m_window_handle			= m_window_handle;
	rdr_settings.m_device_config			= rdr::GetDefaultDeviceConfigWindowed();//rdr::GetDefaultDeviceConfigFullScreen(1024, 768);//
	rdr_settings.m_allocator				= &m_allocator;
	rdr_settings.m_client_area				= m_client_area;
	rdr_settings.m_zbuffer_format			= D3DFMT_D24S8;
	rdr_settings.m_swap_effect				= D3DSWAPEFFECT_DISCARD;//D3DSWAPEFFECT_COPY;//DISCARD;
	rdr_settings.m_back_buffer_count		= 1;
	rdr_settings.m_geometry_quality			= m_user_settings.m_geometry_quality;
	rdr_settings.m_texture_quality			= m_user_settings.m_texture_quality;
	rdr_settings.m_background_colour		= 0xFF7F7F7F;
	//rdr_settings.m_window_bounds			= m_window_bounds;
	rdr_settings.m_max_shader_version		= m_user_settings.m_shader_version;
	try
	{
		m_renderer = new Renderer(rdr_settings);
	}
	catch(const rdr::Exception& e)
	{
		m_line_drawer_GUI->MessageBox(e.m_message.c_str(), "Renderer startup failure", MB_ICONEXCLAMATION | MB_OK);
		return false;
	};

	// Initialise both viewports
	for( int i = 0; i < 2; ++i )
	{
		rdr::Viewport*& viewport = (i == 0) ? (m_viewport) : (m_stereo_viewport);
		
		rdr::VPSettings vp_settings;
		vp_settings.m_renderer				= m_renderer;
		vp_settings.m_identifier			= static_cast<rdr::ViewportId>(i);
		try
		{
			viewport = new rdr::Viewport(vp_settings);
		}
		catch(const rdr::Exception& e)
		{
			m_line_drawer_GUI->MessageBox(e.m_message.c_str(), "Viewport creation failure", MB_ICONEXCLAMATION | MB_OK);
			return false;
		}
		viewport->SetRenderState(D3DRS_CULLMODE, D3DCULL_NONE);
	}

	rdr::Light light;
	light.m_on = true;
	m_renderer->m_lighting_manager.m_light[0] = light;
	return true;
}

//*****
// Apply the users settings
void LineDrawer::ApplyUserSettings()
{
	// Check whether we need to recreate the renderer
	if( m_renderer &&
		(!str::Equal(m_renderer->m_material_manager.GetMaxShaderVersion(), m_user_settings.m_shader_version) ||
		m_renderer->GetGeometryQuality() != m_user_settings.m_geometry_quality ||
		m_renderer->GetTextureQuality()	 != m_user_settings.m_texture_quality) )
	{
		bool plugin_running = m_plugin_manager.IsPlugInLoaded();
		m_data_manager		.Clear();
		m_plugin_manager	.StopPlugIn();

		delete m_Lviewport; m_Lviewport = 0;
		delete m_Rviewport; m_Rviewport = 0;
		delete m_renderer;  m_renderer = 0;

		if( !StartRenderer() )
		{
			throw pr::Exception(0, "Renderer startup failure");
		}

		m_origin		.Create(*m_renderer, Colour32Red, Colour32Green, Colour32Blue);
		m_axis			.Create(*m_renderer, Colour32Red, Colour32Green, Colour32Blue);
		m_focus_point	.Create(*m_renderer, Colour32White, Colour32White, Colour32White);
		m_selection_box	.Create(*m_renderer);

		RefreshFromFile(GetTickCount(), false);
		if( plugin_running ) m_plugin_manager.RestartPlugIn();
	}

	m_line_drawer_GUI->UpdateRecentFiles();
	m_line_drawer_GUI->UpdateMenuItemState(LineDrawerGUI::EMenuItemsWithState_ShowOrigin,		m_user_settings.m_show_origin);
	m_line_drawer_GUI->UpdateMenuItemState(LineDrawerGUI::EMenuItemsWithState_ShowAxis,			m_user_settings.m_show_axis);
	m_line_drawer_GUI->UpdateMenuItemState(LineDrawerGUI::EMenuItemsWithState_ShowFocus,		m_user_settings.m_show_focus_point);
	m_line_drawer_GUI->UpdateMenuItemState(LineDrawerGUI::EMenuItemsWithState_ShowSelectionBox, m_user_settings.m_show_selection_box);
	m_line_drawer_GUI->UpdateMenuItemState(LineDrawerGUI::EMenuItemsWithState_PersistState,     m_user_settings.m_persist_object_state);

	if( m_renderer )
	{
		#pragma message(PR_LINK "Remove me")
		{
			m_user_settings.m_light.m_ambient.a = 0.0f;
			m_user_settings.m_light.m_diffuse.a = 1.0f;
			m_user_settings.m_light.m_specular.a = 0.0f;
		}
		SetLight(m_user_settings.m_light, m_user_settings.m_light_is_camera_relative);
	}

	if( m_user_settings.m_enable_resource_monitor && !m_resource_monitor.get() )
	{
		//rdr::TWatched watched;
		//watched.push_back(rdr::ResourceWatch(rdr::EResource_Effect ,rdr::EBuiltInEffect_XYZ					,"XYZ.fx"				));
		//watched.push_back(rdr::ResourceWatch(rdr::EResource_Effect ,rdr::EBuiltInEffect_XYZLit				,"XYZLit.fx"			));
		//watched.push_back(rdr::ResourceWatch(rdr::EResource_Effect ,rdr::EBuiltInEffect_XYZLitPVC			,"XYZLitPVC.fx"			));
		//watched.push_back(rdr::ResourceWatch(rdr::EResource_Effect ,rdr::EBuiltInEffect_XYZLitPVCTextured	,"XYZLitPVCTextured.fx"	));
		//watched.push_back(rdr::ResourceWatch(rdr::EResource_Effect ,rdr::EBuiltInEffect_XYZLitTextured		,"XYZLitTextured.fx"	));
		//watched.push_back(rdr::ResourceWatch(rdr::EResource_Effect ,rdr::EBuiltInEffect_XYZLitTint			,"XYZLitTint.fx"		));
		//watched.push_back(rdr::ResourceWatch(rdr::EResource_Effect ,rdr::EBuiltInEffect_XYZLitTintTextured	,"XYZLitTintTextured.fx"));
		//watched.push_back(rdr::ResourceWatch(rdr::EResource_Effect ,rdr::EBuiltInEffect_XYZPVC				,"XYZPVC.fx"			));
		//watched.push_back(rdr::ResourceWatch(rdr::EResource_Effect ,rdr::EBuiltInEffect_XYZPVCTextured		,"XYZPVCTextured.fx"	));
		//watched.push_back(rdr::ResourceWatch(rdr::EResource_Effect ,rdr::EBuiltInEffect_XYZTextured			,"XYZTextured.fx"		));
		//watched.push_back(rdr::ResourceWatch(rdr::EResource_Effect ,rdr::EBuiltInEffect_XYZTint				,"XYZTint.fx"			));
		//watched.push_back(rdr::ResourceWatch(rdr::EResource_Effect ,rdr::EBuiltInEffect_XYZTintTextured		,"XYZTintTextured.fx"	));
		//rdr::TPaths include_paths; include_paths.push_back("Q:/SDK/pr/pr/renderer/Effects/Shaders"); // These should be in the user settings really
		//m_resource_monitor.reset(new rdr::ResourceMonitor(*m_renderer, watched, include_paths));
	}
	if( !m_user_settings.m_enable_resource_monitor && m_resource_monitor.get() )
	{
		delete m_resource_monitor.release();
	}

	Refresh();
}

// Create a renderer model. Handles no renderer being created
rdr::EResult LineDrawer::CreateModel(rdr::model::Settings const& settings, rdr::Model*& model_out)
{
	if( !m_renderer ) return rdr::EResult_CreateModelFailed;
	return m_renderer->m_model_manager.CreateModel(settings, model_out);
}

// Delete a renderer model
void LineDrawer::DeleteModel(rdr::Model*& model)
{
	return m_renderer->m_model_manager.DeleteModel(model);
}

//*****
// Polling thread for refreshing animations
bool LineDrawer::PollingFunction(void*)
{
	LineDrawer::Get().Refresh();
	return false;
}