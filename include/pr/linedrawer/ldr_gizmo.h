//***************************************************************************************************
// Ldr Manipulaion Gizmos
//  Copyright (c) Rylogic Ltd 2015
//***************************************************************************************************

#pragma once

#include <memory>
#include "pr/linedrawer/ldr_object.h"

namespace pr
{
	namespace ldr
	{
		// Graphics and functionality for a manipulator gizmo
		struct Gizmo :pr::AlignTo<16>
		{
			enum class EMode { Disabled, Translate, Rotate, Scale, };
			enum class EComponent { None, X, Y, Z, };

			pr::Camera*        m_cam;          // The camera used to view the scene containing the gizmo
			pr::Renderer*      m_rdr;          // The renderer, used to create the gizmo graphics
			ContextId          m_ctx_id;       // The context id to use for the gizmo graphics
			EMode              m_mode;         // The mode the gizmo is in
			LdrObjectPtr       m_gfx;          // The graphics object for the gizmo
			pr::m4x4           m_ref_o2w;      // The transform of the gizmo at the time manipulation began
			pr::v2             m_ref_pt;       // The normalised screen space location of where manipulation began
			EComponent         m_last_hit;     // The axis component last hit with the mouse
			EComponent         m_component;    // The axis component being manipulated
			bool               m_manipulating; // True while a manipulation is in progress
			bool               m_moved;        // True if the gizmo has moved, changed colour, or anything requiring a refresh

			// Create a manipulator gizmo
			// 'camera' is needed so that we can perform ray casts into the scene
			// to check for intersection with the gizmo.
			Gizmo(pr::Camera& camera, pr::Renderer& rdr, ContextId ctx_id, EMode mode);

			// Get/Set the mode the gizmo is in
			EMode Mode() const;
			void Mode(EMode mode);

			// Get/Set the gizmo object to world transform
			pr::m4x4 const& O2W() const;
			void O2W(pr::m4x4 const& o2w);

			// Returns the transform offset between the position when
			// manipulating started and the current gizmo position
			pr::m4x4 Offset() const;

			// Interact with the gizmo based on mouse movement.
			// 'pos_ns' should be normalised. i.e. x=[-1, -1], y=[-1,1] with (-1,-1) == (left,bottom). i.e. normal cartesian axes
			// The start of a mouse movement is indicated by 'btn_state' being non-zero
			// The end of the mouse movement is indicated by 'btn_state' being zero
			// 'btn_state' is one of the MK_LBUTTON, MK_RBUTTON, values
			// 'ref_point' should be true on the mouse down/up event, false while dragging
			void MouseControl(pr::v2 const& pos_ns, int btn_state, bool ref_point);

			// Perform a hit test given a normalised screen-space point
			EComponent HitTest(pr::v2 const& pos_ns);

		private:

			void DoTranslation(pr::v2 const& pos_ns);
			void DoRotation(pr::v2 const& pos_ns);
			void DoScale(pr::v2 const& pos_ns);
		};

		// Ref counted pointer to a gizmo
		typedef std::unique_ptr<Gizmo> GizmoPtr;
	}
}
