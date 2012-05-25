//*********************************************
// Renderer Model Generator
//  Copyright © Rylogic Ltd 2007
//*********************************************

#include "renderer/utility/stdafx.h"
#include "pr/renderer/models/modelgenerator.h"
#include "pr/renderer/models/types.h"
#include "pr/renderer/models/model.h"
#include "pr/renderer/renderer/renderer.h"
#include "pr/geometry/geosphere.h"

using namespace pr;
using namespace pr::rdr;
using namespace pr::rdr::model;

// General *****************************************************************************************
// Generate normals for this model
// Assumes the locked region of the model contains a triangle list
void pr::rdr::model::GenerateNormals(MLock& mlock, Range const* v_range, Range const* i_range)
{
	if (!v_range) v_range = &mlock.m_vrange;
	if (!i_range) i_range = &mlock.m_irange;
	PR_ASSERT(PR_DBG_RDR, IsWithin(mlock.m_vlock.m_range, *v_range), "The provided vertex range is not within the locked range");
	PR_ASSERT(PR_DBG_RDR, IsWithin(mlock.m_ilock.m_range, *i_range), "The provided index range is not within the locked range");
	PR_ASSERT(PR_DBG_RDR, (i_range->size() % 3) == 0, "This function assumes the index range refers to a triangle list");
	PR_ASSERT(PR_DBG_RDR, vf::GetFormat(mlock.m_model->GetVertexType()) & vf::EFormat::Norm, "Vertices must have normals");

	// Initialise all of the normals to zero
	vf::iterator vb = mlock.m_vlock.m_ptr + v_range->m_begin;
	for (std::size_t v = 0; v != v_range->size(); ++v, ++vb)
		Zero(vb->normal());
	
	Index* ib = mlock.m_ilock.m_ptr + i_range->m_begin;
	for (std::size_t f = 0, f_end = f + i_range->size()/3; f != f_end; ++f, ib += 3)
	{
		PR_ASSERT(PR_DBG_RDR, mlock.m_vlock.m_range.contains(ib[0]), "Face index refers outside of the locked vertex range");
		PR_ASSERT(PR_DBG_RDR, mlock.m_vlock.m_range.contains(ib[1]), "Face index refers outside of the locked vertex range");
		PR_ASSERT(PR_DBG_RDR, mlock.m_vlock.m_range.contains(ib[2]), "Face index refers outside of the locked vertex range");
		vf::RefVertex v0 = mlock.m_vlock.m_ptr[ib[0]];
		vf::RefVertex v1 = mlock.m_vlock.m_ptr[ib[1]];
		vf::RefVertex v2 = mlock.m_vlock.m_ptr[ib[2]];

		// Calculate a face normal
		v3 norm = Cross3(v1.vertex() - v0.vertex(), v2.vertex() - v0.vertex());
		Normalise3IfNonZero(norm);

		// Add the normal to each vertex that references the face
		v0.normal() += norm;
		v1.normal() += norm;
		v2.normal() += norm;
	}

	// Normalise all of the normals
	vb = mlock.m_vlock.m_ptr + v_range->m_begin;
	for (std::size_t v = 0; v != v_range->size(); ++v, ++vb)
		Normalise3IfNonZero(vb->normal());
}
void pr::rdr::model::GenerateNormals(ModelPtr& model, Range const* v_range, Range const* i_range)
{
	MLock mlock(model);
	GenerateNormals(mlock, v_range, i_range);
}
	
// Set the vertex colours in a model
void pr::rdr::model::SetVertexColours(MLock& mlock, Colour32 colour, Range const* v_range)
{
	if (!v_range) v_range = &mlock.m_vrange;
	PR_ASSERT(PR_DBG_RDR, IsWithin(mlock.m_vlock.m_range, *v_range), "The provided vertex range is not within the locked range");
	PR_ASSERT(PR_DBG_RDR, vf::GetFormat(mlock.m_model->GetVertexType()) & vf::EFormat::Diff, "Vertices must have colours");

	vf::iterator vb = mlock.m_vlock.m_ptr + v_range->m_begin;
	for (std::size_t v = 0; v != v_range->size(); ++v, ++vb)
		vb->colour() = colour;
}
	
// Line *****************************************************************************************
// Return the model buffer requirements of an array of lines
void pr::rdr::model::LineSize(Range& v_range, Range& i_range, std::size_t num_lines)
{
	v_range.set(0, 2 * num_lines);
	i_range.set(0, 2 * num_lines);
}
	
// Return model settings for creating an array of lines
Settings pr::rdr::model::LineModelSettings(std::size_t num_lines)
{
	Range v_range, i_range;
	LineSize(v_range, i_range, num_lines);

	Settings settings;
	settings.m_vertex_type = vf::GetTypeFromGeomType(geom::EVC);
	settings.m_Vcount      = v_range.size();
	settings.m_Icount      = i_range.size();
	return settings;
}
	
