public class GameState
{
    public bool IsPlaying { get; private set; }
    public bool IsAudioPlaying { get; private set; }
    public bool IsPaused { get; private set; }
    public float CurrentTime { get; set; }

    public void StartGame() 
    {
        IsPlaying = true;
        IsPaused = false;
        CurrentTime = -Values.waitTime;
    }

    public void StartAudio()
    {
        IsAudioPlaying = true;
    }

    public void Pause()
    {
        IsPlaying = false;
        IsAudioPlaying = false;
        IsPaused = true;
    }

    public void Resume()
    {
        IsPlaying = true;
        IsAudioPlaying = true;
        IsPaused = false;
    }

    public void EndGame()
    {
        IsPlaying = false;
    }

    public void EndAudio()
    {
        IsAudioPlaying = false;
    }
}