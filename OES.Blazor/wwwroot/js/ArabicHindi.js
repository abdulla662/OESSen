(() => {
    let useArabicNumerals = true;

    const arabicDigits = ["٠", "١", "٢", "٣", "٤", "٥", "٦", "٧", "٨", "٩"];
    const standardDigits = ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9"];

    // Attributes whose values SHOULD be converted (text-like attributes).
    // Add any new attribute name here and it will automatically be:
    //   1. Converted in convertAttributes()
    //   2. Watched by the MutationObserver
    //   3. Protected from being reverted by fixConvertedAttributes()
    const CONVERTIBLE_ATTRIBUTES = [
        "title",
        "aria-label",
        "data-original-title",
        "placeholder"
    ];

    // Parent tag names whose text nodes should NEVER be converted.
    const SKIP_PARENT_TAGS = [
        "SCRIPT", "STYLE", "CODE", "PRE", "TEXTAREA", "INPUT"
    ];

    // CSS selectors for container elements whose content should NEVER be converted.
    // Any element matching these selectors (or nested inside one) will be skipped.
    const SKIP_CONTAINER_SELECTORS = [
        ".sun-editor",
        ".sun-editor-editable",
        ".se-wrapper"
    ];

    // Patterns that, if found in a text node's value, cause that node to be skipped.
    // Useful for protecting URLs, file paths, API endpoints, etc.
    const SKIP_TEXT_PATTERNS = [
        /https?:\/\//i,
        /\.(jpg|jpeg|png|gif|webp|svg|pdf|doc|docx|xls|xlsx|zip|rar)/i,
        /data-[a-z-]+=/i,
        /width\s*[:=]/i,
        /height\s*[:=]/i,
        /style\s*=/i,
        /class\s*=/i,
        /\/?api\//i,
        /\/\w+\/\w+/,
    ];

    const _skipContainerSelector = SKIP_CONTAINER_SELECTORS.join(", ");
    let conversionTimeout;
    let observer;

    function toArabicNumerals(text) {
        return text.replace(/\d/g, d => arabicDigits[d]);
    }

    function toStandardNumerals(text) {
        return text.replace(/[٠١٢٣٤٥٦٧٨٩]/g, d => standardDigits[arabicDigits.indexOf(d)]);
    }

    function shouldSkipConversion(text) {
        return SKIP_TEXT_PATTERNS.some(pattern => pattern.test(text));
    }

    function convertAllText(root) {
        const walker = document.createTreeWalker(
            root,
            NodeFilter.SHOW_TEXT,
            {
                acceptNode(node) {
                    if (!node.nodeValue.trim()) return NodeFilter.FILTER_REJECT;

                    const parentTag = node.parentNode.nodeName;
                    if (SKIP_PARENT_TAGS.includes(parentTag)) return NodeFilter.FILTER_REJECT;

                    if (node.parentNode.closest("[data-convert-numbers]")) {
                        return NodeFilter.FILTER_ACCEPT;
                    }

                    if (node.parentNode.nodeType === Node.ELEMENT_NODE && node.parentNode.closest("[data-no-convert]")) {
                        return NodeFilter.FILTER_REJECT;
                    }

                    if (node.parentNode.closest(_skipContainerSelector)) {
                        return NodeFilter.FILTER_REJECT;
                    }

                    return NodeFilter.FILTER_ACCEPT;
                },
            },
            false,
        );

        let node;
        while ((node = walker.nextNode())) {
            if (shouldSkipConversion(node.nodeValue)) {
                continue;
            }

            const newValue = useArabicNumerals
                ? toArabicNumerals(node.nodeValue)
                : toStandardNumerals(node.nodeValue);

            if (node.nodeValue !== newValue) {
                node.nodeValue = newValue;
            }
        }
    }

    function convertAttributes(root) {
        const selector = CONVERTIBLE_ATTRIBUTES.map(attr => `[${attr}]`).join(", ");
        const elements = root.querySelectorAll(selector);

        elements.forEach(el => {
            if (el.hasAttribute("data-no-convert")) return;
            if (el.closest(_skipContainerSelector)) return;

            CONVERTIBLE_ATTRIBUTES.forEach(attr => {
                if (el.hasAttribute(attr)) {
                    const oldValue = el.getAttribute(attr);

                    if (shouldSkipConversion(oldValue)) return;

                    const newValue = useArabicNumerals
                        ? toArabicNumerals(oldValue)
                        : toStandardNumerals(oldValue);

                    if (oldValue !== newValue) {
                        el.setAttribute(attr, newValue);
                    }
                }
            });
        });
    }

    function fixConvertedAttributes(root = document.body) {
        const convertibleSet = new Set(CONVERTIBLE_ATTRIBUTES);

        const allElements = root.querySelectorAll("*");
        allElements.forEach(el => {
            for (const attr of el.attributes) {
                if (convertibleSet.has(attr.name)) continue;

                if (/[٠١٢٣٤٥٦٧٨٩]/.test(attr.value)) {
                    el.setAttribute(attr.name, toStandardNumerals(attr.value));
                }
            }
        });
    }

    function fullConversion(root = document.body) {
        const editors = document.querySelectorAll(_skipContainerSelector);
        editors.forEach(ed => ed.setAttribute("data-no-convert", "1"));

        convertAllText(root);
        convertAttributes(root);
        fixConvertedAttributes(root);
    }

    function toggleArabicNumerals(state) {
        useArabicNumerals = state;
        fullConversion();

        document.dispatchEvent(
            new CustomEvent("numeralFormatChanged", {
                detail: { useArabicNumerals: state },
            }),
        );
    }

    function debouncedConversion(root) {
        clearTimeout(conversionTimeout);
        conversionTimeout = setTimeout(() => fullConversion(root), 10);
    }

    function setupObserver() {
        if (observer) observer.disconnect();

        observer = new MutationObserver(mutations => {
            let shouldConvert = false;
            let shouldFixAttributes = false;

            mutations.forEach(mutation => {
                if (mutation.target.closest &&
                    mutation.target.closest(_skipContainerSelector)) {
                    return;
                }

                if (mutation.type === "childList" && mutation.addedNodes.length > 0) {
                    mutation.addedNodes.forEach(node => {
                        if (node.nodeType === Node.ELEMENT_NODE) {
                            if (node.matches && node.matches(_skipContainerSelector)) {
                                return;
                            }
                            debouncedConversion(node);
                            shouldConvert = true;
                            shouldFixAttributes = true;
                        } else if (node.nodeType === Node.TEXT_NODE && node.nodeValue.trim()) {
                            shouldConvert = true;
                        }
                    });
                }

                if (
                    mutation.type === "attributes" &&
                    CONVERTIBLE_ATTRIBUTES.includes(mutation.attributeName)
                ) {
                    shouldConvert = true;
                }

                if (mutation.type === "characterData") {
                    shouldConvert = true;
                }
            });

            if (shouldConvert) {
                debouncedConversion();
            }

            if (shouldFixAttributes) {
                setTimeout(() => fixConvertedAttributes(), 20);
            }
        });

        observer.observe(document.body, {
            childList: true,
            subtree: true,
            attributes: true,
            attributeFilter: CONVERTIBLE_ATTRIBUTES,
            characterData: true,
        });
    }

    function setupBlazorIntegration() {
        if (window.Blazor) {
            window.Blazor.addEventListener("enhancedload", () => {
                setTimeout(fullConversion, 50);
            });
        }

        document.addEventListener("blazorComponentUpdated", e => {
            if (e.detail && e.detail.element) {
                fullConversion(e.detail.element);
            } else {
                fullConversion();
            }
        });
    }

    document.addEventListener("DOMContentLoaded", () => {
        fullConversion();
        setupObserver();
        setupBlazorIntegration();
    });

    window.toggleArabicNumerals = toggleArabicNumerals;
    window.forceNumeralConversion = () => fullConversion();
    window.convertElementNumerals = fullConversion;
})();
