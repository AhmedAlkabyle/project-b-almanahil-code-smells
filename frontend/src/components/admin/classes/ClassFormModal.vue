<script setup>
import { computed, reactive, ref, watch } from 'vue'
import { useLanguageStore } from '../../../stores/language'
import BaseModal from '../../common/BaseModal.vue'
import { createClass, updateClass } from '../../../api/classes'

const props = defineProps({
  open: { type: Boolean, default: false },
  // The class being edited, or null when adding a new one.
  classItem: { type: Object, default: null }
})
const emit = defineEmits(['close', 'saved'])

const language = useLanguageStore()
const isArabic = computed(() => language.lang === 'ar')

// ---- Bilingual copy (per-component i18n pattern) ----
const content = {
  en: {
    addTitle: 'Add Class',
    editTitle: 'Edit Class',
    addSubtitle: 'Create a new class for the school.',
    editSubtitle: 'Update this class’s details.',
    classDetails: 'Class Details',
    academicYear: 'Academic Year',
    selectYear: 'Select a year',
    level: 'Level',
    selectLevel: 'Select a level',
    grade: 'Year',
    selectGrade: 'Select a year',
    section: 'Section',
    selectSection: 'Select a section',
    description: 'Description',
    descriptionPh: 'Optional short description…',
    optional: 'Optional',
    preview: 'Preview',
    cancel: 'Cancel',
    save: 'Save',
    saving: 'Saving…',
    errYear: 'Academic year is required.',
    errYearFormat: 'Use the format 2025/2026.',
    errLevel: 'Please choose a level.',
    errGrade: 'Please choose a year.',
    errSection: 'Please choose a section.'
  },
  ar: {
    addTitle: 'إضافة صف',
    editTitle: 'تعديل صف',
    addSubtitle: 'إنشاء صف جديد للمدرسة.',
    editSubtitle: 'تحديث بيانات هذا الصف.',
    classDetails: 'بيانات الصف',
    academicYear: 'العام الدراسي',
    selectYear: 'اختر العام',
    level: 'المرحلة',
    selectLevel: 'اختر المرحلة',
    grade: 'الصف',
    selectGrade: 'اختر الصف',
    section: 'الشعبة',
    selectSection: 'اختر الشعبة',
    description: 'الوصف',
    descriptionPh: 'وصف قصير اختياري…',
    optional: 'اختياري',
    preview: 'معاينة',
    cancel: 'إلغاء',
    save: 'حفظ',
    saving: 'جارٍ الحفظ…',
    errYear: 'العام الدراسي مطلوب.',
    errYearFormat: 'استخدم الصيغة 2025/2026.',
    errLevel: 'يرجى اختيار المرحلة.',
    errGrade: 'يرجى اختيار الصف.',
    errSection: 'يرجى اختيار الشعبة.'
  }
}
const t = computed(() => content[language.lang])

const isEditing = computed(() => !!props.classItem)

// ---- Structured option data (keys match the backend's allowed values) ----
const YEAR_PATTERN = /^\d{4}\/\d{4}$/
const sectionArabic = { A: 'أ', B: 'ب', C: 'ج', D: 'د' }
const arabicOrdinals = ['الأول', 'الثاني', 'الثالث'] // grade 1..3

// Academic-year dropdown: two years back to two years forward, formatted "YYYY/YYYY+1".
// The default (used for a new class) is the current academic year: this calendar year → next.
const currentAcademicYear = (() => {
  const y = new Date().getFullYear()
  return `${y}/${y + 1}`
})()
const yearOptions = computed(() => {
  const y = new Date().getFullYear()
  const list = [y - 2, y - 1, y, y + 1, y + 2].map((s) => `${s}/${s + 1}`)
  // When editing a class whose saved year falls outside this window, keep it selectable.
  if (form.academicYear && !list.includes(form.academicYear)) list.unshift(form.academicYear)
  return list
})

// Level dropdown shows both scripts, primary language first.
const levelOptions = computed(() =>
  isArabic.value
    ? [
        { value: 'Secondary', label: 'إعدادي (Secondary)' },
        { value: 'HighSchool', label: 'ثانوي (High School)' }
      ]
    : [
        { value: 'Secondary', label: 'Secondary (إعدادي)' },
        { value: 'HighSchool', label: 'High School (ثانوي)' }
      ]
)

