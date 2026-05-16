import "@fontsource/inter/400.css";
import "@fontsource/inter/500.css";
import "@fontsource/inter/600.css";
import "@fontsource/inter/700.css";
import "@fontsource/inter/800.css";
import "bootstrap/dist/css/bootstrap.min.css";
import * as bootstrap from "bootstrap";
window.bootstrap = bootstrap;
import "@fortawesome/fontawesome-free/css/all.min.css";
import { transformDateToUnix, writeLogsInPage } from "./utility.js";
import { getDbSchema } from "./dbschema.js";
import { Connection } from "jsstore";

window.transformDateToUnix = transformDateToUnix;
window.getDbSchema = getDbSchema;
window.writeLogsInPage = writeLogsInPage;

const limitApi = 10;

import jsStoreWorker from "jsstore/dist/jsstore.worker.js?url";
window.jsstoreCon = new Connection(new Worker(jsStoreWorker));
window.draggedElement = null;
window.limitApi = limitApi;

if ("serviceWorker" in navigator) {
    window.addEventListener("load", () => {
        navigator.serviceWorker
            .register(import.meta.env.MODE === "production" ? "/service-worker.js" : "/dev-sw.js?dev-sw", {
                type: import.meta.env.MODE === "production" ? "classic" : "module",
            })
            .then((reg) => {
                writeLogsInPage("Service Worker registrato:", reg.scope);
                if (reg.installing) {
                    reg.installing.addEventListener("statechange", (event) => {
                        if (event.target.state === "installed" && reg.installing) {
                            reg.installing.postMessage({ type: "SKIP_WAITING" });
                        }
                    });
                } else if (reg.waiting) {
                    reg.waiting.postMessage({ type: "SKIP_WAITING" });
                }
            })
            .catch((err) => {
                console.error("Service Worker registration failed:", err);
                writeLogsInPage("Service Worker registration failed:", err);
            });
    });
}

// Initialize the application
function initializeApp() {
    import("./index.js")
        .then(() => {
            console.log("Application initialized");
        })
        .catch((err) => {
            console.error("Failed to initialize application:", err);
        });
}

// Add writeError function for compatibility
window.writeError = function (message) {
    console.error(message);
    writeLogsInPage("ERROR: " + message);
};

initializeApp();
