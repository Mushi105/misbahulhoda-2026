import api from './client'

export const pilgrimApi = {
  createProfile: (data) => api.post('/pilgrims/profile', data),
  getMyProfile: () => api.get('/pilgrims/profile/me'),
  addFamilyMember: (data) => api.post('/pilgrims/family-members', data),
  updateFamilyMember: (id, data) => api.put(`/pilgrims/family-members/${id}`, data),
  removeFamilyMember: (id) => api.delete(`/pilgrims/family-members/${id}`),
}

export const adminApi = {
  getDashboard: () => api.get('/admin/dashboard'),
  getPilgrims: (params) => api.get('/admin/pilgrims', { params }),
  updatePilgrimStatus: (id, data) => api.patch(`/admin/pilgrims/${id}/status`, data),
  approvePilgrim: (id) => api.patch(`/admin/pilgrims/${id}/approve`),
  rejectPilgrim: (id, reason) => api.patch(`/admin/pilgrims/${id}/reject`, { reason }),
  markUnderReview: (id) => api.patch(`/admin/pilgrims/${id}/under-review`),
  resetToPending: (id) => api.patch(`/admin/pilgrims/${id}/reset-to-pending`),
  allocateRoom: (pilgrimId, roomId) => api.post(`/admin/pilgrims/${pilgrimId}/allocate-room`, { roomId }),
  allocateBus: (pilgrimId, busId) => api.post(`/admin/pilgrims/${pilgrimId}/allocate-bus`, { busId }),
  getUsers: (params) => api.get('/admin/users', { params }),
  createUser: (data) => api.post('/admin/users', data),
  toggleUserActive: (id) => api.patch(`/admin/users/${id}/toggle-active`),
  changeRole: (id, role) => api.patch(`/admin/users/${id}/change-role`, { role }),
  sendNotification: (data) => api.post('/notifications/send', data),
  emergencyBroadcast: (data) => api.post('/admin/emergency/broadcast', data),
  getVolunteerLiveStatus: () => api.get('/admin/volunteers/live-status'),
}

export const volunteerApi = {
  getAll: (params) => api.get('/volunteers', { params }),
  getMe: () => api.get('/volunteers/me'),
  createProfile: (data) => api.post('/volunteers/profile', data),
  createByAdmin: (data) => api.post('/volunteers/create', data),
  update: (id, data) => api.put(`/volunteers/${id}`, data),
  updateStatus: (id, status) => api.patch(`/volunteers/${id}/status`, { status }),
  checkIn: (id) => api.post(`/volunteers/${id}/checkin`),
  checkOut: (id) => api.post(`/volunteers/${id}/checkout`),
  assignZone: (id, zoneId) => api.patch(`/volunteers/${id}/zone`, { zoneId }),
  assignPilgrim: (id, data) => api.post(`/volunteers/${id}/assign-pilgrim`, data),
  removePilgrimAssignment: (id, pilgrimId) => api.delete(`/volunteers/${id}/assign-pilgrim/${pilgrimId}`),
  assignTask: (data) => api.post('/volunteers/tasks/assign', data),
  completeTask: (taskId) => api.patch(`/volunteers/tasks/${taskId}/complete`),
  getTasks: (id) => api.get(`/volunteers/${id}/tasks`),
  getAttendance: (id) => api.get(`/volunteers/${id}/attendance`),
  getReport: (id) => api.get(`/volunteers/${id}/report`),
}

export const zonesApi = {
  getAll: () => api.get('/zones'),
  create: (data) => api.post('/zones', data),
  update: (id, data) => api.put(`/zones/${id}`, data),
  delete: (id) => api.delete(`/zones/${id}`),
}

export const accommodationApi = {
  getHotels: () => api.get('/accommodation/hotels'),
  getHotelsWithRooms: () => api.get('/accommodation/hotels/with-rooms'),
  createHotel: (data) => api.post('/accommodation/hotels/simple', data),
  deleteHotel: (id) => api.delete(`/accommodation/hotels/${id}`),
  addRoomsToHotel: (hotelId, data) => api.post(`/accommodation/hotels/${hotelId}/add-rooms`, data),
  updateFloor: (floorId, data) => api.put(`/accommodation/floors/${floorId}`, data),
  deleteFloor: (floorId) => api.delete(`/accommodation/floors/${floorId}`),
  getRooms: (params) => api.get('/accommodation/rooms', { params }),
  getOccupancy: () => api.get('/accommodation/occupancy'),
  allocateRoom: (roomId, pilgrimId) => api.post(`/accommodation/rooms/${roomId}/allocate/${pilgrimId}`),
}

export const documentsApi = {
  getAll: () => api.get('/documents'),
  upload: (formData) => api.post('/documents/upload', formData, { headers: { 'Content-Type': 'multipart/form-data' } }),
  download: (id) => api.get(`/documents/${id}/download`, { responseType: 'blob' }),
  delete: (id) => api.delete(`/documents/${id}`),
}

export const financeApi = {
  getSummary:    () => api.get('/finance/summary'),
  getExpenses:   (params) => api.get('/finance/expenses', { params }),
  addExpense:    (data) => api.post('/finance/expenses', data),
  updateExpense: (id, data) => api.put(`/finance/expenses/${id}`, data),
  deleteExpense: (id) => api.delete(`/finance/expenses/${id}`),
  getBudgets:    () => api.get('/finance/budgets'),
  setBudget:     (data) => api.post('/finance/budgets', data),
  export:        () => api.get('/finance/export'),
  getRates:      () => api.get('/finance/rates'),
  setRate:       (data) => api.post('/finance/rates', data),
  getStaff:      () => api.get('/finance/staff'),
  addStaff:      (data) => api.post('/finance/staff', data),
  updateStaff:   (id, data) => api.put(`/finance/staff/${id}`, data),
  markPaid:      (id) => api.patch(`/finance/staff/${id}/mark-paid`),
  deleteStaff:   (id) => api.delete(`/finance/staff/${id}`),
  getTickets:    () => api.get('/finance/tickets'),
  addTicket:     (data) => api.post('/finance/tickets', data),
  updateTicket:  (id, data) => api.put(`/finance/tickets/${id}`, data),
  deleteTicket:  (id) => api.delete(`/finance/tickets/${id}`),
}

