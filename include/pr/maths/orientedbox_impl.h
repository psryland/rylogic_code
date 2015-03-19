//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_ORIENTED_BOX_IMPL_H
#define PR_MATHS_ORIENTED_BOX_IMPL_H

#include "pr/maths/orientedbox.h"

namespace pr
{
	inline OrientedBox operator * (m4x4 const& m, OrientedBox const& ob)
	{
		OrientedBox obox;
		obox.m_box_to_world = m * ob.m_box_to_world;
		obox.m_radius       = ob.m_radius;
		return obox;
	}

	inline float Volume(OrientedBox const& ob)
	{
		return ob.SizeX() * ob.SizeY() * ob.SizeZ();
	}

	inline m4x4 const& Getm4x4(OrientedBox const& ob)
	{
		return ob.m_box_to_world;
	}
	inline m4x4& Getm4x4(OrientedBox& ob)
	{
		return ob.m_box_to_world;
	}

	inline BSphere GetBoundingSphere(OrientedBox const& ob)
	{
		return BSphere::make(ob.m_box_to_world.pos, Length3(ob.m_radius));
	}
}

#endif
