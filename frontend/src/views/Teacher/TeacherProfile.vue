<script setup>
// This is the Teacher's profile page. It shows the teacher's account details
// (name, email, role, teaching level) which are read-only, and it holds the
// "change password" form. To change a password the teacher types their current
// password and a new one, we send them to the server, and we show success or error.
import { ref, computed } from 'vue'
import api from '../../api/axios'
import { useAuthStore } from '../../stores/auth'
import { useLanguageStore } from '../../stores/language'
import TeacherPageHeader from '../../components/teacher/TeacherPageHeader.vue'

const auth = useAuthStore()
const language = useLanguageStore()

// ---- Bilingual copy (existing per-component i18n pattern) ----
const content = {
  en: {
    infoTitle: 'My Information',
    infoSub: 'Your account details (read-only)',
    name: 'Name',
    emailLabel: 'Email',
    roleLabel: 'Role',
    levelLabel: 'Teaching level',
    teacher: 'Teacher',
    secTitle: 'Change Password',
    secSub: 'Update your password to keep your account secure',
    current: 'Current password',
    currentPh: 'Enter your current password',
    newPass: 'New password',
    newPh: 'Enter a new password',
    confirm: 'Confirm new password',
    confirmPh: 'Re-enter the new password',
    rule: 'At least 8 characters, including letters and numbers.',
    show: 'Show',
    hide: 'Hide',
    submit: 'Update password',
    submitting: 'Updating…',
    okChanged: 'Password changed successfully.',
    errMatch: 'New password and confirmation do not match.',
    errWeak: 'Password must be at least 8 characters and include letters and numbers.',
    errSame: 'New password must be different from your current password.',
    errGeneric: 'Could not update the password. Please try again.'
  },
  ar: {
    infoTitle: 'معلوماتي',
    infoSub: 'تفاصيل حسابك (للعرض فقط)',
    name: 'الاسم',
    emailLabel: 'البريد الإلكتروني',
    roleLabel: 'الدور',
    levelLabel: 'مرحلة التدريس',
    teacher: 'معلم',
    secTitle: 'تغيير كلمة المرور',
    secSub: 'حدّث كلمة المرور للحفاظ على أمان حسابك',
    current: 'كلمة المرور الحالية',
    currentPh: 'أدخل كلمة المرور الحالية',
    newPass: 'كلمة المرور الجديدة',
    newPh: 'أدخل كلمة مرور جديدة',
    confirm: 'تأكيد كلمة المرور الجديدة',
    confirmPh: 'أعد إدخال كلمة المرور الجديدة',
    rule: '٨ أحرف على الأقل، تتضمن حروفاً وأرقاماً.',
    show: 'إظهار',
    hide: 'إخفاء',
    submit: 'تحديث كلمة المرور',
    submitting: 'جارٍ التحديث…',
    okChanged: 'تم تغيير كلمة المرور بنجاح.',
    errMatch: 'كلمة المرور الجديدة وتأكيدها غير متطابقين.',
    errWeak: 'يجب أن تتكون كلمة المرور من ٨ أحرف على الأقل وتشمل حروفاً وأرقاماً.',
    errSame: 'يجب أن تكون كلمة المرور الجديدة مختلفة عن الحالية.',
    errGeneric: 'تعذّر تحديث كلمة المرور. يرجى المحاولة مرة أخرى.'
  }
}
const t = computed(() => content[language.lang])

// ---- User display (read-only, from the auth store) ----
const displayName = computed(() => {
  const f = auth.user?.firstName || ''
  const l = auth.user?.lastName || ''
  return `${f} ${l}`.trim() || auth.user?.firstName || 'Teacher'
})
const email = computed(() => auth.user?.email || '—')
const roleLabel = computed(() => t.value.teacher)

// Teacher level — shown only when present in the auth store ("Secondary" / "HighSchool").
const teacherLevelLabel = computed(() => {
  const lvl = auth.user?.teacherLevel
  if (lvl === 'Secondary') return language.isArabic ? 'إعدادي' : 'Secondary'
  if (lvl === 'HighSchool') return language.isArabic ? 'ثانوي' : 'High School'
  return ''
})

// ---- Change password ----
const currentPass = ref('')
const newPass = ref('')
const confirmPass = ref('')
const showCurrent = ref(false)
const showNew = ref(false)
const submitting = ref(false)

// Live strength check (mirrors the backend policy for instant feedback).
const strongEnough = computed(
  () => newPass.value.length >= 8 && /[a-zA-Z]/.test(newPass.value) && /[0-9]/.test(newPass.value)
)
// The button is enabled as soon as all three fields have content (and we're not
// already submitting). The actual rules (strength / match / different) are checked
// on submit and surfaced as friendly messages — so the user always learns *why*
// instead of facing a silently-disabled button.
const canSubmit = computed(
  () => currentPass.value && newPass.value && confirmPass.value && !submitting.value
)

