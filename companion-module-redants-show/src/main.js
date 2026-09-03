const { InstanceBase, InstanceStatus, runEntrypoint } = require('@companion-module/base')
const { apiGet } = require('./api')
const { flatten } = require('./state')
const { actionDefinitions } = require('./actions')
const { presetDefinitions } = require('./presets')

const emptyState = { profiles: [], tiles: [], folders: [], profileChoices: [], tileChoices: [], folderChoices: [] }

class RedAntsShowInstance extends InstanceBase {
	async init(config) {
		this.config = config
		this.state = emptyState
		this.updateStatus(InstanceStatus.Connecting)
		this.applyDefinitions()
		await this.poll()
		this.startPolling()
	}

	async destroy() {
		this.stopPolling()
	}

	async configUpdated(config) {
		this.config = config
		this.stopPolling()
		this.updateStatus(InstanceStatus.Connecting)
		await this.poll()
		this.startPolling()
	}

	getConfigFields() {
		return [
			{
				type: 'static-text',
				id: 'intro',
				width: 12,
				label: 'RedAnts Soundboard',
				value: 'Steuert ein geoeffnetes Board ueber /api/show. Das Board muss im Browser offen sein. Fuer mehrere gleichzeitige Spiele je Board einen Board-Code (Room) verwenden.',
			},
			{ type: 'textinput', id: 'url', label: 'Server-URL', width: 8, default: 'https://show.redants.ch' },
			{ type: 'textinput', id: 'key', label: 'API-Key (Show:ApiKey bzw. Board-Passwort)', width: 4, default: '' },
			{
				type: 'textinput',
				id: 'room',
				label: 'Board-Code (Room, optional) – leer = alle Boards',
				width: 8,
				default: '',
			},
			{ type: 'number', id: 'poll', label: 'Abfrage-Intervall (Sekunden)', width: 4, default: 15, min: 3, max: 300 },
		]
	}

	startPolling() {
		const seconds = Math.max(3, Number(this.config.poll) || 15)
		this.pollTimer = setInterval(() => {
			this.poll().catch(() => {})
		}, seconds * 1000)
	}

	stopPolling() {
		if (this.pollTimer) {
			clearInterval(this.pollTimer)
			this.pollTimer = undefined
		}
	}

	async poll() {
		if (!this.config.url) {
			this.updateStatus(InstanceStatus.BadConfig, 'Keine Server-URL')
			return
		}
		try {
			const json = await apiGet(this, '/api/show/state', false)
			this.state = flatten(json)
			this.applyDefinitions()
			this.setVariableValues({
				profiles_count: this.state.profiles.length,
				tiles_count: this.state.tiles.length,
				folders_count: this.state.folders.length,
				room: this.config.room || '(alle)',
			})
			this.updateStatus(InstanceStatus.Ok)
		} catch (e) {
			this.updateStatus(InstanceStatus.ConnectionFailure, e.message)
			this.log('warn', 'Status-Abfrage fehlgeschlagen: ' + e.message)
		}
	}

	applyDefinitions() {
		this.setActionDefinitions(actionDefinitions(this))
		this.setPresetDefinitions(presetDefinitions(this))
		this.setVariableDefinitions([
			{ variableId: 'profiles_count', name: 'Anzahl Profile' },
			{ variableId: 'tiles_count', name: 'Anzahl Kacheln' },
			{ variableId: 'folders_count', name: 'Anzahl Ordner' },
			{ variableId: 'room', name: 'Board-Code (Room)' },
		])
	}
}

runEntrypoint(RedAntsShowInstance, [])
