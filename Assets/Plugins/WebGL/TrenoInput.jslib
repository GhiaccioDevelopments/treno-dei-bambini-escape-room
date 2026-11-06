// R005: blocco del puntatore (pointer lock) e tastiera per il canvas WebGL.
// - Il blocco lo chiede il C# (CursorManager) subito dopo un clic sul gioco: Chrome/Firefox lo concedono entro
//   l'attivazione utente del clic. Gli errori (es. richiesta entro ~1 s dall'uscita con Esc) sono ignorati in silenzio.
// - Lo stato vero si legge da document.pointerLockElement: il browser può togliere il blocco in ogni momento (Esc, Alt+Tab).
// - Tab non deve spostare il focus fuori dal gioco: preventDefault solo se il focus è sul canvas o sulla pagina.
mergeInto(LibraryManager.library, {
  TrenoInputInit: function () {
    if (typeof window === 'undefined' || window.__trenoInput) return;
    window.__trenoInput = true;
    var canvas = (typeof Module !== 'undefined' && Module['canvas']) ? Module['canvas'] : document.querySelector('canvas');
    window.__trenoCanvas = canvas;
    window.addEventListener('keydown', function (e) {
      if (e.key !== 'Tab' && e.keyCode !== 9) return;
      var a = document.activeElement;
      if (!a || a === document.body || a === canvas || a === document.documentElement) e.preventDefault();
    }, true);
    // un clic sul canvas gli dà il focus (la tastiera resta al gioco anche dopo aver cliccato fuori)
    if (canvas) {
      canvas.addEventListener('mousedown', function () { try { canvas.focus({ preventScroll: true }); } catch (e) { } }, true);
    }
  },

  TrenoRequestPointerLock: function () {
    var c = window.__trenoCanvas || ((typeof Module !== 'undefined' && Module['canvas']) ? Module['canvas'] : document.querySelector('canvas'));
    if (!c || !c.requestPointerLock || document.pointerLockElement === c) return;
    try {
      var p = c.requestPointerLock();
      if (p && typeof p.catch === 'function') p.catch(function () { });
    } catch (e) { }
  },

  TrenoExitPointerLock: function () {
    try { if (document.pointerLockElement && document.exitPointerLock) document.exitPointerLock(); } catch (e) { }
  },

  TrenoPointerLocked: function () {
    var c = window.__trenoCanvas || ((typeof Module !== 'undefined' && Module['canvas']) ? Module['canvas'] : null);
    var el = document.pointerLockElement;
    return (el && (c ? el === c : true)) ? 1 : 0;
  }
});
