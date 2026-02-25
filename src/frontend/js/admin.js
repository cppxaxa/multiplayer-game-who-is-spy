/**
 * admin.js — Admin panel: create room, manage round lifecycle, watch player list.
 * Features: kick player, pause/resume timer, configure turn durations, force-skip turn.
 */
const AdminModule = (() => {
  let _roomCode  = null;
  let _adminPass = null;
  let _poller    = null;
  let _isPaused  = false;
  let _timerInterval = null;
  let _currentState  = null;
  let _durationsLoaded = false;
  let _gameOverShown = false;

  function init() {
    const params = new URLSearchParams(location.search);
    const existingCode = params.get('room');
    if (existingCode) {
      _roomCode  = existingCode.toUpperCase();
      _adminPass = sessionStorage.getItem('adminPass');
      showRoomPanel();
    }

    document.getElementById('btn-create-room').addEventListener('click', onCreateRoom);
    document.getElementById('btn-start-round').addEventListener('click', onStartRound);
    document.getElementById('btn-advance').addEventListener('click', onAdvance);
    document.getElementById('btn-skip-to-voting').addEventListener('click', onSkipToVoting);
    document.getElementById('btn-pause-resume').addEventListener('click', onPauseResume);
    document.getElementById('btn-copy-link').addEventListener('click', onCopyLink);
    document.getElementById('btn-apply-duration').addEventListener('click', onApplyDuration);
    document.getElementById('btn-reset-game').addEventListener('click', onResetGame);

    // Live-update duration labels
    document.getElementById('input-turn-secs').addEventListener('input', e => {
      document.getElementById('turn-secs-display').textContent = e.target.value + 's';
    });
    document.getElementById('input-voting-secs').addEventListener('input', e => {
      document.getElementById('voting-secs-display').textContent = e.target.value + 's';
    });
    document.getElementById('input-turn-secs-create').addEventListener('input', e => {
      document.getElementById('turn-secs-create-display').textContent = e.target.value + 's';
    });
    document.getElementById('input-voting-secs-create').addEventListener('input', e => {
      document.getElementById('voting-secs-create-display').textContent = e.target.value + 's';
    });
  }

  async function onCreateRoom() {
    const pass    = document.getElementById('input-admin-pass').value.trim();
    const max     = parseInt(document.getElementById('input-max-players').value, 10) || 20;
    const turnSec = parseInt(document.getElementById('input-turn-secs-create').value, 10) || 60;
    const voteSec = parseInt(document.getElementById('input-voting-secs-create').value, 10) || 120;
    if (!pass) return Toast.warning('Enter admin password.');
    try {
      const room = await Api.createRoom(pass, max, turnSec, voteSec);
      _adminPass = pass;
      _roomCode  = room.roomCode;
      sessionStorage.setItem('adminPass', pass);
      showRoomPanel();
      Toast.success(`Room ${_roomCode} created!`);
    } catch (e) {
      Toast.danger(`Failed: ${e.message}`);
    }
  }

  async function onStartRound() {
    try {
      await Api.startRound(_roomCode, _adminPass);
      Toast.success('Round started!');
    } catch (e) {
      Toast.danger(`Failed: ${e.message}`);
    }
  }

  async function onAdvance() {
    try {
      await Api.advanceRound(_roomCode, _adminPass);
      Toast.info('Advanced to next turn.');
    } catch (e) {
      Toast.danger(`Failed: ${e.message}`);
    }
  }

  async function onSkipToVoting() {
    if (!confirm('Skip all remaining discussion turns and start voting now?')) return;
    try {
      await Api.skipToVoting(_roomCode, _adminPass);
      Toast.warning('Skipped to voting.');
    } catch (e) {
      Toast.danger(`Failed: ${e.message}`);
    }
  }

  async function onPauseResume() {
    try {
      if (_isPaused) {
        await Api.resumeTimer(_roomCode, _adminPass);
        Toast.info('Timer resumed.');
      } else {
        await Api.pauseTimer(_roomCode, _adminPass);
        Toast.warning('Timer paused.');
      }
    } catch (e) {
      Toast.danger(`Failed: ${e.message}`);
    }
  }

  async function onKickPlayer(playerId, nickname) {
    if (!confirm(`Kick ${nickname}? They will be removed from the game.`)) return;
    try {
      await Api.kickPlayer(_roomCode, playerId, _adminPass);
      Toast.warning(`${nickname} was kicked.`);
    } catch (e) {
      Toast.danger(`Failed: ${e.message}`);
    }
  }

  async function onApplyDuration() {
    const turnSec = parseInt(document.getElementById('input-turn-secs').value, 10) || 60;
    const voteSec = parseInt(document.getElementById('input-voting-secs').value, 10) || 120;
    try {
      await Api.setTurnDuration(_roomCode, _adminPass, turnSec, voteSec, true);
      Toast.success('Duration updated and applied now.');
    } catch (e) {
      Toast.danger(`Failed: ${e.message}`);
    }
  }

  async function onResetGame() {
    if (!confirm('Reset the game? All round data will be cleared and players will return to Waiting state.')) return;
    try {
      await Api.resetGame(_roomCode, _adminPass);
      _durationsLoaded = false;  // allow sliders to re-sync from server on next poll
      Toast.success('Game reset! Ready to start a new round.');
    } catch (e) {
      Toast.danger(`Failed: ${e.message}`);
    }
  }

  function onCopyLink() {
    const link = `${location.origin}/?room=${_roomCode}`;
    navigator.clipboard.writeText(link)
      .then(() => Toast.success('Link copied!'))
      .catch(() => Toast.warning('Copy failed — copy manually.'));
  }

  function showRoomPanel() {
    document.getElementById('section-create').classList.add('d-none');
    document.getElementById('section-room').classList.remove('d-none');
    document.getElementById('display-room-code').textContent = _roomCode;
    const link = `${location.origin}/?room=${_roomCode}`;
    const linkEl = document.getElementById('display-room-link');
    linkEl.href = link;
    linkEl.textContent = link;
    history.replaceState(null, '', `?room=${_roomCode}`);
    startPolling();
  }

  function startPolling() {
    _poller = Polling.create({
      fetch: () => Api.getAdminState(_roomCode, _adminPass),
      onData: renderState,
      onError: () => {}
    });
    _poller.start();
  }

  function renderState(state) {
    if (!state) return;
    _currentState = state;

    const phase  = state.phase  || '—';
    const status = state.room?.status || 'Waiting';
    const round  = state.room?.currentRound || 0;
    const players = state.players || [];
    const winner = state.winner || '';
    _isPaused = state.isPaused || false;

    // Derive a human-readable end label when the game is over
    const endLabel = winner === 'spy' ? 'Spy Won' : winner === 'civilians' ? 'Civilians Won' : 'Ended';

    // Phase badge (card)
    document.getElementById('display-phase').textContent =
      status === 'Ended' ? endLabel : (round > 0 ? `Round ${round} · ${phase}` : status);

    // Phase badge (header) — same colour coding as the player game view
    const headerBadge = document.getElementById('phase-badge');
    const headerPhase = status === 'Ended' ? endLabel : (round > 0 ? phase : 'Waiting');
    const headerCssKey = status === 'Ended'
      ? (winner === 'spy' ? 'spywon' : 'civilianswon')
      : headerPhase.toLowerCase();
    headerBadge.textContent = round > 0 ? `Round ${round} · ${headerPhase}` : headerPhase;
    headerBadge.className = `phase-badge phase-badge--${headerCssKey}`;
    headerBadge.setAttribute('aria-label', `Current phase: ${headerPhase}`);
    headerBadge.classList.remove('d-none');

    // Pause/resume button
    const btnPR = document.getElementById('btn-pause-resume');
    btnPR.textContent = _isPaused ? '▶ Resume Timer' : '⏸ Pause Timer';
    btnPR.className = _isPaused
      ? 'btn btn-success btn-sm flex-grow-1'
      : 'btn btn-warning btn-sm flex-grow-1';
    // Only show during Discussion or Voting
    const timerActive = phase === 'Discussion' || phase === 'Voting';
    btnPR.classList.toggle('d-none', !timerActive || status === 'Ended');

    // Skip-to-voting button — only useful while discussion is still running
    document.getElementById('btn-skip-to-voting')
      .classList.toggle('d-none', phase !== 'Discussion' || status === 'Ended');

    // Admin timer bar
    updateAdminTimer(state);

    // Sync live duration sliders with current room settings — load once only so
    // the admin's in-progress slider adjustments are not clobbered on the next poll.
    if (!_durationsLoaded && state.room?.turnDurationSeconds) {
      _durationsLoaded = true;
      const td = state.room.turnDurationSeconds;
      const vd = state.room.votingDurationSeconds;
      document.getElementById('input-turn-secs').value = td;
      document.getElementById('turn-secs-display').textContent = td + 's';
      document.getElementById('input-voting-secs').value = vd;
      document.getElementById('voting-secs-display').textContent = vd + 's';
    }

    // Word pair strip
    const wordStrip = document.getElementById('display-words');
    if (state.civilianWord && state.spyWord) {
      wordStrip.classList.remove('d-none');
      document.getElementById('display-civilian-word').textContent = state.civilianWord;
      document.getElementById('display-spy-word').textContent = state.spyWord;
    } else {
      wordStrip.classList.add('d-none');
    }

    // Player count summary
    const active     = players.filter(p => !p.isEliminated).length;
    const eliminated = players.filter(p =>  p.isEliminated).length;
    document.getElementById('player-count-summary').textContent =
      `${players.length} total · ${active} active · ${eliminated} eliminated`;

    renderPlayers(players, state.currentTurnPlayerId);
    renderVotes(state.votes || [], players, phase);

    // Reset flag when a new game starts (phase no longer Ended)
    if (status !== 'Ended' && _gameOverShown) _gameOverShown = false;

    // Show game-over modal once when round ends with a winner
    if (status === 'Ended' && winner && !_gameOverShown) {
      _gameOverShown = true;
      showGameOverModal(winner, state.spyNickname);
    }
  }

  function updateAdminTimer(state) {
    clearInterval(_timerInterval);

    const wrap  = document.getElementById('admin-timer-wrap');
    const bar   = document.getElementById('admin-timer-bar');
    const label = document.getElementById('admin-timer-label');
    const pauseLabel = document.getElementById('admin-timer-paused');

    const phase = state.phase;
    if (!state.turnEndsAt || (phase !== 'Discussion' && phase !== 'Voting')) {
      wrap.classList.add('d-none');
      return;
    }

    wrap.classList.remove('d-none');
    const totalSec = phase === 'Voting'
      ? (state.room?.votingDurationSeconds || 120)
      : (state.room?.turnDurationSeconds  || 60);

    if (state.isPaused) {
      const rem = state.pausedSecondsRemaining || 0;
      const pct = (rem / totalSec) * 100;
      bar.style.width = `${pct}%`;
      bar.className = 'progress-bar timer-bar timer-bar--caution';
      label.textContent = `${Math.ceil(rem)}s — PAUSED`;
      pauseLabel.classList.remove('d-none');
      return;
    }

    pauseLabel.classList.add('d-none');

    function tick() {
      const remaining = Math.max(0, (new Date(state.turnEndsAt) - Date.now()) / 1000);
      const pct = (remaining / totalSec) * 100;
      bar.style.width = `${pct}%`;
      bar.className = 'progress-bar timer-bar ' + (
        pct > 50 ? 'timer-bar--safe' :
        pct > 20 ? 'timer-bar--caution' : 'timer-bar--danger'
      ) + (remaining <= 5 ? ' timer-bar--pulse' : '');
      label.textContent = `${Math.ceil(remaining)}s remaining`;
      if (remaining <= 0) clearInterval(_timerInterval);
    }
    tick();
    _timerInterval = setInterval(tick, 500);
  }

  function renderPlayers(players, currentTurnId) {
    const list = document.getElementById('player-list');
    list.innerHTML = '';
    if (!players.length) {
      list.innerHTML = '<li class="text-muted small p-2">No players yet…</li>';
      return;
    }
    players.forEach(p => {
      const isTurn = p.playerId === currentTurnId;
      const isSpy  = p.isSpy === true;

      const li = document.createElement('li');
      li.className = [
        'player-card',
        p.isEliminated ? 'player-card--eliminated' : '',
        isTurn         ? 'player-card--current-turn' : ''
      ].filter(Boolean).join(' ');

      li.innerHTML = `
        <span class="player-card__avatar" aria-hidden="true">
          ${escHtml(p.nickname.charAt(0).toUpperCase())}
        </span>
        <span class="player-card__name">${escHtml(p.nickname)}</span>
        <span class="player-card__badges">
          ${isTurn ? '<span class="badge bg-warning text-dark">🎤 Speaking</span>' : ''}
          ${isSpy  ? '<span class="badge bg-danger">🕵️ Spy</span>'
                   : '<span class="badge bg-primary">👤 Civilian</span>'}
          ${p.isEliminated
              ? '<span class="badge bg-secondary">Eliminated</span>'
              : '<span class="badge bg-success">Active</span>'}
        </span>
        ${!p.isEliminated
          ? `<button class="btn btn-outline-danger btn-sm ms-auto kick-btn"
               data-id="${escHtml(p.playerId)}" data-name="${escHtml(p.nickname)}"
               aria-label="Kick ${escHtml(p.nickname)}">Kick</button>`
          : ''}`;
      list.appendChild(li);
    });

    // Wire kick buttons
    list.querySelectorAll('.kick-btn').forEach(btn => {
      btn.addEventListener('click', () => onKickPlayer(btn.dataset.id, btn.dataset.name));
    });
  }

  function renderVotes(votes, players, phase) {
    const wrap = document.getElementById('vote-roster-wrap');
    const list = document.getElementById('vote-roster-list');

    if (phase !== 'Voting' || !wrap) return wrap?.classList.add('d-none');

    wrap.classList.remove('d-none');

    const active = players.filter(p => !p.isEliminated);
    const castCount = votes.length;
    const totalCount = active.length;

    document.getElementById('vote-roster-progress').textContent =
      `${castCount} / ${totalCount} voted`;

    list.innerHTML = '';
    if (!castCount) {
      list.innerHTML = '<li class="text-muted small p-2">No votes cast yet…</li>';
      return;
    }

    // Build tally: targetId → count
    const tally = {};
    votes.forEach(v => { tally[v.targetPlayerId] = (tally[v.targetPlayerId] || 0) + 1; });
    const maxVotes = Math.max(...Object.values(tally));

    votes.forEach(v => {
      const li = document.createElement('li');
      li.className = 'vote-row';
      li.innerHTML = `
        <span class="vote-row__voter">${escHtml(v.voterNickname)}</span>
        <span class="vote-row__arrow" aria-hidden="true">→</span>
        <span class="vote-row__target ${tally[v.targetPlayerId] === maxVotes ? 'vote-row__target--leading' : ''}">
          ${escHtml(v.targetNickname)}
          <span class="badge bg-secondary ms-1">${tally[v.targetPlayerId]}</span>
        </span>`;
      list.appendChild(li);
    });
  }

  function showGameOverModal(winner, spyNickname) {
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
    return String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;')
                    .replace(/"/g,'&quot;');
  }

  return { init };
})();

document.addEventListener('DOMContentLoaded', AdminModule.init);
