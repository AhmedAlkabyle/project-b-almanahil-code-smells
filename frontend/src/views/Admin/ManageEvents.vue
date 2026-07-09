<script setup>
import { computed, onMounted, ref } from 'vue'
import { useLanguageStore } from '../../stores/language'
import AdminPageHeader from '../../components/admin/AdminPageHeader.vue'
import StatStrip from '../../components/admin/StatStrip.vue'
import { icons } from '../../components/admin/icons'
import EventsGrid from '../../components/admin/events/EventsGrid.vue'
import EventFormModal from '../../components/admin/events/EventFormModal.vue'
import ConfirmDialog from '../../components/common/ConfirmDialog.vue'
import { listEvents, removeEvent } from '../../api/events'
import { listClasses } from '../../api/classes'

const language = useLanguageStore()

// ---- Bilingual copy (per-component i18n pattern) ----
const content = {
  en: {
    searchPh: 'Search by title or description…',
    newEvent: 'New Event',
    allStatus: 'All',
    upcoming: 'Upcoming',
    past: 'Past',
    allTypes: 'All types',
    statTotal: 'Total',
    statUpcoming: 'Upcoming',
    statEvents: 'Events',
    statAnnouncements: 'Announcements',
    loading: 'Loading events…',
    loadError: 'We couldn’t load the events. Please try again.',
    retry: 'Retry',
    noMatch: 'No events match your filters.',
    confirmTitle: 'Delete event?',
    confirmMsg: 'This item will be permanently removed and will no longer be visible to users.',
    confirmYes: 'Delete',
    cancel: 'Cancel',
    types: { Announcement: 'Announcement', Event: 'Event' }
  },
  ar: {
    searchPh: 'ابحث بالعنوان أو الوصف…',
    newEvent: 'فعالية جديدة',
    allStatus: 'الكل',
    upcoming: 'القادمة',
    past: 'السابقة',
    allTypes: 'كل الأنواع',
    statTotal: 'الإجمالي',
    statUpcoming: 'القادمة',
    statEvents: 'الفعاليات',
    statAnnouncements: 'الإعلانات',
    loading: 'جارٍ تحميل الفعاليات…',
    loadError: 'تعذّر تحميل الفعاليات. يرجى المحاولة مرة أخرى.',
    retry: 'إعادة المحاولة',
    noMatch: 'لا توجد فعاليات مطابقة لبحثك.',
    confirmTitle: 'حذف الفعالية؟',
    confirmMsg: 'سيتم حذف هذا العنصر نهائياً ولن يظهر للمستخدمين بعد الآن.',
    confirmYes: 'حذف',
    cancel: 'إلغاء',
    types: { Announcement: 'إعلان', Event: 'فعالية' }
  }
}
const t = computed(() => content[language.lang])

const TYPES = ['Announcement', 'Event']

// ---- State ----
const events = ref([])
const classes = ref([]) // for the modal's SpecificClass picker
const loading = ref(true)
const loadError = ref('')
const search = ref('')
const statusFilter = ref('') // '' = all | 'upcoming' | 'past'
const typeFilter = ref('') // '' = all

// ---- Flash message (auto-dismisses) ----
const flashMsg = ref(null)
let flashTimer = null
function flash(type, text) {
  flashMsg.value = { type, text }
  clearTimeout(flashTimer)
  flashTimer = setTimeout(() => (flashMsg.value = null), 4500)
}

// ---- Load ----
async function loadEvents() {
  loading.value = true
  loadError.value = ''
  try {
    // Classes are needed by the modal's SpecificClass picker; fetch alongside events.
    const [eventsRes, classesRes] = await Promise.all([listEvents(), listClasses()])
    events.value = eventsRes.data ?? []
    classes.value = classesRes.data ?? []
  } catch (err) {
    loadError.value = err.message || t.value.loadError
  } finally {
    loading.value = false
  }
}
async function reload() {
  try {
    const { data } = await listEvents()
    events.value = data ?? []
  } catch (err) {
    flash('error', err.message)
  }
}
onMounted(loadEvents)

