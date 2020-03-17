//*************************************************************
// Expression Evaluator
//  Copyright (c) Rylogic Ltd 2008
//*************************************************************
#pragma once
#include <cmath>
#include <cstring>
#include <array>
#include <vector>
#include <string_view>
#include <algorithm>
#include <exception>
#include <type_traits>
#include <initializer_list>
#include <cassert>
#include <cerrno>
#include <charconv>
#include "pr/common/hash.h"
#include "pr/common/algorithm.h"
#include "pr/str/string_core.h"
#include "pr/container/span.h"
#include "pr/container/byte_data.h"

namespace pr
{
	namespace eval
	{
		// Expression tokens
		enum class ETok :uint8_t
		{
			None,
			If,
			Else,
			Comma,
			LogOR,
			LogAND,
			BitOR,
			BitXOR,
			BitAND,
			LogEql,
			LogNEql,
			LogLT,
			LogLTEql,
			LogGT,
			LogGTEql,
			LeftShift,
			RightShift,
			Add,
			Sub,
			Mul,
			Div,
			Mod,
			UnaryPlus,
			UnaryMinus,
			Comp,
			Not,
			Fmod,
			Abs,
			Ceil,
			Floor,
			Round,
			Min,
			Max,
			Clamp,
			Sin,
			Cos,
			Tan,
			ASin,
			ACos,
			ATan,
			ATan2,
			SinH,
			CosH,
			TanH,
			Exp,
			Log,
			Log10,
			Pow,
			Sqr,
			Sqrt,
			Len2,
			Len3,
			Len4,
			Deg,
			Rad,
			Hash,
			OpenParenthesis,
			CloseParenthesis,
			Value,
			Identifier,
		};

		// Constants
		constexpr double const TAU = double(6.283185307179586476925286766559);
		constexpr double const PHI = double(1.618033988749894848204586834);

		// Convert a string into a character stream
		template <typename Char> struct char_range
		{
			Char const* m_ptr;
			Char const* m_end;

			char_range()
				:m_ptr()
				,m_end()
			{}
			char_range(Char const* ptr, Char const* end)
				:m_ptr(ptr)
				,m_end(end)
			{}
			char_range(Char const* ptr)
				:char_range(ptr, ptr + std::char_traits<Char>::length(ptr))
			{}
			char_range(std::basic_string<Char> const& s)
				:char_range(s.data(), s.data() + s.size())
			{}
			char_range(std::basic_string_view<Char> sv)
				:char_range(sv.data(), sv.data() + sv.size())
			{}
			operator bool() const
			{
				return m_ptr != m_end;
			}
			Char operator *() const
			{
				return m_ptr != m_end ? *m_ptr : '\0';
			}
			char_range& operator ++()
			{
				m_ptr += int(m_ptr != m_end);
				return *this;
			}
			char_range& operator += (int n)
			{
				m_ptr += std::min(n, static_cast<int>(m_end - m_ptr));
				return *this;
			}
			char_range operator + (int n) const
			{
				return char_range(m_ptr + std::min(n, static_cast<int>(m_end - m_ptr)), m_end);
			}
			size_t size() const
			{
				return m_end - m_ptr;
			}
			std::basic_string<Char> str() const
			{
				return std::basic_string<Char>(m_ptr, m_end);
			}
			std::basic_string_view<Char> str_view() const
			{
				return std::basic_string_view<Char>(m_ptr, m_end - m_ptr);
			}
		};

		// Identifier - use narrow strings because they're smaller
		using Ident = std::string;

		// An integral or floating point value
		struct Val
		{
			enum class EType :uint8_t { Unknown, Intg, Real, };

			// The value
			union
			{
				long long m_ll;
				double m_db;
			};
			EType m_ty;
			uint8_t pad[7];

			Val() noexcept
				:m_ll()
				, m_ty(EType::Unknown)
				, pad()
			{
			}
			Val(long long ll) noexcept
				:m_ll(ll)
				, m_ty(EType::Intg)
				, pad()
			{
			}
			Val(double db) noexcept
				:m_db(db)
				, m_ty(EType::Real)
				, pad()
			{
			}
			Val(int i) noexcept
				:Val(static_cast<long long>(i))
			{
			}
			Val(float f) noexcept
				:Val(static_cast<double>(f))
			{
			}
			Val& operator = (unsigned long long v) noexcept
			{
				m_ll = static_cast<long long>(v);
				m_ty = EType::Intg;
				return *this;
			}
			Val& operator = (long long v) noexcept
			{
				m_ll = v;
				m_ty = EType::Intg;
				return *this;
			}
			Val& operator = (double v) noexcept
			{
				m_db = v;
				m_ty = EType::Real;
				return *this;
			}
			Val& operator = (wchar_t v) noexcept
			{
				m_ll = v;
				m_ty = EType::Intg;
				return *this;
			}
			Val& operator = (char v) noexcept
			{
				m_ll = v;
				m_ty = EType::Intg;
				return *this;
			}
			Val& operator = (bool v) noexcept
			{
				m_ll = v;
				m_ty = EType::Intg;
				return *this;
			}

			// Read the value as a double or long long
			double db() const
			{
				return
					m_ty == EType::Real ? m_db :
					m_ty == EType::Intg ? static_cast<double>(m_ll) :
					throw std::runtime_error("Value not given. Value type is unknown");
			}
			long long ll() const
			{
				return
					m_ty == EType::Intg ? m_ll :
					m_ty == EType::Real ? static_cast<long long>(m_db) :
					throw std::runtime_error("Value not given. Value type is unknown");
			}

			// True if a valid value has been assigned
			bool valid() const
			{
				return m_ty != EType::Unknown;
			}

			// Operators
			friend Val  operator +  (Val const& lhs, Val const& rhs) { return (lhs.m_ty == EType::Real || rhs.m_ty == EType::Real) ? Val(lhs.db() + rhs.db()) : Val(lhs.ll() + rhs.ll()); }
			friend Val  operator -  (Val const& lhs, Val const& rhs) { return (lhs.m_ty == EType::Real || rhs.m_ty == EType::Real) ? Val(lhs.db() - rhs.db()) : Val(lhs.ll() - rhs.ll()); }
			friend Val  operator *  (Val const& lhs, Val const& rhs) { return (lhs.m_ty == EType::Real || rhs.m_ty == EType::Real) ? Val(lhs.db() * rhs.db()) : Val(lhs.ll() * rhs.ll()); }
			friend Val  operator /  (Val const& lhs, Val const& rhs) { return (lhs.m_ty == EType::Real || rhs.m_ty == EType::Real) ? Val(lhs.db() / rhs.db()) : Val(lhs.ll() / rhs.ll()); }
			friend bool operator == (Val const& lhs, Val const& rhs) { return (lhs.m_ty == EType::Real || rhs.m_ty == EType::Real) ? lhs.db() == rhs.db() : lhs.ll() == rhs.ll(); }
			friend bool operator <  (Val const& lhs, Val const& rhs) { return (lhs.m_ty == EType::Real || rhs.m_ty == EType::Real) ? lhs.db() < rhs.db() : lhs.ll() < rhs.ll(); }
			friend bool operator <= (Val const& lhs, Val const& rhs) { return (lhs.m_ty == EType::Real || rhs.m_ty == EType::Real) ? lhs.db() <= rhs.db() : lhs.ll() <= rhs.ll(); }
		};

		// A mapping from variable name to value
		struct Arg
		{
			Ident m_name;
			Val m_value;
			bool m_fixed;

			Arg()
				:m_name()
				,m_value()
				,m_fixed()
			{}
			Arg(Arg&& rhs)
				:m_name(std::move(rhs.m_name))
				,m_value(std::move(rhs.m_value))
				,m_fixed(rhs.m_fixed)
			{}
			Arg(Arg const& rhs)
				:m_name(rhs.m_name)
				,m_value(rhs.m_value)
				,m_fixed(rhs.m_fixed)
			{}
			Arg(std::string_view name)
				:m_name(Narrow(name))
				,m_value()
				,m_fixed(false)
			{}
			Arg(std::wstring_view name)
				:m_name(Narrow(name))
				,m_value()
				,m_fixed(false)
			{}
			Arg(std::string_view name, Val value)
				:m_name(Narrow(name))
				,m_value(value)
				,m_fixed(true)
			{}
			Arg(std::wstring_view name, Val value)
				:m_name(Narrow(name))
				,m_value(value)
				,m_fixed(true)
			{}
			Arg& operator = (Arg&& rhs)
			{
				if (this == &rhs) return *this;
				std::swap(m_name, rhs.m_name);
				std::swap(m_value, rhs.m_value);
				std::swap(m_fixed, rhs.m_fixed);
				return *this;
			}
			Arg& operator = (Arg const& rhs)
			{
				if (this == &rhs) return *this;
				m_name = rhs.m_name;
				m_value = rhs.m_value;
				m_fixed = rhs.m_fixed;
				return *this;
			}
			bool operator == (std::string_view name) const
			{
				return m_name == name;
			}
		};

		// A compiled expression
		struct Expression
		{
			// The compiled expression
			pr::byte_data<> m_op;

			// The names (and default values) of unique identifiers in the
			// expression (in order of discovery from left to right).
			std::vector<Arg> m_args;

			// The number of non-fixed arguments
			int Dimension() const
			{
				int dim = 0;
				for (auto& a : m_args)
					dim += int(a.m_fixed == false);

				return dim;
			}

			// Add or replace an independent variable
			void AddVariable(Arg const& arg)
			{
				auto iter = pr::find_if(m_args, [&](auto& a) { return a.m_name == arg.m_name; });
				if (iter != std::end(m_args))
					*iter = arg;
				else
					m_args.push_back(arg);
			}

			// Is callable/valid test
			explicit operator bool() const
			{
				return !m_op.empty();
			}

			// Evaluate the expression
			Val operator()(std::initializer_list<Arg> args) const
			{
				return call(args);
			}
			template <typename... A> Val operator()(A... a) const
			{
				if constexpr (sizeof...(A) == 0)
				{
					return call();
				}
				else
				{
					std::array<Val, sizeof...(A)> values = {a...};
					if (values.size() > m_args.size())
						throw std::runtime_error("Too many arguments given");

					// Any arguments not given remain as their defaults
					auto args = m_args;
					for (int i = 0, iend = static_cast<int>(values.size()); i != iend; ++i)
						args[i].m_value = values[i];

					return call(args);
				}
			}

