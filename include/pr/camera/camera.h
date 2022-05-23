//******************************************
// Camera
//  Copyright (c) Rylogic Ltd 2009
//******************************************
// Camera to world matrix plus FoV and focus point
// Supports 3D trackball-like mouse control and basic keyboard control
#pragma once
#include <memory>
#include "pr/maths/maths.h"
#include "pr/maths/bit_fields.h"
#include "pr/common/assert.h"
#include "pr/common/keystate.h"
#include "pr/common/flags_enum.h"
#include "pr/common/value_ptr.h"

namespace pr::camera
{
	// Navigation verbs
	enum class ENavOp
	{
		None      = 0,
		Translate = 1 << 0,
		Rotate    = 1 << 1,
		Zoom      = 1 << 2,
		_flags_enum,
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
			ArrowKeys();
		}
		void ArrowKeys()
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
		void WASDKeys()
		{
			m_bindings[int(ENavKey::Left)] = 'A';
			m_bindings[int(ENavKey::Up)] = 'W';
			m_bindings[int(ENavKey::Right)] = 'D';
			m_bindings[int(ENavKey::Down)] = 'S';
			m_bindings[int(ENavKey::In)] = 'Q';
			m_bindings[int(ENavKey::Out)] = 'E';
			m_bindings[int(ENavKey::Rotate)] = VK_SHIFT;
			m_bindings[int(ENavKey::TranslateZ)] = VK_CONTROL;
			m_bindings[int(ENavKey::Accurate)] = VK_SHIFT;
			m_bindings[int(ENavKey::SuperAccurate)] = VK_CONTROL;
			m_bindings[int(ENavKey::PerpendicularZ)] = VK_MENU;
		}
	};

	// Prevent translation/rotation on particular axes
	enum class ELockMask
	{
		None           = 0,
		TransX         = 1 << 0,
		TransY         = 1 << 1,
		TransZ         = 1 << 2,
		RotX           = 1 << 3,
		RotY           = 1 << 4,
		RotZ           = 1 << 5,
		Zoom           = 1 << 6,
		All            = (1 << 7) - 1, // Not including camera relative
		CameraRelative = 1 << 7,
		Translation    = TransX | TransY | TransZ,
		Rotation       = RotX | RotY | RotZ,
		_flags_enum,
	};

	// Convert an MK_ value into the default navigation operation
	inline ENavOp MouseBtnToNavOp(int mk)
	{
		auto op = ENavOp::None;
		if (AllSet(mk, MK_LBUTTON)) op |= ENavOp::Rotate;
		if (AllSet(mk, MK_RBUTTON)) op |= ENavOp::Translate;
		if (AllSet(mk, MK_MBUTTON)) op |= ENavOp::Zoom;
		return op;
	}
}
namespace pr
{
	// Camera matrix with 3D trackball-like control
	// Note:
	// All points are in normalised screen space regardless of aspect ratio,
	//  i.e. x=[-1, -1], y=[-1,1] with (-1,-1) = (left,bottom), i.e. normal Cartesian axes.
	// Use:
	//  point = v2(2.0f * pt.x / float(Width) - 1.0f, 1.0f - 2.0f * pt.y / float(Height));
	struct Camera
	{
		// Note:
		// The camera does not contain any info about the size of the screen
		// that the camera view is on. Therefore, there are no screen space to normalised
		// screen space methods in here. You need the Window for that.

		struct NavState
		{
			// Notes:
			//  - This is not a pointer within 'Camera' because that breaks the value semantics
			//    of the Camera class. I've experimented with 'value_ptr<>' but using the destructor
			//    to revert the transaction causes problems when copies exist.
			//  - An option is to have the caller provide an instance of this struct in the MouseControl
			//    methods, but that creates a problem for interop.
			//  - The simplest option is to just make this a member of the Camera object.

			m4x4   m_c2w0;        // The starting position during a mouse movement
			double m_fovY0;       // The starting FOV during a mouse movement
			double m_focus_dist0; // The starting focus distance during a mouse movement
			v2     m_Tref;        // Movement start reference point for translation
			v2     m_Rref;        // Movement start reference point for rotation
			v2     m_Zref;        // Movement start reference point for zoom

			NavState()
				: m_c2w0()
				, m_fovY0()
				, m_focus_dist0()
				, m_Tref()
				, m_Rref()
				, m_Zref()
			{}

			// Save the current camera state as the initial state
			void Commit(Camera const& cam)
			{
				m_c2w0        = cam.m_c2w;
				m_fovY0       = cam.m_fovY;
				m_focus_dist0 = cam.m_focus_dist;
			}

			// Roll back to the initial values.
			void Revert(Camera& cam)
			{
				cam.m_c2w        = m_c2w0;
				cam.m_fovY       = m_fovY0;
				cam.m_focus_dist = m_focus_dist0;
				cam.m_moved      = true;
			}
		};

