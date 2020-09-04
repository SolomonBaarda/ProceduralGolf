using UnityEngine;

public interface ICanBeFollowed
{
    Vector3 Forward { get; }

    Vector3 Position { get; }

    bool IsOnGround { get; }
}
