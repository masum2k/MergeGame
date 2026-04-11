using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Smooth camera controller for a 2D farm/merge game.
/// Supports drag-to-pan (touch + mouse), scroll-to-zoom, and pinch-to-zoom.
/// Automatically ignores input when pointer is over UI elements.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Pan Settings")]
    [SerializeField] private float panSmoothing = 8f;
    [SerializeField] private float panInertiaDecay = 5f;

    [Header("Zoom Settings")]
    [SerializeField] private float minZoom = 2f;
    [SerializeField] private float maxZoom = 8f;
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float zoomSmoothing = 8f;

    // Bounds
    private float _minX, _maxX, _minY, _maxY;
    private bool _hasBounds = false;

    // Pan state
    private Camera _cam;
    private Vector3 _dragOrigin;
    private bool _isPanning = false;
    private Vector3 _velocity = Vector3.zero;
    private Vector3 _targetPosition;

    // Zoom state
    private float _targetZoom;

    // Pinch state (mobile)
    private float _lastPinchDist;
    private bool _isPinching = false;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        if (_cam == null) _cam = Camera.main;
    }

    private void Start()
    {
        _targetPosition = transform.position;
        _targetZoom = _cam.orthographicSize;
    }

    /// <summary>
    /// Set the camera movement bounds (called by GameAutoSetup).
    /// </summary>
    public void SetBounds(float minX, float maxX, float minY, float maxY)
    {
        _minX = minX;
        _maxX = maxX;
        _minY = minY;
        _maxY = maxY;
        _hasBounds = true;
    }

    private void Update()
    {
        // Don't process camera input when pointer is over UI
        bool overUI = IsPointerOverUI();

        HandleZoomInput(overUI);
        HandlePanInput(overUI);
        ApplySmoothing();
    }

    // ==========================================
    //  PAN
    // ==========================================

    private Vector3 _lastMousePos;

    private void HandlePanInput(bool overUI)
    {
        // Use right mouse button or middle mouse button for panning (always allowed)
        // Allow left button only if not over UI
        bool panButtonDown = Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2) || (Input.GetMouseButtonDown(0) && !overUI);
        bool panButtonHeld = Input.GetMouseButton(1) || Input.GetMouseButton(2) || (Input.GetMouseButton(0) && _isPanning);

        // --- MOBILE / MOUSE Unified Pan ---
        if (panButtonDown)
        {
            _isPanning = true;
            _lastMousePos = Input.mousePosition;
            _velocity = Vector3.zero;
        }
        else if (panButtonHeld && _isPanning)
        {
            Vector3 currentMousePos = Input.mousePosition;
            if (currentMousePos != _lastMousePos)
            {
                // Convert screen delta to world delta
                Vector3 worldOld = _cam.ScreenToWorldPoint(_lastMousePos);
                Vector3 worldNew = _cam.ScreenToWorldPoint(currentMousePos);
                Vector3 diff = worldOld - worldNew;

                _targetPosition += diff;
                
                // Track velocity for inertia (prevent division by zero)
                if (Time.deltaTime > 0)
                    _velocity = diff / Time.deltaTime;
                
                _lastMousePos = currentMousePos;
            }
        }
        else
        {
            _isPanning = false;
        }

        // Apply inertia when not panning
        if (!_isPanning && _velocity.sqrMagnitude > 0.0001f)
        {
            _targetPosition += _velocity * Time.deltaTime;
            _velocity = Vector3.Lerp(_velocity, Vector3.zero, panInertiaDecay * Time.deltaTime);
        }
    }

    // ==========================================
    //  ZOOM
    // ==========================================

    private void HandleZoomInput(bool overUI)
    {
        // --- MOBILE: Pinch to zoom ---
        if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            float currentDist = Vector2.Distance(t0.position, t1.position);

            if (!_isPinching)
            {
                _isPinching = true;
                _isPanning = false;
                _lastPinchDist = currentDist;
            }
            else
            {
                float delta = _lastPinchDist - currentDist;
                _targetZoom += delta * zoomSpeed * 0.01f;
                _targetZoom = Mathf.Clamp(_targetZoom, minZoom, maxZoom);
                _lastPinchDist = currentDist;
            }
        }
        else
        {
            _isPinching = false;
        }

        // --- DESKTOP: Scroll wheel ---
        if (!overUI)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _targetZoom -= scroll * zoomSpeed;
                _targetZoom = Mathf.Clamp(_targetZoom, minZoom, maxZoom);
            }
        }
    }

    // ==========================================
    //  APPLY
    // ==========================================

    private void ApplySmoothing()
    {
        // Clamp target position within bounds
        if (_hasBounds)
        {
            // Recalculate bounds based on current zoom level
            float orthoH = _targetZoom;
            float orthoW = orthoH * _cam.aspect;

            float effectiveMinX = _minX + orthoW;
            float effectiveMaxX = _maxX - orthoW;
            float effectiveMinY = _minY + orthoH;
            float effectiveMaxY = _maxY - orthoH;

            // If the grid is smaller than viewport, center it
            if (effectiveMinX > effectiveMaxX)
            {
                float center = (_minX + _maxX) * 0.5f;
                effectiveMinX = effectiveMaxX = center;
            }
            if (effectiveMinY > effectiveMaxY)
            {
                float center = (_minY + _maxY) * 0.5f;
                effectiveMinY = effectiveMaxY = center;
            }

            _targetPosition.x = Mathf.Clamp(_targetPosition.x, effectiveMinX, effectiveMaxX);
            _targetPosition.y = Mathf.Clamp(_targetPosition.y, effectiveMinY, effectiveMaxY);
        }

        // Smooth position
        Vector3 newPos = Vector3.Lerp(transform.position, _targetPosition, panSmoothing * Time.deltaTime);
        newPos.z = transform.position.z; // Preserve Z
        transform.position = newPos;

        // Smooth zoom
        _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, _targetZoom, zoomSmoothing * Time.deltaTime);
    }

    // ==========================================
    //  HELPERS
    // ==========================================

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        // On mobile, check all touches
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                if (IsPointerOverRealUI(Input.GetTouch(i).fingerId))
                    return true;
            }
            return false;
        }

        // On desktop, check mouse
        return IsPointerOverRealUI();
    }

    private bool IsPointerOverRealUI(int fingerId = -1)
    {
        if (EventSystem.current == null) return false;

        // If the pointer is not even over a GameObject, return false
        if (fingerId == -1)
        {
            if (!EventSystem.current.IsPointerOverGameObject()) return false;
        }
        else
        {
            if (!EventSystem.current.IsPointerOverGameObject(fingerId)) return false;
        }

        // If it is over something, we need to check if it's UI layer (5)
        // Physics2DRaycaster on the main camera makes IsPointerOverGameObject return true for grid slots.
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = (fingerId == -1) ? (Vector2)Input.mousePosition : Input.GetTouch(fingerId).position;
        
        System.Collections.Generic.List<RaycastResult> results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var res in results)
        {
            // Layer 5 is the default UI layer in Unity
            if (res.gameObject.layer == 5) 
                return true;

            // NEW: Also block panning if we click on a slot that has a crop.
            // This prevents the camera from moving while trying to drag/merge.
            GridSlot slot = res.gameObject.GetComponent<GridSlot>();
            if (slot != null && !slot.IsEmpty && !slot.IsLocked)
                return true;
        }

        return false;
    }
}
