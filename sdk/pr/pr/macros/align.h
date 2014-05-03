#pragma once

// Waiting for C++ support...
#define alignas(alignment) __declspec(align(alignment))


#define PR_ALIGN(alignment, what) __declspec( align (alignment) ) what

