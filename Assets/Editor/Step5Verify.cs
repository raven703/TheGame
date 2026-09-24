using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Temporary automated Step 5 verification (deleted after the run).</summary>
[InitializeOnLoad]
public static class Step5Verify
{
    private const string SessionKey = "Step5Verify.Active";
    private const string OutDir = "Temp/Step5Verify";
    private const int Width = 1280;
    private const int Height = 720;

    private static double t0;
    private static double captureT0;
    private static int phase;
    private static int wantShip;
    private static int capturesDone;
    private static bool forcedPlayer;
    private static int framesInCapture;

    private static float minDist = 9999f;
    private static float maxDist;
    private static float distAtFirstShot = -1f;
    private static float distAtSecondShot = -1f;
    private static int playerShots;
    private static int enemyShots;
    private static int errorCount;
    private static int exceptionCount;
    private static int warningCount;

    private static GameObject player;
    private static GameObject enemy;
    private static LineRenderer playerBeam;
    private static LineRenderer enemyBeam;
    private static RenderTexture rt;
    private static Camera cam;

    static Step5Verify()
    {
        if (!SessionState.GetBool(SessionKey, false))
            return;

        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        Application.logMessageReceived -= OnLog;
        Application.logMessageReceived += OnLog;
    }

    public static void Run()
    {
        Directory.CreateDirectory(OutDir);
        SessionState.SetBool(SessionKey, true);
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        Application.logMessageReceived -= OnLog;
        Application.logMessageReceived += OnLog;
        EditorSceneManager.OpenScene("Assets/Scenes/CombatSandbox.unity");
        EditorApplication.EnterPlaymode();
    }

    private static void OnLog(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Error) errorCount++;
        else if (type == LogType.Exception) exceptionCount++;
        else if (type == LogType.Warning) warningCount++;