			// Execute the expression with the given arguments
			template <typename ArgsCont = std::initializer_list<Arg>, typename = std::enable_if_t<std::is_same_v<ArgsCont::value_type, Arg>>>
			Val call(ArgsCont const& args = {}) const
			{
				// Note:
				//  Parameters are pushed onto the stack in left to right order,
				//  so when popping them off, the first is the rightmost argument.
				std::vector<Val> stack;
				for (size_t i = 0, iend = m_op.size(); i != iend;)
				{
					auto tok = m_op.read<ETok>(i);
					switch (tok)
					{
					case ETok::None:
						{
							break;
						}
					case ETok::Identifier:
						{
							auto count = m_op.read<uint8_t>(i);
							auto name = std::string_view(&m_op.at_byte_ofs<char>(i), count);
							auto iter = pr::find_if(args, [=](auto& a) { return a.m_name == name; });
							if (iter == std::end(args))
								throw std::runtime_error(std::string("Missing argument: ").append(name));

							stack.push_back(iter->m_value);
							i += count * sizeof(char);
							break;
						}
					case ETok::Value:
						{
							// Deserialise a 'Val' instance
							auto ty = m_op.read<Val::EType>(i);
							ty == Val::EType::Intg ? stack.push_back(m_op.read<long long>(i)) :
							ty == Val::EType::Real ? stack.push_back(m_op.read<double>(i)) :
							throw std::runtime_error("Invalid literal value");
							break;
						}
					case ETok::Add:
						{
							if (stack.size() < 2) throw std::runtime_error("Insufficient arguments for add expression");
							auto b = stack.back(); stack.pop_back();
							auto a = stack.back(); stack.pop_back();
							stack.push_back(a + b);
							break;
						}
					case ETok::Sub:
						{
							if (stack.size() < 2) throw std::runtime_error("Insufficient arguments for subtract expression");
							auto b = stack.back(); stack.pop_back();
							auto a = stack.back(); stack.pop_back();
							stack.push_back(a - b);
							break;
						}
					case ETok::Mul:
						{
							if (stack.size() < 2) throw std::runtime_error("Insufficient arguments for multiply expression");
							auto b = stack.back(); stack.pop_back();
							auto a = stack.back(); stack.pop_back();
							stack.push_back(a * b);
							break;
						}
					case ETok::Div:
						{
							if (stack.size() < 2) throw std::runtime_error("Insufficient arguments for divide expression");
							auto b = stack.back(); stack.pop_back();
							auto a = stack.back(); stack.pop_back();
							stack.push_back(a / b);
							break;
						}
					case ETok::Mod:
						{
							if (stack.size() < 2) throw std::runtime_error("Insufficient arguments for modulus expression");
							auto b = stack.back(); stack.pop_back();
							auto a = stack.back(); stack.pop_back();
							stack.push_back(a.ll() % b.ll());
							break;
						}
					case ETok::UnaryPlus:
						{
							if (stack.size() < 1) throw std::runtime_error("Insufficient arguments for unary plus expression");
							auto x = stack.back(); stack.pop_back();
							stack.push_back(x);
							break;
						}
					case ETok::UnaryMinus:
						{
							if (stack.size() < 1) throw std::runtime_error("Insufficient arguments for unary minus expression");
							auto x = stack.back(); stack.pop_back();
							x.m_ty == Val::EType::Real ? stack.push_back(-x.db()) : // Don't move 'stack.push_back' out of the ?: operator 
								x.m_ty == Val::EType::Intg ? stack.push_back(-x.ll()) : // because ?: will promote the integral result to a double
								throw std::runtime_error("Unknown result type for unary minus");
							break;
						}
					case ETok::Comp:
						{
							if (stack.size() < 1) throw std::runtime_error("Insufficient arguments for twos complement expression");
							auto x = stack.back(); stack.pop_back();
							stack.push_back(~x.ll());
							break;
						}
					case ETok::Not:
						{
							if (stack.size() < 1) throw std::runtime_error("Insufficient arguments for boolean NOT expression");
							auto x = stack.back(); stack.pop_back();
							stack.push_back(!x.ll());
							break;
						}
					case ETok::LogOR:
						{
							if (stack.size() < 2) throw std::runtime_error("Insufficient arguments for logical OR expression");
							auto b = stack.back(); stack.pop_back();
							auto a = stack.back(); stack.pop_back();
							stack.push_back(a.ll() || b.ll());
							break;
						}
					case ETok::LogAND:
						{
							if (stack.size() < 2) throw std::runtime_error("Insufficient arguments for logical AND expression");
							auto b = stack.back(); stack.pop_back();
							auto a = stack.back(); stack.pop_back();
							stack.push_back(a.ll() && b.ll());
							break;
						}
					case ETok::LogEql:
						{
							if (stack.size() < 2) throw std::runtime_error("Insufficient arguments for equals expression");
							auto b = stack.back(); stack.pop_back();
							auto a = stack.back(); stack.pop_back();
							stack.push_back(a == b);
							break;
						}
					case ETok::LogNEql:
						{
							if (stack.size() < 2) throw std::runtime_error("Insufficient arguments for not equal expression");
							auto b = stack.back(); stack.pop_back();
							auto a = stack.back(); stack.pop_back();
							stack.push_back(!(a == b));
							break;
						}
					case ETok::LogLT:
						{
							if (stack.size() < 2) throw std::runtime_error("Insufficient arguments for less than expression");
							auto b = stack.back(); stack.pop_back();
							auto a = stack.back(); stack.pop_back();
							stack.push_back(a < b);
							break;
						}
					case ETok::LogLTEql:
						{
							if (stack.size() < 2) throw std::runtime_error("Insufficient arguments for less than or equal expression");
							auto b = stack.back(); stack.pop_back();
							auto a = stack.back(); stack.pop_back();
							stack.push_back(a <= b);
							break;
						}
					case ETok::LogGT:
						{
							if (stack.size() < 2) throw std::runtime_error("Insufficient arguments for greater than expression");
							auto b = stack.back(); stack.pop_back();
							auto a = stack.back(); stack.pop_back();
							stack.push_back(!(a <= b));
							break;
						}
					case ETok::LogGTEql:
						{
							if (stack.size() < 2) throw std::runtime_error("Insufficient arguments for greater than or equal expression");
							auto b = stack.back(); stack.pop_back();
							auto a = stack.back(); stack.pop_back();
							stack.push_back(!(a < b));
							break;
						}
					case ETok::BitOR:
						{
							if (stack.size() < 2) throw std::runtime_error("Insufficient arguments for bitwise OR expression");
							auto b = stack.back(); stack.pop_back();
							auto a = stack.back(); stack.pop_back();
							stack.push_back(a.ll() | b.ll());
							break;
						}
					case ETok::BitAND:
						{
							if (stack.size() < 2) throw std::runtime_error("Insufficient arguments for bitwise AND expression");
							auto b = stack.back(); stack.pop_back();
							auto a = stack.back(); stack.pop_back();
							stack.push_back(a.ll() & b.ll());
							break;
						}
					case ETok::BitXOR:
						{
							if (stack.size() < 2) throw std::runtime_error("Insufficient arguments for bitwise XOR expression");
							auto b = stack.back(); stack.pop_back();
							auto a = stack.back(); stack.pop_back();
							stack.push_back(a.ll() ^ b.ll());
							break;
						}
					case ETok::LeftShift:
						{
							if (stack.size() < 2) throw std::runtime_error("Insufficient arguments for bitwise left shift expression");
							auto b = stack.back(); stack.pop_back();
							auto a = stack.back(); stack.pop_back();
							stack.push_back(static_cast<int64_t>(static_cast<uint64_t>(a.ll()) << b.ll()));
							break;
						}
					case ETok::RightShift:
						{
							if (stack.size() < 2) throw std::runtime_error("Insufficient arguments for bitwise left shift expression");
							auto b = stack.back(); stack.pop_back();
							auto a = stack.back(); stack.pop_back();
							stack.push_back(static_cast<int64_t>(static_cast<uint64_t>(a.ll()) >> b.ll()));
							break;
						}
					case ETok::Fmod:
						{
							if (stack.size() < 2) throw std::runtime_error("Insufficient arguments for fmod() expression");
							auto b = stack.back(); stack.pop_back();
							auto a = stack.back(); stack.pop_back();
							stack.push_back(std::fmod(a.db(), b.db()));
							break;
						}
					case ETok::Ceil:
						{
							if (stack.size() < 1) throw std::runtime_error("Insufficient arguments for ceil() expression");
							auto x = stack.back(); stack.pop_back();
							stack.push_back(std::ceil(x.db()));
							break;
						}
					case ETok::Floor:
						{
							if (stack.size() < 1) throw std::runtime_error("Insufficient arguments for floor() expression");
							auto x = stack.back(); stack.pop_back();
							stack.push_back(std::floor(x.db()));
							break;
						}
					case ETok::Round:
						{
							if (stack.size() < 1) throw std::runtime_error("Insufficient arguments for round() expression");
							auto x = stack.back(); stack.pop_back();
							stack.push_back(std::round(x.db()));
							break;
						}
					case ETok::Min:
						{
							if (stack.size() < 2) throw std::runtime_error("Insufficient arguments for min() expression");
							auto b = stack.back(); stack.pop_back();
							auto a = stack.back(); stack.pop_back();
							stack.push_back(a < b ? a : b);
							break;
						}
					case ETok::Max:
						{
							if (stack.size() < 2) throw std::runtime_error("Insufficient arguments for max() expression");
							auto b = stack.back(); stack.pop_back();
							auto a = stack.back(); stack.pop_back();
							stack.push_back(a < b ? b : a);
							break;
						}
					case ETok::Clamp:
						{
							if (stack.size() < 3) throw std::runtime_error("Insufficient arguments for clamp() expression");
							auto mx = stack.back(); stack.pop_back();
							auto mn = stack.back(); stack.pop_back();
							auto x = stack.back(); stack.pop_back();
							stack.push_back(x < mn ? mn : mx < x ? mx : x);
							break;
						}
					case ETok::Abs:
						{
							if (stack.size() < 1) throw std::runtime_error("Insufficient arguments for abs() expression");
							auto x = stack.back(); stack.pop_back();
							x.m_ty == Val::EType::Intg ? stack.push_back(std::abs(x.ll())) : // Don't move 'stack.push_back' out of the ?: operator 
							x.m_ty == Val::EType::Real ? stack.push_back(std::abs(x.db())) : // because ?: will promote the integral result to a double
							throw std::runtime_error("Invalid argument for 'abs'");
							break;
						}
					case ETok::Sin:
						{
							if (stack.size() < 1) throw std::runtime_error("Insufficient arguments for sin() expression");
							auto x = stack.back(); stack.pop_back();
							stack.push_back(std::sin(x.db()));
							break;
						}
					case ETok::Cos:
						{
							if (stack.size() < 1) throw std::runtime_error("Insufficient arguments for cos() expression");
							auto x = stack.back(); stack.pop_back();
							stack.push_back(std::cos(x.db()));
							break;
						}
					case ETok::Tan:
						{
							if (stack.size() < 1) throw std::runtime_error("Insufficient arguments for tan() expression");
							auto x = stack.back(); stack.pop_back();
							stack.push_back(std::tan(x.db()));
							break;
						}
					case ETok::ASin:
						{
							if (stack.size() < 1) throw std::runtime_error("Insufficient arguments for asin() expression");
							auto x = stack.back(); stack.pop_back();
							stack.push_back(std::asin(x.db()));
							break;
						}
					case ETok::ACos:
						{
							if (stack.size() < 1) throw std::runtime_error("Insufficient arguments for acos() expression");
							auto x = stack.back(); stack.pop_back();
							stack.push_back(std::acos(x.db()));
							break;
						}
					case ETok::ATan:
						{
							if (stack.size() < 1) throw std::runtime_error("Insufficient arguments for atan() expression");
							auto x = stack.back(); stack.pop_back();
							stack.push_back(std::atan(x.db()));
							break;
						}
					case ETok::ATan2:
						{
							if (stack.size() < 2) throw std::runtime_error("Insufficient arguments for atan2() expression");
							auto x = stack.back(); stack.pop_back();
							auto y = stack.back(); stack.pop_back();
							stack.push_back(std::atan2(y.db(), x.db()));
							break;
						}
					case ETok::SinH:
						{
							if (stack.size() < 1) throw std::runtime_error("Insufficient arguments for sinh() expression");
							auto x = stack.back(); stack.pop_back();
							stack.push_back(std::sinh(x.db()));
							break;
						}
					case ETok::CosH:
						{
							if (stack.size() < 1) throw std::runtime_error("Insufficient arguments for cosh() expression");
							auto x = stack.back(); stack.pop_back();
							stack.push_back(std::cosh(x.db()));
							break;
						}
					case ETok::TanH:
						{
							if (stack.size() < 1) throw std::runtime_error("Insufficient arguments for tanh() expression");
							auto x = stack.back(); stack.pop_back();
							stack.push_back(std::tanh(x.db()));
							break;
						}
					case ETok::Exp:
						{
							if (stack.size() < 1) throw std::runtime_error("Insufficient arguments for exp() expression");
							auto x = stack.back(); stack.pop_back();
							stack.push_back(std::exp(x.db()));
							break;
						}
					case ETok::Log:
						{
							if (stack.size() < 1) throw std::runtime_error("Insufficient arguments for log() expression");
							auto x = stack.back(); stack.pop_back();
							stack.push_back(std::log(x.db()));
							break;
						}
					case ETok::Log10:
						{
							if (stack.size() < 1) throw std::runtime_error("Insufficient arguments for log10() expression");
							auto x = stack.back(); stack.pop_back();
							stack.push_back(std::log10(x.db()));
							break;
						}
					case ETok::Pow:
						{
							if (stack.size() < 2) throw std::runtime_error("Insufficient arguments for pow() expression");
							auto y = stack.back(); stack.pop_back();
							auto x = stack.back(); stack.pop_back();
							stack.push_back(std::pow(x.db(), y.db()));
							break;
						}
					case ETok::Sqr:
						{
							if (stack.size() < 1) throw std::runtime_error("Insufficient arguments for sqr() expression");
							auto x = stack.back(); stack.pop_back();
							stack.push_back(x * x);
							break;
						}
					case ETok::Sqrt:
						{
							if (stack.size() < 1) throw std::runtime_error("Insufficient arguments for sqrt() expression");
							auto x = stack.back(); stack.pop_back();
							stack.push_back(std::sqrt(x.db()));
							break;
						}
					case ETok::Len2:
						{
							if (stack.size() < 2) throw std::runtime_error("Insufficient arguments for len2() expression");
							auto y = stack.back(); stack.pop_back();
							auto x = stack.back(); stack.pop_back();
							stack.push_back(std::sqrt(0.0 + x.db() * x.db() + y.db() * y.db()));
							break;
						}
					case ETok::Len3:
						{
							if (stack.size() < 3) throw std::runtime_error("Insufficient arguments for len3() expression");
							auto z = stack.back(); stack.pop_back();
							auto y = stack.back(); stack.pop_back();
							auto x = stack.back(); stack.pop_back();
							stack.push_back(std::sqrt(0.0 + x.db() * x.db() + y.db() * y.db() + z.db() * z.db()));
							break;
						}
					case ETok::Len4:
						{
							if (stack.size() < 4) throw std::runtime_error("Insufficient arguments for len4() expression");
							auto w = stack.back(); stack.pop_back();
							auto z = stack.back(); stack.pop_back();
							auto y = stack.back(); stack.pop_back();
							auto x = stack.back(); stack.pop_back();
							stack.push_back(std::sqrt(0.0 + x.db() * x.db() + y.db() * y.db() + z.db() * z.db() + w.db() * w.db()));
							break;
						}
					case ETok::Deg:
						{
							if (stack.size() < 1) throw std::runtime_error("Insufficient arguments for deg() expression");
							auto x = stack.back(); stack.pop_back();
							stack.push_back(x.db() * (360.0 / TAU));
							break;
						}
					case ETok::Rad:
						{
							if (stack.size() < 1) throw std::runtime_error("Insufficient arguments for rad() expression");
							auto x = stack.back(); stack.pop_back();
							stack.push_back(x.db() * (TAU / 360.0));
							break;
						}
					case ETok::If:
						{
							if (stack.size() < 1) throw std::runtime_error("Insufficient arguments for if expression");
							auto boolean = stack.back(); stack.pop_back();
							auto jmp = m_op.read<int>(i);
							if (boolean == Val(0))
							{
								i += jmp;

								// If the next instruction is an 'else' statement, skip over it so that the else body
								// gets executed. Remember If == branch-if-zero, Else == branch-always
								if (m_op.at_byte_ofs<ETok>(i) == ETok::Else)
									i += sizeof(ETok) + sizeof(int);
							}
							break;
						}
					case ETok::Else:
						{
							auto jmp = m_op.read<int>(i);
							i += jmp;
							break;
						}
					default:
						{
							throw std::runtime_error("Unknown expression token");
						}
					}
				}
				if (stack.size() != 1)
					throw std::runtime_error("Expression does not evaluate to a single result");

				return stack.front();
			}
		};

