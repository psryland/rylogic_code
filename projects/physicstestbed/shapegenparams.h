//******************************************
// Shape Gen Params
//******************************************
#pragma once

enum EShapeGen
{
	EShapeGen_World,
	EShapeGen_Local
};

struct ShapeGenParams
{
	ShapeGenParams()
	:m_sph_min_radius(0.1f)
	,m_sph_max_radius(2.0f)
	,m_cyl_min_radius(0.1f)
	,m_cyl_max_radius(2.0f)
	,m_cyl_min_height(0.1f)
	,m_cyl_max_height(2.0f)
	,m_box_min_dim(pr::v4::make(0.2f, 0.2f, 0.2f, 0.0f))
	,m_box_max_dim(pr::v4::make(1.0f, 1.0f, 1.0f, 0.0f))
	,m_ply_vert_count(20)
	,m_ply_min_dim(pr::v4::make(-1.0f, -1.0f, -1.0f, 0.0f))
	,m_ply_max_dim(pr::v4::make( 1.0f,  1.0f,  1.0f, 0.0f))
	{}
	float m_sph_min_radius;
	float m_sph_max_radius;
	float m_cyl_min_radius;
	float m_cyl_max_radius;
	float m_cyl_min_height;
	float m_cyl_max_height;
	pr::v4	m_box_min_dim;
	pr::v4	m_box_max_dim;
	int		m_ply_vert_count;
	pr::v4	m_ply_min_dim;
	pr::v4	m_ply_max_dim;
};

inline ShapeGenParams& ShapeGen()
{
	static ShapeGenParams params;
	return params;
}