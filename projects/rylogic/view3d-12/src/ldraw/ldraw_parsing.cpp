//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/ldraw/ldraw_parsing.h"
#include "pr/view3d-12/ldraw/ldraw_serialiser_text.h"
#include "pr/view3d-12/ldraw/ldraw_serialiser_binary.h"
#include "pr/view3d-12/compute/gpu_job.h"
#include "pr/view3d-12/lighting/light.h"
#include "pr/view3d-12/model/model.h"
#include "pr/view3d-12/model/model_desc.h"
#include "pr/view3d-12/model/model_generator.h"
#include "pr/view3d-12/model/model_tree.h"
#include "pr/view3d-12/model/nugget.h"
#include "pr/view3d-12/model/vertex_layout.h"
#include "pr/view3d-12/resource/resource_factory.h"
#include "pr/view3d-12/sampler/sampler_desc.h"
#include "pr/view3d-12/scene/scene.h"
#include "pr/view3d-12/shaders/shader_arrow_head.h"
#include "pr/view3d-12/shaders/shader_point_sprites.h"
#include "pr/view3d-12/shaders/shader_thick_line.h"
#include "pr/view3d-12/texture/texture_desc.h"

#include "pr/str/extract.h"
#include "pr/maths/frustum.h"
#include "pr/maths/convex_hull.h"
#include "pr/container/byte_data.h"
#include "pr/script/forward.h"
#include "pr/storage/csv.h"

namespace pr::rdr12::ldraw
{
	// Notes:
	//  - Error Handling:
	//    Don't assume the report error callback will throw, try to continue or fail gracefully.

	using Guid = pr::Guid;
	using VCont = pr::vector<v4>;
	using NCont = pr::vector<v4>;
	using ICont = pr::vector<uint16_t>;
	using CCont = pr::vector<Colour32>;
	using TCont = pr::vector<v2>;
	using GCont = pr::vector<NuggetDesc>;
	using ModelCont = ParseResult::ModelLookup;
	using Font = ModelGenerator::Font;
	using TextFormat = ModelGenerator::TextFormat;
	using TextLayout = ModelGenerator::TextLayout;

	enum class EFlags
	{
		None = 0,
		ExplicitName = 1 << 0,
		ExplicitColour = 1 << 1,
		_flags_enum = 0,
	};

	// Template prototype for ObjectCreators
	struct IObjectCreator;
	template <ELdrObject ObjType> struct ObjectCreator;

