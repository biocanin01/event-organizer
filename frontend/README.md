# EventOrganizer frontend

React 19 and TypeScript client for the EventOrganizer API.

## Development

1. Copy `.env.example` to `.env` when a different API address is needed.
2. Start the backend API on `http://localhost:5117`.
3. Run `npm.cmd run dev` from this folder.

## Quality checks

```text
npm.cmd run typecheck
npm.cmd run lint
npm.cmd run test -- --run
npm.cmd run build
```