// Generate lines from an array of points
// 'point' is an array of start and end points for lines
// 'colours' is an array of colour values or a pointer to a single colour.
// 'num_colours' should be either, 0, 1, or num_lines * 2
pr::rdr::ModelPtr pr::rdr::model::Line(MLock& mlock, MaterialManager& matmgr, v4 const* point, std::size_t num_lines, Colour32 const* colours, std::size_t num_colours, rdr::Material const* mat, Range* v_range, Range* i_range)
{
	// Handle optional parameters
	Range local_v_range, local_i_range;
	if (!v_range) v_range = &local_v_range;
	if (!i_range) i_range = &local_i_range;
	LineSize(*v_range, *i_range, num_lines);
	
	Colour32 local_colours[2] = {Colour32White, Colour32White};
	if (num_colours == 1)            { local_colours[0] = colours[0]; local_colours[1] = colours[0]; }
	if (!colours || num_colours < 2) { colours = local_colours; num_colours = 2; }
	std::size_t col_inc = 2 * std::size_t(num_colours == 2*num_lines);
	
	// Shift the ranges into the editable range in the model
	v_range->shift((int)mlock.m_vrange.m_begin);
	i_range->shift((int)mlock.m_irange.m_begin);
	PR_ASSERT(PR_DBG_RDR, mlock.m_vrange.contains(*v_range), FmtS("Insufficient space in model buffer. Additional %d verts required", v_range->size() - mlock.m_vrange.size()));
	PR_ASSERT(PR_DBG_RDR, mlock.m_irange.contains(*i_range), FmtS("Insufficient space in model buffer. Additional %d indices required", v_range->size() - mlock.m_vrange.size()));
	
	bool has_alpha = false;
	vf::iterator vb   = mlock.m_vlock.m_ptr + v_range->m_begin;
	rdr::Index*  ib   = mlock.m_ilock.m_ptr + i_range->m_begin;
	rdr::Index   base = value_cast<rdr::Index>(v_range->m_begin);
	for (std::size_t i = 0; i != num_lines; ++i, point += 2, base += 2, colours += col_inc)
	{
		vb->set(point[0], colours[0]), ++vb;
		vb->set(point[1], colours[1]), ++vb;
		
		// Grow the bounding box
		pr::Encompase(mlock.m_model->m_bbox, point[0]);
		pr::Encompase(mlock.m_model->m_bbox, point[1]);
		
		*ib++ = base + 0;
		*ib++ = base + 1;
		
		// Look for alpha
		has_alpha |= colours[0].a() != 0xFF;
		has_alpha |= colours[1].a() != 0xFF;
	}
	
	// Add a render nugget
	rdr::Material local_mat = mat ? *mat : matmgr.GetMaterial(geom::EVC);
	SetAlphaRenderStates(local_mat.m_rsb, has_alpha);
	mlock.m_model->SetMaterial(local_mat, EPrimitive::LineList, false, v_range, i_range);

	// Update the editable ranges
	mlock.m_vrange.m_begin += v_range->size();
	mlock.m_irange.m_begin += i_range->size();
	return mlock.m_model;
}
pr::rdr::ModelPtr pr::rdr::model::Line(MLock& mlock, MaterialManager& matmgr, v4 const* point, std::size_t num_lines, Colour32 colour, rdr::Material const* mat, Range* v_range, Range* i_range)
{
	return Line(mlock, matmgr, point, num_lines, &colour, 1, mat, v_range, i_range);
}
pr::rdr::ModelPtr pr::rdr::model::Line(Renderer& rdr, v4 const* point, std::size_t num_lines, Colour32 const* colours, std::size_t num_colours, rdr::Material const* mat, Range* v_range, Range* i_range)
{
	MLock mlock(rdr.m_mdl_mgr.CreateModel(LineModelSettings(num_lines)));
	return Line(mlock, rdr.m_mat_mgr, point, num_lines, colours, num_colours, mat, v_range, i_range);
}
pr::rdr::ModelPtr pr::rdr::model::Line(Renderer& rdr, v4 const* point, std::size_t num_lines, Colour32 colour, rdr::Material const* mat, Range* v_range, Range* i_range)
{
	return Line(rdr, point, num_lines, &colour, 1, mat, v_range, i_range);
}
pr::rdr::ModelPtr pr::rdr::model::LineD(MLock& mlock, MaterialManager& matmgr, v4 const* points, v4 const* directions, std::size_t num_lines, Colour32 const* colours, std::size_t num_colours, rdr::Material const* mat, Range* v_range, Range* i_range)
{
	pr::Array<pr::v4> point(num_lines * 2);
	v4* pt = &point[0];
	for (std::size_t i = 0; i != num_lines; ++i, ++points, ++directions)
	{
		*pt++ = *points;
		*pt++ = *points + *directions;
	}
	return Line(mlock, matmgr, &point[0], num_lines, colours, num_colours, mat, v_range, i_range);
}
pr::rdr::ModelPtr pr::rdr::model::LineD(MLock& mlock, MaterialManager& matmgr, v4 const* points, v4 const* directions, std::size_t num_lines, Colour32 colour, rdr::Material const* mat, Range* v_range, Range* i_range)
{
	return LineD(mlock, matmgr, points, directions, num_lines, &colour, 1, mat, v_range, i_range);
}
pr::rdr::ModelPtr pr::rdr::model::LineD(Renderer& rdr, v4 const* points, v4 const* directions, std::size_t num_lines, Colour32 const* colours, std::size_t num_colours, rdr::Material const* mat, Range* v_range, Range* i_range)
{
	MLock mlock(rdr.m_mdl_mgr.CreateModel(LineModelSettings(num_lines)));
	return LineD(mlock, rdr.m_mat_mgr, points, directions, num_lines, colours, num_colours, mat, v_range, i_range);
}
pr::rdr::ModelPtr pr::rdr::model::LineD(Renderer& rdr, v4 const* points, v4 const* directions, std::size_t num_lines, Colour32 colour, rdr::Material const* mat, Range* v_range, Range* i_range)
{
	return LineD(rdr, points, directions, num_lines, &colour, 1, mat, v_range, i_range);
}
	
// Quad *****************************************************************************************
// Return the model buffer requirements for an array of quads
void pr::rdr::model::QuadSize(Range& v_range, Range& i_range, std::size_t num_quads)
{
	v_range.set(0, 4 * num_quads);
	i_range.set(0, 6 * num_quads);
}
	
// Return model settings for creating an array of quads
Settings pr::rdr::model::QuadModelSettings(std::size_t num_quads)
{
	Range v_range, i_range;
	QuadSize(v_range, i_range, num_quads);

	Settings settings;
	settings.m_vertex_type = vf::GetTypeFromGeomType(geom::EVNCT);
	settings.m_Vcount      = v_range.size();
	settings.m_Icount      = i_range.size();
	return settings;
}
	
