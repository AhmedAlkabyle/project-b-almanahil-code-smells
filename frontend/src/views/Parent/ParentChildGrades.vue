<script setup>
// Parent view of their CHILD'S grades (read-only). The selected child comes from the
// shared parent store; the data is fetched from GET /api/grades/student/{childId}, whose
// RBAC already allows the student's parent (via ParentStudents) — a parent can only ever
// see their own child's grades. Slate/gray theme, bilingual, RTL.
import { computed, ref, watch } from 'vue'
import { useLanguageStore } from '../../stores/language'
import { useParentStore } from '../../stores/parent'
import ParentPageHeader from '../../components/parent/ParentPageHeader.vue'
import ParentChildBar from '../../components/parent/ParentChildBar.vue'
import { getStudentGrades } from '../../api/grades'

const language = useLanguageStore()
const parent = useParentStore()

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
    loading: 'Loading grades…',
    loadError: 'We couldn’t load these grades. Please try again.',
    retry: 'Retry',
    empty: 'No grades yet',
    emptyHint: 'Your child’s grades will appear here once their teachers start recording them.'
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
    loading: 'جارٍ تحميل الدرجات…',
    loadError: 'تعذّر تحميل هذه الدرجات. يرجى المحاولة مرة أخرى.',
    retry: 'إعادة المحاولة',
    empty: 'لا توجد درجات بعد',
    emptyHint: 'ستظهر درجات ابنك هنا بمجرد أن يبدأ معلموه في تسجيلها.'
  }
}
const t = computed(() => content[language.lang])

// ---- State ----
const records = ref([])
const loading = ref(false)
const loadError = ref('')

async function loadForChild(childId) {
  if (!childId) {
    records.value = []
    loadError.value = ''
    return
  }
  loading.value = true
  loadError.value = ''
  try {
    const { data } = await getStudentGrades(childId)
    records.value = data ?? []
  } catch (err) {
    loadError.value = err.message || t.value.loadError // surfaces the backend's 403 message too
    records.value = []
  } finally {
    loading.value = false
  }
}
// Reload whenever the selected child changes (immediate: fires once children resolve).
watch(() => parent.selectedChildId, loadForChild, { immediate: true })

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
  <div class="parent-grades" :dir="language.dir">
    <ParentPageHeader />
    <ParentChildBar />

    <!-- Only show the data section once a child is selected -->
    <template v-if="parent.selectedChildId">
      <!-- Loading -->
      <div v-if="loading" class="state-card">
        <span class="spinner"></span>
        <p>{{ t.loading }}</p>
      </div>

      <!-- Error -->
      <div v-else-if="loadError" class="state-card error-state">
        <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="9" /><path d="M12 8v5M12 16h.01" /></svg>
        <p>{{ loadError }}</p>
        <button type="button" class="retry-btn" @click="loadForChild(parent.selectedChildId)">{{ t.retry }}</button>
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
    </template>
  </div>
</template>

<style scoped>
.parent-grades {
  --gray: #64748b;
  --orange: var(--ds-orange, #f2a03d);
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
  animation: p-rise 0.45s ease both;
}
@keyframes p-rise {
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
  border: 1px solid #e2e8f0;
  color: #475569;
  box-shadow: 0 4px 12px rgba(30, 41, 59, 0.05);
}
.sum .dot {
  width: 9px;
  height: 9px;
  border-radius: 50%;
}
.sum.avg .dot { background: #64748b; }
.sum.count .dot { background: #6366f1; }

/* Panel wrapping the table (slate→orange top accent) */
.panel {
  position: relative;
  background: #fff;
  border: 1px solid #e2e8f0;
  border-radius: 18px;
  box-shadow: 0 8px 22px rgba(30, 41, 59, 0.05);
  overflow: hidden;
}
.panel::before {
  content: '';
  position: absolute;
  top: 0;
  inset-inline: 0;
  height: 3px;
  z-index: 2;
  background: linear-gradient(90deg, var(--gray), var(--orange));
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
  color: #94a3b8;
  background: #f8fafc;
  border-bottom: 1px solid #eef2f7;
  white-space: nowrap;
}
.rec-table tbody td {
  padding: 0.85rem 1.25rem;
  border-bottom: 1px solid #f1f5f9;
  color: #334155;
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
  background: linear-gradient(180deg, var(--orange), #f6b65f);
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
  background: #f9fbff;
}
@keyframes rec-row-in {
  from { opacity: 0; transform: translateY(6px); }
  to { opacity: 1; transform: translateY(0); }
}
@media (prefers-reduced-motion: reduce) {
  .parent-grades,
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
  color: #475569;
  background: #e9eef5;
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
  color: #64748b;
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
  border: 1px solid #e2e8f0;
  border-radius: 18px;
  box-shadow: 0 8px 22px rgba(30, 41, 59, 0.05);
  color: #64748b;
}
.state-card p {
  margin: 0;
  font-size: 0.92rem;
}
.state-card h3 {
  margin: 0;
  font-size: 1.2rem;
  font-weight: 800;
  color: #1e293b;
}
.state-badge {
  width: 66px;
  height: 66px;
  border-radius: 50%;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 0.3rem;
  color: #64748b;
  background: radial-gradient(circle, #eef2f7, #e2e8f0);
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
  border: 3px solid #e2e8f0;
  border-top-color: var(--gray);
  animation: spin 0.8s linear infinite;
}
@keyframes spin {
  to { transform: rotate(360deg); }
}
.retry-btn {
  margin-top: 0.3rem;
  padding: 0.55rem 1.3rem;
  border: 1px solid #cbd5e1;
  border-radius: 10px;
  background: #fff;
  font-family: inherit;
  font-size: 0.85rem;
  font-weight: 700;
  color: #475569;
  cursor: pointer;
  transition: background 0.15s ease;
}
.retry-btn:hover {
  background: #f1f5f9;
}
@media (prefers-reduced-motion: reduce) {
  .spinner {
    animation-duration: 1.6s;
  }
}
</style>
