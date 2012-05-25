//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************
// Definition of the instance base class and built in instances for the renderer
//
// Usage:
//  Client code can use the instance structs provided here or derive there own from
//  InstanceBase. If custom instances are used in conjunction with custom shaders
//  dynamic casts should be used to down-cast the instance struct to the appropriate type.
//
#pragma once
#ifndef PR_RDR_INSTANCE_H
#define PR_RDR_INSTANCE_H

#include "pr/renderer/types/forward.h"
#include "pr/renderer/renderstates/renderstate.h"
#include "pr/renderer/models/model.h"
#include "pr/renderer/models/modelmanager.h"
#include "pr/renderer/viewport/sortkey.h"

#ifndef PR_ASSERT
#   define PR_ASSERT_DEFINED
#   define PR_ASSERT(grp, exp)
#endif

namespace pr
{
	namespace rdr
	{
		// Instance data layout:
		//  instance::Base
		//  instance::CptDesc[NumCpts]
		//  component
		//  component
		//  component
		
		// Helper components
		struct m4x4Func
		{
			typedef m4x4 const& (*m4x4FuncPtr)(void* context);
			m4x4FuncPtr m_get_i2w;
			void*       m_context;
			m4x4 const& GetI2W() const { return m_get_i2w(m_context); }
		};
		
		namespace instance
		{
			// Component types
			enum
			{
				ECpt_ModelPtr,
				ECpt_I2WTransform,
				ECpt_I2WTransformPtr,
				ECpt_I2WTransformFuncPtr,
				ECpt_C2STransform,
				ECpt_C2STransformPtr,
				ECpt_SortkeyOverride,
				ECpt_RenderState,
				ECpt_TintColour32,
				ECpt_FirstUserCpt // Clients may add other component types
			};
			
			// Component description
			struct CptDesc
			{
				pr::uint16 m_type;    // The type of component this is an offset to
				pr::uint16 m_offset;  // Byte offset from the instance pointer
				static CptDesc make(pr::uint16 type, pr::uint16 offset) { CptDesc c = {type, offset}; return c; }
			};
			
			// The header for an instance. All instances must start with one of these
			struct Base
			{
				pr::uint m_cpt_count;
				
				static Base make(pr::uint cpt_count)  { Base b = {cpt_count}; return b; }
				template <typename CptType> CptType const& GetCpt(CptDesc const* iter) const { return *reinterpret_cast<CptType const*>(reinterpret_cast<uint8 const*>(this) + iter->m_offset); }
				template <typename CptType> CptType&       GetCpt(CptDesc*       iter)       { return *reinterpret_cast<CptType*>(reinterpret_cast<uint8*>(this) + iter->m_offset); }
				
				CptDesc const* begin() const { return reinterpret_cast<CptDesc const*>(this + 1); }
				CptDesc*       begin()       { return reinterpret_cast<CptDesc*>(this + 1); }
				CptDesc const* end() const   { return begin() + m_cpt_count; }
				CptDesc*       end()         { return begin() + m_cpt_count; }
			};
			
			// Functions for accessing Cpts **************************************
			
			// Find the 'index'th component in an instance. Returns true if the component was found
			template <typename CptType> inline CptType const* FindCpt(Base const& inst, uint16 cpt_type, int index = 0)
			{
				for (CptDesc const* i = inst.begin(), *iend = inst.end(); i != iend; ++i)
					if (i->m_type == cpt_type && index-- == 0) return &inst.GetCpt<CptType>(i);
				return 0;
			}
			template <typename CptType> inline CptType* FindCpt(Base& inst, uint16 cpt_type, int index = 0)
			{
				for (CptDesc* i = inst.begin(), *iend = inst.end(); i != iend; ++i)
					if (i->m_type == cpt_type && index-- == 0) return &inst.GetCpt<CptType>(i);
				return 0;
			}
			
			// Get the 'index'th component in an instance
			template <typename CptType> inline CptType const& GetCpt(Base const& inst, uint16 cpt_type, int index = 0)
			{
				CptType const* cpt = FindCpt<CptType>(inst, cpt_type, index); PR_ASSERT(PR_DBG, cpt, "");
				return *cpt;
			}
			template <typename CptType> inline CptType& GetCpt(Base& inst, uint16 cpt_type, int index = 0)
			{
				CptType* cpt = FindCpt<CptType>(inst, cpt_type, index); PR_ASSERT(PR_DBG, cpt, "");
				return *cpt;
			}
			
			// Helper Functions **************************************
			
