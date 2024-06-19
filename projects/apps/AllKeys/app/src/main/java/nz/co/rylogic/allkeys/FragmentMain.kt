package nz.co.rylogic.allkeys
//TODO
//  Fix colors in menu

import android.content.Context
import android.graphics.drawable.ColorDrawable
import android.media.SoundPool
import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.GridLayout
import android.widget.SeekBar
import androidx.core.content.ContextCompat
import androidx.fragment.app.Fragment
import nz.co.rylogic.allkeys.databinding.FragmentMainBinding
import java.beans.PropertyChangeEvent
import java.beans.PropertyChangeListener

// A simple [Fragment] subclass as the default destination in the navigation.
class FragmentMain : Fragment(), PropertyChangeListener
{
	// Colours for the beat indicators
	private lateinit var mBeat0: ColorDrawable
	private lateinit var mBeat1: ColorDrawable

	// This property is only valid between onCreateView and  onDestroyView.
	private lateinit var mBinding: FragmentMainBinding

	// References to the beat indicators
	private var mBeats: List<GridLayout> = listOf()

	// App settings
	private lateinit var mSettings: Settings

	// The thing that changes the chord
	private lateinit var mUpdater: Updater

	// For playing root note sounds
	private lateinit var mSoundPoolNotes: SoundPool
	private var mSoundRootNotes: Map<String, List<Int>> = mapOf()

	// For playing metronome sounds
	private lateinit var mSoundPoolClicks: SoundPool
	private var mSoundClicks: Map<String, Int> = mapOf()

	// The sound of the root note to play
	private var mSoundRootNote: Int = 0

	override fun onCreateView(inflater: LayoutInflater, container: ViewGroup?, savedInstanceState: Bundle?): View
	{
		mSettings = Settings(context as Context)
		mUpdater = Updater(context as Context, Handler(Looper.getMainLooper()))

		mSoundPoolNotes = SoundPool.Builder().setMaxStreams(1).build()
		mSoundPoolClicks = SoundPool.Builder().setMaxStreams(1).build()

		loadRootNotes()
		loadClicks()

		mBinding = FragmentMainBinding.inflate(inflater, container, false)
		mBeat0 = ColorDrawable(ContextCompat.getColor(requireContext(), R.color.metronome_beat0))
		mBeat1 = ColorDrawable(ContextCompat.getColor(requireContext(), R.color.metronome_beat1))

		val tempoLayout = mBinding.tempoLayout
		mBeats = listOf(tempoLayout.beat0, tempoLayout.beat1, tempoLayout.beat2, tempoLayout.beat3, tempoLayout.beat4, tempoLayout.beat5, tempoLayout.beat6)
		mUpdater.addPropertyChangeListener(this)
		return mBinding.root
	}

	override fun onDestroyView()
	{
		super.onDestroyView()
		mSoundPoolNotes.release()
		mSoundPoolClicks.release()
	}

	override fun onViewCreated(view: View, savedInstanceState: Bundle?)
	{
		super.onViewCreated(view, savedInstanceState)

		// Update the tempo slider
		val tempoLayout = mBinding.tempoLayout
		tempoLayout.labelTempo.text = getString(R.string.tempo_bpm, mSettings.tempo)
		tempoLayout.sliderSpeed.progress = mSettings.tempo
		tempoLayout.sliderSpeed.setOnSeekBarChangeListener(object : SeekBar.OnSeekBarChangeListener
		{
			override fun onProgressChanged(sb: SeekBar, progress: Int, from: Boolean)
			{
				mSettings.tempo = progress
				tempoLayout.labelTempo.text = getString(R.string.tempo_bpm, mSettings.tempo)
			}

			override fun onStartTrackingTouch(p0: SeekBar?)
			{
			}

			override fun onStopTrackingTouch(p0: SeekBar?)
			{
			}
		})

		// Update the beat indicators
		for (i in mBeats.indices)
		{
			mBeats[i].background = if (i == 0) mBeat1 else mBeat0
			mBeats[i].visibility = if (i < mSettings.beatsPerBar) View.VISIBLE else View.GONE
		}

		// Buttons
		val buttonLayout = mBinding.buttonLayout
		buttonLayout.buttonNext.setOnClickListener {
			mUpdater.runMode = ERunMode.StepOne
		}
		buttonLayout.buttonStart.setOnClickListener {
			if (mUpdater.runMode == ERunMode.Stopped)
				mUpdater.runMode = ERunMode.Continuous
			else
				mUpdater.runMode = ERunMode.Stopped
		}

		// Initialise the updater
		mUpdater.reset(mSettings)
	}