// Today's local calendar day (yyyy-mm-dd) — an event counts as "upcoming" if its date
// is today or later. Compared lexicographically against the event's date part.
const todayStr = computed(() => {
  const n = new Date()
  return `${n.getFullYear()}-${String(n.getMonth() + 1).padStart(2, '0')}-${String(n.getDate()).padStart(2, '0')}`
})
const isUpcoming = (e) => (e.date || '').slice(0, 10) >= todayStr.value

// ---- Status tab counts (whole dataset) ----
const counts = computed(() => {
  const list = events.value
  const upcoming = list.filter(isUpcoming).length
  return { total: list.length, upcoming, past: list.length - upcoming }
})

// ---- Filtering (status chip + type + search), newest first ----
const visibleEvents = computed(() => {
  const q = search.value.trim().toLowerCase()
  return events.value
    .filter((e) => {
      if (typeFilter.value && e.type !== typeFilter.value) return false
      if (statusFilter.value === 'upcoming' && !isUpcoming(e)) return false
      if (statusFilter.value === 'past' && isUpcoming(e)) return false
      if (q && !`${e.title} ${e.description}`.toLowerCase().includes(q)) return false
      return true
    })
    .slice()
    .sort((a, b) => new Date(b.date) - new Date(a.date))
})

// ---- Summary stats (whole dataset) ----
const statItems = computed(() => {
  const list = events.value
  return [
    { label: t.value.statTotal, value: list.length, variant: 'navy', icon: icons.events },
    { label: t.value.statUpcoming, value: counts.value.upcoming, variant: 'green', icon: icons.calendar },
    { label: t.value.statEvents, value: list.filter((e) => e.type === 'Event').length, variant: 'blue', icon: icons.calendar },
    { label: t.value.statAnnouncements, value: list.filter((e) => e.type === 'Announcement').length, variant: 'amber', icon: icons.announce }
  ]
})

// ---- Add / edit modal (performs the API call itself) ----
const modalOpen = ref(false)
const editingEvent = ref(null)

function openAdd() {
  editingEvent.value = null
  modalOpen.value = true
}
function openEdit(event) {
  editingEvent.value = event
  modalOpen.value = true
}
function closeModal() {
  modalOpen.value = false
  editingEvent.value = null
}
function onSaved({ message }) {
  closeModal()
  flash('success', message)
  reload()
}

// ---- Delete (with confirm) ----
const confirmOpen = ref(false)
const confirmTarget = ref(null)
const deleting = ref(false)

function askDelete(event) {
  confirmTarget.value = event
  confirmOpen.value = true
}
function closeConfirm() {
  if (deleting.value) return
  confirmOpen.value = false
  confirmTarget.value = null
}
async function confirmDelete() {
  const target = confirmTarget.value
  if (!target) return
  deleting.value = true
  try {
    const { message } = await removeEvent(target.id)
    deleting.value = false
    closeConfirm()
    flash('success', message)
    reload()
  } catch (err) {
    deleting.value = false
    closeConfirm()
    flash('error', err.message)
  }
}
</script>

