//*********************************************
// Camera - A class to manage the view transform
//	(C)opyright Rylogic Limited 2007
//*********************************************
#ifndef PR_CAMERA_H
#define PR_CAMERA_H

#include "pr/maths/maths.h"
#include "pr/common/PRTypes.h"
#include "pr/common/assert.h"
#include <d3d9.h>
#include <d3dx9.h>

#ifndef PR_DBG_CAMERA
	#define PR_DBG_CAMERA  PR_DBG_COMMON
#endif//PR_DBG_CAMERA

namespace pr
{
	struct CameraSettings
	{
		CameraSettings()
		:m_orientation(QuatIdentity)
		,m_position(v4Origin)
		,m_near(0.01f)
		,m_far(100.0f)
		,m_use_FOV_for_perspective(true)
		,m_fov(maths::pi / 4.0f)
		,m_aspect(1.0f)
		,m_width(1.0f)
		,m_height(1.0f)
		,m_righthanded(true)
		,m_3dcamera(true)
		{}
		void	WidthOrHeightChanged()		{ m_fov = 2.0f * ATan2(m_width / 2.0f, m_near); m_aspect = m_width / m_height; }
		void	AspectOrFOVChanged()		{ m_width = 2.0f * m_near * Tan(m_fov / 2.0f); m_height = m_width / m_aspect; }
		void	MakeSelfConsistent()		{ if( m_use_FOV_for_perspective )	AspectOrFOVChanged(); else WidthOrHeightChanged(); }
		
		Quat	m_orientation;
		v4		m_position;
		float	m_near;
		float	m_far;
		bool	m_use_FOV_for_perspective;
		float	m_fov;
		float	m_aspect;
		float	m_width;					// The width at the near clip plane
		float	m_height;					// The height at the near clip plane
		bool	m_righthanded;
		bool	m_3dcamera;
	};

	//**********************************************************************************
	// A base class for managing the view and projection matrices
	class Camera
	{
	public:
		Camera(const CameraSettings& settings);
		virtual ~Camera() {}
		
		enum Axis			{ X = 0, Y = 1, Z = 2, NumberOfAxes };
		enum Angle			{ Pitch = 0, Yaw = 1, Roll = 2 };
		enum ViewProperty	{ Width, Height, Near, Far, FOV, Aspect };

		// Accessor methods
		const v4&	GetPosition()	const 						{ return m_settings.m_position;	}
		const v4&	GetForward()	const						{ return m_forward; }
		const v4&	GetLeft()		const						{ return m_left; }
		const v4&	GetUp()			const						{ return m_up; }
		const Frustum& GetViewFrustum() const					{ return m_frustum; }
		const m4x4&	GetCameraToWorld() const;
		const m4x4&	GetWorldToCamera() const;
		const m4x4&	GetCameraToScreen()	const;

		bool	Is3D() const									{ return m_settings.m_3dcamera; }
		bool	IsRightHanded() const							{ return m_settings.m_righthanded; }
		void	Render3D(bool _3d_on)							{ m_settings.m_3dcamera = _3d_on; m_camera_to_screen_changed = true; }
		void	RightHanded(bool righthanded)					{ m_settings.m_righthanded = righthanded; }
		void	LockAxis(Axis which, bool locked)				{ m_lock_axis[which] = locked; } //e.g.	m_camera.LockAxis(Camera::Axis::X, true);
		void	SetPosition(const v4& pos)						{ m_settings.m_position = pos;	}
		void	SetPosition(float x, float y, float z)			{ SetPosition(v4::make(x, y, z, 1.0f)); }
		void	SetUp(const v4& up)								{ LookAt(GetPosition() + GetForward(), up); }
		void	LookAt(const v4& target, const v4& up);
		void	LookAt(const v4& target)						{ LookAt(target, GetUp()); }
		void	LookAt(float tx, float ty, float tz)			{ LookAt(v4::make(tx, ty, tz, 1.0f), GetUp()); }
		void	LookAt(float tx, float ty, float tz, float ux, float uy, float uz){ LookAt(v4::make(tx, ty, tz, 1.0f), v4::make(ux, uy, uz, 0.0f)); }
		void	SetViewProperty(ViewProperty prop, float value);
		float	GetViewProperty(ViewProperty prop) const;
		v4		ScreenToWorld(v4 screen) const;
		bool	IsVisible(const BoundingBox& bbox) const		{ return IsWithin(m_frustum, GetWorldToCamera() * bbox); }
		
