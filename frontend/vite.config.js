import { defineConfig } from "vite";
import { VitePWA } from "vite-plugin-pwa";

export default defineConfig(({ command }) => {
    const isDev = command === "serve";
    const API_BASE_URL = isDev ? "http://localhost:5062/api" : "/api";

    return {
        base: "/",
        define: {
            "import.meta.env.API_BASE_URL": JSON.stringify(API_BASE_URL),
        },
        server: {
            host: "0.0.0.0",
            port: 5173,
            strictPort: true
        },
        publicDir: "public",
        build: {
            rollupOptions: {
                input: "index.html",
            },
            copyPublicDir: true,
        },
        plugins: [
            VitePWA({
                registerType: "autoUpdate",
                strategies: "injectManifest",
                srcDir: "src",
                filename: "service-worker.js",
                injectRegister: null,
                manifest: false,
                devOptions: {
                    enabled: true,
                    type: "module",
                },
                injectManifest: {
                    globPatterns: ["**/*.{js,css,html,ico,png,svg}"],
                },
            }),
        ],
    };
});
