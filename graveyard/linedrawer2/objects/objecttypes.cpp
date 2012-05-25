//*******************************************************************************************
//
//	A base class for all the things that can be drawn
//
//*******************************************************************************************
#include "Stdafx.h"
#if USE_OLD_PARSER
#include "pr/maths/ConvexHull.h"
#include "pr/geometry/geosphere.h"
#include "pr/geometry/mesh_tools.h"
#include "pr/geometry/optimise_mesh.h"
#include "LineDrawer/Objects/ObjectTypes.h"
#include "LineDrawer/Source/LineDrawer.h"

// Helper function for access to the material manager
inline rdr::MaterialManager& RdrMaterialManager()	{ return LineDrawer::Get().m_renderer->m_material_manager; }

//// Return the tint version of an effect id
//inline rdr::RdrId GetTintEffectId(rdr::RdrId effect_id)
//{
//	switch( effect_id )
//	{
//	case rdr::EEffect_xyz:				return rdr::EEffect_xyztint;
//	case rdr::EEffect_xyzlit:			return rdr::EEffect_xyzlittint;
//	case rdr::EEffect_xyzlittextured:	return rdr::EEffect_xyzlittinttextured;
//	case rdr::EEffect_xyztextured:		return rdr::EEffect_xyztinttextured;
//	}
//	return effect_id;
//}

using namespace rdr;

//*****
// Construction
LdrObject::LdrObject(eType sub_type, const std::string& name, Colour32 colour, const std::string& source)
:m_point			()
,m_instance			()
,m_sub_type			(sub_type)
,m_name				(name)
,m_base_colour		(colour)
,m_bbox				(BBoxReset)
,m_animation		()
,m_object_to_parent	(m4x4Identity)
,m_parent			(0)
,m_child			()
,m_enabled			(true)
,m_wireframe		(false)
,m_source_string	("")
,m_tree_item		(0)
,m_list_item		(DataManagerGUI::INVALID_LIST_ITEM)
,m_user_data		(0)
{
	m_instance.m_model = 0;
	m_instance.m_base.m_cpt_count = NumComponents;

	SetColour(m_base_colour, false, false);
	m_source_string  = "*";
	m_source_string += GetLDObjectTypeString(sub_type);
	m_source_string += " ";
	m_source_string += m_name;
	m_source_string += " ";
	m_source_string += Fmt("%8.8X", m_base_colour);
	m_source_string += "\n{ ";
	m_source_string += source;
	m_source_string += " }\n";
	str::Replace(m_source_string, "\n", "\r\n");
}

//*****
// Destruction
LdrObject::~LdrObject()
{
	if( m_instance.m_model ) { LineDrawer::Get().DeleteModel(m_instance.m_model); }

	// Delete the children
	for( TLdrObjectPtrVec::iterator c = m_child.begin(), c_end = m_child.end(); c != c_end; ++c )
	{
		delete *c;
	}
	m_child.clear();
}

//*****
// Turn on animations
bool LdrObject::SetCyclic(bool start)
{
	bool cycling_on = false;
	for( TLdrObjectPtrVec::iterator i = m_child.begin(), i_end = m_child.end(); i != i_end; ++i )
	{
		cycling_on |= (*i)->SetCyclic(start);
	}
	return cycling_on;
}

// Set the enabled state of this object 
void LdrObject::SetEnable(bool enabled, bool recursive)
{
	m_enabled = enabled;
	if( !recursive ) return;
	for( TLdrObjectPtrVec::iterator i = m_child.begin(), i_end = m_child.end(); i != i_end; ++i )
	{
		(*i)->SetEnable(enabled, recursive);
	}
}

// Set the wireframe state of this object
void LdrObject::SetWireframe(bool wireframe, bool recursive)
{
	m_wireframe = wireframe;
	if( m_wireframe )	m_instance.m_render_state.SetRenderState  (D3DRS_FILLMODE, D3DFILL_WIREFRAME);
	else				m_instance.m_render_state.ClearRenderState(D3DRS_FILLMODE);
	if( !recursive ) return;
	for( TLdrObjectPtrVec::iterator i = m_child.begin(), i_end = m_child.end(); i != i_end; ++i )
	{
		(*i)->SetWireframe(wireframe, recursive);
	}
}

// Toggle the alpha for this object
void LdrObject::SetAlpha(bool on, bool recursive)
{
	if( on )
	{
		m_instance.m_colour.a() = 0x80;
		SetColour(m_instance.m_colour, false, false);
	}
	else
	{
		m_instance.m_colour.a() = m_base_colour.a();
		SetColour(m_instance.m_colour, false, false);
	}
	if( !recursive ) return;
	for( TLdrObjectPtrVec::iterator i = m_child.begin(), i_end = m_child.end(); i != i_end; ++i )
	{
		(*i)->SetAlpha(on, recursive);
	}
}

// Set the colour of this object
void LdrObject::SetColour(Colour32 colour, bool recursive, bool mask)
{
	if( mask ) 	m_instance.m_colour.m_aarrggbb &= colour.m_aarrggbb;
	else		m_instance.m_colour				= colour;
	if( m_instance.m_colour.a() != 0xFF )
	{
        m_instance.m_sk_override.Set(1 << ESort_AlphaOfs, 1 << ESort_AlphaOfs);
		rdr::SetAlphaRenderStates(m_instance.m_render_state, true);
	}
	else
	{
		rdr::SetAlphaRenderStates(m_instance.m_render_state, false);
	}
	if( !recursive ) return;
	for( TLdrObjectPtrVec::iterator i = m_child.begin(), i_end = m_child.end(); i != i_end; ++i )
	{
		(*i)->SetColour(colour, recursive, mask);
	}
}

//*****
// Set a transform based on the animation of this object
void LdrObject::SetAnimationOffset(m4x4& animation_offset)
{
	PR_ASSERT(PR_DBG_LDR, m_animation.m_style != AnimationData::NoAnimation);

	float anim_time = LineDrawer::Get().m_animation_control.GetAnimationTime();
	
	float t = 0.0f;
	switch( m_animation.m_style )
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

	Rotation4x4(animation_offset, m_animation.m_rotation_axis, m_animation.m_angular_speed * t, v4Origin);
	animation_offset[3]	= m_animation.m_velocity * t;
	animation_offset[3].w = 1.0f;
}

