<script setup>
// Student dashboard home. Orange welcome hero + a row of stat cards computed from the
// student's OWN data (attendance % + average grade, both fetched with the student's JWT),
// then a lower area with a LIVE "recent announcements" preview (GET /api/events/my) and
// quick-access shortcut cards that link to the student's real pages. Themed orange,
// bilingual (EN+AR) + RTL.
import { computed, onMounted, ref } from 'vue'
import { useAuthStore } from '../../stores/auth'
import { useLanguageStore } from '../../stores/language'
import StatCard from '../../components/admin/StatCard.vue'
import { icons } from '../../components/admin/icons'
import { getMyAttendance } from '../../api/attendance'
import { getStudentGrades } from '../../api/grades'
import { getMyEvents } from '../../api/events'

const auth = useAuthStore()
const language = useLanguageStore()

const displayName = computed(() => auth.user?.firstName || 'Student')

// ---- Bilingual copy (same per-component i18n pattern as the teacher dashboard) ----
const content = {
  en: {
    welcome: 'Welcome back,',
    heroSub: "Here's your learning space at Almanahil Libyan School.",
    portal: 'Student portal',
    dash: '—',
    stats: { attendance: 'My Attendance', average: 'Average Grade', myClass: 'My Class', subjects: 'My Subjects' },
    annTitle: 'Recent announcements',
    annSub: 'The latest news and events meant for you.',
    viewAll: 'View all',
    annLoading: 'Loading announcements…',
    annError: 'We couldn’t load announcements.',
    annEmpty: 'No announcements for you right now.',
    retry: 'Retry',
    types: { Announcement: 'Announcement', Event: 'Event' },
    quickTitle: 'Quick access',
    quickSub: 'Jump straight to your pages.',
    quick: {
      attendance: { t: 'My Attendance', s: 'Your attendance records' },
      grades: { t: 'My Grades', s: 'Your grades across subjects' },
      materials: { t: 'Learning Materials', s: 'Resources from your teachers' },
      events: { t: 'Events & Announcements', s: 'All school news for you' }
    }
  },
  ar: {
    welcome: 'مرحباً بعودتك،',
    heroSub: 'هذه مساحتك التعليمية في مدرسة المناهل الليبية.',
    portal: 'بوابة الطالب',
    dash: '—',
    stats: { attendance: 'حضوري', average: 'معدل الدرجات', myClass: 'صفي', subjects: 'موادي' },
    annTitle: 'أحدث الإعلانات',
    annSub: 'آخر الأخبار والفعاليات الخاصة بك.',
    viewAll: 'عرض الكل',
    annLoading: 'جارٍ تحميل الإعلانات…',
    annError: 'تعذّر تحميل الإعلانات.',
    annEmpty: 'لا توجد إعلانات لك حالياً.',
    retry: 'إعادة المحاولة',
    types: { Announcement: 'إعلان', Event: 'فعالية' },
    quickTitle: 'وصول سريع',
    quickSub: 'انتقل مباشرةً إلى صفحاتك.',
    quick: {
      attendance: { t: 'حضوري', s: 'سجلات حضورك' },
      grades: { t: 'درجاتي', s: 'درجاتك في جميع المواد' },
      materials: { t: 'المواد التعليمية', s: 'موارد من معلميك' },
      events: { t: 'الفعاليات والإعلانات', s: 'كل أخبار المدرسة لك' }
    }
  }
}
const t = computed(() => content[language.lang])

// ---- Greeting + date (localized) ----
const now = new Date()
const greeting = computed(() => {
  const h = now.getHours()
  if (language.isArabic) return h < 12 ? 'صباح الخير' : 'مساء الخير'
  if (h < 12) return 'Good morning'
  if (h < 18) return 'Good afternoon'
  return 'Good evening'
})
const dateStr = computed(() =>
  now.toLocaleDateString(language.isArabic ? 'ar' : 'en-US', {
    weekday: 'long',
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  })
)

// ---- The student's own data (attendance + grades) ----
const attendance = ref([])
const grades = ref([])

async function loadAll() {
  const id = auth.user?.id
  // Partial-failure tolerant: one endpoint failing still lets the other card show.
  const [attRes, gradeRes] = await Promise.allSettled([
    getMyAttendance(),
    id ? getStudentGrades(id) : Promise.resolve({ data: [] })
  ])
  attendance.value = attRes.status === 'fulfilled' ? (attRes.value.data ?? []) : []
  grades.value = gradeRes.status === 'fulfilled' ? (gradeRes.value.data ?? []) : []
}