<template>
  <div class="manage-events" :dir="language.dir">
    <AdminPageHeader />

    <!-- Summary stats -->
    <StatStrip :items="statItems" />

    <!-- Flash message -->
    <Transition name="flash">
      <div v-if="flashMsg" class="flash" :class="flashMsg.type">
        <svg v-if="flashMsg.type === 'success'" viewBox="0 0 24 24"><path d="M20 6 9 17l-5-5" /></svg>
        <svg v-else viewBox="0 0 24 24"><circle cx="12" cy="12" r="9" /><path d="M12 8v5M12 16h.01" /></svg>
        <span>{{ flashMsg.text }}</span>
        <button type="button" class="flash-x" @click="flashMsg = null" aria-label="Dismiss">
          <svg viewBox="0 0 24 24"><path d="M18 6 6 18M6 6l12 12" /></svg>
        </button>
      </div>
    </Transition>

    <!-- Events panel: search, status chips, type filter, and the cards unified in one card -->
    <div class="panel">
      <!-- Head: search + New Event -->
      <div class="panel-head">
        <label class="search">
          <svg viewBox="0 0 24 24"><circle cx="11" cy="11" r="7" /><path d="m21 21-4.3-4.3" /></svg>
          <input v-model="search" type="text" :placeholder="t.searchPh" />
        </label>

        <button type="button" class="add-btn" @click="openAdd">
          <svg viewBox="0 0 24 24"><path d="M12 5v14M5 12h14" /></svg>
          {{ t.newEvent }}
        </button>
      </div>

      <!-- Filter bar: status chips (start) + type dropdown (end) -->
      <div class="filter-bar">
        <div class="status-tabs" role="tablist">
          <button
            type="button"
            class="status-tab tab-all"
            :class="{ active: statusFilter === '' }"
            role="tab"
            :aria-selected="statusFilter === ''"
            @click="statusFilter = ''"
          >
            <span class="tab-label">{{ t.allStatus }}</span>
            <span class="tab-count">{{ counts.total }}</span>
          </button>
          <button
            type="button"
            class="status-tab tab-upcoming"
            :class="{ active: statusFilter === 'upcoming' }"
            role="tab"
            :aria-selected="statusFilter === 'upcoming'"
            @click="statusFilter = 'upcoming'"
          >
            <span class="tab-dot"></span>
            <span class="tab-label">{{ t.upcoming }}</span>
            <span class="tab-count">{{ counts.upcoming }}</span>
          </button>
          <button
            type="button"
            class="status-tab tab-past"
            :class="{ active: statusFilter === 'past' }"
            role="tab"
            :aria-selected="statusFilter === 'past'"
            @click="statusFilter = 'past'"
          >
            <span class="tab-dot"></span>
            <span class="tab-label">{{ t.past }}</span>
            <span class="tab-count">{{ counts.past }}</span>
          </button>
        </div>

        <div class="sub-filters">
          <select v-model="typeFilter" class="mini-select" :aria-label="t.allTypes">
            <option value="">{{ t.allTypes }}</option>
            <option v-for="ty in TYPES" :key="ty" :value="ty">{{ t.types[ty] }}</option>
          </select>
        </div>
      </div>

      <!-- Body: loading / error / no-match / events grid -->
      <div class="panel-body">
        <div v-if="loading" class="state-block">
          <span class="spinner"></span>
          <p>{{ t.loading }}</p>
        </div>

        <div v-else-if="loadError" class="state-block error-state">
          <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="9" /><path d="M12 8v5M12 16h.01" /></svg>
          <p>{{ loadError }}</p>
          <button type="button" class="retry-btn" @click="loadEvents">{{ t.retry }}</button>
        </div>

        <div v-else-if="events.length && !visibleEvents.length" class="state-block">
          <svg class="state-ic" viewBox="0 0 24 24"><circle cx="11" cy="11" r="7" /><path d="m21 21-4.3-4.3" /></svg>
          <p>{{ t.noMatch }}</p>
        </div>

        <EventsGrid v-else :events="visibleEvents" @edit="openEdit" @delete="askDelete" />
      </div>
    </div>

    <!-- Add / edit modal -->
    <EventFormModal
      :open="modalOpen"
      :event="editingEvent"
      :classes="classes"
      @close="closeModal"
      @saved="onSaved"
    />

    <!-- Confirm delete -->
    <ConfirmDialog
      :open="confirmOpen"
      :title="t.confirmTitle"
      :message="t.confirmMsg"
      :confirm-label="t.confirmYes"
      :cancel-label="t.cancel"
      variant="danger"
      :busy="deleting"
      @confirm="confirmDelete"
      @cancel="closeConfirm"
    />
  </div>
</template>