		// Returns the precedence of a token
		// How to work out precedence:
		//   NewOp = the op whose precedence you want to know
		//   RhsOp = an op in this list
		// Ask, "If I encounter RhsOp next after NewOp, should NewOp go on hold while
		//  RhsOp is evaluated, or should I stop and evaluate up to NewOp before carrying on".
		//  Also the visa versa case.
		// If NewOp should go on hold, then it has lower precedence (i.e. NewOp < RhsOp)
		// If NewOp needs evaluating, then RhsOp has lower precedence (i.e. RhsOp > NewOp)
		constexpr int Precedence(ETok tok)
		{
			switch (tok)
			{
			default: throw std::runtime_error("Unknown token");
			case ETok::None            : return   0;
			case ETok::Comma           : return  20;
			case ETok::If              : return  30;
			case ETok::Else            : return  30;
			case ETok::LogOR           : return  40;
			case ETok::LogAND          : return  50;
			case ETok::BitOR           : return  60;
			case ETok::BitXOR          : return  70;
			case ETok::BitAND          : return  80;
			case ETok::LogEql          : return  90;
			case ETok::LogNEql         : return  90;
			case ETok::LogLT           : return 100;
			case ETok::LogLTEql        : return 100;
			case ETok::LogGT           : return 100;
			case ETok::LogGTEql        : return 100;
			case ETok::LeftShift       : return 110;
			case ETok::RightShift      : return 110;
			case ETok::Add             : return 120;
			case ETok::Sub             : return 120;
			case ETok::Mul             : return 130;
			case ETok::Div             : return 130;
			case ETok::Mod             : return 130;
			case ETok::UnaryPlus       : return 140;
			case ETok::UnaryMinus      : return 140;
			case ETok::Comp            : return 140;
			case ETok::Not             : return 140;
			case ETok::Fmod            : return 200;
			case ETok::Abs             : return 200;
			case ETok::Ceil            : return 200;
			case ETok::Floor           : return 200;
			case ETok::Round           : return 200;
			case ETok::Min             : return 200;
			case ETok::Max             : return 200;
			case ETok::Clamp           : return 200;
			case ETok::Sin             : return 200;
			case ETok::Cos             : return 200;
			case ETok::Tan             : return 200;
			case ETok::ASin            : return 200;
			case ETok::ACos            : return 200;
			case ETok::ATan            : return 200;
			case ETok::ATan2           : return 200;
			case ETok::SinH            : return 200;
			case ETok::CosH            : return 200;
			case ETok::TanH            : return 200;
			case ETok::Exp             : return 200;
			case ETok::Log             : return 200;
			case ETok::Log10           : return 200;
			case ETok::Pow             : return 200;
			case ETok::Sqr             : return 200;
			case ETok::Sqrt            : return 200;
			case ETok::Len2            : return 200;
			case ETok::Len3            : return 200;
			case ETok::Len4            : return 200;
			case ETok::Deg             : return 200;
			case ETok::Rad             : return 200;
			case ETok::Hash            : return 200;
			case ETok::OpenParenthesis : return 300;
			case ETok::CloseParenthesis: return 300;
			case ETok::Value           : return 1000;
			case ETok::Identifier      : return 1000;
			}
		}

		// Advance 'expr' to the next non-whitespace character
		template <typename Char> char_range<Char> EatWS(char_range<Char>& expr)
		{
			for (; expr && std::isspace(*expr); ++expr) {}
			return expr;
		}

		// Read a value (greedily) from 'expr'
		template <typename Char> bool ReadValue(char_range<Char>& expr, Val& out)
		{
			// Conversion functions
			auto ReadReal = [](char_range<Char> src, Val& v) -> size_t
			{
				try
				{
					size_t len;
					v = std::stod(src.str(), &len);
					return len;
				}
				catch (std::invalid_argument const&)
				{
					return 0;
				}
			};
			auto RealIntegral = [](char_range<Char> src, Val& v) -> size_t
			{
				Val sval; size_t len0;
				try { sval = std::stoll(src.str(), &len0, 0); }
				catch (std::out_of_range const&) { len0 = 0; }
				catch (std::invalid_argument const&) { len0 = 0; }
				
				Val uval; size_t len1;
				try { uval = std::stoull(src.str(), &len1, 0); }
				catch (std::out_of_range const&) { len1 = 0; }
				catch (std::invalid_argument const&) { len1 = 0; }

				auto len = std::max(len0, len1);
				if (len == 0)
					return 0;

				// Ignore the optional suffix
				src += static_cast<int>(len);
				if (*src == 'u' || *src == 'U') { ++src; ++len; }
				if (*src == 'l' || *src == 'L') { ++src; ++len; }
				if (*src == 'l' || *src == 'L') { ++src; ++len; }

				// Return the integral value
				v = len0 != 0 ? sval :
					len1 != 0 ? uval :
					throw std::runtime_error("at least one is supposed to be valid");
				return len;
			};
			auto ReadLiteral = [](char_range<Char> src, Val& v) -> size_t
			{
				auto beg = src.m_ptr;

				if (*src != '\'')
					return 0;

				// Allow for escaped characters
				if (*++src == '\\')
				{
					switch (*++src)
					{
					default: break;
					case 'a':  v = '\a'; ++src; break;
					case 'b':  v = '\b'; ++src; break;
					case 'f':  v = '\f'; ++src; break;
					case 'n':  v = '\n'; ++src; break;
					case 'r':  v = '\r'; ++src; break;
					case 't':  v = '\t'; ++src; break;
					case 'v':  v = '\v'; ++src; break;
					case '\'': v = '\''; ++src; break;
					case '\"': v = '\"'; ++src; break;
					case '\\': v = '\\'; ++src; break;
					case '\?': v = '\?'; ++src; break;
					case '0':
					case '1':
					case '2':
					case '3':
						{
							// ASCII character in octal
							std::basic_string<Char> oct;
							for (; *src && *src >= '0' && *src <= '7'; ++src) oct.append(1, *src);
							try { v = std::stoll(oct, nullptr, 8); }
							catch (std::out_of_range const&) { return 0; }
							break;
						}
					case 'x':
						{
							// ASCII or UNICODE character in hex
							std::basic_string<Char> hex;
							for (; *src && std::isxdigit(*src); ++src) hex.append(1, *src);
							try { v = std::stoll(hex, nullptr, 16); }
							catch (std::out_of_range const&) { return 0; }
							break;
						}
					}
				}
				else
				{
					v = *src;
					++src;
				}
				
				if (*src != '\'')
					return 0;
					
				++src;
				return src.m_ptr - beg;
			};

			// Greedy read, whichever consumes the most characters is the interpretted value.
			// Prefer integral over real because inegral values get promoted to real.
			Val v; size_t len = 0;
			if (auto l = RealIntegral(expr, v); l > len) { len = l; out = v; }
			if (auto l = ReadReal    (expr, v); l > len) { len = l; out = v; }
			if (auto l = ReadLiteral (expr, v); l > len) { len = l; out = v; }
			if (len == 0)
				return false;

			expr += static_cast<int>(len);
			return true;
		}