		using NavKeyBindings = camera::NavKeyBindings;
		using ELockMask      = camera::ELockMask;
		using ENavOp         = camera::ENavOp;
		using ENavKey        = camera::ENavKey;

		m4x4           m_c2w;               // Camera to world transform
		NavState       m_nav;               // Navigation initial state data
		v4             m_align;             // The direction to align 'up' to, or v4Zero
		double         m_default_fovY;      // The default field of view
		double         m_fovY;              // Field of view in the Y direction
		double         m_focus_dist;        // Distance from the c2w position to the focus, down the z axis
		double         m_aspect;            // Aspect ratio = width/height
		double         m_near;              // The near plane as a multiple of the focus distance
		double         m_far;               // The near plane as a multiple of the focus distance
		double         m_accuracy_scale;    // Scale factor for high accuracy control
		int            m_accuracy_mode;     // The last accuracy mode: 0 = normal, 1 = accurate, 2 = super accurate
		ELockMask      m_lock_mask;         // Locks on the allowed motion
		NavKeyBindings m_key;               // Key bindings
		bool           m_orthographic;      // True for orthographic camera to screen transforms, false for perspective
		bool           m_moved;             // Dirty flag for when the camera moves

		Camera()
			:Camera(m4x4::Identity())
		{}
		Camera(m4x4 const& c2w, double fovY = maths::tau_by_8, double aspect = 1.0, double focus_dist = 1.0)
			:Camera(c2w, fovY, aspect, focus_dist, false, 0.01, 100.0)
		{}
		Camera(v4_cref<> eye, v4_cref<> pt, v4_cref<> up, double fovY = maths::tau_by_8, double aspect = 1.0)
			:Camera(m4x4::Identity(), fovY, aspect)
		{
			LookAt(eye, pt, up, true);
		}
		Camera(m4x4 const& c2w, double fovY, double aspect, double focus_dist, bool orthographic, double near_, double far_)
			:m_c2w(c2w)
			,m_nav()
			,m_align(v4Zero)
			,m_default_fovY(fovY)
			,m_fovY(m_default_fovY)
			,m_focus_dist(focus_dist)
			,m_aspect(aspect)
			,m_near(near_)
			,m_far(far_)
			,m_accuracy_scale(0.1)
			,m_accuracy_mode()
			,m_lock_mask()
			,m_key()
			,m_orthographic(orthographic)
			,m_moved(false)
		{
			PR_ASSERT(PR_DBG, IsFinite(m_c2w), "invalid scene view parameters");
			PR_ASSERT(PR_DBG, IsFinite(m_fovY), "invalid scene view parameters");
			PR_ASSERT(PR_DBG, IsFinite(m_aspect), "invalid scene view parameters");
			PR_ASSERT(PR_DBG, IsFinite(m_focus_dist), "invalid scene view parameters");
		}

		// Return the camera to world transform
		void CameraToWorld(m4x4 const& c2w, bool commit = true)
		{
			m_c2w = c2w;
			if (commit) Commit();
		}
		m4x4 CameraToWorld() const
		{
			return m_c2w;
		}
		m4x4 WorldToCamera() const
		{
			return InvertFast(m_c2w);
		}

		// Return a projection transform
		m4x4 CameraToScreen(double near_clip, double far_clip, double aspect, double fovY, double focus_dist) const
		{
			auto height = 2.0 * focus_dist * std::tan(fovY * 0.5);
			return m_orthographic
				? m4x4::ProjectionOrthographic(s_cast<float>(height*aspect), s_cast<float>(height), s_cast<float>(near_clip), s_cast<float>(far_clip), true)
				: m4x4::ProjectionPerspectiveFOV(s_cast<float>(fovY), s_cast<float>(aspect), s_cast<float>(near_clip), s_cast<float>(far_clip), true);
		}
		m4x4 CameraToScreen(double near_clip, double far_clip) const
		{
			return CameraToScreen(near_clip, far_clip, m_aspect, m_fovY, m_focus_dist);
		}
		m4x4 CameraToScreen(double aspect, double fovY, double focus_dist) const
		{
			return CameraToScreen(Near(false), Far(false), aspect, fovY, focus_dist);
		}
		m4x4 CameraToScreen() const
		{
			return CameraToScreen(Near(false), Far(false));
		}

