# Docs

The design master is the **GDD** Google Doc (English):
https://docs.google.com/document/d/1neT60ecY91JCXXXcK3oA2VLGAo85USD44nZEfYv5wss
Tabs: GDD (whole game), MVP Scope (complete), Phase 2 Scope, Economy Design. The files in `design/` mirror those
tabs and are kept in sync with it.

| Path | What |
| --- | --- |
| `design/GDD.md` | Whole-game design (GDD tab), with the open items table (Q#) |
| `design/MVP_Scope.md` | MVP scope (complete 2026-11-22): demo path, decisions D#, work W#, risks R# |
| `design/Phase2_Scope.md` | Phase 2 scope: trash and gradual automation, decisions P#, work P-W#, acceptance |
| `design/Economy_Design.md` | Economy design, updated continuously; derivation notes in the appendix |
| `design/Economy.xlsx` | Economy workbook: the source for every number (Params sheet drives the rest) |
| `DEVLOG.md` | Session log, newest first (Done / Decisions / Device findings / Open issues / Next) |
| `DEVICE_FINDINGS.md` | Confirmed RayNeo X3 Pro hardware and SDK facts; read before using any SDK feature |
| `RAYNEO_TODO_PROVIDER.md` | Reverse-engineered interface of the system To-do app content provider |
| `iteration-log/2026-11-07.en.html` | Prototype iteration log with screenshots (open in a browser) |
| `iteration-log/screenshots/` | Screenshots per version: V1 parchment, V2 panels, V3 first APK, latest build |

Scope docs describe one phase each; once a phase is done, its lasting rules belong in `GDD.md`.

The RayNeo OpenXR ARDK is embedded in `Packages/com.unity.xr.rayneo.openxr/` (v1.1.2), so the project
opens on any machine without a local package path.
