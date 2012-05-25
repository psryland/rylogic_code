//*********************************************
// The Navigation manager interprets user input and is used to set up the
// projection matrix in the renderer
//	(C)opyright Rylogic Limited 2007
//*********************************************

#ifndef NAVIGATIONMANAGER_H
#define NAVIGATIONMANAGER_H

#include "pr/maths/maths.h"
#include "pr/common/PollingToEvent.h"
#include "pr/camera/Camera.h"
#include "pr/camera/ICameraController.h"
#include "LineDrawer/Source/CameraView.h"
#include "LineDrawer/Source/LockMask.h"

enum ECameraMode
{
	ECameraMode_Off,
	ECameraMode_FreeCam,
};

class NavigationManager
{
public:
	NavigationManager();
	~NavigationManager();
	
	void	Resize(const IRect& client_area);
	const Frustum&	GetViewFrustum() const					{ return m_camera.GetViewFrustum(); }
	m4x4			GetCameraToScreen() const				{ return m_camera.GetCameraToScreen(); }
	m4x4			GetCameraToWorld() const;
	const m4x4&		GetWorldToCamera();
	
	ldr::CameraData GetCameraData() const;
	void	SetView(const BoundingBox& bbox);
	void	SetView(const CameraView& view);
	void	ApplyView();
	void	ViewTop();
	void	ViewBottom();
	void	ViewLeft();
	void	ViewRight();
	void	ViewFront();
	void	ViewBack();
	
	bool	IsVisible(const BoundingBox& bbox) const		{ return m_camera.IsVisible(bbox); }
	void	LookAtViewCentre()								{ m_camera.LookAt(m_view.m_lookat_centre); }
	void	SetLockMask(LockMask locks)						{ m_locks = locks; }
	LockMask GetLockMask() const							{ return m_locks; }
	void	LockToSelection(bool locked)					{ m_lock_selection = locked; }
	void	SetCameraWander(float radius)					{ m_camera_wander = radius * v4XAxis; m_camera.DTranslateRel(m_camera_wander); }
	void	SetRightHanded(bool righthanded);
	v4		ConvertToWSTranslation(v2 const& vec, v4 const& ws_point) const;
	m4x4	ConvertToWSRotation(v2 vec, v2 point) const;
	v4		ConvertToWSTranslationZ() const;
	void	Translate(v2 vec);
	void	TranslateZ(float delta);
	void	MoveZ(float delta);
	void	Rotate(v2 vec, v2 point);
	void	Zoom(float delta);
	void	SetZoom(float fraction);
	void	Set3D(bool _3d_on);
	void	SetStereoView(bool on);
	void	RelocateCamera(const v4& position, const v4& forward, const v4& up);
	void	WanderCamera();
	void	AlignCamera(const v4& align_axis);
	v4		GetFocusPoint() const;
	float	GetFocusDistance() const;
	void	SetCameraMode(ECameraMode mode);
	void	StepCamera();
	const char* GetStatusString() const;
	
	Camera	m_camera;

private:
	static PollingToEventSettings CameraPollerSettings(void* user_data);
	static bool CameraPoller(void* user);
	float	ConvertFOV(float fov, bool is_3d, bool want_2d) const;

private:
	CameraView					m_view;										// The camera starting point and properties. Not modified by user mouse input
	ICameraController*			m_camera_controller;
	PollingToEvent				m_camera_poller;
	ECameraMode					m_camera_mode;								//
	v4							m_camera_wander;							// Camera wander 
	float						m_focus_dist;								// The distance to the focus point
	float						m_zoom_fraction;							// The ratio of the normal fov over the current fov
	LockMask					m_locks;									// Camera translation/rotation locks
	bool						m_lock_selection;
	unsigned int				m_free_cam_last_time;						// Time variable used to step the free camera
};

#endif//NAVIGATIONMANAGER_H