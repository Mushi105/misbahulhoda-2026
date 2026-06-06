import api from './client'

export const noticeApi = {
  getAll: () => api.get('/notices'),
  create: (data) => api.post('/notices', data),
  update: (id, data) => api.put(`/notices/${id}`, data),
  delete: (id) => api.delete(`/notices/${id}`),
  toggle: (id) => api.patch(`/notices/${id}/toggle`),
}
