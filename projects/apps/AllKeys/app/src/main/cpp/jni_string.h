#pragma once
#include <jni.h>

namespace allkeys
{
	struct jni_string
	{
		JNIEnv *m_env;
		jstring m_str;
		const char *m_utf;

		jni_string(JNIEnv *env, jstring str)
			: m_env(env)
			  , m_str(str)
			  , m_utf(env->GetStringUTFChars(str, nullptr))
		{}

		jni_string(jni_string &&) = default;

		jni_string(const jni_string &) = delete;

		jni_string &operator=(jni_string &&) = default;

		jni_string &operator=(const jni_string &) = delete;

		~jni_string()
		{
			m_env->ReleaseStringUTFChars(m_str, m_utf);
		}

		operator char const *() const
		{
			return m_utf;
		}
	};
}