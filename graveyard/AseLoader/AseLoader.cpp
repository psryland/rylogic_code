//********************************************************************
//
//	An 3DS-Max "ASC" to "DirectX" geometry converter
//
//********************************************************************

#include "pr/Geometry/AseLoader/AseLoader.h"
#include "pr/FileSys/FileEx.h"

using namespace pr;

//*****
// Constructor
AseLoader::AseLoader()
:m_pos(0)
,m_count(0)
{}

//*****
// Destructor
AseLoader::~AseLoader()
{}

//*****
// Load an ASE geometry
HRESULT AseLoader::Load(const char* asefilename, Geometry& geometry, const AseLoaderSettings* settings)
{
	if( settings ) m_settings = *settings;
	geometry.m_name = asefilename;

	FileEx file(geometry.m_name.c_str(), FileEx::Reading);
	if( !file.IsOpen() ) return error::AseLoader_FAILED_TO_OPEN_FILE;

	m_count = file.Length();
	m_source.resize(m_count);
	m_count = file.Read(&m_source[0], m_count);

	// Reset the temperary buffers
	m_vertex.clear();
	m_tex_coord.clear();
	m_material.clear();
	m_face.clear();

	// Load the geometry
	m_pos = 0;
	m_load_result = S_OK;
	while( GetKeyWord() )
	{
		if( strcmp(m_keyword.c_str(), "MATERIAL_LIST") == 0 )
		{
			LoadMaterialList();
		}
		else if( strcmp(m_keyword.c_str(), "GEOMOBJECT") == 0 )
		{
			LoadGeomObject(geometry);
		}
	}
	return m_load_result;
}

//*****
// Load the list of materials
void AseLoader::LoadMaterialList()
{
	if( !FindSectionStart() ) return Error(error::AseLoader_PARSE_ERROR);
	while( GetKeyWord() )
	{
		if( strcmp(m_keyword.c_str(), "MATERIAL_COUNT")	== 0 )
		{
			std::size_t material_count;
			if( !ExtractSizeT(material_count) ) return Error(error::AseLoader_PARSE_ERROR);
			m_material.resize(material_count);
		}
		else if( strcmp(m_keyword.c_str(), "MATERIAL") == 0 )
		{
			LoadMaterial();
		}
	}
	if( !FindSectionEnd() ) return Error(error::AseLoader_PARSE_ERROR);
}

//*****
// Load a material and add it to the material list
void AseLoader::LoadMaterial()
{
	if( !FindSectionStart() )	return Error(error::AseLoader_PARSE_ERROR);
	if( m_material.empty() )	return Error(error::AseLoader_MATERIAL_COUNT_MISSING);

	std::size_t material_index;
	if( !ExtractSizeT(material_index) )	return Error(error::AseLoader_PARSE_ERROR);
	PR_ASSERT(PR_DBG_ASELOADER, material_index < m_material.size());
	material_index = Clamp<std::size_t>(material_index, 0, m_material.size() - 1);
	Material& material = m_material[material_index];
	material = DefaultPRMaterial();

	while( GetKeyWord() )
	{
		if( strcmp(m_keyword.c_str(), "MATERIAL_AMBIENT") == 0 )
		{
			material.m_ambient.a = 1.0f;
			if( !ExtractFloat(material.m_ambient.r) || !ExtractFloat(material.m_ambient.g) || !ExtractFloat(material.m_ambient.b) )
				return Error(error::AseLoader_PARSE_ERROR);
		}
		else if( strcmp(m_keyword.c_str(), "MATERIAL_DIFFUSE") == 0 )
		{
			material.m_diffuse.a = 1.0f;
			if( !ExtractFloat(material.m_diffuse.r) || !ExtractFloat(material.m_diffuse.g) || !ExtractFloat(material.m_diffuse.b) )
				return Error(error::AseLoader_PARSE_ERROR);
		}
		else if( strcmp(m_keyword.c_str(), "MATERIAL_SPECULAR") == 0 )
		{
			material.m_specular.a = 1.0f;
			if( !ExtractFloat(material.m_specular.r) || !ExtractFloat(material.m_specular.g) || !ExtractFloat(material.m_specular.b) )
				return Error(error::AseLoader_PARSE_ERROR);
		}
		else if( strcmp(m_keyword.c_str(), "MATERIAL_SHINESTRENGTH") == 0 )
		{
			if( !ExtractFloat(material.m_power) )
				return Error(error::AseLoader_PARSE_ERROR);
		}
		else if( strcmp(m_keyword.c_str(), "NUMSUBMTLS") == 0 )
		{
			std::size_t sub_material_count;
			if( !ExtractSizeT(sub_material_count) ) return Error(error::AseLoader_PARSE_ERROR);
			material.m_texture.resize(sub_material_count);
		}
		else if( strcmp(m_keyword.c_str(), "SUBMATERIAL") == 0 )
		{
			LoadMaterialSubMaterial(material);
		}
		else if( strcmp(m_keyword.c_str(), "MAP_DIFFUSE") == 0 )
		{
			if( material.m_texture.empty() ) material.m_texture.resize(1);
			LoadMaterialMapDiffuse(material.m_texture[0]);
		}
	}
	if( !FindSectionEnd() ) return Error(error::AseLoader_PARSE_ERROR);
}