// Status banner shown above the form.
const status = ref({ type: '', text: '' })
let statusTimer = null
function flash(type, text) {
  status.value = { type, text }
  clearTimeout(statusTimer)
  if (type === 'success') statusTimer = setTimeout(() => (status.value = { type: '', text: '' }), 4000)
}

// This function runs when the teacher clicks "Update password".
async function submitPassword() {
  // Clear any old status message first.
  status.value = { type: '', text: '' }
  // Step 1: Do quick checks here for instant feedback (the server checks again too).
  // Is the new password strong enough? Do the two new-password boxes match?
  // Is the new password actually different from the current one?
  // Client-side guards for instant feedback (backend re-validates authoritatively).
  if (!strongEnough.value) return flash('error', t.value.errWeak)
  if (newPass.value !== confirmPass.value) return flash('error', t.value.errMatch)
  if (newPass.value === currentPass.value) return flash('error', t.value.errSame)

  // Show the "Updating..." state while we wait for the server.
  submitting.value = true
  try {
    // Step 2: Send the current + new password to the server to make the change.
    // Reuse the existing Module 1 endpoint. The user is identified from the JWT
    // (attached by the axios interceptor), so we only send the passwords.
    const res = await api.post('/api/auth/change-password', {
      currentPassword: currentPass.value,
      newPassword: newPass.value,
      confirmPassword: confirmPass.value
    })
    // Step 3: Success — show a success message and clear the boxes.
    flash('success', res.data?.message || t.value.okChanged)
    currentPass.value = ''
    newPass.value = ''
    confirmPass.value = ''
  } catch (err) {
    // Step 4: Failure — show the error message (e.g. current password was wrong).
    // Surface the backend's friendly message (e.g. "Current password is incorrect.").
    flash('error', err.response?.data?.message || t.value.errGeneric)
  } finally {
    // Turn off the "Updating..." state whether it worked or not.
    submitting.value = false
  }
}
</script>

<template>
  <div class="profile" :dir="language.dir">
    <TeacherPageHeader />

    <div class="cards">
      <!-- My Information (read-only) -->
      <section class="card">
        <div class="card-head">
          <h3>
            <span class="head-ic">
              <svg viewBox="0 0 24 24"><circle cx="12" cy="8" r="4" /><path d="M4 21a8 8 0 0 1 16 0" /></svg>
            </span>
            {{ t.infoTitle }}
          </h3>
          <p>{{ t.infoSub }}</p>
        </div>

        <dl class="info">
          <div class="info-row">
            <dt>{{ t.name }}</dt>
            <dd>{{ displayName }}</dd>
          </div>
          <div class="info-row">
            <dt>{{ t.emailLabel }}</dt>
            <dd>{{ email }}</dd>
          </div>
          <div class="info-row">
            <dt>{{ t.roleLabel }}</dt>
            <dd><span class="role-pill">{{ roleLabel }}</span></dd>
          </div>
          <div v-if="teacherLevelLabel" class="info-row">
            <dt>{{ t.levelLabel }}</dt>
            <dd>{{ teacherLevelLabel }}</dd>
          </div>
        </dl>
      </section>

      <!-- Change Password -->
      <section class="card">
        <div class="card-head">
          <h3>
            <span class="head-ic">
              <svg viewBox="0 0 24 24"><rect x="4" y="11" width="16" height="10" rx="2" /><path d="M8 11V7a4 4 0 0 1 8 0v4" /></svg>
            </span>
            {{ t.secTitle }}
          </h3>
          <p>{{ t.secSub }}</p>
        </div>

        <!-- Status banner -->
        <Transition name="fade">
          <div v-if="status.text" class="banner" :class="status.type">{{ status.text }}</div>
        </Transition>

        <form class="pw-form" @submit.prevent="submitPassword" autocomplete="off">
          <div class="field">
            <label for="tp-current">{{ t.current }}</label>
            <div class="pw-input">
              <input
                id="tp-current"
                :type="showCurrent ? 'text' : 'password'"
                v-model="currentPass"
                :placeholder="t.currentPh"
                autocomplete="current-password"
              />
              <button type="button" class="toggle" @click="showCurrent = !showCurrent">
                {{ showCurrent ? t.hide : t.show }}
              </button>
            </div>
          </div>

          <div class="field">
            <label for="tp-new">{{ t.newPass }}</label>
            <div class="pw-input">
              <input
                id="tp-new"
                :type="showNew ? 'text' : 'password'"
                v-model="newPass"
                :placeholder="t.newPh"
                autocomplete="new-password"
              />
              <button type="button" class="toggle" @click="showNew = !showNew">
                {{ showNew ? t.hide : t.show }}
              </button>
            </div>
            <p class="rule" :class="{ ok: strongEnough, bad: newPass.length > 0 && !strongEnough }">
              <span class="rule-dot"></span>{{ t.rule }}
            </p>
          </div>

          <div class="field">
            <label for="tp-confirm">{{ t.confirm }}</label>
            <div class="pw-input">
              <input
                id="tp-confirm"
                :type="showNew ? 'text' : 'password'"
                v-model="confirmPass"
                :placeholder="t.confirmPh"
                autocomplete="new-password"
              />
            </div>
          </div>

          <button type="submit" class="btn primary" :disabled="!canSubmit">
            {{ submitting ? t.submitting : t.submit }}
          </button>
        </form>
      </section>
    </div>
  </div>
