import { pinyin } from 'pinyin-pro'

export function getFirstLetter(str = '') {
  if (!str) return '#'
  const first = str[0]
  if (/[a-zA-Z]/.test(first)) return first.toUpperCase()
  if (/[一-龥]/.test(first)) {
    const py = pinyin(first, { pattern: 'first', toneType: 'none' })
    return ((py[0]?.[0]) || '#').toUpperCase()
  }
  return '#'
}

export function groupByFirstLetter(list, key = 'name') {
  const map = {}
  ;[...list]
    .sort((a, b) => a[key].localeCompare(b[key], 'zh-CN'))
    .forEach((item) => {
      const letter = getFirstLetter(item[key])
      if (!map[letter]) map[letter] = []
      map[letter].push(item)
    })
  return Object.keys(map)
    .sort((a, b) => {
      if (a === '#') return 1
      if (b === '#') return -1
      return a.localeCompare(b)
    })
    .map((letter) => ({ letter, items: map[letter] }))
}
