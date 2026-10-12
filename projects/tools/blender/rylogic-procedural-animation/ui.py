"""Properties-editor panels for the rig description."""

import bpy
from bpy.types import Panel, UIList

from . import operators


class RYLOGIC_UL_rig_chains(UIList):
	"""Lists an armature's chains."""

	def draw_item(self, context, layout, data, item, icon, active_data, active_property, index):
		# Summarise each chain by role, side and extent
		first = item.bones[0].name if item.bones else "-"
		last = item.bones[-1].name if item.bones else "-"
		layout.label(text=f"{item.role.title()} {item.side.title()}: {first} .. {last} ({len(item.bones)})", icon="LINKED")


class RYLOGIC_UL_rig_contacts(UIList):
	"""Lists an armature's contacts."""

	def draw_item(self, context, layout, data, item, icon, active_data, active_property, index):
		# Show the editable name with its kind and bone
		row = layout.row()
		row.prop(item, "name", text="", emboss=False, icon="SNAP_FACE")
		row.label(text=f"{item.kind.title()} on {item.bone}")


class RYLOGIC_PT_rig(Panel):
	"""The armature's procedural-animation rig: chains, contacts and tools."""

	bl_label = "Rylogic Rig"
	bl_space_type = "PROPERTIES"
	bl_region_type = "WINDOW"
	bl_context = "data"

	@classmethod
	def poll(cls, context):
		return operators.ActiveArmature(context) is not None

	def draw(self, context):
		# Tools that act on the whole rig
		layout = self.layout
		armature = context.active_object.data
		rig = armature.rylogic_rig
		row = layout.row(align=True)
		row.operator("rylogic_rig.propose", icon="LIGHT")
		row.operator("rylogic_rig.validate", icon="CHECKMARK")
		row.operator("rylogic_rig.validate_native", icon="CHECKMARK")
		row = layout.row(align=True)
		row.operator("rylogic_rig.export", icon="EXPORT")
		row.prop(rig, "show_overlay", toggle=True, icon="OVERLAY")

		# The most recent validation result
		if operators.LAST_REPORT:
			box = layout.box()
			for line in operators.LAST_REPORT[:12]:
				box.label(text=line, icon="CHECKMARK" if line.startswith("Valid") else "ERROR")
			if len(operators.LAST_REPORT) > 12:
				box.label(text=f"... and {len(operators.LAST_REPORT) - 12} more (see the Info editor)")

		# Chains, with the active chain's details
		layout.label(text="Chains")
		row = layout.row()
		row.template_list("RYLOGIC_UL_rig_chains", "", rig, "chains", rig, "active_chain", rows=4)
		column = row.column(align=True)
		column.operator("rylogic_rig.item_add", icon="ADD", text="").collection = "CHAINS"
		column.operator("rylogic_rig.item_remove", icon="REMOVE", text="").collection = "CHAINS"
		if 0 <= rig.active_chain < len(rig.chains):
			chain = rig.chains[rig.active_chain]
			box = layout.box()
			box.prop(chain, "role")
			box.prop(chain, "side")
			match chain.role:
				case "LEG" | "ARM":
					box.prop(chain, "pole")
				case "NECK":
					box.prop(chain, "forward")
				case _:
					pass
			box.label(text="Bones: " + ", ".join(ref.name for ref in chain.bones))
			box.operator("rylogic_rig.chain_assign", icon="BONE_DATA")

		# Contacts, with the active contact's details
		layout.label(text="Contacts")
		row = layout.row()
		row.template_list("RYLOGIC_UL_rig_contacts", "", rig, "contacts", rig, "active_contact", rows=4)
		column = row.column(align=True)
		column.operator("rylogic_rig.item_add", icon="ADD", text="").collection = "CONTACTS"
		column.operator("rylogic_rig.item_remove", icon="REMOVE", text="").collection = "CONTACTS"
		if 0 <= rig.active_contact < len(rig.contacts):
			contact = rig.contacts[rig.active_contact]
			box = layout.box()
			box.prop_search(contact, "bone", armature, "bones")
			box.prop(contact, "kind")
			box.prop(contact, "offset")
			box.prop(contact, "normal")


class RYLOGIC_PT_rig_bone(Panel):
	"""The active bone's joint limit and stiffness."""

	bl_label = "Rylogic Rig Joint"
	bl_space_type = "PROPERTIES"
	bl_region_type = "WINDOW"
	bl_context = "bone"

	@classmethod
	def poll(cls, context):
		return context.bone is not None

	def draw(self, context):
		# Show only the parameters that the limit kind uses
		layout = self.layout
		joint = context.bone.rylogic_rig
		layout.prop(joint, "limit_kind")
		match joint.limit_kind:
			case "CONE":
				layout.prop(joint, "cone_angle")
			case "HINGE":
				layout.prop(joint, "hinge_axis")
				layout.prop(joint, "hinge_min")
				layout.prop(joint, "hinge_max")
			case _:
				pass

		layout.prop(joint, "stiffness")


CLASSES = (
	RYLOGIC_UL_rig_chains,
	RYLOGIC_UL_rig_contacts,
	RYLOGIC_PT_rig,
	RYLOGIC_PT_rig_bone,
)


def Register() -> None:
	"""Register the panels and lists."""
	for cls in CLASSES:
		bpy.utils.register_class(cls)


def Unregister() -> None:
	"""Unregister the panels and lists."""
	for cls in reversed(CLASSES):
		bpy.utils.unregister_class(cls)
