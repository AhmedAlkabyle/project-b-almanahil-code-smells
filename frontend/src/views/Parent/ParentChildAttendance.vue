<script setup>
// Parent view of their CHILD'S attendance (read-only). The selected child comes from the
// shared parent store; the data is fetched from GET /api/attendance/student/{childId},
// whose RBAC confirms this parent is linked to this child (via ParentStudents) — a parent
// can never see a child that isn't theirs. Slate/gray theme, bilingual, RTL.
import { computed, ref, watch } from 'vue'
import { useLanguageStore } from '../../stores/language'
import { useParentStore } from '../../stores/parent'
import ParentPageHeader from '../../components/parent/ParentPageHeader.vue'
import ParentChildBar from '../../components/parent/ParentChildBar.vue'
import { getStudentAttendance } from '../../api/attendance'

const language = useLanguageStore()
const parent = useParentStore()

// ---- Bilingual copy (per-component i18n pattern) ----
const content = {
  en: {
    subject: 'Subject',
    class: 'Class',
    date: 'Date',
    status: 'Status',
    note: 'Note',
    dash: '—',
    statuses: { Present: 'Present', Absent: 'Absent', Excused: 'Excused' },
    sumPresent: 'Present',
    sumAbsent: 'Absent',
    sumExcused: 'Excused',
    rate: 'Attendance',
    loading: 'Loading attendance…',
    loadError: 'We couldn’t load this attendance. Please try again.',
    retry: 'Retry',
    empty: 'No attendance records yet',
    emptyHint: 'Your child’s attendance will appear here once their teachers start recording it.'
  },
  ar: {
    subject: 'المادة',
    class: 'الصف',
    date: 'التاريخ',
    status: 'الحالة',
    note: 'ملاحظة',
    dash: '—',
    statuses: { Present: 'حاضر', Absent: 'غائب', Excused: 'بعذر' },
    sumPresent: 'حاضر',
    sumAbsent: 'غائب',
    sumExcused: 'بعذر',
    rate: 'نسبة الحضور',
    loading: 'جارٍ تحميل الحضور…',
    loadError: 'تعذّر تحميل هذا الحضور. يرجى المحاولة مرة أخرى.',
    retry: 'إعادة المحاولة',
    empty: 'لا توجد سجلات حضور بعد',
    emptyHint: 'سيظهر حضور ابنك هنا بمجرد أن يبدأ معلموه في تسجيله.'
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
    const { data } = await getStudentAttendance(childId)
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

// ---- Summary (present / absent / excused + attendance rate) ----
const byStatus = (s) => records.value.filter((r) => r.status === s).length
const summary = computed(() => ({
  present: byStatus('Present'),
  absent: byStatus('Absent'),
  excused: byStatus('Excused')
}))
const rate = computed(() => {
  const total = records.value.length
  if (!total) return null
  return Math.round((summary.value.present / total) * 100)
})

// ---- Helpers ----
const statusClass = (status) => `status-${(status || '').toLowerCase()}`
const statusLabel = (status) => t.value.statuses[status] || status
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
  <div class="parent-attendance" :dir="language.dir">
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
        <span class="state-badge"><svg viewBox="0 0 24 24"><rect x="3" y="4" width="18" height="17" rx="2" /><path d="M8 2v4M16 2v4M3 10h18" /></svg></span>
        <h3>{{ t.empty }}</h3>
        <p>{{ t.emptyHint }}</p>
      </div>

      <!-- Records -->
      <template v-else>
        <!-- Summary chips -->
        <div class="summary">
          <span v-if="rate !== null" class="sum rate"><span class="dot"></span>{{ t.rate }} · {{ rate }}%</span>
          <span class="sum present"><span class="dot"></span>{{ t.sumPresent }} · {{ summary.present }}</span>
          <span class="sum absent"><span class="dot"></span>{{ t.sumAbsent }} · {{ summary.absent }}</span>
          <span class="sum excused"><span class="dot"></span>{{ t.sumExcused }} · {{ summary.excused }}</span>
        </div>

        <!-- Table -->
        <div class="panel">
          <div class="table-wrap">
            <table class="rec-table">
              <thead>
                <tr>
                  <th>{{ t.subject }}</th>
                  <th>{{ t.class }}</th>
                  <th>{{ t.date }}</th>
                  <th>{{ t.status }}</th>
                  <th>{{ t.note }}</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="(rec, i) in records" :key="rec.id" :style="{ '--row-i': i }">
                  <td><span class="subject-pill">{{ rec.subjectName }}</span></td>
                  <td class="class-cell">{{ rec.className || t.dash }}</td>
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
        </div>
      </template>
    </template>
  </div>
</template>

<style scoped>
.parent-attendance {
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
.sum.rate .dot { background: #64748b; }
.sum.present .dot { background: #16a34a; }
.sum.absent .dot { background: #dc2626; }
.sum.excused .dot { background: #d97706; }

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
  .parent-attendance,
  .rec-table tbody tr {
    animation: none;
  }
}

.class-cell {
  font-weight: 700;
  color: #1e293b;
}
.date-cell {
  color: #64748b;
  white-space: nowrap;
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

.note-cell {
  color: #64748b;
  max-width: 220px;
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