		// Return a point in world space corresponding to a normalised screen space point.
		// The x,y components of 'nss_point' should be in normalised screen space, i.e. (-1,-1)->(1,1) (lower left -> upper right).
		// The z component should be the depth into the screen (i.e. d*-c2w.z, where 'd' is typically positive).
		v4 NSSPointToWSPoint(v4 const& nss_point) const
		{
			auto half_height = m_focus_dist * std::tan(m_fovY * 0.5);

			// Calculate the point in camera space
			v4 point;
			point.x = s_cast<float>(nss_point.x * m_aspect * half_height);
			point.y = s_cast<float>(nss_point.y * half_height);
			if (!m_orthographic)
			{
				auto sz = s_cast<float>(nss_point.z / m_focus_dist);
				point.x *= sz;
				point.y *= sz;
			}
			point.z = -nss_point.z;
			point.w = 1.0f;
			return m_c2w * point; // camera space to world space
		}

		// Return a point in normalised screen space corresponding to 'ws_point'
		// The returned 'z' component will be the depth into the screen (i.e. d*-c2w.z, where 'd' is typically positive).
		v4 WSPointToNSSPoint(v4 const& ws_point) const
		{
			auto half_height = m_focus_dist * std::tan(m_fovY * 0.5);

			// Get the point in camera space and project into normalised screen space
			v4 cam = InvertFast(m_c2w) * ws_point;

			v4 point;
			point.x = s_cast<float>(cam.x / (m_aspect * half_height));
			point.y = s_cast<float>(cam.y / (half_height));
			if (!m_orthographic)
			{
				auto sz = s_cast<float>(-m_focus_dist / cam.z);
				point.x *= sz;
				point.y *= sz;
			}
			point.z = -cam.z;
			point.w = 1.0f;
			return point;
		}

		// Return a ray from the camera that passes through 'nss_point' (a normalised screen space point).
		// The x,y components of 'nss_point' should be in normalised screen space, i.e. (-1,-1)->(1,1) (lower left -> upper right).
		// The z component should be the depth into the screen (i.e. d*-c2w.z, where 'd' is typically positive).
		void NSSPointToWSRay(v4 const& nss_point, v4& ws_point, v4& ws_direction) const
		{
			auto pt = NSSPointToWSPoint(nss_point);
			if (Orthographic())
			{
				auto hheight = m_focus_dist * std::tan(m_fovY * 0.5);
				auto hwidth = m_aspect * hheight;
				ws_point = m_c2w.pos + (s_cast<float>(nss_point.x * hwidth) * m_c2w.x) + (s_cast<float>(nss_point.y * hheight) * m_c2w.y);
				ws_direction = -m_c2w.z;
			}
			else
			{
				ws_point = m_c2w.pos;
				ws_direction = Normalise(pt - ws_point);
			}
		}

		// Get/Set the distances to the near and far clip planes
		v2 ClipPlanes(bool focus_relative)
		{
			return v2(s_cast<float>(Near(focus_relative)), s_cast<float>(Far(focus_relative)));
		}
		void ClipPlanes(double near_, double far_, bool focus_relative)
		{
			Near(near_, focus_relative);
			Far(far_, focus_relative);
		}

		// Get/Set the near clip plane
		double Near(bool focus_relative) const
		{
			return (focus_relative ? 1 : m_focus_dist) * m_near;
		}
		void Near(double value, bool focus_relative)
		{
			m_near = value / (focus_relative ? 1.0 : m_focus_dist);
		}

		// Get/Set the far clip plane (in world space)
		double Far(bool focus_relative) const
		{
			return (focus_relative ? 1 : m_focus_dist) * m_far;
		}
		void Far(double value, bool focus_relative)
		{
			m_far = value / (focus_relative ? 1.0 : m_focus_dist);
		}

		// Get/Set the aspect ratio
		double Aspect() const
		{
			return m_aspect;
		}
		void Aspect(double aspect_w_by_h)
		{
			if (aspect_w_by_h <= 0 || !IsFinite(aspect_w_by_h))
				throw std::runtime_error("Aspect ratio value is invalid");

			m_moved |= aspect_w_by_h != m_aspect;
			m_aspect = aspect_w_by_h;
		}

		// Get/Set the horizontal field of view (in radians).
		double FovX() const
		{
			auto fovX = 2.0 * std::atan(std::tan(m_fovY * 0.5) * m_aspect);
			if (fovX <= 0.0 || fovX >= maths::tau_by_2 || !IsFinite(fovX))
				throw std::runtime_error("FovX must be > 0 and < tau/2");
			return fovX;
		}
		void FovX(double fovX)
		{
			if (fovX <= 0.0 || fovX >= maths::tau_by_2 || !IsFinite(fovX))
				throw std::runtime_error("FovX must be > 0 and < tau/2");
			FovY(2.0 * std::atan(std::tan(fovX * 0.5) / m_aspect));
		}

		// Get/Set the vertical field of view (in radians). FOV relationship: tan(fovY/2) * aspect_w_by_h = tan(fovX/2);
		double FovY() const
		{
			return m_fovY;
		}
		void FovY(double fovY)
		{
			if (fovY <= 0.0 || fovY >= maths::tau_by_2 || !IsFinite(fovY))
				throw std::runtime_error("FovY value is invalid");
			
			fovY = Clamp(fovY, maths::tinyd, maths::tau_by_2);
			m_moved |= fovY != m_fovY;
			m_fovY = fovY;
		}

