using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace YxRope
{
    /// <summary>
    /// 绳子两端自带控制柄并且控制柄之间由关节链接,用于绳子要挂重物的需求
    /// </summary>
    public class ControllableRope : LineRenderRope
    {
        public new float Mass = 0.1f;
        [Tooltip("紧绷修正")]
        public bool TightenAmend = false;
        private float _rawLength;
        protected ConfigurableJoint _epJoint = null;

        bool _useExtHeadHandler = false;
        bool _useExtTailHandler = false;
        /// <summary>
        /// 设置使用外部对象作为控制柄
        /// </summary>
        public void SetExtHandler(Transform head, Transform tail)
        {
            HeadHandler = head;
            _useExtHeadHandler = head != null;
            TailHandler = tail;
            _useExtTailHandler = tail != null;
        }
        /// <summary>
        /// 改变参数后重新生成绳子
        /// </summary>
        public override void Recalc()
        {
            _rawLength = Length;
            if (TightenAmend)
                Length = _rawLength - Segment / 4 * 0.01f;

            base.Mass = 0.001f * Length * Width / 0.005f;// 绳子是完全受两个控制柄控制的，所以绳子的质量不影响，设成一个效果好的值就行
            base.Recalc();
            CreateEndPoints();
        }
        /// <summary>
        /// 手动控制头部
        /// </summary>
        /// <returns>头部控制柄刚体</returns>
        public Rigidbody ManualHead(bool enable, bool exclusive = true)
        {
            var hRig = HeadHandler.GetComponent<Rigidbody>();
            var tRig = TailHandler.GetComponent<Rigidbody>();
            hRig.isKinematic = enable;
            if (enable && exclusive)
                tRig.isKinematic = false;
            return hRig;
        }
        /// <summary>
        /// 手动控制尾部
        /// </summary>
        /// <returns>尾部控制柄刚体</returns>
        public Rigidbody ManualTail(bool enable, bool exclusive = true)
        {
            var hRig = HeadHandler.GetComponent<Rigidbody>();
            var tRig = TailHandler.GetComponent<Rigidbody>();
            if (enable && exclusive)
                hRig.isKinematic = false;
            tRig.isKinematic = enable;
            return tRig;
        }
        /// <summary>
        /// 自由物理驱动
        /// </summary>
        public void Auto()
        {
            var hRig = HeadHandler.GetComponent<Rigidbody>();
            var tRig = TailHandler.GetComponent<Rigidbody>();
            hRig.isKinematic = false;
            tRig.isKinematic = false;
        }

        [System.Obsolete("可操控绳子不支持外部设置控制柄，使用ManualHead或GetHeadHandler获取头部控制柄", true)]
        public new void SetHeadHandler(Transform tran, bool ctrl) { }
        [System.Obsolete("可操控绳子不支持外部设置控制柄，使用ManualTail或GetTailHandler获取尾部控制柄", true)]
        public new void SetTailHandler(Transform tran, bool ctrl) { }

        private ConfigurableJoint _exJoint = null;
        public Rigidbody ExHead { get { return _exHead; } }
        public Rigidbody ExTail { get { return _exTail; } }
        private Rigidbody _exHead = null;
        private Rigidbody _exTail = null;
        private Vector3 _exHeadOffset;
        private Vector3 _exTailOffset;
        /// <summary>
        /// 悬挂绳首到一个刚体上
        /// </summary>
        public void HangHead(Rigidbody target, Vector3 offset)
        {
            Destroy(_exJoint);
            _exJoint = null;
            if (_exHead != target)
            {
                if (_exHead != null)
                    foreach (var c in _exHead.GetComponentsInChildren<Collider>())
                    {
                        Ignore(c, false);
                    }

                if (target != null)
                    foreach (var c in target.GetComponentsInChildren<Collider>())
                    {
                        Ignore(c, true);
                    }
            }
            _exHead = target;
            _exHeadOffset = offset;
            HangExternal();
        }
        public void HangHead(Rigidbody target)
        {
            HangHead(target, Vector3.zero);
        }
        /// <summary>
        /// 悬挂绳尾到一个刚体上
        /// </summary>
        public void HangTail(Rigidbody target, Vector3 offset)
        {
            Destroy(_exJoint);
            _exJoint = null;
            if (_exTail != target)
            {
                if (_exTail != null)
                    foreach (var c in _exTail.GetComponentsInChildren<Collider>())
                    {
                        Ignore(c, false);
                    }

                if (target != null)
                    foreach (var c in target.GetComponentsInChildren<Collider>())
                    {
                        Ignore(c, true);
                    }
            }
            _exTail = target;
            _exTailOffset = offset;
            HangExternal();
        }
        public void HangTail(Rigidbody target)
        {
            HangTail(target, Vector3.zero);
        }
        /// <summary>
        /// 获取绳首悬挂的刚体
        /// </summary>
        public Rigidbody GetHeadHang()
        {
            return _exHead;
        }
        /// <summary>
        /// 获取绳尾悬挂的刚体
        /// </summary>
        public Rigidbody GetTailHang()
        {
            return _exTail;
        }
        /// <summary>
        /// 设置绳子与指定刚体是否可碰撞
        /// </summary>
        public new void Ignore(Collider collider, bool ignore)
        {
            base.Ignore(collider, ignore);
            foreach (var c in HeadHandler.GetComponentsInChildren<Collider>())
            {
                Physics.IgnoreCollision(collider, c, ignore);
            }

            foreach (var c in TailHandler.GetComponentsInChildren<Collider>())
            {
                Physics.IgnoreCollision(collider, c, ignore);
            }
        }

        bool _headSyncFromExt = false;
        bool _tailSyncFromExt = false;
        private void HangExternal()
        {
            _headSyncFromExt = _tailSyncFromExt = false;
            Rigidbody from, to;
            Vector3 fromAnchor, toAnchor;
            fromAnchor = toAnchor = Vector3.zero;
            float distance = _rawLength;
            if (_exTail != null)// 如果尾部挂接外部刚体
            {
                from = _exTail;
                fromAnchor = _exTailOffset;
                _tailSyncFromExt = true;
            }
            else
                from = TailHandler.GetComponent<Rigidbody>();

            if (_exHead != null)// 如果首部挂接外部刚体
            {
                to = _exHead;
                toAnchor = _exHeadOffset;
                _headSyncFromExt = true;
            }
            else
                to = HeadHandler.GetComponent<Rigidbody>();

            if (!_tailSyncFromExt && !_headSyncFromExt)// 如果两端没有挂接外部刚体
            {
                _epJoint.xMotion = _epJoint.yMotion = _epJoint.zMotion = ConfigurableJointMotion.Limited;
                return;
            }
            else
            {
                _epJoint.xMotion = _epJoint.yMotion = _epJoint.zMotion = ConfigurableJointMotion.Free;
                if (_tailSyncFromExt)
                    ManualTail(true, false);

                if (_headSyncFromExt)
                    ManualHead(true, false);
            }

            JointRig(from, fromAnchor, to, toAnchor, distance, ref _exJoint);
            if (_headSyncFromExt && _tailSyncFromExt)// 如果两端都挂接了外部刚体，要让外部两个刚体可以碰撞
            {
                _exJoint.enableCollision = true;
            }
        }

        private void JointRig(Rigidbody from, Rigidbody to, float distance, ref ConfigurableJoint joint)
        {
            JointRig(from, Vector3.zero, to, Vector3.zero, distance, ref joint);
        }
        private void JointRig(Rigidbody from, Vector3 fromAnchor, Rigidbody to, Vector3 toAnchor, float distance, ref ConfigurableJoint joint)
        {
            if (joint == null)
                joint = from.gameObject.AddComponent<ConfigurableJoint>();
            else
                Debug.Assert(joint.gameObject == from.gameObject);

            joint.autoConfigureConnectedAnchor = false;
            joint.connectedBody = to;
            joint.anchor = fromAnchor;
            joint.connectedAnchor = toAnchor;
            joint.xMotion = ConfigurableJointMotion.Limited;
            joint.yMotion = ConfigurableJointMotion.Limited;
            joint.zMotion = ConfigurableJointMotion.Limited;
            joint.linearLimit = new SoftJointLimit()
            {
                limit = distance,
                bounciness = 0,
                contactDistance = 0
            };
            joint.linearLimitSpring = new SoftJointLimitSpring()
            {
                spring = 0,
                damper = 0
            };
            joint.projectionMode = JointProjectionMode.PositionAndRotation;
            joint.projectionDistance = 0;
            joint.projectionAngle = 0;
            joint.enablePreprocessing = false;
        }
        /// <summary>
        /// 创建并链接首尾控制柄
        /// </summary>
        private void CreateEndPoints()
        {
            GameObject headHandler;
            GameObject tailHandler;
            if (!_useExtHeadHandler)
            {
                headHandler = GameObject.CreatePrimitive(PrimitiveType.Cube);
                headHandler.name = "HeadHandler";
                headHandler.transform.SetParent(_ropeRoot.transform);
                headHandler.transform.position = _head.transform.position;
                headHandler.transform.localScale = Vector3.one * _capR;
                Destroy(headHandler.GetComponent<Renderer>());
                var a = headHandler.AddComponent<Rigidbody>();
                var halfMass = Mass / 2f;
                a.mass = halfMass;
                a.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
            else
                headHandler = HeadHandler.gameObject;

            base.SetHeadHandler(headHandler.transform, true);

            if (!_useExtTailHandler)
            {
                tailHandler = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tailHandler.name = "TailHandler";
                tailHandler.transform.SetParent(_ropeRoot.transform);
                tailHandler.transform.position = _tail.transform.position;
                tailHandler.transform.localScale = Vector3.one * _capR;
                Destroy(tailHandler.GetComponent<Renderer>());
                var b = tailHandler.AddComponent<Rigidbody>();
                var halfMass = Mass / 2f;
                b.mass = halfMass;
                b.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
            else
                tailHandler = TailHandler.gameObject;

            base.SetTailHandler(tailHandler.transform, true);

            JointRig(tailHandler.GetComponent<Rigidbody>(), headHandler.GetComponent<Rigidbody>(), _rawLength, ref _epJoint);
        }

        protected override void FixedUpdate()
        {
            if (_headSyncFromExt)
            {
                if (_exHead != null)
                    HeadHandler.position = _exHead.transform.TransformPoint(_exHeadOffset);
                else
                    HangHead(null);
            }

            if (_tailSyncFromExt)
            {
                if (_exTail != null)
                    TailHandler.position = _exTail.transform.TransformPoint(_exTailOffset);
                else
                    HangTail(null);
            }

            base.FixedUpdate();
        }

        protected override void OnDestroy()
        {
            if (_epJoint != null)
            {
                Destroy(_epJoint);
                _epJoint = null;
            }
            base.OnDestroy();
        }
    }
}