//*********************************************
// Physics engine
//  Copyright © Rylogic Ltd 2006
//*********************************************

// This is a helper object for constructing collision shapes
//  1) Construct a ShapeBuilder
//  2) Add shapes in any order with arbitrary orientations
//  3) Build the shape using: BuildShape()
//  4) Serialise the shape into a buffer

#pragma once
#ifndef PR_PHYSICS_SHAPE_BUILDER_H
#define PR_PHYSICS_SHAPE_BUILDER_H

#include "pr/common/assert.h"
#include "pr/common/array.h"
#include "pr/common/byte_data.h"
#include "pr/common/refcount.h"
#include "pr/common/refptr.h"
#include "pr/maths/maths.h"
#include "pr/physics/types/forward.h"
#include "pr/physics/material/material.h"
#include "pr/physics/shape/shape.h"
#include "pr/physics/shape/builder/shapebuilderresult.h"

namespace pr
{
	namespace ph
	{
		// Settings for the shape builder
		struct ShapeBuilderSettings
		{
			float m_min_mass;   // The minimum mass a primitive may have (kg)
			float m_min_volume; // The minimum volume a primitive may have (m^3)
			ShapeBuilderSettings() :m_min_mass(1.0f) ,m_min_volume(0.001f * 0.001f * 0.001f) {}
		};
		
		// An object for building collision shapes
		class ShapeBuilder
		{
			struct Prim :pr::RefCount<Prim>
			{
				ByteCont        m_data;  // Data containing the shape
				MassProperties  m_mp;
				BoundingBox     m_bbox;
				mutable Shape*  m_shape; // Used in the debugger only
				ph::Shape const&    GetShape() const    { m_shape = (ph::Shape*)(&m_data[0]); return *reinterpret_cast<ph::Shape const*>(&m_data[0]); }
				ph::Shape&          GetShape()          { m_shape = (ph::Shape*)(&m_data[0]); return *reinterpret_cast<ph::Shape*>(&m_data[0]); }
			};
			typedef pr::Array< pr::RefPtr<Prim> > TPrimList;
			struct Model
			{
				TPrimList       m_prim_list;
				MassProperties  m_mp;
				BoundingBox     m_bbox;
			};
			
			ShapeBuilderSettings    m_settings;
			Model                   m_model;
			
			void    CalculateMassAndCentreOfMass();
			void    MoveToCentreOfMassFrame(v4& model_to_CoMframe);
			void    CalculateBoundingBox();
			void    CalculateInertiaTensor();
			
		public:
			ShapeBuilder(const ShapeBuilderSettings& settings = ShapeBuilderSettings());
			
			// Begin a new physics model
			void Reset();
			
			// Add shapes to the current model.
			template <typename ShapeType>
			ph::EResult AddShape(ShapeType const& shape)
			{
				pr::RefPtr<Prim> prim = new Prim();
				pr::AppendData(prim->m_data, byte_ptr_cast(&shape), byte_ptr_cast(&shape) + shape.m_base.m_size);
				
				// Convert the shape to canonical form (i.e. about it's centre of mass)
				// Fill out the rest of the shape information
				ShapeType& shape_ = shape_cast<ShapeType>(prim->GetShape());
				float      density = ph::GetMaterial(shape_.m_base.m_material_id).m_density;
				CalcMassProperties(shape_, density, prim->m_mp);
				ShiftCentre(shape_, prim->m_mp.m_centre_of_mass);
				CalcBBox(shape_, prim->m_bbox);
				shape_.m_base.m_bbox = prim->m_bbox;
				
				// Validate the primitive
				if (prim->m_mp.m_mass / density < m_settings.m_min_volume)   { return EResult_VolumeTooSmall; }
				if (prim->m_mp.m_mass < m_settings.m_min_mass)               { prim->m_mp.m_mass = m_settings.m_min_mass; }
				m_model.m_prim_list.push_back(prim);
				return EResult_Success;
			}
			
			// Return access to a shape
			ph::Shape const& GetShape(int i) const  { return m_model.m_prim_list[i]->GetShape(); }
			ph::Shape const& GetShape() const       { return m_model.m_prim_list.back()->GetShape(); }
			
			// Serialise the shape into 'model_data'
			// It should be possible to insert the shape returned from here into a larger shape.
			// The highest level shape in a composite shape should have a shape_to_model transform of id.
			// Shape flags only apply to composite shape types
			ph::Shape* BuildShape(ByteCont& model_data, MassProperties& mp, v4& model_to_CoMframe, EShapeHierarchy hierarchy, EShapeFlags shape_flags);
			ph::Shape* BuildShape(ByteCont& model_data, MassProperties& mp, v4& model_to_CoMframe, EShapeHierarchy hierarchy)
			{
				return BuildShape(model_data, mp, model_to_CoMframe, hierarchy, EShapeFlags_None);
			}
			ph::Shape* BuildShape(ByteCont& model_data, MassProperties& mp, v4& model_to_CoMframe, EShapeFlags shape_flags)
			{
				if (m_model.m_prim_list.size() == 1) return BuildShape(model_data, mp, model_to_CoMframe, EShapeHierarchy_Single, shape_flags);
				else                                 return BuildShape(model_data, mp, model_to_CoMframe, EShapeHierarchy_Array , shape_flags);
			}
			ph::Shape* BuildShape(ByteCont& model_data, MassProperties& mp, v4& model_to_CoMframe)
			{
				if (m_model.m_prim_list.size() == 1) return BuildShape(model_data, mp, model_to_CoMframe, EShapeHierarchy_Single);
				else                                 return BuildShape(model_data, mp, model_to_CoMframe, EShapeHierarchy_Array);
			}
		};
	}
}

#endif
