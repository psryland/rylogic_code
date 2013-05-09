//***************************************************************************************************
// Ldr Object Manager
//  Copyright © Rylogic Ltd 2009
//***************************************************************************************************

#include <string>
#include <sstream>
#include <unordered_map>
#include "pr/linedrawer/ldr_object.h"
#include "pr/linedrawer/ldr_objects_dlg.h"
#include "pr/common/assert.h"
#include "pr/common/hash.h"
#include "pr/common/windows_com.h"
#include "pr/maths/convexhull.h"
#include "pr/threads/background_task.h"
#include "pr/gui/progress_dlg.h"
#include "pr/renderer11/renderer.h"

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

// LdrObjectUIData ***************************
pr::ldr::LdrObjectUIData::LdrObjectUIData()
	:m_tree_item(pr::ldr::INVALID_TREE_ITEM)
	,m_list_item(pr::ldr::INVALID_LIST_ITEM)
{}

// LdrObject ***********************************
pr::ldr::LdrObject::LdrObject(ObjectAttributes const& attr, pr::ldr::LdrObject* parent, pr::ldr::ContextId context_id)
	:pr::ldr::RdrInstance()
	,m_o2p(pr::m4x4Identity)
	,m_type(attr.m_type)
	,m_parent(parent)
	,m_child()
	,m_name(attr.m_name)
	,m_context_id(context_id)
	,m_base_colour(attr.m_colour)
	,m_colour_mask()
	,m_anim()
	,m_uidata()
	,m_instanced(attr.m_instance)
	,m_visible(true)
	,m_wireframe(false)
	,m_user_data(0)
{
	m_i2w    = pr::m4x4Identity;
	m_colour = m_base_colour;
	PR_EXPAND(PR_DBG, ++g_ldr_object_tracker);
}
pr::ldr::LdrObject::~LdrObject()
{
	PR_EXPAND(PR_DBG, --g_ldr_object_tracker);
}

// Return the declaration name of this object
std::string pr::ldr::LdrObject::TypeAndName() const
{
	return std::string(ELdrObject::ToString(m_type)) + " " + m_name;
}

// Recursively add this object and its children to a viewport
void pr::ldr::LdrObject::AddToScene(pr::rdr::Scene& scene, float time_s, pr::m4x4 const* p2w)
{
	// Set the instance to world
	m_i2w = *p2w * m_o2p * m_anim.Step(time_s);

	// Add the instance to the viewport drawlist
	if (m_instanced && m_visible && m_model)
		scene.AddInstance(*this); // Could add occlusion culling here...

	// Rince and repeat for all children
	for (ObjectCont::iterator i = m_child.begin(), iend = m_child.end(); i != iend; ++i)
		(*i)->AddToScene(scene, time_s, &m_i2w);
}

//// Recursively add this object using 'bbox_model' instead of its
//// actual model, located and scaled to the transform and box of this object
//void pr::ldr::LdrObject::AddBBoxToViewport(pr::rdr::Viewport& viewport, pr::rdr::ModelPtr bbox_model, float time_s, pr::m4x4 const* p2w)
//{
//	// Set the instance to world
//	pr::m4x4 i2w = *p2w * m_o2p * m_anim.Step(time_s);
//
//	// Add the instance to the viewport drawlist
//	if (m_instanced && m_visible && m_model)
//	{
//#pragma message(PR_LINK "this wont work")
//		m_model = bbox_model;
//		m_i2w = i2w;
//		m_i2w.x *= m_model->m_bbox.SizeX() + pr::maths::tiny;
//		m_i2w.y *= m_model->m_bbox.SizeY() + pr::maths::tiny;
//		m_i2w.z *= m_model->m_bbox.SizeZ() + pr::maths::tiny;
//		m_i2w.w  = i2w.w + m_model->m_bbox.Centre();
//		m_i2w.w.w = 1.0f;
//		viewport.AddInstance(*this); // Could add occlusion culling here...
//	}
//
//	// Rince and repeat for all children
//	for (ObjectCont::iterator i = m_child.begin(), iend = m_child.end(); i != iend; ++i)
//		(*i)->AddBBoxToViewport(viewport, bbox_model, time_s, &i2w);
//}

// Change visibility for this object
void pr::ldr::LdrObject::Visible(bool visible, bool include_children)
{
	m_visible = visible;
	if (!include_children) return;
	for (ObjectCont::iterator i = m_child.begin(), iend = m_child.end(); i != iend; ++i)
		(*i)->Visible(visible, include_children);
}

// Change the render mode for this object
void pr::ldr::LdrObject::Wireframe(bool wireframe, bool include_children)
{
	m_wireframe = wireframe;
	if (m_wireframe) m_rsb.Set(pr::rdr::ERS::FillMode, D3D11_FILL_WIREFRAME);
	else             m_rsb.Clear(pr::rdr::ERS::FillMode);
	if (!include_children) return;
	for (ObjectCont::iterator i = m_child.begin(), iend = m_child.end(); i != iend; ++i)
		(*i)->Wireframe(wireframe, include_children);
}

// Change the colour of this object
void pr::ldr::LdrObject::SetColour(pr::Colour32 colour, pr::uint mask, bool include_children)
{
	pr::Colour32 blend = m_base_colour * colour;
	m_colour.m_aarrggbb = pr::SetBits(m_colour.m_aarrggbb, mask, blend.m_aarrggbb);

	bool has_alpha = m_colour.a() != 0xFF;
	m_sko.Alpha(has_alpha);
	pr::rdr::SetAlphaBlending(m_bsb, m_dsb, m_rsb, 1, has_alpha);

	if (!include_children) return;
	for (ObjectCont::iterator i = m_child.begin(), iend = m_child.end(); i != iend; ++i)
		(*i)->SetColour(colour, mask, include_children);
}

// Called when there are no more references to this object
void pr::ldr::LdrObject::RefCountZero(RefCount<LdrObject>* doomed)
{
	pr::events::Send(pr::ldr::Evt_LdrObjectDelete(static_cast<LdrObject*>(doomed)));
	delete doomed;
}

long pr::ldr::LdrObject::AddRef() const
{
	return pr::RefCount<pr::ldr::LdrObject>::AddRef();
}
long pr::ldr::LdrObject::Release() const
{
	return pr::RefCount<pr::ldr::LdrObject>::Release();
}

// LdrObject Creation functions *********************************************
namespace pr
{
	namespace ldr
	{
		typedef std::unordered_map<pr::hash::HashValue, pr::rdr::ModelPtr> ModelCont;
	}
}

// Helper object for passing parameters between parsing functions
struct ParseParams
{
	pr::Renderer&               m_rdr;
	pr::script::Reader&         m_reader;
	pr::ldr::ObjectCont&        m_objects;
	pr::ldr::ModelCont&         m_models;
	pr::ldr::ContextId          m_context_id;
	pr::hash::HashValue         m_keyword;
	pr::ldr::LdrObject*         m_parent;
	std::size_t&                m_obj_count;
	std::size_t                 m_start_time;
	std::size_t                 m_last_update;

	ParseParams(
		pr::Renderer&              rdr,
		pr::script::Reader&        reader,
		pr::ldr::ObjectCont&       objects,
		pr::ldr::ModelCont&        models,
		pr::ldr::ContextId         context_id,
		pr::hash::HashValue        keyword,
		pr::ldr::LdrObject*        parent,
		std::size_t&               obj_count,
		std::size_t                start_time)
		:m_rdr                     (rdr        )
		,m_reader                  (reader     )
		,m_objects                 (objects    )
		,m_models                  (models     )
		,m_context_id              (context_id )
		,m_keyword                 (keyword    )
		,m_parent                  (parent     )
		,m_obj_count               (obj_count  )
		,m_start_time              (start_time )
		,m_last_update             (start_time )
	{}

	ParseParams(
		ParseParams&              p,
		pr::ldr::ObjectCont&      objects,
		pr::hash::HashValue       keyword,
		pr::ldr::LdrObject*       parent)
		:m_rdr                    (p.m_rdr        )
		,m_reader                 (p.m_reader     )
		,m_objects                (objects        )
		,m_models                 (p.m_models     )
		,m_context_id             (p.m_context_id )
		,m_keyword                (keyword        )
		,m_parent                 (parent         )
		,m_obj_count              (p.m_obj_count  )
		,m_start_time             (p.m_start_time )
		,m_last_update            (p.m_last_update)
	{}

private:
	ParseParams(ParseParams const&);
	ParseParams& operator=(ParseParams const&);
};

// Forward declare the recursive object parsing function
bool ParseLdrObject(ParseParams& p);

// Read the name, colour, and instance flag for an object
pr::ldr::ObjectAttributes ParseAttributes(pr::script::Reader& reader, pr::ldr::ELdrObject model_type)
{
	pr::ldr::ObjectAttributes attr;
	attr.m_type = model_type;
	attr.m_name = pr::ldr::ELdrObject::ToString(model_type);
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
	for (pr::ldr::EKeyword kw; reader.NextKeywordH(kw);)
	{
		switch (kw)
		{
		default: reader.ReportError(pr::script::EResult::UnknownToken); break;
		case pr::ldr::EKeyword::M4x4:      reader.ExtractMatrix4x4S(p2w); break;
		case pr::ldr::EKeyword::M3x3:      reader.ExtractMatrix3x3S(pr::cast_m3x3(p2w)); break;
		case pr::ldr::EKeyword::Pos:       reader.ExtractVector3S(p2w.pos, 1.0f); break;
		case pr::ldr::EKeyword::Direction:
			{
				pr::v4 direction; int axis;
				reader.SectionStart();
				reader.ExtractVector3(direction, 0.0f);
				reader.ExtractInt(axis, 10);
				reader.SectionEnd();
				pr::OriFromDir(cast_m3x3(p2w), direction, axis, pr::v4YAxis);
			}break;
		case pr::ldr::EKeyword::Quat:
			{
				pr::Quat quat;
				reader.ExtractVector4S(pr::cast_v4(quat));
				cast_m3x3(p2w).set(quat);
			}break;
		case pr::ldr::EKeyword::Rand4x4:
			{
				pr::v4 centre; float radius;
				reader.SectionStart();
				reader.ExtractVector3(centre, 1.0f);
				reader.ExtractReal(radius);
				reader.SectionEnd();
				p2w = pr::Random4x4(centre, radius);
			}break;
		case pr::ldr::EKeyword::RandPos:
			{
				pr::v4 centre; float radius;
				reader.SectionStart();
				reader.ExtractVector3(centre, 1.0f);
				reader.ExtractReal(radius);
				reader.SectionEnd();
				p2w.pos = Random3(centre, radius, 1.0f);
			}break;
		case pr::ldr::EKeyword::RandOri:
			{
				pr::cast_m3x3(p2w) = pr::Random3x3();
			}break;
		case pr::ldr::EKeyword::Euler:
			{
				pr::v4 angles;
				reader.ExtractVector3S(angles, 0.0f);
				pr::cast_m3x3(p2w) = pr::m3x3::make(pr::DegreesToRadians(angles.x), pr::DegreesToRadians(angles.y), pr::DegreesToRadians(angles.z));
			}break;
		case pr::ldr::EKeyword::Scale:
			{
				pr::v4 scale;
				reader.ExtractVector3S(scale, 0.0f);
				p2w.x *= scale.x;
				p2w.y *= scale.y;
				p2w.z *= scale.z;
			}break;
		case pr::ldr::EKeyword::Transpose:
			{
				pr::Transpose4x4(p2w);
			}break;
		case pr::ldr::EKeyword::Inverse:
			{
				pr::Inverse(p2w);
			}break;
		case pr::ldr::EKeyword::Normalise:
			{
				p2w.x = pr::Normalise3(p2w.x);
				p2w.y = pr::Normalise3(p2w.y);
				p2w.z = pr::Normalise3(p2w.z);
			}break;
		case pr::ldr::EKeyword::Orthonormalise:
			{
				pr::Orthonormalise(p2w);
			}break;
		}
	}
	reader.SectionEnd();

	// Premultiply the transform
	o2w = p2w * o2w;
}

