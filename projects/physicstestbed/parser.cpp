//******************************************
// Parser
//******************************************

#include "PhysicsTestbed/Stdafx.h"
#include "PhysicsTestbed/Parser.h"
#include "PhysicsTestbed/ParseOutput.h"
#include "PhysicsTestbed/ShapeGenParams.h"
#include "PhysicsTestbed/PhysicsEngine.h"
#include "pr/linedrawer/ldr_helper.h"

using namespace pr;
using namespace parse;

// Parse source data
bool Parser::Load(const char* filename)
{
	try	{  ScriptLoader loader(filename); return Load(loader); }
	catch( script::Exception const& )
	{ return false; }
}
bool Parser::Load(const char* src, std::size_t len)
{
	try	{ ScriptLoader loader(src, len); return Load(loader); }
	catch( script::Exception const& )
	{ return false; }
}

bool Parser::Load(ScriptLoader& loader)
{
	bool success = true;
	try
	{
		while( Parse(loader) != EObjectType_None ) {}
	}
	catch (const script::Exception& e)
	{
		MessageBox(0, Fmt(
			"Source script parser error: %s\n"
			"Near: '%.20s'"
			,ToString(static_cast<script::EResult>(e.m_value)).c_str()
			,loader.GetSourceStringAt()).c_str(), "Source Error", MB_ICONEXCLAMATION|MB_OK);
		success = false;
	}
	return success;
}

// Parses an object. Returns the type of object parsed and its index in the container it was added to
EObjectType Parser::Parse(ScriptLoader& loader, std::string const& keyword)
{
	if( str::EqualNoCase(keyword, "Position"			) )	{ ParseV4(loader, 1.0f);					return EObjectType_Position;		}
	if( str::EqualNoCase(keyword, "RandomPosition"		) )	{ ParseRandomV4(loader, 1.0f);				return EObjectType_Position;		}
	if( str::EqualNoCase(keyword, "Direction"			) )	{ ParseV4(loader, 0.0f);					return EObjectType_Direction;		}
	if( str::EqualNoCase(keyword, "RandomDirection"		) )	{ ParseRandomDirection(loader);				return EObjectType_Direction;		}
	if( str::EqualNoCase(keyword, "Transform"			) )	{ ParseTransform(loader);					return EObjectType_Transform;		}
	if( str::EqualNoCase(keyword, "RandomTransform"		) )	{ ParseRandomTransform(loader);				return EObjectType_Transform;		}
	if( str::EqualNoCase(keyword, "EulerPos"			) ) { ParseEulerPos(loader);					return EObjectType_Transform;		}
	if( str::EqualNoCase(keyword, "Velocity"			) )	{ ParseV4(loader, 0.0f);					return EObjectType_Velocity;		}
	if( str::EqualNoCase(keyword, "RandomVelocity"		) )	{ ParseRandomV4(loader, 0.0f);				return EObjectType_Velocity;		}
	if( str::EqualNoCase(keyword, "AngVelocity"			) )	{ ParseV4(loader, 0.0f);					return EObjectType_AngVelocity;		}
	if( str::EqualNoCase(keyword, "RandomAngVelocity"	) )	{ ParseRandomV4(loader, 0.0f);				return EObjectType_AngVelocity;		}
	if( str::EqualNoCase(keyword, "Gravity"				) )	{ ParseV4(loader, 0.0f);					return EObjectType_Gravity;			}
	if( str::EqualNoCase(keyword, "Mass"				) ) { loader.ExtractFloat(m_value);				return EObjectType_Mass;			} 
	if( str::EqualNoCase(keyword, "RandomGravity"		) )	{ ParseRandomV4(loader, 0.0f);				return EObjectType_Gravity;			}
	if( str::EqualNoCase(keyword, "Name"				) )	{ loader.ExtractString(m_str);				return EObjectType_Name;			}
	if( str::EqualNoCase(keyword, "ByName"				) )	{											return EObjectType_ByName;			}
	if( str::EqualNoCase(keyword, "Colour"				) )	{ ParseColour(loader);						return EObjectType_Colour;			}
	if( str::EqualNoCase(keyword, "RandomColour"		) ) { ParseRandomColour(loader);				return EObjectType_Colour;			}
	if( str::EqualNoCase(keyword, "DisableRender"		) )	{											return EObjectType_DisableRender;	}
	if( str::EqualNoCase(keyword, "Stationary"			) ) {											return EObjectType_Stationary;		}
	if( str::EqualNoCase(keyword, "Gfx"					) )	{ ParseGfx(loader);							return EObjectType_Gfx;				}
	if( str::EqualNoCase(keyword, "Terrain"				) ) { ParseTerrain(loader);						return EObjectType_Terrain;			}
	if( str::EqualNoCase(keyword, "Material"			) )	{ ParseMaterial(loader);					return EObjectType_Material;		}
	if( str::EqualNoCase(keyword, "GravityField"		) ) { ParseGravityField(loader);				return EObjectType_GravityField;	}
	if( str::EqualNoCase(keyword, "Drag"				) )	{ ParseDrag(loader);						return EObjectType_Drag;			}
	if( str::EqualNoCase(keyword, "Model"				) )	{ ParseModel(loader);						return EObjectType_Model;			}
	if( str::EqualNoCase(keyword, "ModelByName"			) )	{ ParseModelByName(loader);					return EObjectType_ModelByName;		}
	if( str::EqualNoCase(keyword, "Deformable"			) ) { ParseDeformable(loader);					return EObjectType_Deformable;		}
	if( str::EqualNoCase(keyword, "DeformableByName"	) )	{ ParseDeformableByName(loader);			return EObjectType_DeformableByName;}
	if( str::EqualNoCase(keyword, "StaticObject"		) )	{ ParseStaticObject(loader);				return EObjectType_StaticObject;	}
	if( str::EqualNoCase(keyword, "PhysicsObject"		) )	{ ParsePhysicsObject(loader);				return EObjectType_PhysicsObject;	}
	if( str::EqualNoCase(keyword, "PhysicsObjectByName"	) )	{ ParsePhysObjByName(loader);				return EObjectType_PhysObjByName;	}
	if( str::EqualNoCase(keyword, "Multibody"			) )	{ ParseMultibody(loader);					return EObjectType_Multibody;		}
	return EObjectType_Unknown;
}
EObjectType Parser::Parse(ScriptLoader& loader)
{
	std::string keyword;
	if( !loader.GetKeyword(keyword) ) return EObjectType_None;
	return Parse(loader, keyword);
}

