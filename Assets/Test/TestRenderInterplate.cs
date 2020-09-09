using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YxRope;

public class TestRenderInterplate : MonoBehaviour {

    public float RopeLength = 0.05f;
    public float RopeWidth = 0.005f;
    public float RopeRenderScale = 0.01f;
    public int Seg = 6;
    public float RopeMass = 0.1f;
    public int RenderInterplateCount = 20;

    ControllableRope _rope;
    Camera _cam;

    // Use this for initialization
    void Start()
    {
        var mat = new Material(Shader.Find("Particles/Alpha Blended"));
        mat.SetColor("_TintColor", new Color32(0xFF, 0x00, 0x00, 0x22));
        Camera.main.transform.position = new Vector3(0.04f, -0.057f, -0.14f);
        Camera.main.nearClipPlane = 0.0001f;
        _cam = Camera.main;
        _cam.transform.position = new Vector3(0, 0.223f, -0.096f) * RopeLength * 10;
        _cam.transform.localEulerAngles = new Vector3(30.2f, 0, 0);
        Destroy(GameObject.Find("Directional Light").GetComponent<Light>());
        // 设置
        var rope = new GameObject("TestRope");
        rope.transform.position = Vector3.up * RopeLength * 1.2f;
        var ctrl = rope.AddComponent<ControllableRope>();
        ctrl.TightenAmend = true;
        ctrl.Width = RopeWidth;
        ctrl.Length = RopeLength;
        ctrl.Segment = Seg;
        ctrl.GenAxis = Vector3.left;
        ctrl.Mass = RopeMass;
        ctrl.AngularDrag = 10;
        ctrl.IterCount = 255;
        ctrl.RenderScale = RopeRenderScale;
        ctrl.SetRenderInterplate(true, RenderInterplateCount);
        ctrl.Mat = mat;
        ctrl.Recalc();
        ctrl.ManualHead(true, false);
        ctrl.ManualTail(true, false);
        _rope = ctrl;
    }
}
