//******************************************************
//
//	CameraView
//
//******************************************************
//	A "View" is a starting point for the camera.
//	Clients ask the nav manager to view a bbox. This puts the camera along the Z axis from
//	the centre of the bbox. Navigation moves/zooms the camera from this position.

#include "Stdafx.h"
#include "LineDrawer/Source/CameraView.h"

//*****
// Constructor
CameraView::CameraView()
{
	m_camera_position	.set(0.0f, 0.0f, 10.0f, 1.0f);
	m_lookat_centre		= v4Origin;
	m_camera_up			= v4YAxis;
	m_near				= 0.01f;
	m_far				= 100.0f;
	m_fov				= maths::pi / 4.0f;
	m_aspect			= 1.0f;
}

// Convert the focus distance into near and far distances
float CameraView::GetNearDist(float focus_dist)
{
	float const dist_to_near_ratio = 0.01f;
	return focus_dist * dist_to_near_ratio;
}
float CameraView::GetFarDist(float focus_dist)
{
	float const dist_to_far_ratio  = 100.0f;
	return focus_dist * dist_to_far_ratio;
}

//*****
// Create a view that encompases a bounding box
void CameraView::CreateFromBBox(const BoundingBox& bbox, const IRect& client_area)
{
	PR_ASSERT(PR_DBG_LDR, bbox.IsValid());

	const float FOV		= maths::pi/4.0f;
	const float TanFOV	= Tan(FOV/2.0f);	// Tan of the half field of view

	float objectW		= bbox.SizeX() * 1.2f;
	float objectH		= bbox.SizeY() * 1.2f;
	float objectD		= bbox.SizeZ() * 1.2f;
	float biggestXY		= (objectW   > objectH) ? (objectW)   : (objectH);
	//float biggestXYZ	= (biggestXY > objectD) ? (biggestXY) : (objectD);

	m_lookat_centre		= bbox.Centre();
	m_camera_position	= m_lookat_centre;
	m_camera_position[2]+= (objectD * 0.5f) + (biggestXY * 0.5f) / TanFOV;
	m_camera_up			= v4YAxis;
	float focus_dist	= Length3(m_camera_position - m_lookat_centre);
	m_near				= GetNearDist(focus_dist);
	m_far				= GetFarDist(focus_dist);
	m_fov				= FOV;
	m_aspect			= client_area.SizeX() / float(client_area.SizeY());
}

//*****
// Set the aspect ratio based on a window client area
void CameraView::SetAspect(const IRect& client_area)
{
	m_aspect = client_area.SizeX() / float(client_area.SizeY());
}
