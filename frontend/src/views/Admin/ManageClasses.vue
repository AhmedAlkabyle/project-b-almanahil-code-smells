<script setup>
import { computed, onMounted, ref } from 'vue'
import { useLanguageStore } from '../../stores/language'
import AdminPageHeader from '../../components/admin/AdminPageHeader.vue'
import StatStrip from '../../components/admin/StatStrip.vue'
import { icons } from '../../components/admin/icons'
import ClassesTable from '../../components/admin/classes/ClassesTable.vue'
import ClassFormModal from '../../components/admin/classes/ClassFormModal.vue'
import AssignStudentsModal from '../../components/admin/classes/AssignStudentsModal.vue'
import ClassStudentsModal from '../../components/admin/classes/ClassStudentsModal.vue'
import ClassTimetableModal from '../../components/admin/classes/ClassTimetableModal.vue'
import ConfirmDialog from '../../components/common/ConfirmDialog.vue'
import { listClasses, removeClass } from '../../api/classes'

const language = useLanguageStore()

// ---- Bilingual copy (per-component i18n pattern) ----
const content = {
  en: {
    searchPh: 'Search by class, level, or year…',
    addClass: 'Add Class',
    allLevels: 'All levels',
    levels: { Secondary: 'Secondary', HighSchool: 'High School' },
    statClasses: 'Total Classes',
    statStudents: 'Total Students',
    statAvg: 'Avg / Class',
    loading: 'Loading classes…',
    loadError: 'We couldn’t load the classes. Please try again.',
    retry: 'Retry',
    noMatch: 'No classes match your filters.',
    confirmTitle: 'Delete class?',
    confirmMsg: 'This class will be permanently removed. Students are not deleted, but they will no longer be linked to it.',
    confirmYes: 'Delete',
    cancel: 'Cancel'
  },
  ar: {
    searchPh: 'ابحث بالصف أو المرحلة أو العام…',
    addClass: 'إضافة صف',
    allLevels: 'كل المراحل',
    levels: { Secondary: 'إعدادي', HighSchool: 'ثانوي' },
    statClasses: 'إجمالي الصفوف',
    statStudents: 'إجمالي الطلاب',
    statAvg: 'متوسط لكل صف',
    loading: 'جارٍ تحميل الصفوف…',
    loadError: 'تعذّر تحميل الصفوف. يرجى المحاولة مرة أخرى.',
    retry: 'إعادة المحاولة',
    noMatch: 'لا توجد صفوف مطابقة لبحثك.',
    confirmTitle: 'حذف الصف؟',
    confirmMsg: 'سيتم حذف هذا الصف نهائيًا. لن يتم حذف الطلاب، لكن لن يبقوا مرتبطين به.',
    confirmYes: 'حذف',
    cancel: 'إلغاء'
  }
}
const t = computed(() => content[language.lang])

// Level filter tabs (the classes analog of Manage Users' role tabs).
const LEVELS = ['Secondary', 'HighSchool']

// ---- State ----
const classes = ref([])
const loading = ref(true)
const loadError = ref('')
const search = ref('')
const levelFilter = ref('') // '' = all levels

// ---- Flash message (auto-dismisses) ----
const flashMsg = ref(null)
let flashTimer = null
function flash(type, text) {
  flashMsg.value = { type, text }
  clearTimeout(flashTimer)
  flashTimer = setTimeout(() => (flashMsg.value = null), 4500)
}

// ---- Load ----
async function loadClasses() {
  loading.value = true
  loadError.value = ''
  try {
    const { data } = await listClasses()
    classes.value = data ?? []
  } catch (err) {
    loadError.value = err.message || t.value.loadError
  } finally {
    loading.value = false
  }
}
async function reload() {
  try {
    const { data } = await listClasses()
    classes.value = data ?? []
  } catch (err) {
    flash('error', err.message)
  }
}
onMounted(loadClasses)

// ---- Filtering (level tab + search, applied together) ----
const filteredClasses = computed(() => {
  const q = search.value.trim().toLowerCase()
  return classes.value.filter((c) => {
    const matchesLevel = !levelFilter.value || c.level === levelFilter.value
    if (!matchesLevel) return false
    if (!q) return true
    // Match the short name, the composed display name, year, level, grade or section.
    const haystack = [c.name, c.displayName, c.academicYear, c.level, c.section, c.grade != null ? String(c.grade) : '']
      .filter(Boolean)
      .join(' ')
      .toLowerCase()
    return haystack.includes(q)
  })
})

// ---- Summary counts (whole dataset) ----
const counts = computed(() => {
  const list = classes.value
  return {
    total: list.length,
    secondary: list.filter((c) => c.level === 'Secondary').length,
    highSchool: list.filter((c) => c.level === 'HighSchool').length
  }
})
function levelCount(level) {
  return level === 'Secondary' ? counts.value.secondary : counts.value.highSchool
}

// ---- Summary stats strip ----
const statItems = computed(() => {
  const list = classes.value
  const students = list.reduce((sum, c) => sum + (c.studentsCount || 0), 0)
  return [
    { label: t.value.statClasses, value: list.length, variant: 'navy', icon: icons.classes },
    { label: t.value.statStudents, value: students, variant: 'blue', icon: icons.users },
    { label: t.value.statAvg, value: list.length ? Math.round(students / list.length) : 0, variant: 'indigo', icon: icons.layers }
  ]
})

// ---- Add / edit modal (performs the API call itself) ----
const modalOpen = ref(false)
const editingClass = ref(null)

function openAdd() {
  editingClass.value = null
  modalOpen.value = true
}
function openEdit(cls) {
  editingClass.value = cls
  modalOpen.value = true
}
function closeModal() {
  modalOpen.value = false
  editingClass.value = null
}
function onSaved({ message }) {
  closeModal()
  flash('success', message)
  reload()
}

