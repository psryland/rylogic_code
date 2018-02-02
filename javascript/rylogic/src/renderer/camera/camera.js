import * as Maths from "../../maths/maths";
import * as v4 from "../../maths/v4";
import * as m4x4 from "../../maths/m4x4";

export var ENavOp = Object.freeze(
{
	"None": 0,
	"Translate": 1 << 0,
	"Rotate": 1 << 1,
	"Zoom": 1 << 2,
});

export var ENavKey = Object.freeze(
{
	"Left": 0,
	"Up": 1,
	"Right": 2,
	"Down": 3,
	"In": 4,
	"Out": 5,
	"Rotate": 6,     // Key to enable camera rotations, maps translation keys to rotations
	"TranslateZ": 7, // Key to set In/Out to be z translations rather than zoom
	"Accurate": 8,
	"SuperAccurate": 9,
	"PerpendicularZ": 10,
});

export var ELockMask = Object.freeze(
{
	"None": 0,
	"TransX": 1 << 0,
	"TransY": 1 << 1,
	"TransZ": 1 << 2,
	"RotX": 1 << 3,
	"RotY": 1 << 4,
	"RotZ": 1 << 5,
	"Zoom": 1 << 6,
	"All": (1 << 7) - 1, // Not including camera relative
	"CameraRelative": 1 << 7,
});

/**
 * Camera matrix class
 */
export class Camera
{
	constructor(fovY, aspect, focus_distance, near, far, orthographic)
	{
		this.m_c2w                 = m4x4.create();                            // Camera to world transform
		this.m_fovY                = fovY || Maths.TauBy8;                     // Field of view in the Y direction
		this.m_aspect              = aspect || 1.0;                            // Aspect ratio = width/height
		this.m_focus_dist          = focus_distance || 1.0;                    // Distance from the c2w position to the focus, down the z axis
		this.m_align               = null;                                     // The direction to align 'up' to, or v4Zero
		this.m_focus_relative_clip = true;                                     // True if the near/far clip planes should be relative to the focus point
		this.m_orthographic        = orthographic || false;                    // True for orthographic camera to screen transforms, false for perspective
		this.m_default_fovY        = this.fovY                                 // The default field of view
		this.m_near                = near ? (near / this.focus_dist) : 0.01;   // The near plane as a multiple of the focus distance
		this.m_far                 = far  ? (far  / this.focus_dist) : 100.0;  // The near plane as a multiple of the focus distance
		this.m_base_c2w            = m4x4.create();                            // The starting position during a mouse movement
		this.m_base_fovY           = this.fovY;                                // The starting FOV during a mouse movement
		this.m_base_focus_dist     = this.focus_dist;                          // The starting focus distance during a mouse movement
		this.m_accuracy_scale      = 0.1;                                      // Scale factor for high accuracy control
		this.m_Tref                = null;                                     // Movement start reference point for translation
		this.m_Rref                = null;                                     // Movement start reference point for rotation
		this.m_Zref                = null;                                     // Movement start reference point for zoom
		this.m_lock_mask           = ELockMask.None;                           // Locks on the allowed motion
		this.m_moved               = null;                                     // Dirty flag for when the camera moves
		
		// Default async key state callback function
		this.KeyDown = function(nav_key)
		{
			return false;
		};
	}

	/**
	 * Get/Set the camera to world transform
	 * @returns {m4x4}
	 */
	get c2w()
	{
		return this.m_c2w;
	}

	/**
	 * Get the world to camera transform
	 * @returns {m4x4}
	 */
	get w2c()
	{
		return m4x4.InvertFast(this.m_c2w);
	}

	/**
	 * Get the camera to screen transform (i.e. projection transform)
	 * @returns {m4x4}
	 */
	get c2s()
	{
		let height = 2.0 * this.m_focus_dist * Math.tan(this.m_fovY * 0.5);
		return this.m_orthographic
			? m4x4.ProjectionOrthographic(height*this.m_aspect, height, this.m_near, this.m_far, true)
			: m4x4.ProjectionPerspectiveFOV(this.m_fovY, this.m_aspect, this.m_near, this.m_far, true);
	}

