//*******************************************************************************************
// Colour32
//  Copyright © Rylogic Ltd 2009
//*******************************************************************************************
#pragma once
#ifndef PR_COMMON_COLOUR_H
#define PR_COMMON_COLOUR_H

#include <memory.h>
#include "pr/common/prtypes.h"
#include "pr/maths/rand.h"
#include "pr/maths/scalar.h"

#ifdef _WINGDI_
#  define PR_SUPPORT_WINGDI(exp) exp
#else
#  define PR_SUPPORT_WINGDI(exp)
#endif

#if defined(DIRECTX_MATH_VERSION)
#  define PR_SUPPORT_DX(exp) exp
#else
#  define PR_SUPPORT_DX(exp)
#endif

namespace pr
{
	// Forward declaration
	struct Colour;
	struct Colour32;
	
	// Predefined windows colours
	namespace EColours
	{
		enum Type
		{
			#pragma region Predefined_windows_colours
			AliceBlue            = 0xFFF0F8FF,
			AntiqueWhite         = 0xFFFAEBD7,
			Aqua                 = 0xFF00FFFF,
			Aquamarine           = 0xFF7FFFD4,
			Azure                = 0xFFF0FFFF,
			Beige                = 0xFFF5F5DC,
			Bisque               = 0xFFFFE4C4,
			Black                = 0xFF000000,
			BlanchedAlmond       = 0xFFFFEBCD,
			Blue                 = 0xFF0000FF,
			BlueViolet           = 0xFF8A2BE2,
			Brown                = 0xFFA52A2A,
			BurlyWood            = 0xFFDEB887,
			CadetBlue            = 0xFF5F9EA0,
			Chartreuse           = 0xFF7FFF00,
			Chocolate            = 0xFFD2691E,
			Coral                = 0xFFFF7F50,
			CornflowerBlue       = 0xFF6495ED,
			Cornsilk             = 0xFFFFF8DC,
			Crimson              = 0xFFDC143C,
			Cyan                 = 0xFF00FFFF,
			DarkBlue             = 0xFF00008B,
			DarkCyan             = 0xFF008B8B,
			DarkGoldenrod        = 0xFFB8860B,
			DarkGrey             = 0xFFA9A9A9,
			DarkGreen            = 0xFF006400,
			DarkKhaki            = 0xFFBDB76B,
			DarkMagenta          = 0xFF8B008B,
			DarkOliveGreen       = 0xFF556B2F,
			DarkOrange           = 0xFFFF8C00,
			DarkOrchid           = 0xFF9932CC,
			DarkRed              = 0xFF8B0000,
			DarkSalmon           = 0xFFE9967A,
			DarkSeaGreen         = 0xFF8FBC8F,
			DarkSlateBlue        = 0xFF483D8B,
			DarkSlateGrey        = 0xFF2F4F4F,
			DarkTurquoise        = 0xFF00CED1,
			DarkViolet           = 0xFF9400D3,
			DeepPink             = 0xFFFF1493,
			DeepSkyBlue          = 0xFF00BFFF,
			DimGrey              = 0xFF696969,
			DodgerBlue           = 0xFF1E90FF,
			FireBrick            = 0xFFB22222,
			FloralWhite          = 0xFFFFFAF0,
			ForestGreen          = 0xFF228B22,
			Fuchsia              = 0xFFFF00FF,
			Gainsboro            = 0xFFDCDCDC,
			GhostWhite           = 0xFFF8F8FF,
			Gold                 = 0xFFFFD700,
			Goldenrod            = 0xFFDAA520,
			Grey                 = 0xFF808080,
			Green                = 0xFF008000,
			GreenYellow          = 0xFFADFF2F,
			Honeydew             = 0xFFF0FFF0,
			HotPink              = 0xFFFF69B4,
			IndianRed            = 0xFFCD5C5C,
			Indigo               = 0xFF4B0082,
			Ivory                = 0xFFFFFFF0,
			Khaki                = 0xFFF0E68C,
			Lavender             = 0xFFE6E6FA,
			LavenderBlush        = 0xFFFFF0F5,
			LawnGreen            = 0xFF7CFC00,
			LemonChiffon         = 0xFFFFFACD,
			LightBlue            = 0xFFADD8E6,
			LightCoral           = 0xFFF08080,
			LightCyan            = 0xFFE0FFFF,
			LightGoldenrodYellow = 0xFFFAFAD2,
			LightGreen           = 0xFF90EE90,
			LightGrey            = 0xFFD3D3D3,
			LightPink            = 0xFFFFB6C1,
			LightSalmon          = 0xFFFFA07A,
			LightSeaGreen        = 0xFF20B2AA,
			LightSkyBlue         = 0xFF87CEFA,
			LightSlateGrey       = 0xFF778899,
			LightSteelBlue       = 0xFFB0C4DE,
			LightYellow          = 0xFFFFFFE0,
			Lime                 = 0xFF00FF00,
			LimeGreen            = 0xFF32CD32,
			Linen                = 0xFFFAF0E6,
			Magenta              = 0xFFFF00FF,
			Maroon               = 0xFF800000,
			MediumAquamarine     = 0xFF66CDAA,
			MediumBlue           = 0xFF0000CD,
			MediumOrchid         = 0xFFBA55D3,
			MediumPurple         = 0xFF9370DB,
			MediumSeaGreen       = 0xFF3CB371,
			MediumSlateBlue      = 0xFF7B68EE,
			MediumSpringGreen    = 0xFF00FA9A,
			MediumTurquoise      = 0xFF48D1CC,
			MediumVioletRed      = 0xFFC71585,
			MidnightBlue         = 0xFF191970,
			MintCream            = 0xFFF5FFFA,
			MistyRose            = 0xFFFFE4E1,
			Moccasin             = 0xFFFFE4B5,
			NavajoWhite          = 0xFFFFDEAD,
			Navy                 = 0xFF000080,
			OldLace              = 0xFFFDF5E6,
			Olive                = 0xFF808000,
			OliveDrab            = 0xFF6B8E23,
			Orange               = 0xFFFFA500,
			OrangeRed            = 0xFFFF4500,
			Orchid               = 0xFFDA70D6,
			PaleGoldenrod        = 0xFFEEE8AA,
			PaleGreen            = 0xFF98FB98,
			PaleTurquoise        = 0xFFAFEEEE,
			PaleVioletRed        = 0xFFDB7093,
			PapayaWhip           = 0xFFFFEFD5,
			PeachPuff            = 0xFFFFDAB9,
			Peru                 = 0xFFCD853F,
			Pink                 = 0xFFFFC0CB,
			Plum                 = 0xFFDDA0DD,
			PowderBlue           = 0xFFB0E0E6,
			Purple               = 0xFF800080,
			Red                  = 0xFFFF0000,
			RosyBrown            = 0xFFBC8F8F,
			RoyalBlue            = 0xFF4169E1,
			SaddleBrown          = 0xFF8B4513,
			Salmon               = 0xFFFA8072,
			SandyBrown           = 0xFFF4A460,
			SeaGreen             = 0xFF2E8B57,
			Seashell             = 0xFFFFF5EE,
			Sienna               = 0xFFA0522D,
			Silver               = 0xFFC0C0C0,
			SkyBlue              = 0xFF87CEEB,
			SlateBlue            = 0xFF6A5ACD,
			SlateGrey            = 0xFF708090,
			Snow                 = 0xFFFFFAFA,
			SpringGreen          = 0xFF00FF7F,
			SteelBlue            = 0xFF4682B4,
			Tan                  = 0xFFD2B48C,
			Teal                 = 0xFF008080,
			Thistle              = 0xFFD8BFD8,
			Tomato               = 0xFFFF6347,
			Turquoise            = 0xFF40E0D0,
			Violet               = 0xFFEE82EE,
			Wheat                = 0xFFF5DEB3,
			White                = 0xFFFFFFFF,
			WhiteSmoke           = 0xFFF5F5F5,
			Yellow               = 0xFFFFFF00,
			YellowGreen          = 0xFF9ACD32,
			#pragma endregion
		};
	}
	
