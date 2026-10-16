# RaceDay API
## 1. Project Description
RaceDay API is a RESTful Web API developed using C# and ASP.NET Core (.NET 10). It manages race events, categories, participant enrollments, and race results.


### Main Features
-User registration and login.

-JWT authentication and role-based authorization.

-Event and category management.

-Participant enrollment management.

-Race result management.

-Automated testing and GitHub Actions CI

## 2. User Roles
### Participant

-Register and log in.

-View race events and categories.

-Enroll in available events.

-Access their profile and permitted enrollment information.

-View results where authorized.

### Organiser

-Log in using an organiser account.

-Create, view, update, and delete race events where authorized.

-Manage categories, enrollments, and results where permitted.

## 3. Technologies Used

-C# and ASP.NET Core (.NET 10)

-SQL Server and Entity Framework Core

-JWT Bearer Authentication

-Swagger / OpenAPI

-GitHub Actions

## 4. Setup Instructions

### Prerequisites

-Visual Studio

_.NET 10 SDK

_SQL Server LocalDB

### Clone the Repository

git clone https://github.com/M-Ndukwane/RaceDay_API_Part2.git
cd RaceDay_API_Part2

Open RaceDay.API.slnx in Visual Studio.

### Database Configuration

#### The development database uses:

-Server: (localdb)\MSSQLLocalDB

-Database: RaceDay

-Authentication: Windows Authentication

Ensure the database and required tables exist before running the API. Do not delete an existing database containing project data.

### Configure JWT

#### Configure these settings in your local environment:

-Jwt:Key

-Jwt:Issuer

-Jwt:Audience

Do not commit secret keys or passwords to GitHub.

## 5. Run the API

#### Run these commands from the repository root:

dotnet restore RaceDay.API.slnx

dotnet build RaceDay.API.slnx

dotnet run --project RaceDay.API/RaceDay.API.csproj

Open the Swagger URL displayed by the application, usually ending in /swagger.

## 6. Authentication

1.Register or use an existing account.

2.Log in to obtain a JWT access token.

3.Click Authorize in Swagger.

4.Enter the token in the required format, usually Bearer YOUR_ACCESS_TOKEN.

5.Test endpoints according to your role's permissions.

## 7. Run Tests

dotnet test RaceDay.API.slnx

GitHub Actions automatically builds and tests the project when changes are pushed to the configured branch or a pull request is opened.
