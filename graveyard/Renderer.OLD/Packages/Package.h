//*****************************************************************************
//
//	Package
//
//*****************************************************************************
//  +-------------------+
//  | +---------------+ |
//  | | Header        | |
//  | |               | |
//  | +---------------+ |
//  | | Geometry Data | |
//  | | +-------+     | |
//  | | | Model |     | |
//  | | +-------+     | |
//  | | | Model |     | |
//  | | +-------+     | |
//  | +---------------+ |
//  | | Texture Data  | |
//  | | +---------+   | |
//  | | | Texture |   | |
//  | | +---------+   | |
//  | | | Texture |   | |
//  | | +---------+   | |
//  | +---------------+ |
//  | | Effect Data   | |
//  | | +--------+    | |
//  | | | Effect |    | |
//  | | +--------+    | |
//  | | | Effect |    | |
//  | | +--------+    | |
//  | +---------------+ |
//  +-------------------+
//
// Provides a collection of functions for loading packages of renderer data.
// A 'model_package' is a nugget file containing models as the child nuggets
// A 'texture_package' is a nugget file containing textures as the child nuggets
// An 'effect_package' is a nugget file containing effects as the child nuggets
// A 'package' is a nugget file containing one or more of the packages above as
// child nuggets.

#ifndef PR_RDR_PACKAGE_H
#define PR_RDR_PACKAGE_H

#include "PR/Renderer/Forward.h"

namespace pr
{
	namespace rdr
	{
		enum EPackageId
		{
			EPackageId_Header	= PR_MAKE_NUGGET_ID('H','d','r',' '),
			EPackageId_Geometry	= PR_MAKE_NUGGET_ID('G','e','o','m'),
			EPackageId_Textures	= PR_MAKE_NUGGET_ID('T','e','x','t'),
			EPackageId_Effects	= PR_MAKE_NUGGET_ID('E','f','f','s'),
			EPackageId_Model	= PR_MAKE_NUGGET_ID('M','d','l',' '),
			EPackageId_Texture 	= PR_MAKE_NUGGET_ID('T','e','x',' '),
			EPackageId_Effect	= PR_MAKE_NUGGET_ID('E','f','f',' '),
		};

		namespace package
		{
			struct Effect
			{
			};
			struct Texture
			{
			};
			struct Model
			{
			};
			struct Header
			{
			};
		}//namespace package
	}//namespace rdr
}//namespace pr

#endif//PR_RDR_PACKAGE_H
