<template>
  <view class="calendar-container">
    <!-- 日期导航 -->
    <view class="date-nav">
      <text class="nav-btn" @tap="goToday">今天</text>
      <text class="nav-btn" @tap="goWeek(-1)">‹ 上一周</text>
      <text class="nav-title">{{ weekTitle }}</text>
      <text class="nav-btn" @tap="goWeek(1)">下一周 ›</text>
      <picker mode="date" :value="selectedDate" @change="onJumpDate">
        <text class="nav-btn jump-btn">📅 跳转</text>
      </picker>
      <text class="add-btn" @tap="openCreate">+ 新增日程</text>
      <text class="freebusy-btn" @tap="openFreeBusy">⏳ 空闲</text>
    </view>

    <!-- 日期快速选择 -->
    <scroll-view class="date-scroll" scroll-x show-scrollbar="false">
      <view
        v-for="(d, i) in weekDays"
        :key="i"
        class="date-chip"
        :class="{ active: d.date === selectedDate }"
        @tap="selectDate(d.date)"
      >
        <text class="chip-weekday">{{ d.weekday }}</text>
        <text class="chip-date">{{ d.day }}</text>
      </view>
    </scroll-view>

    <!-- 日程列表 -->
    <view class="event-section">
      <text class="section-date">{{ formatSelectedDate }}</text>
      <view v-if="events.length" class="event-list">
        <view v-for="evt in events" :key="evt.id" class="event-item">
          <view class="event-time">
            <text class="event-start">{{ formatTime(evt.startTime) }}</text>
            <text class="event-end">{{ formatTime(evt.endTime) }}</text>
          </view>
          <view class="event-info">
            <text class="event-title">{{ evt.title }}</text>
            <text v-if="evt.location" class="event-location">📍 {{ evt.location }}</text>
            <text v-if="evt.recurrenceType && evt.recurrenceType !== 'None'" class="event-recurrence">
              🔄 {{ recurrenceLabel(evt.recurrenceType) }}
            </text>
            <view v-if="evt.attendees?.length" class="event-attendees">
              <text v-for="a in evt.attendees" :key="a.userId" class="event-attendee">
                {{ a.userName }}<text v-if="a.status === 'Accepted'" class="att-status accepted">✓</text>
                <text v-else-if="a.status === 'Declined'" class="att-status declined">✕</text>
              </text>
            </view>
          </view>
          <view class="event-actions">
            <!-- 被别人邀请时显示出席按钮 -->
            <text v-if="evt.userId !== authStore.userInfo?.id && evt.attendees?.some((a:any) => a.userId === authStore.userInfo?.id && a.status === 'Pending')" class="event-attend-btn" @tap.stop="showAttendance(evt)">出席</text>
            <text v-if="evt.recurrenceType && evt.recurrenceType !== 'None'" class="event-occur-btn" @tap.stop="showOccurrences(evt)">📆 展开</text>
            <text class="event-edit" @tap.stop="openEdit(evt)">编辑</text>
            <text class="event-delete" @tap.stop="confirmDelete(evt)">删除</text>
          </view>
        </view>
      </view>
      <view v-else class="event-empty">暂无日程</view>
    </view>

    <!-- 新增/编辑弹窗 -->
    <view v-if="showFormModal" class="modal-overlay" @tap="showFormModal = false">
      <view class="modal-popup" @tap.stop>
        <text class="modal-title">{{ isEditing ? '编辑日程' : '新增日程' }}</text>
        <view class="form-group">
          <text class="form-label">日程标题 *</text>
          <input v-model="form.title" class="form-input" placeholder="请输入标题" />
        </view>
        <view class="form-group">
          <text class="form-label">开始时间 *</text>
          <picker mode="date" :value="form.startDate" @change="onStartDateChange">
            <view class="form-input">{{ form.startDate || '选择日期' }}</view>
          </picker>
          <picker mode="time" :value="form.startTimeVal" @change="onStartTimeChange">
            <view class="form-input">{{ form.startTimeVal || '选择时间' }}</view>
          </picker>
        </view>
        <view class="form-group">
          <text class="form-label">结束时间 *</text>
          <picker mode="date" :value="form.endDate" @change="onEndDateChange">
            <view class="form-input">{{ form.endDate || '选择日期' }}</view>
          </picker>
          <picker mode="time" :value="form.endTimeVal" @change="onEndTimeChange">
            <view class="form-input">{{ form.endTimeVal || '选择时间' }}</view>
          </picker>
        </view>
        <view class="form-group">
          <text class="form-label">地点</text>
          <input v-model="form.location" class="form-input" placeholder="可选" />
        </view>
        <view class="form-group">
          <text class="form-label">描述</text>
          <input v-model="form.description" class="form-input" placeholder="可选" />
        </view>
        <view class="form-group">
          <text class="form-label">重复</text>
          <picker :value="recurrenceIdx" :range="recurrenceOptions" @change="onRecurrenceChange">
            <view class="form-input">{{ recurrenceOptions[recurrenceIdx] }}</view>
          </picker>
        </view>
        <view class="form-group" v-if="form.recurrenceType">
          <text class="form-label">结束重复</text>
          <picker mode="date" :value="form.recurrenceUntil" @change="onRecurrenceUntilChange">
            <view class="form-input">{{ form.recurrenceUntil || '选择结束日期' }}</view>
          </picker>
        </view>
        <view class="form-group">
          <text class="form-label">参会人</text>
          <view class="attendee-chips" v-if="selectedAttendees.length">
            <text v-for="(a, i) in selectedAttendees" :key="a.id" class="attendee-chip">
              {{ a.realName || a.username }}
              <text class="attendee-chip-remove" @tap="removeAttendee(i)">✕</text>
            </text>
          </view>
          <text class="form-add-btn" @tap="openAttendeePicker">
            {{ selectedAttendees.length > 0 ? '+ 添加更多' : '+ 添加参会人' }}
          </text>
        </view>
        <view class="modal-btns">
          <button class="modal-cancel" @tap="showFormModal = false">取消</button>
          <button class="modal-confirm" :disabled="!form.title || eventSubmitting" @tap="saveEvent">保存</button>
        </view>
      </view>
    </view>

    <!-- 出席状态操作（非自己创建的日程） -->
    <view v-if="pendingAttendance" class="modal-overlay" @tap="pendingAttendance = null">
      <view class="modal-popup" @tap.stop>
        <text class="modal-title">出席确认</text>
        <text class="attendance-prompt">是否参加日程「{{ pendingAttendance?.title }}」？</text>
        <view class="modal-btns attendance-btns">
          <button class="modal-cancel" @tap="doAttendance('Declined')">拒绝</button>
          <button class="modal-confirm" @tap="doAttendance('Accepted')">接受</button>
        </view>
      </view>
    </view>

    <!-- 参会人选择器 -->
    <view v-if="showAttendeePicker" class="modal-overlay" @tap="showAttendeePicker = false">
      <view class="modal-popup" @tap.stop>
        <text class="modal-title">选择参会人</text>
        <input v-model="attendeeSearch" class="form-input" placeholder="搜索联系人..." />
        <scroll-view class="attendee-scroll" scroll-y>
          <view
            v-for="c in attendeeCandidates"
            :key="c.id"
            class="attendee-row"
            @tap="toggleAttendee(c)"
          >
            <view class="member-avatar-sm" :style="{ backgroundColor: getColor(c.id) }">
              <text class="avatar-sm-text">{{ (c.realName || c.username)[0] }}</text>
            </view>
            <text class="attendee-row-name">{{ c.realName || c.username }}</text>
            <view class="member-check" :class="{ checked: attendeeSelectedIds.has(c.id) }">
              <text v-if="attendeeSelectedIds.has(c.id)" class="check-mark">✓</text>
            </view>
          </view>
          <view v-if="attendeeCandidates.length === 0" class="attendee-empty">无匹配联系人</view>
        </scroll-view>
        <view class="modal-btns">
          <button class="modal-cancel" @tap="showAttendeePicker = false">取消</button>
          <button class="modal-confirm" @tap="confirmAttendees">确定 ({{ attendeeSelectedIds.size }})</button>
        </view>
      </view>
    </view>

    <!-- 删除确认 -->
    <view v-if="showDeleteModal" class="modal-overlay" @tap="showDeleteModal = false">
      <view class="modal-popup delete-popup" @tap.stop>
        <text class="delete-title">确认删除</text>
        <text class="delete-text">确定要删除日程「{{ deleteTarget?.title }}」吗？</text>
        <view class="modal-btns">
          <button class="modal-cancel" @tap="showDeleteModal = false">取消</button>
          <button class="modal-danger" @tap="doDelete">确定删除</button>
        </view>
      </view>
    </view>

    <!-- 重复日程展开弹窗 -->
    <view v-if="showOccurModal" class="modal-overlay" @tap="showOccurModal = false">
      <view class="modal-popup" @tap.stop>
        <text class="modal-title">📆 重复日程</text>
        <text class="occur-event-title">{{ occurEvent?.title }}</text>
        <text class="occur-recurrence">重复类型：{{ recurrenceLabel(occurEvent?.recurrenceType) }}</text>
        <scroll-view scroll-y class="occur-list">
          <view v-for="(o, i) in occurrences" :key="i" class="occur-item">
            <text class="occur-date">{{ formatOccurDate(o) }}</text>
            <text class="occur-time">{{ formatTime(o) }} - {{ formatTime(occurEvent?.endTime) }}</text>
          </view>
          <view v-if="!occurrences.length" class="occur-empty">暂无数据</view>
        </scroll-view>
        <button class="modal-close" @tap="showOccurModal = false">关闭</button>
      </view>
    </view>

    <!-- 空闲时间查询弹窗 -->
    <view v-if="showFreeBusyModal" class="modal-overlay" @tap="showFreeBusyModal = false">
      <view class="modal-popup fb-popup" @tap.stop>
        <text class="modal-title">⏳ 空闲时间查询</text>
        <view class="fb-date-row">
          <picker mode="date" :value="fbForm.from" @change="onFBFromChange">
            <view class="fb-date-picker">{{ fbForm.from || '开始日期' }}</view>
          </picker>
          <text class="fb-date-sep">→</text>
          <picker mode="date" :value="fbForm.to" @change="onFBToChange">
            <view class="fb-date-picker">{{ fbForm.to || '结束日期' }}</view>
          </picker>
        </view>

        <!-- 筛选栏 -->
        <view class="fb-filter-bar">
          <text class="fb-filter-tab" :class="{ active: fbFilterMode === 'all' }" @tap="fbFilterMode = 'all'">全部 ({{ allContacts.length }})</text>
          <text class="fb-filter-tab" :class="{ active: fbFilterMode === 'free' }" @tap="fbFilterMode = 'free'">🟢 空闲</text>
          <text class="fb-filter-tab" :class="{ active: fbFilterMode === 'busy' }" @tap="fbFilterMode = 'busy'">🔴 忙碌</text>
        </view>

        <!-- 加载状态 -->
        <view v-if="fbLoading" class="fb-loading">
          <text class="fb-loading-text">⏳ 正在查询空闲状态...</text>
        </view>

        <!-- 成员状态列表 -->
        <scroll-view v-else scroll-y class="fb-list">
          <view v-for="m in fbDisplayMembers" :key="m.id" class="fb-member-row">
            <view class="member-avatar-sm" :style="{ backgroundColor: getColor(m.id) }">
              <text class="avatar-sm-text">{{ (m.realName || m.username || '?')[0] }}</text>
            </view>
            <text class="fb-member-name">{{ m.realName || m.username }}</text>
            <text class="fb-member-status" :class="fbMemberStatus(m.id)">
              {{ fbMemberLabel(m.id) }}
            </text>
            <text v-if="fbMemberStatus(m.id) === 'busy' && fbMemberEvent(m.id)" class="fb-member-event">{{ fbMemberEvent(m.id) }}</text>
          </view>
          <view v-if="!fbDisplayMembers.length" class="fb-empty-list">
            <text>无匹配成员</text>
          </view>
        </scroll-view>

        <button class="modal-close" @tap="showFreeBusyModal = false">关闭</button>
      </view>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import { getEvents, createEvent, updateEvent, deleteEvent, updateAttendance, getOccurrences, getFreeBusy } from '@/api/calendar'
