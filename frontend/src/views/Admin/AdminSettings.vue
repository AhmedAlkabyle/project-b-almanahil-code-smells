<script setup>
import { reactive, ref, computed } from 'vue'
import { useLanguageStore } from '../../stores/language'
import AdminPageHeader from '../../components/admin/AdminPageHeader.vue'

const language = useLanguageStore()

// ---- Bilingual copy (existing per-component i18n pattern) ----
const content = {
  en: {
    schoolTitle: 'School Information',
    schoolSub: 'The details that identify your school across the system',
    schoolName: 'School name',
    schoolNamePh: 'Enter the school name',
    emailLabel: 'Email',
    emailPh: 'info@example.com',
    phoneLabel: 'Phone',
    phonePh: 'Enter a phone number',
    addressLabel: 'Address',
    addressPh: 'Enter the school address',

    prefTitle: 'Preferences',
    prefSub: 'Default language and academic year settings',
    defaultLang: 'Default language',
    english: 'English',
    arabic: 'العربية',
    academicYear: 'Academic year',
    academicYearPh: 'e.g. 2025/2026',

    notifTitle: 'Notifications',
    notifSub: 'Choose which alerts the system sends out',
    emailNotif: 'Email notifications',
    emailNotifHint: 'Send system messages by email.',
    newUser: 'New user alerts',
    newUserHint: 'Notify admins when an account is created.',
    eventRem: 'Event reminders',
    eventRemHint: 'Send reminders before upcoming events.',

    save: 'Save changes',
    okSchool: 'School information saved.',
    okPref: 'Preferences saved.',
    okNotif: 'Notification settings saved.',
    errName: 'Please enter the school name.',
    errEmail: 'Please enter a valid email address.'
  },
  ar: {
    schoolTitle: 'معلومات المدرسة',
    schoolSub: 'التفاصيل التي تُعرّف مدرستك عبر النظام',
    schoolName: 'اسم المدرسة',
    schoolNamePh: 'أدخل اسم المدرسة',
    emailLabel: 'البريد الإلكتروني',
    emailPh: 'info@example.com',
    phoneLabel: 'الهاتف',
    phonePh: 'أدخل رقم الهاتف',
    addressLabel: 'العنوان',
    addressPh: 'أدخل عنوان المدرسة',

    prefTitle: 'التفضيلات',
    prefSub: 'إعدادات اللغة الافتراضية والعام الدراسي',
    defaultLang: 'اللغة الافتراضية',
    english: 'English',
    arabic: 'العربية',
    academicYear: 'العام الدراسي',
    academicYearPh: 'مثال: 2025/2026',

    notifTitle: 'الإشعارات',
    notifSub: 'اختر التنبيهات التي يرسلها النظام',
    emailNotif: 'إشعارات البريد الإلكتروني',
    emailNotifHint: 'إرسال رسائل النظام عبر البريد الإلكتروني.',
    newUser: 'تنبيهات المستخدمين الجدد',
    newUserHint: 'إشعار المسؤولين عند إنشاء حساب جديد.',
    eventRem: 'تذكيرات الفعاليات',
    eventRemHint: 'إرسال تذكيرات قبل الفعاليات القادمة.',

    save: 'حفظ التغييرات',
    okSchool: 'تم حفظ معلومات المدرسة.',
    okPref: 'تم حفظ التفضيلات.',
    okNotif: 'تم حفظ إعدادات الإشعارات.',
    errName: 'يرجى إدخال اسم المدرسة.',
    errEmail: 'يرجى إدخال بريد إلكتروني صحيح.'
  }
}
const t = computed(() => content[language.lang])

// ---- School information ----
const school = reactive({
  name: 'Almanahil Libyan School',
  email: 'info@almanahilschool.com',
  phone: '0176073805',
  address: "Jalan Sejahtera 15, Taman Desa Skudai, 81300 Skudai, Johor Darul Ta'zim"
})
const schoolErrors = reactive({ name: '', email: '' })

