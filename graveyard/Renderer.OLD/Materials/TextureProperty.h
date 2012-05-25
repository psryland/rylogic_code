#ifndef TEXTURE_PROPERTY_H
#define TEXTURE_PROPERTY_H

namespace pr
{
	namespace rdr
	{
		enum TextureProperty
		{
			TextureProperty_Alpha		= 1 << 0,
			TextureProperty_Effect		= 1 << 1,
		};
	}//namespace rdr
}//namespace pr

#endif//TEXTURE_PROPERTY_H
