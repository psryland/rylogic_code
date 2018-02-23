import * as Math_ from "../../maths/maths";
import * as M4x4_ from "../../maths/m4x4";
import * as BBox_ from "../../maths/bbox";
import * as Vec4_ from "../../maths/v4";
import * as Vec2_ from "../../maths/v2";
import M4x4 = Math_.M4x4;
import Vec4 = Math_.Vec4;
import Vec2 = Math_.Vec2;
import BBox = Math_.BBox;

export enum ENavOp
{
	None = 0,
	Translate = 1 << 0,
	Rotate = 1 << 1,
	Zoom = 1 << 2,
}
export enum ENavKey
{
	Left = 0,
	Up = 1,
	Right = 2,
	Down = 3,
	In = 4,
	Out = 5,
	Rotate = 6,     // Key to enable camera rotations, maps translation keys to rotations
	TranslateZ = 7, // Key to set In/Out to be z translations rather than zoom
	Accurate = 8,
	SuperAccurate = 9,
	PerpendicularZ = 10,
}
export enum ELockMask 
{
	None = 0,
	TransX = 1 << 0,
	TransY = 1 << 1,
	TransZ = 1 << 2,
	RotX = 1 << 3,
	RotY = 1 << 4,
	RotZ = 1 << 5,
	Zoom = 1 << 6,
	All = (1 << 7) - 1, // Not including camera relative
	CameraRelative = 1 << 7,
}

/** Camera matrix class */
export interface ICamera
{
	clone(): ICamera;

	/** Async key state callback function */
	KeyDown: (nav_key: ENavKey) => boolean;

	/** Get the camera to world transform */
	c2w: M4x4;

	/** Get the world to camera transform */
	w2c: M4x4;

	/** Get the camera to screen transform (i.e. projection transform) */
	c2s: M4x4;

	/** Get/Set the position of the camera in world space */
	pos: Vec4;

	/** Get/Set the focus distance */
	focus_dist: number;

	/** Get the maximum allowed distance for 'focus_dist' */
	focus_dist_max: number;

	/** Get the minimum allowed distance for 'focus_dist' */
	focus_dist_min: number;

	/**
	 * Get/Set the world space position of the focus point.
	 * Set maintains the current camera orientation
	*/
	focus_point: Vec4;

	/** Get the forward direction of the camera (i.e. -Z) */
	fwd: Vec4;

	/**
	 * Get the near plane distance (in world space)
	 * @param focus_relative True to return the focus relative value
	 */
	near(focus_relative: boolean): number;

	/**
	 * Get the far plane distance  (in world space)
	 * @param focus_relative True to return the focus relative value
	 */
	far(focus_relative: boolean): number;

	/** Return the current zoom scaling factor. */
	zoom: number;

	/** Get/Set the horizontal field of view (in radians). */
	fovX: number;

	/** Get/Set the vertical field of view (in radians). */
	fovY: number;

	/** Get/Set the aspect ratio */
	aspect: number;

	/** Get/Set orthographic projection mode */
	orthographic: boolean;

	/** True if the camera has moved (dirty flag for user code) */
	moved: boolean;

	/**
	 * Return the size of the perpendicular area visible to the camera at 'dist' (in world space)
	 * @param dist The distance from the camera to calculate the view area. (default: focus_distance)
	 * @returns The width/height of the view area
	 */
	ViewArea(dist ?: number): Vec2

	/**
	 * Position the camera so that all of 'bbox' is visible to the camera when looking 'forward' and 'up'.
	 * @param bbox The bounding box to view
	 * @param forward The forward direction of the camera
	 * @param up The up direction of the camera
	 * @param focus_dist (default = 0)
	 * @param preserve_aspect (default = true)
	 * @param commit True to commit the movement
	 */
	ViewBBox(bbox_: BBox, forward: Vec4, up: Vec4, focus_dist?: number, preserve_aspect?: boolean, commit?: boolean): void;

	/**
	 * Set the camera fields of view so that a rectangle with dimensions 'width'/'height' exactly fits the view at 'dist'.
	 * @param width The width of the rectangle to view
	 * @param height The height of the rectangle to view
	 * @param focus_dist
	 */
	ViewRect(width: number, height: number, focus_dist ?: number): void;

	/**
		* Position the camera at 'position' looking at 'lookat' with up pointing 'up'.
		* @param position The world space position of the camera
		* @param focus_point The target that the camera is to look at
		* @param up The up direction of the camera in world space
		* @param commit True if the changes to the camera should be committed
		*/
	LookAt(position: Vec4, focus_point: Vec4, up: Vec4, commit?: boolean): void;

