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
    public readonly static RigidPreset Preset_Air = new RigidPreset(0f, 0f, 1f);
    public readonly static RigidPreset Preset_Grass = new RigidPreset(3f, 1f, 1f);
    public readonly static RigidPreset Preset_GrassShort = new RigidPreset(1.5f, 1f, 1f);
    public readonly static RigidPreset Preset_GrassLong = new RigidPreset(5f, 2f, 0.9f);
    public readonly static RigidPreset Preset_Sand = new RigidPreset(9f, 12f, 0.8f);
    public readonly static RigidPreset Preset_Water = new RigidPreset(20f, 50f, 0.5f);
    public readonly static RigidPreset Preset_Ice = new RigidPreset(0f, 0f, 1f);
    [SerializeField] private RigidPreset CurrentPreset;

    // States
    public PlayState State;
    public bool IsOnGround { get; private set; } = false;
    public bool IsFrozen { get; private set; } = false;

    [Space]
    public Biome.Type CurrentBiome;

    // Statistics
    public Stats Progress = new Stats();


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
    private Vector3 LastDirectionWhenRolling;


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
    public UnityAction OnOutOfBounds;


    [Header("References")]
    public Rigidbody rigid;
    public SphereCollider sphereCollider;


    [Header("Line Previews")]
    public LinePreview ShotPowerPreview;
    public LinePreview ShotNormalPreview;
    public TextMesh ShotAnglePreview;



    private void Awake()
    {
        gameObject.layer = Layer;
        transform.localScale = new Vector3(Scale, Scale, Scale);

        OnRollingFinished += Utils.EMPTY;
        OnOutOfBounds += Utils.EMPTY;


        ShotPowerPreview.enabled = false;
    }



    private void FixedUpdate()
    {
        // Get the onground value
        IsOnGround = GroundCheck.IsOnGround(transform.position, sphereCollider.radius + GroundCheck.DEFAULT_RADIUS);

        // Do a raycast down to find the gameobject below
        Collider c = null;
        float maxRaycastDistance = 1000;
        if (Physics.Raycast(new Ray(transform.position, -TerrainManager.UP * maxRaycastDistance), out RaycastHit hit, maxRaycastDistance, GroundCheck.GroundMask))
        {
            c = hit.transform.gameObject.GetComponent<Collider>();
        }


        // Get the biome
        CurrentBiome = Biome.Type.None;
        Biome.Type biomeBelow = Biome.GetBiomeSamplePoint(c, Position); ;
        if (IsOnGround)
        {
            CurrentBiome = biomeBelow;
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
        // On the ground
        if (IsOnGround)
        {
            switch (CurrentBiome)
            {
                case Biome.Type.Grass:
                    CurrentPreset = Preset_Grass;
                    break;
                case Biome.Type.Sand:
                    CurrentPreset = Preset_Sand;
                    break;
                case Biome.Type.GrassShort:
                    CurrentPreset = Preset_GrassShort;
                    break;
                case Biome.Type.GrassLong:
                    CurrentPreset = Preset_GrassLong;
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

        // Set the values
        rigid.angularDrag = CurrentPreset.AngularDrag;
        rigid.drag = CurrentPreset.Drag;




        // Record the last direction we were rolling in
        if (State == PlayState.Rolling)
        {
            LastDirectionWhenRolling = Facing;
        }



        // Check out of bounds
        if (!IsFrozen)
        {
            // We are in water or there is nothing below us (left the map)
            if (CurrentBiome == Biome.Type.Water || biomeBelow == Biome.Type.None)
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
        Progress.Shots.Push(new Stats.Shot(Position, Rotation, Angle, Power));

        // Reset the drag just for the shot
        rigid.drag = 0;
        rigid.angularDrag = 0;

        // Apply the force in direction
        Vector3 force = transform.forward * Power * FullPower * CurrentPreset.ShotPowerMultiplier;
        rigid.AddForce(force, ForceMode.Impulse);
    }



    public void Reset()
    {
        // Reset all movement
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


    public void SetShotPowerPreview(bool useRotation, bool useAngle, bool usePower)
    {
        // Calculate which axis to use
        Vector3 offset = transform.forward;
        offset.x = useRotation ? offset.x : 0;
        offset.z = useRotation ? offset.z : 0;

        offset.y = useAngle ? offset.y : 0;

        // Now ensure it is the correct length
        offset.Normalize();

        // Apply power multiplier if we need to
        offset *= usePower ? Power : 1;

        // Assign the point
        ShotPowerPreview.SetPoints(transform.position, offset);
    }


    public void SetShotNormalPreview()
    {
        Vector3 forwardNoYComponent = Forward;
        forwardNoYComponent.y = 0;

        ShotNormalPreview.SetPoints(transform.position, forwardNoYComponent.normalized);
    }


    public void SetShotAnglePreview(string text)
    {
        Vector3 rotation = new Vector3(0, 90, Angle);

        ShotAnglePreview.transform.localEulerAngles = rotation;

        ShotAnglePreview.text = text;
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

        // Face the direction we were rolling last TODO
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




    public void HoleReached(HoleData hole, DateTime reached)
    {
        // Add the hole 
        Progress.HolesReached.Push(new Stats.Pot(hole, reached, Progress.ShotsForThisHole));

        Progress.Shots.Clear();
    }




    public class Stats
    {
        public Stack<Pot> HolesReached = new Stack<Pot>();

        public Stack<Shot> Shots = new Stack<Shot>();
        public int ShotsForThisHole => Shots.Count;

        public int LastHoleReached { get { if (HolesReached.Count > 0) { return HolesReached.Peek().Hole.Number; } else { return 0; } } }


        public Stats()
        {
            Clear();
        }

        public void Clear()
        {
            HolesReached.Clear();
            Shots.Clear();
        }

        public class Pot
        {
            public HoleData Hole;
            public DateTime TimeReached;

            public int ShotsTaken;

            public Pot(in HoleData reached, in DateTime time, in int shots)
            {
                Hole = reached;
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
        public float AngularDrag;
        public float ShotPowerMultiplier;

        public RigidPreset(float drag, float angularDrag, float shotPowerMultiplier)
        {
            Drag = drag;
            AngularDrag = angularDrag;
            ShotPowerMultiplier = shotPowerMultiplier;
        }
    }

    public enum PlayState
    {
        Shooting,
        Flying,
        Rolling,
    }













    private void OnDrawGizmosSelected()
    {
        // Draw the facing
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + Forward / 2);
    }

}
