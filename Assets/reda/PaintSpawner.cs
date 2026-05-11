using UnityEngine;

public class PaintSpawner : MonoBehaviour
{
    public GameObject paintDropPrefab;
    public VerletPendulum bucketPhysics;

    public float spawnRate = 0.05f; // سرعة التنقيط
    public float downwardFlowSpeed = 2f; // دفع الطلاء لأسفل
    public float paintConsumptionPerDrop = 0.1f; // كم ينقص الطلاء مع كل قطرة

    private float timer = 0f;

    void Update()
    {
        // 1. تحقق إذا كان الدلو موجوداً وفيه طلاء
        if (bucketPhysics == null || bucketPhysics.paintAmount <= 0) return;

        timer += Time.deltaTime;

        if (timer >= spawnRate)
        {
            timer = 0f;
            SpawnDrop();

            // 2. إنقاص كمية الطلاء من الدلو
            bucketPhysics.paintAmount -= paintConsumptionPerDrop;
        }
    }

    void SpawnDrop()
    {
        GameObject drop = Instantiate(paintDropPrefab, transform.position, Quaternion.identity);
        PaintDrop dropScript = drop.GetComponent<PaintDrop>();

        if (dropScript != null)
        {
            Vector3 dropVelocity = bucketPhysics.GetVelocity();
            dropVelocity.y -= downwardFlowSpeed;
            dropScript.initialVelocity = dropVelocity;
        }
    }
}