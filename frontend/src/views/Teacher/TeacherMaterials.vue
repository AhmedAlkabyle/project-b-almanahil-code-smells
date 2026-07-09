<script setup>
// Teacher "Learning Materials". Stage 1 = YouTube links; Stage 2 = PDF uploads (to Supabase
// Storage via the backend). The teacher picks a subject, sees its materials, and adds new
// ones — choosing 'YouTube link' or 'PDF file'. Green theme, bilingual (EN+AR) + RTL.
import { computed, onMounted, ref, watch } from 'vue'
import { useLanguageStore } from '../../stores/language'
import TeacherPageHeader from '../../components/teacher/TeacherPageHeader.vue'
import { getMySubjects, getSubjectMaterials, addMaterial, uploadPdf, deleteMaterial } from '../../api/materials'
import { youtubeId, youtubeThumb, youtubeWatchUrl } from '../../utils/youtube'

const language = useLanguageStore()

// ---- Bilingual copy (per-component i18n pattern) ----
const content = {
  en: {
    subject: 'Subject',
    addTitle: 'Add a material',
    typeYoutube: 'YouTube link',
    typePdf: 'PDF file',
    titleLabel: 'Title',
    titlePh: 'e.g. Chapter 3 — Photosynthesis',
    descLabel: 'Description (optional)',
    descPh: 'A short note about this material…',
    urlLabel: 'YouTube link',
    urlPh: 'https://www.youtube.com/watch?v=…',
    fileLabel: 'PDF file',
    choosePdf: 'Choose a PDF file…',
    add: 'Add material',
    adding: 'Adding…',
    uploading: 'Uploading…',
    youtube: 'YouTube',
    pdf: 'PDF',
    open: 'Open video',
    openPdf: 'Open PDF',
    del: 'Delete',
    confirmDel: 'Delete this material?',
    yes: 'Delete',
    cancel: 'Cancel',
    loadingSubjects: 'Loading your subjects…',
    subjectsError: 'We couldn’t load your subjects. Please try again.',
    noSubjects: 'No subjects assigned yet',
    noSubjectsSub: 'Ask an administrator to assign you a subject so you can add materials.',
    loadingMaterials: 'Loading materials…',
    materialsError: 'We couldn’t load the materials. Please try again.',
    noMaterials: 'No materials yet',
    noMaterialsSub: 'Add your first YouTube link or PDF for this subject using the form above.',
    retry: 'Retry',
    errTitle: 'Please enter a title.',
    errUrl: 'Please enter a valid YouTube link.',
    errFile: 'Please choose a PDF file.',
    errFileType: 'Only PDF files are allowed.',
    errFileSize: 'The file is too large (maximum 50 MB).'
  },
  ar: {
    subject: 'المادة',
    addTitle: 'إضافة مادة تعليمية',
    typeYoutube: 'رابط يوتيوب',
    typePdf: 'ملف PDF',
    titleLabel: 'العنوان',
    titlePh: 'مثال: الفصل ٣ — البناء الضوئي',
    descLabel: 'الوصف (اختياري)',
    descPh: 'ملاحظة قصيرة عن هذه المادة…',
    urlLabel: 'رابط يوتيوب',
    urlPh: 'https://www.youtube.com/watch?v=…',
    fileLabel: 'ملف PDF',
    choosePdf: 'اختر ملف PDF…',
    add: 'إضافة المادة',
    adding: 'جارٍ الإضافة…',
    uploading: 'جارٍ الرفع…',
    youtube: 'يوتيوب',
    pdf: 'PDF',
    open: 'فتح الفيديو',
    openPdf: 'فتح الملف',
    del: 'حذف',
    confirmDel: 'حذف هذه المادة؟',
    yes: 'حذف',
    cancel: 'إلغاء',
    loadingSubjects: 'جارٍ تحميل موادك…',
    subjectsError: 'تعذّر تحميل موادك. يرجى المحاولة مرة أخرى.',
    noSubjects: 'لا توجد مواد مُسندة بعد',
    noSubjectsSub: 'اطلب من المسؤول إسناد مادة لك لتتمكن من إضافة المواد التعليمية.',
    loadingMaterials: 'جارٍ تحميل المواد…',
    materialsError: 'تعذّر تحميل المواد. يرجى المحاولة مرة أخرى.',
    noMaterials: 'لا توجد مواد بعد',
    noMaterialsSub: 'أضف أول رابط يوتيوب أو ملف PDF لهذه المادة باستخدام النموذج أعلاه.',
    retry: 'إعادة المحاولة',
    errTitle: 'يرجى إدخال عنوان.',
    errUrl: 'يرجى إدخال رابط يوتيوب صحيح.',
    errFile: 'يرجى اختيار ملف PDF.',
    errFileType: 'يُسمح بملفات PDF فقط.',
    errFileSize: 'الملف كبير جداً (الحد الأقصى ٥٠ ميجابايت).'
  }
}
const t = computed(() => content[language.lang])

