//*********************************************
// Physics engine
//  Copyright © Rylogic Ltd 2006
//*********************************************

#pragma once
#ifndef PR_PHYSICS_H
#define PR_PHYSICS_H

#define PR_PHYSICS_INTERFACE_INCLUDE 1

#pragma warning (push)
#pragma warning (disable: 4996)

// Types
#include "pr/physics/types/forward.h"

// Engine
#include "pr/physics/engine/settings.h"
#include "pr/physics/engine/engine.h"
#include "pr/physics/engine/igravity.h"

// Broadphase
#include "pr/physics/broadphase/ibroadphase.h"
#include "pr/physics/broadphase/broadphasebrute.h"
#include "pr/physics/broadphase/broadphasesnp.h"

// Collision
#include "pr/physics/collision/icollisionobserver.h"
#include "pr/physics/collision/collider.h"
#include "pr/physics/collision/contactmanifold.h"

// Ray casts
#include "pr/physics/ray/raycast.h"

// Terrain
#include "pr/physics/terrain/iterrain.h"
#include "pr/physics/terrain/terrainplane.h"
#include "pr/physics/terrain/terrainimplicitsurface.h"

// Material
#include "pr/physics/material/imaterial.h"

// Shape
#include "pr/physics/shape/shapes.h"
#include "pr/physics/shape/builder/shapebuilder.h"
#include "pr/physics/shape/builder/shapepolytopehelper.h"

// Rigid body
#include "pr/physics/rigidbody/rigidbody.h"

// Utility
#include "pr/physics/utility/events.h"
#include "pr/physics/utility/globalfunctions.h"
#include "pr/physics/utility/ldrhelper.h"

#pragma warning (pop)

#undef PR_PHYSICS_INTERFACE_INCLUDE
#endif
