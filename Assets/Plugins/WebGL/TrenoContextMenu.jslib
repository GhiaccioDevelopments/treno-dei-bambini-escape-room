// Disattiva il menu contestuale del tasto destro sul canvas WebGL (il tasto destro ruota la camera).
mergeInto(LibraryManager.library, {
  TrenoDisableContextMenu: function () {
    var c = (typeof Module !== 'undefined' && Module['canvas']) ? Module['canvas'] : document.querySelector('canvas');
    if (c && !c.__trenoCtx) {
      c.__trenoCtx = true;
      c.addEventListener('contextmenu', function (e) { e.preventDefault(); });
    }
  }
});
