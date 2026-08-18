<template>
  <div class="flex h-full bg-white dark:bg-gray-900 transition-colors">
    <div class="flex-1 flex flex-col p-6 overflow-hidden">
      <div class="flex items-center justify-between mb-4 flex-shrink-0">
        <div class="flex items-center space-x-3">
          <h2 class="text-xl font-semibold text-gray-900 dark:text-gray-100">{{ headerTitle }}</h2>
          <div class="flex items-center bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-md overflow-hidden">
            <button @click="prev" class="w-8 h-8 hover:bg-gray-50 dark:hover:bg-gray-700 flex items-center justify-center">
              <ChevronLeftIcon class="w-4 h-4 text-gray-600 dark:text-gray-300" />
            </button>
            <button @click="next" class="w-8 h-8 hover:bg-gray-50 dark:hover:bg-gray-700 flex items-center justify-center border-l border-gray-200 dark:border-gray-700">
              <ChevronRightIcon class="w-4 h-4 text-gray-600 dark:text-gray-300" />
            </button>
          </div>
          <button @click="goToday" class="px-3 h-8 text-sm border border-gray-200 dark:border-gray-700 rounded-md hover:bg-gray-50 dark:hover:bg-gray-800 dark:text-gray-200 transition">今天</button>
          <button @click="openCreate" class="ml-2 px-3 h-8 text-sm bg-primary hover:bg-primary-hover text-white rounded-md transition flex items-center">
            <PlusIcon class="w-4 h-4 mr-1" />新建事件
          </button>
        </div>
        <div class="flex items-center bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-md text-sm overflow-hidden">
          <button v-for="v in views" :key="v.value" @click="currentView = v.value"
                  :class="['px-3 h-8 transition-colors', currentView === v.value ? 'bg-primary text-white' : 'hover:bg-gray-50 dark:hover:bg-gray-700 text-gray-700 dark:text-gray-200']">
            {{ v.label }}
          </button>
        </div>
      </div>

      <div v-if="loading" class="flex-1 flex items-center justify-center text-gray-400 text-sm">
        <svg class="animate-spin w-5 h-5 mr-2" viewBox="0 0 24 24" fill="none">
          <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="3" stroke-opacity="0.25" />
          <path d="M4 12a8 8 0 018-8v3a5 5 0 00-5 5H4z" fill="currentColor" />
        </svg>
        加载事件中…
      </div>

      <template v-else>
        <!-- 月视图 -->
        <div v-if="currentView === 'month'" class="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden flex-1 flex flex-col min-h-0">
          <div class="grid grid-cols-7 border-b border-gray-200 dark:border-gray-700 flex-shrink-0">
            <div v-for="d in weekDayLabels" :key="d" class="text-center text-xs font-medium text-gray-500 dark:text-gray-400 py-3">{{ d }}</div>
          </div>
          <div class="grid grid-cols-7 grid-rows-6 flex-1 min-h-0">
            <div v-for="(day, idx) in monthDays" :key="idx"
                 @click="selectDay(day)"
                 class="border-r border-b border-gray-100 dark:border-gray-800 p-2 cursor-pointer transition-colors min-h-0"
                 :class="[!day.inMonth ? 'bg-gray-50 dark:bg-gray-800/30 text-gray-400' : '',
                          day.isSelected ? 'ring-2 ring-primary ring-inset z-10' : '',
                          day.inMonth ? 'hover:bg-primary-50/50 dark:hover:bg-gray-800/50' : '']">
              <div class="text-sm font-medium">
                <span :class="day.isToday ? 'inline-flex items-center justify-center w-6 h-6 rounded-full bg-primary text-white' : ''">{{ day.date }}</span>
              </div>
              <div class="mt-1 space-y-0.5 overflow-hidden">
                <div v-for="ev in day.events.slice(0, 2)" :key="ev.id"
                     @click.stop="openEdit(ev)"
                     class="text-[10px] px-1.5 py-0.5 rounded truncate text-white cursor-pointer hover:opacity-90"
                     :style="{ background: ev.color }" :title="`${ev.start} ${ev.title}`">
                  {{ ev.start }} {{ ev.title }}
                </div>
                <div v-if="day.events.length > 2" class="text-[10px] text-gray-400 px-1.5">+{{ day.events.length - 2 }}</div>
              </div>
            </div>
          </div>
        </div>

        <!-- 周视图 -->
        <div v-else-if="currentView === 'week'" class="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden flex-1 flex flex-col min-h-0">
          <div class="grid grid-cols-8 border-b border-gray-200 dark:border-gray-700 flex-shrink-0 sticky top-0 bg-gray-50 dark:bg-gray-800 z-10">
            <div class="py-2 text-xs text-center text-gray-500 dark:text-gray-400"></div>
            <div v-for="d in weekDays" :key="d.iso" :class="['py-2 text-center border-l border-gray-200 dark:border-gray-700', d.isToday ? 'bg-primary-50 dark:bg-primary/20' : '']">
              <div class="text-xs text-gray-500 dark:text-gray-400">{{ d.weekday }}</div>
              <div class="text-sm font-medium" :class="d.isToday ? 'text-primary' : 'text-gray-900 dark:text-gray-100'">{{ d.date }}</div>
            </div>
          </div>
          <div class="flex-1 overflow-y-auto">
            <div class="grid grid-cols-8 relative" style="min-height: 1536px;">
              <div class="col-span-1 border-r border-gray-100 dark:border-gray-800">
                <div v-for="h in 24" :key="h" class="h-16 border-b border-gray-100 dark:border-gray-800 text-right pr-2 text-xs text-gray-400 pt-1">{{ h - 1 }}:00</div>
              </div>
              <div v-for="d in weekDays" :key="d.iso + 'col'"
                   :class="['col-span-1 border-r border-gray-100 dark:border-gray-800 relative', d.isToday ? 'bg-primary-50/30 dark:bg-primary/10' : '']">
                <div v-for="h in 24" :key="h" class="h-16 border-b border-gray-100 dark:border-gray-800"></div>
                <div v-for="ev in d.events" :key="ev.id"
                     @click="openEdit(ev)"
                     class="absolute left-1 right-1 rounded px-1.5 py-1 text-xs text-white overflow-hidden cursor-pointer hover:opacity-90 transition"
                     :style="[eventStyle(ev), { background: ev.color }]">
                  <div class="font-medium truncate">{{ ev.title }}</div>
                  <div class="text-[10px] opacity-80">{{ ev.start }} - {{ ev.end }}</div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- 日视图 -->
        <div v-else class="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden flex-1 flex flex-col min-h-0">
          <div class="px-6 py-3 border-b border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800 text-center text-sm font-medium text-gray-700 dark:text-gray-200">
            {{ headerTitle }}
          </div>
          <div class="flex-1 overflow-y-auto">
            <div class="grid grid-cols-[60px_1fr] relative" style="min-height: 1536px;">
              <div class="border-r border-gray-100 dark:border-gray-800">
                <div v-for="h in 24" :key="h" class="h-16 border-b border-gray-100 dark:border-gray-800 text-right pr-2 text-xs text-gray-400 pt-1">{{ h - 1 }}:00</div>
              </div>
              <div class="relative border-r border-gray-100 dark:border-gray-800">
                <div v-for="h in 24" :key="h" class="h-16 border-b border-gray-100 dark:border-gray-800"></div>
                <div v-for="ev in dayEvents" :key="ev.id"
                     @click="openEdit(ev)"
                     class="absolute left-2 right-4 rounded px-2 py-1 text-sm text-white overflow-hidden cursor-pointer hover:opacity-90 transition shadow"
                     :style="[eventStyle(ev), { background: ev.color }]">
                  <div class="font-medium">{{ ev.title }}</div>
                  <div class="text-xs opacity-80">{{ ev.start }} - {{ ev.end }}</div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </template>
    </div>

    <!-- 右侧详情（月视图） -->
    <div v-if="currentView === 'month' && !loading" class="w-80 border-l border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-[#1A1D23] p-5 overflow-y-auto flex-shrink-0">
      <div class="text-center mb-5">
        <div class="text-xs text-gray-500 dark:text-gray-400">{{ currentDate.year() }} 年</div>
        <div class="text-2xl font-semibold text-gray-900 dark:text-gray-100 mt-1">{{ currentDate.month() + 1 }} 月 {{ selectedDay }} 日</div>
      </div>
      <h3 class="text-sm font-medium text-gray-900 dark:text-gray-100 mb-3 flex items-center justify-between">
        <span>日程 ({{ selectedEvents.length }})</span>
        <button @click="openCreateForSelected" class="text-xs text-primary hover:underline">+ 新建</button>
      </h3>
      <div v-if="selectedEvents.length === 0" class="text-center py-8 text-sm text-gray-400">当天没有安排</div>
      <div v-else class="space-y-2">
        <div v-for="ev in selectedEvents" :key="ev.id"
             class="bg-white dark:bg-gray-800 rounded-lg p-3 border-l-4 shadow-sm hover:shadow-md transition cursor-pointer"
             :style="{ borderLeftColor: ev.color }"
             @click="openEdit(ev)">
          <div class="font-medium text-sm text-gray-900 dark:text-gray-100">{{ ev.title }}</div>
          <div class="text-xs text-gray-500 mt-1 flex items-center">
            <ClockIcon class="w-3 h-3 mr-1" />{{ ev.start }} - {{ ev.end }}
          </div>
          <div v-if="ev.location" class="text-xs text-gray-500 mt-0.5">📍 {{ ev.location }}</div>
        </div>
      </div>
    </div>

    <!-- 新建/编辑弹窗 -->
    <transition
      enter-active-class="transition duration-150"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100">
      <div v-if="editing" class="fixed inset-0 z-50 bg-black/30 flex items-center justify-center" @click.self="cancelEdit">
        <div class="bg-white dark:bg-gray-900 rounded-lg shadow-xl w-[480px] p-6">
          <h3 class="text-base font-medium text-gray-900 dark:text-gray-100 mb-4">
            {{ editing.id ? '编辑事件' : '新建事件' }}
          </h3>
          <div class="space-y-3">
            <div>
              <label class="block text-xs text-gray-600 dark:text-gray-400 mb-1">标题 *</label>
              <input v-model="editing.title" class="w-full h-9 px-3 text-sm border border-gray-300 dark:border-gray-700 dark:bg-gray-800 dark:text-gray-100 rounded outline-none focus:border-primary" />
            </div>
            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="block text-xs text-gray-600 dark:text-gray-400 mb-1">开始 *</label>
                <input v-model="editing.startDateTime" type="datetime-local" class="w-full h-9 px-2 text-sm border border-gray-300 dark:border-gray-700 dark:bg-gray-800 dark:text-gray-100 rounded outline-none focus:border-primary" />
              </div>
              <div>
                <label class="block text-xs text-gray-600 dark:text-gray-400 mb-1">结束 *</label>
                <input v-model="editing.endDateTime" type="datetime-local" class="w-full h-9 px-2 text-sm border border-gray-300 dark:border-gray-700 dark:bg-gray-800 dark:text-gray-100 rounded outline-none focus:border-primary" />
              </div>
            </div>
            <div>
              <label class="block text-xs text-gray-600 dark:text-gray-400 mb-1">地点</label>
              <input v-model="editing.location" class="w-full h-9 px-3 text-sm border border-gray-300 dark:border-gray-700 dark:bg-gray-800 dark:text-gray-100 rounded outline-none focus:border-primary" />
            </div>
            <div>
              <label class="block text-xs text-gray-600 dark:text-gray-400 mb-1">备注</label>
              <textarea v-model="editing.description" rows="2" class="w-full px-3 py-2 text-sm border border-gray-300 dark:border-gray-700 dark:bg-gray-800 dark:text-gray-100 rounded outline-none focus:border-primary resize-none" />
            </div>
          </div>
          <div class="mt-5 flex justify-between">
            <button v-if="editing.id" @click="doDelete" class="text-sm text-red-500 hover:underline">删除</button>
            <div class="ml-auto space-x-2">
              <button @click="cancelEdit" class="h-8 px-4 text-sm border border-gray-200 dark:border-gray-700 rounded hover:bg-gray-50 dark:hover:bg-gray-800 dark:text-gray-200">取消</button>
              <button @click="saveEdit" :disabled="saving" class="h-8 px-4 text-sm bg-primary text-white rounded hover:bg-primary-hover disabled:opacity-60 transition">
                {{ saving ? '保存中…' : '保存' }}
              </button>
            </div>
          </div>
        </div>
      </div>
    </transition>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import dayjs from '@/utils/dayjs'
