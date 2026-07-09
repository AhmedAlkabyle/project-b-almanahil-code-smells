<script setup>
import { computed, ref, watch } from 'vue'
import { useLanguageStore } from '../../../stores/language'
import BaseModal from '../../common/BaseModal.vue'
import { listUnassignedStudents } from '../../../api/users'
import { enrollStudents } from '../../../api/classes'

const props = defineProps({
  open: { type: Boolean, default: false },
  // The class students are being enrolled into.
  classItem: { type: Object, default: null }
})
const emit = defineEmits(['close', 'enrolled'])

const language = useLanguageStore()

// ---- Bilingual copy (per-component i18n pattern) ----
const content = {
  en: {
    title: 'Assign Students',
    subtitlePrefix: 'Enroll unassigned students into',
    searchPh: 'Search by name or email…',
    selectAll: 'Select all',
    loading: 'Loading students…',
    loadError: 'We couldn’t load the students. Please try again.',
    retry: 'Retry',
    emptyTitle: 'No unassigned students available',
    emptyHint: 'Every student is already enrolled in a class.',
    noMatch: 'No students match your search.',
    cancel: 'Cancel',
    enroll: 'Enroll Selected',
    enrolling: 'Enrolling…',
    pickOne: 'Please select at least one student.'
  },
  ar: {
    title: 'إسناد الطلاب',
    subtitlePrefix: 'تسجيل الطلاب غير المسندين في',
    searchPh: 'ابحث بالاسم أو البريد…',
    selectAll: 'تحديد الكل',
    loading: 'جارٍ تحميل الطلاب…',
    loadError: 'تعذّر تحميل الطلاب. يرجى المحاولة مرة أخرى.',
    retry: 'إعادة المحاولة',
    emptyTitle: 'لا يوجد طلاب غير مسندين',
    emptyHint: 'جميع الطلاب مسجّلون بالفعل في صفوف.',
    noMatch: 'لا يوجد طلاب مطابقون لبحثك.',
    cancel: 'إلغاء',
    enroll: 'تسجيل المحدد',
    enrolling: 'جارٍ التسجيل…',
    pickOne: 'يرجى اختيار طالب واحد على الأقل.'
  }
}
const t = computed(() => content[language.lang])

// ---- State ----
const students = ref([]) // unassigned students loaded from the API
const loading = ref(true)
const loadError = ref('')
const search = ref('')
const selected = ref([]) // selected student ids
const submitting = ref(false)
const apiError = ref('')

// The nice class label shown in the subtitle (falls back to the short name).
const classLabel = computed(() => props.classItem?.displayName || props.classItem?.name || '')

// Filter by name + email (case-insensitive).
const filtered = computed(() => {
  const q = search.value.trim().toLowerCase()
  if (!q) return students.value
  return students.value.filter((s) => `${s.fullName} ${s.email}`.toLowerCase().includes(q))
})

// "Select all" acts on the currently visible (filtered) rows only.
const allVisibleSelected = computed(
  () => filtered.value.length > 0 && filtered.value.every((s) => selected.value.includes(s.id))
)

const isChecked = (id) => selected.value.includes(id)

function toggle(id) {
  selected.value = selected.value.includes(id)
    ? selected.value.filter((x) => x !== id)
    : [...selected.value, id]
}

function toggleAllVisible() {
  if (allVisibleSelected.value) {
    const visible = new Set(filtered.value.map((s) => s.id))
    selected.value = selected.value.filter((id) => !visible.has(id))
  } else {
    selected.value = [...new Set([...selected.value, ...filtered.value.map((s) => s.id)])]
  }
}

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
    const { data } = await listUnassignedStudents()
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
    selected.value = []
    apiError.value = ''
    submitting.value = false
    load()
  }
)

