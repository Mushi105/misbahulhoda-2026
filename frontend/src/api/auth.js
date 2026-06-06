import api from './client'

export const authApi = {
  login: (email, password) => api.post('/auth/login', { email, password }),
  register: (data) => api.post('/auth/register', data),
  refresh: (accessToken, refreshToken) => api.post('/auth/refresh-token', { accessToken, refreshToken }),
}
