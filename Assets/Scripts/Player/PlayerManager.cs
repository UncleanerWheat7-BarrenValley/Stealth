using UnityEngine;

public class PlayerManager : MonoBehaviour, ICharacter
{
    [SerializeField]
    PlayerController playerController;

    public delegate void PlayerDeath();
    public static event PlayerDeath playerDied;
    public float moveSpeed { get; set; } = 5;
    public int Health { get; set; } = 5000;

    public void Damage(int damage)
    {
        Health--;
        if (Health <= 0)
        {
            playerDied();
            playerController.SetState(PlayerController.MyState.dead);
            gameObject.tag = "Untagged";
            return;
        }
    }
}