	// Equivelent to D3DCOLOR
	struct Colour32
	{
		uint32 m_aarrggbb;
		
		static Colour32 make(EColours::Type col)                { Colour32 c; return c.set(col); }
		static Colour32 make(uint32 aarrggbb)                   { Colour32 c; return c.set(aarrggbb); }
		static Colour32 make(float r, float g, float b, float a){ Colour32 c; return c.set(r,g,b,a); }
		static Colour32 make(int r, int g, int b, int a)        { Colour32 c; return c.set(r,g,b,a); }
		
		Colour32& operator = (Colour const& c);
		Colour32& operator = (uint32 i)                         { m_aarrggbb = i; return *this; }
		Colour32& operator = (int i)                            { m_aarrggbb = uint32(i); return *this; }
		Colour32  operator !() const                            { return make((m_aarrggbb&0xFF000000) | (m_aarrggbb^0xFFFFFFFF)); }
		operator uint32() const                                 { return m_aarrggbb; }
		operator Colour() const;
		PR_SUPPORT_WINGDI(Colour32& operator = (const COLORREF& c) { set(GetRValue(c), GetGValue(c), GetBValue(c), uint8(255)); return *this; })
		
		Colour32& zero()                                        { m_aarrggbb = 0; return *this; }
		Colour32& set(EColours::Type col)                       { m_aarrggbb = static_cast<uint32>(col); return *this; }
		Colour32& set(uint32 aarrggbb)                          { m_aarrggbb = aarrggbb; return *this; }
		Colour32& set(uint8 r_, uint8 g_, uint8 b_, uint8 a_)   { m_aarrggbb = (a_<<24) | (r_<<16) | (g_<<8) | (b_); return *this; }
		Colour32& set(int   r_, int   g_, int   b_, int   a_)   { return set(uint8(r_&0xff),   uint8(g_&0xff),   uint8(b_&0xff),   uint8(a_&0xff)); }
		Colour32& set(float r_, float g_, float b_, float a_)   { return set(uint8(r_*255.0f), uint8(g_*255.0f), uint8(b_*255.0f), uint8(a_*255.0f)); }
		
