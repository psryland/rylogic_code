//*******************************************************************************************
// Colour32
//  Copyright (c) Rylogic Ltd 2009
//*******************************************************************************************
#pragma once
#include <string>
#include <type_traits>
#include "pr/maths/maths.h"
#include "pr/macros/enum.h"
#include "pr/common/to.h"
#include "pr/common/interpolate.h"
#include "pr/str/to_string.h"

namespace pr
{
	struct Colour32;
	struct Colour;

	// Colour type traits
	#pragma region Traits
	template <typename T> struct is_colour :std::false_type
	{
		using elem_type = void;
	};
	template <> struct is_colour<Colour32> :std::true_type
	{
		using elem_type = uint8_t;
	};
	template <> struct is_colour<Colour> :std::true_type
	{
		using elem_type = float;
	};
	template <typename T> constexpr bool is_colour_v = is_colour<T>::value;
	template <typename T> using enable_if_col = typename std::enable_if_t<is_colour_v<T>>;

	namespace maths
	{
		template <> struct is_vec<Colour32> :std::true_type
		{
			using elem_type = uint8_t;
			using cp_type = uint8_t;
			static int const dim = 4;
		};
		template <> struct is_vec<Colour> :std::true_type
		{
			using elem_type = float;
			using cp_type = float;
			static int const dim = 4;
		};
	}
	#pragma endregion