	/**
	 * Get/Set the position of the camera in world space
	 * @returns {m4x4}
	 */
	get pos()
	{
		return m4x4.GetW(this.m_c2w);
	}
	set pos(value)
	{
		this.m_moved |= !v4.Eql(value, m4x4.GetW(this.m_c2w));
		m4x4.SetW(this.m_c2w, value);
		m4x4.SetW(this.m_base_c2w, value);
	}

	/**
	 * Get/Set the focus distance
	 * @returns {Number}
	 */
	get focus_dist()
	{
		return this.m_focus_dist;
	}
	set focus_dist(value)
	{
		if (isNaN(value) || value < 0) throw new Error("Invalid focus distance");
		this.m_moved |= value != this.m_focus_dist;
		this.m_base_focus_dist = this.m_focus_dist = Maths.Clamp(value, this.focus_dist_min, this.focus_dist_max);
	}

	/**
	 * Get the maximum allowed distance for 'focus_dist'
	 */
	get focus_dist_max()
	{
		// Clamp so that Near*Far is finite
		// N*F == (m_near * dist) * (m_far * dist) < float_max
		//     == m_near * m_far * dist^2 < float_max
		// => dist < sqrt(float_max) / (m_near * m_far)
		//assert(m_near * m_far > 0);
		const sqrt_float_max = 1.84467435239537E+19;
		return sqrt_float_max / (this.m_near * this.m_far);
	}

	/**
	 * Return the minimum allowed value for 'focus_dist'
	 */
	get focus_dist_min()
	{
		// Clamp so that N - F is non-zero
		// Abs(N - F) == Abs((m_near * dist) - (m_far * dist)) > float_min
		//       == dist * Abs(m_near - m_far) > float_min
		//       == dist > float_min / Abs(m_near - m_far);
		//assert(m_near < m_far);
		const float_min = 1.175494351e-38;
		return float_min / Math.min(Math.abs(this.m_near - this.m_far), 1.0);
	}

	/**
	 * Get/Set the world space position of the focus point
	 * Set maintains the current camera orientation
	 * @returns {v4}
	*/
	get focus_point()
	{
		return v4.Sub(this.pos, v4.MulS(m4x4.GetZ(this.m_c2w), this.focus_dist));
	}
	set focus_point(value)
	{
		// Move the camera position by the difference in focus_point positions
		let pos = v4.Add(this.pos, v4.Sub(value, this.focus_point));
		m4x4.SetW(this.m_c2w, pos);
		m4x4.SetW(this.m_base_c2w, pos);
		this.m_moved = true;
	}

	/**
	 * Get the forward direction of the camera (i.e. -Z)
	 */
	get fwd()
	{
		return v4.Neg(m4x4.GetW(this.m_c2w));
	}

	/**
	 * Get the near plane distance (in world space)
	 */
	get near()
	{
		return (this.m_focus_relative_clip ? this.m_focus_dist : 1) * this.m_near;
	}
	
	/**
	 * Get the far plane distance  (in world space)
	 */
	get far()
	{
		return (this.m_focus_relative_clip ? this.m_focus_dist : 1) * this.m_far;
	}

	/**
	 * Return the current zoom scaling factor.
	 * @returns {Number}
	 */
	get zoom()
	{
		return this.m_default_fovY / this.m_fovY;
	}

	/**
	 * Get/Set the horizontal field of view (in radians).
	 */
	get fovX()
	{
		return 2.0 * Math.atan(Math.tan(this.m_fovY * 0.5) * this.m_aspect);
	}
	set fovX(value)
	{
		this.fovY = (2.0 * Math.atan(Math.tan(value * 0.5) / this.m_aspect));
	}

	/**
	 * Get/Set the vertical field of view (in radians).
	 */
	get fovY()
	{
		return this.m_fovY;
	}
	set fovY(value)
	{
		if (value <= 0 || value >= Maths.TauBy2) throw new Error("Invalid field of view: " + value);
		value = Maths.Clamp(value, Maths.Tiny, Maths.TauBy2 - Maths.Tiny);
		this.m_moved = value != this.m_fovY;
		this.m_fovY = this.m_base_fovY = value;
	}

