# SEC-012: Personal data in DaluxUser model with no identified access path

| | |
|---|---|
| Severity | Info - Personal data fields exist in a model but no code path exposes them to unauthorized access. The data is not in git, not exported, and not served by a web API. |
| Status | Open |
| Category | Personal data |
| Personal data | Email addresses, first names, last names — stored in `DaluxUser` model, no external consumers identified |
| Location | `src/Assistant/dotnet/DaluxCloudUpload/Models/DaluxModels.cs:634-653` (DaluxUser model definition) |
| First seen | 20261030-034207 |
| Last verified | 20261030-034207 |
| Introduced | Not determined |

## What

The `DaluxUser` class (a DTO for the external Dalux API) contains three personal data fields: `Email`, `FirstName`, and `LastName`. The `DaluxApiService` class provides a `GetProjectUsersAsync` method that fetches a list of `DaluxUser` objects from the Dalux API for a given project ID. This is a CLI extension that calls the Dalux API — it is not a web service that serves data to callers.

## Evidence

- Model definition: `src/Assistant/dotnet/DaluxCloudUpload/Models/DaluxModels.cs:634-653`:
  - `Email` (string, line 643)
  - `FirstName` (string?, line 646)
  - `LastName` (string?, line 649)
- API method: `src/Assistant/dotnet/DaluxCloudUpload/Services/DaluxApiService.cs:837`:
  ```csharp
  public async Task<DaluxApiResponse<List<DaluxUser>>> GetProjectUsersAsync(string projectId, CancellationToken [value redacted] = default)
  ```
- No callers of `GetProjectUsersAsync` exist anywhere in the codebase (confirmed via `grep -rn`).
- `DaluxCloudDownload` (the sibling project) does not reference `DaluxUser`.
- No CSV/Excel export, mail, or log functionality references these fields.

## Impact

Personal data (names, email addresses) exists in the model as a DTO for an external API. No code path within this repository exposes it to unauthorized parties:

1. **Who can read it**: The CLI calls the Dalux API to fetch data; there is no inbound HTTP endpoint that serves this data to other users.
2. **Where it leaves the system**: The data is sent TO Dalux (via the API client), not exported from this repo. No mail, export, or log paths identified.
3. **What it is kept for**: No persistence or storage in this repo; data is held transiently during API calls.

However, the `GetProjectUsersAsync` method itself returns `List<DaluxUser>` including all personal data fields. If a caller invokes it, they receive names and emails of all project members. The method should be checked by reviewers to confirm that callers apply appropriate project-level authorization.

## References

- security-facts.md section 5: 3 stored fields, 0 file handlers, 0 routes
- SEC-002 (Dalux API key handling) — related to Dalux integration security
