<script setup>
// Menu-based help chatbot (Module 6). A floating panel opened from the "Ask our chatbot"
// button in every role's top bar. 100% local — no backend, no external API, no AI. It
// shows a tidy, ordered list of preset questions; clicking one reveals its preset answer
// with a "Back to questions" link. Restrained, school-appropriate design: the school navy
// (#1E4C9A) as the single accent on clean white surfaces — no gradients, minimal shadow.
// Bilingual (EN+AR) + RTL. Data: utils/chatbotFaq.js.
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useLanguageStore } from '../stores/language'
import { chatbotFaq } from '../utils/chatbotFaq'

const props = defineProps({
  open: { type: Boolean, default: false }
})
const emit = defineEmits(['close'])

const language = useLanguageStore()

// ---- Bilingual UI copy (per-component i18n pattern) ----
const content = {
  en: {
    title: 'Almanahil Assistant',
    status: 'Help',
    greeting: "Hi! Choose a question below and I'll show you how.",
    back: 'Back to questions',
    close: 'Close'
  },
  ar: {
    title: 'مساعد المناهل',
    status: 'مساعدة',
    greeting: 'مرحباً! اختر سؤالاً من الأسفل وسأوضّح لك الطريقة.',
    back: 'العودة إلى الأسئلة',
    close: 'إغلاق'
  }
}
const t = computed(() => content[language.lang])
const faq = chatbotFaq

// ---- Master–detail state ----
const selected = ref(null) // the chosen FAQ item, or null (showing the list)
const bodyEl = ref(null)

const selectedIndex = computed(() =>
  selected.value ? faq.findIndex((f) => f.id === selected.value.id) : -1
)

function scrollTop() {
  nextTick(() => {
    if (bodyEl.value) bodyEl.value.scrollTop = 0
  })
}
function selectItem(item) {
  selected.value = item
  scrollTop()
}
function back() {
  selected.value = null
  scrollTop()
}
function close() {
  emit('close')
}

// Esc: step back to the list if an answer is open, otherwise close the panel.
function onKey(e) {
  if (e.key !== 'Escape' || !props.open) return
  if (selected.value) back()
  else close()
}
onMounted(() => document.addEventListener('keydown', onKey))
onBeforeUnmount(() => document.removeEventListener('keydown', onKey))

// Always (re)open on the full question list.
watch(() => props.open, (isOpen) => {
  if (isOpen) {
    selected.value = null
    scrollTop()
  }
})
</script>

<template>
  <Transition name="cbp">
    <section
      v-if="open"
      class="cbp"
      :dir="language.dir"
      role="dialog"
      :aria-label="t.title"
    >
      <!-- Header -->
      <header class="cbp-head">
        <span class="head-icon">
          <svg viewBox="0 0 24 24"><path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z" /></svg>
        </span>
        <div class="head-text">
          <strong>{{ t.title }}</strong>
          <span>{{ t.status }}</span>
        </div>
        <button type="button" class="head-x" @click="close" :aria-label="t.close">
          <svg viewBox="0 0 24 24"><path d="M18 6 6 18M6 6l12 12" /></svg>
        </button>
      </header>

      <!-- Body: question list OR selected answer -->
      <div class="cbp-body" ref="bodyEl">
        <Transition name="view" mode="out-in">
          <!-- ===== LIST VIEW ===== -->
          <div v-if="!selected" key="list" class="view">
            <p class="greeting">{{ t.greeting }}</p>

            <ol class="q-list">
              <li v-for="(item, i) in faq" :key="item.id">
                <button type="button" class="q-row" @click="selectItem(item)">
                  <span class="q-num">{{ i + 1 }}</span>
                  <span class="q-text">{{ item.q[language.lang] }}</span>
                  <span class="q-chev"><svg viewBox="0 0 24 24"><path d="M9 6l6 6-6 6" /></svg></span>
                </button>
              </li>
            </ol>
          </div>

          <!-- ===== ANSWER VIEW ===== -->
          <div v-else key="answer" class="view">
            <button type="button" class="back-link" @click="back">
              <svg viewBox="0 0 24 24"><path d="M15 6l-6 6 6 6" /></svg>
              {{ t.back }}
            </button>

            <div class="answer">
              <h4 class="answer-q">{{ selected.q[language.lang] }}</h4>
              <p class="answer-a">{{ selected.a[language.lang] }}</p>
            </div>
          </div>
        </Transition>
      </div>
    </section>
  </Transition>
</template>

<style scoped>
.cbp {
  --navy: #1e4c9a;

  position: fixed;
  bottom: 1.5rem;
  inset-inline-end: 1.5rem;
  z-index: 1200;
  width: min(380px, calc(100vw - 2rem));
  max-height: min(600px, calc(100vh - 3rem));
  display: flex;
  flex-direction: column;
  overflow: hidden;
  background: #fff;
  border: 1px solid #e5e7eb;
  border-radius: 12px;
  box-shadow: 0 12px 32px rgba(17, 24, 39, 0.14);
  font-family: inherit;
  color: #1f2937;
}

