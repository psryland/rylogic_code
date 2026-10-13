"""Live pinning: hold selected bones still while the frame changes, bending the chains above them with IK, and key the result with Auto Key.

Pinning keeps a planted foot or hand still while the body moves over it. While 'Live Pin' is on in Pose Mode, every selected bone that is in one of
the rig's chains, below its first bone, is pinned: it holds the armature-space pose that it had when it was selected, and the bones of its chain
above it bend with IK to reach it. Selected bones below another selected bone, such as the fingers of a pinned hand, are not pinned because they move
with it. Changing the selection changes the pins. On each frame change with Auto Key on, the pinned bones and their chains are keyed with their
solved pose, so stepping through the frames records the planted pose.

A pin is a temporary empty at the pinned pose, a 'Copy Transforms' constraint on the pinned bone and an 'IK' constraint on the bone above it. They
are removed when the pin is released, and while the file is saved.
"""

import bpy
from bpy.app.handlers import persistent

from . import props
from .gait_template import RotationPath

# The names of the pin constraints. Blender adds a suffix to repeated names, so they are matched by prefix.
PIN_HOLD = "Rylogic Pin"
PIN_IK = "Rylogic Pin IK"

# The pins removed while saving, to restore afterwards: (armature object name, bone name, target world matrix)
_SAVED_PINS: list[tuple[str, str, object]] = []


def PinnableBones(armature_obj: bpy.types.Object, names: list[str]) -> dict[str, list[str]]:
	"""Return the bones of 'names' that can be pinned, each with the chain bones above it that bend to reach it, base first.

	Bones below another bone of 'names' are left out because they move with it, as are bones that are not in a chain below its first bone.
	"""
	bones = armature_obj.data.bones
	pinnable: dict[str, list[str]] = {}
	for name in names:
		if any(parent.name in names for parent in bones[name].parent_recursive):
			continue

		chain = _ChainAbove(armature_obj.data, name)
		if chain:
			pinnable[name] = chain

	return pinnable


def PinnedBones(armature_obj: bpy.types.Object) -> list[str]:
	"""Return the names of the armature's pinned bones."""
	return [pose_bone.name for pose_bone in armature_obj.pose.bones if _Hold(pose_bone) is not None]


def Sync(armature_obj: bpy.types.Object) -> None:
	"""Pin the armature's pinnable selected bones while 'Live Pin' is on in Pose Mode, and release every other pin."""
	# Only change the pins when the wanted set differs, because changing constraints triggers another scene update
	live = armature_obj.data.rylogic_rig.pin_live and armature_obj.mode == "POSE"
	wanted = PinnableBones(armature_obj, props.SelectedBoneNames(armature_obj)) if live else {}
	pinned = set(PinnedBones(armature_obj))
	if pinned == wanted.keys():
		return

	for name in pinned - wanted.keys():
		_Unpin(armature_obj, name)
	for name in wanted.keys() - pinned:
		_Pin(armature_obj, name, wanted[name], armature_obj.matrix_world @ armature_obj.pose.bones[name].matrix)

	# Key new pins where they start, so that the keys of later frames do not change the pose at this one
	if bpy.context.scene.tool_settings.use_keyframe_insert_auto:
		KeyPins(armature_obj, bpy.context.scene.frame_current, wanted.keys() - pinned)


def KeyPins(armature_obj: bpy.types.Object, frame: int, names=None) -> None:
	"""Key the solved pose of the armature's pinned bones 'names', or all of them, and the chain bones above them at 'frame'."""
	# Visual keys record the pose with the pin constraints applied, so the pose remains once the pins are released
	for name in PinnedBones(armature_obj) if names is None else names:
		for bone in [name, *_PinChain(armature_obj, name)]:
			pose_bone = armature_obj.pose.bones[bone]
			pose_bone.keyframe_insert("location", frame=frame, group=bone, options={"INSERTKEY_VISUAL"})
			pose_bone.keyframe_insert(RotationPath(pose_bone), frame=frame, group=bone, options={"INSERTKEY_VISUAL"})


def _ChainAbove(armature: bpy.types.Armature, name: str) -> list[str]:
	"""Return the bones from the base of the rig chain containing 'name' down to the bone above it, or an empty list if there are none."""
	# Prefer the longest chain, so a bone in a short and a long chain bends the most joints
	found: list[str] = []
	for chain in armature.rylogic_rig.chains:
		bones = [ref.name for ref in chain.bones]
		if name in bones and bones.index(name) > len(found):
			found = bones[:bones.index(name)]

	return found


def _Hold(pose_bone: bpy.types.PoseBone) -> bpy.types.Constraint | None:
	"""The pin constraint that holds 'pose_bone', if it is pinned."""
	return next((c for c in pose_bone.constraints if c.type == "COPY_TRANSFORMS" and c.name.startswith(PIN_HOLD)), None)


