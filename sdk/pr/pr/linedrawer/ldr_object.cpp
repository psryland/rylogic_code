//***************************************************************************************************
// Ldr Object Manager
//  Copyright © Rylogic Ltd 2009
//***************************************************************************************************

#include <string>
#include <sstream>
#include <array>
#include <unordered_map>
#include "pr/linedrawer/ldr_object.h"
#include "pr/common/assert.h"
#include "pr/common/hash.h"
#include "pr/common/windows_com.h"
#include "pr/maths/convexhull.h"
#include "pr/gui/progress_dlg.h"
#include "pr/renderer11/renderer.h"
#include "pr/renderer11/render/scene.h"

namespace pr
{
	namespace ldr
	{
		typedef pr::hash::HashValue HashValue;
		typedef pr::Array<pr::v4> VCont;
		typedef pr::Array<pr::v4> NCont;
		typedef pr::Array<pr::uint16> ICont;
		typedef pr::Array<pr::Colour32> CCont;
		typedef pr::Array<pr::v2> TCont;

		// Cache the scratch buffers
		VCont g_point;
		NCont g_norms;
		ICont g_index;
		CCont g_color;
		TCont g_texts;
		inline VCont& Point() { g_point.resize(0); return g_point; }
		inline NCont& Norms() { g_norms.resize(0); return g_norms; }
		inline ICont& Index() { g_index.resize(0); return g_index; }
		inline CCont& Color() { g_color.resize(0); return g_color; }
		inline TCont& Texts() { g_texts.resize(0); return g_texts; }

		// Check the hash values are correct
		PR_EXPAND(PR_DBG, static bool s_eldrobject_kws_checked = pr::CheckHashEnum<ELdrObject>([&](char const* s) { return pr::script::Reader::HashKeyword(s,false); }));
		PR_EXPAND(PR_DBG, static bool s_ekeyword_kws_checked   = pr::CheckHashEnum<EKeyword  >([&](char const* s) { return pr::script::Reader::HashKeyword(s,false); }));

		#if PR_DBG
		struct LeakedLdrObjects
		{
			long m_ldr_objects_in_existance;
			LeakedLdrObjects() :m_ldr_objects_in_existance(0) {}
			~LeakedLdrObjects() { PR_ASSERT(PR_DBG, m_ldr_objects_in_existance == 0, "Leaked LdrObjects detected"); }
			void operator ++() { ++m_ldr_objects_in_existance; }
			void operator --() { --m_ldr_objects_in_existance; }
		} g_ldr_object_tracker;
		#endif

		// LdrObjectUIData ***********************************

		LdrObjectUIData::LdrObjectUIData()
			:m_tree_item(INVALID_TREE_ITEM)
			,m_list_item(INVALID_LIST_ITEM)
		{}

		// LdrObject ***********************************

		LdrObject::LdrObject(ObjectAttributes const& attr, LdrObject* parent, ContextId context_id)
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
			,m_uidata()
			,m_step()
			,m_bbox_instance()
			,m_instanced(attr.m_instance)
			,m_visible(true)
			,m_wireframe(false)
			,m_user_data(0)
		{
			m_i2w    = m4x4Identity;
			m_colour = m_base_colour;
			PR_EXPAND(PR_DBG, ++g_ldr_object_tracker);
		}
		LdrObject::~LdrObject()
		{
			PR_EXPAND(PR_DBG, --g_ldr_object_tracker);
		}

		// Return the declaration name of this object
		std::string LdrObject::TypeAndName() const
		{
			return std::string(ELdrObject::ToString(m_type)) + " " + m_name;
		}

		// Recursively add this object and its children to a viewport
		void LdrObject::AddToScene(pr::rdr::Scene& scene, float time_s, pr::m4x4 const* p2w)
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
		void LdrObject::AddBBoxToScene(pr::rdr::Scene& scene, pr::rdr::ModelPtr bbox_model, float time_s, pr::m4x4 const* p2w)
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
				m_bbox_instance.m_i2w.w  = i2w.w + m_model->m_bbox.Centre();
				m_bbox_instance.m_i2w.w.w = 1.0f;
				scene.AddInstance(m_bbox_instance); // Could add occlusion culling here...
			}

