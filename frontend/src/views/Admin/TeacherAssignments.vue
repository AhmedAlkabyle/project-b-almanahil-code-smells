<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { useLanguageStore } from '../../stores/language'
import AdminPageHeader from '../../components/admin/AdminPageHeader.vue'
import StatStrip from '../../components/admin/StatStrip.vue'
import { icons } from '../../components/admin/icons'
import AssignmentsTable from '../../components/admin/assignments/AssignmentsTable.vue'
import AssignmentFormModal from '../../components/admin/assignments/AssignmentFormModal.vue'
import ConfirmDialog from '../../components/common/ConfirmDialog.vue'
import { listAssignments, removeAssignment } from '../../api/assignments'
import { listUsers } from '../../api/users'
import { listSubjects } from '../../api/subjects'
import { listClasses } from '../../api/classes'

const language = useLanguageStore()

// ---- Bilingual copy (per-component i18n pattern) ----
const content = {
  en: {
    searchPh: 'Search by teacher or subject…',
    assignSubject: 'Assign Subject',
    allLevels: 'All levels',
    levels: { Secondary: 'Secondary', HighSchool: 'High School' },
    allYears: 'All years',
    allSections: 'All sections',
    section: 'Section',
    statAssignments: 'Assignments',
    statTeachers: 'Teachers Assigned',
    statSubjects: 'Subjects Covered',
    statUnassigned: 'Unassigned Teachers',
    loading: 'Loading assignments…',
    loadError: 'We couldn’t load the assignments. Please try again.',
    retry: 'Retry',
    noMatch: 'No assignments match your filters.',
    confirmTitle: 'Remove assignment?',
    confirmMsg: 'This teacher will no longer be assigned to this subject. No data is deleted.',
    confirmYes: 'Remove',
    cancel: 'Cancel'
  },
  ar: {
    searchPh: 'ابحث بالمعلم أو المادة…',
    assignSubject: 'تعيين مادة',
    allLevels: 'كل المراحل',
    levels: { Secondary: 'إعدادي', HighSchool: 'ثانوي' },
    allYears: 'كل السنوات',
    allSections: 'كل الشعب',
    section: 'الشعبة',
    statAssignments: 'التعيينات',
    statTeachers: 'المعلمون المعيّنون',
    statSubjects: 'المواد المشمولة',
    statUnassigned: 'معلمون غير معيّنين',
    loading: 'جارٍ تحميل التعيينات…',
    loadError: 'تعذّر تحميل التعيينات. يرجى المحاولة مرة أخرى.',
    retry: 'إعادة المحاولة',
    noMatch: 'لا توجد تعيينات مطابقة لبحثك.',
    confirmTitle: 'إزالة التعيين؟',
    confirmMsg: 'لن يعود هذا المعلم معيّنًا لهذه المادة. لن يتم حذف أي بيانات.',
    confirmYes: 'إزالة',
    cancel: 'إلغاء'
  }
}
const t = computed(() => content[language.lang])

const LEVELS = ['Secondary', 'HighSchool']
const arabicOrdinals = ['الأول', 'الثاني', 'الثالث'] // grade 1..3

// ---- State ----
// assignments already carry teacherName / subjectName / className from the API.
const assignments = ref([])
const teachers = ref([]) // { id, name, level } for the cascade modal
const subjects = ref([]) // { id, name, classId, ... }
const classes = ref([]) // full class objects (level, grade, section…) for the cascade + filters
const loading = ref(true)
const loadError = ref('')
const search = ref('')
const levelFilter = ref('') // '' = all levels
const yearFilter = ref('') // '' = all years (grade as a string)
const sectionFilter = ref('') // '' = all sections

// ---- Flash message (auto-dismisses) ----
const flashMsg = ref(null)
let flashTimer = null
function flash(type, text) {
  flashMsg.value = { type, text }
  clearTimeout(flashTimer)
  flashTimer = setTimeout(() => (flashMsg.value = null), 4500)
}

