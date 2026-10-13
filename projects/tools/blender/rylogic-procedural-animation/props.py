"""Blender properties that hold a procedural-animation rig description.

Per-bone joint data lives on 'bpy.types.Bone.rylogic_rig'. Chains and contacts live on 'bpy.types.Armature.rylogic_rig'.
Bones are referred to by name. See 'rig_desc.py' for how these properties map to the '.rig' schema.
"""

import math
import bpy
from bpy.props import BoolProperty, CollectionProperty, EnumProperty, FloatProperty, FloatVectorProperty, IntProperty, PointerProperty, StringProperty
from bpy.types import PropertyGroup

# Enum items. The identifiers are the lower-case '.rig' names in upper case, so the export is a simple mapping.
CHAIN_ROLES = [
	("SPINE", "Spine", "Pelvis to chest. Fitted as a smooth curve"),
	("NECK", "Neck", "Neck and head. Aimed at a look target"),
	("LEG", "Leg", "Upper leg, lower leg, foot. Solved with two-bone IK"),
	("ARM", "Arm", "Upper arm, forearm, hand. Solved with two-bone IK"),
	("TAIL", "Tail", "A flexible chain solved with FABRIK"),
	("DIGIT", "Digit", "A finger or toe. Posed but not solved"),
]
SIDES = [
	("CENTRE", "Centre", "On the centre line"),
	("LEFT", "Left", "On the left side"),
	("RIGHT", "Right", "On the right side"),
]
CONTACT_KINDS = [
	("SOLE", "Sole", "The underside of a foot"),
	("TOE", "Toe", "The tip of a toe"),
	("PALM", "Palm", "The underside of a hand"),
	("FINGER", "Finger", "The tip of a finger"),
	("TAIL", "Tail", "A point on the tail"),
]


def _MinAngle(name: str, default_degrees: float, description: str) -> FloatProperty:
	"""A range minimum. It is never positive, so every range contains the rest pose."""
	return FloatProperty(name=name, subtype="ANGLE", min=-math.pi, max=0.0, default=math.radians(default_degrees), description=description)


def _MaxAngle(name: str, default_degrees: float, description: str) -> FloatProperty:
	"""A range maximum. It is never negative, so every range contains the rest pose."""
	return FloatProperty(name=name, subtype="ANGLE", min=0.0, max=math.pi, default=math.radians(default_degrees), description=description)


class RYLOGIC_PG_RigBone(PropertyGroup):
	"""The joint limit and stiffness of one bone.

	The limit is the bone's rotation range from its rest pose, split into a swing and a twist (see 'JointLimit' in 'pr/animation/rig.h'). The twist is
	the rotation about the bone's own +Y axis. The swing moves the bone's +Y axis, and is measured about the bone's rest +X and +Z axes, so bone roll
	chooses the swing axes. A zero-width swing range on one axis makes a hinge about the other axis.
	"""

	use_limit: BoolProperty(name="Limit Rotation", default=False, description="Limit the bone's rotation from its rest pose. Without a limit the joint can rotate freely")
	swing_x_min: _MinAngle("Swing X Min", -45, "The most negative swing about the bone's rest +X axis")
	swing_x_max: _MaxAngle("Swing X Max", 45, "The most positive swing about the bone's rest +X axis")
	swing_z_min: _MinAngle("Swing Z Min", -45, "The most negative swing about the bone's rest +Z axis")
	swing_z_max: _MaxAngle("Swing Z Max", 45, "The most positive swing about the bone's rest +Z axis")
	twist_min: _MinAngle("Twist Min", -180, "The most negative twist about the bone's +Y axis")
	twist_max: _MaxAngle("Twist Max", 180, "The most positive twist about the bone's +Y axis")
	stiffness: FloatProperty(name="Stiffness", min=0.0, max=1.0, default=0.0, description="Tail bones only. How strongly the bone returns towards its rest direction")


def SelectBones(armature_obj: bpy.types.Object, names: list[str]) -> None:
	"""Select exactly the named bones of 'armature_obj', making the first one active. Uses edit bones in Edit Mode and pose bones otherwise."""
	# Edit Mode keeps its own bone selection, which is written back to the pose bones on leaving it
	armature = armature_obj.data
	wanted = set(names)
	if armature_obj.mode == "EDIT":
		for edit_bone in armature.edit_bones:
			edit_bone.select = edit_bone.select_head = edit_bone.select_tail = edit_bone.name in wanted
		if names and names[0] in armature.edit_bones:
			armature.edit_bones.active = armature.edit_bones[names[0]]
	else:
		for pose_bone in armature_obj.pose.bones:
			pose_bone.select = pose_bone.name in wanted
		if names and names[0] in armature.bones:
			armature.bones.active = armature.bones[names[0]]


