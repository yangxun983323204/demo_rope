using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace YxRope
{
    /// <summary>
    /// Catmull-Rom样条线
    /// </summary>
    public class CatmullRomSpline
    {

        private Vector3 _head, _tail;
        private Vector3[] _pts;
        private int _ptsLen = 0;
        private Transform[] _trans;
        private int _transLen = 0;
        /// <summary>
        /// 0为points数据，1为transform数据
        /// </summary>
        private byte _type = 0;

        public void Set(Vector3[] points)
        {
            _type = 0;
            _head = points[0] + (points[0] - points[1]);
            Array.Copy(points, _pts, points.Length);
            _ptsLen = points.Length;
            _tail = points[points.Length - 2] + (points[points.Length - 2] - points[points.Length - 3]);
        }

        public void Set(Transform[] trans)
        {
            _type = 1;
            _head = trans[0].position + (trans[0].position - trans[1].position);
            Array.Copy(trans, _trans, trans.Length);
            _transLen = trans.Length;
            _tail = trans[trans.Length - 2].position + (trans[trans.Length - 2].position - trans[trans.Length - 3].position);
        }

        public void SetRef(Vector3[] points)
        {
            _type = 0;
            _head = points[0] + (points[0] - points[1]);
            _pts = points;
            _ptsLen = points.Length;
            _tail = points[points.Length - 2] + (points[points.Length - 2] - points[points.Length - 3]);
        }

        public void SetRef(Transform[] trans)
        {
            _type = 1;
            _head = trans[0].position + (trans[0].position - trans[1].position);
            _trans = trans;
            _transLen = trans.Length;
            _tail = trans[trans.Length - 2].position + (trans[trans.Length - 2].position - trans[trans.Length - 3].position);
        }

        public Vector3 Interp(float t)
        {
            int numSections;
            if (_type == 0)
                numSections = _ptsLen + 2 - 3;
            else
                numSections = _transLen + 2 - 3;

            int currPt = Mathf.Min(Mathf.FloorToInt(t * (float)numSections), numSections - 1);
            float u = t * (float)numSections - (float)currPt;
            Vector3 a, b, c, d;
            a = GetPoint(currPt);
            b = GetPoint(currPt + 1);
            c = GetPoint(currPt + 2);
            d = GetPoint(currPt + 3);

            return .5f * ((-a + 3f * b - 3f * c + d) * (u * u * u) + (2f * a - 5f * b + 4f * c - d) * (u * u) + (-a + c) * u + 2f * b);
        }

        private Vector3 GetPoint(int idx)
        {
            if (_type == 0)
            {
                if (idx == 0)
                    return _head;
                else if (idx == _ptsLen + 1)
                    return _tail;
                else
                    return _pts[idx - 1];
            }
            else
            {
                if (idx == 0)
                    return _head;
                else if (idx == _transLen + 1)
                    return _tail;
                else
                    return _trans[idx - 1].position;
            }
        }
    }
}
