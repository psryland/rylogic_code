//********************************
// Geometry
//  Copyright (c) Rylogic Ltd 2013
//********************************
#pragma once

#include "pr/geometry/common.h"
#include "pr/geometry/models_sphere.h"

namespace pr::geometry
{
	// Notes:
	//  - Skyboxes and environment maps are closely related, typically both share the same texture.
	//  - DX texture cubes are made from 6 textures as they would appear on the *outside* of a box.
	//    however skyboxes are always viewed from inside. Because of this, skybox models should all
	//    use front face culling so that the texture mapping is the same as for environment maps.

	// Returns the number of verts and number of indices needed to hold geometry for a geosphere.
	template <typename Tvr, typename Tir>
	void SkyboxGeosphereSize(int divisions, Tvr& vcount, Tir& icount)
	{
		GeosphereSize(divisions, vcount, icount);
	}

	// Creates a geosphere.
	template <typename TVertIter, typename TIdxIter>
	Props SkyboxGeosphere(float radius, int divisions, Colour32 colour, TVertIter out_verts, TIdxIter out_indices)
	{
		// Remember to use front face culling
		return Geosphere(radius, divisions, colour, out_verts, out_indices);
	}

	// Returns the number of verts and number of indices needed to hold geometry for a five sided cubic dome.
	template <typename Tvr, typename Tir>
	void SkyboxFiveSidedCubicDomeSize(Tvr& vcount, Tir& icount)
	{
		vcount = checked_cast<Tvr>(12);
		icount = checked_cast<Tir>(30);
	}

	// Creates a five sided cube (bottom face removed).
	// The texture coordinates expect a 't' shaped texture where the centre half
	// of the texture is the top (+y), and the l,t,r,b quarters are the walls.
	template <typename TVertIter, typename TIdxIter>
	Props SkyboxFiveSidedCubicDome(float radius, Colour32 colour, TVertIter out_verts, TIdxIter out_indices)
	{
		// The texture should be like:
		//        |    |    
		//     ___|_-Z_|____
		//        |    |   
		//     -X | +Y | +X 
		//     ___|____|____
		//        | +Z |
		//        |    |
		// *Viewed from the outside*. Imagine a table cloth draped over a table.
		// Remember to use front face culling.

		using VIdx = typename std::iterator_traits<TIdxIter>::value_type;

		Props props;
		props.m_geom = EGeom::Vert | EGeom::Tex0;
		props.m_bbox = BBox(v4Origin, v4{ radius });
		props.m_has_alpha = HasAlpha(colour);

		// Set the verts
		auto v_out = out_verts;
		float const s = radius;
		SetPCNT(*v_out++, v4{-s, +s, +s, 1}, colour, v4Zero, v2{ 0.25f, 0.75f}); //0 // +Y
		SetPCNT(*v_out++, v4{+s, +s, +s, 1}, colour, v4Zero, v2{ 0.75f, 0.75f}); //1
		SetPCNT(*v_out++, v4{+s, +s, -s, 1}, colour, v4Zero, v2{ 0.75f, 0.25f}); //2
		SetPCNT(*v_out++, v4{-s, +s, -s, 1}, colour, v4Zero, v2{ 0.25f, 0.25f}); //3

		SetPCNT(*v_out++, v4{-s, -s, +s, 1}, colour, v4Zero, v2{ 0.25f, 0.75f}); //4 // +Z
		SetPCNT(*v_out++, v4{+s, -s, +s, 1}, colour, v4Zero, v2{ 0.25f, 1.25f}); //5
		
		SetPCNT(*v_out++, v4{+s, -s, +s, 1}, colour, v4Zero, v2{ 1.25f, 0.75f}); //6 // +X
		SetPCNT(*v_out++, v4{+s, -s, -s, 1}, colour, v4Zero, v2{ 1.25f, 0.25f}); //7

		SetPCNT(*v_out++, v4{+s, -s, -s, 1}, colour, v4Zero, v2{ 0.75f,-0.25f}); //8 // -Z
		SetPCNT(*v_out++, v4{-s, -s, -s, 1}, colour, v4Zero, v2{ 0.25f,-0.25f}); //9
		
		SetPCNT(*v_out++, v4{-s, -s, -s, 1}, colour, v4Zero, v2{-0.25f, 0.25f}); //10 // -X
		SetPCNT(*v_out++, v4{-s, -s, +s, 1}, colour, v4Zero, v2{-0.25f, 0.75f}); //11

		// Set the faces
		VIdx const indices[] =
		{
			0,  1,  2,  0,  2,  3,
			0,  4,  5,  0,  5,  1,
			1,  6,  7,  1,  7,  2,
			2,  8,  9,  2,  9,  3,
			3, 10, 11,  3, 11,  0,
		};
		for (int i = 0; i != _countof(indices); ++i)
			*i_out++ = indices[i];

		return props;
	}

	// Returns the number of verts and number of indices needed to hold geometry for a six sided cube.
	template <typename Tvr, typename Tir>
	void SkyboxSixSidedCubeSize(Tvr& vcount, Tir& icount)
	{
		vcount = checked_cast<Tvr>(24);
		icount = checked_cast<Tir>(36);
	}

