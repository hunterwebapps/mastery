import { createBrowserRouter, RouterProvider } from 'react-router-dom'
import { RootLayout } from './root-layout'

const router = createBrowserRouter([
  // Onboarding route (outside main layout)
  {
    path: '/onboarding',
    lazy: () => import('@/features/onboarding/pages/onboarding-page'),
  },

  // Main authenticated routes
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
        path: 'goals/new',
        lazy: () => import('@/features/goals/pages/create-goal-page'),
      },
      {
        path: 'goals/:id',
        lazy: () => import('@/features/goals/pages/goal-detail-page'),
      },
      {
        path: 'goals/:id/edit',
        lazy: () => import('@/features/goals/pages/edit-goal-page'),
      },
      {
        path: 'habits',
        lazy: () => import('@/features/habits/pages/habits-page'),
      },
      {
        path: 'habits/new',
        lazy: () => import('@/features/habits/pages/create-habit-page'),
      },
      {
        path: 'habits/:id',
        lazy: () => import('@/features/habits/pages/habit-detail-page'),
      },
      {
        path: 'habits/:id/edit',
        lazy: () => import('@/features/habits/pages/edit-habit-page'),
      },
      {
        path: 'tasks',
        lazy: () => import('@/features/tasks/pages/tasks-page'),
      },
      {
        path: 'tasks/inbox',
        lazy: () => import('@/features/tasks/pages/inbox-page'),
      },
      {
        path: 'tasks/new',
        lazy: () => import('@/features/tasks/pages/create-task-page'),
      },
      {
        path: 'tasks/:id',
        lazy: () => import('@/features/tasks/pages/task-detail-page'),
      },
      {
        path: 'tasks/:id/edit',
        lazy: () => import('@/features/tasks/pages/edit-task-page'),
      },
      {
        path: 'projects',
        lazy: () => import('@/features/projects/pages/projects-page'),
      },
      {
        path: 'projects/new',
        lazy: () => import('@/features/projects/pages/create-project-page'),
      },
      {
        path: 'projects/:id',
        lazy: () => import('@/features/projects/pages/project-detail-page'),
      },
      {
        path: 'projects/:id/edit',
        lazy: () => import('@/features/projects/pages/edit-project-page'),
      },
      {
        path: 'experiments',
        lazy: () => import('@/features/experiments/pages/experiments-page'),
      },
      {
        path: 'experiments/new',
        lazy: () => import('@/features/experiments/pages/create-experiment-page'),
      },
      {
        path: 'experiments/:id',
        lazy: () => import('@/features/experiments/pages/experiment-detail-page'),
      },
      {
        path: 'experiments/:id/edit',
        lazy: () => import('@/features/experiments/pages/edit-experiment-page'),
      },
      {
        path: 'recommendations',
        lazy: () => import('@/features/recommendations/pages/recommendations-page'),
      },
      {
        path: 'check-in',
        lazy: () => import('@/features/check-ins/pages/check-in-page'),
      },
      {
        path: 'profile',
        lazy: () => import('@/features/profile/pages/profile-page'),
      },
    ],
  },
])

export function AppRouter() {
  return <RouterProvider router={router} />
}
