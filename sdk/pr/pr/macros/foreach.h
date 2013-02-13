#ifndef SHARED_FOREACH_H
#define SHARED_FOREACH_H

namespace foreach_private {
	template <typename Container>         struct iterator                  { typedef typename Container::iterator       type; };
	template <typename Container>         struct iterator<const Container> { typedef typename Container::const_iterator type; };
	template <typename T, unsigned int N> struct iterator<      T[N]>      { typedef       T*                           type; };
	template <typename T, unsigned int N> struct iterator<const T[N]>      { typedef const T*                           type; };

	template <typename Container>         struct index_type             { typedef typename Container::size_type type; };
	template <typename T, unsigned int N> struct index_type<      T[N]> { typedef unsigned int                  type; };
	template <typename T, unsigned int N> struct index_type<const T[N]> { typedef unsigned int                  type; };

	template <typename T>       T& bind(      T&);
	template <typename T> const T& bind(const T&);

	template <typename T> typename index_type<T>::type indexer(      T&);
	template <typename T> typename index_type<T>::type indexer(const T&);

	template <typename Container>
	inline typename iterator<Container>::type begin(Container& c) {
		return c.begin();
	}

	template <typename Container>
	inline typename iterator<const Container>::type begin(const Container& c) {
		return c.begin();
	}

	template <typename T, unsigned int N>
	inline typename iterator<T[N]>::type begin(T(&c)[N]) {
		return c;
	}

	template <typename Container>
	inline typename iterator<Container>::type end(Container& c) {
		return c.end();
	}

	template <typename Container>
	inline typename iterator<const Container>::type end(const Container& c) {
		return c.end();
	}

	template <typename T, unsigned int N>
	inline typename iterator<T[N]>::type end(T(&c)[N]) {
		return c + N;
	}
}

#ifdef __GNUC__

	#define foreach(var, container) \
		if (bool __once = false) {} else \
			for (typeof(foreach_private::bind(container))& __container = container; !__once;) \
				for (typeof(foreach_private::begin(__container)) __begin = foreach_private::begin(__container), __end = foreach_private::end(__container); !__once;) \
					if ((__once = true) == false) {} else \
						if (bool __break = false) {} else \
							for (; !__break && __begin != __end; ++__begin) \
								if ((__break = true) == false) {} else \
									for (typeof(*__begin) var = *__begin; __break; __break = false)

	#define foreach_iter(var, iter, container) \
		if (bool __once = false) {} else \
			for (typeof(foreach_private::bind(container))& __container = container; !__once;) \
				for (typeof(foreach_private::begin(__container)) iter = foreach_private::begin(__container), __end = foreach_private::end(__container); !__once;) \
					if ((__once = true) == false) {} else \
						if (bool __break = false) {} else \
							for (; !__break && iter != __end; ++iter) \
								if ((__break = true) == false) {} else \
									for (typeof(*iter) var = *iter; __break; __break = false)

	#define foreach_index(var, index, container) \
		if (bool __once = false) {} else \
			for (typeof(foreach_private::bind(container))& __container = container; !__once;) \
				for (typeof(foreach_private::begin(__container)) __begin = foreach_private::begin(__container), __end = foreach_private::end(__container); !__once;) \
					if ((__once = true) == false) {} else \
						if (bool __break = false) {} else \
							if (typeof(foreach_private::indexer(container)) index = static_cast<typeof(foreach_private::indexer(container))>(0)) {} else \
								for (; !__break && __begin != __end; ++__begin, index = static_cast<typeof(foreach_private::indexer(container))>(index + 1)) \
									if ((__break = true) == false) {} else \
										for (typeof(*__begin) var = *__begin; __break; __break = false)

	#define foreach_iter_index(var, iter, index, container) \
		if (bool __once = false) {} else \
			for (typeof(foreach_private::bind(container))& __container = container; !__once;) \
				for (typeof(foreach_private::begin(__container)) iter = foreach_private::begin(__container), __end = foreach_private::end(__container); !__once;) \
					if ((__once = true) == false) {} else \
						if (bool __break = false) {} else \
							if (typeof(foreach_private::indexer(container)) index = static_cast<typeof(foreach_private::indexer(container))>(0)) {} else \
								for (; !__break && iter != __end; ++iter, index = static_cast<typeof(foreach_private::indexer(container))>(index + 1)) \
									if ((__break = true) == false) {} else \
										for (typeof(*iter) var = *iter; __break; __break = false)

#else

	#define foreach(var, container) \
		if (bool __once = false) {} else \
			for (decltype(foreach_private::bind(container))& __container = container; !__once;) \
				for (auto __begin = foreach_private::begin(__container), __end = foreach_private::end(__container); !__once;) \
					if ((__once = true) == false) {} else \
						if (bool __break = false) {} else \
							for (; !__break && __begin != __end; ++__begin) \
								if ((__break = true) == false) {} else \
									for (auto var = *__begin; __break; __break = false)

	#define foreach_iter(var, iter, container) \
		if (bool __once = false) {} else \
			for (decltype(foreach_private::bind(container))& __container = container; !__once;) \
				for (auto iter = foreach_private::begin(__container), __end = foreach_private::end(__container); !__once;) \
					if ((__once = true) == false) {} else \
						if (bool __break = false) {} else \
							for (; !__break && iter != __end; ++iter) \
								if ((__break = true) == false) {} else \
									for (auto var = *iter; __break; __break = false)

	#define foreach_index(var, index, container) \
		if (bool __once = false) {} else \
			for (decltype(foreach_private::bind(container))& __container = container; !__once;) \
				for (auto __begin = foreach_private::begin(__container), __end = foreach_private::end(__container); !__once;) \
					if ((__once = true) == false) {} else \
						if (bool __break = false) {} else \
							if (decltype(foreach_private::indexer(container)) index = static_cast<decltype(foreach_private::indexer(container))>(0)) {} else \
								for (; !__break && __begin != __end; ++__begin, index = static_cast<decltype(foreach_private::indexer(container))>(index + 1)) \
									if ((__break = true) == false) {} else \
										for (auto var = *__begin; __break; __break = false)

	#define foreach_iter_index(var, iter, index, container) \
		if (bool __once = false) {} else \
			for (decltype(foreach_private::bind(container))& __container = container; !__once;) \
				for (auto iter = foreach_private::begin(__container), __end = foreach_private::end(__container); !__once;) \
					if ((__once = true) == false) {} else \
						if (bool __break = false) {} else \
							if (decltype(foreach_private::indexer(container)) index = static_cast<decltype(foreach_private::indexer(container))>(0)) {} else \
								for (; !__break && iter != __end; ++iter, index = static_cast<decltype(foreach_private::indexer(container))>(index + 1)) \
									if ((__break = true) == false) {} else \
										for (auto var = *iter; __break; __break = false)

#endif

#define foreach_enum(enum_namespace, val) for (enum_namespace::type val = (enum_namespace::type)0; val != enum_namespace::count; val = (enum_namespace::type)(val + 1))

#endif
