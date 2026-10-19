using System;
using System.Collections.Generic;
using UnityEngine;

namespace RayneoIdle.Todo
{
    /// <summary>One item of the RayNeo system To-do app (Gson JSON field names, do not rename).</summary>
    [Serializable]
    public class RemoteTodo
    {
        public long timestamp;        // creation time (ms), also the item key
        public long updateTimestamp;
        public string text;
        public bool finished;
    }

    [Serializable]
    internal class RemoteTodoList
    {
        public List<RemoteTodo> list = new List<RemoteTodo>();
    }

    /// <summary>
    /// Client for the RayNeo system To-do app (com.rayneo.notes), through its exported
    /// TodoContentProvider and ContentResolver.call(). The interface is UNDOCUMENTED (found by
    /// inspecting the APK): it has no "create" method, and may change with any firmware update,
    /// so every call is wrapped and failures only log + return false/empty.
    ///
    /// Methods (method -> arg): getTodoUnfinished / getTodoFinished (no arg, result = {"list":[...]}),
    /// todoSetFinished / todoSetUnfinished / deleteTodo (arg = item timestamp),
    /// startTodo (no arg, result = true: opens the system voice-create screen).
    /// Requires the provider in the manifest <queries> on Android 11+.
    /// Editor: returns a small fake list so the UI can be tested.
    /// </summary>
    public static class SystemTodoClient
    {
        private const string ProviderUri = "content://com.rayneo.notes.TodoContentProvider";
        private const string KeyCallResult = "callResult";
        private const string KeyCallError = "callErrorMessage";
        private const string KeyResult = "result";

        public static List<RemoteTodo> GetUnfinished() => GetList("getTodoUnfinished");
        public static List<RemoteTodo> GetFinished() => GetList("getTodoFinished");

        /// <summary>
        /// Opens the system To-do voice-create screen (com.rayneo.notes TodoCreateActivity) on top of
        /// this app: it records, recognises in the RayNeo cloud (system language, en-US on our device),
        /// saves the todo and returns to this app by itself. This app is paused meanwhile, so detect the
        /// new item by re-reading the list when focus comes back. Returns false if the screen was not started.
        /// </summary>
        public static bool StartVoiceCreate() => Call("startTodo", "", out string started) && started == "true";

        public static bool SetFinished(long timestamp) => Call("todoSetFinished", timestamp.ToString(), out _);
        public static bool SetUnfinished(long timestamp) => Call("todoSetUnfinished", timestamp.ToString(), out _);

        private static List<RemoteTodo> GetList(string method)
        {
            if (!Call(method, "", out string json) || string.IsNullOrEmpty(json)) return new List<RemoteTodo>();
            try
            {
                return JsonUtility.FromJson<RemoteTodoList>(json)?.list ?? new List<RemoteTodo>();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SystemTodo] {method}: bad JSON ({e.Message}): {json}");
                return new List<RemoteTodo>();
            }
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        /// <summary>
        /// ContentResolver.call(uri, method, arg, extras). Blocking binder call (a few ms), main thread is fine.
        /// arg and extras are passed as "" / empty Bundle, never null: JNI cannot infer a signature for null.
        /// </summary>
        private static bool Call(string method, string arg, out string result)
        {
            result = null;
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var resolver = activity.Call<AndroidJavaObject>("getContentResolver"))
                using (var uriClass = new AndroidJavaClass("android.net.Uri"))
                using (var uri = uriClass.CallStatic<AndroidJavaObject>("parse", ProviderUri))
                using (var extras = new AndroidJavaObject("android.os.Bundle"))
                using (var bundle = resolver.Call<AndroidJavaObject>("call", uri, method, arg, extras))
                {
                    if (bundle == null)
                    {
                        Debug.LogWarning($"[SystemTodo] {method}: null result (provider missing or not visible)");
                        return false;
                    }
                    bool ok = bundle.Call<bool>("getBoolean", KeyCallResult, false);
                    if (!ok)
                    {
                        Debug.LogWarning($"[SystemTodo] {method}({arg}) failed: {bundle.Call<string>("getString", KeyCallError)}");
                        return false;
                    }
                    // "result" is a JSON string for the get* methods and a Boolean for startTodo; read it
                    // as Object + toString so both work. Other methods do not set it (null).
                    using (var value = bundle.Call<AndroidJavaObject>("get", KeyResult))
                        result = value?.Call<string>("toString");
                    Debug.Log($"[SystemTodo] {method}({arg}) ok: {result}");
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SystemTodo] {method}({arg}) exception: {e.Message}");
                return false;
            }
        }
#else
        // Editor / other platforms: fake data, the finished flag is kept in memory only.
        private static readonly List<RemoteTodo> fake = new List<RemoteTodo>
        {
            new RemoteTodo { timestamp = 1, text = "Reply to Prof. email" },
            new RemoteTodo { timestamp = 2, text = "Print capstone poster" },
        };

        private static bool Call(string method, string arg, out string result)
        {
            result = null;
            switch (method)
            {
                case "getTodoUnfinished":
                case "getTodoFinished":
                    bool wantFinished = method == "getTodoFinished";
                    var l = new RemoteTodoList();
                    foreach (var t in fake) if (t.finished == wantFinished) l.list.Add(t);
                    result = JsonUtility.ToJson(l);
                    return true;
                case "todoSetFinished":
                case "todoSetUnfinished":
                    long ts = long.Parse(arg);
                    foreach (var t in fake) if (t.timestamp == ts) t.finished = method == "todoSetFinished";
                    return true;
                case "startTodo":
                    // No system screen in the Editor: pretend the user said something.
                    long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    fake.Add(new RemoteTodo { timestamp = now, updateTimestamp = now, text = "Buy milk (editor)" });
                    result = "true";
                    return true;
            }
            return false;
        }
#endif
    }
}
