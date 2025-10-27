# Summary

Provide a concise description of the change and its motivation.

- What problem does this solve?
- Is behavior changed or new capability added?

# Linked Items

- Issue: #
- Spec (if existing capability): `openspec/specs/<capability>/spec.md`
- Proposal (if applicable): `openspec/changes/<change-id>/proposal.md`
- Deltas (if applicable): `openspec/changes/<change-id>/specs/<capability>/spec.md`

# Behavior and Compatibility

- What is the expected behavior after this change?
- Backward compatibility notes and migrations:

# Security / Timeouts / Sessions

- Execution tools affected (EnableExecuteQuery/StoredProcedure/StartQuery/StartStoredProcedure):
- Permissions and data-safety impact:
- Timeout handling (connection/command/per-op/total-call/session):
- Session manager impact (creation, retrieval, cleanup):

# Tests

- Unit tests added/updated:
- Integration tests (Docker SQL Server) added/updated:
- Negative/timeout tests included for DB-affecting changes:

# Logging / Errors

- Logs follow `Microsoft.Extensions.Logging` guidance (no secrets):
- Error messages follow pattern: `Error: {context} while {operation}: {details}`:
- Error JSON structure (if applicable) updated/verified:

# Checklist

- [ ] Small, focused PR (prefer <300 LOC diff)
- [ ] Matches approved spec or proposal (if required)
- [ ] Build succeeds (`dotnet build`)
- [ ] Unit tests pass (`dotnet test`)
- [ ] Integration tests pass (if applicable)
- [ ] Coverage ≥ 80% (project-wide)
- [ ] Formatting/style checks pass
- [ ] OpenSpec validation (if applicable):
  - [ ] `openspec validate <change-id> --strict` (proposal PR)
  - [ ] `openspec validate --strict` (spec changes)
- [ ] Security and permissions reviewed
- [ ] Timeout and session behavior verified
- [ ] Changelog/summary updated in this PR description

# Reviewer Notes

- Areas most risky or needing extra attention:
- Performance considerations (hot paths, allocations):
- Follow-up tasks or out-of-scope items:

# Screenshots / Logs (optional)

Attach relevant output, traces, or screenshots if helpful.
