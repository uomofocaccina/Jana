import { transformDateToUnix, getDbSchema, api } from "./sw-modules.js";
import { Connection } from "jsstore";
import workerInjector from "jsstore/dist/worker_injector";
import { precacheAndRoute } from "workbox-precaching";

precacheAndRoute(self.__WB_MANIFEST);

const schema = getDbSchema(transformDateToUnix);

const jsstoreCon = new Connection();

jsstoreCon.addPlugin(workerInjector);

// Initialize JSStore
async function initDb() {
    try {
        const isCreated = await jsstoreCon.initDb(schema);
        if (isCreated) {
            await writeLogsInHomePage("DB created from service worker");
        } else {
            await writeLogsInHomePage("DB already present from service worker");
        }
    } catch (error) {
        console.error("JSStore initialization failed:", error);
        await writeLogsInHomePage("JSStore error: " + error.message);
    }
}

// Call initialization
initDb();

let userData = { isAdmin: false, isGuest: false, token: null, username: null, tokenExpiry: null, timestamp: null };

self.addEventListener("install", (event) => {
    event.waitUntil(
        (async () => {
            const cache = await caches.open("cache");

            await cache.addAll([
                //'/offline',
            ]);
            //await readDbUserData();
            self.skipWaiting();
        })()
    );
});

self.addEventListener("activate", async (event) => {
    event.waitUntil(
        (async () => {
            await self.clients.claim();
            await jsstoreCon.initDb(schema).then(async (isCreated) => {
                if (isCreated) {
                    await writeLogsInHomePage("DB created from service worker");
                } else {
                    await writeLogsInHomePage("DB already present from service worker");
                }
            });
            await writeLogsInHomePage("reading user data from db");
            //await readDbUserData();
            await workSyncRetry();
        })()
    );
});

self.addEventListener("fetch", (event) => {
    if (event.request.method !== "GET" || !event.request.url.includes("api")) {
        event.respondWith(fetch(event.request));
        return;
    }

    // Otherwise we respond with the result of the async function.
    event.respondWith(
        (async () => {
            // First we open the cache in this event.
            const cache = await caches.open("cache");

            // We check if the element already exists in the cache.
            const cached = await caches.match(event.request, { ignoreSearch: true });
            await writeLogsInHomePage("cache for request:", event.request.url, "cached:", cached);
            const controller = new AbortController();

            const timeout = setTimeout(() => controller.abort(), cached ? 2000 : 6000);

            try {
                // We perform the network request.
                const response = await fetch(event.request, { signal: controller.signal });

                // The request was valid, we cancel the timer.
                clearTimeout(timeout);

                /**
                 * If the response is successful, we clone and cache it.
                 *
                 * The next time the offline user accesses this element they will be able to
                 * view it.
                 */
                if (response.status === 200) {
                    await cache.put(event.request, response.clone());
                }

                /**
                 * We respond with the result of the fetch whether it is 200 or any
                 * other result (30x, 40x...).
                 */
                return response;
            } catch (error) {
                /**
                 * If the network request was invalid we will end up here.
                 *
                 * This may be, for example, because there has been an error on
                 * the server (50x), or simply because the timer passed
                 * without getting a network response, and the controller aborted
                 * the network request so as not to wait any longer.
                 *
                 * If the error comes from the server, we still have the timer active
                 * so we must cancel it.
                 */
                if (error.name !== "AbortError") {
                    clearTimeout(timeout);
                }

                // Display the cached version if it exists, or the /offline page.
                return cached;
            }
        })()
    );
});

