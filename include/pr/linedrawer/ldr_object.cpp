//***************************************************************************************************
// Ldr Object Manager
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************

#include <string>
#include <sstream>
#include <array>
#include <set>
#include <unordered_map>
#include "pr/linedrawer/ldr_object.h"
#include "pr/common/assert.h"
#include "pr/common/hash.h"
#include "pr/common/windows_com.h"
#include "pr/maths/maths.h"
#include "pr/maths/convexhull.h"
#include "pr/gui/progress_dlg.h"
#include "pr/renderer11/renderer.h"

using namespace pr::rdr;
using namespace pr::script;

namespace pr
{
	namespace ldr
	{
		typedef pr::hash::HashValue      HashValue;
		typedef pr::vector<pr::v4>       VCont;
		typedef pr::vector<pr::v4>       NCont;
		typedef pr::vector<pr::uint16>   ICont;
		typedef pr::vector<pr::Colour32> CCont;
		typedef pr::vector<pr::v2>       TCont;
		typedef ParseResult::ModelLookup ModelCont;

		struct Cache
		{
			VCont m_point;
			NCont m_norms;
			ICont m_index;
			CCont m_color;
			TCont m_texts;

			void release()
			{
				m_point.clear();
				m_norms.clear();
				m_index.clear();
				m_color.clear();
				m_texts.clear();
			}
		} g_cache;
		inline VCont& Point() { g_cache.m_point.resize(0); return g_cache.m_point; }
		inline NCont& Norms() { g_cache.m_norms.resize(0); return g_cache.m_norms; }
		inline ICont& Index() { g_cache.m_index.resize(0); return g_cache.m_index; }
		inline CCont& Color() { g_cache.m_color.resize(0); return g_cache.m_color; }
		inline TCont& Texts() { g_cache.m_texts.resize(0); return g_cache.m_texts; }

		// Check the hash values are correct
		PR_EXPAND(PR_DBG, static bool s_eldrobject_kws_checked = pr::CheckHashEnum<ELdrObject>([&](char const* s) { return pr::script::Reader::HashKeyword(s, false); }));
		PR_EXPAND(PR_DBG, static bool s_ekeyword_kws_checked   = pr::CheckHashEnum<EKeyword  >([&](char const* s) { return pr::script::Reader::HashKeyword(s, false); }));

		// LdrObject Creation functions *********************************************

		// Helper object for passing parameters between parsing functions
		struct ParseParams
		{
			pr::Renderer&  m_rdr;
			Reader&        m_reader;
			ObjectCont&    m_objects;
			ModelCont&     m_models;
			ContextId      m_context_id;
			HashValue      m_keyword;
			LdrObject*     m_parent;

			ParseParams(pr::Renderer& rdr, Reader& reader, ObjectCont& objects, ModelCont& models, ContextId context_id, HashValue keyword, LdrObject* parent)
				:m_rdr(rdr)
				,m_reader(reader)
				,m_objects(objects)
				,m_models(models)
				,m_context_id(context_id)
				,m_keyword(keyword)
				,m_parent(parent)
			{}
			ParseParams(ParseParams& p, ObjectCont& objects, HashValue keyword, LdrObject* parent)
				:m_rdr(p.m_rdr)
				,m_reader(p.m_reader)
				,m_objects(objects)
				,m_models(p.m_models)
				,m_context_id(p.m_context_id)
				,m_keyword(keyword)
				,m_parent(parent)
			{}

			PR_NO_COPY(ParseParams);
		};

		// Forward declare the recursive object parsing function
		bool ParseLdrObject(ParseParams& p);

		// Read the name, colour, and instance flag for an object
		ObjectAttributes ParseAttributes(pr::script::Reader& reader, ELdrObject model_type)
		{
			ObjectAttributes attr;
			attr.m_type = model_type;
			attr.m_name = ELdrObject::ToString(model_type);
			
			// Read the next tokens
			string32 tok0, tok1; auto count = 0;
			if (!reader.IsSectionStart()) { reader.ExtractToken(tok0, "{}"); ++count; }
			if (!reader.IsSectionStart()) { reader.ExtractToken(tok1, "{}"); ++count; }
			if (!reader.IsSectionStart()) { reader.ExtractBool(attr.m_instance); }

			// If not all tokens are given, allow the name and/or colour to be optional
			uint32 aarrggbb;
			auto ExtractColour = [](string32 const& tok, uint32& col)
			{
				char const* end;
				col = ::strtoul(tok.c_str(), (char**)&end, 16);
				return std::size_t(end - tok.c_str()) == tok.size();
			};

			// If the second token is a valid colour, assume the first is the name
			if (count == 2 && ExtractColour(tok1, aarrggbb))
			{
				if (!pr::str::ExtractIdentifierC(attr.m_name, std::begin(tok0))) reader.ReportError(pr::script::EResult::TokenNotFound, "object name is invalid");
				attr.m_colour = aarrggbb;
			}
			// If the first token is a valid colour and no second token was given, assume the first token is the colour and no name was given
			else if (count == 1 && ExtractColour(tok0, aarrggbb))
			{
				attr.m_colour = aarrggbb;
			}
			// Otherwise, make no assumptions
			else
			{
				if (count >= 1 && !pr::str::ExtractIdentifierC(attr.m_name, std::begin(tok0))) reader.ReportError(pr::script::EResult::TokenNotFound, "object name is invalid");
				if (count >= 2 && !ExtractColour(tok1, attr.m_colour.m_aarrggbb))              reader.ReportError(pr::script::EResult::TokenNotFound, "object colour is invalid");
			}
			return attr;
		}

		// Parse a transform description
		void ParseTransform(pr::script::Reader& reader, pr::m4x4& o2w)
		{
			assert(pr::IsFinite(o2w) && "A valid 'o2w' must be passed to this function as it premultiplies the transform with the one read from the script");
			pr::m4x4 p2w = pr::m4x4Identity;

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
				case EKeyword::M4x4:
					{
						m4x4 o2w = m4x4Identity;
						reader.ExtractMatrix4x4S(o2w);
						p2w = o2w * p2w;
						break;
					}
				case EKeyword::M3x3:
					{
						m4x4 m = m4x4Identity;
						reader.ExtractMatrix3x3S(m.rot);
						p2w = m * p2w;
						break;
					}
				case EKeyword::Pos:
					{
						m4x4 m = m4x4Identity;
						reader.ExtractVector3S(m.pos, 1.0f);
						p2w = m * p2w;
						break;
					}
				case EKeyword::Align:
					{
						int axis_id;
						pr::v4 direction;
						reader.SectionStart();
						reader.ExtractInt(axis_id, 10);
						reader.ExtractVector3(direction, 0.0f);
						reader.SectionEnd();

						v4 axis = AxisId(axis_id);
						if (IsZero3(axis))
						{
							reader.ReportError(pr::script::EResult::UnknownValue, "axis_id must one of ±1, ±2, ±3");
							break;
						}

						p2w = pr::Rotation4x4(axis, direction, v4Origin) * p2w;
						break;
					}
				case EKeyword::Quat:
					{
						pr::Quat quat;
						reader.ExtractVector4S(quat.xyzw);
						p2w = Rotation4x4(quat, v4Origin) * p2w;
						break;
					}
				case EKeyword::Rand4x4:
					{
						float radius;
						pr::v4 centre;
						reader.SectionStart();
						reader.ExtractVector3(centre, 1.0f);
						reader.ExtractReal(radius);
						reader.SectionEnd();
						p2w = pr::Random4x4(centre, radius) * p2w;
						break;
					}
				case EKeyword::RandPos:
					{
						float radius;
						pr::v4 centre;
						reader.SectionStart();
						reader.ExtractVector3(centre, 1.0f);
						reader.ExtractReal(radius);
						reader.SectionEnd();
						p2w = Translation4x4(Random3(centre, radius, 1.0f)) * p2w;
						break;
					}
				case EKeyword::RandOri:
					{
						m4x4 m = m4x4Identity;
						m.rot = pr::Random3x4();
						p2w = m * p2w;
						break;
					}
				case EKeyword::Euler:
					{
						pr::v4 angles;
						reader.ExtractVector3S(angles, 0.0f);
						p2w = Rotation4x4(pr::DegreesToRadians(angles.x), pr::DegreesToRadians(angles.y), pr::DegreesToRadians(angles.z), pr::v4Origin) * p2w;
						break;
					}
				case EKeyword::Scale:
					{
						pr::v4 scale;
						reader.SectionStart();
						reader.ExtractReal(scale.x);
						if (reader.IsSectionEnd())
							scale.z = scale.y = scale.x;
						else
						{
							reader.ExtractReal(scale.y);
							reader.ExtractReal(scale.z);
						}
						reader.SectionEnd();
						p2w = Scale4x4(scale.x, scale.y, scale.z, v4Origin) * p2w;
						break;
					}
				case EKeyword::Transpose:
					{
						p2w = pr::Transpose4x4(p2w);
						break;
					}
				case EKeyword::Inverse:
					{
						p2w = pr::IsOrthonormal(p2w) ? pr::InvertFast(p2w) : pr::Invert(p2w);
						break;
					}
				case EKeyword::Normalise:
					{
						p2w.x = pr::Normalise3(p2w.x);
						p2w.y = pr::Normalise3(p2w.y);
						p2w.z = pr::Normalise3(p2w.z);
						break;
					}
				case EKeyword::Orthonormalise:
					{
						p2w = pr::Orthonorm(p2w);
						break;
					}
				}
			}
			reader.SectionEnd();

