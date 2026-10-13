//**********************************************
// P3D Graphics Tool
//  Copyright (c) Rylogic Ltd 2026
//**********************************************

#pragma once
#include "src/forward.h"

// Cook a '.rig' description against the skeleton in 'p3d' and write the result to 'outfile'.
// The rig is validated, its bones are checked against the skeleton (names, parents and rest transforms), and its bones are reordered to the skeleton's order,
// so that cooked rig bone 'i' is skin skeleton bone 'i'. Throws 'std::runtime_error' listing every problem if the rig cannot be cooked.
void CookRig(pr::geometry::p3d::File const& p3d, std::filesystem::path const& infile, std::filesystem::path const& outfile, int verbosity);
