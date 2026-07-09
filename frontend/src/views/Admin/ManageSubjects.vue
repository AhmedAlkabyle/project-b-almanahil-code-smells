<script setup>
import { computed, onMounted, ref } from 'vue'
import { useLanguageStore } from '../../stores/language'
import AdminPageHeader from '../../components/admin/AdminPageHeader.vue'
import StatStrip from '../../components/admin/StatStrip.vue'
import { icons } from '../../components/admin/icons'
import SubjectsTable from '../../components/admin/subjects/SubjectsTable.vue'
import SubjectFormModal from '../../components/admin/subjects/SubjectFormModal.vue'
import ConfirmDialog from '../../components/common/ConfirmDialog.vue'
import { listSubjects, removeSubject } from '../../api/subjects'
import { listClasses } from '../../api/classes'

const language = useLanguageStore()

// ---- Bilingual copy (per-component i18n pattern) ----
const content = {
  en: {
    searchPh: 'Search by subject name…',
    addSubject: 'Add Subject',
    secondaryPh: 'Secondary — select class',
    highSchoolPh: 'High School — select class',
    noSecondary: 'No Secondary classes yet',
    noHighSchool: 'No High School classes yet',
    secondaryLabel: 'Secondary classes',
    highSchoolLabel: 'High School classes',
    statSubjects: 'Total Subjects',
    statCovered: 'Classes Covered',
    statAvg: 'Avg / Class',
    loading: 'Loading subjects…',
    loadError: 'We couldn’t load the subjects. Please try again.',
    retry: 'Retry',
    noMatch: 'No subjects match your filters.',
    confirmTitle: 'Delete subject?',
    confirmMsg: 'This subject will be permanently removed. This action cannot be undone.',
    confirmYes: 'Delete',
    cancel: 'Cancel'
  },
  ar: {
    searchPh: 'ابحث باسم المادة…',
    addSubject: 'إضافة مادة',
    secondaryPh: 'الإعدادي — اختر صفًا',
    highSchoolPh: 'الثانوي — اختر صفًا',
    noSecondary: 'لا توجد صفوف إعدادي بعد',
    noHighSchool: 'لا توجد صفوف ثانوي بعد',
    secondaryLabel: 'صفوف الإعدادي',
    highSchoolLabel: 'صفوف الثانوي',
    statSubjects: 'إجمالي المواد',
    statCovered: 'الصفوف المشمولة',
    statAvg: 'متوسط لكل صف',
    loading: 'جارٍ تحميل المواد…',
    loadError: 'تعذّر تحميل المواد. يرجى المحاولة مرة أخرى.',
    retry: 'إعادة المحاولة',
    noMatch: 'لا توجد مواد مطابقة لبحثك.',
    confirmTitle: 'حذف المادة؟',
    confirmMsg: 'ستتم إزالة هذه المادة نهائيًا. لا يمكن التراجع عن هذا الإجراء.',
    confirmYes: 'حذف',
    cancel: 'إلغاء'
  }
}
const t = computed(() => content[language.lang])

// ---- State ----
const subjects = ref([]) // API SubjectResponseDto (carries className + classId)
const classes = ref([])
const loading = ref(true)
const loadError = ref('')
const search = ref('')
const secondaryFilter = ref('') // a Secondary class id (string) or '' = not filtering
const highSchoolFilter = ref('') // a High School class id (string) or '' = not filtering

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
    const [subjectsRes, classesRes] = await Promise.all([listSubjects(), listClasses()])
    subjects.value = subjectsRes.data ?? []
    classes.value = classesRes.data ?? []
  } catch (err) {
    loadError.value = err.message || t.value.loadError
  } finally {
    loading.value = false
  }
}
async function reloadSubjects() {
  try {
    const { data } = await listSubjects()
    subjects.value = data ?? []
  } catch (err) {
    flash('error', err.message)
  }
}
onMounted(loadAll)

// Subjects only carry classId; map each class to its level so we can filter/label by level.
const levelByClassId = computed(() => {
  const map = new Map()
  for (const c of classes.value) map.set(c.id, c.level)
  return map
})

// Two class lists (one per dropdown), derived from each class's level.
const secondaryClasses = computed(() => classes.value.filter((c) => c.level === 'Secondary'))
const highSchoolClasses = computed(() => classes.value.filter((c) => c.level === 'HighSchool'))

// Only ONE dropdown filters at a time; whichever holds a class id wins.
const activeClassId = computed(() => secondaryFilter.value || highSchoolFilter.value || '')

// Selecting a class in one dropdown resets the other (single active filter).
function onSecondaryChange() {
  if (secondaryFilter.value) highSchoolFilter.value = ''
}
function onHighSchoolChange() {
  if (highSchoolFilter.value) secondaryFilter.value = ''
}

// A bilingual, human-readable class label for the dropdown options, e.g.
//   EN: "1/A — Year 1 — Secondary 2025/2026"   AR: "1/A — الأول إعدادي 2025/2026"
const arabicOrdinals = ['الأول', 'الثاني', 'الثالث']
function classLabel(c) {
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
  return label || c.displayName || c.name
}

