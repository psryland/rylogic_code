#ifndef SHARED_ON_EXIT_H
#define SHARED_ON_EXIT_H

#include <fuz/utility/join.hpp>

namespace on_exit
{
	template <typename T>
	char (&lvalue(T& ))[1];
	char (&lvalue(...))[2];

	template <int LValue>
	struct resolve
	{
		template <typename T> static T& f(T* val) { return *val; }
	};

	template <>
	struct resolve<1>
	{
		template <typename T>                 static T  const f(const T& val) { return val; }
		template <typename T, unsigned int N> static T* const f(T(&val)[N])   { return val; }
	};
}

#ifdef __GNUC__
	#define ON_EXIT_PRIVATE_TYPEOF(x) typeof(x)
#else
	#define ON_EXIT_PRIVATE_TYPEOF(x) decltype(x)
	#define ON_EXIT_DISABLE_WARNINGS __pragma(warning(push)) __pragma(warning(disable:4510)) __pragma(warning(disable:4512)) __pragma(warning(disable:4610))
	#define ON_EXIT_DISABLE_WARNINGS_END __pragma(warning(pop))
#endif

#define ON_EXIT_PRIVATE_CONV(val)      on_exit::resolve<sizeof(on_exit::lvalue(val))>::f(val)
#define ON_EXIT_PRIVATE_CONV_TYPE(val) ON_EXIT_PRIVATE_TYPEOF(ON_EXIT_PRIVATE_CONV(val))

#define ON_EXIT_PRIVATE_COUNT3() 0,1,2,3,4,5,6,7,8,err
#define ON_EXIT_PRIVATE_COUNT2(_0,_1,_2,_3,_4,_5,_6,_7,_8,_err, N, ...) N
#define ON_EXIT_PRIVATE_COUNT1(x) ON_EXIT_PRIVATE_COUNT2 x
#define ON_EXIT_PRIVATE_COUNT(...) ON_EXIT_PRIVATE_COUNT1((ON_EXIT_PRIVATE_COUNT3 __VA_ARGS__ (), 0, , 8, 7, 6, 5, 4, 3, 2, 1))

#define ON_EXIT_END(...) FUZ_JOIN(ON_EXIT_END_, ON_EXIT_PRIVATE_COUNT(__VA_ARGS__)(FUZ_JOIN(on_exit_, __LINE__), __VA_ARGS__))