import { getFriends } from '@/api/contacts'
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()

const today = new Date()
const currentWeekStart = ref(getWeekStart(today))
const selectedDate = ref(formatDateStr(today))

const events = ref<any[]>([])
const showFormModal = ref(false)
const isEditing = ref(false)
const editingId = ref('')
const showDeleteModal = ref(false)
const deleteTarget = ref<any>(null)

// 重复日程
const recurrenceOptions = ['不重复', '每天', '每周', '每月']
const recurrenceValues = ['None', 'Daily', 'Weekly', 'Monthly']
const recurrenceIdx = ref(0)

// 参会人
const showAttendeePicker = ref(false)
const attendeeSearch = ref('')
const allContacts = ref<any[]>([])
const selectedAttendees = ref<any[]>([])
const attendeeSelectedIds = ref<Set<string>>(new Set())

// 出席确认
const pendingAttendance = ref<any>(null)

// ---- 重复日程展开 ----
const showOccurModal = ref(false)
const occurEvent = ref<any>(null)
const occurrences = ref<any[]>([])

// ---- 空闲时间查询 ----
const showFreeBusyModal = ref(false)
const fbForm = ref({ from: '', to: '' })
const fbUserIds = ref<Set<string>>(new Set())
const fbResults = ref<any[]>([])
const fbLoading = ref(false)

