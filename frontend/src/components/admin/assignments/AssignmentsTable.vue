<script setup>
import { computed } from 'vue'
import { useLanguageStore } from '../../../stores/language'

const props = defineProps({
  // The already-filtered assignments. Each row carries teacherName, subjectName,
  // className, and `level` (its class's level, enriched by the page for the pill).
  assignments: { type: Array, default: () => [] }
})
const emit = defineEmits(['remove'])

const language = useLanguageStore()
const isArabic = computed(() => language.lang === 'ar')

// ---- Bilingual copy (per-component i18n pattern) ----
const content = {
  en: {
    teacher: 'Teacher Name',
    subject: 'Subject',
    className: 'Class',
    actions: 'Actions',
    remove: 'Unassign',
    empty: 'No assignments yet',
    emptyHint: 'Assign a subject to a teacher to get started.'
  },
  ar: {
    teacher: 'اسم المعلم',
    subject: 'المادة',
    className: 'الصف',
    actions: 'الإجراءات',
    remove: 'إلغاء التعيين',
    empty: 'لا توجد تعيينات بعد',
    emptyHint: 'قم بتعيين مادة لمعلم للبدء.'
  }
}
const t = computed(() => content[language.lang])

// First letter for the avatar chip.
const initial = (name) => (name || '?').charAt(0).toUpperCase()

// Level → colour key + localized label (identical mapping to Classes/Subjects, so the
// admin pages read as one family: Secondary = emerald, High School = indigo).
function levelKey(a) {
  if (a.level === 'HighSchool') return 'highschool'
  if (a.level === 'Secondary') return 'secondary'
  return 'default'
}
function levelLabel(a) {
  if (a.level === 'HighSchool') return isArabic.value ? 'ثانوي' : 'High School'
  if (a.level === 'Secondary') return isArabic.value ? 'إعدادي' : 'Secondary'
  return ''
}
</script>

<template>
  <div class="table-card">
    <!-- Empty state -->
    <div v-if="!assignments.length" class="empty">
      <span class="empty-badge">
        <svg viewBox="0 0 24 24"><circle cx="9" cy="8" r="3.2" /><path d="M3.5 20a5.5 5.5 0 0 1 11 0" /><path d="m15.5 12.5 2 2 4-4" /></svg>
      </span>
      <p class="empty-title">{{ t.empty }}</p>
      <p class="empty-hint">{{ t.emptyHint }}</p>
    </div>

    <!-- Table -->
    <table v-else class="assignments-table">
      <thead>
        <tr>
          <th>{{ t.teacher }}</th>
          <th>{{ t.subject }}</th>
          <th>{{ t.className }}</th>
          <th class="col-actions">{{ t.actions }}</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="(a, i) in assignments" :key="a.id" :style="{ '--row-i': i }">
          <td>
            <div class="name-cell">
              <span class="avatar" :class="`av-${levelKey(a)}`">{{ initial(a.teacherName) }}</span>
              <span class="name-text">{{ a.teacherName }}</span>
            </div>
          </td>
          <td>
            <span class="badge subject">{{ a.subjectName }}</span>
          </td>
          <td>
            <div class="class-cell">
              <span class="class-name">{{ a.className }}</span>
              <span v-if="a.level" class="level-chip" :class="levelKey(a)">
                <span class="lc-dot"></span>{{ levelLabel(a) }}
              </span>
            </div>
          </td>
          <td class="col-actions">
            <div class="actions">
              <button type="button" class="act remove" :title="t.remove" :aria-label="t.remove" @click="emit('remove', a)">
                <svg viewBox="0 0 24 24"><path d="M3 6h18" /><path d="M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2" /><path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" /></svg>
              </button>
            </div>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
.table-card {
  --navy: var(--ds-navy);
  /* Flat: this table lives inside the Teacher Assignments .panel, which owns the card
     chrome (border, radius, shadow, accent). Transparent avoids a card-in-card. */
  background: transparent;
  overflow: hidden;
}

/* Table */
.assignments-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.92rem;
  table-layout: fixed;
}
/* Balanced columns: Teacher flexes wide (start); Subject + Class fill the middle;
   Actions hug the trailing edge — mirrors the Manage Classes / Subjects tables. */
