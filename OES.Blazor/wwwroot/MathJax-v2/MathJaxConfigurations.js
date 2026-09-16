MathJax.Hub.setRenderer("HTML-CSS");
MathJax.Ajax.config.path["arabic"] = "/MathJax-v2/extensions/TeX/arabic";
MathJax.Ajax.config.path["chem"] = "/MathJax-v2/extensions/TeX";
MathJax.Hub.Config({
    tex2jax: { inlineMath: [['$', '$'], ['\\(', '\\)']] },
    extensions: [
        "[arabic]/arabic.js",
        "[chem]/mhchem.js"
    ],
    TeX: {
        extensions: ["[arabic]/arabic.js", "[chem]/mhchem.js"]
    },
    showProcessingMessages: false,
    showMathMenu: false,
    showMathMenuMSIE: false,
    menuSettings: {
        zoom: "Double-Click"
    }
});

// Fix for Arabic characters rendering mirrored inside \ar / \alwaysar expressions.
//
// \alwaysar mirrors the whole expression with transform: scaleX(-1) (class .mfliph in
// arabic.css) so the layout reads right-to-left. Glyphs only stay readable because each
// token is counter-flipped a second time, which cancels the outer mirror on the glyph
// while leaving the layout mirrored.
//
// arabic.js does that counter-flip in markArabicToken, which dispatches on mn / mi / mo
// only. A Latin identifier such as "x" becomes an mi, gets mapped to "س" and is always
// counter-flipped, so it renders upright. A raw Arabic letter such as "ص" is classified
// by MathJax as an ORD TeXAtom (type "texatom"), matches none of the three branches and
// is returned untouched - so nothing cancels the outer mirror and the glyph renders
// mirrored. arabicOperator has the same gap: it counter-flips only when it substitutes a
// known operator ("," ";" "lim"), so an Arabic-bearing mo is left unflipped too.
//
// Both overrides are additive: tokens that arabic.js already handles keep their existing
// behaviour, and Latin operators such as "(" and "<" are deliberately left mirrored
// because mirroring is correct for them in right-to-left math.
MathJax.Hub.Register.StartupHook("Arabic TeX Ready", function () {
    var TEX = MathJax.InputJax.TeX;
    var arabicScript = /[؀-ۿ]/;
    var originalMarkArabicToken = TEX.Parse.prototype.markArabicToken;
    var originalArabicOperator = TEX.Parse.prototype.arabicOperator;

    TEX.Parse.Augment({
        // Raw Arabic letters arrive wrapped in an ORD TeXAtom, which markArabicToken does
        // not dispatch on. Descend into the wrapper so the inner tokens get marked.
        markArabicToken: function (token) {
            if (token && "texatom" === token.type) {
                var inferredRow = token.data && token.data[0];
                if (inferredRow && inferredRow.data) {
                    for (var i = 0; i < inferredRow.data.length; i++) {
                        if (inferredRow.data[i]) {
                            inferredRow.data[i] = this.markArabicToken(inferredRow.data[i]);
                        }
                    }
                }
                return token;
            }

            return originalMarkArabicToken.call(this, token);
        },

        // Counter-flip operator tokens that carry Arabic script, so the outer \alwaysar
        // mirror does not reverse the glyphs.
        arabicOperator: function (token) {
            var marked = originalArabicOperator.call(this, token);
            var characters = marked && marked.data && marked.data[0] && marked.data[0].data
                ? marked.data[0].data[0]
                : null;

            if (!marked.arabicFlipH && typeof characters === "string" && arabicScript.test(characters)) {
                marked.arabicFontLang = "ar";
                marked = this.flipHorizontal(marked);
            }

            return marked;
        }
    });
});

// Fix for the radical sign splitting away from its horizontal bar (vinculum) when a root
// appears alongside other terms, e.g. \sqrt{س}+ص^٢.
//
// MathJax builds a radical from three absolutely positioned pieces: the radicand at
// left:1em, the bar (overlapping "−" glyphs) at left:1em, and the surd glyph "√" at
// left:0em. Mirroring the assembly for right-to-left leaves the surd about 0.2em short of
// the bar, so arabic.js compensates by shifting the surd right by 0.2em - but it only does
// so when the msqrt/mroot node itself carries arabicFlipH.
//
// That is true only when the root is the *entire* \alwaysar argument: MarkAsArabic unwraps
// a single-element inferred mrow, so the flip lands directly on the msqrt. When the root is
// one term among several the inferred mrow has several children, is not unwrapped, and the
// flip lands on the enclosing mrow instead. The msqrt never gets arabicFlipH, the shift is
// skipped, and the bar visibly detaches from the sign.
//
// So apply the same shift whenever a radical is rendered inside a mirrored subtree but was
// not itself flipped - i.e. exactly the cases arabic.js misses. Registered on
// "Arabic HTML-CSS Ready" because arabic.js posts that signal immediately after augmenting
// toHTML; hooking anything earlier gets overwritten by arabic.js's own augmentation.
MathJax.Hub.Register.StartupHook("Arabic HTML-CSS Ready", function () {
    var MML = MathJax.ElementJax.mml;

    // Must stay in step with the shift arabic.js applies to self-flipped radicals.
    var SURD_NUDGE_EM = 0.2;

    function isInsideMirroredSubtree(node) {
        var flips = 0;
        for (var parent = node.parent; parent; parent = parent.parent) {
            if (parent.arabicFlipH) {
                flips++;
            }
        }
        // Nested flips cancel out, so only an odd count actually renders mirrored.
        return flips % 2 === 1;
    }

    function nudgeSurd(pieceContainer) {
        if (!pieceContainer || !pieceContainer.children) {
            return;
        }

        for (var i = 0; i < pieceContainer.children.length; i++) {
            var piece = pieceContainer.children[i];
            if (piece.style && piece.style.position === "absolute") {
                var left = parseFloat(piece.style.left);
                // The radicand and the bar sit at 1em; only the surd is near 0em.
                if (!isNaN(left) && left < 0.5) {
                    piece.style.left = (left + SURD_NUDGE_EM) + "em";
                }
            }
        }
    }

    ["msqrt", "mroot"].forEach(function (type) {
        var originalToHTML = MML[type].prototype.toHTML;

        MML[type].Augment({
            toHTML: function () {
                var span = originalToHTML.apply(this, arguments);

                // When the node is flipped itself, arabic.js has already shifted the surd.
                if (!this.arabicFlipH && isInsideMirroredSubtree(this)) {
                    nudgeSurd(span.firstChild);
                }

                return span;
            }
        });
    });
});