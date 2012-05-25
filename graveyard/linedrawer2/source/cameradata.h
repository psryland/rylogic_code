//******************************************************
//
//	CameraView
//
//******************************************************
//	A "View" is a starting point for the camera.
//	Clients ask the nav manager to view a bbox. This puts the camera along the Z axis from
//	the centre of the bbox. Navigation moves/zooms the camera from this position.

#ifndef PR_LINEDRAWER_CAMERA_DATA_H
#define PR_LINEDRAWER_CAMERA_DATA_H

#include "pr/maths/maths.h"

namespace pr
{
	namespace ldr
	{
		struct CameraData
		{
			pr::v4	m_camera_position;
			pr::v4	m_lookat_centre;
			pr::v4	m_camera_up;
			float	m_near;
			float	m_far;
			float	m_fov;
			float	m_aspect;
			float	m_width;
			float	m_height;
			float	m_focus_dist;
			bool	m_is_3d;
		};
	}//namespace ldr
}//namespace pr

#endif//PR_LINEDRAWER_CAMERA_DATA_H
