<script setup>
// Temporary empty-state page for admin sections that are built in later modules.
// The title comes from the route meta (bilingual), so each section reads correctly.
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { useLanguageStore } from '../../stores/language'
import { icons } from '../../components/admin/icons'

const route = useRoute()
const language = useLanguageStore()

const title = computed(() => route.meta.title?.[language.lang] ?? '')
const message = computed(() =>
  language.isArabic ? 'هذا القسم قيد الإنشاء وسيتوفر قريباً.' : 'This section is under construction and coming soon.'
)
</script>

<template>
  <div class="placeholder">
    <span class="ph-icon"><svg viewBox="0 0 24 24" v-html="icons.settings"></svg></span>
    <h2>{{ title }}</h2>
    <p>{{ message }}</p>
  </div>
</template>

<style scoped>
.placeholder {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  gap: 0.5rem;
  min-height: 60vh;
  color: #64748b;
}
.ph-icon {
  width: 72px;
  height: 72px;
  border-radius: 20px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  background: #eef2fb;
  color: #1e4c9a;
  margin-bottom: 0.5rem;
}
.ph-icon svg {
  width: 34px;
  height: 34px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.7;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.placeholder h2 {
  margin: 0;
  font-size: 1.6rem;
  font-weight: 800;
  color: #0f2444;
}
.placeholder p {
  margin: 0;
  font-size: 0.95rem;
}
</style>
