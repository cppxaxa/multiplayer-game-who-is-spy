/**
 * toast.js — Lightweight toast notification helper.
 * Appends toasts to a fixed container at the bottom-right of the screen.
 */
const Toast = (() => {
  let container = null;

  function getContainer() {
    if (!container) {
      container = document.createElement('div');
      container.className = 'toast-container';
      container.setAttribute('aria-live', 'polite');
      container.setAttribute('aria-atomic', 'false');
      document.body.appendChild(container);
    }
    return container;
  }

  /**
   * Show a toast message.
   * @param {string} message
   * @param {'info'|'success'|'danger'|'warning'} [type='info']
   * @param {number} [duration=4000] — ms before auto-dismiss
   */
  function show(message, type = 'info', duration = 4000) {
    const el = document.createElement('div');
    el.className = `game-toast${type !== 'info' ? ` game-toast--${type}` : ''}`;
    el.textContent = message;
    getContainer().appendChild(el);

    setTimeout(() => {
      el.style.transition = 'opacity 0.3s';
      el.style.opacity = '0';
      setTimeout(() => el.remove(), 300);
    }, duration);
  }

  return {
    info:    (msg, ms) => show(msg, 'info', ms),
    success: (msg, ms) => show(msg, 'success', ms),
    danger:  (msg, ms) => show(msg, 'danger', ms),
    warning: (msg, ms) => show(msg, 'warning', ms)
  };
})();
