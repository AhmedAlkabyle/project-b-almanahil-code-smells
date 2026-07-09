<script setup>
// Reusable confirm dialog used across the admin sections (delete / deactivate…).
// All labels are passed in by the caller so each screen supplies its own i18n text.
import { useLanguageStore } from '../../stores/language'

defineProps({
  open: { type: Boolean, default: false },
  title: { type: String, default: '' },
  message: { type: String, default: '' },
  confirmLabel: { type: String, default: 'Confirm' },
  cancelLabel: { type: String, default: 'Cancel' },
  // Visual tone of the confirm button + icon: 'danger' | 'success' | 'warn'
  variant: { type: String, default: 'danger' },
  // While true, the dialog is working (e.g. calling the API): buttons are disabled
  // and the backdrop click can't dismiss it.
  busy: { type: Boolean, default: false }
})
const emit = defineEmits(['confirm', 'cancel'])

const language = useLanguageStore()
</script>

<template>
  <Teleport to="body">
    <Transition name="cd">
      <div v-if="open" class="cd-overlay" :dir="language.dir" @click.self="!busy && emit('cancel')">
        <div class="cd" role="alertdialog" aria-modal="true">
          <span class="cd-icon" :class="variant">
            <svg viewBox="0 0 24 24"><path d="M12 9v4m0 4h.01M10.3 3.9 1.8 18a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0Z" /></svg>
          </span>
          <h3>{{ title }}</h3>
          <p v-if="message">{{ message }}</p>
          <div class="cd-foot">
            <button type="button" class="cd-btn ghost" :disabled="busy" @click="emit('cancel')">{{ cancelLabel }}</button>
            <button type="button" class="cd-btn" :class="variant" :disabled="busy" @click="emit('confirm')">{{ confirmLabel }}</button>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
.cd-overlay {
  position: fixed;
  inset: 0;
  z-index: 110;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1.5rem;
  background: rgba(15, 23, 42, 0.5);
  backdrop-filter: blur(3px);
  font-family: 'Segoe UI', system-ui, -apple-system, sans-serif;
}
.cd-overlay[dir='rtl'] {
  font-family: 'Segoe UI', 'Tahoma', system-ui, sans-serif;
}
.cd {
  width: 100%;
  max-width: 400px;
  padding: 1.75rem;
  text-align: center;
  background: #fff;
  border-radius: 20px;
  box-shadow: 0 30px 70px rgba(15, 23, 42, 0.35);
}
.cd-icon {
  width: 58px;
  height: 58px;
  border-radius: 50%;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 0.85rem;
}
.cd-icon.danger {
  color: #dc2626;
  background: #fee2e2;
}
.cd-icon.warn {
  color: #b45309;
  background: #fef3c7;
}
.cd-icon.success {
  color: #15803d;
  background: #dcfce7;
}
.cd-icon svg {
  width: 28px;
  height: 28px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.8;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.cd h3 {
  margin: 0 0 0.4rem;
  font-size: 1.2rem;
  font-weight: 800;
  color: #0f2444;
}
.cd p {
  margin: 0 0 1.4rem;
  font-size: 0.9rem;
  color: #6b7280;
  line-height: 1.5;
}
.cd-foot {
  display: flex;
  justify-content: center;
  gap: 0.6rem;
}
.cd-btn {
  padding: 0.7rem 1.4rem;
  border: none;
  border-radius: 11px;
  font-size: 0.9rem;
  font-weight: 700;
  font-family: inherit;
  cursor: pointer;
  transition: transform 0.15s ease, background 0.2s ease, box-shadow 0.2s ease;
}
.cd-btn.ghost {
  background: #f1f4fb;
  color: #475569;
}
.cd-btn.ghost:hover {
  background: #e7edf7;
}
.cd-btn.danger {
  color: #fff;
  background: linear-gradient(135deg, #dc2626, #ef4444);
  box-shadow: 0 8px 18px rgba(220, 38, 38, 0.3);
}
.cd-btn.warn {
  color: #fff;
  background: linear-gradient(135deg, #d97706, #f59e0b);
  box-shadow: 0 8px 18px rgba(217, 119, 6, 0.3);
}
.cd-btn.success {
  color: #fff;
  background: linear-gradient(135deg, #15803d, #16a34a);
  box-shadow: 0 8px 18px rgba(22, 163, 74, 0.3);
}
.cd-btn.danger:hover,
.cd-btn.warn:hover,
.cd-btn.success:hover {
  transform: translateY(-1px);
}

/* Transition */
.cd-enter-active,
.cd-leave-active {
  transition: opacity 0.22s ease;
}
.cd-enter-active .cd,
.cd-leave-active .cd {
  transition: transform 0.22s ease, opacity 0.22s ease;
}
.cd-enter-from,
.cd-leave-to {
  opacity: 0;
}
.cd-enter-from .cd,
.cd-leave-to .cd {
  transform: translateY(16px) scale(0.97);
  opacity: 0;
}
</style>
