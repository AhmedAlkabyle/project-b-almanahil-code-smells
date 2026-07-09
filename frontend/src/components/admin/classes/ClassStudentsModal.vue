<script setup>
import { computed, ref, watch } from 'vue'
import { useLanguageStore } from '../../../stores/language'
import BaseModal from '../../common/BaseModal.vue'
import { listClassStudents, removeStudentFromClass } from '../../../api/classes'

const props = defineProps({
  open: { type: Boolean, default: false },
  // The class whose enrolled students we're viewing.
  classItem: { type: Object, default: null }
})
const emit = defineEmits(['close', 'removed'])

const language = useLanguageStore()

// ---- Bilingual copy (per-component i18n pattern) ----
const content = {
  en: {
    title: 'Enrolled Students',
    subtitlePrefix: 'Students in',
    searchPh: 'Search by name or email…',
    countLabel: 'students',
    loading: 'Loading students…',
    loadError: 'We couldn’t load the students. Please try again.',
    retry: 'Retry',
    emptyTitle: 'No students enrolled',
    emptyHint: 'No students enrolled in this class yet.',
    noMatch: 'No students match your search.',
    remove: 'Remove',
    removing: 'Removing…',
    confirmText: 'Remove from this class?',
    yes: 'Remove',
    no: 'Cancel',
    close: 'Close'
  },
  ar: {
    title: 'الطلاب المسجّلون',
    subtitlePrefix: 'طلاب',
    searchPh: 'ابحث بالاسم أو البريد…',
    countLabel: 'طالب',
    loading: 'جارٍ تحميل الطلاب…',
    loadError: 'تعذّر تحميل الطلاب. يرجى المحاولة مرة أخرى.',
    retry: 'إعادة المحاولة',
    emptyTitle: 'لا يوجد طلاب مسجّلون',
    emptyHint: 'لا يوجد طلاب مسجّلون في هذا الصف بعد.',
    noMatch: 'لا يوجد طلاب مطابقون لبحثك.',
    remove: 'إزالة',
    removing: 'جارٍ الإزالة…',
    confirmText: 'إزالته من هذا الصف؟',
    yes: 'إزالة',
    no: 'إلغاء',
    close: 'إغلاق'
  }
}
const t = computed(() => content[language.lang])

// ---- State ----
const students = ref([]) // enrolled students loaded from the API
const loading = ref(true)
const loadError = ref('')
const search = ref('')
const confirmingId = ref(null) // row awaiting an inline "are you sure?"
const removingId = ref(null) // row whose remove request is in flight
const notice = ref(null) // { type: 'success' | 'error', text }

// Build the class label in the CURRENT language. The backend's displayName is always
// composed in Arabic (e.g. "3/A — الثالث ثانوي 2025/2026"), so in English we compose our
// own: "3/A — Year 3 — High School 2025/2026".
const arabicOrdinals = ['الأول', 'الثاني', 'الثالث'] // grade 1..3
const classLabel = computed(() => {
  const c = props.classItem
  if (!c) return ''
  const bits = [c.name]
  if (c.level && c.grade) {
    bits.push(
      language.isArabic
        ? `${arabicOrdinals[c.grade - 1] ?? c.grade} ${c.level === 'HighSchool' ? 'ثانوي' : 'إعدادي'}`
        : `Year ${c.grade} — ${c.level === 'HighSchool' ? 'High School' : 'Secondary'}`
    )
  }
  let label = bits.filter(Boolean).join(' — ')
  if (c.academicYear) label += ` ${c.academicYear}`
  return label || c.displayName || c.name || ''
})

// Filter by name + email (case-insensitive).
const filtered = computed(() => {
  const q = search.value.trim().toLowerCase()
  if (!q) return students.value
  return students.value.filter((s) => `${s.fullName} ${s.email}`.toLowerCase().includes(q))
})

// Two-letter initials for the avatar chip.
function initials(name) {
  const parts = (name || '').trim().split(/\s+/).filter(Boolean)
  if (!parts.length) return '?'
  const first = parts[0][0]
  const last = parts.length > 1 ? parts[parts.length - 1][0] : ''
  return (first + last).toUpperCase()
}

async function load() {
  loading.value = true
  loadError.value = ''
  try {
    const { data } = await listClassStudents(props.classItem.id)
    students.value = data ?? []
  } catch (err) {
    loadError.value = err.message || t.value.loadError
  } finally {
    loading.value = false
  }
}

// Reset + (re)load every time the modal opens.
watch(
  () => props.open,
  (isOpen) => {
    if (!isOpen) return
    search.value = ''
    confirmingId.value = null
    removingId.value = null
    notice.value = null
    load()
  }
)

function askRemove(s) {
  notice.value = null
  confirmingId.value = s.id
}
function cancelRemove() {
  confirmingId.value = null
}

async function confirmRemove(s) {
  removingId.value = s.id
  notice.value = null
  try {
    const res = await removeStudentFromClass(props.classItem.id, s.id)
    // Drop the student from the local list and tell the parent to update the count.
    students.value = students.value.filter((x) => x.id !== s.id)
    notice.value = { type: 'success', text: res.message }
    emit('removed', { message: res.message, studentId: s.id })
  } catch (err) {
    notice.value = { type: 'error', text: err.message }
  } finally {
    removingId.value = null
    confirmingId.value = null
  }
}
</script>

