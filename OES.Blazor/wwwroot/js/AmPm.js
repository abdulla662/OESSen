window.observeAmPm = function () {
    const translate = () => {
        document.querySelectorAll(".mud-button-label").forEach(el => {
            if (el.textContent.trim() === "AM") {
                el.textContent = "ص";
            }
            if (el.textContent.trim() === "PM") {
                el.textContent = "م";
            }
        });
    };

    translate();

    const observer = new MutationObserver(() => translate());

    observer.observe(document.body, { childList: true, subtree: true });
};