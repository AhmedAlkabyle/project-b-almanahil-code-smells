<script setup>
// Teacher "Attendance" screen. The teacher picks one of her assigned subjects + a date,
// the class's students load with their current status, and she marks Present / Absent /
// Excused (+ optional note) and saves. Green teacher theme, bilingual (EN+AR) + RTL.
import { computed, onMounted, ref, watch } from 'vue'
import { useLanguageStore } from '../../stores/language'
import TeacherPageHeader from '../../components/teacher/TeacherPageHeader.vue'
import { getMySubjects, getAttendanceSheet, saveAttendance } from '../../api/attendance'

const language = useLanguageStore()

// ---- Bilingual copy (per-component i18n pattern) ----
const content = {
  en: {
    subject: 'Subject',
    date: 'Date',
    statuses: { Present: 'Present', Absent: 'Absent', Excused: 'Excused' },
    markAll: 'Mark all Present',
    save: 'Save Attendance',
    saving: 'Saving…',
    notePh: 'Note (optional)',
    loadingSubjects: 'Loading your subjects…',
    subjectsError: 'We couldn’t load your subjects. Please try again.',
    noSubjects: 'No subjects assigned yet',
    noSubjectsSub: 'You have no subjects assigned. Ask an administrator to assign you a subject so you can take attendance.',
    loadingSheet: 'Loading students…',
    noStudents: 'No students in this class',
    noStudentsSub: 'This subject’s class has no enrolled students yet.',
    students: 'students',
    retry: 'Retry'
  },
  ar: {
    subject: 'المادة',
    date: 'التاريخ',
    statuses: { Present: 'حاضر', Absent: 'غائب', Excused: 'بعذر' },
    markAll: 'تعيين الكل حاضر',
    save: 'حفظ الحضور',
    saving: 'جارٍ الحفظ…',
    notePh: 'ملاحظة (اختياري)',
    loadingSubjects: 'جارٍ تحميل موادك…',
    subjectsError: 'تعذّر تحميل موادك. يرجى المحاولة مرة أخرى.',
    noSubjects: 'لا توجد مواد مُسندة بعد',
    noSubjectsSub: 'لا توجد مواد مُسندة إليك. اطلب من المسؤول إسناد مادة لك لتسجيل الحضور.',
    loadingSheet: 'جارٍ تحميل الطلاب…',
    noStudents: 'لا يوجد طلاب في هذا الصف',
    noStudentsSub: 'لا يوجد طلاب مسجلون في صف هذه المادة بعد.',
    students: 'طالب',
    retry: 'إعادة المحاولة'
  }
}
const t = computed(() => content[language.lang])

// The three statuses, in toggle order (tone → colour class).
const STATUSES = [
  { key: 'Present', tone: 'present' },
  { key: 'Absent', tone: 'absent' },
  { key: 'Excused', tone: 'excused' }
]

// ---- State ----
const subjects = ref([])
const subjectsLoading = ref(true)
const subjectsError = ref('')

const selectedSubjectId = ref('')
const date = ref(todayLocal())

const sheet = ref(null)
const rows = ref([]) // editable: [{ studentId, studentName, status, note }]
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
function todayLocal() {
  const n = new Date()
  return `${n.getFullYear()}-${String(n.getMonth() + 1).padStart(2, '0')}-${String(n.getDate()).padStart(2, '0')}`
}
const initial = (name) => (name || '?').charAt(0).toUpperCase()
const subjectLabel = (s) => `${s.subjectName} — ${s.className}`

