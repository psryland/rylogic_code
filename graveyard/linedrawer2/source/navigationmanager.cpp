//*********************************************
// The Navigation manager interprets user input and is used to set up the
// projection matrix in the renderer
//	(C)opyright Rylogic Limited 2007
//*********************************************
#include "Stdafx.h"
#include "pr/common/PollingToEvent.h"
#include "pr/camera/CCKeyboardFreeCamera.h"
#include "pr/camera/CCKeyboardFreeCameraAxisLocked.h"
#include "LineDrawer/Source/LineDrawer.h"
#include "LineDrawer/Source/NavigationManager.h"

// Setup some default camera settings
CameraSettings MakeCameraSettings()
{
	CameraSettings settings;
	settings.m_3dcamera					= true;
	settings.m_righthanded				= true;
	settings.m_use_FOV_for_perspective	= true;
	settings.m_position					.set(0.0f, 0.0f, 10.0f, 1.0f);
	settings.m_orientation				.identity();
	settings.m_near						= 0.01f;
	settings.m_far						= 100.0f;
	settings.m_fov						= maths::pi/4.0f;
	settings.m_aspect					= 1.0f;
	return settings;
}

// Setup the poller settings
PollingToEventSettings NavigationManager::CameraPollerSettings(void* user_data)
{
	PollingToEventSettings settings;
	settings.m_event_function			= 0;
	settings.m_polling_frequency		= 20;
	settings.m_polling_function			= NavigationManager::CameraPoller;
	settings.m_user_data				= user_data;
	return settings;
}

//*****
// Constructor
NavigationManager::NavigationManager()
:m_camera(MakeCameraSettings())
,m_view()
,m_camera_controller(0)
,m_camera_poller(CameraPollerSettings(this))
,m_camera_mode(ECameraMode_Off)
,m_camera_wander(v4Zero)
,m_focus_dist(1.0f)
,m_zoom_fraction(1.0f)
,m_locks()
,m_lock_selection(false)
,m_free_cam_last_time(0)
{
	m_view.CreateFromBBox(BBoxUnit, IRectUnit);
}

//*****
// Destructor
NavigationManager::~NavigationManager()
{
	SetCameraMode(ECameraMode_Off);
}

//*****
// Adjust the camera for the new size of the client area
void NavigationManager::Resize(const IRect& client_area)
{
	m_view.m_aspect = client_area.SizeX() / float(client_area.SizeY());
	m_camera.SetViewProperty(Camera::Aspect, m_view.m_aspect);
}

// Return the camera view object
ldr::CameraData NavigationManager::GetCameraData() const
{
	ldr::CameraData view;
	view.m_camera_position	= GetCameraToWorld().pos;
	view.m_lookat_centre	= GetFocusPoint();
	view.m_focus_dist		= GetFocusDistance();
	view.m_camera_up		= GetCameraToWorld().y;
	view.m_near				= m_camera.GetViewProperty(Camera::Near);
	view.m_far				= m_camera.GetViewProperty(Camera::Far);
	view.m_fov				= m_camera.GetViewProperty(Camera::FOV);
	view.m_aspect			= m_camera.GetViewProperty(Camera::Aspect);
	view.m_width			= m_camera.GetViewProperty(Camera::Width);
	view.m_height			= m_camera.GetViewProperty(Camera::Height);
	view.m_is_3d			= m_camera.Is3D();
	return view;
}

// View a bounding box
void NavigationManager::SetView(const BoundingBox& bbox)
{
	if( bbox == BBoxReset )	m_view.CreateFromBBox(BBoxUnit, LineDrawer::Get().GetClientArea());
	else					m_view.CreateFromBBox(bbox    , LineDrawer::Get().GetClientArea());
}

//*****
// Set the camera view explicitly
void NavigationManager::SetView(const CameraView& view)
{
	m_view = view;
}

//*****
// Set Righthanded on/off
void NavigationManager::SetRightHanded(bool righthanded)
{
	if( m_camera.IsRightHanded() != righthanded )
	{
		m_camera.RightHanded(righthanded);
		m_view.m_camera_position.z = -m_view.m_camera_position.z;
		ApplyView();
	}
}

// Return the camera to world matrix
m4x4 NavigationManager::GetCameraToWorld() const
{
	return m_camera.GetCameraToWorld();
}

