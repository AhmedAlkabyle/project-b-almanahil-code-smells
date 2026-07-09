<script setup>
import { computed, onMounted, ref } from 'vue'
import { useAuthStore } from '../../stores/auth'
import { useLanguageStore } from '../../stores/language'
import StatCard from '../../components/admin/StatCard.vue'
import BaseChart from '../../components/admin/BaseChart.vue'
import { icons } from '../../components/admin/icons'
import { getDashboardStats } from '../../api/dashboard'
import { listClasses } from '../../api/classes'

const auth = useAuthStore()
const language = useLanguageStore()

const displayName = computed(() => auth.user?.firstName || 'Admin')

// ---- Bilingual copy (existing per-component i18n pattern) ----
const content = {
  en: {
    goodMorning: 'Good morning',
    goodAfternoon: 'Good afternoon',
    goodEvening: 'Good evening',
    updated: 'Updated',
    loadingStats: 'Loading…',
    welcome: 'Welcome back,',
    heroSub: "Here's what's happening at Almanahil Libyan School today.",
    userActivity: 'User Activity',
    addUser: 'Add User',
    newEvent: 'New Event',
    heroStats: { users: 'Total Users', classes: 'Active Classes', students: 'Students', teachers: 'Teachers' },
    overviewTitle: 'Overview',
    overviewSub: 'Quick snapshot of users in the system',
    ovStudents: 'Total Students',
    ovTeachers: 'Total Teachers',
    ovParents: 'Parents',
    ovUsers: 'Total Users',
    ovAdmins: 'Total Admins',
    ofTotal: 'of total',
    active: 'active',
    analyticsTitle: 'Analytics',
    analyticsSub: 'Visualize data across the platform',
    quickTitle: 'Quick Access',
    quickSub: 'Jump to a module in one click',
    cards: {
      spc: { t: 'Students per Class', s: 'Students enrolled in each class' },
      roles: { t: 'Users by Role', s: 'Distribution across the system' },
      level: { t: 'Classes by Level', s: 'Secondary vs High School' }
    },
    roleLabels: { students: 'Students', parents: 'Parents', teachers: 'Teachers', admins: 'Admins' },
    levelLabels: { secondary: 'Secondary', high: 'High School' },
    seriesStudents: 'Students',
    chartsError: 'We couldn’t load the analytics. Please try again.',
    noData: 'No data yet',
    quick: {
      users: { t: 'Manage Users', s: 'Add, edit, remove' },
      classes: { t: 'Classes', s: 'Rooms & students' },
      subjects: { t: 'Subjects', s: 'Curriculum catalog' },
      assignments: { t: 'Teacher Assignments', s: 'Who teaches what' },
      events: { t: 'Events', s: 'Publish news' },
      grades: { t: 'Grades', s: 'View records' },
      attendance: { t: 'Attendance', s: 'Daily reports' }
    }
  },
  ar: {
    goodMorning: 'صباح الخير',
    goodAfternoon: 'مساء الخير',
    goodEvening: 'مساء الخير',
    updated: 'آخر تحديث',
    loadingStats: 'جارٍ التحميل…',
    welcome: 'مرحباً بعودتك،',
    heroSub: 'إليك ما يحدث في مدرسة المناهل الزاخرة اليوم.',
    userActivity: 'نشاط المستخدمين',
    addUser: 'إضافة مستخدم',
    newEvent: 'فعالية جديدة',
    heroStats: { users: 'إجمالي المستخدمين', classes: 'الصفوف النشطة', students: 'الطلاب', teachers: 'المعلمون' },
    overviewTitle: 'نظرة عامة',
    overviewSub: 'لمحة سريعة عن المستخدمين في النظام',
    ovStudents: 'إجمالي الطلاب',
    ovTeachers: 'إجمالي المعلمين',
    ovParents: 'أولياء الأمور',
    ovUsers: 'إجمالي المستخدمين',
    ovAdmins: 'إجمالي المدراء',
    ofTotal: 'من الإجمالي',
    active: 'نشط',
    analyticsTitle: 'التحليلات',
    analyticsSub: 'عرض البيانات عبر المنصة',
    quickTitle: 'وصول سريع',
    quickSub: 'انتقل إلى أي قسم بنقرة واحدة',
    cards: {
      spc: { t: 'الطلاب لكل صف', s: 'عدد الطلاب المسجّلين في كل صف' },
      roles: { t: 'المستخدمون حسب الدور', s: 'التوزيع في النظام' },
      level: { t: 'الصفوف حسب المرحلة', s: 'الإعدادي مقابل الثانوي' }
    },
    roleLabels: { students: 'الطلاب', parents: 'أولياء الأمور', teachers: 'المعلمون', admins: 'المدراء' },
    levelLabels: { secondary: 'إعدادي', high: 'ثانوي' },
    seriesStudents: 'الطلاب',
    chartsError: 'تعذّر تحميل التحليلات. يرجى المحاولة مرة أخرى.',
    noData: 'لا توجد بيانات بعد',
    quick: {
      users: { t: 'إدارة المستخدمين', s: 'إضافة وتعديل وحذف' },
      classes: { t: 'الصفوف', s: 'القاعات والطلاب' },
      subjects: { t: 'المواد', s: 'دليل المناهج' },
      assignments: { t: 'تعيينات المعلمين', s: 'من يدرّس ماذا' },
      events: { t: 'الفعاليات', s: 'نشر الأخبار' },
      grades: { t: 'الدرجات', s: 'عرض السجلات' },
      attendance: { t: 'الحضور', s: 'التقارير اليومية' }
    }
  }
}
const t = computed(() => content[language.lang])

