<script setup>
// This is the "forgot my password" page. It resets a password using an email code.
// It works in 3 steps: (1) the user types their email and we ask the server to send
// them a 6-digit code, (2) the user types that code plus a new password and we ask
// the server to check the code and save the new password, (3) we show a success screen.
// The page also supports two languages (English and Arabic).
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import api from '../../api/axios'
import { useLanguageStore } from '../../stores/language'
import LanguageToggle from '../../components/LanguageToggle.vue'
import schoolLogo from '../../assets/school-logo.jpg.jpg'
import loginIllustration from '../../assets/login-illustration.svg.svg'

const router = useRouter()
const language = useLanguageStore()

// step 1 = request code (email), 2 = enter code + new password, 3 = success
const step = ref(1)

const email = ref('')
const code = ref('')
const newPassword = ref('')
const confirmPassword = ref('')

const showPassword = ref(false)
const loading = ref(false)
const errorMessage = ref('')

// ---- Bilingual labels (matches the login page's per-component i18n pattern) ----
const content = {
  en: {
    back: 'Back',
    brandHeading: 'Reset Your Password',
    brandSubtitle: "We'll help you get back into your account safely.",
    steps: ['Enter your email', 'Verify the 6-digit code', 'Set a new password'],
    eyebrow: 'Password Reset',
    // Step 1
    title1: 'Forgot Password?',
    sub1: "Enter your email address and we'll send you a code to reset your password.",
    emailLabel: 'Email Address',
    emailPlaceholder: 'Enter your email',
    sendBtn: 'Send Reset Code',
    sending: 'Sending…',
    rememberQ: 'Remembered your password?',
    signIn: 'Sign In',
    // Step 2
    title2: 'Check Your Email',
    sub2: 'We sent a 6-digit code to',
    codeLabel: 'Reset Code',
    codePlaceholder: 'Enter the 6-digit code',
    newPassLabel: 'New Password',
    newPassPlaceholder: 'Enter a new password',
    confirmLabel: 'Confirm New Password',
    confirmPlaceholder: 'Re-enter the new password',
    rule: 'At least 8 characters, including letters and numbers.',
    resetBtn: 'Reset Password',
    resetting: 'Resetting…',
    resend: 'Resend code',
    changeEmail: 'Change email',
    // Step 3
    title3: 'Password Reset!',
    sub3: 'Your password has been updated. You can now sign in with your new password.',
    backToLogin: 'Back to Sign In',
    // Errors
    invalidEmail: 'Please enter a valid email address (e.g. name@example.com).',
    errCode: 'Please enter the 6-digit code.',
    errWeak: 'Password must be at least 8 characters and include letters and numbers.',
    errMatch: 'New password and confirmation do not match.',
    genericError: 'Something went wrong. Please try again.'
  },
  ar: {
    back: 'رجوع',
    brandHeading: 'إعادة تعيين كلمة المرور',
    brandSubtitle: 'سنساعدك على العودة إلى حسابك بأمان.',
    steps: ['أدخل بريدك الإلكتروني', 'تحقق من الرمز المكوّن من 6 أرقام', 'عيّن كلمة مرور جديدة'],
    eyebrow: 'إعادة تعيين كلمة المرور',
    title1: 'نسيت كلمة المرور؟',
    sub1: 'أدخل بريدك الإلكتروني وسنرسل لك رمزاً لإعادة تعيين كلمة المرور.',
    emailLabel: 'البريد الإلكتروني',
    emailPlaceholder: 'أدخل بريدك الإلكتروني',
    sendBtn: 'إرسال رمز التحقق',
    sending: 'جاري الإرسال…',
    rememberQ: 'تذكرت كلمة المرور؟',
    signIn: 'تسجيل الدخول',
    title2: 'تحقق من بريدك الإلكتروني',
    sub2: 'أرسلنا رمزاً مكوّناً من 6 أرقام إلى',
    codeLabel: 'رمز التحقق',
    codePlaceholder: 'أدخل الرمز المكوّن من 6 أرقام',
    newPassLabel: 'كلمة المرور الجديدة',
    newPassPlaceholder: 'أدخل كلمة مرور جديدة',
    confirmLabel: 'تأكيد كلمة المرور الجديدة',
    confirmPlaceholder: 'أعد إدخال كلمة المرور الجديدة',
    rule: '٨ أحرف على الأقل، تتضمن حروفاً وأرقاماً.',
    resetBtn: 'إعادة تعيين كلمة المرور',
    resetting: 'جاري إعادة التعيين…',
    resend: 'إعادة إرسال الرمز',
    changeEmail: 'تغيير البريد الإلكتروني',
    title3: 'تمت إعادة التعيين!',
    sub3: 'تم تحديث كلمة المرور. يمكنك الآن تسجيل الدخول بكلمة المرور الجديدة.',
    backToLogin: 'العودة لتسجيل الدخول',
    invalidEmail: 'يرجى إدخال بريد إلكتروني صحيح (مثال: name@example.com).',
    errCode: 'يرجى إدخال الرمز المكوّن من 6 أرقام.',
    errWeak: 'يجب أن تتكون كلمة المرور من ٨ أحرف على الأقل وتشمل حروفاً وأرقاماً.',
    errMatch: 'كلمة المرور الجديدة وتأكيدها غير متطابقين.',
    genericError: 'حدث خطأ ما. يرجى المحاولة مرة أخرى.'
  }
}
const t = computed(() => content[language.lang])

