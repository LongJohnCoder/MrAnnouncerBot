﻿//#define profiling
using System;
using System.Linq;
using System.Windows;
using System.Collections.Generic;
using LeapTools;
using Newtonsoft.Json;

namespace DHDM
{
	public class LeapDevice
	{
		LeapMotion leapMotion;
		bool active;
		public bool Active
		{
			get
			{
				return active;
			}
			set
			{
				if (active == value)
					return;
				active = value;
				if (active)
				{
					leapMotion = new LeapMotion();
					leapMotion.HandsMoved += LeapMotion_HandsMoved;
				}
				else
				{
					leapMotion.HandsMoved -= LeapMotion_HandsMoved;
					leapMotion.DisposeController();
					leapMotion = null;
				}
			}
		}

		SkeletalData2d skeletalData2d = new SkeletalData2d();
		bool ShowingDiagnostics()
		{
			return skeletalData2d.ShowingDiagnostics();
		}

		public PalmDirection GetPalmDirection()
		{
			if (skeletalData2d.Hands.Count > 0)
				return skeletalData2d.Hands[0].PalmDirection;

			return PalmDirection.None;
		}

		void ClearImpulseData()
		{
			skeletalData2d.HandEffect = null;
		}

		private void LeapMotion_HandsMoved(object sender, LeapFrameEventArgs ea)
		{
			if (!LeapCalibrator.Calibrated && !ShowingDiagnostics())
				return;

			if (LeapCalibrator.Calibrated)
				skeletalData2d.SetFromFrame(ea.Frame);
			else
				skeletalData2d.SetFromFrame(null);
			HubtasticBaseStation.UpdateSkeletalData(JsonConvert.SerializeObject(skeletalData2d));
			ClearImpulseData();
		}

		public LeapDevice()
		{

		}

		public bool ShowingLiveHandPosition
		{
			get
			{
				return skeletalData2d.ShowLiveHandPosition;
			}
			set
			{
				skeletalData2d.ShowLiveHandPosition = value;
			}
		}
		

		public void SetDiagnosticsOptions(bool showBackPlane, bool showFrontPlane, bool showActivePlane)
		{
			skeletalData2d.ShowActivePlane = showActivePlane;
			skeletalData2d.ShowFrontPlane = showFrontPlane;
			skeletalData2d.ShowBackPlane = showBackPlane;
		}

		public void CalibrationPointUpdated(LeapMotionCalibrationStep leapMotionCalibrationStep, Point position, DndCore.Vector point3D, double scale)
		{
			switch (leapMotionCalibrationStep)
			{
				case LeapMotionCalibrationStep.BackUpperLeft:
					skeletalData2d.BackPlane.UpperLeft2D = Point2D.From(position);
					skeletalData2d.BackPlane.UpperLeft = ScaledPoint.From(point3D, scale);
					break;
				case LeapMotionCalibrationStep.BackLowerRight:
					skeletalData2d.BackPlane.LowerRight2D = Point2D.From(position);
					skeletalData2d.BackPlane.LowerRight = ScaledPoint.From(point3D, scale);
					break;
				case LeapMotionCalibrationStep.FrontUpperLeft:
					skeletalData2d.FrontPlane.UpperLeft2D = Point2D.From(position);
					skeletalData2d.FrontPlane.UpperLeft = ScaledPoint.From(point3D, scale);
					break;
				case LeapMotionCalibrationStep.FrontLowerRight:
					skeletalData2d.FrontPlane.LowerRight2D = Point2D.From(position);
					skeletalData2d.FrontPlane.LowerRight = ScaledPoint.From(point3D, scale);
					break;
			}
		}
		public void TriggerHandFx(HandFxDto handFxDto)
		{
			skeletalData2d.HandEffect = handFxDto;
		}
	}
}