//*****
// Read a sub material
void AseLoader::LoadMaterialSubMaterial(Material& material)
{
	if( material.m_texture.empty() ) return Error(error::AseLoader_SUBMATERIAL_COUNT_MISSING);
	
	// Read the sub material index
	std::size_t sub_material_index;
	if( !ExtractSizeT(sub_material_index) ) return Error(error::AseLoader_PARSE_ERROR);
	PR_ASSERT(PR_DBG_ASELOADER, sub_material_index < material.m_texture.size());
	sub_material_index = Clamp<std::size_t>(sub_material_index, 0, material.m_texture.size() - 1);

	// Read the sub material
	Texture& texture = material.m_texture[sub_material_index];
	if( !FindSectionStart() ) return Error(error::AseLoader_PARSE_ERROR);
	while( GetKeyWord() )
	{
		if( strcmp(m_keyword.c_str(), "MAP_DIFFUSE") == 0 ) LoadMaterialMapDiffuse(texture);
	}
	if( !FindSectionEnd() ) return Error(error::AseLoader_PARSE_ERROR);
}

//*****
// Load a texture
void AseLoader::LoadMaterialMapDiffuse(Texture& texture)
{
	if( !FindSectionStart() ) return Error(error::AseLoader_PARSE_ERROR);
	while( GetKeyWord() )
	{
		if( strcmp(m_keyword.c_str(), "BITMAP")	== 0 )
		{
			if( !ExtractString(texture.m_filename) ) return Error(error::AseLoader_PARSE_ERROR);
			if( _stricmp(texture.m_filename.c_str(), "None") == 0 )
			{
				texture.m_filename = "";
			}
		}
	}
	if( !FindSectionEnd() ) return Error(error::AseLoader_PARSE_ERROR);
}

//*****
// Load an Frame
void AseLoader::LoadGeomObject(Geometry& geometry)
{
	if( !FindSectionStart() ) return Error(error::AseLoader_PARSE_ERROR);

	geometry.m_frame.push_back(Frame());
	Frame& frame = geometry.m_frame.back();
	frame.m_transform.Identity();
	frame.m_mesh.m_geometry_type = geometry::EType_Invalid;

	while( GetKeyWord() )
	{
		if( strcmp(m_keyword.c_str(), "NODE_NAME")	== 0 )
		{
			if( !ExtractString(frame.m_name) ) return Error(error::AseLoader_PARSE_ERROR);
		}
		else if( strcmp(m_keyword.c_str(), "NODE_TM") == 0 )
		{
			if( !FindSectionStart() ) return Error(error::AseLoader_PARSE_ERROR);
			while( GetKeyWord() )
			{
					 if( strcmp(m_keyword.c_str(), "TM_ROW0")	== 0 ) LoadTMRow(frame, 0);
				else if( strcmp(m_keyword.c_str(), "TM_ROW1")	== 0 ) LoadTMRow(frame, 1);
				else if( strcmp(m_keyword.c_str(), "TM_ROW2")	== 0 ) LoadTMRow(frame, 2);
				else if( strcmp(m_keyword.c_str(), "TM_ROW3")	== 0 ) LoadTMRow(frame, 3);
			}
			if( !FindSectionEnd() ) return Error(error::AseLoader_PARSE_ERROR);
		}
		else if( strcmp(m_keyword.c_str(), "GEOMTYPE") == 0 )
		{
			std::size_t geom_type;
			if( !ExtractSizeT(geom_type) ) return Error(error::AseLoader_PARSE_ERROR);
			frame.m_mesh.m_geometry_type = geom_type;
		}
		else if( strcmp(m_keyword.c_str(), "MESH") == 0 )
		{
			LoadMesh(frame);
		}
	}
	if( !FindSectionEnd() ) return Error(error::AseLoader_PARSE_ERROR);
}

