﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ORTS;


namespace ORTS
{
    /// <summary>
    /// Axle drive type to determine an input and solving method for axles
    /// </summary>
    public enum AxleDriveType
    {
        /// <summary>
        /// Without any drive
        /// </summary>
        NotDriven = 0,
        /// <summary>
        /// Traction motor conected through gearbox to axle
        /// </summary>
        MotorDriven = 1,
        /// <summary>
        /// Simple force driven axle
        /// </summary>
        ForceDriven = 2
    }
    /// <summary>
    /// Axle class by Matej Pacha (c)2011, University of Zilina, Slovakia (matej.pacha@kves.uniza.sk)
    /// Use of this code is restricted outside of OpenRails Simulator without special permission!
    /// The class is used to manage and simulate axle forces considering adhesion problems.
    /// Basic configuration:
    ///  - Motor generates motive torque what is converted into a motive force (through gearbox)
    ///    or the motive force is passed directly to the DriveForce property
    ///  - With known TrainSpeed the Update(timeSpan) method computes a dynamic model of the axle
    ///     - additional (optional) parameters are weather conditions and correction parameter
    ///  - Finally an output motive force is stored into the AxleForce
    ///  
    /// Every computation within Axle class uses SI-units system with xxxxxUUU unit notation
    /// </summary>
    public class Axle
    {
        /// <summary>
        /// Integrator used for axle dynamic solving
        /// </summary>
        Integrator axleRevolutionsInt = new Integrator(0.0f, IntegratorMethods.EulerBackMod);

        /// <summary>
        /// Brake force covered by BrakeForceN interface
        /// </summary>
        protected float brakeForceN;
        /// <summary>
        /// Read/Write positive only brake force to the axle, in Newtons
        /// </summary>
        public float BrakeForceN
        {
            set { brakeForceN = value; }
            get { return brakeForceN; }
        }

        /// <summary>
        /// Total force to store sum of all functions
        /// </summary>
        protected float totalForceN;

        /// <summary>
        /// Damping force covered by DampingForceN interface
        /// </summary>
        protected float dampingNs;
        /// <summary>
        /// Read/Write positive only damping force to the axle, in Newton-second
        /// </summary>
        public float DampingNs { set { dampingNs = Math.Abs(value); } get { return dampingNs; } }

        /// <summary>
        /// Axle drive type covered by DriveType interface
        /// </summary>
        protected AxleDriveType driveType;
        /// <summary>
        /// Read/Write Axle drive type flag
        /// </summary>
        public AxleDriveType DriveType { set { driveType = value; } get { return driveType; } }

        /// <summary>
        /// Axle drive represented by a motor, covered by ElectricMotor interface
        /// </summary>
        ElectricMotor motor = null;
        /// <summary>
        /// Read/Write Motor drive parameter.
        /// With setting a value the totalInertiaKgm2 is updated
        /// </summary>
        public ElectricMotor Motor
        {
            set
            {
                motor = value;
                switch(driveType)
                {
                    case AxleDriveType.NotDriven:
                        break;
                    case AxleDriveType.MotorDriven:
                        //Total inertia considering gearbox
                        totalInertiaKgm2 = inertiaKgm2 + transmitionRatio * transmitionRatio * motor.InertiaKgm2;
                        break;
                    case AxleDriveType.ForceDriven:
                        totalInertiaKgm2 = inertiaKgm2;
                        break;
                    default:
                        totalInertiaKgm2 = inertiaKgm2;
                        break;
                }
            }
            get
            {
                return motor;
            }

        }

        /// <summary>
        /// Drive force covered by DriveForceN interface, in Newtons
        /// </summary>
        protected float driveForceN;
        /// <summary>
        /// Read/Write drive force used to pass the force directly to the axle without gearbox, in Newtons
        /// </summary>
        public float DriveForceN { set { driveForceN = value; } get { return driveForceN; } }

        /// <summary>
        /// Sum of inertia over all axle conected rotating mass, in kg.m^2
        /// </summary>
        float totalInertiaKgm2;

