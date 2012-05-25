//*******************************************************************************************
// LineDrawer
//	(c)opyright 2002 Rylogic Limited
//*******************************************************************************************
#include "Stdafx.h"
#if USE_NEW_PARSER
#include "pr/common/PRString.h"
#include "pr/linedrawer/customobjectdata.h"
#include "pr/geometry/Manipulator/Manipulator.h"
#include "LineDrawer/Objects/LdrObjects.h"
#include "LineDrawer/Source/LineDrawer.h"
#include "LineDrawer/GUI/DataManagerGUI.h"

using namespace pr;
using namespace rdr;

//// Return the type from it's string name
//ELdrObject GetLdrObjectType(char const* type_name)
//{
//	switch (
//	#define LDR_OBJECT(identifiertypename ) if (str::EqualNoCase(type_name, #identifier))	return ELdrObject_##identifier;
//	#include "LineDrawer/Objects/LdrObjects.inc"
//	if (str::EqualNoCase(type_name, "custom"))		return ELdrObject_Custom;
//	return ELdrObject_Unknown;
//}

// Converts a script object type enum to a string
char const* ToString(ELdrObject type)
{
	switch (type)
	{
	#define LDR_OBJECT(identifier, hash) case ELdrObject_##identifier: return #identifier;
	#include "LineDrawer/Objects/LdrObjects.inc"
	case ELdrObject_Custom: return "Custom";
	case ELdrObject_Unknown:
	default: return "Unknown";
	}
}

// Return the tint version of an effect id
inline rdr::RdrId GetTintEffectId(rdr::RdrId effect_id)
{
	switch (effect_id)
	{
	default:							return effect_id;
	case rdr::EEffect_xyz:				return rdr::EEffect_xyztint;
	case rdr::EEffect_xyzlit:			return rdr::EEffect_xyzlittint;
	case rdr::EEffect_xyzlittextured:	return rdr::EEffect_xyzlittinttextured;
	case rdr::EEffect_xyztextured:		return rdr::EEffect_xyztinttextured;
	}
}

// Constructor
LdrObject::LdrObject(LineDrawer& ldr, std::string const& name, Colour32 base_colour)
:m_name(name)
,m_base_colour(base_colour)
,m_bbox(BBoxReset)
,m_object_to_parent(m4x4Identity)
,m_parent(0)
,m_child()
,m_enabled(true)
,m_wireframe(false)
,m_instance()
,m_tree_item(0)
,m_list_item(DataManagerGUI::INVALID_LIST_ITEM)
,m_animation()
,m_user_data(0)
,m_ldr(&ldr)
{
	m_instance.m_model = 0;
	m_instance.m_base.m_num_components = NumComponents;
	SetColour(m_base_colour, false, false);
}

// Destructor
LdrObject::~LdrObject()
{
	// Release the graphics model
	if (m_instance.m_model)
		m_ldr->m_renderer->m_model_manager.DeleteModel(m_instance.m_model);

	// Delete the children
	for (TLdrObjectPtrVec::iterator c = m_child.begin(), c_end = m_child.end(); c != c_end; ++c)
		delete *c;

	m_child.clear();
}

// Return the object to world transform for this object.
// If this is a child object we need to multiply transform up to the root
m4x4 LdrObject::ObjectToWorld() const
{
	m4x4 o2w = m_object_to_parent;
	for (LdrObject const* parent = m_parent; parent; parent = parent->m_parent)
		o2w = parent->m_object_to_parent * o2w;
	return o2w;
}

// Return the AABB for this object in it's parent space. If this is a top
// level object then this will be world space.
BoundingBox LdrObject::BBox(bool include_children) const
{
	BoundingBox bbox = BBoxReset;
	if (m_instance.m_model) Encompase(bbox, m_object_to_parent * m_bbox);
	if (!include_children) return bbox;

	for (TLdrObjectPtrVec::const_iterator i = m_child.begin(), i_end = m_child.end(); i != i_end; ++i)
	{
		BoundingBox child_bbox = (*i)->BBox(include_children);
		if( child_bbox == BBoxReset ) continue;
		Encompase(bbox, m_object_to_parent * child_bbox);
	}
	return bbox;
}

