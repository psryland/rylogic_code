//********************************
// PRGeometry Forward
//  Copyright © Rylogic Ltd 2006
//********************************
#ifndef PR_GEOMETRY_FORWARD_H
#define PR_GEOMETRY_FORWARD_H

#include "pr/str/prstring.h"

#ifndef PR_DBG_GEOM
#define PR_DBG_GEOM PR_DBG
#endif

namespace pr
{
	typedef unsigned short GeomType;
	
	namespace geom
	{
		typedef pr::string<char, 32, true> string32;
		
		enum Type
		{
			EInvalid = 0,
			EVertex  = 1 << 0,
			ENormal  = 1 << 1,
			EColour  = 1 << 2,
			ETexture = 1 << 3,
			EAll     =(1 << 4) - 1,
			EVN      = EVertex | ENormal,
			EVC      = EVertex           | EColour,
			EVT      = EVertex                     | ETexture,
			EVNC     = EVertex | ENormal | EColour,
			EVNT     = EVertex | ENormal           | ETexture,
			EVCT     = EVertex           | EColour | ETexture,
			EVNCT    = EVertex | ENormal | EColour | ETexture,
		};
		GeomType Parse(char const* str);
		string32 ToString(GeomType type);
		bool     IsValid(GeomType type);
	}

	struct Texture;
	struct Material;
	struct Vert;
	struct Face;
	struct Mesh;
	struct Frame;
	struct Geometry;
}

#endif
