<script setup>
import { computed } from 'vue'
import { useLanguageStore } from '../../stores/language'
import LanguageToggle from '../../components/LanguageToggle.vue'

const language = useLanguageStore()

// ---- Bilingual content ----
const content = {
  en: {
    back: 'Back',
    eyebrow: 'Account Access',
    heading: 'Getting Your Account',
    intro: 'If you are a student or a parent of a student, you can contact the school to get your login details.',
    contactTitle: 'Contact the school',
    phone1: '0176073805',
    phone2: '0184006808',
    email: 'info@almanahilschool.com',
    backToLogin: 'Back to Sign In'
  },
  ar: {
    back: 'رجوع',
    eyebrow: 'الوصول للحساب',
    heading: 'كيفية الحصول على حسابك',
    intro: 'إذا كنت طالباً أو ولي أمر طالب، يمكنك التواصل مع المدرسة للحصول على بيانات الدخول.',
    contactTitle: 'تواصل مع المدرسة',
    phone1: '0176073805',
    phone2: '0184006808',
    email: 'info@almanahilschool.com',
    backToLogin: 'العودة لتسجيل الدخول'
  }
}

const t = computed(() => content[language.lang])
</script>

<template>
  <div class="account-page" :dir="language.dir" :lang="language.lang">
    <span class="circle circle-1" aria-hidden="true"></span>
    <span class="circle circle-2" aria-hidden="true"></span>

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

    <div class="card">
      <!-- Icon -->
      <div class="icon-badge">
        <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
          <circle cx="9" cy="8" r="3.5" stroke="currentColor" stroke-width="1.8" />
          <path d="M3.5 19a5.5 5.5 0 0 1 11 0" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" />
          <path d="M18 8v6M15 11h6" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" />
        </svg>
      </div>

      <span class="eyebrow">{{ t.eyebrow }}</span>
      <h1 class="heading">{{ t.heading }}</h1>
      <p class="intro">{{ t.intro }}</p>

      <div class="contact-box">
        <h2 class="contact-title">{{ t.contactTitle }}</h2>
        <div class="contact-list">
          <a class="contact-chip" :href="`tel:${t.phone1}`">
            <span class="chip-icon">
              <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
                <path d="M5 4h3l1.5 4-2 1.5a12 12 0 0 0 5 5l1.5-2 4 1.5V19a1 1 0 0 1-1 1A15 15 0 0 1 4 6a1 1 0 0 1 1-2Z" stroke="currentColor" stroke-width="1.8" stroke-linejoin="round" />
              </svg>
            </span>
            <span dir="ltr">{{ t.phone1 }}</span>
          </a>
          <a class="contact-chip" :href="`tel:${t.phone2}`">
            <span class="chip-icon">
              <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
                <path d="M5 4h3l1.5 4-2 1.5a12 12 0 0 0 5 5l1.5-2 4 1.5V19a1 1 0 0 1-1 1A15 15 0 0 1 4 6a1 1 0 0 1 1-2Z" stroke="currentColor" stroke-width="1.8" stroke-linejoin="round" />
              </svg>
            </span>
            <span dir="ltr">{{ t.phone2 }}</span>
          </a>
          <a class="contact-chip" :href="`mailto:${t.email}`">
            <span class="chip-icon">
              <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
                <rect x="3" y="5" width="18" height="14" rx="2" stroke="currentColor" stroke-width="1.8" />
                <path d="m4 7 8 6 8-6" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" />
              </svg>
            </span>
            <span dir="ltr">{{ t.email }}</span>
          </a>
        </div>
      </div>

      <div class="actions">
        <router-link class="btn primary" to="/login">
          <svg class="btn-arrow" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <path d="m15 5-7 7 7 7" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
          </svg>
          <span>{{ t.backToLogin }}</span>
        </router-link>
      </div>
    </div>
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

.account-page {
  --navy: var(--ds-navy);
  --navy-dark: var(--ds-navy-dark);
  --orange: var(--ds-orange);

  position: relative;
  min-height: 100vh;
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 2rem 1.25rem;
  overflow: hidden;
  font-family: 'Segoe UI', system-ui, -apple-system, sans-serif;
  background:
    radial-gradient(circle at 18% 25%, rgba(255, 255, 255, 0.12), transparent 42%),
    radial-gradient(circle at 85% 80%, rgba(242, 160, 61, 0.2), transparent 46%),
    linear-gradient(160deg, #2f63ba 0%, var(--navy) 45%, var(--navy-dark) 100%);
}
.account-page[lang='ar'] {
  font-family: 'Segoe UI', 'Tahoma', system-ui, sans-serif;
}

/* Drifting background circles */
.circle {
  position: absolute;
  border-radius: 50%;
  filter: blur(4px);
}
.circle-1 {
  width: 420px;
  height: 420px;
  top: -140px;
  inset-inline-end: -130px;
  background: rgba(242, 160, 61, 0.16);
  animation: drift 18s ease-in-out infinite;
}
.circle-2 {
  width: 300px;
  height: 300px;
  bottom: -110px;
  inset-inline-start: -90px;
  background: rgba(255, 255, 255, 0.08);
  animation: drift 22s ease-in-out infinite reverse;
}

/* Top corner controls — fixed physical sides so they don't move per language */
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
  background: rgba(255, 255, 255, 0.15);
  transition: background 0.25s ease;
}
.back-link:hover {
  background: rgba(255, 255, 255, 0.28);
}
.back-link svg {
  width: 18px;
  height: 18px;
}

