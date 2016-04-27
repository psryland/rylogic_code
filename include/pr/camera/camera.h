//******************************************
// Camera
//  Copyright (c) Rylogic Ltd 2009
//******************************************
// Camera to world matrix plus FoV and focus point
// Supports 3D trackball-like mouse control and basic keyboard control

#pragma once

#include <bitset>
#include "pr/macros/enum.h"
#include "pr/maths/maths.h"
#include "pr/common/assert.h"
#include "pr/common/keystate.h"

namespace pr
{
	namespace camera
	{
		#define PR_ENUM(x)/*
			*/x(Left     ,= 1 << 0)/* // MK_LBUTTON
			*/x(Right    ,= 1 << 1)/* // MK_RBUTTON
			*/x(Shift    ,= 1 << 2)/* // MK_SHIFT
			*/x(Ctrl     ,= 1 << 3)/* // MK_CONTROL
			*/x(Middle   ,= 1 << 4)/* // MK_MBUTTON
			*/x(XButton1 ,= 1 << 5)/* // MK_XBUTTON1
			*/x(XButton2 ,= 1 << 6)   // MK_XBUTTON2
		PR_DEFINE_ENUM2_FLAGS(ENavBtn, PR_ENUM);
		#undef PR_ENUM

		#define PR_ENUM(x)/*
			*/x(Left         )/*
			*/x(Up           )/*
			*/x(Right        )/*
			*/x(Down         )/*
			*/x(In           )/*
			*/x(Out          )/*
			*/x(Rotate       )/* // Key to enable camera rotations, maps translation keys to rotations
			*/x(TranslateZ   )/* // Key to set In/Out to be z translations rather than zoom
			*/x(Accurate     )/*
			*/x(SuperAccurate)
		PR_DEFINE_ENUM1(ENavKey, PR_ENUM);
		#undef PR_ENUM

		// Map keys to the basic camera controls
		struct NavKeyBindings
		{
			int m_bindings[ENavKey::NumberOf];
			int operator[](ENavKey key) const { return m_bindings[key]; }
			NavKeyBindings()
			{
				m_bindings[ENavKey::Left         ] = VK_LEFT;
				m_bindings[ENavKey::Up           ] = VK_UP;
				m_bindings[ENavKey::Right        ] = VK_RIGHT;
				m_bindings[ENavKey::Down         ] = VK_DOWN;
				m_bindings[ENavKey::In           ] = VK_HOME;
				m_bindings[ENavKey::Out          ] = VK_END;
				m_bindings[ENavKey::Rotate       ] = VK_SHIFT;
				m_bindings[ENavKey::TranslateZ   ] = VK_CONTROL;
				m_bindings[ENavKey::Accurate     ] = VK_SHIFT;
				m_bindings[ENavKey::SuperAccurate] = VK_CONTROL;
			}
		};

		// Prevent translation/rotation on particular axes
		struct LockMask :std::bitset<8>
		{
			enum
			{
				TransX         = 0,
				TransY         = 1,
				TransZ         = 2,
				RotX           = 3,
				RotY           = 4,
				RotZ           = 5,
				Zoom           = 6,
				CameraRelative = 7,
				All            = (1 << 7) - 1, // Not including camera relative
			};
			operator bool() const { return (to_ulong() & All) != 0; }
		};
	}

	// Camera matrix with 3D trackball-like control
	// Note:
	// All points are in normalised screen space regardless of aspect ratio,
	//  i.e. x=[-1, -1], y=[-1,1] with (-1,-1) = (left,bottom), i.e. normal cartesian axes.
	// Use:
	//  point = pr::v2::make(2.0f * pt.x / float(Width) - 1.0f, 1.0f - 2.0f * pt.y / float(Height));
	struct Camera
	{
		pr::m4x4               m_base_c2w;          // The starting position during a mouse movement
		pr::m4x4               m_c2w;               // Camera to world transform
		camera::NavKeyBindings m_key;               // Key bindings
		float                  m_default_fovY;      // The default field of view
		pr::v4                 m_align;             // The directon to align 'up' to, or v4Zero
		camera::LockMask       m_lock_mask;         // Locks on the allowed motion
		bool                   m_orthographic;      // True for orthographic camera to screen transforms, false for perspective
		float                  m_base_fovY;         // The starting fov during a mouse movement
		float                  m_fovY;              // Field of view in the Y direction
		float                  m_base_focus_dist;   // The starting focus distance during a mouse movement
		float                  m_focus_dist;        // Distance from the c2w position to the focus, down the z axis
		float                  m_aspect;            // Aspect ratio = width/height
		float                  m_near;              // The near plane as a multiple of the focus distance
		float                  m_far;               // The near plane as a multiple of the focus distance
		float                  m_accuracy_scale;    // Scale factor for high accuracy control
		pr::v2                 m_Lref;              // Movement start reference point for the left button
		pr::v2                 m_Rref;              // Movement start reference point for the right button
		pr::v2                 m_Mref;              // Movement start reference point for the middle button
		bool                   m_moved;             // Dirty flag for when the camera moves
		bool                   m_focus_rel_clip;    // True if the near/far clip planes should be relative to the focus point

