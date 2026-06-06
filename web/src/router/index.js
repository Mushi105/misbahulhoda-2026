import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const routes = [
  // Public landing page
  { path: '/', component: () => import('@/views/LandingPage.vue') },
  // Login / Register
  { path: '/login', component: () => import('@/views/auth/LoginView.vue'), meta: { guest: true } },
  { path: '/register', redirect: '/login' },
  { path: '/reset-password', component: () => import('@/views/auth/ResetPassword.vue'), meta: { guest: true } },

  {
    path: '/dashboard',
    component: () => import('@/views/DashboardView.vue'),
    meta: { requiresAuth: true }
  },

  // Admin Portal
  {
    path: '/admin',
    component: () => import('@/components/layout/AdminLayout.vue'),
    meta: { requiresAuth: true, roles: ['SuperAdmin', 'Admin'] },
    children: [
      { path: '', redirect: '/admin/dashboard' },
      { path: 'dashboard', component: () => import('@/views/admin/AdminDashboard.vue') },
      { path: 'pilgrims', component: () => import('@/views/admin/PilgrimsManagement.vue') },
      { path: 'volunteers', component: () => import('@/views/admin/VolunteersManagement.vue') },
      { path: 'accommodation', component: () => import('@/views/admin/AccommodationView.vue') },
      { path: 'noticeboard', component: () => import('@/views/admin/NoticeBoardAdmin.vue') },
      { path: 'users', component: () => import('@/views/admin/UsersManagement.vue') },
      { path: 'notifications', component: () => import('@/views/admin/NotificationsAdmin.vue') },
      { path: 'scholars', component: () => import('@/views/admin/ScholarsManagement.vue') },
      { path: 'documents', component: () => import('@/views/admin/DocumentsManagement.vue') },
      { path: 'reports', component: () => import('@/views/admin/ReportsAdmin.vue') },
      { path: 'finance', component: () => import('@/views/admin/FinanceManagement.vue') },
      { path: 'international', component: () => import('@/views/admin/InternationalFinance.vue') },
      { path: 'itinerary', component: () => import('@/views/admin/ItineraryAdmin.vue') },
      { path: 'tours', component: () => import('@/views/admin/TourManagement.vue') },
      { path: 'tour-tracking', component: () => import('@/views/admin/TourLiveTracking.vue') },
    ]
  },

  // Pilgrim Portal
  {
    path: '/pilgrim',
    component: () => import('@/components/layout/AppLayout.vue'),
    meta: { requiresAuth: true, roles: ['Pilgrim'] },
    children: [
      { path: '', redirect: '/pilgrim/portal' },
      { path: 'portal', component: () => import('@/views/pilgrim/PilgrimPortal.vue') },
      { path: 'profile', component: () => import('@/views/pilgrim/PilgrimProfile.vue') },
      { path: 'guide', component: () => import('@/views/pilgrim/TourGuide.vue') },
      { path: 'documents', component: () => import('@/views/pilgrim/DocumentsView.vue') },
      { path: 'tours', component: () => import('@/views/pilgrim/TourApply.vue') },
      { path: 'history', component: () => import('@/views/pilgrim/TourHistory.vue') },
      { path: 'feedback', component: () => import('@/views/pilgrim/FeedbackSubmit.vue') },
    ]
  },

  // Volunteer Portal
  {
    path: '/volunteer',
    component: () => import('@/components/layout/AppLayout.vue'),
    meta: { requiresAuth: true, roles: ['Volunteer', 'VolunteerManager'] },
    children: [
      { path: '', redirect: '/volunteer/dashboard' },
      { path: 'dashboard', component: () => import('@/views/volunteer/VolunteerDashboard.vue') },
      { path: 'documents', component: () => import('@/views/pilgrim/DocumentsView.vue') },
    ]
  },

  { path: '/tracking', component: () => import('@/views/tracking/TrackingView.vue'), meta: { requiresAuth: true } },
  { path: '/majalis', component: () => import('@/views/MajalisView.vue'), meta: { requiresAuth: true } },
  { path: '/:pathMatch(.*)*', component: () => import('@/views/NotFoundView.vue') },
]

const router = createRouter({ history: createWebHistory(), routes })

router.beforeEach((to, from, next) => {
  const auth = useAuthStore()

  if (to.meta.requiresAuth && !auth.isAuthenticated) return next('/login')

  if (to.meta.guest && auth.isAuthenticated) {
    if (auth.isAdmin) return next('/admin/dashboard')
    if (auth.isPilgrim) return next('/pilgrim/portal')
    if (auth.isVolunteer) return next('/volunteer/dashboard')
    return next('/dashboard')
  }

  if (to.meta.roles && !to.meta.roles.includes(auth.role)) {
    if (auth.isAdmin) return next('/admin/dashboard')
    if (auth.isPilgrim) return next('/pilgrim/portal')
    if (auth.isVolunteer) return next('/volunteer/dashboard')
    return next('/')
  }

  next()
})

export default router