	/**
	 * Modify the camera position based on mouse movement.
	 * @param point The normalised screen space mouse position. i.e. x=[-1, -1], y=[-1,1] with (-1,-1) == (left,bottom). i.e. normal Cartesian axes
	 * @param nav_op The type of navigation operation to perform
	 * @param ref_point True on the mouse down/up event, false while dragging
	 * @returns True if the camera has moved
	 */
	MouseControl(point: Vec2, nav_op: ENavOp, ref_point: boolean): boolean;

	/**
	 * Modify the camera position in the camera Z direction based on mouse wheel.
	 * @param point A point in normalised camera space. i.e. x=[-1, -1], y=[-1,1] with (-1,-1) == (left,bottom). i.e. normal Cartesian axes
	 * @param delta The mouse wheel scroll delta value (i.e. 120 = 1 click = 10% of the focus distance)
	 * @param along_ray True if the camera should move along the ray from the camera position to 'point', false to move in the camera z direction.
	 * @param move_focus True if the focus point should be moved along with the camera
	 * @param commit True to commit the movement
	 * @returns True if the camera has moved
	 */
	MouseControlZ(point: Vec2, dist: number, along_ray: boolean, commit?: boolean): boolean;

	/**
	 * Translate by a camera relative amount.
	 * @param dx The x distance to move
	 * @param dy The y distance to move
	 * @param dz The z distance to move
	 * @param commit True to commit the movement
	 * @returns True if the camera has moved (for consistency with MouseControl)
	 */
	Translate(dx: number, dy: number, dz: number, commit?: boolean): boolean;

	/**
	 * Rotate the camera by Euler angles about the focus point.
	 * @param pitch
	 * @param yaw
	 * @param roll
	 * @param commit True to commit the movement
	 * @returns True if the camera has moved (for consistency with MouseControl)
	 */
	Rotate(pitch: number, yaw: number, roll: number, commit?: boolean): boolean;

	/**
	 * Zoom the field of view.
	 * @param zoom The amount to zoom. Must be in the range (-1, 1) where negative numbers zoom in, positive out.
	 * @param commit True to commit the movement
	 * @returns True if the camera has moved (for consistency with MouseControl).
	 */
	Zoom(zoom: number, commit?: boolean): boolean;

	/**
	 * Return a point in world space corresponding to a normalised screen space point.
	 * The x,y components of 'nss_point' should be in normalised screen space, i.e. (-1,-1)->(1,1)
	 * The z component should be the depth into the screen (i.e. d*-c2w.z, where 'd' is typically positive)
	 * @param nss_point The normalised screen space point to convert to world space
	 */
	NSSPointToWSPoint(nss_point: Vec4): Vec4;

	/**
	 * Return a point in normalised screen space corresponding to 'ws_point'
	 * The returned 'z' component will be the world space distance from the camera.
	 * @param ws_point The world space point to convert to normalised screen space.
	 */
	WSPointToNSSPoint(ws_point: Vec4): Vec4;

	/**
	 * Return a point and direction in world space corresponding to a normalised screen space point.
	 * The x,y components of 'nss_point' should be in normalised screen space, i.e. (-1,-1)->(1,1)
	 * The z component should be the world space distance from the camera.
	 * @param nss_point The normalised screen space point to convert to a world space ray
	 */
	NSSPointToWSRay(nss_point: Vec4): { ws_point: Vec4, ws_direction: Vec4 };

	/**
	 * Set the current position, FOV, and focus distance as the position reference.
	 */
	Commit(): void;

	/**
	 * Revert navigation back to the last commit
	 */
	Revert(): void;
}
class CameraImpl implements ICamera
{
	private m_c2w: M4x4;               // Camera to world transform
	private m_fovY: number;            // Field of view in the Y direction
	private m_aspect: number;          // Aspect ratio = width/height
	private m_focus_dist: number;      // Distance from the c2w position to the focus, down the z axis
	private m_align: Vec4 | null;      // The direction to align 'up' to, or v4Zero
	private m_orthographic: boolean;   // True for orthographic camera to screen transforms, false for perspective
	private m_default_fovY: number;    //  The default field of view
	private m_near: number;            // The near plane as a multiple of the focus distance
	private m_far: number;             // The near plane as a multiple of the focus distance
	private m_base_c2w: M4x4;          // The starting position during a mouse movement
	private m_base_fovY: number;       // The starting FOV during a mouse movement
	private m_base_focus_dist: number; // The starting focus distance during a mouse movement
	private m_accuracy_scale: number;  // Scale factor for high accuracy control
	private m_Rref: Vec2 | null;       // Movement start reference point for rotation
	private m_Tref: Vec2 | null;       // Movement start reference point for translation
	private m_Zref: Vec2 | null;       // Movement start reference point for zoom
	private m_lock_mask: ELockMask;    // Locks on the allowed motion
	private m_moved: boolean;          // Dirty flag for when the camera moves