const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

function saveSchool() {
  schoolErrors.name = ''
  schoolErrors.email = ''
  let ok = true
  if (!school.name.trim()) {
    schoolErrors.name = t.value.errName
    ok = false
  }
  if (!emailPattern.test(school.email.trim())) {
    schoolErrors.email = t.value.errEmail
    ok = false
  }
  if (!ok) return
  // TODO: replace with PUT /api/settings/school
  flash('school', t.value.okSchool)
}

// ---- Preferences ----
const academicYear = ref('2025/2026')

function onLangChange(e) {
  language.setLanguage(e.target.value === 'ar' ? 'ar' : 'en')
}

function savePreferences() {
  // TODO: replace with PUT /api/settings/preferences
  flash('pref', t.value.okPref)
}

// ---- Notifications ----
const notif = reactive({
  email: true,
  newUser: true,
  eventReminders: false
})

function saveNotifications() {
  // TODO: replace with PUT /api/settings/notifications
  flash('notif', t.value.okNotif)
}

// ---- Per-card success banners (auto-hide after ~3s) ----
const banners = reactive({ school: '', pref: '', notif: '' })
const timers = {}
function flash(key, text) {
  banners[key] = text
  clearTimeout(timers[key])
  timers[key] = setTimeout(() => (banners[key] = ''), 3000)
}
</script>

<template>
  <div class="admin-settings" :dir="language.dir">
    <AdminPageHeader />

    <!-- School information card -->
    <section class="card">
      <div class="card-head">
        <h3>
          <span class="ico">
            <svg viewBox="0 0 24 24"><path d="M3 21h18" /><path d="M5 21V9l7-5 7 5v12" /><path d="M9 21v-6h6v6" /></svg>
          </span>
          {{ t.schoolTitle }}
        </h3>
        <p>{{ t.schoolSub }}</p>
      </div>

      <Transition name="fade">
        <div v-if="banners.school" class="banner success">{{ banners.school }}</div>
      </Transition>

      <form class="pw-form" @submit.prevent="saveSchool" autocomplete="off">
        <div class="field">
          <label for="st-name">{{ t.schoolName }}</label>
          <input id="st-name" type="text" v-model="school.name" :placeholder="t.schoolNamePh" />
          <span v-if="schoolErrors.name" class="err">{{ schoolErrors.name }}</span>
        </div>

        <div class="field">
          <label for="st-email">{{ t.emailLabel }}</label>
          <input id="st-email" type="email" v-model="school.email" :placeholder="t.emailPh" />
          <span v-if="schoolErrors.email" class="err">{{ schoolErrors.email }}</span>
        </div>

        <div class="field">
          <label for="st-phone">{{ t.phoneLabel }}</label>
          <input id="st-phone" type="text" v-model="school.phone" :placeholder="t.phonePh" />
        </div>

        <div class="field">
          <label for="st-address">{{ t.addressLabel }}</label>
          <textarea id="st-address" rows="3" v-model="school.address" :placeholder="t.addressPh"></textarea>
        </div>

        <button type="submit" class="btn primary">{{ t.save }}</button>
      </form>
    </section>

    <!-- Preferences card -->
    <section class="card">
      <div class="card-head">
        <h3>
          <span class="ico">
            <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="3" /><path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 1 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 1 1-2.83-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 1 1 2.83-2.83l.06.06A1.65 1.65 0 0 0 9 4.6a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 1 1 2.83 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z" /></svg>
          </span>
          {{ t.prefTitle }}
        </h3>
        <p>{{ t.prefSub }}</p>
      </div>

      <Transition name="fade">
        <div v-if="banners.pref" class="banner success">{{ banners.pref }}</div>
      </Transition>

      <form class="pw-form" @submit.prevent="savePreferences" autocomplete="off">
        <div class="field">
          <label for="st-lang">{{ t.defaultLang }}</label>
          <select id="st-lang" :value="language.lang" @change="onLangChange">
            <option value="en">{{ t.english }}</option>
            <option value="ar">{{ t.arabic }}</option>
          </select>
        </div>

        <div class="field">
          <label for="st-year">{{ t.academicYear }}</label>
          <input id="st-year" type="text" v-model="academicYear" :placeholder="t.academicYearPh" />
        </div>

        <button type="submit" class="btn primary">{{ t.save }}</button>
      </form>
    </section>

    <!-- Notifications card -->
    <section class="card">
      <div class="card-head">
        <h3>
          <span class="ico">
            <svg viewBox="0 0 24 24"><path d="M18 8a6 6 0 0 0-12 0c0 7-3 9-3 9h18s-3-2-3-9" /><path d="M13.73 21a2 2 0 0 1-3.46 0" /></svg>
          </span>
          {{ t.notifTitle }}
        </h3>
        <p>{{ t.notifSub }}</p>
      </div>

      <Transition name="fade">
        <div v-if="banners.notif" class="banner success">{{ banners.notif }}</div>
      </Transition>

      <form class="pw-form" @submit.prevent="saveNotifications" autocomplete="off">
        <div class="toggle-row">
          <div class="toggle-copy">
            <span class="toggle-title">{{ t.emailNotif }}</span>
            <span class="toggle-hint">{{ t.emailNotifHint }}</span>
          </div>
          <button
            type="button"
            class="switch"
            :class="{ on: notif.email }"
            role="switch"
            :aria-checked="notif.email"
            @click="notif.email = !notif.email"
          >
            <span class="knob"></span>
          </button>
        </div>

        <div class="toggle-row">
          <div class="toggle-copy">
            <span class="toggle-title">{{ t.newUser }}</span>
            <span class="toggle-hint">{{ t.newUserHint }}</span>
          </div>
          <button
            type="button"
            class="switch"
            :class="{ on: notif.newUser }"
            role="switch"
            :aria-checked="notif.newUser"
            @click="notif.newUser = !notif.newUser"
          >
            <span class="knob"></span>
          </button>
        </div>

        <div class="toggle-row">
          <div class="toggle-copy">
            <span class="toggle-title">{{ t.eventRem }}</span>
            <span class="toggle-hint">{{ t.eventRemHint }}</span>
          </div>
          <button
            type="button"
            class="switch"
            :class="{ on: notif.eventReminders }"
            role="switch"
            :aria-checked="notif.eventReminders"
            @click="notif.eventReminders = !notif.eventReminders"
          >
            <span class="knob"></span>
          </button>
        </div>

        <button type="submit" class="btn primary">{{ t.save }}</button>
      </form>
    </section>
  </div>
