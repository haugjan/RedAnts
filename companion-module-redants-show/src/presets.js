const { hexToColor } = require('./state')

function presetDefinitions(instance) {
	const s = instance.state
	const presets = {}
	const white = 0xffffff

	for (const tile of s.tiles) {
		presets['tile_' + tile.id] = {
			type: 'button',
			category: 'Kacheln',
			name: tile.label,
			style: { text: tile.plain, size: 'auto', color: white, bgcolor: hexToColor(tile.color) },
			steps: [{ down: [{ actionId: 'play_tile', options: { tile: tile.id } }], up: [] }],
			feedbacks: [],
		}
	}

	const transport = [
		{ key: 'stop', text: '⏹ STOP', action: 'stop', bg: 0xc8102e },
		{ key: 'pause', text: '⏸ PAUSE', action: 'pause', bg: 0x444444 },
		{ key: 'resume', text: '▶ WEITER', action: 'resume', bg: 0x1c7c43 },
		{ key: 'fade', text: '🔉 FADE', action: 'fade', bg: 0xe07a1f },
		{ key: 'back', text: '↩ ZURÜCK', action: 'back', bg: 0x333333 },
		{ key: 'home', text: '⌂ HOME', action: 'home', bg: 0x333333 },
	]
	for (const t of transport) {
		presets['transport_' + t.key] = {
			type: 'button',
			category: 'Transport',
			name: t.text,
			style: { text: t.text, size: 'auto', color: white, bgcolor: t.bg },
			steps: [{ down: [{ actionId: t.action, options: {} }], up: [] }],
			feedbacks: [],
		}
	}

	return presets
}

module.exports = { presetDefinitions }
