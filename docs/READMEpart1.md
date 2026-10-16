
---
**Author**: Prudence Dithole
**Module**: Programming 2B (PROG6212)  
**Year**: 2026  
**Institution**: ROSEBANK INTERNATIONAL

# RaceDay - Event Management System
### CI/CD
- GitHub Actions workflow validates repository structure
- ✅ Green build:
![green-build](docs/green-build.png)


## Overview
RaceDay is a web-based event management system designed specifically for the South African road running, walking, and cycling community. The platform allows Event Organisers to create and manage events, categories, and participant results, while Participants can browse upcoming events, enter events, and track their personal performance history. The system supports role-based access with distinct interfaces for Organisers and Participants.

### System Roles

### Organiser
- Create, edit, and delete events
- Manage event categories (age groups and distance categories)
- Capture participant results (finish times and positions)
- View all event enrolments
- Upload event banner images

### Participant
- Create an account and manage profile
- Browse upcoming events with filtering options
- Enter events by selecting a category
- View personal enrolment history
- Track personal race results and performance
- Upload profile picture

## Part 1: System Planning and Database

## Technology Stack
- **Backend**: ASP.NET Core Web API (C#)
- **Database**: SQL Server with Entity Framework Core
- **Frontend**: ASP.NET Core MVC with Bootstrap
- **Authentication**: Session-based with role enforcement
- **Cloud Storage**: Azure Blob Storage
- **Containerization**: Docker
- **CI/CD**: GitHub Actions


### Documentation
- **ERD Diagram**: `/docs/erd.png` 
- **API Endpoint Plan**: `/docs/api-endpoint-plan.md` 
- **SQL Script**: `/docs/raceday-schema.sql` 

## Setup Instructions

### Prerequisites
- SQL Server Management Studio (SSMS)
- .NET 8 SDK
- Visual Studio 2022 or later
- Git
- Docker Desktop (for Part 3)

### Database Setup
1. Open SQL Server Management Studio
2. Connect to your SQL Server instance
3. Open `docs/raceday-schema.sql`
4. Execute the script (press F5)
5. Verify tables and data are created

### Repository Structure
RaceDay/
        .github/S
        -workflows/
        -validate-part1.yml
 docs/
      erd.png
      api-endpoint-plan.pdf
      raceday-schema.sql
README.md

## Features
-  Role-based authentication (Organiser and Participant)
-  Event creation and management
-  Category management for events
-  Participant enrolment system
-  Results capture and tracking
-  Image upload with Azure Blob Storage
-  Dashboard for Organisers
-  Personal performance history for Participants

## Development Roadmap

### Part 1: System Planning ✅ (Complete)
- ERD design with 8 entities
- API endpoint planning
- Database schema creation
- CI/CD setup

### Part 2: API Development (Coming Soon)
- RESTful API with ASP.NET Core
- Entity Framework Core integration
- Role-based authentication
- Unit testing
- Swagger documentation

### Part 3: Web Application (Coming Soon)
- MVC frontend
- Azure Blob Storage
- Docker containerization
- Production-ready deployment

## Video Presentation
[youtube link](https://youtu.be/nC7OiqRuY3E?si=dczsjYThvwXNlgdM)