document.querySelectorAll(".alert[role='status']").forEach((alert) => {
    window.setTimeout(() => alert.remove(), 5000);
});
