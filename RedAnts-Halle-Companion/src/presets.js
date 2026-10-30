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
	presets['panic'] = {
		type: 'button',
		category: 'Tasten',
		name: 'PANIC',
		style: { text: 'PANIC', size: '14', color: combineRgb(255, 255, 255), bgcolor: combineRgb(208, 18, 18) },
		steps: [{ down: [{ actionId: 'panic', options: {} }], up: [] }],
		feedbacks: [],
	}
	for (const what of ['stop', 'fade', 'next', 'previous', 'pause']) {
		presets['musik_' + what] = {
			type: 'button',
			category: 'Musik',
			name: 'Musik ' + what,
			style: { text: what, size: '14', color: combineRgb(255, 255, 255), bgcolor: combineRgb(40, 44, 52) },
			steps: [{ down: [{ actionId: 'music', options: { what } }], up: [] }],
			feedbacks: [],
		}
	}
	return presets
}

module.exports = { presetDefinitions }
