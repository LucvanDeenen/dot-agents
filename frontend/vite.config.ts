import { defineConfig, loadEnv } from "vite";
import vue from "@vitejs/plugin-vue";
import path from "path";

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd());

  return {
    plugins: [vue()],
    server: {
      port: 3000,
      proxy: {
        "/auth": {
          target: env.VITE_BACKEND_URL,
          changeOrigin: true,
          secure: false,
        },
        "/jobs": {
          target: env.VITE_AGENT_API_URL,
          changeOrigin: true,
          secure: false,
        },
        "/agents": {
          target: env.VITE_AGENT_API_URL,
          changeOrigin: true,
          secure: false,
        },
        "/skills": {
          target: env.VITE_AGENT_API_URL,
          changeOrigin: true,
          secure: false,
        },
        "/health": {
          target: env.VITE_AGENT_API_URL,
          changeOrigin: true,
          secure: false,
        },
      },
    },
    resolve: {
      alias: {
        "@": path.resolve(__dirname, "./src"),
      },
    },
    base: "./",
  };
});
