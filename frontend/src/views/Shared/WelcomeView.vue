<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { useLanguageStore } from '../../stores/language'
import LanguageToggle from '../../components/LanguageToggle.vue'
import schoolLogo from '../../assets/school-logo.jpg.jpg'
import schoolHero from '../../assets/school-hero.jpg'
import featureEducation from '../../assets/gallery-2.jpg'
import featureSafe from '../../assets/gallery-1.jpg'
import featureActivities from '../../assets/gallery-4.jpg'

const router = useRouter()
const language = useLanguageStore()

// Photos shown at the top of each feature card (language-independent, by index):
// [0] Excellent Education, [1] Safe Environment, [2] Diverse Activities.
const featurePhotos = [featureEducation, featureSafe, featureActivities]

// Navigate to the login page.
function goToLogin() {
  router.push('/login')
}

// Smoothly scroll back to the top (used by the navbar logo + name).
function goToTop() {
  window.scrollTo({ top: 0, behavior: 'smooth' })
}

// ---- Sticky navbar turns solid after scrolling a little ----
const scrolled = ref(false)
function onScroll() {
  scrolled.value = window.scrollY > 40
}
onMounted(() => window.addEventListener('scroll', onScroll, { passive: true }))
onUnmounted(() => window.removeEventListener('scroll', onScroll))

// ---- Bilingual content ----
const content = {
  en: {
    schoolName: 'Almanahil Libyan School',
    login: 'Sign In',
    hero: {
      heading: 'Welcome to Almanahil Libyan School',
      subtitle: 'We provide an ideal learning environment for your children.',
      cta: 'Login',
      greet: 'Welcome! 👋'
    },
    about: {
      eyebrow: 'Get to Know Us',
      heading: 'About Us',
      body: 'Almanahil Libyan School is dedicated to providing quality education in a safe and nurturing environment, helping every student reach their full potential.'
    },
    features: {
      eyebrow: 'What We Offer',
      heading: 'Our Features',
      items: [
        { title: 'Excellent Education', desc: 'Modern teaching methods to ensure student success.' },
        { title: 'Safe Environment', desc: 'A safe and comfortable space for all students.' },
        { title: 'Diverse Activities', desc: 'Educational and recreational activities to develop student skills.' }
      ]
    },
    contact: {
      eyebrow: 'Get in Touch',
      heading: 'Contact Us',
      address: "Jalan Sejahtera 15, Taman Desa Skudai, 81300 Skudai, Johor Darul Ta'zim",
      phone1: '0176073805',
      phone2: '0184006808',
      email: 'info@almanahilschool.com'
    },
    footer: '© 2026 Almanahil Libyan School. All rights reserved.'
  },
  ar: {
    schoolName: 'مدرسة المناهل الزاخرة (أويا)',
    login: 'تسجيل الدخول',
    hero: {
      heading: 'مرحباً بكم في مدرسة المناهل الزاخرة (أويا)',
      subtitle: 'نوفر بيئة تعليمية مثالية لأطفالكم.',
      cta: 'تسجيل الدخول',
      greet: '👋 أهلاً وسهلاً!'
    },
    about: {
      eyebrow: 'تعرّف علينا',
      heading: 'من نحن',
      body: 'مدرسة المناهل الزاخرة ملتزمة بتقديم تعليم عالي الجودة في بيئة آمنة ومحفزة، لمساعدة كل طالب على تحقيق أفضل إمكاناته.'
    },
    features: {
      eyebrow: 'ماذا نقدم',
      heading: 'مميزات مدرستنا',
      items: [
        { title: 'تعليم ممتاز', desc: 'نوفر أفضل وسائل التعليم الحديثة لضمان نجاح الطلاب.' },
        { title: 'بيئة تعليمية آمنة', desc: 'نضمن بيئة تعليمية آمنة ومريحة لكل الطلاب.' },
        { title: 'أنشطة متنوعة', desc: 'نقدم أنشطة تعليمية وترفيهية لتنمية مهارات الطلاب.' }
      ]
    },
    contact: {
      eyebrow: 'تواصل معنا',
      heading: 'اتصل بنا',
      address: "Jalan Sejahtera 15, Taman Desa Skudai, 81300 Skudai, Johor Darul Ta'zim",
      phone1: '0176073805',
      phone2: '0184006808',
      email: 'info@almanahilschool.com'
    },
    footer: '© 2026 مدرسة المناهل الزاخرة. جميع الحقوق محفوظة.'
  }
}

const t = computed(() => content[language.lang])

// ---- Scroll-reveal directive: fades/slides a section in when it enters view ----
const vReveal = {
  mounted(el) {
    el.classList.add('reveal')
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            entry.target.classList.add('reveal-visible')
            observer.unobserve(entry.target)
          }
        })
      },
      { threshold: 0.15 }
    )
    observer.observe(el)
  }
}

// ---- Staggered reveal: animates direct children in one-by-one when in view ----
const vStagger = {
  mounted(el) {
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            Array.from(entry.target.children).forEach((child, i) => {
              // Small incremental delay per child for the stagger effect.
              setTimeout(() => child.classList.add('card-visible'), i * 140)
            })
            observer.unobserve(entry.target)
          }
        })
      },
      { threshold: 0.2 }
    )
    observer.observe(el)
  }
}
</script>

