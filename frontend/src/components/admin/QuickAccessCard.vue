<script setup>
// Reusable gradient "module" card in the dashboard Quick Access grid.
// Renders a router-link so the whole card navigates to an admin section.
defineProps({
  title: { type: String, required: true },
  subtitle: { type: String, default: '' },
  to: { type: String, required: true },
  variant: { type: String, default: 'indigo' }, // colour theme
  icon: { type: String, default: '' } // inner SVG markup
})
</script>

<template>
  <router-link :to="to" class="qa-card" :class="`v-${variant}`">
    <span class="qa-icon">
      <svg viewBox="0 0 24 24" v-html="icon"></svg>
    </span>
    <span class="qa-arrow" aria-hidden="true">
      <svg viewBox="0 0 24 24"><path d="M9 6l6 6-6 6" /></svg>
    </span>
    <div class="qa-text">
      <span class="qa-title">{{ title }}</span>
      <span v-if="subtitle" class="qa-subtitle">{{ subtitle }}</span>
    </div>
  </router-link>
</template>

<style scoped>
.qa-card {
  position: relative;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  min-height: 130px;
  padding: 1.25rem;
  border-radius: 18px;
  color: #fff;
  text-decoration: none;
  box-shadow: 0 12px 26px rgba(30, 41, 59, 0.16);
  transition: transform 0.25s ease, box-shadow 0.25s ease;
}
.qa-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 20px 38px rgba(30, 41, 59, 0.24);
}
.qa-card:hover .qa-arrow {
  opacity: 1;
  transform: translateX(0);
}
/* Colour variants */
.v-indigo { background: linear-gradient(135deg, #4f6bed, #4361ee); }
.v-sky    { background: linear-gradient(135deg, #2f8fe0, #2472d3); }
.v-violet { background: linear-gradient(135deg, #8b5cf6, #7c3aed); }
.v-green  { background: linear-gradient(135deg, #12b981, #0f9d76); }
.v-rose   { background: linear-gradient(135deg, #f43f7e, #e11d63); }
.v-cyan   { background: linear-gradient(135deg, #22b8cf, #1a97cf); }
.v-orange { background: linear-gradient(135deg, #f7912f, #ef7d18); }

.qa-icon {
  width: 44px;
  height: 44px;
  border-radius: 12px;
  background: rgba(255, 255, 255, 0.2);
  display: inline-flex;
  align-items: center;
  justify-content: center;
}
.qa-icon svg {
  width: 22px;
  height: 22px;
  fill: none;
  stroke: #fff;
  stroke-width: 1.9;
  stroke-linecap: round;
  stroke-linejoin: round;
}
/* Arrow appears in the top corner on hover */
.qa-arrow {
  position: absolute;
  top: 1.25rem;
  inset-inline-end: 1.25rem;
  opacity: 0;
  transform: translateX(-4px);
  transition: opacity 0.25s ease, transform 0.25s ease;
}
.qa-arrow svg {
  width: 20px;
  height: 20px;
  fill: none;
  stroke: #fff;
  stroke-width: 2.2;
  stroke-linecap: round;
  stroke-linejoin: round;
}
/* Flip the arrow direction under RTL */
:global([dir='rtl']) .qa-arrow svg {
  transform: scaleX(-1);
}
.qa-text {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
}
.qa-title {
  font-size: 1.05rem;
  font-weight: 800;
}
.qa-subtitle {
  font-size: 0.82rem;
  opacity: 0.9;
}
</style>