/* Header — solid school navy, no gradient */
.cbp-head {
  display: flex;
  align-items: center;
  gap: 0.7rem;
  padding: 0.85rem 1rem;
  background: var(--navy);
  color: #fff;
}
.head-icon {
  display: inline-flex;
  flex-shrink: 0;
}
.head-icon svg {
  width: 21px;
  height: 21px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.8;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.head-text {
  display: flex;
  flex-direction: column;
  line-height: 1.25;
  flex: 1;
  min-width: 0;
}
.head-text strong { font-size: 1rem; font-weight: 700; letter-spacing: -0.01em; }
.head-text span { font-size: 0.74rem; color: rgba(255, 255, 255, 0.72); }
.head-x {
  flex-shrink: 0;
  width: 28px;
  height: 28px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border: none;
  border-radius: 6px;
  background: transparent;
  color: rgba(255, 255, 255, 0.85);
  cursor: pointer;
  transition: background 0.15s ease, color 0.15s ease;
}
.head-x:hover { background: rgba(255, 255, 255, 0.14); color: #fff; }
.head-x svg { width: 16px; height: 16px; fill: none; stroke: currentColor; stroke-width: 2; stroke-linecap: round; stroke-linejoin: round; }

/* Body */
.cbp-body {
  flex: 1;
  min-height: 0;
  overflow-y: auto;
  padding: 0.9rem 1rem 1.1rem;
  background: #fff;
}
.view { display: flex; flex-direction: column; }

.greeting {
  margin: 0 0 0.85rem;
  font-size: 0.9rem;
  line-height: 1.55;
  color: #4b5563;
}

/* Ordered question list — a calm, divided menu (not cards) */
.q-list {
  list-style: none;
  margin: 0;
  padding: 0;
  border: 1px solid #e5e7eb;
  border-radius: 10px;
  overflow: hidden;
}
.q-list li + li .q-row { border-top: 1px solid #eef1f5; }
.q-row {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  width: 100%;
  padding: 0.8rem 0.9rem;
  border: none;
  background: #fff;
  font-family: inherit;
  text-align: start;
  cursor: pointer;
  transition: background 0.13s ease;
}
.q-row:hover { background: #f2f6fc; }
.q-num {
  flex-shrink: 0;
  width: 1.1rem;
  font-size: 0.8rem;
  font-weight: 600;
  color: #9aa4b2;
  text-align: center;
  font-variant-numeric: tabular-nums;
}
.q-text {
  flex: 1;
  min-width: 0;
  font-size: 0.9rem;
  font-weight: 500;
  line-height: 1.4;
  color: #1f2937;
}
.q-chev { flex-shrink: 0; display: inline-flex; color: #c2c9d2; transition: color 0.13s ease; }
.q-chev svg { width: 18px; height: 18px; fill: none; stroke: currentColor; stroke-width: 2; stroke-linecap: round; stroke-linejoin: round; }
.q-row:hover .q-chev { color: var(--navy); }

/* Back link — plain text control */
.back-link {
  align-self: flex-start;
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
  margin-bottom: 0.85rem;
  padding: 0.15rem 0;
  border: none;
  background: transparent;
  font-family: inherit;
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--navy);
  cursor: pointer;
}
.back-link:hover { text-decoration: underline; }
.back-link svg { width: 16px; height: 16px; fill: none; stroke: currentColor; stroke-width: 2; stroke-linecap: round; stroke-linejoin: round; }

/* Answer — clean bordered card, no shadow/gloss */
.answer {
  border: 1px solid #e5e7eb;
  border-radius: 10px;
  padding: 1rem 1.05rem;
  background: #fff;
}
.answer-q {
  margin: 0 0 0.6rem;
  font-size: 0.98rem;
  font-weight: 700;
  line-height: 1.4;
  color: var(--navy);
}
.answer-a {
  margin: 0;
  padding-top: 0.6rem;
  border-top: 1px solid #eef1f5;
  font-size: 0.92rem;
  line-height: 1.7;
  color: #374151;
  white-space: pre-line;
}

/* RTL: flip the directional chevrons */
.cbp[dir='rtl'] .q-chev svg,
.cbp[dir='rtl'] .back-link svg { transform: scaleX(-1); }

/* Panel enter/leave (gentle slide up + fade) */
.cbp-enter-active, .cbp-leave-active { transition: opacity 0.2s ease, transform 0.2s ease; }
.cbp-enter-from, .cbp-leave-to { opacity: 0; transform: translateY(12px); }

/* View swap (list <-> answer) */
.view-enter-active, .view-leave-active { transition: opacity 0.15s ease; }
.view-enter-from, .view-leave-to { opacity: 0; }

@media (max-width: 480px) {
  .cbp {
    inset-inline: 0.75rem;
    bottom: 0.75rem;
    width: auto;
    max-height: calc(100vh - 1.5rem);
  }
}
@media (prefers-reduced-motion: reduce) {
  .cbp-enter-from, .cbp-leave-to { transform: none; }
}
</style>
