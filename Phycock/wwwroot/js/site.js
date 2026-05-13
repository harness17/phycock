// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

window.PhycockCalendar = (function () {
    function textOrEmpty(value) {
        return value == null ? '' : String(value);
    }

    function appendLine(container, className, text) {
        const value = textOrEmpty(text).trim();
        if (!value) return;

        const line = document.createElement('div');
        line.className = className;
        line.textContent = value;
        container.appendChild(line);
    }

    function renderRecordEvent(info) {
        const props = info.event.extendedProps || {};
        const container = document.createElement('div');
        container.className = 'record-calendar-event';

        appendLine(container, 'record-calendar-event__title', props.primaryText || info.event.title);
        appendLine(container, 'record-calendar-event__meta', props.secondaryText || info.timeText);
        appendLine(container, 'record-calendar-event__note', props.noteText);

        return { domNodes: [container] };
    }

    function applyEventColors(info) {
        const main = info.el.querySelector('.fc-event-main');
        if (info.event.backgroundColor) {
            info.el.style.backgroundColor = info.event.backgroundColor;
        }
        if (info.event.borderColor) {
            info.el.style.borderColor = info.event.borderColor;
        }
        if (info.event.textColor) {
            info.el.style.color = info.event.textColor;
            if (main) {
                main.style.color = info.event.textColor;
            }
        }
    }

    return {
        renderRecordEvent,
        applyEventColors
    };
})();

window.PhycockTimeInput = (function () {
    const timePattern = /^([01]\d|2[0-3]):[0-5]\d$/;

    function init(root) {
        const scope = root || document;
        scope.querySelectorAll('.time-focus-input').forEach(function (input) {
            if (input.dataset.timeFocusInitialized === 'true') return;
            input.dataset.timeFocusInitialized = 'true';
            input.dataset.timeFocusDigitCount = '0';

            input.addEventListener('focus', function () {
                input.dataset.timeFocusDigitCount = '0';
            });
            input.addEventListener('keydown', function (event) {
                if (/^\d$/.test(event.key)) {
                    const currentCount = Number(input.dataset.timeFocusDigitCount || '0');
                    input.dataset.timeFocusDigitCount = String(currentCount + 1);
                    return;
                }

                if (event.key === 'Enter') {
                    focusNextWhenComplete(input);
                }
            });
            input.addEventListener('change', function () {
                const digitCount = Number(input.dataset.timeFocusDigitCount || '0');
                if (digitCount >= 4) {
                    focusNextWhenComplete(input);
                }
            });
        });
    }

    function focusNextWhenComplete(input) {
        if (!timePattern.test(input.value)) return;

        const nextSelector = input.getAttribute('data-next-time-input');
        if (!nextSelector) return;

        const form = input.closest('form');
        const nextInput = (form || document).querySelector(nextSelector);
        if (nextInput) {
            nextInput.focus();
        }
    }

    return {
        init
    };
})();