// Parse a simple animation description
void ParseAnimation(pr::script::Reader& reader, pr::ldr::Animation& anim)
{
	reader.SectionStart();
	for (pr::ldr::EKeyword kw; reader.NextKeywordH(kw);)
	{
		switch (kw)
		{
		default: reader.ReportError(pr::script::EResult::UnknownToken); break;
		case pr::ldr::EKeyword::Style:
			{
				char style[50];
				reader.ExtractIdentifier(style);
				if      (pr::str::EqualI(style, "NoAnimation"    )) anim.m_style = pr::ldr::EAnimStyle::NoAnimation;
				else if (pr::str::EqualI(style, "PlayOnce"       )) anim.m_style = pr::ldr::EAnimStyle::PlayOnce;
				else if (pr::str::EqualI(style, "PlayReverse"    )) anim.m_style = pr::ldr::EAnimStyle::PlayReverse;
				else if (pr::str::EqualI(style, "PingPong"       )) anim.m_style = pr::ldr::EAnimStyle::PingPong;
				else if (pr::str::EqualI(style, "PlayContinuous" )) anim.m_style = pr::ldr::EAnimStyle::PlayContinuous;
			}break;
		case pr::ldr::EKeyword::Period:
			{
				reader.ExtractReal(anim.m_period);
			}break;
		case pr::ldr::EKeyword::Velocity:
			{
				reader.ExtractVector3(anim.m_velocity, 0.0f);
			}break;
		case pr::ldr::EKeyword::AngVelocity:
			{
				reader.ExtractVector3(anim.m_ang_velocity, 0.0f);
			}break;
		}
	}
	reader.SectionEnd();
}

// Parse a step block for an object
void ParseStep(pr::script::Reader& reader, pr::ldr::LdrObjectStepData& step)
{
	reader.ExtractSection(step.m_code, false);
}

// Parse keywords that can appear in any section
// Returns true if the keyword was recognised
bool ParseProperties(ParseParams& p, pr::hash::HashValue kw, pr::ldr::LdrObjectPtr obj)
{
	switch (kw)
	{
	default: return false;
	case pr::ldr::EKeyword::O2W:        ParseTransform(p.m_reader, obj->m_o2p); return true;
	case pr::ldr::EKeyword::Colour:     p.m_reader.ExtractIntS(obj->m_base_colour.m_aarrggbb, 16); return true;
	case pr::ldr::EKeyword::ColourMask: p.m_reader.ExtractIntS(obj->m_colour_mask, 16); return true;
	case pr::ldr::EKeyword::RandColour: obj->m_base_colour = pr::RandomRGB(); return true;
	case pr::ldr::EKeyword::Animation:  ParseAnimation(p.m_reader, obj->m_anim); return true;
	case pr::ldr::EKeyword::Hidden:     obj->m_visible = false; return true;
	case pr::ldr::EKeyword::Wireframe:  obj->m_wireframe = true; return true;
	case pr::ldr::EKeyword::Step:       ParseStep(p.m_reader, obj->m_step); pr::events::Send(pr::ldr::Evt_LdrObjectStepCode(obj)); return true;
	}
}

