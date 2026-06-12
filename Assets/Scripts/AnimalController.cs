using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AnimalController : MonoBehaviour
{
    [Header("이동")]
    public float moveSpeed   = 2f;
    public float idleMinTime = 2f;
    public float idleMaxTime = 5f;

    [Header("화면 여백 (Viewport 0~1)")]
    [Range(0f, 0.3f)] public float marginX = 0.08f;
    [Range(0f, 0.3f)] public float marginY = 0.08f;

    [Header("크기")]
    [Tooltip("동물 전체 스케일 (0.5 = 원본의 절반)")]
    public float animalScale = 0.55f;
    [Tooltip("원근으로 가장 멀리 갔을 때 화면상 크기가 줄어드는 최소 비율")]
    [Range(0.1f, 1f)] public float minSizeRatio = 0.5f;

    [Header("랜덤 행동")]
    [Tooltip("이동 대신 랜덤 특수 애니메이션이 발동될 확률 (0=없음, 1=항상)")]
    [Range(0f, 1f)] public float specialChance      = 0.25f;
    public float specialMinDuration = 1.5f;
    public float specialMaxDuration = 3.5f;

    [Header("이펙트")]
    public GameObject heartPrefab;
    [Tooltip("동물 키 대비 하트 크기 비율 (0.12 = 동물 높이의 12%)")]
    [Range(0f, 0.5f)] public float heartSizeRatio   = 0.12f;
    public float headHeightOffset = 0.05f;

    enum State { Idle, Walking, Petting, Dragging, Special }
    State _state = State.Idle;

    Animator  _anim;
    Camera    _cam;
    Transform _camTransform;
    Collider  _col;
    Coroutine _loop;
    float     _refDist;
    float     _maxViewportY;
    float     _safeTopY;
    float     _maxGroundDist;
    float     _lastScale;

    const string StateIdle     = "Idle_A";
    const string StateWalk     = "Walk";
    const string StatePet      = "Clicked";
    const string StateDrag     = "Fly";
    const string EyesHappy     = "Eyes_Happy";
    const string EyesBlink     = "Eyes_Blink";
    const int    ShapekeyLayer = 1;

    static readonly string[] SpecialAnims =
    {
        "Fly", "Bounce", "Spin", "Eat", "Sit",
        "Jump", "Roll", "Swim", "Idle_B", "Idle_C", "Attack",
    };

    void Start()
    {
        _anim         = GetComponent<Animator>();
        _cam          = Camera.main;
        _camTransform = _cam.transform;
        _col          = GetComponent<Collider>();

        transform.localScale = Vector3.one * animalScale;
        _lastScale = animalScale;

        Vector3 p = transform.position;
        p.y = 0f;
        transform.position = p;

        transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        if (_anim != null)
        {
            _anim.applyRootMotion = false;
            _anim.Play(EyesBlink, ShapekeyLayer);
        }

        _maxViewportY  = 0.96f;
        _maxGroundDist = _cam.farClipPlane * 0.8f;
        _safeTopY      = CalcMaxViewportY();

        Vector3 centerGround = ViewportToGroundPoint(0.5f, 0.35f);
        _refDist = Vector3.Distance(centerGround, _camTransform.position);

        _loop = StartCoroutine(BehaviorLoop());
    }

    void LateUpdate()
    {
        if (_camTransform == null || _refDist <= 0f) return;

        float dist     = Vector3.Distance(transform.position, _camTransform.position);
        float newScale = Mathf.Max(animalScale, minSizeRatio * animalScale * (dist / _refDist));

        if (newScale == _lastScale) return;
        _lastScale           = newScale;
        transform.localScale = new Vector3(newScale, newScale, newScale);
    }

    IEnumerator BehaviorLoop()
    {
        while (true)
        {
            _state = State.Idle;
            SetWalk(false);
            yield return new WaitForSeconds(Random.Range(idleMinTime, idleMaxTime));

            if (_state != State.Idle) { yield return null; continue; }

            if (Random.value < specialChance)
            {
                yield return StartCoroutine(SpecialRoutine());
            }
            else
            {
                Vector3 target = RandomGroundPos();
                _state = State.Walking;
                SetWalk(true);

                while (_state == State.Walking)
                {
                    Vector3 dir = target - transform.position;
                    dir.y = 0f;

                    if (dir.sqrMagnitude < 0.0025f) break;

                    transform.position = Vector3.MoveTowards(
                        transform.position, target, moveSpeed * Time.deltaTime);
                    FaceDirection(dir);
                    yield return null;
                }
            }
        }
    }

    IEnumerator SpecialRoutine()
    {
        _state = State.Special;

        string anim = SpecialAnims[Random.Range(0, SpecialAnims.Length)];
        if (_anim != null) _anim.Play(anim);

        float endTime = Time.time + Random.Range(specialMinDuration, specialMaxDuration);
        while (Time.time < endTime && _state == State.Special)
            yield return null;

        if (_state == State.Special)
            _state = State.Idle;
    }

    Vector2 _mouseDownScreenPos;
    bool    _dragActive = false;
    const float DragThresholdSqr = 8f * 8f;

    Vector2 _dragStartMouseVp;
    Vector2 _dragStartAnimalVp;

    void OnMouseDown()
    {
        if (_state == State.Dragging) return;

        StopLoop();
        StopAllCoroutines();
        _state      = State.Dragging;
        _dragActive = false;
        SetWalk(false);

        _mouseDownScreenPos = Input.mousePosition;
    }

    void OnMouseDrag()
    {
        if (_state != State.Dragging) return;

        float dx = (float)Input.mousePosition.x - _mouseDownScreenPos.x;
        float dy = (float)Input.mousePosition.y - _mouseDownScreenPos.y;
        if (dx * dx + dy * dy < DragThresholdSqr) return;

        if (!_dragActive)
        {
            _dragActive = true;
            if (_anim != null) _anim.Play(StateDrag);

            Vector3 mVp = _cam.ScreenToViewportPoint(Input.mousePosition);
            Vector3 aVp = _cam.WorldToViewportPoint(transform.position);
            _dragStartMouseVp  = new Vector2(mVp.x, mVp.y);
            _dragStartAnimalVp = new Vector2(aVp.x, aVp.y);
        }

        Vector3 currVp = _cam.ScreenToViewportPoint(Input.mousePosition);
        float vx = _dragStartAnimalVp.x + (currVp.x - _dragStartMouseVp.x);
        float vy = _dragStartAnimalVp.y + (currVp.y - _dragStartMouseVp.y);
        vx = Mathf.Clamp(vx, marginX, 1f - marginX);
        vy = Mathf.Clamp(vy, marginY, _maxViewportY);
        transform.position = ViewportToGroundPoint(vx, vy);
    }

    void OnMouseUp()
    {
        if (_state != State.Dragging) return;

        if (!_dragActive)
            StartCoroutine(PetRoutine());
        else
            RestartLoop();
    }

    IEnumerator PetRoutine()
    {
        _state = State.Petting;
        SetWalk(false);

        if (_anim != null)
        {
            _anim.Play(StatePet);
            _anim.Play(EyesHappy, ShapekeyLayer);
        }

        if (heartPrefab != null)
        {
            GameObject heart = Instantiate(heartPrefab, GetHeadPosition(), Quaternion.identity);
            heart.SetActive(true);
            heart.transform.localScale = Vector3.one * (GetAnimalHeight() * heartSizeRatio);
        }

        yield return new WaitForSeconds(1.5f);

        if (_anim != null) _anim.Play(EyesBlink, ShapekeyLayer);
        RestartLoop();
    }

    void OnEnable()
    {
        if (_cam != null && _loop == null)
        {
            _state = State.Idle;
            _loop  = StartCoroutine(BehaviorLoop());
        }
    }

    void OnDisable()
    {
        _loop  = null;
        _state = State.Idle;
    }

    void StopLoop()
    {
        if (_loop != null) { StopCoroutine(_loop); _loop = null; }
    }

    void RestartLoop()
    {
        StopLoop();
        _loop = StartCoroutine(BehaviorLoop());
    }

    void SetWalk(bool walking)
    {
        if (_anim != null) _anim.Play(walking ? StateWalk : StateIdle);
    }

    void FaceDirection(Vector3 dir)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
    }

    float CalcMaxViewportY()
    {
        for (float vy = 1f; vy > 0.5f; vy -= 0.005f)
        {
            Ray r = _cam.ViewportPointToRay(new Vector3(0.5f, vy, 0f));
            if (r.direction.y >= -0.001f) continue;

            float t = -r.origin.y / r.direction.y;
            if (t >= 0f && t <= _maxGroundDist)
                return vy;
        }
        return 0.65f;
    }

    Vector3 ViewportToGroundPoint(float vx, float vy)
    {
        Ray ray = _cam.ViewportPointToRay(new Vector3(vx, vy, 0f));

        if (ray.direction.y < -0.001f)
        {
            float t = -ray.origin.y / ray.direction.y;
            if (t >= 0f && t <= _maxGroundDist)
            {
                Vector3 p = ray.origin + ray.direction * t;
                p.y = 0f;
                return p;
            }
        }

        float safeY = (_safeTopY > 0.01f) ? _safeTopY : 0.65f;
        Ray farRay  = _cam.ViewportPointToRay(new Vector3(vx, safeY, 0f));
        if (farRay.direction.y < -0.001f)
        {
            float ft = -farRay.origin.y / farRay.direction.y;
            if (ft >= 0f && ft <= _maxGroundDist)
            {
                Vector3 fp = farRay.origin + farRay.direction * ft;
                fp.y = 0f;
                return fp;
            }
        }

        return transform.position;
    }

    Vector3 RandomGroundPos()
    {
        float vx = Random.Range(marginX, 1f - marginX);
        float vy = Random.Range(marginY, _maxViewportY);
        return ViewportToGroundPoint(vx, vy);
    }

    float   GetAnimalHeight()  => _col.bounds.size.y;
    Vector3 GetHeadPosition()  =>
        new Vector3(transform.position.x,
                    _col.bounds.max.y + headHeightOffset,
                    transform.position.z);
}
