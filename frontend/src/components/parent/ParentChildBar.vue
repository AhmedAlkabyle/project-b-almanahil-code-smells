<script setup>
// The child selector shown at the top of the parent's data pages. Reads the shared
// parent store: with one child it shows a static chip; with several it shows a dropdown
// so the parent can switch which child's data every page displays. Slate/gray theme.
import { computed } from 'vue'
import { useLanguageStore } from '../../stores/language'
import { useParentStore } from '../../stores/parent'

const language = useLanguageStore()
const parent = useParentStore()

const content = {
  en: {
    viewing: 'Viewing',
    loading: 'Loading your children…',
    none: 'No children are linked to your account yet.',
    noneHint: 'Ask an administrator to link your child to your account.',
    noClass: 'No class'
  },
  ar: {
    viewing: 'عرض بيانات',
    loading: 'جارٍ تحميل أبنائك…',
    none: 'لا يوجد أبناء مرتبطون بحسابك بعد.',
    noneHint: 'اطلب من المسؤول ربط ابنك بحسابك.',
    noClass: 'بدون صف'
  }
}
const t = computed(() => content[language.lang])

const initial = (name) => (name || '?').charAt(0).toUpperCase()

// Two-way binding to the store's selected child.
const selected = computed({
  get: () => parent.selectedChildId,
  set: (id) => parent.setChild(Number(id))
})
</script>

<template>
  <!-- Loading -->
  <div v-if="parent.loading && !parent.children.length" class="child-bar loading">
    <span class="mini-spinner"></span>
    <span>{{ t.loading }}</span>
  </div>

  <!-- No children linked -->
  <div v-else-if="!parent.children.length" class="child-bar empty">
    <span class="cb-ic">
      <svg viewBox="0 0 24 24"><circle cx="9" cy="8" r="3.2" /><path d="M3.5 20a5.5 5.5 0 0 1 11 0" /><path d="M16 8.6a3 3 0 0 1 0 5.8M18.5 20a5 5 0 0 0-3-4.6" /></svg>
    </span>
    <div class="cb-empty-text">
      <strong>{{ t.none }}</strong>
      <span>{{ t.noneHint }}</span>
    </div>
  </div>

  <!-- Child selector / chip -->
  <div v-else class="child-bar">
    <span class="cb-avatar">{{ initial(parent.selectedChild?.studentName) }}</span>
    <span class="cb-label">{{ t.viewing }}</span>

    <!-- Multiple children → dropdown; single child → static name -->
    <select v-if="parent.hasMultiple" v-model="selected" class="cb-select">
      <option v-for="c in parent.children" :key="c.studentId" :value="c.studentId">{{ c.studentName }}</option>
    </select>
    <span v-else class="cb-name">{{ parent.selectedChild?.studentName }}</span>

    <span class="cb-class">{{ parent.selectedChild?.className || t.noClass }}</span>
  </div>
</template>

<style scoped>
.child-bar {
  display: flex;
  align-items: center;
  gap: 0.7rem;
  padding: 0.7rem 1rem;
  background: #fff;
  border: 1px solid #e2e8f0;
  border-radius: 14px;
  box-shadow: 0 6px 16px rgba(30, 41, 59, 0.05);
}
.cb-avatar {
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
  background: linear-gradient(135deg, #64748b, #475569);
  box-shadow: 0 3px 8px rgba(30, 41, 59, 0.18);
}
.cb-label {
  font-size: 0.78rem;
  font-weight: 700;
  letter-spacing: 0.04em;
  text-transform: uppercase;
  color: #94a3b8;
}
.cb-name {
  font-size: 1rem;
  font-weight: 800;
  color: #1e293b;
}
.cb-select {
  padding: 0.5rem 0.85rem;
  font-size: 0.92rem;
  font-family: inherit;
  font-weight: 700;
  color: #1e293b;
  background: #f8fafc;
  border: 1.5px solid #e2e8f0;
  border-radius: 10px;
  cursor: pointer;
  outline: none;
  transition: border-color 0.2s ease, box-shadow 0.2s ease, background 0.2s ease;
}
.cb-select:focus {
  background: #fff;
  border-color: #64748b;
  box-shadow: 0 0 0 3px rgba(100, 116, 139, 0.14);
}
.cb-class {
  margin-inline-start: auto;
  padding: 0.3rem 0.75rem;
  border-radius: 999px;
  font-size: 0.75rem;
  font-weight: 700;
  color: #b5670f;
  background: #ffedd5;
  white-space: nowrap;
}

/* Loading + empty variants */
.child-bar.loading {
  color: #64748b;
  font-size: 0.9rem;
  font-weight: 600;
}
.mini-spinner {
  width: 18px;
  height: 18px;
  flex-shrink: 0;
  border-radius: 50%;
  border: 2.5px solid #e2e8f0;
  border-top-color: #64748b;
  animation: spin 0.8s linear infinite;
}
@keyframes spin {
  to { transform: rotate(360deg); }
}
.child-bar.empty {
  align-items: center;
}
.cb-ic {
  width: 40px;
  height: 40px;
  flex-shrink: 0;
  border-radius: 12px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: #94a3b8;
  background: #f1f5f9;
}
.cb-ic svg {
  width: 20px;
  height: 20px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.8;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.cb-empty-text {
  display: flex;
  flex-direction: column;
  line-height: 1.35;
}
.cb-empty-text strong {
  font-size: 0.92rem;
  font-weight: 800;
  color: #1e293b;
}
.cb-empty-text span {
  font-size: 0.82rem;
  color: #94a3b8;
}

@media (prefers-reduced-motion: reduce) {
  .mini-spinner {
    animation-duration: 1.6s;
  }
}
</style>