</template>

<style scoped>
.admin-settings {
  --navy: var(--ds-navy);
  --navy-dark: var(--ds-navy-dark);
  --orange: var(--ds-orange);
  display: grid;
  grid-template-columns: 1fr;
  gap: 1.5rem;
  max-width: 760px;
}

/* Cards */
.card {
  background: #fff;
  border: 1px solid #eaeef6;
  border-radius: 18px;
  padding: 1.6rem 1.75rem;
  box-shadow: 0 8px 22px rgba(30, 41, 59, 0.05);
}
.card-head {
  margin-bottom: 1.25rem;
}
.card-head h3 {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  margin: 0;
  font-size: 1.2rem;
  font-weight: 800;
  color: #0f2444;
}
.card-head p {
  margin: 0.25rem 0 0;
  font-size: 0.88rem;
  color: #6b7280;
}
.ico {
  width: 30px;
  height: 30px;
  flex-shrink: 0;
  border-radius: 9px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: var(--navy);
  background: linear-gradient(135deg, rgba(30, 76, 154, 0.12), rgba(242, 160, 61, 0.14));
}
.ico svg {
  width: 17px;
  height: 17px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.9;
  stroke-linecap: round;
  stroke-linejoin: round;
}

/* Form layout (mirrors AdminProfile .pw-form / .field) */
.pw-form {
  display: flex;
  flex-direction: column;
  gap: 1.1rem;
}
.field {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}
.field label {
  font-size: 0.85rem;
  font-weight: 700;
  color: #334155;
}
.field input,
.field select,
.field textarea {
  width: 100%;
  padding: 0.75rem 0.95rem;
  font-size: 0.95rem;
  font-family: inherit;
  color: #0f2444;
  background: #f7f9fc;
  border: 1px solid #e2e8f2;
  border-radius: 12px;
  outline: none;
  transition: border-color 0.2s ease, box-shadow 0.2s ease, background 0.2s ease;
}
.field textarea {
  resize: vertical;
  min-height: 84px;
  line-height: 1.5;
}
.field select {
  cursor: pointer;
  appearance: none;
  background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='%236b7280' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'%3E%3Cpolyline points='6 9 12 15 18 9'/%3E%3C/svg%3E");
  background-repeat: no-repeat;
  background-position: right 0.9rem center;
  background-size: 18px;
  padding-inline-end: 2.6rem;
}
[dir='rtl'] .field select {
  background-position: left 0.9rem center;
}
.field input:focus,
.field select:focus,
.field textarea:focus {
  background: #fff;
  border-color: var(--navy);
  box-shadow: 0 0 0 3px rgba(30, 76, 154, 0.12);
}
.err {
  font-size: 0.78rem;
  font-weight: 600;
  color: #b91c1c;
}

