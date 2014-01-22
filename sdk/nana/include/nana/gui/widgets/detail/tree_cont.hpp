/*
 *	A Tree Container class implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0.
 *	(See accompanying file LICENSE_1_0.txt or copy at
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/gui/widgets/detail/tree_cont.hpp
 */

#ifndef NANA_GUI_WIDGETS_DETAIL_TREE_CONT_HPP
#define NANA_GUI_WIDGETS_DETAIL_TREE_CONT_HPP
#include <stack>

namespace nana
{
namespace gui
{
namespace widgets
{
namespace detail
{
		template<typename T>
		struct tree_node
		{
			typedef std::pair<nana::string, T>	value_type;

			value_type	value;

			tree_node	*owner;
			tree_node	*next;
			tree_node	*child;

			tree_node(tree_node* owner)
				:owner(owner), next(0), child(0)
			{}

			~tree_node()
			{
				if(owner)
				{
					tree_node * t = owner->child;
					if(t != this)
					{
						while(t->next != this)
							t = t->next;
						t->next = next;
					}
					else
						owner->child = next;
				}

				tree_node * t = child;
				while(t)
				{
					tree_node * t_next = t->next;
					delete t;
					t = t_next;
				}
			}
		};

		template<typename UserData>
		class tree_cont
		{
			typedef tree_cont self_type;

		public:
			typedef UserData	element_type;
			typedef tree_node<element_type> node_type;
			typedef typename node_type::value_type	value_type;
			tree_cont()
				:root_(0)
			{}

			~tree_cont()
			{
				clear();
			}

			void clear()
			{
				remove(root_.child);
			}

			bool verify(const node_type* node) const
			{
				if(node)
				{
					while(node->owner)
					{
						if(node->owner == &root_)
							return true;

						node = node->owner;
					}
				}
				return false;
			}

			node_type* get_root() const
			{
				return &root_;
			}

			node_type* get_owner(const node_type* node) const
			{
				if(verify(node))
					return (node->owner == &root_ ? 0 : node->owner);
				return 0;
			}

			node_type * node(node_type* node, const nana::string& key)
			{
				if(node)
				{
					for(node_type * child = node->child; child; child = child->next)
					{
						if(child->value.first == key)
							return child;
					}
				}
				return 0;
			}

			node_type* insert(node_type* node, const nana::string& key, const element_type& elem)
			{
				if(0 == node)
				{
					return insert(key, elem);
				}
				else if(verify(node))
				{
					node_type *newnode = 0;
					if(node->child)
					{
						node_type* child = node->child;
						for(; child; child = child->next)
						{
							if(child->value.first == key)
							{
								child->value.second = elem;
								return child;
							}
						}

						child = node->child;
						while(child->next)
							child = child->next;

						newnode = child->next = new node_type(node);
					}
					else
						newnode = node->child = new node_type(node);

					newnode->value.first = key;
					newnode->value.second = elem;
					return newnode;
				}
				return 0;
			}

			node_type* insert(const nana::string& key, const element_type& elem)
			{
				node_type * node = _m_locate<true>(key);
				if(node)
					node->value.second = elem;
				return node;
			}

			void remove(node_type* node)
			{
				if(verify(node))
					delete node;
			}

			node_type* find(const nana::string& path) const
			{
				return _m_locate(path);
			}

			node_type* ref(const nana::string& path)
			{
				return _m_locate<true>(path);
			}

			unsigned indent_size(const node_type* node) const
			{
				if(node)
				{
					unsigned indent = 0;
					for(;(node = node->owner); ++indent)
					{
						if(node == &root_)	return indent;
					}
				}
				return 0;
			}

			template<typename Functor>
			void for_each(node_type* node, Functor f)
			{
				if(0 == node) node = root_.child;
				int state = 0;	//0: Sibling, the last is a sibling of node
								//1: Owner, the last is the owner of node
								//>= 2: Children, the last is is a child of the node that before this node.
				while(node)
				{
					switch(f(*node, state))
					{
					case 0: return;
					case 1:
						{
							if(node->child)
							{
								node = node->child;
								state = 1;
							}
							else
								return;
							continue;
						}
						break;
					}

					if(node->next)
					{
						node = node->next;
						state = 0;
					}
					else
					{
						state = 1;
						if(node == &root_)	return;

						while(true)
						{
							++state;
							if(node->owner->next)
							{
								node = node->owner->next;
								break;
							}
							else
								node = node->owner;

							if(node == &root_)	return;
						}
					}
				}
			}

