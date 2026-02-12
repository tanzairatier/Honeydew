import { createRouter, createWebHistory } from 'vue-router'
import type { RouteRecordRaw } from 'vue-router'
import { getCurrentUser } from '@/api/usersService'
import LoginView from '@/views/LoginView.vue'
import RegisterView from '@/views/RegisterView.vue'
import AppShellView from '@/views/AppShellView.vue'
import HomeView from '@/views/app/HomeView.vue'
import ItemsLayout from '@/views/app/ItemsLayout.vue'
import YourItemsView from '@/views/app/YourItemsView.vue'
import SearchView from '@/views/app/SearchView.vue'
import ExportView from '@/views/app/ExportView.vue'
import PreferencesView from '@/views/app/PreferencesView.vue'
import ProfileView from '@/views/app/ProfileView.vue'
import UsersView from '@/views/app/settings/UsersView.vue'
import GeneralView from '@/views/app/settings/GeneralView.vue'
import BillingView from '@/views/app/settings/BillingView.vue'
import SupportView from '@/views/app/settings/SupportView.vue'

const routes: RouteRecordRaw[] = [
  { path: '/', redirect: '/login' },
  { path: '/login', name: 'login', component: LoginView },
  { path: '/register', name: 'register', component: RegisterView },
  {
    path: '/app',
    component: AppShellView,
    meta: { requiresAuth: true },
    children: [
      { path: '', redirect: 'home' },
      { path: 'home', name: 'home', component: HomeView },
      { path: 'your-items', redirect: { name: 'items-your' } },
      {
        path: 'items',
        component: ItemsLayout,
        children: [
          { path: '', redirect: { name: 'items-your' } },
          { path: 'your-items', name: 'items-your', component: YourItemsView },
          { path: 'search', name: 'items-search', component: SearchView },
          { path: 'export', name: 'items-export', component: ExportView },
        ],
      },
      { path: 'preferences', name: 'preferences', component: PreferencesView },
      { path: 'profile', name: 'profile', component: ProfileView },
      { path: 'settings', redirect: 'settings/users' },
      { path: 'settings/users', name: 'settings-users', component: UsersView },
      { path: 'settings/general', name: 'settings-general', component: GeneralView },
      { path: 'settings/billing', name: 'settings-billing', component: BillingView },
      { path: 'settings/support', name: 'settings-support', component: SupportView },
    ],
  },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
})

router.beforeEach(async (to) => {
  if (to.meta.requiresAuth && !localStorage.getItem('honeydew_token')) {
    return { name: 'login' }
  }
  if (to.meta.requiresAuth && to.path.startsWith('/app/settings')) {
    try {
      const user = await getCurrentUser()
      const isOwner = user.role === 'Owner'
      const canManageUsers = isOwner || user.canCreateUser === true
      if (to.path === '/app/settings/general' || to.path === '/app/settings/billing' || to.path === '/app/settings/support') {
        if (!isOwner) return { path: '/app/home' }
      }
      if (to.path === '/app/settings/users') {
        if (!canManageUsers) return { path: '/app/home' }
      }
    } catch {
      // allow navigation; API will return 403
    }
  }
  return true
})

export default router
