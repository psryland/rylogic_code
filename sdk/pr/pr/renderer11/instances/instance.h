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
//  instance::Base
//  instance::CompDesc[NumCpts]
//  component
//  component
//  component
#pragma once
#ifndef PR_RDR_INSTANCES_INSTANCE_H
#define PR_RDR_INSTANCES_INSTANCE_H

#include "pr/renderer11/forward.h"
//#include "pr/renderer/renderstates/renderstate.h"
#include "pr/renderer11/models/model.h"
//#include "pr/renderer/models/modelmanager.h"
#include "pr/renderer11/render/sortkey.h"

namespace pr
{
	namespace rdr
	{
		// A component that gets an i2w transform via function pointer
		struct m4x4Func
		{
			typedef pr::m4x4 const& (*m4x4FuncPtr)(void* context);
			m4x4FuncPtr m_get_i2w;
			void*       m_context;
			pr::m4x4 const& GetI2W() const { return m_get_i2w(m_context); }
		};
		
		namespace instance
		{
			// Component types
			namespace EComp
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
			struct Base
			{
				pr::uint m_cpt_count;
				
				static Base make(pr::uint cpt_count) { Base b = {cpt_count}; return b; }
				template <typename Comp> Comp const& GetCpt(CompDesc const* iter) const { return *pr::type_ptr<Comp>(pr::byte_ptr(this) + iter->m_offset); }
				template <typename Comp> Comp&       GetCpt(CompDesc*       iter)       { return *pr::type_ptr<Comp>(pr::byte_ptr(this) + iter->m_offset); }
				
				CompDesc const* begin() const { return pr::type_ptr<CompDesc>(this + 1); }
				CompDesc*       begin()       { return pr::type_ptr<CompDesc>(this + 1); }
				CompDesc const* end() const   { return begin() + m_cpt_count; }
				CompDesc*       end()         { return begin() + m_cpt_count; }
			};
			
			// Functions for accessing components **************************************
			
			// Find the 'index'th component in an instance. Returns non-null if the component was found
			template <typename Comp> inline Comp const* FindCpt(Base const& inst, uint16 cpt_type, int index = 0)
			{
				for (CompDesc const* i = inst.begin(), *iend = inst.end(); i != iend; ++i)
					if (i->m_type == cpt_type && index-- == 0) return &inst.GetCpt<Comp>(i);
				return 0;
			}
			template <typename Comp> inline Comp* FindCpt(Base& inst, uint16 cpt_type, int index = 0)
			{
				for (CompDesc* i = inst.begin(), *iend = inst.end(); i != iend; ++i)
					if (i->m_type == cpt_type && index-- == 0) return &inst.GetCpt<Comp>(i);
				return 0;
			}
			
			// Get the 'index'th component in an instance
			template <typename Comp> inline Comp const& GetCpt(Base const& inst, uint16 cpt_type, int index = 0)
			{
				Comp const* cpt = FindCpt<Comp>(inst, cpt_type, index); PR_ASSERT(PR_DBG, cpt, "");
				return *cpt;
			}
			template <typename Comp> inline Comp& GetCpt(Base& inst, uint16 cpt_type, int index = 0)
			{
				Comp* cpt = FindCpt<Comp>(inst, cpt_type, index); PR_ASSERT(PR_DBG, cpt, "");
				return *cpt;
			}
			
			// Helper Functions **************************************
			
			// Return a pointer to the model that this is an instance of
			inline ModelPtr const& GetModel(Base const& inst)
			{
				return GetCpt<ModelPtr>(inst, EComp::ModelPtr);
			}
			
			// Return the instance to world transform for an instance
			// An instance must have an i2w transform or a shared i2w transform
			inline pr::m4x4 const& GetI2W(const Base& inst)
			{
				m4x4 const*        pi2w  = FindCpt<m4x4>       (inst, EComp::I2WTransform);        if (pi2w)  return *pi2w;
				m4x4 const* const* ppi2w = FindCpt<m4x4 const*>(inst, EComp::I2WTransformPtr);     if (ppi2w) return **ppi2w;
				m4x4Func const&    pi2wf = GetCpt <m4x4Func>   (inst, EComp::I2WTransformFuncPtr); return pi2wf.GetI2W();
			}
			
			// Look for a camera to screen transform for an instance
			inline bool FindC2S(Base const& inst, m4x4& camera_to_screen)
			{
				m4x4 const* c2s = FindCpt<m4x4>(inst, EComp::C2STransform);
				if (!c2s)   c2s = FindCpt<m4x4>(inst, EComp::C2STransformPtr);
				if (c2s) { camera_to_screen = *c2s; return true; }
				return false;
			}
		}
		
		// An example instance ***************************************************
		
		// This is only for reference, don't use it
		struct BasicInstance
		{
			enum { NumCpts = 6 };
			
