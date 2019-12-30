//***************************************************************************************************
// Ldr Object
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************

#include <string>
#include <sstream>
#include <array>
#include <unordered_set>
#include <unordered_map>
#include <optional>
#include <mutex>
#include <cwctype>
#include "pr/ldraw/ldr_object.h"
#include "pr/common/assert.h"
#include "pr/common/hash.h"
#include "pr/maths/maths.h"
#include "pr/maths/convex_hull.h"
#include "pr/view3d/renderer.h"
#include "pr/storage/csv.h"
#include "pr/str/extract.h"

using namespace pr::rdr;
using namespace pr::script;

namespace pr::ldr
{
	// Notes:
	// Handling Errors:
	//  For parsing or logical errors (e.g. negative widths, etc) use p.ReportError(EResult, msg)
	//  then return gracefully or continue with a valid value. The ReportError function may not
	//  throw in which case parsing will need to continue with sane values.

	using VCont = pr::vector<pr::v4>;
	using NCont = pr::vector<pr::v4>;
	using ICont = pr::vector<pr::uint16>;
	using CCont = pr::vector<pr::Colour32>;
	using TCont = pr::vector<pr::v2>;
	using GCont = pr::vector<pr::rdr::NuggetProps>;
	using ModelCont = ParseResult::ModelLookup;
	using EResult = pr::script::EResult;
	using Font = pr::rdr::ModelGenerator<>::Font;
	using TextFormat = pr::rdr::ModelGenerator<>::TextFormat;
	using TextLayout = pr::rdr::ModelGenerator<>::TextLayout;

	// A global pool of 'Buffers' objects
	#pragma region Buffer Pool / Cache
	struct alignas(16) Buffers
	{
		VCont m_point;
		NCont m_norms;
		ICont m_index;
		CCont m_color;
		TCont m_texts;
		GCont m_nugts;
	};
	using BuffersPtr = std::unique_ptr<Buffers>;
	std::vector<BuffersPtr> g_buffer_pool;
	std::mutex g_buffer_pool_mutex;
	BuffersPtr GetFromPool()
	{
		std::lock_guard<std::mutex> lock(g_buffer_pool_mutex);
		if (!g_buffer_pool.empty())
		{
			auto ptr = std::move(g_buffer_pool.back());
			g_buffer_pool.pop_back();
			return std::move(ptr);
		}
		return BuffersPtr(new Buffers()); // using make_unique causes a crash in x64 release here
	}
	void ReturnToPool(BuffersPtr& bptr)
	{
		std::lock_guard<std::mutex> lock(g_buffer_pool_mutex);
		g_buffer_pool.push_back(std::move(bptr));
	}

	// Cached buffers for geometry
	struct Cache
	{
		BuffersPtr m_bptr;
		VCont& m_point;
		NCont& m_norms;
		ICont& m_index;
		CCont& m_color;
		TCont& m_texts;
		GCont& m_nugts;
			
		Cache()
			:m_bptr(GetFromPool())
			,m_point(m_bptr->m_point)
			,m_norms(m_bptr->m_norms)
			,m_index(m_bptr->m_index)
			,m_color(m_bptr->m_color)
			,m_texts(m_bptr->m_texts)
			,m_nugts(m_bptr->m_nugts)
		{}
		~Cache()
		{
			Reset();
			ReturnToPool(m_bptr);
		}
		Cache(Cache const& rhs) = delete;
		Cache operator =(Cache const& rhs) = delete;
	
		// Resize all buffers to zero
		void Reset()
		{
			m_point.resize(0);
			m_norms.resize(0);
			m_index.resize(0);
			m_color.resize(0);
			m_texts.resize(0);
			m_nugts.resize(0);
		}
	};
	#pragma endregion

	// Helper object for passing parameters between parsing functions
	struct ParseParams
	{
		using system_clock = std::chrono::system_clock;
		using time_point   = std::chrono::time_point<system_clock>;
		using FontStack    = pr::vector<Font>;

		Renderer&       m_rdr;
		Reader&         m_reader;
		ParseResult&    m_result;
		ObjectCont&     m_objects;
		ModelCont&      m_models;
		pr::Guid        m_context_id;
		Cache           m_cache;
		HashValue       m_keyword;
		LdrObject*      m_parent;
		FontStack       m_font;
		ParseProgressCB m_progress_cb;
		time_point      m_last_progress_update;
		bool&           m_cancel;

		ParseParams(Renderer& rdr, Reader& reader, ParseResult& result, pr::Guid const& context_id, ParseProgressCB progress_cb, bool& cancel)
			:m_rdr(rdr)
			,m_reader(reader)
			,m_result(result)
			,m_objects(result.m_objects)
			,m_models(result.m_models)
			,m_context_id(context_id)
			,m_cache()
			,m_keyword()
			,m_parent()
			,m_font(1)
			,m_progress_cb(progress_cb)
			,m_last_progress_update(system_clock::now())
			,m_cancel(cancel)
		{}
		ParseParams(ParseParams& p, ObjectCont& objects, HashValue keyword, LdrObject* parent)
			:m_rdr(p.m_rdr)
			,m_reader(p.m_reader)
			,m_result(p.m_result)
			,m_objects(objects)
			,m_models(p.m_models)
			,m_context_id(p.m_context_id)
			,m_cache()
			,m_keyword(keyword)
			,m_parent(parent)
			,m_font(p.m_font)
			,m_progress_cb(p.m_progress_cb)
			,m_last_progress_update(p.m_last_progress_update)
			,m_cancel(p.m_cancel)
		{}
		ParseParams(ParseParams const&) = delete;
		ParseParams& operator = (ParseParams const&) = delete;

		// Report an error in the script
		void ReportError(EResult result, std::string const& msg = {})
		{
			m_reader.ReportError(result, m_reader.Location(), msg);
		}

		// Give a progress update
		void ReportProgress()
		{
			using namespace std::chrono;

			// Callback provided?
			if (m_progress_cb == nullptr)
				return;

			// Limit callbacks to once every X seconds.
			if (system_clock::now() - m_last_progress_update < milliseconds(200))
				return;

			// Call the callback with the freshly minted object.
			// If the callback returns false, abort parsing.
			m_cancel = !m_progress_cb(m_context_id, m_result, m_reader.Location(), false);
			m_last_progress_update = system_clock::now();
		}
	};

	// Forward declare the recursive object parsing function
	bool ParseLdrObject(ParseParams& p);

	#pragma region Parse Common Elements

	// Read the name, colour, and instance flag for an object
	ObjectAttributes ParseAttributes(ParseParams& p, ELdrObject model_type)
	{
		ObjectAttributes attr;
		attr.m_type = model_type;
		attr.m_name = "";
			
		// Read the next tokens up to the section start
		wstring32 tok0, tok1; auto count = 0;
		if (!p.m_reader.IsSectionStart()) { p.m_reader.Token(tok0, L"{}"); ++count; }
		if (!p.m_reader.IsSectionStart()) { p.m_reader.Token(tok1, L"{}"); ++count; }
		if (!p.m_reader.IsSectionStart())
			p.ReportError(EResult::UnknownToken, "object attributes are invalid");

		switch (count)
		{
		case 2:
			{
				// Expect: *Type <name> <colour>
				if (!str::ExtractIdentifierC(attr.m_name, std::begin(tok0)))
					p.ReportError(EResult::TokenNotFound, "object name is invalid");
				if (!str::ExtractIntC(attr.m_colour.argb, 16, std::begin(tok1)))
					p.ReportError(EResult::TokenNotFound, "object colour is invalid");
				break;
			}
		case 1:
			{
				// Expect: *Type <name>  or *Type <colour>
				// If the first token is 8 hex digits, assume it is a colour, otherwise assume it is a name
				if (tok0.size() == 8 && pr::all(tok0, std::iswxdigit))
				{
					attr.m_name = "";
					if (!str::ExtractIntC(attr.m_colour.argb, 16, std::begin(tok0)))
						p.ReportError(EResult::TokenNotFound, "object colour is invalid");
				}
				else
				{
					attr.m_colour = 0xFFFFFFFF;
					if (!str::ExtractIdentifierC(attr.m_name, std::begin(tok0)))
						p.ReportError(EResult::TokenNotFound, "object name is invalid");
				}
				break;
			}
		case 0:
			{
				attr.m_name = Enum<ELdrObject>::ToStringA(model_type);
				attr.m_colour = 0xFFFFFFFF;
				break;
			}
		}
		return attr;
	}

	// Parse a camera description
	void ParseCamera(ParseParams& p, ParseResult& out)
	{
		p.m_reader.SectionStart();
		for (EKeyword kw; p.m_reader.NextKeywordH(kw);)
		{
			switch (kw)
			{
			default:
				{
					p.ReportError(EResult::UnknownToken, Fmt("Keyword '%S' is not valid within *Camera", p.m_reader.LastKeyword().c_str()));
					break;
				}
			case EKeyword::O2W:
				{
					auto c2w = pr::m4x4Identity;
					p.m_reader.TransformS(c2w);
					out.m_cam.CameraToWorld(c2w);
					out.m_cam_fields |= ECamField::C2W;
					break;
				}
			case EKeyword::LookAt:
				{
					pr::v4 lookat;
					p.m_reader.Vector3S(lookat, 1.0f);
					pr::m4x4 c2w = out.m_cam.CameraToWorld();
					out.m_cam.LookAt(c2w.pos, lookat, c2w.y);
					out.m_cam_fields |= ECamField::C2W;
					out.m_cam_fields |= ECamField::Focus;
					break;
				}
			case EKeyword::Align:
				{
					pr::v4 align;
					p.m_reader.Vector3S(align, 0.0f);
					out.m_cam.Align(align);
					out.m_cam_fields |= ECamField::Align;
					break;
				}
			case EKeyword::Aspect:
				{
					float aspect;
					p.m_reader.RealS(aspect);
					out.m_cam.Aspect(aspect);
					out.m_cam_fields |= ECamField::Align;
					break;
				}
			case EKeyword::FovX:
				{
					float fovX;
					p.m_reader.RealS(fovX);
					out.m_cam.FovX(fovX);
					out.m_cam_fields |= ECamField::FovY;
					break;
				}
			case EKeyword::FovY:
				{
					float fovY;
					p.m_reader.RealS(fovY);
					out.m_cam.FovY(fovY);
					out.m_cam_fields |= ECamField::FovY;
					break;
				}
			case EKeyword::Fov:
				{
					float fov[2];
					p.m_reader.RealS(fov, 2);
					out.m_cam.Fov(fov[0], fov[1]);
					out.m_cam_fields |= ECamField::Aspect;
					out.m_cam_fields |= ECamField::FovY;
					break;
				}
			case EKeyword::Near:
				{
					p.m_reader.Real(out.m_cam.m_near);
					out.m_cam_fields |= ECamField::Near;
					break;
				}
			case EKeyword::Far:
				{
					p.m_reader.Real(out.m_cam.m_far);
					out.m_cam_fields |= ECamField::Far;
					break;
				}
			case EKeyword::Orthographic:
				{
					out.m_cam.m_orthographic = true;
					out.m_cam_fields |= ECamField::Ortho;
					break;
				}
			}
		}
		p.m_reader.SectionEnd();
	}

	// Parse a font description
	void ParseFont(ParseParams& p, Font& font)
	{
		auto& reader = p.m_reader;

		reader.SectionStart();
		font.m_underline = false;
		font.m_strikeout = false;
		for (EKeyword kw; reader.NextKeywordH(kw);)
		{
			switch (kw)
			{
			default:
				{
					p.ReportError(EResult::UnknownToken, Fmt("Keyword '%S' is not valid within *Font", reader.LastKeyword().c_str()));
					break;
				}
			case EKeyword::Name:
				{
					reader.StringS(font.m_name);
					break;
				}
			case EKeyword::Size:
				{
					reader.RealS(font.m_size);
					break;
				}
			case EKeyword::Colour:
				{
					reader.IntS(font.m_colour.argb, 16);
					break;
				}
			case EKeyword::Weight:
				{
					reader.IntS(font.m_weight, 10);
					break;
				}
			case EKeyword::Style:
				{
					string32 ident;
					reader.IdentifierS(ident);
					if (str::EqualI(ident, "normal" )) font.m_style = DWRITE_FONT_STYLE_NORMAL;
					if (str::EqualI(ident, "italic" )) font.m_style = DWRITE_FONT_STYLE_ITALIC;
					if (str::EqualI(ident, "oblique")) font.m_style = DWRITE_FONT_STYLE_OBLIQUE;
					break;
				}
			case EKeyword::Stretch:
				{
					reader.IntS(font.m_stretch, 10);
					break;
				}
			case EKeyword::Underline:
				{
					font.m_underline = true;
					break;
				}
			case EKeyword::Strikeout:
				{
					font.m_strikeout = true;
					break;
				}
			}
		}
		reader.SectionEnd();
	}

	// Parse a simple animation description
	void ParseAnimation(ParseParams& p, Animation& anim)
	{
		auto& reader = p.m_reader;

		reader.SectionStart();
		for (EKeyword kw; reader.NextKeywordH(kw);)
		{
			switch (kw)
			{
			default:
				{
					p.ReportError(EResult::UnknownToken, Fmt("Keyword '%S' is not valid within *Animation", reader.LastKeyword().c_str()));
					break;
				}
			case EKeyword::Style:
				{
					char style[50];
					reader.Identifier(style);
					if      (str::EqualI(style, "NoAnimation"   )) anim.m_style = EAnimStyle::NoAnimation;
					else if (str::EqualI(style, "PlayOnce"      )) anim.m_style = EAnimStyle::PlayOnce;
					else if (str::EqualI(style, "PlayReverse"   )) anim.m_style = EAnimStyle::PlayReverse;
					else if (str::EqualI(style, "PingPong"      )) anim.m_style = EAnimStyle::PingPong;
					else if (str::EqualI(style, "PlayContinuous")) anim.m_style = EAnimStyle::PlayContinuous;
					break;
				}
			case EKeyword::Period:
				{
					reader.RealS(anim.m_period);
					break;
				}
			case EKeyword::Velocity:
				{
					reader.Vector3S(anim.m_velocity, 0.0f);
					break;
				}
			case EKeyword::AngVelocity:
				{
					reader.Vector3S(anim.m_ang_velocity, 0.0f);
					break;
				}
			}
		}
		reader.SectionEnd();
	}

	// Parse a texture description. Returns a pointer to the Texture created in the renderer.
	bool ParseTexture(ParseParams& p, Texture2DPtr& tex)
	{
		auto& reader = p.m_reader;

		std::wstring tex_resource;
		auto t2s = pr::m4x4Identity;
		bool has_alpha = false;
		SamplerDesc sam;

		reader.SectionStart();
		while (!reader.IsSectionEnd())
		{
			if (reader.IsKeyword())
			{
				auto kw = reader.NextKeywordH<EKeyword>();
				switch (kw)
				{
				default:
					{
						p.ReportError(EResult::UnknownToken, Fmt("Keyword '%S' is not valid within *Texture", reader.LastKeyword().c_str()));
						break;
					}
				case EKeyword::O2W:
					{
						reader.TransformS(t2s);
						break;
					}
				case EKeyword::Addr:
					{
						char word[20];
						reader.SectionStart();
						reader.Identifier(word); sam.AddressU = (D3D11_TEXTURE_ADDRESS_MODE)Enum<ETexAddrMode>::Parse(word, false);
						reader.Identifier(word); sam.AddressV = (D3D11_TEXTURE_ADDRESS_MODE)Enum<ETexAddrMode>::Parse(word, false);
						reader.SectionEnd();
						break;
					}
				case EKeyword::Filter:
					{
						char word[20];
						reader.SectionStart();
						reader.Identifier(word); sam.Filter = (D3D11_FILTER)Enum<EFilter>::Parse(word, false);
						reader.SectionEnd();
						break;
					}
				case EKeyword::Alpha:
					{
						has_alpha = true;
						break;
					}
				}
			}
			else
			{
				reader.String(tex_resource);
			}
		}
		reader.SectionEnd();

		// Silently ignore missing texture files
		if (!tex_resource.empty())
		{
			// Create the texture
			try
			{
				tex = p.m_rdr.m_tex_mgr.CreateTexture2D(AutoId, tex_resource.c_str(), sam, has_alpha, nullptr);
				tex->m_t2s = t2s;
			}
			catch (std::exception const& e)
			{
				p.ReportError(EResult::ValueNotFound, FmtS("Failed to create texture %s\n%s", tex_resource.c_str(), e.what()));
			}
		}
		return true;
	}

	// Parse a video texture
	bool ParseVideo(ParseParams& p, Texture2DPtr& vid)
	{
		auto& reader = p.m_reader;

		std::string filepath;
		reader.SectionStart();
		reader.String(filepath);
		if (!filepath.empty())
		{
			(void)vid;
			//todo
			//' // Load the video texture
			//' try
			//' {
			//' 	vid = p.m_rdr.m_tex_mgr.CreateVideoTexture(AutoId, filepath.c_str());
			//' }
			//' catch (std::exception const& e)
			//' {
			//' 	p.ReportError(EResult::ValueNotFound, pr::FmtS("failed to create video %s\nReason: %s" ,filepath.c_str() ,e.what()));
			//' }
		}
		reader.SectionEnd();
		return true;
	}

	// Parse keywords that can appear in any section. Returns true if the keyword was recognised.
	bool ParseProperties(ParseParams& p, EKeyword kw, LdrObject* obj)
	{
		auto& reader = p.m_reader;

		switch (kw)
		{
		default: return false;
		case EKeyword::O2W:
		case EKeyword::Txfm:
			{
				reader.TransformS(obj->m_o2p);
				return true;
			}
		case EKeyword::Colour:
			{
				reader.IntS(obj->m_base_colour.argb, 16);
				return true;
			}
		case EKeyword::ColourMask:
			{
				reader.IntS(obj->m_colour_mask, 16);
				return true;
			}
		case EKeyword::Reflectivity:
			{
				reader.RealS(obj->m_env);
				return true;
			};
		case EKeyword::RandColour:
			{
				obj->m_base_colour = pr::RandomRGB(g_rng());
				return true;
			}
		case EKeyword::Animation:
			{
				ParseAnimation(p, obj->m_anim);
				return true;
			}
		case EKeyword::Hidden:
			{
				obj->m_flags = SetBits(obj->m_flags, ELdrFlags::Hidden, true);
				return true;
			}
		case EKeyword::Wireframe:
			{
				obj->m_flags = SetBits(obj->m_flags, ELdrFlags::Wireframe, true);
				return true;
			}
		case EKeyword::NoZTest:
			{
				obj->m_flags = SetBits(obj->m_flags, ELdrFlags::NoZTest, true);
				return true;
			}
		case EKeyword::NoZWrite:
			{
				obj->m_flags = SetBits(obj->m_flags, ELdrFlags::NoZWrite, true);
				return true;
			}
		case EKeyword::ScreenSpace:
			{
				// Use a magic number to signal screen space mode to the ApplyState function
				obj->m_screen_space = Sub((multicast::IMultiCast*)1, 0);
				return true;
			}
		case EKeyword::Font:
			{
				ParseFont(p, p.m_font.back());
				return true;
			}
		}
	}

	// Apply the states such as colour,wireframe,etc to the objects renderer model
	void ApplyObjectState(LdrObject* obj)
	{
		// Set colour on 'obj' (so that render states are set correctly)
		// Note that the colour is 'blended' with 'm_base_colour' so m_base_colour * White = m_base_colour.
		obj->Colour(obj->m_base_colour, 0xFFFFFFFF);

		// Apply the colour of 'obj' to all children using a mask
		if (obj->m_colour_mask != 0)
			obj->Colour(obj->m_base_colour, obj->m_colour_mask, "");

		// If flagged as hidden, hide
		if (AllSet(obj->m_flags, ELdrFlags::Hidden))
			obj->Visible(false);

		// If flagged as wireframe, set wireframe
		if (AllSet(obj->m_flags, ELdrFlags::Wireframe))
			obj->Wireframe(true);

		// If NoZTest
		if (AllSet(obj->m_flags, ELdrFlags::NoZTest))
		{
			// Don't test against Z, and draw above all objects
			obj->m_dsb.Set(rdr::EDS::DepthEnable, FALSE);
			obj->m_sko.Group(rdr::ESortGroup::PostAlpha);
		}

		// If NoZWrite
		if (AllSet(obj->m_flags, ELdrFlags::NoZWrite))
		{
			// Don't write to Z and draw behind all objects
			obj->m_dsb.Set(rdr::EDS::DepthWriteMask, D3D11_DEPTH_WRITE_MASK_ZERO);
			obj->m_sko.Group(rdr::ESortGroup::PreOpaques);
		}

		// If flagged as screen space rendering mode
		if (obj->m_screen_space)
			obj->ScreenSpace(true);
	}

	// Get/Create an instance of the point sprites shader
	ShaderPtr PointSpriteShader(Renderer& rdr, v2 point_size, bool depth)
	{
		auto id = pr::hash::Hash("PointSprites", point_size, depth);
		auto shdr = rdr.m_shdr_mgr.GetShader<PointSpritesGS>(id, RdrId(EStockShader::PointSpritesGS));
		shdr->m_size = point_size;
		shdr->m_depth = depth;
		return shdr;
	}

	// Get or create an instance of the thick line shader for line strip geometry
	ShaderPtr ThickLineShaderLS(Renderer& rdr, float line_width)
	{
		auto id = pr::hash::Hash("ThickLineStrip", line_width);
		auto shdr = rdr.m_shdr_mgr.GetShader<ThickLineStripGS>(id, RdrId(EStockShader::ThickLineStripGS));
		shdr->m_width = line_width;
		return shdr;
	}

	// Get or create an instance of the thick line shader for line list geometry
	ShaderPtr ThickLineShaderLL(Renderer& rdr, float line_width)
	{
		auto id = pr::hash::Hash("ThickLineList", line_width);
		auto shdr = rdr.m_shdr_mgr.GetShader<ThickLineListGS>(id, RdrId(EStockShader::ThickLineListGS));
		shdr->m_width = line_width;
		return shdr;
	}

	// Get or create an instance of the arrow head shader
	ShaderPtr ArrowHeadShader(Renderer& rdr, float line_width)
	{
		auto id = pr::hash::Hash("ArrowHead", line_width);
		auto shdr = rdr.m_shdr_mgr.GetShader<ArrowHeadGS>(id, RdrId(EStockShader::ArrowHeadGS));
		shdr->m_size = line_width;
		return shdr;
	}

