(() => {
    const node = document.getElementById("sales-data");
    if (!node || typeof Chart === "undefined") return;
    const data = JSON.parse(node.textContent);
    const colors = ["#8eb4ef", "#8fd0be", "#c3b4ea", "#f0b27a", "#f0a3b8"];
    Chart.defaults.font.family = "Manrope, Segoe UI, sans-serif";
    Chart.defaults.color = "#66758a";

    new Chart(document.getElementById("salesCityChart"), {
        type: "bar",
        data: {
            labels: data.cities,
            datasets: [{ label: "Ciro", data: data.cityRevenue, backgroundColor: "#b7e4d6", borderRadius: 8 }]
        },
        options: { maintainAspectRatio: false, plugins: { legend: { display: false } } }
    });

    new Chart(document.getElementById("categoryMonthChart"), {
        type: "line",
        data: {
            labels: data.months,
            datasets: data.series.slice(0, 5).map((series, index) => ({
                label: series.name,
                data: series.values,
                borderColor: colors[index],
                backgroundColor: colors[index],
                tension: 0.3
            }))
        },
        options: { maintainAspectRatio: false, interaction: { mode: "index", intersect: false } }
    });
})();
