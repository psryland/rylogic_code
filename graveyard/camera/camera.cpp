//*********************************************
// Camera - A class to manage the view transform
//	(c)opyright Rylogic Limited 2007
//*********************************************
#include "Camera.h"

using namespace pr;

//*****
// Constructor
Camera::Camera(const CameraSettings& settings)
:m_settings(settings)
,m_left(-v4XAxis)
,m_up(v4YAxis)
,m_forward(-v4ZAxis)
,m_velocity(v4Zero)
,m_rot_velocity(v4Zero)
,m_camera_moved(true)
,m_world_to_camera_changed(true)
,m_camera_to_screen_changed(true)
{
	m_lock_axis[0] = m_lock_axis[1] = m_lock_axis[2] = false;
	m_settings.MakeSelfConsistent();

	m_frustum.setWHN(m_settings.m_width, m_settings.m_height, m_settings.m_near);
	m_camera_moved = true;
	m_world_to_camera_changed = true;
	m_camera_to_screen_changed = true;
}

//*****
// Return the matrix that describes the cameras position in world space. i.e. Camera to World transform
const m4x4& Camera::GetCameraToWorld() const
{
	m_camera_to_world;
	if( m_camera_moved )
	{
		m_camera_to_world.set(m_settings.m_orientation, m_settings.m_position);
		m_camera_moved = false;
		m_world_to_camera_changed = true;
	}
	return m_camera_to_world;
}

//*****
// Return the matrix that describes world space in camera space. i.e. World to Camera transform
const m4x4& Camera::GetWorldToCamera() const
{
	if( m_world_to_camera_changed || m_camera_moved )
	{
		m_world_to_camera = GetCameraToWorld();
		InverseFast(m_world_to_camera);
		m_world_to_camera_changed = false;
	}
	return m_world_to_camera;
}

//*****
// Return a Projection Matrix for this camera.
const m4x4& Camera::GetCameraToScreen() const
{
	if( m_camera_to_screen_changed )
	{
		if( m_settings.m_3dcamera )
		{
			if( m_settings.m_use_FOV_for_perspective )
			{
				ProjectionPerspectiveFOV(	m_camera_to_screen,
											m_settings.m_fov,
											m_settings.m_aspect,
											m_settings.m_near,
											m_settings.m_far,
											m_settings.m_righthanded);
			}
			else
			{
				ProjectionPerspective(	m_camera_to_screen,
										m_settings.m_width,
										m_settings.m_height,
										m_settings.m_near,
										m_settings.m_far,
										m_settings.m_righthanded);
			}
		}
		else
		{
			ProjectionOrthographic(	m_camera_to_screen,
									m_settings.m_width,
									m_settings.m_height,
									m_settings.m_near,
									m_settings.m_far,
									m_settings.m_righthanded);
		}

		m_frustum.setWHN(m_settings.m_width, m_settings.m_height, m_settings.m_near);
		m_camera_to_screen_changed = false;
	}
	return m_camera_to_screen;
}

//*****
// Point the camera at a target
void Camera::LookAt(const v4& target, const v4& up)
{
	PR_ASSERT_STR(PR_DBG_CAMERA, !(m_settings.m_position == target), "Camera is on the target");
	PR_ASSERT_STR(PR_DBG_CAMERA, !IsZero3(Cross3(target - m_settings.m_position, up)), "Direction for 'up' is the same as to the target");
	if( m_settings.m_position == target ) { m_settings.m_position = target / 2.0f; }

	m4x4 rotation; rotation.identity();
	v4& xaxis = rotation[0];
	v4& yaxis = rotation[1];
	v4& zaxis = rotation[2];

	zaxis = GetNormal3(m_settings.m_position - target);
	if( m_settings.m_righthanded )
	{
		xaxis = GetNormal3(Cross3(up, zaxis));
		yaxis = Cross3(zaxis, xaxis);
	}
	else
	{
		xaxis = GetNormal3(Cross3(zaxis, up));
		zaxis = Cross3(xaxis, zaxis);
	}
	m_settings.m_orientation.set(rotation);
	SetLeftUpForwardVectors();
	m_camera_moved = true;
}

//*****
// Change a view property
void Camera::SetViewProperty(ViewProperty prop, float value)
{
	switch( prop )
	{
	case Near:		m_settings.m_near	= value; break;
	case Far:		m_settings.m_far	= value; break;
	case Width:		m_settings.m_width	= value; m_settings.WidthOrHeightChanged(); break;
	case Height:	m_settings.m_height = value; m_settings.WidthOrHeightChanged(); break;
	case FOV:		m_settings.m_fov	= value; m_settings.AspectOrFOVChanged();   break;
	case Aspect:	m_settings.m_aspect = value; m_settings.AspectOrFOVChanged();   break; 
	default: PR_ASSERT_STR(PR_DBG_CAMERA, false, "Unknown view property");
	};
	m_camera_to_screen_changed = true;
}