		Camera(float fovY = pr::maths::tau_by_8, float aspect = 1.0f)
			:m_base_c2w(pr::m4x4Identity)
			,m_c2w(pr::m4x4Identity)
			,m_key()
			,m_default_fovY(fovY)
			,m_align(v4Zero)
			,m_lock_mask()
			,m_orthographic(false)
			,m_base_fovY(m_default_fovY)
			,m_fovY(m_base_fovY)
			,m_base_focus_dist(1.0f)
			,m_focus_dist(m_base_focus_dist)
			,m_aspect(aspect)
			,m_near(0.01f)
			,m_far(100.0f)
			,m_accuracy_scale(0.1f)
			,m_Lref(v2Zero)
			,m_Rref(v2Zero)
			,m_Mref(v2Zero)
			,m_moved(false)
			,m_focus_rel_clip(true)
		{}

		// Return the camera to world transform
		void CameraToWorld(pr::m4x4 const& c2w, bool commit = true)
		{
			m_c2w = c2w;
			if (commit) Commit();
		}
		pr::m4x4 CameraToWorld() const
		{
			return m_c2w;
		}
		pr::m4x4 WorldToCamera() const
		{
			return InvertFast(m_c2w);
		}

		// Return a perspective projection transform
		pr::m4x4 CameraToScreen(float near_clip, float far_clip) const
		{
			float height = 2.0f * m_focus_dist * pr::Tan(m_fovY * 0.5f);
			return m_orthographic
				? m4x4::ProjectionOrthographic(height*m_aspect, height, near_clip, far_clip, true)
				: m4x4::ProjectionPerspectiveFOV(m_fovY, m_aspect, near_clip, far_clip, true);
		}
		pr::m4x4 CameraToScreen() const
		{
			return CameraToScreen(Near(), Far());
		}

		// Note, the camera does not contain any info about the size of the screen
		// that the camera view is on. Therefore, there are no screen space to normalised
		// screen space methods in here. You need the Window for that.

		// Return a point in world space corresponding to a normalised screen space point.
		// The x,y components of 'nss_point' should be in normalised screen space, i.e. (-1,-1)->(1,1)
		// The z component should be the depth into the screen (i.e. d*-c2w.z, where 'd' is typically positive)
		pr::v4 NSSPointToWSPoint(pr::v4 const& nss_point) const
		{
			float half_height = m_focus_dist * pr::Tan(m_fovY * 0.5f);

			// Calculate the point in camera space
			pr::v4 point;
			point.x = nss_point.x * m_aspect * half_height;
			point.y = nss_point.y * half_height;
			if (!m_orthographic)
			{
				float sz = nss_point.z / m_focus_dist;
				point.x *= sz;
				point.y *= sz;
			}
			point.z = -nss_point.z;
			point.w = 1.0f;
			return m_c2w * point; // camera space to world space
		}

		// Return a point in normalised screen space corresponding to 'ws_point'
		// The returned 'z' component will be the world space distance from the camera
		pr::v4 WSPointToNSSPoint(pr::v4 const& ws_point) const
		{
			float half_height = m_focus_dist * pr::Tan(m_fovY * 0.5f);

			// Get the point in camera space and project into normalised screen space
			pr::v4 cam = InvertFast(m_c2w) * ws_point;

			pr::v4 point;
			point.x = cam.x / (m_aspect * half_height);
			point.y = cam.y / (half_height);
			if (!m_orthographic)
			{
				float sz = -m_focus_dist / cam.z;
				point.x *= sz;
				point.y *= sz;
			}
			point.z = -cam.z;
			point.w = 1.0f;
			return point;
		}

