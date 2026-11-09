(function () {
    "use strict";

    var root = document.documentElement;
    var STORAGE_KEY = "theme";

    function currentTheme() {
        return root.getAttribute("data-bs-theme") === "dark" ? "dark" : "light";
    }

    function refreshLabels() {
        var dark = currentTheme() === "dark";
        var label = dark ? "☀️ Modo claro" : "🌙 Modo oscuro";

        document.querySelectorAll("[data-theme-toggle]").forEach(function (button) {
            button.textContent = label;
            button.setAttribute("aria-label", dark ? "Cambiar a modo claro" : "Cambiar a modo oscuro");
            button.setAttribute("title", label);
        });
    }

    function applyTheme(theme) {
        root.setAttribute("data-bs-theme", theme);
        refreshLabels();
    }

    document.addEventListener("click", function (event) {
        var target = event.target;
        if (!(target instanceof Element)) {
            return;
        }

        var button = target.closest("[data-theme-toggle]");
        if (!button) {
            return;
        }

        var next = currentTheme() === "dark" ? "light" : "dark";
        applyTheme(next);

        try {
            localStorage.setItem(STORAGE_KEY, next);
        } catch (e) {
            // Si el navegador bloquea localStorage, el tema se aplica igual en esta página.
        }
    });

    // Sincroniza si el tema se cambia en otra pestaña.
    window.addEventListener("storage", function (event) {
        if (event.key === STORAGE_KEY &&
            (event.newValue === "light" || event.newValue === "dark")) {
            applyTheme(event.newValue);
        }
    });

    refreshLabels();
})();
