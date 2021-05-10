using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ModalAnalysis;
using WiiBalanceBoard;
using UnityEngine;

public class BalanceGameManager : MonoBehaviour
{
    // Const
    private const string OBSTACLETAG = "BalanceObstacle";
    private const string TARGETTAG = "BalanceTarget";

    // Unity Public
    [SerializeField, Range(1, 100f)]
    private float PullSpeed = 10, PullAcceleration = 15, PullStrength = 1;

    [SerializeField, Range(0, 15)]
    private float PullRange = 4;

    [SerializeField]
    private AudioSource
        OnSuccesSource,
        OnFailureSource
    ;

    [SerializeField]
    private GameObject
        UserSonicObjectPrefab,
        ObstacleSonicObjectPrefab,
        TargetSonicObjectPrefab
    ;

    [SerializeField, Range(1, 10)]
    public int NumObstacles = 2;

    [SerializeField]
    private GameObject[] SpawnAreas;

    [SerializeField]
    private Transform EnvironmentRoot;

    [SerializeField]
    private Transform SurfaceRoot;

    [SerializeField]
    private Transform GameArea;

    // Private
    private Rect areaBounds;

    private ModalManager modalManager;

    private int numActiveSpawnAreas;
    private List<Transform> availablePoints;
    private List<Transform> occupiedPoints;
    private Dictionary<GameObject, Transform> objectPointPairs;

    private Transform userObjectStart;
    private GameObject userObjectInstance;
    private Rigidbody userObjectRigidBody;

    private GameObject targetObjectInstance;
    private List<GameObject> obstacleObjectInstances;

    bool hasTarget;
    private Vector3 pullTarget;

    private float defaultTimeScale;

    private Dictionary<string, System.Action<GameObject>> callbackDict;

    float beginTime;
    float elapsedTime;
    float deadTime = 5f;
    bool onGameObject;
    GameObject connectedObject;

    private void Awake()
    {
        defaultTimeScale = Time.timeScale;
        Time.timeScale = 0;

        //UserSonicObjectPrefab.SetActive(false);

        ObstacleSonicObjectPrefab.SetActive(false);
        TargetSonicObjectPrefab.SetActive(false);

        modalManager = GameObject.Find("Manager")
            .GetComponent<ModalManager>();

        numActiveSpawnAreas = SpawnAreas.Length;
        availablePoints = new List<Transform>();
        occupiedPoints = new List<Transform>();

        foreach (var area in SpawnAreas)
        {
            var points = area.GetComponentsInChildren<Transform>();
            foreach (var point in points)
            {
                if (!area.transform.Equals(point))
                    availablePoints.Add(point);
            }
        }

        obstacleObjectInstances = new List<GameObject>();
        objectPointPairs = new Dictionary<GameObject, Transform>();

        callbackDict = new Dictionary<string, System.Action<GameObject>>
        {
            [TARGETTAG] = OnSucces,
            [OBSTACLETAG] = OnFailure
        };

        Transform[] walls = GameArea
            .GetComponentsInChildren<Transform>();
        Transform leftWall = walls[1];
        Transform rightWall = walls[2];
        Transform topWall = walls[3];
        Transform bottomWall = walls[4];

        float width = rightWall.position.x - leftWall.position.x;
        float height = topWall.position.z - bottomWall.position.z;
        width += rightWall.localScale.z;
        height += topWall.localScale.z;
        float x = -width / 2;
        float y = -height / 2;

        areaBounds = new Rect(x, y, width, height);
    }

    private void Start()
    {
        StartCoroutine(StartAsync());
    }

    private void Update()
    {
        if (onGameObject)
        {
            HandleConnectedObject(connectedObject);
            elapsedTime = Time.time - beginTime;
        }
    }

    private void LateUpdate()
    {
        CheckBounds();
        CheckPullTarget();
    }


    private void CheckPullTarget()
    {
        float prevDist = -1000f;
        Vector3 currentPos = userObjectRigidBody.position;

        obstacleObjectInstances.Add(targetObjectInstance);
        foreach (var obstacle in obstacleObjectInstances)
        {
            Vector3 obsPos
                = obstacle.transform.position;
            obsPos.y = currentPos.y;

            Vector3 direction = obsPos - currentPos;
            float dist = direction.magnitude;

            if (dist >= PullRange)
                continue;

            if (Physics.Raycast(currentPos, direction, out RaycastHit info, direction.magnitude))
                if (info.collider.tag == "Untagged")
                    continue;

            if (dist > prevDist)
            {
                pullTarget = obsPos;
                hasTarget = true;
                prevDist = dist;
            }
        }
        obstacleObjectInstances.Remove(targetObjectInstance);
    }

