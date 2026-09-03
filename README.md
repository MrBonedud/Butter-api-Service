<a id="readme-top"></a>

<!-- SHIELDS -->

[![.NET][.NET]][.NET-url]
[![ASP.NET Core][ASP.NET Core]][ASP.NET Core-url]
[![C#][C#]][C#-url]
[![SQL Server][SQL Server]][SQL Server-url]
[![Entity Framework Core][Entity Framework Core]][Entity Framework Core-url]

<!-- PROJECT LOGO -->
<br />
<div align="center">

<h3 align="center">Butter API</h3>

  <p align="center">
    A real-time movie discovery API for collaborative watch-party rooms
    <br />
    <a href="#about-the-project"><strong>Explore the docs »</strong></a>
    <br />
    <br />
    <a href="#getting-started">Get Started</a>
    &middot;
    <a href="#api-endpoints">API Endpoints</a>
    &middot;
    <a href="#contributing">Report Issues</a>
  </p>
</div>

<!-- TABLE OF CONTENTS -->
<details>
  <summary>Table of Contents</summary>
  <ol>
    <li>
      <a href="#about-the-project">About The Project</a>
      <ul>
        <li><a href="#built-with">Built With</a></li>
        <li><a href="#key-features">Key Features</a></li>
      </ul>
    </li>
    <li>
      <a href="#getting-started">Getting Started</a>
      <ul>
        <li><a href="#prerequisites">Prerequisites</a></li>
        <li><a href="#installation">Installation</a></li>
        <li><a href="#environment-variables">Environment Variables</a></li>
      </ul>
    </li>
    <li><a href="#api-endpoints">API Endpoints</a></li>
    <li><a href="#database-schema">Database Schema</a></li>
    <li><a href="#architecture">Architecture</a></li>
    <li><a href="#deployment">Deployment</a></li>
    <li><a href="#contributing">Contributing</a></li>
    <li><a href="#license">License</a></li>
    <li><a href="#contact">Contact</a></li>
  </ol>
</details>

<!-- ABOUT THE PROJECT -->

## About The Project

Butter is a collaborative movie discovery experience. Users can create a room, invite other participants, browse movie candidates, and swipe together until the group finds a match.

The backend API provides authentication, room and participant management, movie discovery through TMDB, swipe tracking, match detection, and real-time room updates through SignalR.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

### Key Features

- **User Authentication**: Register and log in with JWT access tokens and rotating refresh tokens
- **Profile Management**: View and update the authenticated user's display name
- **Movie Rooms**: Create and join six-character rooms with configurable discovery settings
- **Guest Sessions**: Join rooms anonymously with process-local participant session tokens
- **Movie Discovery**: Search TMDB, view movie details and cast, and retrieve similar movies
- **Collaborative Swiping**: Record left/right votes and detect group matches
- **Real-Time Updates**: Broadcast participant presence, swiping state, swipe activity, and matches with SignalR
- **Room Expiration**: Close rooms after 30 minutes of inactivity or when the last connected guest leaves
- **SQL Server Persistence**: Store users, refresh tokens, rooms, and swipes with Entity Framework Core
- **Swagger Documentation**: Explore the API interactively in the Development environment

<p align="right">(<a href="#readme-top">back to top</a>)</p>

### Built With

- [![.NET][.NET]][.NET-url]
- [![ASP.NET Core][ASP.NET Core]][ASP.NET Core-url]
- [![C#][C#]][C#-url]
- [![SQL Server][SQL Server]][SQL Server-url]
- [![Entity Framework Core][Entity Framework Core]][Entity Framework Core-url]
- [![SignalR][SignalR]][SignalR-url]
- [![TMDB][TMDB]][TMDB-url]

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- GETTING STARTED -->

## Getting Started

### Prerequisites

- .NET 10 SDK
- SQL Server 2019+ or a compatible SQL Server instance
- A TMDB API key for movie search and room candidates
- Git for version control

### Installation

1. Clone the repository:

   ```sh
   git clone <repository-url>
   cd Butter-API
   ```

2. Restore dependencies:

   ```sh
   dotnet restore
   ```

3. Create a local settings file by copying the example below into `appsettings.Local.json` and replace the placeholder values:

   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost,1433;Database=ButterDb;User Id=sa;Password=your-password;Encrypt=True;TrustServerCertificate=True"
     },
     "Jwt": {
       "SecretKey": "your-secret-key-at-least-32-characters-long"
     },
     "Tmdb": {
       "ApiKey": "your-tmdb-api-key"
     }
   }
   ```

   `appsettings.Local.json` is ignored by Git and is loaded after the standard application settings.

4. Apply the Entity Framework Core migrations:

   ```sh
   dotnet ef database update --project Infrastructure --startup-project Butter-API
   ```

5. Start the development server:

   ```sh
   dotnet run --launch-profile https
   ```

The API is available at `https://localhost:7261`. The HTTP development profile uses `http://localhost:5038`. Swagger is available at `/swagger` while running in the Development environment.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

### Environment Variables

The project uses ASP.NET Core configuration. Keep secrets in `appsettings.Local.json`, user secrets, or the hosting platform's secret store rather than committing them to source control.

| Setting | Description |
| ------- | ----------- |
| `ConnectionStrings:DefaultConnection` | SQL Server connection string |
| `Jwt:Issuer` | JWT issuer; defaults to `Butter-API` in Development |
| `Jwt:Audience` | JWT audience; defaults to `Butter-API` in Development |
| `Jwt:SecretKey` | JWT signing key; use a strong key of at least 32 characters |
| `Jwt:AccessTokenMinutes` | Access-token lifetime in minutes; Development uses `15` |
| `Tmdb:BaseUrl` | TMDB API base URL; Development uses `https://api.themoviedb.org/3/` |
| `Tmdb:ApiKey` | TMDB API key |

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- API ENDPOINTS -->

## API Endpoints

### Authentication Routes (`/api/auth`)

| Method | Endpoint | Description | Auth Required |
| ------ | -------- | ----------- | ------------- |
| POST | `/api/auth/register` | Register a new user and receive tokens | No |
| POST | `/api/auth/login` | Authenticate a user and receive tokens | No |
| POST | `/api/auth/refresh-token` | Rotate a refresh token | No |
| GET | `/api/auth/admin-test` | Verify access to an admin-only endpoint | Admin role |

### Profile Routes (`/api/profile`)

| Method | Endpoint | Description | Auth Required |
| ------ | -------- | ----------- | ------------- |
| GET | `/api/profile/me` | Get the current user's profile | Yes |
| PUT | `/api/profile/me` | Update the current user's display name | Yes |

### Room Routes (`/api/rooms`)

| Method | Endpoint | Description | Auth Required |
| ------ | -------- | ----------- | ------------- |
| POST | `/api/rooms` | Create a movie discovery room | Yes |
| PATCH | `/api/rooms/{code}/settings` | Update room settings as the creator | Yes (Creator) |
| GET | `/api/rooms/{code}` | Get room details and connected guests | No |
| POST | `/api/rooms/{code}/join` | Join a room as a guest | No |
| POST | `/api/rooms/{code}/start-swiping` | Start the room's swipe session | Yes (Creator) |
| GET | `/api/rooms/{code}/candidates` | Get filtered movie candidates for a room | No |

Room settings include genre, maximum runtime, and starting decade. Room codes are six-character uppercase alphanumeric values.

### Swipe Routes (`/api/rooms/{code}/swipes`)

| Method | Endpoint | Description | Auth Required |
| ------ | -------- | ----------- | ------------- |
| POST | `/api/rooms/{code}/swipes` | Record a left or right swipe | Guest session |
| GET | `/api/rooms/{code}/swipes/candidates` | Get swipe candidates for a participant | Guest session |

Swipe requests contain `ParticipantId`, `SessionToken`, `TmdbMovieId`, and `Direction` (`Left` or `Right`). When all connected participants vote right on the current movie, the room is marked as matched.

### Movie Routes (`/api/movies`)

| Method | Endpoint | Description |
| ------ | -------- | ----------- |
| GET | `/api/movies/search?query=...&page=...` | Search TMDB movies |
| GET | `/api/movies/{movieId}` | Get movie details |
| GET | `/api/movies/{movieId}/cast` | Get up to 15 cast members |
| GET | `/api/movies/{movieId}/similar?page=...` | Get similar movies |

### SignalR Hub (`/hubs/room`)

Clients connect to `/hubs/room` and call `JoinRoom(roomCode, participantId, sessionToken)`.

| Event | Description |
| ----- | ----------- |
| `RoomPresence` | Sends the current room presence |
| `ParticipantJoined` | Announces a participant joining |
| `ParticipantLeft` | Announces a participant leaving |
| `SwipingStarted` | Announces that swiping has started |
| `SwipeRecorded` | Announces a recorded swipe |
| `MovieMatched` | Announces a room match |

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- DATABASE SCHEMA -->

## Database Schema

### User

Represents a registered Butter account.

- `Id`: User identifier
- `Email`: Unique email address, maximum 256 characters
- `DisplayName`: Display name, maximum 120 characters
- `Role`: User role, such as `User` or `Admin`
- `CreatedAt`: Account creation timestamp

### RefreshToken

Stores refresh tokens issued to users.

- Token values are unique and generated from cryptographically secure random bytes
- Tokens expire after seven days
- Refresh-token rotation revokes the previous token
- Tokens are cascade-deleted with their user

### Room

Represents a collaborative movie discovery room.

- `Id`: Room identifier
- `Code`: Unique six-character room code
- `CreatorId`: Registered user who created the room
- `Status`: `Waiting`, `Swiping`, `Matched`, or `Closed`
- `GenreId`: Optional TMDB genre filter
- `MaxRuntimeMinutes`: Optional runtime filter from 1 to 600 minutes
- `DecadeStart`: Optional decade filter from 1900 to 2100
- `CreatedAt`: Room creation timestamp
- `LastActivityAt`: Last room activity timestamp
- `CurrentMovieId`: Movie currently being voted on

### Swipe

Represents a participant's vote for a movie.

- `Id`: Swipe identifier
- `RoomId`: Related room
- `ParticipantId`: In-room participant identifier
- `TmdbMovieId`: TMDB movie identifier
- `Direction`: `Left` or `Right`
- `CreatedAt`: Swipe creation timestamp
- Unique constraint on `RoomId`, `ParticipantId`, and `TmdbMovieId`

Guests and their session tokens are held in process memory rather than persisted in SQL Server. They are lost when the application restarts and are not shared across multiple application instances.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- ARCHITECTURE -->

## Architecture

### Project Structure

```
Butter-API/
├── Program.cs                         # ASP.NET Core startup and service registration
├── Controllers/
│   ├── AuthController.cs              # Registration, login, and token refresh
│   ├── MoviesController.cs             # TMDB movie endpoints
│   ├── ProfileController.cs            # Current-user profile endpoints
│   ├── RoomsController.cs              # Room lifecycle and settings
│   └── SwipesController.cs             # Collaborative swipe endpoints
├── Hubs/
│   └── RoomHub.cs                      # SignalR room presence and events
├── Core/
│   ├── Common/                         # JWT and TMDB option models
│   ├── DTOs/                           # Request and response contracts
│   ├── Entities/                       # Domain entities and enums
│   ├── Interfaces/                     # Application service contracts
│   └── Models/                         # Supporting domain models
├── Infrastructure/
│   ├── Data/                           # EF Core DbContext and migrations
│   ├── External/                       # TMDB integration
│   ├── Identity/                       # Authentication and token services
│   ├── Repositories/                   # Data-access implementations
│   └── Rooms/                          # Room, swipe, and guest-session services
├── Properties/
│   └── launchSettings.json             # Local HTTP and HTTPS profiles
├── Butter-API.csproj
├── Butter-API.slnx
└── README.md
```

### Authentication Flow

1. A user registers or logs in with an email address and password.
2. The API hashes and validates passwords with ASP.NET Identity's password hasher.
3. The API returns a JWT access token and a seven-day refresh token.
4. The client sends the access token as a Bearer token on protected requests.
5. The API validates the JWT issuer, audience, signature, lifetime, and role claims.
6. The client can exchange a valid refresh token for a new access/refresh-token pair.

### Room Flow

1. An authenticated user creates a room and becomes its creator.
2. Other users join with the room code and receive a guest participant session.
3. The creator configures filters and starts the swiping session.
4. Participants receive movie candidates sourced from TMDB and submit left/right swipes.
5. When all connected participants vote right on the current movie, the room becomes matched.
6. SignalR broadcasts presence, swipe, and match events to connected clients.

### Data and Service Boundaries

```
Client
  ├── REST API ──> Controllers ──> Core interfaces ──> Infrastructure services
  ├── SignalR ──> RoomHub ──> In-memory guest sessions
  └── Movie requests ──> TmdbService ──> TMDB API

Infrastructure services ──> Repositories ──> EF Core ──> SQL Server
```

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- DEPLOYMENT -->

## Deployment

### Production Requirements

- .NET 10 runtime
- Managed or self-hosted SQL Server
- TMDB API key stored in the hosting platform's secret manager
- Strong JWT signing key stored outside source control
- HTTPS termination and a configured frontend origin
- Shared or durable guest-session storage if running more than one API instance

### Build and Run

```sh
dotnet restore
dotnet build --configuration Release
dotnet ef database update --project Infrastructure --startup-project Butter-API
dotnet run --configuration Release
```

The application does not automatically apply migrations at startup. Apply migrations explicitly before serving traffic. No Dockerfile, CI/CD pipeline, or hosting-provider configuration is currently included in the repository.

### Operational Considerations

- Configure CORS for the deployed frontend instead of relying on the development origins.
- Use a shared backplane and distributed guest-session store for multi-instance SignalR deployments.
- Protect and rotate TMDB and JWT credentials.
- Configure database backups before production migration deployments.
- Add centralized exception handling, health checks, and automated tests before production use.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- DEVELOPMENT SCRIPTS -->

## Development Commands

```sh
# Restore NuGet dependencies
dotnet restore

# Build the solution
dotnet build

# Run the API with the HTTPS Development profile
dotnet run --launch-profile https

# Apply EF Core migrations
dotnet ef database update --project Infrastructure --startup-project Butter-API

# Create a migration after changing the data model
dotnet ef migrations add <MigrationName> --project Infrastructure --startup-project Butter-API

# Open the Swagger UI while running in Development
# https://localhost:7261/swagger
```

The repository currently does not contain an automated test project.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- CONTRIBUTING -->

## Contributing

Contributions are welcome! Here's how to get started:

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/AmazingFeature`
3. Follow the existing C# and ASP.NET Core conventions
4. Add or update EF Core migrations when changing persisted entities
5. Verify the API builds before submitting a pull request
6. Open a Pull Request

### Development Guidelines

- Keep domain contracts in `Core` and infrastructure implementations in `Infrastructure`
- Use DTOs for API request and response contracts
- Protect authenticated endpoints with the existing JWT authorization scheme
- Do not commit `appsettings.Local.json` or other secrets
- Keep API responses and validation behavior consistent with neighboring controllers
- Document new endpoints and configuration requirements in this README

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- LICENSE -->

## License

No license file is currently included in the repository. Add a license before distributing the project publicly.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- CONTACT -->

## Contact

For issues, questions, or suggestions:

- Open an issue in the project repository
- Start a discussion for feature requests

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- ACKNOWLEDGMENTS -->

## Acknowledgments

- ASP.NET Core and .NET community for the web framework
- Entity Framework Core for SQL Server persistence
- SignalR for real-time room communication
- TMDB for movie metadata and discovery data

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- MARKDOWN LINKS & IMAGES -->

[.NET]: https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white
[.NET-url]: https://dotnet.microsoft.com/
[ASP.NET Core]: https://img.shields.io/badge/ASP.NET%20Core-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white
[ASP.NET Core-url]: https://learn.microsoft.com/aspnet/core/
[C#]: https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white
[C#-url]: https://learn.microsoft.com/dotnet/csharp/
[SQL Server]: https://img.shields.io/badge/SQL%20Server-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white
[SQL Server-url]: https://www.microsoft.com/sql-server
[Entity Framework Core]: https://img.shields.io/badge/Entity%20Framework%20Core-512BD4?style=for-the-badge&logo=dotnet&logoColor=white
[Entity Framework Core-url]: https://learn.microsoft.com/ef/core/
[SignalR]: https://img.shields.io/badge/SignalR-0078D4?style=for-the-badge&logo=microsoft&logoColor=white
[SignalR-url]: https://learn.microsoft.com/aspnet/core/signalr/
[TMDB]: https://img.shields.io/badge/TMDB-01B4E4?style=for-the-badge&logo=themoviedatabase&logoColor=white
[TMDB-url]: https://www.themoviedb.org/
