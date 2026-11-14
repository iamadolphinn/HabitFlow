window.habitInterop = {
    downloadFile(fileName, content) {
        const blob = new Blob([content], { type: "text/csv;charset=utf-8" });
        const url = URL.createObjectURL(blob);
        const a = document.createElement("a");
        a.href = url;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        a.remove();
        URL.revokeObjectURL(url);
    }
};

// Optional PWA support: the app can be installed on phones/desktops.
if ("serviceWorker" in navigator) {
    navigator.serviceWorker.register("service-worker.js").catch(() => { /* ignore */ });
}

// Diagnostic: if Blazor has not rendered anything after a few seconds, tell the user instead of showing a blank page.
setTimeout(() => {
    if (!document.querySelector("nav, main")) {
        const w = document.getElementById("startup-warning");
        if (w) w.style.display = "block";
    }
}, 5000);