	// Convert a line strip into a line list of "dash" line segments
	void DashLineStrip(VCont const& in, VCont& out, v2 const& dash)
	{
		assert(int(in.size()) >= 2);

		// Turn the sequence of line segments into a single dashed line
		auto t = 0.0f;
		for (int i = 1, iend = int(in.size()); i != iend; ++i)
		{
			auto d = in[i] - in[i-1];
			auto len = Length3(d);

			// Emit dashes over the length of the line segment
			for (;t < len; t += dash.x + dash.y)
			{
				out.push_back(in[i-1] + d * Clamp(t         , 0.0f, len) / len);
				out.push_back(in[i-1] + d * Clamp(t + dash.x, 0.0f, len) / len);
			}
			t -= len + dash.x + dash.y;
		}
	}

	// Convert a line list into a list of "dash" line segments
	void DashLineList(VCont const& in, VCont& out, v2 const& dash)
	{
		assert(int(in.size()) >= 2 && int(in.size() & 1) == 0);

		// Turn the line list 'in' into dashed lines
		// Turn the sequence of line segments into a single dashed line
		for (int i = 0, iend = int(in.size()); i != iend; i += 2)
		{
			auto d  = in[i+1] - in[i];
			auto len = Length3(d);

			// Emit dashes over the length of the line segment
			for (auto t = 0.0f; t < len; t += dash.x + dash.y)
			{
				out.push_back(in[i] + d * Clamp(t, 0.0f, len) / len);
				out.push_back(in[i] + d * Clamp(t + dash.x, 0.0f, len) / len);
			}
		}
	}

	#pragma endregion

	#pragma region Object modifiers
	
	namespace creation
	{
		// Support for objects with a texture
		struct Textured
		{
			Texture2DPtr m_texture;
			NuggetProps m_local_mat;

			Textured()
				:m_texture()
				,m_local_mat()
			{}
			bool ParseKeyword(ParseParams& p, EKeyword kw)
			{
				switch (kw)
				{
				default:
					{
						return false;
					}
				case EKeyword::Texture:
					{
						ParseTexture(p, m_texture);
						return true;
					}
				case EKeyword::Video:
					{
						ParseVideo(p, m_texture);
						return true;
					}
				}
			}
			NuggetProps* Material()
			{
				// This function is used to pass texture/shader data to the model generated.
				// Topo and Geom are not used, each model type knows what topo and geom it's using.
				m_local_mat.m_tex_diffuse = m_texture;
				//if (m_texture->m_video)
				//	m_texture->m_video->Play(true);
				return &m_local_mat;
			}
		};

		// Support for objects with a main axis
		struct MainAxis
		{
			m4x4 m_o2w;
			AxisId m_main_axis; // The natural main axis of the object
			AxisId m_align;     // The axis we want the main axis to be aligned to

			MainAxis(AxisId main_axis = AxisId::PosZ, AxisId align = AxisId::PosZ)
				:m_o2w(m4x4Identity)
				,m_main_axis(main_axis)
				,m_align(align)
			{}
			bool ParseKeyword(ParseParams& p, EKeyword kw)
			{
				switch (kw)
				{
				default:
					{
						return false;
					}
				case EKeyword::AxisId:
					{
						p.m_reader.IntS(m_align.value, 10);
						if (AxisId::IsValid(m_align))
						{
							m_o2w = m4x4::Transform(m_main_axis, m_align, v4Origin);
							return true;
						}

						p.ReportError(EResult::InvalidValue, "AxisId must be +/- 1, 2, or 3 (corresponding to the positive or negative X, Y, or Z axis)");
						return false;
					}
				}
			}

			// True if the main axis is not equal to the desired align axis
			bool RotationNeeded() const
			{
				return m_main_axis != m_align;
			}

			// Returns the rotation from 'main_axis' to 'axis'
			m4x4 const& O2W() const
			{
				return m_o2w;
			}

			// Returns a pointer to a rotation from 'main_axis' to 'axis'. Returns null if identity
			m4x4 const* O2WPtr() const
			{
				return RotationNeeded() ? &m_o2w : nullptr;
			}
		};

		// Support for light sources that cast
		struct CastingLight
		{
			CastingLight()
			{}
			bool ParseKeyword(ParseParams& p, Light& light, EKeyword kw)
			{
				switch (kw)
				{
				default:
					{
						return false;
					}
				case EKeyword::Range:
					{
						p.m_reader.SectionStart();
						p.m_reader.Real(light.m_range);
						p.m_reader.Real(light.m_falloff);
						p.m_reader.SectionEnd();
						return true;
					}
				case EKeyword::Specular:
					{
						p.m_reader.SectionStart();
						p.m_reader.Int(light.m_specular.argb, 16);
						p.m_reader.Real(light.m_specular_power);
						p.m_reader.SectionEnd();
						return true;
					}
				case EKeyword::CastShadow:
					{
						p.m_reader.RealS(light.m_cast_shadow);
						return true;
					}
				}
			}
		};

		// Support for point sprites
		struct PointSprite
		{
			enum class EStyle
			{
				Square,
				Circle,
				Triangle,
				Star,
				Annulus,
			};

			v2     m_point_size;
			EStyle m_style;
			bool   m_depth;

			PointSprite()
				:m_point_size()
				,m_style(EStyle::Square)
				,m_depth(false)
			{}
			bool ParseKeyword(ParseParams& p, EKeyword kw)
			{
				switch (kw)
				{
				default:
					{
						return false;
					}
				case EKeyword::Size:
					{
						// Allow one or two dimensions
						p.m_reader.SectionStart();
						p.m_reader.Real(m_point_size.x);
						if (!p.m_reader.IsSectionEnd())
							p.m_reader.Real(m_point_size.y);
						else
							m_point_size.y = m_point_size.x;
						p.m_reader.SectionEnd();
						return true;
					}
				case EKeyword::Style:
					{
						string32 ident;
						p.m_reader.IdentifierS(ident);
						switch (HashI(ident.c_str()))
						{
						default: p.ReportError(EResult::UnknownToken, Fmt("'%s' is not a valid point sprite style", ident.c_str())); break;
						case HashI("square"):   m_style = EStyle::Square; break;
						case HashI("circle"):   m_style = EStyle::Circle; break;
						case HashI("triangle"): m_style = EStyle::Triangle; break;
						case HashI("star"):     m_style = EStyle::Star; break;
						case HashI("annulus"):  m_style = EStyle::Annulus; break;
						}
						return true;
					}
				case EKeyword::Depth:
					{
						m_depth = true;
						return true;
					}
				}
			}
			Texture2DPtr PointStyleTexture(ParseParams& p)
			{
				EStyle style = m_style;
				iv2 size = To<iv2>(m_point_size);
				iv2 sz(PowerOfTwoGreaterThan(size.x), PowerOfTwoGreaterThan(size.y));
				switch (style)
				{
				default: throw std::exception("Unknown point style");
				case EStyle::Square:
					{
						// No texture needed for square style
						return nullptr;
					}
				case EStyle::Circle:
					{
						auto id = pr::hash::Hash("PointStyleCircle", sz);
						return p.m_rdr.m_tex_mgr.GetTexture<Texture2D>(id, [&]
						{
							auto w0 = sz.x * 0.5f;
							auto h0 = sz.y * 0.5f;
							return CreatePointStyleTexture(p, id, sz, "PointStyleCircle", [=](auto dc, auto fr, auto) { dc->FillEllipse({ { w0, h0 }, w0, h0 }, fr); });
						});
					}
				case EStyle::Triangle:
					{
						auto id = pr::hash::Hash("PointStyleTriangle", sz);
						return p.m_rdr.m_tex_mgr.GetTexture<Texture2D>(id, [&]
						{
							Renderer::Lock lk(p.m_rdr);
							D3DPtr<ID2D1PathGeometry> geom;
							D3DPtr<ID2D1GeometrySink> sink;
							pr::Throw(lk.D2DFactory()->CreatePathGeometry(&geom.m_ptr));
							pr::Throw(geom->Open(&sink.m_ptr));

							auto w0 = 1.0f * sz.x;
							auto h0 = 0.5f * sz.y * (float)tan(pr::DegreesToRadians(60.0f));
							auto h1 = 0.5f * (sz.y - h0);

							sink->BeginFigure({ w0, h1 }, D2D1_FIGURE_BEGIN_FILLED);
							sink->AddLine({ 0.0f * w0, h1 });
							sink->AddLine({ 0.5f * w0, h0 + h1 });
							sink->EndFigure(D2D1_FIGURE_END_CLOSED);
							pr::Throw(sink->Close());

							return CreatePointStyleTexture(p, id, sz, "PointStyleTriangle", [=](auto dc, auto fr, auto) { dc->FillGeometry(geom.get(), fr, nullptr); });
						});
					}
				case EStyle::Star:
					{
						auto id = pr::hash::Hash("PointStyleStar", sz);
						return p.m_rdr.m_tex_mgr.GetTexture<Texture2D>(id, [&]
						{
							Renderer::Lock lk(p.m_rdr);
							D3DPtr<ID2D1PathGeometry> geom;
							D3DPtr<ID2D1GeometrySink> sink;
							pr::Throw(lk.D2DFactory()->CreatePathGeometry(&geom.m_ptr));
							pr::Throw(geom->Open(&sink.m_ptr));

							auto w0 = 1.0f * sz.x;
							auto h0 = 1.0f * sz.y;

							sink->BeginFigure({ 0.5f * w0, 0.0f * h0 }, D2D1_FIGURE_BEGIN_FILLED);
							sink->AddLine({ 0.4f * w0, 0.4f * h0 });
							sink->AddLine({ 0.0f * w0, 0.5f * h0 });
							sink->AddLine({ 0.4f * w0, 0.6f * h0 });
							sink->AddLine({ 0.5f * w0, 1.0f * h0 });
							sink->AddLine({ 0.6f * w0, 0.6f * h0 });
							sink->AddLine({ 1.0f * w0, 0.5f * h0 });
							sink->AddLine({ 0.6f * w0, 0.4f * h0 });
							sink->EndFigure(D2D1_FIGURE_END_CLOSED);
							pr::Throw(sink->Close());

							return CreatePointStyleTexture(p, id, sz, "PointStyleStar", [=](auto dc, auto fr, auto) { dc->FillGeometry(geom.get(), fr, nullptr); });
						});
					}
				case EStyle::Annulus:
					{
						auto id = pr::hash::Hash("PointStyleAnnulus", sz);
						return p.m_rdr.m_tex_mgr.GetTexture<Texture2D>(id, [&]
						{
							auto w0 = sz.x * 0.5f;
							auto h0 = sz.y * 0.5f;
							auto w1 = sz.x * 0.4f;
							auto h1 = sz.y * 0.4f;
							return CreatePointStyleTexture(p, id, sz, "PointStyleAnnulus", [=](auto dc, auto fr, auto bk)
							{
								dc->SetPrimitiveBlend(D2D1_PRIMITIVE_BLEND_COPY);
								dc->FillEllipse({ { w0, h0 }, w0, h0 }, fr);
								dc->FillEllipse({ { w0, h0 }, w1, h1 }, bk);
							});
						});
					}
				}
			}
			template <typename TDrawOnIt>
			Texture2DPtr CreatePointStyleTexture(ParseParams& p, RdrId id, iv2 const& sz, char const* name, TDrawOnIt draw)
			{
				// Create a texture large enough to contain the text, and render the text into it
				SamplerDesc sdesc(D3D11_TEXTURE_ADDRESS_CLAMP, D3D11_FILTER_MIN_MAG_MIP_POINT);
				Texture2DDesc tdesc(size_t(sz.x), size_t(sz.y), 1, DXGI_FORMAT_R8G8B8A8_UNORM);
				tdesc.BindFlags = D3D11_BIND_RENDER_TARGET | D3D11_BIND_SHADER_RESOURCE;
				auto tex = p.m_rdr.m_tex_mgr.CreateTexture2D(id, Image(), tdesc, sdesc, false, name);

				// Get a D2D device context to draw on
				auto dc = tex->GetD2DeviceContext();

				// Create the brushes
				D3DPtr<ID2D1SolidColorBrush> fr_brush;
				D3DPtr<ID2D1SolidColorBrush> bk_brush;
				auto fr = pr::To<D3DCOLORVALUE>(0xFFFFFFFF);
				auto bk = pr::To<D3DCOLORVALUE>(0x00000000);
				pr::Throw(dc->CreateSolidColorBrush(fr, &fr_brush.m_ptr));
				pr::Throw(dc->CreateSolidColorBrush(bk, &bk_brush.m_ptr));

				// Draw the spot
				dc->BeginDraw();
				dc->Clear(&bk);
				draw(dc, fr_brush.get(), bk_brush.get());
				pr::Throw(dc->EndDraw());
				return tex;
			}
		};

		// Support for generate normals
		struct GenNorms
		{
			float m_smoothing_angle;

			GenNorms(float gen_normals = -1.0f)
				:m_smoothing_angle(gen_normals)
			{}
			bool ParseKeyword(ParseParams& p, EKeyword kw)
			{
				switch (kw)
				{
				default:
					{
						return false;
					}
				case EKeyword::GenerateNormals:
					{
						p.m_reader.RealS(m_smoothing_angle);
						m_smoothing_angle = pr::DegreesToRadians(m_smoothing_angle);
						return true;
					}
				}
			}

			// Generate normals if needed
			void Generate(ParseParams& p)
			{
				if (m_smoothing_angle < 0.0f)
					return;

				auto& verts   = p.m_cache.m_point;
				auto& indices = p.m_cache.m_index;
				auto& normals = p.m_cache.m_norms;
				auto& nuggets = p.m_cache.m_nugts;

				// Can't generate normals per nugget because nuggets may share vertices.
				// Generate normals for all vertices (verts used by lines only with have zero-normals)
				normals.resize(verts.size());

				// Generate normals for the nuggets containing faces
				for (auto& nug : nuggets)
				{
					// Not face topology...
					if (nug.m_topo != EPrim::TriList)
						continue;

					// The number of indices in this nugget
					auto iptr = indices.data();
					auto icount = indices.size();
					if (!nug.m_irange.empty())
					{
						iptr += nug.m_irange.begin();
						icount = nug.m_irange.size();
					}

					// Not sure if this works... needs testing
					pr::geometry::GenerateNormals(icount, iptr, m_smoothing_angle,
						[&](pr::uint16 i)
						{
							return verts[i];
						}, 0,
						[&](pr::uint16 new_idx, pr::uint16 orig_idx, v4 const& norm)
						{
							if (new_idx >= verts.size())
							{
								verts.resize(new_idx + 1, verts[orig_idx]);
								normals.resize(new_idx + 1, normals[orig_idx]);
							}
							normals[new_idx] = norm;
						},
						[&](pr::uint16 i0, pr::uint16 i1, pr::uint16 i2)
						{
							*iptr++ = i0;
							*iptr++ = i1;
							*iptr++ = i2;
						});

					// Geometry has normals now
					nug.m_geom |= EGeom::Norm;
				}
			}
		};
	}

	#pragma endregion

	#pragma region ObjectCreator

	// Template prototype for ObjectCreators
	template <ELdrObject ObjType> struct ObjectCreator;

	// Base class for all object creators
	struct IObjectCreator
	{
		ParseParams& p;
		VCont& m_verts;
		ICont& m_indices;
		NCont& m_normals;
		CCont& m_colours;
		TCont& m_texs;
		GCont& m_nuggets;

		virtual ~IObjectCreator() {}
		IObjectCreator(ParseParams& p_)
			:p(p_)
			,m_verts  (p.m_cache.m_point)
			,m_indices(p.m_cache.m_index)
			,m_normals(p.m_cache.m_norms)
			,m_colours(p.m_cache.m_color)
			,m_texs   (p.m_cache.m_texts)
			,m_nuggets(p.m_cache.m_nugts)
		{}
		virtual bool ParseKeyword(EKeyword) { return false; }
		virtual void Parse() { p.ReportError(EResult::UnknownToken, Fmt("Unknown token near '%S'", p.m_reader.LastKeyword().c_str())); }
		virtual void CreateModel(LdrObject*) {}
	};

	#pragma endregion

	#pragma region Sprite Objects

	// ELdrObject::Point
	template <> struct ObjectCreator<ELdrObject::Point> :IObjectCreator
	{
		creation::Textured m_tex;
		creation::PointSprite m_sprite;
		std::optional<bool> m_per_point_colour;

		ObjectCreator(ParseParams& p)
			:IObjectCreator(p)
			,m_tex()
			,m_sprite()
			,m_per_point_colour()
		{}
		bool ParseKeyword(EKeyword kw) override
		{
			return
				m_tex.ParseKeyword(p, kw) ||
				m_sprite.ParseKeyword(p, kw) ||
				IObjectCreator::ParseKeyword(kw);
		}
		void Parse() override
		{
			pr::v4 pt;
			p.m_reader.Vector3(pt, 1.0f);
			m_verts.push_back(pt);

			// Look for the optional colour
			if (!m_per_point_colour)
				m_per_point_colour = p.m_reader.IsMatch(8, std::wregex(L"[0-9a-fA-F]{8}"));

			if (*m_per_point_colour)
			{
				pr::Colour32 col;
				p.m_reader.Int(col.argb, 16);
				m_colours.push_back(col);
			}
		}
		void CreateModel(LdrObject* obj) override
		{
			// Validate
			if (m_verts.size() < 1)
			{
				p.ReportError(EResult::Failed, FmtS("Point object '%s' description incomplete", obj->TypeAndName().c_str()));
				return;
			}

			// Create the model
			obj->m_model = ModelGenerator<>::Points(p.m_rdr, int(m_verts.size()), m_verts.data(), int(m_colours.size()), m_colours.data(), m_tex.Material());
			obj->m_model->m_name = obj->TypeAndName();

			// Use a geometry shader to draw points
			if (m_sprite.m_point_size != v2Zero)
			{
				// Get/Create an instance of the point sprites shader
				auto shdr = PointSpriteShader(p.m_rdr, m_sprite.m_point_size, m_sprite.m_depth);

				// Get/Create the point style texture
				auto tex = m_sprite.PointStyleTexture(p);

				// Update the nuggets
				for (auto& nug : obj->m_model->m_nuggets)
				{
					nug.m_tex_diffuse = tex;
					nug.m_smap[ERenderStep::ForwardRender].m_gs = shdr;
				}
			}
		}
	};

	#pragma endregion

	#pragma region Line Objects

	// ELdrObject::Line
	template <> struct ObjectCreator<ELdrObject::Line> :IObjectCreator
	{
		v2 m_dashed;
		float m_line_width;
		std::optional<bool> m_per_line_colour;

		ObjectCreator(ParseParams& p)
			:IObjectCreator(p)
			,m_dashed(v2XAxis)
			,m_line_width()
			,m_per_line_colour()
		{}
		bool ParseKeyword(EKeyword kw) override
		{
			switch (kw)
			{
			default:
				{
					return IObjectCreator::ParseKeyword(kw);
				}
			case EKeyword::Param:
				{
					float t[2];
					p.m_reader.RealS(t, 2);
					if (m_verts.size() < 2)
					{
						p.ReportError(EResult::Failed, "No preceding line to apply parametric values to");
					}
					auto& p0 = m_verts[m_verts.size() - 2];
					auto& p1 = m_verts[m_verts.size() - 1];
					auto dir = p1 - p0;
					auto pt = p0;
					p0 = pt + t[0] * dir;
					p1 = pt + t[1] * dir;
					return true;
				}
			case EKeyword::Dashed:
				{
					p.m_reader.Vector2S(m_dashed);
					return true;
				}
			case EKeyword::Width:
				{
					p.m_reader.RealS(m_line_width);
					return true;
				}
			}
		}
		void Parse() override
		{
			pr::v4 p0, p1;
			p.m_reader.Vector3(p0, 1.0f);
			p.m_reader.Vector3(p1, 1.0f);
			m_verts.push_back(p0);
			m_verts.push_back(p1);

			// Look for the optional colour
			if (!m_per_line_colour)
				m_per_line_colour = p.m_reader.IsMatch(8, std::wregex(L"[0-9a-fA-F]{8}"));

			if (*m_per_line_colour)
			{
				pr::Colour32 col;
				p.m_reader.Int(col.argb, 16);
				m_colours.push_back(col);
				m_colours.push_back(col);
			}
		}
		void CreateModel(LdrObject* obj) override
		{
			// Validate
			if (m_verts.size() < 2)
			{
				p.ReportError(EResult::Failed, FmtS("Line object '%s' description incomplete", obj->TypeAndName().c_str()));
				return;
			}

			// Convert lines to dashed lines
			if (m_dashed != v2XAxis)
			{
				// Convert each line segment to dashed lines
				VCont verts;
				std::swap(verts, m_verts);
				DashLineList(verts, m_verts, m_dashed);
			}

			// Create the model
			obj->m_model = ModelGenerator<>::Lines(p.m_rdr, int(m_verts.size() / 2), m_verts.data(), int(m_colours.size()), m_colours.data());
			obj->m_model->m_name = obj->TypeAndName();

			// Use thick lines
			if (m_line_width != 0.0f)
			{
				auto shdr = ThickLineShaderLL(p.m_rdr, m_line_width);
				for (auto& nug : obj->m_model->m_nuggets)
					nug.m_smap[ERenderStep::ForwardRender].m_gs = shdr;
			}
		}
	};

