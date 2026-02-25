/**
 * game.js — Main game view: polls player state, renders the full game UI.
 * Features: IsPaused freeze, "Your turn" modal, "Time's up" modal.
 */
const GameModule = (() => {
  let _roomCode  = null;
  let _playerId  = null;
  let _poller    = null;
  let _lastPhase = null;
  let _wordRevealed = false;
  let _timerInterval = null;

  // Turn-modal state — track last turn so we fire each only once per turn
  let _lastTurnId    = null;  // playerId who owns the current turn
  let _speakShown    = false; // "your turn" modal already shown this turn
  let _timesUpShown  = false; // "time's up" modal already shown this turn
  let _gameOverShown = false; // game-over modal already shown for this ended round

  function init() {
    _roomCode = sessionStorage.getItem('roomCode');
    _playerId = sessionStorage.getItem('playerId');
    if (!_roomCode || !_playerId) {
      location.href = '/';
      return;
    }

    document.getElementById('btn-reveal-word').addEventListener('click', showWordModal);
    document.getElementById('word-display').addEventListener('click', revealWord);

    VoteModule.init(_roomCode, _playerId);

    _poller = Polling.create({
      fetch: () => Api.getPlayerState(_roomCode, _playerId),
      onData: render,
      onError: (err) => {
        if (err.status !== 404) return;
        _poller.stop();
        Toast.danger('You have been removed from the game.');
        setTimeout(() => { location.href = `/?room=${_roomCode}`; }, 2000);
      },
      interval: 2000
    });
    _poller.start();
  }

  function render(state) {
    if (!state) return;
    const phase  = state.phase || 'Waiting';
    const status = state.room?.status || 'Waiting';
    const players = state.players || [];

    updatePhase(phase, status, state.winner);
    renderPlayers(players, state.currentTurnPlayerId);
    updateTimer(state);

    // Update current speaker info
    const speakerEl = document.getElementById('current-speaker-info');
    if (phase === 'Discussion' && state.currentTurnPlayerId) {
      const speaker = players.find(p => p.playerId === state.currentTurnPlayerId);
      speakerEl.textContent = `🎤 ${speaker?.nickname ?? '…'} is speaking`;
    } else if (phase === 'Voting') {
      speakerEl.textContent = '🗳️ Voting — cast your vote!';
    } else if (phase === 'Ended') {
      speakerEl.textContent = 'Round ended. Ask the Admin to start the next round.';
    } else {
      speakerEl.textContent = 'Waiting for the round to start…';
    }

    // Detect turn change — reset per-turn modal flags
    if (state.currentTurnPlayerId !== _lastTurnId) {
      _lastTurnId   = state.currentTurnPlayerId;
      _speakShown   = false;
      _timesUpShown = false;
    }

    // Show "your turn to speak" modal once when it becomes our turn
    if (phase === 'Discussion'
        && state.currentTurnPlayerId === _playerId
        && !_speakShown
        && !state.isPaused) {
      _speakShown = true;
      showSpeakModal();
    }

    if (phase !== _lastPhase) {
      // Reset game-over flag when leaving the Ended phase (new round started)
      if (_lastPhase === 'Ended' && phase !== 'Ended') {
        _gameOverShown = false;
      }
      _lastPhase = phase;
      onPhaseChange(phase, state);
    }

    // Show game-over modal once when round ends with a winner
    if (phase === 'Ended' && state.winner && !_gameOverShown) {
      _gameOverShown = true;
      showGameOverModal(state.winner, state.spyNickname);
    }

    // Show/hide vote section — hidden if not Voting phase or if this player is eliminated
    const voteSection = document.getElementById('section-vote');
    const isSelfEliminated = players.find(p => p.playerId === _playerId)?.isEliminated ?? false;
    voteSection.classList.toggle('d-none', phase !== 'Voting' || isSelfEliminated);
    if (phase === 'Voting' && !isSelfEliminated) VoteModule.render(players, state);

    // My word button
    document.getElementById('btn-reveal-word').classList.toggle(
      'd-none', !state.myWord || status === 'Waiting');
    if (state.myWord) {
      document.getElementById('word-display').dataset.word = state.myWord;
    }
  }

  function onPhaseChange(phase, state) {
    if (phase === 'Voting') {
      VoteModule.reset();
      Toast.warning('🗳️ Voting phase! Choose who to eliminate.');
    } else if (phase === 'Ended') {
      const winner = state.winner;
      if (winner === 'spy')       Toast.danger('🕵️ The spy wins!');
      else if (winner === 'civilians') Toast.success('🎉 Civilians win!');
      else                        Toast.danger('⚡ Round ended!');
    } else if (phase === 'Discussion')
      Toast.info('💬 Discussion phase started!');
  }

  function updatePhase(phase, status, winner) {
    const badge = document.getElementById('phase-badge');
    let display, cssKey;
    if (status === 'Ended') {
      display = winner === 'spy' ? 'Spy Won' : winner === 'civilians' ? 'Civilians Won' : 'Ended';
      cssKey  = winner === 'spy' ? 'spywon' : winner === 'civilians' ? 'civilianswon' : 'ended';
    } else {
      display = phase;
      cssKey  = phase.toLowerCase();
    }
    badge.textContent = display;
    badge.className = `phase-badge phase-badge--${cssKey}`;
    badge.setAttribute('aria-label', `Current phase: ${display}`);
  }

  function renderPlayers(players, currentTurnId) {
    const list = document.getElementById('player-list');
    list.innerHTML = '';
    players.forEach(p => {
      const isTurn = p.playerId === currentTurnId;
      const li = document.createElement('li');
      li.className = [
        'player-card',
        p.isEliminated   ? 'player-card--eliminated'   : '',
        isTurn           ? 'player-card--current-turn' : ''
      ].filter(Boolean).join(' ');

      li.innerHTML = `
        <span class="player-card__avatar" aria-hidden="true">
          ${escHtml(p.nickname.charAt(0).toUpperCase())}
        </span>
        <span class="player-card__name">${escHtml(p.nickname)}</span>
        <span class="player-card__badges">
          ${isTurn ? '<span class="badge bg-warning text-dark">🎤 Speaking</span>' : ''}
          ${p.isEliminated ? '<span class="badge bg-danger">Eliminated</span>'
                           : '<span class="badge bg-success">Active</span>'}
          ${p.playerId === _playerId ? '<span class="badge bg-secondary">You</span>' : ''}
        </span>`;
      list.appendChild(li);
    });
  }

  function updateTimer(state) {
    clearInterval(_timerInterval);

    const phase      = state.phase || 'Waiting';
    const turnEndsAt = state.turnEndsAt;
    const isPaused   = state.isPaused || false;
    const pausedSecs = state.pausedSecondsRemaining || 0;
    const totalSec   = phase === 'Voting'
      ? (state.room?.votingDurationSeconds || 120)
      : (state.room?.turnDurationSeconds  || 60);

    if (!turnEndsAt || (phase !== 'Discussion' && phase !== 'Voting')) {
      setTimerBar(0, 0, false, false);
      document.getElementById('timer-label').textContent = '';
      return;
    }

    if (isPaused) {
      const pct = (pausedSecs / totalSec) * 100;
      setTimerBar(pct, pausedSecs, true, true);
      document.getElementById('timer-label').textContent =
        `${Math.ceil(pausedSecs)}s — PAUSED`;
      return;
    }

    const isMyTurn = state.currentTurnPlayerId === _playerId && phase === 'Discussion';

    function tick() {
      const remaining = Math.max(0, (new Date(turnEndsAt) - Date.now()) / 1000);
      const pct = (remaining / totalSec) * 100;
      setTimerBar(pct, remaining, true, false);
      document.getElementById('timer-label').textContent =
        `${Math.ceil(remaining)}s remaining`;

      // Show "time's up" to the speaking player when their turn expires
      if (remaining <= 0) {
        clearInterval(_timerInterval);
        if (isMyTurn && !_timesUpShown) {
          _timesUpShown = true;
          showTimesUpModal();
        }
      }
    }
    tick();
    _timerInterval = setInterval(tick, 500);
  }

  function setTimerBar(pct, remaining, visible, paused) {
    const bar  = document.getElementById('timer-bar');
    const wrap = document.getElementById('timer-bar-wrap');
    wrap.classList.toggle('d-none', !visible);
    bar.style.width = `${pct}%`;
    if (paused) {
      bar.className = 'progress-bar timer-bar timer-bar--caution';
    } else {
      bar.className = 'progress-bar timer-bar ' + (
        pct > 50 ? 'timer-bar--safe' :
        pct > 20 ? 'timer-bar--caution' :
                   'timer-bar--danger'
      ) + (remaining <= 5 ? ' timer-bar--pulse' : '');
    }
  }

  function showWordModal() {
    _wordRevealed = false;
    document.getElementById('word-display').classList.remove('word-reveal--visible');
    const modal = new bootstrap.Modal(document.getElementById('wordModal'));
    modal.show();
  }

  function showSpeakModal() {
    const el = document.getElementById('speakModal');
    // Only show if no other modal is open
    if (document.querySelector('.modal.show')) return;
    const modal = new bootstrap.Modal(el);
    modal.show();
  }

  function showTimesUpModal() {
    // Dismiss any open modal first (e.g. speak modal still up), then show times-up.
    // After the modal is dismissed, call yieldTurn so the turn advances immediately
    // without requiring admin action. Errors are swallowed — if the admin or the
    // auto-advance timer already moved the turn on, the server returns 400 (benign).
    const open = document.querySelector('.modal.show');
    const doShow = () => {
      const el = document.getElementById('timesUpModal');
      el.addEventListener('hidden.bs.modal', async () => {
        try { await Api.yieldTurn(_roomCode, _playerId); } catch (_) {}
      }, { once: true });
      new bootstrap.Modal(el).show();
    };
    if (open) {
      const existing = bootstrap.Modal.getInstance(open);
      existing?.hide();
      open.addEventListener('hidden.bs.modal', doShow, { once: true });
    } else {
      doShow();
    }
  }

  function revealWord() {
    if (_wordRevealed) return;
    _wordRevealed = true;
    const el = document.getElementById('word-display');
    el.classList.add('word-reveal--visible');
    el.textContent = el.dataset.word || '';
  }

  function showGameOverModal(winner, spyNickname) {
    // Dismiss any open modal first, then show game-over
    const open = document.querySelector('.modal.show');
    const doShow = () => {
      const spyWon = winner === 'spy';
      const modalContent = document.getElementById('game-over-modal-content');
      document.getElementById('game-over-icon').textContent = spyWon ? '🕵️' : '🎉';
      document.getElementById('gameOverModalLabel').textContent =
        spyWon ? 'The spy wins!' : 'Civilians win!';
      document.getElementById('game-over-subtitle').textContent =
        spyWon
          ? 'The spy survived and fooled everyone.'
          : 'The civilians found the spy!';
      document.getElementById('game-over-spy-reveal').textContent =
        spyNickname ? `🕵️ The spy was: ${spyNickname}` : '';
      modalContent.style.border = `2px solid ${spyWon
        ? 'var(--color-danger)' : 'var(--color-success)'}`;
      new bootstrap.Modal(document.getElementById('gameOverModal')).show();
    };
    if (open) {
      const existing = bootstrap.Modal.getInstance(open);
      existing?.hide();
      open.addEventListener('hidden.bs.modal', doShow, { once: true });
    } else {
      doShow();
    }
  }

  function escHtml(s) {
    return s.replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;');
  }

  return { init };
})();

document.addEventListener('DOMContentLoaded', GameModule.init);