// Return the world space AABB for this object
BoundingBox LdrObject::WorldSpaceBBox(bool include_children) const
{
	BoundingBox bbox = BBox(include_children);
	for (LdrObject const* parent = m_parent; parent; parent = parent->m_parent)
		bbox = parent->m_object_to_parent * bbox;
	return bbox;
}

// Render the object
void LdrObject::Render(rdr::Viewport& viewport, m4x4 const& parent_object_to_world)
{
	if( m_animation.m_style != AnimationData::NoAnimation && m_ldr->m_animation_control.IsAnimationOn() )
	{
		// Set a transform based on the animation of this object
		m4x4 animation_offset;
		float anim_time = m_ldr->m_animation_control.GetAnimationTime();
				
		float t = 0.0f;
		switch (m_animation.m_style)
		{
		case AnimationData::PlayOnce:		t = (anim_time < m_animation.m_period) ? (anim_time) : (m_animation.m_period); break;
		case AnimationData::PlayReverse:	t = (anim_time < m_animation.m_period) ? (m_animation.m_period - anim_time) : (0.0f); break;
		case AnimationData::PingPong:
			if( Fmod(anim_time, 2.0f * m_animation.m_period) >= m_animation.m_period )
				t = m_animation.m_period - Fmod(anim_time, m_animation.m_period);
			else
				t = Fmod(anim_time, m_animation.m_period);
			break;
		case AnimationData::PlayContinuous:	t = anim_time; break;
		default: PR_ASSERT_STR(PR_DBG_LDR, false, "Unknown animation style");
		}

		Rotation4x4(animation_offset, m_animation.m_rotation_axis, m_animation.m_angular_speed * t);
		animation_offset[3]	= m_animation.m_velocity * t;
		animation_offset[3].w = 1.0f;

		m_instance.m_instance_to_world = parent_object_to_world * m_object_to_parent * animation_offset;
	}
	else
	{
		m_instance.m_instance_to_world	= parent_object_to_world * m_object_to_parent;
	}

	if( m_enabled && m_instance.m_model )
	{
		//if( IsWithin(viewport.GetFrustum(), m_bbox.GetBoundingSphere()) )
		{
			viewport.AddInstance(m_instance.m_base);
		}
	}

	// Render the children
	for (TLdrObjectPtrVec::iterator i = m_child.begin(), i_end = m_child.end(); i != i_end; ++i)
	{
		(*i)->Render(viewport, m_instance.m_instance_to_world);
	}
}

// Enable cycling through child objects. Only applies to groups.
void LdrObject::Cycle(bool on)
{
	for (TLdrObjectPtrVec::iterator i = m_child.begin(), i_end = m_child.end(); i != i_end; ++i)
		(*i)->Cycle(on);
}

// Enable/Disable this object
void LdrObject::SetEnable(bool enabled, bool recursive)
{
	m_enabled = enabled;
	if( !recursive ) return;
	for (TLdrObjectPtrVec::iterator i = m_child.begin(), i_end = m_child.end(); i != i_end; ++i)
		(*i)->SetEnable(enabled, recursive);
}

// Set/Clear wireframe rendering mode for this object
void LdrObject::SetWireframe(bool wireframe, bool recursive)
{
	m_wireframe = wireframe;
	if (m_wireframe)	m_instance.m_render_state.SetRenderState  (D3DRS_FILLMODE, D3DFILL_WIREFRAME);
	else				m_instance.m_render_state.ClearRenderState(D3DRS_FILLMODE);
	if (!recursive) return;
	for (TLdrObjectPtrVec::iterator i = m_child.begin(), i_end = m_child.end(); i != i_end; ++i)
		(*i)->SetWireframe(wireframe, recursive);
}

// Set the tint colour of this object
void LdrObject::SetColour(Colour32 colour, bool recursive, bool mask)
{
	if (mask) 	m_instance.m_colour.m_aarrggbb &= colour.m_aarrggbb;
	else		m_instance.m_colour				= colour;
	
	bool has_alpha = m_instance.m_colour.a() != 0xFF;
	if (has_alpha) m_instance.m_sk_override.Set(1 << ESort_AlphaOfs, 1 << ESort_AlphaOfs);
	rdr::SetAlphaRenderStates(m_instance.m_render_state, has_alpha);

	if( !recursive ) return;
	for (TLdrObjectPtrVec::iterator i = m_child.begin(), i_end = m_child.end(); i != i_end; ++i)
		(*i)->SetColour(colour, recursive, mask);
}