	/**
	 * Create a Camera instance
	 * @param fovY The field of view in the vertical direction (in radians)
	 * @param aspect The aspect ratio of the view
	 * @param focus_distance The distance to the focus point (in world space)
	 * @param near The distance to the near clip plane (in world space)
	 * @param far The distance to the far clip plane (in world space)
	 * @param orthographic True for orthographic projection, false for perspective
	 */
	constructor(fovY: number = Math_.TauBy8, aspect: number = 1.0, focus_distance: number = 1.0, near: number = 0.01, far: number = 100.0, orthographic: boolean = false)
	{
		this.m_c2w = M4x4_.create();
		this.m_fovY = fovY;
		this.m_aspect = aspect;
		this.m_focus_dist = focus_distance;
		this.m_near = near;
		this.m_far = far;
		this.m_orthographic = orthographic;
		this.m_align = null;
		this.m_default_fovY = this.fovY
		this.m_base_c2w = M4x4_.create();
		this.m_base_fovY = this.fovY;
		this.m_base_focus_dist = this.focus_dist;
		this.m_accuracy_scale = 0.1;
		this.m_Tref = null;
		this.m_Rref = null;
		this.m_Zref = null;
		this.m_lock_mask = ELockMask.None;
		this.m_moved = false;

		// Default async key state callback function
		this.KeyDown = function() { return false; };
	}
	clone(): CameraImpl
	{
		let copy = new CameraImpl();
		copy.m_c2w = M4x4_.clone(this.m_c2w);
		copy.m_fovY = this.m_fovY;
		copy.m_aspect = this.m_aspect;
		copy.m_focus_dist = this.m_focus_dist;
		copy.m_near = this.m_near;
		copy.m_far = this.m_far;
		copy.m_orthographic = this.m_orthographic;
		copy.m_align = this.m_align;
		copy.m_default_fovY = this.m_default_fovY;
		copy.m_base_c2w = M4x4_.clone(this.m_base_c2w);
		copy.m_base_fovY = this.m_base_fovY;
		copy.m_base_focus_dist = this.m_base_focus_dist;
		copy.m_accuracy_scale = this.m_accuracy_scale;
		copy.m_Tref = null;
		copy.m_Rref = null;
		copy.m_Zref = null;
		copy.m_lock_mask = this.m_lock_mask;
		copy.m_moved = this.m_moved;
		copy.KeyDown = this.KeyDown;
		return copy;
	}

	/** Async key state callback function */
	public KeyDown: (nav_key: ENavKey) => boolean;

	/** Get the camera to world transform */
	get c2w(): M4x4
	{
		return this.m_c2w;
	}

	/** Get the world to camera transform */
	get w2c(): M4x4
	{
		return M4x4_.InvertFast(this.m_c2w);
	}

	/** Get the camera to screen transform (i.e. projection transform) */
	get c2s(): M4x4
	{
		let height = 2.0 * this.focus_dist * Math.tan(this.fovY * 0.5);
		return this.orthographic
			? M4x4_.ProjectionOrthographic(height * this.m_aspect, height, this.near(false), this.far(false), true)
			: M4x4_.ProjectionPerspectiveFOV(this.m_fovY, this.m_aspect, this.near(false), this.far(false), true);
	}

	/** Get/Set the position of the camera in world space */
	get pos(): Vec4
	{
		return M4x4_.GetW(this.m_c2w);
	}
	set pos(value: Vec4)
	{
		this.m_moved = this.m_moved || !Vec4_.Eql(value, M4x4_.GetW(this.m_c2w));
		M4x4_.SetW(this.m_c2w, value);
		M4x4_.SetW(this.m_base_c2w, value);
	}

	/** Get/Set the focus distance */
	get focus_dist(): number
	{
		return this.m_focus_dist;
	}
	set focus_dist(value: number)
	{
		if (isNaN(value) || value < 0) throw new Error("Invalid focus distance");
		this.m_moved = this.m_moved || value != this.m_focus_dist;
		this.m_base_focus_dist = this.m_focus_dist = Math_.Clamp(value, this.focus_dist_min, this.focus_dist_max);
	}

