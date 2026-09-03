const { control } = require('./api')

function actionDefinitions(instance) {
	const s = instance.state
	const tileDefault = s.tileChoices[0] ? s.tileChoices[0].id : ''
	const folderDefault = s.folderChoices[0] ? s.folderChoices[0].id : ''
	const profileDefault = s.profileChoices[0] ? s.profileChoices[0].id : ''

	return {
		play_tile: {
			name: 'Kachel abspielen',
			options: [
				{ type: 'dropdown', id: 'tile', label: 'Kachel', default: tileDefault, choices: s.tileChoices, allowCustom: true },
			],
			callback: async (a) => control(instance, '/api/show/play/' + encodeURIComponent(a.options.tile)),
		},
		play_song: {
			name: 'Einzelnen Song abspielen',
			options: [
				{ type: 'dropdown', id: 'tile', label: 'Kachel', default: tileDefault, choices: s.tileChoices, allowCustom: true },
				{ type: 'number', id: 'index', label: 'Song-Index (0-basiert)', default: 0, min: 0, max: 63 },
			],
			callback: async (a) =>
				control(instance, '/api/show/song/' + encodeURIComponent(a.options.tile) + '/' + Number(a.options.index || 0)),
		},
		open_folder: {
			name: 'Ordner oeffnen',
			options: [
				{ type: 'dropdown', id: 'folder', label: 'Ordner', default: folderDefault, choices: s.folderChoices, allowCustom: true },
			],
			callback: async (a) => control(instance, '/api/show/folder/' + encodeURIComponent(a.options.folder)),
		},
		back: {
			name: 'Zurueck (Ordner hoch)',
			options: [],
			callback: async () => control(instance, '/api/show/back'),
		},
		home: {
			name: 'Home (Wurzel-Ebene)',
			options: [],
			callback: async () => control(instance, '/api/show/home'),
		},
		switch_profile: {
			name: 'Profil wechseln',
			options: [
				{ type: 'dropdown', id: 'profile', label: 'Profil', default: profileDefault, choices: s.profileChoices, allowCustom: true },
			],
			callback: async (a) => control(instance, '/api/show/profile/' + encodeURIComponent(a.options.profile)),
		},
		stop: { name: 'Stopp', options: [], callback: async () => control(instance, '/api/show/stop') },
		pause: { name: 'Pause', options: [], callback: async () => control(instance, '/api/show/pause') },
		resume: { name: 'Weiter (Resume)', options: [], callback: async () => control(instance, '/api/show/resume') },
		fade: { name: 'Fade-out', options: [], callback: async () => control(instance, '/api/show/fade') },
	}
}

module.exports = { actionDefinitions }