// Set/Clear 50% alpha mode
void LdrObject::SetAlpha(bool on, bool recursive)
{
	m_instance.m_colour.a() = on ? 0x80 : m_base_colour.a();
	SetColour(m_instance.m_colour, false, false);
	if( !recursive ) return;
	for (TLdrObjectPtrVec::iterator i = m_child.begin(), i_end = m_child.end(); i != i_end; ++i)
		(*i)->SetAlpha(on, recursive);
}

// Return access to the model manager
ModelManager& LdrObject::ModelMgr() const
{
	return m_ldr->m_renderer->m_model_manager;
}

// Return access to the material manager
MaterialManager& LdrObject::MatMgr() const
{
	return m_ldr->m_renderer->m_material_manager;
}

//**********************************
// TGroup
void TGroup::CreateRenderObject()
{
	if( m_child.empty() )	m_bbox.Reset();
	else					m_bbox = BBox(true);
}

// Turn on/off cyclicing through child objects
void TGroup::Cyclic(bool on)
{
	m_cycle = on;
	if (m_cycle) { m_start_time = GetTickCount(); }
	LdrObject::Cycle(on);
}

// Render the group
void TGroup::Render(rdr::Viewport& viewport, m4x4 const& parent_object_to_world)
{
	if (!m_enabled || m_child.empty()) return;
	if (!m_cycle) return LdrObject::Render(viewport, parent_object_to_world);
	
	std::size_t num_children = m_child.size();
	std::size_t frame = (GetTickCount() - m_start_time) / m_ms_per_frame;
	switch (m_mode)
	{
	case EMode_StartEnd: frame %= num_children; break;
	case EMode_EndStart: frame = num_children - 1 - (frame % num_children); break;
	case EMode_PingPong:
		if( (frame % (2*num_children)) < num_children )	frame %= num_children;
		else											frame  = num_children - 1 - (frame % num_children);
		break;
	};
	m_child[frame]->Render(viewport, parent_object_to_world);
}

//**********************************
// TPoints
void TPoints::CreateRenderObject(TVecCont const& points)
{
	uint num_vertices = (uint)points.size();

	rdr::model::Settings settings;
	settings.m_vertex_type		= rdr::vf::GetTypeFromGeomType(geometry::EType_Vertex);
	settings.m_Vcount			= num_vertices;
	settings.m_Icount			= 1;
	if (Failed(ModelMgr().CreateModel(settings, m_instance.m_model)))
		throw ELdrObjectException_FailedToCreateRdrModel;

	model::VLock vlock;
	model::ILock ilock;
	
	rdr::vf::iterator vb = m_instance.m_model->LockVBuffer(vlock);
	TVecCont::const_iterator pt_iter = points.begin();
	for (uint i = 0; i != num_vertices; ++i, ++pt_iter)
	{
		vb->set(*pt_iter), ++vb;
		Encompase(m_bbox, *pt_iter);
	}
	rdr::Index* ib = m_instance.m_model->LockIBuffer(ilock);
	*ib	= static_cast<rdr::Index>(0);
	if (m_bbox.Volume() == 0.0f)
	{
		float largest = 0.0f;
		largest = Maximum<float>(m_bbox.SizeX(), largest);
		largest = Maximum<float>(m_bbox.SizeY(), largest);
		largest = Maximum<float>(m_bbox.SizeZ(), largest);
		if (largest == 0.0f)	m_bbox.m_radius.Set(0.5f, 0.5f, 0.5f, 0.0f);
		else					m_bbox.m_radius.Set(largest, largest, largest, 0.0f);
	}

	rdr::Material mat = MatMgr().GetDefaultMaterial(geometry::EType_Vertex);
	mat.m_effect = MatMgr().GetEffect(EEffect_xyztint);
	m_instance.m_model->SetMaterial(mat, rdr::model::EPrimitiveType_PointList);
	m_instance.m_model->SetName(m_name.c_str());
}

