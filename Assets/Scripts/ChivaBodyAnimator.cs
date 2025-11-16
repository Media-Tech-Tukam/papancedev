using UnityEngine;

public class ChivaBodyAnimator : MonoBehaviour
{
    [Header("References")]
    public ImprovedSplineFollower chiva; // Asignar el Root del vehículo

    [Header("Tilt")]
    public float maxTiltAngle = 15f;      // Grados al girar
    public float tiltSpeed = 5f;          // Qué tan rápido se inclina

    [Header("Forward Tilt")]
    public float maxForwardTilt = 12f;    // Inclinación al frenar
    public float forwardTiltSpeed = 4f;

    [Header("Suspension")]
    public float suspensionAmplitude = 0.05f; // Altura del rebote
    public float suspensionFrequency = 2f;    // Velocidad del rebote

    private float tilt = 0f;
    private float forwardTilt = 0f;
    private float suspensionOffset = 0f;
    private Vector3 initialLocalPos;
    private Quaternion initialLocalRot;

    void Start()
    {
        // Guardamos la posición/rotación original del vagón
        initialLocalPos = transform.localPosition;
        initialLocalRot = transform.localRotation;
    }

    void Update()
    {
        if (chiva == null) return;

        // ----------------------
        // 1. INCLINACIÓN LATERAL
        // ----------------------
        float targetTilt = -chiva.GetLateralInput() * maxTiltAngle;
        tilt = Mathf.Lerp(tilt, targetTilt, tiltSpeed * Time.deltaTime);

        // ----------------------
        // 2. INCLINACIÓN FRONTAL
        // ----------------------
        bool braking = Input.GetKey(KeyCode.Space);
        float targetForwardTilt = braking ? maxForwardTilt : 0f;
        forwardTilt = Mathf.Lerp(forwardTilt, targetForwardTilt, forwardTiltSpeed * Time.deltaTime);

        // ----------------------
        // 3. SUSPENSIÓN (rebote)
        // ----------------------
        suspensionOffset =
            Mathf.Sin(Time.time * suspensionFrequency) * suspensionAmplitude;

        // ----------------------
        // 4. APLICAR TRANSFORMACIONES VISUALES
        // ----------------------
        transform.localPosition =
            initialLocalPos +
            new Vector3(0f, suspensionOffset, 0f);

        transform.localRotation =
            initialLocalRot *
            Quaternion.Euler(forwardTilt, 0f, tilt);
    }
}
