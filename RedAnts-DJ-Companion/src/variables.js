const { SLOT_COUNT, slotAt } = require('./state')

function variableDefinitions() {
	const definitions = [
		{ variableId: 'connected', name: 'Board verbunden (ja/nein)' },
		{ variableId: 'profile', name: 'Aktives Profil' },
		{ variableId: 'path', name: 'Geöffneter Ordner' },
		{ variableId: 'now_playing', name: 'Läuft gerade' },
		{ variableId: 'unlocked', name: 'Ton am Board freigeschaltet (ja/nein)' },
		{ variableId: 'room', name: 'Board-Code (Room)' },
	]
	for (let n = 1; n <= SLOT_COUNT; n++) definitions.push({ variableId: 'slot_' + n, name: 'Slot ' + n + ': Beschriftung' })
	return definitions
}

function variableValues(view, room) {
	const values = {
		connected: view ? 'ja' : 'nein',
		profile: view ? view.profileName : '',
		path: view && view.path ? view.path.join(' › ') : '',
		now_playing: (view && view.nowPlaying) || '',
		unlocked: view && view.unlocked ? 'ja' : 'nein',
		room: String(room || '').trim() || '(alle)',
	}
	for (let n = 1; n <= SLOT_COUNT; n++) {
		const slot = slotAt(view, n)
		values['slot_' + n] = slot ? slot.label : ''
	}
	return values
}

module.exports = { variableDefinitions, variableValues }
