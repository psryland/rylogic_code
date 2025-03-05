//********************************
// Ldraw Script type wrappers
//  Copyright (c) Rylogic Ltd 2014
//********************************
#pragma once
#include "pr/view3d-12/forward.h"

namespace pr::rdr12::ldraw
{	
	// Arrow styles
	#define PR_ENUM(x)\
		x(Line        ,= 0)\
		x(Fwd         ,= 1 << 0)\
		x(Back        ,= 1 << 1)\
		x(FwdBack     ,= Fwd | Back)\
		x(_flags_enum ,= 0xFF)
	PR_DEFINE_ENUM2_BASE(EArrowType, PR_ENUM, uint8_t);
	#undef PR_ENUM

	// Point styles
	#define PR_ENUM(x)\
		x(Square)\
		x(Circle)\
		x(Triangle)\
		x(Star)\
		x(Annulus)
	PR_DEFINE_ENUM1_BASE(EPointStyle, PR_ENUM, uint8_t);
	#undef PR_ENUM

	struct Str
	{
		std::string m_str;
		Str(std::string const& str) :m_str(str) {}
		Str(std::wstring const& str) :m_str(Narrow(str)) {}
	};
	struct Pos
	{
		v4 m_pos;
		Pos(v4 const& pos) :m_pos(pos) {}
		Pos(m4x4 const& mat) :m_pos(mat.pos) {}
	};
	struct O2W
	{
		m4x4 m_mat;
		O2W() :m_mat(m4x4Identity) {}
		O2W(v4 const& pos) :m_mat(m4x4::Translation(pos)) {}
		O2W(m4x4 const& mat) :m_mat(mat) {}
	};
	struct Name
	{
		std::string m_name;
		Name() :m_name() {}
		Name(std::string_view str) :m_name(str) {}
		Name(std::wstring_view str) :m_name(Narrow(str)) {}
		template <int N> Name(char const (&str)[N]) :m_name(str) {}
		template <int N> Name(wchar_t const (&str)[N]) :m_name(Narrow(str)) {}
	};
	struct Col
	{
		union {
		Colour32 m_col;
		unsigned int m_ui;
		};
		Col() :Col(0xFFFFFFFF) {}
		Col(Colour32 c) :m_col(c) {}
		Col(unsigned int ui) :m_ui(ui) {}
	};
	struct Size
	{
		float m_size;
		Size() :m_size(0) {}
		Size(float size) :m_size(size) {}
		Size(int size) :m_size(float(size)) {}
	};
	struct Width
	{
		float m_width;
		Width() :m_width(0) {}
		Width(float w) :m_width(w) {}
		Width(int w) :m_width(float(w)) {}
	};
	struct Wireframe
	{
		bool m_wire;
		Wireframe() :m_wire(false) {}
		Wireframe(bool w) :m_wire(w) {}
	};
	struct Solid
	{
		bool m_solid;
		Solid() :m_solid(false) {}
		Solid(bool s) : m_solid(s) {}
	};
	struct Depth
	{
		bool m_depth;
		Depth() :m_depth(false) {}
		Depth(bool d) : m_depth(d) {}
	};
	struct PointStyle
	{
		EPointStyle m_style;
		PointStyle() :m_style() {}
		PointStyle(EPointStyle s) : m_style(s) {}
	};
	struct VariableInt
	{
		int m_value;
		VariableInt() :m_value() {}
		VariableInt(int value) : m_value(value & 0x3FFFFFFF) {}
	};
	struct StringWithLength
	{
		std::string_view m_value;
		StringWithLength() : m_value() {}
		StringWithLength(std::string_view value) : m_value(value) {}
	};

	// Traits for an output stream (e.g. 'std::ostream')
	template <typename TOut> struct traits
	{
		// Binary writer traits
		static TOut& write(TOut& out, std::span<std::byte const> data, int64_t ofs = -1)
		{
			if (ofs != -1)
				out.seekp(ofs).write(char_ptr(data.data()), data.size()).seekp(0, std::ios::end);
			else
				out.write(char_ptr(data.data()), data.size());
			return out;
		}
		static int64_t tellp(TOut& out)
		{
			return out.tellp();
		}

		// Text writer traits
		static TOut& append(TOut& out, std::string_view data)
		{
			out.write(data.data(), data.size());
			return out;
		}
		static int last(TOut&)
		{
			// Last emitted char, if available
			return char_traits<char>::eof();
		}
	};
	template <> struct traits<std::string>
	{
		static std::string& append(std::string& out, std::string_view data)
		{
			out.append(data);
			return out;
		}
		static int last(std::string& out)
		{
			return !out.empty() ? out.back() : char_traits<char>::eof();
		}
	};
	template <> struct traits<byte_data<4>>
	{
		static byte_data<4>& write(byte_data<4>& out, std::span<std::byte const> data, int64_t ofs = -1)
		{
			if (ofs != -1)
				out.overwrite(ofs, data);
			else
				out.append(data);
			return out;
		}
		static int64_t tellp(byte_data<4>& out)
		{
			return out.size();
		}
	};

	// Types that can be 'memcpy'ed
	template <typename T> concept PrimitiveType =
		std::is_integral_v<T> ||
		std::is_floating_point_v<T> ||
		std::is_enum_v<T> ||
		maths::VecOrMatType<T> ||
		false;
	template <typename T> concept PrimitiveSpanType = requires(T t)
	{
		PrimitiveType<typename T::value_type>;
		{ t.data() } -> std::same_as<typename T::value_type*>;
		{ t.size() } -> std::same_as<std::size_t>;
	};
}