// Parse a position
void Parser::ParseV4(ScriptLoader& loader, float w)
{
	loader.FindSectionStart();
	loader.ExtractVector3(m_vec, w);
	loader.FindSectionEnd();
}

// Parse a random position
void Parser::ParseRandomV4(ScriptLoader& loader, float w)
{
	v4 min_pos, max_pos;
	loader.FindSectionStart();
	loader.ExtractVector3(min_pos, 1.0f);
	loader.ExtractVector3(max_pos, 1.0f);
	loader.FindSectionEnd();
	m_vec.set(
		FRand(min_pos.x, max_pos.x),
		FRand(min_pos.y, max_pos.y),
		FRand(min_pos.z, max_pos.z),
		w);
}

// Parse a random direction
void Parser::ParseRandomDirection(ScriptLoader&)
{
	m_vec = v4RandomNormal3(0.0f);
}

// Parse a o2w transform
void Parser::ParseTransform(ScriptLoader& loader)
{
	loader.FindSectionStart();
	loader.Extractm4x4(m_mat);
	loader.FindSectionEnd();
}

// Parse a random transform
void Parser::ParseRandomTransform(ScriptLoader& loader)
{
	v4 centre;
	float range;
	loader.FindSectionStart();
	loader.ExtractVector3(centre, 1.0f);
	loader.ExtractFloat(range);
	loader.FindSectionEnd();
	m_mat = m4x4Random(centre, range);
}

// Parse euler angles + position
void Parser::ParseEulerPos(pr::ScriptLoader& loader)
{
	float pitch, yaw, roll;
	v4 position;
	loader.FindSectionStart();
	loader.ExtractFloat(pitch);
	loader.ExtractFloat(yaw);
	loader.ExtractFloat(roll);
	loader.ExtractVector3(position, 1.0f);
	loader.FindSectionEnd();
	m_mat.set(DegreesToRadians(pitch), DegreesToRadians(yaw), DegreesToRadians(roll), v4Origin);
	m_mat.pos = position;
}

// Parse a colour
void Parser::ParseColour(ScriptLoader& loader)
{
	loader.FindSectionStart();
	loader.ExtractUInt(m_colour.m_aarrggbb, 16);
	loader.FindSectionEnd();
}

// Parse a random colour
void Parser::ParseRandomColour(ScriptLoader&)
{
	m_colour.m_aarrggbb = Colour32RandomRGB();
}

// Parse a non-physical graphics objects
void Parser::ParseGfx(ScriptLoader& loader)
{
	parse::Gfx gfx;
	loader.FindSectionStart();
	gfx.m_ldr_str = loader.CopySection();
	//loader.ExtractString(gfx.m_ldr_str);
	loader.FindSectionEnd();
	m_output.m_graphics.push_back(gfx);
}