import { listEvents, createEvent, updateEvent, deleteEvent, adaptEvent } from '@/api/calendar'
import { ElMessage } from '@/api/toast'
import { ChevronLeftIcon, ChevronRightIcon, PlusIcon, ClockIcon } from '@heroicons/vue/24/outline'

const loading = ref(true)
const events = ref([])
const currentView = ref('month')
const currentDate = ref(dayjs())
const selectedDay = ref(dayjs().date())
const editing = ref(null)
const saving = ref(false)

const views = [{ label: '月', value: 'month' }, { label: '周', value: 'week' }, { label: '日', value: 'day' }]
const weekDayLabels = ['周日', '周一', '周二', '周三', '周四', '周五', '周六']

const eventsByDate = computed(() => {
  const map = {}
  events.value.forEach(ev => {
    if (!map[ev.date]) map[ev.date] = []
    map[ev.date].push(ev)
  })
  return map
})

const monthDays = computed(() => {
  const monthStart = currentDate.value.startOf('month')
  const gridStart = monthStart.startOf('week')
  const days = []
  for (let i = 0; i < 42; i++) {
    const d = gridStart.add(i, 'day')
    const iso = d.format('YYYY-MM-DD')
    days.push({
      date: d.date(),
      inMonth: d.month() === currentDate.value.month(),
      isToday: d.isSame(dayjs(), 'day'),
      isSelected: d.isSame(currentDate.value.year(currentDate.value.year()).month(currentDate.value.month()).date(selectedDay.value), 'day'),
      events: eventsByDate.value[iso] || []
    })
  }
  return days
})

