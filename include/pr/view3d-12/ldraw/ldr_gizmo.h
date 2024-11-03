//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
// Use:
//  Place a gizmo in the scene where you want it and with whatever scale you want.
//  Attach matrices directly to the gizmo, these get updated as the gizmo is used,
//  or, watch for gizmo events, and read
//  Forward mouse events to the gizmo to enable interaction
//  Call AddToScene to make the gizmo visible
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/instance/instance.h"
#include "pr/view3d-12/utility/pipe_state.h"

namespace pr::rdr12
{
	// Manipulation states
	enum class ELdrGizmoState
	{
		StartManip,
		Moving,
		Commit,
		Revert,
	};

	// The manipulation mode for a gizmo
	enum class ELdrGizmoMode
	{
		Translate,
		Rotate,
		Scale,
	};

	using GizmoMovedCB = StaticCB<void, LdrGizmo*, ELdrGizmoState>;

	// Graphics and functionality for a manipulator gizmo.
	struct alignas(16) LdrGizmo :RefCounted<LdrGizmo>
	{
		enum class EComponent { None, X, Y, Z, };
		using M4x4RefCont = pr::vector<m4x4>;
		using AttacheeCont = pr::vector<m4x4*>;

		// Graphics instance for the gizmo
		#define PR_RDR_INST(x)\
			x(m4x4       ,m_i2w   ,EInstComp::I2WTransform   )\
			x(PipeStates ,m_pso   ,EInstComp::PipeStates     )\
			x(ModelPtr   ,m_model ,EInstComp::ModelPtr       )\
			x(Colour32   ,m_colour,EInstComp::TintColour32   )\
			x(SKOverride ,m_sko   ,EInstComp::SortkeyOverride)
		PR_RDR12_DEFINE_INSTANCE(RdrInstance, PR_RDR_INST);
		#undef PR_RDR_INST

		struct alignas(16) Gfx
		{
			m4x4        m_o2w;     // The gizmo object to world
			ModelPtr    m_model;   // Single component model
			RdrInstance m_axis[3]; // An instance of the model for each component axis
			Gfx()
				:m_o2w(m4x4::Identity())
				,m_model()
				,m_axis()
			{}
		};

		M4x4RefCont   m_attached_ref; // A reference matrix for each attachee
		AttacheeCont  m_attached_ptr; // Pointers to the transform of the attachee object
		Renderer*     m_rdr;          // The renderer, used to create the gizmo graphics
		ELdrGizmoMode m_mode;         // The mode the gizmo is in
		Gfx           m_gfx;          // The graphics object for the gizmo
		float         m_scale;        // Scale factor for the gizmo
		m4x4          m_offset;       // The world-space offset transform between when manipulation began and now
		v2            m_ref_pt;       // The normalised screen space location of where manipulation began
		Colour32      m_col_hover;    // The colour the component axis has doing hover
		Colour32      m_col_manip;    // The colour the component axis has doing manipulation
		EComponent    m_last_hit;     // The axis component last hit with the mouse
		EComponent    m_component;    // The axis component being manipulated
		bool          m_manipulating; // True while a manipulation is in progress
		bool          m_impl_enabled; // True if this gizmo should respond to mouse interaction

		// Create a manipulator gizmo
		// 'camera' is needed so that we can perform ray casts into the scene
		// to check for intersection with the gizmo.
		// 'rdr' is used to create the graphics for the gizmo
		// 'mode' is the initial mode for the gizmo
		LdrGizmo(Renderer& rdr, ELdrGizmoMode mode, m4x4 const& o2w);

		// Render access
		Renderer& rdr() const;

		// Raised whenever the gizmo is manipulated
		MultiCast<GizmoMovedCB> Manipulated;

		// Get/Set the mode the gizmo is in
		bool Enabled() const;
		void Enabled(bool enabled);

		// True while manipulation is in progress
		bool Manipulating() const;

		// Get/Set the mode the gizmo is in
		ELdrGizmoMode Mode() const;
		void Mode(ELdrGizmoMode mode);

		// Get/Set the gizmo object to world transform (scale is allowed)
		m4x4 const& O2W() const;
		void O2W(m4x4 const& o2w);

		// Attach/Detach objects by direct reference to their transform which will be moved as the gizmo moves
		void Attach(m4x4& o2w);
		void Detach(m4x4 const& o2w);

		// Record the current matrices as the reference
		void Reference(v2 const& nss_point);

		// Reset all attached objects back to the reference position and end manipulation
		void Revert();

		// Set the ref matrices equal to the controlled matrices
		void Commit();

		// Returns the world space to world space offset transform between the position
		// when manipulation started and the current gizmo position (in world space)
		// Use: new_o2w = Offset() * old_o2w;
		m4x4 Offset() const;

		// Interact with the gizmo based on mouse movement.
		// 'nss_point' should be normalised. i.e. x=[-1, -1], y=[-1,1] with (-1,-1) == (left,bottom). i.e. normal Cartesian axes
		// The start of a mouse movement is indicated by 'btn_state' being non-zero
		// The end of the mouse movement is indicated by 'btn_state' being zero
		// 'nav_op' is a navigation/manipulation verb
		// 'ref_point' should be true on the mouse down/up event, false while dragging
		// Returns true if the gizmo has moved or changed colour
		bool MouseControl(Camera& camera, v2 const& nss_point, camera::ENavOp nav_op, bool ref_point);

		// Perform a hit test given a normalised screen-space point
		EComponent HitTest(Camera& camera, v2 const& nss_point);

		// Resets the other axes to the base colour and sets 'cp' to 'colour'
		void SetAxisColour(EComponent cp, Colour const& colour = ColourZero);

		// Add this gizmo to a scene
		void AddToScene(Scene& scene);

	private:

		void DoTranslation(Camera& camera, v2 const& nss_point);
		void DoRotation(Camera& camera, v2 const& nss_point);
		void DoScale(Camera& camera, v2 const& nss_point);
	};
}
