package nz.co.rylogic.allkeys

import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import androidx.navigation.findNavController
import android.view.MenuItem
import android.view.View
import androidx.appcompat.app.AppCompatDelegate
import androidx.navigation.NavController
import androidx.navigation.fragment.NavHostFragment
import nz.co.rylogic.allkeys.databinding.ActivityMainBinding

class MainActivity : AppCompatActivity()
{
	private lateinit var mNavController: NavController
	private lateinit var mBinding: ActivityMainBinding

	override fun onCreate(savedInstanceState: Bundle?)
	{
		setAppTheme()
		super.onCreate(savedInstanceState)

		// Get the binding data from the main activity XML
		mBinding = ActivityMainBinding.inflate(layoutInflater)
		setContentView(mBinding.root)

		// Setup the navigation
		val navHostFragment = supportFragmentManager.findFragmentById(R.id.nav_host_fragment_content_main) as NavHostFragment
		mNavController = navHostFragment.navController
		mNavController.addOnDestinationChangedListener { _, destination, _ ->
			mBinding.fabMenu.visibility = if (destination.id == R.id.FragmentMain) View.VISIBLE else View.GONE
		}

		mBinding.fabMenu.setOnClickListener {
			// Navigate to the settings fragment
			findNavController(R.id.nav_host_fragment_content_main).navigate(R.id.action_Main_to_Settings)
		}
	}

	override fun onOptionsItemSelected(item: MenuItem): Boolean
	{
		// Handle action bar item clicks here. The action bar will
		// automatically handle clicks on the Home/Up button, so long
		// as you specify a parent activity in AndroidManifest.xml.
		return when (item.itemId)
		{
			R.id.action_settings -> {
				findNavController(R.id.nav_host_fragment_content_main).navigate(R.id.action_Main_to_Settings)
				return true
			}
			else -> super.onOptionsItemSelected(item)
		}
	}

	override fun onSupportNavigateUp(): Boolean
	{
		return mNavController.navigateUp() || super.onSupportNavigateUp()
	}

	private fun setAppTheme()
	{
		val settings = Settings(this)
		when  (settings.theme) {
			"Light" -> AppCompatDelegate.setDefaultNightMode(AppCompatDelegate.MODE_NIGHT_NO)
			"Dark" -> AppCompatDelegate.setDefaultNightMode(AppCompatDelegate.MODE_NIGHT_YES)
			else -> AppCompatDelegate.setDefaultNightMode(AppCompatDelegate.MODE_NIGHT_FOLLOW_SYSTEM)
		}
	}
}