using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using System.Runtime.InteropServices;
using SFB;

//json
[System.Serializable]
public class Map
{
    public string MusicName;
    public string Artist;
    public string Author;
    public string AudioType;
    public List<float> MapInfo;
}
[System.Serializable]
public class Data
{
    public List<Map> Maps;
}

public class AudioSpectrumVisualizer : MonoBehaviour
{
    private AudioSource m_audiosource;
    public AudioClip audioClip;
    public float[] samples;
    public LineRenderer lineRenderer;
    public float activeThreshold;
    private readonly int LINERENDER_POINT_CNT = 128;

    private int bpm;
    private float previousTime;
    public TMP_Text bpmText;
    public LineRenderer beatLine;
    public Transform playerTransform;
    public ParticleSystem playerParticleSystem;

    private bool isPlayGame = false;
    public Transform cameraTransform;
    public RectTransform menuUI;
    public RectTransform otherUI;

    //pool
    public Transform bornPoint;
    public GameObject barriers;
    private int poolMaxSize = 20;
    private ObjectPool<GameObject> barriersPool;
    private int isTop = -1;

    public AudioSource barrierSpawn;
    private float[] nowSamples;
    private float[] lastSamples;
    private bool canSpawn = true;
    public int spawnLimit;

    //mapinfo
    private Map MapData;
    private int playIndex = 0;

    enum PlayMode
    {
        Play,
        AutoGen,
        AIGen
    }
    private PlayMode playMode;

    //UIToolkit
    public GameObject uiPause;
    public GameObject uguiPause;
    public GameObject waitSeconds;
    private UnityEngine.UIElements.Button b_mainMenu;
    private UnityEngine.UIElements.Button b_resume;

    public PointManager pm;

    //webgl button
    public UnityEngine.UI.Button b_play, b_aigen, b_autogen;

    //public stand

    void Awake()
    {
        barriersPool = new ObjectPool<GameObject>(createFunc, actionOnGet, actionOnRelease, actionOnDestroy, true, 10, poolMaxSize);
    }
    GameObject createFunc()
    {
        var obj = Instantiate(barriers, new Vector3(15, 4 * isTop, 0), Quaternion.LookRotation(new Vector3(0, 0, -1 * isTop), new Vector3(0, -isTop, 0)));
        obj.GetComponent<Barrier>().barriersPool = barriersPool;
        return obj;
    }
    void actionOnGet(GameObject obj)
    {
        obj.GetComponent<Barrier>().createTime = Time.time;
        obj.GetComponent<Barrier>().isChecked = false;
        //obj.GetComponent<SpriteRenderer>().material.color = new Color(191 * 0.03f, 0, 0);
        obj.transform.Find("Barrier").GetComponent<Transform>().localPosition = new Vector3(1, 1, 0);
        obj.transform.Find("Barrier").GetComponent<SpriteRenderer>().material.color = new Color(191 * 0.03f, 0, 0);
        obj.transform.SetPositionAndRotation(new Vector3(15, 4 * isTop, 0), Quaternion.LookRotation(new Vector3(0, 0, -1 * isTop), new Vector3(0, -isTop, 0)));
        obj.gameObject.SetActive(true);
    }
    void actionOnRelease(GameObject obj)
    {
        obj.gameObject.SetActive(false);
    }
    void actionOnDestroy(GameObject obj)
    {
        Destroy(obj);
    }

