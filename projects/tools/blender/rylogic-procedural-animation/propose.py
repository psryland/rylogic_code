"""Draft a rig description from an armature's bone names and topology.

The proposal is a starting point for authoring: review it in the viewport overlay and adjust it before export.
It finds the pelvis by name, then follows the hierarchy:
- The spine runs from the pelvis to the chest, which is the first centre bone with left/right children.
- The neck is the run of centre bones above the chest.
- The tail is a run of centre bones below the pelvis, preferring one whose name contains 'tail'.
- Each left/right branch from the pelvis is a leg and from the chest is an arm. A limb is the last three bones before the branch divides into digits.
- Each child run of a hand or foot is a digit.
Bones are assumed to face -Y with +Z up when the rest pose gives no better information.
"""

import math
import re
from dataclasses import dataclass, field

import bpy
from mathutils import Vector

from .rig_desc import ModelSpace

PELVIS_NAMES = ("hips", "pelvis", "hip")
SIDE_PATTERN = re.compile(r"(?:[._\-\s](?P<suffix>[LlRr])$)|(?:(?P<word>left|right))", re.IGNORECASE)


@dataclass
class ProposedChain:
	"""A chain found by the proposal."""

	role: str
	side: str
	bones: list[bpy.types.Bone]
	pole: Vector = field(default_factory=lambda: Vector((0, -1, 0)))
	forward: Vector = field(default_factory=lambda: Vector((0, -1, 0)))


def Side(bone: bpy.types.Bone) -> str:
	"""Return 'LEFT', 'RIGHT' or 'CENTRE' from the bone's name."""
	# Accept '.L', '_R', '-l' style suffixes, or 'left'/'right' anywhere in the name
	match = SIDE_PATTERN.search(bone.name)
	if match is None:
		return "CENTRE"

	letter = (match.group("suffix") or match.group("word"))[0].upper()
	return "LEFT" if letter == "L" else "RIGHT"


def ProposeRig(armature_obj: bpy.types.Object) -> None:
	"""Replace the armature's chains and contacts, and its chain bones' limits, with a proposal. Raises ValueError if no pelvis is found."""
	# Find the chains, then derive joint limits and contacts from them
	armature = armature_obj.data
	space = ModelSpace(armature_obj)
	chains = _FindChains(armature)
	_ProposeDirections(chains, armature)

	# Replace the stored chains
	rig = armature.rylogic_rig
	rig.chains.clear()
	for proposed in chains:
		chain = rig.chains.add()
		chain.role = proposed.role
		chain.side = proposed.side
		chain.pole = proposed.pole
		chain.forward = proposed.forward
		for bone in proposed.bones:
			chain.bones.add().name = bone.name

	rig.active_chain = 0

	# Give the solved chains default limits. Spine and neck bones bend a little at each joint, and the tail is loose but springs back to its rest shape.
	for proposed in chains:
		for bone in proposed.bones:
			joint = bone.rylogic_rig
			joint.stiffness = 0.0
			match proposed.role:
				case "SPINE":
					joint.limit_kind, joint.cone_angle = "CONE", math.radians(25)
				case "NECK":
					joint.limit_kind, joint.cone_angle = "CONE", math.radians(35)
				case "TAIL":
					joint.limit_kind, joint.cone_angle, joint.stiffness = "CONE", math.radians(20), 0.3
				case "LEG" | "ARM" | "DIGIT":
					joint.limit_kind = "NONE"
				case _:
					raise ValueError(f"Unknown chain role '{proposed.role}'")

	# Replace the stored contacts. Each faces down in the rest pose.
	rig.contacts.clear()
	for proposed in chains:
		match proposed.role:
			case "LEG":
				_AddContact(rig, space, proposed.bones[-1], "SOLE", 0.5)
			case "ARM":
				_AddContact(rig, space, proposed.bones[-1], "PALM", 0.5)
			case "DIGIT":
				parent_chain = next((c for c in chains if c.bones[-1] == proposed.bones[0].parent), None)
				if parent_chain is not None:
					_AddContact(rig, space, proposed.bones[-1], "TOE" if parent_chain.role == "LEG" else "FINGER", 1.0)
			case "TAIL":
				for bone in proposed.bones:
					_AddContact(rig, space, bone, "TAIL", 0.5)
			case "SPINE" | "NECK":
				pass
			case _:
				raise ValueError(f"Unknown chain role '{proposed.role}'")

	rig.active_contact = 0


