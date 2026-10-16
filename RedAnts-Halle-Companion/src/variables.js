const variableDefinitions = () => [
	{ variableId: 'deck', name: 'Deck' },
	{ variableId: 'seite', name: 'Seite' },
	{ variableId: 'zeit', name: 'Spielzeit' },
	{ variableId: 'drittel', name: 'Drittel' },
	{ variableId: 'stand_heim', name: 'Stand Heim' },
	{ variableId: 'stand_gast', name: 'Stand Gast' },
	{ variableId: 'musik', name: 'Musik' },
]

const variableValues = (view) => ({
	deck: view?.deck ?? '',
	seite: view?.title ?? '',
	zeit: view?.live?.time ?? '--:--',
	drittel: view?.live?.periodLabel ?? '',
	stand_heim: view?.live?.home ?? '',
	stand_gast: view?.live?.away ?? '',
	musik: view?.live?.music ?? '',
})

module.exports = { variableDefinitions, variableValues }