		// Set both X and Y axis field of view. Implies aspect ratio
		void Fov(double fovX, double fovY)
		{
			if (fovX <= 0.0 || fovX >= maths::tau_by_2 || !IsFinite(fovX))
				throw std::runtime_error("FovX value is invalid");
			if (fovY <= 0.0 || fovY >= maths::tau_by_2 || !IsFinite(fovY))
				throw std::runtime_error("FovY value is invalid");

			fovX = Clamp(fovX, maths::tinyd, maths::tau_by_2);
			fovY = Clamp(fovY, maths::tinyd, maths::tau_by_2);
			auto aspect = std::tan(fovX/2) / std::tan(fovY/2);
			Aspect(aspect);
			FovY(fovY);
		}

		// Adjust the FocusDist, FovX, and FovY so that the average FOV equals 'fov'
		void BalanceFov(double fov)
		{
			if (fov <= 0.0 || fov >= maths::tau_by_2 || !IsFinite(fov))
				throw std::runtime_error("FOV value is invalid");
			
			// Measure the current focus distance and view size at that distance
			auto d = FocusDist();
			auto pt = FocusPoint();
			auto wh = ViewArea(d);
			auto size = (wh.x + wh.y) * 0.5;

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
			auto d2 = (0.5 * size) / std::tan(0.5 * fov);
			auto fovY = 2.0 * std::atan((0.5 * wh.y) / d2);

			FovY(fovY);
			FocusDist(d2);
			FocusPoint(pt);
		}

		// Get/Set the axis to align the camera up direction to
		v4 Align() const
		{
			return m_align;
		}
		void Align(v4 const& up)
		{
			m_align = up;
			if (LengthSq(m_align) > maths::tinyf)
			{
				if (Parallel(m_c2w.z, m_align)) m_c2w = m4x4(m_c2w.y, m_c2w.z, m_c2w.x, m_c2w.w);
				m_c2w = m4x4::LookAt(m_c2w.pos, FocusPoint(), m_align);
				m_moved = true;
			}
		}

		// Return true if the align axis has been set for the camera
		bool IsAligned() const
		{
			return LengthSq(m_align) > maths::tinyf;
		}

		// Get/Set orthographic projection mode
		bool Orthographic() const
		{
			return m_orthographic;
		}
		void Orthographic(bool value)
		{
			m_orthographic = value;
			m_moved = true;
		}

		// Return the size of the perpendicular area visible to the camera at 'dist' (in world space)
		v2 ViewArea(double dist) const
		{
			dist = m_orthographic ? m_focus_dist : dist;
			auto h = 2.0 * std::tan(m_fovY * 0.5);
			return v2(s_cast<float>(dist * h * m_aspect), s_cast<float>(dist * h));
		}

		// Return the view frustum for this camera.
		Frustum ViewFrustum(double zfar) const
		{
			// Note: the frustum is stored with the apex (i.e. camera position) on the +Z axis at 'zfar'
			// and the far plane at (0,0,0). However, the 'Intersect_LineToFrustum' function allows for this
			// meaning clipping can be done in camera space assuming the frustum apex is at (0,0,0)
			return Orthographic()
				? Frustum::MakeOrtho(ViewArea(m_focus_dist))
				: Frustum::MakeFA(s_cast<float>(m_fovY), s_cast<float>(m_aspect), s_cast<float>(zfar));
		}
		Frustum ViewFrustum() const
		{
			return ViewFrustum(Far(false));
		}

		// Get/Set the world space position of the focus point, maintaining the current camera orientation
		v4 FocusPoint() const
		{
			return m_c2w.pos - s_cast<float>(m_focus_dist) * m_c2w.z;
		}
		void FocusPoint(v4 const& position)
		{
			m_c2w.pos += position - FocusPoint();
			m_moved = true;
		}

		// Get/Set the distance to the focus point
		double FocusDist() const
		{
			return m_focus_dist;
		}
		void FocusDist(double dist)
		{
			assert("'dist' should not be negative" && IsFinite(dist) && dist >= 0.0);
			dist = Clamp(dist, FocusDistMin(), FocusDistMax());
			m_moved |= dist != m_focus_dist;
			m_focus_dist = dist;
		}

		// Return the maximum allowed distance for 'FocusDist'
		double FocusDistMax() const
		{
			// Clamp so that Near*Far is finite
			// N*F == (m_near * dist) * (m_far * dist) < float_max
			//     == m_near * m_far * dist^2 < float_max
			// => dist < sqrt(float_max) / (m_near * m_far)
			assert(m_near * m_far > 0);
			double const sqrt_real_max = 1.84467435239537E+19;
			return sqrt_real_max / (m_near * m_far);
		}

