<script setup>
// Teacher "My Subjects". Lists the subjects the logged-in teacher is assigned to teach
// (GET /api/materials/my-subjects → subject + class info), each as a card with the class
// it's taught in and a level badge (from the teacher's own level, which all her classes
// share). Quick links jump to that page's Attendance / Grades. Green theme, bilingual
// (EN+AR) + RTL. Mirrors the teacher dashboard / materials design.
import { computed, onMounted, ref } from 'vue'
import { useAuthStore } from '../../stores/auth'
import { useLanguageStore } from '../../stores/language'
import TeacherPageHeader from '../../components/teacher/TeacherPageHeader.vue'
import { icons } from '../../components/admin/icons'
import { getMySubjects } from '../../api/materials'

const auth = useAuthStore()
const language = useLanguageStore()

// ---- Bilingual copy (per-component i18n pattern) ----
const content = {
  en: {
    countLabel: 'assigned subjects',
    levelSecondary: 'Secondary',
    levelHigh: 'High School',
    attendance: 'Attendance',
    grades: 'Grades',
    loading: 'Loading your subjects…',
    loadError: 'We couldn’t load your subjects. Please try again.',
    retry: 'Retry',
    empty: 'No subjects assigned yet',
    emptySub: 'Ask an administrator to assign you subjects — they’ll appear here.'
  },
  ar: {
    countLabel: 'مادة مُسندة',
    levelSecondary: 'إعدادي',
    levelHigh: 'ثانوي',
    attendance: 'الحضور',
    grades: 'الدرجات',
    loading: 'جارٍ تحميل موادك…',
    loadError: 'تعذّر تحميل موادك. يرجى المحاولة مرة أخرى.',
    retry: 'إعادة المحاولة',
    empty: 'لا توجد مواد مُسندة بعد',
    emptySub: 'اطلب من المسؤول إسناد مواد لك — ستظهر هنا.'
  }
}
const t = computed(() => content[language.lang])

// ---- The teacher's level (applies to all her classes; badge per card) ----
const levelKey = computed(() => (auth.user?.teacherLevel === 'HighSchool' ? 'is-high' : 'is-secondary'))
const levelLabel = computed(() => {
  const lvl = auth.user?.teacherLevel
  if (lvl === 'Secondary') return t.value.levelSecondary
  if (lvl === 'HighSchool') return t.value.levelHigh
  return ''
})

// ---- State ----
const subjects = ref([])
const loading = ref(true)
const loadError = ref('')

async function loadSubjects() {
  loading.value = true
  loadError.value = ''
  try {
    const { data } = await getMySubjects()
    subjects.value = data ?? []
  } catch (err) {
    loadError.value = err.message || t.value.loadError
    subjects.value = []
  } finally {
    loading.value = false
  }
}
onMounted(loadSubjects)
</script>

<template>
  <div class="teacher-subjects" :dir="language.dir">
    <TeacherPageHeader />

    <!-- Loading -->
    <div v-if="loading" class="state-card">
      <span class="spinner"></span>
      <p>{{ t.loading }}</p>
    </div>

    <!-- Error -->
    <div v-else-if="loadError" class="state-card error-state">
      <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="9" /><path d="M12 8v5M12 16h.01" /></svg>
      <p>{{ loadError }}</p>
      <button type="button" class="retry-btn" @click="loadSubjects">{{ t.retry }}</button>
    </div>

    <!-- Empty -->
    <div v-else-if="!subjects.length" class="state-card">
      <span class="state-badge"><svg viewBox="0 0 24 24" v-html="icons.subjects"></svg></span>
      <h3>{{ t.empty }}</h3>
      <p>{{ t.emptySub }}</p>
    </div>

    <!-- Subjects grid -->
    <template v-else>
      <div class="count-strip">
        <span class="count-pill">{{ subjects.length }}</span>
        <span class="count-text">{{ t.countLabel }}</span>
      </div>

      <div class="grid">
        <article v-for="(s, i) in subjects" :key="s.subjectId" class="card" :style="{ '--card-i': i }">
          <div class="card-top">
            <span class="subj-ic"><svg viewBox="0 0 24 24" v-html="icons.subjects"></svg></span>
            <span v-if="levelLabel" class="level-badge" :class="levelKey">{{ levelLabel }}</span>
          </div>

          <h3 class="subj-name">{{ s.subjectName }}</h3>
          <span class="class-line">
            <svg viewBox="0 0 24 24" v-html="icons.classes"></svg>
            {{ s.className }}
          </span>

          <div class="card-foot">
            <router-link :to="{ name: 'teacher-attendance' }" class="chip">
              <svg viewBox="0 0 24 24" v-html="icons.attendance"></svg>
              {{ t.attendance }}
            </router-link>
            <router-link :to="{ name: 'teacher-grades' }" class="chip">
              <svg viewBox="0 0 24 24" v-html="icons.grades"></svg>
              {{ t.grades }}
            </router-link>
          </div>
        </article>
      </div>
    </template>
  </div>
</template>

