public interface ICharacter
{
    int Health { get; set; }
    void Damage(int damage);
    float moveSpeed { get; set; }
}
