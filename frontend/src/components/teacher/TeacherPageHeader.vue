<script setup>
// Reusable GREEN gradient page header for teacher sub-pages — the teacher
// counterpart to AdminPageHeader (same layered glows, dotted texture, orange
// accent), only the base gradient is green. Title/subtitle come from the route
// meta (bilingual) and the icon is chosen from the route name.
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { useLanguageStore } from '../../stores/language'
import { icons } from '../admin/icons'

const route = useRoute()
const language = useLanguageStore()

const title = computed(() => route.meta.title?.[language.lang] ?? '')
const subtitle = computed(() => route.meta.subtitle?.[language.lang] ?? '')

// Map each teacher route to its sidebar icon.
const iconByRoute = {
  'teacher-subjects': 'subjects',
  'teacher-attendance': 'attendance',
  'teacher-grades': 'grades',
  'teacher-materials': 'layers',
  'teacher-events': 'events',
  'teacher-profile': 'user'
}
const icon = computed(() => icons[iconByRoute[route.name]] ?? icons.dashboard)
</script>

<template>
  <header class="page-header" :dir="language.dir">
    <div class="ph-content">
      <span class="ph-icon"><svg viewBox="0 0 24 24" v-html="icon"></svg></span>
      <div class="ph-text">
        <h2 class="ph-title">{{ title }}</h2>
        <p v-if="subtitle" class="ph-sub">{{ subtitle }}</p>
      </div>
    </div>
    <div v-if="$slots.actions" class="ph-actions"><slot name="actions" /></div>
  </header>
</template>

<style scoped>
.page-header {
  position: relative;
  overflow: hidden;
  border-radius: 20px;
  padding: 1.35rem 1.75rem;
  margin-bottom: 1.5rem;
  color: #fff;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  /* Layered-glow GREEN gradient (teacher counterpart of the navy admin header) */
  background:
    radial-gradient(circle at 12% 20%, rgba(255, 255, 255, 0.12), transparent 40%),
    radial-gradient(circle at 92% 12%, rgba(242, 160, 61, 0.22), transparent 46%),
    radial-gradient(circle at 82% 100%, rgba(52, 211, 153, 0.42), transparent 52%),
    linear-gradient(135deg, #15784c 0%, #0f5638 55%, #0b3f29 100%);
  box-shadow: 0 14px 32px rgba(11, 63, 41, 0.24);
}
/* Faded dotted texture, masked toward the edges */
.page-header::before {
  content: '';
  position: absolute;
  inset: 0;
  z-index: 0;
  pointer-events: none;
  background-image: radial-gradient(rgba(255, 255, 255, 0.08) 1.5px, transparent 1.6px);
  background-size: 24px 24px;
  -webkit-mask-image: radial-gradient(ellipse at 40% 30%, #000 6%, transparent 72%);
  mask-image: radial-gradient(ellipse at 40% 30%, #000 6%, transparent 72%);
  opacity: 0.7;
}
/* Soft orange glow blob in the corner */
.page-header::after {
  content: '';
  position: absolute;
  top: -110px;
  inset-inline-end: -70px;
  width: 280px;
  height: 280px;
  z-index: 0;
  border-radius: 50%;
  background: radial-gradient(circle, rgba(242, 160, 61, 0.2), transparent 70%);
  filter: blur(6px);
  pointer-events: none;
}
.ph-content {
  position: relative;
  z-index: 1;
  display: flex;
  align-items: center;
  gap: 1rem;
  min-width: 0;
}
.ph-icon {
  width: 52px;
  height: 52px;
  flex-shrink: 0;
  border-radius: 15px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  background: rgba(255, 255, 255, 0.14);
  border: 1px solid rgba(255, 255, 255, 0.22);
  box-shadow: inset 0 1px 1px rgba(255, 255, 255, 0.25);
}
.ph-icon svg {
  width: 26px;
  height: 26px;
  fill: none;
  stroke: #fff;
  stroke-width: 1.8;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.ph-text {
  min-width: 0;
}
.ph-title {
  margin: 0;
  font-size: 1.55rem;
  font-weight: 800;
  letter-spacing: -0.015em;
  line-height: 1.15;
}
.ph-sub {
  margin: 0.2rem 0 0;
  font-size: 0.9rem;
  color: rgba(255, 255, 255, 0.82);
}
.ph-actions {
  position: relative;
  z-index: 1;
  flex-shrink: 0;
}

@media (max-width: 560px) {
  .page-header {
    flex-direction: column;
    align-items: flex-start;
  }
}
</style>