	override fun onResume()
	{
		super.onResume()
		mUpdater.reset(mSettings)
	}
	override fun onPause()
	{
		mUpdater.runMode = ERunMode.Stopped
		mSoundPoolNotes.autoPause()
		mSoundPoolClicks.autoPause()
		super.onPause()
	}

	override fun propertyChange(prop: PropertyChangeEvent?)
	{
		val chordLayout = mBinding.chordLayout
		val buttonLayout = mBinding.buttonLayout

		// Start/Stop running
		if (prop?.propertyName == Updater.RUN_MODE)
		{
			// Update the button text
			if (mUpdater.runMode == ERunMode.Continuous)
				buttonLayout.buttonStart.setText(R.string.stop)
			else
				buttonLayout.buttonStart.setText(R.string.start)

			// Stop sounds
			val prevRunMode = prop.oldValue as ERunMode
			if (prevRunMode == ERunMode.Continuous && mUpdater.runMode == ERunMode.Stopped)
			{
				mSoundPoolNotes.autoPause()
				mSoundPoolClicks.autoPause()
			}
		}

		// Chord change
		if (prop?.propertyName == Updater.NOTIFY_CHORD)
		{
			// Update the chord text
			chordLayout.textChord.text = getString(R.string.key_and_chord, mUpdater.key, mUpdater.chord)
			chordLayout.textChordNext.text = getString(R.string.key_and_chord, mUpdater.keyNext, mUpdater.chordNext)

			// Update the root note in the media player
			val instrument = mSoundRootNotes[mSettings.rootNoteInstrument]
			if (mSettings.rootNoteSounds && instrument != null)
			{
				mSoundRootNote = when (mUpdater.key)
				{
					"G" -> instrument[0]
					"G♯" -> instrument[1]
					"A♭" -> instrument[1]
					"A" -> instrument[2]
					"A♯" -> instrument[3]
					"B♭" -> instrument[3]
					"B" -> instrument[4]
					"B♯" -> instrument[5]
					"C♭" -> instrument[4]
					"C" -> instrument[5]
					"C♯" -> instrument[6]
					"D♭" -> instrument[6]
					"D" -> instrument[7]
					"D♯" -> instrument[8]
					"E♭" -> instrument[8]
					"E" -> instrument[9]
					"E♯" -> instrument[10]
					"F♭" -> instrument[9]
					"F" -> instrument[10]
					"F♯" -> instrument[11]
					"G♭" -> instrument[11]
					else -> 0
				}
			}
		}

		// Update metronome
		if (prop?.propertyName == Updater.NOTIFY_BEAT)
		{
			val beat = (prop.newValue as Int) % mSettings.beatsPerBar

			// Update the visuals for each beat
			for (i in mBeats.indices)
			{
				mBeats[i].background = if (i == beat) mBeat1 else mBeat0
			}

			// Play the root note on beat 1
			if (mSettings.rootNoteSounds && beat == 0)
			{
				val volume = mSettings.rootNoteVolume / 100.0f
				mSoundPoolNotes.play(mSoundRootNote, volume, volume, 0, 0, 1.0f)
			}

			// Play the metronome sound on each beat
			if (mSettings.metronomeSounds)
			{
				val volume = mSettings.metronomeVolume / 100.0f
				if (beat == 0)
					mSoundPoolClicks.play(mSoundClicks[mSettings.metronomeAccent] ?: 0, 1.0f * volume, 1.0f * volume, 0, 0, 1.0f)
				else
					mSoundPoolClicks.play(mSoundClicks[mSettings.metronomeClick] ?: 0, 0.7f * volume, 0.7f * volume, 0, 0, 1.0f)
			}
		}
	}

