using UnityEngine;

public class PaintDrop : MonoBehaviour
{
    public Vector3 initialVelocity; // السرعة التي ستأخذها من الدلو
    public Vector3 gravity = new Vector3(0, -9.81f, 0);
    public float groundY = 0f; // ارتفاع مستوى اللوحة (الأرض)

    private bool hasHitGround = false;

    void Update()
    {
        if (hasHitGround) return;

        // تطبيق الجاذبية على قطرة الطلاء (السقوط الحر)
        initialVelocity += gravity * Time.deltaTime;
        transform.position += initialVelocity * Time.deltaTime;

        // التحقق الرياضي من الاصطدام باللوحة (بديلاً عن الـ Collider)
        if (transform.position.y <= groundY)
        {
            // تثبيت القطرة على سطح اللوحة تماماً
            transform.position = new Vector3(transform.position.x, groundY, transform.position.z);
            hasHitGround = true;

            // تغيير شكل القطرة لتصبح مسطحة (كأنها بقعة طلاء انسكبت)
            transform.localScale = new Vector3(0.05f, 0.01f, 0.05f);
        }
    }
}