	/** Get the maximum allowed distance for 'focus_dist' */
	get focus_dist_max(): number
	{
		// Clamp so that Near*Far is finite
		// N*F == (m_near * dist) * (m_far * dist) < float_max
		//     == m_near * m_far * dist^2 < float_max
		// => dist < sqrt(float_max) / (m_near * m_far)
		//assert(m_near * m_far > 0);
		const sqrt_float_max = 1.84467435239537E+19;
		return sqrt_float_max / (this.m_near * this.m_far);
	}

	/** Get the minimum allowed distance for 'focus_dist' */
	get focus_dist_min(): number
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
	 * Get/Set the world space position of the focus point.
	 * Set maintains the current camera orientation
	*/
	get focus_point(): Vec4
	{
		return Vec4_.Sub(this.pos, Vec4_.MulS(M4x4_.GetZ(this.m_c2w), this.focus_dist));
	}
	set focus_point(value: Vec4)
	{
		// Move the camera position by the difference in focus_point positions
		let pos = Vec4_.Add(this.pos, Vec4_.Sub(value, this.focus_point));
		M4x4_.SetW(this.m_c2w, pos);
		M4x4_.SetW(this.m_base_c2w, pos);
		this.m_moved = true;
	}

	/** Get the forward direction of the camera (i.e. -Z) */
	get fwd(): Vec4
	{
		return Vec4_.Neg(M4x4_.GetW(this.m_c2w));
	}

	/**
	 * Get the near plane distance (in world space)
	 * @param focus_relative True to return the focus relative value
	 */
	near(focus_relative: boolean): number
	{
		return (focus_relative ? 1 : this.m_focus_dist) * this.m_near;
	}

	/**
	 * Get the far plane distance  (in world space)
	 * @param focus_relative True to return the focus relative value
	 */
	far(focus_relative: boolean): number
	{
		return (focus_relative ? 1 : this.m_focus_dist) * this.m_far;
	}

	/** Return the current zoom scaling factor. */
	get zoom(): number
	{
		return this.m_default_fovY / this.m_fovY;
	}

	/** Get/Set the horizontal field of view (in radians). */
	get fovX(): number
	{
		return 2.0 * Math.atan(Math.tan(this.m_fovY * 0.5) * this.m_aspect);
	}
	set fovX(value: number)
	{
		this.fovY = (2.0 * Math.atan(Math.tan(value * 0.5) / this.m_aspect));
	}

	/** Get/Set the vertical field of view (in radians). */
	get fovY(): number
	{
		return this.m_fovY;
	}
	set fovY(value: number)
	{
		if (value <= 0 || value >= Math_.TauBy2) throw new Error("Invalid field of view: " + value);
		value = Math_.Clamp(value, Math_.Tiny, Math_.TauBy2 - Math_.Tiny);
		this.m_moved = value != this.m_fovY;
		this.m_fovY = this.m_base_fovY = value;
	}

	/** Get/Set the aspect ratio */
	get aspect(): number
	{
		return this.m_aspect;
	}
	set aspect(aspect_w_by_h: number)
	{
		if (isNaN(aspect_w_by_h) || aspect_w_by_h <= 0)
			throw new Error("Invalid camera aspect ratio");

		this.m_moved = aspect_w_by_h != this.m_aspect;
		this.m_aspect = aspect_w_by_h;
	}

	/** Get/Set orthographic projection mode */
	get orthographic(): boolean
	{
		return this.m_orthographic;
	}
	set orthographic(value: boolean)
	{
		this.m_orthographic = value;
	}

	/** True if the camera has moved (dirty flag for user code) */
	get moved(): boolean
	{
		return this.m_moved;
	}
	set moved(value: boolean)
	{
		this.m_moved = value;
	}

	/**
	 * Return the size of the perpendicular area visible to the camera at 'dist' (in world space)
	 * @param dist The distance from the camera to calculate the view area. (default: focus_distance)
	 * @returns The width/height of the view area
	 */
	ViewArea(dist?: number): Vec2
	{
		dist = (!dist || this.orthographic) ? this.focus_dist : dist;
		let h = 2.0 * Math.tan(this.fovY * 0.5);
		return [dist * h * this.aspect, dist * h];
	}