#ifdef __GNUC__

	#define ON_EXIT const struct FUZ_JOIN(on_exit_t_, __LINE__) { ~FUZ_JOIN(on_exit_t_, __LINE__)() {
	#define ON_EXIT_END_0(var) } } var; (void)var;
	#define ON_EXIT_END_1(var, v0) } struct on_exit_private_types { static ON_EXIT_PRIVATE_CONV_TYPE(v0) v0_t(); }; ON_EXIT_PRIVATE_TYPEOF(on_exit_private_types::v0_t()) v0; } var = { ON_EXIT_PRIVATE_CONV(v0) }; (void)var;
	#define ON_EXIT_END_2(var, v0, v1) } struct on_exit_private_types { static ON_EXIT_PRIVATE_CONV_TYPE(v0) v0_t(); static ON_EXIT_PRIVATE_CONV_TYPE(v1) v1_t(); }; ON_EXIT_PRIVATE_TYPEOF(on_exit_private_types::v0_t()) v0; ON_EXIT_PRIVATE_TYPEOF(on_exit_private_types::v1_t()) v1; } var = { ON_EXIT_PRIVATE_CONV(v0), ON_EXIT_PRIVATE_CONV(v1) }; (void)var;
	#define ON_EXIT_END_3(var, v0, v1, v2) } struct on_exit_private_types { static ON_EXIT_PRIVATE_CONV_TYPE(v0) v0_t(); static ON_EXIT_PRIVATE_CONV_TYPE(v1) v1_t(); static ON_EXIT_PRIVATE_CONV_TYPE(v2) v2_t(); }; ON_EXIT_PRIVATE_TYPEOF(on_exit_private_types::v0_t()) v0; ON_EXIT_PRIVATE_TYPEOF(on_exit_private_types::v1_t()) v1; ON_EXIT_PRIVATE_TYPEOF(on_exit_private_types::v2_t()) v2; } var = { ON_EXIT_PRIVATE_CONV(v0), ON_EXIT_PRIVATE_CONV(v1), ON_EXIT_PRIVATE_CONV(v2) }; (void)var;
	#define ON_EXIT_END_4(var, v0, v1, v2, v3) } struct on_exit_private_types { static ON_EXIT_PRIVATE_CONV_TYPE(v0) v0_t(); static ON_EXIT_PRIVATE_CONV_TYPE(v1) v1_t(); static ON_EXIT_PRIVATE_CONV_TYPE(v2) v2_t(); static ON_EXIT_PRIVATE_CONV_TYPE(v3) v3_t(); }; ON_EXIT_PRIVATE_TYPEOF(on_exit_private_types::v0_t()) v0; ON_EXIT_PRIVATE_TYPEOF(on_exit_private_types::v1_t()) v1; ON_EXIT_PRIVATE_TYPEOF(on_exit_private_types::v2_t()) v2; ON_EXIT_PRIVATE_TYPEOF(on_exit_private_types::v3_t()) v3; } var = { ON_EXIT_PRIVATE_CONV(v0), ON_EXIT_PRIVATE_CONV(v1), ON_EXIT_PRIVATE_CONV(v2), ON_EXIT_PRIVATE_CONV(v3) }; (void)var;
	#define ON_EXIT_END_5(var, v0, v1, v2, v3, v4) } struct on_exit_private_types { static ON_EXIT_PRIVATE_CONV_TYPE(v0) v0_t(); static ON_EXIT_PRIVATE_CONV_TYPE(v1) v1_t(); static ON_EXIT_PRIVATE_CONV_TYPE(v2) v2_t(); static ON_EXIT_PRIVATE_CONV_TYPE(v3) v3_t(); static ON_EXIT_PRIVATE_CONV_TYPE(v4) v4_t(); }; ON_EXIT_PRIVATE_TYPEOF(on_exit_private_types::v0_t()) v0; ON_EXIT_PRIVATE_TYPEOF(on_exit_private_types::v1_t()) v1; ON_EXIT_PRIVATE_TYPEOF(on_exit_private_types::v2_t()) v2; ON_EXIT_PRIVATE_TYPEOF(on_exit_private_types::v3_t()) v3; ON_EXIT_PRIVATE_TYPEOF(on_exit_private_types::v4_t()) v4; } var = { ON_EXIT_PRIVATE_CONV(v0), ON_EXIT_PRIVATE_CONV(v1), ON_EXIT_PRIVATE_CONV(v2), ON_EXIT_PRIVATE_CONV(v3), ON_EXIT_PRIVATE_CONV(v4) }; (void)var;
	#define ON_EXIT_END_6(var, v0, v1, v2, v3, v4, v5) } struct on_exit_private_types { static ON_EXIT_PRIVATE_CONV_TYPE(v0) v0_t(); static ON_EXIT_PRIVATE_CONV_TYPE(v1) v1_t(); static ON_EXIT_PRIVATE_CONV_TYPE(v2) v2_t(); static ON_EXIT_PRIVATE_CONV_TYPE(v3) v3_t(); static ON_EXIT_PRIVATE_CONV_TYPE(v4) v4_t(); static ON_EXIT_PRIVATE_CONV_TYPE(v5) v5_t(); }; ON_EXIT_PRIVATE_TYPEOF(on_exit_private_types::v0_t()) v0; ON_EXIT_PRIVATE_TYPEOF(on_exit_private_types::v1_t()) v1; ON_EXIT_PRIVATE_TYPEOF(on_exit_private_types::v2_t()) v2; ON_EXIT_PRIVATE_TYPEOF(on_exit_private_types::v3_t()) v3; ON_EXIT_PRIVATE_TYPEOF(on_exit_private_types::v4_t()) v4; ON_EXIT_PRIVATE_TYPEOF(on_exit_private_types::v5_t()) v5; } var = { ON_EXIT_PRIVATE_CONV(v0), ON_EXIT_PRIVATE_CONV(v1), ON_EXIT_PRIVATE_CONV(v2), ON_EXIT_PRIVATE_CONV(v3), ON_EXIT_PRIVATE_CONV(v4), ON_EXIT_PRIVATE_CONV(v5) }; (void)var;
	#define ON_EXIT_END_7(var, v0, v1, v2, v3, v4, v5, v6) } struct on_exit_private_types { static ON_EXIT_PRIVATE_CONV_TYPE(v0) v0_t(); static ON_EXIT_PRIVATE_CONV_TYPE(v1) v1_t(); static ON_EXIT_PRIVATE_CONV_TYPE(v2) v2_t(); static ON_EXIT_PRIVATE_CONV_TYPE(v3) v3_t(); static ON_EXIT_PRIVATE_CONV_TYPE(v4) v4_t(); static ON_EXIT_PRIVATE_CONV_TYPE(v5) v5_t(); static ON_EXIT_PRIVATE_CONV_TYPE(v6) v6_t(); }; ON_EXIT_PRIVATE_TYPEOF(on_exit_private_types::v0_t()) v0; ON_EXIT_PRIVATE_TYPEOF(on_exit_private_types::v1_t()) v1; ON_EXIT_PRIVATE_TYPEOF(on_exit_private_types::v2_t()) v2; ON_EXIT_PRIVATE_TYPEOF(on_exit_private_types::v3_t()) v3; ON_EXIT_PRIVATE_TYPEOF(on_exit_private_types::v4_t()) v4; ON_EXIT_PRIVATE_TYPEOF(on_exit_private_types::v5_t()) v5; ON_EXIT_PRIVATE_TYPEOF(on_exit_private_types::v6_t()) v6; } var = { ON_EXIT_PRIVATE_CONV(v0), ON_EXIT_PRIVATE_CONV(v1), ON_EXIT_PRIVATE_CONV(v2), ON_EXIT_PRIVATE_CONV(v3), ON_EXIT_PRIVATE_CONV(v4), ON_EXIT_PRIVATE_CONV(v5), ON_EXIT_PRIVATE_CONV(v6) }; (void)var;
	#define ON_EXIT_END_8(var, v0, v1, v2, v3, v4, v5, v6, v7) } struct on_exit_private_types { static ON_EXIT_PRIVATE_CONV_TYPE(v0) v0_t(); static ON_EXIT_PRIVATE_CONV_TYPE(v1) v1_t(); static ON_EXIT_PRIVATE_CONV_TYPE(v2) v2_t(); static ON_EXIT_PRIVATE_CONV_TYPE(v3) v3_t(); static ON_EXIT_PRIVATE_CONV_TYPE(v4) v4_t(); static ON_EXIT_PRIVATE_CONV_TYPE(v5) v5_t(); static ON_EXIT_PRIVATE_CONV_TYPE(v6) v6_t(); static ON_EXIT_PRIVATE_CONV_TYPE(v7) v7_t(); }; ON_EXIT_PRIVATE_TYPEOF(on_exit_private_types::v0_t()) v0; ON_EXIT_PRIVATE_TYPEOF(on_exit_private_types::v1_t()) v1; ON_EXIT_PRIVATE_TYPEOF(on_exit_private_types::v2_t()) v2; ON_EXIT_PRIVATE_TYPEOF(on_exit_private_types::v3_t()) v3; ON_EXIT_PRIVATE_TYPEOF(on_exit_private_types::v4_t()) v4; ON_EXIT_PRIVATE_TYPEOF(on_exit_private_types::v5_t()) v5; ON_EXIT_PRIVATE_TYPEOF(on_exit_private_types::v6_t()) v6; ON_EXIT_PRIVATE_TYPEOF(on_exit_private_types::v7_t()) v7; } var = { ON_EXIT_PRIVATE_CONV(v0), ON_EXIT_PRIVATE_CONV(v1), ON_EXIT_PRIVATE_CONV(v2), ON_EXIT_PRIVATE_CONV(v3), ON_EXIT_PRIVATE_CONV(v4), ON_EXIT_PRIVATE_CONV(v5), ON_EXIT_PRIVATE_CONV(v6), ON_EXIT_PRIVATE_CONV(v7) }; (void)var;

