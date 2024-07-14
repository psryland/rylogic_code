package nz.co.rylogic.allkeys

class MulticastDelegate<T>
{
	private val mDelegates = mutableListOf<(T) -> Unit>()

	fun add(delegate: (T) -> Unit)
	{
		mDelegates.add(delegate)
	}

	fun remove(delegate: (T) -> Unit)
	{
		mDelegates.remove(delegate)
	}

	fun invoke(data: T)
	{
		for (delegate in mDelegates)
			delegate(data)
	}
}