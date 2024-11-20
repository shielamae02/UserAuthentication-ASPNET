# 🔒User Authentication API

## 📖 Overview

This project is a robust **User Authentication API** developed with A**ASP.NET Core Web API** and **.NET 8**, designed to streamline authentication and authorization processes. It provides essential features such as secure token-based authentication, automatic token refresh mechanisms, and comprehensive user management. With **MSSQL** as its database backend, the application ensures reliable data storage and retrieval. Additionally, the project is fully **Dockerized**, enabling effortless setup, deployment, and scalability in diverse environments.

---

## ✨ Features

- **Endpoints**:

  - `POST /login` - Authenticate a user and generate an access token and refresh token.
  - `POST /register` - Register a new user.
  - `POST /refresh-token` - Refresh the access token using a valid refresh token.
  - `POST /logout` - Revoke a user’s refresh token.
  - `POST /forgot-password` - Request a password reset email.
  - `POST /reset-password` - Reset a user’s password with a valid reset token.
  - `GET /me` - Retrieve the authenticated user’s details.

- **Core Functionality**:
  - **Authentication**: Secure login and token generation using JWT.
  - **Authorization**: Role-based access control for restricted endpoints.
  - **Token Refreshing**: Automatic refreshing of access tokens through the `/refresh-token` endpoint.
  - **Background Cleanup Service**: Periodic deletion of revoked and expired tokens.
  - **Docker Support**: Fully containerized for ease of setup and deployment using Docker Compose.

---

## 🛠️ Installation and Setup

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/)
- [Git](https://git-scm.com/)

### Clone the Repository

```bash
git clone https://github.com/shielamae02/UserAuthentication-ASPNET.git
cd UserAuthentication-ASPNET
```

### Configuration

1. Update the appsettings.json file with your MSSQL connection string:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=your-server;Database=your-database;User Id=your-username;Password=your-password;"
     }
   }
   ```
2. Review the docker-compose.yml files to ensure they match your requirements.

### Running the Application Locally

⚙️ Without Docker

1. Build and run the program
   ```bash
   dotnet build
   dotnet run
   ```

🐳 With Docker

1. Create a .env file in the root of your project and populate it with the following values, as shown in the env.sample file:

   ```
   # Base URL for constructing email redirection links (e.g., password reset).
   APPLICATION_URL=""

   # Database connection string.
   DEFAULT_CONNECTION=""

   # JWT Configuration
   JWT_SECRET=""                         # Secret key for signing JWTs.
   JWT_ISSUER=""                         # Issuer claim for JWTs.
   JWT_AUDIENCE=""                       # Audience claim for JWTs.
   JWT_ACCESS_TOKEN_EXPIRY=1             # Access token expiry (hours).
   JWT_REFRESH_TOKEN_EXPIRY=1            # Refresh token expiry (days).
   JWT_RESET_TOKEN_EXPIRY=10             # Reset token expiry (minutes).

   # SMTP Credentials for email sending.
   SMTP_USERNAME=""
   SMTP_PASSWORD=""
   ```

   Update the placeholders with your environment-specific values.

1. Build and run the Docker containers:

   ```bash
   docker-compose up --build
   ```

---

## 📩 Contact

For any questions or feedback, feel free to reach out:

- Email: shiela.mlepon@gmail.com
- GitHub: shielamae02
