<script setup>
import { computed } from 'vue'
import { useLanguageStore } from '../../../stores/language'
import StatStrip from '../StatStrip.vue'
import { icons } from '../icons'

// Attendance summary — renders through the shared StatStrip so it matches every
// other admin section (icon chip + count-up + label). Counts reflect the filtered
// list passed in by the parent.
const props = defineProps({
  present: { type: Number, default: 0 },
  absent: { type: Number, default: 0 },
  late: { type: Number, default: 0 }
})

const language = useLanguageStore()

const content = {
  en: { total: 'Total', present: 'Present', absent: 'Absent', late: 'Late' },
  ar: { total: 'الإجمالي', present: 'حاضر', absent: 'غائب', late: 'متأخر' }
}
const t = computed(() => content[language.lang])

const items = computed(() => [
  { label: t.value.total, value: props.present + props.absent + props.late, variant: 'navy', icon: icons.attendance },
  { label: t.value.present, value: props.present, variant: 'green', icon: icons.check },
  { label: t.value.absent, value: props.absent, variant: 'red', icon: icons.xmark },
  { label: t.value.late, value: props.late, variant: 'amber', icon: icons.clock }
])
</script>

<template>
  <StatStrip :items="items" />
</template>
