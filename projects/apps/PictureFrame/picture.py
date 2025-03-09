import os, json, cv2

# Load the config
config_file = os.path.join(os.path.dirname(__file__), 'config.json')
with open(config_file) as file:
	config = json.load(file)

# Get the image root path
root_path = config["RootPath"]
if not os.path.exists(root_path):
	raise FileNotFoundError(f"Root path {root_path} does not exist")

# Get the image list full path
image_list_fullpath = config['ImageList']
if not os.path.exists(image_list_fullpath):
	raise FileNotFoundError(f"Image list {image_list_fullpath} does not exist")

# Load the image list
with open(image_list_fullpath, "r", encoding="utf-8") as file:
	image_list = file.readlines()

# Create fullscreen window
WindowName = "Picture Frame"
cv2.namedWindow(WindowName)#, cv2.WND_PROP_FULLSCREEN)
#cv2.setWindowProperty(WindowName, cv2.WND_PROP_FULLSCREEN, cv2.WINDOW_FULLSCREEN)

# Get window size (returns [x, y, width, height])
x, y, screen_width, screen_height = cv2.getWindowImageRect(WindowName)

# Display the images or videos until shutdown
while True:
	for image_relpath in image_list:
		image_fullpath = os.path.abspath(os.path.join(root_path, image_relpath.strip()))
		if not os.path.exists(image_fullpath):
			continue

		_, extn = os.path.split(image_fullpath)

		# Display image if it is an image file
		if extn.lower().endswith(('.png', '.jpg', '.jpeg', '.bmp')):

			# Load the image
			image = cv2.imread(image_fullpath)

			# Resize image while maintaining aspect ratio
			height, width = image.shape[:2]
			scale = min(screen_width / width, screen_height / height)
			new_size = (int(width * scale), int(height * scale))
			resized_image = cv2.resize(image, new_size, interpolation=cv2.INTER_AREA)

			# Show the image
			cv2.imshow(WindowName, resized_image)

		# Display video if it is a video file
		elif extn.lower().endswith(('.mp4', '.avi', '.mov')):

			# Load the video
			video = cv2.VideoCapture(image_fullpath)

			# Play the video
			while video.isOpened():
				ret, frame = video.read()
				if not ret:
					break

				# Resize frame while maintaining aspect ratio
				width, height = frame.shape[:2]
				scale = min(screen_width / width, screen_height / height)
				new_size = (int(width * scale), int(height * scale))
				resized_frame = cv2.resize(frame, new_size, interpolation=cv2.INTER_AREA)

				# Show the frame
				cv2.imshow(WindowName, resized_frame)

				# Wait for a key press
				if cv2.waitKey(1) & 0xFF == 27:
					break

			# Release the video
			video.release()

		else:
			continue

		# Wait until a key is pressed, then close
		cv2.waitKey(0)

cv2.destroyAllWindows()          # Close window