//*****
// The next token in the source data should be a 3 float row in the frame transform matrix
void AseLoader::LoadTMRow(Frame& frame, int row)
{
	frame.m_transform[row].Zero();
	frame.m_transform[row][row] = 1.0f;
	if( !ExtractFloat(frame.m_transform[row][0]) ||
		!ExtractFloat(frame.m_transform[row][1]) ||
		!ExtractFloat(frame.m_transform[row][2]) )
	{
		return Error(error::AseLoader_PARSE_ERROR);
	}
}

//*****
// Load a mesh
void AseLoader::LoadMesh(Frame& frame)
{
	if( !FindSectionStart() ) return Error(error::AseLoader_PARSE_ERROR);

	while( GetKeyWord() )
	{
		if( strcmp(m_keyword.c_str(), "MESH_NUMVERTEX")	== 0 )
		{
			std::size_t vertex_count;
			if( !ExtractSizeT(vertex_count) ) return Error(error::AseLoader_PARSE_ERROR);
			m_vertex.clear();
			m_vertex.resize(vertex_count);
		}
		else if( strcmp(m_keyword.c_str(), "MESH_VERTEX_LIST") == 0 )
		{
			if( !FindSectionStart() ) return Error(error::AseLoader_PARSE_ERROR);
			while( GetKeyWord() ) if( strcmp(m_keyword.c_str(), "MESH_VERTEX") == 0 ) LoadVertex();
			if( !FindSectionEnd() ) return Error(error::AseLoader_PARSE_ERROR);
		}
		else if( strcmp(m_keyword.c_str(), "MESH_NUMFACES")		== 0 )
		{
			std::size_t face_count;
			if( !ExtractSizeT(face_count) ) return Error(error::AseLoader_PARSE_ERROR);
			m_face.clear();
			m_face.resize(face_count);
		}
		else if( strcmp(m_keyword.c_str(), "MESH_FACE_LIST")	== 0 )
		{
			if( !FindSectionStart() ) return Error(error::AseLoader_PARSE_ERROR);
			while( GetKeyWord() ) if( strcmp(m_keyword.c_str(), "MESH_FACE")	== 0 ) LoadFace();
			if( !FindSectionEnd() ) return Error(error::AseLoader_PARSE_ERROR);
		}
		else if( strcmp(m_keyword.c_str(), "MESH_NUMTVERTEX")	== 0 )
		{
			std::size_t tvertices_count;
			if( !ExtractSizeT(tvertices_count) ) return Error(error::AseLoader_PARSE_ERROR);
			m_tex_coord.clear();
			m_tex_coord.resize(tvertices_count);
		}
		else if( strcmp(m_keyword.c_str(), "MESH_TVERTLIST")	== 0 )
		{
			if( !FindSectionStart() ) return Error(error::AseLoader_PARSE_ERROR);
			while( GetKeyWord() ) if( strcmp(m_keyword.c_str(), "MESH_TVERT")	== 0 ) LoadTVertex();
			if( !FindSectionEnd() ) return Error(error::AseLoader_PARSE_ERROR);
		}
		else if( strcmp(m_keyword.c_str(), "MESH_NUMTVFACES")	== 0 )
		{
			// Not needed. Should be less than or equal to the number of faces.
		}
		else if( strcmp(m_keyword.c_str(), "MESH_TFACELIST")	== 0 )
		{
			if( !FindSectionStart() ) return Error(error::AseLoader_PARSE_ERROR);
			while( GetKeyWord() ) if( strcmp(m_keyword.c_str(), "MESH_TFACE")	== 0 ) LoadTFace();
			if( !FindSectionEnd() ) return Error(error::AseLoader_PARSE_ERROR);
		}
		else if( strcmp(m_keyword.c_str(), "MESH_NORMALS")		== 0 )
		{
			if( !FindSectionStart() ) return Error(error::AseLoader_PARSE_ERROR);
			while( GetKeyWord() ) if( strcmp(m_keyword.c_str(), "MESH_FACENORMAL") == 0 ) LoadFaceNormal();
			if( !FindSectionEnd() ) return Error(error::AseLoader_PARSE_ERROR);
		}
	}
	if( !FindSectionEnd() ) return Error(error::AseLoader_PARSE_ERROR);

	if( m_settings.m_generate_normals ) GenerateNormals();
	CompleteMesh(frame.m_mesh);
}

