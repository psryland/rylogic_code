//*********************************************
// Collision
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#pragma once
#include "pr/physics2/forward.h"
#include "pr/physics2/shape/mass.h"
#include "pr/physics2/shape/material.h"

namespace pr
{
	namespace physics
	{
		// An object for building collision shapes
		// Note: the shape builder is part of the physics library, not the collision library
		// because it's main job is to determine the inertia properties of the shape, which
		// depends on physics materials, inertia matrices, etc.
		struct ShapeBuilder
		{
			// Settings for the shape builder
			struct Settings
			{
				float m_min_mass;   // The minimum mass a primitive may have (kg)
				float m_min_volume; // The minimum volume a primitive may have (m^3)
				std::function<Material const&(MaterialId)> m_mat_lookup;

				Settings()
					:m_min_mass(1.0f)
					,m_min_volume(0.001f * 0.001f * 0.001f)
					,m_mat_lookup(NoMaterial)
				{}
				static Material const& NoMaterial(MaterialId)
				{
					static Material s_mat;
					return s_mat;
				}
			};

			// Instances of primitives
			struct Prim :pr::AlignTo<16>
			{
				ByteData<16>    m_data;  // Data containing the shape
				MassProperties  m_mp;    // Mass properties for the primitive
				BBox            m_bbox;  // Bounding box for the primitive
				Shape mutable*  m_shape; // Used in the debugger only

				//Shape const& shape() const { m_shape = m_data.cbegin<Shape>(); return *m_shape; }
				Shape& shape()
				{
					m_shape = m_data.begin<Shape>();
					return *m_shape;
				}
			};

			// A collision model
			struct Model :pr::AlignTo<16>
			{
				using PrimList = std::vector<std::unique_ptr<Prim>>;
				PrimList       m_prim_list; // The primitives in the model
				MassProperties m_mp;        // The combined mass properties
				BBox           m_bbox;      // The model bounding box

				Model() = default;
				Model(Model const&) = delete;
				Model& operator = (Model const&) = delete;
			};

			Settings m_settings;
			std::unique_ptr<Model> m_model;

			ShapeBuilder(Settings const& settings = Settings())
				:m_settings(settings)
				,m_model(std::make_unique<Model>())
			{}

			// Begin a new physics model
			void Reset()
			{
				m_model = std::make_unique<Model>();
			}

			// Add shapes to the current model.
			template <typename TShape, typename = enable_if_shape<TShape>> void AddShape(TShape const& shape)
			{
				// Create a new primitive to contain the shape
				auto prim = std::make_unique<Prim>();
				prim->m_data.push_back(&shape, shape.m_base.m_size);
				auto& s = shape_cast<TShape>(prim->shape());

				// Convert the shape to canonical form (i.e. about it's centre of mass)
				auto density = m_settings.m_mat_lookup(s.m_base.m_material_id).m_density;
				prim->m_mp = CalcMassProperties(s, density);
				ShiftCentre(s, prim->m_mp.m_centre_of_mass);

				// Set the bounding box
				prim->m_bbox = CalcBBox(s);
				s.m_base.m_bbox = prim->m_bbox;

				// Validate the primitive
				if (prim->m_mp.m_mass < m_settings.m_min_volume * density)
					throw std::exception("Shape volume is too small");
				if (prim->m_mp.m_mass < m_settings.m_min_mass)
					prim->m_mp.m_mass = m_settings.m_min_mass;

				// Add the primitive to the model
				m_model->m_prim_list.push_back(std::move(prim));
			}

			// Serialise the shape data.
			// It should be possible to insert the shape returned from here into a larger shape.
			// The highest level shape in a composite shape should have a shape_to_model transform of identity.
			// Shape flags only apply to composite shape types
			Shape* BuildShape(ByteData<16>& model_data, MassProperties& mp, v4& model_to_CoMframe, EShape container = EShape::Array, Shape::EFlags shape_flags = Shape::EFlags::None)
			{
				auto& model = *m_model;

				assert("No shapes have been added" && !model.m_prim_list.empty());
				if (model.m_prim_list.size() == 1)
					container = EShape::NoShape;

				// Calculate the mass and centre of mass of the model
				CalculateMassAndCentreOfMass();

				// Move the model to the centre of mass frame
				MoveToCentreOfMassFrame(model_to_CoMframe);

				// Determine the bounding box for the whole model
				CalculateBoundingBox();

				// Create the inertia tensor for the model
				CalculateInertiaTensor();

				// Save the mass properties we've figured out
				mp = model.m_mp;

				auto base = model_data.size();
				switch (container)
				{
				default:
					{
						throw std::exception("Unsupported shape container type");
					}
				case EShape::NoShape:
					{
						assert("Model contains multiple primitives. 'hierarchy' should be one of the composite shape types" && model.m_prim_list.size() == 1);
						model_data.append(model.m_prim_list.front()->m_data);
						return &model_data.at_byte_ofs<Shape>(base);
					}
				case EShape::Array:
					{
						// Add the array shape header, followed by the shapes in the array
						model_data.push_back<ShapeArray>();
						for (auto& prim_ptr : model.m_prim_list)
							model_data.append(prim_ptr->m_data);

						// Update the array shape header
						auto& arr = model_data.at_byte_ofs<ShapeArray>(base);
						arr = ShapeArray(model.m_prim_list.size(), model_data.size() - base, m4x4Identity, 0, shape_flags);
						arr.m_base.m_bbox = model.m_bbox;

						// Return the shape
						return &arr.m_base;
					}
				case EShape::BVTree:
					{
						throw std::exception("Not implemented");
					}
				}
			}