// Section dropdown: أ (A) … د (D), primary language first.
const sectionOptions = computed(() =>
  ['A', 'B', 'C', 'D'].map((s) => ({
    value: s,
    label: isArabic.value ? `${sectionArabic[s]} (${s})` : `${s} (${sectionArabic[s]})`
  }))
)

// Grade label depends on the chosen level AND the language (matches the backend's Arabic label).
function gradeLabel(g) {
  if (isArabic.value) {
    const ord = arabicOrdinals[g - 1] ?? String(g)
    if (!form.level) return ord
    return `${ord} ${form.level === 'HighSchool' ? 'ثانوي' : 'إعدادي'}`
  }
  if (!form.level) return `Year ${g}`
  return `Year ${g} — ${form.level === 'HighSchool' ? 'High School' : 'Secondary'}`
}
const gradeOptions = computed(() => [1, 2, 3].map((g) => ({ value: g, label: gradeLabel(g) })))

// ---- Form state ----
const form = reactive({
  academicYear: '',
  level: '',
  grade: '',
  section: '',
  description: ''
})
const errors = ref({})

// Live preview of the class that will be created, e.g. "1/A — الأول إعدادي 2025/2026".
const composedName = computed(() => {
  if (!form.grade || !form.section) return ''
  const shortName = `${form.grade}/${form.section}`
  const label = gradeLabel(Number(form.grade))
  const year = form.academicYear.trim()
  return `${shortName} — ${label}${year ? ` ${year}` : ''}`
})

// API call state.
const submitting = ref(false)
const apiError = ref('') // backend message shown inline (e.g. duplicate combination)

// Reset / hydrate the form whenever the modal opens.
watch(
  () => props.open,
  (isOpen) => {
    if (!isOpen) return
    errors.value = {}
    apiError.value = ''
    submitting.value = false
    const c = props.classItem
    form.academicYear = c?.academicYear ?? currentAcademicYear
    form.level = c?.level ?? ''
    form.grade = c?.grade ?? ''
    form.section = c?.section ?? ''
    form.description = c?.description ?? ''
  }
)

function validate() {
  const e = {}
  const year = form.academicYear.trim()
  if (!year) e.academicYear = t.value.errYear
  else if (!YEAR_PATTERN.test(year)) e.academicYear = t.value.errYearFormat
  if (!form.level) e.level = t.value.errLevel
  if (!form.grade) e.grade = t.value.errGrade
  if (!form.section) e.section = t.value.errSection
  errors.value = e
  return Object.keys(e).length === 0
}