			// Return a pointer to the model that this is an instance of
			inline ModelPtr const& GetModel(Base const& inst)
			{
				return GetCpt<ModelPtr>(inst, ECpt_ModelPtr);
			}
			
			// Return the instance to world transform for an instance
			// An instance must have an i2w transform or a shared i2w transform
			inline m4x4 const& GetI2W(const Base& inst)
			{
				m4x4 const*        pi2w  = FindCpt<m4x4>       (inst, ECpt_I2WTransform);        if (pi2w)  return *pi2w;
				m4x4 const* const* ppi2w = FindCpt<m4x4 const*>(inst, ECpt_I2WTransformPtr);     if (ppi2w) return **ppi2w;
				m4x4Func const&    pi2wf = GetCpt <m4x4Func>   (inst, ECpt_I2WTransformFuncPtr); return pi2wf.GetI2W();
			}
			
			// Look for a camera to screen transform for an instance
			inline bool FindC2S(Base const& inst, m4x4& camera_to_screen)
			{
				m4x4 const* c2s = FindCpt<m4x4>(inst, ECpt_C2STransform);
				if (!c2s)   c2s = FindCpt<m4x4>(inst, ECpt_C2STransformPtr);
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
				m_cpt[0] = CptDesc::make(ECpt_ModelPtr        ,offsetof(BasicInstance, m_model));
				m_cpt[1] = CptDesc::make(ECpt_I2WTransform    ,offsetof(BasicInstance, m_instance_to_world));
				m_cpt[2] = CptDesc::make(ECpt_SortkeyOverride ,offsetof(BasicInstance, m_sko));
				m_cpt[3] = CptDesc::make(ECpt_RenderState     ,offsetof(BasicInstance, m_render_state));
				m_cpt[4] = CptDesc::make(ECpt_TintColour32    ,offsetof(BasicInstance, m_colour));
				m_cpt[5] = CptDesc::make(ECpt_C2STransform    ,offsetof(BasicInstance, m_camera_to_world));
			}
			
			instance::Base    m_base;
			instance::CptDesc m_cpt[NumCpts];
			
			ModelPtr           m_model;                // The model this is an instance of
			pr::m4x4           m_instance_to_world;    // A i2w transform for the instance
			sort_key::Override m_sko;                  // An override of the nugget sort key for this instance
			rs::Block          m_render_state;         // The render states of the instance
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
			m_cpt[0] = CptDesc::make(enum1, offsetof(name, name1));\
			m_cpt[1] = CptDesc::make(enum2, offsetof(name, name2));\
		}\
		pr::rdr::instance::Base m_base;\
		pr::rdr::instance::CptDesc m_cpt[2];\
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
			m_cpt[0] = CptDesc::make(enum1, offsetof(name, name1));\
			m_cpt[1] = CptDesc::make(enum2, offsetof(name, name2));\
			m_cpt[2] = CptDesc::make(enum3, offsetof(name, name3));\
		}\
		pr::rdr::instance::Base m_base;\
		pr::rdr::instance::CptDesc m_cpt[3];\
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
			m_cpt[0] = CptDesc::make(enum1, offsetof(name, name1));\
			m_cpt[1] = CptDesc::make(enum2, offsetof(name, name2));\
			m_cpt[2] = CptDesc::make(enum3, offsetof(name, name3));\
			m_cpt[3] = CptDesc::make(enum4, offsetof(name, name4));\
		}\
		pr::rdr::instance::Base m_base;\
		pr::rdr::instance::CptDesc m_cpt[4];\
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
			m_cpt[0] = CptDesc::make(enum1, offsetof(name, name1));\
			m_cpt[1] = CptDesc::make(enum2, offsetof(name, name2));\
			m_cpt[2] = CptDesc::make(enum3, offsetof(name, name3));\
			m_cpt[3] = CptDesc::make(enum4, offsetof(name, name4));\
			m_cpt[4] = CptDesc::make(enum5, offsetof(name, name5));\
		}\
		pr::rdr::instance::Base m_base;\
		pr::rdr::instance::CptDesc m_cpt[5];\
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
			m_cpt[0] = CptDesc::make(enum1, offsetof(name, name1));\
			m_cpt[1] = CptDesc::make(enum2, offsetof(name, name2));\
			m_cpt[2] = CptDesc::make(enum3, offsetof(name, name3));\
			m_cpt[3] = CptDesc::make(enum4, offsetof(name, name4));\
			m_cpt[4] = CptDesc::make(enum5, offsetof(name, name5));\
			m_cpt[5] = CptDesc::make(enum6, offsetof(name, name6));\
		}\
		pr::rdr::instance::Base m_base;\
		pr::rdr::instance::CptDesc m_cpt[6];\
		type1 name1;\
		type2 name2;\
		type3 name3;\
		type4 name4;\
		type5 name5;\
		type6 name6;\
	};

