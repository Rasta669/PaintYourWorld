using UnityEngine;
using UnityEngine.Video;

public class VideoDisplay : MonoBehaviour
{

    [SerializeField] string videoFileName;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PlayVideo();
    }

    public void PlayVideo()
    {
        VideoPlayer vplayer = GetComponent<VideoPlayer>();
        if (vplayer != null)
        {
            string vpath = System.IO.Path.Combine(Application.streamingAssetsPath, videoFileName);
            Debug.Log(vpath);
            vplayer.url = vpath;
            vplayer.Play();
        }
    }
}