//*****
// Return a view property
float Camera::GetViewProperty(ViewProperty prop) const
{
	switch( prop )
	{
	case Width:		return m_settings.m_width;
	case Height:	return m_settings.m_height;
	case Near:		return m_settings.m_near;
	case Far:		return m_settings.m_far;
	case FOV:		return m_settings.m_fov;
	case Aspect:	return m_settings.m_aspect;
	default: PR_ASSERT_STR(PR_DBG_CAMERA, false, "Unknown view property"); return 0.0f;
	};
}

//*****
// Convert a screen coordinate into a world space co-ordinate
// The x, y components of 'screen' are the screen space coordinates from top left (0,0) -> bottom right (1,1)
// The z component should be between 0.0f and 1.0f where 0.0f is on the near clip plane and 1.0f is on the far clip plane
v4 Camera::ScreenToWorld(v4 screen) const
{
	// Convert screen into near clipping plane space
	screen.x = (screen.x - 0.5f) * m_settings.m_width;
	screen.y = (screen.y - 0.5f) * m_settings.m_height;
	screen.z = screen.z * (m_settings.m_far - m_settings.m_near) + m_settings.m_near;

	screen.x *= m_settings.m_aspect * screen.z / m_settings.m_near;
	screen.y *= m_settings.m_aspect * screen.z / m_settings.m_near;
	return	m_settings.m_position - m_left * screen.x - m_up * screen.y + m_forward * screen.z;
}

//*****
// Rotate about our axis
void Camera::DRotateRel(const v4& by)
{
	if (IsZero3(by)) return;
	m_camera_moved = true;

	Quat pitch;	pitch.set(m_left,		by[0]);
	Quat yaw;	yaw  .set(m_up,			by[1]);
	Quat roll;	roll .set(m_forward,	by[2]);

	m_settings.m_orientation = m_settings.m_orientation * GetNormal(pitch * yaw * roll);
	SetLeftUpForwardVectors();
}
	
//*****
// Rotate about a point
void Camera::DRotateAbout(const v4& by, const v4& point)
{
	if (IsZero3(by)) return;
	m_camera_moved = true;

	v4 point_to_camera	= m_settings.m_position - point;
	v4 point_up			= Cross3(m_left, point_to_camera);
	v4 point_left		= Cross3(point_up, point_to_camera);
	Normalise3(point_to_camera);
	Normalise3(point_up);
	Normalise3(point_left);

	Quat pitch;	pitch.set(point_left,		by[0]);
	Quat yaw;	yaw  .set(point_up,			by[1]);
	Quat roll;	roll .set(point_to_camera,	by[2]);

	Quat rot = GetNormal(pitch * yaw * roll);
	point_to_camera	= m_settings.m_position - point;
	point_to_camera = Rotate(rot, point_to_camera);
	m_settings.m_position = point + point_to_camera;
	m_settings.m_orientation = rot * m_settings.m_orientation;
	SetLeftUpForwardVectors();
}

//*****
// Update the camera's position and orientation
void Camera::Update(float elapsed_seconds)
{
	if ((IsZero3(m_velocity) && IsZero3(m_rot_velocity)) || elapsed_seconds == 0.0f) return;
	m_camera_moved = true;

	// Update the camera position
	v4 velocity		= m_velocity	 * elapsed_seconds;
	if( m_lock_axis[0] ) velocity[0] = 0.0f;
	if( m_lock_axis[1] ) velocity[1] = 0.0f;
	if( m_lock_axis[2] ) velocity[2] = 0.0f;

	m_settings.m_position += velocity;
	m_settings.m_orientation = m_settings.m_orientation * Quat::make(m_left,	m_rot_velocity[0] * elapsed_seconds); // pitch
	m_settings.m_orientation = m_settings.m_orientation * Quat::make(m_up,		m_rot_velocity[1] * elapsed_seconds); // yaw
	m_settings.m_orientation = m_settings.m_orientation * Quat::make(m_forward,	m_rot_velocity[2] * elapsed_seconds); // roll
	SetLeftUpForwardVectors();
}

//*****
// Set the Left, Up, and Forward vectors based on the current m_orientation
void Camera::SetLeftUpForwardVectors()
{
	Normalise(m_settings.m_orientation);
	m_left		.set(-1.0f,  0.0f,  0.0f, 0.0f);
	m_up		.set( 0.0f,  1.0f,  0.0f, 0.0f);
	m_forward	.set( 0.0f,  0.0f, -1.0f, 0.0f);
	m_left		= Rotate(m_settings.m_orientation, m_left);
	m_up		= Rotate(m_settings.m_orientation, m_up);
	m_forward	= Rotate(m_settings.m_orientation, m_forward);
}
