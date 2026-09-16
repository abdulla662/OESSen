(function () {
    var msgs = {
        ar: '\u064A\u0631\u062C\u0649 \u0645\u0644\u0621 \u0647\u0630\u0627 \u0627\u0644\u062D\u0642\u0644',
        en: 'Please fill out this field'
    };

    function lang() {
        return (localStorage.getItem('appCulture') || 'en').substring(0, 2);
    }

    var tip = document.createElement('div');
    tip.style.cssText = 'display:none;position:fixed;z-index:99999;background:#616161;color:#fff;' +
        'padding:5px 10px;border-radius:4px;font-size:0.75rem;font-family:Cairo,sans-serif;' +
        'pointer-events:none;white-space:nowrap;box-shadow:0 2px 8px rgba(0,0,0,.25);opacity:1;';

    var arrow = document.createElement('div');
    arrow.style.cssText = 'position:absolute;top:-5px;left:50%;transform:translateX(-50%);' +
        'width:0;height:0;border-left:5px solid transparent;border-right:5px solid transparent;' +
        'border-bottom:5px solid #616161;';
    tip.appendChild(arrow);

    var textNode = document.createElement('span');
    tip.appendChild(textNode);

    function showTip(el) {
        var m = "";//msgs[lang()]; if the message is empty, it will not show the tip, which is the expected behavior for languages that are not supported.
        if (!m) return;
        textNode.textContent = m;
        if (!tip.parentNode) document.body.appendChild(tip);
        tip.style.display = 'block';
        var r = el.getBoundingClientRect();
        tip.style.left = (r.left + r.width / 2 - tip.offsetWidth / 2) + 'px';
        tip.style.top = (r.bottom + 8) + 'px';
    }

    function hideTip() {
        tip.style.display = 'none';
    }

    var hoverEl = null;

    document.addEventListener('mouseover', function (e) {
        var el = e.target;
        if (!el.required && el.closest) el = el.closest('[required]');
        if (!el || !el.required) { if (hoverEl) { hoverEl = null; hideTip(); } return; }
        if (el.value && el.value.trim()) { if (hoverEl) { hoverEl = null; hideTip(); } return; }
        el.title = '';
        if (hoverEl === el) return;
        hoverEl = el;
        showTip(el);
    }, true);

    document.addEventListener('mouseout', function (e) {
        var el = e.target;
        if (!el.required && el.closest) el = el.closest('[required]');
        if (!el || el !== hoverEl) return;
        var to = e.relatedTarget;
        if (to && (to === el || el.contains(to))) return;
        hoverEl = null;
        hideTip();
    }, true);

    document.addEventListener('click', function (e) {
        var btn = e.target.closest ? e.target.closest('[type="submit"]') : null;
        if (!btn) return;
        var form = btn.closest('form');
        if (!form) return;
        var m = msgs[lang()];
        if (!m) return;
        form.querySelectorAll('[required]').forEach(function (el) {
            el.setCustomValidity('');
            if (!el.value || !el.value.trim()) el.setCustomValidity(m);
        });
    }, true);

    document.addEventListener('invalid', function (e) {
        var m = msgs[lang()];
        if (m && e.target.validity && e.target.validity.valueMissing)
            e.target.setCustomValidity(m);
    }, true);

    document.addEventListener('input', function (e) {
        if (e.target && e.target.setCustomValidity) e.target.setCustomValidity('');
    }, true);
    document.addEventListener('change', function (e) {
        if (e.target && e.target.setCustomValidity) e.target.setCustomValidity('');
    }, true);

    /* ── Suppress native title continuously ── */
    setInterval(function () {
        document.querySelectorAll('[required]').forEach(function (el) {
            if (el.title) el.title = '';
        });
    }, 300);
})();
