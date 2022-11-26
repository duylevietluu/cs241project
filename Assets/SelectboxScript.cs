using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SelectboxScript : MonoBehaviour
{
    int col, row;
    // Start is called before the first frame update
    void Start()
    {
        col = 1;
        row = 1;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void MoveTo(int tocol, int torow)
    {
        Vector3 vector3 = new Vector3(tocol - col, torow - row);
        transform.localPosition += vector3;
        col = tocol; row = torow;

        //show
        gameObject.SetActive(true);

    }

    public void hide()
    {
        gameObject.SetActive(false);
    }
}
