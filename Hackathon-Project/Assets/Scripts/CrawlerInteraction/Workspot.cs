using UnityEngine;
using UnityEngine.Events;

public class WorkSpot : MonoBehaviour
{
    public enum SpotState {Suspected, Detected, Repaired}
    
    [SerializeField] private string spotId = "Blade 1 - Spot A";
    [Tooltip("Thin cube lying on the surface. Its local X axis is the crack's length.")]
    [SerializeField] private Renderer crackRenderer;
    
    [Header("Scan (sweep over the area)")]
    [SerializeField] private float scanAreaSize = 0.2f;
    [SerializeField] private int scanGridSize = 3;
    [Tooltip("The tool head must get this close to a scan marker. It may hover, no touching needed.")]
    [SerializeField] private float scanRadius = 0.08f;

    [Header("Epoxy (trace along crack)")]
    [SerializeField] private int epoxyPointCount = 5;
    [Tooltip("The tool head must touch the surface and get this close to an epoxy marker.")]
    [SerializeField] private float epoxyRadius = 0.05f;

    [Header("Look")]
    [SerializeField] private Material markerMaterial; //optional, otherwise the default material is used
    [SerializeField] private float markerSize = 0.02f;
    [SerializeField] private Color pendingColor = Color.white;
    [SerializeField] private Color scannedColor = Color.cyan;
    [SerializeField] private Color epoxyColor = Color.yellow;
    [SerializeField] private Color crackColor = Color.white;
    [SerializeField] private Color repairedColor = Color.grey;

    [Header("Events (hook up sounds, haptics, UI…)")]
    public UnityEvent onCrackDetected;
    public UnityEvent onRepaired;
    
    [Header("Surface")]
    [SerializeField] private LayerMask surfaceMask; //CrawlSurface: markers are placed onto the curved blade

    public SpotState State { get; private set; } = SpotState.Suspected;
    public float Progress { get; private set; } //0..1 of the current step
    public string SpotId => spotId;

    private ToolarmController _toolArm;
    private ToolSelector _toolSelector;
    private Renderer[] _scanMarkers;
    private Renderer[] _epoxyMarkers;
    private bool[] _scanDone;
    private bool[] _epoxyDone;
    
    private void Awake()
    {
        _toolArm = FindFirstObjectByType<ToolarmController>();
        _toolSelector = FindFirstObjectByType<ToolSelector>();

        _scanMarkers = CreateScanMarkers();
        _epoxyMarkers = CreateEpoxyMarkers();
        _scanDone = new bool[_scanMarkers.Length];
        _epoxyDone = new bool[_epoxyMarkers.Length];

        ShowState(SpotState.Suspected);
    }

    
    
    private void Update()
    {
        if (_toolArm == null || _toolSelector == null || !_toolArm.ToolModeActive)
            return;

        ToolSelector.ToolType tool = _toolSelector.CurrentTool;

        if (State == SpotState.Suspected && tool == ToolSelector.ToolType.Scan)
        {
            if (Visit(_scanMarkers, _scanDone, scanRadius, scannedColor))
                CompleteScan();
        }
        else if (State == SpotState.Detected && tool == ToolSelector.ToolType.Epoxy)
        {
            if (Visit(_epoxyMarkers, _epoxyDone, epoxyRadius, epoxyColor))
                CompleteRepair();
        }
    }

    
    
    private bool Visit(Renderer[] markers, bool[] done, float radius, Color doneColor)
    {
        Vector3 toolPosition = _toolArm.ToolHead.position;
        int doneCount = 0;

        for (int i = 0; i < markers.Length; i++)
        {
            if (!done[i] && Vector3.Distance(toolPosition, markers[i].transform.position) <= radius)
            {
                done[i] = true;
                markers[i].material.color = doneColor;
            }
            if (done[i])
                doneCount++;
        }

        Progress = (float)doneCount / markers.Length;
        return doneCount == markers.Length;
    }

    
    
    private void CompleteScan()
    {
        ShowState(SpotState.Detected);
        InspectionLog.AddEntry(spotId, InspectionLog.DamageDetected, transform.position);
        onCrackDetected.Invoke();
    }

    
    
