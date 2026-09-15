const SLOT_COUNT = 32

function slotAt(view, number) {
	if (!view || !Array.isArray(view.slots)) return null
	return view.slots.find((s) => s.number === Number(number)) || null
}

function clampSlot(value) {
	const n = Math.round(Number(value) || 1)
	return Math.min(SLOT_COUNT, Math.max(1, n))
}

module.exports = { SLOT_COUNT, slotAt, clampSlot }