// Parse a terrain description
void Parser::ParseTerrain(pr::ScriptLoader& loader)
{
	parse::Terrain terrain;
	loader.FindSectionStart();
	std::string keyword;
	while( loader.GetKeyword(keyword) )
	{
		if( str::EqualNoCase(keyword, "Type" ) )
		{
			terrain.m_type = Terrain::EType_None;
			std::string type; loader.ExtractString(type);
			if     ( pr::str::EqualNoCase(type, "Reflections2D") ) terrain.m_type = Terrain::EType_Reflections_2D;
			else if( pr::str::EqualNoCase(type, "Reflections3D") ) terrain.m_type = Terrain::EType_Reflections_3D;
		}
		else if( str::EqualNoCase(keyword, "XFile"	) )
		{
			std::string xfile_name;
			loader.FindSectionStart();				
			loader.ExtractString(xfile_name);
			loader.FindSectionEnd();
			terrain.m_ldr_str = pr::Fmt("*File terrain_xfile FF00A000 {\"%s\" *GenerateNormals }", xfile_name.c_str());
		}
		else if( str::EqualNoCase(keyword, "Gfx" ) )
		{
			loader.FindSectionStart();
			terrain.m_ldr_str = loader.CopySection();
			loader.FindSectionEnd();			
		}
		else if( str::EqualNoCase(keyword, "Data"	) )
		{
			loader.FindSectionStart();				
			loader.ExtractString(terrain.m_data);
			loader.FindSectionEnd();
		}
	}
	loader.FindSectionEnd();
	m_output.m_terrain.push_back(terrain);
}

void Parser::ParseMaterial(ScriptLoader& loader)
{
	parse::Material material;
	loader.FindSectionStart();
	std::string keyword;
	while( loader.GetKeyword(keyword) )
	{
		if	   ( str::EqualNoCase(keyword, "Density"				) ) { loader.ExtractFloat(material.m_density);					}
		else if( str::EqualNoCase(keyword, "StaticFriction"			) ) { loader.ExtractFloat(material.m_static_friction);			}
		else if( str::EqualNoCase(keyword, "DynamicFriction"		) ) { loader.ExtractFloat(material.m_dynamic_friction);			}
		else if( str::EqualNoCase(keyword, "RollingFriction"		) )	{ loader.ExtractFloat(material.m_rolling_friction);			}
		else if( str::EqualNoCase(keyword, "Elasticity"				) )	{ loader.ExtractFloat(material.m_elasticity);				}
		else if( str::EqualNoCase(keyword, "TangentialElasticity"	) )	{ loader.ExtractFloat(material.m_tangential_elasiticity);	}
		else if( str::EqualNoCase(keyword, "TortionalElasticity"	) )	{ loader.ExtractFloat(material.m_tortional_elasticity);		}
	}
	loader.FindSectionEnd();
	m_output.m_material = material;
}

// Parse a gravity description
void Parser::ParseGravityField(ScriptLoader& loader)
{
	parse::Gravity gravity;
	loader.FindSectionStart();
	std::string keyword;
	while( loader.GetKeyword(keyword) )
	{
		switch( Parse(loader, keyword) )
		{
		default: break;
		case EObjectType_Direction:	gravity.m_direction = m_vec; break;
		case EObjectType_Position:	gravity.m_centre	= m_vec; break;
		case EObjectType_Unknown:
			if	   ( str::EqualNoCase(keyword, "Directional"	) )	{ gravity.m_type = parse::Gravity::EType_Directional; }
			else if( str::EqualNoCase(keyword, "Radial"			) )	{ gravity.m_type = parse::Gravity::EType_Radial; }
			else if( str::EqualNoCase(keyword, "Strength"		) )	{ loader.ExtractFloat(gravity.m_strength); }
		}
	}
	loader.FindSectionEnd();
	if( gravity.m_type == parse::Gravity::EType_Directional ) Normalise3(gravity.m_direction);
	m_output.m_gravity.push_back(gravity);
}

// Parse a drag factor
void Parser::ParseDrag(ScriptLoader& loader)
{
	loader.FindSectionStart();
	loader.ExtractFloat(m_output.m_drag);
	loader.FindSectionEnd();
}

