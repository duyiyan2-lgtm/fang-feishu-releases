<template>
  <div class="rich-editor border border-gray-200 dark:border-gray-700 rounded-md overflow-hidden bg-white dark:bg-gray-900">
    <div v-if="editor" class="flex items-center flex-wrap gap-0.5 px-2 py-1.5 border-b border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/50 sticky top-0 z-10">
      <button @click="editor.chain().focus().toggleBold().run()" :class="btn(editor.isActive('bold'))" title="粗体 (Ctrl+B)">
        <span class="font-bold text-sm">B</span>
      </button>
      <button @click="editor.chain().focus().toggleItalic().run()" :class="btn(editor.isActive('italic'))" title="斜体 (Ctrl+I)">
        <span class="italic text-sm">I</span>
      </button>
      <button @click="editor.chain().focus().toggleStrike().run()" :class="btn(editor.isActive('strike'))" title="删除线">
        <span class="line-through text-sm">S</span>
      </button>
      <span class="w-px h-5 bg-gray-300 dark:bg-gray-700 mx-1"></span>
      <button @click="editor.chain().focus().toggleHeading({ level: 1 }).run()" :class="btn(editor.isActive('heading', { level: 1 }))" title="标题 1">
        <span class="text-sm font-bold">H1</span>
      </button>
      <button @click="editor.chain().focus().toggleHeading({ level: 2 }).run()" :class="btn(editor.isActive('heading', { level: 2 }))" title="标题 2">
        <span class="text-sm font-bold">H2</span>
      </button>
      <button @click="editor.chain().focus().toggleHeading({ level: 3 }).run()" :class="btn(editor.isActive('heading', { level: 3 }))" title="标题 3">
        <span class="text-sm font-bold">H3</span>
      </button>
      <span class="w-px h-5 bg-gray-300 dark:bg-gray-700 mx-1"></span>
      <button @click="editor.chain().focus().toggleBulletList().run()" :class="btn(editor.isActive('bulletList'))" title="无序列表">•</button>
      <button @click="editor.chain().focus().toggleOrderedList().run()" :class="btn(editor.isActive('orderedList'))" title="有序列表">1.</button>
      <button @click="editor.chain().focus().toggleBlockquote().run()" :class="btn(editor.isActive('blockquote'))" title="引用">"</button>
      <button @click="editor.chain().focus().setHorizontalRule().run()" :class="btn(false)" title="分割线">―</button>
      <span class="w-px h-5 bg-gray-300 dark:bg-gray-700 mx-1"></span>
      <button @click="setLink" :class="btn(editor.isActive('link'))" title="链接">🔗</button>
      <button @click="editor.chain().focus().setTextAlign('left').run()" :class="btn(editor.isActive({ textAlign: 'left' }))">⇤</button>
      <button @click="editor.chain().focus().setTextAlign('center').run()" :class="btn(editor.isActive({ textAlign: 'center' }))">↔</button>
      <button @click="editor.chain().focus().setTextAlign('right').run()" :class="btn(editor.isActive({ textAlign: 'right' }))">⇥</button>
      <span class="ml-auto flex items-center gap-1">
        <button @click="editor.chain().focus().undo().run()" :disabled="!editor.can().undo()" class="w-8 h-8 rounded hover:bg-gray-100 dark:hover:bg-gray-700 disabled:opacity-40 flex items-center justify-center text-sm" title="撤销">↶</button>
        <button @click="editor.chain().focus().redo().run()" :disabled="!editor.can().redo()" class="w-8 h-8 rounded hover:bg-gray-100 dark:hover:bg-gray-700 disabled:opacity-40 flex items-center justify-center text-sm" title="重做">↷</button>
        <button @click="editor.chain().focus().clearContent().run()" class="px-2 h-8 rounded hover:bg-red-50 hover:text-red-500 text-xs text-gray-500" title="清空">清空</button>
      </span>
    </div>
    <EditorContent :editor="editor" class="prose prose-sm max-w-none dark:prose-invert p-6 min-h-[400px] focus:outline-none" />
  </div>
</template>

<script setup>
import { useEditor, EditorContent } from '@tiptap/vue-3'
import StarterKit from '@tiptap/starter-kit'
import Placeholder from '@tiptap/extension-placeholder'
import Link from '@tiptap/extension-link'
import { watch } from 'vue'

const props = defineProps({
  modelValue: { type: String, default: '' },
  placeholder: { type: String, default: '开始输入你的内容...' },
  editable: { type: Boolean, default: true }
})
const emit = defineEmits(['update:modelValue'])

const editor = useEditor({
  content: props.modelValue,
  editable: props.editable,
  extensions: [
    StarterKit.configure({ heading: { levels: [1, 2, 3] } }),
    Placeholder.configure({ placeholder: props.placeholder }),
    Link.configure({ openOnClick: false })
  ],
  onUpdate({ editor }) { emit('update:modelValue', editor.getHTML()) }
})

watch(() => props.modelValue, (val) => {
  if (editor.value && editor.value.getHTML() !== val) {
    editor.value.commands.setContent(val || '', false)
  }
})

watch(() => props.editable, (val) => { editor.value?.setEditable(val) })

function setLink() {
  const url = window.prompt('链接地址', 'https://')
  if (url === null) return
  if (url === '') {
    editor.value?.chain().focus().unsetLink().run()
    return
  }
  editor.value?.chain().focus().extendMarkRange('link').setLink({ href: url }).run()
}

function btn(active) {
  return [
    'w-8 h-8 rounded flex items-center justify-center transition text-gray-700 dark:text-gray-200',
    active ? 'bg-primary text-white' : 'hover:bg-gray-100 dark:hover:bg-gray-700'
  ]
}
</script>

<style scoped>
:deep(.ProseMirror p.is-editor-empty:first-child::before) {
  content: attr(data-placeholder);
  float: left;
  color: #adb5bd;
  pointer-events: none;
  height: 0;
}
</style>
