#ifndef PR_DBG_RDR
	#define PR_DBG_RDR  PR_DBG_ON
#endif//PR_DBG_RDR

#if PR_DBG_RDR == 1 && !defined(D3D_DEBUG_INFO)
	#define D3D_DEBUG_INFO
#endif//PR_DBG_RDR

//#define PR_DEBUG_SHADERS
#ifdef PR_DEBUG_SHADERS
	#define PR_DEBUG_SHADERS_ONLY(exp) exp
#else//PR_DEBUG_SHADERS
	#define PR_DEBUG_SHADERS_ONLY(exp)
#endif//PR_DEBUG_SHADERS