//*****
// The next token in the source data should be a vertex in the format: vertex_number X Y Z
void AseLoader::LoadVertex()
{
	if( m_vertex.empty() ) return Error(error::AseLoader_VERTEX_COUNT_MISSING);

	std::size_t vertex_number;
	if( !ExtractSizeT(vertex_number) ) return Error(error::AseLoader_PARSE_ERROR);
	PR_ASSERT(PR_DBG_ASELOADER, vertex_number < m_vertex.size());
	vertex_number = Clamp<std::size_t>(vertex_number, 0, m_vertex.size() - 1);

	v4& vertex = m_vertex[vertex_number].m_vertex; vertex.w = 1.0f;
	if( !ExtractFloat(vertex[0]) ||
		!ExtractFloat(vertex[1]) ||
		!ExtractFloat(vertex[2]) )
	{
		return Error(error::AseLoader_PARSE_ERROR);
	}
}

//*****
// The next token in the source data should be a face in the format: face_number: A: i0 B: i1 C: i2 *MESH_SMOOTHING 0  *MESH_MTLID  0
void AseLoader::LoadFace()
{
	if( m_face.empty() ) return Error(error::AseLoader_FACE_COUNT_MISSING);

	std::size_t face_number;
	if( !ExtractSizeT(face_number) ) return Error(error::AseLoader_PARSE_ERROR);
	PR_ASSERT(PR_DBG_ASELOADER, face_number < m_face.size());
	face_number = Clamp<std::size_t>(face_number, 0, m_face.size() - 1);
	
	std::string junk;
	if( !ExtractWord(junk) )			return Error(error::AseLoader_PARSE_ERROR);

	AseFace& face = m_face[face_number];
	for( int v = 0; v < 3; ++v )
	{
        std::size_t vert_index;
		if( !ExtractWord(junk) )		return Error(error::AseLoader_PARSE_ERROR);
		if( !ExtractSizeT(vert_index) )	return Error(error::AseLoader_PARSE_ERROR);
		PR_ASSERT(PR_DBG_ASELOADER, vert_index < m_vertex.size());
		vert_index = Clamp<std::size_t>(vert_index, 0, m_vertex.size() - 1);
		face.m_vert_index[v] = vert_index;
	}

	face.m_smoothing_group = 0;
	face.m_mat_index = 0;

	// Each face should be followed by a smoothing group and a material id
	while( PeekKeyWord() )
	{
		if( strcmp(m_keyword.c_str(), "MESH_SMOOTHING")	== 0 )
		{
			GetKeyWord();
			if( !ExtractSizeT(face.m_smoothing_group) ) return Error(error::AseLoader_PARSE_ERROR);
		}
		else if( strcmp(m_keyword.c_str(), "MESH_MTLID") == 0 )
		{
			GetKeyWord();
			if( !ExtractSizeT(face.m_mat_index) ) return Error(error::AseLoader_PARSE_ERROR);
			PR_ASSERT(PR_DBG_ASELOADER, face.m_mat_index < m_material.size());
			face.m_mat_index = Clamp<std::size_t>(face.m_mat_index, 0, m_material.size() - 1);
		}
		else
		{
			break;
		}
	}
}

//*****
// Load a texture vertex in the format: tvertex_number U V
void AseLoader::LoadTVertex()
{
	if( m_tex_coord.empty() ) return Error(error::AseLoader_TEXTURE_VERTEX_COUNT_MISSING);

	std::size_t tvertex_number;
	if( !ExtractSizeT(tvertex_number) ) return Error(error::AseLoader_PARSE_ERROR);
	PR_ASSERT(PR_DBG_ASELOADER, tvertex_number < m_tex_coord.size());
	tvertex_number = Clamp<std::size_t>(tvertex_number, 0, m_tex_coord.size() - 1);

	float spare;
	v2& tvertex = m_tex_coord[tvertex_number];
	if( !ExtractFloat(tvertex[0]) ||
		!ExtractFloat(tvertex[1]) ||
		!ExtractFloat(spare) )
	{
		return Error(error::AseLoader_PARSE_ERROR);
	}
}