// ---- Greeting + date (localized) ----
const now = new Date()
const greeting = computed(() => {
  const h = now.getHours()
  if (h < 12) return t.value.goodMorning
  if (h < 18) return t.value.goodAfternoon
  return t.value.goodEvening
})
const dateStr = computed(() =>
  now.toLocaleDateString(language.isArabic ? 'ar' : 'en-US', {
    weekday: 'long',
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  })
)
// ---- Stats + classes (real data for the cards and charts) ----
// stats  → GET /api/dashboard/stats (hero cards + Users-by-Role chart)
// classList → GET /api/classes (Students-per-Class + Classes-by-Level charts; each
//             class already carries studentsCount, so no backend change is needed).
const stats = ref({ users: 0, classes: 0, students: 0, teachers: 0, parents: 0, admins: 0, subjects: 0 })
const classList = ref([])
const statsLoading = ref(true)
const analyticsLoading = ref(true)
const analyticsError = ref('')

async function loadAll() {
  statsLoading.value = true
  analyticsLoading.value = true
  analyticsError.value = ''
  // Fetch independently so a partial failure still renders what it can.
  const [statsRes, classesRes] = await Promise.allSettled([getDashboardStats(), listClasses()])

  if (statsRes.status === 'fulfilled') {
    const d = statsRes.value.data
    stats.value = {
      users: d.totalUsers,
      students: d.totalStudents,
      teachers: d.totalTeachers,
      parents: d.totalParents,
      admins: d.totalAdmins,
      classes: d.totalClasses,
      subjects: d.totalSubjects
    }
  }
  if (classesRes.status === 'fulfilled') {
    classList.value = classesRes.value.data ?? []
  }
  // Only surface an analytics error if BOTH sources failed (otherwise show partial data).
  if (statsRes.status === 'rejected' && classesRes.status === 'rejected') {
    analyticsError.value = t.value.chartsError
  }
  statsLoading.value = false
  analyticsLoading.value = false
}
onMounted(loadAll)

const activityPercent = ref(100) // TODO: derive from real activity data

// Percentage of the total user base (guard against divide-by-zero).
const pct = (n) => (stats.value.users ? Math.round((n / stats.value.users) * 100) : 0)

