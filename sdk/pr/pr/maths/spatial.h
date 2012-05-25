//*****************************************************************************
// Maths library
//  Copyright © Rylogic Ltd 2002
//*****************************************************************************

//#ifndef PR_MATHS_SPATIAL_H
//#define PR_MATHS_SPATIAL_H
//
//namespace pr
//{
//	struct v6
//	{
//		v6&   Zero()										{ w.Zero(); v.Zero(); return *this; }
//		v6&   Set(const v4& w_, const v4& v_)				{ w = w_; v = v_; return *this; }
//		v4 w;
//		v4 v;
//	};
//
//
//// Accessor functions
//void		maSetValueVs(MAv6& v, MAint i, MAreal value);
//bool		maIsZeroVs(const MAv6& v);
//MAreal		maValueVs(const MAv6& v, MAint i);
//const MAv4&	maValueV4Vs(const MAv6& v, MAint i);
//      MAv4&	maValueV4Vs(      MAv6& v, MAint i);
//
//// Manipulation functions
//void		maGetSumVs(MAv6& sum, const MAv6& v1, const MAv6& v2);
//void		maGetScalarMulVs(MAv6& mul, MAreal s, const MAv6& v);
//MAreal		maInnerVs(const MAv6& v1, const MAv6& v2);
//
//
//// Spatial matrix data types and access functions
//// _       _
//// | m1 m2 |
//// |       |
//// | m3 m4 |
//// ¬       ¬
//struct MAm6
//{
//	MAm3 m1;
//	MAm3 m2;
//	MAm3 m3;
//	MAm3 m4;
//};
//
//// Accessor functions
//void	maSetMs(MAm6& m, MAm3ref m1, MAm3ref m2, MAm3ref m3, MAm3ref m4);
//void	maSetIdMs(MAm6& m);
//void	maSetValueMs(MAm6& m, MAint i, MAint j, MAreal value);
//MAreal	maValueMs(const MAm6& m, MAint i, MAint j);
//const MAm3&	maValueM3Vs(const MAm6& m, MAint i);
//      MAm3&	maValueM3Vs(      MAm6& m, MAint i);
//
//// Spatial matrix manipulation functions
//void maGetMulMsVs(MAv6& mul, const MAm6& m, const MAv6& v);
//void maGetMulVsMs(MAv6& mul, const MAv6& v, const MAm6& m);
//void maGetSumMs(MAm6& sum, const MAm6& m1, const MAm6& m2);
//void maGetDifferenceMs(MAm6& diff, const MAm6& m1, const MAm6& m2);
//void maGetScalarMulMs(MAm6& mul, MAreal s, const MAm6& m);
//void maGetMulMs(MAm6& mul, const MAm6& m1, const MAm6& m2);
//
//// Spatial matrix/vector/quaternion functions
//void maGetTransformMs(MAm6& x, MAqref rot, MAv4ref offset);
//void maGetOuterVs(MAm6& m, const MAv6& v1, const MAv6& v2);
//MAint maGaussianMsVs(MAv6& result, MAm6& m, MAv6& v);
//
//
//
//// Spatial transforms as special cases of spatial matrices
//// The spatial matrix equivalent to this transformation is:
//// _         _
//// |  R   0  |
//// |         |
//// |-d^R  R  |
//// ¬         ¬
////(d^ represents the cross product matrix associated with the vector d)
//struct MAx6
//{
//	MAm4 x;	//generalised rotation matrix from frame F to frame G, with offset
//};
//
//// Access functions for Xs
//void maSetM4Xs(MAm4& m, const MAx6& x);
//void maSetXColXs(MAx6& x, MAv4ref v);
//void maSetYColXs(MAx6& x, MAv4ref v);
//void maSetZColXs(MAx6& x, MAv4ref v);
//void maSetTransXs(MAx6& x, MAv4ref v);
//
//void maGetXColXs(MAv4& v, const MAx6& x);
//void maGetYColXs(MAv4& v, const MAx6& x);
//void maGetZColXs(MAv4& v, const MAx6& x);
//void maGetTransXs(MAv4& v, const MAx6& x);
//
//MAreal	maValueXs(const MAx6& x, MAint i, MAint j);
//void	maSetValueXs(MAx6& x, MAint i, MAint j, MAreal value);	//inline MAreal *maPtToElXs(MAx6 *x,MAint i,MAint j);
//void	maGetXs(MAx6& x, MAv4ref pos, MAv4ref orientation, MAv4ref z);
//void	maGetGeneralXs(MAx6& x, MAqref orientation, MAv4ref pos);
//void	maGetGeneralXs(MAx6& x, MAm4ref object_to_world);
//void	maGetShiftXs(MAx6& x, MAv4ref d);
//void	maGetZRotateXs(MAx6& x, MAreal th);
//
//void	maInvertXs(MAx6& xinv, const MAx6& x);
//void	maApplyXsVs(MAv6& newv, const MAx6& x, const MAv6& v);
//void	maApplyXsXs(MAx6& newx, const MAx6& x1, const MAx6& x2);
//void	maApplyXsM4(MAm4& newm, MAm4ref m, const MAx6& x);
//void	maGetM3FromXs(MAm3& m3, const MAx6& x);
//void	maGetInvM3FromXs(MAm3& m3, const MAx6& x);
//void	maApplyXsIa(MAm6& newI, const MAx6& x, const MAm6& I);
//
//
//
//
//// Begin old spatial functions //////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////
////	Spatial vectors/matrices
////	Used in multibody calculations
//
////	Spatial vector data types, access functions
//
//#ifdef MAMATH_NO_INLINE
//
//#undef inline
//
//#else
//
//#include "mapolynomials.h"
//#include "maspatial.h"
//
//#endif
//
//#endif // !defined( _mapolyspatial_h_ )
//
//}//namespace pr
//
//#endif//PR_MATHS_SPATIAL_H