// ---- Assign Students modal (enrolls unassigned students into a class) ----
const assignOpen = ref(false)
const assignTarget = ref(null)

function openAssign(cls) {
  assignTarget.value = cls
  assignOpen.value = true
}
function closeAssign() {
  assignOpen.value = false
  assignTarget.value = null
}
function onEnrolled({ message }) {
  closeAssign()
  flash('success', message)
  reload() // refresh so the students-count reflects the new enrollments
}

// ---- View Students modal (lists enrolled students; can remove them) ----
const viewOpen = ref(false)
const viewTarget = ref(null)

function openView(cls) {
  viewTarget.value = cls
  viewOpen.value = true
}
function closeView() {
  viewOpen.value = false
  viewTarget.value = null
}
// The modal shows its own success notice; here we just keep the main-page count
// in sync by decrementing the viewed class's studentsCount (same object reference).
function onStudentRemoved() {
  const cls = viewTarget.value
  if (cls && typeof cls.studentsCount === 'number') {
    cls.studentsCount = Math.max(0, cls.studentsCount - 1)
  }
}

// ---- Timetable modal (weekly grid editor; saves via the timetable API itself) ----
const timetableOpen = ref(false)
const timetableTarget = ref(null)

function openTimetable(cls) {
  timetableTarget.value = cls
  timetableOpen.value = true
}
function closeTimetable() {
  timetableOpen.value = false
  timetableTarget.value = null
}

// ---- Delete (with confirm) ----
const confirmOpen = ref(false)
const confirmTarget = ref(null)
const deleting = ref(false)

function askDelete(cls) {
  confirmTarget.value = cls
  confirmOpen.value = true
}
function closeConfirm() {
  if (deleting.value) return
  confirmOpen.value = false
  confirmTarget.value = null
}
async function confirmDelete() {
  const cls = confirmTarget.value
  if (!cls) return
  deleting.value = true
  try {
    const { message } = await removeClass(cls.id)
    deleting.value = false
    closeConfirm()
    flash('success', message)
    reload()
  } catch (err) {
    deleting.value = false
    closeConfirm()
    flash('error', err.message)
  }
}
</script>

<template>
  <div class="manage-classes" :dir="language.dir">
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

    <!-- Classes panel: search, level tabs, and table unified in one card -->
    <div class="panel">
      <!-- Head: search + Add Class -->
      <div class="panel-head">
        <label class="search">
          <svg viewBox="0 0 24 24"><circle cx="11" cy="11" r="7" /><path d="m21 21-4.3-4.3" /></svg>
          <input v-model="search" type="text" :placeholder="t.searchPh" />
        </label>

        <button type="button" class="add-btn" @click="openAdd">
          <svg viewBox="0 0 24 24"><path d="M12 5v14M5 12h14" /></svg>
          {{ t.addClass }}
        </button>
      </div>

      <!-- Level filter tabs: click a level to view only those classes -->
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

      <!-- Body: loading / error / no-match / classes table -->
      <div class="panel-body">
        <div v-if="loading" class="state-block">
          <span class="spinner"></span>
          <p>{{ t.loading }}</p>
        </div>

        <div v-else-if="loadError" class="state-block error-state">
          <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="9" /><path d="M12 8v5M12 16h.01" /></svg>
          <p>{{ loadError }}</p>
          <button type="button" class="retry-btn" @click="loadClasses">{{ t.retry }}</button>
        </div>

        <div v-else-if="classes.length && !filteredClasses.length" class="state-block">
          <svg class="state-ic" viewBox="0 0 24 24"><circle cx="11" cy="11" r="7" /><path d="m21 21-4.3-4.3" /></svg>
          <p>{{ t.noMatch }}</p>
        </div>

        <ClassesTable
          v-else
          :classes="filteredClasses"
          @view="openView"
          @timetable="openTimetable"
          @edit="openEdit"
          @delete="askDelete"
          @assign="openAssign"
        />
      </div>
    </div>

    <!-- Add / edit modal -->
    <ClassFormModal
      :open="modalOpen"
      :class-item="editingClass"
      @close="closeModal"
      @saved="onSaved"
    />

    <!-- Assign students modal -->
    <AssignStudentsModal
      :open="assignOpen"
      :class-item="assignTarget"
      @close="closeAssign"
      @enrolled="onEnrolled"
    />

    <!-- View / manage enrolled students modal -->
    <ClassStudentsModal
      :open="viewOpen"
      :class-item="viewTarget"
      @close="closeView"
      @removed="onStudentRemoved"
    />

    <!-- Weekly timetable grid editor -->
    <ClassTimetableModal
      :open="timetableOpen"
      :class-item="timetableTarget"
      @close="closeTimetable"
    />

    <!-- Confirm delete -->
    <ConfirmDialog
      :open="confirmOpen"
      :title="t.confirmTitle"
      :message="t.confirmMsg"
      :confirm-label="t.confirmYes"
      :cancel-label="t.cancel"
      variant="danger"
      :busy="deleting"
      @confirm="confirmDelete"
      @cancel="closeConfirm"
    />
  </div>
</template>

<style scoped>
.manage-classes {
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

/* Classes panel — search + tabs + table unified in one elevated card */
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

/* Add Class button */
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

/* Level filter — segmented control (click a level to filter) */
.level-tabs {
  display: flex;
  flex-wrap: wrap;
  gap: 0.45rem;
  padding: 0.7rem 1rem;
  background: #fbfcfe;
  border-bottom: 1px solid #eef1f7;
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
}
</style>