// ---- Load ----
async function loadAll() {
  loading.value = true
  loadError.value = ''
  try {
    const [assignRes, teachersRes, subjectsRes, classesRes] = await Promise.all([
      listAssignments(),
      listUsers({ role: 'Teacher' }),
      listSubjects(),
      listClasses()
    ])
    assignments.value = assignRes.data ?? []
    // Map teacher users → { id, name, level } — level (TeacherLevel) drives the cascade.
    teachers.value = (teachersRes.data ?? []).map((u) => ({
      id: u.id,
      name: u.fullName,
      level: u.teacherLevel
    }))
    subjects.value = subjectsRes.data ?? []
    classes.value = classesRes.data ?? []
  } catch (err) {
    loadError.value = err.message || t.value.loadError
  } finally {
    loading.value = false
  }
}
async function reloadAssignments() {
  try {
    const { data } = await listAssignments()
    assignments.value = data ?? []
  } catch (err) {
    flash('error', err.message)
  }
}
onMounted(loadAll)

// subjectId → its class meta (level / grade / section), so each assignment row can be
// labelled + filtered by level, year and section.
const classMetaBySubjectId = computed(() => {
  const byClass = new Map(classes.value.map((c) => [c.id, c]))
  const map = new Map()
  for (const s of subjects.value) {
    const c = byClass.get(s.classId)
    map.set(s.id, c ? { level: c.level, grade: c.grade, section: c.section } : {})
  }
  return map
})

// Every assignment enriched with its class's level / grade / section.
const enrichedAssignments = computed(() => {
  const meta = classMetaBySubjectId.value
  return assignments.value.map((a) => {
    const m = meta.get(a.subjectId) || {}
    return { ...a, level: m.level ?? null, grade: m.grade ?? null, section: m.section ?? null }
  })
})

// ---- Level tab counts (whole dataset) ----
const counts = computed(() => {
  const list = enrichedAssignments.value
  return {
    total: list.length,
    secondary: list.filter((a) => a.level === 'Secondary').length,
    highSchool: list.filter((a) => a.level === 'HighSchool').length
  }
})
function levelCount(level) {
  return level === 'Secondary' ? counts.value.secondary : counts.value.highSchool
}

// Year + Section options are derived from what's actually present, narrowed by the
// level (and, for sections, the year) already chosen — so there are no dead options.
const availableYears = computed(() => {
  const set = new Set()
  for (const a of enrichedAssignments.value) {
    if (levelFilter.value && a.level !== levelFilter.value) continue
    if (a.grade) set.add(a.grade)
  }
  return [...set].sort((x, y) => x - y)
})
const availableSections = computed(() => {
  const set = new Set()
  for (const a of enrichedAssignments.value) {
    if (levelFilter.value && a.level !== levelFilter.value) continue
    if (yearFilter.value && String(a.grade) !== yearFilter.value) continue
    if (a.section) set.add(a.section)
  }
  return [...set].sort()
})
function yearLabel(grade) {
  return language.isArabic ? (arabicOrdinals[grade - 1] ?? String(grade)) : `Year ${grade}`
}

// Changing an earlier filter clears the later ones (level → year → section).
watch(levelFilter, () => {
  yearFilter.value = ''
  sectionFilter.value = ''
})
watch(yearFilter, () => {
  sectionFilter.value = ''
})

// ---- Filtering: level tab + year + section + search, applied together ----
const filteredAssignments = computed(() => {
  const q = search.value.trim().toLowerCase()
  return enrichedAssignments.value.filter((a) => {
    if (levelFilter.value && a.level !== levelFilter.value) return false
    if (yearFilter.value && String(a.grade) !== yearFilter.value) return false
    if (sectionFilter.value && a.section !== sectionFilter.value) return false
    if (!q) return true
    return (
      (a.teacherName || '').toLowerCase().includes(q) ||
      (a.subjectName || '').toLowerCase().includes(q) ||
      (a.className || '').toLowerCase().includes(q)
    )
  })
})

// Currently assigned pairs, for the modal's instant duplicate check.
const existingPairs = computed(() =>
  assignments.value.map((a) => ({ teacherId: a.teacherId, subjectId: a.subjectId }))
)

// ---- Summary stats (whole dataset) ----
const statItems = computed(() => {
  const list = assignments.value
  const assignedTeachers = new Set(list.map((a) => a.teacherId)).size
  const coveredSubjects = new Set(list.map((a) => a.subjectId)).size
  return [
    { label: t.value.statAssignments, value: list.length, variant: 'navy', icon: icons.link },
    { label: t.value.statTeachers, value: assignedTeachers, variant: 'green', icon: icons.book },
    { label: t.value.statSubjects, value: coveredSubjects, variant: 'blue', icon: icons.subjects },
    { label: t.value.statUnassigned, value: Math.max(teachers.value.length - assignedTeachers, 0), variant: 'amber', icon: icons.user }
  ]
})

