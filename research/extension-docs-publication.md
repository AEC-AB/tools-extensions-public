# Publishing extension documentation to the public repository

Date: 2026-09-22

## Decision

Keep the canonical extension documentation beside the extension API code in the private `AEC-AB/tools` repository, and publish a generated snapshot to `AEC-AB/tools-extensions-public/docs/dotnet/` after a successful public release.

Use the public repository folder as the durable, reviewable copy. Keep `docs/README.md` as the public landing and provenance page and link to it from the root `README.md`. Optionally enable GitHub Pages from that same tree later; do not use the repository wiki as the publication target.

## Why this fits the current repositories

- The canonical source already lives at `src/assistant/CW.Assistant.Extensions.Docs/docs/`, and its `docs/dotnet/**/*.md` files are packed into `CW.Assistant.Extensions.Docs`. This keeps API changes, documentation changes, package contents, and their code review in one source repository ([package project](https://github.com/AEC-AB/tools/blob/develop/src/assistant/CW.Assistant.Extensions.Docs/CW.Assistant.Extensions.Docs.csproj), [documentation root](https://github.com/AEC-AB/tools/tree/develop/src/assistant/CW.Assistant.Extensions.Docs/docs)).
- The public repository already has a `docs/` location and its root README links to `docs/README.md` and pages below it. Retaining that path avoids breaking existing links and gives contributors an obvious entry point ([public documentation folder](https://github.com/AEC-AB/tools-extensions-public/tree/main/docs), [root README](https://github.com/AEC-AB/tools-extensions-public/blob/main/README.md)).
- GitHub renders relative links in repository Markdown, and recognizes README files in a `docs` directory. A committed folder therefore works without a documentation hosting system ([GitHub README documentation](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/about-readmes)).
- Git history already provides versions and diffs. The versioned NuGet package provides the exact offline documentation matching a consumer's resolved API version, so duplicating every release as `docs/v26.x/` folders would add maintenance without improving the normal public entry point.

## Folder, wiki, and Pages comparison

| Option | Review and provenance | Reader experience | Automation fit | Recommendation |
|---|---|---|---|---|
| `tools-extensions-public/docs/` | Normal commits, pull requests, branch protection, CODEOWNERS, and repository history | Native GitHub Markdown, relative links, repository search, and links from the root README | Straight directory synchronization; can also feed Pages | **Use as the published snapshot** |
| Repository wiki | Stored as a separate Git repository; its publication history is detached from normal repository pull requests | Suitable for manually maintained long-form content, but navigation and links follow wiki conventions | Requires a second Git remote and wiki-specific filename/link handling | Do not use for generated API documentation |
| GitHub Pages | Deployment history and a public website, but not by itself a source-of-truth location | Best presentation and search-engine discoverability | Can deploy the committed `docs/` folder with a GitHub Actions workflow | Optional presentation layer in a second phase |

GitHub documents that wikis are separate Git repositories that can be cloned and pushed independently; only the default branch is rendered live. GitHub also notes that search engines index wikis only under restricted conditions and recommends Pages where indexing matters ([editing wiki pages](https://docs.github.com/en/communities/documenting-your-project-with-wikis/adding-or-editing-wiki-pages), [about wikis](https://docs.github.com/en/communities/documenting-your-project-with-wikis/about-wikis)). Those properties make a wiki a poor mirror for documentation whose review belongs with private API source.

GitHub Pages can publish from a repository's `/docs` folder or from a custom Actions workflow. A `README.md` at the publishing-source root is a supported entry file, so the recommended folder can be enabled as a Pages site without moving or duplicating the files ([publishing source](https://docs.github.com/en/pages/getting-started-with-github-pages/configuring-a-publishing-source-for-your-github-pages-site), [Pages entry file](https://docs.github.com/en/pages/getting-started-with-github-pages/creating-a-github-pages-site)).

## Recommended CI/CD design

### Source and destination

- Author reference content only in `AEC-AB/tools/src/assistant/CW.Assistant.Extensions.Docs/docs/dotnet/`.
- Publish that content to `AEC-AB/tools-extensions-public/docs/dotnet/`.
- Treat `docs/dotnet/` as generated. Do not accept independent edits to that subtree.
- Keep `tools-extensions-public/docs/README.md` as the small public landing/provenance page. Update the displayed package version as part of synchronization, while keeping its navigation stable.
- Keep the public root `README.md` links pointed at `./docs/README.md` and its current child paths.

### Release trigger

Publish only after the `CW.Assistant.Extensions.Docs` NuGet package has been built, validated, and successfully pushed for a public release. Use the exact package version and source commit SHA as pipeline inputs. This makes the website snapshot describe a package users can actually restore.

For the existing Azure pipeline, send a `repository_dispatch` event to `tools-extensions-public` after `Push public packages`, conditioned on a stable release tag such as `refs/tags/v*`. The payload should contain the exact package version and source commit SHA. A workflow in the public repository then performs the extraction and pull-request work. Also support a manual rerun with an explicit package version for recovery. Do not synchronize pull-request builds or arbitrary prerelease builds into the public `main` documentation. GitHub documents `repository_dispatch` as the trigger for activity outside GitHub; the receiving workflow must exist on the target repository's default branch ([workflow events](https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows#repository_dispatch)).

The safest artifact boundary is the published `CW.Assistant.Extensions.Docs` package itself:

1. Download the exact package version that was just published.
2. Extract `contentFiles/any/any/Resources/ExtensionDocs/` into a clean staging directory and map it to public `docs/dotnet/`.
3. Validate that `QUICK_START.md` and the expected platform guides exist, reject broken relative links, and scan for accidental private URLs or credentials.
4. Compare the staged tree with `tools-extensions-public/docs/dotnet/`; exit successfully without a commit when there is no difference.
5. Update the version/source provenance in public `docs/README.md`, push an idempotently named branch such as `automation/extension-docs/<package-version>`, and open or update one pull request into `main`.
6. Let branch protection and documentation checks run. Enable auto-merge after required checks if publication must be hands-off; otherwise require a maintainer to merge the generated pull request.

Using the shipped package as the transfer artifact proves that public `docs/dotnet/` and the package documentation are identical. It also avoids granting the public repository any read access to the private `tools` repository. Retry the download for a bounded interval because package indexing can lag behind a successful push.

### Cross-repository authentication

The workflow's built-in `GITHUB_TOKEN` is intentionally limited to the repository that owns the workflow, so it cannot update the public repository from `tools` ([GitHub App authentication in Actions](https://docs.github.com/en/apps/creating-github-apps/authenticating-with-a-github-app/making-authenticated-api-requests-with-a-github-app-in-a-github-actions-workflow)). Use an organization-owned GitHub App rather than a personal access token:

- Install the app only on `AEC-AB/tools-extensions-public`.
- Grant repository `Contents: read and write` and `Pull requests: read and write`; leave Actions, Administration, Workflows, Packages, and organization permissions unset.
- Store the app client ID as a protected variable and the private key as a secret in the CI system.
- Mint a short-lived installation token during the publication job and scope it to the single destination repository. GitHub recommends minimum app permissions and permits installation tokens to be restricted to selected repositories and permissions ([GitHub App best practices](https://docs.github.com/en/apps/creating-github-apps/about-creating-github-apps/best-practices-for-creating-a-github-app), [official token action](https://github.com/actions/create-github-app-token/blob/main/README.md)).
- Use that token only to send the dispatch and, if chosen for the target workflow, to create/update the synchronization branch and pull request. Do not grant branch-protection bypass. The public workflow can instead use its own `GITHUB_TOKEN` with `contents: write` and `pull-requests: write` if organization settings permit Actions to create pull requests, but App-created pull requests avoid the special approval-required workflow behavior documented for pull requests created by `GITHUB_TOKEN` ([token-triggered workflows](https://docs.github.com/en/actions/concepts/security/github_token#when-github_token-triggers-workflow-runs)).

A repository-scoped write deploy key is technically sufficient for a direct Git push, but it cannot create or update the pull request without a second credential. GitHub documents that writable deploy keys grant repository write access and are tied to a single repository ([deploy keys](https://docs.github.com/en/authentication/connecting-to-github-with-ssh/managing-deploy-keys)). A GitHub App is the cleaner least-privilege identity for both Git and pull-request operations.

### GitHub Pages, if desired

First ship the synchronized folder and correct README links. Then optionally enable Pages in `tools-extensions-public` with `docs/` on `main` as the publishing source. This keeps the website derived from the reviewed public snapshot.

If a custom site generator is introduced later, keep its source in `docs/` and deploy only a generated Pages artifact. A custom Pages workflow needs `contents: read`, `pages: write`, and `id-token: write`, and should use the protected `github-pages` environment ([custom Pages workflows](https://docs.github.com/en/pages/getting-started-with-github-pages/using-custom-workflows-with-github-pages)). Do not commit generated HTML alongside the Markdown.

## Guardrails and acceptance criteria

- One authoring location: `tools/src/assistant/CW.Assistant.Extensions.Docs/docs/`.
- One generated public snapshot: `tools-extensions-public/docs/dotnet/`, with `docs/README.md` as its landing/provenance page.
- Public `docs/dotnet/` is byte-for-byte derived from the released NuGet package.
- Synchronization is idempotent and deletes public files removed from the source package, but cannot change paths outside `docs/dotnet/` and the known provenance fields in `docs/README.md`.
- Every publication records package version and source SHA.
- The root public README's documentation links are checked in CI.
- No long-lived user PAT and no credentials with access to all organization repositories.
- Wiki remains available for genuinely collaborative project notes, but is not used for generated reference documentation.

## Rollout

1. Remove or retire the obsolete public-repository workflow that still assumes the docs package is built from the public repository ([current workflow](https://github.com/AEC-AB/tools-extensions-public/blob/main/.github/workflows/publish-extension-docs-nuget.yml)).
2. Add link and sensitive-content validation around the docs package in `tools`.
3. Add the post-publication synchronization stage and GitHub App credentials.
4. Run once against a known published package and review the generated pull request.
5. Merge, confirm all root README links resolve, and then decide whether the same `docs/` folder should be enabled as GitHub Pages.