// Which brand-panel guide item is active (0-based). At success, all are done.
const activeStep = computed(() => step.value - 1)

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[a-zA-Z]{2,}$/
const strongEnough = computed(
  () => newPassword.value.length >= 8 && /[a-zA-Z]/.test(newPassword.value) && /[0-9]/.test(newPassword.value)
)

// Ask the browser's password manager to save the NEW password for this email, so
// it's autofilled next time the same account signs in. Uses the Credential
// Management API where available; silently no-ops otherwise (older browsers still
// get the native "update password?" prompt via the form's autocomplete hints).
async function saveCredentialToBrowser() {
  try {
    if (window.PasswordCredential && navigator.credentials?.store) {
      const cred = new window.PasswordCredential({
        id: email.value.trim(),
        password: newPassword.value,
        name: email.value.trim()
      })
      await navigator.credentials.store(cred)
    }
  } catch {
    // Non-fatal — user may have declined or the API is unavailable.
  }
}

// This function runs when the user submits their email to ask for a reset code.
// STEP 1 — request a reset code. The backend always responds success (it never
// reveals whether the email exists), so we advance to step 2 on any 2xx.
async function requestCode() {
  // Clear any old error message first.
  errorMessage.value = ''
  // Step 1: Make sure the email looks correct before sending it.
  if (!EMAIL_PATTERN.test(email.value.trim())) {
    errorMessage.value = t.value.invalidEmail
    return
  }
  // Show the "Sending..." state while we wait for the server.
  loading.value = true
  try {
    // Step 2: Ask the server to email a 6-digit code to this address.
    // Backend: POST /api/auth/forgot-password { email } → generic success message.
    await api.post('/api/auth/forgot-password', { email: email.value.trim() })
    // Step 3: Move on to the next screen where the user types the code + a new password.
    step.value = 2
  } catch (err) {
    errorMessage.value = err?.response?.data?.message || t.value.genericError
  } finally {
    loading.value = false
  }
}

// This function runs when the user submits the code and their new password.
// STEP 2 — submit the code + new password.
async function resetPassword() {
  // Clear any old error message first.
  errorMessage.value = ''
  // Step 1: Check the user actually typed a code.
  if (!code.value.trim()) {
    errorMessage.value = t.value.errCode
    return
  }
  // Step 2: Check the new password is strong enough (length + letters + numbers).
  if (!strongEnough.value) {
    errorMessage.value = t.value.errWeak
    return
  }
  // Step 3: Check the new password and its confirmation are the same.
  if (newPassword.value !== confirmPassword.value) {
    errorMessage.value = t.value.errMatch
    return
  }
  // Show the "Resetting..." state while we wait for the server.
  loading.value = true
  try {
    // Step 4: Send the email, the code, and the new password to the server.
    // The server checks the code is correct and then updates the password.
    // Backend: POST /api/auth/reset-password { email, code, newPassword, confirmPassword }.
    await api.post('/api/auth/reset-password', {
      email: email.value.trim(),
      code: code.value.trim(),
      newPassword: newPassword.value,
      confirmPassword: confirmPassword.value
    })
    // Offer the new password to the browser's password manager for this email.
    await saveCredentialToBrowser()
    // Step 5: Success — show the "Password Reset!" screen.
    step.value = 3
  } catch (err) {
    errorMessage.value = err?.response?.data?.message || t.value.genericError
  } finally {
    loading.value = false
  }
}