</template>

<style scoped>
.profile {
  --green: #16a34a;
  --green-strong: #12b981;
  --orange: var(--ds-orange);
  max-width: 760px;
}
.cards {
  display: grid;
  grid-template-columns: 1fr;
  gap: 1.5rem;
}

/* Cards */
.card {
  background: #fff;
  border: 1px solid #e6f0eb;
  border-radius: 18px;
  padding: 1.6rem 1.75rem;
  box-shadow: 0 8px 22px rgba(15, 54, 36, 0.05);
}
.card-head {
  margin-bottom: 1.15rem;
}
.card-head h3 {
  display: flex;
  align-items: center;
  gap: 0.65rem;
  margin: 0;
  font-size: 1.2rem;
  font-weight: 800;
  color: #0f2a1e;
}
.card-head p {
  margin: 0.3rem 0 0;
  font-size: 0.88rem;
  color: #6b8578;
}
/* Small rounded icon badge shared by both card headers */
.head-ic {
  width: 34px;
  height: 34px;
  flex-shrink: 0;
  border-radius: 10px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: var(--green);
  background: linear-gradient(135deg, rgba(16, 163, 74, 0.12), rgba(242, 160, 61, 0.14));
}
.head-ic svg {
  width: 18px;
  height: 18px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.9;
  stroke-linecap: round;
  stroke-linejoin: round;
}

/* Info list */
.info {
  margin: 0;
  padding: 0;
}
.info-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding: 0.9rem 0;
  border-bottom: 1px solid #eef4f1;
}
.info-row:first-child {
  padding-top: 0;
}
.info-row:last-child {
  border-bottom: none;
  padding-bottom: 0;
}
.info-row dt {
  font-size: 0.82rem;
  font-weight: 700;
  letter-spacing: 0.04em;
  text-transform: uppercase;
  color: #82998e;
}
.info-row dd {
  margin: 0;
  font-size: 0.95rem;
  font-weight: 600;
  color: #0f2a1e;
  text-align: end;
  word-break: break-word;
}
.role-pill {
  padding: 0.28rem 0.75rem;
  border-radius: 999px;
  font-size: 0.7rem;
  font-weight: 700;
  letter-spacing: 0.05em;
  text-transform: uppercase;
  color: var(--green);
  background: rgba(16, 163, 74, 0.1);
  white-space: nowrap;
}

/* Password form */
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
.pw-input {
  position: relative;
  display: flex;
  align-items: center;
}
.pw-input input {
  width: 100%;
  padding: 0.75rem 0.95rem;
  padding-inline-end: 4rem;
  font-size: 0.95rem;
  font-family: inherit;
  color: #0f2a1e;
  background: #f5faf7;
  border: 1px solid #dcebe3;
  border-radius: 12px;
  outline: none;
  transition: border-color 0.2s ease, box-shadow 0.2s ease, background 0.2s ease;
}
.pw-input input:focus {
  background: #fff;
  border-color: var(--green);
  box-shadow: 0 0 0 3px rgba(16, 163, 74, 0.12);
}
.toggle {
  position: absolute;
  inset-inline-end: 0.6rem;
  border: none;
  background: transparent;
  color: var(--green);
  font-size: 0.8rem;
  font-weight: 700;
  font-family: inherit;
  cursor: pointer;
  padding: 0.3rem 0.4rem;
}
.rule {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin: 0.15rem 0 0;
  font-size: 0.78rem;
  color: #82998e;
}
.rule-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: #cbd5e1;
  transition: background 0.2s ease;
}
.rule.ok {
  color: #16a34a;
}
.rule.ok .rule-dot {
  background: #16a34a;
}
/* While the new password is being typed but doesn't meet the policy yet */
.rule.bad {
  color: #b45309;
}
.rule.bad .rule-dot {
  background: #d97706;
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
  background: linear-gradient(135deg, var(--green), var(--green-strong));
  box-shadow: 0 8px 18px rgba(16, 163, 74, 0.3);
  align-self: flex-start;
}
.btn.primary:hover:not(:disabled) {
  transform: translateY(-1px);
  box-shadow: 0 12px 24px rgba(16, 163, 74, 0.38);
}
.btn.primary:disabled {
  opacity: 0.5;
  cursor: not-allowed;
  box-shadow: none;
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
</style>
