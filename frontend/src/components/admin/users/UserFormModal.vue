<script setup>
import { computed, reactive, ref, watch } from 'vue'
import { useLanguageStore } from '../../../stores/language'
import { useAuthStore } from '../../../stores/auth'
import BaseModal from '../../common/BaseModal.vue'
import { createUser, updateUser } from '../../../api/users'
import { icons } from '../icons'

const props = defineProps({
  open: { type: Boolean, default: false },
  // The user being edited, or null when adding a new user.
  user: { type: Object, default: null },
  // Available classes (Student → Class dropdown).
  classes: { type: Array, default: () => [] },
  // Existing parent accounts (Student → Parent dropdown).
  parents: { type: Array, default: () => [] }
})
const emit = defineEmits(['close', 'saved'])

const language = useLanguageStore()
const auth = useAuthStore()

// Only this account is allowed to create Admin users.
// NOTE: frontend display only. The backend MUST also enforce that only
// admin@almanahilschool.com can create Admin accounts — never trust the
// frontend for security.
const MAIN_ADMIN_EMAIL = 'admin@almanahilschool.com'

// Teacher & Admin accounts must use a real school mailbox on this domain.
// Frontend check is for instant UX only — the backend enforces the real rule.
const SCHOOL_EMAIL_DOMAIN = '@almanahilschool.com'

// ---- Bilingual copy (per-component i18n pattern) ----
const content = {
  en: {
    addTitle: 'Add User',
    editTitle: 'Edit User',
    addSub: 'Create a new teacher, student, or parent account',
    editSub: 'Update this user’s details',
    // Section headings
    personalInfo: 'Personal Information',
    studentDetails: 'Student Details',
    parentDetails: 'Parent Details',
    teacherDetails: 'Teacher Details',
    teacherLevel: 'Teacher Level',
    selectLevel: 'Select a level',
    // Personal fields
    firstName: 'First Name',
    middleName: 'Middle Name',
    optional: 'optional',
    lastName: 'Last Name',
    gender: 'Gender',
    selectGender: 'Select gender',
    male: 'Male',
    female: 'Female',
    dob: 'Date of Birth',
    email: 'Email',
    role: 'Role',
    accountRole: 'Account Role',
    selectRole: 'Select a role',
    firstNamePh: 'e.g. Ahmed',
    middleNamePh: 'e.g. Ali',
    lastNamePh: 'e.g. Al-Mansouri',
    emailPh: 'name@almanahilschool.com',
    emailHint: 'Use the school domain: name@almanahilschool.com',
    // Role-specific
    classLabel: 'Class',
    selectClass: 'Select a class',
    parentLabel: 'Parent',
    selectParent: 'Select a parent',
    contact: 'Parent / Guardian Contact Number',
    contactPh: 'e.g. +218 91 234 5678',
    phone: 'Phone Number',
    phonePh: 'e.g. +218 91 234 5678',
    noParents: 'Please create a Parent account first, then link this student to them.',
    note: 'A temporary password will be auto-generated and emailed to the user.',
    cancel: 'Cancel',
    save: 'Save User',
    saving: 'Saving…',
    roles: { Teacher: 'Teacher', Student: 'Student', Parent: 'Parent', Admin: 'Admin' },
    err: {
      firstName: 'Please enter the first name.',
      middleName: 'Please enter the middle name.',
      lastName: 'Please enter the last name.',
      gender: 'Please select a gender.',
      dob: 'Please enter the date of birth.',
      dobFuture: 'Date of birth cannot be in the future.',
      email: 'Please enter a valid email address.',
      emailDomain: 'Teacher and admin emails must end with @almanahilschool.com.',
      role: 'Please select a role.',
      class: 'Please select a class for the student.',
      parent: 'Please link the student to a parent.',
      contact: 'Please enter the parent / guardian contact number.',
      phone: 'Please enter the phone number.',
      teacherLevel: 'Please choose the teacher’s level.'
    }
  },
  ar: {
    addTitle: 'إضافة مستخدم',
    editTitle: 'تعديل المستخدم',
    addSub: 'إنشاء حساب معلم أو طالب أو ولي أمر جديد',
    editSub: 'تحديث بيانات هذا المستخدم',
    personalInfo: 'المعلومات الشخصية',
    studentDetails: 'بيانات الطالب',
    parentDetails: 'بيانات ولي الأمر',
    teacherDetails: 'بيانات المعلم',
    teacherLevel: 'مرحلة التدريس',
    selectLevel: 'اختر المرحلة',
    firstName: 'الاسم الأول',
    middleName: 'الاسم الأوسط',
    optional: 'اختياري',
    lastName: 'اسم العائلة',
    gender: 'الجنس',
    selectGender: 'اختر الجنس',
    male: 'ذكر',
    female: 'أنثى',
    dob: 'تاريخ الميلاد',
    email: 'البريد الإلكتروني',
    role: 'الدور',
    accountRole: 'دور الحساب',
    selectRole: 'اختر الدور',
    firstNamePh: 'مثال: أحمد',
    middleNamePh: 'مثال: علي',
    lastNamePh: 'مثال: المنصوري',
    emailPh: 'name@almanahilschool.com',
    emailHint: 'استخدم نطاق المدرسة: name@almanahilschool.com',
    classLabel: 'الصف',
    selectClass: 'اختر الصف',
    parentLabel: 'ولي الأمر',
    selectParent: 'اختر ولي الأمر',
    contact: 'رقم هاتف ولي الأمر / الوصي',
    contactPh: 'مثال: 5678 234 91 218+',
    phone: 'رقم الهاتف',
    phonePh: 'مثال: 5678 234 91 218+',
    noParents: 'يرجى إنشاء حساب ولي أمر أولاً، ثم ربط هذا الطالب به.',
    note: 'سيتم إنشاء كلمة مرور مؤقتة تلقائياً وإرسالها إلى المستخدم عبر البريد الإلكتروني.',
    cancel: 'إلغاء',
    save: 'حفظ المستخدم',
    saving: 'جارٍ الحفظ…',
    roles: { Teacher: 'معلم', Student: 'طالب', Parent: 'ولي أمر', Admin: 'مدير' },
    err: {
      firstName: 'يرجى إدخال الاسم الأول.',
      middleName: 'يرجى إدخال الاسم الأوسط.',
      lastName: 'يرجى إدخال اسم العائلة.',
      gender: 'يرجى اختيار الجنس.',
      dob: 'يرجى إدخال تاريخ الميلاد.',
      dobFuture: 'لا يمكن أن يكون تاريخ الميلاد في المستقبل.',
      email: 'يرجى إدخال بريد إلكتروني صحيح.',
      emailDomain: 'يجب أن ينتهي بريد المعلم والمدير بـ @almanahilschool.com.',
      role: 'يرجى اختيار الدور.',
      class: 'يرجى اختيار صف للطالب.',
      parent: 'يرجى ربط الطالب بولي أمر.',
      contact: 'يرجى إدخال رقم هاتف ولي الأمر / الوصي.',
      phone: 'يرجى إدخال رقم الهاتف.',
      teacherLevel: 'يرجى اختيار مرحلة تدريس المعلم.'
    }
  }
}
const t = computed(() => content[language.lang])