		Colour32 rgba() const                                   { return Colour32::make(((m_aarrggbb&0x00ffffff)<<8)|(m_aarrggbb>>24)); }
		Colour32 aooo() const                                   { return Colour32::make(m_aarrggbb & 0xFF000000); }
		Colour32 oroo() const                                   { return Colour32::make(m_aarrggbb & 0x00FF0000); }
		Colour32 oogo() const                                   { return Colour32::make(m_aarrggbb & 0x0000FF00); }
		Colour32 ooob() const                                   { return Colour32::make(m_aarrggbb & 0x000000FF); }
		Colour32 orgb() const                                   { return Colour32::make(m_aarrggbb & 0x00FFFFFF); }
		uint8&   a()                                            { return reinterpret_cast<uint8*>(&m_aarrggbb)[3]; }
		uint8&   r()                                            { return reinterpret_cast<uint8*>(&m_aarrggbb)[2]; }
		uint8&   g()                                            { return reinterpret_cast<uint8*>(&m_aarrggbb)[1]; }
		uint8&   b()                                            { return reinterpret_cast<uint8*>(&m_aarrggbb)[0]; }
		uint8    a() const                                      { return uint8((m_aarrggbb >> 24) & 0xFF); }
		uint8    r() const                                      { return uint8((m_aarrggbb >> 16) & 0xFF); }
		uint8    g() const                                      { return uint8((m_aarrggbb >>  8) & 0xFF); }
		uint8    b() const                                      { return uint8((m_aarrggbb >>  0) & 0xFF); }
		
		PR_SUPPORT_WINGDI(COLORREF GetColorRef() const             { return RGB(r(), g(), b()); })
		inline uint8 const* ToArray() const                     { return reinterpret_cast<uint8 const*>(this); }
		inline uint8*       ToArray()                           { return reinterpret_cast<uint8*>      (this); }
		inline uint8 const& operator [] (std::size_t i) const   { PR_ASSERT(PR_DBG_MATHS, i < 4, ""); return ToArray()[i]; }
		inline uint8&       operator [] (std::size_t i)         { PR_ASSERT(PR_DBG_MATHS, i < 4, ""); return ToArray()[i]; }
	};

	Colour32 const Colour32Zero   = { 0x00000000 };
	Colour32 const Colour32One    = { 0xFFFFFFFF };
	Colour32 const Colour32White  = { 0xFFFFFFFF };
	Colour32 const Colour32Black  = { 0xFF000000 };
	Colour32 const Colour32Red    = { 0xFFFF0000 };
	Colour32 const Colour32Green  = { 0xFF00FF00 };
	Colour32 const Colour32Blue   = { 0xFF0000FF };
	Colour32 const Colour32Yellow = { 0xFFFFFF00 };
	Colour32 const Colour32Purple = { 0xFFFF00FF };
	Colour32 const Colour32Gray   = { 0xFF808080 };
	