	// ELdrObject::LineD
	template <> struct ObjectCreator<ELdrObject::LineD> :IObjectCreator
	{
		v2 m_dashed;
		float m_line_width;
		std::optional<bool> m_per_line_colour;

		ObjectCreator(ParseParams& p)
			:IObjectCreator(p)
			,m_dashed(v2XAxis)
			,m_line_width()
			,m_per_line_colour()
		{}
		bool ParseKeyword(EKeyword kw) override
		{
			switch (kw)
			{
			default:
				{
					return IObjectCreator::ParseKeyword(kw);
				}
			case EKeyword::Param:
				{
					float t[2];
					p.m_reader.RealS(t, 2);
					if (m_verts.size() < 2)
					{
						p.ReportError(EResult::Failed, "No preceding line to apply parametric values to");
					}
					auto& p0 = m_verts[m_verts.size() - 2];
					auto& p1 = m_verts[m_verts.size() - 1];
					auto dir = p1 - p0;
					auto pt = p0;
					p0 = pt + t[0] * dir;
					p1 = pt + t[1] * dir;
					return true;
				}
			case EKeyword::Dashed:
				{
					p.m_reader.Vector2S(m_dashed);
					return true;
				}
			case EKeyword::Width:
				{
					p.m_reader.RealS(m_line_width);
					return true;
				}
			}
		}
		void Parse() override
		{
			pr::v4 p0, p1;
			p.m_reader.Vector3(p0, 1.0f);
			p.m_reader.Vector3(p1, 0.0f);
			m_verts.push_back(p0);
			m_verts.push_back(p0 + p1);

			// Look for the optional colour
			if (!m_per_line_colour)
				m_per_line_colour = p.m_reader.IsMatch(8, std::wregex(L"[0-9a-fA-F]{8}"));

			if (*m_per_line_colour)
			{
				pr::Colour32 col;
				p.m_reader.Int(col.argb, 16);
				m_colours.push_back(col);
				m_colours.push_back(col);
			}
		}
		void CreateModel(LdrObject* obj) override
		{
			// Validate
			if (m_verts.size() < 2)
			{
				p.ReportError(EResult::Failed, FmtS("LineD object '%s' description incomplete", obj->TypeAndName().c_str()));
				return;
			}

			// Convert lines to dashed lines
			if (m_dashed != v2XAxis)
			{
				// Convert each line segment to dashed lines
				VCont verts;
				std::swap(verts, m_verts);
				DashLineList(verts, m_verts, m_dashed);
			}

			// Create the model
			obj->m_model = ModelGenerator<>::Lines(p.m_rdr, int(m_verts.size() / 2), m_verts.data(), int(m_colours.size()), m_colours.data());
			obj->m_model->m_name = obj->TypeAndName();

			// Use thick lines
			if (m_line_width != 0.0f)
			{
				auto shdr = ThickLineShaderLL(p.m_rdr, m_line_width);
				for (auto& nug : obj->m_model->m_nuggets)
					nug.m_smap[ERenderStep::ForwardRender].m_gs = shdr;
			}
		}
	};

	// ELdrObject::LineStrip
	template <> struct ObjectCreator<ELdrObject::LineStrip> :IObjectCreator
	{
		v2 m_dashed;
		float m_line_width;
		std::optional<bool> m_per_vert_colour;
		bool m_smooth;

		ObjectCreator(ParseParams& p)
			:IObjectCreator(p)
			,m_dashed(v2XAxis)
			,m_line_width()
			,m_per_vert_colour()
			,m_smooth()
		{}
		bool ParseKeyword(EKeyword kw) override
		{
			switch (kw)
			{
			default:
				{
					return IObjectCreator::ParseKeyword(kw);
				}
			case EKeyword::Param:
				{
					float t[2];
					p.m_reader.RealS(t, 2);
					if (m_verts.size() < 2)
					{
						p.ReportError(EResult::Failed, "No preceding line to apply parametric values to");
					}
					auto& p0 = m_verts[m_verts.size() - 2];
					auto& p1 = m_verts[m_verts.size() - 1];
					auto dir = p1 - p0;
					auto pt = p0;
					p0 = pt + t[0] * dir;
					p1 = pt + t[1] * dir;
					return true;
				}
			case EKeyword::Smooth:
				{
					m_smooth = true;
					return true;
				}
			case EKeyword::Dashed:
				{
					p.m_reader.Vector2S(m_dashed);
					return true;
				}
			case EKeyword::Width:
				{
					p.m_reader.RealS(m_line_width);
					return true;
				}
			}
		}
		void Parse() override
		{
			pr::v4 pt;
			p.m_reader.Vector3(pt, 1.0f);
			m_verts.push_back(pt);

			// Look for the optional colour
			if (!m_per_vert_colour)
				m_per_vert_colour = p.m_reader.IsMatch(8, std::wregex(L"[0-9a-fA-F]{8}"));

			if (m_per_vert_colour)
			{
				pr::Colour32 col;
				p.m_reader.Int(col.argb, 16);
				m_colours.push_back(col);
			}
		}
		void CreateModel(LdrObject* obj) override
		{
			// Allow line strips to have 0 or 1 point because they are a created from
			// lists of points and treating 0 or 1 as a special case is inconvenient
			if (m_verts.size() < 2)
				return;

			// Smooth the points
			if (m_smooth)
			{
				VCont verts;
				std::swap(verts, m_verts);
				pr::Smooth(verts, m_verts);
			}

			// Convert lines to dashed lines
			auto line_strip = true;
			if (m_dashed != v2XAxis)
			{
				// 'Dashing' a line turns it into a line list
				VCont verts;
				std::swap(verts, m_verts);
				DashLineStrip(verts, m_verts, m_dashed);
				line_strip = false;
			}

			// The thick line strip shader uses lineadj which requires an extra first and last vert
			if (line_strip && m_line_width != 0.0f)
			{
				m_verts.insert(std::begin(m_verts), m_verts.front());
				m_verts.insert(std::end(m_verts), m_verts.back());
			}

			// Create the model
			obj->m_model = line_strip
				? ModelGenerator<>::LineStrip(p.m_rdr, int(m_verts.size() - 1), m_verts.data(), int(m_colours.size()), m_colours.data())
				: ModelGenerator<>::Lines(p.m_rdr, int(m_verts.size() / 2), m_verts.data(), int(m_colours.size()), m_colours.data());
			obj->m_model->m_name = obj->TypeAndName();

			// Use thick lines
			if (m_line_width != 0.0f)
			{
				auto shdr = line_strip
					? ThickLineShaderLS(p.m_rdr, m_line_width)
					: ThickLineShaderLL(p.m_rdr, m_line_width);

				for (auto& nug : obj->m_model->m_nuggets)
				{
					nug.m_topo = line_strip ? EPrim::LineStripAdj : EPrim::LineList;
					nug.m_smap[ERenderStep::ForwardRender].m_gs = shdr;
				}
			}
		}
	};

	// ELdrObject::LineBox
	template <> struct ObjectCreator<ELdrObject::LineBox> :IObjectCreator
	{
		v2 m_dashed;
		float m_line_width;

		ObjectCreator(ParseParams& p)
			:IObjectCreator(p)
			,m_dashed(v2XAxis)
			,m_line_width()
		{}
		bool ParseKeyword(EKeyword kw) override
		{
			switch (kw)
			{
			default:
				{
					return IObjectCreator::ParseKeyword(kw);
				}
			case EKeyword::Dashed:
				{
					p.m_reader.Vector2S(m_dashed);
					return true;
				}
			case EKeyword::Width:
				{
					p.m_reader.RealS(m_line_width);
					return true;
				}
			}
		}
		void Parse() override
		{
			pr::v4 dim;
			p.m_reader.Real(dim.x);
			if (p.m_reader.IsKeyword() || p.m_reader.IsSectionEnd()) dim.y = dim.x; else p.m_reader.Real(dim.y);
			if (p.m_reader.IsKeyword() || p.m_reader.IsSectionEnd()) dim.z = dim.y; else p.m_reader.Real(dim.z);
			dim *= 0.5f;

			m_verts.push_back(v4(-dim.x, -dim.y, -dim.z, 1.0f));
			m_verts.push_back(v4(+dim.x, -dim.y, -dim.z, 1.0f));
			m_verts.push_back(v4(+dim.x, +dim.y, -dim.z, 1.0f));
			m_verts.push_back(v4(-dim.x, +dim.y, -dim.z, 1.0f));
			m_verts.push_back(v4(-dim.x, -dim.y, +dim.z, 1.0f));
			m_verts.push_back(v4(+dim.x, -dim.y, +dim.z, 1.0f));
			m_verts.push_back(v4(+dim.x, +dim.y, +dim.z, 1.0f));
			m_verts.push_back(v4(-dim.x, +dim.y, +dim.z, 1.0f));

			pr::uint16 idx[] = { 0, 1, 1, 2, 2, 3, 3, 0, 4, 5, 5, 6, 6, 7, 7, 4, 0, 4, 1, 5, 2, 6, 3, 7 };
			m_indices.insert(m_indices.end(), idx, idx + PR_COUNTOF(idx));
		}
		void CreateModel(LdrObject* obj) override
		{
			// Validate
			if (m_verts.empty())
			{
				p.ReportError(EResult::Failed, FmtS("LineBox object '%s' description incomplete", obj->TypeAndName().c_str()));
				return;
			}

			// Convert lines to dashed lines
			if (m_dashed != v2XAxis)
			{
				// Convert each line segment to dashed lines
				VCont verts;
				std::swap(verts, m_verts);
				DashLineList(verts, m_verts, m_dashed);
			}

			// Create the model
			auto cdata = MeshCreationData()
				.verts  (m_verts.data(), int(m_verts.size()))
				.indices(m_indices.data(), int(m_indices.size()))
				.colours(m_colours.data(), int(m_colours.size()))
				.nuggets({NuggetProps(EPrim::LineList, EGeom::Vert|EGeom::Colr)});
			obj->m_model = ModelGenerator<>::Mesh(p.m_rdr, cdata);
			obj->m_model->m_name = obj->TypeAndName();

			// Use thick lines
			if (m_line_width != 0.0f)
			{
				// Get or create an instance of the thick line shader
				auto shdr = ThickLineShaderLL(p.m_rdr, m_line_width);
				for (auto& nug : obj->m_model->m_nuggets)
					nug.m_smap[ERenderStep::ForwardRender].m_gs = shdr;
			}
		}
	};

	// ELdrObject::Grid
	template <> struct ObjectCreator<ELdrObject::Grid> :IObjectCreator
	{
		v2 m_dashed;
		float m_line_width;

		ObjectCreator(ParseParams& p)
			:IObjectCreator(p)
			,m_dashed(v2XAxis)
			,m_line_width()
		{}
		bool ParseKeyword(EKeyword kw) override
		{
			switch (kw)
			{
			default:
				{
					return
						IObjectCreator::ParseKeyword(kw);
				}
			case EKeyword::Dashed:
				{
					p.m_reader.Vector2S(m_dashed);
					return true;
				}
			case EKeyword::Width:
				{
					p.m_reader.RealS(m_line_width);
					return true;
				}
			}
		}
		void Parse() override
		{
			pr::v2 dim, div;
			p.m_reader.Vector2(dim);
			if (p.m_reader.IsKeyword() || p.m_reader.IsSectionEnd())
				div = dim;
			else
				p.m_reader.Vector2(div);

			pr::v2 step = dim / div;
			for (float i = -dim.x / 2; i <= dim.x / 2; i += step.x)
			{
				m_verts.push_back(v4(i, -dim.y / 2, 0, 1));
				m_verts.push_back(v4(i, dim.y / 2, 0, 1));
			}
			for (float i = -dim.y / 2; i <= dim.y / 2; i += step.y)
			{
				m_verts.push_back(v4(-dim.x / 2, i, 0, 1));
				m_verts.push_back(v4(dim.x / 2, i, 0, 1));
			}
		}
		void CreateModel(LdrObject* obj) override
		{
			// Validate
			if (m_verts.empty())
			{
				p.ReportError(EResult::Failed, FmtS("Grid object '%s' description incomplete", obj->TypeAndName().c_str()));
				return;
			}

			// Convert lines to dashed lines
			if (m_dashed != v2XAxis)
			{
				// Convert each line segment to dashed lines
				VCont verts;
				std::swap(verts, m_verts);
				DashLineList(verts, m_verts, m_dashed);
			}

			// Create the model
			obj->m_model = ModelGenerator<>::Lines(p.m_rdr, int(m_verts.size() / 2), m_verts.data(), int(m_colours.size()), m_colours.data());
			obj->m_model->m_name = obj->TypeAndName();

			// Use thick lines
			if (m_line_width != 0.0f)
			{
				// Get or create an instance of the thick line shader
				auto shdr = ThickLineShaderLL(p.m_rdr, m_line_width);
				for (auto& nug : obj->m_model->m_nuggets)
					nug.m_smap[ERenderStep::ForwardRender].m_gs = shdr;
			}
		}
	};

	// ELdrObject::Spline
	template <> struct ObjectCreator<ELdrObject::Spline> :IObjectCreator
	{
		pr::vector<pr::Spline> m_splines;
		CCont m_spline_colours;
		float m_line_width;
		std::optional<bool> m_per_segment_colour;

		ObjectCreator(ParseParams& p)
			:IObjectCreator(p)
			,m_splines()
			,m_spline_colours()
			,m_line_width()
			,m_per_segment_colour()
		{}
		bool ParseKeyword(EKeyword kw) override
		{
			switch (kw)
			{
			default:
				{
					return IObjectCreator::ParseKeyword(kw);
				}
			case EKeyword::Width:
				{
					p.m_reader.RealS(m_line_width);
					return true;
				}
			}
		}
		void Parse() override
		{
			pr::Spline spline;
			p.m_reader.Vector3(spline.x, 1.0f);
			p.m_reader.Vector3(spline.y, 1.0f);
			p.m_reader.Vector3(spline.z, 1.0f);
			p.m_reader.Vector3(spline.w, 1.0f);
			m_splines.push_back(spline);

			// Look for the optional colour
			if (!m_per_segment_colour)
				m_per_segment_colour = p.m_reader.IsMatch(8, std::wregex(L"[0-9a-fA-F]{8}"));

			if (m_per_segment_colour)
			{
				pr::Colour32 col;
				p.m_reader.Int(col.argb, 16);
				m_spline_colours.push_back(col);
			}
		}
		void CreateModel(LdrObject* obj) override
		{
			// Validate
			if (m_splines.empty())
			{
				p.ReportError(EResult::Failed, FmtS("Spline object '%s' description incomplete", obj->TypeAndName().c_str()));
				return;
			}

			// Generate a line strip for all spline segments (separated using strip-cut indices)
			auto seg = -1;
			auto thick = m_line_width != 0.0f;
			pr::vector<pr::v4, 30, true> raster;
			for (auto& spline : m_splines)
			{
				++seg;

				// Generate points for the spline
				raster.resize(0);
				pr::Raster(spline, raster, 30);

				// Check for 16-bit index overflow
				if (m_verts.size() + raster.size() >= 0xFFFF)
				{
					p.ReportError(EResult::Failed, FmtS("Spline object '%s' is too large (index count >= 0xffff)", obj->TypeAndName().c_str()));
					return;
				}

				// Add the line strip to the geometry buffers
				auto vert = uint16(m_verts.size());
				m_verts.insert(std::end(m_verts), std::begin(raster), std::end(raster));

				{// Indices
					auto ibeg = m_indices.size();
					m_indices.reserve(m_indices.size() + raster.size() + (thick ? 2 : 0) + 1);

					// The thick line strip shader uses lineadj which requires an extra first and last vert
					if (thick) m_indices.push_back_fast(vert);

					auto iend = ibeg + raster.size();
					for (auto i = ibeg; i != iend; ++i)
						m_indices.push_back_fast(vert++);

					if (thick) m_indices.push_back_fast(vert);
					m_indices.push_back_fast(uint16(-1)); // strip-cut
				}

				// Colours
				if (*m_per_segment_colour)
				{
					auto ibeg = m_colours.size();
					m_colours.reserve(m_colours.size() + raster.size());
					
					auto iend = ibeg + raster.size();
					for (auto i = ibeg; i != iend; ++i)
						m_colours.push_back_fast(m_spline_colours[seg]);
				}
			}

			// Create the model
			auto cdata = MeshCreationData()
				.verts  (m_verts.data(), int(m_verts.size()))
				.indices(m_indices.data(), int(m_indices.size()))
				.colours(m_colours.data(), int(m_colours.size()))
				.nuggets({NuggetProps(EPrim::LineStrip, EGeom::Vert|EGeom::Colr)});
			obj->m_model = ModelGenerator<>::Mesh(p.m_rdr, cdata);
			obj->m_model->m_name = obj->TypeAndName();

			// Use thick lines
			if (thick)
			{
				auto shdr = ThickLineShaderLS(p.m_rdr, m_line_width);
				for (auto& nug : obj->m_model->m_nuggets)
				{
					nug.m_topo = EPrim::LineStripAdj;
					nug.m_smap[ERenderStep::ForwardRender].m_gs = shdr;
				}
			}
		}
	};

	// ELdrObject::Arrow
	template <> struct ObjectCreator<ELdrObject::Arrow> :IObjectCreator
	{
		enum EArrowType
		{
			Invalid = -1,
			Line = 0,
			Fwd = 1 << 0,
			Back = 1 << 1,
			FwdBack = Fwd | Back
		};

		EArrowType m_type;
		float m_line_width;
		std::optional<bool> m_per_vert_colour;
		bool m_smooth;

		ObjectCreator(ParseParams& p)
			:IObjectCreator(p)
			,m_type(EArrowType::Invalid)
			,m_line_width()
			,m_per_vert_colour()
			,m_smooth()
		{}
		bool ParseKeyword(EKeyword kw) override
		{
			switch (kw)
			{
			default:
				{
					return
						IObjectCreator::ParseKeyword(kw);
				}
			case EKeyword::Width:
				{
					p.m_reader.RealS(m_line_width);
					return true;
				}
			case EKeyword::Smooth:
				{
					m_smooth = true;
					return true;
				}
			}
		}
		void Parse() override
		{
			// Expect the arrow type first
			if (m_type == EArrowType::Invalid)
			{
				string32 ty;
				p.m_reader.Identifier(ty);
				if      (pr::str::EqualNI(ty, "Line"   )) m_type = EArrowType::Line;
				else if (pr::str::EqualNI(ty, "Fwd"    )) m_type = EArrowType::Fwd;
				else if (pr::str::EqualNI(ty, "Back"   )) m_type = EArrowType::Back;
				else if (pr::str::EqualNI(ty, "FwdBack")) m_type = EArrowType::FwdBack;
				else p.ReportError(EResult::UnknownValue, "arrow type must one of Line, Fwd, Back, FwdBack");
			}
			else
			{
				pr::v4 pt;
				p.m_reader.Vector3(pt, 1.0f);
				m_verts.push_back(pt);

				// Look for the optional colour
				if (!m_per_vert_colour)
					m_per_vert_colour = p.m_reader.IsMatch(8, std::wregex(L"[0-9a-fA-F]{8}"));

				if (*m_per_vert_colour)
				{
					pr::Colour32 col;
					p.m_reader.Int(col.argb, 16);
					m_colours.push_back(col);
				}
			}
		}
		void CreateModel(LdrObject* obj) override
		{
			// Validate
			if (m_verts.size() < 2)
			{
				p.ReportError(EResult::Failed, FmtS("Arrow object '%s' description incomplete", obj->TypeAndName().c_str()));
				return;
			}

			// Convert the points into a spline if smooth is specified
			if (m_smooth)
			{
				VCont verts;
				std::swap(verts, m_verts);
				pr::Smooth(verts, m_verts);
			}

			pr::geometry::Props props;

			// Colour interpolation iterator
			auto col = pr::CreateLerpRepeater(m_colours.data(), int(m_colours.size()), int(m_verts.size()), pr::Colour32White);
			auto cc = [&](pr::Colour32 c) { props.m_has_alpha |= HasAlpha(c); return c; };

			// Model bounding box
			auto bb = [&](v4 const& v) { pr::Encompass(props.m_bbox, v); return v; };

			// Generate the model
			// 'm_point' should contain line strip data
			ModelGenerator<>::Cache<> cache(int(m_verts.size() + 2), int(m_verts.size() + 2));

			auto v_in  = std::begin(m_verts);
			auto v_out = std::begin(cache.m_vcont);
			auto i_out = std::begin(cache.m_icont);
			pr::Colour32 c = pr::Colour32White;
			pr::uint16 index = 0;

			// Add the back arrow head geometry (a point)
			if (m_type & EArrowType::Back)
			{
				SetPCN(*v_out++, *v_in, *col, pr::Normalise3(*v_in - *(v_in+1)));
				*i_out++ = index++;
			}

			// Add the line strip
			for (std::size_t i = 0, iend = m_verts.size(); i != iend; ++i)
			{
				SetPC(*v_out++, bb(*v_in++), c = cc(*col++));
				*i_out++ = index++;
			}
			
			// Add the forward arrow head geometry (a point)
			if (m_type & EArrowType::Fwd)
			{
				--v_in;
				SetPCN(*v_out++, *v_in, c, pr::Normalise3(*v_in - *(v_in-1)));
				*i_out++ = index++;
			}

			// Create the model
			VBufferDesc vb(cache.m_vcont.size(), &cache.m_vcont[0]);
			IBufferDesc ib(cache.m_icont.size(), &cache.m_icont[0]);
			obj->m_model = p.m_rdr.m_mdl_mgr.CreateModel(MdlSettings(vb, ib, props.m_bbox));
			obj->m_model->m_name = obj->TypeAndName();

			// Get instances of the arrow head geometry shader and the thick line shader
			auto thk_shdr = ThickLineShaderLL(p.m_rdr, m_line_width);
			auto arw_shdr = ArrowHeadShader(p.m_rdr, m_line_width*2);

			// Create nuggets
			NuggetProps nug;
			pr::rdr::Range vrange(0,0);
			pr::rdr::Range irange(0,0);
			if (m_type & EArrowType::Back)
			{
				vrange = pr::rdr::Range(0, 1);
				irange = pr::rdr::Range(0, 1);
				nug.m_topo = EPrim::PointList;
				nug.m_geom = EGeom::Vert|EGeom::Colr;
				nug.m_smap[ERenderStep::ForwardRender].m_gs = arw_shdr;
				nug.m_vrange = vrange;
				nug.m_irange = irange;
				nug.m_flags = SetBits(nug.m_flags, ENuggetFlag::GeometryHasAlpha, cache.m_vcont[0].m_diff.a != 1.0f);
				obj->m_model->CreateNugget(nug);
			}
			{
				vrange = pr::rdr::Range(vrange.m_end, vrange.m_end + m_verts.size());
				irange = pr::rdr::Range(irange.m_end, irange.m_end + m_verts.size());
				nug.m_topo = EPrim::LineStrip;
				nug.m_geom = EGeom::Vert|EGeom::Colr;
				nug.m_smap[ERenderStep::ForwardRender].m_gs = m_line_width != 0 ? static_cast<ShaderPtr>(thk_shdr) : ShaderPtr();
				nug.m_vrange = vrange;
				nug.m_irange = irange;
				nug.m_flags = SetBits(nug.m_flags, ENuggetFlag::GeometryHasAlpha, props.m_has_alpha);
				obj->m_model->CreateNugget(nug);
			}
			if (m_type & EArrowType::Fwd)
			{
				vrange = pr::rdr::Range(vrange.m_end, vrange.m_end + 1);
				irange = pr::rdr::Range(irange.m_end, irange.m_end + 1);
				nug.m_topo = EPrim::PointList;
				nug.m_geom = EGeom::Vert|EGeom::Colr;
				nug.m_smap[ERenderStep::ForwardRender].m_gs = arw_shdr;
				nug.m_vrange = vrange;
				nug.m_irange = irange;
				nug.m_flags = SetBits(nug.m_flags, ENuggetFlag::GeometryHasAlpha, cache.m_vcont.back().m_diff.a != 1.0f);
				obj->m_model->CreateNugget(nug);
			}
		}
	};

