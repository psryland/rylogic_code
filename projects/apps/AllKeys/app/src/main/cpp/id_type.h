#pragma once
#include <concepts>
#include <stdexcept>

namespace allkeys
{
	template <std::integral T, T Min, T Max> struct Id
	{
		T value;

		explicit Id(long long value)
			: value(static_cast<T>(value))
		{
			if (value < Min || value > Max)
				throw std::out_of_range("Id out of range");
		}
		operator T() const { return value; }
	};
}