	// Equality operators
	inline bool operator == (Colour32 lhs, Colour32 rhs) { return lhs.m_aarrggbb == rhs.m_aarrggbb; }
	inline bool operator != (Colour32 lhs, Colour32 rhs) { return !(lhs == rhs); }
	inline bool operator <  (Colour32 lhs, Colour32 rhs) { return lhs.m_aarrggbb <  rhs.m_aarrggbb; }
	inline bool operator >  (Colour32 lhs, Colour32 rhs) { return lhs.m_aarrggbb >  rhs.m_aarrggbb; }
	inline bool operator <= (Colour32 lhs, Colour32 rhs) { return lhs.m_aarrggbb <= rhs.m_aarrggbb; }
	inline bool operator >= (Colour32 lhs, Colour32 rhs) { return lhs.m_aarrggbb >= rhs.m_aarrggbb; }
	inline bool EqualNoA    (Colour32 lhs, Colour32 rhs) { return lhs.orgb() == rhs.orgb(); }
	
	// Binary operators
	inline Colour32 operator + (Colour32 lhs, Colour32 rhs) { Colour32 c; return c.set(Clamp(lhs.r() + rhs.r()  ,0  ,255  ) ,Clamp(lhs.g() + rhs.g()  ,0  ,255  ) ,Clamp(lhs.b() + rhs.b()  ,0  ,255  ) ,Clamp(lhs.a() + rhs.a()  ,0  ,255  )); }
	inline Colour32 operator - (Colour32 lhs, Colour32 rhs) { Colour32 c; return c.set(Clamp(lhs.r() - rhs.r()  ,0  ,255  ) ,Clamp(lhs.g() - rhs.g()  ,0  ,255  ) ,Clamp(lhs.b() - rhs.b()  ,0  ,255  ) ,Clamp(lhs.a() - rhs.a()  ,0  ,255  )); }
	inline Colour32 operator * (Colour32 lhs, float s)      { Colour32 c; return c.set(Clamp(lhs.r() * s        ,0.f,255.f) ,Clamp(lhs.g() * s        ,0.f,255.f) ,Clamp(lhs.b() * s        ,0.f,255.f) ,Clamp(lhs.a() * s        ,0.f,255.f)); }
	inline Colour32 operator * (float s, Colour32 rhs)      { Colour32 c; return c.set(Clamp(rhs.r() * s        ,0.f,255.f) ,Clamp(rhs.g() * s        ,0.f,255.f) ,Clamp(rhs.b() * s        ,0.f,255.f) ,Clamp(rhs.a() * s        ,0.f,255.f)); }
	inline Colour32 operator * (Colour32 lhs, Colour32 rhs) { Colour32 c; return c.set(Clamp(lhs.r()*rhs.r()/255,0  ,255  ) ,Clamp(lhs.g()*rhs.g()/255,0  ,255  ) ,Clamp(lhs.b()*rhs.b()/255,0  ,255  ) ,Clamp(lhs.a()*rhs.a()/255,0  ,255  )); }
	inline Colour32 operator / (Colour32 lhs, float s)      { Colour32 c; return c.set(Clamp(lhs.r() / s        ,0.f,255.f) ,Clamp(lhs.g() / s        ,0.f,255.f) ,Clamp(lhs.b() / s        ,0.f,255.f) ,Clamp(lhs.a() / s        ,0.f,255.f)); }
	
	// Assignment operators
	inline Colour32& operator += (Colour32& lhs, Colour32 rhs) { lhs = lhs + rhs; return lhs; }
	inline Colour32& operator -= (Colour32& lhs, Colour32 rhs) { lhs = lhs - rhs; return lhs; }
	inline Colour32& operator *= (Colour32& lhs, float s)      { lhs = lhs * s;   return lhs; }
	inline Colour32& operator *= (Colour32& lhs, Colour32 rhs) { lhs = lhs * rhs; return lhs; }
	inline Colour32& operator /= (Colour32& lhs, float s)      { lhs = lhs / s;   return lhs; }
	
