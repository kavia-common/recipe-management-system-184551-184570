# Recipe Backend API

A minimal ASP.NET Core 8 API for recipe management with JWT-based authentication and in-memory storage.

- Port: 3001 (see Properties/launchSettings.json)
- Swagger UI: /docs (e.g., http://localhost:3001/docs)
- Health: GET /

## Auth

1) Register
- POST /api/auth/register
- Body: { "username": "demo2", "password": "password123" }
- 201 Created on success

2) Login
- POST /api/auth/login
- Body: { "username": "demo2", "password": "password123" }
- 200 OK: { "token": "...", "username": "demo2" }

Use "Authorization: Bearer {token}" for authenticated endpoints.

Seed user: demo / password123

## Recipes

- GET /api/recipes
  - Query: ?query=...&ingredient=...&tag=...&skip=0&take=20
  - Public

- GET /api/recipes/{id}
  - Public

- POST /api/recipes
  - Auth required
  - Body:
    {
      "title":"My Dish",
      "description":"Tasty",
      "ingredients":["Item1","Item2"],
      "tags":["tag1","tag2"]
    }

- PUT /api/recipes/{id}
  - Auth required
  - Same body as POST

- DELETE /api/recipes/{id}
  - Auth required

Notes:
- In-memory storage for demo purposes, easy to replace with DB later.
- Validation returns 400 with simple error object; 401 for unauthenticated; 404 for not found.