	// ELdrObject::Matrix3x3
	template <> struct ObjectCreator<ELdrObject::Matrix3x3> :IObjectCreator
	{
		float  m_line_width;

		ObjectCreator(ParseParams& p)
			:IObjectCreator(p)
			,m_line_width()
		{}
		bool ParseKeyword(EKeyword kw) override
		{
			switch (kw)
			{
			default:
				{
					return
						IObjectCreator::ParseKeyword(kw);
				}
			case EKeyword::Width:
				{
					p.m_reader.RealS(m_line_width);
					return true;
				}
			}
		}
		void Parse() override
		{
			pr::m4x4 basis;
			p.m_reader.Matrix3x3(basis.rot);

			pr::v4       pts[] = { pr::v4Origin, basis.x.w1(), pr::v4Origin, basis.y.w1(), pr::v4Origin, basis.z.w1() };
			pr::Colour32 col[] = { pr::Colour32Red, pr::Colour32Red, pr::Colour32Green, pr::Colour32Green, pr::Colour32Blue, pr::Colour32Blue };
			pr::uint16   idx[] = { 0, 1, 2, 3, 4, 5 };

			m_verts.insert(m_verts.end(), pts, pts + PR_COUNTOF(pts));
			m_colours.insert(m_colours.end(), col, col + PR_COUNTOF(col));
			m_indices.insert(m_indices.end(), idx, idx + PR_COUNTOF(idx));
		}
		void CreateModel(LdrObject* obj) override
		{
			// Validate
			if (m_verts.empty())
			{
				p.ReportError(EResult::Failed, FmtS("Matrix3x3 object '%s' description incomplete", obj->TypeAndName().c_str()));
				return;
			}

			// Create the model
			auto cdata = MeshCreationData()
				.verts(m_verts.data(), int(m_verts.size()))
				.indices(m_indices.data(), int(m_indices.size()))
				.colours(m_colours.data(), int(m_colours.size()))
				.nuggets({NuggetProps(EPrim::LineList, EGeom::Vert|EGeom::Colr)});
			obj->m_model = ModelGenerator<>::Mesh(p.m_rdr, cdata);
			obj->m_model->m_name = obj->TypeAndName();

			// Use thick lines
			if (m_line_width != 0.0f)
			{
				// Get or create an instance of the thick line shader
				auto shdr = ThickLineShaderLL(p.m_rdr, m_line_width);
				for (auto& nug : obj->m_model->m_nuggets)
					nug.m_smap[ERenderStep::ForwardRender].m_gs = shdr;
			}
		}
	};

	// ELdrObject::CoordFrame
	template <> struct ObjectCreator<ELdrObject::CoordFrame> :IObjectCreator
	{
		float m_line_width;
		float m_scale;
		bool m_rh;

		ObjectCreator(ParseParams& p)
			:IObjectCreator(p)
			,m_line_width()
			,m_scale()
			,m_rh(true)
		{
			pr::v4       pts[] = { pr::v4Origin, pr::v4XAxis.w1(), pr::v4Origin, pr::v4YAxis.w1(), pr::v4Origin, pr::v4ZAxis.w1() };
			pr::Colour32 col[] = { pr::Colour32Red, pr::Colour32Red, pr::Colour32Green, pr::Colour32Green, pr::Colour32Blue, pr::Colour32Blue };
			pr::uint16   idx[] = { 0, 1, 2, 3, 4, 5 };

			m_verts.insert(m_verts.end(), pts, pts + PR_COUNTOF(pts));
			m_colours.insert(m_colours.end(), col, col + PR_COUNTOF(col));
			m_indices.insert(m_indices.end(), idx, idx + PR_COUNTOF(idx));
		}
		bool ParseKeyword(EKeyword kw) override
		{
			switch (kw)
			{
			default:
				{
					return IObjectCreator::ParseKeyword(kw);
				}
			case EKeyword::Width:
				{
					p.m_reader.RealS(m_line_width);
					return true;
				}
			case EKeyword::Scale:
				{
					p.m_reader.RealS(m_scale);
					return true;
				}
			case EKeyword::LeftHanded:
				{
					m_rh = false;
					return true;
				}
			}
		}
		void CreateModel(LdrObject* obj) override
		{
			if (m_scale != 1.0f)
			{
				for (auto& pt : m_verts)
					pt.xyz *= m_scale;
			}
			if (!m_rh)
			{
				m_verts[3].xyz = -m_verts[3].xyz;
			}

			// Create the model
			obj->m_model = ModelGenerator<>::Lines(p.m_rdr, int(m_verts.size() / 2), m_verts.data(), int(m_colours.size()), m_colours.data());
			obj->m_model->m_name = obj->TypeAndName();

			// Use thick lines
			if (m_line_width != 0.0f)
			{
				// Get or create an instance of the thick line shader
				auto shdr = ThickLineShaderLL(p.m_rdr, m_line_width);
				for (auto& nug : obj->m_model->m_nuggets)
					nug.m_smap[ERenderStep::ForwardRender].m_gs = shdr;
			}
		}
	};

	#pragma endregion

	#pragma region Shapes2d

	// ELdrObject::Circle
	template <> struct ObjectCreator<ELdrObject::Circle> :IObjectCreator
	{
		creation::Textured m_tex;
		creation::MainAxis m_axis;
		v2 m_dim;
		int m_facets;
		bool m_solid;

		ObjectCreator(ParseParams& p)
			:IObjectCreator(p)
			,m_tex()
			,m_axis()
			,m_dim()
			,m_facets(40)
			,m_solid()
		{}
		bool ParseKeyword(EKeyword kw) override
		{
			switch (kw)
			{
			default:
				{
					return
						m_axis.ParseKeyword(p, kw) ||
						m_tex.ParseKeyword(p, kw) ||
						IObjectCreator::ParseKeyword(kw);
				}
			case EKeyword::Solid:
				{
					m_solid = true;
					return true;
				}
			case EKeyword::Facets:
				{
					p.m_reader.IntS(m_facets, 10);
					return true;
				}
			}
		}
		void Parse() override
		{
			p.m_reader.Real(m_dim.x);
			if (p.m_reader.IsKeyword() || p.m_reader.IsSectionEnd())
				m_dim.y = m_dim.x;
			else
				p.m_reader.Real(m_dim.y);

			if (Abs(m_dim) != m_dim)
			{
				p.ReportError(EResult::InvalidValue, "Circle dimensions contain a negative value");
			}
		}
		void CreateModel(LdrObject* obj) override
		{
			// Create the model
			obj->m_model = ModelGenerator<>::Ellipse(p.m_rdr, m_dim.x, m_dim.y, m_solid, m_facets, Colour32White, m_axis.O2WPtr(), m_tex.Material());
			obj->m_model->m_name = obj->TypeAndName();
		}
	};

	// LdrObject::Pie
	template <> struct ObjectCreator<ELdrObject::Pie> :IObjectCreator
	{
		creation::Textured m_tex;
		creation::MainAxis m_axis;
		v2 m_scale;
		v2 m_ang;
		v2 m_rad;
		int m_facets;
		bool m_solid;

		ObjectCreator(ParseParams& p)
			:IObjectCreator(p)
			,m_tex()
			,m_axis()
			,m_scale(v2One)
			,m_ang()
			,m_rad()
			,m_facets(40)
			,m_solid()
		{}
		bool ParseKeyword(EKeyword kw) override
		{
			switch (kw)
			{
			default:
				{
					return
						m_axis.ParseKeyword(p, kw) ||
						m_tex.ParseKeyword(p, kw) ||
						IObjectCreator::ParseKeyword(kw);
				}
			case EKeyword::Solid:
				{
					m_solid = true;
					return true;
				}
			case EKeyword::Scale:
				{
					p.m_reader.Vector2S(m_scale);
					return true;
				}
			case EKeyword::Facets:
				{
					p.m_reader.IntS(m_facets, 10);
					return true;
				}
			}
		}
		void Parse() override
		{
			p.m_reader.Vector2(m_ang);
			p.m_reader.Vector2(m_rad);

			m_ang.x = pr::DegreesToRadians(m_ang.x);
			m_ang.y = pr::DegreesToRadians(m_ang.y);
		}
		void CreateModel(LdrObject* obj) override
		{
			// Create the model
			obj->m_model = ModelGenerator<>::Pie(p.m_rdr, m_scale.x, m_scale.y, m_ang.x, m_ang.y, m_rad.x, m_rad.y, m_solid, m_facets, Colour32White, m_axis.O2WPtr(), m_tex.Material());
			obj->m_model->m_name = obj->TypeAndName();
		}
	};

	// ELdrObject::Rect
	template <> struct ObjectCreator<ELdrObject::Rect> :IObjectCreator
	{
		creation::Textured m_tex;
		creation::MainAxis m_axis;
		v2 m_dim;
		float m_corner_radius;
		int m_facets;
		bool m_solid;

		ObjectCreator(ParseParams& p)
			:IObjectCreator(p)
			,m_tex()
			,m_axis()
			,m_dim()
			,m_corner_radius()
			,m_facets(40)
			,m_solid()
		{}
		bool ParseKeyword(EKeyword kw) override
		{
			switch (kw)
			{
			default:
				{
					return
						m_axis.ParseKeyword(p, kw) ||
						m_tex.ParseKeyword(p, kw) ||
						IObjectCreator::ParseKeyword(kw);
				}
			case EKeyword::CornerRadius:
				{
					p.m_reader.RealS(m_corner_radius);
					return true;
				}
			case EKeyword::Facets:
				{
					p.m_reader.IntS(m_facets, 10);
					m_facets *= 4;
					return true;
				}
			case EKeyword::Solid:
				{
					m_solid = true;
					return true;
				}
			}
		}
		void Parse() override
		{
			p.m_reader.Real(m_dim.x);
			if (p.m_reader.IsKeyword() || p.m_reader.IsSectionEnd())
				m_dim.y = m_dim.x;
			else
				p.m_reader.Real(m_dim.y);
			m_dim *= 0.5f;

			if (Abs(m_dim) != m_dim)
			{
				p.ReportError(EResult::InvalidValue, "Rect dimensions contain a negative value");
			}
		}
		void CreateModel(LdrObject* obj) override
		{
			// Create the model
			obj->m_model = ModelGenerator<>::RoundedRectangle(p.m_rdr, m_dim.x, m_dim.y, m_corner_radius, m_solid, m_facets, Colour32White, m_axis.O2WPtr(), m_tex.Material());
			obj->m_model->m_name = obj->TypeAndName();
		}
	};

	// ELdrObject::Polygon
	template <> struct ObjectCreator<ELdrObject::Polygon> :IObjectCreator
	{
		creation::Textured m_tex;
		creation::MainAxis m_axis;
		pr::vector<v2> m_poly;
		std::optional<bool> m_per_vertex_colour;
		bool m_solid;

		ObjectCreator(ParseParams& p)
			:IObjectCreator(p)
			,m_tex()
			,m_axis()
			,m_poly()
			,m_per_vertex_colour()
			,m_solid()
		{}
		bool ParseKeyword(EKeyword kw) override
		{
			switch (kw)
			{
			default:
				{
					return
						m_axis.ParseKeyword(p, kw) ||
						m_tex.ParseKeyword(p, kw) ||
						IObjectCreator::ParseKeyword(kw);
				}
			case EKeyword::Solid:
				{
					m_solid = true;
					return true;
				}
			}
		}
		void Parse() override
		{
			for (;p.m_reader.IsValue();)
			{
				v2 pt;
				p.m_reader.Vector2(pt);
				m_poly.push_back(pt);

				// Look for the optional colour
				if (!m_per_vertex_colour)
					m_per_vertex_colour = p.m_reader.IsMatch(8, std::wregex(L"[0-9a-fA-F]{8}"));

				if (*m_per_vertex_colour)
				{
					Colour32 col;
					p.m_reader.Int(col.argb, 16);
					m_colours.push_back(col);
				}
			}
		}
		void CreateModel(LdrObject* obj) override
		{
			// Create the model
			obj->m_model = ModelGenerator<>::Polygon(p.m_rdr, int(m_poly.size()), m_poly.data(), m_solid, int(m_colours.size()), m_colours.data(), m_axis.O2WPtr(), m_tex.Material());
			obj->m_model->m_name = obj->TypeAndName();
		}
	};

	#pragma endregion

	#pragma region Quads

	// ELdrObject::Triangle
	template <> struct ObjectCreator<ELdrObject::Triangle> :IObjectCreator
	{
		creation::MainAxis m_axis;
		creation::Textured m_tex;
		std::optional<bool> m_per_vert_colour;

		ObjectCreator(ParseParams& p)
			:IObjectCreator(p)
			,m_axis()
			,m_tex()
			,m_per_vert_colour()
		{}
		bool ParseKeyword(EKeyword kw) override
		{
			return
				m_axis.ParseKeyword(p, kw) ||
				m_tex.ParseKeyword(p, kw) ||
				IObjectCreator::ParseKeyword(kw);
		}
		void Parse() override
		{
			v4 pt[3]; Colour32 col[3];
			for (int i = 0; i != 3; ++i)
			{
				p.m_reader.Vector3(pt[i], 1.0f);
				if (!m_per_vert_colour)
					m_per_vert_colour = p.m_reader.IsMatch(8, std::wregex(L"[0-9a-fA-F]{8}"));
				if (*m_per_vert_colour)
					p.m_reader.Int(col[i].argb, 16);
			}

			m_verts.push_back(pt[0]);
			m_verts.push_back(pt[1]);
			m_verts.push_back(pt[2]);
			m_verts.push_back(pt[2]); // create a degenerate
			if (*m_per_vert_colour)
			{
				m_colours.push_back(col[0]);
				m_colours.push_back(col[1]);
				m_colours.push_back(col[2]);
				m_colours.push_back(col[2]);
			}
		}
		void CreateModel(LdrObject* obj) override
		{
			// Validate
			if (m_verts.empty() || (m_verts.size() % 4) != 0)
			{
				p.ReportError(EResult::Failed, "Object description incomplete");
				return;
			}

			// Apply the axis id rotation
			if (m_axis.RotationNeeded())
			{
				for (auto& pt : m_verts)
					pt = m_axis.O2W() * pt;
			}

			// Create the model
			obj->m_model = ModelGenerator<>::Quad(p.m_rdr, int(m_verts.size() / 4), m_verts.data(), int(m_colours.size()), m_colours.data(), pr::m4x4Identity, m_tex.Material());
			obj->m_model->m_name = obj->TypeAndName();
		}
	};

	// ELdrObject::Quad
	template <> struct ObjectCreator<ELdrObject::Quad> :IObjectCreator
	{
		creation::MainAxis m_axis;
		creation::Textured m_tex;
		std::optional<bool> m_per_vert_colour;

		ObjectCreator(ParseParams& p)
			:IObjectCreator(p)
			,m_axis()
			,m_tex()
			,m_per_vert_colour()
		{}
		bool ParseKeyword(EKeyword kw) override
		{
			return
				m_axis.ParseKeyword(p, kw) ||
				m_tex.ParseKeyword(p, kw) ||
				IObjectCreator::ParseKeyword(kw);
		}
		void Parse() override
		{
			v4 pt[4]; Colour32 col[4];
			for (int i = 0; i != 4; ++i)
			{
				p.m_reader.Vector3(pt[i], 1.0f);
				if (!m_per_vert_colour)
					m_per_vert_colour = p.m_reader.IsMatch(8, std::wregex(L"[0-9a-fA-F]{8}"));
				if (*m_per_vert_colour)
					p.m_reader.Int(col[i].argb, 16);
			}

			m_verts.push_back(pt[0]);
			m_verts.push_back(pt[1]);
			m_verts.push_back(pt[2]);
			m_verts.push_back(pt[3]);
			if (*m_per_vert_colour)
			{
				m_colours.push_back(col[0]);
				m_colours.push_back(col[1]);
				m_colours.push_back(col[2]);
				m_colours.push_back(col[3]);
			}
		}
		void CreateModel(LdrObject* obj) override
		{
			// Validate
			if (m_verts.empty() || (m_verts.size() % 4) != 0)
			{
				p.ReportError(EResult::Failed, "Object description incomplete");
				return;
			}

			// Apply the axis id rotation
			if (m_axis.RotationNeeded())
			{
				for (auto& pt : m_verts)
					pt = m_axis.O2W() * pt;
			}

			// Create the model
			obj->m_model = ModelGenerator<>::Quad(p.m_rdr, int(m_verts.size() / 4), m_verts.data(), int(m_colours.size()), m_colours.data(), pr::m4x4Identity, m_tex.Material());
			obj->m_model->m_name = obj->TypeAndName();
		}
	};

	// ELdrObject::Plane
	template <> struct ObjectCreator<ELdrObject::Plane> :IObjectCreator
	{
		creation::Textured m_tex;

		ObjectCreator(ParseParams& p)
			:IObjectCreator(p)
			,m_tex()
		{}
		bool ParseKeyword(EKeyword kw) override
		{
			return
				m_tex.ParseKeyword(p, kw) ||
				IObjectCreator::ParseKeyword(kw);
		}
		void Parse() override
		{
			v4 pnt, fwd; float w, h;
			p.m_reader.Vector3(pnt, 1.0f);
			p.m_reader.Vector3(fwd, 0.0f);
			p.m_reader.Real(w);
			p.m_reader.Real(h);

			fwd = Normalise3(fwd);
			auto up = Perpendicular(fwd);
			auto left = Cross3(up, fwd);
			up *= h * 0.5f;
			left *= w * 0.5f;
			m_verts.push_back(pnt - up - left);
			m_verts.push_back(pnt - up + left);
			m_verts.push_back(pnt + up - left);
			m_verts.push_back(pnt + up + left);
		}
		void CreateModel(LdrObject* obj) override
		{
			// Validate
			if (m_verts.empty() || (m_verts.size() % 4) != 0)
			{
				p.ReportError(EResult::Failed, "Object description incomplete");
				return;
			}

			// Create the model
			obj->m_model = ModelGenerator<>::Quad(p.m_rdr, int(m_verts.size() / 4), m_verts.data(), int(m_colours.size()), m_colours.data(), pr::m4x4Identity, m_tex.Material());
			obj->m_model->m_name = obj->TypeAndName();
		}
	};

	// ELdrObject::Ribbon
	template <> struct ObjectCreator<ELdrObject::Ribbon> :IObjectCreator
	{
		creation::Textured m_tex;
		creation::MainAxis m_axis;
		float m_width;
		std::optional<bool> m_per_vert_colour;
		bool m_smooth;

		ObjectCreator(ParseParams& p)
			:IObjectCreator(p)
			,m_tex()
			,m_axis()
			,m_width(10.0f)
			,m_per_vert_colour()
			,m_smooth(false)
		{}
		bool ParseKeyword(EKeyword kw) override
		{
			switch (kw)
			{
			default:
				{
					return
						m_axis.ParseKeyword(p, kw) ||
						m_tex.ParseKeyword(p, kw) ||
						IObjectCreator::ParseKeyword(kw);
				}
			case EKeyword::Width:
				{
					p.m_reader.RealS(m_width);
					return true;
				}
			case EKeyword::Smooth:
				{
					m_smooth = true;
					return true;
				}
			}
		}
		void Parse() override
		{
			pr::v4 pt;
			p.m_reader.Vector3(pt, 1.0f);
			m_verts.push_back(pt);

			if (!m_per_vert_colour)
				m_per_vert_colour = p.m_reader.IsMatch(8, std::wregex(L"[0-9a-fA-F]{8}"));
			
			if (*m_per_vert_colour)
			{
				pr::Colour32 col;
				p.m_reader.Int(col.argb, 16);
				m_colours.push_back(col);
			}
		}
		void CreateModel(LdrObject* obj) override
		{
			// Validate
			if (m_verts.size() < 2)
			{
				p.ReportError(EResult::Failed, "Object description incomplete");
				return;
			}

			// Smooth the points
			if (m_smooth)
			{
				VCont verts;
				std::swap(verts, m_verts);
				pr::Smooth(verts, m_verts);
			}

			pr::v4 normal = m_axis.m_align;
			obj->m_model = ModelGenerator<>::QuadStrip(p.m_rdr, int(m_verts.size() - 1), m_verts.data(), m_width, 1, &normal, int(m_colours.size()), m_colours.data(), m_tex.Material());
			obj->m_model->m_name = obj->TypeAndName();
		}
	};

	#pragma endregion

	#pragma region Shapes3d

	// ELdrObject::Box
	template <> struct ObjectCreator<ELdrObject::Box> :IObjectCreator
	{
		creation::Textured m_tex;
		v4 m_dim;

		ObjectCreator(ParseParams& p)
			:IObjectCreator(p)
			,m_tex()
			,m_dim()
		{}
		bool ParseKeyword(EKeyword kw) override
		{
			return
				m_tex.ParseKeyword(p, kw) ||
				IObjectCreator::ParseKeyword(kw);
		}
		void Parse() override
		{
			p.m_reader.Real(m_dim.x);
			if (p.m_reader.IsValue()) p.m_reader.Real(m_dim.y); else m_dim.y = m_dim.x;
			if (p.m_reader.IsValue()) p.m_reader.Real(m_dim.z); else m_dim.z = m_dim.y;
			m_dim *= 0.5f;
		}
		void CreateModel(LdrObject* obj) override
		{
			// Create the model
			obj->m_model = ModelGenerator<>::Box(p.m_rdr, m_dim, pr::m4x4Identity, pr::Colour32White, m_tex.Material());
			obj->m_model->m_name = obj->TypeAndName();
		}
	};

