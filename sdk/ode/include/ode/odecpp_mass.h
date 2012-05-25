/*************************************************************************
 *                                                                       *
 * Open Dynamics Engine, Copyright (C) 2001,2002 Russell L. Smith.       *
 * All rights reserved.  Email: russ@q12.org   Web: www.q12.org          *
 *                                                                       *
 * This library is free software; you can redistribute it and/or         *
 * modify it under the terms of EITHER:                                  *
 *   (1) The GNU Lesser General Public License as published by the Free  *
 *       Software Foundation; either version 2.1 of the License, or (at  *
 *       your option) any later version. The text of the GNU Lesser      *
 *       General Public License is included with this library in the     *
 *       file LICENSE.TXT.                                               *
 *   (2) The BSD-style license that is included with this library in     *
 *       the file LICENSE-BSD.TXT.                                       *
 *                                                                       *
 * This library is distributed in the hope that it will be useful,       *
 * but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the files    *
 * LICENSE.TXT and LICENSE-BSD.TXT for more details.                     *
 *                                                                       *
 *************************************************************************/

#ifndef _ODECPP_MASS_H_
#define _ODECPP_MASS_H_
#ifdef __cplusplus

#include <ode/common.h>
#include <ode/mass.h>

/**
 * Return the combined mass properties of a range of geoms
 *
 * @param begin The start of the range
 * @param end The end of the range
 * @param density The density to assume for each object in the range
 *
 * @return the combined mass properties
 */
template <typename Iter> dMass dGeomGetCombinedMass(Iter first, Iter last, float density)
{
	dMass mass;
	for (; first != last; ++first)
	{
		dMass m;
		dMassSetFromGeom(&m, *first, density);
		dMassAdd(&mass, &m);
	}
	return mass;
}

/**
 * Shift a range of geoms to centre of mass frame
 *
 * @param begin The start of the range
 * @param end The end of the range
 * @param mass The combined mass properties of the range of geoms
 * @param ofs The translation applied to each geom in the range
 *
 * @return the range of geoms and mass will be in centre of mass frame. ofs will contain the translation applied
 */
template <typename Iter> void dGeomMoveToCoMFrame(Iter first, Iter last, dMass* mass, dVector3& ofs)
{
	ofs[0] = -mass->c[0];
	ofs[1] = -mass->c[1];
	ofs[2] = -mass->c[2];
	if (mass->c[0] != 0.0f || mass->c[1] != 0.0f || mass->c[2] != 0.0f)
	{
		for (; first != last; ++first)
		{
			dReal const* pos = dGeomGetOffsetPosition(*first);
			dGeomSetOffsetPosition(*first, pos[0]+ofs[0], pos[1]+ofs[1], pos[2]+ofs[2]);
		}
		dMassTranslate(mass, ofs[0], ofs[1], ofs[2]);
	}
}

#endif
#endif
