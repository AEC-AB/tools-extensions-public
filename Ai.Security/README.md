# Talk to the security agent

This folder is written by the AEC security agent: it reviews this repository for security weaknesses and writes each one up with evidence; it never fixes anything. It runs at night, and only when
something changed - and an edit to this file counts. Write here to steer it or ask it something;
you will find the answer in this file after the next run.

**How to edit.** If a pull request from the agent is open, edit this file on its branch
(`security/ai-security`) - no review needed. Otherwise edit it on the default branch the way you
normally would. Add your text under one of the three headings below. Do not edit the lines the
agent writes (IDs, Status, Answer), except to close an item; add a new line under an answer to follow up.

Rules shared by every repo are kept in `AEC.Nvidia.LLM/services/aec.agents/memory/` and reach this agent on every run; a line under Standing instructions below outranks them for this repo.

## Standing instructions

Rules the agent follows on every run, one per line. Only people write here.

- Report only. Never change code, config or pipelines. Describe what a fix involves; do not write it.
- Never copy a secret value or personal data into any page. Location and kind only.
- Export folders (`*Exports/`) hold customer data files: report their presence in git as a data-protection finding once; do not enumerate their contents.

<!-- Example: - Ignore the folder legacy/: it is no longer deployed. -->

## Requests

One-off work. Start each with a level-3 heading (`### What you want`) and describe it below.
The agent gives it an ID and marks it Done, In progress or Declined, with a reason.

<!-- Example: ### Re-check the authentication of the admin API -->

## Questions and statements

Ask a question, or state something you believe and the agent checks it. Start each with a
level-3 heading (`### Your question`). The agent answers under it, with evidence.
When you are done, change the word after `Status:` on the item's own status line to `Closed`;
closed items move to `.agent/conversation-archive.md`.

<!-- Example: ### Is /api/export reachable without login? -->