// ---- Recent announcements (live, from the student's own events feed) ----
const events = ref([])
const eventsLoading = ref(true)
const eventsError = ref('')

async function loadEvents() {
  eventsLoading.value = true
  eventsError.value = ''
  try {
    const { data } = await getMyEvents()
    events.value = data ?? []
  } catch (err) {
    eventsError.value = err.message || t.value.annError
    events.value = []
  } finally {
    eventsLoading.value = false
  }
}
// The backend returns upcoming events first — show the nearest few.
const recentEvents = computed(() => events.value.slice(0, 3))

onMounted(() => {
  loadAll()
  loadEvents()
})

// Compact, bilingual date+time (e.g. "5 Jul 2026, 14:30"); 24-hour wall-clock.
function formatDate(iso) {
  if (!iso) return ''
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return iso
  return d.toLocaleString(language.isArabic ? 'ar' : 'en-US', {
    year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit', hour12: false
  })
}

// ---- Computed stat values ----
const attendanceRate = computed(() => {
  const total = attendance.value.length
  if (!total) return t.value.dash
  const present = attendance.value.filter((r) => r.status === 'Present').length
  return `${Math.round((present / total) * 100)}%`
})
const averageGrade = computed(() => {
  const p = grades.value.map((r) => (r.maxMark > 0 ? (r.mark / r.maxMark) * 100 : 0))
  if (!p.length) return t.value.dash
  return `${Math.round(p.reduce((a, b) => a + b, 0) / p.length)}%`
})
const myClass = computed(() => grades.value[0]?.className || attendance.value[0]?.className || t.value.dash)
const subjectsCount = computed(() => {
  const ids = new Set()
  grades.value.forEach((r) => ids.add(r.subjectId))
  attendance.value.forEach((r) => ids.add(r.subjectId))
  return ids.size
})

const statCards = computed(() => [
  { variant: 'amber', label: t.value.stats.attendance, value: attendanceRate.value, icon: icons.attendance },
  { variant: 'green', label: t.value.stats.average, value: averageGrade.value, icon: icons.grades },
  { variant: 'indigo', label: t.value.stats.myClass, value: myClass.value, icon: icons.classes },
  { variant: 'cyan', label: t.value.stats.subjects, value: subjectsCount.value, icon: icons.subjects }
])

// ---- Quick-access shortcuts → the student's real pages ----
const quickLinks = computed(() => [
  { name: 'student-attendance', ...t.value.quick.attendance, icon: icons.attendance },
  { name: 'student-grades', ...t.value.quick.grades, icon: icons.grades },
  { name: 'student-materials', ...t.value.quick.materials, icon: icons.layers },
  { name: 'student-events', ...t.value.quick.events, icon: icons.announce }
])
</script>

