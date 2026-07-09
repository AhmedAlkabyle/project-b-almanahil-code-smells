<script setup>
import { computed, reactive, ref, watch } from 'vue'
import { useLanguageStore } from '../../../stores/language'
import BaseModal from '../../common/BaseModal.vue'
import { getTimetable, saveTimetable } from '../../../api/timetable'
import { listSubjects } from '../../../api/subjects'

const props = defineProps({
  open: { type: Boolean, default: false },
  // The class row (needs at least id; name/displayName/level used for the title).
  classItem: { type: Object, default: null }
})
const emit = defineEmits(['close'])

const language = useLanguageStore()

// ---- Bilingual copy (per-component i18n pattern) ----
const content = {
  en: {
    title: 'Class Timetable',
    period: 'Period',
    loading: 'Loading timetable…',
    loadError: 'We couldn’t load the timetable. Please try again.',
    retry: 'Retry',
    noSubjects: 'This class has no subjects yet. Add subjects first, then build its timetable.',
    empty: '—',
    clearAll: 'Clear all',
    cancel: 'Close',
    save: 'Save Timetable',
    saving: 'Saving…',
    saved: 'Timetable saved.'
  },
  ar: {
    title: 'جدول الحصص',
    period: 'الحصة',
    loading: 'جارٍ تحميل الجدول…',
    loadError: 'تعذّر تحميل الجدول. يرجى المحاولة مرة أخرى.',
    retry: 'إعادة المحاولة',
    noSubjects: 'لا توجد مواد لهذا الصف بعد. أضف المواد أولاً، ثم أنشئ جدوله.',
    empty: '—',
    clearAll: 'مسح الكل',
    cancel: 'إغلاق',
    save: 'حفظ الجدول',
    saving: 'جارٍ الحفظ…',
    saved: 'تم حفظ الجدول.'
  }
}
const t = computed(() => content[language.lang])

// ---- State ----
const loading = ref(true)
const loadError = ref('')
const meta = ref(null) // the GET payload: { days, periods, break, periodsPerDay, classDisplayName, ... }
const subjects = ref([]) // this class's subjects (dropdown options)
const cells = ref({}) // key `${day}-${period}` -> subjectId (number) | '' (empty)

const saving = ref(false)
const saveError = ref('') // clash / validation banner (from the backend)
const clashes = ref([]) // structured clash list: [{ subjectName, day, period, otherClassName }]
const savedFlash = ref(false)
let flashTimer = null

const cellKey = (day, period) => `${day}-${period}`

// The class label shown under the modal title, composed in the CURRENT language
// (the backend's classDisplayName is always Arabic).
const arabicOrdinals = ['الأول', 'الثاني', 'الثالث'] // grade 1..3
const heading = computed(() => {
  const c = props.classItem
  if (!c) return meta.value?.classDisplayName || ''
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
  return label || meta.value?.classDisplayName || c.name || ''
})

// Day column header text for the current language.
const dayName = (d) => (language.isArabic ? d.nameAr : d.nameEn)

// The grid rows: one row per period, with the breakfast break row spliced in after
// the period the API says it follows (break.afterPeriod).
const periodRows = computed(() => {
  if (!meta.value) return []
  const brk = meta.value.break
  const rows = []
  for (const p of meta.value.periods) {
    rows.push({ type: 'period', period: p.period, startTime: p.startTime, endTime: p.endTime })
    if (brk && brk.startTime && brk.afterPeriod === p.period) {
      rows.push({
        type: 'break',
        label: language.isArabic ? brk.labelAr : brk.labelEn,
        startTime: brk.startTime,
        endTime: brk.endTime
      })
    }
  }
  return rows
})

// Cells to highlight red = the offending (day, period) pairs from the last clash.
const clashKeys = computed(() => new Set(clashes.value.map((c) => cellKey(c.day, c.period))))
const isClash = (day, period) => clashKeys.value.has(cellKey(day, period))

const hasSubjects = computed(() => subjects.value.length > 0)
const canSave = computed(() => !loading.value && !loadError.value && !saving.value && hasSubjects.value)

