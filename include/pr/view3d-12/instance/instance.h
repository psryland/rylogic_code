//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
// Definition of the instance base class and built-in instances for the renderer.
// Usage:
//   Client code can use the instance structs provided here or derive there own from
//   InstanceBase. If custom instances are used in conjunction with custom shaders
//   dynamic casts should be used to down-cast the instance struct to the appropriate type.
//
// Instance data layout:
//   BaseInstance
//   EInstComp[NumCpts]
//   component
//   component
//   component
#pragma once
#include "pr/view3d-12/forward.h"
//#include "pr/view3d/models/model.h"
#include "pr/view3d/render/sortkey.h"
//#include "pr/view3d/render/state_block.h"

namespace pr::rdr12
{
	// Instance component types
	enum class EInstComp :uint8_t
	{
		None,                // invalid entry (used for padding)
		ModelPtr,            // pr::rdr::ModelPtr
		I2WTransform,        // pr::m4x4
		I2WTransformPtr,     // pr::m4x4*
		I2WTransformFuncPtr, // pr::m4x4 const& (*func)(void* context);
		C2STransform,        // pr::m4x4
		C2SOptional,         // pr::m4x4 (set to m4x4Zero to indicate not used)
		C2STransformPtr,     // pr::m4x4*
		C2STransformFuncPtr, // pr::m4x4 const& (*func)(void* context);
		SortkeyOverride,     // pr::rdr::SKOverride
		BSBlock,             // pr::rdr::BSBlock
		DSBlock,             // pr::rdr::DSBlock
		RSBlock,             // pr::rdr::RSBlock
		Flags,               // EInstFlag
		TintColour32,        // pr::Colour32
		EnvMapReflectivity,  // float
		UniqueId,            // int32
		SSSize,              // pr::v2 (screen space size)
	};

	// Instance flags
	enum class EInstFlag :uint32_t
	{
		None = 0,

		// The object to world transform is not an affine transform
		NonAffine = 1 << 5,

		// Doesn't cast a shadow
		ShadowCastExclude = 1 << 12,

		// flags
		_flags_enum,
	};

	// The size of an instance component
	constexpr int SizeOf(EInstComp comp)
	{
		switch (comp)
		{
			case EInstComp::None:                return 0;
			case EInstComp::ModelPtr:            return sizeof(ModelPtr);
			case EInstComp::I2WTransform:        return sizeof(m4x4);
			case EInstComp::I2WTransformPtr:     return sizeof(m4x4*);
			case EInstComp::I2WTransformFuncPtr: return sizeof(m4x4 const& (*)(void* context));
			case EInstComp::C2STransform:        return sizeof(m4x4);
			case EInstComp::C2SOptional:         return sizeof(m4x4);
			case EInstComp::C2STransformPtr:     return sizeof(m4x4*);
			case EInstComp::C2STransformFuncPtr: return sizeof(m4x4 const& (*)(void* context));
			case EInstComp::SortkeyOverride:     return sizeof(SKOverride);
			//case EInstComp::BSBlock:             return sizeof(BSBlock);
			//case EInstComp::DSBlock:             return sizeof(DSBlock);
			//case EInstComp::RSBlock:             return sizeof(RSBlock);
			case EInstComp::Flags:               return sizeof(EInstFlag);
			case EInstComp::TintColour32:        return sizeof(Colour32);
			case EInstComp::EnvMapReflectivity:  return sizeof(float);
			case EInstComp::UniqueId:            return sizeof(int32);
			case EInstComp::SSSize:              return sizeof(v2);
			default: throw std::runtime_error("Unknown instance component type");
		}
	}

	// The header for an instance. All instances must start with one of these
	struct BaseInstance
	{
		int m_cpt_count;

		static BaseInstance make(int cpt_count)
		{
			BaseInstance b = {cpt_count};
			return b;
		}

		// Enumerate the component types
		EInstComp const* begin() const
		{
			return pr::type_ptr<EInstComp>(this + 1);
		}
		EInstComp const* end() const
		{
			return begin() + m_cpt_count;
		}
		EInstComp* begin()
		{
			return pr::type_ptr<EInstComp>(this + 1);
		}
		EInstComp* end()
		{
			return begin() + m_cpt_count;
		}

		// Access the component at 'ofs'
		template <typename Comp> Comp const* get(size_t ofs) const
		{
			return reinterpret_cast<Comp const*>(byte_ptr(this) + ofs);
		}
		template <typename Comp> Comp* get(size_t ofs)
		{
			return reinterpret_cast<Comp*>(byte_ptr(this) + ofs);
		}

