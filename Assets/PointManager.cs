using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PointManager : MonoBehaviour
{
    public TMP_Text comboText;
    public int combo = 0, miss = 0;
    public delegate void ComboDelegate();
    public event ComboDelegate ComboEvent;

    //ui
    public GameObject settlement;
    public TMP_Text S_Combo, S_Miss;
    public bool isSettlement = false;
    public void Combo()
    {
        if (ComboEvent != null)
        {
            ComboEvent();
            ComboEvent = null;
            Debug.Log("combo:" + combo.ToString());
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        settlement.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        comboText.text = combo.ToString() + "\n" + "Combo";
        if (combo == 0)
        {
            comboText.color = new Color(0, 0, 0, 0);
        }
        else
        {
            comboText.color = new Color(0, 0, 0, 255);
        }

        //ui
        S_Combo.text = combo.ToString() + "\n" + "Combo";
        S_Miss.text = miss.ToString() + "\n" + "Miss";
        if (isSettlement)
        {
            settlement.SetActive(true);
        }
        else
        {
            settlement.SetActive(false);
        }
    }

    public void AddCombo()
    {
        combo += 1;
    }
    public void ClearCombo()
    {
        combo = 0;
    }
    public void AddMiss()
    {
        miss += 1;
    }
    public void ClearMiss()
    {
        miss = 0;
    }
    public void setSettlementTrue()
    {
        isSettlement = true;
    }
    public void setSettlementFalse()
    {
        isSettlement = false;
    }
}