//*****
// Load the face texture co-ord indices in the format: face_number i0 i1 i2
void AseLoader::LoadTFace()
{
	if( m_face.empty() ) return Error(error::AseLoader_FACE_COUNT_MISSING);
    
	std::size_t face_number;
	if( !ExtractSizeT(face_number) ) return Error(error::AseLoader_PARSE_ERROR);
	PR_ASSERT(PR_DBG_ASELOADER, face_number < m_face.size());
	face_number = Clamp<std::size_t>(face_number, 0, m_face.size() - 1);

	AseFace& face = m_face[face_number];	
	for( int t = 0; t < 3; ++t )
	{
        std::size_t tex_index;
		if( !ExtractSizeT(tex_index) ) return Error(error::AseLoader_PARSE_ERROR);
		PR_ASSERT(PR_DBG_ASELOADER, tex_index < m_tex_coord.size());
		tex_index = Clamp<std::size_t>(tex_index, 0, m_tex_coord.size() - 1);
		face.m_tex_index[t] = tex_index;
	}
}

//*****
// The next token in the source data should be a face normal in the format: face_number X Y Z
void AseLoader::LoadFaceNormal()
{
	if( m_face.empty() ) return Error(error::AseLoader_FACE_COUNT_MISSING);
    
	std::size_t face_number;
	if( !ExtractSizeT(face_number) ) return Error(error::AseLoader_PARSE_ERROR);
	PR_ASSERT(PR_DBG_ASELOADER, face_number < m_face.size());
	face_number = Clamp<std::size_t>(face_number, 0, m_face.size() - 1);

	AseFace& face = m_face[face_number];
	v4& face_normal = face.m_face_normal;	face_normal.w = 0.0f;
	if( !ExtractFloat(face_normal[0]) ||
		!ExtractFloat(face_normal[1]) ||
		!ExtractFloat(face_normal[2]) )
	{
		return Error(error::AseLoader_PARSE_ERROR);
	}

	// Each face normal should be followed by 3 vertex normals
	int n = 0;
	while( PeekKeyWord() )
	{
		if( strcmp(m_keyword.c_str(), "MESH_VERTEXNORMAL") == 0 )
		{
			// Don't generate normals cause we have some
			m_settings.m_generate_normals = false;

			GetKeyWord();

			std::size_t vertex_number;
			if( !ExtractSizeT(vertex_number) ) return Error(error::AseLoader_PARSE_ERROR);
			PR_ASSERT(PR_DBG_ASELOADER, vertex_number < m_vertex.size());
			vertex_number = Clamp<std::size_t>(vertex_number, 0, m_vertex.size() - 1);

			v4 vert_norm;	vert_norm.w = 0.0f;
			if( !ExtractFloat(vert_norm[0]) ||
				!ExtractFloat(vert_norm[1]) ||
				!ExtractFloat(vert_norm[2]) )
			{
				return Error(error::AseLoader_PARSE_ERROR);
			}
			m_vertex[vertex_number].AddNormal(face.m_smoothing_group, vert_norm);
			if( n + 1 < 3 ) ++n;
		}
		else
		{
			break;
		}		
	}
}

//*****
// Generate normals using the smoothing groups
void AseLoader::GenerateNormals()
{
	// For each face, calculate a face normal and add it to each of the vertex normals
	std::size_t num_faces = m_face.size();
	for( std::size_t f = 0; f < num_faces; ++f )
	{
		AseFace& face = m_face[f];

		// Calculate a face normal
		AseVertex& V0 = m_vertex[face.m_vert_index[0]];
		AseVertex& V1 = m_vertex[face.m_vert_index[1]];
		AseVertex& V2 = m_vertex[face.m_vert_index[2]];
		face.m_face_normal = Cross3(V1.m_vertex - V0.m_vertex, V2.m_vertex - V0.m_vertex);
		face.m_face_normal.Normalise3();

		// Add the face normal to each vertex that references the face
		V0.AddNormal(face.m_smoothing_group, face.m_face_normal);
		V1.AddNormal(face.m_smoothing_group, face.m_face_normal);
		V2.AddNormal(face.m_smoothing_group, face.m_face_normal);
	}
}

