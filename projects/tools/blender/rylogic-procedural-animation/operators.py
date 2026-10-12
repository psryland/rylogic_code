"""Operators for authoring, checking and exporting a rig description."""

import subprocess
import tempfile
from pathlib import Path

import bpy
from bpy.props import EnumProperty, StringProperty
from bpy.types import Operator
from bpy_extras.io_utils import ExportHelper

from . import propose, rig_desc, validate

# The result of the most recent validation, shown in the Rig panel
LAST_REPORT: list[str] = []


def ActiveArmature(context: bpy.types.Context) -> bpy.types.Object | None:
	"""Return the active object if it is an armature."""
	obj = context.active_object
	return obj if obj is not None and obj.type == "ARMATURE" else None


def WriteRig(armature_obj: bpy.types.Object, path: Path) -> None:
	"""Build, check and write the '.rig' description of 'armature_obj'. Raises ValueError listing every problem if the description is invalid."""
	desc = rig_desc.BuildRigDesc(armature_obj)
	errors = validate.ValidateRigDesc(desc)
	if errors:
		raise ValueError(f"Rig '{armature_obj.name}' is invalid:\n  " + "\n  ".join(errors))

	rig_desc.WriteRigDesc(desc, path)


def _SetReport(lines: list[str]) -> None:
	"""Replace the report shown in the Rig panel."""
	LAST_REPORT.clear()
	LAST_REPORT.extend(lines)


class _ArmatureOperator(Operator):
	"""Base class for operators that act on the active armature."""

	@classmethod
	def poll(cls, context):
		return ActiveArmature(context) is not None


class RYLOGIC_OT_rig_propose(_ArmatureOperator):
	"""Replace the chains, contacts and chain joint limits with a proposal based on bone names and topology"""

	bl_idname = "rylogic_rig.propose"
	bl_label = "Auto-propose"
	bl_options = {"REGISTER", "UNDO"}

	def invoke(self, context, event):
		return context.window_manager.invoke_confirm(self, event)

	def execute(self, context):
		# Propose, then check the result so that problems are visible straight away
		armature_obj = ActiveArmature(context)
		try:
			propose.ProposeRig(armature_obj)
		except ValueError as error:
			self.report({"ERROR"}, str(error))
			return {"CANCELLED"}

		rig = armature_obj.data.rylogic_rig
		errors = validate.ValidateRigDesc(rig_desc.BuildRigDesc(armature_obj))
		_SetReport(errors or ["Valid"])
		self.report({"WARNING"} if errors else {"INFO"}, f"Proposed {len(rig.chains)} chains and {len(rig.contacts)} contacts" + (f"; {len(errors)} problems" if errors else ""))
		return {"FINISHED"}


class RYLOGIC_OT_rig_validate(_ArmatureOperator):
	"""Check the rig description for the rules that the runtime relies on"""

	bl_idname = "rylogic_rig.validate"
	bl_label = "Validate"

	def execute(self, context):
		# Report each problem, or that the rig is valid
		try:
			errors = validate.ValidateRigDesc(rig_desc.BuildRigDesc(ActiveArmature(context)))
		except ValueError as error:
			errors = [str(error)]

		_SetReport(errors or ["Valid"])
		for error in errors:
			self.report({"WARNING"}, error)

		self.report({"ERROR"} if errors else {"INFO"}, f"{len(errors)} problems" if errors else "The rig is valid")
		return {"FINISHED"}


