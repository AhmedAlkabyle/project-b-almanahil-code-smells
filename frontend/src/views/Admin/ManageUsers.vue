<script setup>
import { computed, onMounted, ref } from 'vue'
import { useLanguageStore } from '../../stores/language'
import AdminPageHeader from '../../components/admin/AdminPageHeader.vue'
import StatStrip from '../../components/admin/StatStrip.vue'
import { icons } from '../../components/admin/icons'
import UsersTable from '../../components/admin/users/UsersTable.vue'
import UserFormModal from '../../components/admin/users/UserFormModal.vue'
import ConfirmDialog from '../../components/common/ConfirmDialog.vue'
import { listUsers, setUserStatus, deleteUser } from '../../api/users'
import { listClasses } from '../../api/classes'

const language = useLanguageStore()

// ---- Bilingual copy (per-component i18n pattern) ----
const content = {
  en: {
    searchPh: 'Search by name or email…',
    allRoles: 'All roles',
    addUser: 'Add User',
    statTotal: 'Total Users',
    statTeachers: 'Teachers',
    statStudents: 'Students',
    statParents: 'Parents',
    statAdmins: 'Admins',
    loading: 'Loading users…',
    loadError: 'We couldn’t load the users. Please try again.',
    retry: 'Retry',
    tempPassLabel: 'Temporary password',
    copy: 'Copy',
    copied: 'Copied!',
    confirmTitleOff: 'Deactivate user?',
    confirmTitleOn: 'Activate user?',
    confirmMsgOff: 'This user will lose access until reactivated. No data is deleted.',
    confirmMsgOn: 'This user will regain access to the system.',
    confirmYesOff: 'Deactivate',
    confirmYesOn: 'Activate',
    deleteTitle: 'Delete this account?',
    deleteMsg: 'This permanently removes the user and all of their links (parent–student connections, teaching assignments, and any events they created). This can’t be undone — deactivate instead if you’re unsure.',
    deleteYes: 'Delete',
    working: 'Please wait…',
    cancel: 'Cancel',
    roles: { Teacher: 'Teacher', Student: 'Student', Parent: 'Parent', Admin: 'Admin' }
  },
  ar: {
    searchPh: 'ابحث بالاسم أو البريد الإلكتروني…',
    allRoles: 'كل الأدوار',
    addUser: 'إضافة مستخدم',
    statTotal: 'إجمالي المستخدمين',
    statTeachers: 'المعلمون',
    statStudents: 'الطلاب',
    statParents: 'أولياء الأمور',
    statAdmins: 'المدراء',
    loading: 'جارٍ تحميل المستخدمين…',
    loadError: 'تعذّر تحميل المستخدمين. يرجى المحاولة مرة أخرى.',
    retry: 'إعادة المحاولة',
    tempPassLabel: 'كلمة المرور المؤقتة',
    copy: 'نسخ',
    copied: 'تم النسخ!',
    confirmTitleOff: 'إلغاء تفعيل المستخدم؟',
    confirmTitleOn: 'تفعيل المستخدم؟',
    confirmMsgOff: 'سيفقد هذا المستخدم الوصول حتى إعادة التفعيل. لن يتم حذف أي بيانات.',
    confirmMsgOn: 'سيستعيد هذا المستخدم الوصول إلى النظام.',
    confirmYesOff: 'إلغاء التفعيل',
    confirmYesOn: 'تفعيل',
    deleteTitle: 'حذف هذا الحساب؟',
    deleteMsg: 'سيتم حذف المستخدم نهائيًا مع جميع روابطه (روابط ولي الأمر بالطالب، وتعيينات التدريس، والفعاليات التي أنشأها). لا يمكن التراجع عن هذا — يمكنك إلغاء التفعيل بدلاً من ذلك إن لم تكن متأكدًا.',
    deleteYes: 'حذف',
    working: 'يرجى الانتظار…',
    cancel: 'إلغاء',
    roles: { Teacher: 'معلم', Student: 'طالب', Parent: 'ولي أمر', Admin: 'مدير' }
  }
}
const t = computed(() => content[language.lang])

// Filter tab order mirrors the summary strip above (Student, Parent, Teacher, Admin).
const ROLES = ['Student', 'Parent', 'Teacher', 'Admin']

// ---- State ----
const users = ref([]) // mapped view-model users (see toViewUser)
const classes = ref([])
const loading = ref(true)
const loadError = ref('')

const search = ref('')
const roleFilter = ref('') // '' = all

