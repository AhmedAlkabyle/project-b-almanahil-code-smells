<script setup>
// Shared, read-only events/announcements feed for the Teacher / Student / Parent portals.
// Calls GET /api/events/my (the backend filters by the caller's role/level/class via JWT)
// and renders the events as cards. Themed per role via the `theme` prop (green/orange/slate)
// — the card body reuses the admin events-card design. Bilingual (EN+AR) + RTL.
import { computed, onMounted, ref } from 'vue'
import { useLanguageStore } from '../../stores/language'
import { audienceBadge, audienceTone } from '../../utils/eventAudience'
import { getMyEvents } from '../../api/events'

const props = defineProps({
  // Role theme: 'green' (teacher) | 'orange' (student) | 'slate' (parent).
  theme: { type: String, default: 'green' }
})

const language = useLanguageStore()

// ---- Per-role theme → CSS custom properties (only the accents change) ----
const THEMES = {
  green: {
    '--ef-primary': '#16a34a', '--ef-accent': '#f2a03d', '--ef-heading': '#0f2a1e', '--ef-muted': '#6b8578',
    '--ef-border': '#e6f0eb', '--ef-hover-border': '#cfe7db', '--ef-shadow': 'rgba(15, 54, 36, 0.06)',
    '--ef-soft-a': '#e9f9f0', '--ef-soft-b': '#dff3e8', '--ef-track': '#e2ece7', '--ef-hover-bg': '#eefaf3'
  },
  orange: {
    '--ef-primary': '#ef8a29', '--ef-accent': '#f6b93b', '--ef-heading': '#3a2410', '--ef-muted': '#8a6a4d',
    '--ef-border': '#f2e4d3', '--ef-hover-border': '#f0d6b6', '--ef-shadow': 'rgba(156, 80, 10, 0.06)',
    '--ef-soft-a': '#fff2df', '--ef-soft-b': '#ffe7c6', '--ef-track': '#f0e0cd', '--ef-hover-bg': '#fff3e6'
  },
  slate: {
    '--ef-primary': '#64748b', '--ef-accent': '#f2a03d', '--ef-heading': '#1e293b', '--ef-muted': '#64748b',
    '--ef-border': '#e2e8f0', '--ef-hover-border': '#cbd5e1', '--ef-shadow': 'rgba(30, 41, 59, 0.06)',
    '--ef-soft-a': '#eef2f7', '--ef-soft-b': '#e2e8f0', '--ef-track': '#e2e8f0', '--ef-hover-bg': '#f1f5f9'
  }
}
const themeVars = computed(() => THEMES[props.theme] || THEMES.green)

// ---- Bilingual copy (per-component i18n pattern) ----
const content = {
  en: {
    loading: 'Loading announcements…',
    loadError: 'We couldn’t load your announcements. Please try again.',
    retry: 'Retry',
    emptyTitle: 'No announcements for you right now',
    emptySub: 'New announcements and events meant for you will appear here.',
    types: { Announcement: 'Announcement', Event: 'Event' }
  },
  ar: {
    loading: 'جارٍ تحميل الإعلانات…',
    loadError: 'تعذّر تحميل إعلاناتك. يرجى المحاولة مرة أخرى.',
    retry: 'إعادة المحاولة',
    emptyTitle: 'لا توجد إعلانات لك حالياً',
    emptySub: 'ستظهر هنا الإعلانات والفعاليات الجديدة الخاصة بك.',
    types: { Announcement: 'إعلان', Event: 'فعالية' }
  }
}
const t = computed(() => content[language.lang])

// ---- State ----
const events = ref([])
const loading = ref(true)
const loadError = ref('')

async function loadEvents() {
  loading.value = true
  loadError.value = ''
  try {
    const { data } = await getMyEvents()
    events.value = data ?? []
  } catch (err) {
    loadError.value = err.message || t.value.loadError
    events.value = []
  } finally {
    loading.value = false
  }
}
onMounted(loadEvents)

// Bilingual date + time (e.g. "5 Jul 2026, 14:30"); 24-hour, wall-clock (no TZ shift).
function formatDateTime(iso) {
  if (!iso) return ''
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return iso
  return d.toLocaleString(language.isArabic ? 'ar' : 'en-US', {
    year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit', hour12: false
  })
}
</script>

