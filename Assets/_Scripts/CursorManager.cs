using UnityEngine;

public class CursorManager : MonoBehaviour
{
    public Texture2D cursorTexture;

    void Start()
    {
        Vector2 hotspot = new Vector2(0, 0);

        Cursor.SetCursor(cursorTexture, hotspot, CursorMode.Auto);
    }
}
