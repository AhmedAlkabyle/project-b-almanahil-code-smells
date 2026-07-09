<script setup>
// Parent view of their CHILD'S learning materials (read-only). The selected child comes
// from the shared parent store; the data is fetched from GET /api/materials/child/{childId},
// whose RBAC allows only a parent linked to that student (via ParentStudents) — so a parent
// can only ever see their own child's materials. Shows the same PDFs + YouTube videos the
// child sees, grouped by subject. Slate/gray theme, bilingual (EN+AR) + RTL. Mirrors
// StudentMaterials' display and the ParentChildGrades child-selection pattern.
import { computed, ref, watch } from 'vue'
import { useLanguageStore } from '../../stores/language'
import { useParentStore } from '../../stores/parent'
import ParentPageHeader from '../../components/parent/ParentPageHeader.vue'
import ParentChildBar from '../../components/parent/ParentChildBar.vue'
import { getChildMaterials } from '../../api/materials'
import { youtubeId, youtubeThumb, youtubeWatchUrl } from '../../utils/youtube'

const language = useLanguageStore()
const parent = useParentStore()

// ---- Bilingual copy (per-component i18n pattern) ----
const content = {
  en: {
    youtube: 'YouTube',
    pdf: 'PDF',
    open: 'Open video',
    openPdf: 'Open PDF',
    loading: 'Loading materials…',
    loadError: 'We couldn’t load these materials. Please try again.',
    retry: 'Retry',
    empty: 'No learning materials for your child yet',
    emptySub: 'When teachers add videos and resources for your child’s class, they’ll appear here.'
  },
  ar: {
    youtube: 'يوتيوب',
    pdf: 'PDF',
    open: 'فتح الفيديو',
    openPdf: 'فتح الملف',
    loading: 'جارٍ تحميل المواد…',
    loadError: 'تعذّر تحميل هذه المواد. يرجى المحاولة مرة أخرى.',
    retry: 'إعادة المحاولة',
    empty: 'لا توجد مواد تعليمية لابنك بعد',
    emptySub: 'عندما يضيف المعلمون مقاطع فيديو وموارد لصف ابنك، ستظهر هنا.'
  }
}
const t = computed(() => content[language.lang])

// ---- State ----
const materials = ref([])
const loading = ref(false)
const loadError = ref('')

async function loadForChild(childId) {
  if (!childId) {
    materials.value = []
    loadError.value = ''
    return
  }
  loading.value = true
  loadError.value = ''
  try {
    const { data } = await getChildMaterials(childId)
    materials.value = data ?? []
  } catch (err) {
    loadError.value = err.message || t.value.loadError // surfaces the backend's 403 message too
    materials.value = []
  } finally {
    loading.value = false
  }
}
// Reload whenever the selected child changes (immediate: fires once children resolve).
watch(() => parent.selectedChildId, loadForChild, { immediate: true })

// Group the flat list by subject (the API already orders by subject then newest-first).
const groups = computed(() => {
  const map = new Map()
  for (const m of materials.value) {
    if (!map.has(m.subjectId)) {
      map.set(m.subjectId, { subjectId: m.subjectId, subjectName: m.subjectName, items: [] })
    }
    map.get(m.subjectId).items.push(m)
  }
  return Array.from(map.values())
})

// ---- Helpers ----
const isPdf = (m) => m.materialType === 'Pdf'
const vidId = (m) => youtubeId(m.url)
const thumb = (m) => youtubeThumb(vidId(m))
const openUrl = (m) => (isPdf(m) ? m.url : youtubeWatchUrl(vidId(m), m.url))
</script>

