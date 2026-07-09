<script setup>
import { computed } from 'vue'
import { useLanguageStore } from '../../../stores/language'

const props = defineProps({
  // Records straight from GET /api/attendance/reports:
  // { id, studentName, className, subjectName, date, status, note }
  records: { type: Array, default: () => [] }
})

const language = useLanguageStore()

// ---- Bilingual copy (per-component i18n pattern) ----
const content = {
  en: {
    student: 'Student',
    class: 'Class',
    subject: 'Subject',
    date: 'Date',
    status: 'Status',
    note: 'Note',
    dash: '—',
    statuses: { Present: 'Present', Absent: 'Absent', Excused: 'Excused' }
  },
  ar: {
    student: 'الطالب',
    class: 'الصف',
    subject: 'المادة',
    date: 'التاريخ',
    status: 'الحالة',
    note: 'ملاحظة',
    dash: '—',
    statuses: { Present: 'حاضر', Absent: 'غائب', Excused: 'بعذر' }
  }
}
const t = computed(() => content[language.lang])

// First letter for the avatar chip.
const initial = (name) => (name || '?').charAt(0).toUpperCase()

// Badge colour theme per status (Present=green, Absent=red, Excused=amber).
const statusClass = (status) => `status-${(status || '').toLowerCase()}`
const statusLabel = (status) => t.value.statuses[status] || status

// Localized date label from an ISO 'yyyy-mm-dd' (parsed local to avoid TZ drift).
function formatDate(iso) {
  if (!iso) return t.value.dash
  const [y, m, d] = String(iso).slice(0, 10).split('-').map(Number)
  if (!y || !m || !d) return iso
  const dt = new Date(y, m - 1, d)
  return dt.toLocaleDateString(language.isArabic ? 'ar' : 'en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric'
  })
}
</script>

<template>
  <div class="table-card">
    <table class="attendance-table">
      <thead>
        <tr>
          <th>{{ t.student }}</th>
          <th>{{ t.class }}</th>
          <th>{{ t.subject }}</th>
          <th>{{ t.date }}</th>
          <th>{{ t.status }}</th>
          <th>{{ t.note }}</th>
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
          <td class="date-cell">{{ formatDate(rec.date) }}</td>
          <td>
            <span class="status" :class="statusClass(rec.status)">
              <span class="status-dot"></span>{{ statusLabel(rec.status) }}
            </span>
          </td>
          <td class="note-cell" :title="rec.note || ''">{{ rec.note || t.dash }}</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
.table-card {
  --navy: var(--ds-navy);
  /* Flat: lives inside the AttendanceReports .panel, which owns the card chrome. */
  background: transparent;
  overflow: hidden;
}

/* Table */
.attendance-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.92rem;
  table-layout: fixed;
}
.attendance-table th:nth-child(1),
.attendance-table td:nth-child(1) {
  width: 22%;
}
.attendance-table th:nth-child(2),
.attendance-table td:nth-child(2) {
  width: 12%;
}
.attendance-table th:nth-child(3),
.attendance-table td:nth-child(3) {
  width: 18%;
}
.attendance-table th:nth-child(4),
.attendance-table td:nth-child(4) {
  width: 14%;
}
.attendance-table th:nth-child(5),
.attendance-table td:nth-child(5) {
  width: 14%;
}
.attendance-table th:nth-child(6),
.attendance-table td:nth-child(6) {
  width: 20%;
}
.attendance-table thead th {
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
.attendance-table tbody td {
  padding: 0.85rem 1.25rem;
  border-bottom: 1px solid #f1f4f9;
  color: #334155;
  vertical-align: middle;
}
.attendance-table tbody td:first-child {
  position: relative;
}
/* Warm accent bar at the row's leading edge that grows on hover — same as the other lists. */
.attendance-table tbody td:first-child::before {
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
.attendance-table tbody tr:hover td:first-child::before {
  transform: scaleY(1);
}
.attendance-table tbody tr:last-child td {
  border-bottom: none;
}
/* Gentle staggered entrance (plays once as rows first appear). */
.attendance-table tbody tr {
  transition: background 0.15s ease;
  animation: att-row-in 0.4s ease both;
  animation-delay: calc(min(var(--row-i, 0), 12) * 35ms);
}
.attendance-table tbody tr:hover {
  background: #f9fbff;
}
@keyframes att-row-in {
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
  .attendance-table tbody tr {
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
.date-cell {
  color: #6b7280;
  white-space: nowrap;
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

/* Status badge with dot */
.status {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.3rem 0.75rem;
  border-radius: 999px;
  font-size: 0.78rem;
  font-weight: 700;
  white-space: nowrap;
}
.status-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
}
.status.status-present {
  color: #15803d;
  background: #dcfce7;
}
.status.status-present .status-dot {
  background: #22c55e;
  box-shadow: 0 0 6px rgba(34, 197, 94, 0.6);
}
.status.status-absent {
  color: #dc2626;
  background: #fee2e2;
}
.status.status-absent .status-dot {
  background: #ef4444;
  box-shadow: 0 0 6px rgba(239, 68, 68, 0.6);
}
.status.status-excused {
  color: #b45309;
  background: #fef3c7;
}
.status.status-excused .status-dot {
  background: #f2a03d;
  box-shadow: 0 0 6px rgba(242, 160, 61, 0.6);
}

/* Note (muted, truncated with a tooltip) */
.note-cell {
  color: #6b7280;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* Responsive: let the table scroll horizontally on small screens */
@media (max-width: 860px) {
  .table-card {
    overflow-x: auto;
  }
  .attendance-table {
    min-width: 760px;
  }
}
</style>
