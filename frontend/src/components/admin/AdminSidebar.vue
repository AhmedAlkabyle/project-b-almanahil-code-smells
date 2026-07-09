<script setup>
import { computed } from 'vue'
import { useLanguageStore } from '../../stores/language'
import { useUiStore } from '../../stores/ui'
import schoolLogo from '../../assets/school-logo.jpg.jpg'
import { icons } from './icons'

const language = useLanguageStore()
const ui = useUiStore()

// Slim icon-only rail vs. the full menu (persisted in the UI store).
const collapsed = computed(() => ui.sidebarCollapsed)
const collapseLabel = computed(() =>
  language.isArabic
    ? collapsed.value ? 'توسيع القائمة' : 'طيّ القائمة'
    : collapsed.value ? 'Expand menu' : 'Collapse menu'
)

// ---- Bilingual labels (reuses the existing per-component i18n pattern) ----
const content = {
  en: {
    brandName: 'Almanahil Libyan School',
    brandSub: 'SCHOOL MANAGEMENT SYSTEM'
  },
  ar: {
    brandName: 'مدرسة المناهل الليبية',
    brandSub: 'نظام إدارة المدرسة'
  }
}
const t = computed(() => content[language.lang])

// ---- Navigation grouped into sections for a clearer, more professional menu ----
const navGroups = computed(() => [
  {
    label: language.isArabic ? 'الرئيسية' : 'Main',
    items: [
      { to: '/admin/dashboard', icon: icons.dashboard, label: language.isArabic ? 'الرئيسية' : 'Dashboard' },
      { to: '/admin/users', icon: icons.users, label: language.isArabic ? 'إدارة المستخدمين' : 'Manage Users' }
    ]
  },
  {
    label: language.isArabic ? 'الأكاديمية' : 'Academics',
    items: [
      { to: '/admin/classes', icon: icons.classes, label: language.isArabic ? 'إدارة الصفوف' : 'Manage Classes' },
      { to: '/admin/subjects', icon: icons.subjects, label: language.isArabic ? 'إدارة المواد' : 'Manage Subjects' },
      { to: '/admin/teacher-assignments', icon: icons.assignments, label: language.isArabic ? 'تعيينات المعلمين' : 'Teacher Assignments' },
      { to: '/admin/events', icon: icons.events, label: language.isArabic ? 'إدارة الفعاليات' : 'Manage Events' }
    ]
  },
  {
    label: language.isArabic ? 'التقارير' : 'Reports',
    items: [
      { to: '/admin/grades', icon: icons.grades, label: language.isArabic ? 'عرض الدرجات' : 'View Grades' },
      { to: '/admin/attendance', icon: icons.attendance, label: language.isArabic ? 'تقارير الحضور' : 'Attendance Reports' }
    ]
  }
])
</script>

<template>
  <aside class="sidebar" :class="{ rtl: language.isArabic, collapsed }">
    <!-- Brand -->
    <div class="brand" :title="collapsed ? t.brandName : null">
      <span class="brand-logo-wrap">
        <img :src="schoolLogo" alt="" class="brand-logo" />
      </span>
      <div class="brand-text">
        <span class="brand-name">{{ t.brandName }}</span>
        <span class="brand-sub">{{ t.brandSub }}</span>
      </div>
    </div>

    <!-- Navigation (grouped) -->
    <nav class="nav">
      <div v-for="group in navGroups" :key="group.label" class="nav-group">
        <p class="nav-section"><span>{{ group.label }}</span></p>
        <router-link
          v-for="item in group.items"
          :key="item.to"
          :to="item.to"
          class="nav-item"
          :title="collapsed ? item.label : null"
        >
          <span class="nav-icon"><svg viewBox="0 0 24 24" v-html="item.icon"></svg></span>
          <span class="nav-text">{{ item.label }}</span>
          <span class="nav-chevron" aria-hidden="true">
            <svg viewBox="0 0 24 24" v-html="icons.arrow"></svg>
          </span>
        </router-link>
      </div>
    </nav>

    <!-- Footer: collapse / expand the menu (mirrors the top-bar toggle) -->
    <div class="nav-footer">
      <button
        type="button"
        class="collapse-btn"
        @click="ui.toggleSidebar()"
        :title="collapseLabel"
        :aria-label="collapseLabel"
      >
        <span class="collapse-ic" :class="{ flip: collapsed !== language.isArabic }">
          <svg viewBox="0 0 24 24" v-html="icons.chevronsLeft"></svg>
        </span>
        <span class="collapse-text">{{ collapseLabel }}</span>
      </button>
    </div>
  </aside>
</template>