	/**
	 * Position the camera so that all of 'bbox' is visible to the camera when looking 'forward' and 'up'.
	 * @param bbox The bounding box to view
	 * @param forward The forward direction of the camera
	 * @param up The up direction of the camera
	 * @param focus_dist (default = 0)
	 * @param preserve_aspect (default = true)
	 * @param commit True to commit the movement
	 */
	ViewBBox(bbox_: BBox, forward: Vec4, up: Vec4, focus_dist: number = 0, preserve_aspect: boolean = true, commit: boolean = true)
	{
		// Make a copy so we don't change the callers bbox value
		let bbox = BBox_.clone(bbox_);

		if (!bbox.is_valid)
			throw new Error("Camera: Cannot view an invalid bounding box");
		if (bbox.is_point)
			return;

		// Handle degenerate bounding boxes
		if (Math_.FEql(bbox.radius[0], 0)) bbox.radius[0] = 0.001 * Vec4_.Length(bbox.radius);
		if (Math_.FEql(bbox.radius[1], 0)) bbox.radius[1] = 0.001 * Vec4_.Length(bbox.radius);
		if (Math_.FEql(bbox.radius[2], 0)) bbox.radius[2] = 0.001 * Vec4_.Length(bbox.radius);

		// This code projects 'bbox' onto a plane perpendicular to 'forward' and
		// at the nearest point of the bbox to the camera. It then ensures a circle
		// with radius of the projected 2D bbox fits within the view.
		let bbox_centre = bbox.centre;
		let bbox_radius = bbox.radius;

		// Get the distance from the centre of the bbox to the point nearest the camera
		let sizez = Number.MAX_VALUE;
		sizez = Math.min(sizez, Math.abs(Vec4_.Dot(forward, Vec4_.create(bbox_radius[0], bbox_radius[1], bbox_radius[2], 0))));
		sizez = Math.min(sizez, Math.abs(Vec4_.Dot(forward, Vec4_.create(-bbox_radius[0], bbox_radius[1], bbox_radius[2], 0))));
		sizez = Math.min(sizez, Math.abs(Vec4_.Dot(forward, Vec4_.create(bbox_radius[0], -bbox_radius[1], bbox_radius[2], 0))));
		sizez = Math.min(sizez, Math.abs(Vec4_.Dot(forward, Vec4_.create(bbox_radius[0], bbox_radius[1], -bbox_radius[2], 0))));

		// 'focus_dist' is the focus distance (chosen, or specified) from the centre of the bbox
		// to the camera. Since 'size' is the size to fit at the nearest point of the bbox,
		// the focus distance needs to be 'dist' + 'sizez'.

		// If not preserving the aspect ratio, determine the width
		// and height of the bbox as viewed from the camera.
		if (!preserve_aspect)
		{
			// Get the camera orientation matrix
			let c2w = M4x4_.create(Vec4_.Cross(up, forward), up, forward, Vec4_.Origin);
			let w2c = M4x4_.InvertFast(c2w);

			// Transform the bounding box to camera space
			let bbox_cs = M4x4_.MulMB(w2c, bbox);

			// Get the dimensions
			let bbox_cs_size = bbox_cs.size;
			let width = bbox_cs_size[0];
			let height = bbox_cs_size[1];
			//assert(width != 0 && height != 0);

			// Choose the fields of view. If 'focus_dist' is given, then that determines
			// the X,Y field of view. If not, choose a focus distance based on a view size
			// equal to the average of 'width' and 'height' using the default FOV.
			if (focus_dist == 0)
			{
				let size = (width + height) / 2;
				focus_dist = (0.5 * size) / Math.tan(0.5 * this.m_default_fovY);
			}

			// Set the aspect ratio
			let aspect = width / height;
			let d = focus_dist - sizez;

			// Set the aspect and FOV based on the view of the bbox
			this.aspect = aspect;
			this.fovY = 2 * Math.atan(0.5 * height / d);
		}
		else
		{
			// 'size' is the *radius* (i.e. not the full height) of the bounding box projected onto the 'forward' plane.
			let size = Math.sqrt(Math_.Clamp(Vec4_.LengthSq(bbox_radius) - Math_.Sqr(sizez), 0, Number.MAX_VALUE));

			// Choose the focus distance if not given
			if (focus_dist == 0 || focus_dist < sizez)
			{
				let d = size / (Math.tan(0.5 * this.fovY) * this.aspect);
				focus_dist = sizez + d;
			}
			// Otherwise, set the FOV
			else
			{
				let d = focus_dist - sizez;
				this.fovY = 2 * Math.atan(size * this.aspect / d);
			}
		}

		// The distance from camera to bbox_centre is 'dist + sizez'
		this.LookAt(Vec4_.Sub(bbox_centre, Vec4_.MulS(forward, focus_dist)), bbox_centre, up, commit);
	}