const selectedDayKey = computed(() => currentDate.value.month(currentDate.value.month()).date(selectedDay.value).format('YYYY-MM-DD'))
const selectedEvents = computed(() => eventsByDate.value[selectedDayKey.value] || [])

const weekDays = computed(() => {
  const start = currentDate.value.startOf('week')
  const arr = []
  for (let i = 0; i < 7; i++) {
    const d = start.add(i, 'day')
    arr.push({
      iso: d.format('YYYY-MM-DD'),
      weekday: ['周日', '周一', '周二', '周三', '周四', '周五', '周六'][d.day()],
      date: d.date(),
      isToday: d.isSame(dayjs(), 'day'),
      events: eventsByDate.value[d.format('YYYY-MM-DD')] || []
    })
  }
  return arr
})

const dayEvents = computed(() => eventsByDate.value[currentDate.value.format('YYYY-MM-DD')] || [])

const headerTitle = computed(() => {
  if (currentView.value === 'month') return `${currentDate.value.year()} 年 ${currentDate.value.month() + 1} 月`
  if (currentView.value === 'week') {
    const s = currentDate.value.startOf('week')
    const e = s.add(6, 'day')
    return `${s.format('MM 月 DD 日')} - ${e.format('MM 月 DD 日')}`
  }
  return currentDate.value.format('YYYY 年 MM 月 DD 日')
})

