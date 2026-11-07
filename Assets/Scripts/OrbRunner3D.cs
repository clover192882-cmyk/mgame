using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class OrbRunner3D : MonoBehaviour
{
    const float Arena = 14f;
    const int OrbCount = 5;

    Camera cam;
    Transform player;
    readonly List<Transform> orbs = new List<Transform>();
    readonly List<Transform> enemies = new List<Transform>();
    int score, best;
    float elapsed, enemyTimer;
    bool over;
    GUIStyle big, small, centered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        new GameObject("OrbRunner3D").AddComponent<OrbRunner3D>();
    }

    void Awake()
    {
        cam = Camera.main;
        if (cam == null)
        {
            cam = new GameObject("GameCamera").AddComponent<Camera>();
            cam.tag = "MainCamera";
        }
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.45f, 0.65f, 0.9f);

        if (FindFirstObjectByType<Light>() == null)
        {
            var light = new GameObject("Sun").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
        }

        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(Arena * 2f / 10f, 1, Arena * 2f / 10f);
        Tint(ground, new Color(0.25f, 0.55f, 0.3f));
        Destroy(ground.GetComponent<Collider>());

        MakeWalls();

        player = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
        player.name = "Player";
        Tint(player.gameObject, new Color(0.2f, 0.7f, 1f));
        Destroy(player.GetComponent<Collider>());

        Restart();
    }

    void MakeWalls()
    {
        var wallColor = new Color(0.35f, 0.3f, 0.3f);
        float len = Arena * 2f + 1f;
        Vector3[] pos = { new Vector3(0, 0.5f, Arena + 0.5f), new Vector3(0, 0.5f, -Arena - 0.5f), new Vector3(Arena + 0.5f, 0.5f, 0), new Vector3(-Arena - 0.5f, 0.5f, 0) };
        Vector3[] size = { new Vector3(len, 1, 1), new Vector3(len, 1, 1), new Vector3(1, 1, len), new Vector3(1, 1, len) };
        for (int i = 0; i < 4; i++)
        {
            var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
            w.name = "Wall";
            w.transform.position = pos[i];
            w.transform.localScale = size[i];
            Tint(w, wallColor);
            Destroy(w.GetComponent<Collider>());
        }
    }

    static void Tint(GameObject go, Color c)
    {
        go.GetComponent<Renderer>().material.color = c;
    }

    Vector3 RandomSpot(float margin)
    {
        float r = Arena - margin;
        return new Vector3(Random.Range(-r, r), 0, Random.Range(-r, r));
    }

    Vector3 SpotAwayFromPlayer(float margin, float minDist)
    {
        for (int i = 0; i < 20; i++)
        {
            var s = RandomSpot(margin);
            if ((s - player.position).sqrMagnitude > minDist * minDist) return s;
        }
        return RandomSpot(margin);
    }

    void SpawnOrb()
    {
        var o = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
        o.name = "Orb";
        o.localScale = Vector3.one * 0.6f;
        Tint(o.gameObject, new Color(1f, 0.85f, 0.15f));
        Destroy(o.GetComponent<Collider>());
        o.position = SpotAwayFromPlayer(1.5f, 3f) + Vector3.up * 0.6f;
        orbs.Add(o);
    }

    void SpawnEnemy()
    {
        var e = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
        e.name = "Enemy";
        Tint(e.gameObject, new Color(0.9f, 0.2f, 0.2f));
        Destroy(e.GetComponent<Collider>());
        e.position = SpotAwayFromPlayer(1.5f, 9f) + Vector3.up * 0.5f;
        enemies.Add(e);
    }

    void Restart()
    {
        foreach (var o in orbs) Destroy(o.gameObject);
        foreach (var e in enemies) Destroy(e.gameObject);
        orbs.Clear();
        enemies.Clear();

        player.position = new Vector3(0, 0.5f, 0);
        score = 0;
        elapsed = 0;
        enemyTimer = 8f;
        over = false;

        for (int i = 0; i < OrbCount; i++) SpawnOrb();
        SpawnEnemy();
        SnapCamera();
    }

    void SnapCamera()
    {
        cam.transform.position = player.position + new Vector3(0, 11f, -9f);
        cam.transform.LookAt(player.position);
    }

    void Update()
    {
        var kb = Keyboard.current;

        if (over)
        {
            if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.rKey.wasPressedThisFrame)) Restart();
            return;
        }

        elapsed += Time.deltaTime;

        var move = Vector3.zero;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.z += 1;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.z -= 1;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1;
        }
        if (move.sqrMagnitude > 1) move.Normalize();

        var pos = player.position + move * 8f * Time.deltaTime;
        pos.x = Mathf.Clamp(pos.x, -Arena + 0.5f, Arena - 0.5f);
        pos.z = Mathf.Clamp(pos.z, -Arena + 0.5f, Arena - 0.5f);
        player.position = pos;
        if (move.sqrMagnitude > 0.01f) player.Rotate(Vector3.right, 360f * Time.deltaTime, Space.World);

        for (int i = orbs.Count - 1; i >= 0; i--)
        {
            var o = orbs[i];
            o.position = new Vector3(o.position.x, 0.6f + Mathf.Sin(Time.time * 3f + i) * 0.15f, o.position.z);
            var d = o.position - player.position;
            d.y = 0;
            if (d.sqrMagnitude < 1.1f * 1.1f)
            {
                Destroy(o.gameObject);
                orbs.RemoveAt(i);
                score++;
                SpawnOrb();
            }
        }

        enemyTimer -= Time.deltaTime;
        if (enemyTimer <= 0 && enemies.Count < 8)
        {
            enemyTimer = 8f;
            SpawnEnemy();
        }

        float enemySpeed = 3f + elapsed * 0.03f;
        foreach (var e in enemies)
        {
            var toPlayer = player.position - e.position;
            toPlayer.y = 0;
            if (toPlayer.sqrMagnitude < 0.9f * 0.9f)
            {
                over = true;
                best = Mathf.Max(best, score);
                return;
            }
            e.position += toPlayer.normalized * enemySpeed * Time.deltaTime;
            e.Rotate(Vector3.up, 90f * Time.deltaTime);
        }
    }

    void LateUpdate()
    {
        var target = player.position + new Vector3(0, 11f, -9f);
        cam.transform.position = Vector3.Lerp(cam.transform.position, target, 5f * Time.deltaTime);
        cam.transform.LookAt(player.position);
    }

    void OnGUI()
    {
        if (big == null)
        {
            big = new GUIStyle(GUI.skin.label) { fontSize = 48, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            small = new GUIStyle(GUI.skin.label) { fontSize = 24 };
            centered = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter };
            big.normal.textColor = Color.white;
            small.normal.textColor = Color.white;
            centered.normal.textColor = Color.white;
        }

        GUI.Label(new Rect(16, 10, 400, 40), "Orbs: " + score, small);
        GUI.Label(new Rect(16, 40, 400, 40), "Best: " + best, small);

        if (over)
        {
            GUI.Label(new Rect(0, Screen.height * 0.35f, Screen.width, 70), "CAUGHT!", big);
            GUI.Label(new Rect(0, Screen.height * 0.35f + 70, Screen.width, 50), "Press Space to restart", centered);
        }
        else if (elapsed < 5f)
        {
            GUI.Label(new Rect(0, Screen.height * 0.85f, Screen.width, 50), "Move: WASD or arrows. Collect yellow orbs, avoid the red cubes!", centered);
        }
    }
}
