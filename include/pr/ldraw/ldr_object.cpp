//***************************************************************************************************
// Ldr Object Manager
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************

#include <string>
#include <sstream>
#include <array>
#include <unordered_set>
#include <unordered_map>
#include <mutex>
#include <cwctype>
#include "pr/meta/optional.h"
#include "pr/ldraw/ldr_object.h"
#include "pr/common/assert.h"
#include "pr/crypt/hash.h"
#include "pr/maths/maths.h"
#include "pr/maths/convex_hull.h"
#include "pr/renderer11/renderer.h"
#include "pr/storage/csv.h"
#include "pr/str/extract.h"

using namespace pr::rdr;
using namespace pr::script;

namespace pr
{
	namespace ldr
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
		template <typename T> using optional = pr::optional<T>;

		// A global pool of 'Buffers' objects
		#pragma region Buffer Pool / Cache
		struct Buffers :AlignTo<16>
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

			pr::Renderer&   m_rdr;
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

			ParseParams(pr::Renderer& rdr, Reader& reader, ParseResult& result, pr::Guid const& context_id, ParseProgressCB progress_cb, bool& cancel)
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
			void ReportError(EResult result)
			{
				m_reader.ReportError(result);
			}
			void ReportError(EResult result, char const* msg) 
			{
				m_reader.ReportError(result, msg);
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
				m_cancel = !m_progress_cb(m_context_id, m_result, m_reader.Loc(), false);
				m_last_progress_update = system_clock::now();
			}
		};

		// Template prototype for ObjectCreators
		template <ELdrObject ObjType> struct ObjectCreator;

		// String hash wrapper
		inline size_t Hash(char const* str)
		{
			return pr::hash::Hash(str);
		}

		// Forward declare the recursive object parsing function
		bool ParseLdrObject(ParseParams& p);

		#pragma region Parse Common Elements

		// Read the name, colour, and instance flag for an object
		ObjectAttributes ParseAttributes(pr::script::Reader& reader, ELdrObject model_type)
		{
			ObjectAttributes attr;
			attr.m_type = model_type;
			attr.m_name = "";
			
			// Read the next tokens up to the section start
			wstring32 tok0, tok1; auto count = 0;
			if (!reader.IsSectionStart()) { reader.Token(tok0, L"{}"); ++count; }
			if (!reader.IsSectionStart()) { reader.Token(tok1, L"{}"); ++count; }
			if (!reader.IsSectionStart())
				reader.ReportError(pr::script::EResult::UnknownToken, "object attributes are invalid");

			switch (count)
			{
			case 2:
				{
					// Expect: *Type <name> <colour>
					if (!str::ExtractIdentifierC(attr.m_name, std::begin(tok0)))
						reader.ReportError(pr::script::EResult::TokenNotFound, "object name is invalid");
					if (!str::ExtractIntC(attr.m_colour.argb, 16, std::begin(tok1)))
						reader.ReportError(pr::script::EResult::TokenNotFound, "object colour is invalid");
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
							reader.ReportError(pr::script::EResult::TokenNotFound, "object colour is invalid");
					}
					else
					{
						attr.m_colour = 0xFFFFFFFF;
						if (!str::ExtractIdentifierC(attr.m_name, std::begin(tok0)))
							reader.ReportError(pr::script::EResult::TokenNotFound, "object name is invalid");
					}
					break;
				}
			case 0:
				{
					attr.m_name = ToStringA(model_type);
					attr.m_colour = 0xFFFFFFFF;
					break;
				}
			}
			return attr;
		}

		// Parse a camera description
		void ParseCamera(pr::script::Reader& reader, ParseResult& out)
		{
			reader.SectionStart();
			for (EKeyword kw; reader.NextKeywordH(kw);)
			{
				switch (kw)
				{
				default:
					{
						reader.ReportError(pr::script::EResult::UnknownToken);
						break;
					}
				case EKeyword::O2W:
					{
						auto c2w = pr::m4x4Identity;
						reader.TransformS(c2w);
						out.m_cam.CameraToWorld(c2w);
						out.m_cam_fields |= ECamField::C2W;
						break;
					}
				case EKeyword::LookAt:
					{
						pr::v4 lookat;
						reader.Vector3S(lookat, 1.0f);
						pr::m4x4 c2w = out.m_cam.CameraToWorld();
						out.m_cam.LookAt(c2w.pos, lookat, c2w.y);
						out.m_cam_fields |= ECamField::C2W;
						out.m_cam_fields |= ECamField::Focus;
						break;
					}
				case EKeyword::Align:
					{
						pr::v4 align;
						reader.Vector3S(align, 0.0f);
						out.m_cam.Align(align);
						out.m_cam_fields |= ECamField::Align;
						break;
					}
				case EKeyword::Aspect:
					{
						float aspect;
						reader.RealS(aspect);
						out.m_cam.Aspect(aspect);
						out.m_cam_fields |= ECamField::Align;
						break;
					}
				case EKeyword::FovX:
					{
						float fovX;
						reader.RealS(fovX);
						out.m_cam.FovX(fovX);
						out.m_cam_fields |= ECamField::FovY;
						break;
					}
				case EKeyword::FovY:
					{
						float fovY;
						reader.RealS(fovY);
						out.m_cam.FovY(fovY);
						out.m_cam_fields |= ECamField::FovY;
						break;
					}
				case EKeyword::Fov:
					{
						float fov[2];
						reader.RealS(fov, 2);
						out.m_cam.Fov(fov[0], fov[1]);
						out.m_cam_fields |= ECamField::Aspect;
						out.m_cam_fields |= ECamField::FovY;
						break;
					}
				case EKeyword::Near:
					{
						reader.Real(out.m_cam.m_near);
						out.m_cam_fields |= ECamField::Near;
						break;
					}
				case EKeyword::Far:
					{
						reader.Real(out.m_cam.m_far);
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
			reader.SectionEnd();
		}

		// Parse a font description
		void ParseFont(pr::script::Reader& reader, Font& font)
		{
			reader.SectionStart();
			font.m_underline = false;
			font.m_strikeout = false;
			for (EKeyword kw; reader.NextKeywordH(kw);)
			{
				switch (kw)
				{
				default:
					{
						reader.ReportError(pr::script::EResult::UnknownToken);
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
		void ParseAnimation(pr::script::Reader& reader, Animation& anim)
		{
			reader.SectionStart();
			for (EKeyword kw; reader.NextKeywordH(kw);)
			{
				switch (kw)
				{
				default:
					{
						reader.ReportError(pr::script::EResult::UnknownToken);
						break;
					}
				case EKeyword::Style:
					{
						char style[50];
						reader.Identifier(style);
						if      (pr::str::EqualI(style, "NoAnimation"   )) anim.m_style = EAnimStyle::NoAnimation;
						else if (pr::str::EqualI(style, "PlayOnce"      )) anim.m_style = EAnimStyle::PlayOnce;
						else if (pr::str::EqualI(style, "PlayReverse"   )) anim.m_style = EAnimStyle::PlayReverse;
						else if (pr::str::EqualI(style, "PingPong"      )) anim.m_style = EAnimStyle::PingPong;
						else if (pr::str::EqualI(style, "PlayContinuous")) anim.m_style = EAnimStyle::PlayContinuous;
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
			std::string tex_filepath;
			auto t2s = pr::m4x4Identity;
			bool has_alpha = false;
			SamplerDesc sam;

			p.m_reader.SectionStart();
			while (!p.m_reader.IsSectionEnd())
			{
				if (p.m_reader.IsKeyword())
				{
					auto kw = p.m_reader.NextKeywordH<EKeyword>();
					switch (kw)
					{
					default:
						{
							p.ReportError(EResult::UnknownToken);
							break;
						}
					case EKeyword::O2W:
						{
							p.m_reader.TransformS(t2s);
							break;
						}
					case EKeyword::Addr:
						{
							char word[20];
							p.m_reader.SectionStart();
							p.m_reader.Identifier(word); sam.AddressU = (D3D11_TEXTURE_ADDRESS_MODE)Enum<ETexAddrMode>::Parse(word, false);
							p.m_reader.Identifier(word); sam.AddressV = (D3D11_TEXTURE_ADDRESS_MODE)Enum<ETexAddrMode>::Parse(word, false);
							p.m_reader.SectionEnd();
							break;
						}
					case EKeyword::Filter:
						{
							char word[20];
							p.m_reader.SectionStart();
							p.m_reader.Identifier(word); sam.Filter = (D3D11_FILTER)Enum<EFilter>::Parse(word, false);
							p.m_reader.SectionEnd();
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
					p.m_reader.String(tex_filepath);
				}
			}
			p.m_reader.SectionEnd();

			// Silently ignore missing texture files
			if (!tex_filepath.empty())
			{
				// Create the texture
				try
				{
					tex = p.m_rdr.m_tex_mgr.CreateTexture2D(AutoId, sam, tex_filepath.c_str(), has_alpha, pr::filesys::GetFiletitle(tex_filepath).c_str());
					tex->m_t2s = t2s;
				}
				catch (std::exception const& e)
				{
					p.ReportError(EResult::ValueNotFound, pr::FmtS("failed to create texture %s\n%s", tex_filepath.c_str(), e.what()));
				}
			}
			return true;
		}

		// Parse a video texture
		bool ParseVideo(ParseParams& p, Texture2DPtr& vid)
		{
			std::string filepath;
			p.m_reader.SectionStart();
			p.m_reader.String(filepath);
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
			p.m_reader.SectionEnd();
			return true;
		}

		// Parse keywords that can appear in any section. Returns true if the keyword was recognised.
		bool ParseProperties(ParseParams& p, EKeyword kw, LdrObject* obj)
		{
			switch (kw)
			{
			default: return false;
			case EKeyword::O2W:
			case EKeyword::Txfm:
				{
					p.m_reader.TransformS(obj->m_o2p);
					return true;
				}
			case EKeyword::Colour:
				{
					p.m_reader.IntS(obj->m_base_colour.argb, 16);
					return true;
				}
			case EKeyword::ColourMask:
				{
					p.m_reader.IntS(obj->m_colour_mask, 16);
					return true;
				}
			case EKeyword::RandColour:
				{
					obj->m_base_colour = pr::RandomRGB(g_rng());
					return true;
				}
			case EKeyword::Animation:
				{
					ParseAnimation(p.m_reader, obj->m_anim);
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
					obj->m_screen_space = EventSub((evt::IEventHandler*)1, 0);
					return true;
				}
			case EKeyword::Font:
				{
					ParseFont(p.m_reader, p.m_font.back());
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

		#pragma endregion

		#pragma region ObjectCreator Base Classes

		// Base class for all object creators
		struct IObjectCreator
		{
			ParseParams& p;
			virtual ~IObjectCreator() {}
			IObjectCreator(ParseParams& p_) :p(p_) {}
			virtual bool ParseKeyword(EKeyword)
			{
				return false;
			}
			virtual void Parse()
			{
				p.ReportError(EResult::UnknownToken);
			}
			virtual void CreateModel(LdrObject*)
			{}
		};

		// Base class for objects with a texture
		struct IObjectCreatorTexture :IObjectCreator
		{
			Texture2DPtr m_texture;
			NuggetProps m_local_mat;

			IObjectCreatorTexture(ParseParams& p)
				:IObjectCreator(p)
				,m_texture()
				,m_local_mat()
			{}
			bool ParseKeyword(EKeyword kw) override
			{
				switch (kw) {
				default: return IObjectCreator::ParseKeyword(kw);
				case EKeyword::Texture:  ParseTexture(p, m_texture); return true;
				case EKeyword::Video:    ParseVideo(p, m_texture); return true;
				}
			}
			virtual NuggetProps* Material()
			{
				// This function is used to pass texture/shader data to the model generated.
				// Topo and Geom are not used, each model type knows what topo and geom it's using.
				m_local_mat.m_tex_diffuse = m_texture;
				//if (m_texture->m_video)
				//	m_texture->m_video->Play(true);
				return &m_local_mat;
			}
		};

		// Base class of light source objects
		struct IObjectCreatorLight :IObjectCreator
		{
			Light m_light;

			IObjectCreatorLight(ParseParams& p)
				:IObjectCreator(p)
				,m_light()
			{
				m_light.m_on = true;
			}
			bool ParseKeyword(EKeyword kw) override
			{
				switch (kw)
				{
				default: return IObjectCreator::ParseKeyword(kw);
				case EKeyword::Range:
					{
						p.m_reader.SectionStart();
						p.m_reader.Real(m_light.m_range);
						p.m_reader.Real(m_light.m_falloff);
						p.m_reader.SectionEnd();
						return true;
					}
				case EKeyword::Specular:
					{
						p.m_reader.SectionStart();
						p.m_reader.Int(m_light.m_specular.argb, 16);
						p.m_reader.Real(m_light.m_specular_power);
						p.m_reader.SectionEnd();
						return true;
					}
				case EKeyword::CastShadow:
					{
						p.m_reader.RealS(m_light.m_cast_shadow);
						return true;
					}
				}
			}
			void CreateModel(LdrObject* obj) override
			{
				// Assign the light data as user data
				obj->m_user_data.get<Light>() = m_light;
			}
		};

		// Base class for point sprite objects
		struct IObjectCreatorPoint :IObjectCreatorTexture
		{
			enum class EStyle
			{
				Square,
				Circle,
				Triangle,
				Star,
				Annulus,
			};

			VCont& m_point;
			CCont& m_color;
			bool   m_per_point_colour;
			EStyle m_style;
			v2     m_point_size;
			bool   m_depth;

			IObjectCreatorPoint(ParseParams& p)
				:IObjectCreatorTexture(p)
				,m_point(p.m_cache.m_point)
				,m_color(p.m_cache.m_color)
				,m_per_point_colour(false)
				,m_style(EStyle::Square)
				,m_point_size()
				,m_depth(false)
			{}
			void Parse() override
			{
				pr::v4 pt;
				p.m_reader.Vector3(pt, 1.0f);
				m_point.push_back(pt);
				if (m_per_point_colour)
				{
					pr::Colour32 col;
					p.m_reader.Int(col.argb, 16);
					m_color.push_back(col);
				}
			}
			bool ParseKeyword(EKeyword kw) override
			{
				switch (kw)
				{
				default:
					{
						return IObjectCreatorTexture::ParseKeyword(kw);
					}
				case EKeyword::Coloured:
					{
						m_per_point_colour = true;
						return true;
					}
				case EKeyword::Width:
					{
						p.m_reader.RealS(m_point_size.x);
						m_point_size.y = m_point_size.x;
						return true;
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
						default: p.m_reader.ReportError(pr::script::EResult::UnknownToken); break;
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
			void CreateModel(LdrObject* obj) override
			{
				using namespace pr::rdr;

				// Validate
				if (m_point.size() < 1)
				{
					p.ReportError(EResult::Failed, pr::FmtS("Point object '%s' description incomplete", obj->TypeAndName().c_str()));
					return;
				}

				// Create the model
				obj->m_model = ModelGenerator<>::Points(p.m_rdr, int(m_point.size()), m_point.data(), int(m_color.size()), m_color.data(), Material());
				obj->m_model->m_name = obj->TypeAndName();

				// Use a geometry shader to draw points
				if (m_point_size.x != 0 && m_point_size.y != 0)
				{
					// Get/Create an instance of the point sprites shader
					auto id = pr::hash::Hash("LDrawPointSprites", m_point_size, m_depth);
					auto shdr = p.m_rdr.m_shdr_mgr.GetShader<PointSpritesGS>(id, RdrId(EStockShader::PointSpritesGS));
					shdr->m_size = m_point_size;
					shdr->m_depth = m_depth;

					// Get/Create the point style texture
					auto tex = PointStyleTexture(m_style, To<iv2>(m_point_size));

					// Update the nuggets
					for (auto& nug : obj->m_model->m_nuggets)
					{
						nug.m_tex_diffuse = tex;
						nug.m_smap[ERenderStep::ForwardRender].m_gs = shdr;
					}
				}
			}
			Texture2DPtr PointStyleTexture(EStyle style, iv2 const& size)
			{
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
						return p.m_rdr.m_tex_mgr.GetTexture(id, [=]
						{
							auto w0 = sz.x * 0.5f;
							auto h0 = sz.y * 0.5f;
							return CreatePointStyleTexture(id, sz, "PointStyleCircle", [=](auto dc, auto fr, auto){ dc->FillEllipse({{w0, h0}, w0, h0}, fr); });
						});
					}
				case EStyle::Triangle:
					{
						auto id = pr::hash::Hash("PointStyleTriangle", sz);
						return p.m_rdr.m_tex_mgr.GetTexture(id, [=]
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
							sink->AddLine    ({0.0f*w0, h1});
							sink->AddLine    ({0.5f*w0, h0 + h1});
							sink->EndFigure(D2D1_FIGURE_END_CLOSED);
							pr::Throw(sink->Close());

							return CreatePointStyleTexture(id, sz, "PointStyleTriangle", [=](auto dc, auto fr, auto){ dc->FillGeometry(geom.get(), fr, nullptr); });
						});
					}
				case EStyle::Star:
					{
						auto id = pr::hash::Hash("PointStyleStar", sz);
						return p.m_rdr.m_tex_mgr.GetTexture(id, [=]
						{
							Renderer::Lock lk(p.m_rdr);
							D3DPtr<ID2D1PathGeometry> geom;
							D3DPtr<ID2D1GeometrySink> sink;
							pr::Throw(lk.D2DFactory()->CreatePathGeometry(&geom.m_ptr));
							pr::Throw(geom->Open(&sink.m_ptr));

							auto w0 = 1.0f * sz.x;
							auto h0 = 1.0f * sz.y;

							sink->BeginFigure({0.5f*w0, 0.0f*h0}, D2D1_FIGURE_BEGIN_FILLED);
							sink->AddLine    ({0.4f*w0, 0.4f*h0});
							sink->AddLine    ({0.0f*w0, 0.5f*h0});
							sink->AddLine    ({0.4f*w0, 0.6f*h0});
							sink->AddLine    ({0.5f*w0, 1.0f*h0});
							sink->AddLine    ({0.6f*w0, 0.6f*h0});
							sink->AddLine    ({1.0f*w0, 0.5f*h0});
							sink->AddLine    ({0.6f*w0, 0.4f*h0});
							sink->EndFigure(D2D1_FIGURE_END_CLOSED);
							pr::Throw(sink->Close());

							return CreatePointStyleTexture(id, sz, "PointStyleStar", [=](auto dc, auto fr, auto){ dc->FillGeometry(geom.get(), fr, nullptr); });
						});
					}
				case EStyle::Annulus:
					{
						auto id = pr::hash::Hash("PointStyleAnnulus", sz);
						return p.m_rdr.m_tex_mgr.GetTexture(id, [=]
						{
							auto w0 = sz.x * 0.5f;
							auto h0 = sz.y * 0.5f;
							auto w1 = sz.x * 0.4f;
							auto h1 = sz.y * 0.4f;
							return CreatePointStyleTexture(id, sz, "PointStyleAnnulus", [=](auto dc, auto fr, auto bk)
							{
								dc->SetPrimitiveBlend(D2D1_PRIMITIVE_BLEND_COPY);
								dc->FillEllipse({{w0, h0}, w0, h0}, fr);
								dc->FillEllipse({{w0, h0}, w1, h1}, bk);
							});
						});
					}
				}
			}
			template <typename TDrawOnIt> Texture2DPtr CreatePointStyleTexture(RdrId id, iv2 const& sz, char const* name, TDrawOnIt draw)
			{
				// Create a texture large enough to contain the text, and render the text into it
				SamplerDesc sdesc(D3D11_TEXTURE_ADDRESS_CLAMP, D3D11_FILTER_MIN_MAG_MIP_POINT);
				TextureDesc tdesc(size_t(sz.x), size_t(sz.y), 1, DXGI_FORMAT_R8G8B8A8_UNORM);
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

		// Base class for object creators that are based on lines
		struct IObjectCreatorLine :IObjectCreator
		{
			VCont& m_point;
			ICont& m_index;
			CCont& m_color;
			v2     m_dashed;
			float  m_line_width;
			bool   m_per_line_colour;
			bool   m_smooth;
			bool   m_linestrip;
			bool   m_linemesh;

			IObjectCreatorLine(ParseParams& p, bool linestrip, bool linemesh)
				:IObjectCreator(p)
				,m_point(p.m_cache.m_point)
				,m_index(p.m_cache.m_index)
				,m_color(p.m_cache.m_color)
				,m_line_width()
				,m_dashed(v2XAxis)
				,m_per_line_colour()
				,m_smooth()
				,m_linestrip(linestrip)
				,m_linemesh(linemesh)
			{}
			bool ParseKeyword(EKeyword kw) override
			{
				switch (kw)
				{
				default: return IObjectCreator::ParseKeyword(kw);
				case EKeyword::Coloured:
					{
						m_per_line_colour = true;
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
				case EKeyword::Param:
					{
						float t[2];
						p.m_reader.RealS(t, 2);
						if (m_point.size() < 2)
						{
							p.ReportError(EResult::Failed, "No preceding line to apply parametric values to");
						}
						auto& p0 = m_point[m_point.size() - 2];
						auto& p1 = m_point[m_point.size() - 1];
						auto dir = p1 - p0;
						auto pt = p0;
						p0 = pt + t[0] * dir;
						p1 = pt + t[1] * dir;
						return true;
					}
				}
			}
			void CreateModel(LdrObject* obj) override
			{
				using namespace pr::rdr;

				// Validate
				if (m_point.size() < 2)
				{
					// Allow line strips to have 0 or 1 point because they are a created from
					// lists of points and treating 0 or 1 as a special case is inconvenient
					if (m_linestrip) return;
					p.ReportError(EResult::Failed, pr::FmtS("Line object '%s' description incomplete", obj->TypeAndName().c_str()));
					return;
				}

				// Smooth the points
				if (m_smooth && m_linestrip)
				{
					VCont points;
					std::swap(points, m_point);
					pr::Smooth(points, m_point);
				}

				// Convert lines to dashed lines
				if (m_dashed != v2XAxis)
				{
					VCont points;
					std::swap(points, m_point);

					if (m_linestrip)
					{
						// 'Dashing' a line turns it into a line list
						DashLineStrip(points, m_point, m_dashed);
						m_linestrip = false;
					}
					else if (!m_linemesh)
					{
						// Convert each line segment to dashed lines
						DashLineList(points, m_point, m_dashed);
					}
				}

				// Create the model
				if (m_linemesh)
				{
					auto cdata = MeshCreationData()
						.verts  (m_point.data(), int(m_point.size()))
						.indices(m_index.data(), int(m_index.size()))
						.colours(m_color.data(), int(m_color.size()))
						.nuggets({NuggetProps(m_linestrip ? EPrim::LineStrip : EPrim::LineList, EGeom::Vert|EGeom::Colr)});
					obj->m_model = ModelGenerator<>::Mesh(p.m_rdr, cdata);
				}
				else if (m_linestrip)
				{
					obj->m_model = ModelGenerator<>::LineStrip(p.m_rdr, int(m_point.size() - 1), m_point.data(), int(m_color.size()), m_color.data());
				}
				else
				{
					obj->m_model = ModelGenerator<>::Lines(p.m_rdr, int(m_point.size() / 2), m_point.data(), int(m_color.size()), m_color.data());
				}
				obj->m_model->m_name = obj->TypeAndName();

				// Use thick lines
				if (m_line_width != 0.0f)
				{
					// Get or create an instance of the thick line shader
					auto id = pr::hash::Hash("LDrawThickLine", m_line_width);
					auto shdr = p.m_rdr.m_shdr_mgr.GetShader<ThickLineListGS>(id, RdrId(EStockShader::ThickLineListGS));
					shdr->m_width = m_line_width;

					for (auto& nug : obj->m_model->m_nuggets)
						nug.m_smap[ERenderStep::ForwardRender].m_gs = shdr;
				}
			}
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
		};

		// Base class for object creators that are based on 2d shapes
		struct IObjectCreatorShape2d :IObjectCreatorTexture
		{
			AxisId m_axis_id;
			v4     m_dim;
			int    m_facets;
			bool   m_solid;

			IObjectCreatorShape2d(ParseParams& p)
				:IObjectCreatorTexture(p)
				,m_axis_id(AxisId::PosZ)
				,m_dim()
				,m_facets(40)
				,m_solid(false)
			{}
			bool ParseKeyword(EKeyword kw) override
			{
				switch (kw)
				{
				default: return IObjectCreatorTexture::ParseKeyword(kw);
				case EKeyword::Solid:  m_solid = true; return true;
				case EKeyword::Facets: p.m_reader.IntS(m_facets, 10); return true;
				}
			}
		};

		// Base class for planar objects
		struct IObjectCreatorPlane :IObjectCreatorTexture
		{
			VCont& m_point;
			CCont& m_color;
			bool   m_per_vert_colour;

			IObjectCreatorPlane(ParseParams& p)
				:IObjectCreatorTexture(p)
				,m_point(p.m_cache.m_point)
				,m_color(p.m_cache.m_color)
				,m_per_vert_colour()
			{}
			bool ParseKeyword(EKeyword kw) override
			{
				switch (kw) {
				default: return IObjectCreatorTexture::ParseKeyword(kw);
				case EKeyword::Coloured:
					#pragma region
					{
						m_per_vert_colour = true;
						return true;
					}
					#pragma endregion
				}
			}
			void CreateModel(LdrObject* obj) override
			{
				using namespace pr::rdr;

				// Validate
				if (m_point.empty() || (m_point.size() % 4) != 0)
				{
					p.ReportError(EResult::Failed, "Object description incomplete");
					return;
				}

				// Create the model
				obj->m_model = ModelGenerator<>::Quad(p.m_rdr, int(m_point.size() / 4), m_point.data(), int(m_color.size()), m_color.data(), pr::m4x4Identity, Material());
				obj->m_model->m_name = obj->TypeAndName();
			}
		};

		// Base class for arbitrary cuboid shapes
		struct IObjectCreatorCuboid :IObjectCreatorTexture
		{
			pr::v4 m_pt[8];
			pr::m4x4 m_b2w;

			IObjectCreatorCuboid(ParseParams& p)
				:IObjectCreatorTexture(p)
				,m_pt()
				,m_b2w(m4x4Identity)
			{}
			void CreateModel(LdrObject* obj) override
			{
				obj->m_model = ModelGenerator<>::Boxes(p.m_rdr, 1, m_pt, m_b2w, 0, 0, Material());
				obj->m_model->m_name = obj->TypeAndName();
			}
		};

		// Base class for cone shapes
		struct IObjectCreatorCone :IObjectCreatorTexture
		{
			AxisId m_axis_id;
			v4     m_dim; // x,y = radius, z = height
			v2     m_scale;
			int    m_layers;
			int    m_wedges;

			IObjectCreatorCone(ParseParams& p)
				:IObjectCreatorTexture(p)
				,m_axis_id(AxisId::PosZ)
				,m_dim()
				,m_scale(v2One)
				,m_layers(1)
				,m_wedges(20)
			{}
			bool ParseKeyword(EKeyword kw) override
			{
				switch (kw)
				{
				default: return IObjectCreatorTexture::ParseKeyword(kw);
				case EKeyword::Layers:  p.m_reader.Int(m_layers, 10); return true;
				case EKeyword::Wedges:  p.m_reader.Int(m_wedges, 10); return true;
				case EKeyword::Scale:   p.m_reader.Vector2(m_scale); return true;
				}
			}
			void CreateModel(LdrObject* obj) override
			{
				m4x4 o2w, *po2w = nullptr;
				if (m_axis_id != AxisId::PosZ)
				{
					o2w = m4x4::Transform(AxisId::PosZ, m_axis_id, v4Origin);
					po2w = &o2w;
				}

				// Create the model
				obj->m_model = ModelGenerator<>::Cylinder(p.m_rdr, m_dim.x, m_dim.y, m_dim.z, m_scale.x, m_scale.y, m_wedges, m_layers, 1, &pr::Colour32White, po2w, Material());
				obj->m_model->m_name = obj->TypeAndName();
			}
		};

		// Base class for mesh based objects
		struct IObjectCreatorMesh :IObjectCreatorTexture
		{
			VCont& m_verts;
			ICont& m_indices;
			NCont& m_normals;
			CCont& m_colours;
			TCont& m_texs;
			GCont& m_nuggets;
			float m_gen_normals;

			IObjectCreatorMesh(ParseParams& p)
				:IObjectCreatorTexture(p)
				,m_verts  (p.m_cache.m_point)
				,m_indices(p.m_cache.m_index)
				,m_normals(p.m_cache.m_norms)
				,m_colours(p.m_cache.m_color)
				,m_texs   (p.m_cache.m_texts)
				,m_nuggets(p.m_cache.m_nugts)
				,m_gen_normals(-1.0f)
			{}
			bool ParseKeyword(EKeyword kw) override
			{
				switch (kw)
				{
				default: return IObjectCreatorTexture::ParseKeyword(kw);
				case EKeyword::Verts:
					#pragma region
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
					#pragma endregion
				case EKeyword::Normals:
					#pragma region
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
					#pragma endregion
				case EKeyword::Colours:
					#pragma region
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
					#pragma endregion
				case EKeyword::TexCoords:
					#pragma region
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
					#pragma endregion
				case EKeyword::Lines:
					#pragma region
					{
						NuggetProps nug = *Material();
						nug.m_topo = EPrim::LineList;
						nug.m_geom = EGeom::Vert |
							(!m_colours.empty() ? EGeom::Colr : EGeom::None);
						nug.m_vrange = pr::rdr::Range::Reset();
						nug.m_irange = pr::rdr::Range(m_indices.size(), m_indices.size());
						nug.m_geometry_has_alpha = false;

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
					#pragma endregion
				case EKeyword::Faces:
					#pragma region
					{
						NuggetProps nug = *Material();
						nug.m_topo = EPrim::TriList;
						nug.m_geom = EGeom::Vert |
							(!m_normals.empty() ? EGeom::Norm : EGeom::None) |
							(!m_colours.empty() ? EGeom::Colr : EGeom::None) |
							(!m_texs.empty()    ? EGeom::Tex0 : EGeom::None);
						nug.m_vrange = pr::rdr::Range::Reset();
						nug.m_irange = pr::rdr::Range(m_indices.size(), m_indices.size());
						nug.m_geometry_has_alpha = false;

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
					#pragma endregion
				case EKeyword::Tetra:
					#pragma region
					{
						NuggetProps nug = *Material();
						nug.m_topo = EPrim::TriList;
						nug.m_geom = EGeom::Vert |
							(!m_normals.empty() ? EGeom::Norm : EGeom::None) |
							(!m_colours.empty() ? EGeom::Colr : EGeom::None) |
							(!m_texs.empty()    ? EGeom::Tex0 : EGeom::None);
						nug.m_vrange = pr::rdr::Range::Reset();
						nug.m_irange = pr::rdr::Range(m_indices.size(), m_indices.size());
						nug.m_geometry_has_alpha = false;

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
					#pragma endregion
				case EKeyword::GenerateNormals:
					#pragma region
					{
						p.m_reader.RealS(m_gen_normals);
						m_gen_normals = pr::DegreesToRadians(m_gen_normals);
						return true;
					}
					#pragma endregion
				}
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
							nug.m_geometry_has_alpha = true;
							break;
						}
					}
				}

				// Generate normals if needed
				if (m_gen_normals >= 0.0f)
				{
					// Can't generate normals per nugget because nuggets may share vertices.
					// Generate normals for all vertices (verts used by lines only with have zero-normals)
					m_normals.resize(m_verts.size());

					// Generate normals for the nuggets containing faces
					for (auto& nug : m_nuggets)
					{
						// Not face topology...
						if (nug.m_topo != EPrim::TriList)
							continue;

						// Not sure if this works... needs testing
						auto icount = nug.m_irange.size();
						auto iptr = m_indices.data() + nug.m_irange.begin();
						pr::geometry::GenerateNormals(icount, iptr, m_gen_normals,
							[&](pr::uint16 i)
							{
								return m_verts[i];
							}, 0,
							[&](pr::uint16 new_idx, pr::uint16 orig_idx, v4 const& norm)
							{
								if (new_idx >= m_verts.size())
								{
									m_verts  .resize(new_idx + 1, m_verts[orig_idx]);
									m_normals.resize(new_idx + 1, m_normals[orig_idx]);
								}
								m_normals[new_idx] = norm;
							},
							[&](pr::uint16 i0, pr::uint16 i1, pr::uint16 i2)
							{
								*iptr++ = i0;
								*iptr++ = i1;
								*iptr++ = i2;
							});
					}
				}

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

		#pragma endregion

		#pragma region Sprite Objects

		// ELdrObject::Point
		template <> struct ObjectCreator<ELdrObject::Point> :IObjectCreatorPoint
		{
			ObjectCreator(ParseParams& p)
				:IObjectCreatorPoint(p)
			{}
		};

		#pragma endregion

		#pragma region Line Objects

		// ELdrObject::Line
		template <> struct ObjectCreator<ELdrObject::Line> :IObjectCreatorLine
		{
			ObjectCreator(ParseParams& p)
				:IObjectCreatorLine(p, false, false)
			{}
			void Parse() override
			{
				pr::v4 p0, p1;
				p.m_reader.Vector3(p0, 1.0f);
				p.m_reader.Vector3(p1, 1.0f);
				m_point.push_back(p0);
				m_point.push_back(p1);
				if (m_per_line_colour)
				{
					pr::Colour32 col;
					p.m_reader.Int(col.argb, 16);
					m_color.push_back(col);
					m_color.push_back(col);
				}
			}
		};

		// ELdrObject::LineD
		template <> struct ObjectCreator<ELdrObject::LineD> :IObjectCreatorLine
		{
			ObjectCreator(ParseParams& p)
				:IObjectCreatorLine(p, false, false)
			{}
			void Parse() override
			{
				pr::v4 p0, p1;
				p.m_reader.Vector3(p0, 1.0f);
				p.m_reader.Vector3(p1, 0.0f);
				m_point.push_back(p0);
				m_point.push_back(p0 + p1);
				if (m_per_line_colour)
				{
					pr::Colour32 col;
					p.m_reader.Int(col.argb, 16);
					m_color.push_back(col);
					m_color.push_back(col);
				}
			}
		};

		// ELdrObject::LineStrip
		template <> struct ObjectCreator<ELdrObject::LineStrip> :IObjectCreatorLine
		{
			ObjectCreator(ParseParams& p)
				:IObjectCreatorLine(p, true, false)
			{}
			void Parse() override
			{
				pr::v4 pt;
				p.m_reader.Vector3(pt, 1.0f);
				m_point.push_back(pt);
				
				if (m_per_line_colour)
				{
					pr::Colour32 col;
					p.m_reader.Int(col.argb, 16);
					m_color.push_back(col);
				}
			}
		};

		// ELdrObject::LineBox
		template <> struct ObjectCreator<ELdrObject::LineBox> :IObjectCreatorLine
		{
			ObjectCreator(ParseParams& p)
				:IObjectCreatorLine(p, false, true)
			{}
			void Parse() override
			{
				pr::v4 dim;
				p.m_reader.Real(dim.x);
				if (p.m_reader.IsKeyword() || p.m_reader.IsSectionEnd()) dim.y = dim.x; else p.m_reader.Real(dim.y);
				if (p.m_reader.IsKeyword() || p.m_reader.IsSectionEnd()) dim.z = dim.y; else p.m_reader.Real(dim.z);
				dim *= 0.5f;

				m_point.push_back(v4(-dim.x, -dim.y, -dim.z, 1.0f));
				m_point.push_back(v4(+dim.x, -dim.y, -dim.z, 1.0f));
				m_point.push_back(v4(+dim.x, +dim.y, -dim.z, 1.0f));
				m_point.push_back(v4(-dim.x, +dim.y, -dim.z, 1.0f));
				m_point.push_back(v4(-dim.x, -dim.y, +dim.z, 1.0f));
				m_point.push_back(v4(+dim.x, -dim.y, +dim.z, 1.0f));
				m_point.push_back(v4(+dim.x, +dim.y, +dim.z, 1.0f));
				m_point.push_back(v4(-dim.x, +dim.y, +dim.z, 1.0f));

				pr::uint16 idx[] = { 0, 1, 1, 2, 2, 3, 3, 0, 4, 5, 5, 6, 6, 7, 7, 4, 0, 4, 1, 5, 2, 6, 3, 7 };
				m_index.insert(m_index.end(), idx, idx + PR_COUNTOF(idx));
			}
		};

		// ELdrObject::Grid
		template <> struct ObjectCreator<ELdrObject::Grid> :IObjectCreatorLine
		{
			ObjectCreator(ParseParams& p)
				:IObjectCreatorLine(p, false, false)
			{}
			void Parse() override
			{
				int axis_id;
				pr::v2 dim, div;
				p.m_reader.Int(axis_id, 10);
				p.m_reader.Vector2(dim);
				if (p.m_reader.IsKeyword() || p.m_reader.IsSectionEnd()) div = dim; else p.m_reader.Vector2(div);

				pr::v2 step = dim / div;
				for (float i = -dim.x / 2; i <= dim.x / 2; i += step.x)
				{
					m_point.push_back(v4(i, -dim.y / 2, 0, 1));
					m_point.push_back(v4(i, dim.y / 2, 0, 1));
				}
				for (float i = -dim.y / 2; i <= dim.y / 2; i += step.y)
				{
					m_point.push_back(v4(-dim.x / 2, i, 0, 1));
					m_point.push_back(v4(dim.x / 2, i, 0, 1));
				}
			}
		};

		// ELdrObject::Spline
		template <> struct ObjectCreator<ELdrObject::Spline> :IObjectCreatorLine
		{
			ObjectCreator(ParseParams& p)
				:IObjectCreatorLine(p, true, false)
			{}
			void Parse() override
			{
				pr::Spline spline;
				p.m_reader.Vector3(spline.x, 1.0f);
				p.m_reader.Vector3(spline.y, 1.0f);
				p.m_reader.Vector3(spline.z, 1.0f);
				p.m_reader.Vector3(spline.w, 1.0f);

				// Generate points for the spline
				pr::vector<pr::v4, 30, true> raster;
				pr::Raster(spline, raster, 30);
				m_point.insert(std::end(m_point), std::begin(raster), std::end(raster));

				if (m_per_line_colour)
				{
					pr::Colour32 col;
					p.m_reader.Int(col.argb, 16);
					for (size_t i = 0, iend = raster.size(); i != iend; ++i)
						m_color.push_back(col);
				}
			}
		};

		// ELdrObject::Arrow
		template <> struct ObjectCreator<ELdrObject::Arrow> :IObjectCreatorLine
		{
			enum EArrowType { Invalid = -1, Line = 0, Fwd = 1 << 0, Back = 1 << 1, FwdBack = Fwd | Back };
			EArrowType m_type;

			ObjectCreator(ParseParams& p)
				:IObjectCreatorLine(p, true, false)
				,m_type(EArrowType::Invalid)
			{}
			void Parse() override
			{
				// If no points read yet, expect the arrow type first
				if (m_type == EArrowType::Invalid)
				{
					string32 ty;
					p.m_reader.Identifier(ty);
					if      (pr::str::EqualNI(ty, "Line"   )) m_type = EArrowType::Line;
					else if (pr::str::EqualNI(ty, "Fwd"    )) m_type = EArrowType::Fwd;
					else if (pr::str::EqualNI(ty, "Back"   )) m_type = EArrowType::Back;
					else if (pr::str::EqualNI(ty, "FwdBack")) m_type = EArrowType::FwdBack;
					else { p.ReportError(EResult::UnknownValue, "arrow type must one of Line, Fwd, Back, FwdBack"); return; }
				}
				else
				{
					pr::v4 pt;
					p.m_reader.Vector3(pt, 1.0f);
					m_point.push_back(pt);

					if (m_per_line_colour)
					{
						pr::Colour32 col;
						p.m_reader.Int(col.argb, 16);
						m_color.push_back(col);
					}
				}
			}
			void CreateModel(LdrObject* obj) override
			{
				using namespace pr::rdr;
				
				// Validate
				if (m_point.size() < 2)
				{
					p.ReportError(EResult::Failed, FmtS("Arrow object '%s' description incomplete", obj->TypeAndName().c_str()));
					return;
				}

				// Convert the points into a spline if smooth is specified
				if (m_smooth && m_linestrip)
				{
					auto point = m_point;
					m_point.resize(0);
					pr::Smooth(point, m_point);
				}

				pr::geometry::Props props;

				// Colour interpolation iterator
				auto col = pr::CreateLerpRepeater(m_color.data(), int(m_color.size()), int(m_point.size()), pr::Colour32White);
				auto cc = [&](pr::Colour32 c) { props.m_has_alpha |= HasAlpha(c); return c; };

				// Model bounding box
				auto bb = [&](v4 const& v) { pr::Encompass(props.m_bbox, v); return v; };

				// Generate the model
				// 'm_point' should contain line strip data
				ModelGenerator<>::Cache<> cache(int(m_point.size() + 2), int(m_point.size() + 2));

				auto v_in  = std::begin(m_point);
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
				for (std::size_t i = 0, iend = m_point.size(); i != iend; ++i)
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
				auto id_thk = pr::hash::Hash("LDrawThickLine", m_line_width);
				auto thk_shdr = p.m_rdr.m_shdr_mgr.GetShader<ThickLineListGS>(id_thk, RdrId(EStockShader::ThickLineListGS));
				thk_shdr->m_width = m_line_width;

				auto id_arw = pr::hash::Hash("LDrawArrowHead", m_line_width*2);
				auto arw_shdr = p.m_rdr.m_shdr_mgr.GetShader<ArrowHeadGS>(id_arw, RdrId(EStockShader::ArrowHeadGS));
				arw_shdr->m_size = m_line_width * 2;

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
					nug.m_geometry_has_alpha = cache.m_vcont[0].m_diff.a != 1.0f;
					obj->m_model->CreateNugget(nug);
				}
				{
					vrange = pr::rdr::Range(vrange.m_end, vrange.m_end + m_point.size());
					irange = pr::rdr::Range(irange.m_end, irange.m_end + m_point.size());
					nug.m_topo = EPrim::LineStrip;
					nug.m_geom = EGeom::Vert|EGeom::Colr;
					nug.m_smap[ERenderStep::ForwardRender].m_gs = m_line_width != 0 ? static_cast<ShaderPtr>(thk_shdr) : ShaderPtr();
					nug.m_vrange = vrange;
					nug.m_irange = irange;
					nug.m_geometry_has_alpha = props.m_has_alpha;
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
					nug.m_geometry_has_alpha = cache.m_vcont.back().m_diff.a != 1.0f;
					obj->m_model->CreateNugget(nug);
				}
			}
		};

		// ELdrObject::Matrix3x3
		template <> struct ObjectCreator<ELdrObject::Matrix3x3> :IObjectCreatorLine
		{
			ObjectCreator(ParseParams& p)
				:IObjectCreatorLine(p, false, true)
			{}
			void Parse() override
			{
				pr::m4x4 basis;
				p.m_reader.Matrix3x3(basis.rot);

				pr::v4       pts[] = { pr::v4Origin, basis.x.w1(), pr::v4Origin, basis.y.w1(), pr::v4Origin, basis.z.w1() };
				pr::Colour32 col[] = { pr::Colour32Red, pr::Colour32Red, pr::Colour32Green, pr::Colour32Green, pr::Colour32Blue, pr::Colour32Blue };
				pr::uint16   idx[] = { 0, 1, 2, 3, 4, 5 };

				m_point.insert(m_point.end(), pts, pts + PR_COUNTOF(pts));
				m_color.insert(m_color.end(), col, col + PR_COUNTOF(col));
				m_index.insert(m_index.end(), idx, idx + PR_COUNTOF(idx));
			}
		};

		// ELdrObject::CoordFrame
		template <> struct ObjectCreator<ELdrObject::CoordFrame> :IObjectCreatorLine
		{
			bool m_rh;
			ObjectCreator(ParseParams& p)
				:IObjectCreatorLine(p, false, true)
				,m_rh(true)
			{
				pr::v4       pts[] = { pr::v4Origin, pr::v4XAxis.w1(), pr::v4Origin, pr::v4YAxis.w1(), pr::v4Origin, pr::v4ZAxis.w1() };
				pr::Colour32 col[] = { pr::Colour32Red, pr::Colour32Red, pr::Colour32Green, pr::Colour32Green, pr::Colour32Blue, pr::Colour32Blue };
				pr::uint16   idx[] = { 0, 1, 2, 3, 4, 5 };

				m_point.insert(m_point.end(), pts, pts + PR_COUNTOF(pts));
				m_color.insert(m_color.end(), col, col + PR_COUNTOF(col));
				m_index.insert(m_index.end(), idx, idx + PR_COUNTOF(idx));
			}
			bool ParseKeyword(EKeyword kw) override
			{
				switch (kw)
				{
				default: return IObjectCreatorLine::ParseKeyword(kw);
				case EKeyword::LeftHanded: m_rh = false; return true;
				}
			}
			void Parse() override
			{
				float scale;
				p.m_reader.Real(scale);
				for (auto& pt : m_point)
					pt.xyz *= scale;
			}
			void CreateModel(LdrObject* obj) override
			{
				if (!m_rh)
					m_point[3].xyz = -m_point[3].xyz;
				
				IObjectCreatorLine::CreateModel(obj);
			}
		};

		#pragma endregion

		#pragma region Shapes2d

		// ELdrObject::Circle
		template <> struct ObjectCreator<ELdrObject::Circle> :IObjectCreatorShape2d
		{
			ObjectCreator(ParseParams& p)
				:IObjectCreatorShape2d(p)
			{}
			void Parse() override
			{
				p.m_reader.Int(m_axis_id.value, 10);
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
				using namespace pr::rdr;

				m4x4 o2w, *po2w = nullptr;
				if (m_axis_id != AxisId::PosZ)
				{
					o2w = m4x4::Transform(AxisId::PosZ, m_axis_id, v4Origin);
					po2w = &o2w;
				}

				// Create the model
				obj->m_model = ModelGenerator<>::Ellipse(p.m_rdr, m_dim.x, m_dim.y, m_solid, m_facets, Colour32White, po2w, Material());
				obj->m_model->m_name = obj->TypeAndName();
			}
		};

		// LdrObject::Pie
		template <> struct ObjectCreator<ELdrObject::Pie> :IObjectCreatorShape2d
		{
			v2 m_ang;
			v2 m_rad;

			ObjectCreator(ParseParams& p)
				:IObjectCreatorShape2d(p)
				,m_ang()
				,m_rad()
			{
				m_dim = v4One;
			}
			void Parse() override
			{
				p.m_reader.Int(m_axis_id.value, 10);
				p.m_reader.Vector2(m_ang);
				p.m_reader.Vector2(m_rad);

				m_ang.x = pr::DegreesToRadians(m_ang.x);
				m_ang.y = pr::DegreesToRadians(m_ang.y);
			}
			bool ParseKeyword(EKeyword kw) override
			{
				switch (kw)
				{
				default: return IObjectCreatorShape2d::ParseKeyword(kw);
				case EKeyword::Scale: p.m_reader.Vector2(m_dim.xy); return true;
				}
			}
			void CreateModel(LdrObject* obj) override
			{
				using namespace pr::rdr;

				m4x4 o2w, *po2w = nullptr;
				if (m_axis_id != AxisId::PosZ)
				{
					o2w = m4x4::Transform(AxisId::PosZ, m_axis_id, v4Origin);
					po2w = &o2w;
				}

				// Create the model
				obj->m_model = ModelGenerator<>::Pie(p.m_rdr, m_dim.x, m_dim.y, m_ang.x, m_ang.y, m_rad.x, m_rad.y, m_solid, m_facets, Colour32White, po2w, Material());
				obj->m_model->m_name = obj->TypeAndName();
			}
		};

		// ELdrObject::Rect
		template <> struct ObjectCreator<ELdrObject::Rect> :IObjectCreatorShape2d
		{
			float m_corner_radius;

			ObjectCreator(ParseParams& p)
				:IObjectCreatorShape2d(p)
				,m_corner_radius()
			{}
			void Parse() override
			{
				p.m_reader.Int(m_axis_id.value, 10);
				p.m_reader.Real(m_dim.x);
				if (p.m_reader.IsKeyword() || p.m_reader.IsSectionEnd()) m_dim.y = m_dim.x; else p.m_reader.Real(m_dim.y);
				m_dim *= 0.5f;

				if (Abs(m_dim) != m_dim)
				{
					p.ReportError(EResult::InvalidValue, "Rect dimensions contain a negative value");
				}
			}
			bool ParseKeyword(EKeyword kw) override
			{
				switch (kw)
				{
				default: return IObjectCreatorShape2d::ParseKeyword(kw);
				case EKeyword::CornerRadius: p.m_reader.RealS(m_corner_radius); return true;
				case EKeyword::Facets:       p.m_reader.IntS(m_facets, 10); m_facets *= 4; return true;
				}
			}
			void CreateModel(LdrObject* obj) override
			{
				using namespace pr::rdr;
				m4x4 o2w, *po2w = nullptr;
				if (m_axis_id != AxisId::PosZ)
				{
					o2w = m4x4::Transform(AxisId::PosZ, m_axis_id, v4Origin);
					po2w = &o2w;
				}

				// Create the model
				obj->m_model = ModelGenerator<>::RoundedRectangle(p.m_rdr, m_dim.x, m_dim.y, m_corner_radius, m_solid, m_facets, Colour32White, po2w, Material());
				obj->m_model->m_name = obj->TypeAndName();
			}
		};

		// ELdrObject::Polygon
		template <> struct ObjectCreator<ELdrObject::Polygon> :IObjectCreatorShape2d
		{
			pr::vector<v2> m_verts;
			CCont&         m_colours;
			bool           m_per_line_colour;

			ObjectCreator(ParseParams& p)
				:IObjectCreatorShape2d(p)
				,m_verts()
				,m_colours(p.m_cache.m_color)
				,m_per_line_colour(false)
			{}
			void Parse() override
			{
				p.m_reader.Int(m_axis_id.value, 10);
				for (;p.m_reader.IsValue();)
				{
					v2 pt;
					p.m_reader.Vector2(pt);
					m_verts.push_back(pt);

					if (m_per_line_colour)
					{
						Colour32 col;
						p.m_reader.Int(col.argb, 16);
						m_colours.push_back(col);
					}
				}
			}
			bool ParseKeyword(EKeyword kw) override
			{
				switch (kw)
				{
				default: return IObjectCreatorShape2d::ParseKeyword(kw);
				case EKeyword::Coloured: m_per_line_colour = true; return true;
				}
			}
			void CreateModel(LdrObject* obj) override
			{
				using namespace pr::rdr;
				m4x4 o2w, *po2w = nullptr;
				if (m_axis_id != AxisId::PosZ)
				{
					o2w = m4x4::Transform(AxisId::PosZ, m_axis_id, v4Origin);
					po2w = &o2w;
				}

				// Create the model
				obj->m_model = ModelGenerator<>::Polygon(p.m_rdr, int(m_verts.size()), m_verts.data(), m_solid, int(m_colours.size()), m_colours.data(), po2w, Material());
				obj->m_model->m_name = obj->TypeAndName();
			}
		};

		// ELdrObject::Triangle
		template <> struct ObjectCreator<ELdrObject::Triangle> :IObjectCreatorPlane
		{
			ObjectCreator(ParseParams& p)
				:IObjectCreatorPlane(p)
			{}
			void Parse() override
			{
				pr::v4 pt[3]; pr::Colour32 col[3];
				p.m_reader.Vector3(pt[0], 1.0f) && (!m_per_vert_colour || p.m_reader.Int(col[0].argb, 16));
				p.m_reader.Vector3(pt[1], 1.0f) && (!m_per_vert_colour || p.m_reader.Int(col[1].argb, 16));
				p.m_reader.Vector3(pt[2], 1.0f) && (!m_per_vert_colour || p.m_reader.Int(col[2].argb, 16));
				m_point.push_back(pt[0]);
				m_point.push_back(pt[1]);
				m_point.push_back(pt[2]);
				m_point.push_back(pt[2]); // create a degenerate
				if (m_per_vert_colour)
				{
					m_color.push_back(col[0]);
					m_color.push_back(col[1]);
					m_color.push_back(col[2]);
					m_color.push_back(col[2]);
				}
			}
		};

		// ELdrObject::Quad
		template <> struct ObjectCreator<ELdrObject::Quad> :IObjectCreatorPlane
		{
			ObjectCreator(ParseParams& p)
				:IObjectCreatorPlane(p)
			{}
			void Parse() override
			{
				pr::v4 pt[4]; pr::Colour32 col[4];
				p.m_reader.Vector3(pt[0], 1.0f) && (!m_per_vert_colour || p.m_reader.Int(col[0].argb, 16));
				p.m_reader.Vector3(pt[1], 1.0f) && (!m_per_vert_colour || p.m_reader.Int(col[1].argb, 16));
				p.m_reader.Vector3(pt[2], 1.0f) && (!m_per_vert_colour || p.m_reader.Int(col[2].argb, 16));
				p.m_reader.Vector3(pt[3], 1.0f) && (!m_per_vert_colour || p.m_reader.Int(col[3].argb, 16));
				m_point.push_back(pt[0]);
				m_point.push_back(pt[1]);
				m_point.push_back(pt[2]);
				m_point.push_back(pt[3]);
				if (m_per_vert_colour)
				{
					m_color.push_back(col[0]);
					m_color.push_back(col[1]);
					m_color.push_back(col[2]);
					m_color.push_back(col[3]);
				}
			}
		};

		// ELdrObject::Plane
		template <> struct ObjectCreator<ELdrObject::Plane> :IObjectCreatorPlane
		{
			ObjectCreator(ParseParams& p)
				:IObjectCreatorPlane(p)
			{}
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
				m_point.push_back(pnt - up - left);
				m_point.push_back(pnt - up + left);
				m_point.push_back(pnt + up - left);
				m_point.push_back(pnt + up + left);
			}
		};

		// ELdrObject::Ribbon
		template <> struct ObjectCreator<ELdrObject::Ribbon> :IObjectCreatorPlane
		{
			AxisId m_axis_id;
			float  m_width;
			bool   m_smooth;
			int    m_parm_index;

			ObjectCreator(ParseParams& p)
				:IObjectCreatorPlane(p)
				,m_axis_id(AxisId::PosZ)
				,m_width(10.0f)
				,m_smooth(false)
				,m_parm_index(0)
			{}
			void Parse() override
			{
				switch (m_parm_index){
				case 0: // Axis id
					p.m_reader.Int(m_axis_id.value, 10);
					++m_parm_index;
					break;
				case 1: // Width
					p.m_reader.Real(m_width);
					++m_parm_index;
					break;
				case 2: // Points
					pr::v4 pt;
					p.m_reader.Vector3(pt, 1.0f);
					m_point.push_back(pt);

					if (m_per_vert_colour)
					{
						pr::Colour32 col;
						p.m_reader.Int(col.argb, 16);
						m_color.push_back(col);
					}
					break;
				}
			}
			bool ParseKeyword(EKeyword kw) override
			{
				switch (kw){
				default: return IObjectCreatorPlane::ParseKeyword(kw);
				case EKeyword::Smooth: m_smooth = true; return true;
				}
			}
			void CreateModel(LdrObject* obj) override
			{
				using namespace pr::rdr;

				// Validate
				if (m_point.size() < 2)
				{
					p.ReportError(EResult::Failed, "Object description incomplete");
					return;
				}

				// Smooth the points
				if (m_smooth)
				{
					auto points = m_point;
					m_point.resize(0);
					pr::Smooth(points, m_point);
				}

				pr::v4 normal = m_axis_id;
				obj->m_model = ModelGenerator<>::QuadStrip(p.m_rdr, int(m_point.size() - 1), m_point.data(), m_width, 1, &normal, int(m_color.size()), m_color.data(), Material());
				obj->m_model->m_name = obj->TypeAndName();
			}
		};

		#pragma endregion

		#pragma region Shapes3d

		// ELdrObject::Box
		template <> struct ObjectCreator<ELdrObject::Box> :IObjectCreatorTexture
		{
			v4 m_dim;

			ObjectCreator(ParseParams& p)
				:IObjectCreatorTexture(p)
				,m_dim()
			{}
			void Parse() override
			{
				p.m_reader.Real(m_dim.x);
				if (p.m_reader.IsKeyword() || p.m_reader.IsSectionEnd()) m_dim.y = m_dim.x; else p.m_reader.Real(m_dim.y);
				if (p.m_reader.IsKeyword() || p.m_reader.IsSectionEnd()) m_dim.z = m_dim.y; else p.m_reader.Real(m_dim.z);
				m_dim *= 0.5f;
			}
			void CreateModel(LdrObject* obj) override
			{
				// Create the model
				obj->m_model = ModelGenerator<>::Box(p.m_rdr, m_dim, pr::m4x4Identity, pr::Colour32White, Material());
				obj->m_model->m_name = obj->TypeAndName();
			}
		};

		// ELdrObject::BoxLine
		template <> struct ObjectCreator<ELdrObject::BoxLine> :IObjectCreatorTexture
		{
			m4x4 m_b2w;
			v4 m_dim, m_up;

			ObjectCreator(ParseParams& p)
				:IObjectCreatorTexture(p)
				,m_b2w()
				,m_dim()
				,m_up(v4YAxis)
			{}
			void Parse() override
			{
				float w = 0.1f, h = 0.1f;
				v4 s0, s1;
				p.m_reader.Vector3(s0, 1.0f);
				p.m_reader.Vector3(s1, 1.0f);
				p.m_reader.Real(w);
				if (p.m_reader.IsKeyword() || p.m_reader.IsSectionEnd()) h = w; else p.m_reader.Real(h);
				m_dim = v4(w, h, pr::Length3(s1 - s0), 0.0f) * 0.5f;
				m_b2w = pr::OriFromDir(s1 - s0, 2, m_up, (s1 + s0) * 0.5f);
			}
			bool ParseKeyword(EKeyword kw) override
			{
				switch (kw) {
				default: return IObjectCreatorTexture::ParseKeyword(kw);
				case EKeyword::Up: p.m_reader.Vector3S(m_up, 0.0f); return true;
				}
			}
			void CreateModel(LdrObject* obj) override
			{
				obj->m_model = ModelGenerator<>::Box(p.m_rdr, m_dim, m_b2w, Colour32White, Material());
				obj->m_model->m_name = obj->TypeAndName();
			}
		};

		// ELdrObject::BoxList
		template <> struct ObjectCreator<ELdrObject::BoxList> :IObjectCreatorTexture
		{
			pr::vector<v4,16> m_location;
			v4 m_dim;

			ObjectCreator(ParseParams& p)
				:IObjectCreatorTexture(p)
				,m_location()
				,m_dim()
			{}
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
				obj->m_model = ModelGenerator<>::BoxList(p.m_rdr, int(m_location.size()), m_location.data(), m_dim, 0, 0, Material());
				obj->m_model->m_name = obj->TypeAndName();
			}
		};

		// ELdrObject::FrustumWH
		template <> struct ObjectCreator<ELdrObject::FrustumWH> :IObjectCreatorCuboid
		{
			float m_width, m_height, m_near, m_far, m_view_plane;
			AxisId m_axis_id;

			ObjectCreator(ParseParams& p)
				:IObjectCreatorCuboid(p)
				,m_width(1.0f)
				,m_height(1.0f)
				,m_near(0.0f)
				,m_far(1.0f)
				,m_view_plane(1.0f)
				,m_axis_id(AxisId::PosZ)
			{}
			void Parse() override
			{
				p.m_reader.Int(m_axis_id.value, 10);
				p.m_reader.Real(m_width);
				p.m_reader.Real(m_height);
				p.m_reader.Real(m_near);
				p.m_reader.Real(m_far);
			}
			bool ParseKeyword(EKeyword kw) override
			{
				switch (kw) {
				default: return IObjectCreatorCuboid::ParseKeyword(kw);
				case EKeyword::ViewPlaneZ: p.m_reader.RealS(m_view_plane); return true;
				}
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

				m_b2w = m4x4::Transform(AxisId::PosZ, m_axis_id, v4Origin);

				IObjectCreatorCuboid::CreateModel(obj);
			}
		};

		// ELdrObject::FrustumFA
		template <> struct ObjectCreator<ELdrObject::FrustumFA> :IObjectCreatorCuboid
		{
			float m_fovY, m_aspect, m_near, m_far;
			AxisId m_axis_id;

			ObjectCreator(ParseParams& p)
				:IObjectCreatorCuboid(p)
				,m_fovY(float(maths::tau_by_8))
				,m_aspect(1.0f)
				,m_near(0.0f)
				,m_far(1.0f)
				,m_axis_id(AxisId::PosZ)
			{}
			void Parse() override
			{
				p.m_reader.Int(m_axis_id.value, 10);
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

				m_b2w = m4x4::Transform(AxisId::PosZ, m_axis_id, v4Origin);

				IObjectCreatorCuboid::CreateModel(obj);
			}
		};

		// ELdrObject::Sphere
		template <> struct ObjectCreator<ELdrObject::Sphere> :IObjectCreatorTexture
		{
			v4 m_dim;
			int m_divisions;

			ObjectCreator(ParseParams& p)
				:IObjectCreatorTexture(p)
				,m_dim()
				,m_divisions(3)
			{}
			void Parse() override
			{
				p.m_reader.Real(m_dim.x);
				if (p.m_reader.IsKeyword() || p.m_reader.IsSectionEnd()) m_dim.y = m_dim.x; else p.m_reader.Real(m_dim.y);
				if (p.m_reader.IsKeyword() || p.m_reader.IsSectionEnd()) m_dim.z = m_dim.y; else p.m_reader.Real(m_dim.z);
			}
			bool ParseKeyword(EKeyword kw) override
			{
				switch (kw) {
				default: return IObjectCreatorTexture::ParseKeyword(kw);
				case EKeyword::Divisions: p.m_reader.Int(m_divisions, 10); return true;
				}
			}
			void CreateModel(LdrObject* obj) override
			{
				obj->m_model = ModelGenerator<>::Geosphere(p.m_rdr, m_dim, m_divisions, Colour32White, Material());
				obj->m_model->m_name = obj->TypeAndName();
			}
		};

		// ELdrObject::CylinderHR
		template <> struct ObjectCreator<ELdrObject::CylinderHR> :IObjectCreatorCone
		{
			ObjectCreator(ParseParams& p)
				:IObjectCreatorCone(p)
			{}
			void Parse() override
			{
				p.m_reader.Int(m_axis_id.value, 10);
				p.m_reader.Real(m_dim.z);
				p.m_reader.Real(m_dim.x);
				if (p.m_reader.IsKeyword() || p.m_reader.IsSectionEnd()) m_dim.y = m_dim.x; else p.m_reader.Real(m_dim.y);
			}
		};

		// ELdrObject::ConeHA
		template <> struct ObjectCreator<ELdrObject::ConeHA> :IObjectCreatorCone
		{
			ObjectCreator(ParseParams& p)
				:IObjectCreatorCone(p)
			{}
			void Parse() override
			{
				float h0, h1, a;
				p.m_reader.Int(m_axis_id.value, 10);
				p.m_reader.Real(h0);
				p.m_reader.Real(h1);
				p.m_reader.Real(a);

				m_dim.z = h1 - h0;
				m_dim.x = h0 * Tan(a);
				m_dim.y = h1 * Tan(a);
			}
		};

		// ELdrObject::Tube
		template <> struct ObjectCreator<ELdrObject::Tube> :IObjectCreatorLine
		{
			string32       m_type;         // Cross section type
			pr::vector<v2> m_cs;           // 2d cross section
			float          m_radx, m_rady; // X,Y radii for implicit cross sections
			int            m_facets;       // The number of divisions for Round cross sections
			bool           m_closed;       // True if the tube end caps should be filled in
			bool           m_smooth_cs;    // True if outward normals for the tube are smoothed

			ObjectCreator(ParseParams& p)
				:IObjectCreatorLine(p, false, false)
				,m_type()
				,m_cs()
				,m_radx()
				,m_rady()
				,m_facets(20)
				,m_closed(false)
				,m_smooth_cs(false)
			{}
			void Parse() override
			{
				// Parse the extrusion path
				v4 pt; Colour32 col;
				p.m_reader.Vector3(pt, 1.0f);
				if (m_per_line_colour) p.m_reader.Int(col.argb, 16);
				
				// Ignore degenerates
				if (m_point.empty() || !FEql(m_point.back(), pt))
				{
					m_point.push_back(pt);
					if (m_per_line_colour) m_color.push_back(col);
				}
			}
			bool ParseKeyword(EKeyword kw) override
			{
				switch (kw)
				{
				default: return IObjectCreatorLine::ParseKeyword(kw);
				case EKeyword::Style:
					{
						// Expect *Style { cross_section_type <data> }
						p.m_reader.SectionStart();

						// Determine the cross section type
						p.m_reader.Identifier(m_type);

						// Create the cross section profile based on the style
						if (pr::str::EqualI(m_type, "Round"))
						{
							// Elliptical cross section, expect 1 or 2 radii to follow
							p.m_reader.Real(m_radx);
							if (p.m_reader.IsValue())
								p.m_reader.Real(m_rady);
							else
								m_rady = m_radx;

							m_smooth_cs = true;
						}
						else if (pr::str::EqualI(m_type, "Square"))
						{
							// Square cross section, expect 1 or 2 radii to follow
							p.m_reader.Real(m_radx);
							if (p.m_reader.IsValue())
								p.m_reader.Real(m_rady);
							else
								m_rady = m_radx;

							m_smooth_cs = false;
						}
						else if (pr::str::EqualI(m_type, "CrossSection"))
						{
							// Create the cross section, expect X,Y pairs
							for (;p.m_reader.IsValue();)
							{
								v2 pt;
								p.m_reader.Vector2(pt);
								m_cs.push_back(pt);
							}
						}
						else
						{
							p.ReportError(EResult::UnknownValue, FmtS("Cross Section type %s is not supported", m_type.c_str()));
							//p.m_reader.FindSectionEnd();
							//m_type.clear();
						}

						// Optional parameters
						for (EKeyword kw0; p.m_reader.NextKeywordH(kw0);)
						{
							switch (kw0) {
							case EKeyword::Facets: p.m_reader.IntS(m_facets, 10); break;
							case EKeyword::Smooth: m_smooth_cs = true; break;
							}
						}

						p.m_reader.SectionEnd();
						return true;
					}
				case EKeyword::Closed:
					{
						m_closed = true;
						return true;
					}
				}
			}
			void CreateModel(LdrObject* obj) override
			{
				// If no cross section or extrusion path is given
				if (m_point.empty())
				{
					p.ReportError(EResult::Failed, pr::FmtS("Tube object '%s' description incomplete. No extrusion path", obj->TypeAndName().c_str()));
					return;
				}

				// Create the cross section for implicit profiles
				if (pr::str::EqualI(m_type, "Round"))
				{
					for (auto i = 0; i != m_facets; ++i)
						m_cs.push_back(v2(m_radx * Cos(float(maths::tau) * i / m_facets), m_rady * Sin(float(maths::tau) * i / m_facets)));
				}
				else if (pr::str::EqualI(m_type, "Square"))
				{
					// Create the cross section
					m_cs.push_back(v2(-m_radx, -m_rady));
					m_cs.push_back(v2(+m_radx, -m_rady));
					m_cs.push_back(v2(+m_radx, +m_rady));
					m_cs.push_back(v2(-m_radx, +m_rady));
				}
				else if (pr::str::EqualI(m_type, "CrossSection"))
				{
					if (m_cs.empty())
					{
						p.ReportError(EResult::Failed, pr::FmtS("Tube object '%s' description incomplete", obj->TypeAndName().c_str()));
						return;
					}
					if (pr::geometry::PolygonArea(m_cs.data(), int(m_cs.size())) < 0)
					{
						p.ReportError(EResult::Failed, pr::FmtS("Tube object '%s' cross section has a negative area (winding order is incorrect)", obj->TypeAndName().c_str()));
						return;
					}
				}
				else
				{
					p.ReportError(EResult::Failed, pr::FmtS("Tube object '%s' description incomplete. No style given.", obj->TypeAndName().c_str()));
					return;
				}

				// Smooth the tube centre line
				if (m_smooth)
				{
					auto points = m_point;
					m_point.resize(0);
					pr::Smooth(points, m_point);
				}

				obj->m_model = ModelGenerator<>::Extrude(p.m_rdr, int(m_cs.size()), m_cs.data(), int(m_point.size()), m_point.data(), m_closed, m_smooth_cs, int(m_color.size()), m_color.data());
				obj->m_model->m_name = obj->TypeAndName();
			}
		};

		// ELdrObject::Mesh
		template <> struct ObjectCreator<ELdrObject::Mesh> :IObjectCreatorMesh
		{
			ObjectCreator(ParseParams& p)
				:IObjectCreatorMesh(p)
			{}
			void Parse() override
			{
				p.ReportError(EResult::UnknownValue, "Mesh object description invalid");
			}
		};

		// ELdrObject::ConvexHull
		template <> struct ObjectCreator<ELdrObject::ConvexHull> :IObjectCreatorMesh
		{
			ObjectCreator(ParseParams& p)
				:IObjectCreatorMesh(p)
			{
				m_gen_normals = 0.0f;
			}
			void Parse() override
			{
				p.ReportError(EResult::UnknownValue, "Convex hull object description invalid");
			}
			void CreateModel(LdrObject* obj) override
			{
				// Allocate space for the face indices
				m_indices.resize(6 * (m_verts.size() - 2));

				// Find the convex hull
				size_t num_verts = 0, num_faces = 0;
				pr::ConvexHull(m_verts, m_verts.size(), &m_indices[0], &m_indices[0] + m_indices.size(), num_verts, num_faces);
				m_verts.resize(num_verts);
				m_indices.resize(3 * num_faces);

				// Create a nugget for the hull
				auto nug = *Material();
				nug.m_topo = EPrim::TriList;
				nug.m_geom = EGeom::Vert;
				m_nuggets.push_back(nug);

				IObjectCreatorMesh::CreateModel(obj);
			}
		};

		// ELdrObject::Chart
		template <> struct ObjectCreator<ELdrObject::Chart> :IObjectCreator
		{
			using Column = pr::vector<float>;
			using Table  = pr::vector<Column>;

			AxisId m_axis_id;
			Table m_table;
			CCont m_colours;
			int m_xcolumn;
			float m_width;
			optional<float> m_x0;
			optional<float> m_y0;

			ObjectCreator(ParseParams& p)
				:IObjectCreator(p)
				,m_axis_id(AxisId::PosZ)
				,m_table()
				,m_colours()
				,m_xcolumn(0)
				,m_width(0)
				,m_x0()
				,m_y0()
			{}
			void Parse() override
			{
				// Read the axis id
				p.m_reader.Int(m_axis_id.value, 10);

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
			bool ParseKeyword(EKeyword kw) override
			{
				switch (kw) {
				default: return IObjectCreator::ParseKeyword(kw);
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
					p.ReportError(EResult::Failed, pr::FmtS("Chart object '%s', X axis column does not exist", obj->TypeAndName().c_str()));
					return;
				}

				// Get the rotation for axis id
				auto rot = m3x4::Rotation(AxisId::PosZ, m_axis_id);

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
						verts.push_back(m_axis_id != AxisId::PosZ ? rot * vert : vert);
						lines.push_back(static_cast<pr::uint16>(ibase + i));
						colrs.push_back(colour);
					}

					// Create a nugget for the line strip
					NuggetProps nug(EPrim::LineStrip, EGeom::Vert|EGeom::Colr, nullptr, vrange, irange);
					if (m_width != 0.0f)
					{
						// Use thick lines
						auto id = pr::hash::Hash("LDrawThickLine", m_width);
						auto shdr = p.m_rdr.m_shdr_mgr.GetShader<ThickLineListGS>(id, RdrId(EStockShader::ThickLineListGS));
						shdr->m_width = m_width;
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
			wstring256 m_filepath;
			m4x4 m_bake;
			int m_part;
			float m_gen_normals;

			ObjectCreator(ParseParams& p)
				:IObjectCreator(p)
				,m_filepath()
				,m_bake(m4x4Identity)
				,m_gen_normals(-1.0f)
			{}
			void Parse() override
			{
				p.m_reader.String(m_filepath);
			}
			bool ParseKeyword(EKeyword kw) override
			{
				switch (kw) {
				default: return IObjectCreator::ParseKeyword(kw);
				case EKeyword::Part:
					{
						p.m_reader.IntS(m_part, 10);
						return true;
					}
				case EKeyword::GenerateNormals:
					{
						p.m_reader.RealS(m_gen_normals);
						m_gen_normals = pr::DegreesToRadians(m_gen_normals);
						return true;
					}
				case EKeyword::BakeTransform:
					{
						p.m_reader.TransformS(m_bake);
						return true;
					}
				}
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
				auto format = GetModelFormat(m_filepath.c_str());
				if (format == EModelFileFormat::Unknown)
				{
					auto msg = pr::Fmt("Model file '%s' is not supported.\nSupported Formats: ", Narrow(m_filepath).c_str());
					for (auto f : Enum<EModelFileFormat>::Members()) msg.append(ToStringA(f)).append(" ");
					p.ReportError(EResult::Failed, msg.c_str());
					return;
				}

				// Ask the include handler to turn the filepath into a stream.
				// Load the stream in binary model. The model loading functions can convert binary to text if needed.
				auto src = p.m_reader.Includes().OpenStreamA(m_filepath, IIncludeHandler::EFlags::Binary);
				if (!src || !*src)
				{
					p.ReportError(EResult::Failed, pr::FmtS("Failed to open file stream '%s'", m_filepath.c_str()));
					return;
				}

				// Create the model
				obj->m_model = ModelGenerator<>::LoadModel(p.m_rdr, format, *src, nullptr, m_bake != m4x4Identity ? &m_bake : nullptr, m_gen_normals);
				obj->m_model->m_name = obj->TypeAndName();
			}
		};

		#pragma endregion

		#pragma region Special Objects

		// ELdrObject::DirLight
		template <> struct ObjectCreator<ELdrObject::DirLight> :IObjectCreatorLight
		{
			ObjectCreator(ParseParams& p)
				:IObjectCreatorLight(p)
			{}
			void Parse() override
			{
				p.m_reader.Vector3(m_light.m_direction, 0.0f);
			}
		};

		// ELdrObject::PointLight
		template <> struct ObjectCreator<ELdrObject::PointLight> :IObjectCreatorLight
		{
			ObjectCreator(ParseParams& p)
				:IObjectCreatorLight(p)
			{}
			void Parse() override
			{
				p.m_reader.Vector3(m_light.m_position, 1.0f);
			}
		};

		// ELdrObject::SpotLight
		template <> struct ObjectCreator<ELdrObject::SpotLight> :IObjectCreatorLight
		{
			ObjectCreator(ParseParams& p)
				:IObjectCreatorLight(p)
			{}
			void Parse() override
			{
				p.m_reader.Vector3(m_light.m_position, 1.0f);
				p.m_reader.Vector3(m_light.m_direction, 0.0f);
				p.m_reader.Real(m_light.m_inner_cos_angle); // actually in degrees
				p.m_reader.Real(m_light.m_outer_cos_angle); // actually in degrees
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

			wstring256     m_text;
			EType          m_type;
			TextFmtCont    m_fmt;
			TextLayout     m_layout;
			AxisId         m_axis_id;

			ObjectCreator(ParseParams& p)
				:IObjectCreator(p)
				,m_text()
				,m_type(EType::Full3D)
				,m_fmt()
				,m_layout()
				,m_axis_id(AxisId::PosZ)
			{}
			void Parse() override
			{
				wstring256 text;
				p.m_reader.String(text);
				m_text.append(text);

				// Record the formatting state
				m_fmt.push_back(TextFormat(int(m_text.size() - text.size()), int(text.size()), p.m_font.back()));
			}
			bool ParseKeyword(EKeyword kw) override
			{
				switch (kw)
				{
				default: return IObjectCreator::ParseKeyword(kw);
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
				case EKeyword::Axis:
					{
						p.m_reader.IntS(m_axis_id.value, 10);
						return true;
					}
				}
			}
			void CreateModel(LdrObject* obj) override
			{
				// Create a quad containing the text
				obj->m_model = ModelGenerator<>::Text(p.m_rdr, m_text, m_fmt.data(), int(m_fmt.size()), m_layout, m_axis_id);
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
				auto model_key = Hash(obj->m_name.c_str());
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
		// Note: not using an output iterator style callback because model instancing relies on the map from object to model.
		template <ELdrObject ShapeType> void Parse(ParseParams& p)
		{
			// Read the object attributes: name, colour, instance
			auto attr = ParseAttributes(p.m_reader, ShapeType);
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
			p.m_models[Hash(obj->m_name.c_str())] = obj->m_model;
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
			case ELdrObject::BoxLine:    Parse<ELdrObject::BoxLine   >(p); break;
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
		// 'add_cb' is 'bool function(int object_index, ParseResult& out, pr::script::Location const& loc)'
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
							p.m_reader.ReportError(pr::script::EResult::UnknownToken);
							break;
						}
						assert("Objects removed but 'ParseLdrObject' didn't fail" && int(p.m_objects.size()) > object_count);

						// Call the callback with the freshly minted object.
						add_cb(object_count);
						break;
					}

				// Camera position description
				case EKeyword::Camera:
					{
						ParseCamera(p.m_reader, p.m_result);
						break;
					}

				// Application commands
				case EKeyword::Clear:
					{
						// Clear resets the scene up to the point of the clear, that includes
						// objects we may have already parsed. A use for this is for a script
						// that might be a work in progress, *Clear can be used to remove everything
						// above a point in the script.
						p.m_objects.clear();
						p.m_result.m_clear = true;
						break;
					}
				case EKeyword::AllowMissingIncludes:
					{
						p.m_reader.Includes().m_ignore_missing_includes = true;
						break;
					}
				case EKeyword::Dependency:
					{
						wstring256 dep;
						p.m_reader.String(dep);

						// Get the include handler to open the file, this triggers the FileOpened event allowing
						// the file to be seen as a dependency, without actually using the file content.
						auto inc = p.m_reader.Includes().Open(dep, IIncludeHandler::EFlags::IncludeLocalDir);
					}
				case EKeyword::Wireframe:
					{
						p.m_result.m_wireframe = true;
						break;
					}
				case EKeyword::Font:
					{
						ParseFont(p.m_reader, p.m_font.back());
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
		void Parse(pr::Renderer& rdr, pr::script::Reader& reader, ParseResult& out, Guid const& context_id, ParseProgressCB progress_cb)
		{
			// Give initial and final progress updates
			auto start_loc = reader.Loc();
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
		LdrObjectPtr Create(pr::Renderer& rdr, ObjectAttributes attr, MeshCreationData const& cdata, pr::Guid const& context_id)
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
		LdrObjectPtr CreateEditCB(pr::Renderer& rdr, ObjectAttributes attr, int vcount, int icount, int ncount, EditObjectCB edit_cb, void* ctx, pr::Guid const& context_id)
		{
			LdrObjectPtr obj(new LdrObject(attr, 0, context_id), true);

			// Create buffers for a dynamic model
			VBufferDesc vbs(vcount, sizeof(Vert), EUsage::Dynamic, ECPUAccess::Write);
			IBufferDesc ibs(icount, sizeof(pr::uint16), DxFormat<pr::uint16>::value, EUsage::Dynamic, ECPUAccess::Write);
			MdlSettings settings(vbs, ibs);

			// Create the model
			obj->m_model = rdr.m_mdl_mgr.CreateModel(settings);
			obj->m_model->m_name = obj->TypeAndName();

			// Create dummy nuggets
			pr::rdr::NuggetProps nug(EPrim::PointList, EGeom::Vert);
			nug.m_range_overlaps = true;
			for (int i = ncount; i-- != 0;)
				obj->m_model->CreateNugget(nug);

			// Initialise it via the callback
			edit_cb(obj->m_model.get(), ctx, rdr);
			return obj;
		}

		// Modify the geometry of an LdrObject
		void Edit(pr::Renderer& rdr, LdrObject* object, EditObjectCB edit_cb, void* ctx)
		{
			edit_cb(object->m_model.get(), ctx, rdr);
			pr::events::Send(Evt_LdrObjectChg(object));
		}

		// Update 'object' with info from 'reader'. 'flags' describes the properties of 'object' to update
		void Update(pr::Renderer& rdr, LdrObject* object, pr::script::Reader& reader, EUpdateObject flags)
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

			pr::events::Send(Evt_LdrObjectChg(object));
		}

		// Remove all objects from 'objects' that have a context id matching one in 'doomed' and not in 'excluded'
		// If 'doomed' is 0, all are assumed doomed. If 'excluded' is 0, none are assumed excluded
		// 'excluded' is considered after 'doomed' so if any context ids are in both arrays, they will be excluded.
		void Remove(ObjectCont& objects, pr::Guid const* doomed, std::size_t dcount, pr::Guid const* excluded, std::size_t ecount)
		{
			auto incl = std::initializer_list<pr::Guid>(doomed, doomed + dcount);
			auto excl = std::initializer_list<pr::Guid>(excluded, excluded + ecount);
			pr::erase_if_unstable(objects, [=](auto& ob)
			{
				if (doomed   && !pr::contains(incl, ob->m_context_id)) return false; // not in the doomed list
				if (excluded &&  pr::contains(excl, ob->m_context_id)) return false; // saved by exclusion
				return true;
			});
		}

		// Remove 'obj' from 'objects'
		void Remove(ObjectCont& objects, LdrObject* obj)
		{
			pr::erase_first_unstable(objects, [=](auto& ob){ return ob == obj; });
		}

		// Generate a scene that demos the supported object types and modifiers.
		std::wstring CreateDemoScene()
		{
			return
			#pragma region Demo Scene
LR"(//********************************************
// LineDrawer demo scene
//  Copyright (c) Rylogic Ltd 2009
//********************************************
//
// Notes:
//  axis_id is an integer describing an axis number. It must one of ±1, ±2, ±3
//  corresponding to ±X, ±Y, ±Z respectively

// Clear existing data
*Clear /*{ctx_id ...}*/ // Context ids can be listed within a section

// Allow missing includes not to cause errors
*AllowMissingIncludes
#include "missing_file.ldr"

// Add an explicit dependency on another file - useful for auto refresh
//*Dependency "..\dir\shader_test.hlsl"

// Object descriptions have the following format:
//	*ObjectType [name] [colour] [instance]
//	{
//		...
//	}
//	The name, colour, and instance parameters are optional and have defaults of
//		name     = 'ObjectType'
//		colour   = FFFFFFFF
//		instance = true (described below)
*Box {1 2 3}

)"
LR"(
// An example of applying a transform to an object.
// All objects have an implicit object-to-parent transform that is identity.
// Successive 'o2w' sections pre-multiply this transform for the object.
// Fields within the 'o2w' section are applied in the order they are specified.
*Box o2w_example FF00FF00
{
	2 3 1
	*o2w
	{
		// An empty 'o2w' is equivalent to an identity transform
		*M4x4 {1 0 0 0  0 1 0 0  0 0 1 0  0 0 0 1}    // {xx xy xz xw  yx yy yz yw  zx zy zz zw  wx wy wz ww} - i.e. row major
		*M3x3 {1 0 0  0 1 0  0 0 1}                   // {xx xy xz  yx yy yz  zx zy zz} - i.e. row major
		*Pos {0 1 0}                                  // {x y z}
		*Align {3 0 1 0}                              // {axis_id dx dy dz } - direction vector, and axis id to align to that direction
		*Quat {0 #eval{sin(pi/2)} 0 #eval{cos(pi/2)}} // {x y z s} - quaternion
		*QuatPos {0 1 0 0  1 -2 3}                    // {q.x q.y q.z q.s p.x p.y p.z} - quaternion position
		*Rand4x4 {0 1 0 2}                            // {cx cy cz r} - centre position, radius. Random orientation
		*RandPos {0 1 0 2}                            // {cx cy cz r} - centre position, radius
		*RandOri                                      // Randomises the orientation of the current transform
		*Scale {1 1.2 1}                              // { sx sy sz } - multiples the lengths of x,y,z vectors of the current transform. Accepts 1 or 3 values
		*Normalise                                    // Normalises the lengths of the vectors of the current transform
		*Orthonormalise                               // Normalises the lengths and makes orthogonal the vectors of the current transform
		*Transpose *Transpose                         // Transposes the current transform
		*Inverse *Inverse                             // Inverts the current transform
		*Euler {45 30 60}                             // { pitch yaw roll } - all in degrees. Order of rotations is roll, pitch, yaw
	}
}

)"
LR"(// There are a number of other object modifiers that can also be used:
*Box obj_modifier_example FFFF0000
{
	0.2 0.5 0.4
	*Colour {FFFF00FF}       // Override the base colour of the model
	*ColourMask {FF000000}   // applies: 'child.colour = (obj.base_colour & mask) | (child.base_colour & ~mask)' to all children recursively
	*RandColour              // Apply a random colour to this object
	*Animation               // Add simple animation to this object
	{
		*Style PingPong        // Animation style, one of: NoAnimation, PlayOnce, PlayReverse, PingPong, PlayContinuous
		*Period { 1.2 }        // The period of the animation in seconds
		*Velocity { 1 1 1 }    // Linear velocity vector in m/s
		*AngVelocity { 1 0 0 } // Angular velocity vector in rad/s
	}
	*Hidden                  // Object is created in an invisible state
	*Wireframe               // Object is created with wireframe fill mode
	*Texture                 // Texture (only supported on some object types)
	{
		"#checker"          // texture filepath, stock texture name (e.g. #white, #black, #checker), or texture id (e.g. #1, #3)
		*Addr {Clamp Clamp} // Optional addressing mode for the texture; U, V. Options: Wrap, Mirror, Clamp, Border, MirrorOnce
		*Filter {Linear}    // Optional filtering of the texture. Options: Point, Linear, Anisotropic
		*Alpha              // Optional. Indicates the texture contains alpha pixels
		*o2w                // Optional 3d texture coord transform
		{
			*scale{100 100 1}
			*euler{0 0 90}
		}
	}
	//*ScreenSpace    // If specified, the object's *o2w transform is interpreted as a screen space position/orientation
	                  // Screen space coordinates are in the volume:
	                  //  (-1,-1,-0) = bottom, left, near plane
	                  //  (+1,+1,-1) = top, right, far plane
	//*NoZWrite  // Don't write to the Z buffer
	//*NoZTest   // Don't depth-text
}

)"
LR"(// Model Instancing.
// An instance can be created from any previously defined object. The instance will
// share the renderable model from the object it is an instance of.
// Note that properties of the object are not inherited by the instance.
*Box model_instancing FF0000FF // Define a model to be used only for instancing
{
	0.8 1 2
	*RandColour // Note: this will not be inherited by the instances
	*Hidden     // Hide this object, only show the instances
}
*Instance model_instancing FFFF0000   // The name indicates which model to instance
{
	*o2w {*Pos {5 0 -2}}
}
*Instance model_instancing FF0000FF
{
	*o2w {*Pos {-4 0.5 0.5}}
}

)"
LR"(// Object Nesting.
// Nested objects are given in the space of their parent so a parent transform is applied to all children
*Box nesting_example1 80FFFF00
{
	0.4 0.7 0.3
	*o2w {*pos {0 3 0} *randori}
	*ColourMask { FF000000 }
	*Box nested1_1 FF00FFFF
	{
		0.4 0.7 0.3
		*o2w {*pos {1 0 0} *randori}
		*Box nested1_2 FF00FFFF
		{
			0.4 0.7 0.3
			*o2w {*pos {1 0 0} *randori}
			*Box nested1_3 FF00FFFF
			{
				0.4 0.7 0.3
				*o2w {*pos {1 0 0} *randori}
			}
		}
	}
}
*Box nesting_example2 FFFFFF00
{
	0.4 0.7 0.3
	*o2w {*pos {0 -3 0} *randori}
	*Box nested2_1 FF00FFFF
	{
		0.4 0.7 0.3
		*o2w {*pos {1 0 0} *randori}
		*Box nested2_2 FF00FFFF
		{
			0.4 0.7 0.3
			*o2w {*pos {1 0 0} *randori}
			*Box nested2_3 FF00FFFF
			{
				0.4 0.7 0.3
				*o2w {*pos {1 0 0} *randori}
			}
		}
	}
}

)"
LR"(// ************************************************************************************
// Camera
// ************************************************************************************

// A camera section must be at the top level in the script
// Camera descriptions raise an event immediately after being parsed.
// The application handles this event to set the camera position.
*Camera
{
	// Note: order is important. Camera properties set in the order declared
	*o2w{*pos{0 0 4}}         // Camera position/orientation within the scene
	*LookAt {0 0 0}           // Optional. Point the camera at {x,y,z} from where it currently is. Sets the focus distance
	//*Align {0 1 0}          // Optional. Lock the camera's up axis to  {x,y,z}
	//*Aspect {1.0}           // Optional. Aspect ratio (w/h). FovY is unchanged, FovX is changed. Default is 1
	//*FovX {45}              // Optional. X field of view (deg). Y field of view is determined by aspect ratio
	//*FovY {45}              // Optional. Y field of view (deg). X field of view is determined by aspect ratio (default 45 deg)
	//*Fov {45 45}            // Optional. {Horizontal,Vertical} field of view (deg). Implies aspect ratio.
	//*Near {0.01}            // Optional. Near clip plane distance
	//*Far {100.0}            // Optional. Far clip plane distance
	//*Orthographic           // Optional. Use an orthographic projection rather than perspective
}

)"
LR"(// ************************************************************************************
// Lights
// ************************************************************************************
// Light sources can be top level objects, children of other objects, or contain
// child objects. In some ways they are like a *Group object, they have no geometry
// of their own but can contain objects with geometry.

*DirLight sun FFFF00  // Colour attribute is the colour of the light source
{
	0 -1 -0.3                 // Direction dx,dy,dz (doesn't need to be normalised)
	*Specular {FFFFFF 1000}   // Optional. Specular colour and power
	*CastShadow {10}         // Optional. {range} Shadows are cast from this light source out to range
	*o2w{*pos{5 5 5}}         // Position/orientation of the object
}

*PointLight glow FF00FF
{
	5 5 5                     // Position x,y,z
	*Range {100 0}            // Optional. {range, falloff}. Default is infinite
	*Specular {FFFFFF 1000}   // Optional. Specular colour and power
	//*CastShadow {10}        // Optional. {range} Shadows are cast from this light source out to range
	*o2w{*pos{5 5 5}}
}

*SpotLight spot 00FFFF
{
	3 5 4                     // Position x,y,z
	-1 -1 -1                  // Direction dx,dy,dz (doesn't need to be normalised)
	30 60                     // Inner angle (deg), Outer angle (deg)
	*Range {100 0}            // Optional. {range, falloff}. Default is infinite
	*Specular {FFFFFF 1000}   // Optional. Specular colour and power
	//*CastShadow {10}       // Optional. {range} Shadows are cast from this light source out to range
	*o2w{*pos{5 5 5}}         // Position and orientation (directional lights shine down -z)
}

)"
LR"(// ************************************************************************************
// Objects
// ************************************************************************************
// Below is an example of every supported object type with notes on their syntax

// Font's declared at global scope set the default for following objects
// All fields are optional, replacing the default if given.
*Font
{
	*Name {"tahoma"}  // Font name
	*Size {18}        // Font size (in points)
	*Weight {300}     // Font weight (100 = ultra light, 300 = normal, 900 = heavy)
	*Style {Normal}   // Style. One of: Normal Italic Oblique
	*Stretch {5}      // Stretch. 1 = condensed, 5 = normal, 9 = expanded
	//*Underline      // Underlined text
	//*Strikeout      // Strikethrough text
}

// Screen space text.
*Text camera_space_text
{
	*ScreenSpace

	// Text is concatenated, with changes of style applied
	// Text containing multiple lines has the line-end whitespace and indentation tabs removed.
	*Font {*Name{"Times New Roman"} *Colour {FF0000FF} *Size{18}}
	"This is camera space 
	text with new lines in it, and "
	*NewLine
	*Font {*Colour {FF00FF00} *Size{24} *Style{Oblique}}
	"with varying colours 
	and "
	*Font {*Colour {FFFF0000} *Weight{800} *Style{Italic} *Strikeout}
	"stiles "
	*Font {*Colour {FFFFFF00} *Style{Italic} *Underline}
	"styles "

	// The background colour of the quad
	*BackColour {40A0A0A0}

	// Anchor defines the origin of the quad. (-1,-1) = bottom,left. (0,0) = centre (default). (+1,+1) = top,right
	*Anchor { -1 +1 }

	// Padding between the quad boundary and the text
	*Padding {10 20 10 10}
	
	// *o2w is interpreted as a 'text to camera space' transform
	// (-1,-1,0) is the lower left corner on the near plane.
	// (+1,+1,1) is the upper right corner on the far plane.
	// The quad is automatically scaled to make the text unscaled on-screen
	*o2w{*pos{-1, +1, 0}}
}

// Billboard text always faces the camera
*Text billboard_text
{
	*Font {*Colour {FF00FF00}} // Note: Font changes must come before text that uses the font
	"This is billboard text"
	*Billboard

	*Anchor {0 0}

	// The rotational part of *o2w is ignored, only the 3d position is used
	*o2w{*pos{0,1,0}}
}

// Simple text, billboarded, scaled based on camera distance
*Text three_dee_text
{
	*Font { *Name {"Courier New"} *Size {40} }

	// Normal text string, everything between quotes (including new lines)
	"This is normal         
	3D text. "
	
	*CString
	{
		// Alternative. Text is a C-Style string that uses escape characters
		// Everything between matched quotes (including new lines, although
		// indentation tabs are not removed for C-Style strings)
		"It can even       
		use\n \"C-Style\" strings" 
	}

	*Axis {+3}      // Optional. Set the direction of the quad normal. One of: X = ±1, Y = ±2, Z = ±3
	*Dim {512 128}  // Optional. The size used to determine the layout area
	*Format         // Optional
	{
	    Left      // Horizontal alignment. One of: Left, CentreH, Right
		Top       // Vertical Alignment. One of: Top, CentreV, Bottom
	    Wrap      // Text wrapping. One of: NoWrap, Wrap, WholeWord, Character, EmergencyBreak
	}
	*BackColour {40000000}
	*o2w{*scale{0.02}}
}

// A list of points
*Point pts
{
	0 0 0                 // x y z point positions
	1 1 1
	#embedded(CSharp)
	var rng = new Random();
	for (int i = 0; i != 100; ++i)
		Out.AppendLine(v4.Random3(1.0f, 1.0f, rng).ToString3());
	#end

	*Width {20}              // Optional. Specify a size for the point
	*Style { Circle }        // Optional. One of: Square, Circle, Star, .. Requires 'Width'
	*Texture {"#whitespot"}  // Optional. A texture for each point sprite. Requires 'Width'. Ignored if 'Style' given.
}

// Line modifiers:
//   *Coloured - The lines have an aarrggbb colour after each one. Must occur before line data if used.
//   *Width {w} - Render the lines with the thickness 'w' specified (in pixels).
//   *Param {t0 t1} - Clip/Extend the previous line to the parametric values given.
//   *dashed {on off} - Convert the line to a dashed line with dashes of length 'on' and gaps of length 'off'

// A model containing an arbitrary list of line segments
*Line lines
{
	*Coloured                          // Optional. If specified means the lines have an aarrggbb colour after each one. Must occur before line data if used
	-2  1  4  2 -3 -1 FFFF00FF         // x0 y0 z0  x1 y1 z1 Start and end points for a line
	 1 -2  4 -1 -3 -1 FF00FFFF
	-2  4  1  4 -3  1 FFFFFF00
}

// A model containing a list of line segments given by point and direction
*LineD lineds FF00FF00
{
	//*Coloured            // Optional. *Coloured is valid for all line types
	0  1  0 -1  0  0       // x y z dx dy dz - start and direction for a line
	0  1  0  0  0 -1
	0  1  0  1  0  0
	0  1  0  0  0  1
	*Param {0.2 0.6}       // Optional. Parametric values. Applies to the previous line only
}

// A model containing a sequence of line segments given by a list of points
*LineStrip linestrip
{
	*Coloured              // Optional.
	0 0 0 FF00FF00         // Colour of the vertex in the line strip
	0 0 1 FF0000FF         // *Param can only be used from the second vertex onwards
	0 1 1 FFFF00FF *Param {0.2 0.4}
	1 1 1 FFFFFF00
	1 1 0 FF00FFFF
	1 0 0 FFFFFFFF
	*dashed {0.1 0.05}
}

// A cuboid made from lines
*LineBox linebox
{
	2 4 1 // Width, height, depth. Accepts 1, 2, or 3 dimensions. 1dim = cube, 2 = rod, 3 = arbitrary box
}

// A grid of lines
*Grid grid FFA08080
{
	3      // axis_id
	4 5    // width, height
	8 10   // Optional, w,h divisions. If omitted defaults to width/height
}

// A curve described by a start and end point and two control points
*Spline spline
{
	*Coloured                           // Optional. If specified each spline has an aarrggbb colour after it. Must occur before any spline data if used
	0 0 0  0 0 1  1 0 1  1 0 0 FF00FF00 // p0 p1 p2 p3 - all points are positions
	0 0 0  1 0 0  1 1 0  1 1 1 FFFF0000 // tangents given by p1-p0, p3-p2
	*Width { 4 }                        // Optional line width
}

// An arrow
*Arrow arrow FF00FF00
{
	FwdBack                             // Type of  arrow. One of Line, Fwd, Back, or FwdBack
	*Coloured                           // Optional. If specified each line section has an aarrggbb colour after it. Must occur before any point data if used
	-1 -1 -1 FF00FF00                   // Corner points forming a line strip of connected lines
	-2  3  4 FFFF0000                   // Note, colour blends smoothly between each vertex
	 2  0 -2 FFFFFF00
	*Smooth                             // Optional. Turns the line segments into a smooth spline
	*Width { 5 }                        // Optional line width and arrow head size
}

)"
LR"(// A circle or ellipse
*Circle circle
{
	2                                   // axis_id: ±1 = ±x, ±2 = ±y, ±3 = ±z 
	1.6                                 // radius
	*Solid                              // Optional, if omitted then the circle is an outline only
	*RandColour *o2w{*RandPos{0 0 0 2}} // Object colour is the outline colour
	//*Facets { 40 }                    // Optional, controls the smoothness of the edge
}
*Circle ellipse
{
	2                                   // axis_id: ±1 = ±x, ±2 = ±y, ±3 = ±z
	1.6 0.8                             // radiusx, radiusy
	*Solid                              // Optional, if omitted then the circle is an outline only
	*RandColour *o2w{*RandPos{0 0 0 2}} // Object colour is the outline colour
	//*Facets { 40 }                    // Optional, controls the smoothness of the edge
}

// A pie/wedge
*Pie pie FF00FFFF
{
	2                                  // axis_id: ±1 = ±x, ±2 = ±y, ±3 = ±z
	10 45                              // Start angle, End angle in degress (from the 'x' axis). Equal values creates a ring
	0.1 0.7                            // inner radius, outer radius
	*Scale 1.0 0.8                     // Optional. X,Y scale factors
	*Solid                             // Optional, if omitted then the shape is an outline only
	//*Facets { 40 }                   // Optional, controls the smoothness of the inner and outer edges
}

// A rectangle
*Rect rect FF0000FF                    // Object colour is the outline colour
{
	2                                  // axis_id: ±1 = ±x, ±2 = ±y, ±3 = ±z
	1.2                                // width
	1.3                                // Optional height. If omitted, height = width
	*Solid                             // Optional, if omitted then the shape is an outline only
	*CornerRadius { 0.2 }              // Optional corner radius for rounded corners
	*Facets { 2 }                      // Optional, controls the smoothness of the corners
}

// A 2D polygon
*Polygon poly FFFFFFFF
{
	//*Coloured                          // Optional. If specified means each point has a colour (must come first)
	3                                  // axis_id: ±1 = ±x, ±2 = ±y, ±3 = ±z
	1.0f, 3.0f                         // A list of 2D points with CCW winding order describing the polygon
	1.4f, 1.7f
	0.4f, 2.0f
	1.5f, 1.2f
	1.0f, 0.0f
	1.7f, 1.0f
	2.5f, 0.5f
	2.0f, 1.5f
	2.0f, 2.0f
	1.5f, 2.5f
	*Solid                             // Optional. Filled polygon
}

// A matrix drawn as a set of three basis vectors (X=red, Y=green, Z=blue)
*Matrix3x3 a2b_transform
{
	1 0 0      // X
	0 1 0      // Y
	0 0 1      // Z
}

// A standard right-handed set of basis vectors
*CoordFrame a2b
{
	0.1                                // Optional, scale
	*LeftHanded                        // Optional, create a left handed coordinate frame
}

// A list of triangles
*Triangle triangle FFFFFFFF
{
	*Coloured                          // Optional. If specified means each corner of the triangle has a colour
	-1.5 -1.5 0 FFFF0000               // Three corner points of the triangle
	 1.5 -1.5 0 FF00FF00
	 0.0  1.5 0 FF0000FF
	*o2w{*randpos{0 0 0 2}}
	*Texture {"#checker"}              // Optional texture
}

// A quad given by 4 corner points
*Quad quad FFFFFFFF
{
	*Coloured                 // Optional. If specified means each corner of the quad has a colour
	-1.5 -1.5 0 FFFF0000      // Four corner points of the quad
	 1.5 -1.5 0 FF00FF00      // Corner order should be 'S' layout
	-1.5  1.5 0 FF0000FF      // i.e.
	 1.5  1.5 0 FFFF00FF      //  (-x,-y)  (x,-y)  (-x,y)  (x,y)
	*o2w{*randpos{0 0 0 2}}
	*Texture                  // Optional texture
	{
		"#checker"                                // texture filepath, stock texture name (e.g. #white, #black, #checker), or texture id (e.g. #1, #3)
		*Addr {Clamp Clamp}                       // Optional addressing mode for the texture; U, V. Options: Wrap, Mirror, Clamp, Border, MirrorOnce
		*Filter {Linear}                          // Optional filtering of the texture. Options: Point, Linear, Anisotropic
		*o2w { *scale{100 100 1} *euler{0 0 90} } // Optional 3d texture coord transform
	}
}

// A quad to represent a plane
*Plane plane FF000080
{
	0 -2 -2               // x y z - centre point of the plane
	1 1 1                 // dx dy dz - forward direction of the plane
	0.5 0.5               // width, height of the edges of the plane quad
	*Texture {"#checker"} // Optional texture
}

// A triangle strip of quads following a line
*Ribbon ribbon FF00FFFF
{
	3                     // Axis id. The forward facing axis for the ribbon
	0.1                   // Width (in world space)
	*Coloured             // Optional. If specified means each pair of verts in along the ribbon has a colour
	-1 -2  0 FFFF0000
	-1  3  0 FF00FF00
	 2  0  0 FF0000FF
	*Smooth               // Optional. Generates a spline through the points
	*o2w{*randpos{0 0 0 2} *randori}
	*Texture              // Optional texture repeated along each quad of the ribbon
	{
		"#checker"
	}
}

)"
LR"(// A box given by width, height, and depth
*Box box
{
	0.2 0.5 0.3                       // Width, [height], [depth]. Accepts 1, 2, or 3 dimensions. 1dim=cube, 2=rod, 3=arbitrary box
	*RandColour *o2w{*RandPos{0 0 0 2}}
}

// A box between two points with a width and height in the other two directions
*BoxLine boxline
{
	*Up {0 1 0}                       // Optional. Controls the orientation of width and height for the box (must come first if specified)
	0 1 0  1 2 1  0.1 0.15            // x0 y0 z0  x1 y1 z1  width [height]. height = width if omitted
	*RandColour *o2w{*RandPos{0 0 0 2}}
}

// A list of boxes all with the same dimensions at the given locations
*BoxList boxlist
{
	 0.4  0.2  0.5 // Box dimensions: width, height, depth.
	-1.0 -1.0 -1.0 // locations: x,y,z
	-1.0  1.0 -1.0
	 1.0 -1.0 -1.0
	 1.0  1.0 -1.0
	-1.0 -1.0  1.0
	-1.0  1.0  1.0
	 1.0 -1.0  1.0
	 1.0  1.0  1.0
}

// A frustum given by width, height, near plane and far plane
// Width, Height given at '1' along the z axis by default, unless *ViewPlaneZ is given
*FrustumWH frustumwh
{
	2 1 1 0 1.5                         // axis_id, width, height, near plane, far plane. axis_id: ±1 = ±x, ±2 = ±y, ±3 = ±z
	*ViewPlaneZ { 2 }                   // Optional. The distance at which the frustum has dimensions width,height
	*RandColour *o2w{*RandPos{0 0 0 2}}
}

// A frustum given by field of view (in Y), aspect ratio, and near and far plane distances
*FrustumFA frustumfa
{
	-1 90 1 0.4 1.5                    // axis_id, fovY, aspect, near plane, far plane. axis_id: ±1 = ±x, ±2 = ±y, ±3 = ±z
	*RandColour *o2w{*RandPos{0 0 0 2}}
}

// A sphere given by radius
*Sphere sphere
{
	0.2                                  // radius
	*Divisions 3                         // Optional. Controls the faceting of the sphere
	*RandColour *o2w{*RandPos{0 0 0 2}}
	*Texture                             // Optional texture
	{
		"#checker"
		*Addr {Wrap Wrap}
		*o2w {*scale{10 10 1}}
	}
}
*Sphere ellipsoid
{
	0.2 0.4 0.6                        // xradius [yradius] [zradius]
	*Texture {"#checker"}              // Optional texture
	*RandColour *o2w{*RandPos{0 0 0 2}}
}

// A cylinder given by axis number, height, and radius
*CylinderHR cylinder
{
	2 0.6 0.2                         // axis_id, height, radius. axis_id: ±1 = ±x, ±2 = ±y, ±3 = ±z
	*Layers 3                         // Optional. Controls the number of divisions along the cylinder major axis
	*Wedges 50                        // Optional. Controls the faceting of the curved parts of the cylinder
	*Scale 1.2 0.8                    // Optional. X,Y scale factors
	*Texture {"#checker"}             // Optional texture
	*RandColour *o2w{*RandPos{0 0 0 2}}
}
*CylinderHR cone FFFF00FF
{
	2 0.8 0.5 0                       // axis_id, height, base radius, [tip radius]. axis_id: ±1 = ±x, ±2 = ±y, ±3 = ±z
	*Layers 3                         // Optional. Controls the number of divisions along the cone major axis
	*Wedges 50                        // Optional. Controls the faceting of the curved parts of the cone
	*Scale 1.5 0.4                    // Optional. X,Y scale factors
	*Texture {"#checker"}             // Optional texture
	*RandColour *o2w{*RandPos{0 0 0 2}}
}

// A cone given by axis number, two heights, and solid angle
*ConeHA coneha FF00FFFF
{
	2 0.1 1.2 0.5                     // axis_id, tip-to-top distance, tip-to-base distance, solid angle(rad). axis_id: ±1 = ±x, ±2 = ±y, ±3 = ±z
	*Layers 3                         // Optional. Controls the number of divisions along the cone major axis
	*Wedges 50                        // Optional. Controls the faceting of the curved parts of the cone
	*Scale 1 1                        // Optional. X,Y scale factors
	*Texture {"#checker"}             // Optional texture
	*RandColour *o2w{*RandPos{0 0 0 2}}
}

// An extrusion along a path
*Tube tube FFFFFFFF
{
	*Style
	{
		// The cross section type; one of Round, Square, CrossSection
		//Round 0.2 0.3  // Elliptical profile. radius X [radius Y]  
		//Square 0.2 0.3 // Rectangular profile. radius X [radius Y]
		CrossSection     // <list of X,Y pairs> Arbitrary profile
		-0.2 -0.2
		+0.2 -0.2
		+0.05 +0.0
		+0.2 +0.2
		-0.2 +0.2
		-0.05 +0.0
		*Facets { 50 }  // Optional. Used for Round cross section profiles
		//*Smooth       // Optional. Use smooth normals for the walls of the tube
	}
	*Coloured             // Optional. If specified means each cross section along the tube has a colour
	 0  1  0 FFFF0000
	 0  0  1 FF00FF00
	 0  1  2 FF0000FF
	 1  1  2 FFFF00FF
	*Smooth               // Optional. Generates a spline through the extrusion path
	*Closed               // Optional. Fills in the end caps of the tube
	*o2w{*randpos{0 0 0 2} *randori}
}

)"
LR"(// A mesh of lines, faces, or tetrahedra.
// Syntax:
//	*Mesh [name] [colour]
//	{
//		*Verts { x y z ... }
//		[*Normals { nx ny nz ... }]                            // One per vertex
//		[*Colours { c0 c1 c2 ... }]                            // One per vertex
//		[*TexCoords { tx ty ... }]                             // One per vertex
//		*Faces { f00 f01 f02  f10 f11 f12  f20 f21 f22  ...}   // Indices of faces
//		*Lines { l00 l01  l10 l11  l20 l21  l30 l31 ...}       // Indices of lines
//		*Tetra { t00 t01 t02 t03  t10 t11 t12 t13 ...}         // Indices of tetrahedra
//		[GenerateNormals]                                      // Only works for faces or tetras
//	}
// Each 'Faces', 'Lines', 'Tetra' block is a sub-model within the mesh
// Vertex, normal, colour, and texture data must be given before faces, lines, or tetra data
// Textures must be defined before faces, lines, or tetra
*Mesh mesh FFFFFF00
{
	*Verts {
	1.087695 -2.175121 0.600000
	1.087695  3.726199 0.600000
	2.899199 -2.175121 0.600000
	2.899199  3.726199 0.600000
	1.087695  3.726199 0.721147
	1.087695 -2.175121 0.721147
	2.899199 -2.175121 0.721147
	2.899199  3.726199 0.721147
	1.087695  3.726199 0.721147
	1.087695  3.726199 0.600000
	1.087695 -2.175121 0.600000
	1.087695 -2.175121 0.721147
	2.730441  3.725990 0.721148
	2.740741 -2.175321 0.721147
	2.740741 -2.175321 0.600000
	2.730441  3.725990 0.600000
	}
	*Faces {
	0,1,2;,      // commas and semicolons treated as whitespace
	3,2,1;,
	4,5,6;,
	6,7,4;,
	8,9,10;,
	8,10,11;,
	12,13,14;,
	14,15,12;;
	}
	*GenerateNormals {30}
}

// Find the convex hull of a point cloud
*ConvexHull convexhull FFFFFF00
{
	*Verts {
	-0.998  0.127 -0.614
	 0.618  0.170 -0.040
	-0.300  0.792  0.646
	 0.493 -0.652  0.718
	 0.421  0.027 -0.392
	-0.971 -0.818 -0.271
	-0.706 -0.669  0.978
	-0.109 -0.762 -0.991
	-0.983 -0.244  0.063
	 0.142  0.204  0.214
	-0.668  0.326 -0.098
	}
	*RandColour *o2w{*RandPos{0 0 -1 2}}
}

// Model from a 3d model file.
// Supported formats: *.3ds
//*Model model_from_file FFFFFFFF
//{
//	"filepath"            // The file to create the model from
//	*Part { n }           // For model formats that contain multiple models, allows a specific one to be selected
//	*GenerateNormals {30} // Generate normals for the model (smoothing angle between faces)
//}

// Create a chart from a table of values.
// Expects text data in a 2D matrix. Plots columns 1,2,3,.. v.s. column 0.
*Chart chart FFFFFFFF
{
	3                 // Axis id
	//#include "filepath.csv" // Alternatively, #include a file containing the data:
	index, col1, col2 // Rows containing non-number values are ignored
	0, 10, -10,       // Chart data
	1,  5,   0,
	2,  0,   8,
	3,  2,   5,
	4,  6,  10,       // Trailing blank values are ignored
	                  // Trailing new lines are ignored
	*XColumn { 0 }    // Optional. Define which column to use as the X axis values (default 0). Use -1 to plot vs. index position
	*XAxis { 2 }      // Optional. Explicit X axis Y intercept
	*YAxis { 0 }      // Optional. Explicit Y axis X intercept
	*Colours          // Optional. Assign colours to each column
	{
		FF00FF00
		FFFF0000
	}
	*Width { 1 }      // Optional. A width for the lines
}

// A group of objects
*Group group
{
	*Wireframe     // Object modifiers applied to groups are applied recursively to children within the group
	*Box b FF00FF00 { 0.4 0.1 0.2 }
	*Sphere s FF0000FF { 0.3 *o2w{*pos{0 1 2}}}
}

// Embedded lua code can be used to programmatically generate script
#embedded(lua,support)
	-- lua code
	function make_box(box_number)
		return "*box b"..box_number.." FFFF0000 { 1 *o2w{*randpos {0 1 0 2}}}\n"
	end

	function make_boxes()
		local str = ""
		for i = 0,10 do
			str = str..make_box(i)
		end
		return str
	end
#end

*Group luaboxes1
{
	*o2w {*pos {-10 0 0}}
	#embedded(lua) return make_boxes() #end
}

*Group luaboxes2
{
	*o2w {*pos {10 0 0}}
	#embedded(lua) return make_boxes() #end
}

// Embedded C# code can be used also when used via View3D.
// Embedded CSharp code is constructed as follows
//	 namespace ldr
//	 {
//	 	public class Main
//	 	{
//	 		private StringBuilder Out = new StringBuilder();
//
//			#embedded(CSharp,support) code added here.
//			The 'Main' object exists for the whole script
//			successive #embedded(CSharp,support) sections are appended.
//
//	 		public string Execute()
//	 		{
//	 			#embedded(CSharp) code added here
//				Each #embedded(CSharp) section replaces the previous one.
//	 		
//				Add to the StringBuilder object 'Out'
//	 			return Out.ToString();
//	 		}
//	 	}
//	 }
#embedded(CSharp,support)
	Random m_rng;
	public Main()
	{
		m_rng = new Random();
	}
	m4x4 O2W
	{
		get { return m4x4.Random4x4(v4.Origin, 2.0f, m_rng); }
	}
#end

*Group csharp_spheres
{
	#embedded(CSharp)
	Out.AppendLine(Ldr.Box("CS_Box", 0xFFFF0080, 0.4f, O2W));
	Log.Info("You can also write to the log window (when used in LDraw) ");
	#end
}

)"
LR"(// ************************************************************************************
// Ldr script syntax and features:
// ************************************************************************************
//		*Keyword                    - keywords are identified by '*' characters
//		{// Section begin           - nesting of objects within sections implies parenting
//			// Line comment         - single line comments
//			/* Block comment */     - block comments
//			#eval{1+2}              - macro expression evaluation
//		}// Section end
//
//		C-style preprocessing
//		#include \"include_file\"   - include other script files
//		#define MACRO subst_text    - define text substitution macros
//		MACRO                       - macro substitution
//		#undef MACRO                - un-defining of macros
//		#ifdef MACRO                - nestable preprocessor controlled sections
//		#elif MACRO
//			#ifndef MACRO
//			#endif
//		#else
//		#endif
//		#lit
//			literal text
//		#end
//		#embedded(lua)
//			--lua code
//		#end
)";
#pragma endregion
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

		LdrObject::LdrObject(ObjectAttributes const& attr, LdrObject* parent, pr::Guid const& context_id)
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
			PR_EXPAND(PR_DBG, pr::events::Send(Evt_LdrObjectDestruct(this)));
			PR_EXPAND(PR_DBG, g_ldr_object_tracker.remove(this));
		}

		// Return the declaration name of this object
		string32 LdrObject::TypeAndName() const
		{
			return string32(ToStringA(m_type)) + " " + m_name;
		}

		// Recursively add this object and its children to a viewport
		void LdrObject::AddToScene(Scene& scene, float time_s, pr::m4x4 const* p2w)
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
		void LdrObject::AddBBoxToScene(Scene& scene, ModelPtr bbox_model, float time_s, pr::m4x4 const* p2w)
		{
			// Set the instance to world for this object
			auto i2w = *p2w * m_o2p * m_anim.Step(time_s);

			// Add the bbox instance to the scene drawlist
			if (m_model && !AnySet(m_flags, ELdrFlags::Hidden|ELdrFlags::SceneBoundsExclude))
			{
				// Find the object to world for the bbox
				auto o2w = i2w * m4x4::Scale(
					m_model->m_bbox.SizeX() + pr::maths::tiny,
					m_model->m_bbox.SizeY() + pr::maths::tiny,
					m_model->m_bbox.SizeZ() + pr::maths::tiny,
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
			if (index < 0 || index >= int(m_child.size())) throw std::exception(pr::FmtS("LdrObject child index (%d) out of range [0,%d)", index, int(m_child.size())));
			return m_child[index].get();
		}

		// Get/Set the object to world transform of this object or the first child object matching 'name' (see Apply)
		pr::m4x4 LdrObject::O2W(char const* name) const
		{
			auto obj = Child(name);
			if (obj == nullptr)
				return pr::m4x4Identity;

			// Combine parent transforms back to the root
			auto o2w = obj->m_o2p;
			for (auto p = obj->m_parent; p != nullptr; p = p->m_parent)
				o2w = p->m_o2p * o2w;

			return o2w;
		}
		void LdrObject::O2W(pr::m4x4 const& o2w, char const* name)
		{
			Apply([&](LdrObject* o)
			{
				o->m_o2p = o->m_parent ? pr::InvertFast(o->m_parent->O2W()) * o2w : o2w;
				assert(pr::FEql(o->m_o2p.w.w, 1.0f) && "Invalid instance transform");
				return true;
			}, name);
		}

		// Get/Set the object to parent transform of this object or child objects matching 'name' (see Apply)
		pr::m4x4 LdrObject::O2P(char const* name) const
		{
			auto obj = Child(name);
			return obj ? obj->m_o2p : pr::m4x4Identity;
		}
		void LdrObject::O2P(pr::m4x4 const& o2p, char const* name)
		{
			Apply([&](LdrObject* o)
			{
				assert(pr::FEql(o2p.w.w, 1.0f) && "Invalid instance transform");
				assert(pr::IsFinite(o2p) && "Invalid instance transform");
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
			return obj ? obj->m_screen_space : false;
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
				if (AllSet(o->m_flags, ELdrFlags::Hidden))
				{
				}
				else
				{
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
		void LdrObject::Colour(Colour32 colour, pr::uint mask, char const* name, EColourOp op, float op_value)
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
					nug.m_tint_has_alpha = tint_has_alpha;
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
					nug.m_tint_has_alpha = has_alpha;
					nug.UpdateAlphaStates();
				}

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
			auto idx = pr::index_of(m_child, child);
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
}
