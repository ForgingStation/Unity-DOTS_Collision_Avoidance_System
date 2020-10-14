using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;

public class Unit_Movement_System : SystemBase
{
    public static NativeMultiHashMap<int, float3> cellVsEntityPositions;
    public static int totalCollisions;

    protected override void OnCreate()
    {
        totalCollisions = 0;
        cellVsEntityPositions = new NativeMultiHashMap<int, float3>(0, Allocator.Persistent);
    }

    public static int GetUniqueKeyForPosition(float3 position, int cellSize)
    {
        return (int)(19 * math.floor(position.x / cellSize) + (17 * math.floor(position.z / cellSize)));
    }

    protected override void OnUpdate()
    {
        float deltaTime = Time.DeltaTime;

        EntityQuery eq = GetEntityQuery(typeof(UnitV2_Component));
        cellVsEntityPositions.Clear();
        if (eq.CalculateEntityCount() > cellVsEntityPositions.Capacity)
        {
            cellVsEntityPositions.Capacity = eq.CalculateEntityCount();
        }

        NativeMultiHashMap<int, float3>.ParallelWriter cellVsEntityPositionsParallel = cellVsEntityPositions.AsParallelWriter();
        Entities
            .ForEach((ref UnitV2_Component uc, ref Translation trans) =>
            {
                cellVsEntityPositionsParallel.Add(GetUniqueKeyForPosition(trans.Value, 25), trans.Value);
            }).ScheduleParallel();

        //Resolve All Collisions in current cell and provide average avoidance direction
        /*NativeMultiHashMap<int, float3> cellVsEntityPositionsForJob = cellVsEntityPositions;
        Entities
            .WithReadOnly(cellVsEntityPositionsForJob)
            .ForEach((ref UnitV2_Component uc, ref Translation trans) =>
            {
                int key = GetUniqueKeyForPosition(trans.Value, 25);
                NativeMultiHashMapIterator<int> nmhKeyIterator;
                float3 currentLocationToCheck;
                float distanceThreshold = 1.5f;
                float currentDistance;
                int total = 0;
                uc.avoidanceDirection = float3.zero;
                if (cellVsEntityPositionsForJob.TryGetFirstValue(key, out currentLocationToCheck, out nmhKeyIterator))
                {
                    do
                    {  
                        if (!trans.Value.Equals(currentLocationToCheck))
                        {
                            currentDistance = math.sqrt(math.lengthsq(trans.Value - currentLocationToCheck));
                            if (currentDistance < distanceThreshold)
                            {
                                float3 distanceFromTo = trans.Value - currentLocationToCheck;
                                uc.avoidanceDirection = uc.avoidanceDirection + math.normalize(distanceFromTo / currentDistance);
                                total++;
                            }
                            //Debug
                            if (math.sqrt(math.lengthsq(trans.Value - currentLocationToCheck)) < 0.05f)
                            {
                                uc.timeStamp = 60;
                                uc.collided = true;
                            }
                            if (uc.collided)
                            {
                                uc.timeStamp = uc.timeStamp - deltaTime;
                            }
                            if (uc.timeStamp <= 0)
                            {
                                uc.collided = false;
                            }
                        }
                    } while (cellVsEntityPositionsForJob.TryGetNextValue(out currentLocationToCheck, ref nmhKeyIterator));
                    if (total > 0)
                    {
                        uc.avoidanceDirection = uc.avoidanceDirection / total;
                    }
                }
            }).ScheduleParallel();*/

        //Resolve nearest collision
        NativeMultiHashMap<int, float3> cellVsEntityPositionsForJob = cellVsEntityPositions;
        Entities
            .WithReadOnly(cellVsEntityPositionsForJob)
            .ForEach((ref UnitV2_Component uc, ref Translation trans) =>
            {
                int key = GetUniqueKeyForPosition(trans.Value, 25);
                NativeMultiHashMapIterator<int> nmhKeyIterator;
                float3 currentLocationToCheck;
                float currentDistance = 1.5f;
                int total = 0;
                uc.avoidanceDirection = float3.zero;
                if (cellVsEntityPositionsForJob.TryGetFirstValue(key, out currentLocationToCheck, out nmhKeyIterator))
                {
                    do
                    {
                        if (!trans.Value.Equals(currentLocationToCheck))
                        {
                            if (currentDistance > math.sqrt(math.lengthsq(trans.Value - currentLocationToCheck)))
                            {
                                currentDistance = math.sqrt(math.lengthsq(trans.Value - currentLocationToCheck));
                                float3 distanceFromTo = trans.Value - currentLocationToCheck;
                                uc.avoidanceDirection = math.normalize(distanceFromTo / currentDistance);
                                total++;
                            }
                            //Debug
                            if (math.sqrt(math.lengthsq(trans.Value - currentLocationToCheck)) < 0.05f)
                            {
                                uc.timeStamp = 60;
                                uc.collided = true;
                            }
                            if (uc.collided)
                            {
                                uc.timeStamp = uc.timeStamp - deltaTime;
                            }
                            if (uc.timeStamp <= 0)
                            {
                                uc.collided = false;
                            }
                        }
                    } while (cellVsEntityPositionsForJob.TryGetNextValue(out currentLocationToCheck, ref nmhKeyIterator));
                    if (total > 0)
                    {
                        uc.avoidanceDirection = uc.avoidanceDirection / total;
                    }
                }
            }).ScheduleParallel();

        Entities
            .ForEach((ref UnitV2_Component uc, ref DynamicBuffer<Unit_Buffer> ub, ref Translation trans, ref Rotation rot) =>
            {
                if (ub.Length > 0 && uc.routed)
                {
                    uc.waypointDirection = math.normalize(ub[uc.currentBufferIndex].wayPoints - trans.Value);
                    uc.waypointDirection = uc.waypointDirection + uc.avoidanceDirection;
                    trans.Value += uc.waypointDirection * uc.speed * deltaTime;
                    rot.Value = math.slerp(rot.Value, quaternion.LookRotation(uc.waypointDirection, math.up()), deltaTime * uc.rotationSpeed);
                    if (!uc.reached && math.distance(trans.Value, ub[uc.currentBufferIndex].wayPoints) <= uc.minDistanceReached && uc.currentBufferIndex < ub.Length - 1)
                    {
                        uc.currentBufferIndex = uc.currentBufferIndex + 1;
                        if (uc.currentBufferIndex == ub.Length - 1)
                        {
                            uc.reached = true;
                        }
                    }
                    else if (uc.reached && math.distance(trans.Value, ub[uc.currentBufferIndex].wayPoints) <= uc.minDistanceReached && uc.currentBufferIndex > 0)
                    {
                        uc.currentBufferIndex = uc.currentBufferIndex - 1;
                        if (uc.currentBufferIndex == 0)
                        {
                            uc.reached = false;
                        }
                    }
                }
            }).ScheduleParallel();

    //Debug - HasKey
    /*Entities
    .ForEach((ref UnitV2_Component uc, ref Translation trans) =>
    {
        uc.hashKey = GetUniqueKeyForPosition(trans.Value, 20);
    }).ScheduleParallel();*/

    }

    protected override void OnDestroy()
    {
        cellVsEntityPositions.Dispose();
    }
}