	// Random colours
	inline Colour32 RandomRGB(Rnd& rnd, float a) { return Colour32::make(rnd.u8(), rnd.u8(), rnd.u8(), uint8(a*255.0f)); }
	inline Colour32 RandomRGB(float a)           { return RandomRGB(rand::Rand(), a); }
	inline Colour32 RandomRGB(Rnd& rnd)          { return RandomRGB(rnd, 1.0f); }
	inline Colour32 RandomRGB()                  { return RandomRGB(rand::Rand()); }
	
	// Equivalent to pr::v4, XMVECTOR, etc
	struct Colour
	{
		#pragma warning(push)
		#pragma warning(disable:4201)
		union {
			struct { float r,g,b,a; };
			PR_SUPPORT_DX(DirectX::XMVECTOR vec;)
		};
		#pragma warning(pop)

		static Colour make(float r, float g, float b, float a)   { Colour c; return c.set(r,g,b,a); }
		static Colour make(uint8 r, uint8 g, uint8 b, uint8 a)   { Colour c; return c.set(r,g,b,a); }
		static Colour make(Colour32 c32)                         { Colour c; return c.set(c32); }
		static Colour make(Colour32 c32, float alpha)            { Colour c; return c.set(c32, alpha); }

		Colour&      set(float r_, float g_, float b_, float a_) { r = r_; g = g_; b = b_; a = a_; return *this; }
		Colour&      set(uint8 r_, uint8 g_, uint8 b_, uint8 a_) { return set(r_/255.0f, g_/255.0f, b_/255.0f, a_/255.0f); }
		Colour&      set(Colour32 c32)                           { return set(c32.r(), c32.g(), c32.b(), c32.a()); }
		Colour&      set(Colour32 c32, float alpha)              { return set(c32.r()/255.0f, c32.g()/255.0f, c32.b()/255.0f, alpha); }
		Colour&      zero()                                      { r = g = b = a = 0.0f; return *this; }
		Colour&      one()                                       { r = g = b = a = 1.0f; return *this; }
		Colour32     argb() const	                             { return Colour32::make(r,g,b,a); }
		float const* ToArray() const                             { return reinterpret_cast<float const*>(this); }
		float*       ToArray()                                   { return reinterpret_cast<float*>      (this); }
		
		Colour& operator = (Colour32 c32)                        { return set(c32); }
		operator Colour32() const;
		
		typedef float const (&array_ref)[4];
		operator array_ref() const                               { return reinterpret_cast<array_ref>(*this); }
		
		PR_SUPPORT_WINGDI(Colour& operator = (const COLORREF& c)    { set(GetRValue(c), GetGValue(c), GetBValue(c), 255); return *this; })
		PR_SUPPORT_WINGDI(COLORREF GetColorRef() const              { return RGB(r*255.0f, g*255.0f, b*255.0f); })
	};
	
	Colour const ColourZero   = {0.0f, 0.0f, 0.0f, 0.0f};
	Colour const ColourOne    = {1.0f, 1.0f, 1.0f, 1.0f};
	Colour const ColourWhite  = {1.0f, 1.0f, 1.0f, 1.0f};
	Colour const ColourBlack  = {0.0f, 0.0f, 0.0f, 1.0f};
	Colour const ColourRed    = {1.0f, 0.0f, 0.0f, 1.0f};
	Colour const ColourGreen  = {0.0f, 1.0f, 0.0f, 1.0f};
	Colour const ColourBlue   = {0.0f, 0.0f, 1.0f, 1.0f};
	
	// DirectXMath conversion functions
	PR_SUPPORT_DX(inline DirectX::XMVECTOR const& dxcv(Colour const& c) { return c.vec; })
	PR_SUPPORT_DX(inline DirectX::XMVECTOR&       dxcv(Colour&       c) { return c.vec; })
	
	inline Colour32& Colour32::operator = (Colour const& c)             { return set(c.r, c.g, c.b, c.a); }
	
