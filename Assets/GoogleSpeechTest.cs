using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using FrostweepGames.Plugins.GoogleCloud.SpeechRecognition;
using Fungus;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Google音声認識テスト
/// </summary>
public class GoogleSpeechTest : MonoBehaviour
{
    [SerializeField] private Button recButton;
    [SerializeField] Text resultText;
    //private AudioClip clip1;
    public bool isCalledOnce = false;

    private GCSpeechRecognition _speechRecognition;
    private Dictionary<string, string> dic = new Dictionary<string, string>();

    void Start()
    {
        /*dic.Add("がっこう", "学校");
        dic.Add("がっこう。", "学校");
        dic.Add("だまれ", "黙れ");
        dic.Add("だまれ。", "黙れ");
        dic.Add("ありがとう。", "ありがとう");
        dic.Add("どっかいけ", "どっか行け");
        dic.Add("どっかいけ。", "どっか行け");
        dic.Add("すうがく", "数学");
        dic.Add("すうがく。", "数学");
        dic.Add("しらん", "知らん");
        dic.Add("しらん。", "知らん");*/
        _speechRecognition = GCSpeechRecognition.Instance;

        _speechRecognition.FinishedRecordEvent += OnFinishedRecordEvent;
        _speechRecognition.RecognizeSuccessEvent += OnRecognizeSuccessEvent;

        if (_speechRecognition.HasConnectedMicrophoneDevices())
        {
            _speechRecognition.SetMicrophoneDevice(_speechRecognition.GetMicrophoneDevices()[0]);
        }

        recButton.OnPointerDownAsObservable()
            .Subscribe(_ => _speechRecognition.StartRecord(false)).AddTo(this);

        recButton.OnPointerUpAsObservable()
            .Subscribe(_ => _speechRecognition.StopRecord()).AddTo(this);
    }

    private void OnDestroy()
    {
        _speechRecognition.FinishedRecordEvent -= OnFinishedRecordEvent;
        _speechRecognition.RecognizeSuccessEvent -= OnRecognizeSuccessEvent;
    }

    /// <summary>
    /// 音声認識成功時のコールバックイベント
    /// </summary>
    /// <param name="recognitionResponse">認識結果のレスポンス</param>
    private async void OnRecognizeSuccessEvent(RecognitionResponse recognitionResponse)
    {
        string r = "";

        foreach (var result in recognitionResponse.results)
        {
            foreach (var alternative in result.alternatives)
            {
                if (recognitionResponse.results[0].alternatives[0] != alternative)
                {
                    r = alternative.transcript;
                }
            }
        }

        //resultText.text = r;
        Debug.Log(r);
        /*foreach (string s in dic.Keys)
        {
            if (r == s)
            {
                r = dic[s];
                break;
            }

        }*/

        string str;

        using (var client = new HttpClient())
        {
            str = await client.GetStringAsync("https://api.excelapi.org/language/kanji2kana?text=" + r);
            Debug.Log("hiragana: "+ str);
        
            r = str;

            float[] similarity = new float[MenuDialog.ActiveMenuDialog.CachedButtons.Length];

            Debug.Log(MenuDialog.ActiveMenuDialog.CachedButtons.Length);

            int max_index = 0;
            for (int i = 0; i < MenuDialog.ActiveMenuDialog.CachedButtons.Length; i++)
            {
                Button button2 = MenuDialog.ActiveMenuDialog.CachedButtons[i];
                TextAdapter textAdapter = new TextAdapter();
                textAdapter.InitFromGameObject(button2.gameObject, true);
                Debug.Log(textAdapter.Text);
                string str2 = await client.GetStringAsync("https://api.excelapi.org/language/kanji2kana?text=" + textAdapter.Text);

                similarity[i] = LevenshteinRate(r, str2); //レーベンシュタイン距離を計算
                Debug.Log(similarity[i]);

            }
            Debug.Log("sim: "+ similarity.Distinct().Count());

            if (similarity.Distinct().Count() == 1)
            {
                Debug.Log("同じ距離");
                //EmpathManager2 em = gameObject.AddComponent<EmpathManager2>();
                //em.ButtonClicked(clip1);
            }

            for (int j = 0; j < similarity.Length; j++)
            {
                if (similarity[max_index] > similarity[j])
                {
                    max_index = j;
                }
            }
            Button button3 = MenuDialog.ActiveMenuDialog.CachedButtons[max_index];
            EventSystem.current.SetSelectedGameObject(button3.gameObject);
            button3.onClick.Invoke();

            /*
            for (int i = 0; i < MenuDialog.ActiveMenuDialog.CachedButtons.Length; i++)
            {
                Button button2 = MenuDialog.ActiveMenuDialog.CachedButtons[i];
                TextAdapter textAdapter = new TextAdapter();
                textAdapter.InitFromGameObject(button2.gameObject, true);
                Debug.Log(textAdapter.Text);
                string str2 = await client.GetStringAsync("https://api.excelapi.org/language/kanji2kana?text=" + textAdapter.Text);

                similarity[i] = LevenshteinRate(r, str2); //レーベンシュタイン距離を計算
                Debug.Log(similarity[i]);

                if (similarity[i] < 0.5)
                {
                    EventSystem.current.SetSelectedGameObject(button2.gameObject);
                    button2.onClick.Invoke();
                    break;
                }
            }*/
        }


        /*for (int i=0;i< MenuDialog.ActiveMenuDialog.CachedButtons.Length; i++)
        {
            Button button2 = MenuDialog.ActiveMenuDialog.CachedButtons[i];
            TextAdapter textAdapter = new TextAdapter();
            textAdapter.InitFromGameObject(button2.gameObject, true);
                Debug.Log(textAdapter.Text);

            if (r == textAdapter.Text)
            {
                EventSystem.current.SetSelectedGameObject(button2.gameObject);
                button2.onClick.Invoke();
            }
            
        }*/

    }