// Generate quads from an array of corners
pr::rdr::ModelPtr pr::rdr::model::Quad(MLock& mlock, MaterialManager& matmgr, v4 const* point ,std::size_t num_quads ,Colour32 const* colours ,std::size_t num_colours ,rdr::Material const* mat ,Range* v_range ,Range* i_range)
{
	// Handle optional parameters
	Range local_v_range, local_i_range;
	if (!v_range) v_range = &local_v_range;
	if (!i_range) i_range = &local_i_range;
	QuadSize(*v_range, *i_range, num_quads);

	// Shift the ranges into the editable range in the model
	v_range->shift((int)mlock.m_vrange.m_begin);
	i_range->shift((int)mlock.m_irange.m_begin);
	PR_ASSERT(PR_DBG_RDR, mlock.m_vrange.contains(*v_range), FmtS("Insufficient space in model buffer. Additional %d verts required", v_range->size() - mlock.m_vrange.size()));
	PR_ASSERT(PR_DBG_RDR, mlock.m_irange.contains(*i_range), FmtS("Insufficient space in model buffer. Additional %d indices required", v_range->size() - mlock.m_vrange.size()));

	// Set up a colour array pointer for each quad corner colour
	Colour32 local_colours[4] = {Colour32White, Colour32White, Colour32White, Colour32White};
	if (num_colours == 1)            { local_colours[0] = local_colours[1] = local_colours[2] = local_colours[3] = colours[0]; }
	if (!colours || num_colours < 4) { colours = local_colours; num_colours = 4; }
	std::size_t col_inc = 4 * std::size_t(num_colours == 4*num_quads);

	// Create the model of quads
	bool has_alpha = false;
	vf::iterator vb   = mlock.m_vlock.m_ptr + v_range->m_begin;
	rdr::Index*  ib   = mlock.m_ilock.m_ptr + i_range->m_begin;
	rdr::Index   base = value_cast<rdr::Index>(v_range->m_begin);
	for (std::size_t q = 0; q != num_quads; ++q, point += 4, base += 4, colours += col_inc)
	{
		vb->set(point[0], GetNormal3IfNonZero(Cross3(point[1] - point[0], point[3] - point[0])), colours[0], v2::make(0.0f, 1.0f));		++vb;
		vb->set(point[1], GetNormal3IfNonZero(Cross3(point[2] - point[1], point[0] - point[1])), colours[1], v2::make(1.0f, 1.0f));		++vb;
		vb->set(point[2], GetNormal3IfNonZero(Cross3(point[3] - point[2], point[1] - point[2])), colours[2], v2::make(1.0f, 0.0f));		++vb;
		vb->set(point[3], GetNormal3IfNonZero(Cross3(point[0] - point[3], point[2] - point[3])), colours[3], v2::make(0.0f, 0.0f));		++vb;
		
		// Grow the bounding box
		pr::Encompase(mlock.m_model->m_bbox, point[0]);
		pr::Encompase(mlock.m_model->m_bbox, point[1]);
		pr::Encompase(mlock.m_model->m_bbox, point[2]);
		pr::Encompase(mlock.m_model->m_bbox, point[3]);

		*ib++ = base + 0;
		*ib++ = base + 1;
		*ib++ = base + 2;
		*ib++ = base + 0;
		*ib++ = base + 2;
		*ib++ = base + 3;

		// Look for alpha
		has_alpha |= colours[0].a() != 0xFF;
		has_alpha |= colours[1].a() != 0xFF;
		has_alpha |= colours[2].a() != 0xFF;
		has_alpha |= colours[3].a() != 0xFF;
	}

	GeomType geom_type = geom::EVNC;
	if (mat && mat->m_diffuse_texture) geom_type |= geom::ETexture;

	// Add a render nugget
	rdr::Material local_mat = mat ? *mat : matmgr.GetMaterial(geom_type);
	SetAlphaRenderStates(local_mat.m_rsb, has_alpha);
	mlock.m_model->SetMaterial(local_mat, EPrimitive::TriangleList, false, v_range, i_range);

	// Update the editable ranges
	mlock.m_vrange.m_begin += v_range->size();
	mlock.m_irange.m_begin += i_range->size();
	return mlock.m_model;
}
pr::rdr::ModelPtr pr::rdr::model::Quad(Renderer& rdr,v4 const* point ,std::size_t num_quads ,Colour32 const* colours ,std::size_t num_colours ,rdr::Material const* mat ,Range* v_range ,Range* i_range)
{
	MLock mlock(rdr.m_mdl_mgr.CreateModel(QuadModelSettings(num_quads)));
	return Quad(mlock, rdr.m_mat_mgr, point, num_quads, colours, num_colours, mat, v_range, i_range);
}
pr::rdr::ModelPtr pr::rdr::model::Quad(MLock& mlock ,MaterialManager& matmgr ,v4 const& centre ,v4 const& forward ,float width ,float height ,Colour32 const* colours ,std::size_t num_colours ,rdr::Material const* mat ,Range* v_range ,Range* i_range)
{
	pr::v4 fwd  = pr::GetNormal3(forward);
	pr::v4 up   = pr::Perpendicular(fwd);
	pr::v4 left = pr::Cross3(up, fwd);
	up   *= height * 0.5f;
	left *= width  * 0.5f;
	pr::v4 pt[4];
	pt[0] = centre - up - left;
	pt[1] = centre - up + left;
	pt[2] = centre + up + left;
	pt[3] = centre + up - left;
	return Quad(mlock, matmgr, pt, 1, colours, num_colours, mat, v_range, i_range);
}
pr::rdr::ModelPtr pr::rdr::model::Quad(Renderer& rdr, v4 const& centre ,v4 const& forward ,float width ,float height ,Colour32 const* colours ,std::size_t num_colours ,rdr::Material const* mat ,Range* v_range ,Range* i_range)
{
	MLock mlock(rdr.m_mdl_mgr.CreateModel(QuadModelSettings(1)));
	return Quad(mlock, rdr.m_mat_mgr, centre, forward, width, height, colours, num_colours, mat, v_range, i_range);
}
	
// Sphere *****************************************************************************************
// Return the model buffer requirements for an array of cylinders
void pr::rdr::model::SphereSize(Range& v_range, Range& i_range, std::size_t divisions)
{
	v_range.set(0, pr::geometry::GeosphereVertCount(value_cast<uint>(divisions)));
	i_range.set(0, pr::geometry::GeosphereFaceCount(value_cast<uint>(divisions)) * 3);
}
	
// Return model settings for creating an array of cylinders
Settings pr::rdr::model::SphereModelSettings(std::size_t divisions)
{
	Range v_range, i_range;
	SphereSize(v_range, i_range, divisions);

	Settings settings;
	settings.m_vertex_type = vf::GetTypeFromGeomType(geom::EVNT);
	settings.m_Vcount      = v_range.size();
	settings.m_Icount      = i_range.size();
	return settings;
}
	
