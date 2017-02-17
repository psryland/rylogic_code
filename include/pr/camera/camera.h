//******************************************
// Camera
//  Copyright (c) Rylogic Ltd 2009
//******************************************
// Camera to world matrix plus FoV and focus point
// Supports 3D trackball-like mouse control and basic keyboard control

#pragma once

#include <bitset>
#include "pr/maths/maths.h"
#include "pr/maths/bit_fields.h"
#include "pr/common/assert.h"
#include "pr/common/keystate.h"
#include "pr/common/flags_enum.h"

namespace pr
{
	namespace camera
	{
		// Navigation verbs
		enum class ENavOp
		{
			None      = 0,
			Translate = 1 << 0,
			Rotate    = 1 << 1,
			Zoom      = 1 << 2,
			_bitwise_operators_allowed,
		};

		// Navigation keys
		enum class ENavKey
		{
			Left,
			Up,
			Right,
			Down,
			In,
			Out,
			Rotate,     // Key to enable camera rotations, maps translation keys to rotations
			TranslateZ, // Key to set In/Out to be z translations rather than zoom
			Accurate,
			SuperAccurate,
			PerpendicularZ,
			NumberOf,
		};

		// Map keys to the basic camera controls
		struct NavKeyBindings
		{
			int m_bindings[int(ENavKey::NumberOf)];
			int operator[](ENavKey key) const { return m_bindings[int(key)]; }
			NavKeyBindings()
			{
				m_bindings[int(ENavKey::Left          )] = VK_LEFT;
				m_bindings[int(ENavKey::Up            )] = VK_UP;
				m_bindings[int(ENavKey::Right         )] = VK_RIGHT;
				m_bindings[int(ENavKey::Down          )] = VK_DOWN;
				m_bindings[int(ENavKey::In            )] = VK_HOME;
				m_bindings[int(ENavKey::Out           )] = VK_END;
				m_bindings[int(ENavKey::Rotate        )] = VK_SHIFT;
				m_bindings[int(ENavKey::TranslateZ    )] = VK_CONTROL;
				m_bindings[int(ENavKey::Accurate      )] = VK_SHIFT;
				m_bindings[int(ENavKey::SuperAccurate )] = VK_CONTROL;
				m_bindings[int(ENavKey::PerpendicularZ)] = VK_MENU;
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

		// Convert an MK_ value into the default navigation operation
		inline ENavOp MouseBtnToNavOp(int mk)
		{
			auto op = ENavOp::None;
			if (pr::AllSet(mk, MK_LBUTTON)) op |= ENavOp::Rotate;
			if (pr::AllSet(mk, MK_RBUTTON)) op |= ENavOp::Translate;
			if (pr::AllSet(mk, MK_MBUTTON)) op |= ENavOp::Zoom;
			return op;
		}
	}

	// Camera matrix with 3D trackball-like control
	// Note:
	// All points are in normalised screen space regardless of aspect ratio,
	//  i.e. x=[-1, -1], y=[-1,1] with (-1,-1) = (left,bottom), i.e. normal Cartesian axes.
	// Use:
	//  point = v2(2.0f * pt.x / float(Width) - 1.0f, 1.0f - 2.0f * pt.y / float(Height));
	struct Camera
	{
		using NavKeyBindings = camera::NavKeyBindings;
		using LockMask       = camera::LockMask;
		using ENavOp         = camera::ENavOp;

		m4x4           m_base_c2w;          // The starting position during a mouse movement
		m4x4           m_c2w;               // Camera to world transform
		v4             m_align;             // The direction to align 'up' to, or v4Zero
		float          m_default_fovY;      // The default field of view
		float          m_base_fovY;         // The starting FOV during a mouse movement
		float          m_fovY;              // Field of view in the Y direction
		float          m_base_focus_dist;   // The starting focus distance during a mouse movement
		float          m_focus_dist;        // Distance from the c2w position to the focus, down the z axis
		float          m_aspect;            // Aspect ratio = width/height
		float          m_near;              // The near plane as a multiple of the focus distance
		float          m_far;               // The near plane as a multiple of the focus distance
		float          m_accuracy_scale;    // Scale factor for high accuracy control
		v2             m_Tref;              // Movement start reference point for translation
		v2             m_Rref;              // Movement start reference point for rotation
		v2             m_Zref;              // Movement start reference point for zoom
		NavKeyBindings m_key;               // Key bindings
		LockMask       m_lock_mask;         // Locks on the allowed motion
		bool           m_orthographic;      // True for orthographic camera to screen transforms, false for perspective
		bool           m_moved;             // Dirty flag for when the camera moves
		bool           m_focus_rel_clip;    // True if the near/far clip planes should be relative to the focus point

