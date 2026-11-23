# AGENTS.md

DesktopFlyouts provides lightweight desktop flyouts, menu flyouts, and tray icon integration for
Windows App SDK / WinUI 3, UWP / WinUI 2, and Uno Platform on Linux / X11.

- Start with `git status --short --branch` to understand the current branch and uncommitted changes.
- Most runtime behavior lives in `src/DesktopFlyouts.Shared` behind `#if WASDK`, `#if UWP`, and `#if HAS_UNO`. Keep all branches building when changing shared files.
- Uno defines both `WASDK` and `HAS_UNO`; use `WASDK && !HAS_UNO` for Windows App SDK-specific code.
- Follow `.editorconfig`: UTF-8, CRLF, a final newline, spaces, and 4-space indentation. `.gitattributes` enforces `text=auto eol=crlf`. Preserve existing indentation in project files.
- Follow existing code patterns and dependency property generation conventions. Avoid broad refactors when fixing focused behavior.
- Do not edit generated `bin`, `obj`, or `Generated Files` output.
- Keep usage documentation in `docs/README.md` and the detailed public API reference in `docs/api-reference.md`. Update affected examples, signatures, defaults, platform availability, and links when APIs or behavior change.
- Update `CHANGE_LOG.md` when a change affects public APIs, runtime behavior, platform support, or other notable user-facing functionality or documentation. Add entries under `Unreleased` until the next release is tagged; keep existing release sections tied to their tags.
- Describe breaking changes and their migration steps in the changelog. Keep release history and migration notes out of the usage guide and API reference; those documents should describe the current API.
- Build the libraries and their samples one command at a time, and inspect each result before continuing. Shared runtime changes require checking all three library flavors. Use `/p:AppxBundle=Never` to avoid unnecessary Windows app bundle generation.
- Use Visual Studio's MSBuild for UWP sample projects so the XAML compiler runs correctly; `dotnet msbuild` may leave `InitializeComponent` ungenerated.
- WinUI and UWP builds require Windows; skip them on other platforms. Uno / X11 runtime validation requires Linux. Report checks that could not be run and why.
- Do not launch sample apps or other UI applications, including the WinUI and UWP test apps, unless the user explicitly requests it. Validate with builds and non-UI checks by default.
- If an explicitly requested launch involves the packaged WinUI sample, register its generated `AppxManifest.xml` and launch through `shell:AppsFolder`, rather than running its executable directly. Stop running samples before rebuilding only if their output files are locked.
- `NETSDK1057` preview SDK messages can appear on this machine. Treat them as environment notices unless accompanied by a build failure.
- Build both Windows test apps with Visual Studio's MSBuild using the commands in `tests/README.md`. These template-based test apps use VSTest and a packaged application host; run `tests/Run-Tests.ps1` only when the user explicitly requests UI execution on Windows with an interactive desktop.
- Before finishing, run `git diff --check` and verify that changed text files use CRLF.
- Summarize the changes, validation results, and any remaining risks or limitations.
