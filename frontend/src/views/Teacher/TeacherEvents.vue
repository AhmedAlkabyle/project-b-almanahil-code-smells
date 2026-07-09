<script setup>
// Teacher "Events & Announcements" — the events targeted at this teacher (GET
// /api/events/my, filtered by their level server-side). Green theme, bilingual, RTL.
import { useLanguageStore } from '../../stores/language'
import TeacherPageHeader from '../../components/teacher/TeacherPageHeader.vue'
import EventsFeed from '../../components/shared/EventsFeed.vue'

const language = useLanguageStore()
</script>

<template>
  <div class="teacher-events" :dir="language.dir">
    <TeacherPageHeader />
    <EventsFeed theme="green" />
  </div>
</template>

<style scoped>
.teacher-events {
  display: flex;
  flex-direction: column;
  animation: t-rise 0.45s ease both;
}
@keyframes t-rise {
  from { opacity: 0; transform: translateY(16px); }
  to { opacity: 1; transform: translateY(0); }
}
@media (prefers-reduced-motion: reduce) {
  .teacher-events {
    animation: none;
  }
}
</style>