export const internationalApi = {
  getBalance:       () => api.get('/international/balance'),
  // Incoming Payments
  getIncoming:      () => api.get('/international/incoming'),
  addIncoming:      (data) => api.post('/international/incoming', data),
  updateIncoming:   (id, data) => api.put(`/international/incoming/${id}`, data),
  deleteIncoming:   (id) => api.delete(`/international/incoming/${id}`),
  // Vendor Contracts
  getContracts:     () => api.get('/international/contracts'),
  addContract:      (data) => api.post('/international/contracts', data),
  updateContract:   (id, data) => api.put(`/international/contracts/${id}`, data),
  deleteContract:   (id) => api.delete(`/international/contracts/${id}`),
  // Money Transfers
  getTransfers:     () => api.get('/international/transfers'),
  addTransfer:      (data) => api.post('/international/transfers', data),
  updateTransfer:   (id, data) => api.put(`/international/transfers/${id}`, data),
  deleteTransfer:   (id) => api.delete(`/international/transfers/${id}`),
}

export const itineraryApi = {
  // Pilgrim
  getMy:          ()          => api.get('/itinerary/my'),
  updateTravel:   (data)      => api.put('/itinerary/my/travel', data),
  // Admin
  getToday:       ()          => api.get('/itinerary/today'),
  getUpcoming:    (days = 7)  => api.get('/itinerary/upcoming', { params: { days } }),
  getArrivals:    (date)      => api.get('/itinerary/arrivals',   { params: { date } }),
  getDepartures:  (date)      => api.get('/itinerary/departures', { params: { date } }),
  assignDriver:   (pilgrimId, data) => api.post(`/itinerary/${pilgrimId}/assign`, data),
  updateStatus:   (id, status)      => api.patch(`/itinerary/transfers/${id}/status`, { status }),
  deleteTransfer: (id)              => api.delete(`/itinerary/transfers/${id}`),
  sendReminders:  ()                => api.post('/itinerary/send-reminders'),
}

export const toursApi = {
  // Admin
  getAll:           (params)       => api.get('/tours', { params }),
  getById:          (id)           => api.get(`/tours/${id}`),
  create:           (data)         => api.post('/tours', data),
  update:           (id, data)     => api.put(`/tours/${id}`, data),
  delete:           (id)           => api.delete(`/tours/${id}`),
  complete:         (id)           => api.post(`/tours/${id}/complete`),
  sendFeedbackEmails:   (id)         => api.post(`/tours/${id}/send-feedback-emails`),
  notifyCustomers:      (id)         => api.post(`/tours/${id}/notify-customers`),
  // Packages
  addPackage:       (tourId, data) => api.post(`/tours/${tourId}/packages`, data),
  updatePackage:    (id, data)     => api.put(`/tours/packages/${id}`, data),
  deletePackage:    (id)           => api.delete(`/tours/packages/${id}`),
  // Feedback
  getFeedback:      (tourId)       => api.get(`/tours/${tourId}/feedback`),
  // Pilgrim
  getOpenTours:     ()             => api.get('/tours/open'),
  apply:            (tourId, data) => api.post(`/tours/${tourId}/apply`, data),
  submitFeedback:   (tourId, data) => api.post(`/tours/${tourId}/feedback`, data),
  getMyHistory:     ()             => api.get('/tours/my-history'),
  // Admin
  getUserHistory:   (userId)       => api.get(`/tours/user-history/${userId}`),
  // Schedule & Live Tracking
  getSchedule:      (id)           => api.get(`/tours/${id}/schedule`),
  getLiveTracking:  (id)           => api.get(`/tours/${id}/live-tracking`),
}

export const reportsApi = {
  getSummary: () => api.get('/reports/summary'),
  exportPilgrims: () => api.get('/reports/pilgrims/export'),
  exportVolunteers: () => api.get('/reports/volunteers/export'),
}

export const karwanApi = {
  getAll: () => api.get('/karwan'),
  getById: (id) => api.get(`/karwan/${id}`),
  getLocation: (id) => api.get(`/karwan/${id}/location`),
  updateGps: (data) => api.post('/karwan/gps', data),
}

export const majalisApi = {
  getAll: () => api.get('/majalis'),
  getNamazTimings: (date) => api.get('/majalis/namaz-timings', { params: { date } }),
  getFoodSchedule: (date) => api.get('/majalis/food-schedule', { params: { date } }),
}

export const notificationApi = {
  getMine: () => api.get('/notifications'),
  markRead: (id) => api.patch(`/notifications/${id}/read`),
  markAllRead: async () => {
    const res = await api.get('/notifications')
    const unread = (res.data.data || []).filter(n => !n.isRead)
    await Promise.all(unread.map(n => api.patch(`/notifications/${n.id}/read`)))
    return unread.length
  },
  getUnreadCount: () => api.get('/notifications/unread-count'),
  send: (data) => api.post('/notifications/send', data),
  broadcast: (data) => api.post('/notifications/broadcast', data),
}
