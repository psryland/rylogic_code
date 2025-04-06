# Requires:
#  pip install python-vlc
#  pip install pywin32
#
# Compile to exe with:
# - pip install pyinstaller
# - C:\Users\paulryland\AppData\Roaming\Python\Python311\Scripts\pyinstaller --onefile --noconsole .\picture.py
# How to profile:
# - python -m cProfile -o profile.out picture.py
# - C:\Users\paulryland\AppData\Roaming\Python\Python311\Scripts\snakeviz.exe profile.out

import sys, os, json, platform
from pathlib import Path
import tkinter as Tk
import vlc

class PictureFrame:
	def __init__(self):
		# Get the current directory
		project_dir = Path(__file__).resolve().parent
		self.root_dir = project_dir

		# Load the config
		config_file = self.root_dir / "config.json"
		with open(config_file, "r", encoding="utf-8") as file:
			self.config = json.load(file)

		# Get the image root path
		self.root_path = Path(self.config["RootPath"])
		if not self.root_path.exists():
			raise FileNotFoundError(f"Root path {self.root_path} does not exist")

		# Get the image list full path
		image_list_fullpath = Path(self.config['ImageList'])
		if not image_list_fullpath.exists():
			raise FileNotFoundError(f"Image list {image_list_fullpath} does not exist")

		# Load the image list
		with open(image_list_fullpath, "r", encoding="utf-8") as file:
			self.image_list = [line for line in file.readlines() if line.startswith("#") == 0]

		# Position in the image list
		self.image_index = -1

		# Create a vlc player
		self.vlc = vlc.Instance()
		self.player = self.vlc.media_player_new()

		# Create fullscreen window
		self.window = Tk.Tk()
		self.window.title("Picture Frame")
		self.window.geometry("800x600")  # Set initial size
		#self.window.attributes("-fullscreen", True)  # Fullscreen mode
		self.window.bind("<Escape>", lambda e: self.window.quit())  # Exit on ESC key
		#self.window.bind("<Button-1>", lambda e: self._MouseHandler(e, e.x, e.y, None, None))

		# Create a panel for images and video
		self.bb = Tk.Frame(self.window, bg="black", borderwidth=0, highlightthickness=0)
		self.bb.pack(side=Tk.TOP, fill=Tk.BOTH, expand=True)
		#self.bb.bind("<Button-1>", lambda e: self._MouseHandler(e, e.x, e.y, None, None))

		# Set the video output to the Tkinter window
		if platform.system() == 'Windows':
			self.player.set_hwnd(self.bb.winfo_id())
			## Set the window to be transparent to mouse events
			#import win32gui, win32con
			#hwnd = win32gui.GetWindow(self.player.get_hwnd(), win32con.GW_CHILD)
			#ex_style = win32gui.GetWindowLong(hwnd, win32con.GWL_EXSTYLE)
			#win32gui.SetWindowLong(hwnd, win32con.GWL_EXSTYLE, ex_style | win32con.WS_EX_LAYERED | win32con.WS_EX_TRANSPARENT)
		elif platform.system() == 'Linux':
			self.player.set_xwindow(self.bb.winfo_id())
		elif platform.system() == 'Darwin':  # macOS
			self.player.set_nsobject(self.bb.winfo_id())

		# Transparent overlay for events
		#self.overlay = Tk.Frame(self.bb, bg="", borderwidth=0, highlightthickness=0)
		#self.overlay.place(relx=0, rely=0, relwidth=1, relheight=1)
		#self.overlay.lift()
		#self.overlay.bind("<Button-1>", lambda e: self._MouseHandler(e, e.x, e.y, None, None))

		# Create a text label to show the file path
		self.label_filepath = Tk.Label(self.window, text="", bg="black", fg="white", font=("Arial", 16))

		# A button to show the options menu
		self.button_menu = Tk.Button(self.window, text="...", bg="black", fg="white", border=0, command=self._ShowMenu)

		self.ui_visible = False
		return

	def Run(self):
		self._NextImage()
		self.window.mainloop()

	def _NextImage(self):
		# Check if the window is still open
		if self.bb.winfo_viewable() == 0:
			self.window.after(100, self._NextImage)
			return

		# Get the next image/video to display
		while True:
			self.image_index = (self.image_index + 1) % len(self.image_list)
			image_relpath = self.image_list[self.image_index].strip()
			image_fullpath = (self.root_path / image_relpath).resolve()
			if image_fullpath.exists():
				break

		# Set the filepath label text
		self.label_filepath.configure(text=image_relpath)

		# Add the image to the displayed image log
		self._LogDisplayedImage(image_fullpath)

		# Determine the file type
		_, extn = os.path.split(image_fullpath)

		# Display image if it is an image file
		if extn.lower().endswith(('.png', '.jpg', '.jpeg', '.bmp')):
			self._ShowImage(image_fullpath)
		elif extn.lower().endswith(('.mp4', '.avi', '.mov')):
			self._ShowVideo(image_fullpath)
		else:
			self.window.after(1, self._NextImage)
		return

	def _ShowImage(self, image_fullpath):
		def Stop():
			self.player.stop()
			self.player.set_media(None)
			self.window.after(1, self._NextImage)

		image = self.vlc.media_new(image_fullpath)
		self.player.set_media(image)
		self.player.play()
		self.window.after(1000 * int(self.config['DisplayPeriodSeconds']), Stop)
		return

	def _ShowVideo(self, video_fullpath):
		def Stop():
			if self.player.is_playing():
				self.window.after(500, Stop)
				return
			self.player.stop()
			self.player.set_media(None)
			self.window.after(1, self._NextImage)
			return

		video = self.vlc.media_new(video_fullpath)
		self.player.set_media(video)
		self.player.play()
		self.window.after(500, Stop)
		return

	def _MouseHandler(self, event, x, y, flags, param):
		if event.type == Tk.EventType.ButtonPress:
			self.ui_visible = not self.ui_visible

			# Show other UI elements
			if self.ui_visible:
				btn_width = 30
				sw = self.window.winfo_width()
				self.label_filepath.place(anchor="sw", relx=0.0, rely=1.0, width=sw - btn_width, height=30)
				self.button_menu.place(anchor="se", relx=1.0, rely=1.0, width=btn_width, height=30)
			else:
				self.label_filepath.place_forget()
				self.button_menu.place_forget()
		return

	def _ShowMenu(self):
		pass

	def _LogDisplayedImage(self, image_fullpath):
		log_filepath = self.config['DisplayedImageLog']
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
			file.write(f"{image_fullpath}\n")

		return

	def _Scale(self, iw, ih, sw, sh):
		"""Scale the image to fit the screen while maintaining aspect ratio."""
		scale = min(sw / iw, sh / ih)
		return max(1, int(iw * scale)), max(1, int(ih * scale))

	def _BlitCentred(self, back_buffer, image):
		"""Blit the image onto the back buffer at the centre."""
		ih, iw = image.shape[:2]
		sh, sw = back_buffer.shape[:2]
		x_offset = (sw - iw) // 2
		y_offset = (sh - ih) // 2
		back_buffer[y_offset:y_offset + ih, x_offset:x_offset + iw] = image
		return

	def _ClearBackBuffer(self, back_buffer):
		"""Clear the back buffer to black."""
		back_buffer[:, :] = 0
		return

pic = PictureFrame()
pic.Run()
