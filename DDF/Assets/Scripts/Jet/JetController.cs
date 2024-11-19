using UnityEngine;

public class JetPhysics : MonoBehaviour
{
    [Header("Aerodynamics")]
    public float liftCoefficient = 1.5f; // Lift factor
    public float dragCoefficient = 0.03f; // Drag factor
    public float stallAngle = 15f; // Angle of attack where stall happens
    public float stallLiftMultiplier = 0.3f; // Reduced lift during stall
    public float stallSpeed = 50f; // Minimum speed to generate lift
    public float gravityMultiplier = 9.81f; // Gravity strength

    [Header("Throttle and Speed")]
    public float maxThrottlePower = 200f; // Maximum engine power
    public float throttleChangeRate = 10f; // Speed of throttle adjustment
    public float maxSpeed = 300f; // Maximum speed
    public float afterburnerMultiplier = 1.5f; // Extra power during afterburner

    [Header("Stability and Rotation")]
    public float rotationSpeed = 3f; // Rotational responsiveness
    public float rotationDamping = 0.5f; // Angular drag
    public float autoLevelSpeed = 2f; // Speed to return to level flight

    private Rigidbody rb;
    private float currentThrottle = 0f; // Current throttle percentage
    private bool isAfterburnerActive = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Jet requires a Rigidbody component!");
            return;
        }

        rb.useGravity = false; // We simulate gravity manually for control
    }

    void Update()
    {
        HandleThrottle();
        HandleRotation();
        HandleAfterburner();
    }

    void FixedUpdate()
    {
        ApplyThrust();
        ApplyAerodynamics();
        StabilizeJet();
        ApplyGravity();
    }

    void HandleThrottle()
    {
        // Adjust throttle with Shift and Control
        if (Input.GetKey(KeyCode.LeftShift))
            currentThrottle += throttleChangeRate * Time.deltaTime;
        if (Input.GetKey(KeyCode.LeftControl))
            currentThrottle -= throttleChangeRate * Time.deltaTime;

        // Clamp throttle between 0 and 100%
        currentThrottle = Mathf.Clamp(currentThrottle, 0f, 100f);
    }

    void HandleRotation()
    {
        float pitch = Input.GetAxis("Vertical"); // W/S for pitch
        float yaw = Input.GetAxis("Horizontal"); // A/D for yaw
        float roll = 0f;

        // Add roll with Q/E
        if (Input.GetKey(KeyCode.Q)) roll = 1f;
        if (Input.GetKey(KeyCode.E)) roll = -1f;

        // Apply rotational forces
        Vector3 rotationInput = new Vector3(pitch, yaw, roll) * rotationSpeed * Time.deltaTime;
        rb.AddRelativeTorque(rotationInput, ForceMode.VelocityChange);
    }

    void HandleAfterburner()
    {
        // Toggle afterburner with Left Alt
        if (Input.GetKeyDown(KeyCode.LeftAlt))
            isAfterburnerActive = !isAfterburnerActive;
    }

    void ApplyThrust()
    {
        float thrustPower = maxThrottlePower * (currentThrottle / 100f);

        // Apply afterburner multiplier
        if (isAfterburnerActive)
            thrustPower *= afterburnerMultiplier;

        // Apply forward thrust
        Vector3 thrustForce = transform.forward * thrustPower;
        rb.AddForce(thrustForce, ForceMode.Force);

        // Limit speed
        rb.velocity = Vector3.ClampMagnitude(rb.velocity, maxSpeed * (isAfterburnerActive ? 1.5f : 1f));
    }

    void ApplyAerodynamics()
    {
        Vector3 velocity = rb.velocity;
        float speed = velocity.magnitude;

        // Calculate angle of attack
        Vector3 forwardOnPlane = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        float angleOfAttack = Vector3.SignedAngle(forwardOnPlane, velocity.normalized, transform.right);

        // Lift force (reduces during stall)
        float liftFactor = angleOfAttack < stallAngle && speed > stallSpeed ? 1f : stallLiftMultiplier;
        float lift = liftCoefficient * liftFactor * Mathf.Max(0, speed - stallSpeed);
        rb.AddForce(transform.up * lift, ForceMode.Force);

        // Drag force (higher when climbing steeply)
        float climbDrag = Mathf.Abs(Mathf.Sin(Mathf.Deg2Rad * angleOfAttack));
        float drag = dragCoefficient * (1f + climbDrag) * speed * speed;
        rb.AddForce(-velocity.normalized * drag, ForceMode.Force);
    }

    void StabilizeJet()
    {
        // Gradually reduce angular velocity
        Vector3 angularVelocity = rb.angularVelocity;
        Vector3 damping = -angularVelocity * (rotationDamping * Time.fixedDeltaTime);
        rb.AddTorque(damping, ForceMode.Acceleration);

        // Subtle auto-leveling only at sufficient speed
        if (rb.velocity.magnitude > stallSpeed)
        {
            Quaternion targetRotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, autoLevelSpeed * Time.deltaTime * 0.5f);
        }
    }

    void ApplyGravity()
    {
        float speed = rb.velocity.magnitude;

        // Gravity becomes more aggressive as speed decreases below stall speed
        float gravityScale = speed > stallSpeed ? 1f : Mathf.Lerp(1f, 2f, (stallSpeed - speed) / stallSpeed);
        Vector3 gravityForce = Vector3.down * gravityMultiplier * rb.mass * gravityScale;
        rb.AddForce(gravityForce, ForceMode.Force);
    }
}