	// ELdrObject::Bar
	template <> struct ObjectCreator<ELdrObject::Bar> :IObjectCreator
	{
		creation::Textured m_tex;
		v4 m_pt0, m_pt1, m_up;
		float m_width, m_height;

		ObjectCreator(ParseParams& p)
			:IObjectCreator(p)
			,m_tex()
			,m_pt0()
			,m_pt1()
			,m_up(v4YAxis)
			,m_width(0.1f)
			,m_height(0.1f)
		{}
		bool ParseKeyword(EKeyword kw) override
		{
			switch (kw)
			{
			default:
				{
					return
						m_tex.ParseKeyword(p, kw) ||
						IObjectCreator::ParseKeyword(kw);
				}
			case EKeyword::Up:
				{
					p.m_reader.Vector3S(m_up, 0.0f);
					return true;
				}
			}
		}
		void Parse() override
		{
			p.m_reader.Vector3(m_pt0, 1.0f);
			p.m_reader.Vector3(m_pt1, 1.0f);
			p.m_reader.Real(m_width);
			if (p.m_reader.IsValue())
				p.m_reader.Real(m_height);
			else
				m_height = m_width;
		}
		void CreateModel(LdrObject* obj) override
		{
			auto dim = v4(m_width, m_height, pr::Length3(m_pt1 - m_pt0), 0.0f) * 0.5f;
			auto b2w = pr::OriFromDir(m_pt1 - m_pt0, 2, m_up, (m_pt1 + m_pt0) * 0.5f);
			obj->m_model = ModelGenerator<>::Box(p.m_rdr, dim, b2w, Colour32White, m_tex.Material());
			obj->m_model->m_name = obj->TypeAndName();
		}
	};

	// ELdrObject::BoxList
	template <> struct ObjectCreator<ELdrObject::BoxList> :IObjectCreator
	{
		creation::Textured m_tex;
		pr::vector<v4,16> m_location;
		v4 m_dim;

		ObjectCreator(ParseParams& p)
			:IObjectCreator(p)
			,m_tex()
			,m_location()
			,m_dim()
		{}
		bool ParseKeyword(EKeyword kw) override
		{
			return
				m_tex.ParseKeyword(p, kw) ||
				IObjectCreator::ParseKeyword(kw);
		}
		void Parse() override
		{
			v4 v;
			p.m_reader.Vector3(v, 1.0f);
			if (m_dim == pr::v4Zero)
				m_dim = v.w0();
			else
				m_location.push_back(v);
		}
		void CreateModel(LdrObject* obj) override
		{
			// Validate
			if (m_dim == pr::v4Zero || m_location.size() == 0)
			{
				p.ReportError(EResult::Failed, "BoxList object description incomplete");
				return;
			}
			if (Abs(m_dim) != m_dim)
			{
				p.ReportError(EResult::InvalidValue, "BoxList box dimensions contain a negative value");
				return;
			}

			m_dim *= 0.5f;

			// Create the model
			obj->m_model = ModelGenerator<>::BoxList(p.m_rdr, int(m_location.size()), m_location.data(), m_dim, 0, 0, m_tex.Material());
			obj->m_model->m_name = obj->TypeAndName();
		}
	};

	// ELdrObject::FrustumWH
	template <> struct ObjectCreator<ELdrObject::FrustumWH> :IObjectCreator
	{
		creation::Textured m_tex;
		creation::MainAxis m_axis;
		pr::v4 m_pt[8];
		float m_width, m_height;
		float m_near, m_far;
		float m_view_plane;

		ObjectCreator(ParseParams& p)
			:IObjectCreator(p)
			,m_tex()
			,m_axis()
			,m_pt()
			,m_width(1.0f)
			,m_height(1.0f)
			,m_near(0.0f)
			,m_far(1.0f)
			,m_view_plane(1.0f)
		{}
		bool ParseKeyword(EKeyword kw) override
		{
			switch (kw)
			{
			default:
				{
					return
						m_axis.ParseKeyword(p, kw) ||
						m_tex.ParseKeyword(p, kw) ||
						IObjectCreator::ParseKeyword(kw);
				}
			case EKeyword::ViewPlaneZ:
				{
					p.m_reader.RealS(m_view_plane);
					return true;
				}
			}
		}
		void Parse() override
		{
			p.m_reader.Real(m_width);
			p.m_reader.Real(m_height);
			p.m_reader.Real(m_near);
			p.m_reader.Real(m_far);
		}
		void CreateModel(LdrObject* obj) override
		{
			float w = m_width  * 0.5f / m_view_plane;
			float h = m_height * 0.5f / m_view_plane;
			float n = m_near, f = m_far;

			m_pt[0] = v4(-n*w, -n*h, n, 1.0f);
			m_pt[1] = v4(-n*w, +n*h, n, 1.0f);
			m_pt[2] = v4(+n*w, -n*h, n, 1.0f);
			m_pt[3] = v4(+n*w, +n*h, n, 1.0f);
			m_pt[4] = v4(+f*w, -f*h, f, 1.0f);
			m_pt[5] = v4(+f*w, +f*h, f, 1.0f);
			m_pt[6] = v4(-f*w, -f*h, f, 1.0f);
			m_pt[7] = v4(-f*w, +f*h, f, 1.0f);

			obj->m_model = ModelGenerator<>::Boxes(p.m_rdr, 1, m_pt, m_axis.O2W(), 0, 0, m_tex.Material());
			obj->m_model->m_name = obj->TypeAndName();
		}
	};

	// ELdrObject::FrustumFA
	template <> struct ObjectCreator<ELdrObject::FrustumFA> :IObjectCreator
	{
		creation::Textured m_tex;
		creation::MainAxis m_axis;
		pr::v4 m_pt[8];
		float m_fovY, m_aspect;
		float m_near, m_far;

		ObjectCreator(ParseParams& p)
			:IObjectCreator(p)
			,m_tex()
			,m_axis()
			,m_pt()
			,m_fovY(maths::tau_by_8f)
			,m_aspect(1.0f)
			,m_near(0.0f)
			,m_far(1.0f)
		{}
		bool ParseKeyword(EKeyword kw) override
		{
			return
				m_axis.ParseKeyword(p, kw) ||
				m_tex.ParseKeyword(p, kw) ||
				IObjectCreator::ParseKeyword(kw);
		}
		void Parse() override
		{
			p.m_reader.Real(m_fovY);
			p.m_reader.Real(m_aspect);
			p.m_reader.Real(m_near);
			p.m_reader.Real(m_far);
		}
		void CreateModel(LdrObject* obj) override
		{
			// Construct pointed down +z, then rotate the points based on axis id
			float h = Tan(DegreesToRadians(m_fovY * 0.5f));
			float w = m_aspect * h;
			float n = m_near, f = m_far;
			m_pt[0] = v4(-n*w, -n*h, n, 1.0f);
			m_pt[1] = v4(+n*w, -n*h, n, 1.0f);
			m_pt[2] = v4(-n*w, +n*h, n, 1.0f);
			m_pt[3] = v4(+n*w, +n*h, n, 1.0f);
			m_pt[4] = v4(-f*w, -f*h, f, 1.0f);
			m_pt[5] = v4(+f*w, -f*h, f, 1.0f);
			m_pt[6] = v4(-f*w, +f*h, f, 1.0f);
			m_pt[7] = v4(+f*w, +f*h, f, 1.0f);

			obj->m_model = ModelGenerator<>::Boxes(p.m_rdr, 1, m_pt, m_axis.O2W(), 0, 0, m_tex.Material());
			obj->m_model->m_name = obj->TypeAndName();
		}
	};

	// ELdrObject::Sphere
	template <> struct ObjectCreator<ELdrObject::Sphere> :IObjectCreator
	{
		creation::Textured m_tex;
		v4 m_dim;
		int m_facets;

		ObjectCreator(ParseParams& p)
			:IObjectCreator(p)
			,m_tex()
			,m_dim()
			,m_facets(3)
		{}
		bool ParseKeyword(EKeyword kw) override
		{
			switch (kw)
			{
			default:
				{
					return
						m_tex.ParseKeyword(p, kw) ||
						IObjectCreator::ParseKeyword(kw);
				}
			case EKeyword::Facets:
				{
					p.m_reader.IntS(m_facets, 10);
					return true;
				}
			}
		}
		void Parse() override
		{
			p.m_reader.Real(m_dim.x);
			if (p.m_reader.IsValue()) p.m_reader.Real(m_dim.y); else m_dim.y = m_dim.x; 
			if (p.m_reader.IsValue()) p.m_reader.Real(m_dim.z); else m_dim.z = m_dim.y; 
		}
		void CreateModel(LdrObject* obj) override
		{
			obj->m_model = ModelGenerator<>::Geosphere(p.m_rdr, m_dim, m_facets, Colour32White, m_tex.Material());
			obj->m_model->m_name = obj->TypeAndName();
		}
	};

	// ELdrObject::CylinderHR
	template <> struct ObjectCreator<ELdrObject::CylinderHR> :IObjectCreator
	{
		creation::MainAxis m_axis;
		creation::Textured m_tex;
		v4 m_dim; // x,y = radius, z = height
		v2 m_scale;
		int m_layers;
		int m_wedges;

		ObjectCreator(ParseParams& p)
			:IObjectCreator(p)
			,m_axis()
			,m_tex()
			,m_dim()
			,m_scale(v2One)
			,m_layers(1)
			,m_wedges(20)
		{}
		bool ParseKeyword(EKeyword kw) override
		{
			switch (kw)
			{
			default:
				{
					return
						m_axis.ParseKeyword(p, kw) ||
						m_tex.ParseKeyword(p, kw) ||
						IObjectCreator::ParseKeyword(kw);
				}
			case EKeyword::Facets:
				{
					int facets[2];
					p.m_reader.IntS(facets, 2, 10);
					m_layers = facets[0];
					m_wedges = facets[1];
					return true;
				}
			case EKeyword::Scale:
				{
					p.m_reader.Vector2S(m_scale);
					return true;
				}
			}
		}
		void Parse() override
		{
			p.m_reader.Real(m_dim.z);
			p.m_reader.Real(m_dim.x);
			if (p.m_reader.IsValue()) p.m_reader.Real(m_dim.y); else m_dim.y = m_dim.x;
		}
		void CreateModel(LdrObject* obj) override
		{
			// Create the model
			obj->m_model = ModelGenerator<>::Cylinder(p.m_rdr, m_dim.x, m_dim.y, m_dim.z, m_scale.x, m_scale.y, m_wedges, m_layers, 1, &pr::Colour32White, m_axis.O2WPtr(), m_tex.Material());
			obj->m_model->m_name = obj->TypeAndName();
		}
	};

	// ELdrObject::ConeHA
	template <> struct ObjectCreator<ELdrObject::ConeHA> :IObjectCreator
	{
		creation::MainAxis m_axis;
		creation::Textured m_tex;
		v4 m_dim; // x,y = radius, z = height
		v2 m_scale;
		int m_layers;
		int m_wedges;

		ObjectCreator(ParseParams& p)
			:IObjectCreator(p)
			,m_axis()
			,m_tex()
			,m_dim()
			,m_scale(v2One)
			,m_layers(1)
			,m_wedges(20)
		{}
		bool ParseKeyword(EKeyword kw) override
		{
			switch (kw)
			{
			default:
				{
					return
						m_axis.ParseKeyword(p, kw) ||
						m_tex.ParseKeyword(p, kw) ||
						IObjectCreator::ParseKeyword(kw);
				}
			case EKeyword::Facets:
				{
					int facets[2];
					p.m_reader.IntS(facets, 2, 10);
					m_layers = facets[0];
					m_wedges = facets[1];
					return true;
				}
			case EKeyword::Scale:
				{
					p.m_reader.Vector2S(m_scale);
					return true;
				}
			}
		}
		void Parse() override
		{
			float h0, h1, a;
			p.m_reader.Real(h0);
			p.m_reader.Real(h1);
			p.m_reader.Real(a);

			a = DegreesToRadians(a);

			m_dim.z = h1 - h0;
			m_dim.x = h0 * Tan(a);
			m_dim.y = h1 * Tan(a);
		}
		void CreateModel(LdrObject* obj) override
		{
			// Create the model
			obj->m_model = ModelGenerator<>::Cylinder(p.m_rdr, m_dim.x, m_dim.y, m_dim.z, m_scale.x, m_scale.y, m_wedges, m_layers, 1, &pr::Colour32White, m_axis.O2WPtr(), m_tex.Material());
			obj->m_model->m_name = obj->TypeAndName();
		}
	};

	// ELdrObject::Tube
	template <> struct ObjectCreator<ELdrObject::Tube> :IObjectCreator
	{
		enum ECSType
		{
			Invalid,
			Round,
			Square,
			CrossSection,
		};

		ECSType m_type;         // Cross section type
		pr::vector<v2> m_cs;    // 2d cross section
		float m_radx, m_rady;   // X,Y radii for implicit cross sections
		int m_cs_facets;        // The number of divisions for Round cross sections
		std::optional<bool> m_per_vert_colour; // Colour per vertex
		bool m_closed;          // True if the tube end caps should be filled in
		bool m_cs_smooth;       // True if outward normals for the tube are smoothed
		bool m_smooth;          // True if the extrusion path is smooth

		ObjectCreator(ParseParams& p)
			:IObjectCreator(p)
			,m_type(Invalid)
			,m_cs()
			,m_radx()
			,m_rady()
			,m_cs_facets(20)
			,m_per_vert_colour()
			,m_closed(false)
			,m_cs_smooth(false)
			,m_smooth()
		{}
		bool ParseKeyword(EKeyword kw)
		{
			switch (kw)
			{
			default:
				{
					return IObjectCreator::ParseKeyword(kw);
				}
			case EKeyword::Style:
				{
					// Expect *Style { cross_section_type <data> }
					p.m_reader.SectionStart();

					// Determine the cross section type
					string32 type;
					p.m_reader.Identifier(type);
					m_type =
						str::EqualI(type, "Round") ? ECSType::Round :
						str::EqualI(type, "Square") ? ECSType::Square :
						str::EqualI(type, "CrossSection") ? ECSType::CrossSection :
						ECSType::Invalid;

					// Create the cross section profile based on the style
					switch (m_type)
					{
					default:
						{
							p.ReportError(EResult::UnknownValue, FmtS("Cross Section type %s is not supported", type.c_str()));
							return false;
						}
					case ECSType::Round:
						{
							// Elliptical cross section, expect 1 or 2 radii to follow
							p.m_reader.Real(m_radx);
							if (p.m_reader.IsValue()) p.m_reader.Real(m_rady); else m_rady = m_radx;
							m_cs_smooth = true;
							break;
						}
					case ECSType::Square:
						{
							// Square cross section, expect 1 or 2 radii to follow
							p.m_reader.Real(m_radx);
							if (p.m_reader.IsValue()) p.m_reader.Real(m_rady); else m_rady = m_radx;
							m_cs_smooth = false;
							break;
						}
					case ECSType::CrossSection:
						{
							// Create the cross section, expect X,Y pairs
							for (; p.m_reader.IsValue();)
							{
								v2 pt;
								p.m_reader.Vector2(pt);
								m_cs.push_back(pt);
							}
							break;
						}
					}

					// Optional cross section parameters
					for (EKeyword kw0; p.m_reader.NextKeywordH(kw0);)
					{
						switch (kw0)
						{
						case EKeyword::Facets:
							{
								p.m_reader.IntS(m_cs_facets, 10);
								break;
							}
						case EKeyword::Smooth:
							{
								m_cs_smooth = true;
								break;
							}
						}
					}

					p.m_reader.SectionEnd();
					return true;
				}
			case EKeyword::Smooth:
				{
					m_smooth = true;
					return true;
				}
			case EKeyword::Closed:
				{
					m_closed = true;
					return true;
				}
			}
		}
		void Parse() override
		{
			// Parse the extrusion path
			v4 pt; Colour32 col;
			p.m_reader.Vector3(pt, 1.0f);
			if (!m_per_vert_colour)
				m_per_vert_colour = p.m_reader.IsMatch(8, std::wregex(L"[0-9a-fA-F]{8}"));
			if (*m_per_vert_colour)
				p.m_reader.Int(col.argb, 16);
				
			// Ignore degenerates
			if (m_verts.empty() || !FEql(m_verts.back(), pt))
			{
				m_verts.push_back(pt);
				if (*m_per_vert_colour)
					m_colours.push_back(col);
			}
		}
		void CreateModel(LdrObject* obj) override
		{
			// If no cross section or extrusion path is given
			if (m_verts.empty())
			{
				p.ReportError(EResult::Failed, FmtS("Tube object '%s' description incomplete. No extrusion path", obj->TypeAndName().c_str()));
				return;
			}

			// Create the cross section for implicit profiles
			switch (m_type)
			{
			default:
				{
					p.ReportError(EResult::Failed, FmtS("Tube object '%s' description incomplete. No style given.", obj->TypeAndName().c_str()));
					return;
				}
			case ECSType::Round:
				{
					for (auto i = 0; i != m_cs_facets; ++i)
						m_cs.push_back(v2(m_radx * Cos(float(maths::tau) * i / m_cs_facets), m_rady * Sin(float(maths::tau) * i / m_cs_facets)));
					break;
				}
			case ECSType::Square:
				{
					// Create the cross section
					m_cs.push_back(v2(-m_radx, -m_rady));
					m_cs.push_back(v2(+m_radx, -m_rady));
					m_cs.push_back(v2(+m_radx, +m_rady));
					m_cs.push_back(v2(-m_radx, +m_rady));
					break;
				}
			case ECSType::CrossSection:
				{
					if (m_cs.empty())
					{
						p.ReportError(EResult::Failed, FmtS("Tube object '%s' description incomplete", obj->TypeAndName().c_str()));
						return;
					}
					if (pr::geometry::PolygonArea(m_cs.data(), int(m_cs.size())) < 0)
					{
						p.ReportError(EResult::Failed, FmtS("Tube object '%s' cross section has a negative area (winding order is incorrect)", obj->TypeAndName().c_str()));
						return;
					}
					break;
				}
			}

			// Smooth the tube centre line
			if (m_smooth)
			{
				VCont verts;
				std::swap(verts, m_verts);
				pr::Smooth(verts, m_verts);
			}

			// Create the model
			obj->m_model = ModelGenerator<>::Extrude(p.m_rdr, int(m_cs.size()), m_cs.data(), int(m_verts.size()), m_verts.data(), m_closed, m_cs_smooth, int(m_colours.size()), m_colours.data());
			obj->m_model->m_name = obj->TypeAndName();
		}
	};

