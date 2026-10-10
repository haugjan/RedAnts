const { SLOT_COUNT } = require('./state')

function presetDefinitions() {
	const presets = {}
	for (let n = 1; n <= SLOT_COUNT; n++) {
		presets['slot_' + n] = {
			type: 'button',
			category: 'Slots',
			name: 'Slot ' + n,
			style: { text: String(n), size: 'auto', color: 0x777777, bgcolor: 0x1a1a1a },
			steps: [{ down: [{ actionId: 'press_slot', options: { slot: n } }], up: [] }],
			feedbacks: [{ feedbackId: 'slot', options: { slot: n } }],
		}
	}
	return presets
}

module.exports = { presetDefinitions }
