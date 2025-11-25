# Markly - Social Bookmarking Platform

A social bookmarking platform built with ASP.NET Core MVC, Entity Framework Core, and ASP.NET Identity.

## Features

- User registration and authentication
- Bookmark management (create, edit, delete)
- Categories and tags organization
- Voting and commenting system
- AI-powered tag and category suggestions
- Advanced search functionality
- User profiles with public/private categories

## Prerequisites

- .NET 9.0 SDK
- PostgreSQL database
- Anthropic API key (for AI suggestions)

## Configuration

### Database

Configure your PostgreSQL connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5434;Database=markly;Username=markly_user;Password=markly_password"
  }
}
```

### AI Suggestions (Anthropic Claude)

The application uses Claude 4.5 Haiku for AI-powered tag and category suggestions. To enable this feature:

1. **Get an API key** from [Anthropic Console](https://console.anthropic.com/)

2. **Configure the API key** using one of these methods:

   **Option A: User Secrets (Recommended for development)**
   ```bash
   dotnet user-secrets init
   dotnet user-secrets set "Anthropic:ApiKey" "your-api-key-here"
   ```

   **Option B: Environment Variable**
   ```bash
   export Anthropic__ApiKey="your-api-key-here"
   ```

   **Option C: appsettings.json (Not recommended for production)**
   ```json
   {
     "Anthropic": {
       "ApiKey": "your-api-key-here",
       "Model": "claude-haiku-4-5",
       "MaxTokens": 256
     }
   }
   ```

3. **Rate Limiting Configuration** (optional):
   ```json
   {
     "RateLimiting": {
       "MaxRequestsPerWindow": 10,
       "WindowSeconds": 60
     }
   }
   ```
   This limits each user to 10 AI suggestion requests per minute.

### Running the Application

1. **Apply database migrations:**
   ```bash
   dotnet ef database update
   ```

2. **Run the application:**
   ```bash
   dotnet run
   ```

3. **Access the application** at `https://localhost:5001` or `http://localhost:5000`

## Using AI Suggestions

1. Navigate to Create or Edit Bookmark page
2. Enter a title and optionally a description
3. Click the **"Suggest with AI"** button
4. Review the suggested tags and categories
5. Click on any suggestion to add it to your bookmark
6. New tags/categories will be created automatically if they don't exist

## Project Structure

```
markly/
├── Configuration/          # Settings classes (Anthropic, RateLimiting)
├── Controllers/            # MVC Controllers
├── Data/
│   └── Entities/           # Entity models
├── Helpers/                # Utility classes
├── Migrations/             # EF Core migrations
├── Models/                 # Domain models
├── Services/
│   ├── Interfaces/         # Service contracts
│   └── Implementations/    # Service implementations
├── ViewModels/             # View models
├── Views/                  # Razor views
└── wwwroot/                # Static files (CSS, JS)
```

## Technologies

- ASP.NET Core MVC 9.0
- Entity Framework Core
- PostgreSQL
- ASP.NET Identity
- Bootstrap 5
- Anthropic Claude API

## License

This project is for educational purposes.