			template<typename Functor>
			void for_each(node_type* node, Functor f) const
			{
				if(0 == node) node = root_.child;
				int state = 0;	//0: Sibling, the last is a sibling of node
								//1: Owner, the last is the owner of node
								//>= 2: Children, the last is is a child of the node that before this node.
				while(node)
				{
					switch(f(*node, state))
					{
					case 0: return;
					case 1:
						{
							if(node->child)
							{
								node = node->child;
								state = 1;
							}
							else
								return;
							continue;
						}
						break;
					}

					if(node->next)
					{
						node = node->next;
						state = 0;
					}
					else
					{
						state = 1;
						if(node == &root_)	return;

						while(true)
						{
							++state;
							if(node->owner->next)
							{
								node = node->owner->next;
								break;
							}
							else
								node = node->owner;

							if(node == &root_)	return;
						}
					}
				}
			}

			template<typename PredAllowChild>
			unsigned child_size_if(const nana::string& key, PredAllowChild pac) const
			{
				const node_type * node = _m_locate(key);
				return (node ? child_size_if<PredAllowChild>(*node, pac) : 0);
			}

			template<typename PredAllowChild>
			unsigned child_size_if(const node_type& node, PredAllowChild pac) const
			{
				unsigned size = 0;

				const node_type* pnode = node.child;
				while(pnode)
				{
					++size;
					if(pnode->child && pac(*pnode))
						size += child_size_if<PredAllowChild>(*pnode, pac);

					pnode = pnode->next;
				}
				return size;
			}

			template<typename PredAllowChild>
			unsigned distance_if(const node_type * node, PredAllowChild pac) const
			{
				if(node == 0)	return 0;
				const node_type * iterator = root_.child;

				unsigned off = 0;
				std::stack<const node_type* > stack;

				while(iterator && iterator != node)
				{
					++off;

					if(iterator->child && pac(*iterator))
					{
						stack.push(iterator);
						iterator = iterator->child;
					}
					else
						iterator = iterator->next;

					while(0 == iterator && stack.size())
					{
						iterator = stack.top()->next;
						stack.pop();
					}
				}

				return off;
			}

			template<typename PredAllowChild>
			node_type* advance_if(node_type* node, std::size_t off, PredAllowChild pac)
			{
				if(node == 0)	node = root_.child;

				std::stack<node_type* > stack;

				while(node && off)
				{
					--off;

					if(node->child && pac(*node))
					{
						stack.push(node);
						node = node->child;
					}
					else
						node = node->next;

					while(0 == node && stack.size())
					{
						node = stack.top();
						stack.pop();
						node = node->next;
					}
				}

				return node;
			}
		private:
			//Functor defintions

			struct each_make_node
			{
				each_make_node(self_type& self)
					:node(&(self.root_))
				{}

				bool operator()(const nana::string& key_node)
				{
					node_type *child = node->child;
					node_type *tail = 0;
					while(child)
					{
						if(key_node == child->value.first)
							break;
						tail = child;
						child = child->next;
					}

					if(child == 0)
					{
						child = new node_type(node);
						if(tail)
							tail->next = child;
						else
							node->child = child;

						child->value.first = key_node;
					}

					node = child;
					return true;
				}

				node_type * node;
			};


			struct find_key_node
			{
				find_key_node(const self_type& self)
					:node(&self.root_)
				{}

				bool operator()(const nana::string& key_node)
				{
					return ((node = _m_find(node->child, key_node)) != 0);
				}

				node_type *node;
			};
		private:
			static node_type* _m_find(node_type* node, const nana::string& key_node)
			{
				while(node)
				{
					if(key_node == node->value.first)
						return node;

					node = node->next;
				}
				return 0;
			}

			template<typename Function>
			void _m_for_each(const nana::string& key, Function function) const
			{
				if(key.size())
				{
					nana::string::size_type beg = 0;
					nana::string::size_type end = key.find_first_of(STR("\\/"));

					while(end != nana::string::npos)
					{
						if(beg != end)
						{
							if(function(key.substr(beg, end - beg)) == false)
								return;
						}

						nana::string::size_type next = key.find_first_not_of(STR("\\/"), end);
						if(next != nana::string::npos)
						{
							beg = next;
							end = key.find_first_of(STR("\\/"), beg);
						}
						else
							return;
					}

					function(key.substr(beg, key.size() - beg));
				}
			}

			template<bool CreateIfNotExists>
			node_type* _m_locate(const nana::string& key)
			{
				if(key.size())
				{
					if(CreateIfNotExists)
					{
						each_make_node emn(*this);
						_m_for_each<each_make_node&>(key, emn);
						return emn.node;
					}
					else
					{
						find_key_node fkn(*this);
						_m_for_each<find_key_node&>(key, fkn);
						return const_cast<node_type*>(fkn.node);
					}
				}
				return &root_;
			}

			node_type* _m_locate(const nana::string& key) const
			{
				if(key.size())
				{
					find_key_node fkn(*this);
					_m_for_each<find_key_node&>(key, fkn);
					return fkn.node;
				}
				return &root_;
			}
		private:
			mutable node_type root_;
		};//end class tree_cont
}//end namespace detail
}//end namespace widgets
}//end namespace gui
}//end namesace nana
#endif
