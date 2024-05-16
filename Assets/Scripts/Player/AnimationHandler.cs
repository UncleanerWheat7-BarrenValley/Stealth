using UnityEngine;

public class AnimationHandler : MonoBehaviour
{
    Animator animator;
    public PlayerInput playerInput;
    int animMovementSpeed;
    int animWallMovementSpeed;
    int attack;
    int gunActive;
    int aim;
    int crouch;
    int wallKnock;
    int death;
    // Start is called before the first frame update
    void Start()
    {
        playerInput = GetComponentInParent<PlayerInput>();
        animator = GetComponent<Animator>();
        animMovementSpeed = Animator.StringToHash("MovementSpeed");
        animWallMovementSpeed = Animator.StringToHash("WallMovementSpeed");
        attack = Animator.StringToHash("Attack");
        gunActive = Animator.StringToHash("GunActive");
        aim = Animator.StringToHash("Aim");
        crouch = Animator.StringToHash("Crouch");
        wallKnock = Animator.StringToHash("WallKnock");
        death = Animator.StringToHash("Death");
    }
   
    private void Update()
    {       
        animator.SetFloat(animMovementSpeed, playerInput.moveAmount);
        animator.SetFloat(animWallMovementSpeed, playerInput.horizontalInput);

        if (playerInput.attack)
        {
            Fire(playerInput.gunFlag, playerInput.aimFlag);
        }
    }

    public void Fire(bool gun, bool aiming)
    {
        animator.SetBool(gunActive, gun);
        animator.SetBool(attack, true);
        animator.SetBool(aim, aiming);
        playerInput.attack = false;
    }

    public void EndAttack() //animation event
    {
        animator.SetBool(attack, false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Enemy")
        {
            Debug.LogWarning("ItHit");
            Debug.LogWarning(other.GetComponent<EnemyManager>().Health);
            other.GetComponent<EnemyManager>().Damage(1);
            Debug.LogWarning(other.GetComponent<EnemyManager>().Health);
        }
    }

    public void Crouch(bool crouched) 
    {
        animator.SetBool(crouch, crouched);
    }

    public void WallKnock()
    {
        animator.SetTrigger(wallKnock);
    }

    public void PlayDeath() 
    {
        animator.SetTrigger(death);
    }

}
