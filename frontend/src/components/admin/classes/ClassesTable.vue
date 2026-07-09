<script setup>
import { computed } from 'vue'
import { useLanguageStore } from '../../../stores/language'

const props = defineProps({
  // The already-filtered list of classes to display.
  classes: { type: Array, default: () => [] }
})
const emit = defineEmits(['edit', 'delete', 'assign', 'view', 'timetable'])

const language = useLanguageStore()
const isArabic = computed(() => language.lang === 'ar')

// ---- Bilingual copy (per-component i18n pattern) ----
const content = {
  en: {
    class: 'Class',
    year: 'Academic Year',
    students: 'Students',
    actions: 'Actions',
    view: 'Students',
    timetable: 'Timetable',
    assign: 'Assign',
    edit: 'Edit',
    del: 'Delete',
    dash: '—',
    empty: 'No classes yet',
    emptyHint: 'Add your first class to get started.'
  },
  ar: {
    class: 'الصف',
    year: 'العام الدراسي',
    students: 'الطلاب',
    actions: 'الإجراءات',
    view: 'الطلاب',
    timetable: 'الجدول',
    assign: 'إسناد',
    edit: 'تعديل',
    del: 'حذف',
    dash: '—',
    empty: 'لا توجد صفوف بعد',
    emptyHint: 'أضف أول صف للبدء.'
  }
}
const t = computed(() => content[language.lang])

const arabicOrdinals = ['الأول', 'الثاني', 'الثالث'] // grade 1..3

// Level → a colour key, matching the level filter tabs on the page
// (Secondary = emerald, High School = indigo).
function levelKey(cls) {
  if (cls.level === 'HighSchool') return 'highschool'
  if (cls.level === 'Secondary') return 'secondary'
  return 'default'
}
// Level → its localized short label (shown as a coloured chip beside the name).
function levelLabel(cls) {
  if (cls.level === 'HighSchool') return isArabic.value ? 'ثانوي' : 'High School'
  if (cls.level === 'Secondary') return isArabic.value ? 'إعدادي' : 'Secondary'
  return ''
}
// Just the grade/year part — the level is shown separately as the chip.
// (Only the English wording says "Year"; the DB column stays "Grade".)
function gradeLabel(cls) {
  if (!cls.grade) return ''
  return isArabic.value ? (arabicOrdinals[cls.grade - 1] ?? String(cls.grade)) : `Year ${cls.grade}`
}

// Bold label in the Class cell: the short "1/A" name (falls back to any legacy name).
const primaryName = (cls) => cls.name || t.value.dash
// Avatar chip: the grade number, or the first character as a fallback.
const avatarText = (cls) => (cls.grade ? String(cls.grade) : (cls.name || '?').charAt(0).toUpperCase())
</script>

<template>
  <div class="table-card">
    <!-- Empty state -->
    <div v-if="!classes.length" class="empty">
      <span class="empty-badge">
        <svg viewBox="0 0 24 24"><rect x="3" y="4" width="18" height="14" rx="2" /><path d="M3 10h18" /></svg>
      </span>
      <p class="empty-title">{{ t.empty }}</p>
      <p class="empty-hint">{{ t.emptyHint }}</p>
    </div>

    <!-- Table -->
    <table v-else class="classes-table">
      <thead>
        <tr>
          <th>{{ t.class }}</th>
          <th>{{ t.year }}</th>
          <th>{{ t.students }}</th>
          <th class="col-actions">{{ t.actions }}</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="(cls, i) in classes" :key="cls.id" :style="{ '--row-i': i }">
          <td>
            <div class="name-cell">
              <span class="avatar" :class="`av-${levelKey(cls)}`">{{ avatarText(cls) }}</span>
              <span class="name-block">
                <span class="name-line">
                  <span class="name-text">{{ primaryName(cls) }}</span>
                  <span v-if="cls.level" class="level-chip" :class="levelKey(cls)">
                    <span class="lc-dot"></span>{{ levelLabel(cls) }}
                  </span>
                </span>
                <span v-if="gradeLabel(cls)" class="name-sub">{{ gradeLabel(cls) }}</span>
              </span>
            </div>
          </td>
          <td class="year-cell">{{ cls.academicYear || t.dash }}</td>
          <td>
            <span class="count-badge">{{ cls.studentsCount }}</span>
          </td>
          <td class="col-actions">
            <div class="actions">
              <button type="button" class="act timetable" :title="t.timetable" :aria-label="t.timetable" @click="emit('timetable', cls)">
                <svg viewBox="0 0 24 24"><rect x="3" y="4" width="18" height="17" rx="2" /><path d="M3 9h18M9 9v12M15 9v12M3 15h18" /></svg>
              </button>
              <button type="button" class="act view" :title="t.view" :aria-label="t.view" @click="emit('view', cls)">
                <svg viewBox="0 0 24 24"><path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12Z" /><circle cx="12" cy="12" r="3" /></svg>
              </button>
              <button type="button" class="act assign" :title="t.assign" :aria-label="t.assign" @click="emit('assign', cls)">
                <svg viewBox="0 0 24 24"><path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" /><circle cx="9" cy="7" r="4" /><path d="M19 8v6M22 11h-6" /></svg>
              </button>
              <button type="button" class="act edit" :title="t.edit" :aria-label="t.edit" @click="emit('edit', cls)">
                <svg viewBox="0 0 24 24"><path d="M12 20h9" /><path d="M16.5 3.5a2.12 2.12 0 0 1 3 3L7 19l-4 1 1-4Z" /></svg>
              </button>
              <button type="button" class="act delete" :title="t.del" :aria-label="t.del" @click="emit('delete', cls)">
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
  /* Flat: this table lives inside the ManageClasses .panel, which owns the card
     chrome (border, radius, shadow). Keeping it transparent avoids a card-in-card. */
  background: transparent;
  overflow: hidden;
}

