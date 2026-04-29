using UnityEngine;

public class ArrowController : MonoBehaviour
{
    [SerializeField] private AudioSource bulletSource;
    [SerializeField] private AudioClip flybyClip;
    [SerializeField] private AudioClip hitClip;
    [SerializeField] private AudioClip bodyHitClip;

    private Vector3 _startPos;
    private Vector3 _targetPos;
    private float _travelTime = 0.1f;
    private float _elapsedTime;
    private bool _travelling;
    private bool _bodyHit;
    private bool _hitSomething;

    public void Fire(Transform muzzle, Vector3 targetPos, bool bodyHit = false, bool hitSomething = false)
    {
        CancelInvoke(nameof(Deactivate));
        _targetPos = targetPos;
        _elapsedTime = 0f;
        _travelling = true;
        _bodyHit = bodyHit;
        _hitSomething = hitSomething;

        // detach from any parent so camera/player rotation doesn't drag the arrow
        transform.SetParent(null);

        // position and rotate BEFORE activating so it never appears at the wrong spot
        Vector3 startPos = muzzle.position;
        _startPos = startPos;

        Vector3 dir = targetPos - startPos;
        transform.position = startPos;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);

        gameObject.SetActive(true);

        if (bulletSource != null && flybyClip != null)
            bulletSource.PlayOneShot(flybyClip);
    }

    public void FireFromPosition(Vector3 startPos, Vector3 targetPos, bool hitSomething = true)
    {
        CancelInvoke(nameof(Deactivate));
        _startPos = startPos;
        _targetPos = targetPos;
        _elapsedTime = 0f;
        _travelling = true;
        _bodyHit = false;
        _hitSomething = hitSomething;

        transform.SetParent(null);

        Vector3 dir = targetPos - startPos;
        transform.position = startPos;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);

        gameObject.SetActive(true);

        if (bulletSource != null && flybyClip != null)
            bulletSource.PlayOneShot(flybyClip);
    }

    private void Update()
    {
        if (!_travelling) return;

        _elapsedTime += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsedTime / _travelTime);

        transform.position = Vector3.Lerp(_startPos, _targetPos, t);

        if (t >= 1f)
        {
            _travelling = false;

            if (_hitSomething)
            {
                if (bulletSource != null)
                    bulletSource.PlayOneShot(_bodyHit ? bodyHitClip : hitClip);
                Invoke(nameof(Deactivate), 3f);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }

    private void Deactivate() => gameObject.SetActive(false);
}
