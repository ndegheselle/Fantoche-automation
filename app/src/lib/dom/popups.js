// Functions to show and close
const targets = [
    // Close modal
    {
        selector: '.modal-background, .modal-card-head .delete[aria-label="close"], .modal .button[data-dismiss="modal"]',
        /** @param {Element} element */
        callback: function (element) {
            const $target = element.closest('.modal');
            close($target);
        }
    },
    // Open dropdown
    {
        selector: '.dropdown-trigger [aria-haspopup="true"]',
        /** @param {Element} element */
        callback: function (element) {
            const $target = element.closest('.dropdown');
            show($target);
        }
    }
];

const outsideTargets = [
    '.dropdown.is-active'
];

/** @param {Element} $el */
function sendClosing($el) {
    if (!$el.classList.contains('is-active')) return;
    $el.dispatchEvent(new Event("closing"));
}

/** @param {Element|null} $el */
function show($el) {
    if (!$el) return;
    $el.classList.add('is-active');
}

/** @param {Element|null} $el */
function close($el) {
    if (!$el) return;

    sendClosing($el);
    // Most popups will handle removing 'is-active' on their own (not dropdowns)
    $el.classList.remove('is-active');
}

/** @param {string} selector */
function closeAll(selector) {
    (document.querySelectorAll(selector) || []).forEach(($element) => {
        close($element);
    });
}

export default {
    init() {

        // add js doc for event handler
        document.addEventListener("click",
            /** @param {MouseEvent} e */
            function (e) {
                for (const target of targets) {
                    if (e.target instanceof Element === false) continue;
    
                    let element = e.target?.closest(target.selector);
                    if (element) return target.callback(element);
                }
    
                // Close all opened dropdown (including context menu)
                closeAll(outsideTargets.join(', '));
            });
    
        // Add a keyboard event to close all modals
        document.addEventListener('keydown', (event) => {
            const e = event || window.event;
    
            // Escape key
            if (e.code === 'Escape') {
                closeAll(outsideTargets.join(', '));
            }
        });
    },
    close,
    show
}