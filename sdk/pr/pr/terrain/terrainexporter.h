//*******************************************************************************
// Terrain Exporter
//  Copyright © Rylogic Ltd 2009
//*******************************************************************************
#pragma once
#include <vector>
#include <string>
#include "pr/maths/maths.h"
#include "terrainexporter/forward.h"
#include "terrainexporter/vertex.h"
#include "terrainexporter/face.h"
#include "terrainexporter/edge.h"
#include "terrainexporter/cellex.h"

namespace pr
{
	class TerrainExporter
	{
		// Region parameters
		pr::v4							m_region_origin;			// The offset to the origin of the region (minx, minz)
		pr::FRect						m_region_rect;				// A 2D rectangle for the region (in region space)
		int								m_divisionsX;				// Determines the width of each cell in the region
		int								m_divisionsZ;				// Determines the depth of each cell in the region

		// Source data
		pr::terrain::TVertDict			m_vert_dict;				// A dictionary of vertex indices so that 'm_verts' can contain unique verts only
		pr::terrain::TVertVec			m_verts;					// The verts, faces, and edges of the source data
		pr::terrain::TFaceVec			m_faces;					//  for the terrain.
		pr::terrain::TEdgeSet			m_edges;					// 
		pr::uint						m_face_id;					// Counter for assigning unique ids to faces

		// Generated data
		pr::terrain::TCellExList		m_cell;						// A list of the terrain cells

	public:
		TerrainExporter();

		// This function is called to reset everything in preparation for creating
		// a new region. The parameters of this function are:
		//  'region_origin' - The world co-ordinate for the minX,minZ corner of the region.
		//  'region_sizeX' - The size of the X dimension of the region.
		//  'region_sizeZ' - The size of the Z dimension of the region.
		//  'divisionsX' - The number of divisions to make in the X direction.
		//  'divisionsZ' - The number of divisions to make in the Z direction.
		// The total number of terrain cells in the region will be at least 'divisionsX * divisionsZ'.
		pr::terrain::EResult CreateRegion(pr::v4 const& region_origin, float region_sizeX, float region_sizeZ, int divisionsX, int divisionsZ);

		// Add a single face to the terrain data
		// 'v0', 'v1', 'v2' should be in world space
		// The face normal is assumed to be Cross(v2-v1, v0-v1)
		// 'material_id' is the id of the material for the face
		pr::terrain::EResult AddFace(pr::v4 const& v0, pr::v4 const& v1, pr::v4 const& v2, pr::uint material_id);

		// When all data has been added, these functions are used to generate the terrain height data.
		//	'data' - The terrain height data will be written to the provided buffer.
		//  'thd_filename' - The terrain height data will be written to a file with filename 'thd_filename'.
		pr::terrain::EResult CloseRegion(std::vector<unsigned char>& data);
		pr::terrain::EResult CloseRegion(std::string const& thd_filename);
	};
}//namespace pr