// Generate a sphere
pr::rdr::ModelPtr pr::rdr::model::SphereRxyz(MLock& mlock, MaterialManager& matmgr, float xradius, float yradius, float zradius, v4 const& position, std::size_t divisions, Colour32 colour, rdr::Material const* mat, Range* v_range, Range* i_range)
{
	// Handle optional parameters
	Range local_v_range, local_i_range;
	if (!v_range) v_range = &local_v_range;
	if (!i_range) i_range = &local_i_range;
	SphereSize(*v_range, *i_range, divisions);

	// Shift the ranges into the editable range in the model
	v_range->shift((int)mlock.m_vrange.m_begin);
	i_range->shift((int)mlock.m_irange.m_begin);
	PR_ASSERT(PR_DBG_RDR, mlock.m_vrange.contains(*v_range), FmtS("Insufficient space in model buffer. Additional %d verts required", v_range->size() - mlock.m_vrange.size()));
	PR_ASSERT(PR_DBG_RDR, mlock.m_irange.contains(*i_range), FmtS("Insufficient space in model buffer. Additional %d indices required", v_range->size() - mlock.m_vrange.size()));

	// Generate the geosphere
	pr::Geometry geo_sphere;
	geometry::GenerateGeosphere(geo_sphere, 1.0f, value_cast<uint>(divisions));
	pr::Mesh& geo_sphere_mesh = geo_sphere.m_frame.front().m_mesh;

	v4 point, norm;
	vf::iterator vb   = mlock.m_vlock.m_ptr + v_range->m_begin;
	rdr::Index*  ib   = mlock.m_ilock.m_ptr + i_range->m_begin;
	rdr::Index   base = value_cast<rdr::Index>(v_range->m_begin);
	for (std::size_t v = 0; v != v_range->size(); ++v)
	{
		v4 geo_vert = geo_sphere_mesh.m_vertex[v].m_vertex;
		v2 geo_uv   = geo_sphere_mesh.m_vertex[v].m_tex_vertex;
		point.set(geo_vert.x * xradius, geo_vert.y * yradius, geo_vert.z * zradius, 1.0f);
		norm .set(geo_vert.x / xradius, geo_vert.y / yradius, geo_vert.z / zradius, 0.0f);
		point = position + point;
		Normalise3(norm);
		vb->set(point, norm, colour, geo_uv);
		pr::Encompase(mlock.m_model->m_bbox, point);
		++vb;
	}
	for (std::size_t f = 0, f_end = i_range->size()/3; f != f_end; ++f)
	{
		*ib++ = base + geo_sphere_mesh.m_face[f].m_vert_index[0];
		*ib++ = base + geo_sphere_mesh.m_face[f].m_vert_index[1];
		*ib++ = base + geo_sphere_mesh.m_face[f].m_vert_index[2];
	}

	// Add a render nugget
	rdr::Material local_mat = mat ? *mat : matmgr.GetMaterial(geom::EVNCT);
	SetAlphaRenderStates(local_mat.m_rsb, colour.a() != 0xFF);
	mlock.m_model->SetMaterial(local_mat, EPrimitive::TriangleList, false, v_range, i_range);

	// Update the editable ranges
	mlock.m_vrange.m_begin += v_range->size();
	mlock.m_irange.m_begin += i_range->size();
	return mlock.m_model;
}
pr::rdr::ModelPtr pr::rdr::model::SphereRxyz(Renderer& rdr, float xradius, float yradius, float zradius, v4 const& position, std::size_t divisions, Colour32 colour, rdr::Material const* mat, Range* v_range, Range* i_range)
{
	MLock mlock(rdr.m_mdl_mgr.CreateModel(SphereModelSettings(divisions)));
	return SphereRxyz(mlock, rdr.m_mat_mgr, xradius, yradius, zradius, position, divisions, colour, mat, v_range, i_range);
}
	
// Box *****************************************************************************************
// Return the model buffer requirements for an array of boxes
void pr::rdr::model::BoxSize(Range& v_range, Range& i_range, std::size_t num_boxes)
{
	v_range.set(0, 24 * num_boxes);
	i_range.set(0, 36 * num_boxes);
}
	
// Return model settings for creating an array of boxes
Settings pr::rdr::model::BoxModelSettings(std::size_t num_boxes)
{
	Range v_range, i_range;
	BoxSize(v_range, i_range, num_boxes);

	Settings settings;
	settings.m_vertex_type = vf::GetTypeFromGeomType(geom::EVNC);
	settings.m_Vcount      = v_range.size();
	settings.m_Icount      = i_range.size();
	return settings;
}
	