<template>
  <BaseModal
    :open="open"
    :title="t.title"
    :subtitle="classLabel ? `${t.subtitlePrefix} ${classLabel}` : ''"
    @close="emit('close')"
  >
    <div class="cs-body">
      <!-- Success / error notice -->
      <div v-if="notice" class="cs-notice" :class="notice.type">
        <svg v-if="notice.type === 'success'" viewBox="0 0 24 24"><path d="M20 6 9 17l-5-5" /></svg>
        <svg v-else viewBox="0 0 24 24"><circle cx="12" cy="12" r="9" /><path d="M12 8v5M12 16h.01" /></svg>
        <span>{{ notice.text }}</span>
      </div>

      <!-- Loading -->
      <div v-if="loading" class="cs-state">
        <span class="cs-spinner"></span>
        <p>{{ t.loading }}</p>
      </div>

      <!-- Load error -->
      <div v-else-if="loadError" class="cs-state">
        <span class="cs-icon error">
          <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="9" /><path d="M12 8v5M12 16h.01" /></svg>
        </span>
        <p class="cs-state-text">{{ loadError }}</p>
        <button type="button" class="btn ghost" @click="load">{{ t.retry }}</button>
      </div>

      <!-- Empty: no students enrolled -->
      <div v-else-if="!students.length" class="cs-state">
        <span class="cs-icon">
          <svg viewBox="0 0 24 24"><path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" /><circle cx="9" cy="7" r="4" /><path d="M22 21v-2a4 4 0 0 0-3-3.87" /></svg>
        </span>
        <p class="cs-empty-title">{{ t.emptyTitle }}</p>
        <p class="cs-empty-hint">{{ t.emptyHint }}</p>
      </div>

      <!-- Loaded list -->
      <template v-else>
        <!-- Search + count -->
        <label class="cs-search">
          <svg viewBox="0 0 24 24"><circle cx="11" cy="11" r="7" /><path d="m21 21-4.3-4.3" /></svg>
          <input v-model="search" type="text" :placeholder="t.searchPh" />
        </label>
        <div class="cs-toolbar">
          <span class="cs-count">{{ students.length }}</span>
          <span class="cs-count-label">{{ t.countLabel }}</span>
        </div>

        <!-- Student list -->
        <div v-if="filtered.length" class="cs-list">
          <div v-for="s in filtered" :key="s.id" class="cs-row">
            <span class="cs-avatar">{{ initials(s.fullName) }}</span>
            <span class="cs-info">
              <span class="cs-name">{{ s.fullName }}</span>
              <span class="cs-email">{{ s.email }}</span>
            </span>

            <!-- Inline confirm vs. the Remove button -->
            <span v-if="confirmingId === s.id" class="cs-confirm">
              <span class="cs-confirm-text">{{ t.confirmText }}</span>
              <button
                type="button"
                class="cs-btn danger"
                :disabled="removingId === s.id"
                @click="confirmRemove(s)"
              >
                {{ removingId === s.id ? t.removing : t.yes }}
              </button>
              <button
                type="button"
                class="cs-btn ghost-sm"
                :disabled="removingId === s.id"
                @click="cancelRemove"
              >
                {{ t.no }}
              </button>
            </span>
            <button v-else type="button" class="cs-remove" @click="askRemove(s)">
              <svg viewBox="0 0 24 24"><path d="M3 6h18" /><path d="M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2" /><path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" /></svg>
              <span>{{ t.remove }}</span>
            </button>
          </div>
        </div>

        <!-- No search match -->
        <div v-else class="cs-nomatch">{{ t.noMatch }}</div>
      </template>
    </div>

    <template #footer>
      <button type="button" class="btn ghost" @click="emit('close')">{{ t.close }}</button>
    </template>
  </BaseModal>
</template>

