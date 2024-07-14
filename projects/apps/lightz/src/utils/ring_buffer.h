#pragma once
#include "forward.h"

namespace lightz
{
	template <typename T>
	struct RingBufferItem
	{
		RingbufHandle_t m_rb;
		size_t m_size;
		T* m_item;

		explicit RingBufferItem(RingbufHandle_t rb, TickType_t timeout = portMAX_DELAY)
			: m_rb(rb)
			, m_size()
			, m_item()
		{
			size_t size_in_bytes = 0;
			m_item = static_cast<T*>(xRingbufferReceive(m_rb, &size_in_bytes, timeout));
			m_size = size_in_bytes / sizeof(T);
		}
		~RingBufferItem()
		{
			if (m_item)
				vRingbufferReturnItem(m_rb, (void*)m_item);
		}
		explicit operator bool() const
		{
			return m_item != nullptr;
		}
		size_t size() const
		{
			return m_size;
		}
		T const& operator[](size_t index) const
		{
			if (index >= size())
				throw std::out_of_range("Index out of range");

			return m_item[index];
		}
	};
}