// Generate boxes from an array of corners
// Point Order:
//  -x, -y, -z
//  -x, +y, -z
//  +x, -y, -z
//  +x, +y, -z
//  +x, -y, +z
//  +x, +y, +z
//  -x, -y, +z
//  -x, +y, +z
pr::rdr::ModelPtr pr::rdr::model::Box(MLock& mlock, MaterialManager& matmgr, v4 const* point, std::size_t num_boxes, m4x4 const& o2w, Colour32 colour, rdr::Material const* mat, Range* v_range, Range* i_range)
{
	// Handle optional parameters
	Range local_v_range, local_i_range;
	if (!v_range) v_range = &local_v_range;
	if (!i_range) i_range = &local_i_range;
	BoxSize(*v_range, *i_range, num_boxes);

	// Shift the ranges into the editable range in the model
	v_range->shift((int)mlock.m_vrange.m_begin);
	i_range->shift((int)mlock.m_irange.m_begin);
	PR_ASSERT(PR_DBG_RDR, mlock.m_vrange.contains(*v_range), FmtS("Insufficient space in model buffer. Additional %d verts required", v_range->size() - mlock.m_vrange.size()));
	PR_ASSERT(PR_DBG_RDR, mlock.m_irange.contains(*i_range), FmtS("Insufficient space in model buffer. Additional %d indices required", v_range->size() - mlock.m_vrange.size()));

	pr::v4 pt[8];
	vf::iterator vb   = mlock.m_vlock.m_ptr + v_range->m_begin;
	rdr::Index*  ib   = mlock.m_ilock.m_ptr + i_range->m_begin;
	rdr::Index   base = value_cast<rdr::Index>(v_range->m_begin);
	for (std::size_t i = 0; i != num_boxes; ++i, point += 8, base += 24)
	{
		for (std::size_t j = 0; j != 8; ++j)
		{
			pt[j] = o2w * point[j];
			pr::Encompase(mlock.m_model->m_bbox, pt[j]);
		}

		// Add the verts for the box
		vb->set(pt[0], GetNormal3IfNonZero(Cross3(pt[1] - pt[0], pt[2] - pt[0])), colour), ++vb;
		vb->set(pt[1], GetNormal3IfNonZero(Cross3(pt[3] - pt[1], pt[0] - pt[1])), colour), ++vb;
		vb->set(pt[2], GetNormal3IfNonZero(Cross3(pt[0] - pt[2], pt[3] - pt[2])), colour), ++vb;
		vb->set(pt[3], GetNormal3IfNonZero(Cross3(pt[2] - pt[3], pt[1] - pt[3])), colour), ++vb;

		vb->set(pt[2], GetNormal3IfNonZero(Cross3(pt[3] - pt[2], pt[4] - pt[2])), colour), ++vb;
		vb->set(pt[3], GetNormal3IfNonZero(Cross3(pt[5] - pt[3], pt[2] - pt[3])), colour), ++vb;
		vb->set(pt[4], GetNormal3IfNonZero(Cross3(pt[2] - pt[4], pt[5] - pt[4])), colour), ++vb;
		vb->set(pt[5], GetNormal3IfNonZero(Cross3(pt[4] - pt[5], pt[3] - pt[5])), colour), ++vb;

		vb->set(pt[4], GetNormal3IfNonZero(Cross3(pt[5] - pt[4], pt[6] - pt[4])), colour), ++vb;
		vb->set(pt[5], GetNormal3IfNonZero(Cross3(pt[7] - pt[5], pt[4] - pt[5])), colour), ++vb;
		vb->set(pt[6], GetNormal3IfNonZero(Cross3(pt[4] - pt[6], pt[7] - pt[6])), colour), ++vb;
		vb->set(pt[7], GetNormal3IfNonZero(Cross3(pt[6] - pt[7], pt[5] - pt[7])), colour), ++vb;

		vb->set(pt[6], GetNormal3IfNonZero(Cross3(pt[7] - pt[6], pt[0] - pt[6])), colour), ++vb;
		vb->set(pt[7], GetNormal3IfNonZero(Cross3(pt[1] - pt[7], pt[6] - pt[7])), colour), ++vb;
		vb->set(pt[0], GetNormal3IfNonZero(Cross3(pt[6] - pt[0], pt[1] - pt[0])), colour), ++vb;
		vb->set(pt[1], GetNormal3IfNonZero(Cross3(pt[0] - pt[1], pt[7] - pt[1])), colour), ++vb;

		vb->set(pt[1], GetNormal3IfNonZero(Cross3(pt[7] - pt[1], pt[3] - pt[1])), colour), ++vb;
		vb->set(pt[3], GetNormal3IfNonZero(Cross3(pt[1] - pt[3], pt[5] - pt[3])), colour), ++vb;
		vb->set(pt[5], GetNormal3IfNonZero(Cross3(pt[3] - pt[5], pt[7] - pt[5])), colour), ++vb;
		vb->set(pt[7], GetNormal3IfNonZero(Cross3(pt[5] - pt[7], pt[1] - pt[7])), colour), ++vb;

		vb->set(pt[0], GetNormal3IfNonZero(Cross3(pt[2] - pt[0], pt[6] - pt[0])), colour), ++vb;
		vb->set(pt[2], GetNormal3IfNonZero(Cross3(pt[4] - pt[2], pt[0] - pt[2])), colour), ++vb;
		vb->set(pt[4], GetNormal3IfNonZero(Cross3(pt[6] - pt[4], pt[2] - pt[4])), colour), ++vb;
		vb->set(pt[6], GetNormal3IfNonZero(Cross3(pt[0] - pt[6], pt[4] - pt[6])), colour), ++vb;

		// Add the box indices
		*ib++ = base +  0;
		*ib++ = base +  1;
		*ib++ = base +  3;
		*ib++ = base +  0;
		*ib++ = base +  3;
		*ib++ = base +  2;

		*ib++ = base +  4;
		*ib++ = base +  5;
		*ib++ = base +  7;
		*ib++ = base +  4;
		*ib++ = base +  7;
		*ib++ = base +  6;

		*ib++ = base +  8;
		*ib++ = base +  9;
		*ib++ = base + 11;
		*ib++ = base +  8;
		*ib++ = base + 11;
		*ib++ = base + 10;

		*ib++ = base + 12;
		*ib++ = base + 13;
		*ib++ = base + 15;
		*ib++ = base + 12;
		*ib++ = base + 15;
		*ib++ = base + 14;

		*ib++ = base + 16;
		*ib++ = base + 19;
		*ib++ = base + 18;
		*ib++ = base + 16;
		*ib++ = base + 18;
		*ib++ = base + 17;

		*ib++ = base + 20;
		*ib++ = base + 21;
		*ib++ = base + 22;
		*ib++ = base + 20;
		*ib++ = base + 22;
		*ib++ = base + 23;
	}
	
	// Add a render nugget
	rdr::Material local_mat = mat ? *mat : matmgr.GetMaterial(geom::EVNC);
	SetAlphaRenderStates(local_mat.m_rsb, colour.a() != 0xFF);
	mlock.m_model->SetMaterial(local_mat, EPrimitive::TriangleList, false, v_range, i_range);

	// Update the editable ranges
	mlock.m_vrange.m_begin += v_range->size();
	mlock.m_irange.m_begin += i_range->size();
	return mlock.m_model;
}
pr::rdr::ModelPtr pr::rdr::model::Box(Renderer& rdr, v4 const* point, std::size_t num_boxes, m4x4 const& o2w, Colour32 colour, rdr::Material const* mat, Range* v_range, Range* i_range)
{
	MLock mlock(rdr.m_mdl_mgr.CreateModel(BoxModelSettings(num_boxes)));
	return Box(mlock, rdr.m_mat_mgr, point, num_boxes, o2w, colour, mat, v_range, i_range);
}
pr::rdr::ModelPtr pr::rdr::model::Box(MLock& mlock, MaterialManager& matmgr, v4 const& dim, m4x4 const& o2w, Colour32 colour, rdr::Material const* mat, Range* v_range, Range* i_range)
{
	v4 point[8];
	v4 d = dim * 0.5f;
	point[0].set(-d.x, -d.y, -d.z, 1.0f);
	point[1].set(-d.x,  d.y, -d.z, 1.0f);
	point[2].set( d.x, -d.y, -d.z, 1.0f);
	point[3].set( d.x,  d.y, -d.z, 1.0f);
	point[4].set( d.x, -d.y,  d.z, 1.0f);
	point[5].set( d.x,  d.y,  d.z, 1.0f);
	point[6].set(-d.x, -d.y,  d.z, 1.0f);
	point[7].set(-d.x,  d.y,  d.z, 1.0f);
	return Box(mlock, matmgr, point, 1, o2w, colour, mat, v_range, i_range);
}
pr::rdr::ModelPtr pr::rdr::model::Box(Renderer& rdr, v4 const& dim, m4x4 const& o2w, Colour32 colour, rdr::Material const* mat, Range* v_range, Range* i_range)
{
	MLock mlock(rdr.m_mdl_mgr.CreateModel(BoxModelSettings(1)));
	return Box(mlock, rdr.m_mat_mgr, dim, o2w, colour, mat, v_range, i_range);
}
pr::rdr::ModelPtr pr::rdr::model::BoxList(MLock& mlock, MaterialManager& matmgr, pr::v4 const& dim, v4 const* positions, std::size_t num_boxes, Colour32 colour, rdr::Material const* mat, Range* v_range, Range* i_range)
{
	v4 const* pos = positions;
	pr::Array<pr::v4> points(8*num_boxes);
	v4* pt = &points[0];
	for (std::size_t i = 0; i != num_boxes; ++i, ++pos)
	{
		pt->set(pos->x - dim.x, pos->y - dim.y, pos->z - dim.z, 1.0f), ++pt;
		pt->set(pos->x - dim.x, pos->y + dim.y, pos->z - dim.z, 1.0f), ++pt;
		pt->set(pos->x + dim.x, pos->y - dim.y, pos->z - dim.z, 1.0f), ++pt;
		pt->set(pos->x + dim.x, pos->y + dim.y, pos->z - dim.z, 1.0f), ++pt;
		pt->set(pos->x + dim.x, pos->y - dim.y, pos->z + dim.z, 1.0f), ++pt;
		pt->set(pos->x + dim.x, pos->y + dim.y, pos->z + dim.z, 1.0f), ++pt;
		pt->set(pos->x - dim.x, pos->y - dim.y, pos->z + dim.z, 1.0f), ++pt;
		pt->set(pos->x - dim.x, pos->y + dim.y, pos->z + dim.z, 1.0f), ++pt;
	}
	return Box(mlock, matmgr, &points[0], num_boxes, pr::m4x4Identity, colour, mat, v_range, i_range);
}
pr::rdr::ModelPtr pr::rdr::model::BoxList(Renderer& rdr, pr::v4 const& dim, v4 const* positions, std::size_t num_boxes, Colour32 colour, rdr::Material const* mat, Range* v_range, Range* i_range)
{
	MLock mlock(rdr.m_mdl_mgr.CreateModel(BoxModelSettings(num_boxes)));
	return BoxList(mlock, rdr.m_mat_mgr, dim, positions, num_boxes, colour, mat, v_range, i_range);
}
	
