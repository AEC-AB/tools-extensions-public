# IDS Validator

## Description

Validate one IFC file against the requirements in one buildingSMART IDS file in Assistant. Use this extension across projects. It adds no project-specific filenames, MMI levels, tag patterns, mandatory classes, properties or delivery rules.

Requires Assistant on 64-bit Windows. Validation runs entirely in C# using managed xBim libraries. No separate runtime installation, setup scripts or online schema downloads are needed during validation. Both input files remain unchanged.

## Configuration

| Field | Select | Effect |
|---|---|---|
| IFC file | One existing `.ifc` model | Model to validate. No filename convention is required. |
| IDS file | One existing `.ids` document | All validation requirements come from this file. |
| Report folder | A writable folder | Receives Markdown, JSON, HTML and BCF reports. Use a separate folder for each run you want to retain. |

All fields are required and initially empty. Use Browse to select files and the folder. No test-suite setting is exposed.

## Functionality

1. Select the IFC file, IDS file and report folder, then run the extension.
2. The extension detects the IDS standard declaration and audits the IDS structure and semantics.
3. It evaluates applicability and requirements for entity, attribute, property, classification, material and part-of facets, including restrictions and cardinality.
4. Read the English summary tables in Assistant. Open the HTML report for full evidence, or JSON for automation.
5. Open `deviations.bcfzip` with the corresponding IFC in your BCF application.

| Status in JSON | Assistant result | Meaning |
|---|---|---|
| `passed` | Succeeded | All selected IDS requirements passed. |
| `requirements_failed` | Failed | Validation completed and at least one IDS requirement failed. |
| `technical_error` | Failed | Validation could not be completed reliably. Read the errors and rerun after resolving the cause. |

There is no partially succeeded result. A requirement failure does not mean the validation engine failed to run.

| Output | Contents |
|---|---|
| `result.md` | Summary tables and up to 50 violation rows. |
| `report.html` | Full results, structured actual/expected evidence and diagnostics. |
| `report.json` | Machine-readable status, findings, specification summaries, version selection, source hashes and report paths. |
| `deviations.bcfzip` | BCF 2.1 topic for every requirement violation, with rule ID, actual value, expected constraint and IFC GlobalId selection when available. |

Assistant variables `IdsControlStatus`, `IdsReportPath` and `IdsBcfPath` expose the status and file paths to subsequent workflow steps.

An empty BCF archive is written when there are no requirement violations. Technical errors remain in the reports, not fabricated element issues. A required specification with no matching entity produces a document-level violation. Non-root IFC resources can have no GlobalId; these findings identify the resource by STEP ID and model reference. BCF never invents GlobalIds. Viewpoints contain element selection but no camera or snapshot.

## Troubleshooting

| Symptom | Action |
|---|---|
| Missing input file | Use Browse again and select a locally available file. Download cloud-only files first. |
| Invalid IDS | Read the audit diagnostic and correct the requirements document in your authoring tool, then select the corrected file. The extension never edits it. |
| Unsupported IDS version | Export an IDS 1.0 document from the authoring tool. Draft or future versions are rejected without conversion. |
| Version inferred warning | The file has no IDS schema declaration. It is audited as IDS 1.0, and this assumption is explicitly reported. Ask the author to add the declaration if needed. |
| IFC schema metadata warning | The IDS names a different IFC schema. Requirements were still evaluated; confirm the IDS author intended these requirements for this model. |
| Report cannot be written | Choose a writable folder and close applications locking report files. Existing reports with the same names are replaced. |
| Validation library cannot load | Restore or reinstall the complete extension package so its managed dependency assemblies are present. |
| BCF does not select a resource | Check whether the finding has a real GlobalId. Use the reported STEP ID for non-root resources. Load the exact IFC used for validation. |

## FAQ

**Which IDS versions are supported?**

IDS 1.0 is the supported final standard. The canonical `/IDS/1.0/ids.xsd` declaration selects it; the `1.0.0` alias and HTTP/HTTPS locations are accepted. Explicit draft, future, ambiguous or unrecognised declarations fail with a technical error. Additional versions need their own schema, semantic audit, evaluator and conformance tests before they can be supported.

**Is `<info><version>` used to select the standard?**

No. It is the author's document revision. Standard selection uses the IDS namespace and its `xsi:schemaLocation` pair. With no schema hint, IDS 1.0 is inferred and must pass schema and semantic auditing.

**Is this entirely C#?**

Yes. The extension, IDS evaluator, auditing, reports, BCF writer and conformance runner are C#. Dependencies restore through NuGet. No Python, subprocess engine or setup script is used.

**What IFC versions were verified?**

The official paired corpus covers IFC2X3 and IFC4. The managed IFC parser also supports IFC4X3; version-specific IFC4X3 regression tests are included with the delivered test results. IDS `ifcVersion` metadata does not silently exclude specifications.

**Is it certified?**

No certification is claimed. The delivered conformance record contains all 334 paired cases from the pinned buildingSMART corpus, all matching their expected outcomes. This evidence covers those cases, not every possible IFC and IDS combination.

## Resources

- [buildingSMART IDS standard and official cases](https://github.com/buildingSMART/IDS)
- [IDS developer guide](https://github.com/buildingSMART/IDS/blob/development/Documentation/ImplementersDocumentation/developer-guide.md)
- [buildingSMART IDS audit tool](https://github.com/buildingSMART/IDS-Audit-tool)
- [xBim IDS Validator](https://github.com/xBimTeam/Xbim.IDS.Validator)
- [Third-party notices](THIRD-PARTY-NOTICES.md) and [development and verification](DEVELOPMENT.md) describe dependencies, build commands and standards corrections.

## Version History

- 1.0.0, 2026-10-16: Initial project-independent validator. IDS 1.0 detection and auditing, English table reports, structured evidence, failed control status for violations, and BCF 2.1 element links.
