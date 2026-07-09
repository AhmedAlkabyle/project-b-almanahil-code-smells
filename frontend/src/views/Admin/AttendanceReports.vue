<script setup>
import { computed, onMounted, ref } from 'vue'
import { useLanguageStore } from '../../stores/language'
import AdminPageHeader from '../../components/admin/AdminPageHeader.vue'
import StatStrip from '../../components/admin/StatStrip.vue'
import AttendanceTable from '../../components/admin/attendance/AttendanceTable.vue'
import { icons } from '../../components/admin/icons'
import { getAttendanceReports } from '../../api/attendance'
import { listClasses } from '../../api/classes'
import { listSubjects } from '../../api/subjects'

const language = useLanguageStore()

// ---- Bilingual copy (per-component i18n pattern) ----
const content = {
  en: {
    class: 'Class',
    subject: 'Subject',
    from: 'From',
    to: 'To',
    allClasses: 'All classes',
    allSubjects: 'All subjects',
    clear: 'Clear filters',
    statTotal: 'Total Records',
    statPresent: 'Present',
    statAbsent: 'Absent',
    statExcused: 'Excused',
    loading: 'Loading attendance…',
    loadError: 'We couldn’t load the attendance records. Please try again.',
    retry: 'Retry',
    empty: 'No attendance records for these filters',
    emptyHint: 'Try adjusting the filters above, or check back after teachers record attendance.'
  },
  ar: {
    class: 'الصف',
    subject: 'المادة',
    from: 'من',
    to: 'إلى',
    allClasses: 'كل الصفوف',
    allSubjects: 'كل المواد',
    clear: 'مسح عوامل التصفية',
    statTotal: 'إجمالي السجلات',
    statPresent: 'حاضر',
    statAbsent: 'غائب',
    statExcused: 'بعذر',
    loading: 'جارٍ تحميل الحضور…',
    loadError: 'تعذّر تحميل سجلات الحضور. يرجى المحاولة مرة أخرى.',
    retry: 'إعادة المحاولة',
    empty: 'لا توجد سجلات حضور لهذه العوامل',
    emptyHint: 'حاول تعديل عوامل التصفية بالأعلى، أو تحقق لاحقاً بعد تسجيل المعلمين للحضور.'
  }
}
const t = computed(() => content[language.lang])

const arabicOrdinals = ['الأول', 'الثاني', 'الثالث']
// Bilingual class label for the dropdown (e.g. "1/A — Year 1 — Secondary").
function classLabel(c) {
  const bits = [c.name]
  if (c.level && c.grade) {
    bits.push(
      language.isArabic
        ? `${arabicOrdinals[c.grade - 1] ?? c.grade} ${c.level === 'HighSchool' ? 'ثانوي' : 'إعدادي'}`
        : `Year ${c.grade} — ${c.level === 'HighSchool' ? 'High School' : 'Secondary'}`
    )
  }
  return bits.filter(Boolean).join(' — ') || c.displayName || c.name
}

// ---- State ----
const records = ref([])
const classes = ref([])
const subjects = ref([]) // all subjects (filtered per class for the dropdown)
const loading = ref(true)
const loadError = ref('')

// Filters (mapped to the reports endpoint's query params).
const classFilter = ref('') // '' = all
const subjectFilter = ref('') // '' = all
const fromDate = ref('')
const toDate = ref('')

// Subjects narrowed to the chosen class (or all when no class is picked).
const filteredSubjects = computed(() => {
  if (!classFilter.value) return subjects.value
  return subjects.value.filter((s) => s.classId === Number(classFilter.value))
})

// ---- Load dropdown data (once) ----
async function loadLookups() {
  try {
    const [classesRes, subjectsRes] = await Promise.all([listClasses(), listSubjects()])
    classes.value = classesRes.data ?? []
    subjects.value = subjectsRes.data ?? []
  } catch {
    // Non-fatal: the report still works, the dropdowns are just empty.
  }
}

// ---- Load the report for the current filters (server-side) ----
function buildParams() {
  const p = {}
  if (classFilter.value) p.classId = Number(classFilter.value)
  if (subjectFilter.value) p.subjectId = Number(subjectFilter.value)
  if (fromDate.value) p.from = fromDate.value
  if (toDate.value) p.to = toDate.value
  return p
}
async function loadReports() {
  loading.value = true
  loadError.value = ''
  try {
    const { data } = await getAttendanceReports(buildParams())
    records.value = data ?? []
  } catch (err) {
    loadError.value = err.message || t.value.loadError
    records.value = []
  } finally {
    loading.value = false
  }
}

