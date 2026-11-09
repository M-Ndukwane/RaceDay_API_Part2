# RaceDay – Event Management System

## PROG6212 – Part 3

RaceDay is a web-based race event management system developed using ASP.NET Core Web API and ASP.NET Core MVC with .NET 10.

The application allows organisers to manage sporting events, venues, categories, participant registrations and race results. Participants can register, log in, view available events, enrol in races, manage their profiles and view results.

The project demonstrates RESTful API development, database integration, secure authentication, cloud storage, containerisation, automated testing and continuous integration.

## 1. Technologies Used

- **C# and .NET 10:** Application development.
- **ASP.NET Core Web API:** Backend REST API.
- **ASP.NET Core MVC:** Web application and user interface.
- **Entity Framework Core:** Database access and migrations.
- **Microsoft SQL Server LocalDB:** Relational database.
- **JWT Authentication:** Secure API authentication and role-based authorisation.
- **Azure Blob Storage:** Cloud storage for uploaded images.
- **Docker:** Containerisation of the MVC application.
- **Git and GitHub:** Version control and source code management.
- **GitHub Actions:** Automated building and testing.
- **xUnit:** Automated API testing.
- **Bootstrap, HTML, CSS and JavaScript:** Frontend design.

## 2. Application Features

### Organiser Features

Organisers can log in to their accounts and access a dedicated dashboard. They can create, view, update and delete race events, upload event images, manage venues and event categories, view participant enrolments and manage race results.

### Participant Features

Participants can register and log in, browse available race events, enrol in events, view their enrolments, deregister from events, access race results, update their profile information and change their password.

### Authentication and Security

The API uses JWT authentication to verify users and role-based authorisation to control access to protected endpoints.

The application supports two user roles: Organiser and Participant. Organisers have access to event management functions, while participants can access functions appropriate to their accounts.

Passwords are stored using password hashing rather than plaintext storage.

## 3. Project Structure

The solution contains three main projects:

- **RaceDay.API:** RESTful backend API, controllers, models, database context, migrations and services.
- **RaceDay.MVC:** ASP.NET Core MVC frontend, controllers, ViewModels, Razor views and static assets.
- **RaceDay.API.Tests:** Automated tests for API functionality and authorisation.

The solution file is named `RaceDay.API.slnx`.

## 4. Database

RaceDay uses SQL Server LocalDB and Entity Framework Core.

The database contains the following main tables:

- Roles
- Users
- Venues
- Events
- Categories
- Enrollments
- Results

Entity Framework Core migrations are used to manage database schema changes.

The database connection string is configured through the API's application configuration.

## 5. API Endpoints

The backend exposes RESTful endpoints for managing the application's resources.

| Controller | Base Endpoint | Purpose |
|---|---|---|
| Auth | `/api/Auth` | User registration and login |
| Users | `/api/Users` | User information and profile management |
| Events | `/api/Events` | Race event management |
| Venues | `/api/Venues` | Venue management |
| Categories | `/api/Categories` | Event category management |
| Enrollments | `/api/Enrollments` | Participant enrolment management |
| Results | `/api/Results` | Race result management |

Protected endpoints require a valid JWT bearer token and, where applicable, the appropriate user role.

## 6. Azure Blob Storage

Azure Blob Storage is used to store uploaded event and profile images.

Event image uploads support JPG, PNG and WEBP formats, with a maximum file size of 5 MB.

The Azure Storage connection string is stored securely using .NET User Secrets during local development rather than being committed to GitHub.

## 7. Running the Application Locally

### Requirements

- Visual Studio with ASP.NET and web development support
- .NET 10 SDK
- SQL Server LocalDB
- Access to the configured Azure Storage account for image functionality
- Docker Desktop for container testing

### Setup

1. Clone the repository and switch to the `part-3` branch.
2. Open `RaceDay.API.slnx` in Visual Studio.
3. Restore the NuGet packages.
4. Configure the database connection string and required JWT settings.
5. Configure the Azure Blob Storage connection string using .NET User Secrets.
6. Ensure the existing database schema is available and the required migrations have been applied.
7. Run the RaceDay API.
8. Run the RaceDay MVC application.

The API is configured for local development using the following addresses:

- HTTPS: `https://localhost:7172`
- HTTP: `http://localhost:5151`

The MVC application communicates with the API using the `ApiSettings:BaseUrl` configuration value.

### Important Configuration

Sensitive configuration values, including JWT signing keys and Azure Storage connection strings, must not be committed to the repository.

Developers should configure their own local secrets and database connection settings.

## 8. Docker Containerisation

The RaceDay MVC application includes a Dockerfile.

To build the MVC Docker image, navigate to the `RaceDay.MVC` directory and run:

```powershell
docker build --load -t raceday-mvc .
```

Start the API on the host computer:

```powershell
dotnet run --urls "http://0.0.0.0:5151"
```

Then run the MVC container:

```powershell
docker run -d --name raceday-mvc-container -p 8080:8080 -e ApiSettings__BaseUrl=http://host.docker.internal:5151/ raceday-mvc
```

Open the containerised MVC application at:

`http://localhost:8080`

The API must be running and accessible for the MVC application to communicate with it.

## 9. Automated Testing

The project uses xUnit for automated API testing.

Tests include authentication and role-based authorisation checks.

The test suite uses an isolated testing environment with an in-memory database to avoid modifying the existing development database.

To run the tests from the solution directory:

```powershell
dotnet test RaceDay.API.slnx --configuration Release
```

At the time of the Part 3 verification, all 20 automated tests passed successfully.

## 10. Continuous Integration

GitHub Actions is configured to build and test the RaceDay solution automatically.

The workflow performs the following operations:

1. Checks out the repository.
2. Installs the .NET 10 SDK.
3. Restores project dependencies.
4. Builds the RaceDay API.
5. Builds the RaceDay MVC application.
6. Executes the automated tests.

The workflow runs on pushes and pull requests targeting the configured `main` and `part-3` branches.

The Part 3 GitHub Actions workflow was verified successfully.

## 11. Version Control

Git and GitHub are used to manage the source code.

The Part 3 development work is maintained on a dedicated branch named `part-3`.

The repository contains 22 commits at the time of the Part 3 verification, demonstrating incremental development and version control.

## 12. Conclusion

RaceDay demonstrates the development of a secure, database-driven race event management application using modern .NET technologies.

The system integrates an ASP.NET Core Web API with an MVC frontend, SQL Server, JWT authentication, Azure Blob Storage, Docker and GitHub Actions.

Automated testing and continuous integration support the reliability and maintainability of the application.

The project was developed as part of the PROG6212 assignment.

## youtube links
https://youtu.be/nFBYHoSyqIw?si=OXSQvLv4TkzDsm4t
https://youtube.com/shorts/RlFKaWcBStA?si=MKyVQ9sG_K1Givs1