// Order: Students, Parents, Teachers, Admins, then Total Users last (the running
// total). Colours kept as before (student amber, parent grey, teacher green, admin
// indigo); Total Users uses cyan so it reads distinct from the Admin purple.
const overviewCards = computed(() => [
  { variant: 'amber', label: t.value.ovStudents, value: stats.value.students, badge: `${pct(stats.value.students)}% ${t.value.ofTotal}`, icon: icons.student },
  { variant: 'slate', label: t.value.ovParents, value: stats.value.parents, badge: `${pct(stats.value.parents)}% ${t.value.ofTotal}`, icon: icons.user },
  { variant: 'green', label: t.value.ovTeachers, value: stats.value.teachers, badge: `${pct(stats.value.teachers)}% ${t.value.ofTotal}`, icon: icons.book },
  { variant: 'indigo', label: t.value.ovAdmins, value: stats.value.admins, badge: `${pct(stats.value.admins)}% ${t.value.ofTotal}`, icon: icons.settings },
  { variant: 'cyan', label: t.value.ovUsers, value: stats.value.users, badge: `${stats.value.users} ${t.value.active}`, icon: icons.users }
])

// ---- Charts (Chart.js) --------------------------------------------------------------
const isRtl = computed(() => language.isArabic)

// Brand + role palette (matches the pills/tabs used across the admin).
const C = {
  navy: '#1e4c9a',
  orange: '#f2a03d',
  gray: '#94a3b8',
  green: '#10b981',
  emerald: '#059669',
  indigo: '#4f46e5'
}

const hasClasses = computed(() => classList.value.length > 0)
const hasUsers = computed(() => stats.value.users > 0)

// CHART 1 — Students per Class (bar). Sorted by class name so 1/A, 1/B, 2/A read in order.
const spcData = computed(() => {
  const list = [...classList.value].sort((a, b) =>
    (a.name || a.displayName || '').localeCompare(b.name || b.displayName || '', undefined, { numeric: true })
  )
  return {
    labels: list.map((c) => c.name || c.displayName || '—'),
    datasets: [
      {
        label: t.value.seriesStudents,
        data: list.map((c) => c.studentsCount ?? 0),
        backgroundColor: C.navy,
        hoverBackgroundColor: C.orange,
        borderRadius: 8,
        maxBarThickness: 46
      }
    ]
  }
})

// CHART 2 — Users by Role (doughnut): Students / Parents / Teachers / Admins.
const rolesData = computed(() => ({
  labels: [
    t.value.roleLabels.students,
    t.value.roleLabels.parents,
    t.value.roleLabels.teachers,
    t.value.roleLabels.admins
  ],
  datasets: [
    {
      data: [stats.value.students, stats.value.parents, stats.value.teachers, stats.value.admins],
      backgroundColor: [C.orange, C.gray, C.green, C.navy],
      borderColor: '#fff',
      borderWidth: 2,
      hoverOffset: 6
    }
  ]
}))

// CHART 3 — Classes by Level (doughnut): Secondary vs High School.
const levelData = computed(() => {
  const secondary = classList.value.filter((c) => c.level === 'Secondary').length
  const high = classList.value.filter((c) => c.level === 'HighSchool').length
  return {
    labels: [t.value.levelLabels.secondary, t.value.levelLabels.high],
    datasets: [
      {
        data: [secondary, high],
        backgroundColor: [C.emerald, C.indigo],
        borderColor: '#fff',
        borderWidth: 2,
        hoverOffset: 6
      }
    ]
  }
})

// Option builders react to language so legends/axes flip correctly in RTL.
const barOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  animation: { duration: 600 },
  plugins: {
    legend: { display: false },
    tooltip: { rtl: isRtl.value, backgroundColor: '#0f2444', padding: 10, cornerRadius: 8, titleFont: { weight: '700' } }
  },
  scales: {
    x: {
      reverse: isRtl.value, // first class sits on the correct side in RTL
      grid: { display: false },
      ticks: { color: '#64748b', font: { weight: '700' } }
    },
    y: {
      beginAtZero: true,
      position: isRtl.value ? 'right' : 'left',
      ticks: { precision: 0, stepSize: 1, color: '#94a3b8' },
      grid: { color: '#eef1f7' }
    }
  }
}))

const doughnutOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  cutout: '62%',
  animation: { duration: 600 },
  plugins: {
    legend: {
      position: 'bottom',
      rtl: isRtl.value,
      textDirection: isRtl.value ? 'rtl' : 'ltr',
      labels: { usePointStyle: true, pointStyle: 'circle', padding: 14, color: '#334155', font: { weight: '700' } }
    },
    tooltip: { rtl: isRtl.value, backgroundColor: '#0f2444', padding: 10, cornerRadius: 8 }
  }
}))
</script>