onMounted(async () => {
  await loadLookups()
  await loadReports()
})

// ---- Filter change handlers (explicit → one fetch each, no double-fetch) ----
function onClassChange() {
  // If the chosen subject no longer belongs to the new class, drop it.
  if (subjectFilter.value && !filteredSubjects.value.some((s) => s.id === Number(subjectFilter.value))) {
    subjectFilter.value = ''
  }
  loadReports()
}
function clearFilters() {
  classFilter.value = ''
  subjectFilter.value = ''
  fromDate.value = ''
  toDate.value = ''
  loadReports()
}
const hasFilters = computed(
  () => !!(classFilter.value || subjectFilter.value || fromDate.value || toDate.value)
)

// ---- Summary stats (from the returned/filtered records) ----
const byStatus = (s) => records.value.filter((r) => r.status === s).length
const statItems = computed(() => [
  { label: t.value.statTotal, value: records.value.length, variant: 'navy', icon: icons.attendance },
  { label: t.value.statPresent, value: byStatus('Present'), variant: 'green', icon: icons.check },
  { label: t.value.statAbsent, value: byStatus('Absent'), variant: 'red', icon: icons.xmark },
  { label: t.value.statExcused, value: byStatus('Excused'), variant: 'amber', icon: icons.clock }
])
</script>

<template>
  <div class="attendance-reports" :dir="language.dir">
    <AdminPageHeader />

    <!-- Summary stats reflect the current filter -->
    <StatStrip :items="statItems" />

    <!-- Reports panel: filters + table unified in one card -->
    <div class="panel">
      <!-- Filter bar -->
      <div class="filter-bar">
        <div class="f-field">
          <label>{{ t.class }}</label>
          <select v-model="classFilter" class="f-select" @change="onClassChange">
            <option value="">{{ t.allClasses }}</option>
            <option v-for="c in classes" :key="c.id" :value="c.id">{{ classLabel(c) }}</option>
          </select>
        </div>

        <div class="f-field">
          <label>{{ t.subject }}</label>
          <select v-model="subjectFilter" class="f-select" @change="loadReports">
            <option value="">{{ t.allSubjects }}</option>
            <option v-for="s in filteredSubjects" :key="s.id" :value="s.id">{{ s.name }}</option>
          </select>
        </div>

        <div class="f-field">
          <label>{{ t.from }}</label>
          <input v-model="fromDate" type="date" class="f-date" @change="loadReports" />
        </div>

        <div class="f-field">
          <label>{{ t.to }}</label>
          <input v-model="toDate" type="date" class="f-date" @change="loadReports" />
        </div>

        <button v-if="hasFilters" type="button" class="clear-btn" @click="clearFilters">
          <svg viewBox="0 0 24 24"><path d="M18 6 6 18M6 6l12 12" /></svg>
          {{ t.clear }}
        </button>
      </div>

      <!-- Body: loading / error / empty / table -->
      <div class="panel-body">
        <div v-if="loading" class="state-block">
          <span class="spinner"></span>
          <p>{{ t.loading }}</p>
        </div>

        <div v-else-if="loadError" class="state-block error-state">
          <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="9" /><path d="M12 8v5M12 16h.01" /></svg>
          <p>{{ loadError }}</p>
          <button type="button" class="retry-btn" @click="loadReports">{{ t.retry }}</button>
        </div>

        <div v-else-if="!records.length" class="state-block">
          <span class="empty-badge"><svg viewBox="0 0 24 24"><rect x="3" y="4" width="18" height="17" rx="2" /><path d="M8 2v4M16 2v4M3 10h18" /></svg></span>
          <p class="empty-title">{{ t.empty }}</p>
          <p class="empty-hint">{{ t.emptyHint }}</p>
        </div>

        <AttendanceTable v-else :records="records" />
      </div>
    </div>
  </div>
</template>