		// Read an identifier (greedily) from 'expr'
		template <typename Char> bool ReadIdentifier(char_range<Char>& expr, char_range<Char>& ident)
		{
			// Identifiers start with a letter or underscore
			if (!std::isalpha(*expr) && *expr != '_')
				return false;

			// Greedily read letters, digits, and underscores.
			ident.m_ptr = expr.m_ptr;
			for (; expr && (std::isalnum(*expr) || *expr == '_'); ++expr) {}
			ident.m_end = expr.m_ptr;
			return true;
		}

		// Extract a token from 'expr'
		// 'follows_value' should be true if the preceding expression evaluates to a value
		template <typename Char> ETok Token(char_range<Char>& expr, Val& val, char_range<Char>& ident, bool follows_value)
		{
			// Case insensitive string compare
			auto cmp = [](char_range<Char> s, char const* pattern)
			{
				for (; s && *pattern == std::tolower(*s); ++s, ++pattern) {}
				return *pattern == 0;
			};

			// Skip any leading whitespace
			if (!EatWS(expr))
				return ETok::None;

			// Try an operator
			// Convert Add/Sub to unary plus/minus by looking at the previous expression
			// If the previous expression evaluates to a value then Add/Sub are binary expressions
			switch (std::tolower(*expr))
			{
			default: break;
			case '+': 
				{
					expr += 1;
					return follows_value ? ETok::Add : ETok::UnaryPlus;
				}
			case '-': 
				{
					expr += 1;
					return follows_value ? ETok::Sub : ETok::UnaryMinus;
				}
			case '*': 
				{
					expr += 1;
					return ETok::Mul;
				}
			case '/': 
				{
					expr += 1;
					return ETok::Div;
				}
			case '%':
				{
					expr += 1;
					return ETok::Mod;
				}
			case '~':
				{
					expr += 1;
					return ETok::Comp;
				}
			case ',':
				{
					expr += 1;
					return ETok::Comma;
				}
			case '^':
				{
					expr += 1;
					return ETok::BitXOR;
				}
			case '(':
				{
					expr += 1;
					return ETok::OpenParenthesis;
				}
			case ')': 
				{
					expr += 1;
					return ETok::CloseParenthesis;
				}
			case '?':
				{
					expr += 1;
					return ETok::If;
				}
			case ':':
				{
					expr += 1;
					return ETok::Else;
				}
			case '<':
				{
					if (cmp(expr, "<<")) { expr += 2; return ETok::LeftShift; }
					if (cmp(expr, "<=")) { expr += 2; return ETok::LogLTEql; }
					expr += 1;
					return ETok::LogLT;
				}
			case '>':
				{
					if (cmp(expr, ">>")) { expr += 2; return ETok::RightShift; }
					if (cmp(expr, ">=")) { expr += 2; return ETok::LogGTEql; }
					expr += 1;
					return ETok::LogGT;
				}
			case '|':
				{
					if (cmp(expr, "||")) { expr += 2; return ETok::LogOR; }
					expr += 1;
					return ETok::BitOR;
				}
			case '&':
				{
					if (cmp(expr, "&&")) { expr += 2; return ETok::LogAND; }
					expr += 1;
					return ETok::BitAND;
				}
			case '=':
				{
					if (cmp(expr, "==")) { expr += 2; return ETok::LogEql; }
					break;
				}
			case '!':
				{
					if (cmp(expr, "!=")) { expr += 2; return ETok::LogNEql; }
					expr += 1;
					return ETok::Not;
				}
			case 'a':
				{
					if (cmp(expr, "abs")) { expr += 3; return ETok::Abs; }
					if (cmp(expr, "asin")) { expr += 4; return ETok::ASin; }
					if (cmp(expr, "acos")) { expr += 4; return ETok::ACos; }
					if (cmp(expr, "atan2")) { expr += 5; return ETok::ATan2; }
					if (cmp(expr, "atan")) { expr += 4; return ETok::ATan; }
					break;
				}
			case 'c':
				{
					if (cmp(expr, "clamp")) { expr += 5; return ETok::Clamp; }
					if (cmp(expr, "ceil")) { expr += 4; return ETok::Ceil; }
					if (cmp(expr, "cosh")) { expr += 4; return ETok::CosH; }
					if (cmp(expr, "cos")) { expr += 3; return ETok::Cos; }
					break;
				}
			case 'd':
				{
					if (cmp(expr, "deg")) { expr += 3; return ETok::Deg; }
					break;
				}
			case 'e':
				{
					if (cmp(expr, "exp")) { expr += 3; return ETok::Exp; }
					break;
				}
			case 'f':
				{
					if (cmp(expr, "floor")) { expr += 5; return ETok::Floor; }
					if (cmp(expr, "fmod")) { expr += 4; return ETok::Fmod; }
					if (cmp(expr, "false")) { expr += 5; val = 0.0; return ETok::Value; }
					break;
				}
			case 'h':
				{
					if (cmp(expr, "hash")) { expr += 4; return ETok::Hash; }
					break;
				}
			case 'l':
				{
					if (cmp(expr, "log10")) { expr += 5; return ETok::Log10; }
					if (cmp(expr, "log")) { expr += 3; return ETok::Log; }
					if (cmp(expr, "len2")) { expr += 4; return ETok::Len2; }
					if (cmp(expr, "len3")) { expr += 4; return ETok::Len3; }
					if (cmp(expr, "len4")) { expr += 4; return ETok::Len4; }
					break;
				}
			case 'm':
				{
					if (cmp(expr, "min")) { expr += 3; return ETok::Min; }
					if (cmp(expr, "max")) { expr += 3; return ETok::Max; }
					break;
				}
			case 'p':
				{
					if (cmp(expr, "pow")) { expr += 3; return ETok::Pow; }
					if (cmp(expr, "phi")) { expr += 3; val = PHI; return ETok::Value; }
					if (cmp(expr, "pi")) { expr += 2; val = TAU / 2.0; return ETok::Value; }
					break;
				}
			case 'r':
				{
					if (cmp(expr, "round")) { expr += 5; return ETok::Round; }
					if (cmp(expr, "rad")) { expr += 3; return ETok::Rad; }
					break;
				}
			case 's':
				{
					if (cmp(expr, "sinh")) { expr += 4; return ETok::SinH; }
					if (cmp(expr, "sin")) { expr += 3; return ETok::Sin; }
					if (cmp(expr, "sqrt")) { expr += 4; return ETok::Sqrt; }
					if (cmp(expr, "sqr")) { expr += 3; return ETok::Sqr; }
					break;
				}
			case 't':
				{
					if (cmp(expr, "tanh")) { expr += 4; return ETok::TanH; }
					if (cmp(expr, "tan")) { expr += 3; return ETok::Tan; }
					if (cmp(expr, "tau")) { expr += 3; val = TAU; return ETok::Value; }
					if (cmp(expr, "true")) { expr += 4; val = 1.0; return ETok::Value; }
					break;
				}
			}

			// Try a variable (identifier)
			if (ReadIdentifier(expr, ident))
				return ETok::Identifier;

			// Try an operand
			if (ReadValue(expr, val))
				return ETok::Value;
			
			return ETok::None;
		}

