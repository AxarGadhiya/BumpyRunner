using System.Collections;
using UnityEngine;

public class Bounce : MonoBehaviour
{
    [Header("Bounce Force")]
    [Tooltip("Impulse force applied to the player at the collision point (along the contact normal).")]
    [SerializeField] float force = 10f;

    [Tooltip("Enable/disable applying physics force to the player when bouncing.")]
    [SerializeField] bool applyForceToPlayer = true;

    [Tooltip("Also apply an extra upward impulse to the player on bounce.")]
    [SerializeField] bool addUpwardForce = false;

    [Tooltip("Upward impulse strength when addUpwardForce is enabled.")]
    [SerializeField] float upwardForce = 5f;

    [Tooltip("If true, blend the hit direction with world up, so the bounce goes partly upward (between normal and up).")]
    [SerializeField] bool blendHitWithUpDirection = false;

    [SerializeField] float stunTime = 0.5f; // (currently used only for design reference)

    private Vector3 hitDir;

    [Header("Ragdoll / Hit")]
    [SerializeField] bool activeRagdoll = true;

    [SerializeField] string debugName;

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Collision ENter name=>" + collision.gameObject.name + " With " + debugName);

        if (collision.gameObject.TryGetComponent<CharacterController>(out CharacterController character))
        {

            if (applyForceToPlayer && collision.rigidbody != null && collision.contactCount > 0 && !character.beingHit)
            {
               

                var contact = collision.GetContact(0);
                // Contact normal points from this collider towards the other; base push direction.
                hitDir = contact.normal.normalized;

                // Optionally blend between hit direction and world up (vector between both, normalized).
                Vector3 bounceDir = hitDir;
                if (blendHitWithUpDirection)
                {
                    Vector3 blended = (hitDir + Vector3.up).normalized;
                    if (blended.sqrMagnitude > 0.0001f)
                        bounceDir = blended;
                }

                // Main bounce impulse.
                collision.rigidbody.AddForce(force * Time.deltaTime * bounceDir, ForceMode.Impulse);

                // Optional extra straight-up bounce on top.
                if (addUpwardForce)
                {
                    collision.rigidbody.AddForce(Time.deltaTime * upwardForce * Vector3.up, ForceMode.Impulse);
                }

                Debug.Log($"<color=cyan>Bounce applying force dir={bounceDir}, force={force}, blendUp={blendHitWithUpDirection}, addUp={addUpwardForce}, upForce={upwardForce}</color>");

                ApplyRagdollRoutine(collision);
            }

          
        }
    }

    private void  ApplyRagdollRoutine(Collision collision)
    {

        if (activeRagdoll)
        {
            if (string.IsNullOrEmpty(debugName))
            {
                debugName = gameObject.name;
            }

            Debug.Log($"<color=yellow>Enter Collision game object </color>{collision.gameObject.name} with {debugName}");

            // collision.gameObject.GetComponentInChildren<Animator>().SetBool("STOPALL", value: true);
            if (collision.gameObject.TryGetComponent<CharacterAnimator>(out CharacterAnimator animator))
            {
                animator.Anim.SetBool("STOPALL", value: true);
            }
            //collision.gameObject.GetComponent<Movement>().GettingHit();

            if (collision.gameObject.TryGetComponent<CharacterController>(out CharacterController character))
            {
                character.GettingHit();
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        Debug.Log("Collision Exit name=>" + collision.gameObject.name + " With " + debugName);
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            if (activeRagdoll)
            {
                if (string.IsNullOrEmpty(debugName))
                {
                    debugName = gameObject.name;
                }

                Debug.Log($"<color=yellow> Enter Collision game object </color>{collision.gameObject.name} with {debugName}");

                //collision.gameObject.GetComponentInChildren<Animator>().SetBool("STOPALL", value: false);

                if (collision.gameObject.TryGetComponent<CharacterAnimator>(out CharacterAnimator animator))
                {
                    animator.Anim.SetBool("STOPALL", value: false);
                }
            }
        }
    }
}