const attendeeCandidates = computed(() => {
  let list = allContacts.value.filter((c: any) => c.id !== authStore.userInfo?.id)
  if (attendeeSearch.value.trim()) {
    const kw = attendeeSearch.value.trim().toLowerCase()
    list = list.filter((c: any) => (c.realName || c.username || '').toLowerCase().includes(kw))
  }
  return list
})

const form = ref({
  title: '',
  startDate: '',
  startTimeVal: '09:00',
  endDate: '',
  endTimeVal: '10:00',
  location: '',
  description: '',
  recurrenceType: '',
  recurrenceUntil: '',
})

// 计算周几
const weekDays = computed(() => {
  const days: Array<{ date: string; weekday: string; day: string }> = []
  const start = new Date(currentWeekStart.value)
  const weekdays = ['日', '一', '二', '三', '四', '五', '六']
  for (let i = 0; i < 7; i++) {
    const d = new Date(start)
    d.setDate(start.getDate() + i)
    days.push({
      date: formatDateStr(d),
      weekday: weekdays[d.getDay()],
      day: String(d.getDate()).padStart(2, '0'),
    })
  }
  return days
})

const weekTitle = computed(() => {
  const start = new Date(currentWeekStart.value)
  const end = new Date(start)
  end.setDate(start.getDate() + 6)
  return `${start.getMonth() + 1}/${start.getDate()} - ${end.getMonth() + 1}/${end.getDate()}`
})

