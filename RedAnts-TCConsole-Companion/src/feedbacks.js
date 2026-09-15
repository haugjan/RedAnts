const { SLOT_COUNT, slotAt, clampSlot } = require('./state')

const MUTED = 0x777777
const OFFLINE = 0x1a1a1a

function slotStyle(instance, number) {
	const slot = slotAt(instance.view, number)
	if (!slot) return { text: String(number), size: 'auto', color: MUTED, bgcolor: OFFLINE }

	const style = {
		text: slot.image ? '' : slot.label,
		size: 'auto',
		color: slot.color,
		bgcolor: slot.bgcolor,
	}

	// Bei bebilderten Tasten steckt die Beschriftung im Bild: TcuConsole rechnet
	// sie hinein, weil Companion die Textflaeche nicht begrenzen kann und der
	// Text sonst ueber das Motiv laeuft.
	if (slot.image) {
		const png = instance.images.get(slot.image)
		if (png) style.png64 = png
		else style.text = slot.label
	}

	return style
}

function feedbackDefinitions(instance) {
	return {
		slot: {
			type: 'advanced',
			name: 'Tastenbild (Text, Icon und Farbe von TcuConsole)',
			options: [
				{
					type: 'number',
					id: 'slot',
					label: 'Taste (1–' + SLOT_COUNT + ')',
					default: 1,
					min: 1,
					max: SLOT_COUNT,
				},
			],
			callback: (feedback) => slotStyle(instance, clampSlot(feedback.options.slot)),
		},
	}
}

module.exports = { feedbackDefinitions, slotStyle }