	// Creates a cube.
	// The texture coordinates expect one texture per face.
	// Face order is: +x, -x, +y, -y, +z, -z (same order as cube map textures)
	template <typename TVertIter, typename TIdxIter>
	Props SkyboxSixSidedCube(float radius, Colour32 colour, TVertIter out_verts, TIdxIter out_indices)
	{
		// The texture should be like:
		//        |    |    
		//     ___|_+Y_|________
		//        |    |   	|
		//     -X | +Z | +X | -Z
		//     ___|____|____|___
		//        | -Y |
		//        |    |
		// *Viewed from the outside*. +Z is the nearest face, the others wrap away from you around a box.
		// Remember to use front face culling.
		using VIdx = typename std::iterator_traits<TIdxIter>::value_type;

		Props props;
		props.m_geom = EGeom::Vert | EGeom::Tex0;
		props.m_bbox = BBox(v4Origin, v4{ radius });
		props.m_has_alpha = HasAlpha(colour);

		// Set the verts
		auto v_out = out_verts;
		float const s = radius;
		SetPCNT(*v_out++, v4{+s, +s, +s, 1}, colour, v4Zero, v2{0, 0}); //  0 // +X
		SetPCNT(*v_out++, v4{+s, -s, +s, 1}, colour, v4Zero, v2{0, 1}); //  1
		SetPCNT(*v_out++, v4{+s, -s, -s, 1}, colour, v4Zero, v2{1, 1}); //  2
		SetPCNT(*v_out++, v4{+s, +s, -s, 1}, colour, v4Zero, v2{1, 0}); //  3

		SetPCNT(*v_out++, v4{-s, +s, -s, 1}, colour, v4Zero, v2{0, 0}); //  4 // -X
		SetPCNT(*v_out++, v4{-s, -s, -s, 1}, colour, v4Zero, v2{0, 1}); //  5
		SetPCNT(*v_out++, v4{-s, -s, +s, 1}, colour, v4Zero, v2{1, 1}); //  6
		SetPCNT(*v_out++, v4{-s, +s, +s, 1}, colour, v4Zero, v2{1, 0}); //  7
		
		SetPCNT(*v_out++, v4{-s, +s, -s, 1}, colour, v4Zero, v2{0, 0}); //  8 // +Y
		SetPCNT(*v_out++, v4{-s, +s, +s, 1}, colour, v4Zero, v2{0, 1}); //  9
		SetPCNT(*v_out++, v4{+s, +s, +s, 1}, colour, v4Zero, v2{1, 1}); // 10
		SetPCNT(*v_out++, v4{+s, +s, -s, 1}, colour, v4Zero, v2{1, 0}); // 11
		
		SetPCNT(*v_out++, v4{-s, -s, +s, 1}, colour, v4Zero, v2{0, 0}); // 12 // -Y
		SetPCNT(*v_out++, v4{-s, -s, -s, 1}, colour, v4Zero, v2{0, 1}); // 13
		SetPCNT(*v_out++, v4{+s, -s, -s, 1}, colour, v4Zero, v2{1, 1}); // 14
		SetPCNT(*v_out++, v4{+s, -s, +s, 1}, colour, v4Zero, v2{1, 0}); // 15

		SetPCNT(*v_out++, v4{-s, +s, +s, 1}, colour, v4Zero, v2{0, 0}); // 16 // +Z
		SetPCNT(*v_out++, v4{-s, -s, +s, 1}, colour, v4Zero, v2{0, 1}); // 17
		SetPCNT(*v_out++, v4{+s, -s, +s, 1}, colour, v4Zero, v2{1, 1}); // 18
		SetPCNT(*v_out++, v4{+s, +s, +s, 1}, colour, v4Zero, v2{1, 0}); // 19

		SetPCNT(*v_out++, v4{+s, +s, -s, 1}, colour, v4Zero, v2{0, 0}); // 20 // -Z
		SetPCNT(*v_out++, v4{+s, -s, -s, 1}, colour, v4Zero, v2{0, 1}); // 21
		SetPCNT(*v_out++, v4{-s, -s, -s, 1}, colour, v4Zero, v2{1, 1}); // 22
		SetPCNT(*v_out++, v4{-s, +s, -s, 1}, colour, v4Zero, v2{1, 0}); // 23

		// Set the faces
		VIdx const indices[] =
		{
			0,  1,   2,  0,  2,  3, // 0 - 6
			4,  5,   6,  4,  6,  7, // 6 - 12
			8,  9,  10,  8, 10, 11, // 12 - 18
			12, 13, 14, 12, 14, 15, // 18 - 24
			16, 17, 18, 16, 18, 19, // 24 - 30
			20, 21, 22, 20, 22, 23, // 30 - 36
		};
		auto i_out = out_indices;
		for (int i = 0; i != _countof(indices); ++i)
			*i_out++ = indices[i];

		return props;
	}
}
