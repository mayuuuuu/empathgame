using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using Fungus;
using UnityEngine.EventSystems;
using System.Linq;

public class EmpathManager2 : MonoBehaviour
{
    [SerializeField]
    private string m_subscriptionKey = string.Empty;

    [SerializeField]
    private Text empathResult;

    private AudioClip micClip;
    private float[] microphoneBuffer;

    private int head;
    private int position;
    private bool isRecording;

    public int maxRecordingTime;
    private const int samplingFrequency = 11025;
    private string micDeviceName;

    const int HEADER_SIZE = 44;
    const float rescaleFactor = 32767; //to convert float to Int16

    public bool isCalledOnce;

    public void Start()
    {
        isCalledOnce = true;
        micDeviceName = Microphone.devices[0];
    }

    public void ButtonClicked()
    {
        if (!isRecording)
        {
            StartCoroutine(RecordingForEmpathAPI());
        }
    }

    public IEnumerator RecordingForEmpathAPI()
    {
        RecordingStart();

        yield return new WaitForSeconds(maxRecordingTime);

        RecordingStop();

        yield return null;
    }

    public void RecordingStart()
    {
        StartCoroutine(WavRecording(micDeviceName, maxRecordingTime, samplingFrequency));
    }

    public void RecordingStop()
    {
        isRecording = false;
        position = Microphone.GetPosition(null);
        Microphone.End(micDeviceName);
        Debug.Log("Recording end");
        byte[] empathByte = WavUtility.FromAudioClip(micClip);
        StartCoroutine(Upload(empathByte));
    }

    IEnumerator Upload(byte[] wavbyte)
    {
        WWWForm form = new WWWForm();
        form.AddField("apikey", m_subscriptionKey);
        form.AddBinaryData("wav", wavbyte);
        string receivedJson = null;

        using (UnityWebRequest www = UnityWebRequest.Post("https://api.webempath.net/v2/analyzeWav", form))
        {
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);
            }
            else
            {
                receivedJson = www.downloadHandler.text;
                Debug.Log(receivedJson);
            }
        }

        EmpathData empathData = ConvertEmpathToJson(receivedJson);
        empathResult.text = ConvertEmpathDataToString(empathData);

    }

    public IEnumerator WavRecording(string micDeviceName, int maxRecordingTime, int samplingFrequency)
    {
        Debug.Log("Recording start");
        //Recording開始
        isRecording = true;

        //Buffer
        microphoneBuffer = new float[maxRecordingTime * samplingFrequency];
        //録音開始
        micClip = Microphone.Start(deviceName: micDeviceName, loop: false,
                                   lengthSec: maxRecordingTime, frequency: samplingFrequency);
        yield return null;
    }

    public EmpathData ConvertEmpathToJson(string json)
    {
        Debug.AssertFormat(!string.IsNullOrEmpty(json), "Jsonの取得に失敗しています。");

        EmpathData empathData = null;

        try
        {
            empathData = JsonUtility.FromJson<EmpathData>(json);
        }
        catch (System.Exception i_exception)
        {
            Debug.LogWarningFormat("Jsonをクラスへ変換することに失敗しました。exception={0}", i_exception);
            empathData = null;
        }
        return empathData;
    }

    public string ConvertEmpathDataToString(EmpathData empathData)
    {
        string result;
        if (empathData.error == 0)
        {
            int calm = empathData.calm;
            int anger = empathData.anger;
            int joy = empathData.joy;
            int sorrow = empathData.sorrow;
            int energy = empathData.energy;
            result = "calm : " + calm +
                     "\nanger : " + anger +
                     "\njoy : " + joy +
                     "\nsorrow : " + sorrow +
                     "\nenergy : " + energy;

            int[] selectNum = new int[3];
            selectNum[0] = anger;
            selectNum[1] = joy;
            selectNum[2] = sorrow;


            int name = 0;
            int max1 = selectNum[0];
            for (int i = 1; i < selectNum.Length; i++)
            {
                if (max1 < selectNum[i])
                {
                    name = i;
                    max1 = selectNum[i];
                }
            }

            //int max1 = Math.Max(selectNum[0], Math.Max(selectNum[1], selectNum[2])); //一番大きい値を取得
            Debug.Log(max1);

            if (name == 0)
            {
                for (int i = 0; i < MenuDialog.ActiveMenuDialog.CachedButtons.Length; i++)
                {
                    Button button2 = MenuDialog.ActiveMenuDialog.CachedButtons[i];
                    TextAdapter textAdapter = new TextAdapter();
                    textAdapter.InitFromGameObject(button2.gameObject, true);
                    String str = textAdapter.Text;
                    //Debug.Log(textAdapter.Text);

                    if (str.EndsWith("（怒）"))
                    {
                        //Button button3 = MenuDialog.ActiveMenuDialog.CachedButtons[i];
                        EventSystem.current.SetSelectedGameObject(button2.gameObject);
                        button2.onClick.Invoke();
                    }
                }
            }

            if (name == 1)
            {
                for (int i = 0; i < MenuDialog.ActiveMenuDialog.CachedButtons.Length; i++)
                {
                    Button button2 = MenuDialog.ActiveMenuDialog.CachedButtons[i];
                    TextAdapter textAdapter = new TextAdapter();
                    textAdapter.InitFromGameObject(button2.gameObject, true);
                    String str = textAdapter.Text;
                    //Debug.Log(textAdapter.Text);

                    if (str.EndsWith("（笑）"))
                    {
                        //Button button3 = MenuDialog.ActiveMenuDialog.CachedButtons[i];
                        EventSystem.current.SetSelectedGameObject(button2.gameObject);
                        button2.onClick.Invoke();
                    }
                }
            }
            if (name == 2)
            {
                for (int i = 0; i < MenuDialog.ActiveMenuDialog.CachedButtons.Length; i++)
                {
                    Button button2 = MenuDialog.ActiveMenuDialog.CachedButtons[i];
                    TextAdapter textAdapter = new TextAdapter();
                    textAdapter.InitFromGameObject(button2.gameObject, true);
                    String str = textAdapter.Text;
                    //Debug.Log(textAdapter.Text);

                    if (str.EndsWith("（泣）"))
                    {
                        //Button button3 = MenuDialog.ActiveMenuDialog.CachedButtons[i];
                        EventSystem.current.SetSelectedGameObject(button2.gameObject);
                        button2.onClick.Invoke();
                    }
                }
            }


            /*Button button3 = MenuDialog.ActiveMenuDialog.CachedButtons[name];
            EventSystem.current.SetSelectedGameObject(button3.gameObject);
            button3.onClick.Invoke(); //CashedButton[x]でx番目の選択肢を選ぶ*/

        }
        else
        {
            int error = empathData.error;
            string msg = empathData.msg;
            result = "error : " + error +
                     "\nmsg : " + msg;
        }
        return result;
    }

    public void Update()
    {
        /*if (!isCalledOnce)
        {
            ButtonClicked();
            Debug.Log("start");
            isCalledOnce = true;


        }*/
        /*if (isCalledOnce)
        {
            //_speechRecognition.StopRecord();
            Debug.Log("stop");
        }*/
    }

}
