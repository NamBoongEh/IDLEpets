using UnityEngine;

public class PetEffect : MonoBehaviour
{
    public float lifetime  = 1.2f;
    public float riseSpeed = 0.8f;

    static Shader   s_shader;
    static Material s_material;

    SpriteRenderer _sr;
    Camera         _cam;
    float          _elapsed;
    Vector3        _initScale;

    void Awake()
    {
        gameObject.SetActive(true);
        _sr        = GetComponent<SpriteRenderer>();
        _cam       = Camera.main;
        _initScale = transform.localScale;

        if (s_shader == null)
            s_shader = Resources.Load<Shader>("SpriteKeyColor");

        if (s_shader != null && s_material == null)
        {
            s_material           = new Material(s_shader);
            s_material.hideFlags = HideFlags.HideAndDontSave;
        }

        if (s_material != null && _sr != null)
            _sr.sharedMaterial = s_material;
    }

    void Update()
    {
        _elapsed += Time.deltaTime;
        float t = _elapsed / lifetime;

        transform.position += Vector3.up * riseSpeed * Time.deltaTime;

        if (_cam != null)
            transform.rotation = _cam.transform.rotation;

        transform.localScale = _initScale * (1f - t);

        if (_elapsed >= lifetime)
            Destroy(gameObject);
    }
}
