// ─── WHAT THIS HELPER IS FOR (in plain words) ───
// This is the list of ready-made questions and answers for the little help chatbot.
// It is just plain text stored here (no real AI, no server), shown as clickable
// buttons; each question already has both an English and an Arabic version.
// Menu-based chatbot FAQ (Module 6). 100% local — no backend, no external API, no AI.
// Each entry has a bilingual question (q) and answer (a). Rendered as clickable menu
// buttons by components/ChatbotPanel.vue; picking one shows the preset answer.
export const chatbotFaq = [
  {
    id: 'grades',
    q: { en: 'How do I check my grades?', ar: 'كيف أطّلع على درجاتي؟' },
    a: {
      en: 'Go to “My Grades” (students/parents) or “Grades” (teachers) in the sidebar to view or record grades.',
      ar: 'افتح «درجاتي» (للطلاب وأولياء الأمور) أو «الدرجات» (للمعلمين) من القائمة الجانبية لعرض الدرجات أو تسجيلها.'
    }
  },
  {
    id: 'attendance',
    q: { en: 'How do I view attendance?', ar: 'كيف أعرض الحضور؟' },
    a: {
      en: 'Open “My Attendance” / “Attendance” in the sidebar to see or record attendance.',
      ar: 'افتح «حضوري» أو «الحضور» من القائمة الجانبية لعرض الحضور أو تسجيله.'
    }
  },
  {
    id: 'materials',
    q: { en: 'How do I find learning materials?', ar: 'كيف أجد المواد التعليمية؟' },
    a: {
      en: 'Click “Learning Materials” in the sidebar to view PDFs and video links shared by teachers.',
      ar: 'اضغط «المواد التعليمية» من القائمة الجانبية لعرض ملفات PDF وروابط الفيديو التي يشاركها المعلمون.'
    }
  },
  {
    id: 'events',
    q: { en: 'Where are events and announcements?', ar: 'أين الفعاليات والإعلانات؟' },
    a: {
      en: 'Open “Events & Announcements” in the sidebar to see the latest school news for you.',
      ar: 'افتح «الفعاليات والإعلانات» من القائمة الجانبية لمشاهدة آخر أخبار المدرسة الخاصة بك.'
    }
  },
  {
    id: 'password',
    q: { en: 'How do I change my password?', ar: 'كيف أغيّر كلمة المرور؟' },
    a: {
      en: 'Go to your Profile (top-right menu) and use Change Password; enter your current password first.',
      ar: 'افتح ملفك الشخصي (من القائمة أعلى اليمين) واستخدم «تغيير كلمة المرور»؛ أدخل كلمة المرور الحالية أولاً.'
    }
  },
  {
    id: 'teacher-attendance',
    q: { en: 'How do teachers record attendance?', ar: 'كيف يسجّل المعلم الحضور؟' },
    a: {
      en: 'Open “Attendance”, pick your subject and date, mark each student Present/Absent/Excused, then Save.',
      ar: 'افتح «الحضور»، واختر المادة والتاريخ، وحدّد لكل طالب حاضر/غائب/بعذر، ثم احفظ.'
    }
  },
  {
    id: 'teacher-grades',
    q: { en: 'How do teachers enter grades?', ar: 'كيف يُدخل المعلم الدرجات؟' },
    a: {
      en: 'Open “Grades”, choose your subject and the assessment type (Quiz/Midterm/Final/Homework), enter each student’s mark, then Save.',
      ar: 'افتح «الدرجات»، واختر المادة ونوع التقييم (اختبار قصير/نصفي/نهائي/واجب)، وأدخل درجة كل طالب، ثم احفظ.'
    }
  },
  {
    id: 'teacher-materials',
    q: { en: 'How do teachers add learning materials?', ar: 'كيف يضيف المعلم المواد التعليمية؟' },
    a: {
      en: 'Open “Learning Materials”, choose your subject, add a YouTube link or upload a PDF, then click Add material.',
      ar: 'افتح «المواد التعليمية»، واختر المادة، وأضف رابط يوتيوب أو ارفع ملف PDF، ثم اضغط «إضافة المادة».'
    }
  },
  {
    id: 'parent-child',
    q: { en: "How do parents view their child's info?", ar: 'كيف يتابع وليّ الأمر بيانات ابنه؟' },
    a: {
      en: 'Parents: use the child selector at the top, then open Attendance, Grades, or Learning Materials to see that child’s data. If you have more than one child, switch between them there.',
      ar: 'أولياء الأمور: استخدم أداة اختيار الطالب في الأعلى، ثم افتح الحضور أو الدرجات أو المواد التعليمية لعرض بيانات ذلك الطالب. إن كان لديك أكثر من ابن، بدّل بينهم من هناك.'
    }
  },
  {
    id: 'language',
    q: { en: 'How do I switch language?', ar: 'كيف أبدّل اللغة؟' },
    a: {
      en: 'Use the “English | عربي” toggle in the top bar to switch between English and Arabic at any time — the whole app updates instantly.',
      ar: 'استخدم زر «English | عربي» في الشريط العلوي للتبديل بين الإنجليزية والعربية في أي وقت — يتحدّث التطبيق بأكمله فوراً.'
    }
  },
  {
    id: 'contact',
    q: { en: 'Who do I contact for help?', ar: 'بمن أتواصل للمساعدة؟' },
    a: {
      en: 'Contact the school admin at it@almanahilschool.com or call 0176073805.',
      ar: 'تواصل مع إدارة المدرسة عبر it@almanahilschool.com أو اتصل على 0176073805.'
    }
  }
]