    private void CompleteRepair()
    {
        ShowState(SpotState.Repaired);
        InspectionLog.AddEntry(spotId, InspectionLog.Repaired, transform.position);
        onRepaired.Invoke();
    }

    
    
    private void ShowState(SpotState state)
    {
        State = state;
        Progress = 0f;

        SetActive(_scanMarkers, state == SpotState.Suspected);
        SetActive(_epoxyMarkers, state == SpotState.Detected);

        crackRenderer.enabled = state != SpotState.Suspected;
        if (state == SpotState.Repaired)
            crackRenderer.material.color = repairedColor;
    }

    
    
    private Renderer[] CreateScanMarkers()
    {
        int n = Mathf.Max(2, scanGridSize);
        var markers = new Renderer[n * n];

        for (int x = 0; x < n; x++)
        {
            for (int z = 0; z < n; z++)
            {
                float px = Mathf.Lerp(-0.5f, 0.5f, x / (n - 1f)) * scanAreaSize;
                float pz = Mathf.Lerp(-0.5f, 0.5f, z / (n - 1f)) * scanAreaSize;
                markers[x * n + z] = CreateMarker(new Vector3(px, markerSize * 0.5f, pz));
            }
        }
        return markers;
    }

    
    
    private Renderer[] CreateEpoxyMarkers()
    {
        int n = Mathf.Max(2, epoxyPointCount);
        var markers = new Renderer[n];
        GetCrackEnds(out Vector3 start, out Vector3 end);

        for (int i = 0; i < n; i++)
        {
            Vector3 local = transform.InverseTransformPoint(Vector3.Lerp(start, end, i / (n - 1f)));
            local.y = markerSize * 0.5f; //on the surface
            markers[i] = CreateMarker(local);
        }
        return markers;
    }
    
    
    
    private void GetCrackEnds(out Vector3 start, out Vector3 end)
    {
        Transform crack = crackRenderer.transform;
        MeshFilter filter = crackRenderer.GetComponent<MeshFilter>();
        Bounds bounds = filter != null && filter.sharedMesh != null
            ? filter.sharedMesh.bounds
            : new Bounds(Vector3.zero, Vector3.one);

        Vector3 bestHalf = Vector3.zero;
        float bestLength = -1f;
        foreach (Vector3 axis in new[] { Vector3.right, Vector3.up, Vector3.forward })
        {
            Vector3 half = Vector3.Scale(axis, bounds.extents);
            float length = Vector3.ProjectOnPlane(crack.TransformVector(half), transform.up).magnitude;
            if (length > bestLength)
            {
                bestLength = length;
                bestHalf = half;
            }
        }

        start = crack.TransformPoint(bounds.center - bestHalf);
        end = crack.TransformPoint(bounds.center + bestHalf);
    }

    
    
    private Renderer CreateMarker(Vector3 localPosition)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(marker.GetComponent<Collider>());
        marker.name = "Marker";
        marker.transform.SetParent(transform, false);
        marker.transform.localPosition = SnapToSurface(localPosition);
        marker.transform.localScale = Vector3.one * markerSize;

        Renderer markerRenderer = marker.GetComponent<Renderer>();
        if (markerMaterial != null)
            markerRenderer.sharedMaterial = markerMaterial;
        markerRenderer.material.color = pendingColor;
        return markerRenderer;
    }

    
    
    private static void SetActive(Renderer[] markers, bool active)
    {
        foreach (Renderer marker in markers)
            marker.gameObject.SetActive(active);
    }
    
    

//Moves a marker position onto the actual blade surface below/above it
    private Vector3 SnapToSurface(Vector3 localPosition)
    {
        Vector3 up = transform.up;
        Vector3 world = transform.TransformPoint(localPosition);
        if (Physics.Raycast(world + up * 0.3f, -up, out RaycastHit hit, 0.6f, surfaceMask, QueryTriggerInteraction.Ignore))
            return transform.InverseTransformPoint(hit.point + hit.normal * (markerSize * 0.5f));
        return localPosition;
    }
    
    
    
}