// Map a backend UserResponseDto → the shape the table + form expect.
function toViewUser(u) {
  return {
    id: u.id,
    firstName: u.firstName ?? '',
    middleName: u.middleName ?? '',
    lastName: u.lastName ?? '',
    name: u.fullName,
    gender: u.gender ?? '',
    dob: u.dateOfBirth ?? '',
    email: u.email,
    role: u.role,
    status: u.isActive ? 'Active' : 'Inactive',
    classId: u.classId ?? '',
    className: u.className ?? '',
    photo: u.photo ?? '',
    // phoneNumber is the parent's phone (Parent) or the guardian contact (Student).
    phone: u.role === 'Parent' ? (u.phoneNumber ?? '') : '',
    contactNumber: u.role === 'Student' ? (u.phoneNumber ?? '') : ''
  }
}

// Load users + classes together (both feed the page and the Add/Edit form).
async function loadAll() {
  loading.value = true
  loadError.value = ''
  try {
    const [usersRes, classesRes] = await Promise.all([listUsers(), listClasses()])
    users.value = (usersRes.data ?? []).map(toViewUser)
    classes.value = classesRes.data ?? []
  } catch (err) {
    loadError.value = err.message || t.value.loadError
  } finally {
    loading.value = false
  }
}
// Refresh only the users list (after a create/edit/status change).
async function reloadUsers() {
  try {
    const { data } = await listUsers()
    users.value = (data ?? []).map(toViewUser)
  } catch (err) {
    flash('error', err.message)
  }
}
onMounted(loadAll)

// A student MUST be linked to an existing parent at creation, so the Add form needs
// the current parent accounts (derived from the loaded users — same as ?role=Parent).
const parents = computed(() => users.value.filter((u) => u.role === 'Parent'))

// ---- Filtering (client-side over the loaded list) ----
// Search + role filter are applied together, live as you type.
const filteredUsers = computed(() => {
  const q = search.value.trim().toLowerCase()
  return users.value.filter((u) => {
    const matchesRole = !roleFilter.value || u.role === roleFilter.value
    // Match first / middle / last / full name OR email (case-insensitive).
    const haystack = [u.firstName, u.middleName, u.lastName, u.name, u.email]
      .filter(Boolean)
      .join(' ')
      .toLowerCase()
    const matchesSearch = !q || haystack.includes(q)
    return matchesRole && matchesSearch
  })
})

// ---- Summary counts (whole dataset, not just the filtered view) ----
const counts = computed(() => {
  const list = users.value
  return {
    total: list.length,
    teachers: list.filter((u) => u.role === 'Teacher').length,
    students: list.filter((u) => u.role === 'Student').length,
    parents: list.filter((u) => u.role === 'Parent').length,
    admins: list.filter((u) => u.role === 'Admin').length
  }
})
const statItems = computed(() => [
  // Order + colours match the dashboard: student amber (yellow), parent grey (slate),
  // teacher green, admin indigo, total users cyan — each its own distinct colour.
  { label: t.value.statStudents, value: counts.value.students, variant: 'amber', icon: icons.student },
  { label: t.value.statParents, value: counts.value.parents, variant: 'slate', icon: icons.user },
  { label: t.value.statTeachers, value: counts.value.teachers, variant: 'green', icon: icons.book },
  { label: t.value.statAdmins, value: counts.value.admins, variant: 'indigo', icon: icons.settings },
  { label: t.value.statTotal, value: counts.value.total, variant: 'cyan', icon: icons.users }
])

// Count shown on each role filter tab (whole dataset, not the filtered view).
function roleCount(role) {
  const map = {
    Teacher: counts.value.teachers,
    Student: counts.value.students,
    Parent: counts.value.parents,
    Admin: counts.value.admins
  }
  return map[role] ?? 0
}

// ---- Flash message (success / error banner, auto-dismisses) ----
const flashMsg = ref(null)
let flashTimer = null
function flash(type, text, password = null) {
  flashMsg.value = { type, text, password }
  clearTimeout(flashTimer)
  // Credential flashes stay until dismissed — the admin needs time to copy the password.
  if (!password) flashTimer = setTimeout(() => (flashMsg.value = null), 4500)
}

// Copy-to-clipboard for the temporary password (main-admin reveal).
const copied = ref(false)
async function copyPassword() {
  const pass = flashMsg.value?.password
  if (!pass) return
  try {
    await navigator.clipboard.writeText(pass)
    copied.value = true
    setTimeout(() => (copied.value = false), 1800)
  } catch {
    // Clipboard API blocked/unavailable — the password is still visible to copy manually.
  }
}

// ---- Add / edit modal ----
const modalOpen = ref(false)
const editingUser = ref(null)