<template>
  <div class="parent-materials" :dir="language.dir">
    <ParentPageHeader />
    <ParentChildBar />

    <!-- Only show the data section once a child is selected -->
    <template v-if="parent.selectedChildId">
      <!-- Loading -->
      <div v-if="loading" class="state-card">
        <span class="spinner"></span>
        <p>{{ t.loading }}</p>
      </div>

      <!-- Error -->
      <div v-else-if="loadError" class="state-card error-state">
        <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="9" /><path d="M12 8v5M12 16h.01" /></svg>
        <p>{{ loadError }}</p>
        <button type="button" class="retry-btn" @click="loadForChild(parent.selectedChildId)">{{ t.retry }}</button>
      </div>

      <!-- Empty -->
      <div v-else-if="!materials.length" class="state-card">
        <span class="state-badge"><svg viewBox="0 0 24 24"><path d="m10 9 5 3-5 3z" /><rect x="3" y="4" width="18" height="16" rx="3" /></svg></span>
        <h3>{{ t.empty }}</h3>
        <p>{{ t.emptySub }}</p>
      </div>

      <!-- Grouped by subject -->
      <template v-else>
        <section v-for="g in groups" :key="g.subjectId" class="group">
          <div class="group-head">
            <h3>{{ g.subjectName }}</h3>
            <span class="count">{{ g.items.length }}</span>
          </div>

          <div class="grid">
            <article v-for="(m, i) in g.items" :key="m.id" class="card" :style="{ '--card-i': i }">
              <a class="thumb" :class="{ pdf: isPdf(m) }" :href="openUrl(m)" target="_blank" rel="noopener noreferrer">
                <template v-if="isPdf(m)">
                  <span class="pdf-ic"><svg viewBox="0 0 24 24"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" /><path d="M14 2v6h6" /></svg></span>
                </template>
                <template v-else>
                  <img v-if="thumb(m)" :src="thumb(m)" alt="" loading="lazy" />
                  <span class="play"><svg viewBox="0 0 24 24"><path d="M8 5v14l11-7z" /></svg></span>
                </template>
              </a>
              <div class="card-body">
                <span class="mat-badge" :class="isPdf(m) ? 'is-pdf' : 'is-yt'">
                  <svg v-if="isPdf(m)" viewBox="0 0 24 24"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" fill="none" stroke="currentColor" /><path d="M14 2v6h6" fill="none" stroke="currentColor" /></svg>
                  <svg v-else viewBox="0 0 24 24"><path d="M8 5v14l11-7z" /></svg>
                  {{ isPdf(m) ? t.pdf : t.youtube }}
                </span>
                <h4 class="card-title">{{ m.title }}</h4>
                <p v-if="m.description" class="card-desc">{{ m.description }}</p>
                <a class="open-btn" :href="openUrl(m)" target="_blank" rel="noopener noreferrer">
                  <svg v-if="isPdf(m)" viewBox="0 0 24 24"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" fill="none" stroke="currentColor" stroke-width="2" /><path d="M14 2v6h6" fill="none" stroke="currentColor" stroke-width="2" /></svg>
                  <svg v-else viewBox="0 0 24 24"><path d="M8 5v14l11-7z" /></svg>
                  {{ isPdf(m) ? t.openPdf : t.open }}
                </a>
              </div>
            </article>
          </div>
        </section>
      </template>
    </template>
  </div>
</template>

