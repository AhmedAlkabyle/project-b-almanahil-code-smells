<script setup>
// This is the frame (shell) for every Teacher page. It provides the sidebar,
// the top bar (greeting, account menu, language toggle), and the chatbot button
// that wrap around whatever teacher page is currently shown. The actual page
// content is dropped into the <router-view> in the middle.
import { computed, ref, onMounted, onBeforeUnmount } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { useLanguageStore } from '../stores/language'
import { useUiStore } from '../stores/ui'
import TeacherSidebar from '../components/teacher/TeacherSidebar.vue'
import ChatbotPanel from '../components/ChatbotPanel.vue'
import LanguageToggle from '../components/LanguageToggle.vue'
import { icons } from '../components/admin/icons'

const router = useRouter()
const auth = useAuthStore()
const language = useLanguageStore()
const ui = useUiStore()

const roleLabel = computed(() => (language.isArabic ? 'معلم' : 'Teacher'))
const chatbotLabel = computed(() => (language.isArabic ? 'اسأل المساعد الذكي' : 'Ask our chatbot'))

const displayName = computed(() => auth.user?.firstName || 'Teacher')
const email = computed(() => auth.user?.email || '')
const initial = computed(() => displayName.value.charAt(0).toUpperCase())
const avatar = computed(() => auth.avatar)

// Tooltip / a11y label for the sidebar rail toggle (reflects the current state).
const railToggleLabel = computed(() =>
  language.isArabic
    ? ui.sidebarCollapsed ? 'توسيع القائمة الجانبية' : 'طيّ القائمة الجانبية'
    : ui.sidebarCollapsed ? 'Expand sidebar' : 'Collapse sidebar'
)

// ---- Greeting + date shown on the leading side of the top bar (localized) ----
const now = new Date()
const greeting = computed(() => {
  const h = now.getHours()
  if (language.isArabic) return h < 12 ? 'صباح الخير' : 'مساء الخير'
  if (h < 12) return 'Good morning'
  if (h < 18) return 'Good afternoon'
  return 'Good evening'
})
const todayStr = computed(() =>
  now.toLocaleDateString(language.isArabic ? 'ar' : 'en-US', {
    weekday: 'long',
    day: 'numeric',
    month: 'long'
  })
)
// Day (sun) vs evening/early-morning (moon) — drives the greeting badge icon.
const isNight = computed(() => {
  const h = now.getHours()
  return h < 6 || h >= 18
})

// ---- Account dropdown menu ----
const menuLabels = computed(() =>
  language.isArabic
    ? { profile: 'ملفي الشخصي', logout: 'تسجيل الخروج' }
    : { profile: 'My Profile', logout: 'Logout' }
)

const menuOpen = ref(false)
const userMenu = ref(null)

function toggleMenu() {
  menuOpen.value = !menuOpen.value
}
function closeMenu() {
  menuOpen.value = false
}

// Close the menu when clicking outside it or pressing Escape.
function onDocClick(e) {
  if (menuOpen.value && userMenu.value && !userMenu.value.contains(e.target)) {
    menuOpen.value = false
  }
}
function onKey(e) {
  if (e.key === 'Escape') menuOpen.value = false
}
onMounted(() => {
  document.addEventListener('click', onDocClick)
  document.addEventListener('keydown', onKey)
})
onBeforeUnmount(() => {
  document.removeEventListener('click', onDocClick)
  document.removeEventListener('keydown', onKey)
})

function goProfile() {
  closeMenu()
  router.push('/teacher/profile')
}
// Reuse Module 1 logout: clear auth, then replace to /login (drops history).
function handleLogout() {
  closeMenu()
  auth.logout()
  router.replace('/login')
}

// Menu-based help chatbot (Module 6) — toggles the ChatbotPanel open/closed.
const chatbotOpen = ref(false)
function askChatbot() {
  chatbotOpen.value = !chatbotOpen.value
}
</script>

