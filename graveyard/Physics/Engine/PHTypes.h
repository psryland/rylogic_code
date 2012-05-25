//****************************************
//
//	Enums used by the physics engine
//
//****************************************

#ifndef PR_PHYSICS_TYPES_H
#define PR_PHYSICS_TYPES_H

namespace pr
{
	namespace ph
	{
		enum Axis
		{
			X = 0,
			Y = 1,
			Z = 2,
			W = 3
		};
		enum CollisionResponce
		{
			NoCollision,
			ZerothOrderCollision,
			FirstOrderCollision,
			NumberOf
		};
	}//namespace ph
}//namespace pr

#endif//PR_PHYSICS_TYPES_H