<style scoped>
.parent-materials {
  --gray: #64748b;
  --orange: var(--ds-orange, #f2a03d);
  display: flex;
  flex-direction: column;
  gap: 1.75rem;
  animation: pm-rise 0.45s ease both;
}
@keyframes pm-rise {
  from { opacity: 0; transform: translateY(14px); }
  to { opacity: 1; transform: translateY(0); }
}

/* Subject group */
.group { display: flex; flex-direction: column; gap: 1rem; }
.group-head { display: flex; align-items: center; gap: 0.65rem; }
.group-head h3 {
  position: relative;
  margin: 0;
  padding-inline-start: 0.8rem;
  font-size: 1.2rem;
  font-weight: 800;
  color: #1e293b;
}
.group-head h3::before {
  content: '';
  position: absolute;
  inset-inline-start: 0;
  top: 50%;
  transform: translateY(-50%);
  width: 5px;
  height: 1rem;
  border-radius: 3px;
  background: linear-gradient(180deg, var(--gray), var(--orange));
}
.count {
  min-width: 22px;
  height: 22px;
  padding: 0 0.45rem;
  border-radius: 999px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-size: 0.72rem;
  font-weight: 800;
  color: #475569;
  background: #e9eef5;
}

/* Grid */
.grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 1.25rem; }
.card {
  display: flex;
  flex-direction: column;
  background: #fff;
  border: 1px solid #e2e8f0;
  border-radius: 16px;
  overflow: hidden;
  box-shadow: 0 8px 22px rgba(30, 41, 59, 0.05);
  transition: transform 0.25s ease, box-shadow 0.25s ease, border-color 0.25s ease;
  animation: card-in 0.4s ease backwards;
  animation-delay: calc(min(var(--card-i, 0), 10) * 45ms);
}
@keyframes card-in { from { opacity: 0; transform: translateY(8px); } to { opacity: 1; transform: translateY(0); } }
.card:hover { transform: translateY(-3px); border-color: #cbd5e1; box-shadow: 0 18px 38px rgba(30, 41, 59, 0.1); }
@media (prefers-reduced-motion: reduce) { .card { animation: none; } }

/* Thumbnail */
.thumb { position: relative; display: block; aspect-ratio: 16 / 9; background: #1e293b; overflow: hidden; }
.thumb img { width: 100%; height: 100%; object-fit: cover; display: block; transition: transform 0.3s ease; }
.card:hover .thumb img { transform: scale(1.05); }
.thumb.pdf { display: flex; align-items: center; justify-content: center; background: linear-gradient(135deg, #475569, #1e293b); }
.pdf-ic { width: 62px; height: 62px; border-radius: 16px; display: inline-flex; align-items: center; justify-content: center; background: rgba(255, 255, 255, 0.12); border: 1px solid rgba(255, 255, 255, 0.2); }
.pdf-ic svg { width: 30px; height: 30px; fill: none; stroke: #fff; stroke-width: 1.6; stroke-linecap: round; stroke-linejoin: round; }
.play {
  position: absolute;
  inset: 0;
  margin: auto;
  width: 52px;
  height: 52px;
  border-radius: 50%;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  background: rgba(220, 38, 38, 0.92);
  box-shadow: 0 6px 18px rgba(0, 0, 0, 0.35);
}
.play svg { width: 24px; height: 24px; fill: #fff; margin-inline-start: 2px; }

.card-body { display: flex; flex-direction: column; gap: 0.5rem; padding: 1rem 1.1rem 1.1rem; flex: 1; }
.mat-badge {
  align-self: flex-start;
  display: inline-flex;
  align-items: center;
  gap: 0.3rem;
  padding: 0.24rem 0.6rem;
  border-radius: 999px;
  font-size: 0.68rem;
  font-weight: 800;
}
.mat-badge svg { width: 12px; height: 12px; }
.mat-badge.is-yt { color: #dc2626; background: #fee2e2; }
.mat-badge.is-yt svg { fill: currentColor; }
.mat-badge.is-pdf { color: #4338ca; background: #e0e7ff; }
.card-title { margin: 0; font-size: 1rem; font-weight: 800; color: #1e293b; line-height: 1.35; }
.card-desc {
  margin: 0;
  font-size: 0.85rem;
  color: #64748b;
  line-height: 1.5;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}
.open-btn {
  align-self: flex-start;
  margin-top: auto;
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.5rem 0.9rem;
  border-radius: 10px;
  font-size: 0.83rem;
  font-weight: 700;
  text-decoration: none;
  color: #475569;
  background: #f1f5f9;
  border: 1px solid #cbd5e1;
  transition: background 0.15s ease, transform 0.15s ease;
}
.open-btn:hover { background: #e2e8f0; transform: translateY(-1px); }
.open-btn svg { width: 14px; height: 14px; fill: currentColor; }
.open-btn svg[stroke] { fill: none; }

/* State cards */
.state-card {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  gap: 0.6rem;
  padding: 3rem 1.5rem;
  background: #fff;
  border: 1px solid #e2e8f0;
  border-radius: 18px;
  box-shadow: 0 8px 22px rgba(30, 41, 59, 0.05);
  color: #64748b;
}
.state-card p { margin: 0; font-size: 0.92rem; }
.state-card h3 { margin: 0; font-size: 1.2rem; font-weight: 800; color: #1e293b; }
.state-badge {
  width: 66px;
  height: 66px;
  border-radius: 50%;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 0.3rem;
  color: #64748b;
  background: radial-gradient(circle, #eef2f7, #e2e8f0);
}
.state-badge svg { width: 30px; height: 30px; fill: none; stroke: currentColor; stroke-width: 1.7; stroke-linecap: round; stroke-linejoin: round; }
.state-card.error-state { color: #b91c1c; }
.state-card.error-state svg { width: 34px; height: 34px; fill: none; stroke: currentColor; stroke-width: 1.7; stroke-linecap: round; stroke-linejoin: round; }
.spinner {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  border: 3px solid #e2e8f0;
  border-top-color: var(--gray);
  animation: spin 0.8s linear infinite;
}
@keyframes spin { to { transform: rotate(360deg); } }
.retry-btn {
  margin-top: 0.3rem;
  padding: 0.55rem 1.3rem;
  border: 1px solid #cbd5e1;
  border-radius: 10px;
  background: #fff;
  font-family: inherit;
  font-size: 0.85rem;
  font-weight: 700;
  color: #475569;
  cursor: pointer;
  transition: background 0.15s ease;
}
.retry-btn:hover { background: #f1f5f9; }

@media (prefers-reduced-motion: reduce) {
  .parent-materials { animation: none; }
  .spinner { animation-duration: 1.6s; }
}
</style>
