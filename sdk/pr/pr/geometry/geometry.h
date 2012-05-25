//********************************
// PRGeometry
//  Copyright © Rylogic Ltd 2006
//********************************
#ifndef PR_GEOMETRY_H
#define PR_GEOMETRY_H

#include "pr/common/array.h"
#include "pr/common/colour.h"
#include "pr/maths/maths.h"
#include "pr/geometry/forward.h"

namespace pr
{
	namespace geom
	{
		inline GeomType Parse(char const* str)
		{
			string32 string = str; str::LowerCase(string);
			GeomType type = 0;
			if (str::Contains(string, "vertex" )) type |= EVertex;
			if (str::Contains(string, "normal" )) type |= ENormal;
			if (str::Contains(string, "colour" )) type |= EColour;
			if (str::Contains(string, "texture")) type |= ETexture;
			return type;
		}
		inline string32 ToString(GeomType type)
		{
			string32 string = "";
			if (type & EVertex ) string += "Vertex";
			if (type & ENormal ) string += "Normal";
			if (type & EColour ) string += "Colour";
			if (type & ETexture) string += "Texture";
			return string;
		}
		inline bool IsValid(GeomType type)
		{
			return type != EInvalid && (type & ~EAll) == 0;
		}
	}
	
	typedef pr::Array<pr::v4, 256>       TVecCont;
	typedef pr::Array<pr::uint16, 256>   TIndexCont;
	typedef pr::Array<pr::Colour32, 256> TColour32Cont;
	
	struct Texture
	{
		pr::string<>   m_filename;         // Path to the texture
	};
	typedef pr::Array<Texture, 1> TTextureCont;
	
	struct Material
	{
		pr::Colour     m_ambient;          // Ambient
		pr::Colour     m_diffuse;          // Diffuse
		pr::Colour     m_specular;         // Specular
		float          m_specpower;        // Power
		TTextureCont   m_texture;          // The textures of this material
	};
	typedef pr::Array<Material, 1> TMaterialCont;
	
	struct Vert
	{
		pr::v4         m_vertex;           // Position of this vertex
		pr::v4         m_normal;           // Normal for this vertex
		pr::Colour32   m_colour;           // Vertex colour
		pr::v2         m_tex_vertex;       // Texture co-ordinates for this vertex
		
		static Vert   make(pr::v4 const& vertex, pr::v4 const& normal, pr::Colour32 colour, pr::v2 const& uv) { Vert v; return v.set(vertex, normal, colour, uv); }
		Vert&          set(pr::v4 const& vertex, pr::v4 const& normal, pr::Colour32 colour, pr::v2 const& uv) { m_vertex = vertex; m_normal = normal; m_colour = colour; m_tex_vertex = uv; return *this; }
	};
	typedef pr::Array<Vert, 256> TVertCont;
	inline Vert operator * (pr::m4x4 const& lhs, pr::Vert const& rhs) { Vert v = {lhs * rhs.m_vertex, lhs * rhs.m_normal, rhs.m_colour, rhs.m_tex_vertex}; return v; }
	
	struct Face
	{
		pr::uint16     m_vert_index[3];    // Indices to the vertices of this face
		uint           m_mat_index;        // The index of the material for this face
		uint           m_flags;            // General flags for this face
		
		static Face   make(uint16 i0, uint16 i1, uint16 i2, uint mat_index, uint flags) { Face f; return f.set(i0, i1, i2, mat_index, flags); }
		Face&          set(uint16 i0, uint16 i1, uint16 i2, uint mat_index, uint flags) { m_vert_index[0]=i0; m_vert_index[1]=i1; m_vert_index[2]=i2; m_mat_index=mat_index; m_flags=flags; return *this; }
	};
	typedef pr::Array<Face, 20> TFaceCont;
	
	struct Mesh
	{
		TVertCont      m_vertex;           // The vertices of this mesh
		TFaceCont      m_face;             // The faces of the mesh
		TMaterialCont  m_material;         // The materials used in this mesh
		GeomType       m_geom_type;        // The type of model this is. Used to figure out what FVF to use
	};
	typedef pr::Array<Mesh, 1> TMeshCont;
	
	struct Frame
	{
		pr::string<char,32>  m_name;       // The name of this object
		pr::m4x4             m_transform;  // A transform for this object
		Mesh                 m_mesh;       // The geometry of the object
	};
	typedef pr::Array<Frame, 1> TFrameCont;
	
	struct Geometry
	{
		pr::string<char,32> m_name;        // The filename of this geometry file
		TFrameCont          m_frame;       // The objects contained in ths geometry file
	};
	
	inline Material DefaultPRMaterial()
	{
		Material mat;
		mat.m_ambient   .set(1.0f, 1.0f, 1.0f, 1.0f);
		mat.m_diffuse   .set(1.0f, 1.0f, 1.0f, 1.0f);
		mat.m_specular  .set(0.0f, 0.0f, 0.0f, 1.0f);
		mat.m_specpower = 10.0f;
		return mat;
	}
}

#endif