		// Evaluate an expression.
		// Called recursively for each operation within an expression.
		// 'parent_op' is used to determine precedence order.
		template <typename Char> bool Eval(char_range<Char>& expr, std::span<Val> result, int ridx, ETok parent_op, bool l2r = true)
		{
			// Each time round the while loop should result in a value.
			// Operation tokens result in recursive calls.
			bool follows_value = false;
			for (; expr; )
			{
				Val val;
				char_range<Char> ident;
				auto expr0 = expr;
				auto tok = Token(expr, val, ident, follows_value);
				follows_value = true;

				// If the next token has lower precedence than the parent operation
				// then return to allow the parent op to evaluate.
				auto prec0 = Precedence(tok);
				auto prec1 = Precedence(parent_op);
				if (prec0 < prec1 || (prec0 == prec1 && l2r))
				{
					// Restore the expr so that the last token is returned to the expr.
					expr = expr0;
					return true;
				}
				
				switch (tok)
				{
				case ETok::None:
					{
						return expr;
					}
				case ETok::Identifier:
					{
						throw std::runtime_error("evaluated expressions cannot contain variables");
					}
				case ETok::Value:
					{
						result[ridx] = val;
						break;
					}
				case ETok::Add:
					{
						std::array<Val, 1> rhs;
						if (!Eval(expr, rhs, 0, tok)) return false;
						result[ridx] = result[ridx] + rhs[0];
						break;
					}
				case ETok::Sub:
					{
						std::array<Val, 1> rhs;
						if (!Eval(expr, rhs, 0, tok)) return false;
						result[ridx] = result[ridx] - rhs[0];
						break;
					}
				case ETok::Mul:
					{
						std::array<Val, 1> rhs;
						if (!Eval(expr, rhs, 0, tok)) return false;
						result[ridx] = result[ridx] * rhs[0];
						break;
					}
				case ETok::Div:
					{
						std::array<Val, 1> rhs;
						if (!Eval(expr, rhs, 0, tok)) return false;
						result[ridx] = result[ridx] / rhs[0];
						break;
					}
				case ETok::Mod:
					{
						std::array<Val, 1> rhs;
						if (!Eval(expr, rhs, 0, tok)) return false;
						result[ridx] = result[ridx].ll() % rhs[0].ll();
						break;
					}
				case ETok::UnaryPlus:
					{
						std::array<Val, 1> rhs;
						if (!Eval(expr, rhs, 0, tok, false)) return false;
						result[ridx] = rhs[0];
						break;
					}
				case ETok::UnaryMinus:
					{
						std::array<Val, 1> rhs;
						if (!Eval(expr, rhs, 0, tok, false)) return false;
						switch (rhs[0].m_ty) {
						case Val::EType::Real: result[ridx] = -rhs[0].db(); break; // don't use ?: as the result will always be double
						case Val::EType::Intg: result[ridx] = -rhs[0].ll(); break; // result[ridx].ll() will get promoted to double
						default: throw std::runtime_error("Unknown result type for unary minus");
						}
						break;
					}
				case ETok::Comp:
					{
						std::array<Val, 1> rhs;
						if (!Eval(expr, rhs, 0, tok, false)) return false;
						result[ridx] = ~rhs[0].ll();
						break;
					}
				case ETok::Not:
					{
						std::array<Val, 1> rhs;
						if (!Eval(expr, rhs, 0, tok, false)) return false;
						result[ridx] = !rhs[0].ll();
						break;
					}
				case ETok::LogOR:
					{
						std::array<Val, 1> rhs;
						if (!Eval(expr, rhs, 0, tok)) return false;
						result[ridx] = result[ridx].ll() || rhs[0].ll();
						break;
					}
				case ETok::LogAND:
					{
						std::array<Val, 1> rhs;
						if (!Eval(expr, rhs, 0, tok)) return false;
						result[ridx] = result[ridx].ll() && rhs[0].ll();
						break;
					}
				case ETok::LogEql:
					{
						std::array<Val, 1> rhs;
						if (!Eval(expr, rhs, 0, tok)) return false;
						result[ridx] = result[ridx] == rhs[0];
						break;
					}
				case ETok::LogNEql:
					{
						std::array<Val, 1> rhs;
						if (!Eval(expr, rhs, 0, tok)) return false;
						result[ridx] = !(result[ridx] == rhs[0]);
						break;
					}
				case ETok::LogLT:
					{
						std::array<Val, 1> rhs;
						if (!Eval(expr, rhs, 0, tok)) return false;
						result[ridx] = result[ridx] < rhs[0];
						break;
					}
				case ETok::LogLTEql:
					{
						std::array<Val, 1> rhs;
						if (!Eval(expr, rhs, 0, tok)) return false;
						result[ridx] = result[ridx] <= rhs[0];
						break;
					}
				case ETok::LogGT:
					{
						std::array<Val, 1> rhs;
						if (!Eval(expr, rhs, 0, tok)) return false;
						result[ridx] = !(result[ridx] <= rhs[0]);
						break;
					}
				case ETok::LogGTEql:
					{
						std::array<Val, 1> rhs;
						if (!Eval(expr, rhs, 0, tok)) return false;
						result[ridx] = !(result[ridx] < rhs[0]);
						break;
					}
				case ETok::BitOR:
					{
						std::array<Val, 1> rhs;
						if (!Eval(expr, rhs, 0, tok)) return false;
						result[ridx] = result[ridx].ll() | rhs[0].ll();
						break;
					}
				case ETok::BitXOR:
					{
						std::array<Val, 1> rhs;
						if (!Eval(expr, rhs, 0, tok)) return false;
						result[ridx] = result[ridx].ll() ^ rhs[0].ll();
						break;
					}
				case ETok::BitAND:
					{
						std::array<Val, 1> rhs;
						if (!Eval(expr, rhs, 0, tok)) return false;
						result[ridx] = result[ridx].ll() & rhs[0].ll();
						break;
					}
				case ETok::LeftShift:
					{
						std::array<Val, 1> rhs;
						if (!Eval(expr, rhs, 0, tok)) return false;
						result[ridx] = static_cast<int64_t>(static_cast<uint64_t>(result[ridx].ll()) << rhs[0].ll());
						break;
					}
				case ETok::RightShift:
					{
						std::array<Val, 1> rhs;
						if (!Eval(expr, rhs, 0, tok)) return false;
						result[ridx] = static_cast<int64_t>(static_cast<uint64_t>(result[ridx].ll()) >> rhs[0].ll());
						break;
					}
				case ETok::Fmod:
					{
						std::array<Val, 2> args;
						if (!Eval(expr, args, 0, tok)) return false;
						result[ridx] = std::fmod(args[0].db(), args[1].db());
						break;
					}
				case ETok::Ceil:
					{
						std::array<Val, 1> args;
						if (!Eval(expr, args, 0, tok)) return false;
						result[ridx] = std::ceil(args[0].db());
						break;
					}
				case ETok::Floor:
					{
						std::array<Val, 1> args;
						if (!Eval(expr, args, 0, tok)) return false;
						result[ridx] = std::floor(args[0].db());
						break;
					}
				case ETok::Round:
					{
						std::array<Val, 1> args;
						if (!Eval(expr, args, 0, tok)) return false;
						result[ridx] = std::round(args[0].db());
						break;
					}
				case ETok::Min:
					{
						std::array<Val, 2> args;
						if (!Eval(expr, args, 0, tok)) return false;
						result[ridx] = args[0] < args[1] ? args[0] : args[1];
						break;
					}
				case ETok::Max:
					{
						std::array<Val, 2> args;
						if (!Eval(expr, args, 0, tok)) return false;
						result[ridx] = args[0] < args[1] ? args[1] : args[0];
						break;
					}
				case ETok::Clamp:
					{
						std::array<Val, 3> args;
						if (!Eval(expr, args, 0, tok)) return false;
						result[ridx] = args[0] < args[1] ? args[1] : args[2] < args[0] ? args[2] : args[0];
						break;
					}
				case ETok::Abs:
					{
						std::array<Val, 1> args;
						if (!Eval(expr, args, 0, tok)) return false;
						result[ridx] = 
							args[0].m_ty == Val::EType::Intg ? std::abs(args[0].ll()) :
							args[0].m_ty == Val::EType::Real ? std::abs(args[0].db()) :
							throw std::runtime_error("Invalid argument for 'abs'");
						break;
					}
				case ETok::Sin:
					{
						std::array<Val, 1> args;
						if (!Eval(expr, args, 0, tok)) return false;
						result[ridx] = std::sin(args[0].db());
						break;
					}
				case ETok::Cos:
					{
						std::array<Val, 1> args;
						if (!Eval(expr, args, 0, tok)) return false;
						result[ridx] = std::cos(args[0].db());
						break;
					}
				case ETok::Tan:
					{
						std::array<Val, 1> args;
						if (!Eval(expr, args, 0, tok)) return false;
						result[ridx] = std::tan(args[0].db());
						break;
					}
				case ETok::ASin:
					{
						std::array<Val, 1> args;
						if (!Eval(expr, args, 0, tok)) return false;
						result[ridx] = std::asin(args[0].db());
						break;
					}
				case ETok::ACos:
					{
						std::array<Val, 1> args;
						if (!Eval(expr, args, 0, tok)) return false;
						result[ridx] = std::acos(args[0].db());
						break;
					}
				case ETok::ATan:
					{
						std::array<Val, 1> args;
						if (!Eval(expr, args, 0, tok)) return false;
						result[ridx] = std::atan(args[0].db());
						break;
					}
				case ETok::ATan2:
					{
						std::array<Val, 2> args;
						if (!Eval(expr, args, 0, tok)) return false;
						result[ridx] = std::atan2(args[0].db(), args[1].db());
						break;
					}
				case ETok::SinH:
					{
						std::array<Val, 1> args;
						if (!Eval(expr, args, 0, tok)) return false;
						result[ridx] = std::sinh(args[0].db());
						break;
					}
				case ETok::CosH:
					{
						std::array<Val, 1> args;
						if (!Eval(expr, args, 0, tok)) return false;
						result[ridx] = std::cosh(args[0].db());
						break;
					}
				case ETok::TanH:
					{
						std::array<Val, 1> args;
						if (!Eval(expr, args, 0, tok)) return false;
						result[ridx] = std::tanh(args[0].db());
						break;
					}
				case ETok::Exp:
					{
						std::array<Val, 1> args;
						if (!Eval(expr, args, 0, tok)) return false;
						result[ridx] = std::exp(args[0].db());
						break;
					}
				case ETok::Log:
					{
						std::array<Val, 1> args;
						if (!Eval(expr, args, 0, tok)) return false;
						result[ridx] = std::log(args[0].db());
						break;
					}
				case ETok::Log10:
					{
						std::array<Val, 1> args;
						if (!Eval(expr, args, 0, tok)) return false;
						result[ridx] = std::log10(args[0].db());
						break;
					}
				case ETok::Pow:
					{
						std::array<Val, 2> args;
						if (!Eval(expr, args, 0, tok)) return false;
						result[ridx] = std::pow(args[0].db(), args[1].db());
						break;
					}
				case ETok::Sqr:
					{
						std::array<Val, 1> args;
						if (!Eval(expr, args, 0, tok)) return false;
						result[ridx] = args[0] * args[0];
						break;
					}
				case ETok::Sqrt:
					{
						std::array<Val, 1> args;
						if (!Eval(expr, args, 0, tok)) return false;
						result[ridx] = std::sqrt(args[0].db());
						break;
					}
				case ETok::Len2:
					{
						std::array<Val, 2> args;
						if (!Eval(expr, args, 0, tok)) return false;
						result[ridx] = std::sqrt(0.0 + args[0].db()*args[0].db() + args[1].db()*args[1].db());
						break;
					}
				case ETok::Len3:
					{
						std::array<Val, 3> args;
						if (!Eval(expr, args, 0, tok)) return false;
						result[ridx] = std::sqrt(0.0 + args[0].db()*args[0].db() + args[1].db()*args[1].db() + args[2].db()*args[2].db());
						break;
					}
				case ETok::Len4:
					{
						std::array<Val, 4> args;
						if (!Eval(expr, args, 0, tok)) return false;
						result[ridx] = std::sqrt(0.0 + args[0].db()*args[0].db() + args[1].db()*args[1].db() + args[2].db()*args[2].db() + args[3].db()*args[3].db());
						break;
					}
				case ETok::Deg:
					{
						std::array<Val, 1> args;
						if (!Eval(expr, args, 0, tok)) return false;
						result[ridx] = args[0].db() * (360.0 / TAU);
						break;
					}
				case ETok::Rad:
					{
						std::array<Val, 1> args;
						if (!Eval(expr, args, 0, tok)) return false;
						result[ridx] = args[0].db() * (TAU / 360.0);
						break;
					}
				case ETok::Hash:
					{
						std::basic_string<Char> str;
						EatWS(expr);
						if (*expr == '(') ++expr; else return false;
						EatWS(expr);
						if (*expr == '"') ++expr; else return false;
						for (bool esc = false; expr && (esc || *expr != '\"'); esc = !esc && *expr == '\\', ++expr) str.append(1, *expr);
						if (*expr == '"') ++expr; else return false;
						result[ridx] = static_cast<long long>(hash::HashCT(str.data(), str.data() + str.size()));
						break;
					}
				case ETok::Comma:
					{
						if (ridx + 1 == static_cast<int>(result.size()))
							throw std::runtime_error("too many parameters");

						++ridx;
						follows_value = false;
						break;
					}
				case ETok::OpenParenthesis:
					{
						// Parent op is 'None' because it has the lowest precedence
						if (!Eval(expr, result, ridx, ETok::None)) return false;
						break;
					}
				case ETok::CloseParenthesis:
					{
						// Wait for the parent op to be the 'Open Parenthesis'
						if (parent_op != ETok::None) expr += -1;
						return true;
					}
				case ETok::If:
					{
						std::array<Val, 2> args;
						if (!Eval(expr, args, 0, ETok::None)) return false;
						result[ridx] = result[ridx].ll() != 0 ? args[0] : args[1];
						break;
					}
				case ETok::Else:
					{
						if (ridx + 1 == static_cast<int>(result.size()))
							throw std::runtime_error("result buffer too small");

						if (!Eval(expr, result, ++ridx, ETok::Else)) return false;
						++ridx;
						return true;
					}
				default:
					{
						throw std::runtime_error("unknown expression token");
					}
				}
			}
			return true;
		}
	
