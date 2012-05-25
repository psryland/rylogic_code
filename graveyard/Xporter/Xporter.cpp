//*********************************************************************************
//
// Script Function for MAX
//
//*********************************************************************************
#define DONT_DEFINE_THE_MATH_FUNCTIONS

#include "Headers.h"
#include "Geometry\XFile.h"
#include "Geometry\XSaver\XSaver.h"
#include "Common\Utils.h"
#include "Common\PRString.h"
#include "Utility.h"

// Max thingy to make "Xporter" visible in MAX
def_visible_primitive( Xport, "XExport");
#pragma comment(lib, "d3d9.lib")
#pragma message(PR_LINK "Linking to d3d9.lib")
#pragma comment(lib, "d3dx9.lib")
#pragma message(PR_LINK "Linking to d3dx9.lib")
#pragma comment(lib, "d3dxof.lib")
#pragma message(PR_LINK "Linking to d3dxof.lib")
#pragma comment(lib, "dxguid.lib")
#pragma message(PR_LINK "Linking to dxguid.lib")
#pragma comment(lib, "dxerr9.lib")
#pragma message(PR_LINK "Linking to dxerr9.lib")
#pragma comment(lib, "Maxscrpt.lib")
#pragma message(PR_LINK "Linking to Maxscrpt.lib")
#pragma comment(lib, "core.lib")
#pragma message(PR_LINK "Linking to core.lib")
#pragma comment(lib, "maxutil.lib")
#pragma message(PR_LINK "Linking to maxutil.lib")
#pragma comment(lib, "bmm.lib")
#pragma message(PR_LINK "Linking to bmm.lib")
#pragma comment(lib, "geom.lib")
#pragma message(PR_LINK "Linking to geom.lib")
#pragma comment(lib, "mesh.lib")
#pragma message(PR_LINK "Linking to mesh.lib")

//*****
// Constamts
const DWORD INVALID_INDEX = 0xFFFFFFFF;

//*****
// Structures
struct Vert
{
	DWORD	m_max_vert;
	Point3	m_vertex;
	Point3	m_normal;
	DWORD	m_max_tex;
	Point3	m_tex_coord;
	DWORD	m_smoothing_group;
	DWORD	m_myindex;
	DWORD	m_refindex;
};

//*****
// Class to wrap exporting
class XExporter
{
public:
	XExporter(Interface* max_interface) : m_max_interface(max_interface) {}

	String*	ReturnResult();
	void	DoExport(const TSTR& filename);

private:
	D3DXVECTOR2		P3toD3D2(const Point3& point)						{ return D3DXVECTOR2(point.x, point.y); }
	D3DXVECTOR3		P3toD3D3(const Point3& point)						{ return D3DXVECTOR3(point.x, point.y, point.z); }
	D3DXVECTOR3		MaxToGameRS(const Point3& point)					{ return P3toD3D3((g_Max_Transform * point) * g_Max_Scale); }
	D3DXVECTOR3		MaxToGameR(const Point3& point)						{ return P3toD3D3(g_Max_Transform * point); }
	D3DCOLORVALUE	D3DColourValue(float r, float g, float b, float a)	{ D3DCOLORVALUE c; c.r = r; c.g = g; c.b = b; c.a = a; return c; }
	D3DCOLORVALUE	ColorToD3DCOLORVALUE(const Color& colour)			{ return D3DColourValue(colour.r, colour.g, colour.b, 1.0f); }

	bool	ExportNode(XFrame& frame, INode* node);
	bool	CheckMesh(Mesh* mesh);
	void	BuildMaterialList(INode* node, XMesh& xmesh);
	void	BuildVertexList(Mesh* mesh, PR::Array<Vert, true>& vert_list, DWORD& num_unique_verts);
	void	RemoveDegeneracy(PR::Array<Vert, true>& vert_list);

private:
	Interface*	m_max_interface;
	std::string	m_result;
};

//*********************************************************************************************
// Max script function
Value* Xport_cf(class Value** arg_list, int count)
{
	Interface* max_interface = TheManager->Max();
	if( !max_interface )						return new String(_T("Failed to get the MAX interface"));
	if( max_interface->GetSelNodeCount() == 0 ) return new String(_T("Nothing selected."));

	// Access name of current file
	TSTR filename = max_interface->GetCurFilePath();
	if( filename.data()[0] == '\0' ) return new String(_T("Please save the MAX scene first."));

	XExporter xexporter(max_interface);
	xexporter.DoExport(filename);
	return xexporter.ReturnResult();
}

//*********************************************************************************************
// XExporter implementation
//*****
// Return a result string
String*	XExporter::ReturnResult()
{
	return new String(_T(m_result));
}

