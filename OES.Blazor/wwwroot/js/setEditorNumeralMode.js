(() => {
    let useArabicNumbers = false;

    const arabic = ["٠", "١", "٢", "٣", "٤", "٥", "٦", "٧", "٨", "٩"];
    const latin = ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9"];

    function toArabic(text) {
        return text.replace(/\d/g, d => arabic[d]);
    }

    function toLatin(text) {
        return text.replace(/[٠-٩]/g, d => latin[arabic.indexOf(d)]);
    }

    function convertEditorContent() {
        const editors = document.querySelectorAll(".sun-editor-editable");
        editors.forEach(editor => {
            const walker = document.createTreeWalker(editor, NodeFilter.SHOW_TEXT, null);
            let node;
            while ((node = walker.nextNode())) {
                if (!node.nodeValue.trim()) continue;

                const oldText = node.nodeValue;
                const newText = useArabicNumbers ? toArabic(oldText) : toLatin(oldText);

                if (oldText !== newText) {
                    node.nodeValue = newText;
                }
            }
        });
    }

    document.addEventListener("input", (e) => {
        const target = e.target;
        const element = target instanceof Element ? target : target?.parentElement;

        if (!element || typeof element.closest !== "function") return;

        const editor = element.closest(".sun-editor-editable");
        if (!editor) return;

        const sel = window.getSelection();
        if (!sel || sel.rangeCount === 0) return;

        const range = sel.getRangeAt(0);
        const node = range.startContainer;
        if (!node || node.nodeType !== Node.TEXT_NODE) return;

        const offset = range.startOffset;
        const oldText = node.nodeValue;
        const newText = useArabicNumbers ? toArabic(oldText) : toLatin(oldText);

        if (oldText !== newText) {
            node.nodeValue = newText;

            range.setStart(node, Math.min(offset, newText.length));
            range.setEnd(node, Math.min(offset, newText.length));
            sel.removeAllRanges();
            sel.addRange(range);
        }
    }, true);

    window.setEditorNumeralMode = function (useArabic) {
        useArabicNumbers = useArabic;
        convertEditorContent();
    };
})();