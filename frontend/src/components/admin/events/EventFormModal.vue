<script setup>
// Add / edit modal for an announcement or event. Wraps the shared BaseModal so it
// looks identical to the other admin forms, and performs the API call itself.
import { computed, reactive, ref, watch } from 'vue'
import { useLanguageStore } from '../../../stores/language'
import BaseModal from '../../common/BaseModal.vue'
import { createEvent, updateEvent } from '../../../api/events'
import { AUDIENCE_TYPES, audienceLabel } from '../../../utils/eventAudience'

const props = defineProps({
  open: { type: Boolean, default: false },
  event: { type: Object, default: null }, // null = add mode
  classes: { type: Array, default: () => [] } // for the SpecificClass picker
})
const emit = defineEmits(['close', 'saved'])

const language = useLanguageStore()

// ---- Bilingual copy (per-component i18n pattern) ----
const content = {
  en: {
    addTitle: 'New Event',
    editTitle: 'Edit Event',
    addSub: 'Publish a new announcement or event.',
    editSub: 'Update the details of this item.',
    title: 'Title',
    titlePh: 'e.g. Parent-Teacher Meeting',
    dateTime: 'Date & Time',
    description: 'Description',
    descPh: 'Write a short description…',
    type: 'Type',
    audience: 'Audience',
    audienceHint: 'Who should see this item.',
    classLabel: 'Class',
    classPh: 'Select a class…',
    cancel: 'Cancel',
    save: 'Save',
    saving: 'Saving…',
    types: { Announcement: 'Announcement', Event: 'Event' },
    errTitle: 'Title is required.',
    errDate: 'Date & time is required.',
    errDesc: 'Description is required.',
    errClass: 'Please choose the class this event is for.'
  },
  ar: {
    addTitle: 'فعالية جديدة',
    editTitle: 'تعديل الفعالية',
    addSub: 'انشر إعلاناً أو فعالية جديدة.',
    editSub: 'حدّث تفاصيل هذا العنصر.',
    title: 'العنوان',
    titlePh: 'مثال: اجتماع أولياء الأمور والمعلمين',
    dateTime: 'التاريخ والوقت',
    description: 'الوصف',
    descPh: 'اكتب وصفاً مختصراً…',
    type: 'النوع',
    audience: 'الجمهور',
    audienceHint: 'من يجب أن يرى هذا العنصر.',
    classLabel: 'الصف',
    classPh: 'اختر صفاً…',
    cancel: 'إلغاء',
    save: 'حفظ',
    saving: 'جارٍ الحفظ…',
    types: { Announcement: 'إعلان', Event: 'فعالية' },
    errTitle: 'العنوان مطلوب.',
    errDate: 'التاريخ والوقت مطلوبان.',
    errDesc: 'الوصف مطلوب.',
    errClass: 'يرجى اختيار الصف المقصود بهذه الفعالية.'
  }
}
const t = computed(() => content[language.lang])

const TYPES = ['Announcement', 'Event']
const AUDIENCES = AUDIENCE_TYPES

const arabicOrdinals = ['الأول', 'الثاني', 'الثالث']
// Bilingual class label for the SpecificClass picker (e.g. "1/A — Year 1 — Secondary").
function classLabel(c) {
  const bits = [c.name]
  if (c.level && c.grade) {
    bits.push(
      language.isArabic
        ? `${arabicOrdinals[c.grade - 1] ?? c.grade} ${c.level === 'HighSchool' ? 'ثانوي' : 'إعدادي'}`
        : `Year ${c.grade} — ${c.level === 'HighSchool' ? 'High School' : 'Secondary'}`
    )
  }
  let label = bits.filter(Boolean).join(' — ')
  if (c.academicYear) label += ` ${c.academicYear}`
  return label || c.displayName || c.name
}

// Normalize an API date value to what <input type="datetime-local"> expects
// ("yyyy-MM-ddTHH:mm"). Handles the new datetime ("…T14:30:00") and any legacy
// date-only value ("2026-07-05" → midnight).
function toDateTimeLocal(val) {
  if (!val) return ''
  const s = String(val)
  return s.includes('T') ? s.slice(0, 16) : `${s}T00:00`
}

const isEdit = computed(() => !!props.event?.id)

// API call state.
const submitting = ref(false)
const apiError = ref('') // backend message shown inline

// ---- Form state ----
const form = reactive({
  title: '',
  date: '',
  description: '',
  type: 'Announcement',
  audienceType: 'AllUsers',
  targetClassId: '' // string class id, only used when audienceType === 'SpecificClass'
})
const errors = reactive({ title: false, date: false, description: false, targetClass: false })

const isSpecificClass = computed(() => form.audienceType === 'SpecificClass')

// Clear the class picker whenever the audience isn't "A Specific Class".
watch(
  () => form.audienceType,
  (val) => {
    if (val !== 'SpecificClass') {
      form.targetClassId = ''
      errors.targetClass = false
    }
  }
)