//*****
// Finish off the mesh
void AseLoader::CompleteMesh(Mesh& mesh)	
{
	std::size_t num_faces = m_face.size();
	std::size_t num_vertices = num_faces * 3;

	// Map material indices
	m_material_map.clear();
	for( std::size_t f = 0; f < num_faces; ++f )
	{
		// Map material indices - If there are materials then all "mat_index"'s should be in range
		if( !m_material.empty() )
		{
			// Find the material index for this face in the material index map
			std::list<IndexMap>::iterator map     = m_material_map.begin();
			std::list<IndexMap>::iterator map_end = m_material_map.end();
			for( ; map != map_end; ++map )
			{
				// If found, update the index to the mapped value
				if( m_face[f].m_mat_index == map->m_src_index )
				{
					m_face[f].m_mat_index = map->m_dst_index;
					break;
				}
			}
			// If not found...
			if( map == map_end )
			{
				// Add a material to the mesh
				mesh.m_material.push_back(m_material[m_face[f].m_mat_index]);

				// Add a mapping to the material map
				IndexMap index_map;
				index_map.m_src_index = m_face[f].m_mat_index;
				index_map.m_dst_index = m_material.size() - 1;
				m_material_map.push_back(index_map);

				// Update the material index in the face
				m_face[f].m_mat_index = index_map.m_dst_index;
			}
		}
		else
		{
			m_face[f].m_mat_index = 0;
		}
	}

	// Create an expanded vertex array
	m_expanded.clear();
	m_expanded.resize(num_vertices);
	for( std::size_t f = 0; f < num_faces; ++f )
	{
		AseFace& face = m_face[f];
		for( std::size_t v = 0; v < 3; ++v )
		{
			AseVertex& vertex = m_expanded[f * 3 + v];

			vertex.m_index_position = AseVertex::INVALID;
			if( face.m_vert_index[v] < m_vertex.size() )
			{
				vertex.m_vertex = m_vertex[face.m_vert_index[v]].m_vertex;
				vertex.m_normal = m_vertex[face.m_vert_index[v]].GetNormal(face.m_smoothing_group);
			}
			else
			{
				vertex.m_vertex.Origin();
				vertex.m_normal.Zero();
			}
			
			vertex.m_colour = 0;

			if( m_face[f].m_tex_index[v] < m_tex_coord.size() )
			{
				vertex.m_tex_vertex = m_tex_coord[m_face[f].m_tex_index[v]];
				
				// Adjust for DirectX texture co-ords
				vertex.m_tex_vertex[1] = 1.0f - vertex.m_tex_vertex[1];
			}
			else
			{
				vertex.m_tex_vertex.Zero();
			}
		}
	}

	// Simplify the expanded vertex array
	std::size_t num_unique_verts = 0;
	for( std::size_t v1 = 0; v1 < num_vertices; ++v1 )
	{
		AseVertex& vert1 = m_expanded[v1];
		if( vert1.m_index_position != AseVertex::INVALID ) continue;
		vert1.m_index_position = num_unique_verts;
		++num_unique_verts;

		const std::size_t MAX_DUPLICATE_SEPARATION = 16;
		for( std::size_t v2 = v1 + 1; v2 < v1 + MAX_DUPLICATE_SEPARATION + 1 && v2 < num_vertices; ++v2 )
		{
			AseVertex& vert2 = m_expanded[v2];
			if( vert2.m_index_position != AseVertex::INVALID ) continue;
			if( vert1 == vert2 )
			{
				vert2.m_index_position = vert1.m_index_position;
			}
		}
	}

	// Copy the vertices into the mesh
	mesh.m_vertex.clear();
	mesh.m_vertex.resize(num_unique_verts);
	AseVertex* ase_vertex = &m_expanded[0];
	for( std::size_t v = 0; v < num_unique_verts; ++v )
	{
		while( ase_vertex->m_index_position < v )
		{
			++ase_vertex;
			PR_ASSERT(PR_DBG_ASELOADER, ase_vertex <= &m_expanded[num_vertices - 1]);
		}
		mesh.m_vertex[v] = *static_cast<Vertex*>(ase_vertex);
		++ase_vertex;
	}

	// Copy the faces into the mesh
	mesh.m_face.clear();
	mesh.m_face.resize(num_faces);
	for( std::size_t f = 0; f < num_faces; ++f )
	{
		AseFace&	aseface = m_face[f];
		Face&	face	= mesh.m_face[f];

		face.m_flags		= 0;
		face.m_mat_index	= aseface.m_mat_index;
		for( int i = 0; i < 3; ++i )
		{
			// Update the indices of the faces while copying them into the mesh
			face.m_vert_index[i] = (WORD)m_expanded[f * 3 + i].m_index_position;
			PR_ASSERT(PR_DBG_ASELOADER, face.m_vert_index[i] < mesh.m_vertex.size());
		}
	}

	// Copy the materials into the mesh
	mesh.m_material.clear();
	mesh.m_material.resize(m_material_map.size());
	for( std::list<IndexMap>::const_iterator map = m_material_map.begin(), map_end = m_material_map.end(); map != map_end; ++map )
	{
		mesh.m_material[map->m_dst_index] = m_material[map->m_src_index];
	}
}









