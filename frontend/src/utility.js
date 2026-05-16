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
        //read the current log and put the new log in the top
        let currentLogs = logsContainer.innerHTML;
        // Prepend current timestamp to each log entry
        let currentDate = new Date().toLocaleString();
        let newlog = currentDate + "-" + log + "<br>";
        logsContainer.innerHTML = newlog + currentLogs;
    }
}
