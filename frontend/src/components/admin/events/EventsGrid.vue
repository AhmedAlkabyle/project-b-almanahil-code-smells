<script setup>
// Responsive grid of announcement/event cards. Presentational only —
// all mutations are delegated back up via `edit` / `delete` events.
import { computed } from 'vue'
import { useLanguageStore } from '../../../stores/language'
import { audienceBadge, audienceTone } from '../../../utils/eventAudience'

const props = defineProps({
  events: { type: Array, default: () => [] }
})
const emit = defineEmits(['edit', 'delete'])

const language = useLanguageStore()

// ---- Bilingual copy (per-component i18n pattern) ----
const content = {
  en: {
    edit: 'Edit',
    delete: 'Delete',
    emptyTitle: 'No events yet',
    emptySub: 'Publish your first announcement or event to get started.',
    types: { Announcement: 'Announcement', Event: 'Event' }
  },
  ar: {
    edit: 'تعديل',
    delete: 'حذف',
    emptyTitle: 'لا توجد فعاليات بعد',
    emptySub: 'انشر أول إعلان أو فعالية للبدء.',
    types: { Announcement: 'إعلان', Event: 'فعالية' }
  }
}
const t = computed(() => content[language.lang])

// Format an ISO datetime bilingually via the language store, showing date AND time
// (e.g. "5 Jul 2026, 14:30"). 24-hour clock; wall-clock (no timezone conversion).
function formatDateTime(iso) {
  if (!iso) return ''
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return iso
  return d.toLocaleString(language.isArabic ? 'ar' : 'en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    hour12: false
  })
}
</script>

<template>
  <div class="events-grid" :dir="language.dir">
    <!-- Empty state -->
    <div v-if="!events.length" class="empty">
      <span class="empty-badge">
        <svg viewBox="0 0 24 24"><path d="M3 5h18M3 12h18M3 19h18" opacity="0" /><rect x="3" y="4" width="18" height="17" rx="3" /><path d="M8 2v4M16 2v4M3 10h18" /></svg>
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
          <!-- Audience badge (target of this item) -->
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

        <div class="card-foot">
          <button type="button" class="act edit" :title="t.edit" :aria-label="t.edit" @click="emit('edit', ev)">
            <svg viewBox="0 0 24 24"><path d="M12 20h9" /><path d="M16.5 3.5a2.12 2.12 0 0 1 3 3L7 19l-4 1 1-4Z" /></svg>
          </button>
          <button type="button" class="act delete" :title="t.delete" :aria-label="t.delete" @click="emit('delete', ev)">
            <svg viewBox="0 0 24 24"><path d="M3 6h18" /><path d="M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2" /><path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" /></svg>
          </button>
        </div>
      </article>
    </div>
  </div>
</template>

<style scoped>
.events-grid {
  --navy: var(--ds-navy);
  --orange: var(--ds-orange);
}

.grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 1.25rem;
}

/* ---- Card (matches dashboard panel look) ---- */
.card {
  position: relative;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  background: #fff;
  border: 1px solid #eaeef6;
  border-radius: 18px;
  padding: 1.35rem 1.4rem;
  box-shadow: 0 8px 22px rgba(30, 41, 59, 0.05);
  transition: transform 0.25s ease, box-shadow 0.25s ease, border-color 0.25s ease;
  /* Gentle staggered entrance. `backwards` fill applies the start state only during the
     delay — so once it finishes, the hover-lift transform isn't overridden. */
  animation: evt-card-in 0.4s ease backwards;
  animation-delay: calc(min(var(--card-i, 0), 10) * 45ms);
}
@keyframes evt-card-in {
  from {
    opacity: 0;
    transform: translateY(8px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}
@media (prefers-reduced-motion: reduce) {
  .card {
    animation: none;
  }
}
/* Navy→orange accent line that grows across the top on hover */
.card::before {
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
.card:hover::before {
  transform: scaleX(1);
}
.card:hover {
  transform: translateY(-3px);
  border-color: #dbe3f2;
  box-shadow: 0 18px 38px rgba(30, 41, 59, 0.1);
}
/* Type-coloured accent down the leading edge (blue = Event, amber = Announcement) */
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
  background: var(--navy);
}
.card.type-announce::after {
  background: var(--orange);
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
.aud.blue {
  color: #1e4c9a;
  background: #e0ecff;
}
.aud.green {
  color: #047857;
  background: #d1fae5;
}
.aud.slate {
  color: #475569;
  background: #e8edf5;
}
.aud.orange {
  color: #b45309;
  background: #fef3c7;
}
.aud.emerald {
  color: #0f766e;
  background: #ccfbf1;
}
.aud.indigo {
  color: #4338ca;
  background: #e0e7ff;
}
.aud.navy {
  color: #1e3a8a;
  background: #dbe6ff;
}

.card-title {
  margin: 0 0 0.6rem;
  font-size: 1.1rem;
  font-weight: 800;
  color: #0f2444;
  line-height: 1.35;
}

.card-date {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  font-size: 0.82rem;
  font-weight: 600;
  color: #6b7280;
  margin-bottom: 0.75rem;
}
.card-date svg {
  width: 15px;
  height: 15px;
  fill: none;
  stroke: var(--navy);
  stroke-width: 1.8;
  stroke-linecap: round;
  stroke-linejoin: round;
}

.card-desc {
  margin: 0 0 1.2rem;
  font-size: 0.88rem;
  color: #64748b;
  line-height: 1.55;
  /* Clamp to ~3 lines */
  display: -webkit-box;
  -webkit-line-clamp: 3;
  line-clamp: 3;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.card-foot {
  display: flex;
  justify-content: flex-end;
  gap: 0.4rem;
  margin-top: auto;
  padding-top: 0.25rem;
}
/* Compact, square icon buttons (labels as tooltips) — identical to the Classes/Subjects tables */
.act {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 34px;
  height: 34px;
  padding: 0;
  border: 1px solid #e6ebf4;
  border-radius: 10px;
  background: #fff;
  color: #64748b;
  cursor: pointer;
  transition: background 0.15s ease, color 0.15s ease, border-color 0.15s ease, transform 0.15s ease;
}
.act:hover {
  transform: translateY(-1px);
}
.act svg {
  width: 16px;
  height: 16px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.9;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.act.edit:hover {
  color: var(--navy);
  border-color: #b9ccec;
  background: #eef4ff;
}
.act.delete:hover {
  color: #dc2626;
  border-color: #f6c9c9;
  background: #fef2f2;
}

/* ---- Empty state (flat — it lives inside the ManageEvents .panel body) ---- */
.empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 0.6rem;
  padding: 2.25rem 1.5rem;
  text-align: center;
}
.empty-badge {
  width: 66px;
  height: 66px;
  border-radius: 50%;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  /* Warm, friendly badge (welcome-page orange) — same as the other admin lists. */
  background: radial-gradient(circle, #fff4e6, #ffe7c9);
  color: #eb9a34;
  box-shadow: 0 8px 18px rgba(242, 160, 61, 0.18);
  margin-bottom: 0.4rem;
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
.empty h3 {
  margin: 0;
  font-size: 1.15rem;
  font-weight: 800;
  color: #0f2444;
}
.empty p {
  margin: 0;
  font-size: 0.9rem;
  color: #6b7280;
}
</style>
