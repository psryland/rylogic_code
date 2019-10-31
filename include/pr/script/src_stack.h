//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************

#pragma once

#include "pr/script/forward.h"
#include "pr/script/location.h"
#include "pr/script/fail_policy.h"
#include "pr/script/script_core.h"

namespace pr::script
{
	struct SrcStack
	{
		// Notes:
		//  - SrcStack is not a 'Src' subclass because the stack must not buffer.
		//  - ReadAhead is not defined because that gives the impression this class
		//    is buffering, which it isn't.

	private:

		// The stack of 'Src' instances
		using stack_t = pr::vector<Src*>;
		stack_t m_stack;

		// A null source for when the stack is empty
		NullSrc m_null;

	public:

		SrcStack()
			:m_stack()
			,m_null()
		{}
		explicit SrcStack(Src& src)
			:SrcStack()
		{
			Push(src);
		}
		SrcStack(SrcStack const&) = delete;
		~SrcStack()
		{
			for (; !Empty();)
				Pop();
		}

		// The 'position' within the top source
		Loc const& Location()
		{
			return Top().Location();
		}

		// True if there are no sources on the stack
		bool Empty() const noexcept
		{
			return m_stack.empty();
		}

		// The top source on the stack
		Src const& Top() const noexcept
		{
			return !m_stack.empty() ? *m_stack.back() : static_cast<Src const&>(m_null);
		}
		Src& Top() noexcept
		{
			return !m_stack.empty() ? *m_stack.back() : static_cast<Src&>(m_null);
		}

		// Push a script source onto the stack
		void Push(Src& src)
		{
			m_stack.push_back(&src);
		}

		// Pop a source from the stack
		void Pop()
		{
			if (m_stack.empty())
				throw std::runtime_error("Source stack is empty");

			// Remove the top source from the stack
			m_stack.pop_back();
		}

		// Peek
		char_t operator *()
		{
			// Careful, make sure Peek is readonly. Don't want the debugger to change it's state when viewed in watch windows.
			auto ch = *Top();
			if (ch == '\0' && !m_stack.empty()) throw std::runtime_error("Stack.Pop has been missed");
			return ch;
		}

		// Advance by 1 character
		SrcStack& operator ++()
		{
			Next();
			return *this;
		}

		// Advance by n characters
		SrcStack& operator += (int n)
		{
			Next(n);
			return *this;
		}

		// Increment to the next character
		void Next(int n = 1)
		{
			for (; n > 0; --n)
			{
				Top().Next();
				for (; !m_stack.empty() && *Top() == '\0';)
					Pop();
			}
		}
	};
}


#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::script
{
	PRUnitTest(SrcStackTests)
	{
		char const str1[] = "one";
		char const str2[] = "two";
		StringSrc src1(str1);
		StringSrc src2(str2);
		SrcStack stack(src1);

		for (int i = 0; i != 2; ++i, ++stack)
			PR_CHECK(*stack, str1[i]);

		stack.Push(src2);

		for (int i = 0; i != 3; ++i, ++stack)
			PR_CHECK(*stack, str2[i]);

		for (int i = 2; i != 3; ++i, ++stack)
			PR_CHECK(*stack, str1[i]);

		PR_CHECK(*stack, '\0');
	}
}
#endif