        /// <summary>
        /// Axle inertia covered by InertiaKgm2 interface, in kg.m^2
        /// </summary>
        float inertiaKgm2;
        /// <summary>
        /// Read/Write positive non zero only axle inertia, in kg.m^2
        /// By setting this parameter the totalInertiaKgm2 is updated
        /// Throws exception when zero or negative value is passed
        /// </summary>
        public float InertiaKgm2
        {
            set
            {
                if (value <= 0.0)
                    throw new NotSupportedException("Inertia must be greater than zero");
                inertiaKgm2 = value;
                switch (driveType)
                {
                    case AxleDriveType.NotDriven:
                        break;
                    case AxleDriveType.MotorDriven:
                        totalInertiaKgm2 = inertiaKgm2 + transmitionRatio * transmitionRatio * motor.InertiaKgm2;
                        break;
                    case AxleDriveType.ForceDriven:
                        totalInertiaKgm2 = inertiaKgm2;
                        break;
                    default:
                        totalInertiaKgm2 = inertiaKgm2;
                        break;
                }
            }
            get 
            {
                return inertiaKgm2;
            }
        }

        /// <summary>
        /// Transmition ratio on gearbox covered by TransmitionRatio interface
        /// </summary>
        float transmitionRatio;
        /// <summary>
        /// Read/Write positive nonzero transmition ratio, given by n1:n2 ratio
        /// Throws an exception when negative or zero value is passed
        /// </summary>
        public float TransmitionRatio
        {
            set
            {
                if (value <= 0.0)
                    throw new NotSupportedException("Transmition ratio must be greater than zero");
                transmitionRatio = value;
            }
            get
            {
                return transmitionRatio;
            }
        }

        /// <summary>
        /// Transmition efficiency, relative to 1.0, covered by TransmitionEfficiency interface
        /// </summary>
        float transmitionEfficiency;
        /// <summary>
        /// Read/Write transmition efficiency, relattve to 1.0, within range of 0.0 to 1.0 (1.0 means 100%, 0.5 means 50%)
        /// Throws an exception when out of range value is passed
        /// When 0.0 is set the value of 0.99 is used instead
        /// </summary>
        public float TransmitionEfficiency
        {
            set
            {
                if (value > 1.0f)
                    throw new NotSupportedException("Value must be within the range of 0.0 and 1.0");
                if (value <= 0.0f)
                    transmitionEfficiency = 0.99f;
                else
                    transmitionEfficiency = value;
            }
            get
            {
                return transmitionEfficiency;
            }
        }

        /// <summary>
        /// Axle diameter value, covered by AxleDiameterM interface, in metric meters
        /// </summary>
        float axleDiameterM;
        /// <summary>
        /// Read/Write nonzero positive axle diameter parameter, in metric meters
        /// Throws exception when zero or negative value is passed
        /// </summary>
        public float AxleDiameterM
        {
            set
            {
                if (value <= 0.0f)
                    throw new NotSupportedException("Axle diameter must be greater than zero");
                axleDiameterM = value;
            }
            get
            {
                return axleDiameterM;
            }
        }

        /// <summary>
        /// Read/Write adhesion conditions parameter
        /// Should be set within the range of 0.3 to 1.2 but there is no restriction
        /// - Set 1.0 for dry weather (standard)
        /// - Set 0.7 for wet, rainy weather
        /// </summary>
        public float AdhesionConditions { set; get; }

        /// <summary>
        /// Read/Write correction parameter of adhesion, it has proportional impact on adhesion limit
        /// Should be set to 1.0 for most cases
        /// </summary>
        public float AdhesionK { set; get; }

        /// <summary>
        /// Axle speed value, covered by AxleSpeedMpS interface, in metric meters per second
        /// </summary>
        float axleSpeedMpS;
        /// <summary>
        /// Read only axle speed value, in metric meters per second
        /// </summary>
        public float AxleSpeedMpS
        {
            get
            {
                return axleSpeedMpS;
            }
        }

        /// <summary>
        /// Axle force value, covered by AxleForceN interface, in Newtons
        /// </summary>
        float axleForceN;
        /// <summary>
        /// Read only axle force value, in Newtons
        /// </summary>
        public float AxleForceN
        {
            get
            {
                return axleForceN;
            }
            /*set
            {
                axleForceN = value;
            }*/
        }

        /// <summary>
        /// Read/Write axle weight parameter in Newtons
        /// </summary>
        public float AxleWeightN { set; get; }

        /// <summary>
        /// Read/Write train speed parameter in metric meters per second
        /// </summary>
        public float TrainSpeedMpS { set; get; }