    private float Map(float x, float in_min, float in_max, float out_min, float out_max)
    {
        return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
    }

    private Vector3 SeekAndArrive(Vector3 target)
    {
        Vector3 velocity = userObjectRigidBody.velocity;
        Vector3 currentPos = userObjectRigidBody.position;
        Vector3 desiredVelocity = target - currentPos;

        float d = desiredVelocity.magnitude;
        desiredVelocity.Normalize();

        float m = Map(d, 0, PullRange, 15f, PullSpeed);
        desiredVelocity = desiredVelocity * m;
            
        Vector3 relative =
            desiredVelocity - velocity;

        return Vector3.ClampMagnitude(relative, PullAcceleration);
    }

    private void FixedUpdate()
    {
        Vector3 pullForce = 
            hasTarget ? SeekAndArrive(pullTarget) : Vector3.zero;

        userObjectRigidBody.AddForce(pullForce * PullStrength, ForceMode.Acceleration);
        hasTarget = false;
    }

    IEnumerator StartAsync()
    {
        while (!SpawnGamePrefabs()) 
            yield return null;

        var controllerHandler = userObjectInstance
            .GetComponent<WBBObjectController>();
        controllerHandler
            .OnUserEnter
            .AddListener(OnUserEventHandler);

        userObjectInstance.SetActive(true);
        targetObjectInstance.SetActive(true);

        foreach (var obstacle in obstacleObjectInstances)
            obstacle.SetActive(true);

        targetObjectInstance.GetComponentsInChildren<AudioSource>()[1].Play();
        foreach (var obstacle in obstacleObjectInstances)
            obstacle.GetComponentsInChildren<AudioSource>()[1].Play();

        Time.timeScale = defaultTimeScale;
    }

    private void SetAsAvailable(Transform value)
    {
        occupiedPoints.Remove(value);
        availablePoints.Add(value);
    }

    private void SetAsOccupied(Transform value)
    {
        availablePoints.Remove(value);
        occupiedPoints.Add(value);
    }

    private void GetSpawnParams(Transform parameter, 
        out Vector3 spawnPosition, 
        out Quaternion spawnRotation, 
        float explicitY = -1000)
    {
        spawnRotation = Quaternion.identity;
        spawnPosition.x = parameter.position.x;
        spawnPosition.z = parameter.position.z;

        float baseY = SurfaceRoot.localScale.y / 2;
        if ((int)explicitY != -1000)
        {
            spawnPosition.y = baseY + explicitY;
        }
        else
        {
            spawnPosition.y = baseY + parameter.position.y;
        }
    }

    private GameObject InstantiateWithParams(
        GameObject prefab, Transform parameter, float explicitY = -1000)
    {
        Vector3 spawnPosition;
        Quaternion spawnRotation;
        GetSpawnParams(parameter, out spawnPosition, out spawnRotation, explicitY);
        GameObject instance = Instantiate(prefab, spawnPosition, spawnRotation, EnvironmentRoot);

        if (!objectPointPairs.ContainsKey(instance))
            objectPointPairs.Add(instance, parameter);

        return instance;
    }

    private Transform GetUserSpawnPoint()
    {
        int areaIdx = Random.Range(0, numActiveSpawnAreas);
        GameObject spawnArea = SpawnAreas[areaIdx];

        Transform[] spawnPoints = spawnArea
            .GetComponentsInChildren<Transform>();
        int numSpawnPoints = spawnPoints.Length;
        int spawnPointIdx = Random.Range(1, numSpawnPoints);

        Transform spawnPoint = spawnPoints[spawnPointIdx];
        SetAsOccupied(spawnPoint);

        return spawnPoint;
    }

    private Transform GetEnvironmentSpawnPoint()
    {
        int numActivePoints = availablePoints.Count;
        int pointIdx = Random.Range(0, numActivePoints);

        Transform spawnPoint = availablePoints[pointIdx];
        SetAsOccupied(spawnPoint);

        return spawnPoint;
    }

