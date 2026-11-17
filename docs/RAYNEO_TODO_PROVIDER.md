# RayNeo system To-do app — provider interface (reverse-engineered)

Source: `/system_ext/priv-app/RayNeoNotes/RayNeoNotes.apk` on X3 Pro (pulled 2026-10-18, dexdump).
Undocumented; may change with any firmware update.

## Provider
- Authority: `com.rayneo.notes.TodoContentProvider`
- `exported=true`, **no permission** required.
- `query/insert/update/delete` NOT supported. Everything goes through `ContentResolver.call(uri, method, arg, extras)`.
- Result Bundle: `callResult` (bool), `callErrorMessage` (string), `result` (JSON string or bool).

## Methods (method name -> arg)
| method | arg | effect |
|---|---|---|
| `getTodoUnfinished` | - | `result` = `{"list":[RemoteTodo...]}` |
| `getTodoFinished` | - | same, finished items |
| `todoSetFinished` | timestamp (string of long) | mark done |
| `todoSetUnfinished` | timestamp | mark undone |
| `deleteTodo` | timestamp | delete |
| `todoEdit` | RemoteTodo JSON | replaces **text** of the item with the same timestamp; does NOT create |
| `startTodo` | - | asks the notes app to start its own voice-create flow (works only in some foreground states) |

**There is no add/create method.** `addTodo` -> `UnSupport Method`.

## RemoteTodo JSON
`{ "timestamp": long, "updateTimestamp": long, "text": string, "finished": bool }`
The item key is `timestamp` (creation time ms).

## Storage (internal, not accessible)
SharedPreferences `store_todolist`, key `todolist_content` (Gson TodoItemList), `todolist_id`.

## Other
- Notes app does its own ASR with Microsoft Cognitive Services Speech (cloud).
- Changes are broadcast via `com.rayneo.notes.TodoNotifyProvider` calls: `todoNew`, `todoDelete`,
  `todoSetFinished`, `todoSetUnfinished` (that provider lives in another system app).
- System default `RecognitionService`: `com.rayneo.live.ai/.wakeup.RayNeoRecognitionService`
  -> Android `SpeechRecognizer` should work from Unity.

## adb test
    adb shell content call --uri content://com.rayneo.notes.TodoContentProvider --method getTodoUnfinished
