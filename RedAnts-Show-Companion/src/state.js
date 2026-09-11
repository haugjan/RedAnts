const SLOT_COUNT = 15

function profileChoices(stateJson) {
	const profiles = (stateJson && stateJson.profiles) || []
	return profiles.map((p) => ({ id: p.id, label: p.name }))
}

function slotAt(view, number) {
	if (!view || !Array.isArray(view.slots)) return null
	return view.slots.find((s) => s.number === Number(number)) || null
}

function clampSlot(value) {
	const n = Math.round(Number(value) || 1)
	return Math.min(SLOT_COUNT, Math.max(1, n))
}

function hexToColor(hex) {
	const value = String(hex || '').trim().replace(/^#/, '')
	if (/^[0-9a-f]{6}$/i.test(value)) return parseInt(value, 16)
	if (/^[0-9a-f]{3}$/i.test(value)) return parseInt(value.replace(/./g, (c) => c + c), 16)
	return 0x3c3c3c
}

module.exports = { SLOT_COUNT, profileChoices, slotAt, clampSlot, hexToColor }
