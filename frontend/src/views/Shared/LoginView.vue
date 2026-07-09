<script setup>
// This is the sign-in page (the first screen users see to enter the system).
// The user types their email and password, and we check them with the server.
// If the details are correct, we send the user to their own dashboard.
// If they are wrong, we show a friendly error message and let them try again.
// The page also supports two languages (English and Arabic).
import { ref, computed } from 'vue'
import { useRouter, onBeforeRouteLeave } from 'vue-router'
import { useAuthStore } from '../../stores/auth'
import { useLanguageStore } from '../../stores/language'
import { dashboardFor } from '../../router/roleRoutes'
import LanguageToggle from '../../components/LanguageToggle.vue'
import schoolLogo from '../../assets/school-logo.jpg.jpg'
import loginIllustration from '../../assets/login-illustration.svg.svg'

const router = useRouter()
const auth = useAuthStore()
const language = useLanguageStore()

const email = ref('')
const password = ref('')
const errorMessage = ref('')
const loading = ref(false)
const showPassword = ref(false) // toggles the password field visibility

// Right-panel view switch: false = Sign In form, true = Contact-school view.
// Purely reactive — swaps the panel content in place, no routing/navigation.
const showSignUp = ref(false)

// Template ref to the password <input>, so we can clear the NATIVE value
// synchronously when navigating away (Vue's v-model update alone is async).
const passwordInput = ref(null)

// Set true only when a real login succeeds. Used so we DON'T wipe the password
// on the success navigation (letting the browser still offer to save a real,
// working login) while we DO wipe it for any other exit.
const loginSucceeded = ref(false)

// Empties the password from both Vue state and the actual DOM input. With no
// password left in the field at navigation time, the browser has nothing to
// offer saving — so no "Save/Update password?" prompt for abandoned/failed tries.
function clearPassword() {
  password.value = ''
  if (passwordInput.value) passwordInput.value.value = ''
}

// Runs whenever we leave the login route (clicking Sign Up, Back, etc.). Unless
// a genuine login just succeeded, clear the password so the browser won't prompt.
onBeforeRouteLeave(() => {
  if (!loginSucceeded.value) clearPassword()
})

// ---- Bilingual labels ----
const content = {
  en: {
    brandHeading: 'Welcome to Almanahil Libyan School',
    brandSubtitle: 'Your school, all in one place.',
    highlights: [
      'Access your personal dashboard',
      'Track grades and attendance',
      'Stay connected with the school'
    ],
    welcomeBack: 'Welcome back',
    schoolName: 'Sign In',
    signInTo: 'Enter your details to continue',
    emailLabel: 'Email Address',
    emailPlaceholder: 'Enter your email',
    passwordLabel: 'Password',
    passwordPlaceholder: 'Enter your password',
    forgotPassword: 'Forgot password?',
    loginBtn: 'Sign In',
    loggingIn: 'Signing in…',
    secureNote: 'Secure sign in',
    back: 'Back',
    noAccount: "Don't have an account?",
    signUp: 'Sign Up',
    // Contact-school view (in-page swap of the right panel)
    contactEyebrow: 'Account Access',
    needAccount: 'Need an Account?',
    contactMessage: 'Accounts are created by the school administration. Please contact us to request an account.',
    haveAccount: 'Already have an account?',
    signIn: 'Sign In',
    contactEmail: 'it@almanahilschool.com',
    contactPhone: '0176073805',
    contactAddress: "Jalan Sejahtera 15, Taman Desa Skudai, 81300 Skudai, Johor",
    invalidEmail: 'Please enter a valid email address (e.g. name@example.com).',
    genericError: 'Invalid email or password. Please try again.'
  },
  ar: {
    brandHeading: 'مرحباً بكم في مدرسة المناهل الزاخرة (أويا)',
    brandSubtitle: 'مدرستك، كل شيء في مكان واحد.',
    highlights: [
      'الوصول إلى لوحة التحكم الخاصة بك',
      'متابعة الدرجات والحضور',
      'البقاء على تواصل مع المدرسة'
    ],
    welcomeBack: 'مرحباً بعودتك',
    schoolName: 'تسجيل الدخول',
    signInTo: 'أدخل بياناتك للمتابعة',
    emailLabel: 'البريد الإلكتروني',
    emailPlaceholder: 'أدخل بريدك الإلكتروني',
    passwordLabel: 'كلمة المرور',
    passwordPlaceholder: 'أدخل كلمة المرور',
    forgotPassword: 'نسيت كلمة المرور؟',
    loginBtn: 'تسجيل الدخول',
    loggingIn: 'جاري تسجيل الدخول…',
    secureNote: 'تسجيل دخول آمن',
    back: 'رجوع',
    noAccount: 'ليس لديك حساب؟',
    signUp: 'إنشاء حساب',
    // Contact-school view
    contactEyebrow: 'الوصول إلى الحساب',
    needAccount: 'تحتاج إلى حساب؟',
    contactMessage: 'يتم إنشاء الحسابات من قبل إدارة المدرسة. يرجى التواصل معنا لطلب حساب.',
    haveAccount: 'لديك حساب بالفعل؟',
    signIn: 'تسجيل الدخول',
    contactEmail: 'it@almanahilschool.com',
    contactPhone: '0176073805',
    contactAddress: "Jalan Sejahtera 15, Taman Desa Skudai, 81300 Skudai, Johor",
    invalidEmail: 'يرجى إدخال بريد إلكتروني صحيح (مثال: name@example.com).',
    genericError: 'البريد الإلكتروني أو كلمة المرور غير صحيحة. حاول مرة أخرى.'
  }
}