<template>
  <div class="dashboard">
    <!-- Welcome hero banner -->
    <section class="hero">
      <div class="hero-meta">
        <span class="hero-chip">
          <span class="live-dot"></span>
          {{ greeting }} · {{ dateStr }}
        </span>
        <span v-if="statsLoading" class="hero-chip loading-chip">
          <span class="mini-spinner"></span>
          {{ t.loadingStats }}
        </span>
      </div>

      <h2 class="hero-title">{{ t.welcome }} <span class="hero-name">{{ displayName }}!</span></h2>
      <p class="hero-sub">{{ t.heroSub }}</p>

      <!-- Activity bar -->
      <div class="activity">
        <div class="activity-head">
          <span>{{ t.userActivity }}</span>
          <span>{{ activityPercent }}%</span>
        </div>
        <div class="activity-track">
          <div class="activity-fill" :style="{ width: activityPercent + '%' }"></div>
        </div>
      </div>

      <!-- Quick actions -->
      <div class="hero-actions">
        <button class="hero-btn ghost" type="button" aria-label="Refresh">
          <svg viewBox="0 0 24 24" v-html="icons.refresh"></svg>
        </button>
        <router-link to="/admin/users" class="hero-btn ghost with-text">
          <svg viewBox="0 0 24 24" v-html="icons.plus"></svg>
          <span>{{ t.addUser }}</span>
        </router-link>
        <router-link to="/admin/events" class="hero-btn accent">
          <svg viewBox="0 0 24 24" v-html="icons.bell"></svg>
          <span>{{ t.newEvent }}</span>
        </router-link>
      </div>

      <!-- Colored stat cards, right inside the welcome box -->
      <div class="hero-stats">
        <StatCard
          v-for="(c, i) in overviewCards"
          :key="i"
          :variant="c.variant"
          :label="c.label"
          :value="c.value"
          :badge="c.badge"
          :icon="c.icon"
        />
      </div>
    </section>

    <!-- Analytics (real charts) -->
    <section class="block">
      <div class="block-head">
        <h3>{{ t.analyticsTitle }}</h3>
        <p>{{ t.analyticsSub }}</p>
      </div>
      <div class="analytics-grid">
        <!-- Students per Class (bar, full width) -->
        <article class="panel span-2">
          <div class="panel-head">
            <span class="panel-title">
              <span class="panel-title-badge"><svg viewBox="0 0 24 24" v-html="icons.chart"></svg></span>
              {{ t.cards.spc.t }}
            </span>
          </div>
          <p class="panel-sub">{{ t.cards.spc.s }}</p>
          <div class="chart-box">
            <div v-if="analyticsLoading" class="chart-state"><span class="chart-spinner"></span></div>
            <div v-else-if="analyticsError" class="chart-state error">
              <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="9" /><path d="M12 8v5M12 16h.01" /></svg>
              <span>{{ analyticsError }}</span>
            </div>
            <div v-else-if="!hasClasses" class="chart-state">
              <span class="empty-badge sm"><svg viewBox="0 0 24 24" v-html="icons.chart"></svg></span>
              <span>{{ t.noData }}</span>
            </div>
            <BaseChart v-else type="bar" :data="spcData" :options="barOptions" />
          </div>
        </article>

        <!-- Users by Role (doughnut) -->
        <article class="panel span-1">
          <div class="panel-head">
            <span class="panel-title">
              <span class="panel-title-badge"><svg viewBox="0 0 24 24" v-html="icons.pie"></svg></span>
              {{ t.cards.roles.t }}
            </span>
          </div>
          <p class="panel-sub">{{ t.cards.roles.s }}</p>
          <div class="chart-box">
            <div v-if="analyticsLoading" class="chart-state"><span class="chart-spinner"></span></div>
            <div v-else-if="analyticsError" class="chart-state error">
              <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="9" /><path d="M12 8v5M12 16h.01" /></svg>
              <span>{{ analyticsError }}</span>
            </div>
            <div v-else-if="!hasUsers" class="chart-state">
              <span class="empty-badge sm"><svg viewBox="0 0 24 24" v-html="icons.pie"></svg></span>
              <span>{{ t.noData }}</span>
            </div>
            <BaseChart v-else type="doughnut" :data="rolesData" :options="doughnutOptions" />
          </div>
        </article>

        <!-- Classes by Level (doughnut) -->
        <article class="panel span-1">
          <div class="panel-head">
            <span class="panel-title">
              <span class="panel-title-badge"><svg viewBox="0 0 24 24" v-html="icons.layers"></svg></span>
              {{ t.cards.level.t }}
            </span>
          </div>
          <p class="panel-sub">{{ t.cards.level.s }}</p>
          <div class="chart-box">
            <div v-if="analyticsLoading" class="chart-state"><span class="chart-spinner"></span></div>
            <div v-else-if="analyticsError" class="chart-state error">
              <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="9" /><path d="M12 8v5M12 16h.01" /></svg>
              <span>{{ analyticsError }}</span>
            </div>
            <div v-else-if="!hasClasses" class="chart-state">
              <span class="empty-badge sm"><svg viewBox="0 0 24 24" v-html="icons.layers"></svg></span>
              <span>{{ t.noData }}</span>
            </div>
            <BaseChart v-else type="doughnut" :data="levelData" :options="doughnutOptions" />
          </div>
        </article>
      </div>
    </section>

  </div>
