// client-error-logger.js
// Inkludér dette script i App.razor eller _Host.cshtml
// Sender ubehandlede JavaScript-fejl til /api/client-log

window.onerror = function (message, source, lineno, colno, error) {
    sendClientLog(message, source + ':' + lineno + ':' + colno, error ? error.stack : null);
};

window.onunhandledrejection = function (event) {
    sendClientLog(
        'Unhandled Promise rejection: ' + (event.reason?.message ?? event.reason),
        null,
        event.reason?.stack ?? null
    );
};

function sendClientLog(message, source, stack) {
    fetch('/api/client-log', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ message, source, stack })
    }).catch(() => { /* intentionally silent */ });
}
