<script setup>
// Teacher "Grades" screen. The teacher picks one of her assigned subjects, an assessment
// type (Quiz / Midterm / Final / Homework) and an optional max mark, the class's students
// load with their current mark for that assessment, and she enters marks (+ optional note)
// and saves. Green teacher theme, bilingual (EN+AR) + RTL. Mirrors TeacherAttendance.vue.
import { computed, onMounted, ref, watch } from 'vue'
import { useLanguageStore } from '../../stores/language'
import TeacherPageHeader from '../../components/teacher/TeacherPageHeader.vue'
import { getMySubjects, getGradeSheet, saveGrades } from '../../api/grades'

const language = useLanguageStore()

// ---- Bilingual copy (per-component i18n pattern) ----
const content = {
  en: {
    subject: 'Subject',
    assessment: 'Assessment',
    maxMark: 'Max Mark',
    assessments: { Quiz: 'Quiz', Midterm: 'Midterm', Final: 'Final', Homework: 'Homework' },
    notePh: 'Note (optional)',
    markPh: '—',
    save: 'Save Grades',
    saving: 'Saving…',
    graded: 'Graded',
    average: 'Average',
    hint: 'Enter a mark for each student. Leave a student blank if not graded yet.',
    invalidMark: 'Some marks are out of range. Each mark must be a number between 0 and the max.',
    noMarks: 'Enter at least one mark before saving.',
    loadingSubjects: 'Loading your subjects…',
    subjectsError: 'We couldn’t load your subjects. Please try again.',
    noSubjects: 'No subjects assigned yet',
    noSubjectsSub: 'You have no subjects assigned. Ask an administrator to assign you a subject so you can enter grades.',
    loadingSheet: 'Loading students…',
    noStudents: 'No students in this class',
    noStudentsSub: 'This subject’s class has no enrolled students yet.',
    retry: 'Retry'
  },
  ar: {
    subject: 'المادة',
    assessment: 'التقييم',
    maxMark: 'الدرجة القصوى',
    assessments: { Quiz: 'اختبار قصير', Midterm: 'نصفي', Final: 'نهائي', Homework: 'واجب' },
    notePh: 'ملاحظة (اختياري)',
    markPh: '—',
    save: 'حفظ الدرجات',
    saving: 'جارٍ الحفظ…',
    graded: 'مُقيّم',
    average: 'المعدل',
    hint: 'أدخل درجة لكل طالب. اترك الطالب فارغًا إن لم يُقيَّم بعد.',
    invalidMark: 'بعض الدرجات خارج النطاق. يجب أن تكون كل درجة رقمًا بين 0 والحد الأقصى.',
    noMarks: 'أدخل درجة واحدة على الأقل قبل الحفظ.',
    loadingSubjects: 'جارٍ تحميل موادك…',
    subjectsError: 'تعذّر تحميل موادك. يرجى المحاولة مرة أخرى.',
    noSubjects: 'لا توجد مواد مُسندة بعد',
    noSubjectsSub: 'لا توجد مواد مُسندة إليك. اطلب من المسؤول إسناد مادة لك لإدخال الدرجات.',
    loadingSheet: 'جارٍ تحميل الطلاب…',
    noStudents: 'لا يوجد طلاب في هذا الصف',
    noStudentsSub: 'لا يوجد طلاب مسجلون في صف هذه المادة بعد.',
    retry: 'إعادة المحاولة'
  }
}
const t = computed(() => content[language.lang])

// The four assessment types (must match the backend's AllowedAssessments).
const ASSESSMENTS = ['Quiz', 'Midterm', 'Final', 'Homework']

// ---- State ----
const subjects = ref([])
const subjectsLoading = ref(true)
const subjectsError = ref('')

const selectedSubjectId = ref('')
const assessmentType = ref('Quiz')
const maxMark = ref(100)

const sheet = ref(null)
const rows = ref([]) // editable: [{ studentId, studentName, mark, note, error }]
const sheetLoading = ref(false)
const sheetError = ref('')

const saving = ref(false)

// ---- Flash (auto-dismisses) ----
const flashMsg = ref(null)
let flashTimer = null
function flash(type, text) {
  flashMsg.value = { type, text }
  clearTimeout(flashTimer)
  flashTimer = setTimeout(() => (flashMsg.value = null), 4500)
}

// ---- Helpers ----
const initial = (name) => (name || '?').charAt(0).toUpperCase()
const subjectLabel = (s) => `${s.subjectName} — ${s.className}`

