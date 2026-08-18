<template>
  <div class="flex h-full bg-white dark:bg-gray-900 transition-colors">
    <!-- 左：Tab 切换"分组"和"部门" -->
    <div class="w-56 border-r border-gray-200 dark:border-gray-700 flex flex-col bg-gray-50 dark:bg-[#1A1D23] flex-shrink-0">
      <div class="p-2 border-b border-gray-200 dark:border-gray-700">
        <div class="flex bg-white dark:bg-gray-800 rounded-md p-0.5 text-xs">
          <button @click="leftTab = 'groups'" :class="['flex-1 h-7 rounded transition', leftTab === 'groups' ? 'bg-primary text-white' : 'text-gray-600 dark:text-gray-300']">分组</button>
          <button @click="leftTab = 'depts'" :class="['flex-1 h-7 rounded transition', leftTab === 'depts' ? 'bg-primary text-white' : 'text-gray-600 dark:text-gray-300']">部门</button>
        </div>
      </div>

      <!-- 分组列表 -->
      <div v-if="leftTab === 'groups'" class="flex-1 overflow-y-auto p-4 space-y-0.5">
        <h3 class="text-xs text-gray-500 dark:text-gray-400 font-medium px-3 mb-2">联系人</h3>
        <button v-for="g in groups" :key="g.id" @click="activeGroup = g.id"
                :class="['w-full flex items-center px-3 py-2 rounded-md text-sm transition-colors',
                         activeGroup === g.id
                           ? 'bg-primary-50 dark:bg-primary/20 text-primary dark:text-primary-100'
                           : 'text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800']">
          <component :is="g.icon" class="w-4 h-4 mr-2" />
          <span class="flex-1 text-left">{{ g.label }}</span>
          <span class="text-xs text-gray-400">{{ g.count }}</span>
        </button>
      </div>

      <!-- 部门树（真实后端：嵌套 children） -->
      <div v-else class="flex-1 overflow-y-auto p-2">
        <DeptTreeNode v-for="d in deptTree" :key="d.id" :node="d" :level="0"
                      :selected-id="activeDeptId" @select="activeDeptId = $event" />
        <div v-if="!deptTree.length && !loading" class="text-center py-8 text-xs text-gray-400">暂无部门</div>
      </div>
    </div>

    <!-- 中：列表 -->
    <div class="flex-1 flex flex-col overflow-hidden">
      <div class="h-14 px-6 flex items-center justify-between border-b border-gray-200 dark:border-gray-700">
        <div class="flex items-center">
          <h2 class="text-base font-medium text-gray-900 dark:text-gray-100">
          <template v-if="leftTab === 'groups'">{{ activeGroupLabel }}（{{ filteredContacts.length }}）</template>
          <template v-else>
            <span v-if="activeDeptName">{{ activeDeptName }}（{{ filteredContacts.length }}）</span>
            <span v-else>请选择部门（{{ filteredContacts.length }}）</span>
          </template>
        </h2>
        </div>
        <div class="flex items-center space-x-2">
          <div class="relative">
            <MagnifyingGlassIcon class="absolute left-2.5 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
            <input v-model="search" placeholder="搜索联系人"
                   class="h-8 pl-9 pr-3 text-sm bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 rounded-md outline-none focus:ring-2 focus:ring-primary/30 w-56 dark:text-gray-100 dark:placeholder-gray-500" />
          </div>
          <button @click="showAddFriend = true"
                  class="h-8 px-3 text-sm bg-primary hover:bg-primary-hover text-white rounded-md flex items-center flex-shrink-0">
            <UserPlusIcon class="w-4 h-4 mr-1" />
            添加好友
          </button>
        </div>
      </div>

      <!-- 加载状态 -->
      <div v-if="loading" class="flex-1 flex items-center justify-center text-gray-400 text-sm">
        <svg class="animate-spin w-5 h-5 mr-2" viewBox="0 0 24 24" fill="none">
          <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="3" stroke-opacity="0.25" />
          <path d="M4 12a8 8 0 018-8v3a5 5 0 00-5 5H4z" fill="currentColor" />
        </svg>
        加载中…
      </div>
      <div v-else-if="filteredContacts.length === 0" class="flex-1 flex items-center justify-center text-gray-400 text-sm">没有找到联系人</div>

      <div v-else class="flex-1 overflow-y-auto">
        <!-- 分组模式：按拼音首字母 -->
        <div v-if="leftTab === 'groups'">
          <div v-for="group in groupedContacts" :key="group.letter">
            <div class="sticky top-0 z-10 bg-gray-50/95 dark:bg-gray-800/95 backdrop-blur px-6 py-1.5 text-xs font-medium text-gray-500 dark:text-gray-400 border-b border-gray-100 dark:border-gray-700">
              {{ group.letter }}
            </div>
            <div v-for="c in group.items" :key="c.id"
                 @click="activeContact = c"
                 class="group flex items-center px-6 py-3 cursor-pointer transition-colors"
                 :class="activeContact?.id === c.id ? 'bg-primary-50 dark:bg-primary/20' : 'hover:bg-gray-50 dark:hover:bg-gray-800'">
              <div class="relative flex-shrink-0">
                <div class="w-10 h-10 rounded-full flex items-center justify-center text-white font-medium" :style="{ background: c.color }">{{ c.name[0] }}</div>
                <span v-if="c.online" class="absolute bottom-0 right-0 w-3 h-3 bg-green-500 rounded-full border-2 border-white dark:border-gray-900"></span>
                <button v-if="isStarred(c)" @click.stop="toggleStar(c, $event)" class="absolute -top-1 -left-1 w-4 h-4 bg-yellow-400 rounded-full flex items-center justify-center text-white text-[10px]">★</button>
              </div>
              <div class="ml-3 flex-1 min-w-0">
                <div class="text-sm font-medium text-gray-900 dark:text-gray-100">{{ c.name }}</div>
                <div class="text-xs text-gray-500 truncate">{{ c.title }} · {{ c.dept }}</div>
              </div>
              <div class="flex items-center space-x-1 opacity-0 group-hover:opacity-100 transition-opacity">
                <button @click.stop="toggleStar(c, $event)" :class="['w-8 h-8 rounded-md flex items-center justify-center', isStarred(c) ? 'text-yellow-400 opacity-100' : 'text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700']" :title="isStarred(c) ? '取消星标' : '加星标'">
                  <StarIcon class="w-4 h-4" />
                </button>
                <button @click.stop="startChatWith(c)" class="w-8 h-8 rounded-md hover:bg-gray-100 dark:hover:bg-gray-700 flex items-center justify-center" title="发起聊天"><ChatBubbleLeftRightIcon class="w-4 h-4 text-gray-500" /></button>
                <button class="w-8 h-8 rounded-md hover:bg-gray-100 dark:hover:bg-gray-700 flex items-center justify-center"><PhoneIcon class="w-4 h-4 text-gray-500" /></button>
              </div>
            </div>
          </div>
        </div>

        <!-- 部门模式 -->
        <div v-else>
          <div v-for="c in filteredContacts" :key="c.id"
               @click="activeContact = c"
               @dblclick="startChatWith(c)"
               class="group flex items-center px-6 py-3 cursor-pointer transition-colors"
               :class="activeContact?.id === c.id ? 'bg-primary-50 dark:bg-primary/20' : 'hover:bg-gray-50 dark:hover:bg-gray-800'">
            <div class="relative flex-shrink-0">
              <div class="w-10 h-10 rounded-full flex items-center justify-center text-white font-medium" :style="{ background: c.color }">{{ c.name[0] }}</div>
              <span v-if="c.online" class="absolute bottom-0 right-0 w-3 h-3 bg-green-500 rounded-full border-2 border-white dark:border-gray-900"></span>
            </div>
            <div class="ml-3 flex-1 min-w-0">
              <div class="text-sm font-medium text-gray-900 dark:text-gray-100">{{ c.name }}</div>
              <div class="text-xs text-gray-500 truncate">{{ c.title }} · {{ c.dept }}</div>
            </div>
            <button @click.stop="startChatWith(c)" class="opacity-0 group-hover:opacity-100 w-8 h-8 rounded-md bg-primary text-white hover:bg-primary-hover flex items-center justify-center transition" title="发起聊天">
              <ChatBubbleLeftRightIcon class="w-4 h-4" />
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- 右：详情 -->
    <div v-if="activeContact" class="w-80 border-l border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-[#1A1D23] overflow-y-auto flex-shrink-0">
      <div class="p-6 text-center border-b border-gray-200 dark:border-gray-700">
        <div class="relative inline-block">
          <div class="w-20 h-20 rounded-full flex items-center justify-center text-white text-2xl font-medium mx-auto shadow-md" :style="{ background: activeContact.color }">{{ activeContact.name[0] }}</div>
          <span v-if="activeContact.online" class="absolute bottom-1 right-1 w-4 h-4 bg-green-500 rounded-full border-2 border-white dark:border-gray-900"></span>
        </div>
        <h3 class="mt-3 text-lg font-medium text-gray-900 dark:text-gray-100">{{ activeContact.name }}</h3>
        <p class="text-sm text-gray-500 mt-1">{{ activeContact.title }}</p>
        <div class="mt-4 flex justify-center space-x-2">
          <button @click="startChatWith(activeContact)" class="w-10 h-10 rounded-full bg-primary text-white hover:bg-primary-hover flex items-center justify-center transition shadow" title="发起聊天"><ChatBubbleLeftRightIcon class="w-5 h-5" /></button>
          <button class="w-10 h-10 rounded-full bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 hover:border-primary hover:text-primary text-gray-600 dark:text-gray-300 flex items-center justify-center transition"><PhoneIcon class="w-5 h-5" /></button>
          <button class="w-10 h-10 rounded-full bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 hover:border-primary hover:text-primary text-gray-600 dark:text-gray-300 flex items-center justify-center transition"><VideoCameraIcon class="w-5 h-5" /></button>
          <button class="w-10 h-10 rounded-full bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 hover:border-primary hover:text-primary text-gray-600 dark:text-gray-300 flex items-center justify-center transition"><EnvelopeIcon class="w-5 h-5" /></button>
        </div>
      </div>
      <div class="p-6 space-y-5">
        <div>
          <h4 class="text-xs text-gray-500 dark:text-gray-400 font-medium mb-2.5">个人信息</h4>
          <div class="space-y-2.5 text-sm">
            <div class="flex items-center text-gray-700 dark:text-gray-300"><BriefcaseIcon class="w-4 h-4 mr-3 text-gray-400 flex-shrink-0" /><span>{{ activeContact.dept }} · {{ activeContact.title }}</span></div>
            <div v-if="activeContact.phone" class="flex items-center text-gray-700 dark:text-gray-300"><PhoneIcon class="w-4 h-4 mr-3 text-gray-400 flex-shrink-0" /><span>{{ activeContact.phone }}</span></div>
            <div v-if="activeContact.email" class="flex items-center text-gray-700 dark:text-gray-300"><EnvelopeIcon class="w-4 h-4 mr-3 text-gray-400 flex-shrink-0" /><span class="truncate">{{ activeContact.email }}</span></div>
            <div v-if="activeContact.workPlace" class="flex items-center text-gray-700 dark:text-gray-300"><BuildingOffice2Icon class="w-4 h-4 mr-3 text-gray-400 flex-shrink-0" /><span>{{ activeContact.workPlace }}</span></div>
            <div v-if="activeContact.bio" class="text-xs text-gray-500 dark:text-gray-400 mt-1 italic">"{{ activeContact.bio }}"</div>
          </div>
        </div>
      </div>
    </div>

    <!-- 加好友弹窗 + 好友请求 toast -->
    <AddFriendDialog v-model="showAddFriend" @added="onAdded" />
    <FriendRequestsToast />
  </div>
