﻿// COPYRIGHT 2009, 2010, 2011, 2012, 2013 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

/* DIESEL LOCOMOTIVE CLASSES
 * 
 * The Locomotive is represented by two classes:
 *  MSTSDieselLocomotiveSimulator - defines the behaviour, ie physics, motion, power generated etc
 *  MSTSDieselLocomotiveViewer - defines the appearance in a 3D viewer.  The viewer doesn't
 *  get attached to the car until it comes into viewing range.
 *  
 * Both these classes derive from corresponding classes for a basic locomotive
 *  LocomotiveSimulator - provides for movement, basic controls etc
 *  LocomotiveViewer - provides basic animation for running gear, wipers, etc
 * 
 */

//#define ALLOW_ORTS_SPECIFIC_ENG_PARAMETERS

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using MSTS;
// needed for Debug

namespace ORTS
{
    ///////////////////////////////////////////////////
    ///   SIMULATION BEHAVIOUR
    ///////////////////////////////////////////////////

    /// <summary>
    /// Adds physics and control for a diesel locomotive
    /// </summary>
    public class MSTSDieselLocomotive : MSTSLocomotive
    {
        public float IdleRPM;
        public float MaxRPM;
        public float MaxRPMChangeRate;
        public float PercentChangePerSec = .2f;
        public float IdleExhaust;
        public float InitialExhaust;
        public float ExhaustMagnitude = 4.0f;
        public float MaxExhaust = 50.0f;
        public float ExhaustDynamics = 4.0f;
        public float EngineRPMderivation;
        float EngineRPMold;
        float EngineRPMRatio; // used to compute Variable1 and Variable2

        public float MaxDieselLevelL = 5000.0f;
        public float DieselUsedPerHourAtMaxPowerL = 1.0f;
        public float DieselUsedPerHourAtIdleL = 1.0f;
        public float DieselLevelL = 5000.0f;
        public float DieselFlowLps;
        float DieselWeightKgpL = 0.8f; //per liter
        float InitialMassKg = 100000.0f;

        public float EngineRPM;
        public float ExhaustParticles = 10.0f;
        public Color ExhaustColor = Color.Gray;
        Color ExhaustSteadyColor = Color.Gray;
        Color ExhaustTransientColor = Color.Black;

        public DieselEngines DieselEngines = new DieselEngines();

        public GearBox GearBox = new GearBox();

        public MSTSDieselLocomotive(Simulator simulator, string wagFile)
            : base(simulator, wagFile)
        {
            PowerOn = true;
            InitialMassKg = MassKG;
        }

