public class Player
{
    public string id;
    public string jobType;
    public float x;
    public float y;
    public bool isJumping;
    public bool isRunning;

    public Player(string id, float x, float y, bool isJumping, bool isRunning )
    {
        this.id = id;
        this.x = x;
        this.y = y;
        this.isJumping = isJumping;
        this.isRunning = isRunning;
    }

    public void Update(float x, float y, bool isJumping, bool isRunning)
    {
        this.x = x;
        this.y = y;
        this.isJumping = isJumping;
        this.isRunning = isRunning;
    }
}