<style scoped>
.teacher-subjects {
  --green: #16a34a;
  --green-strong: #12b981;
  --orange: var(--ds-orange, #f2a03d);
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
  animation: ts-rise 0.45s ease both;
}
@keyframes ts-rise {
  from { opacity: 0; transform: translateY(14px); }
  to { opacity: 1; transform: translateY(0); }
}

/* Count strip */
.count-strip { display: flex; align-items: center; gap: 0.55rem; }
.count-pill {
  min-width: 26px;
  height: 26px;
  padding: 0 0.5rem;
  border-radius: 999px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-size: 0.8rem;
  font-weight: 800;
  color: #fff;
  background: linear-gradient(135deg, var(--green), var(--green-strong));
}
.count-text { font-size: 0.85rem; font-weight: 700; color: #345247; }

/* Grid */
.grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(260px, 1fr)); gap: 1.25rem; }
.card {
  position: relative;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  gap: 0.55rem;
  background: #fff;
  border: 1px solid #e6f0eb;
  border-radius: 16px;
  padding: 1.25rem 1.35rem 1.15rem;
  box-shadow: 0 8px 22px rgba(15, 54, 36, 0.05);
  transition: transform 0.25s ease, box-shadow 0.25s ease, border-color 0.25s ease;
  animation: card-in 0.4s ease backwards;
  animation-delay: calc(min(var(--card-i, 0), 12) * 45ms);
}
@keyframes card-in { from { opacity: 0; transform: translateY(8px); } to { opacity: 1; transform: translateY(0); } }
.card:hover { transform: translateY(-3px); border-color: #cfe7db; box-shadow: 0 18px 38px rgba(15, 54, 36, 0.1); }
/* green→orange accent line that grows across the top on hover */
.card::before {
  content: '';
  position: absolute;
  top: 0;
  inset-inline-start: 0;
  width: 100%;
  height: 3px;
  background: linear-gradient(90deg, var(--green), var(--orange));
  transform: scaleX(0);
  transform-origin: left;
  transition: transform 0.35s ease;
}
.card:hover::before { transform: scaleX(1); }
@media (prefers-reduced-motion: reduce) { .card { animation: none; } }

.card-top { display: flex; align-items: center; justify-content: space-between; gap: 0.5rem; }
.subj-ic {
  width: 40px;
  height: 40px;
  border-radius: 11px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: var(--green);
  background: linear-gradient(135deg, rgba(16, 163, 74, 0.12), rgba(242, 160, 61, 0.14));
}
.subj-ic svg { width: 20px; height: 20px; fill: none; stroke: currentColor; stroke-width: 1.9; stroke-linecap: round; stroke-linejoin: round; }
.level-badge {
  padding: 0.26rem 0.65rem;
  border-radius: 999px;
  font-size: 0.7rem;
  font-weight: 800;
  letter-spacing: 0.01em;
}
.level-badge.is-secondary { color: #15784c; background: #dcfce7; }
.level-badge.is-high { color: #4338ca; background: #e0e7ff; }

.subj-name { margin: 0.2rem 0 0; font-size: 1.1rem; font-weight: 800; color: #0f2a1e; line-height: 1.3; }
.class-line {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  font-size: 0.85rem;
  font-weight: 600;
  color: #6b8578;
}
.class-line svg { width: 15px; height: 15px; fill: none; stroke: var(--green); stroke-width: 1.8; stroke-linecap: round; stroke-linejoin: round; }

.card-foot { display: flex; gap: 0.5rem; margin-top: 0.6rem; }
.chip {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  padding: 0.4rem 0.75rem;
  border-radius: 9px;
  font-size: 0.8rem;
  font-weight: 700;
  text-decoration: none;
  color: #15784c;
  background: #eefaf3;
  border: 1px solid #cfe7db;
  transition: background 0.15s ease, transform 0.15s ease;
}
.chip:hover { background: #dcf5e8; transform: translateY(-1px); }
.chip svg { width: 14px; height: 14px; fill: none; stroke: currentColor; stroke-width: 1.9; stroke-linecap: round; stroke-linejoin: round; }

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
  border: 1px solid #e6f0eb;
  border-radius: 18px;
  box-shadow: 0 8px 22px rgba(15, 54, 36, 0.05);
  color: #5c7568;
}
.state-card p { margin: 0; font-size: 0.92rem; }
.state-card h3 { margin: 0; font-size: 1.2rem; font-weight: 800; color: #0f2a1e; }
.state-badge {
  width: 66px;
  height: 66px;
  border-radius: 50%;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 0.3rem;
  color: #16a34a;
  background: radial-gradient(circle, #e9f9f0, #dff3e8);
}
.state-badge svg { width: 30px; height: 30px; fill: none; stroke: currentColor; stroke-width: 1.7; stroke-linecap: round; stroke-linejoin: round; }
.state-card.error-state { color: #b91c1c; }
.state-card.error-state svg { width: 34px; height: 34px; fill: none; stroke: currentColor; stroke-width: 1.7; stroke-linecap: round; stroke-linejoin: round; }
.spinner {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  border: 3px solid #e2ece7;
  border-top-color: var(--green);
  animation: spin 0.8s linear infinite;
}
@keyframes spin { to { transform: rotate(360deg); } }
.retry-btn {
  margin-top: 0.3rem;
  padding: 0.55rem 1.3rem;
  border: 1px solid #cfe7db;
  border-radius: 10px;
  background: #fff;
  font-family: inherit;
  font-size: 0.85rem;
  font-weight: 700;
  color: #15784c;
  cursor: pointer;
  transition: background 0.15s ease;
}
.retry-btn:hover { background: #eefaf3; }

@media (prefers-reduced-motion: reduce) {
  .teacher-subjects { animation: none; }
  .spinner { animation-duration: 1.6s; }
}
</style>
