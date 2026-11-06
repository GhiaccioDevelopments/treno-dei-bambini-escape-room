// R005: blocco del puntatore (pointer lock) e tastiera per il canvas WebGL.
// - Il C# (CursorManager) dice solo se vuole il blocco (TrenoSetWantLock). La richiesta vera parte DENTRO il listener
//   mousedown del canvas, cioè dentro il gesto dell'utente: vale per Chrome, Firefox e Safari. La richiesta fatta da Update
//   (TrenoRequestPointerLock) resta solo come ripiego, quando il blocco serve senza un clic (es. dopo "Riprendi").
// - Lo stato vero si legge da document.pointerLockElement: il browser può togliere il blocco in ogni momento (Esc, Alt+Tab).
// - exitRequested: se l'uscita dal blocco l'ha chiesta il C#, non è una perdita da segnalare (niente pausa).
// - Rifiuti: pointerlockerror dopo una richiesta da clic incrementa un contatore (TrenoLockFailures), azzerato al blocco riuscito.
// - Tab non deve spostare il focus fuori dal gioco: preventDefault solo se il focus è sul canvas o sulla pagina.
mergeInto(LibraryManager.library, {
  TrenoInputInit: function () {
    if (typeof window === 'undefined' || window.__trenoInput) return;
    window.__trenoInput = true;
    window.__trenoWantLock = false;
    window.__trenoExitRequested = false;
    window.__trenoLockLost = false;
    window.__trenoLockFails = 0;
    window.__trenoClickReq = false;
    var canvas = (typeof Module !== 'undefined' && Module['canvas']) ? Module['canvas'] : document.querySelector('canvas');
    window.__trenoCanvas = canvas;
    window.addEventListener('keydown', function (e) {
      if (e.key !== 'Tab' && e.keyCode !== 9) return;
      var a = document.activeElement;
      if (!a || a === document.body || a === canvas || a === document.documentElement) e.preventDefault();
    }, true);
    if (canvas) {
      // un clic sul canvas gli dà il focus e, se il gioco vuole il cursore bloccato, chiede il blocco nel gesto utente
      canvas.addEventListener('mousedown', function () {
        try { canvas.focus({ preventScroll: true }); } catch (e) { }
        if (!window.__trenoWantLock || document.pointerLockElement === canvas || !canvas.requestPointerLock) return;
        window.__trenoClickReq = true;
        try {
          var p = canvas.requestPointerLock();
          if (p && typeof p.catch === 'function') p.catch(function () { });
        } catch (e) { window.__trenoLockFails++; window.__trenoClickReq = false; }
      }, true);
    }
    document.addEventListener('pointerlockchange', function () {
      if (document.pointerLockElement) {
        window.__trenoLockFails = 0; window.__trenoClickReq = false; window.__trenoLockLost = false;
      } else if (window.__trenoExitRequested) {
        window.__trenoExitRequested = false; // uscita voluta dal gioco
      } else {
        window.__trenoLockLost = true; // tolto dal browser (Esc, Alt+Tab...)
      }
    });
    document.addEventListener('pointerlockerror', function () {
      if (window.__trenoClickReq) { window.__trenoLockFails++; window.__trenoClickReq = false; }
    });
  },

  TrenoSetWantLock: function (want) {
    window.__trenoWantLock = !!want;
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
    try {
      if (document.pointerLockElement && document.exitPointerLock) {
        window.__trenoExitRequested = true;
        document.exitPointerLock();
      }
    } catch (e) { }
  },

  TrenoPointerLocked: function () {
    var c = window.__trenoCanvas || ((typeof Module !== 'undefined' && Module['canvas']) ? Module['canvas'] : null);
    var el = document.pointerLockElement;
    return (el && (c ? el === c : true)) ? 1 : 0;
  },

  // 1 una sola volta se il browser ha tolto il blocco senza che il gioco l'abbia chiesto
  TrenoConsumeLockLost: function () {
    var v = window.__trenoLockLost ? 1 : 0;
    window.__trenoLockLost = false;
    return v;
  },

  TrenoLockFailures: function () {
    return window.__trenoLockFails | 0;
  }
});
