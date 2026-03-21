mergeInto(LibraryManager.library, {
    TriggerHaptic: function(durationMs) {
        if (typeof navigator !== 'undefined' && 'vibrate' in navigator) {
            navigator.vibrate(durationMs);
        }
    },
    TriggerHapticPattern: function(patternPtr, length) {
        if (typeof navigator !== 'undefined' && 'vibrate' in navigator) {
            var pattern = [];
            for (var i = 0; i < length; i++) {
                pattern.push(HEAP32[(patternPtr >> 2) + i]);
            }
            navigator.vibrate(pattern);
        }
    }
});