		// Compile an expression
		template <typename Char> bool Compile(char_range<Char>& expr, Expression& compiled, ETok parent_op, bool l2r = true)
		{
			// A flag used to distingush ambiguous operators such as + and -
			bool follows_value = false;

			// Each time round the while loop should result in an operation being
			// added to the expression. Operation tokens result in recursive calls.
			for (; expr; )
			{
				Val val;
				char_range<Char> ident;
				auto expr0 = expr;
				auto tok = Token(expr, val, ident, follows_value);
				follows_value = true;

				// If the next token has lower precedence than the parent operation
				// then return to allow the parent op to evaluate.
				auto prec0 = Precedence(tok);
				auto prec1 = Precedence(parent_op);
				if (prec0 < prec1 || (prec0 == prec1 && l2r))
				{
					// Restore the expr so that the last token is returned to the expr.
					expr = expr0;
					return true;
				}

				switch (tok)
				{
				case ETok::None:
					{
						return expr;
					}
				case ETok::Identifier:
					{
						auto name = Narrow(ident.str_view());
						auto count = static_cast<uint8_t>(std::min<size_t>(255, name.size()));
						compiled.m_op.append(tok);
						compiled.m_op.append(count);
						compiled.m_op.append(name.c_str(), count);
						compiled.AddVariable(Arg(name));
						break;
					}
				case ETok::Value:
					{
						// Manually serialise 'val' to avoid structure padding being added to the CodeBuf.
						// Could make this a function, but it's only used in a couple of places and I don't
						// want to polute the 'eval' namespace.
						compiled.m_op.append(tok);
						compiled.m_op.append(val.m_ty);
						val.m_ty == Val::EType::Intg ? compiled.m_op.append(val.m_ll) :
						val.m_ty == Val::EType::Real ? compiled.m_op.append(val.m_db) :
						throw std::runtime_error("Invalid literal value");
						break;
					}
				case ETok::Add:
				case ETok::Sub:
				case ETok::Mul:
				case ETok::Div:
				case ETok::Mod:
					{
						if (!Compile(expr, compiled, tok)) return false;
						compiled.m_op.append(tok);
						break;
					}
				case ETok::UnaryPlus:
				case ETok::UnaryMinus:
				case ETok::Not:
				case ETok::Comp:
					{
						if (!Compile(expr, compiled, tok, false)) return false;
						compiled.m_op.append(tok);
						break;
					}
				case ETok::LogOR:
				case ETok::LogAND:
				case ETok::LogEql:
				case ETok::LogNEql:
				case ETok::LogLTEql:
				case ETok::LogGTEql:
				case ETok::LogLT:
				case ETok::LogGT:
					{
						if (!Compile(expr, compiled, tok)) return false;
						compiled.m_op.append(tok);
						break;
					}
				case ETok::BitOR:
				case ETok::BitXOR:
				case ETok::BitAND:
				case ETok::LeftShift:
				case ETok::RightShift:
					{
						if (!Compile(expr, compiled, tok)) return false;
						compiled.m_op.append(tok);
						break;
					}
				case ETok::Fmod:
				case ETok::Ceil:
				case ETok::Floor:
				case ETok::Round:
				case ETok::Min:
				case ETok::Max:
				case ETok::Clamp:
				case ETok::Abs:
				case ETok::Sin:
				case ETok::Cos:
				case ETok::Tan:
				case ETok::ASin:
				case ETok::ACos:
				case ETok::ATan:
				case ETok::ATan2:
				case ETok::SinH:
				case ETok::CosH:
				case ETok::TanH:
				case ETok::Exp:
				case ETok::Log:
				case ETok::Log10:
				case ETok::Pow:
				case ETok::Sqr:
				case ETok::Sqrt:
				case ETok::Len2:
				case ETok::Len3:
				case ETok::Len4:
				case ETok::Deg:
				case ETok::Rad:
					{
						if (!Compile(expr, compiled, tok)) return false;
						compiled.m_op.append(tok);
						break;
					}
				case ETok::Hash:
					{
						// Hash only supports literal strings, which are turned into int64 values
						std::basic_string<Char> str;
						EatWS(expr);
						if (*expr == '(') ++expr; else return false;
						EatWS(expr);
						if (*expr == '"') ++expr; else return false;
						for (bool esc = false; expr && (esc || *expr != '\"'); esc = !esc && *expr == '\\', ++expr) str.append(1, *expr);
						if (*expr == '"') ++expr; else return false;
				
						auto hash = static_cast<long long>(hash::HashCT(str.data(), str.data() + str.size()));
						compiled.m_op.append(ETok::Value);
						compiled.m_op.append(Val::EType::Intg);
						compiled.m_op.append(hash);
						break;
					}
				case ETok::Comma:
					{
						follows_value = false;
						break;
					}
				case ETok::OpenParenthesis:
					{
						// Parent op is 'None' because it has the lowest precedence
						if (!Compile(expr, compiled, ETok::None)) return false;
						break;
					}
				case ETok::CloseParenthesis:
					{
						// Wait for the parent op to be the 'Open Parenthesis'
						if (parent_op != ETok::None) expr += -1;
						return true;
					}
				case ETok::If:
					{
						// The boolean expression should already be in 'm_op' because it occurs to
						// the left of the ternary ?: operator.

						// Add the 'If' token which is basically a branch-if-zero instruction. i.e.
						// if the previous value is zero, branch past the 'if' body.
						compiled.m_op.append(tok);

						// Record the location of the branch offset so it can be updated
						// and write a dummy branch offset in the meantime.
						auto ofs0 = compiled.m_op.size();
						compiled.m_op.append(0);

						// Compile the 'if' body
						if (!Compile(expr, compiled, ETok::If)) return false;

						// Determine the offset to jump over the if body. The jump is from the byte after the jump value.
						auto jmp = static_cast<int>(compiled.m_op.size() - ofs0 - sizeof(int));
						compiled.m_op.at_byte_ofs<int>(ofs0) = jmp;
						break;
					}
				case ETok::Else:
					{
						// Add the 'Else' token which is basically a branch-always instruction.
						// Executing an 'If' statement will jump over this instruction so that the else statement is executed.
						compiled.m_op.append(tok);

						// Record the location of the branch offset so it can be updated
						// and write a dummy branch offset in the meantime.
						auto ofs0 = compiled.m_op.size();
						compiled.m_op.append(0);

						// Compile the else body
						if (!Compile(expr, compiled, ETok::Else)) return false;

						// Determine the offset to jump over the else body
						auto jmp = static_cast<int>(compiled.m_op.size() - ofs0 - sizeof(int));
						compiled.m_op.at_byte_ofs<int>(ofs0) = jmp;
						return true;
					}
				default:
					{
						throw std::runtime_error("unknown expression token");
					}
				}
			}
			return true;
		}

		// Evaluate an expression.
		template <typename Char>
		inline Val Evaluate(char_range<Char> expr)
		{
			std::array<Val, 1> result;
			Eval(expr, result, 0, ETok::None);
			return result[0];
		}
		template <typename Char, typename ResType>
		inline bool Evaluate(char_range<Char> expr, ResType& out)
		{
			try
			{
				auto val = Evaluate<Char>(expr);
				if constexpr(std::is_integral_v<ResType>)
					out = static_cast<ResType>(val.ll());
				else if constexpr(std::is_floating_point_v<ResType>)
					out = static_cast<ResType>(val.db());
				else
					static_assert(false, "Unsupported result type");

				return true;
			}
			catch (std::exception const&)
			{
				return false;
			}
		}

		// Compile an expression
		template <typename Char>
		inline Expression Compile(char_range<Char> expr)
		{
			Expression compiled;
			Compile(expr, compiled, ETok::None);
			return std::move(compiled);
		}
	}

	// The following functions are not templated on character type because
	// type deduction cannot convert string types to 'basic_string_view<Char>'.

	// Evaluate an expression. Throws on syntax error.
	inline eval::Val Evaluate(std::string_view expr)
	{
		return eval::Evaluate<char>(expr);
	}
	inline eval::Val Evaluate(std::wstring_view expr)
	{
		return eval::Evaluate<wchar_t>(expr);
	}
	template <typename ResType> inline bool Evaluate(std::string_view expr, ResType& out)
	{
		return eval::Evaluate<char, ResType>(expr, out);
	}
	template <typename ResType> inline bool Evaluate(std::wstring_view expr, ResType& out)
	{
		return eval::Evaluate<wchar_t, ResType>(expr, out);
	}

