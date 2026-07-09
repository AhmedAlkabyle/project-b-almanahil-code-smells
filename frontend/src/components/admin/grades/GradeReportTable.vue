<script setup>
import { computed } from 'vue'
import { useLanguageStore } from '../../../stores/language'

const props = defineProps({
  // Records straight from GET /api/grades/reports:
  // { id, studentId, studentName, className, subjectId, subjectName, assessmentType, mark, maxMark, note }
  records: { type: Array, default: () => [] }
})

const language = useLanguageStore()

// ---- Bilingual copy (per-component i18n pattern) ----
const content = {
  en: {
    student: 'Student',
    class: 'Class',
    subject: 'Subject',
    assessment: 'Assessment',
    mark: 'Mark',
    dash: '—',
    assessments: { Quiz: 'Quiz', Midterm: 'Midterm', Final: 'Final', Homework: 'Homework' }
  },
  ar: {
    student: 'الطالب',
    class: 'الصف',
    subject: 'المادة',
    assessment: 'التقييم',
    mark: 'الدرجة',
    dash: '—',
    assessments: { Quiz: 'اختبار قصير', Midterm: 'نصفي', Final: 'نهائي', Homework: 'واجب' }
  }
}
const t = computed(() => content[language.lang])

// First letter for the avatar chip.
const initial = (name) => (name || '?').charAt(0).toUpperCase()

// Assessment-type badge (tone per type) + localized label.
const assessTone = (a) => `assess-${(a || '').toLowerCase()}`
const assessLabel = (a) => t.value.assessments[a] || a

// Mark helpers: tidy number + performance colour (green ≥ 50%, red < 50%).
const fmt = (n) => {
  const x = Number(n)
  if (!Number.isFinite(x)) return '0'
  return Number.isInteger(x) ? String(x) : String(Math.round(x * 100) / 100)
}
const pct = (r) => (r.maxMark > 0 ? r.mark / r.maxMark : 0)
const markTone = (r) => (pct(r) >= 0.5 ? 'mark-good' : 'mark-bad')
</script>

<template>
  <div class="table-card">
    <table class="grades-table">
      <thead>
        <tr>
          <th>{{ t.student }}</th>
          <th>{{ t.class }}</th>
          <th>{{ t.subject }}</th>
          <th>{{ t.assessment }}</th>
          <th>{{ t.mark }}</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="(rec, i) in records" :key="rec.id" :style="{ '--row-i': i }">
          <td>
            <div class="name-cell">
              <span class="avatar">{{ initial(rec.studentName) }}</span>
              <span class="name-text">{{ rec.studentName }}</span>
            </div>
          </td>
          <td class="class-cell">{{ rec.className || t.dash }}</td>
          <td>
            <span class="subject-pill">{{ rec.subjectName }}</span>
          </td>
          <td>
            <span class="assess" :class="assessTone(rec.assessmentType)">{{ assessLabel(rec.assessmentType) }}</span>
          </td>
          <td>
            <span class="mark" :class="markTone(rec)">{{ fmt(rec.mark) }} / {{ fmt(rec.maxMark) }}</span>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
.table-card {
  --navy: var(--ds-navy);
  /* Flat: lives inside the ViewGrades .panel, which owns the card chrome. */
  background: transparent;
  overflow: hidden;
}

/* Table */
.grades-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.92rem;
  table-layout: fixed;
}
.grades-table th:nth-child(1),
.grades-table td:nth-child(1) {
  width: 28%;
}
.grades-table th:nth-child(2),
.grades-table td:nth-child(2) {
  width: 15%;
}
.grades-table th:nth-child(3),
.grades-table td:nth-child(3) {
  width: 22%;
}
.grades-table th:nth-child(4),
.grades-table td:nth-child(4) {
  width: 18%;
}
.grades-table th:nth-child(5),
.grades-table td:nth-child(5) {
  width: 17%;
}
.grades-table thead th {
  text-align: start;
  padding: 1rem 1.25rem;
  font-size: 0.74rem;
  font-weight: 700;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: #8a94a6;
  background: #f7f9fc;
  border-bottom: 1px solid #eef1f7;
  white-space: nowrap;
}
.grades-table tbody td {
  padding: 0.85rem 1.25rem;
  border-bottom: 1px solid #f1f4f9;
  color: #334155;
  vertical-align: middle;
}
.grades-table tbody td:first-child {
  position: relative;
}
/* Warm accent bar at the row's leading edge that grows on hover — same as the other lists. */
.grades-table tbody td:first-child::before {
  content: '';
  position: absolute;
  inset-inline-start: 0;
  top: 8px;
  bottom: 8px;
  width: 3px;
  border-radius: 3px;
  background: linear-gradient(180deg, var(--ds-orange, #f2a03d), #f6b65f);
  transform: scaleY(0);
  transition: transform 0.22s ease;
}
.grades-table tbody tr:hover td:first-child::before {
  transform: scaleY(1);
}
.grades-table tbody tr:last-child td {
  border-bottom: none;
}
/* Gentle staggered entrance (plays once as rows first appear). */
.grades-table tbody tr {
  transition: background 0.15s ease;
  animation: grade-row-in 0.4s ease both;
  animation-delay: calc(min(var(--row-i, 0), 12) * 35ms);
}
.grades-table tbody tr:hover {
  background: #f9fbff;
}
@keyframes grade-row-in {
  from {
    opacity: 0;
    transform: translateY(6px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}
@media (prefers-reduced-motion: reduce) {
  .grades-table tbody tr {
    animation: none;
  }
}

/* Name cell with avatar */
.name-cell {
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
  background: linear-gradient(135deg, #4361ee, #3b6fe0);
  box-shadow: 0 3px 8px rgba(30, 41, 59, 0.16);
}
.name-text {
  font-weight: 700;
  color: #0f2444;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.class-cell {
  font-weight: 700;
  color: #0f2444;
}

/* Subject pill (light navy) */
.subject-pill {
  display: inline-block;
  max-width: 100%;
  padding: 0.3rem 0.7rem;
  border-radius: 999px;
  font-size: 0.75rem;
  font-weight: 700;
  color: #1e4c9a;
  background: #e0ecff;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  vertical-align: middle;
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
.assess-quiz {
  color: #0369a1;
  background: #e0f2fe;
}
.assess-midterm {
  color: #b45309;
  background: #fef3c7;
}
.assess-final {
  color: #6d28d9;
  background: #ede9fe;
}
.assess-homework {
  color: #047857;
  background: #d1fae5;
}

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
.mark.mark-good {
  color: #15803d;
  background: #dcfce7;
}
.mark.mark-bad {
  color: #dc2626;
  background: #fee2e2;
}

/* Responsive: let the table scroll horizontally on small screens */
@media (max-width: 860px) {
  .table-card {
    overflow-x: auto;
  }
  .grades-table {
    min-width: 760px;
  }
}
</style>