#ifdef PR_ASSERT_DEFINED
#   undef PR_ASSERT_DEFINED
#   undef PR_ASSERT
#endif

#endif

//{
//  namespace pred
//  {
//      // Finds all attractors
//      struct AlwaysTrue
//      {
//          bool operator () (const Base&, float) const                             { return true; }
//      };
//
//      // Do not find attractors within a minimum radius
//      struct MinRadius
//      {
//          MinRadius(float min_radius) : m_min_radiusSq(min_radius * min_radius)   {}
//          bool operator () (const Base&, float distSq) const                      { return distSq >= m_min_radiusSq; }
//          float m_min_radiusSq;
//      };
//
//      // Only find attractors of a particular sub type
//      struct SubType
//      {
//          SubType(EAttractorSubType sub_type) : m_sub_type(sub_type)              {}
//          bool operator () (const Base& a, float) const                           { return a.GetSubType() == m_sub_type; }
//          EAttractorSubType m_sub_type;
//      };
//
//      // Only find attractors within a height range.
//      struct LimitHeight
//      {
//          LimitHeight(float min_height, float max_height)
//          :m_min_height(min_height)
//          ,m_max_height(max_height)
//          {}
//          bool operator () (const Base& a, float) const                           { float y = a.GetPosition()[1]; return y >= m_min_height && y <= m_max_height; }
//          float m_min_height;
//          float m_max_height;
//      };
//
//      // Only find attractors that are associated with instances that have been smashed
//      // This can only be used with attractor types that have an instance reference
//      template <typename InstRefAttractorType>
//      struct IgnoreSmashed
//      {
//          bool operator () (const Base& a, float) const
//          {
//              if( !a.TestIfSmashed() ) return true;
//              InstRefAttractorType& attr = attractor::cast_to<InstRefAttractorType>(a);
//
//              // If this doesn't compile it's because you're trying to use this predicate with an
//              // attractor type that doesn't have the instance reference member (or the GetRegionId() and
//              // GetInstanceIndex() member functions)
//              CInstanceContainer* scene = GetSingletonObject(ESingletonType_SceneManager)->GetSceneData(attr.GetRegionId());
//              return !scene->IsSmashed(attr.GetInstanceIndex());
//          }
//      };
//  }// namespace pred
//  namespace impl
//  {
//      template <typename Pred1, typename Pred2 = pred::AlwaysTrue, typename Pred3 = pred::AlwaysTrue, typename Pred4 = pred::AlwaysTrue>
//      struct combiner
//      {
//          explicit combiner(Pred1 pred1, Pred2 pred2 = Pred2(), Pred3 pred3 = Pred3(), Pred4 pred4 = Pred4())
//          :m_pred1(pred1)
//          ,m_pred2(pred2)
//          ,m_pred3(pred3)
//          ,m_pred4(pred4)
//          {}
//
//          bool operator () (const Base& a, float distSq) const
//          {
//              return m_pred1(a, distSq) && m_pred2(a, distSq) && m_pred3(a, distSq) && m_pred4(a, distSq);
//          }
//
//          Pred1 m_pred1;
//          Pred2 m_pred2;
//          Pred3 m_pred3;
//          Pred4 m_pred4;
//      };
//  }//namespace impl
//
//  // Functions for combining search predicates
//  template <typename Pred1, typename Pred2>
//  inline impl::combiner<Pred1, Pred2>                 CombinePreds(Pred1 pred1, Pred2 pred2)                              { return impl::combiner<Pred1, Pred2>               (pred1, pred2); }
//  template <typename Pred1, typename Pred2, typename Pred3>
//  inline impl::combiner<Pred1, Pred2, Pred3>          CombinePreds(Pred1 pred1, Pred2 pred2, Pred3 pred3)                 { return impl::combiner<Pred1, Pred2, Pred3>        (pred1, pred2, pred3); }
//  template <typename Pred1, typename Pred2, typename Pred3, typename Pred4>
//  inline impl::combiner<Pred1, Pred2, Pred3, Pred4>   CombinePreds(Pred1 pred1, Pred2 pred2, Pred3 pred3, Pred4 pred4)    { return impl::combiner<Pred1, Pred2, Pred3, Pred4> (pred1, pred2, pred3, pred4); }
//
//}