	/**
	 * Get/Set the aspect ratio
	 * @returns {Number}
	 */
	get aspect()
	{
		return this.m_aspect;
	}
	set aspect(aspect_w_by_h)
	{
		if (isNaN(aspect_w_by_h) || aspect_w_by_h <= 0)
			throw new Error("Invalid camera aspect ratio");

		this.m_moved = aspect_w_by_h != this.m_aspect;
		this.m_aspect = aspect_w_by_h;
	}

	/**
	 * Get/Set orthographic projection mode
	 * @returns {boolean}
	 */
	get orthographic()
	{
		return this.m_orthographic;
	}
	set orthographic(value)
	{
		this.m_orthographic = value;
	}

	/**
	 * True if the camera has moved (dirty flag for user code)
	 */
	get moved()
	{
		return this.m_moved;
	}
	set moved(value)
	{
		this.m_moved = value;
	}

	/**
	 * Return the size of the perpendicular area visible to the camera at 'dist' (in world space)
	 * @param {Number} dist (optional) The distance from the camera to calculate the view area. (default: focus_distance)
	 * @returns {[w,h]} The width/height of the view area
	 */
	ViewArea(dist)
	{
		dist = (!dist || this.orthographic) ? this.focus_dist : dist;
		let h = 2.0 * Math.tan(this.fovY * 0.5);
		return [dist * h * this.aspect, dist * h];
	}
	
	/**
	 * Position the camera at 'position' looking at 'lookat' with up pointing 'up'.
	 * @param {v4} position The world space position of the camera
	 * @param {v4} focus_point The target that the camera is to look at
	 * @param {v4} up The up direction of the camera in world space
	 * @param {*} commit (optional) True if the changes to the camera should be commited (default: true)
	 */
	LookAt(position, focus_point, up, commit)
	{
		m4x4.LookAt(position, focus_point, up, this.m_c2w);
		this.m_focus_dist = v4.Length(v4.Sub(focus_point, position));

		// Set the base values
		if (commit === undefined || commit)
			this.Commit();
	}

	/**
	 * Modify the camera position based on mouse movement.
	 * @param {[x,y]} point The normalised screen space mouse position. i.e. x=[-1, -1], y=[-1,1] with (-1,-1) == (left,bottom). i.e. normal Cartesian axes
	 * @param {ENavOp} nav_op The type of navigation operation to perform
	 * @param {boolean} ref_point True on the mouse down/up event, false while dragging
	 * @returns {boolean} True if the camera has moved
	 */
	MouseControl(point, nav_op, ref_point)
	{
		// Navigation operations
		let translate = (nav_op & ENavOp.Translate) != 0;
		let rotate    = (nav_op & ENavOp.Rotate   ) != 0;
		let zoom      = (nav_op & ENavOp.Zoom     ) != 0;

		if (ref_point)
		{
			if ((nav_op & ENavOp.Translate) != 0) this.m_Tref = point.slice();
			if ((nav_op & ENavOp.Rotate   ) != 0) this.m_Rref = point.slice();
			if ((nav_op & ENavOp.Zoom     ) != 0) this.m_Zref = point.slice();
			this.Commit();
		}
		if (zoom || (translate && rotate))
		{
			if (this.KeyDown(ENavKey.TranslateZ))
			{
				// Move in a fraction of the focus distance
				let delta = zoom ? (point[1] - this.m_Zref[1]) : (point[1] - this.m_Tref[1]);
				this.Translate(0, 0, delta * 10.0, false);
			}
			else
			{
				// Zoom the field of view
				let zm = zoom ? (this.m_Zref[1] - point[1]) : (this.m_Tref[1] - point[1]);
				this.Zoom(zm, false);
			}
		}
		if (translate && !rotate)
		{
			let dx = (this.m_Tref[0] - point[0]) * this.focus_dist * Math.tan(this.fovY * 0.5) * this.aspect;
			let dy = (this.m_Tref[1] - point[1]) * this.focus_dist * Math.tan(this.fovY * 0.5);
			this.Translate(dx, dy, 0, false);
		}
		if (rotate && !translate)
		{
			// If in the roll zone
			if (Maths.Length(this.m_Rref) < 0.80)
				this.Rotate((point[1] - this.m_Rref[1]) * Maths.TayBy4, (this.m_Rref[0] - point[0]) * Maths.TauBy4, 0, false);
			else
				this.Rotate(0, 0, Math.atan2(this.m_Rref[1], this.m_Rref[0]) - Math.atan2(point[1], point[0]), false);
		}
		return this.m_moved;
	}