function resendCode() {
  errorMessage.value = ''
  requestCode()
}

function changeEmail() {
  errorMessage.value = ''
  code.value = ''
  step.value = 1
}

function goToLogin() {
  router.push('/login')
}
</script>

<template>
  <div class="forgot-layout" :dir="language.dir" :lang="language.lang">
    <!-- Language toggle (top-right) -->
    <div class="toggle-holder">
      <LanguageToggle />
    </div>

    <!-- Back to login -->
    <router-link to="/login" class="back-link">
      <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
        <path d="m15 5-7 7 7 7" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
      </svg>
      <span>{{ t.back }}</span>
    </router-link>

    <!-- LEFT: branded panel with a live step guide (hidden on mobile) -->
    <aside class="brand-panel">
      <span class="circle" aria-hidden="true"></span>
      <div class="brand-content">
        <h2 class="brand-heading">{{ t.brandHeading }}</h2>
        <div class="illustration-frame">
          <img :src="loginIllustration" alt="" class="illustration" />
        </div>
        <p class="brand-subtitle">{{ t.brandSubtitle }}</p>

        <ol class="guide">
          <li
            v-for="(s, i) in t.steps"
            :key="i"
            :class="{ active: i === activeStep && step !== 3, done: i < activeStep || step === 3 }"
          >
            <span class="guide-num">
              <svg v-if="i < activeStep || step === 3" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                <path d="m5 12 4.5 4.5L19 7" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round" />
              </svg>
              <template v-else>{{ i + 1 }}</template>
            </span>
            <span class="guide-text">{{ s }}</span>
          </li>
        </ol>
      </div>
    </aside>

    <!-- Curved wave boundary between the two panels -->
    <div class="wave-divider" aria-hidden="true">
      <svg viewBox="0 0 80 1000" preserveAspectRatio="none">
        <defs>
          <linearGradient id="fpWaveGrad" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0" stop-color="#2f63ba" />
            <stop offset="0.45" stop-color="#1e4c9a" />
            <stop offset="1" stop-color="#163a78" />
          </linearGradient>
        </defs>
        <path
          d="M0,0 L40,0 C72,110 8,210 40,320 C72,430 8,540 40,650 C72,760 8,870 40,1000 L0,1000 Z"
          fill="url(#fpWaveGrad)"
        />
      </svg>
    </div>

    <!-- RIGHT: form panel -->
    <main class="form-panel">
      <div class="form-card">
        <div class="logo-tile">
          <img :src="schoolLogo" alt="" class="logo" />
        </div>

        <Transition name="step" mode="out-in">
          <div :key="step" class="step-view">
            <!-- ============ STEP 1: request code ============ -->
            <template v-if="step === 1">
              <span class="step-badge">
                <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
                  <rect x="3" y="5" width="18" height="14" rx="2" stroke="currentColor" stroke-width="1.9" />
                  <path d="m4 7 8 6 8-6" stroke="currentColor" stroke-width="1.9" stroke-linecap="round" stroke-linejoin="round" />
                </svg>
              </span>
              <span class="form-eyebrow">{{ t.eyebrow }}</span>
              <h1 class="title">{{ t.title1 }}</h1>
              <p class="subtitle">{{ t.sub1 }}</p>

              <form class="form" @submit.prevent="requestCode" novalidate>
                <label class="field" for="fp-email">
                  <span class="field-label">{{ t.emailLabel }}</span>
                  <div class="input-wrap">
                    <span class="input-icon">
                      <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
                        <rect x="3" y="5" width="18" height="14" rx="2" stroke="currentColor" stroke-width="1.8" />
                        <path d="m4 7 8 6 8-6" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" />
                      </svg>
                    </span>
                    <input id="fp-email" v-model="email" type="email" name="username" :placeholder="t.emailPlaceholder" autocomplete="username" required />
                  </div>
                </label>

                <p v-if="errorMessage" class="error">{{ errorMessage }}</p>

                <button type="submit" class="primary-btn" :disabled="loading">
                  {{ loading ? t.sending : t.sendBtn }}
                </button>
              </form>

              <p class="foot-hint">
                {{ t.rememberQ }}
                <router-link to="/login" class="link">{{ t.signIn }}</router-link>
              </p>
            </template>

            <!-- ============ STEP 2: enter code + new password ============ -->
            <template v-else-if="step === 2">
              <span class="step-badge">
                <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
                  <rect x="5" y="10" width="14" height="10" rx="2" stroke="currentColor" stroke-width="1.9" />
                  <path d="M8 10V7a4 4 0 0 1 8 0v3" stroke="currentColor" stroke-width="1.9" stroke-linecap="round" />
                </svg>
              </span>
              <span class="form-eyebrow">{{ t.eyebrow }}</span>
              <h1 class="title">{{ t.title2 }}</h1>
              <p class="subtitle">{{ t.sub2 }}</p>
              <p class="email-chip" dir="ltr">{{ email }}</p>

              <form class="form" @submit.prevent="resetPassword" novalidate>
                <!-- Hidden username so the password manager ties the new password to this account -->
                <input class="sr-only" type="text" name="username" autocomplete="username" :value="email" tabindex="-1" aria-hidden="true" readonly />

                <!-- Code -->
                <label class="field" for="fp-code">
                  <span class="field-label">{{ t.codeLabel }}</span>
                  <div class="input-wrap">
                    <span class="input-icon">
                      <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
                        <rect x="4" y="5" width="16" height="14" rx="2" stroke="currentColor" stroke-width="1.8" />
                        <path d="M8 10h.01M12 10h.01M16 10h.01M8 14h8" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" />
                      </svg>
                    </span>
                    <input
                      id="fp-code"
                      v-model="code"
                      type="text"
                      inputmode="numeric"
                      maxlength="6"
                      autocomplete="one-time-code"
                      :placeholder="t.codePlaceholder"
                      class="code-input"
                    />
                  </div>
                </label>

                <!-- New password -->
                <label class="field" for="fp-new">
                  <span class="field-label">{{ t.newPassLabel }}</span>
                  <div class="input-wrap">
                    <span class="input-icon">
                      <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
                        <rect x="5" y="10" width="14" height="10" rx="2" stroke="currentColor" stroke-width="1.8" />
                        <path d="M8 10V7a4 4 0 0 1 8 0v3" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" />
                      </svg>
                    </span>
                    <input
                      id="fp-new"
                      v-model="newPassword"
                      :type="showPassword ? 'text' : 'password'"
                      :placeholder="t.newPassPlaceholder"
                      autocomplete="new-password"
                    />
                    <button
                      type="button"
                      class="toggle-eye"
                      @click="showPassword = !showPassword"
                      :aria-label="showPassword ? 'Hide password' : 'Show password'"
                    >
                      <svg v-if="!showPassword" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                        <path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7-10-7-10-7Z" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" />
                        <circle cx="12" cy="12" r="3" stroke="currentColor" stroke-width="1.8" />
                      </svg>
                      <svg v-else viewBox="0 0 24 24" fill="none" aria-hidden="true">
                        <path d="M3 3l18 18" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" />
                        <path d="M10.6 10.6a3 3 0 0 0 4.2 4.2" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" />
                        <path d="M9.4 5.2A9.5 9.5 0 0 1 12 5c6.5 0 10 7 10 7a17 17 0 0 1-2.2 3M6.1 6.1A17 17 0 0 0 2 12s3.5 7 10 7a9.6 9.6 0 0 0 2.6-.35" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" />
                      </svg>
                    </button>
                  </div>
                  <p class="rule" :class="{ ok: strongEnough }"><span class="rule-dot"></span>{{ t.rule }}</p>
                </label>

                <!-- Confirm password -->
                <label class="field" for="fp-confirm">
                  <span class="field-label">{{ t.confirmLabel }}</span>
                  <div class="input-wrap">
                    <span class="input-icon">
                      <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
                        <rect x="5" y="10" width="14" height="10" rx="2" stroke="currentColor" stroke-width="1.8" />
                        <path d="M8 10V7a4 4 0 0 1 8 0v3" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" />
                      </svg>
                    </span>
                    <input
                      id="fp-confirm"
                      v-model="confirmPassword"
                      :type="showPassword ? 'text' : 'password'"
                      :placeholder="t.confirmPlaceholder"
                      autocomplete="new-password"
                    />
                  </div>
                </label>

                <p v-if="errorMessage" class="error">{{ errorMessage }}</p>

                <button type="submit" class="primary-btn" :disabled="loading">
                  {{ loading ? t.resetting : t.resetBtn }}
                </button>

                <div class="step2-links">
                  <button type="button" class="link" @click="resendCode">{{ t.resend }}</button>
                  <span class="dot-sep">•</span>
                  <button type="button" class="link" @click="changeEmail">{{ t.changeEmail }}</button>
                </div>
              </form>
            </template>

            <!-- ============ STEP 3: success ============ -->
            <template v-else>
              <span class="success-icon">
                <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
                  <circle cx="12" cy="12" r="11" fill="#16a34a" />
                  <path d="m7 12.5 3.2 3.2L17 9" stroke="#fff" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round" />
                </svg>
              </span>
              <h1 class="title">{{ t.title3 }}</h1>
              <p class="subtitle">{{ t.sub3 }}</p>
              <button type="button" class="primary-btn" @click="goToLogin">{{ t.backToLogin }}</button>
            </template>
          </div>
        </Transition>
      </div>
    </main>
  </div>
