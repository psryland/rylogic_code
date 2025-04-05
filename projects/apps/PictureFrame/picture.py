# How to profile:
# - python -m cProfile -o profile.out picture.py
# - C:\Users\paulryland\AppData\Roaming\Python\Python311\Scripts\snakeviz.exe profile.out

import os, json, time, cv2
import tkinter as Tk
from PIL import Image, ImageTk
from pathlib import Path

class PictureFrame:
	def __init__(self):
		self.root_dir = Path(__file__).resolve(strict=True).parent

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
			self.image_list = file.readlines()

		# Position in the image list
		self.image_index = -1

		# Create fullscreen window
		self.window = Tk.Tk()
		self.window.title("Picture Frame")
		self.window.geometry("800x600")  # Set initial size
		#self.window.attributes("-fullscreen", True)  # Fullscreen mode
		self.window.bind("<Escape>", lambda e: self.window.quit())  # Exit on ESC key
		self.window.bind("<Button-1>", lambda e: self._MouseHandler(e, e.x, e.y, None, None))  # Left mouse button click 

		# Create a 'label' to show the image in
		self.bb = Tk.Label(self.window, bg="black")
		self.bb.pack(side=Tk.TOP, fill=Tk.BOTH, expand=True)

		# A button options
		self.button_menu = Tk.Button(self.window, text="...", bg="black", fg="white", border=0, command=self._ShowMenu)
		
		# Create a text label to show the file path
		self.label_filepath = Tk.Label(self.window, text="", bg="black", fg="white", font=("Arial", 16))

		# Last frame time
		self.last = 0.0
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
		# Load the image
		image = cv2.imread(image_fullpath)
		if image is None:
			self.window.after(1, self._NextImage)
			return

		self._Render(image)

		delay_ms = 1000 * int(self.config['DisplayPeriodSeconds'])
		self.window.after(delay_ms, self._NextImage)
		return

	def _ShowVideo(self, video_fullpath):

		# Load the video
		video = cv2.VideoCapture(video_fullpath)
		if not video.isOpened():
			video.release()
			self.window.after(1, self._NextImage)
			return
			
		# Get the native frame rate
		fps = video.get(cv2.CAP_PROP_FPS)
		sec_per_frame = 1 / fps if fps > 0 else 0.025  # Fallback to 25ms if FPS is unknown
		self.last = time.perf_counter()

		# Play the video
		def DisplayFrame():
			ret, frame = video.read()
			if not ret:
				video.release()
				self.window.after(1, self._NextImage)
				return

			# Show the frame
			self._Render(frame, spf=sec_per_frame)
			self.window.after(1, DisplayFrame)

		DisplayFrame()
		return

	def _MouseHandler(self, event, x, y, flags, param):
		"""Handle mouse events."""

		if event.type == Tk.EventType.ButtonPress:  # Left mouse button
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
	
	def _Render(self, image, spf=None):
		# Resize image while maintaining aspect ratio
		ih, iw = image.shape[:2]
		sh, sw = self.bb.winfo_height(), self.bb.winfo_width()
		resized_image = cv2.resize(image, self._Scale(iw, ih, sw, sh), interpolation=cv2.INTER_AREA)

		# Convert the image to RGB format for Tkinter
		resized_image = cv2.cvtColor(resized_image, cv2.COLOR_BGR2RGB)
		resized_image = Image.fromarray(resized_image)
		resized_image = ImageTk.PhotoImage(image=resized_image)

		# Limit the frame rate
		if spf is not None:
			elapsed = time.perf_counter() - self.last
			if elapsed < spf: time.sleep(spf - elapsed)

		# Show the image
		self.bb.imgtk = resized_image
		self.bb.configure(image=resized_image)
		self.last = time.perf_counter()

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