		// Return the minimum allowed value for 'FocusDist'
		double FocusDistMin() const
		{
			// Clamp so that N - F is non-zero
			// Abs(N - F) == Abs((m_near * dist) - (m_far * dist)) > float_min
			//       == dist * Abs(m_near - m_far) > float_min
			//       == dist > float_min / Abs(m_near - m_far);
			assert(m_near < m_far);
			double const real_min = limits<double>::min();
			return real_min / Min(Abs(m_near - m_far), 1.0);
		}

		// Modify the camera position based on mouse movement.
		// 'point' should be normalised. i.e. x=[-1,+1], y=[-1,+1] with (-1,-1) == (left,bottom). i.e. normal Cartesian axes
		// 'ref_point' should be true on the mouse down/up event, false while dragging
		// Returns true if the camera has moved
		bool MouseControl(v2 const& point, ENavOp nav_op, bool ref_point)
		{
			// Navigation operations
			bool translate = AllSet(nav_op, ENavOp::Translate);
			bool rotate    = AllSet(nav_op, ENavOp::Rotate   );
			bool zoom      = AllSet(nav_op, ENavOp::Zoom     );
			auto acc_mode  = KeyDown(m_key[ENavKey::Accurate]) + KeyDown(m_key[ENavKey::SuperAccurate]);

			// On mouse down, mouse up, or a change in accuracy mode, record the reference point
			if (ref_point || acc_mode != m_accuracy_mode)
			{
				if (AllSet(nav_op, ENavOp::Translate)) m_nav.m_Tref = point;
				if (AllSet(nav_op, ENavOp::Rotate)) m_nav.m_Rref = point;
				if (AllSet(nav_op, ENavOp::Zoom)) m_nav.m_Zref = point;
				m_accuracy_mode = acc_mode;
				Commit();
			}

			if (zoom || (translate && rotate))
			{
				if (KeyDown(m_key[ENavKey::TranslateZ]))
				{
					// Move in a fraction of the focus distance
					auto delta = zoom ? (point.y - m_nav.m_Zref.y) : (point.y - m_nav.m_Tref.y);
					Translate(0, 0, delta * 10.0, false);
				}
				else
				{
					// Zoom the field of view
					auto zm = zoom ? (m_nav.m_Zref.y - point.y) : (m_nav.m_Tref.y - point.y);
					Zoom(zm, false);
				}
			}
			if (translate && !rotate)
			{
				auto dx = (m_nav.m_Tref.x - point.x) * m_focus_dist * std::tan(m_fovY * 0.5) * m_aspect;
				auto dy = (m_nav.m_Tref.y - point.y) * m_focus_dist * std::tan(m_fovY * 0.5);
				Translate(dx, dy, 0.0, false);
			}
			if (rotate && !translate)
			{
				// If in the roll zone. 'm_Rref' is a point in normalised space[-1, +1] x [-1, +1].
				// So the roll zone is a radial distance from the centre of the screen
				if (Length(m_nav.m_Rref) < 0.80f)
					Rotate((point.y - m_nav.m_Rref.y) * maths::tau_by_4f, (m_nav.m_Rref.x - point.x) * maths::tau_by_4f, 0.0f, false);
				else
					Rotate(0.0f, 0.0f, ATan2(m_nav.m_Rref.y, m_nav.m_Rref.x) - ATan2(point.y, point.x), false);
			}
			return m_moved;
		}

		// Modify the camera position in the camera Z direction based on mouse wheel.
		// 'point' should be normalised. i.e. x=[-1, -1], y=[-1,1] with (-1,-1) == (left,bottom). i.e. normal Cartesian axes
		// 'delta' is the mouse wheel scroll delta value (i.e. 120 = 1 click = 10% of the focus distance)
		// Returns true if the camera has moved
		bool MouseControlZ(v2 const& point, double delta, bool along_ray, bool commit = true)
		{
			// Ignore if Z motion is locked
			if (AllSet(m_lock_mask, ELockMask::TransZ))
				return false;

			auto dist = delta / 120.0;
			if (KeyDown(m_key[ENavKey::Accurate])) dist *= 0.1;
			if (KeyDown(m_key[ENavKey::SuperAccurate])) dist *= 0.1;

			// Scale by the focus distance
			dist *= m_nav.m_focus_dist0 * 0.1;

			// Get the ray in camera space to move the camera along
			auto ray_cs = -v4::ZAxis();
			if (along_ray)
			{
				// Move along a ray cast from the camera position to the
				// mouse point projected onto the focus plane.
				auto pt  = NSSPointToWSPoint(v4(point, s_cast<float>(FocusDist()), 0.0f));
				auto ray_ws = pt - CameraToWorld().pos;
				ray_cs = Normalise(WorldToCamera() * ray_ws, -v4::ZAxis());
			}
			ray_cs *= s_cast<float>(dist);

			// If the 'TranslateZ' key is down move the focus point too.
			// Otherwise move the camera toward or away from the focus point.
			if (!KeyDown(m_key[ENavKey::TranslateZ]))
				m_focus_dist = Clamp(m_nav.m_focus_dist0 + ray_cs.z, FocusDistMin(), FocusDistMax());

			// Translate
			auto pos = m_nav.m_c2w0.pos + m_nav.m_c2w0 * ray_cs;

			// Apply non-camera relative locking
			if (m_lock_mask != ELockMask::None && !AllSet(m_lock_mask, ELockMask::CameraRelative))
			{
				if (AllSet(m_lock_mask, ELockMask::TransX)) pos.x = m_nav.m_c2w0.pos.x;
				if (AllSet(m_lock_mask, ELockMask::TransY)) pos.y = m_nav.m_c2w0.pos.y;
				if (AllSet(m_lock_mask, ELockMask::TransZ)) pos.z = m_nav.m_c2w0.pos.z;
			}

			// Update the camera position
			if (IsFinite(pos))
				m_c2w.pos = pos;

			// Set the base values
			if (commit)
				Commit();

			m_moved = true;
			return m_moved;
		}

