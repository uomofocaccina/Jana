// ES modules for service worker
import { transformDateToUnix } from "./utility.js";
import { getDbSchema } from "./dbschema.js";

// Re-export for service worker compatibility
export { transformDateToUnix, getDbSchema };

const API_BASE_URL = import.meta.env.API_BASE_URL;

function getHeaders(token) {
    const headers = { "Content-Type": "application/json" };
    if (token) {
        headers["Authorization"] = `Bearer ${token}`;
    }
    return headers;
}

async function fetchJson(url, options = {}) {
    const response = await fetch(url, options);
    const data = await response.json().catch(() => ({}));
    //console.log (data);
    if (!response.ok) {
        if (response.status === 401) {
            console.log("Session expired or not authorized", url, options);
            return { success: false, message: "Auth error.", code: 401 };
        }
        //const error = new Error(data.message || "Internal server error");
        //error.response = { status: response.status };
        //throw error;
        const error = data.message || "Internal server error";
        return { success: false, message: error, code: error.status };
    }
    return data;
}

export const api = {
    async login(username, password) {
        try {
            const data = await fetchJson(`${API_BASE_URL}/login`, {
                method: "POST",
                headers: getHeaders(),
                body: JSON.stringify({ username, password }),
            });
            return data;
        } catch (e) {
            return {success: false, message: "Login failed"};
        }
    },

    async changePassword(token, oldPassword, newPassword) {
        try
        {
        const data = await fetchJson(`${API_BASE_URL}/change-password`, {
            method: "POST",
            headers: getHeaders(token),
            body: JSON.stringify({ oldPassword, newPassword }),
        });
        return data;
        
        } catch (e) {
        throw new Error("Password change failed",e);
        }
    },

    async getFolder(token, timestamp, offset = 0, limit = 10) {
        const params = new URLSearchParams({ offset, limit });
        const data = await fetchJson(`${API_BASE_URL}/folder/${timestamp}?${params.toString()}`, {
            headers: getHeaders(token),
        });
        if (data.success) {
            return data;
        }
        throw new Error("Error getting folder");
    },

    async createFolder(token, dataObj) {
        const data = await fetchJson(`${API_BASE_URL}/folder`, {
            method: "PUT",
            headers: getHeaders(token),
            body: JSON.stringify(dataObj),
        });
        if (data.success) {
            return data.timestamp;
        }
        throw new Error("Error creating folder: " + (data.message || JSON.stringify(data)));
    },

    async updateFolder(token, dataObj) {
        const data = await fetchJson(`${API_BASE_URL}/folder`, {
            method: "POST",
            headers: getHeaders(token),
            body: JSON.stringify(dataObj),
        });
        if (data.success) {
            return data.timestamp;
        }
        throw new Error("Error updating folder");
    },

    async deleteFolder(token, id) {
        const data = await fetchJson(`${API_BASE_URL}/folder/${id}`, {
            method: "DELETE",
            headers: getHeaders(token),
        });
        if (data.success) {
            return data.timestamp;
        }
        throw new Error("Error deleting folder");
    },

    async changeParentFolder(token, id, parent) {
        const body = { parent: parent };
        const data = await fetchJson(`${API_BASE_URL}/folder/parent/${id}`, {
            method: "POST",
            headers: getHeaders(token),
            body: JSON.stringify(body),
        });
        if (data.success) {
            return data.timestamp;
        }
        throw new Error("Error changing parent folder");
    },

    async getNotes(token, timestamp, offset = 0, limit = 9) {
        const params = new URLSearchParams({ offset, limit });
        const data = await fetchJson(`${API_BASE_URL}/note/${timestamp}?${params.toString()}`, {
            headers: getHeaders(token),
        });
        if (data.success) {
            return data;
        }
        throw new Error("Error getting notes");
    },

    async createNote(token, dataObj) {
        const data = await fetchJson(`${API_BASE_URL}/note`, {
            method: "PUT",
            headers: getHeaders(token),
            body: JSON.stringify(dataObj),
        });
        if (data.success) {
            return data.timestamp;
        }
        throw new Error("Error creating note: " + (data.message || JSON.stringify(data)));
    },

    async updateNote(token, dataObj) {
        const data = await fetchJson(`${API_BASE_URL}/note`, {
            method: "POST",
            headers: getHeaders(token),
            body: JSON.stringify(dataObj),
        });
        if (data.success) {
            return data.timestamp;
        }
        throw new Error("Error updating note");
    },

    async deleteNote(token, id) {
        const data = await fetchJson(`${API_BASE_URL}/note/${id}`, {
            method: "DELETE",
            headers: getHeaders(token),
        });
        if (data.success) {
            return data.timestamp;
        }
        throw new Error("Error deleting note");
    },

    async changeFolderNote(token, id, folder) {
        const body = { folder: folder };
        const data = await fetchJson(`${API_BASE_URL}/note/directory/${id}`, {
            method: "POST",
            headers: getHeaders(token),
            body: JSON.stringify(body),
        });
        if (data.success) {
            return data.timestamp;
        }
        throw new Error("Error changing parent note");
    },
};
