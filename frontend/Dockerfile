# WorkBase frontend (React/Vite) — build statyczny serwowany przez nginx.
# nginx.conf proxuje /api, /hubs (SignalR) i /health do workbase-api:5000.
FROM node:22-bookworm-slim AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
# Prod env: pusty API (względne /api → nginx proxy), Keycloak na subdomenie auth.
RUN printf 'VITE_API_URL=\nVITE_KEYCLOAK_AUTHORITY=https://auth.wb-partners.pl/realms/workbase\nVITE_KEYCLOAK_CLIENT_ID=workbase-web\nVITE_REDIRECT_URI=https://workbase.wb-partners.pl/auth/callback\nVITE_POST_LOGOUT_REDIRECT_URI=https://workbase.wb-partners.pl\n' > .env.production
RUN npm run build

FROM nginx:1.27-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