/* Toggle switch rows */
.toggle-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding: 0.9rem 1.1rem;
  background: #f7f9fc;
  border: 1px solid #eef1f7;
  border-radius: 14px;
}
.toggle-copy {
  display: flex;
  flex-direction: column;
  gap: 0.1rem;
}
.toggle-title {
  font-weight: 700;
  color: #0f2444;
  font-size: 0.92rem;
}
.toggle-hint {
  font-size: 0.78rem;
  color: #8a94a6;
}
.switch {
  position: relative;
  flex-shrink: 0;
  width: 48px;
  height: 28px;
  border: none;
  border-radius: 999px;
  background: #cbd5e1;
  cursor: pointer;
  padding: 0;
  transition: background 0.22s ease;
}
.switch.on {
  background: var(--navy);
}
.knob {
  position: absolute;
  top: 3px;
  inset-inline-start: 3px;
  width: 22px;
  height: 22px;
  border-radius: 50%;
  background: #fff;
  box-shadow: 0 2px 5px rgba(15, 35, 80, 0.28);
  transition: transform 0.22s ease;
}
.switch.on .knob {
  transform: translateX(20px);
}
[dir='rtl'] .switch.on .knob {
  transform: translateX(-20px);
}

/* Buttons */
.btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.45rem;
  padding: 0.8rem 1.4rem;
  border: none;
  border-radius: 12px;
  font-size: 0.92rem;
  font-weight: 700;
  font-family: inherit;
  cursor: pointer;
  transition: transform 0.2s ease, box-shadow 0.2s ease, background 0.2s ease, opacity 0.2s ease;
}
.btn.primary {
  color: #fff;
  background: linear-gradient(135deg, var(--navy), #2f63ba);
  box-shadow: 0 8px 18px rgba(30, 76, 154, 0.3);
  align-self: flex-start;
}
.btn.primary:hover:not(:disabled) {
  transform: translateY(-1px);
  box-shadow: 0 12px 24px rgba(30, 76, 154, 0.38);
}

/* Status banner */
.banner {
  margin-bottom: 1.1rem;
  padding: 0.75rem 1rem;
  border-radius: 12px;
  font-size: 0.88rem;
  font-weight: 600;
}
.banner.success {
  color: #166534;
  background: #dcfce7;
  border: 1px solid #bbf7d0;
}
.banner.error {
  color: #b91c1c;
  background: #fee2e2;
  border: 1px solid #fecaca;
}
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.25s ease;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

@media (max-width: 560px) {
  .toggle-row {
    align-items: flex-start;
  }
}
</style>
