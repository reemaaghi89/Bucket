using UnityEngine;

public class SphericalPendulumMath : MonoBehaviour
{
    public enum BucketShape { Cylindrical, Conical, Cubic }

    [Header("Data Architecture")]
    public PendulumData data;

    [Header("Mathematical Parameters")]
    public Transform pivot;
    public float baseLength = 5f;
    public float gravity = 9.81f;
    public float airDamping = 0.01f;

    [Header("Bucket Geometry")]
    public BucketShape shape = BucketShape.Cylindrical;
    public float bucketHeight = 0.4f;

    [Header("Variable Mass Dynamics")]
    public float emptyBucketMass = 0.5f;
    public float maxPaintMass = 2.0f;
    public float currentPaintMass = 2.0f;
    public float drainRate = 0.05f;

    private float theta = 0.1f;
    private float phi = 0f;
    private float thetaVelocity = 0f;
    private float phiVelocity = 0f;
    private bool isDragging = false;
    private float effectiveLength = 5f;
    private float dragDepth;

    void Start()
    {
        if (pivot == null) return;

        Vector3 initialOffset = transform.position - pivot.position;
        if (initialOffset.magnitude > 0.1f) baseLength = initialOffset.magnitude;
        else initialOffset = Vector3.down * baseLength;

        UpdateMassAndCOM();

        theta = Mathf.Acos(Mathf.Clamp(-initialOffset.normalized.y, -1f, 1f));
        phi = Mathf.Atan2(initialOffset.z, initialOffset.x);

        // حماية من القطب السفلي
        if (theta < 0.01f) theta = 0.01f;

        UpdatePositionFromAngles();
    }

    void FixedUpdate()
    {
        if (isDragging || pivot == null) return;

        UpdateMassAndCOM();

        // 1. حساب التسارع
        float thetaAcceleration = (Mathf.Sin(theta) * Mathf.Cos(theta) * Mathf.Pow(phiVelocity, 2))
                                  - ((gravity / effectiveLength) * Mathf.Sin(theta));

        float phiAcceleration = 0f;

        // حماية من القطبين (العلوي والسفلي) لمنع القسمة على صفر (تجنب الـ NaN)
        if (Mathf.Abs(Mathf.Sin(theta)) > 0.05f)
        {
            phiAcceleration = -2f * (Mathf.Cos(theta) / Mathf.Sin(theta)) * thetaVelocity * phiVelocity;
        }
        else
        {
            phiVelocity *= 0.90f; // تخميد الدوران عند الأقطاب
        }

        // 2. تطبيق التخامد
        ApplyDamping(ref thetaAcceleration, ref phiAcceleration);

        // تقييد التسارع لتجنب القفزات
        thetaAcceleration = Mathf.Clamp(thetaAcceleration, -40f, 40f);
        phiAcceleration = Mathf.Clamp(phiAcceleration, -40f, 40f);

        // 3. التكامل الزمني
        thetaVelocity += thetaAcceleration * Time.fixedDeltaTime;
        phiVelocity += phiAcceleration * Time.fixedDeltaTime;

        theta += thetaVelocity * Time.fixedDeltaTime;
        phi += phiVelocity * Time.fixedDeltaTime;

        // [السر هنا]: إبقاء الزاوية ضمن النطاق الآمن برمجياً لتستمر بالدوران
        // إذا تجاوزت الزاوية القطب العلوي، نعدلها لتكمل الدورة بسلام
        //if (theta > 3.13f) theta = 3.13f;
        //if (theta < 0.01f) theta = 0.01f;

        // نظام الإصلاح التلقائي
        if (float.IsNaN(theta) || float.IsNaN(phi))
        {
            theta = 0.1f; phi = 0f; thetaVelocity = 0f; phiVelocity = 0f;
        }

        UpdatePositionFromAngles();
        UpdateSharedData();
    }

    void OnMouseDrag()
    {
        dragDepth += Input.GetAxis("Mouse ScrollWheel") * 5f;

        Vector3 mouseScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, dragDepth);
        Vector3 mouseWorldPoint = Camera.main.ScreenToWorldPoint(mouseScreenPoint);