    // Start is called before the first frame update
    void Start()
    {
        /*#if UNITY_WEBGL
            b_play.onClick.AddListener(MenuPlay);
            b_autogen.onClick.AddListener(MenuAutoGen);
        #endif*/

        m_audiosource = gameObject.GetComponent<AudioSource>();
        samples = new float[8192];
        nowSamples = new float[8192];
        lastSamples = new float[8192];
        lineRenderer.positionCount = LINERENDER_POINT_CNT;
        lineRenderer.startWidth = 0.02f;
        lineRenderer.endWidth = 0.02f;

        uiPause.SetActive(false);
        uguiPause.SetActive(false);
        waitSeconds.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        m_audiosource.GetSpectrumData(samples, 0, FFTWindow.BlackmanHarris);
        for (int i = 0, cnt = LINERENDER_POINT_CNT; i < cnt; i++)
        {
            var v = samples[i];
            if (v > activeThreshold)
                lineRenderer.SetPosition(i, new Vector3((i - LINERENDER_POINT_CNT / 2) * 0.2f, v * 20, -5));
            else
                lineRenderer.SetPosition(i, new Vector3((i - LINERENDER_POINT_CNT / 2) * 0.2f, 0, -5));

            lineRenderer.numCapVertices = 1000;
            lineRenderer.numCornerVertices = 1000;


        }

        switch (playMode)
        {
            case PlayMode.AutoGen:
                //AutoGen
                barrierSpawn.GetSpectrumData(nowSamples, 0, FFTWindow.BlackmanHarris);
                float max1 = 0f;
                float max2 = 0f;
                int maxIndex = 0;
                int lastMaxIndex = 0;
                float sum = 0f;
                float average = 0f;
                //nowSamples
                //float calcBegin = Time.time;
                for (int i = 0; i < nowSamples.Length; i++)
                {
                    if (nowSamples[i] > max1)
                    {
                        max2 = max1;
                        max1 = nowSamples[i];
                        maxIndex = i;
                    }
                    else if (nowSamples[i] > max2 && nowSamples[i] < max1)
                    {
                        max2 = nowSamples[i];
                    }
                }
                for (int i = 0; i < nowSamples.Length; i++)
                {
                    sum += nowSamples[i];
                }
                average = sum / nowSamples.Length;
                //LastSamples
                for (int i = 0; i < lastSamples.Length; i++)
                {
                    if (lastSamples[i] > max1)
                    {
                        max2 = max1;
                        max1 = nowSamples[i];
                        lastMaxIndex = i;
                    }
                }
                lastSamples = nowSamples;
                if ((max1 - max2) > average * spawnLimit && maxIndex != lastMaxIndex)
                {
                    //Debug.Log("delay:" + (Time.time - calcBegin));
                    BarrierSpawn();
                }
                break;

            case PlayMode.Play:
                if (isPlayGame)
                {
                    if (playIndex < MapData.MapInfo.Count && barrierSpawn.time >= MapData.MapInfo[playIndex])
                    {
                        BarrierSpawn();
                        playIndex++;
                    }
                }

                break;
        }



        if (Input.GetKeyDown(KeyCode.Space))
        {
            playerTransform.position = new Vector3(playerTransform.position.x, playerTransform.position.y * -1, playerTransform.position.z);
            playerParticleSystem.Play();
        }

        //test
        /*if (Input.GetKeyDown(KeyCode.J))
        {
            isPlayGame = !isPlayGame;
            if (!isPlayGame)
            {
                StopCoroutine(LoadAudioFileCoroutine());
            }
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            BarrierSpawn();
        }*/

        //UIControl
        if (isPlayGame && cameraTransform.position.x < 0)
        {
            cameraTransform.position = new Vector3(Mathf.Lerp(cameraTransform.position.x, 1, 0.01f), 0, -10);
        }
        if (!isPlayGame && cameraTransform.position.x > -22)
        {
            cameraTransform.position = new Vector3(Mathf.Lerp(cameraTransform.position.x, -23, 0.01f), 0, -10);
        }
        if (cameraTransform.position.x > 0)
        {
            cameraTransform.position = new Vector3(0, 0, -10);
        }
        if (cameraTransform.position.x < -22)
        {
            cameraTransform.position = new Vector3(-22, 0, -10);
        }

        if (isPlayGame)
        {
            menuUI.anchoredPosition = new Vector3(Mathf.Lerp(menuUI.anchoredPosition.x, 1000, 0.01f), menuUI.anchoredPosition.y, 0);
            otherUI.anchoredPosition = new Vector3(Mathf.Lerp(otherUI.anchoredPosition.x, -1000, 0.01f), otherUI.anchoredPosition.y, 0);
            uguiPause.SetActive(true);
        }
        else
        {
            menuUI.anchoredPosition = new Vector3(Mathf.Lerp(menuUI.anchoredPosition.x, 0, 0.01f), menuUI.anchoredPosition.y, 0);
            otherUI.anchoredPosition = new Vector3(Mathf.Lerp(otherUI.anchoredPosition.x, 0, 0.01f), otherUI.anchoredPosition.y, 0);
            uguiPause.SetActive(false);
        }

        if (m_audiosource.time >= m_audiosource.clip.length && isPlayGame)
        {
            StartCoroutine(Settlement());
        }

    }

    void FixedUpdate()
    {
        if (beatLine.startColor.a > 0)
        {
            beatLine.startColor -= new Color(0, 0, 0, 0.05f);
            beatLine.endColor -= new Color(0, 0, 0, 0.05f);
        }
    }