// Return the camera view matrix
const m4x4&	NavigationManager::GetWorldToCamera()
{
	if( m_lock_selection )
	{
		BoundingBox bbox;
		if( LineDrawer::Get().m_data_manager_GUI->GetSelectionBBox(bbox, true) )
		{
			m_camera.LookAt(bbox.Centre());
		}
	}
	return m_camera.GetWorldToCamera();
}

//*****
// Set the camera to view the top of the view volume
void NavigationManager::ViewTop()
{
	ApplyView();
	m_camera.SetPosition(m_view.m_lookat_centre + v4::make(0.0f, m_focus_dist, 0.0f, 0.0f));
	m_camera.LookAt     (m_view.m_lookat_centre, -v4ZAxis);
	LineDrawer::Get().RefreshWindowText();
}

//*****
// Set the camera to view the bottom of the view volume
void NavigationManager::ViewBottom()
{
	ApplyView();
	m_camera.SetPosition(m_view.m_lookat_centre + v4::make(0.0f, -m_focus_dist, 0.0f, 0.0f));
	m_camera.LookAt     (m_view.m_lookat_centre, v4ZAxis);
	LineDrawer::Get().RefreshWindowText();
}

//*****
// Set the camera to view the left side of the view volume
void NavigationManager::ViewLeft()
{
	ApplyView();
	m_camera.SetPosition(m_view.m_lookat_centre + v4::make(-m_focus_dist, 0.0f, 0.0f, 0.0f));
	m_camera.LookAt     (m_view.m_lookat_centre, v4YAxis);
	LineDrawer::Get().RefreshWindowText();
}

//*****
// Set the camera to view the right side of the view volume
void NavigationManager::ViewRight()
{
	ApplyView();
	m_camera.SetPosition(m_view.m_lookat_centre + v4::make(m_focus_dist, 0.0f, 0.0f, 0.0f));
	m_camera.LookAt     (m_view.m_lookat_centre, v4YAxis);
	LineDrawer::Get().RefreshWindowText();
}

//*****
// Set the camera to view the front of the view volume
void NavigationManager::ViewFront()
{
	ApplyView();
	m_camera.SetPosition(m_view.m_lookat_centre + v4::make(0.0f, 0.0f, m_focus_dist, 0.0f));
	m_camera.LookAt     (m_view.m_lookat_centre, v4YAxis);
	LineDrawer::Get().RefreshWindowText();
}

//*****
// Set the camera to view the back of the view volume
void NavigationManager::ViewBack()
{
	ApplyView();
	m_camera.SetPosition(m_view.m_lookat_centre + v4::make(0.0f, 0.0f, -m_focus_dist, 0.0f));
	m_camera.LookAt     (m_view.m_lookat_centre, v4YAxis);
	LineDrawer::Get().RefreshWindowText();
}

//*****
// Convert a screen space translation into a world space translation.
v4 NavigationManager::ConvertToWSTranslation(v2 const& vec, v4 const& ws_point) const
{
	IRect client_area = LineDrawer::Get().GetClientArea();
	float scalex = m_camera.GetViewProperty(Camera::Width)  / float(client_area.SizeX());
	float scaley = m_camera.GetViewProperty(Camera::Height) / float(client_area.SizeY());
	if( m_camera.Is3D() )
	{
		v4 pt = ws_point - m_camera.GetPosition();
		scalex *= pt.z / m_view.m_near;
		scaley *= pt.z / m_view.m_near;
	}
	
	return (vec.x * scalex) * m_camera.GetLeft()
		+	vec.y * scaley  * m_camera.GetUp();
}

//*****
// Convert a screen space 2D direction into an object rotation using a track ball style manipulation
m4x4 NavigationManager::ConvertToWSRotation(v2 vec, v2 point) const
{	
	IRect client_area = LineDrawer::Get().GetClientArea();
	float hwidth = client_area.SizeX() / 2.0f;
	float hheight= client_area.SizeY() / 2.0f;
	const float scale = 0.01f;

	if( !m_camera.IsRightHanded() ) vec.y = -vec.y;

	// Convert to screen space with centre in the middle of the screen
	point.x -= hwidth;
	point.y -= hheight;

	float xfraction = 0.5f * Cos(point.x * maths::pi / hwidth) + 0.5f;
	float yfraction = 0.5f * Cos(point.y * maths::pi / hheight) + 0.5f;

	float pitch	= -vec.y * xfraction * scale;
	float yaw	= -vec.x * yfraction * scale;
	float roll	= 0.0f;
	if( point.x > 0.0f ) roll += vec.y * (1.0f - xfraction);
	else				 roll -= vec.y * (1.0f - xfraction);
	if( point.y > 0.0f ) roll -= vec.x * (1.0f - yfraction);
	else				 roll += vec.x * (1.0f - yfraction);
	roll *= scale;
	
	return m4x4::make(pitch, yaw, roll, v4Origin);
}