	/**
	 * Set the camera fields of view so that a rectangle with dimensions 'width'/'height' exactly fits the view at 'dist'.
	 * @param width The width of the rectangle to view
	 * @param height The height of the rectangle to view
	 * @param focus_dist
	 */
	ViewRect(width: number, height: number, focus_dist?: number)
	{
		if (width <= 0 || height <= 0) throw new Error("Invalid view rectangle");
		if (focus_dist && focus_dist <= 0) throw new Error("Invalid focus distance")

		// This works for orthographic mode as well so long as we set FOV
		this.aspect = width / height;

		// If 'focus_dist' is given, choose FOV so that the view exactly fits
		if (focus_dist)
		{
			this.fovY = 2 * Math.atan(0.5 * height / focus_dist);
			this.focus_dist = focus_dist;
		}
		// Otherwise, choose a focus distance that preserves FOV
		else
		{
			this.focus_dist = 0.5 * height / Math.tan(0.5 * this.fovY);
		}
	}

	/**
	 * Position the camera at 'position' looking at 'lookat' with up pointing 'up'.
	 * @param position The world space position of the camera
	 * @param focus_point The target that the camera is to look at
	 * @param up The up direction of the camera in world space
	 * @param commit True if the changes to the camera should be committed
	 */
	LookAt(position: Vec4, focus_point: Vec4, up: Vec4, commit: boolean = true)
	{
		M4x4_.LookAt(position, focus_point, up, this.m_c2w);
		this.m_focus_dist = Vec4_.Length(Vec4_.Sub(focus_point, position));

		// Set the base values
		if (commit)
			this.Commit();
	}

	/**
	 * Modify the camera position based on mouse movement.
	 * @param point The normalised screen space mouse position. i.e. x=[-1, -1], y=[-1,1] with (-1,-1) == (left,bottom). i.e. normal Cartesian axes
	 * @param nav_op The type of navigation operation to perform
	 * @param ref_point True on the mouse down/up event, false while dragging
	 * @returns True if the camera has moved
	 */
	MouseControl(point: Vec2, nav_op: ENavOp, ref_point: boolean): boolean
	{
		// Navigation operations
		let translate = (nav_op & ENavOp.Translate) != 0;
		let rotate = (nav_op & ENavOp.Rotate) != 0;
		let zoom = (nav_op & ENavOp.Zoom) != 0;

		if (ref_point)
		{
			if ((nav_op & ENavOp.Translate) != 0) this.m_Tref = Vec2_.clone(point);
			if ((nav_op & ENavOp.Rotate) != 0) this.m_Rref = Vec2_.clone(point);
			if ((nav_op & ENavOp.Zoom) != 0) this.m_Zref = Vec2_.clone(point);
			this.Commit();
		}
		if ((zoom || (translate && rotate)) && this.m_Zref && this.m_Tref)
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
		if ((translate && !rotate) && this.m_Tref)
		{
			let dx = (this.m_Tref[0] - point[0]) * this.focus_dist * Math.tan(this.fovY * 0.5) * this.aspect;
			let dy = (this.m_Tref[1] - point[1]) * this.focus_dist * Math.tan(this.fovY * 0.5);
			this.Translate(dx, dy, 0, false);
		}
		if ((rotate && !translate) && this.m_Rref)
		{
			// If in the roll zone. 'm_Rref' is a point in normalised space[-1, +1] x [-1, +1].
			// So the roll zone is a radial distance from the centre of the screen
			if (Vec2_.Length(this.m_Rref) < 0.80)
				this.Rotate((point[1] - this.m_Rref[1]) * Math_.TauBy4, (this.m_Rref[0] - point[0]) * Math_.TauBy4, 0, false);
			else
				this.Rotate(0, 0, Math.atan2(this.m_Rref[1], this.m_Rref[0]) - Math.atan2(point[1], point[0]), false);
		}
		return this.m_moved;
	}