		// Translate by a camera relative amount
		// Returns true if the camera has moved (for consistency with MouseControl)
		bool Translate(double dx, double dy, double dz, bool commit = true)
		{
			if (m_lock_mask != ELockMask::None && AllSet(m_lock_mask, ELockMask::CameraRelative))
			{
				if (AllSet(m_lock_mask, ELockMask::TransX)) dx = 0.0f;
				if (AllSet(m_lock_mask, ELockMask::TransY)) dy = 0.0f;
				if (AllSet(m_lock_mask, ELockMask::TransZ)) dz = 0.0f;
			}
			if (KeyDown(m_key[ENavKey::Accurate]))
			{
				dx *= m_accuracy_scale;
				dy *= m_accuracy_scale;
				dz *= m_accuracy_scale;
				if (KeyDown(m_key[ENavKey::SuperAccurate]))
				{
					dx *= m_accuracy_scale;
					dy *= m_accuracy_scale;
					dz *= m_accuracy_scale;
				}
			}

			// Move in a fraction of the focus distance
			dz = -m_nav.m_focus_dist0 * dz * 0.1;
			if (!KeyDown(m_key[ENavKey::TranslateZ]))
				m_focus_dist = Clamp(m_nav.m_focus_dist0 + dz, FocusDistMin(), FocusDistMax());

			// Translate
			auto pos = m_nav.m_c2w0.pos + m_nav.m_c2w0.rot * v4(s_cast<float>(dx), s_cast<float>(dy), s_cast<float>(dz), 0.0f);

			// Apply non-camera relative locking
			if (m_lock_mask != ELockMask::None && !AllSet(m_lock_mask, ELockMask::CameraRelative))
			{
				if (AllSet(m_lock_mask, ELockMask::TransX)) pos.x = m_nav.m_c2w0.pos.x;
				if (AllSet(m_lock_mask, ELockMask::TransY)) pos.y = m_nav.m_c2w0.pos.y;
				if (AllSet(m_lock_mask, ELockMask::TransZ)) pos.z = m_nav.m_c2w0.pos.z;
			}

			// Update the camera position
			if (IsFinite(pos))
				m_c2w.pos = pos;

			// Set the base values
			if (commit)
				Commit();

			m_moved = true;
			return m_moved;
		}

		// Rotate the camera by Euler angles about the focus point
		// Returns true if the camera has moved (for consistency with MouseControl)
		bool Rotate(double pitch, double yaw, double roll, bool commit = true)
		{
			if (m_lock_mask != ELockMask::None)
			{
				if (AllSet(m_lock_mask, ELockMask::RotX)) pitch	= 0.0;
				if (AllSet(m_lock_mask, ELockMask::RotY)) yaw	= 0.0;
				if (AllSet(m_lock_mask, ELockMask::RotZ)) roll	= 0.0;
			}
			if (KeyDown(m_key[ENavKey::Accurate]))
			{
				pitch *= m_accuracy_scale;
				yaw   *= m_accuracy_scale;
				roll  *= m_accuracy_scale;
				if (KeyDown(m_key[ENavKey::SuperAccurate]))
				{
					pitch *= m_accuracy_scale;
					yaw   *= m_accuracy_scale;
					roll  *= m_accuracy_scale;
				}
			}

			// Save the world space position of the focus point
			v4 old_focus = FocusPoint();

			// Rotate the camera matrix
			m_c2w = m_nav.m_c2w0 * m4x4::Transform(s_cast<float>(pitch), s_cast<float>(yaw), s_cast<float>(roll), v4::Origin());

			// Position the camera so that the focus is still in the same position
			m_c2w.pos = old_focus + s_cast<float>(m_focus_dist) * m_c2w.z;

			// If an align axis is given, align up to it
			if (LengthSq(m_align) > maths::tinyf)
			{
				auto up = Perpendicular(m_c2w.pos - old_focus, m_align);
				m_c2w = m4x4::LookAt(m_c2w.pos, old_focus, up);
			}

			// Set the base values
			if (commit)
				Commit();

			m_moved = true;
			return m_moved;
		}

