const fetchJson = async (instance, path, query, signal, method) => {
	const base = (instance.config.url || '').replace(/\/+$/, '')
	const url = new URL(base + path)
	for (const [key, value] of Object.entries(query || {})) {
		if (value !== undefined && value !== null && value !== '') url.searchParams.set(key, String(value))
	}
	const answer = await fetch(url, { method: method || 'GET', signal })
	if (!answer.ok) throw new Error(`${answer.status} ${answer.statusText}`)
	const text = await answer.text()
	return text.length > 0 ? JSON.parse(text) : {}
}

module.exports = {
	apiGet: (instance, path, query, signal) => fetchJson(instance, path, query, signal, 'GET'),
	apiPost: (instance, path, query) => fetchJson(instance, path, query, undefined, 'POST'),
}