	// ELdrObject::Mesh
	template <> struct ObjectCreator<ELdrObject::Mesh> :IObjectCreator
	{
		creation::Textured m_tex;
		creation::GenNorms m_gen_norms;

		ObjectCreator(ParseParams& p)
			:IObjectCreator(p)
			,m_tex()
			,m_gen_norms(-1.0f)
		{}
		bool ParseKeyword(EKeyword kw) override
		{
			switch (kw)
			{
			default:
				{
					return
						m_tex.ParseKeyword(p, kw) ||
						m_gen_norms.ParseKeyword(p, kw) ||
						IObjectCreator::ParseKeyword(kw);
				}
			case EKeyword::Verts:
				{
					int r = 1;
					p.m_reader.SectionStart();
					for (pr::v4 v; !p.m_reader.IsSectionEnd(); ++r)
					{
						p.m_reader.Vector3(v, 1.0f);
						m_verts.push_back(v);
						if (r % 500 == 0) p.ReportProgress();
					}
					p.m_reader.SectionEnd();
					return true;
				}
			case EKeyword::Normals:
				{
					int r = 1;
					p.m_reader.SectionStart();
					for (pr::v4 n; !p.m_reader.IsSectionEnd(); ++r)
					{
						p.m_reader.Vector3(n, 0.0f);
						m_normals.push_back(n);
						if (r % 500 == 0) p.ReportProgress();
					}
					p.m_reader.SectionEnd();
					return true;
				}
			case EKeyword::Colours:
				{
					int r = 1;
					p.m_reader.SectionStart();
					for (pr::Colour32 c; !p.m_reader.IsSectionEnd(); ++r)
					{
						p.m_reader.Int(c.argb, 16);
						m_colours.push_back(c);
						if (r % 500 == 0) p.ReportProgress();
					}
					p.m_reader.SectionEnd();
					return true;
				}
			case EKeyword::TexCoords:
				{
					int r = 1;
					p.m_reader.SectionStart();
					for (pr::v2 t; !p.m_reader.IsSectionEnd(); ++r)
					{
						p.m_reader.Vector2(t);
						m_texs.push_back(t);
						if (r % 500 == 0) p.ReportProgress();
					}
					p.m_reader.SectionEnd();
					return true;
				}
			case EKeyword::Lines:
				{
					auto nug = *m_tex.Material();
					nug.m_topo = EPrim::LineList;
					nug.m_geom = EGeom::Vert |
						(!m_colours.empty() ? EGeom::Colr : EGeom::None);
					nug.m_vrange = pr::rdr::Range::Reset();
					nug.m_irange = pr::rdr::Range(m_indices.size(), m_indices.size());
					nug.m_flags = SetBits(nug.m_flags, ENuggetFlag::GeometryHasAlpha, false);

					int r = 1;
					p.m_reader.SectionStart();
					for (pr::uint16 idx[2]; !p.m_reader.IsSectionEnd(); ++r)
					{
						p.m_reader.Int(idx, 2, 10);
						m_indices.push_back(idx[0]);
						m_indices.push_back(idx[1]);

						nug.m_vrange.encompass(idx[0]);
						nug.m_vrange.encompass(idx[1]);
						nug.m_irange.m_end += 2;

						if (r % 500 == 0) p.ReportProgress();
					}
					p.m_reader.SectionEnd();

					m_nuggets.push_back(nug);
					return true;
				}
			case EKeyword::Faces:
				{
					auto nug = *m_tex.Material();
					nug.m_topo = EPrim::TriList;
					nug.m_geom = EGeom::Vert |
						(!m_normals.empty() ? EGeom::Norm : EGeom::None) |
						(!m_colours.empty() ? EGeom::Colr : EGeom::None) |
						(!m_texs.empty()    ? EGeom::Tex0 : EGeom::None);
					nug.m_vrange = pr::rdr::Range::Reset();
					nug.m_irange = pr::rdr::Range(m_indices.size(), m_indices.size());
					nug.m_flags = SetBits(nug.m_flags, ENuggetFlag::GeometryHasAlpha, false);

					int r = 1;
					p.m_reader.SectionStart();
					for (pr::uint16 idx[3]; !p.m_reader.IsSectionEnd(); ++r)
					{
						p.m_reader.Int(idx, 3, 10);
						m_indices.push_back(idx[0]);
						m_indices.push_back(idx[1]);
						m_indices.push_back(idx[2]);

						nug.m_vrange.encompass(idx[0]);
						nug.m_vrange.encompass(idx[1]);
						nug.m_vrange.encompass(idx[2]);
						nug.m_irange.m_end += 3;

						if (r % 500 == 0) p.ReportProgress();
					}
					p.m_reader.SectionEnd();

					m_nuggets.push_back(nug);
					return true;
				}
			case EKeyword::Tetra:
				{
					auto nug = *m_tex.Material();
					nug.m_topo = EPrim::TriList;
					nug.m_geom = EGeom::Vert |
						(!m_normals.empty() ? EGeom::Norm : EGeom::None) |
						(!m_colours.empty() ? EGeom::Colr : EGeom::None) |
						(!m_texs.empty()    ? EGeom::Tex0 : EGeom::None);
					nug.m_vrange = pr::rdr::Range::Reset();
					nug.m_irange = pr::rdr::Range(m_indices.size(), m_indices.size());
					nug.m_flags = SetBits(nug.m_flags, ENuggetFlag::GeometryHasAlpha, false);

					int r = 1;
					p.m_reader.SectionStart();
					for (pr::uint16 idx[4]; !p.m_reader.IsSectionEnd(); ++r)
					{
						p.m_reader.Int(idx, 4, 10);
						m_indices.push_back(idx[0]);
						m_indices.push_back(idx[1]);
						m_indices.push_back(idx[2]);
						m_indices.push_back(idx[0]);
						m_indices.push_back(idx[2]);
						m_indices.push_back(idx[3]);
						m_indices.push_back(idx[0]);
						m_indices.push_back(idx[3]);
						m_indices.push_back(idx[1]);
						m_indices.push_back(idx[3]);
						m_indices.push_back(idx[2]);
						m_indices.push_back(idx[1]);

						nug.m_vrange.encompass(idx[0]);
						nug.m_vrange.encompass(idx[1]);
						nug.m_vrange.encompass(idx[2]);
						nug.m_vrange.encompass(idx[3]);
						nug.m_irange.m_end += 12;

						if (r % 500 == 0) p.ReportProgress();
					}
					p.m_reader.SectionEnd();

					m_nuggets.push_back(nug);
					return true;
				}
			}
		}
		void Parse() override
		{
			// All fields are child keywords
			p.ReportError(EResult::UnknownValue, "Mesh object description invalid");
		}
		void CreateModel(LdrObject* obj) override
		{
			// Validate
			if (m_indices.empty() || m_verts.empty())
			{
				p.ReportError(EResult::Failed, "Mesh object description incomplete");
				return;
			}
			if (!m_colours.empty() && m_colours.size() != m_verts.size())
			{
				p.ReportError(EResult::SyntaxError, FmtS("Mesh objects with colours require one colour per vertex. %d required, %d given.", int(m_verts.size()), int(m_colours.size())));
				return;
			}
			if (!m_normals.empty() && m_normals.size() != m_verts.size())
			{
				p.ReportError(EResult::SyntaxError, FmtS("Mesh objects with normals require one normal per vertex. %d required, %d given.", int(m_verts.size()), int(m_normals.size())));
				return;
			}
			if (!m_texs.empty() && m_texs.size() != m_verts.size())
			{
				p.ReportError(EResult::SyntaxError, FmtS("Mesh objects with texture coordinates require one coordinate per vertex. %d required, %d given.", int(m_verts.size()), int(m_normals.size())));
				return;
			}
			for (auto& nug : m_nuggets)
			{
				// Check the index range is valid
				if (nug.m_vrange.m_beg < 0 || nug.m_vrange.m_end > m_verts.size())
				{
					p.ReportError(EResult::SyntaxError, FmtS("Mesh object with face, line, or tetra section contains indices out of range (section index: %d).", int(&nug - &m_nuggets[0])));
					return;
				}

				// Set the nugget 'has_alpha' value now we know the indices are valid
				if (!m_colours.empty())
				{
					for (auto i = nug.m_irange.begin(); i != nug.m_irange.end(); ++i)
					{
						if (!HasAlpha(m_colours[m_indices[i]])) continue;
						nug.m_flags = SetBits(nug.m_flags, ENuggetFlag::GeometryHasAlpha, true);
						break;
					}
				}
			}

			// Generate normals if needed
			m_gen_norms.Generate(p);

			// Create the model
			auto cdata = MeshCreationData()
				.verts  (m_verts  .data(), int(m_verts  .size()))
				.indices(m_indices.data(), int(m_indices.size()))
				.nuggets(m_nuggets.data(), int(m_nuggets.size()))
				.colours(m_colours.data(), int(m_colours.size()))
				.normals(m_normals.data(), int(m_normals.size()))
				.tex    (m_texs   .data(), int(m_texs   .size()));
			obj->m_model = ModelGenerator<>::Mesh(p.m_rdr, cdata);
			obj->m_model->m_name = obj->TypeAndName();
		}
	};

	// ELdrObject::ConvexHull
	template <> struct ObjectCreator<ELdrObject::ConvexHull> :IObjectCreator
	{
		creation::Textured m_tex;
		creation::GenNorms m_gen_norms;

		ObjectCreator(ParseParams& p)
			:IObjectCreator(p)
			,m_tex()
			,m_gen_norms(0.0f)
		{}
		bool ParseKeyword(EKeyword kw) override
		{
			switch (kw)
			{
			default:
				{
					return
						m_tex.ParseKeyword(p, kw) ||
						m_gen_norms.ParseKeyword(p, kw) ||
						IObjectCreator::ParseKeyword(kw);
				}
			case EKeyword::Verts:
				{
					int r = 1;
					p.m_reader.SectionStart();
					for (pr::v4 v; !p.m_reader.IsSectionEnd(); ++r)
					{
						p.m_reader.Vector3(v, 1.0f);
						m_verts.push_back(v);
						if (r % 500 == 0) p.ReportProgress();
					}
					p.m_reader.SectionEnd();
					return true;
				}
			}
		}
		void Parse() override
		{
			// All fields are child keywords
			p.ReportError(EResult::UnknownValue, "Convex hull object description invalid");
		}
		void CreateModel(LdrObject* obj) override
		{
			// Validate
			if (m_verts.size() < 2)
			{
				p.ReportError(EResult::Failed, "Convex hull object description incomplete. At least 2 vertices required");
				return;
			}

			// Allocate space for the face indices
			m_indices.resize(6 * (m_verts.size() - 2));

			// Find the convex hull
			size_t num_verts = 0, num_faces = 0;
			pr::ConvexHull(m_verts, m_verts.size(), &m_indices[0], &m_indices[0] + m_indices.size(), num_verts, num_faces);
			m_verts.resize(num_verts);
			m_indices.resize(3 * num_faces);

			// Create a nugget for the hull
			auto nug = *m_tex.Material();
			nug.m_topo = EPrim::TriList;
			nug.m_geom = EGeom::Vert;
			m_nuggets.push_back(nug);

			// Generate normals if needed
			m_gen_norms.Generate(p);

			// Create the model
			auto cdata = MeshCreationData()
				.verts  (m_verts  .data(), int(m_verts  .size()))
				.indices(m_indices.data(), int(m_indices.size()))
				.nuggets(m_nuggets.data(), int(m_nuggets.size()))
				.colours(m_colours.data(), int(m_colours.size()))
				.normals(m_normals.data(), int(m_normals.size()))
				.tex    (m_texs   .data(), int(m_texs   .size()));
			obj->m_model = ModelGenerator<>::Mesh(p.m_rdr, cdata);
			obj->m_model->m_name = obj->TypeAndName();
		}
	};

	// ELdrObject::Chart
	template <> struct ObjectCreator<ELdrObject::Chart> :IObjectCreator
	{
		using Column = pr::vector<float>;
		using Table  = pr::vector<Column>;

		creation::MainAxis m_axis;
		Table m_table;
		CCont m_colours;
		int m_xcolumn;
		float m_width;
		std::optional<float> m_x0;
		std::optional<float> m_y0;

		ObjectCreator(ParseParams& p)
			:IObjectCreator(p)
			,m_axis()
			,m_table()
			,m_colours()
			,m_xcolumn(0)
			,m_width(0)
			,m_x0()
			,m_y0()
		{}
		bool ParseKeyword(EKeyword kw) override
		{
			switch (kw)
			{
			default:
				{
					return
						m_axis.ParseKeyword(p, kw) ||
						IObjectCreator::ParseKeyword(kw);
				}
			case EKeyword::YAxis:
				{
					m_x0 = 0.0f;
					p.m_reader.RealS(*m_x0);
					return true;
				}
			case EKeyword::XAxis:
				{
					m_y0 = 0.0f;
					p.m_reader.RealS(*m_y0);
					return true;
				}
			case EKeyword::XColumn:
				{
					p.m_reader.IntS(m_xcolumn, 10);
					return true;
				}
			case EKeyword::Width:
				{
					p.m_reader.RealS(m_width);
					return true;
				}
			case EKeyword::Colours:
				{
					p.m_reader.SectionStart();
					for (;!p.m_reader.IsSectionEnd();)
					{
						Colour32 col;
						p.m_reader.Int(col.argb, 16);
						m_colours.push_back(col);
					}
					p.m_reader.SectionEnd();
					return true;
				}
			}
		}
		void Parse() override
		{
			// Read up to the first non-delimiter. This is the start of the CSV data
			if (!p.m_reader.IsSectionEnd())
			{
				m_table.reserve(10);

				// Create an adapter that makes the script reader look like a std::stream
				struct StreamWrapper
				{
					Reader&      m_reader; // The reader to adapt
					std::wstring m_delims; // Preserve the delimiters

					StreamWrapper(Reader& reader)
						:m_reader(reader)
						,m_delims(reader.Delimiters())
					{
						// Change the delimiters to suit CSV data
						m_reader.Delimiters(L"");
					}
					~StreamWrapper()
					{
						m_reader.Delimiters(m_delims.c_str());
					}
					StreamWrapper(StreamWrapper const&) = delete;
					StreamWrapper& operator =(StreamWrapper const&) = delete;
					bool good() const
					{
						return !eof() && !bad();
					}
					bool eof() const
					{
						return m_reader.IsKeyword() || m_reader.IsSectionEnd();
					}
					bool bad() const
					{
						return m_reader.IsSourceEnd();
					}
					wchar_t peek()
					{
						return *m_reader.Source();
					}
					wchar_t get()
					{
						auto ch = peek();
						++m_reader.Source();
						return ch;
					}
				};
				StreamWrapper wrap(p.m_reader);

				// Read CSV data up to the section close
				pr::csv::Loc loc;
				std::vector<float> values;
				for (pr::csv::Row row; pr::csv::Read(wrap, row, loc); row.resize(0), values.resize(0))
				{
					// Trim trailing empty values and empty rows
					if (row.size() == 1 && pr::str::Trim(row[0], pr::str::IsWhiteSpace<wchar_t>, false, true).empty())
						row.pop_back();
					if (!row.empty() && pr::str::Trim(row.back(), pr::str::IsWhiteSpace<wchar_t>, false, true).empty())
						row.pop_back();
					if (row.empty())
						continue;

					// Convert the row to values
					bool skip_row = false;
					for (auto& item : row)
					{
						float value;
						if (!pr::str::ExtractRealC(value, item.c_str())) { skip_row = true; break; }
						values.push_back(value);
					}
					if (skip_row) continue;

					// Make sure 'm_table' and 'values' have the same length
					auto width = std::max(m_table.size(), values.size());
					m_table.resize(width);
					values.resize(width);

					// Add the row of values to the table
					for (int i = 0, iend = int(values.size()); i != iend; ++i)
						m_table[i].push_back(values[i]);
				}
			}
		}
		void CreateModel(LdrObject* obj) override
		{
			using namespace pr::rdr;

			// Validate
			if (m_table.size() == 0 || m_table[0].size() < 2)
			{
				// No data
				return;
			}
			if (m_xcolumn < -2 || m_xcolumn >= int(m_table.size()))
			{
				p.ReportError(EResult::Failed, FmtS("Chart object '%s', X axis column does not exist", obj->TypeAndName().c_str()));
				return;
			}

			// Get the rotation for axis id
			auto rot = m_axis.O2W().rot;

			// Default colours to use for each column
			Colour32 const colours[] =
			{
				0xFF0000FF, 0xFF00FF00, 0xFFFF0000,
				0xFF0000A0, 0xFF00A000, 0xFFA00000,
				0xFF000080, 0xFF008000, 0xFF800000,
				0xFF00FFFF, 0xFFFFFF00, 0xFFFF00FF,
				0xFF00A0A0, 0xFFA0A000, 0xFFA000A0,
				0xFF008080, 0xFF808000, 0xFF800080,
			};
			int cidx = 0;

			auto& verts = p.m_cache.m_point;
			auto& lines = p.m_cache.m_index;
			auto& colrs = p.m_cache.m_color;
			auto& nugts = p.m_cache.m_nugts;

			// Record the range of data in the chart
			auto xrange = pr::Range<float>::Reset();
			auto yrange = pr::Range<float>::Reset();

			// Create each column as a line strip nugget
			for (int c = 0, cend = int(m_table.size()); c != cend; ++c)
			{
				auto& col = m_table[c];

				// Don't plot the x axis values
				if (c == m_xcolumn)
					continue;

				// Find the verts/indices range for this nugget
				pr::rdr::Range vrange(verts.size(), verts.size() + col.size());
				pr::rdr::Range irange(lines.size(), lines.size() + col.size());

				// Set a colour for this column
				auto colour = cidx < int(m_colours.size()) ? m_colours[cidx] : colours[cidx % _countof(colours)];
				++cidx;

				// Add a line strip for this column
				auto ibase = verts.size();
				for (int i = 0, iend = int(col.size()); i != iend; ++i)
				{
					auto x = m_xcolumn == -1 ? i : m_table[m_xcolumn][i];
					auto y = col[i];

					Encompass(xrange, x);
					Encompass(yrange, y);

					v4 vert(x, y, 0.0f, 1.0f);
					verts.push_back(rot * vert);
					lines.push_back(static_cast<pr::uint16>(ibase + i));
					colrs.push_back(colour);
				}

				// Create a nugget for the line strip
				NuggetProps nug(EPrim::LineStrip, EGeom::Vert|EGeom::Colr, nullptr, vrange, irange);
				if (m_width != 0.0f)
				{
					// Use thick lines
					auto shdr = ThickLineShaderLL(p.m_rdr, m_width);
					nug.m_smap[ERenderStep::ForwardRender].m_gs = shdr;
				}
				nugts.push_back(nug);
			}

			// Add axes
			{
				// Find the verts/indices range for this nugget
				pr::rdr::Range vrange(verts.size(), verts.size() + 4);
				pr::rdr::Range irange(lines.size(), lines.size() + 4);

				// Draw the X/Y axis through 0,0 if near by, otherwise around the bounds of the data
				if (!m_x0) m_x0 = xrange.m_beg > 0 + 2*xrange.size() ? xrange.m_beg : xrange.m_end < 0 - 2*xrange.size() ? xrange.m_end : 0.0f;
				if (!m_y0) m_y0 = yrange.m_beg > 0 + 2*yrange.size() ? yrange.m_beg : yrange.m_end < 0 - 2*yrange.size() ? yrange.m_end : 0.0f;

				// X/Y Axis
				auto ibase = verts.size();
				verts.push_back(rot * v4(std::min(*m_x0, xrange.m_beg - 0.05f * xrange.size()), *m_y0, 0.0f, 1.0f));
				verts.push_back(rot * v4(std::max(*m_x0, xrange.m_end + 0.05f * xrange.size()), *m_y0, 0.0f, 1.0f));
				verts.push_back(rot * v4(*m_x0, std::min(*m_y0, yrange.m_beg - 0.05f * yrange.size()), 0.0f, 1.0f));
				verts.push_back(rot * v4(*m_x0, std::max(*m_y0, yrange.m_end + 0.05f * yrange.size()), 0.0f, 1.0f));

				lines.push_back(static_cast<pr::uint16>(ibase + 0));
				lines.push_back(static_cast<pr::uint16>(ibase + 1));
				lines.push_back(static_cast<pr::uint16>(ibase + 2));
				lines.push_back(static_cast<pr::uint16>(ibase + 3));

				// Set a colour for the axes
				colrs.push_back(0xFF000000);
				colrs.push_back(0xFF000000);
				colrs.push_back(0xFF000000);
				colrs.push_back(0xFF000000);

				// Create a nugget for the axes
				NuggetProps nug(EPrim::LineList, EGeom::Vert|EGeom::Colr, nullptr, vrange, irange);
				nugts.push_back(nug);
			}

			// Create the model
			auto cdata = MeshCreationData()
				.verts  (verts.data(), int(verts.size()))
				.indices(lines.data(), int(lines.size()))
				.colours(colrs.data(), int(colrs.size()))
				.nuggets(nugts.data(), int(nugts.size()));
			obj->m_model = ModelGenerator<>::Mesh(p.m_rdr, cdata);
			obj->m_model->m_name = obj->TypeAndName();
		}
	};

	// ELdrObject::Model
	template <> struct ObjectCreator<ELdrObject::Model> :IObjectCreator
	{
		std::filesystem::path m_filepath;
		m4x4 m_bake;
		int m_part;
		creation::GenNorms m_gen_norms;

		ObjectCreator(ParseParams& p)
			:IObjectCreator(p)
			,m_filepath()
			,m_bake(m4x4Identity)
			,m_gen_norms()
		{}
		bool ParseKeyword(EKeyword kw) override
		{
			switch (kw)
			{
			default:
				{
					return
						m_gen_norms.ParseKeyword(p, kw) ||
						IObjectCreator::ParseKeyword(kw);
				}
			case EKeyword::Part:
				{
					p.m_reader.IntS(m_part, 10);
					return true;
				}
			case EKeyword::BakeTransform:
				{
					p.m_reader.TransformS(m_bake);
					return true;
				}
			}
		}
		void Parse() override
		{
			std::wstring filepath;
			p.m_reader.String(filepath);
			m_filepath = filepath;
		}
		void CreateModel(LdrObject* obj) override
		{
			using namespace pr::rdr;
			using namespace pr::geometry;

			// Validate
			if (m_filepath.empty())
			{
				p.ReportError(EResult::Failed, "Model filepath not given");
				return;
			}

			// Determine the format from the file extension
			auto format = GetModelFormat(m_filepath);
			if (format == EModelFileFormat::Unknown)
			{
				auto msg = Fmt("Model file '%S' is not supported.\nSupported Formats: ", m_filepath.c_str());
				for (auto f : Enum<EModelFileFormat>::Members()) msg.append(Enum<EModelFileFormat>::ToStringA(f)).append(" ");
				p.ReportError(EResult::Failed, msg.c_str());
				return;
			}

			// Ask the include handler to turn the filepath into a stream.
			// Load the stream in binary model. The model loading functions can convert binary to text if needed.
			auto src = p.m_reader.Includes().OpenStreamA(m_filepath, EIncludeFlags::Binary);
			if (!src || !*src)
			{
				p.ReportError(EResult::Failed, FmtS("Failed to open file stream '%s'", m_filepath.c_str()));
				return;
			}

			// Create the model
			obj->m_model = ModelGenerator<>::LoadModel(p.m_rdr, format, *src, nullptr, m_bake != m4x4Identity ? &m_bake : nullptr, m_gen_norms.m_smoothing_angle);
			obj->m_model->m_name = obj->TypeAndName();
		}
	};

	#pragma endregion

	#pragma region Special Objects

	// ELdrObject::DirLight
	template <> struct ObjectCreator<ELdrObject::DirLight> :IObjectCreator
	{
		creation::CastingLight m_cast;
		Light m_light;

		ObjectCreator(ParseParams& p)
			:IObjectCreator(p)
			,m_cast()
			,m_light()
		{
			m_light.m_on = true;
		}
		bool ParseKeyword(EKeyword kw) override
		{
			return
				m_cast.ParseKeyword(p, m_light, kw) ||
				IObjectCreator::ParseKeyword(kw);
		}
		void Parse() override
		{
			p.m_reader.Vector3(m_light.m_direction, 0.0f);
		}
		void CreateModel(LdrObject* obj) override
		{
			// Assign the light data as user data
			obj->m_user_data.get<Light>() = m_light;
		}
	};

	// ELdrObject::PointLight
	template <> struct ObjectCreator<ELdrObject::PointLight> :IObjectCreator
	{
		creation::CastingLight m_cast;
		Light m_light;

		ObjectCreator(ParseParams& p)
			:IObjectCreator(p)
			,m_cast()
			,m_light()
		{
			m_light.m_on = true;
		}
		bool ParseKeyword(EKeyword kw) override
		{
			return
				m_cast.ParseKeyword(p, m_light, kw) ||
				IObjectCreator::ParseKeyword(kw);
		}
		void Parse() override
		{
			p.m_reader.Vector3(m_light.m_position, 1.0f);
		}
		void CreateModel(LdrObject* obj) override
		{
			// Assign the light data as user data
			obj->m_user_data.get<Light>() = m_light;
		}
	};

	// ELdrObject::SpotLight
	template <> struct ObjectCreator<ELdrObject::SpotLight> :IObjectCreator
	{
		creation::CastingLight m_cast;
		Light m_light;

		ObjectCreator(ParseParams& p)
			:IObjectCreator(p)
			,m_cast()
			,m_light()
		{}
		bool ParseKeyword(EKeyword kw) override
		{
			return
				m_cast.ParseKeyword(p, m_light, kw) ||
				IObjectCreator::ParseKeyword(kw);
		}
		void Parse() override
		{
			p.m_reader.Vector3(m_light.m_position, 1.0f);
			p.m_reader.Vector3(m_light.m_direction, 0.0f);
			p.m_reader.Real(m_light.m_inner_angle); // actually in degrees
			p.m_reader.Real(m_light.m_outer_angle); // actually in degrees
		}
		void CreateModel(LdrObject* obj) override
		{
			// Assign the light data as user data
			obj->m_user_data.get<Light>() = m_light;
		}
	};

