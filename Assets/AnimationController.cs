using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationController : MonoBehaviour
{
    private Animator animator;
    private Rigidbody body;
    public int speedScale = 1;
    public int angularScale = 1;
    private float TorqueYPrev, VelXPrev, VelZPrev = 0;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        body = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        float TorqueY = (body.angularVelocity.y / angularScale);
        float TorqueYDelta = clamp(TorqueY - TorqueYPrev + 0.5f, 0, 1);
        TorqueYPrev = TorqueY;
        animator.SetFloat("TorqueY", TorqueYDelta);

        float VelX = body.velocity.x / speedScale;
        float VelXDelta = clamp(VelX - VelXPrev, -1, 1);
        VelXPrev = VelX;
        animator.SetFloat("VelX", VelXDelta);

        float VelZ = body.velocity.z / speedScale;
        float VelZDelta = clamp(VelZ - VelZPrev, -1, 1);
        VelZPrev = VelZ;
        animator.SetFloat("VelZ", VelZDelta);
    }
    private float clamp(float inp, float min, float max)
    {
        return (inp < min) ? min : (inp > max) ? max : inp;
    }
}
