#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# A base for interactive command line tools
import os, re, atexit, json, readline, shlex, stat, subprocess
from dataclasses import dataclass, field
from pathspec import PathSpec
from pathlib import Path
from typing import List, Tuple, Mapping, Optional, Callable, TypeVar, Generic

T = TypeVar('T')

@dataclass
class StateValue(Generic[T]):
	value: T #Union[bool, str, List[str], Dict[str, str]]
	convert: Callable[[str], T]
	options: Optional[List[str]] = None
	completer: Optional[Callable[[str], List[str]]] = None
	validate: Optional[Callable[[str], bool]] = None
	configurable: bool = True

@dataclass
class Cmd:
	# Aliases that invoke this command
	aliases: List[str]

	# The implementation of the command
	func: Optional[Callable] = None

	# At array of argument sequences.
	# e.g.
	# [
	#    [("get", None)],
	#    [("set", None), ("value", MyCompleter)]
	# ]
	args: List[List[Tuple[str, Optional[Callable]]]] = field(default_factory=list)

	# Help message for the command
	help: str = ""

class CmdTool:

	# Called at the start of contruction
	def __init__(self, tool_name:str, persistence:bool):
		self.Cmds = CmdTool.StockCmds(self)
		self.tool_name = tool_name
		self.persistence = persistence
		self.m_completer:List[Callable[[str], List[str]]] = []
		self.prompt = '> '
		self.cmds = []
		self.ignore = []
		self.root = os.getcwd()
		self.show_cmdlines = False
		self.config_line_wrap_length = 500
		return

	# Called at the end of construction
	def __initend__(self):
		# Setup readline
		readline.set_completer(self._Completer) # type: ignore
		readline.parse_and_bind("tab: complete") # type: ignore

		# Load persistent state
		atexit.register(self._SaveState)
		self._LoadState()

		# Add commands to the tool
		self._SetupCommands()

		# Setup an missing user settings
		self._ApplyConfig()

		# Running flag
		self.running = True
		return

	# Stock commands
	class StockCmds:
		def __init__(self, Self):
			self.Divider = Cmd(
				aliases = [],
				help = "---------------------------------",
			)
			self.Exit = Cmd(
				aliases = ["exit", "quit", "q"],
				func = Self.Quit,
				help = "Exit the program",
			)
			self.Help = Cmd(
				aliases = ["help", "?"],
				func = Self.ShowHelp,
				help = "Show this help message",
			)
			self.ClearTerminal = Cmd(
				aliases = ["clear", "cls"],
				func = Self.ClearTerminal,
				help = "Clear the screen",
			)
			self.Config = Cmd(
				aliases = ["config"],
				func = Self.Config,
				args = [
					[("-setall", None)],
					[("<key>", Self._CompleterStateName)],
					[("<key>", Self._CompleterStateName), ("<value>", Self._CompleterStateValue)],
					[("-save", None)],
				],
				help = "Setup tool and environment variables.",
			)
			self.ListDir = Cmd(
				aliases = ["ls", "dir"],
				func = Self.ListDir,
				args = [
					[("[<path>]", Self._CompleterPath)],
				],
				help = "List the contents of a directory",
			)
			self.ChangeDir = Cmd(
				aliases = ["cd"],
				func = Self.ChangeDir,
				args = [
					[("<path>", Self._CompleterPath)],
				],
				help = "Change the current directory",
			)
			return

	# Setup commands
	def _SetupCommands(self):
		self.cmds = [
			self.Cmds.Exit,
			self.Cmds.Help,
			self.Cmds.ClearTerminal,
			self.Cmds.Config,
			self.Cmds.Divider,
			self.Cmds.ListDir,
			self.Cmds.ChangeDir,
		]

	# Get the state to be saved
	def _State(self) -> dict[str, StateValue]:
		# Override this method to return the state to be saved
		return {
			"root": StateValue(
				value = self.root,
				convert = os.path.abspath,
				completer = self._CompleterPath,
				validate = self._ValidatePathExists,
			),
   			"show_cmdlines": StateValue(
				value = self.show_cmdlines,
    			convert = lambda x: x.lower() == "true",
			),
			"ignore": StateValue(
				value = self.ignore,
				convert = str,
			),
		}

	# Setup the environment variables (after loading the state)
	def Config(self, args:List[str] = []):

		switches = self._ParseSwitches(args)
		state = self._State()
		state = {key: state[key] for key in state if state[key].configurable}

		# Show all configuration values
		if len(args) == 0:
			for key in state:
				self._ShowValue(key, state[key])
			return

		# Set all configuration values
		if "-setall" in switches:
			for key in state:
				self.__dict__[key] = self._PromptForValue(key, state[key])
			self._SaveState()
			self._ApplyConfig()
			return

		# Save the config
		if "-save" in switches:
			self._SaveState()
			self._ApplyConfig()
			return

		# Set a single configuration value
		if len(args) >= 1 and args[0] in state:
			key = args[0]
			value = ' '.join(args[1:]) if len(args) > 1 else None
			self.__dict__[key] = self._PromptForValue(key, state[key], value)
			self._SaveState()
			self._ApplyConfig()
			return

		print("Unknown config value")
		return

	# Apply config to derived values
	def _ApplyConfig(self):
		self.ignore_spec = PathSpec.from_lines("gitwildmatch", self.ignore)
		return

	# Show the welcome message
	def ShowWelcome(self):
		self._ShowTitle()
		self._ShowHelpMessage()
		print()
		return
	def _ShowTitle(self):
		print(f"{Colors.BG_BLACK}{Colors.BRIGHT_WHITE}{self.tool_name}{Colors.RESET}")
		return
	def _ShowHelpMessage(self):
		print(f"{Colors.GRAY}  ? for help{Colors.RESET}")
		return

	# Show the command line help
	def ShowHelp(self, args:List[str] = []):
		print(f"{Colors.GREEN}Commands:{Colors.RESET}\n")
		for cmd in self.cmds:

			# Skip the divider
			if cmd == self.Cmds.Divider:
				print(f"     {Colors.GREEN}------------------------{Colors.RESET}     ")
				continue

			aliases = cmd.aliases
			has_args = len(cmd.args) != 0
   
			# Print the command aliases and help text
			print(f"{Colors.BLUE}  {', '.join(aliases):16}{Colors.RESET} - {cmd.help}")
			
			# Show the options for arguments
			for argset in cmd.args:
				args = [arg[0] for arg in argset]
				print(f"{Colors.GRAY}    {aliases[0]} {' '.join(args)}{Colors.RESET}")

			if has_args:
				print()
		print()
		return

	# Clear the terminal
	def ClearTerminal(self, args:List[str] = []):
		os.system('cls' if os.name == 'nt' else 'clear')
		return

	# Read a single command from user input
	def ReadUserInput(self) -> Tuple[Cmd|None, List[str]]:

		# Split the input into arguments
		args = self._ShellSplit(input(self.prompt))

		# Find the command by alias
		cmd_str = args[0].lower()
		cmd = self._FindCommand(cmd_str)
		return cmd, args[1:]

	# Read and execute a single command
	def RunCommand(self):
		cmd, args = self.ReadUserInput()
		if cmd and cmd.func:
			cmd.func(args)
		else:
			print("Unknown command")
		return

	# Run the tool until quit
	def Run(self, welcome:bool = False):
		if welcome:
			self.ShowWelcome()
		while self.running:
			try:
				self.RunCommand()
			except KeyboardInterrupt:
				print("User cancelled")
			except Exception as e:
				print(f"Error: {str(e)}")
		return

	# Parse the command line arguments
	def ParseCommandLine(self, args: List[str]):
		# Override this method to parse the command line arguments
		return

	# Quit the program
	def Quit(self, args:List[str] = []):
		self.running = False
		return

	# List the contents of a directory
	def ListDir(self, args:List[str]):
		# Get the path to list
		path = Path(args[0] if len(args) > 0 else self.root).absolute()
		if not path.exists():
			print(f"Path '{path}' does not exist")
			return

		# Get the directory contents
		root, dirs, files = next(os.walk(path), (None, [], []))
		if not root:
			print(f"Path '{path}' does not exist")
			return

		# List the directories
		dirs.sort()
		for d in dirs:
			fullpath = Path(root, d)
			filemode = stat.filemode(fullpath.lstat().st_mode)
			if fullpath.is_symlink():
				print(f"{filemode:10} {'':10} {Colors.CYAN}{d}{Colors.RESET} -> {fullpath.readlink()}")
			else:
				print(f"{filemode:10} {'':10} {Colors.BLUE}{d}{Colors.RESET}")

		# List the files
		files.sort()
		for f in files:
			fullpath = Path(root, f)
			size = os.path.getsize(fullpath)
			filemode = stat.filemode(fullpath.lstat().st_mode)
			if fullpath.is_symlink():
				print(f"{filemode:10} {size:10} {Colors.CYAN}{f}{Colors.RESET} -> {fullpath.readlink()}")
			else:
				print(f"{filemode:10} {size:10} {f}")
		return

	# Change the current directory
	def ChangeDir(self, args:List[str]):
		if len(args) != 0:
			path = args[0] if os.path.isabs(args[0]) else os.path.abspath(os.path.join(self.root, args[0]))
			if os.path.exists(path):
				self.root = path
				os.chdir(path)
			else:
				print(f"Path '{path}' does not exist")

		print(f"Current directory: {self.root}")
		return

	# Top level completer
	def _Completer(self, text, state) -> str|None:
		try:
			# Split the line into args
			parts = self._ShellSplit(readline.get_line_buffer()) # type: ignore

			# Use the override completer if available
			if len(self.m_completer) != 0:
				options = self.m_completer[-1](text)
				return options[state] if state < len(options) else None

			# Find the command
			cmd = self._FindCommand(parts[0])
			if not cmd:
				possibles = self._CompleterCommand(parts[0])
				return possibles[state] if state < len(possibles) else None

			# Get the index of the argument being completed
			# part[0] = command, part[1] = first argument, etc.
			arg_idx = len(parts) - 2

			# Completer functions return arrays of options so we can concatenate them
			options = []

			# See if there is a completer for this argument
			argsets = cmd.args
			for argset in argsets:

				# If the 'arg_idx'th argument has a completer function
				completer_func = argset[arg_idx][1] if arg_idx < len(argset) else None
				options.extend(completer_func(text) if completer_func else [])

			# Return the next option
			return options[state] if state < len(options) else None
		except: pass
		return None

	# Completer for commands
	def _CompleterCommand(self, text) -> List[str]:
		return [alias for cmd in self.cmds for alias in cmd.aliases if alias.startswith(text)]

	# Completer for a file path
	def _CompleterPath(self, text) -> List[str]:

		# readline doesn't handle backslashes well, it only contains the text after the last slash.
		# Use the last part of the whole line instead of 'text'
		line = readline.get_line_buffer() # type: ignore
		parts = self._ShellSplit(line)
		text = parts[-1]

		# Index of the last slash (or -1 if not found)
		text = text.replace('\\', '/')
		slash = text.rfind('/') + 1

		# Split the path into parent directory and name
		parent = text[:slash]
		name = text[slash:]

		# Make the parent directory absolute
		fullparent = parent if os.path.isabs(parent) else os.path.join(self.root, parent)
		if not os.path.exists(fullparent):
			return []

		# Get the possible subpaths of the parent directory
		subpaths = [p for p in os.listdir(fullparent) if p.lower().startswith(name.lower())]

		# Get the possible completions relative to 'text'
		if line.rfind('\\') != -1:
			return subpaths
		else:
			return [parent + p for p in subpaths]
		#subpaths = [p.replace('/', '\\') for p in subpaths]
		#return subpaths # [parent + p for p in subpaths]

	# Completer for a state name
	def _CompleterStateName(self, text) -> List[str]:
		return [key for key in self._State() if key.startswith(text)]

	# Completer for a state value
	def _CompleterStateValue(self, text) -> List[str]:
		parts = self._ShellSplit(readline.get_line_buffer()) # type: ignore
		if parts[0].lower() != "config":
			return []

		state = self._State()
		if parts[1] not in state:
			return []

		item = state[parts[1]]
		if item.options:
			return self._CompleterOptions(text, item.options)
		if item.completer:
			return item.completer(parts[2] if len(parts) > 2 else "")

		return []

	# Completer for a list of options
	def _CompleterOptions(self, text, options:List[str]) -> List[str]:
		return [opt for opt in options if opt.startswith(text)]

	# Match the command in the 'aliases' of 'self.cmds'
	def _FindCommand(self, cmd_str:str):
		return next((cmd for cmd in self.cmds if cmd_str in cmd.aliases), None)

	# Set the value of a state item
	def _PromptForValue(self, key: str, item: StateValue, newvalue: Optional[str] = None) -> object:

		current = item.value

		def Validate(newvalue:str) -> bool:
			if item.options and newvalue not in item.options:
				print("Not a valid option")
				return False
			if item.validate and not item.validate(newvalue):
				return False
			return True

		# Repeat until valid input
		while True:
			# If no new value is provided, prompt for one
			if not newvalue:

				# Show the prompt
				self._ShowValue(key, item)
				print(f"New Value (Enter to skip)", end=": ")

				# Print the possible options
				if item.options:
					print(f"\0337\033[90m{', '.join(item.options)}\033[0m\0338", end="")

				# Get the input
				if item.completer: self.m_completer.append(item.completer)
				newvalue = input()
				if item.completer: self.m_completer.pop()

			# Trim whitespace
			newvalue = newvalue.strip()
			if not newvalue:
				return current

			# If 'current' is a dictionary, use [-]key[=value] format
			if isinstance(current, dict):
				if newvalue.startswith("-"):
					key = newvalue[1:].strip()
					if current.pop(key, None) == None:
						print(f"{key} not found.")
					else:
						print("updated.")
				else:
					if not "=" in newvalue:
						print("Invalid format. Expected key=value or -key")
					else:
						key, value = newvalue.split("=", 1)
						key = key.strip()
						value = value.strip()
						if Validate(value):
							current[key] = value
							print("updated.")

			# If 'current' is a list, use [-]value format
			elif isinstance(current, list):
				if newvalue.startswith("-"):
					value = item.convert(newvalue[1:])
					if value not in current:
						print(f"{value} not found.")
					else:
						current = [c for c in current if c != value]
						print("updated.")
				else:
					value = item.convert(newvalue)
					if Validate(value):
						current.append(value)
						print("updated.")

			# Otherwise, just set the value
			else:
				value = item.convert(newvalue)
				if Validate(value):
					current = value
					print("updated.")
					return current

			newvalue = None

	# Print a state item
	def _ShowValue(self, key:str, item: StateValue):
		if isinstance(item.value, dict):
			inline = ' '.join([f"{k}={item.value[k]}" for k in item.value])
			if len(inline) < self.config_line_wrap_length:
				print(f"{Colors.GREEN}{key}{Colors.RESET}: {inline}")
			else:
				print(f"{Colors.GREEN}{key}{Colors.RESET}", end=":\n")
				for k in item.value:
					print(f"  {k}: {item.value[k]}")
		elif isinstance(item.value, list):
			inline = ' '.join(item.value)
			if len(inline) < self.config_line_wrap_length:
				print(f"{Colors.GREEN}{key}{Colors.RESET}: {inline}")
			else:
				print(f"{Colors.GREEN}{key}{Colors.RESET}", end=":\n")
				for value in item.value:
					print(f"  {value}")
		else:
			print(f"{Colors.GREEN}{key}{Colors.RESET}: {item.value}")
		return

	# Split a string into arguments based on shell rules. Always returns at least one argument.
	def _ShellSplit(self, text:str) -> List[str]:
		# Escape backslashes
		text = re.sub(r"(?<!\\)(?<!\\\\)\\", r"\\\\", text)
		parts = shlex.split(text)

		# If there is a whitespace on the end, assume that's the start of an empty argument
		if len(parts) == 0 or text.endswith(" "):
			parts.append("")

		return parts

	# Extract the switches from the command line arguments. Expected format: -switch[=value]
	# Returns a dictionary of {switch: value}, and a list of the arguments that are not switches
	def _ParseSwitches(self, args:List[str]) -> Tuple[dict, List[str]]:
		switches = {}
		remaining = []
		for arg in args:
			if arg.startswith("-"):
				key, value = arg.split("=", 1) if "=" in arg else (arg, "")
				switches[key] = value
			else:
				remaining.append(arg)
		return switches, remaining

	# Validate a path exists
	def _ValidatePathExists(self, path:str) -> bool:
		if Path(path).exists(): return True
		print(f"Path '{path}' does not exist")
		return False

	# Validate a value as a number
	def _ValidateIsNumber(self, value:str) -> bool:
		if value.isdigit(): return True
		print(f"Expected a number")
		return False

	# Run a subprocess
	def _RunSubprocess(self
		, cmd:List[str]
		, stdin: Optional[int] = None
		, stdout: Optional[int] = None
		, stderr: Optional[int] = None
		, shell: bool = False
		, env: Optional[Mapping[str, str]] = None
		, cwd: Optional[str] = None
		, capture_output:bool = False
		, check:bool = False
		, encoding: Optional[str] = None
		, input: Optional[str] = None
		, text: Optional[bool] = None
		, creationflags: int = 0
	) -> Tuple[subprocess.CompletedProcess[bytes], str, str]:
		if self.show_cmdlines: print(" ".join(cmd))
		result = subprocess.run(cmd, stdin=stdin, stdout=stdout, stderr=stderr, shell=shell, env=env, cwd=cwd, capture_output=capture_output, check=check, encoding=encoding, input=input, text=text, creationflags=creationflags)
		out = result.stdout.decode() if "decode" in dir(result.stdout) else str(result.stdout) if result.stdout else ""
		err  = result.stderr.decode() if "decode" in dir(result.stderr) else str(result.stderr) if result.stderr else ""
		return (result, out, err)

	# Spawn an instance of the target
	def _SpawnProcess(self
		, cmd:List[str]
		, stdin: Optional[int] = None
		, stdout: Optional[int] = None
		, stderr: Optional[int] = None
		, shell: bool = False
		, env: Optional[Mapping[str, str]] = None
		, cwd: Optional[str] = None
		, encoding: Optional[str] = None
		, text: Optional[bool] = None
		, creationflags: int = subprocess.DETACHED_PROCESS
	) -> subprocess.Popen:
		if self.show_cmdlines: print(f"Launching '{' '.join(cmd)}'...")
		return subprocess.Popen(cmd, stdin=stdin, stdout=stdout, stderr=stderr, shell=shell, env=env, cwd=cwd, encoding=encoding, text=text, creationflags=creationflags)

	# Paths for saving state
	def _StateDirectory(self) -> Path:
		tool_name = re.sub(r"[ /\\:.\?*]", "", self.tool_name.lower())
		return Path(f"~/.{tool_name}").expanduser()
	def _StateFilepath(self) -> Path:
		return self._StateDirectory() / "state.json"
	def _HistoryFilepath(self) -> Path:
		return self._StateDirectory() / "history.txt"
	
	# Save the state to a json file
	def _SaveState(self):
		if not self.persistence:
			return
	
		# Get the state to be saved
		state = self._State()

		# Make a dictionary of {key: state[key].value}
		state = {key: state[key].value for key in state}

		# Ensure there is a directory for the tool in the users home directory
		os.makedirs(self._StateDirectory(), exist_ok=True)

		# Write the state
		with open(self._StateFilepath(), "w") as file:
			json.dump(state, file, indent=4)

		# Save the command history
		readline.write_history_file(self._HistoryFilepath()) # type: ignore
		print("Config Saved.")
		return

	# Load the state
	def _LoadState(self):
		if not self.persistence:
			return
		
		# Load the state
		if self._StateFilepath().exists():
			try:
				with open(self._StateFilepath(), "r") as file:
					state = json.load(file)
					for key in state:
						self.__dict__[key] = state[key]

			except Exception as e:
				print(f"State file corrupt: {str(e)}")
				print(f"Using defaults...")

		# Load the history
		if os.path.exists(self._HistoryFilepath()):
			readline.read_history_file(self._HistoryFilepath()) # type: ignore

		return