// Parse a physics collision model
void Parser::ParseModel(ScriptLoader& loader)
{
	parse::Model model;
	loader.FindSectionStart();
	std::string keyword;
	while( loader.GetKeyword(keyword) )
	{
		switch( Parse(loader, keyword) )
		{
		default: break;
		case EObjectType_Name:		model.m_name				= m_str; break;
		case EObjectType_Transform: model.m_model_to_world		= m_mat; break;
		case EObjectType_Position:	model.m_model_to_world.pos	= m_vec; break;
		case EObjectType_Unknown:
			{
				bool prim_added = true;
				if	   ( str::EqualNoCase(keyword, "Box"		) )	{ ParseBox(loader);			model.m_prim.push_back(m_prim); }
				else if( str::EqualNoCase(keyword, "Cylinder"	) )	{ ParseCylinder(loader);	model.m_prim.push_back(m_prim); }
				else if( str::EqualNoCase(keyword, "Sphere"		) )	{ ParseSphere(loader);		model.m_prim.push_back(m_prim); }
				else if( str::EqualNoCase(keyword, "Polytope"	) )	{ ParsePolytope(loader);	model.m_prim.push_back(m_prim); }
				else if( str::EqualNoCase(keyword, "Triangle"	) )	{ ParseTriangle(loader);	model.m_prim.push_back(m_prim); }
				else prim_added = false;
				if( prim_added ) Encompase(model.m_bbox, m_prim.m_prim_to_model * m_prim.m_bbox);

				if( str::EqualNoCase(keyword, "Skeleton"		) )	{ ParseSkeleton(loader, model.m_skel); }
			}break;
		}
	}
	loader.FindSectionEnd();

	m_index = 0xFFFFFFFF;
	if( model.has_data() )
	{
		m_index = m_output.m_models.size();
		m_output.m_models.push_back(model);
	}
}

// Parse optional keywords common to all primitives
bool Parser::ParsePrimCommon(ScriptLoader& loader, std::string const& keyword)
{	
	switch( Parse(loader, keyword) )
	{
	default: break;
	case EObjectType_Transform: m_prim.m_prim_to_model		= m_mat; break;
	case EObjectType_Position:	m_prim.m_prim_to_model.pos	= m_vec; break;
	case EObjectType_Colour:	m_prim.m_colour				= m_colour; break;
	case EObjectType_Unknown:
		{
			if( str::EqualNoCase(keyword, "future") )
			{}
			else return false;
		}break;
	}
	return true;
}

// Parse the description of a box
void Parser::ParseBox(ScriptLoader& loader)
{
	m_prim.clear();
	m_prim.m_type = parse::Prim::EType_Box;
	m_prim.m_radius.zero();

	loader.FindSectionStart();
	std::string keyword;
	while( !loader.IsSectionEnd() )
	{
		if( !loader.IsKeyword() )
		{
			loader.ExtractVector3(m_prim.m_radius, 0.0f);
		}
		else if( loader.GetKeyword(keyword) )
		{
			if	   ( ParsePrimCommon(loader, keyword) ) {}
			else if( str::EqualNoCase(keyword, "Random") )
			{
				v4 vmin = v4Zero, vmax = v4Zero;
				loader.FindSectionStart();
				loader.ExtractVector3(vmin, 1.0f);
				loader.ExtractVector3(vmax, 1.0f);
				loader.FindSectionEnd();
				m_prim.m_radius = v4Random3(vmin, vmax, 0.0f);
			}
		}
	}
	Encompase(m_prim.m_bbox, -m_prim.m_radius);
	Encompase(m_prim.m_bbox, +m_prim.m_radius);
	loader.FindSectionEnd();
}

// Parse the description of a cylinder
void Parser::ParseCylinder(ScriptLoader& loader)
{
	m_prim.clear();
	m_prim.m_type = parse::Prim::EType_Cylinder;
	m_prim.m_radius.zero();

	loader.FindSectionStart();
	std::string keyword;
	while( !loader.IsSectionEnd() )
	{
		if( !loader.IsKeyword() )
		{
			loader.ExtractFloat(m_prim.m_radius.y);	// height
			loader.ExtractFloat(m_prim.m_radius.x);	// radius
		}
		else if( loader.GetKeyword(keyword) )
		{
			if	   ( ParsePrimCommon(loader, keyword) ) {}
			else if( str::EqualNoCase(keyword, "Random") )
			{
				v4 vmin = v4Zero, vmax = v4Zero;
				loader.FindSectionStart();
				loader.ExtractFloat(vmin.y); loader.ExtractFloat(vmin.x);
				loader.ExtractFloat(vmax.y); loader.ExtractFloat(vmax.x);
				loader.FindSectionEnd();
				m_prim.m_radius = v4Random3(vmin, vmax, 0.0f);
			}
		}
	}
	v4 bound; bound.set(m_prim.m_radius.x, m_prim.m_radius.y*0.5f, m_prim.m_radius.x, 0.0f);
	Encompase(m_prim.m_bbox, -bound);
	Encompase(m_prim.m_bbox, +bound);
	loader.FindSectionEnd();
}

