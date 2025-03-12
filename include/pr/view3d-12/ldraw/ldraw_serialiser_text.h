//********************************
// Ldraw Script Text Serialiser
//  Copyright (c) Rylogic Ltd 2025
//********************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/ldraw/ldraw.h"
#include "pr/view3d-12/ldraw/ldraw_parsing.h"
#include "pr/view3d-12/ldraw/ldraw_serialiser.h"

namespace pr::rdr12::ldraw
{
	struct TextWriter
	{
		template <typename TOut, typename Type> static void Append(TOut& out, Type)
		{
			// Note: if you hit this error, it's probably because Append(out, ???) is being
			// called where ??? is a type not handled by the overloads of Append.
			// Also watch out for the error being a typedef of a common type,
			// e.g. I've seen 'Type=std::ios_base::openmode' as an error, but really it was
			// 'Type=int' that was missing, because of 'typedef int std::ios_base::openmode'
			static_assert(dependent_false<Type>, "no overload for 'Type'");
		}
		template <typename TOut> static void Append(TOut& out, std::string_view s)
		{
			if (s.empty()) return;
			if (*s.data() != '}' && *s.data() != ')' && *s.data() != ' ' && traits<TOut>::last(out) != '{')
			{
				traits<TOut>::append(out, " ");
			}
			traits<TOut>::append(out, s);
		}
		template <typename TOut> static void Append(TOut& out, std::string&& s)
		{
			Append(out, std::string_view{ s });
		}
		template <typename TOut> static void Append(TOut& out, char const* s)
		{
			Append(out, std::string_view{ s });
		}
		template <typename TOut> static void Append(TOut& out, string32 s)
		{
			Append(out, std::string_view{ s });
		}
		template <typename TOut> static void Append(TOut& out, bool b)
		{
			Append(out, b ? "true" : "false");
		}
		template <typename TOut> static void Append(TOut& out, int i)
		{
			Append(out, To<string32>(i));
		}
		template <typename TOut> static void Append(TOut& out, long i)
		{
			Append(out, To<string32>(i));
		}
		template <typename TOut> static void Append(TOut& out, float f)
		{
			Append(out, To<string32>(f));
		}
		template <typename TOut> static void Append(TOut& out, double f)
		{
			Append(out, To<string32>(f));
		}
		template <typename TOut> static void Append(TOut& out, uint32_t u)
		{
			Append(out, std::format("{:08X}", u));
		}
		template <typename TOut> static void Append(TOut& out, Colour32 c)
		{
			Append(out, c.argb);
		}
		template <typename TOut> static void Append(TOut& out, v2 v)
		{
			Append(out, To<string32>(v));
		}
		template <typename TOut> static void Append(TOut& out, v3 v)
		{
			Append(out, To<string32>(v));
		}
		template <typename TOut> static void Append(TOut& out, v4 v)
		{
			Append(out, To<string32>(v));
		}
		template <typename TOut> static void Append(TOut& out, m4x4 m)
		{
			Append(out, To<string32>(m));
		}
		template <typename TOut, Scalar S> static void Append(TOut& out, Vec2<S, void> v)
		{
			Append(out, To<string32>(v));
		}
		template <typename TOut, Scalar S> static void Append(TOut& out, Vec3<S, void> v)
		{
			Append(out, To<string32>(v));
		}
		template <typename TOut, Scalar S> static void Append(TOut& out, Vec4<S, void> v)
		{
			Append(out, To<string32>(v));
		}
		template <typename TOut, Scalar S> static void Append(TOut& out, Mat4x4<S, void, void> m)
		{
			Append(out, To<string32>(m));
		}
		template <typename TOut> static void Append(TOut& out, EArrowType type)
		{
			switch (type)
			{
				case EArrowType::Fwd:     return Append(out, "Fwd");
				case EArrowType::Back:    return Append(out, "Back");
				case EArrowType::FwdBack: return Append(out, "FwdBack");
				default: throw std::runtime_error("Unknown arrow type");
			}
		}
		template <typename TOut> static void Append(TOut& out, ETexAddrMode addr)
		{
			switch (addr)
			{
				case ETexAddrMode::Wrap:       return Append(out, "Wrap");
				case ETexAddrMode::Mirror:     return Append(out, "Mirror");
				case ETexAddrMode::Clamp:      return Append(out, "Clamp");
				case ETexAddrMode::Border:     return Append(out, "Border");
				case ETexAddrMode::MirrorOnce: return Append(out, "MirrorOnce");
				default: throw std::runtime_error("Unknown texture addressing mode");
			}
		}
		template <typename TOut> static void Append(TOut& out, EFilter filter)
		{
			switch (filter)
			{
				case EFilter::Point:             return Append(out, "Point");
				case EFilter::PointPointLinear:  return Append(out, "PointPointLinear");
				case EFilter::PointLinearPoint:  return Append(out, "PointLinearPoint");
				case EFilter::PointLinearLinear: return Append(out, "PointLinearLinear");
				case EFilter::LinearPointPoint:  return Append(out, "LinearPointPoint");
				case EFilter::LinearPointLinear: return Append(out, "LinearPointLinear");
				case EFilter::LinearLinearPoint: return Append(out, "LinearLinearPoint");
				case EFilter::Linear:            return Append(out, "Linear");
				case EFilter::Anisotropic:       return Append(out, "Anisotropic");
				default: throw std::runtime_error("Unknown texture addressing mode");
			}
		}
		template <typename TOut> static void Append(TOut& out, EPointStyle style)
		{
			switch (style)
			{
				case EPointStyle::Square:   return Append(out, "Square");
				case EPointStyle::Circle:   return Append(out, "Circle");
				case EPointStyle::Triangle: return Append(out, "Triangle");
				case EPointStyle::Star:     return Append(out, "Star");
				case EPointStyle::Annulus:  return Append(out, "Annulus");
				default: throw std::runtime_error("Unknown arrow type");
			}
		}
		template <typename TOut> static void Append(TOut& out, VariableInt var_int)
		{
			Append(out, var_int.m_value);
		}
		template <typename TOut> static void Append(TOut& out, StringWithLength str)
		{
			Append(out, str.m_value);
		}
		template <typename TOut> static void Append(TOut& out, Name n)
		{
			if (n.m_name.empty()) return;
			Append(out, std::string_view{ n.m_name });
		}
		template <typename TOut> static void Append(TOut& out, Col c)
		{
			if (c.m_ui == 0xFFFFFFFF) return;
			Append(out, To<string32>(c.m_col));
		}
		template <typename TOut> static void Append(TOut& out, Size s)
		{
			if (s.m_size == 0) return;
			Write(out, EKeyword::Size, s.m_size);
		}
		template <typename TOut> static void Append(TOut& out, Size2 s)
		{
			if (s.m_size == v2::Zero()) return;
			Write(out, EKeyword::Size, s.m_size);
		}
		template <typename TOut> static void Append(TOut& out, Width w)
		{
			if (w.m_width == 0) return;
			Write(out, EKeyword::Width, w.m_width);
		}
		template <typename TOut> static void Append(TOut& out, Scale2 s)
		{
			if (s.m_scale == v2::One()) return;
			Write(out, EKeyword::Scale, s.m_scale);
		}
		template <typename TOut> static void Append(TOut& out, Scale3 s)
		{
			if (s.m_scale == v3::One()) return;
			Write(out, EKeyword::Scale, s.m_scale);
		}
		template <typename TOut> static void Append(TOut& out, PerItemColour c)
		{
			if (!c.m_per_item_colour) return;
			Write(out, EKeyword::PerItemColour);
		}
		template <typename TOut> static void Append(TOut& out, Depth d)
		{
			if (d.m_depth == false) return;
			Write(out, EKeyword::Depth);
		}
		template <typename TOut> static void Append(TOut& out, Wireframe w)
		{
			if (!w.m_wire) return;
			Write(out, EKeyword::Wireframe);
		}
		template <typename TOut> static void Append(TOut& out, Solid s)
		{
			if (!s.m_solid) return;
			Write(out, EKeyword::Solid);
		}
		template <typename TOut> static void Append(TOut& out, Alpha a)
		{
			if (!a.m_has_alpha) return;
			Write(out, EKeyword::Alpha);
		}
		template <typename TOut> static void Append(TOut& out, AxisId id)
		{
			Write(out, EKeyword::AxisId, static_cast<int>(id));
		}
		template <typename TOut> static void Append(TOut& out, Pos p)
		{
			if (p.m_pos == v4::Origin()) return;
			Write(out, EKeyword::O2W, [&]
			{
				Write(out, EKeyword::Pos, p.m_pos.xyz);
			});
		}
		template <typename TOut> static void Append(TOut& out, O2W o2w)
		{
			if (o2w.m_mat == m4x4::Identity())
				return;

			if (o2w.m_mat.rot == m3x4::Identity() && o2w.m_mat.pos.w == 1)
				return Write(out, EKeyword::O2W, [&]
					{
						Write(out, EKeyword::Pos, o2w.m_mat.pos.xyz);
					});

			Write(out, EKeyword::O2W, [&]
			{
				if (!IsAffine(o2w.m_mat)) Write(out, EKeyword::NonAffine);
				Write(out, EKeyword::M4x4, o2w.m_mat);
			});
		}
		template <typename TOut, typename... Args> static void Append(TOut& out, Args&&... args)
		{
			(Append(out, std::forward<Args>(args)), ...);
		}