const formatSelectedDate = computed(() => {
  const d = new Date(selectedDate.value)
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
})

function getWeekStart(d: Date): string {
  const start = new Date(d)
  const day = start.getDay()
  start.setDate(start.getDate() - day)
  return formatDateStr(start)
}

function formatDateStr(d: Date): string {
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
}

function goToday() {
  const d = new Date()
  currentWeekStart.value = getWeekStart(d)
  selectedDate.value = formatDateStr(d)
  loadEvents()
}

function goWeek(delta: number) {
  const start = new Date(currentWeekStart.value)
  start.setDate(start.getDate() + delta * 7)
  currentWeekStart.value = formatDateStr(start)
  loadEvents()
}

function selectDate(date: string) {
  selectedDate.value = date
  loadEvents()
}

/** 日期跳转 */
function onJumpDate(e: any) {
  const val = e.detail?.value
  if (!val) return
  selectedDate.value = val
  // 将选中日期所在周设为当前周
  const d = new Date(val)
  currentWeekStart.value = getWeekStart(d)
  loadEvents()
}

async function loadEvents() {
  try {
    const start = new Date(currentWeekStart.value)
    const end = new Date(start)
    end.setDate(start.getDate() + 6)
    const params = {
      from: formatDateStr(start),
      to: formatDateStr(end),
    }
    const res: any = await getEvents(params)
    let list = Array.isArray(res) ? res : res?.items || res?.list || []
    // 显示自己的 + 被邀请的日程
    const myId = authStore.userInfo?.id || ''
    // 按选中日期过滤
    events.value = list.filter((e: any) => {
      const eDate = formatDateStr(new Date(e.startTime))
      return eDate === selectedDate.value
    })
  } catch {
    events.value = []
  }
}

function openCreate() {
  isEditing.value = false
  editingId.value = ''
  recurrenceIdx.value = 0
  selectedAttendees.value = []
  attendeeSelectedIds.value = new Set()
  form.value = {
    title: '',
    startDate: selectedDate.value,
    startTimeVal: '09:00',
    endDate: selectedDate.value,
    endTimeVal: '10:00',
    location: '',
    description: '',
    recurrenceType: '',
    recurrenceUntil: '',
  }
  // 加载联系人
  getFriends().then((res: any) => { allContacts.value = Array.isArray(res) ? res : [] }).catch(() => {})
  showFormModal.value = true
}

function openEdit(evt: any) {
  isEditing.value = true
  editingId.value = evt.id
  recurrenceIdx.value = recurrenceValues.indexOf(evt.recurrenceType || 'None')
  if (recurrenceIdx.value < 0) recurrenceIdx.value = 0
  selectedAttendees.value = evt.attendees?.map((a: any) => ({ id: a.userId, realName: a.userName })) || []
  attendeeSelectedIds.value = new Set(selectedAttendees.value.map((a: any) => a.id))
  form.value = {
    title: evt.title || '',
    startDate: formatDateStr(new Date(evt.startTime)),
    startTimeVal: formatTime(evt.startTime),
    endDate: formatDateStr(new Date(evt.endTime)),
    endTimeVal: formatTime(evt.endTime),
    location: evt.location || '',
    description: evt.description || '',
    recurrenceType: evt.recurrenceType || '',
    recurrenceUntil: evt.recurrenceUntil ? formatDateStr(new Date(evt.recurrenceUntil)) : '',
  }
  // 加载联系人
  getFriends().then((res: any) => { allContacts.value = Array.isArray(res) ? res : [] }).catch(() => {})
  showFormModal.value = true
}

const eventSubmitting = ref(false)

async function saveEvent() {
  if (!form.value.title.trim() || eventSubmitting.value) return
  // 客户端校验：结束时间不能早于开始时间
  const start = new Date(`${form.value.startDate}T${form.value.startTimeVal}`)
  const end = new Date(`${form.value.endDate}T${form.value.endTimeVal}`)
  if (end <= start) {
    uni.showToast({ title: '结束时间必须在开始时间之后', icon: 'none' })
    return
  }
  eventSubmitting.value = true
  const data: any = {
    title: form.value.title,
    startTime: `${form.value.startDate}T${form.value.startTimeVal}:00+08:00`,
    endTime: `${form.value.endDate}T${form.value.endTimeVal}:00+08:00`,
    location: form.value.location,
    description: form.value.description,
  }
  // 重复
  if (form.value.recurrenceType && form.value.recurrenceType !== 'None') {
    data.recurrenceType = form.value.recurrenceType
    if (form.value.recurrenceUntil) {
      data.recurrenceUntil = `${form.value.recurrenceUntil}T23:59:00+08:00`
    }
  }
  // 参会人
  if (selectedAttendees.value.length > 0) {
    data.attendeeUserIds = selectedAttendees.value.map((a: any) => a.id)
  }
  try {
    if (isEditing.value && editingId.value) {
      await updateEvent(editingId.value, data)
    } else {
      await createEvent(data)
    }
    uni.showToast({ title: '保存成功', icon: 'success' })
    showFormModal.value = false
    loadEvents()
  } catch {
    uni.showToast({ title: '保存失败', icon: 'none' })
  } finally {
    eventSubmitting.value = false
  }
}

