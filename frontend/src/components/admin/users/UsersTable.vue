<script setup>
import { computed } from 'vue'
import { useLanguageStore } from '../../../stores/language'

const props = defineProps({
  // The already-filtered list of users to display.
  users: { type: Array, default: () => [] }
})
const emit = defineEmits(['edit', 'toggle', 'delete'])

const language = useLanguageStore()

// ---- Bilingual copy (per-component i18n pattern) ----
const content = {
  en: {
    name: 'Name',
    email: 'Email',
    role: 'Role',
    status: 'Status',
    actions: 'Actions',
    edit: 'Edit',
    activate: 'Activate',
    deactivate: 'Deactivate',
    del: 'Delete',
    active: 'Active',
    inactive: 'Inactive',
    empty: 'No users found',
    emptyHint: 'Try adjusting your search or filters.',
    roles: { Teacher: 'Teacher', Student: 'Student', Parent: 'Parent', Admin: 'Admin' }
  },
  ar: {
    name: 'الاسم',
    email: 'البريد الإلكتروني',
    role: 'الدور',
    status: 'الحالة',
    actions: 'الإجراءات',
    edit: 'تعديل',
    activate: 'تفعيل',
    deactivate: 'إلغاء التفعيل',
    del: 'حذف',
    active: 'نشط',
    inactive: 'غير نشط',
    empty: 'لا يوجد مستخدمون',
    emptyHint: 'حاول تعديل البحث أو عوامل التصفية.',
    roles: { Teacher: 'معلم', Student: 'طالب', Parent: 'ولي أمر', Admin: 'مدير' }
  }
}
const t = computed(() => content[language.lang])

// Colour theme per role (badge = role-*, avatar tint = av-* below).
const roleClass = (role) => `role-${role.toLowerCase()}`
const avatarClass = (role) => `av-${(role || '').toLowerCase()}`

// Up to two initials (first + last name word) for the avatar chip.
const initials = (name) => {
  const parts = (name || '').trim().split(/\s+/).filter(Boolean)
  if (!parts.length) return '?'
  if (parts.length === 1) return parts[0].charAt(0).toUpperCase()
  return (parts[0].charAt(0) + parts[parts.length - 1].charAt(0)).toUpperCase()
}
</script>

<template>
  <div class="table-card">
    <!-- Empty state -->
    <div v-if="!users.length" class="empty">
      <span class="empty-badge">
        <svg viewBox="0 0 24 24"><circle cx="11" cy="11" r="7" /><path d="m21 21-4.3-4.3" /></svg>
      </span>
      <p class="empty-title">{{ t.empty }}</p>
      <p class="empty-hint">{{ t.emptyHint }}</p>
    </div>

    <!-- Table -->
    <table v-else class="users-table">
      <thead>
        <tr>
          <th>{{ t.name }}</th>
          <th>{{ t.email }}</th>
          <th>{{ t.role }}</th>
          <th>{{ t.status }}</th>
          <th class="col-actions">{{ t.actions }}</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="(user, i) in users" :key="user.id" :style="{ '--row-i': i }">
          <td>
            <div class="name-cell">
              <span class="avatar" :class="avatarClass(user.role)">
                <img v-if="user.photo" :src="user.photo" :alt="user.name" />
                <template v-else>{{ initials(user.name) }}</template>
              </span>
              <span class="name-block">
                <span class="name-text">{{ user.name }}</span>
                <span v-if="user.className" class="name-sub">{{ user.className }}</span>
              </span>
            </div>
          </td>
          <td class="email-cell">{{ user.email }}</td>
          <td>
            <span class="badge" :class="roleClass(user.role)">{{ t.roles[user.role] }}</span>
          </td>
          <td>
            <span class="status" :class="user.status === 'Active' ? 'is-active' : 'is-inactive'">
              <span class="status-dot"></span>
              {{ user.status === 'Active' ? t.active : t.inactive }}
            </span>
          </td>
          <td class="col-actions">
            <div class="actions">
              <button type="button" class="act edit" @click="emit('edit', user)">
                <svg viewBox="0 0 24 24"><path d="M12 20h9" /><path d="M16.5 3.5a2.12 2.12 0 0 1 3 3L7 19l-4 1 1-4Z" /></svg>
                {{ t.edit }}
              </button>
              <button
                type="button"
                class="act"
                :class="user.status === 'Active' ? 'deactivate' : 'activate'"
                @click="emit('toggle', user)"
              >
                <svg v-if="user.status === 'Active'" viewBox="0 0 24 24"><path d="M18.36 6.64a9 9 0 1 1-12.73 0" /><path d="M12 2v10" /></svg>
                <svg v-else viewBox="0 0 24 24"><circle cx="12" cy="12" r="9" /><path d="m8.5 12 2.5 2.5 4.5-5" /></svg>
                {{ user.status === 'Active' ? t.deactivate : t.activate }}
              </button>
              <button type="button" class="act delete" @click="emit('delete', user)">
                <svg viewBox="0 0 24 24"><path d="M3 6h18" /><path d="M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2" /><path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" /></svg>
                {{ t.del }}
              </button>
            </div>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
