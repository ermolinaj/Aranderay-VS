using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ControlDialogos : MonoBehaviour
{
    public GameObject dialogo;
    public Text txtDialog;
    // Start is called before the first frame update
    void Start()
    {
        dialogo.SetActive(false);
    }
    
    public void Decir(string texto)
    {
        txtDialog.text = texto;
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
