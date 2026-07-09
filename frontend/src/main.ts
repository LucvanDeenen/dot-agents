import { createAuthPlugin } from "auth-client";
import { createApp } from "vue";
import "@/style/style.css";
import App from "./App.vue";

const auth = createAuthPlugin({
  currentSystem: "auth-portal",
  authFrontend: import.meta.env.VITE_FRONTEND_URL,
  authBackend: import.meta.env.VITE_BACKEND_URL,
  hmacSecret: ''
});
createApp(App).use(auth).mount("#app");