const MATERIAL_TYPES = ['YouTube', 'Pdf']
const MAX_PDF_BYTES = 50 * 1024 * 1024

// ---- State ----
const subjects = ref([])
const subjectsLoading = ref(true)
const subjectsError = ref('')
const selectedSubjectId = ref('')

const materials = ref([])
const materialsLoading = ref(false)
const materialsError = ref('')

// Add form
const materialType = ref('YouTube')
const form = ref({ title: '', description: '', url: '' })
const selectedFile = ref(null)
const fileInput = ref(null)
const formError = ref('')
const adding = ref(false)

// Delete (inline confirm)
const pendingDeleteId = ref(null)
const deletingId = ref(null)

watch(materialType, () => {
  formError.value = ''
})

// ---- Flash ----
const flashMsg = ref(null)
let flashTimer = null
function flash(type, text) {
  flashMsg.value = { type, text }
  clearTimeout(flashTimer)
  // Errors stay until dismissed (so the real upload error is readable); others auto-hide.
  if (type !== 'error') flashTimer = setTimeout(() => (flashMsg.value = null), 4500)
}

// ---- Helpers ----
const subjectLabel = (s) => `${s.subjectName} — ${s.className}`
const isPdf = (m) => m.materialType === 'Pdf'
const vidId = (m) => youtubeId(m.url)
const thumb = (m) => youtubeThumb(vidId(m))
const openUrl = (m) => (isPdf(m) ? m.url : youtubeWatchUrl(vidId(m), m.url))
const submitLabel = computed(() =>
  adding.value ? (materialType.value === 'Pdf' ? t.value.uploading : t.value.adding) : t.value.add
)

// ---- Load: the teacher's subjects ----
async function loadSubjects() {
  subjectsLoading.value = true
  subjectsError.value = ''
  try {
    const { data } = await getMySubjects()
    subjects.value = data ?? []
    if (subjects.value.length) selectedSubjectId.value = subjects.value[0].subjectId
  } catch (err) {
    subjectsError.value = err.message || t.value.subjectsError
  } finally {
    subjectsLoading.value = false
  }
}
onMounted(loadSubjects)

// ---- Load: materials for the chosen subject ----
async function loadMaterials() {
  if (!selectedSubjectId.value) return
  materialsLoading.value = true
  materialsError.value = ''
  pendingDeleteId.value = null
  try {
    const { data } = await getSubjectMaterials(selectedSubjectId.value)
    materials.value = data ?? []
  } catch (err) {
    materialsError.value = err.message
    materials.value = []
  } finally {
    materialsLoading.value = false
  }
}
watch(selectedSubjectId, loadMaterials)

// ---- Add a material (YouTube or PDF) ----
function onFileChange(e) {
  selectedFile.value = e.target.files?.[0] || null
  formError.value = ''
}
function resetForm() {
  form.value = { title: '', description: '', url: '' }
  selectedFile.value = null
  if (fileInput.value) fileInput.value.value = ''
}