//*****
// Convert a screen space 2d direction into a translation in the camera Z direction
v4 NavigationManager::ConvertToWSTranslationZ() const
{
	// not done yet
	return v4Origin;
}

//*****
// Translate the camera
void NavigationManager::Translate(v2 vec)
{
	if( IsZero2(vec) ) return;
	if( m_locks && m_locks[LockMask::CameraRelative] )
	{
		if( m_locks[LockMask::TransX] ) vec.x = 0.0f;
		if( m_locks[LockMask::TransY] ) vec.y = 0.0f;
	}
	
	IRect client_area = LineDrawer::Get().GetClientArea();
	float scalex = m_camera.GetViewProperty(Camera::Width)  / float(client_area.SizeX());
	float scaley = m_camera.GetViewProperty(Camera::Height) / float(client_area.SizeY());
	if( m_camera.Is3D() )
	{
		scalex *= m_focus_dist / m_view.m_near;
		scaley *= m_focus_dist / m_view.m_near;
	}

	v4 old_pos = m_camera.GetPosition();
	m_camera.DTranslateRel(-vec.x * scalex, vec.y * scaley, 0.0f);

	if( m_locks && !m_locks[LockMask::CameraRelative] )
	{		
		v4 new_pos = m_camera.GetPosition();
		if( m_locks[LockMask::TransX] ) new_pos.x = old_pos.x;
		if( m_locks[LockMask::TransY] ) new_pos.y = old_pos.y;
		if( m_locks[LockMask::TransZ] ) new_pos.z = old_pos.z;
		m_camera.SetPosition(new_pos);
	}
}

// Translate the camera in the z direction
void NavigationManager::TranslateZ(float delta)
{
	if( m_locks[LockMask::TransZ] ) return;

	// Move in a fraction of the focus distance
	float movez = m_focus_dist * delta / 1200.0f;
	m_camera.DTranslateRel(0.0f, 0.0f, movez);
}

//*****
// Move in/out
void NavigationManager::MoveZ(float delta)
{
	if( m_locks[LockMask::TransZ] ) return;

	// Move in a fraction of the focus distance
	float movez = m_focus_dist * delta / 1200.0f;
	m_camera.DTranslateRel(0.0f, 0.0f, movez);
	m_focus_dist += movez;
	m_camera.SetViewProperty(Camera::Near, CameraView::GetNearDist(m_focus_dist));
	m_camera.SetViewProperty(Camera::Far , CameraView::GetFarDist (m_focus_dist));
}

//*****
// Rotate the camera
void NavigationManager::Rotate(v2 vec, v2 point)
{
	if( IsZero2(vec) ) return;
	IRect client_area = LineDrawer::Get().GetClientArea();
	float hwidth = client_area.SizeX() / 2.0f;
	float hheight= client_area.SizeY() / 2.0f;
	const float scale = 0.01f;

	if( !m_camera.IsRightHanded() ) vec.y = -vec.y;

	// Convert to screen space with centre in the middle of the screen
	point.x -= hwidth;
	point.y -= hheight;

	float xfraction = 0.5f * Cos(point.x * maths::pi / hwidth) + 0.5f;
	float yfraction = 0.5f * Cos(point.y * maths::pi / hheight) + 0.5f;

	float pitch	= -vec.y * xfraction * scale;
	float yaw	= -vec.x * yfraction * scale;
	float roll	= 0.0f;
	if( point.x > 0.0f ) roll += vec.y * (1.0f - xfraction);
	else				 roll -= vec.y * (1.0f - xfraction);
	if( point.y > 0.0f ) roll -= vec.x * (1.0f - yfraction);
	else				 roll += vec.x * (1.0f - yfraction);
	roll *= scale;
	
	if( m_locks[LockMask::RotX] ) pitch	= 0.0f;
	if( m_locks[LockMask::RotY] ) yaw	= 0.0f;
	if( m_locks[LockMask::RotZ] ) roll	= 0.0f;
	
	m_camera.DRotateAbout(pitch, yaw, roll, GetFocusPoint());
}

