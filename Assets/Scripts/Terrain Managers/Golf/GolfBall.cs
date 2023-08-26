using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GolfBall : MonoBehaviour
{
    public const string LAYER_NAME = "Ball";
    public static int Layer => LayerMask.NameToLayer(LAYER_NAME);
    public static int Mask => LayerMask.GetMask(LAYER_NAME);

    public readonly static RigidPreset[] Presets = new RigidPreset[]
    {
        new RigidPreset(0.0f, 1.0f), // None
        new RigidPreset(5.0f, 0.9f), // LongGrass
        new RigidPreset(3.0f, 1.0f), // MediumGrass
        new RigidPreset(1.5f, 1.0f), // ShortGrass
        new RigidPreset(20.0f, 0.5f), // Water
        new RigidPreset(9.0f, 0.8f), // NormalSand
        new RigidPreset(9.0f, 0.8f), // HardSand
        new RigidPreset(9.0f, 0.8f), // SoftSand
        new RigidPreset(0.0f, 1.0f), // Ice
        new RigidPreset(9.0f, 0.8f), // Snow
        new RigidPreset(1.5f, 1.0f), // Regolith
    };

    [SerializeField] private RigidPreset CurrentPreset;

    public HashSet<Biome.Type> InvalidBiomesForCurrentCourse = new HashSet<Biome.Type>();

    // States
    public PlayState State;
    public bool IsOnGround { get; private set; } = false;
    private int ConsecutiveFramesOnGround = 0;
    public bool IsFrozen { get; private set; } = false;

    [Space]
    public Biome.Type CurrentBiome;

    // Statistics
    public Stats Progress = new Stats();

    public Vector3 Forward
    {
        get
        {
            switch (State)
            {
                case PlayState.Aiming:
                    return transform.forward;
                // Use the velocity
                case PlayState.Flying:
                    return rigid.velocity.normalized;
                // Use the velocity
                case PlayState.Rolling:
                    return rigid.velocity.normalized;
                default:
                    return transform.forward;
            }
        }
    }

    public Vector3 Position => transform.position;

    // Settings
    /// <summary>
    /// Maximum possible force applied to the ball when shot.
    /// </summary>
    public const float FullPower = 40;
    /// <summary>
    /// Velocity magnitude to determine when the ball should stop rolling.
    /// </summary>
    public const float SpeedCutoffThreshold = 0.5f;
    private const float SqrSpeedCutoffThreshold = SpeedCutoffThreshold * SpeedCutoffThreshold;
    /// <summary>
    /// Number of seconds in which the ball has to remain below the threshold for it to be stopped.
    /// </summary>
    public const float SecondsRequiredBelowSpeedThreshold = 1f;
    private float stopRollingTimer;

    /// <summary>
    /// Number of physics frames (FixedUpdate calls) before drag applies.
    /// </summary>
    /// <note>
    /// Used to prevent drag being applied to the ball when making a shot, or if the ball bounces.
    /// </note>
    private const int NumPhysicsFramesBeforeDragAplies = 5;

    [Header("Control settings")]
    [Range(0, 1)] public float Power;
    public float Rotation;
    public float Angle;

    const float DefaultRotation = 0.0f, DefaultPower = 0.5f, DefaultAngle = 40.0f;

    /// <summary>
    /// Radius of the Golf Ball
    /// </summary>
    public float Radius => (transform.localScale.x + transform.localScale.y + transform.localScale.z) / 3 / 2;

    /// <summary>
    /// Event called when the GolfBall has finished rolling and the Shooting state has been entered.
    /// </summary>
    public UnityAction OnRollingFinished;

    /// <summary>
    /// Event called when the Golf Ball goes out of bounds of the map
    /// </summary>
    public UnityAction OnOutOfBounds;


    [Header("References")]
    public Rigidbody rigid;
    public SphereCollider sphereCollider;

    private void Awake()
    {
        gameObject.layer = Layer;

        OnRollingFinished += Utils.EMPTY;
        OnOutOfBounds += Utils.EMPTY;
    }

    private void OnDestroy()
    {
        OnRollingFinished -= Utils.EMPTY;
        OnOutOfBounds -= Utils.EMPTY;
    }

    public void Reset()
    {

    }

    private void FixedUpdate()
    {
        bool wasOnGroundLastFrame = IsOnGround;

        // Get the onground value
        Collider[] groundCollisions = Physics.OverlapSphere(transform.position, sphereCollider.radius + 0.01f, GroundCheck.GroundMask);
        IsOnGround = groundCollisions.Length > 0;

        // Get the current biome
        CurrentBiome = IsOnGround ? TerrainChunk.GetBiomeSamplePoint(groundCollisions[0], transform.position) : Biome.Type.None;

        if (IsOnGround && wasOnGroundLastFrame)
        {
            ConsecutiveFramesOnGround++;
        }
        else
        {
            ConsecutiveFramesOnGround = 0;
        }


        PlayState lastFrame = State;

        if (IsOnGround)
        {
            // Speed is below the threshold
            if (rigid.velocity.sqrMagnitude < SqrSpeedCutoffThreshold)
            {
                stopRollingTimer += Time.fixedDeltaTime;
                if (stopRollingTimer >= SecondsRequiredBelowSpeedThreshold)
                {
                    // First frame of shooting
                    if (State == PlayState.Rolling)
                    {
                        // Keep rotation as it is
                        Angle = DefaultAngle;
                        Power = DefaultPower;

                        WaitForNextShot();
                        OnRollingFinished.Invoke();
                    }

                    State = PlayState.Aiming;
                }
            }
            // Still above it
            else
            {
                stopRollingTimer = 0;
                State = PlayState.Rolling;
            }
        }
        else
        {
            State = PlayState.Flying;
        }


        // Update the rigidbody properties
        CurrentPreset = Presets[(int)CurrentBiome];

        // Must have been on the ground for consecutive physics frames before drag applies
        if (ConsecutiveFramesOnGround >= NumPhysicsFramesBeforeDragAplies)
        {
            rigid.drag = CurrentPreset.Drag;
        }
        else
        {
            rigid.drag = 0;
        }


        // Check out of bounds
        if (!IsFrozen)
        {
            // We are in water or there is nothing below us (left the map)
            if (InvalidBiomesForCurrentCourse.Contains(CurrentBiome))
            {
                OnOutOfBounds.Invoke();
            }
        }
    }

    public void Shoot()
    {
        // Undo any freezes
        Freeze(false);

        ValidateValues();
        // Log the shot
        Progress.ShotsCurrentCourse.Push(new Stats.Shot(Position, Rotation, Angle, Power));

        // Reset the drag just for the shot
        rigid.drag = 0;
        ConsecutiveFramesOnGround = 0;

        // Apply the force in direction
        Vector3 force = CalculateInitialShotForce();
        rigid.velocity = force;
    }

    private Vector3 CalculateInitialShotForce()
    {
        return CurrentPreset.ShotPowerMultiplier * FullPower * Power * transform.forward;
    }

    public List<Vector3> CalculateShotPreviewWorldPositions(int maxSteps = 100, float timePerStep = 0.25f)
    {
        return CalculateShotPreviewWorldPositions(CalculateInitialShotForce(), maxSteps, timePerStep);
    }

    private List<Vector3> CalculateShotPreviewWorldPositions(Vector3 initialForce, int maxSteps = 100, float timePerStep = 0.25f)
    {
        List<Vector3> positions = new List<Vector3>() { transform.position };

        float time = 0;
        for (int i = 0; i < maxSteps; i++)
        {
            float displacementX = Utils.CalculateDisplacementSUVAT(initialForce.x, time, Physics.gravity.x);
            float displacementY = Utils.CalculateDisplacementSUVAT(initialForce.y, time, Physics.gravity.y);
            float displacementZ = Utils.CalculateDisplacementSUVAT(initialForce.z, time, Physics.gravity.z);

            Vector3 lastPosition = positions[i];
            Vector3 newWorldPosition = transform.position + new Vector3(displacementX, displacementY, displacementZ);
            Vector3 distance = newWorldPosition - lastPosition;

            // Check that the new position is acessible from the previous position
            if (!Physics.Raycast(lastPosition, distance.normalized, out RaycastHit hit, distance.magnitude, GroundCheck.SolidObjectsMask))
            {
                // Add it if it is
                positions.Add(newWorldPosition);
            }
            else
            {
                // If not, then choose the last point of collision
                positions.Add(hit.point);
                // And break out as this is the last position
                break;
            }

            time += timePerStep;
        }

        positions.RemoveAt(0);

        return positions;
    }

    public void WaitForNextShot()
    {
        if (!IsFrozen)
        {
            StartCoroutine(FreezeUntilShoot());
        }
    }

    private IEnumerator FreezeUntilShoot()
    {
        Freeze(true);

        // Freeze until a shot has been taken
        int shotsBefore = Progress.ShotsForThisHole;
        while (shotsBefore == Progress.ShotsForThisHole)
        {
            yield return null;
        }

        Freeze(false);
    }

    private void Freeze(bool freeze)
    {
        IsFrozen = freeze;

        // Set the freeze mode
        RigidbodyConstraints c = RigidbodyConstraints.None;
        if (freeze)
        {
            c = RigidbodyConstraints.FreezeAll;
        }

        rigid.constraints = c;
    }

    public void SetValues(float rotation, float angle, float power)
    {
        Rotation = rotation;
        Angle = angle;
        Power = power;
        ValidateValues();
    }

    public void MoveGolfBallAndWaitForNextShot(Vector3 pos, float initialRotation = DefaultRotation, float initialPower = DefaultPower, float initialAngle = DefaultAngle)
    {
        StopAllCoroutines();

        // Reset all movement
        rigid.velocity = Vector3.zero;
        rigid.angularVelocity = Vector3.zero;

        transform.position = pos;

        Rotation = initialRotation;

        // Default values
        Power = initialPower;
        Angle = initialAngle;

        ValidateValues();

        WaitForNextShot();
    }

    private void ValidateValues()
    {
        // Clamp the power
        Power = Mathf.Clamp01(Power);

        // Ensure rotation is between 0 and 360
        while (Rotation < 0)
        {
            Rotation += 360;
        }
        while (Rotation > 360)
        {
            Rotation -= 360;
        }

        // Clamp the angle
        Angle = Mathf.Clamp(Angle, 0, 80);

        transform.rotation = Quaternion.Euler(-Angle, Rotation, 0);
    }

    private void OnValidate()
    {
        ValidateValues();
    }

    public void HoleReached(int courseNumber, DateTime reached)
    {
        // Add the hole 
        Progress.CoursesCompleted.Push(new Stats.Pot(courseNumber, reached - Progress.TimeStartedCurrentCourse, Progress.ShotsForThisHole));
        Progress.ShotsCurrentCourse.Clear();
    }


    public class Stats
    {
        public Stack<Pot> CoursesCompleted = new Stack<Pot>();

        public DateTime TimeStartedCurrentCourse;

        public Stack<Shot> ShotsCurrentCourse = new Stack<Shot>();
        public int ShotsForThisHole => ShotsCurrentCourse.Count;

        public int CurrentCourse { get { if (CoursesCompleted.Count > 0) { return CoursesCompleted.Peek().CourseNumber + 1; } else { return 0; } } }


        public Stats()
        {
            Clear();
        }

        public void Clear()
        {
            CoursesCompleted.Clear();
            ShotsCurrentCourse.Clear();
        }

        public class Pot
        {
            public int CourseNumber;
            public TimeSpan Time;
            public int ShotsTaken;

            public Pot(int courseNumber, in TimeSpan duration, in int shots)
            {
                CourseNumber = courseNumber;
                Time = duration;
                ShotsTaken = shots;
            }
        }

        public class Shot
        {
            public Vector3 PositionFrom;
            public float Rotation;
            public float Angle;
            public float Power;

            public Shot(Vector3 from, float rotation, float angle, float power)
            {
                PositionFrom = from;
                Rotation = rotation;
                Angle = angle;
                Power = power;
            }
        }
    }

    [Serializable]
    public struct RigidPreset
    {
        public float Drag;
        public float ShotPowerMultiplier;

        public RigidPreset(float drag, float shotPowerMultiplier)
        {
            Drag = drag;
            ShotPowerMultiplier = shotPowerMultiplier;
        }
    }

    public enum PlayState
    {
        Aiming,
        Flying,
        Rolling,
    }

    private void OnDrawGizmosSelected()
    {
        // Draw the shot preview
        if (State == PlayState.Aiming)
        {
            Gizmos.color = Color.green;
            foreach (Vector3 pos in CalculateShotPreviewWorldPositions())
            {
                Gizmos.DrawSphere(pos, 0.25f);
            }
        }
        // Draw the facing
        else
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + (Forward / 2));
        }
    }

}
