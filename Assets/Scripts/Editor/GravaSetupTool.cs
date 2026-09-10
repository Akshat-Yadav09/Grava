#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class GravaSetupTool
{
    [MenuItem("Tools/Grava/Auto-Setup Scene (Colliders & Scripts)")]
    public static void AutoSetupScene()
    {
        // 1. Setup PlayerObj
        GameObject playerObj = GameObject.Find("PlayerObj");
        if (playerObj != null)
        {
            Undo.RegisterFullObjectHierarchyUndo(playerObj, "Setup PlayerObj");

            var circleCol = playerObj.GetComponent<CircleCollider2D>();
            if (circleCol == null) circleCol = playerObj.AddComponent<CircleCollider2D>();

            var rb = playerObj.GetComponent<Rigidbody2D>();
            if (rb == null) rb = playerObj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            var playerBall = playerObj.GetComponent<PlayerBall>();
            if (playerBall == null) playerBall = playerObj.AddComponent<PlayerBall>();

            Debug.Log("[Grava] PlayerObj configured with Rigidbody2D, CircleCollider2D, and PlayerBall.");
        }
        else
        {
            Debug.LogWarning("[Grava] PlayerObj not found in active scene.");
        }

        // 2. Setup BlackHole
        GameObject blackHole = GameObject.Find("BlackHole");
        if (blackHole != null)
        {
            Undo.RegisterFullObjectHierarchyUndo(blackHole, "Setup BlackHole");

            var bhController = blackHole.GetComponent<BlackHoleController>();
            if (bhController == null) bhController = blackHole.AddComponent<BlackHoleController>();

            Debug.Log("[Grava] BlackHole configured with BlackHoleController.");
        }
        else
        {
            Debug.LogWarning("[Grava] BlackHole not found in active scene.");
        }

        // 3. Setup WinPoint
        GameObject winPoint = GameObject.Find("WinPoint");
        if (winPoint != null)
        {
            Undo.RegisterFullObjectHierarchyUndo(winPoint, "Setup WinPoint");

            var boxCol = winPoint.GetComponent<BoxCollider2D>();
            if (boxCol == null) boxCol = winPoint.AddComponent<BoxCollider2D>();
            boxCol.isTrigger = true;

            var wp = winPoint.GetComponent<WinPoint>();
            if (wp == null) wp = winPoint.AddComponent<WinPoint>();

            Debug.Log("[Grava] WinPoint configured with trigger BoxCollider2D and WinPoint script.");
        }
        else
        {
            Debug.LogWarning("[Grava] WinPoint not found in active scene.");
        }

        // 4. Setup Walls
        PhysicsMaterial2D bouncyMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/BouncyWall.physicsMaterial2D");

        GameObject wallParent = GameObject.Find("Wall");
        if (wallParent != null)
        {
            BoxCollider2D[] childColliders = wallParent.GetComponentsInChildren<BoxCollider2D>(true);
            foreach (Transform child in wallParent.transform)
            {
                Undo.RegisterFullObjectHierarchyUndo(child.gameObject, "Setup Wall Child");
                var col = child.GetComponent<BoxCollider2D>();
                if (col == null) col = child.gameObject.AddComponent<BoxCollider2D>();
                if (bouncyMat != null)
                {
                    col.sharedMaterial = bouncyMat;
                }
            }
            Debug.Log("[Grava] Wall colliders configured with BouncyWall material.");
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Grava Setup Complete", "Scene components for PlayerObj, BlackHole, Walls, and WinPoint have been successfully configured!", "Awesome");
    }
}
#endif