	// Compile an expression. Throws on syntax error
	inline eval::Expression Compile(std::string_view expr)
	{
		return eval::Compile<char>(expr);
	}
	inline eval::Expression Compile(std::wstring_view expr)
	{
		return eval::Compile<wchar_t>(expr);
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/maths/maths.h"
namespace pr::common
{
	namespace unitests::expr_eval
	{
		template <typename ResType> bool Expr(std::string_view expr, ResType result) { static_assert(false); }
		template <> bool Expr(std::string_view expr, double                  result) { double        val; return Evaluate(expr, val) && FEql(val, result); }
		template <> bool Expr(std::string_view expr, float                   result) { float         val; return Evaluate(expr, val) && FEql(val, result); }
		template <> bool Expr(std::string_view expr, uint64_t                result) { uint64_t      val; return Evaluate(expr, val) && val == result; }
		template <> bool Expr(std::string_view expr, int64_t                 result) { int64_t       val; return Evaluate(expr, val) && val == result; }
		template <> bool Expr(std::string_view expr, unsigned long           result) { unsigned long val; return Evaluate(expr, val) && val == result; }
		template <> bool Expr(std::string_view expr, long                    result) { long          val; return Evaluate(expr, val) && val == result; }
		template <> bool Expr(std::string_view expr, uint32_t                result) { uint32_t      val; return Evaluate(expr, val) && val == result; }
		template <> bool Expr(std::string_view expr, int                     result) { int           val; return Evaluate(expr, val) && val == result; }
		template <> bool Expr(std::string_view expr, bool                    result) { bool          val; return Evaluate(expr, val) && val == result; }

		template <typename ValueType>
		bool ReadValue(pr::eval::char_range<char> expr, ValueType expected_result)
		{
			pr::eval::Val val;
			if (!pr::eval::ReadValue(expr, val))
				return false;

			if constexpr (std::is_integral_v<ValueType> && std::is_unsigned_v<ValueType>)
				return static_cast<uint64_t>(expected_result) == static_cast<uint64_t>(val.ll());
			else if constexpr (std::is_integral_v<ValueType>)
				return expected_result == static_cast<ValueType>(val.ll());
			else if constexpr (std::is_floating_point_v<ValueType>)
				return expected_result == static_cast<ValueType>(val.db());
			else
				static_assert(false);
		}
	}

	PRUnitTest(ExprEvalTests)
	{
		using namespace unitests::expr_eval;
		using namespace pr::eval;
		{
			#define VAL(exp) ReadValue(#exp, (exp))
			PR_CHECK(VAL(1), true);
			PR_CHECK(VAL(1.0), true);
			PR_CHECK(VAL(-1), true);
			PR_CHECK(VAL(-1.0), true);
			PR_CHECK(VAL(10U), true);
			PR_CHECK(VAL(100L), true);
			PR_CHECK(VAL(-100L), true);
			PR_CHECK(VAL(0x1000UL), true);
			PR_CHECK(VAL(0x7FFFFFFF), true);
			PR_CHECK(VAL(0x80000000), true);
			PR_CHECK(VAL(0xFFFFFFFF), true);
			PR_CHECK(VAL(0xFFFFFFFFU), true);
			PR_CHECK(VAL(0xFFFFFFFFULL), true);
			PR_CHECK(VAL(0x7FFFFFFFFFFFFFFFLL), true);
			PR_CHECK(VAL(0xFFFFFFFFFFFFFFFFULL), true);
			#undef VAL
		}
		{
			#define EXPR(exp) Expr(#exp, (exp))
			PR_CHECK(EXPR(1.0), true);
			PR_CHECK(EXPR(+1.0), true);
			PR_CHECK(EXPR(-1.0), true);
			PR_CHECK(EXPR(-(1.0 + 2.0)), true);
			PR_CHECK(EXPR(8.0 * -1.0), true);
			PR_CHECK(EXPR(4.0 * -1.0 + 2.0), true);
			PR_CHECK(EXPR(1.0 + +2.0), true);
			PR_CHECK(EXPR(1.0 - -2.0), true);
			PR_CHECK(EXPR(1.0 - 2.0 - 3.0 + 4.0), true);
			PR_CHECK(EXPR(1.0 * +2.0), true);
			PR_CHECK(EXPR(1 / 2), true);
			PR_CHECK(EXPR(1.0 / 2.0), true);
			PR_CHECK(EXPR(1.0 / 2.0 + 3.0), true);
			PR_CHECK(EXPR(1.0 / 2.0 * 3.0), true);
			PR_CHECK(EXPR((1 || 0) && 2), true);
			PR_CHECK(EXPR(((13 ^ 7) | 6) & 14), true);
			PR_CHECK(EXPR((8 < 9) + (3 <= 3) + (8 > 9) + (2 >= 2) + (1 != 2) + (2 == 2)), true);
			PR_CHECK(EXPR(1.0 + 2.0 * 3.0 - 4.0), true);
			PR_CHECK(EXPR(2.0 * 3.0 + 1.0 - 4.0), true);
			PR_CHECK(EXPR(1.0 - 4.0 + 2.0 * 3.0), true);
			PR_CHECK(EXPR((1.0 + 2.0) * 3.0 - 4.0), true);
			PR_CHECK(EXPR(1.0 + 2.0 * -(3.0 - 4.0)), true);
			PR_CHECK(EXPR(1.0 + (2.0 * (3.0 - 4.0))), true);
			PR_CHECK(EXPR((1.0 + 2.0) * (3.0 - 4.0)), true);
			PR_CHECK(EXPR(~37 & ~0), true);
			PR_CHECK(EXPR(!37 | !0), true);
			PR_CHECK(EXPR(~(0xFFFFFFFF >> 2)), true);
			PR_CHECK(EXPR(~(4294967295 >> 2)), true);
			PR_CHECK(EXPR(~(0xFFFFFFFFLL >> 2)), true);
			PR_CHECK(EXPR(~(4294967295LL >> 2)), true);
			PR_CHECK(Expr("sin(1.0 + 2.0)", std::sin(1.0 + 2.0)), true);
			PR_CHECK(Expr("cos(TAU)", std::cos(eval::TAU)), true);
			PR_CHECK(Expr("tan(PHI)", std::tan(eval::PHI)), true);
			PR_CHECK(Expr("abs( 1.0)", std::abs(1.0)), true);
			PR_CHECK(Expr("abs(-1.0)", std::abs(-1.0)), true);
			PR_CHECK(EXPR(11 % 3), true);
			PR_CHECK(Expr("fmod(11.3, 3.1)", std::fmod(11.3, 3.1)), true);
			PR_CHECK(Expr("3.0 * fmod(17.3, 2.1)", 3.0 * std::fmod(17.3, 2.1)), true);
			PR_CHECK(EXPR(1 << 10), true);
			PR_CHECK(EXPR(1024 >> 3), true);
			PR_CHECK(Expr("ceil(3.4)", std::ceil(3.4)), true);
			PR_CHECK(Expr("ceil(-3.4)", std::ceil(-3.4)), true);
			PR_CHECK(Expr("floor(3.4)", std::floor(3.4)), true);
			PR_CHECK(Expr("floor(-3.4)", std::floor(-3.4)), true);
			PR_CHECK(Expr("round( 3.5)", std::round(3.5)), true);
			PR_CHECK(Expr("round(-3.5)", std::round(-3.5)), true);
			PR_CHECK(Expr("round( 3.2)", std::round(3.2)), true);
			PR_CHECK(Expr("round(-3.2)", std::round(-3.2)), true);
			PR_CHECK(Expr("asin(-0.8)", std::asin(-0.8)), true);
			PR_CHECK(Expr("acos(0.2)", std::acos(0.2)), true);
			PR_CHECK(Expr("atan(2.3/12.9)", std::atan(2.3 / 12.9)), true);
			PR_CHECK(Expr("atan2(2.3,-3.9)", std::atan2(2.3, -3.9)), true);
			PR_CHECK(Expr("sinh(0.8)", std::sinh(0.8)), true);
			PR_CHECK(Expr("cosh(0.2)", std::cosh(0.2)), true);
			PR_CHECK(Expr("tanh(2.3)", std::tanh(2.3)), true);
			PR_CHECK(Expr("exp(2.3)", std::exp(2.3)), true);
			PR_CHECK(Expr("log(209.3)", std::log(209.3)), true);
			PR_CHECK(Expr("log10(209.3)", std::log10(209.3)), true);
			PR_CHECK(Expr("pow(2.3, -1.3)", std::pow(2.3, -1.3)), true);
			PR_CHECK(Expr("sqrt(2.3)", std::sqrt(2.3)), true);
			PR_CHECK(Expr("sqr(-2.3)", pr::Sqr(-2.3)), true);
			PR_CHECK(Expr("len2(3,4)", std::sqrt(3.0 * 3.0 + 4.0 * 4.0)), true);
			PR_CHECK(Expr("len3(3,4,5)", std::sqrt(3.0 * 3.0 + 4.0 * 4.0 + 5.0 * 5.0)), true);
			PR_CHECK(Expr("len4(3,4,5,6)", std::sqrt(3.0 * 3.0 + 4.0 * 4.0 + 5.0 * 5.0 + 6.0 * 6.0)), true);
			PR_CHECK(Expr("deg(-1.24)", -1.24 * (360.0 / eval::TAU)), true);
			PR_CHECK(Expr("rad(241.32)", 241.32 * (eval::TAU / 360.0)), true);
			PR_CHECK(Expr("min(-3.2, -3.4)", std::min(-3.2, -3.4)), true);
			PR_CHECK(Expr("max(-3.2, -3.4)", std::max(-3.2, -3.4)), true);
			PR_CHECK(Expr("clamp(10.0, -3.4, -3.2)", pr::Clamp(10.0, -3.4, -3.2)), true);
			PR_CHECK(Expr("hash(\"A String\")", hash::HashCT("A String")), true);
			PR_CHECK(Expr("sqr(sqrt(2.3)*-abs(4%2)/15.0-tan(TAU/-6))", pr::Sqr(std::sqrt(2.3) * -std::abs(4 % 2) / 15.0 - std::tan(eval::TAU / -6))), true);
			{
				long long v1 = 0, v0 = 123456789000000LL / 2;
				PR_CHECK(Evaluate("123456789000000 / 2", v1), true);
				PR_CHECK(v0 == v1, true);
			}
			PR_CHECK(Expr("1 != 2 ? 5 : 6", 5), true);
			PR_CHECK(Expr("1 == 2 ? 5 : 6", 6), true);
			PR_CHECK(Expr("true ? 5 : 6 + 1", 5), true);
			PR_CHECK(Expr("false ? 5 : 6 + 1", 7), true);
			PR_CHECK(Expr("sqr(-2) ? (1+2) : max(-2,-3)", 3), true);
			PR_CHECK(Expr("-+1", -1), true);
			PR_CHECK(Expr("-++-1", 1), true);
			PR_CHECK(Expr("!!true", 1), true);
			PR_CHECK(Expr("-!!!false", -1), true);
			PR_CHECK(Expr("10 - 3 - 2", 5), true);
			PR_CHECK(Expr("'1' + '2'", '1' + '2'), true);
			#undef EXPR
		}
		{// Compiled expressions
			{ // constants
				auto expr = Compile("2 + x");
				PR_CHECK(expr(3), Val(5));
				PR_CHECK(expr(1.5), Val(3.5));
			}
			{ // add
				auto expr = Compile("x + y");
				PR_CHECK(expr(3, 5), Val(8));
				PR_CHECK(expr({{"y", 5.25}, {"x", -2.5}}), Val(2.75));
			}
			{ // subtract
				auto expr0 = Compile("x - y");
				PR_CHECK(expr0(3, 5), Val(-2));
				PR_CHECK(expr0({{"y", 2.25}, {"x", -2.25}}), Val(-4.5));

				auto expr1 = Compile("x - y - z");
				PR_CHECK(expr1(10, 3, 2), Val(5));
			}
			{ // multiply
				auto expr = Compile("x*x + y");
				PR_CHECK(expr(3, 5), Val(14));
				PR_CHECK(expr({{"y", 2.5}, {"x", -2.5}}), Val(8.75));
			}
			{ // divide
				auto expr = Compile("x / y");
				PR_CHECK(expr(5, 2), Val(2));
				PR_CHECK(expr({{"y", 2.0}, {"x", -5.0}}), Val(-2.5));
			}
			{ // modulus
				auto expr = Compile("x % y");
				PR_CHECK(expr(5, 2), Val(1));
			}
			{ // unary plus
				auto expr = Compile("+x");
				PR_CHECK(expr(5), Val(5));
				PR_CHECK(expr(-5.0), Val(-5.0));
			}
			{ // unary minus
				auto expr0 = Compile("-x");
				PR_CHECK(expr0(5), Val(-5));
				PR_CHECK(expr0(-5.0), Val(+5.0));
			
				auto expr1 = Compile("-+x");
				PR_CHECK(expr1(5), Val(-5));

				auto expr2 = Compile("-++-1");
				PR_CHECK(expr2(), Val(1));
			}
			{ // twos complement
				auto expr = Compile("~x");
				PR_CHECK(expr(5), Val(~5));
			}
			{ // boolean not
				auto expr0 = Compile("!x");
				PR_CHECK(expr0(5), Val(0));
				PR_CHECK(expr0(0), Val(1));

				auto expr1 = Compile("!!true");
				PR_CHECK(expr1(), Val(1));

				auto expr2 = Compile("-!!!false");
				PR_CHECK(expr2(), Val(-1));
			}
			{ // logical OR
				auto expr = Compile("x || y");
				PR_CHECK(expr(0, 0), Val(0));
				PR_CHECK(expr(5, 0), Val(1));
				PR_CHECK(expr(0, 5), Val(1));
				PR_CHECK(expr(3, 5), Val(1));
			}
			{ // logical AND
				auto expr = Compile("x && y");
				PR_CHECK(expr(0, 0), Val(0));
				PR_CHECK(expr(5, 0), Val(0));
				PR_CHECK(expr(0, 5), Val(0));
				PR_CHECK(expr(3, 5), Val(1));
			}
			{ // logical Equal
				auto expr = Compile("x == y");
				PR_CHECK(expr(0, 0), Val(1));
				PR_CHECK(expr(5, 0), Val(0));
				PR_CHECK(expr(5, 5), Val(1));
				PR_CHECK(expr(3.5, 3.5), Val(1));
				PR_CHECK(expr(3.5, 5.3), Val(0));
			}
			{ // logical Not Equal
				auto expr = Compile("x != y");
				PR_CHECK(expr(0, 0), Val(0));
				PR_CHECK(expr(5, 0), Val(1));
				PR_CHECK(expr(5, 5), Val(0));
				PR_CHECK(expr(3.5, 3.5), Val(0));
				PR_CHECK(expr(3.5, 5.3), Val(1));
			}
			{ // logical less than
				auto expr = Compile("x < y");
				PR_CHECK(expr(0, 0), Val(0));
				PR_CHECK(expr(5, 0), Val(0));
				PR_CHECK(expr(3, 5), Val(1));
				PR_CHECK(expr(3.5, 3.5), Val(0));
				PR_CHECK(expr(3.5, 5.3), Val(1));
			}
			{ // logical less than or equal
				auto expr = Compile("x <= y");
				PR_CHECK(expr(0, 0), Val(1));
				PR_CHECK(expr(5, 0), Val(0));
				PR_CHECK(expr(3, 5), Val(1));
				PR_CHECK(expr(3.5, 3.5), Val(1));
				PR_CHECK(expr(3.5, 5.3), Val(1));
			}
			{ // logical greater than
				auto expr = Compile("x > y");
				PR_CHECK(expr(0, 0), Val(0));
				PR_CHECK(expr(5, 0), Val(1));
				PR_CHECK(expr(3, 5), Val(0));
				PR_CHECK(expr(3.5, 3.5), Val(0));
				PR_CHECK(expr(3.5, 5.3), Val(0));
			}
			{ // logical greater than or equal
				auto expr = Compile("x >= y");
				PR_CHECK(expr(0, 0), Val(1));
				PR_CHECK(expr(5, 0), Val(1));
				PR_CHECK(expr(3, 5), Val(0));
				PR_CHECK(expr(3.5, 3.5), Val(1));
				PR_CHECK(expr(3.5, 5.3), Val(0));
			}
			{ // bitwise OR
				auto expr = Compile("x | y");
				PR_CHECK(expr(0x55, 0xAA), Val(0xFF));
				PR_CHECK(expr(0x8000, 1), Val(0x8001));
			}
			{ // bitwise AND
				auto expr = Compile("x & y");
				PR_CHECK(expr(0xFFF0, 0x0FFF), Val(0x0FF0));
			}
			{ // bitwise XOR
				auto expr = Compile("x ^ y");
				PR_CHECK(expr(0xA5, 0x55), Val(0xF0));
			}
			{ // left shift
				auto expr = Compile("x << y");
				PR_CHECK(expr(0x3, 2), Val(0xC));
			}
			{ // right shift
				auto expr = Compile("x >> y");
				PR_CHECK(expr(0xC, 2), Val(0x3));
			}
			{ // fmod
				auto expr = Compile("fmod(x,y)");
				PR_CHECK(FEql(expr(11.3, 3.1).db(), Val(std::fmod(11.3, 3.1)).db()), true);
			}
			{ // ceil
				auto expr = Compile("ceil(x)");
				PR_CHECK(FEql(expr(3.4).db(), Val(std::ceil(3.4)).db()), true);
				PR_CHECK(FEql(expr(-3.4).db(), Val(std::ceil(-3.4)).db()), true);
			}
			{ // floor
				auto expr = Compile("floor(x)");
				PR_CHECK(FEql(expr(3.4).db(), Val(std::floor(3.4)).db()), true);
				PR_CHECK(FEql(expr(-3.4).db(), Val(std::floor(-3.4)).db()), true);
			}
			{ // round
				auto expr = Compile("round(x)");
				PR_CHECK(FEql(expr(+3.5).db(), Val(std::round(+3.5)).db()), true);
				PR_CHECK(FEql(expr(-3.5).db(), Val(std::round(-3.5)).db()), true);
				PR_CHECK(FEql(expr(+3.2).db(), Val(std::round(+3.2)).db()), true);
				PR_CHECK(FEql(expr(-3.2).db(), Val(std::round(-3.2)).db()), true);
			}
			{ // min
				auto expr = Compile("min(x,y)");
				PR_CHECK(FEql(expr(-3.2, -3.4).db(), Val(std::min(-3.2, -3.4)).db()), true);
			}
			{ // max
				auto expr = Compile("max(x,y)");
				PR_CHECK(FEql(expr(-3.2, -3.4).db(), Val(std::max(-3.2, -3.4)).db()), true);
			}
			{ // clamp
				auto expr = Compile("clamp(x,mn,mx)");
				PR_CHECK(FEql(expr(10.0, -3.4, -3.2).db(), Val(pr::Clamp(10.0, -3.4, -3.2)).db()), true);
			}
			{ // asin
				auto expr = Compile("asin(x)");
				PR_CHECK(FEql(expr(-0.8).db(), Val(std::asin(-0.8)).db()), true);
			}
			{ // acos
				auto expr = Compile("acos(x)");
				PR_CHECK(FEql(expr(0.2).db(), Val(std::acos(0.2)).db()), true);
			}
			{ // atan
				auto expr = Compile("atan(x)");
				PR_CHECK(FEql(expr(2.3 / 12.9).db(), Val(std::atan(2.3 / 12.9)).db()), true);
			}
			{ // atan2
				auto expr = Compile("atan2(y,x)");
				PR_CHECK(FEql(expr(2.3, -3.9).db(), Val(std::atan2(2.3, -3.9)).db()), true);
			}
			{ // sinh
				auto expr = Compile("sinh(x)");
				PR_CHECK(FEql(expr(0.8).db(), Val(std::sinh(0.8)).db()), true);
			}
			{ // cosh
				auto expr = Compile("cosh(x)");
				PR_CHECK(FEql(expr(0.2).db(), Val(std::cosh(0.2)).db()), true);
			}
			{ // tanh
				auto expr = Compile("tanh(x)");
				PR_CHECK(FEql(expr(2.3).db(), Val(std::tanh(2.3)).db()), true);
			}
			{ // exp
				auto expr = Compile("exp(x)");
				PR_CHECK(FEql(expr(2.3).db(), Val(std::exp(2.3)).db()), true);
			}
			{ // log
				auto expr = Compile("log(x)");
				PR_CHECK(FEql(expr(209.3).db(), Val(std::log(209.3)).db()), true);
			}
			{ // log10
				auto expr = Compile("log10(x)");
				PR_CHECK(FEql(expr(209.3).db(), Val(std::log10(209.3)).db()), true);
			}
			{ // pow
				auto expr = Compile("pow(x,y)");
				PR_CHECK(FEql(expr(2.3, -1.3).db(), Val(std::pow(2.3, -1.3)).db()), true);
			}
			{ // sqrt
				auto expr = Compile("sqrt(x)");
				PR_CHECK(FEql(expr(2.3).db(), Val(std::sqrt(2.3)).db()), true);
			}
			{ // sqr
				auto expr = Compile("sqr(x)");
				PR_CHECK(FEql(expr(-2.3).db(), Val(pr::Sqr(-2.3)).db()), true);
			}
			{ // len2
				auto expr = Compile("len2(x,y)");
				PR_CHECK(FEql(expr(3, 4).db(), Val(std::sqrt(3.0 * 3.0 + 4.0 * 4.0)).db()), true);
			}
			{ // len3
				auto expr = Compile("len3(x,y,z)");
				PR_CHECK(FEql(expr(3, 4, 5).db(), Val(std::sqrt(3.0 * 3.0 + 4.0 * 4.0 + 5.0 * 5.0)).db()), true);
			}
			{ // len4
				auto expr = Compile("len4(x,y,z,w)");
				PR_CHECK(FEql(expr(3, 4, 5, 6).db(), Val(std::sqrt(3.0 * 3.0 + 4.0 * 4.0 + 5.0 * 5.0 + 6.0 * 6.0)).db()), true);
			}
			{ // deg
				auto expr = Compile("deg(x)");
				PR_CHECK(FEql(expr(-1.24).db(), Val(-1.24 * (360.0 / TAU)).db()), true);
			}
			{ // rad
				auto expr = Compile("rad(x)");
				PR_CHECK(FEql(expr(241.32).db(), Val(241.32 * (TAU / 360.0)).db()), true);
			}
			{ // hash
				auto expr = Compile("hash(\"A String\")");
				PR_CHECK(expr(), Val(hash::HashCT("A String")));
			}
			{ // long expression, no variables
				auto expr = Compile("sqr(sqrt(2.3)*-abs(4%2)/15.0-tan(TAU/-6))");
				PR_CHECK(FEql(expr().db(), Val(pr::Sqr(std::sqrt(2.3) * -std::abs(4 % 2) / 15.0 - std::tan(eval::TAU / -6))).db()), true);
			}
			{ // long expression, with variables
				auto expr = Compile("sqr(sqrt(x)*-abs(y%3)/x-tan(TAU/-y))");
				PR_CHECK(FEql(expr(2.3, 13).db(), Val(pr::Sqr(std::sqrt(2.3) * -std::abs(13 % 3) / 2.3 - std::tan(eval::TAU / -13))).db()), true);
			}
			{ // large values
				auto expr = Compile("123456789000000 / 2");
				PR_CHECK(expr().ll(), 123456789000000LL / 2);
			}
			{ // operator ?:
				auto expr0 = Compile("x != y ? 5 : 6");
				PR_CHECK(expr0(1, 2).ll(), Val(5));
				PR_CHECK(expr0(1, 1).ll(), Val(6));

				auto expr1 = Compile("true ? x : 6 + 1");
				PR_CHECK(expr1(1).ll(), Val(1));

				auto expr2 = Compile("false ? x : 6 + 1");
				PR_CHECK(expr2(1).ll(), Val(7));
			}
			{ // Parenthesis
				auto expr = Compile("sqr(-2) ? (1+2) : max(-2,-3)");
				PR_CHECK(expr(), Val(3));
			}
			{ // literals
				auto expr = Compile("'1' + '2'");
				PR_CHECK(expr(), Val('1' + '2'));
			}
		}
	}
}
#endif