// ---- Load (GET timetable + this class's subjects) ----
async function load() {
  const classId = props.classItem?.id
  if (!classId) return
  loading.value = true
  loadError.value = ''
  saveError.value = ''
  clashes.value = []
  try {
    const [tt, subs] = await Promise.all([getTimetable(classId), listSubjects({ classId })])
    meta.value = tt.data
    subjects.value = subs.data ?? []

    // Seed local cell state from the full grid the API returns (empties included).
    const built = {}
    for (const s of meta.value.slots ?? []) built[cellKey(s.day, s.period)] = s.subjectId ?? ''
    cells.value = built
  } catch (err) {
    loadError.value = err.message || t.value.loadError
  } finally {
    loading.value = false
  }
}

// Reset + load each time the modal opens.
watch(
  () => props.open,
  (open) => {
    clearTimeout(flashTimer)
    savedFlash.value = false
    if (open) load()
  }
)

// ---- Actions ----
function clearAll() {
  const cleared = {}
  for (const key of Object.keys(cells.value)) cleared[key] = ''
  cells.value = cleared
  saveError.value = ''
  clashes.value = []
}

// Build the PUT body from every cell of the grid (empty cells sent as null so removed
// subjects are cleared server-side).
function buildSlots() {
  const slots = []
  for (const d of meta.value.days) {
    for (const p of meta.value.periods) {
      const v = cells.value[cellKey(d.day, p.period)]
      slots.push({
        day: d.day,
        period: p.period,
        subjectId: v === '' || v == null ? null : Number(v)
      })
    }
  }
  return slots
}