        /// <summary>
        /// Parse the wag file parameters required for the simulator and viewer classes
        /// </summary>
        public override void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "engine(dieselengineidlerpm": IdleRPM = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(dieselenginemaxrpm": MaxRPM = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(dieselenginemaxrpmchangerate": MaxRPMChangeRate = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;

                case "engine(effects(dieselspecialeffects": ParseEffects(lowercasetoken, stf); break;
                case "engine(dieselsmokeeffectinitialsmokerate": IdleExhaust = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(dieselsmokeeffectinitialmagnitude": InitialExhaust = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(dieselsmokeeffectmaxsmokerate": MaxExhaust = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(dieselsmokeeffectmaxmagnitude": ExhaustMagnitude = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(ortsdiesel(exhaustcolor": ExhaustSteadyColor.PackedValue = stf.ReadHexBlock(Color.Gray.PackedValue); break;
                case "engine(ortsdiesel(exhausttransientcolor": ExhaustTransientColor.PackedValue = stf.ReadHexBlock(Color.Black.PackedValue); break;
                case "engine(ortsdieselengines": DieselEngines = new DieselEngines(this, stf); break;
                case "engine(maxdiesellevel": MaxDieselLevelL = stf.ReadFloatBlock(STFReader.UNITS.Volume, null); break;
                case "engine(dieselusedperhouratmaxpower": DieselUsedPerHourAtMaxPowerL = stf.ReadFloatBlock(STFReader.UNITS.Volume, null); break;
                case "engine(dieselusedperhouratidle": DieselUsedPerHourAtIdleL = stf.ReadFloatBlock(STFReader.UNITS.Volume, null); break;				
                default:
                    GearBox.Parse(lowercasetoken, stf);
                    base.Parse(lowercasetoken, stf); break;
            }

            if (IdleRPM != 0 && MaxRPM != 0 && MaxRPMChangeRate != 0)
            {
                PercentChangePerSec = MaxRPMChangeRate / (MaxRPM - IdleRPM);
                EngineRPM = IdleRPM;
            }

            if (MaxDieselLevelL != DieselLevelL)
                DieselLevelL = MaxDieselLevelL;
        }

        public override void Initialize()
        {
            if (DieselEngines.Count == 0)
            {
                DieselEngines.Add(new DieselEngine());
                DieselEngines[0].InitFromMSTS(this);
            }

            if ((GearBox != null) && (GearBoxController == null))
            {
                if (!GearBox.IsInitialized)
                    GearBox = null;
                else
                {
                    foreach (DieselEngine de in DieselEngines)
                    {
                        if (de.GearBox == null)
                            de.GearBox = new GearBox(GearBox, de);
                        //if (this.Train.TrainType == Train.TRAINTYPE.AI)
                        //    de.GearBox.GearBoxOperation = GearBoxOperation.Automatic;
                    }
                    GearBoxController = new MSTSNotchController(DieselEngines[0].GearBox.NumOfGears + 1);
                }
            }

            DieselEngines.Initialize(true);

            base.Initialize();
        }

        /// <summary>
        /// This initializer is called when we are making a new copy of a car already
        /// loaded in memory.  We use this one to speed up loading by eliminating the
        /// need to parse the wag file multiple times.
        /// NOTE:  you must initialize all the same variables as you parsed above
        /// </summary>
        public override void InitializeFromCopy(MSTSWagon copy)
        {
            MSTSDieselLocomotive locoCopy = (MSTSDieselLocomotive)copy;
            IdleRPM = locoCopy.IdleRPM;
            MaxRPM = locoCopy.MaxRPM;
            MaxRPMChangeRate = locoCopy.MaxRPMChangeRate;
            PercentChangePerSec = locoCopy.PercentChangePerSec;
            IdleExhaust = locoCopy.IdleExhaust;
            MaxExhaust = locoCopy.MaxExhaust;
            ExhaustDynamics = locoCopy.ExhaustDynamics;
            EngineRPMderivation = locoCopy.EngineRPMderivation;
            EngineRPMold = locoCopy.EngineRPMold;

            MaxDieselLevelL = locoCopy.MaxDieselLevelL;
            DieselUsedPerHourAtMaxPowerL = locoCopy.DieselUsedPerHourAtMaxPowerL;
            DieselUsedPerHourAtIdleL = locoCopy.DieselUsedPerHourAtIdleL;
            if (this.CarID.StartsWith("0"))
                DieselLevelL = locoCopy.DieselLevelL;
            else
                DieselLevelL = locoCopy.MaxDieselLevelL;
            DieselFlowLps = 0.0f;
            InitialMassKg = MassKG;

            EngineRPM = locoCopy.EngineRPM;
            ExhaustParticles = locoCopy.ExhaustParticles;
            ExhaustSteadyColor = locoCopy.ExhaustSteadyColor;
            ExhaustTransientColor = locoCopy.ExhaustTransientColor;
            DieselEngines = new DieselEngines(locoCopy.DieselEngines, this);
            if (locoCopy.GearBoxController != null)
                GearBoxController = new MSTSNotchController(locoCopy.GearBoxController);
            Initialize();
            base.InitializeFromCopy(copy);  // each derived level initializes its own variables
        }

        /// <summary>
        /// We are saving the game.  Save anything that we'll need to restore the 
        /// status later.
        /// </summary>
        public override void Save(BinaryWriter outf)
        {
            // for example
            // outf.Write(Pan);
            base.Save(outf);
            outf.Write(DieselLevelL);
            ControllerFactory.Save(GearBoxController, outf);
            DieselEngines.Save(outf);
        }

        /// <summary>
        /// We are restoring a saved game.  The TrainCar class has already
        /// been initialized.   Restore the game state.
        /// </summary>
        public override void Restore(BinaryReader inf)
        {
            base.Restore(inf);
            DieselLevelL = inf.ReadSingle();
            GearBoxController = (MSTSNotchController)ControllerFactory.Restore(Simulator, inf);
            if (DieselEngines.Count == 0)
            {
                DieselEngines = new DieselEngines(this);
                DieselEngines.Add(new DieselEngine());
                DieselEngines[0].InitFromMSTS(this);
            }
            DieselEngines.Restore(inf);
        }

        /// <summary>
        /// Create a viewer for this locomotive.   Viewers are only attached
        /// while the locomotive is in viewing range.
        /// </summary>
        public override TrainCarViewer GetViewer(Viewer3D viewer)
        {
            return new MSTSDieselLocomotiveViewer(viewer, this);
        }

        /// <summary>
        /// This is a periodic update to calculate physics 
        /// parameters and update the base class's MotiveForceN 
        /// and FrictionForceN values based on throttle settings
        /// etc for the locomotive.
        /// </summary>
        public override void Update(float elapsedClockSeconds)
        {
            if (this.Train.TrainType == Train.TRAINTYPE.AI)
            {
                foreach (DieselEngine de in DieselEngines)
                {
                    if (de.EngineStatus != DieselEngine.Status.Running)
                        de.Initialize(true);
                    if(de.GearBox != null)
                        de.GearBox.GearBoxOperation = GearBoxOperation.Automatic;
                }
            }

            TrainBrakeController.Update(elapsedClockSeconds);
            if( TrainBrakeController.UpdateValue > 0.0 ) {
                Simulator.Confirmer.Update( CabControl.TrainBrake, CabSetting.Increase, GetTrainBrakeStatus() );
            }
            if( TrainBrakeController.UpdateValue < 0.0 ) {
                Simulator.Confirmer.Update( CabControl.TrainBrake, CabSetting.Decrease, GetTrainBrakeStatus() );
            }

            if( EngineBrakeController != null ) {
                EngineBrakeController.Update( elapsedClockSeconds );
                if( EngineBrakeController.UpdateValue > 0.0 ) {
                    Simulator.Confirmer.Update( CabControl.EngineBrake, CabSetting.Increase, GetEngineBrakeStatus() );
                }
                if( EngineBrakeController.UpdateValue < 0.0 ) {
                    Simulator.Confirmer.Update( CabControl.EngineBrake, CabSetting.Decrease, GetEngineBrakeStatus() );
                }
            }

            if ((DynamicBrakeController != null) && (DynamicBrakePercent >= 0))
            {
                if (!DynamicBrake)
                {
                    if (DynamicBrakeController.CommandStartTime + DynamicBrakeDelayS < Simulator.ClockTime)
                    {
                        DynamicBrake = true; // Engage
                        if (IsLeadLocomotive())
                            Simulator.Confirmer.ConfirmWithPerCent(CabControl.DynamicBrake, DynamicBrakeController.CurrentValue * 100);
                    }
                    else if (IsLeadLocomotive())
                        Simulator.Confirmer.Confirm(CabControl.DynamicBrake, CabSetting.On); // Keeping status string on screen so user knows what's happening
                }
                else if (this.IsLeadLocomotive())
                    DynamicBrakePercent = DynamicBrakeController.Update(elapsedClockSeconds) * 100.0f;
                else
                    DynamicBrakeController.Update(elapsedClockSeconds);
            }
            else if ((DynamicBrakeController != null) && (DynamicBrakePercent < 0) && (DynamicBrake))
            {
                if (DynamicBrakeController.CommandStartTime + DynamicBrakeDelayS < Simulator.ClockTime)
                {
                    DynamicBrake = false; // Disengage
                    if (IsLeadLocomotive())
                        Simulator.Confirmer.Confirm(CabControl.DynamicBrake, CabSetting.Off);
                }
                else if (IsLeadLocomotive())
                    Simulator.Confirmer.Confirm(CabControl.DynamicBrake, CabSetting.On); // Keeping status string on screen so user knows what's happening
            }

            


            //Currently the ThrottlePercent is global to the entire train
            //So only the lead locomotive updates it, the others only updates the controller (actually useless)
            if (this.IsLeadLocomotive() || (!AcceptMUSignals))
            {
                ThrottlePercent = ThrottleController.Update(elapsedClockSeconds) * 100.0f;

                if (GearBoxController != null)
                {
                    GearboxGearIndex = (int)GearBoxController.Update(elapsedClockSeconds);
                }
            }
            else
            {
                ThrottleController.Update(elapsedClockSeconds);
                if (GearBoxController != null)
                {
                    GearBoxController.Update(elapsedClockSeconds);
                }
            }
            LocalThrottlePercent = ThrottlePercent;

#if INDIVIDUAL_CONTROL
			//this train is remote controlled, with mine as a helper, so I need to send the controlling information, but not the force.
			if (MultiPlayer.MPManager.IsMultiPlayer() && this.Train.TrainType == Train.TRAINTYPE.REMOTE && this == Program.Simulator.PlayerLocomotive)
			{
				//cannot control train brake as it is the remote's job to do so
				if ((EngineBrakeController != null && EngineBrakeController.UpdateValue != 0.0) || (DynamicBrakeController != null && DynamicBrakeController.UpdateValue != 0.0) || ThrottleController.UpdateValue != 0.0)
				{
					controlUpdated = true;
				}
				ThrottlePercent = ThrottleController.Update(elapsedClockSeconds) * 100.0f;
				return; //done, will go back and send the message to the remote train controller
			}

			if (MultiPlayer.MPManager.IsMultiPlayer() && this.notificationReceived == true)
			{
				ThrottlePercent = ThrottleController.CurrentValue * 100.0f;
				this.notificationReceived = false;
			}
#endif
			
			// TODO  this is a wild simplification for diesel electric
            //float e = (EngineRPM - IdleRPM) / (MaxRPM - IdleRPM); //
            float t = ThrottlePercent / 100f;
            float currentSpeedMpS = Math.Abs(SpeedMpS);
            float currentWheelSpeedMpS = Math.Abs(WheelSpeedMpS);

            if (!this.Simulator.UseAdvancedAdhesion)
                currentWheelSpeedMpS = currentSpeedMpS;

            foreach (DieselEngine de in DieselEngines)
            {
                if (de.EngineStatus == DieselEngine.Status.Running)
                    de.DemandedThrottlePercent = ThrottlePercent;
                else
                    de.DemandedThrottlePercent = 0f;

                if (Direction == Direction.Reverse)
                    PrevMotiveForceN *= -1f;

                if ((de.RealRPM > de.StartingRPM)&&(ThrottlePercent>0))
                    de.OutputPowerW = PrevMotiveForceN > 0 ? PrevMotiveForceN * currentSpeedMpS : 0;
                else
                    de.OutputPowerW = 0.0f;
                de.Update(elapsedClockSeconds);

                if (de.GearBox != null)
                {
                    if ((this.IsLeadLocomotive()))
                    {
                        if (de.GearBox.GearBoxOperation == GearBoxOperation.Manual)
                        {
                            if (GearBoxController.CurrentNotch > 0)
                                de.GearBox.NextGear = de.GearBox.Gears[GearBoxController.CurrentNotch - 1];
                            else
                                de.GearBox.NextGear = null;
                        }
                    }
                    else
                    {
                        if (de.GearBox.GearBoxOperation == GearBoxOperation.Manual)
                        {
                            if (GearboxGearIndex > 0)
                                de.GearBox.NextGear = de.GearBox.Gears[GearboxGearIndex - 1];
                            else
                                de.GearBox.NextGear = null;
                        }
                    }
                    if (de.GearBox.CurrentGear == null)
                        de.OutputPowerW = 0f;

                    de.GearBox.Update(elapsedClockSeconds);
                }
            }

            //Initial smoke, when locomotive is started:

            ExhaustColor = ExhaustSteadyColor;
            
            //if (EngineRPM == IdleRPM)
            //{
            //    ExhaustParticles = IdleExhaust;
            //    ExhaustDynamics = InitialExhaust;
            //    ExhaustColor = ExhaustSteadyColor;
            //}
            //else if (EngineRPMderivation > 0.0f)
            //{
            //    ExhaustParticles = IdleExhaust + (e * MaxExhaust);
            //    ExhaustDynamics = InitialExhaust + (e * ExhaustMagnitude);
            //    ExhaustColor = ExhaustTransientColor;               
            //}
            //else if (EngineRPMderivation < 0.0f)
            //{
            //        ExhaustParticles = IdleExhaust + (e * MaxExhaust);
            //        ExhaustDynamics = InitialExhaust + (e * ExhaustMagnitude);
            //    if (t == 0f)
            //    {
            //        ExhaustColor = ExhaustDecelColor;
            //    }
            //    else
            //    {
            //        ExhaustColor = ExhaustSteadyColor;
            //    }
            //}
            ExhaustParticles = DieselEngines[0].ExhaustParticles;
            ExhaustDynamics = DieselEngines[0].ExhaustDynamics;
            ExhaustColor = DieselEngines[0].ExhaustColor;

            if (PowerOn = DieselEngines.PowerOn)
            {
                if (TractiveForceCurves == null)
                {
                    float maxForceN = Math.Min(t * MaxForceN, currentWheelSpeedMpS == 0.0f ? ( t * MaxForceN ) : ( t * DieselEngines.MaxOutputPowerW / currentWheelSpeedMpS));
                    //float maxForceN = MaxForceN * t;
                    float maxPowerW = 0.98f * DieselEngines.MaxOutputPowerW;      //0.98 added to let the diesel engine handle the adhesion-caused jittering

                    if (DieselEngines.HasGearBox)
                    {
                        MotiveForceN = DieselEngines.MotiveForceN;
                    }
                    else
                    {
                        
                        if (maxForceN * currentWheelSpeedMpS > maxPowerW)
                            maxForceN = maxPowerW / currentWheelSpeedMpS;

                        //if (currentSpeedMpS > MaxSpeedMpS)
                        //    maxForceN = 0;
                        if (currentSpeedMpS > MaxSpeedMpS - 0.05f)
                            maxForceN = 20 * (MaxSpeedMpS - currentSpeedMpS) * maxForceN;
                        if (currentSpeedMpS > (MaxSpeedMpS))
                            maxForceN = 0;
                        MotiveForceN = maxForceN;
                    }
                }
                else
                {
                    if (t > (DieselEngines.MaxOutputPowerW / DieselEngines.MaxPowerW))
                        t = (DieselEngines.MaxOutputPowerW / DieselEngines.MaxPowerW);
                    MotiveForceN = TractiveForceCurves.Get(t, currentWheelSpeedMpS);
                    if (MotiveForceN < 0)
                        MotiveForceN = 0;
                }
                //if (t == 0)
                //    DieselFlowLps = DieselUsedPerHourAtIdleL / 3600.0f;
                //else
                //    DieselFlowLps = ((DieselUsedPerHourAtMaxPowerL - DieselUsedPerHourAtIdleL) * t + DieselUsedPerHourAtIdleL) / 3600.0f;
                DieselFlowLps = DieselEngines.DieselFlowLps;
                DieselLevelL -= DieselEngines.DieselFlowLps * elapsedClockSeconds;
                if (DieselLevelL <= 0.0f)
                {
                    PowerOn = false;
                    SignalEvent(Event.EnginePowerOff);
                }
                MassKG = InitialMassKg - MaxDieselLevelL * DieselWeightKgpL + DieselLevelL * DieselWeightKgpL;
            }

            if (DynamicBrakePercent > 0 && DynamicBrakeForceCurves != null)
            {
                float f = DynamicBrakeForceCurves.Get(.01f * DynamicBrakePercent, currentWheelSpeedMpS);
                if (f > 0)
                {
                    MotiveForceN -= (SpeedMpS > 0 ? 1 : -1) * f;
                    switch (Direction)
                    {
                        case Direction.Forward:
                            //MotiveForceN *= 1;     //Not necessary
                            break;
                        case Direction.Reverse:
                            MotiveForceN *= -1;
                            break;
                        case Direction.N:
                        default:
                            MotiveForceN *= 0;
                            break;
                    }
                }
                //if (Flipped)
                //    MotiveForceN *= -1f;
            }

            if (MaxForceN > 0 && MaxContinuousForceN > 0)
            {
                MotiveForceN *= 1 - (MaxForceN - MaxContinuousForceN) / (MaxForceN * MaxContinuousForceN) * AverageForceN;
                float w = (ContinuousForceTimeFactor - elapsedClockSeconds) / ContinuousForceTimeFactor;
                if (w < 0)
                    w = 0;
                AverageForceN = w * AverageForceN + (1 - w) * MotiveForceN;
            }

#if !NEW_SIGNALLING
            if (this.IsLeadLocomotive())
            {
                switch (Direction)
                {
                    case Direction.Forward:
                        //MotiveForceN *= 1;     //Not necessary
                        break;
                    case Direction.Reverse:
                        MotiveForceN *= -1;
                        break;
                    case Direction.N:
                    default:
                        MotiveForceN *= 0;
                        break;
                }
                ConfirmWheelslip();
            }
            else
            {
                int carCount = 0;
                int controlEngine = -1;

                // When not LeadLocomotive; check if lead is in Neutral
                // if so this loco will have no motive force
				var LeadLocomotive = Simulator.PlayerLocomotive.Train;

                foreach (TrainCar car in LeadLocomotive.Cars)
                {
                    if (car.IsDriveable)
                        if (controlEngine == -1)
                        {
                            controlEngine = carCount;
                            if (car.Direction == Direction.N)
                                MotiveForceN *= 0;
                            else
                            {
                                switch (Direction)
                                {
                                    case Direction.Forward:
                                        MotiveForceN *= 1;     //Not necessary
                                        break;
                                    case Direction.Reverse:
                                        MotiveForceN *= -1;
                                        break;
                                    case Direction.N:
                                    default:
                                        MotiveForceN *= 0;
                                        break;
                                }
                            }
                        }
                    break;
                } // foreach
            } // end when not lead loco
#else

            if (Train.TrainType == Train.TRAINTYPE.PLAYER)
            {
                if (this.IsLeadLocomotive())
                {
                    switch (Direction)
                    {
                        case Direction.Forward:
                            //MotiveForceN *= 1;     //Not necessary
                            break;
                        case Direction.Reverse:
                            MotiveForceN *= -1;
                            break;
                        case Direction.N:
                        default:
                            MotiveForceN *= 0;
                            break;
                    }
                    ConfirmWheelslip();
                }
                else
                {
                    // When not LeadLocomotive; check if lead is in Neutral
                    // if so this loco will have no motive force

                    var LeadLocomotive = Simulator.PlayerLocomotive;

                    if (LeadLocomotive == null) { }
                    else if (LeadLocomotive.Direction == Direction.N)
                        MotiveForceN *= 0;
                    else
                    {
                        switch (Direction)
                        {
                            case Direction.Forward:
                                MotiveForceN *= 1;     //Not necessary
                                break;
                            case Direction.Reverse:
                                MotiveForceN *= -1;
                                break;
                            case Direction.N:
                            default:
                                MotiveForceN *= 0;
                                break;
                        }
                    }
                } // end when not lead loco
            }// end player locomotive

            else // for AI locomotives
            {
                foreach (DieselEngine de in DieselEngines)
                    de.Start();
                switch (Direction)
                {
                    case Direction.Reverse:
                        MotiveForceN *= -1;
                        break;
                    default:
                        break;
                }
            }// end AI locomotive
#endif

            switch (this.Train.TrainType)
            {
                case Train.TRAINTYPE.AI:
                    if (!PowerOn)
                        PowerOn = true;
                    //LimitMotiveForce(elapsedClockSeconds);    //calls the advanced physics
                    LimitMotiveForce();                         //let's call the basic physics instead for now
                    WheelSpeedMpS = Flipped ? -currentSpeedMpS : currentSpeedMpS;            //make the wheels go round
                    break;
                case Train.TRAINTYPE.STATIC:
                    break;
                case Train.TRAINTYPE.PLAYER:
                case Train.TRAINTYPE.REMOTE:
                    // For notched throttle controls (e.g. Dash 9 found on Marias Pass) UpdateValue is always 0.0
                    if (ThrottleController.UpdateValue != 0.0)
                    {
                        Simulator.Confirmer.UpdateWithPerCent(
                            CabControl.Throttle,
                            ThrottleController.UpdateValue > 0 ? CabSetting.Increase : CabSetting.Decrease,
                            ThrottleController.CurrentValue * 100);
                    }
                    if (DynamicBrakeController != null && DynamicBrakeController.UpdateValue != 0.0)
                    {
                        Simulator.Confirmer.UpdateWithPerCent(
                            CabControl.DynamicBrake,
                            DynamicBrakeController.UpdateValue > 0 ? CabSetting.Increase : CabSetting.Decrease,
                            DynamicBrakeController.CurrentValue * 100);
                    }

                    //Force is filtered due to inductance
                    FilteredMotiveForceN = CurrentFilter.Filter(MotiveForceN, elapsedClockSeconds);

                    MotiveForceN = FilteredMotiveForceN;

                    LimitMotiveForce(elapsedClockSeconds);

                    if (WheelslipCausesThrottleDown && WheelSlip)
                        ThrottleController.SetValue(0.0f);
                    break;
                default:
                    break;

            }

            EngineRPMRatio = (DieselEngines[0].RealRPM - DieselEngines[0].IdleRPM) / (DieselEngines[0].MaxRPM - DieselEngines[0].IdleRPM);

            if (GearBox == null) Variable1 = EngineRPMRatio; // Not gearbased, Variable1 similar to Variable2
            else Variable1 = ThrottlePercent / 100.0f; // Gearbased, Variable1 proportional to ThrottlePercent
            // else Variable1 = MotiveForceN / MaxForceN; // Gearbased, Variable1 proportional to motive force
            // allows for motor volume proportional to effort.

            // Refined Variable2 setting to graduate
            if (Variable2 != EngineRPMRatio)
            {
                // We must avoid Variable2 to run outside of [0, 1] range, even temporarily (because of multithreading)
                Variable2 = EngineRPMRatio < Variable2 ?
                    Math.Max(Math.Max(Variable2 - elapsedClockSeconds * PercentChangePerSec, EngineRPMRatio), 0) :
                    Math.Min(Math.Min(Variable2 + elapsedClockSeconds * PercentChangePerSec, EngineRPMRatio), 1);
            }

            EngineRPM = Variable2 * (MaxRPM - IdleRPM) + IdleRPM;

            if (DynamicBrakePercent > 0)
            {
                if (MaxDynamicBrakeForceN == 0)
                    Variable3 = DynamicBrakePercent / 100f;
                else
                    Variable3 = Math.Abs(MotiveForceN) / MaxDynamicBrakeForceN;
            }
            else
                Variable3 = 0;

            if (elapsedClockSeconds > 0.0f)
            {
                EngineRPMderivation = (EngineRPM - EngineRPMold)/elapsedClockSeconds;
                EngineRPMold = EngineRPM;
            }

            if ((MainResPressurePSI < CompressorRestartPressurePSI) && (!CompressorIsOn) && (PowerOn))
                SignalEvent(Event.CompressorOn);
            else if (MainResPressurePSI > MaxMainResPressurePSI && CompressorIsOn)
                SignalEvent(Event.CompressorOff);
            if ((CompressorIsOn)&&(PowerOn))
                MainResPressurePSI += elapsedClockSeconds * MainResChargingRatePSIpS;
            
            if (Train.TrainType == Train.TRAINTYPE.PLAYER && this.IsLeadLocomotive())
                TrainControlSystem.Update();

            PrevMotiveForceN = MotiveForceN;
            base.UpdateParent(elapsedClockSeconds); // Calls the Update() method in the parent class MSTSLocomotive which calls Update() on its parent MSTSWagon which calls ...
        }

        public override float GetDataOf(CabViewControl cvc)
        {
            float data = 0;

            switch (cvc.ControlType)
            {
                case CABViewControlTypes.GEARS:
                    if (DieselEngines.HasGearBox)
                        data = DieselEngines[0].GearBox.CurrentGearIndex + 1;
                    break;
                case CABViewControlTypes.FUEL_GAUGE:
                    if (cvc.Units == CABViewControlUnits.GALLONS)
                        data = L.ToGUS(DieselLevelL);
                    else
                        data = DieselLevelL;
                    break;
                default:
                    data = base.GetDataOf(cvc);
                    break;
            }

            return data;
        }

        public override string GetStatus()
        {
            var result = new StringBuilder();

            result.AppendFormat("Diesel engine = {0}\n", DieselEngines[0].EngineStatus.ToString());
            if(DieselEngines.HasGearBox)
                result.AppendFormat("Diesel RPM = {0:F0} - Gear: {1}\n", DieselEngines[0].RealRPM, DieselEngines[0].GearBox.CurrentGearIndex < 0 ? "N" : (DieselEngines[0].GearBox.CurrentGearIndex + 1).ToString(), DieselEngines[0].GearBox.GearBoxOperation == GearBoxOperation.Automatic ? "Automatic gear" : "");
            else
                result.AppendFormat("Diesel RPM = {0:F0}\n", DieselEngines[0].RealRPM);
            result.AppendFormat("Diesel level = {0:F0} L ({1:F0} gal)\n", DieselLevelL, DieselLevelL / 3.785f);
            result.AppendFormat("Diesel flow = {0:F1} L/h ({1:F1} gal/h)", DieselFlowLps * 3600.0f, DieselFlowLps * 3600.0f / 3.785f);
            return result.ToString();
        }

        public override string GetDebugStatus()
        {
            var status = new StringBuilder();
            status.AppendFormat("Car {0}\t{2} {1}\t{3}\t{4:F0}%\t{5:F0}m/s\t{6:F0}kW\t{7:F0}kN\t{8}\t{9}\t", UiD, Flipped ? "(flip)" : "", Direction == Direction.Forward ? "Fwd" : Direction == Direction.Reverse ? "Rev" : "N", AcceptMUSignals ? "MU'd" : "Single", ThrottlePercent, SpeedMpS, MotiveForceN * SpeedMpS / 1000, MotiveForceN / 1000, WheelSlip ? "Slipping" : "", CouplerOverloaded ? "Coupler overloaded" : "");
            if(DieselEngines.HasGearBox)
                status.AppendFormat("Diesel:\t{0}\t{1:F0}RPM\tGear {2}\t Fuel \t{3:F0}L\t{4:F0}L/h", DieselEngines[0].EngineStatus, DieselEngines[0].RealRPM, DieselEngines.HasGearBox ? DieselEngines[0].GearBox.CurrentGearIndex : 0, DieselLevelL, DieselFlowLps * 3600.0f);
            else
                status.AppendFormat("Diesel:\t{0}\t{1:F0}RPM\t Fuel \t{2:F0}L\t{3:F0}L/h", DieselEngines[0].EngineStatus, DieselEngines[0].RealRPM, DieselLevelL, DieselFlowLps * 3600.0f);
            return status.ToString();
        }

        /// <summary>
        /// Catch the signal to start or stop the diesel
        /// </summary>
        public void StartStopDiesel()
        {
            if (!this.IsLeadLocomotive() && (this.ThrottlePercent == 0))
                PowerOn = !PowerOn;
        }

        public override void SetPower(bool ToState)
        {
            if (ToState)
            {
                foreach (DieselEngine engine in DieselEngines)
                    engine.Start();
            }
            else
            {
                foreach (DieselEngine engine in DieselEngines)
                    engine.Stop();
            }

            base.SetPower(ToState);
        }

        public void RefillWithDiesel()
        {
            DieselLevelL = MaxDieselLevelL;
        }

        //CJ
        public override void Refuel()
        {
            RefillWithDiesel();
            Simulator.Confirmer.Confirm(CabControl.DieselFuel, CabSetting.On);
        }
    } // class DieselLocomotive