</template>

<style scoped>
:global(html),
:global(body),
:global(#app) {
  margin: 0;
  padding: 0;
  width: 100%;
  height: 100%;
}
*,
*::before,
*::after {
  box-sizing: border-box;
}

.forgot-layout {
  --navy: var(--ds-navy);
  --navy-dark: var(--ds-navy-dark);
  --orange: var(--ds-orange);

  position: relative;
  display: flex;
  width: 100%;
  height: 100vh;
  overflow: hidden;
  font-family: 'Segoe UI', system-ui, -apple-system, sans-serif;
}
.forgot-layout[lang='ar'] {
  font-family: 'Segoe UI', 'Tahoma', system-ui, sans-serif;
}

/* Screen-reader-only (hidden username field for password managers) */
.sr-only {
  position: absolute;
  width: 1px;
  height: 1px;
  padding: 0;
  margin: -1px;
  overflow: hidden;
  clip: rect(0, 0, 0, 0);
  border: 0;
}

/* Top-corner controls */
.toggle-holder {
  position: absolute;
  top: 1.25rem;
  right: 1.25rem;
  z-index: 12;
}
.back-link {
  position: absolute;
  top: 1.25rem;
  left: 1.25rem;
  z-index: 12;
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  padding: 0.4rem 0.95rem;
  border-radius: 999px;
  color: #fff;
  font-size: 0.9rem;
  font-weight: 600;
  text-decoration: none;
  background: var(--navy);
  box-shadow: 0 4px 14px rgba(30, 76, 154, 0.3);
  transition: filter 0.25s ease, transform 0.2s ease;
}
.back-link:hover {
  filter: brightness(1.12);
  transform: translateX(-2px);
}
.back-link svg {
  width: 18px;
  height: 18px;
}
.forgot-layout[dir='rtl'] .back-link svg {
  transform: scaleX(-1);
}

