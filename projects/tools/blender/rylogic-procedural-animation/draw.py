"""Viewport overlay that draws joint limits, limb poles, neck facing directions and contacts.

Limits are drawn about each bone's rest direction under its parent's current pose, which is how the solver applies them, so the overlay stays correct
while the armature is posed. The swing limit is the outline that the bone's tip may reach, with spokes to its extremes on the bone's X and Z axes. The
twist limit is an arc about the bone, measured from the bone's rest +X axis.
"""

import math

import bpy
import gpu
from gpu_extras.batch import batch_for_shader
from mathutils import Matrix, Vector

from .limits import SwingRotation

COLOUR_SWING = (1.0, 0.45, 0.1, 1.0)
COLOUR_TWIST = (1.0, 0.85, 0.1, 1.0)
COLOUR_POLE = (0.9, 0.2, 0.9, 1.0)
COLOUR_FORWARD = (0.1, 0.85, 0.95, 1.0)
COLOUR_CONTACT = (0.2, 0.95, 0.3, 1.0)
SEGMENTS = 24

_handle = None


def _Draw() -> None:
	"""Draw the overlay for every visible armature that has it enabled."""
	# Gather line segments per colour, then draw each colour as one batch
	lines: dict[tuple, list[Vector]] = {}
	for obj in bpy.context.visible_objects:
		if obj.type == "ARMATURE" and obj.mode != "EDIT" and obj.data.rylogic_rig.show_overlay:
			_AddArmature(obj, lines)

	if not lines:
		return

	shader = gpu.shader.from_builtin("UNIFORM_COLOR")
	gpu.state.line_width_set(2.0)
	gpu.state.depth_test_set("NONE")
	for colour, points in lines.items():
		batch = batch_for_shader(shader, "LINES", {"pos": points})
		shader.uniform_float("color", colour)
		batch.draw(shader)

	gpu.state.line_width_set(1.0)


def _AddArmature(obj: bpy.types.Object, lines: dict[tuple, list[Vector]]) -> None:
	"""Add the overlay lines for one armature object, in world space."""
	# In Pose Mode with bones selected, show only the overlays that belong to the selected bones, so that one joint can be inspected without clutter
	world = obj.matrix_world
	pose = obj.pose
	rig = obj.data.rylogic_rig
	selected = {pose_bone.name for pose_bone in pose.bones if pose_bone.select} if obj.mode == "POSE" else set()
	def Shown(names) -> bool:
		return not selected or any(name in selected for name in names)

	# Joint limits, about each bone's rest direction under its parent's current frame
	for pose_bone in pose.bones:
		bone = pose_bone.bone
		joint = bone.rylogic_rig
		if not rig.show_limits or not joint.use_limit or not Shown([pose_bone.name]):
			continue

		parent_frame = world @ pose_bone.parent.matrix if pose_bone.parent else world
		parent_rest = bone.parent.matrix_local if bone.parent else Matrix.Identity(4)
		frame = (parent_frame @ parent_rest.inverted() @ bone.matrix_local).normalized()
		apex = (world @ pose_bone.matrix).to_translation()
		length = bone.length * world.to_scale().y
		if not (_IsFull(joint.swing_x_min, joint.swing_x_max) and _IsFull(joint.swing_z_min, joint.swing_z_max)):
			_AddSwing(lines.setdefault(COLOUR_SWING, []), apex, frame.to_3x3(), joint, length)
		if not _IsFull(joint.twist_min, joint.twist_max):
			_AddTwist(lines.setdefault(COLOUR_TWIST, []), apex, frame.to_3x3(), joint.twist_min, joint.twist_max, length)

	# Limb poles from the middle joint, and the neck's facing direction from its last bone
	rotation = world.to_quaternion()
	for chain in rig.chains:
		bones = [pose.bones.get(ref.name) for ref in chain.bones]
		if not bones or any(bone is None for bone in bones) or not Shown(ref.name for ref in chain.bones):
			continue

		match chain.role:
			case "LEG" | "ARM" if rig.show_poles and len(bones) == 3:
				start = (world @ bones[1].matrix).to_translation()
				size = sum(bone.bone.length for bone in bones) * 0.3 * world.to_scale().y
				lines.setdefault(COLOUR_POLE, []).extend([start, start + (rotation @ Vector(chain.pole)).normalized() * size])
			case "NECK" if rig.show_facing:
				start = (world @ bones[-1].matrix).to_translation()
				size = sum(bone.bone.length for bone in bones) * world.to_scale().y
				lines.setdefault(COLOUR_FORWARD, []).extend([start, start + (rotation @ Vector(chain.forward)).normalized() * size])
			case _:
				pass

	# Contacts as a small cross with a normal line
	for contact in rig.contacts if rig.show_contacts else []:
		pose_bone = pose.bones.get(contact.bone)
		if pose_bone is None or not Shown([contact.bone]):
			continue

		frame = world @ pose_bone.matrix
		point = frame @ Vector(contact.offset)
		normal = (frame.to_3x3() @ Vector(contact.normal)).normalized()
		size = pose_bone.bone.length * 0.25 * world.to_scale().y
		side = normal.orthogonal().normalized() * size * 0.3
		other = normal.cross(side)
		lines.setdefault(COLOUR_CONTACT, []).extend([point, point + normal * size, point - side, point + side, point - other, point + other])


