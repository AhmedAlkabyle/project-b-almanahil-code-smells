<script setup>
import { computed } from 'vue'
import { useLanguageStore } from '../../../stores/language'

const props = defineProps({
  // The already-filtered list of subjects. Each row is enriched with `level`
  // (its class's level) by the page, so we can show the matching level pill.
  subjects: { type: Array, default: () => [] }
})
const emit = defineEmits(['edit', 'delete'])

const language = useLanguageStore()
const isArabic = computed(() => language.lang === 'ar')

// ---- Bilingual copy (per-component i18n pattern) ----
const content = {
  en: {
    name: 'Subject Name',
    className: 'Class',
    actions: 'Actions',
    edit: 'Edit',
    del: 'Delete',
    empty: 'No subjects yet',
    emptyHint: 'Add a subject to get started.'
  },
  ar: {
    name: 'اسم المادة',
    className: 'الصف',
    actions: 'الإجراءات',
    edit: 'تعديل',
    del: 'حذف',
    empty: 'لا توجد مواد بعد',
    emptyHint: 'أضف مادة للبدء.'
  }
}
const t = computed(() => content[language.lang])

// First letter for the avatar chip.
const initial = (name) => (name || '?').charAt(0).toUpperCase()

// Level → colour key + localized label (identical mapping to ClassesTable, so the
// two pages read as one family: Secondary = emerald, High School = indigo).
function levelKey(s) {
  if (s.level === 'HighSchool') return 'highschool'
  if (s.level === 'Secondary') return 'secondary'
  return 'default'
}
function levelLabel(s) {
  if (s.level === 'HighSchool') return isArabic.value ? 'ثانوي' : 'High School'
  if (s.level === 'Secondary') return isArabic.value ? 'إعدادي' : 'Secondary'
  return ''
}
</script>

<template>
  <div class="table-card">
    <!-- Empty state -->
    <div v-if="!subjects.length" class="empty">
      <span class="empty-badge">
        <svg viewBox="0 0 24 24"><path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20" /><path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2Z" /></svg>
      </span>
      <p class="empty-title">{{ t.empty }}</p>
      <p class="empty-hint">{{ t.emptyHint }}</p>
    </div>

    <!-- Table -->
    <table v-else class="subjects-table">
      <thead>
        <tr>
          <th>{{ t.name }}</th>
          <th>{{ t.className }}</th>
          <th class="col-actions">{{ t.actions }}</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="(subject, i) in subjects" :key="subject.id" :style="{ '--row-i': i }">
          <td>
            <div class="name-cell">
              <span class="avatar" :class="`av-${levelKey(subject)}`">{{ initial(subject.name) }}</span>
              <span class="name-text">{{ subject.name }}</span>
            </div>
          </td>
          <td>
            <div class="class-cell">
              <span class="class-name">{{ subject.className }}</span>
              <span v-if="subject.level" class="level-chip" :class="levelKey(subject)">
                <span class="lc-dot"></span>{{ levelLabel(subject) }}
              </span>
            </div>
          </td>
          <td class="col-actions">
            <div class="actions">
              <button type="button" class="act edit" :title="t.edit" :aria-label="t.edit" @click="emit('edit', subject)">
                <svg viewBox="0 0 24 24"><path d="M12 20h9" /><path d="M16.5 3.5a2.12 2.12 0 0 1 3 3L7 19l-4 1 1-4Z" /></svg>
              </button>
              <button type="button" class="act delete" :title="t.del" :aria-label="t.del" @click="emit('delete', subject)">
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
  /* Flat: this table lives inside the ManageSubjects .panel, which owns the card
     chrome (border, radius, shadow). Transparent avoids a card-in-card. */
  background: transparent;
  overflow: hidden;
}

/* Table */
.subjects-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.92rem;
  table-layout: fixed;
}
/* Balanced columns: Subject name flexes wide (start); Class holds the pill; Actions
   hug the trailing edge — mirrors the Manage Classes table. */
.subjects-table th:nth-child(1),
.subjects-table td:nth-child(1) {
  width: 46%;
}
.subjects-table th:nth-child(2),
.subjects-table td:nth-child(2) {
  width: 32%;
}
.subjects-table th:nth-child(3),
.subjects-table td:nth-child(3) {
  width: 22%;
  text-align: end;
}
.subjects-table thead th {
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
.subjects-table tbody td {
  padding: 0.85rem 1.25rem;
  border-bottom: 1px solid #f1f4f9;
  color: #334155;
  vertical-align: middle;
}
.subjects-table tbody td:first-child {
  position: relative;
}
/* Warm accent bar at the row's leading edge that grows on hover — echoes the
   welcome page's feature-card accent, and adds a hand-designed touch to the list. */
.subjects-table tbody td:first-child::before {
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
.subjects-table tbody tr:hover td:first-child::before {
  transform: scaleY(1);
}
.subjects-table tbody tr:last-child td {
  border-bottom: none;
}
/* Gentle staggered entrance. Rows are keyed by id, so each only animates when it
   first appears — it plays once on load and stays instant while filtering. */
.subjects-table tbody tr {
  transition: background 0.15s ease;
  animation: subj-row-in 0.4s ease both;
  animation-delay: calc(min(var(--row-i, 0), 10) * 42ms);
}
.subjects-table tbody tr:hover {
  background: #f9fbff;
}
@keyframes subj-row-in {
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
  .subjects-table tbody tr {
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
/* Avatar tinted by level (matches ClassesTable): emerald / indigo / navy */
.avatar.av-secondary {
  background: linear-gradient(135deg, #10b981, #059669);
}
.avatar.av-highschool {
  background: linear-gradient(135deg, #6366f1, #4f46e5);
}
.name-text {
  font-weight: 700;
  color: #0f2444;
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

/* Level pill beside the class name (identical to ClassesTable's .level-chip) */
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
/* Compact, square icon buttons (labels shown as tooltips on hover) — same as Classes */
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
  .subjects-table {
    min-width: 560px;
  }
}
</style>
