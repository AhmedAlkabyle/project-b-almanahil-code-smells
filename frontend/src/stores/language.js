// ─── WHAT THIS FILE DOES (in plain words) ───
// Remembers which language the user picked (English or Arabic) and which way the
// text should flow (left-to-right for English, right-to-left for Arabic). The
// choice is saved so it stays the same next time the app is opened.
import { defineStore } from 'pinia'

// localStorage key for persisting the language choice.
const LANG_KEY = 'almanahil_lang'

// Load the saved language, falling back to English.
function loadStoredLang() {
  const saved = localStorage.getItem(LANG_KEY)
  return saved === 'ar' || saved === 'en' ? saved : 'en'
}

export const useLanguageStore = defineStore('language', {
  state: () => ({
    // Current UI language: 'en' or 'ar'.
    lang: loadStoredLang()
  }),

  getters: {
    // True when the active language is Arabic.
    isArabic: (state) => state.lang === 'ar',
    // Text direction for the <html>/container: 'rtl' for Arabic, else 'ltr'.
    dir: (state) => (state.lang === 'ar' ? 'rtl' : 'ltr')
  },

  actions: {
    // Switch between English and Arabic.
    toggleLanguage() {
      this.setLanguage(this.lang === 'en' ? 'ar' : 'en')
    },

    // Set a specific language and persist it.
    setLanguage(lang) {
      if (lang !== 'en' && lang !== 'ar') return
      this.lang = lang
      localStorage.setItem(LANG_KEY, lang)
    }
  }
})