/* Card */
.card {
  position: relative;
  z-index: 1;
  width: 100%;
  max-width: 500px;
  background: #fff;
  border: 1px solid #eef2f8;
  border-radius: 24px;
  padding: 3.75rem 2.5rem 2.5rem;
  margin-top: 2.5rem;
  text-align: center;
  box-shadow: 0 26px 64px rgba(0, 0, 0, 0.32);
  animation: slide-up 0.5s ease-out both;
}
/* Accent bar along the top of the card (rounded to match the card corners) */
.card::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 6px;
  border-radius: 24px 24px 0 0;
  background: linear-gradient(90deg, var(--navy), var(--orange));
}
.icon-badge {
  position: absolute;
  top: -42px;
  left: 50%;
  margin-left: -42px;
  width: 84px;
  height: 84px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 24px;
  color: #fff;
  background: linear-gradient(135deg, var(--navy), #2f63ba);
  box-shadow: 0 12px 26px rgba(30, 76, 154, 0.4);
  border: 5px solid #fff;
}
.icon-badge svg {
  width: 40px;
  height: 40px;
}
.eyebrow {
  display: block;
  font-size: 0.74rem;
  font-weight: 700;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: var(--orange);
  margin-bottom: 0.4rem;
}
.heading {
  font-size: 1.7rem;
  font-weight: 800;
  color: var(--navy);
  margin: 0 0 0.6rem;
}
.intro {
  font-size: 1rem;
  color: #6b7280;
  margin: 0 auto 1.75rem;
  max-width: 380px;
  line-height: 1.65;
}

/* Contact box */
.contact-box {
  background: #f4f7fc;
  border: 1px solid #e2e8f4;
  border-radius: 16px;
  padding: 1.25rem;
  margin-bottom: 1.75rem;
}
.contact-title {
  font-size: 0.74rem;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--navy);
  margin: 0 0 0.9rem;
  text-align: center;
}
.contact-list {
  display: flex;
  flex-direction: column;
  gap: 0.6rem;
}
/* Each contact method as a clean chip */
.contact-chip {
  display: flex;
  align-items: center;
  gap: 0.8rem;
  padding: 0.7rem 0.9rem;
  background: #fff;
  border: 1px solid #e6ebf4;
  border-radius: 12px;
  color: #374151;
  font-size: 0.96rem;
  font-weight: 600;
  text-decoration: none;
  transition: transform 0.2s ease, border-color 0.2s ease, box-shadow 0.2s ease;
}
.contact-chip:hover {
  transform: translateY(-2px);
  border-color: var(--orange);
  box-shadow: 0 8px 18px rgba(30, 76, 154, 0.1);
}
.chip-icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  flex-shrink: 0;
  border-radius: 10px;
  background: rgba(30, 76, 154, 0.1);
  color: var(--navy);
}
.chip-icon svg {
  width: 19px;
  height: 19px;
}

/* Actions */
.actions {
  display: flex;
  justify-content: center;
}
.btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 0.85rem 2rem;
  border-radius: 12px;
  font-size: 0.98rem;
  font-weight: 700;
  text-decoration: none;
  cursor: pointer;
  transition: transform 0.2s ease, filter 0.25s ease, box-shadow 0.25s ease;
}
.btn.primary {
  background: linear-gradient(135deg, var(--navy), #2f63ba);
  color: #fff;
  box-shadow: 0 10px 24px rgba(30, 76, 154, 0.35);
}
.btn.primary:hover {
  transform: translateY(-2px);
  filter: brightness(1.07);
  box-shadow: 0 14px 30px rgba(30, 76, 154, 0.45);
}
.btn-arrow {
  width: 18px;
  height: 18px;
  transition: transform 0.25s ease;
}
.btn.primary:hover .btn-arrow {
  transform: translateX(-3px);
}
.account-page[dir='rtl'] .btn-arrow {
  transform: scaleX(-1);
}
.account-page[dir='rtl'] .btn.primary:hover .btn-arrow {
  transform: scaleX(-1) translateX(-3px);
}

/* Animations */
@keyframes slide-up {
  from {
    opacity: 0;
    transform: translateY(22px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}
@keyframes drift {
  0%,
  100% {
    transform: translate(0, 0) scale(1);
  }
  50% {
    transform: translate(26px, 18px) scale(1.08);
  }
}

@media (prefers-reduced-motion: reduce) {
  .card,
  .circle,
  .btn {
    animation: none;
    transition: none;
  }
}
</style>
