(() => {
    const sidebar = document.querySelector(".sp-sidebar");
    const overlay = document.querySelector(".sp-overlay");
    const toggle = document.querySelector(".sp-menu-btn");
    const close = () => {
        sidebar?.classList.remove("is-open");
        overlay?.classList.remove("is-open");
    };
    toggle?.addEventListener("click", () => {
        sidebar?.classList.toggle("is-open");
        overlay?.classList.toggle("is-open");
    });
    overlay?.addEventListener("click", close);

    document.querySelectorAll("form").forEach((form) => {
        form.addEventListener("submit", () => document.body.classList.add("is-loading"));
    });
    document.querySelectorAll("a.sp-btn, a.sp-btn-ghost, .sp-pages a, .sp-presets a").forEach((link) => {
        link.addEventListener("click", () => document.body.classList.add("is-loading"));
    });
})();
