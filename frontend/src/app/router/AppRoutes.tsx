import { Route, Routes } from 'react-router'
import { PublicOnlyRoute } from '../../features/auth/PublicOnlyRoute'
import { RequireAuth } from '../../features/auth/RequireAuth'
import { applicationRoles } from '../../features/auth/types'
import { EventsPage } from '../../features/events/EventsPage'
import { AdminOrganizerRequestsPage } from '../../features/organizerRequests/AdminOrganizerRequestsPage'
import { AdminUsersPage } from '../../features/users/AdminUsersPage'
import { DashboardPage } from '../../pages/DashboardPage'
import { HomePage } from '../../pages/HomePage'
import { LoginPage } from '../../pages/LoginPage'
import { NotFoundPage } from '../../pages/NotFoundPage'
import { PlaceholderSectionPage } from '../../pages/PlaceholderSectionPage'
import { RegisterPage } from '../../pages/RegisterPage'
import { EventPlanningPage } from '../../features/planning/EventPlanningPage'
import { ResourcesPage } from '../../features/resources/ResourcesPage'
import { AppShell } from '../layout/AppShell'

export function AppRoutes() {
  return (
    <Routes>
      <Route index element={<HomePage />} />
      <Route element={<PublicOnlyRoute />}>
        <Route path="login" element={<LoginPage />} />
        <Route path="register" element={<RegisterPage />} />
      </Route>
      <Route element={<RequireAuth />}>
        <Route element={<AppShell />}>
          <Route path="dashboard" element={<DashboardPage />} />
          <Route
            path="events"
            element={<EventsPage />}
          />
          <Route
            path="registrations"
            element={
              <PlaceholderSectionPage
                title="Prijave i rezervacije"
                description="Pregled prijava učesnika i rezervacija povezanih sa događajima."
              />
            }
          />
        </Route>
      </Route>
      <Route
        element={
          <RequireAuth
            roles={[applicationRoles.organizer, applicationRoles.admin]}
          />
        }
      >
        <Route element={<AppShell />}>
          <Route
            path="resources"
            element={<ResourcesPage />}
          />
          <Route
            path="events/:eventId/planning"
            element={<EventPlanningPage />}
          />
          <Route
            path="reports"
            element={
              <PlaceholderSectionPage
                title="Izveštaji"
                description="Pregled recenzija i osnovnih pokazatelja organizovanih događaja."
              />
            }
          />
        </Route>
      </Route>
      <Route element={<RequireAuth roles={[applicationRoles.admin]} />}>
        <Route element={<AppShell />}>
          <Route
            path="admin/users"
            element={<AdminUsersPage />}
          />
          <Route
            path="admin/organizer-requests"
            element={<AdminOrganizerRequestsPage />}
          />
        </Route>
      </Route>
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  )
}