        /// <summary>
        /// Read only wheel slip indicator
        /// - is true when absolute value of SlipSpeedMpS is greater than WheelSlipThresholdMpS, otherwise is false
        /// </summary>
        public bool IsWheelSlip
        {
            get
            {
                if (Math.Abs(SlipSpeedMpS) > WheelSlipThresholdMpS) 
                    return true;
                else
                    return false;
            }
        }

        /// <summary>
        /// Read only wheelslip threshold value used to indicate maximal effective slip
        /// - its value si computed as a maximum of slip function:
        ///                 2*K*umax^2 * dV
        ///   f(dV) = u = ---------------------
        ///                umax^2*dV^2 + K^2
        ///   maximum can be found as a derivation f'(dV) = 0
        /// </summary>
        public float WheelSlipThresholdMpS
        {
            get
            {
                if (AdhesionK == 0.0f)
                    AdhesionK = 1.0f;
                float A = 2.0f*AdhesionK*AdhesionConditions*AdhesionConditions;
                float B = AdhesionConditions*AdhesionConditions;
                float C = AdhesionK*AdhesionK;
                float a = -2.0f*A*B;
                float b = A*B;
                float c = A*C;
                return ((-b - (float)Math.Sqrt(b * b - 4.0f * a * c)) / (2.0f * a));
            }
        }

        /// <summary>
        /// Read only wheelslip warning indication
        /// - is true when SlipSpeedMpS is greater than zero and 
        ///   SlipSpeedPercent is greater than SlipWarningThresholdPercent in both directions,
        ///   otherwise is false
        /// </summary>
        public bool IsWheelSlipWarning
        {
            get
            {
                if (SlipSpeedMpS > 0.0f)
                {
                    if ((SlipSpeedPercent > (SlipWarningTresholdPercent)))
                        return true;
                    else
                        return false;
                }
                if (SlipSpeedMpS < 0.0f)
                {
                    if ((SlipSpeedPercent < ( -SlipWarningTresholdPercent)))
                        return true;
                    else
                        return false;
                }
                else
                    return false;
            }
        }

        /// <summary>
        /// Read only slip speed value in metric meters per second
        /// - computed as a substraction of axle speed and train speed
        /// </summary>
        public float SlipSpeedMpS
        {
            get
            {
                return (axleSpeedMpS - TrainSpeedMpS);
            }
        }

        /// <summary>
        /// Read only relative slip speed value, in percent
        /// - the value is relative to WheelSlipThreshold value
        /// </summary>
        public float SlipSpeedPercent
        {
            get
            {
                return SlipSpeedMpS / WheelSlipThresholdMpS * 100.0f;
            }
        }

        /// <summary>
        /// Slip speed rate of change value, in metric (meters per second) per second
        /// </summary>
        protected float slipDerivationMpSS;
        /// <summary>
        /// Slip speed memorized from previous iteration
        /// </summary>
        protected float previousSlipSpeedMpS;
        /// <summary>
        /// Read only slip speed rate of change, in metric (meters per second) per second
        /// </summary>
        public float SlipDerivationMpSS
        {
            get
            {
                return slipDerivationMpSS;
            }
        }

        /// <summary>
        /// Relative slip rate of change
        /// </summary>
        protected float slipDerivationPercentpS;
        /// <summary>
        /// Relativ slip speed from previous iteration
        /// </summary>
        protected float previousSlipPercent;
        /// <summary>
        /// Read only relative slip speed rate of change, in percent per second
        /// </summary>
        public float SlipDerivationPercentpS
        {
            get
            {
                return slipDerivationPercentpS;
            }
        }

        /// <summary>
        /// Read/Write relative slip speed warning threshold value, in percent of maximal effective slip
        /// </summary>
        public float SlipWarningTresholdPercent { set; get; }

