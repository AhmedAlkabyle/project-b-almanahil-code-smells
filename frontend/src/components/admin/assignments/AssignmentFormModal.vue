<script setup>
// Cascading "Assign Subject" dialog — a step-by-step form where each choice filters
// the next:  1) level  →  2) teacher  →  3) year  →  4) class/section  →  5) subject.
// Each step is disabled until the previous one is chosen, and changing an earlier
// step resets every later step. Admin only, bilingual (EN + AR) + RTL. It reuses the
// existing assignment API (POST { teacherId, subjectId } — the subject implies its class).
import { computed, ref, watch } from 'vue'
import { useLanguageStore } from '../../../stores/language'
import BaseModal from '../../common/BaseModal.vue'
import { createAssignment } from '../../../api/assignments'

const props = defineProps({
  open: { type: Boolean, default: false },
  // Teachers as { id, name, level } — level is the TeacherLevel they teach.
  teachers: { type: Array, default: () => [] },
  // Full class objects { id, name, level, grade, section, academicYear, displayName }.
  classes: { type: Array, default: () => [] },
  // Full subject list { id, name, classId, className }.
  subjects: { type: Array, default: () => [] },
  // Currently assigned { teacherId, subjectId } pairs, for the duplicate check.
  existingPairs: { type: Array, default: () => [] }
})
const emit = defineEmits(['close', 'saved'])

const language = useLanguageStore()

// ---- Bilingual copy (per-component i18n pattern) ----
const content = {
  en: {
    title: 'Assign Subject',
    subtitle: 'Follow the steps: pick a level, a teacher, then the exact class and subject.',
    level: 'Level',
    secondary: 'Secondary',
    highSchool: 'High School',
    teacher: 'Teacher',
    teacherPh: 'Select a teacher…',
    year: 'Year',
    yearPh: 'Select a year…',
    class: 'Class / Section',
    classPh: 'Select a class…',
    subject: 'Subject',
    subjectPh: 'Select a subject…',
    assignedTag: 'already assigned',
    chooseLevelFirst: 'Choose a level first',
    chooseTeacherFirst: 'Choose a teacher first',
    chooseYearFirst: 'Choose a year first',
    chooseClassFirst: 'Choose a class first',
    noTeachers: 'No teachers for this level yet.',
    noYears: 'No classes for this level yet.',
    noSubjects: 'This class has no subjects yet.',
    duplicate: 'This teacher is already assigned to this subject.',
    okPrefix: 'Assigned',
    done: 'Done',
    save: 'Assign',
    saving: 'Assigning…'
  },
  ar: {
    title: 'تعيين مادة',
    subtitle: 'اتبع الخطوات: اختر المرحلة، ثم المعلم، ثم الصف والمادة بدقة.',
    level: 'المرحلة',
    secondary: 'إعدادي',
    highSchool: 'ثانوي',
    teacher: 'المعلم',
    teacherPh: 'اختر معلمًا…',
    year: 'السنة',
    yearPh: 'اختر السنة…',
    class: 'الصف / الشعبة',
    classPh: 'اختر صفًا…',
    subject: 'المادة',
    subjectPh: 'اختر مادة…',
    assignedTag: 'معيّنة بالفعل',
    chooseLevelFirst: 'اختر المرحلة أولًا',
    chooseTeacherFirst: 'اختر معلمًا أولًا',
    chooseYearFirst: 'اختر السنة أولًا',
    chooseClassFirst: 'اختر صفًا أولًا',
    noTeachers: 'لا يوجد معلمون لهذه المرحلة بعد.',
    noYears: 'لا توجد صفوف لهذه المرحلة بعد.',
    noSubjects: 'لا توجد مواد لهذا الصف بعد.',
    duplicate: 'هذا المعلم معيّن بالفعل لهذه المادة.',
    okPrefix: 'تم تعيين',
    done: 'تم',
    save: 'تعيين',
    saving: 'جارٍ التعيين…'
  }
}
const t = computed(() => content[language.lang])

const arabicOrdinals = ['الأول', 'الثاني', 'الثالث'] // grade 1..3