	#pragma region Predefined Windows Colours
	#define PR_ENUM(x)\
		x(AliceBlue            , = 0xFFF0F8FF)\
		x(AntiqueWhite         , = 0xFFFAEBD7)\
		x(Aquamarine           , = 0xFF7FFFD4)\
		x(Azure                , = 0xFFF0FFFF)\
		x(Beige                , = 0xFFF5F5DC)\
		x(Bisque               , = 0xFFFFE4C4)\
		x(Black                , = 0xFF000000)\
		x(BlanchedAlmond       , = 0xFFFFEBCD)\
		x(Blue                 , = 0xFF0000FF)\
		x(BlueViolet           , = 0xFF8A2BE2)\
		x(Brown                , = 0xFFA52A2A)\
		x(BurlyWood            , = 0xFFDEB887)\
		x(CadetBlue            , = 0xFF5F9EA0)\
		x(Chartreuse           , = 0xFF7FFF00)\
		x(Chocolate            , = 0xFFD2691E)\
		x(Coral                , = 0xFFFF7F50)\
		x(CornflowerBlue       , = 0xFF6495ED)\
		x(Cornsilk             , = 0xFFFFF8DC)\
		x(Crimson              , = 0xFFDC143C)\
		x(Cyan                 , = 0xFF00FFFF)/* also Aqua
*/		x(DarkBlue             , = 0xFF00008B)\
		x(DarkCyan             , = 0xFF008B8B)\
		x(DarkGoldenrod        , = 0xFFB8860B)\
		x(DarkGrey             , = 0xFFA9A9A9)\
		x(DarkGreen            , = 0xFF006400)\
		x(DarkKhaki            , = 0xFFBDB76B)\
		x(DarkMagenta          , = 0xFF8B008B)\
		x(DarkOliveGreen       , = 0xFF556B2F)\
		x(DarkOrange           , = 0xFFFF8C00)\
		x(DarkOrchid           , = 0xFF9932CC)\
		x(DarkRed              , = 0xFF8B0000)\
		x(DarkSalmon           , = 0xFFE9967A)\
		x(DarkSeaGreen         , = 0xFF8FBC8F)\
		x(DarkSlateBlue        , = 0xFF483D8B)\
		x(DarkSlateGrey        , = 0xFF2F4F4F)\
		x(DarkTurquoise        , = 0xFF00CED1)\
		x(DarkViolet           , = 0xFF9400D3)\
		x(DeepPink             , = 0xFFFF1493)\
		x(DeepSkyBlue          , = 0xFF00BFFF)\
		x(DimGrey              , = 0xFF696969)\
		x(DodgerBlue           , = 0xFF1E90FF)\
		x(FireBrick            , = 0xFFB22222)\
		x(FloralWhite          , = 0xFFFFFAF0)\
		x(ForestGreen          , = 0xFF228B22)\
		x(Gainsboro            , = 0xFFDCDCDC)\
		x(GhostWhite           , = 0xFFF8F8FF)\
		x(Gold                 , = 0xFFFFD700)\
		x(Goldenrod            , = 0xFFDAA520)\
		x(Grey                 , = 0xFF808080)\
		x(Green                , = 0xFF008000)\
		x(GreenYellow          , = 0xFFADFF2F)\
		x(Honeydew             , = 0xFFF0FFF0)\
		x(HotPink              , = 0xFFFF69B4)\
		x(IndianRed            , = 0xFFCD5C5C)\
		x(Indigo               , = 0xFF4B0082)\
		x(Ivory                , = 0xFFFFFFF0)\
		x(Khaki                , = 0xFFF0E68C)\
		x(Lavender             , = 0xFFE6E6FA)\
		x(LavenderBlush        , = 0xFFFFF0F5)\
		x(LawnGreen            , = 0xFF7CFC00)\
		x(LemonChiffon         , = 0xFFFFFACD)\
		x(LightBlue            , = 0xFFADD8E6)\
		x(LightCoral           , = 0xFFF08080)\
		x(LightCyan            , = 0xFFE0FFFF)\
		x(LightGoldenrodYellow , = 0xFFFAFAD2)\
		x(LightGreen           , = 0xFF90EE90)\
		x(LightGrey            , = 0xFFD3D3D3)\
		x(LightPink            , = 0xFFFFB6C1)\
		x(LightSalmon          , = 0xFFFFA07A)\
		x(LightSeaGreen        , = 0xFF20B2AA)\
		x(LightSkyBlue         , = 0xFF87CEFA)\
		x(LightSlateGrey       , = 0xFF778899)\
		x(LightSteelBlue       , = 0xFFB0C4DE)\
		x(LightYellow          , = 0xFFFFFFE0)\
		x(Lime                 , = 0xFF00FF00)\
		x(LimeGreen            , = 0xFF32CD32)\
		x(Linen                , = 0xFFFAF0E6)\
		x(Magenta              , = 0xFFFF00FF)/* also Fuchsia
*/		x(Maroon               , = 0xFF800000)\
		x(MediumAquamarine     , = 0xFF66CDAA)\
		x(MediumBlue           , = 0xFF0000CD)\
		x(MediumOrchid         , = 0xFFBA55D3)\
		x(MediumPurple         , = 0xFF9370DB)\
		x(MediumSeaGreen       , = 0xFF3CB371)\
		x(MediumSlateBlue      , = 0xFF7B68EE)\
		x(MediumSpringGreen    , = 0xFF00FA9A)\
		x(MediumTurquoise      , = 0xFF48D1CC)\
		x(MediumVioletRed      , = 0xFFC71585)\
		x(MidnightBlue         , = 0xFF191970)\
		x(MintCream            , = 0xFFF5FFFA)\
		x(MistyRose            , = 0xFFFFE4E1)\
		x(Moccasin             , = 0xFFFFE4B5)\
		x(NavajoWhite          , = 0xFFFFDEAD)\
		x(Navy                 , = 0xFF000080)\
		x(OldLace              , = 0xFFFDF5E6)\
		x(Olive                , = 0xFF808000)\
		x(OliveDrab            , = 0xFF6B8E23)\
		x(Orange               , = 0xFFFFA500)\
		x(OrangeRed            , = 0xFFFF4500)\
		x(Orchid               , = 0xFFDA70D6)\
		x(PaleGoldenrod        , = 0xFFEEE8AA)\
		x(PaleGreen            , = 0xFF98FB98)\
		x(PaleTurquoise        , = 0xFFAFEEEE)\
		x(PaleVioletRed        , = 0xFFDB7093)\
		x(PapayaWhip           , = 0xFFFFEFD5)\
		x(PeachPuff            , = 0xFFFFDAB9)\
		x(Peru                 , = 0xFFCD853F)\
		x(Pink                 , = 0xFFFFC0CB)\
		x(Plum                 , = 0xFFDDA0DD)\
		x(PowderBlue           , = 0xFFB0E0E6)\
		x(Purple               , = 0xFF800080)\
		x(Red                  , = 0xFFFF0000)\
		x(RosyBrown            , = 0xFFBC8F8F)\
		x(RoyalBlue            , = 0xFF4169E1)\
		x(SaddleBrown          , = 0xFF8B4513)\
		x(Salmon               , = 0xFFFA8072)\
		x(SandyBrown           , = 0xFFF4A460)\
		x(SeaGreen             , = 0xFF2E8B57)\
		x(Seashell             , = 0xFFFFF5EE)\
		x(Sienna               , = 0xFFA0522D)\
		x(Silver               , = 0xFFC0C0C0)\
		x(SkyBlue              , = 0xFF87CEEB)\
		x(SlateBlue            , = 0xFF6A5ACD)\
		x(SlateGrey            , = 0xFF708090)\
		x(Snow                 , = 0xFFFFFAFA)\
		x(SpringGreen          , = 0xFF00FF7F)\
		x(SteelBlue            , = 0xFF4682B4)\
		x(Tan                  , = 0xFFD2B48C)\
		x(Teal                 , = 0xFF008080)\
		x(Thistle              , = 0xFFD8BFD8)\
		x(Tomato               , = 0xFFFF6347)\
		x(Turquoise            , = 0xFF40E0D0)\
		x(Violet               , = 0xFFEE82EE)\
		x(Wheat                , = 0xFFF5DEB3)\
		x(White                , = 0xFFFFFFFF)\
		x(WhiteSmoke           , = 0xFFF5F5F5)\
		x(Yellow               , = 0xFFFFFF00)\
		x(YellowGreen          , = 0xFF9ACD32)
	PR_DEFINE_ENUM2_BASE(EColours, PR_ENUM, uint32_t);
	#undef PR_ENUM
	#pragma endregion