const t = computed(() => content[language.lang])

// Accepts a standard email shape: text@text.tld (tld of 2+ letters).
const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[a-zA-Z]{2,}$/

// Handles the login form submit: authenticate, then redirect by role.
// This function runs when the user clicks the "Sign In" button.
async function handleLogin() {
  // Clear any old error message before we start.
  errorMessage.value = ''

  // Step 1: Check the email looks correct (like name@example.com) before doing anything.
  // Block submission early if the email isn't a valid address.
  if (!EMAIL_PATTERN.test(email.value.trim())) {
    errorMessage.value = t.value.invalidEmail
    return
  }

  // Show the "Signing in..." state so the user knows we are working.
  loading.value = true

  try {
    // Step 2: Send the email and password to the auth store, which asks the server to log in.
    const user = await auth.login(email.value, password.value)
    // Step 3: Success — the server said the details are correct. Send the user to their dashboard.
    // SUCCESS ONLY: a valid token was received. Mark success so the route-leave
    // guard keeps the (correct) credentials in place — this lets the browser
    // still offer to save a *real*, working login as we navigate to the dashboard.
    loginSucceeded.value = true
    router.push(dashboardFor(user.role))
  } catch (err) {
    // Step 4: Failure — the email or password was wrong. Show a friendly error message.
    // Show a friendly message only — never the raw error.
    // Prefer the backend message, otherwise a localized fallback.
    errorMessage.value = err?.response?.data?.message || t.value.genericError

    // FAILED ATTEMPT: wipe the rejected password immediately so nothing lingers
    // for the browser to offer saving (works whether they retry or navigate away).
    clearPassword()
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="login-layout" :dir="language.dir" :lang="language.lang">
    <!-- Language toggle (top-right) -->
    <div class="toggle-holder">
      <LanguageToggle />
    </div>

    <!-- Back to the welcome page -->
    <router-link to="/welcome" class="back-link">
      <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
        <path class="arrow-ltr" d="m15 5-7 7 7 7" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
      </svg>
      <span>{{ t.back }}</span>
    </router-link>

    <!-- LEFT: branded illustration panel (hidden on mobile) -->
    <aside class="brand-panel">
      <span class="circle" aria-hidden="true"></span>
      <div class="brand-content">
        <h2 class="brand-heading">{{ t.brandHeading }}</h2>
        <div class="illustration-frame">
          <img :src="loginIllustration" alt="" class="illustration" />
        </div>
        <p class="brand-subtitle">{{ t.brandSubtitle }}</p>
        <ul class="highlights">
          <li v-for="(item, i) in t.highlights" :key="i">
            <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
              <circle cx="12" cy="12" r="11" fill="#f2a03d" />
              <path d="m7.5 12 3 3 6-6.5" stroke="#fff" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round" />
            </svg>
            <span>{{ item }}</span>
          </li>
        </ul>
      </div>
    </aside>

    <!-- Curved wave boundary between the two panels -->
    <div class="wave-divider" aria-hidden="true">
      <svg viewBox="0 0 80 1000" preserveAspectRatio="none">
        <defs>
          <linearGradient id="waveGrad" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0" stop-color="#2f63ba" />
            <stop offset="0.45" stop-color="#1e4c9a" />
            <stop offset="1" stop-color="#163a78" />
          </linearGradient>
        </defs>
        <path
          d="M0,0 L40,0 C72,110 8,210 40,320 C72,430 8,540 40,650 C72,760 8,870 40,1000 L0,1000 Z"
          fill="url(#waveGrad)"
        />
      </svg>
    </div>

    <!-- RIGHT: login form panel -->
    <main class="form-panel">
      <div class="form-card">
        <div class="logo-tile">
          <img :src="schoolLogo" alt="" class="logo" />
        </div>

        <!-- Right-panel views swap in place with a smooth fade/slide -->
        <Transition name="swap" mode="out-in">
        <!-- ============ SIGN IN VIEW (default) ============ -->
        <div v-if="!showSignUp" key="signin" class="panel-view">
        <span class="form-eyebrow">{{ t.welcomeBack }}</span>
        <h1 class="school-name">{{ t.schoolName }}</h1>
        <p class="subtitle">{{ t.signInTo }}</p>

        <form @submit.prevent="handleLogin" class="login-form">
          <!-- EMAIL -->
          <label class="field field-1" for="login-email">
            <span class="field-label">{{ t.emailLabel }}</span>
            <div class="input-wrap">
              <span class="input-icon">
                <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
                  <rect x="3" y="5" width="18" height="14" rx="2" stroke="currentColor" stroke-width="1.8" />
                  <path d="m4 7 8 6 8-6" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" />
                </svg>
              </span>
              <input
                id="login-email"
                name="email"
                v-model="email"
                type="email"
                :placeholder="t.emailPlaceholder"
                required
                autocomplete="email"
                pattern="[^\s@]+@[^\s@]+\.[a-zA-Z]{2,}"
                :title="t.invalidEmail"
              />
            </div>
          </label>

          <!-- PASSWORD -->
          <label class="field field-2" for="login-password">
            <span class="field-label">{{ t.passwordLabel }}</span>
            <div class="input-wrap">
              <span class="input-icon">
                <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
                  <rect x="5" y="10" width="14" height="10" rx="2" stroke="currentColor" stroke-width="1.8" />
                  <path d="M8 10V7a4 4 0 0 1 8 0v3" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" />
                </svg>
              </span>
              <input
                ref="passwordInput"
                id="login-password"
                name="password"
                v-model="password"
                :type="showPassword ? 'text' : 'password'"
                :placeholder="t.passwordPlaceholder"
                required
                autocomplete="current-password"
              />
              <!-- Show/hide toggle -->
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
          </label>

          <!-- Forgot password link → dedicated reset flow -->
          <router-link to="/forgot-password" class="forgot-link">{{ t.forgotPassword }}</router-link>

          <!-- Friendly error message -->
          <p v-if="errorMessage" class="error">{{ errorMessage }}</p>

          <button type="submit" class="login-btn" :disabled="loading">
            {{ loading ? t.loggingIn : t.loginBtn }}
          </button>

          <!-- Small trust line -->
          <p class="secure-note">
            <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
              <rect x="5" y="11" width="14" height="9" rx="2" stroke="currentColor" stroke-width="1.7" />
              <path d="M8 11V8a4 4 0 0 1 8 0v3" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" />
            </svg>
            <span>{{ t.secureNote }}</span>
          </p>
        </form>

        <!-- Swap to the Contact-school view in place (no navigation) -->
        <p class="signup-hint">
          {{ t.noAccount }}
          <button type="button" class="link-btn" @click="showSignUp = true">{{ t.signUp }}</button>
        </p>
        </div>

        <!-- ============ CONTACT SCHOOL VIEW ============ -->
        <div v-else key="contact" class="panel-view">
          <span class="form-eyebrow">{{ t.contactEyebrow }}</span>
          <h1 class="school-name">{{ t.needAccount }}</h1>
          <p class="subtitle contact-message">{{ t.contactMessage }}</p>

          <ul class="contact-methods">
            <!-- Email (mailto link) -->
            <li>
              <a class="c-row" :href="`mailto:${t.contactEmail}`">
                <span class="c-icon">
                  <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
                    <rect x="3" y="5" width="18" height="14" rx="2" stroke="currentColor" stroke-width="1.8" />
                    <path d="m4 7 8 6 8-6" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" />
                  </svg>
                </span>
                <span dir="ltr">{{ t.contactEmail }}</span>
              </a>
            </li>
            <!-- Phone (tel link) -->
            <li>
              <a class="c-row" :href="`tel:${t.contactPhone}`">
                <span class="c-icon">
                  <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
                    <path d="M5 4h3l1.5 4-2 1.5a12 12 0 0 0 5 5l1.5-2 4 1.5V19a1 1 0 0 1-1 1A15 15 0 0 1 4 6a1 1 0 0 1 1-2Z" stroke="currentColor" stroke-width="1.8" stroke-linejoin="round" />
                  </svg>
                </span>
                <span dir="ltr">{{ t.contactPhone }}</span>
              </a>
            </li>
            <!-- Address (opens the location on Google Maps) -->
            <li>
              <a
                class="c-row"
                href="https://maps.app.goo.gl/z2QTMy4hdRHkg4dR9"
                target="_blank"
                rel="noopener"
              >
                <span class="c-icon">
                  <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
                    <path d="M12 21s7-5.5 7-11a7 7 0 1 0-14 0c0 5.5 7 11 7 11Z" stroke="currentColor" stroke-width="1.8" stroke-linejoin="round" />
                    <circle cx="12" cy="10" r="2.5" stroke="currentColor" stroke-width="1.8" />
                  </svg>
                </span>
                <span dir="ltr">{{ t.contactAddress }}</span>
              </a>
            </li>
          </ul>

          <!-- Swap back to the Sign In form (no navigation) -->
          <p class="signup-hint">
            {{ t.haveAccount }}
            <button type="button" class="link-btn" @click="showSignUp = false">{{ t.signIn }}</button>
          </p>
        </div>
        </Transition>
      </div>
    </main>
  </div>
</template>

<style scoped>
/* Scoped reset so the layout truly fills the viewport with no gaps */
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

.login-layout {
  --navy: var(--ds-navy);
  --navy-dark: var(--ds-navy-dark);
  --orange: var(--ds-orange);

  position: relative;
  display: flex;
  width: 100%;
  height: 100vh;
  margin: 0;
  overflow: hidden;
  font-family: 'Segoe UI', system-ui, -apple-system, sans-serif;
}
.login-layout[lang='ar'] {
  font-family: 'Segoe UI', 'Tahoma', system-ui, sans-serif;
}

/* ---------- Language toggle holder (always top-right) ---------- */
/* Physical right/left so it stays put regardless of language */
.toggle-holder {
  position: absolute;
  top: 1.25rem;
  right: 1.25rem;
  z-index: 12;
}

/* ---------- Back link (always top-left) ---------- */
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
  /* Navy chip so it stays readable over both the navy panel and white panel */
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
  /* Pure VERTICAL gradient so every height is one solid color across the full
     width. This lets the wave divider (also a vertical gradient with the same
     stops) line up seamlessly at the panel edge with no visible seam. */
  background:
    radial-gradient(circle at 22% 24%, rgba(255, 255, 255, 0.12), transparent 40%),
    radial-gradient(circle at 50% 82%, rgba(242, 160, 61, 0.16), transparent 48%),
    linear-gradient(180deg, #2f63ba 0%, var(--navy) 45%, var(--navy-dark) 100%);
}
/* Faded dotted texture for depth */
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
  max-width: 460px;
  color: #fff;
  animation: fade-in 0.8s ease-out both;
}
.brand-heading {
  font-size: 1.95rem;
  font-weight: 800;
  line-height: 1.22;
  letter-spacing: -0.01em;
  color: #fff;
  margin: 0 0 1.5rem;
  text-shadow: 0 2px 12px rgba(0, 0, 0, 0.18);
}
/* Holds the illustration over a soft spotlight so it reads as a hero graphic */
.illustration-frame {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  width: min(100%, 340px);
  margin: 0 auto;
}
/* Warm radial spotlight glow behind the artwork */
.illustration-frame::before {
  content: '';
  position: absolute;
  inset: -14% -6%;
  z-index: 0;
  border-radius: 50%;
  background: radial-gradient(
    circle,
    rgba(242, 160, 61, 0.28),
    rgba(124, 175, 255, 0.14) 45%,
    transparent 70%
  );
  filter: blur(10px);
  animation: pulse-glow 6s ease-in-out infinite;
}
.illustration {
  position: relative;
  z-index: 1;
  width: 100%;
  max-width: 320px;
  height: auto;
  display: block;
  margin: 0 auto;
  /* soft drop shadow gives the illustration a lift off the panel */
  filter: drop-shadow(0 16px 30px rgba(0, 0, 0, 0.32));
  animation: float 5s ease-in-out infinite;
}
.brand-subtitle {
  margin: 1.5rem 0 0;
  font-size: 1.08rem;
  color: rgba(255, 255, 255, 0.9);
}
/* Feature highlights under the subtitle */
.highlights {
  list-style: none;
  margin: 1.5rem 0 0;
  padding: 1.5rem 0 0;
  border-top: 1px solid rgba(255, 255, 255, 0.18);
  display: inline-flex;
  flex-direction: column;
  gap: 0.85rem;
  text-align: start;
}
.highlights li {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  font-size: 0.98rem;
  color: rgba(255, 255, 255, 0.92);
}
.highlights svg {
  width: 22px;
  height: 22px;
  flex-shrink: 0;
}
/* ---------- RIGHT form panel ---------- */
.form-panel {
  position: relative;
  width: 52%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 2rem;
  overflow: hidden;
  /* Soft tinted backdrop so the white card stands out */
  background: linear-gradient(160deg, #ffffff 0%, #eef3fb 100%);
}
/* Decorative soft glows in the form panel */
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
  max-width: 390px;
  text-align: center;
  background: #fff;
  border: 1px solid #eef2f8;
  border-radius: 22px;
  /* extra top padding so the popped-up logo has room */
  padding: 4.25rem 2.25rem 2.5rem;
  margin-top: 3rem; /* leave space above for the floating logo */
  box-shadow: 0 24px 60px rgba(30, 76, 154, 0.18);
  animation: slide-up 0.5s ease-out both;
}
/* Accent bar along the top of the card (rounded to match corners) */
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
/* Logo pops up above the top edge of the card */
.logo-tile {
  position: absolute;
  top: -52px; /* half the tile height, so it straddles the card edge */
  left: 50%;
  margin-left: -52px; /* center horizontally (transform stays free for the float) */
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
  /* gentle up/down float; box-shadow + border are free to animate on hover */
  animation: float 3.5s ease-in-out infinite;
  transition: box-shadow 0.3s ease, border-color 0.3s ease;
}
/* Highlight the logo on hover */
.logo-tile:hover {
  border-color: var(--orange);
  box-shadow: 0 16px 36px rgba(242, 160, 61, 0.5);
}
.logo {
  width: 78px;
  height: 78px;
  object-fit: contain;
  border-radius: 14px;
  /* scaled on hover (independent of the tile's animated transform) */
  transition: transform 0.3s ease;
}
.logo-tile:hover .logo {
  transform: scale(1.1);
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
.school-name {
  font-size: 1.7rem;
  font-weight: 800;
  letter-spacing: -0.01em;
  color: var(--navy);
  margin: 0 0 0.3rem;
}
.subtitle {
  color: #6b7280;
  margin: 0 0 1.75rem;
  font-size: 0.95rem;
}
.login-form {
  display: flex;
  flex-direction: column;
  gap: 1.1rem;
  text-align: start;
}
/* Small, subtle 'Forgot password?' link sitting at the inline-end under the
   password field (flips correctly in RTL via flex-end on the inline axis). */
.forgot-link {
  align-self: flex-end;
  margin-top: -0.55rem;
  font-size: 0.83rem;
  font-weight: 600;
  color: var(--navy);
  text-decoration: none;
  transition: color 0.2s ease;
}
.forgot-link:hover {
  color: var(--orange);
  text-decoration: underline;
}
.field {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  /* Each field eases in with a small upward motion, staggered below */
  animation: field-in 0.5s ease-out both;
}
.field-1 {
  animation-delay: 0.15s;
}
.field-2 {
  animation-delay: 0.28s;
}
.field-label {
  font-size: 0.74rem;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--navy);
}

/* Grouped input: icon badge | divider | input | eye */
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
/* Icon sits in its own badge area with a divider line */
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
  transition: transform 0.3s ease;
}
/* Icon gives a subtle pop when its field is focused */
.input-wrap:focus-within .input-icon svg {
  transform: scale(1.12);
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

/* Show/hide eye button */
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

/* ---------- Curved wave boundary between the panels ---------- */
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
/* Mirror it to the navy side when the layout is RTL */
.login-layout[dir='rtl'] .wave-divider {
  left: auto;
  right: 48%;
  transform: scaleX(-1);
}
/* Hide the divider when the panels stack on mobile */
@media (max-width: 768px) {
  .wave-divider {
    display: none;
  }
}

.error {
  color: #dc2626;
  background: #fef2f2;
  border: 1px solid #fecaca;
  border-radius: 10px;
  padding: 0.65rem 0.8rem;
  margin: 0;
  font-size: 0.88rem;
  animation: fade-in 0.3s ease-out both;
}

.login-btn {
  position: relative;
  overflow: hidden; /* clip the sheen sweep */
  margin-top: 0.6rem;
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
/* Light sheen that sweeps across on hover */
.login-btn::after {
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
.login-btn:hover:not(:disabled)::after {
  inset-inline-start: 125%;
}
.login-btn:hover:not(:disabled) {
  transform: translateY(-2px);
  filter: brightness(1.07);
  box-shadow: 0 14px 30px rgba(30, 76, 154, 0.4);
}
.login-btn:active:not(:disabled) {
  transform: translateY(0);
}
.login-btn:disabled {
  background: #9db8de;
  box-shadow: none;
  cursor: not-allowed;
}

/* Small "secure sign in" trust line under the button */
.secure-note {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.35rem;
  margin: 0.9rem 0 0;
  font-size: 0.8rem;
  color: #9ca3af;
}
.secure-note svg {
  width: 15px;
  height: 15px;
}

/* Sign-up hint below the form */
.signup-hint {
  margin: 1.5rem 0 0;
  padding-top: 1.25rem;
  border-top: 1px solid #eef2f8;
  font-size: 0.9rem;
  color: #6b7280;
}
/* Inline text link rendered as a <button> (swaps the panel view, no navigation) */
.link-btn {
  background: none;
  border: none;
  padding: 0;
  font: inherit;
  cursor: pointer;
  color: var(--navy);
  font-weight: 700;
  text-decoration: none;
  margin-inline-start: 0.25rem;
  transition: color 0.2s ease;
}
.link-btn:hover {
  color: var(--orange);
  text-decoration: underline;
}

/* ---------- Contact-school view ---------- */
.contact-methods {
  list-style: none;
  margin: 0.25rem 0 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  text-align: start;
}
/* Each contact method, styled to match the sign-in inputs */
.c-row {
  display: flex;
  align-items: center;
  gap: 0.8rem;
  padding: 0.75rem 0.9rem;
  background: #f4f7fc;
  border: 1.5px solid #e6ebf4;
  border-radius: 14px;
  color: #374151;
  font-size: 0.92rem;
  font-weight: 600;
  text-decoration: none;
  transition: border-color 0.25s ease, box-shadow 0.25s ease, transform 0.2s ease;
}
/* Only the clickable rows (email/phone) lift + highlight on hover */
a.c-row:hover {
  border-color: var(--orange);
  box-shadow: 0 6px 16px rgba(30, 76, 154, 0.1);
  transform: translateY(-1px);
}
/* Navy gradient icon badge with a white glyph (matches the Sign In button) */
.c-icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 40px;
  height: 40px;
  flex-shrink: 0;
  border-radius: 11px;
  background: linear-gradient(135deg, var(--navy), #2f63ba);
  color: #fff;
  box-shadow: 0 5px 12px rgba(30, 76, 154, 0.28);
}
.c-icon svg {
  width: 18px;
  height: 18px;
}
/* Slightly narrower, centered contact message for balance */
.contact-message {
  max-width: 320px;
  margin-inline: auto;
  line-height: 1.55;
}

/* ---------- Smooth swap between Sign In and Contact views ---------- */
.swap-enter-active,
.swap-leave-active {
  transition: opacity 0.3s ease, transform 0.3s ease;
}
.swap-enter-from {
  opacity: 0;
  transform: translateY(12px);
}
.swap-leave-to {
  opacity: 0;
  transform: translateY(-12px);
}

/* ---------- Animations ---------- */
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
/* Gentle breathing glow behind the illustration */
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
@keyframes field-in {
  from {
    opacity: 0;
    transform: translateY(10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@media (prefers-reduced-motion: reduce) {
  .brand-content,
  .form-card,
  .logo-tile,
  .illustration,
  .illustration-frame::before,
  .brand-panel .circle,
  .wave-divider,
  .wave-divider::before,
  .field,
  .error,
  .login-btn,
  .swap-enter-active,
  .swap-leave-active {
    animation: none;
    transition: none;
  }
}

/* ---------- Responsive: stack on mobile, hide illustration panel ---------- */
@media (max-width: 768px) {
  .login-layout {
    flex-direction: column;
  }
  .brand-panel {
    display: none;
  }
  .form-panel {
    width: 100%;
    min-height: 100vh;
  }
}
</style>