	// ELdrObject::Group
	template <> struct ObjectCreator<ELdrObject::Group> :IObjectCreator
	{
		ObjectCreator(ParseParams& p)
			:IObjectCreator(p)
		{}
		void CreateModel(LdrObject* obj) override
		{
			// Object modifiers applied to groups are applied recursively to children within the group

			// Apply colour to all children
			if (obj->m_colour_mask != 0)
				obj->Colour(obj->m_base_colour, obj->m_colour_mask, "");

			// Apply wireframe to all children
			if (AllSet(obj->m_flags, ELdrFlags::Wireframe))
				obj->Wireframe(true, "");

			// Apply visibility to all children
			if (AllSet(obj->m_flags, ELdrFlags::Hidden))
				obj->Visible(false, "");
		}
	};

	// ELdrObject::Text
	template <> struct ObjectCreator<ELdrObject::Text> :IObjectCreator
	{
		enum class EType
		{
			Full3D,
			Billboard,
			ScreenSpace,
		};
		using TextFmtCont = pr::vector<TextFormat>;

		wstring256 m_text;
		EType m_type;
		TextFmtCont m_fmt;
		TextLayout m_layout;
		creation::MainAxis m_axis;

		ObjectCreator(ParseParams& p)
			:IObjectCreator(p)
			,m_text()
			,m_type(EType::Full3D)
			,m_fmt()
			,m_layout()
			,m_axis(AxisId::PosZ)
		{}
		bool ParseKeyword(EKeyword kw) override
		{
			switch (kw)
			{
			default:
				{
					return
						m_axis.ParseKeyword(p, kw) ||
						IObjectCreator::ParseKeyword(kw);
				}
			case EKeyword::CString:
				{
					wstring256 text;
					p.m_reader.CStringS(text);
					m_text.append(text);

					// Record the formatting state
					m_fmt.push_back(TextFormat(int(m_text.size() - text.size()), int(text.size()), p.m_font.back()));
					return true;
				}
			case EKeyword::NewLine:
				{
					m_text.append(L"\n");
					return true;
				}
			case EKeyword::ScreenSpace:
				{
					m_type = EType::ScreenSpace;
					return true;
				}
			case EKeyword::Billboard:
				{
					m_type = EType::Billboard;
					return true;
				}
			case EKeyword::BackColour:
				{
					p.m_reader.IntS(m_layout.m_bk_colour.argb, 16);
					return true;
				}
			case EKeyword::Format:
				{
					string32 ident;
					p.m_reader.SectionStart();
					for (; !p.m_reader.IsSectionEnd(); )
					{
						p.m_reader.Identifier(ident);
						pr::transform(ident, [](char c) { return static_cast<char>(tolower(c)); });
						switch (HashI(ident.c_str()))
						{
						case HashI("left"          ): m_layout.m_align_h = DWRITE_TEXT_ALIGNMENT_LEADING; break;
						case HashI("centreh"       ): m_layout.m_align_h = DWRITE_TEXT_ALIGNMENT_CENTER; break;
						case HashI("right"         ): m_layout.m_align_h = DWRITE_TEXT_ALIGNMENT_TRAILING; break;
						case HashI("top"           ): m_layout.m_align_v = DWRITE_PARAGRAPH_ALIGNMENT_NEAR; break;
						case HashI("centrev"       ): m_layout.m_align_v = DWRITE_PARAGRAPH_ALIGNMENT_CENTER; break;
						case HashI("bottom"        ): m_layout.m_align_v = DWRITE_PARAGRAPH_ALIGNMENT_FAR; break;
						case HashI("wrap"          ): m_layout.m_word_wrapping = DWRITE_WORD_WRAPPING_WRAP; break;
						case HashI("nowrap"        ): m_layout.m_word_wrapping = DWRITE_WORD_WRAPPING_NO_WRAP; break;
						case HashI("wholeword"     ): m_layout.m_word_wrapping = DWRITE_WORD_WRAPPING_WHOLE_WORD; break;
						case HashI("character"     ): m_layout.m_word_wrapping = DWRITE_WORD_WRAPPING_CHARACTER; break;
						case HashI("emergencybreak"): m_layout.m_word_wrapping = DWRITE_WORD_WRAPPING_EMERGENCY_BREAK; break;
						}
					}
					p.m_reader.SectionEnd();
					return true;
				}
			case EKeyword::Anchor:
				{
					p.m_reader.Vector2S(m_layout.m_anchor);
					return true;
				}
			case EKeyword::Padding:
				{
					v4 padding;
					p.m_reader.Vector4S(padding);
					m_layout.m_padding.left   = padding.x;
					m_layout.m_padding.top    = padding.y;
					m_layout.m_padding.right  = padding.z;
					m_layout.m_padding.bottom = padding.w;
					return true;
				}
			case EKeyword::Dim:
				{
					p.m_reader.Vector2S(m_layout.m_dim);
					return true;
				}
			}
		}
		void Parse() override
		{
			wstring256 text;
			p.m_reader.String(text);
			m_text.append(text);

			// Record the formatting state
			m_fmt.push_back(TextFormat(int(m_text.size() - text.size()), int(text.size()), p.m_font.back()));
		}
		void CreateModel(LdrObject* obj) override
		{
			// Create a quad containing the text
			obj->m_model = ModelGenerator<>::Text(p.m_rdr, m_text, m_fmt.data(), int(m_fmt.size()), m_layout, m_axis.m_align);
			obj->m_model->m_name = obj->TypeAndName();

			// Create the model
			switch (m_type)
			{
			default:
				{
					throw std::exception(FmtS("Unknown Text mode: %d", m_type));
				}
			// Text is a normal 3D object
			case EType::Full3D:
				{
					break;
				}
			// Position the text quad so that it always faces the camera and has the same size
			case EType::Billboard:
				{
					// Do not include in scene bounds calculations because we're scaling
					// this model at a point that the bounding box calculation can't see.
					obj->m_flags = SetBits(obj->m_flags, ELdrFlags::SceneBoundsExclude, true);

					// Update the rendering 'i2w' transform on add-to-scene
					obj->OnAddToScene += [](LdrObject& ob, rdr::Scene const& scene)
					{
						auto c2w = scene.m_view.CameraToWorld();
						auto w2c = scene.m_view.WorldToCamera();
						auto w = float(scene.m_viewport.Width);
						auto h = float(scene.m_viewport.Height);

						// Create a camera with an aspect ratio that matches the viewport
						auto& m_camera = static_cast<Camera const&>(scene.m_view);
						auto  v_camera = m_camera; v_camera.Aspect(w / h);
						auto fd = m_camera.FocusDist();

						// Get the scaling factors from 'm_camera' to 'v_camera'
						auto viewarea_c = m_camera.ViewArea(fd);
						auto viewarea_v = v_camera.ViewArea(fd);

						// Scale the X,Y coords in camera space
						auto pt_cs = w2c * ob.m_i2w.pos;
						pt_cs.x *= viewarea_v.x / viewarea_c.x;
						pt_cs.y *= viewarea_v.y / viewarea_c.y;
						auto pt_ws = c2w * pt_cs;

						// Scale the instance so that it covers 'dim' pixels on-screen
						auto sz_z = abs(pt_cs.z) / m_camera.FocusDist();
						auto sz_x = (viewarea_v.x / w) * sz_z;
						auto sz_y = (viewarea_v.y / h) * sz_z;
						ob.m_i2w = m4x4(c2w.rot, pt_ws) * m4x4::Scale(sz_x, sz_y, 1.0f, v4Origin);
						ob.m_c2s = v_camera.CameraToScreen();
					};
					break;
				}
			// Position the text quad in screen space.
			case EType::ScreenSpace:
				{
					// Scale up the view port to reduce floating point precision noise.
					enum { ViewPortSize = 1024 };

					// Do not include in scene bounds calculations because we're scaling
					// this model at a point that the bounding box calculation can't see.
					obj->m_flags = SetBits(obj->m_flags, ELdrFlags::SceneBoundsExclude, true);

					// Screen space uses a standard normalised orthographic projection
					obj->m_c2s = m4x4::ProjectionOrthographic(float(ViewPortSize), float(ViewPortSize), -0.01f, 1, true);

					// Update the rendering 'i2w' transform on add-to-scene.
					obj->OnAddToScene += [](LdrObject& ob, rdr::Scene const& scene)
					{
						// The 'ob.m_i2w' is a normalised screen space position
						// (-1,-1,-0) is the lower left corner on the near plane,
						// (+1,+1,-1) is the upper right corner on the far plane.
						auto w = float(scene.m_viewport.Width);
						auto h = float(scene.m_viewport.Height);
						auto c2w = scene.m_view.CameraToWorld();

						// Scale the object from physical pixels to normalised screen space
						auto scale = m4x4::Scale(ViewPortSize/w, ViewPortSize/h, 1, v4Origin);

						// Reverse 'pos.z' so positive values can be used
						ob.m_i2w.pos.x *= 0.5f*ViewPortSize;
						ob.m_i2w.pos.y *= 0.5f*ViewPortSize;

						// Convert 'i2w', which is being interpreted as 'i2c', into an actual 'i2w'
						ob.m_i2w = c2w * ob.m_i2w * scale;
					};
					break;
				}
			}
		}
	};

	// ELdrObject::Instance
	template <> struct ObjectCreator<ELdrObject::Instance> :IObjectCreator
	{
		ObjectCreator(ParseParams& p)
			:IObjectCreator(p)
		{}
		void CreateModel(LdrObject* obj) override
		{
			// Locate the model that this is an instance of
			auto model_key = pr::hash::Hash(obj->m_name.c_str());
			auto mdl = p.m_models.find(model_key);
			if (mdl == p.m_models.end())
			{
				p.ReportError(EResult::UnknownValue, "Instance not found");
				return;
			}
			obj->m_model = mdl->second;
		}
	};

	#pragma endregion

	// Parse an ldr object of type 'ShapeType'
	template <ELdrObject ShapeType> void Parse(ParseParams& p)
	{
		// Notes:
		//  - Not using an output iterator style callback because model
		//    instancing relies on the map from object to model.

		// Read the object attributes: name, colour, instance
		auto attr = ParseAttributes(p, ShapeType);
		auto obj  = LdrObjectPtr(new LdrObject(attr, p.m_parent, p.m_context_id), true);

		// Push a font onto the font stack, so that fonts are scoped to object declarations
		auto font_scope = CreateScope(
			[&]{ p.m_font.push_back(p.m_font.back()); },
			[&]{ p.m_font.pop_back(); });

		// Create an object creator for the given type
		ObjectCreator<ShapeType> creator(p);

		// Read the description of the model
		p.m_reader.SectionStart();
		for (;!p.m_cancel && !p.m_reader.IsSectionEnd();)
		{
			if (p.m_reader.IsKeyword())
			{
				// The next script token is a keyword
				auto kw = p.m_reader.NextKeywordH<EKeyword>();

				// Let the object creator have the first go with the keyword
				if (creator.ParseKeyword(kw))
					continue;

				// Is the keyword a common object property
				if (ParseProperties(p, kw, obj.get()))
					continue;

				// Recursively parse child objects
				ParseParams pp(p, obj->m_child, HashValue(kw), obj.get());
				if (ParseLdrObject(pp))
					continue;

				// Unknown token
				p.ReportError(EResult::UnknownToken);
				continue;
			}
			else
			{
				// Not a keyword, let the object creator interpret it
				creator.Parse();
			}
		}
		p.m_reader.SectionEnd();

		// Create the model
		creator.CreateModel(obj.get());

		// Add the model and instance to the containers
		p.m_models[pr::hash::Hash(obj->m_name.c_str())] = obj->m_model;
		p.m_objects.push_back(obj);

		// Reset the memory pool for the next object
		p.m_cache.Reset();

		// Report progress
		p.ReportProgress();
	}

	// Reads a single ldr object from a script adding object (+ children) to 'p.m_objects'.
	// Returns true if an object was read or false if the next keyword is unrecognised
	bool ParseLdrObject(ParseParams& p)
	{
		// Save the current number of objects
		auto object_count = p.m_objects.size();

		// Parse the object
		auto kw = ELdrObject(p.m_keyword);
		switch (kw)
		{
		default: return false;
		case ELdrObject::Point:      Parse<ELdrObject::Point     >(p); break;
		case ELdrObject::Line:       Parse<ELdrObject::Line      >(p); break;
		case ELdrObject::LineD:      Parse<ELdrObject::LineD     >(p); break;
		case ELdrObject::LineStrip:  Parse<ELdrObject::LineStrip >(p); break;
		case ELdrObject::LineBox:    Parse<ELdrObject::LineBox   >(p); break;
		case ELdrObject::Grid:       Parse<ELdrObject::Grid      >(p); break;
		case ELdrObject::Spline:     Parse<ELdrObject::Spline    >(p); break;
		case ELdrObject::Arrow:      Parse<ELdrObject::Arrow     >(p); break;
		case ELdrObject::Circle:     Parse<ELdrObject::Circle    >(p); break;
		case ELdrObject::Rect:       Parse<ELdrObject::Rect      >(p); break;
		case ELdrObject::Polygon:    Parse<ELdrObject::Polygon   >(p); break;
		case ELdrObject::Pie:        Parse<ELdrObject::Pie       >(p); break;
		case ELdrObject::Matrix3x3:  Parse<ELdrObject::Matrix3x3 >(p); break;
		case ELdrObject::CoordFrame: Parse<ELdrObject::CoordFrame>(p); break;
		case ELdrObject::Triangle:   Parse<ELdrObject::Triangle  >(p); break;
		case ELdrObject::Quad:       Parse<ELdrObject::Quad      >(p); break;
		case ELdrObject::Plane:      Parse<ELdrObject::Plane     >(p); break;
		case ELdrObject::Ribbon:     Parse<ELdrObject::Ribbon    >(p); break;
		case ELdrObject::Box:        Parse<ELdrObject::Box       >(p); break;
		case ELdrObject::Bar:        Parse<ELdrObject::Bar       >(p); break;
		case ELdrObject::BoxList:    Parse<ELdrObject::BoxList   >(p); break;
		case ELdrObject::FrustumWH:  Parse<ELdrObject::FrustumWH >(p); break;
		case ELdrObject::FrustumFA:  Parse<ELdrObject::FrustumFA >(p); break;
		case ELdrObject::Sphere:     Parse<ELdrObject::Sphere    >(p); break;
		case ELdrObject::CylinderHR: Parse<ELdrObject::CylinderHR>(p); break;
		case ELdrObject::ConeHA:     Parse<ELdrObject::ConeHA    >(p); break;
		case ELdrObject::Tube:       Parse<ELdrObject::Tube      >(p); break;
		case ELdrObject::Mesh:       Parse<ELdrObject::Mesh      >(p); break;
		case ELdrObject::ConvexHull: Parse<ELdrObject::ConvexHull>(p); break;
		case ELdrObject::Model:      Parse<ELdrObject::Model     >(p); break;
		case ELdrObject::Chart:      Parse<ELdrObject::Chart     >(p); break;
		case ELdrObject::DirLight:   Parse<ELdrObject::DirLight  >(p); break;
		case ELdrObject::PointLight: Parse<ELdrObject::PointLight>(p); break;
		case ELdrObject::SpotLight:  Parse<ELdrObject::SpotLight >(p); break;
		case ELdrObject::Group:      Parse<ELdrObject::Group     >(p); break;
		case ELdrObject::Text:       Parse<ELdrObject::Text      >(p); break;
		case ELdrObject::Instance:   Parse<ELdrObject::Instance  >(p); break;
		}

		// Apply properties to each object added
		assert("No object added, or objects removed, without Parse error" && p.m_objects.size() > object_count);
		for (auto i = object_count, iend = p.m_objects.size(); i != iend; ++i)
			ApplyObjectState(p.m_objects[i].get());

		return true;
	}

	// Reads all ldr objects from a script returning 'result'
	// 'add_cb' is 'bool function(int object_index, ParseResult& out, Location const& loc)'
	template <typename AddCB>
	void ParseLdrObjects(ParseParams& p, AddCB add_cb)
	{
		// Ldr script is not case sensitive
		p.m_reader.CaseSensitive(false);

		// Loop over keywords in the script
		for (;!p.m_cancel && p.m_reader.NextKeywordH(p.m_keyword);)
		{
			auto kw = (EKeyword)p.m_keyword;
			switch (kw)
			{
			default:
				{
					// Save the current number of objects
					auto object_count = int(p.m_objects.size());

					// Assume the keyword is an object and start parsing
					if (!ParseLdrObject(p))
					{
						p.ReportError(EResult::UnknownToken, Fmt("Expected an object declaration"));
						break;
					}
					assert("Objects removed but 'ParseLdrObject' didn't fail" && int(p.m_objects.size()) > object_count);

					// Call the callback with the freshly minted object.
					add_cb(object_count);
					break;
				}
			case EKeyword::Camera:
				{
					// Camera position description
					ParseCamera(p, p.m_result);
					break;
				}
			case EKeyword::Wireframe:
				{
					p.m_result.m_wireframe = true;
					break;
				}
			case EKeyword::Font:
				{
					ParseFont(p, p.m_font.back());
					break;
				}
			case EKeyword::Lock:
				{
					break;
				}
			case EKeyword::Delimiters:
				{
					break;
				}
			}
		}
	}

	// Parse the ldr script in 'reader' adding the results to 'out'.
	// This function can be called from any thread (main or worker) and may be called concurrently by multiple threads.
	// There is synchronisation in the renderer for creating/allocating models. The calling thread must control the
	// lifetimes of the script reader, the parse output, and the 'store' container it refers to.
	void Parse(Renderer& rdr, pr::script::Reader& reader, ParseResult& out, Guid const& context_id, ParseProgressCB progress_cb)
	{
		// Give initial and final progress updates
		auto start_loc = reader.Location();
		auto exit = pr::CreateScope(
			[&]
			{
				// Give an initial progress update
				if (progress_cb != nullptr)
					progress_cb(context_id, out, start_loc, false);
			},
			[&]
			{
				// Give a final progress update
				if (progress_cb != nullptr)
					progress_cb(context_id, out, start_loc, true);
			});

		// Parse the script
		bool cancel = false;
		ParseParams pp(rdr, reader, out, context_id, progress_cb, cancel);
		ParseLdrObjects(pp, [&](int){});
	}

	// Create an ldr object from creation data.
	LdrObjectPtr Create(Renderer& rdr, ObjectAttributes attr, MeshCreationData const& cdata, pr::Guid const& context_id)
	{
		LdrObjectPtr obj(new LdrObject(attr, nullptr, context_id), true);

		// Create the model
		obj->m_model = ModelGenerator<>::Mesh(rdr, cdata);
		obj->m_model->m_name = obj->TypeAndName();
		return obj;
	}

	// Create an instance of an existing ldr object.
	LdrObjectPtr CreateInstance(LdrObject const* existing)
	{
		ObjectAttributes attr(existing->m_type, existing->m_name.c_str(), existing->m_base_colour);
		LdrObjectPtr obj(new LdrObject(attr, nullptr, existing->m_context_id), true);

		// Use the same model
		obj->m_model = existing->m_model;
		return obj;
	}

	// Create an ldr object using a callback to populate the model data.
	// Objects created by this method will have dynamic usage and are suitable for updating every frame via the 'Edit' function.
	LdrObjectPtr CreateEditCB(Renderer& rdr, ObjectAttributes attr, int vcount, int icount, int ncount, EditObjectCB edit_cb, void* ctx, Guid const& context_id)
	{
		LdrObjectPtr obj(new LdrObject(attr, 0, context_id), true);

		// Create buffers for a dynamic model
		VBufferDesc vbs(vcount, sizeof(Vert), EUsage::Dynamic, ECPUAccess::Write);
		IBufferDesc ibs(icount, sizeof(uint16), DxFormat<uint16>::value, EUsage::Dynamic, ECPUAccess::Write);
		MdlSettings settings(vbs, ibs);

		// Create the model
		obj->m_model = rdr.m_mdl_mgr.CreateModel(settings);
		obj->m_model->m_name = obj->TypeAndName();

		// Create dummy nuggets
		rdr::NuggetProps nug(EPrim::PointList, EGeom::Vert);
		nug.m_range_overlaps = true;
		for (int i = ncount; i-- != 0;)
			obj->m_model->CreateNugget(nug);

		// Initialise it via the callback
		edit_cb(obj->m_model.get(), ctx, rdr);
		return obj;
	}

	// Modify the geometry of an LdrObject
	void Edit(Renderer& rdr, LdrObject* object, EditObjectCB edit_cb, void* ctx)
	{
		edit_cb(object->m_model.get(), ctx, rdr);
		//pr::events::Send(Evt_LdrObjectChg(object));
	}

