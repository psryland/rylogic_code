//********************************
// Ldraw Script type wrappers
//  Copyright (c) Rylogic Ltd 2014
//********************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/ldraw/ldraw.h"

namespace pr::rdr12::ldraw
{
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
		O2W() :m_mat(m4x4::Identity()) {}
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
		Colour32 m_col;
		EKeyword m_kw;
		Col() : m_col(0xFFFFFFFF), m_kw(EKeyword::Colour) {}
		Col(Colour32 c) :m_col(c), m_kw(EKeyword::Colour) {}
		Col(uint32_t argb) :m_col(argb), m_kw(EKeyword::Colour) {}
		Col(Colour32 c, EKeyword kw) :m_col(c), m_kw(kw) {}
	};
	struct Size
	{
		float m_size;
		Size() :m_size(0) {}
		Size(float size) :m_size(size) {}
		Size(int size) :m_size(float(size)) {}
	};
	struct Size2
	{
		v2 m_size;
		Size2() :m_size() {}
		Size2(v2 size) :m_size(size) {}
		Size2(iv2 size) :m_size(float(size.x), float(size.y)) {}
	};
	struct Width
	{
		float m_width;
		Width() :m_width(0) {}
		Width(float w) :m_width(w) {}
		Width(int w) :m_width(float(w)) {}
	};
	struct Scale2
	{
		v2 m_scale;
		Scale2() : m_scale(v2::One()) {}
		Scale2(v2 scale) : m_scale(scale) {}
	};
	struct Scale3
	{
		v3 m_scale;
		Scale3() : m_scale(v3::One()) {}
		Scale3(v3 scale) : m_scale(scale) {}
	};
	struct PerItemColour
	{
		bool m_per_item_colour;
		PerItemColour() : m_per_item_colour() {}
		PerItemColour(bool has_colours) : m_per_item_colour(has_colours) {}
		operator bool() const { return m_per_item_colour; }
	};
	struct Depth
	{
		bool m_depth;
		Depth() :m_depth(false) {}
		Depth(bool d) : m_depth(d) {}
	};
	struct Wireframe
	{
		bool m_wire;
		Wireframe() :m_wire(false) {}
		Wireframe(bool w) :m_wire(w) {}
	};
	struct Alpha
	{
		bool m_has_alpha;
		Alpha() :m_has_alpha(false) {}
		Alpha(bool a) : m_has_alpha(a) {}
	};
	struct Solid
	{
		bool m_solid;
		Solid() :m_solid(false) {}
		Solid(bool s) : m_solid(s) {}
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
				out.overwrite(s_cast<ptrdiff_t>(ofs), data);
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
