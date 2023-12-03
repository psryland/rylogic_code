//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/view3d-dll.h"

namespace pr
{
	// D3D12_RANGE / rdr12::Range
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

	// Colour32 / view3d::Colour
	template <> struct Convert<Colour32, view3d::Colour>
	{
		static Colour32 To_(view3d::Colour const& v)
		{
			return Colour32(v);
		}
	};
	template <> struct Convert<view3d::Colour, Colour32>
	{
		static view3d::Colour To_(Colour32 const& v)
		{
			return view3d::Colour(v.argb);
		}
	};

	// v2 / view3d::Vec2
	template <> struct Convert<v2, view3d::Vec2>
	{
		static v2 To_(view3d::Vec2 const& v)
		{
			return v2(v.x, v.y);
		}
	};
	template <> struct Convert<view3d::Vec2, v2>
	{
		static view3d::Vec2 To_(v2 const& v)
		{
			return view3d::Vec2{v.x, v.y};
		}
	};

	// v4 / view3d::Vec4
	template <> struct Convert<v4, view3d::Vec4>
	{
		static v4 To_(view3d::Vec4 const& v)
		{
			return v4(v.x, v.y, v.z, v.w);
		}
	};
	template <> struct Convert<view3d::Vec4, v4>
	{
		static view3d::Vec4 To_(v4 const& v)
		{
			return view3d::Vec4{v.x, v.y, v.z, v.w};
		}
	};

	// m4x4 / view3d::Mat4x4
	template <> struct Convert<m4x4, view3d::Mat4x4>
	{
		static m4x4 To_(view3d::Mat4x4 const& m)
		{
			return m4x4(To<v4>(m.x),To<v4>(m.y),To<v4>(m.z),To<v4>(m.w));
		}
	};
	template <> struct Convert<view3d::Mat4x4, m4x4>
	{
		static view3d::Mat4x4 To_(m4x4 const& m)
		{
			return view3d::Mat4x4{To<view3d::Vec4>(m.x), To<view3d::Vec4>(m.y), To<view3d::Vec4>(m.z), To<view3d::Vec4>(m.w)};
		}
	};

	// BBox / view3d::BBox
	template <> struct Convert<BBox, view3d::BBox>
	{
		static BBox To_(view3d::BBox const& bbox)
		{
			return BBox(To<v4>(bbox.centre), To<v4>(bbox.radius));
		}
	};
	template <> struct Convert<view3d::BBox, BBox>
	{
		static view3d::BBox To_(BBox const& bbox)
		{
			return view3d::BBox{To<view3d::Vec4>(bbox.m_centre), To<view3d::Vec4>(bbox.m_radius)};
		}
	};
}