//*****
// Zoom in/out
void NavigationManager::Zoom(float delta)
{
	if( m_locks[LockMask::Zoom] ) return;

	// Do the zooming
	float fov = ConvertFOV(m_camera.GetViewProperty(Camera::FOV), m_camera.Is3D(), false);
	fov *= 1.0f + delta / 100.0f;
	fov = Clamp(fov, maths::tiny, maths::pi);
	m_camera.SetViewProperty(Camera::FOV, ConvertFOV(fov, true, !m_camera.Is3D()));

	// Record the percentage of zoom for the window text
	m_zoom_fraction = m_view.m_fov / fov;
	LineDrawer::Get().RefreshWindowText();
}

//*****
// Zoom to an absolute amount
void NavigationManager::SetZoom(float fraction)
{
	if( m_locks[LockMask::Zoom] ) return;

	float fov = m_view.m_fov / fraction;
	fov = Clamp(fov, maths::tiny, maths::pi);
	m_camera.SetViewProperty(Camera::FOV, ConvertFOV(fov, true, !m_camera.Is3D()));

	// Record the percentage of zoom for the window text
	m_zoom_fraction = m_view.m_fov / fov;
	LineDrawer::Get().RefreshWindowText();
}

//*****
// Toggle between 2D and 3D
void NavigationManager::Set3D(bool _3d_on)
{
	m_camera.SetViewProperty(Camera::FOV, ConvertFOV(m_camera.GetViewProperty(Camera::FOV), m_camera.Is3D(), !_3d_on));
	//if( _3d_on && !m_camera.Is3D() )
	//{
	//	float tan_fov			= Tan(m_camera.GetViewProperty(Camera::FOV) / 2.0f);
	//	float width_at_near		= m_view.m_near * tan_fov;
	//	float new_fov			= 2.0f * ATan2(width_at_near, m_focus_dist);
	//	m_camera.SetViewProperty(Camera::FOV, new_fov);
	//}
	//else if( !_3d_on && m_camera.Is3D() )
	//{
	//	float tan_fov			= Tan(m_camera.GetViewProperty(Camera::FOV) / 2.0f);
	//	float width_at_focus	= m_focus_dist * tan_fov;
	//	float new_fov			= 2.0f * ATan2(width_at_focus, m_view.m_near);
	//	m_camera.SetViewProperty(Camera::FOV, new_fov);
	//}
	m_camera.Render3D(_3d_on);
}

//*****
// Toggle between stereo view and normal view
void NavigationManager::SetStereoView(bool on)
{
	IRect rect = LineDrawer::Get().GetClientArea();
	float aspect;
	if( on )	aspect = (0.5f * rect.SizeX()) / float(rect.SizeY());
	else		aspect =         rect.SizeX()  / float(rect.SizeY());
	m_camera.SetViewProperty(Camera::Aspect, aspect);
}

//*****
// Reposition the camera preserving the focus distance
void NavigationManager::RelocateCamera(const v4& position, const v4& forward, const v4& up)
{
	m_camera.SetPosition(position);
	m_camera.LookAt(position + forward, up);
}

// Apply wandering to the camera
void NavigationManager::WanderCamera()
{
	// Undo the last wander
	m_camera.DTranslateWorld(-m_camera_wander);

	// Calculate the next wander
	DWORD now = GetTickCount();
	m_camera_wander = Length3(m_camera_wander) * GetNormal3(v4::make(Cos(now/1000.0f), Sin(now/800.0f), Cos(now/500.0f), 0.0f));

	// Apply it
	m_camera.DTranslateWorld(m_camera_wander);
}

// Align the camera if necessary
void NavigationManager::AlignCamera(const v4& align_axis)
{
	if( !IsZero3(Cross3(m_camera.GetForward(), align_axis)) )
	{
		m_camera.SetUp(align_axis);
	}
}

