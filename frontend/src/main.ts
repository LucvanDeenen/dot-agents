import { createApp } from "vue";
import "@/style/style.css";
import App from "./App.vue";

// Auth is not wired up yet: the API has no auth and no view guards on it, so
// the auth-client plugin was registered but unused. It also pulled in a
// private repo over git+ssh, which CI image builds can't clone — dropped
// until auth is actually implemented (see README "Out of scope").
createApp(App).mount("#app");
