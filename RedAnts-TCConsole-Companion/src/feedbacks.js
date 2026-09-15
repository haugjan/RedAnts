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

// Companion laesst Feedbacks auch in der Preset-Vorschau laufen: jede
// Vorschau-Taste ist ein eigenes Control mit der Kennung
// "preset:<Verbindung>:<Preset>:<Hash>" (geprueft an Companion 5.0.3), echte
// Tasten heissen "bank:<id>". Ohne diese Weiche zeigte die Preset-Liste den
// aktuellen Inhalt von TcuConsole statt der Nummern — und aenderte sich mit
// jedem Kontextwechsel, sodass beim Zuweisen nicht erkennbar war, welches
// Preset welche Taste ist.
function isPresetPreview(feedback) {
	return String(feedback.controlId || '').startsWith('preset:')
}

function numberStyle(number) {
	return { text: String(number), size: 'auto', color: 0xffffff, bgcolor: OFFLINE }
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
			callback: (feedback) => {
				const number = clampSlot(feedback.options.slot)
				return isPresetPreview(feedback) ? numberStyle(number) : slotStyle(instance, number)
			},
		},
	}
}

module.exports = { feedbackDefinitions, slotStyle }
