const { InstanceBase, InstanceStatus, runEntrypoint } = require('@companion-module/base')
const { apiGet } = require('./api')
const { actionDefinitions } = require('./actions')
const { feedbackDefinitions } = require('./feedbacks')
const { presetDefinitions } = require('./presets')
const { variableDefinitions, variableValues } = require('./variables')

const REQUEST_TIMEOUT_MS = 40000
const RETRY_DELAY_MS = 3000
const IMAGE_CACHE_LIMIT = 400

const delay = (ms) => new Promise((resolve) => setTimeout(resolve, ms))

class RedAntsTcConsoleInstance extends InstanceBase {
	async init(config) {
		this.config = config
		this.view = null
		this.images = new Map()
		this.generation = 0
		this.setActionDefinitions(actionDefinitions(this))
		this.setFeedbackDefinitions(feedbackDefinitions(this))
		this.setPresetDefinitions(presetDefinitions())
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
		this.images.clear()
		this.start()
	}

	getConfigFields() {
		return [
			{
				type: 'static-text',
				id: 'intro',
				width: 12,
				label: 'TCConsole',
				value:
					'Die Tasten 1–32 werden einmal zugewiesen (Presets "Tasten 1–32" hineinziehen). Was darauf steht und was ein Druck ausloest, bestimmt danach allein TcuConsole. TcuConsole muss laufen.',
			},
			{ type: 'textinput', id: 'url', label: 'TcuConsole-URL', width: 8, default: 'http://localhost:5150' },
		]
	}

	start() {
		const generation = ++this.generation
		if (!this.config.url) {
			this.updateStatus(InstanceStatus.BadConfig, 'Keine TcuConsole-URL')
			return
		}
		this.updateStatus(InstanceStatus.Connecting)
		this.watchDeck(generation)
	}

	stop() {
		this.generation++
		if (this.request) this.request.abort()
	}

	// Long-Poll: TcuConsole haelt die Anfrage, bis sich am Deck etwas aendert.
	// Faellt sie ins Timeout, wird sofort neu gefragt — das kostet nichts und
	// haelt die Verbindung offen.
	async watchDeck(generation) {
		let since
		while (generation === this.generation) {
			const request = new AbortController()
			this.request = request
			const timeout = setTimeout(() => request.abort(), REQUEST_TIMEOUT_MS)
			try {
				const json = await apiGet(this, '/deck/view', { since }, request.signal)
				if (generation !== this.generation) return
				since = json.version
				await this.loadImages(json, generation)
				if (generation !== this.generation) return
				this.showView(json)
				this.updateStatus(InstanceStatus.Ok)
			} catch (e) {
				if (generation !== this.generation) return
				// Ohne seit-Wert neu beginnen: nach einem Neustart von TcuConsole
				// faengt die Zaehlung wieder bei 1 an, und ein alter, hoeherer
				// Wert liesse den Long-Poll bis zum Timeout leer laufen.
				since = undefined
				this.updateStatus(InstanceStatus.ConnectionFailure, e.message)
				await delay(RETRY_DELAY_MS)
			} finally {
				clearTimeout(timeout)
			}
		}
	}

	// Tastenbilder kommen einzeln und nur einmal je Schluessel. Der Schluessel
	// enthaelt Motiv, Beschriftung und Schriftfarbe — aendert sich davon etwas,
	// ist es ein neues Bild.
	async loadImages(view, generation) {
		const keys = new Set((view.slots || []).map((s) => s.image).filter(Boolean))
		for (const key of keys) {
			if (this.images.has(key) || generation !== this.generation) continue
			try {
				const json = await apiGet(this, '/deck/image', { key })
				if (json && json.image) this.images.set(key, json.image)
			} catch (e) {
				this.log('warn', 'Tastenbild ' + key + ' nicht geladen: ' + e.message)
			}
		}
		// Ueber ein ganzes Spiel sammeln sich die Namen mehrerer Kader an. Der
		// Cache wird deshalb geleert statt einzeln aufgeraeumt — die Bilder des
		// aktuellen Decks sind mit dem naechsten Takt wieder da.
		if (this.images.size > IMAGE_CACHE_LIMIT) this.images.clear()
	}

	showView(view) {
		this.view = view
		this.setVariableValues(variableValues(view))
		this.checkFeedbacks('slot')
	}
}

runEntrypoint(RedAntsTcConsoleInstance, [])
