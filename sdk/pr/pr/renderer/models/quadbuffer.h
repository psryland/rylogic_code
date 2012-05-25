//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************

#pragma once
#ifndef PR_RDR_QUAD_BUFFER_H
#define PR_RDR_QUAD_BUFFER_H

#include "pr/renderer/types/forward.h"
#include "pr/renderer/models/model.h"

namespace pr
{
	namespace rdr
	{
		// This class is a helper object for creating models containing quads
		struct QuadBuffer
		{
			enum EState { EState_Idle, EState_Adding };
			Renderer*         m_rdr;
			std::size_t       m_num_quads;
			EState            m_state;
			model::VLock      m_vlock;
			vf::iterator      m_vb;
			pr::rdr::ModelPtr m_model;
			
			QuadBuffer(Renderer& rdr, std::size_t num_quads);
			
			// Called before and after the add calls, saves excessive locking/unlocking of the model buffer
			void Begin();
			void End();
			
			// This is a quad whose verts are in world space but always faces the camera
			// This methods adds 4 verts at the same position, the shader moves them to the correct positions.
			// 'centre' is the centre of the billboard in world space
			// 'corner' is an array of 4 vectors pointing to the corners of the billboard in camera space
			// 'colour' is an array of 4 vertex colours
			// 'tex' is an array of 4 texture coords
			// Note: vertex order is: TL, BL, TR, BR
			void AddBillboard(std::size_t index, v4 const& centre, v4* corner, Colour32* colour, v2* tex);
			void AddBillboard(std::size_t index, v4 const& centre, float width, float height);
			void AddBillboard(std::size_t index, v4 const& centre, float width, float height, Colour32 colour);
			
			// This is a quad whose verts are in screen space
			// x,y = [-1, 1], z = [0,1], orthographic projection
			void AddSprite();
		};
	}
}

#endif