	#pragma region Colour32

	// Equivalent to D3DCOLOR
	struct Colour32
	{
		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union
		{	// Note: 'argb' is little endian
			struct { uint32_t argb; };
			struct { uint16_t gb, ar; };
			struct { uint8_t b, g, r, a; };
			// struct { uint8_t arr[4]; }; - Removed because endian order causes this to be confusing
		};
		#pragma warning(pop)

		// Construct
		Colour32() = default;
		constexpr Colour32(uint32_t aarrggbb)
			:argb(aarrggbb)
		{}
		constexpr Colour32(int aarrggbb)
			:Colour32(static_cast<uint32_t>(aarrggbb))
		{}
		constexpr Colour32(EColours col)
			:Colour32(static_cast<uint32_t>(col))
		{}
		constexpr Colour32(uint8_t r_, uint8_t g_, uint8_t b_, uint8_t a_)
			:Colour32(uint32_t((a_ << 24) | (r_ << 16) | (g_ << 8) | (b_)))
		{}
		constexpr Colour32(int r_, int g_, int b_, int a_)
			:Colour32(
			uint8_t(Clamp(r_, 0, 255)),
			uint8_t(Clamp(g_, 0, 255)),
			uint8_t(Clamp(b_, 0, 255)),
			uint8_t(Clamp(a_, 0, 255)))
		{}
		constexpr Colour32(float r_, float g_, float b_, float a_)
			:Colour32(
			uint8_t(Clamp(r_ * 255.0f + 0.5f, 0.0f, 255.0f)),
			uint8_t(Clamp(g_ * 255.0f + 0.5f, 0.0f, 255.0f)),
			uint8_t(Clamp(b_ * 255.0f + 0.5f, 0.0f, 255.0f)),
			uint8_t(Clamp(a_ * 255.0f + 0.5f, 0.0f, 255.0f)))
		{}
		template <typename T, typename = enable_if_col<T>>
		constexpr Colour32(T const& c)
			:Colour32(r_cp(c), g_cp(c), b_cp(c), a_cp(c))
		{}

		// Operators
		constexpr Colour32& operator = (int i)
		{
			argb = uint32_t(i);
			return *this;
		}
		constexpr Colour32& operator = (uint32_t i)
		{
			argb = i;
			return *this;
		}
		template <typename T, typename = enable_if_col<T>>
		constexpr Colour32& operator = (T const& c)
		{
			return *this = Colour32(r_cp(c), g_cp(c), b_cp(c), a_cp(c));
		}
		constexpr Colour32 operator ~() const
		{
			return Colour32((argb & 0xFF000000) | (argb ^ 0xFFFFFFFF));
		}
		constexpr operator uint32_t() const
		{
			return argb;
		}

		// Component accessors
		constexpr Colour32 rgba() const
		{
			return Colour32(((argb & 0x00ffffff) << 8) | (argb >> 24));
		}