// Parse the description of a sphere
void Parser::ParseSphere(ScriptLoader& loader)
{
	m_prim.clear();
	m_prim.m_type = parse::Prim::EType_Sphere;
	m_prim.m_radius.zero();

	loader.FindSectionStart();
	std::string keyword;
	while( !loader.IsSectionEnd() )
	{
		if( !loader.IsKeyword() )
		{
			loader.ExtractFloat(m_prim.m_radius.x);	// radius
		}
		else if( loader.GetKeyword(keyword) )
		{
			if	   ( ParsePrimCommon(loader, keyword) ) {}
			else if( str::EqualNoCase(keyword, "Random") )
			{
				float vmin, vmax;
				loader.FindSectionStart();
				loader.ExtractFloat(vmin);
				loader.ExtractFloat(vmax);
				loader.FindSectionEnd();
				m_prim.m_radius.x = FRand(vmin, vmax);
			}
		}		
	}
	v4 bound; bound.set(m_prim.m_radius.x, m_prim.m_radius.x, m_prim.m_radius.x, 0.0f);
	Encompase(m_prim.m_bbox, -bound);
	Encompase(m_prim.m_bbox, +bound);
	loader.FindSectionEnd();
}

// Parse the description of a polytope
void Parser::ParsePolytope(ScriptLoader& loader)
{
	m_prim.clear();
	m_prim.m_type = parse::Prim::EType_Polytope;

	loader.FindSectionStart();
	std::string keyword;
	while( !loader.IsSectionEnd() )
	{
		if( !loader.IsKeyword() )
		{
            v4 pt; loader.ExtractVector3(pt, 1.0f);
			m_prim.m_vertex.push_back(pt);
		}
		else if( loader.GetKeyword(keyword) )
		{
			if	   ( ParsePrimCommon(loader, keyword) ) {}
			else if( str::EqualNoCase(keyword, "Random") )
			{
				int count;
				v4 vmin = v4Zero, vmax = v4Zero;
				loader.FindSectionStart();
				loader.ExtractInt(count, 10);
				loader.ExtractVector3(vmin, 1.0f);
				loader.ExtractVector3(vmax, 1.0f);
				loader.FindSectionEnd();
				for( int i = 0; i != count; ++i )
					m_prim.m_vertex.push_back(v4Random3(vmin, vmax, 1.0f));
			}
		}		
	}
	for( parse::TPoints::const_iterator v = m_prim.m_vertex.begin(), v_end = m_prim.m_vertex.end(); v != v_end; ++v )
		Encompase(m_prim.m_bbox, *v);
	
	loader.FindSectionEnd();
}

// Parse the description of a triangle
void Parser::ParseTriangle(ScriptLoader& loader)
{
	m_prim.clear();
	m_prim.m_type = parse::Prim::EType_Triangle;

	loader.FindSectionStart();
	std::string keyword;
	while( !loader.IsSectionEnd() )
	{
		if( !loader.IsKeyword() )
		{
            v4 pt; loader.ExtractVector3(pt, 0.0f);
			m_prim.m_vertex.push_back(pt);
		}
		else if( loader.GetKeyword(keyword) )
		{
			if	   ( ParsePrimCommon(loader, keyword) ) {}
			else if( str::EqualNoCase(keyword, "Random") )
			{
				int count;
				v4 vmin = v4Zero, vmax = v4Zero;
				loader.FindSectionStart();
				loader.ExtractInt(count, 10);
				loader.ExtractVector3(vmin, 0.0f);
				loader.ExtractVector3(vmax, 0.0f);
				loader.FindSectionEnd();
				for( int i = 0; i != 3; ++i )
					m_prim.m_vertex.push_back(v4Random3(vmin, vmax, 0.0f));
			}
		}		
	}
	for( parse::TPoints::const_iterator v = m_prim.m_vertex.begin(), v_end = m_prim.m_vertex.end(); v != v_end; ++v )
		Encompase(m_prim.m_bbox, *v);

	loader.FindSectionEnd();
}

