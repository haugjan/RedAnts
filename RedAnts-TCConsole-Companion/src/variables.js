const { SLOT_COUNT, slotAt } = require('./state')

function variableDefinitions() {
	const definitions = [
		{ variableId: 'connected', name: 'TcuConsole erreichbar (ja/nein)' },
		{ variableId: 'context', name: 'Angezeigte Ebene' },
	]
	for (let n = 1; n <= SLOT_COUNT; n++) {
		definitions.push({ variableId: 'slot_' + n, name: 'Taste ' + n + ': Beschriftung' })
	}
	return definitions
}

function variableValues(view) {
	const values = {
		connected: view ? 'ja' : 'nein',
		context: view ? view.title : '',
	}
	for (let n = 1; n <= SLOT_COUNT; n++) {
		const slot = slotAt(view, n)
		values['slot_' + n] = slot ? slot.label : ''
	}
	return values
}

module.exports = { variableDefinitions, variableValues }
