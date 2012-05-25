#pragma once

// specify how Blender per-face texture coords are handled
enum UVMapping
{
	UV_SimpleMode,      // no vertices are created, texture coords are used from the first face using this vertex
	// may lead to texture artifacts, especially in objects with few vertices/faces
	UV_DuplicateVertex  // if a vertex needs to have more than one texture coordinate pair it will be duplicated
};