    public void LoadFile()
    {
        //string filePath = UnityEditor.EditorUtility.OpenFilePanel("Select Audio", "", "mp3,wav");        
        string filePath = OpenFile();
        Debug.Log(filePath);

        if (!string.IsNullOrEmpty(filePath))
        {
            string[] filePathArray = filePath.Split(".");
            AudioType audioType = AudioType.MPEG;
            switch (filePathArray[filePathArray.Length - 1])
            {
                case "mp3":
                    audioType = AudioType.MPEG;
                    break;

                case "wav":
                    audioType = AudioType.WAV;
                    break;

                case "ogg":
                    audioType = AudioType.OGGVORBIS;
                    break;
            }

            StartCoroutine(LoadAudioFileCoroutine(filePath, audioType));
        }
    }
    public void LoadFolder()
    {
        //string folderPath = UnityEditor.EditorUtility.OpenFolderPanel("Select Foloder", "", "");
        string folderPath = OpenFolder();
        Debug.Log(folderPath);

        if (!string.IsNullOrEmpty(folderPath))
        {
            string mapConfig = LoadMap(folderPath);
            MapData = JsonUtility.FromJson<Map>(mapConfig);
            AudioType audioType = AudioType.MPEG;

            Debug.Log(MapData.MusicName);
            Debug.Log(MapData.Artist);
            Debug.Log(MapData.Author);
            Debug.Log(MapData.AudioType);
            Debug.Log(MapData.MapInfo);

            switch (MapData.AudioType)
            {
                case "mp3":
                    audioType = AudioType.MPEG;
                    break;

                case "wav":
                    audioType = AudioType.WAV;
                    break;

                case "ogg":
                    audioType = AudioType.OGGVORBIS;
                    break;
            }
            StartCoroutine(LoadAudioFileCoroutine(folderPath + "/audio." + MapData.AudioType, audioType));
        }
    }

    IEnumerator LoadAudioFileCoroutine(string filePath = null, AudioType audioType = AudioType.MPEG)
    {
        using (var uwr = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, audioType))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("加载中");
                audioClip = DownloadHandlerAudioClip.GetContent(uwr);

