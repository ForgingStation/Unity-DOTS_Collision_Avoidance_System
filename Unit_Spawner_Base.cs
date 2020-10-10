using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Rendering;
using System.Collections.Generic;

public class Unit_Spawner_Base : MonoBehaviour
{
    public int xGridCount;
    public int zGridCount;
    public Entity prefabToSpawn;
    public int destinationDistanceZAxis;
    public int minSpeed;
    public int maxSpeed;
    public float minDistanceReached;
    public int rotationSpeed;
    public float spawnGridEvery;
    public int maxUnitsToSpawn;
    public Mesh mesh;
    public Material material1;
    public Material material2;

    private float3[] allPositions;
    private EntityManager entitymamager;
    private EntityArchetype ea;
    private float elapsedTime;
    private float3 position;
    private float3 currentPosition;
    private List<Entity> spawnedUnits;
    private int collisions;

    void Start()
    {
        collisions = 0;
        spawnedUnits = new List<Entity>();
        allPositions = new float3[xGridCount * zGridCount];
        currentPosition = transform.position;
        entitymamager = World.DefaultGameObjectInjectionWorld.EntityManager;
        ea = entitymamager.CreateArchetype(
                    typeof(Translation),
                    typeof(Rotation),
                    typeof(LocalToWorld),
                    typeof(RenderMesh),
                    typeof(RenderBounds),
                    typeof(UnitV2_Component)
                    );
        int k = 0;
        for (int i = 1; i <= zGridCount; i++)
        {
            for (int j = 1; j <= xGridCount; j++)
            {
                allPositions[k] = new float3(currentPosition.x + j, currentPosition.y + 1, currentPosition.z + i);
                k++;
            }
        }
    }

    void Update()
    {
        elapsedTime += Time.deltaTime;
        if (elapsedTime > spawnGridEvery)
        {
            elapsedTime = 0;
            if (spawnedUnits.Count < maxUnitsToSpawn)
            {
                for (int h = 0; h <= allPositions.Length - 1; h++)
                {
                    if (spawnedUnits.Count >= maxUnitsToSpawn)
                    {
                        break;
                    }
                    position = allPositions[h];
                    Entity e = entitymamager.CreateEntity(ea);
                    entitymamager.AddComponentData(e, new Translation
                    {
                        Value = position
                    });
                    entitymamager.AddComponentData(e, new Rotation
                    {
                        Value = Quaternion.identity
                    });
                    entitymamager.AddComponentData(e, new UnitV2_Component
                    {
                        fromLocation = position,
                        toLocation = new float3(0, 0, destinationDistanceZAxis) + position,
                        currentBufferIndex = 0,
                        speed = UnityEngine.Random.Range(minSpeed, maxSpeed),
                        minDistanceReached = minDistanceReached,
                        routed = true,
                        rotationSpeed = rotationSpeed
                    });
                    entitymamager.AddSharedComponentData(e, new RenderMesh
                    {
                        mesh = mesh,
                        material = material2,
                        castShadows = UnityEngine.Rendering.ShadowCastingMode.On
                    });
                    DynamicBuffer<Unit_Buffer> ub = entitymamager.AddBuffer<Unit_Buffer>(e);
                    ub.Add(new Unit_Buffer { wayPoints = new float3(position.x, position.y, (position.z + destinationDistanceZAxis)) });
                    ub.Add(new Unit_Buffer { wayPoints = position });
                    spawnedUnits.Add(e);
                }
            }
        }

        //Debug
        collisions = 0;
        foreach (Entity e in spawnedUnits)
        {
            RenderMesh r = entitymamager.GetSharedComponentData<RenderMesh>(e);
            UnitV2_Component uc2 = entitymamager.GetComponentData<UnitV2_Component>(e);
            if (uc2.reached == true)
            {
                r.material = material1;
            }
            else
            {
                r.material = material2;
            }
            if (uc2.collided)
            {
                collisions++;
            }
            entitymamager.SetSharedComponentData<RenderMesh>(e, r);
        }
    }

    private void OnGUI()
    {
        //GUI.Box(new Rect(10, 10, 500, 40), "Cell Size : " + 20);
        GUI.Box(new Rect(10, 10, 500, 40), "Units Spawned : " + spawnedUnits.Count);
        //GUI.Box(new Rect(10, 52, 500, 40), "Collisions Per Second: " + collisions / 2); //2 units colliding will be regarded as 1 collision
        GUI.skin.box.fontSize = 25;
    }
}