	/**
	 * Modify the camera position in the camera Z direction based on mouse wheel.
	 * @param {[x,y]} point A point in normalised camera space. i.e. x=[-1, -1], y=[-1,1] with (-1,-1) == (left,bottom). i.e. normal Cartesian axes
	 * @param {Number} delta The mouse wheel scroll delta value (i.e. 120 = 1 click = 10% of the focus distance)
	 * @param {boolean} along_ray True if the camera should move along the ray from the camera position to 'point', false to move in the camera z direction.
	 * @param {boolean} move_focus True if the focus point should be moved along with the camera
	 * @param {boolean} commit (optional) True to commit the movement
	 * @returns {boolean} True if the camera has moved
	 */
	MouseControlZ(point, dist, along_ray, commit)
	{
		// Ignore if Z motion is locked
		if ((this.m_lock_mask & ELockMask.TransZ) != 0)
			return false;

		// Scale by the focus distance
		dist *= this.m_base_focus_dist * 0.1;

		// Get the ray in camera space to move the camera along
		let ray_cs = v4.Neg(v4.ZAxis);
		if (along_ray)
		{
			// Move along a ray cast from the camera position to the
			// mouse point projected onto the focus plane.
			let pt = this.NSSPointToWSPoint(v4.make(point[0], point[1], this.focus_dist, 0));
			let ray_ws = v4.Sub(pt, this.pos);
			ray_cs = v4.Normalise(m4x4.MulMV(this.w2c, ray_ws), null, {def: v4.Neg(v4.ZAxis)});
		}
		v4.MulS(ray_cs, dist, ray_cs);

		// If 'move_focus', move the focus point too.
		// Otherwise move the camera toward or away from the focus point.
		if (!this.KeyDown(ENavKey.TranslateZ))
			this.m_focus_dist = Maths.Clamp(this.m_base_focus_dist + ray_cs[2], this.focus_dist_min, this.focus_dist_max);

		// Translate
		let pos = v4.Add(m4x4.GetW(this.m_base_c2w), m4x4.MulMV(this.m_base_c2w, ray_cs));
		if (!v4.IsNaN(pos))
			m4x4.SetW(this.m_c2w, pos);

		// Apply non-camera relative locking
		if (this.m_lock_mask != ELockMask.None && !(this.m_lock_mask & ELockMask.CameraRelative))
		{
			if ((m_lock_mask & ELockMask.TransX)) this.m_c2w[12] = this.m_base_c2w[12];
			if ((m_lock_mask & ELockMask.TransY)) this.m_c2w[13] = this.m_base_c2w[13];
			if ((m_lock_mask & ELockMask.TransZ)) this.m_c2w[14] = this.m_base_c2w[14];
		}

		// Set the base values
		if (commit === undefined || commit)
			this.Commit();

		this.m_moved = true;
		return this.m_moved;
	}

