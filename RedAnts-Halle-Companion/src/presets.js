const { combineRgb } = require('@companion-module/base')

const presetDefinitions = (instance) => {
	const presets = {}
	for (let slot = 1; slot <= instance.slotCount(); slot++) {
		presets['taste_' + slot] = {
			type: 'button',
			category: 'Tasten',
			name: 'Taste ' + slot,
			style: { text: String(slot), size: '18', color: combineRgb(255, 255, 255), bgcolor: combineRgb(0, 0, 0) },
			previewStyle: {
				text: 'Taste\\n' + slot,
				size: '18',
				color: combineRgb(255, 255, 255),
				bgcolor: combineRgb(40, 44, 52),
			},
			steps: [{ down: [{ actionId: 'press_slot', options: { slot } }], up: [] }],
			feedbacks: [{ feedbackId: 'slot', options: { slot } }],
		}
	}
	return presets
}

module.exports = { presetDefinitions }