// ---- Form state ----
const form = ref({ level: '', teacherId: '', year: '', classId: '', subjectId: '' })
const submitting = ref(false)
const apiError = ref('') // backend / duplicate message shown as a top banner
const okMsg = ref('') // inline success note (modal stays open for more assigns)
let okTimer = null

// Pairs assigned during THIS modal session — lets the just-assigned subject grey out
// instantly, before the parent's list reload lands and refreshes existingPairs.
const localAssigned = ref(new Set())
const pairKey = (teacherId, subjectId) => `${Number(teacherId)}:${Number(subjectId)}`
function isAssigned(teacherId, subjectId) {
  if (!teacherId || !subjectId) return false
  if (localAssigned.value.has(pairKey(teacherId, subjectId))) return true
  return props.existingPairs.some(
    (p) => Number(p.teacherId) === Number(teacherId) && Number(p.subjectId) === Number(subjectId)
  )
}

// ---- Cascade filters (each derived from the step before it) ----
// 2) teachers whose TeacherLevel matches the chosen level.
const levelTeachers = computed(() => props.teachers.filter((tt) => tt.level === form.value.level))
// 3) the years (grades) that actually have classes at this level — no dead-ends.
const availableYears = computed(() => {
  const grades = new Set()
  for (const c of props.classes) {
    if (c.level === form.value.level && c.grade) grades.add(c.grade)
  }
  return [...grades].sort((a, b) => a - b)
})
// 4) classes matching the chosen level + year.
const levelYearClasses = computed(() =>
  props.classes.filter((c) => c.level === form.value.level && c.grade === Number(form.value.year))
)
// 5) subjects belonging to the chosen class, each flagged if already assigned.
const subjectOptions = computed(() =>
  props.subjects
    .filter((s) => s.classId === Number(form.value.classId))
    .map((s) => ({ id: s.id, name: s.name, assigned: isAssigned(form.value.teacherId, s.id) }))
)

const canAssign = computed(
  () =>
    !!form.value.level &&
    !!form.value.teacherId &&
    !!form.value.year &&
    !!form.value.classId &&
    !!form.value.subjectId
)

// ---- Labels ----
function yearLabel(grade) {
  return language.isArabic ? (arabicOrdinals[grade - 1] ?? String(grade)) : `Year ${grade}`
}
// Neutral short label like "1/A" (never the raw Arabic display name in an English UI).
function classLabel(c) {
  return c.name || c.displayName || `#${c.id}`
}

// ---- Cascade reset: changing a step clears every later step ----
watch(
  () => form.value.level,
  () => {
    form.value.teacherId = ''
    form.value.year = ''
    form.value.classId = ''
    form.value.subjectId = ''
  }
)
watch(
  () => form.value.teacherId,
  () => {
    form.value.year = ''
    form.value.classId = ''
    form.value.subjectId = ''
  }
)
watch(
  () => form.value.year,
  () => {
    form.value.classId = ''
    form.value.subjectId = ''
  }
)
watch(
  () => form.value.classId,
  () => {
    form.value.subjectId = ''
  }
)

// Reset the whole form whenever the modal (re)opens.
watch(
  () => props.open,
  (open) => {
    if (!open) return
    form.value = { level: '', teacherId: '', year: '', classId: '', subjectId: '' }
    apiError.value = ''
    okMsg.value = ''
    submitting.value = false
    localAssigned.value = new Set()
    clearTimeout(okTimer)
  }
)

function pickLevel(level) {
  if (form.value.level !== level) form.value.level = level
}

