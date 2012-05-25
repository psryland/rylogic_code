//******************************************************
//
//	CameraView
//
//******************************************************
//	A "View" is a starting point for the camera.
//	Clients ask the nav manager to view a bbox. This puts the camera along the Z axis from
//	the centre of the bbox. Navigation moves/zooms the camera from this position.

#ifndef PR_LINEDRAWER_LOCK_MASK_H
#define PR_LINEDRAWER_LOCK_MASK_H

#include "pr/maths/maths.h"
#include <bitset>

struct LockMask : std::bitset<8>
{
	enum
	{
		TransX			= 0,
		TransY			= 1,
		TransZ			= 2,
		RotX			= 3,
		RotY			= 4,
		RotZ			= 5,
		Zoom			= 6,
		CameraRelative	= 7,
		All				= (1 << 7) - 1, // Not including camera relative
	};
	operator bool() const { return (to_ulong() & All) != 0; }
};

#endif//PR_LINEDRAWER_LOCK_MASK_H