<template>
  <div class="dashboard" :dir="language.dir">
    <!-- Welcome hero banner (orange) -->
    <section class="hero">
      <div class="hero-meta">
        <span class="hero-chip">
          <span class="live-dot"></span>
          {{ greeting }} · {{ dateStr }}
        </span>
        <span class="hero-chip soon-chip">{{ t.portal }}</span>
      </div>

      <h2 class="hero-title">{{ t.welcome }} <span class="hero-name">{{ displayName }}!</span></h2>
      <p class="hero-sub">{{ t.heroSub }}</p>

      <!-- Stat cards, right inside the welcome box -->
      <div class="hero-stats">
        <StatCard
          v-for="(c, i) in statCards"
          :key="i"
          :variant="c.variant"
          :label="c.label"
          :value="c.value"
          :icon="c.icon"
        />
      </div>
    </section>

    <!-- Lower area: live announcements preview + quick-access shortcuts -->
    <div class="dash-lower">
      <!-- Recent announcements (real data) -->
      <section class="block lower-main">
        <div class="block-head with-action">
          <div class="bh-text">
            <h3>{{ t.annTitle }}</h3>
            <p>{{ t.annSub }}</p>
          </div>
          <router-link :to="{ name: 'student-events' }" class="view-all">
            {{ t.viewAll }}
            <svg viewBox="0 0 24 24" v-html="icons.arrow"></svg>
          </router-link>
        </div>

        <div class="ann-panel">
          <!-- Loading -->
          <div v-if="eventsLoading" class="ann-state">
            <span class="spinner"></span>
            <span>{{ t.annLoading }}</span>
          </div>

          <!-- Error -->
          <div v-else-if="eventsError" class="ann-state err">
            <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="9" /><path d="M12 8v5M12 16h.01" /></svg>
            <span>{{ eventsError }}</span>
            <button type="button" class="retry-btn" @click="loadEvents">{{ t.retry }}</button>
          </div>

          <!-- Empty -->
          <div v-else-if="!recentEvents.length" class="ann-state">
            <span class="ann-empty-badge"><svg viewBox="0 0 24 24" v-html="icons.announce"></svg></span>
            <span>{{ t.annEmpty }}</span>
          </div>

          <!-- List (latest few) -->
          <ul v-else class="ann-list">
            <li v-for="ev in recentEvents" :key="ev.id">
              <router-link :to="{ name: 'student-events' }" class="ann-item">
                <span class="ann-dot" :class="ev.type === 'Event' ? 'is-event' : 'is-announce'"></span>
                <span class="ann-body">
                  <span class="ann-row">
                    <span class="ann-title">{{ ev.title }}</span>
                    <span class="ann-badge" :class="ev.type === 'Event' ? 'is-event' : 'is-announce'">
                      {{ t.types[ev.type] || ev.type }}
                    </span>
                  </span>
                  <span class="ann-date">
                    <svg viewBox="0 0 24 24"><rect x="3" y="4" width="18" height="17" rx="3" /><path d="M8 2v4M16 2v4M3 10h18" /></svg>
                    {{ formatDate(ev.date) }}
                  </span>
                </span>
              </router-link>
            </li>
          </ul>
        </div>
      </section>

      <!-- Quick-access shortcuts -->
      <section class="block lower-side">
        <div class="block-head">
          <h3>{{ t.quickTitle }}</h3>
          <p>{{ t.quickSub }}</p>
        </div>
        <div class="quick-grid">
          <router-link v-for="q in quickLinks" :key="q.name" :to="{ name: q.name }" class="quick-card">
            <span class="quick-badge"><svg viewBox="0 0 24 24" v-html="q.icon"></svg></span>
            <span class="quick-text">
              <span class="quick-t">{{ q.t }}</span>
              <span class="quick-s">{{ q.s }}</span>
            </span>
            <span class="quick-arrow"><svg viewBox="0 0 24 24" v-html="icons.arrow"></svg></span>
          </router-link>
        </div>
      </section>
    </div>
  </div>
</template>

<style scoped>
.dashboard {
  --orange: #ef8a29;
  --orange-strong: #f97316;
  --gold: #f6b93b;

  display: flex;
  flex-direction: column;
  gap: 2rem;
}

@keyframes rise {
  from { opacity: 0; transform: translateY(16px); }
  to { opacity: 1; transform: translateY(0); }
}

