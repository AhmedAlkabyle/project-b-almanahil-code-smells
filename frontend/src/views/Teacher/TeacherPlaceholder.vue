<script setup>
// Temporary "coming soon" page for teacher sections that are built in later
// modules. Mirrors the admin under-construction style, themed green. The section
// title/subtitle come from the route meta (bilingual) via TeacherPageHeader.
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { useLanguageStore } from '../../stores/language'
import TeacherPageHeader from '../../components/teacher/TeacherPageHeader.vue'
import { icons } from '../../components/admin/icons'

const route = useRoute()
const language = useLanguageStore()

const iconByRoute = {
  'teacher-subjects': 'subjects',
  'teacher-attendance': 'attendance',
  'teacher-grades': 'grades',
  'teacher-materials': 'layers',
  'teacher-events': 'events',
  'teacher-profile': 'user'
}
const icon = computed(() => icons[iconByRoute[route.name]] ?? icons.dashboard)

const t = computed(() =>
  language.isArabic
    ? { heading: 'قريباً', message: 'هذا القسم قيد الإنشاء وسيتوفر قريباً.' }
    : { heading: 'Coming soon', message: 'This section is under construction and coming soon.' }
)
</script>

<template>
  <div class="teacher-page" :dir="language.dir">
    <TeacherPageHeader />

    <!-- Coming-soon empty state -->
    <div class="coming-soon">
      <span class="cs-icon"><svg viewBox="0 0 24 24" v-html="icon"></svg></span>
      <h2>{{ t.heading }}</h2>
      <p>{{ t.message }}</p>
    </div>
  </div>
</template>

<style scoped>
.teacher-page {
  display: flex;
  flex-direction: column;
  animation: teacherRise 0.45s ease both;
}
@keyframes teacherRise {
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
  border: 1px solid #e6f0eb;
  border-radius: 18px;
  box-shadow: 0 8px 22px rgba(15, 54, 36, 0.05);
  color: #5c7568;
}
.cs-icon {
  width: 76px;
  height: 76px;
  border-radius: 22px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 0.4rem;
  color: #16a34a;
  background: radial-gradient(circle, #e9f9f0, #dff3e8);
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
  color: #0f2a1e;
}
.coming-soon p {
  margin: 0;
  font-size: 0.95rem;
}

@media (prefers-reduced-motion: reduce) {
  .teacher-page {
    animation: none;
  }
}
</style>
