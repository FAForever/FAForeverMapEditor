using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ozone.UI
{
    public class UiWaves : MonoBehaviour
    {
        [Header("UI")]
        public InputField TexPath;
        public InputField Scale;
        public InputField Speed;
        public InputField Angle;
        
        [Header("Events")]
        public UnityEvent OnEndEdit;
        
        public void InputFieldUpdate()
        {
            OnEndEdit.Invoke();
        }

        public string GetTexPath()
        {
            return TexPath.text;
        }
        
        public void SetTexPath(string path)
        {
            TexPath.text = path;
        }
        
        public float GetScale()
        {
            float scale = LuaParser.Read.StringToFloat(Scale.text);
            if (scale != 0)
                scale = 1 / scale;
            return scale;
        }

        public void SetScale(float scale)
        {
            if (scale != 0)
                scale = 1 / scale;
            Scale.text = scale.ToString();
        }

        public Vector2 GetMovement()
        {
            float angle = LuaParser.Read.StringToFloat(Angle.text) * -1 * Mathf.Deg2Rad;
            float speed = LuaParser.Read.StringToFloat(Speed.text);
            var movement = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
            return movement * speed;
        }

        public void SetMovement(Vector2 movement)
        {
            float angle = Mathf.Atan2(movement.x, movement.y) * Mathf.Rad2Deg;
            // default angle direction is CCW, but we want CW
            angle *= -1;
            Angle.text = angle.ToString();
            Speed.text = movement.magnitude.ToString();
        }
    }
}