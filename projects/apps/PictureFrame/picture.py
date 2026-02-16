# Requires:
#  sudo apt install mpv
#  pip install python-mpv
#
# On RPi,
#   Use ~/.config/mpv/mpv.conf:
#    vo=gpu
#    hwdec=v4l2m2m
#    x11-bypass-compositor=yes
#  Make sure raspi-config->Advanced->Wayland is set to x11
#
# Compile to exe with:
# - pip install pyinstaller
# - C:\Users\paulryland\AppData\Roaming\Python\Python311\Scripts\pyinstaller --onefile --noconsole .\picture.py
# How to profile:
# - python -m cProfile -o profile.out picture.py
# - C:\Users\paulryland\AppData\Roaming\Python\Python311\Scripts\snakeviz.exe profile.out

import sys, os, json, platform, fnmatch, random, pathspec, threading, tempfile, shutil, time
import logging, logging.handlers, traceback
from pathlib import Path
import tkinter as Tk
import mpv

try:
	import psutil
	HAS_PSUTIL = True
except ImportError:
	HAS_PSUTIL = False

def _SetupLogging(log_dir):
	"""Configure logging with rotating file handler and console handler."""
	log_dir.mkdir(parents=True, exist_ok=True)
	log_file = log_dir / "pictureframe.log"

	logger = logging.getLogger("PictureFrame")
	logger.setLevel(logging.DEBUG)

	# Rotating file handler: 1MB per file, keep 10 backups
	file_handler = logging.handlers.RotatingFileHandler(
		log_file, maxBytes=1*1024*1024, backupCount=10, encoding="utf-8")
	file_handler.setLevel(logging.DEBUG)
	file_fmt = logging.Formatter("%(asctime)s [%(levelname)s] %(message)s", datefmt="%Y-%m-%d %H:%M:%S")
	file_handler.setFormatter(file_fmt)

	# Name rotated files with a timestamp instead of numeric suffixes
	def timestamped_namer(default_name):
		base_dir = Path(default_name).parent
		timestamp = time.strftime("%Y%m%d_%H%M%S")
		return str(base_dir / f"pictureframe_{timestamp}.log")
	file_handler.namer = timestamped_namer
	file_handler.rotator = lambda src, dst: Path(src).rename(dst)

	# Console handler
	console_handler = logging.StreamHandler(sys.stdout)
	console_handler.setLevel(logging.INFO)
	console_fmt = logging.Formatter("[%(levelname)s] %(message)s")
	console_handler.setFormatter(console_fmt)

	logger.addHandler(file_handler)
	logger.addHandler(console_handler)
	return logger

def _GetResourceInfo():
	"""Collect current process resource usage."""
	info = {}
	info["thread_count"] = threading.active_count()
	info["thread_names"] = [t.name for t in threading.enumerate()]
	if HAS_PSUTIL:
		try:
			proc = psutil.Process(os.getpid())
			mem = proc.memory_info()
			info["memory_rss_mb"] = round(mem.rss / (1024 * 1024), 1)
			info["memory_vms_mb"] = round(mem.vms / (1024 * 1024), 1)
			info["open_files"] = len(proc.open_files())
			info["num_fds"] = proc.num_handles() if platform.system() == "Windows" else proc.num_fds()
			info["cpu_percent"] = proc.cpu_percent(interval=0)
		except Exception as e:
			info["psutil_error"] = str(e)
	return info