	/**
	 * Translate by a camera relative amount.
	 * @param {Number} dx The x distance to move
	 * @param {Number} dy The y distance to move
	 * @param {Number} dz The z distance to move
	 * @param {boolean} commit (optional) True to commit the movement
	 * @returns {boolean} True if the camera has moved (for consistency with MouseControl)
	 */
	Translate(dx, dy, dz, commit)
	{
		if (this.m_lock_mask != ELockMask.None && (m_lock_mask & ELockMask.CameraRelative))
		{
			if ((this.m_lock_mask & ELockMask.TransX)) dx = 0;
			if ((this.m_lock_mask & ELockMask.TransY)) dy = 0;
			if ((this.m_lock_mask & ELockMask.TransZ)) dz = 0;
		}
		if (this.KeyDown(ENavKey.Accurate))
		{
			dx *= this.m_accuracy_scale;
			dy *= this.m_accuracy_scale;
			dz *= this.m_accuracy_scale;
			if (this.KeyDown(ENavKey.SuperAccurate))
			{
				dx *= this.m_accuracy_scale;
				dy *= this.m_accuracy_scale;
				dz *= this.m_accuracy_scale;
			}
		}

		// Move in a fraction of the focus distance
		dz = -this.m_base_focus_dist * dz * 0.1;
		if (!this.KeyDown(ENavKey.TranslateZ))
			this.m_focus_dist = Maths.Clamp(this.m_base_focus_dist + dz, this.focus_dist_min, this.focus_dist_max);

		// Translate
		let pos = v4.Add(m4x4.GetW(this.m_base_c2w), m4x4.MulMV(this.m_base_c2w, v4.make(dx, dy, dz, 0)));
		if (!v4.IsNaN(pos))
			m4x4.SetW(this.m_c2w, pos);

		// Apply non-camera relative locking
		if (this.m_lock_mask != ELockMask.None && !(this.m_lock_mask & ELockMask.CameraRelative))
		{
			if ((this.m_lock_mask & ELockMaskTransX)) this.m_c2w[12] = this.m_base_c2w[12];
			if ((this.m_lock_mask & ELockMaskTransY)) this.m_c2w[13] = this.m_base_c2w[13];
			if ((this.m_lock_mask & ELockMaskTransZ)) this.m_c2w[14] = this.m_base_c2w[14];
		}

		// Set the base values
		if (commit === undefined || commit)
			this.Commit();

		this.m_moved = true;
		return this.m_moved;
	}

	/**
	 * Rotate the camera by Euler angles about the focus point.
	 * @param {Number} pitch
	 * @param {Number} yaw
	 * @param {Number} roll
	 * @param {boolean} commit (optional) True to commit the movement
	 * @returns {boolean} True if the camera has moved (for consistency with MouseControl)
	 */
	Rotate(pitch, yaw, roll, commit)
	{
		if (this.m_lock_mask != ELockMask.None)
		{
			if ((this.m_lock_mask & ELockMask.RotX)) pitch	= 0;
			if ((this.m_lock_mask & ELockMask.RotY)) yaw	= 0;
			if ((this.m_lock_mask & ELockMask.RotZ)) roll	= 0;
		}
		if (this.KeyDown(ENavKey.Accurate))
		{
			pitch *= this.m_accuracy_scale;
			yaw   *= this.m_accuracy_scale;
			roll  *= this.m_accuracy_scale;
			if (this.KeyDown(ENavKey.SuperAccurate))
			{
				pitch *= this.m_accuracy_scale;
				yaw   *= this.m_accuracy_scale;
				roll  *= this.m_accuracy_scale;
			}
		}

		// Save the world space position of the focus point
		let old_focus = this.focus_point;

		// Rotate the camera matrix
		m4x4.clone(m4x4.MulMM(this.m_base_c2w, m4x4.Euler(pitch, yaw, roll, v4.Origin)), this.m_c2w);

		// Position the camera so that the focus is still in the same position
		m4x4.SetW(this.m_c2w, v4.Add(old_focus, v4.MulS(m4x4.GetZ(m_c2w), this.focus_dist)));

		// If an align axis is given, align up to it
		if (this.m_align)
		{
			let up = v4.Perpendicular(v4.Sub(this.pos, old_focus), this.m_align);
			m4x4.LookAt(this.pos, old_focus, up, this.m_c2w);
		}

		// Set the base values
		if (commit === undefined || commit)
			this.Commit();

		this.m_moved = true;
		return this.m_moved;
	}

	/**
	 * Zoom the field of view.
	 * @param {Number} zoom The amount to zoom. Must be in the range (-1, 1) where negative numbers zoom in, positive out.
	 * @param {boolean} commit (optional) True to commit the movement
	 * @returns {boolean} True if the camera has moved (for consistency with MouseControl).
	 */
	Zoom(zoom, commit)
	{
		if (this.m_lock_mask != ELockMask.None)
		{
			if ((this.m_lock_mask & ELockMask.Zoom)) return false;
		}
		if (this.KeyDown(ENavKey.Accurate))
		{
			zoom *= this.m_accuracy_scale;
			if (this.KeyDown(ENavKey.SuperAccurate))
				zoom *= this.m_accuracy_scale;
		}

		this.m_fovY = (1 + zoom) * this.m_base_fovY;
		this.m_fovY = Maths.Clamp(this.m_fovY, Maths.Tiny, Maths.TauBy2 - Maths.Tiny);

		// Set the base values
		if (commit === undefined || commit)
			this.Commit();

		this.m_moved = true;
		return this.m_moved;
	}

