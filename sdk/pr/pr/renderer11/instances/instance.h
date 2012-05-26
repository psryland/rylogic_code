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

namespace pr
{
	namespace rdr
	{
		// Instance component types
		namespace EInstComp
		{
			enum Type
			{
				ModelPtr,
				I2WTransform,
				I2WTransformPtr,
				I2WTransformFuncPtr,
				C2STransform,
				C2STransformPtr,
				SortkeyOverride,
				RenderState,
				TintColour32,
				FirstUserCpt, // Clients may add other component types
			};
		}
		
		// Component description
		struct CompDesc
		{
			pr::uint16 m_type;    // The type of component this is an offset to
			pr::uint16 m_offset;  // Byte offset from the instance pointer
			static CompDesc make(pr::uint16 type, pr::uint16 offset) { CompDesc c = {type, offset}; return c; }
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
			template <typename Comp> Comp const* find(uint16 comp_type, int index = 0) const
			{
				for (CompDesc const *i = begin(), *iend = end(); i != iend; ++i)
					if (i->m_type == comp_type && index-- == 0)
						return pr::type_ptr<Comp>(pr::byte_ptr(this) + i->m_offset);
				return 0;
			}
			template <typename Comp> Comp* find(uint16 comp_type, int index = 0)
			{
				for (CompDesc *i = begin(), *iend = end(); i != iend; ++i)
					if (i->m_type == comp_type && index-- == 0)
						return pr::type_ptr<Comp>(pr::byte_ptr(this) + i->m_offset);
				return 0;
			}
			
			// Get the 'index'th component in this instance
			template <typename Comp> inline Comp const& get(pr::uint16 comp_type, int index = 0) const
			{
				return *find<Comp>(comp_type, index);
			}
			template <typename Comp> inline Comp& get(pr::uint16 comp_type, int index = 0)
			{
				return find<Comp>(comp_type, index);
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

// Instance type declaration helpers
#define PR_RDR_DECLARE_INSTANCE_TYPE2(name, type1, name1, enum1, type2, name2, enum2)\
	struct name\
	{\
		name() :name1() ,name2()\
		{\
			using namespace pr::rdr;\
			m_base.m_cpt_count = 2;\
			m_cpt[0] = CompDesc::make(enum1, offsetof(name, name1));\
			m_cpt[1] = CompDesc::make(enum2, offsetof(name, name2));\
		}\
		pr::rdr::BaseInstance m_base;\
		pr::rdr::CompDesc m_cpt[2];\
		type1 name1;\
		type2 name2;\
	};
#define PR_RDR_DECLARE_INSTANCE_TYPE3(name, type1, name1, enum1, type2, name2, enum2, type3, name3, enum3)\
	struct name\
	{\
		name() :name1() ,name2() ,name3()\
		{\
			using namespace pr::rdr;\
			m_base.m_cpt_count = 3;\
			m_cpt[0] = CompDesc::make(enum1, offsetof(name, name1));\
			m_cpt[1] = CompDesc::make(enum2, offsetof(name, name2));\
			m_cpt[2] = CompDesc::make(enum3, offsetof(name, name3));\
		}\
		pr::rdr::BaseInstance m_base;\
		pr::rdr::CompDesc m_cpt[3];\
		type1 name1;\
		type2 name2;\
		type3 name3;\
	};
#define PR_RDR_DECLARE_INSTANCE_TYPE4(name, type1, name1, enum1, type2, name2, enum2, type3, name3, enum3, type4, name4, enum4)\
	struct name\
	{\
		name() :name1() ,name2() ,name3() ,name4()\
		{\
			using namespace pr::rdr;\
			m_base.m_cpt_count = 4;\
			m_cpt[0] = CompDesc::make(enum1, offsetof(name, name1));\
			m_cpt[1] = CompDesc::make(enum2, offsetof(name, name2));\
			m_cpt[2] = CompDesc::make(enum3, offsetof(name, name3));\
			m_cpt[3] = CompDesc::make(enum4, offsetof(name, name4));\
		}\
		pr::rdr::BaseInstance m_base;\
		pr::rdr::CompDesc m_cpt[4];\
		type1 name1;\
		type2 name2;\
		type3 name3;\
		type4 name4;\
	};
#define PR_RDR_DECLARE_INSTANCE_TYPE5(name, type1, name1, enum1, type2, name2, enum2, type3, name3, enum3, type4, name4, enum4, type5, name5, enum5)\
	struct name\
	{\
		name() :name1() ,name2() ,name3() ,name4() ,name5()\
		{\
			using namespace pr::rdr;\
			m_base.m_cpt_count = 5;\
			m_cpt[0] = CompDesc::make(enum1, offsetof(name, name1));\
			m_cpt[1] = CompDesc::make(enum2, offsetof(name, name2));\
			m_cpt[2] = CompDesc::make(enum3, offsetof(name, name3));\
			m_cpt[3] = CompDesc::make(enum4, offsetof(name, name4));\
			m_cpt[4] = CompDesc::make(enum5, offsetof(name, name5));\
		}\
		pr::rdr::BaseInstance m_base;\
		pr::rdr::CompDesc m_cpt[5];\
		type1 name1;\
		type2 name2;\
		type3 name3;\
		type4 name4;\
		type5 name5;\
	};
#define PR_RDR_DECLARE_INSTANCE_TYPE6(name, type1, name1, enum1, type2, name2, enum2, type3, name3, enum3, type4, name4, enum4, type5, name5, enum5, type6, name6, enum6)\
	struct name\
	{\
		name() :name1() ,name2() ,name3() ,name4() ,name5() ,name6()\
		{\
			using namespace pr::rdr;\
			m_base.m_cpt_count = 6;\
			m_cpt[0] = CompDesc::make(enum1, offsetof(name, name1));\
			m_cpt[1] = CompDesc::make(enum2, offsetof(name, name2));\
			m_cpt[2] = CompDesc::make(enum3, offsetof(name, name3));\
			m_cpt[3] = CompDesc::make(enum4, offsetof(name, name4));\
			m_cpt[4] = CompDesc::make(enum5, offsetof(name, name5));\
			m_cpt[5] = CompDesc::make(enum6, offsetof(name, name6));\
		}\
		pr::rdr::BaseInstance m_base;\
		pr::rdr::CompDesc m_cpt[6];\
		type1 name1;\
		type2 name2;\
		type3 name3;\
		type4 name4;\
		type5 name5;\
		type6 name6;\
	};

#endif