def _IsFull(low: float, high: float) -> bool:
	"""Report whether a range allows every angle, so that it needs no drawing."""
	return low <= -math.pi + 1e-4 and high >= math.pi - 1e-4


def _AddSwing(points: list[Vector], apex: Vector, frame: Matrix, joint, length: float) -> None:
	"""Add the outline that the bone's tip may reach under its swing limit, with spokes from the joint to the extremes of each swing axis.

	'frame' is the bone's rest orientation under its parent's current pose.
	"""
	# Walk around the swing ellipse. Each quadrant has its own radii, matching 'SwingLimit::Clamp' in 'pr/algorithm/fabrik.h'.
	def Tip(t: float) -> Vector:
		"""Return the bone tip swung to angle 't' around the boundary of the swing ellipse."""
		c, s = math.cos(t), math.sin(t)
		swing_x = c * (joint.swing_x_max if c >= 0 else -joint.swing_x_min)
		swing_z = s * (joint.swing_z_max if s >= 0 else -joint.swing_z_min)
		return apex + frame @ (SwingRotation(swing_x, swing_z) @ Vector((0, length, 0)))

	segments = 2 * SEGMENTS
	rim = [Tip(2 * math.pi * i / segments) for i in range(segments)]
	for i in range(segments):
		points.extend([rim[i], rim[(i + 1) % segments]])

	for i in range(0, segments, segments // 4):
		points.extend([apex, rim[i]])


def _AddTwist(points: list[Vector], apex: Vector, frame: Matrix, low: float, high: float, length: float) -> None:
	"""Add the twist range as an arc about the bone's rest direction, part way along the bone, with spokes to its ends.

	'frame' is the bone's rest orientation under its parent's current pose. Angles are measured from the bone's rest +X axis about its +Y axis.
	"""
	# Rotate the bone's +X axis about its +Y axis across the allowed range
	centre = apex + frame @ Vector((0, length * 0.3, 0))
	radius = length * 0.15
	arc = [centre + frame @ (Matrix.Rotation(low + (high - low) * i / SEGMENTS, 3, "Y") @ Vector((radius, 0, 0))) for i in range(SEGMENTS + 1)]
	for i in range(SEGMENTS):
		points.extend([arc[i], arc[i + 1]])

	points.extend([centre, arc[0], centre, arc[-1]])


def Register() -> None:
	"""Install the viewport draw handler."""
	global _handle
	_handle = bpy.types.SpaceView3D.draw_handler_add(_Draw, (), "WINDOW", "POST_VIEW")


def Unregister() -> None:
	"""Remove the viewport draw handler."""
	global _handle
	if _handle is not None:
		bpy.types.SpaceView3D.draw_handler_remove(_handle, "WINDOW")
		_handle = None