    /// <summary>
    /// 録音終了時のコールバックイベント
    /// </summary>
    /// <param name="clip">音声クリップ</param>
    /// <param name="raw">生データ</param>
    private void OnFinishedRecordEvent(AudioClip clip, float[] raw)
    {
        if (clip == null) return;
        //clip1 = clip;

        RecognitionConfig config = RecognitionConfig.GetDefault();
        config.languageCode = Enumerators.LanguageCode.ja_JP.Parse();
        config.audioChannelCount = clip.channels;

        GeneralRecognitionRequest recognitionRequest = new GeneralRecognitionRequest()
        {
            audio = new RecognitionAudioContent()
            {
                content = raw.ToBase64()
            },
            config = config
        };

        _speechRecognition.Recognize(recognitionRequest);
    }
    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            String[] str = new string[MenuDialog.ActiveMenuDialog.CachedButtons.Length];
            for (int i = 0; i < MenuDialog.ActiveMenuDialog.CachedButtons.Length; i++)
            {
                Button button2 = MenuDialog.ActiveMenuDialog.CachedButtons[i];
                TextAdapter textAdapter = new TextAdapter();
                textAdapter.InitFromGameObject(button2.gameObject, true);
                string s = textAdapter.Text;
                str[i] = s.Substring(0, s.Length - 3);
                //Debug.Log("sは" + str[i]);
            }

            
            Debug.Log(str.Distinct().Count());
            if (str.Distinct().Count() != 2)
            {
                _speechRecognition.StartRecord(false);
                Debug.Log("音声認識開始");
                Debug.Log(MenuDialog.ActiveMenuDialog);


            }

        }
        if (Input.GetKeyUp(KeyCode.A))
        {
            //Debug.Log("stop!!!");
            _speechRecognition.StopRecord();
            Debug.Log("stop");
        }

        /*if (Input.GetKeyDown(KeyCode.A))
        {
            _speechRecognition.StartRecord(false);
            Debug.Log("start");
            Debug.Log(MenuDialog.ActiveMenuDialog);
            
            
        }
        if (Input.GetKeyUp(KeyCode.A))
        {
            _speechRecognition.StopRecord();
            Debug.Log("stop");
        }*/


    }

    public static float LevenshteinRate(string str1, string str2)
    {
        int len1 = (str1 != null) ? str1.Length : 0;
        int len2 = (str2 != null) ? str2.Length : 0;

        if (len1 > len2)
        {
            int tmp = len1;
            len1 = len2;
            len2 = tmp;
        }

        if (len1 == 0)
        {
            return (len2 == 0) ? 0.0f : 1.0f;
        }

        return LevenshteinDistance(str1, str2) / (float)len2;
    }

    public static int LevenshteinDistance(string str1, string str2)
    {
        int n1 = 0;
        int n2 = str2.Length + 2;
        int[] d = new int[n2 << 1];

        for (int i = 0; i < n2; i++)
        {
            d[i] = i;
        }

        d[n2 - 1]++;
        d[d.Length - 1] = 0;

        for (int i = 0; i < str1.Length; i++)
        {
            d[n2] = i + 1;

            for (int j = 0; j < str2.Length; j++)
            {
                int v = d[n1++];

                if (str1[i] == str2[j])
                {
                    v--;
                }

                v = (v < d[n1]) ? v : d[n1];
                v = (v < d[n2]) ? v : d[n2];

                d[++n2] = ++v;
            }

            n1 = d[n1 + 1];
            n2 = d[n2 + 1];
        }

        return d[d.Length - n2 - 2];
    }

    internal void StartRecord(bool v)
    {
        throw new NotImplementedException();
    }
}