		// Write custom data within a section
		template <typename TOut, std::invocable AddBodyFn>
		static void Write(TOut& out, EKeyword keyword, Name name, Col colour, AddBodyFn body_cb)
		{
			// Keyword
			traits<TOut>::append(out, "*");
			traits<TOut>::append(out, EKeyword_::ToStringA(keyword));

			// Optional name/colour
			Append(out, name);
			Append(out, colour);

			// Section start
			traits<TOut>::append(out, " {");

			// Write the section body
			body_cb();

			// Section end
			traits<TOut>::append(out, "}");
		}
		template <typename TOut, std::invocable AddBodyFn>
		static void Write(TOut& out, EKeyword keyword, AddBodyFn body_cb)
		{
			Write(out, keyword, {}, {}, body_cb);
		}

		// Write a single primitive type
		template <typename TOut, typename... TItem> requires (!std::is_invocable_v<TItem> && ...)
		static void Write(TOut& out, EKeyword keyword, Name name, Col colour, TItem&&... items)
		{
			return Write(out, keyword, name, colour, [&]
			{
				(Append(out, std::forward<TItem>(items)), ...);
			});
		}
		template <typename TOut, typename... TItem> requires (!std::is_invocable_v<TItem> && ...)
		static void Write(TOut& out, EKeyword keyword, TItem&&... items)
		{
			return Write(out, keyword, {}, {}, [&]
			{
				(Append(out, std::forward<TItem>(items)), ...);
			});
		}
	};