			// Calculate the mass of the mode' by adding up the mass of all of the primitives.
			// Also, calculate the centre of mass for the object
			void CalculateMassAndCentreOfMass()
			{
				auto& model = *m_model;

				model.m_mp.m_mass = 0.0f;
				model.m_mp.m_centre_of_mass = pr::v4Zero;
				for (auto& prim_ptr : model.m_prim_list)
				{
					auto& prim = *prim_ptr;
					assert("All shapes should be centred on their centre of mass when added to the builder" && FEql3(prim.m_mp.m_centre_of_mass, v4Zero));

					// Accumulate mass and centre of mass
					model.m_mp.m_mass           += prim.m_mp.m_mass;
					model.m_mp.m_centre_of_mass += prim.m_mp.m_mass * prim.shape().m_s2p.pos;
				}

				// Find the centre of mass position
				model.m_mp.m_centre_of_mass /= model.m_mp.m_mass;
				model.m_mp.m_centre_of_mass.w = 0.0f;
			}

			// Relocate the collision model around the centre of mass
			void MoveToCentreOfMassFrame(v4& model_to_CoMframe)
			{
				auto& model = *m_model;

				// Save the shift from model space to centre of mass space
				model_to_CoMframe = model.m_mp.m_centre_of_mass;

				// Now move all of the models so that they are centred around the centre of mass
				for (auto& prim : model.m_prim_list)
					prim->shape().m_s2p.pos -= model.m_mp.m_centre_of_mass;

				// The offset to the centre of mass is now zero
				model.m_mp.m_centre_of_mass = v4Zero;
			}

			// Calculate the bounding box for 'm_model'.
			void CalculateBoundingBox()
			{
				auto& model = *m_model;

				model.m_bbox = BBoxReset;
				for (auto& prim : model.m_prim_list)
					Encompass(model.m_bbox, prim->shape().m_s2p * prim->m_bbox);
			}

			// Calculates the inertia tensor for 'm_model'
			void CalculateInertiaTensor()
			{
				auto& model = *m_model;

				model.m_mp.m_os_inertia_tensor = Inertia{};
				for (auto& p : model.m_prim_list)
				{
					auto& prim = *p;
					assert("All primitives should be in centre of mass frame" && FEql3(prim.m_mp.m_centre_of_mass, v4Zero));

					auto primitive_inertia  = prim.m_mp.m_mass * prim.m_mp.m_os_inertia_tensor;

					// Rotate the inertia tensor into object space
					auto prim_to_model = prim.shape().m_s2p.rot;
					primitive_inertia  = primitive_inertia.Rotate(prim_to_model);

					// Translate the inertia tensor using the parallel axis theorem
					primitive_inertia = primitive_inertia.Translate(prim.shape().m_s2p.pos, prim.m_mp.m_mass, Inertia::AwayFromCoM);

					// Add the inertia to the object inertia tensor
					model.m_mp.m_os_inertia_tensor += primitive_inertia;
				}

				// Assume mass == 1.0f for the model inertia tensor
				model.m_mp.m_os_inertia_tensor /= model.m_mp.m_mass;
			}

			// Return access to a shape
			Shape const& GetShape(int i) const
			{
				auto& model = *m_model;
				return model.m_prim_list[i]->shape();
			}
		};
	}
}


#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_physics_shape_builder)
		{
			using namespace pr::collision;
			using namespace pr::physics;

			ShapeBuilder sb;
			sb.AddShape(ShapeBox(pr::v4(0.2f, 0.4f, 0.3f, 0), pr::m4x4::Translation(0.0f, 0.0f, 0.3f)));
			//sb.AddShape(ShapeBox(pr::v4(0.2f, 0.4f, 0.3f, 0), pr::m4x4::Transform()));
		}
	}
}
#endif