// Parse a description of a skeleton for a model
void Parser::ParseSkeleton(pr::ScriptLoader& loader, parse::Skeleton& skel)
{
	std::string keyword;
	loader.FindSectionStart();
	while( loader.GetKeyword(keyword) )
	{
		switch( Parse(loader, keyword) )
		{
		default: break;
		case EObjectType_Colour:		skel.m_colour = m_colour; break;
		case EObjectType_DisableRender:	skel.m_render = false; break;
		case EObjectType_Unknown:
			if( str::EqualNoCase(keyword, "Anchors") )
			{
				loader.FindSectionStart();
				while( !loader.IsSectionEnd() )
				{
					v4 pt; 
					loader.ExtractVector3(pt, 1.0f);
					skel.m_anchor.push_back(pt);
				}
				loader.FindSectionEnd();
			}
			else if( str::EqualNoCase(keyword, "Struts") )
			{
				loader.FindSectionStart();
				while( !loader.IsSectionEnd() )
				{
					pr::uint i0; loader.ExtractUInt(i0, 10);
					pr::uint i1; loader.ExtractUInt(i1, 10);
					skel.m_strut.push_back(i0);
					skel.m_strut.push_back(i1);
				}
				loader.FindSectionEnd();
			}
		}
	}
	loader.FindSectionEnd();
}

// Parse a model by name
void Parser::ParseModelByName(ScriptLoader& loader)
{
	std::string model_name;
	loader.FindSectionStart();
	loader.ExtractString(model_name);
	loader.FindSectionEnd();

	// Look for the model in the current output
	for( m_index = 0; m_index != m_output.m_models.size(); ++m_index )
	{
		parse::Model const& model = m_output.m_models[m_index];
		if( str::EqualNoCase(model.m_name, model_name) )
			return;
	}
	m_index = 0xFFFFFFFF;
}

// Parse the description of a deformable mesh
void Parser::ParseDeformable(ScriptLoader& loader)
{
	float scale = 1.0f;
	parse::Deformable deformable;
	loader.FindSectionStart();
	std::string keyword;
	while( loader.GetKeyword(keyword) )
	{
		switch( Parse(loader, keyword) )
		{
		default: break;
		case EObjectType_Name:		deformable.m_name				= m_str; break;
		case EObjectType_Transform: deformable.m_model_to_world		= m_mat; break;
		case EObjectType_Position:	deformable.m_model_to_world.pos	= m_vec; break;
		case EObjectType_Colour:	deformable.m_colour				= m_colour; break;
		case EObjectType_Unknown:
			if	   ( str::EqualNoCase(keyword, "SpringsColour"				) )	{ ParseColour(loader); deformable.m_springs_colour = m_colour; }
			else if( str::EqualNoCase(keyword, "BeamsColour"				) )	{ ParseColour(loader); deformable.m_beams_colour = m_colour; }
			else if( str::EqualNoCase(keyword, "SpringConstant"				) )	{ loader.ExtractFloat(deformable.m_spring_constant); }
			else if( str::EqualNoCase(keyword, "DampingConstant"			) )	{ loader.ExtractFloat(deformable.m_damping_constant); }
			else if( str::EqualNoCase(keyword, "SprainPercentage"			) )	{ loader.ExtractFloat(deformable.m_sprain_percentage); }
			else if( str::EqualNoCase(keyword, "DisableColModelGeneration"	) ) { deformable.m_generate_col_models = false; }
			else if( str::EqualNoCase(keyword, "Tolerance"					) )	{ loader.ExtractFloat(deformable.m_convex_tolerance); }
			else if( str::EqualNoCase(keyword, "Scale"						) )	{ loader.ExtractFloat(scale); }
			else if( str::EqualNoCase(keyword, "TetraMeshVerts"				) ||
					 str::EqualNoCase(keyword, "SpringMeshVerts"			) ||
					 str::EqualNoCase(keyword, "Anchors"					) )
			{
				parse::TPoints& verts = str::EqualNoCase(keyword, "TetraMeshVerts")  ? deformable.m_tmesh_verts :
										str::EqualNoCase(keyword, "SpringMeshVerts") ? deformable.m_smesh_verts :
										deformable.m_anchors;
				loader.FindSectionStart();
				while( !loader.IsSectionEnd() )
				{
					v4 pt; loader.ExtractVector3(pt, 1.0f);
					pt *= scale;
					pt.w = 1.0f;
					verts.push_back(pt);
					Encompase(deformable.m_bbox, pt);
				}
				loader.FindSectionEnd();
			}
			else if( str::EqualNoCase(keyword, "Tetra") )
			{
				loader.FindSectionStart();
				while( !loader.IsSectionEnd() )
				{
					unsigned int idx[4];
					loader.ExtractUInt(idx[0], 10);
					loader.ExtractUInt(idx[1], 10);
					loader.ExtractUInt(idx[2], 10);
					loader.ExtractUInt(idx[3], 10);
					deformable.m_tetras.insert(deformable.m_tetras.end(), idx, idx + 4);
				}
				loader.FindSectionEnd();
			}
			else if( str::EqualNoCase(keyword, "Springs") ||
					 str::EqualNoCase(keyword, "Beams") )
			{
				unsigned int index_offset = 0;
				parse::TIndices& edges = str::EqualNoCase(keyword, "Springs") ? deformable.m_springs : deformable.m_beams;
				loader.FindSectionStart();
				while( !loader.IsSectionEnd() )
				{
					if( loader.IsKeyword() )
					{
						loader.GetKeyword(keyword);
						if( str::EqualNoCase(keyword, "IndexOffset") )		{ loader.ExtractUInt(index_offset, 10); }
					}
					else
					{
						unsigned int i0, i1;
						loader.ExtractUInt(i0, 10);
						loader.ExtractUInt(i1, 10);
						edges.push_back(i0 + index_offset);
						edges.push_back(i1 + index_offset);
					}
				}
				loader.FindSectionEnd();
			}
		}
	}
	loader.FindSectionEnd();

	m_index = 0xFFFFFFFF;
	if( deformable.has_data() )
	{
		m_index = m_output.m_deformables.size();
		m_output.m_deformables.push_back(deformable);
	}
}