# Terminal colors
class Colors:	
	# Reset
	RESET = '\033[0m'

	# Regular Colors
	RED = '\033[31m'
	GREEN = '\033[32m'
	YELLOW = '\033[33m'
	BLUE = '\033[34m'
	MAGENTA = '\033[35m'
	CYAN = '\033[36m'
	WHITE = '\033[37m'
	GRAY = '\033[90m'
	
	# Bright Colors
	BRIGHT_BLACK = '\033[90m'
	BRIGHT_RED = '\033[91m'
	BRIGHT_GREEN = '\033[92m'
	BRIGHT_YELLOW = '\033[93m'
	BRIGHT_BLUE = '\033[94m'
	BRIGHT_MAGENTA = '\033[95m'
	BRIGHT_CYAN = '\033[96m'
	BRIGHT_WHITE = '\033[97m'
	
	# Background Colors
	BG_BLACK = '\033[40m'
	BG_RED = '\033[41m'
	BG_GREEN = '\033[42m'
	BG_YELLOW = '\033[43m'
	BG_BLUE = '\033[44m'
	BG_MAGENTA = '\033[45m'
	BG_CYAN = '\033[46m'
	BG_WHITE = '\033[47m'
	
	# Bright Background Colors
	BG_BRIGHT_BLACK = '\033[100m'
	BG_BRIGHT_RED = '\033[101m'
	BG_BRIGHT_GREEN = '\033[102m'
	BG_BRIGHT_YELLOW = '\033[103m'
	BG_BRIGHT_BLUE = '\033[104m'
	BG_BRIGHT_MAGENTA = '\033[105m'
	BG_BRIGHT_CYAN = '\033[106m'
	BG_BRIGHT_WHITE = '\033[107m'

# A demo tool based on CmdTool
class Example(CmdTool):
	def __init__(self):
		super().__init__("Example Tool", False)
		self.dic = {"one": 1, "two": 2, "three": 3}
		self.flag = True
		super().__initend__()
		return
	
	# Get the state to be saved
	def _State(self) -> dict[str, StateValue]:
		state = super()._State()
		state["flag"] = StateValue(
			value = True,
			convert = lambda x: x.lower() == "true",
		)
		state["dic"] = StateValue(
			value = self.dic,
			convert = lambda x: int(x),
		)
		return state

	def _SetupCommands(self):
		super()._SetupCommands()
		self.cmds += [
			self.Cmds.Divider,
			Cmd(
				aliases = ["echo"],
				func = self.Echo,
				args = [
					[("<message>", None)],
				],
				help = "Echo a message",
			)
		]
		return
	
	def Echo(self, args:List[str]):
		print(" ".join(args))
		return

if __name__ == "__main__":
	Example().Run(True)