async function submitAdd() {
  if (adding.value) return
  formError.value = ''
  const title = form.value.title.trim()
  if (!title) return (formError.value = t.value.errTitle)

  const desc = form.value.description.trim()
  let action

  if (materialType.value === 'YouTube') {
    const url = form.value.url.trim()
    if (!youtubeId(url)) return (formError.value = t.value.errUrl)
    action = () =>
      addMaterial({
        subjectId: Number(selectedSubjectId.value),
        title,
        description: desc || null,
        materialType: 'YouTube',
        url
      })
  } else {
    const f = selectedFile.value
    if (!f) return (formError.value = t.value.errFile)
    if (!(f.type === 'application/pdf' || /\.pdf$/i.test(f.name))) return (formError.value = t.value.errFileType)
    if (f.size > MAX_PDF_BYTES) return (formError.value = t.value.errFileSize)
    const fd = new FormData()
    fd.append('subjectId', String(Number(selectedSubjectId.value)))
    fd.append('title', title)
    if (desc) fd.append('description', desc)
    fd.append('file', f)
    action = () => uploadPdf(fd)
  }

  adding.value = true
  try {
    const { message } = await action()
    flash('success', message)
    resetForm()
    await loadMaterials()
  } catch (err) {
    flash('error', err.message) // surfaces the backend's friendly 403 / upload error too
  } finally {
    adding.value = false
  }
}

// ---- Delete a material (inline confirm) ----
function askDelete(id) {
  pendingDeleteId.value = id
}
function cancelDelete() {
  pendingDeleteId.value = null
}
async function confirmDelete(id) {
  deletingId.value = id
  try {
    const { message } = await deleteMaterial(id)
    flash('success', message)
    materials.value = materials.value.filter((m) => m.id !== id)
  } catch (err) {
    flash('error', err.message)
  } finally {
    deletingId.value = null
    pendingDeleteId.value = null
  }
}
</script>