<template>
  <div class="events-feed" :dir="language.dir" :style="themeVars">
    <!-- Loading -->
    <div v-if="loading" class="state-card">
      <span class="spinner"></span>
      <p>{{ t.loading }}</p>
    </div>

    <!-- Error -->
    <div v-else-if="loadError" class="state-card error-state">
      <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="9" /><path d="M12 8v5M12 16h.01" /></svg>
      <p>{{ loadError }}</p>
      <button type="button" class="retry-btn" @click="loadEvents">{{ t.retry }}</button>
    </div>

    <!-- Empty -->
    <div v-else-if="!events.length" class="state-card">
      <span class="empty-badge">
        <svg viewBox="0 0 24 24"><path d="M18 8a6 6 0 1 0-12 0c0 7-3 9-3 9h18s-3-2-3-9" /><path d="M13.7 21a2 2 0 0 1-3.4 0" /></svg>
      </span>
      <h3>{{ t.emptyTitle }}</h3>
      <p>{{ t.emptySub }}</p>
    </div>

    <!-- Cards -->
    <div v-else class="grid">
      <article
        v-for="(ev, i) in events"
        :key="ev.id"
        :style="{ '--card-i': i }"
        class="card"
        :class="ev.type === 'Event' ? 'type-event' : 'type-announce'"
      >
        <div class="card-top">
          <span class="badge" :class="ev.type === 'Event' ? 'is-event' : 'is-announcement'">
            {{ t.types[ev.type] || ev.type }}
          </span>
          <span class="aud" :class="audienceTone(ev.audienceType)">
            <svg v-if="ev.audienceType === 'SpecificClass'" viewBox="0 0 24 24"><path d="M3 21h18" /><path d="M5 21V8l7-4 7 4v13" /><path d="M9 21v-5h6v5" /></svg>
            <svg v-else viewBox="0 0 24 24"><circle cx="9" cy="8" r="3.2" /><path d="M3.5 20a5.5 5.5 0 0 1 11 0" /><path d="M16 8.6a3 3 0 0 1 0 5.8M18.5 20a5 5 0 0 0-3-4.6" /></svg>
            {{ audienceBadge(ev, language.lang) }}
          </span>
        </div>

        <h3 class="card-title">{{ ev.title }}</h3>

        <span class="card-date">
          <svg viewBox="0 0 24 24"><rect x="3" y="4" width="18" height="17" rx="3" /><path d="M8 2v4M16 2v4M3 10h18" /></svg>
          {{ formatDateTime(ev.date) }}
        </span>

        <p class="card-desc">{{ ev.description }}</p>
      </article>
    </div>
  </div>
</template>

<style scoped>
.grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 1.25rem;
}

/* ---- Card ---- */
.card {
  position: relative;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  background: #fff;
  border: 1px solid var(--ef-border);
  border-radius: 18px;
  padding: 1.35rem 1.4rem;
  box-shadow: 0 8px 22px var(--ef-shadow);
  transition: transform 0.25s ease, box-shadow 0.25s ease, border-color 0.25s ease;
  /* Gentle staggered entrance. `backwards` fill so the hover-lift isn't overridden. */
  animation: evt-card-in 0.4s ease backwards;
  animation-delay: calc(min(var(--card-i, 0), 10) * 45ms);
}
@keyframes evt-card-in {
  from { opacity: 0; transform: translateY(8px); }
  to { opacity: 1; transform: translateY(0); }
}
@media (prefers-reduced-motion: reduce) {
  .card {
    animation: none;
  }
}
/* Role-accent line that grows across the top on hover */
.card::before {
  content: '';
  position: absolute;
  top: 0;
  inset-inline-start: 0;
  width: 100%;
  height: 3px;
  background: linear-gradient(90deg, var(--ef-primary), var(--ef-accent));
  transform: scaleX(0);
  transform-origin: left;
  transition: transform 0.35s ease;
}
.card:hover::before {
  transform: scaleX(1);
}
.card:hover {
  transform: translateY(-3px);
  border-color: var(--ef-hover-border);
  box-shadow: 0 18px 38px var(--ef-shadow);
}
/* Type-coloured accent down the leading edge (primary = Event, orange = Announcement) */
.card::after {
  content: '';
  position: absolute;
  top: 1.35rem;
  bottom: 1.35rem;
  inset-inline-start: 0;
  width: 3px;
  border-radius: 0 3px 3px 0;
  background: #cbd5e1;
}
.card.type-event::after {
  background: var(--ef-primary);
}
.card.type-announce::after {
  background: var(--ef-accent);
}

