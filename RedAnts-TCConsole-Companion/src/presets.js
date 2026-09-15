const { SLOT_COUNT } = require('./state')

// Ein Preset je Taste: hineinziehen, fertig. Aktion und Feedback tragen
// dieselbe Nummer — mehr gibt es an dieser Zuordnung nie einzustellen.
function presetDefinitions() {
	const presets = {}
	for (let n = 1; n <= SLOT_COUNT; n++) {
		presets['slot_' + n] = {
			type: 'button',
			category: 'Tasten 1–' + SLOT_COUNT,
			name: 'Taste ' + n,
			style: { text: String(n), size: 'auto', color: 0xffffff, bgcolor: 0x1a1a1a },
			steps: [{ down: [{ actionId: 'press_slot', options: { slot: n } }], up: [] }],
			feedbacks: [{ feedbackId: 'slot', options: { slot: n } }],
		}
	}
	return presets
}

module.exports = { presetDefinitions }
