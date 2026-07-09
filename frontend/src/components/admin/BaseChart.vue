<script setup>
// Thin, reusable Chart.js wrapper. Creates the chart on mount, keeps it in sync when
// the reactive data/options change (e.g. language flip or a fresh fetch), and destroys
// it on unmount so we never leak canvases. `chart.js/auto` registers every controller,
// scale and element for us, so callers just pass a type + data + options.
import { onBeforeUnmount, onMounted, ref, watch } from 'vue'
import Chart from 'chart.js/auto'

// Match the app's font so chart text doesn't look out of place.
Chart.defaults.font.family = "'Segoe UI', system-ui, -apple-system, sans-serif"
Chart.defaults.color = '#64748b'

const props = defineProps({
  type: { type: String, required: true },
  data: { type: Object, required: true },
  options: { type: Object, default: () => ({}) }
})

const canvasEl = ref(null)
let chart = null

onMounted(() => {
  if (!canvasEl.value) return
  chart = new Chart(canvasEl.value, {
    type: props.type,
    data: props.data,
    options: props.options
  })
})

// Re-sync when the data changes (new fetch, or translated labels).
watch(
  () => props.data,
  (d) => {
    if (!chart) return
    chart.data = d
    chart.update()
  },
  { deep: true }
)

// Re-sync when the options change (RTL flip, legend direction…).
watch(
  () => props.options,
  (o) => {
    if (!chart) return
    chart.options = o
    chart.update()
  },
  { deep: true }
)

onBeforeUnmount(() => {
  if (chart) {
    chart.destroy()
    chart = null
  }
})
</script>

<template>
  <div class="chart-holder">
    <canvas ref="canvasEl"></canvas>
  </div>
</template>

<style scoped>
/* Fills its parent; the parent card gives it a fixed height so Chart.js can size the
   canvas responsively (maintainAspectRatio: false) without overflowing. */
.chart-holder {
  position: relative;
  width: 100%;
  height: 100%;
  min-height: 0;
}
</style>
