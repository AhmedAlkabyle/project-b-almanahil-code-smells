<script setup>
import { useLanguageStore } from '../stores/language'

const language = useLanguageStore()
</script>

<template>
  <!-- Pill toggle: EN | عربي. The active language is highlighted. -->
  <button
    class="lang-toggle"
    type="button"
    @click="language.toggleLanguage()"
    :aria-label="language.isArabic ? 'Switch to English' : 'التبديل إلى العربية'"
  >
    <span class="opt" :class="{ active: !language.isArabic }">English</span>
    <span class="sep">|</span>
    <span class="opt" :class="{ active: language.isArabic }">عربي</span>
  </button>
</template>

<style scoped>
.lang-toggle {
  display: inline-flex;
  align-items: center;
  /* Lock order so English is always left and عربي always right */
  direction: ltr;
  gap: 0.25rem;
  /* Solid dark pill so the text stays readable over any background */
  background: rgba(15, 35, 80, 0.6);
  backdrop-filter: blur(6px);
  border: 1px solid rgba(255, 255, 255, 0.45);
  border-radius: 999px;
  padding: 0.3rem 0.4rem;
  cursor: pointer;
  font-family: inherit;
  transition: background 0.25s ease;
}
.lang-toggle:hover {
  background: rgba(15, 35, 80, 0.75);
}
.opt {
  color: #fff;
  font-size: 0.85rem;
  font-weight: 600;
  padding: 0.18rem 0.6rem;
  border-radius: 999px;
  transition: background 0.2s ease, color 0.2s ease;
}
/* The selected language gets a solid white chip with navy text */
.opt.active {
  background: #fff;
  color: #1e4c9a;
  font-weight: 700;
}
.sep {
  color: rgba(255, 255, 255, 0.5);
  font-size: 0.85rem;
}
</style>