// Both languages' domain-error messages, so the live watcher can clear ONLY the
// domain error (and never a format error) even after a language switch.
const DOMAIN_ERR_MSGS = [content.en.err.emailDomain, content.ar.err.emailDomain]

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[a-zA-Z]{2,}$/

// The Role dropdown normally offers Teacher/Student/Parent. Only the main admin
// account may also create Admin users (backend enforces the real rule).
const roleOptions = computed(() => {
  // Same order as the summary strip / filter tabs: Student, Parent, Teacher (+ Admin).
  const base = ['Student', 'Parent', 'Teacher']
  if ((auth.user?.email || '').toLowerCase() === MAIN_ADMIN_EMAIL) base.push('Admin')
  return base
})

// Today's date (YYYY-MM-DD) — used to cap the DOB picker and reject future dates.
const today = computed(() => new Date().toISOString().slice(0, 10))

// ---- Form state ----
const form = reactive({
  firstName: '',
  middleName: '',
  lastName: '',
  gender: '',
  dob: '',
  email: '',
  role: '',
  // Student-only
  classId: '',
  parentId: '',
  contactNumber: '',
  // Parent-only
  phone: '',
  // Teacher-only
  teacherLevel: ''
})
const errors = reactive({
  firstName: '',
  middleName: '',
  lastName: '',
  gender: '',
  dob: '',
  email: '',
  role: '',
  classId: '',
  parentId: '',
  contactNumber: '',
  phone: '',
  teacherLevel: ''
})

const isEdit = computed(() => !!props.user)

// Submission state for the real API call (POST/PUT /api/users).
const submitting = ref(false)
const apiError = ref('') // backend message shown inline (duplicate email, domain, parent not found, …)
// Student / Parent / Teacher each have an extra section; Admin does not.
const showRoleSection = computed(() => form.role === 'Student' || form.role === 'Parent' || form.role === 'Teacher')
// Teacher & Admin must use the school email domain; Student & Parent may use any email.
const requiresSchoolDomain = computed(() => form.role === 'Teacher' || form.role === 'Admin')
// Students & parents must provide a middle name (their full name uses it).
const middleRequired = computed(() => form.role === 'Student' || form.role === 'Parent')

