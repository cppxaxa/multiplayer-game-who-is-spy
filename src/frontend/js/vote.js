/**
 * vote.js — Voting UI module rendered inside game.html when phase = Voting.
 */
const VoteModule = (() => {
  let _roomCode  = null;
  let _playerId  = null;
  let _votedFor  = null;

  function init(roomCode, playerId) {
    _roomCode = roomCode;
    _playerId = playerId;
  }

  /** Clears the locally remembered vote selection. Call when a new voting phase begins. */
  function reset() {
    _votedFor = null;
  }

  /** Called by GameModule when phase is Voting. */
  function render(players, state) {
    const grid = document.getElementById('vote-grid');
    grid.innerHTML = '';

    const active = players.filter(p => !p.isEliminated && p.playerId !== _playerId);
    if (!active.length) {
      grid.innerHTML = '<p class="text-muted small">No one to vote for.</p>';
      return;
    }

    active.forEach(p => {
      const btn = document.createElement('button');
      btn.className = `vote-btn${_votedFor === p.playerId ? ' vote-btn--selected' : ''}`;
      btn.textContent = p.nickname;
      btn.setAttribute('aria-label', `Vote for ${p.nickname}`);
      btn.setAttribute('aria-pressed', _votedFor === p.playerId ? 'true' : 'false');
      btn.addEventListener('click', () => onVote(p.playerId, p.nickname, btn, grid));
      grid.appendChild(btn);
    });
  }

  async function onVote(targetId, targetName, btn, grid) {
    try {
      await Api.castVote(_roomCode, _playerId, targetId);
      _votedFor = targetId;
      // Update button states
      grid.querySelectorAll('.vote-btn').forEach(b => {
        b.classList.remove('vote-btn--selected');
        b.setAttribute('aria-pressed', 'false');
      });
      btn.classList.add('vote-btn--selected');
      btn.setAttribute('aria-pressed', 'true');
      Toast.success(`Voted for ${targetName}`);
    } catch (e) {
      Toast.danger(`Vote failed: ${e.message}`);
    }
  }

  return { init, reset, render };
})();
