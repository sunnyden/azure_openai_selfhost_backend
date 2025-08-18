# Azure OpenAI Selfhost Backend

Azure OpenAI Selfhost Backend is a .NET 8.0 ASP.NET Core Web API that provides a self-hosted proxy for Azure OpenAI services. It includes user management, model management, billing tracking, JWT authentication, and Model Context Protocol (MCP) support for connecting to AI assistants.

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Working Effectively

### Bootstrap, Build, and Test the Repository:
- `dotnet restore` -- takes ~30 seconds initially, ~2 seconds on subsequent runs. Downloads all NuGet packages and dependencies.
- `dotnet build` -- takes ~15 seconds initially, ~2 seconds on subsequent runs. Builds cleanly with NO warnings or errors. NEVER CANCEL.
- `cd OpenAISelfhost && dotnet publish --configuration Release --output ../output` -- takes ~5 seconds. Creates deployment-ready artifacts. Shows nullable reference type warnings (~40+ warnings) but completes successfully. NEVER CANCEL.

### Run the Application:
- **Development mode**: `cd OpenAISelfhost && dotnet run` -- starts on http://localhost:5131
- **Production mode**: `cd output && ./OpenAISelfhost` -- starts on http://localhost:5000
- Build warnings are expected and do NOT indicate errors - the application builds and runs successfully.

### Database Requirements:
- **CRITICAL**: Application requires MySQL 8.0+ database to function. All API endpoints will fail with database connection errors without it.
- Configure connection string in `OpenAISelfhost/appsettings.json` under `ConnectionStrings.DBConn`
- Default format: `"Server=<host>;Port=3306;Database=openai;User=<mysqluser>;Password=<password>;"`
- Database tables: `users`, `chat_model`, `transaction`, `user_model_assignment`

### JWT Configuration:
- Configure JWT settings in `OpenAISelfhost/appsettings.json` under `JWT` section
- Required fields: `ValidAudience`, `ValidIssuer`, `Secret`
- Most API endpoints require JWT authentication

## Validation

### Manual Testing Scenarios:
- **ALWAYS** validate that the application starts without errors: `dotnet run` should show "Now listening on: http://localhost:5131"
- Test basic connectivity: `curl -s -o /dev/null -w "%{http_code}" http://localhost:5131/` should return `404` (expected for root path)
- **Database connection test**: `curl -s http://localhost:5131/user/auth -X POST -H "Content-Type: application/json" -d '{"userName":"test","password":"test"}'` 
  - With database: Should return authentication response or user not found error
  - Without database: Returns MySQL connection error (expected if no DB configured)

### Build Quality Checks:
- `dotnet build` produces zero warnings and zero errors - any warnings or errors indicate a problem
- `dotnet publish` produces nullable reference type warnings (~40+ warnings) - this is expected and acceptable
- Zero compilation errors in both build and publish - any compilation error indicates a problem
- Published output in `./output/` directory contains ~40 files including the main executable

## Common Tasks

### API Endpoints Structure:
- `/user/auth` - User authentication (POST)
- `/user/create` - User registration (POST)  
- `/chat/completion` - Chat completions (POST, requires auth)
- `/model/*` - Model management (requires auth)
- `/transaction/*` - Billing/usage tracking (requires auth)
- `/mcp/transport` - WebSocket MCP transport endpoint

### Key Projects in Solution:
- **OpenAISelfhost** - Main ASP.NET Core Web API project
- **DataContracts** - Shared data models and contracts library

### Technology Stack Details:
- .NET 8.0 with ASP.NET Core Web API
- Entity Framework Core with MySQL (Pomelo.EntityFrameworkCore.MySql)
- Azure OpenAI SDK v2.2.0-beta.4
- Microsoft.Extensions.AI for AI abstractions
- Model Context Protocol (MCP) for AI assistant integration
- JWT Bearer authentication
- WebSocket support for real-time MCP communication

### Development Environment:
- **Required**: .NET 8.0 SDK (dotnet --version should show 8.0.x)
- **Required**: MySQL 8.0+ database server for full functionality
- **Recommended**: HTTP client (curl, Postman) for API testing
- **Test files**: `OpenAISelfhost/OpenAISelfhost.http` contains sample requests

### Common File Locations:
- Main application: `OpenAISelfhost/Program.cs`
- Controllers: `OpenAISelfhost/Controllers/`
- Services: `OpenAISelfhost/Service/`
- Data models: `DataContracts/DataTables/`
- Configuration: `OpenAISelfhost/appsettings.json`, `OpenAISelfhost/appsettings.Development.json`
- Project files: `OpenAISelfhost.sln`, `OpenAISelfhost/OpenAISelfhost.csproj`

### Build and Deploy Process:
1. **Local Development**: `dotnet restore && dotnet build && dotnet run`
2. **Production Build**: `dotnet publish --configuration Release --output ./output`
3. **CI/CD**: GitHub workflow in `.github/workflows/deploy-pipeline-master.yml` builds and deploys on push to master

## Timing and Performance

### Command Timing (Never Cancel):
- `dotnet restore`: ~30 seconds initially, ~2 seconds subsequently - NEVER CANCEL, downloads packages from NuGet
- `dotnet build`: ~15 seconds initially, ~2 seconds subsequently - NEVER CANCEL, compiles solution cleanly  
- `dotnet publish`: ~5 seconds - NEVER CANCEL, creates deployment artifacts with expected warnings
- `dotnet run`: ~5 seconds startup time - NEVER CANCEL during startup

### Application Architecture:
- **Service Layer**: Business logic in `OpenAISelfhost/Service/`
- **Data Layer**: Entity Framework models in `DataContracts/DataTables/`
- **API Layer**: REST controllers in `OpenAISelfhost/Controllers/`
- **Authentication**: JWT-based with custom middleware
- **AI Integration**: Azure OpenAI SDK + Microsoft.Extensions.AI abstractions
- **MCP Support**: Model Context Protocol for AI assistant communication

### Known Limitations:
- Application cannot function without MySQL database connection
- JWT configuration required for authenticated endpoints  
- Azure OpenAI credentials needed for AI functionality
- Publish warnings (~40+ nullable reference type warnings) are expected and safe to ignore