			// Rince and repeat for all children
			for (auto& child : m_child)
				child->AddBBoxToScene(scene, bbox_model, time_s, &m_i2w);
		}

		// Change visibility for this object
		void LdrObject::Visible(bool visible, bool include_children)
		{
			m_visible = visible;
			if (!include_children) return;
			for (auto& child : m_child)
				child->Visible(visible, include_children);
		}

		// Change the render mode for this object
		void LdrObject::Wireframe(bool wireframe, bool include_children)
		{
			m_wireframe = wireframe;
			if (m_wireframe) m_rsb.Set(pr::rdr::ERS::FillMode, D3D11_FILL_WIREFRAME);
			else             m_rsb.Clear(pr::rdr::ERS::FillMode);
			if (!include_children) return;
			for (auto& child : m_child)
				child->Wireframe(wireframe, include_children);
		}

		// Change the colour of this object
		void LdrObject::SetColour(pr::Colour32 colour, pr::uint mask, bool include_children)
		{
			pr::Colour32 blend = m_base_colour * colour;
			m_colour.m_aarrggbb = SetBits(m_colour.m_aarrggbb, mask, blend.m_aarrggbb);

			bool has_alpha = m_colour.a() != 0xFF;
			m_sko.Alpha(has_alpha);
			SetAlphaBlending(m_bsb, m_dsb, m_rsb, has_alpha);

			if (!include_children) return;
			for (auto& child : m_child)
				child->SetColour(colour, mask, include_children);
		}

		// Called when there are no more references to this object
		void LdrObject::RefCountZero(RefCount<LdrObject>* doomed)
		{
			events::Send(Evt_LdrObjectDelete(static_cast<LdrObject*>(doomed)));
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

		// LdrObject Creation functions *********************************************
		typedef std::unordered_map<hash::HashValue, pr::rdr::ModelPtr> ModelCont;

		// Helper object for passing parameters between parsing functions
		struct ParseParams
		{
			pr::Renderer&       m_rdr;
			pr::script::Reader& m_reader;
			ObjectCont&         m_objects;
			ModelCont&          m_models;
			ContextId           m_context_id;
			HashValue           m_keyword;
			LdrObject*          m_parent;
			std::size_t&        m_obj_count;
			std::size_t         m_start_time;
			std::size_t         m_last_update;

			ParseParams(
				pr::Renderer&       rdr,
				pr::script::Reader& reader,
				ObjectCont&         objects,
				ModelCont&          models,
				ContextId           context_id,
				HashValue           keyword,
				LdrObject*          parent,
				std::size_t&        obj_count,
				std::size_t         start_time)
				:m_rdr              (rdr        )
				,m_reader           (reader     )
				,m_objects          (objects    )
				,m_models           (models     )
				,m_context_id       (context_id )
				,m_keyword          (keyword    )
				,m_parent           (parent     )
				,m_obj_count        (obj_count  )
				,m_start_time       (start_time )
				,m_last_update      (start_time )
			{}

			ParseParams(
				ParseParams& p,
				ObjectCont&  objects,
				HashValue    keyword,
				LdrObject*   parent)
				:m_rdr              (p.m_rdr        )
				,m_reader           (p.m_reader     )
				,m_objects          (objects        )
				,m_models           (p.m_models     )
				,m_context_id       (p.m_context_id )
				,m_keyword          (keyword        )
				,m_parent           (parent         )
				,m_obj_count        (p.m_obj_count  )
				,m_start_time       (p.m_start_time )
				,m_last_update      (p.m_last_update)
			{}

		private:
			ParseParams(ParseParams const&);
			ParseParams& operator=(ParseParams const&);
		};

		// Forward declare the recursive object parsing function
		bool ParseLdrObject(ParseParams& p);

		// Read the name, colour, and instance flag for an object
		ObjectAttributes ParseAttributes(pr::script::Reader& reader, ELdrObject model_type)
		{
			ObjectAttributes attr;
			attr.m_type = model_type;
			attr.m_name = ELdrObject::ToString(model_type);
			if (!reader.IsSectionStart()) reader.ExtractIdentifier(attr.m_name);
			if (!reader.IsSectionStart()) reader.ExtractInt(attr.m_colour.m_aarrggbb, 16);
			if (!reader.IsSectionStart()) reader.ExtractBool(attr.m_instance);
			return attr;
		}

		// Parse a transform description
		void ParseTransform(pr::script::Reader& reader, pr::m4x4& o2w)
		{
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
						reader.ExtractMatrix4x4S(p2w);
						break;
					}
				case EKeyword::M3x3:
					{
						reader.ExtractMatrix3x3S(pr::cast_m3x4(p2w));
						break;
					}
				case EKeyword::Pos:
					{
						reader.ExtractVector3S(p2w.pos, 1.0f);
						break;
					}
				case EKeyword::Direction:
					{
						pr::v4 direction; int axis_id;
						reader.SectionStart();
						reader.ExtractVector3(direction, 0.0f);
						reader.ExtractInt(axis_id, 10);
						reader.SectionEnd();

						if (axis_id < 1 || axis_id > 3)
						{
							reader.ReportError(pr::script::EResult::UnknownValue, "axis_id must one of ±1, ±2, ±3");
							break;
						}

						if (axis_id < 0) direction = -direction;
						axis_id = pr::Abs(axis_id) - 1;
						pr::OriFromDir(cast_m3x4(p2w), direction, axis_id, pr::v4YAxis);
						break;
					}
				case EKeyword::Quat:
					{
						pr::Quat quat;
						reader.ExtractVector4S(pr::cast_v4(quat));
						cast_m3x4(p2w).set(quat);
						break;
					}
				case EKeyword::Rand4x4:
					{
						pr::v4 centre; float radius;
						reader.SectionStart();
						reader.ExtractVector3(centre, 1.0f);
						reader.ExtractReal(radius);
						reader.SectionEnd();
						p2w = pr::Random4x4(centre, radius);
						break;
					}
				case EKeyword::RandPos:
					{
						pr::v4 centre; float radius;
						reader.SectionStart();
						reader.ExtractVector3(centre, 1.0f);
						reader.ExtractReal(radius);
						reader.SectionEnd();
						p2w.pos = Random3(centre, radius, 1.0f);
						break;
					}
				case EKeyword::RandOri:
					{
						pr::cast_m3x4(p2w) = pr::Random3x4();
						break;
					}
				case EKeyword::Euler:
					{
						pr::v4 angles;
						reader.ExtractVector3S(angles, 0.0f);
						pr::cast_m3x4(p2w) = pr::m3x4::make(pr::DegreesToRadians(angles.x), pr::DegreesToRadians(angles.y), pr::DegreesToRadians(angles.z));
						break;
					}
				case EKeyword::Scale:
					{
						pr::v4 scale;
						reader.ExtractVector3S(scale, 0.0f);
						p2w.x *= scale.x;
						p2w.y *= scale.y;
						p2w.z *= scale.z;
						break;
					}
				case EKeyword::Transpose:
					{
						pr::Transpose4x4(p2w);
						break;
					}
				case EKeyword::Inverse:
					{
						pr::Inverse(p2w);
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
						pr::Orthonormalise(p2w);
						break;
					}
				}
			}
			reader.SectionEnd();

			// Premultiply the transform
			o2w = p2w * o2w;
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
						if      (pr::str::EqualI(style, "NoAnimation"    )) anim.m_style = EAnimStyle::NoAnimation;
						else if (pr::str::EqualI(style, "PlayOnce"       )) anim.m_style = EAnimStyle::PlayOnce;
						else if (pr::str::EqualI(style, "PlayReverse"    )) anim.m_style = EAnimStyle::PlayReverse;
						else if (pr::str::EqualI(style, "PingPong"       )) anim.m_style = EAnimStyle::PingPong;
						else if (pr::str::EqualI(style, "PlayContinuous" )) anim.m_style = EAnimStyle::PlayContinuous;
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
		bool ParseProperties(ParseParams& p, pr::hash::HashValue kw, LdrObjectPtr obj)
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
					pr::events::Send(Evt_LdrObjectStepCode(obj));
					return true;
				}
			}
		}

		// Parse a texture description
		// Returns a pointer to the pr::rdr::Texture created in the renderer
		bool ParseTexture(ParseParams& p, pr::rdr::Texture2DPtr& tex)
		{
			std::string tex_filepath;
			pr::m4x4 t2s = pr::m4x4Identity;
			pr::rdr::SamplerDesc sam;

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
							p.m_reader.ExtractIdentifier(word); sam.AddressU = (D3D11_TEXTURE_ADDRESS_MODE)pr::rdr::ETexAddrMode::Parse(word, false);
							p.m_reader.ExtractIdentifier(word); sam.AddressV = (D3D11_TEXTURE_ADDRESS_MODE)pr::rdr::ETexAddrMode::Parse(word, false);
							p.m_reader.SectionEnd();
							break;
						}
					case EKeyword::Filter:
						{
							char word[20];
							p.m_reader.SectionStart();
							p.m_reader.ExtractIdentifier(word); sam.Filter = (D3D11_FILTER)pr::rdr::EFilter::Parse(word, false);
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
					tex = p.m_rdr.m_tex_mgr.CreateTexture2D(pr::rdr::AutoId, sam, tex_filepath.c_str());
					tex->m_t2s = t2s;
				}
				catch (std::exception const& e)
				{
					p.m_reader.ReportError(pr::script::EResult::ValueNotFound, pr::FmtS("failed to create texture %s\nReason: %s" ,tex_filepath.c_str() ,e.what()));
				}
			}
			return true;
		}

		// Parse a video texture
		bool ParseVideo(ParseParams& p, pr::rdr::Texture2DPtr& vid)
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
				//	vid = p.m_rdr.m_tex_mgr.CreateVideoTexture(pr::rdr::AutoId, filepath.c_str());
				//}
				//catch (std::exception const& e)
				//{
				//	p.m_reader.ReportError(pr::script::EResult::ValueNotFound, pr::FmtS("failed to create video %s\nReason: %s" ,filepath.c_str() ,e.what()));
				//}
			}
			p.m_reader.SectionEnd();
			return true;
		}

		// Base class for all object creators
		struct IObjectCreator
		{
			virtual ~IObjectCreator() {}
			virtual bool ParseKeyword(ParseParams&, HashValue) { return false; }
			virtual void Parse(ParseParams& p) = 0;
			virtual pr::rdr::ModelPtr CreateModel(ParseParams& p, std::string name) = 0;
			virtual void PostObjectCreation(LdrObjectPtr&) {}
		};

		// Base class for objects with a texture
		struct IObjectCreatorTexture :IObjectCreator
		{
			pr::rdr::Texture2DPtr m_texture;
			pr::rdr::DrawMethod m_local_mat;

			IObjectCreatorTexture() :m_texture() ,m_local_mat() {}
			bool ParseKeyword(ParseParams& p, HashValue kw) override
			{
				switch (kw) {
				default: return IObjectCreator::ParseKeyword(p, kw);
				case EKeyword::Texture:  ParseTexture(p, m_texture); return true;
				case EKeyword::Video:    ParseVideo(p, m_texture); return true;
				}
			}
			pr::rdr::DrawMethod* GetDrawMethod(ParseParams& p)
			{
				// If a texture was given create a material that uses it
				if (!m_texture)
					return nullptr;

				m_local_mat = p.m_rdr.m_shdr_mgr.FindShaderFor<pr::rdr::VertPCNT>();
				m_local_mat.m_tex_diffuse = m_texture;
				//if (m_texture->m_video)
				//	m_texture->m_video->Play(true);
				return &m_local_mat;
			}
		};

		// Template prototype for ObjectCreators
		template <ELdrObject::Enum_ ObjType> struct ObjectCreator;

		// Base class for object creators that are based on lines
		struct IObjectCreatorLine :IObjectCreator
		{
			VCont& m_point;
			ICont& m_index;
			CCont& m_colour;
			bool   m_per_line_colour;
			bool   m_linemesh;

			IObjectCreatorLine() :m_point(Point()) ,m_index(Index()) ,m_colour(Color()) ,m_per_line_colour() ,m_linemesh() {}
			bool ParseKeyword(ParseParams& p, HashValue kw) override
			{
				switch (kw)
				{
				default: return IObjectCreator::ParseKeyword(p,kw);
				case EKeyword::Coloured:  m_per_line_colour = true; return true;
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
						pr::v4 p   = p0;
						pr::v4 dir = p1 - p0;
						p0 = p + t[0] * dir;
						p1 = p + t[1] * dir;
						return true;
					}
				}
			}
			pr::rdr::ModelPtr CreateModel(ParseParams& p, std::string name) override
			{
				using namespace pr::rdr;

				// Validate
				if (m_point.size() < 2)
				{
					p.m_reader.ReportError(FmtS("Line object '%s' description incomplete", name.c_str()));
					return nullptr;
				}

				// Create the model
				ModelPtr model;
				if (m_linemesh)
					model = ModelGenerator<VertPC>::Mesh(p.m_rdr, EPrim::LineList, m_point.size(), m_index.size(), m_point.data(), m_index.data(), m_colour.size(), m_colour.data());
				else
					model = ModelGenerator<VertPC>::Lines(p.m_rdr, m_point.size()/2, m_point.data(), m_colour.size(), m_colour.data());

				model->m_name = name;
				return model;
			}
		};

		// ELdrObject::Line
		template <> struct ObjectCreator<ELdrObject::Line> :IObjectCreatorLine
		{
			void Parse(ParseParams& p) override
			{
				pr::v4 p0,p1;
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

		// ELdrObject::LineList
		template <> struct ObjectCreator<ELdrObject::LineList> :IObjectCreatorLine
		{
			void Parse(ParseParams& p) override
			{
				pr::v4 pt;
				p.m_reader.ExtractVector3(pt, 1.0f);

				if (!m_point.empty())
				{
					if (m_point.size() > 1)
						m_point.push_back(m_point.back());

					m_point.push_back(pt);
					if (m_per_line_colour)
					{
						pr::Colour32 col;
						p.m_reader.ExtractInt(col.m_aarrggbb, 16);
						m_colour.push_back(col);
						m_colour.push_back(col);
					}
				}
				else
				{
					m_point.push_back(pt);
				}
			}
		};

		// ELdrObject::LineBox
		template <> struct ObjectCreator<ELdrObject::LineBox> :IObjectCreatorLine
		{
			void Parse(ParseParams& p) override
			{
				m_linemesh = true;

				pr::v4 dim;
				p.m_reader.ExtractReal(dim.x);
				if (p.m_reader.IsKeyword() || p.m_reader.IsSectionEnd()) dim.y = dim.x; else p.m_reader.ExtractReal(dim.y);
				if (p.m_reader.IsKeyword() || p.m_reader.IsSectionEnd()) dim.z = dim.y; else p.m_reader.ExtractReal(dim.z);
				dim *= 0.5f;

				m_point.push_back(pr::v4::make(-dim.x, -dim.y, -dim.z, 0));
				m_point.push_back(pr::v4::make( dim.x, -dim.y, -dim.z, 0));
				m_point.push_back(pr::v4::make( dim.x,  dim.y, -dim.z, 0));
				m_point.push_back(pr::v4::make(-dim.x,  dim.y, -dim.z, 0));
				m_point.push_back(pr::v4::make(-dim.x, -dim.y,  dim.z, 0));
				m_point.push_back(pr::v4::make( dim.x, -dim.y,  dim.z, 0));
				m_point.push_back(pr::v4::make( dim.x,  dim.y,  dim.z, 0));
				m_point.push_back(pr::v4::make(-dim.x,  dim.y,  dim.z, 0));

				pr::uint16 idx[] = {0,1,1,2,2,3,3,0, 4,5,5,6,6,7,7,4, 0,4,1,5,2,6,3,7};
				m_index.insert(m_index.end(), idx, idx + PR_COUNTOF(idx));
			}
		};

		// ELdrObject::Spline
		template <> struct ObjectCreator<ELdrObject::Spline> :IObjectCreatorLine
		{
			void Parse(ParseParams& p) override
			{
				pr::Spline spline;
				p.m_reader.ExtractVector3(spline.x, 1.0f);
				p.m_reader.ExtractVector3(spline.y, 1.0f);
				p.m_reader.ExtractVector3(spline.z, 1.0f);
				p.m_reader.ExtractVector3(spline.w, 1.0f);

				// Generate points for the spline
				pr::Array<pr::v4> raster;
				pr::Raster(spline, raster, 30);
				for (size_t i = 0, iend = raster.size() - 1; i != iend; ++i)
				{
					m_point.push_back(raster[i+0]);
					m_point.push_back(raster[i+1]);
				}

				if (m_per_line_colour)
				{
					pr::Colour32 col;
					p.m_reader.ExtractInt(col.m_aarrggbb, 16);
					for (size_t i = 0, iend = (raster.size() - 1) * 2; i != iend; ++i)
						m_colour.push_back(col);
				}
			}
		};

		// ELdrObject::Matrix3x3
		template <> struct ObjectCreator<ELdrObject::Matrix3x3> :IObjectCreatorLine
		{
			void Parse(ParseParams& p) override
			{
				m_linemesh = true;

				pr::m4x4 basis;
				p.m_reader.ExtractMatrix3x3(cast_m3x4(basis));

				pr::v4       pts[] = {{0,0,0,1}, basis.x, {0,0,0,1}, basis.y, {0,0,0,1}, basis.z};
				pr::Colour32 col[] = {pr::Colour32Red, pr::Colour32Red, pr::Colour32Green, pr::Colour32Green, pr::Colour32Blue, pr::Colour32Blue};
				pr::uint16   idx[] = {0,1,2,3,4,5};

				m_point .insert(m_point .end(), pts, pts + PR_COUNTOF(pts));
				m_colour.insert(m_colour.end(), col, col + PR_COUNTOF(col));
				m_index .insert(m_index .end(), idx, idx + PR_COUNTOF(idx));
			}
		};

		// Base class for object creators that are based on 2d shapes
		struct IObjectCreatorShape2d :IObjectCreatorTexture
		{
			// Create space for the model
			VCont& m_point;
			ICont& m_index;
			TCont& m_tex;
			int    m_axis_id;
			pr::v4 m_dim;
			int    m_facets;
			bool   m_solid;

			IObjectCreatorShape2d() :m_point(Point()) ,m_index(Index()) ,m_tex(Texts()) ,m_axis_id() ,m_dim() ,m_facets(40) ,m_solid(false) {}
			bool ParseKeyword(ParseParams& p, HashValue kw) override
			{
				switch (kw)
				{
				default: return IObjectCreatorTexture::ParseKeyword(p, kw);
				case EKeyword::Solid:  m_solid = true; return true;
				case EKeyword::Facets: p.m_reader.ExtractIntS(m_facets, 10); return true;
				}
			}
			pr::rdr::ModelPtr CreateModel(ParseParams& p, std::string name) override
			{
				using namespace pr::rdr;

				// Validate
				if (m_axis_id < 1 || m_axis_id > 3)
				{
					p.m_reader.ReportError(pr::script::EResult::UnknownValue, "axis_id must one of ±1, ±2, ±3");
					return nullptr;
				}

				pr::BoundingBox bbox;
				GenerateShape(bbox);

				// Permute the verts, normals, and bbox
				auto axis_id = pr::Abs(m_axis_id) - 1;
				for (auto& v : m_point) v = pr::Permute3(v             , axis_id + 1);
				auto norm                 = pr::Permute3(pr::v4ZAxis   , axis_id + 1);
				bbox.m_radius             = pr::Permute3(bbox.m_radius , axis_id + 1);

				// Create the model
				ModelPtr model;
				if (m_solid)
					model = ModelGenerator<>::Mesh(p.m_rdr, EPrim::TriList, m_point.size(), m_index.size(), m_point.data(), m_index.data(), 0, 0, 1, &norm, m_tex.data(), GetDrawMethod(p));
				else
					model = ModelGenerator<>::Mesh(p.m_rdr, EPrim::LineList, m_point.size(), m_index.size(), m_point.data(), m_index.data());

				model->m_bbox = bbox;
				model->m_name = name;
				return model;
			}
			virtual void GenerateShape(pr::BoundingBox& bbox) = 0;
		};

		// ELdrObject::Circle
		template <> struct ObjectCreator<ELdrObject::Circle> :IObjectCreatorShape2d
		{
			void Parse(ParseParams& p) override
			{
				p.m_reader.ExtractInt(m_axis_id, 10);
				p.m_reader.ExtractReal(m_dim.x);
				if (p.m_reader.IsKeyword() || p.m_reader.IsSectionEnd()) m_dim.y = m_dim.x; else p.m_reader.ExtractReal(m_dim.y);
			}
			void GenerateShape(pr::BoundingBox& bbox) override
			{
				m_facets = std::max(m_facets, 3);

				// Create space for the model
				auto vcount = m_facets;
				auto icount = m_solid ? 3 * (m_facets - 2) : 2 * m_facets;
				m_point.resize(vcount);
				m_index.resize(icount);

				// Fill the vertex buffer
				auto vb = std::begin(m_point);
				for (int i = 0; i != m_facets; ++i)
				{
					auto c = pr::Cos(pr::maths::tau * i / m_facets);
					auto s = pr::Sin(pr::maths::tau * i / m_facets);
					*vb++ = pr::v4::make(m_dim.x * c, m_dim.y * s, 0, 1);
				}
				assert(vb == std::end(m_point));

				// Generate texture coords for solid shapes
				if (m_solid)
				{
					m_tex.resize(vcount);
					auto tb = std::begin(m_tex);
					for (int i = 0; i != m_facets; ++i)
					{
						auto c = pr::Cos(pr::maths::tau * i / m_facets);
						auto s = pr::Sin(pr::maths::tau * i / m_facets);
						*tb++ = pr::v2::make(0.5f*(c+1), 0.5f*(1-s));
					}
					assert(tb == std::end(m_tex));
				}

				// Fill the index buffer
				auto ib = std::begin(m_index);
				if (m_solid)
				{
					for (int i = 1; i != vcount - 1; ++i)
					{
						*ib++ = 0;
						*ib++ = checked_cast<pr::uint16>(i);
						*ib++ = checked_cast<pr::uint16>(i + 1);
					}
				}
				else // border only
				{
					for (int i = 0; i != vcount; ++i)
					{
						*ib++ = checked_cast<pr::uint16>(i);
						*ib++ = checked_cast<pr::uint16>((i + 1) % vcount);
					}
				}
				assert(ib == std::end(m_index));

				// Set the bounding box
				bbox.set(pr::v4Origin, pr::v4::make(m_dim.x, m_dim.y, 0, 0));
			}
		};

		// ELdrObject::Rect
		template <> struct ObjectCreator<ELdrObject::Rect> :IObjectCreatorShape2d
		{
			float m_corner_radius;

			ObjectCreator() :m_corner_radius() {}
			bool ParseKeyword(ParseParams& p, HashValue kw) override
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
				p.m_reader.ExtractInt(m_axis_id, 10);
				p.m_reader.ExtractReal(m_dim.x);
				if (p.m_reader.IsKeyword() || p.m_reader.IsSectionEnd()) m_dim.y = m_dim.x; else p.m_reader.ExtractReal(m_dim.y);
				m_dim *= 0.5f;
			}
			void GenerateShape(pr::BoundingBox& bbox) override
			{
				// Limit the rounding to half the smallest rectangle side length
				auto rad = m_corner_radius;
				if (rad > m_dim.x) rad = m_dim.x;
				if (rad > m_dim.y) rad = m_dim.y;
				auto verts_per_cnr = rad != 0.0f ? std::max(m_facets/4, 0) + 1 : 1;

				// Create space for the model
				auto vcount = 4 * verts_per_cnr;
				auto icount = m_solid ? 12 * verts_per_cnr - 6 : 8 * verts_per_cnr;
				m_point.resize(vcount);
				m_index.resize(icount);

				// Fill the vertex buffer
				auto vb  = std::begin(m_point);
				auto vb0 = vb + verts_per_cnr * 0;
				auto vb1 = vb + verts_per_cnr * 1;
				auto vb2 = vb + verts_per_cnr * 2;
				auto vb3 = vb + verts_per_cnr * 3;
				for (int i = 0; i != verts_per_cnr; ++i)
				{
					auto c = verts_per_cnr > 1 ? pr::Cos(pr::maths::tau_by_4 * i / (verts_per_cnr - 1)) : 0;
					auto s = verts_per_cnr > 1 ? pr::Sin(pr::maths::tau_by_4 * i / (verts_per_cnr - 1)) : 0;

					*vb0++ = pr::v4::make(-m_dim.x + rad * (1 - c), -m_dim.y + rad * (1 - s), 0, 1);
					*vb1++ = pr::v4::make( m_dim.x - rad * (1 - s), -m_dim.y + rad * (1 - c), 0, 1);
					*vb2++ = pr::v4::make( m_dim.x - rad * (1 - c),  m_dim.y - rad * (1 - s), 0, 1);
					*vb3++ = pr::v4::make(-m_dim.x + rad * (1 - s),  m_dim.y - rad * (1 - c), 0, 1);
				}
				assert(vb3 == std::end(m_point));

				// Generate texture coords for solid shapes
				if (m_solid)
				{
					m_tex.resize(vcount);
					auto tx = rad / (2 * m_dim.x);
					auto ty = rad / (2 * m_dim.y);

					auto tb  = std::begin(m_tex);
					auto tb0 = tb + verts_per_cnr * 0;
					auto tb1 = tb + verts_per_cnr * 1;
					auto tb2 = tb + verts_per_cnr * 2;
					auto tb3 = tb + verts_per_cnr * 3;
					for (int i = 0; i != verts_per_cnr; ++i)
					{
						auto c = verts_per_cnr > 1 ? pr::Cos(pr::maths::tau_by_4 * i / (verts_per_cnr - 1)) : 0;
						auto s = verts_per_cnr > 1 ? pr::Sin(pr::maths::tau_by_4 * i / (verts_per_cnr - 1)) : 0;

						*tb0++ = pr::v2::make(0+s*tx, 1-c*ty);
						*tb1++ = pr::v2::make(1-c*tx, 1-s*ty);
						*tb2++ = pr::v2::make(1-s*tx, 0+c*ty);
						*tb3++ = pr::v2::make(0+c*tx, 0+s*ty);
					}
					assert(tb3 == std::end(m_tex));
				}

				// Fill the index buffer
				auto ib = std::begin(m_index);
				if (m_solid)
				{
					for (int i = 0, iend = (vcount / 2) - 1; i != iend; ++i)
					{
						*ib++ = checked_cast<pr::uint16>(vcount - i - 1);
						*ib++ = checked_cast<pr::uint16>(i);
						*ib++ = checked_cast<pr::uint16>(vcount - i - 2);

						*ib++ = checked_cast<pr::uint16>(vcount - i - 2);
						*ib++ = checked_cast<pr::uint16>(i);
						*ib++ = checked_cast<pr::uint16>(i + 1);
					}
				}
				else // border only
				{
					for (int i = 0; i != vcount; ++i)
					{
						*ib++ = checked_cast<pr::uint16>(i);
						*ib++ = checked_cast<pr::uint16>((i + 1) % vcount);
					}
				}
				assert(ib == std::end(m_index));

				// Set the bounding box
				bbox.set(pr::v4Origin, pr::v4::make(m_dim.x, m_dim.y, 0, 0));
			}
		};

		// Base class for planar objects
		struct IObjectCreatorPlane :IObjectCreatorTexture
		{
			VCont& m_point;
			CCont& m_colour;
			bool   m_per_vert_colour;

			IObjectCreatorPlane() :m_point(Point()) ,m_colour(Color()) ,m_per_vert_colour() {}
			bool ParseKeyword(ParseParams& p, HashValue kw) override
			{
				switch (kw) {
				default: return IObjectCreatorTexture::ParseKeyword(p, kw);
				case EKeyword::Coloured: m_per_vert_colour = true; return true;
				}
			}
			pr::rdr::ModelPtr CreateModel(ParseParams& p, std::string name) override
			{
				using namespace pr::rdr;

				// Validate
				if (m_point.empty() || (m_point.size() % 4) != 0)
				{
					p.m_reader.ReportError("Object description incomplete");
					return nullptr;
				}

				// Create the model
				ModelPtr model = ModelGenerator<>::Quad(p.m_rdr, m_point.size()/4, m_point.data(), m_colour.size(), m_colour.data(), pr::m4x4Identity, GetDrawMethod(p));
				model->m_name = name;
				return model;
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
				pr::v4 pnt,fwd; float w,h;
				p.m_reader.ExtractVector3(pnt, 1.0f);
				p.m_reader.ExtractVector3(fwd, 0.0f);
				p.m_reader.ExtractReal(w);
				p.m_reader.ExtractReal(h);

				fwd = pr::Normalise3(fwd);
				pr::v4 up   = pr::Perpendicular(fwd);
				pr::v4 left = pr::Cross3(up, fwd);
				up   *= h * 0.5f;
				left *= w * 0.5f;
				m_point.push_back(pnt - up - left);
				m_point.push_back(pnt - up + left);
				m_point.push_back(pnt + up - left);
				m_point.push_back(pnt + up + left);
			}
		};

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
			pr::rdr::ModelPtr CreateModel(ParseParams& p, std::string name) override
			{
				// Create the model
				auto model = pr::rdr::ModelGenerator<>::Box(p.m_rdr, m_dim * 0.5f, pr::m4x4Identity, pr::Colour32White, GetDrawMethod(p));
				model->m_name = name;
				return model;
			}
		};

		// ELdrObject::BoxLine
		template <> struct ObjectCreator<ELdrObject::BoxLine> :IObjectCreatorTexture
		{
			pr::m4x4 m_b2w;
			pr::v4 m_dim, m_up;

			ObjectCreator() :m_b2w() ,m_dim() ,m_up(pr::v4YAxis) {}
			bool ParseKeyword(ParseParams& p, HashValue kw) override
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
			pr::rdr::ModelPtr CreateModel(ParseParams& p, std::string name) override
			{
				auto model = pr::rdr::ModelGenerator<>::Box(p.m_rdr, m_dim * 0.5f, m_b2w, pr::Colour32White, GetDrawMethod(p));
				model->m_name = name;
				return model;
			}
		};

		// ELdrObject::BoxList
		template <> struct ObjectCreator<ELdrObject::BoxList> :IObjectCreatorTexture
		{
			pr::Array<pr::v4, 16> m_location;
			pr::v4 m_dim;

			ObjectCreator() :m_location() ,m_dim() {}
			void Parse(ParseParams& p) override
			{
				pr::v4 v;
				p.m_reader.ExtractVector3(v, 1.0f);
				if (m_dim == pr::v4Zero)
					m_dim = v.w0() * 0.5f;
				else
					m_location.push_back(v);
			}
			pr::rdr::ModelPtr CreateModel(ParseParams& p, std::string name) override
			{
				// Validate
				if (m_dim == pr::v4Zero || m_location.size() == 0)
				{
					p.m_reader.ReportError("Box list object description incomplete");
					return nullptr;
				}

				// Create the model
				auto model = pr::rdr::ModelGenerator<>::BoxList(p.m_rdr, m_location.size(), m_location.data(), m_dim, 0, 0, GetDrawMethod(p));
				model->m_name = name;
				return model;
			}
		};

		// Base class for arbitrary cubiod shapes
		struct IObjectCreatorCuboid :IObjectCreatorTexture
		{
			pr::v4 m_pt[8];
			pr::m4x4 m_b2w;

			IObjectCreatorCuboid() :m_pt() ,m_b2w(pr::m4x4Identity) {}
			pr::rdr::ModelPtr CreateModel(ParseParams& p, std::string name) override
			{
				auto model = pr::rdr::ModelGenerator<>::Boxes(p.m_rdr, 1, m_pt, m_b2w, 0, 0, GetDrawMethod(p));
				model->m_name = name;
				return model;
			}
		};

		// ELdrObject::FrustumWH
		template <> struct ObjectCreator<ELdrObject::FrustumWH> :IObjectCreatorCuboid
		{
			void Parse(ParseParams& p) override
			{
				int axis_id;
				p.m_reader.ExtractInt(axis_id, 10);

				float w,h,n,f;
				p.m_reader.ExtractReal(w);
				p.m_reader.ExtractReal(h);
				p.m_reader.ExtractReal(n);
				p.m_reader.ExtractReal(f);
				w *= 0.5f;
				h *= 0.5f;

				m_pt[0].set(-n*w, -n*h, n, 1.0f);
				m_pt[1].set(-n*w,  n*h, n, 1.0f);
				m_pt[2].set( n*w, -n*h, n, 1.0f);
				m_pt[3].set( n*w,  n*h, n, 1.0f);
				m_pt[4].set( f*w, -f*h, f, 1.0f);
				m_pt[5].set( f*w,  f*h, f, 1.0f);
				m_pt[6].set(-f*w, -f*h, f, 1.0f);
				m_pt[7].set(-f*w,  f*h, f, 1.0f);

				switch (axis_id){
				default: p.m_reader.ReportError(pr::script::EResult::UnknownValue, "axis_id must one of ±1, ±2, ±3"); return;
				case  1: m_b2w = pr::Rotation4x4(0.0f ,-pr::maths::tau_by_4 ,0.0f ,pr::v4Origin); break;
				case -1: m_b2w = pr::Rotation4x4(0.0f , pr::maths::tau_by_4 ,0.0f ,pr::v4Origin); break;
				case  2: m_b2w = pr::Rotation4x4(-pr::maths::tau_by_4 ,0.0f ,0.0f ,pr::v4Origin); break;
				case -2: m_b2w = pr::Rotation4x4( pr::maths::tau_by_4 ,0.0f ,0.0f ,pr::v4Origin); break;
				case  3: m_b2w = pr::m4x4Identity; break;
				case -3: m_b2w = pr::Rotation4x4(0.0f ,pr::maths::tau_by_2 ,0.0f ,pr::v4Origin); break;
				}
			}
		};

		// ELdrObject::FrustumFA
		template <> struct ObjectCreator<ELdrObject::FrustumFA> :IObjectCreatorCuboid
		{
			void Parse(ParseParams& p) override
			{
				int axis_id;
				p.m_reader.ExtractInt(axis_id,10);

				float fovY,aspect,n,f;
				p.m_reader.ExtractReal(fovY);
				p.m_reader.ExtractReal(aspect);
				p.m_reader.ExtractReal(n);
				p.m_reader.ExtractReal(f);

				// Construct pointed down +z, then rotate the points based on axis id
				float h = pr::Tan(pr::DegreesToRadians(fovY * 0.5f));
				float w = aspect * h;
				m_pt[0].set(-n*w, -n*h, n, 1.0f);
				m_pt[1].set( n*w, -n*h, n, 1.0f);
				m_pt[2].set(-n*w,  n*h, n, 1.0f);
				m_pt[3].set( n*w,  n*h, n, 1.0f);
				m_pt[4].set(-f*w, -f*h, f, 1.0f);
				m_pt[5].set( f*w, -f*h, f, 1.0f);
				m_pt[6].set(-f*w,  f*h, f, 1.0f);
				m_pt[7].set( f*w,  f*h, f, 1.0f);

				switch (axis_id) {
				default: p.m_reader.ReportError(pr::script::EResult::UnknownValue, "axis_id must one of ±1, ±2, ±3"); return;
				case  1: m_b2w = pr::Rotation4x4(0.0f , pr::maths::tau_by_4 ,0.0f ,pr::v4Origin); break;
				case -1: m_b2w = pr::Rotation4x4(0.0f ,-pr::maths::tau_by_4 ,0.0f ,pr::v4Origin); break;
				case  2: m_b2w = pr::Rotation4x4(-pr::maths::tau_by_4 ,0.0f ,0.0f ,pr::v4Origin); break;
				case -2: m_b2w = pr::Rotation4x4( pr::maths::tau_by_4 ,0.0f ,0.0f ,pr::v4Origin); break;
				case  3: m_b2w = pr::m4x4Identity; break;
				case -3: m_b2w = pr::Rotation4x4(0.0f ,pr::maths::tau_by_2 ,0.0f ,pr::v4Origin); break;
				}
			}
		};

		// ELdrObject::Sphere
		template <> struct ObjectCreator<ELdrObject::Sphere> :IObjectCreatorTexture
		{
			pr::v4 m_dim;
			int m_divisions;

			ObjectCreator() :m_dim() ,m_divisions(3) {}
			bool ParseKeyword(ParseParams& p, HashValue kw) override
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
			pr::rdr::ModelPtr CreateModel(ParseParams& p, std::string name) override
			{
				auto model = pr::rdr::ModelGenerator<>::Geosphere(p.m_rdr, m_dim, m_divisions, pr::Colour32White, GetDrawMethod(p));
				model->m_name = name;
				return model;
			}
		};

		// Base class for cone shapes
		struct IObjectCreatorCone :IObjectCreatorTexture
		{
			int m_axis_id;
			pr::v4 m_dim; // x,y = radius, z = height
			pr::v2 m_scale;
			int m_layers, m_wedges;

			IObjectCreatorCone() :m_axis_id(1) ,m_dim() ,m_scale() ,m_layers(1) ,m_wedges(20) {}
			bool ParseKeyword(ParseParams& p, HashValue kw) override
			{
				switch (kw) {
				default: return IObjectCreatorTexture::ParseKeyword(p, kw);
				case EKeyword::Layers:  p.m_reader.ExtractInt(m_layers, 10); return true;
				case EKeyword::Wedges:  p.m_reader.ExtractInt(m_wedges, 10); return true;
				case EKeyword::Scale:   p.m_reader.ExtractVector2(m_scale); return true;
				}
			}
			pr::rdr::ModelPtr CreateModel(ParseParams& p, std::string name) override
			{
				// Get the transform so that model is aligned to 'axis_id'
				pr::m4x4 o2w = pr::m4x4Identity;
				switch (m_axis_id)
				{
				default:
					p.m_reader.ReportError(pr::script::EResult::UnknownValue, "axis_id must one of ±1, ±2, ±3");
					return nullptr;
				case  1: o2w = pr::Rotation4x4(0.0f ,-pr::maths::tau_by_4 ,0.0f ,pr::v4Origin); break;
				case -1: o2w = pr::Rotation4x4(0.0f , pr::maths::tau_by_4 ,0.0f ,pr::v4Origin); break;
				case  2: o2w = pr::Rotation4x4(-pr::maths::tau_by_4 ,0.0f ,0.0f ,pr::v4Origin); break;
				case -2: o2w = pr::Rotation4x4( pr::maths::tau_by_4 ,0.0f ,0.0f ,pr::v4Origin); break;
				case  3: o2w = pr::m4x4Identity; break;
				case -3: o2w = pr::Rotation4x4(0.0f ,pr::maths::tau_by_2 ,0.0f ,pr::v4Origin); break;
				}

				// Create the model
				auto model = pr::rdr::ModelGenerator<>::Cylinder(p.m_rdr ,m_dim.x ,m_dim.y ,m_dim.z ,o2w ,m_scale.x ,m_scale.y ,m_wedges ,m_layers ,1 ,&pr::Colour32White ,GetDrawMethod(p));
				model->m_name = name;
				return model;
			}
		};

		// ELdrObject::CylinderHR
		template <> struct ObjectCreator<ELdrObject::CylinderHR> :IObjectCreatorCone
		{
			void Parse(ParseParams& p) override
			{
				p.m_reader.ExtractInt(m_axis_id, 10);
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
				p.m_reader.ExtractInt(m_axis_id, 10);
				p.m_reader.ExtractReal(h0);
				p.m_reader.ExtractReal(h1);
				p.m_reader.ExtractReal(a);

				m_dim.z = h1 - h0;
				m_dim.x = h0 * pr::Tan(a);
				m_dim.y = h1 * pr::Tan(a);
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
			pr::rdr::EPrim::Enum_ m_prim_type;
			bool m_generate_normals;

			IObjectCreatorMesh() :m_verts(Point()) ,m_normals(Norms()) ,m_colours(Color()) ,m_texs(Texts()) ,m_indices(Index()) ,m_prim_type() ,m_generate_normals(false) {}
			bool ParseKeyword(ParseParams& p, HashValue kw) override
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
						m_prim_type = pr::rdr::EPrim::LineList;
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
						m_prim_type = pr::rdr::EPrim::TriList;
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
						m_prim_type = pr::rdr::EPrim::TriList;
						return true;
					}
				case EKeyword::GenerateNormals:
					{
						m_generate_normals = true;
						return true;
					}
				}
			}
			pr::rdr::ModelPtr CreateModel(ParseParams& p, std::string name) override
			{
				using namespace pr::rdr;

				// Validate
				if (m_indices.empty() || m_verts.empty())
				{
					p.m_reader.ReportError("Mesh object description incomplete");
					return nullptr;
				}

				// Generate normals if needed
				if (m_generate_normals)
				{
					m_normals.resize(m_verts.size());
					pr::geometry::GenerateNormals(m_indices.size(), m_indices.data(),
						[&](std::size_t i){ return m_verts[i]; },
						[&](std::size_t i){ return m_normals[i]; },
						[&](std::size_t i, pr::v4 const& nm){ m_normals[i] = nm; });
				}

				// Create the model
				auto model = ModelGenerator<>::Mesh(
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
					GetDrawMethod(p));
				model->m_name = name;
				return model;
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
			pr::rdr::ModelPtr CreateModel(ParseParams& p, std::string name) override
			{
				if (m_prim_type == pr::rdr::EPrim::LineList)
				{
					m_generate_normals = false;
					m_normals.clear();
				}
				return IObjectCreatorMesh::CreateModel(p, name);
			}
		};

		// ELdrObject::ConvexHull
		template <> struct ObjectCreator<ELdrObject::ConvexHull> :IObjectCreatorMesh
		{
			void Parse(ParseParams& p) override
			{
				p.m_reader.ReportError(pr::script::EResult::UnknownValue, "Convext hull object description invalid");
				p.m_reader.FindSectionEnd();
			}
			pr::rdr::ModelPtr CreateModel(ParseParams& p, std::string name) override
			{
				m_indices.resize(6 * (m_verts.size() - 2));

				// Find the convex hull
				size_t num_verts = 0, num_faces = 0;
				pr::ConvexHull(m_verts, m_verts.size(), &m_indices[0], &m_indices[0] + m_indices.size(), num_verts, num_faces);
				m_verts  .resize(num_verts);
				m_indices.resize(3*num_faces);

				m_prim_type = pr::rdr::EPrim::TriList;
				m_generate_normals = true;
				return IObjectCreatorMesh::CreateModel(p, name);
			}
		};

		// Parse an ldr object
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
					HashValue kw = p.m_reader.NextKeywordH();
					ParseParams pp(p, obj->m_child, kw, obj.m_ptr);
					if (ParseLdrObject(pp)) continue;
					if (ParseProperties(pp, kw, obj)) continue;
					if (creator.ParseKeyword(p, kw)) continue;
					p.m_reader.ReportError(pr::script::EResult::UnknownToken);
					continue;
				}
				else
				{
					creator.Parse(p);
				}
			}
			p.m_reader.SectionEnd();

			// Create the model and add the model and instance to the containers
			obj->m_model = creator.CreateModel(p, obj->TypeAndName());
			p.m_models[pr::hash::HashC(obj->m_name.c_str())] = obj->m_model;
			p.m_objects.push_back(obj);
		}

		// Read a group description
		void ParseGroup(ParseParams& p)
		{
			// Read the object attributes: name, colour, instance
			ObjectAttributes attr = ParseAttributes(p.m_reader, ELdrObject::Group);
			LdrObjectPtr obj(new LdrObject(attr, p.m_parent, p.m_context_id));

			// Read the description of the model
			p.m_reader.SectionStart();
			while (!p.m_reader.IsSectionEnd())
			{
				if (p.m_reader.IsKeyword())
				{
					HashValue kw = p.m_reader.NextKeywordH();
					ParseParams pp(p, obj->m_child, kw, obj.m_ptr);
					if (ParseLdrObject(pp)) continue;
					if (ParseProperties(pp, kw, obj)) continue;
					p.m_reader.ReportError(pr::script::EResult::UnknownToken);
				}
				else
				{
					p.m_reader.ReportError(pr::script::EResult::UnknownValue);
					p.m_reader.FindSectionEnd();
				}
			}
			p.m_reader.SectionEnd();

			// Object modifiers applied to groups are applied recursively to children within the group
			obj->m_colour_mask = 0xFFFFFFFF; // The group colour tints all children
			if (obj->m_wireframe)
			{
				for (auto child : obj->m_child)
					child->Wireframe(true, true);
			}
			if (!obj->m_visible)
			{
				for (auto child : obj->m_child)
					child->Visible(false, true);
			}

			// Add the model and instance to the containers
			p.m_objects.push_back(obj);
		}

		// Read an instance description
		void ParseInstance(ParseParams& p)
		{
			// Read the object attributes: name, colour, instance. (note, instance will be ignored)
			ObjectAttributes attr = ParseAttributes(p.m_reader, ELdrObject::Instance);

			// Locate the model that this is an instance of
			auto model_key = pr::hash::HashC(attr.m_name.c_str());
			auto mdl = p.m_models.find(model_key);
			if (mdl == p.m_models.end())
			{
				p.m_reader.ReportError(pr::script::EResult::UnknownValue, "Instance not found");
				return;
			}

			LdrObjectPtr obj(new LdrObject(attr, p.m_parent, p.m_context_id));
			obj->m_model = mdl->second;

			// Parse any properties of the instance
			p.m_reader.SectionStart();
			while (!p.m_reader.IsSectionEnd())
			{
				if (p.m_reader.IsKeyword())
				{
					HashValue kw = p.m_reader.NextKeywordH();
					ParseParams pp(p, obj->m_child, kw, obj.m_ptr);
					if (ParseLdrObject(pp)) continue;
					if (ParseProperties(pp, kw, obj)) continue;
					p.m_reader.ReportError(pr::script::EResult::UnknownToken);
				}
				else
				{
					p.m_reader.ReportError(pr::script::EResult::UnknownValue);
					p.m_reader.FindSectionEnd();
				}
			}
			p.m_reader.SectionEnd();

			// Add the instance to the container
			p.m_objects.push_back(obj);
		}

		// Read an ldr object from the script.
		// Returns true if the next keyword is a ldr object, false if the keyword is unrecognised
		bool ParseLdrObject(ParseParams& p)
		{
			auto object_count = p.m_objects.size();
			switch ((ELdrObject::Enum_)p.m_keyword)
			{
			default: return false;
			case ELdrObject::Line:         Parse<ELdrObject::Line>      (p); break;
			case ELdrObject::LineD:        Parse<ELdrObject::LineD>     (p); break;
			case ELdrObject::LineList:     Parse<ELdrObject::LineList>  (p); break;
			case ELdrObject::LineBox:      Parse<ELdrObject::LineBox>   (p); break;
			case ELdrObject::Spline:       Parse<ELdrObject::Spline>    (p); break;
			case ELdrObject::Circle:       Parse<ELdrObject::Circle>    (p); break;
			case ELdrObject::Rect:         Parse<ELdrObject::Rect>      (p); break;
			case ELdrObject::Matrix3x3:    Parse<ELdrObject::Matrix3x3> (p); break;
			case ELdrObject::Triangle:     Parse<ELdrObject::Triangle>  (p); break;
			case ELdrObject::Quad:         Parse<ELdrObject::Quad>      (p); break;
			case ELdrObject::Plane:        Parse<ELdrObject::Plane>     (p); break;
			case ELdrObject::Box:          Parse<ELdrObject::Box>       (p); break;
			case ELdrObject::BoxLine:      Parse<ELdrObject::BoxLine>   (p); break;
			case ELdrObject::BoxList:      Parse<ELdrObject::BoxList>   (p); break;
			case ELdrObject::FrustumWH:    Parse<ELdrObject::FrustumWH> (p); break;
			case ELdrObject::FrustumFA:    Parse<ELdrObject::FrustumFA> (p); break;
			case ELdrObject::Sphere:       Parse<ELdrObject::Sphere>    (p); break;
			case ELdrObject::CylinderHR:   Parse<ELdrObject::CylinderHR>(p); break;
			case ELdrObject::ConeHA:       Parse<ELdrObject::ConeHA>    (p); break;
			case ELdrObject::Mesh:         Parse<ELdrObject::Mesh>      (p); break;
			case ELdrObject::ConvexHull:   Parse<ELdrObject::ConvexHull>(p); break;
			case ELdrObject::Group:        ParseGroup                   (p); break;
			case ELdrObject::Instance:     ParseInstance                (p); break;
			}

			// Apply properties to each object added
			for (auto i = object_count, iend = p.m_objects.size(); i != iend; ++i)
			{
				LdrObjectPtr& obj = p.m_objects[i];
				++p.m_obj_count;

				// Set colour on 'obj' (so that render states are set correctly)
				// Note that the colour is 'blended' with 'm_base_colour' so
				// m_base_colour * White = m_base_colour.
				obj->SetColour(pr::Colour32White, 0xFFFFFFFF, false);

				// Apply the colour of 'obj' to all children using a mask
				if (obj->m_colour_mask != 0)
					for (auto& child : obj->m_child)
						child->SetColour(obj->m_base_colour, obj->m_colour_mask, true);

				// If flagged as wireframe, set wireframe
				if (obj->m_wireframe)
					obj->Wireframe(true, false);

				// If flagged as hidden, hide
				if (!obj->m_visible)
					obj->Visible(false, false);
			}

			// Give progress updates
			auto now = GetTickCount();
			if (now - p.m_start_time > 200 && now - p.m_last_update > 100)
			{
				p.m_last_update = now;
				pr::events::Send(Evt_LdrProgress((int)p.m_obj_count, -1, "Parsing scene", true, p.m_objects.back()));
			}
			return true;
		}

		// Add the ldr objects described in 'reader' to 'objects'
		// Note: this is done as a background thread while a progrss dialog is displayed
		void Add(pr::Renderer& rdr, pr::script::Reader& reader, ObjectCont& objects, ContextId context_id, bool async)
		{
			// Helper object for forwarding LdrProgress events to a dialog
			struct OnLdrProgress :pr::events::IRecv<Evt_LdrProgress>
			{
				pr::gui::ProgressDlg* m_dlg;
				OnLdrProgress(pr::gui::ProgressDlg* dlg) :m_dlg(dlg) {}
				void OnEvent(Evt_LdrProgress const& e)
				{
					if (m_dlg == nullptr) return;

					// Adding objects generates progress events.
					char const* type = e.m_obj ? ELdrObject::ToString(e.m_obj->m_type) : "";
					std::string name = e.m_obj ? e.m_obj->m_name : "";
					m_dlg->Progress(e.m_count / (float)e.m_total, (e.m_total == -1) ?
						pr::FmtS("%s...\r\nObject count: %d\r\n%s %s" ,e.m_desc ,e.m_count ,type ,name.c_str()) :
						pr::FmtS("%s...\r\nObject: %d of %d\r\n%s %s" ,e.m_desc ,e.m_count ,e.m_total ,type ,name.c_str()));
				}
			};

			// Does the work of parsing objects and adds them to 'models'
			// 'total' is the total number of objects added
			auto ParseObjects = [&](pr::gui::ProgressDlg* dlg)
				{
					// CoInitialise
					pr::InitCom init_com;

					ModelCont models;
					std::size_t total = 0;
					OnLdrProgress on_ldr_progress(dlg);
					DWORD now = GetTickCount();

					int initial = int(objects.size());
					for (EKeyword kw; reader.NextKeywordH(kw);)
					{
						switch (kw)
						{
						default:
							{
								ParseParams pp(rdr, reader, objects, models, context_id, kw, 0, total, now);
								if (ParseLdrObject(pp)) continue;
								reader.ReportError(pr::script::EResult::UnknownToken);
								break;
							}

						// Application commands
						case EKeyword::Clear: break; // use event
						case EKeyword::Wireframe: break;
						case EKeyword::Camera: break;
						case EKeyword::Lock: break;
						case EKeyword::Delimiters: break;
						}
					}
					int final = int(objects.size());

					// Notify observers of the objects that have been added
					pr::events::Send(Evt_AddBegin());
					for (int idx = 0, total = final - initial; idx != total; ++idx)
						pr::events::Send(Evt_LdrObjectAdd(objects[idx + initial]));
					pr::events::Send(Evt_AddEnd(initial, final));
				};

			if (async)
			{
				// Run the adding process as a background task while displaying a progress dialog
				pr::gui::ProgressDlg dlg("Processing ldr script", "", ParseObjects);
				dlg.DoModal(100);
			}
			else
			{
				ParseObjects(nullptr);
			}

			// Release scratch buffers
			Point().clear();
			Norms().clear();
			Index().clear();
			Color().clear();
			Texts().clear();
		}

		// Add a custom object
		LdrObjectPtr Add(
			pr::Renderer& rdr,
			ObjectAttributes attr,
			pr::rdr::EPrim prim_type,
			int icount,
			int vcount,
			pr::uint16 const* indices,
			pr::v4 const* verts,
			int ccount,
			pr::Colour32 const* colours,
			int ncount,
			pr::v4 const* normals,
			pr::v2 const* tex_coords,
			ContextId context_id)
		{
			LdrObjectPtr obj(new LdrObject(attr, 0, context_id));

			pr::rdr::EGeom  geom_type  = pr::rdr::EGeom::Vert;
			if (normals)    geom_type |= pr::rdr::EGeom::Norm;
			if (colours)    geom_type |= pr::rdr::EGeom::Colr;
			if (tex_coords) geom_type |= pr::rdr::EGeom::Tex0;

			// Create a tint material
			pr::rdr::DrawMethod mat = rdr.m_shdr_mgr.FindShaderFor(geom_type);

			// Create the model
			obj->m_model = pr::rdr::ModelGenerator<>::Mesh(rdr, prim_type, vcount, icount, verts, indices, ccount, colours, ncount, normals, tex_coords, &mat);
			obj->m_model->m_name = obj->TypeAndName();
			pr::events::Send(Evt_LdrObjectAdd(obj));
			return obj;
		}

		// Add a custom object via callback
		// Objects created by this method will have dynamic usage and are suitable for updating every frame
		// They are intended to be used with the 'Edit' function.
		LdrObjectPtr Add(pr::Renderer& rdr, ObjectAttributes attr, int icount, int vcount, EditObjectCB edit_cb, void* ctx, ContextId context_id)
		{
			LdrObjectPtr obj(new LdrObject(attr, 0, context_id));

			// Create buffers for a dynamic model
			pr::rdr::VBufferDesc vbs(vcount, sizeof(pr::rdr::VertPCNT), D3D11_USAGE_DYNAMIC, D3D11_CPU_ACCESS_WRITE);
			pr::rdr::IBufferDesc ibs(icount, sizeof(pr::uint16), pr::rdr::DxFormat<pr::uint16>::value, D3D11_USAGE_DYNAMIC, D3D11_CPU_ACCESS_WRITE);
			pr::rdr::MdlSettings settings(vbs, ibs);

			// Create the model
			obj->m_model = rdr.m_mdl_mgr.CreateModel(settings);
			obj->m_model->m_name = obj->TypeAndName();

			// Initialise it via the callback
			edit_cb(obj->m_model, ctx, rdr);
			pr::events::Send(Evt_LdrObjectAdd(obj));
			return obj;
		}

		// Modify the geometry of an LdrObject
		void Edit(pr::Renderer& rdr, LdrObjectPtr object, EditObjectCB edit_cb, void* ctx)
		{
			edit_cb(object->m_model, ctx, rdr);
			pr::events::Send(Evt_LdrObjectChg(object));
		}

		// Remove all objects from 'objects' that have a context id matching one in 'doomed' and not in 'excluded'
		// If 'doomed' is 0, all are assumed doomed. If 'excluded' is 0, none are assumed excluded
		// 'excluded' is considered after 'doomed' so if any context ids are in both arrays, they will be excluded.
		void Remove(ObjectCont& objects, ContextId const* doomed, std::size_t dcount, ContextId const* excluded, std::size_t ecount)
		{
			ContextId const* dend = doomed   + dcount;
			ContextId const* eend = excluded + ecount;
			for (size_t i = objects.size(); i-- != 0;)
			{
				if (doomed   && std::find(doomed  , dend, objects[i]->m_context_id) == dend) continue; // not in the doomed list
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
R"(
//********************************************
// LineDrawer demo scene
//  Copyright © Rylogic Ltd 2009
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
		*M4x4 {1 0 0 0  0 1 0 0  0 0 1 0  0 0 0 1}  // *M4x4 {xx xy xz xw  yx yy yz yw  zx zy zz zw  wx wy wz ww} - i.e. row major
		*M3x3 {1 0 0  0 1 0  0 0 1}                 // *M3x3 {xx xy xz  yx yy yz  zx zy zz} - i.e. row major
		*Pos {0 1 0}                                // *Pos {x y z}
		*Direction {0 1 0 3}                        // *Direction {dx dy dz axis_id} - direction vector, and axis id to align to that direction
		*Quat {0 1 0 0.3}                           // *Quat {x y z s} - quaternion
		*Rand4x4 {0 1 0 2}                          // *Rand4x4 {cx cy cz r} - centre position, radius. Random orientation
		*RandPos {0 1 0 2}                          // *RandPos {cx cy cz r} - centre position, radius
		*RandOri                                    // Randomises the orientation of the current transform
		*Scale {1 1.2 1}                            // *Scale { sx sy sz } - multiples the lengths of x,y,z vectors of the current transform
		*Transpose                                  // Transposes the current transform
		*Inverse                                    // Inverts the current transform
		*Euler {45 30 60}                           // *Euler { pitch yaw roll } - all in degrees. Order of rotations is roll, pitch, yaw
		*Normalise                                  // Normalises the lengths of the vectors of the current transform
		*Orthonormalise                             // Normalises the lengths and makes orthogonal the vectors of the current transform
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
	3 1 2
	*RandColour              // Note: this will not be inheritted by the instances
}

*Instance model_instancing FFFF0000   // The name indicates which model to instance
{
	*o2w {*Pos {2 0 0}}
}
*Instance model_instancing FF0000FF
{
	*o2w {*Pos {1 0.2 0.5}}
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
// Below is an example of every supported object type with notes on their syntax
// ************************************************************************************

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
*LineList linelist
{
	*Coloured                        // Optional. *Coloured is valid for all line types
	0 0 0                            // Note, colour and *param cannot be given here since there is no previous line to apply them to, doing so will cause a parse error.
	0 0 1 FF00FF00 *Param {-0.2 0.5} // Colour and *Param apply to each previous line
	0 1 1 FF0000FF
	1 1 1 FFFF00FF
	1 1 0 FFFFFF00 *Param {0.5 0.9}
	1 0 0 FF00FFFF
}

// A cuboid made from lines
*LineBox linebox
{
	2 4 1 // Width, height, depth. Accepts 1, 2, or 3 dimensions. 1dim = cube, 2 = rod, 3 = arbitrary box
}

// A curve described by a start and end point and two control points
*Spline spline
{
	*Coloured                           // Optional. If specified each spline has an aarrggbb colour after it. Must occur before any spline data if used
	0 0 0  0 0 1  1 0 1  1 0 0 FF00FF00 // p0 p1 p2 p3 - all points are positions
	0 0 0  1 0 0  1 1 0  1 1 1 FFFF0000 // tangents given by p1-p0, p3-p2
}

// A circle or ellipse
*Circle circle
{
	2 1.6                               // axis_id, radius. axis_id: ±1 = ±x, ±2 = ±y, ±3 = ±z
	*Solid                              // Optional, if omitted then the circle is an outline only
	*RandColour *o2w{*RandPos{0 0 0 2}} // Object colour is the outline colour
	//*Facets { 40 }                    // Optional, controls the smoothness of the edge
}
*Circle ellipse
{
	2 1.6 0.8                           // axis_id, radiusx, radiusy. axis_id: ±1 = ±x, ±2 = ±y, ±3 = ±z
	*Solid                              // Optional, if omitted then the circle is an outline only
	*RandColour *o2w{*RandPos{0 0 0 2}} // Object colour is the outline colour
	//*Facets { 40 }                      // Optional, controls the smoothness of the edge
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
// Points along the z axis. Width, Height given at '1' along the z axis
*FrustumWH frustumwh
{
	2 1 1 0 1.5                         // axis_id, width, height, near plane, far plane. axis_id: ±1 = ±x, ±2 = ±y, ±3 = ±z
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
	*GenerateNormals
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
// Ldr script syntax and features:
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
	}
}