    ///////////////////////////////////////////////////
    ///   3D VIEW
    ///////////////////////////////////////////////////

    /// <summary>
    /// Adds any special Diesel loco animation to the basic LocomotiveViewer class
    /// </summary>
    class MSTSDieselLocomotiveViewer : MSTSLocomotiveViewer
    {
        MSTSDieselLocomotive DieselLocomotive { get { return (MSTSDieselLocomotive)Car; } }
        List<ParticleEmitterDrawer> Exhaust = new List<ParticleEmitterDrawer>();

        public MSTSDieselLocomotiveViewer(Viewer3D viewer, MSTSDieselLocomotive car)
            : base(viewer, car)
        {
            // Now all the particle drawers have been setup, assign them textures based
            // on what emitters we know about.

            string dieselTexture = viewer.Simulator.BasePath + @"\GLOBAL\TEXTURES\dieselsmoke.ace";

            foreach (var drawers in from drawer in ParticleDrawers
                                    where drawer.Key.ToLowerInvariant().StartsWith("exhaust")
                                    select drawer.Value)
            {
                Exhaust.AddRange(drawers);
            }
            foreach (var drawer in Exhaust)
            {
                drawer.SetTexture(viewer.TextureManager.Get(dieselTexture));
                drawer.SetEmissionRate(car.ExhaustParticles);
                drawer.SetParticleDuration(car.ExhaustDynamics);
            }
        }

        
        /// <summary>
        /// A keyboard or mouse click has occured. Read the UserInput
        /// structure to determine what was pressed.
        /// </summary>
        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
            if( UserInput.IsPressed( UserCommands.ControlDieselPlayer ) ) {
                if( DieselLocomotive.ThrottlePercent < 1 ) 
                {
//                    DieselLocomotive.PowerOn = !DieselLocomotive.PowerOn;
                    if (DieselLocomotive.DieselEngines[0].EngineStatus == DieselEngine.Status.Stopped)
                    {
                        DieselLocomotive.DieselEngines[0].Start();
                        DieselLocomotive.SignalEvent(Event.EnginePowerOn); // power on sound hook
                    }
                    if (DieselLocomotive.DieselEngines[0].EngineStatus == DieselEngine.Status.Running)
                    {
                        DieselLocomotive.DieselEngines[0].Stop();
                        DieselLocomotive.SignalEvent(Event.EnginePowerOff); // power off sound hook
                    }
                    Viewer.Simulator.Confirmer.Confirm( CabControl.PlayerDiesel, DieselLocomotive.DieselEngines.PowerOn ? CabSetting.On : CabSetting.Off );
                } 
                else
                {
                    Viewer.Simulator.Confirmer.Warning( CabControl.PlayerDiesel, CabSetting.Warn1 );
                }
            }
            if (UserInput.IsPressed(UserCommands.ControlDieselHelper))
            {
                var powerOn = false;
                var helperLocos = 0;

                foreach (var car in DieselLocomotive.Train.Cars)
                {
                    var mstsDieselLocomotive = car as MSTSDieselLocomotive;
                    if (mstsDieselLocomotive != null)
                    {
                        if (mstsDieselLocomotive.DieselEngines.Count > 0)
                        {
                            if ((car == Program.Simulator.PlayerLocomotive))
                            {
                                if ((mstsDieselLocomotive.DieselEngines.Count > 1))
                                {
                                    for (int i = 1; i < mstsDieselLocomotive.DieselEngines.Count; i++)
                                    {
                                        if (mstsDieselLocomotive.DieselEngines[i].EngineStatus == DieselEngine.Status.Stopped)
                                        {
                                            mstsDieselLocomotive.DieselEngines[i].Start();
                                        }
                                        if (mstsDieselLocomotive.DieselEngines[i].EngineStatus == DieselEngine.Status.Running)
                                        {
                                            mstsDieselLocomotive.DieselEngines[i].Stop();
                                        }
                                    }
                                }
                            }
                            else
                            {
                                foreach (DieselEngine de in mstsDieselLocomotive.DieselEngines)
                                {
                                    if (de.EngineStatus == DieselEngine.Status.Stopped)
                                    {
                                        de.Start();
                                    }
                                    if (de.EngineStatus == DieselEngine.Status.Running)
                                    {
                                        de.Stop();
                                    }
                                }
                            }
                        }
                        //mstsDieselLocomotive.StartStopDiesel();
                        powerOn = mstsDieselLocomotive.DieselEngines.PowerOn;
                        if ((car != Program.Simulator.PlayerLocomotive)&&(mstsDieselLocomotive.AcceptMUSignals))
                        {
                            if ((mstsDieselLocomotive.DieselEngines[0].EngineStatus == DieselEngine.Status.Stopped) ||
                                (mstsDieselLocomotive.DieselEngines[0].EngineStatus == DieselEngine.Status.Stopping))
                                mstsDieselLocomotive.SignalEvent(Event.EnginePowerOff);
                            else
                                mstsDieselLocomotive.SignalEvent(Event.EnginePowerOn);
                        }
                        helperLocos++;
                    }
                }
                // One confirmation however many helper locomotives
                // <CJComment> Couldn't make one confirmation per loco work correctly :-( </CJComment>
                if( helperLocos > 0 ) {
                    Viewer.Simulator.Confirmer.Confirm( CabControl.HelperDiesel, powerOn ? CabSetting.On : CabSetting.Off );
                }
            }
            base.HandleUserInput(elapsedTime);
        }


        /// <summary>
        /// We are about to display a video frame.  Calculate positions for 
        /// animated objects, and add their primitives to the RenderFrame list.
        /// </summary>
        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            foreach (var drawer in Exhaust)
            {
                drawer.SetEmissionRate(((MSTSDieselLocomotive)this.Car).ExhaustParticles);
                drawer.SetEmissionColor(((MSTSDieselLocomotive)this.Car).ExhaustColor);
                drawer.SetParticleDuration(((MSTSDieselLocomotive)this.Car).ExhaustDynamics);
            }
            base.PrepareFrame(frame, elapsedTime);
        }

        /// <summary>
        /// This doesn't function yet.
        /// </summary>
        public override void Unload()
        {
            base.Unload();
        }

    } // class MSTSDieselLocomotiveViewer

}
