const { SLOT_COUNT } = require('./state')

const WHITE = 0xffffff
const BLACK = 0x000000

function button(category, name, style, actionId, options, feedbacks) {
	return {
		type: 'button',
		category,
		name,
		style: { size: 'auto', color: WHITE, ...style },
		steps: [{ down: [{ actionId, options: options || {} }], up: [] }],
		feedbacks: feedbacks || [],
	}
}

function presetDefinitions(instance) {
	const presets = {}

	for (let n = 1; n <= SLOT_COUNT; n++) {
		presets['slot_' + n] = button('Slots', 'Slot ' + n, { text: 'Slot ' + n, bgcolor: BLACK }, 'press_slot', { slot: n }, [
			{ feedbackId: 'slot', options: { slot: n } },
		])
	}

	const profile = '$(' + instance.label + ':profile)'
	presets.profile_next = button('Steuerung', 'Profil: nächstes', { text: 'Profil ▶\n' + profile, bgcolor: 0x1d3557 }, 'profile_next')
	presets.profile_prev = button('Steuerung', 'Profil: vorheriges', { text: '◀ Profil\n' + profile, bgcolor: 0x1d3557 }, 'profile_prev')
	presets.home = button('Steuerung', 'Home', { text: '⌂\nHome', bgcolor: 0x333333 }, 'home')
	presets.back = button('Steuerung', 'Zurück', { text: '↩\nZurück', bgcolor: 0x333333 }, 'back')
	presets.stop = button('Steuerung', 'Stopp', { text: '⏹\nStopp', bgcolor: 0xc8102e }, 'stop')
	presets.pause = button('Steuerung', 'Pause', { text: '⏸\nPause', bgcolor: 0x444444 }, 'pause')
	presets.resume = button('Steuerung', 'Weiter', { text: '▶\nWeiter', bgcolor: 0x1c7c43 }, 'resume')
	presets.fade = button('Steuerung', 'Fade-out', { text: '🔉\nFade', bgcolor: 0xe07a1f }, 'fade')

	return presets
}

module.exports = { presetDefinitions }
