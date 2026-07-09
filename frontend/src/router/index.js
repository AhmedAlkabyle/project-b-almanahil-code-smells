// ─── WHAT THIS FILE DOES (in plain words) ───
// This is the "map" of the whole website. It says which web address (URL) shows
// which page. It also has a "security guard" (below) that checks: are you logged
// in? Are you allowed to see this page for your role (Admin/Teacher/Student/Parent)?
// If not, it quietly sends you somewhere you ARE allowed to be.
import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { dashboardFor } from './roleRoutes'

import WelcomeView from '../views/Shared/WelcomeView.vue'
import LoginView from '../views/Shared/LoginView.vue'
import ForgotPasswordView from '../views/Shared/ForgotPasswordView.vue'
import RequestAccountView from '../views/Shared/RequestAccountView.vue'
import AdminLayout from '../layouts/AdminLayout.vue'
import TeacherLayout from '../layouts/TeacherLayout.vue'
import AdminDashboard from '../views/Admin/AdminDashboard.vue'
import AdminProfile from '../views/Admin/AdminProfile.vue'
import ManageUsers from '../views/Admin/ManageUsers.vue'
import ManageClasses from '../views/Admin/ManageClasses.vue'
import ManageSubjects from '../views/Admin/ManageSubjects.vue'
import TeacherAssignments from '../views/Admin/TeacherAssignments.vue'
import ManageEvents from '../views/Admin/ManageEvents.vue'
import ViewGrades from '../views/Admin/ViewGrades.vue'
import AttendanceReports from '../views/Admin/AttendanceReports.vue'
import TeacherDashboard from '../views/Teacher/TeacherDashboard.vue'
import TeacherProfile from '../views/Teacher/TeacherProfile.vue'
import TeacherAttendance from '../views/Teacher/TeacherAttendance.vue'
import TeacherGrades from '../views/Teacher/TeacherGrades.vue'
import TeacherEvents from '../views/Teacher/TeacherEvents.vue'
import TeacherMaterials from '../views/Teacher/TeacherMaterials.vue'
import TeacherSubjects from '../views/Teacher/TeacherSubjects.vue'
import StudentLayout from '../layouts/StudentLayout.vue'
import StudentDashboard from '../views/Student/StudentDashboard.vue'
import StudentAttendance from '../views/Student/StudentAttendance.vue'
import StudentGrades from '../views/Student/StudentGrades.vue'
import StudentEvents from '../views/Student/StudentEvents.vue'
import StudentMaterials from '../views/Student/StudentMaterials.vue'
import StudentProfile from '../views/Student/StudentProfile.vue'
import ParentLayout from '../layouts/ParentLayout.vue'
import ParentDashboard from '../views/Parent/ParentDashboard.vue'
import ParentChildAttendance from '../views/Parent/ParentChildAttendance.vue'
import ParentChildGrades from '../views/Parent/ParentChildGrades.vue'
import ParentEvents from '../views/Parent/ParentEvents.vue'
import ParentProfile from '../views/Parent/ParentProfile.vue'
import ParentChildMaterials from '../views/Parent/ParentChildMaterials.vue'

// Bilingual title/subtitle shown in the admin top bar (read by AdminLayout).
const adminMeta = (en, ar, enSub = '', arSub = '') => ({
  title: { en, ar },
  subtitle: { en: enSub, ar: arSub }
})

// Same shape for teacher pages (read by TeacherPageHeader). Kept as its own
// helper for clarity, though structurally identical to adminMeta.
const teacherMeta = (en, ar, enSub = '', arSub = '') => ({
  title: { en, ar },
  subtitle: { en: enSub, ar: arSub }
})

// Same shape for student pages (read by StudentPageHeader). Structurally identical
// to adminMeta/teacherMeta — kept separate for clarity.
const studentMeta = (en, ar, enSub = '', arSub = '') => ({
  title: { en, ar },
  subtitle: { en: enSub, ar: arSub }
})

// Same shape for parent pages (read by ParentPageHeader). Structurally identical to the
// others — kept separate for clarity.
const parentMeta = (en, ar, enSub = '', arSub = '') => ({
  title: { en, ar },
  subtitle: { en: enSub, ar: arSub }
})