self.addEventListener("message", async (event) => {

    if (event.data && event.data.type === "SKIP_WAITING") {
        self.skipWaiting();
    }

    //check message for user actions
    //login_request
    if (event.data && event.data.type === "login_request") {
        let result = await login(event.data.obj.username, event.data.obj.password);
        await writeLogsInHomePage("login result:", result);
        if (result) {
            event.source.postMessage({ type: "login_ok", message: "ok" });
            event.source.postMessage({ type: "userData_response", result: userData });
        } else {
            event.source.postMessage({ type: "login_error", message: "Authentication failed" });
        }
        return;
    }

    //guest_request
    if (event.data && event.data.type === "guest_request") {
        userData = { isAdmin: false, isGuest: true, token: null, username: "Guest", tokenExpiry: null, timestamp: transformDateToUnix() };
        event.source.postMessage({ type: "guest_ok", message: "Guest mode on" });
        await setDbUserData();
        event.source.postMessage({ type: "userData_response", result: userData });
        return;
    }

    //logout_request
    if (event.data && event.data.type === "logout_request") {
        userData = { isAdmin: false, isGuest: false, token: null, username: "logout", tokenExpiry: null, timestamp: transformDateToUnix() };
        event.source.postMessage({ type: "login_need", message: "Auth required" });
        await setDbUserData();
        return;
    }

    if (event.data.type === "db_zappa_request") {
        let result = await db_zappa_request();
        event.source.postMessage({ type: event.data.type + "_response", success: result });
    }

    //main actions, if guest is false make check and net action

    if (userData.token == null && !userData.isGuest) {
        await readDbUserData();
        if (!checkValidAuthToken()) {
            await writeLogsInHomePage("Invalid token, login required.");
            event.source.postMessage({ type: "login_need", message: "Auth required" });
            return;
        } else {
            event.source.postMessage({ type: "userData_response", result: userData });
        }
    }

    //userinfo_request
    if (event.data && event.data.type === "userData_request") {
        event.source.postMessage({ type: "userData_response", result: userData });
        return;
    }

    if (userData.isGuest) {
        event.source.postMessage({ type: "userData_response", result: userData });
        //event.source.postMessage({ type: "guest_mode_on", success: true });
        return;
    }

    if (event.data.type && event.data.type.includes("sync")) {
        await workSyncRetry();
    }

    if (event.data.type === "syncDataFolder") {
        let result = await syncDataFolder(event.data.obj.limit, userData.token);
        event.source.postMessage({ type: event.data.type + "_response", success: result });
    }
    if (event.data.type === "syncDataNote") {
        let result = await syncDataNote(event.data.obj.limit, userData.token);
        event.source.postMessage({ type: event.data.type + "_response", success: result });
    }
    if (event.data.type === "syncDataNoteAndFolder") {
        let resultFolder = await syncDataFolder(event.data.obj.limit, userData.token);
        let resultNote = await syncDataNote(event.data.obj.limit, userData.token);
        let result = { folder: resultFolder, note: resultNote };
        event.source.postMessage({ type: event.data.type + "_response", result: result });
        event.source.postMessage({ type: "userData_response", result: userData });
    }
    //folder
    if (event.data.type === "syncAddFolder") {
        let result = await syncAddFolder(event.data.obj, userData.token);
        event.source.postMessage({ type: event.data.type + "_response", success: result });
        event.source.postMessage({ type: "sync_response", success: result });
        if (!result) {
            logDataForSyncRetry(event.data.type, event.data.obj, userData.token);
        }
    }
    if (event.data.type === "syncEditFolder") {
        let result = await syncEditFolder(event.data.obj, userData.token);
        event.source.postMessage({ type: event.data.type + "_response", success: result });
        event.source.postMessage({ type: "sync_response", success: result });
        if (!result) {
            logDataForSyncRetry(event.data.type, event.data.obj, userData.token);
        }
    }
    if (event.data.type === "syncDeleteFolder") {
        let result = await syncDeleteFolder(event.data.obj.item, userData.token);
        event.source.postMessage({ type: event.data.type + "_response", success: result });
        event.source.postMessage({ type: "sync_response", success: result });
        if (!result) {
            logDataForSyncRetry(event.data.type, event.data.obj, userData.token);
        }
    }
    if (event.data.type === "syncChangeFolderParent") {
        let result = await syncChangeFolderParent(event.data.obj, userData.token);
        event.source.postMessage({ type: event.data.type + "_response", success: result });
        event.source.postMessage({ type: "sync_response", success: result });
        if (!result) {
            logDataForSyncRetry(event.data.type, event.data.obj, userData.token);
        }
    }
    //note
    if (event.data.type === "syncAddNote") {
        let result = await syncAddNote(event.data.obj, userData.token);
        event.source.postMessage({ type: event.data.type + "_response", success: result });
        event.source.postMessage({ type: "sync_response", success: result });
        if (!result) {
            logDataForSyncRetry(event.data.type, event.data.obj, userData.token);
        }
    }
    if (event.data.type === "syncEditNote") {
        let result = await syncEditNote(event.data.obj, userData.token);
        event.source.postMessage({ type: event.data.type + "_response", success: result });
        event.source.postMessage({ type: "sync_response", success: result });
        if (!result) {
            logDataForSyncRetry(event.data.type, event.data.obj, userData.token);
        }
    }
    if (event.data.type === "syncDeleteNote") {
        let result = await syncDeleteNote(event.data.obj, userData.token);
        event.source.postMessage({ type: event.data.type + "_response", success: result });
        event.source.postMessage({ type: "sync_response", success: result });
        if (!result) {
            logDataForSyncRetry(event.data.type, event.data.obj, userData.token);
        }
    }
    if (event.data.type === "syncChangeFolderNote") {
        let result = await syncChangeFolderNote(event.data.obj, userData.token);
        event.source.postMessage({ type: event.data.type + "_response", success: result });
        event.source.postMessage({ type: "sync_response", success: result });
        if (!result) {
            logDataForSyncRetry(event.data.type, event.data.obj, userData.token);
        }
    }
    if (event.data.type === "workSyncRetry") {
        await workSyncRetry();
    }
    if (event.data.type === "changePassword_request") {
        let oldPassword = event.data.obj.oldPassword;
        let newPassword = event.data.obj.newPassword;
        let result = await changePassword(oldPassword, newPassword, userData.token);
        event.source.postMessage({ type: "changePassword_response", result: result });
    }
});

