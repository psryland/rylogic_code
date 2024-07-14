#pragma once
#include <jni.h>
#include <cstdint>
#include <utility>
#include <span>

namespace allkeys
{
	struct jni_bytearray
	{
		JNIEnv *m_env;
		jbyteArray m_data;
		jbyte* m_buf;
		size_t m_size;

		jni_bytearray(JNIEnv *env, jbyteArray data)
			: m_env(env)
			, m_data(data)
			, m_buf(env->GetByteArrayElements(data, nullptr))
			, m_size(env->GetArrayLength(data))
		{}
		jni_bytearray(jni_bytearray && rhs) noexcept
			: m_env(rhs.m_env)
			, m_data(rhs.m_data)
			, m_buf(rhs.m_buf)
			, m_size(rhs.m_size)
		{
			m_env = nullptr;
			m_data = nullptr;
			m_buf = nullptr;
		}
		jni_bytearray(const jni_bytearray &) = delete;
		jni_bytearray &operator=(jni_bytearray && rhs) noexcept
		{
			if (this == &rhs) return *this;
			std::swap(m_env, rhs.m_env);
			std::swap(m_data, rhs.m_data);
			std::swap(m_buf, rhs.m_buf);
			std::swap(m_size, rhs.m_size);
			return *this;
		}
		jni_bytearray &operator=(const jni_bytearray &) = delete;
		~jni_bytearray()
		{
			if (m_env == nullptr) return;
			m_env->ReleaseByteArrayElements(m_data, m_buf, JNI_ABORT);
		}

		operator std::span<uint8_t const>() const
		{
			return { reinterpret_cast<uint8_t const*>(m_buf), m_size};
		}
	};
}
