using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YxRope;

public class TestControllableRope : MonoBehaviour {

    public enum CtrlType
    {
        CtrlHead,CtrlTail,AutoFx
    }

    public CtrlType CType = CtrlType.CtrlHead;

    public float RopeLength = 0.1f;
    public float RopeMass = 0.1f;

    ControllableRope _rope;
    Camera _cam;
    int _floorLayerMask;

	// Use this for initialization
	void Start () {
        Debug.Log("操作说明：鼠标左键拖动来拉动绳子，tab键切换拉动绳子哪一端");
        var mat = new Material(Shader.Find("Particles/Alpha Blended"));
        mat.SetColor("_TintColor", new Color32(0xFF, 0x00, 0x00, 0x22));
        Camera.main.transform.position = new Vector3(0.04f, -0.057f, -0.14f);
        Camera.main.nearClipPlane = 0.0001f;
        _cam = Camera.main;
        _cam.transform.position = new Vector3(0, 0.223f, -0.096f) * RopeLength *10;
        _cam.transform.localEulerAngles = new Vector3(30.2f,0,0);
        Destroy(GameObject.Find("Directional Light").GetComponent<Light>());
        // 设置
        var rope = new GameObject("TestRope");
        rope.transform.position = Vector3.up * RopeLength * 1.2f;
        var ctrl = rope.AddComponent<ControllableRope>();
        ctrl.Width = 0.05f * RopeLength;
        ctrl.Length = RopeLength;
        ctrl.Segment = 12;
        ctrl.GenAxis = Vector3.up * -1; // 竖直向下创建
        ctrl.Mass = RopeMass;
        ctrl.AngularDrag = 10;
        ctrl.IterCount = 255;
        ctrl.RenderScale = 0.5f;
        ctrl.Mat = mat;
        ctrl.Recalc();
        ctrl.ManualHead(true);
        _rope = ctrl;
        // 创建地面
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.transform.position = Vector3.zero;
        floor.transform.localEulerAngles = Vector3.zero;
        _floorLayerMask = 1<<6;
        floor.layer = 6;
    }

    Rigidbody _handler;
    // Update is called once per frame
    void Update () {
        if (Input.GetKeyUp(KeyCode.Tab))
        {
            CType = (CtrlType)(((int)CType + 1) % 3);
            Debug.Log("当前控制:" + CType);
        }

        switch (CType)
        {
            case CtrlType.CtrlHead:
                _handler = _rope.ManualHead(true);
                break;
            case CtrlType.CtrlTail:
                _handler = _rope.ManualTail(true);
                break;
            case CtrlType.AutoFx:
                _handler = null;
                _rope.Auto();
                break;
        }

        if (_handler!=null)
        {
            if (Input.GetMouseButton(0))
            {
                var ray = _cam.ScreenPointToRay(Input.mousePosition);
                RaycastHit info;
                if(Physics.Raycast(ray,out info, 1000, _floorLayerMask))
                {
                    _handler.position = info.point + new Vector3(0, RopeLength / 2, 0);
                }
            }
        }
    }
}