//*****
// Skip over white space
void AseLoader::SkipWhiteSpace()
{
	while( m_pos < m_count && isspace(m_source[m_pos]) )
		++m_pos;
}

//*****
// Moves 'm_pos' to one past the '{' character
bool AseLoader::FindSectionStart()
{
	while( m_pos < m_count )
	{
		if( m_source[m_pos] == '{' ) { ++m_pos; return true; }
		if( m_source[m_pos] == '}' )
		{
			++m_pos;
			return false;
		}
		if( ++m_pos == m_count ) return false;
	}
	return false;
}

//*****
// Moves 'm_pos' to one past the '}' character
bool AseLoader::FindSectionEnd()
{
	while( m_pos < m_count )
	{
		if( m_source[m_pos] == '}' ) { ++m_pos; return true; }
		if( ++m_pos == m_count ) return false;
	}
	return false;
}

//*****
// Scans from 'm_pos' to the first '*' char or end of geometry
// If an '{' char is encountered then scanning skips everything until a '}' is found
bool AseLoader::GetKeyWord()
{
	while( m_load_result == S_OK && m_pos < m_count )
	{
		if( m_source[m_pos] == '*' )
		{
			++m_pos;
			return ExtractWord(m_keyword);
		}

		if( m_source[m_pos] == '{' )
		{
			++m_pos;
			if( !SkipSection() ) return false;
		}

		if( m_source[m_pos] == '}' ) return false;
		if( ++m_pos == m_count ) return false;
	}
	return false;
}

//*****
// Scan for the next keyword but don't update our position in the source data
bool AseLoader::PeekKeyWord()
{
	std::size_t pos = m_pos;
	bool result = GetKeyWord();
	m_pos = pos;
	return result;
}

//*****
// Extracts characters between '"'
bool AseLoader::ExtractString(std::string& word)
{
	SkipWhiteSpace();
	
	word = "";
	if( m_source[m_pos] != '"' ) return false;
	++m_pos;
	while( m_pos < m_count && m_source[m_pos] != '"' )
	{
		word += m_source[m_pos];
		if( ++m_pos == m_count ) return false;
	}
	return true;
}

//*****
// Extracts characters upto a white space
bool AseLoader::ExtractWord(std::string& word)
{
	SkipWhiteSpace();
	
	word = "";
	while( m_pos < m_count && m_source[m_pos] != '*' && !isspace(m_source[m_pos]) )
	{
		word += m_source[m_pos];
		if( ++m_pos == m_count ) return false;
	}
	return true;
}

//*****
// Read a std::size_t from the source data
bool AseLoader::ExtractSizeT(std::size_t& _sizet, int radix)
{
	SkipWhiteSpace();
	
	std::string sizet_str;
	while( m_pos < m_count && m_source[m_pos] != '*' && isdigit(m_source[m_pos]) )
	{
		sizet_str += m_source[m_pos];
		if( ++m_pos == m_count ) return false;
	}
	_sizet = strtoul(sizet_str.c_str(), 0, radix);
	return true;
}

//*****
// Read a float from the source data
bool AseLoader::ExtractFloat(float& real)
{
	SkipWhiteSpace();
	
	std::string float_str;
	while( m_pos < m_count && m_source[m_pos] != '*' && (isdigit(m_source[m_pos]) || m_source[m_pos] == '.' || m_source[m_pos] == '-') )
	{
		float_str += m_source[m_pos];
		if( ++m_pos == m_count ) return false;
	}
	real = static_cast<float>(strtod(float_str.c_str(), 0));
	return true;
}

//*****
// Skip over a '{' '}' section
bool AseLoader::SkipSection()
{
	while( m_pos < m_count && m_source[m_pos] != '}' )
	{
		if( ++m_pos == m_count ) return false;
	}
	++m_pos;
	return true;
}
