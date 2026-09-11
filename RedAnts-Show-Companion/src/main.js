const { InstanceBase, InstanceStatus, runEntrypoint } = require('@companion-module/base')
const { apiGet } = require('./api')
const { profileChoices } = require('./state')
const { actionDefinitions } = require('./actions')
const { feedbackDefinitions } = require('./feedbacks')
const { presetDefinitions } = require('./presets')
const { variableDefinitions, variableValues } = require('./variables')

const REQUEST_TIMEOUT_MS = 40000
const RETRY_DELAY_MS = 3000
const PROFILE_REFRESH_MS = 60000

const delay = (ms) => new Promise((resolve) => setTimeout(resolve, ms))

class RedAntsShowInstance extends InstanceBase {
	async init(config) {
		this.config = config
		this.view = null
		this.profiles = []
		this.generation = 0
		this.setActionDefinitions(actionDefinitions(this))
		this.setFeedbackDefinitions(feedbackDefinitions(this))
		this.setPresetDefinitions(presetDefinitions(this))
		this.setVariableDefinitions(variableDefinitions())
		this.showView(null)
		this.start()
	}

	async destroy() {
		this.stop()
	}

	async configUpdated(config) {
		this.stop()
		this.config = config
		this.start()
	}

	getConfigFields() {
		return [
			{
				type: 'static-text',
				id: 'intro',
				width: 12,
				label: 'RedAnts Soundboard',
				value:
					'Die Slots 1–15 zeigen live, was das Board gerade anzeigt (Profil und Ordner inklusive). Das Board muss im Browser offen sein. Fuer mehrere gleichzeitige Spiele je Board einen Board-Code (Room) verwenden.',
			},
			{ type: 'textinput', id: 'url', label: 'Server-URL', width: 8, default: 'https://show.redants.ch' },
			{ type: 'textinput', id: 'key', label: 'API-Key (Show:ApiKey bzw. Board-Passwort)', width: 4, default: '' },
			{ type: 'textinput', id: 'room', label: 'Board-Code (Room, optional) – leer = zuletzt aktives Board', width: 8, default: '' },
		]
	}

	start() {
		const generation = ++this.generation
		if (!this.config.url) {
			this.updateStatus(InstanceStatus.BadConfig, 'Keine Server-URL')
			return
		}
		this.updateStatus(InstanceStatus.Connecting)
		this.watchBoard(generation)
		this.loadProfiles(generation)
		this.profileTimer = setInterval(() => this.loadProfiles(generation), PROFILE_REFRESH_MS)
	}

	stop() {
		this.generation++
		if (this.request) this.request.abort()
		if (this.profileTimer) clearInterval(this.profileTimer)
		this.profileTimer = undefined
	}

	async watchBoard(generation) {
		let since
		while (generation === this.generation) {
			const request = new AbortController()
			this.request = request
			const timeout = setTimeout(() => request.abort(), REQUEST_TIMEOUT_MS)
			try {
				const json = await apiGet(this, '/api/show/view', { since }, request.signal)
				if (generation !== this.generation) return
				since = json.version
				this.showView(json.connected ? json.view : null)
				if (json.connected) this.updateStatus(InstanceStatus.Ok)
				else this.updateStatus(InstanceStatus.UnknownWarning, 'Kein Board offen')
			} catch (e) {
				if (generation !== this.generation) return
				since = undefined
				this.updateStatus(InstanceStatus.ConnectionFailure, e.message)
				await delay(RETRY_DELAY_MS)
			} finally {
				clearTimeout(timeout)
			}
		}
	}

	async loadProfiles(generation) {
		try {
			const json = await apiGet(this, '/api/show/state')
			if (generation !== this.generation) return
			this.profiles = profileChoices(json)
			this.setActionDefinitions(actionDefinitions(this))
		} catch (e) {
			this.log('warn', 'Profile konnten nicht geladen werden: ' + e.message)
		}
	}

	showView(view) {
		this.view = view
		this.setVariableValues(variableValues(view, this.config.room))
		this.checkFeedbacks('slot')
	}
}

runEntrypoint(RedAntsShowInstance, [])
