@echo off

set src_directory      = "P:\PR\PR\Renderer\Effects\Shaders"
set shader_nugget_file = "P:\PR\PR\Renderer\Effects\Shaders\Shaders.nugget"
set shader_header_data = "P:\PR\PR\Renderer\Effects\BuiltInShadersInc.h"

rem Turn the shaders into a nugget file
P:\PR\Tools\Bin\WadMaker -D %src_directory% -O %shader_nugget_file% -V

rem Turn the nugget file into header file data
P:\PR\Tools\Bin\HeaderFileData -F %shader_nugget_file% -O %shader_header_data%
