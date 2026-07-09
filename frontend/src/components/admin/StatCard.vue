<script setup>
import { ref, watch, onMounted } from 'vue'

// Reusable colored gradient stat card for the dashboard "Overview" row.
const props = defineProps({
  label: { type: String, required: true },
  value: { type: [String, Number], default: 0 },
  badge: { type: String, default: '' },
  // Colour theme: amber | green | slate | blue
  variant: { type: String, default: 'blue' },
  // Inner SVG markup (from components/admin/icons.js)
  icon: { type: String, default: '' }
})

// Count-up animation for numeric values (falls back to the raw value for text).
const displayValue = ref(0)

function animateTo(target) {
  const num = Number(target)
  if (!Number.isFinite(num)) {
    displayValue.value = target // non-numeric → show as-is
    return
  }
  const reduce = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches
  if (reduce || num === 0) {
    displayValue.value = num
    return
  }
  const duration = 900
  const start = performance.now()
  const tick = (t) => {
    const p = Math.min((t - start) / duration, 1)
    // easeOutCubic for a natural deceleration
    const eased = 1 - Math.pow(1 - p, 3)
    displayValue.value = Math.round(num * eased)
    if (p < 1) requestAnimationFrame(tick)
  }
  requestAnimationFrame(tick)
}

onMounted(() => animateTo(props.value))
watch(() => props.value, (v) => animateTo(v))
</script>

<template>
  <div class="stat-card" :class="`v-${variant}`">
    <div class="stat-top">
      <span class="stat-icon">
        <svg viewBox="0 0 24 24" v-html="icon"></svg>
      </span>
      <span v-if="badge" class="stat-badge">{{ badge }}</span>
    </div>
    <div class="stat-body">
      <p class="stat-label">{{ label }}</p>
      <p class="stat-value">{{ displayValue }}</p>
    </div>
  </div>
</template>

<style scoped>
.stat-card {
  position: relative;
  border-radius: 16px;
  padding: 0.8rem 0.95rem;
  min-height: 92px;
  color: #fff;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  gap: 0.5rem;
  box-shadow: 0 10px 22px rgba(30, 41, 59, 0.14);
  overflow: hidden;
  transition: transform 0.25s ease, box-shadow 0.25s ease;
}
.stat-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 18px 34px rgba(30, 41, 59, 0.2);
}
/* Soft glossy highlight sweeping across the top of every card */
.stat-card::before {
  content: '';
  position: absolute;
  top: -60%;
  inset-inline-start: -20%;
  width: 90%;
  height: 120%;
  background: radial-gradient(circle, rgba(255, 255, 255, 0.28), transparent 62%);
  pointer-events: none;
}
/* Decorative glow blob anchored in the far corner */
.stat-card::after {
  content: '';
  position: absolute;
  bottom: -70px;
  inset-inline-end: -50px;
  width: 180px;
  height: 180px;
  border-radius: 50%;
  background: radial-gradient(circle, rgba(255, 255, 255, 0.18), transparent 70%);
  pointer-events: none;
}
/* Colour variants — a distinct, vibrant hue per card */
.v-amber  { background: linear-gradient(135deg, #f7b733, #f2a03d); } /* Students */
.v-rose   { background: linear-gradient(135deg, #fb7185, #f43f5e); } /* Parents  */
.v-indigo { background: linear-gradient(135deg, #4f46e5, #3730a3); } /* Admins   */
.v-green  { background: linear-gradient(135deg, #16a34a, #12b981); } /* Teachers */
.v-cyan   { background: linear-gradient(135deg, #22d3ee, #0891b2); } /* Total Users — clearly ≠ Admin */
/* Kept for reuse elsewhere */
.v-slate  { background: linear-gradient(135deg, #3a4459, #2a3346); }
.v-blue   { background: linear-gradient(135deg, #4361ee, #3b6fe0); }

.stat-top {
  position: relative;
  z-index: 1;
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.stat-body {
  position: relative;
  z-index: 1;
}
.stat-icon {
  width: 33px;
  height: 33px;
  border-radius: 9px;
  background: rgba(255, 255, 255, 0.2);
  border: 1px solid rgba(255, 255, 255, 0.28);
  box-shadow: inset 0 1px 1px rgba(255, 255, 255, 0.3);
  display: inline-flex;
  align-items: center;
  justify-content: center;
}
.stat-icon svg {
  width: 18px;
  height: 18px;
  fill: none;
  stroke: #fff;
  stroke-width: 1.9;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.stat-badge {
  background: rgba(255, 255, 255, 0.24);
  border: 1px solid rgba(255, 255, 255, 0.3);
  font-size: 0.62rem;
  font-weight: 700;
  padding: 0.26rem 0.6rem;
  border-radius: 999px;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  white-space: nowrap;
  backdrop-filter: blur(2px);
}
.stat-label {
  margin: 0;
  font-size: 0.7rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  opacity: 0.92;
}
.stat-value {
  margin: 0.05rem 0 0;
  font-size: 1.5rem;
  font-weight: 800;
  line-height: 1;
  text-shadow: 0 2px 10px rgba(0, 0, 0, 0.16);
}
</style>
