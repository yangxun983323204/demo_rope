using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YxRope;

// 测试绳子两端连接其它刚体
public class TestControllableRopeHang : MonoBehaviour {

    public enum CtrlType
    {
        CtrlHead, CtrlTail
    }

    public CtrlType CType = CtrlType.CtrlHead;
    public float RopeLength = 0.1f;
    public float RopeMass = 0.1f;

    ControllableRope _rope;
    Camera _cam;
    int _floorLayerMask;

    GameObject _obj1, _obj2;

    private void Start()
    {
        Debug.Log("操作说明：鼠标左键拖动来拉动绳子，tab键切换拉动绳子哪一端，1键切换绳首是否挂物体，2键切换绳尾是否挂物体");
        var mat = new Material(Shader.Find("Particles/Alpha Blended"));
        mat.SetColor("_TintColor", new Color32(0xFF, 0x00, 0x00, 0x22));
        _cam = Camera.main;
        _cam.nearClipPlane = 0.0001f;
        _cam.fieldOfView = 30;
        _cam.transform.position = new Vector3(0, 0.223f, -0.35f) * RopeLength * 10;
        _cam.transform.localEulerAngles = new Vector3(30.2f, 0, 0);
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
        ctrl.Auto();
        _rope = ctrl;
        // 创建地面
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.transform.position = Vector3.zero;
        floor.transform.localEulerAngles = Vector3.zero;
        _floorLayerMask = 1 << 6;
        floor.layer = 6;
        // 创建挂接物
        _obj1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var rig1 = _obj1.AddComponent<Rigidbody>();
        rig1.freezeRotation = true;
        _obj1.GetComponent<Renderer>().material.color = Color.red;
        _obj1.transform.position = Vector3.zero;
        _obj1.transform.localScale = Vector3.one * 0.01f;

        _obj2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var rig2 = _obj2.AddComponent<Rigidbody>();
        rig2.freezeRotation = true;
        _obj2.GetComponent<Renderer>().material.color = Color.blue;
        _obj2.transform.position = Vector3.left * 0.05f;
        _obj2.transform.localScale = Vector3.one * 0.01f;

        _rope.HangHead(_obj1.GetComponent<Rigidbody>());
        _rope.HangTail(_obj2.GetComponent<Rigidbody>());
    }

    Rigidbody _handler;
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Tab))
        {
            CType = (CtrlType)(((int)CType + 1) % 2);
            Debug.Log("当前控制:" + CType);
        }

        if (Input.GetKeyUp(KeyCode.Alpha1))
        {
            if (_rope.GetHeadHang() == null)
                _rope.HangHead(_obj1.GetComponent<Rigidbody>());
            else
            {
                _rope.HangHead(null);
                _rope.ManualHead(false);
            }
        }
        else if (Input.GetKeyUp(KeyCode.Alpha2))
        {
            if (_rope.GetTailHang() == null)
                _rope.HangTail(_obj2.GetComponent<Rigidbody>());
            else
            {
                _rope.HangTail(null);
                _rope.ManualTail(false);
            }
        }

        switch (CType)
        {
            case CtrlType.CtrlHead:
                _handler = _rope.GetHeadHang();
                if (_handler == null)
                {
                    _handler = _rope.GetHeadHandler().GetComponent<Rigidbody>();
                    _rope.ManualHead(true, _rope.GetTailHang()==null);
                }
                break;
            case CtrlType.CtrlTail:
                _handler = _rope.GetTailHang();
                if (_handler == null)
                {
                    _handler = _rope.GetTailHandler().GetComponent<Rigidbody>();
                    _rope.ManualTail(true, _rope.GetHeadHang()==null);
                }
                break;
        }

        if (_handler != null)
        {
            if (Input.GetMouseButton(0))
            {
                var ray = _cam.ScreenPointToRay(Input.mousePosition);
                RaycastHit info;
                if (Physics.Raycast(ray, out info, 1000, _floorLayerMask))
                {
                    _handler.position = info.point + new Vector3(0, 0.011f / 2, 0);
                }
            }
        }
    }
}
