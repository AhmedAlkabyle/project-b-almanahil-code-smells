// ─── WHAT THIS HELPER IS FOR (in plain words) ───
// An event/announcement is aimed at ONE group of people (all teachers, one class...).
// This file lists those groups, gives each a nice name in English and Arabic, and
// picks a colour for its little badge, so the whole app labels them the same way.
// Shared audience metadata for events/announcements — kept in one place so the form
// dropdown and the list badges never drift apart. The keys MUST match the backend's
// AllowedAudiences (EventsController): an event targets exactly ONE of these.

/** The nine audiences, in the order they appear in the form dropdown. */
export const AUDIENCE_TYPES = [
  'AllUsers',
  'AllTeachers',
  'TeachersSecondary',
  'TeachersHighSchool',
  'AllParents',
  'AllStudents',
  'AllSecondary',
  'AllHighSchool',
  'SpecificClass'
]

const LABELS = {
  en: {
    AllUsers: 'All Users',
    AllTeachers: 'All Teachers',
    TeachersSecondary: 'Teachers — Secondary',
    TeachersHighSchool: 'Teachers — High School',
    AllParents: 'All Parents',
    AllStudents: 'All Students',
    AllSecondary: 'All Secondary',
    AllHighSchool: 'All High School',
    SpecificClass: 'A Specific Class'
  },
  ar: {
    AllUsers: 'جميع المستخدمين',
    AllTeachers: 'جميع المعلمين',
    TeachersSecondary: 'المعلمون — الإعدادي',
    TeachersHighSchool: 'المعلمون — الثانوي',
    AllParents: 'جميع أولياء الأمور',
    AllStudents: 'جميع الطلاب',
    AllSecondary: 'كل الإعدادي',
    AllHighSchool: 'كل الثانوي',
    SpecificClass: 'صف محدد'
  }
}

/** Localized label for an audience key (falls back to the raw key). */
export function audienceLabel(type, lang = 'en') {
  return LABELS[lang]?.[type] ?? LABELS.en[type] ?? type
}

/**
 * The text shown on a list badge. For SpecificClass it becomes "Class 1/A" (EN) /
 * "صف 1/A" (AR) using the resolved target class name; otherwise the audience label.
 */
export function audienceBadge(ev, lang = 'en') {
  if (ev?.audienceType === 'SpecificClass') {
    const name = ev.targetClassName
    if (name) return lang === 'ar' ? `صف ${name}` : `Class ${name}`
    return audienceLabel('SpecificClass', lang) // class was removed → generic label
  }
  return audienceLabel(ev?.audienceType, lang)
}

/**
 * A colour "tone" key for the badge, grouped by who it targets, so the list is easy to
 * scan (teachers=green, parents=slate, students=orange, levels=emerald/indigo,
 * a class=navy, everyone=blue). Consumed by CSS classes like `.aud.<tone>`.
 */
export function audienceTone(type) {
  switch (type) {
    case 'AllTeachers':
    case 'TeachersSecondary':
    case 'TeachersHighSchool':
      return 'green'
    case 'AllParents':
      return 'slate'
    case 'AllStudents':
      return 'orange'
    case 'AllSecondary':
      return 'emerald'
    case 'AllHighSchool':
      return 'indigo'
    case 'SpecificClass':
      return 'navy'
    case 'AllUsers':
    default:
      return 'blue'
  }
}
