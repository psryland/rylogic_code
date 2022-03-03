#ifndef PR_TO_RI_CONVERSIONS_H
#define PR_TO_RI_CONVERSIONS_H

#include "PR/Maths/Maths.h"
#include "Maths/Mamath.h"

namespace pr
{
	namespace no_step_into
	{
		inline pr::v4	mav4_to_v4	(MAv4ref vec)			{ return pr::v4::construct(vec[0], vec[1], vec[2], vec[3]); }
		inline pr::m3x3 mam3_to_m3x3(MAm3ref mat)			{ return pr::m3x3::construct(mat[0][0], mat[0][1], mat[0][2],
																						 mat[1][0], mat[1][1], mat[1][2],
																						 mat[2][0], mat[2][1], mat[2][2]); }
		inline pr::m4x4	mam4_to_m4x4(MAm4ref mat)			{ return pr::m4x4::construct(mat[0][0], mat[0][1], mat[0][2], mat[0][3],
																						 mat[1][0], mat[1][1], mat[1][2], mat[1][3],
																						 mat[2][0], mat[2][1], mat[2][2], mat[2][3],
																						 mat[3][0], mat[3][1], mat[3][2], mat[3][3]); }

		inline MAv4		v4_to_mav4	(const pr::v4& vec)		{ return MAv4::construct(vec.x, vec.y, vec.z, vec.w); }
		inline MAm3		m3x3_to_mam3(const pr::m3x3& mat)	{ return MAm3::construct(mat.x.x, mat.x.y, mat.x.z,
																					 mat.y.x, mat.y.y, mat.y.z,
																					 mat.z.x, mat.z.y, mat.z.z); }
		inline MAm4		m4x4_to_mam4(const pr::m4x4& mat)	{ return MAm4::construct(mat.x.x, mat.y.x, mat.z.x, mat.w.x,
																					 mat.x.y, mat.y.y, mat.z.y, mat.w.y,
																					 mat.x.z, mat.y.z, mat.z.z, mat.w.z,
																					 mat.x.w, mat.y.w, mat.z.w, mat.w.w); }
	}//namespace no_step_into
}//namespace pr
using namespace pr::no_step_into;

#endif//PR_TO_RI_CONVERSIONS_H