.card-top {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 0.45rem;
  margin-bottom: 0.85rem;
}
.badge {
  display: inline-flex;
  align-items: center;
  padding: 0.3rem 0.7rem;
  border-radius: 999px;
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.02em;
}
.badge.is-event {
  color: #1e4c9a;
  background: #e0ecff;
}
.badge.is-announcement {
  color: #b45309;
  background: #fef3c7;
}

/* Audience pill — colour-toned by who it targets (see audienceTone) */
.aud {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  padding: 0.28rem 0.62rem 0.28rem 0.5rem;
  border-radius: 999px;
  font-size: 0.7rem;
  font-weight: 800;
  letter-spacing: 0.01em;
  white-space: nowrap;
}
.aud svg {
  width: 13px;
  height: 13px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.9;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.aud.blue { color: #1e4c9a; background: #e0ecff; }
.aud.green { color: #047857; background: #d1fae5; }
.aud.slate { color: #475569; background: #e8edf5; }
.aud.orange { color: #b45309; background: #fef3c7; }
.aud.emerald { color: #0f766e; background: #ccfbf1; }
.aud.indigo { color: #4338ca; background: #e0e7ff; }
.aud.navy { color: #1e3a8a; background: #dbe6ff; }

.card-title {
  margin: 0 0 0.6rem;
  font-size: 1.1rem;
  font-weight: 800;
  color: var(--ef-heading);
  line-height: 1.35;
}
.card-date {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  font-size: 0.82rem;
  font-weight: 600;
  color: var(--ef-muted);
  margin-bottom: 0.75rem;
}
.card-date svg {
  width: 15px;
  height: 15px;
  fill: none;
  stroke: var(--ef-primary);
  stroke-width: 1.8;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.card-desc {
  margin: 0;
  font-size: 0.88rem;
  color: var(--ef-muted);
  line-height: 1.55;
  white-space: pre-line;
  /* Clamp to ~4 lines */
  display: -webkit-box;
  -webkit-line-clamp: 4;
  line-clamp: 4;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

/* ---- State cards (loading / error / empty) ---- */
.state-card {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  gap: 0.55rem;
  padding: 3.25rem 1.5rem;
  background: #fff;
  border: 1px solid var(--ef-border);
  border-radius: 18px;
  box-shadow: 0 8px 22px var(--ef-shadow);
  color: var(--ef-muted);
}
.state-card p {
  margin: 0;
  font-size: 0.92rem;
}
.state-card h3 {
  margin: 0;
  font-size: 1.2rem;
  font-weight: 800;
  color: var(--ef-heading);
}
.empty-badge {
  width: 66px;
  height: 66px;
  border-radius: 50%;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 0.3rem;
  color: var(--ef-primary);
  background: radial-gradient(circle, var(--ef-soft-a), var(--ef-soft-b));
}
.empty-badge svg {
  width: 30px;
  height: 30px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.6;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.state-card.error-state {
  color: #b91c1c;
}
.state-card.error-state svg {
  width: 34px;
  height: 34px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.7;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.spinner {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  border: 3px solid var(--ef-track);
  border-top-color: var(--ef-primary);
  animation: spin 0.8s linear infinite;
}
@keyframes spin {
  to { transform: rotate(360deg); }
}
.retry-btn {
  margin-top: 0.3rem;
  padding: 0.55rem 1.3rem;
  border: 1px solid var(--ef-hover-border);
  border-radius: 10px;
  background: #fff;
  font-family: inherit;
  font-size: 0.85rem;
  font-weight: 700;
  color: var(--ef-primary);
  cursor: pointer;
  transition: background 0.15s ease;
}
.retry-btn:hover {
  background: var(--ef-hover-bg);
}
@media (prefers-reduced-motion: reduce) {
  .spinner {
    animation-duration: 1.6s;
  }
}
</style>