		// Direct camera movement methods
		void	DTranslateRel(const v4& by)						{ if(IsZero3(by)) return; m_camera_moved = true; m_settings.m_position += Rotate(m_settings.m_orientation, by); }
		void	DTranslateRel(float x, float y, float z)		{ DTranslateRel(v4::make(x, y, z, 0.0f)); }
		void	DTranslateWorld(const v4& by)					{ if(IsZero3(by)) return; m_camera_moved = true; m_settings.m_position += by; }
		void	DTranslateWorld(float x, float y, float z)		{ DTranslateWorld(v4::make(x, y, z, 0.0f)); }
		void	DRotateRel(const v4& by);
		void	DRotateRel(float pitch, float yaw, float roll)	{ DRotateRel(v4::make(pitch, yaw, roll, 0.0f)); }
		void	DRotateWorld(const v4& by)						{ by; PR_ASSERT_STR(PR_DBG_CAMERA, false, "Not Implemented"); }
		void	DRotateWorld(float pitch, float yaw, float roll){ DRotateWorld(v4::make(pitch, yaw, roll, 0.0f)); }
		void	DRotateAbout(const v4& by, const v4& point);
		void	DRotateAbout(float pitch, float yaw, float roll, const v4& point) { DRotateAbout(v4::make(pitch, yaw, roll, 0.0f), point); }

		// Velocity camera control methods
		void	VTranslateRel(const v4& by)						{ m_velocity = Rotate(m_settings.m_orientation, by); }
		void	VTranslateRel(float x, float y, float z)		{ VTranslateRel(v4::make(x, y, z, 0.0f)); }
		void	VTranslateWorld(const v4& by)					{ m_velocity = by; }
		void	VTranslateWorld(float x, float y, float z)		{ VTranslateWorld(v4::make(x, y, z, 0.0f)); }
		void	VRotateRel(const v4& by)						{ by; PR_ASSERT_STR(PR_DBG_CAMERA, false, "Not Implemented"); }
		void	VRotateRel(float pitch, float yaw, float roll)	{ VRotateRel(v4::make(pitch, yaw, roll, 0.0f)); }
		void	VRotateWorld(const v4& by)						{ by; PR_ASSERT_STR(PR_DBG_CAMERA, false, "Not Implemented"); }
		void	VRotateWorld(float pitch, float yaw, float roll){ VRotateWorld(v4::make(pitch, yaw, roll, 0.0f)); }

		// Acceleration camera control methods
		void	ATranslateRel(const v4& by)						{ m_velocity += Rotate(m_settings.m_orientation, by); }
		void	ATranslateRel(float x, float y, float z)		{ VTranslateRel(v4::make(x, y, z, 0.0f)); }
		void	ATranslateWorld(const v4& by)					{ m_velocity += by; }
		void	ATranslateWorld(float x, float y, float z)		{ VTranslateWorld(v4::make(x, y, z, 0.0f)); }
		void	ARotateRel(const v4& by)						{ m_rot_velocity += by; }
		void	ARotateRel(float pitch, float yaw, float roll)	{ VRotateRel(v4::make(pitch, yaw, roll, 0.0f)); }
		void	ARotateWorld(const v4& by)						{ by; PR_ASSERT_STR(PR_DBG_CAMERA, false, "Not Implemented"); }
		void	ARotateWorld(float pitch, float yaw, float roll){ ARotateWorld(v4::make(pitch, yaw, roll, 0.0f)); }