//*****
// Export the selected nodes
void XExporter::DoExport(const TSTR& filename)
{
	// Create an XFile and set the file name
	XFile xfile;
	xfile.SetXFilename(filename.data());

	// Add the meshes to the x file
	int	num_nodes = m_max_interface->GetSelNodeCount();
	for( int inode = 0; inode < num_nodes; ++inode )
	{
		INode* node = m_max_interface->GetSelNode(inode);
		if( node )
		{
			// Stick each nodes in a frame
			XFrame frame;
			if( ExportNode(frame, node) )
			{
				xfile.m_frame.push_back(frame);
			}
		}
	}
	
	// Write out the xfile
	XSaver XSaver;
	if( FAILED(!XSaver.Save(xfile)) )
	{
		m_result += "Failed to write X File.";
		return;
	}
	m_result = "Export done.";

}

//*****
// Export a node into 'xfile'
bool XExporter::ExportNode(XFrame& frame, INode* node)
{
	XMesh xmesh;

	// Get the mesh from MAX
	Mesh* mesh = GetMesh(m_max_interface, node);
	if( mesh )
	{
		if( !CheckMesh(mesh) ) return false;
		bool is_textured = mesh->numTVerts > 0;

		// Build up a list of materials
		BuildMaterialList(node, xmesh);
		
		// Build up a list of vertices
		PR::Array<Vert, true> vert_list;
		DWORD num_unique_verts = 0;
		BuildVertexList(mesh, vert_list, num_unique_verts);

		// Pre-allocate vertex, index, normal vectors
		xmesh.m_vertex.resize(num_unique_verts);
		xmesh.m_normal.resize(num_unique_verts);
		xmesh.m_face.resize(mesh->numFaces);
		if( is_textured ) xmesh.m_tex_coord.resize(num_unique_verts);
		
		// Create the XMesh
		xmesh.SetName(node->GetName());
		xmesh.m_num_indices = mesh->numFaces * 3;
		for( int f = 0; f < mesh->numFaces; ++f )
		{
			XFace face;
			Face& max_face	 = mesh->faces[f];
			face.m_mat_index = (is_textured) ? (max_face.getMatID() % xmesh.m_material.size()) : (0);

			face.m_vert_index.resize(3);
			face.m_norm_index.resize(3);
			for( int i = 0; i < 3; ++i )
			{
				DWORD index		= f * 3 + i;
				DWORD myindex	= vert_list[index].m_myindex;
				DWORD refindex	= vert_list[index].m_refindex;
				if( refindex != INVALID_INDEX )
				{
					myindex = vert_list[refindex].m_myindex;
					PR_ASSERT(myindex != INVALID_INDEX);
				}				
				
				PR_ASSERT(myindex < num_unique_verts);
				PR_ASSERT(vert_list[index].m_max_vert == max_face.v[i]);

				xmesh.m_vertex[myindex] = v4(MaxToGameRS(vert_list[index].m_vertex), 1.0f);
				xmesh.m_normal[myindex]	= v4(MaxToGameR (vert_list[index].m_normal), 0.0f);
				if( is_textured ) xmesh.m_tex_coord[myindex] = P3toD3D2(vert_list[index].m_tex_coord);

				face.m_vert_index[i] = myindex;
				face.m_norm_index[i] = myindex;
			}
			
			xmesh.m_face[f] = face;
		}
	}
	frame.m_mesh.push_back(xmesh);	
	return true;
}

//*****
// Fix up the mesh
bool XExporter::CheckMesh(Mesh* mesh)
{
	mesh->DeleteIsoVerts();
	mesh->DeleteIsoMapVerts();
//	mesh->RemoveDegenerateFaces();	// Don't call this, it crashes Max...
	mesh->RemoveIllegalFaces();
	mesh->buildNormals();
	mesh->buildRenderNormals();
	mesh->faceSel.ClearAll();
	return true;
}

