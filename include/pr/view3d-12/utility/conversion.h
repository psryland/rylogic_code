//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/view3d-dll.h"

namespace pr
{
	template <> struct Convert<D3D12_RANGE, rdr12::Range>
	{
		static D3D12_RANGE To_(rdr12::Range const& r)
		{
			return reinterpret_cast<D3D12_RANGE const&>(r);
		}
	};
	template <> struct Convert<rdr12::Range, D3D12_RANGE>
	{
		static rdr12::Range To_(D3D12_RANGE const& r)
		{
			return reinterpret_cast<rdr12::Range const&>(r);
		}
	};

	// From view3d to pr::maths
	template <> struct Convert<v2, view3d::Vec2>
	{
		static v2 To_(view3d::Vec2 const& v)
		{
			return v2(v.x, v.y);
		}
	};
	template <> struct Convert<v4, view3d::Vec4>
	{
		static v4 To_(view3d::Vec4 const& v)
		{
			return v4(v.x, v.y, v.z, v.w);
		}
	};
	template <> struct Convert<m4x4, view3d::Mat4x4>
	{
		static m4x4 To_(view3d::Mat4x4 const& m)
		{
			return m4x4(To<v4>(m.x),To<v4>(m.y),To<v4>(m.z),To<v4>(m.w));
		}
	};

	// From pr::maths to view3d
	template <> struct Convert<view3d::Vec2, v2>
	{
		static view3d::Vec2 To_(v2 const& v)
		{
			return view3d::Vec2{v.x, v.y};
		}
	};
	template <> struct Convert<view3d::Vec4, v4>
	{
		static view3d::Vec4 To_(v4 const& v)
		{
			return view3d::Vec4{v.x, v.y, v.z, v.w};
		}
	};
	template <> struct Convert<view3d::Mat4x4, m4x4>
	{
		static view3d::Mat4x4 To_(m4x4 const& m)
		{
			return view3d::Mat4x4{To<view3d::Vec4>(m.x), To<view3d::Vec4>(m.y), To<view3d::Vec4>(m.z), To<view3d::Vec4>(m.w)};
		}
	};
}