		// Slow the camera down
		void	Stop()											{ Zero(m_velocity); Zero(m_rot_velocity); }
		void	Drag(float percentage)							{ m_velocity *= percentage;	}
		void	RotDrag(float percentage)						{ m_rot_velocity *= percentage; }

		// Actually move the camera
		void	Update(float elapsed_seconds);

	protected:
		void	SetLeftUpForwardVectors();

	protected:
		CameraSettings m_settings;
		v4		m_left;
		v4		m_up;
		v4		m_forward;
		
		v4		m_velocity;			// dx, dy, dz
		v4		m_rot_velocity;		// pitch, yaw, roll
		bool	m_lock_axis[3];

		// Matrix caching members
		mutable bool m_camera_moved;
		mutable m4x4 m_camera_to_world;
		mutable bool m_world_to_camera_changed;
		mutable m4x4 m_world_to_camera;
		mutable bool m_camera_to_screen_changed;
		mutable m4x4 m_camera_to_screen;
		mutable Frustum	m_frustum;
	};

}//namespace pr

#endif//PR_CAMERA_H

	/*****
	// Inertial camera handling
	void DoCameraControl()
	{
		D3DXVECTOR3 accel(0.0f, 0.0f, 0.0f);
		D3DXVECTOR3 rot(0.0f, 0.0f, 0.0f);
		float l_scale = 0.3f;
		float a_scale = 0.1f;

		if( m_input.KeyDown(DIK_S) )	m_camera.Stop();
		if( m_input.KeyDown(DIK_L) )	m_camera.LookAt(D3DXVECTOR3(0.0f, 0.0f, 0.0f), D3DXVECTOR3(0.0f, 1.0f, 0.0f));
		if( m_input.KeyDown(DIK_Z) )		accel[Camera::Y]	+= -1.0f;
		if( m_input.KeyDown(DIK_A) )		accel[Camera::Y]	+=  1.0f;
		if( m_input.KeyDown(DIK_X) )		rot[Camera::Roll]+= -1.0f;
		if( m_input.KeyDown(DIK_C) )		rot[Camera::Roll]+=  1.0f;

		if( m_input.KeyDown(DIK_LSHIFT) || m_input.KeyDown(DIK_RSHIFT) )	// Linear
		{
			if( m_input.KeyDown(DIK_LEFT) )		accel[Camera::X] += -1.0f;
			if( m_input.KeyDown(DIK_RIGHT) )	accel[Camera::X] +=  1.0f;
			if( m_input.KeyDown(DIK_UP) )		accel[Camera::Z] += -1.0f;
			if( m_input.KeyDown(DIK_DOWN) )		accel[Camera::Z] +=  1.0f;
		}
		else								// Rotational
		{
			if( m_input.KeyDown(DIK_LEFT) )		rot[Camera::Yaw]	+=  1.0f;
			if( m_input.KeyDown(DIK_RIGHT) )	rot[Camera::Yaw]	+= -1.0f;
			if( m_input.KeyDown(DIK_UP) )		rot[Camera::Pitch]	+=  1.0f;
			if( m_input.KeyDown(DIK_DOWN) )		rot[Camera::Pitch]	+= -1.0f;
		}

		if( m_input.KeyDown(DIK_LCONTROL) || m_input.KeyDown(DIK_RCONTROL) )	{ l_scale = 3.0f; a_scale = 0.2f;	}
		if( m_input.KeyDown(DIK_CAPSLOCK) )										{ l_scale *= 10.0f;					}
		
		m_camera.AccelRel(accel * l_scale);
		m_camera.Rotate(rot * a_scale);
		m_camera.Update(m_elapsed_seconds);
		m_camera.Drag(0.95f);
		m_camera.RotDrag(0.95f);
	}
	*/