import sys, os, subprocess
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "script")))
import Rylogic as Tools
import UserVars

# Build .proto files into C# classes
def CompileProto(proto_file:str):
	# Get the full path to the proto file
	proto_file = os.path.abspath(proto_file)

	# Get the directory it's in
	dirname = os.path.split(proto_file)[0]

	# Run the compiler
	Tools.Exec([UserVars.protoc, "-I="+dirname, "--csharp_out="+dirname, "--grpc_out="+dirname, "--plugin=protoc-gen-grpc="+UserVars.grpc_csharp_plugin, proto_file])
	return

if __name__ == "__main__":
	try:
		proj_dir = UserVars.root + "\\projects\\LDraw.API"

		if len(sys.argv) > 1:
			CompileProto(sys.argv[1])
		else:
			for dirname,_,filenames in os.walk(proj_dir+"\\src\\proto"):
				for filename in filenames:
					# Look for files with .proto extensions
					fname,ext = os.path.splitext(filename)
					if not ext == ".proto": continue

					# Build .proto files into C# classes
					CompileProto(os.path.join(dirname, filename))

		sys.exit(0)

	except Exception as ex:
		print(str(ex))
		sys.exit(-1)