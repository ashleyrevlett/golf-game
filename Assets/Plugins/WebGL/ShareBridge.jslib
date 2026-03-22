mergeInto(LibraryManager.library, {
    ShareScore: function(textPtr, gameObjectNamePtr) {
        var text = UTF8ToString(textPtr);
        var goName = UTF8ToString(gameObjectNamePtr);
        var url = window.location.href;
        var fullText = text + " " + url;
        try {
            if (navigator.share) {
                navigator.share({ text: fullText }).then(function() {
                    SendMessage(goName, 'OnShareResult', 'shared');
                }).catch(function() {
                    SendMessage(goName, 'OnShareResult', 'failed');
                });
            } else if (navigator.clipboard) {
                navigator.clipboard.writeText(fullText).then(function() {
                    SendMessage(goName, 'OnShareResult', 'copied');
                }).catch(function() {
                    SendMessage(goName, 'OnShareResult', 'failed');
                });
            } else {
                SendMessage(goName, 'OnShareResult', 'failed');
            }
        } catch(e) {
            SendMessage(goName, 'OnShareResult', 'failed');
        }
    }
});
