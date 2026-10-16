const { apiPost } = require('./api')

const slotOption = (slots) => ({
	type: 'number',
	id: 'slot',
	label: 'Taste',
	default: 1,
	min: 1,
	max: Math.max(slots, 1),
})

const actionDefinitions = (instance) => ({
	press_slot: {
		name: 'Taste drücken',
		options: [slotOption(instance.slotCount())],
		callback: async (action) => instance.press(Number(action.options.slot)),
	},
	panic: {
		name: 'PANIC',
		options: [],
		callback: async () => instance.press(instance.panicKey()),
	},
	music: {
		name: 'Musik steuern',
		options: [
			{
				type: 'dropdown',
				id: 'what',
				label: 'Befehl',
				default: 'stop',
				choices: [
					{ id: 'stop', label: 'Stopp' },
					{ id: 'fade', label: 'Fade-out' },
					{ id: 'next', label: 'Nächstes Stück' },
					{ id: 'previous', label: 'Vorheriges Stück' },
					{ id: 'pause', label: 'Pause und Weiter' },
				],
			},
		],
		callback: async (action) => {
			try {
				await apiPost(instance, '/deck/music/' + action.options.what, { deckId: instance.config.deck })
			} catch (e) {
				instance.log('warn', 'Musikbefehl gescheitert: ' + e.message)
			}
		},
	},
	home: {
		name: 'Zur Startseite',
		options: [],
		callback: async () => {
			try {
				await apiPost(instance, '/deck/home', { deckId: instance.config.deck })
			} catch (e) {
				instance.log('warn', 'Startseite gescheitert: ' + e.message)
			}
		},
	},
})

module.exports = { actionDefinitions }
