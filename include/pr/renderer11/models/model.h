//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once
#ifndef PR_RDR_MODELS_MODEL_H
#define PR_RDR_MODELS_MODEL_H

#include "pr/renderer11/forward.h"
#include "pr/renderer11/models/nugget.h"

namespace pr
{
	namespace rdr
	{
		// A graphics model containing vertices and indices
		struct Model :pr::RefCount<Model>
		{
			ModelBufferPtr  m_model_buffer;  // The buffer that contains this model's vertex and index data
			Range           m_vrange;        // The first and number of verties for this model within 'm_model_buffer'
			Range           m_irange;        // The first and number of indices for this model within 'm_model_buffer'
			TNuggetChain    m_nuggets;       // The nuggets for this model
			pr::BBox        m_bbox;          // A bounding box for the model. Set by the client
			string32        m_name;          // A human readable name for the model
			mutable int     m_dbg_flags;     // Flags used by PR_DBG_RDR to output info once only

			// Only the model manager should be creating these
			Model(MdlSettings const& settings, ModelBufferPtr& model_buffer);
			~Model();

			// Access to the vertex/index buffers
			// Only returns false if 'D3D11_MAP_FLAG_DO_NOT_WAIT' flag is set, all other fail cases throw
			bool MapVerts  (Lock& lock, D3D11_MAP map_type = D3D11_MAP_WRITE, uint flags = 0, Range vrange = RangeZero);
			bool MapIndices(Lock& lock, D3D11_MAP map_type = D3D11_MAP_WRITE, uint flags = 0, Range irange = RangeZero);

			// Create a nugget from a range within this model
			// Ranges are model relative, i.e. the first vert in the model is range [0,1)
			// Remember you might need to delete render nuggets first
			void CreateNugget(NuggetProps props);

			// Call to release the nuggets that this model has been
			// divided into. Nuggets are the contiguous sub groups
			// of the model geometry that use the same data.
			void DeleteNuggets();

			Range VRange() const { return m_vrange; }
			Range IRange() const { return m_irange; }

			// Refcounting cleanup function
			static void RefCountZero(pr::RefCount<Model>* doomed);

		private:

			Model(const Model&);
			Model& operator =(const Model&);
		};
	}
}

#endif