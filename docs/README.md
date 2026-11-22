# Docs

The design lives in one place: the **GDD** (Google Docs, English):
https://docs.google.com/document/d/1neT60ecY91JCXXXcK3oA2VLGAo85USD44nZEfYv5wss
Tabs: GDD (whole game), MVP Scope (complete, kept as a record), Phase 2 Scope, Economy Draft.
Only working documents that change often stay here as files.

| Path | What |
| --- | --- |
| `DEVLOG.md` | Session log, newest first (Done / Decisions / Device findings / Open issues / Next) |
| `DEVICE_FINDINGS.md` | Confirmed RayNeo X3 Pro hardware and SDK facts; read before using any SDK feature |
| `RAYNEO_TODO_PROVIDER.md` | Reverse-engineered interface of the system To-do app content provider |
| `design/Phase2_Scope_Draft.md` | Phase 2 scope (working copy of the GDD tab): goal, scope, decisions P#, work P-W#, acceptance |
| `design/Economy_Draft_Doc.md` | Economy design (working copy of the GDD tab) with the derivation notes in the appendix |
| `design/Economy_Draft.xlsx` | Economy workbook: the source for every number (Params sheet drives the rest) |
| `iteration-log/2026-11-07.en.html` | Prototype iteration log with screenshots (open in a browser) |
| `iteration-log/screenshots/` | Screenshots per version: V1 parchment, V2 panels, V3 first APK, latest build |

The RayNeo OpenXR ARDK is embedded in `Packages/com.unity.xr.rayneo.openxr/` (v1.1.2), so the project
opens on any machine without a local package path.
