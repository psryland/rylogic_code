//***************************************************************************************************
// Ldr Manipulation Gizmos
//  Copyright (c) Rylogic Ltd 2015
//***************************************************************************************************
// Use:
//  Place a gizmo in the scene where you want it and with whatever scale you want.
//  Attach matrices directly to the gizmo, these get updated as the gizmo is used,
//  or, watch for gizmo events, and read
//  Forward mouse events to the gizmo to enable interaction
//  Call AddToScene to make the gizmo visible
#pragma once

#include <memory>
#include "pr/macros/enum.h"
#include "pr/common/new.h"
#include "pr/common/refcount.h"
#include "pr/common/refptr.h"
#include "pr/common/events.h"
#include "pr/container/vector.h"
#include "pr/maths/maths.h"
#include "pr/renderer11/instance.h"

namespace pr
{
	namespace ldr
	{
		// Forwards
		struct LdrGizmo;
		typedef pr::RefPtr<LdrGizmo> LdrGizmoPtr;
		typedef pr::vector<LdrGizmoPtr, 8> GizmoCont;

		enum class ELdrGizmoEvent
		{
			StartManip,
			Moving,
			Commit,
			Revert,
		};

		// Events
		struct Evt_Gizmo
		{
			LdrGizmo*      m_gizmo;
			ELdrGizmoEvent m_state;

			Evt_Gizmo(LdrGizmo& gizmo, ELdrGizmoEvent state)
				:m_gizmo(&gizmo)
				,m_state(state)
			{}
		};

		// Static callback function
		struct LdrGizmoCB
		{
			typedef void (__stdcall *Func)(void* ctx, Evt_Gizmo const& args);
			Func m_func;
			void* m_ctx;
			LdrGizmoCB(Func func, void* ctx)
				:m_func(func)
				,m_ctx(ctx)
			{}
			void operator ()(Evt_Gizmo const& args) const
			{
				m_func(m_ctx, args);
			}
		};