function openAdd() {
  editingUser.value = null
  modalOpen.value = true
}
function openEdit(user) {
  editingUser.value = user
  modalOpen.value = true
}
function closeModal() {
  modalOpen.value = false
  editingUser.value = null
}
// The modal performs the POST/PUT itself and emits 'saved' with the API's message.
function onSaved({ message, temporaryPassword }) {
  closeModal()
  // The main admin gets the temp password back to show + copy; others just get the message.
  flash('success', message, temporaryPassword || null)
  reloadUsers()
}

// ---- Activate / deactivate (soft, with confirm — no hard delete) ----
const confirmOpen = ref(false)
const confirmTarget = ref(null)
const toggling = ref(false)

function askToggle(user) {
  confirmTarget.value = user
  confirmOpen.value = true
}
function closeConfirm() {
  if (toggling.value) return
  confirmOpen.value = false
  confirmTarget.value = null
}
async function confirmToggle() {
  const user = confirmTarget.value
  if (!user) return
  const makeActive = user.status !== 'Active' // Active → deactivate; Inactive → activate
  toggling.value = true
  try {
    const { message } = await setUserStatus(user.id, makeActive)
    user.status = makeActive ? 'Active' : 'Inactive'
    toggling.value = false
    closeConfirm()
    flash('success', message)
  } catch (err) {
    toggling.value = false
    closeConfirm()
    flash('error', err.message)
  }
}

// Whether the confirm dialog is about deactivating (true) or activating (false).
const isDeactivating = computed(() => confirmTarget.value?.status === 'Active')

// ---- Delete account (permanent hard delete, with confirm) ----
const deleteOpen = ref(false)
const deleteTarget = ref(null)
const deleting = ref(false)

function askDelete(user) {
  deleteTarget.value = user
  deleteOpen.value = true
}
function closeDelete() {
  if (deleting.value) return
  deleteOpen.value = false
  deleteTarget.value = null
}
async function confirmDelete() {
  const user = deleteTarget.value
  if (!user) return
  deleting.value = true
  try {
    const { message } = await deleteUser(user.id)
    deleting.value = false
    closeDelete()
    flash('success', message)
    reloadUsers()
  } catch (err) {
    deleting.value = false
    closeDelete()
    flash('error', err.message)
  }
}
</script>

