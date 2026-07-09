<script setup>
import { computed, ref, watch } from 'vue'
import { useLanguageStore } from '../../../stores/language'
import BaseModal from '../../common/BaseModal.vue'
import { createSubject, updateSubject } from '../../../api/subjects'

const props = defineProps({
  open: { type: Boolean, default: false },
  // The subject being edited, or null when adding a new one.
  subject: { type: Object, default: null },
  // Class list for the first dropdown.
  classes: { type: Array, default: () => [] },
  // Existing subjects (used to flag ones already added to the chosen class).
  subjects: { type: Array, default: () => [] }
})
const emit = defineEmits(['close', 'saved'])

const language = useLanguageStore()

// ---- Level-based subject catalogues (constants) ------------------------------------
// The Subject model stores a single (English) Name, so the English label is what we
// save; the Arabic label is shown in the UI when the language is Arabic.
const SECONDARY_SUBJECTS = [
  { en: 'Islamic Education', ar: 'التربية الإسلامية' },
  { en: 'Arabic', ar: 'اللغة العربية' },
  { en: 'English', ar: 'اللغة الإنجليزية' },
  { en: 'Mathematics', ar: 'الرياضيات' },
  { en: 'Science', ar: 'العلوم' },
  { en: 'Computer Science', ar: 'الحاسوب' }
]
const HIGHSCHOOL_SUBJECTS = [
  { en: 'Islamic Education', ar: 'التربية الإسلامية' },
  { en: 'Arabic', ar: 'اللغة العربية' },
  { en: 'English', ar: 'اللغة الإنجليزية' },
  { en: 'Mathematics', ar: 'الرياضيات' },
  { en: 'Physics', ar: 'الفيزياء' },
  { en: 'Chemistry', ar: 'الكيمياء' },
  { en: 'Biology', ar: 'الأحياء' },
  { en: 'Computer Science', ar: 'الحاسوب' }
]

// ---- Bilingual copy (per-component i18n pattern) ----
const content = {
  en: {
    addTitle: 'Add Subject',
    editTitle: 'Edit Subject',
    addSubtitle: 'Pick a class, then choose a subject for its level.',
    editSubtitle: 'Update this subject’s class or name.',
    classLabel: 'Class',
    classPh: 'Select a class',
    subjectLabel: 'Subject',
    subjectPh: 'Select a subject',
    subjectHint: 'Choose a class first to see its subjects.',
    classErr: 'Please choose a class.',
    subjectErr: 'Please choose a subject.',
    alreadyAdded: 'already added',
    allAdded: 'All subjects for this class have already been added.',
    cancel: 'Cancel',
    save: 'Save',
    saving: 'Saving…'
  },
  ar: {
    addTitle: 'إضافة مادة',
    editTitle: 'تعديل المادة',
    addSubtitle: 'اختر صفًا، ثم اختر مادة مناسبة لمرحلته.',
    editSubtitle: 'حدّث صف هذه المادة أو اسمها.',
    classLabel: 'الصف',
    classPh: 'اختر صفًا',
    subjectLabel: 'المادة',
    subjectPh: 'اختر مادة',
    subjectHint: 'اختر صفًا أولاً لعرض المواد المتاحة.',
    classErr: 'يرجى اختيار صف.',
    subjectErr: 'يرجى اختيار مادة.',
    alreadyAdded: 'مضافة بالفعل',
    allAdded: 'تمت إضافة جميع مواد هذا الصف بالفعل.',
    cancel: 'إلغاء',
    save: 'حفظ',
    saving: 'جارٍ الحفظ…'
  }
}
const t = computed(() => content[language.lang])

const isEdit = computed(() => !!props.subject)

// API call state.
const submitting = ref(false)
const apiError = ref('') // backend message shown inline (duplicate, class not found)

// ---- Form state ----
const classId = ref('')
const subjectName = ref('') // the (English) subject name — this is what we save
const errors = ref({ classId: false, subject: false })

// Arabic ordinals for the class display label (grade 1..3).
const arabicOrdinals = ['الأول', 'الثاني', 'الثالث']

// A bilingual, human-readable label for a class, e.g.
//   EN: "1/A — Year 1 — Secondary 2025/2026"
//   AR: "1/A — الأول إعدادي 2025/2026"
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

// The class object currently selected (drives the subject list + level).
const selectedClass = computed(
  () => props.classes.find((c) => c.id === Number(classId.value)) || null
)

// The catalogue for the selected class's level (empty until a class is chosen).
const levelSubjects = computed(() => {
  const lvl = selectedClass.value?.level
  if (lvl === 'HighSchool') return HIGHSCHOOL_SUBJECTS
  if (lvl === 'Secondary') return SECONDARY_SUBJECTS
  return []
})