/* ---------- LEFT brand panel ---------- */
.brand-panel {
  width: 48%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 2.5rem;
  position: relative;
  overflow: hidden;
  background:
    radial-gradient(circle at 22% 24%, rgba(255, 255, 255, 0.12), transparent 40%),
    radial-gradient(circle at 50% 82%, rgba(242, 160, 61, 0.16), transparent 48%),
    linear-gradient(180deg, #2f63ba 0%, var(--navy) 45%, var(--navy-dark) 100%);
}
.brand-panel::before {
  content: '';
  position: absolute;
  inset: 0;
  pointer-events: none;
  background-image: radial-gradient(rgba(255, 255, 255, 0.1) 1.5px, transparent 1.6px);
  background-size: 30px 30px;
  -webkit-mask-image: radial-gradient(ellipse at 50% 45%, #000 10%, transparent 78%);
  mask-image: radial-gradient(ellipse at 50% 45%, #000 10%, transparent 78%);
  opacity: 0.7;
}
.brand-panel .circle {
  position: absolute;
  width: 420px;
  height: 420px;
  border-radius: 50%;
  background: rgba(242, 160, 61, 0.16);
  top: -140px;
  right: -140px;
  animation: drift 18s ease-in-out infinite;
}
.brand-content {
  position: relative;
  z-index: 1;
  text-align: center;
  max-width: 430px;
  color: #fff;
  animation: fade-in 0.8s ease-out both;
}
.brand-heading {
  font-size: 1.95rem;
  font-weight: 800;
  line-height: 1.22;
  letter-spacing: -0.01em;
  margin: 0 0 1.25rem;
  text-shadow: 0 2px 12px rgba(0, 0, 0, 0.18);
}
.illustration-frame {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  width: min(100%, 300px);
  margin: 0 auto;
}
.illustration-frame::before {
  content: '';
  position: absolute;
  inset: -14% -6%;
  z-index: 0;
  border-radius: 50%;
  background: radial-gradient(circle, rgba(242, 160, 61, 0.28), rgba(124, 175, 255, 0.14) 45%, transparent 70%);
  filter: blur(10px);
  animation: pulse-glow 6s ease-in-out infinite;
}
.illustration {
  position: relative;
  z-index: 1;
  width: 100%;
  max-width: 280px;
  height: auto;
  display: block;
  margin: 0 auto;
  filter: drop-shadow(0 16px 30px rgba(0, 0, 0, 0.32));
  animation: float 5s ease-in-out infinite;
}
.brand-subtitle {
  margin: 1.25rem 0 0;
  font-size: 1.02rem;
  color: rgba(255, 255, 255, 0.9);
}
/* Step guide */
.guide {
  list-style: none;
  margin: 1.5rem 0 0;
  padding: 1.5rem 0 0;
  border-top: 1px solid rgba(255, 255, 255, 0.18);
  display: flex;
  flex-direction: column;
  gap: 0.9rem;
  text-align: start;
}
.guide li {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  font-size: 0.98rem;
  color: rgba(255, 255, 255, 0.55);
  transition: color 0.3s ease;
}
.guide-num {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 30px;
  height: 30px;
  flex-shrink: 0;
  border-radius: 50%;
  font-size: 0.85rem;
  font-weight: 800;
  color: rgba(255, 255, 255, 0.75);
  background: rgba(255, 255, 255, 0.12);
  border: 1.5px solid rgba(255, 255, 255, 0.25);
  transition: all 0.3s ease;
}
.guide-num svg {
  width: 16px;
  height: 16px;
}
/* Active step: orange highlight */
.guide li.active {
  color: #fff;
  font-weight: 700;
}
.guide li.active .guide-num {
  color: #fff;
  background: var(--orange);
  border-color: var(--orange);
  box-shadow: 0 6px 16px rgba(242, 160, 61, 0.5);
}
/* Completed step: filled check */
.guide li.done {
  color: rgba(255, 255, 255, 0.9);
}
.guide li.done .guide-num {
  color: #fff;
  background: #16a34a;
  border-color: #16a34a;
}

/* ---------- Curved wave boundary ---------- */
.wave-divider {
  position: absolute;
  top: 0;
  bottom: 0;
  left: 48%;
  width: 80px;
  z-index: 2;
  pointer-events: none;
}
.wave-divider svg {
  width: 100%;
  height: 100%;
  display: block;
}
.forgot-layout[dir='rtl'] .wave-divider {
  left: auto;
  right: 48%;
  transform: scaleX(-1);
}

/* ---------- RIGHT form panel ---------- */
.form-panel {
  position: relative;
  width: 52%;
  height: 100%;
  display: flex;
  /* Scroll the form panel (not the whole page) when the step is taller than the
     viewport — the branded panel stays fixed. margin:auto on the card centers it
     when there's room and lets it scroll from the top when there isn't. */
  padding: 3.5rem 2rem 2.5rem;
  overflow-x: hidden;
  overflow-y: auto;
  background: linear-gradient(160deg, #ffffff 0%, #eef3fb 100%);
}
.form-panel::before,
.form-panel::after {
  content: '';
  position: absolute;
  border-radius: 50%;
  pointer-events: none;
}
.form-panel::before {
  width: 260px;
  height: 260px;
  top: -90px;
  right: -70px;
  background: rgba(242, 160, 61, 0.08);
  filter: blur(8px);
}
.form-panel::after {
  width: 300px;
  height: 300px;
  bottom: -110px;
  left: -90px;
  background: rgba(30, 76, 154, 0.07);
  filter: blur(8px);
}
.form-card {
  position: relative;
  z-index: 3;
  width: 100%;
  max-width: 400px;
  text-align: center;
  background: #fff;
  border: 1px solid #eef2f8;
  border-radius: 22px;
  padding: 4rem 2.25rem 2.25rem;
  margin: auto;
  flex-shrink: 0;
  box-shadow: 0 24px 60px rgba(30, 76, 154, 0.18);
  animation: slide-up 0.5s ease-out both;
}
.form-card::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 6px;
  border-radius: 22px 22px 0 0;
  background: linear-gradient(90deg, var(--navy), var(--orange));
}
.logo-tile {
  position: absolute;
  top: -52px;
  left: 50%;
  margin-left: -52px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 104px;
  height: 104px;
  background: #fff;
  border-radius: 24px;
  border: 5px solid #fff;
  box-shadow: 0 12px 26px rgba(30, 76, 154, 0.25);
  cursor: pointer;
  animation: float 3.5s ease-in-out infinite;
  transition: box-shadow 0.3s ease, border-color 0.3s ease;
  z-index: 4;
}
/* Orange highlight on hover — same treatment as the login / welcome logo */
.logo-tile:hover {
  border-color: var(--orange);
  box-shadow: 0 16px 36px rgba(242, 160, 61, 0.5);
}
.logo {
  width: 78px;
  height: 78px;
  object-fit: contain;
  border-radius: 14px;
  transition: transform 0.3s ease;
}
.logo-tile:hover .logo {
  transform: scale(1.1);
}

/* Per-step icon badge above the eyebrow */
.step-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 46px;
  height: 46px;
  margin: 0 auto 0.7rem;
  border-radius: 14px;
  color: var(--navy);
  background: linear-gradient(135deg, rgba(30, 76, 154, 0.12), rgba(242, 160, 61, 0.16));
}
.step-badge svg {
  width: 24px;
  height: 24px;
}

.form-eyebrow {
  display: block;
  font-size: 0.74rem;
  font-weight: 700;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: var(--orange);
  margin-bottom: 0.3rem;
}
.title {
  font-size: 1.6rem;
  font-weight: 800;
  letter-spacing: -0.01em;
  color: var(--navy);
  margin: 0 0 0.3rem;
}
.subtitle {
  color: #6b7280;
  margin: 0 0 0.6rem;
  font-size: 0.95rem;
  line-height: 1.5;
}
/* Email pill in step 2 */
.email-chip {
  display: inline-block;
  margin: 0 0 1.5rem;
  padding: 0.4rem 0.9rem;
  border-radius: 999px;
  background: #eef4ff;
  border: 1px solid #d7e3fb;
  color: var(--navy);
  font-size: 0.88rem;
  font-weight: 700;
}

.form {
  display: flex;
  flex-direction: column;
  gap: 0.95rem;
  text-align: start;
}
.field {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}
.field-label {
  font-size: 0.74rem;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--navy);
}
.input-wrap {
  display: flex;
  align-items: stretch;
  background: #f4f7fc;
  border: 1.5px solid #e6ebf4;
  border-radius: 14px;
  overflow: hidden;
  transition: border-color 0.3s ease, box-shadow 0.3s ease, background 0.3s ease;
}
.input-wrap:focus-within {
  border-color: var(--navy);
  background: #fff;
  box-shadow: 0 0 0 4px rgba(30, 76, 154, 0.14);
}
.input-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 52px;
  flex-shrink: 0;
  color: var(--navy);
  border-inline-end: 1.5px solid #e6ebf4;
  transition: border-color 0.3s ease;
}
.input-wrap:focus-within .input-icon {
  border-inline-end-color: rgba(30, 76, 154, 0.25);
}
.input-icon svg {
  width: 20px;
  height: 20px;
}
.input-wrap input {
  flex: 1;
  min-width: 0;
  border: none;
  background: transparent;
  padding: 0.95rem 1rem;
  font-size: 1rem;
  color: #1f2937;
  outline: none;
}
.input-wrap input::placeholder {
  color: #9ca3af;
}
.code-input {
  letter-spacing: 0.3em;
  font-weight: 700;
}
.toggle-eye {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 50px;
  flex-shrink: 0;
  padding: 0;
  border: none;
  background: transparent;
  color: #9ca3af;
  cursor: pointer;
  transition: color 0.25s ease;
}
.toggle-eye:hover {
  color: var(--navy);
}
.toggle-eye svg {
  width: 20px;
  height: 20px;
}

