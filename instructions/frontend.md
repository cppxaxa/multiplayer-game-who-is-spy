# Instructions

* Prefer vanilla JS always or jQuery; jQuery UI components are allowed where needed
* Use Bootstrap for layout and components (no other CSS framework)
* Keep your code minimal but easy to read
* Code may get huge, so a compacted summary for AI uses should be put in "summary" folder
* Follow modularity and maintainability rules in `instructions/code-style.md` — each UI section is its own JS module, no inline JS or styles, shared utilities in `src/frontend/js/shared/`
* **No build step** — frontend must require zero compilation or transpilation; files are served and rendered directly from source. This means:
  * No TypeScript, no Babel, no Webpack, no Vite, no bundlers of any kind
  * No npm build scripts; `node_modules` must not be required at runtime
  * Use plain ES6 `<script type="module">` or classic `<script>` tags with CDN-hosted libraries (Bootstrap, jQuery) via `<script src>` — no npm imports in JS files
  * CSS is plain `.css` files — no Sass, Less, or PostCSS

# UX Style

* Layout and density: control-panel style (like RedHat Cockpit) — compact, information-dense, most actions visible on a single screen without scrolling; **desktop-first**, efficiently use the full screen width on laptops and large monitors
* Color: use the game color palette from `summary/frontend.md` (vibrant, not corporate grey)
* Avoid large decorative text; prefer labels, badges, and status indicators
* Actions and game controls are NOT hidden in modals/dialogs — keep them inline or in collapsible/accordion sections to save space
* Dialogs only for destructive confirmations (e.g. "End game?") or word reveal (full-screen overlay)
