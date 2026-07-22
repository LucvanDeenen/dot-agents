# Frontend image: Vite build served by nginx, which also proxies the API
# routes to the api container (mirrors the dev-server proxy in vite.config.ts).
FROM --platform=$BUILDPLATFORM node:22-slim AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
# No VITE_* build args needed: the API is reached over same-origin paths that
# nginx proxies at runtime, so nothing environment-specific is baked in.
RUN npm run build

FROM nginx:alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