/* ---------- Hero (orange) ---------- */
.hero {
  position: relative;
  overflow: hidden;
  border-radius: 20px;
  padding: 0.95rem 1.5rem 1.15rem;
  color: #fff;
  animation: rise 0.5s ease both;
  /* Layered radial glows over a deep orange gradient (student counterpart of the
     green teacher hero). */
  background:
    radial-gradient(circle at 15% 25%, rgba(255, 255, 255, 0.14), transparent 40%),
    radial-gradient(circle at 90% 12%, rgba(255, 221, 148, 0.24), transparent 44%),
    radial-gradient(circle at 78% 96%, rgba(246, 185, 59, 0.45), transparent 48%),
    linear-gradient(135deg, #e07f1c 0%, #c1650f 55%, #9c500a 100%);
  box-shadow: 0 18px 40px rgba(156, 80, 10, 0.28);
}
/* Faded dotted texture for depth (masked toward the edges) */
.hero::before {
  content: '';
  position: absolute;
  inset: 0;
  z-index: 0;
  pointer-events: none;
  background-image: radial-gradient(rgba(255, 255, 255, 0.09) 1.5px, transparent 1.6px);
  background-size: 26px 26px;
  -webkit-mask-image: radial-gradient(ellipse at 62% 32%, #000 8%, transparent 74%);
  mask-image: radial-gradient(ellipse at 62% 32%, #000 8%, transparent 74%);
  opacity: 0.75;
}
/* Soft gold glow blob in the corner */
.hero::after {
  content: '';
  position: absolute;
  top: -120px;
  inset-inline-end: -80px;
  width: 320px;
  height: 320px;
  z-index: 0;
  border-radius: 50%;
  background: radial-gradient(circle, rgba(246, 185, 59, 0.24), transparent 70%);
  filter: blur(8px);
  pointer-events: none;
}
.hero > * {
  position: relative;
  z-index: 1;
}
.hero-meta {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.75rem;
  font-size: 0.78rem;
}
.hero-chip {
  display: inline-flex;
  align-items: center;
  gap: 0.45rem;
  padding: 0.4rem 0.9rem;
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.14);
  border: 1px solid rgba(255, 255, 255, 0.2);
  backdrop-filter: blur(4px);
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.04em;
}
.soon-chip {
  text-transform: none;
  letter-spacing: 0;
  color: #ffe8c4;
}
.live-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: #fcd34d;
  box-shadow: 0 0 8px rgba(252, 211, 77, 0.9);
}
.hero-title {
  margin: 0.5rem 0 0.15rem;
  font-size: 1.45rem;
  font-weight: 800;
  letter-spacing: -0.01em;
}
.hero-name {
  color: #ffd9a8;
}
.hero-sub {
  margin: 0;
  color: rgba(255, 255, 255, 0.85);
  font-size: 0.9rem;
}

/* Stat cards row inside the welcome box. */
.hero-stats {
  margin-top: 0.9rem;
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 0.7rem;
}

/* ---------- Lower area layout ---------- */
.dash-lower {
  display: grid;
  grid-template-columns: 1.6fr 1fr;
  gap: 1.5rem;
  align-items: start;
}

/* ---------- Block heading ---------- */
.block {
  animation: rise 0.5s ease both;
  animation-delay: 0.12s;
}
.block-head.with-action {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
}
.block-head h3 {
  position: relative;
  margin: 0;
  padding-inline-start: 0.85rem;
  font-size: 1.35rem;
  font-weight: 800;
  color: #3a2410;
  letter-spacing: -0.01em;
}
/* Short orange→gold accent bar before the section heading */
.block-head h3::before {
  content: '';
  position: absolute;
  inset-inline-start: 0;
  top: 50%;
  transform: translateY(-50%);
  width: 5px;
  height: 1.05rem;
  border-radius: 3px;
  background: linear-gradient(180deg, var(--orange), var(--gold));
}
.block-head p {
  margin: 0.2rem 0 1.1rem;
  font-size: 0.9rem;
  color: #8a6a4d;
}
.view-all {
  display: inline-flex;
  align-items: center;
  gap: 0.3rem;
  flex-shrink: 0;
  padding-top: 0.25rem;
  font-size: 0.83rem;
  font-weight: 700;
  color: var(--orange-strong);
  text-decoration: none;
  white-space: nowrap;
}
.view-all:hover { text-decoration: underline; }
.view-all svg {
  width: 15px;
  height: 15px;
  fill: none;
  stroke: currentColor;
  stroke-width: 2.2;
  stroke-linecap: round;
  stroke-linejoin: round;
}

/* ---------- Recent-announcements panel ---------- */
.ann-panel {
  background: #fff;
  border: 1px solid #f2e4d3;
  border-radius: 18px;
  padding: 0.4rem 0.5rem;
  box-shadow: 0 8px 22px rgba(156, 80, 10, 0.05);
  min-height: 150px;
}
.ann-list {
  list-style: none;
  margin: 0;
  padding: 0;
}
.ann-list li:not(:last-child) {
  border-bottom: 1px solid #f7ede0;
}
.ann-item {
  display: flex;
  align-items: flex-start;
  gap: 0.8rem;
  padding: 0.85rem 0.85rem;
  border-radius: 12px;
  text-decoration: none;
  transition: background 0.15s ease;
}
.ann-item:hover { background: #fff3e6; }
.ann-dot {
  width: 10px;
  height: 10px;
  margin-top: 0.35rem;
  border-radius: 50%;
  flex-shrink: 0;
}
.ann-dot.is-event { background: var(--orange); }
.ann-dot.is-announce { background: var(--gold); }
.ann-body {
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
  flex: 1;
  min-width: 0;
}
.ann-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.6rem;
}
.ann-title {
  font-size: 0.93rem;
  font-weight: 700;
  color: #3a2410;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.ann-badge {
  flex-shrink: 0;
  padding: 0.2rem 0.55rem;
  border-radius: 999px;
  font-size: 0.68rem;
  font-weight: 800;
  letter-spacing: 0.01em;
}
.ann-badge.is-event { color: #1e4c9a; background: #e0ecff; }
.ann-badge.is-announce { color: #b45309; background: #fef3c7; }
.ann-date {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  font-size: 0.78rem;
  font-weight: 600;
  color: #8a6a4d;
}
.ann-date svg {
  width: 14px;
  height: 14px;
  fill: none;
  stroke: var(--orange);
  stroke-width: 1.8;
  stroke-linecap: round;
  stroke-linejoin: round;
}

/* Announcement panel states (loading / error / empty) */
.ann-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 0.6rem;
  padding: 2.4rem 1.5rem;
  text-align: center;
  color: #a1846a;
  font-size: 0.9rem;
  font-weight: 600;
  min-height: 150px;
}
.ann-state.err { color: #b91c1c; }
.ann-state.err svg {
  width: 30px;
  height: 30px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.7;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.ann-empty-badge {
  width: 56px;
  height: 56px;
  border-radius: 50%;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: #d8b48c;
  background: radial-gradient(circle, #fff4e4, #ffe9cc);
}
.ann-empty-badge svg {
  width: 26px;
  height: 26px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.6;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.spinner {
  width: 30px;
  height: 30px;
  border-radius: 50%;
  border: 3px solid #f0e0cd;
  border-top-color: var(--orange);
  animation: spin 0.8s linear infinite;
}
@keyframes spin { to { transform: rotate(360deg); } }
.retry-btn {
  margin-top: 0.2rem;
  padding: 0.5rem 1.2rem;
  border: 1px solid #f0d6b6;
  border-radius: 10px;
  background: #fff;
  font-family: inherit;
  font-size: 0.83rem;
  font-weight: 700;
  color: var(--orange-strong);
  cursor: pointer;
  transition: background 0.15s ease;
}
.retry-btn:hover { background: #fff3e6; }

/* ---------- Quick-access cards ---------- */
.quick-grid {
  display: flex;
  flex-direction: column;
  gap: 0.8rem;
}
.quick-card {
  position: relative;
  display: flex;
  align-items: center;
  gap: 0.9rem;
  padding: 0.85rem 1rem;
  background: #fff;
  border: 1px solid #f2e4d3;
  border-radius: 14px;
  text-decoration: none;
  box-shadow: 0 6px 16px rgba(156, 80, 10, 0.05);
  transition: transform 0.2s ease, box-shadow 0.2s ease, border-color 0.2s ease;
}
.quick-card:hover {
  transform: translateY(-2px);
  border-color: #f0d6b6;
  box-shadow: 0 14px 30px rgba(156, 80, 10, 0.12);
}
.quick-badge {
  width: 40px;
  height: 40px;
  flex-shrink: 0;
  border-radius: 11px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: var(--orange);
  background: linear-gradient(135deg, rgba(239, 138, 41, 0.14), rgba(246, 185, 59, 0.18));
}
.quick-badge svg {
  width: 20px;
  height: 20px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.9;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.quick-text {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
  flex: 1;
  min-width: 0;
}
.quick-t {
  font-size: 0.95rem;
  font-weight: 800;
  color: #3a2410;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.quick-s {
  font-size: 0.78rem;
  color: #8a6a4d;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.quick-arrow {
  flex-shrink: 0;
  color: var(--orange);
  display: inline-flex;
  transition: transform 0.2s ease;
}
.quick-arrow svg {
  width: 18px;
  height: 18px;
  fill: none;
  stroke: currentColor;
  stroke-width: 2.2;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.quick-card:hover .quick-arrow { transform: translateX(3px); }

/* RTL: point the arrows the other way (and flip the hover nudge). */
.dashboard[dir='rtl'] .view-all svg,
.dashboard[dir='rtl'] .quick-arrow svg { transform: scaleX(-1); }
.dashboard[dir='rtl'] .quick-card:hover .quick-arrow { transform: translateX(-3px); }

/* ---------- Responsive ---------- */
@media (max-width: 1100px) {
  .hero-stats {
    grid-template-columns: repeat(2, 1fr);
  }
  .dash-lower {
    grid-template-columns: 1fr;
  }
}
@media (max-width: 480px) {
  .hero-stats {
    grid-template-columns: 1fr;
  }
  .ann-row {
    flex-direction: column;
    align-items: flex-start;
    gap: 0.25rem;
  }
}

@media (prefers-reduced-motion: reduce) {
  .hero,
  .block {
    animation: none;
  }
  .spinner {
    animation-duration: 1.6s;
  }
}
</style>