class RYLOGIC_OT_rig_validate_native(_ArmatureOperator):
	"""Export the armature's skinned meshes and rig to a temporary folder and check them with the p3d tool's canonical '-Rig' command"""

	bl_idname = "rylogic_rig.validate_native"
	bl_label = "Validate (native)"

	def execute(self, context):
		# The p3d tool is configured in the add-on preferences
		p3d_path = Path(context.preferences.addons[__package__].preferences.p3d_path)
		if not p3d_path.is_file():
			self.report({"ERROR"}, "Set the p3d tool path in the add-on preferences")
			return {"CANCELLED"}

		armature_obj = ActiveArmature(context)
		with tempfile.TemporaryDirectory(prefix="rylogic_rig_") as folder:
			# Export the armature and the meshes it deforms as a skinned model
			folder = Path(folder)
			try:
				_ExportSkinnedGlb(context, armature_obj, folder / "model.glb")
				rig_desc.WriteRigDesc(rig_desc.BuildRigDesc(armature_obj), folder / "in.rig")
			except (ValueError, RuntimeError) as error:
				self.report({"ERROR"}, str(error))
				return {"CANCELLED"}

			# Cook the rig against the model's skeleton
			result = subprocess.run(
				[str(p3d_path), "-fi", str(folder / "model.glb"), "-Rig", str(folder / "in.rig"), str(folder / "out.rig"), "-fo", str(folder / "model.p3d")],
				capture_output=True, text=True,
			)

		# Report the tool's output
		output = [line for line in (result.stdout + result.stderr).splitlines() if line.strip()]
		_SetReport(["Valid (native)"] if result.returncode == 0 else output or [f"p3d failed with exit code {result.returncode}"])
		for line in output if result.returncode != 0 else []:
			self.report({"WARNING"}, line)

		self.report({"INFO"} if result.returncode == 0 else {"ERROR"}, "The rig is valid (native)" if result.returncode == 0 else "Native validation failed")
		return {"FINISHED"}


class RYLOGIC_OT_rig_export(_ArmatureOperator, ExportHelper):
	"""Write the rig description to a '.rig' file"""

	bl_idname = "rylogic_rig.export"
	bl_label = "Export .rig"

	filename_ext = ".rig"
	filter_glob: StringProperty(default="*.rig", options={"HIDDEN"})

	def execute(self, context):
		# Refuse to write an invalid rig
		try:
			WriteRig(ActiveArmature(context), Path(self.filepath))
		except ValueError as error:
			self.report({"ERROR"}, str(error))
			return {"CANCELLED"}

		self.report({"INFO"}, f"Wrote {self.filepath}")
		return {"FINISHED"}


class RYLOGIC_OT_rig_item_add(_ArmatureOperator):
	"""Add a chain or contact"""

	bl_idname = "rylogic_rig.item_add"
	bl_label = "Add"
	bl_options = {"REGISTER", "UNDO"}

	collection: EnumProperty(items=[("CHAINS", "Chains", ""), ("CONTACTS", "Contacts", "")])

	def execute(self, context):
		# Append a new item and make it active
		rig = ActiveArmature(context).data.rylogic_rig
		match self.collection:
			case "CHAINS":
				rig.chains.add()
				rig.active_chain = len(rig.chains) - 1
			case "CONTACTS":
				contact = rig.contacts.add()
				contact.name = f"contact.{len(rig.contacts)}"
				rig.active_contact = len(rig.contacts) - 1
			case _:
				raise ValueError(f"Unknown collection '{self.collection}'")

		return {"FINISHED"}


class RYLOGIC_OT_rig_item_remove(_ArmatureOperator):
	"""Remove the active chain or contact"""

	bl_idname = "rylogic_rig.item_remove"
	bl_label = "Remove"
	bl_options = {"REGISTER", "UNDO"}

	collection: EnumProperty(items=[("CHAINS", "Chains", ""), ("CONTACTS", "Contacts", "")])

	def execute(self, context):
		# Remove the active item and keep the selection in range
		rig = ActiveArmature(context).data.rylogic_rig
		match self.collection:
			case "CHAINS":
				if 0 <= rig.active_chain < len(rig.chains):
					rig.chains.remove(rig.active_chain)
					rig.active_chain = max(0, min(rig.active_chain, len(rig.chains) - 1))
			case "CONTACTS":
				if 0 <= rig.active_contact < len(rig.contacts):
					rig.contacts.remove(rig.active_contact)
					rig.active_contact = max(0, min(rig.active_contact, len(rig.contacts) - 1))
			case _:
				raise ValueError(f"Unknown collection '{self.collection}'")

		return {"FINISHED"}


