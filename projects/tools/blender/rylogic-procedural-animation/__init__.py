"""Rylogic Procedural Animation: author the rig description used by the 'pr::animation' procedural animation system.

A rig description names the solvable chains of an armature (spine, neck, legs, arms, tail, digits), each bone's joint limit, and the contact points that
are placed on surfaces. It is written as a '.rig' JSON file next to the skinned model, and cooked by the p3d tool's '-Rig' command, which is the canonical
validation. The schema is owned by 'pr/animation/rig_json.h' in the Rylogic code library.

Joint limits bound each bone's rotation from its rest pose as a swing of the bone's direction and a twist about it (see 'limits.py'). The swing range is
set separately about the bone's rest +X and +Z axes, so roll the bone to align those axes with the joint. A zero-width range on one swing axis makes a
hinge about the other. Limits can be set by hand in the Bone properties, or derived from poses:
	1. In the Rylogic Rig panel, pose the armature at the limit of its range of motion and press 'Capture Pose'. This keys every bone's rotation into
	   the range-of-motion action, creating it if needed. Repeat for each limit, e.g. legs fully forward, back, out and twisted. Posing one rotation
	   axis at a time for all limbs (R X X, R Z Z, R Y Y rotate about a bone's local axes) is easier than posing whole extreme poses. The order of the
	   keyframes does not matter.
	2. Press 'Derive Limits'. Each bone that moves in the captured poses gets the smallest limit that contains them all, widened by 'Padding'.
	   Bones that never move keep their limits. With 'Mirror Samples' on, the poses of either side also count, mirrored, for the other side, so a
	   single capture can pose the left leg forward and the right leg back. Turn it off for asymmetric rigs.
	3. Review the limits in the viewport overlay and adjust them in the Bone properties. 'Symmetrize Limits' copies the selected bones' limits,
	   mirrored, to their left/right partners.
Mirroring needs left and right bones whose names differ only by side and whose rest transforms are mirror images, as Blender's Symmetrize makes them.

Contacts are points on a bone, with an outward surface normal, that are placed on surfaces, e.g. the soles of the feet. Point the normal out of the
part of the body that touches the surface. Add a contact in the Rylogic Rig panel, then press 'Pick on Surface' and click the skinned mesh. The
contact moves to the clicked point on the bone that most deforms that face, with the face's normal. This works on a posed mesh. Esc or right-click
cancels.

Selecting a chain in the panel selects its bones. Selecting a bone makes a chain and a contact that use it active, unless the active ones already do.

The viewport overlay draws swing limits (orange), twist limits (yellow), limb poles (magenta), the neck's facing direction (sky blue) and contacts
(green). Limb poles and facing directions are fixed rest-pose model-space directions. The toggles under 'Show Overlay' hide each type. In Pose Mode
with bones selected, only the overlays of the selected bones are drawn.

Scripted use, e.g. from an asset exporter:
	addon = importlib.import_module("rylogic-procedural-animation")
	addon.register()  # the rig properties are read through registered property groups
	addon.WriteRig(armature_obj, path)
"""

bl_info = {
	"name": "Rylogic Procedural Animation",
	"author": "Paul Ryland",
	"version": (2, 0, 0),
	"blender": (4, 2, 0),
	"description": "Author and export rig descriptions for the Rylogic procedural animation system",
	"location": "Properties > Armature > Rylogic Rig, and Properties > Bone > Rylogic Rig Joint",
	"category": "Animation",
}

import bpy

# Blender's 'Reload Scripts' re-runs this module but keeps its submodules cached, so reload them too for edits to take effect without a restart.
# Dependencies come before the modules that import names from them.
if "operators" in locals():
	import importlib
	for _module in (limits, rig_desc, validate, props, operators, ui, draw):
		importlib.reload(_module)

from . import draw, operators, props, ui
from .operators import WriteRig
from .rig_desc import BuildRigDesc, WriteRigDesc
from .validate import ValidateRigDesc

__all__ = ["BuildRigDesc", "ValidateRigDesc", "WriteRig", "WriteRigDesc", "register", "unregister"]


def register() -> None:
	"""Register the add-on's properties, operators, panels and viewport overlay."""
	# The overlay needs a viewport, so it is skipped when Blender runs without a user interface
	props.Register()
	operators.Register()
	ui.Register()
	if not bpy.app.background:
		draw.Register()


def unregister() -> None:
	"""Unregister everything that 'register' added."""
	draw.Unregister()
	ui.Unregister()
	operators.Unregister()
	props.Unregister()