		// Set alpha channel
		constexpr Colour32 a0() const
		{
			return Colour32(argb & 0x00FFFFFF);
		}
		constexpr Colour32 a1() const
		{
			return Colour32(argb | 0xFF000000);
		}

		// Operators
		friend constexpr bool operator == (Colour32 lhs, Colour32 rhs)
		{
			return lhs.argb == rhs.argb;
		}
		friend constexpr bool operator != (Colour32 lhs, Colour32 rhs)
		{
			return lhs.argb != rhs.argb;
		}
		friend constexpr bool operator <  (Colour32 lhs, Colour32 rhs)
		{
			return lhs.argb < rhs.argb;
		}
		friend constexpr bool operator >  (Colour32 lhs, Colour32 rhs)
		{
			return lhs.argb > rhs.argb;
		}
		friend constexpr bool operator <= (Colour32 lhs, Colour32 rhs)
		{
			return lhs.argb <= rhs.argb;
		}
		friend constexpr bool operator >= (Colour32 lhs, Colour32 rhs)
		{
			return lhs.argb >= rhs.argb;
		}
		friend constexpr bool EqualNoA(Colour32 lhs, Colour32 rhs)
		{
			return lhs.a0() == rhs.a0();
		}

		friend constexpr Colour32 operator + (Colour32 lhs, Colour32 rhs)
		{
			return Colour32(
				lhs.r + rhs.r,
				lhs.g + rhs.g,
				lhs.b + rhs.b,
				lhs.a + rhs.a);
		}
		friend constexpr Colour32 operator - (Colour32 lhs, Colour32 rhs)
		{
			return Colour32(
				lhs.r - rhs.r,
				lhs.g - rhs.g,
				lhs.b - rhs.b,
				lhs.a - rhs.a);
		}
		friend constexpr Colour32 operator * (Colour32 lhs, float s)
		{
			return Colour32(
				lhs.r * s,
				lhs.g * s,
				lhs.b * s,
				lhs.a * s);
		}
		friend constexpr Colour32 operator * (float s, Colour32 rhs)
		{
			return rhs * s;
		}
		friend constexpr Colour32 operator * (Colour32 lhs, Colour32 rhs)
		{
			return Colour32(
				lhs.r * rhs.r / 255,
				lhs.g * rhs.g / 255,
				lhs.b * rhs.b / 255,
				lhs.a * rhs.a / 255);
		}
		friend constexpr Colour32 operator / (Colour32 lhs, float s)
		{
			assert("divide by zero" && s != 0);
			return lhs * 1.0f / s;
		}
		friend constexpr Colour32 operator % (Colour32 lhs, int s)
		{
			assert("divide by zero" && s != 0);
			return Colour32(
				lhs.r % s,
				lhs.g % s,
				lhs.b % s,
				lhs.a % s);
		}
		friend constexpr Colour32& operator += (Colour32& lhs, Colour32 rhs)
		{
			return lhs = lhs + rhs;
		}
		friend constexpr Colour32& operator -= (Colour32& lhs, Colour32 rhs)
		{
			return lhs = lhs - rhs;
		}
		friend constexpr Colour32& operator *= (Colour32& lhs, float s)
		{
			return lhs = lhs * s;
		}
		friend constexpr Colour32& operator *= (Colour32& lhs, Colour32 rhs)
		{
			return lhs = lhs * rhs;
		}
		friend constexpr Colour32& operator /= (Colour32& lhs, float s)
		{
			return lhs = lhs / s;
		}
		friend constexpr Colour32& operator %= (Colour32& lhs, int s)
		{
			return lhs = lhs % s;
		}
	};
	static_assert(std::is_trivially_copyable_v<Colour32>, "Colour32 should be a pod type");
	static_assert(is_colour_v<Colour32>, "");

	// Define component accessors
	constexpr float r_cp(Colour32 v) { return v.r / 255.0f; }
	constexpr float g_cp(Colour32 v) { return v.g / 255.0f; }
	constexpr float b_cp(Colour32 v) { return v.b / 255.0f; }
	constexpr float a_cp(Colour32 v) { return v.a / 255.0f; }
	constexpr float x_cp(Colour32 v) { return r_cp(v); }
	constexpr float y_cp(Colour32 v) { return g_cp(v); }
	constexpr float z_cp(Colour32 v) { return b_cp(v); }
	constexpr float w_cp(Colour32 v) { return a_cp(v); }