/* Table */
.classes-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.92rem;
  table-layout: fixed;
}
/* Balanced columns so the sparse data fills the row evenly and the action icons
   sit in a tidy trailing group — not stranded far out at the table edge.
   Class flexes wide (start); Academic Year + Students are centred to fill the
   middle; Actions hug the trailing edge. */
.classes-table th:nth-child(1),
.classes-table td:nth-child(1) {
  width: 36%;
}
.classes-table th:nth-child(2),
.classes-table td:nth-child(2) {
  width: 22%;
  text-align: center;
}
.classes-table th:nth-child(3),
.classes-table td:nth-child(3) {
  width: 12%;
  text-align: center;
}
.classes-table th:nth-child(4),
.classes-table td:nth-child(4) {
  width: 30%;
  text-align: end;
}
.classes-table thead th {
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
.classes-table tbody td {
  padding: 0.85rem 1.25rem;
  border-bottom: 1px solid #f1f4f9;
  color: #334155;
  vertical-align: middle;
}
.classes-table tbody td:first-child {
  position: relative;
}
/* Warm accent bar at the row's leading edge that grows on hover — echoes the
   welcome page's feature-card accent, and adds a hand-designed touch to the list. */
.classes-table tbody td:first-child::before {
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
.classes-table tbody tr:hover td:first-child::before {
  transform: scaleY(1);
}
.classes-table tbody tr:last-child td {
  border-bottom: none;
}
/* Gentle staggered entrance. Rows are keyed by id, so each only animates when it
   first appears — it plays once on load and stays instant while filtering. */
.classes-table tbody tr {
  transition: background 0.15s ease;
  animation: cls-row-in 0.4s ease both;
  animation-delay: calc(min(var(--row-i, 0), 10) * 42ms);
}
.classes-table tbody tr:hover {
  background: #f9fbff;
}
@keyframes cls-row-in {
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
  .classes-table tbody tr {
    animation: none;
  }
}

/* Name cell with avatar */
.name-cell {
  display: flex;
  align-items: center;
  gap: 0.7rem;
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
/* Avatar tinted by level (matches the level tabs): emerald / indigo / navy */
.avatar.av-secondary {
  background: linear-gradient(135deg, #10b981, #059669);
}
.avatar.av-highschool {
  background: linear-gradient(135deg, #6366f1, #4f46e5);
}
.name-block {
  display: flex;
  flex-direction: column;
  line-height: 1.25;
  min-width: 0;
  gap: 0.15rem;
}
.name-line {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  flex-wrap: wrap;
}
.name-text {
  font-weight: 700;
  color: #0f2444;
}
.name-sub {
  font-size: 0.76rem;
  font-weight: 600;
  color: #94a3b8;
}

/* Level chip beside the class name */
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
.year-cell {
  color: #6b7280;
  white-space: nowrap;
}

/* Students count badge */
.count-badge {
  display: inline-block;
  padding: 0.3rem 0.7rem;
  border-radius: 999px;
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.02em;
  color: #1e4c9a;
  background: #e0ecff;
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
/* Compact, square icon buttons (labels shown as tooltips on hover) */
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
.act.timetable:hover {
  color: #7c3aed;
  border-color: #ddd6fe;
  background: #f5f3ff;
}
.act.view:hover {
  color: #0284c7;
  border-color: #bae6fd;
  background: #f0f9ff;
}
.act.assign:hover {
  color: #059669;
  border-color: #a7f3d0;
  background: #ecfdf5;
}
.act.edit:hover {
  color: var(--navy);
  border-color: #b9ccec;
  background: #eef4ff;
}
.act.delete:hover {
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
  .classes-table {
    min-width: 720px;
  }
}
</style>