<template>
  <div class="teacher-materials" :dir="language.dir">
    <TeacherPageHeader />

    <!-- Flash -->
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

    <!-- Subjects: loading / error / empty -->
    <div v-if="subjectsLoading" class="state-card">
      <span class="spinner"></span>
      <p>{{ t.loadingSubjects }}</p>
    </div>

    <div v-else-if="subjectsError" class="state-card error-state">
      <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="9" /><path d="M12 8v5M12 16h.01" /></svg>
      <p>{{ subjectsError }}</p>
      <button type="button" class="retry-btn" @click="loadSubjects">{{ t.retry }}</button>
    </div>

    <div v-else-if="!subjects.length" class="state-card">
      <span class="state-badge"><svg viewBox="0 0 24 24"><path d="M5 4.5A1.5 1.5 0 0 1 6.5 3H19v15H6.5A1.5 1.5 0 0 0 5 19.5z" /><path d="M19 18v3H6.5A1.5 1.5 0 0 1 5 19.5" /></svg></span>
      <h3>{{ t.noSubjects }}</h3>
      <p>{{ t.noSubjectsSub }}</p>
    </div>

    <template v-else>
      <!-- Subject picker -->
      <div class="controls">
        <div class="ctrl">
          <label>{{ t.subject }}</label>
          <select v-model="selectedSubjectId" class="select">
            <option v-for="s in subjects" :key="s.subjectId" :value="s.subjectId">{{ subjectLabel(s) }}</option>
          </select>
        </div>
      </div>

      <!-- Add form -->
      <form class="form-card" @submit.prevent="submitAdd">
        <h3 class="form-title">
          <span class="ft-badge"><svg viewBox="0 0 24 24"><path d="M12 5v14M5 12h14" /></svg></span>
          {{ t.addTitle }}
        </h3>

        <!-- Type toggle -->
        <div class="type-toggle" role="group">
          <button
            v-for="mt in MATERIAL_TYPES"
            :key="mt"
            type="button"
            class="type-btn"
            :class="{ active: materialType === mt }"
            @click="materialType = mt"
          >
            <svg v-if="mt === 'YouTube'" viewBox="0 0 24 24"><path d="M8 5v14l11-7z" fill="currentColor" stroke="none" /></svg>
            <svg v-else viewBox="0 0 24 24"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" /><path d="M14 2v6h6" /></svg>
            {{ mt === 'YouTube' ? t.typeYoutube : t.typePdf }}
          </button>
        </div>

        <div class="fields">
          <div class="field field-wide">
            <label>{{ t.titleLabel }}</label>
            <input v-model="form.title" type="text" :placeholder="t.titlePh" maxlength="200" />
          </div>

          <!-- YouTube: url — PDF: file -->
          <div v-if="materialType === 'YouTube'" class="field field-wide">
            <label>{{ t.urlLabel }}</label>
            <input v-model="form.url" type="url" :placeholder="t.urlPh" inputmode="url" />
          </div>
          <div v-else class="field field-wide">
            <label>{{ t.fileLabel }}</label>
            <label class="file-drop" :class="{ chosen: selectedFile }">
              <input ref="fileInput" type="file" accept="application/pdf,.pdf" @change="onFileChange" />
              <svg viewBox="0 0 24 24"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" /><path d="M14 2v6h6" /></svg>
              <span>{{ selectedFile ? selectedFile.name : t.choosePdf }}</span>
            </label>
          </div>

          <div class="field field-full">
            <label>{{ t.descLabel }}</label>
            <textarea v-model="form.description" :placeholder="t.descPh" rows="2" maxlength="1000"></textarea>
          </div>
        </div>

        <p v-if="formError" class="form-err">{{ formError }}</p>

        <div class="form-foot">
          <button type="submit" class="add-btn" :disabled="adding">
            <span v-if="adding" class="btn-spinner"></span>{{ submitLabel }}
          </button>
        </div>
      </form>

      <!-- Materials: loading / error / empty / grid -->
      <div v-if="materialsLoading" class="state-card">
        <span class="spinner"></span>
        <p>{{ t.loadingMaterials }}</p>
      </div>

      <div v-else-if="materialsError" class="state-card error-state">
        <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="9" /><path d="M12 8v5M12 16h.01" /></svg>
        <p>{{ materialsError }}</p>
        <button type="button" class="retry-btn" @click="loadMaterials">{{ t.retry }}</button>
      </div>

      <div v-else-if="!materials.length" class="state-card">
        <span class="state-badge"><svg viewBox="0 0 24 24"><path d="m10 9 5 3-5 3z" /><rect x="3" y="4" width="18" height="16" rx="3" /></svg></span>
        <h3>{{ t.noMaterials }}</h3>
        <p>{{ t.noMaterialsSub }}</p>
      </div>

      <div v-else class="grid">
        <article v-for="(m, i) in materials" :key="m.id" class="card" :style="{ '--card-i': i }">
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
            <h3 class="card-title">{{ m.title }}</h3>
            <p v-if="m.description" class="card-desc">{{ m.description }}</p>

            <div v-if="pendingDeleteId === m.id" class="confirm">
              <span>{{ t.confirmDel }}</span>
              <div class="confirm-actions">
                <button type="button" class="mini danger" :disabled="deletingId === m.id" @click="confirmDelete(m.id)">{{ t.yes }}</button>
                <button type="button" class="mini" @click="cancelDelete">{{ t.cancel }}</button>
              </div>
            </div>
            <div v-else class="card-foot">
              <a class="open-btn" :href="openUrl(m)" target="_blank" rel="noopener noreferrer">
                <svg v-if="isPdf(m)" viewBox="0 0 24 24"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" fill="none" stroke="currentColor" stroke-width="2" /><path d="M14 2v6h6" fill="none" stroke="currentColor" stroke-width="2" /></svg>
                <svg v-else viewBox="0 0 24 24"><path d="M8 5v14l11-7z" /></svg>
                {{ isPdf(m) ? t.openPdf : t.open }}
              </a>
              <button type="button" class="act delete" :title="t.del" :aria-label="t.del" @click="askDelete(m.id)">
                <svg viewBox="0 0 24 24"><path d="M3 6h18" /><path d="M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2" /><path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" /></svg>
              </button>
            </div>
          </div>
        </article>
      </div>
    </template>
  </div>
