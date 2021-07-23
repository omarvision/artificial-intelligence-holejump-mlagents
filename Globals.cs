
using UnityEngine;
using UnityEngine.UI;

public static class Globals
{
    public static int Episode = 0;
    public static int Success = 0;
    public static int Fail = 0;
    private static Text txtDebug = null;

    public static void ScreenText(string msg = "")
    {
        if (txtDebug == null)
        {
            try
            {
                txtDebug = GameObject.Find("txtDebug").gameObject.GetComponent<Text>();
            }
            catch (System.Exception ex)
            {
                Debug.Log(string.Format("Globals.ScreenText() EXCEPTION: {0}", ex.Message));
            }            
        }

        if (txtDebug != null)
        {
            if (Success + Fail == 0)
            {
                txtDebug.text = string.Format("Episode={0} | {1}"
                    , Episode
                    , msg
                    );
            }
            else
            {
                float SuccessPercent = (Success / (float)(Success + Fail)) * 100;
                txtDebug.text = string.Format("Episode={0}, Success={1}, Fail={2} %{3} | {4}"
                    , Episode
                    , Success
                    , Fail
                    , SuccessPercent.ToString("0")
                    , msg
                    );
            }
        }

    }
}
