package nz.co.rylogic.allkeys

import android.app.Application

class AllKeysApp : Application()
{
	lateinit var settings: Settings
	override fun onCreate()
	{
		super.onCreate()
		settings = Settings(this)
	}
}