</template>

<script setup>
defineOptions({ name: 'Contacts' })

import { ref, computed, h, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { listContacts, getDepartmentTree, adaptContact } from '@/api/contacts'
import { groupByFirstLetter } from '@/utils/pinyin'
import { ElMessage } from '@/api/toast'
import { useMessagesStore } from '@/stores/messages'
import { useUserStore } from '@/stores/user'
import { useFriendStore } from '@/stores/friend'
import AddFriendDialog from '@/components/AddFriendDialog.vue'
import FriendRequestsToast from '@/components/FriendRequestsToast.vue'
import {
  UserGroupIcon, StarIcon, ClockIcon, BuildingOfficeIcon, BuildingOffice2Icon,
  MagnifyingGlassIcon, PhoneIcon, ChatBubbleLeftRightIcon,
  VideoCameraIcon, EnvelopeIcon, BriefcaseIcon, ChevronRightIcon,
  UserPlusIcon
} from '@heroicons/vue/24/outline'

const router = useRouter()
const messagesStore = useMessagesStore()
const userStore = useUserStore()
const friendStore = useFriendStore()

const showAddFriend = ref(false)
function onAdded() {
  showAddFriend.value = false
  friendStore.fetchAll()
}

const loading = ref(true)
const contacts = ref([])
const deptTree = ref([])
const search = ref('')
const leftTab = ref('groups')
const activeGroup = ref('all')
const activeDeptId = ref(null)
const activeContact = ref(null)
const starredIds = ref(loadStarredIds())

const starredContacts = computed(() => contacts.value.filter(c => starredIds.value.includes(c.id)))
const groups = computed(() => [
  { id: 'all',      label: '全部联系人', icon: UserGroupIcon,       count: contacts.value.length },
  { id: 'star',     label: '星标联系人', icon: StarIcon,           count: starredContacts.value.length },
  { id: 'recent',   label: '最近联系',   icon: ClockIcon,          count: Math.min(contacts.value.length, 5) },
  { id: 'internal', label: '内部通讯录', icon: BuildingOfficeIcon, count: contacts.value.length }
])
const activeGroupLabel = computed(() => groups.value.find(g => g.id === activeGroup.value)?.label || '全部联系人')

const activeDeptName = computed(() => findDeptName(deptTree.value, activeDeptId.value))
function findDeptName(tree, id) {
  if (!tree || !id) return ''
  for (const n of tree) {
    if (n.id === id) return n.name
    const inner = findDeptName(n.children, id)
    if (inner) return inner
  }
  return ''
}

const STORAGE_KEY = 'feishu-star-contacts'

const filteredContacts = computed(() => {
  let list = contacts.value
  if (leftTab.value === 'groups') {
    if (activeGroup.value === 'star') {
      list = starredContacts.value
    } else if (activeGroup.value === 'recent') {
      // 后端没 lastLogin 字段，用前 5 个当「最近」
      list = list.slice(0, 5)
    } else if (activeGroup.value === 'internal') {
      // 后端都是内部通讯录 = 全部
    }
  }
  if (leftTab.value === 'depts' && activeDeptId.value) {
    list = list.filter(c => c.departmentId === activeDeptId.value)
  }
  const kw = search.value.trim().toLowerCase()
  if (kw) list = list.filter(c => (c.name || '').toLowerCase().includes(kw) || (c.title || '').toLowerCase().includes(kw) || (c.dept || '').toLowerCase().includes(kw))
  return list
})

function toggleStar(c, e) {
  if (e) e.stopPropagation()
  let stars = [...starredIds.value]
  if (stars.includes(c.id)) {
    stars = stars.filter(id => id !== c.id)
    ElMessage({ message: '已取消星标', type: 'info' })
  } else {
    stars.push(c.id)
    ElMessage({ message: '已加星标', type: 'success' })
  }
  starredIds.value = stars
  localStorage.setItem(STORAGE_KEY, JSON.stringify(stars))
}

function isStarred(c) {
  return starredIds.value.includes(c.id)
}

function loadStarredIds() {
  try {
    const ids = JSON.parse(localStorage.getItem(STORAGE_KEY) || '[]')
    return Array.isArray(ids) ? ids : []
  } catch {
    return []
  }
}

/** 发起私聊：创建会话 → 跳到消息页 → 选中会话 */
async function startChatWith(c) {
  if (!c || !c.id) return
  if (c.id === userStore.userInfo?.id) {
    ElMessage({ message: '不能跟自己聊天', type: 'warning' })
    return
  }
  try {
    const conv = await messagesStore.startConversation([c.id])
    // startConversation 已刷新列表并返回 convId，选中并跳转
    if (conv?.id) await messagesStore.selectConversation(conv.id)
    router.push('/messages')
  } catch (e) {
    ElMessage({ message: '发起聊天失败：' + (e.message || '未知错误'), type: 'error' })
  }
}

const groupedContacts = computed(() => groupByFirstLetter(filteredContacts.value))

onMounted(async () => {
  loading.value = true
  try {
    const [list, tree] = await Promise.all([listContacts(), getDepartmentTree(), friendStore.fetchAll()])
    contacts.value = (list || []).map(adaptContact)
    deptTree.value = tree || []
    if (!activeContact.value && contacts.value.length) {
      activeContact.value = contacts.value[0]
    }
  } catch (e) {
    ElMessage({ message: '加载联系人失败', type: 'error' })
  } finally {
    loading.value = false
  }
})

/**
 * 递归部门树组件
 */
const DeptTreeNode = {
  props: ['node', 'level', 'selectedId'],
  emits: ['select'],
  setup(props, { emit }) {
    const expanded = ref(props.level < 2)
    function toggle() { expanded.value = !expanded.value }
    return () => {
      const hasChild = props.node.children && props.node.children.length > 0
      const selected = props.selectedId === props.node.id
      return h('div', {}, [
        h('div', {
          class: ['flex items-center py-1.5 px-2 rounded cursor-pointer text-sm transition',
                  selected ? 'bg-primary-50 dark:bg-primary/20 text-primary' : 'hover:bg-gray-100 dark:hover:bg-gray-800 text-gray-700 dark:text-gray-300'],
          style: { paddingLeft: (props.level * 14 + 8) + 'px' },
          onClick: () => emit('select', props.node.id)
        }, [
          hasChild
            ? h(ChevronRightIcon, { class: ['w-3 h-3 mr-1 transition', expanded.value ? 'rotate-90' : ''], onClick: (e) => { e.stopPropagation(); toggle() } })
            : h('span', { class: 'w-3 h-3 mr-1' }),
          h('span', { class: 'text-base mr-1.5' }, '🏢'),
          h('span', { class: 'flex-1 truncate' }, props.node.name),
          h('span', { class: 'text-xs text-gray-400' }, (props.node._count || ''))
        ]),
        expanded.value && hasChild
          ? h('div', {}, props.node.children.map(c => h(DeptTreeNode, { node: c, level: props.level + 1, selectedId: props.selectedId, onSelect: (id) => emit('select', id) })))
          : null
      ])
    }
  }
}
</script>
