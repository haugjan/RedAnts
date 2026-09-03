function buildUrl(config, path, includeRoom) {
	const base = String(config.url || '').replace(/\/+$/, '')
	const url = new URL(base + path)
	if (config.key) url.searchParams.set('key', config.key)
	if (includeRoom && config.room) url.searchParams.set('room', String(config.room).trim())
	return url.toString()
}

async function apiGet(instance, path, includeRoom) {
	const url = buildUrl(instance.config, path, includeRoom)
	const res = await fetch(url, { method: 'GET' })
	if (!res.ok) {
		throw new Error('HTTP ' + res.status + ' fuer ' + path)
	}
	const text = await res.text()
	return text ? JSON.parse(text) : null
}

async function control(instance, path) {
	try {
		await apiGet(instance, path, true)
	} catch (e) {
		instance.log('warn', 'Befehl fehlgeschlagen (' + path + '): ' + e.message)
	}
}

module.exports = { buildUrl, apiGet, control }
