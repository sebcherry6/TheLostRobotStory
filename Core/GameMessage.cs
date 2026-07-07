public class GameMessage
{
    public string Text;

    public float Timer;


    public bool Active =>
        Timer > 0;


    public void Show(string text, float time)
    {
        Text = text;
        Timer = time;
    }


    public void Update(float dt)
    {
        if (Timer > 0)
            Timer -= dt;
    }
}