<style scoped>
.cs-body {
  --accent: var(--ds-navy, #1e4c9a);
  --accent-ring: rgba(30, 76, 154, 0.14);
  display: flex;
  flex-direction: column;
  gap: 0.9rem;
}

/* Success / error notice */
.cs-notice {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  padding: 0.75rem 0.95rem;
  border-radius: 12px;
  font-size: 0.85rem;
  font-weight: 600;
  line-height: 1.4;
}
.cs-notice svg {
  width: 18px;
  height: 18px;
  flex-shrink: 0;
  fill: none;
  stroke: currentColor;
  stroke-width: 2;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.cs-notice.success {
  color: #15803d;
  background: #f0fdf4;
  border: 1px solid #bbf7d0;
}
.cs-notice.error {
  color: #b91c1c;
  background: #fef2f2;
  border: 1px solid #fecaca;
}

/* Search box */
.cs-search {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.7rem 0.95rem;
  background: #f4f7fc;
  border: 1.5px solid #e6ebf4;
  border-radius: 14px;
  transition: border-color 0.2s ease, box-shadow 0.2s ease, background 0.2s ease;
}
.cs-search:focus-within {
  background: #fff;
  border-color: var(--accent);
  box-shadow: 0 0 0 4px var(--accent-ring);
}
.cs-search svg {
  width: 17px;
  height: 17px;
  flex-shrink: 0;
  fill: none;
  stroke: #9ca3af;
  stroke-width: 1.9;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.cs-search input {
  width: 100%;
  min-width: 0;
  border: none;
  outline: none;
  background: transparent;
  font-family: inherit;
  font-size: 0.92rem;
  color: #0f2444;
}

/* Toolbar (enrolled count) */
.cs-toolbar {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0 0.15rem;
}
.cs-count {
  min-width: 26px;
  padding: 0.2rem 0.6rem;
  border-radius: 999px;
  text-align: center;
  font-size: 0.78rem;
  font-weight: 800;
  color: var(--accent);
  background: #e0ecff;
}
.cs-count-label {
  font-size: 0.82rem;
  font-weight: 700;
  color: #64748b;
}

/* Scrollable list */
.cs-list {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  max-height: 340px;
  overflow-y: auto;
  padding: 0.15rem;
  margin: -0.15rem;
}
.cs-row {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.6rem 0.8rem;
  border: 1.5px solid #e6ebf4;
  border-radius: 13px;
  background: #fff;
}

/* Avatar chip */
.cs-avatar {
  flex-shrink: 0;
  width: 38px;
  height: 38px;
  border-radius: 50%;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-weight: 800;
  font-size: 0.82rem;
  color: #fff;
  background: linear-gradient(135deg, #4361ee, #3b6fe0);
  box-shadow: 0 3px 8px rgba(30, 41, 59, 0.16);
}
.cs-info {
  display: flex;
  flex-direction: column;
  line-height: 1.3;
  min-width: 0;
  flex: 1;
}
.cs-name {
  font-weight: 700;
  font-size: 0.92rem;
  color: #0f2444;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.cs-email {
  font-size: 0.78rem;
  color: #8a94a6;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

/* Remove button */
.cs-remove {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  flex-shrink: 0;
  padding: 0.42rem 0.75rem;
  border: 1px solid #e2e8f2;
  border-radius: 9px;
  background: #fff;
  font-family: inherit;
  font-size: 0.8rem;
  font-weight: 700;
  color: #475569;
  cursor: pointer;
  transition: background 0.15s ease, color 0.15s ease, border-color 0.15s ease;
}
.cs-remove svg {
  width: 15px;
  height: 15px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.9;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.cs-remove:hover {
  color: #dc2626;
  border-color: #f6c9c9;
  background: #fef2f2;
}

/* Inline confirm */
.cs-confirm {
  display: inline-flex;
  align-items: center;
  gap: 0.45rem;
  flex-shrink: 0;
}
.cs-confirm-text {
  font-size: 0.78rem;
  font-weight: 700;
  color: #b91c1c;
  white-space: nowrap;
}
.cs-btn {
  padding: 0.38rem 0.7rem;
  border-radius: 8px;
  border: 1px solid transparent;
  font-family: inherit;
  font-size: 0.78rem;
  font-weight: 700;
  cursor: pointer;
  transition: filter 0.15s ease, background 0.15s ease;
}
.cs-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
.cs-btn.danger {
  color: #fff;
  background: #dc2626;
}
.cs-btn.danger:hover:not(:disabled) {
  filter: brightness(1.05);
}
.cs-btn.ghost-sm {
  color: #475569;
  background: #f1f4fb;
}
.cs-btn.ghost-sm:hover:not(:disabled) {
  background: #e7edf7;
}

/* No-search-match line */
.cs-nomatch {
  padding: 1.5rem 1rem;
  text-align: center;
  font-size: 0.88rem;
  color: #8a94a6;
}

/* Shared state block (loading / error / empty) */
.cs-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  gap: 0.5rem;
  padding: 2.5rem 1rem;
}
.cs-state-text {
  margin: 0;
  font-size: 0.9rem;
  color: #6b7280;
}
.cs-state p {
  margin: 0;
}
.cs-icon {
  width: 60px;
  height: 60px;
  border-radius: 50%;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 0.25rem;
  background: radial-gradient(circle, #eef2fb, #e6ecf7);
  color: #b6bfd0;
}
.cs-icon.error {
  background: #fef2f2;
  color: #ef4444;
}
.cs-icon svg {
  width: 26px;
  height: 26px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.8;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.cs-empty-title {
  margin: 0;
  font-size: 1rem;
  font-weight: 800;
  color: #0f2444;
}
.cs-empty-hint {
  margin: 0;
  font-size: 0.86rem;
  color: #8a94a6;
}

/* Spinner */
.cs-spinner {
  width: 34px;
  height: 34px;
  border-radius: 50%;
  border: 3px solid #e6ebf4;
  border-top-color: var(--accent);
  animation: cs-spin 0.7s linear infinite;
}
@keyframes cs-spin {
  to {
    transform: rotate(360deg);
  }
}
</style>
