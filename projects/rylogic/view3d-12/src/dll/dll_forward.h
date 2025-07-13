//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/view3d-dll.h"

namespace pr::view3d
{
	struct Includes;
}
namespace pr::rdr12
{
	using EStockObject = view3d::EStockObject;
	using ObjectSet = std::unordered_set<view3d::Object>;
	using GizmoSet  = std::unordered_set<view3d::Gizmo>;
	using GuidSet   = std::unordered_set<Guid>;
}