			// Premultiply the object to world transform
			o2w = p2w * o2w;
			PR_INFO_IF(PR_DBG, o2w.w.w != 1.0f, "o2w.w.w != 1.0f - non orthonormal transform");
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
						pr::m4x4 c2w = pr::m4x4Identity;
						ParseTransform(reader, c2w);
						out.m_cam.CameraToWorld(c2w);
						out.m_cam_fields |= ParseResult::ECamField::C2W;
						break;
					}
				case EKeyword::LookAt:
					{
						pr::v4 lookat;
						reader.ExtractVector3S(lookat, 1.0f);
						pr::m4x4 c2w = out.m_cam.CameraToWorld();
						out.m_cam.LookAt(c2w.pos, lookat, c2w.y);
						out.m_cam_fields |= ParseResult::ECamField::C2W;
						out.m_cam_fields |= ParseResult::ECamField::Focus;
						break;
					}
				case EKeyword::Align:
					{
						pr::v4 align;
						reader.ExtractVector3S(align, 0.0f);
						out.m_cam.SetAlign(align);
						out.m_cam_fields |= ParseResult::ECamField::Align;
						break;
					}
				case EKeyword::Aspect:
					{
						float aspect;
						reader.ExtractRealS(aspect);
						out.m_cam.Aspect(aspect);
						out.m_cam_fields |= ParseResult::ECamField::Align;
						break;
					}
				case EKeyword::FovX:
					{
						float fovX;
						reader.ExtractRealS(fovX);
						out.m_cam.FovX(fovX);
						out.m_cam_fields |= ParseResult::ECamField::FovY;
						break;
					}
				case EKeyword::FovY:
					{
						float fovY;
						reader.ExtractRealS(fovY);
						out.m_cam.FovY(fovY);
						out.m_cam_fields |= ParseResult::ECamField::FovY;
						break;
					}
				case EKeyword::Fov:
					{
						float fov[2];
						reader.ExtractRealArrayS(fov, 2);
						out.m_cam.Fov(fov[0], fov[1]);
						out.m_cam_fields |= ParseResult::ECamField::Aspect;
						out.m_cam_fields |= ParseResult::ECamField::FovY;
						break;
					}
				case EKeyword::Near:
					{
						reader.ExtractReal(out.m_cam.m_near);
						out.m_cam_fields |= ParseResult::ECamField::Near;
						break;
					}
				case EKeyword::Far:
					{
						reader.ExtractReal(out.m_cam.m_far);
						out.m_cam_fields |= ParseResult::ECamField::Far;
						break;
					}
				case EKeyword::AbsoluteClipPlanes:
					{
						out.m_cam.m_focus_rel_clip = false;
						out.m_cam_fields |= ParseResult::ECamField::AbsClip;
						break;
					}
				case EKeyword::Orthographic:
					{
						out.m_cam.m_orthographic = true;
						out.m_cam_fields |= ParseResult::ECamField::Ortho;
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
						reader.ExtractIdentifier(style);
						if      (pr::str::EqualI(style, "NoAnimation"   )) anim.m_style = EAnimStyle::NoAnimation;
						else if (pr::str::EqualI(style, "PlayOnce"      )) anim.m_style = EAnimStyle::PlayOnce;
						else if (pr::str::EqualI(style, "PlayReverse"   )) anim.m_style = EAnimStyle::PlayReverse;
						else if (pr::str::EqualI(style, "PingPong"      )) anim.m_style = EAnimStyle::PingPong;
						else if (pr::str::EqualI(style, "PlayContinuous")) anim.m_style = EAnimStyle::PlayContinuous;
						break;
					}
				case EKeyword::Period:
					{
						reader.ExtractReal(anim.m_period);
						break;
					}
				case EKeyword::Velocity:
					{
						reader.ExtractVector3(anim.m_velocity, 0.0f);
						break;
					}
				case EKeyword::AngVelocity:
					{
						reader.ExtractVector3(anim.m_ang_velocity, 0.0f);
						break;
					}
				}
			}
			reader.SectionEnd();
		}

		// Parse a step block for an object
		void ParseStep(pr::script::Reader& reader, LdrObjectStepData& step)
		{
			reader.ExtractSection(step.m_code, false);
		}

		// Parse keywords that can appear in any section
		// Returns true if the keyword was recognised
		bool ParseProperties(ParseParams& p, EKeyword kw, LdrObjectPtr obj)
		{
			switch (kw)
			{
			default: return false;
			case EKeyword::O2W:
				{
					ParseTransform(p.m_reader, obj->m_o2p);
					return true;
				}
			case EKeyword::Colour:
				{
					p.m_reader.ExtractIntS(obj->m_base_colour.m_aarrggbb, 16);
					return true;
				}
			case EKeyword::ColourMask:
				{
					p.m_reader.ExtractIntS(obj->m_colour_mask, 16);
					return true;
				}
			case EKeyword::RandColour:
				{
					obj->m_base_colour = pr::RandomRGB();
					return true;
				}
			case EKeyword::Animation:
				{
					ParseAnimation(p.m_reader, obj->m_anim);
					return true;
				}
			case EKeyword::Hidden:
				{
					obj->m_visible = false;
					return true;
				}
			case EKeyword::Wireframe:
				{
					obj->m_wireframe = true;
					return true;
				}
			case EKeyword::Step:
				{
					ParseStep(p.m_reader, obj->m_step);
					return true;
				}
			}
		}

		// Parse a texture description
		// Returns a pointer to the Texture created in the renderer
		bool ParseTexture(ParseParams& p, Texture2DPtr& tex)
		{
			std::string tex_filepath;
			pr::m4x4 t2s = pr::m4x4Identity;
			SamplerDesc sam;

			p.m_reader.SectionStart();
			while (!p.m_reader.IsSectionEnd())
			{
				if (p.m_reader.IsKeyword())
				{
					EKeyword kw = p.m_reader.NextKeywordH<EKeyword>();
					switch (kw)
					{
					default:
					{
						p.m_reader.ReportError(pr::script::EResult::UnknownToken);
						break;
					}
					case EKeyword::O2W:
					{
						ParseTransform(p.m_reader, t2s);
						break;
					}
					case EKeyword::Addr:
					{
						char word[20];
						p.m_reader.SectionStart();
						p.m_reader.ExtractIdentifier(word); sam.AddressU = (D3D11_TEXTURE_ADDRESS_MODE)ETexAddrMode::Parse(word, false);
						p.m_reader.ExtractIdentifier(word); sam.AddressV = (D3D11_TEXTURE_ADDRESS_MODE)ETexAddrMode::Parse(word, false);
						p.m_reader.SectionEnd();
						break;
					}
					case EKeyword::Filter:
					{
						char word[20];
						p.m_reader.SectionStart();
						p.m_reader.ExtractIdentifier(word); sam.Filter = (D3D11_FILTER)EFilter::Parse(word, false);
						p.m_reader.SectionEnd();
						break;
					}
					}
				}
				else
				{
					p.m_reader.ExtractString(tex_filepath);
				}
			}
			p.m_reader.SectionEnd();

			// Silently ignore missing texture files
			if (!tex_filepath.empty())
			{
				// Create the texture
				try
				{
					tex = p.m_rdr.m_tex_mgr.CreateTexture2D(AutoId, sam, tex_filepath.c_str());
					tex->m_t2s = t2s;
				}
				catch (std::exception const& e)
				{
					p.m_reader.ReportError(pr::script::EResult::ValueNotFound, pr::FmtS("failed to create texture %s\nReason: %s", tex_filepath.c_str(), e.what()));
				}
			}
			return true;
		}

		// Parse a video texture
		bool ParseVideo(ParseParams& p, Texture2DPtr& vid)
		{
			std::string filepath;
			p.m_reader.SectionStart();
			p.m_reader.ExtractString(filepath);
			if (!filepath.empty())
			{
				(void)vid;
				//todo
				//// Load the video texture
				//try
				//{
				//	vid = p.m_rdr.m_tex_mgr.CreateVideoTexture(AutoId, filepath.c_str());
				//}
				//catch (std::exception const& e)
				//{
				//	p.m_reader.ReportError(pr::script::EResult::ValueNotFound, pr::FmtS("failed to create video %s\nReason: %s" ,filepath.c_str() ,e.what()));
				//}
			}
			p.m_reader.SectionEnd();
			return true;
		}

		// Template prototype for ObjectCreators
		template <ELdrObject::Enum_ ObjType> struct ObjectCreator;

		#pragma region ObjectCreator Base Classes

		// Base class for all object creators
		struct IObjectCreator
		{
			virtual ~IObjectCreator() {}
			virtual bool ParseKeyword(ParseParams&, EKeyword)
			{
				return false;
			}
			virtual void Parse(ParseParams& p)
			{
				p.m_reader.ReportError(pr::script::EResult::UnknownToken);
			}
			virtual void CreateModel(ParseParams& p, LdrObjectPtr obj)
			{
				(void)p, obj;
			}
		};

		// Base class for objects with a texture
		struct IObjectCreatorTexture :IObjectCreator
		{
			Texture2DPtr m_texture;
			NuggetProps m_local_mat;

			IObjectCreatorTexture() :m_texture(), m_local_mat() {}
			bool ParseKeyword(ParseParams& p, EKeyword kw) override
			{
				switch (kw) {
				default: return IObjectCreator::ParseKeyword(p, kw);
				case EKeyword::Texture:  ParseTexture(p, m_texture); return true;
				case EKeyword::Video:    ParseVideo(p, m_texture); return true;
				}
			}
			virtual NuggetProps* GetDrawData()
			{
				m_local_mat.m_topo = EPrim::Invalid;
				m_local_mat.m_geom = EGeom::Invalid;
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

			IObjectCreatorLight() :m_light() { m_light.m_on = true; }
			bool ParseKeyword(ParseParams& p, EKeyword kw) override
			{
				switch (kw) {
				default: return IObjectCreator::ParseKeyword(p, kw);
				case EKeyword::Range:
					{
						p.m_reader.SectionStart();
						p.m_reader.ExtractReal(m_light.m_range);
						p.m_reader.ExtractReal(m_light.m_falloff);
						p.m_reader.SectionEnd();
						return true;
					}
				case EKeyword::Specular:
					{
						p.m_reader.SectionStart();
						p.m_reader.ExtractInt(m_light.m_specular.m_aarrggbb, 16);
						p.m_reader.ExtractReal(m_light.m_specular_power);
						p.m_reader.SectionEnd();
						return true;
					}
				case EKeyword::CastShadow:
					{
						p.m_reader.ExtractRealS(m_light.m_cast_shadow);
						return true;
					}
				}
			}
			void CreateModel(ParseParams&, LdrObjectPtr obj) override
			{
				// Assign the light data as user data
				obj->m_user_data.get<Light>() = m_light;
			}
		};

		// Base class for object creators that are based on lines
		struct IObjectCreatorLine :IObjectCreator
		{
			VCont& m_point;
			ICont& m_index;
			CCont& m_colour;
			float  m_line_width;
			bool   m_per_line_colour;
			bool   m_smooth;
			bool   m_linestrip;
			bool   m_linemesh;

			IObjectCreatorLine(bool linestrip, bool linemesh)
				:m_point(Point())
				,m_index(Index())
				,m_colour(Color())
				,m_line_width()
				,m_per_line_colour()
				,m_smooth()
				,m_linestrip(linestrip)
				,m_linemesh(linemesh)
			{}
			bool ParseKeyword(ParseParams& p, EKeyword kw) override
			{
				switch (kw)
				{
				default: return IObjectCreator::ParseKeyword(p, kw);
				case EKeyword::Coloured: m_per_line_colour = true; return true;
				case EKeyword::Smooth: m_smooth = true; return true;
				case EKeyword::Width: p.m_reader.ExtractRealS(m_line_width); return true;
				case EKeyword::Param:
					{
						float t[2];
						p.m_reader.ExtractRealArrayS(t, 2);
						if (m_point.size() < 2)
						{
							p.m_reader.ReportError("No preceeding line to apply parametric values to");
							return true;
						}
						auto& p0 = m_point[m_point.size() - 2];
						auto& p1 = m_point[m_point.size() - 1];
						pr::v4 p = p0;
						pr::v4 dir = p1 - p0;
						p0 = p + t[0] * dir;
						p1 = p + t[1] * dir;
						return true;
					}
				}
			}
			void CreateModel(ParseParams& p, LdrObjectPtr obj) override
			{
				using namespace pr::rdr;

				// Validate
				if (m_point.size() < 2)
				{
					p.m_reader.ReportError(FmtS("Line object '%s' description incomplete", obj->TypeAndName().c_str()));
					return;
				}

				// Smooth the points
				if (m_smooth && m_linestrip)
				{
					auto points = m_point;
					m_point.resize(0);
					pr::Smooth(points, m_point);
				}

				// Create the model
				if      (m_linemesh)  obj->m_model = ModelGenerator<>::Mesh(p.m_rdr, m_linestrip ? EPrim::LineStrip : EPrim::LineList, m_point.size(), m_index.size(), m_point.data(), m_index.data(), m_colour.size(), m_colour.data());
				else if (m_linestrip) obj->m_model = ModelGenerator<>::LineStrip(p.m_rdr, m_point.size() - 1, m_point.data(), m_colour.size(), m_colour.data());
				else                  obj->m_model = ModelGenerator<>::Lines(p.m_rdr, m_point.size() / 2, m_point.data(), m_colour.size(), m_colour.data());
				obj->m_model->m_name = obj->TypeAndName();

				// Use thick lines
				if (m_line_width != 0.0f)
				{
					auto shdr = p.m_rdr.m_shdr_mgr.FindShader(EStockShader::ThickLineListGS)->Clone<ThickLineListShaderGS>(AutoId, pr::FmtS("thick_line_%f", m_line_width));
					shdr->m_default_width = m_line_width;
					for (auto& nug : obj->m_model->m_nuggets)
						nug.m_smap[ERenderStep::ForwardRender].m_gs = shdr;
				}
			}
		};

		// Base class for object creators that are based on 2d shapes
		struct IObjectCreatorShape2d :IObjectCreatorTexture
		{
			pr::AxisId m_axis_id;
			pr::v4 m_dim;
			int    m_facets;
			bool   m_solid;

			IObjectCreatorShape2d() :m_axis_id(), m_dim(), m_facets(40), m_solid(false) {}
			bool ParseKeyword(ParseParams& p, EKeyword kw) override
			{
				switch (kw)
				{
				default: return IObjectCreatorTexture::ParseKeyword(p, kw);
				case EKeyword::Solid:  m_solid = true; return true;
				case EKeyword::Facets: p.m_reader.ExtractIntS(m_facets, 10); return true;
				}
			}
		};

		// Base class for planar objects
		struct IObjectCreatorPlane :IObjectCreatorTexture
		{
			VCont& m_point;
			CCont& m_colour;
			bool   m_per_vert_colour;

			IObjectCreatorPlane() :m_point(Point()), m_colour(Color()), m_per_vert_colour() {}
			bool ParseKeyword(ParseParams& p, EKeyword kw) override
			{
				switch (kw) {
				default: return IObjectCreatorTexture::ParseKeyword(p, kw);
				case EKeyword::Coloured: m_per_vert_colour = true; return true;
				}
			}
			void CreateModel(ParseParams& p, LdrObjectPtr obj) override
			{
				using namespace pr::rdr;

				// Validate
				if (m_point.empty() || (m_point.size() % 4) != 0)
				{
					p.m_reader.ReportError("Object description incomplete");
					return;
				}

				// Create the model
				obj->m_model = ModelGenerator<>::Quad(p.m_rdr, m_point.size() / 4, m_point.data(), m_colour.size(), m_colour.data(), pr::m4x4Identity, GetDrawData());
				obj->m_model->m_name = obj->TypeAndName();
			}
		};

		// Base class for arbitrary cubiod shapes
		struct IObjectCreatorCuboid :IObjectCreatorTexture
		{
			pr::v4 m_pt[8];
			pr::m4x4 m_b2w;

			IObjectCreatorCuboid() :m_pt(), m_b2w(pr::m4x4Identity) {}
			void CreateModel(ParseParams& p, LdrObjectPtr obj) override
			{
				obj->m_model = ModelGenerator<>::Boxes(p.m_rdr, 1, m_pt, m_b2w, 0, 0, GetDrawData());
				obj->m_model->m_name = obj->TypeAndName();
			}
		};

		// Base class for cone shapes
		struct IObjectCreatorCone :IObjectCreatorTexture
		{
			pr::AxisId m_axis_id;
			pr::v4 m_dim; // x,y = radius, z = height
			pr::v2 m_scale;
			int m_layers, m_wedges;

			IObjectCreatorCone() :m_axis_id(), m_dim(), m_scale(pr::v2One), m_layers(1), m_wedges(20) {}
			bool ParseKeyword(ParseParams& p, EKeyword kw) override
			{
				switch (kw) {
				default: return IObjectCreatorTexture::ParseKeyword(p, kw);
				case EKeyword::Layers:  p.m_reader.ExtractInt(m_layers, 10); return true;
				case EKeyword::Wedges:  p.m_reader.ExtractInt(m_wedges, 10); return true;
				case EKeyword::Scale:   p.m_reader.ExtractVector2(m_scale); return true;
				}
			}
			void CreateModel(ParseParams& p, LdrObjectPtr obj) override
			{
				pr::m4x4 o2w, *po2w = nullptr;
				if (m_axis_id != 3)
				{
					o2w = pr::Rotation4x4(pr::AxisId(3), m_axis_id, pr::v4Origin);
					po2w = &o2w;
				}

				// Create the model
				obj->m_model = ModelGenerator<>::Cylinder(p.m_rdr, m_dim.x, m_dim.y, m_dim.z, m_scale.x, m_scale.y, m_wedges, m_layers, 1, &pr::Colour32White, po2w, GetDrawData());
				obj->m_model->m_name = obj->TypeAndName();
			}
		};

		// Base class for mesh based objects
		struct IObjectCreatorMesh :IObjectCreatorTexture
		{
			VCont& m_verts;
			NCont& m_normals;
			CCont& m_colours;
			TCont& m_texs;
			ICont& m_indices;
			EPrim::Enum_ m_prim_type;
			float m_gen_normals;

			IObjectCreatorMesh() :m_verts(Point()), m_normals(Norms()), m_colours(Color()), m_texs(Texts()), m_indices(Index()), m_prim_type(), m_gen_normals(-1.0f) {}
			bool ParseKeyword(ParseParams& p, EKeyword kw) override
			{
				switch (kw) {
				default: return IObjectCreatorTexture::ParseKeyword(p, kw);
				case EKeyword::Verts:
					{
						p.m_reader.SectionStart();
						for (pr::v4 v; !p.m_reader.IsSectionEnd();) { p.m_reader.ExtractVector3(v, 1.0f); m_verts.push_back(v); }
						p.m_reader.SectionEnd();
						return true;
					}
				case EKeyword::Normals:
					{
						p.m_reader.SectionStart();
						for (pr::v4 n; !p.m_reader.IsSectionEnd();) { p.m_reader.ExtractVector3(n, 0.0f); m_normals.push_back(n); }
						p.m_reader.SectionEnd();
						return true;
					}
				case EKeyword::Colours:
					{
						p.m_reader.SectionStart();
						for (pr::Colour32 c; !p.m_reader.IsSectionEnd();) { p.m_reader.ExtractInt(c.m_aarrggbb, 16); m_colours.push_back(c); }
						p.m_reader.SectionEnd();
						return true;
					}
				case EKeyword::TexCoords:
					{
						p.m_reader.SectionStart();
						for (pr::v2 t; !p.m_reader.IsSectionEnd();) { p.m_reader.ExtractVector2(t); m_texs.push_back(t); }
						p.m_reader.SectionEnd();
						return true;
					}
				case EKeyword::Lines:
					{
						p.m_reader.SectionStart();
						for (pr::uint16 idx[2]; !p.m_reader.IsSectionEnd();)
						{
							p.m_reader.ExtractIntArray(idx, 2, 10);
							m_indices.push_back(idx[0]);
							m_indices.push_back(idx[1]);
						}
						p.m_reader.SectionEnd();
						m_prim_type = EPrim::LineList;
						return true;
					}
				case EKeyword::Faces:
					{
						p.m_reader.SectionStart();
						for (pr::uint16 idx[3]; !p.m_reader.IsSectionEnd();)
						{
							p.m_reader.ExtractIntArray(idx, 3, 10);
							m_indices.push_back(idx[0]);
							m_indices.push_back(idx[1]);
							m_indices.push_back(idx[2]);
						}
						p.m_reader.SectionEnd();
						m_prim_type = EPrim::TriList;
						return true;
					}
				case EKeyword::Tetra:
					{
						p.m_reader.SectionStart();
						for (pr::uint16 idx[4]; !p.m_reader.IsSectionEnd();)
						{
							p.m_reader.ExtractIntArray(idx, 4, 10);
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
						}
						p.m_reader.SectionEnd();
						m_prim_type = EPrim::TriList;
						return true;
					}
				case EKeyword::GenerateNormals:
					{
						p.m_reader.ExtractRealS(m_gen_normals);
						m_gen_normals = pr::DegreesToRadians(m_gen_normals);
						return true;
					}
				}
			}
			void CreateModel(ParseParams& p, LdrObjectPtr obj) override
			{
				using namespace pr::rdr;

				// Validate
				if (m_indices.empty() || m_verts.empty())
				{
					p.m_reader.ReportError("Mesh object description incomplete");
					return;
				}

				// Generate normals if needed
				if (m_gen_normals >= 0.0f && m_prim_type == EPrim::TriList)
				{
					auto iout = std::begin(m_indices);
					m_normals.resize(m_verts.size());

					pr::geometry::GenerateNormals(m_indices.size(), m_indices.data(), m_gen_normals,
						[&](pr::uint16 i){ return m_verts[i]; }, 0,
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
							*iout++ = i0;
							*iout++ = i1;
							*iout++ = i2;
						});
				}

				// Create the model
				obj->m_model = ModelGenerator<>::Mesh(
					p.m_rdr,
					m_prim_type,
					m_verts.size(),
					m_indices.size(),
					m_verts.data(),
					m_indices.data(),
					m_colours.size(),
					m_colours.data(),
					m_normals.size(),
					m_normals.data(),
					m_texs.data(),
					GetDrawData());
				obj->m_model->m_name = obj->TypeAndName();
			}
		};

		#pragma endregion

		#pragma region Line Objects

		// ELdrObject::Line
		template <> struct ObjectCreator<ELdrObject::Line> :IObjectCreatorLine
		{
			ObjectCreator() :IObjectCreatorLine(false, false) {}
			void Parse(ParseParams& p) override
			{
				pr::v4 p0, p1;
				p.m_reader.ExtractVector3(p0, 1.0f);
				p.m_reader.ExtractVector3(p1, 1.0f);
				m_point.push_back(p0);
				m_point.push_back(p1);
				if (m_per_line_colour)
				{
					pr::Colour32 col;
					p.m_reader.ExtractInt(col.m_aarrggbb, 16);
					m_colour.push_back(col);
					m_colour.push_back(col);
				}
			}
		};

		// ELdrObject::LineD
		template <> struct ObjectCreator<ELdrObject::LineD> :IObjectCreatorLine
		{
			ObjectCreator() :IObjectCreatorLine(false, false) {}
			void Parse(ParseParams& p) override
			{
				pr::v4 p0, p1;
				p.m_reader.ExtractVector3(p0, 1.0f);
				p.m_reader.ExtractVector3(p1, 0.0f);
				m_point.push_back(p0);
				m_point.push_back(p0 + p1);
				if (m_per_line_colour)
				{
					pr::Colour32 col;
					p.m_reader.ExtractInt(col.m_aarrggbb, 16);
					m_colour.push_back(col);
					m_colour.push_back(col);
				}
			}
		};

		// ELdrObject::LineStrip
		template <> struct ObjectCreator<ELdrObject::LineStrip> :IObjectCreatorLine
		{
			ObjectCreator() :IObjectCreatorLine(true, false) {}
			void Parse(ParseParams& p) override
			{
				pr::v4 pt;
				p.m_reader.ExtractVector3(pt, 1.0f);
				m_point.push_back(pt);
				
				if (m_per_line_colour)
				{
					pr::Colour32 col;
					p.m_reader.ExtractInt(col.m_aarrggbb, 16);
					m_colour.push_back(col);
				}
			}
		};

		// ELdrObject::LineBox
		template <> struct ObjectCreator<ELdrObject::LineBox> :IObjectCreatorLine
		{
			ObjectCreator() :IObjectCreatorLine(false, true) {}
			void Parse(ParseParams& p) override
			{
				pr::v4 dim;
				p.m_reader.ExtractReal(dim.x);
				if (p.m_reader.IsKeyword() || p.m_reader.IsSectionEnd()) dim.y = dim.x; else p.m_reader.ExtractReal(dim.y);
				if (p.m_reader.IsKeyword() || p.m_reader.IsSectionEnd()) dim.z = dim.y; else p.m_reader.ExtractReal(dim.z);
				dim *= 0.5f;

				m_point.push_back(pr::v4::make(-dim.x, -dim.y, -dim.z, 1.0f));
				m_point.push_back(pr::v4::make(+dim.x, -dim.y, -dim.z, 1.0f));
				m_point.push_back(pr::v4::make(+dim.x, +dim.y, -dim.z, 1.0f));
				m_point.push_back(pr::v4::make(-dim.x, +dim.y, -dim.z, 1.0f));
				m_point.push_back(pr::v4::make(-dim.x, -dim.y, +dim.z, 1.0f));
				m_point.push_back(pr::v4::make(+dim.x, -dim.y, +dim.z, 1.0f));
				m_point.push_back(pr::v4::make(+dim.x, +dim.y, +dim.z, 1.0f));
				m_point.push_back(pr::v4::make(-dim.x, +dim.y, +dim.z, 1.0f));

				pr::uint16 idx[] = { 0, 1, 1, 2, 2, 3, 3, 0, 4, 5, 5, 6, 6, 7, 7, 4, 0, 4, 1, 5, 2, 6, 3, 7 };
				m_index.insert(m_index.end(), idx, idx + PR_COUNTOF(idx));
			}
		};

		// ELdrObject::Grid
		template <> struct ObjectCreator<ELdrObject::Grid> :IObjectCreatorLine
		{
			ObjectCreator() :IObjectCreatorLine(false, false) {}
			void Parse(ParseParams& p) override
			{
				int axis_id;
				pr::v2 dim, div;
				p.m_reader.ExtractInt(axis_id, 10);
				p.m_reader.ExtractVector2(dim);
				if (p.m_reader.IsKeyword() || p.m_reader.IsSectionEnd()) div = dim; else p.m_reader.ExtractVector2(div);

				pr::v2 step = dim / div;
				for (float i = -dim.x / 2; i <= dim.x / 2; i += step.x)
				{
					m_point.push_back(pr::v4::make(i, -dim.y / 2, 0, 1));
					m_point.push_back(pr::v4::make(i, dim.y / 2, 0, 1));
				}
				for (float i = -dim.y / 2; i <= dim.y / 2; i += step.y)
				{
					m_point.push_back(pr::v4::make(-dim.x / 2, i, 0, 1));
					m_point.push_back(pr::v4::make(dim.x / 2, i, 0, 1));
				}
			}
		};

		// ELdrObject::Spline
		template <> struct ObjectCreator<ELdrObject::Spline> :IObjectCreatorLine
		{
			ObjectCreator() :IObjectCreatorLine(true, false) {}
			void Parse(ParseParams& p) override
			{
				pr::Spline spline;
				p.m_reader.ExtractVector3(spline.x, 1.0f);
				p.m_reader.ExtractVector3(spline.y, 1.0f);
				p.m_reader.ExtractVector3(spline.z, 1.0f);
				p.m_reader.ExtractVector3(spline.w, 1.0f);

				// Generate points for the spline
				pr::vector<pr::v4, 30, true> raster;
				pr::Raster(spline, raster, 30);
				m_point.insert(std::end(m_point), std::begin(raster), std::end(raster));

				if (m_per_line_colour)
				{
					pr::Colour32 col;
					p.m_reader.ExtractInt(col.m_aarrggbb, 16);
					for (size_t i = 0, iend = raster.size(); i != iend; ++i)
						m_colour.push_back(col);
				}
			}
		};

		// ELdrObject::Arrow
		template <> struct ObjectCreator<ELdrObject::Arrow> :IObjectCreatorLine
		{
			enum EArrowType { Invalid = -1, Line = 0, Fwd = 1 << 0, Back = 1 << 1, FwdBack = Fwd | Back };
			EArrowType m_type;

			ObjectCreator() :IObjectCreatorLine(true, false) ,m_type(EArrowType::Invalid) {}
			void Parse(ParseParams& p) override
			{
				// If no points read yet, expect the arrow type first
				if (m_type == EArrowType::Invalid)
				{
					string32 ty;
					p.m_reader.ExtractIdentifier(ty);
					if      (pr::str::EqualNI(ty, "Line"   )) m_type = EArrowType::Line;
					else if (pr::str::EqualNI(ty, "Fwd"    )) m_type = EArrowType::Fwd;
					else if (pr::str::EqualNI(ty, "Back"   )) m_type = EArrowType::Back;
					else if (pr::str::EqualNI(ty, "FwdBack")) m_type = EArrowType::FwdBack;
					else { p.m_reader.ReportError(pr::script::EResult::UnknownValue, "arrow type must one of Line, Fwd, Back, FwdBack"); return; }
				}
				else
				{
					pr::v4 pt;
					p.m_reader.ExtractVector3(pt, 1.0f);
					m_point.push_back(pt);

					if (m_per_line_colour)
					{
						pr::Colour32 col;
						p.m_reader.ExtractInt(col.m_aarrggbb, 16);
						m_colour.push_back(col);
					}
				}
			}
			void CreateModel(ParseParams& p, LdrObjectPtr obj) override
			{
				using namespace pr::rdr;
				
				// Validate
				if (m_point.size() < 2)
				{
					p.m_reader.ReportError(FmtS("Arrow object '%s' description incomplete", obj->TypeAndName().c_str()));
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

				// Colour interpolator iterator
				auto col = pr::CreateLerpRepeater(m_colour.data(), m_colour.size(), m_point.size(), pr::Colour32White);
				auto cc = [&](pr::Colour32 c) { props.m_has_alpha |= c.a() != 0xff; return c; };

				// Model bounding box
				auto bb = [&](v4 const& v) { pr::Encompass(props.m_bbox, v); return v; };

				// Generate the model
				// 'm_point' should contain line strip data
				ModelGenerator<>::Cont cont(m_point.size() + 2, m_point.size() + 2);
				auto v_in  = std::begin(m_point);
				auto v_out = std::begin(cont.m_vcont);
				auto i_out = std::begin(cont.m_icont);
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
				VBufferDesc vb(cont.m_vcont.size(), &cont.m_vcont[0]);
				IBufferDesc ib(cont.m_icont.size(), &cont.m_icont[0]);
				obj->m_model = p.m_rdr.m_mdl_mgr.CreateModel(MdlSettings(vb, ib, props.m_bbox));
				obj->m_model->m_name = obj->TypeAndName();

				// Get instances of the arrow head geometry shader and the thick line shader
				auto thk_shdr = p.m_rdr.m_shdr_mgr.FindShader(EStockShader::ThickLineListGS)->Clone<ThickLineListShaderGS>(AutoId, "thick_line");
				thk_shdr->m_default_width = m_line_width;
				auto arw_shdr = p.m_rdr.m_shdr_mgr.FindShader(EStockShader::ArrowHeadGS)->Clone<ArrowHeadShaderGS>(AutoId, "arrow_head");
				arw_shdr->m_default_width = m_line_width * 2;

				// Create nuggets
				NuggetProps nug;
				pr::rdr::Range vrange = {};
				pr::rdr::Range irange = {};
				if (m_type & EArrowType::Back)
				{
					vrange.set(0, 1);
					irange.set(0, 1);
					nug.m_topo = EPrim::PointList;
					nug.m_geom = EGeom::Vert|EGeom::Colr;
					nug.m_smap[ERenderStep::ForwardRender].m_gs = arw_shdr;
					nug.m_vrange = vrange;
					nug.m_irange = irange;
					SetAlphaBlending(nug, cont.m_vcont[0].m_diff.a != 1.0f);
					obj->m_model->CreateNugget(nug);
				}
				{
					vrange.set(vrange.m_end, vrange.m_end + m_point.size());
					irange.set(irange.m_end, irange.m_end + m_point.size());
					nug.m_topo = EPrim::LineStrip;
					nug.m_geom = EGeom::Vert|EGeom::Colr;
					nug.m_smap[ERenderStep::ForwardRender].m_gs = m_line_width != 0 ? static_cast<ShaderPtr>(thk_shdr) : ShaderPtr();
					nug.m_vrange = vrange;
					nug.m_irange = irange;
					SetAlphaBlending(nug, props.m_has_alpha);
					obj->m_model->CreateNugget(nug);
				}
				if (m_type & EArrowType::Fwd)
				{
					vrange.set(vrange.m_end, vrange.m_end + 1);
					irange.set(irange.m_end, irange.m_end + 1);
					nug.m_topo = EPrim::PointList;
					nug.m_geom = EGeom::Vert|EGeom::Colr;
					nug.m_smap[ERenderStep::ForwardRender].m_gs = arw_shdr;
					nug.m_vrange = vrange;
					nug.m_irange = irange;
					SetAlphaBlending(nug, cont.m_vcont.back().m_diff.a != 1.0f);
					obj->m_model->CreateNugget(nug);
				}
			}
		};

		// ELdrObject::Matrix3x3
		template <> struct ObjectCreator<ELdrObject::Matrix3x3> :IObjectCreatorLine
		{
			ObjectCreator() :IObjectCreatorLine(false, true) {}
			void Parse(ParseParams& p) override
			{
				pr::m4x4 basis;
				p.m_reader.ExtractMatrix3x3(cast_m3x4(basis));

				pr::v4       pts[] = { pr::v4Origin, basis.x.w1(), pr::v4Origin, basis.y.w1(), pr::v4Origin, basis.z.w1() };
				pr::Colour32 col[] = { pr::Colour32Red, pr::Colour32Red, pr::Colour32Green, pr::Colour32Green, pr::Colour32Blue, pr::Colour32Blue };
				pr::uint16   idx[] = { 0, 1, 2, 3, 4, 5 };

				m_point .insert(m_point .end(), pts, pts + PR_COUNTOF(pts));
				m_colour.insert(m_colour.end(), col, col + PR_COUNTOF(col));
				m_index .insert(m_index .end(), idx, idx + PR_COUNTOF(idx));
			}
		};

		#pragma endregion

		#pragma region Shapes2d

		// ELdrObject::Circle
		template <> struct ObjectCreator<ELdrObject::Circle> :IObjectCreatorShape2d
		{
			void Parse(ParseParams& p) override
			{
				p.m_reader.ExtractInt(m_axis_id.value, 10);
				p.m_reader.ExtractReal(m_dim.x);
				if (p.m_reader.IsKeyword() || p.m_reader.IsSectionEnd()) m_dim.y = m_dim.x; else p.m_reader.ExtractReal(m_dim.y);
			}
			void CreateModel(ParseParams& p, LdrObjectPtr obj) override
			{
				using namespace pr::rdr;

				pr::m4x4 o2w, *po2w = nullptr;
				if (m_axis_id != 3)
				{
					o2w = pr::Rotation4x4(pr::AxisId(3), m_axis_id, pr::v4Origin);
					po2w = &o2w;
				}

				// Create the model
				obj->m_model = ModelGenerator<>::Ellipse(p.m_rdr, m_dim.x, m_dim.y, m_solid, m_facets, Colour32White, po2w, GetDrawData());
				obj->m_model->m_name = obj->TypeAndName();
			}
		};

		// LdrObject::Pie
		template <> struct ObjectCreator<ELdrObject::Pie> :IObjectCreatorShape2d
		{
			pr::v2 m_ang, m_rad;

			ObjectCreator() :m_ang() ,m_rad() { m_dim = v4One; }
			bool ParseKeyword(ParseParams& p, EKeyword kw) override
			{
				switch (kw)
				{
				default: return IObjectCreatorShape2d::ParseKeyword(p, kw);
				case EKeyword::Scale: p.m_reader.ExtractVector2(m_dim.xy); return true;
				}
			}
			void Parse(ParseParams& p) override
			{
				p.m_reader.ExtractInt(m_axis_id.value, 10);
				p.m_reader.ExtractVector2(m_ang);
				p.m_reader.ExtractVector2(m_rad);

				if (m_ang.x == m_ang.y) m_ang.y += 360.0f;
				m_ang.x = pr::DegreesToRadians(m_ang.x);
				m_ang.y = pr::DegreesToRadians(m_ang.y);
			}
			void CreateModel(ParseParams& p, LdrObjectPtr obj) override
			{
				using namespace pr::rdr;

				pr::m4x4 o2w, *po2w = nullptr;
				if (m_axis_id != 3)
				{
					o2w = pr::Rotation4x4(pr::AxisId(3), m_axis_id, pr::v4Origin);
					po2w = &o2w;
				}

				// Create the model
				obj->m_model = ModelGenerator<>::Pie(p.m_rdr, m_dim.x, m_dim.y, m_ang.x, m_ang.y, m_rad.x, m_rad.y, m_solid, m_facets, Colour32White, po2w, GetDrawData());
				obj->m_model->m_name = obj->TypeAndName();
			}
		};

		// ELdrObject::Rect
		template <> struct ObjectCreator<ELdrObject::Rect> :IObjectCreatorShape2d
		{
			float m_corner_radius;

			ObjectCreator() :m_corner_radius() {}
			bool ParseKeyword(ParseParams& p, EKeyword kw) override
			{
				switch (kw)
				{
				default: return IObjectCreatorShape2d::ParseKeyword(p, kw);
				case EKeyword::CornerRadius: p.m_reader.ExtractRealS(m_corner_radius); return true;
				case EKeyword::Facets:       p.m_reader.ExtractIntS(m_facets, 10); m_facets *= 4; return true;
				}
			}
			void Parse(ParseParams& p) override
			{
				p.m_reader.ExtractInt(m_axis_id.value, 10);
				p.m_reader.ExtractReal(m_dim.x);
				if (p.m_reader.IsKeyword() || p.m_reader.IsSectionEnd()) m_dim.y = m_dim.x; else p.m_reader.ExtractReal(m_dim.y);
				m_dim *= 0.5f;
			}
			void CreateModel(ParseParams& p, LdrObjectPtr obj) override
			{
				using namespace pr::rdr;

				pr::m4x4 o2w, *po2w = nullptr;
				if (m_axis_id != 3)
				{
					o2w = pr::Rotation4x4(pr::AxisId(3), m_axis_id, pr::v4Origin);
					po2w = &o2w;
				}

				// Create the model
				obj->m_model = ModelGenerator<>::RoundedRectangle(p.m_rdr, m_dim.x, m_dim.y, m_corner_radius, m_solid, m_facets, Colour32White, po2w, GetDrawData());
				obj->m_model->m_name = obj->TypeAndName();
			}
		};

		// ELdrObject::Triangle
		template <> struct ObjectCreator<ELdrObject::Triangle> :IObjectCreatorPlane
		{
			void Parse(ParseParams& p) override
			{
				pr::v4 pt[3]; pr::Colour32 col[3];
				p.m_reader.ExtractVector3(pt[0], 1.0f) && (!m_per_vert_colour || p.m_reader.ExtractInt(col[0].m_aarrggbb, 16));
				p.m_reader.ExtractVector3(pt[1], 1.0f) && (!m_per_vert_colour || p.m_reader.ExtractInt(col[1].m_aarrggbb, 16));
				p.m_reader.ExtractVector3(pt[2], 1.0f) && (!m_per_vert_colour || p.m_reader.ExtractInt(col[2].m_aarrggbb, 16));
				m_point.push_back(pt[0]);
				m_point.push_back(pt[1]);
				m_point.push_back(pt[2]);
				m_point.push_back(pt[2]); // create a degenerate
				if (m_per_vert_colour)
				{
					m_colour.push_back(col[0]);
					m_colour.push_back(col[1]);
					m_colour.push_back(col[2]);
					m_colour.push_back(col[2]);
				}
			}
		};

		// ELdrObject::Quad
		template <> struct ObjectCreator<ELdrObject::Quad> :IObjectCreatorPlane
		{
			void Parse(ParseParams& p) override
			{
				pr::v4 pt[4]; pr::Colour32 col[4];
				p.m_reader.ExtractVector3(pt[0], 1.0f) && (!m_per_vert_colour || p.m_reader.ExtractInt(col[0].m_aarrggbb, 16));
				p.m_reader.ExtractVector3(pt[1], 1.0f) && (!m_per_vert_colour || p.m_reader.ExtractInt(col[1].m_aarrggbb, 16));
				p.m_reader.ExtractVector3(pt[2], 1.0f) && (!m_per_vert_colour || p.m_reader.ExtractInt(col[2].m_aarrggbb, 16));
				p.m_reader.ExtractVector3(pt[3], 1.0f) && (!m_per_vert_colour || p.m_reader.ExtractInt(col[3].m_aarrggbb, 16));
				m_point.push_back(pt[0]);
				m_point.push_back(pt[1]);
				m_point.push_back(pt[2]);
				m_point.push_back(pt[3]);
				if (m_per_vert_colour)
				{
					m_colour.push_back(col[0]);
					m_colour.push_back(col[1]);
					m_colour.push_back(col[2]);
					m_colour.push_back(col[3]);
				}
			}
		};

		// ELdrObject::Plane
		template <> struct ObjectCreator<ELdrObject::Plane> :IObjectCreatorPlane
		{
			void Parse(ParseParams& p) override
			{
				pr::v4 pnt, fwd; float w, h;
				p.m_reader.ExtractVector3(pnt, 1.0f);
				p.m_reader.ExtractVector3(fwd, 0.0f);
				p.m_reader.ExtractReal(w);
				p.m_reader.ExtractReal(h);

				fwd = pr::Normalise3(fwd);
				pr::v4 up = pr::Perpendicular(fwd);
				pr::v4 left = pr::Cross3(up, fwd);
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
			pr::AxisId m_axis_id;
			float m_width;
			bool m_smooth;
			int m_parm_index;

			ObjectCreator() :m_axis_id(3) ,m_width(10.0f) ,m_smooth(false) ,m_parm_index(0) {}
			bool ParseKeyword(ParseParams& p, EKeyword kw) override
			{
				switch (kw){
				default: return IObjectCreatorPlane::ParseKeyword(p, kw);
				case EKeyword::Smooth: m_smooth = true; return true;
				}
			}
			void Parse(ParseParams& p) override
			{
				switch (m_parm_index){
				case 0: // Axis id
					p.m_reader.ExtractInt(m_axis_id.value, 10);
					++m_parm_index;
					break;
				case 1: // Width
					p.m_reader.ExtractReal(m_width);
					++m_parm_index;
					break;
				case 2: // Points
					pr::v4 pt;
					p.m_reader.ExtractVector3(pt, 1.0f);
					m_point.push_back(pt);

					if (m_per_vert_colour)
					{
						pr::Colour32 col;
						p.m_reader.ExtractInt(col.m_aarrggbb, 16);
						m_colour.push_back(col);
					}
					break;
				}
			}
			void CreateModel(ParseParams& p, LdrObjectPtr obj) override
			{
				using namespace pr::rdr;

				// Validate
				if (m_point.size() < 2)
				{
					p.m_reader.ReportError("Object description incomplete");
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
				obj->m_model = ModelGenerator<>::QuadStrip(p.m_rdr, m_point.size() - 1, m_point.data(), m_width, 1, &normal, m_colour.size(), m_colour.data(), GetDrawData());
				obj->m_model->m_name = obj->TypeAndName();
			}
		};

		#pragma endregion

		#pragma region Shapes3d

		// ELdrObject::Box
		template <> struct ObjectCreator<ELdrObject::Box> :IObjectCreatorTexture
		{
			pr::v4 m_dim;

			ObjectCreator() :m_dim() {}
			void Parse(ParseParams& p) override
			{
				p.m_reader.ExtractReal(m_dim.x);
				if (p.m_reader.IsKeyword() || p.m_reader.IsSectionEnd()) m_dim.y = m_dim.x; else p.m_reader.ExtractReal(m_dim.y);
				if (p.m_reader.IsKeyword() || p.m_reader.IsSectionEnd()) m_dim.z = m_dim.y; else p.m_reader.ExtractReal(m_dim.z);
				m_dim *= 0.5f;
			}
			void CreateModel(ParseParams& p, LdrObjectPtr obj) override
			{
				// Create the model
				obj->m_model = ModelGenerator<>::Box(p.m_rdr, m_dim, pr::m4x4Identity, pr::Colour32White, GetDrawData());
				obj->m_model->m_name = obj->TypeAndName();
			}
		};

		// ELdrObject::BoxLine
		template <> struct ObjectCreator<ELdrObject::BoxLine> :IObjectCreatorTexture
		{
			pr::m4x4 m_b2w;
			pr::v4 m_dim, m_up;

			ObjectCreator() :m_b2w(), m_dim(), m_up(pr::v4YAxis) {}
			bool ParseKeyword(ParseParams& p, EKeyword kw) override
			{
				switch (kw) {
				default: return IObjectCreatorTexture::ParseKeyword(p, kw);
				case EKeyword::Up: p.m_reader.ExtractVector3S(m_up, 0.0f); return true;
				}
			}
			void Parse(ParseParams& p) override
			{
				float w = 0.1f, h = 0.1f;
				pr::v4 s0, s1;
				p.m_reader.ExtractVector3(s0, 1.0f);
				p.m_reader.ExtractVector3(s1, 1.0f);
				p.m_reader.ExtractReal(w);
				if (p.m_reader.IsKeyword() || p.m_reader.IsSectionEnd()) h = w; else p.m_reader.ExtractReal(h);
				m_dim.set(w, h, pr::Length3(s1 - s0), 0.0f);
				m_dim *= 0.5f;
				m_b2w = pr::OriFromDir(s1 - s0, 2, m_up, (s1 + s0) * 0.5f);
			}
			void CreateModel(ParseParams& p, LdrObjectPtr obj) override
			{
				obj->m_model = ModelGenerator<>::Box(p.m_rdr, m_dim, m_b2w, pr::Colour32White, GetDrawData());
				obj->m_model->m_name = obj->TypeAndName();
			}
		};

		// ELdrObject::BoxList
		template <> struct ObjectCreator<ELdrObject::BoxList> :IObjectCreatorTexture
		{
			pr::vector<pr::v4, 16> m_location;
			pr::v4 m_dim;

			ObjectCreator() :m_location(), m_dim() {}
			void Parse(ParseParams& p) override
			{
				pr::v4 v;
				p.m_reader.ExtractVector3(v, 1.0f);
				if (m_dim == pr::v4Zero)
					m_dim = v.w0();
				else
					m_location.push_back(v);
			}
			void CreateModel(ParseParams& p, LdrObjectPtr obj) override
			{
				// Validate
				if (m_dim == pr::v4Zero || m_location.size() == 0)
				{
					p.m_reader.ReportError("Box list object description incomplete");
					return;
				}

				m_dim *= 0.5f;

				// Create the model
				obj->m_model = ModelGenerator<>::BoxList(p.m_rdr, m_location.size(), m_location.data(), m_dim, 0, 0, GetDrawData());
				obj->m_model->m_name = obj->TypeAndName();
			}
		};

		// ELdrObject::FrustumWH
		template <> struct ObjectCreator<ELdrObject::FrustumWH> :IObjectCreatorCuboid
		{
			float m_width, m_height, m_near, m_far, m_view_plane;
			pr::AxisId m_axis_id;

			ObjectCreator() :m_width(1.0f), m_height(1.0f), m_near(0.0f), m_far(1.0f), m_view_plane(1.0f), m_axis_id(3) {}
			bool ParseKeyword(ParseParams& p, EKeyword kw) override
			{
				switch (kw) {
				default: return IObjectCreatorCuboid::ParseKeyword(p, kw);
				case EKeyword::ViewPlaneZ: p.m_reader.ExtractRealS(m_view_plane); return true;
				}
			}
			void Parse(ParseParams& p) override
			{
				p.m_reader.ExtractInt(m_axis_id.value, 10);
				p.m_reader.ExtractReal(m_width);
				p.m_reader.ExtractReal(m_height);
				p.m_reader.ExtractReal(m_near);
				p.m_reader.ExtractReal(m_far);
			}
			void CreateModel(ParseParams& p, LdrObjectPtr obj) override
			{
				float w = m_width  * 0.5f / m_view_plane;
				float h = m_height * 0.5f / m_view_plane;
				float n = m_near, f = m_far;

				m_pt[0].set(-n*w, -n*h, n, 1.0f);
				m_pt[1].set(-n*w, n*h, n, 1.0f);
				m_pt[2].set(n*w, -n*h, n, 1.0f);
				m_pt[3].set(n*w, n*h, n, 1.0f);
				m_pt[4].set(f*w, -f*h, f, 1.0f);
				m_pt[5].set(f*w, f*h, f, 1.0f);
				m_pt[6].set(-f*w, -f*h, f, 1.0f);
				m_pt[7].set(-f*w, f*h, f, 1.0f);

				switch (m_axis_id){
				default: p.m_reader.ReportError(pr::script::EResult::UnknownValue, "axis_id must one of ±1, ±2, ±3"); return;
				case  1: m_b2w = pr::Rotation4x4(0.0f, -pr::maths::tau_by_4, 0.0f, pr::v4Origin); break;
				case -1: m_b2w = pr::Rotation4x4(0.0f, pr::maths::tau_by_4, 0.0f, pr::v4Origin); break;
				case  2: m_b2w = pr::Rotation4x4(-pr::maths::tau_by_4, 0.0f, 0.0f, pr::v4Origin); break;
				case -2: m_b2w = pr::Rotation4x4(pr::maths::tau_by_4, 0.0f, 0.0f, pr::v4Origin); break;
				case  3: m_b2w = pr::m4x4Identity; break;
				case -3: m_b2w = pr::Rotation4x4(0.0f, pr::maths::tau_by_2, 0.0f, pr::v4Origin); break;
				}

				IObjectCreatorCuboid::CreateModel(p, obj);
			}
		};

		// ELdrObject::FrustumFA
		template <> struct ObjectCreator<ELdrObject::FrustumFA> :IObjectCreatorCuboid
		{
			float m_fovY, m_aspect, m_near, m_far;
			pr::AxisId m_axis_id;

			ObjectCreator() :m_fovY(pr::maths::tau_by_8), m_aspect(1.0f), m_near(0.0f), m_far(1.0f), m_axis_id(3) {}
			void Parse(ParseParams& p) override
			{
				p.m_reader.ExtractInt(m_axis_id.value, 10);
				p.m_reader.ExtractReal(m_fovY);
				p.m_reader.ExtractReal(m_aspect);
				p.m_reader.ExtractReal(m_near);
				p.m_reader.ExtractReal(m_far);
			}
			void CreateModel(ParseParams& p, LdrObjectPtr obj) override
			{
				// Construct pointed down +z, then rotate the points based on axis id
				float h = pr::Tan(pr::DegreesToRadians(m_fovY * 0.5f));
				float w = m_aspect * h;
				float n = m_near, f = m_far;
				m_pt[0].set(-n*w, -n*h, n, 1.0f);
				m_pt[1].set(n*w, -n*h, n, 1.0f);
				m_pt[2].set(-n*w, n*h, n, 1.0f);
				m_pt[3].set(n*w, n*h, n, 1.0f);
				m_pt[4].set(-f*w, -f*h, f, 1.0f);
				m_pt[5].set(f*w, -f*h, f, 1.0f);
				m_pt[6].set(-f*w, f*h, f, 1.0f);
				m_pt[7].set(f*w, f*h, f, 1.0f);

				switch (m_axis_id) {
				default: p.m_reader.ReportError(pr::script::EResult::UnknownValue, "axis_id must one of ±1, ±2, ±3"); return;
				case  1: m_b2w = pr::Rotation4x4(0.0f, pr::maths::tau_by_4, 0.0f, pr::v4Origin); break;
				case -1: m_b2w = pr::Rotation4x4(0.0f, -pr::maths::tau_by_4, 0.0f, pr::v4Origin); break;
				case  2: m_b2w = pr::Rotation4x4(-pr::maths::tau_by_4, 0.0f, 0.0f, pr::v4Origin); break;
				case -2: m_b2w = pr::Rotation4x4(pr::maths::tau_by_4, 0.0f, 0.0f, pr::v4Origin); break;
				case  3: m_b2w = pr::m4x4Identity; break;
				case -3: m_b2w = pr::Rotation4x4(0.0f, pr::maths::tau_by_2, 0.0f, pr::v4Origin); break;
				}

				IObjectCreatorCuboid::CreateModel(p, obj);
			}
		};

		// ELdrObject::Sphere
		template <> struct ObjectCreator<ELdrObject::Sphere> :IObjectCreatorTexture
		{
			pr::v4 m_dim;
			int m_divisions;

			ObjectCreator() :m_dim(), m_divisions(3) {}
			bool ParseKeyword(ParseParams& p, EKeyword kw) override
			{
				switch (kw) {
				default: return IObjectCreatorTexture::ParseKeyword(p, kw);
				case EKeyword::Divisions: p.m_reader.ExtractInt(m_divisions, 10); return true;
				}
			}
			void Parse(ParseParams& p) override
			{
				p.m_reader.ExtractReal(m_dim.x);
				if (p.m_reader.IsKeyword() || p.m_reader.IsSectionEnd()) m_dim.y = m_dim.x; else p.m_reader.ExtractReal(m_dim.y);
				if (p.m_reader.IsKeyword() || p.m_reader.IsSectionEnd()) m_dim.z = m_dim.y; else p.m_reader.ExtractReal(m_dim.z);
			}
			void CreateModel(ParseParams& p, LdrObjectPtr obj) override
			{
				obj->m_model = ModelGenerator<>::Geosphere(p.m_rdr, m_dim, m_divisions, pr::Colour32White, GetDrawData());
				obj->m_model->m_name = obj->TypeAndName();
			}
		};

		// ELdrObject::CylinderHR
		template <> struct ObjectCreator<ELdrObject::CylinderHR> :IObjectCreatorCone
		{
			void Parse(ParseParams& p) override
			{
				p.m_reader.ExtractInt(m_axis_id.value, 10);
				p.m_reader.ExtractReal(m_dim.z);
				p.m_reader.ExtractReal(m_dim.x);
				if (p.m_reader.IsKeyword() || p.m_reader.IsSectionEnd()) m_dim.y = m_dim.x; else p.m_reader.ExtractReal(m_dim.y);
			}
		};

		// ELdrObject::ConeHA
		template <> struct ObjectCreator<ELdrObject::ConeHA> :IObjectCreatorCone
		{
			void Parse(ParseParams& p) override
			{
				float h0, h1, a;
				p.m_reader.ExtractInt(m_axis_id.value, 10);
				p.m_reader.ExtractReal(h0);
				p.m_reader.ExtractReal(h1);
				p.m_reader.ExtractReal(a);

				m_dim.z = h1 - h0;
				m_dim.x = h0 * pr::Tan(a);
				m_dim.y = h1 * pr::Tan(a);
			}
		};
		// ELdrObject::Mesh
		template <> struct ObjectCreator<ELdrObject::Mesh> :IObjectCreatorMesh
		{
			void Parse(ParseParams& p) override
			{
				p.m_reader.ReportError(pr::script::EResult::UnknownValue, "Mesh object description invalid");
				p.m_reader.FindSectionEnd();
			}
		};

		// ELdrObject::ConvexHull
		template <> struct ObjectCreator<ELdrObject::ConvexHull> :IObjectCreatorMesh
		{
			ObjectCreator()
			{
				m_prim_type = EPrim::TriList;
				m_gen_normals = 0.0f;
			}
			void Parse(ParseParams& p) override
			{
				p.m_reader.ReportError(pr::script::EResult::UnknownValue, "Convext hull object description invalid");
				p.m_reader.FindSectionEnd();
			}
			void CreateModel(ParseParams& p, LdrObjectPtr obj) override
			{
				// Allocate space for the face indices
				m_indices.resize(6 * (m_verts.size() - 2));

				// Find the convex hull
				size_t num_verts = 0, num_faces = 0;
				pr::ConvexHull(m_verts, m_verts.size(), &m_indices[0], &m_indices[0] + m_indices.size(), num_verts, num_faces);
				m_verts.resize(num_verts);
				m_indices.resize(3 * num_faces);

				IObjectCreatorMesh::CreateModel(p, obj);
			}
		};

		// ELdrObject::Model
		template <> struct ObjectCreator<ELdrObject::Model> :IObjectCreator
		{
			string512 m_filepath;
			m4x4 m_bake;
			float m_gen_normals;

			ObjectCreator() :m_filepath() ,m_bake(m4x4Identity) ,m_gen_normals(-1.0f) {}
			bool ParseKeyword(ParseParams& p, EKeyword kw) override
			{
				switch (kw) {
				default: return IObjectCreator::ParseKeyword(p, kw);
				case EKeyword::GenerateNormals:
					{
						p.m_reader.ExtractRealS(m_gen_normals);
						m_gen_normals = pr::DegreesToRadians(m_gen_normals);
						return true;
					}
				case EKeyword::BakeTransform:
					{
						ParseTransform(p.m_reader, m_bake);
						return true;
					}
				}
			}
			void Parse(ParseParams& p) override
			{
				p.m_reader.ExtractString(m_filepath);
			}
			void CreateModel(ParseParams& p, LdrObjectPtr obj) override
			{
				using namespace pr::rdr;
				using namespace pr::geometry;

				// Validate
				if (m_filepath.empty())
				{
					p.m_reader.ReportError("Model filepath not given");
					return;
				}

				// Determine the format from the file extension
				ModelFileInfo info = GetModelFileInfo(m_filepath.c_str());
				if (info.m_format == EModelFileFormat::Unknown)
				{
					string512 msg = pr::Fmt("Mesh file '%s' is not supported.\nSupported Formats: ", m_filepath.c_str());
					for (auto f : EModelFileFormat::MemberNames()) msg.append(f).append(" ");
					p.m_reader.ReportError(msg.c_str());
					return;
				}

				// Ask the include handler to turn the filepath into a stream
				auto src = p.m_reader.IncludeHandler()->OpenStream(m_filepath, info.m_is_binary);
				if (!src || !*src)
				{
					p.m_reader.ReportError(pr::FmtS("Failed to open file stream '%s'", m_filepath.c_str()));
					return;
				}

				// Create the model
				obj->m_model = ModelGenerator<>::LoadModel(p.m_rdr, info.m_format, *src, nullptr, m_bake != m4x4Identity ? &m_bake : nullptr, m_gen_normals);
				obj->m_model->m_name = obj->TypeAndName();
			}
		};

		#pragma endregion

		#pragma region Special Objects

		// ELdrObject::DirectionalLight
		template <> struct ObjectCreator<ELdrObject::DirectionalLight> :IObjectCreatorLight
		{
			void Parse(ParseParams& p) override
			{
				p.m_reader.ExtractVector3(m_light.m_direction, 0.0f);
			}
		};

		// ELdrObject::PointLight
		template <> struct ObjectCreator<ELdrObject::PointLight> :IObjectCreatorLight
		{
			void Parse(ParseParams& p) override
			{
				p.m_reader.ExtractVector3(m_light.m_position, 1.0f);
			}
		};

		// ELdrObject::SpotLight
		template <> struct ObjectCreator<ELdrObject::SpotLight> :IObjectCreatorLight
		{
			void Parse(ParseParams& p) override
			{
				p.m_reader.ExtractVector3(m_light.m_position, 1.0f);
				p.m_reader.ExtractVector3(m_light.m_direction, 0.0f);
				p.m_reader.ExtractReal(m_light.m_inner_cos_angle); // actually in degress atm
				p.m_reader.ExtractReal(m_light.m_outer_cos_angle); // actually in degress atm
			}
		};

		//ELdrObject::Group
		template <> struct ObjectCreator<ELdrObject::Group> :IObjectCreator
		{
			void CreateModel(ParseParams&, LdrObjectPtr obj) override
			{
				// Object modifiers applied to groups are applied recursively to children within the group
				// Apply colour to all children
				if (obj->m_colour_mask != 0)
					obj->SetColour(obj->m_base_colour, obj->m_colour_mask, "");

				// Apply wireframe to all children
				if (obj->m_wireframe)
					obj->Wireframe(obj->m_wireframe, "");

				// Apply visibility to all children
				if (!obj->m_visible)
					obj->Visible(obj->m_visible, "");
			}
		};

		// ELdrObject::Instance
		template <> struct ObjectCreator<ELdrObject::Instance> :IObjectCreator
		{
			void CreateModel(ParseParams& p, LdrObjectPtr obj) override
			{
				// Locate the model that this is an instance of
				auto model_key = pr::hash::HashC(obj->m_name.c_str());
				auto mdl = p.m_models.find(model_key);
				if (mdl == p.m_models.end())
				{
					p.m_reader.ReportError(pr::script::EResult::UnknownValue, "Instance not found");
					return;
				}
				obj->m_model = mdl->second;
			}
		};

		#pragma endregion

		// Parse an ldr object of type 'ShapeType'
		// Note: not using an output iterator style callback because model instancing
		// relies on the map from object to model.
		template <ELdrObject::Enum_ ShapeType> void Parse(ParseParams& p)
		{
			// Read the object attributes: name, colour, instance
			ObjectAttributes attr = ParseAttributes(p.m_reader, ShapeType);
			LdrObjectPtr obj(new LdrObject(attr, p.m_parent, p.m_context_id));
			ObjectCreator<ShapeType> creator;

			// Read the description of the model
			p.m_reader.SectionStart();
			while (!p.m_reader.IsSectionEnd())
			{
				if (p.m_reader.IsKeyword())
				{
					// Interpret child objects
					auto kw = p.m_reader.NextKeywordH();
					ParseParams pp(p, obj->m_child, kw, obj.m_ptr);
					if (ParseLdrObject(pp)) continue;
					if (ParseProperties(pp, (EKeyword)kw, obj)) continue;
					if (creator.ParseKeyword(p, (EKeyword)kw)) continue;
					p.m_reader.ReportError(pr::script::EResult::UnknownToken);
					continue;
				}
				else
				{
					creator.Parse(p);
				}
			}
			p.m_reader.SectionEnd();

			// Create the model
			creator.CreateModel(p, obj);

			// Add the model and instance to the containers
			p.m_models[pr::hash::HashC(obj->m_name.c_str())] = obj->m_model;
			p.m_objects.push_back(obj);
		}

		// Apply the states such as colour,wireframe,etc to the objects renderer model
		void ApplyObjectState(LdrObjectPtr obj)
		{
			// Set colour on 'obj' (so that render states are set correctly)
			// Note that the colour is 'blended' with 'm_base_colour' so m_base_colour * White = m_base_colour.
			obj->SetColour(obj->m_base_colour, 0xFFFFFFFF);

			// Apply the colour of 'obj' to all children using a mask
			if (obj->m_colour_mask != 0)
				obj->SetColour(obj->m_base_colour, obj->m_colour_mask, "");

			// If flagged as wireframe, set wireframe
			if (obj->m_wireframe)
				obj->Wireframe(true);

			// If flagged as hidden, hide
			if (!obj->m_visible)
				obj->Visible(false);
		}

		// Reads a single ldr object from a script adding object (+ children) to 'p.m_objects'.
		// Returns true if an object was read or false if the next keyword is unrecognised
		bool ParseLdrObject(ParseParams& p)
		{
			auto object_count = p.m_objects.size();
			auto kw = (ELdrObject::Enum_)p.m_keyword;
			switch (kw)
			{
			default: return false;
			case ELdrObject::Line:             Parse<ELdrObject::Line            >(p); break;
			case ELdrObject::LineD:            Parse<ELdrObject::LineD           >(p); break;
			case ELdrObject::LineStrip:        Parse<ELdrObject::LineStrip       >(p); break;
			case ELdrObject::LineBox:          Parse<ELdrObject::LineBox         >(p); break;
			case ELdrObject::Grid:             Parse<ELdrObject::Grid            >(p); break;
			case ELdrObject::Spline:           Parse<ELdrObject::Spline          >(p); break;
			case ELdrObject::Arrow:            Parse<ELdrObject::Arrow           >(p); break;
			case ELdrObject::Circle:           Parse<ELdrObject::Circle          >(p); break;
			case ELdrObject::Rect:             Parse<ELdrObject::Rect            >(p); break;
			case ELdrObject::Pie:              Parse<ELdrObject::Pie             >(p); break;
			case ELdrObject::Matrix3x3:        Parse<ELdrObject::Matrix3x3       >(p); break;
			case ELdrObject::Triangle:         Parse<ELdrObject::Triangle        >(p); break;
			case ELdrObject::Quad:             Parse<ELdrObject::Quad            >(p); break;
			case ELdrObject::Plane:            Parse<ELdrObject::Plane           >(p); break;
			case ELdrObject::Ribbon:           Parse<ELdrObject::Ribbon          >(p); break;
			case ELdrObject::Box:              Parse<ELdrObject::Box             >(p); break;
			case ELdrObject::BoxLine:          Parse<ELdrObject::BoxLine         >(p); break;
			case ELdrObject::BoxList:          Parse<ELdrObject::BoxList         >(p); break;
			case ELdrObject::FrustumWH:        Parse<ELdrObject::FrustumWH       >(p); break;
			case ELdrObject::FrustumFA:        Parse<ELdrObject::FrustumFA       >(p); break;
			case ELdrObject::Sphere:           Parse<ELdrObject::Sphere          >(p); break;
			case ELdrObject::CylinderHR:       Parse<ELdrObject::CylinderHR      >(p); break;
			case ELdrObject::ConeHA:           Parse<ELdrObject::ConeHA          >(p); break;
			case ELdrObject::Mesh:             Parse<ELdrObject::Mesh            >(p); break;
			case ELdrObject::ConvexHull:       Parse<ELdrObject::ConvexHull      >(p); break;
			case ELdrObject::Model:            Parse<ELdrObject::Model           >(p); break;
			case ELdrObject::DirectionalLight: Parse<ELdrObject::DirectionalLight>(p); break;
			case ELdrObject::PointLight:       Parse<ELdrObject::PointLight      >(p); break;
			case ELdrObject::SpotLight:        Parse<ELdrObject::SpotLight       >(p); break;
			case ELdrObject::Group:            Parse<ELdrObject::Group           >(p); break;
			case ELdrObject::Instance:         Parse<ELdrObject::Instance        >(p); break;
			}

			// Apply properties to each object added
			for (auto i = object_count, iend = p.m_objects.size(); i != iend; ++i)
				ApplyObjectState(p.m_objects[i]);

			return true;
		}

		// Reads all ldr objects from a script returning 'result'
		template <typename AddCB>
		void ParseLdrObjects(pr::Renderer& rdr, pr::script::Reader& reader, ContextId context_id, ParseResult& result, AddCB add_cb)
		{
			// Your application needs to have called CoInitialise() before here
			bool cancel = false;
			for (EKeyword kw; !cancel && reader.NextKeywordH(kw);)
			{
				switch (kw)
				{
				default:
					{
						auto object_count = result.m_objects.size();
						ParseParams pp(rdr, reader, result.m_objects, result.m_models, context_id, kw, nullptr);
						if (!ParseLdrObject(pp))
						{
							reader.ReportError(pr::script::EResult::UnknownToken);
							break;
						}

						// Notify of an object added. Cancel if 'add_cb' returns false
						cancel = !add_cb(result.m_objects[object_count]);
						break;
					}

				// Camera position description
				case EKeyword::Camera:
					{
						ParseCamera(reader, result);
						break;
					}

				// Application commands
				case EKeyword::Clear:
					{
						// Clear resets the scene up to the point of the clear, that includes
						// objects we may have already parsed. A use for this is for a script
						// that might be a work in progress, *Clear can be used to remove everything
						// above a point in the script.
						result.m_objects.clear();
						result.m_clear = true;
						break;
					}
				case EKeyword::Wireframe:
					{
						result.m_wireframe = true;
						break;
					}
				case EKeyword::Lock: break;
				case EKeyword::Delimiters: break;
				}
			}

			// Release scratch buffers
			g_cache.release();
		}

		// Parse the ldr script in 'reader' adding the results to 'out'
		// If 'async' is true, a progress dialog is displayed and parsing is done in a background thread.
		void Parse(pr::Renderer& rdr, pr::script::Reader& reader, ParseResult& out, bool async, ContextId context_id)
		{
			// Does the work of parsing objects and adds them to 'models'
			// 'total' is the total number of objects added
			auto ParseObjects = [&](pr::gui::ProgressDlg* dlg, ParseResult& out)
			{
				// Note: your application needs to have called CoInitialise() before here
				std::size_t start_time = GetTickCount();
				std::size_t last_update = start_time;
				ParseLdrObjects(rdr, reader, context_id, out, [&](LdrObjectPtr& obj)
					{
						// See if it's time for a progress update
						if (dlg == nullptr) return true;
						auto now = GetTickCount();
						if (now - start_time < 200 || now - last_update < 100)
							return true;

						last_update = now;
						char const* type = obj ? ELdrObject::ToString(obj->m_type) : "";
						std::string name = obj ? obj->m_name : "";
						return dlg->Progress(-1.0f, pr::FmtS("Parsing scene...\r\nObject count: %d\r\n%s %s", out.m_objects.size(), type, name.c_str()));
					});
			};

			if (async)
			{
				// Run the adding process as a background task while displaying a progress dialog
				pr::gui::ProgressDlg dlg("Processing script", "", ParseObjects, std::ref(out));
				dlg.DoModal(100);
			}
			else
			{
				ParseObjects(nullptr, out);
			}
		}

		// Add a custom object via callback
		// Objects created by this method will have dynamic usage and are suitable for updating every frame
		// They are intended to be used with the 'Edit' function.
		LdrObjectPtr Add(pr::Renderer& rdr, ObjectAttributes attr, EPrim topo, int icount, int vcount, pr::uint16 const* indices, pr::v4 const* verts, int ccount, pr::Colour32 const* colours, int ncount, pr::v4 const* normals, pr::v2 const* tex_coords, ContextId context_id)
		{
			LdrObjectPtr obj(new LdrObject(attr, 0, context_id));

			EGeom  geom_type = EGeom::Vert;
			if (normals)    geom_type |= EGeom::Norm;
			if (colours)    geom_type |= EGeom::Colr;
			if (tex_coords) geom_type |= EGeom::Tex0;

			// Create the model
			NuggetProps mat(topo, geom_type);
			obj->m_model = ModelGenerator<>::Mesh(rdr, topo, vcount, icount, verts, indices, ccount, colours, ncount, normals, tex_coords, &mat);
			obj->m_model->m_name = obj->TypeAndName();
			return obj;
		}

		// Add a custom object via callback
		// Objects created by this method will have dynamic usage and are suitable for updating every frame
		// They are intended to be used with the 'Edit' function.
		LdrObjectPtr Add(pr::Renderer& rdr, ObjectAttributes attr, int icount, int vcount, EditObjectCB edit_cb, void* ctx, ContextId context_id)
		{
			LdrObjectPtr obj(new LdrObject(attr, 0, context_id));

			// Create buffers for a dynamic model
			VBufferDesc vbs(vcount, sizeof(Vert), D3D11_USAGE_DYNAMIC, D3D11_CPU_ACCESS_WRITE);
			IBufferDesc ibs(icount, sizeof(pr::uint16), DxFormat<pr::uint16>::value, D3D11_USAGE_DYNAMIC, D3D11_CPU_ACCESS_WRITE);
			MdlSettings settings(vbs, ibs);

			// Create the model
			obj->m_model = rdr.m_mdl_mgr.CreateModel(settings);
			obj->m_model->m_name = obj->TypeAndName();

			// Initialise it via the callback
			edit_cb(obj->m_model, ctx, rdr);
			return obj;
		}

		// Modify the geometry of an LdrObject
		void Edit(pr::Renderer& rdr, LdrObjectPtr object, EditObjectCB edit_cb, void* ctx)
		{
			edit_cb(object->m_model, ctx, rdr);
			pr::events::Send(Evt_LdrObjectChg(object));
		}

		// Update 'object' with info from 'desc'. 'keep' describes the properties of 'object' to update
		void Update(pr::Renderer& rdr, LdrObjectPtr object, char const* desc, EUpdateObject flags)
		{
			// Parse 'desc' for the new model
			pr::script::Loc loc("UpdateObject", 0, 0);
			pr::script::PtrSrc src(desc, &loc);
			pr::script::Reader reader;
			reader.AddSource(src);

			ParseResult result;
			ParseLdrObjects(rdr, reader, object->m_context_id, result, [&](LdrObjectPtr rhs)
			{
				// Want the first root level object
				if (rhs->m_parent != nullptr)
					return true;

				// Swap the bits we want from 'rhs'
				// Note: we can't swap everything then copy back the bits we want to keep
				// because LdrObject is reference counted and isn't copyable. This is risky
				// though, if new members are added I'm bound to forget to consider them here :-/
				// Commented out parts are those delibrately kept

				// RdrInstance
				if (flags & EUpdateObject::Model)
				{
					std::swap(object->m_model, rhs->m_model);
					std::swap(object->m_sko, rhs->m_sko);
					std::swap(object->m_bsb, rhs->m_bsb);
					std::swap(object->m_dsb, rhs->m_dsb);
					std::swap(object->m_rsb, rhs->m_rsb);
				}
				if (flags & EUpdateObject::Transform)
					std::swap(object->m_i2w, rhs->m_i2w);
				if (flags & EUpdateObject::Colour)
					std::swap(object->m_colour, rhs->m_colour);

				// LdrObject
				std::swap(object->m_type, rhs->m_type);
				if (flags & EUpdateObject::Name)
					std::swap(object->m_name, rhs->m_name);
				if (flags & EUpdateObject::Transform)
					std::swap(object->m_o2p, rhs->m_o2p);
				if (flags & EUpdateObject::Wireframe)
					std::swap(object->m_wireframe, rhs->m_wireframe);
				if (flags & EUpdateObject::Visibility)
					std::swap(object->m_visible, rhs->m_visible);
				if (flags & EUpdateObject::Animation)
					std::swap(object->m_anim, rhs->m_anim);
				if (flags & EUpdateObject::StepData)
					std::swap(object->m_step, rhs->m_step);
				if (flags & EUpdateObject::ColourMask)
					std::swap(object->m_colour_mask, rhs->m_colour_mask);
				if (flags & EUpdateObject::Colour)
					std::swap(object->m_base_colour, rhs->m_base_colour);

				// Transfer the child objects
				if (flags & EUpdateObject::Children)
				{
					object->RemoveAllChildren();
					while (!rhs->m_child.empty())
						object->AddChild(rhs->RemoveChild(0));
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
		void Remove(ObjectCont& objects, ContextId const* doomed, std::size_t dcount, ContextId const* excluded, std::size_t ecount)
		{
			ContextId const* dend = doomed + dcount;
			ContextId const* eend = excluded + ecount;
			for (size_t i = objects.size(); i-- != 0;)
			{
				if (doomed   && std::find(doomed, dend, objects[i]->m_context_id) == dend) continue; // not in the doomed list
				if (excluded && std::find(excluded, eend, objects[i]->m_context_id) != eend) continue; // saved by exclusion
				objects.erase(objects.begin() + i);
			}
		}

		// Remove 'obj' from 'objects'
		void Remove(ObjectCont& objects, LdrObjectPtr obj)
		{
			for (auto i = objects.begin(), iend = objects.end(); i != iend; ++i)
			{
				if (*i != obj) continue;
				objects.erase(i);
				break;
			}
		}

		// Parse the source data in 'reader' using the same syntax
		// as we use for ldr object '*o2w' transform descriptions.
		// The source should begin with '{' and end with '}', i.e. *o2w { ... } with the *o2w already read
		pr::m4x4 ParseLdrTransform(pr::script::Reader& reader)
		{
			pr::m4x4 o2w = pr::m4x4Identity;
			ParseTransform(reader, o2w);
			return o2w;
		}

		// Generate a scene that demos the supported object types and modifers.
		std::string CreateDemoScene()
		{
			std::stringstream out; out <<
R"(//********************************************
// LineDrawer demo scene
//  Copyright (c) Rylogic Ltd 2009
//********************************************
//
// Notes:
//  axis_id is an integer describing an axis number. It must one of ±1, ±2, ±3
//  corresponding to ±X, ±Y, ±Z respectively

// Clear existing data
*Clear /*{ctx_id ...}*/ // Context ids can be listed within a section

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

// An example of applying a transform to an object.
// All objects have an implicit object-to-parent transform that is identity.
// Successive 'o2w' sections premultiply this transform for the object.
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

// There are a number of other object modifiers that can also be used:
*Box obj_modifier_example FFFF0000
{
	0.2 0.5 0.4
	*Colour {FFFF00FF}       // Override the base colour of the model
	*ColourMask {FF000000}   // applies: 'child.colour = (obj.base_colour & mask) | (child.base_colour & ~mask)' to all children recursively
	*RandColour              // Apply a random colour to this object
	*Animation               // Add simple animation to this object
	{
		*Style PingPong      // Animation style, one of: NoAnimation, PlayOnce, PlayReverse, PingPong, PlayContinuous
		*Period 1.2          // The period of the animation in seconds
		*Velocity 1 1 1      // Linear velocity vector in m/s
		*AngVelocity 1 0 0   // Angular velocity vector in rad/s
	}
	*Hidden                  // Object is created in an invisible state
	*Wireframe               // Object is created with wireframe fill mode
	*Texture                 // Texture (only supported on some object types)
	{
		"#checker"          // texture filepath, stock texture name (e.g. #white, #black, #checker), or texture id (e.g. #1, #3)
		*Addr {Clamp Clamp} // Optional addressing mode for the texture; U, V. Options: Wrap, Mirror, Clamp, Border, MirrorOnce
		*Filter {Linear}    // Optional filtering of the texture. Options: Point, Linear, Anisotropic
		*o2w                // Optional 3d texture coord transform
		{
			*scale{100 100 1}
			*euler{0 0 90}
		}
	}
}

// Model Instancing.
// An instance can be created from any previously defined object. The instance will
// share the renderable model from the object it is an instance of.
// Note that properties of the object are not inherited by the instance.
// The instance flag (false in this example) is used to prevent the model ever being drawn
// It is different to the *Hidden property as that can be changed in the UI
*Box model_instancing FF0000FF false   // Define a model to be used only for instancing
{
	0.8 1 2
	*RandColour              // Note: this will not be inheritted by the instances
}

*Instance model_instancing FFFF0000   // The name indicates which model to instance
{
	*o2w {*Pos {5 0 -2}}
}
*Instance model_instancing FF0000FF
{
	*o2w {*Pos {-4 0.5 0.5}}
}

// Object Nesting.
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
)";
			out <<
				R"(
// ************************************************************************************
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
	//*AbsoluteClipPlanes     // Optional. Clip planes are a fixed distance, not relative to the focus point distance
	//*Orthographic           // Optional. Use an orthographic projection rather than perspective
}

// ************************************************************************************
// Lights
// ************************************************************************************
// Light sources can be top level objects, children of other objects, or contain
// child objects. In some ways they are like a *Group object, they have no geometry
// of their own but can contain objects with geometry.

*DirectionalLight sun FFFF00  // Colour attribute is the colour of the light source
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
)";
out <<
	R"(
// ************************************************************************************
// Objects
// ************************************************************************************
// Below is an example of every supported object type with notes on their syntax

// Line modifiers:
//   *Coloured - The lines have an aarrggbb colour after each one. Must occur before line data if used.
//   *Width - Render the lines with the thickness specified (in pixels).
//   *Param - Clip the previous line to the parametric values given.

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
	-2  3  4 FFFF0000                   // Note, colour blend smoothly between each vertex
	 2  0 -2 FFFFFF00
	*Smooth                             // Optional. Turns the line segments into a smooth spline
	*Width { 5 }                        // Optional line width and arrow head size
}

// A circle or ellipse
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

// A matrix drawn as a set of three basis vectors (X=red, Y=green, Z=blue)
*Matrix3x3 a2b_transform
{
	1 0 0      // X
	0 1 0      // Y
	0 0 1      // Z
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
	*Coloured             // Optional. If specific means each pair of verts in along the ribbon has a colour
	-1 -2  0 FFFF0000
	-1  3  0 FF00FF00
	 2  0  0 FF0000FF
	*Smooth               // Optional. Generates a spline throught the points
	*o2w{*randpos{0 0 0 2} *randori}
	*Texture              // Optional texture repeated along each quad of the ribbon
	{
		"#checker"
	}
}

// A box given by width, height, and depth
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

// A mesh of lines, faces, or tetrahedra.
// Syntax:
//	*Mesh [name] [colour]
//	{
//		*Verts { x y z ... }
//		[*Normals { nx ny nz ... }]                            // One per vertex
//		[*Colours { c0 c1 c2 ... }]                            // One per vertex
//		[*TexCoords { tx ty ... }]                             // One per vertex
//		[GenerateNormals]                                      // Only works for faces or tetras
//		*Faces { f00 f01 f02  f10 f11 f12  f20 f21 f22  ...}   // Indices of faces
//		*Lines { l00 l01  l10 l11  l20 l21  l30 l31 ...}       // Indices of lines
//		*Tetra { t00 t01 t02 t03  t10 t11 t12 t13 ...}         // Indices of tetrahedra
//	}
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
//	"filepath"           // The file to create the model from
//	*Part { n }          // For model formats that contain multiple models, allows a specific one to be selected
//	*GenerateNormals     // Generate normals for the model
//}

// A group of objects
*Group group
{
	*Wireframe     // Object modifiers applied to groups are applied recursively to children within the group
	*Box b FF00FF00 { 0.4 0.1 0.2 }
	*Sphere s FF0000FF { 0.3 *o2w{*pos{0 1 2}}}
}

// Embedded lua code can be used to programmatically generate script
#embedded(lua)
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
)";
			out <<
				R"(
// ************************************************************************************
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
			return out.str();
		}

		// LdrObject ***********************************

		#if PR_DBG
		struct LeakedLdrObjects
		{
			std::set<LdrObject const*> m_ldr_objects;
			
			LeakedLdrObjects()
				:m_ldr_objects()
			{}
			~LeakedLdrObjects()
			{
				if (m_ldr_objects.empty()) return;

				std::string msg = "Leaked LdrObjects detected:\n";
				for (auto ldr : m_ldr_objects)
					msg.append(ldr->TypeAndName()).append("\n");

				PR_ASSERT(1, m_ldr_objects.empty(), msg.c_str());
			}
			void add(LdrObject const* ldr)
			{
				m_ldr_objects.insert(ldr);
			}
			void remove(LdrObject const* ldr)
			{
				m_ldr_objects.erase(ldr);
			}
		} g_ldr_object_tracker;
		#endif

		LdrObject::LdrObject(ObjectAttributes const& attr, LdrObject* parent, ContextId context_id)
			:RdrInstance()
			, m_o2p(m4x4Identity)
			, m_type(attr.m_type)
			, m_parent(parent)
			, m_child()
			, m_name(attr.m_name)
			, m_context_id(context_id)
			, m_base_colour(attr.m_colour)
			, m_colour_mask()
			, m_anim()
			, m_step()
			, m_bbox_instance()
			, m_instanced(attr.m_instance)
			, m_visible(true)
			, m_wireframe(false)
			, m_user_data()
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
			return string32(ELdrObject::ToString(m_type)) + " " + m_name;
		}

		// Recursively add this object and its children to a viewport
		void LdrObject::AddToScene(Scene& scene, float time_s, pr::m4x4 const* p2w)
		{
			// Set the instance to world
			m_i2w = *p2w * m_o2p * m_anim.Step(time_s);

			// Add the instance to the scene drawlist
			if (m_instanced && m_visible && m_model)
				scene.AddInstance(*this); // Could add occlusion culling here...

			// Rinse and repeat for all children
			for (auto& child : m_child)
				child->AddToScene(scene, time_s, &m_i2w);
		}

		// Recursively add this object using 'bbox_model' instead of its
		// actual model, located and scaled to the transform and box of this object
		void LdrObject::AddBBoxToScene(Scene& scene, ModelPtr bbox_model, float time_s, pr::m4x4 const* p2w)
		{
			// Set the instance to world
			pr::m4x4 i2w = *p2w * m_o2p * m_anim.Step(time_s);

			// Add the bbox instance to the scene drawlist
			if (m_instanced && m_visible && m_model)
			{
				m_bbox_instance.m_model = bbox_model;
				m_bbox_instance.m_i2w = i2w;
				m_bbox_instance.m_i2w.x *= m_model->m_bbox.SizeX() + pr::maths::tiny;
				m_bbox_instance.m_i2w.y *= m_model->m_bbox.SizeY() + pr::maths::tiny;
				m_bbox_instance.m_i2w.z *= m_model->m_bbox.SizeZ() + pr::maths::tiny;
				m_bbox_instance.m_i2w.w = i2w.w + m_model->m_bbox.Centre();
				m_bbox_instance.m_i2w.w.w = 1.0f;
				scene.AddInstance(m_bbox_instance); // Could add occlusion culling here...
			}

			// Rince and repeat for all children
			for (auto& child : m_child)
				child->AddBBoxToScene(scene, bbox_model, time_s, &m_i2w);
		}

		// Set the visibility of this object or child objects matching 'name' (see Apply)
		void LdrObject::Visible(bool visible, char const* name)
		{
			Apply([=](LdrObject* o){ o->m_visible = visible; return true; }, name);
		}

		// Set the render mode for this object or child objects matching 'name' (see Apply)
		void LdrObject::Wireframe(bool wireframe, char const* name)
		{
			Apply([=](LdrObject* o)
			{
				o->m_wireframe = wireframe;
				if (o->m_wireframe) o->m_rsb.Set(ERS::FillMode, D3D11_FILL_WIREFRAME);
				else                o->m_rsb.Clear(ERS::FillMode);
				return true;
			}, name);
		}

		// Set the colour of this object or child objects matching 'name' (see Apply)
		// Object base colour is not changed, only the tint colour = tint
		void LdrObject::SetColour(pr::Colour32 colour, pr::uint mask, char const* name)
		{
			Apply([=](LdrObject* o)
			{
				o->m_colour.m_aarrggbb = SetBits(o->m_base_colour.m_aarrggbb, mask, colour.m_aarrggbb);

				bool has_alpha = o->m_colour.a() != 0xFF;
				o->m_sko.Alpha(has_alpha);
				SetAlphaBlending(o->m_bsb, o->m_dsb, o->m_rsb, has_alpha);
				return true;
			}, name);
		}

		// Set the texture on this object or child objects matching 'name' (see Apply)
		// Note for difference mode drawlist management, if the object is currently in
		// one or more drawlists (i.e. added to a scene) it will need to be removed and
		// re-added so that the sort order is correct.
		void LdrObject::SetTexture(Texture2DPtr tex, char const* name)
		{
			Apply([=](LdrObject* o)
			{
				if (o->m_model == nullptr) return true;
				for (auto& nug : o->m_model->m_nuggets)
				{
					nug.m_tex_diffuse = tex;

					o->m_sko.Alpha(tex->m_has_alpha);
					SetAlphaBlending(nug, tex->m_has_alpha);
					// The drawlists will need to be resorted...
				}
				return true;
			}, name);
		}

		// Add/Remove 'child' as a child of this object
		void LdrObject::AddChild(LdrObjectPtr child)
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
			delete doomed;
		}
		long LdrObject::AddRef() const
		{
			return RefCount<LdrObject>::AddRef();
		}
		long LdrObject::Release() const
		{
			return RefCount<LdrObject>::Release();
		}
	}
}