	/**
	 * Modify the camera position in the camera Z direction based on mouse wheel.
	 * @param point A point in normalised camera space. i.e. x=[-1, -1], y=[-1,1] with (-1,-1) == (left,bottom). i.e. normal Cartesian axes
	 * @param delta The mouse wheel scroll delta value (i.e. 120 = 1 click = 10% of the focus distance)
	 * @param along_ray True if the camera should move along the ray from the camera position to 'point', false to move in the camera z direction.
	 * @param move_focus True if the focus point should be moved along with the camera
	 * @param commit True to commit the movement
	 * @returns True if the camera has moved
	 */
	MouseControlZ(point: Vec2, dist: number, along_ray: boolean, commit: boolean = true): boolean
	{
		// Ignore if Z motion is locked
		if ((this.m_lock_mask & ELockMask.TransZ) != 0)
			return false;

		// Scale by the focus distance
		dist *= this.m_base_focus_dist * 0.1;

		// Get the ray in camera space to move the camera along
		let ray_cs = Vec4_.Neg(Vec4_.ZAxis);
		if (along_ray)
		{
			// Move along a ray cast from the camera position to the
			// mouse point projected onto the focus plane.
			let pt = this.NSSPointToWSPoint(Vec4_.create(point[0], point[1], this.focus_dist, 0));
			let ray_ws = Vec4_.Sub(pt, this.pos);
			ray_cs = Vec4_.Normalise(M4x4_.MulMV(this.w2c, ray_ws), undefined, { def: Vec4_.Neg(Vec4_.ZAxis) });
		}
		Vec4_.MulS(ray_cs, dist, ray_cs);

		// If 'move_focus', move the focus point too.
		// Otherwise move the camera toward or away from the focus point.
		if (!this.KeyDown(ENavKey.TranslateZ))
			this.m_focus_dist = Math_.Clamp(this.m_base_focus_dist + ray_cs[2], this.focus_dist_min, this.focus_dist_max);

		// Translate
		let pos = Vec4_.Add(M4x4_.GetW(this.m_base_c2w), M4x4_.MulMV(this.m_base_c2w, ray_cs));
		if (!Vec4_.IsNaN(pos))
			M4x4_.SetW(this.m_c2w, pos);

		// Apply non-camera relative locking
		if (this.m_lock_mask != ELockMask.None && !(this.m_lock_mask & ELockMask.CameraRelative))
		{
			if ((this.m_lock_mask & ELockMask.TransX)) this.m_c2w[12] = this.m_base_c2w[12];
			if ((this.m_lock_mask & ELockMask.TransY)) this.m_c2w[13] = this.m_base_c2w[13];
			if ((this.m_lock_mask & ELockMask.TransZ)) this.m_c2w[14] = this.m_base_c2w[14];
		}

		// Set the base values
		if (commit)
			this.Commit();

		this.m_moved = true;
		return this.m_moved;
	}

	/**
	 * Translate by a camera relative amount.
	 * @param dx The x distance to move
	 * @param dy The y distance to move
	 * @param dz The z distance to move
	 * @param commit True to commit the movement
	 * @returns True if the camera has moved (for consistency with MouseControl)
	 */
	Translate(dx: number, dy: number, dz: number, commit: boolean = true): boolean
	{
		if (this.m_lock_mask != ELockMask.None && (this.m_lock_mask & ELockMask.CameraRelative))
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
			this.m_focus_dist = Math_.Clamp(this.m_base_focus_dist + dz, this.focus_dist_min, this.focus_dist_max);

		// Translate
		let pos = Vec4_.Add(M4x4_.GetW(this.m_base_c2w), M4x4_.MulMV(this.m_base_c2w, Vec4_.create(dx, dy, dz, 0)));
		if (!Vec4_.IsNaN(pos))
			M4x4_.SetW(this.m_c2w, pos);

		// Apply non-camera relative locking
		if (this.m_lock_mask != ELockMask.None && !(this.m_lock_mask & ELockMask.CameraRelative))
		{
			if ((this.m_lock_mask & ELockMask.TransX)) this.m_c2w[12] = this.m_base_c2w[12];
			if ((this.m_lock_mask & ELockMask.TransY)) this.m_c2w[13] = this.m_base_c2w[13];
			if ((this.m_lock_mask & ELockMask.TransZ)) this.m_c2w[14] = this.m_base_c2w[14];
		}

		// Set the base values
		if (commit === undefined || commit)
			this.Commit();

		this.m_moved = true;
		return this.m_moved;
	}

	/**
	 * Rotate the camera by Euler angles about the focus point.
	 * @param pitch
	 * @param yaw
	 * @param roll
	 * @param commit True to commit the movement
	 * @returns True if the camera has moved (for consistency with MouseControl)
	 */
	Rotate(pitch: number, yaw: number, roll: number, commit: boolean = true): boolean
	{
		if (this.m_lock_mask != ELockMask.None)
		{
			if ((this.m_lock_mask & ELockMask.RotX)) pitch = 0;
			if ((this.m_lock_mask & ELockMask.RotY)) yaw = 0;
			if ((this.m_lock_mask & ELockMask.RotZ)) roll = 0;
		}
		if (this.KeyDown(ENavKey.Accurate))
		{
			pitch *= this.m_accuracy_scale;
			yaw *= this.m_accuracy_scale;
			roll *= this.m_accuracy_scale;
			if (this.KeyDown(ENavKey.SuperAccurate))
			{
				pitch *= this.m_accuracy_scale;
				yaw *= this.m_accuracy_scale;
				roll *= this.m_accuracy_scale;
			}
		}

		// Save the world space position of the focus point
		let old_focus = this.focus_point;

		// Rotate the camera matrix
		M4x4_.clone(M4x4_.MulMM(this.m_base_c2w, M4x4_.Euler(pitch, yaw, roll, Vec4_.Origin)), this.m_c2w);

		// Position the camera so that the focus is still in the same position
		M4x4_.SetW(this.m_c2w, Vec4_.Add(old_focus, Vec4_.MulS(M4x4_.GetZ(this.m_c2w), this.focus_dist)));

		// If an align axis is given, align up to it
		if (this.m_align)
		{
			let up = Vec4_.Perpendicular(Vec4_.Sub(this.pos, old_focus), this.m_align);
			M4x4_.LookAt(this.pos, old_focus, up, this.m_c2w);
		}

		// Set the base values
		if (commit)
			this.Commit();

		this.m_moved = true;
		return this.m_moved;
	}