.table-card {
  --navy: var(--ds-navy);
  background: transparent;
  overflow: hidden;
}

/* Table */
.users-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.92rem;
}
.users-table thead th {
  text-align: start;
  padding: 1rem 1.25rem;
  font-size: 0.74rem;
  font-weight: 700;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: #8a94a6;
  background: #f7f9fc;
  border-bottom: 1px solid #eef1f7;
  white-space: nowrap;
}
.users-table tbody td {
  padding: 0.85rem 1.25rem;
  border-bottom: 1px solid #f1f4f9;
  color: #334155;
  vertical-align: middle;
}
.users-table tbody td:first-child {
  position: relative;
}
/* Warm accent bar at the row's leading edge that grows on hover — echoes the
   welcome page's feature-card accent, and adds a hand-designed touch to the list. */
.users-table tbody td:first-child::before {
  content: '';
  position: absolute;
  inset-inline-start: 0;
  top: 8px;
  bottom: 8px;
  width: 3px;
  border-radius: 3px;
  background: linear-gradient(180deg, var(--ds-orange, #f2a03d), #f6b65f);
  transform: scaleY(0);
  transition: transform 0.22s ease;
}
.users-table tbody tr:hover td:first-child::before {
  transform: scaleY(1);
}
.users-table tbody tr:last-child td {
  border-bottom: none;
}
/* Gentle staggered entrance. Rows are keyed by id, so each only animates when it
   first appears — it plays once on load and stays instant while filtering. */
.users-table tbody tr {
  transition: background 0.15s ease;
  animation: user-row-in 0.4s ease both;
  animation-delay: calc(min(var(--row-i, 0), 10) * 42ms);
}
.users-table tbody tr:hover {
  background: #f9fbff;
}
@keyframes user-row-in {
  from {
    opacity: 0;
    transform: translateY(6px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}
@media (prefers-reduced-motion: reduce) {
  .users-table tbody tr {
    animation: none;
  }
}

/* Name cell with avatar */
.name-cell {
  display: flex;
  align-items: center;
  gap: 0.7rem;
}
.avatar {
  width: 42px;
  height: 42px;
  flex-shrink: 0;
  border-radius: 50%;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-weight: 800;
  font-size: 0.92rem;
  letter-spacing: 0.02em;
  color: #fff;
  background: linear-gradient(135deg, #94a3b8, #64748b);
  box-shadow: 0 3px 8px rgba(15, 23, 42, 0.18), inset 0 0 0 2px rgba(255, 255, 255, 0.35);
  overflow: hidden;
}
.avatar img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}
/* Avatar tint per role — matches the role colours used across the page */
.avatar.av-teacher {
  background: linear-gradient(135deg, #22c55e, #15803d);
}
.avatar.av-student {
  background: linear-gradient(135deg, #fbbf24, #d97706);
}
.avatar.av-parent {
  background: linear-gradient(135deg, #94a3b8, #64748b);
}
.avatar.av-admin {
  background: linear-gradient(135deg, #818cf8, #4f46e5);
}
.name-block {
  display: flex;
  flex-direction: column;
  line-height: 1.25;
  min-width: 0;
}
.name-text {
  font-weight: 700;
  color: #0f2444;
}
.name-sub {
  font-size: 0.76rem;
  font-weight: 600;
  color: #94a3b8;
}
.email-cell {
  color: #6b7280;
}

/* Role badges */
.badge {
  display: inline-block;
  padding: 0.3rem 0.7rem;
  border-radius: 999px;
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.02em;
}
.role-teacher {
  color: #15803d;
  background: #dcfce7;
}
.role-student {
  color: #b45309;
  background: #fef3c7;
}
.role-parent {
  color: #475569;
  background: #e5e9f0;
}
.role-admin {
  color: #4f46e5;
  background: #e0e7ff;
}

/* Status */
.status {
  display: inline-flex;
  align-items: center;
  gap: 0.45rem;
  padding: 0.32rem 0.7rem;
  border-radius: 999px;
  font-size: 0.78rem;
  font-weight: 700;
}
.status-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
}
.status.is-active {
  color: #15803d;
  background: #dcfce7;
}
.status.is-active .status-dot {
  background: #22c55e;
  box-shadow: 0 0 6px rgba(34, 197, 94, 0.6);
}
.status.is-inactive {
  color: #64748b;
  background: #eef1f7;
}
.status.is-inactive .status-dot {
  background: #cbd5e1;
}

/* Actions */
.col-actions {
  text-align: end;
}
.actions {
  display: inline-flex;
  gap: 0.5rem;
  justify-content: flex-end;
}
.act {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  padding: 0.45rem 0.8rem;
  border: 1px solid #e2e8f2;
  border-radius: 9px;
  background: #fff;
  font-family: inherit;
  font-size: 0.82rem;
  font-weight: 700;
  color: #475569;
  cursor: pointer;
  transition: background 0.15s ease, color 0.15s ease, border-color 0.15s ease;
}
.act svg {
  width: 15px;
  height: 15px;
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
.act.deactivate:hover {
  color: #dc2626;
  border-color: #f6c9c9;
  background: #fef2f2;
}
.act.activate {
  color: #15803d;
  border-color: #bbf7d0;
  background: #f0fdf4;
}
.act.activate:hover {
  background: #dcfce7;
}
.act.delete {
  color: #dc2626;
}
.act.delete:hover {
  color: #b91c1c;
  border-color: #f6c9c9;
  background: #fef2f2;
}

/* Empty state */
.empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  gap: 0.35rem;
  padding: 3.5rem 1rem;
}
.empty-badge {
  width: 64px;
  height: 64px;
  border-radius: 50%;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 0.5rem;
  /* Warm, friendly badge (welcome-page orange) so an empty list feels inviting. */
  background: radial-gradient(circle, #fff4e6, #ffe7c9);
  color: #eb9a34;
  box-shadow: 0 8px 18px rgba(242, 160, 61, 0.18);
}
.empty-badge svg {
  width: 28px;
  height: 28px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.8;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.empty-title {
  margin: 0;
  font-size: 1.05rem;
  font-weight: 800;
  color: #0f2444;
}
.empty-hint {
  margin: 0;
  font-size: 0.88rem;
  color: #8a94a6;
}

/* Responsive: let the table scroll horizontally on small screens */
@media (max-width: 720px) {
  .table-card {
    overflow-x: auto;
  }
  .users-table {
    min-width: 720px;
  }
}
</style>
