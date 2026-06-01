export function transformDateToUnix(date) {
    if (!date) {
        return new Date().getTime();
    }
    return new Date(date).getTime();
}

export function writeLogsInPage(...args) {
    const log = args.map((x) => (typeof x === "string" ? x + " " : JSON.stringify(x) + " ")).join("");
    console.log(log);
    const logsContainer = document.getElementById("logs");
    if (logsContainer) {
        // Prepend current timestamp to each log entry.
        // Use textContent (not innerHTML) so log content — which may include
        // JSON.stringify'd user data (note/folder titles) that does NOT escape
        // < and > — cannot be interpreted as HTML (DOM XSS).
        let currentDate = new Date().toLocaleString();
        const entry = document.createElement("div");
        entry.textContent = currentDate + "-" + log;
        logsContainer.prepend(entry);
    }
}