	/**
	 * Zoom the field of view.
	 * @param zoom The amount to zoom. Must be in the range (-1, 1) where negative numbers zoom in, positive out.
	 * @param commit True to commit the movement
	 * @returns True if the camera has moved (for consistency with MouseControl).
	 */
	Zoom(zoom: number, commit: boolean = true): boolean
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
		this.m_fovY = Math_.Clamp(this.m_fovY, Math_.Tiny, Math_.TauBy2 - Math_.Tiny);

		// Set the base values
		if (commit)
			this.Commit();

		this.m_moved = true;
		return this.m_moved;
	}

	/**
	 * Return a point in world space corresponding to a normalised screen space point.
	 * The x,y components of 'nss_point' should be in normalised screen space, i.e. (-1,-1)->(1,1)
	 * The z component should be the depth into the screen (i.e. d*-c2w.z, where 'd' is typically positive)
	 * @param nss_point The normalised screen space point to convert to world space
	 */
	NSSPointToWSPoint(nss_point: Vec4): Vec4
	{
		let half_height = this.focus_dist * Math.tan(this.fovY * 0.5);

		// Calculate the point in camera space
		let point = Vec4_.create();
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
		return M4x4_.MulMV(this.c2w, point); // camera space to world space
	}

	/**
	 * Return a point in normalised screen space corresponding to 'ws_point'
	 * The returned 'z' component will be the world space distance from the camera.
	 * @param ws_point The world space point to convert to normalised screen space.
	 */
	WSPointToNSSPoint(ws_point: Vec4): Vec4
	{
		let half_height = this.focus_dist * Math.tan(this.fovY * 0.5);

		// Get the point in camera space and project into normalised screen space
		let cam = M4x4_.MulMV(this.w2c, ws_point);

		let point = Vec4_.create();
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
	 * @param nss_point The normalised screen space point to convert to a world space ray
	 */
	NSSPointToWSRay(nss_point: Vec4): { ws_point: Vec4, ws_direction: Vec4 }
	{
		let pt = this.NSSPointToWSPoint(nss_point);
		let ws_point = M4x4_.GetW(this.c2w);
		let ws_direction = Vec4_.Normalise(Vec4_.Sub(pt, ws_point));
		return { ws_point: ws_point, ws_direction: ws_direction };
	}

	/**
	 * Set the current position, FOV, and focus distance as the position reference.
	 */
	Commit(): void
	{
		M4x4_.Orthonorm(this.m_c2w, this.m_c2w)
		M4x4_.clone(this.m_c2w, this.m_base_c2w);
		this.m_base_fovY = this.m_fovY;
		this.m_base_focus_dist = this.m_focus_dist;
	}

	/**
	 * Revert navigation back to the last commit
	 */
	Revert(): void
	{
		M4x4_.clone(this.m_base_c2w, this.m_c2w);
		this.m_fovY = this.m_base_fovY;
		this.m_focus_dist = this.m_base_focus_dist;
		this.m_moved = true;
	}
}

/**
 * Create a Camera instance
 * @param fovY The field of view in the vertical direction (in radians)
 * @param aspect The aspect ratio of the view
 * @param focus_distance The distance to the focus point (in world space)
 * @param near The distance to the near clip plane (in world space)
 * @param far The distance to the far clip plane (in world space)
 * @param orthographic True for orthographic projection, false for perspective
 */
export function Create(fovY: number = Math_.TauBy8, aspect: number = 1.0, focus_distance: number = 1.0, near: number = 0.01, far: number = 100.0, orthographic: boolean = false): ICamera
{
	return new CameraImpl(fovY, aspect, focus_distance, near, far, orthographic);
}

/**
 * Create a deep copy of 'camera'
 * @param camera The camera to be cloned
 */
export function clone(camera: ICamera): ICamera
{
	return camera.clone();
}