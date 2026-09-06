function flatten(stateJson) {
	const profiles = (stateJson && stateJson.profiles) || []
	const tiles = []
	const folders = []

	for (const profile of profiles) {
		const walk = (nodes, trail) => {
			for (const node of nodes || []) {
				const path = trail ? trail + ' › ' + node.label : node.label
				if (node.folder) {
					folders.push({ id: node.id, label: profile.name + ' › ' + path, color: node.color })
					walk(node.children, path)
				} else {
					tiles.push({
						id: node.id,
						label: profile.name + ' › ' + path,
						plain: node.label,
						color: node.color,
						songs: node.songs || 0,
						profileId: profile.id,
					})
				}
			}
		}
		walk(profile.tiles, '')
	}

	const profileChoices = profiles.map((p) => ({ id: p.id, label: p.name }))
	const tileChoices = tiles.map((t) => ({ id: t.id, label: t.label }))
	const folderChoices = folders.map((f) => ({ id: f.id, label: f.label }))

	return { profiles, tiles, folders, profileChoices, tileChoices, folderChoices }
}

function hexToColor(hex) {
	if (!hex) return 0x3c3c3c
	const m = /^#?([0-9a-f]{6})$/i.exec(String(hex).trim())
	return m ? parseInt(m[1], 16) : 0x3c3c3c
}

module.exports = { flatten, hexToColor }
