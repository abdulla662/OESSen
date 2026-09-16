window.pageCssManager = {
    register: function (cssFileName) {
        // Remove any existing dynamic CSS before adding new one
        document.querySelectorAll("link[data-page-css]").forEach(link => link.remove());
        // Create and append new <link>
        const link = document.createElement("link");
        link.rel = "stylesheet";
        link.href = `/css/${cssFileName}`;
        link.type = "text/css";
        link.setAttribute("data-page-css", cssFileName);
        document.head.appendChild(link);
    },
    unregister: function () {
        // Remove dynamically added CSS
        document.querySelectorAll("link[data-page-css]").forEach(link => link.remove());
    }
};