		// Find the 'index'th component of type 'comp' in this instance. Returns non-null if the component was found
		template <typename Comp> Comp const* find(EInstComp comp, int index = 0) const
		{
			auto byte_ofs = PadTo<size_t>(sizeof(BaseInstance) + m_cpt_count * sizeof(EInstComp), 16);
			for (auto& c : *this)
			{
				if (c == comp && index-- == 0) return get<Comp>(byte_ofs);
				byte_ofs += SizeOf(c);
			}
			return nullptr;
		}
		template <typename Comp> Comp* find(EInstComp comp, int index = 0)
		{
			auto byte_ofs = PadTo<size_t>(sizeof(BaseInstance) + m_cpt_count * sizeof(EInstComp), 16);
			for (auto& c : *this)
			{
				if (c == comp && index-- == 0) return get<Comp>(byte_ofs);
				byte_ofs += SizeOf(c);
			}
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

	// A component that gets a transform via function pointer.
	struct m4x4Func
	{
		using m4x4FuncPtr = m4x4 const& (*)(void* ctx);

		m4x4FuncPtr m_func;
		void*       m_ctx;

		m4x4 const& Txfm() const
		{
			return m_func(m_ctx);
		}
	};

	// Return a pointer to the model that this is an instance of.
	inline ModelPtr const& GetModel(BaseInstance const& inst)
	{
		return inst.get<ModelPtr>(EInstComp::ModelPtr);
	}

	// Return the instance to world transform for an instance.
	// An instance must have an i2w transform or a shared i2w transform.
	inline m4x4 const& GetO2W(BaseInstance const& inst)
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

	// Look for a camera to screen (or instance specific projection) transform for an instance.
	// Returns null if the instance doesn't have one.
	inline bool FindC2S(BaseInstance const& inst, m4x4& camera_to_screen)
	{
		auto pc2s = inst.find<m4x4>(EInstComp::C2STransform);
		if (pc2s)
		{
			camera_to_screen = *pc2s;
			return true;
		}
				
		auto c2s_optional = inst.find<m4x4>(EInstComp::C2SOptional);
		if (c2s_optional && c2s_optional->x != v4Zero)
		{
			camera_to_screen = *c2s_optional;
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

	// Return the instance flags associated with 'inst'
	inline EInstFlag GetFlags(BaseInstance const& inst)
	{
		auto const* flags = inst.find<EInstFlag>(EInstComp::Flags);
		return flags ? *flags : EInstFlag::None;
	}

	// Return the id assigned to this instance, or '0' if not found
	inline int UniqueId(BaseInstance const& inst)
	{
		auto puid = inst.find<int>(EInstComp::UniqueId);
		return puid ? *puid : 0;
	}

	// Cast from a 'BaseInstance' pointer to an instance type
	template <typename InstType> constexpr InstType const* cast(BaseInstance const* base_ptr)
	{
		return type_ptr<InstType>(byte_ptr(base_ptr) - offsetof(InstType, m_base));
	}
	template <typename InstType> constexpr InstType* cast(BaseInstance* base_ptr)
	{
		return type_ptr<InstType>(byte_ptr(base_ptr) - offsetof(InstType, m_base));
	}

	// Use this to define class types that are compatible with the renderer
	// Example:
	//  #define PR_RDR_INST(x)\
	//     x(ModelPtr ,m_model  ,EInstComp::ModelPtr)\
	//     x(Colour32 ,m_colour ,EInstComp::TintColour32)
	//  PR_RDR_DEFINE_INSTANCE(name, PR_RDR_INST)
	//  #undef PR_RDR_INST

	// Macro instance generator functions
	#define PR_RDR_INST_INITIALISERS(ty,nm,em)      ,nm()
	#define PR_RDR_INST_MEMBER_COUNT(ty,nm,em)      + 1
	#define PR_RDR_INST_MEMBERS(ty,nm,em)           ty nm;
	#define PR_RDR_INST_INIT_COMPONENTS(ty,nm,em)   m_cpt[i++] = em;

	// Notes:
	// No inheritance in this type. It relies on POD behaviour
	// Be careful with alignment of members, esp. m4x4's
	#define PR_RDR_DEFINE_INSTANCE(name, fields)\
		struct name\
		{\
			static constexpr int CompCount = 0 fields(PR_RDR_INST_MEMBER_COUNT);\
			pr::rdr12::BaseInstance m_base;\
			pr::rdr12::EInstComp m_cpt[CompCount + pr::Pad<size_t>(sizeof(pr::rdr12::BaseInstance) + CompCount, 16U)];\
			fields(PR_RDR_INST_MEMBERS)\
			\
			name()\
				:m_base()\
				,m_cpt()\
				fields(PR_RDR_INST_INITIALISERS)\
			{\
				using namespace pr::rdr12;\
				static_assert(offsetof(name, m_base) == 0, "'m_base' must be be the first member");\
				int i = 0;\
				m_base.m_cpt_count = CompCount;\
				fields(PR_RDR_INST_INIT_COMPONENTS)\
			}\
		};

	static_assert(sizeof(EInstComp) == 1, "Padding of Instance types relies on this");
}
