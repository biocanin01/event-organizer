import { Route, Routes } from 'react-router'
import { HomePage } from '../../pages/HomePage'
import { LoginPlaceholderPage } from '../../pages/LoginPlaceholderPage'
import { NotFoundPage } from '../../pages/NotFoundPage'

export function AppRoutes() {
  return (
    <Routes>
      <Route index element={<HomePage />} />
      <Route path="login" element={<LoginPlaceholderPage />} />
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  )
}