	/**
	 * Return a point in world space corresponding to a normalised screen space point.
	 * The x,y components of 'nss_point' should be in normalised screen space, i.e. (-1,-1)->(1,1)
	 * The z component should be the depth into the screen (i.e. d*-c2w.z, where 'd' is typically positive)
	 * @param {v4} nss_point
	 * @returns {v4}
	 */
	NSSPointToWSPoint(nss_point)
	{
		let half_height = this.focus_dist * Math.tan(this.fovY * 0.5);

		// Calculate the point in camera space
		let point = v4.create();
		point[0] = nss_point[0] * this.aspect * half_height;
		point[1] = nss_point[1] * half_height;
		if (!this.orthographic)
		{
			let sz = nss_point[2] / this.focus_dist;
			point[0] *= sz;
			point[1] *= sz;
		}
		point[2] = -nss_point[2];
		point[3] = 1.0;
		return m4x4.MulMV(this.c2w, point); // camera space to world space
	}

	/**
	 * Return a point in normalised screen space corresponding to 'ws_point'
	 * The returned 'z' component will be the world space distance from the camera.
	 * @param {v4} ws_point
	 * @returns {v4}
	 */
	WSPointToNSSPoint(ws_point)
	{
		let half_height = this.focus_dist * Math.tan(this.fovY * 0.5);

		// Get the point in camera space and project into normalised screen space
		let cam = m4x4.MulMV(this.w2c, ws_point);

		let point = v4.create();
		point[0] = cam[0] / (this.aspect * half_height);
		point[1] = cam[1] / (half_height);
		if (!this.orthographic)
		{
			let sz = -this.focus_dist / cam[2];
			point[0] *= sz;
			point[1] *= sz;
		}
		point[2] = -cam[2];
		point[3] = 1.0;
		return point;
	}

	/**
	 * Return a point and direction in world space corresponding to a normalised screen space point.
	 * The x,y components of 'nss_point' should be in normalised screen space, i.e. (-1,-1)->(1,1)
	 * The z component should be the world space distance from the camera.
	 * @param {v4} nss_point
	 * @returns {{ws_point:v4,ws_direction:v4]}
	 */
	NSSPointToWSRay(nss_point)
	{
		let pt = this.NSSPointToWSPoint(nss_point);
		let ws_point = m4x4.GetW(this.c2w);
		let ws_direction = v4.Normalise(v4.Sub(pt, ws_point));
		return {ws_point:ws_point, ws_direction:ws_direction};
	}
	
	/**
	 * Set the current position, FOV, and focus distance as the position reference.
	 */
	Commit()
	{
		m4x4.Orthonorm(this.m_c2w, this.m_c2w)
		m4x4.clone(this.m_c2w, this.m_base_c2w);
		this.m_base_fovY = this.m_fovY;
		this.m_base_focus_dist = this.m_focus_dist;
	}

	/**
	 * Revert navigation back to the last commit
	 */
	Revert()
	{
		m4x4.clone(this.m_base_c2w, this.m_c2w);
		this.m_fovY = this.m_base_fovY;
		this.m_focus_dist = this.m_base_focus_dist;
		this.m_moved = true;
	}
}

/**
 * Create a Camera instance
 * @param {Number} fovY the field of view in the vertical direction (in radians)
 * @param {Number} aspect the aspect ratio of the view
 * @param {Number} focus_distance the distance to the focus point (in world space)
 * @param {Number} near the distance to the near clip plane (in world space)
 * @param {Number} far the distance to the far clip plane (in world space)
 * @param {boolean} orthographic true for orthographic projection, false for perspective
 * @returns {Camera}
 */
export function Create(fovY, aspect, focus_distance, near, far, orthographic)
{
	return new Camera(fovY, aspect, focus_distance, near, far, orthographic);
}

