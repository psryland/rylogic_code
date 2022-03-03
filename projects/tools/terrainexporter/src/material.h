//*******************************************************************************
// Terrain Exporter
//  Copyright © Rylogic Ltd 2009
//*******************************************************************************
#pragma once
#include "terrainexporter/forward.h"

namespace pr
{
	namespace terrain
	{
		struct Material
		{
			pr::uint16		m_material;
			std::string		m_texture_filename;
			Material() :m_material(0) ,m_texture_filename("") {}
		};
	}//namespace terrain
}//namespace pr