async function save() {
  if (!canSave.value) return
  saving.value = true
  saveError.value = ''
  clashes.value = []
  savedFlash.value = false
  try {
    const res = await saveTimetable(meta.value.classId, buildSlots())
    if (res.ok) {
      // Keep the modal open showing the saved state; flash a success banner.
      savedFlash.value = true
      clearTimeout(flashTimer)
      flashTimer = setTimeout(() => (savedFlash.value = false), 3500)
    } else {
      // Clash (or other validation): show the friendly message + highlight the cells.
      saveError.value = res.message
      clashes.value = res.clashes ?? []
    }
  } catch (err) {
    saveError.value = err.message
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <BaseModal :open="open" size="xl" :title="t.title" :subtitle="heading" @close="emit('close')">
    <!-- Loading -->
    <div v-if="loading" class="tt-state">
      <span class="tt-spinner"></span>
      <p>{{ t.loading }}</p>
    </div>

    <!-- Load error -->
    <div v-else-if="loadError" class="tt-state error">
      <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="9" /><path d="M12 8v5M12 16h.01" /></svg>
      <p>{{ loadError }}</p>
      <button type="button" class="tt-retry" @click="load">{{ t.retry }}</button>
    </div>

    <template v-else>
      <!-- Success flash -->
      <Transition name="tt-fade">
        <div v-if="savedFlash" class="tt-banner success">
          <svg viewBox="0 0 24 24"><path d="M20 6 9 17l-5-5" /></svg>
          <span>{{ t.saved }}</span>
        </div>
      </Transition>

      <!-- Clash / error banner (prominent, red) -->
      <Transition name="tt-fade">
        <div v-if="saveError" class="tt-banner error">
          <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="9" /><path d="M12 8v5M12 16h.01" /></svg>
          <span>{{ saveError }}</span>
        </div>
      </Transition>

      <!-- No-subjects note -->
      <div v-if="!hasSubjects" class="note">
        <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="9" /><path d="M12 8v5M12 16h.01" /></svg>
        <span>{{ t.noSubjects }}</span>
      </div>

      <!-- Grid -->
      <div class="tt-scroll">
        <table class="tt">
          <thead>
            <tr>
              <th class="tt-corner">{{ t.period }}</th>
              <th v-for="d in meta.days" :key="d.day">{{ dayName(d) }}</th>
            </tr>
          </thead>
          <tbody>
            <template v-for="row in periodRows" :key="row.type + '-' + (row.period ?? row.startTime)">
              <!-- A period row -->
              <tr v-if="row.type === 'period'">
                <th class="tt-period" scope="row">
                  <span class="pn">{{ t.period }} {{ row.period }}</span>
                  <span class="pt">{{ row.startTime }}–{{ row.endTime }}</span>
                </th>
                <td
                  v-for="d in meta.days"
                  :key="d.day"
                  class="tt-cell"
                  :class="{ clash: isClash(d.day, row.period) }"
                >
                  <select v-model="cells[cellKey(d.day, row.period)]" :disabled="saving">
                    <option value="">{{ t.empty }}</option>
                    <option v-for="s in subjects" :key="s.id" :value="s.id">{{ s.name }}</option>
                  </select>
                </td>
              </tr>

              <!-- The breakfast break divider (not editable) -->
              <tr v-else class="tt-break-row">
                <td class="tt-break" :colspan="meta.days.length + 1">
                  <svg viewBox="0 0 24 24"><path d="M17 8h1a4 4 0 1 1 0 8h-1" /><path d="M3 8h14v9a4 4 0 0 1-4 4H7a4 4 0 0 1-4-4Z" /><path d="M6 2v2M10 2v2M14 2v2" /></svg>
                  <span>{{ row.label }} · {{ row.startTime }}–{{ row.endTime }}</span>
                </td>
              </tr>
            </template>
          </tbody>
        </table>
      </div>
    </template>

    <template #footer>
      <button type="button" class="btn ghost tt-clear" @click="clearAll" :disabled="saving || loading || loadError">
        {{ t.clearAll }}
      </button>
      <button type="button" class="btn ghost" @click="emit('close')" :disabled="saving">{{ t.cancel }}</button>
      <button type="button" class="btn primary" @click="save" :disabled="!canSave">
        {{ saving ? t.saving : t.save }}
      </button>
    </template>
  </BaseModal>
</template>

<style scoped>
/* ---- Banners ---- */
.tt-banner {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  padding: 0.8rem 1rem;
  border-radius: 12px;
  font-size: 0.88rem;
  font-weight: 600;
  line-height: 1.45;
}
.tt-banner svg {
  width: 20px;
  height: 20px;
  flex-shrink: 0;
  fill: none;
  stroke: currentColor;
  stroke-width: 2;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.tt-banner.success {
  color: #15803d;
  background: #f0fdf4;
  border: 1px solid #bbf7d0;
}
.tt-banner.error {
  color: #b91c1c;
  background: #fef2f2;
  border: 1px solid #fecaca;
}
.tt-fade-enter-active,
.tt-fade-leave-active {
  transition: opacity 0.2s ease, transform 0.2s ease;
}
.tt-fade-enter-from,
.tt-fade-leave-to {
  opacity: 0;
  transform: translateY(-5px);
}

/* Info note icon sizing. BaseModal styles the .note box; without this the inline
   <svg> renders at its default size as a solid black circle (the bug this fixes). */
.note svg {
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

/* ---- Grid ---- */
.tt-scroll {
  max-height: 60vh;
  overflow: auto;
  border: 1px solid #e6ebf4;
  border-radius: 14px;
}
.tt {
  width: 100%;
  border-collapse: separate;
  border-spacing: 0;
  min-width: 560px;
  font-size: 0.85rem;
}
/* Sticky day headers */
.tt thead th {
  position: sticky;
  top: 0;
  z-index: 2;
  padding: 0.7rem 0.6rem;
  background: linear-gradient(135deg, #1e4c9a, #2f63ba);
  color: #fff;
  font-size: 0.74rem;
  font-weight: 800;
  letter-spacing: 0.03em;
  text-transform: uppercase;
  white-space: nowrap;
  text-align: center;
  border-inline-end: 1px solid rgba(255, 255, 255, 0.16);
}
.tt thead th:last-child {
  border-inline-end: none;
}
.tt-corner {
  text-align: start !important;
}

/* Period (row) label */
.tt-period {
  display: flex;
  flex-direction: column;
  justify-content: center;
  gap: 0.1rem;
  padding: 0.55rem 0.7rem;
  background: #f7f9fc;
  border-inline-end: 1px solid #eef1f7;
  border-bottom: 1px solid #eef1f7;
  text-align: start;
  white-space: nowrap;
  min-width: 118px;
}
.tt-period .pn {
  font-weight: 800;
  color: #0f2444;
  font-size: 0.82rem;
}
.tt-period .pt {
  font-size: 0.72rem;
  font-weight: 600;
  color: #94a3b8;
}

/* Editable subject cell */
.tt-cell {
  padding: 0.35rem;
  border-inline-end: 1px solid #f1f4f9;
  border-bottom: 1px solid #f1f4f9;
  vertical-align: middle;
}
.tt-cell:last-child {
  border-inline-end: none;
}
/* Last row sits flush against the container's rounded border (no double line). */
.tt tbody tr:last-child .tt-cell,
.tt tbody tr:last-child .tt-period,
.tt tbody tr:last-child .tt-break {
  border-bottom: none;
}
.tt-cell select {
  width: 100%;
  box-sizing: border-box;
  padding: 0.45rem 0.5rem;
  font-size: 0.82rem;
  font-family: inherit;
  color: #0f2444;
  background: #f9fafc;
  border: 1.5px solid #e6ebf4;
  border-radius: 9px;
  outline: none;
  cursor: pointer;
  transition: border-color 0.15s ease, box-shadow 0.15s ease, background 0.15s ease;
}
.tt-cell select:focus {
  background: #fff;
  border-color: var(--ds-navy);
  box-shadow: 0 0 0 3px rgba(30, 76, 154, 0.14);
}
.tt-cell select:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
/* Clash highlight — the offending cell(s) turn red so the admin sees what to change. */
.tt-cell.clash select {
  border-color: #ef4444;
  background: #fef2f2;
  box-shadow: 0 0 0 3px rgba(239, 68, 68, 0.16);
}

/* Breakfast break divider row */
.tt-break-row .tt-break {
  padding: 0.5rem 0.9rem;
  text-align: center;
  border-bottom: 1px solid #f1f4f9;
  background: repeating-linear-gradient(-45deg, #fff7ed, #fff7ed 10px, #ffedd5 10px, #ffedd5 20px);
  color: #9a3412;
  font-weight: 800;
  font-size: 0.8rem;
  letter-spacing: 0.02em;
}
.tt-break span {
  vertical-align: middle;
}
.tt-break svg {
  width: 16px;
  height: 16px;
  margin-inline-end: 0.4rem;
  vertical-align: middle;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.9;
  stroke-linecap: round;
  stroke-linejoin: round;
}

/* Push "Clear all" to the far (start) side of the footer. */
.tt-clear {
  margin-inline-end: auto;
}

/* ---- Loading / error states ---- */
.tt-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 0.7rem;
  padding: 3rem 1rem;
  color: #6b7280;
  font-size: 0.92rem;
}
.tt-state.error {
  color: #b91c1c;
}
.tt-state.error svg {
  width: 34px;
  height: 34px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.7;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.tt-state p {
  margin: 0;
}
.tt-spinner {
  width: 30px;
  height: 30px;
  border-radius: 50%;
  border: 3px solid #e2e8f0;
  border-top-color: var(--ds-navy);
  animation: tt-spin 0.8s linear infinite;
}
@keyframes tt-spin {
  to {
    transform: rotate(360deg);
  }
}
.tt-retry {
  margin-top: 0.2rem;
  padding: 0.5rem 1.2rem;
  border: 1px solid #e2e8f2;
  border-radius: 10px;
  background: #fff;
  font-family: inherit;
  font-size: 0.85rem;
  font-weight: 700;
  color: var(--ds-navy);
  cursor: pointer;
}
.tt-retry:hover {
  background: #eef4ff;
}
@media (prefers-reduced-motion: reduce) {
  .tt-spinner {
    animation-duration: 1.6s;
  }
}
</style>