class RYLOGIC_OT_rig_chain_assign(_ArmatureOperator):
	"""Set the active chain's bones to the selected bones, ordered from base to end"""

	bl_idname = "rylogic_rig.chain_assign"
	bl_label = "Assign Selected Bones"
	bl_options = {"REGISTER", "UNDO"}

	def execute(self, context):
		# Order the selection by depth so that the chain runs from base to end; Validate reports a selection that is not a parent-child run
		armature = ActiveArmature(context).data
		rig = armature.rylogic_rig
		if not 0 <= rig.active_chain < len(rig.chains):
			self.report({"ERROR"}, "No active chain")
			return {"CANCELLED"}

		selected = [bone for bone in armature.bones if bone.select]
		selected.sort(key=lambda bone: len(bone.parent_recursive))
		chain = rig.chains[rig.active_chain]
		chain.bones.clear()
		for bone in selected:
			chain.bones.add().name = bone.name

		return {"FINISHED"}


def _ExportSkinnedGlb(context: bpy.types.Context, armature_obj: bpy.types.Object, path: Path) -> None:
	"""Export 'armature_obj' and the meshes it deforms as a Z-up skinned GLB, restoring the selection afterwards."""
	# The export uses the selection, so remember it
	meshes = [obj for obj in context.scene.objects if obj.type == "MESH" and any(mod.type == "ARMATURE" and mod.object == armature_obj for mod in obj.modifiers)]
	if not meshes:
		raise ValueError(f"No mesh is deformed by armature '{armature_obj.name}'")

	view_layer = context.view_layer
	saved_selection = [obj for obj in view_layer.objects if obj.select_get()]
	saved_active = view_layer.objects.active
	try:
		# Match the skinned-model export used by asset pipelines: Blender's Z-up axes, with skins
		for obj in saved_selection:
			obj.select_set(False)
		for obj in [armature_obj, *meshes]:
			obj.select_set(True)

		view_layer.objects.active = armature_obj
		result = bpy.ops.export_scene.gltf(
			filepath=str(path), check_existing=False, export_format="GLB", use_selection=True,
			export_yup=False, export_skins=True, export_animations=False, export_materials="NONE", will_save_settings=False,
		)
		if result != {"FINISHED"}:
			raise RuntimeError(f"Blender glTF export did not finish: {result}")
	finally:
		# Restore the authoring selection even when the export fails
		for obj in view_layer.objects:
			obj.select_set(obj in saved_selection)

		view_layer.objects.active = saved_active


class RYLOGIC_AP_preferences(bpy.types.AddonPreferences):
	"""Add-on settings."""

	bl_idname = __package__

	p3d_path: StringProperty(name="p3d Tool", subtype="FILE_PATH", description="The p3d tool (p3d.exe), used by 'Validate (native)'")

	def draw(self, context):
		self.layout.prop(self, "p3d_path")


CLASSES = (
	RYLOGIC_OT_rig_propose,
	RYLOGIC_OT_rig_validate,
	RYLOGIC_OT_rig_validate_native,
	RYLOGIC_OT_rig_export,
	RYLOGIC_OT_rig_item_add,
	RYLOGIC_OT_rig_item_remove,
	RYLOGIC_OT_rig_chain_assign,
	RYLOGIC_AP_preferences,
)


def Register() -> None:
	"""Register the operators and preferences."""
	for cls in CLASSES:
		bpy.utils.register_class(cls)


def Unregister() -> None:
	"""Unregister the operators and preferences."""
	for cls in reversed(CLASSES):
		bpy.utils.unregister_class(cls)
