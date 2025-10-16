#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Utility for maintaining bookmarks in a project
# Usage:
#  Bookmark.py add -marks=<bookmarks_file> -label=<bookmark_name> -file=<file_name> -line=<line_number> [-group=<group_name>]
#     Add a bookmark to the list
#  Bookmark.py remove -marks=<bookmarks_file> [-label=<bookmark_name>] [-file=<file_name>] [-line=<line_number>] [-group=<group_name>]
#     Remove a bookmark from the list by label, file, or file:line
#  Bookmark.py list -marks=<bookmarks_file>
#     List all bookmarks
#  Bookmark.py ui -marks=<bookmarks_file> [-spawn] [-vscode] [-vs]
#     Show the UI and show bookmarks in Visual Studio Code or Visual Studio
#
import sys, os, json, subprocess, ctypes, win32com.client
from typing import List, Optional, Dict, Any, Callable
from dataclasses import dataclass
from pathlib import Path
from watchdog.observers import Observer
from watchdog.events import FileSystemEventHandler, FileSystemEvent
from tkinter import Tk, ttk, font

@dataclass
class Bookmark:
	label:str
	file:str
	line:int

class JsonEncoder(json.JSONEncoder):
	def default(self, obj):
		return obj.__dict__ if isinstance(obj, Bookmark) else super().default(obj)
def JsonDecoder(dct):
	return Bookmark(**dct) if "label" in dct and "file" in dct and "line" in dct else dct

GroupOther = "Other"

# Load the bookmarks from a file
def _LoadMarks(marks_file:str) -> dict:
	try:
		if not os.path.exists(marks_file):
			return {}
		with open(marks_file, "r") as f:
			return json.loads(f.read(), object_hook=JsonDecoder)
	except Exception as e:
		print(f"Failed to load bookmarks: {e}")
		return {}

# Save the bookmarks to a file
def _SaveMarks(marks_file:str, marks:Dict[Any, Any]):
	with open(marks_file, "w") as f:
		json.dump(marks, f,	indent=4, cls=JsonEncoder)

# Open a file in Visual Studio Code
def OpenInVSCode(file:str, line:int):
	try:
		subprocess.Popen(["code", "--goto", f"{file}:{line}"], shell=True)
	except Exception as e:
		print(f"Failed to open file in Visual Studio Code: {e}")
	return

# Open a file in Visual Studio
def OpenInVS(file:str, line:int):
	try:
		# Get the Visual Studio DTE object
		dte = win32com.client.GetActiveObject("VisualStudio.DTE")

		# Open the file
		dte.ItemOperations.OpenFile(file)

		# Get the text selection object
		selection = dte.ActiveDocument.Selection

		# Move to the specified line
		selection.GotoLine(line)

		# Ensure the line is visible
		selection.SelectLine()

	except Exception as e:
		print(f"Failed to open file in Visual Studio: {e}")
	return

# Add a bookmark
def Add(marks_file:str, label:str, file:str, line:str, group:Optional[str] = None):

	if label is None or len(label) == 0:
		raise ValueError("Missing label")
	if file is None or len(file) == 0:
		raise ValueError("Missing file")
	if line is None or len(line) == 0:
		raise ValueError("Missing line")
	
	line = int(line)
	group = group if group is not None else GroupOther
	
	marks = _LoadMarks(marks_file)
	marks.setdefault(group, [])

	# Remove for duplicates
	marks[group] = [mark for mark in marks[group] if Path(mark.file) != Path(file) or mark.line != line]

	# Add the new bookmark
	marks[group].append(Bookmark(label, file, line))

	_SaveMarks(marks_file, marks)
	return

