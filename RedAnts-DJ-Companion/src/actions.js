const { control } = require('./api')
const { SLOT_COUNT, clampSlot } = require('./state')

function simple(instance, name, path) {
	return { name, options: [], callback: async () => control(instance, path) }
}

function actionDefinitions(instance) {
	const profiles = instance.profiles || []

	return {
		press_slot: {
			name: 'Slot drücken (Inhalt kommt vom Board)',
			options: [{ type: 'number', id: 'slot', label: 'Slot (1–' + SLOT_COUNT + ')', default: 1, min: 1, max: SLOT_COUNT }],
			callback: async (a) => control(instance, '/api/dj/press/' + clampSlot(a.options.slot)),
		},
		profile_next: simple(instance, 'Profil: nächstes', '/api/dj/profile-next'),
		profile_prev: simple(instance, 'Profil: vorheriges', '/api/dj/profile-prev'),
		switch_profile: {
			name: 'Profil wählen',
			options: [
				{
					type: 'dropdown',
					id: 'profile',
					label: 'Profil',
					default: profiles[0] ? profiles[0].id : '',
					choices: profiles,
					allowCustom: true,
				},
			],
			callback: async (a) => control(instance, '/api/dj/profile/' + encodeURIComponent(a.options.profile)),
		},
		back: simple(instance, 'Zurück (Ordner hoch)', '/api/dj/back'),
		home: simple(instance, 'Home (Wurzel-Ebene)', '/api/dj/home'),
		stop: simple(instance, 'Stopp', '/api/dj/stop'),
		pause: simple(instance, 'Pause', '/api/dj/pause'),
		resume: simple(instance, 'Weiter', '/api/dj/resume'),
		fade: simple(instance, 'Fade-out', '/api/dj/fade'),
	}
}

module.exports = { actionDefinitions }