	#pragma region Constants
	constexpr Colour32 Colour32Zero   = { 0x00000000 };
	constexpr Colour32 Colour32One    = { 0xFFFFFFFF };
	constexpr Colour32 Colour32White  = { 0xFFFFFFFF };
	constexpr Colour32 Colour32Black  = { 0xFF000000 };
	constexpr Colour32 Colour32Red    = { 0xFFFF0000 };
	constexpr Colour32 Colour32Green  = { 0xFF00FF00 };
	constexpr Colour32 Colour32Blue   = { 0xFF0000FF };
	constexpr Colour32 Colour32Yellow = { 0xFFFFFF00 };
	constexpr Colour32 Colour32Purple = { 0xFFFF00FF };
	constexpr Colour32 Colour32Gray   = { 0xFF808080 };
	#pragma endregion

	#pragma region Functions

	// True if 'col' requires alpha blending
	inline bool HasAlpha(Colour32 col)
	{
		return col.a != 0x00 && col.a != 0xFF;
	}

	// Find the 4D distance squared between two colours
	inline int DistanceSq(Colour32 lhs, Colour32 rhs)
	{
		return
			Sqr(lhs.r - rhs.r) +
			Sqr(lhs.g - rhs.g) +
			Sqr(lhs.b - rhs.b) +
			Sqr(lhs.a - rhs.a);
	}

	// Linearly interpolate between colours
	inline Colour32 Lerp(Colour32 lhs, Colour32 rhs, float frac)
	{
		return Colour32(
			int(lhs.r * (1.0f - frac) + rhs.r * frac),
			int(lhs.g * (1.0f - frac) + rhs.g * frac),
			int(lhs.b * (1.0f - frac) + rhs.b * frac),
			int(lhs.a * (1.0f - frac) + rhs.a * frac));
	}

	// Convert this colour to it's associated gray-scale value
	inline Colour32 ToGrayScale(Colour32 col)
	{
		auto gray = static_cast<uint8_t>(0.3f*col.r + 0.59f*col.g + 0.11f*col.b);
		return Colour32(gray, gray, gray, col.a);
	}

	// Create a random colour
	template <typename Rng = std::default_random_engine> inline Colour32 RandomRGB(Rng& rng, float a)
	{
		std::uniform_real_distribution<float> dist(0.0f, 1.0f);
		return Colour32(dist(rng), dist(rng), dist(rng), a);
	}

	// Create a random colour
	template <typename Rng = std::default_random_engine> inline Colour32 RandomRGB(Rng& rng)
	{
		return RandomRGB(rng, 1.0f);
	}

	#pragma endregion

	#pragma endregion

	#pragma region Colour

	// Equivalent to pr::v4, XMVECTOR, D3DCOLORVALUE, etc
	struct alignas(16) Colour
	{
		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct/union
		union
		{
			struct { float r, g, b, a; };
			struct { v4 rgba; };
			struct { v3 rgb; float a; };
			struct { float arr[4]; };
			#if PR_MATHS_USE_INTRINSICS
			__m128 vec;
			#endif
		};
		#pragma warning(pop)

		Colour() = default;
		Colour(float r_, float g_, float b_, float a_)
			#if PR_MATHS_USE_INTRINSICS
			:vec(_mm_set_ps(a_, b_, g_, r_))
			#else
			: r(r_)
			, g(g_)
			, b(b_)
			, a(a_)
			#endif
		{
			// Note: Do not clamp values, use 'Clamp' if that's what you want
			assert(maths::is_aligned(this));
		}
		Colour(uint8_t r_, uint8_t g_, uint8_t b_, uint8_t a_)
			:Colour(r_ / 255.0f, g_ / 255.0f, b_ / 255.0f, a_ / 255.0f)
		{}
		Colour(Colour32 c32)
			:Colour(c32.r, c32.g, c32.b, c32.a)
		{}
		Colour(Colour32 c32, float alpha)
			:Colour(c32.r / 255.0f, c32.g / 255.0f, c32.b / 255.0f, alpha)
		{}
		template <typename T, typename = enable_if_col<T>> Colour(T const& v)
			: Colour(r_cp(v), g_cp(v), b_cp(v), a_cp(v))
		{}
		#if PR_MATHS_USE_INTRINSICS
		Colour(__m128 v)
			: vec(v)
		{
			assert(maths::is_aligned(this));
		}
		#endif

