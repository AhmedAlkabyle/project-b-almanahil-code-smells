<script setup>
// Student "Events" — the events targeted at this student (GET /api/events/my, filtered by
// their class level + classId server-side). Orange theme, bilingual, RTL.
import { useLanguageStore } from '../../stores/language'
import StudentPageHeader from '../../components/student/StudentPageHeader.vue'
import EventsFeed from '../../components/shared/EventsFeed.vue'

const language = useLanguageStore()
</script>

<template>
  <div class="student-events" :dir="language.dir">
    <StudentPageHeader />
    <EventsFeed theme="orange" />
  </div>
</template>

<style scoped>
.student-events {
  display: flex;
  flex-direction: column;
  animation: s-rise 0.45s ease both;
}
@keyframes s-rise {
  from { opacity: 0; transform: translateY(16px); }
  to { opacity: 1; transform: translateY(0); }
}
@media (prefers-reduced-motion: reduce) {
  .student-events {
    animation: none;
  }
}
</style>
