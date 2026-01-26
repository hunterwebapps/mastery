# Mastery

AI-guided personal mastery system with closed-loop feedback for goals, habits, and productivity.

## Architecture

This is a monorepo containing:

- **`src/web`** - React SPA (Vite + TypeScript + PWA)
- **`src/api`** - .NET 10 API (Clean Architecture + DDD + Entity Framework)

## Tech Stack

### Frontend
- React 19 with TypeScript
- Vite for build tooling
- TanStack Query for server state
- Zustand for client state
- Tailwind CSS for styling
- PWA support for offline functionality

### Backend
- .NET 10 with ASP.NET Core
- Clean Architecture with DDD
- Entity Framework Core with SQL Server
- MediatR for CQRS
- FluentValidation for input validation

## Getting Started

### Prerequisites
- Node.js 20+
- .NET 10 SDK
- SQL Server (or Docker)

### Frontend
```bash
cd src/web
npm install
npm run dev
```

### Backend
```bash
cd src/api
dotnet restore
dotnet run --project Mastery.Api
```

## Project Structure

```
mastery/
├── src/
│   ├── web/           # React SPA
│   └── api/           # .NET API
│       ├── Mastery.Domain/
│       ├── Mastery.Application/
│       ├── Mastery.Infrastructure/
│       └── Mastery.Api/
├── docs/              # Documentation
└── .github/           # CI/CD workflows
```

## License

Private - All rights reserved.
