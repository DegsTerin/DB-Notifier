/** Module purpose: Implements vite config for the provider-neutral DB-Notifier Dashboard without direct database access. */
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
});