// Names already taken for the selected class (excludes the subject being edited).
const takenNames = computed(() => {
  const cid = Number(classId.value)
  const set = new Set()
  for (const s of props.subjects) {
    if (s.classId !== cid) continue
    if (isEdit.value && props.subject && s.id === props.subject.id) continue
    set.add(s.name)
  }
  return set
})

// The subject <option> list: catalogue for the level, each flagged if already added.
const subjectOptions = computed(() => {
  const opts = levelSubjects.value.map((s) => ({
    value: s.en,
    label: language.isArabic ? s.ar : s.en,
    taken: takenNames.value.has(s.en)
  }))
  // Keep the edited subject selectable even if it's a custom/legacy name not in the
  // catalogue — but only while its original class is still the one selected.
  if (isEdit.value && props.subject && Number(classId.value) === props.subject.classId) {
    const editName = props.subject.name
    if (editName && !opts.some((o) => o.value === editName)) {
      opts.unshift({ value: editName, label: editName, taken: false })
    }
  }
  return opts
})

// In Add mode, warn when every subject for the class is already added.
const allTaken = computed(
  () => !isEdit.value && subjectOptions.value.length > 0 && subjectOptions.value.every((o) => o.taken)
)

// Reset / hydrate whenever the modal opens.
watch(
  () => props.open,
  (open) => {
    if (!open) return
    classId.value = props.subject?.classId ?? ''
    subjectName.value = props.subject?.name ?? ''
    errors.value = { classId: false, subject: false }
    apiError.value = ''
    submitting.value = false
  }
)

// If the class changes and the chosen subject no longer fits the new level, clear it.
watch(classId, () => {
  if (subjectName.value && !subjectOptions.value.some((o) => o.value === subjectName.value)) {
    subjectName.value = ''
  }
})

function validate() {
  errors.value = {
    classId: classId.value === '' || classId.value === null,
    subject: !subjectName.value
  }
  return !errors.value.classId && !errors.value.subject
}

async function submit() {
  apiError.value = ''
  if (!validate()) return

  // Keep the existing payload shape the backend expects — just sourced from the dropdowns.
  const payload = { name: subjectName.value, classId: Number(classId.value) }
  submitting.value = true
  try {
    const res = isEdit.value
      ? await updateSubject(props.subject.id, payload)
      : await createSubject(payload)
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
    :subtitle="isEdit ? t.editSubtitle : t.addSubtitle"
    @close="emit('close')"
  >
    <div v-if="apiError" class="form-error">
      <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="9" /><path d="M12 8v5M12 16h.01" /></svg>
      <span>{{ apiError }}</span>
    </div>

    <!-- 1) Class first -->
    <div class="field">
      <label>{{ t.classLabel }}</label>
      <select v-model="classId" :class="{ invalid: errors.classId }">
        <option value="" disabled>{{ t.classPh }}</option>
        <option v-for="c in classes" :key="c.id" :value="c.id">{{ classLabel(c) }}</option>
      </select>
      <span v-if="errors.classId" class="err">{{ t.classErr }}</span>
    </div>

    <!-- 2) Subject (level-aware, disabled until a class is chosen) -->
    <div class="field">
      <label>{{ t.subjectLabel }}</label>
      <select
        v-model="subjectName"
        :class="{ invalid: errors.subject }"
        :disabled="!classId"
      >
        <option value="" disabled>{{ t.subjectPh }}</option>
        <option
          v-for="o in subjectOptions"
          :key="o.value"
          :value="o.value"
          :disabled="o.taken"
        >
          {{ o.taken ? `${o.label} — ${t.alreadyAdded}` : o.label }}
        </option>
      </select>
      <span v-if="!classId" class="field-hint">{{ t.subjectHint }}</span>
      <span v-else-if="errors.subject" class="err">{{ t.subjectErr }}</span>

      <div v-if="allTaken" class="note">
        <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="9" /><path d="M12 8v5M12 16h.01" /></svg>
        <span>{{ t.allAdded }}</span>
      </div>
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
/* Small muted helper line under a field (e.g. "choose a class first"). */
.field-hint {
  font-size: 0.78rem;
  color: #8a94a6;
}
/* Make the disabled subject dropdown read clearly as not-yet-available. */
select:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}
/* Size the .note info icon (BaseModal styles the box, not the inline svg — without
   this the <circle> renders as a full-size solid black circle). */
.note svg {
  width: 18px;
  height: 18px;
  flex-shrink: 0;
  margin-top: 1px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.9;
  stroke-linecap: round;
  stroke-linejoin: round;
}
</style>