// Cone *****************************************************************************************
void pr::rdr::model::ConeSize(Range& v_range, Range& i_range, std::size_t layers, std::size_t wedges)
{
	PR_ASSERT(PR_DBG_RDR, layers >= 1, "");
	PR_ASSERT(PR_DBG_RDR, wedges >= 3, "");

	v_range.set(0, 2 + (wedges + 1) * (layers + 3));
	i_range.set(0, 6 * (wedges + 0) * (layers + 1));
}
	
Settings pr::rdr::model::ConeModelSettings(std::size_t layers, std::size_t wedges)
{
	Range v_range, i_range;
	ConeSize(v_range ,i_range ,layers ,wedges);

	Settings settings;
	settings.m_vertex_type = vf::GetTypeFromGeomType(geom::EVNC);
	settings.m_Vcount      = v_range.size();
	settings.m_Icount      = i_range.size();
	return settings;
}
	
pr::rdr::ModelPtr pr::rdr::model::Cone(MLock& mlock ,MaterialManager& matmgr ,float height ,float radius0 ,float radius1 ,float xscale ,float yscale ,m4x4 const& o2w ,std::size_t layers ,std::size_t wedges ,Colour32 colour ,rdr::Material const* mat ,Range* v_range ,Range* i_range)
{
	// Handle optional parameters
	Range local_v_range, local_i_range;
	if (!v_range) v_range = &local_v_range;
	if (!i_range) i_range = &local_i_range;
	ConeSize(*v_range ,*i_range ,layers ,wedges);
	
	// Shift the ranges into the editable range in the model
	v_range->shift((int)mlock.m_vrange.m_begin);
	i_range->shift((int)mlock.m_irange.m_begin);
	PR_ASSERT(PR_DBG_RDR, mlock.m_vrange.contains(*v_range), FmtS("Insufficient space in model buffer. Additional %d verts required", v_range->size() - mlock.m_vrange.size()));
	PR_ASSERT(PR_DBG_RDR, mlock.m_irange.contains(*i_range), FmtS("Insufficient space in model buffer. Additional %d indices required", v_range->size() - mlock.m_vrange.size()));
	
	vf::iterator vb   = mlock.m_vlock.m_ptr + v_range->m_begin;
	rdr::Index*  ib   = mlock.m_ilock.m_ptr + i_range->m_begin;
	rdr::Index   base = value_cast<rdr::Index>(v_range->m_begin);
	
	float z = -height*0.5f, dz = height/layers;
	float da = 2.0f * maths::tau_by_2 / wedges;
	
	// Bottom face
	v4 point = o2w * v4::make(0, 0, z, 1.0f);
	v4 norm  = o2w * -v4ZAxis;
	v2 uv    = v2::make(0.5f, 1.0f);
	vb->set(point, norm, colour, uv); ++vb;
	pr::Encompase(mlock.m_model->m_bbox, point);
	for (std::size_t w = 0; w <= wedges; ++w)
	{
		float a = da*w;
		point = o2w * v4::make(Cos(a)*radius0*xscale ,Sin(a)*radius0*yscale ,z ,1.0f);
		vb->set(point, norm, colour, uv); ++vb;
		pr::Encompase(mlock.m_model->m_bbox, point);
	}

	// The walls
	for (std::size_t l = 0; l <= layers; ++l)
	{
		float r = ((layers - l)*radius0 + l*radius1) / layers;
		float nz = radius0 - radius1;
		for (std::size_t w = 0; w <= wedges; ++w)
		{
			float a = da*w + (l%2)*da*0.5f;
			point = o2w * v4::make(Cos(a)*r*xscale ,Sin(a)*r*yscale ,z ,1.0f);
			norm  = o2w * GetNormal3(v4::make(height*Cos(a+da*0.5f)/xscale ,height*Sin(a+da*0.5f)/yscale ,nz ,0.0f));
			uv    = v2::make(a / maths::tau, 1.0f - (z + height*0.5f) / height);
			vb->set(point, norm, colour, uv); ++vb;
			pr::Encompase(mlock.m_model->m_bbox, point);
		}
		z += dz;
	}

	// Top face
	z = height / 2.0f;
	norm = o2w * v4ZAxis;
	uv   = v2::make(0.5, 0.0f);
	for (std::size_t w = 0; w <= wedges; ++w)
	{
		float a = da*w + (layers%2)*da*0.5f;
		point = o2w * v4::make(Cos(a)*radius1*xscale ,Sin(a)*radius1*yscale ,z ,1.0f);
		vb->set(point, norm, colour, uv); ++vb;
		pr::Encompase(mlock.m_model->m_bbox, point);
	}
	point = o2w * v4::make(0 ,0 ,z ,1.0f);
	vb->set(point, norm, colour, uv); ++vb;
	pr::Encompase(mlock.m_model->m_bbox, point);
	
	PR_ASSERT(PR_DBG_RDR, (std::size_t)(vb - mlock.m_vlock.m_ptr) == v_range->m_end, "");
	
	rdr::Index num_wedges = value_cast<rdr::Index>(wedges), num_layers = value_cast<rdr::Index>(layers);
	rdr::Index verts_per_layer = num_wedges + 1;
	rdr::Index last = 1 + (3+num_layers) * verts_per_layer;
	
	// Create the bottom face
	for (rdr::Index w = 0; w != num_wedges; ++w)
	{
		*ib++ = base + 0;
		*ib++ = base + w + 1;
		*ib++ = base + w + 2;
	}
	
	// Create the walls
	for (rdr::Index l = 1; l <= num_layers; ++l)
	{
		for (rdr::Index w = 0; w != num_wedges; ++w)
		{
			if (l%2)
			{
				*ib++ = base + w + 1 + (l+1)*verts_per_layer;
				*ib++ = base + w + 1 + (l+0)*verts_per_layer;
				*ib++ = base + w + 2 + (l+0)*verts_per_layer;
				*ib++ = base + w + 1 + (l+1)*verts_per_layer;
				*ib++ = base + w + 2 + (l+0)*verts_per_layer;
				*ib++ = base + w + 2 + (l+1)*verts_per_layer;
			}
			else
			{
				*ib++ = base + w + 1 + (l+1)*verts_per_layer;
				*ib++ = base + w + 1 + (l+0)*verts_per_layer;
				*ib++ = base + w + 2 + (l+1)*verts_per_layer;
				*ib++ = base + w + 2 + (l+1)*verts_per_layer;
				*ib++ = base + w + 1 + (l+0)*verts_per_layer;
				*ib++ = base + w + 2 + (l+0)*verts_per_layer;
			}
		}
	}
	
	// Create the top face
	for (rdr::Index w = 0; w != num_wedges; ++w)
	{
		*ib++ = base + last;
		*ib++ = base + last - verts_per_layer + w + 1;
		*ib++ = base + last - verts_per_layer + w;
	}
	
	PR_ASSERT(PR_DBG_RDR, (std::size_t)(ib - mlock.m_ilock.m_ptr) == i_range->m_end, "");
	
	// Add a render nugget
	rdr::Material local_mat = mat ? *mat : matmgr.GetMaterial(geom::EVNCT);
	SetAlphaRenderStates(local_mat.m_rsb, colour.a() != 0xFF);
	mlock.m_model->SetMaterial(local_mat, EPrimitive::TriangleList, false, v_range, i_range);

	// Update the editable ranges
	mlock.m_vrange.m_begin += v_range->size();
	mlock.m_irange.m_begin += i_range->size();
	return mlock.m_model;
}
pr::rdr::ModelPtr pr::rdr::model::Cone(Renderer& rdr ,float height ,float radius0 ,float radius1 ,float xscale ,float yscale ,m4x4 const& o2w ,std::size_t layers ,std::size_t wedges ,Colour32 colour ,rdr::Material const* mat ,Range* v_range ,Range* i_range)
{
	MLock mlock(rdr.m_mdl_mgr.CreateModel(ConeModelSettings(layers ,wedges)));
	return Cone(mlock ,rdr.m_mat_mgr ,height ,radius0 ,radius1 ,xscale ,yscale ,o2w ,layers ,wedges ,colour ,mat ,v_range ,i_range);
}
pr::rdr::ModelPtr pr::rdr::model::CylinderHRxy(MLock& mlock, MaterialManager& matmgr ,float height, float xradius, float yradius, m4x4 const& o2w, std::size_t layers, std::size_t wedges, Colour32 colour, rdr::Material const* mat, Range* v_range, Range* i_range)
{
	return Cone(mlock ,matmgr ,height ,1.0f ,1.0f ,xradius ,yradius ,o2w ,layers ,wedges ,colour ,mat ,v_range ,i_range);
}
pr::rdr::ModelPtr pr::rdr::model::CylinderHRxy(Renderer& rdr, float height, float xradius, float yradius, m4x4 const& o2w, std::size_t layers, std::size_t wedges, Colour32 colour, rdr::Material const* mat, Range* v_range, Range* i_range)
{
	return Cone(rdr, height ,1.0f ,1.0f ,xradius ,yradius ,o2w ,layers ,wedges ,colour ,mat ,v_range ,i_range);
}
	