<template>
  <div class="welcome" :dir="language.dir" :lang="language.lang">
    <!-- A) STICKY NAVBAR -->
    <header class="navbar" :class="{ solid: scrolled }">
      <div class="nav-brand" @click="goToTop" role="button" tabindex="0" @keydown.enter="goToTop">
        <img :src="schoolLogo" alt="" class="nav-logo" />
        <span class="nav-name">{{ t.schoolName }}</span>
      </div>
      <div class="nav-actions">
        <LanguageToggle />
        <button class="nav-login" @click="goToLogin">{{ t.login }}</button>
      </div>
    </header>

    <!-- B) HERO -->
    <section class="hero">
      <span class="circle circle-1" aria-hidden="true"></span>
      <span class="circle circle-2" aria-hidden="true"></span>
      <span class="circle circle-3" aria-hidden="true"></span>
      <span class="circle circle-4" aria-hidden="true"></span>

      <div class="hero-inner">
        <div class="hero-text">
          <div class="hero-logo-tile">
            <img :src="schoolLogo" alt="" class="hero-logo" />
          </div>
          <h1 class="hero-heading">{{ t.hero.heading }}</h1>
          <p class="hero-subtitle">{{ t.hero.subtitle }}</p>
        </div>
        <div class="hero-art">
          <div class="photo-frame">
            <img :src="schoolHero" alt="Almanahil Libyan School" class="hero-photo" />
          </div>
        </div>
      </div>

      <!-- Friendly waving mascot greeting visitors, in the empty bottom corner -->
      <div class="mascot" aria-hidden="true">
        <div class="speech-bubble">{{ t.hero.greet }}</div>
        <svg class="mascot-svg" viewBox="0 0 120 150" fill="none">
          <!-- ground shadow -->
          <ellipse cx="60" cy="142" rx="30" ry="6" fill="rgba(0,0,0,0.18)" />
          <!-- torso (shirt) -->
          <path d="M40 96c0-11 9-20 20-20s20 9 20 20v24H40V96Z" fill="#1e4c9a" />
          <!-- static arm -->
          <line x1="44" y1="92" x2="33" y2="116" stroke="#1e4c9a" stroke-width="11" stroke-linecap="round" />
          <circle cx="33" cy="118" r="6.5" fill="#f7c9a3" />
          <!-- head -->
          <circle cx="60" cy="50" r="26" fill="#f7c9a3" />
          <!-- hair -->
          <path d="M34 48a26 26 0 0 1 52 0c0-4-6-7-26-7s-26 3-26 7Z" fill="#3a2a1a" />
          <!-- eyes -->
          <circle cx="51" cy="50" r="2.6" fill="#2a2a2a" />
          <circle cx="69" cy="50" r="2.6" fill="#2a2a2a" />
          <!-- smile -->
          <path d="M52 60c3 3.5 13 3.5 16 0" stroke="#2a2a2a" stroke-width="2.4" stroke-linecap="round" />
          <!-- waving arm (animated) -->
          <g class="wave-arm">
            <line x1="76" y1="92" x2="94" y2="62" stroke="#1e4c9a" stroke-width="11" stroke-linecap="round" />
            <circle cx="96" cy="58" r="7" fill="#f7c9a3" />
          </g>
        </svg>
      </div>
    </section>

    <!-- C) ABOUT -->
    <section class="about" v-reveal>
      <!-- Wave divider flowing down from the navy hero -->
      <div class="section-wave wave-navy" aria-hidden="true">
        <svg viewBox="0 0 1440 90" preserveAspectRatio="none">
          <path d="M0,0 L1440,0 L1440,38 C1100,92 760,8 380,46 C230,61 100,58 0,40 Z" fill="#163a78" />
        </svg>
      </div>
      <span class="section-eyebrow">{{ t.about.eyebrow }}</span>
      <h2 class="section-heading">{{ t.about.heading }}</h2>
      <span class="heading-underline" aria-hidden="true"></span>
      <p class="about-body">{{ t.about.body }}</p>
    </section>

    <!-- D) FEATURES -->
    <section class="features" v-reveal>
      <span class="section-eyebrow">{{ t.features.eyebrow }}</span>
      <h2 class="section-heading">{{ t.features.heading }}</h2>
      <span class="heading-underline" aria-hidden="true"></span>
      <div class="feature-grid" v-stagger>
        <article v-for="(item, i) in t.features.items" :key="i" class="feature-card" :class="`accent-${i}`">
          <!-- Circular photo with a gradient ring -->
          <div class="feature-photo-frame">
            <img :src="featurePhotos[i]" :alt="item.title" class="feature-photo" />
          </div>
          <div class="feature-body">
            <h3 class="feature-title">{{ item.title }}</h3>
            <span class="title-bar" aria-hidden="true"></span>
            <p class="feature-desc">{{ item.desc }}</p>
          </div>
        </article>
      </div>
    </section>

    <!-- E) CONTACT -->
    <section class="contact" v-reveal>
      <!-- Wave divider transitioning from the grey Features section -->
      <div class="section-wave" aria-hidden="true">
        <svg viewBox="0 0 1440 90" preserveAspectRatio="none">
          <path d="M0,0 L1440,0 L1440,38 C1100,92 760,8 380,46 C230,61 100,58 0,40 Z" fill="#f3f5f9" />
        </svg>
      </div>
      <span class="section-eyebrow light">{{ t.contact.eyebrow }}</span>
      <h2 class="section-heading light">{{ t.contact.heading }}</h2>
      <span class="heading-underline light" aria-hidden="true"></span>
      <ul class="contact-list">
        <!-- Address (clickable: opens the location on Google Maps) -->
        <li>
          <a
            class="contact-row contact-link address-row"
            href="https://maps.app.goo.gl/z2QTMy4hdRHkg4dR9"
            target="_blank"
            rel="noopener"
          >
            <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
              <path d="M12 21s7-5.5 7-11a7 7 0 1 0-14 0c0 5.5 7 11 7 11Z" stroke="currentColor" stroke-width="1.8" stroke-linejoin="round" />
              <circle cx="12" cy="10" r="2.5" stroke="currentColor" stroke-width="1.8" />
            </svg>
            <span>{{ t.contact.address }}</span>
          </a>
        </li>
        <!-- Phone 1 (clickable tel: link) -->
        <li>
          <a class="contact-row contact-link" :href="`tel:${t.contact.phone1}`">
            <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
              <path d="M5 4h3l1.5 4-2 1.5a12 12 0 0 0 5 5l1.5-2 4 1.5V19a1 1 0 0 1-1 1A15 15 0 0 1 4 6a1 1 0 0 1 1-2Z" stroke="currentColor" stroke-width="1.8" stroke-linejoin="round" />
            </svg>
            <span dir="ltr">{{ t.contact.phone1 }}</span>
          </a>
        </li>
        <!-- Phone 2 (clickable tel: link) -->
        <li>
          <a class="contact-row contact-link" :href="`tel:${t.contact.phone2}`">
            <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
              <path d="M5 4h3l1.5 4-2 1.5a12 12 0 0 0 5 5l1.5-2 4 1.5V19a1 1 0 0 1-1 1A15 15 0 0 1 4 6a1 1 0 0 1 1-2Z" stroke="currentColor" stroke-width="1.8" stroke-linejoin="round" />
            </svg>
            <span dir="ltr">{{ t.contact.phone2 }}</span>
          </a>
        </li>
        <!-- Email (clickable mailto: link) -->
        <li>
          <a class="contact-row contact-link" :href="`mailto:${t.contact.email}`">
            <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
              <rect x="3" y="5" width="18" height="14" rx="2" stroke="currentColor" stroke-width="1.8" />
              <path d="m4 7 8 6 8-6" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" />
            </svg>
            <span dir="ltr">{{ t.contact.email }}</span>
          </a>
        </li>
        <!-- Facebook page (clickable, opens in new tab) -->
        <li>
          <a
            class="contact-row contact-link fb-row"
            href="https://www.facebook.com/profile.php?id=100057288796646"
            target="_blank"
            rel="noopener noreferrer"
          >
            <!-- Facebook icon in a blue circle -->
            <svg class="fb-icon" viewBox="0 0 24 24" fill="none" aria-hidden="true">
              <circle cx="12" cy="12" r="11" fill="#1877F2" />
              <path
                d="M15.1 12.6h-2v6.8h-2.8v-6.8H8.9v-2.4h1.4V8.7c0-1.9 1.1-3 2.9-3 .8 0 1.6.1 1.6.1v1.8h-.9c-.9 0-1.2.6-1.2 1.1v1.4h2l-.3 2.4Z"
                fill="#fff"
              />
            </svg>
            <span>{{ t.schoolName }}</span>
          </a>
        </li>
      </ul>
    </section>

    <!-- F) FOOTER -->
    <footer class="footer">
      <!-- Facebook link with the school name (bilingual) -->
      <a
        class="footer-fb"
        href="https://www.facebook.com/profile.php?id=100057288796646"
        target="_blank"
        rel="noopener noreferrer"
      >
        <svg class="fb-icon" viewBox="0 0 24 24" fill="none" aria-hidden="true">
          <circle cx="12" cy="12" r="11" fill="#1877F2" />
          <path
            d="M15.1 12.6h-2v6.8h-2.8v-6.8H8.9v-2.4h1.4V8.7c0-1.9 1.1-3 2.9-3 .8 0 1.6.1 1.6.1v1.8h-.9c-.9 0-1.2.6-1.2 1.1v1.4h2l-.3 2.4Z"
            fill="#fff"
          />
        </svg>
        <span>{{ t.schoolName }}</span>
      </a>
      <p>{{ t.footer }}</p>
    </footer>
  </div>