// ---- Add modal (performs the API call itself) ----
const modalOpen = ref(false)

function openAdd() {
  modalOpen.value = true
}
function closeModal() {
  modalOpen.value = false
}
// The cascade modal stays open so the admin can assign several subjects in a row —
// we only refresh the list + flash; the modal resets its own subject step.
function onSaved({ message }) {
  flash('success', message)
  reloadAssignments()
}

// ---- Remove (with confirm) ----
const confirmOpen = ref(false)
const confirmTarget = ref(null)
const deleting = ref(false)

function askRemove(assignment) {
  confirmTarget.value = assignment
  confirmOpen.value = true
}
function closeConfirm() {
  if (deleting.value) return
  confirmOpen.value = false
  confirmTarget.value = null
}
async function confirmRemove() {
  const target = confirmTarget.value
  if (!target) return
  deleting.value = true
  try {
    const { message } = await removeAssignment(target.id)
    deleting.value = false
    closeConfirm()
    flash('success', message)
    reloadAssignments()
  } catch (err) {
    deleting.value = false
    closeConfirm()
    flash('error', err.message)
  }
}
</script>

<template>
  <div class="teacher-assignments" :dir="language.dir">
    <AdminPageHeader />

    <!-- Summary stats -->
    <StatStrip :items="statItems" />

    <!-- Flash message -->
    <Transition name="flash">
      <div v-if="flashMsg" class="flash" :class="flashMsg.type">
        <svg v-if="flashMsg.type === 'success'" viewBox="0 0 24 24"><path d="M20 6 9 17l-5-5" /></svg>
        <svg v-else viewBox="0 0 24 24"><circle cx="12" cy="12" r="9" /><path d="M12 8v5M12 16h.01" /></svg>
        <span>{{ flashMsg.text }}</span>
        <button type="button" class="flash-x" @click="flashMsg = null" aria-label="Dismiss">
          <svg viewBox="0 0 24 24"><path d="M18 6 6 18M6 6l12 12" /></svg>
        </button>
      </div>
    </Transition>

    <!-- Assignments panel: search, level tabs, year/section filters, and table in one card -->
    <div class="panel">
      <!-- Head: search + Assign Subject -->
      <div class="panel-head">
        <label class="search">
          <svg viewBox="0 0 24 24"><circle cx="11" cy="11" r="7" /><path d="m21 21-4.3-4.3" /></svg>
          <input v-model="search" type="text" :placeholder="t.searchPh" />
        </label>

        <button type="button" class="add-btn" @click="openAdd">
          <svg viewBox="0 0 24 24"><path d="M12 5v14M5 12h14" /></svg>
          {{ t.assignSubject }}
        </button>
      </div>

      <!-- Filter bar: level tabs (left) + year & section dropdowns (right) -->
      <div class="filter-bar">
        <div class="level-tabs" role="tablist" :aria-label="t.allLevels">
          <button
            type="button"
            class="level-tab tab-all"
            :class="{ active: levelFilter === '' }"
            role="tab"
            :aria-selected="levelFilter === ''"
            @click="levelFilter = ''"
          >
            <span class="tab-label">{{ t.allLevels }}</span>
            <span class="tab-count">{{ counts.total }}</span>
          </button>
          <button
            v-for="lv in LEVELS"
            :key="lv"
            type="button"
            class="level-tab"
            :class="[`tab-${lv.toLowerCase()}`, { active: levelFilter === lv }]"
            role="tab"
            :aria-selected="levelFilter === lv"
            @click="levelFilter = lv"
          >
            <span class="tab-dot"></span>
            <span class="tab-label">{{ t.levels[lv] }}</span>
            <span class="tab-count">{{ levelCount(lv) }}</span>
          </button>
        </div>

        <div class="sub-filters">
          <select v-model="yearFilter" class="mini-select" :disabled="!availableYears.length" :aria-label="t.allYears">
            <option value="">{{ t.allYears }}</option>
            <option v-for="y in availableYears" :key="y" :value="String(y)">{{ yearLabel(y) }}</option>
          </select>
          <select v-model="sectionFilter" class="mini-select" :disabled="!availableSections.length" :aria-label="t.allSections">
            <option value="">{{ t.allSections }}</option>
            <option v-for="s in availableSections" :key="s" :value="s">{{ t.section }} {{ s }}</option>
          </select>
        </div>
      </div>

      <!-- Body: loading / error / no-match / assignments table -->
      <div class="panel-body">
        <div v-if="loading" class="state-block">
          <span class="spinner"></span>
          <p>{{ t.loading }}</p>
        </div>

        <div v-else-if="loadError" class="state-block error-state">
          <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="9" /><path d="M12 8v5M12 16h.01" /></svg>
          <p>{{ loadError }}</p>
          <button type="button" class="retry-btn" @click="loadAll">{{ t.retry }}</button>
        </div>

        <div v-else-if="assignments.length && !filteredAssignments.length" class="state-block">
          <svg class="state-ic" viewBox="0 0 24 24"><circle cx="11" cy="11" r="7" /><path d="m21 21-4.3-4.3" /></svg>
          <p>{{ t.noMatch }}</p>
        </div>

        <AssignmentsTable v-else :assignments="filteredAssignments" @remove="askRemove" />
      </div>
    </div>

    <!-- Assign modal (cascade) -->
    <AssignmentFormModal
      :open="modalOpen"
      :teachers="teachers"
      :classes="classes"
      :subjects="subjects"
      :existing-pairs="existingPairs"
      @close="closeModal"
      @saved="onSaved"
    />

    <!-- Confirm remove -->
    <ConfirmDialog
      :open="confirmOpen"
      :title="t.confirmTitle"
      :message="t.confirmMsg"
      :confirm-label="t.confirmYes"
      :cancel-label="t.cancel"
      variant="danger"
      :busy="deleting"
      @confirm="confirmRemove"
      @cancel="closeConfirm"
    />
  </div>
