using UnityEngine;
using UnityEngine.EventSystems;

public class PointerDebug : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public void OnPointerEnter(PointerEventData e) => Debug.Log("ENTER button");
    public void OnPointerExit(PointerEventData e)  => Debug.Log("EXIT button");
    public void OnPointerClick(PointerEventData e) => Debug.Log("CLICK button");

    void Update()
    {
        if (Input.anyKeyDown)
            foreach (KeyCode k in System.Enum.GetValues(typeof(KeyCode)))
                if (Input.GetKeyDown(k)) Debug.Log("KEY: " + k);
    }
}