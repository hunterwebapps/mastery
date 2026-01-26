import { createBrowserRouter, RouterProvider } from 'react-router-dom'
import { RootLayout } from './root-layout'

const router = createBrowserRouter([
  {
    path: '/',
    element: <RootLayout />,
    children: [
      {
        index: true,
        lazy: () => import('@/features/dashboard/pages/dashboard-page'),
      },
      {
        path: 'goals',
        lazy: () => import('@/features/goals/pages/goals-page'),
      },
      {
        path: 'habits',
        lazy: () => import('@/features/habits/pages/habits-page'),
      },
      {
        path: 'check-in',
        lazy: () => import('@/features/check-ins/pages/check-in-page'),
      },
    ],
  },
])

export function AppRouter() {
  return <RouterProvider router={router} />
}
