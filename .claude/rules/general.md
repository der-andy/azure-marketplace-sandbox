## Language
- ALL written output must be in English: code, comments, commits, docs, specs, context entries, UI text
- No exceptions — even if the user writes in another language, all artifacts are English

## Bash Commands
- NEVER chain shell commands with `&&`, `||`, or `;` — permission checks may reject chained commands
- Run each command in a separate Bash call instead
- Parallel independent calls (multiple Bash tool uses in one message) are fine and encouraged

## Human-in-the-Loop
- Always ask for user approval before finalizing deliverables
- Present options using clear choices rather than open-ended questions
- Never proceed to the next workflow phase without user confirmation

## File Handling
- ALWAYS read a file before modifying it - never assume contents from memory
- After context compaction, re-read files before continuing work
- Run `git diff` to verify what has already been changed in this session
- Never guess at import paths, component names, or API routes - verify by reading
