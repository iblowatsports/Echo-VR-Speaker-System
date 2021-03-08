using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DropdownMouseOver : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public bool isOver = false;
    private bool hasReset = true;
    private bool awaitingReset = false;
    private Dropdown _dropdown;

    public void Start(){
        _dropdown = GameObject.Find("AppSelectionDropdown").GetComponent<Dropdown>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        var dropdownList = _dropdown.transform.Find("Dropdown List");
        if(_dropdown.transform.Find("Dropdown List") == null){
            if(hasReset){
                isOver = true;
                hasReset = false;
            }
        }
    }

    public void FixedUpdate(){
        if(awaitingReset && _dropdown.transform.Find("Dropdown List") == null){
            awaitingReset = false;
            hasReset = true;
            isOver = false;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        var dropdownList = _dropdown.transform.Find("Dropdown List");
        if(_dropdown.transform.Find("Dropdown List") == null){
            hasReset = true;
            isOver = false;
        }else{
            awaitingReset = true;
        }
    }
}