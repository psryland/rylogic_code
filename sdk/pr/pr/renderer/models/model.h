//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************

#pragma once
#ifndef PR_RDR_MODEL_H
#define PR_RDR_MODEL_H

#include "pr/renderer/models/types.h"
#include "pr/renderer/models/rendernugget.h"
#include "pr/renderer/renderstates/renderstate.h"
#include "pr/renderer/utility/errors.h"

namespace pr
{
	namespace rdr
	{
		struct Model :pr::RefCount<Model>
		{
			ModelBufferPtr m_model_buffer;  // The buffer that contains this models vertex and index data
			model::Range   m_Vrange;        // The first and number of verties for this model within 'm_model_buffer'
			model::Range   m_Irange;        // The first and number of indices for this model within 'm_model_buffer'
			TNuggetChain   m_render_nugget; // The nuggets for this model
			BoundingBox    m_bbox;          // A bounding box for the model. Set by the client
			string32       m_name;          // A human readable name for the model
			mutable int    m_dbg_flags;     // Flags used by PR_DBG_RDR to output info once only
			
			Model();
			
			// Access to the vertex/index buffers
			vf::iterator  LockVBuffer(model::VLock& lock, model::Range v_range = model::RangeZero, uint flags = 0);
			rdr::Index*   LockIBuffer(model::ILock& lock, model::Range i_range = model::RangeZero, uint flags = 0);
			void          DeleteRenderNuggets();
			void          SetMaterial(rdr::Material const& material, model::EPrimitive::Type prim_type, bool delete_existing_nuggets, model::Range const* v_range = 0, model::Range const* i_range = 0);
			vf::Type      GetVertexType() const;
			void          SetName(const char* name) { m_name = name; }
			model::Range  VRange() const            { return m_Vrange; }
			model::Range  IRange() const            { return m_Irange; }
			
			// Refcounting cleanup function
			static void RefCountZero(pr::RefCount<Model>* doomed);
		private:
			Model(const Model&);
			Model& operator =(const Model&);
		};
	}
}

#endif