		// Return a point and direction in world space corresponding to a normalised sceen space point.
		// The x,y components of 'nss_point' should be in normalised screen space, i.e. (-1,-1)->(1,1)
		// The z component should be the world space distance from the camera
		void NSSPointToWSRay(pr::v4 const& nss_point, pr::v4& ws_point, pr::v4& ws_direction) const
		{
			pr::v4 pt = NSSPointToWSPoint(nss_point);
			ws_point = m_c2w.pos;
			ws_direction = pr::Normalise3(pt - ws_point);
		}

		// Set the distances to the near and far clip planes
		void ClipPlanes(float near_, float far_, bool focus_relative_clip)
		{
			m_near = near_;
			m_far = far_;
			m_focus_rel_clip = focus_relative_clip;
		}

		// Returns a distance scaled by the focus distance (if m_focus_rel_clip is enabled)
		float FocusRelativeDistance(float dist) const
		{
			return (m_focus_rel_clip ? m_focus_dist : 1) * dist;
		}

		// The near clip plane
		float Near() const
		{
			return FocusRelativeDistance(m_near);
		}

		// The far clip plane
		float Far() const
		{
			return FocusRelativeDistance(m_far);
		}

		// Return the aspect ratio
		float Aspect() const
		{
			return m_aspect;
		}

		// Set the aspect ratios
		void Aspect(float aspect_w_by_h)
		{
			PR_ASSERT(PR_DBG, aspect_w_by_h > 0.0f && pr::IsFinite(aspect_w_by_h), "");
			m_moved = aspect_w_by_h != m_aspect;
			m_aspect = aspect_w_by_h;
		}

		// Return the horizontal field of view (in radians).
		float FovX() const
		{
			return 2.0f * pr::ATan(pr::Tan(m_fovY * 0.5f) * m_aspect);
		}

		// Set the XAxis field of view
		void FovX(float fovX)
		{
			FovY(2.0f * pr::ATan(pr::Tan(fovX * 0.5f) / m_aspect));
		}

		// Return the vertical field of view (in radians).
		float FovY() const
		{
			return m_fovY;
		}

		// Set the YAxis field of view. FOV relationship: tan(fovY/2) * aspect_w_by_h = tan(fovX/2);
		void FovY(float fovY)
		{
			PR_ASSERT(PR_DBG, fovY >= 0.0f && pr::IsFinite(fovY), "");
			m_moved = fovY != m_fovY;
			m_base_fovY = m_fovY = fovY;
		}

		// Set both X and Y axis field of view. Implies aspect ratio
		void Fov(float fovX, float fovY)
		{
			auto aspect = pr::Tan(fovX/2) / pr::Tan(fovY/2);
			Aspect(aspect);
			FovY(fovY);
		}

		// Return the size of the perpendicular area visible to the camera at 'dist' (in world space)
		pr::v2 ViewArea(float dist) const
		{
			auto h = 2.0f * pr::Tan(m_fovY * 0.5f);
			return m_orthographic
				? pr::v2(h * m_aspect, h)
				: pr::v2(dist * h * m_aspect, dist * h);
		}

		// Return the view frustum for this camera
		Frustum ViewFrustum() const
		{
			return pr::Frustum::makeFA(m_fovY, m_aspect);
		}

		// Return the world space position of the focus point
		pr::v4 FocusPoint() const
		{
			return m_c2w.pos - m_c2w.z * m_focus_dist;
		}

		// Set the focus point, maintaining the current camera orientation
		void FocusPoint(pr::v4 const& position)
		{
			m_moved = true;
			m_c2w.pos += position - FocusPoint();
			m_base_c2w = m_c2w; // Update the base point
		}

		// Return the distance to the focus point
		float FocusDist() const
		{
			return m_focus_dist;
		}

		// Set the distance to the focus point
		void FocusDist(float dist)
		{
			PR_ASSERT(PR_DBG, pr::IsFinite(dist), "");
			m_moved = dist != m_focus_dist;
			m_base_focus_dist = m_focus_dist = dist;
		}