        Vector3 offset = (mouseWorldPoint - pivot.position).normalized * effectiveLength;

        float newTheta = Mathf.Acos(Mathf.Clamp(-offset.y / effectiveLength, -1f, 1f));
        float newPhi = Mathf.Atan2(offset.z, offset.x);

        thetaVelocity = (newTheta - theta) / Time.deltaTime;
        float deltaPhi = Mathf.DeltaAngle(phi * Mathf.Rad2Deg, newPhi * Mathf.Rad2Deg) * Mathf.Deg2Rad;
        phiVelocity = deltaPhi / Time.deltaTime;

        theta = newTheta;
        phi = newPhi;

        // حماية أثناء السحب
        if (theta < 0.01f) theta = 0.01f;
        if (theta > 3.13f) theta = 3.13f;

        UpdatePositionFromAngles();
    }

    void OnMouseDown()
    {
        isDragging = true;
        thetaVelocity = 0f;
        phiVelocity = 0f;
        dragDepth = Camera.main.WorldToScreenPoint(transform.position).z;
    }

    

    void OnMouseUp() { isDragging = false; }

    void UpdateMassAndCOM()
    {
        if (currentPaintMass > 0)
        {
            currentPaintMass -= drainRate * Time.fixedDeltaTime;
            if (currentPaintMass < 0) currentPaintMass = 0;
        }

        float fillRatio = maxPaintMass > 0 ? currentPaintMass / maxPaintMass : 0;
        float z_cm = 0f;

        switch (shape)
        {
            case BucketShape.Conical: z_cm = (3f / 4f) * bucketHeight * Mathf.Pow(fillRatio, 1f / 3f); break;
            default: z_cm = (bucketHeight * fillRatio) / 2f; break;
        }
        effectiveLength = baseLength + (bucketHeight - z_cm);
    }

    void ApplyDamping(ref float thetaAcc, ref float phiAcc)
    {
        float dragCoefficient = 1.0f;
        if (shape == BucketShape.Cylindrical) dragCoefficient = 0.82f;
        else if (shape == BucketShape.Cubic) dragCoefficient = 1.05f;
        else if (shape == BucketShape.Conical) dragCoefficient = 0.50f;

        float totalMass = emptyBucketMass + currentPaintMass;
        float massDerivative = (currentPaintMass > 0) ? drainRate : 0f;

        float totalDamping = (airDamping * dragCoefficient) + (massDerivative / totalMass);
        thetaAcc -= totalDamping * thetaVelocity;
        phiAcc -= totalDamping * phiVelocity;
    }

    void UpdatePositionFromAngles()
    {
        float x = effectiveLength * Mathf.Sin(theta) * Mathf.Cos(phi);
        float z = effectiveLength * Mathf.Sin(theta) * Mathf.Sin(phi);
        float y = -effectiveLength * Mathf.Cos(theta);

        transform.position = pivot.position + new Vector3(x, y, z);
        if (pivot != null) Debug.DrawLine(pivot.position, transform.position, Color.white);
    }

    void UpdateSharedData()
    {
        if (data == null) return;

        data.totalMass = emptyBucketMass + currentPaintMass;
        data.currentPaintMass = currentPaintMass;
        data.effectiveLength = effectiveLength;
        data.fillRatio = maxPaintMass > 0 ? currentPaintMass / maxPaintMass : 0;
        data.theta = theta;
        data.phi = phi;
        data.angularVelocityTheta = thetaVelocity;
        data.angularVelocityPhi = phiVelocity;

        Vector3 gravVec = new Vector3(0, -gravity, 0);
        data.gravityVector = gravVec;

        float tangentialAccel = effectiveLength * Mathf.Pow(thetaVelocity, 2);
        data.effectiveGravity = gravVec.magnitude + (tangentialAccel / effectiveLength);

        Vector3 velocityDir = Vector3.Cross(transform.position - pivot.position, Vector3.up).normalized;
        data.linearVelocity = velocityDir * (effectiveLength * phiVelocity);
        data.currentPosition = transform.position;
    }
}