//*********************************************************************************
//
// Script Function for MAX
//
//*********************************************************************************

#include "Headers.h"
#include "Common\PRList.h"
#include "Common\PRString.h"
#include "Common\Fmt.h"
#include "Common\MsgBox.h"
#include "Utility.h"

// Max thingy to make "Check" visible in MAX
def_visible_primitive( Check, "Check");

float g_Min_Vertex_Separation		= 0.1f;
float g_Min_Tex_Coord_Separation	= 0.001f;
float g_Min_Face_Area				= 0.001f;

//*****
// Class to wrap the checker
class Checker
{
public:
	Checker(Interface* max_interface) : m_max_interface(max_interface), m_bad_vertices(10) {}
	
	void	StartCheck();
	void	CheckVertices(Mesh* mesh);
	void	CheckTextureVertices(Mesh* mesh);
	void	CheckFaces(Mesh* mesh);
	String*	ReturnResult();

private:
	Interface*			m_max_interface;
	std::string			m_result;
	PR::List<int, true>	m_bad_vertices;
};

//*********************************************************************************************
// Max script function
Value* Check_cf(class Value** arg_list, int count)
{
	if( count >= 1 )	g_Min_Vertex_Separation		= arg_list[0]->to_float();
	if( count >= 2 )	g_Min_Tex_Coord_Separation	= arg_list[1]->to_float();
	if( count >= 3 )	g_Min_Face_Area				= arg_list[2]->to_float();

	Interface* max_interface = TheManager->Max();
	if( !max_interface ) return new String(_T("Failed to get the MAX interface"));
	
	int	num_nodes = max_interface->GetSelNodeCount();
	if( num_nodes == 0 ) return new String(_T("Nothing selected."));

	Checker checker(max_interface);
	
	// Add the meshes to the x file
	for( int inode = 0; inode < num_nodes; ++inode )
	{
		INode* node = max_interface->GetSelNode(inode);
		if( node )
		{
			Mesh* mesh = GetMesh(max_interface, node);
			if( mesh )
			{
				checker.StartCheck();
				checker.CheckVertices(mesh);
				checker.CheckFaces(mesh);
			}
		}
	}

	max_interface->RedrawViews(0);
	return checker.ReturnResult();
}

//*********************************************************************************************
// Checker implementation
//*****
// Return a result
String*	Checker::ReturnResult()
{
	if( m_result.Length() == 0 ) return new String(_T("Passed."));
	return new String(_T(m_result));
}

//*****
// Initialise things before doing the check
void Checker::StartCheck()
{
	m_bad_vertices.Destroy();
}

//*****
// Check for vertices that are too close together. Returns true if none are found
void Checker::CheckVertices(Mesh* mesh)
{
	bool found_one = false;
	for( int i = 0; i < mesh->numVerts; ++i )
	{
		for( int j = i + 1; j < mesh->numVerts; ++j )
		{
			Point3 diff = mesh->verts[i] - mesh->verts[j];
			
			if( diff.Length() < g_Min_Vertex_Separation )
			{
				for( DWORD k = 0; k < m_bad_vertices.GetCount(); ++k )
					if( m_bad_vertices[k] == i )
						break;

				if( k == m_bad_vertices.GetCount() )
				{
					if( !found_one )
					{
						found_one = true;
						m_result += "Close Vertices: ";
					}
					mesh->vertSel.Set(i);
					mesh->vertSel.Set(j);
					m_result += Fmt("%d-%d,", i + 1, j + 1);
					m_bad_vertices.ExtendTail() = i;
				}
			}
		}
	}
	if( found_one )	m_result += "\n";
}

//*****
// Check for faces whose area is too small
void Checker::CheckFaces(Mesh* mesh)
{
	// Check for small faces
	bool found_one = false;
	for( int f = 0; f < mesh->numFaces; ++f )
	{
		Face& face = mesh->faces[f];
		Point3 side1	= mesh->verts[face.v[1]] - mesh->verts[face.v[0]];
		Point3 side2	= mesh->verts[face.v[2]] - mesh->verts[face.v[0]];
		if( (side1 ^ side2).Length() / 2.0f < g_Min_Face_Area )
		{
			if( !found_one )
			{
				found_one = true;
				m_result += "Small Faces: ";
			}
			m_result += Fmt("%d,", f + 1);
			mesh->faceSel.Set(f);
		}		
	}
	if( found_one )	m_result += "\n";

	// Check for bad texture vertices
	found_one = false;
	for( f = 0; f < mesh->numFaces; ++f )
	{
		TVFace& face = mesh->tvFace[f];
		
		Point3 t0 = mesh->tVerts[face.t[0]];
		Point3 t1 = mesh->tVerts[face.t[1]];
		Point3 t2 = mesh->tVerts[face.t[2]];

		if( (t1 - t0).Length() < g_Min_Tex_Coord_Separation ||
			(t2 - t1).Length() < g_Min_Tex_Coord_Separation ||
			(t0 - t2).Length() < g_Min_Tex_Coord_Separation )
		{
			if( !found_one )
			{
				found_one = true;
				m_result += "Faces with bad texture coords: ";
			}
			m_result += Fmt("%d,", f + 1);
			mesh->faceSel.Set(f);
		}
	}	
}