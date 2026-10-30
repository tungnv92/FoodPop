using UnityEngine;

public class ISaveData<T>
{
    public virtual void OnSaveData(string sKey)
    {
        string sValue = JsonUtility.ToJson(this);
        PlayerPrefs.SetString(sKey, sValue);
        //Debug.Log(""+ sKey +"_"+ sValue);
    }
    public virtual T OnLoadData(string sKey)
    {
        string sData = PlayerPrefs.GetString(sKey);
        //Debug.Log("OnLoadData " + sKey + "_Value: " + sData);
        if (string.IsNullOrEmpty(sData))
        {
            return default(T);
        } else
        {
            T saveData = JsonUtility.FromJson<T>(sData);
            return saveData;
        }
    }
}