//**********************************
// TLines
void TLines::CreateRenderObject(TVecCont const& points, TColour32Cont const& colours)
{
	PR_ASSERT(PR_DBG_LDR, points.size() % 2 == 0);

	// Create a tint material
	rdr::Material mat = MatMgr().GetDefaultMaterial(geometry::EType_Vertex);
	mat.m_effect = MatMgr().GetEffect(EEffect_xyztint);

	// Create a model containing an array of lines
	v4 const* pts = points.empty() ? 0 : &points[0];
	Colour32 const* cols = colours.empty() ? 0 : &colours[0];
	m_instance.m_model = pr::rdr::model::Line(*m_ldr->m_renderer, pts, points.size()/2, cols, colours.size(), &mat);
	if (!m_instance.m_model) throw ELdrObjectException_FailedToCreateRdrModel;

	// Give it a name
	m_instance.m_model->SetName(m_name.c_str());

	// Find the bounding box
	for (TVecCont::const_iterator i = points.begin(), i_end = points.end(); i != i_end; ++i)
		Encompase(m_bbox, *i);
}

//**********************************
// TTriangles
void TTriangles::CreateRenderObject(pr::TVertexCont const& verts, pr::GeomType geom_type, std::string const& texture)
{
	PR_ASSERT(PR_DBG_LDR, (verts.size() % 3) == 0);
	uint num_tris = (uint)verts.size() / 3;
	uint num_vertices = num_tris * 3;
	uint num_indices = num_tris * 3;
	GeomType vert_type = geom_type | geometry::EType_Normal;

	rdr::model::Settings settings;
	settings.m_vertex_type		= rdr::vf::GetTypeFromGeomType(vert_type);
	settings.m_Vcount			= num_vertices;
	settings.m_Icount			= num_indices;
	if (Failed(ModelMgr().CreateModel(settings, m_instance.m_model)))
		throw ELdrObjectException_FailedToCreateRdrModel;

	model::VLock vlock;
	rdr::vf::iterator vb = m_instance.m_model->LockVBuffer(vlock);
	for( uint t = 0; t < num_tris; ++t )
	{
		Vertex const* vert = &verts[t * 3];
		for (int i = 0; i != 3; ++i)
		{
			v4			pt		= vert[i].m_vertex;
			v4			norm	= (geom_type & geometry::EType_Normal ) != 0 ? vert[i].m_normal : Cross3(vert[1].m_vertex - vert[0].m_vertex, vert[2].m_vertex - vert[1].m_vertex).Normalise3IfNonZero();
			Colour32	col		= (geom_type & geometry::EType_Colour ) != 0 ? vert[i].m_colour : Colour32One;
			v2			tex		= (geom_type & geometry::EType_Texture) != 0 ? vert[i].m_tex_vertex : v2::make(0.0f, 0.0f);
			vb->set(pt, norm, col, tex), ++vb;
			Encompase(m_bbox, pt);
		}
	}

	model::ILock ilock;
	rdr::Index* ib = m_instance.m_model->LockIBuffer(ilock);
	rdr::Index index = 0;
	PR_EXPAND(PR_DBG_LDR, rdr::Index* ibstart = ib;)
	for( uint r = 0; r < num_tris; ++r )
	{
		*ib = index + 0;											++ib;
		*ib = index + 1;											++ib;
		*ib = index + 2;											++ib;
		index += 3;
	}
	PR_ASSERT(PR_DBG_LDR, (uint)(ib - ibstart) == num_indices);

	RdrId effect_id = GetTintEffectId(rdr::GetDefaultEffectId(vert_type));

	rdr::Material mat = MatMgr().GetDefaultMaterial(vert_type);
	mat.m_effect = MatMgr().GetEffect(effect_id);
    if (!texture.empty()) MatMgr().LoadTexture(texture.c_str(), mat.m_diffuse_texture);
	m_instance.m_model->SetMaterial(mat, rdr::model::EPrimitiveType_TriangleList);
	m_instance.m_model->SetName(m_name.c_str());
}