#else

	#define ON_EXIT const struct FUZ_JOIN(on_exit_t_, __LINE__) { ~FUZ_JOIN(on_exit_t_, __LINE__)() {
	#define ON_EXIT_END_0(var) } } var; (void)var;
	#define ON_EXIT_END_1(var, v0) } struct on_exit_private_types { typedef ON_EXIT_PRIVATE_CONV_TYPE(v0) v0_t; }; on_exit_private_types::v0_t v0; ON_EXIT_DISABLE_WARNINGS } ON_EXIT_DISABLE_WARNINGS_END var = { ON_EXIT_PRIVATE_CONV(v0) }; (void)var;
	#define ON_EXIT_END_2(var, v0, v1) } struct on_exit_private_types { typedef ON_EXIT_PRIVATE_CONV_TYPE(v0) v0_t; typedef ON_EXIT_PRIVATE_CONV_TYPE(v1) v1_t; }; on_exit_private_types::v0_t v0; on_exit_private_types::v1_t v1; ON_EXIT_DISABLE_WARNINGS } ON_EXIT_DISABLE_WARNINGS_END var = { ON_EXIT_PRIVATE_CONV(v0), ON_EXIT_PRIVATE_CONV(v1) }; (void)var;
	#define ON_EXIT_END_3(var, v0, v1, v2) } struct on_exit_private_types { typedef ON_EXIT_PRIVATE_CONV_TYPE(v0) v0_t; typedef ON_EXIT_PRIVATE_CONV_TYPE(v1) v1_t; typedef ON_EXIT_PRIVATE_CONV_TYPE(v2) v2_t; }; on_exit_private_types::v0_t v0; on_exit_private_types::v1_t v1; on_exit_private_types::v2_t v2; ON_EXIT_DISABLE_WARNINGS } ON_EXIT_DISABLE_WARNINGS_END var = { ON_EXIT_PRIVATE_CONV(v0), ON_EXIT_PRIVATE_CONV(v1), ON_EXIT_PRIVATE_CONV(v2) }; (void)var;
	#define ON_EXIT_END_4(var, v0, v1, v2, v3) } struct on_exit_private_types { typedef ON_EXIT_PRIVATE_CONV_TYPE(v0) v0_t; typedef ON_EXIT_PRIVATE_CONV_TYPE(v1) v1_t; typedef ON_EXIT_PRIVATE_CONV_TYPE(v2) v2_t; typedef ON_EXIT_PRIVATE_CONV_TYPE(v3) v3_t; }; on_exit_private_types::v0_t v0; on_exit_private_types::v1_t v1; on_exit_private_types::v2_t v2; on_exit_private_types::v3_t v3; ON_EXIT_DISABLE_WARNINGS } ON_EXIT_DISABLE_WARNINGS_END var = { ON_EXIT_PRIVATE_CONV(v0), ON_EXIT_PRIVATE_CONV(v1), ON_EXIT_PRIVATE_CONV(v2), ON_EXIT_PRIVATE_CONV(v3) }; (void)var;
	#define ON_EXIT_END_5(var, v0, v1, v2, v3, v4) } struct on_exit_private_types { typedef ON_EXIT_PRIVATE_CONV_TYPE(v0) v0_t; typedef ON_EXIT_PRIVATE_CONV_TYPE(v1) v1_t; typedef ON_EXIT_PRIVATE_CONV_TYPE(v2) v2_t; typedef ON_EXIT_PRIVATE_CONV_TYPE(v3) v3_t; typedef ON_EXIT_PRIVATE_CONV_TYPE(v4) v4_t; }; on_exit_private_types::v0_t v0; on_exit_private_types::v1_t v1; on_exit_private_types::v2_t v2; on_exit_private_types::v3_t v3; on_exit_private_types::v4_t v4; ON_EXIT_DISABLE_WARNINGS } ON_EXIT_DISABLE_WARNINGS_END var = { ON_EXIT_PRIVATE_CONV(v0), ON_EXIT_PRIVATE_CONV(v1), ON_EXIT_PRIVATE_CONV(v2), ON_EXIT_PRIVATE_CONV(v3), ON_EXIT_PRIVATE_CONV(v4) }; (void)var;
	#define ON_EXIT_END_6(var, v0, v1, v2, v3, v4, v5) } struct on_exit_private_types { typedef ON_EXIT_PRIVATE_CONV_TYPE(v0) v0_t; typedef ON_EXIT_PRIVATE_CONV_TYPE(v1) v1_t; typedef ON_EXIT_PRIVATE_CONV_TYPE(v2) v2_t; typedef ON_EXIT_PRIVATE_CONV_TYPE(v3) v3_t; typedef ON_EXIT_PRIVATE_CONV_TYPE(v4) v4_t; typedef ON_EXIT_PRIVATE_CONV_TYPE(v5) v5_t; }; on_exit_private_types::v0_t v0; on_exit_private_types::v1_t v1; on_exit_private_types::v2_t v2; on_exit_private_types::v3_t v3; on_exit_private_types::v4_t v4; on_exit_private_types::v5_t v5; ON_EXIT_DISABLE_WARNINGS } ON_EXIT_DISABLE_WARNINGS_END var = { ON_EXIT_PRIVATE_CONV(v0), ON_EXIT_PRIVATE_CONV(v1), ON_EXIT_PRIVATE_CONV(v2), ON_EXIT_PRIVATE_CONV(v3), ON_EXIT_PRIVATE_CONV(v4), ON_EXIT_PRIVATE_CONV(v5) }; (void)var;
	#define ON_EXIT_END_7(var, v0, v1, v2, v3, v4, v5, v6) } struct on_exit_private_types { typedef ON_EXIT_PRIVATE_CONV_TYPE(v0) v0_t; typedef ON_EXIT_PRIVATE_CONV_TYPE(v1) v1_t; typedef ON_EXIT_PRIVATE_CONV_TYPE(v2) v2_t; typedef ON_EXIT_PRIVATE_CONV_TYPE(v3) v3_t; typedef ON_EXIT_PRIVATE_CONV_TYPE(v4) v4_t; typedef ON_EXIT_PRIVATE_CONV_TYPE(v5) v5_t; typedef ON_EXIT_PRIVATE_CONV_TYPE(v6) v6_t; }; on_exit_private_types::v0_t v0; on_exit_private_types::v1_t v1; on_exit_private_types::v2_t v2; on_exit_private_types::v3_t v3; on_exit_private_types::v4_t v4; on_exit_private_types::v5_t v5; on_exit_private_types::v6_t v6; ON_EXIT_DISABLE_WARNINGS } ON_EXIT_DISABLE_WARNINGS_END var = { ON_EXIT_PRIVATE_CONV(v0), ON_EXIT_PRIVATE_CONV(v1), ON_EXIT_PRIVATE_CONV(v2), ON_EXIT_PRIVATE_CONV(v3), ON_EXIT_PRIVATE_CONV(v4), ON_EXIT_PRIVATE_CONV(v5), ON_EXIT_PRIVATE_CONV(v6) }; (void)var;
	#define ON_EXIT_END_8(var, v0, v1, v2, v3, v4, v5, v6, v7) } struct on_exit_private_types { typedef ON_EXIT_PRIVATE_CONV_TYPE(v0) v0_t; typedef ON_EXIT_PRIVATE_CONV_TYPE(v1) v1_t; typedef ON_EXIT_PRIVATE_CONV_TYPE(v2) v2_t; typedef ON_EXIT_PRIVATE_CONV_TYPE(v3) v3_t; typedef ON_EXIT_PRIVATE_CONV_TYPE(v4) v4_t; typedef ON_EXIT_PRIVATE_CONV_TYPE(v5) v5_t; typedef ON_EXIT_PRIVATE_CONV_TYPE(v6) v6_t; typedef ON_EXIT_PRIVATE_CONV_TYPE(v7) v7_t; }; on_exit_private_types::v0_t v0; on_exit_private_types::v1_t v1; on_exit_private_types::v2_t v2; on_exit_private_types::v3_t v3; on_exit_private_types::v4_t v4; on_exit_private_types::v5_t v5; on_exit_private_types::v6_t v6; on_exit_private_types::v7_t v7; ON_EXIT_DISABLE_WARNINGS } ON_EXIT_DISABLE_WARNINGS_END var = { ON_EXIT_PRIVATE_CONV(v0), ON_EXIT_PRIVATE_CONV(v1), ON_EXIT_PRIVATE_CONV(v2), ON_EXIT_PRIVATE_CONV(v3), ON_EXIT_PRIVATE_CONV(v4), ON_EXIT_PRIVATE_CONV(v5), ON_EXIT_PRIVATE_CONV(v6), ON_EXIT_PRIVATE_CONV(v7) }; (void)var;

#endif


#endif