.assignments-table th:nth-child(1),
.assignments-table td:nth-child(1) {
  width: 34%;
}
.assignments-table th:nth-child(2),
.assignments-table td:nth-child(2) {
  width: 26%;
}
.assignments-table th:nth-child(3),
.assignments-table td:nth-child(3) {
  width: 26%;
}
.assignments-table th:nth-child(4),
.assignments-table td:nth-child(4) {
  width: 14%;
  text-align: end;
}
.assignments-table thead th {
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
.assignments-table tbody td {
  padding: 0.85rem 1.25rem;
  border-bottom: 1px solid #f1f4f9;
  color: #334155;
  vertical-align: middle;
}
.assignments-table tbody td:first-child {
  position: relative;
}
/* Warm accent bar at the row's leading edge that grows on hover — echoes the
   welcome page's feature-card accent, adding a hand-designed touch to the list. */
.assignments-table tbody td:first-child::before {
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
.assignments-table tbody tr:hover td:first-child::before {
  transform: scaleY(1);
}
.assignments-table tbody tr:last-child td {
  border-bottom: none;
}
/* Gentle staggered entrance. Rows are keyed by id, so each only animates when it
   first appears — it plays once on load and stays instant while filtering. */
.assignments-table tbody tr {
  transition: background 0.15s ease;
  animation: asg-row-in 0.4s ease both;
  animation-delay: calc(min(var(--row-i, 0), 10) * 42ms);
}
.assignments-table tbody tr:hover {
  background: #f9fbff;
}
@keyframes asg-row-in {
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
  .assignments-table tbody tr {
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
/* Avatar tinted by level (matches Classes/Subjects): emerald / indigo / navy */
.avatar.av-secondary {
  background: linear-gradient(135deg, #10b981, #059669);
}
.avatar.av-highschool {
  background: linear-gradient(135deg, #6366f1, #4f46e5);
}
.name-text {
  font-weight: 700;
  color: #0f2444;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* Subject badge */
.badge {
  display: inline-block;
  padding: 0.3rem 0.7rem;
  border-radius: 999px;
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.02em;
}
.badge.subject {
  color: #1e4c9a;
  background: #e0ecff;
}

/* Class cell: the short class name + its level pill */
.class-cell {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  flex-wrap: wrap;
}
.class-name {
  font-weight: 700;
  color: #0f2444;
}

/* Level pill beside the class name (identical to Classes/Subjects' .level-chip) */
.level-chip {
  display: inline-flex;
  align-items: center;
  gap: 0.32rem;
  padding: 0.12rem 0.5rem 0.12rem 0.42rem;
  border-radius: 999px;
  font-size: 0.66rem;
  font-weight: 800;
  letter-spacing: 0.03em;
  text-transform: uppercase;
  white-space: nowrap;
}
.level-chip .lc-dot {
  width: 7px;
  height: 7px;
  border-radius: 50%;
  flex-shrink: 0;
}
.level-chip.secondary {
  color: #047857;
  background: #d1fae5;
}
.level-chip.secondary .lc-dot {
  background: #059669;
}
.level-chip.highschool {
  color: #4338ca;
  background: #e0e7ff;
}
.level-chip.highschool .lc-dot {
  background: #4f46e5;
}

/* Actions */
.col-actions {
  text-align: end;
}
.actions {
  display: inline-flex;
  gap: 0.4rem;
  justify-content: flex-end;
}
/* Compact, square icon button (label shown as a tooltip on hover) — same as Classes */
.act {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 34px;
  height: 34px;
  padding: 0;
  border: 1px solid #e6ebf4;
  border-radius: 10px;
  background: #fff;
  color: #64748b;
  cursor: pointer;
  transition: background 0.15s ease, color 0.15s ease, border-color 0.15s ease, transform 0.15s ease;
}
.act:hover {
  transform: translateY(-1px);
}
.act svg {
  width: 16px;
  height: 16px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.9;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.act.remove:hover {
  color: #dc2626;
  border-color: #f6c9c9;
  background: #fef2f2;
}

/* Empty state */
.empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  gap: 0.35rem;
  padding: 3.5rem 1rem;
}
.empty-badge {
  width: 64px;
  height: 64px;
  border-radius: 50%;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 0.5rem;
  /* Warm, friendly badge (welcome-page orange) so an empty list feels inviting. */
  background: radial-gradient(circle, #fff4e6, #ffe7c9);
  color: #eb9a34;
  box-shadow: 0 8px 18px rgba(242, 160, 61, 0.18);
}
.empty-badge svg {
  width: 28px;
  height: 28px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.8;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.empty-title {
  margin: 0;
  font-size: 1.05rem;
  font-weight: 800;
  color: #0f2444;
}
.empty-hint {
  margin: 0;
  font-size: 0.88rem;
  color: #8a94a6;
}

/* Responsive: let the table scroll horizontally on small screens */
@media (max-width: 720px) {
  .table-card {
    overflow-x: auto;
  }
  .assignments-table {
    min-width: 640px;
  }
}
</style>
