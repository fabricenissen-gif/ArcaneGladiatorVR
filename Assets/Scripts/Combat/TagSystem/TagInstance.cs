public class TagInstance
{
    public TagType Type { get; private set; }
    public float Duration { get; private set; }
    public float RemainingTime { get; private set; }
    public int StackCount { get; private set; }
    public bool IsExpired => RemainingTime <= 0f;

    public TagInstance(TagType type, float duration, int initialStacks = 1)
    {
        Type = type;
        Duration = duration;
        RemainingTime = duration;
        StackCount = initialStacks;
    }

    public void Tick(float deltaTime)
    {
        RemainingTime -= deltaTime;
    }

    public void Refresh(float newDuration)
    {
        Duration = newDuration;
        RemainingTime = newDuration;
    }

    public void AddStack()
    {
        StackCount++;
    }

    public void SetStacks(int count)
    {
        StackCount = count;
    }
}