class PictureFrame:
	def __init__(self):
		# Get the current directory
		project_dir = Path(__file__).resolve().parent
		self.root_dir = project_dir

		# Set up logging first so everything else can use it
		self.log = _SetupLogging(project_dir / "logs")
		self.log.info("=" * 60)
		self.log.info("PictureFrame starting up")
		self.log.info(f"Platform: {platform.system()} {platform.release()}")
		self.log.info(f"Python: {sys.version}")
		self.log.info(f"psutil available: {HAS_PSUTIL}")
		self.log.info(f"Project dir: {project_dir}")
		self._images_displayed = 0
		self._plays_since_recycle = 0
		self._start_time = time.time()

		# Install global exception hook to catch unhandled exceptions
		def _unhandled_exception(exc_type, exc_value, exc_tb):
			self.log.critical("UNHANDLED EXCEPTION - this is likely the crash cause:")
			self.log.critical("".join(traceback.format_exception(exc_type, exc_value, exc_tb)))
			self.log.critical(f"Resource state at crash: {_GetResourceInfo()}")
			sys.__excepthook__(exc_type, exc_value, exc_tb)
		sys.excepthook = _unhandled_exception

		self.image_list = []
		self.image_index = -1
		self.ui_visible = False
		self.menu_visible = False
		self.issue_number = 0
		self.pending_after_ids = []  # Track scheduled after callbacks to prevent memory leaks
		self.cleanup_after_id = None  # Separate timer for periodic cache cleanup (not cancelled by image transitions)
		self.resource_monitor_after_id = None  # Separate timer for resource monitoring

		# Prefetch state for loading next media while current is displaying
		self.prefetch_cache_dir = Path(tempfile.gettempdir()) / "pictureframe_cache"
		self.prefetch_cache_dir.mkdir(exist_ok=True)
		self.prefetch_lock = threading.Lock()
		self.prefetch_pending_set = set()  # Set of paths currently being prefetched
		self.prefetch_cache = {}  # Maps source_path -> cached_path for all cached files
		self._LoadCacheManifest()  # Load existing cache from previous sessions
		self.log.info(f"Cache dir: {self.prefetch_cache_dir}")

		# Load the config
		self.config = self._LoadConfig()
		self.log.info(f"Config loaded: DisplayPeriod={self.config['DisplayPeriodSeconds']}s, PrefetchCount={self.config.get('PrefetchCount', 10)}")

		# Get the image root path
		self.image_dir = Path(self.config[f"ImageRoot-{platform.system()}"])
		self.log.info(f"Image dir: {self.image_dir}")
		if not self.image_dir.exists():
			self.log.warning(f"Image root path not available: {self.image_dir}")

		# Create a tk window
		self.window = Tk.Tk()
		self.window.title("Picture Frame")
		self.window.geometry("1024x768")  # Set initial size
		self.window.attributes("-fullscreen", bool(self.config['FullScreen']))

		# Create a backbuffer for images and video
		self.bb = Tk.Frame(self.window, bg="black", borderwidth=0, highlightthickness=0)
		self.bb.pack(side=Tk.TOP, fill=Tk.BOTH, expand=True)

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

		# Initialize player as None - will be created later when window is ready
		self.player = None

		# Key binds
		self.window.bind("<Escape>", lambda e: self._Shutdown())  # Exit on ESC key
		self.window.bind("<Configure>", lambda e: self._UpdateUI())
		self.bb.bind("<Button-1>", self._ShowOverlays)

		self.log.info("PictureFrame initialized successfully")
		return

	# Initialize the MPV player when the window is ready
	def _InitPlayer(self):
		if self.player is not None:
			return
		
		self.log.info("Attempting to initialize MPV player...")
		
		# Force the frame to be displayed and get its actual window ID
		try:
			self.bb.update()
			self.window.update()
			
			# Get the window ID after everything is properly realized
			wid = self.bb.winfo_id()
			self.log.info(f"Window ID obtained: {wid}")
			self.log.info(f"Frame geometry: {self.bb.winfo_width()}x{self.bb.winfo_height()}")
			self.log.info(f"Frame is viewable: {self.bb.winfo_viewable()}")
			
		except Exception as e:
			self.log.error(f"Error getting window information: {e}", exc_info=True)
			wid = None
		
		# Try different MPV configurations in order of preference
		configs = [
			# Config 1: Full embedding with window ID
			{
				"name": "Embedded with window ID",
				"config": {
					"wid": str(wid) if wid else None,
					"input_default_bindings": False,
					"input_vo_keyboard": False,
					"osc": False,
					"ytdl": False,
					"image_display_duration": 60,
					"vo": "x11" if platform.system() == "Linux" else None
				}
			},
			# Config 2: Simple embedded (no geometry control)
			{
				"name": "Simple embedded",
				"config": {
					"wid": str(wid) if wid else None,
					"ytdl": False,
					"image_display_duration": 60,
					"osc": False
				}
			},
			# Config 3: Minimal embedded
			{
				"name": "Minimal embedded",
				"config": {
					"wid": str(wid) if wid else None,
					"ytdl": False,
					"image_display_duration": 60
				}
			},
			# Config 4: Basic MPV (will create separate window)
			{
				"name": "Basic MPV",
				"config": {
					"ytdl": False,
					"image_display_duration": 60
				}
			}
		]
		
		for i, attempt in enumerate(configs):
			if wid is None and "wid" in attempt["config"]:
				self.log.debug(f"Skipping {attempt['name']} - no window ID available")
				continue
				
			try:
				self.log.info(f"Trying configuration {i+1}: {attempt['name']}")
				
				# Filter out None values
				config = {k: v for k, v in attempt["config"].items() if v is not None}
				self.log.debug(f"MPV config: {config}")
				
				self.player = mpv.MPV(**config)
				self.log.info(f"MPV player initialized successfully with {attempt['name']}")
				return
				
			except Exception as e:
				self.log.warning(f"Failed with {attempt['name']}: {e}", exc_info=True)
				if i < len(configs) - 1:
					self.log.info("Trying next configuration...")
				continue
		
		self.log.error("All MPV configurations failed!")
		self.player = None

	# Destroy and recreate the MPV player to reclaim leaked memory
	def _RecyclePlayer(self):
		recycle_interval = self.config.get('PlayerRecycleInterval', 200)
		if self._plays_since_recycle < recycle_interval:
			return

		self.log.info(f"Recycling MPV player after {self._plays_since_recycle} plays to reclaim memory")
		self._StopMedia()
		if self.player is not None:
			try:
				self.player.terminate()
			except Exception as e:
				self.log.error(f"Error terminating player during recycle: {e}", exc_info=True)
			self.player = None
		self._plays_since_recycle = 0
		self._InitPlayer()

	# Scan for images and videos
	def Scan(self):
		patterns = self.config['ImagePatterns'] + self.config['VideoPatterns']

		self.log.info(f"Image root path: {self.image_dir}")
		self.log.info(f"Include patterns: {patterns}")

		self.log.info("Loading ignore list...")
		ignore_patterns = self._LoadIgnorePatterns()
		ignore_spec = pathspec.PathSpec.from_lines('gitwildmatch', ignore_patterns)

		self.log.info("Scanning...")

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

		# Init the UI
		self._UpdateUI()

		# Start periodic cache cleanup (every 10 minutes)
		self._SchedulePeriodicCacheCleanup()

		# Start periodic resource monitoring (every 60 seconds)
		self._ScheduleResourceMonitor()

		# Wait for the image directory to become available (e.g. network share mounted)
		self._WaitForImageDir()
		self.window.mainloop()

	# Poll until the image directory is accessible, then start displaying
	def _WaitForImageDir(self):
		if not self.image_dir.exists():
			self.log.warning(f"Waiting for image directory: {self.image_dir}")
			self.no_images_label.configure(text=f"Waiting for:\n{self.image_dir}")
			self.no_images_label.place(anchor="center", relx=0.5, rely=0.5)
			self._ScheduleAfter(5000, self._WaitForImageDir)
			return

		self.no_images_label.place_forget()
		self.log.info(f"Image directory available: {self.image_dir}")

		# Load the image list
		self.image_list = self._LoadImageList()
		self.log.info(f"Loaded {len(self.image_list)} images")
		self._Shuffle()

		# Wait for the window to be viewable before starting playback
		def WaitTillShown():
			if self.bb.winfo_viewable() == 0:
				self.window.after(500, WaitTillShown)
				return

			# Initialize the MPV player now that the window is ready
			self._InitPlayer()
			
			# Start displaying images
			self._NextImage()
			return

		WaitTillShown()

	# Current image relative path (or None)
	@property
	def CurrentImageRelpath(self):
		return self.image_list[self.image_index].strip() if self.image_index >= 0 and self.image_index < len(self.image_list) else None

	# Display the next image
	def _NextImage(self):
		self._CancelPendingAfterCallbacks()  # Cancel any pending timers before showing new image
		self.issue_number += 1
		try:
			fullpath, relpath = self._FindMedia(+1)
			self._DisplayMedia(fullpath, relpath, self.issue_number, +1)
		except Exception as e:
			self.log.error(f"Error in _NextImage: {e}", exc_info=True)
			self._ScheduleAfter(5000, self._NextImage)
		return

	# Display the previous image
	def _PrevImage(self):
		self._CancelPendingAfterCallbacks()  # Cancel any pending timers before showing new image
		self.issue_number += 1
		try:
			fullpath, relpath = self._FindMedia(-1)
			self._DisplayMedia(fullpath, relpath, self.issue_number, -1)
		except Exception as e:
			self.log.error(f"Error in _PrevImage: {e}", exc_info=True)
			self._ScheduleAfter(5000, self._PrevImage)
		return

	# Find the next/previous image in the list
	def _FindMedia(self, increment):

		# Quick check: if the image directory itself is unreachable, don't iterate
		# all files (each exists() call on a dead CIFS mount blocks for the timeout)
		try:
			if not self.image_dir.exists():
				self.log.warning(f"Image directory not accessible: {self.image_dir}")
				return Path(), Path()
		except OSError as e:
			self.log.warning(f"Image directory check failed: {e}")
			return Path(), Path()

		# Limit the search to test every image in the list
		for i in range(len(self.image_list)):

			# Increment the image index
			self.image_index = (self.image_index + len(self.image_list) + increment) % len(self.image_list)

			relpath = self.CurrentImageRelpath
			fullpath = (self.image_dir / relpath).resolve()

			try:
				if fullpath.exists():
					return fullpath, relpath
			except OSError:
				# Network path errors should not block the loop
				continue

		return Path(), Path()

	# Display the image/video at 'self.image_index'
	def _DisplayMedia(self, fullpath, relpath, issue_number, direction=1):
		# Kill any call that doesn't match the issue number
		if issue_number != self.issue_number:
			return

		self._images_displayed += 1
		self._plays_since_recycle += 1

		# Stop any existing media and recycle MPV if needed to reclaim memory
		self._StopMedia()
		self._RecyclePlayer()

		# Set the filepath label text
		self.label_filepath.configure(text=relpath)

		# Display a no images message if there are no images
		self.no_images_label.place_forget()
		if not fullpath or fullpath == Path():
			self.log.warning("No accessible media found, will retry in 5 seconds")
			self.no_images_label.configure(text=f"Waiting for:\n{self.image_dir}")
			self.no_images_label.place(anchor="center", relx=0.5, rely=0.5)
			self._ScheduleAfter(5000, self._NextImage)
			return

		# Add the image to the displayed image log
		self._LogDisplayed(fullpath)

		# Determine the file type
		extn = fullpath.suffix.lower()

		# Display image if it is an image file
		try:
			if extn in ['.png', '.jpg', '.jpeg', '.bmp']:
				self._ShowImage(fullpath, issue_number, direction)
			elif extn in ['.mp4', '.avi', '.mov']:
				self._ShowVideo(fullpath, issue_number, direction)
			else:
				self.log.warning(f"Unsupported file type: {extn} for {fullpath}")
				self._ScheduleAfter(100, self._NextImage)
		except Exception as e:
			self.log.error(f"Error displaying media {fullpath}: {e}", exc_info=True)
			self._ScheduleAfter(100, self._NextImage)
		return

	# Display a still image
	def _ShowImage(self, image_fullpath, issue_number, direction=1):
		if self.player is None:
			self.log.warning("MPV player not initialized, skipping image display")
			self._ScheduleAfter(100, self._NextImage)
			return
			
		def Stop():
			if issue_number != self.issue_number: return
			self._StopMedia()
			self._NextImage()
			return

		try:
			# Use cached file if available, otherwise use original path
			playback_path = self._GetPlaybackPath(image_fullpath)
			self.player.play(str(playback_path))
			
			# Start prefetching the next image in the current navigation direction
			self._PrefetchNext(direction)
			
			self._ScheduleAfter(1000 * int(self.config['DisplayPeriodSeconds']), Stop)
		except Exception as e:
			self.log.error(f"Error playing image {image_fullpath}: {e}", exc_info=True)
			self._ScheduleAfter(100, self._NextImage)
		return

	# Display a video
	def _ShowVideo(self, video_fullpath, issue_number, direction=1):
		if self.player is None:
			self.log.warning("MPV player not initialized, skipping video display")
			self._ScheduleAfter(100, self._NextImage)
			return
			
		def Stop():
			if issue_number != self.issue_number: return
			try:
				if self.player and self.player.eof_reached == False:
					self._ScheduleAfter(500, Stop)
					return
			except Exception as e:
				self.log.error(f"Error checking eof_reached: {e}", exc_info=True)
			self._StopMedia()
			self._NextImage()
			return

		try:
			# Use cached file if available, otherwise use original path
			playback_path = self._GetPlaybackPath(video_fullpath)
			self.player.play(str(playback_path))
			
			# Start prefetching the next media in the current navigation direction
			self._PrefetchNext(direction)
			
			self._ScheduleAfter(500, Stop)
		except Exception as e:
			self.log.error(f"Error playing video {video_fullpath}: {e}", exc_info=True)
			self._ScheduleAfter(100, self._NextImage)
		return

	# Stop and clean up any media
	def _StopMedia(self):
		if threading.current_thread() != threading.main_thread():
			raise Exception("ReleaseMedia called from non-main thread")

		if self.player is not None:
			try:
				self.player.stop()
			except Exception as e:
				self.log.error(f"Error stopping player: {e}", exc_info=True)
		return

	# Show/Hide UI elements
	def _UpdateUI(self):
		have_image = self.CurrentImageRelpath is not None
		sw = self.window.winfo_width()
		btn_width = 30

		# Show/hide the mouse
		if self.ui_visible:
			self.window.config(cursor="arrow")
		else:
			self.window.config(cursor="none")

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
		if self.menu_visible:
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

	# Show the forward/backward buttons
	def _ShowOverlays(self, event=None):
		self.ui_visible = not self.ui_visible
		self._UpdateUI()
		return

	# Show the options menu
	def _ShowMenu(self):
		self.menu_visible = not self.menu_visible
		self.ui_visible = self.menu_visible
		self._UpdateUI()
		return

	# Log the displayed image to a file
	def _LogDisplayed(self, fullpath):
		log_filepath = self.root_dir / self.config['DisplayedImageLog']
		if log_filepath is None:
			return

		# Show displayed images in the terminal
		if bool(self.config["LogToTerminal"]):
			self.log.info(f"Displaying: {fullpath}")

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
			self.log.warning(f"Image list not found: {image_list_fullpath}")
			return image_list

		# Load the image list
		with open(image_list_fullpath, "r", encoding="utf-8") as file:
			image_list = [line.strip() for line in file.readlines() if not line.startswith("#")]

		return image_list

	# Save the image list to file
	def _SaveImageList(self, image_list):
		self.log.info(f"Saving image list ({len(image_list)} images)...")

		# Stable order
		image_list.sort()

		# Write the image list to the file
		image_list_fullpath = self.root_dir / self.config['ImageList']
		with open(image_list_fullpath, 'w', encoding='utf-8') as file:
			for relpath in image_list:
				file.write(f"{relpath}\n")

		self.log.info(f"Image list saved: {image_list_fullpath}")
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
		self.log.info(f"Saving ignore patterns ({len(ignore_patterns)} patterns)...")

		# Stable order
		ignore_patterns.sort()

		# Write the ignore list to the file
		ignore_patterns_fullpath = self.root_dir / self.config['IgnorePatterns']
		with open(ignore_patterns_fullpath, 'w', encoding='utf-8') as file:
			for pattern in ignore_patterns:
				file.write(f"{pattern}\n")

		self.log.info(f"Ignore list saved: {ignore_patterns_fullpath}")
		return

	# Start prefetching multiple media files ahead in the background
	def _PrefetchNext(self, direction=1):
		prefetch_count = self.config.get('PrefetchCount', 10)
		
		for i in range(1, prefetch_count + 1):
			increment = direction * i
			self._PrefetchFile(increment)
		return

	# Prefetch a single file at the given offset from current index
	def _PrefetchFile(self, increment):
		# Calculate what the target image index will be
		target_index = (self.image_index + len(self.image_list) + increment) % len(self.image_list)
		if target_index < 0 or target_index >= len(self.image_list):
			return
		
		target_relpath = self.image_list[target_index].strip()
		target_fullpath = (self.image_dir / target_relpath).resolve()
		
		if not target_fullpath.exists():
			return
		
		with self.prefetch_lock:
			# Already cached?
			if target_fullpath in self.prefetch_cache:
				return
			
			# Already prefetching this file?
			if target_fullpath in self.prefetch_pending_set:
				return
			
			self.prefetch_pending_set.add(target_fullpath)
		
		# Start background thread to copy the file
		thread = threading.Thread(
			target=self._PrefetchWorker,
			args=(target_fullpath,),
			daemon=True
		)
		thread.start()
		return

	# Background worker that copies a file to local cache
	def _PrefetchWorker(self, source_path):
		try:
			# Generate a unique cached filename (include hash to handle duplicate names)
			cache_name = f"{hash(str(source_path)) & 0xFFFFFFFF:08x}_{source_path.name}"
			cached_path = self.prefetch_cache_dir / cache_name
			
			# Copy the file from network to local cache
			shutil.copy2(source_path, cached_path)
			
			with self.prefetch_lock:
				# Add to cache dictionary and remove from pending set
				self.prefetch_cache[source_path] = cached_path
				self.prefetch_pending_set.discard(source_path)
				self.log.debug(f"Prefetched: {source_path.name} (cache size: {len(self.prefetch_cache)})")
			
			# Save manifest after successful prefetch and enforce size limit
			self._SaveCacheManifest()
			self._EnforceCacheSizeLimit()
		except Exception as e:
			self.log.error(f"Prefetch failed for {source_path}: {e}", exc_info=True)
			with self.prefetch_lock:
				self.prefetch_pending_set.discard(source_path)
		return

	# Get the cached path for a media file, or the original if not cached
	def _GetPlaybackPath(self, fullpath):
		with self.prefetch_lock:
			if fullpath in self.prefetch_cache:
				return self.prefetch_cache[fullpath]
		return fullpath

	# Load cache manifest from previous session
	def _LoadCacheManifest(self):
		manifest_path = self.prefetch_cache_dir / "manifest.json"
		try:
			if not manifest_path.exists():
				return
			
			with open(manifest_path, "r", encoding="utf-8") as f:
				manifest = json.load(f)
			
			# Validate that cached files still exist and populate the cache dictionary
			for source_str, cached_str in manifest.items():
				source_path = Path(source_str)
				cached_path = Path(cached_str)
				if cached_path.exists():
					self.prefetch_cache[source_path] = cached_path
			
			# log may not be set up yet during __init__, use print as fallback
			msg = f"Loaded {len(self.prefetch_cache)} cached files from previous session"
			if hasattr(self, 'log'):
				self.log.info(msg)
			else:
				print(msg)
		except Exception as e:
			msg = f"Error loading cache manifest: {e}"
			if hasattr(self, 'log'):
				self.log.error(msg, exc_info=True)
			else:
				print(msg)
		return

	# Save cache manifest for future sessions
	def _SaveCacheManifest(self):
		manifest_path = self.prefetch_cache_dir / "manifest.json"
		try:
			with self.prefetch_lock:
				manifest = {str(k): str(v) for k, v in self.prefetch_cache.items()}
			
			with open(manifest_path, "w", encoding="utf-8") as f:
				json.dump(manifest, f, indent=2)
		except Exception as e:
			self.log.error(f"Error saving cache manifest: {e}", exc_info=True)
		return

	# Clean up old files in the prefetch cache directory
	def _CleanupOldCacheFiles(self):
		try:
			if not self.prefetch_cache_dir or not self.prefetch_cache_dir.exists():
				return
			
			max_age_seconds = self.config.get('CacheCleanupMinutes', 10) * 60
			now = time.time()
			files_removed = 0
			for cached_file in self.prefetch_cache_dir.iterdir():
				if cached_file.is_file() and cached_file.name != "manifest.json":

					# Use creation time, not mtime which shutil.copy2 preserves from the source
					file_age = now - cached_file.stat().st_ctime
					if file_age > max_age_seconds:
						with self.prefetch_lock:
							keys_to_remove = [k for k, v in self.prefetch_cache.items() if v == cached_file]
							for key in keys_to_remove:
								del self.prefetch_cache[key]
						cached_file.unlink()
						files_removed += 1
			
			if files_removed > 0:
				self.log.info(f"Cache cleanup: removed {files_removed} old files (cache size: {len(self.prefetch_cache)})")
				self._SaveCacheManifest()

			# Also enforce the cache size limit
			self._EnforceCacheSizeLimit()
		except Exception as e:
			self.log.error(f"Error cleaning old cache files: {e}", exc_info=True)
		return

	# Enforce the maximum cache size limit by evicting oldest files first
	def _EnforceCacheSizeLimit(self):
		max_size_bytes = self.config.get('CacheMaxSizeMB', 1024) * 1024 * 1024
		try:
			# Collect all cached files with size and creation time
			cached_files = []
			for cached_file in self.prefetch_cache_dir.iterdir():
				if cached_file.is_file() and cached_file.name != "manifest.json":
					stat = cached_file.stat()
					cached_files.append((cached_file, stat.st_size, stat.st_ctime))

			total_size = sum(size for _, size, _ in cached_files)
			if total_size <= max_size_bytes:
				return

			# Evict oldest files first (by creation time)
			cached_files.sort(key=lambda x: x[2])
			remaining_count = len(cached_files)
			files_removed = False
			for cached_file, file_size, _ in cached_files:
				if total_size <= max_size_bytes:
					break

				# Allow a single file to exceed the limit
				if remaining_count <= 1:
					break

				with self.prefetch_lock:
					keys_to_remove = [k for k, v in self.prefetch_cache.items() if v == cached_file]
					for key in keys_to_remove:
						del self.prefetch_cache[key]

				cached_file.unlink()
				total_size -= file_size
				remaining_count -= 1
				files_removed = True
				print(f"Evicted from cache (size limit): {cached_file.name}")

			if files_removed:
				self._SaveCacheManifest()
				print(f"Cache size after eviction: {total_size / (1024*1024):.1f} MB")
		except Exception as e:
			print(f"Error enforcing cache size limit: {e}")
		return

	# Schedule periodic cache cleanup (uses a separate timer so image transitions don't cancel it)
	def _SchedulePeriodicCacheCleanup(self):
		cleanup_interval_ms = self.config.get('CacheCleanupMinutes', 10) * 60 * 1000
		self._CleanupOldCacheFiles()
		self.cleanup_after_id = self.window.after(cleanup_interval_ms, self._SchedulePeriodicCacheCleanup)
		return

	# Periodic resource monitoring - logs process health every 60 seconds
	def _ScheduleResourceMonitor(self):
		self._LogResourceStatus()
		self.resource_monitor_after_id = self.window.after(60_000, self._ScheduleResourceMonitor)
		return

	def _LogResourceStatus(self):
		try:
			res = _GetResourceInfo()
			uptime = time.time() - self._start_time

			# Calculate cache disk usage
			cache_disk_mb = 0.0
			try:
				if self.prefetch_cache_dir.exists():
					cache_disk_mb = sum(
						f.stat().st_size for f in self.prefetch_cache_dir.iterdir() if f.is_file()
					) / (1024 * 1024)
			except Exception:
				pass

			with self.prefetch_lock:
				cache_entries = len(self.prefetch_cache)
				pending_prefetches = len(self.prefetch_pending_set)
			pending_afters = len(self.pending_after_ids)

			parts = [
				f"uptime={uptime/3600:.1f}h",
				f"displayed={self._images_displayed}",
				f"threads={res.get('thread_count', '?')}",
				f"pending_afters={pending_afters}",
				f"cache_entries={cache_entries}",
				f"cache_disk={cache_disk_mb:.1f}MB",
				f"pending_prefetch={pending_prefetches}",
			]
			if "memory_rss_mb" in res:
				parts.append(f"rss={res['memory_rss_mb']}MB")
				parts.append(f"vms={res['memory_vms_mb']}MB")
			if "num_fds" in res:
				parts.append(f"handles={res['num_fds']}")
			if "open_files" in res:
				parts.append(f"open_files={res['open_files']}")
			if "cpu_percent" in res:
				parts.append(f"cpu={res['cpu_percent']}%")

			self.log.info(f"RESOURCE MONITOR: {', '.join(parts)}")

			# Log thread names at debug level for diagnostics
			self.log.debug(f"Active threads: {res.get('thread_names', [])}")

			# Warn if resources look concerning
			if res.get("memory_rss_mb", 0) > 500:
				self.log.warning(f"HIGH MEMORY: RSS={res['memory_rss_mb']}MB")
			if res.get("thread_count", 0) > 50:
				self.log.warning(f"HIGH THREAD COUNT: {res['thread_count']}")
			if res.get("num_fds", 0) > 500:
				self.log.warning(f"HIGH HANDLE COUNT: {res['num_fds']}")
			if cache_disk_mb > 1024:
				self.log.warning(f"LARGE CACHE: {cache_disk_mb:.0f}MB on disk")
		except Exception as e:
			self.log.error(f"Error in resource monitor: {e}", exc_info=True)
		return

	def _Shutdown(self):
		self.log.info("Shutting down...")
		self.log.info(f"Final resource state: {_GetResourceInfo()}")
		uptime = time.time() - self._start_time
		self.log.info(f"Uptime: {uptime/3600:.1f} hours, images displayed: {self._images_displayed}")

		# Cancel all pending after callbacks to prevent leaks
		self._CancelPendingAfterCallbacks()
		if self.cleanup_after_id is not None:
			try:
				self.window.after_cancel(self.cleanup_after_id)
			except (ValueError, Tk.TclError):
				pass
		if self.resource_monitor_after_id is not None:
			try:
				self.window.after_cancel(self.resource_monitor_after_id)
			except (ValueError, Tk.TclError):
				pass
		self._StopMedia()
		if self.player is not None:
			try:
				self.player.terminate()
			except Exception as e:
				self.log.error(f"Error terminating player: {e}", exc_info=True)
		self.log.info("Shutdown complete")
		self.window.quit()
		return

	# Schedule a callback with 'after' and track it for cancellation
	def _ScheduleAfter(self, delay_ms, callback):
		after_id = None
		
		def wrapped_callback():
			# Remove this callback's ID from the tracking list when it executes
			if after_id in self.pending_after_ids:
				self.pending_after_ids.remove(after_id)
			callback()
		
		after_id = self.window.after(delay_ms, wrapped_callback)
		self.pending_after_ids.append(after_id)
		return after_id

	# Cancel all pending after callbacks to prevent memory leaks
	def _CancelPendingAfterCallbacks(self):
		for after_id in self.pending_after_ids:
			try:
				self.window.after_cancel(after_id)
			except (ValueError, Tk.TclError):
				pass  # Ignore errors from already-executed or invalid callbacks
		self.pending_after_ids.clear()
		return

if __name__ == "__main__":
	#sys.argv = [sys.argv[0], "--scan"]  # For testing only
	pic = None
	try:
		pic = PictureFrame()
		if len(sys.argv) > 1 and sys.argv[1] == "--scan":
			pic.Scan()
		else:
			pic.Run()
	except Exception:
		# Ensure the crash is captured in the log file
		if pic and hasattr(pic, 'log'):
			pic.log.critical(f"FATAL CRASH:\n{traceback.format_exc()}")
			pic.log.critical(f"Resource state at crash: {_GetResourceInfo()}")
		else:
			# Logging not yet initialized, write a crash file as a last resort
			crash_path = Path(__file__).resolve().parent / "logs" / "crash.log"
			crash_path.parent.mkdir(parents=True, exist_ok=True)
			with open(crash_path, "a", encoding="utf-8") as f:
				f.write(f"\n{'='*60}\n{time.strftime('%Y-%m-%d %H:%M:%S')}\n")
				f.write(traceback.format_exc())
		raise