// Parse a texture description
// Returns a pointer to the pr::rdr::Texture created in the renderer
bool ParseTexture(ParseParams& p, pr::rdr::Texture2DPtr& tex)
{
	std::string tex_filepath;
	p.m_reader.SectionStart();
	p.m_reader.ExtractString(tex_filepath);
	if (!tex_filepath.empty())
	{
		pr::m4x4 t2s = pr::m4x4Identity;
		pr::rdr::SamplerDesc sam;

		// Parse extra texture properties
		for (pr::ldr::EKeyword kw; p.m_reader.NextKeywordH(kw);)
		{
			char word[20];
			switch (kw)
			{
			default: p.m_reader.ReportError(pr::script::EResult::UnknownToken); break;
			case pr::ldr::EKeyword::O2W:
				ParseTransform(p.m_reader, t2s);
				break;
			case pr::ldr::EKeyword::Addr:
				p.m_reader.SectionStart();
				p.m_reader.ExtractIdentifier(word); sam.AddressU = (D3D11_TEXTURE_ADDRESS_MODE)pr::rdr::ETexAddrMode::Parse(word, false);
				p.m_reader.ExtractIdentifier(word); sam.AddressV = (D3D11_TEXTURE_ADDRESS_MODE)pr::rdr::ETexAddrMode::Parse(word, false);
				p.m_reader.SectionEnd();
				break;
			case pr::ldr::EKeyword::Filter:
				p.m_reader.SectionStart();
				p.m_reader.ExtractIdentifier(word); sam.Filter = (D3D11_FILTER)pr::rdr::EFilter::Parse(word, false);
				p.m_reader.SectionEnd();
				break;
			}
		}

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
	p.m_reader.SectionEnd();
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

// Read a line description
template <pr::ldr::ELdrObject::Enum_ LineType> void ParseLine(ParseParams& p)
{
	// Read the object attributes: name, colour, instance
	pr::ldr::ObjectAttributes attr = ParseAttributes(p.m_reader, LineType);
	pr::ldr::LdrObjectPtr obj(new pr::ldr::LdrObject(attr, p.m_parent, p.m_context_id));

	// These could be a scratch buffer to save on allocs
	pr::Array<pr::v4> point;
	pr::Array<pr::Colour32> colour;
	pr::Array<pr::uint16> index;

	// Read the description of the model
	pr::v4 p0, p1 = pr::v4Zero;
	pr::Colour32 col;
	bool linemesh = false, per_line_colour = false;
	int faceting = 50;
	p.m_reader.SectionStart();
	while (!p.m_reader.IsSectionEnd())
	{
		if (p.m_reader.IsKeyword())
		{
			pr::hash::HashValue kw = p.m_reader.NextKeywordH();
			ParseParams pp(p, obj->m_child, kw, obj.m_ptr);
			if (ParseLdrObject(pp)) continue;
			if (ParseProperties(pp, kw, obj)) continue;
			if (kw == pr::ldr::EKeyword::Coloured) { per_line_colour = true; continue; }
			if (kw == pr::ldr::EKeyword::Param)
			{
				float t[2];
				p.m_reader.ExtractRealArrayS(t, 2);
				size_t pt_count = point.size();
				if (pt_count < 2) continue;
				pr::v4 dir = point[pt_count-1] - point[pt_count-2];
				point[pt_count-1] = point[pt_count-2] + t[1] * dir;
				point[pt_count-2] = point[pt_count-2] + t[0] * dir;
				continue;
			}
			p.m_reader.ReportError(pr::script::EResult::UnknownToken);
			continue;
		}
		switch (LineType)
		{
		case pr::ldr::ELdrObject::Line:
			{
				p.m_reader.ExtractVector3(p0, 1.0f);
				p.m_reader.ExtractVector3(p1, 1.0f);
				if (per_line_colour) p.m_reader.ExtractInt(col.m_aarrggbb, 16);
				point.push_back(p0);
				point.push_back(p1);
				if (per_line_colour) { colour.push_back(col); colour.push_back(col); }
			}break;
		case pr::ldr::ELdrObject::LineD:
			{
				p.m_reader.ExtractVector3(p0, 1.0f);
				p.m_reader.ExtractVector3(p1, 1.0f);
				if (per_line_colour) p.m_reader.ExtractInt(col.m_aarrggbb, 16);
				point.push_back(p0);
				point.push_back(p0 + p1);
				if (per_line_colour) { colour.push_back(col); colour.push_back(col); }
			}break;
		case pr::ldr::ELdrObject::LineList:
			{
				if (point.empty()) { p.m_reader.ExtractVector3(p0, 1.0f); }
				else               { p0 = p1; }
				p.m_reader.ExtractVector3(p1, 1.0f);
				if (per_line_colour) p.m_reader.ExtractInt(col.m_aarrggbb, 16);
				point.push_back(p0);
				point.push_back(p1);
				if (per_line_colour) { colour.push_back(col); colour.push_back(col); }
			}break;
		case pr::ldr::ELdrObject::LineBox:
			{
				pr::v4 dim;
				p.m_reader.ExtractReal(dim.x);
				if (p.m_reader.IsKeyword() || p.m_reader.IsSectionEnd()) dim.y = dim.x; else p.m_reader.ExtractReal(dim.y);
				if (p.m_reader.IsKeyword() || p.m_reader.IsSectionEnd()) dim.z = dim.y; else p.m_reader.ExtractReal(dim.z);
				dim *= 0.5f;
				linemesh = true;
				point.push_back(pr::v4::make(-dim.x, -dim.y, -dim.z, 0));
				point.push_back(pr::v4::make( dim.x, -dim.y, -dim.z, 0));
				point.push_back(pr::v4::make( dim.x,  dim.y, -dim.z, 0));
				point.push_back(pr::v4::make(-dim.x,  dim.y, -dim.z, 0));
				point.push_back(pr::v4::make(-dim.x, -dim.y,  dim.z, 0));
				point.push_back(pr::v4::make( dim.x, -dim.y,  dim.z, 0));
				point.push_back(pr::v4::make( dim.x,  dim.y,  dim.z, 0));
				point.push_back(pr::v4::make(-dim.x,  dim.y,  dim.z, 0));
				pr::uint16 idx[] = {0,1,1,2,2,3,3,0, 4,5,5,6,6,7,7,4, 0,4,1,5,2,6,3,7};
				index.insert(index.end(), idx, idx + PR_COUNTOF(idx));
			}break;
		case pr::ldr::ELdrObject::Spline:
			{
				pr::Spline spline;
				p.m_reader.ExtractVector3(spline.x,1.0f);
				p.m_reader.ExtractVector3(spline.y,1.0f);
				p.m_reader.ExtractVector3(spline.z,1.0f);
				p.m_reader.ExtractVector3(spline.w,1.0f);
				if (per_line_colour) p.m_reader.ExtractInt(col.m_aarrggbb, 16);

				// Generate points for the spline
				pr::Array<pr::v4> raster; pr::Raster(spline, raster, 30);
				for (size_t i = 0, iend = raster.size()-1; i < iend; ++i)
				{
					point.push_back(raster[i+0]);
					point.push_back(raster[i+1]);
					if (per_line_colour) { colour.push_back(col); colour.push_back(col); }
				}
			}break;
		case pr::ldr::ELdrObject::Circle:
			{
				int axis_id;  p.m_reader.ExtractInt(axis_id, 10);
				float radius; p.m_reader.ExtractReal(radius);
				if (axis_id < 1 || axis_id > 3) { p.m_reader.ReportError(pr::script::EResult::UnknownValue, "axis_id must one of ±1, ±2, ±3"); return; }
				point.reserve(faceting * 2);
				axis_id = pr::Abs(axis_id) - 1;
				p0[(axis_id+0)%3] = 0.0f;
				p0[(axis_id+1)%3] = radius;
				p0[(axis_id+2)%3] = 0.0f;
				for (int i = 1; i != faceting+1; ++i, p0 = p1)
				{
					p1[(axis_id+0)%3] = 0.0f;
					p1[(axis_id+1)%3] = radius * pr::Cos(i * pr::maths::tau / faceting);
					p1[(axis_id+2)%3] = radius * pr::Sin(i * pr::maths::tau / faceting);
					point.push_back(p0);
					point.push_back(p1);
				}
			}break;
		case pr::ldr::ELdrObject::Ellipse:
			{
				int axis_id;   p.m_reader.ExtractInt(axis_id, 10);
				float radiusx; p.m_reader.ExtractReal(radiusx);
				float radiusy; p.m_reader.ExtractReal(radiusy);
				if (axis_id < 1 || axis_id > 3) { p.m_reader.ReportError(pr::script::EResult::UnknownValue, "axis_id must one of ±1, ±2, ±3"); return; }
				point.reserve(faceting * 2);
				axis_id = pr::Abs(axis_id) - 1;
				p0[(axis_id+0)%3] = 0.0f;
				p0[(axis_id+1)%3] = radiusx;
				p0[(axis_id+2)%3] = 0.0f;
				for (int i = 1; i != faceting+1; ++i, p0 = p1)
				{
					p1[(axis_id+0)%3] = 0.0f;
					p1[(axis_id+1)%3] = radiusx * pr::Cos(i * pr::maths::tau / faceting);
					p1[(axis_id+2)%3] = radiusy * pr::Sin(i * pr::maths::tau / faceting);
					point.push_back(p0);
					point.push_back(p1);
				}
			}break;
		case pr::ldr::ELdrObject::Matrix3x3:
			{
				pr::m4x4 basis; p.m_reader.ExtractMatrix3x3(cast_m3x3(basis));
				linemesh = true;
				pr::v4 pt[] = {{0,0,0,1}, basis.x, {0,0,0,1}, basis.y, {0,0,0,1}, basis.z};
				pr::Colour32 col[] = {pr::Colour32Red, pr::Colour32Red, pr::Colour32Green, pr::Colour32Green, pr::Colour32Blue, pr::Colour32Blue};
				pr::uint16 idx[] = {0,1,2,3,4,5};
				point.insert(point.end(), pt, pt + PR_COUNTOF(pt));
				colour.insert(colour.end(), col, col + PR_COUNTOF(col));
				index.insert(index.end(), idx, idx + PR_COUNTOF(idx));
			}break;
		}
	}
	p.m_reader.SectionEnd();

	// Ensure we have enough data to create the model
	if (point.size() < 2)
	{
		p.m_reader.ReportError("Line object description incomplete");
		return;
	}

	// Create the model
	if (linemesh) obj->m_model = pr::rdr::ModelGenerator<pr::rdr::VertPC>::Mesh (p.m_rdr, pr::rdr::EPrim::LineList, point.size(), index.size(), point.data(), index.data(), colour.size(), colour.data());
	else          obj->m_model = pr::rdr::ModelGenerator<pr::rdr::VertPC>::Lines(p.m_rdr, point.size()/2, point.data(), colour.size(), colour.data());
	obj->m_model->m_name = obj->TypeAndName();

	// Add the model and instance to the containers
	p.m_models[pr::hash::HashC(obj->m_name.c_str())] = obj->m_model;
	p.m_objects.push_back(obj);
}

// Read a description of a quad
template <pr::ldr::ELdrObject::Enum_ PlaneType> void ParsePlane(ParseParams& p)
{
	// Read the object attributes: name, colour, instance
	pr::ldr::ObjectAttributes attr = ParseAttributes(p.m_reader, PlaneType);
	pr::ldr::LdrObjectPtr obj(new pr::ldr::LdrObject(attr, p.m_parent, p.m_context_id));

	// These could be a scratch buffer to save on allocs
	pr::Array<pr::v4> point;
	pr::Array<pr::Colour32> colour;

	// Read the description of the model
	pr::rdr::Texture2DPtr texture = 0;
	bool per_vert_colour = false;
	bool create = true;
	p.m_reader.SectionStart();
	while (!p.m_reader.IsSectionEnd())
	{
		if (p.m_reader.IsKeyword())
		{
			pr::hash::HashValue kw = p.m_reader.NextKeywordH();
			ParseParams pp(p, obj->m_child, kw, obj.m_ptr);
			if (ParseLdrObject(pp)) continue;
			if (ParseProperties(pp, kw, obj)) continue;
			if (kw == pr::ldr::EKeyword::Coloured) { per_vert_colour = true; continue; }
			if (kw == pr::ldr::EKeyword::Texture)  { ParseTexture(p, texture); continue; }
			if (kw == pr::ldr::EKeyword::Video)    { ParseVideo(p, texture); continue; }
			p.m_reader.ReportError(pr::script::EResult::UnknownToken);
		}
		switch (PlaneType)
		{
		case pr::ldr::ELdrObject::Triangle:
			{
				pr::v4 pt[3]; pr::Colour32 col[3];
				create &= p.m_reader.ExtractVector3(pt[0], 1.0f) && (!per_vert_colour || p.m_reader.ExtractInt(col[0].m_aarrggbb, 16));
				create &= p.m_reader.ExtractVector3(pt[1], 1.0f) && (!per_vert_colour || p.m_reader.ExtractInt(col[1].m_aarrggbb, 16));
				create &= p.m_reader.ExtractVector3(pt[2], 1.0f) && (!per_vert_colour || p.m_reader.ExtractInt(col[2].m_aarrggbb, 16));
				point.push_back(pt[0]);
				point.push_back(pt[1]); // create a degenerate
				point.push_back(pt[1]);
				point.push_back(pt[2]);
				if (per_vert_colour)
				{
					colour.push_back(col[0]);
					colour.push_back(col[1]);
					colour.push_back(col[1]);
					colour.push_back(col[2]);
				}
			}break;
		case pr::ldr::ELdrObject::Quad:
			{
				pr::v4 pt[4]; pr::Colour32 col[4];
				create &= p.m_reader.ExtractVector3(pt[0], 1.0f) && (!per_vert_colour || p.m_reader.ExtractInt(col[0].m_aarrggbb, 16));
				create &= p.m_reader.ExtractVector3(pt[1], 1.0f) && (!per_vert_colour || p.m_reader.ExtractInt(col[1].m_aarrggbb, 16));
				create &= p.m_reader.ExtractVector3(pt[2], 1.0f) && (!per_vert_colour || p.m_reader.ExtractInt(col[2].m_aarrggbb, 16));
				create &= p.m_reader.ExtractVector3(pt[3], 1.0f) && (!per_vert_colour || p.m_reader.ExtractInt(col[3].m_aarrggbb, 16));
				point.push_back(pt[0]);
				point.push_back(pt[1]);
				point.push_back(pt[2]);
				point.push_back(pt[3]);
				if (per_vert_colour)
				{
					colour.push_back(col[0]);
					colour.push_back(col[1]);
					colour.push_back(col[2]);
					colour.push_back(col[3]);
				}
			}break;
		case pr::ldr::ELdrObject::Plane:
			{
				pr::v4 pnt,fwd; float w,h;
				create &= p.m_reader.ExtractVector3(pnt, 1.0f);
				create &= p.m_reader.ExtractVector3(fwd, 0.0f);
				create &= p.m_reader.ExtractReal(w);
				create &= p.m_reader.ExtractReal(h);

				fwd = pr::Normalise3(fwd);
				pr::v4 up = pr::Perpendicular(fwd);
				pr::v4 left = pr::Cross3(up, fwd);
				up   *= h * 0.5f;
				left *= w * 0.5f;
				point.push_back(pnt - up - left);
				point.push_back(pnt - up + left);
				point.push_back(pnt + up + left);
				point.push_back(pnt + up - left);
			}break;
		default:
			p.m_reader.ReportError(pr::script::EResult::UnknownValue);
			p.m_reader.FindSectionEnd();
			break;
		}
	}
	p.m_reader.SectionEnd();

	// Create the model
	if (!create || point.empty())
	{
		switch (PlaneType)
		{
		default: return;
		case pr::ldr::ELdrObject::Triangle: p.m_reader.ReportError("Triangle object description incomplete"); return;
		case pr::ldr::ELdrObject::Quad:     p.m_reader.ReportError("Quad object description incomplete");     return;
		case pr::ldr::ELdrObject::Plane:    p.m_reader.ReportError("Plane object description incomplete");    return;
		}
	}

	// If a texture was given, load it and create a material that uses it
	pr::rdr::DrawMethod local_mat, *mat = 0;
	if (texture)
	{
		local_mat = p.m_rdr.m_shdr_mgr.FindShaderFor<pr::rdr::VertPCNT>();
		local_mat.m_tex_diffuse = texture;
		mat = &local_mat;
		//if (texture->m_video)
		//	texture->m_video->Play(true);
	}

	// Create the model
	obj->m_model = pr::rdr::ModelGenerator<>::Quad(p.m_rdr, point.size()/4, point.data(), colour.size(), colour.data(), pr::m4x4Identity, mat);
	obj->m_model->m_name = obj->TypeAndName();

	// Add the model and instance to the containers
	p.m_models[pr::hash::HashC(obj->m_name.c_str())] = obj->m_model;
	p.m_objects.push_back(obj);
}

// Read a box description given by width, height, and depth
template <pr::ldr::ELdrObject::Enum_ BoxType> void ParseBox(ParseParams& p)
{
	// Read the object attributes: name, colour, instance
	pr::ldr::ObjectAttributes attr = ParseAttributes(p.m_reader, BoxType);
	pr::ldr::LdrObjectPtr obj(new pr::ldr::LdrObject(attr, p.m_parent, p.m_context_id));

	// Read the description of the model
	pr::Array<pr::v4, 16> position;
	pr::v4 dim = pr::v4Zero, pt[8], up = pr::v4YAxis;
	pr::m4x4 b2w = pr::m4x4Identity;
	bool create = false;
	p.m_reader.SectionStart();
	while (!p.m_reader.IsSectionEnd())
	{
		if (p.m_reader.IsKeyword())
		{
			pr::hash::HashValue kw = p.m_reader.NextKeywordH();
			ParseParams pp(p, obj->m_child, kw, obj.m_ptr);
			if (ParseLdrObject(pp)) continue;
			if (ParseProperties(pp, kw, obj)) continue;
			if (kw == pr::ldr::EKeyword::Up)      { p.m_reader.ExtractVector3S(up, 0.0f); continue; }
			p.m_reader.ReportError(pr::script::EResult::UnknownToken);
			continue;
		}
		switch (BoxType)
		{
		case pr::ldr::ELdrObject::Box:
			{
				create |= p.m_reader.ExtractReal(dim.x);
				if (p.m_reader.IsKeyword() || p.m_reader.IsSectionEnd()) dim.y = dim.x; else create |= p.m_reader.ExtractReal(dim.y);
				if (p.m_reader.IsKeyword() || p.m_reader.IsSectionEnd()) dim.z = dim.y; else create |= p.m_reader.ExtractReal(dim.z);
			}break;
		case pr::ldr::ELdrObject::BoxLine:
			{
				float w = 0.1f, h = 0.1f;
				pr::v4 s0 = pr::v4Origin, s1 = pr::v4ZAxis;
				create |= (p.m_reader.ExtractVector3(s0, 1.0f) && p.m_reader.ExtractVector3(s1, 1.0f) && p.m_reader.ExtractReal(w) && p.m_reader.ExtractReal(h));
				pr::OriFromDir(b2w, s1 - s0, 2, up, (s1 + s0) * 0.5f);
				dim.set(w, h, pr::Length3(s1 - s0), 0.0f);
			}break;
		case pr::ldr::ELdrObject::BoxList:
			{
				pr::v4 v;
				p.m_reader.ExtractVector3(v, 0.0f);
				if (dim == pr::v4Zero) dim = v;
				else { position.push_back(v.w1()); create = true; }
			}break;
		case pr::ldr::ELdrObject::FrustumWH:
			{
				int axis_id = 2; float w = 1.0f, h = 1.0f, n = 0.0f, f = 1.0f;
				create |= p.m_reader.ExtractInt(axis_id,10) && p.m_reader.ExtractReal(w) && p.m_reader.ExtractReal(h) && p.m_reader.ExtractReal(n) && p.m_reader.ExtractReal(f);
				w *= 0.5f;
				h *= 0.5f;
				pt[0].set(-n*w, -n*h, n, 1.0f);
				pt[1].set(-n*w,  n*h, n, 1.0f);
				pt[2].set( n*w, -n*h, n, 1.0f);
				pt[3].set( n*w,  n*h, n, 1.0f);
				pt[4].set( f*w, -f*h, f, 1.0f);
				pt[5].set( f*w,  f*h, f, 1.0f);
				pt[6].set(-f*w, -f*h, f, 1.0f);
				pt[7].set(-f*w,  f*h, f, 1.0f);
				switch (axis_id){
				default: p.m_reader.ReportError(pr::script::EResult::UnknownValue, "axis_id must one of ±1, ±2, ±3"); return;
				case  1: b2w = pr::Rotation4x4(0.0f ,-pr::maths::tau_by_4 ,0.0f ,pr::v4Origin); break;
				case -1: b2w = pr::Rotation4x4(0.0f , pr::maths::tau_by_4 ,0.0f ,pr::v4Origin); break;
				case  2: b2w = pr::Rotation4x4(-pr::maths::tau_by_4 ,0.0f ,0.0f ,pr::v4Origin); break;
				case -2: b2w = pr::Rotation4x4( pr::maths::tau_by_4 ,0.0f ,0.0f ,pr::v4Origin); break;
				case  3: b2w = pr::m4x4Identity; break;
				case -3: b2w = pr::Rotation4x4(0.0f ,pr::maths::tau_by_2 ,0.0f ,pr::v4Origin); break;
				}
			}break;
		case pr::ldr::ELdrObject::FrustumFA:
			{
				int axis_id = 2; float fovY = 90.0f, aspect = 1.0f, n = 0.0f, f = 1.0f;
				create |= p.m_reader.ExtractInt(axis_id,10) && p.m_reader.ExtractReal(fovY) && p.m_reader.ExtractReal(aspect) && p.m_reader.ExtractReal(n) && p.m_reader.ExtractReal(f);
				float h = pr::Tan(pr::DegreesToRadians(fovY * 0.5f));
				float w = aspect * h;
				pt[0].set(-n*w, -n*h, n, 1.0f);
				pt[1].set(-n*w,  n*h, n, 1.0f);
				pt[2].set( n*w, -n*h, n, 1.0f);
				pt[3].set( n*w,  n*h, n, 1.0f);
				pt[4].set( f*w, -f*h, f, 1.0f);
				pt[5].set( f*w,  f*h, f, 1.0f);
				pt[6].set(-f*w, -f*h, f, 1.0f);
				pt[7].set(-f*w,  f*h, f, 1.0f);
				switch (axis_id) {
				default: p.m_reader.ReportError(pr::script::EResult::UnknownValue, "axis_id must one of ±1, ±2, ±3"); return;
				case  1: b2w = pr::Rotation4x4(0.0f ,-pr::maths::tau_by_4 ,0.0f ,pr::v4Origin); break;
				case -1: b2w = pr::Rotation4x4(0.0f , pr::maths::tau_by_4 ,0.0f ,pr::v4Origin); break;
				case  2: b2w = pr::Rotation4x4(-pr::maths::tau_by_4 ,0.0f ,0.0f ,pr::v4Origin); break;
				case -2: b2w = pr::Rotation4x4( pr::maths::tau_by_4 ,0.0f ,0.0f ,pr::v4Origin); break;
				case  3: b2w = pr::m4x4Identity; break;
				case -3: b2w = pr::Rotation4x4(0.0f ,pr::maths::tau_by_2 ,0.0f ,pr::v4Origin); break;
				}
			}break;
		default:
			p.m_reader.ReportError(pr::script::EResult::UnknownValue);
			p.m_reader.FindSectionEnd();
			break;
		}
	}
	p.m_reader.SectionEnd();

	// Create the model
	if (!create)
	{
		p.m_reader.ReportError("Box object description incomplete");
		return;
	}

	// Create the model
	switch (BoxType)
	{
	default: PR_ASSERT(PR_DBG_LDROBJMGR, false, ""); return;
	case pr::ldr::ELdrObject::Box:
	case pr::ldr::ELdrObject::BoxLine:   obj->m_model = pr::rdr::ModelGenerator<>::Box(p.m_rdr, dim * 0.5f, b2w); break;
	case pr::ldr::ELdrObject::BoxList:   obj->m_model = pr::rdr::ModelGenerator<>::BoxList(p.m_rdr, position.size(), position.data(), dim * 0.5f); break;
	case pr::ldr::ELdrObject::FrustumWH:
	case pr::ldr::ELdrObject::FrustumFA: obj->m_model = pr::rdr::ModelGenerator<>::Boxes(p.m_rdr, 1, pt, b2w); break;
	}
	obj->m_model->m_name = obj->TypeAndName();

	// Add the model and instance to the containers
	p.m_models[pr::hash::HashC(obj->m_name.c_str())] = obj->m_model;
	p.m_objects.push_back(obj);
}

// Read a sphere description given by a single radius
template <pr::ldr::ELdrObject::Enum_ SphereType> void ParseSphere(ParseParams& p)
{
	// Read the object attributes: name, colour, instance
	pr::ldr::ObjectAttributes attr = ParseAttributes(p.m_reader, SphereType);
	pr::ldr::LdrObjectPtr obj(new pr::ldr::LdrObject(attr, p.m_parent, p.m_context_id));

	// Read the description of the model
	pr::v4 radius = pr::v4Zero;
	int divisions = 3;
	pr::rdr::Texture2DPtr texture = 0;
	bool create = false;
	p.m_reader.SectionStart();
	while (!p.m_reader.IsSectionEnd())
	{
		if (p.m_reader.IsKeyword())
		{
			pr::hash::HashValue kw = p.m_reader.NextKeywordH();
			ParseParams pp(p, obj->m_child, kw, obj.m_ptr);
			if (ParseLdrObject(pp)) continue;
			if (ParseProperties(pp, kw, obj)) continue;
			if (kw == pr::ldr::EKeyword::Divisions) { p.m_reader.ExtractInt(divisions, 10); continue; }
			if (kw == pr::ldr::EKeyword::Texture)   { ParseTexture(p, texture); continue; }
			p.m_reader.ReportError(pr::script::EResult::UnknownToken);
			continue;
		}
		switch (SphereType)
		{
		case pr::ldr::ELdrObject::Sphere:
			{
				float r;
				create |= p.m_reader.ExtractReal(r);
				radius.set(r, 0.0f);
			}break;
		case pr::ldr::ELdrObject::SphereRxyz:
			{
				create |= 
					p.m_reader.ExtractReal(radius.x) &&
					p.m_reader.ExtractReal(radius.y) &&
					p.m_reader.ExtractReal(radius.z);
			}break;
		}
	}
	p.m_reader.SectionEnd();

	// Create the model
	if (!create)
	{
		p.m_reader.ReportError("Sphere object description incomplete");
		return;
	}

	// If a texture was given, load it and create a material that uses it
	pr::rdr::DrawMethod local_mat, *mat = 0;
	if (texture)
	{
		local_mat = p.m_rdr.m_shdr_mgr.FindShaderFor<pr::rdr::VertPCNT>();
		local_mat.m_tex_diffuse = texture;
		mat = &local_mat;
		//if (texture->m_video)
		//	texture->m_video->Play(true);
	}

	// Create the model
	obj->m_model = pr::rdr::ModelGenerator<>::Geosphere(p.m_rdr, radius, divisions, pr::Colour32White, mat);
	obj->m_model->m_name = obj->TypeAndName();

	// Add the model and instance to the containers
	p.m_models[pr::hash::HashC(obj->m_name.c_str())] = obj->m_model;
	p.m_objects.push_back(obj);
}

// Parse a cone description given by axis id, height, radii, and scale factors
template <pr::ldr::ELdrObject::Enum_ ConeType> void ParseCone(ParseParams& p)
{
	// Read the object attributes: name, colour, instance
	pr::ldr::ObjectAttributes attr = ParseAttributes(p.m_reader, ConeType);
	pr::ldr::LdrObjectPtr obj(new pr::ldr::LdrObject(attr, p.m_parent, p.m_context_id));

	// Read the description of the model
	int axis_id = 1;
	float height = 1.0f;
	float radius[2] = {1.0f, 1.0f};
	float scale[2] = {1.0f, 1.0f};
	int layers = 1, wedges = 20;
	pr::rdr::Texture2DPtr texture = 0;
	bool create = true;
	p.m_reader.SectionStart();
	while (!p.m_reader.IsSectionEnd())
	{
		if (p.m_reader.IsKeyword())
		{
			pr::hash::HashValue kw = p.m_reader.NextKeywordH();
			ParseParams pp(p, obj->m_child, kw, obj.m_ptr);
			if (ParseLdrObject(pp)) continue;
			if (ParseProperties(pp, kw, obj)) continue;
			if (kw == pr::ldr::EKeyword::Layers)  { p.m_reader.ExtractInt(layers, 10); continue; }
			if (kw == pr::ldr::EKeyword::Wedges)  { p.m_reader.ExtractInt(wedges, 10); continue; }
			if (kw == pr::ldr::EKeyword::Scale)   { p.m_reader.ExtractRealArray(scale, 2); continue; }
			if (kw == pr::ldr::EKeyword::Texture) { ParseTexture(p, texture); continue; }
			p.m_reader.ReportError(pr::script::EResult::UnknownToken);
			continue;
		}
		switch (ConeType)
		{
		case pr::ldr::ELdrObject::CylinderHR:
			{
				create &= p.m_reader.ExtractInt(axis_id, 10);
				create &= p.m_reader.ExtractReal(height);
				create &= p.m_reader.ExtractReal(radius[0]);
				radius[1] = radius[0];
			}break;
		case pr::ldr::ELdrObject::ConeHR:
			{
				create &= p.m_reader.ExtractInt(axis_id, 10);
				create &= p.m_reader.ExtractReal(height);
				create &= p.m_reader.ExtractReal(radius[0]);
				create &= p.m_reader.ExtractReal(radius[1]);
			}break;
		case pr::ldr::ELdrObject::ConeHA:
			{
				float h0, h1, a;
				create &= p.m_reader.ExtractInt(axis_id, 10);
				create &= p.m_reader.ExtractReal(h0);
				create &= p.m_reader.ExtractReal(h1);
				create &= p.m_reader.ExtractReal(a);
				height = h1 - h0;
				radius[0] = h0 * pr::Tan(a);
				radius[1] = h1 * pr::Tan(a);
			}break;
		}
	}
	p.m_reader.SectionEnd();

	// Create the model
	if (!create)
	{
		switch (ConeType)
		{
		default: return;
		case pr::ldr::ELdrObject::CylinderHR: p.m_reader.ReportError("Cylinder object description incomplete"); return;
		case pr::ldr::ELdrObject::ConeHR:
		case pr::ldr::ELdrObject::ConeHA:     p.m_reader.ReportError("Cone object description incomplete"); return;
		}
	}

	// Get the transform so that model is aligned to 'axis_id'
	pr::m4x4 o2w = pr::m4x4Identity;
	switch (axis_id)
	{
	default: p.m_reader.ReportError(pr::script::EResult::UnknownValue, "axis_id must one of ±1, ±2, ±3"); return;
	case  1: o2w = pr::Rotation4x4(0.0f ,-pr::maths::tau_by_4 ,0.0f ,pr::v4Origin); break;
	case -1: o2w = pr::Rotation4x4(0.0f , pr::maths::tau_by_4 ,0.0f ,pr::v4Origin); break;
	case  2: o2w = pr::Rotation4x4(-pr::maths::tau_by_4 ,0.0f ,0.0f ,pr::v4Origin); break;
	case -2: o2w = pr::Rotation4x4( pr::maths::tau_by_4 ,0.0f ,0.0f ,pr::v4Origin); break;
	case  3: o2w = pr::m4x4Identity; break;
	case -3: o2w = pr::Rotation4x4(0.0f ,pr::maths::tau_by_2 ,0.0f ,pr::v4Origin); break;
	}

	// If a texture was given, load it and create a material that uses it
	pr::rdr::DrawMethod local_mat, *mat = 0;
	if (texture)
	{
		local_mat = p.m_rdr.m_shdr_mgr.FindShaderFor<pr::rdr::VertPCNT>();
		local_mat.m_tex_diffuse = texture;
		mat = &local_mat;
		//if (texture->m_video)
		//	texture->m_video->Play(true);
	}

	// Create the model
	obj->m_model = pr::rdr::ModelGenerator<>::Cylinder(p.m_rdr ,radius[0] ,radius[1] ,height ,o2w ,scale[0] ,scale[1] ,wedges ,layers ,1 ,&pr::Colour32White ,mat);
	obj->m_model->m_name = obj->TypeAndName();

	// Add the model and instance to the containers
	p.m_models[pr::hash::HashC(obj->m_name.c_str())] = obj->m_model;
	p.m_objects.push_back(obj);
}

// Parse a mesh of lines, faces, or tetrahedra
template <pr::ldr::ELdrObject::Enum_ MeshType> void ParseMesh(ParseParams& p)
{
	// Read the object attributes: name, colour, instance
	pr::ldr::ObjectAttributes attr = ParseAttributes(p.m_reader, MeshType);
	pr::ldr::LdrObjectPtr obj(new pr::ldr::LdrObject(attr, p.m_parent, p.m_context_id));

	// These could be a scratch buffer to save on allocs
	pr::Array<pr::v4>       verts;
	pr::Array<pr::v4>       normals;
	pr::Array<pr::Colour32> colours;
	pr::Array<pr::v2>       texs;
	pr::Array<pr::uint16>   indices;

	// Read the description of the model
	pr::rdr::EPrim prim_type = pr::rdr::EPrim::Invalid;
	bool generate_normals = false;
	pr::v4 v, n; pr::uint c; pr::v2 t;
	p.m_reader.SectionStart();
	while (!p.m_reader.IsSectionEnd())
	{
		if (!p.m_reader.IsKeyword())
		{
			p.m_reader.ReportError(pr::script::EResult::UnknownValue);
			p.m_reader.FindSectionEnd();
			break;
		}
		pr::ldr::EKeyword kw = p.m_reader.NextKeywordH<pr::ldr::EKeyword>();
		switch (kw)
		{
		default:
			{
				ParseParams pp(p, obj->m_child, kw, obj.m_ptr);
				if (ParseLdrObject(pp)) continue;
				if (ParseProperties(pp, kw, obj)) continue;
				p.m_reader.ReportError(pr::script::EResult::UnknownToken);
			}break;
		case pr::ldr::EKeyword::Verts:
			{
				p.m_reader.SectionStart();
				while (!p.m_reader.IsSectionEnd()) { p.m_reader.ExtractVector3(v, 1.0f); verts.push_back(v); }
				p.m_reader.SectionEnd();
			}break;
		case pr::ldr::EKeyword::Normals:
			{
				p.m_reader.SectionStart();
				while (!p.m_reader.IsSectionEnd()) { p.m_reader.ExtractVector3(n, 0.0f); normals.push_back(n); }
				p.m_reader.SectionEnd();
			}break;
		case pr::ldr::EKeyword::Colours:
			{
				p.m_reader.SectionStart();
				while (!p.m_reader.IsSectionEnd()) { p.m_reader.ExtractInt(c, 16); colours.push_back(pr::Colour32::make(c)); }
				p.m_reader.SectionEnd();
			}break;
		case pr::ldr::EKeyword::TexCoords:
			{
				p.m_reader.SectionStart();
				while (!p.m_reader.IsSectionEnd()) { p.m_reader.ExtractVector2(t); texs.push_back(t); }
				p.m_reader.SectionEnd();
			}break;
		case pr::ldr::EKeyword::Lines:
			{
				p.m_reader.SectionStart();
				while (!p.m_reader.IsSectionEnd())
				{
					pr::uint16 idx[2]; p.m_reader.ExtractIntArray(idx, 2, 10);
					indices.push_back(idx[0]);
					indices.push_back(idx[1]);
				}
				p.m_reader.SectionEnd();
				prim_type = pr::rdr::EPrim::LineList;
			}break;
		case pr::ldr::EKeyword::Faces:
			{
				p.m_reader.SectionStart();
				while (!p.m_reader.IsSectionEnd())
				{
					pr::uint16 idx[3]; p.m_reader.ExtractIntArray(idx, 3, 10);
					indices.push_back(idx[0]);
					indices.push_back(idx[1]);
					indices.push_back(idx[2]);
				}
				p.m_reader.SectionEnd();
				prim_type = pr::rdr::EPrim::TriList;
			}break;
		case pr::ldr::EKeyword::Tetra:
			{
				p.m_reader.SectionStart();
				while (!p.m_reader.IsSectionEnd())
				{
					pr::uint16 idx[4]; p.m_reader.ExtractIntArray(idx, 4, 10);
					indices.push_back(idx[0]);
					indices.push_back(idx[1]);
					indices.push_back(idx[2]);
					indices.push_back(idx[0]);
					indices.push_back(idx[2]);
					indices.push_back(idx[3]);
					indices.push_back(idx[0]);
					indices.push_back(idx[3]);
					indices.push_back(idx[1]);
					indices.push_back(idx[3]);
					indices.push_back(idx[2]);
					indices.push_back(idx[1]);
				}
				p.m_reader.SectionEnd();
				prim_type = pr::rdr::EPrim::TriList;
			}break;
		case pr::ldr::EKeyword::GenerateNormals:
			{
				generate_normals = true;
			}break;
		}
	}
	p.m_reader.SectionEnd();

	switch (MeshType)
	{
	case pr::ldr::ELdrObject::Mesh:
		if (prim_type == pr::rdr::EPrim::LineList)
		{
			generate_normals = false;
			normals.clear();
		}break;
	case pr::ldr::ELdrObject::ConvexHull:
		{
			indices.resize(6 * (verts.size() - 2));

			// Find the convex hull
			size_t num_verts = 0, num_faces = 0;
			pr::ConvexHull(verts, verts.size(), &indices[0], &indices[0]+indices.size(), num_verts, num_faces);
			verts.resize(num_verts);
			indices.resize(3*num_faces);

			prim_type = pr::rdr::EPrim::TriList;
			generate_normals = true;
		}break;
	}

	// Create the model
	if (indices.empty() || verts.empty())
	{
		switch (MeshType)
		{
		case pr::ldr::ELdrObject::Mesh:       p.m_reader.ReportError("Mesh object description incomplete");       return;
		case pr::ldr::ELdrObject::ConvexHull: p.m_reader.ReportError("ConvexHull object description incomplete"); return;
		}
	}

	// Generate normals if needed
	if (generate_normals)
	{
		normals.resize(verts.size());
		pr::geometry::GenerateNormals(indices.size(), indices.data(),
			[&](std::size_t i){ return verts[i]; },
			[&](std::size_t i){ return normals[i]; },
			[&](std::size_t i, pr::v4 const& nm){ normals[i] = nm; });
	}

	// Create the model
	obj->m_model = pr::rdr::ModelGenerator<>::Mesh(
		p.m_rdr,
		prim_type,
		verts.size(),
		indices.size(),
		verts.data(),
		indices.data(),
		colours.size(),
		colours.data(),
		normals.data(),
		texs.data());
	obj->m_model->m_name = obj->TypeAndName();

	// Add the model and instance to the containers
	p.m_models[pr::hash::HashC(obj->m_name.c_str())] = obj->m_model;
	p.m_objects.push_back(obj);
}

// Read a group description
void ParseGroup(ParseParams& p)
{
	// Read the object attributes: name, colour, instance
	pr::ldr::ObjectAttributes attr = ParseAttributes(p.m_reader, pr::ldr::ELdrObject::Group);
	pr::ldr::LdrObjectPtr obj(new pr::ldr::LdrObject(attr, p.m_parent, p.m_context_id));

	// Read the description of the model
	p.m_reader.SectionStart();
	while (!p.m_reader.IsSectionEnd())
	{
		if (p.m_reader.IsKeyword())
		{
			pr::hash::HashValue kw = p.m_reader.NextKeywordH();
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
		for (pr::ldr::ObjectCont::iterator i = obj->m_child.begin(), iend = obj->m_child.end(); i != iend; ++i)
			(*i)->Wireframe(true, true);
	}
	if (!obj->m_visible)
	{
		for (pr::ldr::ObjectCont::iterator i = obj->m_child.begin(), iend = obj->m_child.end(); i != iend; ++i)
			(*i)->Visible(false, true);
	}

	// Add the model and instance to the containers
	p.m_objects.push_back(obj);
}

// Read an instance description
void ParseInstance(ParseParams& p)
{
	// Read the object attributes: name, colour, instance. (note, instance will be ignored)
	pr::ldr::ObjectAttributes attr = ParseAttributes(p.m_reader, pr::ldr::ELdrObject::Instance);

	// Locate the model that this is an instance of
	pr::hash::HashValue model_key = pr::hash::HashC(attr.m_name.c_str());
	pr::ldr::ModelCont::iterator mdl = p.m_models.find(model_key);
	if (mdl == p.m_models.end()) { p.m_reader.ReportError(pr::script::EResult::UnknownValue); return; }

	pr::ldr::LdrObjectPtr obj(new pr::ldr::LdrObject(attr, p.m_parent, p.m_context_id));
	obj->m_model = mdl->second;

	// Parse any properties of the instance
	p.m_reader.SectionStart();
	while (!p.m_reader.IsSectionEnd())
	{
		if (p.m_reader.IsKeyword())
		{
			pr::hash::HashValue kw = p.m_reader.NextKeywordH();
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
	using namespace pr::ldr;

	std::size_t object_count = p.m_objects.size();
	switch (p.m_keyword)
	{
	default: return false;
	case ELdrObject::Line:         ParseLine<ELdrObject::Line>         (p); break;
	case ELdrObject::LineD:        ParseLine<ELdrObject::LineD>        (p); break;
	case ELdrObject::LineList:     ParseLine<ELdrObject::LineList>     (p); break;
	case ELdrObject::LineBox:      ParseLine<ELdrObject::LineBox>      (p); break;
	case ELdrObject::Spline:       ParseLine<ELdrObject::Spline>       (p); break;
	case ELdrObject::Circle:       ParseLine<ELdrObject::Circle>       (p); break;
	case ELdrObject::Ellipse:      ParseLine<ELdrObject::Ellipse>      (p); break;
	case ELdrObject::Matrix3x3:    ParseLine<ELdrObject::Matrix3x3>    (p); break;
	case ELdrObject::Triangle:     ParsePlane<ELdrObject::Triangle>    (p); break;
	case ELdrObject::Quad:         ParsePlane<ELdrObject::Quad>        (p); break;
	case ELdrObject::Plane:        ParsePlane<ELdrObject::Plane>       (p); break;
	case ELdrObject::Box:          ParseBox<ELdrObject::Box>           (p); break;
	case ELdrObject::BoxLine:      ParseBox<ELdrObject::BoxLine>       (p); break;
	case ELdrObject::BoxList:      ParseBox<ELdrObject::BoxList>       (p); break;
	case ELdrObject::FrustumWH:    ParseBox<ELdrObject::FrustumWH>     (p); break;
	case ELdrObject::FrustumFA:    ParseBox<ELdrObject::FrustumFA>     (p); break;
	case ELdrObject::Sphere:       ParseSphere<ELdrObject::Sphere>     (p); break;
	case ELdrObject::SphereRxyz:   ParseSphere<ELdrObject::SphereRxyz> (p); break;
	case ELdrObject::CylinderHR:   ParseCone<ELdrObject::CylinderHR>   (p); break;
	case ELdrObject::ConeHR:       ParseCone<ELdrObject::ConeHR>       (p); break;
	case ELdrObject::ConeHA:       ParseCone<ELdrObject::ConeHA>       (p); break;
	case ELdrObject::Mesh:         ParseMesh<ELdrObject::Mesh>         (p); break;
	case ELdrObject::ConvexHull:   ParseMesh<ELdrObject::ConvexHull>   (p); break;
	case ELdrObject::Group:        ParseGroup                          (p); break;
	case ELdrObject::Instance:     ParseInstance                       (p); break;
	}

	// Apply properties to each object added
	for (std::size_t i = object_count, iend = p.m_objects.size(); i != iend; ++i)
	{
		LdrObjectPtr& obj = p.m_objects[i];
		++p.m_obj_count;

		// Set colour on 'obj' (so that render states are set correctly)
		// 'm_base_colour' only applies to the top level object. Consider
		// a *Box with a nested *Line, if the colour applied to all children
		// and the *Box was FFFF0000, then *Line could only ever be red as well.
		obj->SetColour(obj->m_base_colour, 0xFFFFFFFF, false);

		// Apply the colour of 'obj' to all children using a mask
		if (obj->m_colour_mask != 0)
			for (ObjectCont::iterator i = obj->m_child.begin(), iend = obj->m_child.end(); i != iend; ++i)
				(*i)->SetColour(obj->m_base_colour, obj->m_colour_mask, true);

		// If flagged as wireframe, set wireframe
		if (obj->m_wireframe)
			obj->Wireframe(obj->m_wireframe, false);

		// If flagged as hidden, hide
		if (!obj->m_visible)
			obj->Visible(obj->m_visible, false);
	}

	// Give progress updates
	std::size_t now = GetTickCount();
	if (now - p.m_start_time > 200 && now - p.m_last_update > 100)
	{
		p.m_last_update = now;
		pr::events::Send(pr::ldr::Evt_LdrProgress((int)p.m_obj_count, -1, "Parsing scene", true, p.m_objects.back()));
	}

	return true;
}

// Add the ldr objects described in 'reader' to 'objects'
// Note: this is done as a background thread while a progrss dialog is displayed
void pr::ldr::Add(pr::Renderer& rdr, pr::script::Reader& reader, pr::ldr::ObjectCont& objects, pr::ldr::ContextId context_id, bool async)
{
	// Creates a collection of objects in a background thread
	struct Adder
		:pr::threads::BackgroundTask
		,pr::events::IRecv<pr::ldr::Evt_LdrProgress>
	{
		pr::Renderer*         m_rdr;
		pr::script::Reader*   m_reader;
		pr::ldr::ContextId    m_context_id;
		pr::ldr::ObjectCont*  m_objects;
		pr::ldr::ModelCont    m_models;
		std::size_t           m_total;
		pr::script::Exception m_exception;

		Adder(pr::Renderer& rdr, pr::script::Reader& reader, pr::ldr::ObjectCont& objects, pr::ldr::ContextId context_id)
			:m_rdr(&rdr)
			,m_reader(&reader)
			,m_context_id(context_id)
			,m_objects(&objects)
			,m_models()
			,m_total()
			,m_exception()
		{}
		void DoWork(void*)
		{
			// CoInitialise
			pr::InitCom init_com;

			try
			{
				DWORD now = GetTickCount();
				int initial = int(m_objects->size());
				for (pr::ldr::EKeyword kw; m_reader->NextKeywordH(kw);)
				{
					switch (kw)
					{
					default:
						{
							ParseParams pp(*m_rdr, *m_reader, *m_objects, m_models, m_context_id, kw, 0, m_total, now);
							if (ParseLdrObject(pp)) continue;
							m_reader->ReportError(pr::script::EResult::UnknownToken);
						}break;

						// Application commands
					case pr::ldr::EKeyword::Clear: break; // use event
					case pr::ldr::EKeyword::Wireframe: break;
					case pr::ldr::EKeyword::Camera: break;
					case pr::ldr::EKeyword::Lock: break;
					case pr::ldr::EKeyword::Delimiters: break;
					}
				}
				int final = int(m_objects->size());

				// Notify observers of the objects that have been added
				pr::events::Send(pr::ldr::Evt_AddBegin());
				for (int idx = 0, total = final - initial, pc = 0; idx != total; ++idx)
				{
					if (idx * 100 > pc * total)
					{
						pc = idx * 100 / total;
						ReportProgress(idx, total, pr::FmtS("Creating UI entry for object %d...", idx));
					}
					pr::events::Send(pr::ldr::Evt_LdrObjectAdd((*m_objects)[idx + initial]));
				}
				pr::events::Send(pr::ldr::Evt_AddEnd(initial, final));
			}
			catch (pr::script::Exception const& e) { m_exception = e; }
			catch (...) { m_exception.m_code = pr::script::EResult::Failed; m_exception.m_msg = "Unknown exception occurred"; }
		}
		void OnEvent(pr::ldr::Evt_LdrProgress const& e)
		{
			// Adding objects generates progress events.
			char const* type = e.m_obj ? pr::ldr::ELdrObject::ToString(e.m_obj->m_type) : "";
			std::string name = e.m_obj ? e.m_obj->m_name : "";
			ReportProgress(e.m_count, e.m_total, (e.m_total == -1) ?
				pr::FmtS("%s...\r\nObject count: %d\r\n%s %s" ,e.m_desc ,e.m_count ,type ,name.c_str()) :
				pr::FmtS("%s...\r\nObject: %d of %d\r\n%s %s" ,e.m_desc ,e.m_count ,e.m_total ,type ,name.c_str()));
		}
	};

	// Run the adding process as a background task while displaying a progress dialog
	Adder adder(rdr, reader, objects, context_id);
	if (async) { pr::gui::ProgressDlg dlg; dlg.DoModal("Processing ldr script", adder); }
	else       { adder.DoWork(0); }

	// If an exception occurred, relay the message
	if (adder.m_exception.code() != pr::script::EResult::Success)
		reader.ReportError(adder.m_exception.what());
}

// Add a custom object
pr::ldr::LdrObjectPtr pr::ldr::Add(
	pr::Renderer& rdr,
	pr::ldr::ObjectAttributes attr,
	pr::rdr::EPrim prim_type,
	int icount,
	int vcount,
	pr::uint16 const* indices,
	pr::v4 const* verts,
	int ccount,
	pr::Colour32 const* colours,
	pr::v4 const* normals,
	pr::v2 const* tex_coords,
	pr::ldr::ContextId context_id)
{
	pr::ldr::LdrObjectPtr obj(new pr::ldr::LdrObject(attr, 0, context_id));

	pr::rdr::EGeom  geom_type  = pr::rdr::EGeom::Vert;
	if (normals)    geom_type |= pr::rdr::EGeom::Norm;
	if (colours)    geom_type |= pr::rdr::EGeom::Colr;
	if (tex_coords) geom_type |= pr::rdr::EGeom::Tex0;

	// Create a tint material
	pr::rdr::DrawMethod mat = rdr.m_shdr_mgr.FindShaderFor(geom_type);
	//pr::rdr::Material mat = rdr.m_mat_mgr.GetMaterial(geom_type);

	// Create the model
	obj->m_model = pr::rdr::ModelGenerator<>::Mesh(rdr, prim_type, vcount, icount, verts, indices, ccount, colours, normals, tex_coords, &mat);
	obj->m_model->m_name = obj->TypeAndName();
	pr::events::Send(pr::ldr::Evt_LdrObjectAdd(obj));
	return obj;
}

// Add a custom object via callback
pr::ldr::LdrObjectPtr pr::ldr::Add(pr::Renderer& rdr, pr::ldr::ObjectAttributes attr, int icount, int vcount, EditObjectCB edit_cb, void* ctx, pr::ldr::ContextId context_id)
{
	pr::ldr::LdrObjectPtr obj(new pr::ldr::LdrObject(attr, 0, context_id));

	pr::rdr::MdlSettings settings(
		pr::rdr::VBufferDesc::Of<pr::rdr::VertPCNT>(vcount),
		pr::rdr::IBufferDesc::Of<pr::uint16>(icount));

	obj->m_model = rdr.m_mdl_mgr.CreateModel(settings);
	obj->m_model->m_name = obj->TypeAndName();
	edit_cb(obj->m_model, ctx, rdr);
	pr::events::Send(pr::ldr::Evt_LdrObjectAdd(obj));
	return obj;
}

// Remove all objects from 'objects' that have a context id matching one in 'doomed' and not in 'excluded'
// If 'doomed' is 0, all are assumed doomed. If 'excluded' is 0, none are assumed excluded
// 'excluded' is considered after 'doomed' so if any context ids are in both arrays, they will be excluded.
void pr::ldr::Remove(pr::ldr::ObjectCont& objects, pr::ldr::ContextId const* doomed, std::size_t dcount, pr::ldr::ContextId const* excluded, std::size_t ecount)
{
	pr::ldr::ContextId const* dend = doomed   + dcount;
	pr::ldr::ContextId const* eend = excluded + ecount;
	for (size_t i = objects.size(); i-- != 0;)
	{
		if (doomed   && std::find(doomed  , dend, objects[i]->m_context_id) == dend) continue; // not in the doomed list
		if (excluded && std::find(excluded, eend, objects[i]->m_context_id) != eend) continue; // saved by exclusion
		objects.erase(objects.begin() + i);
	}
}

// Remove 'obj' from 'objects'
void pr::ldr::Remove(pr::ldr::ObjectCont& objects, pr::ldr::LdrObjectPtr obj)
{
	for (pr::ldr::ObjectCont::const_iterator i = objects.begin(), iend = objects.end(); i != iend; ++i)
	{
		if (*i != obj) continue;
		objects.erase(i);
		break;
	}
}

// Modify the geometry of an LdrObject
void pr::ldr::Edit(pr::Renderer& rdr, LdrObjectPtr object, EditObjectCB edit_cb, void* ctx)
{
	edit_cb(object->m_model, ctx, rdr);
	pr::events::Send(pr::ldr::Evt_LdrObjectChg(object));
}

// Parse the source data in 'reader' using the same syntax
// as we use for ldr object '*o2w' transform descriptions.
// The source should begin with '{' and end with '}', i.e. *o2w { ... } with the *o2w already read
pr::m4x4 pr::ldr::ParseLdrTransform(pr::script::Reader& reader)
{
	pr::m4x4 o2w = pr::m4x4Identity;
	ParseTransform(reader, o2w);
	return o2w;
}

// Generate a scene that demos the supported object types and modifers.
std::string pr::ldr::CreateDemoScene()
{
	std::stringstream out;
	out <<
		"//********************************************\n"
		"// LineDrawer demo scene\n"
		"//  Copyright © Rylogic Ltd 2009\n"
		"//********************************************\n"
		"\n"
		"// Clear existing data\n"
		"*Clear /*{ctx_id ...}*/ // Context ids can be listed within a section\n"
		"\n"
		"// Object descriptions have the following format:\n"
		"//	*ObjectType [name] [colour] [instance]\n"
		"//	{\n"
		"//		...\n"
		"//	}\n"
		"//	The name, colour, and instance parameters are optional and have defaults of\n"
		"//		name     = 'ObjectType'\n"
		"//		colour   = FFFFFFFF\n"
		"//		instance = true\n"
		"*Box {1 2 3}\n"
		"\n"
		"// An example of applying a transform to an object.\n"
		"// All objects have an implicit object-to-parent transform that is identity.\n"
		"// Successive 'o2w' sections premultiply this transform for the object.\n"
		"// Fields within the 'o2w' section are applied in the order they are specified.\n"
		"*Box o2w_example FF00FF00\n"
		"{\n"
		"	2 3 1\n"
		"	*o2w\n"
		"	{\n"
		"		// An empty 'o2w' is equivalent to an identity transform\n"
		"		*M4x4 {1 0 0 0  0 1 0 0  0 0 1 0  0 0 0 1}  // *M4x4 {xx xy xz xw  yx yy yz yw  zx zy zz zw  wx wy wz ww} - i.e. row major\n"
		"		*M3x3 {1 0 0  0 1 0  0 0 1}                 // *M3x3 {xx xy xz  yx yy yz  zx zy zz} - i.e. row major\n"
		"		*Pos {0 1 0}                                // *Pos {x y z}\n"
		"		*Direction {0 1 0 2}                        // *Direction {dx dy dz axis} - direction vector, and axis to align to that direction\n"
		"		*Quat {0 1 0 0.3}                           // *Quat {x y z s} - quaternion\n"
		"		*Rand4x4 {0 1 0 2}                          // *Rand4x4 {cx cy cz r} - centre position, radius. Random orientation\n"
		"		*RandPos {0 1 0 2}                          // *RandPos {cx cy cz r} - centre position, radius\n"
		"		*RandOri                                    // Randomises the orientation of the current transform\n"
		"		*Scale {1 1.2 1}                            // *Scale { sx sy sz } - multiples the lengths of x,y,z vectors of the current transform\n"
		"		*Transpose                                  // Transposes the current transform\n"
		"		*Inverse                                    // Inverts the current transform\n"
		"		*Euler {45 30 60}                           // *Euler { pitch yaw roll } - all in degrees. Order of rotations is roll, pitch, yaw\n"
		"		*Normalise                                  // Normalises the lengths of the vectors of the current transform\n"
		"		*Orthonormalise                             // Normalises the lengths and makes orthogonal the vectors of the current transform\n"
		"	}\n"
		"}\n"
		"\n"
		"// There are a number of other object modifiers that can also be used\n"
		"// For example:\n"
		"*Box obj_modifier_example FFFF0000\n"
		"{\n"
		"	0.2 0.5 0.4\n"
		"	*Colour {FFFF00FF}       // Override the base colour of the model\n"
		"	*ColourMask {FF000000}   // applies: 'child.colour = (obj.base_colour & mask) | (child.base_colour & ~mask)' to all children recursively\n"
		"	*RandColour              // Apply a random colour to this object\n"
		"	*Animation               // Add simple animation to this object\n"
		"	{\n"
		"		*Style PingPong      // Animation style, one of: NoAnimation, PlayOnce, PlayReverse, PingPong, PlayContinuous\n"
		"		*Period 1.2          // The period of the animation in seconds\n"
		"		*Velocity 1 1 1      // Linear velocity in m/s\n"
		"		*AngVelocity 1 0 0   // Angular velocity in rad/s\n"
		"	}\n"
		"	*Hidden                  // Object is created in an invisible state\n"
		"	*Wireframe               // Object is created with render mode equal to wireframe\n"
		"}\n"
		"\n"
		"// An example of model instancing.\n"
		"// An instance can be created from any previously defined object. The instance will\n"
		"// share the renderable model from the object it is an instance of. Note that properties\n"
		"// of the object are not inheritted by the instance\n"
		"// The instance flag (false in this example) is used to prevent the model ever being drawn\n"
		"// It is different to the *Hidden property as that can be changed in the UI\n"
		"*Box model_instancing FF0000FF false\n"
		"{\n"
		"	3 1 2\n"
		"	*RandColour              // Note: this will not be inheritted by the instances\n"
		"}\n"
		"\n"
		"*Instance model_instancing FFFF0000 \n"
		"{\n"
		"	*o2w {*Pos {2 0 0}}\n"
		"}\n"
		"*Instance model_instancing FF0000FF\n"
		"{\n"
		"	*o2w {*Pos {1 0.2 0.5}}\n"
		"}\n"
		"\n"
		"// An example of object nesting.\n"
		"// Nested objects are given in the space of their parent so a parent transform is applied to all child\n"
		"*Box nesting_example1 80FFFF00\n"
		"{\n"
		"	0.4 0.7 0.3\n"
		"	*o2w {*pos {0 3 0} *randori}\n"
		"	*ColourMask { FF000000 }\n"
		"	*Box nested1_1 FF00FFFF\n"
		"	{\n"
		"		0.4 0.7 0.3\n"
		"		*o2w {*pos {1 0 0} *randori}\n"
		"		*Box nested1_2 FF00FFFF\n"
		"		{\n"
		"			0.4 0.7 0.3\n"
		"			*o2w {*pos {1 0 0} *randori}\n"
		"			*Box nested1_3 FF00FFFF\n"
		"			{\n"
		"				0.4 0.7 0.3\n"
		"				*o2w {*pos {1 0 0} *randori}\n"
		"			}\n"
		"		}\n"
		"	}\n"
		"}\n"
		"*Box nesting_example2 FFFFFF00\n"
		"{\n"
		"	0.4 0.7 0.3\n"
		"	*o2w {*pos {0 -3 0} *randori}\n"
		"	*Box nested2_1 FF00FFFF\n"
		"	{\n"
		"		0.4 0.7 0.3\n"
		"		*o2w {*pos {1 0 0} *randori}\n"
		"		*Box nested2_2 FF00FFFF\n"
		"		{\n"
		"			0.4 0.7 0.3\n"
		"			*o2w {*pos {1 0 0} *randori}\n"
		"			*Box nested2_3 FF00FFFF\n"
		"			{\n"
		"				0.4 0.7 0.3\n"
		"				*o2w {*pos {1 0 0} *randori}\n"
		"			}\n"
		"		}\n"
		"	}\n"
		"}\n"
		"\n"
		"// ************************************************************************************\n"
		"// Below is an example of each of the supported object types with notes on their syntax\n"
		"// ************************************************************************************\n"
		"\n"
		"// A model containing an arbitrary list of line segments\n"
		"*Line lines\n"
		"{\n"
		"	*Coloured                          // Optional. If specified means the lines have an aarrggbb colour after each one. Must occur before line data if used\n"
		"	-2  1  4  2 -3 -1 FFFF00FF         // x0 y0 z0  x1 y1 z1 Start and end points for a line\n"
		"	 1 -2  4 -1 -3 -1 FF00FFFF\n"
		"	-2  4  1  4 -3  1 FFFFFF00\n"
		"}\n"
		"\n"
		"// A model containing a list of line segments given by point and direction\n"
		"*LineD lineds FF00FF00\n"
		"{\n"
		"	//*Coloured                        // Optional. *Coloured is valid for all line types\n"
		"	0 1 0 -1 0 0                       // x y z dx dy dz - start and direction for a line\n"
		"	0 1 0 0 0 -1\n"
		"	0 1 0 1 0 0\n"
		"	0 1 0 0 0 1 *Param {0.2 0.6}       // Optional. Parametric values. Applies to the previous line\n"
		"}\n"
		"\n"
		"// A model containing a sequence of line segments given by a list of points\n"
		"*LineList linelist\n"
		"{\n"
		"	*Coloured\n"
		"	0 0 0                              // Note, colour and *param cannot be given here since there is no previous line to apply them to, doing so will cause a parse error.\n"
		"	0 0 1 FF00FF00 *Param {-0.2 0.5}   // Colour and *param apply from now on to each previous line\n"
		"	0 1 1 FF0000FF\n"
		"	1 1 1 FFFF00FF\n"
		"	1 1 0 FFFFFF00 *Param {0.5 0.9}\n"
		"	1 0 0 FF00FFFF\n"
		"}\n"
		"\n"
		"// A cuboid made from lines\n"
		"*LineBox linebox\n"
		"{\n"
		"	2 4 1                              // Width, height, depth. Accepts 1,2,or 3 dimensions. 1dim=cube, 2=rod, 3=arbitrary box\n"
		"}\n"
		"\n"
		"// A curve described by a start and end point and two control points\n"
		"*Spline spline FF00FF00\n"
		"{\n"
		"	0 0 0  0 0 1  1 0 1  1 0 0        // p0 p1 p2 p3 - note, all are positions\n"
		"	0 0 0  1 0 0  1 1 0  1 1 1        // tangents given by p1-p0, p3-p2\n"
		"}\n"
		"\n"
		"// A circle\n"
		"*Circle circle\n"
		"{\n"
		"	2 1.6                              // axis_id, radius. axis_id: ±1 = ±x, ±2 = ±y, ±3 = ±z\n"
		"	*RandColour *o2w{*RandPos{0 0 0 2}}\n"
		"}\n"
		"\n"
		"// An ellipse\n"
		"*Ellipse ellipse\n"
		"{\n"
		"	2 1.6  0.8                         // axis_id, radiusx, radiusy. axis_id: ±1 = ±x, ±2 = ±y, ±3 = ±z\n"
		"	*RandColour *o2w{*RandPos{0 0 0 2}}\n"
		"}\n"
		"\n"
		"// A matrix drawn as a set of three basis vectors (X=red, Y=green, Z=blue)\n"
		"*Matrix3x3 a2b_transform\n"
		"{\n"
		"	1 0 0      // X\n"
		"	0 1 0      // Y\n"
		"	0 0 1      // Z\n"
		"}\n"
		"\n"
		"// A list of triangles\n"
		"*Triangle triangle FFFFFFFF\n"
		"{\n"
		"	*Coloured                          // Optional. If specified means each corner of the triangle has a colour\n"
		"	-1.5 -1.5 0 FFFF0000               // Three corner points of the triangle\n"
		"	 1.5 -1.5 0 FF00FF00\n"
		"	 1.5  1.5 0 FF0000FF\n"
		"	*o2w{*randpos{0 0 0 2}}\n"
		"	*Texture {\"#checker\"}              // Optional texture\n"
		"}\n"
		"// A quad given by 4 corner points\n"
		"*Quad quad FFFFFFFF\n"
		"{\n"
		"	*Coloured                          // Optional. If specified means each corner of the quad has a colour\n"
		"	-1.5 -1.5 0 FFFF0000               // Four corner points of the quad\n"
		"	 1.5 -1.5 0 FF00FF00               // Triangles formed are: 0 1 2, 0 2 3\n"
		"	 1.5  1.5 0 FF0000FF\n"
		"	-1.5  1.5 0 FFFF00FF\n"
		"	*o2w{*randpos{0 0 0 2}}\n"
		"	*Texture                           // Optional texture\n"
		"	{\n"
		"		\"#checker\"                     // texture filepath, stock texture name (e.g. #white, #black, #checker), or texture id (e.g. #1, #3)\n"
		"		*Addr {Clamp Clamp}            // Optional addressing mode for the texture; U, V. Options: Wrap, Mirror, Clamp, Border, MirrorOnce\n"
		"		*Filter {Linear Linear Linear} // Optional filtering of the texture; mip, min, mag. Options: None, Point, Linear, Anisotropic, PyramidalQuad, GaussianQuad\n"
		"		*o2w { *scale{100 100 1} *euler{0 0 90} } // Optional 3d texture coord transform\n"
		"	}\n"
		"}\n"
		"*Plane plane FF000080\n"
		"{\n"
		"	0 -2 -2                            // x y z - centre point of the plane\n"
		"	1 1 1                              // dx dy dz - forward direction of the plane\n"
		"	0.5 0.5                            // width, height of the edges of the plane quad\n"
		"	*Texture {\"#checker\"}              // Optional texture\n"
		"}\n"
		"\n"
		"// A box given by width, height, and depth\n"
		"*Box box\n"
		"{\n"
		"	0.2 0.5 0.3                       // Width, height, depth. Accepts 1,2,or 3 dimensions. 1dim=cube, 2=rod, 3=arbitrary box\n"
		"	*RandColour *o2w{*RandPos{0 0 0 2}}\n"
		"}\n"
		"\n"
		"// A box between two points with a width and height in the other two directions\n"
		"*BoxLine boxline\n"
		"{\n"
		"	*Up {0 1 0}                       // Optional. Controls the orientation of width and height for the box (must come first if specified)\n"
		"	0 1 0  1 2 1  0.1 0.15            // x0 y0 z0  x1 y1 z1  width  height\n"
		"	*RandColour *o2w{*RandPos{0 0 0 2}}\n"
		"}\n"
		"\n"
		"// A list of boxes all with the same dimensions at the given locations\n"
		"*BoxList boxlist\n"
		"{\n"
		"	0.4 0.2 0.5                       // Box dimensions: width, height, depth.\n"
		"	-1.0 -1.0 -1.0                    // locations: x,y,z\n"
		"	-1.0  1.0 -1.0\n"
		"	 1.0 -1.0 -1.0\n"
		"	 1.0  1.0 -1.0\n"
		"	-1.0 -1.0  1.0\n"
		"	-1.0  1.0  1.0\n"
		"	 1.0 -1.0  1.0\n"
		"	 1.0  1.0  1.0\n"
		"}\n"
		"\n"
		"// A frustum given by width, height, near plane and far plane\n"
		"// Points along the z axis. Width, Height given at '1' along the z axis\n"
		"*FrustumWH frustumwh\n"
		"{\n"
		"	2 1 1 0 1.5                         // axis_id, width, height, near plane, far plane. axis_id: ±1 = ±x, ±2 = ±y, ±3 = ±z\n"
		"	*RandColour *o2w{*RandPos{0 0 0 2}}\n"
		"}\n"
		"\n"
		"// A frustum given by field of view (in Y), aspect ratio, and near and far plane distances\n"
		"*FrustumFA frustumfa\n"
		"{\n"
		"	-1 90 1 0.4 1.5                    // axis_id, fovY, aspect, near plane, far plane. axis_id: ±1 = ±x, ±2 = ±y, ±3 = ±z\n"
		"	*RandColour *o2w{*RandPos{0 0 0 2}}\n"
		"}\n"
		"\n"
		"// A sphere given by radius\n"
		"*Sphere sphere\n"
		"{\n"
		"	0.2\n"
		"	*Divisions 3                       // Optional. Controls the faceting of the sphere\n"
		"	*RandColour *o2w{*RandPos{0 0 0 2}}\n"
		"	*Texture {\"#checker\" *Addr {Wrap Wrap} *o2w {*scale{10 10 1}} }              // Optional texture\n"
		"}\n"
		"\n"
		"// A sphere given by 3 radii\n"
		"*SphereRxyz sphererxyz\n"
		"{\n"
		"	0.2 0.4 0.6                        // xradius yradius zradius\n"
		"	*RandColour *o2w{*RandPos{0 0 0 2}}\n"
		"	*Texture {\"#checker\"}             // Optional texture\n"
		"}\n"
		"\n"
		"// A cylinder given by axis number, height, and radius\n"
		"*CylinderHR cylinderhr\n"
		"{\n"
		"	2 0.6 0.2                         // axis_id, height, radius. axis_id: ±1 = ±x, ±2 = ±y, ±3 = ±z\n"
		"	*Layers 3                         // Optional. Controls the number of divisions along the cylinder major axis\n"
		"	*Wedges 50                        // Optional. Controls the faceting of the curved parts of the cylinder\n"
		"	*Scale 1.2 0.8                    // Optional. X,Y scale factors\n"
		"	*RandColour *o2w{*RandPos{0 0 0 2}}\n"
		"	*Texture {\"#checker\"}             // Optional texture\n"
		"}\n"
		"\n"
		"// A cone given by axis number, height, base radius and tip radius\n"
		"*ConeHR conehr FFFF00FF\n"
		"{\n"
		"	2 0.8 0.5 0                       // axis_id, height, base radius, tip radius. axis_id: ±1 = ±x, ±2 = ±y, ±3 = ±z\n"
		"	*Layers 3                         // Optional. Controls the number of divisions along the cone major axis\n"
		"	*Wedges 50                        // Optional. Controls the faceting of the curved parts of the cone\n"
		"	*Scale 1.5 0.4                    // Optional. X,Y scale factors\n"
		"	*RandColour *o2w{*RandPos{0 0 0 2}}\n"
		"	*Texture {\"#checker\"}             // Optional texture\n"
		"}\n"
		"\n"
		"// A cone given by axis number, two heights, and solid angle\n"
		"*ConeHA coneha FF00FFFF\n"
		"{\n"
		"	2 0 1.2 0.5                       // axis_id, tip height, base height, solid angle(rad). axis_id: ±1 = ±x, ±2 = ±y, ±3 = ±z\n"
		"	*Layers 3                         // Optional. Controls the number of divisions along the cone major axis\n"
		"	*Wedges 50                        // Optional. Controls the faceting of the curved parts of the cone\n"
		"	*Scale 1 1                        // Optional. X,Y scale factors\n"
		"	*RandColour *o2w{*RandPos{0 0 0 2}}\n"
		"	*Texture {\"#checker\"}             // Optional texture\n"
		"}\n"
		"\n"
		"// A mesh of lines, faces, or tetrahedra.\n"
		"// Syntax:\n"
		"//	*Mesh [name] [colour]\n"
		"//	{\n"
		"//		*Verts { x y z ... }\n"
		"//		[*Normals { nx ny nz ... }]                            // One per vertex\n"
		"//		[*Colours { c0 c1 c2 ... }]                            // One per vertex\n"
		"//		[*TexCoords { tx ty ... }]                             // One per vertex\n"
		"//		[GenerateNormals]                                      // Only works for faces or tetras\n"
		"//		*Faces { f00 f01 f02  f10 f11 f12  f20 f21 f22  ...}   // Indices of faces\n"
		"//		*Lines { l00 l01  l10 l11  l20 l21  l30 l31 ...}       // Indices of lines\n"
		"//		*Tetra { t00 t01 t02 t03  t10 t11 t12 t13 ...}         // Indices of tetrahedra\n"
		"//	}\n"
		"*Mesh mesh FFFFFF00\n"
		"{\n"
		"	*Verts {\n"
		"	1.087695 -2.175121 0.600000\n"
		"	1.087695  3.726199 0.600000\n"
		"	2.899199 -2.175121 0.600000\n"
		"	2.899199  3.726199 0.600000\n"
		"	1.087695  3.726199 0.721147\n"
		"	1.087695 -2.175121 0.721147\n"
		"	2.899199 -2.175121 0.721147\n"
		"	2.899199  3.726199 0.721147\n"
		"	1.087695  3.726199 0.721147\n"
		"	1.087695  3.726199 0.600000\n"
		"	1.087695 -2.175121 0.600000\n"
		"	1.087695 -2.175121 0.721147\n"
		"	2.730441  3.725990 0.721148\n"
		"	2.740741 -2.175321 0.721147\n"
		"	2.740741 -2.175321 0.600000\n"
		"	2.730441  3.725990 0.600000\n"
		"	}\n"
		"	*Faces {\n"
		"	0,1,2;,\n"
		"	3,2,1;,\n"
		"	4,5,6;,\n"
		"	6,7,4;,\n"
		"	8,9,10;,\n"
		"	8,10,11;,\n"
		"	12,13,14;,\n"
		"	14,15,12;;\n"
		"	}\n"
		"	*GenerateNormals\n"
		"}\n"
		"\n"
		"// Find the convex hull of a point cloud\n"
		"*ConvexHull convexhull FFFFFF00\n"
		"{\n"
		"	*Verts {\n"
		"	-0.998  0.127 -0.614\n"
		"	 0.618  0.170 -0.040\n"
		"	-0.300  0.792  0.646\n"
		"	 0.493 -0.652  0.718\n"
		"	 0.421  0.027 -0.392\n"
		"	-0.971 -0.818 -0.271\n"
		"	-0.706 -0.669  0.978\n"
		"	-0.109 -0.762 -0.991\n"
		"	-0.983 -0.244  0.063\n"
		"	 0.142  0.204  0.214\n"
		"	-0.668  0.326 -0.098\n"
		"	}\n"
		"	*RandColour *o2w{*RandPos{0 0 -1 2}}\n"
		"}\n"
		"\n"
		"// A group of objects\n"
		"*Group group\n"
		"{\n"
		"	*Wireframe     // Object modifiers applied to groups are applied recursively to children within the group\n"
		"	*Box b FF00FF00 { 0.4 0.1 0.2 }\n"
		"	*Sphere s FF0000FF { 0.3 *o2w{*pos{0 1 2}}}\n"
		"}\n"
		"\n"
		"#embedded(lua)\n"
		"-- lua code\n"
		"function make_box(box_number)\n"
		"	return \"*box b\"..box_number..\" FFFF0000 { 1 *o2w{*randpos {0 1 0 2}}}\\n\"\n"
		"end\n"
		"\n"
		"function make_boxes()\n"
		"	local str = \"\"\n"
		"	for i = 0,10 do\n"
		"		str = str..make_box(i)\n"
		"	end\n"
		"	return str\n"
		"end\n"
		"#end\n"
		"\n"
		"*Group luaboxes1\n"
		"{\n"
		"	*o2w {*pos {-10 0 0}}\n"
		"	#embedded(lua) return make_boxes() #end\n"
		"}\n"
		"\n"
		"*Group luaboxes2\n"
		"{\n"
		"	*o2w {*pos {10 0 0}}\n"
		"	#embedded(lua) return make_boxes() #end\n"
		"}\n"
		"\n"
		"// Ldr script syntax and features:\n"
		"//		*Keyword                    - keywords are identified by '*' characters\n"
		"//		{// Section begin           - nesting of objects within sections implies parenting\n"
		"//			// Line comment         - single line comments\n"
		"//			/* Block comment */     - block comments\n"
		"//			#eval{1+2}              - macro expression evaluation\n"
		"//		}// Section end\n"
		"//		C-style preprocessing\n"
		"//		#include \"include_file\"   - include other script files\n"
		"//		#define MACRO subst_text    - define text substitution macros\n"
		"//		MACRO                       - macro substitution\n"
		"//		#undef MACRO                - un-defining of macros\n"
		"//		#ifdef MACRO                - nestable preprocessor controlled sections\n"
		"//		#elif MACRO\n"
		"//			#ifndef MACRO\n"
		"//			#endif\n"
		"//		#else\n"
		"//		#endif\n"
		"//		#lit\n"
		"//			literal text\n"
		"//		#end\n"
		"//		#embedded(lua)\n"
		"//			--lua code\n"
		"//		#end\n"
		"\n"
		"\n"
		;
	return out.str();
}