// The max mark actually used (falls back to 100 when blank / invalid).
const effectiveMax = computed(() => {
  const n = Number(maxMark.value)
  return Number.isFinite(n) && n > 0 ? n : 100
})

// Live summary: how many students have a mark entered + the average of those marks.
const gradedCount = computed(() =>
  rows.value.filter((r) => String(r.mark).trim() !== '').length
)
const average = computed(() => {
  const vals = rows.value
    .map((r) => String(r.mark).trim())
    .filter((m) => m !== '' && Number.isFinite(Number(m)))
    .map(Number)
  if (!vals.length) return null
  return Math.round((vals.reduce((a, b) => a + b, 0) / vals.length) * 10) / 10
})

// ---- Load: the teacher's subjects ----
async function loadSubjects() {
  subjectsLoading.value = true
  subjectsError.value = ''
  try {
    const { data } = await getMySubjects()
    subjects.value = data ?? []
    // Auto-select the first subject so the sheet loads right away.
    if (subjects.value.length) selectedSubjectId.value = subjects.value[0].subjectId
  } catch (err) {
    subjectsError.value = err.message || t.value.subjectsError
  } finally {
    subjectsLoading.value = false
  }
}
onMounted(loadSubjects)

// ---- Load: the grade sheet for the chosen subject + assessment type ----
async function loadSheet() {
  if (!selectedSubjectId.value) return
  sheetLoading.value = true
  sheetError.value = ''
  try {
    const { data } = await getGradeSheet(selectedSubjectId.value, assessmentType.value)
    sheet.value = data
    // Reflect this assessment's stored max (each assessment keeps its own).
    maxMark.value = data.maxMark ?? 100
    rows.value = (data.students ?? []).map((s) => ({
      studentId: s.studentId,
      studentName: s.studentName,
      // null = not graded yet → leave the input blank.
      mark: s.mark === null || s.mark === undefined ? '' : String(s.mark),
      note: s.note ?? '',
      error: false
    }))
  } catch (err) {
    sheetError.value = err.message // surfaces the backend's friendly 403 message too
    sheet.value = null
    rows.value = []
  } finally {
    sheetLoading.value = false
  }
}

// Reload whenever the subject or assessment type changes (max mark is a local edit,
// so it doesn't trigger a reload).
watch([selectedSubjectId, assessmentType], loadSheet)

// ---- Save ----
// Build the entries payload: skip blank (ungraded) students, flag out-of-range marks.
function buildEntries() {
  const max = effectiveMax.value
  const entries = []
  let hasError = false
  for (const r of rows.value) {
    const raw = String(r.mark).trim()
    if (raw === '') {
      r.error = false
      continue // unset → not sent
    }
    const n = Number(raw)
    if (!Number.isFinite(n) || n < 0 || n > max) {
      r.error = true
      hasError = true
      continue
    }
    r.error = false
    entries.push({
      studentId: r.studentId,
      mark: n,
      note: r.note && r.note.trim() ? r.note.trim() : null
    })
  }
  return { entries, hasError }
}

