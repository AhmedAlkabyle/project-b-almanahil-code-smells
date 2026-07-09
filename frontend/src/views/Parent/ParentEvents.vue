<script setup>
// Parent "Events" — the events targeted at this parent (GET /api/events/my, filtered by
// AllParents + their children's levels/classes server-side). Slate theme, bilingual, RTL.
import { useLanguageStore } from '../../stores/language'
import ParentPageHeader from '../../components/parent/ParentPageHeader.vue'
import EventsFeed from '../../components/shared/EventsFeed.vue'

const language = useLanguageStore()
</script>

<template>
  <div class="parent-events" :dir="language.dir">
    <ParentPageHeader />
    <EventsFeed theme="slate" />
  </div>
</template>

<style scoped>
.parent-events {
  display: flex;
  flex-direction: column;
  animation: p-rise 0.45s ease both;
}
@keyframes p-rise {
  from { opacity: 0; transform: translateY(16px); }
  to { opacity: 1; transform: translateY(0); }
}
@media (prefers-reduced-motion: reduce) {
  .parent-events {
    animation: none;
  }
}
</style>