		// Modify the camera position based on mouse movement.
		// 'point' should be normalised. i.e. x=[-1, -1], y=[-1,1] with (-1,-1) == (left,bottom). i.e. normal cartesian axes
		// The start of a mouse movement is indicated by 'btn_state' being non-zero
		// The end of the mouse movement is indicated by 'btn_state' being zero
		// 'ref_point' should be true on the mouse down/up event, false while dragging
		// Returns true if the camera has moved
		bool MouseControl(pr::v2 const& point, camera::ENavBtn btn_state, bool ref_point)
		{
			// Button states
			bool lbtn = (btn_state & camera::ENavBtn::Left) != 0;
			bool rbtn = (btn_state & camera::ENavBtn::Right) != 0;
			bool mbtn = (btn_state & camera::ENavBtn::Middle) != 0;

			if (ref_point)
			{
				if (btn_state & camera::ENavBtn::Left)   m_Lref = point;
				if (btn_state & camera::ENavBtn::Right)  m_Rref = point;
				if (btn_state & camera::ENavBtn::Middle) m_Mref = point;
				Commit();
			}
			if (mbtn || (lbtn && rbtn))
			{
				if (KeyDown(m_key[camera::ENavKey::TranslateZ]))
				{
					// Move in a fraction of the focus distance
					float delta = mbtn ? (point.y - m_Mref.y) : (point.y - m_Lref.y);
					Translate(0, 0, delta * 10.0f, false);
				}
				else
				{
					// Zoom the field of view
					float zoom = mbtn ? (m_Mref.y - point.y) : (m_Lref.y - point.y);
					Zoom(zoom, false);
				}
			}
			if (lbtn && !rbtn)
			{
				float dx = (m_Lref.x - point.x) * m_focus_dist * pr::Tan(m_fovY * 0.5f) * m_aspect;
				float dy = (m_Lref.y - point.y) * m_focus_dist * pr::Tan(m_fovY * 0.5f);
				Translate(dx, dy, 0.0f, false);
			}
			if (rbtn && !lbtn)
			{
				// If in the roll zone
				if (Length2(m_Rref) < 0.80f) Rotate((point.y - m_Rref.y) * maths::tau_by_4, (m_Rref.x - point.x) * maths::tau_by_4, 0.0f, false);
				else                         Rotate(0.0f, 0.0f, ATan2(m_Rref.y, m_Rref.x) - ATan2(point.y, point.x), false);
			}
			return m_moved;
		}

		// Translate by a camera relative amount
		// Returns true if the camera has moved (for consistency with MouseControl)
		bool Translate(float dx, float dy, float dz, bool commit = true)
		{
			if (m_lock_mask && m_lock_mask[camera::LockMask::CameraRelative])
			{
				if (m_lock_mask[camera::LockMask::TransX]) dx = 0.0f;
				if (m_lock_mask[camera::LockMask::TransY]) dy = 0.0f;
				if (m_lock_mask[camera::LockMask::TransZ]) dz = 0.0f;
			}
			if (KeyDown(m_key[camera::ENavKey::Accurate]))
			{
				dx *= m_accuracy_scale;
				dy *= m_accuracy_scale;
				dz *= m_accuracy_scale;
				if (KeyDown(m_key[camera::ENavKey::SuperAccurate]))
				{
					dx *= m_accuracy_scale;
					dy *= m_accuracy_scale;
					dz *= m_accuracy_scale;
				}
			}

			// Move in a fraction of the focus distance
			dz = -m_base_focus_dist * dz * 0.1f;
			if (!KeyDown(m_key[camera::ENavKey::TranslateZ]))
				m_focus_dist = m_base_focus_dist + dz;

			// Translate
			auto pos = m_base_c2w.pos + m_base_c2w.rot * pr::v4(dx, dy, dz, 0.0f);
			if (IsFinite(pos))
				m_c2w.pos = pos;

			// Apply non-camera relative locking
			if (m_lock_mask && !m_lock_mask[camera::LockMask::CameraRelative])
			{
				if (m_lock_mask[camera::LockMask::TransX]) m_c2w.pos.x = m_base_c2w.pos.x;
				if (m_lock_mask[camera::LockMask::TransY]) m_c2w.pos.y = m_base_c2w.pos.y;
				if (m_lock_mask[camera::LockMask::TransZ]) m_c2w.pos.z = m_base_c2w.pos.z;
			}

			// Set the base values
			if (commit) Commit();

			m_moved = true;
			return m_moved;
		}