		Camera()
			:Camera(pr::m4x4Identity)
		{}
		Camera(m4x4 const& c2w, float fovY = pr::maths::tau_by_8, float aspect = 1.0f, float focus_dist = 1.0f)
			:Camera(c2w, fovY, aspect, focus_dist, false, 0.01f, 100.0f)
		{}
		Camera(m4x4 const& c2w, float fovY, float aspect, float focus_dist, bool orthographic, float near_, float far_)
			:m_base_c2w(c2w)
			,m_c2w(m_base_c2w)
			,m_align(v4Zero)
			,m_default_fovY(fovY)
			,m_base_fovY(m_default_fovY)
			,m_fovY(m_base_fovY)
			,m_base_focus_dist(focus_dist)
			,m_focus_dist(m_base_focus_dist)
			,m_aspect(aspect)
			,m_near(near_)
			,m_far(far_)
			,m_accuracy_scale(0.1f)
			,m_Tref(v2Zero)
			,m_Rref(v2Zero)
			,m_Zref(v2Zero)
			,m_key()
			,m_lock_mask()
			,m_orthographic(orthographic)
			,m_moved(false)
			,m_focus_rel_clip(true)
		{
			PR_ASSERT(PR_DBG, pr::IsFinite(m_c2w), "invalid scene view parameters");
			PR_ASSERT(PR_DBG, pr::IsFinite(m_fovY), "invalid scene view parameters");
			PR_ASSERT(PR_DBG, pr::IsFinite(m_aspect), "invalid scene view parameters");
			PR_ASSERT(PR_DBG, pr::IsFinite(m_focus_dist), "invalid scene view parameters");
		}
		Camera(v4_cref eye, v4_cref pt, v4_cref up, float fovY = maths::tau_by_8, float aspect = 1.0f)
			:Camera(pr::m4x4Identity, fovY, aspect)
		{
			LookAt(eye, pt, up, true);
		}

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

		// Return a point and direction in world space corresponding to a normalised screen space point.
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
			PR_ASSERT(PR_DBG, fovY >= 0.0f && fovY < maths::tau_by_2 && pr::IsFinite(fovY), "");
			
			fovY = Clamp(fovY, maths::tiny, float(maths::tau_by_2));
			m_moved = fovY != m_fovY;
			m_base_fovY = m_fovY = fovY;
		}

		// Set both X and Y axis field of view. Implies aspect ratio
		void Fov(float fovX, float fovY)
		{
			PR_ASSERT(PR_DBG, pr::IsFinite(fovX) && pr::IsFinite(fovY), "");
			PR_ASSERT(PR_DBG, fovX < maths::tau_by_2 && fovY < maths::tau_by_2, "");
			fovX = Clamp(fovX, maths::tiny, float(maths::tau_by_2));
			fovY = Clamp(fovY, maths::tiny, float(maths::tau_by_2));
			auto aspect = pr::Tan(fovX/2) / pr::Tan(fovY/2);
			Aspect(aspect);
			FovY(fovY);
		}

		// Adjust the FocusDist, FovX, and FovY so that the average FOV equals 'fov'
		void BalanceFov(float fov)
		{
			// Measure the current focus distance and view size at that distance
			auto d = FocusDist();
			auto pt = FocusPoint();
			auto wh = ViewArea(d);
			auto size = (wh.x + wh.y) * 0.5f;

			// The focus distance at 'fov' with a view size of 'size' is:
			//'   d2 = (0.5 * size) / tan(0.5 * fov);
			// The FOV at distance 'd2' is 
			//'   fovX = 2 * atan((wh.x * 0.5) / d2);
			//'   fovY = 2 * atan((wh.y * 0.5) / d2);
			// Since the aspect is unchanged, we only need to calculate fovY
			// Simplifying by substituting for 'd2'
			//'   fovY = 2 * atan((wh.y * 0.5) / ((0.5 * size) / tan(0.5 * fov)));
			//'   fovY = 2 * atan((wh.y * 0.5) * tan(0.5 * fov) / (0.5 * size));
			//'   fovY = 2 * atan(wh.y * tan(0.5 * fov) / size);
			//'   fovY = 2 * atan(tan(0.5 * fov) * wh.y / size);

			// Calculate the actual Y FOV at 'd2'
			auto d2 = (0.5f * size) / pr::Tan(0.5f * fov);
			auto fovY = 2.0f * pr::ATan((0.5f * wh.y) / d2);

			FovY(fovY);
			FocusDist(d2);
			FocusPoint(pt);
		}

