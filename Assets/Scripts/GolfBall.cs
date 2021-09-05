using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GolfBall : MonoBehaviour, ICanBeFollowed
{
    public const string LAYER_NAME = "Ball";
    public static int Layer => LayerMask.NameToLayer(LAYER_NAME);
    public static int Mask => LayerMask.GetMask(LAYER_NAME);

    // Constants 
    public readonly static RigidPreset Preset_Air = new RigidPreset(0f, 1f);
    public readonly static RigidPreset Preset_Grass = new RigidPreset(3f, 1f);
    public readonly static RigidPreset Preset_GrassShort = new RigidPreset(1.5f, 1f);
    public readonly static RigidPreset Preset_GrassLong = new RigidPreset(5f, 0.9f);
    public readonly static RigidPreset Preset_Sand = new RigidPreset(9f, 0.8f);
    public readonly static RigidPreset Preset_Water = new RigidPreset(20f, 0.5f);
    public readonly static RigidPreset Preset_Ice = new RigidPreset(0f, 1f);
    [SerializeField] private RigidPreset CurrentPreset;

    // States
    public PlayState State;
    public bool IsOnGround { get; private set; } = false;
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
    public const float FullPower = 50;
    public const float SpeedCutoffThreshold = 0.5f;
    public const float SecondsRequiredBelowSpeedThreshold = 1f;
    private float stopRollingTimer;

    [Header("Control settings")]
    [Range(0, 1)] public float Power;
    public float Rotation;
    public float Angle;

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

    public ShotPreview ShotPreview;

    private void Awake()
    {
        gameObject.layer = Layer;

        OnRollingFinished += Utils.EMPTY;
        OnOutOfBounds += Utils.EMPTY;

        ShotPreview.enabled = false;
    }

    private void OnDestroy()
    {
        OnRollingFinished -= Utils.EMPTY;
        OnOutOfBounds -= Utils.EMPTY;
    }

    public void Reset()
    {
        // Reset all movement
        rigid.velocity = Vector3.zero;
        rigid.angularVelocity = Vector3.zero;

        SetDefaultShotValues();
    }

    private void FixedUpdate()
    {
        // Get the onground value
        Collider[] groundCollisions = GroundCheck.DoSphereCast(transform.position, sphereCollider.radius + 0.01f);
        IsOnGround = groundCollisions.Length > 0;

        // Get the current biome
        CurrentBiome = IsOnGround ? Biome.GetBiomeSamplePoint(groundCollisions[0], transform.position) : Biome.Type.None;


        PlayState lastFrame = State;

        if (IsOnGround)
        {
                        // Speed is below the threshold
            if (rigid.velocity.magnitude < SpeedCutoffThreshold)
            {
                stopRollingTimer += Time.fixedDeltaTime;
                if (stopRollingTimer >= SecondsRequiredBelowSpeedThreshold)
                {
                    // First frame of shooting
                    if (State == PlayState.Rolling)
                    {
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
        // On the ground
        if (IsOnGround)
        {
            switch (CurrentBiome)
            {
                case Biome.Type.ShortGrass:
                    CurrentPreset = Preset_GrassShort;
                    break;
                case Biome.Type.MediumGrass:
                    CurrentPreset = Preset_Grass;
                    break;
                case Biome.Type.LongGrass:
                    CurrentPreset = Preset_GrassLong;
                    break;
                case Biome.Type.Sand:
                    CurrentPreset = Preset_Sand;
                    break;
                case Biome.Type.Water:
                    CurrentPreset = Preset_Water;
                    break;
                case Biome.Type.Ice:
                    CurrentPreset = Preset_Ice;
                    break;
            }
        }
        else
        {
            CurrentPreset = Preset_Air;
        }


        // Apply drag
        // Must have been on the ground for two consecutive physics frames before drag applies
        if (false)
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
            if (CurrentBiome == Biome.Type.Water)
            {
                OnOutOfBounds.Invoke();
            }
        }

        if (rigid.drag != 0)
            Debug.Log(rigid.drag);
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
        //StartCoroutine(DisableDragUntilOffGround());

        // Apply the force in direction
        Vector3 force = CalculateInitialShotForce();
        rigid.velocity = force;


        List<Vector3> positions = CalculateShotPreviewWorldPositions();
        for (int i = 0; i < positions.Count - 1; i++)
        {
            Debug.DrawLine(positions[i], positions[i + 1], Color.red, 100);
        }
    }

    private IEnumerator DisableDragUntilOffGround()
    {
        // Set the drag to 0 while we are on the ground
        while (IsOnGround)
        {
            
            yield return null;
        }
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
        List<Vector3> positions = new List<Vector3>();

        float time = 0;
        for (int i = 0; i < maxSteps; i++)
        {

            float displacementX = Utils.CalculateDisplacementSUVAT(initialForce.x, time, Physics.gravity.x);
            float displacementY = Utils.CalculateDisplacementSUVAT(initialForce.y, time, Physics.gravity.y);
            float displacementZ = Utils.CalculateDisplacementSUVAT(initialForce.z, time, Physics.gravity.z);

            Vector3 worldPosition = new Vector3(displacementX, displacementY, displacementZ) + transform.position;

            // Break out if this was the first position to go below the ground
            if (!GroundCheck.DoRaycastDown(worldPosition, out _, 100000))
            {
                if (positions.Count - 1 >= 0)
                {
                    // Calculate the position on the ground
                    Vector3 lastPosition = positions[positions.Count - 1];
                    Vector3 direction = (worldPosition - lastPosition).normalized;
                    if (GroundCheck.DoRaycast(lastPosition, direction, out RaycastHit hit, 1000))
                    {
                        positions.Add(hit.point);
                    }
                }
                break;
            }

            positions.Add(worldPosition);
            time += timePerStep;
        }

        return positions;
    }

    public void UpdateShotPreview()
    {
        Vector3[] positions = CalculateShotPreviewWorldPositions().ToArray();
        ShotPreview.SetPoints(positions, transform.rotation);
    }

    public void WaitForNextShot()
    {
        Reset();

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

    public void SetShotAnglePreview(string text)
    {
        Vector3 rotation = new Vector3(0, 90, Angle);
        ShotPreview.SetAnglePreview(rotation, text);
    }

    public void SetValues(float rotation, float angle, float power)
    {
        Rotation = rotation;
        Angle = angle;
        Power = power;
        ValidateValues();
    }

    private void SetDefaultShotValues()
    {
        // Default values
        Power = 0.5f;
        Angle = 40;

        // Face the direction we were rolling last @TODO
        //transform.forward = LastDirectionWhenRolling;
        //Quaternion.Euler(LastDirectionWhenRolling).eulerAngles.y

        Rotation = transform.eulerAngles.y;

        ValidateValues();
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
        Progress.CoursesCompleted.Push(new Stats.Pot(courseNumber, reached, Progress.ShotsForThisHole));
        Progress.ShotsCurrentCourse.Clear();
    }


    public class Stats
    {
        public Stack<Pot> CoursesCompleted = new Stack<Pot>();

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
            public DateTime TimeReached;
            public int ShotsTaken;

            public Pot(int courseNumber, in DateTime time, in int shots)
            {
                CourseNumber = courseNumber;
                TimeReached = time;
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
        if(State == PlayState.Aiming)
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
            Gizmos.DrawLine(transform.position, transform.position + Forward / 2);
        }
    }

}
