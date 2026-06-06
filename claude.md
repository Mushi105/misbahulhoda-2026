# CLAUDE.md

# Project Name
Misbahuda – Arbaeen 2026 Pilgrimage Management System

# Project Vision

Build a production-ready enterprise pilgrimage management system for Arbaeen 2026.

The platform will manage:
- Pilgrims
- Volunteers
- Accommodation
- Karwan tracking
- Ziyarat schedules
- Majalis schedules
- Notifications
- WhatsApp communication
- Airport pickup
- Bus allocation
- Room management
- Live GPS tracking

The system should support 1000+ concurrent users.

---

# Technology Stack

## Backend
- ASP.NET Core 8 Web API
- Clean Architecture
- CQRS with MediatR
- Entity Framework Core
- PostgreSQL
- Redis Cache
- SignalR
- JWT Authentication

## Frontend
- Vue.js 3
- Composition API
- Pinia
- Vue Router
- Axios
- Tailwind CSS

## DevOps
- Docker
- Docker Compose
- Nginx
- GitHub Actions
- Ubuntu VPS

---

# Architecture Rules

Follow enterprise-level architecture.

Projects:
- Misbahuda.API
- Misbahuda.Application
- Misbahuda.Domain
- Misbahuda.Infrastructure

Rules:
- Use SOLID principles
- Use Repository Pattern
- Use Unit Of Work
- Use DTOs
- Use FluentValidation
- Use AutoMapper
- Use async/await everywhere
- Use dependency injection
- Avoid business logic inside controllers
- Controllers should remain thin

---

# User Roles

## Roles
- Super Admin
- Admin
- Volunteer Manager
- Volunteer
- Pilgrim
- Driver

Implement role-based authorization everywhere.

---

# Main Features

## Pilgrim Management
Pilgrims can:
- Register/login
- Upload passport and visa
- Enter visa number
- Enter country
- Enter family member count
- Enter arrival/departure dates
- Track application status
- View room allocation
- View bus allocation
- Receive notifications
- Track Karwan location

---

## Volunteer Management

Volunteers have separate dashboard.

Features:
- Task assignment
- Attendance
- Shift management
- Emergency assignment
- WhatsApp notifications
- Airport support
- Bus coordination
- Crowd management
- Food distribution
- Hotel support

Volunteer statuses:
- Available
- Busy
- Offline
- Emergency Assigned

---

## Accommodation Management

Features:
- Hotels
- Buildings
- Floors
- Rooms
- Bed capacity
- Family allocation
- Occupancy tracking

---

## Karwan Tracking

Features:
- Live GPS tracking
- Bus tracking
- Stop locations
- Pole number tracking
- ETA updates
- Google Maps integration

---

## Majalis Management

Features:
- Urdu Majalis
- English Majalis
- Molana profiles
- Noha Khuwan profiles
- Namaz timings
- Food schedules

---

## Notification System

Support:
- Push notifications
- WhatsApp notifications
- Email notifications
- SMS notifications

Events:
- Approval notifications
- Room allocation
- Bus departure
- Majalis reminder
- Food timing
- Emergency alerts

---

# Database Rules

Use PostgreSQL.

Requirements:
- Proper indexing
- Foreign keys
- Soft delete
- Audit columns
- Optimized queries

Main Tables:
- Users
- Roles
- Pilgrims
- Volunteers
- Tasks
- Hotels
- Rooms
- Buses
- Karwans
- Notifications
- Majalis
- GPSLocations
- AuditLogs

---

# API Standards

Use RESTful APIs.

Requirements:
- Versioned APIs
- Swagger documentation
- Global exception handling
- Standard response wrapper
- Pagination
- Filtering
- Sorting
- Rate limiting

Example Response:
{
  "success": true,
  "message": "Operation successful",
  "data": {}
}

---

# Security Requirements

Implement:
- JWT Authentication
- Refresh Tokens
- HTTPS
- Rate limiting
- Secure file uploads
- Audit logs
- Password hashing

---

# Frontend UI Rules

Design:
- Islamic elegant theme
- Black, green, gold colors
- Elderly-friendly UI
- Responsive design
- Mobile-first approach

Pages:
- Dashboard
- Pilgrim Portal
- Volunteer Portal
- Admin Panel
- Karwan Tracking
- Notifications
- Accommodation
- Majalis Schedule

---

# Coding Standards

Backend:
- Use feature-based folders
- Use clean naming conventions
- Add XML comments
- Use cancellation tokens
- Use async APIs

Frontend:
- Reusable components
- Composition API only
- Proper state management
- Lazy loading
- API abstraction layer

---

# Performance Requirements

System must support:
- 1000+ concurrent users
- Realtime SignalR communication
- Fast API responses
- Redis caching
- Optimized database queries

---

# Deliverables

Generate:
- Complete backend architecture
- Database schema
- Entity models
- DTOs
- API endpoints
- Vue.js frontend
- Authentication flow
- Docker configuration
- Deployment scripts
- Admin dashboard
- Volunteer dashboard
- Realtime notification system
- WhatsApp integration

---

# Important Instructions

Always generate:
- Production-ready code
- Scalable architecture
- Clean code
- Maintainable code
- Secure code

Avoid:
- Spaghetti code
- Business logic inside controllers
- Duplicate code
- Hardcoded values

Focus on:
- Scalability
- Security
- Performance
- Maintainability
- Realtime communication