//section receved from message
async function db_zappa_request() {
    try {
        await jsstoreCon.remove({
            from: "SyncState",
        });
        await jsstoreCon.remove({
            from: "Folder",
        });
        await jsstoreCon.remove({
            from: "Note",
        });
        return true;
    } catch (error) {
        await writeLogsInHomePage("Error during db_zappa_request:", error.message);
        return false;
    }
}

async function syncDataFolder(limit, token) {
    let result = -1;
    let lastSyncDir = await readDbSyncState("Folder");

    try {
        let offset = 0;
        let countRecord = 100;
        while (countRecord > offset) {
            if (offset > 0) {
                await postMessageWithoutEvent("syncNewData_response", { success: result });
            }
            const resultGetDir = await api.getFolder(token, lastSyncDir, offset, limit);
            if (checkApiReturn401NeedLogin(resultGetDir)) {
                return -1;
            }
            if (resultGetDir.success) {
                countRecord = resultGetDir.count;
                result = resultGetDir.count;
                //await writeLogsInHomePage("Total folders:", resultGetDir.count);
                //await writeLogsInHomePage("folders get:", resultGetDir.data);
                if (resultGetDir.count > 0) {
                    await insertDb(resultGetDir.data, "Folder");
                    //await writeLogsInHomePage("Folder inserted in db");
                }
            } else {
                //something go wrong.
                countRecord = 0;
                result = -1;
            }
            offset += limit;
        }
    } catch (error) {
        await writeLogsInHomePage("Error getting folders:", error.message);
        result = -1;
    }

    if (result >= 0) await updateDbSyncState("Folder");

    return result;
}

async function syncDataNote(limit, token) {
    let result = -1;
    let lastSyncNote = await readDbSyncState("Note");

    try {
        let offset = 0;
        let countRecord = 100;
        while (countRecord > offset) {
            if (offset > 0) {
                await postMessageWithoutEvent("syncNewData_response", { success: result });
            }
            const resultGetNotes = await api.getNotes(token, lastSyncNote, offset, limit);
            if (checkApiReturn401NeedLogin(resultGetNotes)) {
                return -1;
            }
            if (resultGetNotes.success) {
                countRecord = resultGetNotes.count;
                result = resultGetNotes.count;
                //await writeLogsInHomePage("Total notes:", resultGetNotes.count);
                //await writeLogsInHomePage("Notes get:", resultGetNotes.data);
                if (resultGetNotes.count > 0) {
                    await insertDb(resultGetNotes.data, "Note");
                    //await writeLogsInHomePage("Notes inserted in db.");
                }
            } else {
                countRecord = 0;
                result = -1;
            }
            offset += limit;
        }
    } catch (error) {
        await writeLogsInHomePage("Error getting notes:", error.message);
        //await writeLogsInHomePageWithEvent(event, "Error getting notes:", error.message);
        result = -1;
    }

    if (result >= 0) await updateDbSyncState("Note");

    return result;
}

//folder
async function syncAddFolder(obj, token) {
    try {
        await syncDataFolder(10, token);
        const timestampServer = await api.createFolder(token, obj);
        if (checkApiReturn401NeedLogin(timestampServer)) {
            return false;
        }
        if (timestampServer) {
            await updateDbSyncState("Folder", timestampServer);
            return true;
        } else {
            return false;
        }
    } catch (error) {
        await writeLogsInHomePage("Error sync add folder:", error.message);
        return false;
    }
}

async function syncEditFolder(obj, token) {
    try {
        await syncDataFolder(10, token);
        const timestampServer = await api.updateFolder(token, obj);
        if (checkApiReturn401NeedLogin(timestampServer)) {
            return false;
        }
        await updateDbSyncState("Folder", timestampServer);
        return true;
    } catch (error) {
        await writeLogsInHomePage("Error sync edit folder:", error.message);
        return false;
    }
}