//**********************************
// TBox methods
void TBoxes::CreateRenderObject(TVecCont const& points)
{
	using namespace rdr;
	using namespace rdr::model;
	PR_ASSERT(PR_DBG_LDR, points.size() % 8 == 0);

	// Create a tint material
	rdr::Material mat = MatMgr().GetDefaultMaterial(geometry::EType_Vertex | geometry::EType_Normal);
	mat.m_effect = MatMgr().GetEffect(EEffect_xyzlittint);

	// Create a model containing an array of boxes
	m_instance.m_model = Box(*m_ldr->m_renderer, &points[0], points.size()/8, Colour32White, &mat);
	if (!m_instance.m_model) throw ELdrObjectException_FailedToCreateRdrModel;

	// Give it a name
	m_instance.m_model->SetName(m_name.c_str());

	// Find the bounding box
	for (TVecCont::const_iterator i = points.begin(), i_end = points.end(); i != i_end; ++i)
		Encompase(m_bbox, *i);
}

//**********************************
// TCylinder
void TCylinder::CreateRenderObject(float height, float radiusX, float radiusZ, uint wedges, uint layers)
{
	PR_ASSERT(PR_DBG_LDR, layers >= 1);
	PR_ASSERT(PR_DBG_LDR, wedges >= 3);

	// Create a tint material
	rdr::Material mat = MatMgr().GetDefaultMaterial(geometry::EType_Vertex | geometry::EType_Normal);
	mat.m_effect = MatMgr().GetEffect(EEffect_xyzlittint);

	// Create a model containing a cylinder
	m_instance.m_model = pr::rdr::model::CylinderHRxRz(*m_ldr->m_renderer, height, radiusX, radiusZ, m4x4Identity, layers, wedges, Colour32White, &mat);
	if (!m_instance.m_model) throw ELdrObjectException_FailedToCreateRdrModel;

	// Give it a name
	m_instance.m_model->SetName(m_name.c_str());

	// Update the bounding box
	Encompase(m_bbox, v4::make(-radiusX, -height, -radiusZ, 0.0f));
	Encompase(m_bbox, v4::make( radiusX,  height,  radiusZ, 0.0f));
}

//**********************************
// TSphere
void TSphere::CreateRenderObject(float radiusX, float radiusY, float radiusZ, uint divisions, std::string const& texture)
{
	// Create a tint material
	GeomType geom_type = geometry::EType_Vertex | geometry::EType_Normal;
	EEffect effect_type = EEffect_xyzlittint;
	if (!texture.empty()) { geom_type |= geometry::EType_Texture; effect_type = EEffect_xyzlittinttextured; }
	rdr::Material mat = MatMgr().GetDefaultMaterial(effect_type);
	if (!texture.empty()) MatMgr().LoadTexture(texture.c_str(), mat.m_diffuse_texture);
	
	// Create a model containing a sphere
	m_instance.m_model = pr::rdr::model::SphereRxRyRz(*m_ldr->m_renderer, radiusX, radiusY, radiusZ, v4Origin, divisions, Colour32White, &mat);
	if (!m_instance.m_model) throw ELdrObjectException_FailedToCreateRdrModel;

	// Give it a name
	m_instance.m_model->SetName(m_name.c_str());

	// Update the bounding box
	Encompase(m_bbox, v4::make(-radiusX, -radiusY, -radiusZ, 0.0f));
	Encompase(m_bbox, v4::make( radiusX,  radiusY,  radiusZ, 0.0f));
}