def _FindChains(armature: bpy.types.Armature) -> list[ProposedChain]:
	"""Find the chains of a vertebrate-like armature."""
	# The pelvis anchors the search
	pelvis = next((bone for bone in armature.bones if bone.name.lower() in PELVIS_NAMES), None)
	if pelvis is None:
		raise ValueError(f"No pelvis bone found. Name it one of: {', '.join(PELVIS_NAMES)}")

	chains: list[ProposedChain] = []
	centre_children = [child for child in pelvis.children if Side(child) == "CENTRE"]

	# The tail is a centre run with no side branches, preferring one named 'tail'
	tail_root = next((child for child in centre_children if "tail" in child.name.lower()), None)
	if tail_root is None:
		tail_root = next((child for child in centre_children if not _HasSideDescendant(child)), None)
	if tail_root is not None:
		chains.append(ProposedChain("TAIL", "CENTRE", _SingleRun(tail_root)))

	# The spine runs up to the chest, which is the first centre bone with side children
	spine = [pelvis]
	spine_root = next((child for child in centre_children if child != tail_root and _HasSideDescendant(child)), None)
	bone = spine_root
	while bone is not None:
		spine.append(bone)
		if any(Side(child) != "CENTRE" for child in bone.children):
			break
		bone = next((child for child in bone.children if Side(child) == "CENTRE"), None)

	chains.append(ProposedChain("SPINE", "CENTRE", spine))
	chest = spine[-1]

	# The neck is the run of centre bones above the chest
	neck_root = next((child for child in chest.children if Side(child) == "CENTRE"), None) if chest != pelvis else None
	if neck_root is not None:
		chains.append(ProposedChain("NECK", "CENTRE", _SingleRun(neck_root)))

	# Legs branch from the pelvis and arms from the chest. Each limb ends where it divides into digits.
	for parent, role in ((pelvis, "LEG"), (chest, "ARM")):
		if role == "ARM" and chest == pelvis:
			continue
		for limb_root in (child for child in parent.children if Side(child) != "CENTRE"):
			run = _SingleRun(limb_root)
			if len(run) < 3:
				continue
			chains.append(ProposedChain(role, Side(limb_root), run[-3:]))
			for digit_root in run[-1].children:
				chains.append(ProposedChain("DIGIT", Side(limb_root), _SingleRun(digit_root)))

	return chains


def _ProposeDirections(chains: list[ProposedChain], armature: bpy.types.Armature) -> None:
	"""Set limb poles and the neck's facing direction, in armature space."""
	# The body faces from the pelvis towards the chest, levelled. An upright or missing spine falls back to Blender's front view (-Y).
	spine = next(chain for chain in chains if chain.role == "SPINE")
	body = spine.bones[-1].head_local - spine.bones[0].head_local
	forward = Vector((body.x, body.y, 0))
	forward = forward.normalized() if forward.length > 0.25 * body.length and body.length > 0 else Vector((0, -1, 0))

	for chain in chains:
		match chain.role:
			case "LEG" | "ARM":
				# Bend the way the rest pose bends; a straight limb bends knees forward and elbows back
				a, b, c = (bone.head_local for bone in chain.bones)
				axis = (c - a).normalized()
				bend = (b - a) - axis * (b - a).dot(axis)
				if bend.length <= 0.02 * (c - a).length:
					bend = forward if chain.role == "LEG" else -forward
					bend = bend - axis * bend.dot(axis)
				if bend.length <= 1e-6:
					bend = axis.orthogonal()
				chain.pole = bend.normalized()
			case "NECK":
				# The head faces the body's forward direction
				chain.forward = forward
			case "SPINE" | "TAIL" | "DIGIT":
				pass
			case _:
				raise ValueError(f"Unknown chain role '{chain.role}'")


def _AddContact(rig, space: ModelSpace, bone: bpy.types.Bone, kind: str, along: float) -> None:
	"""Add a downward-facing contact at fraction 'along' of the bone's length."""
	# Express the model-space down direction in the bone's rest space
	contact = rig.contacts.add()
	contact.name = f"{kind.lower()}.{bone.name}"
	contact.bone = bone.name
	contact.kind = kind
	contact.offset = (0.0, bone.length * along, 0.0)
	contact.normal = (space.BoneRest(bone).to_quaternion().inverted() @ Vector((0, 0, -1))).normalized()


def _SingleRun(bone: bpy.types.Bone) -> list[bpy.types.Bone]:
	"""Return 'bone' and its descendants while each has exactly one child, ending at the bone that branches or ends."""
	run = [bone]
	while len(run[-1].children) == 1:
		run.append(run[-1].children[0])

	return run


def _HasSideDescendant(bone: bpy.types.Bone) -> bool:
	"""Report whether any descendant of 'bone' is a left or right bone."""
	return any(Side(child) != "CENTRE" for child in bone.children_recursive)
