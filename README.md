Jana 0.8
Just Another Note Application.
===========================

This is a simple note-taking application built with a frontend using plain Javascript and a backend using .NET. It also includes a service worker for offline capabilities.

Why Jana?
--------
After reading Tiago Forte's book "Build a Second Brain", I decided to create a simple app to organize notes that could help me manage my ideas and knowledge more effectively.
Just folders and notes.
A single web application to manage everything on my computer and phone.
Data is stored locally in the browser to ensure privacy and offline availability. A private backend enables synchronization between devices.

Features
--------
- Create, read, update, and delete notes.
- Organize notes into folders.
- Offline access.
- Synchronization between devices via a private backend.

Technologies Features
--------
- Frontend: Plain Javascript, Vite, JSStore for local storage.
- Backend: .NET for API and data synchronization.
- Service Worker: Workbox for offline capabilities.


Running with Docker
--------

**Docker Compose — image from dockerhub (recommended):**

Copy `docker-compose.yml`, set a strong `JWT__JWTKey`, then run:
```bash
docker compose up -d
```
The SQLite database is persisted in `./data` on the host, mapped to `/app/db` inside the container.

**Docker Compose — build dai sorgenti:**

To build the image directly from source (requires Docker with BuildKit):
```bash
docker compose -f docker-compose-build.yml up -d --build
```
The Dockerfile performs a multi-stage build: compiles the .NET 8 backend, builds the frontend with Node 22, then assembles a minimal runtime image. The first build takes a few minutes; subsequent builds are faster thanks to layer caching.

**Docker CLI:**
```bash
docker run -d \
  --restart unless-stopped \
  -v /your/path/db:/app/db \
  -p 8080:8080 \
  -e JWT__JWTKey=your_long_random_secret \
  -e JWT__JWTIssuer=Jana \
  -e JWT__JTWAudience=Jana \
  --name jana \
  uomofocaccin/jana
```

The app will be available at `http://localhost:8080`.  
Default credentials: `admin` / `admin` — **change the password after first login**.

TODO / Roadmap
--------
- [ ] Admin section to manage users (create, disable, reset password)
- [ ] Icon-based context menu for actions: new note, new folder, edit, delete
- [ ] Favorites: mark notes/folders as favorites for quick access
- [ ] WebSocket support for real-time note updates across devices

Getting Started For Development
--------
1. Start the Backend:
   - Navigate to the backend directory: `cd backend`
   - Run the .NET application: `dotnet run`
2. Start the Frontend:
    - Navigate to the frontend directory: `cd frontend`
    - Install dependencies: `npm install`
    - Start the development server: `npm run dev`
3. Open your chrome browser and navigate to `http://localhost:5173` to access the application. (the debug work only with chrome)