//*****
// Render the object
void LdrObject::Render(rdr::Viewport& viewport, const m4x4& parent_object_to_world)
{
	if( m_animation.m_style != AnimationData::NoAnimation && LineDrawer::Get().m_animation_control.IsAnimationOn() )
	{
		m4x4 animation_offset;
		SetAnimationOffset(animation_offset);
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
	for( TLdrObjectPtrVec::iterator i = m_child.begin(), i_end = m_child.end(); i != i_end; ++i )
	{
		(*i)->Render(viewport, m_instance.m_instance_to_world);
	}
}

//*****
// Get a bounding box for this object and it's children
BoundingBox LdrObject::BBox(bool including_children) const
{
	BoundingBox bbox; bbox.reset();
	if( including_children )
	{
		for( TLdrObjectPtrVec::const_iterator i = m_child.begin(), i_end = m_child.end(); i != i_end; ++i )
		{
			BoundingBox child_bbox = (*i)->BBox(including_children);
			if( child_bbox != BBoxReset )
			{
				Encompase(bbox, m_object_to_parent * child_bbox);
			}
		}
	}
	
	if( m_instance.m_model )
	{
		Encompase(bbox, m_object_to_parent * m_bbox);
	}

	return bbox;
}

//*****
// Get a bounding box for this object and it's children in world space
BoundingBox LdrObject::WorldSpaceBBox(bool including_children) const
{
	BoundingBox bbox = BBox(including_children);

	// Transform it into world space
	LdrObject* parent = m_parent;
	while( parent )
	{
		bbox = parent->m_object_to_parent * bbox;
		parent = parent->m_parent;
	}

	return bbox;
}

//*****
// Return the object to world transform for this object
m4x4 LdrObject::ObjectToWorld() const
{
	m4x4 o2w = m_object_to_parent;

	// Transform it into world space
	LdrObject* parent = m_parent;
	while( parent )
	{
		o2w = parent->m_object_to_parent * o2w;
		parent = parent->m_parent;
	}

	return o2w;
}

//**********************************
// TPoint methods
void TPoint::CreateRenderObject()
{
	uint num_vertices = (uint)m_point.size();

	rdr::model::Settings settings;
	settings.m_vertex_type		= rdr::vf::GetTypeFromGeomType(geometry::EType_Vertex);
	settings.m_Vcount			= num_vertices;
	settings.m_Icount			= 1;
	if( Failed(LineDrawer::Get().CreateModel(settings, m_instance.m_model)) ) { return; }

	model::VLock vlock;
	model::ILock ilock;
	
	rdr::vf::iterator	vb = m_instance.m_model->LockVBuffer(vlock);
	TPointVec::const_iterator pt_iter = m_point.begin();
	for( uint i = 0; i < num_vertices; ++i, ++pt_iter )
	{
		vb->set(*pt_iter), ++vb;
		Encompase(m_bbox, *pt_iter);
	}
	rdr::Index* ib = m_instance.m_model->LockIBuffer(ilock);
	*ib	= static_cast<rdr::Index>(0);
	if( Volume(m_bbox) == 0.0f )
	{
		float largest = 0.0f;
		largest = Maximum<float>(m_bbox.SizeX(), largest);
		largest = Maximum<float>(m_bbox.SizeY(), largest);
		largest = Maximum<float>(m_bbox.SizeZ(), largest);
		if( largest == 0.0f )	m_bbox.m_radius.set(0.5f, 0.5f, 0.5f, 0.0f);
		else					m_bbox.m_radius.set(largest, largest, largest, 0.0f);
	}

	rdr::Material mat = RdrMaterialManager().GetDefaultMaterial(geometry::EType_Vertex);
	mat.m_effect = RdrMaterialManager().GetEffect(EEffect_xyztint);
	m_instance.m_model->SetMaterial(mat, rdr::model::EPrimitiveType_PointList);
	m_instance.m_model->SetName(m_name.c_str());
}

//**********************************
// TLine methods
void TLine::CreateRenderObject()
{
	using namespace rdr;
	using namespace rdr::model;
	PR_ASSERT(PR_DBG_LDR, m_point.size() % 2 == 0);

	// Create a tint material
	rdr::Material mat = RdrMaterialManager().GetDefaultMaterial(geometry::EType_Vertex);
	mat.m_effect = RdrMaterialManager().GetEffect(EEffect_xyztint);

	// Create a model containing an array of lines
	m_instance.m_model = pr::rdr::model::Line(*LineDrawer::Get().m_renderer, &m_point[0], m_point.size()/2, Colour32White, &mat);
	if( !m_instance.m_model ) { return; }

	// Give it a name
	m_instance.m_model->SetName(m_name.c_str());

	// Find the bounding box
	for( TPointVec::const_iterator i = m_point.begin(), i_end = m_point.end(); i != i_end; ++i )
		Encompase(m_bbox, *i);
}

//**********************************
// TTriangle methods
void TTriangle::CreateRenderObject()
{
	PR_ASSERT(PR_DBG_LDR, m_point.size() % 3 == 0);
	uint num_tris = (uint)m_point.size() / 3;
	uint num_vertices = num_tris * 3;
	uint num_indices = num_tris * 3;
	GeomType vert_type = geometry::EType_Vertex | geometry::EType_Normal;
	if( !m_vertex_colour.empty() )	vert_type |= geometry::EType_Colour;

	rdr::model::Settings settings;
	settings.m_vertex_type		= rdr::vf::GetTypeFromGeomType(vert_type);
	settings.m_Vcount			= num_vertices;
	settings.m_Icount			= num_indices;
	if( Failed(LineDrawer::Get().CreateModel(settings, m_instance.m_model)) ) { return; }

	uint colour_idx = 0;
	Colour32 colour[3] = {Colour32One, Colour32One, Colour32One};

	model::VLock vlock;
	rdr::vf::iterator vb = m_instance.m_model->LockVBuffer(vlock);
	for( uint t = 0; t < num_tris; ++t )
	{
		v4* point = &m_point[t * 3];
		v4 norm = Cross3(point[1] - point[0], point[2] - point[1]);
		if( !m_vertex_colour.empty() )
		{
			colour[0] = m_vertex_colour[colour_idx];	colour_idx = Clamp<uint>(colour_idx + 1, 0, (uint)m_vertex_colour.size() - 1);
			colour[1] = m_vertex_colour[colour_idx];	colour_idx = Clamp<uint>(colour_idx + 1, 0, (uint)m_vertex_colour.size() - 1);
			colour[2] = m_vertex_colour[colour_idx];	colour_idx = Clamp<uint>(colour_idx + 1, 0, (uint)m_vertex_colour.size() - 1);
		}
		Normalise3IfNonZero(norm);
		vb->set(point[0], norm, colour[0]), ++vb;
		vb->set(point[1], norm, colour[1]), ++vb;
		vb->set(point[2], norm, colour[2]), ++vb;
		Encompase(m_bbox, point[0]);
		Encompase(m_bbox, point[1]);
		Encompase(m_bbox, point[2]);
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

	rdr::Material mat = RdrMaterialManager().GetDefaultMaterial(vert_type);
	mat.m_effect = RdrMaterialManager().GetEffect(effect_id);
	m_instance.m_model->SetMaterial(mat, rdr::model::EPrimitiveType_TriangleList);
	m_instance.m_model->SetName(m_name.c_str());
}

//**********************************
// TQuad methods
void TQuad::CreateRenderObject()
{
	PR_ASSERT(PR_DBG_LDR, m_point.size() % 4 == 0);
	uint num_quads = (uint)m_point.size() / 4;
	uint num_vertices = num_quads * 4;
	uint num_indices = num_quads * 6;
	GeomType vert_type = geometry::EType_Vertex | geometry::EType_Normal;
	if( !m_vertex_colour.empty() ) vert_type |= geometry::EType_Colour;
	if( !m_texture.empty()       ) vert_type |= geometry::EType_Texture;

	rdr::model::Settings settings;
	settings.m_vertex_type		= rdr::vf::GetTypeFromGeomType(vert_type);
	settings.m_Vcount			= num_vertices;
	settings.m_Icount			= num_indices;
	if( Failed(LineDrawer::Get().CreateModel(settings, m_instance.m_model)) ) { return; }

	uint colour_idx = 0;
	Colour32 colour[4] = {Colour32One, Colour32One, Colour32One, Colour32One};

	model::VLock vlock;
	rdr::vf::iterator vb = m_instance.m_model->LockVBuffer(vlock);
	for( uint q = 0; q < num_quads; ++q )
	{
		v4 norm;
		v4* point = &m_point[q * 4];
		if( !m_vertex_colour.empty() )
		{
			colour[0] = m_vertex_colour[colour_idx];	colour_idx = Clamp<uint>(colour_idx + 1, 0, (uint)m_vertex_colour.size() - 1);
			colour[1] = m_vertex_colour[colour_idx];	colour_idx = Clamp<uint>(colour_idx + 1, 0, (uint)m_vertex_colour.size() - 1);
			colour[2] = m_vertex_colour[colour_idx];	colour_idx = Clamp<uint>(colour_idx + 1, 0, (uint)m_vertex_colour.size() - 1);
			colour[3] = m_vertex_colour[colour_idx];	colour_idx = Clamp<uint>(colour_idx + 1, 0, (uint)m_vertex_colour.size() - 1);
		}

		norm = Cross3(point[1] - point[0], point[3] - point[0]); Normalise3IfNonZero(norm);
		vb->set(point[0], norm, colour[0], v2::make(0.0f, 1.0f)), ++vb;
		Encompase(m_bbox, point[0]);
		
		norm = Cross3(point[2] - point[1], point[0] - point[1]); Normalise3IfNonZero(norm);
		vb->set(point[1], norm, colour[1], v2::make(1.0f, 1.0f)), ++vb;
		Encompase(m_bbox, point[1]);

		norm = Cross3(point[3] - point[2], point[1] - point[2]); Normalise3IfNonZero(norm);
		vb->set(point[2], norm, colour[2], v2::make(1.0f, 0.0f)), ++vb;
		Encompase(m_bbox, point[2]);

		norm = Cross3(point[0] - point[3], point[2] - point[3]); Normalise3IfNonZero(norm);
		vb->set(point[3], norm, colour[3], v2::make(0.0f, 0.0f)), ++vb;
		Encompase(m_bbox, point[3]);
	}

	model::ILock ilock;
	rdr::Index* ib = m_instance.m_model->LockIBuffer(ilock);
	rdr::Index index = 0;
	PR_EXPAND(PR_DBG_LDR, rdr::Index* ibstart = ib;)
	for( uint r = 0; r < num_quads; ++r )
	{
		*ib = index + 0;											++ib;
		*ib = index + 1;											++ib;
		*ib = index + 2;											++ib;
		*ib = index + 0;											++ib;
		*ib = index + 2;											++ib;
		*ib = index + 3;											++ib;
		index += 4;
	}
	PR_ASSERT(PR_DBG_LDR, (uint)(ib - ibstart) == num_indices);

	RdrId effect_id = GetTintEffectId(rdr::GetDefaultEffectId(vert_type));

	rdr::Material mat = RdrMaterialManager().GetDefaultMaterial(vert_type);
	mat.m_effect = RdrMaterialManager().GetEffect(effect_id);

	rdr::Texture* tex;
    if( !m_texture.empty() && Succeeded(RdrMaterialManager().LoadTexture(m_texture.c_str(), tex)) )
	{	mat.m_diffuse_texture = tex; }
	m_instance.m_model->SetMaterial(mat, rdr::model::EPrimitiveType_TriangleList);
	m_instance.m_model->SetName(m_name.c_str());
}

//**********************************
// TBox methods
void TBox::CreateRenderObject()
{
	using namespace rdr;
	using namespace rdr::model;
	PR_ASSERT(PR_DBG_LDR, m_point.size() % 8 == 0);

	// Create a tint material
	rdr::Material mat = RdrMaterialManager().GetDefaultMaterial(geometry::EType_Vertex | geometry::EType_Normal);
	mat.m_effect = RdrMaterialManager().GetEffect(EEffect_xyzlittint);

	// Create a model containing an array of boxes
	m_instance.m_model = Box(*LineDrawer::Get().m_renderer, &m_point[0], m_point.size()/8, pr::m4x4Identity, Colour32White, &mat);
	if( !m_instance.m_model ) { return; }

	// Give it a name
	m_instance.m_model->SetName(m_name.c_str());

	// Find the bounding box
	for( TPointVec::const_iterator i = m_point.begin(), i_end = m_point.end(); i != i_end; ++i )
		Encompase(m_bbox, *i);
}

//**********************************
// TCylinder methods
void TCylinder::CreateRenderObject()
{
	PR_ASSERT(PR_DBG_LDR, m_layers >= 1);
	PR_ASSERT(PR_DBG_LDR, m_wedges >= 3);
	PR_ASSERT(PR_DBG_LDR, m_point.size() == 1);
	float height		= m_point[0][0];
	float xradius		= m_point[0][1];
	float zradius		= m_point[0][2];
	uint num_faces		= 2 * m_wedges * (m_layers + 1);
	uint num_vertices	= 2 + m_wedges * (m_layers + 3);
	uint num_indices	= 3 * num_faces;

	rdr::model::Settings settings;
	settings.m_vertex_type		= rdr::vf::GetTypeFromGeomType(geometry::EType_Vertex | geometry::EType_Normal);
	settings.m_Vcount			= num_vertices;
	settings.m_Icount			= num_indices;
	if( Failed(LineDrawer::Get().CreateModel(settings, m_instance.m_model)) ) { return; }
	
	model::VLock vlock;
	rdr::vf::iterator vb = m_instance.m_model->LockVBuffer(vlock);

	uint16 w, l, wedges = (uint16)m_wedges;
	float y = -height / 2.0f, dy = height / m_layers;
	float da = 2.0f * maths::pi / m_wedges;
	
	// Bottom face
	v4 point = {0, y, 0, 1.0f};
	vb->set(point, -v4YAxis), ++vb;
	Encompase(m_bbox, point);
	for( w = 0; w < m_wedges; ++w )
	{
		point.set(Cos(w * da) * xradius, y, Sin(w * da) * zradius, 1.0f);
		vb->set(point, -v4YAxis), ++vb;
		Encompase(m_bbox, point);		
	}

	// The walls
	v4 norm;
	for( l = 0; l <= m_layers; ++l )
	{
		for( w = 0; w < m_wedges; ++w )
		{
			point.set(Cos(w * da) * xradius, y,    Sin(w * da) * zradius, 1.0f);
			norm .set(Cos(w * da) / xradius, 0.0f, Sin(w * da) / zradius, 0.0f);
			Normalise3(norm);

			vb->set(point, norm), ++vb;
			Encompase(m_bbox, point);		
		}
		y += dy;
	}

	// Top face
	y = height / 2.0f;
	for( w = 0; w < m_wedges; ++w )
	{
		point.set(Cos(w * da) * xradius, y, Sin(w * da) * zradius, 1.0f);
		vb->set(point, v4YAxis), ++vb;
		Encompase(m_bbox, point);		
	}
	point.set(0, y, 0, 1.0f);
	vb->set(point, v4YAxis), ++vb;
	Encompase(m_bbox, point);

	model::ILock ilock;
	rdr::Index* ib = m_instance.m_model->LockIBuffer(ilock);

	// Create the bottom face
	PR_EXPAND(PR_DBG_LDR, rdr::Index* ibstart = ib;)
	for( w = 1; w <= wedges; ++w )
	{
		*ib = 0;													++ib;
		*ib = w;													++ib;
		*ib = w + 1;												++ib;
	}
	*(ib - 1) = 1;
	
	// Create the walls
	for( l = 1; l <= m_layers; ++l )
	{
		for( w = 1; w <= wedges; ++w )
		{
			*ib = w + 1 + l * wedges;								++ib;
			*ib = w     + l * wedges;								++ib;
			*ib = w     + (l + 1) * wedges;							++ib;
											
			*ib = w + 1 + l * wedges;								++ib;
			*ib = w     + (l + 1) * wedges;							++ib;
			*ib = w + 1 + (l + 1) * wedges;							++ib;
		}
		*(ib - 6) = 1 + l * wedges;
		*(ib - 3) = 1 + l * wedges;
		*(ib - 1) = 1 + (l + 1) * wedges;
	}
	
	// Create the top face
	uint16 last = (uint16)(num_vertices - 1);
	for( w = 1; w <= wedges; ++w )
	{
		*ib = last;													++ib;
		*ib = last - wedges + w;									++ib;
		*ib = last - wedges + w - 1;								++ib;
	}
	*(ib - 2) = last - wedges;
	PR_ASSERT(PR_DBG_LDR, (uint)(ib - ibstart) == num_indices);

	rdr::Material mat = RdrMaterialManager().GetDefaultMaterial(geometry::EType_Vertex | geometry::EType_Normal);
	mat.m_effect = RdrMaterialManager().GetEffect(EEffect_xyzlittint);
	m_instance.m_model->SetMaterial(mat, rdr::model::EPrimitiveType_TriangleList);
	m_instance.m_model->SetName(m_name.c_str());
}

//**********************************
// TSphere methods
void TSphere::CreateRenderObject()
{
	PR_ASSERT(PR_DBG_LDR, m_point.size() == 1);

	pr::Geometry geo_sphere;
	geometry::GenerateGeosphere(geo_sphere, 1.0f, m_divisions);
	pr::Mesh& geo_sphere_mesh = geo_sphere.m_frame.front().m_mesh;

	float xradius		= m_point[0][0];
	float yradius		= m_point[0][1];
	float zradius		= m_point[0][2];
	std::size_t num_vertices = geo_sphere_mesh.m_vertex.size();
	std::size_t num_faces	 = geo_sphere_mesh.m_face.size();
	std::size_t num_indices  = num_faces * 3;
	
	rdr::model::Settings settings;
	if( m_texture.empty() )		settings.m_vertex_type = rdr::vf::GetTypeFromGeomType(geometry::EType_Vertex | geometry::EType_Normal);
	else						settings.m_vertex_type = rdr::vf::GetTypeFromGeomType(geometry::EType_Vertex | geometry::EType_Normal | geometry::EType_Texture);
	settings.m_Vcount			= num_vertices;
	settings.m_Icount			= num_indices;
	if( Failed(LineDrawer::Get().CreateModel(settings, m_instance.m_model)) ) { return; }
	
	model::VLock vlock;
	rdr::vf::iterator vb = m_instance.m_model->LockVBuffer(vlock);

	v4 point, norm;
	for( std::size_t v = 0; v != num_vertices; ++v )
	{
		v4 geo_vert = geo_sphere_mesh.m_vertex[v].m_vertex;
		v2 geo_uv   = geo_sphere_mesh.m_vertex[v].m_tex_vertex;
		point.set(geo_vert.x * xradius, geo_vert.y * yradius, geo_vert.z * zradius, 1.0f);
		norm .set(geo_vert.x / xradius, geo_vert.y / yradius, geo_vert.z / zradius, 0.0f);
		Normalise3(norm);
		vb->set(point, norm, Colour32One, geo_uv), ++vb;
		Encompase(m_bbox, point);
	}

	model::ILock ilock;
	rdr::Index* ib = m_instance.m_model->LockIBuffer(ilock);
	PR_EXPAND(PR_DBG_LDR, rdr::Index* ibstart = ib;)
	for( std::size_t f = 0; f != num_faces; ++f )
	{
		*ib = geo_sphere_mesh.m_face[f].m_vert_index[0];			++ib;
		*ib = geo_sphere_mesh.m_face[f].m_vert_index[1];			++ib;
		*ib = geo_sphere_mesh.m_face[f].m_vert_index[2];			++ib;
	}
	PR_ASSERT(PR_DBG_LDR, (uint)(ib - ibstart) == num_indices);

	rdr::Texture* tex;
	if( !m_texture.empty() && Succeeded(RdrMaterialManager().LoadTexture(m_texture.c_str(), tex)) )
	{
		rdr::Material mat;
		mat.m_effect			= RdrMaterialManager().GetEffect(EEffect_xyzlittinttextured);
		mat.m_diffuse_texture	= tex;
		m_instance.m_model->SetMaterial(mat, rdr::model::EPrimitiveType_TriangleList);
	}
	else
	{
		rdr::Material mat = RdrMaterialManager().GetDefaultMaterial(geometry::EType_Vertex | geometry::EType_Normal);
		mat.m_effect = RdrMaterialManager().GetEffect(EEffect_xyzlittint);
		m_instance.m_model->SetMaterial(mat, rdr::model::EPrimitiveType_TriangleList);
	}
	m_instance.m_model->SetName(m_name.c_str());
}

//**********************************
// TPolytope methods
void TPolytope::CreateRenderObject()
{
	std::vector<rdr::Index>	vindex(m_point.size());
	std::vector<rdr::Index>	face(6 * (m_point.size() - 2));
	std::generate(vindex.begin(), vindex.end(), pr::ArithmeticSequence<rdr::Index>(0,1));
	std::size_t num_verts;
	std::size_t num_faces;

	// Find the convex hull
	ConvexHull(&m_point[0], &vindex[0], &vindex[0] + vindex.size(), &face[0], &face[0] + face.size(), num_verts, num_faces);
	vindex.resize(num_verts);
	face.resize(3*num_faces);

	rdr::model::Settings settings;
	settings.m_vertex_type		= rdr::vf::GetTypeFromGeomType(geometry::EType_Vertex | geometry::EType_Normal | geometry::EType_Texture);
	settings.m_Vcount			= vindex.size();
	settings.m_Icount			= face.size();
	if( Failed(LineDrawer::Get().CreateModel(settings, m_instance.m_model)) ) { return; }
	
	model::MLock mlock(m_instance.m_model);

	rdr::vf::iterator vb = mlock.m_vlock.m_ptr;
	for( std::vector<rdr::Index>::const_iterator v = vindex.begin(), v_end = vindex.end(); v != v_end; ++v )
	{
		vb->set(m_point[*v], v4Zero, Colour32One, v2Zero), ++vb;
		Encompase(m_bbox, m_point[*v]);
	}

	rdr::Index* ib = mlock.m_ilock.m_ptr;
	for( std::vector<rdr::Index>::const_iterator f = face.begin(), f_end = face.end(); f != f_end; ++f )
	{
		*ib++ = *f;
	}

	model::GenerateNormals(mlock);

	rdr::Material mat = RdrMaterialManager().GetDefaultMaterial(geometry::EType_Vertex | geometry::EType_Normal);
	mat.m_effect = RdrMaterialManager().GetEffect(EEffect_xyzlittint);
	m_instance.m_model->SetMaterial(mat, rdr::model::EPrimitiveType_TriangleList);
	m_instance.m_model->SetName(m_name.c_str());
}

//**********************************
// TFrustum methods
void TFrustum::CreateRenderObject()
{
	PR_ASSERT(PR_DBG_LDR, m_point.size() == 8);
	//float width			= m_point[0][0];
	//float height		= m_point[0][1];
	//float Near			= m_point[1][0];
	//float Far			= m_point[1][1];
	uint num_faces		= 12;
	uint num_vertices	= 3 * 8;	// Each vertex has three normals
	uint num_indices	= 3 * num_faces;

	rdr::model::Settings settings;
	settings.m_vertex_type		= rdr::vf::GetTypeFromGeomType(geometry::EType_Vertex | geometry::EType_Normal);
	settings.m_Vcount			= num_vertices;
	settings.m_Icount			= num_indices;
	if( Failed(LineDrawer::Get().CreateModel(settings, m_instance.m_model)) ) { return; }

	model::VLock vlock;
	rdr::vf::iterator vb = m_instance.m_model->LockVBuffer(vlock);
	v4 Lnorm = {-m_point[7][2],	0.0f,			 m_point[7][0], 0.0f};	Normalise3(Lnorm);
	v4 Tnorm = { 0.0f,			-m_point[6][2],	 m_point[6][1], 0.0f};	Normalise3(Tnorm);
	v4 Rnorm = { m_point[5][2],	0.0f,			-m_point[5][0], 0.0f};	Normalise3(Rnorm);
	v4 Bnorm = { 0.0f,			 m_point[5][2], -m_point[5][1], 0.0f};	Normalise3(Bnorm);

    vb->set(m_point[0], -v4ZAxis), ++vb;
	vb->set(m_point[1], -v4ZAxis), ++vb;
	vb->set(m_point[2], -v4ZAxis), ++vb;
	vb->set(m_point[3], -v4ZAxis), ++vb;
	
	vb->set(m_point[2], Rnorm), ++vb;
	vb->set(m_point[3], Rnorm), ++vb;
	vb->set(m_point[4], Rnorm), ++vb;
	vb->set(m_point[5], Rnorm), ++vb;
	
	vb->set(m_point[4], v4ZAxis), ++vb;
	vb->set(m_point[5], v4ZAxis), ++vb;
	vb->set(m_point[6], v4ZAxis), ++vb;
	vb->set(m_point[7], v4ZAxis), ++vb;
	
	vb->set(m_point[6], Lnorm), ++vb;
	vb->set(m_point[7], Lnorm), ++vb;
	vb->set(m_point[0], Lnorm), ++vb;
	vb->set(m_point[1], Lnorm), ++vb;
	
	vb->set(m_point[1], Bnorm), ++vb;
	vb->set(m_point[3], Bnorm), ++vb;
	vb->set(m_point[5], Bnorm), ++vb;
	vb->set(m_point[7], Bnorm), ++vb;

	vb->set(m_point[0], Tnorm), ++vb;
	vb->set(m_point[2], Tnorm), ++vb;
	vb->set(m_point[4], Tnorm), ++vb;
	vb->set(m_point[6], Tnorm), ++vb;

	Encompase(m_bbox, m_point[0]);
	Encompase(m_bbox, m_point[1]);
	Encompase(m_bbox, m_point[2]);
	Encompase(m_bbox, m_point[3]);
	Encompase(m_bbox, m_point[4]);
	Encompase(m_bbox, m_point[5]);
	Encompase(m_bbox, m_point[6]);
	Encompase(m_bbox, m_point[7]);

	model::ILock ilock;
	rdr::Index* ib = m_instance.m_model->LockIBuffer(ilock);
	PR_EXPAND(PR_DBG_LDR, rdr::Index* ibstart = ib;)
	*ib	= 0;														++ib;
	*ib	= 1;														++ib;
	*ib	= 3;														++ib;
	*ib	= 0;														++ib;
	*ib	= 3;														++ib;
	*ib = 2;														++ib;
				
	*ib = 4;														++ib;
	*ib = 5;														++ib;
	*ib = 7;														++ib;
	*ib = 4;														++ib;
	*ib = 7;														++ib;
	*ib = 6;														++ib;
				
	*ib = 8;														++ib;
	*ib = 9;														++ib;
	*ib = 11;														++ib;
	*ib = 8;														++ib;
	*ib = 11;														++ib;
	*ib = 10;														++ib;
				
	*ib = 12;														++ib;
	*ib = 13;														++ib;
	*ib = 15;														++ib;
	*ib = 12;														++ib;
	*ib = 15;														++ib;
	*ib = 14;														++ib;
				
	*ib = 16;														++ib;
	*ib = 19;														++ib;
	*ib = 18;														++ib;
	*ib = 16;														++ib;
	*ib = 18;														++ib;
	*ib = 17;														++ib;
				
	*ib = 20;														++ib;
	*ib = 21;														++ib;
	*ib = 22;														++ib;
	*ib = 20;														++ib;
	*ib = 22;														++ib;
	*ib = 23;														++ib;
	PR_ASSERT(PR_DBG_LDR, (uint)(ib - ibstart) == num_indices);
	
	rdr::Material mat = RdrMaterialManager().GetDefaultMaterial(geometry::EType_Vertex | geometry::EType_Normal);
	mat.m_effect = RdrMaterialManager().GetEffect(EEffect_xyzlittint);
	m_instance.m_model->SetMaterial(mat, rdr::model::EPrimitiveType_TriangleList);
	m_instance.m_model->SetName(m_name.c_str());
}

//**********************************
// TGrid methods
void TGrid::CreateRenderObject()
{
	PR_ASSERT(PR_DBG_LDR, m_point.size() > 0);
	uint16 width	= static_cast<uint16>(m_point[0][0]);
	uint16 height	= static_cast<uint16>(m_point[0][1]);
	uint num_points	= (uint)m_point.size() - 1;
	PR_ASSERT(PR_DBG_LDR, num_points == (uint)width * height);
    uint num_edges		= (width - 1) * height + (height - 1) * width;
	uint num_indices	= num_edges * 2;
	uint num_vertices	= num_points;

	rdr::model::Settings settings;
	settings.m_vertex_type		= rdr::vf::GetTypeFromGeomType(geometry::EType_Vertex);
	settings.m_Vcount			= num_vertices;
	settings.m_Icount			= num_indices;
	if( Failed(LineDrawer::Get().CreateModel(settings, m_instance.m_model)) ) { return; }

	model::VLock vlock;
	rdr::vf::iterator vb = m_instance.m_model->LockVBuffer(vlock);
	for( uint v = 0; v < num_vertices; ++v )
	{
		vb->set(m_point[v + 1], v4Zero), ++vb;
		Encompase(m_bbox, m_point[v + 1]);
	}

	model::ILock ilock;
	rdr::Index* ib = m_instance.m_model->LockIBuffer(ilock);
	PR_EXPAND(PR_DBG_LDR, rdr::Index* ibstart = ib;)

	// Across
	for( uint16 h = 0; h != height; ++h )
	{
		rdr::Index row = width * h;
		for( uint16 w = 0; w != width - 1; ++w )
		{
			rdr::Index col = row + w;
			*ib = col;												++ib;
			*ib = col + 1;											++ib;
		}
	}
	// Down
	for( uint16 w = 0; w != width; ++w )
	{
		rdr::Index col = w;
		for( uint16 h = 0; h != height - 1; ++h )
		{
			rdr::Index row = col + h * width;
			*ib = row;												++ib;
			*ib = row + width;										++ib;
		}
	}
	PR_ASSERT(PR_DBG_LDR, (uint)(ib - ibstart) == num_indices);

	rdr::Material mat = RdrMaterialManager().GetDefaultMaterial(geometry::EType_Vertex);
	mat.m_effect = RdrMaterialManager().GetEffect(EEffect_xyztint);
	m_instance.m_model->SetMaterial(mat, rdr::model::EPrimitiveType_LineList);
	m_instance.m_model->SetName(m_name.c_str());
}

//**********************************
// TSurface methods
void TSurface::CreateRenderObject()
{
	PR_ASSERT(PR_DBG_LDR, m_point.size() > 0);
	uint16 width	= static_cast<uint16>(m_point[0][0]);
	uint16 height	= static_cast<uint16>(m_point[0][1]);
	uint num_points	= (uint)m_point.size() - 1;
	PR_ASSERT(PR_DBG_LDR, num_points == (uint)width * height);
    uint num_faces		= 2 * (width - 1) * (height - 1);
	uint num_indices	= num_faces * 3;
	uint num_vertices	= num_points;

	rdr::model::Settings settings;
	settings.m_vertex_type		= rdr::vf::GetTypeFromGeomType(geometry::EType_Vertex | geometry::EType_Normal);
	settings.m_Vcount			= num_vertices;
	settings.m_Icount			= num_indices;
	if( Failed(LineDrawer::Get().CreateModel(settings, m_instance.m_model)) ) { return; }

	model::MLock mlock(m_instance.m_model);
	rdr::vf::iterator vb = mlock.m_vlock.m_ptr;
	for( uint v = 0; v < num_vertices; ++v )
	{
		vb->set(m_point[v + 1], v4Zero), ++vb;
		Encompase(m_bbox, m_point[v + 1]);
	}

	rdr::Index* ib = mlock.m_ilock.m_ptr;
	for( uint16 h = 0; h < height - 1; ++h )
	{
		rdr::Index row = width * h;
		for( uint16 w = 0; w < width - 1; ++w )
		{
			rdr::Index col = row + w;
			*ib++ = col;
			*ib++ = col + width;
			*ib++ = col + 1 + width;
			*ib++ = col;
			*ib++ = col + 1 + width;
			*ib++ = col + 1;
		}
	}

	model::GenerateNormals(mlock);

	rdr::Material mat = RdrMaterialManager().GetDefaultMaterial(geometry::EType_Vertex | geometry::EType_Normal);
	mat.m_effect = RdrMaterialManager().GetEffect(EEffect_xyzlittint);
	m_instance.m_model->SetMaterial(mat, rdr::model::EPrimitiveType_TriangleList);
	m_instance.m_model->SetName(m_name.c_str());
}


//**********************************
// TMatrix methods
void TMatrix::CreateRenderObject()
{
	PR_ASSERT(PR_DBG_LDR, m_point.size() > 0);
	uint num_matrices	= (uint)m_point.size() / 4;
	uint num_vertices	= num_matrices * 6;
	uint num_indices	= num_matrices * 6;

	rdr::model::Settings settings;
	settings.m_vertex_type		= rdr::vf::GetTypeFromGeomType(geometry::EType_Vertex | geometry::EType_Colour);
	settings.m_Vcount			= num_vertices;
	settings.m_Icount			= num_indices;
	if( Failed(LineDrawer::Get().CreateModel(settings, m_instance.m_model)) ) { return; }

	model::VLock vlock;
	rdr::vf::iterator vb = m_instance.m_model->LockVBuffer(vlock);
	for( uint m = 0; m != num_matrices; ++m )
	{
		v4 position = m_point[m + 3];

		vb->set(position                 , v4Zero, Colour32Red), ++vb;
		vb->set(position + m_point[m + 0], v4Zero, Colour32Red), ++vb;

		vb->set(position                 , v4Zero, Colour32Green), ++vb;
		vb->set(position + m_point[m + 1], v4Zero, Colour32Green), ++vb;

		vb->set(position                 , v4Zero, Colour32Blue), ++vb;
		vb->set(position + m_point[m + 2], v4Zero, Colour32Blue), ++vb;

		Encompase(m_bbox, position                 );
		Encompase(m_bbox, position + m_point[m + 0]);
		Encompase(m_bbox, position + m_point[m + 1]);
		Encompase(m_bbox, position + m_point[m + 2]);
	}
						  
	model::ILock ilock;
	rdr::Index* ib = m_instance.m_model->LockIBuffer(ilock);
	PR_EXPAND(PR_DBG_LDR, rdr::Index* ibstart = ib;)
	for( uint i = 0; i != num_indices; ++i )
	{
		*ib = static_cast<rdr::Index>(i);							++ib;
	}
	PR_ASSERT(PR_DBG_LDR, (uint)(ib - ibstart) == num_indices);

	rdr::Material mat = RdrMaterialManager().GetDefaultMaterial(geometry::EType_Vertex | geometry::EType_Colour);
	mat.m_effect = RdrMaterialManager().GetEffect(EEffect_xyzpvc);
	m_instance.m_model->SetMaterial(mat, rdr::model::EPrimitiveType_LineList);
	m_instance.m_model->SetName(m_name.c_str());
}

//**********************************
// TMesh methods
void TMesh::CreateRenderObject()
{
	uint num_indices	= (uint)m_index.size();
	uint num_vertices	= (uint)m_point.size();
	uint num_normals	= (uint)m_normal.size();
	GeomType geom_type	= geometry::EType_Vertex | (geometry::EType_Normal * !m_line_list);

	rdr::model::Settings settings;
	settings.m_vertex_type		= rdr::vf::GetTypeFromGeomType(geom_type);
	settings.m_Vcount			= num_vertices;
	settings.m_Icount			= num_indices;
	if( Failed(LineDrawer::Get().CreateModel(settings, m_instance.m_model)) ) { return; }

	model::MLock mlock(m_instance.m_model);
	rdr::vf::iterator vb = mlock.m_vlock.m_ptr;
	for( uint v = 0; v < num_vertices; ++v )
	{
		if( num_normals == num_vertices )
		{
			vb->set(m_point[v], m_normal[v]), ++vb;
		}
		else
		{
			vb->set(m_point[v], v4Zero), ++vb;
		}
		Encompase(m_bbox, m_point[v]);
	}

	rdr::Index* ib = mlock.m_ilock.m_ptr;
	for( uint i = 0; i < num_indices; ++i )
	{
		*ib++ = m_index[i];
	}

	if( m_generate_normals && (geom_type & geometry::EType_Normal) != 0 )
		model::GenerateNormals(mlock);

	rdr::Material mat = RdrMaterialManager().GetDefaultMaterial(geom_type);
	if( (geom_type & geometry::EType_Normal) != 0 )
		mat.m_effect = RdrMaterialManager().GetEffect(EEffect_xyzlittint);
	else
		mat.m_effect = RdrMaterialManager().GetEffect(EEffect_xyztint);
	m_instance.m_model->SetMaterial(mat, !m_line_list ? rdr::model::EPrimitiveType_TriangleList : rdr::model::EPrimitiveType_LineList);
	m_instance.m_model->SetName(m_name.c_str());
}

//**********************************
// TFile methods
void TFile::CreateRenderObject()
{
	PR_ASSERT_STR(PR_DBG_LDR, !m_geometry.m_frame.empty(), "Geometry has not been loaded");
	PR_ASSERT_STR(PR_DBG_LDR, m_frame_number == Clamp<uint>(m_frame_number, 0, (uint)(m_geometry.m_frame.size() - 1)), "Frame number out of range");
	Frame& frame = m_geometry.m_frame[m_frame_number];

	PR_ASSERT_STR(PR_DBG_LDR, geometry::IsValid(frame.m_mesh.m_geometry_type), "Invalid geometry type");
	
	// If the first normal is Zero then generate normals for the mesh
	if( (frame.m_mesh.m_geometry_type & geometry::EType_Normal) && IsZero3(frame.m_mesh.m_vertex[0].m_normal) )
	{
		geometry::GenerateNormals(frame.m_mesh);
	}
	geometry::OptimiseMesh(frame.m_mesh);
	m_bbox = geometry::GetBoundingBox(frame.m_mesh);

	// Load the model
	if( Failed(rdr::LoadMesh(*LineDrawer::Get().m_renderer, frame.m_mesh, m_instance.m_model)) )
	{
		return;
	}

	// Loop through the nuggets for the model setting the effect to the tint version
	Effect*      lit_effect			= RdrMaterialManager().GetEffect(EEffect_xyzlit);
	Effect*      lit_pvc_effect		= RdrMaterialManager().GetEffect(EEffect_xyzlitpvc);
	Effect* tint_lit_effect			= RdrMaterialManager().GetEffect(EEffect_xyzlittint);
	Effect*      tex_effect			= RdrMaterialManager().GetEffect(EEffect_xyzlittextured);
	Effect* tint_tex_effect			= RdrMaterialManager().GetEffect(EEffect_xyzlittinttextured);
	Effect*      lit_pvc_tex_effect = RdrMaterialManager().GetEffect(EEffect_xyzlitpvctextured);
	Effect* tint_lit_pvc_tex_effect = RdrMaterialManager().GetEffect(EEffect_xyzlitpvctinttextured);
	for( TNuggetChain::iterator n = m_instance.m_model->m_render_nugget.begin(), n_end = m_instance.m_model->m_render_nugget.end(); n != n_end; ++n )
	{
			 if( n->m_material.m_effect == lit_effect			) { n->m_material.m_effect = tint_lit_effect; }
		else if( n->m_material.m_effect == lit_pvc_effect		) { n->m_material.m_effect = tint_lit_effect; }
		else if( n->m_material.m_effect == tex_effect			) { n->m_material.m_effect = tint_tex_effect; }
		else if( n->m_material.m_effect == lit_pvc_tex_effect	) { n->m_material.m_effect = tint_lit_pvc_tex_effect; }
		else { PR_ASSERT_STR(PR_DBG_LDR, false, "Unknown effect being used"); }
	}

	model::MLock mlock(m_instance.m_model);
	if( m_generate_normals ) model::GenerateNormals(mlock); 
	m_instance.m_model->SetName(frame.m_name.c_str());
}

//**********************************
// TGroup methods
void TGroup::CreateRenderObject()
{
	if( m_child.empty() )	m_bbox.reset();
	else					m_bbox = BBox();
}

//**********************************
// TGroupCyclic methods
void TGroupCyclic::CreateRenderObject()
{
	if( m_child.empty() )	m_bbox.reset();
	else					m_bbox = BBox();
}

//*****
// Turn on animations
bool TGroupCyclic::SetCyclic(bool start)
{
	if( start )
	{
		m_start_time = GetTickCount();
		m_cycling	 = true;
	}
	else
	{
		m_cycling = false;
	}
	bool cycling_on = m_cycling;
	
	// Set the children
	for( TLdrObjectPtrVec::iterator i = m_child.begin(), i_end = m_child.end(); i != i_end; ++i )
	{
		cycling_on |= (*i)->SetCyclic(start);
	}

	return cycling_on;
}

//*****
// Render the correct frame
void TGroupCyclic::Render(rdr::Viewport& viewport, const m4x4& parent_object_to_world)
{
	if( !m_enabled ) return;

	if( m_cycling )
	{
		uint num_children = (uint)m_child.size();
		uint now = (uint)GetTickCount() - m_start_time;
		uint frame = now / m_ms_per_frame;
		switch( m_style )
		{
		case START_END:	frame %= num_children; break;
		case END_START: frame = num_children - 1 - (frame % num_children); break;
		case PING_PONG:
			if( (frame % (2*num_children)) < num_children )	frame %= num_children;
			else											frame  = num_children - 1 - (frame % num_children);
			break;
		};

		PR_ASSERT(PR_DBG_LDR, frame < (uint)m_child.size());
		m_child[frame]->Render(viewport, parent_object_to_world);
	}
	else
	{
		PR_ASSERT(PR_DBG_LDR, !m_child.empty());
		m_child[0]->Render(viewport, parent_object_to_world);
	}
}

//*****
// A custom object with model data modified via call back function
TCustom::TCustom(LineDrawer&, pr::ldr::CustomObjectData const& data)
:LdrObject(tCustom, data.m_name, data.m_colour, "")
{
	rdr::model::Settings settings;
	settings.m_vertex_type		= rdr::vf::GetTypeFromGeomType(data.m_geom_type);
	settings.m_Vcount			= data.m_num_verts;
	settings.m_Icount			= data.m_num_indices;
	if( Failed(LineDrawer::Get().CreateModel(settings, m_instance.m_model)) ) { return; }

	m_instance.m_model->SetName(data.m_name.c_str());
	m_instance.m_instance_to_world = data.m_i2w;
	data.m_create_func(m_instance.m_model, m_bbox, data.m_user_data, RdrMaterialManager());
}
#endif//USE_OLD_PARSER