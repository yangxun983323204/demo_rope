using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YxRope
{
    public class LineRenderRope : MonoBehaviour
    {
        public float Length = 1;
        public float Width = 0.01f;
        public int Segment = 20;
        public Vector3 GenAxis = new Vector3(1, 0, 0);
        public int IterCount = 50;
        public bool SelfCollision = false;
        public float RenderScale = 1;
        public float Mass = 0.01f;
        public float AngularDrag = 10f;
        public Material Mat;

        [SerializeField]
        protected Transform HeadHandler;
        [SerializeField]
        protected bool HeadCtrl = true;
        [SerializeField]
        protected Transform TailHandler;
        [SerializeField]
        protected bool TailCtrl = false;

        [Tooltip("是否使用插值渲染")]
        [SerializeField]
        protected bool RenderInterplate = false;
        [SerializeField]
        protected int RenderInterplateCount = 20;

        public Transform[] Nodes;

        public GameObject RopeRoot { get { return _ropeRoot; } }
        protected GameObject _ropeRoot;
        protected float _epR;
        // 胶囊体半径
        protected float _capR;
        // 胶囊体高度
        protected float _capH;
        protected float _span;
        protected Vector3 _dir;
        protected float _segMass;
        protected float _inertia;// 惯性张量，用以控制节点转动的能量
                                 //
        protected Transform _self;
        protected Rigidbody _head;
        protected Rigidbody _tail;
        //
        protected CatmullRomSpline _spline = new CatmullRomSpline();

        protected bool _needAutoRun = true;
        void Start()
        {
            _self = transform;
            if (_needAutoRun)
                Recalc();
        }

        public virtual void Recalc()
        {
            _needAutoRun = false;
            CalcArg();
            SetupRender();
            if (_ropeRoot == null)
                _ropeRoot = new GameObject("RopeRoot-" + System.Guid.NewGuid().ToString());

            for (int i = _ropeRoot.transform.childCount - 1; i >= 0; i--)
            {
                var c = _ropeRoot.transform.GetChild(i);
                c.parent = null;
                Destroy(c.gameObject);
            }
            _ropeRoot.transform.position = transform.position;
            // 创建节点
            Nodes = new Transform[Segment];
            CreateStartNode();
            for (int i = 1; i < Segment - 1; i++)
            {
                CreateSegment(i);
            }
            CreateEndNode();
            // 控制柄
            _head = Nodes[0].GetComponent<Rigidbody>();
            _tail = Nodes[Nodes.Length - 1].GetComponent<Rigidbody>();
            if (HeadHandler != null)
            {
                _head.isKinematic = HeadCtrl;
                foreach (var col in HeadHandler.GetComponentsInChildren<Collider>())
                {
                    Ignore(col, true);
                }
            }

            if (TailHandler != null)
            {
                _tail.isKinematic = TailCtrl;
                foreach (var col in TailHandler.GetComponentsInChildren<Collider>())
                {
                    Ignore(col, true);
                }
            }
        }

        protected void CalcArg()
        {
            //  R + span + Segment + ... + Segment + span + R = Length
            _epR = Width / 2;
            GenAxis = GenAxis.normalized;
            _dir = -GenAxis;
            _span = Length * 0.01f;
            _capR = Width / 2;
            _capH = (Length - _epR * 2 - _span * (Segment - 1)) / (Segment - 2);
            _segMass = Mass / Segment;
            // 动能定理 E=0.5mv^2
            // v=wr w是角速度,r是半径 => E=0.5m(wr)^2 => 令K=mr^2 => E=0.5Kw^2  K为转动惯量
            _inertia = _segMass * Mathf.Pow(_capH, 2) * 10 / _capH;
        }

        protected void CreateStartNode()
        {
            var obj = new GameObject("start");
            var cap = obj.AddComponent<SphereCollider>();
            cap.radius = _epR;
            var rig = obj.AddComponent<Rigidbody>();
            rig.solverIterations = IterCount;
            rig.solverVelocityIterations = IterCount;
            rig.mass = _segMass;
            rig.angularDrag = AngularDrag;
            rig.inertiaTensor = Vector3.one * _inertia;
            Nodes[0] = rig.transform;
            rig.isKinematic = true;
            obj.transform.SetParent(_ropeRoot.transform);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localEulerAngles = Vector3.zero;
        }
        protected void CreateEndNode()
        {
            var i = Nodes.Length - 1;
            var obj = new GameObject("end");
            var cap = obj.AddComponent<SphereCollider>();
            cap.radius = _epR;
            var rig = obj.AddComponent<Rigidbody>();
            rig.solverIterations = IterCount;
            rig.solverVelocityIterations = IterCount;
            rig.mass = _segMass;
            rig.angularDrag = AngularDrag;
            rig.inertiaTensor = Vector3.one * _inertia;
            Nodes[i] = rig.transform;
            obj.transform.SetParent(_ropeRoot.transform);
            var offsetFromOrigin = Mathf.Clamp01(i) * _capR + Mathf.Clamp01(i - 1) * _capH + Mathf.Clamp(i, 0, float.MaxValue) * _span;
            obj.transform.localPosition = GenAxis * (offsetFromOrigin + _capR);
            obj.transform.localEulerAngles = Vector3.zero;
            //
            var joint = obj.AddComponent<ConfigurableJoint>();
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedBody = Nodes[i - 1].GetComponent<Rigidbody>();
            if (!SelfCollision)
            {
                for (int j = i - 1; j >= 0; j--)
                {
                    Physics.IgnoreCollision(cap, Nodes[j].GetComponent<Collider>(), true);
                }
            }

            joint.anchor = Vector3.zero;
            joint.connectedAnchor = GenAxis * (_span + _capH / 2f + _epR); ;
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;
            joint.projectionMode = JointProjectionMode.PositionAndRotation;
            joint.projectionDistance = 0;
            joint.projectionAngle = 0;
            joint.enablePreprocessing = false;
        }
        protected void CreateSegment(int i)
        {
            var obj = new GameObject("node" + i);
            Collider cap = null;
            var ccap = obj.AddComponent<CapsuleCollider>();
            ccap.direction = (int)Mathf.Abs(GenAxis.y) + (int)Mathf.Abs(GenAxis.z) * 2;
            ccap.radius = _capR;
            ccap.height = _capH;
            cap = ccap;
            var rig = obj.AddComponent<Rigidbody>();
            rig.solverIterations = IterCount;
            rig.solverVelocityIterations = IterCount;
            rig.mass = _segMass;
            rig.angularDrag = AngularDrag;
            rig.inertiaTensor = Vector3.one * _inertia;
            Nodes[i] = rig.transform;
            obj.transform.SetParent(_ropeRoot.transform);
            var offsetFromOrigin = Mathf.Clamp01(i) * _capR + Mathf.Clamp01(i - 1) * _capH + Mathf.Clamp(i, 0, float.MaxValue) * _span;
            obj.transform.localPosition = GenAxis * (offsetFromOrigin + _capH / 2f);
            obj.transform.localEulerAngles = Vector3.zero;

            var joint = obj.AddComponent<ConfigurableJoint>();
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedBody = Nodes[i - 1].GetComponent<Rigidbody>();
            if (!SelfCollision)
            {
                for (int j = i - 1; j >= 0; j--)
                {
                    Physics.IgnoreCollision(cap, Nodes[j].GetComponent<Collider>(), true);
                }
            }

            if (i == 1)
            {
                joint.anchor = -GenAxis * (_span + _capH / 2f + _epR);
                joint.connectedAnchor = Vector3.zero;
            }
            else
            {
                joint.anchor = -GenAxis * (_span / 2f + _capH / 2f);
                joint.connectedAnchor = -joint.anchor;
            }

            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;
            joint.projectionMode = JointProjectionMode.PositionAndRotation;
            joint.projectionDistance = 0;
            joint.projectionAngle = 0;
            joint.enablePreprocessing = false;
        }

        LineRenderer _renderer;
        protected void SetupRender()
        {
            if (_renderer == null)
                _renderer = gameObject.AddComponent<LineRenderer>();

            _renderer.widthMultiplier = Width * RenderScale;
            _renderer.positionCount = RenderInterplate ? RenderInterplateCount : Segment;
            _renderer.useWorldSpace = true;
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _renderer.allowOcclusionWhenDynamic = false;
            _renderer.alignment = LineAlignment.View;
            _renderer.numCornerVertices = 16;
            _renderer.numCapVertices = 16;
            _renderer.material = Mat;
        }

        protected virtual void FixedUpdate()
        {
            if (HeadHandler != null)
            {
                if (HeadCtrl)
                {
                    _head.transform.position = HeadHandler.position;
                }
                else
                {
                    HeadHandler.position = _head.transform.position;
                }
            }

            if (TailHandler != null)
            {
                if (TailCtrl)
                {
                    _tail.transform.position = TailHandler.position;
                }
                else
                {
                    TailHandler.position = _tail.transform.position;
                }
            }
        }

        protected virtual void Update()
        {
            if (_renderer != null && Nodes != null)
            {
                if (RenderInterplate)
                {
                    _spline.SetRef(Nodes);
                    float step = 1f / RenderInterplateCount;
                    for (int i = 0; i < RenderInterplateCount; i++)
                    {
                        _renderer.SetPosition(i, _spline.Interp(step * i));
                    }
                }
                else
                {
                    for (int i = 0; i < Nodes.Length; i++)
                    {
                        _renderer.SetPosition(i, Nodes[i].position);
                    }
                }
            }

            var oldHPos = HeadHandler.position;
            var oldTPos = TailHandler.position;
            _self.position = _head.transform.position;
            HeadHandler.position = oldHPos;
            TailHandler.position = oldTPos;
        }

        /// <summary>
        /// 设置绳子的头部辅助控制对象
        /// </summary>
        /// <param name="tran">辅助控制对象</param>
        /// <param name="ctrl">是否由辅助控制对象控制变换(最好不要同时控制首尾)</param>
        public void SetHeadHandler(Transform tran, bool ctrl)
        {

            SetHandler(ref HeadHandler, ref HeadCtrl, ref _head, tran, ctrl);
        }
        /// <summary>
        /// 设置绳子的尾部辅助控制对象
        /// </summary>
        /// <param name="tran">辅助控制对象</param>
        /// <param name="ctrl">是否由辅助控制对象控制变换(最好不要同时控制首尾)</param>
        public void SetTailHandler(Transform tran, bool ctrl)
        {
            SetHandler(ref TailHandler, ref TailCtrl, ref _tail, tran, ctrl);
        }

        protected void SetHandler(ref Transform handler, ref bool state, ref Rigidbody endpoint, Transform target, bool ctrl)
        {
            state = ctrl;
            if (endpoint != null)
                endpoint.isKinematic = state;

            if (handler != target)
            {
                if (Nodes != null)
                {
                    if (handler != null)
                    {
                        foreach (var col in handler.GetComponentsInChildren<Collider>())
                        {
                            Ignore(col, false);
                        }
                    }
                    foreach (var col in target.GetComponentsInChildren<Collider>())
                    {
                        Ignore(col, true);
                    }
                }
            }

            handler = target;
        }
        /// <summary>
        /// 让绳子忽略某物体的碰撞
        /// </summary>
        /// <param name="collider"></param>
        /// <param name="ignore"></param>
        public void Ignore(Collider collider, bool ignore)
        {
            for (int i = 0; i < Nodes.Length; i++)
            {
                Physics.IgnoreCollision(collider, Nodes[i].GetComponent<Collider>(), ignore);
            }
        }

        public Transform GetHeadHandler()
        {
            return HeadHandler;
        }

        public Transform GetTailHandler()
        {
            return TailHandler;
        }

        public void SetRenderInterplate(bool enable, int count = 20)
        {
            RenderInterplate = enable;
            RenderInterplateCount = count;
        }

        protected virtual void OnDestroy()
        {
            Destroy(_ropeRoot);
        }
    }
}
