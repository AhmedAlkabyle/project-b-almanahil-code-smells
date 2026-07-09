<script setup>
import { ref, watch, onMounted, onBeforeUnmount } from 'vue'

// Reusable summary-stats row shared by every admin section. Each item is a small
// white card with a coloured icon chip, a count-up number, and a label — the same
// language as the dashboard, giving every section a consistent "at a glance" strip.
//
// items: [{ label, value:Number, suffix?:String, variant?:String, icon?:String }]
//   variant → icon-chip colour: navy | blue | green | amber | indigo | red | slate | cyan
//   icon    → inner SVG markup (from components/admin/icons.js), rendered via v-html
const props = defineProps({
  items: { type: Array, default: () => [] }
})

// Count-up display values (one per item), animated with easeOutCubic.
const display = ref(props.items.map(() => 0))
let raf = 0

function run() {
  cancelAnimationFrame(raf)
  const targets = props.items.map((i) => Number(i.value) || 0)
  const reduce = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches
  if (reduce) {
    display.value = targets.slice()
    return
  }
  const duration = 850
  const start = performance.now()
  const from = targets.map((_, idx) => (typeof display.value[idx] === 'number' ? display.value[idx] : 0))
  const tick = (now) => {
    const p = Math.min((now - start) / duration, 1)
    const eased = 1 - Math.pow(1 - p, 3)
    display.value = targets.map((tVal, idx) => Math.round(from[idx] + (tVal - from[idx]) * eased))
    if (p < 1) raf = requestAnimationFrame(tick)
  }
  raf = requestAnimationFrame(tick)
}

onMounted(run)
onBeforeUnmount(() => cancelAnimationFrame(raf))
// Re-animate whenever any value changes (e.g. filters on report pages).
watch(
  () => props.items.map((i) => i.value),
  run
)
</script>

<template>
  <div class="stats">
    <div v-for="(item, idx) in items" :key="item.label" class="stat">
      <span class="stat-ic" :class="`v-${item.variant || 'navy'}`">
        <svg viewBox="0 0 24 24" v-html="item.icon"></svg>
      </span>
      <div class="stat-text">
        <span class="stat-num">{{ display[idx] }}<span v-if="item.suffix" class="stat-suffix">{{ item.suffix }}</span></span>
        <span class="stat-lbl">{{ item.label }}</span>
      </div>
    </div>
  </div>
</template>

<style scoped>
.stats {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(190px, 1fr));
  gap: 1rem;
}
.stat {
  display: flex;
  align-items: center;
  gap: 0.9rem;
  padding: 1rem 1.1rem;
  background: #fff;
  border: 1px solid #eaeef6;
  border-radius: 16px;
  box-shadow: 0 8px 22px rgba(30, 41, 59, 0.05);
  transition: transform 0.2s ease, box-shadow 0.2s ease;
}
.stat:hover {
  transform: translateY(-3px);
  box-shadow: 0 14px 30px rgba(30, 41, 59, 0.1);
}
.stat-ic {
  width: 44px;
  height: 44px;
  flex-shrink: 0;
  border-radius: 12px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
}
.stat-ic svg {
  width: 22px;
  height: 22px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.9;
  stroke-linecap: round;
  stroke-linejoin: round;
}
/* Icon-chip colour variants */
.v-navy {
  color: var(--ds-navy, #1e4c9a);
  background: rgba(30, 76, 154, 0.1);
}
.v-blue {
  color: #2563eb;
  background: #dbeafe;
}
.v-green {
  color: #15803d;
  background: #dcfce7;
}
.v-amber {
  color: #b45309;
  background: #fef3c7;
}
.v-indigo {
  color: #4f46e5;
  background: #e0e7ff;
}
.v-red {
  color: #dc2626;
  background: #fee2e2;
}
.v-slate {
  color: #475569;
  background: #e5e9f0;
}
.v-cyan {
  color: #0891b2;
  background: #cffafe;
}
.stat-text {
  display: flex;
  flex-direction: column;
  line-height: 1.2;
  min-width: 0;
}
.stat-num {
  font-size: 1.5rem;
  font-weight: 800;
  color: #0f2444;
}
.stat-suffix {
  font-size: 1rem;
  font-weight: 700;
  margin-inline-start: 1px;
}
.stat-lbl {
  font-size: 0.8rem;
  font-weight: 600;
  color: #8a94a6;
}
</style>
