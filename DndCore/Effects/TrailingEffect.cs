﻿using System;
using System.Linq;

namespace DndCore
{
	public class TrailingEffect
	{
		public TrailingEffect()
		{
			Opacity = 1;
			Scale = 1;
			MaxScale = double.MaxValue;
			ScaleVariance = 0;
			Brightness = 100;
			Saturation = 100;
		}

		public int LeftRightDistanceBetweenPrints { get; set; }
		public int MinForwardDistanceBetweenPrints { get; set; }

		//public int MedianSoundInterval { get; set; }

		/// <summary>
		/// This can be a semicolon-separated list of sound files with optional fields for MedianSoundInterval and 
		/// PlusMinusSoundInterval (comma-separated, and placed inside parens after the sound file name). 
		/// 
		/// For example: 
		/// 	Flap[6] (600,300); Crow[3] (500,300)
		/// </summary>
		public string OnPrintPlaySound { get; set; }
		//public double PlusMinusSoundInterval { get; set; }

		
		public string Name { get; set; }
		public string EffectType { get; set; }
		public int StartIndex { get; set; }
		public int FadeIn { get; set; }
		public int Lifespan { get; set; }
		public int FadeOut { get; set; }
		public double Opacity { get; set; }
		public double Scale { get; set; }
		public double ScaleVariance { get; set; }
		public string HueShift { get; set; }
		public int HueShiftRandom { get; set; }
		public int Saturation { get; set; }
		public int Brightness { get; set; }
		public int RotationOffset { get; set; }
		public int RotationOffsetRandom { get; set; }
		public bool FlipHalfTime { get; set; }
		public bool ScaleWithVelocity { get; set; }
		public double MinScale { get; set; }
		public double MaxScale { get; set; }

		public static TrailingEffect From(TrailingEffectsDto dto)
		{
			TrailingEffect trailingEffect = new TrailingEffect();
			trailingEffect.Brightness = MathUtils.GetInt(dto.Brightness, 100);
			trailingEffect.FlipHalfTime = MathUtils.IsChecked(dto.FlipHalfTime);
			trailingEffect.EffectType = dto.EffectType;
			trailingEffect.FadeIn = MathUtils.GetInt(dto.FadeIn);
			trailingEffect.FadeOut = MathUtils.GetInt(dto.FadeOut);
			trailingEffect.HueShift = dto.HueShift;
			trailingEffect.HueShiftRandom = MathUtils.GetInt(dto.HueShiftRandom);
			trailingEffect.LeftRightDistanceBetweenPrints = MathUtils.GetInt(dto.LeftRightDistanceBetweenPrints);
			trailingEffect.Lifespan = MathUtils.GetInt(dto.Lifespan);
			//trailingEffect.MedianSoundInterval = MathUtils.GetInt(dto.MedianSoundInterval);
			trailingEffect.MinForwardDistanceBetweenPrints = MathUtils.GetInt(dto.MinForwardDistanceBetweenPrints);
			trailingEffect.Name = dto.Name;
			trailingEffect.OnPrintPlaySound = dto.OnPrintPlaySound;
			trailingEffect.Opacity = MathUtils.GetDouble(dto.Opacity, 1);
			trailingEffect.Saturation = MathUtils.GetInt(dto.Saturation, 100);
			//trailingEffect.PlusMinusSoundInterval = MathUtils.GetInt(dto.PlusMinusSoundInterval);
			trailingEffect.RotationOffset = MathUtils.GetInt(dto.RotationOffset);
			trailingEffect.RotationOffsetRandom = MathUtils.GetInt(dto.RotationOffsetRandom);
			trailingEffect.StartIndex = MathUtils.GetInt(dto.StartIndex);
			trailingEffect.Scale = MathUtils.GetDouble(dto.Scale, 1);
			trailingEffect.ScaleWithVelocity = MathUtils.IsChecked(dto.ScaleWithVelocity);
			trailingEffect.MinScale = MathUtils.GetDouble(dto.MinScale, 0);
			trailingEffect.MaxScale = MathUtils.GetDouble(dto.MaxScale, double.MaxValue);
			trailingEffect.ScaleVariance = MathUtils.GetDouble(dto.ScaleVariance, 0);

			return trailingEffect;
		}

	}
}
