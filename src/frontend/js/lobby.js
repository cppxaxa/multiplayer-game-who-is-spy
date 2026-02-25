/**
 * lobby.js — Join room page: nickname + room code → redirect to game.html.
 */
const LobbyModule = (() => {
  function init() {
    // Pre-fill room code from URL param
    const params = new URLSearchParams(location.search);
    const code = params.get('room');
    if (code) {
      document.getElementById('input-room-code').value = code.toUpperCase();
    }

    document.getElementById('btn-join').addEventListener('click', onJoin);
    document.getElementById('input-room-code').addEventListener('input', e => {
      e.target.value = e.target.value.toUpperCase();
    });
  }

  async function onJoin() {
    const nickname = document.getElementById('input-nickname').value.trim();
    const code     = document.getElementById('input-room-code').value.trim().toUpperCase();
    const errEl    = document.getElementById('join-error');
    errEl.textContent = '';

    if (!nickname) { errEl.textContent = 'Please enter a nickname.'; return; }
    if (code.length !== 6) { errEl.textContent = 'Room code must be 6 characters.'; return; }

    const btn = document.getElementById('btn-join');
    btn.disabled = true;
    btn.textContent = 'Joining…';

    try {
      const result = await Api.joinRoom(code, nickname);
      sessionStorage.setItem('playerId',  result.playerId);
      sessionStorage.setItem('roomCode',  code);
      sessionStorage.setItem('nickname',  nickname);
      location.href = `/game.html?room=${code}`;
    } catch (e) {
      errEl.textContent = e.message || 'Could not join. Check the room code and try again.';
      btn.disabled = false;
      btn.textContent = 'Join Game';
    }
  }

  return { init };
})();

document.addEventListener('DOMContentLoaded', LobbyModule.init);
