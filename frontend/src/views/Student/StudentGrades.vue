<script setup>
// Student "My Grades" screen (functional). The logged-in student views ONLY their own
// grades — fetched from GET /api/grades/student/{ownId}, using the student's own id from
// the auth store. That endpoint's RBAC allows Admin, the student themselves, or their
// parent, so a student can only ever pull their own grades. Orange theme, bilingual, RTL.
import { computed, onMounted, ref } from 'vue'
import { useAuthStore } from '../../stores/auth'
import { useLanguageStore } from '../../stores/language'
import StudentPageHeader from '../../components/student/StudentPageHeader.vue'
import { getStudentGrades } from '../../api/grades'

const auth = useAuthStore()
const language = useLanguageStore()

// ---- Bilingual copy (per-component i18n pattern) ----
const content = {
  en: {
    subject: 'Subject',
    assessment: 'Assessment',
    mark: 'Mark',
    note: 'Note',
    dash: '—',
    assessments: { Quiz: 'Quiz', Midterm: 'Midterm', Final: 'Final', Homework: 'Homework' },
    average: 'Average',
    graded: 'Graded',
    loading: 'Loading your grades…',
    loadError: 'We couldn’t load your grades. Please try again.',
    retry: 'Retry',
    empty: 'No grades yet',
    emptyHint: 'Your grades will appear here once your teachers start recording them.'
  },
  ar: {
    subject: 'المادة',
    assessment: 'التقييم',
    mark: 'الدرجة',
    note: 'ملاحظة',
    dash: '—',
    assessments: { Quiz: 'اختبار قصير', Midterm: 'نصفي', Final: 'نهائي', Homework: 'واجب' },
    average: 'المعدل',
    graded: 'عدد الدرجات',
    loading: 'جارٍ تحميل درجاتك…',
    loadError: 'تعذّر تحميل درجاتك. يرجى المحاولة مرة أخرى.',
    retry: 'إعادة المحاولة',
    empty: 'لا توجد درجات بعد',
    emptyHint: 'ستظهر درجاتك هنا بمجرد أن يبدأ معلموك في تسجيلها.'
  }
}
const t = computed(() => content[language.lang])

// ---- State ----
const records = ref([])
const loading = ref(true)
const loadError = ref('')

async function loadGrades() {
  const studentId = auth.user?.id
  loading.value = true
  loadError.value = ''
  try {
    // Guard: without an id we can't scope the request — treat as empty rather than
    // calling the endpoint with a bad path.
    if (!studentId) {
      records.value = []
      return
    }
    const { data } = await getStudentGrades(studentId)
    records.value = data ?? []
  } catch (err) {
    loadError.value = err.message || t.value.loadError
    records.value = []
  } finally {
    loading.value = false
  }
}
onMounted(loadGrades)

// ---- Summary (average % + graded count) ----
const percentages = computed(() =>
  records.value.map((r) => (r.maxMark > 0 ? (r.mark / r.maxMark) * 100 : 0))
)
const average = computed(() => {
  const p = percentages.value
  return p.length ? Math.round(p.reduce((a, b) => a + b, 0) / p.length) : null
})

// ---- Helpers ----
const assessTone = (a) => `assess-${(a || '').toLowerCase()}`
const assessLabel = (a) => t.value.assessments[a] || a
const fmt = (n) => {
  const x = Number(n)
  if (!Number.isFinite(x)) return '0'
  return Number.isInteger(x) ? String(x) : String(Math.round(x * 100) / 100)
}
const markTone = (r) => ((r.maxMark > 0 ? r.mark / r.maxMark : 0) >= 0.5 ? 'mark-good' : 'mark-bad')
</script>