	// Buffers
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
			return ptr;
		}
		return BuffersPtr(new Buffers()); // using make_unique causes a crash in x64 release here
	}
	void ReturnToPool(BuffersPtr&& bptr)
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
			ReturnToPool(std::move(m_bptr));
		}
		Cache(Cache&& rhs) = delete;
		Cache(Cache const& rhs) = delete;
		Cache operator =(Cache&& rhs) = delete;
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
		// Notes:
		// - Ldr object can be created in a background thread. So there is a separate command list.
		using system_clock = std::chrono::system_clock;
		using time_point = std::chrono::time_point<system_clock>;
		using FontStack = pr::vector<Font>;
		using GpuJob = GpuJob<D3D12_COMMAND_LIST_TYPE_DIRECT>;

		Renderer&       m_rdr;
		ParseResult&    m_result;
		ObjectCont&     m_objects;
		ModelCont&      m_models;
		ResourceFactory m_factory;
		ReportErrorCB   m_report_error;
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

		ParseParams(Renderer& rdr, ParseResult& result, Guid const& context_id, ReportErrorCB error_cb, ParseProgressCB progress_cb, bool& cancel)
			: m_rdr(rdr)
			, m_result(result)
			, m_objects(result.m_objects)
			, m_models(result.m_models)
			, m_factory(rdr)
			, m_report_error(error_cb)
			, m_context_id(context_id)
			, m_cache()
			, m_type(ELdrObject::Unknown)
			, m_parent()
			, m_parent_creator()
			, m_font(1)
			, m_progress_cb(progress_cb)
			, m_last_progress_update(system_clock::now())
			, m_flags(EFlags::None)
			, m_cancel(cancel)
		{}
		ParseParams(ParseParams& pp, ObjectCont& objects, LdrObject* parent, IObjectCreator* parent_creator)
			: m_rdr(pp.m_rdr)
			, m_result(pp.m_result)
			, m_objects(objects)
			, m_models(pp.m_models)
			, m_factory(pp.m_rdr)
			, m_report_error(pp.m_report_error)
			, m_context_id(pp.m_context_id)
			, m_cache()
			, m_type(ELdrObject::Unknown)
			, m_parent(parent)
			, m_parent_creator(parent_creator)
			, m_font(pp.m_font)
			, m_progress_cb(pp.m_progress_cb)
			, m_last_progress_update(pp.m_last_progress_update)
			, m_flags(pp.m_flags)
			, m_cancel(pp.m_cancel)
		{}
		ParseParams(ParseParams&&) = delete;
		ParseParams(ParseParams const&) = delete;
		ParseParams& operator = (ParseParams&&) = delete;
		ParseParams& operator = (ParseParams const&) = delete;

		// Report an error in the script
		void ReportError(EParseError code, Location const& loc, std::string_view msg)
		{
			if (m_report_error == nullptr)
				return;

			//if (msg.empty()) msg = To<std::string>::ToStringA(result);
			m_report_error(code, loc, msg);
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
			m_cancel = !m_progress_cb(m_context_id, m_result, Location(), false);
			m_last_progress_update = system_clock::now();
		}
	};

	// Info on a texture in an ldr object
	struct TextureInfo
	{
		m4x4 m_t2s = m4x4::Identity();
		std::filesystem::path m_filepath = {"#white"};
		TextureDesc m_tdesc = {};
		SamDesc m_sdesc = {};
		bool m_has_alpha = false;
	};

	// Forward declare the recursive object parsing function
	bool ParseLdrObject(ELdrObject type, IReader& reader, ParseParams& pp);

	#pragma region Parse Common Elements

	// Parse a camera description
	void ParseCamera(IReader& reader, ParseParams& pp, ParseResult& out)
	{
		auto section = reader.SectionScope();
		for (EKeyword kw; reader.NextKeyword(kw);)
		{
			switch (kw)
			{
				case EKeyword::O2W:
				{
					auto c2w = m4x4::Identity();
					reader.Transform(c2w);
					out.m_cam.CameraToWorld(c2w);
					out.m_cam_fields |= ECamField::C2W;
					break;
				}
				case EKeyword::LookAt:
				{
					auto lookat = reader.Vector3f().w1();
					m4x4 c2w = out.m_cam.CameraToWorld();
					out.m_cam.LookAt(c2w.pos, lookat, c2w.y);
					out.m_cam_fields |= ECamField::C2W;
					out.m_cam_fields |= ECamField::Focus;
					break;
				}
				case EKeyword::Align:
				{
					auto align = reader.Vector3f().w0();
					out.m_cam.Align(align);
					out.m_cam_fields |= ECamField::Align;
					break;
				}
				case EKeyword::Aspect:
				{
					auto aspect = reader.Real<float>();
					out.m_cam.Aspect(aspect);
					out.m_cam_fields |= ECamField::Align;
					break;
				}
				case EKeyword::FovX:
				{
					auto fovX = reader.Real<float>();
					out.m_cam.FovX(fovX);
					out.m_cam_fields |= ECamField::FovY;
					break;
				}
				case EKeyword::FovY:
				{
					auto fovY = reader.Real<float>();
					out.m_cam.FovY(fovY);
					out.m_cam_fields |= ECamField::FovY;
					break;
				}
				case EKeyword::Fov:
				{
					auto fov = reader.Vector2f();
					out.m_cam.Fov(fov.x, fov.y);
					out.m_cam_fields |= ECamField::Aspect;
					out.m_cam_fields |= ECamField::FovY;
					break;
				}
				case EKeyword::Near:
				{
					auto near_ = reader.Real<float>();
					out.m_cam.Near(near_, true);
					out.m_cam_fields |= ECamField::Near;
					break;
				}
				case EKeyword::Far:
				{
					auto far_ = reader.Real<float>();
					out.m_cam.Far(far_, true);
					out.m_cam_fields |= ECamField::Far;
					break;
				}
				case EKeyword::Orthographic:
				{
					out.m_cam.Orthographic(true);
					out.m_cam_fields |= ECamField::Ortho;
					break;
				}
				default:
				{
					pp.ReportError(EParseError::UnknownKeyword, reader.Loc(), std::format("Keyword '{}' is not valid within *Camera", EKeyword_::ToStringA(kw)));
					break;
				}
			}
		}
	}

	// Parse a font description
	void ParseFont(IReader& reader, ParseParams& pp, Font& font)
	{
		font.m_underline = false;
		font.m_strikeout = false;
		auto section = reader.SectionScope();
		for (EKeyword kw; reader.NextKeyword(kw);)
		{
			switch (kw)
			{
				case EKeyword::Name:
				{
					font.m_name = Widen(reader.String<string32>());
					break;
				}
				case EKeyword::Size:
				{
					font.m_size = reader.Real<float>();
					break;
				}
				case EKeyword::Colour:
				{
					font.m_colour = reader.Int<uint32_t>(16);
					break;
				}
				case EKeyword::Weight:
				{
					font.m_weight = s_cast<DWRITE_FONT_WEIGHT>(reader.Int<int>(10));
					break;
				}
				case EKeyword::Style:
				{
					auto ident = reader.Identifier<string32>();
					if (str::EqualI(ident, "normal")) font.m_style = DWRITE_FONT_STYLE_NORMAL;
					if (str::EqualI(ident, "italic")) font.m_style = DWRITE_FONT_STYLE_ITALIC;
					if (str::EqualI(ident, "oblique")) font.m_style = DWRITE_FONT_STYLE_OBLIQUE;
					break;
				}
				case EKeyword::Stretch:
				{
					font.m_stretch = s_cast<DWRITE_FONT_STRETCH>(reader.Int<int>(10));
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
					pp.ReportError(EParseError::UnknownKeyword, reader.Loc(), std::format("Keyword '{}' is not valid within *Font", EKeyword_::ToStringA(kw)));
					break;
				}
			}
		}
	}

	// Parse a simple animation description
	void ParseAnimation(IReader& reader, ParseParams& pp, Animation& anim)
	{
		auto section = reader.SectionScope();
		for (EKeyword kw; reader.NextKeyword(kw);)
		{
			switch (kw)
			{
				case EKeyword::Style:
				{
					auto style = reader.Identifier<string32>();
					if (str::EqualI(style, "NoAnimation")) anim.m_style = EAnimStyle::NoAnimation;
					else if (str::EqualI(style, "Once")) anim.m_style = EAnimStyle::Once;
					else if (str::EqualI(style, "Repeat")) anim.m_style = EAnimStyle::Repeat;
					else if (str::EqualI(style, "Continuous")) anim.m_style = EAnimStyle::Continuous;
					else if (str::EqualI(style, "PingPong")) anim.m_style = EAnimStyle::PingPong;
					break;
				}
				case EKeyword::Period:
				{
					anim.m_period = reader.Real<float>();
					break;
				}
				case EKeyword::Velocity:
				{
					anim.m_vel = reader.Vector3f().w0();
					break;
				}
				case EKeyword::Accel:
				{
					anim.m_acc = reader.Vector3f().w0();
					break;
				}
				case EKeyword::AngVelocity:
				{
					anim.m_avel = reader.Vector3f().w0();
					break;
				}
				case EKeyword::AngAccel:
				{
					anim.m_aacc = reader.Vector3f().w0();
					break;
				}
				default:
				{
					pp.ReportError(EParseError::UnknownKeyword, reader.Loc(), std::format("Keyword '{}' is not valid within *Animation", EKeyword_::ToStringA(kw)));
					break;
				}
			}
		}
	}
	
	// Parse a texture description
	void ParseTexture(IReader& reader, ParseParams& pp, TextureInfo& tex)
	{
		auto section = reader.SectionScope();
		for (EKeyword kw; reader.NextKeyword(kw);)
		{
			switch (kw)
			{
				case EKeyword::FilePath:
				{
					tex.m_filepath = reader.String<std::filesystem::path>();
					break;
				}
				case EKeyword::O2W:
				{
					reader.Transform(tex.m_t2s);
					break;
				}
				case EKeyword::Addr:
				{
					tex.m_sdesc.AddressU = s_cast<D3D12_TEXTURE_ADDRESS_MODE>(reader.Enum<ETexAddrMode>());
					tex.m_sdesc.AddressV = reader.IsSectionEnd() ? tex.m_sdesc.AddressU : s_cast<D3D12_TEXTURE_ADDRESS_MODE>(reader.Enum<ETexAddrMode>());
					tex.m_sdesc.AddressW = reader.IsSectionEnd() ? tex.m_sdesc.AddressV : s_cast<D3D12_TEXTURE_ADDRESS_MODE>(reader.Enum<ETexAddrMode>());
					break;
				}
				case EKeyword::Filter:
				{
					tex.m_sdesc.Filter = s_cast<D3D12_FILTER>(reader.Enum<EFilter>());
					break;
				}
				case EKeyword::Alpha:
				{
					tex.m_has_alpha = true;
					break;
				}
				default:
				{
					pp.ReportError(EParseError::UnknownKeyword, reader.Loc(), std::format("Keyword '{}' is not valid within *Texture", EKeyword_::ToStringA(kw)));
					break;
				}
			}
		}
	}

	// Parse keywords that can appear in any section. Returns true if the keyword was recognised.
	bool ParseProperties(IReader& reader, ParseParams& pp, EKeyword kw, LdrObject* obj)
	{
		switch (kw)
		{
			case EKeyword::Name:
			{
				obj->m_name = reader.Identifier<string32>();
				return true;
			}
			case EKeyword::O2W:
			case EKeyword::Txfm:
			{
				reader.Transform(obj->m_o2p);
				obj->m_ldr_flags = SetBits(obj->m_ldr_flags, ELdrFlags::NonAffine, !IsAffine(obj->m_o2p));
				return true;
			}
			case EKeyword::Colour:
			{
				obj->m_base_colour = reader.Int<uint32_t>(16);
				return true;
			}
			case EKeyword::ColourMask:
			{
				obj->m_colour_mask = reader.Int<uint32_t>(16);
				return true;
			}
			case EKeyword::Reflectivity:
			{
				obj->m_env = reader.Real<float>();
				return true;
			};
			case EKeyword::RandColour:
			{
				obj->m_base_colour = RandomRGB(g_rng(), 0.5f, 1.0f);
				return true;
			}
			case EKeyword::Font:
			{
				ParseFont(reader, pp, pp.m_font.back());
				return true;
			}
			case EKeyword::Animation:
			{
				ParseAnimation(reader, pp, obj->m_anim);
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
			default:
			{
				return false;
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

	#pragma endregion

	#pragma region Object modifiers
	
	namespace creation
	{
		// Support for objects with a texture
		struct Textured
		{
			Texture2DPtr m_texture;
			SamplerPtr m_sampler;
			SamDesc m_def_sdesc;

			explicit Textured(SamDesc def_sdesc)
				: m_texture()
				, m_sampler()
				, m_def_sdesc(def_sdesc)
			{}
			bool ParseKeyword(IReader& reader, ParseParams& pp, EKeyword kw)
			{
				switch (kw)
				{
					case EKeyword::Texture:
					{
						TextureInfo tex_info;
						ParseTexture(reader, pp, tex_info);

						// Create the texture
						try
						{
							auto desc = TextureDesc(AutoId, {}).has_alpha(tex_info.m_has_alpha);
							m_texture = pp.m_factory.CreateTexture2D(tex_info.m_filepath, tex_info.m_tdesc);
							m_texture->m_t2s = tex_info.m_t2s;
						}
						catch (std::exception const& e)
						{
							pp.ReportError(EParseError::NotFound, reader.Loc(), std::format("Failed to create texture {}\n{}", tex_info.m_filepath.string(), e.what()));
						}

						// Create the sampler
						try
						{
							auto desc = SamplerDesc(tex_info.m_sdesc.Id(), tex_info.m_sdesc);
							m_sampler = pp.m_factory.GetSampler(desc);
						}
						catch(std::exception const& e)
						{
							pp.ReportError(EParseError::NotFound, reader.Loc(), std::format("Failed to create sampler for texture {}\n{}", tex_info.m_filepath.string(), e.what()));
						}
						return true;
					}
					case EKeyword::Video:
					{
						auto filepath = reader.String<string32>();
						if (!filepath.empty())
						{
							//todo
							//' // Load the video texture
							//' try
							//' {
							//' 	vid = pp.m_rdr.m_tex_mgr.CreateVideoTexture(AutoId, filepath.c_str());
							//' }
							//' catch (std::exception const& e)
							//' {
							//' 	pp.ReportError(EScriptResult::ValueNotFound, reader.Location(), std::format("failed to create video {}\n{}", filepath, e.what()));
							//' }
						}
						return true;
					}
					default:
					{
						return false;
					}
				}
			}
		};

		// Support for objects with a main axis
		struct MainAxis
		{
			m4x4 m_o2w;
			AxisId m_main_axis; // The natural main axis of the object
			AxisId m_align;     // The axis we want the main axis to be aligned to

			MainAxis(AxisId main_axis = AxisId::PosZ, AxisId align = AxisId::PosZ)
				:m_o2w(m4x4::Identity())
				,m_main_axis(main_axis)
				,m_align(align)
			{}
			bool ParseKeyword(IReader& reader, ParseParams& pp, EKeyword kw)
			{
				switch (kw)
				{
					case EKeyword::AxisId:
					{
						m_align.value = reader.Int<int>(10);
						if (AxisId::IsValid(m_align))
						{
							m_o2w = m4x4::Transform(m_main_axis, m_align, v4::Origin());
							return true;
						}

						pp.ReportError(EParseError::InvalidValue, reader.Loc(), "AxisId must be +/- 1, 2, or 3 (corresponding to the positive or negative X, Y, or Z axis)");
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

			// Apply main axis transform
			void Apply(std::span<v4> verts)
			{
				if (m_main_axis == m_align)
					return;

				for (auto& v : verts)
					v = m_o2w * v;
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
				: m_point_size()
				, m_style(EStyle::Square)
				, m_depth(false)
			{}
			bool ParseKeyword(IReader& reader, ParseParams& pp, EKeyword kw)
			{
				switch (kw)
				{
					case EKeyword::Size:
					{
						m_point_size = reader.Vector2f();
						return true;
					}
					case EKeyword::Style:
					{
						auto ident = reader.Identifier<string32>();
						switch (HashI(ident.c_str()))
						{
							case HashI("square"):   m_style = EStyle::Square; break;
							case HashI("circle"):   m_style = EStyle::Circle; break;
							case HashI("triangle"): m_style = EStyle::Triangle; break;
							case HashI("star"):     m_style = EStyle::Star; break;
							case HashI("annulus"):  m_style = EStyle::Annulus; break;
							default: pp.ReportError(EParseError::UnknownKeyword, reader.Loc(), std::format("'{}' is not a valid point sprite style", ident)); break;
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
			
			template <typename TDrawOnIt>
			Texture2DPtr CreatePointStyleTexture(ParseParams& pp, RdrId id, iv2 const& sz, char const* name, TDrawOnIt draw)
			{
				// Create a texture large enough to contain the text, and render the text into it
				//'SamDesc sdesc(D3D11_TEXTURE_ADDRESS_CLAMP, D3D11_FILTER_MIN_MAG_MIP_POINT);
				auto tdesc = ResDesc::Tex2D(Image(sz.x, sz.y, nullptr, DXGI_FORMAT_B8G8R8A8_UNORM), 1, EUsage::RenderTarget);
				auto desc = TextureDesc(id, tdesc).name(name);
				auto tex = pp.m_factory.CreateTexture2D(desc);

				(void)draw;
				#if 0 //todo
				// Get a D2D device context to draw on
				auto dc = tex->GetD2DeviceContext();

				// Create the brushes
				D3DPtr<ID2D1SolidColorBrush> fr_brush;
				D3DPtr<ID2D1SolidColorBrush> bk_brush;
				auto fr = D3DCOLORVALUE{1.f, 1.f, 1.f, 1.f};
				auto bk = D3DCOLORVALUE{0.f, 0.f, 0.f, 0.f};
				Check(dc->CreateSolidColorBrush(fr, &fr_brush.m_ptr));
				Check(dc->CreateSolidColorBrush(bk, &bk_brush.m_ptr));

				// Draw the spot
				dc->BeginDraw();
				dc->Clear(&bk);
				draw(dc, fr_brush.get(), bk_brush.get());
				pr::Check(dc->EndDraw());
				#endif
				return tex;
			}
			Texture2DPtr PointStyleTexture(ParseParams& pp)
			{
				EStyle style = m_style;
				iv2 size = To<iv2>(m_point_size);
				iv2 sz(PowerOfTwoGreaterEqualTo(size.x), PowerOfTwoGreaterEqualTo(size.y));
				switch (style)
				{
					case EStyle::Square:
					{
						// No texture needed for square style
						return nullptr;
					}
					case EStyle::Circle:
					{
						ResourceStore::Access store(pp.m_rdr);
						auto id = pr::hash::HashArgs("PointStyleCircle", sz);
						return store.FindTexture<Texture2D>(id, [&]
						{
							auto w0 = sz.x * 0.5f;
							auto h0 = sz.y * 0.5f;
							return CreatePointStyleTexture(pp, id, sz, "PointStyleCircle", [=](auto& dc, auto fr, auto) { dc->FillEllipse({ {w0, h0}, w0, h0 }, fr); });
						});
					}
					case EStyle::Triangle:
					{
						ResourceStore::Access store(pp.m_rdr);
						auto id = pr::hash::HashArgs("PointStyleTriangle", sz);
						return store.FindTexture<Texture2D>(id, [&]
						{
							D3DPtr<ID2D1PathGeometry> geom;
							D3DPtr<ID2D1GeometrySink> sink;
							pr::Check(pp.m_rdr.D2DFactory()->CreatePathGeometry(&geom.m_ptr));
							pr::Check(geom->Open(&sink.m_ptr));

							auto w0 = 1.0f * sz.x;
							auto h0 = 0.5f * sz.y * (float)tan(pr::DegreesToRadians(60.0f));
							auto h1 = 0.5f * (sz.y - h0);

							sink->BeginFigure({ w0, h1 }, D2D1_FIGURE_BEGIN_FILLED);
							sink->AddLine({ 0.0f * w0, h1 });
							sink->AddLine({ 0.5f * w0, h0 + h1 });
							sink->EndFigure(D2D1_FIGURE_END_CLOSED);
							pr::Check(sink->Close());

							return CreatePointStyleTexture(pp, id, sz, "PointStyleTriangle", [=](auto& dc, auto fr, auto) { dc->FillGeometry(geom.get(), fr, nullptr); });
						});
					}
					case EStyle::Star:
					{
						ResourceStore::Access store(pp.m_rdr);
						auto id = pr::hash::HashArgs("PointStyleStar", sz);
						return store.FindTexture<Texture2D>(id, [&]
						{
							D3DPtr<ID2D1PathGeometry> geom;
							D3DPtr<ID2D1GeometrySink> sink;
							pr::Check(pp.m_rdr.D2DFactory()->CreatePathGeometry(&geom.m_ptr));
							pr::Check(geom->Open(&sink.m_ptr));

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
							pr::Check(sink->Close());

							return CreatePointStyleTexture(pp, id, sz, "PointStyleStar", [=](auto& dc, auto fr, auto) { dc->FillGeometry(geom.get(), fr, nullptr); });
						});
					}
					case EStyle::Annulus:
					{
						ResourceStore::Access store(pp.m_rdr);
						auto id = pr::hash::HashArgs("PointStyleAnnulus", sz);
						return store.FindTexture<Texture2D>(id, [&]
						{
							auto w0 = sz.x * 0.5f;
							auto h0 = sz.y * 0.5f;
							auto w1 = sz.x * 0.4f;
							auto h1 = sz.y * 0.4f;
							return CreatePointStyleTexture(pp, id, sz, "PointStyleAnnulus", [=](auto& dc, auto fr, auto bk)
							{
								dc->SetPrimitiveBlend(D2D1_PRIMITIVE_BLEND_COPY);
								dc->FillEllipse({ {w0, h0}, w0, h0 }, fr);
								dc->FillEllipse({ {w0, h0}, w1, h1 }, bk);
							});
						});
					}
					default:
					{
						throw std::runtime_error("Unknown point style");
					}
				}
			}
		};

		// Support baked in transforms
		struct BakeTransform
		{
			m4x4 m_o2w;

			BakeTransform(m4x4 const& o2w = m4x4::Zero())
				:m_o2w(o2w)
			{}
			bool ParseKeyword(IReader& reader, ParseParams&, EKeyword kw)
			{
				switch (kw)
				{
					case EKeyword::BakeTransform:
					{
						reader.Transform(m_o2w);
						return true;
					}
					default:
					{
						return false;
					}
				}
			}
			
			// Returns a pointer to a rotation from 'main_axis' to 'axis'. Returns null if identity
			m4x4 const* O2WPtr() const
			{
				return m_o2w.w.w != 0 ? &m_o2w : nullptr;
			}
		};

		// Support for generate normals
		struct GenNorms
		{
			float m_smoothing_angle;

			GenNorms(float gen_normals = -1.0f)
				:m_smoothing_angle(gen_normals)
			{}
			bool ParseKeyword(IReader& reader, ParseParams&, EKeyword kw)
			{
				switch (kw)
				{
					case EKeyword::GenerateNormals:
					{
						m_smoothing_angle = reader.Real<float>();
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
			void Generate(ParseParams& pp)
			{
				if (m_smoothing_angle < 0.0f)
					return;

				auto& verts = pp.m_cache.m_point;
				auto& indices = pp.m_cache.m_index;
				auto& normals = pp.m_cache.m_norms;
				auto& nuggets = pp.m_cache.m_nugts;

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
					if (nug.m_irange != Range::Reset())
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

			explicit operator bool() const
			{
				return m_smoothing_angle >= 0;
			}
		};

		// Support for smoothed lines
		struct SmoothLine
		{
			bool m_smooth;
			SmoothLine()
				: m_smooth()
			{}
			bool ParseKeyword(IReader&, ParseParams&, EKeyword kw)
			{
				switch (kw)
				{
					case EKeyword::Smooth:
					{
						m_smooth = true;
						return true;
					}
					default:
					{
						return false;
					}
				}
			}
			void Apply(VCont& verts)
			{
				if (!m_smooth)
					return;

				VCont out;
				std::swap(out, verts);
				Smooth(out, Spline::ETopo::Continuous3, [&](auto points, auto)
				{
					verts.insert(verts.end(), points.begin(), points.end());
				});
			}
		};

		// Support for parametric ranges
		struct Parametrics
		{
			pr::vector<int> m_index;
			pr::vector<v2> m_para;
			Parametrics()
				: m_index()
				, m_para()
			{}
			bool ParseKeyword(IReader& reader, ParseParams&, EKeyword kw)
			{
				switch (kw)
				{
					case EKeyword::Param:
					{
						// Expect tuples of (item index, [t0, t1])
						for (; !reader.IsSectionEnd(); )
						{
							auto idx = reader.Int<int>();
							auto para = reader.Vector2f();
							m_index.push_back(idx);
							m_para.push_back(para);
						}
						return true;
					}
					default:
					{
						return false;
					}
				}
			}
			void Apply(std::span<v4> verts, ParseParams& pp, Location const& loc)
			{
				for (int i = 0, iend = isize(m_index); i != iend; ++i)
				{
					if (i * 2 >= iend)
						pp.ReportError(EParseError::IndexOutOfRange, loc, std::format("Index {} is out of range (max={})", i, iend/2));

					auto const& para = m_para[i];
					auto& p0 = verts[i * 2 + 0];
					auto& p1 = verts[i * 2 + 1];
					auto dir = p1 - p0;
					auto pt = p0;
					p0 = pt + para.x * dir;
					p1 = pt + para.y * dir;
				}
			}
		};

		// Support for dashed lines
		struct DashedLines
		{
			v2 m_dash;

			DashedLines()
				: m_dash({ 1,0 })
			{}
			bool ParseKeyword(IReader& reader, ParseParams&, EKeyword kw)
			{
				switch (kw)
				{
					case EKeyword::Dashed:
					{
						m_dash = reader.Vector2f();
						return true;
					}
					default:
					{
						return false;
					}
				}
			}
			void Apply(VCont& verts, bool& is_line_list)
			{
				if (m_dash == v2(1, 0))
					return;

				// Convert each line segment to dashed lines
				VCont out;
				std::swap(out, verts);
				if (is_line_list)
					DashLineList(out, verts, m_dash);
				else
					DashLineStrip(out, verts, m_dash);

				// Conversion to dashes turns the buffer into a line list
				is_line_list = true;
			}

		private:

			// Convert a line strip into a line list of "dash" line segments
			void DashLineStrip(VCont const& in, VCont& out, v2 const& dash)
			{
				assert(isize(in) >= 2);

				// Turn the sequence of line segments into a single dashed line
				auto t = 0.0f;
				for (int i = 1, iend = isize(in); i != iend; ++i)
				{
					auto d = in[i] - in[i - 1];
					auto len = Length(d);

					// Emit dashes over the length of the line segment
					for (; t < len; t += dash.x + dash.y)
					{
						out.push_back(in[i - 1] + d * Clamp(t, 0.0f, len) / len);
						out.push_back(in[i - 1] + d * Clamp(t + dash.x, 0.0f, len) / len);
					}
					t -= len + dash.x + dash.y;
				}
			}

			// Convert a line list into a list of "dash" line segments
			void DashLineList(VCont const& in, VCont& out, v2 const& dash)
			{
				assert(isize(in) >= 2 && (isize(in) & 1) == 0);

				// Turn the line list 'in' into dashed lines
				// Turn the sequence of line segments into a single dashed line
				for (int i = 0, iend = isize(in); i != iend; i += 2)
				{
					auto d = in[i + 1] - in[i];
					auto len = Length(d);

					// Emit dashes over the length of the line segment
					for (auto t = 0.0f; t < len; t += dash.x + dash.y)
					{
						out.push_back(in[i] + d * Clamp(t, 0.0f, len) / len);
						out.push_back(in[i] + d * Clamp(t + dash.x, 0.0f, len) / len);
					}
				}
			}
		};
	}

	#pragma endregion

	#pragma region ObjectCreator

	// Base class for all object creators
	struct IObjectCreator
	{
		ParseParams& m_pp;
		VCont& m_verts;
		ICont& m_indices;
		NCont& m_normals;
		CCont& m_colours;
		TCont& m_texs;
		GCont& m_nuggets;

		IObjectCreator(ParseParams& pp)
			: m_pp(pp)
			, m_verts  (pp.m_cache.m_point)
			, m_indices(pp.m_cache.m_index)
			, m_normals(pp.m_cache.m_norms)
			, m_colours(pp.m_cache.m_color)
			, m_texs   (pp.m_cache.m_texts)
			, m_nuggets(pp.m_cache.m_nugts)
		{}
		virtual ~IObjectCreator() = default;

		// Create an LdrObject from 'reader'
		virtual LdrObjectPtr Parse(IReader& reader)
		{
			// Notes:
			//  - Not using an output iterator style callback because model
			//    instancing relies on the map from object to model.
			auto start_location = reader.Loc();
			auto section = reader.SectionScope();

			// Read the object attributes: name, colour, instance
			auto obj = LdrObjectPtr(new LdrObject(m_pp.m_type, m_pp.m_parent, m_pp.m_context_id), true);

			// Read the description of the model
			for (EKeyword kw; !m_pp.m_cancel && reader.NextKeyword(kw);)
			{
				// Let the object creator have the first go with the keyword
				if (ParseKeyword(reader, kw))
					continue;

				// Is the keyword a common object property?
				if (ParseProperties(reader, m_pp, kw, obj.get()))
					continue;

				// Recursively parse child objects
				ParseParams pp(m_pp, obj->m_child, obj.get(), this);
				if (ParseLdrObject((ELdrObject)kw, reader, pp))
					continue;

				// Unknown token
				m_pp.ReportError(EParseError::UnknownKeyword, reader.Loc(), "Unknown keyword");
				continue;
			}

			// Complete the model for 'm_obj'
			CreateModel(obj.get(), start_location);

			return obj;
		}
		virtual bool ParseKeyword(IReader&, EKeyword)
		{
			return false;
		}
		virtual void CreateModel(LdrObject*, Location const&)
		{
			// Don't return a model from this method, because the overrides are also configuring 'obj'
			// It's doesn't make sense to separate this from the returned model.
		}
	};

	#pragma endregion

	#pragma region Sprite Objects

	// ELdrObject::Point
	template <> struct ObjectCreator<ELdrObject::Point> :IObjectCreator
	{
		creation::Textured m_tex;
		creation::PointSprite m_sprite;
		bool m_per_item_colour;

		ObjectCreator(ParseParams& pp)
			: IObjectCreator(pp)
			, m_tex(SamDesc::AnisotropicClamp())
			, m_sprite()
			, m_per_item_colour()
		{}
		bool ParseKeyword(IReader& reader, EKeyword kw) override
		{
			switch (kw)
			{
				case EKeyword::Data:
				{
					for (; !reader.IsSectionEnd();)
					{
						m_verts.push_back(reader.Vector3f().w1());
						if (m_per_item_colour)
							m_colours.push_back(reader.Int<uint32_t>(16));
					}
					return true;
				}
				case EKeyword::PerItemColour:
				{
					m_per_item_colour = true;
					return true;
				}
				default:
				{
					return
						m_tex.ParseKeyword(reader, m_pp, kw) ||
						m_sprite.ParseKeyword(reader, m_pp, kw) ||
						IObjectCreator::ParseKeyword(reader, kw);
				}
			}
		}
		void CreateModel(LdrObject* obj, Location const&) override
		{
			// No points = no model
			if (m_verts.empty())
				return;

			// Create the model
			auto opts = ModelGenerator::CreateOptions().colours(m_colours);
			obj->m_model = ModelGenerator::Points(m_pp.m_factory, m_verts, &opts);
			obj->m_model->m_name = obj->TypeAndName();

			// Use a geometry shader to draw points
			if (m_sprite.m_point_size != v2::Zero())
			{
				auto gs_points = Shader::Create<shaders::PointSpriteGS>(m_sprite.m_point_size, m_sprite.m_depth);

				obj->m_model->DeleteNuggets();
				obj->m_model->CreateNugget(m_pp.m_factory, NuggetDesc(ETopo::PointList, EGeom::Vert | EGeom::Colr | EGeom::Tex0)
					.use_shader(ERenderStep::RenderForward, gs_points)
					.tex_diffuse(m_sprite.PointStyleTexture(m_pp)));
			}
		}
	};

	#pragma endregion

	#pragma region Line Objects

	// ELdrObject::Line
	template <> struct ObjectCreator<ELdrObject::Line> :IObjectCreator
	{
		creation::Parametrics m_parametric;
		creation::DashedLines m_dashed;
		float m_line_width;
		bool m_per_item_colour;

		ObjectCreator(ParseParams& pp)
			: IObjectCreator(pp)
			, m_parametric()
			, m_dashed()
			, m_line_width()
			, m_per_item_colour()
		{}
		bool ParseKeyword(IReader& reader, EKeyword kw) override
		{
			switch (kw)
			{
				case EKeyword::Data:
				{
					auto p0 = reader.Vector3f().w1();
					auto p1 = reader.Vector3f().w1();
					m_verts.push_back(p0);
					m_verts.push_back(p1);
					if (m_per_item_colour)
					{
						Colour32 col = reader.Int<uint32_t>(16);
						m_colours.push_back(col);
						m_colours.push_back(col);
					}
					return true;
				}
				case EKeyword::Width:
				{
					m_line_width = reader.Real<float>();
					return true;
				}
				case EKeyword::PerItemColour:
				{
					m_per_item_colour = true;
					return true;
				}
				default:
				{
					return
						m_parametric.ParseKeyword(reader, m_pp, kw) ||
						m_dashed.ParseKeyword(reader, m_pp, kw) ||
						IObjectCreator::ParseKeyword(reader, kw);
				}
			}
		}
		void CreateModel(LdrObject* obj, Location const& loc) override
		{
			// No points = no model
			if (m_verts.size() < 2)
				return;

			// Clip lines to parametric values
			m_parametric.Apply(m_verts, m_pp, loc);

			// Convert lines to dashed lines
			bool is_line_list = true;
			m_dashed.Apply(m_verts, is_line_list);

			// Create the model
			auto opts = ModelGenerator::CreateOptions().colours(m_colours);
			obj->m_model = ModelGenerator::Lines(m_pp.m_factory, isize(m_verts) / 2, m_verts, &opts);
			obj->m_model->m_name = obj->TypeAndName();

			// Use thick lines
			if (m_line_width != 0.0f)
			{
				auto shdr = Shader::Create<shaders::ThickLineListGS>(m_line_width);
				for (auto& nug : obj->m_model->m_nuggets)
					nug.m_shaders.push_back({ shdr, ERenderStep::RenderForward });
			}
		}
	};

	// ELdrObject::LineD
	template <> struct ObjectCreator<ELdrObject::LineD> :IObjectCreator
	{
		creation::Parametrics m_parametric;
		creation::DashedLines m_dashed;
		float m_line_width;
		bool m_per_item_colour;

		ObjectCreator(ParseParams& pp)
			: IObjectCreator(pp)
			, m_parametric()
			, m_dashed()
			, m_line_width()
			, m_per_item_colour()
		{}
		bool ParseKeyword(IReader& reader, EKeyword kw) override
		{
			switch (kw)
			{
				case EKeyword::Data:
				{
					auto p0 = reader.Vector3f().w1();
					auto p1 = reader.Vector3f().w0();
					m_verts.push_back(p0);
					m_verts.push_back(p0 + p1);
					if (m_per_item_colour)
					{
						Colour32 col = reader.Int<uint32_t>(16);
						m_colours.push_back(col);
						m_colours.push_back(col);
					}
					return true;
				}
				case EKeyword::Width:
				{
					m_line_width = reader.Real<float>();
					return true;
				}
				case EKeyword::PerItemColour:
				{
					m_per_item_colour = true;
					return true;
				}
				default:
				{
					return
						m_parametric.ParseKeyword(reader, m_pp, kw) ||
						m_dashed.ParseKeyword(reader, m_pp, kw) ||
						IObjectCreator::ParseKeyword(reader, kw);
				}
			}
		}
		void CreateModel(LdrObject* obj, Location const& loc) override
		{
			// No points = no model
			if (m_verts.size() < 2)
				return;

			// Clip lines to parametric values
			m_parametric.Apply(m_verts, m_pp, loc);

			// Convert lines to dashed lines
			bool is_line_list = true;
			m_dashed.Apply(m_verts, is_line_list);

			// Create the model
			auto opts = ModelGenerator::CreateOptions().colours(m_colours);
			obj->m_model = ModelGenerator::Lines(m_pp.m_factory, isize(m_verts) / 2, m_verts, &opts);
			obj->m_model->m_name = obj->TypeAndName();

			// Use thick lines
			if (m_line_width != 0.0f)
			{
				auto shdr = Shader::Create<shaders::ThickLineListGS>(m_line_width);
				for (auto& nug : obj->m_model->m_nuggets)
					nug.m_shaders.push_back({ shdr, ERenderStep::RenderForward });
			}
		}
	};

	// ELdrObject::LineStrip
	template <> struct ObjectCreator<ELdrObject::LineStrip> :IObjectCreator
	{
		creation::DashedLines m_dashed;
		float m_line_width;
		bool m_per_item_colour;
		bool m_smooth;

		ObjectCreator(ParseParams& pp)
			: IObjectCreator(pp)
			, m_dashed()
			, m_line_width()
			, m_per_item_colour()
			, m_smooth()
		{}
		bool ParseKeyword(IReader& reader, EKeyword kw) override 
		{
			switch (kw)
			{
				case EKeyword::Data:
				{
					auto pt = reader.Vector3f().w1();
					m_verts.push_back(pt);
					if (m_per_item_colour)
					{
						Colour32 col = reader.Int<uint32_t>(16);
						m_colours.push_back(col);
					}
					return true;
				}
				case EKeyword::Smooth:
				{
					m_smooth = true;
					return true;
				}
				case EKeyword::Width:
				{
					m_line_width = reader.Real<float>();
					return true;
				}
				case EKeyword::PerItemColour:
				{
					m_per_item_colour = true;
					return true;
				}
				default:
				{
					return
						m_dashed.ParseKeyword(reader, m_pp, kw) ||
						IObjectCreator::ParseKeyword(reader, kw);
				}
			}
		}
		void CreateModel(LdrObject* obj, Location const&) override
		{
			// No points = no model
			if (m_verts.size() < 2)
				return;

			// Smooth the points
			if (m_smooth)
			{
				VCont verts;
				std::swap(verts, m_verts);
				Smooth(verts, Spline::ETopo::Continuous3, [&](auto points, auto)
				{
					m_verts.insert(m_verts.end(), points.begin(), points.end());
				});
			}

			// Convert lines to dashed lines
			auto is_line_list = false;
			m_dashed.Apply(m_verts, is_line_list);

			// The thick line strip shader uses LineAdj which requires an extra first and last vert
			if (!is_line_list && m_line_width != 0.0f)
			{
				m_verts.insert(std::begin(m_verts), m_verts.front());
				m_verts.insert(std::end(m_verts), m_verts.back());
			}

			// Create the model
			auto opts = ModelGenerator::CreateOptions().colours(m_colours);
			obj->m_model = is_line_list
				? ModelGenerator::Lines(m_pp.m_factory, isize(m_verts) / 2, m_verts, &opts)
				: ModelGenerator::LineStrip(m_pp.m_factory, isize(m_verts) - 1, m_verts, &opts);
			obj->m_model->m_name = obj->TypeAndName();

			// Use thick lines
			if (m_line_width != 0.0f)
			{
				auto shdr = is_line_list
					? static_cast<ShaderPtr>(Shader::Create<shaders::ThickLineListGS>(m_line_width))
					: static_cast<ShaderPtr>(Shader::Create<shaders::ThickLineStripGS>(m_line_width));

				for (auto& nug : obj->m_model->m_nuggets)
				{
					nug.m_topo = is_line_list ? ETopo::LineList : ETopo::LineStripAdj;
					nug.m_shaders.push_back({ shdr, ERenderStep::RenderForward });
				}
			}
		}
	};

	// ELdrObject::LineBox
	template <> struct ObjectCreator<ELdrObject::LineBox> :IObjectCreator
	{
		creation::DashedLines m_dashed;
		float m_line_width;

		ObjectCreator(ParseParams& pp)
			: IObjectCreator(pp)
			, m_dashed()
			, m_line_width()
		{}
		bool ParseKeyword(IReader& reader, EKeyword kw) override 
		{
			switch (kw)
			{
				case EKeyword::Data:
				{
					auto dim = v4(reader.Real<float>()).w0();
					if (!reader.IsSectionEnd()) dim.y = reader.Real<float>();
					if (!reader.IsSectionEnd()) dim.z = reader.Real<float>();
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
					return true;
				}
				case EKeyword::Width:
				{
					m_line_width = reader.Real<float>();
					return true;
				}
				default:
				{
					return
						m_dashed.ParseKeyword(reader, m_pp, kw) ||
						IObjectCreator::ParseKeyword(reader, kw);
				}
			}
		}
		void CreateModel(LdrObject* obj, Location const&) override
		{
			// No points = no model
			if (m_verts.empty())
				return;

			// Convert lines to dashed lines
			auto is_line_list = true;
			m_dashed.Apply(m_verts, is_line_list);

			m_nuggets.push_back(NuggetDesc(ETopo::LineList, EGeom::Vert|EGeom::Colr));

			// Create the model
			auto cdata = MeshCreationData()
				.verts  (m_verts)
				.indices(m_indices)
				.colours(m_colours)
				.nuggets(m_nuggets);
			obj->m_model = ModelGenerator::Mesh(m_pp.m_factory, cdata);
			obj->m_model->m_name = obj->TypeAndName();

			// Use thick lines
			if (m_line_width != 0.0f)
			{
				// Get or create an instance of the thick line shader
				auto shdr = Shader::Create<shaders::ThickLineListGS>(m_line_width);
				for (auto& nug : obj->m_model->m_nuggets)
					nug.m_shaders.push_back({ shdr, ERenderStep::RenderForward });
			}
		}
	};

	// ELdrObject::Grid
	template <> struct ObjectCreator<ELdrObject::Grid> :IObjectCreator
	{
		creation::DashedLines m_dashed;
		creation::MainAxis m_axis;
		float m_line_width;

		ObjectCreator(ParseParams& pp)
			: IObjectCreator(pp)
			, m_dashed()
			, m_axis()
			, m_line_width()
		{}
		bool ParseKeyword(IReader& reader, EKeyword kw) override
		{
			switch (kw)
			{
				case EKeyword::Data:
				{
					auto dim = reader.Vector2f();
					auto div = reader.Vector2f();

					auto step = dim / div;
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
					return true;
				}
				case EKeyword::Width:
				{
					m_line_width = reader.Real<float>();
					return true;
				}
				default:
				{
					return
						m_dashed.ParseKeyword(reader, m_pp, kw) ||
						m_axis.ParseKeyword(reader, m_pp, kw) ||
						IObjectCreator::ParseKeyword(reader, kw);
				}
			}
		}
		void CreateModel(LdrObject* obj, Location const&) override
		{
			// Validate
			if (m_verts.empty())
				return;

			// Convert lines to dashed lines
			auto is_line_list = true;
			m_dashed.Apply(m_verts, is_line_list);

			// Apply main axis transform
			m_axis.Apply(m_verts);

			// Create the model
			auto opts = ModelGenerator::CreateOptions().colours(m_colours);
			obj->m_model = ModelGenerator::Lines(m_pp.m_factory, isize(m_verts) / 2, m_verts, &opts);
			obj->m_model->m_name = obj->TypeAndName();

			// Use thick lines
			if (m_line_width != 0.0f)
			{
				// Get or create an instance of the thick line shader
				auto shdr = Shader::Create<shaders::ThickLineListGS>(m_line_width);
				for (auto& nug : obj->m_model->m_nuggets)
					nug.m_shaders.push_back({ shdr, ERenderStep::RenderForward });
			}
		}
	};

	// ELdrObject::Spline
	template <> struct ObjectCreator<ELdrObject::Spline> :IObjectCreator
	{
		pr::vector<Spline> m_splines;
		CCont m_spline_colours;
		float m_line_width;
		bool m_per_item_colour;

		ObjectCreator(ParseParams& pp)
			: IObjectCreator(pp)
			, m_splines()
			, m_spline_colours()
			, m_line_width()
			, m_per_item_colour()
		{}
		bool ParseKeyword(IReader& reader, EKeyword kw) override
		{
			switch (kw)
			{
				case EKeyword::Data:
				{
					Spline spline;
					spline.x = reader.Vector3f().w1();
					spline.y = reader.Vector3f().w1();
					spline.z = reader.Vector3f().w1();
					spline.w = reader.Vector3f().w1();
					m_splines.push_back(spline);
					if (m_per_item_colour)
					{
						Colour32 col = reader.Int<uint32_t>(16);
						m_spline_colours.push_back(col);
					}
					return true;
				}
				case EKeyword::Width:
				{
					m_line_width = reader.Real<float>();
					return true;
				}
				case EKeyword::PerItemColour:
				{
					m_per_item_colour = true;
					return true;
				}
				default:
				{
					return IObjectCreator::ParseKeyword(reader, kw);
				}
			}
		}
		void CreateModel(LdrObject* obj, Location const& loc) override
		{
			// Validate
			if (m_splines.empty())
				return;

			// Generate a line strip for all spline segments (separated using strip-cut indices)
			auto seg = -1;
			auto thick = m_line_width != 0.0f;
			pr::vector<v4, 30, true> raster;
			for (auto& spline : m_splines)
			{
				++seg;

				// Generate points for the spline
				raster.resize(0);
				Raster(spline, raster, 30);

				// Check for 16-bit index overflow
				if (m_verts.size() + raster.size() >= 0xFFFF)
				{
					m_pp.ReportError(EParseError::TooLarge, loc, std::format("Spline object '{}' is too large (index count >= 0xffff)", obj->TypeAndName()));
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
				if (m_per_item_colour)
				{
					auto ibeg = m_colours.size();
					m_colours.reserve(m_colours.size() + raster.size());
					
					auto iend = ibeg + raster.size();
					for (auto i = ibeg; i != iend; ++i)
						m_colours.push_back_fast(m_spline_colours[seg]);
				}
			}

			m_nuggets.push_back(NuggetDesc(ETopo::LineStrip, EGeom::Vert|EGeom::Colr));

			// Create the model
			auto cdata = MeshCreationData()
				.verts  (m_verts)
				.indices(m_indices)
				.colours(m_colours)
				.nuggets(m_nuggets);
			obj->m_model = ModelGenerator::Mesh(m_pp.m_factory, cdata);
			obj->m_model->m_name = obj->TypeAndName();

			// Use thick lines
			if (thick)
			{
				auto shdr = Shader::Create<shaders::ThickLineStripGS>(m_line_width);
				for (auto& nug : obj->m_model->m_nuggets)
				{
					nug.m_topo = ETopo::LineStripAdj;
					nug.m_shaders.push_back({ shdr, ERenderStep::RenderForward });
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
		bool m_per_item_colour;
		creation::SmoothLine m_smooth;

		ObjectCreator(ParseParams& pp)
			: IObjectCreator(pp)
			, m_type(EArrowType::Invalid)
			, m_line_width()
			, m_per_item_colour()
			, m_smooth()
		{}
		bool ParseKeyword(IReader& reader, EKeyword kw) override
		{
			switch (kw)
			{
				case EKeyword::Data:
				{
					// Expect the arrow type first
					if (m_type == EArrowType::Invalid)
					{
						auto ty = reader.Identifier<string32>();
						if (str::EqualNI(ty, "Line")) m_type = EArrowType::Line;
						else if (str::EqualNI(ty, "Fwd")) m_type = EArrowType::Fwd;
						else if (str::EqualNI(ty, "Back")) m_type = EArrowType::Back;
						else if (str::EqualNI(ty, "FwdBack")) m_type = EArrowType::FwdBack;
						else m_pp.ReportError(EParseError::InvalidValue, reader.Loc(), "arrow type must one of Line, Fwd, Back, FwdBack");
					}
					else
					{
						m_verts.push_back(reader.Vector3f().w1());
						if (m_per_item_colour)
							m_colours.push_back(reader.Int<uint32_t>(16));
					}
					return true;
				}
				case EKeyword::Width:
				{
					m_line_width = reader.Real<float>();
					return true;
				}
				case EKeyword::PerItemColour:
				{
					m_per_item_colour = true;
					return true;
				}
				default:
				{
					return
						m_smooth.ParseKeyword(reader, m_pp, kw) ||
						IObjectCreator::ParseKeyword(reader, kw);
				}
			}
		}
		void CreateModel(LdrObject* obj, Location const&) override
		{
			// Validate
			if (m_verts.size() < 2)
				return;

			// Convert the points into a spline if smooth is specified
			m_smooth.Apply(m_verts);

			// Geometry properties
			geometry::Props props;

			// Colour interpolation iterator
			auto col = pr::CreateLerpRepeater(m_colours.data(), isize(m_colours), isize(m_verts), Colour32White);
			auto cc = [&](pr::Colour32 c) { props.m_has_alpha |= HasAlpha(c); return c; };

			// Model bounding box
			auto bb = [&](v4 const& v) { Grow(props.m_bbox, v); return v; };

			// Generate the model
			// 'm_verts' should contain line strip data
			ModelGenerator::Cache<> cache(isize(m_verts) + 2, isize(m_verts) + 2, 0, 2);

			auto v_in  = m_verts.data();
			auto v_out = cache.m_vcont.data();
			auto i_out = cache.m_icont.data<uint16_t>();
			Colour32 c = Colour32White;
			uint16_t index = 0;

			// Add the back arrow head geometry (a point)
			if (m_type & EArrowType::Back)
			{
				SetPCN(*v_out++, *v_in, Colour(*col), pr::Normalise(*v_in - *(v_in+1)));
				*i_out++ = index++;
			}

			// Add the line strip
			for (std::size_t i = 0, iend = m_verts.size(); i != iend; ++i)
			{
				SetPC(*v_out++, bb(*v_in++), Colour(c = cc(*col++)));
				*i_out++ = index++;
			}
			
			// Add the forward arrow head geometry (a point)
			if (m_type & EArrowType::Fwd)
			{
				--v_in;
				SetPCN(*v_out++, *v_in, Colour(c), pr::Normalise(*v_in - *(v_in-1)));
				*i_out++ = index++;
			}

			// Create the model
			ModelDesc mdesc(cache.m_vcont.cspan(), cache.m_icont.span<uint16_t const>(), props.m_bbox, obj->TypeAndName().c_str());
			obj->m_model = m_pp.m_factory.CreateModel(mdesc);

			// Get instances of the arrow head geometry shader and the thick line shader
			auto thk_shdr = Shader::Create<shaders::ThickLineListGS>(m_line_width);
			auto arw_shdr = Shader::Create<shaders::ArrowHeadGS>(m_line_width*2);

			// Create nuggets
			Range vrange(0,0);
			Range irange(0,0);
			if (m_type & EArrowType::Back)
			{
				vrange = Range(0, 1);
				irange = Range(0, 1);
				NuggetDesc nug;
				nug.m_topo = ETopo::PointList;
				nug.m_geom = EGeom::Vert|EGeom::Colr;
				nug.m_vrange = vrange;
				nug.m_irange = irange;
				nug.m_nflags = SetBits(nug.m_nflags, ENuggetFlag::GeometryHasAlpha, cache.m_vcont[0].m_diff.a != 1.0f);
				nug.m_shaders.push_back({ arw_shdr, ERenderStep::RenderForward });
				obj->m_model->CreateNugget(m_pp.m_factory, nug);
			}
			if (true)
			{
				vrange = Range(vrange.m_end, vrange.m_end + m_verts.size());
				irange = Range(irange.m_end, irange.m_end + m_verts.size());
				NuggetDesc nug;
				nug.m_topo = ETopo::LineStrip;
				nug.m_geom = EGeom::Vert|EGeom::Colr;
				nug.m_vrange = vrange;
				nug.m_irange = irange;
				nug.m_nflags = SetBits(nug.m_nflags, ENuggetFlag::GeometryHasAlpha, props.m_has_alpha);
				if (m_line_width != 0) nug.m_shaders.push_back({ thk_shdr, ERenderStep::RenderForward });
				obj->m_model->CreateNugget(m_pp.m_factory, nug);
			}
			if (m_type & EArrowType::Fwd)
			{
				vrange = Range(vrange.m_end, vrange.m_end + 1);
				irange = Range(irange.m_end, irange.m_end + 1);
				NuggetDesc nug;
				nug.m_topo = ETopo::PointList;
				nug.m_geom = EGeom::Vert|EGeom::Colr;
				nug.m_vrange = vrange;
				nug.m_irange = irange;
				nug.m_nflags = SetBits(nug.m_nflags, ENuggetFlag::GeometryHasAlpha, cache.m_vcont.back().m_diff.a != 1.0f);
				nug.m_shaders.push_back({ arw_shdr, ERenderStep::RenderForward });
				obj->m_model->CreateNugget(m_pp.m_factory, nug);
			}
		}
	};

	// ELdrObject::Matrix3x3
	template <> struct ObjectCreator<ELdrObject::Matrix3x3> :IObjectCreator
	{
		float m_line_width;

		ObjectCreator(ParseParams& pp)
			: IObjectCreator(pp)
			, m_line_width()
		{}
		bool ParseKeyword(IReader& reader, EKeyword kw) override
		{
			switch (kw)
			{
				case EKeyword::Data:
				{
					auto basis = m4x4{ reader.Matrix3x3(), v4::Origin() };

					v4       pts[] = { v4::Origin(), basis.x.w1(), v4::Origin(), basis.y.w1(), v4::Origin(), basis.z.w1() };
					Colour32 col[] = { Colour32Red, Colour32Red, Colour32Green, Colour32Green, Colour32Blue, Colour32Blue };
					uint16_t idx[] = { 0, 1, 2, 3, 4, 5 };

					m_verts.insert(m_verts.end(), pts, pts + _countof(pts));
					m_colours.insert(m_colours.end(), col, col + _countof(col));
					m_indices.insert(m_indices.end(), idx, idx + _countof(idx));
					return true;
				}
				case EKeyword::Width:
				{
					m_line_width = reader.Real<float>();
					return true;
				}
				default:
				{
					return IObjectCreator::ParseKeyword(reader, kw);
				}
			}
		}
		void CreateModel(LdrObject* obj, Location const&) override
		{
			// Validate
			if (m_verts.empty())
				return;

			m_nuggets.push_back(NuggetDesc(ETopo::LineList, EGeom::Vert|EGeom::Colr));

			// Create the model
			auto cdata = MeshCreationData()
				.verts(m_verts)
				.indices(m_indices)
				.colours(m_colours)
				.nuggets(m_nuggets);
			obj->m_model = ModelGenerator::Mesh(m_pp.m_factory, cdata);
			obj->m_model->m_name = obj->TypeAndName();

			// Use thick lines
			if (m_line_width != 0.0f)
			{
				// Get or create an instance of the thick line shader
				auto shdr = Shader::Create<shaders::ThickLineListGS>(m_line_width);
				for (auto& nug : obj->m_model->m_nuggets)
					nug.m_shaders.push_back({ shdr, ERenderStep::RenderForward });
			}
		}
	};

	// ELdrObject::CoordFrame
	template <> struct ObjectCreator<ELdrObject::CoordFrame> :IObjectCreator
	{
		float m_line_width;
		float m_scale;
		bool m_rh;

		ObjectCreator(ParseParams& pp)
			: IObjectCreator(pp)
			, m_line_width()
			, m_scale()
			, m_rh(true)
		{
			v4       pts[] = { v4::Origin(), v4::XAxis().w1(), v4::Origin(), v4::YAxis().w1(), v4::Origin(), v4::ZAxis().w1() };
			Colour32 col[] = { Colour32Red, Colour32Red, Colour32Green, Colour32Green, Colour32Blue, Colour32Blue };
			uint16_t idx[] = { 0, 1, 2, 3, 4, 5 };

			m_verts.insert(m_verts.end(), pts, pts + _countof(pts));
			m_colours.insert(m_colours.end(), col, col + _countof(col));
			m_indices.insert(m_indices.end(), idx, idx + _countof(idx));
		}
		bool ParseKeyword(IReader& reader, EKeyword kw) override
		{
			switch (kw)
			{
				case EKeyword::Width:
				{
					m_line_width = reader.Real<float>();
					return true;
				}
				case EKeyword::Scale:
				{
					m_scale = reader.Real<float>();
					return true;
				}
				case EKeyword::LeftHanded:
				{
					m_rh = false;
					return true;
				}
				default:
				{
					return IObjectCreator::ParseKeyword(reader, kw);
				}
			}
		}
		void CreateModel(LdrObject* obj, Location const&) override
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
			auto opts = ModelGenerator::CreateOptions().colours(m_colours);
			obj->m_model = ModelGenerator::Lines(m_pp.m_factory, int(m_verts.size() / 2), m_verts, &opts);
			obj->m_model->m_name = obj->TypeAndName();

			// Use thick lines
			if (m_line_width != 0.0f)
			{
				// Get or create an instance of the thick line shader
				auto shdr = Shader::Create<shaders::ThickLineListGS>(m_line_width);
				for (auto& nug : obj->m_model->m_nuggets)
					nug.m_shaders.push_back({ shdr, ERenderStep::RenderForward });
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

		ObjectCreator(ParseParams& pp)
			: IObjectCreator(pp)
			, m_tex(SamDesc::AnisotropicClamp())
			, m_axis()
			, m_dim()
			, m_facets(40)
			, m_solid()
		{}
		bool ParseKeyword(IReader& reader, EKeyword kw) override
		{
			switch (kw)
			{
				case EKeyword::Data:
				{
					m_dim.x = reader.Real<float>();
					m_dim.y = reader.IsSectionEnd() ? m_dim.x : reader.Real<float>();
					if (Abs(m_dim) != m_dim)
					{
						m_pp.ReportError(EParseError::InvalidValue, reader.Loc(), "Circle dimensions contain a negative value");
						m_dim = Abs(m_dim);
					}

					return true;
				}
				case EKeyword::Solid:
				{
					m_solid = true;
					return true;
				}
				case EKeyword::Facets:
				{
					m_facets = reader.Int<int>(10);
					return true;
				}
				default:
				{
					return
						m_axis.ParseKeyword(reader, m_pp, kw) ||
						m_tex.ParseKeyword(reader, m_pp, kw) ||
						IObjectCreator::ParseKeyword(reader, kw);
				}
			}
		}
		void CreateModel(LdrObject* obj, Location const&) override
		{
			// Create the model
			auto opts = ModelGenerator::CreateOptions().colours(m_colours).bake(m_axis.O2WPtr()).tex_diffuse(m_tex.m_texture, m_tex.m_sampler);
			obj->m_model = ModelGenerator::Ellipse(m_pp.m_factory, m_dim.x, m_dim.y, m_solid, m_facets, &opts);
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

		ObjectCreator(ParseParams& pp)
			: IObjectCreator(pp)
			, m_tex(SamDesc::AnisotropicClamp())
			, m_axis()
			, m_scale(v2One)
			, m_ang()
			, m_rad()
			, m_facets(40)
			, m_solid()
		{}
		bool ParseKeyword(IReader& reader, EKeyword kw) override
		{
			switch (kw)
			{
				case EKeyword::Data:
				{
					m_ang = reader.Vector2f();
					m_rad = reader.Vector2f();
					m_ang.x = DegreesToRadians(m_ang.x);
					m_ang.y = DegreesToRadians(m_ang.y);
					return true;
				}
				case EKeyword::Solid:
				{
					m_solid = true;
					return true;
				}
				case EKeyword::Scale:
				{
					m_scale = reader.Vector2f();
					return true;
				}
				case EKeyword::Facets:
				{
					m_facets = reader.Int<int>(10);
					return true;
				}
				default:
				{
					return
						m_axis.ParseKeyword(reader, m_pp, kw) ||
						m_tex.ParseKeyword(reader, m_pp, kw) ||
						IObjectCreator::ParseKeyword(reader, kw);
				}
			}
		}
		void CreateModel(LdrObject* obj, Location const&) override
		{
			// Create the model
			auto opts = ModelGenerator::CreateOptions().colours(m_colours).bake(m_axis.O2WPtr()).tex_diffuse(m_tex.m_texture, m_tex.m_sampler);
			obj->m_model = ModelGenerator::Pie(m_pp.m_factory, m_scale.x, m_scale.y, m_ang.x, m_ang.y, m_rad.x, m_rad.y, m_solid, m_facets, &opts);
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

		ObjectCreator(ParseParams& pp)
			: IObjectCreator(pp)
			, m_tex(SamDesc::AnisotropicClamp())
			, m_axis()
			, m_dim()
			, m_corner_radius()
			, m_facets(40)
			, m_solid()
		{}
		bool ParseKeyword(IReader& reader, EKeyword kw) override
		{
			switch (kw)
			{
				case EKeyword::Data:
				{
					m_dim.x = reader.Real<float>();
					m_dim.y = reader.IsSectionEnd() ? m_dim.x : reader.Real<float>();
					m_dim *= 0.5f;

					if (Abs(m_dim) != m_dim)
					{
						m_pp.ReportError(EParseError::InvalidValue, reader.Loc(), "Rect dimensions contain a negative value");
						m_dim = Abs(m_dim);
					}
					return true;
				}
				case EKeyword::CornerRadius:
				{
					m_corner_radius = reader.Real<float>();
					return true;
				}
				case EKeyword::Facets:
				{
					m_facets = reader.Int<int>(10);
					m_facets *= 4;
					return true;
				}
				case EKeyword::Solid:
				{
					m_solid = true;
					return true;
				}
				default:
				{
					return
						m_axis.ParseKeyword(reader, m_pp, kw) ||
						m_tex.ParseKeyword(reader, m_pp, kw) ||
						IObjectCreator::ParseKeyword(reader, kw);
				}
			}
		}
		void CreateModel(LdrObject* obj, Location const&) override
		{
			// Create the model
			auto opts = ModelGenerator::CreateOptions().colours(m_colours).bake(m_axis.O2WPtr()).tex_diffuse(m_tex.m_texture, m_tex.m_sampler);
			obj->m_model = ModelGenerator::RoundedRectangle(m_pp.m_factory, m_dim.x, m_dim.y, m_corner_radius, m_solid, m_facets, &opts);
			obj->m_model->m_name = obj->TypeAndName();
		}
	};

	// ELdrObject::Polygon
	template <> struct ObjectCreator<ELdrObject::Polygon> :IObjectCreator
	{
		creation::Textured m_tex;
		creation::MainAxis m_axis;
		pr::vector<v2> m_poly;
		bool m_per_item_colour;
		bool m_solid;

		ObjectCreator(ParseParams& pp)
			: IObjectCreator(pp)
			, m_tex(SamDesc::AnisotropicClamp())
			, m_axis()
			, m_poly()
			, m_per_item_colour()
			, m_solid()
		{}
		bool ParseKeyword(IReader& reader, EKeyword kw) override
		{
			switch (kw)
			{
				case EKeyword::Data:
				{
					for (; !reader.IsSectionEnd(); )
					{
						m_poly.push_back(reader.Vector2f());
						if (m_per_item_colour)
							m_colours.push_back(reader.Int<uint32_t>(16));
					}
					return true;
				}
				case EKeyword::Solid:
				{
					m_solid = true;
					return true;
				}
				case EKeyword::PerItemColour:
				{
					m_per_item_colour = true;
					return true;
				}
				default:
				{
					return
						m_axis.ParseKeyword(reader, m_pp, kw) ||
						m_tex.ParseKeyword(reader, m_pp, kw) ||
						IObjectCreator::ParseKeyword(reader, kw);
				}
			}
		}
		void CreateModel(LdrObject* obj, Location const&) override
		{
			// Check the polygon winding order
			if (geometry::PolygonArea(m_poly) < 0)
			{
				std::ranges::reverse(m_poly);
				std::ranges::reverse(m_colours);
			}

			// Create the model
			auto opts = ModelGenerator::CreateOptions().colours(m_colours).bake(m_axis.O2WPtr()).tex_diffuse(m_tex.m_texture, m_tex.m_sampler);
			obj->m_model = ModelGenerator::Polygon(m_pp.m_factory, m_poly, m_solid, &opts);
			obj->m_model->m_name = obj->TypeAndName();
		}
	};

	#pragma endregion

	#pragma region Quads

	// ELdrObject::Triangle
	template <> struct ObjectCreator<ELdrObject::Triangle> :IObjectCreator
	{
		creation::Textured m_tex;
		creation::MainAxis m_axis;
		bool m_per_item_colour;

		ObjectCreator(ParseParams& pp)
			: IObjectCreator(pp)
			, m_tex(SamDesc::AnisotropicClamp())
			, m_axis()
			, m_per_item_colour()
		{}
		bool ParseKeyword(IReader& reader, EKeyword kw) override
		{
			switch (kw)
			{
				case EKeyword::Data:
				{
					v4 pt[3] = {};
					Colour32 col[3] = {};
					for (int i = 0; i != 3; ++i)
					{
						pt[i] = reader.Vector3f().w1();
						if (m_per_item_colour)
							col[i] = reader.Int<uint32_t>(16);
					}

					m_verts.push_back(pt[0]);
					m_verts.push_back(pt[1]);
					m_verts.push_back(pt[2]);
					m_verts.push_back(pt[2]); // create a degenerate
					if (m_per_item_colour)
					{
						m_colours.push_back(col[0]);
						m_colours.push_back(col[1]);
						m_colours.push_back(col[2]);
						m_colours.push_back(col[2]);
					}
					return true;
				}
				case EKeyword::PerItemColour:
				{
					m_per_item_colour = true;
					return true;
				}
				default:
				{
					return
						m_axis.ParseKeyword(reader, m_pp, kw) ||
						m_tex.ParseKeyword(reader, m_pp, kw) ||
						IObjectCreator::ParseKeyword(reader, kw);
				}
			}
		}
		void CreateModel(LdrObject* obj, Location const&) override
		{
			// Validate
			if (m_verts.empty() || (isize(m_verts) % 4) != 0)
				return;

			// Apply the axis id rotation
			m_axis.Apply(m_verts);

			// Create the model
			auto opts = ModelGenerator::CreateOptions().colours(m_colours).bake(m_axis.O2WPtr()).tex_diffuse(m_tex.m_texture, m_tex.m_sampler);
			obj->m_model = ModelGenerator::Quad(m_pp.m_factory, isize(m_verts) / 4, m_verts, &opts);
			obj->m_model->m_name = obj->TypeAndName();
		}
	};

	// ELdrObject::Quad
	template <> struct ObjectCreator<ELdrObject::Quad> :IObjectCreator
	{
		creation::Textured m_tex;
		creation::MainAxis m_axis;
		bool m_per_item_colour;

		ObjectCreator(ParseParams& pp)
			: IObjectCreator(pp)
			, m_tex(SamDesc::AnisotropicClamp())
			, m_axis()
			, m_per_item_colour()
		{}
		bool ParseKeyword(IReader& reader, EKeyword kw) override
		{
			switch (kw)
			{
				case EKeyword::Data:
				{
					v4 pt[4] = {};
					Colour32 col[4] = {};
					for (int i = 0; i != 4; ++i)
					{
						pt[i] = reader.Vector3f().w1();
						if (m_per_item_colour)
							col[i] = reader.Int<uint32_t>(16);
					}

					m_verts.push_back(pt[0]);
					m_verts.push_back(pt[1]);
					m_verts.push_back(pt[2]);
					m_verts.push_back(pt[3]);
					if (m_per_item_colour)
					{
						m_colours.push_back(col[0]);
						m_colours.push_back(col[1]);
						m_colours.push_back(col[2]);
						m_colours.push_back(col[3]);
					}
					return true;
				}
				case EKeyword::PerItemColour:
				{
					m_per_item_colour = true;
					return true;
				}
				default:
				{
					return
						m_axis.ParseKeyword(reader, m_pp, kw) ||
						m_tex.ParseKeyword(reader, m_pp, kw) ||
						IObjectCreator::ParseKeyword(reader, kw);
				}
			}
		}
		void CreateModel(LdrObject* obj, Location const&) override
		{
			// Validate
			if (m_verts.empty() || (isize(m_verts) % 4) != 0)
				return;

			// Already done in 'bake()'?
			//// Apply the axis id rotation
			//m_axis.Apply(m_verts);

			// Create the model
			auto opts = ModelGenerator::CreateOptions().colours(m_colours).bake(m_axis.O2WPtr()).tex_diffuse(m_tex.m_texture, m_tex.m_sampler);
			obj->m_model = ModelGenerator::Quad(m_pp.m_factory, isize(m_verts) / 4, m_verts, &opts);
			obj->m_model->m_name = obj->TypeAndName();
		}
	};

	// ELdrObject::Plane
	template <> struct ObjectCreator<ELdrObject::Plane> :IObjectCreator
	{
		creation::Textured m_tex;
		creation::MainAxis m_axis_id;
		v2 m_dim;

		ObjectCreator(ParseParams& pp)
			: IObjectCreator(pp)
			, m_tex(SamDesc::AnisotropicWrap())
			, m_dim()
		{}
		bool ParseKeyword(IReader& reader, EKeyword kw) override
		{
			switch (kw)
			{
				case EKeyword::Data:
				{
					m_dim.x = reader.Real<float>();
					m_dim.y = reader.Real<float>();
					m_dim *= 0.5f;
					return true;
				}
				default:
				{
					return
						m_axis_id.ParseKeyword(reader, m_pp, kw) ||
						m_tex.ParseKeyword(reader, m_pp, kw) ||
						IObjectCreator::ParseKeyword(reader, kw);
				}
			}
		}
		void CreateModel(LdrObject* obj, Location const&) override
		{
			if (m_dim == v2::Zero())
				return;

			v4 verts[4] = {
				v4{-m_dim.x, -m_dim.y, 0, 1},
				v4{+m_dim.x, -m_dim.y, 0, 1},
				v4{-m_dim.x, +m_dim.y, 0, 1},
				v4{+m_dim.x, +m_dim.y, 0, 1},
			};

			// Create the model
			auto opts = ModelGenerator::CreateOptions().bake(m_axis_id.O2WPtr()).tex_diffuse(m_tex.m_texture, m_tex.m_sampler);
			obj->m_model = ModelGenerator::Quad(m_pp.m_factory, 1, verts, &opts);
			obj->m_model->m_name = obj->TypeAndName();
		}
	};

	// ELdrObject::Ribbon
	template <> struct ObjectCreator<ELdrObject::Ribbon> :IObjectCreator
	{
		// Notes:
		//  - Defaulting to 'clamp' because ribbons use the first row of the 2D texture and extrude it.
		//    This doesn't work with 'wrap' or 'border' modes.

		creation::Textured m_tex;
		creation::MainAxis m_axis;
		creation::SmoothLine m_smooth;
		float m_width;
		bool m_per_item_colour;

		ObjectCreator(ParseParams& pp)
			: IObjectCreator(pp)
			, m_tex(SamDesc::AnisotropicClamp())
			, m_axis()
			, m_smooth()
			, m_width(10.0f)
			, m_per_item_colour()
		{}
		bool ParseKeyword(IReader& reader, EKeyword kw) override
		{
			switch (kw)
			{
				case EKeyword::Data:
				{
					m_verts.push_back(reader.Vector3f().w1());
					if (m_per_item_colour)
						m_colours.push_back(reader.Int<uint32_t>(16));
					return true;
				}
				case EKeyword::Width:
				{
					m_width = reader.Real<float>();
					return true;
				}
				case EKeyword::PerItemColour:
				{
					m_per_item_colour = true;
					return true;
				}
				default:
				{
					return
						m_smooth.ParseKeyword(reader, m_pp, kw) ||
						m_axis.ParseKeyword(reader, m_pp, kw) ||
						m_tex.ParseKeyword(reader, m_pp, kw) ||
						IObjectCreator::ParseKeyword(reader, kw);
				}
			}
		}
		void CreateModel(LdrObject* obj, Location const&) override
		{
			// Validate
			if (isize(m_verts) < 2)
				return;

			// Smooth the points
			m_smooth.Apply(m_verts);

			v4 normal = m_axis.m_align;
			auto opts = ModelGenerator::CreateOptions().colours(m_colours).bake(m_axis.O2WPtr()).tex_diffuse(m_tex.m_texture, m_tex.m_sampler);
			obj->m_model = ModelGenerator::QuadStrip(m_pp.m_factory, isize(m_verts) - 1, m_verts, m_width, { &normal, 1 }, &opts);
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

		ObjectCreator(ParseParams& pp)
			: IObjectCreator(pp)
			, m_tex(SamDesc::AnisotropicClamp())
			, m_dim()
		{}
		bool ParseKeyword(IReader& reader, EKeyword kw) override
		{
			switch (kw)
			{
				case EKeyword::Data:
				{
					m_dim.x = reader.Real<float>();
					m_dim.y = reader.IsSectionEnd() ? m_dim.x : reader.Real<float>();
					m_dim.z = reader.IsSectionEnd() ? m_dim.y : reader.Real<float>();
					m_dim *= 0.5f;
					return true;
				}
				default:
				{
					return
						m_tex.ParseKeyword(reader, m_pp, kw) ||
						IObjectCreator::ParseKeyword(reader, kw);
				}
			}
		}
		void CreateModel(LdrObject* obj, Location const&) override
		{
			// Create the model
			auto opts = ModelGenerator::CreateOptions().colours(m_colours).tex_diffuse(m_tex.m_texture, m_tex.m_sampler);
			obj->m_model = ModelGenerator::Box(m_pp.m_factory, m_dim, &opts);
			obj->m_model->m_name = obj->TypeAndName();
		}
	};

	// ELdrObject::Bar
	template <> struct ObjectCreator<ELdrObject::Bar> :IObjectCreator
	{
		creation::Textured m_tex;
		v4 m_pt0, m_pt1, m_up;
		float m_width, m_height;

		ObjectCreator(ParseParams& pp)
			: IObjectCreator(pp)
			, m_tex(SamDesc::AnisotropicClamp())
			, m_pt0()
			, m_pt1()
			, m_up(v4::YAxis())
			, m_width(0.1f)
			, m_height(0.1f)
		{}
		bool ParseKeyword(IReader& reader, EKeyword kw) override
		{
			switch (kw)
			{
				case EKeyword::Data:
				{
					m_pt0 = reader.Vector3f().w1();
					m_pt1 = reader.Vector3f().w1();
					m_width = reader.Real<float>();
					m_height = reader.IsSectionEnd() ? m_width : reader.Real<float>();
					return true;
				}
				case EKeyword::Up:
				{
					m_up = reader.Vector3f().w0();
					return true;
				}
				default:
				{
					return
						m_tex.ParseKeyword(reader, m_pp, kw) ||
						IObjectCreator::ParseKeyword(reader, kw);
				}
			}
		}
		void CreateModel(LdrObject* obj, Location const&) override
		{
			auto dim = v4(m_width, m_height, Length(m_pt1 - m_pt0), 0.0f) * 0.5f;
			auto b2w = OriFromDir(m_pt1 - m_pt0, AxisId::PosZ, m_up, (m_pt1 + m_pt0) * 0.5f);
			auto opts = ModelGenerator::CreateOptions().colours(m_colours).bake(b2w).tex_diffuse(m_tex.m_texture, m_tex.m_sampler);
			obj->m_model = ModelGenerator::Box(m_pp.m_factory, dim, &opts);
			obj->m_model->m_name = obj->TypeAndName();
		}
	};

	// ELdrObject::BoxList
	template <> struct ObjectCreator<ELdrObject::BoxList> :IObjectCreator
	{
		creation::Textured m_tex;
		pr::vector<v4,16> m_location;
		v4 m_dim;

		ObjectCreator(ParseParams& pp)
			: IObjectCreator(pp)
			, m_tex(SamDesc::AnisotropicClamp())
			, m_location()
			, m_dim()
		{}
		bool ParseKeyword(IReader& reader, EKeyword kw) override
		{
			switch (kw)
			{
				case EKeyword::Data:
				{
					m_dim = reader.Vector3f().w0();
					for (; !reader.IsSectionEnd(); )
						m_location.push_back(reader.Vector3f().w1());

					return true;
				}
				default:
				{
					return
						m_tex.ParseKeyword(reader, m_pp, kw) ||
						IObjectCreator::ParseKeyword(reader, kw);
				}
			}
		}
		void CreateModel(LdrObject* obj, Location const& loc) override
		{
			// Validate
			if (m_dim == v4::Zero() || m_location.empty())
				return;

			if (Abs(m_dim) != m_dim)
			{
				m_pp.ReportError(EParseError::InvalidValue, loc, "BoxList box dimensions contain a negative value");
				m_dim = Abs(m_dim);
			}

			m_dim *= 0.5f;

			// Create the model
			auto opts = ModelGenerator::CreateOptions().colours(m_colours).tex_diffuse(m_tex.m_texture, m_tex.m_sampler);
			obj->m_model = ModelGenerator::BoxList(m_pp.m_factory, isize(m_location), m_location, m_dim, &opts);
			obj->m_model->m_name = obj->TypeAndName();
		}
	};

	// ELdrObject::FrustumWH
	template <> struct ObjectCreator<ELdrObject::FrustumWH> :IObjectCreator
	{
		creation::Textured m_tex;
		creation::MainAxis m_axis;
		v4 m_pt[8];
		float m_width, m_height;
		float m_near, m_far;
		float m_view_plane;

		ObjectCreator(ParseParams& pp)
			: IObjectCreator(pp)
			, m_tex(SamDesc::AnisotropicClamp())
			, m_axis()
			, m_pt()
			, m_width(1.0f)
			, m_height(1.0f)
			, m_near(0.0f)
			, m_far(1.0f)
			, m_view_plane(0.0f)
		{}
		bool ParseKeyword(IReader& reader, EKeyword kw) override
		{
			switch (kw)
			{
				case EKeyword::Data:
				{
					m_width = reader.Real<float>();
					m_height = reader.Real<float>();
					m_near = reader.Real<float>();
					m_far = reader.Real<float>();
					return true;
				}
				case EKeyword::ViewPlaneZ:
				{
					m_view_plane = reader.Real<float>();
					return true;
				}
				default:
				{
					return
						m_axis.ParseKeyword(reader, m_pp, kw) ||
						m_tex.ParseKeyword(reader, m_pp, kw) ||
						IObjectCreator::ParseKeyword(reader, kw);
				}
			}
		}
		void CreateModel(LdrObject* obj, Location const&) override
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

			auto opts = ModelGenerator::CreateOptions().colours(m_colours).bake(m_axis.O2WPtr()).tex_diffuse(m_tex.m_texture, m_tex.m_sampler);
			obj->m_model = ModelGenerator::Boxes(m_pp.m_factory, 1, m_pt, &opts);
			obj->m_model->m_name = obj->TypeAndName();
		}
	};

	// ELdrObject::FrustumFA
	template <> struct ObjectCreator<ELdrObject::FrustumFA> :IObjectCreator
	{
		creation::Textured m_tex;
		creation::MainAxis m_axis;
		v4 m_pt[8];
		float m_fovY, m_aspect;
		float m_near, m_far;

		ObjectCreator(ParseParams& pp)
			: IObjectCreator(pp)
			, m_tex(SamDesc::AnisotropicClamp())
			, m_axis()
			, m_pt()
			, m_fovY(maths::tau_by_8f)
			, m_aspect(1.0f)
			, m_near(0.0f)
			, m_far(1.0f)
		{}
		bool ParseKeyword(IReader& reader, EKeyword kw) override
		{
			switch (kw)
			{
				case EKeyword::Data:
				{
					m_fovY = reader.Real<float>();
					m_aspect = reader.Real<float>();
					m_near = reader.Real<float>();
					m_far = reader.Real<float>();
					return true;
				}
				default:
				{
					return
						m_axis.ParseKeyword(reader, m_pp, kw) ||
						m_tex.ParseKeyword(reader, m_pp, kw) ||
						IObjectCreator::ParseKeyword(reader, kw);
				}
			}
		}
		void CreateModel(LdrObject* obj, Location const&) override
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

			auto opts = ModelGenerator::CreateOptions().colours(m_colours).bake(m_axis.O2WPtr()).tex_diffuse(m_tex.m_texture, m_tex.m_sampler);
			obj->m_model = ModelGenerator::Boxes(m_pp.m_factory, 1, m_pt, &opts);
			obj->m_model->m_name = obj->TypeAndName();
		}
	};

	// ELdrObject::Sphere
	template <> struct ObjectCreator<ELdrObject::Sphere> :IObjectCreator
	{
		creation::Textured m_tex;
		v4 m_dim;
		int m_facets;

		ObjectCreator(ParseParams& pp)
			: IObjectCreator(pp)
			, m_tex(SamDesc::AnisotropicWrap())
			, m_dim()
			, m_facets(3)
		{}
		bool ParseKeyword(IReader& reader, EKeyword kw) override
		{
			switch (kw)
			{
				case EKeyword::Data:
				{
					m_dim.x = reader.Real<float>();
					m_dim.y = reader.IsSectionEnd() ? m_dim.x : reader.Real<float>();
					m_dim.z = reader.IsSectionEnd() ? m_dim.y : reader.Real<float>();
					return true;
				}
				case EKeyword::Facets:
				{
					m_facets = reader.Int<int>(10);
					return true;
				}
				default:
				{
					return
						m_tex.ParseKeyword(reader, m_pp, kw) ||
						IObjectCreator::ParseKeyword(reader, kw);
				}
			}
		}
		void CreateModel(LdrObject* obj, Location const&) override
		{
			auto opts = ModelGenerator::CreateOptions().colours(m_colours).tex_diffuse(m_tex.m_texture, m_tex.m_sampler);
			obj->m_model = ModelGenerator::Geosphere(m_pp.m_factory, m_dim, m_facets, &opts);
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

		ObjectCreator(ParseParams& pp)
			: IObjectCreator(pp)
			, m_axis()
			, m_tex(SamDesc::AnisotropicClamp())
			, m_dim()
			, m_scale(v2::One())
			, m_layers(1)
			, m_wedges(20)
		{}
		bool ParseKeyword(IReader& reader, EKeyword kw) override
		{
			switch (kw)
			{
				case EKeyword::Data:
				{
					m_dim.z = reader.Real<float>();
					m_dim.x = reader.Real<float>();
					m_dim.y = reader.IsSectionEnd() ? m_dim.x : reader.Real<float>();
					return true;
				}
				case EKeyword::Facets:
				{
					auto facets = reader.Vector2i(10);
					m_layers = facets.x;
					m_wedges = facets.y;
					return true;
				}
				case EKeyword::Scale:
				{
					m_scale = reader.Vector2f();
					return true;
				}
				default:
				{
					return
						m_axis.ParseKeyword(reader, m_pp, kw) ||
						m_tex.ParseKeyword(reader, m_pp, kw) ||
						IObjectCreator::ParseKeyword(reader, kw);
				}
			}
		}
		void CreateModel(LdrObject* obj, Location const&) override
		{
			// Create the model
			auto opts = ModelGenerator::CreateOptions().colours(m_colours).bake(m_axis.O2WPtr()).tex_diffuse(m_tex.m_texture, m_tex.m_sampler);
			obj->m_model = ModelGenerator::Cylinder(m_pp.m_factory, m_dim.x, m_dim.y, m_dim.z, m_scale.x, m_scale.y, m_wedges, m_layers, &opts);
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

		ObjectCreator(ParseParams& pp)
			: IObjectCreator(pp)
			, m_axis()
			, m_tex(SamDesc::AnisotropicClamp())
			, m_dim()
			, m_scale(v2::One())
			, m_layers(1)
			, m_wedges(20)
		{}
		bool ParseKeyword(IReader& reader, EKeyword kw) override
		{
			switch (kw)
			{
				case EKeyword::Data:
				{
					auto h0 = reader.Real<float>();
					auto h1 = reader.Real<float>();
					auto a = reader.Real<float>();

					a = DegreesToRadians(a);

					m_dim.z = h1 - h0;
					m_dim.x = h0 * Tan(a);
					m_dim.y = h1 * Tan(a);
					return true;
				}
				case EKeyword::Facets:
				{
					auto facets = reader.Vector2i(10);
					m_layers = facets.x;
					m_wedges = facets.y;
					return true;
				}
				case EKeyword::Scale:
				{
					m_scale = reader.Vector2f();
					return true;
				}
				default:
				{
					return
						m_axis.ParseKeyword(reader, m_pp, kw) ||
						m_tex.ParseKeyword(reader, m_pp, kw) ||
						IObjectCreator::ParseKeyword(reader, kw);
				}
			}
		}
		void CreateModel(LdrObject* obj, Location const&) override
		{
			// Create the model
			auto opts = ModelGenerator::CreateOptions().colours(m_colours).bake(m_axis.O2WPtr()).tex_diffuse(m_tex.m_texture, m_tex.m_sampler);
			obj->m_model = ModelGenerator::Cylinder(m_pp.m_factory, m_dim.x, m_dim.y, m_dim.z, m_scale.x, m_scale.y, m_wedges, m_layers, &opts);
			obj->m_model->m_name = obj->TypeAndName();
		}
	};

	// ELdrObject::Tube
	template <> struct ObjectCreator<ELdrObject::Tube> :IObjectCreator
	{
		enum class ECSType
		{
			Invalid,
			Round,
			Square,
			Polygon,
		};

		pr::vector<v2> m_cs;           // 2d cross section
		v2 m_radius;                   // X,Y radii for implicit cross sections
		ECSType m_cs_type;             // Cross section type
		int m_cs_facets;               // The number of divisions for Round cross sections
		bool m_cs_smooth;              // True if outward normals for the tube are smoothed
		bool m_per_item_colour;        // Colour per vertex
		bool m_closed;                 // True if the tube end caps should be filled in
		creation::SmoothLine m_smooth; // True if the extrusion path is smooth

		ObjectCreator(ParseParams& pp)
			: IObjectCreator(pp)
			, m_cs()
			, m_radius()
			, m_cs_type(ECSType::Invalid)
			, m_cs_facets(20)
			, m_cs_smooth(false)
			, m_per_item_colour()
			, m_closed(false)
			, m_smooth()
		{}
		bool ParseKeyword(IReader& reader, EKeyword kw) override
		{
			switch (kw)
			{
				case EKeyword::Data:
				{
					// Parse the extrusion path
					auto pt = reader.Vector3f().w1();
					auto col = m_per_item_colour ? Colour32(reader.Int<uint32_t>(16)) : Colour32White;

					// Ignore degenerates
					if (m_verts.empty() || !FEql(m_verts.back(), pt))
					{
						m_verts.push_back(pt);
						if (m_per_item_colour)
							m_colours.push_back(col);
					}

					return true;
				}
				case EKeyword::CrossSection:
				{
					auto section = reader.SectionScope();
					ParseCrossSection(reader);
					return true;
				}
				case EKeyword::Closed:
				{
					m_closed = true;
					return true;
				}
				case EKeyword::PerItemColour:
				{
					m_per_item_colour = true;
					return true;
				}
				default:
				{
					return
						m_smooth.ParseKeyword(reader, m_pp, kw) ||
						IObjectCreator::ParseKeyword(reader, kw);
				}
			}
		}
		void ParseCrossSection(IReader& reader)
		{
			for (EKeyword kw; reader.NextKeyword(kw);)
			{
				switch (kw)
				{
					case EKeyword::Round:
					{
						// Elliptical cross section, expect 1 or 2 radii to follow
						m_radius.x = reader.Real<float>();
						m_radius.y = reader.IsSectionEnd() ? m_radius.x : reader.Real<float>();
						m_cs_smooth = true;
						m_cs_type = ECSType::Round;
						break;
					}
					case EKeyword::Square:
					{
						// Square cross section, expect 1 or 2 radii to follow
						m_radius.x = reader.Real<float>();
						m_radius.y = reader.IsSectionEnd() ? m_radius.x : reader.Real<float>();
						m_cs_smooth = false;
						m_cs_type = ECSType::Square;
						break;
					}
					case EKeyword::Polygon:
					{
						// Create the cross section, expect X,Y pairs
						for (; !reader.IsSectionEnd();)
							m_cs.push_back(reader.Vector2f());

						m_cs_type = ECSType::Polygon;
						break;
					}
					case EKeyword::Facets:
					{
						m_cs_facets = reader.Int<int>(10);
						break;
					}
					case EKeyword::Smooth:
					{
						m_cs_smooth = true;
						break;
					}
					default:
					{
						m_pp.ReportError(EParseError::InvalidValue, reader.Loc(), std::format("Cross Section keyword {} is not supported", EKeyword_::ToStringA(kw)));
						break;
					}
				}
			}
		}
		void CreateModel(LdrObject* obj, Location const& loc) override
		{
			// If no cross section or extrusion path is given
			if (m_verts.empty())
				return;

			// Create the cross section for implicit profiles
			switch (m_cs_type)
			{
				case ECSType::Round:
				{
					for (auto i = 0; i != m_cs_facets; ++i)
						m_cs.push_back(v2(
							m_radius.x * Cos(float(maths::tau) * i / m_cs_facets),
							m_radius.y * Sin(float(maths::tau) * i / m_cs_facets)));
					break;
				}
				case ECSType::Square:
				{
					// Create the cross section
					m_cs.push_back(v2(-m_radius.x, -m_radius.y));
					m_cs.push_back(v2(+m_radius.x, -m_radius.y));
					m_cs.push_back(v2(+m_radius.x, +m_radius.y));
					m_cs.push_back(v2(-m_radius.x, +m_radius.y));
					break;
				}
				case ECSType::Polygon:
				{
					if (m_cs.empty())
					{
						m_pp.ReportError(EParseError::DataMissing, loc, std::format("Tube object '{}' description incomplete", obj->TypeAndName()));
						return;
					}

					// Ensure a positive area
					if (geometry::PolygonArea(m_cs) < 0)
						std::reverse(std::begin(m_cs), std::end(m_cs));

					break;
				}
				default:
				{
					m_pp.ReportError(EParseError::DataMissing, loc, std::format("Tube object '{}' description incomplete. No style given.", obj->TypeAndName()));
					return;
				}
			}

			// Smooth the tube centre line
			m_smooth.Apply(m_verts);

			// Create the model
			auto opts = ModelGenerator::CreateOptions().colours(m_colours);
			obj->m_model = ModelGenerator::Extrude(m_pp.m_factory, m_cs, m_verts, m_closed, m_cs_smooth, &opts);
			obj->m_model->m_name = obj->TypeAndName();
		}
	};

	// ELdrObject::Mesh
	template <> struct ObjectCreator<ELdrObject::Mesh> :IObjectCreator
	{
		creation::Textured m_tex;
		creation::GenNorms m_gen_norms;

		ObjectCreator(ParseParams& pp)
			: IObjectCreator(pp)
			, m_tex(SamDesc::AnisotropicClamp())
			, m_gen_norms(-1.0f)
		{}
		bool ParseKeyword(IReader& reader, EKeyword kw) override
		{
			switch (kw)
			{
				case EKeyword::Verts:
				{
					for (int r = 1; !reader.IsSectionEnd(); ++r)
					{
						m_verts.push_back(reader.Vector3f().w1());
						if (r % 500 == 0) m_pp.ReportProgress();
					}
					return true;
				}
				case EKeyword::Normals:
				{
					for (int r = 1; !reader.IsSectionEnd(); ++r)
					{
						m_normals.push_back(reader.Vector3f().w0());
						if (r % 500 == 0) m_pp.ReportProgress();
					}
					return true;
				}
				case EKeyword::Colours:
				{
					for (int r = 1; !reader.IsSectionEnd(); ++r)
					{
						m_colours.push_back(Colour(reader.Int<uint32_t>(16)));
						if (r % 500 == 0) m_pp.ReportProgress();
					}
					return true;
				}
				case EKeyword::TexCoords:
				{
					for (int r = 1; !reader.IsSectionEnd(); ++r)
					{
						m_texs.push_back(reader.Vector2f());
						if (r % 500 == 0) m_pp.ReportProgress();
					}
					return true;
				}
				case EKeyword::Lines:
				case EKeyword::LineList:
				case EKeyword::LineStrip:
				{
					auto is_strip = kw == EKeyword::LineStrip;
					auto nug = NuggetDesc(is_strip ? ETopo::LineStrip : ETopo::LineList, EGeom::Vert |
						(!m_colours.empty() ? EGeom::Colr : EGeom::None))
						.vrange(Range::Reset())
						.irange(Range(m_indices.size(), m_indices.size()))
						.tex_diffuse(m_tex.m_texture)
						.sam_diffuse(m_tex.m_sampler);

					for (int r = 1; !reader.IsSectionEnd(); ++r)
					{
						auto idx = reader.Int<uint16_t>(10);
						m_indices.push_back(idx);
						nug.m_vrange.grow(idx);
						++nug.m_irange.m_end;

						if (r % 500 == 0) m_pp.ReportProgress();
					}

					m_nuggets.push_back(nug);
					return true;
				}
				case EKeyword::Faces:
				case EKeyword::TriList:
				case EKeyword::TriStrip:
				{
					auto is_strip = kw == EKeyword::TriStrip;
					auto nug = NuggetDesc(is_strip ? ETopo::TriStrip : ETopo::TriList, EGeom::Vert |
						(!m_normals.empty() ? EGeom::Norm : EGeom::None) |
						(!m_colours.empty() ? EGeom::Colr : EGeom::None) |
						(!m_texs.empty() ? EGeom::Tex0 : EGeom::None))
						.vrange(Range::Reset())
						.irange(Range(m_indices.size(), m_indices.size()))
						.tex_diffuse(m_tex.m_texture)
						.sam_diffuse(m_tex.m_sampler);

					for (int r = 1; !reader.IsSectionEnd(); ++r)
					{
						auto idx = reader.Int<uint16_t>(10);
						m_indices.push_back(idx);
						nug.m_vrange.grow(idx);
						++nug.m_irange.m_end;

						if (r % 500 == 0) m_pp.ReportProgress();
					}

					m_nuggets.push_back(nug);
					return true;
				}
				case EKeyword::Tetra:
				{
					auto nug = NuggetDesc(ETopo::TriList, EGeom::Vert |
						(!m_normals.empty() ? EGeom::Norm : EGeom::None) |
						(!m_colours.empty() ? EGeom::Colr : EGeom::None) |
						(!m_texs.empty() ? EGeom::Tex0 : EGeom::None))
						.vrange(Range::Reset())
						.irange(Range(m_indices.size(), m_indices.size()))
						.tex_diffuse(m_tex.m_texture)
						.sam_diffuse(m_tex.m_sampler);

					for (int r = 1;!reader.IsSectionEnd(); ++r)
					{
						uint16_t const idx[] = {
							reader.Int<uint16_t>(10),
							reader.Int<uint16_t>(10),
							reader.Int<uint16_t>(10),
							reader.Int<uint16_t>(10),
						};
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

						if (r % 500 == 0) m_pp.ReportProgress();
					}

					m_nuggets.push_back(nug);
					return true;
				}
				default:
				{
					return
						m_tex.ParseKeyword(reader, m_pp, kw) ||
						m_gen_norms.ParseKeyword(reader, m_pp, kw) ||
						IObjectCreator::ParseKeyword(reader, kw);
				}
			}
		}
		void CreateModel(LdrObject* obj, Location const& loc) override
		{
			// Validate
			if (m_indices.empty() || m_verts.empty())
			{
				m_pp.ReportError(EParseError::DataMissing, loc, "Mesh object description incomplete");
				return;
			}
			if (!m_colours.empty() && m_colours.size() != m_verts.size())
			{
				m_pp.ReportError(EParseError::DataMissing, loc, std::format("Mesh objects with colours require one colour per vertex. {} required, {} given.", isize(m_verts), isize(m_colours)));
				return;
			}
			if (!m_normals.empty() && m_normals.size() != m_verts.size())
			{
				m_pp.ReportError(EParseError::DataMissing, loc, std::format("Mesh objects with normals require one normal per vertex. {} required, {} given.", isize(m_verts), isize(m_normals)));
				return;
			}
			if (!m_texs.empty() && m_texs.size() != m_verts.size())
			{
				m_pp.ReportError(EParseError::DataMissing, loc, std::format("Mesh objects with texture coordinates require one coordinate per vertex. {} required, {} given.", isize(m_verts), isize(m_normals)));
				return;
			}
			for (auto& nug : m_nuggets)
			{
				// Check the index range is valid
				if (nug.m_vrange.m_beg < 0 || nug.m_vrange.m_end > isize(m_verts))
				{
					m_pp.ReportError(EParseError::InvalidValue, loc, std::format("Mesh object with face, line, or tetra section contains indices out of range (section index: {}).", int(&nug - &m_nuggets[0])));
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
			m_gen_norms.Generate(m_pp);

			// Create the model
			auto cdata = MeshCreationData()
				.verts(m_verts)
				.indices(m_indices)
				.nuggets(m_nuggets)
				.colours(m_colours)
				.normals(m_normals)
				.tex(m_texs);
			obj->m_model = ModelGenerator::Mesh(m_pp.m_factory, cdata);
			obj->m_model->m_name = obj->TypeAndName();
		}
	};

	// ELdrObject::ConvexHull
	template <> struct ObjectCreator<ELdrObject::ConvexHull> :IObjectCreator
	{
		creation::Textured m_tex;
		creation::GenNorms m_gen_norms;

		ObjectCreator(ParseParams& pp)
			: IObjectCreator(pp)
			, m_tex(SamDesc::AnisotropicClamp())
			, m_gen_norms(0.0f)
		{}
		bool ParseKeyword(IReader& reader, EKeyword kw) override
		{
			switch (kw)
			{
				case EKeyword::Verts:
				{
					for (int r = 1; !reader.IsSectionEnd(); ++r)
					{
						m_verts.push_back(reader.Vector3f().w1());
						if (r % 500 == 0) m_pp.ReportProgress();
					}
					return true;
				}
				default:
				{
					return
						m_tex.ParseKeyword(reader, m_pp, kw) ||
						m_gen_norms.ParseKeyword(reader, m_pp, kw) ||
						IObjectCreator::ParseKeyword(reader, kw);
				}
			}
		}
		void CreateModel(LdrObject* obj, Location const&) override
		{
			// Validate
			if (m_verts.size() < 2)
				return;

			// Allocate space for the face indices
			m_indices.resize(6 * (m_verts.size() - 2));

			// Find the convex hull
			size_t num_verts = 0, num_faces = 0;
			pr::ConvexHull(m_verts, m_verts.size(), &m_indices[0], &m_indices[0] + m_indices.size(), num_verts, num_faces);
			m_verts.resize(num_verts);
			m_indices.resize(3 * num_faces);

			// Create a nugget for the hull
			m_nuggets.push_back(NuggetDesc(ETopo::TriList, EGeom::Vert).tex_diffuse(m_tex.m_texture).sam_diffuse(m_tex.m_sampler));

			// Generate normals if needed
			m_gen_norms.Generate(m_pp);

			// Create the model
			auto cdata = MeshCreationData()
				.verts(m_verts)
				.indices(m_indices)
				.nuggets(m_nuggets)
				.colours(m_colours)
				.normals(m_normals)
				.tex(m_texs);
			obj->m_model = ModelGenerator::Mesh(m_pp.m_factory, cdata);
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
		iv2   m_dim;         // Table dimensions (columns, rows) or bounds of the table dimensions.

		ObjectCreator(ParseParams& pp)
			: IObjectCreator(pp)
			, m_data()
			, m_index()
			, m_dim()
		{}
		bool ParseKeyword(IReader& reader, EKeyword kw) override
		{
			switch (kw)
			{
				case EKeyword::Data:
				{
					// Expect the first value to be the number of columns
					auto column_count = reader.Int<int>(10);

					// Read data till the end of the section
					for (; !reader.IsSectionEnd();)
					{
						auto value = reader.Real<double>();
						m_data.push_back(value);
					}

					// Expect a rectangular block of data
					m_dim = { column_count, (isize(m_data) + column_count - 1) / column_count };
					m_index.resize(0);
					return true;
				}
				case EKeyword::Source:
				{
					// Source is a file containing data
					auto filepath = reader.String<std::filesystem::path>();
					std::ifstream file(filepath);
					ParseDataStream(file);
					return true;
				}
				default:
				{
					return IObjectCreator::ParseKeyword(reader, kw);
				}
			}
		}
		template <typename Stream> void ParseDataStream(Stream& stream)
		{
			using namespace pr::csv;
			using namespace pr::str;

			m_data.clear();
			m_index.clear();
			m_dim = iv2::Zero();

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
		void CreateModel(LdrObject*, Location const&) override
		{
			// The chart does not contain a model. Instead, nested 'Series'
			// objects form the models, based on the data in this object.
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
		
		ObjectCreator(ParseParams& pp)
			: IObjectCreator(pp)
			, m_chart()
			, m_xaxis()
			, m_yaxis()
			, m_xiter()
			, m_yiter()
			, m_line_width()
		{}
		LdrObjectPtr Parse(IReader& reader) override
		{
			// Find the ancestor chart creator
			for (auto const* parent = m_pp.m_parent_creator; parent; parent = parent->m_pp.m_parent_creator)
			{
				if (parent->m_pp.m_type != ELdrObject::Chart) continue;
				m_chart = static_cast<ObjectCreator<ELdrObject::Chart> const*>(parent);
				break;
			}
			if (!m_chart)
			{
				m_pp.ReportError(EParseError::NotFound, {}, "Series objects must be children of a Chart object");
				return nullptr; // Not possible to carry on without a chart
			}
			return IObjectCreator::Parse(reader);
		}
		bool ParseKeyword(IReader& reader, EKeyword kw) override
		{
			switch (kw)
			{
				case EKeyword::XAxis:
				{
					m_xaxis = eval::Compile(reader.String<string32>());
					for (auto& name : m_xaxis.m_arg_names)
						m_xiter.push_back(DataIter{name, m_chart->m_dim});
					
					return true;
				}
				case EKeyword::YAxis:
				{
					m_yaxis = eval::Compile(reader.String<string32>());
					for (auto& name : m_yaxis.m_arg_names)
						m_yiter.push_back(DataIter{name, m_chart->m_dim});

					return true;
				}
				case EKeyword::Width:
				{
					m_line_width = reader.Real<float>();
					return true;
				}
				default:
				{
					return IObjectCreator::ParseKeyword(reader, kw);
				}
			}
		}
		void CreateModel(LdrObject* obj, Location const&) override
		{
			// Determine the index of this series within the chart
			int child_index = 0;
			for (auto& child : m_pp.m_parent->m_child)
				child_index += int(child->m_type == ELdrObject::Series);

			// Generate a name if none given
			if (!AllSet(m_pp.m_flags, EFlags::ExplicitName))
				obj->m_name = std::format("Series {}", child_index);

			// Assign a colour if none given
			if (!AllSet(m_pp.m_flags, EFlags::ExplicitColour))
				obj->m_base_colour = colours[child_index % _countof(colours)];

			auto& verts = m_pp.m_cache.m_point;
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
			if (verts.empty())
				return;

			auto opts = ModelGenerator::CreateOptions().colours({&obj->m_base_colour, 1});
			obj->m_model = ModelGenerator::LineStrip(m_pp.m_factory, isize(verts) - 1, verts, &opts);
			obj->m_model->m_name = obj->TypeAndName();

			// Use thick lines
			if (m_line_width > 1.0f)
			{
				// Get or create an instance of the thick line shader
				auto shdr = Shader::Create<shaders::ThickLineListGS>(m_line_width);
				for (auto& nug : obj->m_model->m_nuggets)
					nug.m_shaders.push_back({ shdr, ERenderStep::RenderForward });
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
	};

	// ELdrObject::Model
	template <> struct ObjectCreator<ELdrObject::Model> :IObjectCreator
	{
		std::filesystem::path m_filepath;
		std::unique_ptr<std::istream> m_file_stream;
		creation::GenNorms m_gen_norms;
		creation::BakeTransform m_bake;

		ObjectCreator(ParseParams& pp)
			: IObjectCreator(pp)
			, m_filepath()
			, m_file_stream()
			, m_gen_norms()
			, m_bake()
		{}
		bool ParseKeyword(IReader& reader, EKeyword kw) override
		{
			switch (kw)
			{
				case EKeyword::FilePath:
				{
					// Ask the include handler to turn the filepath into a stream.
					// Load the stream in binary mode. The model loading functions can convert binary to text if needed.
					m_filepath = reader.String<std::filesystem::path>();
					m_file_stream = reader.PathResolver.OpenStream(m_filepath, IPathResolver::EFlags::Binary);
					return true;
				}
				default:
				{
					return
						m_bake.ParseKeyword(reader, m_pp, kw) ||
						m_gen_norms.ParseKeyword(reader, m_pp, kw) ||
						IObjectCreator::ParseKeyword(reader, kw);
				}
			}
		}
		void CreateModel(LdrObject* obj, Location const& loc) override
		{
			using namespace pr::geometry;
			using namespace std::filesystem;

			// Validate
			if (m_filepath.empty())
			{
				m_pp.ReportError(EParseError::DataMissing, loc, "Model filepath not given");
				return;
			}
			if (!m_file_stream)
			{
				m_pp.ReportError(EParseError::NotFound, loc, "Failed to open the model file");
				return;
			}

			// Determine the format from the file extension
			auto format = GetModelFormat(m_filepath);
			if (format == EModelFileFormat::Unknown)
			{
				auto msg = std::format("Model file '{}' is not supported.\nSupported Formats: ", m_filepath.string());
				for (auto f : Enum<EModelFileFormat>::Members()) msg.append(Enum<EModelFileFormat>::ToStringA(f)).append(" ");
				m_pp.ReportError(EParseError::InvalidValue, loc, msg);
				return;
			}

			// Attach a texture filepath resolver
			auto search_paths = std::vector<path> {
				path(m_filepath.string() + ".textures"),
				m_filepath.parent_path(),
			};
			AutoSub sub = m_pp.m_rdr.ResolveFilepath += [&](auto&, ResolvePathArgs& args)
			{
				// Look in a folder with the same name as the model
				auto resolved = pr::filesys::ResolvePath<std::vector<path>>(args.filepath, search_paths, nullptr, false, nullptr);
				if (!exists(resolved)) return;
				args.filepath = resolved;
				args.handled = true;
			};

			// Create the models
			auto opts = ModelGenerator::CreateOptions().colours(m_colours).bake(m_bake.O2WPtr());
			ModelGenerator::LoadModel(format, m_pp.m_factory, *m_file_stream, [&](ModelTree const& tree)
				{
					auto child = ModelTreeToLdr(tree, obj->m_context_id);
					if (child != nullptr) obj->AddChild(child);
					return false;
				}, &opts);
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
				LdrObjectPtr obj(new LdrObject(ELdrObject::Model, nullptr, context_id), true);
				obj->m_name = node.m_model->m_name;
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
		using VCPair = struct { float m_value; Colour32 m_colour; };
		using ColourBands = pr::vector<VCPair>;

		// An axis for the space that the equation is plotted in
		struct Axis
		{
			float m_min;
			float m_max;
			ColourBands m_col;

			Axis()
				: m_min(maths::float_max)
				, m_max(maths::float_lowest)
				, m_col()
			{
			}
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
				VCPair vc = { value, Colour32White };

				// Clamp the range
				if (m_min <= m_max)
				{
					vc.m_value = Clamp(value, m_min, m_max);
					if (vc.m_value != value)
						vc.m_colour.a = 0;
				}

				// Interpolate the colour
				if (!m_col.empty() && vc.m_value == value)
				{
					int i = 0, iend = static_cast<int>(m_col.size());
					for (; i != iend && vc.m_value >= m_col[i].m_value; ++i) {}
					if (i == 0)
						vc.m_colour = m_col.front().m_colour;
					else if (i == iend)
						vc.m_colour = m_col.back().m_colour;
					else
					{
						auto f = Frac(m_col[i - 1].m_value, vc.m_value, m_col[i].m_value);
						vc.m_colour = Lerp(m_col[i - 1].m_colour, m_col[i].m_colour, f);
					}
				}

				return vc;
			}
			static Axis Parse(IReader& reader, ParseParams& pp)
			{
				Axis axis;
				for (EKeyword kw; !reader.NextKeyword(kw); )
				{
					switch (kw)
					{
						case EKeyword::Range:
						{
							axis.m_min = reader.Real<float>();
							axis.m_max = reader.Real<float>();
							break;
						}
						case EKeyword::Colours:
						{
							for (; !reader.IsSectionEnd(); )
							{
								VCPair vcpair = {
									.m_value = reader.Real<float>(),
									.m_colour = reader.Int<uint32_t>(16),
								};
								axis.m_col.push_back(vcpair);
							}
							sort(axis.m_col, [](auto& l, auto& r) { return l.m_value < r.m_value; });
							break;
						}
						default:
						{
							pp.ReportError(EParseError::UnknownKeyword, reader.Loc(), std::format("Keyword '{}' is not valid within *Axis", EKeyword_::ToStringA(kw)));
							break;
						}
					}
				}
				return axis;
			}
		};

		// Data stored in the object's user data to help with plotting
		struct Extras
		{
			std::array<Axis, 3> m_axis;
			float m_weight;
			Extras()
				: m_axis()
				, m_weight(0.5f)
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

		// Used to prevent unnecessary recreations of the equation model
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

		ObjectCreator(ParseParams& pp)
			: IObjectCreator(pp)
			, m_eq()
			, m_args()
			, m_resolution(10000)
			, m_extras()
		{}
		bool ParseKeyword(IReader& reader, EKeyword kw) override
		{
			switch (kw)
			{
				case EKeyword::Data:
				{
					// Compile the equation
					try
					{
						auto equation = reader.String<string32>();
						m_eq = eval::Compile(equation);
					}
					catch (std::exception const& ex)
					{
						m_pp.ReportError(EParseError::InvalidValue, reader.Loc(), std::format("Equation expression is invalid: {}", ex.what()));
					}
					return true;
				}
				case EKeyword::Resolution:
				{
					m_resolution = reader.Int<int>(10);
					m_resolution = std::clamp(m_resolution, 8, 0xFFFF);
					return true;
				}
				case EKeyword::Param:
				{
					auto variable = reader.String<string32>();
					auto value = reader.Real<double>();
					m_args.add(variable, value);
					return true;
				}
				case EKeyword::Weight:
				{
					m_extras.m_weight = reader.Real<float>();
					m_extras.m_weight = std::clamp(m_extras.m_weight, -1.0f, +1.0f);
					return true;
				}
				case EKeyword::XAxis:
				{
					auto section = reader.SectionScope();
					m_extras.m_axis[0] = Axis::Parse(reader, m_pp);
					return true;
				}
				case EKeyword::YAxis:
				{
					auto section = reader.SectionScope();
					m_extras.m_axis[1] = Axis::Parse(reader, m_pp);
					return true;
				}
				case EKeyword::ZAxis:
				{
					auto section = reader.SectionScope();
					m_extras.m_axis[2] = Axis::Parse(reader, m_pp);
					return true;
				}
				default:
				{
					return IObjectCreator::ParseKeyword(reader, kw);
				}
			}
		}
		void CreateModel(LdrObject* obj, Location const& loc) override
		{
			using namespace pr::geometry;

			// Validate
			if (!m_eq)
			{
				m_pp.ReportError(EParseError::DataMissing, loc, "Equation not given");
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
				default: throw std::runtime_error(std::format("Unsupported equation dimension: {}", dim));
			}

			// Store the expression in the object user data
			obj->m_user_data.get<eval::Expression>() = m_eq;
			obj->m_user_data.get<Extras>() = m_extras;
			obj->m_user_data.get<Cache>() = Cache{};

			// Create buffers for a dynamic model
			ModelDesc mdesc(
				ResDesc::VBuf<Vert>(vcount, {}),
				ResDesc::IBuf<uint32_t>(icount, {}),
				BBox::Reset(), obj->TypeAndName().c_str());

			// Create the model
			obj->m_model = m_pp.m_factory.CreateModel(mdesc);
			obj->m_model->m_name = obj->TypeAndName();
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
			auto area = cam.ViewRectAtDistance(cam.FocusDist());

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
				NuggetDesc n = {};
				n.m_topo = ETopo::LineStrip;
				n.m_geom = EGeom::Vert;
				n.m_vrange = Range(0, count);
				n.m_irange = Range(0, count);
				n.m_nflags = extras.has_alpha() ? ENuggetFlag::GeometryHasAlpha : ENuggetFlag::None;
				model.DeleteNuggets();

				ResourceFactory factory(model.rdr());
				model.CreateNugget(factory, n);
			}
		}
		static void SurfacePlot(Model& model, BBox_cref range, eval::Expression const& equation, Extras const& extras, bool init)
		{
			// Notes:
			//  - 'range' is the independent variable range. For surface plots, 'x' and 'y' are used.
			//  - 'extras.m_axis' contains the bounds on output values and colour gradients.

			// Determine the largest hex patch that can be made with the available model size:
			//  i.e. solve for the minimum value for 'rings' in:
			//'    vcount = ArithmeticSum(0, 6, rings) + 1;
			//'    icount = ArithmeticSum(0, 12, rings) + 2*rings;
			// ArithmeticSum := (n + 1) * (a0 + an) / 2, where an = (a0 + n * step)
			//    3r^2 + 3r + 1-vcount = 0  =>  r = (-3 +/- sqrt(-3 + 12*vcount)) / 6
			//    6r^2 + 8r - icount = 0    =>  r = (-8 +/- sqrt(64 + 24*icount)) / 12
			auto vrings = (-3 + sqrt(-3 + 12 * model.m_vcount)) / 6;
			auto irings = (-8 + sqrt(64 + 24 * model.m_icount)) / 12;
			auto rings = static_cast<int>(std::min(vrings, irings));
			auto dx_step = range.SizeX() * 1e-5f;
			auto dy_step = range.SizeY() * 1e-5f;

			auto [nv, ni] = geometry::HexPatchSize(rings);
			assert(nv <= (int)model.m_vcount);
			assert(ni <= (int)model.m_icount);

			ResourceFactory factory(model.rdr());
			auto update_v = model.UpdateVertices(factory);
			auto update_i = model.UpdateIndices(factory);
			auto vout = update_v.ptr<Vert>();
			auto iout = update_i.ptr<uint32_t>();

			auto props = geometry::HexPatch(rings,
				[&](v4_cref pos, Colour32, v4_cref, v2_cref)
				{
					// Evaluate the function at points around the focus point, but shift them back
					// so the focus point is centred around (0,0,0), then set the o2w transform

					// 'pos' is a point in the range [-1.0,+1.0]. Rescale to the range.
					// 'weight' controls the density of points near the range centre since 'len_sq' is on [0,1].
					auto dir = pos.w0();
					auto len_sq = LengthSq(dir);
					auto weight = Lerp(extras.m_weight, 1.0f, len_sq);
					auto pt = range.Centre() + dir * range.Radius() * weight;
					
					// Evaluate the equation at 'pt' to get z = f(x,y) and the colour.
					auto [z,col] = extras.m_axis[2].clamp(static_cast<float>(equation(pt.x, pt.y).db()));

					// Evaluate the normal at 'pt'. Want to choose a 'd' value that is proportional to the density of points at 'pt'
					auto dx = dx_step * weight; // this isn't right, 'd' should be the smallest step that produces an accurate normal...
					auto dy = dy_step * weight;

					// Evaluate the function at four points around (x,y) to get the height 'h'
					auto h = equation(v4(pt.x - dx, pt.x + dx, pt.x, pt.x), v4(pt.y, pt.y, pt.y - dy, pt.y + dy)).v4();
					auto n = Cross(v4(2 * dx, 0, h.y - h.x, 0), v4(0, 2 * dy, h.w - h.z, 0));
					auto norm = Normalise(n, v4::Zero());

					SetPCNT(*vout++, v4(pt.x, pt.y, z, 1), Colour(col), norm, v2::Zero());
				},
				[&](auto idx)
				{
					*iout++ = idx;
				});

			assert(vout - update_v.ptr<Vert>() == nv);
			assert(iout - update_i.ptr<uint32_t>() == ni);
			update_v.Commit(D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER);
			update_i.Commit(D3D12_RESOURCE_STATE_INDEX_BUFFER);

			// Generate nuggets if initialising
			if (init)
			{
				model.CreateNugget(factory, NuggetDesc(ETopo::TriStrip, props.m_geom).vrange(Range(0, nv)).irange(Range(0, ni)).alpha_geom(extras.has_alpha()));
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

	// ELdrObject::Group
	template <> struct ObjectCreator<ELdrObject::Group> :IObjectCreator
	{
		ObjectCreator(ParseParams& pp)
			: IObjectCreator(pp)
		{}
	};

	// ELdrObject::LightSource
	template <> struct ObjectCreator<ELdrObject::LightSource> :IObjectCreator
	{
		Light m_light;

		ObjectCreator(ParseParams& pp)
			: IObjectCreator(pp)
			, m_light()
		{
			m_light.m_on = true;
		}
		bool ParseKeyword(IReader& reader, EKeyword kw) override
		{
			switch (kw)
			{
				case EKeyword::Style:
				{
					auto ident = reader.Identifier<string32>();
					switch (HashI(ident.c_str()))
					{
						case HashI("Directional"): m_light.m_type = ELight::Directional; break;
						case HashI("Point"): m_light.m_type = ELight::Point; break;
						case HashI("Spot"): m_light.m_type = ELight::Spot; break;
						default: m_pp.ReportError(EParseError::InvalidValue, reader.Loc(), std::format("{} is an unknown light type", ident));
					}
					return true;
				}
				case EKeyword::Ambient:
				{
					m_light.m_ambient = reader.Int<uint32_t>(16);
					return true;				
				}
				case EKeyword::Diffuse:
				{
					m_light.m_diffuse = reader.Int<uint32_t>(16);
					return true;
				}
				case EKeyword::Specular:
				{
					m_light.m_specular = reader.Int<uint32_t>(16);
					m_light.m_specular_power = reader.Real<float>();
					return true;
				}
				case EKeyword::Range:
				{
					m_light.m_range = reader.Real<float>();
					m_light.m_falloff = reader.Real<float>();
					return true;
				}
				case EKeyword::Cone:
				{
					m_light.m_inner_angle = reader.Real<float>(); // in degrees
					m_light.m_outer_angle = reader.Real<float>(); // in degrees
					return true;
				}
				case EKeyword::CastShadow:
				{
					m_light.m_cast_shadow = reader.Real<float>();
					return true;
				}
				default:
				{
					return IObjectCreator::ParseKeyword(reader, kw);
				}
			}
		}
		void CreateModel(LdrObject* obj, Location const&) override
		{
			// Assign the light data as user data
			obj->m_user_data.get<Light>() = m_light;
		}
	};

	// ELdrObject::Text
	template <> struct ObjectCreator<ELdrObject::Text> :IObjectCreator
	{
		enum class EType
		{
			Full3D,
			Billboard3D,
			Billboard,
			ScreenSpace,
		};
		using TextFmtCont = pr::vector<TextFormat>;

		wstring32 m_text;
		EType m_type;
		TextFmtCont m_fmt;
		TextLayout m_layout;
		creation::MainAxis m_axis;

		ObjectCreator(ParseParams& pp)
			: IObjectCreator(pp)
			, m_text()
			, m_type(EType::Full3D)
			, m_fmt()
			, m_layout()
			, m_axis(AxisId::PosZ)
		{}
		bool ParseKeyword(IReader& reader, EKeyword kw) override
		{
			switch (kw)
			{
				case EKeyword::Data:
				{
					auto text = reader.String<string32>();
					m_text.append(Widen(text));

					// Record the formatting state
					m_fmt.push_back(TextFormat(int(m_text.size() - text.size()), isize(text), m_pp.m_font.back()));
					return true;
				}
				case EKeyword::CString:
				{
					auto text = reader.CString();
					m_text.append(Widen(text));

					// Record the formatting state
					m_fmt.push_back(TextFormat(int(m_text.size() - text.size()), isize(text), m_pp.m_font.back()));
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
				case EKeyword::Billboard3D:
				{
					m_type = EType::Billboard3D;
					return true;
				}
				case EKeyword::BackColour:
				{
					m_layout.m_bk_colour = reader.Int<uint32_t>(16);
					return true;
				}
				case EKeyword::Format:
				{
					auto ident = reader.Identifier<string32>();
					switch (HashI(ident.c_str()))
					{
						case HashI("Left"): m_layout.m_align_h = DWRITE_TEXT_ALIGNMENT_LEADING; break;
						case HashI("CentreH"): m_layout.m_align_h = DWRITE_TEXT_ALIGNMENT_CENTER; break;
						case HashI("Right"): m_layout.m_align_h = DWRITE_TEXT_ALIGNMENT_TRAILING; break;
						default: m_pp.ReportError(EParseError::UnknownKeyword, reader.Loc(), std::format("{} is not a valid horizontal alignment value", ident));
					}
					ident = reader.Identifier<string32>();
					switch (HashI(ident.c_str()))
					{
						case HashI("Top"): m_layout.m_align_v = DWRITE_PARAGRAPH_ALIGNMENT_NEAR; break;
						case HashI("CentreV"): m_layout.m_align_v = DWRITE_PARAGRAPH_ALIGNMENT_CENTER; break;
						case HashI("bottom"): m_layout.m_align_v = DWRITE_PARAGRAPH_ALIGNMENT_FAR; break;
						default: m_pp.ReportError(EParseError::UnknownKeyword, reader.Loc(), std::format("{} is not a valid vertical alignment value", ident));
					}
					ident = reader.Identifier<string32>();
					switch (HashI(ident.c_str()))
					{
						case HashI("Wrap"): m_layout.m_word_wrapping = DWRITE_WORD_WRAPPING_WRAP; break;
						case HashI("NoWrap"): m_layout.m_word_wrapping = DWRITE_WORD_WRAPPING_NO_WRAP; break;
						case HashI("WholeWord"): m_layout.m_word_wrapping = DWRITE_WORD_WRAPPING_WHOLE_WORD; break;
						case HashI("Character"): m_layout.m_word_wrapping = DWRITE_WORD_WRAPPING_CHARACTER; break;
						case HashI("EmergencyBreak"): m_layout.m_word_wrapping = DWRITE_WORD_WRAPPING_EMERGENCY_BREAK; break;
						default: m_pp.ReportError(EParseError::UnknownKeyword, reader.Loc(), std::format("{} is not a valid word wrapping value", ident));
					}
					return true;
				}
				case EKeyword::Anchor:
				{
					m_layout.m_anchor = reader.Vector2f();
					return true;
				}
				case EKeyword::Padding:
				{
					auto padding = reader.Vector4f();
					m_layout.m_padding.left = padding.x;
					m_layout.m_padding.top = padding.y;
					m_layout.m_padding.right = padding.z;
					m_layout.m_padding.bottom = padding.w;
					return true;
				}
				case EKeyword::Dim:
				{
					m_layout.m_dim = reader.Vector2f();
					return true;
				}
				default:
				{
					return
						m_axis.ParseKeyword(reader, m_pp, kw) ||
						IObjectCreator::ParseKeyword(reader, kw);
				}
			}
		}
		void CreateModel(LdrObject* obj, Location const&) override
		{
			// Create a quad containing the text
			obj->m_model = ModelGenerator::Text(m_pp.m_factory, m_text, m_fmt, m_layout, 1.0, m_axis.m_align);
			obj->m_model->m_name = obj->TypeAndName();

			// Create the model
			switch (m_type)
			{
				// Text is a normal 3D object
				case EType::Full3D:
				{
					break;
				}
				// Position the text quad so that it always faces the camera but scales with distance
				case EType::Billboard3D:
				{
					// Do not include in scene bounds calculations because we're scaling
					// this model at a point that the bounding box calculation can't see.
					obj->Flags(ELdrFlags::SceneBoundsExclude, true, "");

					// Update the rendering 'i2w' transform on add-to-scene
					obj->OnAddToScene += [](LdrObject& ob, Scene const& scene)
					{
						// The size of the text texture is the text metrics size / 96.0.
						auto c2w = scene.m_cam.CameraToWorld();
						auto w2c = scene.m_cam.WorldToCamera();
						auto w = 1.0f * scene.m_viewport.ScreenW;
						auto h = 1.0f * scene.m_viewport.ScreenH;
						#if PR_DBG
						if (w < 1.0f || h < 1.0f)
							throw std::runtime_error("Invalid viewport size");
						#endif

						// Create a camera with an aspect ratio that matches the viewport
						// This handles the case where main camera X/Y are not using the same resolution.
						auto& main_camera = static_cast<Camera const&>(scene.m_cam);
						auto  text_camera = main_camera; text_camera.Aspect(w / h);
						auto fd = main_camera.FocusDist();

						// Get the scaling factors from 'main_camera' to 'text_camera'
						auto viewarea_camera = main_camera.ViewRectAtDistance(fd);
						auto viewarea_txtcam = text_camera.ViewRectAtDistance(fd);

						// Scale the X,Y coordinates in camera space
						auto pt_cs = w2c * ob.m_i2w.pos;
						pt_cs.x *= viewarea_txtcam.x / viewarea_camera.x;
						pt_cs.y *= viewarea_txtcam.y / viewarea_camera.y;
						auto pt_ws = c2w * pt_cs;

						// Position facing the camera
						ob.m_i2w = m4x4(c2w.rot, pt_ws) * ob.m_i2w.scale();
						ob.m_c2s = text_camera.CameraToScreen();
					};
					break;
				}
				// Position the text quad so that it always faces the camera and has the same size
				case EType::Billboard:
				{
					// Do not include in scene bounds calculations because we're scaling
					// this model at a point that the bounding box calculation can't see.
					obj->Flags(
						ELdrFlags::BBoxExclude |
						ELdrFlags::SceneBoundsExclude |
						ELdrFlags::HitTestExclude |
						ELdrFlags::ShadowCastExclude, true, "");

					// Scale up the view port to reduce floating point precision noise.
					constexpr int ViewPortSize = 1024;

					// Screen space uses a standard normalised orthographic projection
					obj->m_c2s = m4x4::ProjectionOrthographic(float(ViewPortSize), float(ViewPortSize), -0.01f, 1, true);

					// Update the rendering 'i2w' transform on add-to-scene
					obj->OnAddToScene += [](LdrObject& ob, Scene const& scene)
					{
						auto& main_camera = static_cast<Camera const&>(scene.m_cam);
						auto c2w = main_camera.CameraToWorld();
						auto w2c = main_camera.WorldToCamera();
						auto w = 1.0f * scene.m_viewport.ScreenW;
						auto h = 1.0f * scene.m_viewport.ScreenH;
						#if PR_DBG
						if (w < 1.0f || h < 1.0f)
							throw std::runtime_error("Invalid viewport size");
						#endif

						// Convert the world space position into a screen space position
						auto pt_ss = w2c * ob.m_i2w.pos;
						auto viewarea = main_camera.ViewRectAtDistance(abs(pt_ss.z));
						pt_ss.x *= ViewPortSize / viewarea.x;
						pt_ss.y *= ViewPortSize / viewarea.y;
						pt_ss.z = static_cast<float>(main_camera.NormalisedDistance(pt_ss.z));

						// The text quad has a scale of 100pt == 1m. For screen space make this 100pt * 96/72 == 133px
						constexpr float m_to_px = 133.0f;

						// Scale the object from physical pixels to normalised screen space
						auto scale = m4x4::Scale(m_to_px * ViewPortSize / w, m_to_px * ViewPortSize / h, 1, v4::Origin());

						// Construct the 'i2w' using the screen space position
						ob.m_i2w = c2w * m4x4::Translation(pt_ss.x, pt_ss.y, pt_ss.z) * scale * ob.m_i2w.scale();
					};
					break;
				}
				// Position the text quad in screen space.
				case EType::ScreenSpace:
				{
					// Do not include in scene bounds calculations because we're scaling
					// this model at a point that the bounding box calculation can't see.
					obj->Flags(
						ELdrFlags::BBoxExclude |
						ELdrFlags::SceneBoundsExclude |
						ELdrFlags::HitTestExclude |
						ELdrFlags::ShadowCastExclude, true, "");

					// Scale up the view port to reduce floating point precision noise.
					constexpr int ViewPortSize = 1024;

					// Screen space uses a standard normalised orthographic projection
					obj->m_c2s = m4x4::ProjectionOrthographic(float(ViewPortSize), float(ViewPortSize), -0.01f, 1, true);

					// Update the rendering 'i2w' transform on add-to-scene.
					obj->OnAddToScene += [](LdrObject& ob, Scene const& scene)
					{
						// The 'ob.m_i2w' is a normalised screen space position
						// (-1,-1,-0) is the lower left corner on the near plane,
						// (+1,+1,-1) is the upper right corner on the far plane.
						auto& main_camera = static_cast<Camera const&>(scene.m_cam);
						auto c2w = main_camera.CameraToWorld();
						auto w = 1.0f * scene.m_viewport.ScreenW;
						auto h = 1.0f * scene.m_viewport.ScreenH;
						#if PR_DBG
						if (w < 1.0f || h < 1.0f)
							throw std::runtime_error("Invalid viewport size");
						#endif

						// Convert the position given in the ldr script as 2D screen space
						// Note: pt_ss.z should already be the normalised distance from the camera
						auto pt_ss = ob.m_i2w.pos;
						pt_ss.x *= 0.5f * ViewPortSize;
						pt_ss.y *= 0.5f * ViewPortSize;

						// The text quad has a scale of 100pt == 1m. For screen space make this 100pt * 96/72 == 133px
						constexpr float m_to_px = 133.0f;

						// Scale the object from physical pixels to normalised screen space
						auto scale = m4x4::Scale(m_to_px * ViewPortSize / w, m_to_px * ViewPortSize / h, 1, v4::Origin());

						// Convert 'i2w', which is 'i2c' in the ldr script, into an actual 'i2w'
						ob.m_i2w = c2w * m4x4::Translation(pt_ss.x, pt_ss.y, pt_ss.z) * scale * ob.m_i2w.scale();
					};
					break;
				}
				default:
				{
					throw std::runtime_error(std::format("Unknown Text mode: {}", int(m_type)));
				}
			}
		}
	};

	// ELdrObject::Instance
	template <> struct ObjectCreator<ELdrObject::Instance> :IObjectCreator
	{
		ObjectCreator(ParseParams& pp)
			: IObjectCreator(pp)
		{}
		void CreateModel(LdrObject* obj, Location const& loc) override
		{
			// Locate the model that this is an instance of
			auto model_key = hash::Hash(obj->m_name.c_str());
			auto mdl = m_pp.m_models.find(model_key);
			if (mdl == m_pp.m_models.end())
			{
				m_pp.ReportError(EParseError::NotFound, loc, "Model not found. Can't create an instance.");
				return;
			}
			obj->m_model = mdl->second;
		}
	};

	// ELdrObject::Unknown
	template <> struct ObjectCreator<ELdrObject::Unknown> : IObjectCreator
	{
		ObjectCreator(ParseParams& pp)
			: IObjectCreator(pp)
		{}
	};

	// ELdrObject::Custom
	template <> struct ObjectCreator<ELdrObject::Custom> : IObjectCreator
	{
		ObjectCreator(ParseParams& pp)
			: IObjectCreator(pp)
		{}
	};

	#pragma endregion

	// Reads a single ldr object from a script adding object (+ children) to 'pp.m_objects'.
	// Returns true if an object was read or false if the next keyword is unrecognised
	bool ParseLdrObject(ELdrObject type, IReader& reader, ParseParams& pp)
	{
		// Push a font onto the font stack, so that fonts are scoped to object declarations
		auto font_scope = Scope<void>(
			[&]{ pp.m_font.push_back(pp.m_font.back()); },
			[&]{ pp.m_font.pop_back(); });

		// Parse the object
		LdrObjectPtr obj = {};
		switch (type)
		{
			#define PR_LDR_PARSE_IMPL(name, hash)\
				case ELdrObject::name:\
				{\
					pp.m_type = ELdrObject::name;\
					ObjectCreator<ELdrObject::name> creator(pp);\
					obj = creator.Parse(reader);\
					break;\
				}
			#define PR_LDR_PARSE(x) x(PR_LDR_PARSE_IMPL)
			PR_LDR_PARSE(PR_ENUM_LDRAW_OBJECTS)
			default: return false;
		}

		// Apply properties to the object
		// This is done after objects are parsed so that recursive properties can be applied
		ApplyObjectState(obj.get());
		
		// Add the model and instance to the containers
		pp.m_models[hash::Hash(obj->m_name.c_str())] = obj->m_model;
		pp.m_objects.push_back(obj);

		// Reset the memory pool for the next object
		pp.m_cache.Reset();

		// Report progress
		pp.ReportProgress();

		return true;
	}

	// Reads all ldr objects from a script.
	// 'add_cb' is 'bool function(int object_index, ParseResult& out, Location const& loc)'
	template <typename AddCB>
	void ParseLdrObjects(IReader& reader, ParseParams& pp, AddCB add_cb)
	{
		// Loop over keywords in the script
		for (EKeyword kw; !pp.m_cancel && reader.NextKeyword(kw);)
		{
			switch (kw)
			{
				case EKeyword::Camera:
				{
					// Camera position description
					ParseCamera(reader, pp, pp.m_result);
					break;
				}
				case EKeyword::Wireframe:
				{
					pp.m_result.m_wireframe = true;
					break;
				}
				case EKeyword::Font:
				{
					ParseFont(reader, pp, pp.m_font.back());
					break;
				}
				default:
				{
					// Save the current number of objects
					auto object_count = isize(pp.m_objects);

					// Assume the keyword is an object and start parsing
					if (!ParseLdrObject(static_cast<ELdrObject>(kw), reader, pp))
					{
						pp.ReportError(EParseError::UnknownKeyword, reader.Loc(), "Expected an object declaration");
						break;
					}

					assert("Objects removed but 'ParseLdrObject' didn't fail" && isize(pp.m_objects) > object_count);

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
	ParseResult Parse(Renderer& rdr, IReader& reader, Guid const& context_id)
	{
		ParseResult out;

		// Give initial and final progress updates
		auto exit = Scope<void>(
			[&] { !reader.Progress || reader.Progress(context_id, out, reader.Loc(), false); }, // Give an initial progress update
			[&] { !reader.Progress || reader.Progress(context_id, out, reader.Loc(), true); }); // Give a final progress update

		// Parse the script
		bool cancel = false;
		ParseParams pp(rdr, out, context_id, reader.ReportError, reader.Progress, cancel);
		ParseLdrObjects(reader, pp, [&](int){});
		return out;
	}
	ParseResult Parse(Renderer& rdr, std::string_view ldr_script, Guid const& context_id)
	{
		mem_istream<char> src{ ldr_script };
		rdr12::ldraw::TextReader reader(src, {});
		return Parse(rdr, reader, context_id);
	}
	ParseResult Parse(Renderer& rdr, std::wstring_view ldr_script, Guid const& context_id)
	{
		mem_istream<wchar_t> src{ ldr_script };
		rdr12::ldraw::TextReader reader(src, {});
		return Parse(rdr, reader, context_id);
	}
	ParseResult ParseFile(Renderer& rdr, std::filesystem::path ldr_filepath, Guid const& context_id)
	{
		if (ldr_filepath.extension() == ".ldr")
		{
			std::ifstream src{ ldr_filepath };
			rdr12::ldraw::TextReader reader(src, ldr_filepath);
			return Parse(rdr, reader, context_id);
		}
		if (ldr_filepath.extension() == ".bdr")
		{
			std::ifstream src{ ldr_filepath, std::ios::binary };
			rdr12::ldraw::BinaryReader reader(src, ldr_filepath);
			return Parse(rdr, reader, context_id);
		}
		return {};
	}

	// Create an ldr object from creation data.
	LdrObjectPtr Create(Renderer& rdr, ELdrObject type, MeshCreationData const& cdata, Guid const& context_id)
	{
		LdrObjectPtr obj(new LdrObject(type, nullptr, context_id), true);

		// Create the model
		ResourceFactory factory(rdr);
		obj->m_model = ModelGenerator::Mesh(factory, cdata);
		obj->m_model->m_name = obj->TypeAndName();
		return obj;
	}

	// Create an ldr object from a p3d model.
	LdrObjectPtr CreateP3D(Renderer& rdr, ELdrObject type, std::filesystem::path const& p3d_filepath, Guid const& context_id)
	{
		LdrObjectPtr obj(new LdrObject(type, nullptr, context_id), true);

		// Create the model
		ResourceFactory factory(rdr);
		std::ifstream src(p3d_filepath, std::ios::binary);
		ModelGenerator::LoadP3DModel(factory, src, [&](ModelTree const& tree)
			{
				auto child = ObjectCreator<ELdrObject::Model>::ModelTreeToLdr(tree, obj->m_context_id);
				if (child != nullptr) obj->AddChild(child);
				return false;
			});

		return obj;
	}
	LdrObjectPtr CreateP3D(Renderer& rdr, ELdrObject type, std::span<std::byte const> p3d_data, Guid const& context_id)
	{
		LdrObjectPtr obj(new LdrObject(type, nullptr, context_id), true);

		// Create the model
		ResourceFactory factory(rdr);
		mem_istream<char> src(p3d_data.data(), p3d_data.size());
		ModelGenerator::LoadP3DModel(factory, src, [&](ModelTree const& tree)
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
		LdrObjectPtr obj(new LdrObject(existing->m_type, nullptr, existing->m_context_id), true);

		// Use the same model
		obj->m_model = existing->m_model;
		obj->m_name = existing->m_name;
		obj->m_base_colour = existing->m_base_colour;

		// Recursively create instances of the child objects
		for (auto& child : existing->m_child)
			obj->m_child.push_back(CreateInstance(child.get()));

		return obj;
	}

	// Create an ldr object using a callback to populate the model data.
	// Objects created by this method will have dynamic usage and are suitable for updating every frame via the 'Edit' function.
	LdrObjectPtr CreateEditCB(Renderer& rdr, ELdrObject type, int vcount, int icount, int ncount, EditObjectCB edit_cb, void* ctx, Guid const& context_id)
	{
		LdrObjectPtr obj(new LdrObject(type, 0, context_id), true);

		// Create buffers for a dynamic model
		ModelDesc settings(
			ResDesc::VBuf<Vert>(vcount, {}),
			ResDesc::IBuf<uint16_t>(icount, {}),
			BBox::Reset(), obj->TypeAndName().c_str());

		// Create the model
		ResourceFactory factory(rdr);
		obj->m_model = factory.CreateModel(settings);

		// Create dummy nuggets
		NuggetDesc nug(ETopo::PointList, EGeom::Vert);
		nug.m_nflags = SetBits(nug.m_nflags, ENuggetFlag::RangesCanOverlap, true);
		for (int i = ncount; i-- != 0;)
			obj->m_model->CreateNugget(factory, nug);

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
	void Update(Renderer& rdr, LdrObject* object, IReader& reader, EUpdateObject flags)
	{
		bool cancel = false;

		// Parsing parameters
		ParseResult result;
		ParseParams pp(rdr, result, object->m_context_id, reader.ReportError, reader.Progress, cancel);
	
		// Parse 'reader' for the new model
		ParseLdrObjects(reader, pp, [&](int object_index)
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

	// IReader ------------------------------------------------------------------------------------

	// Reads a transform accumulatively. 'o2w' must be a valid initial transform
	m4x4& IReader::Transform(m4x4& o2w)
	{
		assert(IsFinite(o2w) && "A valid 'o2w' must be passed to this function as it pre-multiplies the transform with the one read from the script");

		auto p2w = m4x4::Identity();
		auto affine = IsAffine(o2w);
		auto section = SectionScope();

		// Parse the transform
		for (EKeyword kw; NextKeyword(kw);)
		{
			switch (kw)
			{
				case EKeyword::M4x4:
				{
					auto m = Matrix4x4();
					if (affine && m.w.w != 1)
					{
						ReportError(EParseError::InvalidValue, Loc(), "Specify 'NonAffine' if M4x4 is intentionally non-affine.");
						m = m4x4::Identity();
					}
					p2w = m * p2w;
					break;
				}
				case EKeyword::M3x3:
				{
					auto m = m4x4{ Matrix3x3(), v4::Origin() };
					p2w = m * p2w;
					break;
				}
				case EKeyword::Pos:
				{
					auto m = m4x4{ m3x4::Identity(), Vector3f().w1() };
					p2w = m * p2w;
					break;
				}
				case EKeyword::Align:
				{
					auto axis_id = Int<int>(10);
					auto direction = Vector3f().w0();

					v4 axis = AxisId(axis_id);
					if (axis == v4::Zero())
					{
						ReportError(EParseError::InvalidValue, Loc(), "axis_id must one of \xc2\xb1""1, \xc2\xb1""2, \xc2\xb1""3");
						axis = v4::ZAxis();
					}

					p2w = m4x4::Transform(axis, direction, v4::Origin()) * p2w;
					break;
				}
				case EKeyword::LookAt:
				{
					auto point = Vector3f().w1();
					p2w = m4x4::LookAt(o2w.pos, point, o2w.y) * p2w;
					break;
				}
				case EKeyword::Quat:
				{
					auto q = quat{ Vector4f() };
					p2w = m4x4::Transform(q, v4::Origin()) * p2w;
					break;
				}
				case EKeyword::QuatPos:
				{
					auto q = quat{ Vector4f() };
					auto p = Vector3f().w1();
					p2w = m4x4::Transform(q, p) * p2w;
					break;
				}
				case EKeyword::Rand4x4:
				{
					auto centre = Vector3f().w1();
					auto radius = Real<float>();
					p2w = m4x4::Random(g_rng(), centre, radius) * p2w;
					break;
				}
				case EKeyword::RandPos:
				{
					auto centre = Vector3f().w1();
					auto radius = Real<float>();
					p2w = m4x4::Translation(v4::Random(g_rng(), centre, radius, 1)) * p2w;
					break;
				}
				case EKeyword::RandOri:
				{
					auto m = m4x4(m3x4::Random(g_rng()), v4::Origin());
					p2w = m * p2w;
					break;
				}
				case EKeyword::Euler:
				{
					auto angles = Vector3f().w0();
					p2w = m4x4::Transform(DegreesToRadians(angles.x), DegreesToRadians(angles.y), DegreesToRadians(angles.z), v4::Origin()) * p2w;
					break;
				}
				case EKeyword::Scale:
				{
					v4 scale = {};
					scale.x = Real<float>();
					scale.y = IsSectionEnd() ? scale.x : Real<float>();
					scale.z = IsSectionEnd() ? scale.y : Real<float>();
					p2w = m4x4::Scale(scale.x, scale.y, scale.z, v4::Origin()) * p2w;
					break;
				}
				case EKeyword::Transpose:
				{
					p2w = Transpose4x4(p2w);
					break;
				}
				case EKeyword::Inverse:
				{
					p2w = IsOrthonormal(p2w) ? InvertFast(p2w) : Invert(p2w);
					break;
				}
				case EKeyword::Normalise:
				{
					p2w.x = Normalise(p2w.x);
					p2w.y = Normalise(p2w.y);
					p2w.z = Normalise(p2w.z);
					break;
				}
				case EKeyword::Orthonormalise:
				{
					p2w = Orthonorm(p2w);
					break;
				}
				case EKeyword::NonAffine:
				{
					affine = false;
					break;
				}
				default:
				{
					ReportError(EParseError::UnexpectedToken, Loc(), std::format("{} is not a valid Transform keyword", EKeyword_::ToStringA(kw)));
					break;
				}
			}
		}
		
		if (affine && !IsAffine(p2w))
		{
			ReportError(EParseError::UnexpectedToken, Loc(), "Transform is not affine. If non-affine is intended, use *NonAffine {}");
			p2w = m4x4::Identity();
		}

		// Pre-multiply the object to world transform
		o2w = p2w * o2w;
		return o2w;
	}

	// Reads a C-style string
	string32 IReader::CString()
	{
		string32 out = {};
		auto string = String<string32>();
		if (!str::ExtractStringC<string32, char const*, char>(out, string.c_str(), '\\', nullptr, nullptr))
			ReportError(EParseError::InvalidValue, Loc(), "C-style string expected");

		return out;
	}
}