<style scoped>
.manage-events {
  --navy: var(--ds-navy);
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

/* Flash message */
.flash {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  padding: 0.85rem 1.1rem;
  border-radius: 12px;
  font-size: 0.9rem;
  font-weight: 600;
  line-height: 1.4;
}
.flash svg {
  width: 20px;
  height: 20px;
  flex-shrink: 0;
  fill: none;
  stroke: currentColor;
  stroke-width: 2;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.flash.success {
  color: #15803d;
  background: #f0fdf4;
  border: 1px solid #bbf7d0;
}
.flash.error {
  color: #b91c1c;
  background: #fef2f2;
  border: 1px solid #fecaca;
}
.flash span {
  flex: 1;
}
.flash-x {
  border: none;
  background: transparent;
  color: inherit;
  cursor: pointer;
  padding: 0.15rem;
  opacity: 0.6;
  transition: opacity 0.15s ease;
}
.flash-x:hover {
  opacity: 1;
}
.flash-x svg {
  width: 15px;
  height: 15px;
}
.flash-enter-active,
.flash-leave-active {
  transition: opacity 0.25s ease, transform 0.25s ease;
}
.flash-enter-from,
.flash-leave-to {
  opacity: 0;
  transform: translateY(-6px);
}

/* Events panel — search + filters + cards unified in one elevated card */
.panel {
  position: relative;
  background: #fff;
  border: 1px solid #eaeef6;
  border-radius: 18px;
  box-shadow: 0 8px 22px rgba(30, 41, 59, 0.05);
  overflow: hidden;
  animation: panel-rise 0.5s ease both;
}
/* Signature navy→orange accent line across the top of the list card
   (the same gradient as the welcome page's heading underline). */
.panel::before {
  content: '';
  position: absolute;
  top: 0;
  inset-inline: 0;
  height: 3px;
  z-index: 2;
  background: var(--ds-accent-bar, linear-gradient(90deg, #1e4c9a, #f2a03d));
}
@keyframes panel-rise {
  from {
    opacity: 0;
    transform: translateY(14px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}
@media (prefers-reduced-motion: reduce) {
  .panel {
    animation: none;
  }
}
.panel-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  flex-wrap: wrap;
  padding: 1.1rem 1.25rem;
  border-bottom: 1px solid #eef1f7;
}
.search {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.6rem 1rem;
  background: #fff;
  border: 1px solid #e6e9f2;
  border-radius: 999px;
  min-width: 280px;
  transition: border-color 0.2s ease, box-shadow 0.2s ease;
}
.search:focus-within {
  border-color: var(--navy);
  box-shadow: 0 0 0 3px rgba(30, 76, 154, 0.12);
}
.search svg {
  width: 17px;
  height: 17px;
  fill: none;
  stroke: #9ca3af;
  stroke-width: 1.9;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.search input {
  border: none;
  outline: none;
  background: transparent;
  font-size: 0.9rem;
  font-family: inherit;
  color: #1f2937;
  width: 100%;
}

/* New Event button */
.add-btn {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.7rem 1.3rem;
  border: none;
  border-radius: 12px;
  font-size: 0.9rem;
  font-weight: 700;
  font-family: inherit;
  color: #fff;
  background: linear-gradient(135deg, var(--navy), #2f63ba);
  box-shadow: 0 8px 18px rgba(30, 76, 154, 0.3);
  cursor: pointer;
  transition: transform 0.15s ease, box-shadow 0.2s ease;
}
.add-btn:hover {
  transform: translateY(-1px);
  box-shadow: 0 12px 24px rgba(30, 76, 154, 0.4);
}
.add-btn svg {
  width: 17px;
  height: 17px;
  fill: none;
  stroke: currentColor;
  stroke-width: 2.2;
  stroke-linecap: round;
  stroke-linejoin: round;
}

/* Filter bar: status chips on the start, type dropdown on the end */
.filter-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  flex-wrap: wrap;
  padding: 0.7rem 1rem;
  background: #fbfcfe;
  border-bottom: 1px solid #eef1f7;
}

/* Status filter — segmented chips (same look as the Classes level tabs) */
.status-tabs {
  display: flex;
  flex-wrap: wrap;
  gap: 0.45rem;
}
.status-tab {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 0.9rem;
  border: 1px solid transparent;
  border-radius: 10px;
  background: transparent;
  font-family: inherit;
  font-size: 0.88rem;
  font-weight: 700;
  color: #64748b;
  cursor: pointer;
  transition: background 0.15s ease, color 0.15s ease, box-shadow 0.2s ease;
}
.status-tab:hover {
  background: #f4f7fc;
  color: #334155;
}
.tab-dot {
  width: 9px;
  height: 9px;
  border-radius: 50%;
  flex-shrink: 0;
}
.tab-upcoming .tab-dot {
  background: #059669;
}
.tab-past .tab-dot {
  background: #94a3b8;
}
.tab-count {
  min-width: 22px;
  height: 20px;
  padding: 0 0.4rem;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border-radius: 999px;
  background: #eef1f7;
  color: #64748b;
  font-size: 0.72rem;
  font-weight: 800;
}
.status-tab.active {
  color: #fff;
}
.status-tab.active .tab-dot {
  background: rgba(255, 255, 255, 0.9);
}
.status-tab.active .tab-count {
  background: rgba(255, 255, 255, 0.22);
  color: #fff;
}
.tab-all.active {
  background: var(--navy);
  box-shadow: 0 6px 14px rgba(30, 76, 154, 0.28);
}
.tab-upcoming.active {
  background: #059669;
  box-shadow: 0 6px 14px rgba(5, 150, 105, 0.28);
}
.tab-past.active {
  background: #64748b;
  box-shadow: 0 6px 14px rgba(100, 116, 139, 0.28);
}

/* Type dropdown (compact admin filter style) */
.sub-filters {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-wrap: wrap;
}
.mini-select {
  padding: 0.5rem 0.9rem;
  font-size: 0.86rem;
  font-family: inherit;
  font-weight: 600;
  color: #334155;
  background: #fff;
  border: 1px solid #e6e9f2;
  border-radius: 10px;
  cursor: pointer;
  outline: none;
  transition: border-color 0.2s ease, box-shadow 0.2s ease;
}
.mini-select:focus {
  border-color: var(--navy);
  box-shadow: 0 0 0 3px rgba(30, 76, 154, 0.12);
}

/* Body holds the cards grid + the loading / error / no-match states. A whisper-light
   tint lets the white event cards read as elevated tiles rather than card-on-card. */
.panel-body {
  min-height: 120px;
  padding: 1.25rem;
  background: #fbfcfe;
}
.state-block {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 0.7rem;
  padding: 2.5rem 1.5rem;
  color: #6b7280;
  font-size: 0.92rem;
}
.state-block p {
  margin: 0;
}
.state-block .state-ic {
  width: 32px;
  height: 32px;
  fill: none;
  stroke: #b6bfd0;
  stroke-width: 1.7;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.state-block.error-state {
  color: #b91c1c;
}
.state-block.error-state svg {
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
  border: 3px solid #e2e8f0;
  border-top-color: var(--navy);
  animation: spin 0.8s linear infinite;
}
@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}
.retry-btn {
  margin-top: 0.3rem;
  padding: 0.55rem 1.3rem;
  border: 1px solid #e2e8f2;
  border-radius: 10px;
  background: #fff;
  font-family: inherit;
  font-size: 0.85rem;
  font-weight: 700;
  color: var(--navy);
  cursor: pointer;
  transition: background 0.15s ease;
}
.retry-btn:hover {
  background: #eef4ff;
}
@media (prefers-reduced-motion: reduce) {
  .spinner {
    animation-duration: 1.6s;
  }
}

@media (max-width: 560px) {
  .search {
    min-width: 0;
    flex: 1;
  }
  .panel-head {
    flex-direction: column;
    align-items: stretch;
  }
  .add-btn {
    justify-content: center;
  }
  .filter-bar {
    flex-direction: column;
    align-items: stretch;
  }
  .sub-filters {
    justify-content: stretch;
  }
  .mini-select {
    flex: 1;
  }
}
</style>
