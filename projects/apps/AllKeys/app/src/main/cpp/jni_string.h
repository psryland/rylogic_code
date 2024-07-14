#pragma once
#include <utility>
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
		jni_string(jni_string && rhs) noexcept
			: m_env(rhs.m_env)
			, m_str(rhs.m_str)
			, m_utf(rhs.m_utf)
		{
			m_env = nullptr;
			m_utf = nullptr;
		}
		jni_string(const jni_string &) = delete;
		jni_string &operator=(jni_string && rhs) noexcept
		{
			if (this == &rhs) return *this;
			std::swap(m_env, rhs.m_env);
			std::swap(m_str, rhs.m_str);
			std::swap(m_utf, rhs.m_utf);
			return *this;
		}
		jni_string &operator=(const jni_string &) = delete;
		~jni_string()
		{
			if (m_env == nullptr) return;
			m_env->ReleaseStringUTFChars(m_str, m_utf);
		}

		operator char const *() const
		{
			return m_utf;
		}
	};
}