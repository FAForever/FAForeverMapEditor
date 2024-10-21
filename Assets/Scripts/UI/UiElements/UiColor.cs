using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ozone.UI
{
	public class UiColor : MonoBehaviour
	{
		public Image ColorPreview;
		public Image AlphaPreview;

		public InputField Red;
		public InputField Green;
		public InputField Blue;
		public InputField Alpha;
		public InputField Multiplier;

		public Slider RedSlider;
		public Slider GreenSlider;
		public Slider BlueSlider;
		public Slider AlphaSlider;

		public UnityEvent OnInputBegin;
		public UnityEvent OnInputFinish;
		public UnityEvent OnValueChanged;

		bool Loading = false;

		Vector4 LastValue = Vector4.one;

		void SetSliderMax()
		{
			float Clamp = 1;
			RedSlider.maxValue = Clamp;
			GreenSlider.maxValue = Clamp;
			BlueSlider.maxValue = Clamp;
			if (AlphaSlider)
				AlphaSlider.maxValue = Clamp;
		}

		void UpdateLastValue()
		{
			LastValue.x = RedSlider.value * LuaParser.Read.StringToInt(Multiplier.text);
			LastValue.y = GreenSlider.value * LuaParser.Read.StringToInt(Multiplier.text);
			LastValue.z = BlueSlider.value * LuaParser.Read.StringToInt(Multiplier.text);
			if (AlphaSlider)
				LastValue.w = AlphaSlider.value;
			else
				LastValue.w = 1;
		}

		public Color GetColorValue()
		{
			return new Color(LastValue.x, LastValue.y, LastValue.z, LastValue.w);
		}

		public Vector3 GetVectorValue()
		{
			return LastValue;
		}

		public Vector4 GetVector4Value()
		{
			return LastValue;
		}

		public void SetColorField(float R, float G, float B, float A = 1)
		{
			SetSliderMax();
			Loading = true;

			int multiplier = Mathf.CeilToInt(Mathf.Max(R, G, B)); 

			RedSlider.value = FormatFloat(R / multiplier);
			GreenSlider.value = FormatFloat(G / multiplier);
			BlueSlider.value = FormatFloat(B / multiplier);

			Red.text = R.ToString();
			Green.text = G.ToString();
			Blue.text = B.ToString();
			Multiplier.text = multiplier.ToString();

			if (AlphaSlider)
			{
				AlphaSlider.value = FormatFloat(A);
				Alpha.text = AlphaSlider.value.ToString();
			}

			UpdateLastValue();
			UpdateGfx();

			Loading = false;
		}


		public void SetColorField(Color BeginColor)
        {
            SetSliderMax();
            Loading = true;

            int multiplier = Mathf.CeilToInt(Mathf.Max(BeginColor.r, BeginColor.g, BeginColor.b));

            RedSlider.value = BeginColor.r / multiplier;
			GreenSlider.value = BeginColor.g / multiplier;
			BlueSlider.value = BeginColor.b / multiplier;

			Red.text = BeginColor.r.ToString();
			Green.text = BeginColor.r.ToString();
			Blue.text = BeginColor.r.ToString();
            Multiplier.text = multiplier.ToString();

            if (AlphaSlider)
			{
				AlphaSlider.value = BeginColor.a;
				Alpha.text = AlphaSlider.value.ToString();
			}

			UpdateLastValue();
			UpdateGfx();

			Loading = false;
		}

		const float FloatSteps = 10000;
		float FormatFloat(float value)
		{
			return Mathf.RoundToInt(value * FloatSteps) / FloatSteps;
		}

		public void InputFieldUpdate()
		{
			if (Loading)
				return;

			Loading = true;
			float R = LuaParser.Read.StringToFloat(Red.text);
			float G = LuaParser.Read.StringToFloat(Green.text);
			float B = LuaParser.Read.StringToFloat(Blue.text);

            int multiplier = Mathf.CeilToInt(Mathf.Max(R, G, B));

            RedSlider.value = FormatFloat(R / multiplier);
            GreenSlider.value = FormatFloat(G / multiplier);
            BlueSlider.value = FormatFloat(B / multiplier);

            Red.text = R.ToString();
            Green.text = G.ToString();
            Blue.text = B.ToString();
            Multiplier.text = multiplier.ToString();

            if (AlphaSlider)
			{
				AlphaSlider.value = FormatFloat(LuaParser.Read.StringToFloat(Alpha.text));
				Alpha.text = AlphaSlider.value.ToString();
			}

			UpdateLastValue();

			Loading = false;

			UpdateGfx();
			Begin = false;
			OnInputFinish.Invoke();
		}


		bool UpdatingSlider = false;
		bool Begin = false;
		public void SliderUpdate(bool Finish)
		{
			if (Loading || UpdatingSlider)
				return;

			if (Finish && !Begin)
				return;

			if (!Begin)
			{
				OnInputBegin.Invoke();
				Begin = true;
			}

			UpdatingSlider = true;
			RedSlider.value = FormatFloat(RedSlider.value);
			GreenSlider.value = FormatFloat(GreenSlider.value);
			BlueSlider.value = FormatFloat(BlueSlider.value);

			Red.text = (RedSlider.value * LuaParser.Read.StringToInt(Multiplier.text)).ToString();
			Green.text = (GreenSlider.value * LuaParser.Read.StringToInt(Multiplier.text)).ToString();
            Blue.text = (BlueSlider.value * LuaParser.Read.StringToInt(Multiplier.text)).ToString();

            if (AlphaSlider)
			{
				AlphaSlider.value = FormatFloat(AlphaSlider.value);
				Alpha.text = AlphaSlider.value.ToString();
			}

			UpdateLastValue();

			UpdatingSlider = false;

			UpdateGfx();
			if (Finish)
			{
				OnInputFinish.Invoke();
				Begin = false;
			}
			else
				OnValueChanged.Invoke();
		}

		void UpdateGfx()
		{
			ColorPreview.color = new Color(RedSlider.value, GreenSlider.value, BlueSlider.value, 1);
			if (AlphaPreview)
			{
				AlphaPreview.color = Color.Lerp(Color.black, Color.white, AlphaSlider.value);
			}
		}
	}
}