        /// <summary>
        /// Nonparametric constructor of Axle class instance
        /// - sets motor parameter to null
        /// - sets TransmitionEfficiency to 0.99 (99%)
        /// - sets SlipWarningThresholdPercent to 70%
        /// - sets axle DriveType to ForceDriven
        /// - updates totalInertiaKgm2 parameter
        /// </summary>
        public Axle()
        {
            motor = null;
            transmitionEfficiency = 0.99f;
            SlipWarningTresholdPercent = 70.0f;
            driveType = AxleDriveType.ForceDriven;
            axleRevolutionsInt.IsLimited = true;
            
            switch (driveType)
            {
                case AxleDriveType.NotDriven:
                    break;
                case AxleDriveType.MotorDriven:
                    axleRevolutionsInt.Max = 5000.0f;
                    axleRevolutionsInt.Min = -5000.0f;
                    totalInertiaKgm2 = inertiaKgm2 + transmitionRatio * transmitionRatio * motor.InertiaKgm2;
                    break;
                case AxleDriveType.ForceDriven:
                    axleRevolutionsInt.Max = 100.0f;
                    axleRevolutionsInt.Min = -100.0f;
                    totalInertiaKgm2 = inertiaKgm2;
                    break;
                default:
                    totalInertiaKgm2 = inertiaKgm2;
                    break;
            }
        }

        /// <summary>
        /// Creates motor driven axle class instance
        /// - sets TransmitionEfficiency to 0.99 (99%)
        /// - sets SlipWarningThresholdPercent to 70%
        /// - sets axle DriveType to MotorDriven
        /// - updates totalInertiaKgm2 parameter
        /// </summary>
        /// <param name="electricMotor">Electric motor connected with the axle</param>
        public Axle(ElectricMotor electricMotor)
        {
            motor = electricMotor;
            motor.AxleConnected = this;
            transmitionEfficiency = 0.99f;
            driveType = AxleDriveType.MotorDriven;
            axleRevolutionsInt.IsLimited = true;
            switch (driveType)
            {
                case AxleDriveType.NotDriven:
                    totalInertiaKgm2 = inertiaKgm2;
                    break;
                case AxleDriveType.MotorDriven:
                    axleRevolutionsInt.Max = 5000.0f;
                    axleRevolutionsInt.Min = -5000.0f;
                    totalInertiaKgm2 = inertiaKgm2 + transmitionRatio * transmitionRatio * motor.InertiaKgm2;
                    break;
                case AxleDriveType.ForceDriven:
                    axleRevolutionsInt.Max = 100.0f;
                    axleRevolutionsInt.Min = -100.0f;
                    totalInertiaKgm2 = inertiaKgm2;
                    break;
                default:
                    totalInertiaKgm2 = inertiaKgm2;
                    break;
            }
        }

        /// <summary>
        /// Main Update method
        /// - computes slip characteristics to get new axle force
        /// - computes axle dynamic model according to its driveType
        /// - computes wheelslip indicators
        /// </summary>
        /// <param name="timeSpan"></param>
        public virtual void Update(float timeSpan)
        {
            //Update axle force ( = k * loadTorqueNm)
            axleForceN = AxleWeightN * SlipCharacteristics(AxleSpeedMpS - TrainSpeedMpS, TrainSpeedMpS, AdhesionK, AdhesionConditions);                
            switch (driveType)
            {
                case AxleDriveType.NotDriven:
                    //Axle revolutions integration
                    axleSpeedMpS = axleRevolutionsInt.Integrate(timeSpan,
                        axleDiameterM * axleDiameterM / (4.0f * (totalInertiaKgm2))
                        * (2.0f * transmitionRatio / axleDiameterM * (-Math.Abs(brakeForceN)) - AxleForceN));
                    break;
                case AxleDriveType.MotorDriven:
                    //Axle revolutions integration
                    if (TrainSpeedMpS == 0.0f)
                    {
                        dampingNs = 0.0f;
                        brakeForceN = 0.0f;
                    }
                    axleSpeedMpS = axleRevolutionsInt.Integrate(timeSpan,
                        axleDiameterM * axleDiameterM / (4.0f * (totalInertiaKgm2))
                        * (2.0f * transmitionRatio / axleDiameterM * motor.DevelopedTorqueNm * transmitionEfficiency
                        - Math.Abs(brakeForceN) - (axleSpeedMpS > 0.0 ? Math.Abs(dampingNs) : 0.0f)) - AxleForceN);

                    //update motor values
                    motor.RevolutionsRad = axleSpeedMpS * 2.0f * transmitionRatio / (axleDiameterM);
                    motor.Update(timeSpan);
                    break;
                case AxleDriveType.ForceDriven:
                    //Axle revolutions integration
                    if (TrainSpeedMpS == 0.0f) 
                    {
                        if (Math.Abs(driveForceN) == 0.0f)
                        {
                            Reset();
                            axleSpeedMpS = 0.0f;
                            axleForceN = 0.0f;
                        }
                        else
                            axleForceN = driveForceN;
                    }
                    else
                    {
                        if (axleSpeedMpS > 0.0f)
                        {
                            axleSpeedMpS = axleRevolutionsInt.Integrate(timeSpan,
                                    (
                                        (
                                        driveForceN * transmitionEfficiency
                                        - brakeForceN
                                        - slipDerivationMpSS * dampingNs
                                        - AxleForceN
                                        )
                                    / totalInertiaKgm2)
                                    );
                        }
                        else
                        {
                            axleSpeedMpS = axleRevolutionsInt.Integrate(timeSpan,
                                    (
                                        (
                                        driveForceN * transmitionEfficiency
                                        + brakeForceN
                                        - slipDerivationMpSS * dampingNs
                                        - AxleForceN
                                        )
                                    / totalInertiaKgm2)
                                    );
                        }
                    }
                    break;
                default:
                    totalInertiaKgm2 = inertiaKgm2;
                    break;
            }

            slipDerivationMpSS = (SlipSpeedMpS - previousSlipSpeedMpS)/ timeSpan;
            previousSlipSpeedMpS = SlipSpeedMpS;

            slipDerivationPercentpS = (SlipSpeedPercent - previousSlipPercent) / timeSpan;
            previousSlipPercent = SlipSpeedPercent;
            
        }