		// Array access
		float const& operator [] (int i) const
		{
			assert("index out of range" && i >= 0 && i < _countof(arr));
			return arr[i];
		}
		float& operator [] (int i)
		{
			assert("index out of range" && i >= 0 && i < _countof(arr));
			return arr[i];
		}

		// Operators
		Colour& operator = (Colour32 c32)
		{
			return *this = Colour(c32);
		}
		template <typename T, typename = enable_if_col<T>> Colour& operator = (T const& c)
		{
			return *this = Colour(r_cp(c), g_cp(c), b_cp(c), a_cp(c));
		}

		// Component accessors
		Colour32 argb() const
		{
			return Colour32(r, g, b, a);
		}

		// Set alpha channel
		Colour a0() const
		{
			return Colour(r, g, b, 0.0f);
		}
		Colour a1() const
		{
			return Colour(r, g, b, 1.0f);
		}

		// Note: operators do not clamp values, use 'Clamp' if that's what you want
		friend bool operator == (Colour const& lhs, Colour const& rhs)
		{
			return memcmp(&lhs, &rhs, sizeof(lhs)) == 0;
		}
		friend bool operator != (Colour const& lhs, Colour const& rhs)
		{
			return memcmp(&lhs, &rhs, sizeof(lhs)) != 0;
		}
		friend bool operator <  (Colour const& lhs, Colour const& rhs)
		{
			return memcmp(&lhs, &rhs, sizeof(lhs)) < 0;
		}
		friend bool operator >  (Colour const& lhs, Colour const& rhs)
		{
			return memcmp(&lhs, &rhs, sizeof(lhs)) > 0;
		}
		friend bool operator <= (Colour const& lhs, Colour const& rhs)
		{
			return memcmp(&lhs, &rhs, sizeof(lhs)) <= 0;
		}
		friend bool operator >= (Colour const& lhs, Colour const& rhs)
		{
			return memcmp(&lhs, &rhs, sizeof(lhs)) >= 0;
		}
		friend bool EqualNoA(Colour const& lhs, Colour const& rhs)
		{
			return lhs.r == rhs.r && lhs.g == rhs.g && lhs.b == rhs.b;
		}

		friend Colour pr_vectorcall operator + (Colour const& lhs, Colour const& rhs)
		{
			return Colour(
				lhs.r + rhs.r,
				lhs.g + rhs.g,
				lhs.b + rhs.b,
				lhs.a + rhs.a);
		}
		friend Colour pr_vectorcall operator - (Colour const& lhs, Colour const& rhs)
		{
			return Colour(
				lhs.r - rhs.r,
				lhs.g - rhs.g,
				lhs.b - rhs.b,
				lhs.a - rhs.a);
		}
		friend Colour pr_vectorcall operator * (Colour const& lhs, float s)
		{
			return Colour(
				lhs.r * s,
				lhs.g * s,
				lhs.b * s,
				lhs.a * s);
		}
		friend Colour pr_vectorcall operator * (float s, Colour const& rhs)
		{
			return rhs * s;
		}
		friend Colour pr_vectorcall operator * (Colour const& lhs, Colour const& rhs)
		{
			return Colour32(
				lhs.r * rhs.r,
				lhs.g * rhs.g,
				lhs.b * rhs.b,
				lhs.a * rhs.a);
		}
		friend Colour pr_vectorcall operator / (Colour const& lhs, float s)
		{
			assert("divide by zero" && s != 0);
			return Colour(
				lhs.r / s,
				lhs.g / s,
				lhs.b / s,
				lhs.a / s);
		}
		friend Colour& pr_vectorcall operator += (Colour& lhs, Colour const& rhs)
		{
			return lhs = lhs + rhs;
		}
		friend Colour& pr_vectorcall operator -= (Colour& lhs, Colour const& rhs)
		{
			return lhs = lhs - rhs;
		}
		friend Colour& pr_vectorcall operator *= (Colour& lhs, float s)
		{
			return lhs = lhs * s;
		}
		friend Colour& pr_vectorcall operator /= (Colour& lhs, float s)
		{
			return lhs = lhs / s;
		}
	};
	static_assert(is_colour_v<Colour>, "");
	static_assert(std::is_trivially_copyable_v<Colour>, "Colour should be a pod type");
	static_assert(std::alignment_of_v<Colour> == 16, "Colour should have 16 byte alignment");
	#if PR_MATHS_USE_INTRINSICS && !defined(_M_IX86)
	using Colour_cref = Colour const;
	#else
	using Colour_cref = Colour const&;
	#endif

