using Godot;

/// <summary>
/// Script cho nhà thật (đã xây xong), chỉ đứng yên và thuộc group "Building".
/// </summary>
public partial class RealHouse : StaticBody2D
{
    [Export] public Sprite2D BuildingSprite;

    public override void _Ready()
    {
        AddToGroup("Building");

        // đặt z-index lớn để luôn nằm trên các tầng tilemap
        ZAsRelative = false;
        ZIndex = 100;
        if (BuildingSprite != null)
        {
            BuildingSprite.ZIndex = ZIndex;
            BuildingSprite.ZAsRelative = false;
        }
    }

    /// <summary>
    /// Gán texture cho nhà dựa theo hướng người chơi đã chọn khi đặt ghost.
    /// </summary>
    public void SetTexture(Texture2D texture)
    {
        if (BuildingSprite != null && texture != null)
        {
            BuildingSprite.Texture = texture;
        }
    }
}
