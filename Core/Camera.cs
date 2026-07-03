using Microsoft.Xna.Framework;

public class Camera
{
    public Vector2 Position;

    private float _smoothness = 0.1f;

    public void Follow(Vector2 targetPosition)
    {
        Vector2 target = new Vector2(
            -targetPosition.X + 400,
            -targetPosition.Y + 240
        );

        Position = Vector2.Lerp(Position, target, _smoothness);
    }

    public Matrix GetViewMatrix()
    {
        return Matrix.CreateTranslation(new Vector3(Position, 0f));
    }
}