// Parse a deformable mesh by name
void Parser::ParseDeformableByName(ScriptLoader& loader)
{
	std::string deformable_name;
	loader.FindSectionStart();
	loader.ExtractString(deformable_name);
	loader.FindSectionEnd();

	// Look for the deformable in the current output
	for( m_index = 0; m_index != m_output.m_deformables.size(); ++m_index )
	{
		parse::Deformable const& deformable = m_output.m_deformables[m_index];
		if( str::EqualNoCase(deformable.m_name, deformable_name) )
			return;
	}
	m_index = 0xFFFFFFFF;
}

// Parse a static scene section 
void Parser::ParseStaticObject(ScriptLoader& loader)
{
	parse::Static statik;
	
	loader.FindSectionStart();
	std::string keyword;
	while( loader.GetKeyword(keyword) )
	{
		switch( Parse(loader, keyword) )
		{
		default: break;
		case EObjectType_Name:			statik.m_name				= m_str; break;
		case EObjectType_Transform:		statik.m_inst_to_world		= m_mat; break;
		case EObjectType_Position:		statik.m_inst_to_world.pos	= m_vec; break;
		case EObjectType_Colour:		statik.m_colour				= m_colour; break;
		case EObjectType_Model:
		case EObjectType_ModelByName:
			statik.m_model_index = m_index;
			Encompase(statik.m_bbox, m_output.m_models[m_index].m_bbox);
			break;
		case EObjectType_Unknown:
			break;
		}
	}
	loader.FindSectionEnd();

	m_index = 0xFFFFFFFF;
	if( statik.m_model_index != 0xFFFFFFFF ) 
	{
		m_index = m_output.m_statics.size();
		m_output.m_statics.push_back(statik);
		Encompase(m_output.m_world_bounds, statik.m_inst_to_world * statik.m_bbox);
	}
}

// Parse a dynamic object
void Parser::ParsePhysicsObject(ScriptLoader& loader)
{
	parse::PhysObj phys;

	loader.FindSectionStart();
	std::string keyword;
	while( loader.GetKeyword(keyword) )
	{
		switch( Parse(loader, keyword) )
		{
		default: break;
		case EObjectType_Name:			phys.m_name					= m_str; break;
		case EObjectType_ByName:		phys.m_by_name_only			= true;	break;
		case EObjectType_Transform:		phys.m_object_to_world		= m_mat; break;
		case EObjectType_Position:		phys.m_object_to_world.pos	= m_vec; break;
		case EObjectType_Velocity:		phys.m_velocity				= m_vec; break;
		case EObjectType_AngVelocity:	phys.m_ang_velocity			= m_vec; break;
		case EObjectType_Gravity:		phys.m_gravity				= m_vec; break;
		case EObjectType_Mass:			phys.m_mass					= m_value; break;
		case EObjectType_Colour:		phys.m_colour				= m_colour; break;
		case EObjectType_Stationary:	phys.m_stationary			= true; break;
		case EObjectType_Model:
		case EObjectType_ModelByName:
			phys.m_model_type  = EObjectType_Model;
			phys.m_model_index = m_index;
			if( m_index != 0xFFFFFFFF )
				Encompase(phys.m_bbox, m_output.m_models[m_index].m_bbox);
			break;
		case EObjectType_Deformable:
		case EObjectType_DeformableByName:
			if( m_index != 0xFFFFFFFF )
			{
				phys.m_model_type  = EObjectType_Deformable;
				phys.m_model_index = m_index;
				Encompase(phys.m_bbox, m_output.m_deformables[m_index].m_bbox);
			}
			break;
		case EObjectType_Unknown:
			break;
		}
	}
	loader.FindSectionEnd();

	m_index = 0xFFFFFFFF;
	if( phys.m_model_type != EObjectType_None ) 
	{
		m_index = m_output.m_phys_obj.size();
		m_output.m_phys_obj.push_back(phys);
		if( !phys.m_by_name_only )
			Encompase(m_output.m_world_bounds, phys.m_object_to_world * phys.m_bbox);
	}
}