		// Zoom the field of view. 'zoom' should be in the range (-1, 1) where negative numbers zoom in, positive out
		// Returns true if the camera has moved (for consistency with MouseControl)
		bool Zoom(double zoom, bool commit = true)
		{
			if (m_lock_mask != ELockMask::None)
			{
				if (AllSet(m_lock_mask, ELockMask::Zoom)) return false;
			}
			if (KeyDown(m_key[ENavKey::Accurate]))
			{
				zoom *= m_accuracy_scale;
				if (KeyDown(m_key[ENavKey::SuperAccurate]))
					zoom *= m_accuracy_scale;
			}

			m_fovY = (1.0 + zoom) * m_nav.m_fovY0;
			m_fovY = Clamp(m_fovY, maths::tinyd, maths::tau_by_2 - maths::tinyd);

			// Set the base values
			if (commit)
				Commit();

			m_moved = true;
			return m_moved;
		}

		// Return the current zoom scaling factor
		double Zoom() const
		{
			return m_default_fovY / m_fovY;
		}

		// Reset the FOV to the default
		void ResetZoom()
		{
			m_moved = true;
			m_fovY = m_default_fovY;
		}

		// Set the current position, FOV, and focus distance as the position reference
		void Commit()
		{
			m_c2w = Orthonorm(m_c2w);
			m_nav.Commit(*this);
		}

		// Revert navigation back to the last commit
		void Revert()
		{
			m_nav.Revert(*this);
		}

		// Position the camera at 'position' looking at 'lookat' with up pointing 'up'
		void LookAt(v4 const& position, v4 const& lookat, v4 const& up, bool commit = true)
		{
			m_c2w = m4x4::LookAt(position, lookat, up);
			m_focus_dist = Clamp<double>(Length(lookat - position), FocusDistMin(), FocusDistMax());

			// Set the base values
			if (commit) Commit();
		}

		// Position the camera so that all of 'bbox' is visible to the camera when looking 'forward' and 'up'
		void View(BBox const& bbox, v4 const& forward, v4 const& up, double focus_dist = 0, bool preserve_aspect = true, bool update_base = true)
		{
			if (!bbox.valid())
				throw std::runtime_error("Camera: Cannot view an invalid bounding box");
			if (bbox.is_point())
				return;

			// This code projects 'bbox' onto a plane perpendicular to 'forward' and
			// at the nearest point of the bbox to the camera. It then ensures a circle
			// with radius of the projected 2D bbox fits within the view.
			auto bbox_centre = bbox.Centre();
			auto bbox_radius = bbox.Radius();

			// Get the distance from the centre of the bbox to the point nearest the camera
			auto sizez = maths::float_max;
			sizez = Min(sizez, Abs(Dot3(forward, v4( bbox_radius.x,  bbox_radius.y,  bbox_radius.z, 0.0f))));
			sizez = Min(sizez, Abs(Dot3(forward, v4(-bbox_radius.x,  bbox_radius.y,  bbox_radius.z, 0.0f))));
			sizez = Min(sizez, Abs(Dot3(forward, v4( bbox_radius.x, -bbox_radius.y,  bbox_radius.z, 0.0f))));
			sizez = Min(sizez, Abs(Dot3(forward, v4( bbox_radius.x,  bbox_radius.y, -bbox_radius.z, 0.0f))));

			// 'focus_dist' is the focus distance (chosen, or specified) from the centre of the bbox
			// to the camera. Since 'size' is the size to fit at the nearest point of the bbox,
			// the focus distance needs to be 'dist' + 'sizez'.

			// If not preserving the aspect ratio, determine the width
			// and height of the bbox as viewed from the camera.
			if (!preserve_aspect)
			{
				// Get the camera orientation matrix
				m3x4 c2w(Cross3(up, forward), up, forward);
				auto w2c = InvertFast(c2w);

				auto bbox_cs = w2c * bbox;
				auto width  = bbox_cs.SizeX();
				auto height = bbox_cs.SizeY();
				auto aspect = width / height;

				// Set the aspect ratio
				if (aspect < maths::float_eps || !IsFinite(aspect))
				{
					// Handle degeneracy
					const auto min_aspect = maths::tinyf;
					const auto max_aspect = 1 / maths::tinyf;
					if      (width  > maths::float_eps) height = width / max_aspect;
					else if (height > maths::float_eps) width = min_aspect / height;
					else { width = 1; height = 1; }
					aspect = width / height;
				}
				Aspect(aspect);

				// Choose the field of view. If 'focus_dist' is given, then that determines
				// the X,Y field of view. If not, choose a focus distance based on a view size
				// equal to the average of 'width' and 'height' using the default FOV.
				if (focus_dist != 0)
				{
					FovY(2.0 * std::atan(0.5 * height / focus_dist));
				}
				else
				{
					auto size = (width + height) / 2.0;
					focus_dist = (0.5 * size) / std::tan(0.5 * m_default_fovY);

					// Allow for the depth of the bbox. Assume the W/H of the bbox are at the nearest
					// face of the bbox to the camera. Unless, the bbox.radius.z is greater than the 
					// default focus distance. In that case, just use the bbox.radius.z. The FoV will
					// cover the centre of the bbox
					auto d = 1.1 * sizez > focus_dist ? focus_dist : focus_dist - sizez;
					FovY(2.0 * std::atan(0.5 * height / d));
				}
			}
			else
			{
				// 'size' is the *radius* (i.e. not the full height) of the bounding box projected onto the 'forward' plane.
				auto size = Sqrt(Clamp(LengthSq(bbox_radius) - Sqr(sizez), 0.0f, maths::float_max));

				// Choose the focus distance if not given
				if (focus_dist == 0 || focus_dist < sizez)
				{
					auto d = size / (std::tan(0.5 * FovY()) * m_aspect);
					focus_dist = sizez + d;
				}
				// Otherwise, set the FOV
				else
				{
					auto d = focus_dist - sizez;
					FovY(2.0 * std::atan(size * m_aspect / d));
				}
			}

			// the distance from camera to bbox_centre is 'dist + sizez'
			LookAt(bbox_centre - s_cast<float>(focus_dist) * forward, bbox_centre, up, update_base);
		}

