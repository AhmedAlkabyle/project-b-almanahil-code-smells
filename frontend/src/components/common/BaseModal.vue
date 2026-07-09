<script setup>
// Reusable form-modal shell (overlay + card + header + body/footer slots).
// Matches the Manage Users modal style so every admin form looks identical.
import { useLanguageStore } from '../../stores/language'

defineProps({
  open: { type: Boolean, default: false },
  title: { type: String, default: '' },
  subtitle: { type: String, default: '' },
  // 'default' (480px) | 'wide' (640px) | 'xl' (960px) — xl fits wide grids (e.g. the timetable).
  size: { type: String, default: 'default' }
})
const emit = defineEmits(['close'])

const language = useLanguageStore()
</script>

<template>
  <Teleport to="body">
    <Transition name="bm">
      <div v-if="open" class="bm-overlay" :dir="language.dir" @click.self="emit('close')">
        <div class="bm" :class="`bm-${size}`" role="dialog" aria-modal="true">
          <!-- Header -->
          <div class="bm-head">
            <div>
              <h3>{{ title }}</h3>
              <p v-if="subtitle">{{ subtitle }}</p>
            </div>
            <button type="button" class="bm-close" @click="emit('close')" aria-label="Close">
              <svg viewBox="0 0 24 24"><path d="M18 6 6 18M6 6l12 12" /></svg>
            </button>
          </div>

          <!-- Body (form fields) -->
          <div class="bm-body">
            <slot />
          </div>

          <!-- Footer (action buttons) -->
          <div class="bm-foot">
            <slot name="footer" />
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
.bm-overlay {
  position: fixed;
  inset: 0;
  z-index: 100;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1.5rem;
  background: rgba(15, 23, 42, 0.5);
  backdrop-filter: blur(3px);
  font-family: 'Segoe UI', system-ui, -apple-system, sans-serif;
}
.bm-overlay[dir='rtl'] {
  font-family: 'Segoe UI', 'Tahoma', system-ui, sans-serif;
}
.bm {
  --navy: var(--ds-navy);
  width: 100%;
  max-width: 480px;
  max-height: 90vh;
  overflow-y: auto;
  background: #fff;
  border-radius: 20px;
  box-shadow: 0 30px 70px rgba(15, 23, 42, 0.35);
}
/* Wider variant for multi-section forms (e.g. Add/Edit User) */
.bm-wide {
  max-width: 640px;
}
/* Extra-wide variant for grids that need the room (e.g. the class timetable). */
.bm-xl {
  max-width: 960px;
}
.bm-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
  padding: 1.5rem 1.6rem 1rem;
}
.bm-head h3 {
  margin: 0;
  font-size: 1.3rem;
  font-weight: 800;
  color: #0f2444;
}
.bm-head p {
  margin: 0.2rem 0 0;
  font-size: 0.85rem;
  color: #6b7280;
}
.bm-close {
  flex-shrink: 0;
  width: 34px;
  height: 34px;
  border-radius: 9px;
  border: none;
  background: #f1f4fb;
  color: #64748b;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  transition: background 0.15s ease, color 0.15s ease;
}
.bm-close:hover {
  background: #fee2e2;
  color: #dc2626;
}
.bm-close svg {
  width: 18px;
  height: 18px;
  fill: none;
  stroke: currentColor;
  stroke-width: 2;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.bm-body {
  padding: 0.5rem 1.6rem 0.5rem;
  display: flex;
  flex-direction: column;
  gap: 1rem;
}
.bm-foot {
  display: flex;
  justify-content: flex-end;
  gap: 0.6rem;
  padding: 1rem 1.6rem 1.6rem;
}

/* Transition */
.bm-enter-active,
.bm-leave-active {
  transition: opacity 0.22s ease;
}
.bm-enter-active .bm,
.bm-leave-active .bm {
  transition: transform 0.22s ease, opacity 0.22s ease;
}
.bm-enter-from,
.bm-leave-to {
  opacity: 0;
}
.bm-enter-from .bm,
.bm-leave-to .bm {
  transform: translateY(16px) scale(0.97);
  opacity: 0;
}

/* ---- Shared field styles for slotted form controls ----
   Exposed via :deep so each section's fields look identical without re-declaring. */
.bm-body :deep(.field) {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}
.bm-body :deep(.field label) {
  font-size: 0.85rem;
  font-weight: 700;
  color: #334155;
}
.bm-body :deep(.field input),
.bm-body :deep(.field select),
.bm-body :deep(.field textarea) {
  width: 100%;
  max-width: 100%;
  /* Padding is included in the width so a 100%-wide control never overflows the
     modal (prevents a horizontal scrollbar). */
  box-sizing: border-box;
  padding: 0.8rem 0.95rem;
  font-size: 0.92rem;
  font-family: inherit;
  color: #0f2444;
  /* Match the login form fields exactly */
  background: #f4f7fc;
  border: 1.5px solid #e6ebf4;
  border-radius: 14px;
  outline: none;
  transition: border-color 0.3s ease, box-shadow 0.3s ease, background 0.3s ease;
}
.bm-body :deep(.field textarea) {
  min-height: 96px;
  resize: vertical;
}
.bm-body :deep(.field select) {
  cursor: pointer;
}
.bm-body :deep(.field input:focus),
.bm-body :deep(.field select:focus),
.bm-body :deep(.field textarea:focus) {
  background: #fff;
  border-color: var(--navy);
  /* Same focus glow as the login inputs */
  box-shadow: 0 0 0 4px rgba(30, 76, 154, 0.14);
}
.bm-body :deep(.field input.invalid),
.bm-body :deep(.field select.invalid),
.bm-body :deep(.field textarea.invalid) {
  border-color: #ef4444;
  background: #fef2f2;
}
.bm-body :deep(.field .err) {
  font-size: 0.78rem;
  font-weight: 600;
  color: #dc2626;
}

/* An inline info note (e.g. auto-password hints) inside the body */
.bm-body :deep(.note) {
  display: flex;
  align-items: flex-start;
  gap: 0.6rem;
  padding: 0.8rem 0.9rem;
  border-radius: 12px;
  background: #eef4ff;
  border: 1px solid #d7e3fb;
  color: #1e4c9a;
  font-size: 0.82rem;
  line-height: 1.4;
}

/* ---- Shared footer button styles for slotted buttons ---- */
.bm-foot :deep(.btn) {
  padding: 0.7rem 1.3rem;
  border: none;
  border-radius: 11px;
  font-size: 0.9rem;
  font-weight: 700;
  font-family: inherit;
  cursor: pointer;
  transition: transform 0.15s ease, box-shadow 0.2s ease, background 0.2s ease;
}
.bm-foot :deep(.btn.ghost) {
  background: #f1f4fb;
  color: #475569;
}
.bm-foot :deep(.btn.ghost:hover) {
  background: #e7edf7;
}
.bm-foot :deep(.btn.primary) {
  color: #fff;
  background: linear-gradient(135deg, var(--navy), #2f63ba);
  box-shadow: 0 8px 18px rgba(30, 76, 154, 0.3);
}
.bm-foot :deep(.btn.primary:hover) {
  transform: translateY(-1px);
  box-shadow: 0 12px 24px rgba(30, 76, 154, 0.38);
}
</style>
