# Agent working notes

Persistent memory of the AEC security review agent for this project. Committed with the findings so every run continues where the last one stopped.

| File | Purpose | Who writes it |
|---|---|---|
| `coverage.md` | Repo map, commit per repo at last run, categories and areas reviewed, backlog of what remains. | Agent |
| `learnings.md` | Standing instructions and lessons. Reviewers add lines under "From humans"; the agent never removes them. | Humans and agent |
| `run-log.md` | The ten most recent runs. Older entries are trimmed; git history is the full record. | Agent (runner writes the header) |
| `pr-summary.md` | Overwritten each run; becomes the pull request description. | Agent |

To dismiss a finding: set its status to `False positive` or `Accepted risk` in `../Findings.md` and on the finding page, and add one line here under "From humans" saying why. The agent stops reporting it and only re-verifies.
