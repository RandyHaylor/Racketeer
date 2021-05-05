using System;
using UnityEngine;
using static FloatOscillator;

namespace Shapes.Samples.Lines
{
    public class AnimatedLine : MonoBehaviour
    {
        public Transform startPointTransform;
        public Transform endPointTransform;

        public LineInfo lineInfo;
        
        private LineInfo currentLineInfo;
        [Header("Animation options")] public bool oscillateWidth;
        public FloatOscillator.OscillatorType widthOscillatorType;
        public float widthOscillationSpeed = 1f;
        public float minWidthMultiple = 0f;
        public float maxWidthMultiple = 1f;
        private float widthMultiplierCacheVar;

        private Camera mainCam;

        private void Awake()
        {
            mainCam = Camera.main;
            currentLineInfo = new LineInfo();
        }

        private void Update()
        {
            if (startPointTransform == null || endPointTransform == null) return;

            lineInfo.startPos = startPointTransform.position;
            lineInfo.endPos = endPointTransform.position;

            lineInfo.forward = -mainCam.transform.forward;
            currentLineInfo = lineInfo; //applying line info from inspector or whatever is controlling the line animator script

            if (oscillateWidth) OscillateLineWidth();

            LineSegment.Draw(currentLineInfo);
        }

        private void OscillateLineWidth()
        {
            widthMultiplierCacheVar = ((Oscillate(widthOscillationSpeed, widthOscillatorType) * (maxWidthMultiple - minWidthMultiple)) + minWidthMultiple);
            currentLineInfo.width = lineInfo.width * widthMultiplierCacheVar;
            currentLineInfo.arrowWidth = lineInfo.arrowWidth * widthMultiplierCacheVar;
        }
    }
}