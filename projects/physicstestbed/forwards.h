//******************************************
// Forwards
//******************************************
#pragma once

struct CPhysicsTestbedApp;
class PhysicsTestbed;
class CControls;
class CCollisionWatch;
class PhysicsEngine;
class SceneManager;
class Parser;
namespace parse
{
	struct Gfx;
	struct Terrain;
	struct Material;
	struct Gravity;
	struct Prim;
	struct Model;
	struct Deformable;
	struct Static;
	struct PhysObj;
	struct Multibody;
	struct Output;
}//namespace parse
namespace col
{
	struct Contact;
	struct Data;
}//namespace col
struct CollisionModel;
struct CollisionModelList;
struct DeformableModel;
struct Skeleton;
struct TestbedState;
class Static;
class Prop;

#if PHYSICS_ENGINE==RYLOGIC_PHYSICS
	#include "pr/Physics/Physics.h"
	#include "pr/geometry/Tetramesh.h"
	#include "pr/geometry/DeformableMesh.h"
	typedef pr::ph::Rigidbody					PhysObj;
	typedef pr::ph::ContactManifold				ColInfo;
	typedef pr::tetramesh::VIndex				VIndex;
	typedef pr::v4								VertType;
	inline	pr::v4 conv(pr::v4 const& v) { return v; }
#elif PHYSICS_ENGINE==REFLECTIONS_PHYSICS
	#include "Physics/Include/Physics.h"
	#include "Physics/Include/PhysicsDev.h"
	#include "PhysicsTestbed/PRtoRIconversions.h"
	typedef PHobject							PhysObj;
	typedef PHcollisionFrameInfo				ColInfo;
	typedef PHtetramesh::VIndex					VIndex;
	typedef PHv4								VertType;
	inline	pr::v4 conv(PHv4ref v) { return pr::no_step_into::mav4_to_v4(v); }
#endif