	private fun loadRootNotes()
	{
		val piano = listOf(
			mSoundPoolNotes.load(context, R.raw.piano_00_g, 1),
			mSoundPoolNotes.load(context, R.raw.piano_01_ab, 1),
			mSoundPoolNotes.load(context, R.raw.piano_02_a, 1),
			mSoundPoolNotes.load(context, R.raw.piano_03_bb, 1),
			mSoundPoolNotes.load(context, R.raw.piano_04_b, 1),
			mSoundPoolNotes.load(context, R.raw.piano_05_c, 1),
			mSoundPoolNotes.load(context, R.raw.piano_06_db, 1),
			mSoundPoolNotes.load(context, R.raw.piano_07_d, 1),
			mSoundPoolNotes.load(context, R.raw.piano_08_eb, 1),
			mSoundPoolNotes.load(context, R.raw.piano_09_e, 1),
			mSoundPoolNotes.load(context, R.raw.piano_10_f, 1),
			mSoundPoolNotes.load(context, R.raw.piano_11_gb, 1),
		)
		val acousticBase = listOf(
			mSoundPoolNotes.load(context, R.raw.acoustic_bass_00_g, 1),
			mSoundPoolNotes.load(context, R.raw.acoustic_bass_01_ab, 1),
			mSoundPoolNotes.load(context, R.raw.acoustic_bass_02_a, 1),
			mSoundPoolNotes.load(context, R.raw.acoustic_bass_03_bb, 1),
			mSoundPoolNotes.load(context, R.raw.acoustic_bass_04_b, 1),
			mSoundPoolNotes.load(context, R.raw.acoustic_bass_05_c, 1),
			mSoundPoolNotes.load(context, R.raw.acoustic_bass_06_db, 1),
			mSoundPoolNotes.load(context, R.raw.acoustic_bass_07_d, 1),
			mSoundPoolNotes.load(context, R.raw.acoustic_bass_08_eb, 1),
			mSoundPoolNotes.load(context, R.raw.acoustic_bass_09_e, 1),
			mSoundPoolNotes.load(context, R.raw.acoustic_bass_10_f, 1),
			mSoundPoolNotes.load(context, R.raw.acoustic_bass_11_gb, 1),
		)
		val electricBass = listOf(
			mSoundPoolNotes.load(context, R.raw.electric_bass_00_g, 1),
			mSoundPoolNotes.load(context, R.raw.electric_bass_01_ab, 1),
			mSoundPoolNotes.load(context, R.raw.electric_bass_02_a, 1),
			mSoundPoolNotes.load(context, R.raw.electric_bass_03_bb, 1),
			mSoundPoolNotes.load(context, R.raw.electric_bass_04_b, 1),
			mSoundPoolNotes.load(context, R.raw.electric_bass_05_c, 1),
			mSoundPoolNotes.load(context, R.raw.electric_bass_06_db, 1),
			mSoundPoolNotes.load(context, R.raw.electric_bass_07_d, 1),
			mSoundPoolNotes.load(context, R.raw.electric_bass_08_eb, 1),
			mSoundPoolNotes.load(context, R.raw.electric_bass_09_e, 1),
			mSoundPoolNotes.load(context, R.raw.electric_bass_10_f, 1),
			mSoundPoolNotes.load(context, R.raw.electric_bass_11_gb, 1),
		)
		val instrumentNames = resources.getStringArray(R.array.instruments)
		mSoundRootNotes = mapOf(
			instrumentNames[0] to piano,
			instrumentNames[1] to acousticBase,
			instrumentNames[2] to electricBass,
		)
	}

	private fun loadClicks()
	{
		val clickNames = resources.getStringArray(R.array.metronome_sounds)
		mSoundClicks = mapOf(
			clickNames[0] to mSoundPoolClicks.load(context, R.raw.click_00_clave, 1),
			clickNames[1] to mSoundPoolClicks.load(context, R.raw.click_01_woodblock_low, 1),
			clickNames[2] to mSoundPoolClicks.load(context, R.raw.click_02_woodblock_med, 1),
			clickNames[3] to mSoundPoolClicks.load(context, R.raw.click_03_woodblock_high, 1),
			clickNames[4] to mSoundPoolClicks.load(context, R.raw.click_04_cowbell, 1),
			clickNames[5] to mSoundPoolClicks.load(context, R.raw.click_05_drumsticks, 1),
		)
	}
}
