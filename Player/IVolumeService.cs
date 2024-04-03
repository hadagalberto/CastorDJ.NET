namespace CastorDJ.Player
{
    public interface IVolumeService
    {
        float GetVolume(ulong guildId);

        void SetVolume(ulong guildId, float volume);
    }
}
