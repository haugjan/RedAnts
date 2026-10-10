function buildUrl(config, path, params) {
	const base = String(config.url || '').replace(/\/+$/, '')
	const url = new URL(base + path)
	if (config.key) url.searchParams.set('key', config.key)
	const room = String(config.room || '').trim()
	if (room) url.searchParams.set('room', room)
	for (const [name, value] of Object.entries(params || {})) {
		if (value !== undefined && value !== null) url.searchParams.set(name, String(value))
	}
	return url.toString()
}

async function apiGet(instance, path, params, signal) {
	const res = await fetch(buildUrl(instance.config, path, params), { method: 'GET', signal })
	if (res.status === 401) throw new Error('API-Key falsch (HTTP 401)')
	if (!res.ok) throw new Error('HTTP ' + res.status + ' fuer ' + path)
	const text = await res.text()
	return text ? JSON.parse(text) : null
}

async function control(instance, path) {
	try {
		await apiGet(instance, path)
	} catch (e) {
		instance.log('warn', 'Befehl fehlgeschlagen (' + path + '): ' + e.message)
	}
}

module.exports = { buildUrl, apiGet, control }
