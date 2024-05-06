window.reqcraft = window.reqcraft || {};

window.reqcraft.scrollContainer = function(ref) {
    ref.scrollTop = ref.scrollHeight;
}

window.reqcraft.copyContent = function(text) {
    if(navigator.clipboard) {
        navigator.clipboard.writeText(text).catch(err => {
            console.error('Failed to copy text: ', err);
        });
    }
}