</template>

<style scoped>
.dashboard {
  --navy: var(--ds-navy);
  --navy-dark: var(--ds-navy-dark);
  --orange: var(--ds-orange);

  display: flex;
  flex-direction: column;
  gap: 2rem;
}

/* Gentle entrance for the main sections */
@keyframes rise {
  from { opacity: 0; transform: translateY(16px); }
  to { opacity: 1; transform: translateY(0); }
}

/* ---------- Hero ---------- */
.hero {
  position: relative;
  overflow: hidden;
  border-radius: 20px;
  padding: 0.95rem 1.5rem 1.05rem;
  color: #fff;
  animation: rise 0.5s ease both;
  /* Layered radial glows over a deep navy gradient — same treatment as the
     login brand panel and welcome hero, for a cohesive look. */
  background:
    radial-gradient(circle at 15% 25%, rgba(255, 255, 255, 0.12), transparent 40%),
    radial-gradient(circle at 90% 12%, rgba(242, 160, 61, 0.2), transparent 44%),
    radial-gradient(circle at 78% 96%, rgba(88, 130, 220, 0.45), transparent 48%),
    linear-gradient(135deg, #1b3a7a 0%, #1e295f 55%, #182247 100%);
  box-shadow: 0 18px 40px rgba(24, 34, 71, 0.28);
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
/* Soft orange glow blob in the corner */
.hero::after {
  content: '';
  position: absolute;
  top: -120px;
  inset-inline-end: -80px;
  width: 320px;
  height: 320px;
  z-index: 0;
  border-radius: 50%;
  background: radial-gradient(circle, rgba(242, 160, 61, 0.22), transparent 70%);
  filter: blur(8px);
  pointer-events: none;
}
/* Keep all hero content above the decorative layers */
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
  background: rgba(255, 255, 255, 0.12);
  border: 1px solid rgba(255, 255, 255, 0.18);
  backdrop-filter: blur(4px);
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.04em;
}
.live-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: #4ade80;
  box-shadow: 0 0 8px rgba(74, 222, 128, 0.9);
}
/* Small "fetching stats" chip shown while GET /api/dashboard/stats is in flight */
.loading-chip {
  text-transform: none;
  letter-spacing: 0;
}
.mini-spinner {
  width: 12px;
  height: 12px;
  border-radius: 50%;
  border: 2px solid rgba(255, 255, 255, 0.35);
  border-top-color: #fff;
  animation: dash-spin 0.7s linear infinite;
}
@keyframes dash-spin {
  to {
    transform: rotate(360deg);
  }
}
.hero-updated {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  color: rgba(255, 255, 255, 0.75);
}
.hero-updated svg {
  width: 15px;
  height: 15px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.8;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.hero-title {
  margin: 0.5rem 0 0.15rem;
  font-size: 1.45rem;
  font-weight: 800;
  letter-spacing: -0.01em;
}
.hero-name {
  color: #7cc0ff;
}
.hero-sub {
  margin: 0;
  color: rgba(255, 255, 255, 0.82);
  font-size: 0.9rem;
}

/* Activity bar */
.activity {
  margin: 0.75rem 0 0;
  max-width: 560px;
}
.activity-head {
  display: flex;
  justify-content: space-between;
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: rgba(255, 255, 255, 0.8);
  margin-bottom: 0.45rem;
}
.activity-track {
  height: 7px;
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.15);
  overflow: hidden;
}
.activity-fill {
  height: 100%;
  border-radius: 999px;
  background: linear-gradient(90deg, #38bdf8, #60a5fa);
  transition: width 0.6s ease;
}

/* Hero action buttons (sit top-right on wide screens) */
.hero-actions {
  position: absolute;
  top: 1.05rem;
  inset-inline-end: 1.5rem;
  display: flex;
  gap: 0.6rem;
}
.hero-btn {
  display: inline-flex;
  align-items: center;
  gap: 0.45rem;
  padding: 0.6rem 1rem;
  border-radius: 12px;
  border: 1px solid rgba(255, 255, 255, 0.18);
  background: rgba(255, 255, 255, 0.1);
  color: #fff;
  font-size: 0.88rem;
  font-weight: 700;
  font-family: inherit;
  text-decoration: none;
  cursor: pointer;
  transition: background 0.2s ease, transform 0.2s ease;
}
.hero-btn:hover {
  background: rgba(255, 255, 255, 0.2);
  transform: translateY(-1px);
}
.hero-btn.accent {
  position: relative;
  overflow: hidden;
  border: none;
  background: linear-gradient(135deg, #38bdf8, #3b82f6);
  box-shadow: 0 8px 20px rgba(56, 189, 248, 0.4);
}
/* Light sheen that sweeps across the accent button on hover */
.hero-btn.accent::after {
  content: '';
  position: absolute;
  top: 0;
  inset-inline-start: -75%;
  width: 50%;
  height: 100%;
  background: linear-gradient(120deg, transparent, rgba(255, 255, 255, 0.45), transparent);
  transform: skewX(-20deg);
  transition: inset-inline-start 0.6s ease;
  pointer-events: none;
}
.hero-btn.accent:hover::after {
  inset-inline-start: 125%;
}
.hero-btn svg {
  width: 17px;
  height: 17px;
  fill: none;
  stroke: currentColor;
  stroke-width: 2;
  stroke-linecap: round;
  stroke-linejoin: round;
}

/* Colored stat cards row inside the welcome box.
   auto-fit + minmax lets all 5 cards sit in one row on wide screens and wrap
   gracefully on narrower ones without manual breakpoints. */
.hero-stats {
  margin-top: 0.72rem;
  display: grid;
  grid-template-columns: repeat(5, minmax(0, 1fr));
  gap: 0.7rem;
}

/* ---------- Generic block ---------- */
.block-head h3 {
  position: relative;
  margin: 0;
  padding-inline-start: 0.85rem;
  font-size: 1.35rem;
  font-weight: 800;
  color: #0f2444;
  letter-spacing: -0.01em;
}
/* Short navy→orange accent bar before each section heading */
.block-head h3::before {
  content: '';
  position: absolute;
  inset-inline-start: 0;
  top: 50%;
  transform: translateY(-50%);
  width: 5px;
  height: 1.05rem;
  border-radius: 3px;
  background: linear-gradient(180deg, var(--navy), var(--orange));
}
.block-head p {
  margin: 0.2rem 0 1.1rem;
  font-size: 0.9rem;
  color: #6b7280;
}
/* Analytics — bar spans the full width (row 1); the two doughnuts share row 2 */
.analytics-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 1.25rem;
}
.block {
  animation: rise 0.5s ease both;
  animation-delay: 0.12s;
}
.panel {
  position: relative;
  overflow: hidden;
  background: #fff;
  border: 1px solid #eaeef6;
  border-radius: 18px;
  padding: 1.4rem 1.5rem;
  min-height: 220px;
  display: flex;
  flex-direction: column;
  box-shadow: 0 8px 22px rgba(30, 41, 59, 0.05);
  transition: transform 0.25s ease, box-shadow 0.25s ease, border-color 0.25s ease;
}
/* Navy→orange accent line that grows across the top of a panel on hover */
.panel::before {
  content: '';
  position: absolute;
  top: 0;
  inset-inline-start: 0;
  width: 100%;
  height: 3px;
  background: linear-gradient(90deg, var(--navy), var(--orange));
  transform: scaleX(0);
  transform-origin: left;
  transition: transform 0.35s ease;
}
.panel:hover::before {
  transform: scaleX(1);
}
.panel:hover {
  transform: translateY(-3px);
  border-color: #dbe3f2;
  box-shadow: 0 18px 38px rgba(30, 41, 59, 0.1);
}
.span-2 { grid-column: span 2; }
.panel-title {
  display: inline-flex;
  align-items: center;
  gap: 0.65rem;
  font-size: 1.05rem;
  font-weight: 800;
  color: #0f2444;
}
/* Tinted rounded badge holding the panel icon */
.panel-title-badge {
  width: 34px;
  height: 34px;
  flex-shrink: 0;
  border-radius: 10px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: var(--navy);
  background: linear-gradient(135deg, rgba(30, 76, 154, 0.12), rgba(242, 160, 61, 0.14));
}
.panel-title-badge svg {
  width: 18px;
  height: 18px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.9;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.panel-sub {
  margin: 0.3rem 0 0;
  font-size: 0.85rem;
  color: #6b7280;
}
.empty {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 0.75rem;
  color: #aab4c8;
}
.empty-badge {
  width: 66px;
  height: 66px;
  border-radius: 50%;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  background: radial-gradient(circle, #eef2fb, #e6ecf7);
  color: #b6bfd0;
}
.empty-badge svg {
  width: 30px;
  height: 30px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.5;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.empty span {
  font-size: 0.9rem;
  font-weight: 600;
}

/* ---------- Charts ---------- */
/* The chart fills the remaining card space; a min-height gives Chart.js a box to size
   the responsive canvas into (maintainAspectRatio: false) without overflowing. */
.chart-box {
  position: relative;
  flex: 1;
  min-height: 260px;
  margin-top: 1rem;
}
.span-2 .chart-box {
  min-height: 300px;
}
/* Per-card loading / error / empty state (shown in place of the chart) */
.chart-state {
  height: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 0.7rem;
  color: #aab4c8;
  font-size: 0.9rem;
  font-weight: 600;
  text-align: center;
}
.chart-state.error {
  color: #b91c1c;
}
.chart-state.error svg {
  width: 30px;
  height: 30px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.7;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.chart-spinner {
  width: 30px;
  height: 30px;
  border-radius: 50%;
  border: 3px solid #e2e8f0;
  border-top-color: var(--navy);
  animation: dash-spin 0.8s linear infinite;
}
.empty-badge.sm {
  width: 52px;
  height: 52px;
}
.empty-badge.sm svg {
  width: 24px;
  height: 24px;
}
@media (prefers-reduced-motion: reduce) {
  .chart-spinner {
    animation-duration: 1.6s;
  }
}

/* ---------- Responsive ---------- */
@media (max-width: 1100px) {
  .hero-stats {
    grid-template-columns: repeat(3, 1fr);
  }
  .analytics-grid {
    grid-template-columns: 1fr;
  }
  .span-2 {
    grid-column: span 1;
  }
}
@media (max-width: 720px) {
  .hero-actions {
    position: static;
    margin-top: 1.1rem;
  }
  .hero-stats {
    grid-template-columns: repeat(2, 1fr);
  }
}
@media (max-width: 420px) {
  .hero-stats {
    grid-template-columns: 1fr;
  }
}

/* Respect users who prefer reduced motion */
@media (prefers-reduced-motion: reduce) {
  .hero,
  .block {
    animation: none;
  }
}
</style>
