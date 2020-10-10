using Unity.Entities;
using Unity.Mathematics;

public struct UnitV2_Component : IComponentData
{
    public float3 toLocation;
    public float3 fromLocation;
    public bool routed;
    public bool reached;
    //Movement
    public float3 waypointDirection;
    public float speed;
    public float minDistanceReached;
    public int currentBufferIndex;
    public int rotationSpeed;
    //Collision Avoidance
    public float3 avoidanceDirection;
    //Debug
    public bool collided;
    public int hashKey;
    public float timeStamp;
}