</template>

<style scoped>
.teacher-materials {
  --green: #16a34a;
  --green-strong: #12b981;
  --orange: var(--ds-orange, #f2a03d);
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
  animation: t-rise 0.45s ease both;
}
@keyframes t-rise {
  from { opacity: 0; transform: translateY(14px); }
  to { opacity: 1; transform: translateY(0); }
}

/* Flash */
.flash {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  padding: 0.85rem 1.1rem;
  border-radius: 12px;
  font-size: 0.9rem;
  font-weight: 600;
}
.flash svg { width: 20px; height: 20px; flex-shrink: 0; fill: none; stroke: currentColor; stroke-width: 2; stroke-linecap: round; stroke-linejoin: round; }
.flash.success { color: #15803d; background: #f0fdf4; border: 1px solid #bbf7d0; }
.flash.error { color: #b91c1c; background: #fef2f2; border: 1px solid #fecaca; }
.flash span { flex: 1; }
.flash-x { border: none; background: transparent; color: inherit; cursor: pointer; padding: 0.15rem; opacity: 0.6; }
.flash-x:hover { opacity: 1; }
.flash-x svg { width: 15px; height: 15px; }
.flash-enter-active, .flash-leave-active { transition: opacity 0.25s ease, transform 0.25s ease; }
.flash-enter-from, .flash-leave-to { opacity: 0; transform: translateY(-6px); }

/* Cards shell */
.controls,
.form-card {
  position: relative;
  background: #fff;
  border: 1px solid #e6f0eb;
  border-radius: 18px;
  box-shadow: 0 8px 22px rgba(15, 54, 36, 0.05);
  overflow: hidden;
}
.controls::before,
.form-card::before {
  content: '';
  position: absolute;
  top: 0;
  inset-inline: 0;
  height: 3px;
  z-index: 1;
  background: linear-gradient(90deg, var(--green), var(--orange));
}

/* Subject picker */
.controls { display: flex; flex-wrap: wrap; gap: 1rem; padding: 1.2rem 1.25rem; }
.ctrl { display: flex; flex-direction: column; gap: 0.4rem; min-width: 240px; flex: 1; }
.ctrl label { font-size: 0.8rem; font-weight: 700; color: #345247; }
.select {
  width: 100%;
  box-sizing: border-box;
  padding: 0.7rem 0.9rem;
  font-size: 0.92rem;
  font-family: inherit;
  color: #0f2a1e;
  background: #f5faf7;
  border: 1.5px solid #e2ece7;
  border-radius: 12px;
  outline: none;
  cursor: pointer;
  transition: border-color 0.2s ease, box-shadow 0.2s ease, background 0.2s ease;
}
.select:focus { background: #fff; border-color: var(--green); box-shadow: 0 0 0 4px rgba(22, 163, 74, 0.13); }

/* Add form */
.form-card { padding: 1.3rem 1.4rem; }
.form-title { display: flex; align-items: center; gap: 0.6rem; margin: 0 0 1rem; font-size: 1.1rem; font-weight: 800; color: #0f2a1e; }
.ft-badge { width: 32px; height: 32px; border-radius: 9px; display: inline-flex; align-items: center; justify-content: center; color: var(--green); background: linear-gradient(135deg, rgba(16, 163, 74, 0.12), rgba(242, 160, 61, 0.14)); }
.ft-badge svg { width: 17px; height: 17px; fill: none; stroke: currentColor; stroke-width: 2.2; stroke-linecap: round; }

/* Type toggle */
.type-toggle {
  display: inline-flex;
  margin-bottom: 1rem;
  border: 1.5px solid #e2ece7;
  border-radius: 12px;
  overflow: hidden;
  background: #f5faf7;
}
.type-btn {
  display: inline-flex;
  align-items: center;
  gap: 0.45rem;
  padding: 0.6rem 1.1rem;
  border: none;
  background: transparent;
  font-family: inherit;
  font-size: 0.87rem;
  font-weight: 700;
  color: #6b8578;
  cursor: pointer;
  transition: background 0.15s ease, color 0.15s ease;
}
.type-btn:not(:last-child) { border-inline-end: 1px solid #e2ece7; }
.type-btn svg { width: 16px; height: 16px; fill: none; stroke: currentColor; stroke-width: 1.9; stroke-linecap: round; stroke-linejoin: round; }
.type-btn:hover:not(.active) { background: #eef6f1; color: #0f2a1e; }
.type-btn.active { background: linear-gradient(135deg, var(--green), var(--green-strong)); color: #fff; }

.fields { display: grid; grid-template-columns: 1fr 1fr; gap: 0.9rem; }
.field { display: flex; flex-direction: column; gap: 0.4rem; }
.field-full { grid-column: 1 / -1; }
.field label { font-size: 0.8rem; font-weight: 700; color: #345247; }
.field input,
.field textarea {
  width: 100%;
  box-sizing: border-box;
  padding: 0.65rem 0.85rem;
  font-size: 0.9rem;
  font-family: inherit;
  color: #0f2a1e;
  background: #f5faf7;
  border: 1.5px solid #e2ece7;
  border-radius: 11px;
  outline: none;
  resize: vertical;
  transition: border-color 0.2s ease, box-shadow 0.2s ease, background 0.2s ease;
}
.field input:focus,
.field textarea:focus { background: #fff; border-color: var(--green); box-shadow: 0 0 0 3px rgba(22, 163, 74, 0.12); }

/* File picker */
.file-drop {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  padding: 0.7rem 0.9rem;
  border: 1.5px dashed #cfe0d7;
  border-radius: 11px;
  background: #f5faf7;
  cursor: pointer;
  color: #6b8578;
  font-size: 0.88rem;
  font-weight: 600;
  transition: border-color 0.2s ease, background 0.2s ease, color 0.2s ease;
}
.file-drop:hover { border-color: var(--green); background: #eefaf3; }
.file-drop.chosen { border-style: solid; border-color: #a7d8bf; color: #0f2a1e; background: #fff; }
.file-drop input { display: none; }
.file-drop svg { width: 20px; height: 20px; flex-shrink: 0; fill: none; stroke: var(--green); stroke-width: 1.8; stroke-linecap: round; stroke-linejoin: round; }
.file-drop span { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }

.form-err {
  margin: 0.8rem 0 0;
  padding: 0.55rem 0.85rem;
  border-radius: 10px;
  background: #fef2f2;
  border: 1px solid #fecaca;
  color: #b91c1c;
  font-size: 0.85rem;
  font-weight: 600;
}
.form-foot { display: flex; justify-content: flex-end; margin-top: 1rem; }
.add-btn {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.65rem 1.5rem;
  border: none;
  border-radius: 11px;
  font-family: inherit;
  font-size: 0.9rem;
  font-weight: 800;
  color: #fff;
  background: linear-gradient(135deg, var(--green), var(--green-strong));
  box-shadow: 0 8px 18px rgba(16, 163, 74, 0.3);
  cursor: pointer;
  transition: transform 0.15s ease, box-shadow 0.2s ease, opacity 0.2s ease;
}
.add-btn:hover:not(:disabled) { transform: translateY(-1px); box-shadow: 0 12px 24px rgba(16, 163, 74, 0.4); }
.add-btn:disabled { opacity: 0.7; cursor: not-allowed; }
.btn-spinner {
  width: 15px;
  height: 15px;
  border-radius: 50%;
  border: 2px solid rgba(255, 255, 255, 0.5);
  border-top-color: #fff;
  animation: spin 0.7s linear infinite;
}

/* Materials grid */
.grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 1.25rem; }
.card {
  display: flex;
  flex-direction: column;
  background: #fff;
  border: 1px solid #e6f0eb;
  border-radius: 16px;
  overflow: hidden;
  box-shadow: 0 8px 22px rgba(15, 54, 36, 0.05);
  transition: transform 0.25s ease, box-shadow 0.25s ease, border-color 0.25s ease;
  animation: card-in 0.4s ease backwards;
  animation-delay: calc(min(var(--card-i, 0), 10) * 45ms);
}
@keyframes card-in { from { opacity: 0; transform: translateY(8px); } to { opacity: 1; transform: translateY(0); } }
.card:hover { transform: translateY(-3px); border-color: #cfe7db; box-shadow: 0 18px 38px rgba(15, 54, 36, 0.1); }
@media (prefers-reduced-motion: reduce) { .card { animation: none; } }

/* Thumbnail */
.thumb { position: relative; display: block; aspect-ratio: 16 / 9; background: #0f2a1e; overflow: hidden; }
.thumb img { width: 100%; height: 100%; object-fit: cover; display: block; transition: transform 0.3s ease; }
.card:hover .thumb img { transform: scale(1.05); }
.thumb.pdf { display: flex; align-items: center; justify-content: center; background: linear-gradient(135deg, #14532d, #0f2a1e); }
.pdf-ic { width: 62px; height: 62px; border-radius: 16px; display: inline-flex; align-items: center; justify-content: center; background: rgba(255, 255, 255, 0.1); border: 1px solid rgba(255, 255, 255, 0.18); }
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
.mat-badge.is-pdf svg { width: 12px; height: 12px; }
.card-title { margin: 0; font-size: 1rem; font-weight: 800; color: #0f2a1e; line-height: 1.35; }
.card-desc {
  margin: 0;
  font-size: 0.85rem;
  color: #6b8578;
  line-height: 1.5;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}
.card-foot { display: flex; align-items: center; justify-content: space-between; gap: 0.5rem; margin-top: auto; padding-top: 0.35rem; }
.open-btn {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.5rem 0.9rem;
  border-radius: 10px;
  font-size: 0.83rem;
  font-weight: 700;
  text-decoration: none;
  color: #15784c;
  background: #eefaf3;
  border: 1px solid #cfe7db;
  transition: background 0.15s ease, transform 0.15s ease;
}
.open-btn:hover { background: #dcf5e8; transform: translateY(-1px); }
.open-btn svg { width: 14px; height: 14px; }
.open-btn svg[stroke] { fill: none; }
.act {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 34px;
  height: 34px;
  border: 1px solid #e6ebf0;
  border-radius: 10px;
  background: #fff;
  color: #64748b;
  cursor: pointer;
  transition: background 0.15s ease, color 0.15s ease, border-color 0.15s ease, transform 0.15s ease;
}
.act svg { width: 16px; height: 16px; fill: none; stroke: currentColor; stroke-width: 1.9; stroke-linecap: round; stroke-linejoin: round; }
.act.delete:hover { color: #dc2626; border-color: #f6c9c9; background: #fef2f2; transform: translateY(-1px); }

/* Inline delete confirm */
.confirm { margin-top: auto; padding-top: 0.35rem; display: flex; flex-direction: column; gap: 0.5rem; }
.confirm > span { font-size: 0.85rem; font-weight: 700; color: #b91c1c; }
.confirm-actions { display: flex; gap: 0.5rem; }
.mini {
  padding: 0.4rem 0.9rem;
  border-radius: 9px;
  font-family: inherit;
  font-size: 0.82rem;
  font-weight: 700;
  cursor: pointer;
  border: 1px solid #e2ece7;
  background: #fff;
  color: #475f55;
  transition: background 0.15s ease;
}
.mini:hover { background: #f1f5f3; }
.mini.danger { color: #fff; background: #dc2626; border-color: #dc2626; }
.mini.danger:hover { background: #b91c1c; }
.mini:disabled { opacity: 0.6; cursor: not-allowed; }

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

@media (max-width: 640px) {
  .fields { grid-template-columns: 1fr; }
  .type-toggle { width: 100%; }
  .type-btn { flex: 1; justify-content: center; }
}
@media (prefers-reduced-motion: reduce) {
  .teacher-materials { animation: none; }
  .spinner, .btn-spinner { animation-duration: 1.6s; }
}
</style>
