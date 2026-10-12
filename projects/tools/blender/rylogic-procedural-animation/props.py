"""Blender properties that hold a procedural-animation rig description.

Per-bone joint data lives on 'bpy.types.Bone.rylogic_rig'. Chains and contacts live on 'bpy.types.Armature.rylogic_rig'.
Bones are referred to by name. See 'rig_desc.py' for how these properties map to the '.rig' schema.
"""

import math
import bpy
from bpy.props import BoolProperty, CollectionProperty, EnumProperty, FloatProperty, FloatVectorProperty, IntProperty, PointerProperty, StringProperty
from bpy.types import PropertyGroup

# Enum items. The identifiers are the lower-case '.rig' names in upper case, so the export is a simple mapping.
LIMIT_KINDS = [
	("NONE", "None", "The joint can rotate freely"),
	("CONE", "Cone", "The bone stays within a cone about its rest direction"),
	("HINGE", "Hinge", "The bone rotates only about one axis, within an angle range"),
]
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


class RYLOGIC_PG_RigBone(PropertyGroup):
	"""The joint limit and stiffness of one bone."""

	limit_kind: EnumProperty(name="Limit", items=LIMIT_KINDS, default="NONE", description="How the bone's direction is limited relative to its parent")
	cone_angle: FloatProperty(name="Cone Angle", subtype="ANGLE", min=1e-4, max=math.pi, default=math.radians(30), description="The half-angle of the cone about the rest direction")
	hinge_axis: FloatVectorProperty(name="Hinge Axis", size=3, default=(1.0, 0.0, 0.0), description="The hinge axis in the bone's rest space. It should be perpendicular to the bone")
	hinge_min: FloatProperty(name="Hinge Min", subtype="ANGLE", min=-math.pi, max=math.pi, default=math.radians(-45), description="The minimum signed angle about the hinge axis from the rest direction")
	hinge_max: FloatProperty(name="Hinge Max", subtype="ANGLE", min=-math.pi, max=math.pi, default=math.radians(45), description="The maximum signed angle about the hinge axis from the rest direction")
	stiffness: FloatProperty(name="Stiffness", min=0.0, max=1.0, default=0.0, description="Tail bones only. How strongly the bone returns towards its rest direction")


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
	active_chain: IntProperty(name="Active Chain", default=0)
	contacts: CollectionProperty(name="Contacts", type=RYLOGIC_PG_RigContact)
	active_contact: IntProperty(name="Active Contact", default=0)
	show_overlay: BoolProperty(name="Show Overlay", default=True, description="Draw joint limits, poles, facing directions and contacts in the viewport")


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
