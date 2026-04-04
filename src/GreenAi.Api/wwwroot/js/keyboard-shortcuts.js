// keyboard-shortcuts.js
// Registers a global Ctrl+K listener that opens the Command Palette.
// CommandPalette.razor calls greenai.registerCtrlK(dotNetRef) on mount
// and greenai.unregisterCtrlK() on dispose.

window.greenai = window.greenai || {};

(function (g) {
    var _ref = null;
    var _handler = null;

    g.registerCtrlK = function (dotNetRef) {
        _ref = dotNetRef;
        _handler = function (e) {
            if (e.ctrlKey && e.key === 'k') {
                e.preventDefault();
                _ref.invokeMethodAsync('OpenPalette');
            }
        };
        window.addEventListener('keydown', _handler);
    };

    g.unregisterCtrlK = function () {
        if (_handler) {
            window.removeEventListener('keydown', _handler);
            _handler = null;
        }
        if (_ref) {
            _ref.dispose();
            _ref = null;
        }
    };
}(window.greenai));