<style scoped>
.sidebar {
  --navy: var(--ds-navy);
  --orange: var(--ds-orange);

  position: sticky;
  top: 0;
  align-self: flex-start;
  height: 100vh;
  width: 260px;
  flex-shrink: 0;
  display: flex;
  flex-direction: column;
  padding: 1rem 0.85rem 1rem;
  color: #cdd6ea;
  /* Deep navy shell with a soft blue glow up top + a faint warm glow at the base */
  background:
    radial-gradient(circle at 22% -8%, rgba(79, 130, 220, 0.22), transparent 44%),
    radial-gradient(circle at 90% 108%, rgba(242, 160, 61, 0.08), transparent 42%),
    linear-gradient(180deg, #0f1e3d 0%, #0b1730 100%);
  border-inline-end: 1px solid rgba(255, 255, 255, 0.06);
  /* The shell never scrolls: the brand stays pinned at the top and the collapse
     footer pinned at the bottom, so the collapse button is always reachable
     without scrolling. Only the nav list (below) scrolls when it's too tall. */
  overflow: hidden;
  font-family: 'Segoe UI', system-ui, -apple-system, sans-serif;
  /* Smoothly slide between the full menu and the slim rail */
  transition: width 0.28s ease, padding 0.28s ease;
}

/* Brand */
.brand {
  flex-shrink: 0;
  display: flex;
  align-items: center;
  gap: 0.6rem;
  padding: 0.2rem 0.35rem 0.8rem;
  margin-bottom: 0.4rem;
  border-bottom: 1px solid rgba(255, 255, 255, 0.07);
}
.brand-logo {
  width: 44px;
  height: 44px;
  object-fit: contain;
  border-radius: 12px;
  /* Bright white tile with a soft shadow, like the welcome/login logo */
  background: #fff;
  padding: 5px;
  box-shadow: 0 6px 16px rgba(0, 0, 0, 0.32);
  transition: transform 0.25s ease, box-shadow 0.25s ease;
}
/* Gentle up/down float — same subtle treatment as the login/welcome logo.
   Kept on a wrapper so the hover scale (on the img) doesn't fight the float. */
.brand-logo-wrap {
  display: inline-flex;
  flex-shrink: 0;
  animation: sidebar-logo-float 4s ease-in-out infinite;
}
.brand:hover .brand-logo {
  transform: scale(1.06);
  box-shadow: 0 8px 22px rgba(242, 160, 61, 0.45);
}
@keyframes sidebar-logo-float {
  0%,
  100% {
    transform: translateY(0);
  }
  50% {
    transform: translateY(-4px);
  }
}
@media (prefers-reduced-motion: reduce) {
  .brand-logo-wrap {
    animation: none;
  }
}
.brand-text {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
  min-width: 0;
  line-height: 1.18;
}
.brand-name {
  color: #fff;
  font-weight: 800;
  font-size: 0.84rem;
  letter-spacing: -0.01em;
  white-space: nowrap;
}
.brand-sub {
  font-size: 0.53rem;
  letter-spacing: 0.12em;
  color: #8494b8;
  font-weight: 600;
  white-space: nowrap;
}

/* Nav — the only scrolling region. The negative inline margin + matching padding
   widen its scroll box out to the sidebar edges, so the active item's edge accent
   (which sits in the gutter) isn't clipped by the scroll overflow. */
.nav {
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
  overflow-y: auto;
  margin-inline: -0.85rem;
  padding-inline: 0.85rem;
  scrollbar-width: none; /* Firefox */
  -ms-overflow-style: none; /* legacy Edge/IE */
}
.nav::-webkit-scrollbar {
  width: 0;
  height: 0;
  display: none;
}
.nav-group {
  display: flex;
  flex-direction: column;
  gap: 0.2rem;
}
.nav-group + .nav-group {
  margin-top: 0.4rem;
}
.nav-section {
  margin: 0.45rem 0.75rem 0.3rem;
  font-size: 0.62rem;
  font-weight: 800;
  letter-spacing: 0.16em;
  text-transform: uppercase;
  color: #63739c;
}
.nav-item {
  position: relative;
  display: flex;
  align-items: center;
  gap: 0.7rem;
  padding: 0.46rem 0.6rem;
  border-radius: 12px;
  color: #b9c4de;
  text-decoration: none;
  font-size: 0.9rem;
  font-weight: 600;
  transition: background 0.2s ease, color 0.2s ease;
}
.nav-item:hover {
  background: rgba(255, 255, 255, 0.05);
  color: #fff;
}
/* Rounded icon tile — the main visual upgrade; lights up on hover/active */
.nav-icon {
  width: 34px;
  height: 34px;
  flex-shrink: 0;
  border-radius: 9px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: #aab6d6;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid rgba(255, 255, 255, 0.07);
  transition: background 0.2s ease, border-color 0.2s ease, color 0.2s ease;
}
.nav-icon svg {
  width: 18px;
  height: 18px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.85;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.nav-item:hover .nav-icon {
  background: rgba(255, 255, 255, 0.1);
  border-color: rgba(255, 255, 255, 0.14);
  color: #fff;
}
.nav-text {
  flex: 1;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
/* Chevron that fades in on hover / active */
.nav-chevron {
  display: inline-flex;
  flex-shrink: 0;
  opacity: 0;
  color: rgba(255, 255, 255, 0.55);
  transition: opacity 0.2s ease;
}
.nav-chevron svg {
  width: 15px;
  height: 15px;
  fill: none;
  stroke: currentColor;
  stroke-width: 2.4;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.nav-item:hover .nav-chevron {
  opacity: 0.7;
}
/* Flip the chevron so it points inward in RTL */
.sidebar.rtl .nav-chevron {
  transform: scaleX(-1);
}

/* Active link: highlighted gradient pill + inline orange accent + lit icon tile */
.nav-item.router-link-active {
  background: linear-gradient(135deg, var(--navy), #2f63ba);
  color: #fff;
  box-shadow: 0 10px 22px rgba(30, 76, 154, 0.45);
}
.nav-item.router-link-active::before {
  content: '';
  position: absolute;
  inset-inline-start: -0.85rem;
  top: 50%;
  transform: translateY(-50%);
  width: 4px;
  height: 55%;
  border-radius: 0 3px 3px 0;
  background: var(--orange);
}
.nav-item.router-link-active .nav-icon {
  background: rgba(255, 255, 255, 0.2);
  border-color: rgba(255, 255, 255, 0.3);
  color: #fff;
}
.nav-item.router-link-active .nav-chevron {
  opacity: 1;
  color: #fff;
}

/* ---- Footer: collapse / expand control ----
   A non-shrinking flex item, so it's always fixed at the bottom of the sidebar
   and fully visible — no scrolling needed to reach the collapse button. */
.nav-footer {
  flex-shrink: 0;
  margin-top: 0.5rem;
  padding-top: 0.6rem;
  background: #0b1730;
  border-top: 1px solid rgba(255, 255, 255, 0.07);
}
.collapse-btn {
  width: 100%;
  display: flex;
  align-items: center;
  gap: 0.7rem;
  padding: 0.5rem 0.6rem;
  border: none;
  border-radius: 12px;
  background: transparent;
  color: #94a2c6;
  font-family: inherit;
  font-size: 0.82rem;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.2s ease, color 0.2s ease;
}
.collapse-btn:hover {
  background: rgba(255, 255, 255, 0.05);
  color: #fff;
}
.collapse-ic {
  width: 34px;
  height: 34px;
  flex-shrink: 0;
  border-radius: 9px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid rgba(255, 255, 255, 0.07);
  transition: transform 0.28s ease, background 0.2s ease, border-color 0.2s ease;
}
.collapse-btn:hover .collapse-ic {
  background: rgba(255, 255, 255, 0.1);
  border-color: rgba(255, 255, 255, 0.14);
}
.collapse-ic svg {
  width: 17px;
  height: 17px;
  fill: none;
  stroke: currentColor;
  stroke-width: 2.2;
  stroke-linecap: round;
  stroke-linejoin: round;
}
/* Point the double-chevron the right way: inward to collapse, outward to expand,
   mirrored for RTL (flip whenever collapsed state differs from Arabic). */
.collapse-ic.flip {
  transform: scaleX(-1);
}

/* ---- Collapsed (slim icon rail) ---- */
.sidebar.collapsed {
  width: 82px;
  padding-inline: 0.7rem;
}
.sidebar.collapsed .brand {
  justify-content: center;
  padding-inline: 0;
}
.sidebar.collapsed .brand-text {
  display: none;
}
/* Section labels turn into subtle divider lines between icon groups */
.sidebar.collapsed .nav-section {
  height: 1px;
  margin: 0.65rem 0.55rem 0.6rem;
  background: rgba(255, 255, 255, 0.09);
  overflow: hidden;
}
.sidebar.collapsed .nav-section span {
  display: none;
}
.sidebar.collapsed .nav-item {
  justify-content: center;
  gap: 0;
  padding-inline: 0;
}
.sidebar.collapsed .nav-text,
.sidebar.collapsed .nav-chevron,
.sidebar.collapsed .collapse-text {
  display: none;
}
/* The gradient pill already marks the active item — drop the edge accent so it
   never clips against the narrow rail. */
.sidebar.collapsed .nav-item.router-link-active::before {
  display: none;
}
.sidebar.collapsed .collapse-btn {
  justify-content: center;
  padding-inline: 0;
}
</style>
