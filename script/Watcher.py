from typing import List, Callable, Optional
from pathspec import PathSpec
from watchdog.observers import Observer
from watchdog.events import FileSystemEventHandler, FileSystemEvent

# export the FileSystemEvent class
__all__ = ['FileSystemEvent']

# Directory change watcher
class Watcher(FileSystemEventHandler):
	def __init__(self, directory:str, callback:Callable, ignore_spec:Optional[PathSpec] = None, files_only:bool = False, dirs_only:bool = False):
		self.directory = directory
		self.callback = callback
		self.ignore_spec = ignore_spec
		self.files_only = files_only
		self.dirs_only = dirs_only
		self.observer = Observer()
		self.observer.schedule(self, self.directory, recursive=True)
	def __del__(self):
		self.observer.stop()
		self.observer.join()

	# Start/Stop watching property
	@property
	def Watching(self):
		return self.observer.is_alive()
	@Watching.setter
	def Watching(self, value:bool):
		if value: self.observer.start()
		else: self.observer.stop()

	# File change event
	def on_any_event(self, event:FileSystemEvent):
		if event.is_synthetic:
			return
		if self.files_only and event.is_directory:
			return
		if self.dirs_only and not event.is_directory:
			return
		if self.ignore_spec and self.ignore_spec.match_file(event.src_path):
			return
		self.callback(event)
		return

# Testing
if __name__ == '__main__':
	import os, time, sys, atexit

	# Callback function
	def on_change(event:FileSystemEvent):
		print(f'[{event.event_type}] {event.src_path}')

	# Create a watcher
	watcher = Watcher(os.path.dirname(__file__), PathSpec.from_lines('gitwildmatch', ['*.pyc']), on_change)
	watcher.Watching = True

	# Keep the script running
	try:
		while True:
			time.sleep(1)
	except KeyboardInterrupt:
		sys.exit(0)
