const { InstanceBase, InstanceStatus, runEntrypoint } = require('@companion-module/base')
const { apiGet, apiPost } = require('./api')
const { actionDefinitions } = require('./actions')
const { feedbackDefinitions } = require('./feedbacks')
const { presetDefinitions } = require('./presets')
const { variableDefinitions, variableValues } = require('./variables')

const REQUEST_TIMEOUT_MS = 40000
const RETRY_DELAY_MS = 3000

const delay = (ms) => new Promise((resolve) => setTimeout(resolve, ms))

class RedAntsHalleInstance extends InstanceBase {
	async init(config) {
		this.config = config
		this.view = null
		this.images = new Map()
		this.generation = 0
		this.refresh()
		this.start()
	}

	async destroy() {
		this.stop()
	}

	async configUpdated(config) {
		this.stop()
		this.config = config
		this.refresh()
		this.start()
	}

	getConfigFields() {
		return [
			{
				type: 'static-text',
				id: 'intro',
				width: 12,
				label: 'RedAnts Hallensteuerung',
				value:
					'Je Streamdeck eine Instanz: Adresse des Agenten und die Kennung des Decks eintragen. ' +
					'Die Tasten spiegeln, was der Agent gerade zeigt. Ein Preset je Taste liegt bereit, einmal zuordnen genügt.',
			},
			{ type: 'textinput', id: 'url', label: 'Agent', width: 8, default: 'http://localhost:5150' },
			{ type: 'textinput', id: 'deck', label: 'Deck (Kennung, z.B. xl oder mk2)', width: 4, default: 'xl' },
		]
	}

	slotCount() {
		const view = this.view
		if (!view) return 32
		if (view.columns && view.rows) return view.columns * view.rows
		return view.slots?.length ?? 32
	}

	panicKey() {
		return this.view?.panic ?? this.slotCount()
	}

	slot(number) {
		return this.view?.slots?.find((s) => s.number === number) ?? null
	}

	image(key) {
		if (!key) return undefined
		if (this.images.has(key)) return this.images.get(key)
		this.loadImage(key)
		return undefined
	}

	async loadImage(key) {
		this.images.set(key, undefined)
		try {
			const json = await apiGet(this, '/deck/image', { key })
			if (!json.image) return
			this.images.set(key, 'data:image/png;base64,' + json.image)
			this.checkFeedbacks('slot')
		} catch (e) {
			this.images.delete(key)
		}
	}

	async press(slot) {
		try {
			await apiPost(this, '/deck/press/' + slot, { deckId: this.config.deck })
		} catch (e) {
			this.log('warn', 'Taste ' + slot + ' gescheitert: ' + e.message)
		}
	}

	refresh() {
		this.setActionDefinitions(actionDefinitions(this))
		this.setFeedbackDefinitions(feedbackDefinitions(this))
		this.setPresetDefinitions(presetDefinitions(this))
		this.setVariableDefinitions(variableDefinitions())
	}

	start() {
		const generation = ++this.generation
		if (!this.config.url) {
			this.updateStatus(InstanceStatus.BadConfig, 'Keine Adresse des Agenten')
			return
		}
		this.updateStatus(InstanceStatus.Connecting)
		this.watch(generation)
	}

	stop() {
		this.generation++
		if (this.request) this.request.abort()
	}

	async watch(generation) {
		let since
		while (generation === this.generation) {
			const request = new AbortController()
			this.request = request
			const timeout = setTimeout(() => request.abort(), REQUEST_TIMEOUT_MS)
			try {
				const json = await apiGet(this, '/deck/view', { deckId: this.config.deck, since }, request.signal)
				if (generation !== this.generation) return
				since = json.version
				this.show(json)
				this.updateStatus(InstanceStatus.Ok)
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

	show(answer) {
		const slots = answer.slots ?? this.view?.slots
		const known = this.view
		this.view = { ...answer, slots, panic: answer.panic ?? known?.panic }
		if (!known || known.columns !== this.view.columns || known.rows !== this.view.rows) this.refresh()
		this.setVariableValues(variableValues(this.view))
		this.checkFeedbacks('slot')
	}
}

runEntrypoint(RedAntsHalleInstance, [])