//**********************************
// TMesh
void TMesh::CreateRenderObject(TVertexCont const& verts, TIndexCont const& indices, pr::GeomType geom_type, bool generate_normals, bool line_list)
{
	uint num_indices	= (uint)indices.size();
	uint num_vertices	= (uint)verts.size();

	rdr::model::Settings settings;
	settings.m_vertex_type		= rdr::vf::GetTypeFromGeomType(geom_type);
	settings.m_Vcount			= num_vertices;
	settings.m_Icount			= num_indices;
	if (Failed(ModelMgr().CreateModel(settings, m_instance.m_model)))
		throw ELdrObjectException_FailedToCreateRdrModel;

	model::MLock mlock(m_instance.m_model);
	rdr::vf::iterator vb = mlock.m_vlock.m_ptr;
	for (TVertexCont::const_iterator v = verts.begin(), v_end = verts.end(); v != v_end; ++v, ++vb)
	{
		vb->set(*v);
		Encompase(m_bbox, v->m_vertex);	
	}

	rdr::Index* ib = mlock.m_ilock.m_ptr;
	for (TIndexCont::const_iterator i = indices.begin(), i_end = indices.end(); i != i_end; ++i, ++ib)
	{
		*ib = *i;
	}

	if (generate_normals && !line_list)
		model::GenerateNormals(mlock);

	rdr::Material mat = MatMgr().GetDefaultMaterial(geom_type);
	mat.m_effect = !line_list ? MatMgr().GetEffect(EEffect_xyzlittint) : MatMgr().GetEffect(EEffect_xyztint);
	m_instance.m_model->SetMaterial(mat, !line_list ? rdr::model::EPrimitiveType_TriangleList : rdr::model::EPrimitiveType_LineList);
	m_instance.m_model->SetName(m_name.c_str());
}
void TMesh::CreateRenderObject(pr::Mesh const& mesh)
{
	// Load the model
	if (Failed(rdr::LoadMesh(*m_ldr->m_renderer, mesh, m_instance.m_model)))
		throw ELdrObjectException_FailedToCreateRdrModel;

	// Give it a name
	m_instance.m_model->SetName(m_name.c_str());

	// Update the bounding box
	m_bbox = pr::geometry::GetBoundingBox(mesh);

	// Loop through the nuggets for the model setting the effect to the tint version
	// This is not a renderer function because the renderer does not know about specific effects.
	Effect*      lit_effect			= MatMgr().GetEffect(EEffect_xyzlit);
	Effect*      lit_pvc_effect		= MatMgr().GetEffect(EEffect_xyzlitpvc);
	Effect* tint_lit_effect			= MatMgr().GetEffect(EEffect_xyzlittint);
	Effect*      tex_effect			= MatMgr().GetEffect(EEffect_xyzlittextured);
	Effect* tint_tex_effect			= MatMgr().GetEffect(EEffect_xyzlittinttextured);
	Effect*      lit_pvc_tex_effect = MatMgr().GetEffect(EEffect_xyzlitpvctextured);
	Effect* tint_lit_pvc_tex_effect = MatMgr().GetEffect(EEffect_xyzlitpvctinttextured);
	for (TNuggetChain::iterator n = m_instance.m_model->m_render_nugget.begin(), n_end = m_instance.m_model->m_render_nugget.end(); n != n_end; ++n)
	{
			 if( n->m_material.m_effect == lit_effect			) { n->m_material.m_effect = tint_lit_effect; }
		else if( n->m_material.m_effect == lit_pvc_effect		) { n->m_material.m_effect = tint_lit_effect; }
		else if( n->m_material.m_effect == tex_effect			) { n->m_material.m_effect = tint_tex_effect; }
		else if( n->m_material.m_effect == lit_pvc_tex_effect	) { n->m_material.m_effect = tint_lit_pvc_tex_effect; }
		else { PR_ASSERT_STR(PR_DBG_LDR, false, "Unknown effect being used"); }
	}
}

//**********************************
// TCustom
// A custom object with model data modified via call back function
// This type can only be created via the plug in interface
TCustom::TCustom(LineDrawer& ldr, pr::ldr::CustomObjectData const& data)
:LdrObject(ldr, data.m_name, data.m_colour)
{
	rdr::model::Settings settings;
	settings.m_vertex_type		= rdr::vf::GetTypeFromGeomType(data.m_geom_type);
	settings.m_Vcount			= data.m_num_verts;
	settings.m_Icount			= data.m_num_indices;
	if (Failed(ModelMgr().CreateModel(settings, m_instance.m_model)))
		throw ELdrObjectException_FailedToCreateRdrModel;

	m_instance.m_model->SetName(data.m_name.c_str());
	data.m_create_func(m_instance.m_model, m_bbox, data.m_user_data, MatMgr());
}

#endif//USE_NEW_PARSER