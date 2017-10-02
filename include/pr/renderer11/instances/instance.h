//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
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

#include "pr/renderer11/forward.h"
#include "pr/renderer11/models/model.h"
#include "pr/renderer11/render/sortkey.h"
#include "pr/renderer11/render/state_block.h"

namespace pr
{
	namespace rdr
	{
		// Instance component types
		enum class EInstComp :pr::uint16
		{
			ModelPtr           , // pr::rdr::ModelPtr
			I2WTransform       , // pr::m4x4
			I2WTransformPtr    , // pr::m4x4*
			I2WTransformFuncPtr, // pr::m4x4 const& (*func)(void* context);
			C2STransform       , // pr::m4x4
			C2SOptional        , // pr::optional<m4x4>
			C2STransformPtr    , // pr::m4x4*
			C2STransformFuncPtr, // pr::m4x4 const& (*func)(void* context);
			SortkeyOverride    , // pr::rdr::SKOverride
			BSBlock            , // pr::rdr::BSBlock
			DSBlock            , // pr::rdr::DSBlock
			RSBlock            , // pr::rdr::RSBlock
			TintColour32       , // pr::Colour32
			SSWidth            , // pr::uint (screen space width)
			FirstUserCpt       , // Clients may add other component types
		};

		// Component description
		struct CompDesc
		{
			EInstComp m_type;    // The type of component this is an offset to
			pr::uint16 m_offset;  // Byte offset from the instance pointer
			static CompDesc make(EInstComp comp, pr::uint16 offset)
			{
				CompDesc c = {comp, offset};
				return c;
			}
		};

		// The header for an instance. All instances must start with one of these
		struct BaseInstance
		{
			pr::uint m_cpt_count;

			static BaseInstance make(pr::uint cpt_count)
			{
				BaseInstance b = {cpt_count};
				return b;
			}

			CompDesc const* begin() const { return pr::type_ptr<CompDesc>(this + 1); }
			CompDesc*       begin()       { return pr::type_ptr<CompDesc>(this + 1); }
			CompDesc const* end() const   { return begin() + m_cpt_count; }
			CompDesc*       end()         { return begin() + m_cpt_count; }

			// Access the component at 'ofs'
			template <typename Comp> Comp const* get(pr::uint16 ofs) const
			{
				return reinterpret_cast<Comp const*>(byte_ptr(this) + ofs);
			}
			template <typename Comp> Comp* get(pr::uint16 ofs)
			{
				return reinterpret_cast<Comp*>(byte_ptr(this) + ofs);
			}

			// Find the 'index'th component in this instance. Returns non-null if the component was found
			template <typename Comp> Comp const* find(EInstComp comp, int index = 0) const
			{
				for (auto& c : *this)
					if (c.m_type == comp && index-- == 0)
						return get<Comp>(c.m_offset);
				return nullptr;
			}
			template <typename Comp> Comp* find(EInstComp comp, int index = 0)
			{
				for (auto& c : *this)
					if (c.m_type == comp && index-- == 0)
						return get<Comp>(c.m_offset);
				return nullptr;
			}

			// Get the 'index'th component in this instance
			template <typename Comp> inline Comp const& get(EInstComp comp, int index = 0) const
			{
				auto c = find<Comp>(comp, index);
				if (c == nullptr) throw std::exception("This instance does not have the requested component");
				return *c;
			}
			template <typename Comp> inline Comp& get(EInstComp comp, int index = 0)
			{
				auto c = find<Comp>(comp, index);
				if (c == nullptr) throw std::exception("This instance does not have the requested component");
				return *c;
			}
		};

		// A component that gets a transform via function pointer
		struct m4x4Func
		{
			using m4x4FuncPtr = pr::m4x4 const& (*)(void* ctx);

			m4x4FuncPtr m_func;
			void*       m_ctx;

			pr::m4x4 const& Txfm() const
			{
				return m_func(m_ctx);
			}
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
			auto pi2w = inst.find<m4x4>(EInstComp::I2WTransform);
			if (pi2w)
				return *pi2w;

			auto ppi2w = inst.find<m4x4 const*>(EInstComp::I2WTransformPtr);
			if (ppi2w)
				return **ppi2w;

			auto pi2wf = inst.find<m4x4Func>(EInstComp::I2WTransformFuncPtr);
			if (pi2wf && pi2wf->m_func != nullptr)
				return pi2wf->Txfm();

			return m4x4Identity;
		}

		// Look for a camera to screen (or instance specific projection) transform for an instance
		// Returns null if the instance doesn't have one
		inline bool FindC2S(BaseInstance const& inst, pr::m4x4& camera_to_screen)
		{
			auto pc2s = inst.find<m4x4>(EInstComp::C2STransform);
			if (pc2s)
			{
				camera_to_screen = *pc2s;
				return true;
			}
				
			auto c2s_optional = inst.find<pr::optional<m4x4>>(EInstComp::C2SOptional);
			if (c2s_optional && *c2s_optional != nullptr)
			{
				camera_to_screen = (*c2s_optional).value();
				return true;
			}

			auto ppc2s = inst.find<m4x4 const*>(EInstComp::C2STransformPtr);
			if (ppc2s)
			{
				camera_to_screen = **ppc2s;
				return true;
			}

			auto pc2sf = inst.find<m4x4Func>(EInstComp::C2STransformFuncPtr);
			if (pc2sf && pc2sf->m_func != nullptr)
			{
				camera_to_screen = pc2sf->Txfm();
				return true;
			}

			return false;
		}
	}
}
