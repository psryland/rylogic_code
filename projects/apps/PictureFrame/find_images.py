import os, json, fnmatch

# Load the config
config_file = os.path.join(os.path.dirname(__file__), 'config.json')
with open(config_file) as file:
	config = json.load(file)

# Get the image root path
root_path = config["RootPath"]
if not os.path.exists(root_path):
	raise FileNotFoundError(f"Root path {root_path} does not exist")

patterns = config['ImagePatterns'] + config['VideoPatterns']

print(f"Root path: {root_path}")
print(f"Include patterns: {patterns}")
print("Scanning...")

# Generate an image list
image_list = []
for dirpath, dirnames, filenames in os.walk(root_path):

	# Get the full paths of the files
	fullpaths = [os.path.abspath(os.path.join(dirpath, filename)) for filename in filenames]

	# Filter files based on the include patterns
	fullpaths = [f for f in fullpaths if any(fnmatch.fnmatch(f, pattern) for pattern in patterns)]
	if not fullpaths:
		continue

	image_list += fullpaths

image_list.sort()

print("Saving...")

# Write the image list to the file
image_list_fullpath = config['ImageList']
with open(image_list_fullpath, 'w') as file:
	for image_fullpath in image_list:

		# Make relative to the root path
		image_relpath = os.path.relpath(image_fullpath, root_path)
		file.write(f"{image_relpath}\n")

print(f"Image list generated: {image_list_fullpath}")