	struct TextReader : IReader
	{
		alignas(8) std::byte m_src[208];
		alignas(8) std::byte m_pp[880];
		mutable Location m_location;
		string32 m_keyword;
		wstring32 m_delim;
		int m_section_level;
		int m_nest_level;

		TextReader(std::istream& stream, std::filesystem::path src_filepath, EEncoding enc = EEncoding::utf8, ReportErrorCB report_error_cb = nullptr, ParseProgressCB progress_cb = nullptr, IPathResolver const& resolver = NoIncludes::Instance());
		TextReader(std::wistream& stream, std::filesystem::path src_filepath, EEncoding enc = EEncoding::utf16_le, ReportErrorCB report_error_cb = nullptr, ParseProgressCB progress_cb = nullptr, IPathResolver const& resolver = NoIncludes::Instance());
		TextReader(TextReader&&) = delete;
		TextReader(TextReader const&) = delete;
		TextReader& operator=(TextReader&&) = delete;
		TextReader& operator=(TextReader const&) = delete;
		~TextReader();

		// Return the current location in the source
		virtual Location const& Loc() const override;

		// Move into a nested section
		virtual void PushSection() override;

		// Leave the current nested section
		virtual void PopSection() override;

		// True when the current position has reached the end of the current section
		virtual bool IsSectionEnd() override;

		// True when the source is exhausted
		virtual bool IsSourceEnd() override;

		// Get the next keyword within the current section.
		// Returns false if at the end of the section
		virtual bool NextKeywordImpl(int& kw) override;

		// Read an identifier from the current section. Leading '10xxxxxx' bytes are the length (in bytes). Default length is the full section
		virtual string32 IdentifierImpl() override;

		// Read a utf8 string from the current section. Leading '10xxxxxx' bytes are the length (in bytes). Default length is the full section
		virtual string32 StringImpl(char escape_char) override;

		// Read an integral value from the current section
		virtual int64_t IntImpl(int byte_count, int radix) override;

		// Read a floating point value from the current section
		virtual double RealImpl(int byte_count) override;

		// Read an enum value from the current section
		virtual int64_t EnumImpl(int byte_count, ParseEnumIdentCB parse) override;

		// Read a boolean value from the current section
		virtual bool BoolImpl() override;
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::rdr12::ldraw
{
	PRUnitTest(LDrawTextSerialiserTests)
	{
		std::ostringstream strm;
		TextWriter::Write(strm, EKeyword::Point, "TestPoints", 0xFF00FF00, [&]
		{
			TextWriter::Write(strm, EKeyword::Data, v3(1, 1, 1), v3(2, 2, 2), v3(3, 3, 3));
			TextWriter::Write(strm, EKeyword::Line, "TestLines", 0xFF0000FF, [&]
			{
				TextWriter::Write(strm, EKeyword::Data, v3(-1, -1, 0), v3(1, 1, 0), v3(-1, 1, 0), v3(1, -1, 0));
			});
			TextWriter::Write(strm, EKeyword::Sphere, "TestSphere", 0xFFFF0000, [&]
			{
				TextWriter::Write(strm, EKeyword::Data, 1.0f);
			});
			TextWriter::Write(strm, EKeyword::Custom, [&]
			{
				std::string_view s = "ShortString";
				TextWriter::Write(strm, EKeyword::Name, StringWithLength{ s }, StringWithLength{ s });
			});
		});

		auto data = strm.str();
		PR_EXPECT(data.size() != 0);

		#if 0
		{
			std::ofstream ofile(temp_dir / "ldraw_test.lbr");
			ofile.write(data.data(), data.size());
		}
		#endif
	}
}
#endif
