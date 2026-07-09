<script setup>
// Temporary "coming soon" page for parent sections that are built in later modules.
// Mirrors the student under-construction style, themed slate-gray. The section
// title/subtitle come from the route meta (bilingual) via ParentPageHeader.
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { useLanguageStore } from '../../stores/language'
import ParentPageHeader from '../../components/parent/ParentPageHeader.vue'
import { icons } from '../../components/admin/icons'

const route = useRoute()
const language = useLanguageStore()

const iconByRoute = {
  'parent-attendance': 'attendance',
  'parent-grades': 'grades',
  'parent-materials': 'layers',
  'parent-events': 'events',
  'parent-profile': 'user'
}
const icon = computed(() => icons[iconByRoute[route.name]] ?? icons.dashboard)

const t = computed(() =>
  language.isArabic
    ? { heading: 'قريباً', message: 'هذا القسم قيد الإنشاء وسيتوفر قريباً.' }
    : { heading: 'Coming soon', message: 'This section is under construction and coming soon.' }
)
</script>

<template>
  <div class="parent-page" :dir="language.dir">
    <ParentPageHeader />

    <!-- Coming-soon empty state -->
    <div class="coming-soon">
      <span class="cs-icon"><svg viewBox="0 0 24 24" v-html="icon"></svg></span>
      <h2>{{ t.heading }}</h2>
      <p>{{ t.message }}</p>
    </div>
  </div>
</template>

<style scoped>
.parent-page {
  display: flex;
  flex-direction: column;
  animation: parentRise 0.45s ease both;
}
@keyframes parentRise {
  from { opacity: 0; transform: translateY(16px); }
  to { opacity: 1; transform: translateY(0); }
}

.coming-soon {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  gap: 0.55rem;
  min-height: 46vh;
  padding: 2.5rem 1.5rem;
  background: #fff;
  border: 1px solid #e2e8f0;
  border-radius: 18px;
  box-shadow: 0 8px 22px rgba(30, 41, 59, 0.05);
  color: #64748b;
}
.cs-icon {
  width: 76px;
  height: 76px;
  border-radius: 22px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 0.4rem;
  color: #475569;
  background: radial-gradient(circle, #eef2f7, #e2e8f0);
}
.cs-icon svg {
  width: 36px;
  height: 36px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.7;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.coming-soon h2 {
  margin: 0;
  font-size: 1.55rem;
  font-weight: 800;
  color: #1e293b;
}
.coming-soon p {
  margin: 0;
  font-size: 0.95rem;
}

@media (prefers-reduced-motion: reduce) {
  .parent-page {
    animation: none;
  }
}
</style>