		// Rotate the camera by Euler angles about the focus point
		// Returns true if the camera has moved (for consistency with MouseControl)
		bool Rotate(float pitch, float yaw, float roll, bool commit = true)
		{
			if (m_lock_mask)
			{
				if (m_lock_mask[camera::LockMask::RotX]) pitch	= 0.0f;
				if (m_lock_mask[camera::LockMask::RotY]) yaw	= 0.0f;
				if (m_lock_mask[camera::LockMask::RotZ]) roll	= 0.0f;
			}
			if (KeyDown(m_key[camera::ENavKey::Accurate]))
			{
				pitch *= m_accuracy_scale;
				yaw   *= m_accuracy_scale;
				roll  *= m_accuracy_scale;
				if (KeyDown(m_key[camera::ENavKey::SuperAccurate]))
				{
					pitch *= m_accuracy_scale;
					yaw   *= m_accuracy_scale;
					roll  *= m_accuracy_scale;
				}
			}

			// Save the world space position of the focus point
			pr::v4 old_focus = FocusPoint();

			// Rotate the camera matrix
			m_c2w = m_base_c2w * m4x4::Rotation(pitch, yaw, roll, v4Origin);

			// Position the camera so that the focus is still in the same position
			m_c2w.pos = old_focus + m_c2w.z * m_focus_dist;

			// If an align axis is given, align up to it
			if (Length3Sq(m_align) > maths::tiny)
				m_c2w = m4x4::LookAt(m_c2w.pos, old_focus, m_align);

			// Set the base values
			if (commit) Commit();

			m_moved = true;
			return m_moved;
		}

		// Zoom the field of view. 'zoom' should be in the range (-1, 1) where negative numbers zoom in, positive out
		// Returns true if the camera has moved (for consistency with MouseControl)
		bool Zoom(float zoom, bool commit = true)
		{
			if (m_lock_mask)
			{
				if (m_lock_mask[camera::LockMask::Zoom]) return false;
			}
			if (KeyDown(m_key[camera::ENavKey::Accurate]))
			{
				zoom *= m_accuracy_scale;
				if (KeyDown(m_key[camera::ENavKey::SuperAccurate]))
					zoom *= m_accuracy_scale;
			}

			m_fovY = (1.0f + zoom) * m_base_fovY;
			m_fovY = pr::Clamp(m_fovY, pr::maths::tiny, pr::maths::tau_by_2 - pr::maths::tiny);

			// Set the base values
			if (commit) Commit();

			m_moved = true;
			return m_moved;
		}

		// Set the current position, fov, and focus distance as the position reference
		void Commit()
		{
			m_base_c2w  = m_c2w;
			m_base_fovY = m_fovY;
			m_base_focus_dist = m_focus_dist;
			m_base_c2w = pr::Orthonorm(m_base_c2w);
		}

		// Revert navigation back to the last commit
		void Revert()
		{
			m_c2w = m_base_c2w;
			m_fovY = m_base_fovY;
			m_focus_dist = m_base_focus_dist;
			m_moved = true;
		}

		// Return the current zoom scaling factor
		float Zoom() const
		{
			return m_default_fovY / m_fovY;
		}

		// Reset the fov to the default
		void ResetZoom()
		{
			m_moved = true;
			m_base_fovY = m_fovY = m_default_fovY;
		}

		// Set the axis to align the camera up axis to
		void SetAlign(pr::v4 const& up)
		{
			m_align = up;
			if (Length3Sq(m_align) > maths::tiny)
			{
				if (pr::Parallel3(m_c2w.z, m_align)) m_c2w = m4x4(m_c2w.y, m_c2w.z, m_c2w.x, m_c2w.w);
				m_c2w = m4x4::LookAt(m_c2w.pos, FocusPoint(), m_align);
				m_moved = true;
				m_base_c2w = m_c2w; // Update the base point
			}
		}

