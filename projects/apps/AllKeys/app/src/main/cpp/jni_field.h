#pragma once
#include <jni.h>
#include <type_traits>
#include <stdexcept>
#include "jni_string.h"

namespace allkeys
{
	// Access a field from a Java object
	template <typename T> T get(JNIEnv* env, jobject obj, char const* field)
	{
		auto cls = env->GetObjectClass(obj);
		if constexpr (std::is_same_v<T, bool>)
		{
			auto id = env->GetFieldID(cls, field, "Z");
			return static_cast<T>(env->GetBooleanField(obj, id));
		}
		if constexpr (std::is_same_v<T, unsigned char>)
		{
			auto id = env->GetFieldID(cls, field, "B");
			return static_cast<T>(env->GetByteField(obj, id));
		}
		if constexpr (std::is_same_v<T, char>)
		{
			auto id = env->GetFieldID(cls, field, "C");
			return static_cast<T>(env->GetCharField(obj, id));
		}
		if constexpr (std::is_same_v<T, short>)
		{
			auto id = env->GetFieldID(cls, field, "S");
			return static_cast<T>(env->GetShortField(obj, id));
		}
		if constexpr (std::is_same_v<T, int>)
		{
			auto id = env->GetFieldID(cls, field, "I");
			return static_cast<T>(env->GetIntField(obj, id));
		}
		if constexpr (std::is_same_v<T, long long>)
		{
			auto id = env->GetFieldID(cls, field, "J");
			return static_cast<T>(env->GetLongField(obj, id));
		}
		if constexpr (std::is_same_v<T, float>)
		{
			auto id = env->GetFieldID(cls, field, "F");
			return static_cast<T>(env->GetFloatField(obj, id));
		}
		if constexpr (std::is_same_v<T, double>)
		{
			auto id = env->GetFieldID(cls, field, "D");
			return static_cast<T>(env->GetDoubleField(obj, id));
		}
		if constexpr (std::is_same_v<T, jobject>)
		{
			auto id = env->GetFieldID(cls, field, "Ljava/lang/Object;");
			return static_cast<T>(env->GetObjectField(obj, id));
		}
		if constexpr (std::is_same_v<T, jni_string>)
		{
			auto id = env->GetFieldID(cls, field, "Ljava/lang/String;");
			auto str = static_cast<jstring>(env->GetObjectField(obj, id));
			return jni_string(env, str);
		}
		if constexpr (std::is_pointer_v<T>)
		{
			auto id = env->GetFieldID(cls, field, "J");
			auto handle = env->GetLongField(obj, id);
			return reinterpret_cast<T>(handle);
		}
		throw std::runtime_error("Unsupported field type");
	}
}