async function submit() {
  apiError.value = ''
  if (!selected.value.length) {
    apiError.value = t.value.pickOne
    return
  }
  submitting.value = true
  try {
    const res = await enrollStudents(props.classItem.id, selected.value)
    emit('enrolled', { message: res.message })
  } catch (err) {
    apiError.value = err.message
  } finally {
    submitting.value = false
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
    <div class="as-body">
      <!-- Inline API error (e.g. enroll failed) -->
      <div v-if="apiError" class="form-error">
        <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="9" /><path d="M12 8v5M12 16h.01" /></svg>
        <span>{{ apiError }}</span>
      </div>

      <!-- Loading -->
      <div v-if="loading" class="as-state">
        <span class="as-spinner"></span>
        <p>{{ t.loading }}</p>
      </div>

      <!-- Load error -->
      <div v-else-if="loadError" class="as-state">
        <span class="as-icon error">
          <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="9" /><path d="M12 8v5M12 16h.01" /></svg>
        </span>
        <p class="as-state-text">{{ loadError }}</p>
        <button type="button" class="btn ghost" @click="load">{{ t.retry }}</button>
      </div>

      <!-- Empty: no unassigned students at all -->
      <div v-else-if="!students.length" class="as-state">
        <span class="as-icon">
          <svg viewBox="0 0 24 24"><path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" /><circle cx="9" cy="7" r="4" /><path d="M22 11h-6" /></svg>
        </span>
        <p class="as-empty-title">{{ t.emptyTitle }}</p>
        <p class="as-empty-hint">{{ t.emptyHint }}</p>
      </div>

      <!-- Loaded list -->
      <template v-else>
        <!-- Search -->
        <label class="as-search">
          <svg viewBox="0 0 24 24"><circle cx="11" cy="11" r="7" /><path d="m21 21-4.3-4.3" /></svg>
          <input v-model="search" type="text" :placeholder="t.searchPh" />
        </label>

        <!-- Toolbar: select-all + selected count -->
        <div class="as-toolbar">
          <label class="as-selall">
            <input type="checkbox" :checked="allVisibleSelected" @change="toggleAllVisible" />
            <span>{{ t.selectAll }}</span>
          </label>
          <span class="as-count">{{ selected.length }}</span>
        </div>

        <!-- Student list -->
        <div v-if="filtered.length" class="as-list">
          <label
            v-for="s in filtered"
            :key="s.id"
            class="as-row"
            :class="{ checked: isChecked(s.id) }"
          >
            <input type="checkbox" class="as-native" :checked="isChecked(s.id)" @change="toggle(s.id)" />
            <span class="as-tick">
              <svg viewBox="0 0 24 24"><path d="M20 6 9 17l-5-5" /></svg>
            </span>
            <span class="as-avatar">{{ initials(s.fullName) }}</span>
            <span class="as-info">
              <span class="as-name">{{ s.fullName }}</span>
              <span class="as-email">{{ s.email }}</span>
            </span>
          </label>
        </div>

        <!-- No search match -->
        <div v-else class="as-nomatch">{{ t.noMatch }}</div>
      </template>
    </div>

    <template #footer>
      <button type="button" class="btn ghost" @click="emit('close')" :disabled="submitting">
        {{ t.cancel }}
      </button>
      <button
        type="button"
        class="btn primary"
        @click="submit"
        :disabled="submitting || loading || !selected.length"
      >
        {{ submitting ? t.enrolling : `${t.enroll}${selected.length ? ` (${selected.length})` : ''}` }}
      </button>
    </template>
  </BaseModal>
</template>

<style scoped>
.as-body {
  --accent: var(--ds-navy, #1e4c9a);
  --accent-ring: rgba(30, 76, 154, 0.14);
  display: flex;
  flex-direction: column;
  gap: 0.9rem;
}

/* Inline API error (matches the Add User / Add Class banner) */
.form-error {
  display: flex;
  align-items: flex-start;
  gap: 0.6rem;
  padding: 0.85rem 0.95rem;
  border-radius: 12px;
  background: #fef2f2;
  border: 1px solid #fecaca;
  color: #b91c1c;
  font-size: 0.85rem;
  font-weight: 600;
  line-height: 1.45;
}
.form-error svg {
  width: 18px;
  height: 18px;
  flex-shrink: 0;
  margin-top: 1px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.9;
  stroke-linecap: round;
  stroke-linejoin: round;
}

/* Search box */
.as-search {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.7rem 0.95rem;
  background: #f4f7fc;
  border: 1.5px solid #e6ebf4;
  border-radius: 14px;
  transition: border-color 0.2s ease, box-shadow 0.2s ease, background 0.2s ease;
}
.as-search:focus-within {
  background: #fff;
  border-color: var(--accent);
  box-shadow: 0 0 0 4px var(--accent-ring);
}
.as-search svg {
  width: 17px;
  height: 17px;
  flex-shrink: 0;
  fill: none;
  stroke: #9ca3af;
  stroke-width: 1.9;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.as-search input {
  width: 100%;
  min-width: 0;
  border: none;
  outline: none;
  background: transparent;
  font-family: inherit;
  font-size: 0.92rem;
  color: #0f2444;
}

/* Toolbar */
.as-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  padding: 0 0.15rem;
}
.as-selall {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.85rem;
  font-weight: 700;
  color: #47536a;
  cursor: pointer;
  user-select: none;
}
.as-selall input {
  width: 16px;
  height: 16px;
  accent-color: var(--accent);
  cursor: pointer;
}
.as-count {
  min-width: 26px;
  padding: 0.2rem 0.6rem;
  border-radius: 999px;
  text-align: center;
  font-size: 0.78rem;
  font-weight: 800;
  color: var(--accent);
  background: #e0ecff;
}

/* Scrollable list */
.as-list {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  max-height: 320px;
  overflow-y: auto;
  padding: 0.15rem;
  margin: -0.15rem;
}
.as-row {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.6rem 0.8rem;
  border: 1.5px solid #e6ebf4;
  border-radius: 13px;
  background: #fff;
  cursor: pointer;
  transition: border-color 0.15s ease, background 0.15s ease, box-shadow 0.15s ease;
}
.as-row:hover {
  border-color: #cdd9ee;
  background: #f9fbff;
}
.as-row.checked {
  border-color: var(--accent);
  background: #eef4ff;
  box-shadow: 0 0 0 3px var(--accent-ring);
}

/* Hide the native checkbox but keep it accessible + functional */
.as-native {
  position: absolute;
  opacity: 0;
  width: 0;
  height: 0;
  pointer-events: none;
}

/* Custom check indicator */
.as-tick {
  flex-shrink: 0;
  width: 22px;
  height: 22px;
  border-radius: 7px;
  border: 2px solid #cbd5e1;
  background: #fff;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  transition: background 0.15s ease, border-color 0.15s ease;
}
.as-tick svg {
  width: 14px;
  height: 14px;
  fill: none;
  stroke: #fff;
  stroke-width: 3;
  stroke-linecap: round;
  stroke-linejoin: round;
  opacity: 0;
  transition: opacity 0.15s ease;
}
.as-row.checked .as-tick {
  background: var(--accent);
  border-color: var(--accent);
}
.as-row.checked .as-tick svg {
  opacity: 1;
}

/* Avatar chip */
.as-avatar {
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
.as-info {
  display: flex;
  flex-direction: column;
  line-height: 1.3;
  min-width: 0;
}
.as-name {
  font-weight: 700;
  font-size: 0.92rem;
  color: #0f2444;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.as-email {
  font-size: 0.78rem;
  color: #8a94a6;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

/* No-search-match line */
.as-nomatch {
  padding: 1.5rem 1rem;
  text-align: center;
  font-size: 0.88rem;
  color: #8a94a6;
}

/* Shared state block (loading / error / empty) */
.as-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  gap: 0.5rem;
  padding: 2.5rem 1rem;
}
.as-state-text {
  margin: 0;
  font-size: 0.9rem;
  color: #6b7280;
}
.as-state p {
  margin: 0;
}
.as-icon {
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
.as-icon.error {
  background: #fef2f2;
  color: #ef4444;
}
.as-icon svg {
  width: 26px;
  height: 26px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.8;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.as-empty-title {
  margin: 0;
  font-size: 1rem;
  font-weight: 800;
  color: #0f2444;
}
.as-empty-hint {
  margin: 0;
  font-size: 0.86rem;
  color: #8a94a6;
}

/* Spinner */
.as-spinner {
  width: 34px;
  height: 34px;
  border-radius: 50%;
  border: 3px solid #e6ebf4;
  border-top-color: var(--accent);
  animation: as-spin 0.7s linear infinite;
}
@keyframes as-spin {
  to {
    transform: rotate(360deg);
  }
}
</style>