async function syncDeleteFolder(obj, token) {
    try {
        await syncDataFolder(10, token);
        const timestampServer = await api.deleteFolder(token, obj);
        if (checkApiReturn401NeedLogin(timestampServer)) {
            return false;
        }
        await updateDbSyncState("Folder", timestampServer);
        return true;
    } catch (error) {
        await writeLogsInHomePage("Error sync delete folder:", error.message);
        return false;
    }
}

async function syncChangeFolderParent(obj, token) {
    try {
        await syncDataFolder(10, token);
        const timestampServer = await api.changeParentFolder(token, obj.item, obj.parent);
        if (checkApiReturn401NeedLogin(timestampServer)) {
            return false;
        }
        await updateDbSyncState("Folder", timestampServer);
        return true;
    } catch (error) {
        await writeLogsInHomePage("Error sync edit parent folder for this:", error.message);
        return false;
    }
}

//Note

async function syncAddNote(obj, token) {
    try {
        await syncDataNote(10, token);
        const timestampServer = await api.createNote(token, obj);
        if (checkApiReturn401NeedLogin(timestampServer)) {
            return false;
        }
        await updateDbSyncState("Note", timestampServer);
        return true;
    } catch (error) {
        await writeLogsInHomePage("Error sync add note:", error.message);
        return false;
    }
}

async function syncEditNote(obj, token) {
    try {
        await syncDataNote(10, token);
        const timestampServer = await api.updateNote(token, obj);
        if (checkApiReturn401NeedLogin(timestampServer)) {
            return false;
        }
        await updateDbSyncState("Note", timestampServer);
        return true;
    } catch (error) {
        await writeLogsInHomePage("Error sync edit note:", error.message);
        return false;
    }
}

async function syncDeleteNote(obj, token) {
    try {
        await syncDataNote(10, token);
        const timestampServer = await api.deleteNote(token, obj.item);
        if (checkApiReturn401NeedLogin(timestampServer)) {
            return false;
        }
        await updateDbSyncState("Note", timestampServer);
        return true;
    } catch (error) {
        await writeLogsInHomePage("Error sync delete note:", error.message);
        return false;
    }
}

async function syncChangeFolderNote(obj, token) {
    try {
        await syncDataNote(10, token);
        const timestampServer = await api.changeFolderNote(token, obj.item, obj.parent);
        if (checkApiReturn401NeedLogin(timestampServer)) {
            return false;
        }
        await updateDbSyncState("Note", timestampServer);
        return true;
    } catch (error) {
        await writeLogsInHomePage("Error sync move note:", error.message);
        return false;
    }
}
//auth
async function login(username, password) {
    let result = await api.login(username, password);
    if (result.success) {
        userData.token = result.token;
        userData.username = username;
        userData.tokenExpiry = calculateTokenExpiry(result.token);
        userData.isGuest = false;
        userData.timestamp = result.timestamp;
        userData.isAdmin = result.admin;
        await setDbUserData();
        return true;
    } else {
        return false;
    }
}
async function changePassword(oldPassword, newPassword, token) {
    try {
        let result = await api.changePassword(token, oldPassword, newPassword);
        if (checkApiReturn401NeedLogin(result)) {
            return false;
        }
        return result;
    } catch (error) {
        await writeLogsInHomePage("Error in change password:", error.message);
        return false;
    }
}
//authHelper
function checkValidAuthToken() {
    if (userData.isGuest) {
        return true;
    }
    if (!userData.token) {
        return false;
    }
    if (userData.tokenExpiry && userData.tokenExpiry > Date.now()) {
        return true;
    }
    return false;
}
function checkApiReturn401NeedLogin(data) {
    if (data && data.code && data.code === 401) {
        postMessageWithoutEvent("login_need", { message: "Auth required" });
        return true;
    }
    return false;
}
function calculateTokenExpiry(token) {
    //I have a JWT token from which to retrieve the expiry
    if (!token) return null;
    let parts = token.split(".");
    if (parts.length !== 3) return null;
    let payload = parts[1];
    try {
        let decoded = atob(payload);
        let obj = JSON.parse(decoded);
        if (obj.exp) {
            return obj.exp * 1000;
        }
    } catch (e) {
        // Malformed token or payload
        return null;
    }
}
//db
async function setDbUserData() {
    try {
        await jsstoreCon.remove({
            from: "Auth",
        });
        await insertDb([userData], "Auth");
    } catch (error) {
        await writeLogsInHomePage("Error set DbUserData:", error.message);
        return false;
    }
}

