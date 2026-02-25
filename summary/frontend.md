# Design Guidelines

The UI combines a **control-panel layout** (dense, information-visible, RedHat Cockpit-style) with a **game color palette** (vibrant, not corporate). Most game state and actions are visible on a single screen; only word reveal and destructive confirmations use full-screen overlays.

**Stack:** Vanilla JS (preferred) or jQuery · Bootstrap for layout/components · plain CSS for game-specific overrides via custom properties.

## Colors
Override Bootstrap's defaults with CSS custom properties in `:root`:

```css
:root {
  --color-primary:   #FF6B35;  /* energetic orange — CTAs, highlights */
  --color-secondary: #004E89;  /* deep blue — headers, panels */
  --color-success:   #10B981;  /* green — civilian safe, correct */
  --color-danger:    #EF4444;  /* red — elimination, spy reveal */
  --color-warning:   #F59E0B;  /* amber — timer critical, caution */
  --color-bg:        #0F172A;  /* near-black — page background */
  --color-surface:   #1E293B;  /* dark card/panel background */
  --color-text:      #F1F5F9;  /* primary text */
  --color-muted:     #94A3B8;  /* secondary / placeholder text */
}
```

Never use color as the **only** signal — always pair with an icon, label, or badge (important for colorblind players).

## Typography
- Use Bootstrap's default type scale; no custom fonts.
- Avoid large decorative headings — prefer compact labels, badges, and status chips.
- Weights: `400` (body), `600` (labels/status), `700` (scores/round number).
- Relative units (`rem`) throughout — never hard-coded `px` for font sizes.

## Layout & Spacing
- Bootstrap 12-column grid. **Desktop-first** — optimise for laptops and large screens, use available space efficiently.
- Target: all primary game actions visible **without scrolling** on a 1280px wide screen.
- Multi-column layout where space allows (e.g. player list alongside game controls side-by-side).
- Collapsible/accordion sections for secondary options (settings, player list overflow).
- All interactive targets ≥ 44 × 44 px.

## Component Conventions
| Component | Style notes |
|-----------|-------------|
| **Buttons** | Bootstrap `btn` classes + `--color-primary` override; `btn-sm` preferred to keep density |
| **Player list** | Compact table or `list-group`; status badges (Active / Eliminated / Spy✓) inline on each row |
| **Timer bar** | Bootstrap `progress`; color transitions `--color-success → --color-warning → --color-danger`; pulses in last 5 s |
| **Word reveal** | Full-screen Bootstrap modal overlay; large centered text; tap to reveal (blurred until then) |
| **Vote section** | Inline button group listing active player names; selected state uses `--color-primary` |
| **Phase indicator** | Small badge/chip in the header showing current phase: `Waiting` · `Discussion` · `Voting` · `Ended` |

## Responsive Breakpoints
Bootstrap defaults — design down from desktop, not up from mobile:
```
lg  ≥ 992px   (primary target — laptop/desktop)
md  ≥ 768px   (tablet landscape, acceptable degradation)
sm  ≥ 576px   (tablet portrait, minimal support)
xs  < 576px   (not a priority)
```


## Animations
Keep animations purposeful and fast (`150–300ms ease`):
- **Elimination:** player row fades + strikethrough.
- **Spy reveal:** modal scales in with a red flash.
- **Phase change:** badge color transition.
- Always respect `prefers-reduced-motion`:

```css
@media (prefers-reduced-motion: reduce) {
  *, *::before, *::after {
    animation-duration: 0.01ms !important;
    transition-duration: 0.01ms !important;
  }
}
```

## Game Flow Notes

- **Kicked-player redirect:** When an admin kicks a player, the player row is deleted from the table. The kicked player's `game.html` polls `GET /state/player/{id}` every 2 s; on receiving a 404, the poller stops, `Toast.danger('You have been removed from the game.')` is shown, and after 2 s the player is redirected to `/?room={roomCode}` (the join page, which auto-fills the room code). Non-404 poll errors (network blips, 503, etc.) are silently ignored so transient issues don't eject players.

---

# Accessibility guidelines

The game is playable with keyboard-only navigation and screen readers, and meets **WCAG 2.1 AA**.

## Semantic HTML
- Page structure: `<header>`, `<main>`, `<footer>`.
- Player list: `<ul>` / `<li>` (not `<div>`).
- All clickable elements: `<button>` (never a styled `<div>`).
- Inputs always paired with a `<label for="...">`.
- Heading hierarchy: one `<h1>` per page, then `<h2>` / `<h3>` in order.

## ARIA
| Situation | Attribute |
|-----------|-----------|
| Turn / timer updates | `aria-live="polite"` on the container |
| Elimination announcement | `aria-live="assertive" aria-atomic="true"` |
| Modal dialog | `role="dialog" aria-labelledby aria-describedby` |
| Icon-only buttons | `aria-label="Vote for Alice"` |
| Toggle (mute, etc.) | `aria-pressed="true/false"` |
| Loading / polling | `aria-busy="true"` on the relevant region |

Use `aria-live` sparingly — too many live regions create noise for screen reader users.

## Keyboard Navigation
- Every interactive element reachable by `Tab` in logical reading order.
- `Enter` / `Space` activates buttons.
- `Escape` closes modals; focus returns to the trigger element.
- Include a **skip link** as the first focusable element:
  ```html
  <a href="#main-content" class="sr-only-focusable">Skip to main content</a>
  ```

## Contrast
- Normal text: ≥ **4.5 : 1** against its background.
- Large text (≥ 18 pt / 14 pt bold) and UI components: ≥ **3 : 1**.
- Verify with [WebAIM Contrast Checker](https://webaim.org/resources/contrastchecker/) before finalising palette.

## Focus Indicators
Never remove the default outline without a replacement:
```css
:focus-visible {
  outline: 2px solid var(--color-primary);
  outline-offset: 2px;
  border-radius: 4px;
}
```

## Screen Reader Utilities
```css
.sr-only {
  position: absolute;
  width: 1px; height: 1px;
  padding: 0; margin: -1px;
  overflow: hidden;
  clip: rect(0,0,0,0);
  white-space: nowrap;
  border: 0;
}
```
Use `.sr-only` to add context invisible to sighted users, e.g.:
```html
<button>Vote <span class="sr-only">for Player 5</span></button>
```

## Forms
```html
<label for="nickname">Your nickname</label>
<input id="nickname" type="text" required aria-required="true"
       autocomplete="nickname" placeholder="e.g., Alice">

<!-- Inline validation error -->
<div role="alert" aria-live="assertive" id="nickname-error"></div>
```

## Mobile & Touch
- `<meta name="viewport" content="width=device-width, initial-scale=1">` — **do not** set `user-scalable=no`.
- Test with VoiceOver (iOS) and TalkBack (Android).
- All touch targets ≥ 44 × 44 px with ≥ 8 px spacing between them.

## Quick Testing Checklist
- [ ] Tab through the entire UI without a mouse.
- [ ] Run Lighthouse accessibility audit (target score ≥ 90).
- [ ] Verify all contrast ratios meet AA.
- [ ] Enable a screen reader and complete a full game flow.
- [ ] Test with a colorblind simulator (Deuteranopia / Protanopia).
- [ ] Confirm `prefers-reduced-motion` disables all non-essential animation.