// Teacher-level dropdown options (primary language first — mirrors the class levels).
const teacherLevelOptions = computed(() =>
  language.lang === 'ar'
    ? [
        { value: 'Secondary', label: 'إعدادي (Secondary)' },
        { value: 'HighSchool', label: 'ثانوي (High School)' }
      ]
    : [
        { value: 'Secondary', label: 'Secondary (إعدادي)' },
        { value: 'HighSchool', label: 'High School (ثانوي)' }
      ]
)

// Icons shown on the role selector pills.
const roleIcons = {
  Teacher: icons.book,
  Student: icons.student,
  Parent: icons.user,
  Admin: icons.settings
}

// Each role paints the form its own colour (same palette as the users list: student
// amber, parent slate, teacher green, admin indigo). Exposed as CSS vars on the form,
// and inline on the footer Save button (which lives outside the form and can't inherit).
const ROLE_THEME = {
  Student: { main: '#d97706', strong: '#b45309', soft: '#fef3c7', ring: 'rgba(217, 119, 6, 0.14)', shadow: 'rgba(217, 119, 6, 0.22)' },
  Parent: { main: '#64748b', strong: '#475569', soft: '#eef1f6', ring: 'rgba(100, 116, 139, 0.16)', shadow: 'rgba(100, 116, 139, 0.22)' },
  Teacher: { main: '#16a34a', strong: '#15803d', soft: '#dcfce7', ring: 'rgba(22, 163, 74, 0.14)', shadow: 'rgba(22, 163, 74, 0.22)' },
  Admin: { main: '#4f46e5', strong: '#4338ca', soft: '#e0e7ff', ring: 'rgba(79, 70, 229, 0.14)', shadow: 'rgba(79, 70, 229, 0.22)' }
}
const DEFAULT_THEME = { main: '#1e4c9a', strong: '#2f63ba', soft: '#e8eefb', ring: 'rgba(30, 76, 154, 0.12)', shadow: 'rgba(30, 76, 154, 0.2)' }

const themeStyle = computed(() => {
  const th = ROLE_THEME[form.role] || DEFAULT_THEME
  return {
    '--role-main': th.main,
    '--role-strong': th.strong,
    '--role-soft': th.soft,
    '--role-ring': th.ring,
    '--role-shadow': th.shadow
  }
})
const saveBtnStyle = computed(() => {
  const th = ROLE_THEME[form.role] || DEFAULT_THEME
  return {
    background: `linear-gradient(135deg, ${th.main}, ${th.strong})`,
    boxShadow: `0 8px 18px ${th.shadow}`
  }
})

// Split a legacy single "name" into parts (fallback when a row has no name parts).
function splitName(u) {
  const raw = (u?.name || '').trim()
  if (!raw) return { first: '', middle: '', last: '' }
  const bits = raw.split(/\s+/)
  if (bits.length === 1) return { first: bits[0], middle: '', last: '' }
  return { first: bits[0], middle: bits.slice(1, -1).join(' '), last: bits[bits.length - 1] }
}

function resetForm() {
  form.firstName = ''
  form.middleName = ''
  form.lastName = ''
  form.gender = ''
  form.dob = ''
  form.email = ''
  form.role = ''
  form.classId = ''
  form.parentId = ''
  form.contactNumber = ''
  form.phone = ''
  form.teacherLevel = ''
}

// Reset (or hydrate) the form whenever the modal opens.
watch(
  () => props.open,
  (open) => {
    if (!open) return
    clearErrors()
    apiError.value = ''
    submitting.value = false
    if (props.user) {
      const u = props.user
      const parts = splitName(u)
      form.firstName = u.firstName ?? parts.first
      form.middleName = u.middleName ?? parts.middle
      form.lastName = u.lastName ?? parts.last
      form.gender = u.gender ?? ''
      form.dob = u.dob ?? ''
      form.email = u.email ?? ''
      form.role = u.role ?? ''
      form.classId = u.classId ?? ''
      form.parentId = u.parentId ?? ''
      form.contactNumber = u.contactNumber ?? ''
      form.phone = u.phone ?? ''
      form.teacherLevel = u.teacherLevel ?? ''
    } else {
      resetForm()
    }
  }
)

// When the role changes, drop any conditional value/error that no longer applies
// so a hidden field can never carry a stale selection into the saved record.
watch(
  () => form.role,
  (role) => {
    if (role !== 'Student') {
      form.classId = ''
      form.parentId = ''
      form.contactNumber = ''
      errors.classId = ''
      errors.parentId = ''
      errors.contactNumber = ''
    }
    if (role !== 'Parent') {
      form.phone = ''
      errors.phone = ''
    }
    if (role !== 'Teacher') {
      form.teacherLevel = ''
      errors.teacherLevel = ''
    }
  }
)

