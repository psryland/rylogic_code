//**********************************
// Script charactor source stack
//  Copyright (c) Rylogic Ltd 2007
//**********************************

#pragma once
#ifndef PR_SCRIPT_STREAM_STACK_H
#define PR_SCRIPT_STREAM_STACK_H

#include "pr/script/script_core.h"
#include "pr/script/char_stream.h"

namespace pr
{
	namespace script
	{
		// A stack of character stream sources
		struct SrcStack :Src
		{
		private:
			struct Item
			{
				Src* m_src;     // Pointer to the character source
				bool m_cleanup; // True if the source should be deleted when popped off the stack
				Item(Src* src, bool cleanup) :m_src(src) ,m_cleanup(cleanup) {}
				void cleanup() { if (m_cleanup) delete m_src; }
			};

			typedef pr::Array<Item, 16> Stack; // The stack of char sources
			Stack m_stack;

		public:
			SrcStack()
				:Src()
				,m_stack()
			{}

			// Construct the stack with a char source
			explicit SrcStack(Src& src)
				:Src()
				,m_stack()
			{
				push(src);
			}

			// Construct the stack with a char source that needs deleting when popped from the stack
			explicit SrcStack(Src* src, bool delete_on_pop)
				:Src()
				,m_stack()
			{
				push(src, delete_on_pop);
			}

			virtual ~SrcStack()
			{
				while (!m_stack.empty())
					pop();
			}

			// Push a stream onto the stack
			void push(Src& src)
			{
				push(&src, false);
			}
			void push(Src* src, bool delete_on_pop)
			{
				m_stack.push_back(Item(src, delete_on_pop));
			}

			// Pop the top stream off the stack
			void pop()
			{
				if (m_stack.empty()) return;
				m_stack.back().cleanup();
				m_stack.pop_back();
			}

			// Return the number of sources on the stack
			size_t size() const
			{
				return m_stack.size();
			}

			ESrcType type() const override { return m_stack.empty() ? ESrcType::Unknown : m_stack.back().m_src->type(); }
			Loc  loc() const override      { return m_stack.empty() ? Loc() : m_stack.back().m_src->loc(); }
			void loc(Loc& l) override      { if (!m_stack.empty()) m_stack.back().m_src->loc(l); }

		protected:
			char peek() const override { return m_stack.empty() ? 0 : **m_stack.back().m_src; }
			void next() override       { ++(*m_stack.back().m_src); }
			void seek() override       { for (; !m_stack.empty() && **m_stack.back().m_src == 0; pop()) {} }
		};
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_script_stream_stack)
		{
			using namespace pr;
			using namespace pr::script;

			{//SrcStack
				char const* str1 = "one";
				char const* str2 = "two";
				PtrSrc src1(str1);
				PtrSrc src2(str2);
				SrcStack stack(src1);

				for (int i = 0; i != 2; ++i, ++stack)
					PR_CHECK(*stack, str1[i]);

				stack.push(src2);

				for (int i = 0; i != 3; ++i, ++stack)
					PR_CHECK(*stack, str2[i]);

				for (int i = 2; i != 3; ++i, ++stack)
					PR_CHECK(*stack, str1[i]);

				PR_CHECK(*stack, 0);
			}
		}
	}
}
#endif

#endif