// Parse a physics object by name
void Parser::ParsePhysObjByName(ScriptLoader& loader)
{
	std::string phys_obj_name;
	loader.FindSectionStart();
	loader.ExtractString(phys_obj_name);
	loader.FindSectionEnd();

	// Look for the physics object in the current output
	for( m_index = 0; m_index != m_output.m_phys_obj.size(); ++m_index )
	{
		parse::PhysObj const& phys_obj = m_output.m_phys_obj[m_index];
		if( str::EqualNoCase(phys_obj.m_name, phys_obj_name) )
			return;
	}
	m_index = 0xFFFFFFFF;
}

// Parse a multi body object
void Parser::ParseMultibody(ScriptLoader& loader, parse::Multibody* parent)
{
	parse::Multibody multi;
	loader.FindSectionStart();
	std::string keyword;
	while( loader.GetKeyword(keyword) )
	{
		switch( Parse(loader, keyword) )
		{
		default: break;
		case EObjectType_Name:			multi.m_name				= m_str; break;
		case EObjectType_Transform:		multi.m_object_to_world		= m_mat; break;
		case EObjectType_Position:		multi.m_object_to_world.pos	= m_vec; break;
		case EObjectType_Velocity:		multi.m_velocity			= m_vec; break;
		case EObjectType_AngVelocity:	multi.m_ang_velocity		= m_vec; break;
		case EObjectType_Gravity:		multi.m_gravity				= m_vec; break;
		case EObjectType_Colour:		multi.m_colour				= m_colour; break;
		case EObjectType_PhysicsObject:
		case EObjectType_PhysObjByName:
			multi.m_phys_obj_index = m_index;
			Encompase(multi.m_bbox, m_output.m_phys_obj[m_index].m_bbox);
			break;
		case EObjectType_Unknown:
			if( str::EqualNoCase(keyword, "Joint") )
			{
				ParseMultibody(loader, &multi);
			}
			else if( str::EqualNoCase(keyword, "ParentAttach") )
			{
				loader.FindSectionStart();
				loader.ExtractVector3(multi.m_ps_attach.x, 0.0f);
				loader.ExtractVector3(multi.m_ps_attach.y, 0.0f);
				loader.ExtractVector3(multi.m_ps_attach.z, 0.0f);
				loader.FindSectionEnd();
			}
			else if( str::EqualNoCase(keyword, "Attach") )
			{
				loader.FindSectionStart();
				loader.ExtractVector3(multi.m_os_attach.x, 0.0f);
				loader.ExtractVector3(multi.m_os_attach.y, 0.0f);
				loader.ExtractVector3(multi.m_os_attach.z, 0.0f);
				loader.FindSectionEnd();
			}
			else if( str::EqualNoCase(keyword, "JointType"			) )	{ loader.ExtractInt(multi.m_joint_type, 10); }
			else if( str::EqualNoCase(keyword, "JointPos"			) )	{ loader.ExtractFloat(multi.m_pos); }
			else if( str::EqualNoCase(keyword, "JointVel"			) )	{ loader.ExtractFloat(multi.m_vel); }
			else if( str::EqualNoCase(keyword, "JointLimits"		) )	{ loader.ExtractFloat(multi.m_lower_limit); loader.ExtractFloat(multi.m_upper_limit); }
			else if( str::EqualNoCase(keyword, "JointRestitution"	) )	{ loader.ExtractFloat(multi.m_restitution); }
			else if( str::EqualNoCase(keyword, "JointZero"			) )	{ loader.ExtractFloat(multi.m_joint_zero); }
			else if( str::EqualNoCase(keyword, "JointSpring"		) ) { loader.ExtractFloat(multi.m_joint_spring); }
			else if( str::EqualNoCase(keyword, "JointDamping"		) ) { loader.ExtractFloat(multi.m_joint_damping); }
		}
	}
	loader.FindSectionEnd();

	m_index = 0xFFFFFFFF;
	if( multi.m_phys_obj_index != 0xFFFFFFFF )
	{
		if( parent )
		{
			parent->m_joints.push_back(multi);
			Encompase(parent->m_bbox, multi.m_bbox);
		}
		else
		{
			m_index = m_output.m_multis.size();
			m_output.m_multis.push_back(multi);
			Encompase(m_output.m_world_bounds, multi.m_object_to_world * multi.m_bbox);
		}
	}
}