<template>
  <div class="teacher-shell" :dir="language.dir" :lang="language.lang">
    <!-- Decorative floating circles (welcome-hero style), behind everything -->
    <div class="teacher-decor" aria-hidden="true">
      <span class="a-circle c1"></span>
      <span class="a-circle c2"></span>
      <span class="a-circle c3"></span>
      <span class="a-circle c4"></span>
    </div>

    <TeacherSidebar />

    <!-- Module 6: menu-based help chatbot (opens from the top-bar "Ask our chatbot" button) -->
    <ChatbotPanel :open="chatbotOpen" @close="chatbotOpen = false" />

    <div class="teacher-main">
      <!-- Top bar — same clean utility bar as admin. Leading edge: the sidebar
           toggle + greeting/date. Trailing edge: the chatbot shortcut, the account
           menu (profile / logout) and the language toggle. Flips naturally in Arabic. -->
      <header class="topbar" :dir="language.dir">
        <!-- Leading cluster: sidebar toggle + greeting/date -->
        <div class="topbar-lead">
          <button
            type="button"
            class="rail-toggle"
            @click="ui.toggleSidebar()"
            :title="railToggleLabel"
            :aria-label="railToggleLabel"
            :aria-pressed="ui.sidebarCollapsed"
          >
            <svg viewBox="0 0 24 24" v-html="icons.menu"></svg>
          </button>

          <!-- Greeting + date (shown on every teacher page) -->
          <div class="topbar-greeting">
            <span class="tg-badge" :class="{ night: isNight }">
              <svg v-if="isNight" viewBox="0 0 24 24"><path d="M21 12.8A8.5 8.5 0 1 1 11.2 3a6.5 6.5 0 0 0 9.8 9.8Z" /></svg>
              <svg v-else viewBox="0 0 24 24"><circle cx="12" cy="12" r="4.2" /><path d="M12 2.5v2M12 19.5v2M4.6 4.6l1.4 1.4M18 18l1.4 1.4M2.5 12h2M19.5 12h2M4.6 19.4l1.4-1.4M18 6l1.4-1.4" /></svg>
            </span>
            <div class="tg-text">
              <span class="tg-hi">{{ greeting }}, <strong>{{ displayName }}</strong></span>
              <span class="tg-date">{{ todayStr }}</span>
            </div>
          </div>
        </div>

        <div class="topbar-actions">
          <!-- Ask our chatbot — placeholder for the AI assistant (wired up later) -->
          <button type="button" class="chatbot-btn" @click="askChatbot">
            <span class="chatbot-ic">
              <svg viewBox="0 0 24 24"><path d="M12 3l1.7 4.3L18 9l-4.3 1.7L12 15l-1.7-4.3L6 9l4.3-1.7L12 3Z" /><path d="M18.6 13.6l.65 1.75 1.75.65-1.75.65-.65 1.75-.65-1.75-1.75-.65 1.75-.65.65-1.75Z" /></svg>
            </span>
            <span class="chatbot-text">{{ chatbotLabel }}</span>
          </button>

          <!-- Account menu -->
          <div class="topbar-user-wrap" ref="userMenu">
            <button
              type="button"
              class="topbar-user"
              :class="{ open: menuOpen }"
              @click="toggleMenu"
              :aria-expanded="menuOpen"
              aria-haspopup="true"
            >
              <span class="topbar-avatar">
                <img v-if="avatar" :src="avatar" alt="" />
                <template v-else>{{ initial }}</template>
              </span>
              <div class="topbar-user-text">
                <span class="topbar-user-name">{{ displayName }}</span>
                <span class="topbar-user-role">{{ roleLabel }}</span>
              </div>
              <svg class="topbar-caret" viewBox="0 0 24 24"><path d="M6 9l6 6 6-6" /></svg>
            </button>

            <Transition name="menu">
              <div v-if="menuOpen" class="user-menu" role="menu">
                <div class="user-menu-head">
                  <span class="user-menu-avatar">
                    <img v-if="avatar" :src="avatar" alt="" />
                    <template v-else>{{ initial }}</template>
                  </span>
                  <div class="user-menu-id">
                    <span class="user-menu-name">{{ displayName }}</span>
                    <span v-if="email" class="user-menu-email">{{ email }}</span>
                    <span class="user-menu-role">{{ roleLabel }}</span>
                  </div>
                </div>
                <div class="user-menu-sep"></div>
                <button type="button" class="user-menu-item" role="menuitem" @click="goProfile">
                  <svg viewBox="0 0 24 24"><circle cx="12" cy="8" r="4" /><path d="M4 21a8 8 0 0 1 16 0" /></svg>
                  <span>{{ menuLabels.profile }}</span>
                </button>
                <button type="button" class="user-menu-item danger" role="menuitem" @click="handleLogout">
                  <svg viewBox="0 0 24 24" v-html="icons.logout"></svg>
                  <span>{{ menuLabels.logout }}</span>
                </button>
              </div>
            </Transition>
          </div>

          <!-- Language toggle -->
          <LanguageToggle class="topbar-lang" />
        </div>
      </header>

      <!-- Routed page content -->
      <main class="teacher-content">
        <router-view />
      </main>
    </div>
  </div>
