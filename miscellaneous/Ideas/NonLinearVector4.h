#ifndef PR_MATHS_NON_LINEAR_VECTOR4_H
#define PR_MATHS_NON_LINEAR_VECTOR4_H

namespace pr
{
	struct NonLinearComponent
	{
		virtual float operator (const v4&) = 0;
	}

	template<typename X, typename Y, typename Z, typename W>
	struct NonLinearV4
	{
		X x;
		Y y;
		Z z;
		W w;
	};

	template<typename VecX, typename VecY, typename VecZ, typename VecW>
	struct NonLinearM4x4
	{
		VecX x;
		VecY y;
		VecZ z;
		VecW w;
	};

	inline v4 operator * (const NonLinearM4x4& lhs, const v4& rhs)
	{
		v4 v = 
		{
			x.x(rhs) + y.x(rhs) + z.x(rhs) + w.x(rhs),
			x.y(rhs) + y.y(rhs) + z.y(rhs) + w.y(rhs),
			x.z(rhs) + y.z(rhs) + z.z(rhs) + w.z(rhs),
			x.w(rhs) + y.w(rhs) + z.w(rhs) + w.w(rhs)
		};
		return v;
	}
}//namespace pr
	
#endif//PR_MATHS_NON_LINEAR_VECTOR4_H
