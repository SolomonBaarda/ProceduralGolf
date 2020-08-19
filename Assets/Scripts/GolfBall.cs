using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class GolfBall : MonoBehaviour, ICanBeFollowed
{
    // Constants 
    public readonly static RigidPreset Preset_Air = new RigidPreset(0f, 0f);
    public readonly static RigidPreset Preset_Grass = new RigidPreset(2.5f, 1f);
    public readonly static RigidPreset Preset_GrassHole = new RigidPreset(1.5f, 1f);
    public readonly static RigidPreset Preset_Sand = new RigidPreset(7f, 10f);
    public readonly static RigidPreset Preset_Water = new RigidPreset(20f, 50f);
    public readonly static RigidPreset Preset_Ice = new RigidPreset(0f, 0f);

    // States
    public PlayState State;
    public bool IsOnGround = false;
    public bool IsFrozen = false;

    [Space]
    public Biome.Type CurrentBiome;
    private TerrainMap.Point biomeSamplePoint;

    // Statistics
    public Stats GameStats;


    private Vector3 Facing
    {
        get
        {
            if (rigid != null)
            {
                switch (State)
                {
                    case PlayState.Shooting:
                        return transform.forward;
                    // Use the velocity
                    case PlayState.Flying:
                        return rigid.velocity;
                    // Use the velocity
                    case PlayState.Rolling:
                        return rigid.velocity;
                }
            }

            return Vector3.zero;
        }
    }

    public Vector3 Forward => Facing.normalized;

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


    private float Scale => (transform.localScale.x + transform.localScale.y + transform.localScale.z) / 3;
    public float Radius => Scale / 2;

    /// <summary>
    /// Event called when the GolfBall has finished rolling and the Shooting state has been entered.
    /// </summary>
    public UnityAction OnRollingFinished;


    [Header("References")]
    public Rigidbody rigid;
    public SphereCollider sphereCollider;


    [Header("Line Preview")]
    public Material LinePreviewMaterial;
    private LineRenderer shotPreview;
    public Gradient LineColour;


    private void Awake()
    {
        transform.localScale = new Vector3(Scale, Scale, Scale);

        GameStats = new Stats();
        GameStats.Reset();

        OnRollingFinished += Utils.EMPTY;

        // Set the shot preview
        shotPreview = gameObject.AddComponent<LineRenderer>();
        shotPreview.widthCurve = AnimationCurve.Linear(0f, 0.05f, 1f, 0.05f);
        shotPreview.enabled = false;
        shotPreview.material = LinePreviewMaterial;
        shotPreview.colorGradient = LineColour;
    }



    private void FixedUpdate()
    {
        // Get the onground value
        Collider[] groundCollisions = GroundCheck.GetGroundCollisions(transform.position, sphereCollider.radius + GroundCheck.DEFAULT_RADIUS);
        IsOnGround = groundCollisions.Length > 0;

        // Get the current biome
        Collider c = null;
        if (groundCollisions.Length > 0)
        {
            c = groundCollisions[0];
        }

        biomeSamplePoint = Biome.GetBiomeSamplePoint(c, Position);
        if (biomeSamplePoint != null)
        {
            CurrentBiome = biomeSamplePoint.Biome;
        }


        PlayState lastFrame = State;

        // Update the state
        // On the ground
        if (IsOnGround)
        {
            float speed = rigid.velocity.magnitude;

            // Speed is below the threshold
            if (speed < SpeedCutoffThreshold)
            {
                stopRollingTimer += Time.fixedDeltaTime;
                if (stopRollingTimer >= SecondsRequiredBelowSpeedThreshold)
                {
                    State = PlayState.Shooting;

                    // First frame of shooting
                    if (lastFrame == PlayState.Rolling && State == PlayState.Shooting)
                    {
                        WaitForNextShot();
                        OnRollingFinished.Invoke();
                    }
                }
            }
            // Still above it
            else
            {
                stopRollingTimer = 0;
                State = PlayState.Rolling;
            }

        }
        // In the air
        else
        {
            State = PlayState.Flying;
        }


        // Update the rigidbody properties
        RigidPreset r = Preset_Air;
        // On the ground
        if (State == PlayState.Rolling)
        {
            switch (CurrentBiome)
            {
                case Biome.Type.Grass:
                    r = Preset_Grass;
                    break;
                case Biome.Type.Sand:
                    r = Preset_Sand;
                    break;
                case Biome.Type.Hole:
                    r = Preset_GrassHole;
                    break;
                case Biome.Type.Water:
                    r = Preset_Water;
                    break;
                case Biome.Type.Ice:
                    r = Preset_Ice;
                    break;
            }

        }

        // Set the values
        rigid.angularDrag = r.AngularDrag;
        rigid.drag = r.Drag;
    }



    public void Shoot()
    {
        // Undo any freezes
        Freeze(false);

        // Apply the force in direction
        Vector3 force = transform.forward * Power * FullPower;
        rigid.AddForce(force, ForceMode.Impulse);

        GameStats.Shots++;
    }



    public void Reset()
    {
        transform.rotation = Quaternion.identity;

        rigid.velocity = Vector3.zero;
        rigid.angularVelocity = Vector3.zero;

        SetDefaultShotValues();
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
        int shotsBefore = GameStats.Shots;
        while (shotsBefore == GameStats.Shots)
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


    public void SetShotPreview(bool useRotation, bool useAngle, bool usePower, float lengthMultiplier)
    {
        // Calculate which axis to use
        Vector3 offset = transform.forward;
        offset.x = useRotation ? offset.x : 0;
        offset.z = useRotation ? offset.z : 0;

        offset.y = useAngle ? offset.y : 0;

        // Now ensure it is the correct length
        offset.Normalize();
        offset *= lengthMultiplier;

        // Apply power multiplier if we need to
        offset *= usePower ? Power : 1;

        // Assign the point
        shotPreview.SetPositions(new Vector3[] { transform.position, transform.position + offset });
    }

    public void SetShotPreviewVisible(bool visible)
    {
        shotPreview.enabled = visible;
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
        Rotation = 0;
        Angle = 40;

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




    private void OnDrawGizmos()
    {
        // Draw the facing
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + Forward / 2);


        if (biomeSamplePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(biomeSamplePoint.LocalVertexPosition + biomeSamplePoint.Offset, 0.1f);
        }
    }






    public class Stats
    {
        public int Shots;

        public void Reset()
        {
            Shots = 0;
        }
    }




    public struct RigidPreset
    {
        public float Drag;
        public float AngularDrag;

        public RigidPreset(float drag, float angularDrag)
        {
            Drag = drag;
            AngularDrag = angularDrag;
        }
    }

    public enum PlayState
    {
        Shooting,
        Flying,
        Rolling,
    }


}
