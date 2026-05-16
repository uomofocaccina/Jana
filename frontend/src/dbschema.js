export function getDbSchema(transformDateToUnix) {
    var tableFolder = {
        name: "Folder",
        columns: {
            id: {
                primaryKey: true,
                dataType: "string",
            },
            text: {
                notNull: true,
                dataType: "string",
            },
            parent: {
                dataType: "string",
            },
            timestamp: {
                dataType: "number",
                notNull: true,
                default: transformDateToUnix(),
            },
            active: {
                dataType: "number",
                default: 1,
                notNull: true,
            },
        },
    };

    var tableNote = {
        name: "Note",
        columns: {
            id: {
                primaryKey: true,
                dataType: "string",
            },
            text: {
                notNull: true,
                dataType: "string",
            },
            folder: {
                dataType: "string",
            },
            title: {
                dataType: "string",
            },
            timestamp: {
                dataType: "number",
                notNull: true,
                default: transformDateToUnix(),
            },
            active: {
                dataType: "number",
                default: 1,
                notNull: true,
            },
        },
    };

    var tableSyncState = {
        name: "SyncState",
        columns: {
            id: {
                primaryKey: true,
                dataType: "string",
            },
            timestamp: {
                dataType: "number",
                notNull: true,
                default: transformDateToUnix(),
            },
        },
    };

    var tableSyncRetry = {
        name: "SyncRetry",
        columns: {
            id: {
                primaryKey: true,
                autoIncrement: true,
                dataType: "number",
            },
            method: {
                dataType: "string",
            },
            data: {
                dataType: "object",
            },
            token: {
                dataType: "string",
            },
            timestamp: {
                dataType: "number",
                notNull: true,
                default: transformDateToUnix(),
            },
        },
    };

    var tableAuth = {
        name: "Auth",
        columns: {
            username: {
                primaryKey: true,
                dataType: "string",
            },
            token: {
                dataType: "string",
            },
            isGuest: {
                dataType: "boolean",
                default: false,
            },
            isAdmin: {
                dataType: "boolean",
                default: false,
            },
            tokenExpiry: {
                dataType: "number",
            },
            timestamp: {
                dataType: "number",
                notNull: true,
                default: transformDateToUnix(),
            },
        },
    };

    var db = {
        name: "SPara",
        tables: [tableFolder, tableSyncState, tableNote, tableSyncRetry, tableAuth],
        version: 8,
    };
    return db;
}
