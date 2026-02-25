/**
 * polling.js — Reusable polling helper.
 * Creates a poller that calls a fetch function on an interval and invokes
 * callbacks on success or error.
 */
const Polling = (() => {
  /**
   * @param {Object} opts
   * @param {() => Promise<any>} opts.fetch  — async function to call
   * @param {(data: any) => void} opts.onData — called with successful result
   * @param {(err: Error) => void} [opts.onError] — called on error (optional)
   * @param {number} [opts.interval=2000] — ms between calls
   * @returns {{ start: () => void, stop: () => void }}
   */
  function create({ fetch, onData, onError, interval = 2000 }) {
    let timer = null;
    let running = false;

    async function tick() {
      try {
        const data = await fetch();
        onData(data);
      } catch (err) {
        if (onError) onError(err);
      }
    }

    return {
      start() {
        if (running) return;
        running = true;
        tick(); // immediate first call
        timer = setInterval(tick, interval);
      },
      stop() {
        running = false;
        if (timer !== null) {
          clearInterval(timer);
          timer = null;
        }
      }
    };
  }

  return { create };
})();