def _PinIk(armature_obj: bpy.types.Object, name: str) -> tuple[bpy.types.PoseBone, bpy.types.Constraint] | None:
	"""The pose bone and IK constraint that bend the chain above the pinned bone 'name'."""
	target = _Hold(armature_obj.pose.bones[name]).target
	for pose_bone in armature_obj.pose.bones:
		for constraint in pose_bone.constraints:
			if constraint.type == "IK" and constraint.name.startswith(PIN_IK) and constraint.target == target:
				return pose_bone, constraint

	return None


def _PinChain(armature_obj: bpy.types.Object, name: str) -> list[str]:
	"""The names of the chain bones that bend to reach the pinned bone 'name'."""
	found = _PinIk(armature_obj, name)
	if found is None:
		return []

	pose_bone, constraint = found
	return [pose_bone.name, *(parent.name for parent in pose_bone.parent_recursive[:constraint.chain_count - 1])]


def _Pin(armature_obj: bpy.types.Object, name: str, chain: list[str], world: object) -> None:
	"""Pin the bone 'name' at the world matrix 'world', bending the bones of 'chain' to reach it."""
	# The target shows where the bone is pinned, but cannot be selected, so clicking near it does not leave Pose Mode
	target = bpy.data.objects.new(f"_rylogic_pin.{armature_obj.name}.{name}", None)
	bpy.context.scene.collection.objects.link(target)
	target.empty_display_type = "SPHERE"
	target.empty_display_size = 0.15 * armature_obj.data.bones[name].length
	target.hide_select = True
	target.matrix_world = world

	# The chain's last bone reaches the pinned bone's head, then the pinned bone copies the pinned pose
	ik = armature_obj.pose.bones[chain[-1]].constraints.new("IK")
	ik.name, ik.target, ik.chain_count = PIN_IK, target, len(chain)
	hold = armature_obj.pose.bones[name].constraints.new("COPY_TRANSFORMS")
	hold.name, hold.target = PIN_HOLD, target


def _Unpin(armature_obj: bpy.types.Object, name: str) -> None:
	"""Release the pinned bone 'name', removing its constraints and target."""
	found = _PinIk(armature_obj, name)
	if found is not None:
		found[0].constraints.remove(found[1])

	pose_bone = armature_obj.pose.bones[name]
	hold = _Hold(pose_bone)
	target = hold.target
	pose_bone.constraints.remove(hold)
	if target is not None:
		bpy.data.objects.remove(target)


@persistent
def _OnDepsgraphUpdate(scene, depsgraph) -> None:
	"""Update the active armature's pins, because selection changes are only visible as scene updates."""
	armature_obj = bpy.context.view_layer.objects.active
	if armature_obj is not None and armature_obj.type == "ARMATURE":
		Sync(armature_obj)


@persistent
def _OnFrameChange(scene, depsgraph) -> None:
	"""Key the pinned bones at the new frame when Auto Key is on, like Auto Key does for bones that the user moves."""
	if not scene.tool_settings.use_keyframe_insert_auto:
		return

	for obj in scene.objects:
		if obj.type == "ARMATURE":
			KeyPins(obj, scene.frame_current)


@persistent
def _OnSavePre(*_) -> None:
	"""Remove the pins while saving, so that saved files and the assets exported from them never contain the temporary constraints."""
	_SAVED_PINS.clear()
	for obj in bpy.data.objects:
		if obj.type == "ARMATURE":
			for name in PinnedBones(obj):
				_SAVED_PINS.append((obj.name, name, _Hold(obj.pose.bones[name]).target.matrix_world.copy()))
				_Unpin(obj, name)


@persistent
def _OnSavePost(*_) -> None:
	"""Restore the pins removed while saving."""
	for obj_name, name, world in _SAVED_PINS:
		obj = bpy.data.objects[obj_name]
		_Pin(obj, name, _ChainAbove(obj.data, name), world)

	_SAVED_PINS.clear()


_HANDLERS = (
	(bpy.app.handlers.depsgraph_update_post, _OnDepsgraphUpdate),
	(bpy.app.handlers.frame_change_post, _OnFrameChange),
	(bpy.app.handlers.save_pre, _OnSavePre),
	(bpy.app.handlers.save_post, _OnSavePost),
)


def Register() -> None:
	"""Attach the scene handlers that maintain and key the pins."""
	for handlers, handler in _HANDLERS:
		handlers.append(handler)


def Unregister() -> None:
	"""Detach the scene handlers."""
	for handlers, handler in _HANDLERS:
		handlers.remove(handler)
