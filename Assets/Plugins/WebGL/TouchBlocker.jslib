mergeInto(LibraryManager.library, {
    DisableBrowserTouchDefaults: function() {
        var canvas = document.querySelector('#unity-canvas') || document.querySelector('canvas');
        if (canvas) {
            canvas.style.touchAction = 'none';
            canvas.addEventListener('touchmove', function(e) { e.preventDefault(); }, { passive: false });
            canvas.addEventListener('touchstart', function(e) { e.preventDefault(); }, { passive: false });
            canvas.addEventListener('contextmenu', function(e) { e.preventDefault(); });
        }
    }
});