	// Update 'object' with info from 'reader'. 'flags' describes the properties of 'object' to update
	void Update(Renderer& rdr, LdrObject* object, Reader& reader, EUpdateObject flags)
	{
		// Parsing parameters
		ParseResult result;
		bool cancel = false;
		ParseParams pp(rdr, reader, result, object->m_context_id, nullptr, cancel);
	
		// Parse 'reader' for the new model
		ParseLdrObjects(pp, [&](int object_index)
		{
			// Want the first root level object
			auto& rhs = result.m_objects[object_index];
			if (rhs->m_parent != nullptr)
				return true;

			// Swap the bits we want from 'rhs'
			// Note: we can't swap everything then copy back the bits we want to keep
			// because LdrObject is reference counted and isn't copyable. This is risky
			// though, if new members are added I'm bound to forget to consider them here :-/

			// RdrInstance
			if (AllSet(flags, EUpdateObject::Model))
			{
				std::swap(object->m_model, rhs->m_model);
				std::swap(object->m_sko, rhs->m_sko);
				std::swap(object->m_bsb, rhs->m_bsb);
				std::swap(object->m_dsb, rhs->m_dsb);
				std::swap(object->m_rsb, rhs->m_rsb);
			}
			if (AllSet(flags, EUpdateObject::Transform))
				std::swap(object->m_i2w, rhs->m_i2w);
			if (AllSet(flags, EUpdateObject::Colour))
				std::swap(object->m_colour, rhs->m_colour);

			// LdrObject
			std::swap(object->m_type, rhs->m_type);
			if (AllSet(flags, EUpdateObject::Name))
				std::swap(object->m_name, rhs->m_name);
			if (AllSet(flags, EUpdateObject::Transform))
				std::swap(object->m_o2p, rhs->m_o2p);
			if (AllSet(flags, EUpdateObject::Flags))
				std::swap(object->m_flags, rhs->m_flags);
			if (AllSet(flags, EUpdateObject::Animation))
				std::swap(object->m_anim, rhs->m_anim);
			if (AllSet(flags, EUpdateObject::ColourMask))
				std::swap(object->m_colour_mask, rhs->m_colour_mask);
			if (AllSet(flags, EUpdateObject::Reflectivity))
				std::swap(object->m_env, rhs->m_env);
			if (AllSet(flags, EUpdateObject::Colour))
				std::swap(object->m_base_colour, rhs->m_base_colour);

			// Transfer the child objects
			if (AllSet(flags, EUpdateObject::Children))
			{
				object->RemoveAllChildren();
				while (!rhs->m_child.empty())
				{
					auto child = rhs->RemoveChild(0);
					object->AddChild(child);
				}
			}
			else
				ApplyObjectState(object);

			// Only want one object
			return false;
		});

		//pr::events::Send(Evt_LdrObjectChg(object));
	}

	// Remove all objects from 'objects' that have a context id matching one in 'doomed' and not in 'excluded'
	// If 'doomed' is 0, all are assumed doomed. If 'excluded' is 0, none are assumed excluded
	// 'excluded' is considered after 'doomed' so if any context ids are in both arrays, they will be excluded.
	void Remove(ObjectCont& objects, Guid const* doomed, std::size_t dcount, Guid const* excluded, std::size_t ecount)
	{
		auto incl = std::initializer_list<Guid>(doomed, doomed + dcount);
		auto excl = std::initializer_list<Guid>(excluded, excluded + ecount);
		erase_if_unstable(objects, [=](auto& ob)
		{
			if (doomed   && !contains(incl, ob->m_context_id)) return false; // not in the doomed list
			if (excluded &&  contains(excl, ob->m_context_id)) return false; // saved by exclusion
			return true;
		});
	}

	// Remove 'obj' from 'objects'
	void Remove(ObjectCont& objects, LdrObject* obj)
	{
		erase_first_unstable(objects, [=](auto& ob){ return ob == obj; });
	}

	// LdrObject ***********************************

	#pragma region LdrObject

	#if PR_DBG
	#define PR_LDR_CALLSTACKS 0
	struct LeakedLdrObjects
	{
		std::unordered_set<LdrObject const*> m_ldr_objects;
		std::string m_call_stacks;
		std::mutex m_mutex;
			
		LeakedLdrObjects()
			:m_ldr_objects()
			,m_mutex()
		{}
		~LeakedLdrObjects()
		{
			if (m_ldr_objects.empty()) return;

			size_t const msg_max_len = 1000;
			std::string msg = "Leaked LdrObjects detected:\n";
			for (auto ldr : m_ldr_objects)
			{
				msg.append(ldr->TypeAndName()).append("\n");
				if (msg.size() > msg_max_len)
				{
					msg.resize(msg_max_len - 3);
					msg.append("...");
					break;
				}
			}

			PR_ASSERT(1, m_ldr_objects.empty(), msg.c_str());
		}
		void add(LdrObject const* ldr)
		{
			std::lock_guard<std::mutex> lock(m_mutex);
			m_ldr_objects.insert(ldr);
		}
		void remove(LdrObject const* ldr)
		{
			#if PR_LDR_CALLSTACKS
			#pragma message(PR_LINK "WARNING: ************************************************** PR_LDR_CALLSTACKS enabled")
			m_call_stacks.append(FmtS("[%p] %s\n", ldr, ldr->TypeAndName().c_str()));
			pr::DumpStack([&](auto& sym, auto& file, int line){ m_call_stacks.append(FmtS("%s(%d): %s\n", file.c_str(), line, sym.c_str()));}, 2U, 50U);
			m_call_stacks.append("\n");
			#endif

			std::lock_guard<std::mutex> lock(m_mutex);
			m_ldr_objects.erase(ldr);
		}
	} g_ldr_object_tracker;
	#endif

	LdrObject::LdrObject(ObjectAttributes const& attr, LdrObject* parent, Guid const& context_id)
		:RdrInstance()
		,m_o2p(m4x4Identity)
		,m_type(attr.m_type)
		,m_parent(parent)
		,m_child()
		,m_name(attr.m_name)
		,m_context_id(context_id)
		,m_base_colour(attr.m_colour)
		,m_colour_mask()
		,m_anim()
		,m_bbox_instance()
		,m_screen_space()
		,m_flags(ELdrFlags::None)
		,m_user_data()
	{
		m_i2w = m4x4Identity;
		m_colour = m_base_colour;
		PR_EXPAND(PR_DBG, g_ldr_object_tracker.add(this));
	}
	LdrObject::~LdrObject()
	{
		PR_EXPAND(PR_DBG, g_ldr_object_tracker.remove(this));
	}

	// Return the declaration name of this object
	string32 LdrObject::TypeAndName() const
	{
		return string32(Enum<ELdrObject>::ToStringA(m_type)) + " " + m_name;
	}

	// Recursively add this object and its children to a viewport
	void LdrObject::AddToScene(Scene& scene, float time_s, m4x4 const* p2w)
	{
		// Set the instance to world.
		// Take a copy in case the 'OnAddToScene' event changes it.
		// We want parenting to be unaffected by the event handlers.
		auto i2w = *p2w * m_o2p * m_anim.Step(time_s);
		m_i2w = i2w;
		PR_ASSERT(PR_DBG, FEql(m_i2w.w.w, 1.0f), "Invalid instance transform");

		// Allow the object to change it's transform just before rendering
		OnAddToScene(*this, scene);

		// Add the instance to the scene drawlist
		if (m_model && !AllSet(m_flags, ELdrFlags::Hidden))
		{
			// Could add occlusion culling here...
			scene.AddInstance(*this);
		}

		// Rinse and repeat for all children
		for (auto& child : m_child)
			child->AddToScene(scene, time_s, &i2w);
	}

	// Recursively add this object using 'bbox_model' instead of its
	// actual model, located and scaled to the transform and box of this object
	void LdrObject::AddBBoxToScene(Scene& scene, ModelPtr bbox_model, float time_s, m4x4 const* p2w)
	{
		// Set the instance to world for this object
		auto i2w = *p2w * m_o2p * m_anim.Step(time_s);

		// Add the bbox instance to the scene drawlist
		if (m_model && !AnySet(m_flags, ELdrFlags::Hidden|ELdrFlags::SceneBoundsExclude))
		{
			// Find the object to world for the bbox
			auto o2w = i2w * m4x4::Scale(
				m_model->m_bbox.SizeX() + maths::tiny,
				m_model->m_bbox.SizeY() + maths::tiny,
				m_model->m_bbox.SizeZ() + maths::tiny,
				m_model->m_bbox.Centre());

			m_bbox_instance.m_model = bbox_model;
			m_bbox_instance.m_i2w = o2w;
			scene.AddInstance(m_bbox_instance); // Could add occlusion culling here...
		}

		// Rinse and repeat for all children
		for (auto& child : m_child)
			child->AddBBoxToScene(scene, bbox_model, time_s, &m_i2w);
	}

	// Get the first child object of this object that matches 'name' (see Apply)
	LdrObject* LdrObject::Child(char const* name) const
	{
		LdrObject* obj = nullptr;
		Apply([&](LdrObject* o){ obj = o; return false; }, name);
		return obj;
	}
	LdrObject* LdrObject::Child(int index) const
	{
		if (index < 0 || index >= int(m_child.size())) throw std::exception(FmtS("LdrObject child index (%d) out of range [0,%d)", index, int(m_child.size())));
		return m_child[index].get();
	}

	// Get/Set the object to world transform of this object or the first child object matching 'name' (see Apply)
	m4x4 LdrObject::O2W(char const* name) const
	{
		auto obj = Child(name);
		if (obj == nullptr)
			return m4x4Identity;

		// Combine parent transforms back to the root
		auto o2w = obj->m_o2p;
		for (auto p = obj->m_parent; p != nullptr; p = p->m_parent)
			o2w = p->m_o2p * o2w;

		return o2w;
	}
	void LdrObject::O2W(m4x4 const& o2w, char const* name)
	{
		Apply([&](LdrObject* o)
		{
			o->m_o2p = o->m_parent ? InvertFast(o->m_parent->O2W()) * o2w : o2w;
			assert(FEql(o->m_o2p.w.w, 1.0f) && "Invalid instance transform");
			return true;
		}, name);
	}

	// Get/Set the object to parent transform of this object or child objects matching 'name' (see Apply)
	m4x4 LdrObject::O2P(char const* name) const
	{
		auto obj = Child(name);
		return obj ? obj->m_o2p : m4x4Identity;
	}
	void LdrObject::O2P(m4x4 const& o2p, char const* name)
	{
		Apply([&](LdrObject* o)
		{
			assert(FEql(o2p.w.w, 1.0f) && "Invalid instance transform");
			assert(IsFinite(o2p) && "Invalid instance transform");
			o->m_o2p = o2p;
			return true;
		}, name);
	}

	// Get/Set the visibility of this object or child objects matching 'name' (see Apply)
	bool LdrObject::Visible(char const* name) const
	{
		auto obj = Child(name);
		return obj ? !AllSet(obj->m_flags, ELdrFlags::Hidden) : false;
	}
	void LdrObject::Visible(bool visible, char const* name)
	{
		Flags(ELdrFlags::Hidden, !visible, name);
	}

	// Get/Set the render mode for this object or child objects matching 'name' (see Apply)
	bool LdrObject::Wireframe(char const* name) const
	{
		auto obj = Child(name);
		return obj ? AllSet(obj->m_flags, ELdrFlags::Wireframe) : false;
	}
	void LdrObject::Wireframe(bool wireframe, char const* name)
	{
		Flags(ELdrFlags::Wireframe, wireframe, name);
	}

	// Get/Set screen space rendering mode for this object and all child objects
	bool LdrObject::ScreenSpace() const
	{
		auto obj = Child("");
		return obj ? static_cast<bool>(obj->m_screen_space) : false;
	}
	void LdrObject::ScreenSpace(bool screen_space)
	{
		Apply([=](LdrObject* o)
		{
			if (screen_space)
			{
				enum { ViewPortSize = 2 };

				// Do not include in scene bounds calculations because we're scaling
				// this model at a point that the bounding box calculation can't see.
				o->m_flags = SetBits(o->m_flags, ELdrFlags::SceneBoundsExclude, true);

				// Update the rendering 'i2w' transform on add-to-scene.
				o->m_screen_space = o->OnAddToScene += [](LdrObject& ob, rdr::Scene const& scene)
				{
					// The 'ob.m_i2w' is a normalised screen space position
					// (-1,-1,-0) is the lower left corner on the near plane,
					// (+1,+1,-1) is the upper right corner on the far plane.
					auto w = float(scene.m_viewport.Width);
					auto h = float(scene.m_viewport.Height);
					auto c2w = scene.m_view.CameraToWorld();

					// Screen space uses a standard normalised orthographic projection
					ob.m_c2s = w >= h
						? m4x4::ProjectionOrthographic(float(ViewPortSize) * w / h, float(ViewPortSize), -0.01f, 1.01f, true)
						: m4x4::ProjectionOrthographic(float(ViewPortSize), float(ViewPortSize) * h / w, -0.01f, 1.01f, true);

					// Scale the object to normalised screen space
					auto scale = w >= h
						? m4x4::Scale(0.5f * ViewPortSize * (w/h), 0.5f * ViewPortSize, 1, v4Origin)
						: m4x4::Scale(0.5f * ViewPortSize, 0.5f * ViewPortSize * (h/w), 1, v4Origin);

					// Scale the X,Y position so that positions are still in normalised screen space
					ob.m_i2w.pos.x *= w >= h ? (w/h) : 1.0f;
					ob.m_i2w.pos.y *= w >= h ? 1.0f : (h/w);
			
					// Convert 'i2w', which is being interpreted as 'i2c', into an actual 'i2w'
					ob.m_i2w = c2w * ob.m_i2w * scale;
				};
			}
			else
			{
				o->m_c2s = m4x4Zero;
				o->m_flags = SetBits(o->m_flags, ELdrFlags::SceneBoundsExclude, false);
				o->OnAddToScene -= o->m_screen_space;
			}
			return true;
		}, "");
	}

	// Get/Set meta behaviour flags for this object or child objects matching 'name' (see Apply)
	ELdrFlags LdrObject::Flags(char const* name) const
	{
		// Mainly used to allow non-user objects to be added to a scene
		// and not affect the bounding box of the scene
		auto obj = Child(name);
		return obj ? obj->m_flags : ELdrFlags::None;
	}
	void LdrObject::Flags(ELdrFlags flags, bool state, char const* name)
	{
		Apply([=](LdrObject* o)
		{
			// Apply flag changes
			o->m_flags = SetBits(o->m_flags, flags, state);

			// Hidden
			if (o->m_model != nullptr)
			{
				// Even though Ldraw doesn't add instances that are hidden,
				// set the visibility flags on the nuggets for consistency.
				auto hidden = AllSet(o->m_flags, ELdrFlags::Hidden);
				for (auto& nug : o->m_model->m_nuggets)
					SetBits(nug.m_flags, ENuggetFlag::Hidden, hidden);
			}

			// Wireframe
			if (AllSet(o->m_flags, ELdrFlags::Wireframe))
			{
				o->m_rsb.Set(ERS::FillMode, D3D11_FILL_WIREFRAME);
			}
			else
			{
				o->m_rsb.Clear(ERS::FillMode);
			}

			// No Z Test
			if (AllSet(o->m_flags, ELdrFlags::NoZTest))
			{
				// Don't test against Z, and draw above all objects
				o->m_dsb.Set(rdr::EDS::DepthEnable, FALSE);
				o->m_sko.Group(rdr::ESortGroup::PostAlpha);
			}
			else
			{
				o->m_dsb.Set(rdr::EDS::DepthEnable, TRUE);
				o->m_sko = SKOverride();
			}

			// If NoZWrite
			if (AllSet(o->m_flags, ELdrFlags::NoZWrite))
			{
				// Don't write to Z and draw behind all objects
				o->m_dsb.Set(rdr::EDS::DepthWriteMask, D3D11_DEPTH_WRITE_MASK_ZERO);
				o->m_sko.Group(rdr::ESortGroup::PreOpaques);
			}
			else
			{
				o->m_dsb.Set(rdr::EDS::DepthWriteMask, D3D11_DEPTH_WRITE_MASK_ALL);
				o->m_sko = SKOverride();
			}

			return true;
		}, name);
	}

	// Get/Set the render group for this object or child objects matching 'name' (see Apply)
	rdr::ESortGroup LdrObject::SortGroup(char const* name) const
	{
		auto obj = Child(name);
		return obj ? obj->m_sko.Group() : rdr::ESortGroup::Default;
	}
	void LdrObject::SortGroup(rdr::ESortGroup grp, char const* name)
	{
		Apply([=](LdrObject* o)
		{
			o->m_sko.Group(grp);
			return true;
		}, name);
	}

	// Get/Set the nugget flags for this object or child objects matching 'name' (see Apply)
	rdr::ENuggetFlag LdrObject::NuggetFlags(char const* name, int index) const
	{
		auto obj = Child(name);
		if (obj == nullptr || obj->m_model == nullptr)
			return rdr::ENuggetFlag::None;

		if (index >= static_cast<int>(obj->m_model->m_nuggets.size()))
			throw std::runtime_error("nugget index out of range");

		auto nug = obj->m_model->m_nuggets.begin();
		for (int i = 0; i != index; ++i, ++nug) {}
		return nug->m_flags;
	}
	void LdrObject::NuggetFlags(rdr::ENuggetFlag flags, bool state, char const* name, int index)
	{
		Apply([=](LdrObject* obj)
		{
			if (obj->m_model != nullptr)
			{
				auto nug = obj->m_model->m_nuggets.begin();
				for (int i = 0; i != index; ++i, ++nug) {}
				nug->m_flags = SetBits(nug->m_flags, flags, state);
			}
			return true;
		}, name);
	}

	// Get/Set the nugget flags for this object or child objects matching 'name' (see Apply)
	Colour32 LdrObject::NuggetTint(char const* name, int index) const
	{
		auto obj = Child(name);
		if (obj == nullptr || obj->m_model == nullptr)
			return Colour32White;

		if (index >= static_cast<int>(obj->m_model->m_nuggets.size()))
			throw std::runtime_error("nugget index out of range");

		auto nug = obj->m_model->m_nuggets.begin();
		for (int i = 0; i != index; ++i, ++nug) {}
		return nug->m_tint;
	}
	void LdrObject::NuggetTint(Colour32 tint, char const* name, int index)
	{
		Apply([=](LdrObject* obj)
		{
			if (obj->m_model != nullptr)
			{
				auto nug = obj->m_model->m_nuggets.begin();
				for (int i = 0; i != index; ++i, ++nug) {}
				nug->m_tint = tint;
			}
			return true;
		}, name);
	}

	// Get/Set the colour of this object or child objects matching 'name' (see Apply)
	// For 'Get', the colour of the first object to match 'name' is returned
	// For 'Set', the object base colour is not changed, only the tint colour = tint
	Colour32 LdrObject::Colour(bool base_colour, char const* name) const
	{
		Colour32 col;
		Apply([&](LdrObject const* o)
		{
			col = base_colour ? o->m_base_colour : o->m_colour;
			return false; // stop at the first match
		}, name);
		return col;
	}
	void LdrObject::Colour(Colour32 colour, uint mask, char const* name, EColourOp op, float op_value)
	{
		Apply([=](LdrObject* o)
		{
			switch (op)
			{
			case EColourOp::Overwrite:
				o->m_colour.argb = SetBits(o->m_base_colour.argb, mask, colour.argb);
				break;
			case EColourOp::Add:
				o->m_colour.argb = SetBits(o->m_base_colour.argb, mask, (o->m_base_colour + colour).argb);
				break;
			case EColourOp::Subtract:
				o->m_colour.argb = SetBits(o->m_base_colour.argb, mask, (o->m_base_colour - colour).argb);
				break;
			case EColourOp::Multiply:
				o->m_colour.argb = SetBits(o->m_base_colour.argb, mask, (o->m_base_colour * colour).argb);
				break;
			case EColourOp::Lerp:
				o->m_colour.argb = SetBits(o->m_base_colour.argb, mask, Lerp(o->m_base_colour, colour, op_value).argb);
				break;
			}
			if (o->m_model == nullptr)
				return true;

			auto tint_has_alpha = HasAlpha(o->m_colour);
			for (auto& nug : o->m_model->m_nuggets)
			{
				nug.m_flags = SetBits(nug.m_flags, ENuggetFlag::TintHasAlpha, tint_has_alpha);
				nug.UpdateAlphaStates();
			}

			return true;
		}, name);
	}

	// Restore the colour to the initial colour for this object or child objects matching 'name' (see Apply)
	void LdrObject::ResetColour(char const* name)
	{
		Apply([=](LdrObject* o)
		{
			o->m_colour = o->m_base_colour;
			if (o->m_model == nullptr) return true;

			auto has_alpha = HasAlpha(o->m_colour);
			for (auto& nug : o->m_model->m_nuggets)
			{
				nug.m_flags = SetBits(nug.m_flags, ENuggetFlag::TintHasAlpha, has_alpha);
				nug.UpdateAlphaStates();
			}

			return true;
		}, name);
	}

	// Get/Set the reflectivity of this object or child objects matching 'name' (see Apply)
	float LdrObject::Reflectivity(char const* name) const
	{
		float env;
		Apply([&](LdrObject const* o)
		{
			env = o->m_env;
			return false; // stop at the first match
		}, name);
		return env;
	}
	void LdrObject::Reflectivity(float reflectivity, char const* name)
	{
		Apply([=](LdrObject* o)
		{
			o->m_env = reflectivity;
			return true;
		}, name);
	}

	// Set the texture on this object or child objects matching 'name' (see Apply)
	// Note for 'difference-mode' drawlist management: if the object is currently in
	// one or more drawlists (i.e. added to a scene) it will need to be removed and
	// re-added so that the sort order is correct.
	void LdrObject::SetTexture(Texture2D* tex, char const* name)
	{
		Apply([=](LdrObject* o)
		{
			if (o->m_model == nullptr) return true;
			for (auto& nug : o->m_model->m_nuggets)
			{
				nug.m_tex_diffuse = Texture2DPtr(tex, true);
				nug.UpdateAlphaStates();
			}

			return true;
		}, name);
	}

	// Add/Remove 'child' as a child of this object
	void LdrObject::AddChild(LdrObjectPtr& child)
	{
		PR_ASSERT(PR_DBG, child->m_parent != this, "child is already a child of this object");
		PR_ASSERT(PR_DBG, child->m_parent == nullptr, "child already has a parent");
		child->m_parent = this;
		m_child.push_back(child);
	}
	LdrObjectPtr LdrObject::RemoveChild(LdrObjectPtr& child)
	{
		PR_ASSERT(PR_DBG, child->m_parent == this, "child is not a child of this object");
		auto idx = index_of(m_child, child);
		return RemoveChild(idx);
	}
	LdrObjectPtr LdrObject::RemoveChild(size_t i)
	{
		PR_ASSERT(PR_DBG, i >= 0 && i < m_child.size(), "child index out of range");
		auto child = m_child[i];
		m_child.erase(std::begin(m_child) + i);
		child->m_parent = nullptr;
		return child;
	}
	void LdrObject::RemoveAllChildren()
	{
		while (!m_child.empty())
			RemoveChild(0);
	}

	// Called when there are no more references to this object
	void LdrObject::RefCountZero(RefCount<LdrObject>* doomed)
	{
		delete static_cast<LdrObject*>(doomed);
	}
	long LdrObject::AddRef() const
	{
		return RefCount<LdrObject>::AddRef();
	}
	long LdrObject::Release() const
	{
		return RefCount<LdrObject>::Release();
	}

	#pragma endregion
}
