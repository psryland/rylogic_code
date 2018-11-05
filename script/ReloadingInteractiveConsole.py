import sys, os, imp, code

class ReloadingInteractiveConsole(code.InteractiveConsole):

	def __init__(self, local=None):
		super().__init__(local)
		#readline.parse_and_bind("tab: complete")
		self.stored_modifier_times = {}
		self.CheckModulesForReload()

	def runcode(self, code):
		self.CheckModulesForReload()
		super().runcode(code)
		self.CheckModulesForReload() # maybe new modules are loaded

	def CheckModulesForReload(self):
		for module_name, module in sys.modules.items():
			if not hasattr(module, '__file__'):
				continue

			module_modifier_time = os.path.getmtime(module.__file__)
			if self.stored_modifier_times.get(module_name, module_modifier_time) < module_modifier_time:
				imp.reload(module)

			self.stored_modifier_times[module_name] = module_modifier_time

def interact(banner=None, local=None, exitmsg=None):
	ReloadingInteractiveConsole(local).interact(banner=banner, exitmsg=exitmsg)

