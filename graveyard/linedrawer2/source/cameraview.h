//******************************************************
//
//	CameraView
//
//******************************************************
//	A "View" is a starting point for the camera.
//	Clients ask the nav manager to view a bbox. This puts the camera along the Z axis from
//	the centre of the bbox. Navigation moves/zooms the camera from this position.

#ifndef PR_LINEDRAWER_CAMERA_VIEW_H
#define PR_LINEDRAWER_CAMERA_VIEW_H

#include <bitset>
#include "pr/maths/maths.h"
#include "LineDrawer/Source/CameraData.h"

struct ViewMask : std::bitset<14>
{
	enum
	{
		PositionX		= 0,
		PositionY		= 1,
		PositionZ		= 2,
		UpX				= 3,
		UpY				= 4,
		UpZ				= 5,
		LookAt			= 6,
		FOV				= 7,
		Aspect			= 8,
		Near			= 9,
		Far				= 10,
		AlignX			= 11,
		AlignY			= 12,
		AlignZ			= 13,
	};
	operator bool() const { return to_ulong() != 0; }
};

struct CameraView : pr::ldr::CameraData
{
	CameraView();
	static float GetNearDist(float focus_dist);
	static float GetFarDist(float focus_dist);
	void CreateFromBBox(const BoundingBox& bbox, const IRect& client_area);
	void SetAspect(const IRect& client_area);
};

#endif//PR_LINEDRAWER_CAMERA_VIEW_H