function prev() {
  if (currentView.value === 'month') currentDate.value = currentDate.value.subtract(1, 'month')
  else if (currentView.value === 'week') currentDate.value = currentDate.value.subtract(7, 'day')
  else currentDate.value = currentDate.value.subtract(1, 'day')
}
function next() {
  if (currentView.value === 'month') currentDate.value = currentDate.value.add(1, 'month')
  else if (currentView.value === 'week') currentDate.value = currentDate.value.add(7, 'day')
  else currentDate.value = currentDate.value.add(1, 'day')
}
function goToday() {
  currentDate.value = dayjs()
  selectedDay.value = dayjs().date()
}
function selectDay(day) {
  if (day.inMonth) {
    selectedDay.value = day.date
  } else {
    if (day.date > 20) currentDate.value = currentDate.value.add(1, 'month')
    else currentDate.value = currentDate.value.subtract(1, 'month')
    selectedDay.value = day.date
  }
}
function eventStyle(ev) {
  const [sh, sm] = ev.start.split(':').map(Number)
  const [eh, em] = ev.end.split(':').map(Number)
  return {
    top: `${(sh + sm / 60) * 64}px`,
    height: `${Math.max(28, (eh + em / 60 - sh - sm / 60) * 64)}px`
  }
}

// CRUD
function openCreate() { openCreateForSelected() }
function openCreateForSelected() {
  const date = currentDate.value.month(currentDate.value.month()).date(selectedDay.value)
  editing.value = {
    id: null,
    title: '',
    startDateTime: date.hour(10).minute(0).second(0).format('YYYY-MM-DDTHH:mm'),
    endDateTime: date.hour(11).minute(0).second(0).format('YYYY-MM-DDTHH:mm'),
    location: '',
    description: ''
  }
}
function openEdit(ev) {
  editing.value = {
    id: ev.id,
    title: ev.title,
    startDateTime: dayjs(ev.startFull).format('YYYY-MM-DDTHH:mm'),
    endDateTime: dayjs(ev.endFull).format('YYYY-MM-DDTHH:mm'),
    location: ev.location,
    description: ev.description
  }
}
function cancelEdit() { editing.value = null }