    private GameObject SpawnUserPrefab(GameObject prefab)
    {
        userObjectStart = GetUserSpawnPoint();
        return InstantiateWithParams(prefab, userObjectStart, 5f);
    }

    private GameObject SpawnEnvironmentPrefab(GameObject prefab)
    {
        Transform spawnPoint = GetEnvironmentSpawnPoint();
        return InstantiateWithParams(prefab, spawnPoint);
    }

    private void SetTransform(GameObject obj, Transform parameter, float explicitY = -1000)
    {
        Vector3 spawnPosition;
        Quaternion spawnRotation;
        GetSpawnParams(parameter, out spawnPosition, out spawnRotation, explicitY);

        if (!objectPointPairs.ContainsKey(obj))
            objectPointPairs.Add(obj, parameter);

        obj.transform.position = spawnPosition;
        obj.transform.localRotation = spawnRotation;
    }

    private bool SpawnGamePrefabs()
    {
        userObjectInstance = SpawnUserPrefab(UserSonicObjectPrefab);
        userObjectRigidBody = userObjectInstance
            .GetComponent<Rigidbody>();

        targetObjectInstance = SpawnEnvironmentPrefab(TargetSonicObjectPrefab);

        int nobstacles = Mathf.Clamp(NumObstacles, 1, availablePoints.Count - 1);
        for (int i = 0; i < nobstacles; i++)
        {
            var obstacle = SpawnEnvironmentPrefab(ObstacleSonicObjectPrefab);
            obstacleObjectInstances.Add(obstacle);
        }

        SetAsAvailable(userObjectStart);
        return true;
    }


    /*
     * 
     *              GAME LOGIC, CALLBACKS AND GUI 
     *                  FUNCTIONS / METHODS
     * 
     */


    private void OnSucces(GameObject obj)
    {
        OnSuccesSource.Play();
        MoveTarget(obj);
    }

    private void OnFailure(GameObject obj)
    {
        OnFailureSource.Play();
        ResetGame();
    }

    public void OnUserEventHandler(GameObject other, CollisionEvent eventType)
    {
        switch (eventType)
        {
            case CollisionEvent.ENTER:
                connectedObject = other;
                onGameObject = true;
                beginTime = Time.time;
                break;
            case CollisionEvent.EXIT:
                connectedObject = null;
                onGameObject = false;
                elapsedTime = 0;
                break;
        }
    }

    private void HandleConnectedObject(GameObject connected)
    {
        if (elapsedTime > deadTime)
        {
            callbackDict[connected.tag]?.Invoke(connected);
            onGameObject = false;
        }
    }

    private void MoveTarget(GameObject target)
    {
        if (!objectPointPairs.ContainsKey(target))
            return;

        Transform oldSpawnPoint =
             objectPointPairs[target];
        Transform newSpawnPoint =
            GetEnvironmentSpawnPoint();

        SetTransform(target, newSpawnPoint);

        objectPointPairs[target] = newSpawnPoint;
        SetAsAvailable(oldSpawnPoint);
    }

    private void ResetSpawnLocations()
    {
        foreach (var occupiedPoint in occupiedPoints.ToList())
            SetAsAvailable(occupiedPoint);
    }

    private void ResetController()
    {
        var controllerHandler = userObjectInstance
            .GetComponent<WBBObjectController>();
        controllerHandler.ResetAll();
    }

    private void ResetTransforms()
    {
        SetTransform(userObjectInstance, GetUserSpawnPoint(), 5f);
        SetTransform(targetObjectInstance, GetEnvironmentSpawnPoint());

        foreach (var obstacle in obstacleObjectInstances)
            SetTransform(obstacle, GetEnvironmentSpawnPoint());
    }

    public void ResetGame()
    {
        Time.timeScale = 0;

        ResetSpawnLocations();
        ResetController();
        ResetTransforms();

        beginTime = 0;
        elapsedTime = 0;
        Time.timeScale = defaultTimeScale;
    }

    public void PauseGame()
    {

    }

    public void ResumeGame()
    {

    }

    private void CheckBounds()
    {
        Vector3 userPosition =
            userObjectRigidBody.position;

        Vector2 point;
        point.x = userPosition.x;
        point.y = userPosition.z;
        if (!areaBounds.Contains(point))
            ResetGame();
    }

}

public enum CollisionEvent
{
    ENTER,
    STAY,
    EXIT
}