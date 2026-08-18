<template>
  <div class="flex h-full">
    <!-- 左侧 API 列表 -->
    <div class="w-80 border-r border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-[#1A1D23] flex flex-col flex-shrink-0">
      <div class="px-4 py-3 border-b border-gray-200 dark:border-gray-700">
        <input v-model="search" placeholder="搜索 API"
               class="w-full h-8 px-3 text-sm bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-md outline-none dark:text-gray-100" />
      </div>
      <div class="flex-1 overflow-y-auto">
        <div v-for="g in groupedApis" :key="g.name">
          <div class="px-4 py-2 text-xs font-medium text-gray-500 bg-gray-100 dark:bg-gray-800/50 sticky top-0">
            {{ g.name }}
          </div>
          <button v-for="api in g.items" :key="api.path" @click="active = api"
                  :class="['w-full text-left px-4 py-2 flex items-center space-x-2 text-sm transition',
                           active?.path === api.path
                             ? 'bg-primary-50 dark:bg-primary/20 text-primary'
                             : 'hover:bg-gray-100 dark:hover:bg-gray-800 text-gray-700 dark:text-gray-300']">
            <span :class="['px-1.5 py-0.5 rounded text-[10px] font-bold w-12 text-center flex-shrink-0', methodClass(api.method)]">{{ api.method }}</span>
            <span class="font-mono text-xs truncate">{{ api.path }}</span>
          </button>
        </div>
      </div>
    </div>

    <!-- 详情 -->
    <div class="flex-1 overflow-y-auto">
      <div class="max-w-3xl mx-auto p-8" v-if="active">
        <h2 class="text-xl font-semibold text-gray-900 dark:text-gray-100">{{ active.desc }}</h2>
        <div class="mt-3 flex items-center space-x-2">
          <span :class="['px-2 py-0.5 rounded text-xs font-bold', methodClass(active.method)]">{{ active.method }}</span>
          <code class="text-sm font-mono text-gray-700 dark:text-gray-200 bg-gray-100 dark:bg-gray-800 px-2 py-1 rounded">{{ active.path }}</code>
        </div>

        <section class="mt-8">
          <h3 class="text-sm font-medium text-gray-900 dark:text-gray-100 mb-3 pb-2 border-b border-gray-200 dark:border-gray-700">📋 描述</h3>
          <p class="text-sm text-gray-600 dark:text-gray-300">{{ active.desc }}</p>
        </section>

        <section class="mt-6">
          <h3 class="text-sm font-medium text-gray-900 dark:text-gray-100 mb-3 pb-2 border-b border-gray-200 dark:border-gray-700">📥 请求参数</h3>
          <table class="w-full text-sm">
            <thead class="text-xs text-gray-500"><tr><th class="text-left py-2">名称</th><th class="text-left py-2">类型</th><th class="text-left py-2">必填</th><th class="text-left py-2">说明</th></tr></thead>
            <tbody class="text-gray-700 dark:text-gray-300">
              <tr class="border-t border-gray-100 dark:border-gray-800"><td class="py-2.5 font-mono text-xs">page</td><td class="py-2.5">integer</td><td class="py-2.5">否</td><td class="py-2.5">页码，从 1 开始</td></tr>
              <tr class="border-t border-gray-100 dark:border-gray-800"><td class="py-2.5 font-mono text-xs">page_size</td><td class="py-2.5">integer</td><td class="py-2.5">否</td><td class="py-2.5">每页条数，默认 20</td></tr>
              <tr class="border-t border-gray-100 dark:border-gray-800"><td class="py-2.5 font-mono text-xs">keyword</td><td class="py-2.5">string</td><td class="py-2.5">否</td><td class="py-2.5">搜索关键字</td></tr>
            </tbody>
          </table>
        </section>

        <section class="mt-6">
          <h3 class="text-sm font-medium text-gray-900 dark:text-gray-100 mb-3 pb-2 border-b border-gray-200 dark:border-gray-700">📤 响应示例</h3>
          <pre class="bg-gray-900 text-green-400 p-4 rounded-md text-xs overflow-x-auto"><code>{
  "code": 0,
  "message": "success",
  "data": {
    "items": [],
    "total": 0
  }
}</code></pre>
        </section>

        <section class="mt-6">
          <h3 class="text-sm font-medium text-gray-900 dark:text-gray-100 mb-3 pb-2 border-b border-gray-200 dark:border-gray-700">🔐 鉴权</h3>
          <p class="text-sm text-gray-600 dark:text-gray-300">
            使用 <code class="px-1.5 bg-gray-100 dark:bg-gray-800 rounded text-xs">Authorization: Bearer {token}</code> 请求头传递 Token
          </p>
        </section>

        <section class="mt-6">
          <h3 class="text-sm font-medium text-gray-900 dark:text-gray-100 mb-3 pb-2 border-b border-gray-200 dark:border-gray-700">🧪 试试它</h3>
          <button class="h-9 px-4 bg-primary text-white rounded-md text-sm hover:bg-primary-hover">执行请求</button>
        </section>
      </div>

      <div v-else class="flex h-full items-center justify-center text-gray-400 text-sm">请选择左侧 API 查看详情</div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { mockApiDocs } from '@/api/mock'

const search = ref('')
const active = ref(mockApiDocs[0])

const filteredApis = computed(() => {
  const kw = search.value.trim().toLowerCase()
  if (!kw) return mockApiDocs
  return mockApiDocs.filter(a => a.path.toLowerCase().includes(kw) || a.desc.toLowerCase().includes(kw))
})

const groupedApis = computed(() => {
  const map = {}
  filteredApis.value.forEach(a => {
    if (!map[a.group]) map[a.group] = []
    map[a.group].push(a)
  })
  return Object.keys(map).map(name => ({ name, items: map[name] }))
})

function methodClass(m) {
  return {
    GET:    'bg-blue-100 text-blue-700',
    POST:   'bg-green-100 text-green-700',
    PUT:    'bg-orange-100 text-orange-700',
    DELETE: 'bg-red-100 text-red-700'
  }[m] || 'bg-gray-100 text-gray-700'
}
</script>