async function saveEdit() {
  const e = editing.value
  if (!e.title) return ElMessage({ message: '请输入标题', type: 'warning' })
  saving.value = true
  try {
    const payload = {
      title: e.title,
      startTime: dayjs(e.startDateTime).toISOString(),
      endTime: dayjs(e.endDateTime).toISOString(),
      location: e.location || null,
      description: e.description || null
    }
    if (e.id) {
      const updated = await updateEvent(e.id, payload)
      ElMessage({ message: '已更新', type: 'success' })
      const idx = events.value.findIndex(x => x.id === e.id)
      if (idx > -1) events.value.splice(idx, 1, adaptEvent(updated))
    } else {
      const created = await createEvent(payload)
      ElMessage({ message: '已创建', type: 'success' })
      events.value.push(adaptEvent(created))
    }
    editing.value = null
  } catch (err) {
    ElMessage({ message: '保存失败：' + (err.message || ''), type: 'error' })
  } finally {
    saving.value = false
  }
}

async function doDelete() {
  if (!editing.value?.id) return
  if (!confirm('确认删除该事件？')) return
  try {
    await deleteEvent(editing.value.id)
    events.value = events.value.filter(x => x.id !== editing.value.id)
    ElMessage({ message: '已删除', type: 'success' })
    editing.value = null
  } catch (e) {
    ElMessage({ message: '删除失败', type: 'error' })
  }
}

async function loadEvents() {
  loading.value = true
  try {
    // 加载当前可视范围（前后各 1 个月以防越界）
    const start = currentDate.value.startOf('month').subtract(7, 'day').toISOString()
    const end = currentDate.value.endOf('month').add(7, 'day').toISOString()
    const list = await listEvents(start, end)
    events.value = (list || []).map(adaptEvent)
  } catch (e) {
    ElMessage({ message: '加载事件失败', type: 'error' })
  } finally {
    loading.value = false
  }
}

onMounted(loadEvents)
</script>