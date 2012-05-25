//****************************************************************
//
//	Inline include file for physics material declarations
//
//****************************************************************
//
//	Usage:
//		DECLARE_PHYSICS_MATERIAL(name, static_friction, dynamic_friction, elasticity, tangential_elasticity)
//
//	name:					must not contain spaces
//	static_friction:		a float between 0.0f and 1.0f	0.0f = no friction, 1.0f = superglue
//	dynamic_friction:		a float between 0.0f and 1.0f	0.0f = no friction, 1.0f = superglue
//	elasticity:				a float between 0.0f and 1.0f	0.0f = inelastic, 1.0f = completely elastic
//	tangential_elasticity:	a float between -1.0f and 1.0f	-1.0f = bounces back, 1.0f = bounces forward
DECLARE_PHYSICS_MATERIAL(Teflon, 0.0f, 0.0f, 1.0f, 1.0f)