// ---- Filtering: the chosen class (from either list) + search ----
// Each row is enriched with its class `level` so the table can show the level pill.
const filteredSubjects = computed(() => {
  const q = search.value.trim().toLowerCase()
  const id = activeClassId.value
  const lvlMap = levelByClassId.value
  return subjects.value
    .filter((s) => {
      const matchesClass = !id || s.classId === Number(id)
      const matchesSearch = !q || s.name.toLowerCase().includes(q)
      return matchesClass && matchesSearch
    })
    .map((s) => ({ ...s, level: lvlMap.get(s.classId) ?? null }))
})

// ---- Summary stats strip ----
const statItems = computed(() => {
  const list = subjects.value
  const covered = new Set(list.map((s) => s.classId)).size
  return [
    { label: t.value.statSubjects, value: list.length, variant: 'navy', icon: icons.subjects },
    { label: t.value.statCovered, value: covered, variant: 'blue', icon: icons.classes },
    { label: t.value.statAvg, value: covered ? Math.round(list.length / covered) : 0, variant: 'indigo', icon: icons.layers }
  ]
})

// ---- Add / edit modal (performs the API call itself) ----
const modalOpen = ref(false)
const editingSubject = ref(null)

function openAdd() {
  editingSubject.value = null
  modalOpen.value = true
}
function openEdit(subject) {
  editingSubject.value = subject
  modalOpen.value = true
}
function closeModal() {
  modalOpen.value = false
  editingSubject.value = null
}
function onSaved({ message }) {
  closeModal()
  flash('success', message)
  reloadSubjects()
}

// ---- Delete (with confirm) ----
const confirmOpen = ref(false)
const confirmTarget = ref(null)
const deleting = ref(false)

function askDelete(subject) {
  confirmTarget.value = subject
  confirmOpen.value = true
}
function closeConfirm() {
  if (deleting.value) return
  confirmOpen.value = false
  confirmTarget.value = null
}
async function confirmDelete() {
  const target = confirmTarget.value
  if (!target) return
  deleting.value = true
  try {
    const { message } = await removeSubject(target.id)
    deleting.value = false
    closeConfirm()
    flash('success', message)
    reloadSubjects()
  } catch (err) {
    deleting.value = false
    closeConfirm()
    flash('error', err.message)
  }
}
</script>

<template>
  <div class="manage-subjects" :dir="language.dir">
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

    <!-- Subjects panel: search, level chips, and table unified in one card -->
    <div class="panel">
      <!-- Head: search + two class lists + Add Subject -->
      <div class="panel-head">
        <div class="filters">
          <label class="search">
            <svg viewBox="0 0 24 24"><circle cx="11" cy="11" r="7" /><path d="m21 21-4.3-4.3" /></svg>
            <input v-model="search" type="text" :placeholder="t.searchPh" />
          </label>

          <!-- Two class lists: one Secondary, one High School. Pick a class from either to
               show that class's subjects; selecting in one resets the other. -->
          <select
            v-model="secondaryFilter"
            class="class-select"
            :aria-label="t.secondaryLabel"
            :disabled="!secondaryClasses.length"
            @change="onSecondaryChange"
          >
            <option value="">{{ secondaryClasses.length ? t.secondaryPh : t.noSecondary }}</option>
            <option v-for="c in secondaryClasses" :key="c.id" :value="String(c.id)">{{ classLabel(c) }}</option>
          </select>

          <select
            v-model="highSchoolFilter"
            class="class-select"
            :aria-label="t.highSchoolLabel"
            :disabled="!highSchoolClasses.length"
            @change="onHighSchoolChange"
          >
            <option value="">{{ highSchoolClasses.length ? t.highSchoolPh : t.noHighSchool }}</option>
            <option v-for="c in highSchoolClasses" :key="c.id" :value="String(c.id)">{{ classLabel(c) }}</option>
          </select>
        </div>

        <button type="button" class="add-btn" @click="openAdd">
          <svg viewBox="0 0 24 24"><path d="M12 5v14M5 12h14" /></svg>
          {{ t.addSubject }}
        </button>
      </div>

      <!-- Body: loading / error / no-match / subjects table -->
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

        <div v-else-if="subjects.length && !filteredSubjects.length" class="state-block">
          <svg class="state-ic" viewBox="0 0 24 24"><circle cx="11" cy="11" r="7" /><path d="m21 21-4.3-4.3" /></svg>
          <p>{{ t.noMatch }}</p>
        </div>

        <SubjectsTable v-else :subjects="filteredSubjects" @edit="openEdit" @delete="askDelete" />
      </div>
    </div>

    <!-- Add / edit modal -->
    <SubjectFormModal
      :open="modalOpen"
      :subject="editingSubject"
      :classes="classes"
      :subjects="subjects"
      @close="closeModal"
      @saved="onSaved"
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
.manage-subjects {
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

/* Subjects panel — search + filters + table unified in one elevated card */
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
/* Search + class dropdown cluster (left), Add Subject stays on the right. */
.filters {
  display: flex;
  align-items: center;
  gap: 0.7rem;
  flex-wrap: wrap;
}
/* Class drill-down dropdown (matches the admin filter style) */
.class-select {
  padding: 0.6rem 1rem;
  font-size: 0.9rem;
  font-family: inherit;
  color: #334155;
  background: #fff;
  border: 1px solid #e6e9f2;
  border-radius: 12px;
  cursor: pointer;
  outline: none;
  transition: border-color 0.2s ease, box-shadow 0.2s ease;
}
.class-select:focus {
  border-color: var(--navy);
  box-shadow: 0 0 0 3px rgba(30, 76, 154, 0.12);
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

/* Add Subject button */
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