			// Note: technically having this constructor could mean the order of
			// members in this struct isn't maintained. However, in practise they are.
			BasicInstance()
			{
				using namespace instance;
				m_base.m_cpt_count = NumCpts;
				m_cpt[0] = CompDesc::make(EComp::ModelPtr        ,offsetof(BasicInstance, m_model));
				m_cpt[1] = CompDesc::make(EComp::I2WTransform    ,offsetof(BasicInstance, m_instance_to_world));
				m_cpt[2] = CompDesc::make(EComp::SortkeyOverride ,offsetof(BasicInstance, m_sko));
				//m_cpt[3] = CompDesc::make(EComp::RenderState     ,offsetof(BasicInstance, m_render_state));
				m_cpt[4] = CompDesc::make(EComp::TintColour32    ,offsetof(BasicInstance, m_colour));
				m_cpt[5] = CompDesc::make(EComp::C2STransform    ,offsetof(BasicInstance, m_camera_to_world));
			}
			
			instance::Base    m_base;
			instance::CompDesc m_cpt[NumCpts];
			
			pr::rdr::ModelPtr           m_model;                // The model this is an instance of
			pr::m4x4           m_instance_to_world;    // A i2w transform for the instance
			sort_key::Override m_sko;                  // An override of the nugget sort key for this instance
			//rs::Block          m_render_state;         // The render states of the instance
			pr::Colour32       m_colour;               // A colour value for the instance
			pr::m4x4           m_camera_to_world;      // A projection transform for the instance
		};
	}
}

// Instance type declaration helpers
#define PR_RDR_DECLARE_INSTANCE_TYPE2(name, type1, name1, enum1, type2, name2, enum2)\
	struct name\
	{\
		name() :name1() ,name2()\
		{\
			using namespace pr::rdr::instance;\
			m_base.m_cpt_count = 2;\
			m_cpt[0] = CompDesc::make(enum1, offsetof(name, name1));\
			m_cpt[1] = CompDesc::make(enum2, offsetof(name, name2));\
		}\
		pr::rdr::instance::Base m_base;\
		pr::rdr::instance::CompDesc m_cpt[2];\
		type1 name1;\
		type2 name2;\
	};
#define PR_RDR_DECLARE_INSTANCE_TYPE3(name, type1, name1, enum1, type2, name2, enum2, type3, name3, enum3)\
	struct name\
	{\
		name() :name1() ,name2() ,name3()\
		{\
			using namespace pr::rdr::instance;\
			m_base.m_cpt_count = 3;\
			m_cpt[0] = CompDesc::make(enum1, offsetof(name, name1));\
			m_cpt[1] = CompDesc::make(enum2, offsetof(name, name2));\
			m_cpt[2] = CompDesc::make(enum3, offsetof(name, name3));\
		}\
		pr::rdr::instance::Base m_base;\
		pr::rdr::instance::CompDesc m_cpt[3];\
		type1 name1;\
		type2 name2;\
		type3 name3;\
	};
#define PR_RDR_DECLARE_INSTANCE_TYPE4(name, type1, name1, enum1, type2, name2, enum2, type3, name3, enum3, type4, name4, enum4)\
	struct name\
	{\
		name() :name1() ,name2() ,name3() ,name4()\
		{\
			using namespace pr::rdr::instance;\
			m_base.m_cpt_count = 4;\
			m_cpt[0] = CompDesc::make(enum1, offsetof(name, name1));\
			m_cpt[1] = CompDesc::make(enum2, offsetof(name, name2));\
			m_cpt[2] = CompDesc::make(enum3, offsetof(name, name3));\
			m_cpt[3] = CompDesc::make(enum4, offsetof(name, name4));\
		}\
		pr::rdr::instance::Base m_base;\
		pr::rdr::instance::CompDesc m_cpt[4];\
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
			using namespace pr::rdr::instance;\
			m_base.m_cpt_count = 5;\
			m_cpt[0] = CompDesc::make(enum1, offsetof(name, name1));\
			m_cpt[1] = CompDesc::make(enum2, offsetof(name, name2));\
			m_cpt[2] = CompDesc::make(enum3, offsetof(name, name3));\
			m_cpt[3] = CompDesc::make(enum4, offsetof(name, name4));\
			m_cpt[4] = CompDesc::make(enum5, offsetof(name, name5));\
		}\
		pr::rdr::instance::Base m_base;\
		pr::rdr::instance::CompDesc m_cpt[5];\
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
			using namespace pr::rdr::instance;\
			m_base.m_cpt_count = 6;\
			m_cpt[0] = CompDesc::make(enum1, offsetof(name, name1));\
			m_cpt[1] = CompDesc::make(enum2, offsetof(name, name2));\
			m_cpt[2] = CompDesc::make(enum3, offsetof(name, name3));\
			m_cpt[3] = CompDesc::make(enum4, offsetof(name, name4));\
			m_cpt[4] = CompDesc::make(enum5, offsetof(name, name5));\
			m_cpt[5] = CompDesc::make(enum6, offsetof(name, name6));\
		}\
		pr::rdr::instance::Base m_base;\
		pr::rdr::instance::CompDesc m_cpt[6];\
		type1 name1;\
		type2 name2;\
		type3 name3;\
		type4 name4;\
		type5 name5;\
		type6 name6;\
	};

#endif