</template>

<style scoped>
.teacher-assignments {
  --navy: var(--ds-navy);
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

/* Flash message */
.flash {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  padding: 0.85rem 1.1rem;
  border-radius: 12px;
  font-size: 0.9rem;
  font-weight: 600;
  line-height: 1.4;
}
.flash svg {
  width: 20px;
  height: 20px;
  flex-shrink: 0;
  fill: none;
  stroke: currentColor;
  stroke-width: 2;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.flash.success {
  color: #15803d;
  background: #f0fdf4;
  border: 1px solid #bbf7d0;
}
.flash.error {
  color: #b91c1c;
  background: #fef2f2;
  border: 1px solid #fecaca;
}
.flash span {
  flex: 1;
}
.flash-x {
  border: none;
  background: transparent;
  color: inherit;
  cursor: pointer;
  padding: 0.15rem;
  opacity: 0.6;
  transition: opacity 0.15s ease;
}
.flash-x:hover {
  opacity: 1;
}
.flash-x svg {
  width: 15px;
  height: 15px;
}
.flash-enter-active,
.flash-leave-active {
  transition: opacity 0.25s ease, transform 0.25s ease;
}
.flash-enter-from,
.flash-leave-to {
  opacity: 0;
  transform: translateY(-6px);
}

/* Assignments panel — search + filters + table unified in one elevated card */
.panel {
  position: relative;
  background: #fff;
  border: 1px solid #eaeef6;
  border-radius: 18px;
  box-shadow: 0 8px 22px rgba(30, 41, 59, 0.05);
  overflow: hidden;
  animation: panel-rise 0.5s ease both;
}
/* Signature navy→orange accent line across the top of the list card
   (the same gradient as the welcome page's heading underline). */
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
.panel-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  flex-wrap: wrap;
  padding: 1.1rem 1.25rem;
  border-bottom: 1px solid #eef1f7;
}
.search {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.6rem 1rem;
  background: #fff;
  border: 1px solid #e6e9f2;
  border-radius: 999px;
  min-width: 280px;
  transition: border-color 0.2s ease, box-shadow 0.2s ease;
}
.search:focus-within {
  border-color: var(--navy);
  box-shadow: 0 0 0 3px rgba(30, 76, 154, 0.12);
}
.search svg {
  width: 17px;
  height: 17px;
  fill: none;
  stroke: #9ca3af;
  stroke-width: 1.9;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.search input {
  border: none;
  outline: none;
  background: transparent;
  font-size: 0.9rem;
  font-family: inherit;
  color: #1f2937;
  width: 100%;
}

/* Assign Subject button */
.add-btn {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.7rem 1.3rem;
  border: none;
  border-radius: 12px;
  font-size: 0.9rem;
  font-weight: 700;
  font-family: inherit;
  color: #fff;
  background: linear-gradient(135deg, var(--navy), #2f63ba);
  box-shadow: 0 8px 18px rgba(30, 76, 154, 0.3);
  cursor: pointer;
  transition: transform 0.15s ease, box-shadow 0.2s ease;
}
.add-btn:hover {
  transform: translateY(-1px);
  box-shadow: 0 12px 24px rgba(30, 76, 154, 0.4);
}
.add-btn svg {
  width: 17px;
  height: 17px;
  fill: none;
  stroke: currentColor;
  stroke-width: 2.2;
  stroke-linecap: round;
  stroke-linejoin: round;
}

/* Filter bar: level tabs on the start, year/section on the end */
.filter-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  flex-wrap: wrap;
  padding: 0.7rem 1rem;
  background: #fbfcfe;
  border-bottom: 1px solid #eef1f7;
}

/* Level filter — segmented control (click a level to filter) */
.level-tabs {
  display: flex;
  flex-wrap: wrap;
  gap: 0.45rem;
}
.level-tab {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 0.9rem;
  border: 1px solid transparent;
  border-radius: 10px;
  background: transparent;
  font-family: inherit;
  font-size: 0.88rem;
  font-weight: 700;
  color: #64748b;
  cursor: pointer;
  transition: background 0.15s ease, color 0.15s ease, box-shadow 0.2s ease;
}
.level-tab:hover {
  background: #f4f7fc;
  color: #334155;
}
.tab-dot {
  width: 9px;
  height: 9px;
  border-radius: 50%;
  flex-shrink: 0;
}
.tab-secondary .tab-dot {
  background: #059669;
}
.tab-highschool .tab-dot {
  background: #4f46e5;
}
.tab-count {
  min-width: 22px;
  height: 20px;
  padding: 0 0.4rem;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border-radius: 999px;
  background: #eef1f7;
  color: #64748b;
  font-size: 0.72rem;
  font-weight: 800;
}
/* Active tab lights up in its own level colour */
.level-tab.active {
  color: #fff;
}
.level-tab.active .tab-dot {
  background: rgba(255, 255, 255, 0.9);
}
.level-tab.active .tab-count {
  background: rgba(255, 255, 255, 0.22);
  color: #fff;
}
.tab-all.active {
  background: var(--navy);
  box-shadow: 0 6px 14px rgba(30, 76, 154, 0.28);
}
.tab-secondary.active {
  background: #059669;
  box-shadow: 0 6px 14px rgba(5, 150, 105, 0.28);
}
.tab-highschool.active {
  background: #4f46e5;
  box-shadow: 0 6px 14px rgba(79, 70, 229, 0.28);
}

/* Year + Section dropdowns (compact admin filter style) */
.sub-filters {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-wrap: wrap;
}
.mini-select {
  padding: 0.5rem 0.9rem;
  font-size: 0.86rem;
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
.mini-select:focus {
  border-color: var(--navy);
  box-shadow: 0 0 0 3px rgba(30, 76, 154, 0.12);
}
.mini-select:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}

/* Body: loading / error / no-match states (flat, inside the panel body) */
.panel-body {
  min-height: 120px;
}
.state-block {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 0.7rem;
  padding: 3.5rem 1.5rem;
  color: #6b7280;
  font-size: 0.92rem;
}
.state-block p {
  margin: 0;
}
.state-block .state-ic {
  width: 32px;
  height: 32px;
  fill: none;
  stroke: #b6bfd0;
  stroke-width: 1.7;
  stroke-linecap: round;
  stroke-linejoin: round;
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
  .search {
    min-width: 0;
    flex: 1;
  }
  .panel-head {
    flex-direction: column;
    align-items: stretch;
  }
  .add-btn {
    justify-content: center;
  }
  .filter-bar {
    flex-direction: column;
    align-items: stretch;
  }
  .sub-filters {
    justify-content: stretch;
  }
  .mini-select {
    flex: 1;
  }
}
</style>