# Remove a bookmark
def Remove(marks_file:str, label:Optional[str], file:Optional[str], line:Optional[str], group:Optional[str] = None):

	marks = _LoadMarks(marks_file)

	# Ignore if group not found
	group = group if group is not None else GroupOther
	if group not in marks:
		return

	# Remove matching labels
	if label is not None:
		marks[group] = [mark for mark in marks[group] if mark.label != label]

	# Remove matching file and line
	if file is not None:
		if line is not None:
			file = Path(file)
			line = int(line)
			marks[group] = [mark for mark in marks[group] if Path(mark.file) != file or mark.line != line]
		else:
			file = Path(file)
			marks[group] = [mark for mark in marks[group] if Path(mark.file) != file]

	_SaveMarks(marks_file, marks)
	return

# List all bookmarks
def Show(marks_file:str):

	marks = _LoadMarks(marks_file)

	for group in marks:
		print(f"Group: {group}")
		for mark in marks[group]:
			print(f"\t{mark.label} - {mark.file}({mark.line})")

	return

# Display a UI for managing bookmarks
def ShowUI(marks_file:str, spawn:bool = False, switches:Dict[str, Any] = {}):

	# Spawn the UI in a separate process
	if spawn:
		switches.pop("-spawn", None)
		args = [f"{k}={v}" for k, v in switches.items()]
		subprocess.Popen([sys.executable, __file__, "ui"] + args, creationflags=subprocess.DETACHED_PROCESS)
		return
	
	class UI(FileSystemEventHandler):
		def __init__(self):
			self.ui = Tk()
			self.ui.title("Bookmarks")
			self.ui.bind("<Configure>", self._SaveUIState)
			self._LoadUIState()

			try:
				# Enable DPI awareness on Windows
				ctypes.windll.shcore.SetProcessDpiAwareness(2)
				scale_factor = ctypes.windll.shcore.GetScaleFactorForDevice(0) / 100  # Get scale factor in percentage
				self.ui.tk.call('tk', 'scaling', scale_factor)
			except Exception as e:
				print(f"Error setting DPI awareness: {e}")

			# Set the default fault size
			self.default_font = font.nametofont("TkDefaultFont")
			self.default_font.configure(size=self.ui.winfo_height() // 40)

			# Create a style for the treeview
			style = ttk.Style()
			style.configure("Treeview", rowheight=26, font=self.default_font)

			# Create the treeview
			self.tree = ttk.Treeview(self.ui, columns=("File", "Line"), show="tree headings", selectmode="browse", style="Treeview")
			self.tree.heading("#0", text="Label", anchor="w")
			self.tree.heading("File", text="File", anchor="w")
			self.tree.heading("Line", text="Line", anchor="w")

			# Load the bookmarks
			self.marks_file = marks_file
			self.marks = _LoadMarks(marks_file)
			self._PopulateTree()

			# Bind actions to the UI
			self.tree.bind("<F2>", self._RenameItem)
			self.tree.bind("<Double-1>", self._GotoBookmark)
			self.tree.pack(expand=True, fill="both")

			# Watch for changes to the marks file
			self.observer = Observer()
			self.observer.schedule(self, os.path.dirname(marks_file), recursive=False)
			self.observer.start()

		# FS watcher callback
		def on_modified(self, event:FileSystemEvent):
			if event.event_type != "modified": return
			if Path(event.src_path) != Path(marks_file): return
			self.marks = _LoadMarks(marks_file)
			self._PopulateTree()

		# Run the UI
		def Run(self):
			self.ui.mainloop()
			self.observer.stop()
			self.observer.join()

		# Refresh the treeview
		def _PopulateTree(self):
	
			# Set the bookmark label
			self.update_map : Dict[str, Callable] = {}
			def SetBMLabel(bm:Bookmark, label:str):
				bm.label = label
				_SaveMarks(marks_file, self.marks)

			# Save the expanded state and clear all items
			expanded = set([self.tree.item(item)["text"] for item in self.tree.get_children()])
			self.tree.delete(*self.tree.get_children())

			# Populate the treeview
			for group in self.marks:
				group_tree = self.tree.insert("", "end", text=group)
				for bm in self.marks[group]:
					item_id = self.tree.insert(group_tree, "end", text=bm.label, values=(bm.file, bm.line))
					self.update_map[item_id] = lambda label, bm=bm: SetBMLabel(bm, label)

			# Restore the expanded state
			for item in self.tree.get_children():
				self.tree.item(item, open=(self.tree.item(item)["text"] in expanded))

		# Rename action on a bookmark
		def _RenameItem(self, event):
			item_id = self.tree.identify_row(event.y)
			column = self.tree.identify_column(event.x)
			if column != '#0' or item_id not in self.update_map:
				return

			# Do the rename
			def DoRenameItem(item_id, edit_box):
				label = edit_box.get()
				edit_box.destroy()

				self.update_map[item_id](label)
				_SaveMarks(marks_file, self.marks)

			# Show an edit box for renaming
			x, y, width, height = self.tree.bbox(item_id, column)
			edit_box = ttk.Entry(self.ui, font=self.default_font)
			edit_box.place(x=x, y=height, anchor="w", width=width)
			edit_box.insert(0, self.tree.item(item_id, "text"))
			edit_box.bind("<Return>", lambda e: DoRenameItem(item_id, edit_box))
			edit_box.bind("<FocusOut>", lambda e: edit_box.destroy())
			edit_box.focus()

		# Bind double-click event to the on_item_double_click function
		def _GotoBookmark(self, event):
			item_id = self.tree.focus()  # Get the ID of the selected item
			values = self.tree.item(item_id, "values")
			if len(values) != 2:
				return
			
			file, line = values

			# Open the file at the bookmark position
			if "-vscode" in switches and os.path.exists(file):
				OpenInVSCode(file, int(line))
			if "-vs" in switches and os.path.exists(file):
				OpenInVS(file, int(line))

		# Save the UI state
		def _SaveUIState(self, event):
			state = {"x": self.ui.winfo_x(), "y": self.ui.winfo_y(), "w": self.ui.winfo_width(), "h": self.ui.winfo_height()}
			with open(os.path.expanduser("~/.bookmark_ui_state.json"), "w") as f:
				json.dump(state, f, indent=4)
			
		# Load UI state
		def _LoadUIState(self):
			if not os.path.exists(os.path.expanduser("~/.bookmark_ui_state.json")):
				return
			with open(os.path.expanduser("~/.bookmark_ui_state.json"), "r") as f:
				state = json.loads(f.read())
				self.ui.geometry(f"{state['w']}x{state['h']}+{state['x']}+{state['y']}")

	UI().Run()
	return

# Syntax help
def ShowHelp():
	print(
		"Utility for maintaining bookmarks in a project\n"
		"Usage:\n"
		"  Bookmark.py add|remove|list|ui|help <options>\n"
		"Options:\n"
		"  -label=<bookmark_name>  The name of the bookmark\n"
		"  -file=<file_name>       The file to bookmark\n"
		"  -line=<line_number>     The line number to bookmark\n"
		"  -group=<group_name>     The group to add the bookmark to\n"
		"  -marks=<bookmarks_file> The file to store the bookmarks\n"
		"  -spawn                  Spawn the UI in a separate process\n"
		"  -vscode                 Open bookmarks in Visual Studio Code\n"
		"  -vs                     Open bookmarks in Visual Studio\n"
		"\n"
		"Notes:\n"
		"  The '-marks' option is optional. If not given, the bookmarks file is searched for\n"
		"  using the following paths:\n"
		"    <current-directory>/.bookmarks.json\n"
		"    <current-directory>/.vs/.bookmarks.json  (if -vs option is given)\n"
		"    <current-directory>/.vscode/.bookmarks.json (if -vscode option is given)\n"
		"    ~/.bookmarks.json\n"
		"\n"
		"Examples:\n"
		"  Bookmark.py add -label=<bookmark_name> -file=<file_name> -line=<line_number> [-group=<group_name>] [-marks=<bookmarks_file>]\n"
		"     Add a bookmark to the list\n"
		"  Bookmark.py remove [-label=<bookmark_name>] [-file=<file_name>] [-line=<line_number>] [-group=<group_name>] [-marks=<bookmarks_file>]\n"
		"     Remove a bookmark from the list by label, file, or file:line\n"
		"  Bookmark.py list -marks=<bookmarks_file>\n"
		"     List all bookmarks\n"
		"  Bookmark.py ui -marks=<bookmarks_file> [-spawn] [-vscode] [-vs]\n"
		"     Show the UI and show bookmarks in Visual Studio Code or Visual Studio\n"
	)
	return

# Find the marks file to operate on
def GetMarksFile(switches:Dict[str, Any]) -> str:
	
	# Check the command line
	marks_file = switches["-marks"] if "-marks" in switches else None
	if marks_file is not None and os.path.exists(marks_file):
		return os.path.abspath(marks_file)

	# Look in the local directory
	if marks_file is None and os.path.exists('.bookmarks.json'):
		return os.path.abspath('.bookmarks.json')
	
	# Look in the .vs directory
	if "-vs" in switches and os.path.exists('.vs/.bookmarks.json'):
		return  os.path.abspath('.vs/.bookmarks.json')

	# Look in the .vscode directory
	if "-vscode" in switches and os.path.exists('.vscode/.bookmarks.json'):
		return os.path.abspath('.vscode/.bookmarks.json')

	# Look in the home directory
	if os.path.exists(os.path.expanduser("~/.bookmarks.json")):
		return os.path.expanduser("~/.bookmarks.json")
	
	# Create one in the home directory
	marks_file = os.path.expanduser("~/.bookmarks.json")
	_SaveMarks(marks_file, {})
	return marks_file

# Run a bookmarks command
def Cmd(args:List[str]):

	cmd = None
	switches:Dict[str, Any] = {}

	# Parse the args
	for arg in args:
		if arg.startswith("-"):
			key, value = arg.split("=", 1) if "=" in arg else (arg, "")
			switches[key] = value
		elif cmd is None:
			cmd = arg.lower()
		else:
			raise ValueError(f"Unexpected argument: {arg}")

	# Default to UI mode
	if cmd is None:
		cmd = "ui"

	# Get the marks file
	marks_file = GetMarksFile(switches)

	# Run the command
	if cmd == "add":
		label = switches["-label"] if "-label" in switches else "New Bookmark"
		file = switches["-file"] if "-file" in switches else ""
		line = switches["-line"] if "-line" in switches else "0"
		group = switches["-group"] if "-group" in switches else None
		Add(marks_file, label, file, int(line), group)
		return

	if cmd == "remove":
		label = switches["-label"] if "-label" in switches else None
		file = switches["-file"] if "-file" in switches else None
		line = switches["-line"] if "-line" in switches else None
		group = switches["-group"] if "-group" in switches else None
		Remove(marks_file, label, file, line, group)
		return

	if cmd == "list":
		Show(marks_file)
		return

	if cmd == "ui":
		spawn = True if "-spawn" in switches else False
		ShowUI(marks_file, spawn, switches)
		return

	if cmd == "help":
		ShowHelp()
		return

# Entry point
if __name__ == "__main__":
	
	# Examples:
	if False:
		#sys.argv = [""]
		#sys.argv = ["", "add", "-label=My Bookmark0", "-file=C:\\Some Path\\To\\A\\File.exe", "-line=1234", "-marks=bookmarks.txt", "-group=My Group"]
		#sys.argv = ["", "remove", "-label=My Bookmark0", "-file=C:\\Some Path\\To\\A\\File.exe", "-line=1234", "-marks=bookmarks.txt", "-group=My Group"]
		#sys.argv = ["", "list", "-marks=bookmarks.txt"]
		#sys.argv = ["", "ui", "-marks=bookmarks.txt", "-spawn"]
		#sys.argv = ["", "ui", "-marks=bookmarks.txt", "-vscode", "-vs"]
		print("WARNING: command line arguments are disabled in this mode.")
		pass

	try:
		Cmd(sys.argv[1:])
	except Exception as e:
		print(f"Error: {str(e)}")
	pass
