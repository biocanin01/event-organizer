import { Route, Routes } from 'react-router'
import { PublicOnlyRoute } from '../../features/auth/PublicOnlyRoute'
import { RequireAuth } from '../../features/auth/RequireAuth'
import { applicationRoles } from '../../features/auth/types'
import { DashboardPage } from '../../pages/DashboardPage'
import { HomePage } from '../../pages/HomePage'
import { LoginPage } from '../../pages/LoginPage'
import { NotFoundPage } from '../../pages/NotFoundPage'
import { PlaceholderSectionPage } from '../../pages/PlaceholderSectionPage'
import { RegisterPage } from '../../pages/RegisterPage'
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
            element={
              <PlaceholderSectionPage
                title="Dogadjaji"
                description="Pregled dogadjaja i buduce akcije za prijavu, kreiranje i uredjivanje."
              />
            }
          />
          <Route
            path="registrations"
            element={
              <PlaceholderSectionPage
                title="Prijave i rezervacije"
                description="Pregled prijava ucesnika i rezervacija povezanih sa dogadjajima."
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
            element={
              <PlaceholderSectionPage
                title="Resursi"
                description="Pregled resursa i priprema za objedinjeno planiranje rezervacija."
              />
            }
          />
          <Route
            path="reports"
            element={
              <PlaceholderSectionPage
                title="Izvestaji"
                description="Pregled recenzija i osnovnih pokazatelja organizovanih dogadjaja."
              />
            }
          />
        </Route>
      </Route>
      <Route element={<RequireAuth roles={[applicationRoles.admin]} />}>
        <Route element={<AppShell />}>
          <Route
            path="admin/users"
            element={
              <PlaceholderSectionPage
                title="Korisnici"
                description="Administratorski pregled korisnika, statusa i aktivnosti."
              />
            }
          />
          <Route
            path="admin/organizer-requests"
            element={
              <PlaceholderSectionPage
                title="Zahtevi za organizatore"
                description="Administratorsko odobravanje i odbijanje zahteva za Organizer rolu."
              />
            }
          />
        </Route>
      </Route>
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  )
}
