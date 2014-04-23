//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
// Definition of the instance base class and built in instances for the renderer
//
// Usage:
//  Client code can use the instance structs provided here or derive there own from
//  InstanceBase. If custom instances are used in conjunction with custom shaders
//  dynamic casts should be used to down-cast the instance struct to the appropriate type.
//
// Instance data layout:
//  BaseInstance
//  CompDesc[NumCpts]
//  component
//  component
//  component
#pragma once
#ifndef PR_RDR_INSTANCES_INSTANCE_H
#define PR_RDR_INSTANCES_INSTANCE_H

#include "pr/renderer11/forward.h"
#include "pr/renderer11/models/model.h"
#include "pr/renderer11/render/sortkey.h"
#include "pr/renderer11/render/blend_state.h"
#include "pr/renderer11/render/raster_state.h"

namespace pr
{
	namespace rdr
	{
		// Instance component types
		#define PR_ENUM(x)/*
			*/x(ModelPtr           )/* pr::rdr::ModelPtr
			*/x(I2WTransform       )/* pr::m4x4
			*/x(I2WTransformPtr    )/* pr::m4x4*
			*/x(I2WTransformFuncPtr)/* pr::m4x4 const& (*func)(void* context);
			*/x(C2STransform       )/* pr::m4x4
			*/x(C2STransformPtr    )/* pr::m4x4*
			*/x(SortkeyOverride    )/* pr::rdr::SKOverride
			*/x(BSBlock            )/* pr::rdr::BSBlock
			*/x(DSBlock            )/* pr::rdr::DSBlock
			*/x(RSBlock            )/* pr::rdr::RSBlock
			*/x(TintColour32       )/* pr::Colour32
			*/x(FirstUserCpt       ) // Clients may add other component types
		PR_DEFINE_ENUM1(EInstComp, PR_ENUM);
		#undef PR_ENUM

		// Component description
		struct CompDesc
		{
			pr::uint16 m_type;    // The type of component this is an offset to
			pr::uint16 m_offset;  // Byte offset from the instance pointer
			static CompDesc make(EInstComp comp, pr::uint16 offset) { CompDesc c = {value_cast<pr::uint16>(comp.value), offset}; return c; }
		};

		// The header for an instance. All instances must start with one of these
		struct BaseInstance
		{
			pr::uint m_cpt_count;

			static BaseInstance make(pr::uint cpt_count) { BaseInstance b = {cpt_count}; return b; }

			CompDesc const* begin() const { return pr::type_ptr<CompDesc>(this + 1); }
			CompDesc*       begin()       { return pr::type_ptr<CompDesc>(this + 1); }
			CompDesc const* end() const   { return begin() + m_cpt_count; }
			CompDesc*       end()         { return begin() + m_cpt_count; }

			// Find the 'index'th component in this instance. Returns non-null if the component was found
			template <typename Comp> Comp const* find(EInstComp comp, int index = 0) const
			{
				//for (auto c : *this)
				//	if (c.m_type == comp && index-- == 0)
				//		return pr::type_ptr<Comp>(pr::byte_ptr(this) + c.m_offset);
				for (CompDesc const *i = begin(), *iend = end(); i != iend; ++i)
					if (i->m_type == comp && index-- == 0)
						return pr::type_ptr<Comp>(pr::byte_ptr(this) + i->m_offset);
				return nullptr;
			}
			template <typename Comp> Comp* find(EInstComp comp, int index = 0)
			{
				//for (auto c : *this)
				//	if (c.m_type == comp && index-- == 0)
				//		return pr::type_ptr<Comp>(pr::byte_ptr(this) + c.m_offset);
				for (CompDesc *i = begin(), *iend = end(); i != iend; ++i)
					if (i->m_type == comp && index-- == 0)
						return pr::type_ptr<Comp>(pr::byte_ptr(this) + i->m_offset);
				return nullptr;
			}

			// Get the 'index'th component in this instance
			template <typename Comp> inline Comp const& get(EInstComp comp, int index = 0) const
			{
				Comp const* c = find<Comp>(comp, index);
				PR_ASSERT(PR_DBG_RDR, c != 0, "This instance does not have the requested component");
				return *c;
			}
			template <typename Comp> inline Comp& get(EInstComp comp, int index = 0)
			{
				Comp* c = find<Comp>(comp, index);
				PR_ASSERT(PR_DBG_RDR, c != 0, "This instance does not have the requested component");
				return *c;
			}
		};

		// Helper Components/Functions **************************************

		// A component that gets an i2w transform via function pointer
		struct m4x4Func
		{
			typedef pr::m4x4 const& (*m4x4FuncPtr)(void* context);
			m4x4FuncPtr m_get_i2w;
			void*       m_context;
			pr::m4x4 const& GetI2W() const { return m_get_i2w(m_context); }
		};

		// Return a pointer to the model that this is an instance of
		inline ModelPtr const& GetModel(BaseInstance const& inst)
		{
			return inst.get<ModelPtr>(EInstComp::ModelPtr);
		}

		// Return the instance to world transform for an instance
		// An instance must have an i2w transform or a shared i2w transform
		inline pr::m4x4 const& GetO2W(BaseInstance const& inst)
		{
			m4x4 const*        pi2w  = inst.find<m4x4>       (EInstComp::I2WTransform);        if (pi2w)  return *pi2w;
			m4x4 const* const* ppi2w = inst.find<m4x4 const*>(EInstComp::I2WTransformPtr);     if (ppi2w) return **ppi2w;
			m4x4Func const&    pi2wf = inst.get<m4x4Func>    (EInstComp::I2WTransformFuncPtr); return pi2wf.GetI2W();
		}

		// Look for a camera to screen (or instance specific projection) transform for an instance
		inline bool FindC2S(BaseInstance const& inst, m4x4& camera_to_screen)
		{
			m4x4 const* c2s = inst.find<m4x4>(EInstComp::C2STransform);
			if (!c2s)   c2s = inst.find<m4x4>(EInstComp::C2STransformPtr);
			if (!c2s) return false; // not found
			camera_to_screen = *c2s;
			return true;
		}
	}
}

#endif