// Live email-domain feedback for Teacher/Admin. Re-checks when the role or the email
// changes (e.g. switching Student → Teacher with a personal address already typed).
// Only manages the domain rule — the format check still runs on submit.
watch([() => form.role, () => form.email], () => {
  const email = form.email.trim()
  if (requiresSchoolDomain.value && email && EMAIL_PATTERN.test(email)
      && !email.toLowerCase().endsWith(SCHOOL_EMAIL_DOMAIN)) {
    errors.email = t.value.err.emailDomain
  } else if (DOMAIN_ERR_MSGS.includes(errors.email)) {
    // Clear only the domain error — never wipe a format error set on submit.
    errors.email = ''
  }
})

// For a student, the stored phone IS the parent's contact — prefill it from the
// chosen parent when the admin picks one (they can still edit it afterwards).
// Uses @change (user action only) so hydrating an existing student never clobbers
// their saved contact number.
function onParentChange() {
  const parent = props.parents.find((p) => p.id === Number(form.parentId))
  if (parent?.phone) form.contactNumber = parent.phone
}

function clearErrors() {
  Object.keys(errors).forEach((k) => (errors[k] = ''))
}

function validate() {
  clearErrors()
  let ok = true
  // Always required
  if (!form.firstName.trim()) { errors.firstName = t.value.err.firstName; ok = false }
  if (middleRequired.value && !form.middleName.trim()) { errors.middleName = t.value.err.middleName; ok = false }
  if (!form.lastName.trim()) { errors.lastName = t.value.err.lastName; ok = false }
  if (!form.gender) { errors.gender = t.value.err.gender; ok = false }
  if (!form.dob) {
    errors.dob = t.value.err.dob
    ok = false
  } else if (form.dob > today.value) {
    errors.dob = t.value.err.dobFuture
    ok = false
  }
  const email = form.email.trim()
  if (!EMAIL_PATTERN.test(email)) {
    errors.email = t.value.err.email; ok = false
  } else if (requiresSchoolDomain.value && !email.toLowerCase().endsWith(SCHOOL_EMAIL_DOMAIN)) {
    // Teacher/Admin school-domain rule (the backend enforces the real check).
    errors.email = t.value.err.emailDomain; ok = false
  }
  if (!form.role) { errors.role = t.value.err.role; ok = false }

  // Role-specific
  if (form.role === 'Student') {
    if (!form.classId) { errors.classId = t.value.err.class; ok = false }
    // Parent linking happens at creation only (the API doesn't re-link on edit).
    if (!isEdit.value) {
      if (!props.parents.length) ok = false // blocked; friendly note shown in the UI
      if (!form.parentId) { errors.parentId = t.value.err.parent; ok = false }
    }
    if (!form.contactNumber.trim()) { errors.contactNumber = t.value.err.contact; ok = false }
  }
  if (form.role === 'Parent') {
    if (!form.phone.trim()) { errors.phone = t.value.err.phone; ok = false }
  }
  if (form.role === 'Teacher') {
    if (!form.teacherLevel) { errors.teacherLevel = t.value.err.teacherLevel; ok = false }
  }
  return ok
}

