const { combineRgb } = require('@companion-module/base')

const hex = (value) => {
	const number = Number(value) || 0
	return combineRgb((number >> 16) & 0xff, (number >> 8) & 0xff, number & 0xff)
}

const feedbackDefinitions = (instance) => ({
	slot: {
		type: 'advanced',
		name: 'Taste: Farbe, Text und Symbol',
		options: [
			{
				type: 'number',
				id: 'slot',
				label: 'Taste',
				default: 1,
				min: 1,
				max: Math.max(instance.slotCount(), 1),
			},
		],
		callback: (feedback) => {
			const slot = instance.slot(Number(feedback.options.slot))
			if (!slot) return { bgcolor: combineRgb(0, 0, 0), color: combineRgb(255, 255, 255), text: '' }
			return {
				bgcolor: hex(slot.bgcolor),
				color: hex(slot.color),
				text: slot.image ? '' : slot.label || '',
				png64: slot.image ? instance.image(slot.image) : undefined,
				size: 'auto',
			}
		},
	},
})

module.exports = { feedbackDefinitions }