	// Binary operators
	inline Colour operator + (Colour const& lhs, Colour const& rhs)     { Colour c = {Clamp(lhs.r+rhs.r,0.0f,1.0f), Clamp(lhs.g+rhs.g,0.0f,1.0f), Clamp(lhs.b+rhs.b,0.0f,1.0f), Clamp(lhs.a+rhs.a,0.0f,1.0f)}; return c; }
	inline Colour operator - (Colour const& lhs, Colour const& rhs)     { Colour c = {Clamp(lhs.r-rhs.r,0.0f,1.0f), Clamp(lhs.g-rhs.g,0.0f,1.0f), Clamp(lhs.b-rhs.b,0.0f,1.0f), Clamp(lhs.a-rhs.a,0.0f,1.0f)}; return c; }
	inline Colour operator * (Colour const& lhs, float s)               { Colour c = {Clamp(lhs.r*s,    0.0f,1.0f), Clamp(lhs.g*s,    0.0f,1.0f), Clamp(lhs.b*s,    0.0f,1.0f), Clamp(lhs.a*s,    0.0f,1.0f)}; return c; }
	inline Colour operator * (float s, Colour const& rhs)               { Colour c = {Clamp(s*rhs.r,    0.0f,1.0f), Clamp(s*rhs.g,    0.0f,1.0f), Clamp(s*rhs.b,    0.0f,1.0f), Clamp(s*rhs.a,    0.0f,1.0f)}; return c; }
	inline Colour operator / (Colour const& lhs, float s)               { Colour c = {Clamp(lhs.r/s,    0.0f,1.0f), Clamp(lhs.g/s,    0.0f,1.0f), Clamp(lhs.b/s,    0.0f,1.0f), Clamp(lhs.a/s,    0.0f,1.0f)}; return c; }
	
	// Equality operators
	inline bool FEql        (Colour const& lhs, Colour const& rhs)      { return FEql(lhs.r, rhs.r) && FEql(lhs.g, rhs.g) && FEql(lhs.b, rhs.b) && FEql(lhs.a, rhs.a); }
	inline bool FEqlNoA     (Colour const& lhs, Colour const& rhs)      { return FEql(lhs.r, rhs.r) && FEql(lhs.g, rhs.g) && FEql(lhs.b, rhs.b); }
	inline bool	EqualNoA    (Colour const& lhs, Colour const& rhs)      { return lhs.r == rhs.r && lhs.g == rhs.g && lhs.b == rhs.b; }
	inline bool	operator == (Colour const& lhs, Colour const& rhs)      { return memcmp(&lhs, &rhs, sizeof(lhs)) == 0; }
	inline bool	operator != (Colour const& lhs, Colour const& rhs)      { return memcmp(&lhs, &rhs, sizeof(lhs)) != 0; }
	inline bool operator <  (Colour const& lhs, Colour const& rhs)      { return memcmp(&lhs, &rhs, sizeof(lhs)) <  0; }
	inline bool operator >  (Colour const& lhs, Colour const& rhs)      { return memcmp(&lhs, &rhs, sizeof(lhs)) >  0; }
	inline bool operator <= (Colour const& lhs, Colour const& rhs)      { return memcmp(&lhs, &rhs, sizeof(lhs)) <= 0; }
	inline bool operator >= (Colour const& lhs, Colour const& rhs)      { return memcmp(&lhs, &rhs, sizeof(lhs)) >= 0; }
	
	// Assignment operators
	inline Colour& operator += (Colour& lhs, Colour const& rhs)         { lhs = lhs + rhs; return lhs; }
	inline Colour& operator -= (Colour& lhs, Colour const& rhs)         { lhs = lhs - rhs; return lhs; }
	inline Colour& operator *= (Colour& lhs, float s)                   { lhs = lhs * s; return lhs; }
	inline Colour& operator /= (Colour& lhs, float s)                   { lhs = lhs / s; return lhs; }
	
	// Conversion operators
	inline Colour32::operator Colour() const { return Colour::make(*this); }
	inline Colour::operator Colour32() const { return Colour32::make(r,g,b,a); }
	
	// Miscellaneous *******************************************************************************************
	
	// Find the 4D distance squared between two colours
	inline int DistanceSq(Colour32 lhs, Colour32 rhs)
	{
		return  Sqr((int)lhs.r() - (int)rhs.r()) +
				Sqr((int)lhs.g() - (int)rhs.g()) +
				Sqr((int)lhs.b() - (int)rhs.b()) +
				Sqr((int)lhs.a() - (int)rhs.a());
	}
	
	// Linear interpolate between colours
	inline Colour32 Lerp(Colour32 lhs, Colour32 rhs, float frac)
	{
		Colour32 col = Colour32Zero;
		for (int i = 0; i != 4; ++i)
			col[i] = uint8(lhs[i] * (1.0f - frac) + rhs[i] * frac);
		return col;
	}
}

#undef PR_SUPPORT_WINGDI
#undef PR_SUPPORT_DX

#endif