<template>
  <div class="manage-users" :dir="language.dir">
    <AdminPageHeader />

    <!-- Summary counts -->
    <StatStrip :items="statItems" />

    <!-- Flash message (success / error) -->
    <Transition name="flash">
      <div v-if="flashMsg" class="flash" :class="flashMsg.type">
        <svg v-if="flashMsg.type === 'success'" viewBox="0 0 24 24"><path d="M20 6 9 17l-5-5" /></svg>
        <svg v-else viewBox="0 0 24 24"><circle cx="12" cy="12" r="9" /><path d="M12 8v5M12 16h.01" /></svg>
        <div class="flash-body">
          <span>{{ flashMsg.text }}</span>
          <!-- Temporary password (shown only when the API returned one — main admin) -->
          <div v-if="flashMsg.password" class="flash-cred">
            <span class="flash-cred-label">{{ t.tempPassLabel }}:</span>
            <code class="flash-pass">{{ flashMsg.password }}</code>
            <button type="button" class="flash-copy" @click="copyPassword">
              <svg viewBox="0 0 24 24"><rect x="9" y="9" width="11" height="11" rx="2" /><path d="M5 15V5a2 2 0 0 1 2-2h10" /></svg>
              {{ copied ? t.copied : t.copy }}
            </button>
          </div>
        </div>
        <button type="button" class="flash-x" @click="flashMsg = null" aria-label="Dismiss">
          <svg viewBox="0 0 24 24"><path d="M18 6 6 18M6 6l12 12" /></svg>
        </button>
      </div>
    </Transition>

    <!-- Users panel: search, role tabs, and table unified in one card -->
    <div class="panel">
      <!-- Head: search + Add User -->
      <div class="panel-head">
        <label class="search">
          <svg viewBox="0 0 24 24"><circle cx="11" cy="11" r="7" /><path d="m21 21-4.3-4.3" /></svg>
          <input v-model="search" type="text" :placeholder="t.searchPh" />
        </label>

        <button type="button" class="add-btn" @click="openAdd">
          <svg viewBox="0 0 24 24"><path d="M12 5v14M5 12h14" /></svg>
          {{ t.addUser }}
        </button>
      </div>

      <!-- Role filter tabs: click a role to view only those users -->
      <div class="role-tabs" role="tablist" :aria-label="t.allRoles">
        <button
          type="button"
          class="role-tab tab-all"
          :class="{ active: roleFilter === '' }"
          role="tab"
          :aria-selected="roleFilter === ''"
          @click="roleFilter = ''"
        >
          <span class="tab-label">{{ t.allRoles }}</span>
          <span class="tab-count">{{ counts.total }}</span>
        </button>
        <button
          v-for="r in ROLES"
          :key="r"
          type="button"
          class="role-tab"
          :class="[`tab-${r.toLowerCase()}`, { active: roleFilter === r }]"
          role="tab"
          :aria-selected="roleFilter === r"
          @click="roleFilter = r"
        >
          <span class="tab-dot"></span>
          <span class="tab-label">{{ t.roles[r] }}</span>
          <span class="tab-count">{{ roleCount(r) }}</span>
        </button>
      </div>

      <!-- Body: loading / error / users table -->
      <div class="panel-body">
        <div v-if="loading" class="state-block">
          <span class="spinner"></span>
          <p>{{ t.loading }}</p>
        </div>

        <div v-else-if="loadError" class="state-block error-state">
          <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="9" /><path d="M12 8v5M12 16h.01" /></svg>
          <p>{{ loadError }}</p>
          <button type="button" class="retry-btn" @click="loadAll">{{ t.retry }}</button>
        </div>

        <UsersTable v-else :users="filteredUsers" @edit="openEdit" @toggle="askToggle" @delete="askDelete" />
      </div>
    </div>

    <!-- Add / edit modal (performs the API call itself) -->
    <UserFormModal
      :open="modalOpen"
      :user="editingUser"
      :classes="classes"
      :parents="parents"
      @close="closeModal"
      @saved="onSaved"
    />

    <!-- Confirm activate/deactivate -->
    <Teleport to="body">
      <Transition name="modal">
        <div v-if="confirmOpen" class="overlay" :dir="language.dir" @click.self="closeConfirm">
          <div class="confirm" role="alertdialog" aria-modal="true">
            <span class="confirm-icon" :class="isDeactivating ? 'warn' : 'ok'">
              <svg viewBox="0 0 24 24"><path d="M12 9v4m0 4h.01M10.3 3.9 1.8 18a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0Z" /></svg>
            </span>
            <h3>{{ isDeactivating ? t.confirmTitleOff : t.confirmTitleOn }}</h3>
            <p>{{ isDeactivating ? t.confirmMsgOff : t.confirmMsgOn }}</p>
            <div class="confirm-foot">
              <button type="button" class="btn ghost" @click="closeConfirm" :disabled="toggling">{{ t.cancel }}</button>
              <button
                type="button"
                class="btn"
                :class="isDeactivating ? 'danger' : 'success'"
                :disabled="toggling"
                @click="confirmToggle"
              >
                {{ toggling ? t.working : (isDeactivating ? t.confirmYesOff : t.confirmYesOn) }}
              </button>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>

    <!-- Confirm permanent delete -->
    <ConfirmDialog
      :open="deleteOpen"
      :title="t.deleteTitle"
      :message="t.deleteMsg"
      :confirm-label="t.deleteYes"
      :cancel-label="t.cancel"
      variant="danger"
      :busy="deleting"
      @confirm="confirmDelete"
      @cancel="closeDelete"
    />
  </div>
</template>