	// Define component accessors
	inline float pr_vectorcall r_cp(Colour_cref v) { return v.r; }
	inline float pr_vectorcall g_cp(Colour_cref v) { return v.g; }
	inline float pr_vectorcall b_cp(Colour_cref v) { return v.b; }
	inline float pr_vectorcall a_cp(Colour_cref v) { return v.a; }

	inline float pr_vectorcall x_cp(Colour_cref v) { return r_cp(v); }
	inline float pr_vectorcall y_cp(Colour_cref v) { return g_cp(v); }
	inline float pr_vectorcall z_cp(Colour_cref v) { return b_cp(v); }
	inline float pr_vectorcall w_cp(Colour_cref v) { return a_cp(v); }

	#pragma region Constants
	Colour const ColourZero   = {0.0f, 0.0f, 0.0f, 0.0f};
	Colour const ColourOne    = {1.0f, 1.0f, 1.0f, 1.0f};
	Colour const ColourWhite  = {1.0f, 1.0f, 1.0f, 1.0f};
	Colour const ColourBlack  = {0.0f, 0.0f, 0.0f, 1.0f};
	Colour const ColourRed    = {1.0f, 0.0f, 0.0f, 1.0f};
	Colour const ColourGreen  = {0.0f, 1.0f, 0.0f, 1.0f};
	Colour const ColourBlue   = {0.0f, 0.0f, 1.0f, 1.0f};
	#pragma endregion

	#pragma region Functions
	// Colour FEql
	inline bool pr_vectorcall FEqlRelative(Colour_cref lhs, Colour_cref rhs, float tol)
	{
		#if PR_MATHS_USE_INTRINSICS
		const __m128 zero = {tol, tol, tol, tol};
		auto d = _mm_sub_ps(lhs.vec, rhs.vec);                         /// d = lhs - rhs
		auto r = _mm_cmple_ps(_mm_mul_ps(d,d), _mm_mul_ps(zero,zero)); /// r = sqr(d) <= sqr(zero)
		return (_mm_movemask_ps(r) & 0x0f) == 0x0f;
		#else
		return
			FEqlRelative(lhs.r, rhs.r, tol) &&
			FEqlRelative(lhs.g, rhs.g, tol) &&
			FEqlRelative(lhs.b, rhs.b, tol) &&
			FEqlRelative(lhs.a, rhs.a, tol);
		#endif
	}
	inline bool pr_vectorcall FEql(Colour_cref lhs, Colour_cref rhs)
	{
		return FEqlRelative(lhs, rhs, maths::tinyf);
	}
	inline bool pr_vectorcall FEqlNoA(Colour_cref lhs, Colour_cref rhs)
	{
		return FEql(lhs.a0(), rhs.a0());
	}

	// Clamp colour values to the interval [mn,mx]
	inline Colour Clamp(Colour_cref c, float mn, float mx)
	{
		return Colour(
			Clamp(c.r, mn, mx),
			Clamp(c.g, mn, mx),
			Clamp(c.b, mn, mx),
			Clamp(c.a, mn, mx));
	}

	// Normalise all components of 'v'
	inline Colour pr_vectorcall Normalise(Colour_cref v)
	{
		#if PR_MATHS_USE_INTRINSICS
		return Colour(_mm_div_ps(v.vec, _mm_sqrt_ps(_mm_dp_ps(v.vec, v.vec, 0xFF))));
		#else
		return v / Length4(v);
		#endif
	}
	#pragma endregion

	#pragma endregion

	#pragma region COLORREF
	#if defined(_WINGDI_) && !defined(NOGDI)
	inline float r_cp(COLORREF v) { return GetRValue(v) / 255.0f; }
	inline float g_cp(COLORREF v) { return GetGValue(v) / 255.0f; }
	inline float b_cp(COLORREF v) { return GetBValue(v) / 255.0f; }
	inline float a_cp(COLORREF)   { return 1.0f; }

	// Treat COLORREF as a colour type
	template <> struct is_colour<COLORREF> :std::true_type
	{
		using elem_type = uint8_t;
	};
	#endif
	#pragma endregion