async function submit() {
  apiError.value = ''
  if (!canAssign.value) return

  const teacherId = Number(form.value.teacherId)
  const subjectId = Number(form.value.subjectId)

  // Exact-duplicate guard (same teacher + same subject row). The SAME subject name in a
  // DIFFERENT class is a different subjectId, so that combination is always allowed.
  if (isAssigned(teacherId, subjectId)) {
    apiError.value = t.value.duplicate
    return
  }

  // Capture names for the success note before we clear the subject.
  const subj = props.subjects.find((s) => s.id === subjectId)
  const cls = props.classes.find((c) => c.id === Number(form.value.classId))

  submitting.value = true
  try {
    const res = await createAssignment({ teacherId, subjectId })
    // Remember locally so the just-assigned subject greys out immediately.
    localAssigned.value = new Set(localAssigned.value).add(pairKey(teacherId, subjectId))
    // Refresh the list + flash on the parent (it keeps the modal open for more assigns).
    emit('saved', { message: res.message })
    // Keep level + teacher + year + class; clear only the subject for a fast re-assign.
    form.value.subjectId = ''
    const label = subj ? `${subj.name}${cls ? ` — ${classLabel(cls)}` : ''}` : ''
    okMsg.value = `${t.value.okPrefix} ${label}`.trim()
    clearTimeout(okTimer)
    okTimer = setTimeout(() => (okMsg.value = ''), 4000)
  } catch (err) {
    apiError.value = err.message
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <BaseModal :open="open" :title="t.title" :subtitle="t.subtitle" @close="emit('close')">
    <!-- Backend / duplicate error -->
    <div v-if="apiError" class="form-error">
      <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="9" /><path d="M12 8v5M12 16h.01" /></svg>
      <span>{{ apiError }}</span>
    </div>

    <!-- Inline success (the modal stays open so several subjects can be assigned quickly) -->
    <Transition name="ok">
      <div v-if="okMsg" class="assign-ok">
        <svg viewBox="0 0 24 24"><path d="M20 6 9 17l-5-5" /></svg>
        <span>{{ okMsg }}</span>
      </div>
    </Transition>

    <!-- Step 1 — Level (segmented toggle) -->
    <div class="field">
      <label><span class="step-badge" :class="{ done: form.level }">1</span>{{ t.level }}</label>
      <div class="seg" role="group" :aria-label="t.level">
        <button
          type="button"
          class="seg-btn"
          :class="{ active: form.level === 'Secondary' }"
          @click="pickLevel('Secondary')"
        >
          <svg viewBox="0 0 24 24"><path d="M12 3 1 8l11 5 9-4.09" /><path d="M6 10.5V15c0 1.5 3 3 6 3s6-1.5 6-3v-4.5" /><path d="M21 8v5" /></svg>
          {{ t.secondary }}
        </button>
        <button
          type="button"
          class="seg-btn"
          :class="{ active: form.level === 'HighSchool' }"
          @click="pickLevel('HighSchool')"
        >
          <svg viewBox="0 0 24 24"><path d="M22 10 12 5 2 10l10 5 10-5Z" /><path d="M6 12v5c0 1 2.7 2.5 6 2.5s6-1.5 6-2.5v-5" /></svg>
          {{ t.highSchool }}
        </button>
      </div>
    </div>

    <!-- Step 2 — Teacher (filtered by level) -->
    <div class="field">
      <label><span class="step-badge" :class="{ done: form.teacherId }">2</span>{{ t.teacher }}</label>
      <select v-model="form.teacherId" :disabled="!form.level">
        <option value="" disabled>{{ form.level ? t.teacherPh : t.chooseLevelFirst }}</option>
        <option v-for="tt in levelTeachers" :key="tt.id" :value="tt.id">{{ tt.name }}</option>
      </select>
      <span v-if="form.level && !levelTeachers.length" class="field-hint">{{ t.noTeachers }}</span>
    </div>

    <!-- Step 3 — Year (filtered by level) -->
    <div class="field">
      <label><span class="step-badge" :class="{ done: form.year }">3</span>{{ t.year }}</label>
      <select v-model="form.year" :disabled="!form.teacherId">
        <option value="" disabled>{{ form.teacherId ? t.yearPh : t.chooseTeacherFirst }}</option>
        <option v-for="y in availableYears" :key="y" :value="String(y)">{{ yearLabel(y) }}</option>
      </select>
      <span v-if="form.teacherId && !availableYears.length" class="field-hint">{{ t.noYears }}</span>
    </div>

    <!-- Step 4 — Class / Section (filtered by level + year) -->
    <div class="field">
      <label><span class="step-badge" :class="{ done: form.classId }">4</span>{{ t.class }}</label>
      <select v-model="form.classId" :disabled="!form.year">
        <option value="" disabled>{{ form.year ? t.classPh : t.chooseYearFirst }}</option>
        <option v-for="c in levelYearClasses" :key="c.id" :value="String(c.id)">{{ classLabel(c) }}</option>
      </select>
    </div>

    <!-- Step 5 — Subject (belongs to the chosen class) -->
    <div class="field">
      <label><span class="step-badge" :class="{ done: form.subjectId }">5</span>{{ t.subject }}</label>
      <select v-model="form.subjectId" :disabled="!form.classId">
        <option value="" disabled>{{ form.classId ? t.subjectPh : t.chooseClassFirst }}</option>
        <option v-for="o in subjectOptions" :key="o.id" :value="String(o.id)" :disabled="o.assigned">
          {{ o.assigned ? `${o.name} — ${t.assignedTag}` : o.name }}
        </option>
      </select>
      <span v-if="form.classId && !subjectOptions.length" class="field-hint">{{ t.noSubjects }}</span>
    </div>

    <template #footer>
      <button type="button" class="btn ghost" @click="emit('close')" :disabled="submitting">{{ t.done }}</button>
      <button type="button" class="btn primary" @click="submit" :disabled="!canAssign || submitting">
        {{ submitting ? t.saving : t.save }}
      </button>
    </template>
  </BaseModal>
</template>

<style scoped>
/* Numbered step badge before each field label — gives the form a clear cascade feel. */
.step-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 20px;
  height: 20px;
  margin-inline-end: 0.5rem;
  border-radius: 50%;
  font-size: 0.72rem;
  font-weight: 800;
  color: #64748b;
  background: #eef1f7;
  transition: background 0.2s ease, color 0.2s ease;
}
/* Once a step is chosen its badge fills with the brand navy → a satisfying progression. */
.step-badge.done {
  color: #fff;
  background: linear-gradient(135deg, var(--ds-navy, #1e4c9a), var(--ds-navy-strong, #2f63ba));
}

/* Segmented level toggle (two mutually-exclusive buttons) */
.seg {
  display: flex;
  gap: 0.6rem;
}
.seg-btn {
  flex: 1;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 0.75rem 0.9rem;
  border: 1.5px solid #e6ebf4;
  border-radius: 14px;
  background: #f4f7fc;
  font-family: inherit;
  font-size: 0.9rem;
  font-weight: 700;
  color: #475569;
  cursor: pointer;
  transition: border-color 0.18s ease, background 0.18s ease, color 0.18s ease, box-shadow 0.18s ease, transform 0.15s ease;
}
.seg-btn svg {
  width: 18px;
  height: 18px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.8;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.seg-btn:hover {
  border-color: #b9ccec;
  color: var(--ds-navy, #1e4c9a);
}
.seg-btn.active {
  color: #fff;
  background: linear-gradient(135deg, var(--ds-navy, #1e4c9a), var(--ds-navy-strong, #2f63ba));
  border-color: transparent;
  box-shadow: 0 8px 18px rgba(30, 76, 154, 0.28);
}

/* Small muted helper line under a field (e.g. "No teachers for this level yet."). */
.field-hint {
  font-size: 0.78rem;
  color: #8a94a6;
}

/* A disabled step reads clearly as not-yet-available. */
select:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}

/* Inline success note (green) — confirms an assign while the modal stays open. */
.assign-ok {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  padding: 0.75rem 0.9rem;
  border-radius: 12px;
  background: #f0fdf4;
  border: 1px solid #bbf7d0;
  color: #15803d;
  font-size: 0.85rem;
  font-weight: 600;
  line-height: 1.4;
}
.assign-ok svg {
  width: 18px;
  height: 18px;
  flex-shrink: 0;
  fill: none;
  stroke: currentColor;
  stroke-width: 2.2;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.ok-enter-active,
.ok-leave-active {
  transition: opacity 0.25s ease, transform 0.25s ease;
}
.ok-enter-from,
.ok-leave-to {
  opacity: 0;
  transform: translateY(-6px);
}
</style>
