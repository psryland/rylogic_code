//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/ldraw/ldr_object.h"
#include "pr/view3d-12/main/renderer.h"
#include "pr/view3d-12/scene/scene.h"
#include "pr/view3d-12/model/nugget.h"
#include "pr/view3d-12/model/model.h"
#include "pr/view3d-12/model/model_desc.h"
#include "pr/view3d-12/model/model_tree.h"
#include "pr/view3d-12/model/model_generator.h"
#include "pr/view3d-12/model/vertex_layout.h"
#include "pr/view3d-12/resource/resource_manager.h"
#include "pr/view3d-12/texture/texture_desc.h"
#include "pr/view3d-12/shaders/shader_point_sprites.h"
#include "pr/view3d-12/shaders/shader_thick_line.h"
#include "pr/view3d-12/shaders/shader_arrow_head.h"
#include "pr/view3d-12/lighting/light.h"
#include "pr/view3d-12/utility/wrappers.h"
#include "pr/view3d-12/utility/diagnostics.h"
#include "pr/view3d-12/utility/pipe_state.h"

#include "pr/maths/frustum.h"
#include "pr/maths/convex_hull.h"
#include "pr/script/forward.h"
#include "pr/storage/csv.h"
//#include <string>
//#include <sstream>
//#include <array>
//#include <unordered_set>
//#include <unordered_map>
//#include <optional>
//#include <mutex>
//#include "pr/ldraw/ldr_object.h"
//#include "pr/common/assert.h"
//#include "pr/common/hash.h"
//#include "pr/maths/maths.h"
//#include "pr/geometry/intersect.h"
//#include "pr/filesys/filesys.h"
//#include "pr/str/extract.h"
//#include "pr/view3d/renderer.h"

using namespace pr::script;

namespace pr::rdr12
{
	// Notes:
	//  - Handling Errors:
	//    For parsing or logical errors (e.g. negative widths, etc) use p.ReportError(EResult, msg)
	//    then return gracefully or continue with a valid value. The ReportError function may not
	//    throw in which case parsing will need to continue with sane values.

	using Guid = pr::Guid;
	using VCont = pr::vector<v4>;
	using NCont = pr::vector<v4>;
	using ICont = pr::vector<uint16_t>;
	using CCont = pr::vector<Colour32>;
	using TCont = pr::vector<v2>;
	using GCont = pr::vector<NuggetData>;
	using ModelCont = ParseResult::ModelLookup;
	using EScriptResult = pr::script::EResult;
	using EScriptResult_ = pr::script::EResult_;
	using Font = ModelGenerator::Font;
	using TextFormat = ModelGenerator::TextFormat;
	using TextLayout = ModelGenerator::TextLayout;
	
	enum class EFlags
	{
		None = 0,
		ExplicitName = 1 << 0,
		ExplicitColour = 1 << 1,
		_flags_enum,
	};

	// Template prototype for ObjectCreators
	struct IObjectCreator;
	template <ELdrObject ObjType> struct ObjectCreator;

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

	// Global buffers
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
		Guid            m_context_id;
		Cache           m_cache;
		ELdrObject      m_type;
		LdrObject*      m_parent;
		IObjectCreator* m_parent_creator;
		FontStack       m_font;
		ParseProgressCB m_progress_cb;
		time_point      m_last_progress_update;
		EFlags          m_flags;
		bool&           m_cancel;

		ParseParams(Renderer& rdr, Reader& reader, ParseResult& result, Guid const& context_id, ParseProgressCB progress_cb, bool& cancel)
			:m_rdr(rdr)
			,m_reader(reader)
			,m_result(result)
			,m_objects(result.m_objects)
			,m_models(result.m_models)
			,m_context_id(context_id)
			,m_cache()
			,m_type(ELdrObject::Unknown)
			,m_parent()
			,m_parent_creator()
			,m_font(1)
			,m_progress_cb(progress_cb)
			,m_last_progress_update(system_clock::now())
			,m_flags(EFlags::None)
			,m_cancel(cancel)
		{}
		ParseParams(ParseParams& p, ObjectCont& objects, LdrObject* parent, IObjectCreator* parent_creator)
			:m_rdr(p.m_rdr)
			,m_reader(p.m_reader)
			,m_result(p.m_result)
			,m_objects(objects)
			,m_models(p.m_models)
			,m_context_id(p.m_context_id)
			,m_cache()
			,m_type(ELdrObject::Unknown)
			,m_parent(parent)
			,m_parent_creator(parent_creator)
			,m_font(p.m_font)
			,m_progress_cb(p.m_progress_cb)
			,m_last_progress_update(p.m_last_progress_update)
			,m_flags(p.m_flags)
			,m_cancel(p.m_cancel)
		{}
		ParseParams(ParseParams const&) = delete;
		ParseParams& operator = (ParseParams const&) = delete;

