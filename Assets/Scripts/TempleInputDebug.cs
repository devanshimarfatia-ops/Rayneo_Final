using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// Temporary diagnostic: logs every key/button press so you can confirm
/// exactly what the X3 Pro right-temple tap registers as in Unity.
/// Build and run this on-device, tap the right temple, then check the
/// logcat / on-screen log for the key name.
/// Remove this once you've confirmed the binding.
/// </summary>
public class TempleInputDebug : MonoBehaviour
{
    void OnEnable()
    {
        if (Keyboard.current != null)
            Keyboard.current.onTextInput += OnTextInput;
    }

    void Update()
    {
        // New Input System: check every key on the current keyboard device
        if (Keyboard.current != null)
        {
            foreach (KeyControl key in Keyboard.current.allKeys)
            {
                if (key.wasPressedThisFrame)
                    Debug.Log($"[TempleInputDebug] Keyboard key pressed: {key.name}");
            }
        }

        // Also check gamepad-style buttons, since DPAD_CENTER / BUTTON_A
        // sometimes surface as a generic HID gamepad device instead
        if (Gamepad.current != null)
        {
            foreach (ButtonControl button in Gamepad.current.allControls)
            {
                if (button is ButtonControl b && b.wasPressedThisFrame)
                    Debug.Log($"[TempleInputDebug] Gamepad button pressed: {b.name}");
            }
        }

        // Legacy fallback (works even without the new Input System active)
        if (Input.anyKeyDown)
        {
            foreach (KeyCode kcode in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(kcode))
                    Debug.Log($"[TempleInputDebug] Legacy KeyCode pressed: {kcode}");
            }
        }
    }

    void OnTextInput(char c)
    {
        Debug.Log($"[TempleInputDebug] Text input: '{c}'");
    }

    void OnDisable()
    {
        if (Keyboard.current != null)
            Keyboard.current.onTextInput -= OnTextInput;
    }
}