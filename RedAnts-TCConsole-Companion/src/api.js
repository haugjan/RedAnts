function buildUrl(config, path, params) {
	const base = String(config.url || '').replace(/\/+$/, '')
	const url = new URL(base + path)
	for (const [name, value] of Object.entries(params || {})) {
		if (value !== undefined && value !== null) url.searchParams.set(name, String(value))
	}
	return url.toString()
}

async function request(instance, method, path, params, signal) {
	const res = await fetch(buildUrl(instance.config, path, params), { method, signal })
	if (!res.ok) throw new Error('HTTP ' + res.status + ' fuer ' + path)
	const text = await res.text()
	return text ? JSON.parse(text) : null
}

const apiGet = (instance, path, params, signal) => request(instance, 'GET', path, params, signal)

async function press(instance, slot) {
	try {
		const json = await request(instance, 'POST', '/deck/press/' + slot)
		if (json && json.note) instance.log('warn', 'Taste ' + slot + ': ' + json.note)
	} catch (e) {
		instance.log('warn', 'Taste ' + slot + ' fehlgeschlagen: ' + e.message)
	}
}

module.exports = { buildUrl, apiGet, press }