// Capsule *****************************************************************************************
// Return the model buffer requirements for a capsule
void pr::rdr::model::CapsuleSize(Range& v_range, Range& i_range, std::size_t divisions)
{
	PR_ASSERT(PR_DBG_RDR, divisions >= 1, "");

	v_range.set(0, pr::geometry::GeosphereVertCount(value_cast<uint>(divisions)));
	i_range.set(0, pr::geometry::GeosphereFaceCount(value_cast<uint>(divisions)) * 3);
}
	
// Return model settings for creating a capsule
Settings pr::rdr::model::CapsuleModelSettings(std::size_t divisions)
{
	Range v_range, i_range;
	CapsuleSize(v_range, i_range, divisions);

	Settings settings;
	settings.m_vertex_type = vf::GetTypeFromGeomType(geom::EVNC);
	settings.m_Vcount      = v_range.size();
	settings.m_Icount      = i_range.size();
	return settings;
}
	
// Generate a capsule
pr::rdr::ModelPtr pr::rdr::model::CapsuleHRxy(MLock& mlock, MaterialManager& matmgr, float height, float xradius, float yradius, m4x4 const& o2w, std::size_t divisions, Colour32 colour, rdr::Material const* mat, Range* v_range, Range* i_range)
{
	// Handle optional parameters
	Range local_v_range, local_i_range;
	if (!v_range) v_range = &local_v_range;
	if (!i_range) i_range = &local_i_range;
	CapsuleSize(*v_range, *i_range, divisions);

	// Shift the ranges into the editable range in the model
	v_range->shift((int)mlock.m_vrange.m_begin);
	i_range->shift((int)mlock.m_irange.m_begin);
	PR_ASSERT(PR_DBG_RDR, mlock.m_vrange.contains(*v_range), FmtS("Insufficient space in model buffer. Additional %d verts required", v_range->size() - mlock.m_vrange.size()));
	PR_ASSERT(PR_DBG_RDR, mlock.m_irange.contains(*i_range), FmtS("Insufficient space in model buffer. Additional %d indices required", v_range->size() - mlock.m_vrange.size()));

	// Generate a geosphere
	pr::Geometry geo_sphere;
	geometry::GenerateGeosphere(geo_sphere, 1.0f, value_cast<uint>(divisions));
	pr::Mesh& geo_sphere_mesh = geo_sphere.m_frame.front().m_mesh;

	vf::iterator vb   = mlock.m_vlock.m_ptr + v_range->m_begin;
	rdr::Index*  ib   = mlock.m_ilock.m_ptr + i_range->m_begin;
	rdr::Index   base = value_cast<rdr::Index>(v_range->m_begin);

	// Todo:
	(void)geo_sphere_mesh;
	(void)vb;
	(void)ib;
	(void)base;
	(void)o2w;
	(void)yradius;
	(void)xradius;
	(void)height;

	// Add a render nugget
	rdr::Material local_mat = mat ? *mat : matmgr.GetMaterial(geom::EVNC);
	SetAlphaRenderStates(local_mat.m_rsb, colour.a() != 0xFF);
	mlock.m_model->SetMaterial(local_mat, EPrimitive::TriangleList, false, v_range, i_range);

	// Update the editable ranges
	mlock.m_vrange.m_begin += v_range->size();
	mlock.m_irange.m_begin += i_range->size();
	return mlock.m_model;
}
pr::rdr::ModelPtr pr::rdr::model::CapsuleHRxy(Renderer& rdr, float height, float xradius, float yradius, m4x4 const& o2w, std::size_t divisions, Colour32 colour, rdr::Material const* mat, Range* v_range, Range* i_range)
{
	MLock mlock(rdr.m_mdl_mgr.CreateModel(CapsuleModelSettings(divisions)));
	return CapsuleHRxy(mlock, rdr.m_mat_mgr, height, xradius, yradius, o2w, divisions, colour, mat, v_range, i_range);
}
	