		// Graphics and functionality for a manipulator gizmo.
		struct LdrGizmo
			:pr::AlignTo<16>
			,pr::RefCount<LdrGizmo>
		{
			enum class EMode { Translate, Rotate, Scale, };
			enum class EComponent { None, X, Y, Z, };
			typedef pr::vector<pr::m4x4>  M4x4RefCont;
			typedef pr::vector<pr::m4x4*> AttacheeCont;
			typedef pr::vector<LdrGizmoCB> CallbackCont;

			// Graphics instance for the gizmo
			#define PR_RDR_INST(x)\
				x(pr::m4x4            ,m_i2w    ,pr::rdr::EInstComp::I2WTransform   )\
				x(pr::rdr::ModelPtr   ,m_model  ,pr::rdr::EInstComp::ModelPtr       )\
				x(pr::Colour32        ,m_colour ,pr::rdr::EInstComp::TintColour32   )\
				x(pr::rdr::SKOverride ,m_sko    ,pr::rdr::EInstComp::SortkeyOverride)\
				x(pr::rdr::BSBlock    ,m_bsb    ,pr::rdr::EInstComp::BSBlock        )\
				x(pr::rdr::DSBlock    ,m_dsb    ,pr::rdr::EInstComp::DSBlock        )\
				x(pr::rdr::RSBlock    ,m_rsb    ,pr::rdr::EInstComp::RSBlock        )
			PR_RDR_DEFINE_INSTANCE(RdrInstance, PR_RDR_INST);
			#undef PR_RDR_INST

			struct Gfx :pr::AlignTo<16>
			{
				pr::m4x4          m_o2w;     // The gizmo object to world
				pr::rdr::ModelPtr m_model;   // Single component model
				RdrInstance       m_axis[3]; // An instance of the model for each component axis
				Gfx() :m_o2w(pr::m4x4Identity) ,m_model() ,m_axis() {}
			};

			M4x4RefCont      m_attached_ref; // A reference matrix for each attachee
			AttacheeCont     m_attached_ptr; // Pointers to the transform of the attachee object
			CallbackCont     m_callbacks;    // Callback functions to call as the gizmo is manipulated
			pr::Renderer*    m_rdr;          // The renderer, used to create the gizmo graphics
			EMode            m_mode;         // The mode the gizmo is in
			Gfx              m_gfx;          // The graphics object for the gizmo
			float            m_scale;        // Scale factor for the gizmo
			pr::m4x4         m_offset;       // The world-space offset transform between when manipulation began and now
			pr::v2           m_ref_pt;       // The normalised screen space location of where manipulation began
			pr::Colour32     m_col_hover;    // The colour the component axis has doing hover
			pr::Colour32     m_col_manip;    // The colour the component axis has doing manipulation
			EComponent       m_last_hit;     // The axis component last hit with the mouse
			EComponent       m_component;    // The axis component being manipulated
			bool             m_manipulating; // True while a manipulation is in progress
			bool             m_impl_enabled; // True if this gizmo should respond to mouse interaction

			// Create a manipulator gizmo
			// 'camera' is needed so that we can perform ray casts into the scene
			// to check for intersection with the gizmo.
			// 'rdr' is used to create the graphics for the gizmo
			// 'mode' is the initial mode for the gizmo
			LdrGizmo(pr::Renderer& rdr, EMode mode, pr::m4x4 const& o2w);

			// Get/Set the mode the gizmo is in
			bool Enabled() const;
			void Enabled(bool enabled);

			// True while manipulation is in progress
			bool Manipulating() const;

			// Get/Set the mode the gizmo is in
			EMode Mode() const;
			void Mode(EMode mode);

			// Get/Set the gizmo object to world transform (scale is allowed)
			pr::m4x4 const& O2W() const;
			void O2W(pr::m4x4 const& o2w);

			// Attach/Detach objects by direct reference to their transform which will be moved as the gizmo moves
			void Attach(pr::m4x4& o2w);
			void Detach(pr::m4x4 const& o2w);

			// Attach/Detach a callback that will be called whenever the gizmo moves
			void Attach(LdrGizmoCB::Func func, void* ctx);
			void Detach(LdrGizmoCB::Func func);

			// Record the current matrices as the reference
			void Reference(pr::v2 const& nss_point);

			// Reset all attached objects back to the reference position and end manipulation
			void Revert();

			// Set the ref matrices equal to the controlled matrices
			void Commit();

			// Returns the world space to world space offset transform between the position
			// when manipulation started and the current gizmo position (in world space)
			// Use: new_o2w = Offset() * old_o2w;
			pr::m4x4 Offset() const;

			// Interact with the gizmo based on mouse movement.
			// 'nss_point' should be normalised. i.e. x=[-1, -1], y=[-1,1] with (-1,-1) == (left,bottom). i.e. normal Cartesian axes
			// The start of a mouse movement is indicated by 'btn_state' being non-zero
			// The end of the mouse movement is indicated by 'btn_state' being zero
			// 'nav_op' is a navigation/manipulation verb
			// 'ref_point' should be true on the mouse down/up event, false while dragging
			// Returns true if the gizmo has moved or changed colour
			bool MouseControl(pr::Camera& camera, pr::v2 const& nss_point, pr::camera::ENavOp nav_op, bool ref_point);

			// Perform a hit test given a normalised screen-space point
			EComponent HitTest(pr::Camera& camera, pr::v2 const& nss_point);

			// Resets the other axes to the base colour and sets 'cp' to 'colour'
			void SetAxisColour(EComponent cp, pr::Colour const& colour = pr::ColourZero);

			// Add this gizmo to a scene
			void AddToScene(pr::rdr::Scene& scene);

		private:

			void DoTranslation(pr::Camera& camera, pr::v2 const& nss_point);
			void DoRotation(pr::Camera& camera, pr::v2 const& nss_point);
			void DoScale(pr::Camera& camera, pr::v2 const& nss_point);
		};
	}
}