                m_audiosource.clip = audioClip;
                barrierSpawn.clip = audioClip;
                AnalyzeBPM();
                Debug.Log("加载完成");
                pm.ComboEvent += pm.ClearMiss;
                pm.Combo();
                isPlayGame = true;
            }
            else
            {
                Debug.LogError(UnityWebRequest.Result.DataProcessingError);
            }
        }
    }
    string LoadMap(string folderPath)
    {
        string readData;
        string url = folderPath + "/config.json";
        /*using (StreamReader sr = File.OpenText(url))
        {
            readData = sr.ReadToEnd();
            sr.Close();
        }*/
        readData = File.ReadAllText(url);
        return readData;
    }

    void AnalyzeBPM()
    {
        bpm = UniBpmAnalyzer.AnalyzeBpm(m_audiosource.clip);
        Debug.Log("BPM is " + bpm);
        StartCoroutine("AudioPlay");
        barrierSpawn.Play();
    }

    void Beat()
    {
        //beatLine.SetColors(new Color(0, 0, 0, 51), new Color(0, 0, 0, 51));
        canSpawn = true;
        beatLine.startColor = new Color(0, 0, 0, 20);
        beatLine.endColor = new Color(0, 0, 0, 20);
    }

    void BarrierSpawn()
    {
        if (playMode == PlayMode.AutoGen)
        {
            if (canSpawn)
            {
                canSpawn = false;
                isTop = isTop * -1;
                GameObject temp = barriersPool.Get();
            }
        }
        if (playMode == PlayMode.Play)
        {
            isTop = isTop * -1;
            GameObject temp = barriersPool.Get();
        }
    }

    IEnumerator Settlement()
    {
        yield return new WaitForSeconds(1);
        pm.ComboEvent += pm.setSettlementTrue;
        pm.Combo();
    }

    public IEnumerator AudioPlay()
    {
        InvokeRepeating("Beat", 2f, 60f / (float)bpm);
        yield return new WaitForSeconds(2);
        m_audiosource.Play();
    }

    //playmode
    public void MenuAutoGen()
    {
        if (!isPlayGame)
        {
            playMode = PlayMode.AutoGen;
            playerTransform.position = new Vector3(-7f, -3.5f, 0f);
            LoadFile();
        }
    }
    public void MenuPlay()
    {
        if (!isPlayGame)
        {
            playMode = PlayMode.Play;
            playerTransform.position = new Vector3(-7f, -3.5f, 0f);
            LoadFolder();
        }
    }

    public void Pause()
    {
        if (isPlayGame)
        {
            uiPause.SetActive(true);
            b_mainMenu = uiPause.GetComponent<UIDocument>().rootVisualElement.Q<UnityEngine.UIElements.Button>("MainMenu");
            b_resume = uiPause.GetComponent<UIDocument>().rootVisualElement.Q<UnityEngine.UIElements.Button>("Resume");
            b_mainMenu.RegisterCallback<ClickEvent>(evt => { MainMenu(); });
            b_resume.RegisterCallback<ClickEvent>(evt => { StartCoroutine(Resume()); });
            barrierSpawn.Pause();
            m_audiosource.Pause();
            Time.timeScale = 0;
        }
    }
    public IEnumerator Resume()
    {
        if (isPlayGame)
        {
            uiPause.SetActive(false);
            waitSeconds.SetActive(true);
            waitSeconds.GetComponent<TMP_Text>().text = "3";
            yield return new WaitForSecondsRealtime(1);
            waitSeconds.GetComponent<TMP_Text>().text = "2";
            yield return new WaitForSecondsRealtime(1);
            waitSeconds.GetComponent<TMP_Text>().text = "1";
            yield return new WaitForSecondsRealtime(1);
            waitSeconds.SetActive(false);
            barrierSpawn.UnPause();
            m_audiosource.UnPause();
            Time.timeScale = 1;
        }
    }
    public void MainMenu()
    {
        if (isPlayGame)
        {
            isPlayGame = false;
            uiPause.SetActive(false);
            barrierSpawn.Stop();
            m_audiosource.Stop();
            playIndex = 0;
            pm.ComboEvent += pm.setSettlementFalse;
            pm.ComboEvent += pm.ClearCombo;
            pm.ComboEvent += pm.ClearMiss;
            pm.Combo();
            Time.timeScale = 1;
        }
    }


    string OpenFile()
    {
        switch (Application.platform)
        {
            case RuntimePlatform.WindowsPlayer:
                /*FileOpenDialog dialog = new FileOpenDialog();

                dialog.structSize = Marshal.SizeOf(dialog);

                dialog.filter = "mp3 files\0*.mp3\0wav files\0*.wav\0ogg files\0*.ogg\0All Files\0*.*\0\0";

                dialog.file = new string(new char[256]);

                dialog.maxFile = dialog.file.Length;

                dialog.fileTitle = new string(new char[64]);

                dialog.maxFileTitle = dialog.fileTitle.Length;

                dialog.initialDir = UnityEngine.Application.dataPath;  //默认路径

                dialog.title = "Select File";

                dialog.defExt = "mp3,wav,ogg";
                dialog.flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000200 | 0x00000008;  //OFN_EXPLORER|OFN_FILEMUSTEXIST|OFN_PATHMUSTEXIST| OFN_ALLOWMULTISELECT|OFN_NOCHANGEDIR

                if (DialogShow.GetOpenFileName(dialog))
                {
                    return dialog.file;
                }
                else
                {
                    return null;
                }*/
                var extensions = new[] {
                    new ExtensionFilter("Sound Files", "mp3", "wav"),
                    new ExtensionFilter("All Files", "*")
                };
                string path = StandaloneFileBrowser.OpenFilePanel("", "", extensions, false)[0];
                return path;

            case RuntimePlatform.WindowsEditor:
                extensions = new[] {
                    new ExtensionFilter("Sound Files", "mp3", "wav"),
                    new ExtensionFilter("All Files", "*")
                };
                path = StandaloneFileBrowser.OpenFilePanel("", "", extensions, false)[0];
                return path;

            case RuntimePlatform.WebGLPlayer:
                extensions = new[] {
                    new ExtensionFilter("Sound Files", "mp3", "wav"),
                    new ExtensionFilter("All Files", "*")
                };
                path = StandaloneFileBrowser.OpenFilePanel("", "", extensions, false)[0];
                return path;
        }
        return null;
    }
    string OpenFolder()
    {
        switch (Application.platform)
        {
            case RuntimePlatform.WindowsPlayer:
                string path = StandaloneFileBrowser.OpenFolderPanel("", "", false)[0];
                return path;

            case RuntimePlatform.WindowsEditor:
                path = StandaloneFileBrowser.OpenFolderPanel("", "", false)[0];
                return path;

            case RuntimePlatform.WebGLPlayer:
                path = StandaloneFileBrowser.OpenFolderPanel("", "", false)[0];
                return path;
        }
        return null;
    }
}
