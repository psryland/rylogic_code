"""Viewport overlay that draws joint limits, limb poles, neck facing directions and contacts.

Limits are drawn about each bone's rest direction under its parent's current pose, which is how the solver applies them, so the overlay stays correct
while the armature is posed.
"""

import math

import bpy
import gpu
from gpu_extras.batch import batch_for_shader
from mathutils import Matrix, Vector

COLOUR_CONE = (1.0, 0.85, 0.1, 1.0)
COLOUR_HINGE = (1.0, 0.45, 0.1, 1.0)
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
	# Joint limits, about each bone's rest direction under its parent's current frame
	world = obj.matrix_world
	pose = obj.pose
	for pose_bone in pose.bones:
		bone = pose_bone.bone
		joint = bone.rylogic_rig
		if joint.limit_kind == "NONE":
			continue

		parent_frame = world @ pose_bone.parent.matrix if pose_bone.parent else world
		parent_rest = bone.parent.matrix_local if bone.parent else Matrix.Identity(4)
		frame = (parent_frame @ parent_rest.inverted() @ bone.matrix_local).normalized()
		apex = (world @ pose_bone.matrix).to_translation()
		rest_dir = frame.to_3x3() @ Vector((0, 1, 0))
		length = bone.length * world.to_scale().y
		match joint.limit_kind:
			case "CONE":
				_AddCone(lines.setdefault(COLOUR_CONE, []), apex, rest_dir.normalized(), joint.cone_angle, length)
			case "HINGE":
				axis = (frame.to_3x3() @ Vector(joint.hinge_axis)).normalized()
				_AddArc(lines.setdefault(COLOUR_HINGE, []), apex, rest_dir.normalized(), axis, joint.hinge_min, joint.hinge_max, length)
			case _:
				pass

	# Limb poles from the middle joint, and the neck's facing direction from its last bone
	rig = obj.data.rylogic_rig
	rotation = world.to_quaternion()
	for chain in rig.chains:
		bones = [pose.bones.get(ref.name) for ref in chain.bones]
		if not bones or any(bone is None for bone in bones):
			continue

		match chain.role:
			case "LEG" | "ARM" if len(bones) == 3:
				start = (world @ bones[1].matrix).to_translation()
				size = sum(bone.bone.length for bone in bones) * 0.3 * world.to_scale().y
				lines.setdefault(COLOUR_POLE, []).extend([start, start + (rotation @ Vector(chain.pole)).normalized() * size])
			case "NECK":
				start = (world @ bones[-1].matrix).to_translation()
				size = sum(bone.bone.length for bone in bones) * world.to_scale().y
				lines.setdefault(COLOUR_FORWARD, []).extend([start, start + (rotation @ Vector(chain.forward)).normalized() * size])
			case _:
				pass

	# Contacts as a small cross with a normal line
	for contact in rig.contacts:
		pose_bone = pose.bones.get(contact.bone)
		if pose_bone is None:
			continue

		frame = world @ pose_bone.matrix
		point = frame @ Vector(contact.offset)
		normal = (frame.to_3x3() @ Vector(contact.normal)).normalized()
		size = pose_bone.bone.length * 0.25 * world.to_scale().y
		side = normal.orthogonal().normalized() * size * 0.3
		other = normal.cross(side)
		lines.setdefault(COLOUR_CONTACT, []).extend([point, point + normal * size, point - side, point + side, point - other, point + other])


def _AddCone(points: list[Vector], apex: Vector, axis: Vector, angle: float, length: float) -> None:
	"""Add a cone of half-angle 'angle' about 'axis' as a rim circle with four slant lines."""
	# The rim lies on a sphere of radius 'length' about the apex
	centre = apex + axis * length * math.cos(angle)
	radius = length * math.sin(angle)
	u = axis.orthogonal().normalized()
	v = axis.cross(u)
	rim = [centre + (u * math.cos(t) + v * math.sin(t)) * radius for t in (2 * math.pi * i / SEGMENTS for i in range(SEGMENTS))]
	for i in range(SEGMENTS):
		points.extend([rim[i], rim[(i + 1) % SEGMENTS]])

	for i in range(0, SEGMENTS, SEGMENTS // 4):
		points.extend([apex, rim[i]])


def _AddArc(points: list[Vector], apex: Vector, rest_dir: Vector, axis: Vector, min_angle: float, max_angle: float, length: float) -> None:
	"""Add a hinge's allowed range as an arc about 'axis', with lines to its ends and a short axis marker."""
	# Rotate the rest direction about the axis across the allowed range
	arc = [apex + Matrix.Rotation(min_angle + (max_angle - min_angle) * i / SEGMENTS, 3, axis) @ rest_dir * length for i in range(SEGMENTS + 1)]
	for i in range(SEGMENTS):
		points.extend([arc[i], arc[i + 1]])

	points.extend([apex, arc[0], apex, arc[-1], apex, apex + axis * length * 0.3])


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