<template>
  <div class="student-grades" :dir="language.dir">
    <StudentPageHeader />

    <!-- Loading -->
    <div v-if="loading" class="state-card">
      <span class="spinner"></span>
      <p>{{ t.loading }}</p>
    </div>

    <!-- Error -->
    <div v-else-if="loadError" class="state-card error-state">
      <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="9" /><path d="M12 8v5M12 16h.01" /></svg>
      <p>{{ loadError }}</p>
      <button type="button" class="retry-btn" @click="loadGrades">{{ t.retry }}</button>
    </div>

    <!-- Empty -->
    <div v-else-if="!records.length" class="state-card">
      <span class="state-badge"><svg viewBox="0 0 24 24"><circle cx="12" cy="9" r="6" /><path d="m9 14-1 8 4-3 4 3-1-8" /></svg></span>
      <h3>{{ t.empty }}</h3>
      <p>{{ t.emptyHint }}</p>
    </div>

    <!-- Records -->
    <template v-else>
      <!-- Summary chips -->
      <div class="summary">
        <span v-if="average !== null" class="sum avg"><span class="dot"></span>{{ t.average }} · {{ average }}%</span>
        <span class="sum count"><span class="dot"></span>{{ t.graded }} · {{ records.length }}</span>
      </div>

      <!-- Table -->
      <div class="panel">
        <div class="table-wrap">
          <table class="rec-table">
            <thead>
              <tr>
                <th>{{ t.subject }}</th>
                <th>{{ t.assessment }}</th>
                <th>{{ t.mark }}</th>
                <th>{{ t.note }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(rec, i) in records" :key="rec.id" :style="{ '--row-i': i }">
                <td><span class="subject-pill">{{ rec.subjectName }}</span></td>
                <td>
                  <span class="assess" :class="assessTone(rec.assessmentType)">{{ assessLabel(rec.assessmentType) }}</span>
                </td>
                <td>
                  <span class="mark" :class="markTone(rec)">{{ fmt(rec.mark) }} / {{ fmt(rec.maxMark) }}</span>
                </td>
                <td class="note-cell" :title="rec.note || ''">{{ rec.note || t.dash }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </template>
  </div>
</template>

<style scoped>
.student-grades {
  --orange: #ef8a29;
  --gold: #f6b93b;
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
  animation: s-rise 0.45s ease both;
}
@keyframes s-rise {
  from { opacity: 0; transform: translateY(14px); }
  to { opacity: 1; transform: translateY(0); }
}

/* Summary chips */
.summary {
  display: flex;
  align-items: center;
  gap: 0.55rem;
  flex-wrap: wrap;
}
.sum {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.4rem 0.85rem;
  border-radius: 999px;
  font-size: 0.82rem;
  font-weight: 800;
  background: #fff;
  border: 1px solid #f2e4d3;
  color: #6b543d;
  box-shadow: 0 4px 12px rgba(156, 80, 10, 0.05);
}
.sum .dot {
  width: 9px;
  height: 9px;
  border-radius: 50%;
}
.sum.avg .dot { background: #ef8a29; }
.sum.count .dot { background: #6366f1; }

/* Panel wrapping the table (orange→gold top accent) */
.panel {
  position: relative;
  background: #fff;
  border: 1px solid #f2e4d3;
  border-radius: 18px;
  box-shadow: 0 8px 22px rgba(156, 80, 10, 0.05);
  overflow: hidden;
}
.panel::before {
  content: '';
  position: absolute;
  top: 0;
  inset-inline: 0;
  height: 3px;
  z-index: 2;
  background: linear-gradient(90deg, var(--orange), var(--gold));
}
.table-wrap {
  overflow-x: auto;
}

/* Table */
.rec-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.92rem;
}
.rec-table thead th {
  text-align: start;
  padding: 1rem 1.25rem;
  font-size: 0.74rem;
  font-weight: 700;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: #a1876b;
  background: #fdf6ee;
  border-bottom: 1px solid #f3e6d6;
  white-space: nowrap;
}
.rec-table tbody td {
  padding: 0.85rem 1.25rem;
  border-bottom: 1px solid #f6ecdf;
  color: #4a3a29;
  vertical-align: middle;
}
.rec-table tbody td:first-child {
  position: relative;
}
.rec-table tbody td:first-child::before {
  content: '';
  position: absolute;
  inset-inline-start: 0;
  top: 8px;
  bottom: 8px;
  width: 3px;
  border-radius: 3px;
  background: linear-gradient(180deg, var(--orange), var(--gold));
  transform: scaleY(0);
  transition: transform 0.22s ease;
}
.rec-table tbody tr:hover td:first-child::before {
  transform: scaleY(1);
}
.rec-table tbody tr:last-child td {
  border-bottom: none;
}
.rec-table tbody tr {
  transition: background 0.15s ease;
  animation: rec-row-in 0.4s ease both;
  animation-delay: calc(min(var(--row-i, 0), 12) * 35ms);
}
.rec-table tbody tr:hover {
  background: #fffaf3;
}
@keyframes rec-row-in {
  from { opacity: 0; transform: translateY(6px); }
  to { opacity: 1; transform: translateY(0); }
}
@media (prefers-reduced-motion: reduce) {
  .student-grades,
  .rec-table tbody tr {
    animation: none;
  }
}

.subject-pill {
  display: inline-block;
  padding: 0.3rem 0.7rem;
  border-radius: 999px;
  font-size: 0.75rem;
  font-weight: 700;
  color: #b5670f;
  background: #ffedd5;
  white-space: nowrap;
}

/* Assessment-type badge (tone per type) */
.assess {
  display: inline-block;
  padding: 0.3rem 0.7rem;
  border-radius: 999px;
  font-size: 0.75rem;
  font-weight: 700;
  white-space: nowrap;
}
.assess-quiz { color: #0369a1; background: #e0f2fe; }
.assess-midterm { color: #b45309; background: #fef3c7; }
.assess-final { color: #6d28d9; background: #ede9fe; }
.assess-homework { color: #047857; background: #d1fae5; }

/* Mark ('X / max') coloured by performance */
.mark {
  display: inline-flex;
  align-items: baseline;
  gap: 0.15rem;
  padding: 0.3rem 0.7rem;
  border-radius: 999px;
  font-size: 0.82rem;
  font-weight: 800;
  white-space: nowrap;
}
.mark.mark-good { color: #15803d; background: #dcfce7; }
.mark.mark-bad { color: #dc2626; background: #fee2e2; }

.note-cell {
  color: #8a6a4d;
  max-width: 240px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
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
  border: 1px solid #f2e4d3;
  border-radius: 18px;
  box-shadow: 0 8px 22px rgba(156, 80, 10, 0.05);
  color: #8a6a4d;
}
.state-card p {
  margin: 0;
  font-size: 0.92rem;
}
.state-card h3 {
  margin: 0;
  font-size: 1.2rem;
  font-weight: 800;
  color: #3a2410;
}
.state-badge {
  width: 66px;
  height: 66px;
  border-radius: 50%;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 0.3rem;
  color: #ef8a29;
  background: radial-gradient(circle, #fff2df, #ffe7c6);
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
  border: 3px solid #f0e0cd;
  border-top-color: var(--orange);
  animation: spin 0.8s linear infinite;
}
@keyframes spin {
  to { transform: rotate(360deg); }
}
.retry-btn {
  margin-top: 0.3rem;
  padding: 0.55rem 1.3rem;
  border: 1px solid #f3d3ab;
  border-radius: 10px;
  background: #fff;
  font-family: inherit;
  font-size: 0.85rem;
  font-weight: 700;
  color: #b5670f;
  cursor: pointer;
  transition: background 0.15s ease;
}
.retry-btn:hover {
  background: #fff3e6;
}
@media (prefers-reduced-motion: reduce) {
  .spinner {
    animation-duration: 1.6s;
  }
}
</style>
