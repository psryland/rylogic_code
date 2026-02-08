//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/utility/conversion.h"
#include "pr/view3d-12/instance/instance.h"
#include "pr/view3d-12/ldraw/ldraw_object.h"

namespace pr
{
	// rdr12::HitTestRay / view3d::HitTestRay
	rdr12::HitTestRay Convert<rdr12::HitTestRay, view3d::HitTestRay>::Func(view3d::HitTestRay h)
	{
		return rdr12::HitTestRay{
			.m_ws_origin = To<v4>(h.m_ws_origin),
			.m_ws_direction = To<v4>(h.m_ws_direction),
			.m_snap_mode = To<rdr12::ESnapMode>(h.m_snap_mode),
			.m_snap_distance = h.m_snap_distance,
			.m_id = static_cast<int>(h.m_id),
		};
	}

	// rdr12::HitTestReset / view3d::HitTestReset
	rdr12::HitTestResult Convert<rdr12::HitTestResult, view3d::HitTestResult>::Func(view3d::HitTestResult const& hit)
	{
		return rdr12::HitTestResult{
			.m_ws_ray_origin = To<v4>(hit.m_ws_ray_origin),
			.m_ws_ray_direction = To<v4>(hit.m_ws_ray_direction),
			.m_ws_intercept = To<v4>(hit.m_ws_intercept),
			.m_ws_normal = To<v4>(hit.m_ws_normal),
			.m_instance = reinterpret_cast<rdr12::BaseInstance const*>(hit.m_obj),
			.m_distance = hit.m_distance,
			.m_ray_index = hit.m_ray_index,
			.m_ray_id = hit.m_ray_id,
			.m_snap_type = static_cast<rdr12::ESnapType>(hit.m_snap_type),
		};
	}
	view3d::HitTestResult Convert<view3d::HitTestResult, rdr12::HitTestResult>::Func(rdr12::HitTestResult const& hit)
	{
		return view3d::HitTestResult{
			.m_ws_ray_origin = To<view3d::Vec4>(hit.m_ws_ray_origin),
			.m_ws_ray_direction = To<view3d::Vec4>(hit.m_ws_ray_direction),
			.m_ws_intercept = To<view3d::Vec4>(hit.m_ws_intercept),
			.m_ws_normal = To<view3d::Vec4>(hit.m_ws_normal),
			.m_obj = const_cast<view3d::Object>(rdr12::cast<rdr12::ldraw::LdrObject>(hit.m_instance)),
			.m_distance = hit.m_distance,
			.m_ray_index = hit.m_ray_index,
			.m_ray_id = hit.m_ray_id,
			.m_snap_type = static_cast<view3d::ESnapType>(hit.m_snap_type),
		};
	}
}