//*****
// Add materials to the xmesh
void XExporter::BuildMaterialList(INode* node, XMesh& xmesh)
{
	Mtl* base_material = node->GetMtl();
	if( !base_material ) return;	// No material assigned

	// Get the number of materials assigned
	int num_materials = 0;
		 if( base_material->ClassID() == Class_ID(DMTL_CLASS_ID,  0) )	num_materials = 1;
	else if( base_material->ClassID() == Class_ID(MULTI_CLASS_ID, 0) )	num_materials = base_material->NumSubMtls();
	else PR_ERROR_STR("Unknown material class");
	
	// Process each material
	xmesh.m_material.resize(num_materials);
	for( int i = 0; i < num_materials; ++i )
	{
		Mtl* material = base_material;
		if( material->ClassID() == Class_ID(MULTI_CLASS_ID, 0) )
			material = material->GetSubMtl(i);

		// We can only deal with standard MAX materials
		if( material->ClassID() == Class_ID(DMTL_CLASS_ID, 0) )
		{
			StdMat* stdmaterial = (StdMat*)material;
			Color	diffuse		= stdmaterial->GetDiffuse(0);
			Color	specular	= stdmaterial->GetSpecular(0);
			Color	emissive	= stdmaterial->GetAmbient(0);
			
			XMaterial material;
			material.m_material.Diffuse  = ColorToD3DCOLORVALUE(diffuse);
			material.m_material.Specular = ColorToD3DCOLORVALUE(specular);
			material.m_material.Emissive = ColorToD3DCOLORVALUE(emissive);
			material.m_material.Ambient  = material.m_material.Diffuse;
			material.m_material.Power	 = stdmaterial->GetShinStr(0);	// Shininess strenght

			// If the material has a texture...(check if diffuse map is enabled)
			if( stdmaterial->MapEnabled(ID_DI) )
			{
				Texmap *texture_map = stdmaterial->GetSubTexmap(ID_DI);
				if( texture_map && texture_map->ClassID() == Class_ID(BMTEX_CLASS_ID, 0) )
				{
					BitmapTex *bmptexture = (BitmapTex*)texture_map;
					Strncpy(material.m_texture_filename, bmptexture->GetMapName(), MAX_NAME_LENGTH);
					
					int length = strlen(material.m_texture_filename);
					for( int i = 0; i < length; ++i )
						if( material.m_texture_filename[i] == '\\' )
							material.m_texture_filename[i] = '/';
				}
			}
			xmesh.m_material[i] = material;
		}
	}
}

//*****
// Construct a list of vertices from the mesh.
void XExporter::BuildVertexList(Mesh* mesh, PR::Array<Vert, true>& vert_list, DWORD& num_unique_verts)
{
	bool is_textured = mesh->numTVerts > 0;
	vert_list.resize(mesh->numFaces * 3);

	int v = 0;
	Point3 v0, v1, v2;
	for( int f = 0; f < mesh->numFaces; ++f )
	{
		Face& max_face	 = mesh->faces[f];
	
		// Calculate the face normal
		v0 = mesh->verts[max_face.v[0]];
		v1 = mesh->verts[max_face.v[1]];
		v2 = mesh->verts[max_face.v[2]];
		Point3 face_normal = (v1 - v0)^(v2 - v1);

		// Make a vertex for each vertex of each face
		for( int i = 0; i < 3; ++i )
		{
			Vert vert;
			vert.m_max_vert		= max_face.v[i];
			vert.m_vertex		= mesh->verts[max_face.v[i]];
			vert.m_normal		= face_normal;
			vert.m_max_tex		= 0;
			vert.m_tex_coord	= Point3(0.0f, 0.0f, 0.0f);
			if( is_textured )
			{
				PR_ASSERT(mesh->tVerts && (DWORD)mesh->numTVerts > mesh->tvFace[f].t[i]);
				vert.m_max_tex		= mesh->tvFace[f].t[i];
				vert.m_tex_coord	= mesh->tVerts[mesh->tvFace[f].t[i]];
				vert.m_tex_coord.y	= 1.0f - vert.m_tex_coord.y;
			}
			vert.m_smoothing_group = max_face.smGroup;
			vert.m_refindex		= INVALID_INDEX;
			vert.m_myindex		= INVALID_INDEX;
			
			vert_list[v] = vert;
			++v;
		}
	}

	// Combine vertex normals for vertices that are equal and from the same smoothing group
	int i, size = vert_list.size();
	for( i = 0; i < size; ++i )
	{
		for( int j = i + 1; j < size; ++j )
		{
			if( vert_list[i].m_max_vert	== vert_list[j].m_max_vert &&				// If the vertices are equal
			   (vert_list[i].m_smoothing_group & vert_list[j].m_smoothing_group) )	// and they're from the same smoothing group
			{
				Point3 norm_i = vert_list[i].m_normal;
				Point3 norm_j = vert_list[j].m_normal;
				vert_list[i].m_normal += norm_j;
				vert_list[j].m_normal += norm_i;
				vert_list[i].m_smoothing_group |= vert_list[j].m_smoothing_group;
				vert_list[j].m_smoothing_group |= vert_list[i].m_smoothing_group;
			}
		}
		vert_list[i].m_normal.Unify();
	}

	// Set the index position of the vertices and combine vertices that are the same
	num_unique_verts = 0;
	for( i = 0; i < size; ++i )
	{
		if( vert_list[i].m_refindex != INVALID_INDEX )
			continue;

		vert_list[i].m_myindex = num_unique_verts;
		++num_unique_verts;
		for( int j = i + 1; j < size; ++j )
		{
			if( vert_list[j].m_refindex != INVALID_INDEX )
				continue;

			if( vert_list[i].m_max_vert == vert_list[j].m_max_vert &&
				vert_list[i].m_max_tex  == vert_list[j].m_max_tex  &&
			   (vert_list[i].m_smoothing_group & vert_list[j].m_smoothing_group) )
			{
				vert_list[j].m_refindex = i;
			}
		}
	}
}