async function save() {
  if (!rows.value.length || saving.value) return
  const { entries, hasError } = buildEntries()
  if (hasError) return flash('error', t.value.invalidMark)
  if (!entries.length) return flash('error', t.value.noMarks)

  saving.value = true
  try {
    const payload = {
      subjectId: Number(selectedSubjectId.value),
      assessmentType: assessmentType.value,
      maxMark: effectiveMax.value,
      entries
    }
    const { message } = await saveGrades(selectedSubjectId.value, payload)
    flash('success', message)
  } catch (err) {
    flash('error', err.message)
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div class="teacher-grades" :dir="language.dir">
    <TeacherPageHeader />

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

    <!-- Subjects: loading / error / empty -->
    <div v-if="subjectsLoading" class="state-card">
      <span class="spinner"></span>
      <p>{{ t.loadingSubjects }}</p>
    </div>

    <div v-else-if="subjectsError" class="state-card error-state">
      <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="9" /><path d="M12 8v5M12 16h.01" /></svg>
      <p>{{ subjectsError }}</p>
      <button type="button" class="retry-btn" @click="loadSubjects">{{ t.retry }}</button>
    </div>

    <div v-else-if="!subjects.length" class="state-card">
      <span class="state-badge"><svg viewBox="0 0 24 24"><path d="M5 4.5A1.5 1.5 0 0 1 6.5 3H19v15H6.5A1.5 1.5 0 0 0 5 19.5z" /><path d="M19 18v3H6.5A1.5 1.5 0 0 1 5 19.5" /></svg></span>
      <h3>{{ t.noSubjects }}</h3>
      <p>{{ t.noSubjectsSub }}</p>
    </div>

    <template v-else>
      <!-- Controls: subject + assessment type + max mark -->
      <div class="controls">
        <div class="ctrl">
          <label>{{ t.subject }}</label>
          <select v-model="selectedSubjectId" class="select">
            <option v-for="s in subjects" :key="s.subjectId" :value="s.subjectId">{{ subjectLabel(s) }}</option>
          </select>
        </div>

        <div class="ctrl ctrl-assess">
          <label>{{ t.assessment }}</label>
          <div class="assess-tabs" role="group">
            <button
              v-for="a in ASSESSMENTS"
              :key="a"
              type="button"
              class="assess-tab"
              :class="{ active: assessmentType === a }"
              @click="assessmentType = a"
            >{{ t.assessments[a] }}</button>
          </div>
        </div>

        <div class="ctrl ctrl-max">
          <label>{{ t.maxMark }}</label>
          <input v-model="maxMark" type="number" min="1" step="any" inputmode="decimal" class="max-input" />
        </div>
      </div>

      <!-- Sheet: loading / error / empty / list -->
      <div v-if="sheetLoading" class="state-card">
        <span class="spinner"></span>
        <p>{{ t.loadingSheet }}</p>
      </div>

      <div v-else-if="sheetError" class="state-card error-state">
        <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="9" /><path d="M12 8v5M12 16h.01" /></svg>
        <p>{{ sheetError }}</p>
        <button type="button" class="retry-btn" @click="loadSheet">{{ t.retry }}</button>
      </div>

      <div v-else-if="sheet && !rows.length" class="state-card">
        <span class="state-badge"><svg viewBox="0 0 24 24"><circle cx="9" cy="8" r="3.2" /><path d="M3.5 20a5.5 5.5 0 0 1 11 0" /><path d="M16 8.6a3 3 0 0 1 0 5.8M18.5 20a5 5 0 0 0-3-4.6" /></svg></span>
        <h3>{{ t.noStudents }}</h3>
        <p>{{ t.noStudentsSub }}</p>
      </div>

      <div v-else-if="sheet" class="sheet">
        <!-- Head: live summary + hint + save -->
        <div class="sheet-head">
          <div class="head-left">
            <div class="summary">
              <span class="sum graded"><span class="dot"></span>{{ t.graded }} · {{ gradedCount }}/{{ rows.length }}</span>
              <span v-if="average !== null" class="sum avg"><span class="dot"></span>{{ t.average }} · {{ average }} / {{ effectiveMax }}</span>
            </div>
            <p class="sheet-hint">{{ t.hint }}</p>
          </div>
          <div class="head-actions">
            <button type="button" class="save-btn" :disabled="saving" @click="save">
              {{ saving ? t.saving : t.save }}
            </button>
          </div>
        </div>

        <!-- Students -->
        <div class="students">
          <div v-for="(row, i) in rows" :key="row.studentId" class="student-row" :style="{ '--row-i': i }">
            <div class="stu">
              <span class="avatar">{{ initial(row.studentName) }}</span>
              <span class="stu-name">{{ row.studentName }}</span>
            </div>

            <div class="mark-box" :class="{ invalid: row.error }">
              <input
                v-model="row.mark"
                type="number"
                class="mark-input"
                min="0"
                :max="effectiveMax"
                step="any"
                inputmode="decimal"
                :placeholder="t.markPh"
                :aria-label="row.studentName"
                @input="row.error = false"
              />
              <span class="mark-max">/ {{ effectiveMax }}</span>
            </div>

            <input v-model="row.note" type="text" class="note-input" :placeholder="t.notePh" />
          </div>
        </div>

        <!-- Foot: save again for long lists -->
        <div class="sheet-foot">
          <button type="button" class="save-btn" :disabled="saving" @click="save">
            {{ saving ? t.saving : t.save }}
          </button>
        </div>
      </div>
    </template>
  </div>
</template>

<style scoped>
.teacher-grades {
  --green: #16a34a;
  --green-strong: #12b981;
  --orange: var(--ds-orange, #f2a03d);
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
  animation: t-rise 0.45s ease both;
}
@keyframes t-rise {
  from {
    opacity: 0;
    transform: translateY(14px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
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

/* Card shell shared by controls + sheet (green-tinted, green→orange top accent) */
.controls,
.sheet {
  position: relative;
  background: #fff;
  border: 1px solid #e6f0eb;
  border-radius: 18px;
  box-shadow: 0 8px 22px rgba(15, 54, 36, 0.05);
  overflow: hidden;
}
.controls::before,
.sheet::before {
  content: '';
  position: absolute;
  top: 0;
  inset-inline: 0;
  height: 3px;
  z-index: 1;
  background: linear-gradient(90deg, var(--green), var(--orange));
}

/* Controls: subject + assessment + max mark */
.controls {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-end;
  gap: 1rem;
  padding: 1.2rem 1.25rem;
}
.ctrl {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  min-width: 220px;
  flex: 1;
}
.ctrl-assess {
  flex: 2;
}
.ctrl-max {
  flex: 0 0 auto;
  min-width: 120px;
  max-width: 150px;
}
.ctrl label {
  font-size: 0.8rem;
  font-weight: 700;
  color: #345247;
}
.select,
.max-input {
  width: 100%;
  box-sizing: border-box;
  padding: 0.7rem 0.9rem;
  font-size: 0.92rem;
  font-family: inherit;
  color: #0f2a1e;
  background: #f5faf7;
  border: 1.5px solid #e2ece7;
  border-radius: 12px;
  outline: none;
  transition: border-color 0.2s ease, box-shadow 0.2s ease, background 0.2s ease;
}
.select {
  cursor: pointer;
}
.select:focus,
.max-input:focus {
  background: #fff;
  border-color: var(--green);
  box-shadow: 0 0 0 4px rgba(22, 163, 74, 0.13);
}

/* Assessment-type segmented tabs */
.assess-tabs {
  display: inline-flex;
  flex-wrap: wrap;
  border: 1.5px solid #e2ece7;
  border-radius: 12px;
  overflow: hidden;
  background: #f5faf7;
}
.assess-tab {
  padding: 0.62rem 0.95rem;
  border: none;
  background: transparent;
  font-family: inherit;
  font-size: 0.85rem;
  font-weight: 700;
  color: #6b8578;
  cursor: pointer;
  transition: background 0.15s ease, color 0.15s ease;
}
.assess-tab:not(:last-child) {
  border-inline-end: 1px solid #e2ece7;
}
.assess-tab:hover:not(.active) {
  background: #eef6f1;
  color: #0f2a1e;
}
.assess-tab.active {
  background: linear-gradient(135deg, var(--green), var(--green-strong));
  color: #fff;
}

/* Sheet head: summary + hint + save */
.sheet-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  flex-wrap: wrap;
  padding: 1rem 1.25rem;
  border-bottom: 1px solid #eef4f0;
  background: #fbfdfc;
}
.head-left {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  min-width: 0;
}
.summary {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-wrap: wrap;
}
.sum {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.3rem 0.7rem;
  border-radius: 999px;
  font-size: 0.78rem;
  font-weight: 800;
  background: #f1f5f3;
  color: #475f55;
}
.sum .dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
}
.sum.graded .dot {
  background: #16a34a;
}
.sum.avg .dot {
  background: #f2a03d;
}
.sheet-hint {
  margin: 0;
  font-size: 0.78rem;
  color: #7a9488;
}
.head-actions {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  flex-wrap: wrap;
}
.save-btn {
  padding: 0.65rem 1.4rem;
  border: none;
  border-radius: 11px;
  font-family: inherit;
  font-size: 0.9rem;
  font-weight: 800;
  color: #fff;
  background: linear-gradient(135deg, var(--green), var(--green-strong));
  box-shadow: 0 8px 18px rgba(16, 163, 74, 0.3);
  cursor: pointer;
  transition: transform 0.15s ease, box-shadow 0.2s ease, opacity 0.2s ease;
}
.save-btn:hover:not(:disabled) {
  transform: translateY(-1px);
  box-shadow: 0 12px 24px rgba(16, 163, 74, 0.4);
}
.save-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

/* Student rows */
.students {
  display: flex;
  flex-direction: column;
}
.student-row {
  display: grid;
  grid-template-columns: minmax(150px, 1.3fr) auto minmax(150px, 1fr);
  align-items: center;
  gap: 0.9rem;
  padding: 0.7rem 1.25rem;
  border-bottom: 1px solid #f1f6f3;
  animation: row-in 0.35s ease both;
  animation-delay: calc(min(var(--row-i, 0), 12) * 35ms);
}
.student-row:last-child {
  border-bottom: none;
}
@keyframes row-in {
  from {
    opacity: 0;
    transform: translateY(6px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}
.stu {
  display: flex;
  align-items: center;
  gap: 0.7rem;
  min-width: 0;
}
.avatar {
  width: 38px;
  height: 38px;
  flex-shrink: 0;
  border-radius: 50%;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-weight: 800;
  font-size: 0.95rem;
  color: #fff;
  background: linear-gradient(135deg, #16a34a, #12b981);
  box-shadow: 0 3px 8px rgba(15, 54, 36, 0.18);
}
.stu-name {
  font-weight: 700;
  color: #0f2a1e;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* Mark input + "/ max" */
.mark-box {
  display: inline-flex;
  align-items: center;
  gap: 0.45rem;
}
.mark-input {
  width: 88px;
  box-sizing: border-box;
  padding: 0.5rem 0.6rem;
  border: 1.5px solid #e2ece7;
  border-radius: 9px;
  font-family: inherit;
  font-size: 0.92rem;
  font-weight: 700;
  text-align: center;
  color: #0f2a1e;
  background: #fff;
  outline: none;
  transition: border-color 0.2s ease, box-shadow 0.2s ease;
}
.mark-input:focus {
  border-color: var(--green);
  box-shadow: 0 0 0 3px rgba(22, 163, 74, 0.12);
}
.mark-box.invalid .mark-input {
  border-color: #dc2626;
  box-shadow: 0 0 0 3px rgba(220, 38, 38, 0.14);
}
.mark-max {
  font-size: 0.85rem;
  font-weight: 700;
  color: #6b8578;
  white-space: nowrap;
}

/* Per-student note */
.note-input {
  width: 100%;
  box-sizing: border-box;
  padding: 0.5rem 0.7rem;
  border: 1px solid #e2ece7;
  border-radius: 9px;
  font-family: inherit;
  font-size: 0.85rem;
  color: #0f2a1e;
  background: #fff;
  outline: none;
  transition: border-color 0.2s ease, box-shadow 0.2s ease;
}
.note-input:focus {
  border-color: var(--green);
  box-shadow: 0 0 0 3px rgba(22, 163, 74, 0.12);
}

/* Foot save */
.sheet-foot {
  display: flex;
  justify-content: flex-end;
  padding: 1rem 1.25rem;
  border-top: 1px solid #eef4f0;
  background: #fbfdfc;
}

/* ---- State cards (loading / error / empty) ---- */
.state-card {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  gap: 0.6rem;
  padding: 3rem 1.5rem;
  background: #fff;
  border: 1px solid #e6f0eb;
  border-radius: 18px;
  box-shadow: 0 8px 22px rgba(15, 54, 36, 0.05);
  color: #5c7568;
}
.state-card p {
  margin: 0;
  font-size: 0.92rem;
}
.state-card h3 {
  margin: 0;
  font-size: 1.2rem;
  font-weight: 800;
  color: #0f2a1e;
}
.state-badge {
  width: 66px;
  height: 66px;
  border-radius: 50%;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 0.3rem;
  color: #16a34a;
  background: radial-gradient(circle, #e9f9f0, #dff3e8);
}
.state-badge svg {
  width: 30px;
  height: 30px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.7;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.state-card.error-state {
  color: #b91c1c;
}
.state-card.error-state svg {
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
  border: 3px solid #e2ece7;
  border-top-color: var(--green);
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
  border: 1px solid #cfe7db;
  border-radius: 10px;
  background: #fff;
  font-family: inherit;
  font-size: 0.85rem;
  font-weight: 700;
  color: #15784c;
  cursor: pointer;
  transition: background 0.15s ease;
}
.retry-btn:hover {
  background: #eefaf3;
}

/* Responsive: stack each student row on small screens */
@media (max-width: 720px) {
  .student-row {
    grid-template-columns: 1fr auto;
    gap: 0.6rem 0.9rem;
  }
  .note-input {
    grid-column: 1 / -1;
  }
}
@media (prefers-reduced-motion: reduce) {
  .teacher-grades,
  .student-row {
    animation: none;
  }
  .spinner {
    animation-duration: 1.6s;
  }
}
</style>
