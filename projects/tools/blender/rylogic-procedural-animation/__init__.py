"""Rylogic Procedural Animation: author the rig description used by the 'pr::animation' procedural animation system.

A rig description names the solvable chains of an armature (spine, neck, legs, arms, tail, digits), each bone's joint limit, and the contact points that
are placed on surfaces. It is written as a '.rig' JSON file next to the skinned model, and cooked by the p3d tool's '-Rig' command, which is the canonical
validation. The schema is owned by 'pr/animation/rig_json.h' in the Rylogic code library.

Scripted use, e.g. from an asset exporter:
	addon = importlib.import_module("rylogic-procedural-animation")
	addon.register()  # the rig properties are read through registered property groups
	addon.WriteRig(armature_obj, path)
"""

bl_info = {
	"name": "Rylogic Procedural Animation",
	"author": "Paul Ryland",
	"version": (1, 0, 0),
	"blender": (4, 2, 0),
	"description": "Author and export rig descriptions for the Rylogic procedural animation system",
	"location": "Properties > Armature > Rylogic Rig, and Properties > Bone > Rylogic Rig Joint",
	"category": "Animation",
}

import bpy

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