async function submit() {
  apiError.value = ''
  if (!validate()) return

  const payload = {
    academicYear: form.academicYear.trim(),
    level: form.level,
    grade: Number(form.grade),
    section: form.section,
    description: form.description.trim() || null,
    // Compatibility shim: the current backend AUTO-COMPOSES Name from grade + section and
    // ignores this field. An older backend build (e.g. one that hasn't been restarted yet)
    // still requires a non-empty Name — sending it keeps Save working against both.
    name: form.grade && form.section ? `${form.grade}/${form.section}` : ''
  }
  submitting.value = true
  try {
    const res = isEditing.value
      ? await updateClass(props.classItem.id, payload)
      : await createClass(payload)
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
    size="wide"
    :title="isEditing ? t.editTitle : t.addTitle"
    :subtitle="isEditing ? t.editSubtitle : t.addSubtitle"
    @close="emit('close')"
  >
    <div class="cf-form">
      <div v-if="apiError" class="form-error">
        <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="9" /><path d="M12 8v5M12 16h.01" /></svg>
        <span>{{ apiError }}</span>
      </div>

      <!-- Section 1: Class details — Academic Year + Level, then Grade + Section -->
      <section class="form-section">
        <h4 class="section-title">
          <span class="sec-ic"><svg viewBox="0 0 24 24"><path d="M3 21h18" /><path d="M5 21V8l7-4 7 4v13" /><path d="M9 21v-5h6v5" /></svg></span>
          {{ t.classDetails }}
        </h4>

        <div class="pi-grid">
          <!-- Academic Year (dropdown: two years back → two years forward) -->
          <div class="field">
            <label for="cf-year">{{ t.academicYear }}</label>
            <div class="in">
              <span class="in-ic"><svg viewBox="0 0 24 24"><rect x="3" y="4" width="18" height="17" rx="2" /><path d="M3 9h18M8 3v4M16 3v4" /></svg></span>
              <select id="cf-year" v-model="form.academicYear" :class="{ invalid: errors.academicYear }">
                <option value="" disabled>{{ t.selectYear }}</option>
                <option v-for="y in yearOptions" :key="y" :value="y">{{ y }}</option>
              </select>
            </div>
            <span v-if="errors.academicYear" class="err">{{ errors.academicYear }}</span>
          </div>

          <!-- Level -->
          <div class="field">
            <label for="cf-level">{{ t.level }}</label>
            <div class="in">
              <span class="in-ic"><svg viewBox="0 0 24 24"><path d="m12 3 9 5-9 5-9-5 9-5z" /><path d="m3 12 9 5 9-5" /></svg></span>
              <select id="cf-level" v-model="form.level" :class="{ invalid: errors.level }">
                <option value="" disabled>{{ t.selectLevel }}</option>
                <option v-for="o in levelOptions" :key="o.value" :value="o.value">{{ o.label }}</option>
              </select>
            </div>
            <span v-if="errors.level" class="err">{{ errors.level }}</span>
          </div>

          <!-- Grade (labels adapt to the selected level) -->
          <div class="field">
            <label for="cf-grade">{{ t.grade }}</label>
            <div class="in">
              <span class="in-ic"><svg viewBox="0 0 24 24"><path d="M12 3 1 8l11 5 9-4.09" /><path d="M6 10.5V15c0 1.5 3 3 6 3s6-1.5 6-3v-4.5" /></svg></span>
              <select id="cf-grade" v-model="form.grade" :class="{ invalid: errors.grade }">
                <option value="" disabled>{{ t.selectGrade }}</option>
                <option v-for="o in gradeOptions" :key="o.value" :value="o.value">{{ o.label }}</option>
              </select>
            </div>
            <span v-if="errors.grade" class="err">{{ errors.grade }}</span>
          </div>

          <!-- Section -->
          <div class="field">
            <label for="cf-section">{{ t.section }}</label>
            <div class="in">
              <span class="in-ic"><svg viewBox="0 0 24 24"><path d="M4 9h16M4 15h16M10 3 8 21M16 3l-2 18" /></svg></span>
              <select id="cf-section" v-model="form.section" :class="{ invalid: errors.section }">
                <option value="" disabled>{{ t.selectSection }}</option>
                <option v-for="o in sectionOptions" :key="o.value" :value="o.value">{{ o.label }}</option>
              </select>
            </div>
            <span v-if="errors.section" class="err">{{ errors.section }}</span>
          </div>

          <!-- Live preview of the composed class -->
          <div v-if="composedName" class="preview span-2">
            <span class="preview-label">{{ t.preview }}</span>
            <span class="preview-value">{{ composedName }}</span>
          </div>
        </div>
      </section>

      <!-- Section 2: Description (optional) -->
      <section class="form-section">
        <h4 class="section-title">
          <span class="sec-ic"><svg viewBox="0 0 24 24"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" /><path d="M14 2v6h6" /><path d="M8 13h8M8 17h6" /></svg></span>
          {{ t.description }}
          <span class="opt">({{ t.optional }})</span>
        </h4>

        <div class="field">
          <textarea id="cf-desc" v-model="form.description" :placeholder="t.descriptionPh"></textarea>
        </div>
      </section>
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
/* Matches the Add/Edit User form: card sections, icon-badge headings, a 2-column
   grid, and crisp leading-icon inputs — so both modals look like one family. */
.cf-form {
  display: flex;
  flex-direction: column;
  gap: 1.3rem;
  /* Navy accent (the classes colour) used for badges, focus and the preview. */
  --accent: var(--ds-navy, #1e4c9a);
  --accent-soft: #e8eefb;
  --accent-ring: rgba(30, 76, 154, 0.14);
}

/* Section cards */
.form-section {
  display: flex;
  flex-direction: column;
  gap: 1.15rem;
  padding: 1.3rem 1.4rem 1.45rem;
  background: linear-gradient(180deg, #fcfdff, #f8fafd);
  border: 1px solid #e9eef6;
  border-radius: 16px;
  box-shadow: 0 6px 18px rgba(30, 41, 59, 0.05);
}
.section-title {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  margin: 0;
  font-size: 0.95rem;
  font-weight: 800;
  letter-spacing: -0.01em;
  color: #0f2444;
}
.sec-ic {
  width: 34px;
  height: 34px;
  flex-shrink: 0;
  border-radius: 10px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: var(--accent);
  background: var(--accent-soft);
}
.sec-ic svg {
  width: 18px;
  height: 18px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.9;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.opt {
  font-weight: 600;
  font-size: 0.85rem;
  color: #94a3b8;
}

/* Two-column grid; full-width items use .span-2. minmax(0,1fr) prevents overflow. */
.pi-grid {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
  gap: 1.15rem 1.35rem;
}
.span-2 {
  grid-column: 1 / -1;
}

/* Leading icon inside each field */
.cf-form :deep(.field) {
  min-width: 0;
}
.cf-form :deep(.in) {
  position: relative;
  min-width: 0;
}
.cf-form :deep(.in-ic) {
  position: absolute;
  inset-inline-start: 0.9rem;
  top: 50%;
  transform: translateY(-50%);
  width: 18px;
  height: 18px;
  display: inline-flex;
  color: #9aa5b5;
  pointer-events: none;
  transition: color 0.2s ease;
}
.cf-form :deep(.in-ic svg) {
  width: 100%;
  height: 100%;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.9;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.cf-form :deep(.in:focus-within .in-ic) {
  color: var(--accent);
}
/* Crisp white inputs + room for the icon. Doubled .cf-form.cf-form raises specificity
   just above BaseModal's shared field rules so only THIS form changes. */
.cf-form.cf-form :deep(.field label) {
  font-size: 0.82rem;
  font-weight: 600;
  color: #47536a;
}
.cf-form.cf-form :deep(.field input),
.cf-form.cf-form :deep(.field select),
.cf-form.cf-form :deep(.field textarea) {
  box-sizing: border-box;
  min-width: 0;
  max-width: 100%;
  background: #fff;
  border-color: #e3e9f3;
  border-radius: 12px;
  box-shadow: 0 1px 2px rgba(15, 23, 42, 0.04);
}
.cf-form.cf-form :deep(.in input),
.cf-form.cf-form :deep(.in select) {
  padding-inline-start: 2.65rem;
}
.cf-form.cf-form :deep(.field input:focus),
.cf-form.cf-form :deep(.field select:focus),
.cf-form.cf-form :deep(.field textarea:focus) {
  background: #fff;
  border-color: var(--accent);
  box-shadow: 0 0 0 3px var(--accent-ring);
}
.cf-form.cf-form :deep(.field input.invalid),
.cf-form.cf-form :deep(.field select.invalid),
.cf-form.cf-form :deep(.field textarea.invalid) {
  border-color: #ef4444;
  background: #fef2f2;
}

/* Live preview of the composed class name */
.preview {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 0.3rem 0.7rem;
  padding: 0.8rem 1rem;
  border-radius: 12px;
  background: linear-gradient(135deg, #eef4ff, #f4f8ff);
  border: 1px dashed #b9ccec;
}
.preview-label {
  font-size: 0.72rem;
  font-weight: 800;
  letter-spacing: 0.05em;
  text-transform: uppercase;
  color: #64748b;
  flex-shrink: 0;
}
.preview-value {
  font-size: 0.98rem;
  font-weight: 800;
  color: var(--accent);
}

/* Stack to one column on very narrow screens */
@media (max-width: 540px) {
  .pi-grid {
    grid-template-columns: 1fr;
  }
}
</style>