// Mesh *****************************************************************************************
// Return the model buffer requirements of a mesh
void pr::rdr::model::MeshSize(Range& v_range, Range& i_range, std::size_t num_verts, std::size_t num_indices)
{
	v_range.set(0, num_verts);
	i_range.set(0, num_indices);
}
	
// Return model settings for creating a mesh
Settings pr::rdr::model::MeshModelSettings(std::size_t num_verts, std::size_t num_indices, pr::GeomType geom_type)
{
	Range v_range, i_range;
	MeshSize(v_range, i_range, num_verts, num_indices);

	Settings settings;
	settings.m_vertex_type = vf::GetTypeFromGeomType(geom_type);
	settings.m_Vcount      = v_range.size();
	settings.m_Icount      = i_range.size();
	return settings;
}
	
// Add a mesh to a model.
// 'normals', 'colours', 'tex_coords' can all be 0
// Remember you can call "model->GenerateNormals()" to generate normals.
pr::rdr::ModelPtr pr::rdr::model::Mesh(
	MLock& mlock,
	MaterialManager& matmgr,
	EPrimitive::Type prim_type,
	pr::GeomType geom_type,
	std::size_t num_indices,
	std::size_t num_verts,
	rdr::Index const* indices,
	v4 const* verts,
	v4 const* normals,
	Colour32 const* colours,
	v2 const* tex_coords,
	m4x4 const& o2w,
	Colour32 colour,
	rdr::Material const* mat,
	Range* v_range,
	Range* i_range)
{
	// Handle optional parameters
	Range local_v_range, local_i_range;
	if (!v_range) v_range = &local_v_range;
	if (!i_range) i_range = &local_i_range;
	MeshSize(*v_range, *i_range, num_verts, num_indices);

	// Shift the ranges into the editable range in the model
	v_range->shift((int)mlock.m_vrange.m_begin);
	i_range->shift((int)mlock.m_irange.m_begin);
	PR_ASSERT(PR_DBG_RDR, mlock.m_vrange.contains(*v_range), FmtS("Insufficient space in model buffer. Additional %d verts required", v_range->size() - mlock.m_vrange.size()));
	PR_ASSERT(PR_DBG_RDR, mlock.m_irange.contains(*i_range), FmtS("Insufficient space in model buffer. Additional %d indices required", v_range->size() - mlock.m_vrange.size()));

	bool has_alpha = false;
	vf::iterator vb   = mlock.m_vlock.m_ptr + v_range->m_begin;
	rdr::Index*  ib   = mlock.m_ilock.m_ptr + i_range->m_begin;
	rdr::Index   base = value_cast<rdr::Index>(v_range->m_begin);
	for (std::size_t i = 0; i != num_verts; ++i)
	{
		v4 const*       v = verts++;
		v4 const*       n = normals    ? normals++    : &v4YAxis;
		Colour32 const* c = colours    ? colours++    : &colour;
		v2 const*       t = tex_coords ? tex_coords++ : &v2Zero;
		v4 point = o2w * *v;
		v4 norm  = o2w * *n;
		vb->set(point, norm, *c, *t); ++vb;

		// Grow the bounding box
		pr::Encompase(mlock.m_model->m_bbox, point);

		// Look for alpha
		has_alpha |= c->a() != 0xFF;
	}
	for (std::size_t i = 0; i != num_indices; ++i)
	{
		*ib++ = base + *indices++;
	}

	// Add a render nugget
	rdr::Material local_mat = mat ? *mat : matmgr.GetMaterial(geom_type);
	SetAlphaRenderStates(local_mat.m_rsb, has_alpha);
	mlock.m_model->SetMaterial(local_mat, prim_type, false, v_range, i_range);

	// Update the editable ranges
	mlock.m_vrange.m_begin += v_range->size();
	mlock.m_irange.m_begin += i_range->size();
	return mlock.m_model;
}
pr::rdr::ModelPtr pr::rdr::model::Mesh(Renderer& rdr, EPrimitive::Type prim_type, pr::GeomType geom_type, std::size_t num_indices, std::size_t num_verts, rdr::Index const* indices, v4 const* verts, v4 const* normals, Colour32 const* colours, v2 const* tex_coords, m4x4 const& o2w, Colour32 colour, rdr::Material const* mat, Range* v_range, Range* i_range)
{
	MLock mlock(rdr.m_mdl_mgr.CreateModel(MeshModelSettings(num_verts, num_indices, geom_type)));
	return Mesh(mlock, rdr.m_mat_mgr, prim_type, geom_type, num_indices, num_verts, indices, verts, normals, colours, tex_coords, o2w, colour, mat, v_range, i_range);
}
	