function confirmDelete(evt: any) {
  deleteTarget.value = evt
  showDeleteModal.value = true
}

async function doDelete() {
  if (!deleteTarget.value) return
  try {
    await deleteEvent(deleteTarget.value.id)
    uni.showToast({ title: '删除成功', icon: 'success' })
    showDeleteModal.value = false
    deleteTarget.value = null
    loadEvents()
  } catch {
    uni.showToast({ title: '删除失败', icon: 'none' })
  }
}

function formatTime(t: string) {
  if (!t) return ''
  const d = new Date(t)
  return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`
}

// 重复选择
function onRecurrenceChange(e: any) {
  recurrenceIdx.value = e.detail.value
  form.value.recurrenceType = recurrenceValues[e.detail.value]
}
function onRecurrenceUntilChange(e: any) {
  form.value.recurrenceUntil = e.detail.value
}
function recurrenceLabel(type: string): string {
  const map: Record<string, string> = { Daily: '每天', Weekly: '每周', Monthly: '每月' }
  return map[type] || type
}

// 参会人选择
function openAttendeePicker() {
  attendeeSearch.value = ''
  attendeeSelectedIds.value = new Set(selectedAttendees.value.map((a: any) => a.id))
  showAttendeePicker.value = true
}
function toggleAttendee(c: any) {
  if (attendeeSelectedIds.value.has(c.id)) attendeeSelectedIds.value.delete(c.id)
  else attendeeSelectedIds.value.add(c.id)
  attendeeSelectedIds.value = new Set(attendeeSelectedIds.value)
}
function confirmAttendees() {
  selectedAttendees.value = allContacts.value.filter((c: any) => attendeeSelectedIds.value.has(c.id))
  showAttendeePicker.value = false
}
function removeAttendee(i: number) {
  selectedAttendees.value.splice(i, 1)
}

// 出席状态
function showAttendance(evt: any) {
  pendingAttendance.value = evt
}
async function doAttendance(status: 'Accepted' | 'Declined') {
  if (!pendingAttendance.value) return
  try {
    await updateAttendance(pendingAttendance.value.id, status)
    uni.showToast({ title: status === 'Accepted' ? '已接受' : '已拒绝', icon: 'success' })
    pendingAttendance.value = null
    loadEvents()
  } catch { uni.showToast({ title: '操作失败', icon: 'none' }) }
}

/** 重复日程展开 */
async function showOccurrences(evt: any) {
  occurEvent.value = evt
  occurrences.value = []
  showOccurModal.value = true
  try {
    const res: any = await getOccurrences(evt.id)
    occurrences.value = Array.isArray(res) ? res : []
  } catch {
    occurrences.value = []
  }
}

function formatOccurDate(dateStr: string) {
  if (!dateStr) return ''
  const d = new Date(dateStr)
  const weekdays = ['日', '一', '二', '三', '四', '五', '六']
  return `${d.getFullYear()}/${d.getMonth()+1}/${d.getDate()} 周${weekdays[d.getDay()]}`
}

/** 空闲时间查询 — 用 getFriends 加载联系人列表 */
async function loadContactsForFreeBusy() {
  fbLoading.value = true
  try {
    const res: any = await getFriends()
    const list = Array.isArray(res) ? res : []
    if (list.length > 0) {
      allContacts.value = list
      fbUserIds.value = new Set(list.map((c: any) => c.id))
      await doFreeBusyQuery()
    }
  } catch {
    console.warn('loadContactsForFreeBusy: 获取联系人失败')
  } finally {
    fbLoading.value = false
  }
}

function openFreeBusy() {
  const today = formatDateStr(new Date())
  const end = new Date()
  end.setDate(end.getDate() + 7)
  fbForm.value = { from: today, to: formatDateStr(end) }
  fbUserIds.value = new Set()
  fbResults.value = []
  fbLoading.value = false
  fbFilterMode.value = 'all'
  showFreeBusyModal.value = true
  // 优先用已有 allContacts，没有再加载
  if (allContacts.value.length > 0) {
    fbUserIds.value = new Set(allContacts.value.map((c: any) => c.id))
    doFreeBusyQuery()
  } else {
    loadContactsForFreeBusy()
  }
}

function toggleFBUser(userId: string) {
  if (fbUserIds.value.has(userId)) fbUserIds.value.delete(userId)
  else fbUserIds.value.add(userId)
  fbUserIds.value = new Set(fbUserIds.value)
}

const fbFilterMode = ref<'all' | 'free' | 'busy'>('all')

const fbDisplayMembers = computed(() => {
  const busyIds = new Set<string>()
  for (const r of fbResults.value) {
    if (r.status === 'Busy') {
      busyIds.add(r.userId)
    }
  }
  let list = allContacts.value
  if (fbFilterMode.value === 'free') {
    list = list.filter((c: any) => !busyIds.has(c.id))
  } else if (fbFilterMode.value === 'busy') {
    list = list.filter((c: any) => busyIds.has(c.id))
  }
  return list
})

function fbMemberStatus(userId: string): 'busy' | 'free' | 'unknown' {
  if (fbLoading.value) return 'unknown'
  const r = fbResults.value.find((r: any) => r.userId === userId)
  if (!r) return 'free'
  return r.status === 'Busy' ? 'busy' : 'free'
}

function fbMemberLabel(userId: string): string {
  const status = fbMemberStatus(userId)
  if (status === 'unknown') return '⏳ 查询中'
  if (status === 'busy') return '🔴 忙碌'
  return '🟢 空闲'
}

function fbMemberEvent(userId: string): string {
  const r = fbResults.value.find((r: any) => r.userId === userId)
  if (!r) return ''
  return `${formatTime(r.startTime)}-${formatTime(r.endTime)}`
}

async function doFreeBusyQuery() {
  if (!fbForm.value.from || !fbForm.value.to) return
  const userIds = Array.from(fbUserIds.value)
  if (userIds.length === 0) return
  fbLoading.value = true
  try {
    const res: any = await getFreeBusy({
      from: fbForm.value.from,
      to: fbForm.value.to,
      userIds: userIds,
    })
    fbResults.value = Array.isArray(res) ? res : []
  } catch (e: any) {
    console.error('free-busy 查询异常:', e?.message)
    fbResults.value = []
  } finally {
    fbLoading.value = false
  }
}

function onFBFromChange(e: any) {
  fbForm.value.from = e.detail.value
  doFreeBusyQuery()
}
function onFBToChange(e: any) {
  fbForm.value.to = e.detail.value
  doFreeBusyQuery()
}

/** 头像颜色 */
function getColor(id: string): string {
  const colors = ['#409EFF', '#67C23A', '#E6A23C', '#F56C6C', '#909399', '#B37FEB', '#00BFA5', '#FF7043']
  let hash = 0
  for (let i = 0; i < id.length; i++) hash = ((hash << 5) - hash) + id.charCodeAt(i)
  return colors[Math.abs(hash) % colors.length]
}

// Picker 回调
function onStartDateChange(e: any) { form.value.startDate = e.detail.value }
function onStartTimeChange(e: any) { form.value.startTimeVal = e.detail.value }
function onEndDateChange(e: any) { form.value.endDate = e.detail.value }
function onEndTimeChange(e: any) { form.value.endTimeVal = e.detail.value }

onMounted(() => loadEvents())
onShow(() => loadEvents())
</script>

<style scoped>
.calendar-container {
  min-height: 100vh;
  background: #f6f8fc;
}
.date-nav {
  display: flex;
  align-items: center;
  padding: 20rpx 24rpx 16rpx;
  background: #fff;
  gap: 12rpx;
  box-shadow: 0 8rpx 24rpx rgba(31, 49, 84, 0.04);
}
.nav-btn {
  font-size: 24rpx;
  color: #1f6fff;
  padding: 8rpx 14rpx;
  background: #f1f6ff;
  border-radius: 999rpx;
  flex-shrink: 0;
}
.nav-title {
  flex: 1;
  text-align: center;
  font-size: 28rpx;
  font-weight: 600;
  color: #1d2129;
}
.add-btn {
  font-size: 24rpx;
  color: #fff;
  font-weight: 600;
  flex-shrink: 0;
  padding: 10rpx 18rpx;
  background: #1f6fff;
  border-radius: 999rpx;
}

/* 日期横向滚动 */
.date-scroll {
  white-space: nowrap;
  background: #fff;
  padding: 16rpx 24rpx;
  border-bottom: 1rpx solid #edf1f7;
}
.date-chip {
  display: inline-flex;
  flex-direction: column;
  align-items: center;
  width: 88rpx;
  padding: 12rpx 0;
  margin-right: 12rpx;
  border-radius: 22rpx;
  background: #f6f8fc;
  border: 1rpx solid #edf1f7;
}
.date-chip.active {
  background: linear-gradient(135deg, #1f6fff, #18b7ff);
  border-color: transparent;
  box-shadow: 0 12rpx 28rpx rgba(31, 111, 255, 0.2);
}
.chip-weekday {
  font-size: 22rpx;
  color: #7b8494;
}
.date-chip.active .chip-weekday { color: #fff; }
.chip-date {
  font-size: 32rpx;
  font-weight: 600;
  color: #1d2129;
  margin-top: 4rpx;
}
.date-chip.active .chip-date { color: #fff; }

/* 日程列表 */
.event-section {
  padding: 24rpx;
}
.section-date {
  font-size: 28rpx;
  font-weight: 600;
  color: #111827;
  display: block;
  margin-bottom: 16rpx;
}
.event-item {
  display: flex;
  background: #fff;
  border-radius: 28rpx;
  padding: 26rpx;
  margin-bottom: 16rpx;
  align-items: center;
  box-shadow: 0 14rpx 36rpx rgba(31, 49, 84, 0.06);
  position: relative;
}
.event-item::before {
  content: '';
  position: absolute;
  left: 0;
  top: 28rpx;
  bottom: 28rpx;
  width: 6rpx;
  border-radius: 999rpx;
  background: linear-gradient(180deg, #1f6fff, #18b7ff);
}
.event-time {
  display: flex;
  flex-direction: column;
  align-items: center;
  margin-right: 20rpx;
  min-width: 100rpx;
}
.event-start {
  font-size: 28rpx;
  font-weight: 600;
  color: #1f6fff;
}
.event-end {
  font-size: 20rpx;
  color: #a8b0c2;
  margin-top: 4rpx;
}
.event-info {
  flex: 1;
  min-width: 0;
}
.event-title {
  font-size: 28rpx;
  color: #111827;
  font-weight: 500;
  display: block;
}
.event-location {
  font-size: 22rpx;
  color: #7b8494;
  margin-top: 4rpx;
  display: block;
}
.event-actions {
  display: flex;
  flex-direction: column;
  gap: 8rpx;
  flex-shrink: 0;
  margin-left: 16rpx;
}
.event-edit {
  font-size: 22rpx;
  color: #1f6fff;
}
.event-delete {
  font-size: 22rpx;
  color: #ef4444;
}
.event-empty {
  text-align: center;
  padding: 80rpx 0;
  font-size: 26rpx;
  color: #a8b0c2;
  background: #fff;
  border-radius: 28rpx;
  box-shadow: 0 14rpx 36rpx rgba(31, 49, 84, 0.06);
}

/* 弹窗 */
.modal-overlay {
  position: fixed;
  top: 0; left: 0; right: 0; bottom: 0;
  background: rgba(0,0,0,0.45);
  z-index: 999;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 60rpx;
}
.modal-popup {
  width: 100%;
  max-width: 560rpx;
  background: #fff;
  border-radius: 28rpx;
  padding: 32rpx;
  max-height: 80vh;
  overflow-y: auto;
}
.modal-title {
  font-size: 32rpx;
  font-weight: 600;
  display: block;
  text-align: center;
  margin-bottom: 24rpx;
}
.form-group { margin-bottom: 20rpx; }
.form-label {
  font-size: 24rpx;
  color: #7b8494;
  display: block;
  margin-bottom: 6rpx;
}
.form-input {
  height: 64rpx;
  background: #f6f8fc;
  border-radius: 16rpx;
  padding: 0 20rpx;
  font-size: 26rpx;
  color: #111827;
  display: flex;
  align-items: center;
}
.modal-btns { display: flex; gap: 20rpx; margin-top: 24rpx; }
.modal-cancel, .modal-confirm, .modal-danger {
  flex: 1;
  height: 72rpx;
  line-height: 72rpx;
  font-size: 26rpx;
  border-radius: 36rpx;
  border: none;
  text-align: center;
}
.modal-cancel { background: #f6f8fc; color: #374151; }
.modal-confirm { background: #1f6fff; color: #fff; }
.modal-confirm[disabled] { opacity: 0.4; }
.modal-danger { background: #ef4444; color: #fff; }
.delete-popup { text-align: center; }
.delete-title { font-size: 32rpx; font-weight: 600; display: block; margin-bottom: 16rpx; }
.delete-text { font-size: 28rpx; color: #111827; display: block; }

/* 重复日程 */
.event-recurrence { font-size: 22rpx; color: #7b8494; margin-top: 4rpx; display: block; }

/* 参会人 */
.attendee-chips { display: flex; flex-wrap: wrap; gap: 8rpx; margin-bottom: 8rpx; }
.attendee-chip {
  display: inline-flex; align-items: center; gap: 4rpx;
  font-size: 22rpx; color: #1f6fff; background: #eef4ff;
  padding: 4rpx 12rpx; border-radius: 999rpx;
}
.attendee-chip-remove { font-size: 18rpx; color: #7b8494; }
.form-add-btn { font-size: 24rpx; color: #1f6fff; padding: 8rpx 0; display: inline-block; }
.attendee-scroll { max-height: 350rpx; margin-bottom: 16rpx; border: 1rpx solid #edf1f7; border-radius: 16rpx; }
.attendee-row { display: flex; align-items: center; padding: 16rpx 20rpx; border-bottom: 1rpx solid #f0f2f5; }
.attendee-row:active { background: #eef4ff; }
.attendee-row:last-child { border-bottom: none; }
.attendee-row-name { flex: 1; font-size: 26rpx; color: #111827; }
.attendee-empty { text-align: center; padding: 40rpx; font-size: 24rpx; color: #a8b0c2; }
.member-avatar-sm {
  width: 48rpx; height: 48rpx; border-radius: 12rpx;
  display: flex; align-items: center; justify-content: center;
  margin-right: 16rpx; flex-shrink: 0;
}
.avatar-sm-text { color: #fff; font-size: 22rpx; }
.member-check {
  width: 32rpx; height: 32rpx; border: 2rpx solid #cfd6e3;
  border-radius: 50%; display: flex; align-items: center; justify-content: center;
}
.member-check.checked { background: #1f6fff; border-color: #1f6fff; }
.check-mark { color: #fff; font-size: 18rpx; }

/* 出席 */
.event-attendees { display: flex; flex-wrap: wrap; gap: 4rpx; margin-top: 4rpx; }
.event-attendee { font-size: 20rpx; color: #64748b; background: #f6f8fc; padding: 2rpx 8rpx; border-radius: 4rpx; }
.att-status.accepted { color: #67C23A; margin-left: 2rpx; }
.att-status.declined { color: #f56c6c; margin-left: 2rpx; }
.event-attend-btn { font-size: 22rpx; color: #fff; background: #67C23A; padding: 4rpx 12rpx; border-radius: 999rpx; }
.attendance-prompt { font-size: 28rpx; color: #111827; text-align: center; margin-bottom: 20rpx; }
.attendance-btns { gap: 16rpx; }

/* 空闲查询按钮 */
.freebusy-btn {
  font-size: 24rpx;
  color: #1f6fff;
  padding: 8rpx 14rpx;
  background: #eef4ff;
  border-radius: 999rpx;
  flex-shrink: 0;
}

/* 重复日程展开按钮 */
.event-occur-btn {
  font-size: 22rpx;
  color: #7b8494;
}

/* 空闲时间查询弹窗 */
.fb-popup { max-height: 80vh; display: flex; flex-direction: column; }
.fb-date-row {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 12rpx;
  margin-bottom: 20rpx;
}
.fb-date-picker {
  padding: 12rpx 24rpx;
  background: #f6f8fc;
  border-radius: 16rpx;
  font-size: 26rpx;
  color: #1d2129;
  border: 1rpx solid #edf1f7;
}
.fb-date-sep { font-size: 28rpx; color: #a8b0c2; }

/* 筛选栏 */
.fb-filter-bar {
  display: flex;
  gap: 12rpx;
  margin-bottom: 16rpx;
  border-bottom: 1rpx solid #f0f2f5;
  padding-bottom: 12rpx;
}
.fb-filter-tab {
  font-size: 24rpx;
  color: #7b8494;
  padding: 6rpx 16rpx;
  border-radius: 999rpx;
  background: #f6f8fc;
}
.fb-filter-tab.active {
  color: #fff;
  background: linear-gradient(135deg, #1f6fff, #18b7ff);
}

/* 成员状态列表 */
.fb-list {
  max-height: 350rpx;
  margin-bottom: 12rpx;
  border: 1rpx solid #edf1f7;
  border-radius: 16rpx;
}
.fb-member-row {
  display: flex;
  align-items: center;
  padding: 14rpx 20rpx;
  border-bottom: 1rpx solid #f0f2f5;
}
.fb-member-row:last-child { border-bottom: none; }
.fb-member-name { flex: 1; font-size: 26rpx; color: #1d2129; }
.fb-member-status { font-size: 22rpx; white-space: nowrap; }
.fb-member-event {
  font-size: 18rpx;
  color: #7b8494;
  margin-left: 8rpx;
  max-width: 140rpx;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.fb-empty-list {
  text-align: center;
  padding: 40rpx;
  font-size: 24rpx;
  color: #a8b0c2;
}
.fb-loading {
  text-align: center;
  padding: 60rpx 0;
}
.fb-loading-text {
  font-size: 26rpx;
  color: #7b8494;
}

/* 弹窗内复用 member-avatar-sm + avatar-sm-text */
.member-avatar-sm {
  width: 48rpx; height: 48rpx; border-radius: 12rpx;
  display: flex; align-items: center; justify-content: center;
  margin-right: 16rpx; flex-shrink: 0;
}
.avatar-sm-text { color: #fff; font-size: 22rpx; }

/* 重复日程展开 */
.occur-event-title { font-size: 28rpx; font-weight: 600; color: #111827; display: block; text-align: center; }
.occur-recurrence { font-size: 22rpx; color: #7b8494; text-align: center; display: block; margin-bottom: 16rpx; }
.occur-list { max-height: 350rpx; }
.occur-item { padding: 12rpx 20rpx; border-bottom: 1rpx solid #f0f2f5; display: flex; justify-content: space-between; align-items: center; }
.occur-date { font-size: 26rpx; color: #1d2129; }
.occur-time { font-size: 22rpx; color: #7b8494; }
.occur-empty { text-align: center; padding: 30rpx; font-size: 24rpx; color: #a8b0c2; }
</style>