		// Report an error in the script
		void ReportError(EScriptResult result, std::string msg = {})
		{
			if (msg.empty()) msg = EScriptResult_::ToStringA(result);
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
	bool ParseLdrObject(ELdrObject type, ParseParams& p);

	#pragma region Parse Common Elements

	// Read the name, colour, and instance flag for an object
	ObjectAttributes ParseAttributes(ParseParams& p)
	{
		ObjectAttributes attr;
		attr.m_type = p.m_type;
		attr.m_name = "";
			
		// Read the next tokens up to the section start
		wstring32 tok0, tok1; auto count = 0;
		if (!p.m_reader.IsSectionStart()) { p.m_reader.Token(tok0, L"{}"); ++count; }
		if (!p.m_reader.IsSectionStart()) { p.m_reader.Token(tok1, L"{}"); ++count; }
		if (!p.m_reader.IsSectionStart())
			p.ReportError(EScriptResult::UnknownToken, "object attributes are invalid");

		switch (count)
		{
			case 2:
			{
				// Expect: *Type <name> <colour>
				if (!str::ExtractIdentifierC(attr.m_name, std::begin(tok0)))
					p.ReportError(EScriptResult::TokenNotFound, "object name is invalid");
				if (!str::ExtractIntC(attr.m_colour.argb, 16, std::begin(tok1)))
					p.ReportError(EScriptResult::TokenNotFound, "object colour is invalid");
				p.m_flags |= EFlags::ExplicitName;
				p.m_flags |= EFlags::ExplicitColour;
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
						p.ReportError(EScriptResult::TokenNotFound, "object colour is invalid");
	
					p.m_flags |= EFlags::ExplicitColour;
				}
				else
				{
					attr.m_colour = 0xFFFFFFFF;
					if (!str::ExtractIdentifierC(attr.m_name, std::begin(tok0)))
						p.ReportError(EScriptResult::TokenNotFound, "object name is invalid");
				
					p.m_flags |= EFlags::ExplicitName;
				}
				break;
			}
			case 0:
			{
				attr.m_name = Enum<ELdrObject>::ToStringA(p.m_type);
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
				default:
				{
					p.ReportError(EScriptResult::UnknownToken, Fmt("Keyword '%S' is not valid within *Camera", p.m_reader.LastKeyword().c_str()));
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
					if (str::EqualI(ident, "normal")) font.m_style = DWRITE_FONT_STYLE_NORMAL;
					if (str::EqualI(ident, "italic")) font.m_style = DWRITE_FONT_STYLE_ITALIC;
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
				default:
				{
					p.ReportError(EScriptResult::UnknownToken, Fmt("Keyword '%S' is not valid within *Font", reader.LastKeyword().c_str()));
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
				case EKeyword::Style:
				{
					char style[50];
					reader.Identifier(style);
					if (str::EqualI(style, "NoAnimation")) anim.m_style = EAnimStyle::NoAnimation;
					else if (str::EqualI(style, "Once")) anim.m_style = EAnimStyle::Once;
					else if (str::EqualI(style, "Repeat")) anim.m_style = EAnimStyle::Repeat;
					else if (str::EqualI(style, "Continuous")) anim.m_style = EAnimStyle::Continuous;
					else if (str::EqualI(style, "PingPong")) anim.m_style = EAnimStyle::PingPong;
					break;
				}
				case EKeyword::Period:
				{
					reader.RealS(anim.m_period);
					break;
				}
				case EKeyword::Velocity:
				{
					reader.Vector3S(anim.m_vel, 0.0f);
					break;
				}
				case EKeyword::Accel:
				{
					reader.Vector3S(anim.m_acc, 0.0f);
					break;
				}
				case EKeyword::AngVelocity:
				{
					reader.Vector3S(anim.m_avel, 0.0f);
					break;
				}
				case EKeyword::AngAccel:
				{
					reader.Vector3S(anim.m_aacc, 0.0f);
					break;
				}
				default:
				{
					p.ReportError(EScriptResult::UnknownToken, Fmt("Keyword '%S' is not valid within *Animation", reader.LastKeyword().c_str()));
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
		SamDesc sam;

		reader.SectionStart();
		while (!reader.IsSectionEnd())
		{
			if (reader.IsKeyword())
			{
				auto kw = reader.NextKeywordH<EKeyword>();
				switch (kw)
				{
					case EKeyword::O2W:
					{
						reader.TransformS(t2s);
						break;
					}
					case EKeyword::Addr:
					{
						char word[20];
						reader.SectionStart();
						reader.Identifier(word); sam.AddressU = (D3D12_TEXTURE_ADDRESS_MODE)Enum<ETexAddrMode>::Parse(word, false);
						reader.Identifier(word); sam.AddressV = (D3D12_TEXTURE_ADDRESS_MODE)Enum<ETexAddrMode>::Parse(word, false);
						reader.SectionEnd();
						break;
					}
					case EKeyword::Filter:
					{
						char word[20];
						reader.SectionStart();
						reader.Identifier(word); sam.Filter = (D3D12_FILTER)Enum<EFilter>::Parse(word, false);
						reader.SectionEnd();
						break;
					}
					case EKeyword::Alpha:
					{
						has_alpha = true;
						break;
					}
					default:
					{
						p.ReportError(EScriptResult::UnknownToken, Fmt("Keyword '%S' is not valid within *Texture", reader.LastKeyword().c_str()));
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
				TextureDesc desc(AutoId, ResDesc(), has_alpha, 0, nullptr);
				tex = p.m_rdr.res_mgr().CreateTexture2D(tex_resource, desc);
				tex->m_t2s = t2s;
			}
			catch (std::exception const& e)
			{
				p.ReportError(EScriptResult::ValueNotFound, FmtS("Failed to create texture %s\n%s", tex_resource.c_str(), e.what()));
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
			//' 	p.ReportError(EScriptResult::ValueNotFound, pr::FmtS("failed to create video %s\nReason: %s" ,filepath.c_str() ,e.what()));
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
			case EKeyword::O2W:
			case EKeyword::Txfm:
			{
				reader.TransformS(obj->m_o2p);
				obj->m_ldr_flags = SetBits(obj->m_ldr_flags, ELdrFlags::NonAffine, !IsAffine(obj->m_o2p));
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
				obj->m_ldr_flags = SetBits(obj->m_ldr_flags, ELdrFlags::Hidden, true);
				return true;
			}
			case EKeyword::Wireframe:
			{
				obj->m_ldr_flags = SetBits(obj->m_ldr_flags, ELdrFlags::Wireframe, true);
				return true;
			}
			case EKeyword::NoZTest:
			{
				obj->m_ldr_flags = SetBits(obj->m_ldr_flags, ELdrFlags::NoZTest, true);
				return true;
			}
			case EKeyword::NoZWrite:
			{
				obj->m_ldr_flags = SetBits(obj->m_ldr_flags, ELdrFlags::NoZWrite, true);
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
			default: return false;
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
		if (AllSet(obj->m_ldr_flags, ELdrFlags::Hidden))
			obj->Visible(false);

		// If flagged as wireframe, set wireframe
		if (AllSet(obj->m_ldr_flags, ELdrFlags::Wireframe))
			obj->Wireframe(true);

		// If NoZTest
		if (AllSet(obj->m_ldr_flags, ELdrFlags::NoZTest))
		{
			// Don't test against Z, and draw above all objects
			obj->m_pso.Set<EPipeState::DepthEnable>(FALSE);
			obj->m_sko.Group(ESortGroup::PostAlpha);
		}

		// If NoZWrite
		if (AllSet(obj->m_ldr_flags, ELdrFlags::NoZWrite))
		{
			// Don't write to Z and draw behind all objects
			obj->m_pso.Set<EPipeState::DepthWriteMask>(D3D12_DEPTH_WRITE_MASK_ZERO);
			obj->m_sko.Group(ESortGroup::PreOpaques);
		}

		// If flagged as screen space rendering mode
		if (obj->m_screen_space)
			obj->ScreenSpace(true);
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
			auto len = Length(d);

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
			auto len = Length(d);

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
			NuggetData m_local_mat;

			Textured()
				:m_texture()
				,m_local_mat()
			{}
			bool ParseKeyword(ParseParams& p, EKeyword kw)
			{
				switch (kw)
				{
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
					default:
					{
						return false;
					}
				}
			}
			NuggetData* Material()
			{
				// This function is used to pass texture/shader data to the model generator.
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
					case EKeyword::AxisId:
					{
						p.m_reader.IntS(m_align.value, 10);
						if (AxisId::IsValid(m_align))
						{
							m_o2w = m4x4::Transform(m_main_axis, m_align, v4Origin);
							return true;
						}

						p.ReportError(EScriptResult::InvalidValue, "AxisId must be +/- 1, 2, or 3 (corresponding to the positive or negative X, Y, or Z axis)");
						return false;
					}
					default:
					{
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
					default:
					{
						return false;
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
							case HashI("square"):   m_style = EStyle::Square; break;
							case HashI("circle"):   m_style = EStyle::Circle; break;
							case HashI("triangle"): m_style = EStyle::Triangle; break;
							case HashI("star"):     m_style = EStyle::Star; break;
							case HashI("annulus"):  m_style = EStyle::Annulus; break;
							default: p.ReportError(EScriptResult::UnknownToken, Fmt("'%s' is not a valid point sprite style", ident.c_str())); break;
						}
						return true;
					}
					case EKeyword::Depth:
					{
						m_depth = true;
						return true;
					}
					default:
					{
						return false;
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
					case EStyle::Square:
					{
						// No texture needed for square style
						return nullptr;
					}
					case EStyle::Circle:
					{
						auto id = pr::hash::Hash("PointStyleCircle", sz);
						return p.m_rdr.res_mgr().FindTexture<Texture2D>(id, [&]
							{
								auto w0 = sz.x * 0.5f;
								auto h0 = sz.y * 0.5f;
								return CreatePointStyleTexture(p, id, sz, "PointStyleCircle", [=](auto dc, auto fr, auto) { dc->FillEllipse({{w0, h0}, w0, h0}, fr); });
							});
					}
					case EStyle::Triangle:
					{
						auto id = pr::hash::Hash("PointStyleTriangle", sz);
						return p.m_rdr.res_mgr().FindTexture<Texture2D>(id, [&]
							{
								Renderer::Lock lk(p.m_rdr);
								D3DPtr<ID2D1PathGeometry> geom;
								D3DPtr<ID2D1GeometrySink> sink;
								pr::Throw(lk.D2DFactory()->CreatePathGeometry(&geom.m_ptr));
								pr::Throw(geom->Open(&sink.m_ptr));

								auto w0 = 1.0f * sz.x;
								auto h0 = 0.5f * sz.y * (float)tan(pr::DegreesToRadians(60.0f));
								auto h1 = 0.5f * (sz.y - h0);

								sink->BeginFigure({w0, h1}, D2D1_FIGURE_BEGIN_FILLED);
								sink->AddLine({0.0f * w0, h1});
								sink->AddLine({0.5f * w0, h0 + h1});
								sink->EndFigure(D2D1_FIGURE_END_CLOSED);
								pr::Throw(sink->Close());

								return CreatePointStyleTexture(p, id, sz, "PointStyleTriangle", [=](auto dc, auto fr, auto) { dc->FillGeometry(geom.get(), fr, nullptr); });
							});
					}
					case EStyle::Star:
					{
						auto id = pr::hash::Hash("PointStyleStar", sz);
						return p.m_rdr.res_mgr().FindTexture<Texture2D>(id, [&]
							{
								Renderer::Lock lk(p.m_rdr);
								D3DPtr<ID2D1PathGeometry> geom;
								D3DPtr<ID2D1GeometrySink> sink;
								pr::Throw(lk.D2DFactory()->CreatePathGeometry(&geom.m_ptr));
								pr::Throw(geom->Open(&sink.m_ptr));

								auto w0 = 1.0f * sz.x;
								auto h0 = 1.0f * sz.y;

								sink->BeginFigure({0.5f * w0, 0.0f * h0}, D2D1_FIGURE_BEGIN_FILLED);
								sink->AddLine({0.4f * w0, 0.4f * h0});
								sink->AddLine({0.0f * w0, 0.5f * h0});
								sink->AddLine({0.4f * w0, 0.6f * h0});
								sink->AddLine({0.5f * w0, 1.0f * h0});
								sink->AddLine({0.6f * w0, 0.6f * h0});
								sink->AddLine({1.0f * w0, 0.5f * h0});
								sink->AddLine({0.6f * w0, 0.4f * h0});
								sink->EndFigure(D2D1_FIGURE_END_CLOSED);
								pr::Throw(sink->Close());

								return CreatePointStyleTexture(p, id, sz, "PointStyleStar", [=](auto dc, auto fr, auto) { dc->FillGeometry(geom.get(), fr, nullptr); });
							});
					}
					case EStyle::Annulus:
					{
						auto id = pr::hash::Hash("PointStyleAnnulus", sz);
						return p.m_rdr.res_mgr().FindTexture<Texture2D>(id, [&]
							{
								auto w0 = sz.x * 0.5f;
								auto h0 = sz.y * 0.5f;
								auto w1 = sz.x * 0.4f;
								auto h1 = sz.y * 0.4f;
								return CreatePointStyleTexture(p, id, sz, "PointStyleAnnulus", [=](auto dc, auto fr, auto bk)
									{
										dc->SetPrimitiveBlend(D2D1_PRIMITIVE_BLEND_COPY);
										dc->FillEllipse({{w0, h0}, w0, h0}, fr);
										dc->FillEllipse({{w0, h0}, w1, h1}, bk);
									});
							});
					}
					default: throw std::exception("Unknown point style");

				}
			}
			template <typename TDrawOnIt>
			Texture2DPtr CreatePointStyleTexture(ParseParams& p, RdrId id, iv2 const& sz, char const* name, TDrawOnIt draw)
			{
				// Create a texture large enough to contain the text, and render the text into it
				//'SamDesc sdesc(D3D11_TEXTURE_ADDRESS_CLAMP, D3D11_FILTER_MIN_MAG_MIP_POINT);
				auto tdesc = ResDesc::Tex2D(Image(sz.x, sz.y, nullptr, DXGI_FORMAT_B8G8R8A8_UNORM), 1, EUsage::RenderTarget);
				TextureDesc desc(id, tdesc, false, 0, name);
				auto tex = p.m_rdr.res_mgr().CreateTexture2D(desc);

				// Get a D2D device context to draw on
				auto dc = tex->GetD2DeviceContext();

				// Create the brushes
				D3DPtr<ID2D1SolidColorBrush> fr_brush;
				D3DPtr<ID2D1SolidColorBrush> bk_brush;
				auto fr = D3DCOLORVALUE{1.f, 1.f, 1.f, 1.f};
				auto bk = D3DCOLORVALUE{0.f, 0.f, 0.f, 0.f};
				Throw(dc->CreateSolidColorBrush(fr, &fr_brush.m_ptr));
				Throw(dc->CreateSolidColorBrush(bk, &bk_brush.m_ptr));

				// Draw the spot
				dc->BeginDraw();
				dc->Clear(&bk);
				draw(dc, fr_brush.get(), bk_brush.get());
				pr::Throw(dc->EndDraw());
				return tex;
			}
		};

		// Support baked in transforms
		struct BakeTransform
		{
			m4x4 m_bake;

			BakeTransform(m4x4 const& bake = m4x4::Zero())
				:m_bake(bake)
			{}
			bool ParseKeyword(ParseParams& p, EKeyword kw)
			{
				switch (kw)
				{
					case EKeyword::BakeTransform:
					{
						p.m_reader.TransformS(m_bake);
						return true;
					}
					default:
					{
						return false;
					}
				}
			}
			explicit operator bool() const
			{
				return m_bake.w.w != 0;
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
					case EKeyword::GenerateNormals:
					{
						p.m_reader.RealS(m_smoothing_angle);
						m_smoothing_angle = pr::DegreesToRadians(m_smoothing_angle);
						return true;
					}
					default:
					{
						return false;
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
					if (nug.m_topo != ETopo::TriList)
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
					pr::geometry::GenerateNormals(icount, iptr, m_smoothing_angle, 0,
						[&](uint16_t i)
						{
							return verts[i];
						},
						[&](uint16_t new_idx, uint16_t orig_idx, v4 const& norm)
						{
							if (new_idx >= verts.size())
							{
								verts.resize(new_idx + 1, verts[orig_idx]);
								normals.resize(new_idx + 1, normals[orig_idx]);
							}
							normals[new_idx] = norm;
						},
						[&](uint16_t i0, uint16_t i1, uint16_t i2)
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
		virtual bool ParseKeyword(EKeyword)
		{
			return false;
		}
		virtual void Parse()
		{
			p.ReportError(EScriptResult::UnknownToken, Fmt("Unknown token near '%S'", p.m_reader.LastKeyword().c_str()));
		}
		virtual void CreateModel(LdrObject*)
		{
		}
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
				p.ReportError(EScriptResult::Failed, FmtS("Point object '%s' description incomplete", obj->TypeAndName().c_str()));
				return;
			}

			// Create the model
			obj->m_model = ModelGenerator::Points(p.m_rdr, int(m_verts.size()), m_verts.data(), int(m_colours.size()), m_colours.data(), m_tex.Material());
			obj->m_model->m_name = obj->TypeAndName();

			// Use a geometry shader to draw points
			if (m_sprite.m_point_size != v2Zero)
			{
				// Get/Create an instance of the point sprites shader
				auto shdr = Shader::Create<shaders::PointSpriteGS>(m_sprite.m_point_size, m_sprite.m_depth);

				// Get/Create the point style texture
				auto tex = m_sprite.PointStyleTexture(p);

				// Update the nuggets
				for (auto& nug : obj->m_model->m_nuggets)
				{
					nug.m_tex_diffuse = tex;
					nug.m_shaders.push_back({ERenderStep::RenderForward, shdr});
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
						p.ReportError(EScriptResult::Failed, "No preceding line to apply parametric values to");
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
				p.ReportError(EScriptResult::Failed, FmtS("Line object '%s' description incomplete", obj->TypeAndName().c_str()));
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
			obj->m_model = ModelGenerator::Lines(p.m_rdr, int(m_verts.size() / 2), m_verts.data(), int(m_colours.size()), m_colours.data());
			obj->m_model->m_name = obj->TypeAndName();

			// Use thick lines
			if (m_line_width != 0.0f)
			{
				auto shdr = Shader::Create<shaders::ThickLineListGS>(m_line_width);
				for (auto& nug : obj->m_model->m_nuggets)
					nug.m_shaders.push_back({ERenderStep::RenderForward, shdr});
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
						p.ReportError(EScriptResult::Failed, "No preceding line to apply parametric values to");
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
				p.ReportError(EScriptResult::Failed, FmtS("LineD object '%s' description incomplete", obj->TypeAndName().c_str()));
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
			obj->m_model = ModelGenerator::Lines(p.m_rdr, int(m_verts.size() / 2), m_verts.data(), int(m_colours.size()), m_colours.data());
			obj->m_model->m_name = obj->TypeAndName();

			// Use thick lines
			if (m_line_width != 0.0f)
			{
				auto shdr = Shader::Create<shaders::ThickLineListGS>(m_line_width);
				for (auto& nug : obj->m_model->m_nuggets)
					nug.m_shaders.push_back({ERenderStep::RenderForward, shdr});
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
						p.ReportError(EScriptResult::Failed, "No preceding line to apply parametric values to");
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

			if (*m_per_vert_colour)
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
				pr::Smooth(verts, Spline::ETopo::Continuous3, m_verts);
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

			// The thick line strip shader uses LineAdj which requires an extra first and last vert
			if (line_strip && m_line_width != 0.0f)
			{
				m_verts.insert(std::begin(m_verts), m_verts.front());
				m_verts.insert(std::end(m_verts), m_verts.back());
			}

			// Create the model
			obj->m_model = line_strip
				? ModelGenerator::LineStrip(p.m_rdr, int(m_verts.size() - 1), m_verts.data(), int(m_colours.size()), m_colours.data())
				: ModelGenerator::Lines(p.m_rdr, int(m_verts.size() / 2), m_verts.data(), int(m_colours.size()), m_colours.data());
			obj->m_model->m_name = obj->TypeAndName();

			// Use thick lines
			if (m_line_width != 0.0f)
			{
				auto shdr = line_strip
					? static_cast<ShaderPtr>(Shader::Create<shaders::ThickLineStripGS>(m_line_width))
					: static_cast<ShaderPtr>(Shader::Create<shaders::ThickLineListGS>(m_line_width));

				for (auto& nug : obj->m_model->m_nuggets)
				{
					nug.m_topo = line_strip ? ETopo::LineStripAdj : ETopo::LineList;
					nug.m_shaders.push_back({ERenderStep::RenderForward, shdr});
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

			uint16_t idx[] = { 0, 1, 1, 2, 2, 3, 3, 0, 4, 5, 5, 6, 6, 7, 7, 4, 0, 4, 1, 5, 2, 6, 3, 7 };
			m_indices.insert(m_indices.end(), idx, idx + _countof(idx));
		}
		void CreateModel(LdrObject* obj) override
		{
			// Validate
			if (m_verts.empty())
			{
				p.ReportError(EScriptResult::Failed, FmtS("LineBox object '%s' description incomplete", obj->TypeAndName().c_str()));
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
				.nuggets({NuggetData(ETopo::LineList, EGeom::Vert|EGeom::Colr)});
			obj->m_model = ModelGenerator::Mesh(p.m_rdr, cdata);
			obj->m_model->m_name = obj->TypeAndName();

			// Use thick lines
			if (m_line_width != 0.0f)
			{
				// Get or create an instance of the thick line shader
				auto shdr = Shader::Create<shaders::ThickLineListGS>(m_line_width);
				for (auto& nug : obj->m_model->m_nuggets)
					nug.m_shaders.push_back({ERenderStep::RenderForward, shdr});
			}
		}
	};

	// ELdrObject::Grid
	template <> struct ObjectCreator<ELdrObject::Grid> :IObjectCreator
	{
		v2 m_dashed;
		float m_line_width;
		creation::MainAxis m_axis;

		ObjectCreator(ParseParams& p)
			:IObjectCreator(p)
			,m_dashed(v2XAxis)
			,m_line_width()
			,m_axis()
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
				p.ReportError(EScriptResult::Failed, FmtS("Grid object '%s' description incomplete", obj->TypeAndName().c_str()));
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

			// Apply main axis transform
			if (m_axis.O2WPtr() != nullptr)
			{
				for (auto& v : m_verts)
					v = *m_axis.O2WPtr() * v;
			}

			// Create the model
			obj->m_model = ModelGenerator::Lines(p.m_rdr, int(m_verts.size() / 2), m_verts.data(), int(m_colours.size()), m_colours.data());
			obj->m_model->m_name = obj->TypeAndName();

			// Use thick lines
			if (m_line_width != 0.0f)
			{
				// Get or create an instance of the thick line shader
				auto shdr = Shader::Create<shaders::ThickLineListGS>(m_line_width);
				for (auto& nug : obj->m_model->m_nuggets)
					nug.m_shaders.push_back({ERenderStep::RenderForward, shdr});
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
				case EKeyword::Width:
				{
					p.m_reader.RealS(m_line_width);
					return true;
				}
				default:
				{
					return IObjectCreator::ParseKeyword(kw);
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
				p.ReportError(EScriptResult::Failed, FmtS("Spline object '%s' description incomplete", obj->TypeAndName().c_str()));
				return;
			}

			// Generate a line strip for all spline segments (separated using strip-cut indices)
			auto seg = -1;
			auto thick = m_line_width != 0.0f;
			pr::vector<v4, 30, true> raster;
			for (auto& spline : m_splines)
			{
				++seg;

				// Generate points for the spline
				raster.resize(0);
				pr::Raster(spline, raster, 30);

				// Check for 16-bit index overflow
				if (m_verts.size() + raster.size() >= 0xFFFF)
				{
					p.ReportError(EScriptResult::Failed, FmtS("Spline object '%s' is too large (index count >= 0xffff)", obj->TypeAndName().c_str()));
					return;
				}

				// Add the line strip to the geometry buffers
				auto vert = uint16_t(m_verts.size());
				m_verts.insert(std::end(m_verts), std::begin(raster), std::end(raster));

				{// Indices
					auto ibeg = m_indices.size();
					m_indices.reserve(m_indices.size() + raster.size() + (thick ? 2 : 0) + 1);

					// The thick line strip shader uses LineAdj which requires an extra first and last vert
					if (thick)
						m_indices.push_back_fast(vert);

					auto iend = ibeg + raster.size();
					for (auto i = ibeg; i != iend; ++i)
						m_indices.push_back_fast(vert++);

					if (thick)
						m_indices.push_back_fast(vert);

					m_indices.push_back_fast(uint16_t(-1)); // strip-cut
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
				.nuggets({NuggetData(ETopo::LineStrip, EGeom::Vert|EGeom::Colr)});
			obj->m_model = ModelGenerator::Mesh(p.m_rdr, cdata);
			obj->m_model->m_name = obj->TypeAndName();

			// Use thick lines
			if (thick)
			{
				auto shdr = Shader::Create<shaders::ThickLineStripGS>(m_line_width);
				for (auto& nug : obj->m_model->m_nuggets)
				{
					nug.m_topo = ETopo::LineStripAdj;
					nug.m_shaders.push_back({ERenderStep::RenderForward, shdr});
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
				default:
				{
					return
						IObjectCreator::ParseKeyword(kw);
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
				else p.ReportError(EScriptResult::UnknownValue, "arrow type must one of Line, Fwd, Back, FwdBack");
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
				p.ReportError(EScriptResult::Failed, FmtS("Arrow object '%s' description incomplete", obj->TypeAndName().c_str()));
				return;
			}

			// Convert the points into a spline if smooth is specified
			if (m_smooth)
			{
				VCont verts;
				std::swap(verts, m_verts);
				pr::Smooth(verts, Spline::ETopo::Continuous3, m_verts);
			}

			pr::geometry::Props props;

			// Colour interpolation iterator
			auto col = pr::CreateLerpRepeater(m_colours.data(), int(m_colours.size()), int(m_verts.size()), pr::Colour32White);
			auto cc = [&](pr::Colour32 c) { props.m_has_alpha |= HasAlpha(c); return c; };

			// Model bounding box
			auto bb = [&](v4 const& v) { Grow(props.m_bbox, v); return v; };

			// Generate the model
			// 'm_verts' should contain line strip data
			ModelGenerator::Cache<> cache(int(m_verts.size() + 2), int(m_verts.size() + 2), 0, 2);

			auto v_in  = m_verts.data();
			auto v_out = cache.m_vcont.data();
			auto i_out = cache.m_icont.data<uint16_t>();
			pr::Colour32 c = pr::Colour32White;
			uint16_t index = 0;

			// Add the back arrow head geometry (a point)
			if (m_type & EArrowType::Back)
			{
				SetPCN(*v_out++, *v_in, *col, pr::Normalise(*v_in - *(v_in+1)));
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
				SetPCN(*v_out++, *v_in, c, pr::Normalise(*v_in - *(v_in-1)));
				*i_out++ = index++;
			}

			// Create the model
			ModelDesc mdesc(cache.m_vcont.cspan(), cache.m_icont.span<uint16_t const>(), props.m_bbox, obj->TypeAndName().c_str());
			obj->m_model = p.m_rdr.res_mgr().CreateModel(mdesc);

			// Get instances of the arrow head geometry shader and the thick line shader
			auto thk_shdr = Shader::Create<shaders::ThickLineListGS>(m_line_width);
			auto arw_shdr = Shader::Create<shaders::ArrowHeadGS>(m_line_width*2);

			// Create nuggets
			NuggetData nug;
			Range vrange(0,0);
			Range irange(0,0);
			if (m_type & EArrowType::Back)
			{
				vrange = Range(0, 1);
				irange = Range(0, 1);
				nug.m_topo = ETopo::PointList;
				nug.m_geom = EGeom::Vert|EGeom::Colr;
				nug.m_shaders.push_back({ERenderStep::RenderForward, arw_shdr});
				nug.m_vrange = vrange;
				nug.m_irange = irange;
				nug.m_nflags = SetBits(nug.m_nflags, ENuggetFlag::GeometryHasAlpha, cache.m_vcont[0].m_diff.a != 1.0f);
				obj->m_model->CreateNugget(nug);
			}
			{
				vrange = Range(vrange.m_end, vrange.m_end + m_verts.size());
				irange = Range(irange.m_end, irange.m_end + m_verts.size());
				nug.m_topo = ETopo::LineStrip;
				nug.m_geom = EGeom::Vert|EGeom::Colr;
				nug.m_shaders.push_back({ERenderStep::RenderForward, m_line_width != 0 ? static_cast<ShaderPtr>(thk_shdr) : ShaderPtr()});
				nug.m_vrange = vrange;
				nug.m_irange = irange;
				nug.m_nflags = SetBits(nug.m_nflags, ENuggetFlag::GeometryHasAlpha, props.m_has_alpha);
				obj->m_model->CreateNugget(nug);
			}
			if (m_type & EArrowType::Fwd)
			{
				vrange = Range(vrange.m_end, vrange.m_end + 1);
				irange = Range(irange.m_end, irange.m_end + 1);
				nug.m_topo = ETopo::PointList;
				nug.m_geom = EGeom::Vert|EGeom::Colr;
				nug.m_shaders.push_back({ERenderStep::RenderForward, arw_shdr});
				nug.m_vrange = vrange;
				nug.m_irange = irange;
				nug.m_nflags = SetBits(nug.m_nflags, ENuggetFlag::GeometryHasAlpha, cache.m_vcont.back().m_diff.a != 1.0f);
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
				case EKeyword::Width:
				{
					p.m_reader.RealS(m_line_width);
					return true;
				}
				default:
				{
					return
						IObjectCreator::ParseKeyword(kw);
				}
			}
		}
		void Parse() override
		{
			pr::m4x4 basis;
			p.m_reader.Matrix3x3(basis.rot);

			pr::v4       pts[] = { pr::v4Origin, basis.x.w1(), pr::v4Origin, basis.y.w1(), pr::v4Origin, basis.z.w1() };
			pr::Colour32 col[] = { pr::Colour32Red, pr::Colour32Red, pr::Colour32Green, pr::Colour32Green, pr::Colour32Blue, pr::Colour32Blue };
			uint16_t   idx[] = { 0, 1, 2, 3, 4, 5 };

			m_verts.insert(m_verts.end(), pts, pts + _countof(pts));
			m_colours.insert(m_colours.end(), col, col + _countof(col));
			m_indices.insert(m_indices.end(), idx, idx + _countof(idx));
		}
		void CreateModel(LdrObject* obj) override
		{
			// Validate
			if (m_verts.empty())
			{
				p.ReportError(EScriptResult::Failed, FmtS("Matrix3x3 object '%s' description incomplete", obj->TypeAndName().c_str()));
				return;
			}

			// Create the model
			auto cdata = MeshCreationData()
				.verts(m_verts.data(), int(m_verts.size()))
				.indices(m_indices.data(), int(m_indices.size()))
				.colours(m_colours.data(), int(m_colours.size()))
				.nuggets({NuggetData(ETopo::LineList, EGeom::Vert|EGeom::Colr)});
			obj->m_model = ModelGenerator::Mesh(p.m_rdr, cdata);
			obj->m_model->m_name = obj->TypeAndName();

			// Use thick lines
			if (m_line_width != 0.0f)
			{
				// Get or create an instance of the thick line shader
				auto shdr = Shader::Create<shaders::ThickLineListGS>(m_line_width);
				for (auto& nug : obj->m_model->m_nuggets)
					nug.m_shaders.push_back({ERenderStep::RenderForward, shdr});
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
			uint16_t   idx[] = { 0, 1, 2, 3, 4, 5 };

			m_verts.insert(m_verts.end(), pts, pts + _countof(pts));
			m_colours.insert(m_colours.end(), col, col + _countof(col));
			m_indices.insert(m_indices.end(), idx, idx + _countof(idx));
		}
		bool ParseKeyword(EKeyword kw) override
		{
			switch (kw)
			{
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
				default:
				{
					return IObjectCreator::ParseKeyword(kw);
				}
			}
		}
		void CreateModel(LdrObject* obj) override
		{
			// Scale doesn't use the *o2w scale because that is recursive
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
			obj->m_model = ModelGenerator::Lines(p.m_rdr, int(m_verts.size() / 2), m_verts.data(), int(m_colours.size()), m_colours.data());
			obj->m_model->m_name = obj->TypeAndName();

			// Use thick lines
			if (m_line_width != 0.0f)
			{
				// Get or create an instance of the thick line shader
				auto shdr = Shader::Create<shaders::ThickLineListGS>(m_line_width);
				for (auto& nug : obj->m_model->m_nuggets)
					nug.m_shaders.push_back({ERenderStep::RenderForward, shdr});
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
				p.ReportError(EScriptResult::InvalidValue, "Circle dimensions contain a negative value");
			}
		}
		void CreateModel(LdrObject* obj) override
		{
			// Create the model
			obj->m_model = ModelGenerator::Ellipse(p.m_rdr, m_dim.x, m_dim.y, m_solid, m_facets, Colour32White, m_axis.O2WPtr(), m_tex.Material());
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
			obj->m_model = ModelGenerator::Pie(p.m_rdr, m_scale.x, m_scale.y, m_ang.x, m_ang.y, m_rad.x, m_rad.y, m_solid, m_facets, Colour32White, m_axis.O2WPtr(), m_tex.Material());
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
				p.ReportError(EScriptResult::InvalidValue, "Rect dimensions contain a negative value");
			}
		}
		void CreateModel(LdrObject* obj) override
		{
			// Create the model
			obj->m_model = ModelGenerator::RoundedRectangle(p.m_rdr, m_dim.x, m_dim.y, m_corner_radius, m_solid, m_facets, Colour32White, m_axis.O2WPtr(), m_tex.Material());
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
			obj->m_model = ModelGenerator::Polygon(p.m_rdr, int(m_poly.size()), m_poly.data(), m_solid, int(m_colours.size()), m_colours.data(), m_axis.O2WPtr(), m_tex.Material());
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
				p.ReportError(EScriptResult::Failed, "Object description incomplete");
				return;
			}

			// Apply the axis id rotation
			if (m_axis.RotationNeeded())
			{
				for (auto& pt : m_verts)
					pt = m_axis.O2W() * pt;
			}

			// Create the model
			obj->m_model = ModelGenerator::Quad(p.m_rdr, int(m_verts.size() / 4), m_verts.data(), int(m_colours.size()), m_colours.data(), pr::m4x4Identity, m_tex.Material());
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
				p.ReportError(EScriptResult::Failed, "Object description incomplete");
				return;
			}

			// Apply the axis id rotation
			if (m_axis.RotationNeeded())
			{
				for (auto& pt : m_verts)
					pt = m_axis.O2W() * pt;
			}

			// Create the model
			obj->m_model = ModelGenerator::Quad(p.m_rdr, int(m_verts.size() / 4), m_verts.data(), int(m_colours.size()), m_colours.data(), pr::m4x4Identity, m_tex.Material());
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

			fwd = Normalise(fwd);
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
				p.ReportError(EScriptResult::Failed, "Object description incomplete");
				return;
			}

			// Create the model
			obj->m_model = ModelGenerator::Quad(p.m_rdr, int(m_verts.size() / 4), m_verts.data(), int(m_colours.size()), m_colours.data(), pr::m4x4Identity, m_tex.Material());
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
				p.ReportError(EScriptResult::Failed, "Object description incomplete");
				return;
			}

			// Smooth the points
			if (m_smooth)
			{
				VCont verts;
				std::swap(verts, m_verts);
				pr::Smooth(verts, Spline::ETopo::Continuous3, m_verts);
			}

			pr::v4 normal = m_axis.m_align;
			obj->m_model = ModelGenerator::QuadStrip(p.m_rdr, int(m_verts.size() - 1), m_verts.data(), m_width, 1, &normal, int(m_colours.size()), m_colours.data(), m_tex.Material());
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
			obj->m_model = ModelGenerator::Box(p.m_rdr, m_dim, pr::m4x4Identity, pr::Colour32White, m_tex.Material());
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
			auto dim = v4(m_width, m_height, Length(m_pt1 - m_pt0), 0.0f) * 0.5f;
			auto b2w = OriFromDir(m_pt1 - m_pt0, AxisId::PosZ, m_up, (m_pt1 + m_pt0) * 0.5f);
			obj->m_model = ModelGenerator::Box(p.m_rdr, dim, b2w, Colour32White, m_tex.Material());
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
				p.ReportError(EScriptResult::Failed, "BoxList object description incomplete");
				return;
			}
			if (Abs(m_dim) != m_dim)
			{
				p.ReportError(EScriptResult::InvalidValue, "BoxList box dimensions contain a negative value");
				return;
			}

			m_dim *= 0.5f;

			// Create the model
			obj->m_model = ModelGenerator::BoxList(p.m_rdr, int(m_location.size()), m_location.data(), m_dim, 0, 0, m_tex.Material());
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
			,m_view_plane(0.0f)
		{}
		bool ParseKeyword(EKeyword kw) override
		{
			switch (kw)
			{
				case EKeyword::ViewPlaneZ:
				{
					p.m_reader.RealS(m_view_plane);
					return true;
				}
				default:
				{
					return
						m_axis.ParseKeyword(p, kw) ||
						m_tex.ParseKeyword(p, kw) ||
						IObjectCreator::ParseKeyword(kw);
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
			// Construct pointing down -z, then rotate the points based on axis id.
			// Do this because frustums are commonly used for camera views and cameras point down -z.
			// If the near plane is given, but no view plane, assume the near plane is the view plane.
			float n = m_near, f = m_far;
			auto vp = m_view_plane != 0 ? m_view_plane : m_near != 0 ? m_near : 1.0f;
			auto w = 0.5f * m_width  / vp;
			auto h = 0.5f * m_height / vp;

			m_pt[0] = v4(-f*w, -f*h, -f, 1.0f);
			m_pt[1] = v4(+f*w, -f*h, -f, 1.0f);
			m_pt[2] = v4(-f*w, +f*h, -f, 1.0f);
			m_pt[3] = v4(+f*w, +f*h, -f, 1.0f);
			m_pt[4] = v4(-n*w, -n*h, -n, 1.0f);
			m_pt[5] = v4(+n*w, -n*h, -n, 1.0f);
			m_pt[6] = v4(-n*w, +n*h, -n, 1.0f);
			m_pt[7] = v4(+n*w, +n*h, -n, 1.0f);

			obj->m_model = ModelGenerator::Boxes(p.m_rdr, 1, m_pt, m_axis.O2W(), 0, 0, m_tex.Material());
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
			// Construct pointing down -z, then rotate the points based on axis id.
			// Do this because frustums are commonly used for camera views and cameras point down -z.
			float h = Tan(DegreesToRadians(m_fovY * 0.5f));
			float w = m_aspect * h;
			float n = m_near, f = m_far;
			
			m_pt[0] = v4(-f*w, -f*h, -f, 1.0f);
			m_pt[1] = v4(+f*w, -f*h, -f, 1.0f);
			m_pt[2] = v4(-f*w, +f*h, -f, 1.0f);
			m_pt[3] = v4(+f*w, +f*h, -f, 1.0f);
			m_pt[4] = v4(-n*w, -n*h, -n, 1.0f);
			m_pt[5] = v4(+n*w, -n*h, -n, 1.0f);
			m_pt[6] = v4(-n*w, +n*h, -n, 1.0f);
			m_pt[7] = v4(+n*w, +n*h, -n, 1.0f);

			obj->m_model = ModelGenerator::Boxes(p.m_rdr, 1, m_pt, m_axis.O2W(), 0, 0, m_tex.Material());
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
			obj->m_model = ModelGenerator::Geosphere(p.m_rdr, m_dim, m_facets, Colour32White, m_tex.Material());
			obj->m_model->m_name = obj->TypeAndName();
		}
	};

	// ELdrObject::Cylinder
	template <> struct ObjectCreator<ELdrObject::Cylinder> :IObjectCreator
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
			obj->m_model = ModelGenerator::Cylinder(p.m_rdr, m_dim.x, m_dim.y, m_dim.z, m_scale.x, m_scale.y, m_wedges, m_layers, 1, &pr::Colour32White, m_axis.O2WPtr(), m_tex.Material());
			obj->m_model->m_name = obj->TypeAndName();
		}
	};

	// ELdrObject::Cone
	template <> struct ObjectCreator<ELdrObject::Cone> :IObjectCreator
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
			obj->m_model = ModelGenerator::Cylinder(p.m_rdr, m_dim.x, m_dim.y, m_dim.z, m_scale.x, m_scale.y, m_wedges, m_layers, 1, &pr::Colour32White, m_axis.O2WPtr(), m_tex.Material());
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
							p.ReportError(EScriptResult::UnknownValue, FmtS("Cross Section type %s is not supported", type.c_str()));
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
				p.ReportError(EScriptResult::Failed, FmtS("Tube object '%s' description incomplete. No extrusion path", obj->TypeAndName().c_str()));
				return;
			}

			// Create the cross section for implicit profiles
			switch (m_type)
			{
			default:
				{
					p.ReportError(EScriptResult::Failed, FmtS("Tube object '%s' description incomplete. No style given.", obj->TypeAndName().c_str()));
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
						p.ReportError(EScriptResult::Failed, FmtS("Tube object '%s' description incomplete", obj->TypeAndName().c_str()));
						return;
					}
					if (pr::geometry::PolygonArea(m_cs.data(), int(m_cs.size())) < 0)
					{
						p.ReportError(EScriptResult::Failed, FmtS("Tube object '%s' cross section has a negative area (winding order is incorrect)", obj->TypeAndName().c_str()));
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
				pr::Smooth(verts, Spline::ETopo::Continuous3, m_verts);
			}

			// Create the model
			obj->m_model = ModelGenerator::Extrude(p.m_rdr, int(m_cs.size()), m_cs.data(), int(m_verts.size()), m_verts.data(), m_closed, m_cs_smooth, int(m_colours.size()), m_colours.data());
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
				case EKeyword::LineList:
				case EKeyword::LineStrip:
				{
					auto is_strip = kw == EKeyword::LineStrip;
					auto nug = *m_tex.Material();
					nug.m_topo = is_strip ? ETopo::LineStrip : ETopo::LineList;
					nug.m_geom = EGeom::Vert |
						(!m_colours.empty() ? EGeom::Colr : EGeom::None);
					nug.m_vrange = Range::Reset();
					nug.m_irange = Range(m_indices.size(), m_indices.size());
					nug.m_nflags = SetBits(nug.m_nflags, ENuggetFlag::GeometryHasAlpha, false);

					int r = 1;
					p.m_reader.SectionStart();
					for (uint16_t idx; !p.m_reader.IsSectionEnd(); ++r)
					{
						p.m_reader.Int(idx, 10);
						m_indices.push_back(idx);
						nug.m_vrange.grow(idx);
						++nug.m_irange.m_end;

						if (r % 500 == 0) p.ReportProgress();
					}
					p.m_reader.SectionEnd();

					m_nuggets.push_back(nug);
					return true;
				}
				case EKeyword::Faces:
				case EKeyword::TriList:
				case EKeyword::TriStrip:
				{
					auto is_strip = kw == EKeyword::TriStrip;
					auto nug = *m_tex.Material();
					nug.m_topo = is_strip ? ETopo::TriStrip : ETopo::TriList;
					nug.m_geom = EGeom::Vert |
						(!m_normals.empty() ? EGeom::Norm : EGeom::None) |
						(!m_colours.empty() ? EGeom::Colr : EGeom::None) |
						(!m_texs.empty() ? EGeom::Tex0 : EGeom::None);
					nug.m_vrange = Range::Reset();
					nug.m_irange = Range(m_indices.size(), m_indices.size());
					nug.m_nflags = SetBits(nug.m_nflags, ENuggetFlag::GeometryHasAlpha, false);

					int r = 1;
					p.m_reader.SectionStart();
					for (uint16_t idx; !p.m_reader.IsSectionEnd(); ++r)
					{
						p.m_reader.Int(idx, 10);
						m_indices.push_back(idx);
						nug.m_vrange.grow(idx);
						++nug.m_irange.m_end;

						if (r % 500 == 0) p.ReportProgress();
					}
					p.m_reader.SectionEnd();

					m_nuggets.push_back(nug);
					return true;
				}
				case EKeyword::Tetra:
				{
					auto nug = *m_tex.Material();
					nug.m_topo = ETopo::TriList;
					nug.m_geom = EGeom::Vert |
						(!m_normals.empty() ? EGeom::Norm : EGeom::None) |
						(!m_colours.empty() ? EGeom::Colr : EGeom::None) |
						(!m_texs.empty() ? EGeom::Tex0 : EGeom::None);
					nug.m_vrange = Range::Reset();
					nug.m_irange = Range(m_indices.size(), m_indices.size());
					nug.m_nflags = SetBits(nug.m_nflags, ENuggetFlag::GeometryHasAlpha, false);

					int r = 1;
					p.m_reader.SectionStart();
					for (uint16_t idx[4]; !p.m_reader.IsSectionEnd(); ++r)
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

						nug.m_vrange.grow(idx[0]);
						nug.m_vrange.grow(idx[1]);
						nug.m_vrange.grow(idx[2]);
						nug.m_vrange.grow(idx[3]);
						nug.m_irange.m_end += 12;

						if (r % 500 == 0) p.ReportProgress();
					}
					p.m_reader.SectionEnd();

					m_nuggets.push_back(nug);
					return true;
				}
				default:
				{
					return
						m_tex.ParseKeyword(p, kw) ||
						m_gen_norms.ParseKeyword(p, kw) ||
						IObjectCreator::ParseKeyword(kw);
				}
			}
		}
		void Parse() override
		{
			// All fields are child keywords
			p.ReportError(EScriptResult::UnknownValue, "Mesh object description invalid");
		}
		void CreateModel(LdrObject* obj) override
		{
			// Validate
			if (m_indices.empty() || m_verts.empty())
			{
				p.ReportError(EScriptResult::Failed, "Mesh object description incomplete");
				return;
			}
			if (!m_colours.empty() && m_colours.size() != m_verts.size())
			{
				p.ReportError(EScriptResult::SyntaxError, FmtS("Mesh objects with colours require one colour per vertex. %d required, %d given.", int(m_verts.size()), int(m_colours.size())));
				return;
			}
			if (!m_normals.empty() && m_normals.size() != m_verts.size())
			{
				p.ReportError(EScriptResult::SyntaxError, FmtS("Mesh objects with normals require one normal per vertex. %d required, %d given.", int(m_verts.size()), int(m_normals.size())));
				return;
			}
			if (!m_texs.empty() && m_texs.size() != m_verts.size())
			{
				p.ReportError(EScriptResult::SyntaxError, FmtS("Mesh objects with texture coordinates require one coordinate per vertex. %d required, %d given.", int(m_verts.size()), int(m_normals.size())));
				return;
			}
			for (auto& nug : m_nuggets)
			{
				// Check the index range is valid
				if (nug.m_vrange.m_beg < 0 || nug.m_vrange.m_end > m_verts.size())
				{
					p.ReportError(EScriptResult::SyntaxError, FmtS("Mesh object with face, line, or tetra section contains indices out of range (section index: %d).", int(&nug - &m_nuggets[0])));
					return;
				}

				// Set the nugget 'has_alpha' value now we know the indices are valid
				if (!m_colours.empty())
				{
					for (auto i = nug.m_irange.begin(); i != nug.m_irange.end(); ++i)
					{
						if (!HasAlpha(m_colours[m_indices[i]])) continue;
						nug.m_nflags = SetBits(nug.m_nflags, ENuggetFlag::GeometryHasAlpha, true);
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
			obj->m_model = ModelGenerator::Mesh(p.m_rdr, cdata);
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
			p.ReportError(EScriptResult::UnknownValue, "Convex hull object description invalid");
		}
		void CreateModel(LdrObject* obj) override
		{
			// Validate
			if (m_verts.size() < 2)
			{
				p.ReportError(EScriptResult::Failed, "Convex hull object description incomplete. At least 2 vertices required");
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
			nug.m_topo = ETopo::TriList;
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
			obj->m_model = ModelGenerator::Mesh(p.m_rdr, cdata);
			obj->m_model->m_name = obj->TypeAndName();
		}
	};

	// ELdrObject::Chart
	template <> struct ObjectCreator<ELdrObject::Chart> :IObjectCreator
	{
		// Notes:
		//  - 'm_data' may be a fully populated NxM table, or a jagged array.
		//  - If jagged, then 'm_index' will be non-empty and 'm_dim' will be the bounding dimensions of the table.
		//  - If non-jagged, then 'm_index' will be empty, and 'm_dim' represents the dimensions of the table.

		using Index = std::vector<int>;         // The index for accessing rows in the data table
		using Table = std::vector<double>;      // The source data loaded into memory

		Table m_data;        // A 2D table of data (row major, i.e. rows are contiguous)
		Index m_index;       // The offset into 'm_data' for the start if each row (if jagged) else empty.
		iv2   m_dim;         // Table dimensions or bounds of the table dimensions.

		ObjectCreator(ParseParams& p)
			:IObjectCreator(p)
			,m_data()
			,m_index()
			,m_dim()
		{}
		template <typename Stream> void ParseDataStream(Stream& stream)
		{
			using namespace pr::csv;
			using namespace pr::str;

			m_data.clear();
			m_index.clear();
			m_dim = iv2Zero;

			// Read CSV data up to the section close
			m_data.reserve(100); Loc loc;
			for (Row row; Read(stream, row, loc); row.resize(0))
			{
				// Trim trailing empty values and empty rows
				if (row.size() == 1 && Trim(row[0], IsWhiteSpace<wchar_t>, false, true).empty())
					row.pop_back();
				if (!row.empty() && Trim(row.back(), IsWhiteSpace<wchar_t>, false, true).empty())
					row.pop_back();
				if (row.empty())
					continue;

				// Convert the row to values. Stop at the first element that fails to parse as a value
				auto row_count = 0;
				for (auto& item : row)
				{
					double value;
					if (!ExtractRealC(value, item.c_str())) break;
					m_data.push_back(value);
					++row_count;
				}

				// Skip rows with no data
				if (row_count == 0)
					continue;

				// Assume the table is non-jagged until we find a different number of items in a row
				if (!m_index.empty()) // Table is jagged already
				{
					m_index.push_back(m_index.back() + row_count);
					m_dim.x = max(m_dim.x, row_count);
					m_dim.y++;
				}
				else if (m_dim.x == row_count) // Table is not jagged (yet), row length is the same
				{
					m_dim.y++;
				}
				else if (m_dim.x == 0) // Table is empty, set the row length
				{
					m_dim.x = row_count;
					m_dim.y = 1;
				}
				else // Row length has changed, convert to jagged
				{
					m_index.reserve(m_dim.y + 1);

					// Fill 'm_index' with rows of length 'm_dim.x'
					m_index.push_back(0);
					for (int i = 0; i != m_dim.y; ++i)
						m_index.push_back(m_index.back() + m_dim.x);

					m_index.push_back(m_index.back() + row_count);
					m_dim.x = max(m_dim.x, row_count);
					m_dim.y++;
				}
			}
			
			// If this is jagged data, then 'm_index' should have 'm_dim.y + 1' items
			// with the last value == to the number of elements in the data table.
			assert(m_index.empty() || (int)m_index.size() == m_dim.y + 1);
			assert(m_index.empty() || m_index.back() == (int)m_data.size());
		}
		bool ParseKeyword(EKeyword kw) override
		{
			switch (kw)
			{
				default:
				{
					return IObjectCreator::ParseKeyword(kw);
				}
				case EKeyword::Data:
				{
					// Create an adapter that makes the script reader look like a std::stream
					struct StreamWrapper
					{
						Reader&      m_reader; // The reader to adapt
						std::wstring m_delims; // Preserve the delimiters

						StreamWrapper(Reader& reader)
							:m_reader(reader)
							,m_delims(reader.Delimiters())
						{
							// Read up to the first non-delimiter. This is the start of the CSV data
							m_reader.IsSectionEnd();

							// Change the delimiters to suit CSV data
							m_reader.Delimiters(L"");
						}
						~StreamWrapper()
						{
							// Restore the delimiters
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

					// Data is literal data given in the script
					p.m_reader.SectionStart();
					ParseDataStream(wrap);
					p.m_reader.SectionEnd();
					return true;
				}
				case EKeyword::Source:
				{
					// Source is a file containing data
					auto filepath = p.m_reader.FilepathS();
					std::ifstream file(filepath);
					ParseDataStream(file);
					return true;
				}
			}
		}
	};

	// ELdrObject::Series
	template <> struct ObjectCreator<ELdrObject::Series> :IObjectCreator
	{
		struct DataIter
		{
			eval::IdentHash m_arghash; // The hash of the argument name
			iv2 m_idx0;                // The virtual coordinate of the iterator position
			iv2 m_step;                // The amount to advance 'idx' by with each iteration
			iv2 m_max;                 // Where iteration stops.

			DataIter(std::string const& name, iv2 max)
			{
				// Convert a name like "C32" or "R21" into an iterator into 'm_data'
				if (name.size() < 2)
					throw std::runtime_error(Fmt("Invalid series data references: '%s'", name.c_str()));
			
				bool is_column = name[0] == 'c' || name[0] == 'C';
				if (name[0] != 'c' && name[0] != 'C' && name[0] != 'r' && name[0] != 'R')
					throw std::runtime_error(Fmt("Series data reference should start with 'C' or 'R': '%s'", name.c_str()));
			
				int idx = 0;
				if (!str::ExtractIntC(idx, 10, name.c_str() + 1))
					throw std::runtime_error(Fmt("Series data references should contain an index: '%s'", name.c_str()));

				m_arghash = eval::hashname(name);
				m_idx0 = is_column ? iv2{idx, 0} : iv2{0, idx};
				m_step = is_column ? iv2{0, 1} : iv2{1, 0};
				m_max = max; //todo
			}
		};

		// Default colours to use for each series
		static inline Colour32 const colours[] =
		{
			0xFF70ad47, 0xFF4472c4, 0xFFed7d31,
			0xFF264478, 0xFF9e480e, 0xFFffc000,
			0xFF9e480e, 0xFF636363,

			//0xFF0000FF, 0xFF00FF00, 0xFFFF0000,
			//0xFF0000A0, 0xFF00A000, 0xFFA00000,
			//0xFF000080, 0xFF008000, 0xFF800000,
			//0xFF00FFFF, 0xFFFFFF00, 0xFFFF00FF,
			//0xFF00A0A0, 0xFFA0A000, 0xFFA000A0,
			//0xFF008080, 0xFF808000, 0xFF800080,
		};

		ObjectCreator<ELdrObject::Chart> const* m_chart;
		eval::Expression m_xaxis;
		eval::Expression m_yaxis;
		pr::vector<DataIter> m_xiter;
		pr::vector<DataIter> m_yiter;
		float m_line_width;
		
		ObjectCreator(ParseParams& p)
			:IObjectCreator(p)
			,m_xaxis()
			,m_yaxis()
			,m_xiter()
			,m_yiter()
		{
			// Find the ancestor chart creator
			auto const* parent = p.m_parent_creator;
			for (; parent != nullptr && parent->p.m_type != ELdrObject::Chart; parent = parent->p.m_parent_creator) {}
			if (parent != nullptr)
			{
				m_chart = static_cast<ObjectCreator<ELdrObject::Chart> const*>(parent);
			}
			else
			{
				p.ReportError(EScriptResult::SyntaxError, "Series objects must be children of a Chart object");
				throw std::runtime_error("Series objects must be children of a Chart object");
			}
		}
		double GetValue(DataIter const& iter, int i, bool& in_range) const
		{
			// Points outside the data set are considered zeros
			auto idx = iter.m_idx0 + i * iter.m_step;
			auto is_within = idx.x >= 0 && idx.x < iter.m_max.x && idx.y >= 0 && idx.y < iter.m_max.y;
			if (!is_within)
				return 0.0;

			// 'iter' still points to valid data
			in_range |= true;

			// Not jagged
			if (m_chart->m_index.empty())
				return m_chart->m_data[idx.y * m_chart->m_dim.x + idx.x];

			// If 'm_data' is a jagged array, get the number of values on the current row
			auto num_on_row = m_chart->m_index[idx.y + 1] - m_chart->m_index[idx.y];
			return idx.x < num_on_row ? m_chart->m_data[m_chart->m_index[idx.y] + idx.x] : 0.0;
		}
		bool ParseKeyword(EKeyword kw) override
		{
			switch (kw)
			{
				case EKeyword::XAxis:
				{
					std::string expr;
					p.m_reader.StringS(expr);
					m_xaxis = pr::eval::Compile(expr);
					for (auto& name : m_xaxis.m_arg_names)
						m_xiter.push_back(DataIter{name, m_chart->m_dim});
					
					return true;
				}
				case EKeyword::YAxis:
				{
					std::string expr;
					p.m_reader.StringS(expr);
					m_yaxis = pr::eval::Compile(expr);
					for (auto& name : m_yaxis.m_arg_names)
						m_yiter.push_back(DataIter{name, m_chart->m_dim});

					return true;
				}
				case EKeyword::Width:
				{
					p.m_reader.RealS(m_line_width);
					return true;
				}
				default:
				{
					return IObjectCreator::ParseKeyword(kw);
				}
			}
		}
		void CreateModel(LdrObject* obj) override
		{
			// Determine the index of this series within the chart
			int child_index = 0;
			for (auto& child : p.m_parent->m_child)
				child_index += int(child->m_type == ELdrObject::Series);

			// Generate a name if none given
			if (!AllSet(p.m_flags, EFlags::ExplicitName))
			{
				obj->m_name = FmtS("Series %d", child_index);
			}

			// Assign a colour if none given
			if (!AllSet(p.m_flags, EFlags::ExplicitColour))
			{
				obj->m_base_colour = colours[child_index % _countof(colours)];
			}

			auto& verts = p.m_cache.m_point;
			verts.clear();

			// Merge the args from both expressions
			auto args = eval::ArgSet{};
			args.add(m_xaxis.m_args);
			args.add(m_yaxis.m_args);

			// Iterate over the data points
			for (int i = 0;;++i)
			{
				// Initialise the expression arguments for 'i'
				bool in_range = false;
				for (auto& iter : m_xiter)
					args.set(iter.m_arghash, GetValue(iter, i, in_range));
				for (auto& iter : m_yiter)
					args.set(iter.m_arghash, GetValue(iter, i, in_range));
				if (!in_range)
					break;

				// Evaluate the data point at 'i'
				auto x = m_xaxis(args);
				auto y = m_yaxis(args);
				verts.push_back(v4{static_cast<float>(x.db()), static_cast<float>(y.db()), 0, 1});
			}
				
			// Create a plot from the points
			if (!verts.empty())
			{
				obj->m_model = ModelGenerator::LineStrip(p.m_rdr, int(verts.size()) - 1, verts.data(), 1, &obj->m_base_colour);
				obj->m_model->m_name = obj->TypeAndName();

				// Use thick lines
				if (m_line_width > 1.0f)
				{
					// Get or create an instance of the thick line shader
					auto shdr = Shader::Create<shaders::ThickLineListGS>(m_line_width);
					for (auto& nug : obj->m_model->m_nuggets)
						nug.m_shaders.push_back({ERenderStep::RenderForward, shdr});
				}
			}
		}
	};

	// ELdrObject::Model
	template <> struct ObjectCreator<ELdrObject::Model> :IObjectCreator
	{
		std::filesystem::path m_filepath;
		creation::GenNorms m_gen_norms;
		creation::BakeTransform m_bake;
		int m_part;

		ObjectCreator(ParseParams& p)
			:IObjectCreator(p)
			,m_filepath()
			,m_bake()
			,m_gen_norms()
		{}
		bool ParseKeyword(EKeyword kw) override
		{
			return
				m_bake.ParseKeyword(p, kw) ||
				m_gen_norms.ParseKeyword(p, kw) ||
				IObjectCreator::ParseKeyword(kw);
		}
		void Parse() override
		{
			std::wstring filepath;
			p.m_reader.String(filepath);
			m_filepath = filepath;
		}
		void CreateModel(LdrObject* obj) override
		{
			using namespace pr::geometry;
			using namespace std::filesystem;

			// Validate
			if (m_filepath.empty())
			{
				p.ReportError(EScriptResult::Failed, "Model filepath not given");
				return;
			}

			// Determine the format from the file extension
			auto format = GetModelFormat(m_filepath);
			if (format == EModelFileFormat::Unknown)
			{
				auto msg = Fmt("Model file '%S' is not supported.\nSupported Formats: ", m_filepath.c_str());
				for (auto f : Enum<EModelFileFormat>::Members()) msg.append(Enum<EModelFileFormat>::ToStringA(f)).append(" ");
				p.ReportError(EScriptResult::Failed, msg.c_str());
				return;
			}

			// Ask the include handler to turn the filepath into a stream.
			// Load the stream in binary mode. The model loading functions can convert binary to text if needed.
			auto src = p.m_reader.Includes().OpenStreamA(m_filepath, EIncludeFlags::Binary);
			if (!src || !*src)
			{
				p.ReportError(EScriptResult::Failed, FmtS("Failed to open file stream '%s'", m_filepath.c_str()));
				return;
			}

			// Attach a texture filepath resolver
			auto search_paths = std::vector<path>
			{
				path(m_filepath.string() + ".textures"),
				m_filepath.parent_path(),
			};
			AutoSub sub = p.m_rdr.res_mgr().ResolveFilepath += [&](auto&, ResolvePathArgs& args)
			{
				// Look in a folder with the same name as the model
				auto resolved = pr::filesys::ResolvePath<std::vector<path>>(args.filepath, search_paths, nullptr, false, nullptr);
				if (!exists(resolved)) return;
				args.filepath = resolved;
				args.handled = true;
			};

			ModelGenerator::CreateOptions m_opts = {};
			if (m_bake) m_opts.m_bake = &m_bake.m_bake;


			// Create the models
			ModelGenerator::LoadModel(format, p.m_rdr, *src, [&](ModelTree const& tree)
				{
					auto child = ModelTreeToLdr(tree, obj->m_context_id);
					if (child != nullptr) obj->AddChild(child);
					return false;
				}, m_opts);
		}

		// Convert a model tree into a tree of LdrObjects
		static LdrObjectPtr ModelTreeToLdr(ModelTree const& tree, Guid const& context_id)
		{
			std::vector<LdrObjectPtr> stack;
			for (auto& node : tree)
			{
				// If 'node' is at the same level or above the current leaf,
				// then all the children for that leaf have been added.
				for (size_t lvl = s_cast<size_t>(node.m_level + 1); stack.size() > lvl; )
					stack.pop_back();

				// Create an LdrObject for each model
				ObjectAttributes attr{ELdrObject::Model, node.m_model->m_name.c_str()};
				LdrObjectPtr obj(new LdrObject(attr, nullptr, context_id), true);
				obj->m_model = node.m_model;
				obj->m_o2p = node.m_o2p;

				// Add 'obj' as a child of the current leaf node
				if (!stack.empty())
					stack.back()->AddChild(obj);

				// Add 'obj' as the current leaf node
				stack.push_back(obj);
			}
			return !stack.empty() ? stack[0] : nullptr;
		}
	};

	// ELdrObject::Equation
	template <> struct ObjectCreator<ELdrObject::Equation> :IObjectCreator
	{
		struct Extras
		{
			using VCPair = struct { float m_value; Colour32 m_colour; };
			using ColourBands = pr::vector<VCPair>;
			struct Axis
			{
				float m_min;
				float m_max;
				ColourBands m_col;

				Axis()
					:m_min(maths::float_max)
					,m_max(maths::float_lowest)
					,m_col()
				{}
				bool limited() const
				{
					return m_min <= m_max;
				}
				float centre() const
				{
					return (m_min + m_max) * 0.5f;
				}
				float radius() const
				{
					return Abs(m_max - m_min) * 0.5f;
				}
				VCPair clamp(float value) const
				{
					VCPair vc = {value, Colour32White};

					// Clamp the range
					if (m_min <= m_max)
					{
						vc.m_value = Clamp(value, m_min, m_max);
						if (vc.m_value != value)
							vc.m_colour.a = 0;
					}

					// Lerp the colour
					if (!m_col.empty() && vc.m_value == value)
					{
						int i = 0, iend = static_cast<int>(m_col.size());
						for (; i != iend && vc.m_value >= m_col[i].m_value; ++i) {}
						if      (i ==    0) vc.m_colour = m_col.front().m_colour;
						else if (i == iend) vc.m_colour = m_col.back().m_colour;
						else
						{
							auto f = Frac(m_col[i - 1].m_value, vc.m_value, m_col[i].m_value);
							vc.m_colour = Lerp(m_col[i - 1].m_colour, m_col[i].m_colour, f);
						}
					}

					return vc;
				}
				static Axis Parse(Reader& reader)
				{
					Axis axis;
					reader.SectionStart();
					for (; !reader.IsSectionEnd(); )
					{
						if (!reader.IsKeyword())
						{
							reader.Real(axis.m_min);
							reader.Real(axis.m_max);
						}
						else
						{
							auto kw = reader.NextKeywordH<EKeyword>();
							switch (kw)
							{
							case EKeyword::Range:
								{
									reader.SectionStart();
									reader.Real(axis.m_min);
									reader.Real(axis.m_max);
									reader.SectionEnd();
									break;
								}
							case EKeyword::Colours:
								{
									reader.SectionStart();
									for (; !reader.IsSectionEnd(); )
									{
										VCPair vcpair;
										reader.Real(vcpair.m_value);
										reader.Int(vcpair.m_colour.argb, 16);
										axis.m_col.push_back(vcpair);
									}
									reader.SectionEnd();
									sort(axis.m_col, [](auto& l, auto& r) { return l.m_value < r.m_value; });
									break;
								}
							}
						}
					}
					reader.SectionEnd();
					return axis;
				}
			};

			std::array<Axis, 3> m_axis;
			float m_weight;
			Extras()
				:m_axis()
				,m_weight(0.5f)
			{}
			bool has_alpha() const
			{
				for (auto& axis : m_axis)
				{
					for (auto& col : axis.m_col)
					{
						if (col.m_colour.a == 0xFF) continue;
						return true;
					}
				}
				return false;
			}
			BBox clamp_range(BBox range) const
			{
				for (int i = 0, iend = static_cast<int>(m_axis.size()); i != iend; ++i)
				{
					auto& axis = m_axis[i];
					if (!axis.limited()) continue;
					range.m_centre[i] = axis.centre();
					range.m_radius[i] = axis.radius();
				}
				return range;
			}
		};
		struct Cache
		{
			BBox m_range; // The last size of the rendered equation

			Cache()
				:m_range(BBox::Reset())
			{}
		};

		eval::Expression m_eq;
		eval::ArgSet m_args;
		int m_resolution;
		Extras m_extras;

		ObjectCreator(ParseParams& p)
			:IObjectCreator(p)
			,m_eq()
			,m_args()
			,m_resolution(10000)
			,m_extras()
		{}
		bool ParseKeyword(EKeyword kw) override
		{
			switch (kw)
			{
				case EKeyword::Resolution:
				{
					p.m_reader.IntS(m_resolution, 10);
					m_resolution = std::clamp(m_resolution, 8, 0xFFFF);
					return true;
				}
				case EKeyword::Param:
				{
					std::string name;
					double value;
					p.m_reader.SectionStart();
					p.m_reader.String(name);
					p.m_reader.Real(value);
					p.m_reader.SectionEnd();
					m_args.add(name, value);
					return true;
				}
				case EKeyword::Weight:
				{
					p.m_reader.RealS(m_extras.m_weight);
					return true;
				}
				case EKeyword::XAxis:
				{
					m_extras.m_axis[0] = Extras::Axis::Parse(p.m_reader);
					return true;
				}
				case EKeyword::YAxis:
				{
					m_extras.m_axis[1] = Extras::Axis::Parse(p.m_reader);
					return true;
				}
				case EKeyword::ZAxis:
				{
					m_extras.m_axis[2] = Extras::Axis::Parse(p.m_reader);
					return true;
				}
				default:
				{
					return
						IObjectCreator::ParseKeyword(kw);
				}
			}
		}
		void Parse() override
		{
			// Read the equation expression
			std::wstring equation;
			p.m_reader.String(equation);

			// Compile the equation
			try { m_eq = eval::Compile<wchar_t>(equation); }
			catch (std::exception const& ex)
			{
				p.ReportError(EScriptResult::ExpressionSyntaxError, Fmt("Equation expression is invalid: %s", ex.what()));
				return;
			}
		}
		void CreateModel(LdrObject* obj) override
		{
			using namespace pr::geometry;

			// Validate
			if (!m_eq)
			{
				p.ReportError(EScriptResult::Failed, "Equation not given");
				return;
			}

			// Apply any constants
			m_eq.m_args.add(m_args);

			// Update the model before each render so the range depends on the visible area at the focus point
			obj->OnAddToScene += UpdateModel;

			// Choose suitable 'vcount, icount' based on the equation dimension and resolution
			int vcount, icount, dim = m_eq.m_args.unassigned_count();
			switch (dim)
			{
				case 1: vcount = icount = m_resolution; break;
				case 2: vcount = m_resolution; icount = 2 * m_resolution; break;
				case 3: vcount = icount = m_resolution; break;
				default: throw std::runtime_error(Fmt("Unsupported equation dimension: %d", dim));
			}

			// Create buffers for a dynamic model
			ModelDesc mdesc(
				ResDesc::Buf<Vert>(vcount, nullptr),
				ResDesc::Buf<uint32_t>(icount, nullptr),
				BBox::Reset(), obj->TypeAndName().c_str());

			// Create the model
			obj->m_model = p.m_rdr.res_mgr().CreateModel(mdesc);

			// Store the expression in the object user data
			obj->m_user_data.get<eval::Expression>() = m_eq;
			obj->m_user_data.get<Extras>() = m_extras;
			obj->m_user_data.get<Cache>() = Cache{};
		}

		// Generate the model based on the visible range
		static void UpdateModel(LdrObject& ob, Scene const& scene)
		{
			// Notes:
			//  - This code attempts to give the effect of an infinite function or surface by creating graphics
			//    within the view frustum as the camera moves. It evaluates the equation within a cube centred
			//    on the focus point.
			//  - no back-face culling
			//  - only update the model when the camera moves by ? distance.
			//  - functions can have infinities and divide by zero
			//  - set the bbox to match the view volume so that auto range doesn't zoom out to infinity

			auto& model = *ob.m_model.get();
			auto& equation = ob.m_user_data.get<eval::Expression>();
			auto& extras = ob.m_user_data.get<Extras>();
			auto& cache = ob.m_user_data.get<Cache>();
			auto init = model.m_nuggets.empty();

			// Find the range to plot the equation over
			auto& cam = scene.m_cam;
			auto fp = cam.FocusPoint();
			auto area = cam.ViewArea(cam.FocusDist());

			// Determine the interval to plot within. Default to a sphere around the focus point.
			auto range = BBox(fp, v4(area.x, area.x, area.x, 0));
			range = extras.clamp_range(range);

			// Only update the model if necessary
			if (init || !IsWithin(cache.m_range, range))
			{
				// Functions can have infinities and divide by zeros. Set the bbox
				// to match the view volume so that auto-range doesn't zoom out to infinity.
				model.m_bbox.reset();
				model.m_bbox = BBox::Unit();

				// Update the model by evaluating the equation
				switch (equation.m_args.unassigned_count())
				{
					case 1: // Line plot
					{
						LinePlot(model, range, equation, extras, init);
						break;
					}
					case 2: // Surface plot
					{
						SurfacePlot(model, range, equation, extras, init);
						break;
					}
					case 3:
					{
						CloudPlot(model, range, equation, extras, init);
						break;
					}
					default:
					{
						assert(false && "Unsupported equation dimension");
						break;
					}
				}

				// Save the range last rendered
				cache.m_range = range;
			}

			// Update object colour, visibility, etc
			ApplyObjectState(&ob);
		}
		static void LinePlot(Model& model, BBox_cref range, eval::Expression const& equation, Extras const& extras, bool init)
		{
			// Notes:
			//  - 'range' is the independent variable range. For line plots, only 'x' is used
			//  - 'extras.m_axis' contains the bounds on output values and colour gradients.
			auto vcount = s_cast<int>(model.m_vcount);
			auto icount = s_cast<int>(model.m_icount);
			auto count = std::min(vcount, icount);
			
			(void)equation, extras, range;
			#if 0 //todo
			// Populate verts
			{
				Lock vlock;
				model.MapVerts(vlock, EMap::WriteDiscard);
				auto vout = vlock.ptr<Vert>();
				for (int i = 0; i != count; ++i)
				{
					// 'f' is a point in the range [-1.0,+1.0]. Rescale to the range.
					// 'weight' controls the density of points near the range centre.
					auto f = Lerp(-1.0f, +1.0f, (1.0f * i) / (count - 1));
					auto x = range.m_centre.x + f * range.m_radius.x * Pow(Abs(f), extras.m_weight);
					auto [y,col] = extras.m_axis[1].clamp(static_cast<float>(equation(x).db()));
					SetPC(*vout, v4(x, y, 0, 1), col);
					++vout;
				}
			}

			// Populate faces
			if (init)
			{
				Lock ilock;
				model.MapIndices(ilock, EMap::WriteDiscard);
				auto iout = ilock.ptr<uint32_t>();
				for (int i = 0; i != count; ++i)
				{
					*iout = static_cast<uint32_t>(i);
					++iout;
				}
			}
			#endif

			// Populate nuggets
			if (init)
			{
				// Create a nugget
				NuggetData n = {};
				n.m_topo = ETopo::LineStrip;
				n.m_geom = EGeom::Vert;
				n.m_vrange = Range(0, count);
				n.m_irange = Range(0, count);
				n.m_nflags = extras.has_alpha() ? ENuggetFlag::GeometryHasAlpha : ENuggetFlag::None;
				model.DeleteNuggets();
				model.CreateNugget(n);
			}
		}
		static void SurfacePlot(Model& model, BBox_cref range, eval::Expression const& equation, Extras const& extras, bool init)
		{
			// Notes:
			//  - 'range' is the independent variable range. For surface plots, 'x' and 'y' are used.
			//  - 'extras.m_axis' contains the bounds on output values and colour gradients.

			// Determine the largest hex patch that can be made with the available model size:
			//  i.e. solve for the minimum value for 'rings' in:
			//    vcount = ArithmeticSum(0, 6, rings) + 1;
			//    icount = ArithmeticSum(0, 12, rings) + 2*rings;
			// ArithmeticSum := (n + 1) * (a0 + an) / 2, where an = (a0 + n * step)
			//    3r� + 3r + 1-vcount = 0  =>  r = (-3 � sqrt(-3 + 12*vcount)) / 6
			//    6r� + 8r - icount = 0    =>  r = (-8 � sqrt(64 + 24*icount)) / 12
			auto vrings = (-3 + sqrt(-3 + 12 * model.m_vcount)) / 6;
			auto irings = (-8 + sqrt(64 + 24 * model.m_icount)) / 12;
			auto rings = static_cast<int>(std::min(vrings, irings));

			auto [nv, ni] = geometry::HexPatchSize(rings);
			assert(nv <= s_cast<int>(model.m_vcount));
			assert(ni <= s_cast<int>(model.m_icount));

			// Evaluate the normal at (x,y)
			auto norm = [&](float x, float y)
			{
				// Smallest non-zero of (x,y)
				auto d =
					(x != 0 && y != 0) ? Min(Abs(x), Abs(y)) * 0.00001f :
					(x != 0 || y != 0) ? Max(Abs(x), Abs(y)) * 0.00001f :
					maths::tinyf;
				auto pt = equation(v4(x - d, x + d, x, x), v4(y, y, y - d, y + d)).v4();
				auto n = Cross(v4(2*d, 0, pt.y - pt.x, 0), v4(0, 2*d, pt.w - pt.z, 0));
				return Normalise(n, v4Zero);
			};

			(void)range, equation;
			#if 0 //todo
			MLock mlock(&model, EMap::WriteDiscard);
			auto vout = mlock.m_vlock.ptr<Vert>();
			auto iout = mlock.m_ilock.ptr<uint32_t>();
			auto props = geometry::HexPatch(rings,
				[&](v4_cref<> pos, Colour32, v4_cref<>, v2_cref<>)
				{
					// Evaluate the function at points around the focus point, but shift them back
					// so the focus point is centred around (0,0,0), then set the o2w transform

					// 'pos' is a point in the range [-1.0,+1.0]. Rescale to the range.
					// 'weight' controls the density of points near the range centre.
					auto dir = pos.w0();
					auto len_sq = LengthSq(dir);
					auto pt = range.Centre() + dir * range.Radius() * Pow(len_sq, extras.m_weight * 0.5f);
					auto [z,col] = extras.m_axis[2].clamp(static_cast<float>(equation(pt.x, pt.y).db()));
					SetPCNT(*vout++, v4(pt.x, pt.y, z, 1), col, norm(pt.x, pt.y), v2Zero);
				},
				[&](auto idx)
				{
					*iout++ = idx;
				});
			#endif

			// Generate nuggets if initialising
			if (init)
			{
				auto [vcount, icount] = geometry::HexPatchSize(rings);

				NuggetData n = {};
				n.m_topo = ETopo::TriStrip;
				//todo n.m_geom = props.m_geom;
				n.m_vrange = Range(0, vcount);
				n.m_irange = Range(0, icount);
				n.m_nflags = extras.has_alpha() ? ENuggetFlag::GeometryHasAlpha : ENuggetFlag::None;
				model.CreateNugget(n);
			}
		}
		static void CloudPlot(Model& model, BBox_cref range, eval::Expression const& equation, Extras const& extras, bool init)
		{
			(void)model, range, equation, extras, init;
			throw std::runtime_error("Plots of 3 independent variables are not supported");
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
							case HashI("left"          ): m_layout.m_align_h       = DWRITE_TEXT_ALIGNMENT_LEADING; break;
							case HashI("centreh"       ): m_layout.m_align_h       = DWRITE_TEXT_ALIGNMENT_CENTER; break;
							case HashI("right"         ): m_layout.m_align_h       = DWRITE_TEXT_ALIGNMENT_TRAILING; break;
							case HashI("top"           ): m_layout.m_align_v       = DWRITE_PARAGRAPH_ALIGNMENT_NEAR; break;
							case HashI("centrev"       ): m_layout.m_align_v       = DWRITE_PARAGRAPH_ALIGNMENT_CENTER; break;
							case HashI("bottom"        ): m_layout.m_align_v       = DWRITE_PARAGRAPH_ALIGNMENT_FAR; break;
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
					m_layout.m_padding.left = padding.x;
					m_layout.m_padding.top = padding.y;
					m_layout.m_padding.right = padding.z;
					m_layout.m_padding.bottom = padding.w;
					return true;
				}
				case EKeyword::Dim:
				{
					p.m_reader.Vector2S(m_layout.m_dim);
					return true;
				}
				default:
				{
					return
						m_axis.ParseKeyword(p, kw) ||
						IObjectCreator::ParseKeyword(kw);
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
			obj->m_model = ModelGenerator::Text(p.m_rdr, m_text, m_fmt.data(), int(m_fmt.size()), m_layout, m_axis.m_align);
			obj->m_model->m_name = obj->TypeAndName();

			// Create the model
			switch (m_type)
			{
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
					obj->Flags(ELdrFlags::SceneBoundsExclude, true, "");

					// Update the rendering 'i2w' transform on add-to-scene
					obj->OnAddToScene += [](LdrObject& ob, Scene const& scene)
					{
						auto c2w = scene.m_cam.CameraToWorld();
						auto w2c = scene.m_cam.WorldToCamera();
						auto w = 1.0f * scene.m_viewport.ScreenW;
						auto h = 1.0f * scene.m_viewport.ScreenH;
						#if PR_DBG
						if (w < 1.0f || h < 1.0f)
							throw std::runtime_error("Invalid viewport size");
						#endif

						// Create a camera with an aspect ratio that matches the viewport
						auto& m_camera = static_cast<Camera const&>(scene.m_cam);
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
					static constexpr int ViewPortSize = 1024;

					// Do not include in scene bounds calculations because we're scaling
					// this model at a point that the bounding box calculation can't see.
					obj->Flags(
						ELdrFlags::BBoxExclude |
						ELdrFlags::SceneBoundsExclude |
						ELdrFlags::HitTestExclude |
						ELdrFlags::ShadowCastExclude, true, "");

					// Screen space uses a standard normalised orthographic projection
					obj->m_c2s = m4x4::ProjectionOrthographic(float(ViewPortSize), float(ViewPortSize), -0.01f, 1, true);

					// Update the rendering 'i2w' transform on add-to-scene.
					obj->OnAddToScene += [](LdrObject& ob, Scene const& scene)
					{
						// The 'ob.m_i2w' is a normalised screen space position
						// (-1,-1,-0) is the lower left corner on the near plane,
						// (+1,+1,-1) is the upper right corner on the far plane.
						auto w = 1.0f * scene.m_viewport.ScreenW;
						auto h = 1.0f * scene.m_viewport.ScreenH;
						auto c2w = scene.m_cam.CameraToWorld();
						#if PR_DBG
						if (w < 1.0f || h < 1.0f)
							throw std::runtime_error("Invalid viewport size");
						#endif

						// Scale the object from physical pixels to normalised screen space
						auto scale = m4x4::Scale(ViewPortSize / w, ViewPortSize / h, 1, v4Origin);

						// Reverse 'pos.z' so positive values can be used
						ob.m_i2w.pos.x *= 0.5f * ViewPortSize;
						ob.m_i2w.pos.y *= 0.5f * ViewPortSize;

						// Convert 'i2w', which is being interpreted as 'i2c', into an actual 'i2w'
						ob.m_i2w = c2w * ob.m_i2w * scale;
					};
					break;
				}
				default:
				{
					throw std::runtime_error(FmtS("Unknown Text mode: %d", m_type));
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
				p.ReportError(EScriptResult::UnknownValue, "Instance not found");
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
		p.m_type = ShapeType;

		// Read the object attributes: name, colour, instance
		auto attr = ParseAttributes(p);
		auto obj  = LdrObjectPtr(new LdrObject(attr, p.m_parent, p.m_context_id), true);

		// Push a font onto the font stack, so that fonts are scoped to object declarations
		auto font_scope = Scope<void>(
			[&]{ p.m_font.push_back(p.m_font.back()); },
			[&]{ p.m_font.pop_back(); });

		// Create an object creator for the given type
		ObjectCreator<ShapeType> creator(p);

		// Read the description of the model
		p.m_reader.SectionStart();
		for (;!p.m_cancel && !p.m_reader.IsSectionEnd();)
		{
			// Check for incomplete script
			if (p.m_reader.IsSourceEnd())
			{
				p.ReportError(EScriptResult::UnexpectedEndOfFile);
				break;
			}

			// Handle keywords
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
				ParseParams pp(p, obj->m_child, obj.get(), &creator);
				if (ParseLdrObject((ELdrObject)kw, pp))
					continue;

				// Unknown token
				p.ReportError(EScriptResult::UnknownToken);
				continue;
			}

			// Not a keyword, let the object creator interpret it
			creator.Parse();
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
	template <> void Parse<ELdrObject::Unknown>(ParseParams&) {}
	template <> void Parse<ELdrObject::Custom>(ParseParams&) {}

	// Reads a single ldr object from a script adding object (+ children) to 'p.m_objects'.
	// Returns true if an object was read or false if the next keyword is unrecognised
	bool ParseLdrObject(ELdrObject type, ParseParams& p)
	{
		// Save the current number of objects
		auto object_count = p.m_objects.size();

		// Parse the object
		switch (type)
		{
			#define PR_LDR_PARSE_IMPL(name, hash) case ELdrObject::name: Parse<ELdrObject::name>(p); break;
			#define PR_LDR_PARSE(x) x(PR_LDR_PARSE_IMPL)
			PR_LDR_PARSE(PR_ENUM_LDROBJECTS)
			default: return false;
		}

		// Apply properties to each object added
		// This is done after objects are parsed so that recursive properties can be applied
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
		for (HashValue32 kw; !p.m_cancel && p.m_reader.NextKeywordH(kw);)
		{
			switch ((EKeyword)kw)
			{
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
				default:
				{
					// Save the current number of objects
					auto object_count = int(p.m_objects.size());

					// Assume the keyword is an object and start parsing
					if (!ParseLdrObject(static_cast<ELdrObject>(kw), p))
					{
						p.ReportError(EScriptResult::UnknownToken, Fmt("Expected an object declaration"));
						break;
					}
					assert("Objects removed but 'ParseLdrObject' didn't fail" && int(p.m_objects.size()) > object_count);

					// Call the callback with the freshly minted object.
					add_cb(object_count);
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
		auto exit = Scope<void>(
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
	LdrObjectPtr Create(Renderer& rdr, ObjectAttributes attr, MeshCreationData const& cdata, Guid const& context_id)
	{
		LdrObjectPtr obj(new LdrObject(attr, nullptr, context_id), true);

		// Create the model
		obj->m_model = ModelGenerator::Mesh(rdr, cdata);
		obj->m_model->m_name = obj->TypeAndName();
		return obj;
	}

	// Create an ldr object from a p3d model.
	LdrObjectPtr CreateP3D(Renderer& rdr, ObjectAttributes attr, std::filesystem::path const& p3d_filepath, Guid const& context_id)
	{
		LdrObjectPtr obj(new LdrObject(attr, nullptr, context_id), true);

		// Create the model
		std::ifstream src(p3d_filepath, std::ios::binary);
		ModelGenerator::LoadP3DModel(rdr, src, [&](ModelTree const& tree)
			{
				auto child = ObjectCreator<ELdrObject::Model>::ModelTreeToLdr(tree, obj->m_context_id);
				if (child != nullptr) obj->AddChild(child);
				return false;
			});

		return obj;
	}
	LdrObjectPtr CreateP3D(Renderer& rdr, ObjectAttributes attr, size_t size, void const* p3d_data, Guid const& context_id)
	{
		LdrObjectPtr obj(new LdrObject(attr, nullptr, context_id), true);

		// Create the model
		pr::mem_istream<char> src(p3d_data, size);
		ModelGenerator::LoadP3DModel(rdr, src, [&](ModelTree const& tree)
			{
				auto child = ObjectCreator<ELdrObject::Model>::ModelTreeToLdr(tree, obj->m_context_id);
				if (child != nullptr) obj->AddChild(child);
				return false;
			});

		return obj;
	}

	// Create an instance of an existing ldr object.
	LdrObjectPtr CreateInstance(LdrObject const* existing)
	{
		ObjectAttributes attr(existing->m_type, existing->m_name.c_str(), existing->m_base_colour);
		LdrObjectPtr obj(new LdrObject(attr, nullptr, existing->m_context_id), true);

		// Use the same model
		obj->m_model = existing->m_model;

		// Recursively create instances of the child objects
		for (auto& child : existing->m_child)
			obj->m_child.push_back(CreateInstance(child.get()));

		return obj;
	}

	// Create an ldr object using a callback to populate the model data.
	// Objects created by this method will have dynamic usage and are suitable for updating every frame via the 'Edit' function.
	LdrObjectPtr CreateEditCB(Renderer& rdr, ObjectAttributes attr, int vcount, int icount, int ncount, EditObjectCB edit_cb, void* ctx, Guid const& context_id)
	{
		LdrObjectPtr obj(new LdrObject(attr, 0, context_id), true);

		// Create buffers for a dynamic model
		ModelDesc settings(
			ResDesc::Buf<Vert>(vcount, nullptr),
			ResDesc::Buf<uint16_t>(icount, nullptr),
			BBox::Reset(), obj->TypeAndName().c_str());
		settings.m_vb.HeapProps = HeapProps::Upload();
		settings.m_ib.HeapProps = HeapProps::Upload();

		// Create the model
		obj->m_model = rdr.res_mgr().CreateModel(settings);

		// Create dummy nuggets
		NuggetData nug(ETopo::PointList, EGeom::Vert);
		nug.m_nflags = SetBits(nug.m_nflags, ENuggetFlag::RangesCanOverlap, true);
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
				std::swap(object->m_pso, rhs->m_pso);
				std::swap(object->m_iflags, rhs->m_iflags);
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
				std::swap(object->m_ldr_flags, rhs->m_ldr_flags);
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
	}

	// Remove all objects from 'objects' that have a context id matching one in 'incl' and not in 'excl'
	// If 'incl' is empty, all are assumed included. If 'excl' is empty, none are assumed excluded.
	// 'excl' is considered after 'incl' so if any context ids are in both arrays, they will be excluded.
	void Remove(ObjectCont& objects, std::span<Guid const> incl, std::span<Guid const> excl)
	{
		erase_if_unstable(objects, [=](auto& ob)
		{
			if (!incl.empty() && !contains(incl, ob->m_context_id)) return false; // not in the doomed list
			if (!excl.empty() &&  contains(excl, ob->m_context_id)) return false; // saved by exclusion
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
		,m_ldr_flags(ELdrFlags::None)
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
	void LdrObject::AddToScene(Scene& scene, float time_s, m4x4 const* p2w, ELdrFlags parent_flags)
	{
		// Set the instance to world.
		// Take a copy in case the 'OnAddToScene' event changes it.
		// We want parenting to be unaffected by the event handlers.
		auto i2w = *p2w * m_o2p * m_anim.Step(time_s);
		m_i2w = i2w;

		// Combine recursive flags
		auto flags = m_ldr_flags | (parent_flags & (ELdrFlags::Hidden|ELdrFlags::Wireframe|ELdrFlags::NonAffine));
		PR_ASSERT(PR_DBG, AllSet(flags, ELdrFlags::NonAffine) || IsAffine(m_i2w), "Invalid instance transform");

		// Allow the object to change it's transform just before rendering
		OnAddToScene(*this, scene);

		// Add the instance to the scene drawlist
		if (m_model && !AllSet(flags, ELdrFlags::Hidden))
		{
			// Could add occlusion culling here...
			scene.AddInstance(*this);
		}

		// Rinse and repeat for all children
		for (auto& child : m_child)
			child->AddToScene(scene, time_s, &i2w, flags);
	}

	// Recursively add this object using 'bbox_model' instead of its
	// actual model, located and scaled to the transform and box of this object
	void LdrObject::AddBBoxToScene(Scene& scene, float time_s, m4x4 const* p2w, ELdrFlags parent_flags)
	{
		// Set the instance to world for this object
		auto i2w = *p2w * m_o2p * m_anim.Step(time_s);

		// Combine recursive flags
		auto flags = m_ldr_flags | (parent_flags & (ELdrFlags::Hidden|ELdrFlags::Wireframe|ELdrFlags::NonAffine));
		PR_ASSERT(PR_DBG, AllSet(flags, ELdrFlags::NonAffine) || IsAffine(m_i2w), "Invalid instance transform");

		// Add the bbox instance to the scene drawlist
		if (m_model && !AnySet(flags, ELdrFlags::Hidden|ELdrFlags::SceneBoundsExclude))
		{
			// Find the object to world for the bbox
			m_bbox_instance.m_model = scene.rdr().res_mgr().FindModel(EStockModel::BBoxModel);
			m_bbox_instance.m_i2w = i2w * BBoxTransform(m_model->m_bbox);
			scene.AddInstance(m_bbox_instance);
		}

		// Rinse and repeat for all children
		for (auto& child : m_child)
			child->AddBBoxToScene(scene, time_s, &i2w, flags);
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
		return obj ? !AllSet(obj->m_ldr_flags, ELdrFlags::Hidden) : false;
	}
	void LdrObject::Visible(bool visible, char const* name)
	{
		Flags(ELdrFlags::Hidden, !visible, name);
	}

	// Get/Set the render mode for this object or child objects matching 'name' (see Apply)
	bool LdrObject::Wireframe(char const* name) const
	{
		auto obj = Child(name);
		return obj ? AllSet(obj->m_ldr_flags, ELdrFlags::Wireframe) : false;
	}
	void LdrObject::Wireframe(bool wireframe, char const* name)
	{
		Flags(ELdrFlags::Wireframe, wireframe, name);
	}
	
	// Get/Set the visibility of normals for this object or child objects matching 'name' (see Apply)
	bool LdrObject::Normals(char const* name) const
	{
		auto obj = Child(name);
		return obj ? AllSet(obj->m_ldr_flags, ELdrFlags::Normals) : false;
	}
	void LdrObject::Normals(bool show, char const* name)
	{
		Flags(ELdrFlags::Normals, show, name);
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
				static constexpr int ViewPortSize = 2;

				// Do not include in scene bounds calculations because we're scaling
				// this model at a point that the bounding box calculation can't see.
				o->m_ldr_flags = SetBits(o->m_ldr_flags, ELdrFlags::SceneBoundsExclude, true);

				// Update the rendering 'i2w' transform on add-to-scene.
				o->m_screen_space = o->OnAddToScene += [](LdrObject& ob, Scene const& scene)
				{
					// The 'ob.m_i2w' is a normalised screen space position
					// (-1,-1,-0) is the lower left corner on the near plane,
					// (+1,+1,-1) is the upper right corner on the far plane.
					auto w = float(scene.m_viewport.Width);
					auto h = float(scene.m_viewport.Height);
					auto c2w = scene.m_cam.CameraToWorld();

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
				o->m_ldr_flags = SetBits(o->m_ldr_flags, ELdrFlags::SceneBoundsExclude, false);
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
		return obj ? obj->m_ldr_flags : ELdrFlags::None;
	}
	void LdrObject::Flags(ELdrFlags flags, bool state, char const* name)
	{
		Apply([=](LdrObject* o)
		{
			// Apply flag changes
			o->m_ldr_flags = SetBits(o->m_ldr_flags, flags, state);

			// Hidden
			if (o->m_model != nullptr)
			{
				// Even though Ldraw doesn't add instances that are hidden,
				// set the visibility flags on the nuggets for consistency.
				auto hidden = AllSet(o->m_ldr_flags, ELdrFlags::Hidden);
				for (auto& nug : o->m_model->m_nuggets)
					nug.m_nflags = SetBits(nug.m_nflags, ENuggetFlag::Hidden, hidden);
			}

			// Wireframe
			if (AllSet(o->m_ldr_flags, ELdrFlags::Wireframe))
			{
				o->m_pso.Set<EPipeState::FillMode>(D3D12_FILL_MODE_WIREFRAME);
			}
			else
			{
				o->m_pso.Clear<EPipeState::FillMode>();
			}

			// No Z Test
			if (AllSet(o->m_ldr_flags, ELdrFlags::NoZTest))
			{
				// Don't test against Z, and draw above all objects
				o->m_pso.Set<EPipeState::DepthEnable>(FALSE);
				o->m_sko.Group(ESortGroup::PostAlpha);
			}
			else
			{
				o->m_pso.Set<EPipeState::DepthEnable>(TRUE);
				o->m_sko = SKOverride();
			}

			// If NoZWrite
			if (AllSet(o->m_ldr_flags, ELdrFlags::NoZWrite))
			{
				// Don't write to Z and draw behind all objects
				o->m_pso.Set<EPipeState::DepthWriteMask>(D3D12_DEPTH_WRITE_MASK_ZERO);
				o->m_sko.Group(ESortGroup::PreOpaques);
			}
			else
			{
				o->m_pso.Set<EPipeState::DepthWriteMask>(D3D12_DEPTH_WRITE_MASK_ALL);
				o->m_sko = SKOverride();
			}

			// Normals
			if (o->m_model != nullptr)
			{
				auto show_normals = AllSet(o->m_ldr_flags, ELdrFlags::Normals);
				ShowNormals(o->m_model.get(), show_normals);
			}

			// Shadow cast
			{
				auto vampire = AllSet(o->m_ldr_flags, ELdrFlags::ShadowCastExclude);
				o->m_iflags = SetBits(o->m_iflags, EInstFlag::ShadowCastExclude, vampire);
			}

			// Non-Affine
			{
				auto non_affine = AllSet(o->m_ldr_flags, ELdrFlags::NonAffine);
				o->m_iflags = SetBits(o->m_iflags, EInstFlag::NonAffine, non_affine);
			}

			return true;
		}, name);
	}

	// Get/Set the render group for this object or child objects matching 'name' (see Apply)
	ESortGroup LdrObject::SortGroup(char const* name) const
	{
		auto obj = Child(name);
		return obj ? obj->m_sko.Group() : ESortGroup::Default;
	}
	void LdrObject::SortGroup(ESortGroup grp, char const* name)
	{
		Apply([=](LdrObject* o)
		{
			o->m_sko.Group(grp);
			return true;
		}, name);
	}

	// Get/Set the nugget flags for this object or child objects matching 'name' (see Apply)
	ENuggetFlag LdrObject::NuggetFlags(char const* name, int index) const
	{
		auto obj = Child(name);
		if (obj == nullptr || obj->m_model == nullptr)
			return ENuggetFlag::None;

		if (index >= static_cast<int>(obj->m_model->m_nuggets.size()))
			throw std::runtime_error("nugget index out of range");

		auto nug = obj->m_model->m_nuggets.begin();
		for (int i = 0; i != index; ++i, ++nug) {}
		return nug->m_nflags;
	}
	void LdrObject::NuggetFlags(ENuggetFlag flags, bool state, char const* name, int index)
	{
		Apply([=](LdrObject* obj)
		{
			if (obj->m_model != nullptr)
			{
				auto nug = obj->m_model->m_nuggets.begin();
				for (int i = 0; i != index; ++i, ++nug) {}
				nug->m_nflags = SetBits(nug->m_nflags, flags, state);
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
	void LdrObject::Colour(Colour32 colour, uint32_t mask, char const* name, EColourOp op, float op_value)
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
					nug.m_nflags = SetBits(nug.m_nflags, ENuggetFlag::TintHasAlpha, tint_has_alpha);
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
				nug.m_nflags = SetBits(nug.m_nflags, ENuggetFlag::TintHasAlpha, has_alpha);
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

	// Return the bounding box for this object in model space
	// To convert this to parent space multiply by 'm_o2p'
	// e.g. BBoxMS() for "*Box { 1 2 3 *o2w{*rand} }" will return bb.m_centre = origin, bb.m_radius = (1,2,3)
	BBox LdrObject::BBoxMS(bool include_children, std::function<bool(LdrObject const&)> pred, float time_s, m4x4 const* p2w_, ELdrFlags parent_flags) const
	{
		auto& p2w = p2w_ ? *p2w_ : m4x4::Identity();
		auto i2w = p2w * m_anim.Step(time_s);

		// Combine recursive flags
		auto flags = m_ldr_flags | (parent_flags & (ELdrFlags::BBoxExclude|ELdrFlags::NonAffine));

		// Start with the bbox for this object
		auto bbox = BBox::Reset();
		if (m_model != nullptr && !AnySet(flags, ELdrFlags::BBoxExclude) && pred(*this)) // Get the bbox from the graphics model
		{
			if (m_model->m_bbox.valid())
			{
				if (IsAffine(i2w))
					Grow(bbox, i2w * m_model->m_bbox);
				else
					Grow(bbox, MulNonAffine(i2w, m_model->m_bbox));
			}
		}
		if (include_children) // Add the bounding boxes of the children
		{
			for (auto& child : m_child)
			{
				auto c2w = i2w * child->m_o2p;
				auto cbbox = child->BBoxMS(include_children, pred, time_s, &c2w, flags);
				if (cbbox.valid()) Grow(bbox, cbbox);
			}
		}
		return bbox;
	}
	BBox LdrObject::BBoxMS(bool include_children) const
	{
		return BBoxMS(include_children, [](LdrObject const&){ return true; });
	}

	// Return the bounding box for this object in world space.
	// If this is a top level object, this will be equivalent to 'm_o2p * BBoxMS()'
	// If not then, then the returned bbox will be transformed to the top level object space
	BBox LdrObject::BBoxWS(bool include_children, std::function<bool(LdrObject const&)> pred, float time_s) const
	{
		// Get the combined o2w transform;
		m4x4 o2w = m_o2p;
		for (LdrObject* parent = m_parent; parent; parent = parent->m_parent)
			o2w = parent->m_o2p * parent->m_anim.Step(time_s) * o2w;

		return BBoxMS(include_children, pred, time_s, &o2w);
	}
	BBox LdrObject::BBoxWS(bool include_children) const
	{
		return BBoxWS(include_children, [](LdrObject const&){ return true; });
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
