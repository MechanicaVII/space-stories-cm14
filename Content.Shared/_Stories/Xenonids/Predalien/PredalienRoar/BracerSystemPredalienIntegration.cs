namespace Content.Shared._Stories.Hunter.Bracer;

public sealed partial class BracerSystem
{
    public bool STTryForceDecloak(EntityUid user)
    {
        if (!TryFindWornBracer(user, out var bracer))
            return false;

        SetCloak(user, bracer.Value, false, true, true);
        return true;
    }
}
