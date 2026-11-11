(() => {
    const node = document.getElementById("dashboard-data");
    if (!node || typeof Chart === "undefined") return;
    const data = JSON.parse(node.textContent);
    const palette = ["#8eb4ef", "#8fd0be", "#c3b4ea", "#f0b27a", "#f0a3b8", "#9eb0c6", "#7ec8d6", "#d7c4a3"];
    Chart.defaults.font.family = "Manrope, Segoe UI, sans-serif";
    Chart.defaults.color = "#66758a";

    const money = (value) => new Intl.NumberFormat("tr-TR", { style: "currency", currency: "TRY", maximumFractionDigits: 0 }).format(value);

    new Chart(document.getElementById("monthlyChart"), {
        data: {
            labels: data.labels,
            datasets: [
                { type: "bar", label: "Ciro", data: data.revenue, backgroundColor: "#c5d9fb", borderRadius: 8, yAxisID: "y" },
                { type: "line", label: "Sipariş", data: data.orders, borderColor: "#e08a45", backgroundColor: "#e08a45", tension: 0.35, yAxisID: "y1" }
            ]
        },
        options: {
            maintainAspectRatio: false,
            interaction: { mode: "index", intersect: false },
            scales: {
                y: { ticks: { callback: (value) => money(value) } },
                y1: { position: "right", grid: { drawOnChartArea: false } }
            }
        }
    });

    new Chart(document.getElementById("categoryChart"), {
        type: "bar",
        data: { labels: data.categories, datasets: [{ label: "Ciro", data: data.categoryRevenue, backgroundColor: "#d9cff6", borderRadius: 8 }] },
        options: { indexAxis: "y", maintainAspectRatio: false, plugins: { legend: { display: false } } }
    });

    new Chart(document.getElementById("cityChart"), {
        type: "bar",
        data: { labels: data.cities, datasets: [{ label: "Ciro", data: data.cityRevenue, backgroundColor: "#b7e4d6", borderRadius: 8 }] },
        options: { maintainAspectRatio: false, plugins: { legend: { display: false } } }
    });

    new Chart(document.getElementById("statusChart"), {
        type: "doughnut",
        data: { labels: data.statuses, datasets: [{ data: data.statusCounts, backgroundColor: palette }] },
        options: { maintainAspectRatio: false, plugins: { legend: { position: "bottom" } } }
    });
})();