<style scoped>
.manage-users {
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
.flash-body {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 0.55rem;
}
/* Temporary-password reveal (main admin only) */
.flash-cred {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 0.5rem;
}
.flash-cred-label {
  font-weight: 700;
  font-size: 0.82rem;
}
.flash-pass {
  font-family: 'Consolas', 'Courier New', monospace;
  font-size: 0.95rem;
  font-weight: 700;
  letter-spacing: 0.05em;
  padding: 0.25rem 0.6rem;
  border-radius: 8px;
  background: rgba(21, 128, 61, 0.12);
  border: 1px solid rgba(21, 128, 61, 0.25);
  color: #14532d;
  user-select: all;
}
.flash-copy {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  padding: 0.3rem 0.7rem;
  border: 1px solid rgba(21, 128, 61, 0.3);
  border-radius: 8px;
  background: #fff;
  color: #15803d;
  font-family: inherit;
  font-size: 0.8rem;
  font-weight: 700;
  cursor: pointer;
  transition: background 0.15s ease;
}
.flash-copy:hover {
  background: #f0fdf4;
}
.flash-copy svg {
  width: 14px;
  height: 14px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.9;
  stroke-linecap: round;
  stroke-linejoin: round;
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

/* Loading / error state (flat, inside the panel body) */
.state-block {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 0.7rem;
  padding: 3.5rem 1.5rem;
  color: #6b7280;
  font-size: 0.92rem;
}
.state-block p {
  margin: 0;
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

/* Users panel — search + tabs + table unified in one elevated card */
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
/* Role filter — segmented control (click a role to filter) */
.role-tabs {
  display: flex;
  flex-wrap: wrap;
  gap: 0.45rem;
  padding: 0.7rem 1rem;
  background: #fbfcfe;
  border-bottom: 1px solid #eef1f7;
}
.role-tab {
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
.role-tab:hover {
  background: #f4f7fc;
  color: #334155;
}
.tab-dot {
  width: 9px;
  height: 9px;
  border-radius: 50%;
  flex-shrink: 0;
}
.tab-teacher .tab-dot {
  background: #16a34a;
}
.tab-student .tab-dot {
  background: #d97706;
}
.tab-parent .tab-dot {
  background: #64748b;
}
.tab-admin .tab-dot {
  background: #4f46e5;
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
/* Active tab lights up in its own role colour */
.role-tab.active {
  color: #fff;
}
.role-tab.active .tab-dot {
  background: rgba(255, 255, 255, 0.9);
}
.role-tab.active .tab-count {
  background: rgba(255, 255, 255, 0.22);
  color: #fff;
}
.tab-all.active {
  background: var(--navy);
  box-shadow: 0 6px 14px rgba(30, 76, 154, 0.28);
}
.tab-teacher.active {
  background: #16a34a;
  box-shadow: 0 6px 14px rgba(22, 163, 74, 0.28);
}
.tab-student.active {
  background: #d97706;
  box-shadow: 0 6px 14px rgba(217, 119, 6, 0.28);
}
.tab-parent.active {
  background: #64748b;
  box-shadow: 0 6px 14px rgba(100, 116, 139, 0.28);
}
.tab-admin.active {
  background: #4f46e5;
  box-shadow: 0 6px 14px rgba(79, 70, 229, 0.28);
}

/* Add User button */
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

/* Confirm dialog */
.overlay {
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
.overlay[dir='rtl'] {
  font-family: 'Segoe UI', 'Tahoma', system-ui, sans-serif;
}
.confirm {
  width: 100%;
  max-width: 400px;
  padding: 1.75rem;
  text-align: center;
  background: #fff;
  border-radius: 20px;
  box-shadow: 0 30px 70px rgba(15, 23, 42, 0.35);
}
.confirm-icon {
  width: 58px;
  height: 58px;
  border-radius: 50%;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 0.85rem;
}
.confirm-icon.warn {
  color: #dc2626;
  background: #fee2e2;
}
.confirm-icon.ok {
  color: #15803d;
  background: #dcfce7;
}
.confirm-icon svg {
  width: 28px;
  height: 28px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.8;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.confirm h3 {
  margin: 0 0 0.4rem;
  font-size: 1.2rem;
  font-weight: 800;
  color: #0f2444;
}
.confirm p {
  margin: 0 0 1.4rem;
  font-size: 0.9rem;
  color: #6b7280;
  line-height: 1.5;
}
.confirm-foot {
  display: flex;
  justify-content: center;
  gap: 0.6rem;
}
.btn {
  padding: 0.7rem 1.4rem;
  border: none;
  border-radius: 11px;
  font-size: 0.9rem;
  font-weight: 700;
  font-family: inherit;
  cursor: pointer;
  transition: transform 0.15s ease, background 0.2s ease, box-shadow 0.2s ease;
}
.btn:disabled {
  opacity: 0.65;
  cursor: not-allowed;
}
.btn.ghost {
  background: #f1f4fb;
  color: #475569;
}
.btn.ghost:hover {
  background: #e7edf7;
}
.btn.danger {
  color: #fff;
  background: linear-gradient(135deg, #dc2626, #ef4444);
  box-shadow: 0 8px 18px rgba(220, 38, 38, 0.3);
}
.btn.success {
  color: #fff;
  background: linear-gradient(135deg, #15803d, #16a34a);
  box-shadow: 0 8px 18px rgba(22, 163, 74, 0.3);
}
.btn.danger:not(:disabled):hover,
.btn.success:not(:disabled):hover {
  transform: translateY(-1px);
}

/* Shared modal transition */
.modal-enter-active,
.modal-leave-active {
  transition: opacity 0.22s ease;
}
.modal-enter-active .confirm,
.modal-leave-active .confirm {
  transition: transform 0.22s ease, opacity 0.22s ease;
}
.modal-enter-from,
.modal-leave-to {
  opacity: 0;
}
.modal-enter-from .confirm,
.modal-leave-to .confirm {
  transform: translateY(16px) scale(0.97);
  opacity: 0;
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
}
</style>