const routes = [
  // Root redirects to the welcome (landing) page.
  { path: '/', redirect: '/welcome' },

  // Public routes — no authentication required.
  { path: '/welcome', name: 'welcome', component: WelcomeView },
  { path: '/login', name: 'login', component: LoginView },
  { path: '/forgot-password', name: 'forgot-password', component: ForgotPasswordView },
  { path: '/signup', name: 'signup', component: RequestAccountView },

  // Admin area — wrapped in AdminLayout (sidebar + topbar). The Admin guard on
  // the parent is inherited by every child via merged route meta.
  {
    path: '/admin',
    component: AdminLayout,
    meta: { requiresAuth: true, role: 'Admin' },
    children: [
      { path: '', redirect: { name: 'admin-dashboard' } },
      { path: 'dashboard', name: 'admin-dashboard', component: AdminDashboard, meta: adminMeta('Dashboard Overview', 'نظرة عامة على لوحة التحكم', 'School performance at a glance', 'أداء المدرسة في لمحة') },
      { path: 'profile', name: 'admin-profile', component: AdminProfile, meta: adminMeta('My Profile', 'ملفي الشخصي', 'Manage your account and password', 'إدارة حسابك وكلمة المرور') },
      // Placeholder sections (built in later modules)
      { path: 'users', name: 'admin-users', component: ManageUsers, meta: adminMeta('Manage Users', 'إدارة المستخدمين', 'Add, edit, and remove users', 'إضافة وتعديل وحذف المستخدمين') },
      { path: 'classes', name: 'admin-classes', component: ManageClasses, meta: adminMeta('Manage Classes', 'إدارة الصفوف', 'Create and manage school classes', 'إنشاء وإدارة صفوف المدرسة') },
      { path: 'subjects', name: 'admin-subjects', component: ManageSubjects, meta: adminMeta('Manage Subjects', 'إدارة المواد', 'Create and manage subjects', 'إنشاء وإدارة المواد الدراسية') },
      { path: 'teacher-assignments', name: 'admin-assignments', component: TeacherAssignments, meta: adminMeta('Teacher Assignments', 'تعيينات المعلمين', 'Assign subjects to teachers', 'إسناد المواد إلى المعلمين') },
      { path: 'events', name: 'admin-events', component: ManageEvents, meta: adminMeta('Manage Events', 'إدارة الفعاليات', 'Publish announcements and events', 'نشر الإعلانات والفعاليات') },
      { path: 'grades', name: 'admin-grades', component: ViewGrades, meta: adminMeta('View Grades', 'عرض الدرجات', 'Student grade records', 'سجلات درجات الطلاب') },
      { path: 'attendance', name: 'admin-attendance', component: AttendanceReports, meta: adminMeta('Attendance Reports', 'تقارير الحضور', 'Daily attendance reports', 'تقارير الحضور اليومية') }
    ]
  },
  // Teacher area — wrapped in TeacherLayout (green sidebar + topbar), mirroring
  // the admin structure. The Teacher guard on the parent is inherited by every
  // child via merged route meta. All feature pages are placeholders for now.
  {
    path: '/teacher',
    component: TeacherLayout,
    meta: { requiresAuth: true, role: 'Teacher' },
    children: [
      { path: '', redirect: { name: 'teacher-dashboard' } },
      { path: 'dashboard', name: 'teacher-dashboard', component: TeacherDashboard, meta: teacherMeta('Dashboard', 'لوحة التحكم', 'Your teaching overview', 'نظرة عامة على التدريس') },
      { path: 'subjects', name: 'teacher-subjects', component: TeacherSubjects, meta: teacherMeta('My Subjects', 'موادي', 'Subjects you teach', 'المواد التي تدرّسها') },
      { path: 'attendance', name: 'teacher-attendance', component: TeacherAttendance, meta: teacherMeta('Attendance', 'الحضور', 'Record and review attendance', 'تسجيل ومراجعة الحضور') },
      { path: 'grades', name: 'teacher-grades', component: TeacherGrades, meta: teacherMeta('Grades', 'الدرجات', 'Enter and manage grades', 'إدخال وإدارة الدرجات') },
      { path: 'materials', name: 'teacher-materials', component: TeacherMaterials, meta: teacherMeta('Learning Materials', 'المواد التعليمية', 'Share resources with students', 'مشاركة الموارد مع الطلاب') },
      { path: 'events', name: 'teacher-events', component: TeacherEvents, meta: teacherMeta('Events & Announcements', 'الفعاليات والإعلانات', 'School events and announcements', 'فعاليات وإعلانات المدرسة') },
      { path: 'profile', name: 'teacher-profile', component: TeacherProfile, meta: teacherMeta('My Profile', 'ملفي الشخصي', 'View your info and change your password', 'عرض معلوماتك وتغيير كلمة المرور') }
    ]
  },
  // Student area — wrapped in StudentLayout (orange sidebar + topbar), mirroring the
  // admin/teacher structure. The Student guard on the parent is inherited by every
  // child via merged route meta.
  {
    path: '/student',
    component: StudentLayout,
    meta: { requiresAuth: true, role: 'Student' },
    children: [
      { path: '', redirect: { name: 'student-dashboard' } },
      { path: 'dashboard', name: 'student-dashboard', component: StudentDashboard, meta: studentMeta('Dashboard', 'لوحة التحكم', 'Your learning overview', 'نظرة عامة على تعلّمك') },
      { path: 'attendance', name: 'student-attendance', component: StudentAttendance, meta: studentMeta('My Attendance', 'حضوري', 'Your attendance records', 'سجلات حضورك') },
      { path: 'grades', name: 'student-grades', component: StudentGrades, meta: studentMeta('My Grades', 'درجاتي', 'Your grades across subjects', 'درجاتك في جميع المواد') },
      { path: 'materials', name: 'student-materials', component: StudentMaterials, meta: studentMeta('Learning Materials', 'المواد التعليمية', 'Resources shared by your teachers', 'موارد يشاركها معلموك') },
      { path: 'events', name: 'student-events', component: StudentEvents, meta: studentMeta('Events & Announcements', 'الفعاليات والإعلانات', 'School events and announcements', 'فعاليات وإعلانات المدرسة') },
      { path: 'profile', name: 'student-profile', component: StudentProfile, meta: studentMeta('My Profile', 'ملفي الشخصي', 'View your info and change your password', 'عرض معلوماتك وتغيير كلمة المرور') }
    ]
  },
  // Parent area — wrapped in ParentLayout (slate sidebar + topbar), mirroring the
  // student structure. The Parent guard on the parent record is inherited by every
  // child via merged route meta.
  {
    path: '/parent',
    component: ParentLayout,
    meta: { requiresAuth: true, role: 'Parent' },
    children: [
      { path: '', redirect: { name: 'parent-dashboard' } },
      { path: 'dashboard', name: 'parent-dashboard', component: ParentDashboard, meta: parentMeta('Dashboard', 'لوحة التحكم', "Your child's overview", 'نظرة عامة على ابنك') },
      { path: 'attendance', name: 'parent-attendance', component: ParentChildAttendance, meta: parentMeta("Child's Attendance", 'حضور الطالب', "Your child's attendance records", 'سجلات حضور ابنك') },
      { path: 'grades', name: 'parent-grades', component: ParentChildGrades, meta: parentMeta("Child's Grades", 'درجات الطالب', "Your child's grades across subjects", 'درجات ابنك في جميع المواد') },
      { path: 'materials', name: 'parent-materials', component: ParentChildMaterials, meta: parentMeta('Learning Materials', 'المواد التعليمية', 'Resources shared by teachers', 'موارد يشاركها المعلمون') },
      { path: 'events', name: 'parent-events', component: ParentEvents, meta: parentMeta('Events & Announcements', 'الفعاليات والإعلانات', 'School events and announcements', 'فعاليات وإعلانات المدرسة') },
      { path: 'profile', name: 'parent-profile', component: ParentProfile, meta: parentMeta('My Profile', 'ملفي الشخصي', 'View your info and change your password', 'عرض معلوماتك وتغيير كلمة المرور') }
    ]
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

/*
 * ───────────────────────── ROLE FORMAT ─────────────────────────
 * The backend's LoginResponse.Role (and the JWT role claim) is the enum NAME as
 * a PascalCase string: "Admin" | "Teacher" | "Student" | "Parent".
 * The auth store persists that string verbatim at user.role, so route meta.role
 * uses the same casing and we compare with a strict === (no number→string map
 * is needed because the role is already a string, not an enum number).
 * ────────────────────────────────────────────────────────────────
 */

// localStorage keys — mirror the constants in stores/auth.js. Used only as a
// fallback so the guard still works if the Pinia store isn't hydrated yet.
const TOKEN_KEY = 'almanahil_token'
const USER_KEY = 'almanahil_user'

// Reads the persisted role directly from localStorage (fallback for hard reloads).
function storedRole() {
  try {
    const raw = localStorage.getItem(USER_KEY)
    return raw ? JSON.parse(raw)?.role ?? null : null
  } catch {
    return null
  }
}

// This "guard" runs automatically EVERY time the user tries to open a new page.
// It decides whether to let them in, or to redirect them somewhere else first.
// Global navigation guard: enforces auth + per-role access on every route change.
router.beforeEach((to) => {
  const auth = useAuthStore()

  // Prefer the Pinia store; fall back to localStorage so guards survive a hard
  // refresh even before the store has hydrated.
  const token = auth.token || localStorage.getItem(TOKEN_KEY)
  const role = auth.userRole || storedRole()
  const isAuthenticated = !!token

  // Step 1: This page needs a login, but the user is NOT logged in.
  //         → Send them to the login page.
  // 1) Protected route, but the user is NOT logged in → send to login.
  if (to.meta.requiresAuth && !isAuthenticated) {
    return '/login'
  }

  // Step 2: The user IS logged in, but this page is for a different role
  //         (e.g. a Student trying to open an Admin page).
  //         → Send them back to their OWN dashboard instead of the login page.
  // 2) Protected route, but the user's role does NOT match the route's required
  //    role → bounce them to THEIR OWN dashboard (not to login).
  if (to.meta.requiresAuth && to.meta.role && role !== to.meta.role) {
    return dashboardFor(role)
  }

  // Step 3: The user is ALREADY logged in but is trying to open the login or
  //         welcome page again (which is pointless).
  //         → Send them straight to their own dashboard.
  // 3) Already logged in and trying to view a public auth page (/login, /welcome)
  //    → redirect to their own dashboard instead.
  if (isAuthenticated && (to.path === '/login' || to.path === '/welcome')) {
    return dashboardFor(role)
  }

  // Otherwise allow navigation.
  return true
})

export default router