function resetErrors() {
  errors.title = false
  errors.date = false
  errors.description = false
  errors.targetClass = false
}

// Hydrate (edit) or reset (add) whenever the modal opens.
watch(
  () => props.open,
  (open) => {
    if (!open) return
    resetErrors()
    apiError.value = ''
    submitting.value = false
    if (props.event) {
      form.title = props.event.title ?? ''
      form.date = toDateTimeLocal(props.event.date)
      form.description = props.event.description ?? ''
      form.type = props.event.type ?? 'Announcement'
      form.audienceType = props.event.audienceType ?? 'AllUsers'
      form.targetClassId = props.event.targetClassId ? String(props.event.targetClassId) : ''
    } else {
      form.title = ''
      form.date = ''
      form.description = ''
      form.type = 'Announcement'
      form.audienceType = 'AllUsers'
      form.targetClassId = ''
    }
  }
)

function validate() {
  errors.title = !form.title.trim()
  errors.date = !form.date
  errors.description = !form.description.trim()
  // A class is required only when targeting a specific class.
  errors.targetClass = isSpecificClass.value && !form.targetClassId
  return !errors.title && !errors.date && !errors.description && !errors.targetClass
}

async function submit() {
  apiError.value = ''
  if (!validate()) return

  const payload = {
    title: form.title.trim(),
    date: form.date, // 'yyyy-MM-ddTHH:mm' → DateTime (wall-clock)
    description: form.description.trim(),
    type: form.type,
    audienceType: form.audienceType,
    // Only send a class id for SpecificClass; the backend forces it null otherwise.
    targetClassId: isSpecificClass.value ? Number(form.targetClassId) : null
  }
  submitting.value = true
  try {
    const res = isEdit.value
      ? await updateEvent(props.event.id, payload)
      : await createEvent(payload)
    emit('saved', { message: res.message })
  } catch (err) {
    apiError.value = err.message
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <BaseModal
    :open="open"
    :title="isEdit ? t.editTitle : t.addTitle"
    :subtitle="isEdit ? t.editSub : t.addSub"
    @close="emit('close')"
  >
    <div v-if="apiError" class="form-error">
      <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="9" /><path d="M12 8v5M12 16h.01" /></svg>
      <span>{{ apiError }}</span>
    </div>

    <div class="field">
      <label>{{ t.title }}</label>
      <input
        v-model="form.title"
        type="text"
        :placeholder="t.titlePh"
        :class="{ invalid: errors.title }"
      />
      <span v-if="errors.title" class="err">{{ t.errTitle }}</span>
    </div>

    <div class="field">
      <label>{{ t.dateTime }}</label>
      <input v-model="form.date" type="datetime-local" :class="{ invalid: errors.date }" />
      <span v-if="errors.date" class="err">{{ t.errDate }}</span>
    </div>

    <div class="field">
      <label>{{ t.type }}</label>
      <select v-model="form.type">
        <option v-for="ty in TYPES" :key="ty" :value="ty">{{ t.types[ty] }}</option>
      </select>
    </div>

    <!-- Audience: exactly one target per event -->
    <div class="field">
      <label>{{ t.audience }}</label>
      <select v-model="form.audienceType">
        <option v-for="a in AUDIENCES" :key="a" :value="a">{{ audienceLabel(a, language.lang) }}</option>
      </select>
      <span class="field-hint">{{ t.audienceHint }}</span>
    </div>

    <!-- Class picker: shown only when 'A Specific Class' is chosen -->
    <div v-if="isSpecificClass" class="field">
      <label>{{ t.classLabel }}</label>
      <select v-model="form.targetClassId" :class="{ invalid: errors.targetClass }">
        <option value="" disabled>{{ t.classPh }}</option>
        <option v-for="c in classes" :key="c.id" :value="String(c.id)">{{ classLabel(c) }}</option>
      </select>
      <span v-if="errors.targetClass" class="err">{{ t.errClass }}</span>
    </div>

    <div class="field">
      <label>{{ t.description }}</label>
      <textarea
        v-model="form.description"
        :placeholder="t.descPh"
        :class="{ invalid: errors.description }"
      ></textarea>
      <span v-if="errors.description" class="err">{{ t.errDesc }}</span>
    </div>

    <template #footer>
      <button type="button" class="btn ghost" @click="emit('close')" :disabled="submitting">{{ t.cancel }}</button>
      <button type="button" class="btn primary" @click="submit" :disabled="submitting">
        {{ submitting ? t.saving : t.save }}
      </button>
    </template>
  </BaseModal>
</template>

<style scoped>
/* Small muted helper line under a field (e.g. "Who should see this item."). */
.field-hint {
  font-size: 0.78rem;
  color: #8a94a6;
}
</style>
