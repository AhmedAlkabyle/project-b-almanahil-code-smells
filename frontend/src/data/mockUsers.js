// Mock user records for the Manage Users screen.
// Roles are the same PascalCase strings the backend uses: Teacher | Student | Parent | Admin.
// Every row carries the personal fields the Add/Edit form collects:
//   firstName, middleName, lastName, name (full, for the table/search), gender, dob, email, role, status
// Role-specific fields:
//   - Student → classId (→ mockClasses), parentId (→ a Parent row), contactNumber (the parent's phone)
//   - Parent  → phone (the parent's own contact number)
// TODO: replace with GET /api/users once the backend endpoint exists.
export const mockUsers = [
  {
    id: 1,
    firstName: 'Fatima', middleName: '', lastName: 'Al-Zahra', name: 'Fatima Al-Zahra',
    gender: 'Female', dob: '1988-03-14',
    email: 'fatima.teacher@almanahilschool.com', role: 'Teacher', status: 'Active'
  },
  {
    id: 2,
    firstName: 'Khalid', middleName: '', lastName: 'Mustafa', name: 'Khalid Mustafa',
    gender: 'Male', dob: '1985-07-22',
    email: 'khalid.teacher@almanahilschool.com', role: 'Teacher', status: 'Active'
  },
  {
    id: 3,
    firstName: 'Ahmed', middleName: '', lastName: 'Al-Mansouri', name: 'Ahmed Al-Mansouri',
    gender: 'Male', dob: '2015-09-10',
    email: 'ahmed.student@almanahilschool.com', role: 'Student', status: 'Active',
    classId: 1, parentId: 6, contactNumber: '+218 91 234 5678'
  },
  {
    id: 4,
    firstName: 'Layla', middleName: '', lastName: 'Ibrahim', name: 'Layla Ibrahim',
    gender: 'Female', dob: '2014-02-18',
    email: 'layla.student@almanahilschool.com', role: 'Student', status: 'Active',
    classId: 2, parentId: 7, contactNumber: '+218 92 345 6789'
  },
  {
    id: 5,
    firstName: 'Yusuf', middleName: '', lastName: 'Khaled', name: 'Yusuf Khaled',
    gender: 'Male', dob: '2013-11-05',
    email: 'yusuf.student@almanahilschool.com', role: 'Student', status: 'Inactive',
    classId: 3, parentId: 8, contactNumber: '+218 94 456 7890'
  },
  {
    id: 6,
    firstName: 'Nadia', middleName: '', lastName: 'Ahmed', name: 'Nadia Ahmed',
    gender: 'Female', dob: '1986-06-30',
    email: 'nadia.parent@almanahilschool.com', role: 'Parent', status: 'Active',
    phone: '+218 91 234 5678'
  },
  {
    id: 7,
    firstName: 'Sami', middleName: '', lastName: 'Ibrahim', name: 'Sami Ibrahim',
    gender: 'Male', dob: '1982-12-12',
    email: 'sami.parent@almanahilschool.com', role: 'Parent', status: 'Active',
    phone: '+218 92 345 6789'
  },
  {
    id: 8,
    firstName: 'Huda', middleName: '', lastName: 'Saleh', name: 'Huda Saleh',
    gender: 'Female', dob: '1984-04-25',
    email: 'huda.parent@almanahilschool.com', role: 'Parent', status: 'Inactive',
    phone: '+218 94 456 7890'
  }
]

export default mockUsers
