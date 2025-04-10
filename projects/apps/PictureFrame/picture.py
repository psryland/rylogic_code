# Requires:
#  pip install python-vlc
#
# Compile to exe with:
# - pip install pyinstaller
# - C:\Users\paulryland\AppData\Roaming\Python\Python311\Scripts\pyinstaller --onefile --noconsole .\picture.py
# How to profile:
# - python -m cProfile -o profile.out picture.py
# - C:\Users\paulryland\AppData\Roaming\Python\Python311\Scripts\snakeviz.exe profile.out

import sys, os, json, platform, fnmatch, random, pathspec, threading
from pathlib import Path
import tkinter as Tk
import vlc

class PictureFrame:
	def __init__(self):
		# Get the current directory
		project_dir = Path(__file__).resolve().parent
		self.root_dir = project_dir

		# Load the config
		self.config = self._LoadConfig()

		# Get the image root path
		self.image_dir = Path(self.config[f"ImageRoot-{platform.system()}"])
		if not self.image_dir.exists():
			raise FileNotFoundError(f"Image root path {self.image_dir} does not exist")

		# Create a tk window
		self.window = Tk.Tk()
		self.window.title("Picture Frame")
		self.window.geometry("800x600")  # Set initial size
		self.window.attributes("-fullscreen", bool(self.config['FullScreen']))
		self.window.bind("<Escape>", lambda e: self.window.quit())  # Exit on ESC key
		self.window.bind("<Configure>", lambda e: self._UpdateUI())
		
		# Create a backbuffer for images and video
		self.bb = Tk.Frame(self.window, bg="black", borderwidth=0, highlightthickness=0)
		self.bb.pack(side=Tk.TOP, fill=Tk.BOTH, expand=True)
		
		# Create a vlc player and set the video output to the Tkinter window
		self.vlc = vlc.Instance("--no-xlib", "--no-video-title-show", "--quiet", "--verbose=2")
		self.player = self.vlc.media_player_new()
		self.player.event_manager().event_attach(vlc.EventType.MediaPlayerStopped, self._OnStopped)
		self.player.event_manager().event_attach(vlc.EventType.MediaPlayerEndReached, self._OnEndReached)
		self.player.event_manager().event_attach(vlc.EventType.MediaPlayerEncounteredError, self._OnErrorEncountered)

		# Create a text label to show the file path
		self.label_filepath = Tk.Label(self.window, text="", bg="black", fg="white", font=("Arial", 12))

		# Next and previous buttons
		self.button_next = Tk.Button(self.window, text=">", bg="black", fg="white", font=("Arial", 20), border=0, command=self._NextImage)
		self.button_prev = Tk.Button(self.window, text="<", bg="black", fg="white", font=("Arial", 20), border=0, command=self._PrevImage)

		# A button to show the options menu
		self.button_menu = Tk.Button(self.window, text="...", bg="black", fg="white", border=0, command=self._ShowMenu)

		# Options menu
		self.options_panel = Tk.Frame(self.window, bg="black", borderwidth=0, highlightthickness=0)
		self.options_dontshowagain = Tk.Button(
			self.options_panel,
			text="Don't Show This Again", bg="black", fg="white",
			border=0, borderwidth=0, highlightthickness=0,
			command=self._DontShowAgain)
		self.options_showhidefilepath = Tk.Button(
			self.options_panel,
			text="Show/Hide Filepath", bg="black", fg="white",
			border=0, borderwidth=0, highlightthickness=0,
			command=self._ToggleShowFilePath)
		self.options_shuffle = Tk.Button(
			self.options_panel,
			text="Shuffle", bg="black", fg="white",
			border=0, borderwidth=0, highlightthickness=0,
			command=self._Shuffle)
		self.options_fullscreen = Tk.Button(
			self.options_panel,
			text="FullScreen", bg="black", fg="white",
			border=0, borderwidth=0, highlightthickness=0,
			command=self._FullScreen)
		self.options_displayrate = Tk.Scale(
			self.options_panel, from_=1, to=60, orient=Tk.HORIZONTAL,
			label="Display Rate", bg="black", fg="white",
			border=0, borderwidth=0, highlightthickness=0,
			command=self._ChangeDisplayRate)

		self.options_dontshowagain.pack(side=Tk.TOP, fill=Tk.X, padx=5, pady=5)
		self.options_showhidefilepath.pack(side=Tk.TOP, fill=Tk.X, padx=5, pady=5)
		self.options_shuffle.pack(side=Tk.TOP, fill=Tk.X, padx=5, pady=5)
		self.options_fullscreen.pack(side=Tk.TOP, fill=Tk.X, padx=5, pady=5)
		self.options_displayrate.pack(side=Tk.TOP, fill=Tk.X, padx=5, pady=5)

		# No images text
		self.no_images_label = Tk.Label(self.window, text="No images found", bg="black", fg="white", font=("Arial", 20))

		self.image_list = []
		self.image_index = -1
		self.media = None
		self.ui_visible = False
		self.issue_number = 0
		return

	# Scan for images and videos
	def Scan(self):
		patterns = self.config['ImagePatterns'] + self.config['VideoPatterns']

		print(f"Image root path: {self.image_dir}")
		print(f"Include patterns: {patterns}")

		print("Loading ignore list...")
		ignore_patterns = self._LoadIgnorePatterns()
		ignore_spec = pathspec.PathSpec.from_lines('gitwildmatch', ignore_patterns)

		print("Scanning...")

		# Generate an image list
		image_list = []
		for dirpath, dirnames, filenames in os.walk(self.image_dir):

			# Filter filenames based on extensions
			filenames = [f for f in filenames if any(fnmatch.fnmatch(f, pattern) for pattern in patterns)]

			# Get paths relative to the image directory
			relpaths = [os.path.relpath(os.path.join(dirpath, filename), self.image_dir).replace('\\','/') for filename in filenames]

			# Filter using the ignore patterns
			relpaths = [relpath for relpath in relpaths if not ignore_spec.match_file(relpath)]

			image_list += relpaths

		self._SaveImageList(image_list)
		return
	
	# Run the main loop
	def Run(self):
		# Load the image list
		self.image_list = self._LoadImageList()
		self._Shuffle()

		self._UpdateUI()

		# Wait for the window to be viewable
		def WaitTillShown():
			if self.bb.winfo_viewable() == 0:
				self.window.after(500, WaitTillShown)
				return

			# Bind VLC to the window handle
			if platform.system() == 'Windows':
				self.player.set_hwnd(self.bb.winfo_id())
			elif platform.system() == 'Linux':
				self.player.set_xwindow(self.bb.winfo_id())
			elif platform.system() == 'Darwin':  # macOS
				self.player.set_nsobject(self.bb.winfo_id())

			# Start displaying images
			self._NextImage()
			return
		
		WaitTillShown()
		self.window.mainloop()

	# Current image relative path (or None)
	@property
	def CurrentImageRelpath(self):
		return self.image_list[self.image_index].strip() if self.image_index >= 0 and self.image_index < len(self.image_list) else None
	
	# Display the next image
	def _NextImage(self):
		self.issue_number += 1
		fullpath, relpath = self._FindMedia(+1)
		self._DisplayMedia(fullpath, relpath, self.issue_number)
		return

	# Display the previous image
	def _PrevImage(self):
		self.issue_number += 1
		fullpath, relpath = self._FindMedia(-1)
		self._DisplayMedia(fullpath, relpath, self.issue_number)
		return

	# Find the next/previous image in the list
	def _FindMedia(self, increment):
		# Limit the search to test every image in the list
		for i in range(len(self.image_list)):
			
			# Increment the image index
			self.image_index = (self.image_index + len(self.image_list) + increment) % len(self.image_list)

			relpath = self.CurrentImageRelpath
			fullpath = (self.image_dir / relpath).resolve()

			if fullpath.exists():
				return fullpath, relpath

		return Path(), Path()

	# Display the image/video at 'self.image_index'
	def _DisplayMedia(self, fullpath, relpath, issue_number):
		# Kill any call that doesn't match the issue number
		if issue_number != self.issue_number:
			return

		# Stop any existing media
		self._StopMedia()

		# Set the filepath label text
		self.label_filepath.configure(text=relpath)

		# Display a no images message if there are no images
		self.no_images_label.place_forget()
		if not fullpath or fullpath == Path():
			self.no_images_label.place(anchor="center", relx=0.5, rely=0.5)
			return

		# Add the image to the displayed image log
		self._LogDisplayed(fullpath)

		# Determine the file type
		extn = fullpath.suffix.lower()

		# Display image if it is an image file
		try:
			if extn in ['.png', '.jpg', '.jpeg', '.bmp']:
				self._ShowImage(fullpath, issue_number)
			elif extn in ['.mp4', '.avi', '.mov']:
				self._ShowVideo(fullpath, issue_number)
			else:
				self.window.after(100, self._NextImage)
		except Exception as e:
			print(f"Error displaying image {fullpath}: {e}")
			self.window.after(100, self._NextImage)
		return

	# Display a still image
	def _ShowImage(self, image_fullpath, issue_number):
		def Stop():
			if issue_number != self.issue_number: return
			self._StopMedia()
			self._NextImage()
			return

		if self.media is not None: raise Exception("Media is not None")
		self.media = self.vlc.media_new(image_fullpath)
		self.player.set_media(self.media)
		self.player.play()
		self.window.after(1000 * int(self.config['DisplayPeriodSeconds']), Stop)
		return

	# Display a video
	def _ShowVideo(self, video_fullpath, issue_number):
		def Stop():
			if issue_number != self.issue_number: return
			if self.player.is_playing():
				self.window.after(500, Stop)
				return
			self._StopMedia()
			self._NextImage()
			return

		if self.media is not None: raise Exception("Media is not None")
		self.media = self.vlc.media_new(video_fullpath)
		self.player.set_media(self.media)
		self.player.play()
		self.window.after(500, Stop)
		return

	# Stop and clean up any media
	def _StopMedia(self):
		if threading.current_thread() != threading.main_thread():
			raise Exception("ReleaseMedia called from non-main thread")

		print("Stopping media...")

		# If currently playing, call stop and wait for the media to be released
		# VLC does not raise the 'Stopped' event unless it was playing, but it's not safe to
		# release the media until the 'Stopped' event is received.
		if self.player.is_playing():
			self.player.stop()

		# Wait for the media player to finish using the media
		while self.media is not None:
			self.window.update()

		return

	# Event called from VLC when the media stops
	def _OnStopped(self, event):
		# It's only safe to release media when stopped or end-reached is received, not before.
		self.window.after(1, self._ReleaseMedia)
		return

	# Event called from VLC when the media reaches the end
	def _OnEndReached(self, event):
		# It's only safe to release media when stopped or end-reached is received, not before.
		self.window.after(1, self._ReleaseMedia)
		return

	# Delete 'self.media'
	def _ReleaseMedia(self):
		# This is an absolute minefield. Releasing media will crash VLC if not done properly,
		# Unfortunately, the API doesn't provide a simple way to check for "safe to release".
		if threading.current_thread() != threading.main_thread():
			raise Exception("ReleaseMedia called from non-main thread")
		
		print("Releasing media...")
		self.player.set_media(None)
		if self.media is not None: self.media.release()
		self.media = None
		return

	# VLC reported error
	def _OnErrorEncountered(self, event):
		print(f"Recved ErrorEncountered: {event}")
		self._StopMedia()
		self._NextImage()
		return

	# Show/Hide UI elements
	def _UpdateUI(self):
		have_image = self.CurrentImageRelpath is not None
		sw = self.window.winfo_width()
		btn_width = 30

		# Menu button is always visible
		self.button_menu.place(anchor="se", relx=1.0, rely=1.0, width=btn_width, height=30)

		# Show the filepath label if the config says so
		if bool(self.config["ShowImageInfo"]) and have_image:
			self.label_filepath.place(anchor="sw", relx=0.0, rely=1.0, width=sw - btn_width, height=30)
		else:
			self.label_filepath.place_forget()

		# Display optional stuff
		if self.ui_visible and have_image:
			self.button_next.place(anchor="e", relx=0.99, rely=0.5, width=btn_width, height=btn_width)
			self.button_prev.place(anchor="w", relx=0.01, rely=0.5, width=btn_width, height=btn_width)
		else:
			self.button_next.place_forget()
			self.button_prev.place_forget()

		# Display the options menu
		if self.ui_visible:
			x,y = self.button_menu.winfo_x(), self.button_menu.winfo_y()
			self.options_panel.place(anchor="se", x = x + self.button_menu.winfo_width(), y = y)

			# Update button states
			btn_state = "normal" if have_image else "disabled"
			self.options_dontshowagain.config(state=btn_state)
			self.options_showhidefilepath.config(text="Hide Filepath" if bool(self.config["ShowImageInfo"]) else "Show Filepath", state=btn_state)
			self.options_shuffle.config(state=btn_state)
			self.options_fullscreen.config(text="Exit FullScreen" if bool(self.config["FullScreen"]) else "FullScreen")
			self.options_displayrate.config(state=btn_state)
			self.options_displayrate.set(self.config['DisplayPeriodSeconds'])
		else:
			self.options_panel.place_forget()

		return

	# Show the options menu
	def _ShowMenu(self):
		self.ui_visible = not self.ui_visible
		self._UpdateUI()
		return

	# Log the displayed image to a file
	def _LogDisplayed(self, fullpath):
		log_filepath = self.root_dir / self.config['DisplayedImageLog']
		if log_filepath is None:
			return
		
		# If the log file is larger than 1MB, keep the last 1000 lines
		if os.path.exists(log_filepath) and os.path.getsize(log_filepath) > 1024 * 1024:
			with open(log_filepath, 'r', encoding='utf-8') as file:
				lines = file.readlines()

			lines = lines[-1000:]

			with open(log_filepath, 'w', encoding='utf-8') as file:
				file.writelines(lines)

		# Append the new image to the log file
		with open(log_filepath, 'a', encoding='utf-8') as file:
			file.write(f"{fullpath}\n")

		return

	# Change the image info display setting
	def _ToggleShowFilePath(self):
		self.config["ShowImageInfo"] = not bool(self.config["ShowImageInfo"])
		self._SaveConfig()

		self._UpdateUI()
		return

	# Toggle the fullscreen setting
	def _FullScreen(self):
		self.config["FullScreen"] = not bool(self.config["FullScreen"])
		self._SaveConfig()

		self.window.attributes("-fullscreen", bool(self.config['FullScreen']))
		self.window.after(10, self._UpdateUI)
		return

	# Set the display rate
	def _ChangeDisplayRate(self, value):
		self.config["DisplayPeriodSeconds"] = int(value)
		self._SaveConfig()
		return

	# Stop an image from being displayed again
	def _DontShowAgain(self):
		# Add the current image to the ignore list
		current_relpath = self.image_list[self.image_index].strip()

		# Update the ignore list
		ignore_patterns = set(self._LoadIgnorePatterns())
		ignore_patterns.add(current_relpath)
		self._SaveIgnorePatterns(ignore_patterns)

		# Remove the image from the image list
		image_list = self._LoadImageList()
		image_list.remove(current_relpath)
		self._SaveImageList(image_list)

		# Remove the image from the loaded image list
		self.image_list.remove(current_relpath)
		self._NextImage()
		return

	# Shuffle the image list
	def _Shuffle(self):
		random.shuffle(self.image_list)
		return

	# Load the config
	def _LoadConfig(self):
		config_file = self.root_dir / f"config.json"
		with open(config_file, "r", encoding="utf-8") as file:
			return json.load(file)

	# Save the config
	def _SaveConfig(self):
		config_file = self.root_dir / f"config.json"
		with open(config_file, "w", encoding="utf-8") as file:
			json.dump(self.config, file, indent=4, ensure_ascii=False)
		return

	# Read the list of images
	def _LoadImageList(self):
		image_list = []

		# Get the image list full path
		image_list_fullpath = self.root_dir / self.config['ImageList']
		if not image_list_fullpath.exists():
			raise FileNotFoundError(f"Image list {image_list_fullpath} does not exist")

		# Load the image list
		with open(image_list_fullpath, "r", encoding="utf-8") as file:
			image_list = [line.strip() for line in file.readlines() if not line.startswith("#")]

		return image_list

	# Save the image list to file
	def _SaveImageList(self, image_list):
		print("Saving image list...", end="")

		# Stable order
		image_list.sort()

		# Write the image list to the file
		image_list_fullpath = self.root_dir / self.config['ImageList']
		with open(image_list_fullpath, 'w', encoding='utf-8') as file:
			for relpath in image_list:
				file.write(f"{relpath}\n")

		print(f"done. Image list: {image_list_fullpath}")
		return

	# Read the list of ignore patterns
	def _LoadIgnorePatterns(self):

		# Get the ignore list full path
		ignore_patterns_fullpath = self.root_dir / self.config['IgnorePatterns']
		if not ignore_patterns_fullpath.exists():
			return []
		
		# Load the ignore list
		with open(ignore_patterns_fullpath, "r", encoding="utf-8") as file:
			ignore_patterns = [line.strip() for line in file.readlines() if not line.startswith("#")]

		# Use: spec = pathspec.PathSpec.from_lines('gitwildmatch', ignore_patterns)
		return ignore_patterns
	
	# Save the list of images to ignore
	def _SaveIgnorePatterns(self, ignore_patterns):
		print("Saving ignore patterns...", end="")

		# Stable order
		ignore_patterns.sort()

		# Write the ignore list to the file
		ignore_patterns_fullpath = self.root_dir / self.config['IgnorePatterns']
		with open(ignore_patterns_fullpath, 'w', encoding='utf-8') as file:
			for pattern in ignore_patterns:
				file.write(f"{pattern}\n")

		print(f"done. Ignore list: {ignore_patterns_fullpath}")
		return

if __name__ == "__main__":
	#sys.argv = [sys.argv[0], "--scan"]  # For testing only
	pic = PictureFrame()
	if len(sys.argv) > 1 and sys.argv[1] == "--scan":
		pic.Scan()
	else:
		pic.Run()
