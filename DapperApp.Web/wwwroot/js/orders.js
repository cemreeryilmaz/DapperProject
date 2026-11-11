(() => {
    const modalElement = document.getElementById("orderModal");
    if (!modalElement || typeof bootstrap === "undefined") return;
    const modal = new bootstrap.Modal(modalElement);
    const money = (value) => new Intl.NumberFormat("tr-TR", { style: "currency", currency: "TRY" }).format(value);
    const esc = (value) => String(value ?? "").replace(/[&<>"']/g, (ch) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", "\"": "&quot;", "'": "&#39;" }[ch]));
    const when = (value) => new Intl.DateTimeFormat("tr-TR", { dateStyle: "medium", timeStyle: "short" }).format(new Date(value));

    document.querySelectorAll("[data-order-id]").forEach((row) => {
        row.addEventListener("click", async () => {
            const id = row.getAttribute("data-order-id");
            const response = await fetch(`/Admin/OrderExplorer/Detail/${id}`);
            if (!response.ok) {
                document.getElementById("orderModalBody").textContent = "Sipariş detayı alınamadı.";
                modal.show();
                return;
            }

            const order = await response.json();
            const lines = (order.lines || []).map((line) => `
                <tr>
                    <td>${esc(line.productName)}</td>
                    <td>${esc(line.categoryName)}</td>
                    <td class="sp-num">${line.quantity}</td>
                    <td class="sp-num">${money(line.unitPrice)}</td>
                    <td class="sp-num">${money(line.lineTotal)}</td>
                </tr>`).join("");

            document.getElementById("orderModalTitle").textContent = order.orderNo;
            document.getElementById("orderModalBody").innerHTML = `
                <div class="sp-status-list">
                    <div><span>Müşteri</span><b>${esc(order.customerName)}</b></div>
                    <div><span>E-posta</span><b>${esc(order.email)}</b></div>
                    <div><span>Şehir</span><b>${esc(order.cityName)}</b></div>
                    <div><span>Tarih</span><b>${when(order.orderDate)}</b></div>
                    <div><span>Ödeme</span><b>${esc(order.paymentMethod)}</b></div>
                    <div><span>Durum</span><b>${esc(order.status)}</b></div>
                    <div><span>Tutar</span><b>${money(order.totalAmount)}</b></div>
                </div>
                <div class="sp-table-wrap" style="margin-top:16px">
                    <table class="sp-table">
                        <thead><tr><th>Ürün</th><th>Kategori</th><th class="sp-num">Adet</th><th class="sp-num">Birim</th><th class="sp-num">Tutar</th></tr></thead>
                        <tbody>${lines}</tbody>
                    </table>
                </div>`;
            modal.show();
        });
    });
})();
