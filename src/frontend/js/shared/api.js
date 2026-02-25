/**
 * api.js — Thin fetch wrapper for all API calls.
 * Exposes a single `Api` object; all methods return parsed JSON or throw on error.
 */
const Api = (() => {
  async function request(method, path, body, headers = {}) {
    const opts = {
      method,
      headers: { 'Content-Type': 'application/json', ...headers }
    };
    if (body !== undefined) opts.body = JSON.stringify(body);
    const res = await fetch(path, opts);
    if (!res.ok) {
      const text = await res.text().catch(() => res.statusText);
      const err = new Error(text || `HTTP ${res.status}`);
      err.status = res.status;
      throw err;
    }
    const ct = res.headers.get('Content-Type') || '';
    return ct.includes('application/json') ? res.json() : null;
  }

  return {
    /** Create a room. Requires adminPass. */
    createRoom: (adminPass, maxPlayers, turnDurationSeconds, votingDurationSeconds) =>
      request('POST', '/api/rooms', { maxPlayers, turnDurationSeconds, votingDurationSeconds }, { AdminPass: adminPass }),

    /** Get room metadata. */
    getRoom: (code) => request('GET', `/api/rooms/${code}`),

    /** Join a room with a nickname. Returns { playerId }. */
    joinRoom: (code, nickname) =>
      request('POST', `/api/rooms/${code}/players`, { nickname }),

    /** List players in a room. */
    getPlayers: (code) => request('GET', `/api/rooms/${code}/players`),

    /** Leave a room. */
    leaveRoom: (code, playerId) =>
      request('DELETE', `/api/rooms/${code}/players/${playerId}`),

    /** Start a new round. Requires adminPass. */
    startRound: (code, adminPass) =>
      request('POST', `/api/rooms/${code}/rounds/start`, {}, { AdminPass: adminPass }),

    /** Advance the current round phase / turn. Requires adminPass. */
    advanceRound: (code, adminPass) =>
      request('POST', `/api/rooms/${code}/rounds/advance`, {}, { AdminPass: adminPass }),

    /** Skip remaining discussion turns and jump straight to Voting. Requires adminPass. */
    skipToVoting: (code, adminPass) =>
      request('POST', `/api/rooms/${code}/rounds/skip-to-voting`, {}, { AdminPass: adminPass }),

    /** Cast a vote. */
    castVote: (code, voterId, targetId) =>
      request('POST', `/api/rooms/${code}/votes`, { voterId, targetId }),

    /** Full game state (admin/observer view). */
    getState: (code) => request('GET', `/api/rooms/${code}/state`),

    /** Player-specific state including their word. */
    getPlayerState: (code, playerId) =>
      request('GET', `/api/rooms/${code}/state/player/${playerId}`),

    /** Admin state — includes IsSpy per player and both words. Requires adminPass. */
    getAdminState: (code, adminPass) =>
      request('GET', `/api/rooms/${code}/state/admin`, undefined, { AdminPass: adminPass }),

    /** Admin-kick a player (removes them from the room). Requires adminPass. */
    kickPlayer: (code, playerId, adminPass) =>
      request('POST', `/api/rooms/${code}/players/${playerId}/kick`, {}, { AdminPass: adminPass }),

    /** Pause the current turn timer. Requires adminPass. */
    pauseTimer: (code, adminPass) =>
      request('POST', `/api/rooms/${code}/timer/pause`, {}, { AdminPass: adminPass }),

    /** Resume a paused timer. Requires adminPass. */
    resumeTimer: (code, adminPass) =>
      request('POST', `/api/rooms/${code}/timer/resume`, {}, { AdminPass: adminPass }),

    /** Update turn/voting durations. Requires adminPass. */
    setTurnDuration: (code, adminPass, turnDurationSeconds, votingDurationSeconds, applyNow) =>
      request('POST', `/api/rooms/${code}/timer/duration`,
        { turnDurationSeconds, votingDurationSeconds, applyNow }, { AdminPass: adminPass }),

    /** Reset the game to Waiting state. Requires adminPass. */
    resetGame: (code, adminPass) =>
      request('POST', `/api/rooms/${code}/reset`, {}, { AdminPass: adminPass }),

    /** Yield the current speaker's turn (player self-advance). No adminPass required. */
    yieldTurn: (code, playerId) =>
      request('POST', `/api/rooms/${code}/rounds/yield`, { playerId })
  };
})();
