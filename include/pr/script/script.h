//**********************************
// Script Reader
//  Copyright (c) Rylogic Ltd 2007
//**********************************
#pragma once

#include "pr/script/script_core.h"
#include "pr/script/keywords.h"
#include "pr/script/char_stream.h"
#include "pr/script/stream_stack.h"
#include "pr/script/newline_strip.h"
#include "pr/script/comment_strip.h"
#include "pr/script/pp_macro.h"
#include "pr/script/pp_macro_db.h"
#include "pr/script/includes.h"
#include "pr/script/preprocessor.h"
#include "pr/script/token.h"
#include "pr/script/tokeniser.h"

#pragma message("TODO: change pr::script::Reader to use wchar_t")