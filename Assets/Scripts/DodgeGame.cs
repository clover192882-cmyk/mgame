using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DodgeGame : MonoBehaviour
{
    const float HalfH = 5f;
    const int GameLayer = 31;

    Sprite square;
    Camera cam;
    Transform player;
    readonly List<Transform> blocks = new List<Transform>();
    float spawnTimer, score, best, elapsed;
    bool over;
    GUIStyle big, small;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        new GameObject("DodgeGame").AddComponent<DodgeGame>();
    }

    void Awake()
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        square = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

        cam = Camera.main;
        if (cam == null) cam = new GameObject("GameCamera").AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = HalfH;
        cam.transform.SetPositionAndRotation(new Vector3(0, 0, -10), Quaternion.identity);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.09f, 0.15f);
        cam.cullingMask = 1 << GameLayer;

        player = MakeBlock(new Color(0.3f, 0.9f, 0.5f), new Vector2(1.2f, 0.6f)).transform;
        Restart();
    }

    GameObject MakeBlock(Color color, Vector2 size)
    {
        var go = new GameObject("Block") { layer = GameLayer };
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = square;
        sr.color = color;
        go.transform.localScale = size;
        return go;
    }

    void Restart()
    {
        foreach (var b in blocks) Destroy(b.gameObject);
        blocks.Clear();
        player.position = new Vector3(0, -HalfH + 0.8f, 0);
        score = 0;
        elapsed = 0;
        spawnTimer = 0;
        over = false;
    }

    void Update()
    {
        var kb = Keyboard.current;

        if (over)
        {
            bool pressed = (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.rKey.wasPressedThisFrame))
                           || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
            if (pressed) Restart();
            return;
        }

        float halfW = HalfH * cam.aspect;
        elapsed += Time.deltaTime;
        score += Time.deltaTime;

        float dir = 0;
        if (kb != null)
        {
            if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) dir -= 1;
            if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) dir += 1;
        }
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.isPressed)
            dir += mouse.position.ReadValue().x < Screen.width * 0.5f ? -1 : 1;

        var p = player.position;
        p.x = Mathf.Clamp(p.x + Mathf.Clamp(dir, -1, 1) * 9f * Time.deltaTime, -halfW + 0.6f, halfW - 0.6f);
        player.position = p;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0)
        {
            spawnTimer = Mathf.Max(0.25f, 0.8f - elapsed * 0.01f);
            float w = Random.Range(0.6f, 1.6f);
            var b = MakeBlock(new Color(0.95f, 0.35f, 0.35f), new Vector2(w, w)).transform;
            b.position = new Vector3(Random.Range(-halfW + w * 0.5f, halfW - w * 0.5f), HalfH + 1f, 0);
            blocks.Add(b);
        }

        float fall = 4f + elapsed * 0.15f;
        for (int i = blocks.Count - 1; i >= 0; i--)
        {
            var b = blocks[i];
            b.position += Vector3.down * fall * Time.deltaTime;

            if (Overlaps(player, b))
            {
                over = true;
                best = Mathf.Max(best, score);
                return;
            }

            if (b.position.y < -HalfH - 1f)
            {
                Destroy(b.gameObject);
                blocks.RemoveAt(i);
            }
        }
    }

    static bool Overlaps(Transform a, Transform b)
    {
        Vector3 d = a.position - b.position;
        Vector3 reach = (a.localScale + b.localScale) * 0.5f;
        return Mathf.Abs(d.x) < reach.x && Mathf.Abs(d.y) < reach.y;
    }

    void OnGUI()
    {
        if (big == null)
        {
            big = new GUIStyle(GUI.skin.label) { fontSize = 48, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            small = new GUIStyle(GUI.skin.label) { fontSize = 24 };
            big.normal.textColor = Color.white;
            small.normal.textColor = Color.white;
        }

        GUI.Label(new Rect(16, 10, 400, 40), "Score: " + Mathf.FloorToInt(score), small);
        GUI.Label(new Rect(16, 40, 400, 40), "Best: " + Mathf.FloorToInt(best), small);

        if (over)
        {
            GUI.Label(new Rect(0, Screen.height * 0.35f, Screen.width, 70), "GAME OVER", big);
            GUI.Label(new Rect(0, Screen.height * 0.35f + 70, Screen.width, 50), "Press Space or click to restart", new GUIStyle(small) { alignment = TextAnchor.MiddleCenter });
        }
        else if (elapsed < 4f)
        {
            GUI.Label(new Rect(0, Screen.height * 0.85f, Screen.width, 50), "Move: A/D or Left/Right arrows. Dodge the red blocks!", new GUIStyle(small) { alignment = TextAnchor.MiddleCenter });
        }
    }
}
