//*********************************************
// Maths Library
//  Copyright © Rylogic Ltd 2006
//*********************************************

#pragma once
#ifndef PR_MATHS_MACROS_H
#define PR_MATHS_MACROS_H

#ifndef PR_MATHS_USE_D3DX
#	ifdef _D3D9_H_
#		define PR_MATHS_USE_D3DX 1
#	else
#		define PR_MATHS_USE_D3DX 0
#	endif
#endif

#ifndef PR_MATHS_USE_OPEN_MP
#define PR_MATHS_USE_OPEN_MP 0
#endif

#ifndef PR_MATHS_USE_INTRINSICS
#define PR_MATHS_USE_INTRINSICS 1
#endif

#ifndef PR_MATHS_USE_CREF
#define PR_MATHS_USE_CREF 1
#endif

#endif
