const { press } = require('./api')
const { SLOT_COUNT, clampSlot } = require('./state')

function actionDefinitions(instance) {
	return {
		press_slot: {
			name: 'Taste drücken (Inhalt kommt von TcuConsole)',
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
			callback: async (a) => press(instance, clampSlot(a.options.slot)),
		},
	}
}

module.exports = { actionDefinitions }
