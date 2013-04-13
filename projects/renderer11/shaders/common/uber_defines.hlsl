//***********************************************
// Renderer
//  Copyright © Rylogic Ltd 2010
//***********************************************
// Setup defaults for defines
#ifndef PR_RDR_SHADER_UBER_DEFINES_HLSL
#define PR_RDR_SHADER_UBER_DEFINES_HLSL

#ifndef PR_RDR_SHADER_DEFAULT
#define PR_RDR_SHADER_DEFAULT 0
#endif

// Define defaults
#ifndef PR_RDR_SHADER_VS
#define PR_RDR_SHADER_VS PR_RDR_SHADER_DEFAULT
#endif
#ifndef PR_RDR_SHADER_PS
#define PR_RDR_SHADER_PS 0
#endif
#ifndef PR_RDR_SHADER_CBUFFRAME
#define PR_RDR_SHADER_CBUFFRAME 1
#endif
#ifndef PR_RDR_SHADER_CBUFMODEL
#define PR_RDR_SHADER_CBUFMODEL 1
#endif
#ifndef PR_RDR_SHADER_VSIN_POS3
#define PR_RDR_SHADER_VSIN_POS3 PR_RDR_SHADER_DEFAULT
#endif
#ifndef PR_RDR_SHADER_VSIN_NORM3
#define PR_RDR_SHADER_VSIN_NORM3 PR_RDR_SHADER_DEFAULT
#endif
#ifndef PR_RDR_SHADER_VSIN_DIFF0
#define PR_RDR_SHADER_VSIN_DIFF0 PR_RDR_SHADER_DEFAULT
#endif
#ifndef PR_RDR_SHADER_VSIN_2DTEX0
#define PR_RDR_SHADER_VSIN_2DTEX0 PR_RDR_SHADER_DEFAULT
#endif
#ifndef PR_RDR_SHADER_VSOUT_SSPOS4
#define PR_RDR_SHADER_VSOUT_SSPOS4 PR_RDR_SHADER_DEFAULT
#endif
#ifndef PR_RDR_SHADER_VSOUT_WSPOS4
#define PR_RDR_SHADER_VSOUT_WSPOS4 PR_RDR_SHADER_DEFAULT
#endif
#ifndef PR_RDR_SHADER_VSOUT_WSNORM4
#define PR_RDR_SHADER_VSOUT_WSNORM4 PR_RDR_SHADER_DEFAULT
#endif
#ifndef PR_RDR_SHADER_VSOUT_DIFF0
#define PR_RDR_SHADER_VSOUT_DIFF0 PR_RDR_SHADER_DEFAULT
#endif
#ifndef PR_RDR_SHADER_VSOUT_2DTEX0
#define PR_RDR_SHADER_VSOUT_2DTEX0 PR_RDR_SHADER_DEFAULT
#endif
#ifndef PR_RDR_SHADER_TXFM
#define PR_RDR_SHADER_TXFM PR_RDR_SHADER_DEFAULT
#endif
#ifndef PR_RDR_SHADER_TXFMWS
#define PR_RDR_SHADER_TXFMWS PR_RDR_SHADER_DEFAULT
#endif
#ifndef PR_RDR_SHADER_TINT0
#define PR_RDR_SHADER_TINT0 PR_RDR_SHADER_DEFAULT
#endif
#ifndef PR_RDR_SHADER_DIFF0
#define PR_RDR_SHADER_DIFF0 PR_RDR_SHADER_DEFAULT
#endif
#ifndef PR_RDR_SHADER_TEX0
#define PR_RDR_SHADER_TEX0 PR_RDR_SHADER_DEFAULT
#endif
#ifndef PR_RDR_SHADER_PVC
#define PR_RDR_SHADER_PVC PR_RDR_SHADER_DEFAULT
#endif
#ifndef PR_RDR_SHADER_LIGHTING
#define PR_RDR_SHADER_LIGHTING PR_RDR_SHADER_DEFAULT
#endif

// Macro support
#define   EXP0(exp)
#define   EXP1(exp) exp
#define  EXP00(exp)
#define  EXP01(exp) exp
#define  EXP10(exp) exp
#define  EXP11(exp) exp
#define EXP000(exp)
#define EXP001(exp) exp
#define EXP010(exp) exp
#define EXP011(exp) exp
#define EXP100(exp) exp
#define EXP101(exp) exp
#define EXP110(exp) exp
#define EXP111(exp) exp

// Note: can be used EXPAND(test, DEFINE1##DEFINE2##DEFINE3)
#define JOIN(x,y) x##y
#define EXPAND(exp,grp) JOIN(EXP,grp)(exp)

#endif