		// Return the size of the perpendicular area visible to the camera at 'dist' (in world space)
		pr::v2 ViewArea(float dist) const
		{
			dist = m_orthographic ? m_focus_dist : dist;
			auto h = 2.0f * pr::Tan(m_fovY * 0.5f);
			return pr::v2(dist * h * m_aspect, dist * h);
		}

		// Return the view frustum for this camera
		Frustum ViewFrustum(float zfar) const
		{
			return Frustum::makeFA(m_fovY, m_aspect, zfar);
		}
		Frustum ViewFrustum() const
		{
			return ViewFrustum(m_far);
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

		// Return the maximum allowed distance for 'FocusDist'
		float FocusDistMax() const
		{
			// Clamp so that Near*Far is finite
			// N*F == (m_near * dist) * (m_far * dist) < float_max
			//     == m_near * m_far * dist^2 < float_max
			// => dist < sqrt(float_max) / (m_near * m_far)
			PR_ASSERT(PR_DBG, m_near * m_far > 0, "");
			return Sqrt(maths::float_max) / (m_near * m_far);
		}

		// Return the minimum allowed value for 'FocusDist'
		float FocusDistMin() const
		{
			// Clamp so that N - F is non-zero
			// Abs(N - F) == Abs((m_near * dist) - (m_far * dist)) > float_min
			//       == dist * Abs(m_near - m_far) > float_min
			//       == dist > float_min / Abs(m_near - m_far);
			PR_ASSERT(PR_DBG, m_near < m_far, "");
			return maths::float_min / Min(Abs(m_near - m_far), 1.0f);
		}

		// Set the distance to the focus point
		void FocusDist(float dist)
		{
			PR_ASSERT(PR_DBG, pr::IsFinite(dist) && dist >= 0.0f, "'dist' should not be negative");
			m_moved |= dist != m_focus_dist;
			m_base_focus_dist = m_focus_dist = Clamp(dist, FocusDistMin(), FocusDistMax());
		}

		// Modify the camera position based on mouse movement.
		// 'point' should be normalised. i.e. x=[-1, -1], y=[-1,1] with (-1,-1) == (left,bottom). i.e. normal Cartesian axes
		// The start of a mouse movement is indicated by 'btn_state' being non-zero
		// The end of the mouse movement is indicated by 'btn_state' being zero
		// 'ref_point' should be true on the mouse down/up event, false while dragging
		// Returns true if the camera has moved
		bool MouseControl(pr::v2 const& point, ENavOp nav_op, bool ref_point)
		{
			// Navigation operations
			bool translate = int(nav_op & camera::ENavOp::Translate) != 0;
			bool rotate    = int(nav_op & camera::ENavOp::Rotate   ) != 0;
			bool zoom      = int(nav_op & camera::ENavOp::Zoom     ) != 0;

			if (ref_point)
			{
				if (int(nav_op & camera::ENavOp::Translate) != 0) m_Tref = point;
				if (int(nav_op & camera::ENavOp::Rotate   ) != 0) m_Rref = point;
				if (int(nav_op & camera::ENavOp::Zoom     ) != 0) m_Zref = point;
				Commit();
			}
			if (zoom || (translate && rotate))
			{
				if (KeyDown(m_key[camera::ENavKey::TranslateZ]))
				{
					// Move in a fraction of the focus distance
					float delta = zoom ? (point.y - m_Zref.y) : (point.y - m_Tref.y);
					Translate(0, 0, delta * 10.0f, false);
				}
				else
				{
					// Zoom the field of view
					float zm = zoom ? (m_Zref.y - point.y) : (m_Tref.y - point.y);
					Zoom(zm, false);
				}
			}
			if (translate && !rotate)
			{
				float dx = (m_Tref.x - point.x) * m_focus_dist * pr::Tan(m_fovY * 0.5f) * m_aspect;
				float dy = (m_Tref.y - point.y) * m_focus_dist * pr::Tan(m_fovY * 0.5f);
				Translate(dx, dy, 0.0f, false);
			}
			if (rotate && !translate)
			{
				// If in the roll zone
				if (Length2(m_Rref) < 0.80f) Rotate((point.y - m_Rref.y) * float(maths::tau_by_4), (m_Rref.x - point.x) * float(maths::tau_by_4), 0.0f, false);
				else                         Rotate(0.0f, 0.0f, ATan2(m_Rref.y, m_Rref.x) - ATan2(point.y, point.x), false);
			}
			return m_moved;
		}

		// Modify the camera position in the camera Z direction based on mouse wheel.
		// 'point' should be normalised. i.e. x=[-1, -1], y=[-1,1] with (-1,-1) == (left,bottom). i.e. normal Cartesian axes
		// 'delta' is the mouse wheel scroll delta value (i.e. 120 = 1 click = 10% of the focus distance)
		// Returns true if the camera has moved
		bool MouseControlZ(pr::v2 const& point, float delta, bool along_ray, bool commit = true)
		{
			auto dist = delta / 120.0f;
			if (KeyDown(m_key[camera::ENavKey::Accurate])) dist *= 0.1f;
			if (KeyDown(m_key[camera::ENavKey::SuperAccurate])) dist *= 0.1f;

			// Scale by the focus distance
			dist *= m_base_focus_dist * 0.1f;

			// Get the ray in camera space to move the camera along
			auto ray_cs = -v4ZAxis;
			if (along_ray)
			{
				// Move along a ray cast from the camera position to the
				// mouse point projected onto the focus plane.
				auto pt  = NSSPointToWSPoint(v4(point, FocusDist(), 0.0f));
				auto ray_ws = pt - CameraToWorld().pos;
				ray_cs = Normalise3(WorldToCamera() * ray_ws, -v4ZAxis);
			}
			ray_cs *= dist;

			// If the 'TranslateZ' key is down move the focus point too.
			// Otherwise move the camera toward or away from the focus point.
			if (!KeyDown(m_key[camera::ENavKey::TranslateZ]))
				m_focus_dist = Clamp(m_base_focus_dist + ray_cs.z, FocusDistMin(), FocusDistMax());

			// Translate
			auto pos = m_base_c2w.pos + m_base_c2w * ray_cs;
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
				m_focus_dist = Clamp(m_base_focus_dist + dz, FocusDistMin(), FocusDistMax());

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
			m_c2w = m_base_c2w * m4x4::Transform(pitch, yaw, roll, v4Origin);

			// Position the camera so that the focus is still in the same position
			m_c2w.pos = old_focus + m_c2w.z * m_focus_dist;

			// If an align axis is given, align up to it
			if (Length3Sq(m_align) > maths::tiny)
			{
				auto up = pr::Perpendicular(m_c2w.pos - old_focus, m_align);
				m_c2w = m4x4::LookAt(m_c2w.pos, old_focus, up);
			}

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
			m_fovY = pr::Clamp(m_fovY, pr::maths::tiny, float(pr::maths::tau_by_2 - pr::maths::tiny));

			// Set the base values
			if (commit) Commit();

			m_moved = true;
			return m_moved;
		}

		// Return the current zoom scaling factor
		float Zoom() const
		{
			return m_default_fovY / m_fovY;
		}

		// Reset the FOV to the default
		void ResetZoom()
		{
			m_moved = true;
			m_base_fovY = m_fovY = m_default_fovY;
		}

		// Set the current position, FOV, and focus distance as the position reference
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

		// Set the axis to align the camera up axis to
		void SetAlign(pr::v4 const& up)
		{
			m_align = up;
			if (Length3Sq(m_align) > maths::tiny)
			{
				if (Parallel(m_c2w.z, m_align)) m_c2w = m4x4(m_c2w.y, m_c2w.z, m_c2w.x, m_c2w.w);
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
			m_focus_dist = Clamp(Length3(lookat - position), FocusDistMin(), FocusDistMax());

			// Set the base values
			if (commit) Commit();
		}

		// Position the camera so that all of 'bbox' is visible to the camera when looking 'forward' and 'up'
		void View(pr::BBox const& bbox, pr::v4 const& forward, pr::v4 const& up, float focus_dist = 0, bool preserve_aspect = true, bool update_base = true)
		{
			if (bbox.empty()) return;

			// This code projects 'bbox' onto a plane perpendicular to 'forward' and
			// at the nearest point of the bbox to the camera. It then ensures a circle
			// with radius of the projected 2D bbox fits within the view.
			auto bbox_centre = bbox.Centre();
			auto bbox_radius = bbox.Radius();

			// Get the distance from the centre of the bbox to the point nearest the camera
			auto sizez = pr::maths::float_max;
			sizez = pr::Min(sizez, pr::Abs(pr::Dot3(forward, pr::v4( bbox_radius.x,  bbox_radius.y,  bbox_radius.z, 0.0f))));
			sizez = pr::Min(sizez, pr::Abs(pr::Dot3(forward, pr::v4(-bbox_radius.x,  bbox_radius.y,  bbox_radius.z, 0.0f))));
			sizez = pr::Min(sizez, pr::Abs(pr::Dot3(forward, pr::v4( bbox_radius.x, -bbox_radius.y,  bbox_radius.z, 0.0f))));
			sizez = pr::Min(sizez, pr::Abs(pr::Dot3(forward, pr::v4( bbox_radius.x,  bbox_radius.y, -bbox_radius.z, 0.0f))));

			// 'focus_dist' is the focus distance (chosen, or specified) from the centre of the bbox
			// to the camera. Since 'size' is the size to fit at the nearest point of the bbox,
			// the focus distance needs to be 'dist' + 'sizez'.

			// If not preserving the aspect ratio, determine the width
			// and height of the bbox as viewed from the camera.
			if (!preserve_aspect)
			{
				// Get the camera orientation matrix
				m3x4 c2w(pr::Cross3(up, forward), up, forward);
				auto w2c = pr::InvertFast(c2w);

				auto bbox_cs = w2c * bbox;
				auto width   = bbox_cs.SizeX();
				auto height  = bbox_cs.SizeY();

				// Choose the fields of view. If 'focus_dist' is given, then that determines
				// the X,Y field of view. If not, choose a focus distance based on a view size
				// equal to the average of 'width' and 'height' using the default FOV.
				if (focus_dist == 0)
				{
					auto size = (width + height) / 2.0f;
					focus_dist = (0.5f * size) / pr::Tan(0.5f * m_default_fovY);
				}

				// Set the aspect ratio
				auto aspect = width / height;
				auto d = focus_dist - sizez;

				// Set the aspect and FOV based on the view of the bbox
				Aspect(aspect);
				FovY(2.0f * pr::ATan(0.5f * height / d));
			}
			else
			{
				// 'size' is the *radius* (i.e. not the full height) of the bounding box projected onto the 'forward' plane.
				auto size = pr::Sqrt(pr::Clamp(pr::Length3Sq(bbox_radius) - pr::Sqr(sizez), 0.0f, pr::maths::float_max));

				// Choose the focus distance if not given
				if (focus_dist == 0 || focus_dist < sizez)
				{
					auto d = size / (pr::Tan(0.5f * FovY()) * m_aspect);
					focus_dist = sizez + d;
				}
				// Otherwise, set the FOV
				else
				{
					auto d = focus_dist - sizez;
					FovY(2.0f * pr::ATan(size * m_aspect / d));
				}
			}

			// the distance from camera to bbox_centre is 'dist + sizez'
			LookAt(bbox_centre - forward * focus_dist, bbox_centre, up, update_base);
		}

		// Set the camera fields of view so that a rectangle with dimensions 'width'/'height' exactly fits the view at 'dist'.
		void View(float width, float height, float focus_dist = 0)
		{
			PR_ASSERT(PR_DBG, width > 0 && height > 0 && focus_dist >= 0, "");

			// This works for orthographic mode as well so long as we set FOV
			Aspect(width / height);

			// If 'focus_dist' is given, choose FOV so that the view exactly fits
			if (focus_dist != 0)
			{
				FovY(2.0f * pr::ATan(0.5f * height / focus_dist));
				FocusDist(focus_dist);
			}
			// Otherwise, choose a focus distance that preserves FOV
			else
			{
				FocusDist(0.5f * height / pr::Tan(0.5f * FovY()));
			}
		}

		// Orbit the camera about the focus point by 'angle_rad' radians
		void Orbit(float angle_rad, bool commit = true)
		{
			// Record the focus point
			pr::v4 old_focus = FocusPoint();

			// Find the axis of rotation
			pr::v4 axis = IsAligned() ? pr::InvertFast(m_c2w) * m_align : m_c2w.y;

			// Rotate the camera transform and reposition to look at the focus point
			m_c2w     = m_c2w * m4x4::Transform(axis, angle_rad, v4Origin);
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