//*****
// Turn on/off the inertial camera
void NavigationManager::SetCameraMode(ECameraMode mode)
{
	if( mode == m_camera_mode ) return;

	// Turn the existing camera off
	if( m_camera_mode != ECameraMode_Off )
	{
		m_camera_mode = ECameraMode_Off;
		delete m_camera_controller; m_camera_controller = 0;
		m_camera_poller.Stop();
		m_camera_poller.BlockTillDead();
	}
	
	// Select a new camera
	if( mode != ECameraMode_Off )
	{
		// Setup the camera controller
		CameraControllerSettings ccsettings;
		ccsettings.m_camera						= &m_camera;
		ccsettings.m_window_handle				= LineDrawer::Get().m_window_handle;
		ccsettings.m_app_instance				= LineDrawer::Get().m_app_instance;
		ccsettings.m_linear_acceleration		= LineDrawer::Get().m_data_manager.m_bbox.Diametre() * 0.005f;
		ccsettings.m_max_linear_velocity		= LineDrawer::Get().m_data_manager.m_bbox.Diametre() * 0.005f;
		ccsettings.m_rotational_acceleration	= 0.05f;
		ccsettings.m_max_rotational_velocity	= 2.0f;

		try
		{
			// Setup a polling thread to receive input
			if( !m_camera_poller.Start() ) throw pr::Exception(-1, "Failed to start camera polling thread");

			switch( mode )
			{
			case ECameraMode_FreeCam:			m_camera_controller = new camera::KeyboardFreeCamera(ccsettings); break;
			default: PR_ASSERT_STR(PR_DBG_LDR, false, "Unknown camera controller type");
			}
			m_camera_mode = mode;
		}
		catch(const pr::Exception& e)
		{
			LineDrawer::Get().m_error_output.Error(Fmt("Failed to start inertial camera. Reason: %s", e.m_message.c_str()).c_str());
			delete m_camera_controller; m_camera_controller = 0;
			m_camera_mode = ECameraMode_Off;
			m_camera_poller.Stop();
		}
	}
}

// Move the free camera
void NavigationManager::StepCamera()
{
	if( m_camera_mode != ECameraMode_Off )
	{
        uint now = (uint)GetTickCount();
		float elapsed_seconds = (now - m_free_cam_last_time) / 1000.0f;
		if( elapsed_seconds > 1.0f ) elapsed_seconds = 1.0f;
		m_free_cam_last_time = now;

		m_camera_controller->Step(elapsed_seconds);
		LineDrawer::Get().Refresh();
	}
}

//*****
// Returns the position of the focus point in world space
v4 NavigationManager::GetFocusPoint() const
{
	return m_camera.GetPosition() + m_camera.GetForward() * m_focus_dist;
}

//*****
// Return the distance to the focus point
float NavigationManager::GetFocusDistance() const
{
	return m_focus_dist;
}

//*****
// Return a string discribing our status
const char* NavigationManager::GetStatusString() const
{
	static char status[100];
	ZeroMemory(status, sizeof(status));
	
	// Only print something if Zoom is not 100%
	if( Abs(m_zoom_fraction - 1.0f) > 0.01f )
        _snprintf(status, 100, "Zoom: %3.0f", m_zoom_fraction * 100.0f);
	
	return status;
}

//*****
// Configures the camera with the view
void NavigationManager::ApplyView()
{
	m_camera.SetPosition(m_view.m_camera_position);
	m_camera.LookAt(m_view.m_lookat_centre, m_view.m_camera_up);
	m_camera.SetViewProperty(Camera::Near,		m_view.m_near);
	m_camera.SetViewProperty(Camera::Far,		m_view.m_far);
	m_camera.SetViewProperty(Camera::FOV,		ConvertFOV(m_view.m_fov, true, !m_camera.Is3D()));
	m_camera.SetViewProperty(Camera::Aspect,	m_view.m_aspect);
	m_focus_dist = Length3(m_view.m_camera_position - m_view.m_lookat_centre);

	m_zoom_fraction = 1.0f;
	LineDrawer::Get().RefreshWindowText();
}

//*****
// Convert a field of view so that the screen width stays the same when switching between
// 2d and 3d. 2d has the screen width of the 
float NavigationManager::ConvertFOV(float fov, bool is_3d, bool want_2d) const
{
	if( is_3d && want_2d )
	{
		float tan_fov			= Tan(fov / 2.0f);
		float width_at_focus	= m_focus_dist * tan_fov;
		return 2.0f * ATan2(width_at_focus, m_view.m_near);
	}
	else if( !is_3d && !want_2d )
	{
		float tan_fov			= Tan(fov / 2.0f);
		float width_at_near		= m_view.m_near * tan_fov;
		return 2.0f * ATan2(width_at_near, m_focus_dist);
	}
	return fov;
}

//*****
// Step the Camera Controller
bool NavigationManager::CameraPoller(void*)
{
	if( !LineDrawer::Get().IsBusy() )
	{
		PostMessage(LineDrawer::Get().m_window_handle, WM_COMMAND, ID_POLL_CAMERA, 0);
	}
	return false;
}