        /// <summary>
        /// Resets all integral values (set to zero)
        /// </summary>
        public void Reset()
        {
            axleRevolutionsInt.Reset();
            if (motor != null)
                motor.Reset();

        }

        /// <summary>
        /// Slip characteristics computation
        /// - Computes adhesion limit using Curtius-Kniffler formula:
        ///                 7.5
        ///     umax = ---------------------  + 0.161
        ///             speed * 3.6 + 44.0
        /// - Computes slip speed
        /// - Computes relative adhesion force as a result of slip characteristics:
        ///             2*K*umax^2*dV
        ///     u = ---------------------
        ///           umax^2*dv^2 + K^2
        /// </summary>
        /// <param name="slipSpeed">Diference between train speed and wheel speed MpS</param>
        /// <param name="speed">Current speed MpS</param>
        /// <param name="K">Slip speed correction. If is set K = 0 then K = 0.7 is used</param>
        /// <param name="conditions">Relative weather conditions, usually from 0.2 to 1.0</param>
        /// <returns>Relative force transmitted to the rail</returns>
        public static float SlipCharacteristics(float slipSpeed, float speed, float K, float conditions)
        {
            speed = Math.Abs(3.6f*speed);
            float umax = (7.5f / (speed + 44.0f) + 0.161f); // Curtius - Kniffler equation
            umax *= conditions;
            if (K == 0.0)
                K = 1;
            slipSpeed *= 3.6f;
            return 2.0f * K * umax * umax * slipSpeed / (umax * umax * slipSpeed * slipSpeed + K * K);
        }

        /// <summary>
        /// Optional Friction computation function
        /// - Computes Davis formula for given parameters:
        ///     Fo = Weight / 9810 * (A + B * V + C * V^2)
        /// </summary>
        /// <param name="A">Static friction parameter [N/kN]</param>
        /// <param name="B">Rolling friction parameter [N/kN]</param>
        /// <param name="C">Air friction parameter [N/kN]</param>
        /// <param name="speedMpS">Speed in MpS</param>
        /// <param name="weight">Weight in kg</param>
        /// <returns>Friction force in Newtons, Returns zero for zero speed, Returns negative for negative speed</returns>
        public static float Friction(float A, float B, float C, float speedMpS, float weight)
        {
            speedMpS *= 3.6f;
            if (speedMpS == 0.0f)
                return 0.0f;
            if (speedMpS > 0.0f)
                return weight / 1000.0f * 9.81f * (A + B * speedMpS + C * speedMpS * speedMpS);
            else
                return -weight / 1000.0f * 9.81f * (A + B * -1.0f * speedMpS + C * speedMpS * speedMpS);
        }
    }
}