		// Return true if the align axis has been set for the camera
		bool IsAligned() const
		{
			return Length3Sq(m_align) > maths::tiny;
		}

		// Position the camera at 'position' looking at 'lookat' with up pointing 'up'
		void LookAt(pr::v4 const& position, pr::v4 const& lookat, pr::v4 const& up, bool commit = true)
		{
			m_c2w = m4x4::LookAt(position, lookat, up);
			m_focus_dist = Length3(lookat - position);

			// Set the base values
			if (commit) Commit();
		}

		// Position the camera so that all of 'bbox' is visible to the camera when looking 'forward' and 'up'
		void View(pr::BBox const& bbox, pr::v4 const& forward, pr::v4 const& up, bool update_base)
		{
			if (bbox.empty()) return;

			pr::v4 bbox_centre = bbox.Centre();
			pr::v4 bbox_radius = bbox.Radius();

			// Get the radius of the bbox projected onto the plane 'forward'
			float sizez = pr::maths::float_max;
			sizez = pr::Min(sizez, pr::Abs(pr::Dot3(forward, pr::v4( bbox_radius.x,  bbox_radius.y,  bbox_radius.z, 0.0f))));
			sizez = pr::Min(sizez, pr::Abs(pr::Dot3(forward, pr::v4(-bbox_radius.x,  bbox_radius.y,  bbox_radius.z, 0.0f))));
			sizez = pr::Min(sizez, pr::Abs(pr::Dot3(forward, pr::v4( bbox_radius.x, -bbox_radius.y,  bbox_radius.z, 0.0f))));
			sizez = pr::Min(sizez, pr::Abs(pr::Dot3(forward, pr::v4( bbox_radius.x,  bbox_radius.y, -bbox_radius.z, 0.0f))));
			float sizexy = pr::Sqrt(pr::Clamp(pr::Length3Sq(bbox_radius) - pr::Sqr(sizez), 0.0f, pr::maths::float_max));

			// 'sizexy' is the radius of the bounding box projected into the 'forward' plane
			// 'dist' is the distance at which 'sizexy' is visible
			// => the distance from camera to bbox_centre is 'dist + sizez'
			float fov = m_aspect >= 1.0f ? m_fovY : m_fovY * m_aspect;
			float dist = sizexy / pr::Tan(fov * 0.5f);

			LookAt(bbox_centre - forward * (sizez + dist), bbox_centre, up, update_base);
		}

		// Orbit the camera about the focus point by 'angle_rad' radians
		void Orbit(float angle_rad, bool commit = true)
		{
			// Record the focus point
			pr::v4 old_focus = FocusPoint();

			// Find the axis of rotation
			pr::v4 axis = IsAligned() ? pr::InvertFast(m_c2w) * m_align : m_c2w.y;

			// Rotate the camera transform and reposition to look at the focus point
			m_c2w     = m_c2w * m4x4::Rotation(axis, angle_rad, v4Origin);
			m_c2w.pos = old_focus + m_focus_dist * m_c2w.z;
			m_c2w     = pr::Orthonorm(m_c2w);

			// Set the base values
			if (commit)
				Commit();

			m_moved = true;
		}

		// Keyboard navigation. Remember to use 'if (GetForegroundWindow() == GetConsoleWindow())' for navigation only while the window has focus
		void KBNav(float mov, float rot)
		{
			// Control translates
			if (!KeyDown(VK_CONTROL))
			{
				if (KeyDown(VK_LEFT )) Rotate(0, rot,0,true);
				if (KeyDown(VK_RIGHT)) Rotate(0,-rot,0,true);
				if (KeyDown(VK_UP   )) Rotate(-rot,0,0,true);
				if (KeyDown(VK_DOWN )) Rotate( rot,0,0,true);
				if (KeyDown(VK_HOME )) Translate(0, mov,0,true);
				if (KeyDown(VK_END  )) Translate(0,-mov,0,true);
			}
			else
			{
				if (KeyDown(VK_LEFT )) Translate(-mov,0,0,true);
				if (KeyDown(VK_RIGHT)) Translate( mov,0,0,true);
				if (KeyDown(VK_UP   )) Translate(0,0,-mov,true);
				if (KeyDown(VK_DOWN )) Translate(0,0, mov,true);
			}
		}
	};
}