</template>

<style scoped>
/* Neutralize the leftover Vite starter constraints on #app so the layout truly
   fills the page and connects to the sidebar (same as the admin layout). */
:global(html),
:global(body),
:global(#app) {
  margin: 0;
  padding: 0;
  width: 100%;
}
:global(#app) {
  max-width: none;
  min-height: 100vh;
  border: none;
  text-align: start;
  display: block;
}

.teacher-shell {
  position: relative;
  display: flex;
  min-height: 100vh;
  /* Soft, pale glow wash over a light green-tinted gradient — the teacher
     counterpart to the admin shell (navy → green). */
  background:
    radial-gradient(circle at 68% 4%, rgba(16, 163, 74, 0.06), transparent 22%),
    radial-gradient(circle at 96% 26%, rgba(242, 160, 61, 0.06), transparent 22%),
    linear-gradient(180deg, #f3faf6 0%, #edf6f0 100%);
  font-family: 'Segoe UI', system-ui, -apple-system, sans-serif;
}

/* ---- Decorative floating circles (welcome-hero style, tinted for the light
   green teacher canvas). Fixed, behind-everything layer. */
.teacher-decor {
  position: fixed;
  inset: 0;
  z-index: 0;
  overflow: hidden;
  pointer-events: none;
}
.a-circle {
  position: absolute;
  border-radius: 50%;
  filter: blur(6px);
}
.a-circle.c1 {
  width: 460px;
  height: 460px;
  top: -150px;
  inset-inline-end: -120px;
  background: rgba(242, 160, 61, 0.08);
  animation: teacher-drift 22s ease-in-out infinite;
}
.a-circle.c2 {
  width: 360px;
  height: 360px;
  bottom: -130px;
  inset-inline-start: 300px;
  background: rgba(16, 163, 74, 0.06);
  animation: teacher-drift 26s ease-in-out infinite reverse;
}
.a-circle.c3 {
  width: 240px;
  height: 240px;
  top: 24%;
  inset-inline-start: 42%;
  background: rgba(52, 211, 153, 0.12);
  animation: teacher-drift 30s ease-in-out infinite;
}
/* Thin outlined ring (like the welcome hero's .circle-4) */
.a-circle.c4 {
  width: 220px;
  height: 220px;
  bottom: 12%;
  inset-inline-end: 8%;
  background: transparent;
  border: 1.5px solid rgba(16, 163, 74, 0.14);
  filter: none;
  animation: teacher-drift 34s ease-in-out infinite reverse;
}
@keyframes teacher-drift {
  0%,
  100% {
    transform: translate(0, 0) scale(1);
  }
  50% {
    transform: translate(26px, 20px) scale(1.06);
  }
}
@media (prefers-reduced-motion: reduce) {
  .a-circle {
    animation: none;
  }
}
.teacher-shell[lang='ar'] {
  font-family: 'Segoe UI', 'Tahoma', system-ui, sans-serif;
}
.teacher-main {
  position: relative;
  z-index: 1;
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
}

/* Top bar */
.topbar {
  position: sticky;
  top: 0;
  z-index: 20;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding: 0.8rem 2rem;
  background: rgba(255, 255, 255, 0.82);
  backdrop-filter: blur(14px);
  -webkit-backdrop-filter: blur(14px);
  border-bottom: 1px solid #e4efe9;
  box-shadow: 0 4px 20px rgba(15, 54, 36, 0.05);
}
/* Leading cluster: rail toggle + greeting */
.topbar-lead {
  display: flex;
  align-items: center;
  gap: 0.85rem;
  min-width: 0;
}
/* Sidebar collapse/expand toggle (mirrors the one in the sidebar footer) */
.rail-toggle {
  width: 42px;
  height: 42px;
  flex-shrink: 0;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border-radius: 12px;
  border: 1px solid #e0ede7;
  background: #fff;
  color: #15683f;
  cursor: pointer;
  box-shadow: 0 1px 2px rgba(15, 54, 36, 0.04);
  transition: background 0.2s ease, border-color 0.2s ease, color 0.2s ease, transform 0.2s ease;
}
.rail-toggle:hover {
  background: #eef8f2;
  border-color: #cfe7db;
  color: #16a34a;
  transform: translateY(-1px);
}
.rail-toggle:active {
  transform: translateY(0);
}
.rail-toggle svg {
  width: 20px;
  height: 20px;
  fill: none;
  stroke: currentColor;
  stroke-width: 2;
  stroke-linecap: round;
  stroke-linejoin: round;
}
/* Leading greeting + date */
.topbar-greeting {
  display: flex;
  align-items: center;
  gap: 0.7rem;
  min-width: 0;
}
.tg-badge {
  width: 38px;
  height: 38px;
  flex-shrink: 0;
  border-radius: 12px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: #c07c16;
  background: linear-gradient(135deg, #fff2db, #ffe7c4);
}
.tg-badge.night {
  color: #4f46e5;
  background: linear-gradient(135deg, #eef0ff, #e6e9ff);
}
.tg-badge svg {
  width: 19px;
  height: 19px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.9;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.tg-text {
  display: flex;
  flex-direction: column;
  line-height: 1.25;
  min-width: 0;
}
.tg-hi {
  font-size: 0.95rem;
  font-weight: 700;
  color: #143a2a;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.tg-hi strong {
  color: #16a34a;
  font-weight: 800;
}
.tg-date {
  font-size: 0.76rem;
  color: #82998e;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.topbar-actions {
  display: flex;
  align-items: center;
  gap: 0.7rem;
}
.topbar-lang {
  flex-shrink: 0;
}
/* Soften the (dark) language pill for the light top bar */
.topbar-lang :deep(.lang-toggle) {
  border-color: rgba(16, 163, 74, 0.22);
}

/* Ask-our-chatbot button (identical AI-accent treatment to the admin side) */
.chatbot-btn {
  display: inline-flex;
  align-items: center;
  gap: 0.55rem;
  padding-block: 0.4rem;
  padding-inline: 0.45rem 1rem;
  border: 1px solid #e2e6f6;
  border-radius: 999px;
  /* Soft indigo tint marks this as the special "AI assistant" action */
  background: linear-gradient(135deg, #eff3ff, #f3f0ff);
  color: #3730a3;
  font-family: inherit;
  font-size: 0.9rem;
  font-weight: 700;
  cursor: pointer;
  transition: border-color 0.2s ease, box-shadow 0.2s ease, transform 0.2s ease;
}
.chatbot-btn:hover {
  border-color: #c7cdf2;
  box-shadow: 0 8px 20px rgba(79, 70, 229, 0.16);
  transform: translateY(-1px);
}
.chatbot-ic {
  width: 30px;
  height: 30px;
  flex-shrink: 0;
  border-radius: 50%;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: #fff;
  background: linear-gradient(135deg, #4f46e5, #3b6fe0);
  box-shadow: 0 4px 10px rgba(79, 70, 229, 0.35);
}
.chatbot-ic svg {
  width: 17px;
  height: 17px;
  fill: currentColor;
}
.chatbot-text {
  white-space: nowrap;
}
.topbar-user-wrap {
  position: relative;
}
.topbar-user {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  padding-block: 0.3rem;
  padding-inline: 0.35rem 0.7rem;
  border-radius: 999px;
  border: 1px solid #e7f1eb;
  background: #fff;
  box-shadow: 0 1px 2px rgba(15, 54, 36, 0.04);
  font-family: inherit;
  cursor: pointer;
  transition: background 0.2s ease, border-color 0.2s ease, box-shadow 0.2s ease, transform 0.2s ease;
}
.topbar-user:hover,
.topbar-user.open {
  border-color: #d3e8dd;
  box-shadow: 0 8px 18px rgba(15, 54, 36, 0.1);
  transform: translateY(-1px);
}
.topbar-avatar {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  overflow: hidden;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-weight: 800;
  color: #fff;
  background: linear-gradient(135deg, #16a34a, #12b981);
  box-shadow: 0 0 0 2px #fff, 0 0 0 4px rgba(16, 163, 74, 0.25);
}
.topbar-avatar img,
.user-menu-avatar img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}
.topbar-user-text {
  display: flex;
  flex-direction: column;
  line-height: 1.25;
  text-align: start;
}
.topbar-user-name {
  font-weight: 700;
  font-size: 0.92rem;
  color: #0f2a1e;
}
.topbar-user-role {
  font-size: 0.78rem;
  color: #6b8578;
}
.topbar-caret {
  width: 16px;
  height: 16px;
  fill: none;
  stroke: #9ab3a6;
  stroke-width: 2.2;
  stroke-linecap: round;
  stroke-linejoin: round;
  transition: transform 0.25s ease;
}
.topbar-user.open .topbar-caret {
  transform: rotate(180deg);
}

/* ---- Account dropdown menu ---- */
.user-menu {
  position: absolute;
  top: calc(100% + 0.6rem);
  inset-inline-end: 0;
  z-index: 40;
  min-width: 250px;
  padding: 0.5rem;
  background: #fff;
  border: 1px solid #e5efe9;
  border-radius: 16px;
  box-shadow: 0 20px 44px rgba(15, 54, 36, 0.16);
}
.user-menu-head {
  display: flex;
  align-items: center;
  gap: 0.7rem;
  padding: 0.6rem 0.65rem 0.7rem;
}
.user-menu-avatar {
  width: 44px;
  height: 44px;
  flex-shrink: 0;
  border-radius: 50%;
  overflow: hidden;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-weight: 800;
  font-size: 1.05rem;
  color: #fff;
  background: linear-gradient(135deg, #16a34a, #12b981);
}
.user-menu-id {
  display: flex;
  flex-direction: column;
  min-width: 0;
  line-height: 1.3;
}
.user-menu-name {
  font-weight: 800;
  font-size: 0.95rem;
  color: #0f2a1e;
}
.user-menu-email {
  font-size: 0.78rem;
  color: #6b8578;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.user-menu-role {
  margin-top: 0.15rem;
  font-size: 0.64rem;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: #16a34a;
}
.user-menu-sep {
  height: 1px;
  margin: 0.35rem 0.4rem 0.4rem;
  background: #eef4f1;
}
.user-menu-item {
  width: 100%;
  display: flex;
  align-items: center;
  gap: 0.7rem;
  padding: 0.65rem 0.7rem;
  border: none;
  border-radius: 10px;
  background: transparent;
  color: #334155;
  font-family: inherit;
  font-size: 0.9rem;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.18s ease, color 0.18s ease;
}
.user-menu-item svg {
  width: 19px;
  height: 19px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.9;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.user-menu-item:hover {
  background: #eef8f2;
  color: #0f2a1e;
}
.user-menu-item.danger {
  color: #dc2626;
}
.user-menu-item.danger:hover {
  background: rgba(220, 38, 38, 0.1);
  color: #b91c1c;
}

/* Dropdown transition */
.menu-enter-active,
.menu-leave-active {
  transition: opacity 0.18s ease, transform 0.18s ease;
  transform-origin: top;
}
.menu-enter-from,
.menu-leave-to {
  opacity: 0;
  transform: translateY(-8px) scale(0.98);
}

.teacher-content {
  flex: 1;
  padding: 1.75rem 2rem 2.5rem;
}

/* Responsive: drop the greeting and collapse the chatbot to its icon on small screens */
@media (max-width: 720px) {
  .topbar-greeting {
    display: none;
  }
  .chatbot-text {
    display: none;
  }
  .chatbot-btn {
    padding-inline: 0.4rem;
  }
  .topbar {
    padding: 0.8rem 1rem;
  }
  .teacher-content {
    padding: 1.25rem 1rem 2rem;
  }
}
</style>
