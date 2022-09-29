package nz.co.rylogic.allkeys

import android.content.Context
import android.content.SharedPreferences
import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.SeekBar
import androidx.fragment.app.Fragment
import androidx.preference.PreferenceManager
import nz.co.rylogic.allkeys.databinding.FragmentMainBinding
import java.beans.PropertyChangeEvent
import java.beans.PropertyChangeListener
import java.util.*

// A simple [Fragment] subclass as the default destination in the navigation.
class FragmentMain : Fragment(), PropertyChangeListener
{
	// This property is only valid between onCreateView and  onDestroyView.
	private val binding get() = mBinding!!
	private var mBinding: FragmentMainBinding? = null

	// The thing that changes the chord
	private val mUpdater :Updater = Updater(Handler(Looper.getMainLooper()))

	override fun onCreateView(inflater: LayoutInflater, container: ViewGroup?, savedInstanceState: Bundle?): View
	{
		mBinding = FragmentMainBinding.inflate(inflater, container, false)
		mUpdater.addPropertyChangeListener(this)
		return binding.root
	}

	override fun onDestroyView()
	{
		super.onDestroyView()
		mBinding = null
	}

	override fun onViewCreated(view: View, savedInstanceState: Bundle?)
	{
		super.onViewCreated(view, savedInstanceState)
		val sp: SharedPreferences = PreferenceManager.getDefaultSharedPreferences(context as Context)

		fun period(): Double {
			val count = (binding.sliderSpeed.max - binding.sliderSpeed.progress) + 1 // [20,1]
			return count / 3.0 // [10,0.5]
		}
		binding.sliderSpeed.progress = binding.sliderSpeed.max / 2
		binding.sliderSpeed.setOnSeekBarChangeListener(object : SeekBar.OnSeekBarChangeListener {
			override fun onProgressChanged(sb:SeekBar, progress:Int, from:Boolean) {
				mUpdater.running = false
				mUpdater.period = period()
			}
			override fun onStartTrackingTouch(p0: SeekBar?) {}
			override fun onStopTrackingTouch(p0: SeekBar?) {}
		})
		binding.buttonNext.setOnClickListener{
			mUpdater.next()
		}
		binding.buttonStart.setOnClickListener {
			if (mUpdater.running)
			{
				mUpdater.running = false
			}
			else
			{
				mUpdater.reset(sp, period())
				mUpdater.running = true
			}
		}

		mUpdater.reset(sp, period())
	}

	override fun onPause()
	{
		mUpdater.running = false
		super.onPause()
	}

	override fun propertyChange(p0: PropertyChangeEvent?)
	{
		binding.textChord.text = "${mUpdater.key} ${mUpdater.chord}"

		// Update the button text
		if (mUpdater.running)
			binding.buttonStart.setText(R.string.stop)
		else
			binding.buttonStart.setText(R.string.start)
	}
}
