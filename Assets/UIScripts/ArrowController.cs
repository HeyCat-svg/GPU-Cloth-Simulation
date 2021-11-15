using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowController : MonoBehaviour
{
    public GameObject arrowX, arrowY, arrowZ;
    public float originDistance=4.0f;
    public float distance= 4.0f;
    private enum Direction { dirX, dirY, dirZ };
    private Direction directionFlag;
    public bool isDrag;
    private Vector3 mousePos;
    private Vector3 ScreenSpace, curScreenSpace, offset, CurPosition;

    // Start is called before the first frame update
    void Start()
    {
        isDrag = false;
    }

    private void OnEnable()
    {
        float scale = 0.02f;
        transform.localScale = new Vector3(scale * distance / 4.0f, scale * distance / 4.0f, scale * distance / 4.0f);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Debug.Log(hit.collider.gameObject.name);
                switch (hit.collider.gameObject.name)
                {
                    case "arrowX": directionFlag = Direction.dirX; isDrag = true; break;
                    case "arrowY": directionFlag = Direction.dirY; isDrag = true; break;
                    case "arrowZ": directionFlag = Direction.dirZ; isDrag = true; break;
                    default: break;
                }
            }
            mousePos = Input.mousePosition;
        }
        if (Input.GetMouseButtonUp(0))
        {
            isDrag = false;
        }
        DragTarget();
    }

    public void DragTarget()
    {
        if (isDrag)
        {
            ScreenSpace = Camera.main.WorldToScreenPoint(transform.position); //目标世界坐标转屏幕，获取z值
            curScreenSpace = new Vector3(Input.mousePosition.x, Input.mousePosition.y, ScreenSpace.z);//当前鼠标位置转世界
            offset = transform.position - Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, ScreenSpace.z));
            CurPosition = Camera.main.ScreenToWorldPoint(curScreenSpace) + offset;

            switch (directionFlag)
            {
                case Direction.dirX:
                    transform.position = new Vector3(CurPosition.x, transform.position.y, transform.position.z);
                    break;
                case Direction.dirY:
                    transform.position = new Vector3(transform.position.x, CurPosition.y, transform.position.z);
                    break;
                case Direction.dirZ:
                    transform.position = new Vector3(transform.position.x, transform.position.y, CurPosition.z);
                    break;

            }
            mousePos = Input.mousePosition;
        }
    }
}
