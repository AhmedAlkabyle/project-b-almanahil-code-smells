// ─── WHAT THIS FILE DOES (in plain words) ───
// This file talks to the backend about LEARNING MATERIALS (PDFs & video links):
//   GET    /api/materials/my-subjects    → a teacher's subjects
//   GET    /api/materials/subject/{id}   → materials for one subject
//   GET    /api/materials/my             → the logged-in student's materials
//   GET    /api/materials/child/{id}     → a parent's view of one child's materials
//   POST   /api/materials                → add a material (e.g. a YouTube link)
//   POST   /api/materials/upload-pdf     → upload a PDF file
//   DELETE /api/materials/{id}           → delete a material
import api from './axios'
import { request } from './client'

// Learning Materials API (Module 5). Every function returns { data, message }.
const BASE = '/api/materials'

/** GET /api/materials/my-subjects — the logged-in teacher's assigned subjects. */
export const getMySubjects = () => request(api.get(`${BASE}/my-subjects`))

/** GET /api/materials/subject/{subjectId} — materials for one subject. */
export const getSubjectMaterials = (subjectId) => request(api.get(`${BASE}/subject/${subjectId}`))

/** GET /api/materials/my — every material for the logged-in student's class subjects. */
export const getMyMaterials = () => request(api.get(`${BASE}/my`))

/** GET /api/materials/child/{studentId} — a parent's view of one child's class materials. */
export const getChildMaterials = (studentId) => request(api.get(`${BASE}/child/${studentId}`))

/** POST /api/materials — { subjectId, title, description?, materialType, url }. */
export const addMaterial = (payload) => request(api.post(BASE, payload))

/**
 * POST /api/materials/upload-pdf — multipart/form-data upload of a PDF.
 * Pass a FormData with fields: subjectId, title, description?, file. Axios sets the
 * multipart boundary automatically from the FormData.
 */
export const uploadPdf = (formData) => request(api.post(`${BASE}/upload-pdf`, formData))

/** DELETE /api/materials/{id}. */
export const deleteMaterial = (id) => request(api.delete(`${BASE}/${id}`))