async function readDbUserData() {
    try {
        var userDataDb = await jsstoreCon.select({
            from: "Auth",
        });
        if (userDataDb.length != 0) {
            userData.token = userDataDb[0].token;
            userData.username = userDataDb[0].username;
            userData.tokenExpiry = userDataDb[0].tokenExpiry;
            userData.isGuest = userDataDb[0].isGuest;
            userData.isAdmin = userDataDb[0].isAdmin;
        }
        //await writeLogsInHomePage("debug userdata:", userDataDb);
    } catch (error) {
        await writeLogsInHomePage("Error read readDbUserData:", error.message);
        return false;
    }
}

async function readDbSyncState(type) {
    let lastSync = transformDateToUnix("2020-01-01");
    try {
        var lastSyncDb = await jsstoreCon.select({
            from: "SyncState",
            where: {
                id: type,
            },
        });

        if (lastSyncDb.length != 0) {
            lastSync = lastSyncDb[0].timestamp;
        }
    } catch (error) {
        await writeLogsInHomePage("Error read SyncState:", error.message);
        return false;
    }

    return lastSync;
}

async function insertDb(data, type) {
    try {
        await jsstoreCon.insert({
            into: type,
            upsert: true,
            values: data,
        });
        return true;
    } catch (error) {
        await writeLogsInHomePage("Error updating the database:", error.message);
        return false;
    }
}

async function updateDbSyncState(type, timestamp) {
    if (!timestamp) timestamp = transformDateToUnix();

    await jsstoreCon.insert({
        into: "SyncState",
        upsert: true,
        values: [{ id: type, timestamp: timestamp }],
    });
}

async function logDataForSyncRetry(method, data, token) {
    try {
        await jsstoreCon.insert({
            into: "SyncRetry",
            upsert: true,
            values: [{ method: method, data: data, token: token }],
        });
        await writeLogsInHomePage("Log for sync retry inserted successfully.");
    } catch (error) {
        await writeLogsInHomePage("Error inserting log for sync retry:", error.message);
    }
}
async function getSyncRetryLogs() {
    try {
        const logs = await jsstoreCon.select({
            from: "SyncRetry",
            order: {
                by: "id",
            },
        });
        return logs;
    } catch (error) {
        await writeLogsInHomePage("Error getting sync retry logs:", error.message);
        return [];
    }
}

const syncMethodsRetry = {
    syncAddFolder,
    syncEditFolder,
    syncDeleteFolder,
    syncChangeFolderParent,
    syncAddNote,
    syncEditNote,
    syncDeleteNote,
    syncChangeFolderNote,
};

async function workSyncRetry() {
    try {
        const logs = await getSyncRetryLogs();
        if (logs.length > 0) {
            await writeLogsInHomePage("Sync retry logs found:", logs);
            for (const log of logs) {
                await writeLogsInHomePage(`Retrying method: ${log.method} with data:`, log.data);
                try {
                    let data = log.data;

                    // Check if the method exists
                    if (!syncMethodsRetry[log.method]) {
                        await writeLogsInHomePage(`Unknown method: ${log.method}`);
                        continue;
                    }

                    // Call the method with appropriate parameters
                    let result;
                    if (log.method === "syncDeleteFolder") {
                        result = await syncMethodsRetry[log.method](data.item, log.token);
                    } else if (log.method === "syncDeleteNote") {
                        result = await syncMethodsRetry[log.method](data, log.token);
                    } else {
                        result = await syncMethodsRetry[log.method](data, log.token);
                    }

                    if (result) {
                        await jsstoreCon.remove({
                            from: "SyncRetry",
                            where: { id: log.id },
                        });
                        await postMessageWithoutEvent("sync_response", { success: result });
                    }
                } catch (error) {
                    await writeLogsInHomePage(`Error retrying method ${log.method}:`, error.message);
                }
            }
        }
    } catch (error) {
        await writeLogsInHomePage("Error checking sync retry:", error.message);
    }
}

async function postMessageWithoutEvent(type, obj) {
    const allClients = await self.clients.matchAll();
    for (const client of allClients) {
        client.postMessage({ type: type, obj });
    }
}

async function writeLogsInHomePage(...args) {
    let log = args.map((x) => (typeof x === "string" ? x + " " : JSON.stringify(x) + " ")).join("");
    console.log(log);
    log = "sw: " + log;
    await postMessageWithoutEvent("writeLogsInPage", { log: log });
}