async function submit() {
  apiError.value = ''
  if (!validate()) return

  const isStudent = form.role === 'Student'
  const isParent = form.role === 'Parent'
  const isTeacher = form.role === 'Teacher'

  // Fields shared by create + update (map to Create/UpdateUserDto). Non-applicable
  // role fields are nulled so no stale data is ever sent.
  const base = {
    firstName: form.firstName.trim(),
    middleName: form.middleName.trim() || null,
    lastName: form.lastName.trim(),
    email: form.email.trim(),
    gender: form.gender,
    dateOfBirth: form.dob, // 'yyyy-mm-dd' → DateOnly
    // phoneNumber holds the parent's phone (Parent) or the guardian contact (Student).
    phoneNumber: isParent ? form.phone.trim() : isStudent ? form.contactNumber.trim() : null,
    classId: isStudent ? Number(form.classId) : null,
    teacherLevel: isTeacher ? form.teacherLevel : null
  }

  submitting.value = true
  try {
    let message
    let temporaryPassword = null
    if (isEdit.value) {
      // Update personal info (+ class/contact). Role & parent link stay unchanged.
      const res = await updateUser(props.user.id, base)
      message = res.message
    } else {
      // Create: add the role and (for students) the parent to link.
      const res = await createUser({
        ...base,
        role: form.role,
        parentId: isStudent ? Number(form.parentId) : null
      })
      message = res.message
      // Present ONLY for the main administrator (the backend gates this) so it can be
      // shown + copied. Other admins get null and only the "emailed" message.
      temporaryPassword = res.data?.temporaryPassword ?? null
    }
    emit('saved', { message, temporaryPassword })
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
    size="wide"
    @close="emit('close')"
  >
    <form id="user-form" class="uf-form" :style="themeStyle" @submit.prevent="submit" novalidate>
      <!-- Backend error (duplicate email, domain rule, "Selected parent not found", …) -->
      <div v-if="apiError" class="form-error">
        <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="9" /><path d="M12 8v5M12 16h.01" /></svg>
        <span>{{ apiError }}</span>
      </div>

      <!-- ============ SECTION 1: Account Role ============ -->
      <section class="form-section">
        <h4 class="section-title">
          <span class="sec-ic"><svg viewBox="0 0 24 24"><path d="M12 3 4 6v5c0 5 3.4 8 8 9 4.6-1 8-4 8-9V6l-8-3z" /><path d="m9 12 2 2 4-4" /></svg></span>
          {{ t.accountRole }}
        </h4>

        <!-- Role selector (prominent, full width) -->
        <div class="field">
          <div class="role-pills" :class="{ 'is-invalid': errors.role }">
            <button
              v-for="r in roleOptions"
              :key="r"
              type="button"
              class="role-pill"
              :class="[`rp-${r.toLowerCase()}`, { active: form.role === r }]"
              @click="form.role = r"
            >
              <span class="role-pill-ic"><svg viewBox="0 0 24 24" v-html="roleIcons[r]"></svg></span>
              {{ t.roles[r] }}
            </button>
          </div>
          <span v-if="errors.role" class="err">{{ errors.role }}</span>
        </div>
      </section>

      <!-- ============ SECTION 2: Personal Information ============ -->
      <section class="form-section">
        <h4 class="section-title">
          <span class="sec-ic"><svg viewBox="0 0 24 24"><circle cx="12" cy="8" r="4" /><path d="M4 21c0-4 4-6.5 8-6.5s8 2.5 8 6.5" /></svg></span>
          {{ t.personalInfo }}
        </h4>

        <div class="pi-grid">
          <div class="field">
            <label for="uf-first">{{ t.firstName }}</label>
            <div class="in">
              <span class="in-ic"><svg viewBox="0 0 24 24"><circle cx="12" cy="8" r="4" /><path d="M4 21c0-4 4-6.5 8-6.5s8 2.5 8 6.5" /></svg></span>
              <input id="uf-first" v-model="form.firstName" type="text" :placeholder="t.firstNamePh" :class="{ invalid: errors.firstName }" />
            </div>
            <span v-if="errors.firstName" class="err">{{ errors.firstName }}</span>
          </div>

          <div class="field">
            <label for="uf-middle">
              {{ t.middleName }}
              <span v-if="!middleRequired" class="opt">({{ t.optional }})</span>
            </label>
            <div class="in">
              <span class="in-ic"><svg viewBox="0 0 24 24"><circle cx="12" cy="8" r="4" /><path d="M4 21c0-4 4-6.5 8-6.5s8 2.5 8 6.5" /></svg></span>
              <input id="uf-middle" v-model="form.middleName" type="text" :placeholder="t.middleNamePh" :class="{ invalid: errors.middleName }" />
            </div>
            <span v-if="errors.middleName" class="err">{{ errors.middleName }}</span>
          </div>

          <div class="field">
            <label for="uf-last">{{ t.lastName }}</label>
            <div class="in">
              <span class="in-ic"><svg viewBox="0 0 24 24"><circle cx="12" cy="8" r="4" /><path d="M4 21c0-4 4-6.5 8-6.5s8 2.5 8 6.5" /></svg></span>
              <input id="uf-last" v-model="form.lastName" type="text" :placeholder="t.lastNamePh" :class="{ invalid: errors.lastName }" />
            </div>
            <span v-if="errors.lastName" class="err">{{ errors.lastName }}</span>
          </div>

          <div class="field">
            <label for="uf-gender">{{ t.gender }}</label>
            <div class="in">
              <span class="in-ic"><svg viewBox="0 0 24 24"><circle cx="12" cy="8" r="4" /><path d="M4 21c0-4 4-6.5 8-6.5s8 2.5 8 6.5" /></svg></span>
              <select id="uf-gender" v-model="form.gender" :class="{ invalid: errors.gender }">
                <option value="" disabled>{{ t.selectGender }}</option>
                <option value="Male">{{ t.male }}</option>
                <option value="Female">{{ t.female }}</option>
              </select>
            </div>
            <span v-if="errors.gender" class="err">{{ errors.gender }}</span>
          </div>

          <div class="field">
            <label for="uf-dob">{{ t.dob }}</label>
            <div class="in">
              <span class="in-ic"><svg viewBox="0 0 24 24"><rect x="3" y="4" width="18" height="17" rx="2" /><path d="M3 9h18M8 3v4M16 3v4" /></svg></span>
              <input id="uf-dob" v-model="form.dob" type="date" :max="today" :class="{ invalid: errors.dob }" />
            </div>
            <span v-if="errors.dob" class="err">{{ errors.dob }}</span>
          </div>

          <div class="field">
            <label for="uf-email">{{ t.email }}</label>
            <div class="in">
              <span class="in-ic"><svg viewBox="0 0 24 24"><rect x="3" y="5" width="18" height="14" rx="2" /><path d="m3.5 6.5 8.5 6 8.5-6" /></svg></span>
              <input id="uf-email" v-model="form.email" type="email" :placeholder="t.emailPh" :class="{ invalid: errors.email }" />
            </div>
            <span v-if="errors.email" class="err">{{ errors.email }}</span>
            <span v-else-if="requiresSchoolDomain" class="hint">{{ t.emailHint }}</span>
          </div>
        </div>
      </section>

      <!-- ============ SECTION 3: Role-specific ============ -->
      <section v-if="showRoleSection" class="form-section">
        <!-- Student -->
        <template v-if="form.role === 'Student'">
          <h4 class="section-title">
            <span class="sec-ic"><svg viewBox="0 0 24 24"><path d="M12 3 1 8l11 5 9-4.09" /><path d="M6 10.5V15c0 1.5 3 3 6 3s6-1.5 6-3v-4.5" /><path d="M21 8v5" /></svg></span>
            {{ t.studentDetails }}
          </h4>

          <!-- No parents yet → friendly note + save is blocked (creation only) -->
          <div v-if="!isEdit && !parents.length" class="parent-warn">
            <svg viewBox="0 0 24 24"><path d="M12 9v4m0 4h.01M10.3 3.9 1.8 18a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0Z" /></svg>
            <span>{{ t.noParents }}</span>
          </div>

          <div v-else class="pi-grid">
            <div class="field">
              <label for="uf-class">{{ t.classLabel }}</label>
              <div class="in">
                <span class="in-ic"><svg viewBox="0 0 24 24"><path d="M12 3 1 8l11 5 9-4.09" /><path d="M6 10.5V15c0 1.5 3 3 6 3s6-1.5 6-3v-4.5" /></svg></span>
                <select id="uf-class" v-model="form.classId" :class="{ invalid: errors.classId }">
                  <option value="" disabled>{{ t.selectClass }}</option>
                  <option v-for="c in classes" :key="c.id" :value="c.id">{{ c.displayName || c.name }}</option>
                </select>
              </div>
              <span v-if="errors.classId" class="err">{{ errors.classId }}</span>
            </div>

            <div v-if="!isEdit" class="field">
              <label for="uf-parent">{{ t.parentLabel }}</label>
              <div class="in">
                <span class="in-ic"><svg viewBox="0 0 24 24"><circle cx="9" cy="8" r="3.2" /><path d="M3.5 20a5.5 5.5 0 0 1 11 0" /><path d="M16 8.6a3 3 0 0 1 0 5.8M18.5 20a5 5 0 0 0-3-4.6" /></svg></span>
                <select id="uf-parent" v-model="form.parentId" @change="onParentChange" :class="{ invalid: errors.parentId }">
                  <option value="" disabled>{{ t.selectParent }}</option>
                  <option v-for="p in parents" :key="p.id" :value="p.id">{{ p.name }}</option>
                </select>
              </div>
              <span v-if="errors.parentId" class="err">{{ errors.parentId }}</span>
            </div>

            <div class="field span-2">
              <label for="uf-contact">{{ t.contact }}</label>
              <div class="in">
                <span class="in-ic"><svg viewBox="0 0 24 24"><path d="M22 16.9v3a2 2 0 0 1-2.2 2 19.8 19.8 0 0 1-8.6-3.1 19.5 19.5 0 0 1-6-6 19.8 19.8 0 0 1-3.1-8.6A2 2 0 0 1 4.1 2h3a2 2 0 0 1 2 1.7c.1.9.4 1.8.7 2.7a2 2 0 0 1-.5 2.1L8.1 9.8a16 16 0 0 0 6 6l1.3-1.3a2 2 0 0 1 2.1-.5c.9.3 1.8.6 2.7.7a2 2 0 0 1 1.7 2z" /></svg></span>
                <input id="uf-contact" v-model="form.contactNumber" type="tel" :placeholder="t.contactPh" :class="{ invalid: errors.contactNumber }" />
              </div>
              <span v-if="errors.contactNumber" class="err">{{ errors.contactNumber }}</span>
            </div>
          </div>
        </template>

        <!-- Parent -->
        <template v-else-if="form.role === 'Parent'">
          <h4 class="section-title">
            <span class="sec-ic"><svg viewBox="0 0 24 24"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" /><circle cx="9" cy="7" r="4" /><path d="M23 21v-2a4 4 0 0 0-3-3.87" /><path d="M16 3.13a4 4 0 0 1 0 7.75" /></svg></span>
            {{ t.parentDetails }}
          </h4>

          <div class="pi-grid">
            <div class="field span-2">
              <label for="uf-phone">{{ t.phone }}</label>
              <div class="in">
                <span class="in-ic"><svg viewBox="0 0 24 24"><path d="M22 16.9v3a2 2 0 0 1-2.2 2 19.8 19.8 0 0 1-8.6-3.1 19.5 19.5 0 0 1-6-6 19.8 19.8 0 0 1-3.1-8.6A2 2 0 0 1 4.1 2h3a2 2 0 0 1 2 1.7c.1.9.4 1.8.7 2.7a2 2 0 0 1-.5 2.1L8.1 9.8a16 16 0 0 0 6 6l1.3-1.3a2 2 0 0 1 2.1-.5c.9.3 1.8.6 2.7.7a2 2 0 0 1 1.7 2z" /></svg></span>
                <input id="uf-phone" v-model="form.phone" type="tel" :placeholder="t.phonePh" :class="{ invalid: errors.phone }" />
              </div>
              <span v-if="errors.phone" class="err">{{ errors.phone }}</span>
            </div>
          </div>
        </template>

        <!-- Teacher -->
        <template v-else-if="form.role === 'Teacher'">
          <h4 class="section-title">
            <span class="sec-ic"><svg viewBox="0 0 24 24"><path d="M12 3 1 8l11 5 9-4.09" /><path d="M6 10.5V15c0 1.5 3 3 6 3s6-1.5 6-3v-4.5" /><path d="M21 8v5" /></svg></span>
            {{ t.teacherDetails }}
          </h4>

          <div class="pi-grid">
            <div class="field span-2">
              <label for="uf-tlevel">{{ t.teacherLevel }}</label>
              <div class="in">
                <span class="in-ic"><svg viewBox="0 0 24 24"><path d="m12 3 9 5-9 5-9-5 9-5z" /><path d="m3 12 9 5 9-5" /></svg></span>
                <select id="uf-tlevel" v-model="form.teacherLevel" :class="{ invalid: errors.teacherLevel }">
                  <option value="" disabled>{{ t.selectLevel }}</option>
                  <option v-for="o in teacherLevelOptions" :key="o.value" :value="o.value">{{ o.label }}</option>
                </select>
              </div>
              <span v-if="errors.teacherLevel" class="err">{{ errors.teacherLevel }}</span>
            </div>
          </div>
        </template>
      </section>

      <!-- Auto-password note (no password field anywhere) -->
      <div class="note">
        <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="9" /><path d="M12 8h.01M11 12h1v4h1" /></svg>
        <span>{{ t.note }}</span>
      </div>
    </form>

    <template #footer>
      <button type="button" class="btn ghost" @click="emit('close')" :disabled="submitting">{{ t.cancel }}</button>
      <button type="submit" form="user-form" class="btn primary" :style="saveBtnStyle" :disabled="submitting">
        {{ submitting ? t.saving : t.save }}
      </button>
    </template>
  </BaseModal>
</template>

<style scoped>
/* The form lives inside BaseModal's body; field/label/input styling is provided
   by BaseModal (:deep) so it matches every other admin form. Here we add the
   card-style sections, the icon-badge headings, the role pills, and the grid. */
.uf-form {
  display: flex;
  flex-direction: column;
  gap: 1.15rem;
}

/* Each section is a soft card for clearer grouping */
.form-section {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  padding: 1.15rem 1.2rem 1.3rem;
  background: linear-gradient(180deg, #fcfdff, #f8fafd);
  border: 1px solid #e9eef6;
  border-radius: 16px;
  box-shadow: 0 6px 18px rgba(30, 41, 59, 0.05);
}

/* Section header: rounded icon badge + title */
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
  color: var(--role-main, #1e4c9a);
  background: var(--role-soft, #e8eefb);
  transition: color 0.2s ease, background 0.2s ease;
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

/* Role selector pills (replaces the plain dropdown) */
.role-pills {
  display: flex;
  flex-wrap: wrap;
  gap: 0.55rem;
}
.role-pill {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 0.95rem 0.5rem 0.55rem;
  border: 1.5px solid #e4e9f2;
  border-radius: 12px;
  background: #fff;
  font-family: inherit;
  font-size: 0.88rem;
  font-weight: 700;
  color: #64748b;
  cursor: pointer;
  transition: border-color 0.15s ease, background 0.15s ease, color 0.15s ease, transform 0.15s ease, box-shadow 0.15s ease;
}
.role-pill:hover {
  border-color: #c8d6ee;
  color: #0f2444;
  transform: translateY(-1px);
}
.role-pill-ic {
  width: 27px;
  height: 27px;
  flex-shrink: 0;
  border-radius: 8px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  background: #f1f5fb;
  color: #94a3b8;
  transition: background 0.15s ease, color 0.15s ease;
}
.role-pill-ic svg {
  width: 15px;
  height: 15px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.9;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.role-pill.active {
  border-color: var(--role-main, #1e4c9a);
  background: linear-gradient(135deg, var(--role-soft, #e8eefb), #fff);
  color: var(--role-strong, #1e4c9a);
  box-shadow: 0 6px 16px var(--role-shadow, rgba(30, 76, 154, 0.16));
}
.role-pill.active .role-pill-ic {
  background: var(--role-main, #1e4c9a);
  color: #fff;
}
/* Each pill carries its own role colour on the icon badge, even when not selected */
.rp-student .role-pill-ic { background: #fef3c7; color: #d97706; }
.rp-parent .role-pill-ic { background: #e5e9f0; color: #64748b; }
.rp-teacher .role-pill-ic { background: #dcfce7; color: #16a34a; }
.rp-admin .role-pill-ic { background: #e0e7ff; color: #4f46e5; }
.role-pills.is-invalid .role-pill {
  border-color: #f4c9c9;
}

/* Two-column field grid; full-width fields use .span-2.
   minmax(0, 1fr) lets columns shrink below their content so nothing overflows. */
.pi-grid {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
  gap: 1rem 1.1rem;
}
.span-2 {
  grid-column: 1 / -1;
}
.opt {
  font-weight: 600;
  color: #94a3b8;
}
/* Small helper hint under the email field (school-domain reminder for Teacher/Admin) */
.hint {
  font-size: 0.76rem;
  font-weight: 600;
  color: #8a94a6;
}

/* ---- Field polish (scoped to this form; BaseModal's shared style is untouched) ---- */
/* Leading icon sitting inside each field */
.uf-form :deep(.in) {
  position: relative;
  min-width: 0;
}
/* Fields never exceed their grid column — this is what removes the horizontal scroll */
.uf-form :deep(.field) {
  min-width: 0;
}
.uf-form :deep(.in-ic) {
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
.uf-form :deep(.in-ic svg) {
  width: 100%;
  height: 100%;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.9;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.uf-form :deep(.in:focus-within .in-ic) {
  color: var(--role-main, #1e4c9a);
}
/* Crisper white inputs + room for the icon. The doubled .uf-form.uf-form raises
   specificity just above BaseModal's shared field rules so only THIS form changes. */
.uf-form.uf-form :deep(.field label) {
  font-size: 0.82rem;
  font-weight: 600;
  color: #47536a;
}
.uf-form.uf-form :deep(.field input),
.uf-form.uf-form :deep(.field select),
.uf-form.uf-form :deep(.field textarea) {
  box-sizing: border-box;
  min-width: 0;
  max-width: 100%;
  background: #fff;
  border-color: #e3e9f3;
  border-radius: 12px;
  box-shadow: 0 1px 2px rgba(15, 23, 42, 0.04);
}
.uf-form.uf-form :deep(.in input),
.uf-form.uf-form :deep(.in select) {
  padding-inline-start: 2.65rem;
}
.uf-form.uf-form :deep(.field input:focus),
.uf-form.uf-form :deep(.field select:focus),
.uf-form.uf-form :deep(.field textarea:focus) {
  background: #fff;
  border-color: var(--role-main, #1e4c9a);
  box-shadow: 0 0 0 3px var(--role-ring, rgba(30, 76, 154, 0.14));
}
.uf-form.uf-form :deep(.field input.invalid),
.uf-form.uf-form :deep(.field select.invalid),
.uf-form.uf-form :deep(.field textarea.invalid) {
  border-color: #ef4444;
  background: #fef2f2;
}

/* Backend/API error banner at the top of the form */
.form-error {
  display: flex;
  align-items: flex-start;
  gap: 0.6rem;
  padding: 0.85rem 0.95rem;
  border-radius: 12px;
  background: #fef2f2;
  border: 1px solid #fecaca;
  color: #b91c1c;
  font-size: 0.85rem;
  font-weight: 600;
  line-height: 1.45;
}
.form-error svg {
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

/* Warning shown when a Student is being created but no parents exist yet */
.parent-warn {
  display: flex;
  align-items: flex-start;
  gap: 0.6rem;
  padding: 0.85rem 0.95rem;
  border-radius: 12px;
  background: #fff7ed;
  border: 1px solid #fed7aa;
  color: #b45309;
  font-size: 0.83rem;
  font-weight: 600;
  line-height: 1.45;
}
.parent-warn svg {
  width: 18px;
  height: 18px;
  flex-shrink: 0;
  margin-top: 1px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.8;
  stroke-linecap: round;
  stroke-linejoin: round;
}

/* Auto-password note (BaseModal styles .note, we just add the icon sizing) */
.note {
  display: flex;
  align-items: flex-start;
  gap: 0.6rem;
}
.note svg {
  width: 18px;
  height: 18px;
  flex-shrink: 0;
  margin-top: 1px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.8;
  stroke-linecap: round;
  stroke-linejoin: round;
}

/* Stack to one column on narrow screens */
@media (max-width: 540px) {
  .pi-grid {
    grid-template-columns: 1fr;
  }
}
</style>