	#pragma region Conversion

	namespace convert
	{
		// Colour to string
		template <typename Str, typename Char = typename Str::value_type>
		struct ColourToString
		{
			static Str To(Colour32 c)
			{
				return pr::To<Str>(c.argb, 16);
			}
			static Str To(Colour const& c)
			{
				return To(static_cast<Colour32>(c));
			}
		};

		// Whatever to Colour32
		struct ToColour32
		{
			template <typename Str, typename Char = typename string_traits<Str>::value_type, typename = std::enable_if_t<is_string_v<Str>>>
			static Colour32 To(Str const& s, Char const** end = nullptr)
			{
				auto ptr = string_traits<Str>::ptr(s);
				auto argb = pr::To<unsigned int>(ptr, 16, end); // pr:: needed
				return Colour32(argb);
			}
			static Colour32 To(Colour const& c)
			{
				return c.argb();
			}
		};

		// Whatever to Colour
		struct ToColour
		{
			template <typename Str, typename Char = typename string_traits<Str>::value_type, typename = std::enable_if_t<is_string_v<Str>>>
			static Colour To(Str const& s, Char const** end = nullptr)
			{
				Char* e;
				auto r = pr::To<float>(s, &e);
				auto g = pr::To<float>(e, &e);
				auto b = pr::To<float>(e, &e);
				auto a = pr::To<float>(e, &e);
				if (end) *end = e;
				return Colour(r,g,b,a);
			}
			static Colour To(Colour32 c)
			{
				return static_cast<Colour>(c);
			}
		};

		// Whatever to D3DCOLORVALUE
		#ifdef D3DCOLORVALUE_DEFINED
		struct ToD3DCOLORVALUE
		{
			static D3DCOLORVALUE To(Colour const& c)
			{
				return D3DCOLORVALUE{c.r, c.g, c.b, c.a};
			}
			static D3DCOLORVALUE To(Colour32 c)
			{
				return To(static_cast<Colour>(c));
			}
		};
		#endif
	}
	template <typename Char>                struct Convert<std::basic_string<Char>, Colour32> :convert::ColourToString<std::basic_string<Char>> {};
	template <typename Char>                struct Convert<std::basic_string<Char>, Colour>   :convert::ColourToString<std::basic_string<Char>> {};
	template <typename Char, int L, bool F> struct Convert<pr::string<Char,L,F>,    Colour32> :convert::ColourToString<pr::string<Char,L,F>> {};
	template <typename Char, int L, bool F> struct Convert<pr::string<Char,L,F>,    Colour>   :convert::ColourToString<pr::string<Char,L,F>> {};
	template <typename TFrom> struct Convert<Colour32, TFrom> :convert::ToColour32 {};
	template <typename TFrom> struct Convert<Colour,   TFrom> :convert::ToColour {};
	#ifdef D3DCOLORVALUE_DEFINED
	template <typename TFrom> struct Convert<D3DCOLORVALUE, TFrom> :convert::ToD3DCOLORVALUE {};
	#endif

	// Write a colour to a stream
	template <typename Char>
	inline std::basic_ostream<Char>& operator << (std::basic_ostream<Char>& out, Colour32 col)
	{
		auto str = To<std::basic_string<Char>>(col);
		return out << str.c_str();
	}
	template <typename Char, typename TColour, typename = enable_if_col<TColour>>
	inline std::basic_ostream<Char>& operator << (std::basic_ostream<Char>& out, TColour const& col)
	{
		auto str = To<std::basic_string<Char>>(Colour32(col));
		return out << str.c_str();
	}

	#pragma endregion

	#pragma region Interpolate

	template <> struct Interpolate<Colour32>
	{
		struct Point
		{
			template <typename F> Colour32 operator()(Colour32 lhs, Colour32, F, F) const
			{
				return lhs;
			}
		};
		struct Linear
		{
			template <typename F> Colour32 operator()(Colour32 lhs, Colour32 rhs, F n, F N) const
			{
				if (N-- <= 1) return lhs;
				return Lerp(lhs, rhs, float(n)/N);
			}
		};
	};

	#pragma endregion
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::gfx
{
	PRUnitTest(ColourTests)
	{
		Colour32 c0(0xFF, 0xFF, 0xFF, 0xFF);
		PR_CHECK(c0.argb, 0xFFFFFFFFU);
	}
}
#endif