def SelectedBoneNames(armature_obj: bpy.types.Object) -> list[str]:
	"""Return the names of the selected bones of 'armature_obj'. Uses edit bones in Edit Mode and pose bones otherwise."""
	if armature_obj.mode == "EDIT":
		return [edit_bone.name for edit_bone in armature_obj.data.edit_bones if edit_bone.select]

	return [pose_bone.name for pose_bone in armature_obj.pose.bones if pose_bone.select]


def _OnActiveChainChanged(rig: "RYLOGIC_PG_Rig", context: bpy.types.Context) -> None:
	"""Select the bones of the newly active chain, so that the chain is visible in the viewport."""
	# Only act on the armature whose list was clicked
	armature_obj = context.active_object
	if armature_obj is None or armature_obj.data != rig.id_data or not 0 <= rig.active_chain < len(rig.chains):
		return

	SelectBones(armature_obj, [ref.name for ref in rig.chains[rig.active_chain].bones])


class RYLOGIC_PG_RigBoneRef(PropertyGroup):
	"""A reference to a bone by name. The name is the built-in 'name' property."""


class RYLOGIC_PG_RigChain(PropertyGroup):
	"""A run of parent-child bones that is solved as one unit."""

	role: EnumProperty(name="Role", items=CHAIN_ROLES, default="SPINE")
	side: EnumProperty(name="Side", items=SIDES, default="CENTRE")
	bones: CollectionProperty(name="Bones", type=RYLOGIC_PG_RigBoneRef, description="Bones from base to end. Each bone is the parent of the next")
	active_bone: IntProperty(name="Active Bone", default=0)
	pole: FloatVectorProperty(name="Pole", size=3, default=(0.0, -1.0, 0.0), description="Leg and arm only. The rest-pose armature-space direction that the middle joint bends towards")
	forward: FloatVectorProperty(name="Forward", size=3, default=(0.0, -1.0, 0.0), description="Neck only. The rest-pose armature-space direction that the last bone faces")


class RYLOGIC_PG_RigContact(PropertyGroup):
	"""A named point on a bone that can be placed on a surface. The name is the built-in 'name' property."""

	bone: StringProperty(name="Bone", description="The bone that the contact is attached to")
	kind: EnumProperty(name="Kind", items=CONTACT_KINDS, default="SOLE")
	offset: FloatVectorProperty(name="Offset", size=3, subtype="TRANSLATION", default=(0.0, 0.0, 0.0), description="The contact point in the bone's rest space")
	normal: FloatVectorProperty(name="Normal", size=3, default=(0.0, 0.0, -1.0), description="The outward surface direction in the bone's rest space")


class RYLOGIC_PG_Rig(PropertyGroup):
	"""The chains and contacts of an armature."""

	chains: CollectionProperty(name="Chains", type=RYLOGIC_PG_RigChain)
	active_chain: IntProperty(name="Active Chain", default=0, update=_OnActiveChainChanged, description="The chain shown below. Selecting a chain selects its bones")
	contacts: CollectionProperty(name="Contacts", type=RYLOGIC_PG_RigContact)
	active_contact: IntProperty(name="Active Contact", default=0)
	show_overlay: BoolProperty(name="Show Overlay", default=True, description="Draw joint limits, poles, facing directions and contacts in the viewport")
	rom_action: PointerProperty(name="Range of Motion", type=bpy.types.Action, description="An action whose keyframes pose the armature at the extremes of each joint's range of motion")
	rom_mirror: BoolProperty(name="Mirror Samples", default=True, description="Derive the same, mirrored, limits for left and right bones from the poses of either side. Turn off for asymmetric rigs")
	rom_padding: FloatProperty(name="Padding", subtype="ANGLE", min=0.0, max=math.radians(45), default=math.radians(5), description="Extra range added beyond the posed extremes when deriving limits")


CLASSES = (
	RYLOGIC_PG_RigBone,
	RYLOGIC_PG_RigBoneRef,
	RYLOGIC_PG_RigChain,
	RYLOGIC_PG_RigContact,
	RYLOGIC_PG_Rig,
)


def Register() -> None:
	"""Register the property groups and attach them to bones and armatures."""
	for cls in CLASSES:
		bpy.utils.register_class(cls)

	bpy.types.Bone.rylogic_rig = PointerProperty(type=RYLOGIC_PG_RigBone)
	bpy.types.Armature.rylogic_rig = PointerProperty(type=RYLOGIC_PG_Rig)


def Unregister() -> None:
	"""Detach and unregister the property groups. Stored values remain in the .blend file."""
	del bpy.types.Armature.rylogic_rig
	del bpy.types.Bone.rylogic_rig
	for cls in reversed(CLASSES):
		bpy.utils.unregister_class(cls)
