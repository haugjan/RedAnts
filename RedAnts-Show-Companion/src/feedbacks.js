const { SLOT_COUNT, slotAt, clampSlot, hexToColor } = require('./state')

const WHITE = 0xffffff
const BLACK = 0x000000
const MUTED = 0x777777
const DISABLED = 0x262626
const OFFLINE = 0x1a1a1a
const CONTROL_COLORS = { Back: 0x333333, Pause: 0x444444, Fade: 0xe07a1f, Previous: 0x1d3557, Next: 0x1d3557 }

function slotStyle(slot, number) {
	if (!slot) return { text: String(number), size: 'auto', color: MUTED, bgcolor: OFFLINE }
	if (slot.kind === 'Empty') return { text: '', size: 'auto', color: WHITE, bgcolor: BLACK }
	const text = (slot.icon ? slot.icon + '\n' : '') + slot.label
	if (!slot.enabled) return { text, size: 'auto', color: MUTED, bgcolor: DISABLED }
	if (slot.kind in CONTROL_COLORS) return { text, size: 'auto', color: WHITE, bgcolor: CONTROL_COLORS[slot.kind] }
	const color = hexToColor(slot.color)
	if (slot.active) return { text, size: 'auto', color, bgcolor: WHITE }
	return { text, size: 'auto', color: WHITE, bgcolor: color }
}

function feedbackDefinitions(instance) {
	return {
		slot: {
			type: 'advanced',
			name: 'Slot-Anzeige (Text, Icon und Farbe vom Board)',
			options: [{ type: 'number', id: 'slot', label: 'Slot (1–' + SLOT_COUNT + ')', default: 1, min: 1, max: SLOT_COUNT }],
			callback: (feedback) => {
				const number = clampSlot(feedback.options.slot)
				return slotStyle(slotAt(instance.view, number), number)
			},
		},
	}
}

module.exports = { feedbackDefinitions, slotStyle }