.rule {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin: 0.15rem 0 0;
  font-size: 0.78rem;
  color: #8a94a6;
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

.error {
  color: #dc2626;
  background: #fef2f2;
  border: 1px solid #fecaca;
  border-radius: 10px;
  padding: 0.65rem 0.8rem;
  margin: 0;
  font-size: 0.88rem;
}

.primary-btn {
  position: relative;
  overflow: hidden;
  margin-top: 0.4rem;
  padding: 0.95rem;
  background: linear-gradient(135deg, var(--navy), #2f63ba);
  color: #fff;
  border: none;
  border-radius: 12px;
  font-size: 1.02rem;
  font-weight: 700;
  letter-spacing: 0.02em;
  cursor: pointer;
  box-shadow: 0 10px 24px rgba(30, 76, 154, 0.3);
  transition: transform 0.2s ease, filter 0.25s ease, box-shadow 0.3s ease;
}
.primary-btn::after {
  content: '';
  position: absolute;
  top: 0;
  inset-inline-start: -75%;
  width: 50%;
  height: 100%;
  background: linear-gradient(120deg, transparent, rgba(255, 255, 255, 0.35), transparent);
  transform: skewX(-20deg);
  transition: inset-inline-start 0.6s ease;
  pointer-events: none;
}
.primary-btn:hover:not(:disabled)::after {
  inset-inline-start: 125%;
}
.primary-btn:hover:not(:disabled) {
  transform: translateY(-2px);
  filter: brightness(1.07);
  box-shadow: 0 14px 30px rgba(30, 76, 154, 0.4);
}
.primary-btn:disabled {
  background: #9db8de;
  box-shadow: none;
  cursor: not-allowed;
}

.link {
  background: none;
  border: none;
  padding: 0;
  font: inherit;
  cursor: pointer;
  color: var(--navy);
  font-weight: 700;
  text-decoration: none;
  transition: color 0.2s ease;
}
.link:hover {
  color: var(--orange);
  text-decoration: underline;
}
.foot-hint {
  margin: 1.5rem 0 0;
  padding-top: 1.25rem;
  border-top: 1px solid #eef2f8;
  font-size: 0.9rem;
  color: #6b7280;
}
.step2-links {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.6rem;
  margin-top: 0.4rem;
  font-size: 0.86rem;
}
.dot-sep {
  color: #cbd5e1;
}

.success-icon {
  display: inline-flex;
  margin-bottom: 0.4rem;
}
.success-icon svg {
  width: 64px;
  height: 64px;
  filter: drop-shadow(0 8px 18px rgba(22, 163, 74, 0.35));
}

/* Smooth transition between steps */
.step-enter-active,
.step-leave-active {
  transition: opacity 0.3s ease, transform 0.3s ease;
}
.step-enter-from {
  opacity: 0;
  transform: translateY(14px);
}
.step-leave-to {
  opacity: 0;
  transform: translateY(-14px);
}

@keyframes slide-up {
  from {
    opacity: 0;
    transform: translateY(20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}
@keyframes float {
  0%,
  100% {
    transform: translateY(0);
  }
  50% {
    transform: translateY(-8px);
  }
}
@keyframes pulse-glow {
  0%,
  100% {
    opacity: 0.75;
    transform: scale(1);
  }
  50% {
    opacity: 1;
    transform: scale(1.06);
  }
}
@keyframes drift {
  0%,
  100% {
    transform: translate(0, 0) scale(1);
  }
  50% {
    transform: translate(28px, 20px) scale(1.08);
  }
}
@keyframes fade-in {
  from {
    opacity: 0;
  }
  to {
    opacity: 1;
  }
}

@media (prefers-reduced-motion: reduce) {
  .brand-content,
  .form-card,
  .logo-tile,
  .illustration,
  .illustration-frame::before,
  .brand-panel .circle,
  .step-enter-active,
  .step-leave-active {
    animation: none;
    transition: none;
  }
}

/* ---------- Responsive: stack on mobile, hide brand panel ---------- */
@media (max-width: 768px) {
  .forgot-layout {
    flex-direction: column;
  }
  .brand-panel,
  .wave-divider {
    display: none;
  }
  .form-panel {
    width: 100%;
    min-height: 100vh;
  }
}
</style>