const summary = computed(() => {
  const s = { present: 0, absent: 0, excused: 0 }
  for (const r of rows.value) {
    if (r.status === 'Present') s.present++
    else if (r.status === 'Absent') s.absent++
    else if (r.status === 'Excused') s.excused++
  }
  return s
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

// ---- Load: the attendance sheet for the chosen subject + date ----
async function loadSheet() {
  if (!selectedSubjectId.value) return
  sheetLoading.value = true
  sheetError.value = ''
  try {
    const { data } = await getAttendanceSheet(selectedSubjectId.value, date.value)
    sheet.value = data
    // Default an untaken student to Present (take attendance = all present, mark exceptions).
    rows.value = (data.students ?? []).map((s) => ({
      studentId: s.studentId,
      studentName: s.studentName,
      status: s.status ?? 'Present',
      note: s.note ?? ''
    }))
  } catch (err) {
    sheetError.value = err.message // surfaces the backend's friendly 403 message too
    sheet.value = null
    rows.value = []
  } finally {
    sheetLoading.value = false
  }
}

// Reload whenever the subject or date changes.
watch([selectedSubjectId, date], loadSheet)

// ---- Quick action + save ----
function markAllPresent() {
  rows.value.forEach((r) => {
    r.status = 'Present'
  })
}

async function save() {
  if (!rows.value.length || saving.value) return
  saving.value = true
  try {
    const payload = {
      subjectId: Number(selectedSubjectId.value),
      date: date.value,
      entries: rows.value.map((r) => ({
        studentId: r.studentId,
        status: r.status,
        note: r.note && r.note.trim() ? r.note.trim() : null
      }))
    }
    const { message } = await saveAttendance(selectedSubjectId.value, payload)
    flash('success', message)
  } catch (err) {
    flash('error', err.message)
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div class="teacher-attendance" :dir="language.dir">
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
      <!-- Controls: subject + date -->
      <div class="controls">
        <div class="ctrl">
          <label>{{ t.subject }}</label>
          <select v-model="selectedSubjectId" class="select">
            <option v-for="s in subjects" :key="s.subjectId" :value="s.subjectId">{{ subjectLabel(s) }}</option>
          </select>
        </div>
        <div class="ctrl">
          <label>{{ t.date }}</label>
          <input v-model="date" type="date" class="date-input" />
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
        <!-- Head: live summary + actions -->
        <div class="sheet-head">
          <div class="summary">
            <span class="sum present"><span class="dot"></span>{{ t.statuses.Present }} · {{ summary.present }}</span>
            <span class="sum absent"><span class="dot"></span>{{ t.statuses.Absent }} · {{ summary.absent }}</span>
            <span class="sum excused"><span class="dot"></span>{{ t.statuses.Excused }} · {{ summary.excused }}</span>
          </div>
          <div class="head-actions">
            <button type="button" class="ghost-btn" @click="markAllPresent">
              <svg viewBox="0 0 24 24"><path d="M20 6 9 17l-5-5" /></svg>
              {{ t.markAll }}
            </button>
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

            <div class="seg" role="group" :aria-label="row.studentName">
              <button
                v-for="st in STATUSES"
                :key="st.key"
                type="button"
                class="seg-btn"
                :class="[st.tone, { active: row.status === st.key }]"
                @click="row.status = st.key"
              >
                {{ t.statuses[st.key] }}
              </button>
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
.teacher-attendance {
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

/* Controls: subject + date */
.controls {
  display: flex;
  flex-wrap: wrap;
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
.ctrl label {
  font-size: 0.8rem;
  font-weight: 700;
  color: #345247;
}
.select,
.date-input {
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
  cursor: pointer;
  transition: border-color 0.2s ease, box-shadow 0.2s ease, background 0.2s ease;
}
.select:focus,
.date-input:focus {
  background: #fff;
  border-color: var(--green);
  box-shadow: 0 0 0 4px rgba(22, 163, 74, 0.13);
}

/* Sheet head: summary + actions */
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
.sum.present .dot {
  background: #16a34a;
}
.sum.absent .dot {
  background: #dc2626;
}
.sum.excused .dot {
  background: #d97706;
}
.head-actions {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  flex-wrap: wrap;
}
.ghost-btn {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.6rem 1rem;
  border: 1px solid #cfe7db;
  border-radius: 11px;
  background: #fff;
  color: #15784c;
  font-family: inherit;
  font-size: 0.85rem;
  font-weight: 700;
  cursor: pointer;
  transition: background 0.15s ease, transform 0.15s ease;
}
.ghost-btn svg {
  width: 16px;
  height: 16px;
  fill: none;
  stroke: currentColor;
  stroke-width: 2.2;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.ghost-btn:hover {
  background: #eefaf3;
  transform: translateY(-1px);
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

/* Segmented status toggle */
.seg {
  display: inline-flex;
  border: 1px solid #e2ece7;
  border-radius: 10px;
  overflow: hidden;
  background: #f6faf8;
}
.seg-btn {
  padding: 0.45rem 0.85rem;
  border: none;
  background: transparent;
  font-family: inherit;
  font-size: 0.82rem;
  font-weight: 700;
  color: #6b8578;
  cursor: pointer;
  transition: background 0.15s ease, color 0.15s ease;
}
.seg-btn:not(:last-child) {
  border-inline-end: 1px solid #e2ece7;
}
.seg-btn:hover:not(.active) {
  background: #eef6f1;
  color: #0f2a1e;
}
.seg-btn.active.present {
  background: #16a34a;
  color: #fff;
}
.seg-btn.active.absent {
  background: #dc2626;
  color: #fff;
}
.seg-btn.active.excused {
  background: #d97706;
  color: #fff;
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
    grid-template-columns: 1fr;
    gap: 0.6rem;
  }
  .seg {
    width: 100%;
  }
  .seg-btn {
    flex: 1;
  }
}
@media (prefers-reduced-motion: reduce) {
  .teacher-attendance,
  .student-row {
    animation: none;
  }
  .spinner {
    animation-duration: 1.6s;
  }
}
</style>