        if (condition.StartsWith("PlayerShip выстрелил")) { playerShots++; TryOnShotFired(true); }
        else if (condition.StartsWith("EnemyShip выстрелил")) { enemyShots++; TryOnShotFired(false); }
    }

    private static void TryOnShotFired(bool isPlayer)
    {
        try { OnShotFired(isPlayer); }
        catch { }
    }

    private static void Dbg(string msg)
    {
        try { File.AppendAllText(Path.Combine(OutDir, "debug.txt"), $"[{DateTime.Now:HH:mm:ss.fff}] {msg}\n"); }
        catch { }
    }

    private static bool EnsureRefs()
    {
        if (player != null && enemy != null)
            return true;

        player = GameObject.Find("PlayerShip");
        enemy = GameObject.Find("EnemyShip");
        if (player == null || enemy == null)
            return false;

        var playerHealth = player.GetComponent<ShipHealth>();
        var enemyHealth = enemy.GetComponent<ShipHealth>();
        if (playerHealth == null || enemyHealth == null)
            return false;

        player.GetComponent<ShipWeapon>().SetTarget(enemyHealth);
        cam = Camera.main;
        if (cam != null && rt == null)
            rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
        playerBeam = FindBeam(player);
        enemyBeam = FindBeam(enemy);
        Dbg($"refs ok: cam={(cam != null)}, playerBeam={(playerBeam != null)}, enemyBeam={(enemyBeam != null)}");
        return true;
    }

    private static void OnShotFired(bool isPlayer)
    {
        if (!EditorApplication.isPlaying)
            return;
        if (!EnsureRefs())
            return;
        if (playerBeam == null || enemyBeam == null || cam == null || rt == null)
            return;

        int ship = isPlayer ? 1 : 2;
        if (phase != 0 || (wantShip != 0 && wantShip != ship))
            return;

        forcedPlayer = isPlayer;
        var shooter = isPlayer ? player : enemy;
        var targetGo = isPlayer ? enemy : player;
        var beam = isPlayer ? playerBeam : enemyBeam;
        var distance = Vector3.Distance(player.transform.position, enemy.transform.position);
        if (capturesDone == 0)
            distAtFirstShot = distance;
        else
            distAtSecondShot = distance;

        beam.enabled = true;
        SetBeamPositions(beam, shooter, targetGo);
        cam.targetTexture = rt;
        Time.timeScale = 0f; // freeze: WaitForSeconds(0.1) never elapses, beam stays on for deterministic capture
        captureT0 = EditorApplication.timeSinceStartup;
        framesInCapture = 0;
        phase = 1;
        Dbg($"shot event ship={(isPlayer ? "player" : "enemy")} dist={distance:F2} -> capture started (frozen)");
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying)
        {
            if (phase == 99 || phase == 100)
            {
                Dbg("not playing -> writing report");
                FinishAndExit();
            }
            return;
        }

        if (t0 == 0)
        {
            t0 = EditorApplication.timeSinceStartup;
            Dbg("entered play mode");
        }

        if (phase == 99)
        {
            Dbg("writing report + exiting immediately (no ExitPlaymode: avoids domain reload)");
            FinishAndExit();
            return;
        }

        if (!EnsureRefs())
            return;

        var distance = Vector3.Distance(player.transform.position, enemy.transform.position);
        minDist = Mathf.Min(minDist, distance);
        maxDist = Mathf.Max(maxDist, distance);

        if (phase == 0)
        {
            var playerOn = playerBeam != null && playerBeam.enabled && wantShip != 2;
            var enemyOn = enemyBeam != null && enemyBeam.enabled && wantShip != 1;
            if (playerOn || enemyOn)
            {
                forcedPlayer = playerOn;
                if (capturesDone == 0)
                    distAtFirstShot = distance;
                else
                    distAtSecondShot = distance;
                if (cam != null)
                    cam.targetTexture = rt;
                Time.timeScale = 0f; // freeze coroutine so beam stays on
                captureT0 = EditorApplication.timeSinceStartup;
                framesInCapture = 0;
                phase = 1;
            }
        }
        else if (phase == 1)
        {
            if (forcedPlayer)
            {
                playerBeam.enabled = true;
                SetBeamPositions(playerBeam, player, enemy);
            }
            else
            {
                enemyBeam.enabled = true;
                SetBeamPositions(enemyBeam, enemy, player);
            }

            var sinceCapture = EditorApplication.timeSinceStartup - captureT0;
            framesInCapture++;
            Dbg($"capture frame {framesInCapture} (+{sinceCapture:F1}s)");
            if (sinceCapture >= 2.0 && cam != null && cam.targetTexture == rt)
            {
                Capture(forcedPlayer ? "step5_laser_player.png" : "step5_laser_enemy.png");
                cam.targetTexture = null;
                Time.timeScale = 1f; // let ShowLaser finish and hide the beam
                capturesDone++;
                Dbg($"captured #{capturesDone}");
                if (capturesDone < 2)
                {
                    wantShip = forcedPlayer ? 2 : 1;
                    phase = 0;
                }
                else
                {
                    phase = 2;
                }
            }
        }
        else if (phase == 2)
        {
            if (playerBeam != null)
                playerBeam.enabled = false;
            if (enemyBeam != null)
                enemyBeam.enabled = false;
        }

        var elapsed = EditorApplication.timeSinceStartup - t0;
        if (elapsed > 30 || (phase == 2 && elapsed > 8))
            phase = 99;
    }

    private static void Capture(string fileName)
    {
        var previous = RenderTexture.active;
        RenderTexture.active = rt;
        var texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
        texture.Apply();
        File.WriteAllBytes(Path.Combine(OutDir, fileName), texture.EncodeToPNG());
        RenderTexture.active = previous;
        UnityEngine.Object.DestroyImmediate(texture);
    }

    private static void SetBeamPositions(LineRenderer beam, GameObject shooter, GameObject targetObject)
    {
        var weapon = shooter.GetComponent<ShipWeapon>();
        var muzzle = weapon != null && weapon.weaponPoint != null
            ? weapon.weaponPoint.position
            : shooter.transform.position;
        beam.SetPosition(0, muzzle);
        beam.SetPosition(1, targetObject.transform.position);
    }

    private static LineRenderer FindBeam(GameObject ship)
    {
        var child = ship.transform.Find("LaserBeam");
        return child != null ? child.GetComponent<LineRenderer>() : null;
    }

    private static string DescribeBeam(LineRenderer beam)
    {
        if (beam == null)
            return "MISSING";

        var shaderName = beam.sharedMaterial != null ? beam.sharedMaterial.shader.name : "<none>";
        return $"enabled={beam.enabled}, positionCount={beam.positionCount}, " +
               $"startWidth={beam.startWidth}, endWidth={beam.endWidth}, useWorldSpace={beam.useWorldSpace}, " +
               $"startColor={beam.startColor}, endColor={beam.endColor}, sortingOrder={beam.sortingOrder}, shader={shaderName}, " +
               $"p0={beam.GetPosition(0)}, p1={beam.GetPosition(1)}";
    }

    private static void FinishAndExit()
    {
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= OnLog;
        SessionState.SetBool(SessionKey, false);

        var enemyAi = enemy != null ? enemy.GetComponent<EnemyAI>() : null;
        var enemyWeapon = enemy != null ? enemy.GetComponent<ShipWeapon>() : null;

        var report = new System.Text.StringBuilder();
        report.AppendLine("=== Step 5 automated verification ===");
        report.AppendLine($"beam screenshots captured: {capturesDone} (expect 2)");
        report.AppendLine($"PlayerShip beam: {DescribeBeam(playerBeam)}");
        report.AppendLine($"EnemyShip beam : {DescribeBeam(enemyBeam)}");
        report.AppendLine($"shots: player={playerShots}, enemy={enemyShots}");
        report.AppendLine($"AI: component={(enemyAi != null)}, target={(enemyWeapon != null && enemyWeapon.Target != null ? enemyWeapon.Target.name : "<null>")}, optimalDistance={(enemyAi != null ? enemyAi.optimalDistance.ToString("F2") : "n/a")}");
        report.AppendLine($"distance player->enemy: start=10.00, min={minDist:F2}, max={maxDist:F2}, atPlayerShot={distAtFirstShot:F2}, atEnemyShot={distAtSecondShot:F2}");
        report.AppendLine($"console: errors={errorCount}, exceptions={exceptionCount}, warnings={warningCount}");

        File.WriteAllText(Path.Combine(OutDir, "report.txt"), report.ToString());
        EditorApplication.Exit(0);
    }
}
