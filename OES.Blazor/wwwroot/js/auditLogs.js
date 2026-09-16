window.downloadExcelViaFetch = async (url, bodyJson, fileName, token) => {
    const response = await fetch(url, {
        method: 'POST',
        headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
        },
        body: bodyJson
    });

    if (!response.ok) {
        throw new Error(`Export failed: ${response.status} ${response.statusText}`);
    }

    const blob = await response.blob();
    const blobUrl = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = blobUrl;
    anchor.download = fileName;
    document.body.appendChild(anchor);
    anchor.click();
    document.body.removeChild(anchor);
    URL.revokeObjectURL(blobUrl);
};

window.auditLogs = {
    _dotnetHelper: null,
    _lastEvent: { key: null, time: 0 },
    _observer: null,
    _handlers: new Map(),

    _shouldLog(label) {
        const now = Date.now();
        const key = `click-${label}`;
        if (this._lastEvent.key === key && (now - this._lastEvent.time) < 300) {
            return false;
        }
        this._lastEvent.key = key;
        this._lastEvent.time = now;
        return true;
    },

    _logButtonClick(event) {
        const button = event.currentTarget;
        const action = getButtonLabel(button);
        if (!this._shouldLog(action)) return;

        const logData = {
            Action: action,
            Url: window.location.href,
            PathName: window.location.pathname,
        };

        // console.log({ logData });

        if (this._dotnetHelper) {
            this._dotnetHelper.invokeMethodAsync('LogAuditEvent', logData);
        }
    },

    _attachListener(button) {
        if (this._handlers.has(button)) return;
        const handler = this._logButtonClick.bind(this);
        button.addEventListener('click', handler);
        this._handlers.set(button, handler);
    },

    register(dotnetHelper) {
        this._dotnetHelper = dotnetHelper;

        document.querySelectorAll('button').forEach(el => this._attachListener(el));

        this._observer = new MutationObserver(mutations => {
            for (const mutation of mutations) {
                mutation.addedNodes.forEach(node => {
                    if (node.nodeType !== 1) return;
                    if (node.tagName === 'BUTTON') this._attachListener(node);
                    if (node.querySelectorAll) node.querySelectorAll('button').forEach(el => this._attachListener(el));
                });
            }
        });

        this._observer.observe(document.body, { childList: true, subtree: true });
    },

    unregister() {
        this._handlers.forEach((handler, button) => {
            button.removeEventListener('click', handler);
        });
        this._handlers.clear();

        if (this._observer) {
            this._observer.disconnect();
            this._observer = null;
        }

        this._dotnetHelper = null;
        this._lastEvent = { key: null, time: 0 };
    }
};

function getTooltipLabel(button) {
    const describedBy = button.getAttribute('aria-describedby');
    if (describedBy) {
        const el = document.getElementById(describedBy);
        if (el) {
            const text = el.textContent.trim();
            if (text) return text;
        }
    }

    const tooltipRoot = button.closest('.mud-tooltip-root');
    if (tooltipRoot) {
        const tooltipEl = tooltipRoot.querySelector(
            '.mud-popover-cascading-value, [role="tooltip"], [id^="popover"]'
        );
        if (tooltipEl) {
            const text = tooltipEl.textContent.trim();
            if (text) return text;
        }
        if (tooltipRoot.title) return tooltipRoot.title.trim();
    }

    return null;
}

function getButtonLabel(button) {
    if (button.dataset.name) return button.dataset.name;

    const text = button.textContent.trim();
    if (text) return text;

    if (button.title) return button.title;

    const ariaLabel = button.getAttribute('aria-label');
    if (ariaLabel) return ariaLabel;

    const tooltipLabel = getTooltipLabel(button);
    if (tooltipLabel) return tooltipLabel;

    // Search for classes that start with "label-"
    const labelClass = [...button.classList]
        .find(c => c.startsWith('label-'));

    if (labelClass)
        return labelClass.substring('label-'.length);

    return '(unlabeled)';
}