<style scoped>
.attendance-reports {
  --navy: var(--ds-navy);
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

/* Panel — filters + table unified in one elevated card */
.panel {
  position: relative;
  background: #fff;
  border: 1px solid #eaeef6;
  border-radius: 18px;
  box-shadow: 0 8px 22px rgba(30, 41, 59, 0.05);
  overflow: hidden;
  animation: panel-rise 0.5s ease both;
}
.panel::before {
  content: '';
  position: absolute;
  top: 0;
  inset-inline: 0;
  height: 3px;
  z-index: 2;
  background: var(--ds-accent-bar, linear-gradient(90deg, #1e4c9a, #f2a03d));
}
@keyframes panel-rise {
  from {
    opacity: 0;
    transform: translateY(14px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}
@media (prefers-reduced-motion: reduce) {
  .panel {
    animation: none;
  }
}

/* Filter bar */
.filter-bar {
  display: flex;
  align-items: flex-end;
  gap: 0.8rem;
  flex-wrap: wrap;
  padding: 1rem 1.25rem;
  background: #fbfcfe;
  border-bottom: 1px solid #eef1f7;
}
.f-field {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  min-width: 150px;
}
.f-field label {
  font-size: 0.72rem;
  font-weight: 800;
  letter-spacing: 0.04em;
  text-transform: uppercase;
  color: #8a94a6;
  padding-inline-start: 0.15rem;
}
.f-select,
.f-date {
  padding: 0.55rem 0.85rem;
  font-size: 0.88rem;
  font-family: inherit;
  font-weight: 600;
  color: #334155;
  background: #fff;
  border: 1px solid #e6e9f2;
  border-radius: 10px;
  cursor: pointer;
  outline: none;
  transition: border-color 0.2s ease, box-shadow 0.2s ease;
}
.f-select:focus,
.f-date:focus {
  border-color: var(--navy);
  box-shadow: 0 0 0 3px rgba(30, 76, 154, 0.12);
}
.clear-btn {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.55rem 0.9rem;
  border: 1px solid #e2e8f2;
  border-radius: 10px;
  background: #fff;
  color: #64748b;
  font-family: inherit;
  font-size: 0.83rem;
  font-weight: 700;
  cursor: pointer;
  transition: background 0.15s ease, color 0.15s ease, border-color 0.15s ease;
}
.clear-btn svg {
  width: 14px;
  height: 14px;
  fill: none;
  stroke: currentColor;
  stroke-width: 2.2;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.clear-btn:hover {
  color: #dc2626;
  border-color: #f6c9c9;
  background: #fef2f2;
}

/* Body + states */
.panel-body {
  min-height: 120px;
}
.state-block {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  gap: 0.55rem;
  padding: 3.25rem 1.5rem;
  color: #6b7280;
  font-size: 0.92rem;
}
.state-block p {
  margin: 0;
}
.state-block.error-state {
  color: #b91c1c;
}
.state-block.error-state svg {
  width: 34px;
  height: 34px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.7;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.empty-badge {
  width: 64px;
  height: 64px;
  border-radius: 50%;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 0.35rem;
  background: radial-gradient(circle, #fff4e6, #ffe7c9);
  color: #eb9a34;
  box-shadow: 0 8px 18px rgba(242, 160, 61, 0.18);
}
.empty-badge svg {
  width: 28px;
  height: 28px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.8;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.empty-title {
  font-size: 1.05rem;
  font-weight: 800;
  color: #0f2444;
}
.empty-hint {
  font-size: 0.88rem;
  color: #8a94a6;
  max-width: 420px;
}
.spinner {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  border: 3px solid #e2e8f0;
  border-top-color: var(--navy);
  animation: spin 0.8s linear infinite;
}
@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}
.retry-btn {
  margin-top: 0.3rem;
  padding: 0.55rem 1.3rem;
  border: 1px solid #e2e8f2;
  border-radius: 10px;
  background: #fff;
  font-family: inherit;
  font-size: 0.85rem;
  font-weight: 700;
  color: var(--navy);
  cursor: pointer;
  transition: background 0.15s ease;
}
.retry-btn:hover {
  background: #eef4ff;
}
@media (prefers-reduced-motion: reduce) {
  .spinner {
    animation-duration: 1.6s;
  }
}

@media (max-width: 560px) {
  .filter-bar {
    flex-direction: column;
    align-items: stretch;
  }
  .f-field {
    min-width: 0;
  }
  .clear-btn {
    justify-content: center;
  }
}
</style>