		// Set the camera fields of view so that a rectangle with dimensions 'width'/'height' exactly fits the view at 'dist'.
		void View(float width, float height, double focus_dist = 0)
		{
			PR_ASSERT(PR_DBG, width > 0 && height > 0 && focus_dist >= 0, "");

			// This works for orthographic mode as well so long as we set FOV
			Aspect(width / height);

			// If 'focus_dist' is given, choose FOV so that the view exactly fits
			if (focus_dist != 0)
			{
				FovY(2.0 * std::atan(0.5 * height / focus_dist));
				FocusDist(focus_dist);
			}
			// Otherwise, choose a focus distance that preserves FOV
			else
			{
				FocusDist(0.5 * height / std::tan(0.5 * FovY()));
			}
		}

		// Orbit the camera about the focus point by 'angle_rad' radians
		void Orbit(float angle_rad, bool commit = true)
		{
			// Record the focus point
			v4 old_focus = FocusPoint();

			// Find the axis of rotation
			v4 axis = IsAligned() ? InvertFast(m_c2w) * m_align : m_c2w.y;

			// Rotate the camera transform and reposition to look at the focus point
			m_c2w     = m_c2w * m4x4::Transform(axis, angle_rad, v4Origin);
			m_c2w.pos = old_focus + s_cast<float>(m_focus_dist) * m_c2w.z;
			m_c2w     = Orthonorm(m_c2w);

			// Set the base values
			if (commit)
				Commit();

			m_moved = true;
		}

		// Keyboard navigation.
		void KBNav(float mov, float rot)
		{
			// Notes:
			//  - Remember to use 'if (GetForegroundWindow() == GetConsoleWindow())' for navigation only while the window has focus

			if (KeyDown(m_key[ENavKey::Rotate]))
			{
				if (KeyDown(m_key[ENavKey::Left ])) Rotate(0, +rot, 0, true);
				if (KeyDown(m_key[ENavKey::Right])) Rotate(0, -rot, 0, true);
				if (KeyDown(m_key[ENavKey::Up   ])) Rotate(-rot, 0, 0, true);
				if (KeyDown(m_key[ENavKey::Down ])) Rotate(+rot, 0, 0, true);
				if (KeyDown(m_key[ENavKey::In   ])) Translate(0, +mov, 0, true);
				if (KeyDown(m_key[ENavKey::Out  ])) Translate(0, -mov, 0, true);
			}
			else
			{
				if (KeyDown(m_key[ENavKey::Left ])) Translate(-mov, 0, 0, true);
				if (KeyDown(m_key[ENavKey::Right])) Translate(+mov, 0, 0, true);
				if (KeyDown(m_key[ENavKey::Up   ])) Translate(0, 0, -mov, true);
				if (KeyDown(m_key[ENavKey::Down ])) Translate(0, 0, +mov, true);
			}
		}
	};
}
