using UnityEngine;

public class VerletPendulum : MonoBehaviour
{
    [Header("Basic Settings")]
    public Transform pivot; // نقطة التعليق [cite: 39]
    public float restLength = 5f; // طول الحبل [cite: 37]
    public Vector3 gravity = new Vector3(0, -9.81f, 0); // الجاذبية [cite: 46]

    [Header("Physics Properties")]
    public float bucketMass = 1f; // وزن الدلو وهو فارغ [cite: 31]
    public float paintAmount = 100f; // كمية الطلاء الحالية [cite: 33]
    [Range(0, 1)] public float damping = 0.995f; // معامل التخميد (مقاومة الهواء) [cite: 48]
    public float friction = 0.01f; // الاحتكاك عند نقطة التعليق [cite: 50]

    private Vector3 currentPosition;
    private Vector3 velocity;
    private bool isDragging = false;

    void Start()
    {
        // إذا لم تكن قد سحبت الدلو، سيبدأ من موقعه الحالي في الـ Scene
        currentPosition = transform.position;

        // حساب طول الحبل تلقائياً بناءً على المسافة في البداية (اختياري)
        if (pivot != null) restLength = Vector3.Distance(pivot.position, currentPosition);
    }

    void Update()
    {
        if (isDragging)
        {
            HandleMouseDragging();
        }
        else
        {
            ApplyPhysics();
        }

      // رسم الحبل في نافذة Scene للتوضيح [cite: 61]
        if (pivot != null) Debug.DrawLine(pivot.position, transform.position, Color.white);
    }

    void HandleMouseDragging()
    {
        // 1. إنشاء شعاع من الكاميرا إلى موقع الماوس
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // 2. إنشاء مستوى وهمي يمر بالدلو ويواجه الكاميرا (لضمان السحب في كل الاتجاهات)
        Plane dragPlane = new Plane(-Camera.main.transform.forward, transform.position);

        if (dragPlane.Raycast(ray, out float distance))
        {
            Vector3 mousePoint = ray.GetPoint(distance);
            Vector3 direction = mousePoint - pivot.position;

            // 3. قيد الحبل: إجبار الدلو على البقاء ضمن طول الحبل
            currentPosition = pivot.position + (direction.normalized * restLength);
            transform.position = currentPosition;

            // تصفير السرعة لمنع الدلو من "القفز" عند تركه
            velocity = Vector3.zero;
        }
    }

    void ApplyPhysics()
    {
        // 1. حساب الكتلة الكلية (الدلو + الطلاء المتبقي)
        // ملاحظة: تناقص الكتلة يجعل تأثير مقاومة الهواء أكبر مع الوقت 
        float totalMass = bucketMass + (paintAmount * 0.01f);

       // 2. تطبيق قوة الجاذبية [cite: 71, 80]
        Vector3 force = gravity * totalMass;

       // 3. إضافة مقاومة الهواء (تتناسب عكسياً مع الكتلة وطردياً مع السرعة) 
        // القوة تعاكس اتجاه السرعة دائماً
        Vector3 airResistance = -velocity * (1f - damping);
        force += airResistance;

        // 4. قانون نيوتن الثاني: التسارع = القوة / الكتلة
        Vector3 acceleration = force / totalMass;

        // 5. تحديث السرعة والموقع (Verlet-like Integration)
        velocity += acceleration * Time.deltaTime;

      // تطبيق احتكاك بسيط إضافي عند الحركة [cite: 50]
        velocity *= (1f - friction * Time.deltaTime);

        Vector3 nextPosition = currentPosition + velocity * Time.deltaTime;

       // 6. الحفاظ على طول الحبل (القيد الفيزيائي) [cite: 78]
        if (pivot != null)
        {
            Vector3 directionToPivot = nextPosition - pivot.position;
            Vector3 correctedPosition = pivot.position + (directionToPivot.normalized * restLength);

            // إعادة حساب السرعة بناءً على الموقع المصحح لضمان استقرار الحركة
            velocity = (correctedPosition - currentPosition) / Time.deltaTime;
            currentPosition = correctedPosition;
            transform.position = currentPosition;
        }
    }

    void OnMouseDown() { isDragging = true; }
    void OnMouseUp() { isDragging = false; }

    public Vector3 GetVelocity() { return velocity; }
}