</template>

<style scoped>
/* Scoped reset so the page fills the width with no gaps */
:global(html),
:global(body),
:global(#app) {
  margin: 0;
  padding: 0;
  width: 100%;
}
/* Smooth scrolling for in-page jumps (e.g. navbar -> top) */
:global(html) {
  scroll-behavior: smooth;
}

*,
*::before,
*::after {
  box-sizing: border-box;
}

.welcome {
  --navy: var(--ds-navy);
  --navy-dark: var(--ds-navy-dark);
  --orange: var(--ds-orange);

  width: 100%;
  margin: 0;
  /* clip (not hidden) so it doesn't break the sticky navbar */
  overflow-x: clip;
  font-family: 'Segoe UI', system-ui, -apple-system, sans-serif;
  color: #1f2937;
}
.welcome[lang='ar'] {
  font-family: 'Segoe UI', 'Tahoma', system-ui, sans-serif;
  line-height: 1.7;
}

/* ---------- Sticky navbar (a distinct top bar) ---------- */
.navbar {
  position: sticky;
  top: 0;
  z-index: 30;
  display: flex;
  align-items: center;
  justify-content: space-between;
  /* Keep the layout fixed (logo left, controls right) in both languages */
  direction: ltr;
  padding: 1rem 2.25rem;
  /* Solid bar, clearly separated from the hero with a border + shadow */
  background: var(--navy-dark);
  border-bottom: 1px solid rgba(255, 255, 255, 0.14);
  box-shadow: 0 6px 22px rgba(0, 0, 0, 0.25);
  transition: background 0.3s ease, box-shadow 0.3s ease, padding 0.3s ease;
}
.navbar.solid {
  background: var(--navy);
  box-shadow: 0 8px 26px rgba(0, 0, 0, 0.3);
  padding-top: 0.65rem;
  padding-bottom: 0.65rem;
}
.nav-brand {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  cursor: pointer;
  outline: none;
}
.nav-brand:focus-visible {
  outline: 2px solid var(--orange);
  outline-offset: 4px;
  border-radius: 8px;
}
/* Small rounded white tile holding the logo */
.nav-logo {
  width: 46px;
  height: 46px;
  object-fit: contain;
  border-radius: 12px;
  background: #fff;
  padding: 4px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.2);
  transition: transform 0.25s ease, box-shadow 0.25s ease;
}
.nav-brand:hover .nav-logo {
  transform: scale(1.08);
  box-shadow: 0 6px 16px rgba(0, 0, 0, 0.28);
}
.nav-name {
  color: #fff;
  font-weight: 700;
  font-size: 1.1rem;
  letter-spacing: 0.01em;
  transition: color 0.25s ease;
}
.nav-brand:hover .nav-name {
  color: var(--orange);
}
.nav-actions {
  display: flex;
  align-items: center;
  gap: 1rem;
}
.nav-login {
  background: linear-gradient(135deg, #f6b65f, var(--orange));
  border: none;
  color: #fff;
  padding: 0.55rem 1.5rem;
  border-radius: 999px;
  font-weight: 700;
  cursor: pointer;
  box-shadow: 0 6px 16px rgba(242, 160, 61, 0.4);
  transition: transform 0.25s ease, filter 0.25s ease, box-shadow 0.25s ease;
}
.nav-login:hover {
  transform: translateY(-2px);
  box-shadow: 0 10px 22px rgba(242, 160, 61, 0.55);
  filter: brightness(1.07);
}

/* ---------- Hero ---------- */
.hero {
  position: relative;
  /* navbar is now in-flow (sticky), so fill the rest of the viewport */
  min-height: calc(100vh - 78px);
  display: flex;
  align-items: center;
  padding: 3rem 2rem;
  overflow: hidden;
  /* Layered radial glows over a deep navy gradient for depth */
  background:
    radial-gradient(circle at 16% 28%, rgba(255, 255, 255, 0.14), transparent 42%),
    radial-gradient(circle at 88% 18%, rgba(242, 160, 61, 0.22), transparent 46%),
    radial-gradient(circle at 75% 95%, rgba(47, 99, 186, 0.5), transparent 50%),
    linear-gradient(150deg, #2f63ba 0%, var(--navy) 48%, var(--navy-dark) 100%);
}
/* Subtle dotted texture, faded toward the edges */
.hero::before {
  content: '';
  position: absolute;
  inset: 0;
  z-index: 0;
  pointer-events: none;
  background-image: radial-gradient(rgba(255, 255, 255, 0.1) 1.5px, transparent 1.6px);
  background-size: 30px 30px;
  -webkit-mask-image: radial-gradient(ellipse at 50% 40%, #000 10%, transparent 78%);
  mask-image: radial-gradient(ellipse at 50% 40%, #000 10%, transparent 78%);
  opacity: 0.7;
}
/* Soft glow anchored at the bottom for a gentle vignette */
.hero::after {
  content: '';
  position: absolute;
  left: 50%;
  bottom: -260px;
  width: 760px;
  height: 520px;
  transform: translateX(-50%);
  z-index: 0;
  pointer-events: none;
  background: radial-gradient(ellipse, rgba(120, 170, 255, 0.28), transparent 70%);
  filter: blur(10px);
}
.circle {
  position: absolute;
  border-radius: 50%;
  filter: blur(6px); /* soften the blobs into glows */
}
.circle-1 {
  width: 480px;
  height: 480px;
  top: -170px;
  right: -150px;
  background: rgba(242, 160, 61, 0.18);
  animation: drift 18s ease-in-out infinite;
}
.circle-2 {
  width: 340px;
  height: 340px;
  bottom: -130px;
  left: -110px;
  background: rgba(255, 255, 255, 0.09);
  animation: drift 22s ease-in-out infinite reverse;
}
.circle-3 {
  width: 220px;
  height: 220px;
  top: 18%;
  left: 10%;
  background: rgba(124, 175, 255, 0.16);
  animation: drift 26s ease-in-out infinite;
}
/* Thin outlined ring for extra visual interest */
.circle-4 {
  width: 160px;
  height: 160px;
  bottom: 16%;
  right: 8%;
  background: transparent;
  border: 2px solid rgba(255, 255, 255, 0.15);
  filter: none;
  animation: drift 30s ease-in-out infinite reverse;
}
.hero-inner {
  position: relative;
  z-index: 1;
  width: 100%;
  max-width: 1140px;
  margin: 0 auto;
  display: flex;
  align-items: center;
  gap: 3rem;
}
.hero-text {
  flex: 1;
  color: #fff;
  animation: slide-up 0.7s ease-out both;
}
.hero-logo-tile {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 128px;
  height: 128px;
  background: #fff;
  border-radius: 26px;
  margin-bottom: 1.5rem;
  cursor: pointer;
  /* Base shadow (used during entrance and when motion is reduced) */
  box-shadow: 0 10px 22px rgba(0, 0, 0, 0.28);
  /* Gentle entrance (once), then a continuous soft float with a synced shadow */
  animation:
    logo-entrance 0.7s ease-out both,
    logo-float 3s ease-in-out 0.7s infinite;
}
/* Smoothly pause the float on hover */
.hero-logo-tile:hover {
  animation-play-state: paused;
}
.hero-logo {
  width: 100px;
  height: 100px;
  object-fit: contain;
  border-radius: 16px;
  /* Scale handled on the image so it's independent of the tile's animated transform */
  transition: transform 0.3s ease;
}
.hero-logo-tile:hover .hero-logo {
  transform: scale(1.08);
}
/* Entrance: fade in + scale up before the float loop begins */
@keyframes logo-entrance {
  from {
    opacity: 0;
    transform: translateY(6px) scale(0.85);
  }
  to {
    opacity: 1;
    transform: translateY(0) scale(1);
  }
}
/* Float: bob up/down ~11px with a soft shadow that grows/shrinks in sync */
@keyframes logo-float {
  0%,
  100% {
    transform: translateY(0);
    box-shadow: 0 10px 22px rgba(0, 0, 0, 0.28);
  }
  50% {
    transform: translateY(-11px);
    box-shadow: 0 24px 30px rgba(0, 0, 0, 0.16);
  }
}
.hero-heading {
  font-size: 2.8rem;
  font-weight: 800;
  line-height: 1.18;
  letter-spacing: -0.02em;
  color: #fff;
  margin: 0 0 1rem;
  text-shadow: 0 3px 20px rgba(0, 0, 0, 0.22);
}
.hero-subtitle {
  font-size: 1.2rem;
  line-height: 1.6;
  color: rgba(255, 255, 255, 0.9);
  margin: 0 0 2rem;
}
.hero-cta {
  position: relative;
  overflow: hidden; /* clip the sheen sweep */
  background: linear-gradient(135deg, #f6b65f, var(--orange));
  color: #fff;
  border: none;
  padding: 0.95rem 2.8rem;
  border-radius: 12px;
  font-size: 1.05rem;
  font-weight: 700;
  cursor: pointer;
  box-shadow: 0 10px 28px rgba(242, 160, 61, 0.5);
  transition: transform 0.3s ease, filter 0.3s ease, box-shadow 0.3s ease;
}
/* Light sheen that sweeps across on hover */
.hero-cta::after {
  content: '';
  position: absolute;
  top: 0;
  inset-inline-start: -75%;
  width: 50%;
  height: 100%;
  background: linear-gradient(120deg, transparent, rgba(255, 255, 255, 0.4), transparent);
  transform: skewX(-20deg);
  transition: inset-inline-start 0.6s ease;
  pointer-events: none;
}
.hero-cta:hover::after {
  inset-inline-start: 125%;
}
.hero-cta:hover {
  transform: translateY(-3px);
  filter: brightness(1.07);
  box-shadow: 0 16px 36px rgba(242, 160, 61, 0.6);
}
.hero-art {
  position: relative;
  flex: 1;
  display: flex;
  justify-content: center;
}

/* ---------- Waving mascot ---------- */
.mascot {
  position: absolute;
  bottom: 28px;
  inset-inline-start: 40px;
  width: 110px;
  z-index: 3;
  animation: mascot-in 0.7s ease-out 0.5s both;
}
.mascot-svg {
  width: 100%;
  height: auto;
  display: block;
  filter: drop-shadow(0 8px 16px rgba(0, 0, 0, 0.22));
}
/* The waving forearm pivots at the shoulder */
.wave-arm {
  transform-box: view-box;
  transform-origin: 76px 92px;
  animation: wave 2.4s ease-in-out infinite;
}
/* Speech bubble above the mascot */
.speech-bubble {
  position: absolute;
  top: -22px;
  inset-inline-start: 56px;
  background: #fff;
  color: var(--navy);
  font-weight: 700;
  font-size: 0.9rem;
  white-space: nowrap;
  padding: 0.5rem 0.85rem;
  border-radius: 14px;
  box-shadow: 0 8px 20px rgba(0, 0, 0, 0.22);
  animation: bubble-float 3s ease-in-out infinite;
}
/* Little tail pointing down toward the mascot */
.speech-bubble::after {
  content: '';
  position: absolute;
  bottom: -6px;
  inset-inline-start: 16px;
  width: 14px;
  height: 14px;
  background: #fff;
  transform: rotate(45deg);
  border-radius: 0 0 3px 0;
}

@keyframes wave {
  0%,
  55%,
  100% {
    transform: rotate(0deg);
  }
  62%,
  82% {
    transform: rotate(16deg);
  }
  72%,
  92% {
    transform: rotate(-12deg);
  }
}
@keyframes bubble-float {
  0%,
  100% {
    transform: translateY(0);
  }
  50% {
    transform: translateY(-5px);
  }
}
@keyframes mascot-in {
  from {
    opacity: 0;
    transform: translateY(20px) scale(0.9);
  }
  to {
    opacity: 1;
    transform: translateY(0) scale(1);
  }
}
/* Framed school photo on the right of the hero */
.photo-frame {
  position: relative;
  width: 100%;
  max-width: 480px;
  border-radius: 20px;
  padding: 8px;
  overflow: hidden; /* clip the zoom-on-hover to the frame */
  background: rgba(255, 255, 255, 0.12);
  border: 1px solid rgba(255, 255, 255, 0.3);
  box-shadow: 0 22px 50px rgba(0, 0, 0, 0.35), 0 0 0 1px rgba(242, 160, 61, 0.15);
  /* Fade in on load + gentle continuous float (frame transform) */
  animation: photo-in 0.9s ease-out both, float 6s ease-in-out 0.9s infinite;
  transition: box-shadow 0.4s ease;
}
.photo-frame:hover {
  box-shadow: 0 26px 60px rgba(0, 0, 0, 0.45), 0 0 0 1px rgba(242, 160, 61, 0.35);
}
.hero-photo {
  display: block;
  width: 100%;
  height: auto;
  border-radius: 14px;
  object-fit: cover;
  /* Subtle zoom-on-hover (image transform, independent of the frame float) */
  transition: transform 0.5s ease;
}
.photo-frame:hover .hero-photo {
  transform: scale(1.04);
}
@keyframes photo-in {
  from {
    opacity: 0;
    transform: translateY(18px) scale(0.98);
  }
  to {
    opacity: 1;
    transform: translateY(0) scale(1);
  }
}

/* ---------- Shared section bits ---------- */
/* Small uppercase label above each section heading */
.section-eyebrow {
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 0.6rem;
  margin-bottom: 0.7rem;
  font-size: 0.78rem;
  font-weight: 700;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--orange);
}
.section-eyebrow::before,
.section-eyebrow::after {
  content: '';
  width: 22px;
  height: 2px;
  border-radius: 2px;
  background: var(--orange);
  opacity: 0.65;
}
.section-eyebrow.light {
  color: var(--orange);
}
.section-heading {
  font-size: 2rem;
  font-weight: 800;
  letter-spacing: -0.01em;
  color: var(--navy);
  text-align: center;
  margin: 0 0 0.6rem;
}
.section-heading.light {
  color: #fff;
}
.heading-underline {
  display: block;
  width: 64px;
  height: 4px;
  border-radius: 2px;
  /* Navy→orange gradient adds depth vs. a flat bar */
  background: linear-gradient(90deg, var(--navy), var(--orange));
  margin: 0 auto 1.75rem;
}
/* On the dark contact section, use a warm orange gradient for contrast */
.heading-underline.light {
  background: linear-gradient(90deg, #f6b65f, var(--orange));
}

/* ---------- About ---------- */
.about {
  position: relative;
  overflow: hidden;
  padding: 6.5rem 2rem 5rem;
  text-align: center;
  /* Soft white-to-pale-blue wash with two faint corner glows */
  background:
    radial-gradient(circle at 8% 18%, rgba(30, 76, 154, 0.07), transparent 30%),
    radial-gradient(circle at 92% 82%, rgba(242, 160, 61, 0.09), transparent 32%),
    linear-gradient(180deg, #ffffff 0%, #f4f7fc 100%);
}
/* Decorative soft blobs behind the text */
.about::before {
  content: '';
  position: absolute;
  width: 240px;
  height: 240px;
  border-radius: 50%;
  background: rgba(30, 76, 154, 0.05);
  filter: blur(6px);
  top: -70px;
  inset-inline-start: -60px;
  animation: drift 24s ease-in-out infinite;
}
.about::after {
  content: '';
  position: absolute;
  width: 300px;
  height: 300px;
  border-radius: 50%;
  background: rgba(242, 160, 61, 0.07);
  filter: blur(8px);
  bottom: -110px;
  inset-inline-end: -80px;
  animation: drift 28s ease-in-out infinite reverse;
}
.about > * {
  position: relative;
  z-index: 1;
}
.about-body {
  max-width: 760px;
  margin: 0 auto;
  font-size: 1.12rem;
  line-height: 1.7;
  color: #4b5563;
}

/* ---------- Features ---------- */
.features {
  position: relative;
  overflow: hidden;
  padding: 5rem 2rem;
  /* Light grey wash with subtle navy/orange corner glows */
  background:
    radial-gradient(circle at 12% 12%, rgba(30, 76, 154, 0.06), transparent 28%),
    radial-gradient(circle at 88% 88%, rgba(242, 160, 61, 0.07), transparent 30%),
    linear-gradient(180deg, #eef2f8 0%, #f3f5f9 100%);
}
/* Faint dotted texture, fading toward the edges */
.features::before {
  content: '';
  position: absolute;
  inset: 0;
  z-index: 0;
  pointer-events: none;
  background-image: radial-gradient(rgba(30, 76, 154, 0.06) 1.5px, transparent 1.6px);
  background-size: 26px 26px;
  -webkit-mask-image: radial-gradient(ellipse at 50% 50%, #000 15%, transparent 80%);
  mask-image: radial-gradient(ellipse at 50% 50%, #000 15%, transparent 80%);
}
/* Drifting accent blob for a touch of life */
.features::after {
  content: '';
  position: absolute;
  z-index: 0;
  width: 320px;
  height: 320px;
  border-radius: 50%;
  background: rgba(30, 76, 154, 0.06);
  filter: blur(8px);
  top: -120px;
  inset-inline-end: -90px;
  pointer-events: none;
  animation: drift 26s ease-in-out infinite;
}
.features > * {
  position: relative;
  z-index: 1;
}
.feature-grid {
  max-width: 1080px;
  margin: 0 auto;
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 1.75rem;
}
.feature-card {
  position: relative;
  background: #fff;
  border: 1px solid #eef2f8;
  border-radius: 26px;
  text-align: center;
  cursor: pointer;
  padding: 2.75rem 1.5rem 2.5rem;
  display: flex;
  flex-direction: column;
  align-items: center;
  overflow: hidden; /* keep the top accent bar within the rounded corners */
  box-shadow: 0 10px 30px rgba(30, 76, 154, 0.08);
  /* Hidden until revealed by the stagger directive */
  opacity: 0;
  transform: translateY(28px);
  transition: opacity 0.5s ease, transform 0.35s ease, box-shadow 0.3s ease,
    border-color 0.3s ease, background 0.3s ease;
}
/* Per-card accent colours (tie back to the original icon palette) */
.accent-0 {
  --ring: linear-gradient(135deg, #1e4c9a, #2f63ba);
  --solid: #1e4c9a;
  --glow: rgba(30, 76, 154, 0.3);
}
.accent-1 {
  --ring: linear-gradient(135deg, #15a06b, #1ec88a);
  --solid: #15a06b;
  --glow: rgba(21, 160, 107, 0.3);
}
.accent-2 {
  --ring: linear-gradient(135deg, #f2a03d, #f6b65f);
  --solid: #e08e29;
  --glow: rgba(242, 160, 61, 0.35);
}
/* Thin accent bar along the top, revealed on hover */
.feature-card::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 5px;
  background: var(--ring);
  transform: scaleX(0);
  transition: transform 0.35s ease;
}
/* Revealed state (class added one-by-one for the stagger effect) */
.feature-card.card-visible {
  opacity: 1;
  transform: translateY(0);
}
.feature-card:hover {
  transform: translateY(-10px);
  border-color: #fff;
  /* faint cool wash so the card lifts with a touch of color */
  background: linear-gradient(180deg, #ffffff 0%, #f6f9ff 100%);
  box-shadow: 0 26px 50px rgba(30, 76, 154, 0.2);
}
.feature-card:hover::before {
  transform: scaleX(1);
}
/* Circular photo wrapped in a gradient ring */
.feature-photo-frame {
  width: 148px;
  height: 148px;
  border-radius: 50%;
  padding: 5px; /* thickness of the gradient ring */
  background: var(--ring);
  flex-shrink: 0;
  margin-bottom: 1.5rem;
  box-shadow: 0 10px 24px var(--glow);
  transition: transform 0.35s ease, box-shadow 0.35s ease;
}
.feature-card:hover .feature-photo-frame {
  transform: scale(1.04);
  box-shadow: 0 14px 30px var(--glow);
}
.feature-photo {
  display: block;
  width: 100%;
  height: 100%;
  border-radius: 50%;
  object-fit: cover; /* crop nicely to fill the circle */
  border: 4px solid #fff; /* white gap between ring and photo */
  transition: transform 0.4s ease;
}
/* Gently zoom the photo inside the circle on hover */
.feature-card:hover .feature-photo {
  transform: scale(1.08);
}
/* Text below the photo */
.feature-body {
  flex: 1; /* keeps the 3 cards equal height with aligned content */
  display: flex;
  flex-direction: column;
  align-items: center;
}
.feature-title {
  font-size: 1.25rem;
  font-weight: 800;
  color: var(--navy);
  margin: 0 0 0.65rem;
}
/* Short accent underline under each title */
.title-bar {
  width: 38px;
  height: 3px;
  border-radius: 2px;
  background: var(--solid);
  margin-bottom: 0.9rem;
  transition: width 0.35s ease;
}
.feature-card:hover .title-bar {
  width: 60px;
}
.feature-desc {
  font-size: 0.97rem;
  line-height: 1.6;
  color: #6b7280;
  margin: 0;
}

/* ---------- Contact ---------- */
.contact {
  position: relative;
  overflow: hidden;
  padding: 6.5rem 2rem 5rem;
  color: #fff;
  /* Deep navy with layered glows, matching the hero's depth */
  background:
    radial-gradient(circle at 15% 20%, rgba(255, 255, 255, 0.1), transparent 38%),
    radial-gradient(circle at 85% 88%, rgba(242, 160, 61, 0.18), transparent 44%),
    linear-gradient(150deg, var(--navy) 0%, var(--navy-dark) 100%);
}
/* Subtle dotted texture, faded toward the edges */
.contact::before {
  content: '';
  position: absolute;
  inset: 0;
  z-index: 0;
  pointer-events: none;
  background-image: radial-gradient(rgba(255, 255, 255, 0.08) 1.5px, transparent 1.6px);
  background-size: 30px 30px;
  -webkit-mask-image: radial-gradient(ellipse at 50% 40%, #000 10%, transparent 80%);
  mask-image: radial-gradient(ellipse at 50% 40%, #000 10%, transparent 80%);
}
.contact > * {
  position: relative;
  z-index: 1;
}
/* Wave divider sitting flush at the top of the contact section */
.section-wave {
  position: absolute;
  top: -1px;
  left: 0;
  width: 100%;
  line-height: 0;
  z-index: 1;
  pointer-events: none;
}
.section-wave svg {
  display: block;
  width: 100%;
  height: 70px;
}
.contact-list {
  max-width: 640px;
  margin: 0 auto;
  list-style: none;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 1.15rem;
}
.contact-list li {
  font-size: 1.05rem;
}
/* Each row (address span-row or clickable link). Shifts + brightens on hover. */
.contact-row {
  display: flex;
  align-items: center;
  gap: 0.9rem;
  color: #fff;
  text-decoration: none;
  transition: transform 0.25s ease, color 0.25s ease;
}
.contact-row:hover {
  transform: translateX(6px);
  color: var(--orange);
}
/* In RTL the row shifts toward the other side so it still moves "outward" */
.welcome[dir='rtl'] .contact-row:hover {
  transform: translateX(-6px);
}
/* Only the clickable rows get a pointer cursor */
.contact-link {
  cursor: pointer;
}
/* Keep the full address on a single line */
.address-row span {
  white-space: nowrap;
}
.contact-link span {
  transition: text-decoration-color 0.2s ease;
}
.contact-link:hover span {
  text-decoration: underline;
}
.contact-list svg {
  width: 24px;
  height: 24px;
  flex-shrink: 0;
  color: var(--orange);
}
/* Facebook icon keeps its own blue/white fills regardless of context */
.fb-icon {
  width: 24px;
  height: 24px;
  flex-shrink: 0;
}
.fb-row {
  font-weight: 700;
}

/* ---------- Footer ---------- */
.footer {
  background: #122c5c;
  color: rgba(255, 255, 255, 0.8);
  text-align: center;
  padding: 1.6rem 2rem;
  font-size: 0.88rem;
}
.footer p {
  margin: 0;
}
/* Facebook link in the footer (icon + bilingual school name) */
.footer-fb {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  margin-bottom: 0.6rem;
  color: #fff;
  font-weight: 700;
  text-decoration: none;
  transition: color 0.25s ease;
}
.footer-fb:hover {
  color: var(--orange);
}
.footer-fb:hover span {
  text-decoration: underline;
}

/* ---------- Scroll-reveal ---------- */
.reveal {
  opacity: 0;
  transform: translateY(28px);
  transition: opacity 0.6s ease-out, transform 0.6s ease-out;
}
.reveal-visible {
  opacity: 1;
  transform: translateY(0);
}

/* ---------- Animations ---------- */
@keyframes slide-up {
  from {
    opacity: 0;
    transform: translateY(24px);
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
    transform: translateY(-12px);
  }
}
@keyframes drift {
  0%,
  100% {
    transform: translate(0, 0) scale(1);
  }
  50% {
    transform: translate(30px, 22px) scale(1.08);
  }
}

@media (prefers-reduced-motion: reduce) {
  :global(html) {
    scroll-behavior: auto;
  }
  .hero-text,
  .hero-logo-tile,
  .photo-frame,
  .hero-photo,
  .mascot,
  .wave-arm,
  .speech-bubble,
  .circle,
  .about::before,
  .about::after,
  .features::after,
  .reveal,
  .feature-card,
  .feature-photo,
  .contact-row,
  .nav-logo,
  .nav-name,
  .hero-cta {
    animation: none;
    transition: none;
  }
  /* Keep revealed content visible without motion */
  .reveal,
  .feature-card {
    opacity: 1;
    transform: none;
  }
}

/* ---------- Responsive ---------- */
@media (max-width: 860px) {
  .hero-inner {
    flex-direction: column;
    text-align: center;
  }
  /* Photo sits below the hero text on mobile (DOM order) */
  .hero-art {
    order: 1;
    margin-top: 0.5rem;
  }
  .photo-frame {
    margin: 0 auto;
  }
  /* Hide the corner mascot on small screens to avoid overlap */
  .mascot {
    display: none;
  }
  .hero-heading {
    font-size: 2.1rem;
  }
  .feature-grid {
    grid-template-columns: 1fr;
  }
  .navbar {
    padding: 0.8rem 1.25rem;
  }
  .nav-name {
    display: none;
  }
  /* The full address can't fit one line on phones — let it wrap & shrink */
  .address-row span {
    white-space: normal;
    font-size: 0.95rem;
  }
}
</style>
